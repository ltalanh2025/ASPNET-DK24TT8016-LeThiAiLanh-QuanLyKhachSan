using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLKS.Data;
using QLKS.Infrastructure;
using QLKS.Models;

namespace QLKS.Controllers
{
    public class CustomerHomeController : BaseController
    {
        private readonly QLKSEntities db = new QLKSEntities();

        [AllowAnonymous]
        public ActionResult Index()
        {
            var model = new CustomerHomeViewModel
            {
                Search = new RoomSearchViewModel
                {
                    NgayNhanPhong = DateTime.Today.AddDays(1),
                    NgayTraPhong = DateTime.Today.AddDays(2),
                    SoNguoi = 1
                },
                FeaturedRooms = db.Phongs
                    .AsNoTracking()
                    .Include(x => x.LoaiPhong)
                    .Where(x => x.LoaiPhong != null)
                    .OrderBy(x => x.MaPhong)
                    .Take(3)
                    .Select(x => new CustomerRoomPreviewViewModel
                    {
                        MaPhong = x.MaPhong,
                        SoPhong = x.SoPhong,
                        TenLoai = x.LoaiPhong.TenLoai,
                        SoNguoiToiDa = x.LoaiPhong.SoNguoiToiDa ?? 0,
                        GiaMoiDem = x.LoaiPhong.GiaMacDinh ?? 0,
                        MoTa = x.MoTaChiTiet ?? x.LoaiPhong.MoTa,
                        AnhDaiDien = x.AnhDaiDien
                    })
                    .ToList(),
                FeaturedServices = LoadServices(4)
            };

            return View(model);
        }

        [AllowAnonymous]
        public ActionResult Services()
        {
            return View(new CustomerServicesViewModel { Services = LoadServices(null) });
        }

        [AllowAnonymous]
        public ActionResult Amenities()
        {
            return RedirectToAction("Services");
        }

        [AllowAnonymous]
        public ActionResult Contact()
        {
            return View(BuildContactPage(new CustomerContactViewModel
            {
                TenNguoiGui = Convert.ToString(Session[CustomerSessionKeys.CustomerName]),
                Email = Convert.ToString(Session[CustomerSessionKeys.CustomerEmail])
            }));
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Contact([Bind(Prefix = "Form")] CustomerContactViewModel form)
        {
            if (!ModelState.IsValid)
                return View(BuildContactPage(form));

            TempData["Success"] = "Biểu mẫu liên hệ hợp lệ. Project hiện chưa có bảng liên hệ nên nội dung chỉ được kiểm tra trong chế độ demo và không được lưu.";
            return RedirectToAction("Contact");
        }

        private IList<CustomerServiceViewModel> LoadServices(int? take)
        {
            var query = db.DichVus.AsNoTracking().OrderBy(x => x.TenDV).AsQueryable();
            if (take.HasValue) query = query.Take(take.Value);
            var services = query.Select(x => new CustomerServiceViewModel
            {
                MaDV = x.MaDV,
                TenDV = x.TenDV,
                DonGia = x.DonGia ?? 0,
                MoTa = x.MoTa
            }).ToList();

            var illustrations = new[]
            {
                "~/Content/Customer/images/services/amenity-dining.jpg",
                "~/Content/Customer/images/services/amenity-spa.jpg",
                "~/Content/Customer/images/services/amenity-bar.jpg",
                "~/Content/Customer/images/services/amenity-gym.jpg"
            };
            for (var index = 0; index < services.Count; index++)
            {
                services[index].AnhMinhHoa = illustrations[index % illustrations.Length];
                services[index].TrangThaiHienThi = "Có trong danh mục";
            }
            return services;
        }

        private static CustomerContactPageViewModel BuildContactPage(CustomerContactViewModel form)
        {
            return new CustomerContactPageViewModel
            {
                Form = form ?? new CustomerContactViewModel(),
                HotelName = AppConfig.Get("HotelName", "Khách sạn"),
                Address = AppConfig.Get("HotelAddress"),
                Phone = AppConfig.Get("HotelPhone"),
                Email = AppConfig.Get("HotelEmail"),
                WorkingHours = AppConfig.Get("HotelWorkingHours")
            };
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
