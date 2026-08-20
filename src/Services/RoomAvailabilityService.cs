using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using QLKS.Data;
using QLKS.Infrastructure;

namespace QLKS.Services
{
    public class RoomAvailabilityService
    {
        private readonly QLKSEntities db;

        public RoomAvailabilityService(QLKSEntities db)
        {
            this.db = db ?? throw new ArgumentNullException(nameof(db));
        }

        public IList<Phong> FindAvailableRooms(DateTime checkIn, DateTime checkOut, int guests, DateTime now)
        {
            var from = checkIn.Date;
            var to = checkOut.Date;
            var candidateIds = db.Phongs
                .Where(x => x.LoaiPhong.SoNguoiToiDa >= guests &&
                            x.LoaiPhong.GiaMacDinh >= 0 &&
                            x.TrangThai != RoomStatus.Maintenance)
                .Select(x => x.MaPhong)
                .ToList();

            var unavailableIds = candidateIds.Where(id => !IsRoomAvailable(id, from, to, now, null)).ToList();
            return db.Phongs.Include(x => x.LoaiPhong)
                .Where(x => candidateIds.Contains(x.MaPhong) && !unavailableIds.Contains(x.MaPhong))
                .OrderBy(x => x.Tang).ThenBy(x => x.SoPhong).ToList();
        }

        public bool IsRoomAvailable(int roomId, DateTime checkIn, DateTime checkOut, DateTime now, int? excludedBookingId)
        {
            var from = checkIn.Date;
            var to = checkOut.Date;
            if (to <= from) return false;

            var hasBookingConflict = db.DatPhongOnlines.Any(x =>
                x.MaPhong == roomId &&
                (!excludedBookingId.HasValue || x.MaDatPhong != excludedBookingId.Value) &&
                x.NgayNhanPhong < to && x.NgayTraPhong > from &&
                (x.TrangThai == OnlineBookingStatus.PendingConfirmation ||
                 x.TrangThai == OnlineBookingStatus.Confirmed));

            if (hasBookingConflict) return false;

            return !db.ChiTietHoaDons.Any(x =>
                x.MaPhong == roomId &&
                x.HoaDon.DaThanhToan != true &&
                (x.HoaDon.TinhTrang == (int)InvoiceStatus.Reserved || x.HoaDon.TinhTrang == (int)InvoiceStatus.CheckedIn) &&
                (!x.HoaDon.NgayCheckIn.HasValue || x.HoaDon.NgayCheckIn.Value.Date < to) &&
                (!x.HoaDon.NgayCheckOut.HasValue || x.HoaDon.NgayCheckOut.Value.Date > from));
        }

        public bool LockRoom(int roomId)
        {
            var lockedId = db.Database.SqlQueryRaw<int>(
                "SELECT MaPhong AS Value FROM dbo.tblPhong WITH (UPDLOCK, HOLDLOCK) WHERE MaPhong = {0}", roomId)
                .SingleOrDefault();
            return lockedId == roomId && lockedId != default;
        }
    }
}
