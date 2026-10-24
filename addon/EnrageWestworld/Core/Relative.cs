using System.Collections.Generic;
using System.Runtime.Serialization;

namespace EnrageWestworld.Core
{
	[DataContract]
	public sealed class Relative
	{
		[DataMember]
		public string Name;

		[DataMember]
		public string Relation;

		[DataMember]
		public bool Woman;

		[DataMember]
		public string Town;

		[DataMember]
		public string Line;

		[DataMember]
		public List<Hearsay> Heard = new List<Hearsay>();

		public string FirstName => Name.Split(' ')[0];
	}
}
