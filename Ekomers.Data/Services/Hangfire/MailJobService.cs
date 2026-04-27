using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Mail;
using System.Text;

namespace Ekomers.Data.Services
{
	public class MailJobService : IMailJobService
	{
		public async Task SendMailAsync(string to, string subject, string body)
		{
			using var smtp = new SmtpClient("mail.holimer.com.tr")
			{
				Port = 587,
				Credentials = new NetworkCredential("portal@holimer.com.tr", "Hol*2022"),
				EnableSsl = false,
			};

			var mail = new MailMessage
			{
				From = new MailAddress("portal@holimer.com.tr"),
				Subject = subject,
				Body = body,
				IsBodyHtml = true
			};

			mail.To.Add(to); 
			try
			{
				await smtp.SendMailAsync(mail);
				Console.WriteLine("E-posta başarıyla gönderildi.");
			}
			catch (Exception ex)
			{
				Console.WriteLine("E-posta gönderilemedi: " + ex.Message);
			}
		}
	}
}
