using System.Collections.Generic;

namespace EnrageWestworld.Core
{
	public sealed class Folk
	{
		public string Id;

		public string Name;

		public string Town;

		public string Temper;

		public bool Woman;

		public bool Met;

		public bool Reported;

		public bool KeptQuiet;

		public int Grudge;

		public int Fear;

		public int Like;

		public int Favours;

		public List<string> Witnessed = new List<string>();

		public List<string> FavourNotes = new List<string>();

		public string Key => "folk:" + (Id ?? Name);
	}
}
