using Ekomers.Data.Services.IServices;
using Ekomers.Models.Ekomers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Data.Services
{
    public class DynamicTableService: IDynamicTableService
    {
        private readonly ApplicationDbContext _context;

        public DynamicTableService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task CreateDynamicTableForUserAsync(string userId, string tableName, List<(string ColumnName, string ColumnType)> columns)
        {
            // Metadata kaydet
            int order = 0;
            foreach (var (columnName, columnType) in columns)
            {
                var metadata = new TableMetadata
                {
                    UserId = userId,
                    TableName = tableName,
                    ColumnName = columnName,
                    ColumnType = columnType,
                    ColumnOrder = order++
                };
                _context.TableMetadata.Add(metadata);
            }
            await _context.SaveChangesAsync();
			List<(string ColumnName, string ColumnType)> columnBase = new List<(string, string)>
            { 
	            ("IsActive", "bit"),
	            ("IsDelete", "bit"),
	            ("CreateDate", "datetime"),
	            ("DeleteDate", "datetime"),
	            ("CreateUserID", "nvarchar(max)"),
	            ("DeleteUserID", "nvarchar(max)"),
	            ("DosyaID", "nvarchar(max)"),
	            ("UpdateDate", "datetime"),
	            ("UpdateUserID", "nvarchar(max)")
            };
			// Dinamik tablo oluşturma SQL
			var columnDefinitions = string.Join(", ", columns.Select(c => $"{c.ColumnName} {c.ColumnType}"));
			var columnBaseDefinitions = string.Join(", ", columnBase.Select(c => $"{c.ColumnName} {c.ColumnType}"));
            var createTableSql = $"CREATE TABLE {tableName} (ID INT PRIMARY KEY IDENTITY, {columnDefinitions+','+ columnBaseDefinitions})";

            await _context.Database.ExecuteSqlRawAsync(createTableSql);
        }
		 
		public async Task<List<ExpandoObject>> GetDynamicTableDataAsExpandoAsync(string tableName)
		{
			var result = new List<ExpandoObject>();

			// Sorguyu oluşturun
			var sqlQuery = $"SELECT * FROM {tableName}";

			// Veritabanı bağlantısını açın
			using (var command = _context.Database.GetDbConnection().CreateCommand())
			{
				command.CommandText = sqlQuery;
				await _context.Database.OpenConnectionAsync();

				// DataReader ile satırları okuyun
				using (var reader = await command.ExecuteReaderAsync())
				{
					while (await reader.ReadAsync())
					{
						dynamic row = new ExpandoObject();
						var dictionary = (IDictionary<string, object>)row;

						for (int i = 0; i < reader.FieldCount; i++)
						{
							dictionary.Add(reader.GetName(i), reader.GetValue(i));
						}

						result.Add(row);
					}
				}
			}

			return result;
		}

	}

}
