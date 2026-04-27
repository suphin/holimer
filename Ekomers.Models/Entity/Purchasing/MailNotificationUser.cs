using Ekomers.Models.Ekomers;
using Ekomers.Models.Enums;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ekomers.Models.Entity
{
	public class MailNotificationUser
	{
		public int Id { get; set; }

		public string UserId { get; set; } // AspNetUsers FK
		public MailNotificationType Type { get; set; }

		public DateTime CreatedDate { get; set; }

		// Navigation
		public Kullanici User { get; set; }
	}
}
