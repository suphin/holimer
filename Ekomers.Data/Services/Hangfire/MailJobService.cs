using Ekomers.Models.Configuration;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace Ekomers.Data.Services;

public class MailJobService : IMailJobService
{
    private readonly EmailSettings _settings;

    public MailJobService(IOptions<EmailSettings> settings) => _settings = settings.Value;

    public async Task SendMailAsync(string to, string subject, string body)
    {
        using var smtp = new SmtpClient(_settings.Host)
        {
            Port = _settings.Port,
            Credentials = new NetworkCredential(_settings.UserName, _settings.Password),
            EnableSsl = _settings.EnableSsl
        };
        using var mail = new MailMessage
        {
            From = new MailAddress(_settings.FromEmail, _settings.FromName),
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
