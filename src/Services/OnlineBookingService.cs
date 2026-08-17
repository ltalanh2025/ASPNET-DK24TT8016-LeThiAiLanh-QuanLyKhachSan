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
        private readonly PaymentService payments;

        public OnlineBookingService(QLKSEntities db, IPaymentGateway gateway = null)
        {
            this.db = db ?? throw new ArgumentNullException(nameof(db));
            availability = new RoomAvailabilityService(db);
            payments = new PaymentService(gateway ?? new MockPaymentGateway());
        }

        public int ExpirePendingBookings(DateTime now)
        {
            return db.Database.ExecuteSqlRaw(
                "UPDATE dbo.tblDatPhongOnline SET TrangThai = {0} " +
                "WHERE TrangThai = {1} AND HanThanhToan < {2}",
                OnlineBookingStatus.Expired,
                OnlineBookingStatus.PendingPayment,
                now);
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
                        return Rollback<DatPhongOnline>(transaction, "Phòng vừa được khách khác giữ chỗ. Vui lòng chọn phòng khác.");

                    var nights = (checkOut - checkIn).Days;
                    var price = RoundMoney(room.LoaiPhong.GiaMacDinh.Value);
                    var total = RoundMoney(price * nights);
                    var deposit = RoundMoney(total * OnlineBookingPolicy.DepositRate);
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
                        TienCoc = deposit,
                        TrangThai = OnlineBookingStatus.PendingPayment,
                        HanThanhToan = now.AddMinutes(OnlineBookingPolicy.PaymentWindowMinutes),
                        GhiChu = Clean(model.GhiChu, 500)
                    };
                    db.DatPhongOnlines.Add(booking);
                    db.SaveChanges();
                    transaction.Commit();
                    return ServiceResult<DatPhongOnline>.Success(booking, "Đã giữ phòng trong 15 phút. Vui lòng thanh toán tiền cọc.");
                }
                catch (DbUpdateException)
                {
                    transaction.Rollback();
                    return ServiceResult<DatPhongOnline>.Failure("Không thể giữ phòng do dữ liệu vừa thay đổi. Vui lòng tìm lại phòng.");
                }
                catch (DataException)
                {
                    transaction.Rollback();
                    return ServiceResult<DatPhongOnline>.Failure("Không thể tạo đơn đặt phòng. Vui lòng thử lại.");
                }
            }
        }

        public ServiceResult<DatPhongOnline> ProcessPayment(int bookingId, int customerId, bool simulateSuccess, DateTime now)
        {
            using (var transaction = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    var booking = db.DatPhongOnlines.Include(x => x.ThanhToanCocs)
                        .FirstOrDefault(x => x.MaDatPhong == bookingId && x.MaKH == customerId);
                    if (booking == null) return Rollback<DatPhongOnline>(transaction, "Không tìm thấy đơn đặt phòng của bạn.");
                    if (!availability.LockRoom(booking.MaPhong)) return Rollback<DatPhongOnline>(transaction, "Phòng không tồn tại.");

                    var successfulPayment = booking.ThanhToanCocs.FirstOrDefault(x => x.TrangThai == DepositPaymentStatus.Succeeded);
                    if (successfulPayment != null &&
                        (booking.TrangThai == OnlineBookingStatus.Deposited || booking.TrangThai == OnlineBookingStatus.Confirmed))
                    {
                        transaction.Commit();
                        return ServiceResult<DatPhongOnline>.Success(booking, "Tiền cọc của đơn này đã được ghi nhận trước đó.", true);
                    }

                    if (booking.TrangThai == OnlineBookingStatus.PendingPayment && booking.HanThanhToan < now)
                    {
                        booking.TrangThai = OnlineBookingStatus.Expired;
                        db.SaveChanges();
                        transaction.Commit();
                        return ServiceResult<DatPhongOnline>.Failure("Đơn đã hết thời hạn thanh toán 15 phút.");
                    }
                    if (booking.TrangThai != OnlineBookingStatus.PendingPayment)
                        return Rollback<DatPhongOnline>(transaction, "Đơn không ở trạng thái có thể thanh toán.");
                    if (!availability.IsRoomAvailable(booking.MaPhong, booking.NgayNhanPhong, booking.NgayTraPhong, now, booking.MaDatPhong))
                        return Rollback<DatPhongOnline>(transaction, "Phòng không còn trống trong khoảng ngày đã đặt.");

                    var expectedTotal = RoundMoney(booking.DonGiaTaiThoiDiemDat * booking.SoDem);
                    var expectedDeposit = RoundMoney(expectedTotal * OnlineBookingPolicy.DepositRate);
                    var payment = payments.CreateDepositTransaction(booking.MaDatPhong, expectedDeposit, simulateSuccess, now);

                    if (simulateSuccess)
                    {
                        db.ThanhToanCocs.Add(payment);
                        booking.TongTienDuKien = expectedTotal;
                        booking.TienCoc = expectedDeposit;
                        booking.TrangThai = OnlineBookingStatus.Deposited;
                        booking.NgayThanhToanCoc = now;
                        db.SaveChanges();
                        transaction.Commit();
                        return ServiceResult<DatPhongOnline>.Success(booking, "Thanh toán cọc 20% thành công.");
                    }

                    db.ThanhToanCocs.Add(payment);
                    db.SaveChanges();
                    transaction.Commit();
                    return ServiceResult<DatPhongOnline>.Failure("Thanh toán mô phỏng thất bại. Phòng vẫn được giữ đến khi hết hạn.");
                }
                catch (DbUpdateException)
                {
                    transaction.Rollback();
                    return ServiceResult<DatPhongOnline>.Failure("Giao dịch đã được xử lý hoặc dữ liệu vừa thay đổi. Vui lòng kiểm tra lại đơn.");
                }
                catch (DataException)
                {
                    transaction.Rollback();
                    return ServiceResult<DatPhongOnline>.Failure("Không thể xử lý giao dịch cọc. Vui lòng thử lại.");
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

                    booking.TrangThai = booking.TrangThai == OnlineBookingStatus.PendingPayment
                        ? OnlineBookingStatus.Cancelled
                        : OnlineBookingStatus.RefundPending;
                    booking.NgayHuy = now;
                    booking.LyDoHuy = Clean(reason, 500);
                    db.SaveChanges();
                    transaction.Commit();
                    return ServiceResult<DatPhongOnline>.Success(booking,
                        booking.TrangThai == OnlineBookingStatus.RefundPending
                            ? "Đã hủy đơn. Tiền cọc đang chờ nhân viên xử lý hoàn."
                            : "Đã hủy đơn đặt phòng.");
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
                    if (booking.TrangThai != OnlineBookingStatus.Deposited)
                        return Rollback<DatPhongOnline>(transaction, "Chỉ có thể xác nhận đơn đã đặt cọc.");
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

                    var paid = booking.TrangThai == OnlineBookingStatus.Deposited || booking.TrangThai == OnlineBookingStatus.Confirmed;
                    booking.TrangThai = paid ? OnlineBookingStatus.RefundPending : OnlineBookingStatus.Cancelled;
                    booking.NgayHuy = now;
                    booking.LyDoHuy = Clean(reason, 500);
                    AuditLogService.Write(db, employeeId, "Hủy đơn online", "Hủy đơn #" + booking.MaDatPhong + ". Lý do: " + Clean(reason, 300));
                    db.SaveChanges();
                    transaction.Commit();
                    return ServiceResult<DatPhongOnline>.Success(booking, paid ? "Đơn đã chuyển sang chờ hoàn cọc." : "Đã hủy đơn.");
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

        public ServiceResult<DatPhongOnline> RefundByEmployee(int bookingId, int employeeId, byte[] rowVersion, DateTime now)
        {
            using (var transaction = db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                try
                {
                    var booking = db.DatPhongOnlines.Include(x => x.ThanhToanCocs).FirstOrDefault(x => x.MaDatPhong == bookingId);
                    if (booking == null) return Rollback<DatPhongOnline>(transaction, "Không tìm thấy đơn đặt phòng.");
                    if (!ApplyRowVersion(booking, rowVersion)) return Rollback<DatPhongOnline>(transaction, "Đơn vừa được cập nhật. Vui lòng tải lại.");
                    if (booking.TrangThai != OnlineBookingStatus.RefundPending)
                        return Rollback<DatPhongOnline>(transaction, "Đơn không ở trạng thái chờ hoàn cọc.");
                    var payment = booking.ThanhToanCocs.FirstOrDefault(x => x.TrangThai == DepositPaymentStatus.Succeeded);
                    if (payment == null) return Rollback<DatPhongOnline>(transaction, "Không tìm thấy giao dịch cọc thành công để hoàn.");

                    payment.TrangThai = DepositPaymentStatus.Refunded;
                    payment.NoiDung = "Đã hoàn cọc mô phỏng lúc " + now.ToString("dd/MM/yyyy HH:mm") + ".";
                    booking.TrangThai = OnlineBookingStatus.Refunded;
                    AuditLogService.Write(db, employeeId, "Hoàn cọc đơn online", "Xử lý hoàn cọc đơn #" + booking.MaDatPhong + ", giao dịch " + payment.MaGiaoDich + ".");
                    db.SaveChanges();
                    transaction.Commit();
                    return ServiceResult<DatPhongOnline>.Success(booking, "Đã ghi nhận hoàn cọc mô phỏng.");
                }
                catch (DbUpdateConcurrencyException)
                {
                    transaction.Rollback();
                    return ServiceResult<DatPhongOnline>.Failure("Đơn vừa được cập nhật. Vui lòng tải lại.");
                }
                catch (DbUpdateException)
                {
                    transaction.Rollback();
                    return ServiceResult<DatPhongOnline>.Failure("Không thể xử lý hoàn cọc do dữ liệu vừa thay đổi.");
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
                    if (booking.TrangThai != OnlineBookingStatus.Deposited && booking.TrangThai != OnlineBookingStatus.Confirmed)
                        return Rollback<DatPhongOnline>(transaction, "Chỉ đơn đã đặt cọc hoặc đã xác nhận mới được check-in.");
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
                        TienCocDaNhan = booking.TienCoc,
                        GhiChu = Clean("Check-in từ đơn online #" + booking.MaDatPhong + ". Đã nhận cọc " + booking.TienCoc.ToString("N0") + " đ. " + booking.GhiChu, 255)
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
            var successful = booking.ThanhToanCocs == null ? null :
                booking.ThanhToanCocs.FirstOrDefault(x => x.TrangThai == DepositPaymentStatus.Succeeded || x.TrangThai == DepositPaymentStatus.Refunded);
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
                TienCoc = booking.TienCoc,
                TrangThai = booking.TrangThai,
                HanThanhToan = booking.HanThanhToan,
                TransactionCode = successful == null ? null : successful.MaGiaoDich,
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
                TienCoc = item.TienCoc,
                TrangThai = item.TrangThai,
                HanThanhToan = item.HanThanhToan,
                TransactionCode = item.TransactionCode,
                RowVersion = item.RowVersion,
                SoNguoi = booking.SoNguoi,
                DonGiaTaiThoiDiemDat = booking.DonGiaTaiThoiDiemDat,
                NgayThanhToanCoc = booking.NgayThanhToanCoc,
                NgayXacNhan = booking.NgayXacNhan,
                ConfirmedBy = booking.NhanVien == null ? null : booking.NhanVien.TenNV,
                NgayHuy = booking.NgayHuy,
                LyDoHuy = booking.LyDoHuy,
                GhiChu = booking.GhiChu,
                MaHoaDon = booking.MaHoaDon,
                Payments = booking.ThanhToanCocs == null
                    ? new List<DepositPaymentViewModel>()
                    : booking.ThanhToanCocs.OrderByDescending(x => x.ThoiGianTao).Select(x => new DepositPaymentViewModel
                    {
                        TransactionCode = x.MaGiaoDich,
                        Amount = x.SoTien,
                        Method = x.PhuongThuc,
                        Status = x.TrangThai,
                        CreatedAt = x.ThoiGianTao,
                        PaidAt = x.ThoiGianThanhToan
                    }).ToList()
            };
        }
    }
}
