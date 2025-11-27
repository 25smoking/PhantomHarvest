using System;

namespace PhantomHarvest.Helper
{
	// Token: 0x0200002A RID: 42
	public class Login
	{
		// Token: 0x1700000E RID: 14
		// (get) Token: 0x060000FA RID: 250 RVA: 0x0000AB0F File Offset: 0x00008D0F
		// (set) Token: 0x060000FB RID: 251 RVA: 0x0000AB17 File Offset: 0x00008D17
		public int id { get; set; }

		// Token: 0x1700000F RID: 15
		// (get) Token: 0x060000FC RID: 252 RVA: 0x0000AB20 File Offset: 0x00008D20
		// (set) Token: 0x060000FD RID: 253 RVA: 0x0000AB28 File Offset: 0x00008D28
		public string hostname { get; set; }

		// Token: 0x17000010 RID: 16
		// (get) Token: 0x060000FE RID: 254 RVA: 0x0000AB31 File Offset: 0x00008D31
		// (set) Token: 0x060000FF RID: 255 RVA: 0x0000AB39 File Offset: 0x00008D39
		public string httpRealm { get; set; }

		// Token: 0x17000011 RID: 17
		// (get) Token: 0x06000100 RID: 256 RVA: 0x0000AB42 File Offset: 0x00008D42
		// (set) Token: 0x06000101 RID: 257 RVA: 0x0000AB4A File Offset: 0x00008D4A
		public string formSubmitURL { get; set; }

		// Token: 0x17000012 RID: 18
		// (get) Token: 0x06000102 RID: 258 RVA: 0x0000AB53 File Offset: 0x00008D53
		// (set) Token: 0x06000103 RID: 259 RVA: 0x0000AB5B File Offset: 0x00008D5B
		public string usernameField { get; set; }

		// Token: 0x17000013 RID: 19
		// (get) Token: 0x06000104 RID: 260 RVA: 0x0000AB64 File Offset: 0x00008D64
		// (set) Token: 0x06000105 RID: 261 RVA: 0x0000AB6C File Offset: 0x00008D6C
		public string passwordField { get; set; }

		// Token: 0x17000014 RID: 20
		// (get) Token: 0x06000106 RID: 262 RVA: 0x0000AB75 File Offset: 0x00008D75
		// (set) Token: 0x06000107 RID: 263 RVA: 0x0000AB7D File Offset: 0x00008D7D
		public string encryptedUsername { get; set; }

		// Token: 0x17000015 RID: 21
		// (get) Token: 0x06000108 RID: 264 RVA: 0x0000AB86 File Offset: 0x00008D86
		// (set) Token: 0x06000109 RID: 265 RVA: 0x0000AB8E File Offset: 0x00008D8E
		public string encryptedPassword { get; set; }

		// Token: 0x17000016 RID: 22
		// (get) Token: 0x0600010A RID: 266 RVA: 0x0000AB97 File Offset: 0x00008D97
		// (set) Token: 0x0600010B RID: 267 RVA: 0x0000AB9F File Offset: 0x00008D9F
		public string guid { get; set; }

		// Token: 0x17000017 RID: 23
		// (get) Token: 0x0600010C RID: 268 RVA: 0x0000ABA8 File Offset: 0x00008DA8
		// (set) Token: 0x0600010D RID: 269 RVA: 0x0000ABB0 File Offset: 0x00008DB0
		public int encType { get; set; }

		// Token: 0x17000018 RID: 24
		// (get) Token: 0x0600010E RID: 270 RVA: 0x0000ABB9 File Offset: 0x00008DB9
		// (set) Token: 0x0600010F RID: 271 RVA: 0x0000ABC1 File Offset: 0x00008DC1
		public long timeCreated { get; set; }

		// Token: 0x17000019 RID: 25
		// (get) Token: 0x06000110 RID: 272 RVA: 0x0000ABCA File Offset: 0x00008DCA
		// (set) Token: 0x06000111 RID: 273 RVA: 0x0000ABD2 File Offset: 0x00008DD2
		public long timeLastUsed { get; set; }

		// Token: 0x1700001A RID: 26
		// (get) Token: 0x06000112 RID: 274 RVA: 0x0000ABDB File Offset: 0x00008DDB
		// (set) Token: 0x06000113 RID: 275 RVA: 0x0000ABE3 File Offset: 0x00008DE3
		public long timePasswordChanged { get; set; }

		// Token: 0x1700001B RID: 27
		// (get) Token: 0x06000114 RID: 276 RVA: 0x0000ABEC File Offset: 0x00008DEC
		// (set) Token: 0x06000115 RID: 277 RVA: 0x0000ABF4 File Offset: 0x00008DF4
		public int timesUsed { get; set; }

		// Token: 0x1700001C RID: 28
		// (get) Token: 0x06000116 RID: 278 RVA: 0x0000ABFD File Offset: 0x00008DFD
		// (set) Token: 0x06000117 RID: 279 RVA: 0x0000AC05 File Offset: 0x00008E05
		public string syncCounter { get; set; }

		// Token: 0x1700001D RID: 29
		// (get) Token: 0x06000118 RID: 280 RVA: 0x0000AC0E File Offset: 0x00008E0E
		// (set) Token: 0x06000119 RID: 281 RVA: 0x0000AC16 File Offset: 0x00008E16
		public string everSynced { get; set; }

		// Token: 0x1700001E RID: 30
		// (get) Token: 0x0600011A RID: 282 RVA: 0x0000AC1F File Offset: 0x00008E1F
		// (set) Token: 0x0600011B RID: 283 RVA: 0x0000AC27 File Offset: 0x00008E27
		public string encryptedUnknownFields { get; set; }
	}
}
