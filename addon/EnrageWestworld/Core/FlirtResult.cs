using System.Collections.Generic;

namespace EnrageWestworld.Core
{
	public sealed class FlirtResult
	{
		public string YouSay;

		public string Reply;

		public bool Liked;

		public List<KeyValuePair<Person, string>> Jealous = new List<KeyValuePair<Person, string>>();
	}
}
