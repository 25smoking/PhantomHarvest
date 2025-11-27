using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace PhantomHarvest.Helper
{
	// Token: 0x02000023 RID: 35
	internal class Navicat11Cipher
	{
		// Token: 0x060000BA RID: 186 RVA: 0x00008EF8 File Offset: 0x000070F8
		protected static byte[] StringToByteArray(string hex)
		{
			return (from x in Enumerable.Range(0, hex.Length)
			where x % 2 == 0
			select Convert.ToByte(hex.Substring(x, 2), 16)).ToArray<byte>();
		}

		// Token: 0x060000BB RID: 187 RVA: 0x00008F60 File Offset: 0x00007160
		protected static void XorBytes(byte[] a, byte[] b, int len)
		{
			for (int i = 0; i < len; i++)
			{
				int num = i;
				a[num] ^= b[i];
			}
		}

		// Token: 0x060000BC RID: 188 RVA: 0x00008F88 File Offset: 0x00007188
		public Navicat11Cipher()
		{
			byte[] bytes = Encoding.UTF8.GetBytes("3DC5CA39");
			SHA1CryptoServiceProvider sha1CryptoServiceProvider = new SHA1CryptoServiceProvider();
			sha1CryptoServiceProvider.TransformFinalBlock(bytes, 0, bytes.Length);
			this.blowfishCipher = new Blowfish();
			this.blowfishCipher.InitializeKey(sha1CryptoServiceProvider.Hash);
		}

		// Token: 0x060000BD RID: 189 RVA: 0x00008FDC File Offset: 0x000071DC
		public Navicat11Cipher(string CustomUserKey)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(CustomUserKey);
			byte[] key = new SHA1CryptoServiceProvider().TransformFinalBlock(bytes, 0, 8);
			this.blowfishCipher = new Blowfish();
			this.blowfishCipher.InitializeKey(key);
		}

		// Token: 0x060000BE RID: 190 RVA: 0x00009020 File Offset: 0x00007220
		public string DecryptString(string ciphertext)
		{
			int num = 8;
			byte[] array = Navicat11Cipher.StringToByteArray(ciphertext);
			byte[] array2 = Enumerable.Repeat<byte>(byte.MaxValue, num).ToArray<byte>();
			this.blowfishCipher.BlockEncrypt(array2, 0, array2, 0);
			byte[] array3 = new byte[0];
			int num2 = array.Length / num;
			int num3 = array.Length % num;
			byte[] array4 = new byte[num];
			byte[] array5 = new byte[num];
			for (int i = 0; i < num2; i++)
			{
				Array.Copy(array, num * i, array4, 0, num);
				Array.Copy(array4, array5, num);
				this.blowfishCipher.BlockDecrypt(array4, 0, array4, 0);
				Navicat11Cipher.XorBytes(array4, array2, num);
				array3 = array3.Concat(array4).ToArray<byte>();
				Navicat11Cipher.XorBytes(array2, array5, num);
			}
			if (num3 != 0)
			{
				Array.Clear(array4, 0, array4.Length);
				Array.Copy(array, num * num2, array4, 0, num3);
				this.blowfishCipher.BlockEncrypt(array2, 0, array2, 0);
				Navicat11Cipher.XorBytes(array4, array2, num);
				array3 = array3.Concat(array4.Take(num3).ToArray<byte>()).ToArray<byte>();
			}
			return Encoding.UTF8.GetString(array3);
		}

		// Token: 0x0400003F RID: 63
		private Blowfish blowfishCipher;
	}
}
