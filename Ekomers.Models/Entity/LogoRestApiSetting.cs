using System;

namespace Ekomers.Models.Entity
{
	public class LogoRestApiSetting
	{
		public int ID { get; set; }
		public string ServerAddress { get; set; } = string.Empty;
		public int Port { get; set; }
		public string Protocol { get; set; } = "HTTP";
		public string RestUserName { get; set; } = string.Empty;
		public string RestPasswordProtected { get; set; } = string.Empty;
		public string FirmNumber { get; set; } = string.Empty;
		public string PeriodNumber { get; set; } = string.Empty;
		public string? ClientId { get; set; }
		public string? ClientSecretProtected { get; set; }
		public DateTime? LastTestDate { get; set; }
		public bool? LastTestSucceeded { get; set; }
		public string? LastTestMessage { get; set; }
		public DateTime UpdatedAt { get; set; }
		public string? UpdatedBy { get; set; }
	}
}
