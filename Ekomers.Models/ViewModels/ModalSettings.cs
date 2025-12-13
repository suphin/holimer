using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.ViewModels
{
	public class ModalSettings
	{
        public string ModalID { get; set; }
        public string Genislik { get; set; }
        public string BodyID { get; set; }
		public bool Full { get; set; }= false;
	}
}
