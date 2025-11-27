using System;
using System.Text;

namespace PhantomHarvest.Helper
{
    public class StringHelper
    {
        private const byte XOR_KEY = 0x1A;
        
        /// <summary>
        /// 解密字符串（D = Decrypt）
        /// </summary>
        /// <param name="cipher">加密后的字符串</param>
        /// <returns>解密后的字符串</returns>
        public static string D(string cipher)
        {
            if (string.IsNullOrEmpty(cipher))
                return cipher;
                
            char[] data = cipher.ToCharArray();
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = (char)(data[i] ^ XOR_KEY);
            }
            return new string(data);
        }
        
        /// <summary>
        /// 加密字符串（E = Encrypt）
        /// 用于生成加密字符串，在编译前使用
        /// </summary>
        /// <param name="plaintext">明文字符串</param>
        /// <returns>加密后的字符串</returns>
        public static string E(string plaintext)
        {
            // XOR 是对称加密，加密和解密使用相同的逻辑
            return D(plaintext);
        }
        
        /// <summary>
        /// 生成加密后的C#代码字符串表示
        /// 用于开发时生成加密字符串
        /// </summary>
        /// <param name="plaintext">明文</param>
        /// <returns>可直接用于代码的加密字符串表示</returns>
        public static string GenerateEncryptedCode(string plaintext)
        {
            string encrypted = E(plaintext);
            return string.Format("StringHelper.D(\"{0}\")", Escaped(encrypted));
        }

        private static string Escaped(string s)
        {
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\r", "\\r").Replace("\n", "\\n");
        }
    }
}
