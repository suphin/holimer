using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.ViewModels
{
    public class BaseVM
    {
        public int ID { get; set; }
        public bool IsActive { get; set; }
        public bool IsDelete { get; set; }
        public DateTime? CreateDate { get; set; } 
        public DateTime? DeleteDate { get; set; }
        public string? UserID { get; set; }
        public string? CreateUserID { get; set; }
        public string? DeleteUserID { get; set; }
        public string? DosyaID { get; set; }
        public string? CreateUserName { get; set; }
        public string? DeleteUserName { get; set; }
		public DateTime? UpdateDate { get; set; }
		public string? UpdateUserID { get; set; }
		public string? UpdateUserName { get; set; }
        public string? ControllerName { get; set; }
        public string? ModalTitle { get; set; }
        public bool IsDisabled { get; set; }
		public int PageIndex { get; set; }
		public int PageSize { get; set; }
		public int TotalCount { get; set; }
		public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public CancellationToken ct = default;
		public bool Sorgulandi { get; set; }


	}
}
