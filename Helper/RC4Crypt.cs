using System;
using System.Collections.Generic;
using System.Linq;

namespace PhantomHarvest.Helper
{
	// Token: 0x0200002E RID: 46
	public static class RC4Crypt
	{
		// Token: 0x0600015B RID: 347 RVA: 0x0000B143 File Offset: 0x00009343
		public static byte[] Decrypt(byte[] key, byte[] data)
		{
			return RC4Crypt.EncryptOutput(key, data).ToArray<byte>();
		}

		// Token: 0x0600015C RID: 348 RVA: 0x0000B154 File Offset: 0x00009354
		private static byte[] EncryptInitalize(byte[] key)
		{
			byte[] array = (from i in Enumerable.Range(0, 256)
			select (byte)i).ToArray<byte>();
			int j = 0;
			int num = 0;
			while (j < 256)
			{
				num = (num + (int)key[j % key.Length] + (int)array[j] & 255);
				RC4Crypt.Swap(array, j, num);
				j++;
			}
			return array;
		}

		// Token: 0x0600015D RID: 349 RVA: 0x0000B1C8 File Offset: 0x000093C8
		private static IEnumerable<byte> EncryptOutput(byte[] key, IEnumerable<byte> data)
		{
			byte[] s = RC4Crypt.EncryptInitalize(key);
			int i = 0;
			int j = 0;
			return data.Select(delegate(byte b)
			{
				i = (i + 1 & 255);
				j = (j + (int)s[i] & 255);
				RC4Crypt.Swap(s, i, j);
				return (byte)(b ^ s[(int)(s[i] + s[j] & byte.MaxValue)]);
			});
		}

		// Token: 0x0600015E RID: 350 RVA: 0x0000B208 File Offset: 0x00009408
		private static void Swap(byte[] s, int i, int j)
		{
			byte b = s[i];
			s[i] = s[j];
			s[j] = b;
		}
	}
}
