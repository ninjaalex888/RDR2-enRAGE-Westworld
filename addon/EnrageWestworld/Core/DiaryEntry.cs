using System.Runtime.Serialization;

namespace EnrageWestworld.Core
{
	[DataContract]
	public sealed class DiaryEntry
	{
		[DataMember(EmitDefaultValue = false)]
		public long At;

		[DataMember(EmitDefaultValue = false)]
		public string Date;

		[DataMember(EmitDefaultValue = false)]
		public string Text;
	}
}
