using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Xml.Linq;
using Ekomers.Web.Services;
using Microsoft.Extensions.Options;

static void Check(bool ok,string label){if(!ok)throw new Exception("FAIL "+label);Console.WriteLine("PASS "+label);}
static string Uuid(int firm)=>firm==100?"11111111-1111-1111-1111-111111111111":"33333333-3333-3333-3333-333333333333";
static Dictionary<string,string?> Settings()=>new()
{
    ["ELogo:ServiceUrl"]="https://fixture.invalid/100",["ELogo:Username"]="TEST_HOLIMER",["ELogo:Password"]="TEST_ONLY_100",
    ["ELogoFw:ServiceUrl"]="https://fixture.invalid/103",["ELogoFw:Username"]="TEST_FW",["ELogoFw:Password"]="TEST_ONLY_103"
};
static ServiceProvider Provider(Dictionary<string,string?> settings)
{
    var configuration=new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
    return new ServiceCollection().Configure<ELogoOptions>(ELogoPostboxClient.HolimerProfile,configuration.GetSection("ELogo"))
        .Configure<ELogoOptions>(ELogoPostboxClient.FwProfile,configuration.GetSection("ELogoFw")).BuildServiceProvider();
}
using var services=Provider(Settings());using var handler=new FakePostbox();using var http=new HttpClient(handler);
var client=new ELogoPostboxClient(http,services.GetRequiredService<IOptionsMonitor<ELogoOptions>>());
foreach(var firm in new[]{100,103})
{
    var file=await client.GetIncomingInvoicePdfAsync(firm,Uuid(firm));
    Check(file.ContentType=="application/pdf"&&Encoding.ASCII.GetString(file.Content).Contains("firm="+firm),$"Firm {firm} uses its own endpoint, credentials, UUID and PDF");
}
Check(handler.Calls.Count(x=>x.Firm==100)==3&&handler.Calls.Count(x=>x.Firm==103)==3&&handler.Sessions.IsEmpty,"Each account logs in, reads its document and logs out independently");
await Task.WhenAll(Enumerable.Range(0,16).Select(async i=>{var firm=i%2==0?100:103;var file=await client.GetIncomingInvoicePdfAsync(firm,Uuid(firm));if(!Encoding.ASCII.GetString(file.Content).Contains("firm="+firm))throw new Exception("Mixed-company PDF");}));
Check(handler.Sessions.IsEmpty&&handler.Calls.Count==54,"Concurrent requests never mix account settings or sessions");
var count=handler.Calls.Count;
foreach(var firm in new[]{0,101,999})
{
    try{await client.GetIncomingInvoicePdfAsync(firm,Uuid(100));throw new Exception("Unsupported firm accepted");}
    catch(ELogoConfigurationException){Check(handler.Calls.Count==count,$"Unconfigured firm {firm} does not fall back to another account");}
}
foreach(var broken in new[]{"missing","password","endpoint"})
{
    var settings=Settings();
    if(broken=="missing")foreach(var key in settings.Keys.Where(x=>x.StartsWith("ELogoFw:")).ToList())settings.Remove(key);
    if(broken=="password")settings["ELogoFw:Password"]="";
    if(broken=="endpoint")settings["ELogoFw:ServiceUrl"]="http://fixture.invalid/103";
    using var provider=Provider(settings);var invalid=new ELogoPostboxClient(http,provider.GetRequiredService<IOptionsMonitor<ELogoOptions>>());
    try{await invalid.GetIncomingInvoicePdfAsync(103,Uuid(103));throw new Exception("Incomplete FW configuration accepted");}
    catch(ELogoConfigurationException ex){Check(handler.Calls.Count==count&&ex.Message.Contains("ELogoFw")&&!ex.Message.Contains("TEST_ONLY"),$"FW {broken} configuration fails before any network request with a safe profile-specific error");}
}
Console.WriteLine("No real credentials, external requests or production database used.");

sealed class FakePostbox:HttpMessageHandler
{
    public ConcurrentQueue<(int Firm,string Operation)> Calls {get;}=new();
    public ConcurrentDictionary<string,int> Sessions {get;}=new();
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request,CancellationToken ct)
    {
        await Task.Yield();
        var firm=int.Parse(request.RequestUri!.AbsolutePath.Trim('/'));
        var xml=XDocument.Parse(await request.Content!.ReadAsStringAsync(ct));var operation=xml.Descendants().Single(x=>x.Name.LocalName=="Body").Elements().Single().Name.LocalName;
        string Value(string name)=>xml.Descendants().FirstOrDefault(x=>x.Name.LocalName==name)?.Value??"";
        if(!request.Options.TryGetValue(IntegrationLoggingHandler.ConnectionKey,out var connection)||!connection.Contains(firm.ToString()))throw new Exception("Missing company in integration logging context");
        Calls.Enqueue((firm,operation));XElement body;
        switch(operation)
        {
            case "Login":
                if(Value("userName")!=(firm==100?"TEST_HOLIMER":"TEST_FW")||Value("passWord")!="TEST_ONLY_"+firm)throw new Exception("Wrong account credentials selected");
                var session=Guid.NewGuid().ToString("N");Sessions[session]=firm;body=new XElement("LoginResponse",new XElement("LoginResult",true),new XElement("sessionID",session));break;
            case "GetDocumentData":
                if(!Sessions.TryGetValue(Value("sessionID"),out var owner)||owner!=firm||Value("uuid")!=(firm==100?"11111111-1111-1111-1111-111111111111":"33333333-3333-3333-3333-333333333333"))throw new Exception("Wrong company session or invoice UUID");
                body=new XElement("GetDocumentDataResponse",new XElement("GetDocumentDataResult",new XElement("resultCode",1)),new XElement("document",new XElement("fileName","test.pdf"),new XElement("Value",Convert.ToBase64String(Encoding.ASCII.GetBytes("%PDF-1.4\nfirm="+firm)))));break;
            case "Logout":
                if(!Sessions.TryRemove(Value("sessionID"),out var logoutOwner)||logoutOwner!=firm)throw new Exception("Wrong company logout");
                body=new XElement("LogoutResponse");break;
            default:throw new Exception("Unexpected operation");
        }
        XNamespace soap="http://schemas.xmlsoap.org/soap/envelope/";
        return new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent(new XDocument(new XElement(soap+"Envelope",new XElement(soap+"Body",body))).ToString(),Encoding.UTF8,"text/xml")};
    }
}
