using System;
using System.Data;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QLKS.Data;
using QLKS.Infrastructure;
using QLKS.Models;
using QLKS.Services;

namespace QLKS.Controllers
{
    [RoleAuthorize(RoleNames.Admin, RoleNames.Receptionist)]
    public class HoaDonController : BaseController
    {
        private readonly QLKSEntities db = new QLKSEntities();

        public ActionResult Index(string maHoaDon, string khachHang, int? trangThai, DateTime? tuNgay, DateTime? denNgay, int page = 1)
        {
            const int pageSize = 15;
            page = Math.Max(1, page);
            var query = db.HoaDons
                .Include(x => x.KhachHang)
                .Include(x => x.ChiTietHoaDons).ThenInclude(y => y.Phong)
                .Include(x => x.ChiTietDichVus)
                .AsQueryable();

            var normalizedCode = string.IsNullOrWhiteSpace(maHoaDon) ? null : maHoaDon.Trim().TrimStart('#');
            int invoiceId;
            if (normalizedCode != null)
            {
                if (int.TryParse(normalizedCode, out invoiceId)) query = query.Where(x => x.MaHD == invoiceId);
                else query = query.Where(x => false);
            }

            var normalizedCustomer = string.IsNullOrWhiteSpace(khachHang) ? null : khachHang.Trim();
            if (normalizedCustomer != null) query = query.Where(x => x.KhachHang.TenKH.Contains(normalizedCustomer));
            if (trangThai.HasValue) query = query.Where(x => x.TinhTrang == trangThai.Value);
            if (tuNgay.HasValue) query = query.Where(x => x.NgayLap.HasValue && x.NgayLap.Value.Date >= tuNgay.Value.Date);
            if (denNgay.HasValue) query = query.Where(x => x.NgayLap.HasValue && x.NgayLap.Value.Date <= denNgay.Value.Date);

            if (tuNgay.HasValue && denNgay.HasValue && tuNgay.Value.Date > denNgay.Value.Date)
                ModelState.AddModelError(string.Empty, "Ngày bắt đầu không được sau ngày kết thúc.");

            var totalItems = query.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages > 0 && page > totalPages) page = totalPages;
            var invoices = query.OrderByDescending(x => x.MaHD).Skip((page - 1) * pageSize).Take(pageSize).ToList();

            return View(new InvoiceIndexViewModel
            {
                InvoiceCode = normalizedCode,
                Customer = normalizedCustomer,
                Status = trangThai,
                FromDate = tuNgay,
                ToDate = denNgay,
                Results = new PagedResultViewModel<HoaDon>
                {
                    Items = invoices,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalItems = totalItems
                }
            });
        }

        public ActionResult Create(int? MaPhong)
        {
            var model = new CheckInViewModel { MaPhong = MaPhong, NgayCheckIn = DateTime.Now };
            LoadCheckInLists(model.MaKH, model.MaPhong);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CheckInViewModel model)
        {
            if (model.NgayCheckIn.HasValue && model.NgayCheckIn.Value > DateTime.Now.AddMinutes(5))
                ModelState.AddModelError("NgayCheckIn", "Ngày check-in không được ở tương lai.");
            if (model.NgayCheckIn.HasValue && model.NgayCheckIn.Value < DateTime.Today.AddDays(-1))
                ModelState.AddModelError("NgayCheckIn", "Ngày check-in không được lùi quá một ngày.");

            if (!ModelState.IsValid)
            {
                LoadCheckInLists(model.MaKH, model.MaPhong);
                return View(model);
            }

            using (var transaction = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    var customer = db.KhachHangs.Find(model.MaKH.Value);
                    var room = db.Phongs.Include(x => x.LoaiPhong).FirstOrDefault(x => x.MaPhong == model.MaPhong.Value);
                    if (customer == null) ModelState.AddModelError("MaKH", "Khách hàng không tồn tại.");
                    if (room == null) ModelState.AddModelError("MaPhong", "Phòng không tồn tại.");
                    else
                    {
                        if (room.TrangThai != RoomStatus.Available)
                            ModelState.AddModelError("MaPhong", "Phòng không còn ở trạng thái Trống.");
                        if (room.LoaiPhong == null || !room.LoaiPhong.GiaMacDinh.HasValue || room.LoaiPhong.GiaMacDinh.Value < 0)
                            ModelState.AddModelError("MaPhong", "Phòng chưa có giá hợp lệ.");

                        var hasActiveInvoice = db.ChiTietHoaDons.Any(x =>
                            x.MaPhong == room.MaPhong && x.HoaDon.DaThanhToan != true &&
                            (x.HoaDon.TinhTrang == (int)InvoiceStatus.Reserved || x.HoaDon.TinhTrang == (int)InvoiceStatus.CheckedIn));
                        if (hasActiveInvoice) ModelState.AddModelError("MaPhong", "Phòng đang có hóa đơn hoạt động.");
                    }

                    if (!ModelState.IsValid)
                    {
                        transaction.Rollback();
                        LoadCheckInLists(model.MaKH, model.MaPhong);
                        return View(model);
                    }

                    var userId = (int)Session[SessionKeys.UserId];
                    var invoice = new HoaDon
                    {
                        MaKH = customer.MaKH,
                        MaNV = userId,
                        NgayLap = DateTime.Now,
                        NgayCheckIn = model.NgayCheckIn,
                        GhiChu = string.IsNullOrWhiteSpace(model.GhiChu) ? null : model.GhiChu.Trim(),
                        DaThanhToan = false,
                        TinhTrang = (int)InvoiceStatus.CheckedIn
                    };
                    invoice.ChiTietHoaDons.Add(new ChiTietHoaDon
                    {
                        MaPhong = room.MaPhong,
                        DonGiaThucTe = room.LoaiPhong.GiaMacDinh,
                        SoNgayO = 0
                    });
                    room.TrangThai = RoomStatus.Occupied;
                    db.HoaDons.Add(invoice);
                    db.SaveChanges();

                    AuditLogService.Write(db, userId, "Check-in", "Check-in phòng " + room.SoPhong + ", hóa đơn #" + invoice.MaHD + ".");
                    db.SaveChanges();
                    transaction.Commit();
                    TempData["Success"] = "Check-in thành công.";
                    return RedirectToAction("Details", new { id = invoice.MaHD });
                }
                catch (DbUpdateException)
                {
                    transaction.Rollback();
                    ModelState.AddModelError(string.Empty, "Không thể check-in do dữ liệu vừa thay đổi. Vui lòng tải lại và thử lại.");
                }
                catch (DataException)
                {
                    transaction.Rollback();
                    ModelState.AddModelError(string.Empty, "Không thể lưu check-in. Vui lòng kiểm tra dữ liệu và thử lại.");
                }
            }

            LoadCheckInLists(model.MaKH, model.MaPhong);
            return View(model);
        }

        public ActionResult Details(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(System.Net.HttpStatusCode.BadRequest);
            var invoice = LoadInvoice(id.Value);
            if (invoice == null) return HttpNotFound();

            ViewBag.DichVuList = new SelectList(db.DichVus.OrderBy(x => x.TenDV).ToList(), "MaDV", "TenDV");
            return View(invoice);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ThemDichVu(int MaHD, int MaDV, int SoLuong)
        {
            if (SoLuong <= 0)
            {
                TempData["Error"] = "Số lượng dịch vụ phải lớn hơn 0.";
                return RedirectToAction("Details", new { id = MaHD });
            }

            var invoice = db.HoaDons.Find(MaHD);
            var service = db.DichVus.Find(MaDV);
            if (invoice == null || service == null) return HttpNotFound();
            if (invoice.DaThanhToan == true || invoice.TinhTrang == (int)InvoiceStatus.Paid)
            {
                TempData["Error"] = "Không thể thêm dịch vụ vào hóa đơn đã thanh toán.";
                return RedirectToAction("Details", new { id = MaHD });
            }

            var existing = db.ChiTietDichVus.FirstOrDefault(x => x.MaHD == MaHD && x.MaDV == MaDV);
            if (existing != null)
            {
                existing.SoLuong = (existing.SoLuong ?? 0) + SoLuong;
                existing.DonGia = service.DonGia;
            }
            else
            {
                db.ChiTietDichVus.Add(new ChiTietDichVu
                {
                    MaHD = MaHD,
                    MaDV = MaDV,
                    SoLuong = SoLuong,
                    DonGia = service.DonGia
                });
            }

            AuditLogService.Write(db, CurrentUserId, "Thêm dịch vụ", "Thêm " + SoLuong + " " + service.TenDV + " vào hóa đơn #" + MaHD + ".");
            db.SaveChanges();
            TempData["Success"] = "Thêm dịch vụ thành công.";
            return RedirectToAction("Details", new { id = MaHD });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CheckOut(int id)
        {
            var invoice = db.HoaDons
                .Include(x => x.ChiTietHoaDons).ThenInclude(y => y.Phong)
                .Include(x => x.ChiTietDichVus)
                .FirstOrDefault(x => x.MaHD == id);
            if (invoice == null) return HttpNotFound();
            if (invoice.DaThanhToan == true || invoice.TinhTrang == (int)InvoiceStatus.Paid)
            {
                TempData["Error"] = "Hóa đơn này đã được thanh toán.";
                return RedirectToAction("Details", new { id });
            }

            var now = DateTime.Now;
            var checkIn = invoice.NgayCheckIn ?? invoice.NgayLap ?? now;
            var days = Math.Max(1, (now.Date - checkIn.Date).Days);

            decimal roomTotal = 0;
            foreach (var detail in invoice.ChiTietHoaDons)
            {
                detail.SoNgayO = days;
                roomTotal += (detail.DonGiaThucTe ?? 0) * days;
                if (detail.Phong != null) detail.Phong.TrangThai = RoomStatus.Cleaning;
            }

            decimal serviceTotal = invoice.ChiTietDichVus.Sum(x => (x.DonGia ?? 0) * (x.SoLuong ?? 0));
            invoice.NgayCheckOut = now;
            invoice.TongTien = roomTotal + serviceTotal;
            invoice.DaThanhToan = true;
            invoice.TinhTrang = (int)InvoiceStatus.Paid;

            AuditLogService.Write(db, CurrentUserId, "Thanh toán hóa đơn", "Thanh toán hóa đơn #" + invoice.MaHD + " với tổng tiền " + invoice.TongTien?.ToString("N0") + " đ.");
            db.SaveChanges();
            TempData["Success"] = "Thanh toán và trả phòng thành công.";
            return RedirectToAction("Details", new { id });
        }

        private HoaDon LoadInvoice(int id)
        {
            return db.HoaDons
                .Include(x => x.KhachHang)
                .Include(x => x.NhanVien)
                .Include(x => x.TinhTrangHoaDon)
                .Include(x => x.ChiTietHoaDons).ThenInclude(y => y.Phong.LoaiPhong)
                .Include(x => x.ChiTietDichVus).ThenInclude(y => y.DichVu)
                .FirstOrDefault(x => x.MaHD == id);
        }

        private void LoadCheckInLists(int? selectedCustomer, int? selectedRoom)
        {
            ViewBag.MaKH = new SelectList(db.KhachHangs.OrderBy(x => x.TenKH).ToList(), "MaKH", "TenKH", selectedCustomer);
            var availableRooms = db.Phongs.Include(x => x.LoaiPhong)
                .Where(x => x.TrangThai == RoomStatus.Available || (selectedRoom.HasValue && x.MaPhong == selectedRoom.Value))
                .OrderBy(x => x.SoPhong)
                .ToList();
            ViewBag.MaPhong = new SelectList(availableRooms.Select(x => new
            {
                x.MaPhong,
                TenHienThi = "Phòng " + x.SoPhong + " (" + (x.LoaiPhong == null ? "Không rõ loại" : x.LoaiPhong.TenLoai) + " - " + ((x.LoaiPhong == null ? 0 : x.LoaiPhong.GiaMacDinh) ?? 0).ToString("N0") + " đ/đêm)"
            }), "MaPhong", "TenHienThi", selectedRoom);

            if (selectedRoom.HasValue)
                ViewBag.SelectedRoom = db.Phongs.Include(x => x.LoaiPhong).FirstOrDefault(x => x.MaPhong == selectedRoom.Value);
        }

        private int CurrentUserId { get { return (int)Session[SessionKeys.UserId]; } }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
