using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using Microsoft.Extensions.Options;

namespace Ekomers.Web.Services;

public sealed class ELogoOptions
{
    public string ServiceUrl { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string AppName { get; set; } = "Ekomers";
    public string Source { get; set; } = "ES";
    public string Version { get; set; } = "1.0";
}

public sealed record ELogoDocumentFile(byte[] Content, string FileName, string ContentType);

public interface IELogoPostboxClient
{
    Task<ELogoDocumentFile> GetIncomingInvoicePdfAsync(string uuid, CancellationToken ct = default);
}

public sealed class ELogoPostboxClient : IELogoPostboxClient
{
    private static readonly XNamespace SoapNamespace = "http://schemas.xmlsoap.org/soap/envelope/";
    private static readonly XNamespace ServiceNamespace = "http://tempuri.org/";
    private static readonly XNamespace ContractNamespace = "http://schemas.datacontract.org/2004/07/eFaturaWebService";
    private static readonly XNamespace ArrayNamespace = "http://schemas.microsoft.com/2003/10/Serialization/Arrays";
    private const string ActionPrefix = "http://tempuri.org/IPostBoxService/";
    private const int MaximumPdfSize = 50 * 1024 * 1024;

    private readonly HttpClient _httpClient;
    private readonly ELogoOptions _options;

    public ELogoPostboxClient(HttpClient httpClient, IOptions<ELogoOptions> options)
    {
        _httpClient = httpClient;
        _options = options.Value;
    }

    public async Task<ELogoDocumentFile> GetIncomingInvoicePdfAsync(string uuid, CancellationToken ct = default)
    {
        if (!Guid.TryParse(uuid, out var parsedUuid))
            throw new InvalidOperationException("e-Fatura ETTN/UUID bilgisi geçersiz.");

        ValidateConfiguration();
        string? sessionId = null;
        try
        {
            sessionId = await LoginAsync(ct);
            var response = await SendSoapAsync(
                "GetDocumentData",
                new XElement(ServiceNamespace + "GetDocumentData",
                    new XElement(ServiceNamespace + "sessionID", sessionId),
                    new XElement(ServiceNamespace + "uuid", parsedUuid.ToString("D")),
                    new XElement(ServiceNamespace + "paramList",
                        new XElement(ArrayNamespace + "string", "DOCUMENTTYPE=EINVOICE"),
                        new XElement(ArrayNamespace + "string", "DATAFORMAT=PDF"))),
                ct);

            EnsureSuccessfulResult(response, "GetDocumentDataResult", "eLogo fatura PDF'i alınamadı");
            var document = response.Descendants().FirstOrDefault(x => x.Name.LocalName == "document")
                ?? throw new InvalidOperationException("eLogo yanıtında belge bilgisi bulunamadı.");
            var value = document.Descendants().FirstOrDefault(x => x.Name.LocalName == "Value")?.Value;
            if (string.IsNullOrWhiteSpace(value))
                throw new InvalidOperationException("eLogo yanıtında fatura dosyası bulunamadı.");

            byte[] serviceContent;
            try
            {
                serviceContent = Convert.FromBase64String(value);
            }
            catch (FormatException ex)
            {
                throw new InvalidOperationException("eLogo fatura dosyası geçerli Base64 biçiminde değil.", ex);
            }

            var responseFileName = document.Descendants().FirstOrDefault(x => x.Name.LocalName == "fileName")?.Value;
            return ExtractPdf(serviceContent, responseFileName, parsedUuid);
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(sessionId))
                await TryLogoutAsync(sessionId, ct);
        }
    }

    private async Task<string> LoginAsync(CancellationToken ct)
    {
        var response = await SendSoapAsync(
            "Login",
            new XElement(ServiceNamespace + "Login",
                new XElement(ServiceNamespace + "login",
                    new XElement(ContractNamespace + "appStr", _options.AppName),
                    new XElement(ContractNamespace + "passWord", _options.Password),
                    new XElement(ContractNamespace + "source", _options.Source),
                    new XElement(ContractNamespace + "userName", _options.Username),
                    new XElement(ContractNamespace + "version", _options.Version))),
            ct);

        var loginResult = response.Descendants().FirstOrDefault(x => x.Name.LocalName == "LoginResult")?.Value;
        var sessionId = response.Descendants().FirstOrDefault(x => x.Name.LocalName == "sessionID")?.Value;
        if (!bool.TryParse(loginResult, out var succeeded) || !succeeded || string.IsNullOrWhiteSpace(sessionId))
            throw new InvalidOperationException("eLogo oturumu açılamadı. Web servis kullanıcı adı ve şifresini kontrol ediniz.");
        return sessionId;
    }

    private async Task TryLogoutAsync(string sessionId, CancellationToken ct)
    {
        try
        {
            await SendSoapAsync(
                "Logout",
                new XElement(ServiceNamespace + "Logout",
                    new XElement(ServiceNamespace + "sessionID", sessionId)),
                ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Belge başarıyla alındıysa Logout hatası kullanıcı işlemini bozmamalıdır.
        }
    }

    private async Task<XDocument> SendSoapAsync(string operation, XElement body, CancellationToken ct)
    {
        var envelope = new XDocument(
            new XElement(SoapNamespace + "Envelope",
                new XAttribute(XNamespace.Xmlns + "soapenv", SoapNamespace),
                new XAttribute(XNamespace.Xmlns + "tem", ServiceNamespace),
                new XAttribute(XNamespace.Xmlns + "efat", ContractNamespace),
                new XAttribute(XNamespace.Xmlns + "arr", ArrayNamespace),
                new XElement(SoapNamespace + "Header"),
                new XElement(SoapNamespace + "Body", body)));

        using var request = new HttpRequestMessage(HttpMethod.Post, _options.ServiceUrl);
        request.Headers.TryAddWithoutValidation("SOAPAction", $"\"{ActionPrefix}{operation}\"");
        request.Content = new StringContent(
            envelope.ToString(SaveOptions.DisableFormatting),
            Encoding.UTF8,
            "text/xml");

        using var response = await _httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, ct);
        var responseXml = await response.Content.ReadAsStringAsync(ct);
        XDocument document;
        try
        {
            document = XDocument.Parse(responseXml);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new InvalidOperationException(
                response.IsSuccessStatusCode
                    ? "eLogo servisi geçerli bir SOAP yanıtı döndürmedi."
                    : $"eLogo servisi HTTP {(int)response.StatusCode} ({response.StatusCode}) hatası döndürdü.",
                ex);
        }

        var fault = document.Descendants().FirstOrDefault(x => x.Name.LocalName == "Fault");
        if (fault != null)
        {
            var faultText = fault.Descendants().FirstOrDefault(x => x.Name.LocalName == "faultstring")?.Value;
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(faultText)
                ? "eLogo servisi SOAP hatası döndürdü."
                : "eLogo servisi: " + faultText);
        }
        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException($"eLogo servisi HTTP {(int)response.StatusCode} ({response.StatusCode}) hatası döndürdü.");
        return document;
    }

    private static void EnsureSuccessfulResult(XDocument response, string resultElementName, string errorPrefix)
    {
        var result = response.Descendants().FirstOrDefault(x => x.Name.LocalName == resultElementName)
            ?? throw new InvalidOperationException(errorPrefix + ": sonuç bilgisi bulunamadı.");
        var resultCodeText = result.Descendants().FirstOrDefault(x => x.Name.LocalName == "resultCode")?.Value;
        var resultMessage = result.Descendants().FirstOrDefault(x => x.Name.LocalName == "resultMsg")?.Value;
        if (!int.TryParse(resultCodeText, out var resultCode) || resultCode != 1)
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(resultMessage)
                ? $"{errorPrefix} (kod: {resultCodeText ?? "bilinmiyor"})."
                : $"{errorPrefix}: {resultMessage}");
    }

    private static ELogoDocumentFile ExtractPdf(byte[] serviceContent, string? responseFileName, Guid uuid)
    {
        if (serviceContent.Length >= 4 && Encoding.ASCII.GetString(serviceContent, 0, 4) == "%PDF")
            return new ELogoDocumentFile(serviceContent, SafePdfName(responseFileName, uuid), "application/pdf");

        if (serviceContent.Length < 4 || serviceContent[0] != 0x50 || serviceContent[1] != 0x4B)
            throw new InvalidOperationException("eLogo belge içeriği PDF veya ZIP biçiminde değil.");

        using var archiveStream = new MemoryStream(serviceContent, writable: false);
        using var archive = new ZipArchive(archiveStream, ZipArchiveMode.Read, leaveOpen: false);
        var pdfEntry = archive.Entries.FirstOrDefault(x =>
            !string.IsNullOrEmpty(x.Name) && x.Name.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase));
        if (pdfEntry == null)
            throw new InvalidOperationException("eLogo ZIP dosyasında PDF bulunamadı.");
        if (pdfEntry.Length <= 0 || pdfEntry.Length > MaximumPdfSize)
            throw new InvalidOperationException("eLogo PDF dosyasının boyutu geçersiz veya izin verilen sınırı aşıyor.");

        using var pdfStream = new MemoryStream((int)pdfEntry.Length);
        using (var entryStream = pdfEntry.Open())
            entryStream.CopyTo(pdfStream);
        var pdf = pdfStream.ToArray();
        if (pdf.Length < 4 || Encoding.ASCII.GetString(pdf, 0, 4) != "%PDF")
            throw new InvalidOperationException("eLogo ZIP içindeki dosya geçerli bir PDF değil.");
        return new ELogoDocumentFile(pdf, SafePdfName(pdfEntry.Name, uuid), "application/pdf");
    }

    private static string SafePdfName(string? fileName, Guid uuid)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        if (string.IsNullOrWhiteSpace(name)) name = $"eFatura-{uuid:D}";
        var invalid = Path.GetInvalidFileNameChars();
        name = string.Concat(name.Where(x => !invalid.Contains(x)));
        return string.IsNullOrWhiteSpace(name) ? $"eFatura-{uuid:D}.pdf" : name + ".pdf";
    }

    private void ValidateConfiguration()
    {
        if (!Uri.TryCreate(_options.ServiceUrl, UriKind.Absolute, out var serviceUri) || serviceUri.Scheme != Uri.UriSchemeHttps)
            throw new InvalidOperationException("ELogo:ServiceUrl geçerli bir HTTPS adresi değil.");
        if (string.IsNullOrWhiteSpace(_options.Username) || string.IsNullOrWhiteSpace(_options.Password))
            throw new InvalidOperationException("eLogo web servis kullanıcı bilgileri tanımlı değil.");
    }
}
