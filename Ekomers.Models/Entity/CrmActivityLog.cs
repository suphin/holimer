using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.Entity
{
	 
	public class CrmActivityLog
	{
		public int Id { get; set; }

		public DateTime DateTime { get; set; }

		[StringLength(100)]
		public string UserName { get; set; }

		[StringLength(100)]
		public string ControllerName { get; set; }

		[StringLength(100)]
		public string ActionName { get; set; }

		[StringLength(4096)]
		public string Parameters { get; set; }

		[StringLength(255)]
		public string Info { get; set; }
		[StringLength(4096)]
		public string Details { get; set; }
	}
}
