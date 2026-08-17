using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace QLKS.Data
{
    public partial class QLKSEntities : DbContext
    {
        public QLKSEntities()
        {
        }

        public QLKSEntities(DbContextOptions<QLKSEntities> options)
            : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer(Infrastructure.AppConfig.GetEntityConnectionString());
            }
        }

        public virtual DbSet<ChiTietDichVu> ChiTietDichVus { get; set; }
        public virtual DbSet<ChiTietHoaDon> ChiTietHoaDons { get; set; }
        public virtual DbSet<DatPhongOnline> DatPhongOnlines { get; set; }
        public virtual DbSet<DichVu> DichVus { get; set; }
        public virtual DbSet<HoaDon> HoaDons { get; set; }
        public virtual DbSet<HinhAnhPhong> HinhAnhPhongs { get; set; }
        public virtual DbSet<KhachHang> KhachHangs { get; set; }
        public virtual DbSet<LoaiPhong> LoaiPhongs { get; set; }
        public virtual DbSet<NhanVien> NhanViens { get; set; }
        public virtual DbSet<NhatKyHoatDong> NhatKyHoatDongs { get; set; }
        public virtual DbSet<Phong> Phongs { get; set; }
        public virtual DbSet<TinhTrang> TinhTrangs { get; set; }
        public virtual DbSet<ThanhToanCoc> ThanhToanCocs { get; set; }
        public virtual DbSet<VaiTro> VaiTroes { get; set; }

        public virtual DbSet<BaoCaoDoanhThuResult> BaoCaoDoanhThuResults { get; set; }
        public virtual DbSet<ThongKeTanSuatPhongResult> ThongKeTanSuatPhongResults { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<BaoCaoDoanhThuResult>().HasNoKey();
            modelBuilder.Entity<ThongKeTanSuatPhongResult>().HasNoKey();

            modelBuilder.Entity<DatPhongOnline>()
                .Property(e => e.RowVersion)
                .IsRowVersion();
        }

        public virtual int sp_Admin_ResetMatKhau(Nullable<int> maNV, string matKhauMoi, Nullable<int> adminID)
        {
            var maNVParam = new SqlParameter("@MaNV", (object)maNV ?? DBNull.Value);
            var matKhauMoiParam = new SqlParameter("@MatKhauMoi", (object)matKhauMoi ?? DBNull.Value);
            var adminIDParam = new SqlParameter("@AdminID", (object)adminID ?? DBNull.Value);

            return Database.ExecuteSqlRaw("EXEC sp_Admin_ResetMatKhau @MaNV, @MatKhauMoi, @AdminID",
                maNVParam, matKhauMoiParam, adminIDParam);
        }

        public virtual List<BaoCaoDoanhThuResult> sp_BaoCaoDoanhThu(Nullable<int> thang, Nullable<int> nam)
        {
            var thangParam = new SqlParameter("@Thang", (object)thang ?? DBNull.Value);
            var namParam = new SqlParameter("@Nam", (object)nam ?? DBNull.Value);

            return Set<BaoCaoDoanhThuResult>()
                .FromSqlRaw("EXEC sp_BaoCaoDoanhThu @Thang, @Nam", thangParam, namParam)
                .ToList();
        }

        public virtual int sp_ThemNhanVien(string tenDangNhap, string matKhau, string tenNV, string gioiTinh, Nullable<DateTime> ngaySinh, string sDT, Nullable<int> vaiTro, Nullable<int> nguoiThucHien)
        {
            var tenDangNhapParam = new SqlParameter("@TenDangNhap", (object)tenDangNhap ?? DBNull.Value);
            var matKhauParam = new SqlParameter("@MatKhau", (object)matKhau ?? DBNull.Value);
            var tenNVParam = new SqlParameter("@TenNV", (object)tenNV ?? DBNull.Value);
            var gioiTinhParam = new SqlParameter("@GioiTinh", (object)gioiTinh ?? DBNull.Value);
            var ngaySinhParam = new SqlParameter("@NgaySinh", (object)ngaySinh ?? DBNull.Value);
            var sDTParam = new SqlParameter("@SDT", (object)sDT ?? DBNull.Value);
            var vaiTroParam = new SqlParameter("@VaiTro", (object)vaiTro ?? DBNull.Value);
            var nguoiThucHienParam = new SqlParameter("@NguoiThucHien", (object)nguoiThucHien ?? DBNull.Value);

            return Database.ExecuteSqlRaw("EXEC sp_ThemNhanVien @TenDangNhap, @MatKhau, @TenNV, @GioiTinh, @NgaySinh, @SDT, @VaiTro, @NguoiThucHien",
                tenDangNhapParam, matKhauParam, tenNVParam, gioiTinhParam, ngaySinhParam, sDTParam, vaiTroParam, nguoiThucHienParam);
        }

        public virtual List<ThongKeTanSuatPhongResult> sp_ThongKeTanSuatPhong(Nullable<DateTime> tuNgay, Nullable<DateTime> denNgay)
        {
            var tuNgayParam = new SqlParameter("@TuNgay", (object)tuNgay ?? DBNull.Value);
            var denNgayParam = new SqlParameter("@DenNgay", (object)denNgay ?? DBNull.Value);

            return Set<ThongKeTanSuatPhongResult>()
                .FromSqlRaw("EXEC sp_ThongKeTanSuatPhong @TuNgay, @DenNgay", tuNgayParam, denNgayParam)
                .ToList();
        }
    }
}
