using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLKS.Data
{
    [Table("tblNhatKyHoatDong")]
    public partial class NhatKyHoatDong
    {
        [Key]
        public int IDLog { get; set; }
        public Nullable<int> MaNV { get; set; }
        public string HanhDong { get; set; }
        public Nullable<System.DateTime> ThoiGian { get; set; }
        public string GhiChu { get; set; }
    
        [ForeignKey("MaNV")]
        public virtual NhanVien NhanVien { get; set; }
    }
}
