using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Models.ViewModels
{
    
    public class DynamicTableVM
    {
        public string TableName { get; set; }
        public List<DynamicColumn> Columns { get; set; }
    }

    public class DynamicColumn
    {
        public string ColumnName { get; set; }
        public string ColumnType { get; set; } // int, nvarchar, datetime gibi
    }
}
