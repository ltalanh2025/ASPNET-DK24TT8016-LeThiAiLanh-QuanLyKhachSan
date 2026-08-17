using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLKS.Data;
using QLKS.Infrastructure;
using QLKS.Models;
using QLKS.Services;

namespace QLKS.Controllers
{
    public class OnlineBookingController : BaseController
    {
        private readonly QLKSEntities db = new QLKSEntities();
        private readonly OnlineBookingService bookingService;
        private readonly RoomAvailabilityService availability;

        public OnlineBookingController()
        {
            bookingService = new OnlineBookingService(db);
            availability = new RoomAvailabilityService(db);
        }

        [AllowAnonymous]
        public ActionResult Index()
        {
            return RedirectToAction("Index", "CustomerHome");
        }

        [AllowAnonymous]
        public ActionResult Search()
        {
            return View(new RoomSearchViewModel
            {
                NgayNhanPhong = DateTime.Today.AddDays(1),
                NgayTraPhong = DateTime.Today.AddDays(2),
                SoNguoi = 1
            });
        }

        [AllowAnonymous]
        public ActionResult AvailableRooms(RoomSearchViewModel model)
        {
            bookingService.ExpirePendingBookings(DateTime.Now);
            if (!TryValidateModel(model)) return View("Search", model);

            var checkIn = model.NgayNhanPhong.Value.Date;
            var checkOut = model.NgayTraPhong.Value.Date;
            var nights = (checkOut - checkIn).Days;
            var rooms = availability.FindAvailableRooms(checkIn, checkOut, model.SoNguoi.Value, DateTime.Now)
                .Select(x => ToAvailableRoom(x, nights)).ToList();
            return View(new AvailableRoomsPageViewModel { Search = model, Rooms = rooms });
        }

        [AllowAnonymous]
        public ActionResult RoomDetails(int maPhong, DateTime ngayNhanPhong, DateTime ngayTraPhong, int soNguoi)
        {
            bookingService.ExpirePendingBookings(DateTime.Now);
            var search = new RoomSearchViewModel
            {
                NgayNhanPhong = ngayNhanPhong.Date,
                NgayTraPhong = ngayTraPhong.Date,
                SoNguoi = soNguoi
            };
            if (!TryValidateModel(search))
            {
                TempData["Error"] = "Khoảng ngày hoặc số người không hợp lệ.";
                return RedirectToAction("Search");
            }

            var nights = (ngayTraPhong.Date - ngayNhanPhong.Date).Days;
            var room = availability.FindAvailableRooms(ngayNhanPhong.Date, ngayTraPhong.Date, soNguoi, DateTime.Now)
                .FirstOrDefault(x => x.MaPhong == maPhong);
            if (room == null)
            {
                TempData["Error"] = "Phòng không còn trống trong khoảng ngày đã chọn.";
                return RedirectToAction("AvailableRooms", search);
            }

            return View(new RoomDetailsPageViewModel
            {
                Search = search,
                Room = ToAvailableRoom(room, nights)
            });
        }

        [CustomerAuthorize]
        public ActionResult Create(int maPhong, DateTime ngayNhanPhong, DateTime ngayTraPhong, int soNguoi)
        {
            bookingService.ExpirePendingBookings(DateTime.Now);
            var search = new RoomSearchViewModel
            {
                NgayNhanPhong = ngayNhanPhong.Date,
                NgayTraPhong = ngayTraPhong.Date,
                SoNguoi = soNguoi
            };
            if (!TryValidateModel(search))
            {
                TempData["Error"] = "Khoảng ngày hoặc số người không hợp lệ.";
                return RedirectToAction("Search");
            }

            var room = availability.FindAvailableRooms(ngayNhanPhong.Date, ngayTraPhong.Date, soNguoi, DateTime.Now)
                .FirstOrDefault(x => x.MaPhong == maPhong);
            if (room == null)
            {
                TempData["Error"] = "Phòng không còn trống trong khoảng ngày đã chọn.";
                return RedirectToAction("AvailableRooms", search);
            }

            return View(new OnlineBookingCreateViewModel
            {
                MaPhong = maPhong,
                NgayNhanPhong = ngayNhanPhong.Date,
                NgayTraPhong = ngayTraPhong.Date,
                SoNguoi = soNguoi,
                Room = ToAvailableRoom(room, (ngayTraPhong.Date - ngayNhanPhong.Date).Days),
                Customer = LoadCurrentCustomerSummary()
            });
        }

        [HttpPost]
        [CustomerAuthorize]
        [ValidateAntiForgeryToken]
        public ActionResult Create(OnlineBookingCreateViewModel model)
        {
            if (!model.XacNhanChinhSach)
                ModelState.AddModelError("XacNhanChinhSach", "Bạn cần xác nhận đã đọc chính sách đặt và hủy phòng.");
            if (model.NgayNhanPhong.HasValue && model.NgayNhanPhong.Value.Date < DateTime.Today)
                ModelState.AddModelError("NgayNhanPhong", "Ngày nhận phòng không được nhỏ hơn hôm nay.");
            if (model.NgayNhanPhong.HasValue && model.NgayTraPhong.HasValue &&
                model.NgayTraPhong.Value.Date <= model.NgayNhanPhong.Value.Date)
                ModelState.AddModelError("NgayTraPhong", "Ngày trả phòng phải sau ngày nhận phòng.");

            if (!ModelState.IsValid)
            {
                LoadBookingPreview(model);
                return View(model);
            }

            var customerId = (int)Session[CustomerSessionKeys.CustomerId];
            var result = bookingService.CreateBooking(customerId, model, DateTime.Now);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                LoadBookingPreview(model);
                return View(model);
            }

            TempData["Success"] = result.Message;
            return RedirectToAction("Payment", new { id = result.Value.MaDatPhong });
        }

        [CustomerAuthorize]
        public ActionResult Payment(int id)
        {
            bookingService.ExpirePendingBookings(DateTime.Now);
            var booking = LoadCustomerBooking(id);
            if (booking == null) return HttpNotFound();

            var details = OnlineBookingViewModelFactory.ToDetails(booking);
            var model = new OnlineBookingPaymentViewModel
            {
                MaDatPhong = details.MaDatPhong,
                MaKH = details.MaKH,
                CustomerName = details.CustomerName,
                CustomerEmail = details.CustomerEmail,
                CustomerPhone = details.CustomerPhone,
                RoomNumber = details.RoomNumber,
                NgayDat = details.NgayDat,
                NgayNhanPhong = details.NgayNhanPhong,
                NgayTraPhong = details.NgayTraPhong,
                SoDem = details.SoDem,
                TongTienDuKien = details.TongTienDuKien,
                TienCoc = details.TienCoc,
                TrangThai = details.TrangThai,
                HanThanhToan = details.HanThanhToan,
                TransactionCode = details.TransactionCode,
                RowVersion = details.RowVersion,
                SoNguoi = details.SoNguoi,
                DonGiaTaiThoiDiemDat = details.DonGiaTaiThoiDiemDat,
                NgayThanhToanCoc = details.NgayThanhToanCoc,
                NgayXacNhan = details.NgayXacNhan,
                ConfirmedBy = details.ConfirmedBy,
                NgayHuy = details.NgayHuy,
                LyDoHuy = details.LyDoHuy,
                GhiChu = details.GhiChu,
                MaHoaDon = details.MaHoaDon,
                Payments = details.Payments,
                DepositRatePercent = OnlineBookingPolicy.DepositRate * 100m,
                CanPay = OnlineBookingPresentation.CanPay(booking.TrangThai, booking.HanThanhToan, DateTime.Now)
            };
            return View(model);
        }

        [HttpPost]
        [CustomerAuthorize]
        [ValidateAntiForgeryToken]
        public ActionResult ProcessPayment(int id, bool simulateSuccess = true)
        {
            var customerId = (int)Session[CustomerSessionKeys.CustomerId];
            var result = bookingService.ProcessPayment(id, customerId, simulateSuccess, DateTime.Now);
            TempData[result.Succeeded ? "Success" : "Error"] = result.Message;
            return RedirectToAction("Details", new { id });
        }

        [CustomerAuthorize]
        public ActionResult Details(int id)
        {
            bookingService.ExpirePendingBookings(DateTime.Now);
            var booking = LoadCustomerBooking(id);
            if (booking == null) return HttpNotFound();
            return View(OnlineBookingViewModelFactory.ToDetails(booking));
        }

        [CustomerAuthorize]
        public ActionResult MyBookings()
        {
            bookingService.ExpirePendingBookings(DateTime.Now);
            var customerId = (int)Session[CustomerSessionKeys.CustomerId];
            var bookings = db.DatPhongOnlines
                .Include(x => x.KhachHang)
                .Include(x => x.Phong)
                .Include(x => x.ThanhToanCocs)
                .Where(x => x.MaKH == customerId)
                .OrderByDescending(x => x.NgayDat)
                .ToList()
                .Select(OnlineBookingViewModelFactory.ToListItem)
                .ToList();
            return View(bookings);
        }

        [CustomerAuthorize]
        public ActionResult Cancel(int id)
        {
            bookingService.ExpirePendingBookings(DateTime.Now);
            var booking = LoadCustomerBooking(id);
            if (booking == null) return HttpNotFound();
            if (!OnlineBookingPresentation.CanCustomerCancel(booking.TrangThai))
            {
                TempData["Error"] = "Đơn không ở trạng thái có thể hủy.";
                return RedirectToAction("Details", new { id });
            }

            return View(new OnlineBookingCancelViewModel
            {
                MaDatPhong = booking.MaDatPhong,
                RowVersion = Convert.ToBase64String(booking.RowVersion ?? Array.Empty<byte>()),
                Booking = OnlineBookingViewModelFactory.ToDetails(booking)
            });
        }

        [HttpPost]
        [CustomerAuthorize]
        [ValidateAntiForgeryToken]
        public ActionResult Cancel(OnlineBookingCancelViewModel model)
        {
            var customerId = (int)Session[CustomerSessionKeys.CustomerId];
            var booking = LoadCustomerBooking(model.MaDatPhong);
            if (booking == null) return HttpNotFound();

            byte[] rowVersionBytes = null;
            try
            {
                if (!string.IsNullOrWhiteSpace(model.RowVersion))
                    rowVersionBytes = Convert.FromBase64String(model.RowVersion.Trim());
            }
            catch (FormatException)
            {
                ModelState.AddModelError("RowVersion", "Phiên bản dữ liệu không hợp lệ.");
            }

            if (!ModelState.IsValid)
            {
                model.Booking = OnlineBookingViewModelFactory.ToDetails(booking);
                return View(model);
            }

            var result = bookingService.CancelByCustomer(model.MaDatPhong, customerId, model.LyDoHuy, rowVersionBytes, DateTime.Now);
            if (!result.Succeeded)
            {
                ModelState.AddModelError(string.Empty, result.Message);
                model.Booking = OnlineBookingViewModelFactory.ToDetails(booking);
                return View(model);
            }

            TempData["Success"] = result.Message;
            return RedirectToAction("Details", new { id = model.MaDatPhong });
        }

        private DatPhongOnline LoadCustomerBooking(int id)
        {
            var customerId = (int)Session[CustomerSessionKeys.CustomerId];
            return db.DatPhongOnlines
                .Include(x => x.KhachHang)
                .Include(x => x.Phong.LoaiPhong)
                .Include(x => x.NhanVien)
                .Include(x => x.ThanhToanCocs)
                .FirstOrDefault(x => x.MaDatPhong == id && x.MaKH == customerId);
        }

        private AvailableRoomViewModel ToAvailableRoom(Phong room, int nights)
        {
            var price = room.LoaiPhong?.GiaMacDinh ?? 0;
            var total = price * nights;
            var imageService = new RoomImageService(db);
            var gallery = imageService.GetGalleriesForRooms(new[] { room.MaPhong }, Url);
            var roomGallery = gallery.ContainsKey(room.MaPhong) ? gallery[room.MaPhong] : null;

            return new AvailableRoomViewModel
            {
                MaPhong = room.MaPhong,
                SoPhong = room.SoPhong,
                Tang = room.Tang ?? 0,
                TenLoai = room.LoaiPhong?.TenLoai,
                SoNguoiToiDa = room.LoaiPhong?.SoNguoiToiDa ?? 0,
                GiaMoiDem = price,
                MoTa = room.MoTaChiTiet ?? room.LoaiPhong?.MoTa,
                AnhDaiDien = room.AnhDaiDien,
                PrimaryImageUrl = roomGallery?.PrimaryImageUrl ?? RoomImageService.DefaultImagePath,
                ImageUrls = roomGallery?.ImageUrls ?? new List<string>(),
                ImageAltText = roomGallery?.ImageAltText ?? ("Ảnh phòng " + room.SoPhong),
                SoDem = nights,
                TongTienDuKien = total,
                TienCoc = decimal.Round(total * OnlineBookingPolicy.DepositRate, 2, MidpointRounding.AwayFromZero),
                ConTrongTrongKhoang = true
            };
        }

        private CustomerBookingSummaryViewModel LoadCurrentCustomerSummary()
        {
            var customerId = (int)Session[CustomerSessionKeys.CustomerId];
            var customer = db.KhachHangs.Find(customerId);
            return new CustomerBookingSummaryViewModel
            {
                TenKH = customer?.TenKH,
                Email = customer?.Email,
                DienThoai = customer?.DienThoai
            };
        }

        private void LoadBookingPreview(OnlineBookingCreateViewModel model)
        {
            if (model.MaPhong.HasValue && model.NgayNhanPhong.HasValue && model.NgayTraPhong.HasValue &&
                model.NgayTraPhong.Value > model.NgayNhanPhong.Value)
            {
                var room = db.Phongs.Include(x => x.LoaiPhong).FirstOrDefault(x => x.MaPhong == model.MaPhong.Value);
                if (room != null)
                {
                    var nights = (model.NgayTraPhong.Value.Date - model.NgayNhanPhong.Value.Date).Days;
                    model.Room = ToAvailableRoom(room, nights);
                }
            }
            model.Customer = LoadCurrentCustomerSummary();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
