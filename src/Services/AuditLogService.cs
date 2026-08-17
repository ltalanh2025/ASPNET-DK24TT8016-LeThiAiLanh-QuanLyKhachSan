using System;
using QLKS.Data;

namespace QLKS.Services
{
    public static class AuditLogService
    {
        public static void Write(QLKSEntities db, int? userId, string action, string description)
        {
            if (db == null) throw new ArgumentNullException(nameof(db));
            db.NhatKyHoatDongs.Add(new NhatKyHoatDong
            {
                MaNV = userId,
                HanhDong = Truncate(action, 255),
                GhiChu = description,
                ThoiGian = DateTime.Now
            });
        }

        private static string Truncate(string value, int maximumLength)
        {
            if (string.IsNullOrEmpty(value) || value.Length <= maximumLength) return value;
            return value.Substring(0, maximumLength);
        }
    }
}
