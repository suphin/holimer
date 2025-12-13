using Ekomers.Models.LogoDb;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Data
{ 
	public class LogoContext : DbContext
	{
		public LogoContext(DbContextOptions<LogoContext> options) : base(options) { }

		public DbSet<LG_100_ITEMS> Items { get; set; }
		public DbSet<PORTAL_ITEMS_LIST> LogoItems { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			modelBuilder.Entity<LG_100_ITEMS>()
				.ToTable("LG_100_ITEMS", "dbo", t => t.ExcludeFromMigrations());

			modelBuilder.Entity<LG_100_ITEMS>()
				.HasKey(x => x.LOGICALREF);

			modelBuilder.Entity<PORTAL_ITEMS_LIST>()
				.ToTable("PORTAL_ITEMS_LIST", "dbo", t => t.ExcludeFromMigrations());

			modelBuilder.Entity<PORTAL_ITEMS_LIST>()
				.HasKey(x => x.ProductId);

			base.OnModelCreating(modelBuilder);
		}
	}
}
