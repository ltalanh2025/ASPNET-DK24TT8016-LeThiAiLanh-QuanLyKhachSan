using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLKS.Data
{
    [Table("tblDatPhongOnline")]
    public partial class DatPhongOnline
    {
        public DatPhongOnline()
        {
            this.ThanhToanCocs = new HashSet<ThanhToanCoc>();
        }

        [Key]
        public int MaDatPhong { get; set; }
        public int MaKH { get; set; }
        public int MaPhong { get; set; }
        public DateTime NgayDat { get; set; }
        public DateTime NgayNhanPhong { get; set; }
        public DateTime NgayTraPhong { get; set; }
        public int SoNguoi { get; set; }
        public decimal DonGiaTaiThoiDiemDat { get; set; }
        public int SoDem { get; set; }
        public decimal TongTienDuKien { get; set; }
        public decimal TienCoc { get; set; }
        public string TrangThai { get; set; }
        public DateTime HanThanhToan { get; set; }
        public Nullable<DateTime> NgayThanhToanCoc { get; set; }
        public Nullable<DateTime> NgayXacNhan { get; set; }
        public Nullable<int> MaNVXacNhan { get; set; }
        public Nullable<DateTime> NgayHuy { get; set; }
        public string LyDoHuy { get; set; }
        public string GhiChu { get; set; }
        public Nullable<int> MaHoaDon { get; set; }
        public byte[] RowVersion { get; set; }

        [ForeignKey("MaKH")]
        public virtual KhachHang KhachHang { get; set; }

        [ForeignKey("MaPhong")]
        public virtual Phong Phong { get; set; }

        [ForeignKey("MaNVXacNhan")]
        public virtual NhanVien NhanVien { get; set; }

        [ForeignKey("MaHoaDon")]
        public virtual HoaDon HoaDon { get; set; }

        public virtual ICollection<ThanhToanCoc> ThanhToanCocs { get; set; }
    }
}
