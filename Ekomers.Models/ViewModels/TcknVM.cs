using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.ViewModels
{
	public class TcknVM
	{
        public long Tckn { get; set; }
        public string Ad { get; set; }
        public string Soyad { get; set; }
        public DateTime DogumTarihi { get; set; }
        public int DogumYil { get; set; }

        public int DogumYili => DogumTarihi.Year;
		public int Yas
		{
			get
			{
				var today = DateTime.Today;
				var age = today.Year - DogumTarihi.Year;

				if (today < DogumTarihi.AddYears(age))
				{
					age--;
				}

				return age;
			}
		}

		public string KpsUserName { get; set; }

		public string KpsPassword { get; set; }
		public string Endpoint { get; set; }

		public string StsEndpoint { get; set; }
	}
}
