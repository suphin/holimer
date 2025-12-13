using Ekomers.Models.Ekomers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Data.Services.IServices
{
    public interface IGeneralService<T> where T : BaseEntity
    {
        IQueryable<T> GetAll();
      
        T Find(string itemName);
        T Find(int itemId);
        void Insert(T item);
        void Update(T item);
        void Delete(T item);
        void Delete(int itemId);
        IQueryable<T> GetWith(Expression<Func<T, bool>> Predicate);
        IQueryable<T> GetWith(int skip, int take, Expression<Func<T, bool>> Predicate);
        //IPagedList<T> GetPagedList(int pageNr, int recordNr, Expression<Func<T, bool>> Predicate);
        int Count();
        int Count(Expression<Func<T, bool>> Predicate);
    }
}
