using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLKS.Data
{
    [Table("tblChiTietDichVu")]
    public partial class ChiTietDichVu
    {
        [Key]
        public int ID { get; set; }
        public Nullable<int> MaHD { get; set; }
        public Nullable<int> MaDV { get; set; }
        public Nullable<int> SoLuong { get; set; }
        public Nullable<decimal> DonGia { get; set; }
    
        [ForeignKey("MaDV")]
        public virtual DichVu DichVu { get; set; }

        [ForeignKey("MaHD")]
        public virtual HoaDon HoaDon { get; set; }
    }
}
