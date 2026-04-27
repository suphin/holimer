using Ekomers.Models.Entity;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;
namespace Ekomers.Data.Services
{
	public class TtnService
	{
		private readonly ApplicationDbContext _context;

		public TtnService(ApplicationDbContext context)
		{
			_context = context;
		}

		public async Task<string> GenerateTtnAsync()
		{
			var today = DateTime.Today;

			var seq = await _context.TtnSequences
				.FirstOrDefaultAsync(x => x.Tarih == today);

			if (seq == null)
			{
				seq = new TtnSequence
				{
					Tarih = today,
					SonNumara = 1
				};

				_context.TtnSequences.Add(seq);
			}
			else
			{
				seq.SonNumara++;
			}

			await _context.SaveChangesAsync();

			string number = seq.SonNumara.ToString("D4"); // 0001 format
			string datePart = today.ToString("yyyyMMdd");

			return $"TTN-{datePart}-{number}";
		}
	}
}
