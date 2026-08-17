using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLKS.Data
{
    [Table("tblPhong")]
    public partial class Phong
    {
        public Phong()
        {
            this.ChiTietHoaDons = new HashSet<ChiTietHoaDon>();
            this.DatPhongOnlines = new HashSet<DatPhongOnline>();
            this.HinhAnhPhongs = new HashSet<HinhAnhPhong>();
        }
    
        [Key]
        public int MaPhong { get; set; }
        public string SoPhong { get; set; }
        public Nullable<int> Tang { get; set; }
        public string TrangThai { get; set; }
        public string MoTaChiTiet { get; set; }
        public string AnhDaiDien { get; set; }
        public Nullable<int> MaLoai { get; set; }
    
        [ForeignKey("MaLoai")]
        public virtual LoaiPhong LoaiPhong { get; set; }

        public virtual ICollection<ChiTietHoaDon> ChiTietHoaDons { get; set; }
        public virtual ICollection<DatPhongOnline> DatPhongOnlines { get; set; }
        public virtual ICollection<HinhAnhPhong> HinhAnhPhongs { get; set; }
    }
}
