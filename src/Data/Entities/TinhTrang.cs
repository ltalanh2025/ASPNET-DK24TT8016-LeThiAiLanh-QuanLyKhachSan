using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLKS.Data
{
    [Table("tblTinhTrang")]
    public partial class TinhTrang
    {
        public TinhTrang()
        {
            this.HoaDons = new HashSet<HoaDon>();
        }
    
        [Key]
        public int ID { get; set; }
        public string TenTinhTrang { get; set; }
    
        public virtual ICollection<HoaDon> HoaDons { get; set; }
    }
}
