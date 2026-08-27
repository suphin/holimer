using Afbel.Common.Services;
using Ekomers.Models.Configuration;
using Microsoft.Extensions.Options;
using System.Net;
using System.Net.Mail;

namespace Ekomers.Common.Services;

public class SmsSettings
{
    public string Operator { get; set; } = string.Empty;
    public string ChannelCode { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Originator { get; set; } = string.Empty;
}

public class EmailSenderService : IEmailSenderService
{
    private readonly EmailSettings _settings;

    public EmailSenderService(IOptions<EmailSettings> settings) => _settings = settings.Value;

    public async Task<bool> SendEmailAsync(string email, string subject, string message)
    {
        try
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
                Body = message,
                IsBodyHtml = true
            };
            mailMessage.To.Add(email);
            await smtpClient.SendMailAsync(mailMessage);
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"E-posta gönderme hatası: {ex.Message}");
            return false;
        }
    }
}

public class SmsSenderTuraCell : ISmsSender
{
    private readonly SmsSettings _smsSettings;

    public SmsSenderTuraCell(IOptions<SmsSettings> smsSettings) => _smsSettings = smsSettings.Value;

    public async Task<bool> SendSmsAsync(string number, string message)
    {
        var postUrl = "http://processor.smsorigin.com/xml/process.aspx";
        var sDate = DateTime.Now.ToString("ddMMyyyyHHmm");
        var eDate = DateTime.Now.AddMinutes(10).ToString("ddMMyyyyHHmm");

        var posXml = "<?xml version=\"1.0\" encoding=\"utf-8\"?>" +
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

        var responseFromServer = HttpService.GetResponse(postUrl, posXml);
        return responseFromServer.Contains("ID:");
    }
}
