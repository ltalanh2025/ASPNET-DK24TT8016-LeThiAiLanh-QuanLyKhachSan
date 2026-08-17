using System;
using QLKS.Infrastructure;

namespace QLKS.Services
{
    public static class OnlineBookingPresentation
    {
        public static string Label(string status)
        {
            switch (status)
            {
                case OnlineBookingStatus.PendingPayment: return "Chờ thanh toán";
                case OnlineBookingStatus.Deposited: return "Đã đặt cọc";
                case OnlineBookingStatus.Confirmed: return "Đã xác nhận";
                case OnlineBookingStatus.Cancelled: return "Đã hủy";
                case OnlineBookingStatus.Expired: return "Hết hạn";
                case OnlineBookingStatus.CheckedIn: return "Đã check-in";
                case OnlineBookingStatus.RefundPending: return "Chờ hoàn cọc";
                case OnlineBookingStatus.Refunded: return "Đã hoàn cọc";
                default: return string.IsNullOrWhiteSpace(status) ? "Không xác định" : status;
            }
        }

        public static string CssClass(string status)
        {
            switch (status)
            {
                case OnlineBookingStatus.PendingPayment: return "status-warning";
                case OnlineBookingStatus.Deposited: return "status-info";
                case OnlineBookingStatus.Confirmed: return "status-success";
                case OnlineBookingStatus.CheckedIn: return "status-primary";
                case OnlineBookingStatus.RefundPending: return "status-warning";
                case OnlineBookingStatus.Refunded: return "status-neutral";
                case OnlineBookingStatus.Cancelled:
                case OnlineBookingStatus.Expired: return "status-danger";
                default: return "status-neutral";
            }
        }

        public static string Icon(string status)
        {
            switch (status)
            {
                case OnlineBookingStatus.PendingPayment: return "⌛";
                case OnlineBookingStatus.Deposited: return "₫";
                case OnlineBookingStatus.Confirmed: return "✓";
                case OnlineBookingStatus.CheckedIn: return "●";
                case OnlineBookingStatus.RefundPending: return "↻";
                case OnlineBookingStatus.Refunded: return "↩";
                case OnlineBookingStatus.Cancelled: return "×";
                case OnlineBookingStatus.Expired: return "!";
                default: return "•";
            }
        }

        public static bool CanCustomerCancel(string status)
        {
            return status == OnlineBookingStatus.PendingPayment ||
                   status == OnlineBookingStatus.Deposited ||
                   status == OnlineBookingStatus.Confirmed;
        }

        public static bool CanPay(string status, DateTime deadline, DateTime now)
        {
            return status == OnlineBookingStatus.PendingPayment && deadline >= now;
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
