using Ekomers.Common.Models;
using Ekomers.Common.Services.IServices;
using Microsoft.Extensions.Caching.Memory;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Ekomers.Common.Services
{
	public class TcmbService : ITcmbService
	{
		private readonly IMemoryCache _cache;

		public TcmbService(IMemoryCache cache)
		{
			_cache = cache;
		}

		public async Task<KurlarVM> DovizKuruGetir()
		{
			// cache key
			const string cacheKey = "TCMB_Kurlar";

			if (_cache.TryGetValue(cacheKey, out KurlarVM kurlar))
			{
				return kurlar; // cache’den oku
			}

			using var http = new HttpClient();
			var xmlString = await http.GetStringAsync("https://www.tcmb.gov.tr/kurlar/today.xml");

			var doc = new XmlDocument();
			doc.LoadXml(xmlString);

			// USD
			var usdNode = doc.SelectSingleNode("//Currency[@CurrencyCode='USD']");
			var usdBuying = usdNode.SelectSingleNode("ForexBuying")?.InnerText;
			var usdSelling = usdNode.SelectSingleNode("ForexSelling")?.InnerText;

			// EUR
			var eurNode = doc.SelectSingleNode("//Currency[@CurrencyCode='EUR']");
			var eurBuying = eurNode.SelectSingleNode("ForexBuying")?.InnerText;
			var eurSelling = eurNode.SelectSingleNode("ForexSelling")?.InnerText;

			kurlar = new KurlarVM
			{
				UsdAlis = usdBuying.Replace(".",","),
				UsdSatis = usdSelling.Replace(".", ","),
				EurAlis = eurBuying.Replace(".", ","),
				EurSatis = eurSelling.Replace(".", ",")
			};

			// Cache ayarı (örnek: 1 saat bellekte tut)
			_cache.Set(cacheKey, kurlar,
				new MemoryCacheEntryOptions
				{
					AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1),
					SlidingExpiration = TimeSpan.FromMinutes(20),
					Priority = CacheItemPriority.High
				});

			return kurlar;
		}


	}
}
