using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Ekomers.Models.Entity
{
	[Table("Reports")]
	public class Report
	{
		[Key]
		public int Id { get; set; }

		[Required]
		[MaxLength(200)]
		public string ReportName { get; set; } = string.Empty;

		[MaxLength(100)]
		public string? Category { get; set; }

		[Required]
		public string SqlQuery { get; set; } = string.Empty;

		public bool IsActive { get; set; } = true;

		public int DisplayOrder { get; set; } = 0;

		public DateTime CreatedDate { get; set; } = DateTime.Now;
	}
}
