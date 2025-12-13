using Ekomers.Models.Ekomers;
using Ekomers.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.Entity
{
	public class UserAction:BaseEntity
	{
		public string? ShortcutAction { get; set; }
		public string? ShortcutController { get; set; }
		public string? UserID { get; set; }
		public int? UserShortCutFieldID { get; set; }
		

	}
	public class UserActionVM : BaseVM
	{
		public string? ShortcutAction { get; set; }
		public string? ShortcutController { get; set; }
		public string? UserID { get; set; }
		public int? UserShortCutFieldID { get; set; }
		public string? UserShortCutFieldName { get; set; }
		public List<UserActionVM> UserActionVMListe { get; set; }

	}
	public class UserShortCutField : BaseEntity
	{
		public string? Ad { get; set; } 
		public string? Aciklama { get; set; } 
		public int? Siralama { get; set; } 

	}
	public class UserShortCutFieldVM : BaseVM
	{
		public string? Ad { get; set; }
		public string? Aciklama { get; set; }
		public int? Siralama { get; set; }
		public List<UserShortCutFieldVM> UserShortCutFieldVMListe { get; set; }

	}
	public class UserShortCut : BaseEntity
	{
		public string? UserID { get; set; }
		public int? UserShortCutFieldID { get; set; }
		 

	}
}
