using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLKS.Data;
using QLKS.Infrastructure;
using QLKS.Models;

namespace QLKS.Controllers
{
    [SessionAuthorize]
    public class HomeController : BaseController
    {
        private readonly QLKSEntities db = new QLKSEntities();

        public ActionResult Index()
        {
            var roleName = Session[SessionKeys.RoleName] as string;
            if (roleName == RoleNames.Housekeeping) return RedirectToAction("Index", "TapVu");

            var today = DateTime.Today;
            var canViewRevenue = roleName == RoleNames.Admin;
            var currentUserId = (int)Session[SessionKeys.UserId];
            var recentLogs = db.NhatKyHoatDongs
                .Include(x => x.NhanVien)
                .Where(x => roleName == RoleNames.Admin || x.MaNV == currentUserId)
                .OrderByDescending(x => x.ThoiGian)
                .Take(6)
                .Select(x => new RecentActivityViewModel
                {
                    Time = x.ThoiGian,
                    UserName = x.NhanVien == null ? "Hệ thống" : x.NhanVien.TenNV,
                    Action = x.HanhDong,
                    Description = x.GhiChu
                })
                .ToList();

            var model = new DashboardViewModel
            {
                TotalRooms = db.Phongs.Count(),
                AvailableRooms = db.Phongs.Count(x => x.TrangThai == RoomStatus.Available),
                OccupiedRooms = db.Phongs.Count(x => x.TrangThai == RoomStatus.Occupied),
                CleaningRooms = db.Phongs.Count(x => x.TrangThai == RoomStatus.Cleaning),
                MaintenanceRooms = db.Phongs.Count(x => x.TrangThai == RoomStatus.Maintenance),
                CurrentGuests = db.HoaDons.Count(x => x.DaThanhToan != true && x.TinhTrang == (int)InvoiceStatus.CheckedIn),
                UnpaidInvoices = db.HoaDons.Count(x => x.DaThanhToan != true && x.TinhTrang == (int)InvoiceStatus.CheckedIn),
                CheckInsToday = db.HoaDons.Count(x => x.NgayCheckIn.HasValue && x.NgayCheckIn.Value.Date == today),
                CanViewRevenue = canViewRevenue,
                RevenueToday = canViewRevenue
                ? db.HoaDons
                    .Where(x => x.DaThanhToan == true && x.TinhTrang == (int)InvoiceStatus.Paid && x.NgayCheckOut.HasValue && x.NgayCheckOut.Value.Date == today)
                    .Sum(x => (decimal?)x.TongTien) ?? 0
                : 0,
                RecentActivities = recentLogs
            };

            return View(model);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
