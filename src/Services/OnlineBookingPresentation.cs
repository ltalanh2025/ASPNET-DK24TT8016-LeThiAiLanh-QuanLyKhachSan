using System;
using QLKS.Data;
using QLKS.Infrastructure;

namespace QLKS.Services
{
    public static class OnlineBookingPresentation
    {
        public static string Label(string status)
        {
            switch (status)
            {
                case OnlineBookingStatus.PendingConfirmation: return "Chờ xác nhận";
                case OnlineBookingStatus.Confirmed: return "Đã xác nhận";
                case OnlineBookingStatus.Cancelled: return "Đã hủy";
                case OnlineBookingStatus.CheckedIn: return "Đã check-in";
                default: return string.IsNullOrWhiteSpace(status) ? "Không xác định" : status;
            }
        }

        public static string CssClass(string status)
        {
            switch (status)
            {
                case OnlineBookingStatus.PendingConfirmation: return "status-warning";
                case OnlineBookingStatus.Confirmed: return "status-success";
                case OnlineBookingStatus.CheckedIn: return "status-primary";
                case OnlineBookingStatus.Cancelled: return "status-danger";
                default: return "status-neutral";
            }
        }

        public static string Icon(string status)
        {
            switch (status)
            {
                case OnlineBookingStatus.PendingConfirmation: return "⌛";
                case OnlineBookingStatus.Confirmed: return "✓";
                case OnlineBookingStatus.CheckedIn: return "●";
                case OnlineBookingStatus.Cancelled: return "×";
                default: return "•";
            }
        }

        public static bool CanCustomerCancel(string status)
        {
            return status == OnlineBookingStatus.PendingConfirmation ||
                   status == OnlineBookingStatus.Confirmed;
        }

        public static bool CanCustomerCancelWithTime(DatPhongOnline booking, DateTime now)
        {
            return CanCustomerCancelWithTime(booking.TrangThai, booking.NgayNhanPhong, now);
        }

        public static bool CanCustomerCancelWithTime(string status, DateTime ngayNhanPhong, DateTime now)
        {
            return CanCustomerCancel(status) &&
                   now < ngayNhanPhong.AddHours(-OnlineBookingPolicy.CancelDeadlineHours);
        }
    }

    public class ServiceResult<T>
    {
        public bool Succeeded { get; private set; }
        public bool IsIdempotent { get; private set; }
        public string Message { get; private set; }
        public T Value { get; private set; }

        public static ServiceResult<T> Success(T value, string message, bool idempotent = false)
        {
            return new ServiceResult<T> { Succeeded = true, Value = value, Message = message, IsIdempotent = idempotent };
        }

        public static ServiceResult<T> Failure(string message)
        {
            return new ServiceResult<T> { Succeeded = false, Message = message };
        }
    }
}