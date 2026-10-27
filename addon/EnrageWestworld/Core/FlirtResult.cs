using System.Collections.Generic;

namespace EnrageWestworld.Core
{
	public sealed class FlirtResult
	{
		public string YouSay;

		public string Reply;

		public bool Liked;

		public string Answer;   // yes, maybe or no (Interest)

		public string Why;      // the reason that weighed most

		public int Chance = -1; // the percent her answer came to (Interest.ForPlayer), -1 for the old rule

		public bool Tryst;      // a woman said yes: flirting with her again soon takes them somewhere quieter (Trysts.cs)

		public string TrystKind;   // saloon, taken or single

		public Person Who;      // who answered

		public int Heart;       // how fond of him they were already (0-3; FlirtVoices picks a voice by it)

		public List<KeyValuePair<Person, string>> Jealous = new List<KeyValuePair<Person, string>>();
	}
}
