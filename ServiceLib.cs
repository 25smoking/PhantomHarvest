using System;
using System.IO;
using System.Reflection;

namespace PhantomHarvest
{
    public class ServiceLib
    {
        /// <summary>
        /// DLL 导出方法，用于反射加载执行
        /// </summary>
        /// <param name="args">命令行参数字符串</param>
        public static void Execute(string args)
        {
            string[] arguments = string.IsNullOrEmpty(args) ? new string[0] : args.Split(' ');
            
            // 调用 Program.Main
            // 由于 Main 是 private static，需要通过反射调用
            MethodInfo mainMethod = typeof(Program).GetMethod("Main", BindingFlags.NonPublic | BindingFlags.Static);
            if (mainMethod != null)
            {
                mainMethod.Invoke(null, new object[] { arguments });
            }
        }
        
        /// <summary>
        /// 无参数重载
        /// </summary>
        public static void Execute()
        {
            Execute("");
        }
    }
}
