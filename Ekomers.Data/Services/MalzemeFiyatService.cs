using Ekomers.Data.Services.IServices;
using Ekomers.Models.Ekomers;
using Ekomers.Models.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ekomers.Data.Services
{
	public class MalzemeFiyatService : IMalzemeFiyatService
	{
		private readonly ApplicationDbContext _context;

		public MalzemeFiyatService(ApplicationDbContext context)
		{
			_context = context;
		}

		public async Task TopluFiyatGuncelleAsync(List<MalzemeFiyatGuncelleDto> model)
		{
			using var transaction = await _context.Database.BeginTransactionAsync();

			try
			{
				var malzemeIds = model.Select(x => x.MalzemeId).ToList();

				var malzemeler = await _context.Malzeme
					.Where(x => malzemeIds.Contains(x.ID))
					.ToListAsync();

				foreach (var item in model)
				{
					var malzeme = malzemeler.FirstOrDefault(x => x.ID == item.MalzemeId);
					if (malzeme == null)
						continue;

					// 1️⃣ Ana tablo güncelle
					malzeme.Fiyat = item.YeniFiyat; 
					malzeme.Maliyet = item.YeniMaliyet;
					malzeme.DovizTur = item.DovizTur;
					malzeme.SonFiyatGuncellemeTarih = DateTime.Now;
					// 2️⃣ Fiyat geçmişine ekle
					var fiyatKaydi = new MalzemeFiyat
					{
						MalzemeID = malzeme.ID,
						Fiyat = item.YeniFiyat,
						Maliyet = item.YeniMaliyet,
						DovizTur = item.DovizTur,
						Aciklama = item.Aciklama,
						Tarih = DateTime.Now
					};

					_context.MalzemeFiyat.Add(fiyatKaydi);
				}

				await _context.SaveChangesAsync();
				await transaction.CommitAsync();
			}
			catch
			{
				await transaction.RollbackAsync();
				throw;
			}
		}

		public async Task TopluMaliyetGuncelleAsync(List<MalzemeFiyatGuncelleDto> model)
		{
			using var transaction = await _context.Database.BeginTransactionAsync();

			try
			{
				var malzemeIds = model.Select(x => x.MalzemeId).ToList();

				var malzemeler = await _context.Malzeme
					.Where(x => malzemeIds.Contains(x.ID))
					.ToListAsync();

				foreach (var item in model)
				{
					var malzeme = malzemeler.FirstOrDefault(x => x.ID == item.MalzemeId);
					if (malzeme == null)
						continue;

					// 1️⃣ Ana tablo güncelle
					 
					malzeme.MaliyetSatis = item.YeniMaliyet;
					malzeme.DovizTur = item.DovizTur;
					malzeme.SonMaliyetGuncellemeTarih = DateTime.Now;
					// 2️⃣ Fiyat geçmişine ekle
					var fiyatKaydi = new MalzemeMaliyetFiyat
					{
						MalzemeID = malzeme.ID, 
						Maliyet = item.YeniMaliyet,
						DovizTur = item.DovizTur,
						Aciklama = item.Aciklama,
						Tarih = DateTime.Now
					};

					_context.MalzemeMaliyetFiyat.Add(fiyatKaydi);
				}

				await _context.SaveChangesAsync();
				await transaction.CommitAsync();
			}
			catch
			{
				await transaction.RollbackAsync();
				throw;
			}
		}
	}

}
