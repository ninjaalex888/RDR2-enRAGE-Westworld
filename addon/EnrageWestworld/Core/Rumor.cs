using System.Runtime.Serialization;

namespace EnrageWestworld.Core
{
	[DataContract]
	public sealed class Rumor
	{
		[DataMember(EmitDefaultValue = false)]
		public string FromId;

		[DataMember(EmitDefaultValue = false)]
		public bool NotByYou;   // a death someone else caused (an execution on the road): no avenger, no grief letter to him

		[DataMember(EmitDefaultValue = false)]
		public string FromName;

		[DataMember(EmitDefaultValue = false)]
		public bool FromWoman;

		[DataMember(EmitDefaultValue = false)]
		public string Text;

		[DataMember(EmitDefaultValue = false)]
		public int Hops = 1;

		[DataMember(EmitDefaultValue = false)]
		public string ToId;

		[DataMember(EmitDefaultValue = false)]
		public string ToRelative;

		[DataMember(EmitDefaultValue = false)]
		public string Relation;

		[DataMember(EmitDefaultValue = false)]
		public string How;

		[DataMember(EmitDefaultValue = false)]
		public long SentAt;

		[DataMember(EmitDefaultValue = false)]
		public long DueAt;
	}
}
