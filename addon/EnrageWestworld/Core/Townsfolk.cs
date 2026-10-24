using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace EnrageWestworld.Core
{
	public static class Townsfolk
	{
		private static readonly Regex Saw = new Regex("^saw you (.+?)( - (.+?))?$");

		private static readonly Regex Masked = new Regex("^saw you (.+?) \\(masked\\) - then saw your face( - (.+?))?$");

		private static readonly Regex HeardFriend = new Regex("^heard you killed (his|her) friend (.+?)( - .+)?$");

		public static List<string> Lines(Folk f, PersonState st, PlayerRef player, string seed, bool romance = false)
		{
			List<string> list = new List<string>();
			seed = (f.Id ?? f.Name) + "|" + st.Talks + "|" + seed;
			string text = f.FavourNotes.LastOrDefault((string x) => !st.Has("fav:" + x));
			if (text != null)
			{
				st.Mark("fav:" + text);
				MemoryItem m = new MemoryItem
				{
					Text = text,
					Emotion = "Joy",
					Cause = "you"
				};
				Person p = new Person
				{
					Id = f.Key,
					Name = f.Name,
					Woman = f.Woman
				};
				list.Add(Voice.SpokenLine(p, m) + " " + Voice.Pick(seed, "Never did thank you proper.", "I owe you, mister.", "Won't forget it."));
			}
			string text2 = f.Witnessed.LastOrDefault((string x) => !st.Has("saw:" + x));
			if (text2 != null && list.Count == 0)
			{
				st.Mark("saw:" + text2);
				string text3 = Witness(f, text2, seed);
				if (text3 != null)
				{
					list.Add(text3);
				}
			}
			if (list.Count == 0 && f.Met && !st.KnowsName && st.Talks >= 1 && f.Grudge < 40)
			{
				st.KnowsName = true;
				Person p2 = new Person
				{
					Id = f.Key,
					Name = f.Name,
					Woman = f.Woman,
					Disposition = ((f.Like >= 20) ? "Friendly" : "Neutral")
				};
				list.AddRange(Voice.AskName(p2, player, seed));
			}
			else if (romance && list.Count == 0 && st.Heart != 0)
			{
				string text4 = Romance.FlirtLine(Shell(f), you: new PlayerRef
				{
					FullName = player.FullName,
					Scarred = player.Scarred,
					KnownByName = st.KnowsName
				}, heart: st.Heart, seed: seed + "flirt");
				if (text4 != null)
				{
					list.Add(text4);
				}
			}
			else if (list.Count == 0 && st.KnowsName && f.Like >= 25)
			{
				list.Add(Voice.Pick(seed, "Good to see you again, Mr. " + player.Surname + ".", "Mr. " + player.Surname + ". Always a pleasure.", player.FirstName + "! How've you been?"));
			}
			if (list.Count > 0 || f.Met)
			{
				st.Talks++;
			}
			return list;
		}

		public static string Witness(Folk f, string text, string seed)
		{
			Match match;
			if ((match = Masked.Match(text)).Success)
			{
				return "You wore a mask when you " + match.Groups[1].Value + ". But I saw your face." + Tail(f, seed);
			}
			if ((match = HeardFriend.Match(text)).Success)
			{
				return "Heard you killed my friend " + Person.FirstOf(match.Groups[2].Value) + "." + ((f.Grudge >= 30) ? " I won't forget that." : " Is it true?");
			}
			if (text.StartsWith("heard what you did"))
			{
				return "I heard what you did." + Tail(f, seed);
			}
			if ((match = Saw.Match(text)).Success)
			{
				string text2 = (match.Groups[3].Success ? match.Groups[3].Value.Split(',')[0].Trim() : null);
				string text3 = ((text2 != null && text2 != "the wilds") ? (" in " + text2) : string.Empty);
				return "I saw you " + match.Groups[1].Value + text3 + "." + Tail(f, seed);
			}
			return null;
		}

		private static string Tail(Folk f, string seed)
		{
			if (f.Fear >= 40)
			{
				return " " + Voice.Pick(seed, "Please... I keep to myself.", "I ain't said nothing to nobody.");
			}
			if (f.Reported)
			{
				return " I told the law. Figured you should know.";
			}
			if (f.KeptQuiet)
			{
				return " Never told a soul. Don't make me regret it.";
			}
			if (f.Grudge >= 40)
			{
				return " " + Voice.Pick(seed, "Folks like you make me sick.", "You ought to be ashamed.");
			}
			return " " + Voice.Pick(seed, "Ain't forgot it.", "Thought you should know.");
		}

		private static Person Shell(Folk f)
		{
			return new Person
			{
				Id = f.Key,
				Name = f.Name,
				Woman = f.Woman,
				Disposition = ((f.Like >= 20) ? "Friendly" : "Neutral")
			};
		}

		public static Remark RemarkFor(Folk f, PersonState st, string seed, bool romance = false, PlayerRef player = null)
		{
			if (romance && st.Heart != 0 && player != null && !st.Has("remheart:" + st.Heart))
			{
				st.Mark("remheart:" + st.Heart);
				PlayerRef you = new PlayerRef
				{
					FullName = player.FullName,
					Scarred = player.Scarred,
					KnownByName = st.KnowsName
				};
				Remark remark = Romance.RemarkFor(Shell(f), st.Heart, you, seed);
				if (remark != null)
				{
					return remark;
				}
			}
			string text = f.Witnessed.LastOrDefault((string x) => !st.Has("rem:" + x));
			if (text != null && (f.Fear >= 25 || f.Grudge >= 25))
			{
				st.Mark("rem:" + text);
				Match match = Saw.Match(text);
				string text2 = (match.Success ? match.Groups[1].Value : null);
				if (f.Fear >= f.Grudge)
				{
					return new Remark
					{
						Muttered = true,
						Key = text,
						Text = ((text2 != null) ? ("That's him. I saw him " + text2 + ".") : "That's him...")
					};
				}
				bool flag = text2 != null && (text2.Contains("kill") || text2.Contains("shoot"));
				Remark remark2 = new Remark();
				remark2.Key = text;
				remark2.Text = (flag ? Voice.Pick(seed, "Murderer.", "I know what you are.") : Voice.Pick(seed, "I know what you are.", "I seen what you did."));
				return remark2;
			}
			string text3 = f.FavourNotes.LastOrDefault((string x) => !st.Has("remfav:" + x));
			if (text3 != null)
			{
				st.Mark("remfav:" + text3);
				Remark remark2 = new Remark();
				remark2.Key = text3;
				remark2.Text = Voice.Pick(seed, "Hey, it's you! Thank you again, mister.", "That's the man who helped me!");
				return remark2;
			}
			return null;
		}
	}
}
