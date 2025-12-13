using Ekomers.Models.Ekomers;
using AutoMapper;
using Ekomers.Models.ViewModels;
using Ekomers.Models.Entity;

namespace Ekomers.Web
{


    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
         

            CreateMap<Dosya, DosyaVM>(); 
            CreateMap<DosyaVM, Dosya>();

			CreateMap<Malzeme, MalzemelerVM>();
			CreateMap<MalzemelerVM, Malzeme>();

			CreateMap<Map, MapVM>();
			CreateMap<MapVM, Map>();			 
			
			CreateMap<PortalMenu, PortalMenuVM>();
			CreateMap<PortalMenuVM, PortalMenu>();			 

			CreateMap<TableMetadata, TableMetadataVM>();
			CreateMap<TableMetadataVM, TableMetadata>();	 


			CreateMap<Sirketler, SirketlerVM>();
			CreateMap<SirketlerVM, Sirketler>();

			CreateMap<Eczane, EczaneVM>();
			CreateMap<EczaneVM, Eczane>();


			CreateMap<Musteriler, MusterilerVM>();
			CreateMap<MusterilerVM, Musteriler>();

			CreateMap<UserShortCutFieldVM, UserShortCutField>();
			CreateMap<UserShortCutField, UserShortCutFieldVM>();

			CreateMap<YetkilendirmeVM, Yetkilendirme>();
			CreateMap<Yetkilendirme, YetkilendirmeVM>();

			CreateMap<Musteriler, MusterilerVM>();
			CreateMap<MusterilerVM, Musteriler>();

			CreateMap<Aktivite, AktiviteVM>();
			CreateMap<AktiviteVM, Aktivite>();

			CreateMap<Firsat, FirsatVM>();
			CreateMap<FirsatVM, Firsat>();

			CreateMap<Teklif, TeklifVM>();
			CreateMap<TeklifVM, Teklif>();

			CreateMap<TeklifUrunler, TeklifUrunlerVM>();
			CreateMap<TeklifUrunlerVM, TeklifUrunler>();

			CreateMap<Siparis, SiparisVM>();
			CreateMap<SiparisVM, Siparis>();

			CreateMap<SiparisUrunler, SiparisUrunlerVM>();
			CreateMap<SiparisUrunlerVM, SiparisUrunler>();

			CreateMap<SiparisIade, SiparisIadeVM>();
			CreateMap<SiparisIadeVM, SiparisIade>();

			CreateMap<SiparisIadeUrunler, SiparisIadeUrunlerVM>();
			CreateMap<SiparisIadeUrunlerVM, SiparisIadeUrunler>();


			CreateMap<Recete, ReceteVM>();
			CreateMap<ReceteVM, Recete>();

			CreateMap<ReceteUrunler, ReceteUrunlerVM>();
			CreateMap<ReceteUrunlerVM, ReceteUrunler>();

			CreateMap<Uretim, UretimVM>();
			CreateMap<UretimVM, Uretim>();

			CreateMap<Satislar, SatislarVM>();
			CreateMap<SatislarVM, Satislar>();

			CreateMap<SatislarUrunler, SatislarUrunlerVM>();
			CreateMap<SatislarUrunlerVM, SatislarUrunler>();

			CreateMap<Sozlesmeler, SozlesmelerVM>();
			CreateMap<SozlesmelerVM, Sozlesmeler>();
		}
    }
}
