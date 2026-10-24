using System.Collections.Generic;
using System.Runtime.Serialization;

namespace EnrageWestworld.Core
{
	[DataContract]
	public sealed class Affair
	{
		[DataMember]
		public string AId;

		[DataMember]
		public string AName;

		[DataMember]
		public bool AWoman;

		[DataMember]
		public string BId;

		[DataMember]
		public string BName;

		[DataMember]
		public bool BWoman;

		[DataMember]
		public string BLine;

		[DataMember]
		public string Town;

		[DataMember]
		public int Stage;

		[DataMember]
		public int Heat;

		[DataMember]
		public int BLikes;

		[DataMember]
		public long Since;

		[DataMember]
		public long At;

		[DataMember]
		public string Ended;

		[DataMember]
		public string RivalId;

		[DataMember]
		public string RivalName;

		[DataMember]
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
