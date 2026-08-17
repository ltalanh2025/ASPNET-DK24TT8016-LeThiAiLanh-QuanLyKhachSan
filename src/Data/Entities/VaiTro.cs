using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLKS.Data
{
    [Table("tblVaiTro")]
    public partial class VaiTro
    {
        public VaiTro()
        {
            this.NhanViens = new HashSet<NhanVien>();
        }
    
        [Key]
        public int IDVaiTro { get; set; }
        public string TenVaiTro { get; set; }
        public string MoTa { get; set; }
    
        public virtual ICollection<NhanVien> NhanViens { get; set; }
    }
}
