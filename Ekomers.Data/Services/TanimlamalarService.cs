using Ekomers.Data.Repository.IRepository;
using Ekomers.Data.Services.IServices;
using Ekomers.Models.Ekomers;
using AutoMapper;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Data.Services
{
    public class TanimlamalarService: ITanimlamalarService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly IRepository<Mahalle> _mahalleRepo;
        private readonly IRepository<Sokak> _sokakRepo;
     

        public TanimlamalarService(IMapper mapper, ApplicationDbContext context,
                       IRepository<Mahalle> mahalleRepo,
                       IRepository<Sokak> sokakRepo
                      )
        {
            _context = context;
            _mapper = mapper;
            _mahalleRepo = mahalleRepo;
            _sokakRepo = sokakRepo;
           

        }
    }
}
