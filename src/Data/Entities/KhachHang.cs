using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLKS.Data
{
    [Table("tblKhachHang")]
    public partial class KhachHang
    {
        public KhachHang()
        {
            this.HoaDons = new HashSet<HoaDon>();
            this.DatPhongOnlines = new HashSet<DatPhongOnline>();
        }
    
        [Key]
        public int MaKH { get; set; }
        public string TenKH { get; set; }
        public string CCCD { get; set; }
        public string GioiTinh { get; set; }
        public Nullable<int> NamSinh { get; set; }
        public string DienThoai { get; set; }
        public string Email { get; set; }
        public string DiaChi { get; set; }
        public string MatKhau { get; set; }
    
        public virtual ICollection<HoaDon> HoaDons { get; set; }
        public virtual ICollection<DatPhongOnline> DatPhongOnlines { get; set; }
    }
}
