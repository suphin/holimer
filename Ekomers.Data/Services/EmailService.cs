using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Ekomers.Data.Services.IServices;
 
namespace Ekomers.Data.Services
{
 

    public class EmailService : IEmailService
    {
        private readonly string _fromEmail;
        private readonly string _fromPassword;
        private readonly string _smtpServer;
        private readonly int _smtpPort;

        public EmailService(string fromEmail, string fromPassword, string smtpServer, int smtpPort)
        {
            _fromEmail = fromEmail;
            _fromPassword = fromPassword;
            _smtpServer = smtpServer;
            _smtpPort = smtpPort;
        }

        public void SendEmail(string toEmail, string subject, string body)
        {
            using (var smtpClient = new SmtpClient(_smtpServer))
            {
                smtpClient.Port = _smtpPort;
                smtpClient.Credentials = new NetworkCredential(_fromEmail, _fromPassword);
                smtpClient.EnableSsl = true;

                var mailMessage = new MailMessage
                {
                    From = new MailAddress(_fromEmail),
                    Subject = subject,
                    Body = body,
                    IsBodyHtml = true,
                };

                mailMessage.To.Add(toEmail);

                try
                {
                    smtpClient.Send(mailMessage);
                    Console.WriteLine("E-posta başarıyla gönderildi.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("E-posta gönderilemedi: " + ex.Message);
                }
            }
        }
    }


}
