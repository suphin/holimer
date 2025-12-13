using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Data.Repository.IRepository
{
    
    public interface IGeneralRepository<TEntity> where TEntity : class
    {
        TEntity FindById(object EntityId);
        IEnumerable<TEntity> Select(Expression<Func<TEntity, bool>> Filter = null);
        IQueryable<TEntity> GetAll();
        int Count();
        int Count(Expression<Func<TEntity, bool>> Predicate);
        IQueryable<TEntity> GetWith(Expression<Func<TEntity, bool>> Predicate);
        IQueryable<TEntity> GetWith(int skip, int take, Expression<Func<TEntity, bool>> Predicate);
        List<TEntity> GetList();
       // IPagedList<TEntity> GetPagedList(int pageNr, int recordNr, Expression<Func<TEntity, bool>> Predicate);
        void Insert(TEntity Entity);
        void Update(TEntity Entity);
        void Delete(object EntityId);
        void Delete(TEntity Entity);
        void InsertRange(List<TEntity> Entity);
        void SendEmail(TEntity Entity);


        //async yapılar
        Task<List<TEntity>> GetListAsyc();

    }
}
