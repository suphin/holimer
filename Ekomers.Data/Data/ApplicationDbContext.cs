using Ekomers.Models.Ekomers; 
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using static System.Net.Mime.MediaTypeNames;
using Microsoft.AspNetCore.Mvc.RazorPages;

//using NetTopologySuite.Geometries;
using Azure.Core;
using Ekomers.Models.Entity;



namespace Ekomers.Data
{
    public class ApplicationDbContext : IdentityDbContext<Kullanici, Rol, string>
    //public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }
        //public DbSet<Sayfa> Sayfa { get; set; }

        /* ortak tablolar*/
        
		public DbSet<Departman> Departman { get; set; }
       
        public DbSet<DepartmanBirim> DepartmanBirim { get; set; }
        public DbSet<UserImage> UserImage { get; set; }
		public DbSet<Sehirler> Sehirler { get; set; }
		public DbSet<VergiDairesi> VergiDairesi { get; set; }
		public DbSet<Mahalle> Mahalle { get; set; }
        public DbSet<Sokak> Sokak { get; set; }
     
        public DbSet<TableMetadata> TableMetadata { get; set; }
        public DbSet<Geojson> Geojson { get; set; }


        
   
         

        public DbSet<UserActivityLog> UserActivityLog { get; set; }
        public DbSet<CrmActivityLog> CrmActivityLog { get; set; }



    
 
        /*  ortak tabloları*/
        public DbSet<Dosya> Dosya { get; set; }
        /* :end*/
   

		 

  
		 


 


		/*  stok tabloları*/
		public DbSet<MalzemeGrup> MalzemeGrup { get; set; }
		public DbSet<MalzemeBirim> MalzemeBirim { get; set; }
		public DbSet<MalzemeFiyat> MalzemeFiyat { get; set; }
		public DbSet<MalzemeMaliyetFiyat> MalzemeMaliyetFiyat { get; set; }
		public DbSet<MalzemeTipi> MalzemeTipi { get; set; } 
		public DbSet<Malzeme> Malzeme { get; set; } 
		public DbSet<MalzemeStok> MalzemeStok { get; set; } 
		public DbSet<Iade> Iade { get; set; } 
		public DbSet<IadeSebep> IadeSebep { get; set; } 
		public DbSet<MalzemeIade> MalzemeIade { get; set; } 
		public DbSet<MalzemeDepo> MalzemeDepo { get; set; } 
		public DbSet<MalzemeHareketTur> MalzemeHareketTur { get; set; }
		public DbSet<DovizTur> DovizTur { get; set; }
		/* stok:end*/

 




		 
		public DbSet<PortalMenu> PortalMenu { get; set; }
		public DbSet<AuthorizationCategory> AuthorizationCategory { get; set; }
		public DbSet<Yetkilendirme> Yetkilendirme { get; set; }




		// crm 
		public DbSet<Musteriler> Musteriler { get; set; }
		public DbSet<MusteriTip> MusteriTip { get; set; }
		public DbSet<Aktivite> Aktivite { get; set; }
		public DbSet<AktiviteTur> AktiviteTur { get; set; }
		public DbSet<Firsat> Firsat { get; set; }
		public DbSet<FirsatDurum> FirsatDurum { get; set; }
	 
		public DbSet<Teklif> Teklif { get; set; }
		public DbSet<TeklifDurum> TeklifDurum { get; set; }
		public DbSet<TeklifTur> TeklifTur { get; set; }
		public DbSet<TeklifUrunler> TeklifUrunler{ get; set; }
        public DbSet<TeklifIskonto> TeklifIskonto { get; set; }
        public DbSet<Siparis> Siparis { get; set; }
		public DbSet<SiparisDurum> SiparisDurum { get; set; }
		public DbSet<SiparisTur> SiparisTur { get; set; }
		public DbSet<SiparisIskonto> SiparisIskonto { get; set; }
		public DbSet<SiparisUrunler> SiparisUrunler { get; set; }
		public DbSet<CrmHedefler> CrmHedefler { get; set; }

		public DbSet<SiparisIade> SiparisIade { get; set; }
		public DbSet<SiparisIadeDurum> SiparisIadeDurum { get; set; }
		public DbSet<SiparisIadeSebep> SiparisIadeSebep { get; set; }
		public DbSet<SiparisIadePlatform> SiparisIadePlatform { get; set; }
		public DbSet<SiparisIadeTur> SiparisIadeTur { get; set; }
		public DbSet<SiparisIadeUrunler> SiparisIadeUrunler { get; set; }
		// şirketler 

		public DbSet<Sirketler> Sirketler { get; set; }
        public DbSet<Eczane> Eczane { get; set; }
       
        public DbSet<UserShortCut> UserShortCut { get; set; }
        public DbSet<UserShortCutField> UserShortCutField { get; set; }


		#region View tablalar



		#endregion

		//protected override void OnModelCreating(ModelBuilder modelBuilder)
		//{
		//    modelBuilder.Entity<GeoRoad>(entity =>
		//    {
		//        entity.Property(e => e.Geometry).HasColumnType("geometry");
		//    });
		//}



		//protected override void OnModelCreating(ModelBuilder builder)
		//{
		//    builder.Entity<DashboardVM>().HasNoKey();
		//}

		//public List<SevkEdilenlerVM> LogoSevkEdilenler()
		//{
		//    return Database.Sql("SELECT FROM table WHERE ");
		//}




		// depo sayım
		public DbSet<Warehouse> Warehouses { get; set; }
		public DbSet<Product> Products { get; set; }
		public DbSet<WarehouseInventory> WarehouseInventories { get; set; }
		public DbSet<StockCount> StockCounts { get; set; }


		
		public DbSet<Recete> Recete { get; set; }
		public DbSet<ReceteUrunler> ReceteUrunler { get; set; }
		public DbSet<ReceteParametre> ReceteParametre { get; set; }
		public DbSet<ReceteParametreDeger> ReceteParametreDeger { get; set; }
		public DbSet<Uretici> Uretici { get; set; }
		public DbSet<Uretim> Uretim { get; set; }
		public DbSet<UretimUrunler> UretimUrunler { get; set; }
		public DbSet<UretimTeslimat> UretimTeslimat { get; set; }
		public DbSet<UretimParametreDeger> UretimParametreDeger { get; set; }




		//satışlar
		public DbSet<Satislar> Satislar { get; set; }
		public DbSet<SatislarDurum> SatislarDurum { get; set; }
		public DbSet<SatislarSebep> SatislarSebep { get; set; }
		public DbSet<SatislarPlatform> SatislarPlatform { get; set; }
		public DbSet<SatislarTur> SatislarTur { get; set; }
		public DbSet<SatislarUrunler> SatislarUrunler { get; set; }


		//sözleşmeler - dijital şirket			 
		public DbSet<Sozlesmeler> Sozlesmeler { get; set; }
		public DbSet<SozlesmelerDurum> SozlesmelerDurum { get; set; }
		public DbSet<SozlesmelerKonu> SozlesmelerKonu { get; set; }


	}
}