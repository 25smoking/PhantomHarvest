using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace PhantomHarvest.Helper
{
	// Token: 0x02000031 RID: 49
	public class DataParser
	{
		// Token: 0x06000175 RID: 373 RVA: 0x0000BFFC File Offset: 0x0000A1FC
		public DataParser(string baseName)
		{
			if (File.Exists(baseName))
			{
				this.db_bytes = File.ReadAllBytes(baseName);
				if (Encoding.Default.GetString(this.db_bytes, 0, 15).CompareTo("SQLite format 3") != 0)
				{
					throw new Exception("Not a valid SQLite 3 Database File");
				}
				if (this.db_bytes[52] != 0)
				{
					throw new Exception("Auto-vacuum capable database is not supported");
				}
				this.page_size = (ushort)this.ConvertToInteger(16, 2);
				this.encoding = this.ConvertToInteger(56, 4);
				if (decimal.Compare(new decimal(this.encoding), 0m) == 0)
				{
					this.encoding = 1UL;
				}
				this.ReadMasterTable(100UL);
			}
		}

		// 【新增】支持内存中的 SQLite 数据库（用于无文件落地）
		// Token: 0x06000175_NEW RID: 373_NEW
		public DataParser(byte[] fileBytes)
		{
			this.db_bytes = fileBytes;
			if (Encoding.Default.GetString(this.db_bytes, 0, 15).CompareTo("SQLite format 3") != 0)
			{
				throw new Exception("Not a valid SQLite 3 Database File");
			}
			if (this.db_bytes[52] != 0)
			{
				throw new Exception("Auto-vacuum capable database is not supported");
			}
			this.page_size = (ushort)this.ConvertToInteger(16, 2);
			this.encoding = this.ConvertToInteger(56, 4);
			if (decimal.Compare(new decimal(this.encoding), 0m) == 0)
			{
				this.encoding = 1UL;
			}
			this.ReadMasterTable(100UL);
		}

		// Token: 0x06000176 RID: 374 RVA: 0x0000C0D4 File Offset: 0x0000A2D4
		private ulong ConvertToInteger(int startIndex, int Size)
		{
			if (Size > 8 | Size == 0)
			{
				return 0UL;
			}
			ulong num = 0UL;
			int num2 = Size - 1;
			for (int i = 0; i <= num2; i++)
			{
				num = (num << 8 | (ulong)this.db_bytes[startIndex + i]);
			}
			return num;
		}

		// Token: 0x06000177 RID: 375 RVA: 0x0000C114 File Offset: 0x0000A314
		private long CVL(int startIndex, int endIndex)
		{
			endIndex++;
			byte[] array = new byte[8];
			int num = endIndex - startIndex;
			bool flag = false;
			if (num == 0 | num > 9)
			{
				return 0L;
			}
			if (num == 1)
			{
				array[0] = (byte)(this.db_bytes[startIndex] & 127);
				return BitConverter.ToInt64(array, 0);
			}
			if (num == 9)
			{
				flag = true;
			}
			int num2 = 1;
			int num3 = 7;
			int num4 = 0;
			if (flag)
			{
				array[0] = this.db_bytes[endIndex - 1];
				endIndex--;
				num4 = 1;
			}
			for (int i = endIndex - 1; i >= startIndex; i += -1)
			{
				if (i - 1 >= startIndex)
				{
					array[num4] = (byte)(((int)((byte)(this.db_bytes[i] >> (num2 - 1 & 7))) & 255 >> num2) | (int)((byte)(this.db_bytes[i - 1] << (num3 & 7))));
					num2++;
					num4++;
					num3--;
				}
				else if (!flag)
				{
					array[num4] = (byte)((int)((byte)(this.db_bytes[i] >> (num2 - 1 & 7))) & 255 >> num2);
				}
			}
			return BitConverter.ToInt64(array, 0);
		}

		// Token: 0x06000178 RID: 376 RVA: 0x0000C21B File Offset: 0x0000A41B
		public int GetRowCount()
		{
			return this.table_entries.Length;
		}

		// Token: 0x06000179 RID: 377 RVA: 0x0000C228 File Offset: 0x0000A428
		public string[] GetTableNames()
		{
			List<string> list = new List<string>();
			int num = this.master_table_entries.Length - 1;
			for (int i = 0; i <= num; i++)
			{
				if (this.master_table_entries[i].item_type == "table")
				{
					list.Add(this.master_table_entries[i].item_name);
				}
			}
			return list.ToArray();
		}

		// Token: 0x0600017A RID: 378 RVA: 0x0000C28C File Offset: 0x0000A48C
		public long GetRawID(int row_num)
		{
			if (row_num >= this.table_entries.Length)
			{
				return 0L;
			}
			return this.table_entries[row_num].row_id;
		}

		// Token: 0x0600017B RID: 379 RVA: 0x0000C2AD File Offset: 0x0000A4AD
		public string GetValue(int row_num, int field)
		{
			if (row_num >= this.table_entries.Length)
			{
				return null;
			}
			if (field >= this.table_entries[row_num].content.Length)
			{
				return null;
			}
			return this.table_entries[row_num].content[field];
		}

		// Token: 0x0600017C RID: 380 RVA: 0x0000C2E8 File Offset: 0x0000A4E8
		public string GetValue(int row_num, string field)
		{
			int num = -1;
			int num2 = this.field_names.Length - 1;
			for (int i = 0; i <= num2; i++)
			{
				if (this.field_names[i].ToLower().CompareTo(field.ToLower()) == 0)
				{
					num = i;
					break;
				}
			}
			if (num == -1)
			{
				return null;
			}
			return this.GetValue(row_num, num);
		}

		// Token: 0x0600017D RID: 381 RVA: 0x0000C33C File Offset: 0x0000A53C
		private int GVL(int startIndex)
		{
			if (startIndex > this.db_bytes.Length)
			{
				return 0;
			}
			int num = startIndex + 8;
			for (int i = startIndex; i <= num; i++)
			{
				if (i > this.db_bytes.Length - 1)
				{
					return 0;
				}
				if ((this.db_bytes[i] & 128) != 128)
				{
					return i;
				}
			}
			return startIndex + 8;
		}

		// Token: 0x0600017E RID: 382 RVA: 0x0000C38F File Offset: 0x0000A58F
		private bool IsOdd(long value)
		{
			return (value & 1L) == 1L;
		}

		// Token: 0x0600017F RID: 383 RVA: 0x0000C39C File Offset: 0x0000A59C
		private void ReadMasterTable(ulong Offset)
		{
			if (this.db_bytes[(int)Offset] == 13)
			{
				ushort num = Convert.ToUInt16(decimal.Subtract(new decimal(this.ConvertToInteger(Convert.ToInt32(decimal.Add(new decimal(Offset), 3m)), 2)), 1m));
				int num2 = 0;
				if (this.master_table_entries != null)
				{
					num2 = this.master_table_entries.Length;
					Array.Resize<DataParser.sqlite_master_entry>(ref this.master_table_entries, this.master_table_entries.Length + (int)num + 1);
				}
				else
				{
					this.master_table_entries = new DataParser.sqlite_master_entry[(int)(num + 1)];
				}
				int num3 = (int)num;
				for (int i = 0; i <= num3; i++)
				{
					ulong num4 = this.ConvertToInteger(Convert.ToInt32(decimal.Add(decimal.Add(new decimal(Offset), 8m), new decimal(i * 2))), 2);
					if (decimal.Compare(new decimal(Offset), 100m) != 0)
					{
						num4 += Offset;
					}
					int num5 = this.GVL((int)num4);
					this.CVL((int)num4, num5);
					int num6 = this.GVL(Convert.ToInt32(decimal.Add(decimal.Add(new decimal(num4), decimal.Subtract(new decimal(num5), new decimal(num4))), 1m)));
					this.master_table_entries[num2 + i].row_id = this.CVL(Convert.ToInt32(decimal.Add(decimal.Add(new decimal(num4), decimal.Subtract(new decimal(num5), new decimal(num4))), 1m)), num6);
					num4 = Convert.ToUInt64(decimal.Add(decimal.Add(new decimal(num4), decimal.Subtract(new decimal(num6), new decimal(num4))), 1m));
					num5 = this.GVL((int)num4);
					num6 = num5;
					long value = this.CVL((int)num4, num5);
					long[] array = new long[5];
					int num7 = 0;
					do
					{
						num5 = num6 + 1;
						num6 = this.GVL(num5);
						array[num7] = this.CVL(num5, num6);
						if (array[num7] > 9L)
						{
							if (this.IsOdd(array[num7]))
							{
								array[num7] = (long)Math.Round((double)(array[num7] - 13L) / 2.0);
							}
							else
							{
								array[num7] = (long)Math.Round((double)(array[num7] - 12L) / 2.0);
							}
						}
						else
						{
							array[num7] = (long)((ulong)this.SQLDataTypeSize[(int)array[num7]]);
						}
						num7++;
					}
					while (num7 <= 4);
					if (decimal.Compare(new decimal(this.encoding), 1m) == 0)
					{
						this.master_table_entries[num2 + i].item_type = Encoding.UTF8.GetString(this.db_bytes, Convert.ToInt32(decimal.Add(new decimal(num4), new decimal(value))), (int)array[0]);
					}
					else if (decimal.Compare(new decimal(this.encoding), 2m) == 0)
					{
						this.master_table_entries[num2 + i].item_type = Encoding.Unicode.GetString(this.db_bytes, Convert.ToInt32(decimal.Add(new decimal(num4), new decimal(value))), (int)array[0]);
					}
					else if (decimal.Compare(new decimal(this.encoding), 3m) == 0)
					{
						this.master_table_entries[num2 + i].item_type = Encoding.BigEndianUnicode.GetString(this.db_bytes, Convert.ToInt32(decimal.Add(new decimal(num4), new decimal(value))), (int)array[0]);
					}
					if (decimal.Compare(new decimal(this.encoding), 1m) == 0)
					{
						this.master_table_entries[num2 + i].item_name = Encoding.Default.GetString(this.db_bytes, Convert.ToInt32(decimal.Add(decimal.Add(new decimal(num4), new decimal(value)), new decimal(array[0]))), (int)array[1]);
					}
					else if (decimal.Compare(new decimal(this.encoding), 2m) == 0)
					{
						this.master_table_entries[num2 + i].item_name = Encoding.Unicode.GetString(this.db_bytes, Convert.ToInt32(decimal.Add(decimal.Add(new decimal(num4), new decimal(value)), new decimal(array[0]))), (int)array[1]);
					}
					else if (decimal.Compare(new decimal(this.encoding), 3m) == 0)
					{
						this.master_table_entries[num2 + i].item_name = Encoding.BigEndianUnicode.GetString(this.db_bytes, Convert.ToInt32(decimal.Add(decimal.Add(new decimal(num4), new decimal(value)), new decimal(array[0]))), (int)array[1]);
					}
					this.master_table_entries[num2 + i].root_num = (long)this.ConvertToInteger(Convert.ToInt32(decimal.Add(decimal.Add(decimal.Add(decimal.Add(new decimal(num4), new decimal(value)), new decimal(array[0])), new decimal(array[1])), new decimal(array[2]))), (int)array[3]);
					if (decimal.Compare(new decimal(this.encoding), 1m) == 0)
					{
						this.master_table_entries[num2 + i].sql_statement = Encoding.Default.GetString(this.db_bytes, Convert.ToInt32(decimal.Add(decimal.Add(decimal.Add(decimal.Add(decimal.Add(new decimal(num4), new decimal(value)), new decimal(array[0])), new decimal(array[1])), new decimal(array[2])), new decimal(array[3]))), (int)array[4]);
					}
					else if (decimal.Compare(new decimal(this.encoding), 2m) == 0)
					{
						this.master_table_entries[num2 + i].sql_statement = Encoding.Unicode.GetString(this.db_bytes, Convert.ToInt32(decimal.Add(decimal.Add(decimal.Add(decimal.Add(decimal.Add(new decimal(num4), new decimal(value)), new decimal(array[0])), new decimal(array[1])), new decimal(array[2])), new decimal(array[3]))), (int)array[4]);
					}
					else if (decimal.Compare(new decimal(this.encoding), 3m) == 0)
					{
						this.master_table_entries[num2 + i].sql_statement = Encoding.BigEndianUnicode.GetString(this.db_bytes, Convert.ToInt32(decimal.Add(decimal.Add(decimal.Add(decimal.Add(decimal.Add(new decimal(num4), new decimal(value)), new decimal(array[0])), new decimal(array[1])), new decimal(array[2])), new decimal(array[3]))), (int)array[4]);
					}
				}
				return;
			}
			if (this.db_bytes[(int)Offset] == 5)
			{
				int num8 = (int)Convert.ToUInt16(decimal.Subtract(new decimal(this.ConvertToInteger(Convert.ToInt32(decimal.Add(new decimal(Offset), 3m)), 2)), 1m));
				for (int j = 0; j <= num8; j++)
				{
					ushort num9 = (ushort)this.ConvertToInteger(Convert.ToInt32(decimal.Add(decimal.Add(new decimal(Offset), 12m), new decimal(j * 2))), 2);
					this.ReadMasterTable((decimal.Compare(new decimal(Offset), 100m) == 0) ? Convert.ToUInt64(decimal.Multiply(decimal.Subtract(new decimal(this.ConvertToInteger((int)num9, 4)), 1m), new decimal((int)this.page_size))) : Convert.ToUInt64(decimal.Multiply(decimal.Subtract(new decimal(this.ConvertToInteger((int)(Offset + (ulong)num9), 4)), 1m), new decimal((int)this.page_size))));
				}
				this.ReadMasterTable(Convert.ToUInt64(decimal.Multiply(decimal.Subtract(new decimal(this.ConvertToInteger(Convert.ToInt32(decimal.Add(new decimal(Offset), 8m)), 4)), 1m), new decimal((int)this.page_size))));
			}
		}

		// Token: 0x06000180 RID: 384 RVA: 0x0000CBB0 File Offset: 0x0000ADB0
		public bool ReadTable(string TableName)
		{
			int num = -1;
			int num2 = this.master_table_entries.Length - 1;
			for (int i = 0; i <= num2; i++)
			{
				if (this.master_table_entries[i].item_name.ToLower().CompareTo(TableName.ToLower()) == 0)
				{
					num = i;
					break;
				}
			}
			if (num == -1)
			{
				return false;
			}
			string[] array = this.master_table_entries[num].sql_statement.Substring(this.master_table_entries[num].sql_statement.IndexOf("(") + 1).Split(new char[]
			{
				','
			});
			int num3 = array.Length - 1;
			for (int j = 0; j <= num3; j++)
			{
				array[j] = array[j].TrimStart(new char[0]);
				int num4 = array[j].IndexOf(" ");
				if (num4 > 0)
				{
					array[j] = array[j].Substring(0, num4);
				}
				if (array[j].IndexOf("UNIQUE") == 0)
				{
					break;
				}
				Array.Resize<string>(ref this.field_names, j + 1);
				this.field_names[j] = array[j];
			}
			return this.ReadTableFromOffset((ulong)((this.master_table_entries[num].root_num - 1L) * (long)((ulong)this.page_size)));
		}

		// Token: 0x06000181 RID: 385 RVA: 0x0000CCEC File Offset: 0x0000AEEC
		private bool ReadTableFromOffset(ulong Offset)
		{
			if (this.db_bytes[(int)Offset] == 13)
			{
				int num = Convert.ToInt32(decimal.Subtract(new decimal(this.ConvertToInteger(Convert.ToInt32(decimal.Add(new decimal(Offset), 3m)), 2)), 1m));
				int num2 = 0;
				if (this.table_entries != null)
				{
					num2 = this.table_entries.Length;
					Array.Resize<DataParser.table_entry>(ref this.table_entries, this.table_entries.Length + num + 1);
				}
				else
				{
					this.table_entries = new DataParser.table_entry[num + 1];
				}
				int num3 = num;
				for (int i = 0; i <= num3; i++)
				{
					DataParser.record_header_field[] array = new DataParser.record_header_field[1];
					ulong num4 = this.ConvertToInteger(Convert.ToInt32(decimal.Add(decimal.Add(new decimal(Offset), 8m), new decimal(i * 2))), 2);
					if (decimal.Compare(new decimal(Offset), 100m) != 0)
					{
						num4 += Offset;
					}
					int num5 = this.GVL((int)num4);
					this.CVL((int)num4, num5);
					int num6 = this.GVL(Convert.ToInt32(decimal.Add(decimal.Add(new decimal(num4), decimal.Subtract(new decimal(num5), new decimal(num4))), 1m)));
					this.table_entries[num2 + i].row_id = this.CVL(Convert.ToInt32(decimal.Add(decimal.Add(new decimal(num4), decimal.Subtract(new decimal(num5), new decimal(num4))), 1m)), num6);
					num4 = Convert.ToUInt64(decimal.Add(decimal.Add(new decimal(num4), decimal.Subtract(new decimal(num6), new decimal(num4))), 1m));
					num5 = this.GVL((int)num4);
					num6 = num5;
					long num7 = this.CVL((int)num4, num5);
					long num8 = Convert.ToInt64(decimal.Add(decimal.Subtract(new decimal(num4), new decimal(num5)), 1m));
					int num9 = 0;
					while (num8 < num7)
					{
						Array.Resize<DataParser.record_header_field>(ref array, num9 + 1);
						num5 = num6 + 1;
						num6 = this.GVL(num5);
						array[num9].type = this.CVL(num5, num6);
						if (array[num9].type > 9L)
						{
							if (this.IsOdd(array[num9].type))
							{
								array[num9].size = (long)Math.Round((double)(array[num9].type - 13L) / 2.0);
							}
							else
							{
								array[num9].size = (long)Math.Round((double)(array[num9].type - 12L) / 2.0);
							}
						}
						else
						{
							array[num9].size = (long)((ulong)this.SQLDataTypeSize[(int)array[num9].type]);
						}
						num8 = num8 + (long)(num6 - num5) + 1L;
						num9++;
					}
					this.table_entries[num2 + i].content = new string[array.Length - 1 + 1];
					int num10 = 0;
					int num11 = array.Length - 1;
					for (int j = 0; j <= num11; j++)
					{
						if (array[j].type > 9L)
						{
							if (!this.IsOdd(array[j].type))
							{
								if (decimal.Compare(new decimal(this.encoding), 1m) == 0)
								{
									byte[] array2 = new byte[array[j].size];
									Array.Copy(this.db_bytes, (long)Convert.ToInt32(decimal.Add(decimal.Add(new decimal(num4), new decimal(num7)), new decimal(num10))), array2, 0L, array[j].size);
									this.table_entries[num2 + i].content[j] = Convert.ToBase64String(array2);
								}
								else if (decimal.Compare(new decimal(this.encoding), 2m) == 0)
								{
									this.table_entries[num2 + i].content[j] = Encoding.Unicode.GetString(this.db_bytes, Convert.ToInt32(decimal.Add(decimal.Add(new decimal(num4), new decimal(num7)), new decimal(num10))), (int)array[j].size);
								}
								else if (decimal.Compare(new decimal(this.encoding), 3m) == 0)
								{
									this.table_entries[num2 + i].content[j] = Encoding.BigEndianUnicode.GetString(this.db_bytes, Convert.ToInt32(decimal.Add(decimal.Add(new decimal(num4), new decimal(num7)), new decimal(num10))), (int)array[j].size);
								}
							}
							else
							{
								this.table_entries[num2 + i].content[j] = Encoding.Default.GetString(this.db_bytes, Convert.ToInt32(decimal.Add(decimal.Add(new decimal(num4), new decimal(num7)), new decimal(num10))), (int)array[j].size);
							}
						}
						else
						{
							int startIndex = Convert.ToInt32(decimal.Add(decimal.Add(new decimal(num4), new decimal(num7)), new decimal(num10)));
							this.table_entries[num2 + i].content[j] = Convert.ToString(this.ConvertToInteger(startIndex, (int)array[j].size));
						}
						num10 += (int)array[j].size;
					}
				}
			}
			else if (this.db_bytes[(int)Offset] == 5)
			{
				int num12 = (int)Convert.ToUInt16(decimal.Subtract(new decimal(this.ConvertToInteger(Convert.ToInt32(decimal.Add(new decimal(Offset), 3m)), 2)), 1m));
				for (int k = 0; k <= num12; k++)
				{
					ushort num13 = (ushort)this.ConvertToInteger(Convert.ToInt32(decimal.Add(decimal.Add(new decimal(Offset), 12m), new decimal(k * 2))), 2);
					this.ReadTableFromOffset(Convert.ToUInt64(decimal.Multiply(decimal.Subtract(new decimal(this.ConvertToInteger((int)(Offset + (ulong)num13), 4)), 1m), new decimal((int)this.page_size))));
				}
				this.ReadTableFromOffset(Convert.ToUInt64(decimal.Multiply(decimal.Subtract(new decimal(this.ConvertToInteger(Convert.ToInt32(decimal.Add(new decimal(Offset), 8m)), 4)), 1m), new decimal((int)this.page_size))));
			}
			return true;
		}

		// Token: 0x04000095 RID: 149
		private byte[] db_bytes;

		// Token: 0x04000096 RID: 150
		private readonly ulong encoding;

		// Token: 0x04000097 RID: 151
		private string[] field_names = new string[1];

		// Token: 0x04000098 RID: 152
		private DataParser.sqlite_master_entry[] master_table_entries;

		// Token: 0x04000099 RID: 153
		private ushort page_size;

		// Token: 0x0400009A RID: 154
		private readonly byte[] SQLDataTypeSize = new byte[]
		{
			0,
			1,
			2,
			3,
			4,
			6,
			8,
			8,
			0,
			0
		};

		// Token: 0x0400009B RID: 155
		private DataParser.table_entry[] table_entries;

		// Token: 0x0200005E RID: 94
		private struct record_header_field
		{
			// Token: 0x040001AF RID: 431
			public long size;

			// Token: 0x040001B0 RID: 432
			public long type;
		}

		// Token: 0x0200005F RID: 95
		private struct sqlite_master_entry
		{
			// Token: 0x040001B1 RID: 433
			public long row_id;

			// Token: 0x040001B2 RID: 434
			public string item_type;

			// Token: 0x040001B3 RID: 435
			public string item_name;

			// Token: 0x040001B4 RID: 436


			// Token: 0x040001B5 RID: 437
			public long root_num;

			// Token: 0x040001B6 RID: 438
			public string sql_statement;
		}

		// Token: 0x02000060 RID: 96
		private struct table_entry
		{
			// Token: 0x040001B7 RID: 439
			public long row_id;

			// Token: 0x040001B8 RID: 440
			public string[] content;
		}
		public void ApplyWal(byte[] walBytes)
		{
			if (walBytes == null || walBytes.Length < 32) return;

			// 解析 WAL 头
			// 偏移量 8 处的页面大小是大端序。
			uint walPageSize = DataParser.BigEndianToUInt32(walBytes, 8);
			
			// 验证页面大小
			if (this.page_size == 0 && walPageSize > 0) 
			{
				this.page_size = (ushort)walPageSize;
			}
			
			if (walPageSize == 0 || (this.page_size > 0 && walPageSize != this.page_size))
			{
				// 不匹配或无效，如果 WAL 为 0，尝试使用 DB 页面大小？
				// 或者如果 WAL 说 X 而 DB 说 Y，通常 WAL 对帧具有权威性？
				// 但帧必须匹配 DB 页面大小。
				if (walPageSize == 0) walPageSize = this.page_size;
			}

			int offset = 32;
			while (offset + 24 + (int)walPageSize <= walBytes.Length)
			{
				// 读取帧头
				uint pageNumber = DataParser.BigEndianToUInt32(walBytes, offset);
				
				// 页面数据
				int dataOffset = offset + 24;
				
				// 应用到 db_bytes
				// 页面编号是从 1 开始的。
				long dbOffset = (long)(pageNumber - 1) * (long)walPageSize;
				
				if (dbOffset + (long)walPageSize > (long)this.db_bytes.Length)
				{
					// 调整 db_bytes 大小
					Array.Resize<byte>(ref this.db_bytes, (int)(dbOffset + (long)walPageSize));
				}
				
				Array.Copy(walBytes, dataOffset, this.db_bytes, dbOffset, (long)walPageSize);
				
				offset += 24 + (int)walPageSize;
			}
		}

		private static uint BigEndianToUInt32(byte[] buffer, int offset)
		{
			return (uint)((uint)buffer[offset] << 24 | (uint)buffer[offset + 1] << 16 | (uint)buffer[offset + 2] << 8 | (uint)buffer[offset + 3]);
		}
	}
}
