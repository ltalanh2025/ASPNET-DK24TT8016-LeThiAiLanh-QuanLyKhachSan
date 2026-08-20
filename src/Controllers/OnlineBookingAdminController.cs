using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLKS.Data;
using QLKS.Infrastructure;
using QLKS.Models;
using QLKS.Services;

namespace QLKS.Controllers
{
    [RoleAuthorize(RoleNames.Admin, RoleNames.Receptionist)]
    public class OnlineBookingAdminController : BaseController
    {
        private readonly QLKSEntities db = new QLKSEntities();
        private readonly OnlineBookingService bookingService;

        public OnlineBookingAdminController()
        {
            bookingService = new OnlineBookingService(db);
        }

        public ActionResult Index(string q, string trangThai, int page = 1)
        {
            const int pageSize = 15;
            page = Math.Max(1, page);

            var query = db.DatPhongOnlines
                .Include(x => x.KhachHang)
                .Include(x => x.Phong)
                .AsQueryable();

            var search = Normalize(q);
            int bookingId;
            if (search != null)
            {
                var hasId = int.TryParse(search.TrimStart('#'), out bookingId);
                query = query.Where(x =>
                    (hasId && x.MaDatPhong == bookingId) ||
                    x.KhachHang.TenKH.Contains(search) ||
                    x.KhachHang.Email.Contains(search) ||
                    x.KhachHang.DienThoai.Contains(search) ||
                    x.Phong.SoPhong.Contains(search));
            }

            var status = OnlineBookingStatus.All.Contains(trangThai) ? trangThai : null;
            if (status != null) query = query.Where(x => x.TrangThai == status);

            var totalItems = query.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages > 0 && page > totalPages) page = totalPages;
            var items = query.OrderByDescending(x => x.NgayDat)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList()
                .Select(OnlineBookingViewModelFactory.ToListItem)
                .ToList();

            return View(new OnlineBookingFilterViewModel
            {
                Search = search,
                Status = status,
                Results = new PagedResultViewModel<OnlineBookingListItemViewModel>
                {
                    Items = items,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalItems = totalItems
                }
            });
        }

        public ActionResult Details(int id)
        {
            var booking = LoadBooking(id);
            return booking == null ? (ActionResult)HttpNotFound() : View(OnlineBookingViewModelFactory.ToDetails(booking));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Confirm(OnlineBookingAdminActionViewModel model)
        {
            return RunAction(model, (id, employeeId, version) =>
                bookingService.ConfirmByEmployee(id, employeeId, version, DateTime.Now));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Reject(OnlineBookingAdminActionViewModel model)
        {
            if (string.IsNullOrWhiteSpace(model.Reason))
            {
                TempData["Error"] = "Vui lòng nhập lý do từ chối/hủy đơn.";
                return RedirectToAction("Details", new { id = model.MaDatPhong });
            }

            return RunAction(model, (id, employeeId, version) =>
                bookingService.RejectByEmployee(id, employeeId, model.Reason, version, DateTime.Now));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CheckIn(OnlineBookingAdminActionViewModel model)
        {
            var parsed = ParseVersion(model.RowVersion);
            if (parsed == null)
            {
                TempData["Error"] = "Phiên bản đơn không hợp lệ. Vui lòng tải lại.";
                return RedirectToAction("Details", new { id = model.MaDatPhong });
            }

            var result = bookingService.CheckInByEmployee(model.MaDatPhong, CurrentEmployeeId, parsed, DateTime.Now);
            TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
            if (result.Succeeded && result.Value.MaHoaDon.HasValue)
                return RedirectToAction("Details", "HoaDon", new { id = result.Value.MaHoaDon.Value });
            return RedirectToAction("Details", new { id = model.MaDatPhong });
        }

        private ActionResult RunAction(
            OnlineBookingAdminActionViewModel model,
            Func<int, int, byte[], ServiceResult<DatPhongOnline>> action)
        {
            var parsed = ParseVersion(model.RowVersion);
            if (parsed == null)
            {
                TempData["Error"] = "Phiên bản đơn không hợp lệ. Vui lòng tải lại.";
                return RedirectToAction("Details", new { id = model.MaDatPhong });
            }

            var result = action(model.MaDatPhong, CurrentEmployeeId, parsed);
            TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
            return RedirectToAction("Details", new { id = model.MaDatPhong });
        }

        private DatPhongOnline LoadBooking(int id)
        {
            return db.DatPhongOnlines
                .Include(x => x.KhachHang)
                .Include(x => x.Phong.LoaiPhong)
                .Include(x => x.NhanVien)
                .FirstOrDefault(x => x.MaDatPhong == id);
        }

        private static byte[] ParseVersion(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            try { return Convert.FromBase64String(value.Trim()); }
            catch (FormatException) { return null; }
        }

        private static string Normalize(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private int CurrentEmployeeId { get { return (int)Session[SessionKeys.UserId]; } }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}