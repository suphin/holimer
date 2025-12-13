using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Data.Services.IServices
{
	public interface IDatabaseService
	{
		 
		Task<bool> EXECUTENONQUERY(string SORGU);
		Task<string> EXECUTESCALAR(string SORGU);
		//Task<DataSet> DATASET(string sorgu, string tabload);
		Task<SqlDataReader> DATAREADER(string sorgu);


	}
}
