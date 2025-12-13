using Ekomers.Data.Repository.IRepository;
using Ekomers.Data.Services.IServices;
using Ekomers.Models.Ekomers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Data.Services
{
    public class GeneralService<T> : IGeneralService<T> where T : BaseEntity
    {
        private readonly IUnitOfWork _uow;
        private readonly IGeneralRepository<T> _itemRepository;
        public GeneralService(IUnitOfWork uow)
        {
            _uow = uow;
            _itemRepository = uow.GetGeneralRepository<T>();
        }
        public void Delete(int itemId)
        {
            Delete(_itemRepository.FindById(itemId));
        }
        public void Delete(T item)
        {
            _itemRepository.Delete(item);
        }
        public T Find(int itemId)
        {
            return _itemRepository.FindById(itemId);
        }
        public T Find(string itemName)
        {
            return _itemRepository.FindById(itemName);
        }
        public IQueryable<T> GetAll()
        {
            return _itemRepository.GetAll();
        }
        public void Insert(T item)
        {
            _itemRepository.Insert(item);
        }
        public void Update(T item)
        {
            _itemRepository.Update(item);
        }

        public IQueryable<T> GetWith(System.Linq.Expressions.Expression<Func<T, bool>> Predicate)
        {
            return _itemRepository.GetWith(Predicate);
        }
        //public IPagedList<T> GetPagedList(int pageNr, int recordNr, Expression<Func<T, bool>> Predicate)
        //{
        //    return _itemRepository.GetPagedList(pageNr, recordNr, Predicate);
        //}

        public int Count()
        {
            return _itemRepository.Count();
        }

        public int Count(Expression<Func<T, bool>> Predicate)
        {
            return _itemRepository.Count(Predicate);
        }

        //public IPagedList<T> GetPagedWith(int pageNr, int recordNr, Expression<Func<T, bool>> Predicate)
        //{
        //    throw new NotImplementedException();
        //}

        public IQueryable<T> GetWith(int skip, int take, Expression<Func<T, bool>> Predicate)
        {
            throw new NotImplementedException();
        }

      
    }
}
