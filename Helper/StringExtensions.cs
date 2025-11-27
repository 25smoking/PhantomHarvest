using System;

namespace PhantomHarvest.Helper
{
	// Token: 0x02000026 RID: 38
	internal static class StringExtensions
	{
		// Token: 0x060000C0 RID: 192 RVA: 0x0000913C File Offset: 0x0000733C
		public static bool IsWhiteSpace(this string input)
		{
			for (int i = 0; i < input.Length; i++)
			{
				if (!char.IsWhiteSpace(input[i]))
				{
					return false;
				}
			}
			return true;
		}

		// Token: 0x060000C1 RID: 193 RVA: 0x00009170 File Offset: 0x00007370
		public static int CountChar(this string text, char c, int startIndex = 0, int endIndex = -1)
		{
			int num = 0;
			if (endIndex == -1)
			{
				endIndex = text.Length;
			}
			for (int i = startIndex; i < endIndex; i++)
			{
				if (text[i] == c)
				{
					num++;
				}
			}
			return num;
		}
	}
}
