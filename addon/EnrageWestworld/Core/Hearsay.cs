using System.Runtime.Serialization;

namespace EnrageWestworld.Core
{
	[DataContract]
	public sealed class Hearsay
	{
		[DataMember(EmitDefaultValue = false)]
		public string FromId;

		[DataMember(EmitDefaultValue = false)]
		public string FromName;

		[DataMember(EmitDefaultValue = false)]
		public string Relation;

		[DataMember(EmitDefaultValue = false)]
		public bool FromWoman;

		[DataMember(EmitDefaultValue = false)]
		public string Text;

		[DataMember(EmitDefaultValue = false)]
		public int Hops;

		[DataMember(EmitDefaultValue = false)]
		public long At;

		[DataMember(EmitDefaultValue = false)]
		public bool Told;

		[DataMember(EmitDefaultValue = false)]
		public bool Thought;

		public string Key => FromId + "|" + Text;
	}
}
