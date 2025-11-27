using System;
using System.Collections.Generic;
using System.Text;

namespace PhantomHarvest.Helper
{
	// Token: 0x0200001F RID: 31
	public class Asn1DerObject
	{
		// Token: 0x17000001 RID: 1
		// (get) Token: 0x0600008E RID: 142 RVA: 0x0000744B File Offset: 0x0000564B
		// (set) Token: 0x0600008F RID: 143 RVA: 0x00007453 File Offset: 0x00005653
		public Asn1Der.Type Type { get; set; }

		// Token: 0x17000002 RID: 2
		// (get) Token: 0x06000090 RID: 144 RVA: 0x0000745C File Offset: 0x0000565C
		// (set) Token: 0x06000091 RID: 145 RVA: 0x00007464 File Offset: 0x00005664
		public int Lenght { get; set; }

		public Asn1DerObject()
		{
			this.objects = new List<Asn1DerObject>();
		}

		// Token: 0x17000003 RID: 3
		// (get) Token: 0x06000092 RID: 146 RVA: 0x0000746D File Offset: 0x0000566D
		// (set) Token: 0x06000093 RID: 147 RVA: 0x00007475 File Offset: 0x00005675
		public List<Asn1DerObject> objects { get; set; }

		// Token: 0x17000004 RID: 4
		// (get) Token: 0x06000094 RID: 148 RVA: 0x0000747E File Offset: 0x0000567E
		// (set) Token: 0x06000095 RID: 149 RVA: 0x00007486 File Offset: 0x00005686
		public byte[] Data { get; set; }

		// Token: 0x06000096 RID: 150 RVA: 0x00007490 File Offset: 0x00005690
		public override string ToString()
		{
			StringBuilder stringBuilder = new StringBuilder();
			StringBuilder stringBuilder2 = new StringBuilder();
			Asn1Der.Type type = this.Type;
			switch (type)
			{
			case Asn1Der.Type.Integer:
			{
				foreach (byte b in this.Data)
				{
					stringBuilder2.AppendFormat("{0:X2}", b);
				}
				StringBuilder stringBuilder3 = stringBuilder;
				string str = "\tINTEGER ";
				StringBuilder stringBuilder4 = stringBuilder2;
				stringBuilder3.AppendLine(str + ((stringBuilder4 != null) ? stringBuilder4.ToString() : null));
				break;
			}
			case Asn1Der.Type.BitString:
			case Asn1Der.Type.Null:
				break;
			case Asn1Der.Type.OctetString:
			{
				foreach (byte b2 in this.Data)
				{
					stringBuilder2.AppendFormat("{0:X2}", b2);
				}
				StringBuilder stringBuilder5 = stringBuilder;
				string str2 = "\tOCTETSTRING ";
				StringBuilder stringBuilder6 = stringBuilder2;
				stringBuilder5.AppendLine(str2 + ((stringBuilder6 != null) ? stringBuilder6.ToString() : null));
				break;
			}
			case Asn1Der.Type.ObjectIdentifier:
			{
				foreach (byte b3 in this.Data)
				{
					stringBuilder2.AppendFormat("{0:X2}", b3);
				}
				StringBuilder stringBuilder7 = stringBuilder;
				string str3 = "\tOBJECTIDENTIFIER ";
				StringBuilder stringBuilder8 = stringBuilder2;
				stringBuilder7.AppendLine(str3 + ((stringBuilder8 != null) ? stringBuilder8.ToString() : null));
				break;
			}
			default:
				if (type == Asn1Der.Type.Sequence)
				{
					stringBuilder.AppendLine("SEQUENCE {");
				}
				break;
			}
			foreach (Asn1DerObject value in this.objects)
			{
				stringBuilder.Append(value);
			}
			if (this.Type.Equals(Asn1Der.Type.Sequence))
			{
				stringBuilder.AppendLine("}");
			}
			return stringBuilder.ToString();
		}
	}
}
