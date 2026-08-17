using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLKS.Data
{
    [Table("tblNhanVien")]
    public partial class NhanVien
    {
        public NhanVien()
        {
            this.HoaDons = new HashSet<HoaDon>();
            this.NhatKyHoatDongs = new HashSet<NhatKyHoatDong>();
            this.DatPhongOnlines = new HashSet<DatPhongOnline>();
        }
    
        [Key]
        public int MaNV { get; set; }
        public string TenDangNhap { get; set; }
        public string MatKhau { get; set; }
        public string TenNV { get; set; }
        public string GioiTinh { get; set; }
        public Nullable<System.DateTime> NgaySinh { get; set; }
        public string SDT { get; set; }
        public Nullable<bool> TrangThai { get; set; }
        public Nullable<int> VaiTro { get; set; }
    
        [ForeignKey("VaiTro")]
        public virtual VaiTro VaiTroInfo { get; set; }

        public virtual ICollection<HoaDon> HoaDons { get; set; }
        public virtual ICollection<NhatKyHoatDong> NhatKyHoatDongs { get; set; }
        public virtual ICollection<DatPhongOnline> DatPhongOnlines { get; set; }
    }
}
