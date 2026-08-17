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
    public class CustomerAccountController : BaseController
    {
        private readonly QLKSEntities db = new QLKSEntities();

        [AllowAnonymous]
        public ActionResult Register(string returnUrl)
        {
            if (Session[CustomerSessionKeys.CustomerId] is int) return RedirectToAction("Profile");
            ViewBag.ReturnUrl = returnUrl;
            return View(new CustomerRegisterViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Register(CustomerRegisterViewModel model, string returnUrl)
        {
            if (!model.DongYDieuKhoan)
                ModelState.AddModelError("DongYDieuKhoan", "Bạn cần đồng ý với điều khoản sử dụng.");
            var email = NormalizeEmail(model.Email);
            if (email != null && db.KhachHangs.Any(x => x.Email == email))
                ModelState.AddModelError("Email", "Email này đã được sử dụng.");
            var identity = Clean(model.CCCD);
            if (identity != null && db.KhachHangs.Any(x => x.CCCD == identity))
                ModelState.AddModelError("CCCD", "CCCD này đã tồn tại trong hệ thống.");

            if (!ModelState.IsValid)
            {
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }

            var customer = new KhachHang
            {
                TenKH = model.TenKH.Trim(),
                Email = email,
                DienThoai = Clean(model.DienThoai),
                CCCD = identity,
                DiaChi = Clean(model.DiaChi),
                MatKhau = PasswordHasher.Hash(model.MatKhau)
            };
            db.KhachHangs.Add(customer);
            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Không thể tạo tài khoản. Email hoặc CCCD có thể vừa được đăng ký.");
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }

            SignIn(customer);
            TempData["Success"] = "Đăng ký tài khoản thành công.";
            if (Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction("Search", "OnlineBooking");
        }

        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            if (Session[CustomerSessionKeys.CustomerId] is int) return RedirectToAction("Search", "OnlineBooking");
            ViewBag.ReturnUrl = returnUrl;
            return View(new CustomerLoginViewModel());
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(CustomerLoginViewModel model, string returnUrl)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }

            var email = NormalizeEmail(model.Email);
            var customer = db.KhachHangs.FirstOrDefault(x => x.Email == email);
            if (customer == null || !PasswordHasher.IsHash(customer.MatKhau) || !PasswordHasher.Verify(model.MatKhau, customer.MatKhau))
            {
                ModelState.AddModelError(string.Empty, "Email hoặc mật khẩu không đúng.");
                ViewBag.ReturnUrl = returnUrl;
                return View(model);
            }

            SignIn(customer);
            TempData["Success"] = "Đăng nhập thành công.";
            if (Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);
            return RedirectToAction("Search", "OnlineBooking");
        }

        [HttpPost]
        [CustomerAuthorize]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            Session.Remove(CustomerSessionKeys.CustomerId);
            Session.Remove(CustomerSessionKeys.CustomerName);
            Session.Remove(CustomerSessionKeys.CustomerEmail);
            return RedirectToAction("Search", "OnlineBooking");
        }

        [CustomerAuthorize]
        public ActionResult Profile()
        {
            var customer = FindCurrentCustomer();
            if (customer == null) return ForceLogin();
            var model = new CustomerProfileViewModel
            {
                TenKH = customer.TenKH,
                Email = customer.Email,
                DienThoai = customer.DienThoai,
                CCCD = customer.CCCD,
                DiaChi = customer.DiaChi
            };
            ApplyProfileMetadata(model);
            return View(model);
        }

        [HttpPost]
        [CustomerAuthorize]
        [ValidateAntiForgeryToken]
        public ActionResult Profile(CustomerProfileViewModel model)
        {
            var customer = FindCurrentCustomer();
            if (customer == null) return ForceLogin();
            var email = NormalizeEmail(model.Email);
            var identity = Clean(model.CCCD);
            if (db.KhachHangs.Any(x => x.MaKH != customer.MaKH && x.Email == email))
                ModelState.AddModelError("Email", "Email này đã được sử dụng.");
            if (identity != null && db.KhachHangs.Any(x => x.MaKH != customer.MaKH && x.CCCD == identity))
                ModelState.AddModelError("CCCD", "CCCD này đã tồn tại.");
            if (!ModelState.IsValid)
            {
                ApplyProfileMetadata(model);
                return View(model);
            }

            customer.TenKH = model.TenKH.Trim();
            customer.Email = email;
            customer.DienThoai = Clean(model.DienThoai);
            customer.CCCD = identity;
            customer.DiaChi = Clean(model.DiaChi);
            try
            {
                db.SaveChanges();
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Không thể cập nhật vì email hoặc CCCD vừa được sử dụng.");
                ApplyProfileMetadata(model);
                return View(model);
            }

            SignIn(customer);
            TempData["Success"] = "Đã cập nhật thông tin cá nhân.";
            return RedirectToAction("Profile");
        }

        [CustomerAuthorize]
        public ActionResult ChangePassword()
        {
            return View(new CustomerChangePasswordViewModel());
        }

        [HttpPost]
        [CustomerAuthorize]
        [ValidateAntiForgeryToken]
        public ActionResult ChangePassword(CustomerChangePasswordViewModel model)
        {
            if (!ModelState.IsValid) return View(model);
            var customer = FindCurrentCustomer();
            if (customer == null) return ForceLogin();
            if (!PasswordHasher.IsHash(customer.MatKhau) || !PasswordHasher.Verify(model.MatKhauHienTai, customer.MatKhau))
            {
                ModelState.AddModelError("MatKhauHienTai", "Mật khẩu hiện tại không đúng.");
                return View(model);
            }

            customer.MatKhau = PasswordHasher.Hash(model.MatKhauMoi);
            db.SaveChanges();
            TempData["Success"] = "Đổi mật khẩu thành công.";
            return RedirectToAction("Profile");
        }

        private KhachHang FindCurrentCustomer()
        {
            var rawId = Session[CustomerSessionKeys.CustomerId];
            var id = rawId is int ? (int?)rawId : null;
            return id.HasValue ? db.KhachHangs.Find(id.Value) : null;
        }

        private void SignIn(KhachHang customer)
        {
            Session[CustomerSessionKeys.CustomerId] = customer.MaKH;
            Session[CustomerSessionKeys.CustomerName] = customer.TenKH;
            Session[CustomerSessionKeys.CustomerEmail] = customer.Email;
        }

        private ActionResult ForceLogin()
        {
            Session.Remove(CustomerSessionKeys.CustomerId);
            Session.Remove(CustomerSessionKeys.CustomerName);
            Session.Remove(CustomerSessionKeys.CustomerEmail);
            return RedirectToAction("Login");
        }

        private static string NormalizeEmail(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();
        }

        private static string Clean(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        }

        private static void ApplyProfileMetadata(CustomerProfileViewModel model)
        {
            if (model == null) return;
            model.NgayTaoTaiKhoan = null;
            model.TrangThaiTaiKhoan = "Đang sử dụng";
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
