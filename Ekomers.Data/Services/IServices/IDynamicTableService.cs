using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Data.Services.IServices
{
    public interface IDynamicTableService
    {
        Task CreateDynamicTableForUserAsync(string userId, string tableName, List<(string ColumnName, string ColumnType)> columns);
        Task<List<ExpandoObject>> GetDynamicTableDataAsExpandoAsync(string tableName);

	}
}
