using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
	public static class Talk
	{
		public static int Trust(Person p, PersonState st)
		{
			int num = st.Talks * 8 + p.TimesSpared * 15 + (p.OwesYou ? 25 : 0) - p.Hate - p.Fear / 2;
			if (p.Friendly)
			{
				num += 10;
			}
			if (p.Surly)
			{
				num -= 10;
			}
			num += p.Memories.Count((MemoryItem m) => m.AboutYou && m.Emotion == "Joy") * 10;
			num -= p.Memories.Count((MemoryItem m) => m.AboutYou && (m.Emotion == "Anger" || m.Emotion == "Grief")) * 10;
			return num - p.Memories.Count((MemoryItem m) => m.AboutYou && (m.Emotion == "Fear" || m.Emotion == "Shame")) * 8;
		}

		private static int Openness(Person p, PersonState st)
		{
			int num = Trust(p, st);
			if (p.Mood == "Drunk")
			{
				num += 35;
			}
			if (p.Has("braggart"))
			{
				num += 20;
			}
			if (p.Has("drunk") && p.Need("Drink") >= 60)
			{
				num += 10;
			}
			return num;
		}

		public static PlayerRef RefFor(Person p, PersonState st, PlayerRef player)
		{
			MemoryItem memoryItem = (from m in p.Memories
				where m.AboutYou && m.Intensity >= 30
				orderby m.Intensity descending
				select m).FirstOrDefault();
			Hearsay hearsay = st.Heard.OrderByDescending((Hearsay h) => h.At).FirstOrDefault();
			return new PlayerRef
			{
				FullName = player.FullName,
				Scarred = player.Scarred,
				KnownByName = st.KnowsName,
				Deed = memoryItem?.Text,
				Heard = ((hearsay != null) ? HeardClause(hearsay) : null)
			};
		}

		public static string HeardClause(Hearsay h)
		{
			string text = Voice.Retell(h.Text, h.FromWoman);
			if (!text.StartsWith("you ", StringComparison.OrdinalIgnoreCase))
			{
				return null;
			}
			string text2 = Person.FirstOf(h.FromName ?? "");
			string text3 = (h.FromWoman ? "her" : "him");
			string text4 = (h.FromWoman ? "her" : "his");
			string text5 = " " + text.Substring(4) + " ";
			if (text5.Contains(" " + text3 + " "))
			{
				text5 = ReplaceFirst(text5, " " + text3 + " ", " " + text2 + " ");
			}
			else if (text5.Contains(" " + text4 + " "))
			{
				text5 = ReplaceFirst(text5, " " + text4 + " ", " " + text2 + "'s ");
			}
			return "who" + text5.TrimEnd();
		}

		private static string ReplaceFirst(string s, string a, string b)
		{
			int num = s.IndexOf(a, StringComparison.Ordinal);
			if (num >= 0)
			{
				return s.Substring(0, num) + b + s.Substring(num + a.Length);
			}
			return s;
		}

		public static List<string> Next(Person p, PersonState st, PlayerRef player, TalkContext ctx)
		{
			List<string> list = new List<string>();
			string text = p.Id + "|" + st.Talks + "|" + ctx.Seed;
			PlayerRef playerRef = RefFor(p, st, player);
			int open = Openness(p, st);
			int num = ((!p.Outgoing || p.Surly) ? 1 : 2);
			if (p.Murderous || p.Hate >= 70)
			{
				MemoryItem memoryItem = StrongestAboutYou(p, st, true);
				if (memoryItem != null)
				{
					st.Mark("mem:" + memoryItem.Key);
					list.Add(Voice.MemoryToYou(p, memoryItem, text));
				}
				else
				{
					list.Add(Voice.Pick(text, "Walk away. While you still can.", "Nothing to talk about. Not with you.", "Keep moving, " + (st.KnowsName ? playerRef.Surname : "stranger") + "."));
				}
				Done(st, ctx);
				return list;
			}
			MemoryItem memoryItem2 = StrongestAboutYou(p, st, null);
			if (memoryItem2 != null && memoryItem2.Intensity >= 40)
			{
				st.Mark("mem:" + memoryItem2.Key);
				if (st.Talks == 0 && !st.KnowsName)
				{
					list.Add(Voice.Pick(text + "rec", "Hold on... I know you.", "Well, well. It's you.", "I remember you."));
				}
				list.Add(Voice.MemoryToYou(p, memoryItem2, text));
				Done(st, ctx);
				return list;
			}
			if (num > 0 && !st.Has("intro"))
			{
				st.Mark("intro");
				list.AddRange(Voice.Intro(p, text));
				Done(st, ctx);
				return list;
			}
			if ((p.Fear >= 50 || p.Memories.Any((MemoryItem m) => m.AboutYou && m.Emotion == "Fear" && m.Intensity >= 60)) && st.Talks < 4)
			{
				list.Add(Voice.Pick(text + "scared", "I ain't looking for a fight, mister.", "Please... I'd sooner be alone.", "Whatever it is, it wasn't me.", "What is it you're after?"));
				Done(st, ctx);
				return list;
			}
			if (num > 0 && !st.KnowsName && st.Talks >= 1)
			{
				st.KnowsName = true;
				st.NameAt = ctx.Now;
				list.AddRange(Voice.AskName(p, player, text));
				Done(st, ctx);
				return list;
			}
			if (p.Vengeful || p.Hate >= 40)
			{
				GoalItem goalItem = p.Goals.FirstOrDefault((GoalItem g) => g.Type == "Avenge" && g.Target == "you" && !st.Has("goal:" + g.Type));
				Hearsay hearsay = st.Heard.FirstOrDefault((Hearsay x) => !x.Told);
				ThoughtItem topThought = p.TopThought;
				if (goalItem != null)
				{
					st.Mark("goal:" + goalItem.Type);
					list.Add(Voice.GoalLine(p, goalItem, text));
				}
				else if (hearsay != null)
				{
					hearsay.Told = true;
					list.Add(Voice.HearsayToYou(p, Teller(hearsay), hearsay.Text, hearsay.FromWoman, text));
				}
				else if (topThought != null && !st.Has("mood:" + topThought.Text) && Voice.Chance(text + "mood", 50))
				{
					st.Mark("mood:" + topThought.Text);
					list.Add(Voice.MoodLine(p, topThought, text));
				}
				else
				{
					list.Add(Voice.Pick(text + "curt", "We got nothing to talk about.", "Spit it out, then.", "You're wasting my time.", "Move along."));
				}
				Done(st, ctx);
				return list;
			}
			if (ctx.Romance)
			{
				if (st.Heart == -1)
				{
					list.Add(Romance.FlirtLine(p, -1, playerRef, text));
					Done(st, ctx);
					return list;
				}
				if (st.Has("intel:due") && !st.Has("intel:told") && ctx.Intel != null)
				{
					st.Mark("intel:told");
					string[] array = ctx.Intel(p);
					string text2 = ((array != null) ? Romance.IntelLine(p.GangName, array[0], array[1], text) : null);
					if (text2 != null)
					{
						list.Add(text2);
						num--;
					}
				}
				if (num > 0 && st.JealousOf != null && !st.Has("jealous:" + st.JealousOf))
				{
					st.Mark("jealous:" + st.JealousOf);
					list.Add(Romance.JealousLine(st.JealousOf, text));
					num--;
				}
				if (num > 0 && st.KnowsWife && !st.Has("wife"))
				{
					st.Mark("wife");
					list.Add(Romance.WifeLine(text));
					num--;
				}
				if (num > 0 && st.Heart >= 1)
				{
					string text3 = Romance.FlirtLine(p, st.Heart, playerRef, text + "flirt");
					if (text3 != null)
					{
						list.Add(text3);
						num--;
					}
				}
			}
			HashSet<string> leads = new HashSet<string>();
			while (num > 0)
			{
				string text4 = NextTopic(p, st, playerRef, ctx, open, text + "|" + num, leads);
				if (text4 == null)
				{
					break;
				}
				list.Add(text4);
				num--;
			}
			if (list.Count == 0)
			{
				list.Add(Voice.MoodLine(p, null, text));
			}
			Done(st, ctx);
			return list;
		}

		private static void Done(PersonState st, TalkContext ctx)
		{
			st.Talks++;
			st.LastTalkAt = ctx.Now;
		}

		private static MemoryItem StrongestAboutYou(Person p, PersonState st, bool? bad)
		{
			return (from m in p.Memories
				where m.AboutYou && !st.Has("mem:" + m.Key) && (!bad.HasValue || bad.Value == (m.Emotion == "Anger" || m.Emotion == "Fear" || m.Emotion == "Grief" || m.Emotion == "Shame"))
				orderby m.Intensity descending
				select m).FirstOrDefault();
		}

		private static string NextTopic(Person p, PersonState st, PlayerRef you, TalkContext ctx, int open, string seed, HashSet<string> leads)
		{
			Hearsay hearsay = (from x in st.Heard
				where !x.Told
				orderby x.At descending
				select x).FirstOrDefault();
			if (hearsay != null)
			{
				hearsay.Told = true;
				string source = Teller(hearsay);
				return Voice.HearsayToYou(p, source, hearsay.Text, hearsay.FromWoman, seed);
			}
			foreach (PastItem item in p.Past)
			{
				if (st.Has("cover:" + item.Index) && !st.Has("unmask:" + item.Index) && open >= 45)
				{
					st.Mark("unmask:" + item.Index);
					st.Mark("past:" + item.Index);
					item.Known = true;
					return Voice.Unmask(p, item, seed);
				}
			}
			foreach (PastItem item2 in p.Past.OrderBy((PastItem x) => x.Year))
			{
				if (!st.Has("past:" + item2.Index) && !st.Has("cover:" + item2.Index) && !(item2.Kind == "born") && !(item2.Kind == "family"))
				{
					if (!item2.Secret)
					{
						st.Mark("past:" + item2.Index);
						item2.Known = true;
						return Voice.PastLine(p, item2, seed, leads);
					}
					if (open >= 40)
					{
						st.Mark("past:" + item2.Index);
						item2.Known = true;
						return Voice.Confide(p, item2, seed);
					}
					if (p.Facet("Honesty") < 35 && !p.Has("honest") && !st.Told.Any((string k) => k.StartsWith("cover:")))
					{
						st.Mark("cover:" + item2.Index);
						return Voice.Cover(p, item2, seed);
					}
				}
			}
			if (!st.Has("family"))
			{
				st.Mark("family");
				return Voice.FamilyLine(p, seed);
			}
			ThoughtItem topThought = p.TopThought;
			if (topThought != null && !st.Has("mood:" + topThought.Text))
			{
				st.Mark("mood:" + topThought.Text);
				return Voice.MoodLine(p, topThought, seed);
			}
			foreach (GoalItem goal in p.Goals)
			{
				bool flag = p.Personality == "Proud" || p.Personality == "Hothead";
				bool flag2 = goal.Type == "Avenge" && goal.Target == "you" && p.Hate >= 40;
				if (!st.Has("goal:" + goal.Type) && (open >= 30 || flag2 || (flag && (goal.Type == "Avenge" || goal.Type == "Prove"))))
				{
					st.Mark("goal:" + goal.Type);
					return Voice.GoalLine(p, goal, seed);
				}
			}
			if (st.Family == null)
			{
				st.Family = Family.For(p);
			}
			foreach (Relative item3 in st.Family)
			{
				if (!st.Has("rel:" + item3.Name) && (open >= 10 || item3.Relation == "wife" || item3.Relation == "husband"))
				{
					st.Mark("rel:" + item3.Name);
					return Family.Mention(p, item3, seed);
				}
			}
			foreach (Gossip item4 in ctx.Gossip)
			{
				if (!st.Has("gossip:" + item4.Key))
				{
					st.Mark("gossip:" + item4.Key);
					return item4.Line;
				}
			}
			return null;
		}

		public static string Teller(Hearsay h)
		{
			string text = Person.FirstOf(h.FromName ?? "someone");
			if (string.IsNullOrEmpty(h.Relation))
			{
				return text;
			}
			return "My " + h.Relation + " " + text;
		}
	}
}
