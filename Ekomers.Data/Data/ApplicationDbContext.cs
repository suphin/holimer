using Ekomers.Models.Ekomers; 
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

using static System.Net.Mime.MediaTypeNames;
using Microsoft.AspNetCore.Mvc.RazorPages;

//using NetTopologySuite.Geometries;
 
using Ekomers.Models.Entity;
using Ekomers.Models.Entity.Production;
using Ekomers.Models.Entity.Purchasing;



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
        public DbSet<TtnSequence> TtnSequences { get; set; }
        public DbSet<MailNotificationUser> MailNotificationUsers { get; set; }


        
   
         

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
		public DbSet<UretimEmriMalzeme> UretimEmriMalzeme { get; set; }
		public DbSet<StokRezervasyon> StokRezervasyon { get; set; }
		public DbSet<DepoHazirlamaEmri> DepoHazirlamaEmri { get; set; }
		public DbSet<DepoHazirlamaKalem> DepoHazirlamaKalem { get; set; }
		public DbSet<DepoHazirlamaKalemLot> DepoHazirlamaKalemLot { get; set; }

		// Bağımsız üretim yönetimi (Prd) tabloları
		public DbSet<PrdUnit> PrdUnits { get; set; }
		public DbSet<PrdMaterial> PrdMaterials { get; set; }
		public DbSet<PrdWarehouse> PrdWarehouses { get; set; }
		public DbSet<PrdStockLot> PrdStockLots { get; set; }
		public DbSet<PrdStockMovement> PrdStockMovements { get; set; }
		public DbSet<PrdInventoryDocument> PrdInventoryDocuments { get; set; }
		public DbSet<PrdInventoryDocumentLine> PrdInventoryDocumentLines { get; set; }
		public DbSet<PrdRecipe> PrdRecipes { get; set; }
		public DbSet<PrdRecipeVersion> PrdRecipeVersions { get; set; }
		public DbSet<PrdRecipeItem> PrdRecipeItems { get; set; }
		public DbSet<PrdProductionPlanHeader> PrdProductionPlanHeaders { get; set; }
		public DbSet<PrdProductionPlan> PrdProductionPlans { get; set; }
		public DbSet<PrdProductionPlanRequirement> PrdProductionPlanRequirements { get; set; }
		public DbSet<PrdProductionOrder> PrdProductionOrders { get; set; }
		public DbSet<PrdMaterialRequirement> PrdMaterialRequirements { get; set; }
		public DbSet<PrdStockReservation> PrdStockReservations { get; set; }
		public DbSet<PrdWarehouseTask> PrdWarehouseTasks { get; set; }
		public DbSet<PrdWarehouseTaskItem> PrdWarehouseTaskItems { get; set; }
		public DbSet<PrdWarehouseTaskLot> PrdWarehouseTaskLots { get; set; }
		public DbSet<PrdProductionMaterialActual> PrdProductionMaterialActuals { get; set; }
		public DbSet<PrdProductionResult> PrdProductionResults { get; set; }

		// Bağımsız satınalma yönetimi (Pur) tabloları
		public DbSet<PurPurchaseRequest> PurPurchaseRequests { get; set; }
		public DbSet<PurPurchaseRequestLine> PurPurchaseRequestLines { get; set; }
		public DbSet<PurRequestApprovalHistory> PurRequestApprovalHistories { get; set; }




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

		// satın alma
		public DbSet<PurchaseRequest> PurchaseRequests { get; set; }

		public DbSet<Request> Request { get; set; }
		public DbSet<RequestUrunler> RequestUrunler { get; set; }
		public DbSet<RequestDurum> RequestDurum { get; set; }
		public DbSet<RequestTur> RequestTur { get; set; }

		public DbSet<Offer> Offer { get; set; }
		public DbSet<OfferDurum> OfferDurum { get; set; }
		public DbSet<OfferTur> OfferTur { get; set; }
		public DbSet<OfferOdemeTur> OfferOdemeTur { get; set; }


		public DbSet<Envanter> Envanter { get; set; }
		public DbSet<EnvanterTur> EnvanterTur { get; set; }
		public DbSet<EnvanterTip> EnvanterTip { get; set; }
		public DbSet<EnvanterTipOzellik> EnvanterTipOzellik { get; set; }
		public DbSet<EnvanterOzellik> EnvanterOzellik { get; set; }


		public DbSet<EnvanterDepartman> EnvanterDepartman { get; set; }
		public DbSet<EnvanterBolum> EnvanterBolum { get; set; }

		public DbSet<Zimmet> Zimmet { get; set; }	
		public DbSet<Personel> Personel { get; set; }
		public DbSet<PersonelGorev> PersonelGorev { get; set; }
		public DbSet<PersonelDurum> PersonelDurum { get; set; }

		protected override void OnModelCreating(ModelBuilder modelBuilder)
		{
			base.OnModelCreating(modelBuilder);

			modelBuilder.Entity<Malzeme>()
				.HasIndex(x => x.LogoKod)
				.IsUnique()
				.HasFilter("[LogoKod] IS NOT NULL");

			modelBuilder.Entity<Recete>()
				.HasIndex(x => new { x.Kod, x.VersiyonNo })
				.IsUnique()
				.HasFilter("[Kod] IS NOT NULL");
			modelBuilder.Entity<Recete>().Property(x => x.VersiyonNo).HasDefaultValue(1);
			modelBuilder.Entity<Recete>().Property(x => x.BazMiktar).HasPrecision(18, 6).HasDefaultValue(1m);
			modelBuilder.Entity<ReceteUrunler>().Property(x => x.Zorunlu).HasDefaultValue(true);

			modelBuilder.Entity<Uretim>()
				.HasIndex(x => x.UretimEmriNo)
				.IsUnique()
				.HasFilter("[UretimEmriNo] IS NOT NULL");
			modelBuilder.Entity<Uretim>().Property(x => x.ReceteVersiyonNo).HasDefaultValue(1);

			modelBuilder.Entity<UretimEmriMalzeme>().Property(x => x.ReceteMiktari).HasPrecision(18, 6);
			modelBuilder.Entity<UretimEmriMalzeme>().Property(x => x.TeorikMiktar).HasPrecision(18, 6);
			modelBuilder.Entity<UretimEmriMalzeme>().Property(x => x.RezerveMiktar).HasPrecision(18, 6);
			modelBuilder.Entity<UretimEmriMalzeme>().Property(x => x.SevkEdilenMiktar).HasPrecision(18, 6);
			modelBuilder.Entity<UretimEmriMalzeme>().Property(x => x.GercekTuketimMiktari).HasPrecision(18, 6);
			modelBuilder.Entity<UretimEmriMalzeme>().Property(x => x.IadeMiktari).HasPrecision(18, 6);
			modelBuilder.Entity<UretimEmriMalzeme>().Property(x => x.FireMiktari).HasPrecision(18, 6);
			modelBuilder.Entity<UretimEmriMalzeme>().Property(x => x.AciklanamayanFark).HasPrecision(18, 6);
			modelBuilder.Entity<StokRezervasyon>().Property(x => x.RezerveMiktar).HasPrecision(18, 6);
			modelBuilder.Entity<StokRezervasyon>().Property(x => x.KullanilanMiktar).HasPrecision(18, 6);
			modelBuilder.Entity<StokRezervasyon>().Property(x => x.SerbestBirakilanMiktar).HasPrecision(18, 6);
			modelBuilder.Entity<DepoHazirlamaKalem>().Property(x => x.IstenenMiktar).HasPrecision(18, 6);
			modelBuilder.Entity<DepoHazirlamaKalem>().Property(x => x.HazirlananMiktar).HasPrecision(18, 6);
			modelBuilder.Entity<DepoHazirlamaKalem>().Property(x => x.SevkEdilenMiktar).HasPrecision(18, 6);
			modelBuilder.Entity<DepoHazirlamaKalem>().Property(x => x.EksikMiktar).HasPrecision(18, 6);
			modelBuilder.Entity<DepoHazirlamaKalemLot>().Property(x => x.Miktar).HasPrecision(18, 6);

			modelBuilder.Entity<Recete>()
				.HasOne<Recete>().WithMany().HasForeignKey(x => x.OncekiVersiyonID).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<Recete>()
				.HasOne<MalzemeBirim>().WithMany().HasForeignKey(x => x.BirimID).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<ReceteUrunler>()
				.HasOne<MalzemeBirim>().WithMany().HasForeignKey(x => x.BirimID).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<Uretim>()
				.HasOne<MalzemeDepo>().WithMany().HasForeignKey(x => x.KaynakDepoID).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<Uretim>()
				.HasOne<MalzemeDepo>().WithMany().HasForeignKey(x => x.UretimDepoID).OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<UretimEmriMalzeme>()
				.HasOne<Uretim>().WithMany().HasForeignKey(x => x.UretimID).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<UretimEmriMalzeme>()
				.HasOne<Malzeme>().WithMany().HasForeignKey(x => x.MalzemeID).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<UretimEmriMalzeme>()
				.HasOne<ReceteUrunler>().WithMany().HasForeignKey(x => x.ReceteKalemID).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<UretimEmriMalzeme>()
				.HasOne<MalzemeBirim>().WithMany().HasForeignKey(x => x.BirimID).OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<StokRezervasyon>()
				.HasOne<Uretim>().WithMany().HasForeignKey(x => x.UretimID).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<StokRezervasyon>()
				.HasOne<UretimEmriMalzeme>().WithMany().HasForeignKey(x => x.UretimEmriMalzemeID).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<StokRezervasyon>()
				.HasOne<Malzeme>().WithMany().HasForeignKey(x => x.MalzemeID).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<StokRezervasyon>()
				.HasOne<MalzemeDepo>().WithMany().HasForeignKey(x => x.DepoID).OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<DepoHazirlamaEmri>()
				.HasOne<Uretim>().WithMany().HasForeignKey(x => x.UretimID).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<DepoHazirlamaEmri>()
				.HasOne<MalzemeDepo>().WithMany().HasForeignKey(x => x.KaynakDepoID).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<DepoHazirlamaEmri>()
				.HasOne<MalzemeDepo>().WithMany().HasForeignKey(x => x.HedefDepoID).OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<DepoHazirlamaKalem>()
				.HasOne<DepoHazirlamaEmri>().WithMany().HasForeignKey(x => x.DepoHazirlamaEmriID).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<DepoHazirlamaKalem>()
				.HasOne<UretimEmriMalzeme>().WithMany().HasForeignKey(x => x.UretimEmriMalzemeID).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<DepoHazirlamaKalem>()
				.HasOne<Malzeme>().WithMany().HasForeignKey(x => x.MalzemeID).OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<DepoHazirlamaKalemLot>()
				.HasOne<DepoHazirlamaKalem>().WithMany().HasForeignKey(x => x.DepoHazirlamaKalemID).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<DepoHazirlamaKalemLot>()
				.HasOne<StokRezervasyon>().WithMany().HasForeignKey(x => x.StokRezervasyonID).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<MalzemeStok>()
				.HasOne<Uretim>().WithMany().HasForeignKey(x => x.UretimID).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<MalzemeStok>()
				.HasOne<StokRezervasyon>().WithMany().HasForeignKey(x => x.StokRezervasyonID).OnDelete(DeleteBehavior.Restrict);

			modelBuilder.Entity<StokRezervasyon>()
				.HasIndex(x => new { x.MalzemeID, x.DepoID, x.LotNumara, x.SktTarih, x.Durum });

			ConfigureProductionModel(modelBuilder);
			ConfigurePurchasingModel(modelBuilder);
		}

		private static void ConfigureProductionModel(ModelBuilder modelBuilder)
		{
			Type[] productionTypes =
			[
				typeof(PrdUnit), typeof(PrdMaterial), typeof(PrdWarehouse), typeof(PrdStockLot),
				typeof(PrdStockMovement), typeof(PrdInventoryDocument), typeof(PrdInventoryDocumentLine),
				typeof(PrdRecipe), typeof(PrdRecipeVersion), typeof(PrdRecipeItem),
				typeof(PrdProductionPlanHeader), typeof(PrdProductionPlan), typeof(PrdProductionPlanRequirement),
				typeof(PrdProductionOrder), typeof(PrdMaterialRequirement),
				typeof(PrdStockReservation), typeof(PrdWarehouseTask), typeof(PrdWarehouseTaskItem),
				typeof(PrdWarehouseTaskLot), typeof(PrdProductionMaterialActual), typeof(PrdProductionResult)
			];

			foreach (var type in productionTypes)
			{
				modelBuilder.Entity(type).ToTable(type.Name);
				foreach (var property in modelBuilder.Entity(type).Metadata.GetProperties()
					.Where(x => x.ClrType == typeof(decimal) || x.ClrType == typeof(decimal?)))
				{
					property.SetPrecision(18);
					property.SetScale(6);
				}
			}

			modelBuilder.Entity<PrdUnit>().HasIndex(x => x.Code).IsUnique();
			modelBuilder.Entity<PrdUnit>().Property(x => x.Code).HasMaxLength(50);
			modelBuilder.Entity<PrdUnit>().Property(x => x.Name).HasMaxLength(100);
			modelBuilder.Entity<PrdMaterial>().HasIndex(x => x.Code).IsUnique();
			modelBuilder.Entity<PrdMaterial>().HasIndex(x => x.LogoCode);
			modelBuilder.Entity<PrdMaterial>().Property(x => x.Code).HasMaxLength(100);
			modelBuilder.Entity<PrdMaterial>().Property(x => x.LogoCode).HasMaxLength(100);
			modelBuilder.Entity<PrdMaterial>().Property(x => x.Name).HasMaxLength(250);
			modelBuilder.Entity<PrdWarehouse>().HasIndex(x => x.Code).IsUnique();
			modelBuilder.Entity<PrdWarehouse>().Property(x => x.Code).HasMaxLength(50);
			modelBuilder.Entity<PrdWarehouse>().Property(x => x.Name).HasMaxLength(150);
			modelBuilder.Entity<PrdStockLot>().HasIndex(x => new { x.MaterialId, x.WarehouseId, x.LotNumber }).IsUnique();
			modelBuilder.Entity<PrdStockLot>().Property(x => x.LotNumber).HasMaxLength(100);
			modelBuilder.Entity<PrdStockMovement>().HasIndex(x => new { x.MaterialId, x.WarehouseId, x.StockLotId, x.MovementDate });
			modelBuilder.Entity<PrdStockMovement>().HasIndex(x => x.InventoryDocumentId);
			modelBuilder.Entity<PrdStockMovement>().Property(x => x.DocumentNumber).HasMaxLength(100);
			modelBuilder.Entity<PrdStockMovement>().Property(x => x.TransferNumber).HasMaxLength(100);
			modelBuilder.Entity<PrdStockMovement>().Property(x => x.CurrencyCode).HasMaxLength(3);
			modelBuilder.Entity<PrdInventoryDocument>().HasIndex(x => x.DocumentNumber).IsUnique();
			modelBuilder.Entity<PrdInventoryDocument>().Property(x => x.DocumentNumber).HasMaxLength(50);
			modelBuilder.Entity<PrdInventoryDocument>().Property(x => x.PostedUserId).HasMaxLength(450);
			modelBuilder.Entity<PrdInventoryDocument>().Property(x => x.CurrencyCode).HasMaxLength(3);
			modelBuilder.Entity<PrdInventoryDocument>().Property(x => x.SourceDocumentType).HasMaxLength(50);
			modelBuilder.Entity<PrdInventoryDocumentLine>().HasIndex(x => new { x.InventoryDocumentId, x.Sequence }).IsUnique();
			modelBuilder.Entity<PrdInventoryDocumentLine>().HasIndex(x => new { x.MaterialId, x.SourceStockLotId });
			modelBuilder.Entity<PrdInventoryDocumentLine>().Property(x => x.LotNumber).HasMaxLength(100);
			modelBuilder.Entity<PrdInventoryDocumentLine>().Property(x => x.CurrencyCode).HasMaxLength(3);
			modelBuilder.Entity<PrdRecipe>().HasIndex(x => x.Code).IsUnique();
			modelBuilder.Entity<PrdRecipe>().Property(x => x.Code).HasMaxLength(100);
			modelBuilder.Entity<PrdRecipe>().Property(x => x.Name).HasMaxLength(250);
			modelBuilder.Entity<PrdRecipeVersion>().HasIndex(x => new { x.RecipeId, x.VersionNumber }).IsUnique();
			modelBuilder.Entity<PrdRecipeItem>().HasIndex(x => new { x.RecipeVersionId, x.Sequence }).IsUnique();
			modelBuilder.Entity<PrdRecipeItem>().Property(x => x.AlternativeGroupCode).HasMaxLength(50);
			modelBuilder.Entity<PrdProductionPlanHeader>().HasIndex(x => x.PlanNumber).IsUnique();
			modelBuilder.Entity<PrdProductionPlanHeader>().Property(x => x.PlanNumber).HasMaxLength(50);
			modelBuilder.Entity<PrdProductionPlanHeader>().Property(x => x.LockedUserId).HasMaxLength(450);
			modelBuilder.Entity<PrdProductionPlan>().HasIndex(x => x.PlanNumber).IsUnique();
			modelBuilder.Entity<PrdProductionPlan>().Property(x => x.PlanNumber).HasMaxLength(50);
			modelBuilder.Entity<PrdProductionPlan>().Property(x => x.BatchNumber).HasMaxLength(100);
			modelBuilder.Entity<PrdProductionOrder>().HasIndex(x => x.OrderNumber).IsUnique();
			modelBuilder.Entity<PrdProductionOrder>().Property(x => x.OrderNumber).HasMaxLength(50);
			modelBuilder.Entity<PrdProductionOrder>().Property(x => x.BatchNumber).HasMaxLength(100);
			modelBuilder.Entity<PrdWarehouseTask>().HasIndex(x => x.TaskNumber).IsUnique();
			modelBuilder.Entity<PrdWarehouseTask>().Property(x => x.TaskNumber).HasMaxLength(50);
			modelBuilder.Entity<PrdProductionResult>().Property(x => x.BatchNumber).HasMaxLength(100);
			modelBuilder.Entity<PrdStockReservation>().HasIndex(x => new { x.MaterialId, x.WarehouseId, x.StockLotId, x.Status });

			modelBuilder.Entity<PrdMaterial>().HasOne<PrdUnit>().WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdStockLot>().HasOne<PrdMaterial>().WithMany().HasForeignKey(x => x.MaterialId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdStockLot>().HasOne<PrdWarehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdStockMovement>().HasOne<PrdMaterial>().WithMany().HasForeignKey(x => x.MaterialId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdStockMovement>().HasOne<PrdWarehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdStockMovement>().HasOne<PrdStockLot>().WithMany().HasForeignKey(x => x.StockLotId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdStockMovement>().HasOne<PrdUnit>().WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdInventoryDocument>().HasOne<PrdWarehouse>().WithMany().HasForeignKey(x => x.SourceWarehouseId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdInventoryDocument>().HasOne<PrdWarehouse>().WithMany().HasForeignKey(x => x.TargetWarehouseId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdInventoryDocument>().HasOne<PrdInventoryDocument>().WithMany().HasForeignKey(x => x.ReversalDocumentId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdInventoryDocumentLine>().HasOne<PrdInventoryDocument>().WithMany().HasForeignKey(x => x.InventoryDocumentId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdInventoryDocumentLine>().HasOne<PrdMaterial>().WithMany().HasForeignKey(x => x.MaterialId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdInventoryDocumentLine>().HasOne<PrdUnit>().WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdInventoryDocumentLine>().HasOne<PrdStockLot>().WithMany().HasForeignKey(x => x.SourceStockLotId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdInventoryDocumentLine>().HasOne<PrdStockLot>().WithMany().HasForeignKey(x => x.TargetStockLotId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdStockMovement>().HasOne<PrdInventoryDocument>().WithMany().HasForeignKey(x => x.InventoryDocumentId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdStockMovement>().HasOne<PrdInventoryDocumentLine>().WithMany().HasForeignKey(x => x.InventoryDocumentLineId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdRecipe>().HasOne<PrdMaterial>().WithMany().HasForeignKey(x => x.ProductMaterialId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdRecipeVersion>().HasOne<PrdRecipe>().WithMany().HasForeignKey(x => x.RecipeId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdRecipeVersion>().HasOne<PrdUnit>().WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdRecipeItem>().HasOne<PrdRecipeVersion>().WithMany().HasForeignKey(x => x.RecipeVersionId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdRecipeItem>().HasOne<PrdMaterial>().WithMany().HasForeignKey(x => x.MaterialId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdRecipeItem>().HasOne<PrdUnit>().WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdProductionPlan>().HasOne<PrdProductionPlanHeader>().WithMany().HasForeignKey(x => x.ProductionPlanHeaderId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdProductionPlan>().HasOne<PrdRecipeVersion>().WithMany().HasForeignKey(x => x.RecipeVersionId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdProductionPlan>().HasOne<PrdMaterial>().WithMany().HasForeignKey(x => x.ProductMaterialId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdProductionPlan>().HasOne<PrdUnit>().WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdProductionPlanRequirement>().HasIndex(x => new { x.ProductionPlanHeaderId, x.MaterialId, x.UnitId }).IsUnique();
			modelBuilder.Entity<PrdProductionPlanRequirement>().HasOne<PrdProductionPlanHeader>().WithMany().HasForeignKey(x => x.ProductionPlanHeaderId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdProductionPlanRequirement>().HasOne<PrdMaterial>().WithMany().HasForeignKey(x => x.MaterialId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdProductionPlanRequirement>().HasOne<PrdUnit>().WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdProductionOrder>().HasOne<PrdProductionPlan>().WithMany().HasForeignKey(x => x.ProductionPlanId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdProductionOrder>().HasOne<PrdRecipeVersion>().WithMany().HasForeignKey(x => x.RecipeVersionId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdProductionOrder>().HasOne<PrdMaterial>().WithMany().HasForeignKey(x => x.ProductMaterialId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdProductionOrder>().HasOne<PrdWarehouse>().WithMany().HasForeignKey(x => x.SourceWarehouseId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdProductionOrder>().HasOne<PrdWarehouse>().WithMany().HasForeignKey(x => x.ProductionWarehouseId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdProductionOrder>().HasOne<PrdUnit>().WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdMaterialRequirement>().HasOne<PrdProductionOrder>().WithMany().HasForeignKey(x => x.ProductionOrderId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdMaterialRequirement>().HasOne<PrdRecipeItem>().WithMany().HasForeignKey(x => x.RecipeItemId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdMaterialRequirement>().HasOne<PrdMaterial>().WithMany().HasForeignKey(x => x.MaterialId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdMaterialRequirement>().HasOne<PrdUnit>().WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdStockReservation>().HasOne<PrdMaterialRequirement>().WithMany().HasForeignKey(x => x.MaterialRequirementId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdStockReservation>().HasOne<PrdMaterial>().WithMany().HasForeignKey(x => x.MaterialId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdStockReservation>().HasOne<PrdWarehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdStockReservation>().HasOne<PrdStockLot>().WithMany().HasForeignKey(x => x.StockLotId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdWarehouseTask>().HasOne<PrdProductionOrder>().WithMany().HasForeignKey(x => x.ProductionOrderId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdWarehouseTask>().HasOne<PrdWarehouse>().WithMany().HasForeignKey(x => x.SourceWarehouseId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdWarehouseTask>().HasOne<PrdWarehouse>().WithMany().HasForeignKey(x => x.TargetWarehouseId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdWarehouseTaskItem>().HasOne<PrdWarehouseTask>().WithMany().HasForeignKey(x => x.WarehouseTaskId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdWarehouseTaskItem>().HasOne<PrdMaterialRequirement>().WithMany().HasForeignKey(x => x.MaterialRequirementId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdWarehouseTaskItem>().HasOne<PrdMaterial>().WithMany().HasForeignKey(x => x.MaterialId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdWarehouseTaskItem>().HasOne<PrdUnit>().WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdWarehouseTaskLot>().HasOne<PrdWarehouseTaskItem>().WithMany().HasForeignKey(x => x.WarehouseTaskItemId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdWarehouseTaskLot>().HasOne<PrdStockReservation>().WithMany().HasForeignKey(x => x.StockReservationId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdWarehouseTaskLot>().HasOne<PrdStockLot>().WithMany().HasForeignKey(x => x.StockLotId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdProductionMaterialActual>().HasOne<PrdProductionOrder>().WithMany().HasForeignKey(x => x.ProductionOrderId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdProductionMaterialActual>().HasOne<PrdMaterialRequirement>().WithMany().HasForeignKey(x => x.MaterialRequirementId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdProductionMaterialActual>().HasOne<PrdMaterial>().WithMany().HasForeignKey(x => x.MaterialId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdProductionMaterialActual>().HasOne<PrdStockLot>().WithMany().HasForeignKey(x => x.StockLotId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdProductionMaterialActual>().HasOne<PrdUnit>().WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdProductionResult>().HasOne<PrdProductionOrder>().WithMany().HasForeignKey(x => x.ProductionOrderId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdProductionResult>().HasOne<PrdMaterial>().WithMany().HasForeignKey(x => x.ProductMaterialId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdProductionResult>().HasOne<PrdWarehouse>().WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdProductionResult>().HasOne<PrdUnit>().WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PrdProductionResult>().HasOne<PrdStockLot>().WithMany().HasForeignKey(x => x.StockLotId).OnDelete(DeleteBehavior.Restrict);
		}

		private static void ConfigurePurchasingModel(ModelBuilder modelBuilder)
		{
			Type[] purchasingTypes =
			[
				typeof(PurPurchaseRequest), typeof(PurPurchaseRequestLine),
				typeof(PurRequestApprovalHistory)
			];

			foreach (var type in purchasingTypes)
			{
				modelBuilder.Entity(type).ToTable(type.Name);
				foreach (var property in modelBuilder.Entity(type).Metadata.GetProperties()
					.Where(x => x.ClrType == typeof(decimal) || x.ClrType == typeof(decimal?)))
				{
					property.SetPrecision(18);
					property.SetScale(6);
				}
			}

			modelBuilder.Entity<PurPurchaseRequest>().HasIndex(x => x.RequestNumber).IsUnique();
			modelBuilder.Entity<PurPurchaseRequest>().Property(x => x.RequestNumber).HasMaxLength(50);
			modelBuilder.Entity<PurPurchaseRequest>().Property(x => x.RequestedUserId).HasMaxLength(450);
			modelBuilder.Entity<PurPurchaseRequest>().Property(x => x.SubmittedUserId).HasMaxLength(450);
			modelBuilder.Entity<PurPurchaseRequest>().Property(x => x.Notes).HasMaxLength(1000);

			modelBuilder.Entity<PurPurchaseRequestLine>().HasIndex(x => new { x.PurchaseRequestId, x.Sequence });
			modelBuilder.Entity<PurPurchaseRequestLine>().HasIndex(x => new { x.PurchaseRequestId, x.Status });
			modelBuilder.Entity<PurPurchaseRequestLine>().Property(x => x.SourceReferenceType).HasMaxLength(50);
			modelBuilder.Entity<PurPurchaseRequestLine>().Property(x => x.Reason).HasMaxLength(500);
			modelBuilder.Entity<PurPurchaseRequestLine>().Property(x => x.ApprovedUserId).HasMaxLength(450);
			modelBuilder.Entity<PurPurchaseRequestLine>().Property(x => x.ApprovalNote).HasMaxLength(1000);

			modelBuilder.Entity<PurRequestApprovalHistory>().HasIndex(x => new { x.PurchaseRequestId, x.PurchaseRequestLineId, x.ActionDate });
			modelBuilder.Entity<PurRequestApprovalHistory>().Property(x => x.ActionUserId).HasMaxLength(450);
			modelBuilder.Entity<PurRequestApprovalHistory>().Property(x => x.Note).HasMaxLength(1000);

			modelBuilder.Entity<PurPurchaseRequestLine>().HasOne<PurPurchaseRequest>().WithMany().HasForeignKey(x => x.PurchaseRequestId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PurPurchaseRequestLine>().HasOne<PrdMaterial>().WithMany().HasForeignKey(x => x.MaterialId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PurPurchaseRequestLine>().HasOne<PrdUnit>().WithMany().HasForeignKey(x => x.UnitId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PurRequestApprovalHistory>().HasOne<PurPurchaseRequest>().WithMany().HasForeignKey(x => x.PurchaseRequestId).OnDelete(DeleteBehavior.Restrict);
			modelBuilder.Entity<PurRequestApprovalHistory>().HasOne<PurPurchaseRequestLine>().WithMany().HasForeignKey(x => x.PurchaseRequestLineId).OnDelete(DeleteBehavior.Restrict);
		}



	}
}
