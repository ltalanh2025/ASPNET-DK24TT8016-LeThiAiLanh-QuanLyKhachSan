using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using QLKS.Data;
using QLKS.Infrastructure;
using QLKS.Models;

namespace QLKS.Services
{
    public class OnlineBookingService
    {
        private readonly QLKSEntities db;
        private readonly RoomAvailabilityService availability;

        public OnlineBookingService(QLKSEntities db)
        {
            this.db = db ?? throw new ArgumentNullException(nameof(db));
            availability = new RoomAvailabilityService(db);
        }

        public ServiceResult<DatPhongOnline> CreateBooking(int customerId, OnlineBookingCreateViewModel model, DateTime now)
        {
            if (model == null || !model.MaPhong.HasValue || !model.NgayNhanPhong.HasValue || !model.NgayTraPhong.HasValue || !model.SoNguoi.HasValue)
                return ServiceResult<DatPhongOnline>.Failure("Thông tin đặt phòng chưa đầy đủ.");

            var checkIn = model.NgayNhanPhong.Value.Date;
            var checkOut = model.NgayTraPhong.Value.Date;
            if (checkIn < now.Date) return ServiceResult<DatPhongOnline>.Failure("Ngày nhận phòng không được nhỏ hơn hôm nay.");
            if (checkOut <= checkIn) return ServiceResult<DatPhongOnline>.Failure("Ngày trả phòng phải sau ngày nhận phòng.");
            if (model.SoNguoi.Value <= 0) return ServiceResult<DatPhongOnline>.Failure("Số người phải lớn hơn 0.");

            using (var transaction = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    if (!db.KhachHangs.Any(x => x.MaKH == customerId))
                        return Rollback<DatPhongOnline>(transaction, "Tài khoản khách hàng không tồn tại.");
                    if (!availability.LockRoom(model.MaPhong.Value))
                        return Rollback<DatPhongOnline>(transaction, "Phòng không tồn tại.");

                    var room = db.Phongs.Include(x => x.LoaiPhong).FirstOrDefault(x => x.MaPhong == model.MaPhong.Value);
                    if (room == null || room.LoaiPhong == null || !room.LoaiPhong.GiaMacDinh.HasValue)
                        return Rollback<DatPhongOnline>(transaction, "Phòng chưa có thông tin giá hợp lệ.");
                    if (!room.LoaiPhong.SoNguoiToiDa.HasValue || room.LoaiPhong.SoNguoiToiDa.Value < model.SoNguoi.Value)
                        return Rollback<DatPhongOnline>(transaction, "Phòng không đủ sức chứa cho số người đã chọn.");
                    if (room.TrangThai == RoomStatus.Maintenance)
                        return Rollback<DatPhongOnline>(transaction, "Phòng đang bảo trì và chưa thể đặt online.");
                    if (!availability.IsRoomAvailable(room.MaPhong, checkIn, checkOut, now, null))
                        return Rollback<DatPhongOnline>(transaction, "Phòng vừa được khách khác đặt. Vui lòng chọn phòng khác.");

                    var nights = (checkOut - checkIn).Days;
                    var price = RoundMoney(room.LoaiPhong.GiaMacDinh.Value);
                    var total = RoundMoney(price * nights);
                    var booking = new DatPhongOnline
                    {
                        MaKH = customerId,
                        MaPhong = room.MaPhong,
                        NgayDat = now,
                        NgayNhanPhong = checkIn,
                        NgayTraPhong = checkOut,
                        SoNguoi = model.SoNguoi.Value,
                        DonGiaTaiThoiDiemDat = price,
                        SoDem = nights,
                        TongTienDuKien = total,
                        TienCoc = 0,
                        TrangThai = OnlineBookingStatus.PendingConfirmation,
                        HanThanhToan = now,
                        GhiChu = Clean(model.GhiChu, 500)
                    };
                    db.DatPhongOnlines.Add(booking);
                    db.SaveChanges();
                    transaction.Commit();
                    return ServiceResult<DatPhongOnline>.Success(booking, "Đã gửi yêu cầu đặt phòng. Chờ nhân viên xác nhận.");
                }
                catch (DbUpdateException)
                {
                    transaction.Rollback();
                    return ServiceResult<DatPhongOnline>.Failure("Không thể đặt phòng do dữ liệu vừa thay đổi. Vui lòng tìm lại phòng.");
                }
                catch (DataException)
                {
                    transaction.Rollback();
                    return ServiceResult<DatPhongOnline>.Failure("Không thể tạo đơn đặt phòng. Vui lòng thử lại.");
                }
            }
        }

        public ServiceResult<DatPhongOnline> CancelByCustomer(int bookingId, int customerId, string reason, byte[] rowVersion, DateTime now)
        {
            using (var transaction = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    var booking = db.DatPhongOnlines.FirstOrDefault(x => x.MaDatPhong == bookingId && x.MaKH == customerId);
                    if (booking == null) return Rollback<DatPhongOnline>(transaction, "Không tìm thấy đơn đặt phòng của bạn.");
                    if (!ApplyRowVersion(booking, rowVersion)) return Rollback<DatPhongOnline>(transaction, "Đơn vừa được cập nhật. Vui lòng tải lại trang.");
                    if (!OnlineBookingPresentation.CanCustomerCancel(booking.TrangThai))
                        return Rollback<DatPhongOnline>(transaction, "Đơn ở trạng thái hiện tại không thể hủy.");
                    if (now >= booking.NgayNhanPhong.AddHours(-OnlineBookingPolicy.CancelDeadlineHours))
                        return Rollback<DatPhongOnline>(transaction, "Chỉ được hủy trước thời điểm nhận phòng ít nhất " + OnlineBookingPolicy.CancelDeadlineHours + " giờ. Vui lòng liên hệ lễ tân.");

                    booking.TrangThai = OnlineBookingStatus.Cancelled;
                    booking.NgayHuy = now;
                    booking.LyDoHuy = Clean(reason, 500);
                    db.SaveChanges();
                    transaction.Commit();
                    return ServiceResult<DatPhongOnline>.Success(booking, "Đã hủy đơn đặt phòng.");
                }
                catch (DbUpdateConcurrencyException)
                {
                    transaction.Rollback();
                    return ServiceResult<DatPhongOnline>.Failure("Đơn vừa được người khác cập nhật. Vui lòng tải lại.");
                }
                catch (DbUpdateException)
                {
                    transaction.Rollback();
                    return ServiceResult<DatPhongOnline>.Failure("Không thể hủy đơn do dữ liệu vừa thay đổi.");
                }
            }
        }

        public ServiceResult<DatPhongOnline> ConfirmByEmployee(int bookingId, int employeeId, byte[] rowVersion, DateTime now)
        {
            using (var transaction = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    var booking = db.DatPhongOnlines.Include(x => x.Phong).FirstOrDefault(x => x.MaDatPhong == bookingId);
                    if (booking == null) return Rollback<DatPhongOnline>(transaction, "Không tìm thấy đơn đặt phòng.");
                    if (!ApplyRowVersion(booking, rowVersion)) return Rollback<DatPhongOnline>(transaction, "Đơn vừa được cập nhật. Vui lòng tải lại.");
                    if (booking.TrangThai != OnlineBookingStatus.PendingConfirmation)
                        return Rollback<DatPhongOnline>(transaction, "Chỉ có thể xác nhận đơn đang chờ xác nhận.");
                    if (!availability.LockRoom(booking.MaPhong) ||
                        !availability.IsRoomAvailable(booking.MaPhong, booking.NgayNhanPhong, booking.NgayTraPhong, now, booking.MaDatPhong))
                        return Rollback<DatPhongOnline>(transaction, "Phòng đang có lịch xung đột và chưa thể xác nhận.");

                    booking.TrangThai = OnlineBookingStatus.Confirmed;
                    booking.MaNVXacNhan = employeeId;
                    booking.NgayXacNhan = now;
                    AuditLogService.Write(db, employeeId, "Xác nhận đơn online", "Xác nhận đơn #" + booking.MaDatPhong + ", phòng " + booking.Phong.SoPhong + ".");
                    db.SaveChanges();
                    transaction.Commit();
                    return ServiceResult<DatPhongOnline>.Success(booking, "Đã xác nhận đơn đặt phòng online.");
                }
                catch (DbUpdateConcurrencyException)
                {
                    transaction.Rollback();
                    return ServiceResult<DatPhongOnline>.Failure("Đơn vừa được cập nhật. Vui lòng tải lại.");
                }
                catch (DbUpdateException)
                {
                    transaction.Rollback();
                    return ServiceResult<DatPhongOnline>.Failure("Không thể xác nhận đơn do dữ liệu vừa thay đổi.");
                }
            }
        }

        public ServiceResult<DatPhongOnline> RejectByEmployee(int bookingId, int employeeId, string reason, byte[] rowVersion, DateTime now)
        {
            using (var transaction = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    var booking = db.DatPhongOnlines.FirstOrDefault(x => x.MaDatPhong == bookingId);
                    if (booking == null) return Rollback<DatPhongOnline>(transaction, "Không tìm thấy đơn đặt phòng.");
                    if (!ApplyRowVersion(booking, rowVersion)) return Rollback<DatPhongOnline>(transaction, "Đơn vừa được cập nhật. Vui lòng tải lại.");
                    if (!OnlineBookingPresentation.CanCustomerCancel(booking.TrangThai))
                        return Rollback<DatPhongOnline>(transaction, "Đơn ở trạng thái hiện tại không thể hủy.");
                    if (string.IsNullOrWhiteSpace(reason)) return Rollback<DatPhongOnline>(transaction, "Vui lòng nhập lý do từ chối/hủy.");

                    booking.TrangThai = OnlineBookingStatus.Cancelled;
                    booking.NgayHuy = now;
                    booking.LyDoHuy = Clean(reason, 500);
                    AuditLogService.Write(db, employeeId, "Hủy đơn online", "Hủy đơn #" + booking.MaDatPhong + ". Lý do: " + Clean(reason, 300));
                    db.SaveChanges();
                    transaction.Commit();
                    return ServiceResult<DatPhongOnline>.Success(booking, "Đã hủy đơn đặt phòng.");
                }
                catch (DbUpdateConcurrencyException)
                {
                    transaction.Rollback();
                    return ServiceResult<DatPhongOnline>.Failure("Đơn vừa được cập nhật. Vui lòng tải lại.");
                }
                catch (DbUpdateException)
                {
                    transaction.Rollback();
                    return ServiceResult<DatPhongOnline>.Failure("Không thể hủy đơn do dữ liệu vừa thay đổi.");
                }
            }
        }

        public ServiceResult<DatPhongOnline> CheckInByEmployee(int bookingId, int employeeId, byte[] rowVersion, DateTime now)
        {
            using (var transaction = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    var booking = db.DatPhongOnlines
                        .Include(x => x.Phong.LoaiPhong)
                        .FirstOrDefault(x => x.MaDatPhong == bookingId);
                    if (booking == null) return Rollback<DatPhongOnline>(transaction, "Không tìm thấy đơn đặt phòng.");
                    if (booking.MaHoaDon.HasValue && booking.TrangThai == OnlineBookingStatus.CheckedIn)
                    {
                        transaction.Commit();
                        return ServiceResult<DatPhongOnline>.Success(booking, "Đơn đã được check-in trước đó.", true);
                    }
                    if (!ApplyRowVersion(booking, rowVersion)) return Rollback<DatPhongOnline>(transaction, "Đơn vừa được cập nhật. Vui lòng tải lại.");
                    if (booking.TrangThai != OnlineBookingStatus.Confirmed)
                        return Rollback<DatPhongOnline>(transaction, "Chỉ đơn đã xác nhận mới được check-in.");
                    if (now.Date < booking.NgayNhanPhong.Date)
                        return Rollback<DatPhongOnline>(transaction, "Chưa đến ngày nhận phòng của đơn.");
                    if (now.Date >= booking.NgayTraPhong.Date)
                        return Rollback<DatPhongOnline>(transaction, "Đơn đã quá ngày trả phòng và không thể check-in.");
                    if (!availability.LockRoom(booking.MaPhong)) return Rollback<DatPhongOnline>(transaction, "Phòng không tồn tại.");
                    if (booking.Phong == null || booking.Phong.TrangThai != RoomStatus.Available)
                        return Rollback<DatPhongOnline>(transaction, "Phòng chưa ở trạng thái Trống.");
                    if (!availability.IsRoomAvailable(booking.MaPhong, booking.NgayNhanPhong, booking.NgayTraPhong, now, booking.MaDatPhong))
                        return Rollback<DatPhongOnline>(transaction, "Phòng đang có lịch xung đột.");
                    if (db.ChiTietHoaDons.Any(x => x.MaPhong == booking.MaPhong && x.HoaDon.DaThanhToan != true && x.HoaDon.TinhTrang == (int)InvoiceStatus.CheckedIn))
                        return Rollback<DatPhongOnline>(transaction, "Phòng đang có hóa đơn hoạt động.");

                    var invoice = new HoaDon
                    {
                        MaKH = booking.MaKH,
                        MaNV = employeeId,
                        NgayLap = now,
                        NgayCheckIn = now,
                        DaThanhToan = false,
                        TinhTrang = (int)InvoiceStatus.CheckedIn,
                        TienCocDaNhan = 0,
                        GhiChu = Clean("Check-in từ đơn online #" + booking.MaDatPhong + ". " + booking.GhiChu, 255)
                    };
                    invoice.ChiTietHoaDons.Add(new ChiTietHoaDon
                    {
                        MaPhong = booking.MaPhong,
                        DonGiaThucTe = booking.DonGiaTaiThoiDiemDat,
                        SoNgayO = 0
                    });
                    booking.Phong.TrangThai = RoomStatus.Occupied;
                    db.HoaDons.Add(invoice);
                    db.SaveChanges();

                    booking.MaHoaDon = invoice.MaHD;
                    booking.TrangThai = OnlineBookingStatus.CheckedIn;
                    if (!booking.MaNVXacNhan.HasValue) booking.MaNVXacNhan = employeeId;
                    if (!booking.NgayXacNhan.HasValue) booking.NgayXacNhan = now;
                    AuditLogService.Write(db, employeeId, "Check-in đơn online", "Check-in đơn #" + booking.MaDatPhong + " thành hóa đơn #" + invoice.MaHD + ".");
                    db.SaveChanges();
                    transaction.Commit();
                    return ServiceResult<DatPhongOnline>.Success(booking, "Check-in từ đơn online thành công.");
                }
                catch (DbUpdateConcurrencyException)
                {
                    transaction.Rollback();
                    return ServiceResult<DatPhongOnline>.Failure("Đơn vừa được cập nhật. Vui lòng tải lại.");
                }
                catch (DbUpdateException)
                {
                    transaction.Rollback();
                    return ServiceResult<DatPhongOnline>.Failure("Không thể check-in do dữ liệu vừa thay đổi.");
                }
                catch (DataException)
                {
                    transaction.Rollback();
                    return ServiceResult<DatPhongOnline>.Failure("Không thể tạo hóa đơn từ đơn online.");
                }
            }
        }

        private bool ApplyRowVersion(DatPhongOnline booking, byte[] rowVersion)
        {
            if (rowVersion == null || booking.RowVersion == null || !booking.RowVersion.SequenceEqual(rowVersion)) return false;
            db.Entry(booking).OriginalValues["RowVersion"] = rowVersion;
            return true;
        }

        private static decimal RoundMoney(decimal value)
        {
            return decimal.Round(value, 2, MidpointRounding.AwayFromZero);
        }

        private static string Clean(string value, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;
            var cleaned = value.Trim();
            return cleaned.Length <= maxLength ? cleaned : cleaned.Substring(0, maxLength);
        }

        private static ServiceResult<T> Rollback<T>(Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction, string message)
        {
            transaction?.Rollback();
            return ServiceResult<T>.Failure(message);
        }
    }

    public static class OnlineBookingViewModelFactory
    {
        public static OnlineBookingListItemViewModel ToListItem(DatPhongOnline booking)
        {
            return new OnlineBookingListItemViewModel
            {
                MaDatPhong = booking.MaDatPhong,
                MaKH = booking.MaKH,
                CustomerName = booking.KhachHang == null ? string.Empty : booking.KhachHang.TenKH,
                CustomerEmail = booking.KhachHang == null ? string.Empty : booking.KhachHang.Email,
                CustomerPhone = booking.KhachHang == null ? string.Empty : booking.KhachHang.DienThoai,
                RoomNumber = booking.Phong == null ? string.Empty : booking.Phong.SoPhong,
                NgayDat = booking.NgayDat,
                NgayNhanPhong = booking.NgayNhanPhong,
                NgayTraPhong = booking.NgayTraPhong,
                SoDem = booking.SoDem,
                TongTienDuKien = booking.TongTienDuKien,
                TrangThai = booking.TrangThai,
                RowVersion = booking.RowVersion
            };
        }

        public static OnlineBookingDetailsViewModel ToDetails(DatPhongOnline booking)
        {
            var item = ToListItem(booking);
            return new OnlineBookingDetailsViewModel
            {
                MaDatPhong = item.MaDatPhong,
                MaKH = item.MaKH,
                CustomerName = item.CustomerName,
                CustomerEmail = item.CustomerEmail,
                CustomerPhone = item.CustomerPhone,
                RoomNumber = item.RoomNumber,
                NgayDat = item.NgayDat,
                NgayNhanPhong = item.NgayNhanPhong,
                NgayTraPhong = item.NgayTraPhong,
                SoDem = item.SoDem,
                TongTienDuKien = item.TongTienDuKien,
                TrangThai = item.TrangThai,
                RowVersion = item.RowVersion,
                SoNguoi = booking.SoNguoi,
                DonGiaTaiThoiDiemDat = booking.DonGiaTaiThoiDiemDat,
                NgayXacNhan = booking.NgayXacNhan,
                ConfirmedBy = booking.NhanVien == null ? null : booking.NhanVien.TenNV,
                NgayHuy = booking.NgayHuy,
                LyDoHuy = booking.LyDoHuy,
                GhiChu = booking.GhiChu,
                MaHoaDon = booking.MaHoaDon
            };
        }
    }
}