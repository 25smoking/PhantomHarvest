using System;

namespace PhantomHarvest.Helper
{
	// Token: 0x02000025 RID: 37
	public struct IniLine
	{
		// Token: 0x1700000B RID: 11
		// (get) Token: 0x060000BF RID: 191 RVA: 0x00009131 File Offset: 0x00007331
		public bool IsArray
		{
			get
			{
				return this.array != null;
			}
		}

		// Token: 0x04000045 RID: 69
		public LineType type;

		// Token: 0x04000046 RID: 70
		public string section;

		// Token: 0x04000047 RID: 71
		public string comment;

		// Token: 0x04000048 RID: 72
		public string key;

		// Token: 0x04000049 RID: 73
		public string value;

		// Token: 0x0400004A RID: 74
		public string[] array;

		// Token: 0x0400004B RID: 75
		public char quotechar;
	}
}
