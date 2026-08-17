using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLKS.Data
{
    [Table("tblThanhToanCoc")]
    public partial class ThanhToanCoc
    {
        [Key]
        public int MaThanhToan { get; set; }
        public int MaDatPhong { get; set; }
        public string MaGiaoDich { get; set; }
        public decimal SoTien { get; set; }
        public string PhuongThuc { get; set; }
        public string TrangThai { get; set; }
        public DateTime ThoiGianTao { get; set; }
        public Nullable<DateTime> ThoiGianThanhToan { get; set; }
        public string NoiDung { get; set; }

        [ForeignKey("MaDatPhong")]
        public virtual DatPhongOnline DatPhongOnline { get; set; }
    }
}
