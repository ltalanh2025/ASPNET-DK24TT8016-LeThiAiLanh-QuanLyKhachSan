using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Routing;
using QLKS.Data;

namespace QLKS.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        [StringLength(50)]
        [Display(Name = "Tên đăng nhập")]
        public string UserName { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [StringLength(200)]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; }
    }

    public class CheckInViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn khách hàng.")]
        [Display(Name = "Khách hàng")]
        public int? MaKH { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phòng.")]
        [Display(Name = "Phòng")]
        public int? MaPhong { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập ngày check-in.")]
        [Display(Name = "Ngày check-in")]
        public DateTime? NgayCheckIn { get; set; }

        [StringLength(255)]
        [Display(Name = "Ghi chú")]
        public string GhiChu { get; set; }
    }

    public abstract class RoomViewModelBase
    {
        [Required(ErrorMessage = "Số phòng không được để trống.")]
        [StringLength(20)]
        [Display(Name = "Số phòng")]
        public string SoPhong { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tầng.")]
        [Range(1, 200, ErrorMessage = "Tầng phải từ 1 đến 200.")]
        [Display(Name = "Tầng")]
        public int? Tang { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn loại phòng.")]
        [Display(Name = "Loại phòng")]
        public int? MaLoai { get; set; }

        [StringLength(2000)]
        [Display(Name = "Mô tả chi tiết")]
        public string MoTaChiTiet { get; set; }
    }

    public class RoomCreateViewModel : RoomViewModelBase
    {
    }

    public class RoomEditViewModel : RoomViewModelBase
    {
        public int MaPhong { get; set; }
        public string AnhHienTai { get; set; }
    }

    public abstract class RoomImageInputViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "Vui lòng nhập đường dẫn ảnh.")]
        [StringLength(1000, ErrorMessage = "Đường dẫn ảnh không được vượt quá 1000 ký tự.")]
        [Display(Name = "Đường dẫn ảnh")]
        public string DuongDanAnh { get; set; }

        [StringLength(255, ErrorMessage = "Mô tả không được vượt quá 255 ký tự.")]
        [Display(Name = "Mô tả")]
        public string MoTa { get; set; }

        [Display(Name = "Ảnh đại diện")]
        public bool LaAnhDaiDien { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Thứ tự hiển thị không được âm.")]
        [Display(Name = "Thứ tự hiển thị")]
        public int ThuTuHienThi { get; set; }

        [Display(Name = "Đang hiển thị")]
        public bool TrangThai { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!string.IsNullOrWhiteSpace(DuongDanAnh) && !QLKS.Services.RoomImageService.IsValidImagePath(DuongDanAnh))
                yield return new ValidationResult("Chỉ chấp nhận URL https:// hợp lệ hoặc đường dẫn cục bộ trong thư mục /Content/. Không dùng http, javascript, data, file hoặc ../.", new[] { "DuongDanAnh" });
            if (LaAnhDaiDien && !TrangThai)
                yield return new ValidationResult("Ảnh đại diện phải ở trạng thái đang hiển thị.", new[] { "TrangThai" });
        }
    }

    public class RoomImageCreateViewModel : RoomImageInputViewModel
    {
        [Required(ErrorMessage = "Vui lòng chọn phòng.")]
        [Display(Name = "Phòng")]
        public int? MaPhong { get; set; }
    }

    public class RoomImageEditViewModel : RoomImageInputViewModel
    {
        [Required]
        public int MaHinhAnh { get; set; }
        [Required]
        public int MaPhong { get; set; }
    }

    public class RoomImageViewModel
    {
        public int MaHinhAnh { get; set; }
        public int MaPhong { get; set; }
        public string DuongDanAnh { get; set; }
        public string ImageUrl { get; set; }
        public string MoTa { get; set; }
        public bool LaAnhDaiDien { get; set; }
        public int ThuTuHienThi { get; set; }
        public bool TrangThai { get; set; }
        public DateTime NgayTao { get; set; }
        public string AltText { get; set; }
    }

    public class RoomImageGalleryViewModel
    {
        public int MaPhong { get; set; }
        public string SoPhong { get; set; }
        public string PrimaryImageUrl { get; set; }
        public IList<string> ImageUrls { get; set; }
        public string ImageAltText { get; set; }
        public IList<RoomImageViewModel> Images { get; set; }
        public int ActiveImageCount { get { return Images == null ? 0 : Images.Count(x => x.TrangThai); } }
    }

    public class RoomImageAdminIndexViewModel
    {
        public Phong Room { get; set; }
        public IList<RoomImageViewModel> Images { get; set; }
    }

    public class RoomAdminDetailsViewModel
    {
        public Phong Room { get; set; }
        public RoomImageGalleryViewModel Gallery { get; set; }
    }

    public class EmployeeCreateViewModel
    {
        [Required, StringLength(50, MinimumLength = 3)]
        [RegularExpression(@"^[A-Za-z0-9._-]+$", ErrorMessage = "Tên đăng nhập chỉ gồm chữ không dấu, số, dấu chấm, gạch dưới hoặc gạch ngang.")]
        [Display(Name = "Tên đăng nhập")]
        public string TenDangNhap { get; set; }

        [Required, StringLength(100, MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string MatKhau { get; set; }

        [Required, DataType(DataType.Password)]
        [Compare("MatKhau", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        [Display(Name = "Xác nhận mật khẩu")]
        public string XacNhanMatKhau { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Họ tên")]
        public string TenNV { get; set; }

        [StringLength(10)]
        [Display(Name = "Giới tính")]
        public string GioiTinh { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Ngày sinh")]
        public DateTime? NgaySinh { get; set; }

        [RegularExpression(@"^(0|\+84)[0-9]{9,10}$", ErrorMessage = "Số điện thoại không hợp lệ.")]
        [StringLength(20)]
        [Display(Name = "Số điện thoại")]
        public string SDT { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn vai trò.")]
        [Display(Name = "Vai trò")]
        public int? VaiTro { get; set; }
    }

    public class EmployeeEditViewModel
    {
        public int MaNV { get; set; }

        [Required, StringLength(50, MinimumLength = 3)]
        [RegularExpression(@"^[A-Za-z0-9._-]+$", ErrorMessage = "Tên đăng nhập không hợp lệ.")]
        [Display(Name = "Tên đăng nhập")]
        public string TenDangNhap { get; set; }

        [Required, StringLength(100)]
        [Display(Name = "Họ tên")]
        public string TenNV { get; set; }

        [StringLength(10)]
        [Display(Name = "Giới tính")]
        public string GioiTinh { get; set; }

        [DataType(DataType.Date)]
        [Display(Name = "Ngày sinh")]
        public DateTime? NgaySinh { get; set; }

        [RegularExpression(@"^(0|\+84)[0-9]{9,10}$", ErrorMessage = "Số điện thoại không hợp lệ.")]
        [StringLength(20)]
        [Display(Name = "Số điện thoại")]
        public string SDT { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn vai trò.")]
        [Display(Name = "Vai trò")]
        public int? VaiTro { get; set; }

        [Display(Name = "Đang hoạt động")]
        public bool TrangThai { get; set; }
    }

    public abstract class CustomerViewModelBase
    {
        [Required, StringLength(100)]
        [Display(Name = "Họ tên")]
        public string TenKH { get; set; }

        [StringLength(20)]
        [RegularExpression(@"^([0-9]{9}|[0-9]{12})$", ErrorMessage = "CCCD phải gồm 9 hoặc 12 chữ số.")]
        public string CCCD { get; set; }

        [StringLength(10)]
        [Display(Name = "Giới tính")]
        public string GioiTinh { get; set; }

        [Range(1900, 2100, ErrorMessage = "Năm sinh không hợp lệ.")]
        [Display(Name = "Năm sinh")]
        public int? NamSinh { get; set; }

        [RegularExpression(@"^(0|\+84)[0-9]{9,10}$", ErrorMessage = "Số điện thoại không hợp lệ.")]
        [StringLength(20)]
        [Display(Name = "Điện thoại")]
        public string DienThoai { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ.")]
        [StringLength(100)]
        public string Email { get; set; }

        [StringLength(255)]
        [Display(Name = "Địa chỉ")]
        public string DiaChi { get; set; }
    }

    public class CustomerCreateViewModel : CustomerViewModelBase
    {
    }

    public class CustomerEditViewModel : CustomerViewModelBase
    {
        public int MaKH { get; set; }
    }

    public class ChangePasswordViewModel
    {
        [Required, DataType(DataType.Password)]
        [Display(Name = "Mật khẩu hiện tại")]
        public string MatKhauHienTai { get; set; }

        [Required, StringLength(100, MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu mới")]
        public string MatKhauMoi { get; set; }

        [Required, DataType(DataType.Password)]
        [Compare("MatKhauMoi", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        [Display(Name = "Xác nhận mật khẩu mới")]
        public string XacNhanMatKhauMoi { get; set; }
    }

    public class RevenueReportRowViewModel
    {
        public int Ngay { get; set; }
        public int SoLuongDon { get; set; }
        public decimal DoanhThu { get; set; }
    }

    public class PageHeaderViewModel
    {
        public string Title { get; set; }
        public string Subtitle { get; set; }
        public string Icon { get; set; }
        public string ActionUrl { get; set; }
        public string ActionText { get; set; }
        public string ActionIcon { get; set; }
        public string ActionCssClass { get; set; }
    }

    public class EmptyStateViewModel
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public string Icon { get; set; }
        public string ActionUrl { get; set; }
        public string ActionText { get; set; }
    }

    public class StatusBadgeViewModel
    {
        public string Text { get; set; }
        public string CssClass { get; set; }
        public string Icon { get; set; }
    }

    public class PaginationViewModel
    {
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public string Action { get; set; }
        public string Controller { get; set; }
        public RouteValueDictionary RouteValues { get; set; }
    }

    public class PagedResultViewModel<T>
    {
        public IList<T> Items { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; }
        public int TotalItems { get; set; }
        public int TotalPages
        {
            get { return PageSize <= 0 ? 0 : (int)Math.Ceiling(TotalItems / (double)PageSize); }
        }
    }

    public class RecentActivityViewModel
    {
        public DateTime? Time { get; set; }
        public string UserName { get; set; }
        public string Action { get; set; }
        public string Description { get; set; }
    }

    public class DashboardViewModel
    {
        public int TotalRooms { get; set; }
        public int AvailableRooms { get; set; }
        public int OccupiedRooms { get; set; }
        public int CleaningRooms { get; set; }
        public int MaintenanceRooms { get; set; }
        public int CurrentGuests { get; set; }
        public int UnpaidInvoices { get; set; }
        public int CheckInsToday { get; set; }
        public decimal RevenueToday { get; set; }
        public bool CanViewRevenue { get; set; }
        public IList<RecentActivityViewModel> RecentActivities { get; set; }
    }

    public class ActiveRoomStayViewModel
    {
        public int InvoiceId { get; set; }
        public string CustomerName { get; set; }
        public DateTime? CheckIn { get; set; }
    }

    public class RoomIndexViewModel
    {
        public IList<Phong> Rooms { get; set; }
        public IDictionary<int, ActiveRoomStayViewModel> ActiveStays { get; set; }
        public string Search { get; set; }
        public int? Floor { get; set; }
        public int? RoomTypeId { get; set; }
        public string Status { get; set; }
        public IList<int> Floors { get; set; }
        public IList<LoaiPhong> RoomTypes { get; set; }
        public IDictionary<int, RoomImageGalleryViewModel> ImageGalleries { get; set; }
    }

    public class InvoiceIndexViewModel
    {
        public PagedResultViewModel<HoaDon> Results { get; set; }
        public string InvoiceCode { get; set; }
        public string Customer { get; set; }
        public int? Status { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    public class CustomerIndexViewModel
    {
        public PagedResultViewModel<KhachHang> Results { get; set; }
        public string Name { get; set; }
        public string IdentityNumber { get; set; }
        public string Phone { get; set; }
    }

    public class EmployeeIndexViewModel
    {
        public PagedResultViewModel<NhanVien> Results { get; set; }
        public string Search { get; set; }
        public int? RoleId { get; set; }
        public bool? IsActive { get; set; }
        public IList<VaiTro> Roles { get; set; }
    }

    public class HousekeepingIndexViewModel
    {
        public IList<Phong> Rooms { get; set; }
        public int? Floor { get; set; }
        public string Status { get; set; }
        public IList<int> Floors { get; set; }
    }

    public class AuditLogIndexViewModel
    {
        public PagedResultViewModel<NhatKyHoatDong> Results { get; set; }
        public string Employee { get; set; }
        public string ActionType { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public IList<string> ActionTypes { get; set; }
    }

    public class CustomerRegisterViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên."), StringLength(100)]
        [Display(Name = "Họ tên")]
        public string TenKH { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email."), EmailAddress(ErrorMessage = "Email không hợp lệ."), StringLength(100)]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại."), StringLength(20)]
        [RegularExpression(@"^(0|\+84)[0-9]{9,10}$", ErrorMessage = "Số điện thoại không hợp lệ.")]
        [Display(Name = "Số điện thoại")]
        public string DienThoai { get; set; }

        [StringLength(20)]
        [RegularExpression(@"^([0-9]{9}|[0-9]{12})$", ErrorMessage = "CCCD phải gồm 9 hoặc 12 chữ số.")]
        public string CCCD { get; set; }

        [StringLength(255)]
        [Display(Name = "Địa chỉ")]
        public string DiaChi { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu."), StringLength(100, MinimumLength = 8)]
        [DataType(DataType.Password), Display(Name = "Mật khẩu")]
        public string MatKhau { get; set; }

        [Required(ErrorMessage = "Vui lòng xác nhận mật khẩu."), DataType(DataType.Password)]
        [Compare("MatKhau", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        [Display(Name = "Xác nhận mật khẩu")]
        public string XacNhanMatKhau { get; set; }

        [Display(Name = "Đồng ý điều khoản sử dụng và chính sách bảo mật")]
        public bool DongYDieuKhoan { get; set; }
    }

    public class CustomerLoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập email."), EmailAddress(ErrorMessage = "Email không hợp lệ."), StringLength(100)]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu."), DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string MatKhau { get; set; }
    }

    public class CustomerProfileViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên."), StringLength(100)]
        [Display(Name = "Họ tên")]
        public string TenKH { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email."), EmailAddress(ErrorMessage = "Email không hợp lệ."), StringLength(100)]
        public string Email { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại."), StringLength(20)]
        [RegularExpression(@"^(0|\+84)[0-9]{9,10}$", ErrorMessage = "Số điện thoại không hợp lệ.")]
        [Display(Name = "Số điện thoại")]
        public string DienThoai { get; set; }

        [StringLength(20)]
        [RegularExpression(@"^([0-9]{9}|[0-9]{12})$", ErrorMessage = "CCCD phải gồm 9 hoặc 12 chữ số.")]
        public string CCCD { get; set; }

        [StringLength(255), Display(Name = "Địa chỉ")]
        public string DiaChi { get; set; }

        [Display(Name = "Ngày tạo tài khoản")]
        public DateTime? NgayTaoTaiKhoan { get; set; }

        [Display(Name = "Trạng thái tài khoản")]
        public string TrangThaiTaiKhoan { get; set; }
    }

    public class CustomerChangePasswordViewModel
    {
        [Required, DataType(DataType.Password), Display(Name = "Mật khẩu hiện tại")]
        public string MatKhauHienTai { get; set; }

        [Required, StringLength(100, MinimumLength = 8), DataType(DataType.Password), Display(Name = "Mật khẩu mới")]
        public string MatKhauMoi { get; set; }

        [Required, DataType(DataType.Password), Compare("MatKhauMoi", ErrorMessage = "Mật khẩu xác nhận không khớp.")]
        [Display(Name = "Xác nhận mật khẩu mới")]
        public string XacNhanMatKhauMoi { get; set; }
    }

    public class RoomSearchViewModel : IValidatableObject
    {
        [Required(ErrorMessage = "Vui lòng chọn ngày nhận phòng."), DataType(DataType.Date)]
        [Display(Name = "Ngày nhận phòng")]
        public DateTime? NgayNhanPhong { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn ngày trả phòng."), DataType(DataType.Date)]
        [Display(Name = "Ngày trả phòng")]
        public DateTime? NgayTraPhong { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số người."), Range(1, 50, ErrorMessage = "Số người phải từ 1 đến 50.")]
        [Display(Name = "Số người")]
        public int? SoNguoi { get; set; }

        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (NgayNhanPhong.HasValue && NgayNhanPhong.Value.Date < DateTime.Today)
                yield return new ValidationResult("Ngày nhận phòng không được nhỏ hơn hôm nay.", new[] { "NgayNhanPhong" });
            if (NgayNhanPhong.HasValue && NgayTraPhong.HasValue && NgayTraPhong.Value.Date <= NgayNhanPhong.Value.Date)
                yield return new ValidationResult("Ngày trả phòng phải sau ngày nhận phòng.", new[] { "NgayTraPhong" });
        }
    }

    public class AvailableRoomViewModel
    {
        public int MaPhong { get; set; }
        public string SoPhong { get; set; }
        public int Tang { get; set; }
        public string TenLoai { get; set; }
        public int SoNguoiToiDa { get; set; }
        public decimal GiaMoiDem { get; set; }
        public string MoTa { get; set; }
        public string AnhDaiDien { get; set; }
        public string PrimaryImageUrl { get; set; }
        public IList<string> ImageUrls { get; set; }
        public string ImageAltText { get; set; }
        public int SoDem { get; set; }
        public decimal TongTienDuKien { get; set; }
        public decimal TienCoc { get; set; }
        public bool ConTrongTrongKhoang { get; set; }
    }

    public class AvailableRoomsPageViewModel
    {
        public RoomSearchViewModel Search { get; set; }
        public IList<AvailableRoomViewModel> Rooms { get; set; }
    }

    public class CustomerRoomPreviewViewModel
    {
        public int MaPhong { get; set; }
        public string SoPhong { get; set; }
        public string TenLoai { get; set; }
        public int SoNguoiToiDa { get; set; }
        public decimal GiaMoiDem { get; set; }
        public string MoTa { get; set; }
        public string AnhDaiDien { get; set; }
        public string PrimaryImageUrl { get; set; }
        public IList<string> ImageUrls { get; set; }
        public string ImageAltText { get; set; }
    }

    public class CustomerServiceViewModel
    {
        public int MaDV { get; set; }
        public string TenDV { get; set; }
        public decimal DonGia { get; set; }
        public string MoTa { get; set; }
        public string AnhMinhHoa { get; set; }
        public string TrangThaiHienThi { get; set; }
    }

    public class CustomerHomeViewModel
    {
        public RoomSearchViewModel Search { get; set; }
        public IList<CustomerRoomPreviewViewModel> FeaturedRooms { get; set; }
        public IList<CustomerServiceViewModel> FeaturedServices { get; set; }
    }

    public class CustomerAmenitiesViewModel
    {
        public IList<CustomerServiceViewModel> Services { get; set; }
    }

    public class CustomerServicesViewModel
    {
        public IList<CustomerServiceViewModel> Services { get; set; }
    }

    public class CustomerContactViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên."), StringLength(100)]
        [Display(Name = "Họ và tên")]
        public string TenNguoiGui { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập email."), EmailAddress(ErrorMessage = "Email không hợp lệ."), StringLength(100)]
        public string Email { get; set; }

        [StringLength(50)]
        [Display(Name = "Loại yêu cầu")]
        public string LoaiYeuCau { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập nội dung."), StringLength(1000, MinimumLength = 10, ErrorMessage = "Nội dung cần từ 10 đến 1000 ký tự.")]
        [Display(Name = "Nội dung")]
        public string NoiDung { get; set; }
    }

    public class CustomerContactPageViewModel
    {
        public CustomerContactViewModel Form { get; set; }
        public string HotelName { get; set; }
        public string Address { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string WorkingHours { get; set; }
    }

    public class CustomerBookingSummaryViewModel
    {
        public string TenKH { get; set; }
        public string Email { get; set; }
        public string DienThoai { get; set; }
    }

    public class RoomDetailsPageViewModel
    {
        public RoomSearchViewModel Search { get; set; }
        public AvailableRoomViewModel Room { get; set; }
    }

    public class OnlineBookingCreateViewModel
    {
        [Required]
        public int? MaPhong { get; set; }

        [Required, DataType(DataType.Date)]
        public DateTime? NgayNhanPhong { get; set; }

        [Required, DataType(DataType.Date)]
        public DateTime? NgayTraPhong { get; set; }

        [Required, Range(1, 50)]
        public int? SoNguoi { get; set; }

        [StringLength(500), Display(Name = "Ghi chú")]
        public string GhiChu { get; set; }

        [Display(Name = "Tôi đã đọc và đồng ý với chính sách đặt, thanh toán và hủy phòng")]
        public bool XacNhanChinhSach { get; set; }

        public AvailableRoomViewModel Room { get; set; }
        public CustomerBookingSummaryViewModel Customer { get; set; }
    }

    public class OnlineBookingListItemViewModel
    {
        public int MaDatPhong { get; set; }
        public int MaKH { get; set; }
        public string CustomerName { get; set; }
        public string CustomerEmail { get; set; }
        public string CustomerPhone { get; set; }
        public string RoomNumber { get; set; }
        public int RoomId { get; set; }
        public string PrimaryImageUrl { get; set; }
        public string ImageAltText { get; set; }
        public DateTime NgayDat { get; set; }
        public DateTime NgayNhanPhong { get; set; }
        public DateTime NgayTraPhong { get; set; }
        public int SoDem { get; set; }
        public decimal TongTienDuKien { get; set; }
        public decimal TienCoc { get; set; }
        public string TrangThai { get; set; }
        public DateTime HanThanhToan { get; set; }
        public string TransactionCode { get; set; }
        public byte[] RowVersion { get; set; }
    }

    public class OnlineBookingDetailsViewModel : OnlineBookingListItemViewModel
    {
        public int SoNguoi { get; set; }
        public decimal DonGiaTaiThoiDiemDat { get; set; }
        public DateTime? NgayThanhToanCoc { get; set; }
        public DateTime? NgayXacNhan { get; set; }
        public string ConfirmedBy { get; set; }
        public DateTime? NgayHuy { get; set; }
        public string LyDoHuy { get; set; }
        public string GhiChu { get; set; }
        public int? MaHoaDon { get; set; }
        public IList<DepositPaymentViewModel> Payments { get; set; }
    }

    public class DepositPaymentViewModel
    {
        public string TransactionCode { get; set; }
        public decimal Amount { get; set; }
        public string Method { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? PaidAt { get; set; }
    }

    public class OnlineBookingPaymentViewModel : OnlineBookingDetailsViewModel
    {
        public decimal DepositRatePercent { get; set; }
        public bool CanPay { get; set; }
    }

    public class OnlineBookingCancelViewModel
    {
        [Required]
        public int MaDatPhong { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập lý do hủy."), StringLength(500, MinimumLength = 5)]
        [Display(Name = "Lý do hủy")]
        public string LyDoHuy { get; set; }

        [Required]
        public string RowVersion { get; set; }

        public OnlineBookingDetailsViewModel Booking { get; set; }
    }

    public class OnlineBookingFilterViewModel
    {
        public string Search { get; set; }
        public string Status { get; set; }
        public PagedResultViewModel<OnlineBookingListItemViewModel> Results { get; set; }
    }

    public class OnlineBookingAdminActionViewModel
    {
        [Required]
        public int MaDatPhong { get; set; }

        [Required]
        public string RowVersion { get; set; }

        [StringLength(500)]
        public string Reason { get; set; }
    }
}
