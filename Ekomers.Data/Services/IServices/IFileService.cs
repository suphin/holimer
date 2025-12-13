using Ekomers.Models.ViewModels;  
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ekomers.Data.Services.IServices
{
    public interface IFileService
    {
        // dosya işlemleri
        /// <summary>
        /// Verilen `DosyaVM` modeline göre bir dosyayı kaydeder.
        /// </summary>
        /// <param name="Model">Kaydedilecek dosyayı temsil eden `DosyaVM` nesnesi.</param>
        /// <returns>İşlemin başarılı olup olmadığını belirten bir boolean değeri içeren bir görev döner.</returns>
        Task<bool> DosyaKaydet(DosyaVM Model);
        Task<bool> KoordinatKaydet(DosyaVM modelview);
		/// <summary>
		/// Belirtilen KayitID ve ModulID'ye göre dosya listesini getirir.
		/// </summary>
		/// <param name="KayitID">Dosyaların ait olduğu kaydın ID değeri.</param>
		/// <param name="ModulID">Dosyaların ait olduğu modülün ID değeri.</param>
		/// <returns>Dosya listesini içeren bir görev. Görev sonucu, `DosyaVM` nesnelerinin listesini içerir.</returns>
		Task<List<DosyaVM>> DosyaGetir(int KayitID, int ModulID);
        Task<List<DosyaVM>> DosyaGetir(int ModulID);

        /// <summary>
        /// Belirtilen ID'ye göre bir dosyayı siler.
        /// </summary>
        /// <param name="id">Silinecek dosyanın ID değeri.</param>
        /// <returns>İşlemin başarılı olup olmadığını belirten bir boolean değeri içeren bir görev döner.</returns>
        Task<bool> DosyaSil(int id);

        Task<bool> UploadDataToFTP(string jsonData, string fileName, string ftpUrl);
        Task<bool> UploadDataToFTP(string jsonData, string fileName, string ftpUrl,string userName, string passWord);

		Task<bool> UpdateIs360(int id,bool Is360);
	}
}
