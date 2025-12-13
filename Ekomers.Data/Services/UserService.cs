using Ekomers.Data.Repository.IRepository;
using Ekomers.Data.Services.IServices;
using Ekomers.Models.Ekomers;
using System.Security.Principal;

namespace Ekomers.Data.Services
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _context;
        private readonly IUserActivityLogRepository _userActivityLogRepo;

        public UserService(ApplicationDbContext context, IUserActivityLogRepository userActivityLogRepo)
        {
            _context = context;
            _userActivityLogRepo = userActivityLogRepo;
        }

        public void AddUserActivityLog(string ControllerName, string ActionName, string Parameters, string Info, string UserName)
        {
            try
            {
                _userActivityLogRepo.Add(new UserActivityLog()
                {
                    UserName = UserName,
                    DateTime = DateTime.Now,
                    ControllerName = ControllerName,
                    ActionName = ActionName,
                    Parameters = Parameters,
                    Info = Info
                });
                _context.SaveChanges();
            } catch (Exception ex)
            {
                string msg = ex.Message;
            }
        }
    }
}
