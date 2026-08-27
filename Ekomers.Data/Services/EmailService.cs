using Ekomers.Data.Services.IServices;
using Ekomers.Models.Configuration;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace Ekomers.Data.Services;

public class EmailService : IEmailService
{
    private readonly EmailSettings _settings;

    public EmailService(IOptions<EmailSettings> settings) => _settings = settings.Value;

    public void SendEmail(string toEmail, string subject, string body)
    {
        using var smtpClient = new SmtpClient(_settings.Host)
        {
            Port = _settings.Port,
            Credentials = new NetworkCredential(_settings.UserName, _settings.Password),
            EnableSsl = _settings.EnableSsl
        };
        using var mailMessage = new MailMessage
        {
            From = new MailAddress(_settings.FromEmail, _settings.FromName),
            Subject = subject,
            Body = body,
            IsBodyHtml = true
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
