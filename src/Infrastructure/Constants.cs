namespace QLKS.Infrastructure
{
    public static class RoleNames
    {
        public const string Admin = "Admin";
        public const string Receptionist = "LeTan";
        public const string Housekeeping = "TapVu";
    }

    public static class SessionKeys
    {
        public const string UserId = "UserId";
        public const string DisplayName = "DisplayName";
        public const string RoleId = "RoleId";
        public const string RoleName = "RoleName";
    }

    public static class CustomerSessionKeys
    {
        public const string CustomerId = "CustomerId";
        public const string CustomerName = "CustomerName";
        public const string CustomerEmail = "CustomerEmail";
    }

    public static class RoomStatus
    {
        public const string Available = "Trống";
        public const string Occupied = "Đang ở";
        public const string Cleaning = "Đang dọn";
        public const string Maintenance = "Bảo trì";
    }

    public enum InvoiceStatus
    {
        Reserved = 1,
        CheckedIn = 2,
        Paid = 3
    }

    public static class OnlineBookingStatus
    {
        public const string PendingConfirmation = "ChoXacNhan";
        public const string Confirmed = "DaXacNhan";
        public const string Cancelled = "DaHuy";
        public const string CheckedIn = "DaCheckIn";

        public static readonly string[] All =
        {
            PendingConfirmation, Confirmed, Cancelled, CheckedIn
        };
    }

    public static class OnlineBookingPolicy
    {
        public const int CancelDeadlineHours = 12;
    }
}
