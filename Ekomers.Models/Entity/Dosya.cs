using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.Ekomers
{
    public class Dosya:BaseEntity
    {
        public string? DosyaAdi { get; set; }
        public string? DosyaUzantisi { get; set; }
        public long? DosyaBoyutu { get; set; }
        public string? YuklenmeTarihi { get; set; }
        public string? OlusturulmaTarihi { get; set; }
        public string? DegistirilmeTarihi { get; set; }
        public string? DosyaYolu { get; set; }
        public string? ModulDosyaYolu { get; set; }
        public string? DosyaSahibi { get; set; }
        public string? DosyaTuru { get; set; }
        public string? FileID { get; set; }
        public string? FileName { get; set; }
        public string? Aciklama { get; set; }
		public bool? Is360 { get; set; }
		public double? Latitude { get; set; }
		public double? Longitude { get; set; }
		public int? KayitID { get; set; }
        public int? ModulID { get; set; }
    }

}
