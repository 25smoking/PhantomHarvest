using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace PhantomHarvest.Helper
{
	// Token: 0x02000030 RID: 48
	public class ArchiveManager : IDisposable
	{
		// Token: 0x06000162 RID: 354 RVA: 0x0000B3AC File Offset: 0x000095AC
		static ArchiveManager()
		{
			ArchiveManager.CrcTable = new uint[256];
			for (int i = 0; i < ArchiveManager.CrcTable.Length; i++)
			{
				uint num = (uint)i;
				for (int j = 0; j < 8; j++)
				{
					if ((num & 1U) != 0U)
					{
						num = (3988292384U ^ num >> 1);
					}
					else
					{
						num >>= 1;
					}
				}
				ArchiveManager.CrcTable[i] = num;
			}
		}

		// Token: 0x06000163 RID: 355 RVA: 0x0000B414 File Offset: 0x00009614
		public static ArchiveManager Create(string _filename, string _comment = null)
		{
			ArchiveManager zipStorer = ArchiveManager.Create(new FileStream(_filename, FileMode.Create, FileAccess.ReadWrite), _comment, false);
			zipStorer.Comment = (_comment ?? string.Empty);
			zipStorer.FileName = _filename;
			return zipStorer;
		}

		// Token: 0x06000164 RID: 356 RVA: 0x0000B43C File Offset: 0x0000963C
		public static ArchiveManager Create(Stream _stream, string _comment = null, bool _leaveOpen = false)
		{
			return new ArchiveManager
			{
				Comment = (_comment ?? string.Empty),
				ZipFileStream = _stream,
				Access = FileAccess.Write,
				leaveOpen = _leaveOpen
			};
		}

		// Token: 0x06000165 RID: 357 RVA: 0x0000B468 File Offset: 0x00009668
		public ArchiveManager.ZipFileEntry AddFile(ArchiveManager.Compression _method, string _pathname, string _filenameInZip, string _comment = null)
		{
			if (this.Access == FileAccess.Read)
			{
				throw new InvalidOperationException("Writing is not alowed");
			}
			ArchiveManager.ZipFileEntry result;
			using (FileStream fileStream = new FileStream(_pathname, FileMode.Open, FileAccess.Read))
			{
				result = this.AddStream(_method, _filenameInZip, fileStream, File.GetLastWriteTime(_pathname), _comment);
			}
			return result;
		}

		// Token: 0x06000166 RID: 358 RVA: 0x0000B4C4 File Offset: 0x000096C4
		public ArchiveManager.ZipFileEntry AddStream(ArchiveManager.Compression _method, string _filenameInZip, Stream _source, DateTime _modTime, string _comment = null)
		{
			return this.AddStreamAsync(_method, _filenameInZip, _source, _modTime, _comment);
		}

		// Token: 0x06000167 RID: 359 RVA: 0x0000B4D4 File Offset: 0x000096D4
		// Token: 0x06000167 RID: 359 RVA: 0x0000B4D4 File Offset: 0x000096D4
		public ArchiveManager.ZipFileEntry AddStreamAsync(ArchiveManager.Compression _method, string _filenameInZip, Stream _source, DateTime _modTime, string _comment = null)
		{
			if (this.Access == FileAccess.Read)
			{
				throw new InvalidOperationException("Writing is not alowed");
			}
			
			lock (this._lock)
			{
				ArchiveManager.ZipFileEntry zipFileEntry = new ArchiveManager.ZipFileEntry
				{
					Method = _method,
					EncodeUTF8 = this.EncodeUTF8,
					FilenameInZip = this.NormalizedFilename(_filenameInZip),
					Comment = (_comment ?? string.Empty),
					Crc32 = 0U,
					HeaderOffset = (uint)this.ZipFileStream.Position,
					CreationTime = _modTime,
					ModifyTime = _modTime,
					AccessTime = _modTime
				};
				this.WriteLocalHeader(zipFileEntry);
				zipFileEntry.FileOffset = (uint)this.ZipFileStream.Position;
				this.Store(zipFileEntry, _source);
				_source.Close();
				this.UpdateCrcAndSizes(zipFileEntry);
				this.Files.Add(zipFileEntry);
				return zipFileEntry;
			}
		}

		// Token: 0x06000168 RID: 360 RVA: 0x0000B59C File Offset: 0x0000979C
		public void AddDirectory(ArchiveManager.Compression _method, string _pathname, string _pathnameInZip, string _comment = null)
		{
			if (this.Access == FileAccess.Read)
			{
				throw new InvalidOperationException("Writing is not allowed");
			}
			int num = _pathname.LastIndexOf(Path.DirectorySeparatorChar);
			string text = Path.DirectorySeparatorChar.ToString();
			string text2 = (num >= 0) ? _pathname.Remove(0, num + 1) : _pathname;
			if (_pathnameInZip != null && _pathnameInZip != "")
			{
				text2 = _pathnameInZip + text2;
			}
			if (!text2.EndsWith(text, StringComparison.CurrentCulture))
			{
				text2 += text;
			}
			foreach (string text3 in Directory.GetFiles(_pathname))
			{
				this.AddFile(_method, text3, text2 + Path.GetFileName(text3), "");
			}
			foreach (string pathname in Directory.GetDirectories(_pathname))
			{
				this.AddDirectory(_method, pathname, text2, "");
			}
		}

		// Token: 0x06000169 RID: 361 RVA: 0x0000B684 File Offset: 0x00009884
		public void Close()
		{
			if (this.Access != FileAccess.Read)
			{
				uint offset = (uint)this.ZipFileStream.Position;
				uint num = 0U;
				if (this.CentralDirImage != null)
				{
					this.ZipFileStream.Write(this.CentralDirImage, 0, this.CentralDirImage.Length);
				}
				foreach (ArchiveManager.ZipFileEntry zfe in this.Files)
				{
					long position = this.ZipFileStream.Position;
					this.WriteCentralDirRecord(zfe);
					num += (uint)(this.ZipFileStream.Position - position);
				}
				if (this.CentralDirImage != null)
				{
					this.WriteEndRecord(num + (uint)this.CentralDirImage.Length, offset);
				}
				else
				{
					this.WriteEndRecord(num, offset);
				}
			}
			if (this.ZipFileStream != null && !this.leaveOpen)
			{
				this.ZipFileStream.Flush();
				this.ZipFileStream.Dispose();
				this.ZipFileStream = null;
			}
		}

		// Token: 0x0600016A RID: 362 RVA: 0x0000B784 File Offset: 0x00009984
		private void WriteLocalHeader(ArchiveManager.ZipFileEntry _zfe)
		{
			long position = this.ZipFileStream.Position;
			byte[] bytes = (_zfe.EncodeUTF8 ? Encoding.UTF8 : ArchiveManager.DefaultEncoding).GetBytes(_zfe.FilenameInZip);
			byte[] array = this.CreateExtraInfo(_zfe);
			if (this.Password != null)
			{
				byte[] array2 = new byte[11];
				array2[0] = 1;
				array2[1] = 153;
				array2[2] = 7;
				array2[3] = 0;
				array2[4] = 2;
				array2[5] = 0;
				array2[6] = 65;
				array2[7] = 69;
				array2[8] = 3;
				BitConverter.GetBytes((ushort)_zfe.Method).CopyTo(array2, 9);
				byte[] array3 = new byte[array.Length + array2.Length];
				Array.Copy(array, 0, array3, 0, array.Length);
				Array.Copy(array2, 0, array3, array.Length, array2.Length);
				array = array3;
			}
			this.ZipFileStream.Write(new byte[]
			{
				80,
				75,
				3,
				4
			}, 0, 4);
			this.ZipFileStream.Write(BitConverter.GetBytes((ushort)((this.Password != null) ? 51 : 20)), 0, 2);
			ushort num = (ushort)(_zfe.EncodeUTF8 ? 2048 : 0);
			if (this.Password != null)
			{
				num |= 1;
			}
			this.ZipFileStream.Write(BitConverter.GetBytes(num), 0, 2);
			ushort value = (ushort)_zfe.Method;
			if (this.Password != null)
			{
				value = 99;
			}
			this.ZipFileStream.Write(BitConverter.GetBytes(value), 0, 2);
			this.ZipFileStream.Write(BitConverter.GetBytes(this.DateTimeToDosTime(_zfe.ModifyTime)), 0, 4);
			this.ZipFileStream.Write(new byte[12], 0, 12);
			this.ZipFileStream.Write(BitConverter.GetBytes((ushort)bytes.Length), 0, 2);
			this.ZipFileStream.Write(BitConverter.GetBytes((ushort)array.Length), 0, 2);
			this.ZipFileStream.Write(bytes, 0, bytes.Length);
			this.ZipFileStream.Write(array, 0, array.Length);
			_zfe.HeaderSize = (uint)(this.ZipFileStream.Position - position);
		}

		// Token: 0x0600016B RID: 363 RVA: 0x0000B8B4 File Offset: 0x00009AB4
		private void WriteCentralDirRecord(ArchiveManager.ZipFileEntry _zfe)
		{
			Encoding encoding = _zfe.EncodeUTF8 ? Encoding.UTF8 : ArchiveManager.DefaultEncoding;
			byte[] bytes = encoding.GetBytes(_zfe.FilenameInZip);
			byte[] bytes2 = encoding.GetBytes(_zfe.Comment);
			byte[] array = this.CreateExtraInfo(_zfe);
			if (this.Password != null)
			{
				byte[] array2 = new byte[11];
				array2[0] = 1;
				array2[1] = 153;
				array2[2] = 7;
				array2[3] = 0;
				array2[4] = 2;
				array2[5] = 0;
				array2[6] = 65;
				array2[7] = 69;
				array2[8] = 3;
				BitConverter.GetBytes((ushort)_zfe.Method).CopyTo(array2, 9);
				byte[] array3 = new byte[array.Length + array2.Length];
				Array.Copy(array, 0, array3, 0, array.Length);
				Array.Copy(array2, 0, array3, array.Length, array2.Length);
				array = array3;
			}
			this.ZipFileStream.Write(new byte[]
			{
				80,
				75,
				1,
				2,
				23,
				11
			}, 0, 6);
			this.ZipFileStream.Write(BitConverter.GetBytes((ushort)((this.Password != null) ? 51 : 20)), 0, 2);
			ushort num = (ushort)(_zfe.EncodeUTF8 ? 2048 : 0);
			if (this.Password != null)
			{
				num |= 1;
			}
			this.ZipFileStream.Write(BitConverter.GetBytes(num), 0, 2);
			ushort value = (ushort)_zfe.Method;
			if (this.Password != null)
			{
				value = 99;
			}
			this.ZipFileStream.Write(BitConverter.GetBytes(value), 0, 2);
			this.ZipFileStream.Write(BitConverter.GetBytes(this.DateTimeToDosTime(_zfe.ModifyTime)), 0, 4);
			this.ZipFileStream.Write(BitConverter.GetBytes(_zfe.Crc32), 0, 4);
			this.ZipFileStream.Write(BitConverter.GetBytes(_zfe.CompressedSize), 0, 4);
			this.ZipFileStream.Write(BitConverter.GetBytes(_zfe.FileSize), 0, 4);
			this.ZipFileStream.Write(BitConverter.GetBytes((ushort)bytes.Length), 0, 2);
			this.ZipFileStream.Write(BitConverter.GetBytes((ushort)array.Length), 0, 2);
			this.ZipFileStream.Write(BitConverter.GetBytes((ushort)bytes2.Length), 0, 2);
			this.ZipFileStream.Write(BitConverter.GetBytes(0), 0, 2);
			this.ZipFileStream.Write(BitConverter.GetBytes(0), 0, 2);
			this.ZipFileStream.Write(BitConverter.GetBytes(0), 0, 2);
			this.ZipFileStream.Write(BitConverter.GetBytes(33024), 0, 2);
			this.ZipFileStream.Write(BitConverter.GetBytes(_zfe.HeaderOffset), 0, 4);
			this.ZipFileStream.Write(bytes, 0, bytes.Length);
			this.ZipFileStream.Write(array, 0, array.Length);
			this.ZipFileStream.Write(bytes2, 0, bytes2.Length);
		}

		// Token: 0x0600016C RID: 364 RVA: 0x0000BA90 File Offset: 0x00009C90
		private void WriteEndRecord(uint _size, uint _offset)
		{
			byte[] bytes = (this.EncodeUTF8 ? Encoding.UTF8 : ArchiveManager.DefaultEncoding).GetBytes(this.Comment);
			this.ZipFileStream.Write(new byte[]
			{
				80,
				75,
				5,
				6,
				0,
				0,
				0,
				0
			}, 0, 8);
			this.ZipFileStream.Write(BitConverter.GetBytes((int)((ushort)this.Files.Count + this.ExistingFiles)), 0, 2);
			this.ZipFileStream.Write(BitConverter.GetBytes((int)((ushort)this.Files.Count + this.ExistingFiles)), 0, 2);
			this.ZipFileStream.Write(BitConverter.GetBytes(_size), 0, 4);
			this.ZipFileStream.Write(BitConverter.GetBytes(_offset), 0, 4);
			this.ZipFileStream.Write(BitConverter.GetBytes((ushort)bytes.Length), 0, 2);
			this.ZipFileStream.Write(bytes, 0, bytes.Length);
		}

		// Token: 0x0600016D RID: 365 RVA: 0x0000BB74 File Offset: 0x00009D74
		private ArchiveManager.Compression Store(ArchiveManager.ZipFileEntry _zfe, Stream _source)
		{
			byte[] array = new byte[16384];
			uint num = 0U;
			long position = this.ZipFileStream.Position;
			long position2 = _source.CanSeek ? _source.Position : 0L;
			Stream stream = this.ZipFileStream;
			HMACSHA1 hmacsha = null;
			if (this.Password != null)
			{
				byte[] array2 = new byte[16];
				new RNGCryptoServiceProvider().GetBytes(array2);
				this.ZipFileStream.Write(array2, 0, 16);
				Rfc2898DeriveBytes rfc2898DeriveBytes = new Rfc2898DeriveBytes(this.Password, array2, 1000);
				byte[] bytes = rfc2898DeriveBytes.GetBytes(32);
				byte[] bytes2 = rfc2898DeriveBytes.GetBytes(32);
				byte[] bytes3 = rfc2898DeriveBytes.GetBytes(2);
				this.ZipFileStream.Write(bytes3, 0, 2);
				hmacsha = new HMACSHA1(bytes2);
				stream = new ArchiveManager.AesCtrStream(this.ZipFileStream, bytes, array2, hmacsha);
			}
			Stream stream2 = (_zfe.Method == ArchiveManager.Compression.Store) ? stream : new DeflateStream(stream, CompressionMode.Compress, true);
			_zfe.Crc32 = uint.MaxValue;
			int num2;
			do
			{
				num2 = _source.Read(array, 0, array.Length);
				num += (uint)num2;
				if (num2 > 0)
				{
					stream2.Write(array, 0, num2);
					uint num3 = 0U;
					while ((ulong)num3 < (ulong)((long)num2))
					{
						_zfe.Crc32 = (ArchiveManager.CrcTable[(int)((UIntPtr)((_zfe.Crc32 ^ (uint)array[(int)num3]) & 255U))] ^ _zfe.Crc32 >> 8);
						num3 += 1U;
					}
				}
			}
			while (num2 > 0);
			stream2.Flush();
			if (_zfe.Method == ArchiveManager.Compression.Deflate)
			{
				stream2.Dispose();
			}
			if (this.Password != null)
			{
				hmacsha.TransformFinalBlock(new byte[0], 0, 0);
				byte[] array3 = new byte[10];
				Array.Copy(hmacsha.Hash, 0, array3, 0, 10);
				this.ZipFileStream.Write(array3, 0, 10);
			}
			_zfe.Crc32 ^= uint.MaxValue;
			_zfe.FileSize = num;
			_zfe.CompressedSize = (uint)(this.ZipFileStream.Position - position);
			if (_zfe.Method == ArchiveManager.Compression.Deflate && !this.ForceDeflating && _source.CanSeek && _zfe.CompressedSize > _zfe.FileSize)
			{
				_zfe.Method = ArchiveManager.Compression.Store;
				this.ZipFileStream.Position = position;
				this.ZipFileStream.SetLength(position);
				_source.Position = position2;
				return this.Store(_zfe, _source);
			}
			return _zfe.Method;
		}

		// Token: 0x0600016E RID: 366 RVA: 0x0000BCCC File Offset: 0x00009ECC
		private uint DateTimeToDosTime(DateTime _dt)
		{
			return (uint)(_dt.Second / 2 | _dt.Minute << 5 | _dt.Hour << 11 | _dt.Day << 16 | _dt.Month << 21 | _dt.Year - 1980 << 25);
		}

		// Token: 0x0600016F RID: 367 RVA: 0x0000BD20 File Offset: 0x00009F20
		private byte[] CreateExtraInfo(ArchiveManager.ZipFileEntry _zfe)
		{
			byte[] array = new byte[36];
			BitConverter.GetBytes(10).CopyTo(array, 0);
			BitConverter.GetBytes(32).CopyTo(array, 2);
			BitConverter.GetBytes(1).CopyTo(array, 8);
			BitConverter.GetBytes(24).CopyTo(array, 10);
			BitConverter.GetBytes(_zfe.ModifyTime.ToFileTime()).CopyTo(array, 12);
			BitConverter.GetBytes(_zfe.AccessTime.ToFileTime()).CopyTo(array, 20);
			BitConverter.GetBytes(_zfe.CreationTime.ToFileTime()).CopyTo(array, 28);
			return array;
		}

		// Token: 0x06000170 RID: 368 RVA: 0x0000BDB8 File Offset: 0x00009FB8
		private void UpdateCrcAndSizes(ArchiveManager.ZipFileEntry _zfe)
		{
			long position = this.ZipFileStream.Position;
			this.ZipFileStream.Position = (long)((ulong)(_zfe.HeaderOffset + 8U));
			this.ZipFileStream.Write(BitConverter.GetBytes((ushort)_zfe.Method), 0, 2);
			this.ZipFileStream.Position = (long)((ulong)(_zfe.HeaderOffset + 14U));
			this.ZipFileStream.Write(BitConverter.GetBytes(_zfe.Crc32), 0, 4);
			this.ZipFileStream.Write(BitConverter.GetBytes(_zfe.CompressedSize), 0, 4);
			this.ZipFileStream.Write(BitConverter.GetBytes(_zfe.FileSize), 0, 4);
			this.ZipFileStream.Position = position;
		}

		// Token: 0x06000171 RID: 369 RVA: 0x0000BE68 File Offset: 0x0000A068
		private string NormalizedFilename(string _filename)
		{
			string text = _filename.Replace('\\', '/');
			int num = text.IndexOf(':');
			if (num >= 0)
			{
				text = text.Remove(0, num + 1);
			}
			return text.Trim(new char[]
			{
				'/'
			});
		}

		// Token: 0x06000172 RID: 370 RVA: 0x0000BEAC File Offset: 0x0000A0AC
		private bool ReadFileInfo()
		{
			if (this.ZipFileStream.Length < 22L)
			{
				return false;
			}
			try
			{
				this.ZipFileStream.Seek(-17L, SeekOrigin.End);
				BinaryReader binaryReader = new BinaryReader(this.ZipFileStream);
				for (;;)
				{
					this.ZipFileStream.Seek(-5L, SeekOrigin.Current);
					if (binaryReader.ReadUInt32() == 101010256U)
					{
						break;
					}
					if (this.ZipFileStream.Position <= 0L)
					{
						goto Block_6;
					}
				}
				this.ZipFileStream.Seek(6L, SeekOrigin.Current);
				ushort existingFiles = binaryReader.ReadUInt16();
				int num = binaryReader.ReadInt32();
				uint num2 = binaryReader.ReadUInt32();
				ushort num3 = binaryReader.ReadUInt16();
				if (this.ZipFileStream.Position + (long)((ulong)num3) != this.ZipFileStream.Length)
				{
					return false;
				}
				this.ExistingFiles = existingFiles;
				this.CentralDirImage = new byte[num];
				this.ZipFileStream.Seek((long)((ulong)num2), SeekOrigin.Begin);
				this.ZipFileStream.Read(this.CentralDirImage, 0, num);
				this.ZipFileStream.Seek((long)((ulong)num2), SeekOrigin.Begin);
				return true;
				Block_6:;
			}
			catch
			{
			}
			return false;
		}

		public void SetPassword(string _password)
		{
			this.Password = _password;
		}

		// Token: 0x06000173 RID: 371 RVA: 0x0000BFCC File Offset: 0x0000A1CC
		public void Dispose()
		{
			this.Close();
		}

		// Token: 0x04000089 RID: 137
		public bool EncodeUTF8 = true;

		// Token: 0x0400008A RID: 138
		public bool ForceDeflating;

		// Token: 0x0400008B RID: 139
		private List<ArchiveManager.ZipFileEntry> Files = new List<ArchiveManager.ZipFileEntry>();

		// Token: 0x0400008C RID: 140
		private string FileName;

		// Token: 0x0400008D RID: 141
		private Stream ZipFileStream;

		// Token: 0x0400008E RID: 142
		private string Comment = string.Empty;

		private string Password = null;

		// Token: 0x0400008F RID: 143
		private byte[] CentralDirImage;

		// Token: 0x04000090 RID: 144
		private ushort ExistingFiles;

		// Token: 0x04000091 RID: 145
		private FileAccess Access;

		// Token: 0x04000092 RID: 146
		private bool leaveOpen;

		// Token: 0x04000093 RID: 147
		private static uint[] CrcTable;

		// Token: 0x04000094 RID: 148
		private static Encoding DefaultEncoding = Encoding.GetEncoding(437);
		
		private object _lock = new object();

		// Token: 0x0200005C RID: 92
		public enum Compression : ushort
		{
			// Token: 0x040001A0 RID: 416
			Store,
			// Token: 0x040001A1 RID: 417
			Deflate = 8
		}

		// Token: 0x0200005D RID: 93
		public class ZipFileEntry
		{
			// Token: 0x060001E1 RID: 481 RVA: 0x00010BDF File Offset: 0x0000EDDF
			public override string ToString()
			{
				return this.FilenameInZip;
			}

			// Token: 0x040001A2 RID: 418
			public ArchiveManager.Compression Method;

			// Token: 0x040001A3 RID: 419
			public string FilenameInZip;

			// Token: 0x040001A4 RID: 420
			public uint FileSize;

			// Token: 0x040001A5 RID: 421
			public uint CompressedSize;

			// Token: 0x040001A6 RID: 422
			public uint HeaderOffset;

			// Token: 0x040001A7 RID: 423
			public uint FileOffset;

			// Token: 0x040001A8 RID: 424
			public uint HeaderSize;

			// Token: 0x040001A9 RID: 425
			public uint Crc32;

			// Token: 0x040001AA RID: 426
			public DateTime ModifyTime;

			// Token: 0x040001AB RID: 427
			public DateTime CreationTime;

			// Token: 0x040001AC RID: 428
			public DateTime AccessTime;

			// Token: 0x040001AD RID: 429
			public string Comment;

			// Token: 0x040001AE RID: 430
			public bool EncodeUTF8;
		}

		private class AesCtrStream : Stream
		{
			private readonly Stream BaseStream;
			private readonly ICryptoTransform Encryptor;
			private readonly HMACSHA1 Hmac;
			private readonly byte[] Counter;
			private readonly byte[] Keystream;
			private int KeystreamPos;

			public AesCtrStream(Stream baseStream, byte[] key, byte[] salt, HMACSHA1 hmac)
			{
				this.BaseStream = baseStream;
				this.Hmac = hmac;
				this.Counter = new byte[16];
				this.Counter[0] = 1; 
				this.Keystream = new byte[16];
				this.KeystreamPos = 16;
				AesManaged aes = new AesManaged();
				aes.Mode = CipherMode.ECB;
				aes.Padding = PaddingMode.None;
				this.Encryptor = aes.CreateEncryptor(key, new byte[16]);
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				byte[] encrypted = new byte[count];
				for (int i = 0; i < count; i++)
				{
					if (this.KeystreamPos == 16)
					{
						this.Encryptor.TransformBlock(this.Counter, 0, 16, this.Keystream, 0);
						this.KeystreamPos = 0;
						for (int j = 0; j < 16; j++)
						{
							if (++this.Counter[j] != 0) break;
						}
					}
					encrypted[i] = (byte)(buffer[offset + i] ^ this.Keystream[this.KeystreamPos++]);
				}
				this.Hmac.TransformBlock(encrypted, 0, count, null, 0);
				this.BaseStream.Write(encrypted, 0, count);
			}

			public override bool CanRead { get { return false; } }
			public override bool CanSeek { get { return false; } }
			public override bool CanWrite { get { return true; } }
			public override long Length { get { throw new NotSupportedException(); } }
			public override long Position { get { throw new NotSupportedException(); } set { throw new NotSupportedException(); } }
			public override void Flush() { this.BaseStream.Flush(); }
			public override int Read(byte[] buffer, int offset, int count) { throw new NotSupportedException(); }
			public override void SetLength(long value) { throw new NotSupportedException(); }
			public override long Seek(long offset, SeekOrigin origin) { throw new NotSupportedException(); }
		}
	}
}
