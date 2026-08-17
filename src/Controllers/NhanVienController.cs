using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QLKS.Data;
using QLKS.Infrastructure;
using QLKS.Models;
using QLKS.Services;

namespace QLKS.Controllers
{
    [RoleAuthorize(RoleNames.Admin)]
    public class NhanVienController : BaseController
    {
        private readonly QLKSEntities db = new QLKSEntities();

        public ActionResult Index(string q, int? vaiTro, bool? trangThai, int page = 1)
        {
            const int pageSize = 15;
            page = Math.Max(1, page);
            var query = db.NhanViens.Include(x => x.VaiTroInfo).AsQueryable();
            var normalizedSearch = NormalizeOptional(q);
            if (normalizedSearch != null)
                query = query.Where(x => x.TenNV.Contains(normalizedSearch) || x.TenDangNhap.Contains(normalizedSearch) || x.SDT.Contains(normalizedSearch));
            if (vaiTro.HasValue) query = query.Where(x => x.VaiTro == vaiTro.Value);
            if (trangThai.HasValue) query = query.Where(x => x.TrangThai == trangThai.Value);

            var totalItems = query.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages > 0 && page > totalPages) page = totalPages;
            var employees = query.OrderBy(x => x.TenNV).Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return View(new EmployeeIndexViewModel
            {
                Search = normalizedSearch,
                RoleId = vaiTro,
                IsActive = trangThai,
                Roles = db.VaiTroes.OrderBy(x => x.TenVaiTro).ToList(),
                Results = new PagedResultViewModel<NhanVien>
                {
                    Items = employees,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalItems = totalItems
                }
            });
        }

        public ActionResult Create()
        {
            LoadRoles(null);
            return View(new EmployeeCreateViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create(EmployeeCreateViewModel model)
        {
            ValidateEmployee(model.TenDangNhap, model.NgaySinh, model.VaiTro, null);
            if (!ModelState.IsValid)
            {
                LoadRoles(model.VaiTro);
                return View(model);
            }

            var employee = new NhanVien
            {
                TenDangNhap = model.TenDangNhap.Trim(),
                MatKhau = PasswordHasher.Hash(model.MatKhau),
                TenNV = model.TenNV.Trim(),
                GioiTinh = NormalizeOptional(model.GioiTinh),
                NgaySinh = model.NgaySinh,
                SDT = NormalizeOptional(model.SDT),
                VaiTro = model.VaiTro,
                TrangThai = true
            };

            try
            {
                db.NhanViens.Add(employee);
                AuditLogService.Write(db, CurrentUserId, "Tạo nhân viên", "Tạo tài khoản " + employee.TenDangNhap + ".");
                db.SaveChanges();
                TempData["Success"] = "Tạo nhân viên thành công.";
                return RedirectToAction("Index");
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Không thể tạo nhân viên. Tên đăng nhập có thể đã tồn tại.");
                LoadRoles(model.VaiTro);
                return View(model);
            }
        }

        public ActionResult Edit(int id)
        {
            var employee = db.NhanViens.Find(id);
            if (employee == null) return HttpNotFound();
            var model = new EmployeeEditViewModel
            {
                MaNV = employee.MaNV,
                TenDangNhap = employee.TenDangNhap,
                TenNV = employee.TenNV,
                GioiTinh = employee.GioiTinh,
                NgaySinh = employee.NgaySinh,
                SDT = employee.SDT,
                VaiTro = employee.VaiTro,
                TrangThai = employee.TrangThai == true
            };
            LoadRoles(model.VaiTro);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit(EmployeeEditViewModel model)
        {
            var employee = db.NhanViens.Include(x => x.VaiTroInfo).FirstOrDefault(x => x.MaNV == model.MaNV);
            if (employee == null) return HttpNotFound();

            ValidateEmployee(model.TenDangNhap, model.NgaySinh, model.VaiTro, model.MaNV);
            ValidateAdminContinuity(employee, model.VaiTro, model.TrangThai);
            if (employee.MaNV == CurrentUserId && !model.TrangThai)
                ModelState.AddModelError("TrangThai", "Bạn không thể tự khóa tài khoản của mình.");

            if (!ModelState.IsValid)
            {
                LoadRoles(model.VaiTro);
                return View(model);
            }

            employee.TenDangNhap = model.TenDangNhap.Trim();
            employee.TenNV = model.TenNV.Trim();
            employee.GioiTinh = NormalizeOptional(model.GioiTinh);
            employee.NgaySinh = model.NgaySinh;
            employee.SDT = NormalizeOptional(model.SDT);
            employee.VaiTro = model.VaiTro;
            employee.TrangThai = model.TrangThai;

            try
            {
                AuditLogService.Write(db, CurrentUserId, "Sửa nhân viên", "Cập nhật tài khoản " + employee.TenDangNhap + ".");
                db.SaveChanges();
                TempData["Success"] = "Cập nhật nhân viên thành công.";
                return RedirectToAction("Index");
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Không thể cập nhật nhân viên. Tên đăng nhập có thể đã tồn tại.");
                LoadRoles(model.VaiTro);
                return View(model);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(int id, string matKhauMoi)
        {
            var employee = db.NhanViens.Find(id);
            if (employee == null) return HttpNotFound();
            if (string.IsNullOrWhiteSpace(matKhauMoi) || matKhauMoi.Length < 8 || matKhauMoi.Length > 100)
            {
                TempData["Error"] = "Mật khẩu mới phải từ 8 đến 100 ký tự.";
                return RedirectToAction("Index");
            }

            employee.MatKhau = PasswordHasher.Hash(matKhauMoi);
            AuditLogService.Write(db, CurrentUserId, "Reset mật khẩu", "Reset mật khẩu tài khoản " + employee.TenDangNhap + ".");
            db.SaveChanges();
            TempData["Success"] = "Reset mật khẩu thành công.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ToggleStatus(int id)
        {
            var employee = db.NhanViens.Include(x => x.VaiTroInfo).FirstOrDefault(x => x.MaNV == id);
            if (employee == null) return HttpNotFound();
            if (employee.MaNV == CurrentUserId)
            {
                TempData["Error"] = "Bạn không thể tự khóa tài khoản của chính mình.";
                return RedirectToAction("Index");
            }

            var nextStatus = employee.TrangThai != true;
            ValidateAdminContinuity(employee, employee.VaiTro, nextStatus);
            if (!ModelState.IsValid)
            {
                TempData["Error"] = ModelState[string.Empty]?.Errors.FirstOrDefault()?.ErrorMessage ?? "Không thể khóa tài khoản này.";
                return RedirectToAction("Index");
            }

            employee.TrangThai = nextStatus;
            AuditLogService.Write(db, CurrentUserId, "Đổi trạng thái tài khoản", (nextStatus ? "Mở khóa" : "Khóa") + " tài khoản " + employee.TenDangNhap + ".");
            db.SaveChanges();
            TempData["Success"] = (nextStatus ? "Mở khóa" : "Khóa") + " tài khoản thành công.";
            return RedirectToAction("Index");
        }

        private void ValidateEmployee(string userName, DateTime? birthDate, int? roleId, int? employeeId)
        {
            var normalizedUser = NormalizeOptional(userName);
            if (normalizedUser != null && db.NhanViens.Any(x => x.TenDangNhap == normalizedUser && (!employeeId.HasValue || x.MaNV != employeeId.Value)))
                ModelState.AddModelError("TenDangNhap", "Tên đăng nhập đã tồn tại.");
            if (birthDate.HasValue && birthDate.Value.Date > DateTime.Today)
                ModelState.AddModelError("NgaySinh", "Ngày sinh không được ở tương lai.");
            if (roleId.HasValue && !db.VaiTroes.Any(x => x.IDVaiTro == roleId.Value))
                ModelState.AddModelError("VaiTro", "Vai trò không hợp lệ.");
        }

        private void ValidateAdminContinuity(NhanVien target, int? newRoleId, bool newStatus)
        {
            var currentRole = RoleNameNormalizer.Normalize(target.VaiTroInfo == null ? null : target.VaiTroInfo.TenVaiTro);
            if (currentRole != RoleNames.Admin) return;

            var newRole = newRoleId.HasValue
                ? RoleNameNormalizer.Normalize(db.VaiTroes.Where(x => x.IDVaiTro == newRoleId.Value).Select(x => x.TenVaiTro).FirstOrDefault())
                : currentRole;

            var willRemainActiveAdmin = newRole == RoleNames.Admin && newStatus;
            if (willRemainActiveAdmin) return;

            var activeAdminCount = db.NhanViens.Count(x =>
                x.TrangThai == true &&
                x.MaNV != target.MaNV &&
                db.VaiTroes.Where(r => r.IDVaiTro == x.VaiTro).Select(r => r.TenVaiTro).FirstOrDefault() == "Admin");

            if (activeAdminCount < 1)
                ModelState.AddModelError(string.Empty, "Hệ thống phải có ít nhất một tài khoản Admin đang hoạt động.");
        }

        private void LoadRoles(int? selectedRole)
        {
            ViewBag.VaiTro = new SelectList(db.VaiTroes.OrderBy(x => x.TenVaiTro).ToList(), "IDVaiTro", "TenVaiTro", selectedRole);
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
