using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
	public static class Family
	{
		private static readonly string[] MenNames = new string[24]
		{
			"Thomas", "James", "William", "John", "Henry", "Samuel", "Joseph", "Charles", "George", "Edward",
			"Walter", "Albert", "Frank", "Isaac", "Abel", "Silas", "Josiah", "Eli", "Ezra", "Patrick",
			"Michael", "Daniel", "Owen", "Luther"
		};

		private static readonly string[] WomenNames = new string[24]
		{
			"Mary", "Martha", "Sarah", "Elizabeth", "Anna", "Emma", "Clara", "Ruth", "Hattie", "Ida",
			"Nora", "Lottie", "Abigail", "Margaret", "Ellen", "Bridget", "Josephine", "Lucy", "Ada", "Florence",
			"Rose", "Esther", "Mae", "Cora"
		};

		private static readonly string[] Towns = new string[18]
		{
			"Saint Denis", "Saint Denis", "Saint Denis", "Saint Denis", "Saint Denis", "Saint Denis", "Valentine", "Valentine", "Rhodes", "Rhodes",
			"Blackwater", "Blackwater", "Annesburg", "Strawberry", "Van Horn", "Armadillo", "Lagras", "Emerald Ranch"
		};

		private static readonly string[] FarTowns = new string[8] { "Philadelphia", "New York", "Boston", "Chicago", "St. Louis", "Kansas City", "San Francisco", "Baltimore" };

		private static readonly string[] IrishTowns = new string[4] { "County Cork, back in Ireland", "Dublin", "County Wexford, back in Ireland", "Boston" };

		private static readonly string[] MexicanTowns = new string[4] { "Chuparosa", "Escalera", "Chihuahua", "El Paso" };

		private static readonly string[] WomenLines = new string[10] { "keeps a boarding house", "sews for a dressmaker", "teaches school", "takes in washing", "nurses at the hospital", "sings in the church choir", "married a banker and won't let anyone forget it", "runs a little bakery", "keeps house for a widower", "writes every month" };

		private static readonly string[] MenLines = new string[8] { "tends bar", "wears a deputy's badge", "clerks at a bank", "preaches on Sundays", "keeps a livery stable", "lays track for the railroad", "drinks more than he should", "practises law, and won't let anyone forget it" };

		private static readonly string[] CityMenLines = new string[3] { "works the docks", "drives a streetcar", "rolls cigars at the factory" };

		private static readonly string[] ParentLines = new string[5] { "is getting on in years", "prays for {p} every night", "hasn't spoken to {p} in years", "writes every month", "keeps a candle in the window for {p}" };

		public static List<Relative> For(Person p)
		{
			Random random = new Random(Voice.Hash((p.Id ?? p.Name ?? "?") + "|family"));
			List<Relative> list = new List<Relative>();
			string text = (string.IsNullOrEmpty(p.Surname) ? "" : (" " + p.Surname));
			int age = p.AgeIn(Voice.NowYear);
			if (p.Family == "married")
			{
				string text2 = ((string.IsNullOrEmpty(p.SpouseName) || p.SpouseName.StartsWith("a ")) ? (p.Woman ? MenNames : WomenNames)[random.Next(MenNames.Length)] : p.SpouseName.Split(' ')[0]);
				list.Add(new Relative
				{
					Name = text2 + text,
					Relation = (p.Woman ? "husband" : "wife"),
					Woman = !p.Woman,
					Town = ((!string.IsNullOrEmpty(p.Home)) ? p.Home : Towns[random.Next(Towns.Length)]),
					Line = ((p.Children > 0) ? "raises the children" : "keeps the house")
				});
			}
			int num = ((random.Next(100) >= 20) ? ((random.Next(100) < 60) ? 1 : 2) : 0);
			List<string> list2 = new List<string>();
			for (int i = 0; i < num; i++)
			{
				string text3 = PickRelation(random, age, p, list2);
				if (text3 == null)
				{
					break;
				}
				list2.Add(text3);
				int num2;
				switch (text3)
				{
				case "cousin":
					num2 = ((random.Next(2) == 0) ? 1 : 0);
					break;
				default:
					num2 = 0;
					break;
				case "mother":
				case "sister":
				case "aunt":
				case "daughter":
				case "niece":
					num2 = 1;
					break;
				}
				bool flag = (byte)num2 != 0;
				string text4 = (flag ? WomenNames : MenNames)[random.Next(MenNames.Length)];
				string text5 = text;
				if (flag)
				{
					switch (text3)
					{
					case "sister":
					case "aunt":
					case "daughter":
					case "cousin":
						if (random.Next(100) < 35)
						{
							text5 = " " + (new string[8] { "Hale", "Doyle", "Whitaker", "Lambert", "Moreau", "Fischer", "Barlow", "Quinn" })[random.Next(8)];
						}
						break;
					}
				}
				string text6 = PickTown(random, p);
				string[] array;
				switch (text6)
				{
				default:
					array = MenLines;
					break;
				case "Saint Denis":
				case "New York":
				case "Chicago":
				case "Philadelphia":
					array = MenLines.Concat(CityMenLines).ToArray();
					break;
				}
				string[] array2 = array;
				string line = ((text3 == "mother" || text3 == "father") ? ParentLines[random.Next(ParentLines.Length)] : (flag ? WomenLines[random.Next(WomenLines.Length)] : array2[random.Next(array2.Length)]));
				list.Add(new Relative
				{
					Name = text4 + text5,
					Relation = text3,
					Woman = flag,
					Town = text6,
					Line = line
				});
			}
			return list;
		}

		private static string PickRelation(Random rnd, int age, Person p, List<string> used)
		{
			List<string> list = new List<string>();
			if (age < 45)
			{
				list.AddRange(new string[4] { "mother", "mother", "mother", "father" });
			}
			list.AddRange(new string[5] { "brother", "brother", "sister", "sister", "cousin" });
			if (age >= 30)
			{
				list.AddRange(new string[2] { "uncle", "aunt" });
			}
			if (age >= 42 && p.Children > 0)
			{
				list.AddRange(new string[4] { "son", "daughter", "son", "daughter" });
			}
			list.RemoveAll((string o) => used.Contains(o) && (o == "mother" || o == "father"));
			if (list.Count != 0)
			{
				return list[rnd.Next(list.Count)];
			}
			return null;
		}

		private static string PickTown(Random rnd, Person p)
		{
			string text = p.Birthplace ?? string.Empty;
			int num = rnd.Next(100);
			if (text.Contains("Ireland") && num < 50)
			{
				return IrishTowns[rnd.Next(IrishTowns.Length)];
			}
			if ((p.GangId == "delobo" || text.Contains("Mexico") || text.Contains("Nuevo Paraiso")) && num < 60)
			{
				return MexicanTowns[rnd.Next(MexicanTowns.Length)];
			}
			if (num < 22)
			{
				return FarTowns[rnd.Next(FarTowns.Length)];
			}
			string text2 = Towns[rnd.Next(Towns.Length)];
			if (text2 == p.Home)
			{
				text2 = (("Saint Denis" == p.Home) ? "Valentine" : "Saint Denis");
			}
			return text2;
		}

		public static string Mention(Person p, Relative r, string seed)
		{
			string text = (r.Woman ? "She" : "He");
			string text2 = (r.Town.Contains(",") ? ("in " + r.Town) : ("in " + r.Town));
			string firstName = r.FirstName;
			string text3 = (r.Line ?? "").Replace("{p}", "me");
			string text4 = ((!(r.Relation == "wife") && !(r.Relation == "husband")) ? ("My " + r.Relation + " " + firstName + " lives " + text2 + ". " + text + " " + text3 + ".") : ("My " + r.Relation + ", " + firstName + ", is " + text2 + ". " + text + " " + text3 + "."));
			if (p.IsOutlaw && Voice.Chance(seed + r.Name, 50))
			{
				text4 = text4 + " " + Voice.Pick(seed + r.Name, text + " thinks I'm in honest work.", "Haven't written in a year.", text + " keeps begging me to come home.");
			}
			return text4;
		}

		public static string Letter(Person p, MemoryItem m, PlayerRef you, string seed)
		{
			PlayerRef you2 = new PlayerRef
			{
				FullName = you.FullName,
				KnownByName = you.KnownByName,
				Scarred = you.Scarred
			};
			string text = Voice.ToPrivate(Voice.SpokenLine(p, m), p, you2);
			text = text.Replace("That scarred stranger", "A stranger with a scarred face").Replace("that scarred stranger", "a stranger with a scarred face").Replace("That stranger", "A stranger")
				.Replace("that stranger", "a stranger");
			if (you.KnownByName)
			{
				text = text.Replace(you.FullName, "a man called " + you.FullName);
			}
			return Grammar.UpperFirst(text);
		}

		public static string Reaction(Relative r, Person p, MemoryItem m, string seed)
		{
			bool flag = m.Emotion == "Anger" || m.Emotion == "Fear" || m.Emotion == "Grief" || m.Emotion == "Shame";
			string text = (r.Woman ? "She" : "He");
			string text2 = (p.Woman ? "her" : "him");
			if (m.Text == "fell for you")
			{
				return Voice.Pick(seed, text + "'s writing back, asking all about him.", text + " wants to meet the fella.", text + " says to bring him home for Sunday dinner.");
			}
			if (m.Text != null && m.Text.StartsWith("you broke"))
			{
				return Voice.Pick(seed, text + "'s writing back to say no man is worth it.", text + " says to come home.");
			}
			if (!flag)
			{
				return Voice.Pick(seed, text + "'s praying for the stranger who did right by " + text2 + ".", text + " read it twice.");
			}
			if (!r.Woman && (r.Relation == "brother" || r.Relation == "father" || r.Relation == "son" || r.Relation == "cousin" || r.Relation == "uncle"))
			{
				return Voice.Pick(seed, text + " says he'll come west and settle it himself.", text + "'s asking around about the man who did it.");
			}
			return Voice.Pick(seed, text + "'s worried sick.", text + "'s writing back, begging " + text2 + " to come home.", text + " can't sleep for worrying.");
		}
	}
}
