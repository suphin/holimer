using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Ekomers.Models.Entity
{
	[Table("TtnSequences")]
	public class TtnSequence
	{
		[Key]
		public int Id { get; set; }

		[Required]
		public DateTime Tarih { get; set; }

		[Required]
		public int SonNumara { get; set; }
	}
}
