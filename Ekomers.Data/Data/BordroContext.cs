using Ekomers.Models.BordroDb;
using Ekomers.Models.LogoDb;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Data
{ 
	public class BordroContext : DbContext
	{
		public BordroContext(DbContextOptions<BordroContext> options) : base(options) { }

		public DbSet<LH_001_PERSON> Personel { get; set; }
		//public DbSet<PORTAL_ITEMS_LIST> LogoItems { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<LH_001_PERSON>()
				.ToTable("LH_001_PERSON", "dbo", t => t.ExcludeFromMigrations());

			modelBuilder.Entity<LH_001_PERSON>()
				.HasKey(x => x.LOGICALREF);

			//modelBuilder.Entity<PORTAL_ITEMS_LIST>()
			//	.ToTable("PORTAL_ITEMS_LIST", "dbo", t => t.ExcludeFromMigrations());

			//modelBuilder.Entity<PORTAL_ITEMS_LIST>()
			//	.HasKey(x => x.ProductId);

			base.OnModelCreating(modelBuilder);
		}
	}
}
