using Ekomers.Models.Enums;
using Microsoft.AspNetCore.Mvc.Rendering;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ekomers.Models.ViewModels
{
	public class MailSettingVM
	{
		public List<SelectListItem> Users { get; set; }
		public List<string> SelectedUserIds { get; set; }

		public MailNotificationType Type { get; set; }
	}
}
