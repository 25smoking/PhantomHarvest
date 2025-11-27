using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace PhantomHarvest.Helper
{
	// Token: 0x02000027 RID: 39
	public class Pixini
	{
		// Token: 0x14000001 RID: 1
		// (add) Token: 0x060000C2 RID: 194 RVA: 0x000091A8 File Offset: 0x000073A8
		// (remove) Token: 0x060000C3 RID: 195 RVA: 0x000091E0 File Offset: 0x000073E0
		public event Action<string> LogWarning;

		// Token: 0x14000002 RID: 2
		// (add) Token: 0x060000C4 RID: 196 RVA: 0x00009218 File Offset: 0x00007418
		// (remove) Token: 0x060000C5 RID: 197 RVA: 0x00009250 File Offset: 0x00007450
		public event Action<string> LogError;

		// Token: 0x1700000C RID: 12
		public string this[string key, string sectionName = "default"]
		{
			get
			{
				IniLine iniLine;
				if (!this.GetLineInfo(key, sectionName, out iniLine))
				{
					return null;
				}
				return iniLine.value;
			}
			set
			{
				string key2 = sectionName.ToLower();
				string b = key.ToLower();
				List<IniLine> list;
				if (this.sectionMap.TryGetValue(key2, out list))
				{
					int num = -1;
					for (int i = 0; i < list.Count; i++)
					{
						if (list[i].type == LineType.KeyValue && list[i].key.ToLower() == b)
						{
							num = i;
						}
					}
					if (num > -1)
					{
						IniLine value2 = list[num];
						IniLine iniLine;
						if (this.ParseValue(value, 0, out iniLine))
						{
							value2.value = iniLine.value;
							value2.quotechar = iniLine.quotechar;
							value2.array = iniLine.array;
						}
						else
						{
							value2.value = value;
							value2.array = null;
						}
						list[num] = value2;
						return;
					}
					IniLine item;
					if (!this.ParseValue(value, 0, out item))
					{
						list.Add(new IniLine
						{
							type = LineType.KeyValue,
							section = sectionName,
							key = key,
							value = value,
							quotechar = '\0'
						});
						return;
					}
					item.section = sectionName;
					item.key = key;
					list.Add(item);
					return;
				}
				else
				{
					this.sectionMap[sectionName] = new List<IniLine>();
					this.AddIniLine(new IniLine
					{
						type = LineType.Section,
						section = sectionName
					});
					IniLine current;
					if (!this.ParseValue(value, 0, out current))
					{
						this.AddIniLine(new IniLine
						{
							type = LineType.KeyValue,
							section = sectionName,
							key = key,
							value = value
						});
						return;
					}
					current.section = sectionName;
					current.key = key;
					this.AddIniLine(current);
					return;
				}
			}
		}

		// Token: 0x1700000D RID: 13
		// (get) Token: 0x060000C8 RID: 200 RVA: 0x00009464 File Offset: 0x00007664
		public string[] SectionNames
		{
			get
			{
				return this.sectionMap.Keys.ToArray<string>();
			}
		}

		// Token: 0x060000CA RID: 202 RVA: 0x00009487 File Offset: 0x00007687
		public Pixini()
		{
			this.Init();
		}

		// Token: 0x060000CB RID: 203 RVA: 0x000094BA File Offset: 0x000076BA
		private void Init()
		{
			this.structureOrder = new List<string>();
			this.sectionMap = new Dictionary<string, List<IniLine>>();
			this.lineNumber = 1;
			this.currentSection = "default";
		}

		// Token: 0x060000CC RID: 204 RVA: 0x000094E4 File Offset: 0x000076E4
		public static Pixini Load(string filename)
		{
			Pixini pixini = new Pixini();
			using (StreamReader streamReader = new StreamReader(filename))
			{
				string text;
				while ((text = streamReader.ReadLine()) != null)
				{
					pixini.Parse(text.Trim());
				}
			}
			pixini.PostProcess();
			return pixini;
		}

		public static Pixini LoadFromMemory(string content)
		{
			Pixini pixini = new Pixini();
			using (StringReader stringReader = new StringReader(content))
			{
				string text;
				while ((text = stringReader.ReadLine()) != null)
				{
					pixini.Parse(text.Trim());
				}
			}
			pixini.PostProcess();
			return pixini;
		}

		public static Pixini LoadFromString(string text)
		{
			return LoadFromMemory(text);
		}

		// Token: 0x060000CE RID: 206 RVA: 0x00009594 File Offset: 0x00007794
		public void Save(string filename, bool saveBackupOfPrevious = false)
		{
			if (saveBackupOfPrevious && File.Exists(filename))
			{
				File.Copy(filename, Path.GetFileNameWithoutExtension(filename) + ".bak");
			}
			this.HandleDefaultSection();
			using (StreamWriter streamWriter = new StreamWriter(filename))
			{
				IEnumerator<string> enumerator = this.Lines();
				while (enumerator.MoveNext())
				{
					if (enumerator.Current != null)
					{
						streamWriter.WriteLine(enumerator.Current);
					}
				}
			}
		}

		// Token: 0x060000CF RID: 207 RVA: 0x00009610 File Offset: 0x00007810
		public T Get<T>(string key, string sectionName = "default", T defaultVal = default(T))
		{
			string value = this[key, sectionName];
			TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));
			if (string.IsNullOrEmpty(value) || !converter.CanConvertFrom(typeof(string)))
			{
				return defaultVal;
			}
			T result;
			try
			{
				result = (T)((object)converter.ConvertFrom(value));
			}
			catch
			{
				result = defaultVal;
			}
			return result;
		}

		// Token: 0x060000D0 RID: 208 RVA: 0x00009678 File Offset: 0x00007878
		public void Set<T>(string key, string sectionName, T val)
		{
			this[key, sectionName] = val.ToString();
		}

		// Token: 0x060000D1 RID: 209 RVA: 0x0000968F File Offset: 0x0000788F
		public void Set<T>(string key, T val)
		{
			this[key, "default"] = val.ToString();
		}

		// Token: 0x060000D2 RID: 210 RVA: 0x000096AC File Offset: 0x000078AC
		public T[] GetArr<T>(string key, string sectionName = "default")
		{
			TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));
			IniLine iniLine;
			if (!this.GetLineInfo(key, sectionName, out iniLine) || iniLine.array == null)
			{
				return null;
			}
			return iniLine.array.Select(delegate(string val)
			{
				T result;
				if (string.IsNullOrEmpty(val) || !converter.CanConvertFrom(typeof(string)))
				{
					result = default(T);
					return result;
				}
				try
				{
					result = (T)((object)converter.ConvertFrom(val));
				}
				catch
				{
					result = default(T);
				}
				return result;
			}).ToArray<T>();
		}

		// Token: 0x060000D3 RID: 211 RVA: 0x00009708 File Offset: 0x00007908
		public bool SetA<T>(string key, string sectionName, params T[] vals)
		{
			IniLine iniLine;
			if (!this.GetLineInfo(key, sectionName, out iniLine) || iniLine.array == null)
			{
				return false;
			}
			iniLine.value = null;
			iniLine.array = (from val in vals
			select val.ToString()).ToArray<string>();
			iniLine.quotechar = '\0';
			this.ReplaceIniLine(iniLine);
			return true;
		}

		// Token: 0x060000D4 RID: 212 RVA: 0x00009775 File Offset: 0x00007975
		public bool SetA<T>(string key, params T[] vals)
		{
			return this.SetA<T>(key, "default", vals);
		}

		// Token: 0x060000D5 RID: 213 RVA: 0x00009784 File Offset: 0x00007984
		public bool SectionExists(string sectionName)
		{
			return !string.IsNullOrEmpty(sectionName) && this.sectionMap.ContainsKey(sectionName.ToLower());
		}

		// Token: 0x060000D6 RID: 214 RVA: 0x000097A4 File Offset: 0x000079A4
		public bool Delete(string key, string sectionName)
		{
			sectionName = sectionName.ToLower();
			key = key.ToLower();
			int num = -1;
			List<IniLine> list;
			if (this.sectionMap.TryGetValue(sectionName, out list))
			{
				for (int i = 0; i < list.Count; i++)
				{
					if (list[i].type == LineType.KeyValue && list[i].key.ToLower() == key)
					{
						num = i;
					}
				}
				if (num > -1)
				{
					list.RemoveAt(num);
					if (!this.IniListContainstype(list, LineType.KeyValue))
					{
						list.Clear();
						this.sectionMap[sectionName] = null;
						for (int j = this.structureOrder.Count - 1; j >= 0; j--)
						{
							if (this.structureOrder[j] == sectionName)
							{
								this.structureOrder.RemoveAt(j);
								break;
							}
						}
					}
					return true;
				}
			}
			return false;
		}

		// Token: 0x060000D7 RID: 215 RVA: 0x00009878 File Offset: 0x00007A78
		public bool DeleteSection(string sectionName)
		{
			sectionName = sectionName.ToLower();
			List<IniLine> list;
			if (this.sectionMap.TryGetValue(sectionName, out list))
			{
				list.Clear();
				this.sectionMap[sectionName] = null;
				for (int i = this.structureOrder.Count - 1; i >= 0; i--)
				{
					if (this.structureOrder[i] == sectionName)
					{
						this.structureOrder.RemoveAt(i);
						break;
					}
				}
				return true;
			}
			return false;
		}

		// Token: 0x060000D8 RID: 216 RVA: 0x000098ED File Offset: 0x00007AED
		public bool Delete(string key)
		{
			return this.Delete("default", key);
		}

		// Token: 0x060000D9 RID: 217 RVA: 0x000098FC File Offset: 0x00007AFC
		public bool IsArray(string key, string sectionName = "default")
		{
			IniLine iniLine;
			return this.GetLineInfo(key, sectionName, out iniLine) && iniLine.IsArray;
		}

		// Token: 0x060000DA RID: 218 RVA: 0x00009920 File Offset: 0x00007B20
		public override string ToString()
		{
			IEnumerator<string> enumerator = this.Lines();
			StringBuilder stringBuilder = new StringBuilder();
			while (enumerator.MoveNext())
			{
				if (enumerator.Current != null)
				{
					stringBuilder.AppendLine(enumerator.Current);
				}
			}
			return stringBuilder.ToString();
		}

		// Token: 0x060000DB RID: 219 RVA: 0x0000995F File Offset: 0x00007B5F
		private void PostProcess()
		{
			this.HandleDefaultSection();
		}

		// Token: 0x060000DC RID: 220 RVA: 0x00009968 File Offset: 0x00007B68
		private void HandleDefaultSection()
		{
			List<IniLine> list;
			if (this.sectionMap.TryGetValue(Pixini.defaultSectionLowerCased, out list) && list[0].type != LineType.Section && this.IniListContainstype(list, LineType.Section) && list.Count > 1)
			{
				for (int i = list.Count - 1; i >= 0; i--)
				{
					if (list[i].type == LineType.Section)
					{
						list.RemoveAt(i);
						break;
					}
				}
				int num = -1;
				for (int j = 0; j < list.Count; j++)
				{
					if (list[j].type != LineType.Comment && list[j].type != LineType.Section)
					{
						num = j;
						break;
					}
				}
				if (num != -1)
				{
					list.Insert(num, new IniLine
					{
						type = LineType.Section,
						section = "default"
					});
				}
			}
		}

		// Token: 0x060000DD RID: 221 RVA: 0x00009A40 File Offset: 0x00007C40
		private bool GetLineInfo(string key, string sectionName, out IniLine info)
		{
			sectionName = sectionName.ToLower();
			key = key.ToLower();
			List<IniLine> list;
			if (this.sectionMap.TryGetValue(sectionName, out list))
			{
				for (int i = 0; i < list.Count; i++)
				{
					if (list[i].type == LineType.KeyValue && list[i].key.ToLower() == key)
					{
						info = list[i];
						return true;
					}
				}
			}
			info = new IniLine
			{
				type = LineType.None
			};
			return false;
		}

		// Token: 0x060000DE RID: 222 RVA: 0x00009AD0 File Offset: 0x00007CD0
		private List<IniLine> GetSectionList(string sectionName)
		{
			sectionName = sectionName.ToLower();
			List<IniLine> result;
			if (this.sectionMap.TryGetValue(sectionName, out result))
			{
				return result;
			}
			return null;
		}

		// Token: 0x060000DF RID: 223 RVA: 0x00009AF8 File Offset: 0x00007CF8
		private int GetKeyIndex(string key, List<IniLine> section)
		{
			if (section == null)
			{
				return -1;
			}
			key = key.ToLower();
			for (int i = 0; i < section.Count; i++)
			{
				if (section[i].type == LineType.KeyValue && section[i].key.ToLower() == key)
				{
					return i;
				}
			}
			return -1;
		}

		// Token: 0x060000E0 RID: 224 RVA: 0x00009B50 File Offset: 0x00007D50
		private bool ReplaceIniLine(IniLine newIniLine)
		{
			List<IniLine> sectionList = this.GetSectionList(newIniLine.section);
			if (sectionList == null)
			{
				return false;
			}
			int keyIndex = this.GetKeyIndex(newIniLine.key, sectionList);
			if (keyIndex > -1)
			{
				sectionList[keyIndex] = newIniLine;
				return true;
			}
			return false;
		}

		// Token: 0x060000E1 RID: 225 RVA: 0x00009B8C File Offset: 0x00007D8C
		private int IndexOfKvSeparator(string txt)
		{
			return txt.IndexOf(this.inputKVSeparator);
		}

		// Token: 0x060000E2 RID: 226 RVA: 0x00009B9C File Offset: 0x00007D9C
		private void AddIniLine(IniLine current)
		{
			string text = current.section.ToLower();
			if (current.type == LineType.Section)
			{
				if (!this.structureOrder.Contains(current.section.ToLower()))
				{
					this.structureOrder.Add(current.section.ToLower());
				}
			}
			else if (text == Pixini.defaultSectionLowerCased && !this.structureOrder.Contains(current.section.ToLower()))
			{
				this.structureOrder.Add(Pixini.defaultSectionLowerCased);
			}
			List<IniLine> list;
			if (!this.sectionMap.TryGetValue(text, out list))
			{
				this.sectionMap[text] = new List<IniLine>();
				list = this.sectionMap[text];
			}
			if (current.type != LineType.Section || (current.type == LineType.Section && !this.IniListContainstype(list, LineType.Section)))
			{
				list.Add(current);
			}
		}

		// Token: 0x060000E3 RID: 227 RVA: 0x00009C74 File Offset: 0x00007E74
		private bool IniListContainstype(List<IniLine> lines, LineType type)
		{
			for (int i = 0; i < lines.Count; i++)
			{
				if (lines[i].type == type)
				{
					return true;
				}
			}
			return false;
		}

		// Token: 0x060000E4 RID: 228 RVA: 0x00009CA4 File Offset: 0x00007EA4
		private void FireLogWarning(string text, params object[] args)
		{
			string obj = string.Format("[line {0}] WARN: {1}", this.lineNumber, string.Format(text, args));
			Action<string> logWarning = this.LogWarning;
			if (logWarning == null)
			{
				return;
			}
			logWarning(obj);
		}

		// Token: 0x060000E5 RID: 229 RVA: 0x00009CE0 File Offset: 0x00007EE0
		private void FireLogError(string text, params object[] args)
		{
			string obj = string.Format("[line {0}] ERR: {1}", this.lineNumber, string.Format(text, args));
			Action<string> logError = this.LogError;
			if (logError == null)
			{
				return;
			}
			logError(obj);
		}

		// Token: 0x060000E6 RID: 230 RVA: 0x00009D1C File Offset: 0x00007F1C
		private void Parse(string line)
		{
			IniLine current;
			if (this.ParseLineComment(line, out current))
			{
				this.AddIniLine(current);
			}
			else if (this.ParseSection(line, out current))
			{
				this.AddIniLine(current);
			}
			else if (this.ParseKeyValue(line, out current))
			{
				this.AddIniLine(current);
			}
			this.lineNumber++;
		}

		// Token: 0x060000E7 RID: 231 RVA: 0x00009D74 File Offset: 0x00007F74
		private bool ParseLineComment(string line, out IniLine info)
		{
			info = default(IniLine);
			if (string.IsNullOrEmpty(line) || line.IsWhiteSpace() || line[0] != ';')
			{
				return false;
			}
			info = new IniLine
			{
				type = LineType.Comment,
				section = this.currentSection,
				comment = line.Substring(1, line.Length - 1)
			};
			return true;
		}

		// Token: 0x060000E8 RID: 232 RVA: 0x00009DE0 File Offset: 0x00007FE0
		private bool ParseSection(string line, out IniLine info)
		{
			info = default(IniLine);
			if (string.IsNullOrEmpty(line) || line.IsWhiteSpace() || line[0] != '[' || line.IndexOf(']') == -1)
			{
				return false;
			}
			StringBuilder stringBuilder = new StringBuilder();
			int num = 1;
			while (num < line.Length && line[num] != ']')
			{
				stringBuilder.Append(line[num]);
				num++;
			}
			if (stringBuilder.Length == 0)
			{
				return false;
			}
			string text = null;
			if (line.IndexOf(';') != -1 && line.IndexOf(';') < line.Length - 1)
			{
				text = line.Substring(line.IndexOf(';') + 1, line.Length - 1 - line.IndexOf(';'));
				if (string.IsNullOrEmpty(text) || text.IsWhiteSpace())
				{
					text = null;
				}
			}
			this.currentSection = stringBuilder.ToString();
			info = new IniLine
			{
				type = LineType.Section,
				section = stringBuilder.ToString(),
				comment = text
			};
			return true;
		}

		// Token: 0x060000E9 RID: 233 RVA: 0x00009EE4 File Offset: 0x000080E4
		private bool ParseValue(string input, int startIndex, out IniLine info)
		{
			char c = '\0';
			if (startIndex >= input.Length)
			{
				info = default(IniLine);
				return false;
			}
			while (char.IsWhiteSpace(input[startIndex]) && startIndex < input.Length)
			{
				startIndex++;
			}
			int num = input.CountChar('"', startIndex, -1);
			if (num < 2)
			{
				num = input.CountChar('\'', startIndex, -1);
				if (num < 2)
				{
					num = -1;
				}
			}
			int num2 = -1;
			StringBuilder stringBuilder = new StringBuilder(input.Length - startIndex);
			for (int i = startIndex; i < input.Length; i++)
			{
				if (num == -1 && input[i] == ';')
				{
					num2 = i;
					break;
				}
				if (num > 0 && (input[i] == '"' || input[i] == '\''))
				{
					num--;
					if (num == 0)
					{
						c = input[i];
						stringBuilder.Remove(0, 1);
						num2 = i + 1;
						if (num2 >= input.Length)
						{
							num2 = -1;
							break;
						}
						break;
					}
				}
				stringBuilder.Append(input[i]);
			}
			string comment = null;
			if (num2 > -1)
			{
				while (num2 < input.Length && char.IsWhiteSpace(input[num2]))
				{
					num2++;
				}
				if (input[num2] == ';' & num2 + 1 < input.Length)
				{
					comment = input.Substring(num2 + 1, input.Length - (num2 + 1));
				}
			}
			string text = stringBuilder.ToString();
			string[] array = null;
			if (c == '\0' && text.IndexOf(',') > -1)
			{
				array = (from csv in text.Split(new char[]
				{
					','
				})
				select csv.Trim()).ToArray<string>();
				if (array.Length == 1)
				{
					array = null;
				}
			}
			info = new IniLine
			{
				type = LineType.KeyValue,
				value = text,
				comment = comment,
				quotechar = c,
				array = array
			};
			return true;
		}

		// Token: 0x060000EA RID: 234 RVA: 0x0000A0C0 File Offset: 0x000082C0
		private bool ParseKeyValue(string input, out IniLine info)
		{
			info = default(IniLine);
			if (string.IsNullOrEmpty(input) || input.IsWhiteSpace())
			{
				return false;
			}
			int num = this.IndexOfKvSeparator(input);
			if (num == -1)
			{
				return false;
			}
			StringBuilder stringBuilder = new StringBuilder();
			for (int i = 0; i < num; i++)
			{
				if (char.IsWhiteSpace(input[i]))
				{
					this.FireLogWarning("Key names can't contain spaces. {0} was truncated to {1}", new object[]
					{
						input.Substring(0, num),
						stringBuilder.ToString()
					});
					break;
				}
				stringBuilder.Append(input[i]);
			}
			if (stringBuilder.Length == 0)
			{
				return false;
			}
			int startIndex = num + 1;
			if (!this.ParseValue(input, startIndex, out info))
			{
				return false;
			}
			info.section = this.currentSection;
			info.key = stringBuilder.ToString();
			return true;
		}

		// Token: 0x060000EB RID: 235 RVA: 0x0000A180 File Offset: 0x00008380
		private string GetString(IniLine iniStruct)
		{
			switch (iniStruct.type)
			{
			case LineType.Comment:
				return ";" + iniStruct.comment;
			case LineType.KeyValue:
			{
				string text = iniStruct.value;
				if (iniStruct.array != null)
				{
					text = string.Join(", ", iniStruct.array);
				}
				if (!string.IsNullOrEmpty(iniStruct.comment))
				{
					if (iniStruct.quotechar > '\0')
					{
						return string.Format("{0}{1}{4}{2}{4} ;{3}", new object[]
						{
							iniStruct.key,
							this.outputKVSeparator,
							text,
							iniStruct.comment,
							iniStruct.quotechar
						});
					}
					return string.Format("{0}{1}{2} ;{3}", new object[]
					{
						iniStruct.key,
						this.outputKVSeparator,
						text,
						iniStruct.comment
					});
				}
				else
				{
					if (iniStruct.quotechar > '\0')
					{
						return string.Format("{0}{1}{3}{2}{3}", new object[]
						{
							iniStruct.key,
							this.outputKVSeparator,
							text,
							iniStruct.quotechar
						});
					}
					return string.Format("{0}{1}{2}", iniStruct.key, this.outputKVSeparator, text);
				}

			}
			case LineType.Section:
				if (!string.IsNullOrEmpty(iniStruct.comment))
				{
					return "[" + iniStruct.section + "] ;" + iniStruct.comment;
				}
				return "[" + iniStruct.section + "]";
			default:
				return string.Empty;
			}
		}

		// Token: 0x060000EC RID: 236 RVA: 0x0000A313 File Offset: 0x00008513
		private IEnumerator<string> Lines()
		{
			LineType lineType = LineType.None;
			foreach (string text in this.structureOrder)
			{
				List<IniLine> list;
				if (!this.sectionMap.TryGetValue(text, out list))
				{
					throw new Exception("Unable to find the section in the dictionary list: " + text);
				}
				foreach (IniLine line in list)
				{
					if ((this.emptyLinesBetweenSections && line.type == LineType.Section && lineType == LineType.KeyValue) || (this.emptyLineAboveComments && line.type == LineType.Comment && lineType == LineType.KeyValue) || (this.emptyLinesBetweenKeyValuePairs && ((line.type == LineType.KeyValue && lineType == LineType.KeyValue) || (line.type == LineType.KeyValue && lineType == LineType.Section))))
					{
						yield return string.Empty;
					}
					yield return this.GetString(line);
					lineType = line.type;
				}

			}

			yield break;

		}

		// Token: 0x0400004C RID: 76
		private const string DEFAULT_SECTION = "default";

		// Token: 0x0400004D RID: 77
		private static string defaultSectionLowerCased = "default".ToLower();

		// Token: 0x0400004E RID: 78
		public char inputKVSeparator = '=';

		// Token: 0x0400004F RID: 79
		public char outputKVSeparator = '=';

		// Token: 0x04000052 RID: 82
		private int lineNumber = 1;

		// Token: 0x04000053 RID: 83
		public Dictionary<string, List<IniLine>> sectionMap;

		// Token: 0x04000054 RID: 84
		private List<string> structureOrder;

		// Token: 0x04000055 RID: 85
		private string currentSection;

		// Token: 0x04000056 RID: 86
		public bool emptyLinesBetweenSections = true;

		// Token: 0x04000057 RID: 87
		public bool emptyLineAboveComments = true;

		// Token: 0x04000058 RID: 88
		public bool emptyLinesBetweenKeyValuePairs;
	}
}
