using AutoMapper;
using Ekomers.Data.Repository.IRepository;
using Ekomers.Data.Services.IServices;
using Ekomers.Models.Ekomers;
using Ekomers.Models.Entity;
using Ekomers.Models.ViewModels;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ekomers.Data.Services
{
	public class StokService : IStokService
	{
		private readonly ApplicationDbContext _context;
		private readonly IHttpContextAccessor _httpContextAccessor;
		private readonly IWebHostEnvironment _hostingEnvironment;
		private readonly IMapper _mapper;
		private readonly ClaimsPrincipal _user;
		private readonly string _userId;

		private readonly IRepository<Kullanici> _userRepo;

		private readonly IRepository<MalzemeGrup> _grupRepo;
		private readonly IRepository<MalzemeGrup> _altGrupRepo;
		private readonly IRepository<Malzeme> _malzemeRepo;
		private readonly IRepository<MalzemeBirim> _malzemeBirimRepo;
		private readonly IRepository<MalzemeTipi> _malzemeTipiRepo;
		private readonly IRepository<MalzemeFiyat> _malzemeFiyatRepo;
		private readonly IRepository<MalzemeDepo> _depoRepo;
		private readonly IRepository<MalzemeStok> _stokRepo;
		private readonly IRepository<MalzemeHareketTur> _hareketTurRepo;
		private readonly IRepository<Departman> _departmanRepo;
		private readonly IRepository<DovizTur> _dovizTurRepo;
		 
		public StokService(IMapper mapper, ApplicationDbContext context, 
			IHttpContextAccessor httpContextAccessor, IRepository<MalzemeGrup> grupRepo,
			IRepository<Kullanici> userRepo, IRepository<Malzeme> malzemeRepo,
			IRepository<MalzemeGrup> altGrupRepo, IRepository<MalzemeBirim> malzemeBirimRepo, IRepository<MalzemeStok> stokRepo, IRepository<MalzemeHareketTur> hareketTurRepo
			, IRepository<MalzemeTipi> malzemeTipiRepo, IRepository<MalzemeFiyat> malzemeFiyatRepo, IRepository<MalzemeDepo> depoRepo, IRepository<Departman> departmanRepo,
			IRepository<DovizTur> dovizTurRepo,
			IWebHostEnvironment hostingEnvironment)
		{
			_httpContextAccessor = httpContextAccessor;
			_user = _httpContextAccessor.HttpContext.User;
			_userId = _user.FindFirst(ClaimTypes.NameIdentifier)?.Value;

			_context = context;
			_mapper = mapper;

			_grupRepo = grupRepo;
			_userRepo = userRepo;
			_malzemeRepo = malzemeRepo;
			_altGrupRepo = altGrupRepo;
			_malzemeBirimRepo = malzemeBirimRepo;
			_malzemeTipiRepo = malzemeTipiRepo;
			_malzemeFiyatRepo = malzemeFiyatRepo;
			_depoRepo = depoRepo;
			_departmanRepo = departmanRepo;
			_stokRepo = stokRepo;
			_hareketTurRepo= hareketTurRepo;
			_hostingEnvironment= hostingEnvironment;
			_dovizTurRepo = dovizTurRepo;
		}
		public IQueryable<MalzemeStokVM> GenelListe()
		{
			var result = from stok in _stokRepo.GetAll2(a => a.IsActive == true && a.IsDelete == false)

						 join malzeme in _malzemeRepo.GetAll2(a => a.IsActive == true && a.IsDelete == false) on stok.MalzemeID equals malzeme.ID
						 into malzemeGroup
						 from malzeme in malzemeGroup.DefaultIfEmpty()


						 //join altGrup in _altGrupRepo.GetAll2(a => a.IsActive == true && a.IsDelete == false) on malzeme.GrupID equals altGrup.ID
						 //into altGrupGroup
						 //from altGrup in altGrupGroup.DefaultIfEmpty()

						 //join grup in _altGrupRepo.GetAll2(a => a.IsActive == true && a.IsDelete == false) on altGrup.ParentID equals grup.ID
						 //into grupGroup
						 //from grup in grupGroup.DefaultIfEmpty()

						 join birim in _malzemeBirimRepo.GetAll2() on malzeme.BirimID equals birim.ID
						 into birimGroup
						 from birim in birimGroup.DefaultIfEmpty()

						 join tip in _malzemeTipiRepo.GetAll2() on malzeme.TipID equals tip.ID
						into tipGroup
						 from tip in tipGroup.DefaultIfEmpty()

						 join depo in _depoRepo.GetAll2() on stok.DepoID equals depo.ID
						into depoGroup
						 from depo in depoGroup.DefaultIfEmpty()

						 join departman in _departmanRepo.GetAll2() on depo.DepartmanID equals departman.ID
					   into departmanGroup
						 from departman in departmanGroup.DefaultIfEmpty()

						 join hareketTur in _hareketTurRepo.GetAll2() on stok.HareketTurID equals hareketTur.ID
					   into hareketTurGroup
						 from hareketTur in hareketTurGroup.DefaultIfEmpty()


						 join user in _userRepo.GetAll2() on malzeme.CreateUserID equals user.Id
						 into userGroup
						 from user in userGroup.DefaultIfEmpty()

						 select new MalzemeStokVM
						 {
							 ID = stok.ID,
							 MalzemeID= stok.MalzemeID,
							 MalzemeAd = malzeme.Ad ?? "",
							 MalzemeKod = malzeme.Kod ?? "",
							 Marka = malzeme.Marka ?? "",
							 Model = malzeme.Model ?? "",
							 MalzemeAciklama = stok.MalzemeAciklama ?? "",
							 LotNumara = stok.LotNumara,
							 SktTarih = stok.SktTarih,
							 //Grup = grup.Ad,
							 //AltGrup = altGrup.Ad,
							 //GrupID = grup.ID,
							 //AltGrupID = altGrup.ID,
							 BirimID = birim.ID!=null ? malzeme.BirimID:0,
							 BirimAd = birim.Ad ?? "",
							 TipAd = tip.Ad ?? "",
							 TipID = tip.ID!=null ? malzeme.TipID:0 ,
							 KritikMiktar = malzeme.KritikMiktar ?? 0,
							 CreateUserID = stok.CreateUserID,
							 CreateUserName = user != null ? user.AdSoyad : "",
							 CreateDate = stok.CreateDate ?? new DateTime(1000, 1, 1),
							 HareketTurID=stok.HareketTurID,
							 HareketTur=hareketTur.Ad,
							 GirisCikis=stok.GirisCikis,
							 DepoAd=depo.Ad,
							 DepoID=stok.DepoID,
                             GirisCikisDurum = hareketTur.GirisCikisDurum,
							 Tarih=stok.Tarih,
							 Miktar=stok.Miktar,
							 Aciklama=stok.Aciklama,
							 DepartmanID=depo.DepartmanID,
							 DepartmanAd=departman.Ad,
							 HareketTurGirisCikis=hareketTur.GirisCikisDurum


						 };
			return result;
		}

		

		public Task<MalzemeStokVM> VeriDoldurGenel(params string[] listTypes)
		{
			throw new NotImplementedException();
		}
		public bool VeriEkle(MalzemeStokVM modelv)
		{
			MalzemeHareketTur tur = _hareketTurRepo.GetById(modelv.HareketTurID);
			MalzemeStok hareket = _stokRepo.GetById(modelv.ID);
			if (hareket != null)
			{
				hareket.MalzemeID = modelv.MalzemeID;
				hareket.Miktar = modelv.Miktar;
				hareket.Aciklama = modelv.Aciklama;
				hareket.MalzemeAciklama = modelv.MalzemeAciklama;
				hareket.HareketTurID = modelv.HareketTurID;
				hareket.GirisCikis = (bool)tur.GirisCikisDurum;
				hareket.DepoID = modelv.DepoID;
				hareket.Tarih = modelv.Tarih;
				hareket.SktTarih = modelv.SktTarih;
				hareket.LotNumara = modelv.LotNumara;
				hareket.UpdateDate = DateTime.Now;
				hareket.UpdateUserID = _userId;
				_stokRepo.Update(hareket);
			}
			else
			{
				var model = new MalzemeStok
				{
					MalzemeID = modelv.MalzemeID,
					Miktar = modelv.Miktar,
					Aciklama = modelv.Aciklama,
					MalzemeAciklama = modelv.MalzemeAciklama,
					HareketTurID = modelv.HareketTurID,
					GirisCikis = (bool)tur.GirisCikisDurum,
					LotNumara = modelv.LotNumara,
					SktTarih = modelv.SktTarih,
					DepoID = modelv.DepoID,
					Tarih = modelv.Tarih,
					CreateDate = DateTime.Now,
					IsActive = true,
					IsDelete = false,
					CreateUserID = _userId
				};
				_stokRepo.Add(model);

			}
			_context.SaveChanges();

			return true;
		}
		public async Task<bool> VeriEkleCoklu(MalzemeStokVM modelv)
		{
			var newGuid = Guid.NewGuid().ToString();
			foreach (MalzemeStokVM item in modelv.MalzemeStokVMListe)
			{
				var model = new MalzemeStok
				{
					MalzemeID = item.MalzemeID,
					Miktar = item.Miktar,
					Aciklama = modelv.Aciklama,
					MalzemeAciklama=item.MalzemeAciklama,
					HareketTurID=modelv.HareketTurID,
					GirisCikis=(bool)modelv.GirisCikisDurum,
					DepoID=modelv.DepoID,
					Tarih=modelv.Tarih,
					CreateDate = DateTime.Now,
					IsActive = true,
					IsDelete = false,
					CreateUserID = _userId,
					DosyaID= newGuid,
					LotNumara=item.LotNumara,
					SktTarih=item.SktTarih
				};
				_stokRepo.Add(model);
			}

			await _context.SaveChangesAsync();
			return true;
		}
		public async Task<bool> VeriEkleTransferCoklu(MalzemeStokVM modelv)
		{
			var newGuid = Guid.NewGuid().ToString();
			foreach (MalzemeStokVM item in modelv.MalzemeStokVMListe)
			{
				var model = new MalzemeStok
				{
					MalzemeID = item.MalzemeID,
					Miktar = item.Miktar,
					Aciklama = modelv.Aciklama,
					MalzemeAciklama = item.MalzemeAciklama,
					HareketTurID = modelv.HareketTurID,
					GirisCikis = false,
					DepoID = modelv.DepoID,
					Tarih = modelv.Tarih,
					CreateDate = DateTime.Now,
					IsActive = true,
					IsDelete = false,
					CreateUserID = _userId,
					DosyaID = newGuid,
                    LotNumara = modelv.LotNumara,
                    SktTarih = modelv.SktTarih
                };
				var modelTransfer = new MalzemeStok
				{
					MalzemeID = item.MalzemeID,
					Miktar = item.Miktar,
					Aciklama = modelv.Aciklama,
					MalzemeAciklama = item.MalzemeAciklama,
					HareketTurID = modelv.HareketTurID,
					GirisCikis = true,
					DepoID = modelv.DepoTransferID,
					Tarih = modelv.Tarih,
					CreateDate = DateTime.Now,
					IsActive = true,
					IsDelete = false,
					CreateUserID = _userId,
					DosyaID = newGuid,
                    LotNumara = item.LotNumara,
                    SktTarih = item.SktTarih
                };
				_stokRepo.Add(model);
				_stokRepo.Add(modelTransfer);
			}


			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<MalzemeStokVM> VeriGetir(int HareketID)
		{
			if (HareketID <= 0)
			{
				return new MalzemeStokVM();
			}

			MalzemeStokVM hareket = GenelListe().Where(p => p.ID == HareketID).FirstOrDefault();
			if (hareket == null)
			{
				return new MalzemeStokVM();
			}

			return hareket;
		}
		public Task<List<MalzemeStokVM>> VeriListele()
		{
			throw new NotImplementedException();
		}
		public async Task<List<MalzemeStokVM>> VeriListele(MalzemeStokVM model,int departmanID)
		{
			var liste = GenelListe();

			 


			if (model.MalzemeAd != null)
			{
				liste = liste.Where(p => p.MalzemeAd.Contains(model.MalzemeAd));
			}
			if (model.MalzemeKod != null)
			{
				liste = liste.Where(p => p.MalzemeKod.Contains(model.MalzemeKod));
			}
			if (model.DepoID != 0)
			{
				liste = liste.Where(p => p.DepoID == model.DepoID);
			}
			if (model.BirimID != 0)
			{
				liste = liste.Where(p => p.BirimID == model.BirimID);
			}
			if (model.TipID != 0)
			{
				liste = liste.Where(p => p.TipID == model.TipID);
			}
			if (model.HareketTurID != 0)
			{
				liste = liste.Where(p => p.HareketTurID == model.HareketTurID);
			}

			if (_user.HasClaim(a => a.Type == ClaimTypes.Role && a.Value == "StokAdmin"))
			{
				var List = liste.OrderByDescending(a => a.ID)
										 .Take(1000)
										 .ToList();
				return List;
			}
			else
			{
				var List = liste.Where(p => p.DepartmanID == departmanID).OrderByDescending(a => a.ID)
										 .Take(1000)
										 .ToList();
				return List;
			}

			 
		}

		public async Task<List<MalzemeStokVM>> VeriListele(int departmanID)
		{
			 


			if (_user.HasClaim(a => a.Type == ClaimTypes.Role && a.Value == "StokAdmin"))
			{
				var List = GenelListe().OrderByDescending(a => a.ID)
									   .Take(1000)
									   .ToList();
				return List;
			}
			else
			{
				var List = GenelListe().Where(p => p.DepartmanID == departmanID).OrderByDescending(a => a.ID)
										 .Take(1000)
										 .ToList();
				return List;
			}
		
		}
		public Task<List<MalzemeStokVM>> VeriListele(MalzemeStokVM model)
		{
			throw new NotImplementedException();
		}

		public async Task<bool> VeriSil(int HareketID)
		{
			MalzemeStok stok = _stokRepo.GetById(HareketID);

			if (stok != null)
			{
				stok.DeleteDate = DateTime.Now;
				stok.IsDelete = true;
				stok.DeleteUserID = _userId;
				_stokRepo.Update(stok);
				await _context.SaveChangesAsync();
			}
			return true;
		}

		#region "Grup ve Alt Grup"

		
		public async Task<bool> GrupVeriEkle(MalzemeGrup Grup)
		{
			MalzemeGrup? grup = _grupRepo.GetById(Grup.ID);
			if (grup == null)
			{
				var model = new MalzemeGrup
				{
					Ad = Grup.Ad,
					Kod = Grup.Kod,
					Aciklama = Grup.Aciklama,
					ParentID = 0,
					CreateDate = DateTime.Now,
					IsActive = true,
					IsDelete = false,
					CreateUserID = _userId
				};
				_grupRepo.Add(model);
			}
			else 
			{
				grup.Ad = Grup.Ad;
				grup.Kod = Grup.Kod;
				grup.Aciklama = Grup.Aciklama;
				_grupRepo.Update(grup);
			}
			
			await _context.SaveChangesAsync();
			return true;
		}
		public async Task<bool> AltGrupVeriEkle(MalzemeGrup AltGrup)
		{
			MalzemeGrup? altgrup = _grupRepo.GetById(AltGrup.ID);
			if (altgrup == null)
			{
				var model = new MalzemeGrup
				{
					Ad = AltGrup.Ad,
					Kod = AltGrup.Kod,
					Aciklama = AltGrup.Aciklama,
					ParentID = AltGrup.ParentID,
					CreateDate = DateTime.Now,
					IsActive = true,
					IsDelete = false,
					CreateUserID = _userId
				};

				_grupRepo.Add(model);
			}
			else 
			{
				altgrup.Ad = AltGrup.Ad;
				altgrup.Kod = AltGrup.Kod;
				altgrup.Aciklama = AltGrup.Aciklama;
				_grupRepo.Update(altgrup);
			}
			
			await _context.SaveChangesAsync();
			return true;
		}
		public async Task<bool> GrupSil(int GrupID)
		{
			MalzemeGrup grup = _grupRepo.GetById(GrupID);

			if (grup != null)
			{
				grup.DeleteDate = DateTime.Now;
				grup.IsDelete = true;
				grup.DeleteUserID = _userId;
				_grupRepo.Update(grup);
				await _context.SaveChangesAsync();
			}
			return true;
		}

		public async Task<bool> AltGrupSil(int AltGrupID)
		{
			MalzemeGrup grup = _grupRepo.GetById(AltGrupID);

			if (grup != null)
			{
				grup.DeleteDate = DateTime.Now;
				grup.IsDelete = true;
				grup.DeleteUserID = _userId;
				_grupRepo.Update(grup);
				await _context.SaveChangesAsync();
			}
			return true;
		}

		public async Task<MalzemeGrup> GrupGetir(int GrupID)
		{
			var grup=await _grupRepo.GetByIdAsync(GrupID);
			return grup;
		}

		public async Task<MalzemeGrup> AltGrupGetir(int AltGrupID)
		{
			return await _grupRepo.GetByIdAsync(AltGrupID);
		}
        public async Task<List<MalzemeGrup>> KategoriListele(int KategoriID)
        {
            var list = await _grupRepo.GetAll2(p => p.IsActive == true && p.IsDelete == false && p.ParentID== KategoriID).ToListAsync();
            if (list == null)
            {
                return new List<MalzemeGrup>();
            }
            return list;
        }
       
        public async Task<List<MalzemeGrup>> GrupListeleHepsi()
		{
			var list= await _grupRepo.GetAll2(p=>p.IsActive==true && p.IsDelete==false).ToListAsync();
			if (list==null)
			{
				return new List<MalzemeGrup>();
			}
			return list;
		}
		public async Task<List<MalzemeGrup>> GrupListele()
		{
			var list = await _grupRepo.GetAll2(p => p.ParentID == 0 && p.IsActive == true && p.IsDelete == false).ToListAsync();
			if (list == null)
			{
				return new List<MalzemeGrup>();
			}
			return list;
		}
		public async Task<List<MalzemeGrup>> AltGrupListele(int GrupID)
		{
			return await _grupRepo.GetAll2(p => p.ParentID == GrupID && p.IsActive == true && p.IsDelete == false).ToListAsync();
		}
		#endregion


		#region "Malzemeler"
		public IQueryable<MalzemelerVM> MalzemeGenelListe()
		{
			var result = from malzeme in _malzemeRepo.GetAll2(a => a.IsActive == true && a.IsDelete == false)

						 join altGrup in _altGrupRepo.GetAll2(a => a.IsActive == true && a.IsDelete == false) on malzeme.GrupID equals altGrup.ID
						 into altGrupGroup
						 from altGrup in altGrupGroup.DefaultIfEmpty()

						 join grup in _altGrupRepo.GetAll2(a => a.IsActive == true && a.IsDelete == false) on altGrup.ParentID equals grup.ID
						 into grupGroup
						 from grup in grupGroup.DefaultIfEmpty()

						 join birim in _malzemeBirimRepo.GetAll2() on malzeme.BirimID equals birim.ID
						 into birimGroup
						 from birim in birimGroup.DefaultIfEmpty()

						 join doviz in _dovizTurRepo.GetAll2() on malzeme.DovizTur equals doviz.ID
						 into dovizGroup
						 from doviz in dovizGroup.DefaultIfEmpty()

						 join tip in _malzemeTipiRepo.GetAll2() on malzeme.TipID equals tip.ID
						into tipGroup
						 from tip in tipGroup.DefaultIfEmpty()


						 join user in _userRepo.GetAll2() on malzeme.CreateUserID equals user.Id
						 into userGroup
						 from user in userGroup.DefaultIfEmpty()
 

						 select new MalzemelerVM
						 {
							 ID = malzeme.ID, 
							 Ad = malzeme.Ad != null ? malzeme.Ad : "",
							 Kod = malzeme.Kod != null ? malzeme.Kod : "",
							 Marka=malzeme.Marka ?? "",
							 Model=malzeme.Model ?? "",
							 Aciklama = malzeme.Aciklama != null ? malzeme.Aciklama : "",
							 Grup=grup.Ad,
							 AltGrup=altGrup.Ad,
							 GrupID=grup.ID,
							 AltGrupID=altGrup.ID,
							 BirimID=malzeme.BirimID,
							 BirimAd=birim.Ad,
							 TipAd=tip.Ad,
							 TipID=tip.ID,							 
							 KritikMiktar=malzeme.KritikMiktar,
							 CreateUserID = malzeme.CreateUserID,
							 CreateUserName = user != null ? user.AdSoyad : "",
							 CreateDate = malzeme.CreateDate != null ? malzeme.CreateDate : new DateTime(1000, 1, 1), 
							 Fotograf = malzeme.Fotograf ?? "",
							 Fiyat=malzeme.Fiyat,
							 Kdv=malzeme.Kdv ,
							 Indirim=malzeme.Indirim,
							 DovizTur=malzeme.DovizTur,
							 DovizTurAd = doviz.Ad
						 };

		 

			return result;
		}

		public async Task<List<MalzemelerVM>> MalzemeListele()
		{
			var list = await MalzemeGenelListe().OrderBy(p=>p.Grup).ThenBy(p=>p.AltGrup).Take(1000).ToListAsync();
			return list;
		}
		public async Task<List<DovizTur>> DovizTurListele()
		{
			return await _context.DovizTur.ToListAsync();
			 
		}
		public async Task<List<MalzemelerVM>> MalzemeAra(string malzemeAd)
		{
			var list =await MalzemeGenelListe().Where(p => p.Ad.Contains(malzemeAd)).ToListAsync();
			 
			return list;
		}
		public async Task<MalzemelerVM> VeriDoldur(params string[] listTypes)
		{
			var donusModel = new MalzemelerVM();
			try
			{
				foreach (var listType in listTypes)
				{
					switch (listType)
					{
						case nameof(donusModel.MalzemeTipiListe):
							donusModel.MalzemeTipiListe = _malzemeTipiRepo.GetAll(a => a.IsActive == true && a.IsDelete == false).OrderBy(a => a.Ad).ToList();
							break;
						case nameof(donusModel.MalzemeBirimListe):
							donusModel.MalzemeBirimListe = _malzemeBirimRepo.GetAll(a => a.IsActive == true && a.IsDelete == false).OrderBy(a => a.Ad).ToList();
							break;
						case nameof(donusModel.MalzemeGrupListe):
							donusModel.MalzemeGrupListe = _grupRepo.GetAll(a => a.IsActive == true && a.IsDelete == false).OrderBy(a => a.Ad).ToList();
							break;
						case nameof(donusModel.DovizTurListe):
							donusModel.DovizTurListe = _dovizTurRepo.GetAll(a => a.IsActive == true && a.IsDelete == false).ToList();
							break;

						default:
							throw new ArgumentException($"Invalid list type: {listType}");
					}
				}
			}
			catch (Exception e)
			{


			}
			return donusModel;
		}

		public async Task<List<MalzemelerVM>> MalzemeListele(MalzemelerVM model)
		{
			var liste = MalzemeGenelListe();

			if (model.Ad != null)
			{
				liste = liste.Where(p => p.Ad.Contains(model.Ad));
			}
			if (model.Kod != null)
			{
				liste = liste.Where(p => p.Kod.Contains(model.Kod));
			}
			if (model.BirimID != 0)
			{
				liste = liste.Where(p => p.BirimID == model.BirimID);
			}
			if (model.TipID != 0)
			{
				liste = liste.Where(p => p.TipID == model.TipID);
			}
			if (model.GrupID != 0)
			{
				liste = liste.Where(p => p.GrupID == model.GrupID);
			} 

			var donus = liste.OrderByDescending(a => a.ID).OrderBy(p => p.Grup).ThenBy(p => p.AltGrup).Take(1000).ToList();
			return donus;
		}

		public async Task<bool> MalzemeEkle(MalzemelerVM modelv)
		{
			Malzeme malzeme = _malzemeRepo.GetById(modelv.ID);
			if (malzeme == null)
			{
				var model = new Malzeme
				{
					Ad = modelv.Ad,
					Kod = modelv.Kod,
					Aciklama = modelv.Aciklama, 
					BirimID= modelv.BirimID,
					TipID= modelv.TipID,
					GrupID= (int)modelv.GrupID,
					KritikMiktar = modelv.KritikMiktar,
					Marka= modelv.Marka,
					Model= modelv.Model,
					CreateDate = DateTime.Now,
					IsActive = true,
					IsDelete = false,
					CreateUserID = _userId,
					Fiyat = modelv.Fiyat,
					Kdv = modelv.Kdv,
					Indirim = modelv.Indirim,
					DovizTur= modelv.DovizTur
				};
				_malzemeRepo.Add(model);
			}
			else
			{
				 
				malzeme.Ad = modelv.Ad;
				malzeme.Kod = modelv.Kod;
				malzeme.Aciklama = modelv.Aciklama;
				malzeme.BirimID = modelv.BirimID;
				malzeme.TipID = modelv.TipID;
				malzeme.GrupID = (int)modelv.GrupID;
				malzeme.KritikMiktar = modelv.KritikMiktar;
				malzeme.Marka = modelv.Marka;
				malzeme.Model = modelv.Model;
				malzeme.Fiyat = modelv.Fiyat;
				malzeme.Kdv = modelv.Kdv;
				malzeme.Indirim = modelv.Indirim;
				malzeme.DovizTur = modelv.DovizTur;
				 
				_malzemeRepo.Update(malzeme);
			}

			await _context.SaveChangesAsync();
			return true;
		}
		public async Task<bool> MalzemeSil(int MalzemeID)
		{
			Malzeme malzeme = _malzemeRepo.GetById(MalzemeID);

			if (malzeme != null)
			{
				malzeme.DeleteDate = DateTime.Now;
				malzeme.IsDelete = true;
				malzeme.DeleteUserID = _userId;
				_malzemeRepo.Update(malzeme);
				await _context.SaveChangesAsync();
			}
			return true;
		}
		public async Task<MalzemelerVM> MalzemeGetir(int MalzemeID)
		{
			if (MalzemeID <= 0)
			{
				return new MalzemelerVM();
			}

			MalzemelerVM kayit = MalzemeGenelListe().Where(p => p.ID == MalzemeID).FirstOrDefault();
			if (kayit == null)
			{
				return new MalzemelerVM();
			}

			return kayit;
		}

		public async Task<MalzemelerVM> MalzemeKodlaGetir(string MalzemeKod)
		{
			if (MalzemeKod ==null)
			{
				return new MalzemelerVM();
			}

			MalzemelerVM kayit = MalzemeGenelListe().Where(p => p.Kod == MalzemeKod).FirstOrDefault();
			if (kayit == null)
			{
				return new MalzemelerVM();
			}

			return kayit;
		}
		public async Task<List<MalzemelerVM>> DepoDurumu(int departmanID)
		{
			//IQueryable<MalzemeDepo> depoListesi;
			
			//if (_user.HasClaim(a => a.Type == ClaimTypes.Role && a.Value == "StokAdmin"))
			//{
			//	 depoListesi = _depoRepo.GetAll2(d => d.IsActive == true && d.IsDelete == false); // Depo bilgileri
			//}
			//else
			//{
			//	 depoListesi = _depoRepo.GetAll2(d => d.IsActive == true && d.IsDelete == false && d.DepartmanID==departmanID); // Depo bilgileri
			//}

			var stokHareketListesi = _stokRepo.GetAll2(a => a.IsActive == true && a.IsDelete == false);
			var malzemeListesi = MalzemeGenelListe();
			var depoListesi = _depoRepo.GetAll2(d => d.IsActive == true && d.IsDelete == false);

			var sonuc = (from malz in malzemeListesi
						 join stok in stokHareketListesi on malz.ID equals stok.MalzemeID into gj
						 from subYg in gj.DefaultIfEmpty()

						 join depo in depoListesi on subYg.DepoID equals depo.ID into depoJoin
						 from depoSub in depoJoin.DefaultIfEmpty()

						 group subYg by new { malz.ID, malz.Ad, malz.Kod, malz.KritikMiktar, DepoAd=depoSub.Ad,depoSub.DepartmanID } into grouped

						 

						 select new MalzemelerVM
						 {
							 Ad = grouped.Key.Ad,
							 ID = grouped.Key.ID,
							 Kod = grouped.Key.Kod,
							 DepoAd = grouped.Key.DepoAd, // Depo adı eklendi
							 DepartmanID = grouped.Key.DepartmanID,
							 KritikMiktar = grouped.Key.KritikMiktar,
							 GirenMiktar = grouped.Where(y => y != null && y.GirisCikis == true).Sum(y => y.Miktar),
							 CikanMiktar = grouped.Where(y => y != null && y.GirisCikis == false).Sum(y => y.Miktar),
							 KalanMiktar = (grouped.Where(y => y != null && y.GirisCikis == true).Sum(y => y.Miktar)) -
										   (grouped.Where(y => y != null && y.GirisCikis == false).Sum(y => y.Miktar))
						 });

			if (_user.HasClaim(a => a.Type == ClaimTypes.Role && a.Value == "StokAdmin"))
			{
				var list = sonuc.Where(p => p.GirenMiktar > 0 || p.CikanMiktar > 0).ToList();
				return list;
			}
			else
			{
				var list = sonuc.Where(p => (p.GirenMiktar > 0 || p.CikanMiktar > 0) && p.DepartmanID==departmanID).ToList();
				return list;
			}

			 
		}
		public async Task<List<MalzemelerVM>> DepoDurumu(MalzemelerVM modelv,int departmanID)
		{
			 

			var stokHareketListesi = _stokRepo.GetAll2(a => a.IsActive == true && a.IsDelete == false);
			var malzemeListesi = MalzemeGenelListe();
			var depoListesi = _depoRepo.GetAll2(d => d.IsActive == true && d.IsDelete == false); // Depo bilgileri

			 

			var sonuc = (from malz in malzemeListesi
						 join stok in stokHareketListesi on malz.ID equals stok.MalzemeID into gj
						 from subYg in gj.DefaultIfEmpty()

						 join depo in depoListesi on subYg.DepoID equals depo.ID into depoJoin
						 from depoSub in depoJoin.DefaultIfEmpty()

						 group subYg by new { malz.ID, malz.Ad, malz.Kod, malz.KritikMiktar, DepoAd = depoSub.Ad,DepoID=depoSub.ID, depoSub.DepartmanID } into grouped



						 select new MalzemelerVM
						 {
							 Ad = grouped.Key.Ad,
							 ID = grouped.Key.ID,
							 Kod = grouped.Key.Kod,
							 DepoAd = grouped.Key.DepoAd, // Depo adı eklendi
							 DepoID = grouped.Key.DepoID, // Depo adı eklendi
							 DepartmanID = grouped.Key.DepartmanID,
							 KritikMiktar = grouped.Key.KritikMiktar,
							 GirenMiktar = grouped.Where(y => y != null && y.GirisCikis == true).Sum(y => y.Miktar),
							 CikanMiktar = grouped.Where(y => y != null && y.GirisCikis == false).Sum(y => y.Miktar),
							 KalanMiktar = (grouped.Where(y => y != null && y.GirisCikis == true).Sum(y => y.Miktar)) -
										   (grouped.Where(y => y != null && y.GirisCikis == false).Sum(y => y.Miktar))
						 });

			if (modelv.Ad != null)
			{
				sonuc = sonuc.Where(p => p.Ad.Contains(modelv.Ad));
			}
			if (modelv.Kod != null)
			{
				sonuc = sonuc.Where(p => p.Kod.Contains(modelv.Kod));
			}
			if (modelv.DepoID != 0)
			{
				sonuc = sonuc.Where(p => p.DepoID == modelv.DepoID);
			}

			 


			if (_user.HasClaim(a => a.Type == ClaimTypes.Role && a.Value == "StokAdmin"))
			{
				var list = sonuc.Where(p => p.GirenMiktar > 0 || p.CikanMiktar > 0).ToList();
				return list;
			}
			else
			{
				var list = sonuc.Where(p => (p.GirenMiktar > 0 || p.CikanMiktar > 0) && p.DepartmanID == departmanID).ToList();
				return list;
			}
			 
		}
		public void FotoYukle(MalzemelerVM model)
		{
			var urun = _malzemeRepo.GetById(model.ID);
			if (urun != null)
			{
				urun.Fotograf = model.Fotograf;
				_malzemeRepo.Update(urun);
				_context.SaveChanges();
			}

		}

		public async Task<List<MalzemelerVM>> KategoriMalzemeListele(int KategoriID)
		{
			var list = await MalzemeGenelListe().Where(p => p.AltGrupID==KategoriID).ToListAsync();
			//var list = await MalzemeGenelListe().ToListAsync();

			return list;
		}

		public async Task<List<MalzemeFiyat>> FiyatGetir(int UrunID)
		{
			try
			{
				List<MalzemeFiyat> fiyatlar = await _malzemeFiyatRepo.GetAll2().Where(p => p.MalzemeID == UrunID).ToListAsync();
				if (fiyatlar == null)
				{
					return new List<MalzemeFiyat>();
				}
				return fiyatlar;
			}
			catch (Exception ex)
			{

				return new List<MalzemeFiyat>(); ;
			}
		}

		public async Task<bool> FiyatDegistir(MalzemeFiyat model)
		{
			try
			{
				var urun = await _malzemeRepo.GetByIdAsync(model.ID);
				if (urun != null)
				{
					urun.Fiyat = model.Fiyat;
					urun.Maliyet = model.Maliyet;
					urun.DovizTur = model.DovizTur;
					_malzemeRepo.Update(urun);
					await _context.SaveChangesAsync();
				}
				var fiyat = new MalzemeFiyat();
				fiyat.Aciklama = model.Aciklama;
				fiyat.MalzemeID = model.ID;
				fiyat.Fiyat = model.Fiyat;
				fiyat.Maliyet = model.Maliyet;
				fiyat.CreateDate = DateTime.Now;
				fiyat.Tarih = DateTime.Now;
				fiyat.IsActive = true;
				fiyat.IsDelete = false;
				fiyat.CreateUserID = _userId;
				fiyat.DovizTur = model.DovizTur;

				_malzemeFiyatRepo.Update(fiyat);
				await _context.SaveChangesAsync();
				return true;
			}
			catch (Exception ex)
			{

				return false;
			}
		}

		#endregion

		#region "Depolar"
		public IQueryable<MalzemeDepoVM> DepoGenelListe()
		{
			var result = from depo in _depoRepo.GetAll2(a => a.IsActive == true && a.IsDelete == false)

						 join departman in _departmanRepo.GetAll2(a => a.IsActive == true && a.IsDelete == false) on depo.DepartmanID equals departman.ID
						 into departmanGroup
						 from departman in departmanGroup.DefaultIfEmpty()

						 join user in _userRepo.GetAll2() on depo.CreateUserID equals user.Id
						 into userGroup
						 from user in userGroup.DefaultIfEmpty()

						 select new MalzemeDepoVM
						 {
							 ID = depo.ID,
							 Ad = depo.Ad ?? "",
							 Kod = depo.Kod ?? "",
							 Aciklama = depo.Aciklama ?? "",
							 Departman= departman !=null?departman.Ad:"",
							 DepartmanID = depo.DepartmanID,
							 Adres = depo.Adres ?? "",
							 Ozellik = depo.Ozellik ??"",
							 CreateUserID = depo.CreateUserID,
							 CreateUserName = user != null ? user.AdSoyad : "",
							 CreateDate = depo.CreateDate != null ? depo.CreateDate : new DateTime(1000, 1, 1),
							 
						 };
			return result;
		}
		public async Task<List<MalzemeDepoVM>> DepoListele(int departmanID)
		{
			if (_user.IsInRole("Admin"))
			{
				var List = await DepoGenelListe().OrderBy(p => p.Departman).Take(1000).ToListAsync();
				return List;
			}
			else
			{
				var List = await DepoGenelListe().Where(a=>a.DepartmanID==departmanID).OrderBy(p => p.Departman).Take(1000).ToListAsync();
				return List;
			}
			//var list = await DepoGenelListe().OrderBy(p => p.Departman).Take(1000).ToListAsync();
			//return list;
		}
		public async Task<List<MalzemeDepoVM>> DepoListele()
		{
			 
				var List = await DepoGenelListe().OrderBy(p => p.Departman).Take(1000).ToListAsync();
				return List; 
		}
		public async Task<List<Departman>> DepartmanListele()
		{
			return await _departmanRepo.GetAll2(a => a.IsActive == true && a.IsDelete == false && a.ID!=1).OrderBy(p=>p.Ad).ToListAsync();
		}

		

		public async Task<bool> DepoEkle(MalzemeDepoVM modelv)
		{
			MalzemeDepo depo = _depoRepo.GetById(modelv.ID);
			if (depo == null)
			{
				var model = new MalzemeDepo
				{
					Ad = modelv.Ad,
					Kod = modelv.Kod,
					Aciklama = modelv.Aciklama,
					Adres = modelv.Adres,
					Ozellik = modelv.Ozellik,
					DepartmanID= modelv.DepartmanID,
					CreateDate = DateTime.Now,
					IsActive = true,
					IsDelete = false,
					CreateUserID = _userId
				};
				_depoRepo.Add(model);
			}
			else
			{

				depo.Ad = modelv.Ad;
				depo.Kod = modelv.Kod;
				depo.Aciklama = modelv.Aciklama; 
				depo.Adres = modelv.Adres;
				depo.Ozellik = modelv.Ozellik;
				depo.DepartmanID = modelv.DepartmanID;
				_depoRepo.Update(depo);
			}

			await _context.SaveChangesAsync();
			return true;
		}

		public async Task<bool> DepoSil(int DepoID)
		{
			MalzemeDepo depo = _depoRepo.GetById(DepoID);
			if (depo != null)
			{
				depo.DeleteDate = DateTime.Now;
				depo.IsDelete = true;
				depo.DeleteUserID = _userId;
				_depoRepo.Update(depo);
				await _context.SaveChangesAsync();
			}
			return true;
		}
		  
		public async Task<MalzemeDepoVM> DepoGetir(int DepoID)
		{
			if (DepoID <= 0)
			{
				return new MalzemeDepoVM();
			}

			MalzemeDepoVM kayit = DepoGenelListe().Where(p => p.ID == DepoID).FirstOrDefault();
			if (kayit == null)
			{
				return new MalzemeDepoVM();
			}

			return kayit;
		}

		public async Task<List<MalzemeDepoVM>> DepoListele(MalzemeDepoVM modelv)
		{
			var liste = DepoGenelListe();

			if (modelv.Ad != null)
			{
				liste = liste.Where(p => p.Ad.Contains(modelv.Ad));
			}
			if (modelv.Kod != null)
			{
				liste = liste.Where(p => p.Kod.Contains(modelv.Kod));
			}
			if (modelv.DepartmanID != 0)
			{
				liste = liste.Where(p => p.DepartmanID == modelv.DepartmanID);
			}		 

			var donus = liste.OrderByDescending(a => a.ID).OrderBy(p => p.Departman).Take(1000).ToList();
			return donus;
		}

		public async Task<List<MalzemeHareketTur>> HareketListele()
		{
			return await _hareketTurRepo.GetAll2(a => a.IsActive == true && a.IsDelete == false).ToListAsync();
		}





        #endregion


        #region "TreeView Recursive"
     
        public List<KategoriTreeItem> GetKategoriTree()
        {
            var kategoriler = _grupRepo.GetAll2(p => p.IsActive == true && p.IsDelete == false).ToList();
            return BuildKategoriTree(kategoriler, 0);
        }
       
        public List<KategoriTreeItem> BuildKategoriTree(List<MalzemeGrup> kategoriler, int? parentId)
        {
            var result = new List<KategoriTreeItem>();

            // ParentID'ye göre filtreleme
            var filteredKategoriler = kategoriler.Where(k => k.ParentID == parentId).ToList();

            foreach (var kategori in filteredKategoriler)
            {
                var item = new KategoriTreeItem
                {
                    ID = kategori.ID,
                    Ad = kategori.Ad,
                    Kod = kategori.Kod,
					ParentID=(int)kategori.ParentID,
                    Children = BuildKategoriTree(kategoriler, kategori.ID)  // Recursive çağrı
                };

                result.Add(item);
            }

            return result;
        }

		 public List<KategoriTreeItem> GetKategoriTree(string arama)
        {
            var kategoriler = _grupRepo.GetAll2(p => p.IsActive == true && p.IsDelete == false).ToList();
            return BuildKategoriTree(kategoriler, 0, arama);
        }

        public List<KategoriTreeItem> BuildKategoriTree(List<MalzemeGrup> kategoriler, int? parentId, string arama)
        {
            var result = new List<KategoriTreeItem>();

            // ParentID'ye göre filtreleme
            var filteredKategoriler = kategoriler.Where(k => k.ParentID == parentId).ToList();

            foreach (var kategori in filteredKategoriler)
            {
                // Alt kategorilerde arama yaparak hiyerarşiyi koru
                var children = BuildKategoriTree(kategoriler, kategori.ID);

                // Eğer kategori adı arama terimini içeriyorsa veya alt kategorilerde bir sonuç varsa
                if (kategori.Ad.Contains(arama) || children.Count > 0)
                {
                    var item = new KategoriTreeItem
                    {
                        ID = kategori.ID,
                        Ad = kategori.Ad,
                        Kod = kategori.Kod,
                        ParentID = (int)kategori.ParentID,
                        Children = children // Recursive çağrıdan dönen alt kategoriler
                    };

                    result.Add(item);
                }
            }

            return result;
        }

		
		#endregion
	}
}
