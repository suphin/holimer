using Ekomers.Data.Repository.IRepository;
using Ekomers.Models.Ekomers;

namespace Ekomers.Data.Repository
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;
        }
        public void Dispose()
        {
            _context.Dispose();
        }

        public void Save()
        {
            _context.SaveChanges();
        }

        public IRepository<T> GetRepository<T>() where T : BaseEntity
        {
            return new Repository<T>(_context);
        }

        public IGeneralRepository<TEntity> GetGeneralRepository<TEntity>() where TEntity : BaseEntity
        {
            throw new NotImplementedException();
        }

        //public ISayfaRepository Sayfa => new SayfaRepository(_context);
        
       
        
        
    }
}
