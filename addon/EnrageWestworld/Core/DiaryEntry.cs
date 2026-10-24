using System.Runtime.Serialization;

namespace EnrageWestworld.Core
{
	[DataContract]
	public sealed class DiaryEntry
	{
		[DataMember]
		public long At;

		[DataMember]
		public string Date;

		[DataMember]
		public string Text;
	}
}
