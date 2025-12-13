using Ekomers.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.Ekomers
{
    public class TableMetadata:BaseEntity
    { 
        public string UserId { get; set; } // Kullanıcıya göre kayıt için 
        public string TableName { get; set; }
        public string ColumnName { get; set; }
        public string ColumnType { get; set; }
        public int ColumnOrder { get; set; } // Alan sırası
    }

	public class TableMetadataVM : BaseVM
	{
		public string UserId { get; set; } // Kullanıcıya göre kayıt için 
		public string TableName { get; set; }
		public string ColumnName { get; set; }
		public string ColumnType { get; set; }
		public int ColumnOrder { get; set; } // Alan sırası
		public List<TableMetadataVM> TableMetadataVMListe { get; set; }
	}
}
