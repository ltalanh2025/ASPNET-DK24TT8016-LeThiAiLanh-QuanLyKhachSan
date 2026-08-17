using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace QLKS.Data
{
    [Table("tblLoaiPhong")]
    public partial class LoaiPhong
    {
        public LoaiPhong()
        {
            this.Phongs = new HashSet<Phong>();
        }
    
        [Key]
        public int MaLoai { get; set; }
        public string TenLoai { get; set; }
        public Nullable<int> SoNguoiToiDa { get; set; }
        public Nullable<decimal> GiaMacDinh { get; set; }
        public string MoTa { get; set; }
    
        public virtual ICollection<Phong> Phongs { get; set; }
    }
}
