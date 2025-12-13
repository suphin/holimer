using Ekomers.Data.Services.IServices;
 
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace Ekomers.Data.Services
{
	public class DatabaseService :IDatabaseService
	{
		private readonly ApplicationDbContext _context;
		private const int SayfadakiKayitSayisi = 100;
		public DatabaseService(ApplicationDbContext context)
		{
			_context = context;
		}
	 
		public async Task<bool> EXECUTENONQUERY(string SORGU)
		{
			using (var cmd = _context.Database.GetDbConnection().CreateCommand())
			{
				try
				{
					if (cmd.Connection.State != ConnectionState.Open)
						{
							cmd.Connection.Open();
						}

					cmd.CommandText = SORGU; 
					await cmd.ExecuteNonQueryAsync();
					return true;
				}
				catch (Exception)
				{

					return false;
				} 
					
				 
			}
		}
           
	 

		public async Task<string> EXECUTESCALAR(string SORGU)
		{
			using (var cmd = _context.Database.GetDbConnection().CreateCommand())
			{
				try
				{ 
					if (cmd.Connection.State != ConnectionState.Open)
					{
						cmd.Connection.Open();
					}

					cmd.CommandText = SORGU;
					var sonuc = Convert.ToString(await cmd.ExecuteScalarAsync()) ;
					return sonuc;
				}
				catch (Exception ex)
				{

					return ex.Message;
				}


			}
		}

	
		public async Task<SqlDataReader> DATAREADER(string sorgu)
		{
			try
			{
				using (var cmd = _context.Database.GetDbConnection().CreateCommand())
				{
					  SqlDataReader okuyucu = null;
						if (cmd.Connection.State != ConnectionState.Open)
						{
							cmd.Connection.Open();
						}
						cmd.CommandText = sorgu;
						 
						DbDataReader reader = await cmd.ExecuteReaderAsync();

						if (reader.HasRows)
						{
							while (await reader.ReadAsync())
							{
							return okuyucu;
							}
						}
						else 
						reader.Close(); 
						return null;

				}
			}
			catch (Exception ex)
			{
				return null;
			}
		}

	}

		
	}

