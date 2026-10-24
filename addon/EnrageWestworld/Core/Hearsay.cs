using System.Runtime.Serialization;

namespace EnrageWestworld.Core
{
	[DataContract]
	public sealed class Hearsay
	{
		[DataMember]
		public string FromId;

		[DataMember]
		public string FromName;

		[DataMember]
		public string Relation;

		[DataMember]
		public bool FromWoman;

		[DataMember]
		public string Text;

		[DataMember]
		public int Hops;

		[DataMember]
		public long At;

		[DataMember]
		public bool Told;

		[DataMember]
		public bool Thought;

		public string Key => FromId + "|" + Text;
	}
}
