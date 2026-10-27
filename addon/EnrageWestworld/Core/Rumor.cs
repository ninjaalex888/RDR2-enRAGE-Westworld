using System.Runtime.Serialization;

namespace EnrageWestworld.Core
{
	[DataContract]
	public sealed class Rumor
	{
		[DataMember]
		public string FromId;

		[DataMember]
		public bool NotByYou;   // a death someone else caused (an execution on the road): no avenger, no grief letter to him

		[DataMember]
		public string FromName;

		[DataMember]
		public bool FromWoman;

		[DataMember]
		public string Text;

		[DataMember]
		public int Hops = 1;

		[DataMember]
		public string ToId;

		[DataMember]
		public string ToRelative;

		[DataMember]
		public string Relation;

		[DataMember]
		public string How;

		[DataMember]
		public long SentAt;

		[DataMember]
		public long DueAt;
	}
}
