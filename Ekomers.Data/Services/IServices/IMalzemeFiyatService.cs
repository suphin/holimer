using Ekomers.Models.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace Ekomers.Data.Services.IServices
{
	public interface IMalzemeFiyatService
	{
		Task TopluFiyatGuncelleAsync(List<MalzemeFiyatGuncelleDto> model);
		Task TopluMaliyetGuncelleAsync(List<MalzemeFiyatGuncelleDto> model);
	}
}
