using System;
using System.Linq;
using System.Net;
using Microsoft.EntityFrameworkCore;
using QLKS.Data;
using QLKS.Infrastructure;
using QLKS.Models;
using QLKS.Services;

namespace QLKS.Controllers
{
    [RoleAuthorize(RoleNames.Admin, RoleNames.Receptionist)]
    public class KhachHangController : BaseController
    {
        private readonly QLKSEntities db = new QLKSEntities();

        public ActionResult Index(string ten, string cccd, string dienThoai, int page = 1)
        {
            const int pageSize = 15;
            page = Math.Max(1, page);
            var query = db.KhachHangs.AsQueryable();
            var normalizedName = NormalizeOptional(ten);
            var normalizedIdentity = NormalizeOptional(cccd);
            var normalizedPhone = NormalizeOptional(dienThoai);
            if (normalizedName != null) query = query.Where(x => x.TenKH.Contains(normalizedName));
            if (normalizedIdentity != null) query = query.Where(x => x.CCCD.Contains(normalizedIdentity));
            if (normalizedPhone != null) query = query.Where(x => x.DienThoai.Contains(normalizedPhone));

            var totalItems = query.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages > 0 && page > totalPages) page = totalPages;
            var customers = query.OrderBy(x => x.TenKH).Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return View(new CustomerIndexViewModel
            {
                Name = normalizedName,
                IdentityNumber = normalizedIdentity,
                Phone = normalizedPhone,
                Results = new PagedResultViewModel<KhachHang>
                {
                    Items = customers,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalItems = totalItems
                }
            });
        }

        public ActionResult Details(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var customer = db.KhachHangs.Find(id.Value);
            if (customer == null) return HttpNotFound();
            return View(customer);
        }

        public ActionResult Create()
        {
            return View(new CustomerCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(CustomerCreateViewModel model)
        {
            ValidateCustomer(model.CCCD, model.NamSinh, null);
            if (!ModelState.IsValid) return View(model);

            var customer = new KhachHang
            {
                TenKH = model.TenKH.Trim(),
                CCCD = NormalizeOptional(model.CCCD),
                GioiTinh = NormalizeOptional(model.GioiTinh),
                NamSinh = model.NamSinh,
                DienThoai = NormalizeOptional(model.DienThoai),
                Email = NormalizeOptional(model.Email),
                DiaChi = NormalizeOptional(model.DiaChi),
                MatKhau = null
            };

            try
            {
                db.KhachHangs.Add(customer);
                AuditLogService.Write(db, CurrentUserId, "Thêm khách hàng", "Tạo hồ sơ khách hàng " + customer.TenKH + ".");
                db.SaveChanges();
                TempData["Success"] = "Thêm khách hàng thành công.";
                return RedirectToAction("Index");
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Không thể thêm khách hàng. CCCD có thể đã tồn tại.");
                return View(model);
            }
        }

        public ActionResult Edit(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var customer = db.KhachHangs.Find(id.Value);
            if (customer == null) return HttpNotFound();
            return View(new CustomerEditViewModel
            {
                MaKH = customer.MaKH,
                TenKH = customer.TenKH,
                CCCD = customer.CCCD,
                GioiTinh = customer.GioiTinh,
                NamSinh = customer.NamSinh,
                DienThoai = customer.DienThoai,
                Email = customer.Email,
                DiaChi = customer.DiaChi
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(CustomerEditViewModel model)
        {
            var customer = db.KhachHangs.Find(model.MaKH);
            if (customer == null) return HttpNotFound();

            ValidateCustomer(model.CCCD, model.NamSinh, model.MaKH);
            if (!ModelState.IsValid) return View(model);

            customer.TenKH = model.TenKH.Trim();
            customer.CCCD = NormalizeOptional(model.CCCD);
            customer.GioiTinh = NormalizeOptional(model.GioiTinh);
            customer.NamSinh = model.NamSinh;
            customer.DienThoai = NormalizeOptional(model.DienThoai);
            customer.Email = NormalizeOptional(model.Email);
            customer.DiaChi = NormalizeOptional(model.DiaChi);

            try
            {
                AuditLogService.Write(db, CurrentUserId, "Sửa khách hàng", "Cập nhật hồ sơ khách hàng " + customer.TenKH + ".");
                db.SaveChanges();
                TempData["Success"] = "Cập nhật khách hàng thành công.";
                return RedirectToAction("Index");
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Không thể cập nhật khách hàng. CCCD có thể đã tồn tại.");
                return View(model);
            }
        }

        public ActionResult Delete(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var customer = db.KhachHangs.Find(id.Value);
            if (customer == null) return HttpNotFound();
            return View(customer);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var customer = db.KhachHangs.Find(id);
            if (customer == null) return HttpNotFound();
            if (db.HoaDons.Any(x => x.MaKH == id))
            {
                TempData["Error"] = "Không thể xóa khách hàng đã có lịch sử hóa đơn.";
                return RedirectToAction("Index");
            }

            try
            {
                db.KhachHangs.Remove(customer);
                AuditLogService.Write(db, CurrentUserId, "Xóa khách hàng", "Xóa hồ sơ khách hàng " + customer.TenKH + " chưa có hóa đơn.");
                db.SaveChanges();
                TempData["Success"] = "Xóa khách hàng thành công.";
            }
            catch (DbUpdateException)
            {
                TempData["Error"] = "Không thể xóa khách hàng vì đang được dữ liệu khác tham chiếu.";
            }
            return RedirectToAction("Index");
        }

        private int CurrentUserId { get { return (int)Session[SessionKeys.UserId]; } }

        private void ValidateCustomer(string identityNumber, int? birthYear, int? customerId)
        {
            var normalized = NormalizeOptional(identityNumber);
            if (normalized != null && db.KhachHangs.Any(x => x.CCCD == normalized && (!customerId.HasValue || x.MaKH != customerId.Value)))
                ModelState.AddModelError("CCCD", "CCCD đã tồn tại.");
            if (birthYear.HasValue && birthYear.Value > DateTime.Today.Year)
                ModelState.AddModelError("NamSinh", "Năm sinh không được lớn hơn năm hiện tại.");
        }

        private static string NormalizeOptional(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
