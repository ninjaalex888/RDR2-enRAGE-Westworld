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

		/// enRAGE's own flag (set when it reads the event as history); never set by Core.
		public bool IsPast;

		public List<LifePart> People = new List<LifePart>();
	}
}
