using Ekomers.Data.Repository.IRepository;
using Ekomers.Models.Ekomers;

namespace Ekomers.Data.Repository
{
    public class UserActivityLogRepository : Repository<UserActivityLog>, IUserActivityLogRepository
    {
        private ApplicationDbContext _context;

        public UserActivityLogRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }
    }
}
