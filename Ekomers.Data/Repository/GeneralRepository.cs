 
using Ekomers.Data.Repository.IRepository;
using Ekomers.Models.Ekomers;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Data.Repository
{
   

    public class GeneralRepository<TEntity> : IGeneralRepository<TEntity> where TEntity : BaseEntity
    {
        private readonly ApplicationDbContext _context;
       // private PurchasingCRMContext _context;

        private DbSet<TEntity> _dbSet;

        private DbSet<TEntity> _dbCopy;
        public GeneralRepository(ApplicationDbContext Context)
        {
            _context = Context;
            _dbSet = _context.Set<TEntity>();

        }


        public void Delete(object EntityId)
        {
            TEntity entityDelete = _dbSet.Find(EntityId);
            Delete(entityDelete);
        }
        public void Delete(TEntity Entity)
        {
            if (_context.Entry(Entity).State == EntityState.Detached)
            {
                _dbSet.Attach(Entity);
            }
            _dbSet.Remove(Entity);
        }

        public virtual TEntity FindById(object EntityId)
        {
            return _dbSet.Find(EntityId);
        }

        public virtual void Insert(TEntity Entity)
        {
            _dbSet.Add(Entity);
        }

        public virtual IEnumerable<TEntity> Select(Expression<Func<TEntity, bool>> Filter = null)
        {
            if (Filter != null)
            {
                return _dbSet.Where(Filter);
            }
            return _dbSet;
        }

        public virtual void Update(TEntity Entity)
        {
            _dbSet.Attach(Entity);
            _context.Entry(Entity).State = EntityState.Modified;
        }
        public virtual IQueryable<TEntity> GetAll()
        {
            return _dbSet;
        }

        public virtual List<TEntity> GetList()
        {
            return _dbSet.ToList();
        }
        public void InsertRange(List<TEntity> Entity)
        {
            _dbSet.AddRange(Entity);
        }

        public void SendEmail(TEntity Entity)
        {
            _dbSet.Add(Entity);
        }

        public IQueryable<TEntity> GetWith(Expression<Func<TEntity, bool>> Predicate)
        {
            return _dbSet.Where(Predicate);
        }

        public IQueryable<TEntity> GetWith(int skip, int take, Expression<Func<TEntity, bool>> Predicate = null)
        {
            return _dbSet.OrderBy(x => x.CreateDate).Skip(skip).Take(take).Where(Predicate);
        }
        //public IPagedList<TEntity> GetPagedList(int pageNr, int recordNr, Expression<Func<TEntity, bool>> Predicate = null)
        //{
        //    return _dbSet.OrderByDescending(x => x.ID).Where(Predicate).ToPagedList(pageNr, recordNr);
        //}

        public int Count()
        {
            return _dbSet.Count();
        }

        public int Count(Expression<Func<TEntity, bool>> Predicate)
        {
            return _dbSet.Count(Predicate);
        }

        public async Task<List<TEntity>> GetListAsyc()
        {
          return await _dbSet.ToListAsync();
        }
    }
}
