using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
	public sealed class TalkContext
	{
		public string Seed = "";

		public long Now;

		public string Town;

		public List<Gossip> Gossip = new List<Gossip>();

		public bool Romance;

		public Func<Person, string[]> Intel;
	}
}
