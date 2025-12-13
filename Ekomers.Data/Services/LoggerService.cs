using Ekomers.Data.Repository.IRepository;
using Ekomers.Data.Services.IServices;
using Ekomers.Models.Ekomers;
using Ekomers.Models.Entity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Data.Services
{
	public class LoggerService : ILoggerService
	{
		private readonly ApplicationDbContext _context;
		private readonly IRepository<CrmActivityLog> _crmActivityLogRepo;

		public LoggerService(ApplicationDbContext context, IRepository<CrmActivityLog> crmActivityLogRepo)
		{
			_context = context;
			_crmActivityLogRepo = crmActivityLogRepo;
		}
		public async Task AddCrmLog(string ControllerName, string ActionName, string Parameters, string Info, string UserName, string Details)
		{
			try
			{
				_crmActivityLogRepo.Add(new CrmActivityLog()
				{
					UserName = UserName,
					DateTime = DateTime.Now,
					ControllerName = ControllerName,
					ActionName = ActionName,
					Parameters = Parameters,
					Info = Info,
					Details = Details
				});
				await _context.SaveChangesAsync();
			}
			catch (Exception ex)
			{
				string msg = ex.Message;
			}
		}
	}
}
