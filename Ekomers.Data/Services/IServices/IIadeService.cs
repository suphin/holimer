using Ekomers.Models.Ekomers;
using Ekomers.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Data.Services.IServices
{
	 
	public interface IIadeService : IGenelService<MalzemeIadeVM, MalzemeIade>
	{
		Task<bool> VeriEkleCoklu(MalzemeIadeVM modelv);
		Task<bool> VeriEkleTransferCoklu(MalzemeIadeVM modelv);
		Task<bool> GrupVeriEkle(MalzemeGrup Grup);
		Task<bool> GrupSil(int GrupID);
		Task<MalzemeGrup> GrupGetir(int GrupID);
		Task<List<MalzemeGrup>> GrupListele();
		Task<List<MalzemeGrup>> GrupListeleHepsi();
		Task<List<MalzemeGrup>> KategoriListele(int KategoriID);
		Task<List<MalzemelerVM>> KategoriMalzemeListele(int KategoriID);
		Task<bool> AltGrupVeriEkle(MalzemeGrup AltGrup);
		Task<bool> AltGrupSil(int AltGrupID);
		Task<MalzemeGrup> AltGrupGetir(int AltGrupID);
		Task<List<MalzemeGrup>> AltGrupListele(int GrupID);

		Task<List<MalzemelerVM>> MalzemeListele();
		Task<List<MalzemelerVM>> MalzemeAra(string malzemeAd);
		Task<List<MalzemelerVM>> MalzemeListele(MalzemelerVM modelv);
		Task<bool> MalzemeEkle(MalzemelerVM modelv);
		Task<MalzemelerVM> MalzemeGetir(int MalzemeID);
		Task<MalzemelerVM> MalzemeKodlaGetir(string MalzemeKod);
		Task<bool> MalzemeSil(int MalzemeID);
		void FotoYukle(MalzemelerVM model);
		Task<MalzemelerVM> VeriDoldur(params string[] listTypes);
		Task<List<MalzemelerVM>> DepoDurumu(int departmanID);
		Task<List<MalzemelerVM>> DepoDurumu(MalzemelerVM modelv, int departmanID);



		Task<bool> DepoEkle(MalzemeDepoVM modelv);
		Task<bool> DepoSil(int DepoID);
		Task<MalzemeDepoVM> DepoGetir(int DepoID);
		Task<List<MalzemeDepoVM>> DepoListele();
		Task<List<MalzemeDepoVM>> DepoListele(int departmanID);
		Task<List<MalzemeDepoVM>> DepoListele(MalzemeDepoVM modelv);
		Task<List<MalzemeIadeVM>> VeriListele(MalzemeIadeVM model, int departmanID);
		Task<List<Departman>> DepartmanListele();

		Task<List<MalzemeHareketTur>> HareketListele();
		Task<List<DovizTur>> DovizTurListele();


		Task<List<MalzemeIadeVM>> VeriListele(int departmanID);


		//treeview
		List<KategoriTreeItem> GetKategoriTree();
		List<KategoriTreeItem> GetKategoriTree(string arama);

		List<KategoriTreeItem> BuildKategoriTree(List<MalzemeGrup> kategoriler, int? parentId);
		List<KategoriTreeItem> BuildKategoriTree(List<MalzemeGrup> kategoriler, int? parentId, string arama);


		Task<List<MalzemeFiyat>> FiyatGetir(int UrunID);
		Task<bool> FiyatDegistir(MalzemeFiyat model);
	}
}
