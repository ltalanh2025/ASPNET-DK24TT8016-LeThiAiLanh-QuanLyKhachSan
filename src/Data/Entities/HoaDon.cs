using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLKS.Data
{
    [Table("tblHoaDon")]
    public partial class HoaDon
    {
        public HoaDon()
        {
            this.ChiTietDichVus = new HashSet<ChiTietDichVu>();
            this.ChiTietHoaDons = new HashSet<ChiTietHoaDon>();
            this.DatPhongOnlines = new HashSet<DatPhongOnline>();
        }
    
        [Key]
        public int MaHD { get; set; }
        public Nullable<int> MaKH { get; set; }
        public Nullable<int> MaNV { get; set; }
        public Nullable<System.DateTime> NgayLap { get; set; }
        public Nullable<System.DateTime> NgayCheckIn { get; set; }
        public Nullable<System.DateTime> NgayCheckOut { get; set; }
        public Nullable<decimal> TongTien { get; set; }
        public Nullable<int> TinhTrang { get; set; }
        public string GhiChu { get; set; }
        public Nullable<bool> DaThanhToan { get; set; }
        public decimal TienCocDaNhan { get; set; }
    
        [ForeignKey("MaKH")]
        public virtual KhachHang KhachHang { get; set; }

        [ForeignKey("MaNV")]
        public virtual NhanVien NhanVien { get; set; }

        [ForeignKey("TinhTrang")]
        public virtual TinhTrang TinhTrangHoaDon { get; set; }

        public virtual ICollection<ChiTietDichVu> ChiTietDichVus { get; set; }
        public virtual ICollection<ChiTietHoaDon> ChiTietHoaDons { get; set; }
        public virtual ICollection<DatPhongOnline> DatPhongOnlines { get; set; }
    }
}
