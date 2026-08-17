using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLKS.Data
{
    [Table("tblDichVu")]
    public partial class DichVu
    {
        public DichVu()
        {
            this.ChiTietDichVus = new HashSet<ChiTietDichVu>();
        }
    
        [Key]
        public int MaDV { get; set; }
        public string TenDV { get; set; }
        public Nullable<decimal> DonGia { get; set; }
        public string MoTa { get; set; }
    
        public virtual ICollection<ChiTietDichVu> ChiTietDichVus { get; set; }
    }
}
