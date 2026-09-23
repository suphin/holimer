using System;
using System.ComponentModel.DataAnnotations;

namespace Ekomers.Models.ViewModels
{
	public class LogoRestApiSettingsVM
	{
		[Required(ErrorMessage = "Sunucu adresi zorunludur.")]
		[StringLength(255)]
		[Display(Name = "Sunucu IP adresi")]
		public string ServerAddress { get; set; } = string.Empty;

		[Range(1, 65535, ErrorMessage = "Port 1 ile 65535 arasında olmalıdır.")]
		public int Port { get; set; } = 32001;

		[Required]
		public string Protocol { get; set; } = "HTTP";

		[Required(ErrorMessage = "REST kullanıcı adı zorunludur.")]
		[StringLength(200)]
		[Display(Name = "REST kullanıcı adı")]
		public string RestUserName { get; set; } = string.Empty;

		[StringLength(500)]
		[DataType(DataType.Password)]
		[Display(Name = "REST şifresi")]
		public string? RestPassword { get; set; }

		[Required(ErrorMessage = "Firma numarası zorunludur.")]
		[StringLength(20)]
		[Display(Name = "Firma no")]
		public string FirmNumber { get; set; } = string.Empty;

		[Required(ErrorMessage = "Dönem zorunludur.")]
		[StringLength(10)]
		[Display(Name = "Dönem")]
		public string PeriodNumber { get; set; } = string.Empty;

		[StringLength(200)]
		[Display(Name = "Client ID (isteğe bağlı)")]
		public string? ClientId { get; set; }

		[StringLength(500)]
		[DataType(DataType.Password)]
		[Display(Name = "Client Secret (isteğe bağlı)")]
		public string? ClientSecret { get; set; }

		public bool HasSavedPassword { get; set; }
		public bool HasSavedClientSecret { get; set; }
		public DateTime? LastTestDate { get; set; }
		public bool? LastTestSucceeded { get; set; }
		public string? LastTestMessage { get; set; }
	}
}
