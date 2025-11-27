using System;

namespace PhantomHarvest.Tools
{
	// Token: 0x02000005 RID: 5
	public sealed class JavaRng
	{
		// Token: 0x06000014 RID: 20 RVA: 0x00002BB8 File Offset: 0x00000DB8
		public JavaRng(long seed)
		{
			this._seed = ((seed ^ 25214903917L) & 281474976710655L);
		}

		// Token: 0x06000015 RID: 21 RVA: 0x00002BDB File Offset: 0x00000DDB
		public long nextLong()
		{
			return ((long)this.next(32) << 32) + (long)this.next(32);
		}

		// Token: 0x06000016 RID: 22 RVA: 0x00002BF4 File Offset: 0x00000DF4
		public int nextInt(int bound)
		{
			if (bound <= 0)
			{
				throw new ArgumentOutOfRangeException("bound", bound, "bound must be positive");
			}
			int num = this.next(31);
			int num2 = bound - 1;
			if ((bound & num2) == 0)
			{
				num = (int)((long)bound * (long)num >> 31);
			}
			else
			{
				int num3 = num;
				while (num3 - (num = num3 % bound) + num2 < 0)
				{
					num3 = this.next(31);
				}
			}
			return num;
		}

		// Token: 0x06000017 RID: 23 RVA: 0x00002C53 File Offset: 0x00000E53
		private int next(int bits)
		{
			this._seed = (this._seed * 25214903917L + 11L & 281474976710655L);
			return (int)(this._seed >> 48 - bits);
		}

		// Token: 0x04000003 RID: 3
		private long _seed;

		// Token: 0x04000004 RID: 4
		private const long LARGE_PRIME = 25214903917L;

		// Token: 0x04000005 RID: 5
		private const long SMALL_PRIME = 11L;
	}
}
