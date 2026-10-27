using System.Collections.Generic;
using System.Runtime.Serialization;

namespace EnrageWestworld.Core
{
	[DataContract]
	public sealed class Affair
	{
		[DataMember(EmitDefaultValue = false)]
		public string AId;

		[DataMember(EmitDefaultValue = false)]
		public string AName;

		[DataMember(EmitDefaultValue = false)]
		public bool AWoman;

		[DataMember(EmitDefaultValue = false)]
		public string BId;

		[DataMember(EmitDefaultValue = false)]
		public string BName;

		[DataMember(EmitDefaultValue = false)]
		public bool BWoman;

		[DataMember(EmitDefaultValue = false)]
		public string BLine;

		[DataMember(EmitDefaultValue = false)]
		public string Town;

		[DataMember(EmitDefaultValue = false)]
		public int Stage;

		[DataMember(EmitDefaultValue = false)]
		public int Heat;

		[DataMember(EmitDefaultValue = false)]
		public int BLikes;

		[DataMember(EmitDefaultValue = false)]
		public long Since;

		[DataMember(EmitDefaultValue = false)]
		public long At;

		[DataMember(EmitDefaultValue = false)]
		public string Ended;

		[DataMember(EmitDefaultValue = false)]
		public string RivalId;

		[DataMember(EmitDefaultValue = false)]
		public string RivalName;

		[DataMember(EmitDefaultValue = false)]
		public List<string> Said = new List<string>();

		public bool Active => Ended == null;

		public bool Offstage => BId == null;

		public bool Involves(string id)
		{
			if (id != null)
			{
				if (!(AId == id))
				{
					return BId == id;
				}
				return true;
			}
			return false;
		}

		public string Other(string id)
		{
			if (!(AId == id))
			{
				return AName;
			}
			return BName;
		}

		public string OtherId(string id)
		{
			if (!(AId == id))
			{
				return AId;
			}
			return BId;
		}

		public bool OtherWoman(string id)
		{
			if (!(AId == id))
			{
				return AWoman;
			}
			return BWoman;
		}
	}
}
