using Ekomers.Data.Services.IServices;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Data.Services
{
    public class EntityService : IEntityService
    {
        public List<object> Listele(Type entityType)
        {
           
                return new List<object>(); // Boş liste döner
           
        }
    }
}
