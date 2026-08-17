using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLKS.Data;
using QLKS.Infrastructure;
using QLKS.Models;

namespace QLKS.Controllers
{
    [RoleAuthorize(RoleNames.Admin)]
    public class AdminController : BaseController
    {
        private readonly QLKSEntities db = new QLKSEntities();

        public ActionResult BaoCao(int? thang, int? nam)
        {
            var selectedMonth = thang ?? DateTime.Now.Month;
            var selectedYear = nam ?? DateTime.Now.Year;
            if (selectedMonth < 1 || selectedMonth > 12)
            {
                ModelState.AddModelError("thang", "Tháng phải từ 1 đến 12.");
                selectedMonth = DateTime.Now.Month;
            }
            if (selectedYear < 2000 || selectedYear > DateTime.Now.Year + 1)
            {
                ModelState.AddModelError("nam", "Năm báo cáo không hợp lệ.");
                selectedYear = DateTime.Now.Year;
            }

            ViewBag.Thang = selectedMonth;
            ViewBag.Nam = selectedYear;
            ViewBag.HotelName = AppConfig.Get("HotelName", "Khách sạn");
            ViewBag.HotelAddress = AppConfig.Get("HotelAddress", string.Empty);

            var data = db.HoaDons
                .Where(x => x.DaThanhToan == true &&
                            x.TinhTrang == (int)InvoiceStatus.Paid &&
                            x.NgayCheckOut.HasValue &&
                            x.NgayCheckOut.Value.Month == selectedMonth &&
                            x.NgayCheckOut.Value.Year == selectedYear)
                .GroupBy(x => x.NgayCheckOut.Value.Day)
                .Select(x => new RevenueReportRowViewModel
                {
                    Ngay = x.Key,
                    SoLuongDon = x.Count(),
                    DoanhThu = x.Sum(y => y.TongTien) ?? 0
                })
                .OrderBy(x => x.Ngay)
                .ToList();

            return View(data);
        }

        public ActionResult Log(string nhanVien, string hanhDong, DateTime? tuNgay, DateTime? denNgay, int page = 1)
        {
            const int pageSize = 20;
            page = Math.Max(1, page);
            var query = db.NhatKyHoatDongs
                .Include(x => x.NhanVien)
                .AsQueryable();
            var normalizedEmployee = string.IsNullOrWhiteSpace(nhanVien) ? null : nhanVien.Trim();
            var normalizedAction = string.IsNullOrWhiteSpace(hanhDong) ? null : hanhDong.Trim();
            if (normalizedEmployee != null) query = query.Where(x => x.NhanVien.TenNV.Contains(normalizedEmployee) || x.NhanVien.TenDangNhap.Contains(normalizedEmployee));
            if (normalizedAction != null) query = query.Where(x => x.HanhDong == normalizedAction);
            if (tuNgay.HasValue) query = query.Where(x => x.ThoiGian.HasValue && x.ThoiGian.Value.Date >= tuNgay.Value.Date);
            if (denNgay.HasValue) query = query.Where(x => x.ThoiGian.HasValue && x.ThoiGian.Value.Date <= denNgay.Value.Date);
            if (tuNgay.HasValue && denNgay.HasValue && tuNgay.Value.Date > denNgay.Value.Date)
                ModelState.AddModelError(string.Empty, "Ngày bắt đầu không được sau ngày kết thúc.");

            var totalItems = query.Count();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);
            if (totalPages > 0 && page > totalPages) page = totalPages;
            var logs = query.OrderByDescending(x => x.ThoiGian).Skip((page - 1) * pageSize).Take(pageSize).ToList();
            return View(new AuditLogIndexViewModel
            {
                Employee = normalizedEmployee,
                ActionType = normalizedAction,
                FromDate = tuNgay,
                ToDate = denNgay,
                ActionTypes = db.NhatKyHoatDongs.Where(x => x.HanhDong != null).Select(x => x.HanhDong).Distinct().OrderBy(x => x).ToList(),
                Results = new PagedResultViewModel<NhatKyHoatDong>
                {
                    Items = logs,
                    CurrentPage = page,
                    PageSize = pageSize,
                    TotalItems = totalItems
                }
            });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
