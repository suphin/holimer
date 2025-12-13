using Afbel.Common.Services;
using CMFCell;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;
namespace Ekomers.Common.Services;

 
public class SmsSettings
{

	public string Operator { get; set; }
	public string ChannelCode { get; set; }
	public string UserName { get; set; }
	public string Password { get; set; }
	public string Originator { get; set; }
}
public class EmailSenderService : IEmailSenderService
{
    //public Task SendEmailAsync(string email, string subject, string message)
    //{
    //    // Plug in your email service here to send an email.
    //    return Task.FromResult(0);
    //}

	public async Task<bool> SendEmailAsync(string email, string subject, string message)
	{
		try
		{
			var smtpClient = new SmtpClient("mail.holimer.com.tr") // Örn: smtp.gmail.com
			{
				Port = 587, // Gmail için 587 (TLS) veya 465 (SSL)
				Credentials = new NetworkCredential("portal@holimer.com.tr", "Hol*2022"),
				EnableSsl = false,
				Host = "mail.holimer.com.tr"
			};

			var mailMessage = new MailMessage
			{
				From = new MailAddress("portal@holimer.com.tr","Holimer İlaç A.Ş. Portal"),
				Subject = subject,
				Body = message,
				IsBodyHtml = true, // HTML desteği için
			};

			mailMessage.To.Add(email);
			await smtpClient.SendMailAsync(mailMessage);
			return true;
		}
		catch (Exception ex)
		{
			// Hata loglama
			Console.WriteLine($"E-posta gönderme hatası: {ex.Message}");
			return false;
		}
	}

}
public class SmsSenderTuraCell : ISmsSender
{
	private readonly SmsSettings _smsSettings;
	public SmsSenderTuraCell(IOptions<SmsSettings> smsSettings)
	{
		_smsSettings = smsSettings.Value;
	}
	public async Task<bool> SendSmsAsync(string number, string message)
	{
		var postUrl = "http://processor.smsorigin.com/xml/process.aspx";
		// Sistem saatini al ve formatla
		var sDate = DateTime.Now.ToString("ddMMyyyyHHmm"); // Şu anki tarih ve saat
		var eDate = DateTime.Now.AddMinutes(10).ToString("ddMMyyyyHHmm"); // 10 dakika eklenmiş tarih ve saat


		var PosXML = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
				"<MainmsgBody>" +
				"<Command>0</Command>" +
				"<PlatformID>1</PlatformID>" +
				"<ChannelCode>" + _smsSettings.ChannelCode + "</ChannelCode>" +
				"<UserName>" + _smsSettings.UserName + "</UserName>" +
				"<PassWord>" + _smsSettings.Password + "</PassWord>" +
				"<Type>1</Type>" +
				"<Concat>0</Concat>" +
				"<Originator>" + _smsSettings.Originator + "</Originator>" +
				"<Mesgbody>" + message + "</Mesgbody>" +
				"<Numbers>9" + number + "</Numbers>" +
				"<SDate>" + sDate + "</SDate>" +
				 "<EDate>" + eDate + "</EDate>" +
				"</MainmsgBody>";

		String responseFromServer = HttpService.GetResponse(postUrl, PosXML);

		if (responseFromServer.Contains("ID:"))
		{
			return true;
		}
		return false;
	}
	public bool SmsSend(string phone, string messagee)
	{
		string username = "5010-5****";
		string password = "O****";
		string from = "OR******";

		DateTime? sendDate = null;
		TimeSpan? validityPeriod = null;

		SmsMessage _message = new SmsMessage();
		_message.Message = messagee;
		_message.Recipients.Add("+90" + phone);

		_message.Originator = from;
		_message.SendDate = sendDate;
		_message.ValidityPeriod = validityPeriod;

		SmsMessage _sentMessage = null;
		try
		{
			Uri uri = new Uri("https://service6.mobilpark.biz/xml/default.aspx");
			_sentMessage = _message.Send(uri, username, password);
			return true;
		}
		catch (Exception ex)
		{
			return false;
		}

	}
}