using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLKS.Data;
using QLKS.Infrastructure;
using QLKS.Models;
using QLKS.Services;

namespace QLKS.Controllers
{
    [RoleAuthorize(RoleNames.Admin, RoleNames.Housekeeping)]
    public class TapVuController : BaseController
    {
        private readonly QLKSEntities db = new QLKSEntities();

        public ActionResult Index(int? tang, string trangThai)
        {
            var query = db.Phongs.Include(x => x.LoaiPhong)
                .Where(x => x.TrangThai == RoomStatus.Cleaning || x.TrangThai == RoomStatus.Maintenance)
                .AsQueryable();
            if (tang.HasValue) query = query.Where(x => x.Tang == tang.Value);
            if (trangThai == RoomStatus.Cleaning || trangThai == RoomStatus.Maintenance)
                query = query.Where(x => x.TrangThai == trangThai);

            var rooms = query
                .OrderBy(x => x.SoPhong)
                .ToList();
            return View(new HousekeepingIndexViewModel
            {
                Rooms = rooms,
                Floor = tang,
                Status = trangThai,
                Floors = db.Phongs.Where(x => x.Tang.HasValue).Select(x => x.Tang.Value).Distinct().OrderBy(x => x).ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult HoanTat(int id)
        {
            var room = db.Phongs.Find(id);
            if (room == null) return HttpNotFound();
            if (room.TrangThai != RoomStatus.Cleaning)
            {
                TempData["Error"] = "Chỉ có thể hoàn tất phòng đang ở trạng thái Đang dọn.";
                return RedirectToAction("Index");
            }

            var hasActiveInvoice = db.ChiTietHoaDons.Any(x => x.MaPhong == id && x.HoaDon.DaThanhToan != true &&
                (x.HoaDon.TinhTrang == (int)InvoiceStatus.Reserved || x.HoaDon.TinhTrang == (int)InvoiceStatus.CheckedIn));
            if (hasActiveInvoice)
            {
                TempData["Error"] = "Không thể hoàn tất dọn phòng khi vẫn còn hóa đơn hoạt động.";
                return RedirectToAction("Index");
            }

            room.TrangThai = RoomStatus.Available;
            AuditLogService.Write(db, CurrentUserId, "Hoàn tất dọn phòng", "Phòng " + room.SoPhong + " đã được chuyển về Trống.");
            db.SaveChanges();
            TempData["Success"] = "Đã hoàn tất dọn phòng.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult BaoTri(int id, string lyDo)
        {
            var room = db.Phongs.Find(id);
            if (room == null) return HttpNotFound();
            if (string.IsNullOrWhiteSpace(lyDo) || lyDo.Trim().Length > 500)
            {
                TempData["Error"] = "Vui lòng nhập lý do bảo trì (tối đa 500 ký tự).";
                return RedirectToAction("Index");
            }

            var hasActiveInvoice = db.ChiTietHoaDons.Any(x => x.MaPhong == id && x.HoaDon.DaThanhToan != true &&
                (x.HoaDon.TinhTrang == (int)InvoiceStatus.Reserved || x.HoaDon.TinhTrang == (int)InvoiceStatus.CheckedIn));
            if (room.TrangThai == RoomStatus.Occupied || hasActiveInvoice)
            {
                TempData["Error"] = "Không thể chuyển phòng đang có khách sang Bảo trì.";
                return RedirectToAction("Index");
            }

            room.TrangThai = RoomStatus.Maintenance;
            AuditLogService.Write(db, CurrentUserId, "Báo bảo trì", "Phòng " + room.SoPhong + ": " + lyDo.Trim());
            db.SaveChanges();
            TempData["Success"] = "Đã ghi nhận yêu cầu bảo trì.";
            return RedirectToAction("Index");
        }

        private int CurrentUserId { get { return (int)Session[SessionKeys.UserId]; } }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
