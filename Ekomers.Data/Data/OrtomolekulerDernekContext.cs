using Ekomers.Models.Entity;
using Ekomers.Models.LogoDb;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Data
{ 
	public class OrtomolekulerDernek : DbContext
	{
		public OrtomolekulerDernek(DbContextOptions<OrtomolekulerDernek> options) : base(options) { }

		public DbSet<DernekHaberler> Haberler { get; set; }
		public DbSet<DernekBloglar> Bloglar { get; set; }
		public DbSet<DernekEtkinlikler> Etkinlikler { get; set; }
		public DbSet<DernekKaynaklar> Kaynaklar { get; set; }

		 
	}
}
