using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Ekomers.Data.Repository.IRepository;
using Ekomers.Models.Ekomers;

namespace Ekomers.Data.Repository
{
    public class KullaniciRepository : Repository<Kullanici>, IKullaniciRepository
    {
        private ApplicationDbContext _context;

        public KullaniciRepository(ApplicationDbContext context) : base(context)
        {
            _context = context;
        }
    }
}
