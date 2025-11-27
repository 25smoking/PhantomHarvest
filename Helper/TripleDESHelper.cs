using System;
using System.IO;
using System.Security.Cryptography;

namespace PhantomHarvest.Helper
{
	// Token: 0x0200002F RID: 47
	public class TripleDESHelper
	{
		// Token: 0x0600015F RID: 351 RVA: 0x0000B224 File Offset: 0x00009424
		public static string DESCBCDecryptor(byte[] key, byte[] iv, byte[] input)
		{
			string result;
			using (TripleDESCryptoServiceProvider tripleDESCryptoServiceProvider = new TripleDESCryptoServiceProvider())
			{
				tripleDESCryptoServiceProvider.Key = key;
				tripleDESCryptoServiceProvider.IV = iv;
				tripleDESCryptoServiceProvider.Mode = CipherMode.CBC;
				tripleDESCryptoServiceProvider.Padding = PaddingMode.None;
				ICryptoTransform transform = tripleDESCryptoServiceProvider.CreateDecryptor(tripleDESCryptoServiceProvider.Key, tripleDESCryptoServiceProvider.IV);
				using (MemoryStream memoryStream = new MemoryStream(input))
				{
					using (CryptoStream cryptoStream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Read))
					{
						using (StreamReader streamReader = new StreamReader(cryptoStream))
						{
							result = streamReader.ReadToEnd();
						}
					}
				}
			}
			return result;
		}

		// Token: 0x06000160 RID: 352 RVA: 0x0000B2EC File Offset: 0x000094EC
		public static byte[] DESCBCDecryptorByte(byte[] key, byte[] iv, byte[] input)
		{
			byte[] array = new byte[512];
			using (TripleDESCryptoServiceProvider tripleDESCryptoServiceProvider = new TripleDESCryptoServiceProvider())
			{
				tripleDESCryptoServiceProvider.Key = key;
				tripleDESCryptoServiceProvider.IV = iv;
				tripleDESCryptoServiceProvider.Mode = CipherMode.CBC;
				tripleDESCryptoServiceProvider.Padding = PaddingMode.None;
				ICryptoTransform transform = tripleDESCryptoServiceProvider.CreateDecryptor(tripleDESCryptoServiceProvider.Key, tripleDESCryptoServiceProvider.IV);
				using (MemoryStream memoryStream = new MemoryStream(input))
				{
					using (CryptoStream cryptoStream = new CryptoStream(memoryStream, transform, CryptoStreamMode.Read))
					{
						cryptoStream.Read(array, 0, array.Length);
					}
				}
			}
			return array;
		}
	}
}
