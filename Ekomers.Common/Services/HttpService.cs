using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace Afbel.Common.Services
{
	public static class HttpService
	{

		public static string GetResponse(string url, string data)
		{
			try
			{
				WebRequest _WebRequest = WebRequest.Create(url);
				_WebRequest.Method = "POST";

				byte[] byteArray = Encoding.UTF8.GetBytes(data);
				_WebRequest.ContentType = "application/x-www-form-urlencoded";
				_WebRequest.ContentLength = byteArray.Length;

				Stream dataStream = _WebRequest.GetRequestStream();
				dataStream.Write(byteArray, 0, byteArray.Length);
				dataStream.Close();

				WebResponse _WebResponse = _WebRequest.GetResponse();
				Console.WriteLine(((HttpWebResponse)_WebResponse).StatusDescription);
				dataStream = _WebResponse.GetResponseStream();

				StreamReader reader = new StreamReader(dataStream);
				string responseFromServer = reader.ReadToEnd();
				return responseFromServer;
			}
			catch (Exception ex)
			{

				return ex.Message;
			}

		}
		public static string GetResponse2(string url, string data)
		{
			string webPageContent = string.Empty;
			try
			{

				byte[] byteArray = Encoding.UTF8.GetBytes(data);


				WebRequest _WebRequest = WebRequest.Create(url);
				_WebRequest.Method = "POST";
				_WebRequest.ContentType = "application/x-www-form-urlencoded;charset=utf-8";
				_WebRequest.ContentLength = byteArray.Length;

				using (Stream dataStream = _WebRequest.GetRequestStream())
				{
					dataStream.Write(byteArray, 0, byteArray.Length);
				}

				using (HttpWebResponse WebResponse = (HttpWebResponse)_WebRequest.GetResponse())
				{
					using (StreamReader reader = new StreamReader(WebResponse.GetResponseStream()))
					{
						webPageContent = reader.ReadToEnd();
					}
				}

				return webPageContent;
			}
			catch (Exception ex)
			{

				return ex.Message;
			}

		}
		public static string GetResponseISO(string url, string data)
		{
			string webPageContent = string.Empty;
			try
			{

				byte[] byteArray = System.Text.Encoding.GetEncoding("ISO-8859-9").GetBytes(data);


				WebRequest _WebRequest = WebRequest.Create(url);
				_WebRequest.Method = "POST";
				_WebRequest.ContentType = "application/x-www-form-urlencoded;charset=utf-8";
				_WebRequest.ContentLength = byteArray.Length;

				using (Stream dataStream = _WebRequest.GetRequestStream())
				{
					dataStream.Write(byteArray, 0, byteArray.Length);
				}

				using (HttpWebResponse WebResponse = (HttpWebResponse)_WebRequest.GetResponse())
				{
					using (StreamReader reader = new StreamReader(WebResponse.GetResponseStream()))
					{
						webPageContent = reader.ReadToEnd();
					}
				}

				return webPageContent;
			}
			catch (Exception ex)
			{

				return ex.Message;
			}

		}
	}
}
