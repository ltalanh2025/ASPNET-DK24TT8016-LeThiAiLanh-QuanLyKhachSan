using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLKS.Data
{
    [Table("tblHinhAnhPhong")]
    public partial class HinhAnhPhong
    {
        [Key]
        public int MaHinhAnh { get; set; }
        public int MaPhong { get; set; }
        public string DuongDanAnh { get; set; }
        public string MoTa { get; set; }
        public bool LaAnhDaiDien { get; set; }
        public int ThuTuHienThi { get; set; }
        public bool TrangThai { get; set; }
        public DateTime NgayTao { get; set; }

        [ForeignKey("MaPhong")]
        public virtual Phong Phong { get; set; }
    }
}
