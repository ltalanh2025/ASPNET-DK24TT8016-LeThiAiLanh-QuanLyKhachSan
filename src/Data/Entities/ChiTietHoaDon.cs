using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLKS.Data
{
    [Table("tblChiTietHoaDon")]
    public partial class ChiTietHoaDon
    {
        [Key]
        public int ID { get; set; }
        public Nullable<int> MaHD { get; set; }
        public Nullable<int> MaPhong { get; set; }
        public Nullable<decimal> DonGiaThucTe { get; set; }
        public Nullable<int> SoNgayO { get; set; }
    
        [ForeignKey("MaPhong")]
        public virtual Phong Phong { get; set; }

        [ForeignKey("MaHD")]
        public virtual HoaDon HoaDon { get; set; }
    }
}
