using System.Collections.Generic;

namespace EnrageWestworld.Core
{
	public sealed class LifeEvent
	{
		public string Id;

		public string Kind;

		public string Title;

		public bool Heard;

		public long GameMin;

		public List<LifePart> People = new List<LifePart>();
	}
}
