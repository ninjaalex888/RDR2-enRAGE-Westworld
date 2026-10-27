using System.Collections.Generic;
using System.Runtime.Serialization;

namespace EnrageWestworld.Core
{
	[DataContract]
	public sealed class Relative
	{
		[DataMember(EmitDefaultValue = false)]
		public string Name;

		[DataMember(EmitDefaultValue = false)]
		public string Relation;

		[DataMember(EmitDefaultValue = false)]
		public bool Woman;

		[DataMember(EmitDefaultValue = false)]
		public string Town;

		[DataMember(EmitDefaultValue = false)]
		public string Line;

		[DataMember(EmitDefaultValue = false)]
		public List<Hearsay> Heard = new List<Hearsay>();

		public string FirstName => Name.Split(' ')[0];
	}
}
