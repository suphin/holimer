using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Data.Repository.IRepository
{
    public interface IRepository<T> where T : class
    {
        DbSet<T> HamTablo();
        void Add(T entity);
        Task AddAsync(T entity)=>Task.CompletedTask;
		void AddRange(List<T> entity);
		Task AddRangeAsync(List<T> entity) => Task.CompletedTask;
		void Update(T entity);
		Task UpdateAsync(T entity)=>Task.CompletedTask;
        void Delete(T entity);
       
        T GetById(int id);
        Task<T> GetByIdAsync(int id);
        IEnumerable<T> GetAll(Expression<Func<T, bool>> filter);
        IQueryable<T> GetAll2(Expression<Func<T, bool>> filter);
        IQueryable<T> GetAll2();
        Task<IQueryable<T>> GetAll2Async();
        IEnumerable<T> GetAll();
        Task<IEnumerable<T>> GetAllAsync();

        T GetFirstOrDefault(Expression<Func<T, bool>> filter);
        IQueryable<T> Include(params Expression<Func<T, object>>[] includes);
        int Count();
        Task<IEnumerable<T>> GetPagedAsync(int pageNumber, int pageSize);
    }

	public interface IRepositoryAsync<T> where T : class
	{
		Task AddAsync(T entity); 
		//Task AddRangeAsync(List<T> entity)
		Task UpdateAsync(T entity);
		Task DeleteAsync(T entity);

		Task<T> GetByIdAsync(int id);
		Task<IEnumerable<T>> GetAllAsync(Expression<Func<T, bool>> filter);
		Task<IEnumerable<T>> GetAllAsync();
		Task<T> GetFirstOrDefaultAsync(Expression<Func<T, bool>> filter);
		Task<int> CountAsync();

	}
}
