using Ekomers.Data.Repository.IRepository;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Data.Repository
{
    public class Repository<T> : IRepository<T> where T : class
    {
        private readonly ApplicationDbContext _context;
        private DbSet<T> _dbSet; 

        public Repository(ApplicationDbContext context)
        {
            _context = context;
            _dbSet = _context.Set<T>();
        }
        public DbSet<T> HamTablo()
        { 
            return _dbSet;
        }
        public IEnumerable<T> GetAll(Expression<Func<T, bool>> filter)
        {
            IQueryable<T> query = _dbSet;
            if (filter != null)
            {
                query = query.Where(filter);
            }
            return query.ToList();
        }
        public IQueryable<T> GetAll2(Expression<Func<T, bool>> filter)
        {
            return _dbSet.Where(filter);
        }

        public IEnumerable<T> GetAll()
        {
            return _dbSet;
        }
        public IQueryable<T> GetAll2()
        {
            return _dbSet;
        }
        public T GetFirstOrDefault(Expression<Func<T, bool>> filter)
        {
            IQueryable<T> query = _dbSet;
            if (filter != null)
            {
                query = query.Where(filter);
            }
            return query.FirstOrDefault();
        }

        public int Count()
        {
            return _dbSet.Count();
        }

        public void Delete(T entity)
        {
            _dbSet.Remove(entity);
        }

        public T GetById(int id)
        {
            return _dbSet.Find(id);
        }
        public async Task<T> GetByIdAsync(int id)
        {
            return await _dbSet.FindAsync(id);
        }
        public void Add(T entity)
        {
            _dbSet.Add(entity);
        }
		public async Task AddAsync(T entity)
		{
			await _dbSet.AddAsync(entity);
		}
		public void AddRange(List<T> entity)
		{
			_dbSet.AddRange(entity);
		}
		public async Task AddRangeAsync(List<T> entity)
		{
			await _dbSet.AddRangeAsync(entity);
		}
		public void Update(T entity)
        { 
                _dbSet.Update(entity);           
           
        }
		public async Task UpdateAsync(T entity)
		{
			 _dbSet.Update(entity);

		}
		public IQueryable<T> Include(params Expression<Func<T, object>>[] includes)
        {
            IIncludableQueryable<T, object> query = null;

            if (includes.Length > 0)
            {
                query = _dbSet.Include(includes[0]);
            }
            for (int queryIndex = 1; queryIndex < includes.Length; ++queryIndex)
            {
                query = query.Include(includes[queryIndex]);
            }

            return query == null ? _dbSet : (IQueryable<T>)query;
        }



        public async Task<IEnumerable<T>> GetPagedAsync(int pageNumber, int pageSize)
        {
            return await _dbSet
                                 .Skip((pageNumber - 1) * pageSize)
                                 .Take(pageSize)
                                 .ToListAsync();
        }

        public async Task<IQueryable<T>> GetAll2Async()
        {
            return   _dbSet;
        }

        public async Task<IEnumerable<T>> GetAllAsync()
        { 
                return await _dbSet.ToListAsync();
            
        }
    }


	public class RepositoryAsync<T> : IRepositoryAsync<T> where T : class
	{
		private readonly ApplicationDbContext _context;
		private readonly DbSet<T> _dbSet;

		public RepositoryAsync(ApplicationDbContext context)
		{
			_context = context;
			_dbSet = _context.Set<T>();
		}

		public async Task AddAsync(T entity)
		{
			await _dbSet.AddAsync(entity);
		}

  //      public async Task AddRangeAsync(List<T> entities)
  //      {
  //          await _dbSet.AddRangeAsync(entities);
		//}

		public async Task UpdateAsync(T entity)
		{
			_dbSet.Update(entity);
		}

		public async Task DeleteAsync(T entity)
		{
			_dbSet.Remove(entity);
		}

		public async Task<T> GetByIdAsync(int id)
		{
			return await _dbSet.FindAsync(id);
		}

		public async Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>> filter)
		{
			IQueryable<T> query = _dbSet;
			if (filter != null)
			{
				query = query.Where(filter);
			}
			return await query.ToListAsync();
		}

		public async Task<IEnumerable<T>> GetAllAsync()
		{
			return await _dbSet.ToListAsync();
		}

		public async Task<T> GetFirstOrDefaultAsync(Expression<Func<T, bool>> filter)
		{
			IQueryable<T> query = _dbSet;
			if (filter != null)
			{
				query = query.Where(filter);
			}
			return await query.FirstOrDefaultAsync();
		}

		public async Task<int> CountAsync()
		{
			return await _dbSet.CountAsync();
		}

		 
	}
}
