using System;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLKS.Data;
using QLKS.Infrastructure;
using QLKS.Models;
using QLKS.Services;

namespace QLKS.Controllers
{
    public class AccountController : BaseController
    {
        private readonly QLKSEntities db = new QLKSEntities();

        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            if (Session[SessionKeys.UserId] is int) return RedirectToAction("Index", "Home");
            ViewBag.ReturnUrl = returnUrl;
            return View(new LoginViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }

            var userName = model.UserName.Trim();
            var employee = db.NhanViens.Include(x => x.VaiTroInfo)
                .FirstOrDefault(x => x.TenDangNhap == userName);

            var passwordValid = employee != null &&
                (PasswordHasher.Verify(model.Password, employee.MatKhau) ||
                 (!PasswordHasher.IsHash(employee.MatKhau) && string.Equals(employee.MatKhau, model.Password, StringComparison.Ordinal)));

            if (!passwordValid)
            {
                AuditLogService.Write(db, employee == null ? (int?)null : employee.MaNV, "Đăng nhập thất bại", "Tên đăng nhập không tồn tại hoặc mật khẩu không đúng.");
                db.SaveChanges();
                ModelState.AddModelError(string.Empty, "Tên đăng nhập hoặc mật khẩu không đúng.");
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }

            if (employee.TrangThai != true)
            {
                AuditLogService.Write(db, employee.MaNV, "Đăng nhập thất bại", "Tài khoản đang bị khóa.");
                db.SaveChanges();
                ModelState.AddModelError(string.Empty, "Tài khoản đã bị khóa.");
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }

            var roleName = RoleNameNormalizer.Normalize(employee.VaiTroInfo == null ? null : employee.VaiTroInfo.TenVaiTro);
            if (string.IsNullOrWhiteSpace(roleName))
            {
                ModelState.AddModelError(string.Empty, "Tài khoản chưa được gán vai trò hợp lệ.");
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }

            if (!PasswordHasher.IsHash(employee.MatKhau)) employee.MatKhau = PasswordHasher.Hash(model.Password);

            Session.Clear();
            Session[SessionKeys.UserId] = employee.MaNV;
            Session[SessionKeys.DisplayName] = employee.TenNV;
            Session[SessionKeys.RoleId] = employee.VaiTro;
            Session[SessionKeys.RoleName] = roleName;

            AuditLogService.Write(db, employee.MaNV, "Đăng nhập thành công", "Nhân viên đã đăng nhập vào hệ thống.");
            db.SaveChanges();

            if (Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            if (roleName == RoleNames.Housekeeping) return RedirectToAction("Index", "TapVu");
            return RedirectToAction("Index", "Home");
        }

        [SessionAuthorize]
        public ActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        [HttpPost]
        [SessionAuthorize]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var userId = (int)Session[SessionKeys.UserId];
            var employee = db.NhanViens.Find(userId);
            if (employee == null || employee.TrangThai != true)
            {
                Session.Clear();
                return RedirectToAction("Login");
            }

            var currentPasswordValid = PasswordHasher.Verify(model.MatKhauHienTai, employee.MatKhau) ||
                (!PasswordHasher.IsHash(employee.MatKhau) && string.Equals(employee.MatKhau, model.MatKhauHienTai, StringComparison.Ordinal));
            if (!currentPasswordValid)
            {
                ModelState.AddModelError("MatKhauHienTai", "Mật khẩu hiện tại không đúng.");
                return View(model);
            }

            employee.MatKhau = PasswordHasher.Hash(model.MatKhauMoi);
            AuditLogService.Write(db, userId, "Đổi mật khẩu", "Người dùng đã đổi mật khẩu.");
            db.SaveChanges();
            TempData["Success"] = "Đổi mật khẩu thành công.";
            return RedirectToAction("Index", "Home");
        }

        [HttpPost]
        [SessionAuthorize]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            var userId = (int?)Session[SessionKeys.UserId];
            AuditLogService.Write(db, userId, "Đăng xuất", "Người dùng đã đăng xuất khỏi hệ thống.");
            db.SaveChanges();
            Session.Clear();
            Session.Abandon();
            return RedirectToAction("Login");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
