using Ekomers.Models.Ekomers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Data.Repository.IRepository
{
    public interface IUnitOfWork : IDisposable
    {
        //IKullaniciRepository Kullanici { get; }
        //IKullaniciGrupRepository KullaniciGrup { get; }
        //IKullaniciLogRepository KullaniciLog { get; }
        //ISayfaRepository Sayfa { get; }
        //ISayfaKapsamRepository SayfaKapsam { get; }
        
        

        void Save();
        IRepository<TEntity> GetRepository<TEntity>() where TEntity : BaseEntity;
        IGeneralRepository<TEntity> GetGeneralRepository<TEntity>() where TEntity : BaseEntity;
    }
}
