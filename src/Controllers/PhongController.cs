using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QLKS.Data;
using QLKS.Infrastructure;
using QLKS.Models;
using QLKS.Services;

namespace QLKS.Controllers
{
    [SessionAuthorize]
    public class PhongController : BaseController
    {
        private readonly QLKSEntities db = new QLKSEntities();

        public ActionResult Index(string q, int? tang, int? maLoai, string trangThai)
        {
            var query = db.Phongs.Include(x => x.LoaiPhong).AsQueryable();
            var normalizedSearch = string.IsNullOrWhiteSpace(q) ? null : q.Trim();
            if (normalizedSearch != null) query = query.Where(x => x.SoPhong.Contains(normalizedSearch));
            if (tang.HasValue) query = query.Where(x => x.Tang == tang.Value);
            if (maLoai.HasValue) query = query.Where(x => x.MaLoai == maLoai.Value);
            if (!string.IsNullOrWhiteSpace(trangThai)) query = query.Where(x => x.TrangThai == trangThai);

            var rooms = query.ToList()
                .OrderBy(x => (x.SoPhong ?? string.Empty).Length)
                .ThenBy(x => x.SoPhong)
                .ToList();

            var activeStays = db.ChiTietHoaDons
                .Where(x => x.MaPhong.HasValue && x.MaHD.HasValue && x.HoaDon.DaThanhToan != true &&
                            (x.HoaDon.TinhTrang == (int)InvoiceStatus.Reserved || x.HoaDon.TinhTrang == (int)InvoiceStatus.CheckedIn))
                .Select(x => new
                {
                    RoomId = x.MaPhong.Value,
                    InvoiceId = x.MaHD.Value,
                    CustomerName = x.HoaDon.KhachHang.TenKH,
                    CheckIn = x.HoaDon.NgayCheckIn
                })
                .ToList()
                .GroupBy(x => x.RoomId)
                .ToDictionary(x => x.Key, x => new ActiveRoomStayViewModel
                {
                    InvoiceId = x.First().InvoiceId,
                    CustomerName = x.First().CustomerName,
                    CheckIn = x.First().CheckIn
                });

            var model = new RoomIndexViewModel
            {
                Rooms = rooms,
                ActiveStays = activeStays,
                Search = normalizedSearch,
                Floor = tang,
                RoomTypeId = maLoai,
                Status = trangThai,
                Floors = db.Phongs.Where(x => x.Tang.HasValue).Select(x => x.Tang.Value).Distinct().OrderBy(x => x).ToList(),
                RoomTypes = db.LoaiPhongs.OrderBy(x => x.TenLoai).ToList()
            };
            return View(model);
        }

        [RoleAuthorize(RoleNames.Admin)]
        public ActionResult Create()
        {
            LoadRoomTypes(null);
            return View(new RoomCreateViewModel());
        }

        [HttpPost]
        [RoleAuthorize(RoleNames.Admin)]
        [ValidateAntiForgeryToken]
        public ActionResult Create(RoomCreateViewModel model, IFormFile HinhAnh)
        {
            ValidateRoom(model.SoPhong, model.MaLoai, null);
            if (!ModelState.IsValid)
            {
                LoadRoomTypes(model.MaLoai);
                return View(model);
            }

            var imageName = ImageUploadService.Save(HinhAnh, Server.MapPath("~/Content/Images/"), ModelState, "HinhAnh");
            if (!ModelState.IsValid)
            {
                LoadRoomTypes(model.MaLoai);
                return View(model);
            }

            var room = new Phong
            {
                SoPhong = model.SoPhong.Trim(),
                Tang = model.Tang,
                MaLoai = model.MaLoai,
                MoTaChiTiet = NormalizeOptional(model.MoTaChiTiet),
                AnhDaiDien = imageName ?? ImageUploadService.DefaultImage,
                TrangThai = RoomStatus.Available
            };

            try
            {
                db.Phongs.Add(room);
                AuditLogService.Write(db, CurrentUserId, "Thêm phòng", "Tạo phòng " + room.SoPhong + ".");
                db.SaveChanges();
                TempData["Success"] = "Thêm phòng thành công.";
                return RedirectToAction("Index");
            }
            catch (DbUpdateException)
            {
                DeleteNewImage(imageName);
                ModelState.AddModelError(string.Empty, "Không thể thêm phòng. Số phòng có thể đã tồn tại.");
                LoadRoomTypes(model.MaLoai);
                return View(model);
            }
        }

        [RoleAuthorize(RoleNames.Admin)]
        public ActionResult Edit(int id)
        {
            var room = db.Phongs.Find(id);
            if (room == null) return HttpNotFound();
            var model = new RoomEditViewModel
            {
                MaPhong = room.MaPhong,
                SoPhong = room.SoPhong,
                Tang = room.Tang,
                MaLoai = room.MaLoai,
                MoTaChiTiet = room.MoTaChiTiet,
                AnhHienTai = room.AnhDaiDien
            };
            LoadRoomTypes(model.MaLoai);
            return View(model);
        }

        [HttpPost]
        [RoleAuthorize(RoleNames.Admin)]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(RoomEditViewModel model, IFormFile HinhAnh)
        {
            var room = db.Phongs.Find(model.MaPhong);
            if (room == null) return HttpNotFound();

            ValidateRoom(model.SoPhong, model.MaLoai, model.MaPhong);
            model.AnhHienTai = room.AnhDaiDien;
            if (!ModelState.IsValid)
            {
                LoadRoomTypes(model.MaLoai);
                return View(model);
            }

            var newImage = ImageUploadService.Save(HinhAnh, Server.MapPath("~/Content/Images/"), ModelState, "HinhAnh");
            if (!ModelState.IsValid)
            {
                LoadRoomTypes(model.MaLoai);
                return View(model);
            }

            var oldImage = room.AnhDaiDien;
            room.SoPhong = model.SoPhong.Trim();
            room.Tang = model.Tang;
            room.MaLoai = model.MaLoai;
            room.MoTaChiTiet = NormalizeOptional(model.MoTaChiTiet);
            if (!string.IsNullOrEmpty(newImage)) room.AnhDaiDien = newImage;

            try
            {
                AuditLogService.Write(db, CurrentUserId, "Sửa phòng", "Cập nhật thông tin phòng " + room.SoPhong + ".");
                db.SaveChanges();
                if (!string.IsNullOrEmpty(newImage)) DeleteOldImage(oldImage);
                TempData["Success"] = "Cập nhật phòng thành công.";
                return RedirectToAction("Index");
            }
            catch (DbUpdateException)
            {
                DeleteNewImage(newImage);
                ModelState.AddModelError(string.Empty, "Không thể cập nhật phòng. Số phòng có thể đã tồn tại.");
                LoadRoomTypes(model.MaLoai);
                return View(model);
            }
        }

        [RoleAuthorize(RoleNames.Admin)]
        public ActionResult Delete(int id)
        {
            var room = db.Phongs.Include(x => x.LoaiPhong).FirstOrDefault(x => x.MaPhong == id);
            if (room == null) return HttpNotFound();
            return View(room);
        }

        [HttpPost, ActionName("Delete")]
        [RoleAuthorize(RoleNames.Admin)]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var room = db.Phongs.Find(id);
            if (room == null) return HttpNotFound();
            if (db.ChiTietHoaDons.Any(x => x.MaPhong == id))
            {
                TempData["Error"] = "Không thể xóa phòng đã có lịch sử hóa đơn.";
                return RedirectToAction("Index");
            }

            var imageName = room.AnhDaiDien;
            try
            {
                db.Phongs.Remove(room);
                AuditLogService.Write(db, CurrentUserId, "Xóa phòng", "Xóa phòng " + room.SoPhong + " chưa có hóa đơn.");
                db.SaveChanges();
                DeleteOldImage(imageName);
                TempData["Success"] = "Xóa phòng thành công.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Không thể xóa phòng vì đang có ràng buộc dữ liệu.";
            }
            return RedirectToAction("Index");
        }

        private void ValidateRoom(string roomNumber, int? roomTypeId, int? roomId)
        {
            if (string.IsNullOrWhiteSpace(roomNumber)) return;
            var normalized = roomNumber.Trim();
            if (db.Phongs.Any(x => x.SoPhong == normalized && (!roomId.HasValue || x.MaPhong != roomId.Value)))
                ModelState.AddModelError("SoPhong", "Số phòng đã tồn tại.");
            if (roomTypeId.HasValue && !db.LoaiPhongs.Any(x => x.MaLoai == roomTypeId.Value))
                ModelState.AddModelError("MaLoai", "Loại phòng không tồn tại.");
        }

        private void LoadRoomTypes(int? selectedType)
        {
            ViewBag.MaLoai = new SelectList(db.LoaiPhongs.OrderBy(x => x.TenLoai).ToList(), "MaLoai", "TenLoai", selectedType);
        }

        private void DeleteOldImage(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName) || fileName.Equals(ImageUploadService.DefaultImage, StringComparison.OrdinalIgnoreCase)) return;
            try
            {
                var path = Path.Combine(Server.MapPath("~/Content/Images/"), fileName);
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }
            catch { }
        }

        private void DeleteNewImage(string fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName)) return;
            try
            {
                var path = Path.Combine(Server.MapPath("~/Content/Images/"), fileName);
                if (System.IO.File.Exists(path)) System.IO.File.Delete(path);
            }
            catch { }
        }

        private static string NormalizeOptional(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private int CurrentUserId { get { return (int)Session[SessionKeys.UserId]; } }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
