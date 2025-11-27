using System;
using System.Security.Cryptography;
using System.Text;

namespace PhantomHarvest.Helper
{
	// Token: 0x02000020 RID: 32
	internal class Bcrypt
	{
		// Token: 0x06000098 RID: 152 RVA: 0x0000766C File Offset: 0x0000586C
		private byte[] BcryptHash(Blowfish blowfish, byte[] sha2pass, byte[] sha2salt)
		{
			blowfish.InitializeState();
			blowfish.ExpandState(sha2pass, sha2salt);
			for (int i = 0; i < 64; i++)
			{
				blowfish.ExpandState(sha2salt);
				blowfish.ExpandState(sha2pass);
			}
			byte[] array = (byte[])Bcrypt._bcryptCipherText.Clone();
			for (int j = 0; j < 64; j++)
			{
				for (int k = 0; k < 32; k += 8)
				{
					blowfish.BlockEncrypt(array, k, array, k);
				}
			}
			for (int l = 0; l < 32; l += 4)
			{
				byte b = array[l];
				byte b2 = array[l + 1];
				byte b3 = array[l + 2];
				byte b4 = array[l + 3];
				array[l + 3] = b;
				array[l + 2] = b2;
				array[l + 1] = b3;
				array[l] = b4;
			}
			return array;
		}

		// Token: 0x06000099 RID: 153 RVA: 0x00007724 File Offset: 0x00005924
		public byte[] BcryptPbkdf(string pass, byte[] salt, uint rounds, int keylen)
		{
			if (rounds < 1U)
			{
				return null;
			}
			if (salt.Length == 0 || keylen <= 0 || keylen > 1024)
			{
				return null;
			}
			byte[] array = new byte[keylen];
			int num = (keylen + 32 - 1) / 32;
			int num2 = (keylen + num - 1) / num;
			Blowfish blowfish = new Blowfish();
			using (SHA512CryptoServiceProvider sha512CryptoServiceProvider = new SHA512CryptoServiceProvider())
			{
				byte[] bytes = Encoding.UTF8.GetBytes(pass);
				byte[] sha2pass = sha512CryptoServiceProvider.ComputeHash(bytes);
				byte[] array2 = new byte[4];
				int num3 = 1;
				while (keylen > 0)
				{
					array2[0] = (byte)(num3 >> 24);
					array2[1] = (byte)(num3 >> 16);
					array2[2] = (byte)(num3 >> 8);
					array2[3] = (byte)num3;
					sha512CryptoServiceProvider.Initialize();
					sha512CryptoServiceProvider.TransformBlock(salt, 0, salt.Length, null, 0);
					sha512CryptoServiceProvider.TransformFinalBlock(array2, 0, array2.Length);
					byte[] sha2salt = sha512CryptoServiceProvider.Hash;
					byte[] array3 = this.BcryptHash(blowfish, sha2pass, sha2salt);
					byte[] array4 = (byte[])array3.Clone();
					for (uint num4 = rounds; num4 > 1U; num4 -= 1U)
					{
						sha512CryptoServiceProvider.Initialize();
						sha2salt = sha512CryptoServiceProvider.ComputeHash(array3);
						array3 = this.BcryptHash(blowfish, sha2pass, sha2salt);
						for (int i = 0; i < array4.Length; i++)
						{
							byte[] array5 = array4;
							int num5 = i;
							array5[num5] ^= array3[i];
						}
					}
					num2 = Math.Min(num2, keylen);
					int j;
					for (j = 0; j < num2; j++)
					{
						int num6 = j * num + (num3 - 1);
						if (num6 >= array.Length)
						{
							break;
						}
						array[num6] = array4[j];
					}
					keylen -= j;
					num3++;
				}
			}
			return array;
		}

		// Token: 0x0400002D RID: 45
		private static readonly byte[] _bcryptCipherText = Encoding.ASCII.GetBytes("OxychromaticBlowfishSwatDynamite");
	}
}
