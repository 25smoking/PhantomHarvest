using System;
using System.Security.Cryptography;
using System.Text;

namespace PhantomHarvest.Helper
{
	// Token: 0x0200002D RID: 45
	public class Pbkdf2
	{
		// Token: 0x0600014E RID: 334 RVA: 0x0000AE4C File Offset: 0x0000904C
		public Pbkdf2(HMAC algorithm, byte[] password, byte[] salt, int iterations)
		{
			if (algorithm == null)
			{
				throw new ArgumentNullException("algorithm", "Algorithm cannot be null.");
			}
			this.Algorithm = algorithm;
			KeyedHashAlgorithm algorithm2 = this.Algorithm;
			if (password == null)
			{
				throw new ArgumentNullException("password", "Password cannot be null.");
			}
			algorithm2.Key = password;
			if (salt == null)
			{
				throw new ArgumentNullException("salt", "Salt cannot be null.");
			}
			this.Salt = salt;
			this.IterationCount = iterations;
			this.BlockSize = this.Algorithm.HashSize / 8;
			this.BufferBytes = new byte[this.BlockSize];
		}

		// Token: 0x0600014F RID: 335 RVA: 0x0000AEE8 File Offset: 0x000090E8
		public Pbkdf2(HMAC algorithm, byte[] password, byte[] salt) : this(algorithm, password, salt, 1000)
		{
		}

		// Token: 0x06000150 RID: 336 RVA: 0x0000AEF8 File Offset: 0x000090F8
		public Pbkdf2(HMAC algorithm, string password, string salt, int iterations) : this(algorithm, Encoding.UTF8.GetBytes(password), Encoding.UTF8.GetBytes(salt), iterations)
		{
		}

		// Token: 0x06000151 RID: 337 RVA: 0x0000AF19 File Offset: 0x00009119
		public Pbkdf2(HMAC algorithm, string password, string salt) : this(algorithm, password, salt, 1000)
		{
		}

		// Token: 0x17000024 RID: 36
		// (get) Token: 0x06000152 RID: 338 RVA: 0x0000AF29 File Offset: 0x00009129
		// (set) Token: 0x06000153 RID: 339 RVA: 0x0000AF31 File Offset: 0x00009131
		public HMAC Algorithm { get; private set; }

		// Token: 0x17000025 RID: 37
		// (get) Token: 0x06000154 RID: 340 RVA: 0x0000AF3A File Offset: 0x0000913A
		// (set) Token: 0x06000155 RID: 341 RVA: 0x0000AF42 File Offset: 0x00009142
		public byte[] Salt { get; private set; }

		// Token: 0x17000026 RID: 38
		// (get) Token: 0x06000156 RID: 342 RVA: 0x0000AF4B File Offset: 0x0000914B
		// (set) Token: 0x06000157 RID: 343 RVA: 0x0000AF53 File Offset: 0x00009153
		public int IterationCount { get; private set; }

		// Token: 0x06000158 RID: 344 RVA: 0x0000AF5C File Offset: 0x0000915C
		public byte[] GetBytes(int count)
		{
			byte[] array = new byte[count];
			int i = 0;
			int num = this.BufferEndIndex - this.BufferStartIndex;
			if (num > 0)
			{
				if (count < num)
				{
					Buffer.BlockCopy(this.BufferBytes, this.BufferStartIndex, array, 0, count);
					this.BufferStartIndex += count;
					return array;
				}
				Buffer.BlockCopy(this.BufferBytes, this.BufferStartIndex, array, 0, num);
				this.BufferStartIndex = (this.BufferEndIndex = 0);
				i += num;
			}
			while (i < count)
			{
				int num2 = count - i;
				this.BufferBytes = this.Func();
				if (num2 <= this.BlockSize)
				{
					Buffer.BlockCopy(this.BufferBytes, 0, array, i, num2);
					this.BufferStartIndex = num2;
					this.BufferEndIndex = this.BlockSize;
					return array;
				}
				Buffer.BlockCopy(this.BufferBytes, 0, array, i, this.BlockSize);
				i += this.BlockSize;
			}
			return array;
		}

		// Token: 0x06000159 RID: 345 RVA: 0x0000B040 File Offset: 0x00009240
		private byte[] Func()
		{
			byte[] array = new byte[this.Salt.Length + 4];
			Buffer.BlockCopy(this.Salt, 0, array, 0, this.Salt.Length);
			Buffer.BlockCopy(Pbkdf2.GetBytesFromInt(this.BlockIndex), 0, array, this.Salt.Length, 4);
			byte[] array2 = this.Algorithm.ComputeHash(array);
			byte[] array3 = array2;
			for (int i = 2; i <= this.IterationCount; i++)
			{
				array2 = this.Algorithm.ComputeHash(array2, 0, array2.Length);
				for (int j = 0; j < this.BlockSize; j++)
				{
					array3[j] ^= array2[j];
				}
			}
			if (this.BlockIndex == 4294967295U)
			{
				throw new InvalidOperationException("Derived key too long.");
			}
			this.BlockIndex += 1U;
			return array3;
		}

		// Token: 0x0600015A RID: 346 RVA: 0x0000B108 File Offset: 0x00009308
		private static byte[] GetBytesFromInt(uint i)
		{
			byte[] bytes = BitConverter.GetBytes(i);
			if (BitConverter.IsLittleEndian)
			{
				return new byte[]
				{
					bytes[3],
					bytes[2],
					bytes[1],
					bytes[0]
				};
			}
			return bytes;
		}

		// Token: 0x04000081 RID: 129
		private readonly int BlockSize;

		// Token: 0x04000082 RID: 130
		private uint BlockIndex = 1U;

		// Token: 0x04000083 RID: 131
		private byte[] BufferBytes;

		// Token: 0x04000084 RID: 132
		private int BufferStartIndex;

		// Token: 0x04000085 RID: 133
		private int BufferEndIndex;
	}
}
