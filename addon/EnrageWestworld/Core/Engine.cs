using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace EnrageWestworld.Core
{
	public sealed partial class Engine
	{
		public readonly Store Store;

		public readonly PlayerRef Player;

		public readonly Settings Cfg;

		public Func<long, string> DateText = (long m) => "Day " + m / 1440;

		public Action<string> Log = delegate
		{
		};

		public readonly List<Toast> Out = new List<Toast>();

		public readonly HashSet<string> DiaryChanged = new HashSet<string>();

		private Dictionary<string, Person> _byId = new Dictionary<string, Person>();

		private readonly Random _rng;

		public string CurrentTown;

		public float PlayerX, PlayerY, PlayerZ;   // where the player is, for deeds the engine notices itself

		public bool John;

		public readonly List<EnrageEffect> Effects = new List<EnrageEffect>();

		public Func<Person, string[]> IntelFor;

		public Func<string, bool> FolkDead;   // one of enRAGE's townsfolk has died, by "folk:" key (the game side asks enRAGE)

		private static readonly string[] Towns = new string[7] { "Valentine", "Saint Denis", "Rhodes", "Strawberry", "Annesburg", "Blackwater", "Armadillo" };

		public IEnumerable<Person> People => _byId.Values;

		public Engine(Store store, PlayerRef player, Settings cfg, int seed)
		{
			Store = store;
			Player = player;
			Cfg = cfg;
			_rng = new Random(seed);
			InnerLife.Counterpart = LinkedName;   // worries name the real person linked to them (Links.cs)
			InnerLife.CounterpartDead = LinkedDead;
			HookMindFirst();   // the mind's own thoughts first, with [Mind] MindLive on (Engine.MindLive.cs)
		}

		public Person Find(string id)
		{
			if (id == null || !_byId.TryGetValue(id, out var value))
			{
				return null;
			}
			return value;
		}

		/// Whether the last Observe was given the roster (false standing alone, with enRAGE absent: the roster-side work waits).
		public bool RosterKnown { get; private set; } = true;

		public void Observe(IList<Person> roster, long now)
		{
			if (now <= 0)
			{
				return;
			}
			// standing alone (2026-10-29, roadmap 34): a null roster means none is known, not that everyone is gone; what reads the
			// roster (the records' memories, deaths and romances, their diaries and word of mouth due) waits for enRAGE
			RosterKnown = roster != null;
			if (roster == null)
			{
				roster = new Person[0];
			}
			if (Store.InstalledAt == 0L)
			{
				Store.InstalledAt = now;
			}
			if (Store.LastGameMin > 0 && now < Store.LastGameMin - 120)
			{
				int num = Store.Rewind(now);
				ForgetThinking();   // what the minds thought after the cut hasn't happened either (Engine.Minds.cs)
				Log("Game time went back (" + Store.WasAt + " -> " + now + "): an older save was loaded. Rolled back " + num + " things that hadn't happened yet.");
			}
			Store.LastGameMin = now;
			_byId = new Dictionary<string, Person>();
			foreach (Person item in roster)
			{
				if (item != null && item.Id != null)
				{
					_byId[item.Id] = item;
				}
			}
			foreach (Person item2 in RosterKnown ? roster : new Person[0])
			{
				if (item2 == null || item2.Id == null || !item2.IsAlive || item2.Memories == null)
				{
					continue;
				}
				foreach (MemoryItem memory in item2.Memories)
				{
					if (!memory.AboutYou || memory.Intensity < 30)
					{
						continue;
					}
					PersonState personState = Store.Get(item2);
					if (personState.Spread.Contains(memory.Key))
					{
						continue;
					}
					personState.Spread.Add(memory.Key);
					if (personState.Spread.Count > 40)
					{
						personState.Spread.RemoveRange(0, personState.Spread.Count - 40);
					}
					if (memory.At >= Store.InstalledAt)
					{
						Log("NEW " + item2.Name + ": " + memory.Emotion + " " + memory.Intensity + " \"" + memory.Text + "\"");
						MemoryMovesHeart(item2, personState, memory, now);
						bool flag = memory.Text == "fell for you" || memory.Text.StartsWith("you broke");
						// written that evening and told an hour on, if they live that long (Knowing.cs); before, both
						// happened in the same second, so a man killed half a minute after a killing had it in his diary
						if (Cfg.Diaries && memory.Intensity >= 40 && !flag)
						{
							Later(LaterAct.Of("diary", item2.Id, memory, Knowing.DiaryDue(Math.Max(memory.At, now), item2.Id + memory.Key)));
						}
						if (Cfg.WordOfMouth)
						{
							Later(LaterAct.Of("word", item2.Id, memory, Math.Max(memory.At, now) + Knowing.WordAfterMinutes));
						}
					}
				}
			}
			if (RosterKnown)
			{
				ObserveDeaths(roster, now);
			}
			Deliver(now);
			DueLater(now);
			if ((Cfg.Romance || Cfg.NpcRomance) && RosterKnown)
			{
				RomanceTick(roster, now);
			}
			WorldTick(now);
		}

		private void Later(LaterAct a)
		{
			Store.Later.Add(a);
			if (Store.Later.Count > Knowing.Max)
			{
				Store.Later.RemoveRange(0, Store.Later.Count - Knowing.Max);
			}
		}

		/// The diary entries and word of mouth whose time has come, from the living only: the dead write and tell nothing.
		private void DueLater(long now)
		{
			if (Store.Later.Count == 0)
			{
				return;
			}
			foreach (LaterAct a in Store.Later.Where((LaterAct x) => x.DueAt <= now).ToList())
			{
				if (a.Kind != "news" && !RosterKnown)
				{
					continue;   // a record's diary or word, kept for when enRAGE is back (standing alone)
				}
				Store.Later.Remove(a);
				if (a.Kind == "news")
				{
					// what a town hears once it's found (an execution on the road): about the dead, so no one need be alive
					Push(new Toast { Kind = "life", Title = a.Title, PersonId = a.PersonId, Weight = 40, Text = a.Text });
					Log("NEWS " + a.Title + ": " + a.Text);
					continue;
				}
				Person p = Find(a.PersonId);
				if (p == null || !p.IsAlive || TellsNothing(p.Id))   // enRAGE's flag, or our own ledger when its sync missed the death
				{
					Log("LATER " + (p != null ? p.Name : a.PersonId) + " didn't live to " + (a.Kind == "diary" ? "write of" : "tell") + " \"" + a.Text + "\"");
					continue;
				}
				MemoryItem m = a.Memory();
				if (a.Kind == "diary" && Cfg.Diaries)
				{
					WriteDiary(p, Store.Get(p), m, now);
				}
				else if (a.Kind == "word" && Cfg.WordOfMouth)
				{
					Spread(p, m, now);
				}
			}
		}

		// what the corner has said today about each thing that happened (Knowing.Topic -> game day), so five friends
		// grieving one man make one notice; not saved
		private readonly Dictionary<string, long> _cornerTopics = new Dictionary<string, long>();

		/// Whether a notice about this may still be shown today; marks it shown when it may.
		private bool CornerFresh(string text, long now)
		{
			string topic = Knowing.Topic(text);
			long day;
			if (topic.Length == 0 || (_cornerTopics.TryGetValue(topic, out day) && day == now / 1440))
			{
				return false;
			}
			_cornerTopics[topic] = now / 1440;
			if (_cornerTopics.Count > 200)
			{
				_cornerTopics.Clear();
			}
			return true;
		}

		private void WriteDiary(Person p, PersonState st, MemoryItem m, long now)
		{
			if (!(p.Smarts == "Dim") || Voice.Hash(p.Id + "letters") % 100 >= 70)
			{
				PlayerRef you = Talk.RefFor(p, st, Player);
				string date = DateText(now);
				string text = Voice.Diary(p, m, you, date, p.Id + m.Key);
				st.Diary.Add(new DiaryEntry
				{
					At = now,
					Date = date,
					Text = text
				});
				if (st.Diary.Count > 60)
				{
					st.Diary.RemoveAt(0);
				}
				DiaryChanged.Add(p.Id);
				st.Thought.Add(m.Key);
				PushAbout(p, new Toast
				{
					Kind = "diary",
					Title = p.Name + "'s diary",
					Text = Short(text.Substring(text.IndexOf(" - ", StringComparison.Ordinal) + 3)),
					PersonId = p.Id,
					Weight = m.Intensity
				});
			}
		}

		private void Spread(Person p, MemoryItem m, long now)
		{
			foreach (KeyValuePair<Person, string> item in Hearers(p))
			{
				Store.Rumors.Add(new Rumor
				{
					FromId = p.Id,
					FromName = p.Name,
					FromWoman = p.Woman,
					Text = m.Text,
					Hops = 1,
					ToId = item.Key.Id,
					Relation = item.Value,
					How = "word",
					SentAt = now,
					DueAt = now + 60 + _rng.Next(300)
				});
			}
			if (m.Intensity >= 45)
			{
				PersonState personState = Store.Get(p);
				if (personState.Family == null)
				{
					personState.Family = Family.For(p);
				}
				Relative relative = personState.Family.OrderBy((Relative x) => (!(x.Relation == "wife") && !(x.Relation == "husband")) ? ((x.Relation == "mother") ? 1 : 2) : 0).FirstOrDefault();
				if (relative != null && Voice.Hash(p.Id + m.Key + "letter") % 100 < 75)
				{
					bool flag = m.Intensity >= 70 && (m.Emotion == "Fear" || m.Emotion == "Grief");
					Store.Rumors.Add(new Rumor
					{
						FromId = p.Id,
						FromName = p.Name,
						FromWoman = p.Woman,
						Text = m.Text,
						Hops = 1,
						ToRelative = relative.Name,
						How = (flag ? "telegram" : "letter"),
						SentAt = now,
						DueAt = now + (flag ? (120 + _rng.Next(240)) : (1440 + _rng.Next(2880)))
					});
				}
			}
		}

		private IEnumerable<KeyValuePair<Person, string>> Hearers(Person p)
		{
			HashSet<string> seen = new HashSet<string>();
			Person person = Find(p.KinId);
			if (person != null && person.IsAlive && seen.Add(person.Id))
			{
				yield return new KeyValuePair<Person, string>(person, Gendered(p.KinRelation ?? "kin", p.Woman));
			}
			foreach (Person value in _byId.Values)
			{
				if (value.KinId == p.Id && value.IsAlive && seen.Add(value.Id))
				{
					yield return new KeyValuePair<Person, string>(value, Gendered(Inverse(value.KinRelation), p.Woman));
				}
			}
			foreach (string item in p.FriendIds ?? new List<string>())
			{
				Person person2 = Find(item);
				if (person2 != null && person2.IsAlive && seen.Add(person2.Id))
				{
					yield return new KeyValuePair<Person, string>(person2, "friend");
				}
			}
		}

		private static string Inverse(string rel)
		{
			switch (rel)
			{
			case "uncle":
				return "nephew";
			case "nephew":
				return "uncle";
			case "father":
				return "son";
			case "son":
				return "father";
			case null:
				return "kin";
			default:
				return rel;
			}
		}

		private static string Gendered(string rel, bool woman)
		{
			if (!woman)
			{
				return rel;
			}
			switch (rel)
			{
			case "brother":
				return "sister";
			case "uncle":
				return "aunt";
			case "nephew":
				return "niece";
			case "father":
				return "mother";
			case "son":
				return "daughter";
			default:
				return rel;
			}
		}

		public void Deliver(long now)
		{
			foreach (Rumor item in Store.Rumors.Where((Rumor r) => r.DueAt <= now).ToList())
			{
				if (!RosterKnown && item.ToId != null && item.How != "death" && Find(item.ToId) == null)
				{
					continue;   // word for one of enRAGE's people, kept for when enRAGE is back (standing alone)
				}
				Store.Rumors.Remove(item);
				if (item.How == "death")
				{
					DeliverDeath(item, now);
				}
				else if (item.FromId != null && TellsNothing(item.FromId))
				{
					// the dead tell nothing and write no letters: a telling in flight when they died is lost with them
					// (Lex's players, 2026-10-28: an interrogated man killed afterwards went on "talking about how he was questioned")
					Log("WORD dropped: " + item.FromName + " is dead and tells nothing (" + item.How + ", hop " + item.Hops + "): " + item.Text);
				}
				else if (item.ToId != null)
				{
					DeliverToPerson(item, now);
				}
				else if (item.ToRelative != null)
				{
					DeliverToRelative(item, now);
				}
			}
		}

		private void DeliverToPerson(Rumor r, long now)
		{
			Person person = Find(r.ToId);
			if (person == null || !person.IsAlive)
			{
				return;
			}
			PersonState personState = Store.Get(person);
			Hearsay h = new Hearsay
			{
				FromId = r.FromId,
				FromName = r.FromName,
				FromWoman = r.FromWoman,
				Relation = r.Relation,
				Text = r.Text,
				Hops = r.Hops,
				At = now
			};
			if (personState.Heard.Any((Hearsay x) => x.Key == h.Key))
			{
				return;
			}
			personState.Heard.Add(h);
			if (personState.Heard.Count > 12)
			{
				personState.Heard.RemoveAt(0);
			}
			Log("WORD " + person.Name + " heard from " + r.FromName + " (" + r.Relation + ", hop " + r.Hops + "): " + r.Text);
			string text = Voice.Retell(r.Text, r.FromWoman);
			string text2 = ((r.Relation == "friend") ? "friend " : (r.Relation + " ")) + Person.FirstOf(r.FromName);
			PushAbout(Knows(person) ? person : Find(r.FromId), new Toast
			{
				Kind = "word",
				Title = "Word gets around",
				PersonId = person.Id,
				Weight = 30,
				Text = person.Name + " heard from " + person.His + " " + text2 + " that " + text + "."
			});
			if (r.Hops >= 2)
			{
				return;
			}
			string text3 = Voice.Named(Voice.Retell(r.Text, r.FromWoman), Person.FirstOf(r.FromName), r.FromWoman);
			foreach (KeyValuePair<Person, string> item in Hearers(person))
			{
				if (!(item.Key.Id == r.FromId))
				{
					Store.Rumors.Add(new Rumor
					{
						FromId = person.Id,
						FromName = person.Name,
						FromWoman = person.Woman,
						Text = Voice.Exaggerate(text3),
						Hops = r.Hops + 1,
						ToId = item.Key.Id,
						Relation = item.Value,
						How = "word",
						SentAt = now,
						DueAt = now + 120 + _rng.Next(480)
					});
				}
			}
		}

		private void DeliverToRelative(Rumor r, long now)
		{
			Person person = Find(r.FromId);
			PersonState personState = Store.Find(r.FromId);
			if (person != null && personState != null && personState.Family != null)
			{
				Relative relative = personState.Family.FirstOrDefault((Relative x) => x.Name == r.ToRelative);
				if (relative != null)
				{
					MemoryItem m = new MemoryItem
					{
						Text = r.Text,
						Emotion = EmotionOf(person, r.Text),
						Cause = "you"
					};
					relative.Heard.Add(new Hearsay
					{
						FromId = person.Id,
						FromName = person.Name,
						FromWoman = person.Woman,
						Text = r.Text,
						Hops = 1,
						At = now
					});
					PlayerRef you = Talk.RefFor(person, personState, Player);
					string text = ((r.How == "telegram") ? "a telegram" : "a letter");
					string text2 = RelationTo(relative, person);
					string text3 = Family.Letter(person, m, you, person.Id + r.Text);
					string text4 = Family.Reaction(relative, person, m, person.Id + r.Text + relative.Name);
					Log("LETTER " + person.Name + " -> " + relative.Name + " (" + relative.Relation + ", " + relative.Town + "): " + r.Text);
					PushAbout(person, new Toast
					{
						Kind = "letter",
						Title = "Word reaches " + ShortTown(relative.Town),
						PersonId = person.Id,
						Weight = 35,
						Text = relative.Name + " got " + text + " from " + (relative.Woman ? "her" : "his") + " " + text2 + " " + person.FirstName + ": \"" + text3 + "\" " + text4
					});
				}
			}
		}

		private static string ShortTown(string town)
		{
			int num = town.IndexOf(',');
			if (num <= 0)
			{
				return town;
			}
			return town.Substring(0, num);
		}

		/// What someone with this relation calls the other person: a mother's son or daughter, a brother's brother or sister.
		internal static string KinWord(string relation, bool otherWoman)
		{
			return RelationTo(new Relative { Relation = relation }, new Person { Woman = otherWoman });
		}

		internal static string RelationTo(Relative r, Person p)
		{
			bool woman = p.Woman;
			switch (r.Relation)
			{
			case "wife":
			case "husband":
				if (!woman)
				{
					return "husband";
				}
				return "wife";
			case "father":
			case "mother":
				if (!woman)
				{
					return "son";
				}
				return "daughter";
			case "son":
			case "daughter":
				if (!woman)
				{
					return "father";
				}
				return "mother";
			case "aunt":
			case "uncle":
				if (!woman)
				{
					return "nephew";
				}
				return "niece";
			case "brother":
			case "sister":
				if (!woman)
				{
					return "brother";
				}
				return "sister";
			default:
				return r.Relation;
			}
		}

		private static string EmotionOf(Person p, string text)
		{
			MemoryItem memoryItem = p.Memories.FirstOrDefault((MemoryItem x) => x.Text == text);
			if (memoryItem == null)
			{
				return "Anger";
			}
			return memoryItem.Emotion;
		}

		public List<string> Greet(Person p, long now, TalkContext ctx)
		{
			PersonState personState = Store.Get(p);
			ctx.Now = now;
			ctx.Romance = Cfg.Romance;
			ctx.Intel = IntelFor;
			ctx.Gossip.AddRange(RomanceGossip(p));
			Greeted(p, personState, now);
			List<string> list = WithExtras(p, personState, Talk.Next(p, personState, Player, ctx), now);
			Log("TALK " + p.Name + " (talk " + personState.Talks + ", trust " + Talk.Trust(p, personState) + "): " + string.Join(" | ", list));
			return list;
		}

		public void Sighted(Person p, long now, string town)
		{
			PersonState personState = Store.Get(p);
			if (now - personState.LastSeenAt > 120)
			{
				personState.Sightings++;
				if (personState.FirstSeenTown == null && !string.IsNullOrEmpty(town))
				{
					personState.FirstSeenTown = town;
					personState.FirstSeenAt = now;
				}
			}
			personState.LastSeenAt = now;
		}

		private Remark RemarkForCore(Person p, long now)
		{
			PersonState st = Store.Get(p);
			PlayerRef playerRef = Talk.RefFor(p, st, Player);
			string text = p.Id + "|rem|" + st.Told.Count;
			Remark early = EarlyRemark(p, st, playerRef, text, now);
			if (early != null)
			{
				return early;
			}
			MemoryItem memoryItem = (from x in p.Memories
				where x.AboutYou && x.Intensity >= 40 && !st.Has("rem:" + x.Key)
				orderby x.Intensity descending
				select x).FirstOrDefault();
			if (memoryItem != null)
			{
				st.Mark("rem:" + memoryItem.Key);
				return MemoryRemark(p, memoryItem, playerRef, text);
			}
			Hearsay hearsay = st.Heard.FirstOrDefault((Hearsay x) => !st.Has("rem:" + x.Key));
			if (hearsay != null)
			{
				st.Mark("rem:" + hearsay.Key);
				string text2 = Talk.HeardClause(hearsay);
				Remark remark;
				if (p.Fear >= 40 || p.Personality == "Coward")
				{
					remark = new Remark();
					remark.Muttered = true;
					remark.Key = hearsay.Key;
					remark.Text = ((text2 != null) ? ("That's him. The one " + text2 + ".") : ("That's him... the one " + Person.FirstOf(hearsay.FromName) + " told me about."));
					return remark;
				}
				remark = new Remark();
				remark.Key = hearsay.Key;
				remark.Text = Voice.Pick(text, "I know what you did to " + Person.FirstOf(hearsay.FromName) + ".", "You're the one " + (text2 ?? "they talk about") + ".");
				return remark;
			}
			Remark mid = MidRemark(p, st, playerRef, text, now);
			if (mid != null)
			{
				return mid;
			}
			if (p.Vengeful || p.Murderous)
			{
				Remark remark = new Remark();
				remark.Key = "glare";
				remark.Text = Voice.Pick(text, "You got some nerve showing your face here.", "Keep walking.", "I see you.");
				return remark;
			}
			if (Cfg.Romance && st.Heart != 0)
			{
				Remark remark2 = Romance.RemarkFor(p, st.Heart, playerRef, text + now / 120);
				if (remark2 != null)
				{
					return remark2;
				}
			}
			if (st.KnowsName && Talk.Trust(p, st) >= 25)
			{
				Remark remark = new Remark();
				remark.Key = "hello";
				remark.Text = Voice.Pick(text + now / 60, "Evening, " + playerRef.Surname + ".", "Mr. " + playerRef.Surname + ".", "Good to see you, " + playerRef.FirstName + ".").Replace("Evening", Greeting(now));
				return remark;
			}
			if (st.Sightings >= 3 && st.Talks == 0 && !st.Has("rem:again"))
			{
				st.Mark("rem:again");
				Remark remark = new Remark();
				remark.Key = "again";
				remark.Text = (p.Surly ? "You again." : Voice.Pick(text, "Seen you around.", "You keep turning up."));
				return remark;
			}
			return null;
		}

		private static string Greeting(long now)
		{
			int num = (int)(now / 60 % 24);
			if (num >= 12)
			{
				if (num >= 17)
				{
					return "Evening";
				}
				return "Afternoon";
			}
			return "Morning";
		}

		private static Remark MemoryRemark(Person p, MemoryItem m, PlayerRef you, string seed)
		{
			string text = Voice.Relative(m.Text, p.Woman);
			switch (m.Emotion)
			{
			case "Fear":
				return new Remark
				{
					Muttered = true,
					Key = m.Key,
					Text = ((text != null) ? ("It's him... " + text + ".") : "It's him. Don't look at him.")
				};
			case "Joy":
				return new Remark
				{
					Key = m.Key,
					Text = ((text != null && text.StartsWith("the man who")) ? ("Hey... you're " + text.Replace("the man who", "the one who") + ". Thank you.") : "Hey. I remember you. Thank you.")
				};
			case "Anger":
			case "Grief":
			{
				Remark remark = new Remark();
				remark.Key = m.Key;
				remark.Text = Voice.Pick(seed, "You. I remember you.", "I ain't forgot what you did.", "Look who it is.");
				return remark;
			}
			case "Shame":
			{
				Remark remark = new Remark();
				remark.Muttered = true;
				remark.Key = m.Key;
				remark.Text = Voice.Pick(seed, "Don't look at me.", "Not him. Not now.");
				return remark;
			}
			case "Pride":
			{
				Remark remark = new Remark();
				remark.Key = m.Key;
				remark.Text = ((m.Text == "killed you") ? "No. No, I killed you. I watched you die." : Voice.Pick(seed, "Remember me?", "Back for more?"));
				return remark;
			}
			default:
				return null;
			}
		}

		/// inView (0.6.3, Lex: every thought over the thinker's head): only people it says are in view now are asked, so a
		/// thought goes over a head and one of someone far off waits, unused, until he's seen.
		public Toast NextThought(long now, Func<string, bool> inView = null)
		{
			List<KeyValuePair<int, Func<Toast>>> list = new List<KeyValuePair<int, Func<Toast>>>();
			foreach (Person value in _byId.Values)
			{
				if (!value.IsAlive || (inView != null && !inView(value.Id)))
				{
					continue;
				}
				Person who = value;
				PersonState personState = Store.Find(value.Id);
				if (!Knowing.Met(personState))
				{
					continue;   // only people the player has met speak in the corner (Knowing.cs); before, strangers' grief filled it
				}
				foreach (MemoryItem item in value.Memories.Where((MemoryItem x) => x.AboutYou && x.Intensity >= 30))
				{
					if (!personState.Thought.Contains(item.Key))
					{
						MemoryItem mem = item;
						int key = item.Intensity + ((now - item.At < 1440) ? 25 : 0);
						list.Add(new KeyValuePair<int, Func<Toast>>(key, () => CornerFresh(mem.Text, now) ? MemoryThoughtToast(who, mem) : Skip(who, mem.Key)));
					}
				}
				foreach (Hearsay item2 in personState.Heard.Where((Hearsay x) => !x.Thought))
				{
					Hearsay hs = item2;
					list.Add(new KeyValuePair<int, Func<Toast>>(40, () =>
					{
						if (CornerFresh(hs.FromName + " " + hs.Text, now))
							return HearsayToast(who, hs);
						hs.Thought = true;   // the same thing, already said today by someone else
						return null;
					}));
				}
				if (personState.KnowsName && !personState.Thought.Contains("name") && personState.Heart < 2)
				{
					list.Add(new KeyValuePair<int, Func<Toast>>(30, () => NameToast(who)));
				}
				if (Cfg.Romance && personState.Heart != 0 && !personState.Thought.Contains("heart:" + personState.Heart))
				{
					list.Add(new KeyValuePair<int, Func<Toast>>(38, () => HeartToast(who)));
				}
				if (personState.Sightings >= 1 && !personState.Thought.Contains("sighting") && !value.Memories.Any((MemoryItem x) => x.AboutYou))
				{
					list.Add(new KeyValuePair<int, Func<Toast>>(20 + Math.Min(15, personState.Sightings * 3), () => SightingToast(who, now)));
				}
				foreach (Relative item3 in personState.Family ?? new List<Relative>())
				{
					foreach (Hearsay item4 in item3.Heard.Where((Hearsay x) => !x.Thought))
					{
						Relative rel = item3;
						Hearsay hh = item4;
						list.Add(new KeyValuePair<int, Func<Toast>>(25, () => ReplyToast(who, rel, hh)));
					}
				}
			}
			if (list.Count == 0)
			{
				return null;
			}
			list = list.OrderByDescending((KeyValuePair<int, Func<Toast>> o) => o.Key + _rng.Next(30)).ToList();
			Toast toast = list[0].Value();
			if (toast != null)
			{
				Log("THOUGHT " + toast.Title + ": " + toast.Text);
			}
			return toast;
		}

		/// A thought not shown, because the corner said the same today: marked thought, so it doesn't come back stale.
		private Toast Skip(Person p, string key)
		{
			Store.Get(p).Thought.Add(key);
			return null;
		}

		private Toast MemoryThoughtToast(Person p, MemoryItem m)
		{
			PersonState personState = Store.Get(p);
			personState.Thought.Add(m.Key);
			PlayerRef you = Talk.RefFor(p, personState, Player);
			return new Toast
			{
				Kind = "thought",
				Title = p.Name,
				PersonId = p.Id,
				Weight = m.Intensity,
				Text = Short(Voice.MemoryThought(p, m, you, p.Id + "|t"))
			};
		}

		private Toast HearsayToast(Person p, Hearsay h)
		{
			PersonState st = Store.Get(p);
			h.Thought = true;
			PlayerRef playerRef = Talk.RefFor(p, st, Player);
			playerRef.Heard = null;
			Toast toast = new Toast();
			toast.Kind = "thought";
			toast.Title = p.Name;
			toast.PersonId = p.Id;
			toast.Weight = 40;
			toast.Text = Short(Voice.HearsayThought(p, Person.FirstOf(h.FromName), h.Text, h.FromWoman, playerRef, p.Id + h.Key));
			return toast;
		}

		private Toast NameToast(Person p)
		{
			PersonState personState = Store.Get(p);
			personState.Thought.Add("name");
			int num = Talk.Trust(p, personState);
			MemoryItem memoryItem = (from x in p.Memories
				where x.AboutYou && x.Emotion != "Joy" && x.Emotion != "Pride"
				orderby x.Intensity descending
				select x).FirstOrDefault();
			string text = ((memoryItem != null && memoryItem.Emotion == "Fear") ? "Now I know who to be afraid of." : ((p.Hate >= 40 || memoryItem != null) ? "I'll remember that name." : ((num < 25) ? Voice.Pick(p.Id + "nm", "Something about him I can't place.", "I've heard that name somewhere.") : "Seems a decent sort.")));
			return new Toast
			{
				Kind = "thought",
				Title = p.Name,
				PersonId = p.Id,
				Weight = 30,
				Text = Player.FullName + ". So that's his name. " + text
			};
		}

		private Toast HeartToast(Person p)
		{
			PersonState personState = Store.Get(p);
			personState.Thought.Add("heart:" + personState.Heart);
			PlayerRef you = Talk.RefFor(p, personState, Player);
			string text = Romance.Thought(p, personState.Heart, you, p.Id + "|heart|" + personState.Heart);
			if (text != null)
			{
				return new Toast
				{
					Kind = "thought",
					Title = p.Name,
					PersonId = p.Id,
					Weight = 38,
					Text = text
				};
			}
			return null;
		}

		private Toast SightingToast(Person p, long now)
		{
			PersonState personState = Store.Get(p);
			personState.Thought.Add("sighting");
			string text = (string.IsNullOrEmpty(personState.FirstSeenTown) ? "on the road" : ("in " + personState.FirstSeenTown));
			string text2 = ((now - personState.FirstSeenAt < 1440) ? "today" : "the other day");
			string seed = p.Id + "sight";
			string text3 = ((p.YourNotoriety >= 50) ? Voice.Pick(seed, "Swear I've seen his face on a poster.", "Had the look of a man with a price on his head.") : ((!(p.Personality == "Coward") && p.Fear < 40) ? ((p.Personality == "Hothead" || p.Personality == "Proud") ? "Looked at me like he was better than me." : (p.Friendly ? "Tipped his hat. Seemed decent enough." : ((!p.Surly) ? (Player.Scarred ? "Scars all down his face. Quiet sort." : "Big fella. Looked like he'd seen a war.") : "Didn't like the look of him."))) : "Kept my head down till he passed."));
			string text4 = (Player.Scarred ? "a scarred stranger" : "a stranger");
			Toast toast = new Toast();
			toast.Kind = "thought";
			toast.Title = p.Name;
			toast.PersonId = p.Id;
			toast.Weight = 20;
			toast.Text = "Saw " + text4 + " " + text + " " + text2 + ". " + text3;
			return toast;
		}

		private Toast ReplyToast(Person p, Relative r, Hearsay h)
		{
			h.Thought = true;
			string text = (r.Woman ? "She" : "He");
			MemoryItem memoryItem = new MemoryItem
			{
				Text = h.Text,
				Emotion = EmotionOf(p, h.Text)
			};
			string text2 = ((memoryItem.Emotion != "Joy" && memoryItem.Emotion != "Pride") ? Voice.Pick(p.Id + r.Name, text + " wants me to come home.", text + " says to stay clear of the man.", text + " says I should've stayed in " + ShortTown(r.Town) + ".") : Voice.Pick(p.Id + r.Name, text + " says the Lord sent him.", text + " says not every stranger's a bad one."));
			Toast toast = new Toast();
			toast.Kind = "thought";
			toast.Title = p.Name;
			toast.PersonId = p.Id;
			toast.Weight = 25;
			toast.Text = "Letter from my " + r.Relation + " " + r.FirstName + " in " + ShortTown(r.Town) + ". " + text2;
			return toast;
		}

		/// The player knows them: he has met them (Knowing.Met). It used to count anyone with a memory of him, or who had
		/// heard of him, so news "of people you know" came from strangers.
		public bool Knows(Person p)
		{
			return p != null && Knowing.Met(Store.Find(p.Id));
		}

		/// Small fixes to event titles built from names: "hit the The Klan" -> "hit the Klan", doubled spaces.
		public static string Tidy(string title)
		{
			if (string.IsNullOrEmpty(title))
				return title;
			string t = System.Text.RegularExpressions.Regex.Replace(title, @"\b([Tt]he) The\b", "$1");
			t = System.Text.RegularExpressions.Regex.Replace(t, @"\b([Aa]) The\b", "the");
			while (t.Contains("  "))
				t = t.Replace("  ", " ");
			return t;
		}

		/// enRAGE's gang and faction events: wars, feuds, clashes, rivalries, unrest, rank changes, jobs. They are news about the
		/// world, not about one person's life, and are shown apart from the ticker (see WarNewsTo).
		internal static bool IsWarfare(string kind)
		{
			switch (kind)
			{
			case "WAR":
			case "FEUD":
			case "CLASH":
			case "RIVALRY":
			case "TURMOIL":
			case "RANKS":
			case "JOB":
			case "VDL":
				return true;
			default:
				return false;
			}
		}

		internal static string WarLabel(string kind)
		{
			switch (kind)
			{
			case "WAR": return "Gang war";
			case "FEUD": return "Feud";
			case "CLASH": return "Clash";
			case "RIVALRY": return "Rivals";
			case "TURMOIL": return "Unrest";
			case "RANKS": return "Gang ranks";
			case "JOB": return "Gang job";
			default: return "Gang news";
			}
		}

		public Toast LifeUpdate(LifeEvent ev)
		{
			if (Store.Announced.Contains(ev.Id))
			{
				return null;
			}
			Store.Announced.Add(ev.Id);
			if (Store.Announced.Count > 400)
			{
				Store.Announced.RemoveRange(0, 100);
			}
			if (ev.GameMin < Store.InstalledAt || string.IsNullOrEmpty(ev.Title))
			{
				return null;
			}
			if (ev.Heard && ev.Kind != "LIFE")
			{
				return null;
			}
			foreach (LifePart person2 in ev.People)
			{
				Person person = Find(person2.Id);
				if (person != null)
				{
					string text = Link(person);
					if (text != null)
					{
						string text2 = EffectText(person, person2.Effects);
						string text3 = Grammar.Sentence(Tidy(ev.Title)) + ((text2.Length > 0) ? (" " + text2) : "");
						Log("LIFE " + person.Name + " (" + text + "): " + text3);
						return new Toast
						{
							Kind = IsWarfare(ev.Kind) ? "world" : "life",
							Title = IsWarfare(ev.Kind) ? WarLabel(ev.Kind) : text,
							PersonId = person.Id,
							Weight = 25,
							Text = Short(text3)
						};
					}
				}
			}
			return null;
		}

		private string Link(Person p)
		{
			if (Knows(p))
			{
				return p.Name;
			}
			foreach (KeyValuePair<Person, string> item in Hearers(p))
			{
				if (Knows(item.Key))
				{
					return item.Key.Name + "'s " + Gendered(Inverse(item.Value), (p.Woman == item.Key.Woman) ? p.Woman : p.Woman);
				}
			}
			return null;
		}

		private static string EffectText(Person p, List<string> effects)
		{
			List<string> list = new List<string>();
			string text = Grammar.UpperFirst(p.He);
			foreach (string item in effects ?? new List<string>())
			{
				Match match;
				if ((match = Regex.Match(item, "^laid up (\\d+)h$")).Success)
				{
					int num = int.Parse(match.Groups[1].Value);
					list.Add((num <= 24) ? (text + "'s laid up for a day.") : (text + " won't be riding anywhere for " + Voice.Number((num + 12) / 24) + " days."));
				}
				else if ((match = Regex.Match(item, "^scar: (.+)$")).Success)
				{
					list.Add(text + "'ll carry " + match.Groups[1].Value + ".");
				}
				else if (item == "friendship over")
				{
					list.Add("That friendship's over.");
				}
				else if ((match = Regex.Match(item, "^hate (\\d+) -> (\\d+)$")).Success)
				{
					int num2 = int.Parse(match.Groups[1].Value);
					int num3 = int.Parse(match.Groups[2].Value);
					if (num3 > num2 + 2)
					{
						list.Add(text + " hates you more than ever.");
					}
					else if (num3 < num2 - 2)
					{
						list.Add(text + "'s cooling off toward you.");
					}
				}
				else if ((match = Regex.Match(item, "^fear (\\d+) -> (\\d+)$")).Success)
				{
					int num4 = int.Parse(match.Groups[1].Value);
					int num5 = int.Parse(match.Groups[2].Value);
					if (num5 > num4 + 2)
					{
						list.Add(text + "'s warier of you now.");
					}
					else if (num5 < num4 - 2)
					{
						list.Add(text + "'s less afraid of you.");
					}
				}
			}
			return string.Join(" ", list.Take(2));
		}

		private void Push(Toast t)
		{
			Out.Add(t);
		}

		/// A notice about someone, only if the player knows them (Knowing.cs): what strangers write, hear and tell stays
		/// in their memories and the log, out of the corner.
		private void PushAbout(Person p, Toast t)
		{
			if (Knows(p))
			{
				Out.Add(t);
			}
		}

		public static string Short(string s, int max = 160)
		{
			if (s == null || s.Length <= max)
			{
				return s;
			}
			int num = s.LastIndexOfAny(new char[3] { '.', '!', '?' }, max - 1);
			if (num >= 40)
			{
				return s.Substring(0, num + 1);
			}
			num = s.LastIndexOf(' ', max - 2);
			return s.Substring(0, (num > 0) ? num : (max - 1)) + "...";
		}

		private void Effect(EnrageEffect e)
		{
			if (Cfg.RomanceAffectsEnrage && e.PersonId != null)
			{
				Effects.Add(e);
			}
		}

		private void Thought(string id, string mood, string text, int weight, int hours)
		{
			Effect(new EnrageEffect
			{
				PersonId = id,
				Kind = "thought",
				Mood = mood,
				Text = text,
				Amount = weight,
				Hours = hours
			});
		}

		private void Remember(string id, string emotion, int intensity, string text, string cause)
		{
			Effect(new EnrageEffect
			{
				PersonId = id,
				Kind = "memory",
				Emotion = emotion,
				Amount = intensity,
				Text = text,
				Cause = cause
			});
		}

		private void History(string id, string text)
		{
			Effect(new EnrageEffect
			{
				PersonId = id,
				Kind = "history",
				Text = text
			});
		}

		private void HateChange(string id, int delta)
		{
			if (delta != 0)
			{
				Effect(new EnrageEffect
				{
					PersonId = id,
					Kind = "hate",
					Amount = delta
				});
			}
		}

		private bool Literate(Person p)
		{
			if (p.Smarts == "Dim")
			{
				return Voice.Hash(p.Id + "letters") % 100 >= 70;
			}
			return true;
		}

		private string PlayerCalled(PersonState st)
		{
			if (st == null || !st.KnowsName)
			{
				return "a stranger";
			}
			return Player.FullName;
		}

		public int DrawnToYou(Person p)
		{
			if (!Cfg.Romance)
			{
				return 0;
			}
			return Romance.ToPlayer(p, Cfg.SameSexRomance, John);
		}

		public void Greeted(Person p, PersonState st, long now)
		{
			if (Cfg.Romance)
			{
				if (DrawnToYou(p) >= 30 && now - st.ContactAt > 180)
				{
					Feel(p, st, 3 + (p.Friendly ? 1 : 0), now, "greeted");
				}
				st.ContactAt = now;
			}
		}

		public FlirtResult FlirtWith(Person p, long now, IList<Person> watching)
		{
			FlirtResult flirtResult = new FlirtResult();
			if (!Cfg.Romance || !Romance.Adult(p))
			{
				return flirtResult;
			}
			PersonState personState = Store.Get(p);
			string text = p.Id + "|flirt|" + personState.Flirts + "|" + now / 30;
			PlayerRef you = Talk.RefFor(p, personState, Player);
			flirtResult.YouSay = Romance.PlayerLine(John, p.Woman, personState.Heart, text);
			int num = Romance.ToPlayer(p, Cfg.SameSexRomance, John);
			// what they think of him decides it (Regard, as their thoughts about him), with their day and their nature
			string regard = Regard.OfPerson(p, personState, true);
			string why;
			Interest.Ask ask = AskFor(p, regard, personState, p.Home ?? CurrentTown, now, Romance.Suits(p, Cfg.SameSexRomance, John), !Romance.Free(p) || SpokenFor(p.Id));
			string answer = PlayerAnswer(p, ask, flirtResult, out why);
			flirtResult.Answer = answer;
			flirtResult.Why = why;
			string text2 = answer == "no" ? why : null;
			personState.ContactAt = now;
			if (text2 != null && !Interest.Soft(why))
			{
				flirtResult.Reply = Romance.Rebuff(p, text2, text);
				if (text2 == "notdrawn")
				{
					Feel(p, personState, -5, now, "not interested");
					if (p.Personality == "Hothead" || p.Personality == "Proud")
					{
						HateChange(p.Id, 3);
					}
				}
				else if (text2 != "spam")
				{
					Feel(p, personState, -3, now, text2);
				}
			}
			else
			{
				personState.FlirtedAt = now;
				if (answer == "yes")
				{
					personState.Flirts++;
					flirtResult.Liked = true;
					flirtResult.Reply = Romance.Welcome(p, Math.Max(personState.Heart, 0), you, text, why);
					flirtResult.Heart = Math.Max(personState.Heart, 0);
					Feel(p, personState, 8 + num / 10, now, "flirted");
				}
				else if (answer == "maybe")
				{
					flirtResult.Reply = Romance.Unsure(p, text, why);
					Feel(p, personState, 3, now, "flirted");
				}
				else
				{
					flirtResult.Reply = Romance.Rebuff(p, text2, text);   // what put them off, weighing him up
					Feel(p, personState, -2, now, "flirted");
				}
			}
			foreach (Person item in watching ?? new List<Person>())
			{
				if (item != null && !(item.Id == p.Id))
				{
					PersonState personState2 = Store.Find(item.Id);
					if (personState2 != null && personState2.Heart >= 2)
					{
						personState2.JealousOf = p.Name;
						Feel(item, personState2, -20, now, "jealous of " + p.Name);
						Thought(item.Id, "Bitter", Voice.Pick(item.Id + "|" + p.Id, "watching him sweet-talk " + p.FirstName, "seeing him charm " + p.FirstName, "him and " + p.FirstName), -2, 48);
						HateChange(item.Id, 3);
						flirtResult.Jealous.Add(new KeyValuePair<Person, string>(item, Voice.Pick(item.Id + now, "I saw that.", "Really? Right in front of me?", "Don't you dare look at me.")));
					}
				}
			}
			Affair affair = Store.Affairs.FirstOrDefault((Affair x) => x.Active && x.Stage >= 2 && x.Involves(p.Id));
			if (affair != null)
			{
				string otherId = affair.OtherId(p.Id);
				Person person = (watching ?? new List<Person>()).FirstOrDefault((Person w) => w != null && w.Id == otherId);
				if (person != null)
				{
					HateChange(person.Id, 5);
					Thought(person.Id, "Bitter", Voice.Pick(person.Id + "|" + p.Id, "a stranger sweet-talking " + p.FirstName, "a stranger charming " + p.FirstName), -2, 48);
					flirtResult.Jealous.Add(new KeyValuePair<Person, string>(person, "That's my " + (p.Woman ? "girl" : "fella") + " you're sweet-talking, mister."));
				}
			}
			Log("FLIRT " + p.Name + " (" + (p.Personality ?? "no temper") + "; thinks of you: " + Interest.Explain(ask) + ChanceText(flirtResult) + " -> " + answer + " (" + why + "); affection " + personState.Affection + ", " +
			    Romance.HeartNames[personState.Heart + 1] + "): " + flirtResult.YouSay + " -> " + flirtResult.Reply);
			return flirtResult;
		}

		public string Insulted(Person p, long now)
		{
			if (!Cfg.Romance)
			{
				return null;
			}
			PersonState personState = Store.Find(p.Id);
			if (personState == null || (personState.Affection <= 0 && personState.Heart <= 0))
			{
				return null;
			}
			if (personState.Heart >= 2)
			{
				Scorn(p, personState, now, "what " + (personState.KnowsName ? Player.FirstName : "that stranger") + " said");
				return Voice.Pick(p.Id + now, "I thought... never mind what I thought.", "So that's how it is.", "I was a fool.");
			}
			Feel(p, personState, -15, now, "insulted");
			return null;
		}

		private void Feel(Person p, PersonState st, int delta, long now, string why)
		{
			int heart = st.Heart;
			st.Affection = Romance.Clamp(st.Affection + delta, -100, 100);
			if (heart == -1 && (now - st.HeartAt < 4320 || st.Affection < 40))
			{
				return;
			}
			int num = Romance.HeartFor(st.Affection, st.Flirts);
			if (num != heart)
			{
				st.Heart = num;
				st.HeartAt = now;
				Log("HEART " + p.Name + ": " + Romance.HeartNames[heart + 1] + " -> " + Romance.HeartNames[num + 1] + " (affection " + st.Affection + ", " + why + ")");
				if (num > heart)
				{
					Warmer(p, st, num, now);
				}
			}
		}

		private void Warmer(Person p, PersonState st, int heart, long now)
		{
			PlayerRef you = Talk.RefFor(p, st, Player);
			if (Cfg.Diaries && !st.Has("heartdiary:" + heart) && Literate(p))
			{
				string text = Romance.Diary(p, heart, you, DateText(now), p.Id + "|heart|" + heart);
				if (text != null)
				{
					st.Mark("heartdiary:" + heart);
					st.Diary.Add(new DiaryEntry
					{
						At = now,
						Date = DateText(now),
						Text = text
					});
					DiaryChanged.Add(p.Id);
					PushAbout(p, new Toast
					{
						Kind = "diary",
						Title = p.Name + "'s diary",
						PersonId = p.Id,
						Weight = 30 + heart * 5,
						Text = Short(text.Substring(text.IndexOf(" - ", StringComparison.Ordinal) + 3))
					});
				}
			}
			if (st.Has("warm:" + heart))
			{
				return;
			}
			st.Mark("warm:" + heart);
			switch (heart)
			{
			case 2:
				if (p.Hate > 0)
				{
					HateChange(p.Id, -Math.Min(p.Hate, 15));
				}
				History(p.Id, "SWEET ON " + PlayerCalled(st).ToUpperInvariant());
				break;
			case 3:
				Remember(p.Id, "Joy", 50, Voice.Pick(p.Id + "|fell", "fell for you", "lost my heart to you", "fell for you, hard"), "you");
				if (p.IsOutlaw && IntelFor != null)
				{
					st.Mark("intel:due");
				}
				break;
			case 4:
				Thought(p.Id, "Joyful", st.KnowsName ? Player.FirstName : "a certain stranger", 3, 96);
				History(p.Id, "SWEETHEARTS with " + PlayerCalled(st));
				break;
			}
		}

		private void Scorn(Person p, PersonState st, long now, string what)
		{
			int heart = st.Heart;
			st.Affection = Math.Min(st.Affection, 0) - 20;
			st.Heart = -1;
			st.HeartAt = now;
			st.Flirts = 0;
			Log("HEART " + p.Name + ": " + Romance.HeartNames[heart + 1] + " -> scorned (" + what + ")");
			PlayerRef you = Talk.RefFor(p, st, Player);
			if (Cfg.Diaries && Literate(p))
			{
				string text = Romance.Diary(p, -1, you, DateText(now), p.Id + "|scorn|" + now);
				st.Diary.Add(new DiaryEntry
				{
					At = now,
					Date = DateText(now),
					Text = text
				});
				DiaryChanged.Add(p.Id);
				PushAbout(p, new Toast
				{
					Kind = "diary",
					Title = p.Name + "'s diary",
					PersonId = p.Id,
					Weight = 45,
					Text = Short(text.Substring(text.IndexOf(" - ", StringComparison.Ordinal) + 3))
				});
			}
			Thought(p.Id, "Humiliated", what, -3, 72);
			Remember(p.Id, "Shame", 55, "you broke " + p.His + " heart", "you");
			HateChange(p.Id, 10);
		}

		private void MemoryMovesHeart(Person p, PersonState st, MemoryItem m, long now)
		{
			if (!Cfg.Romance || m.Text == "fell for you" || m.Text.StartsWith("you broke"))
			{
				return;
			}
			if (m.Emotion == "Anger" || m.Emotion == "Fear" || m.Emotion == "Grief" || (m.Emotion == "Shame" && m.Text.StartsWith("you ")))
			{
				if (st.Heart >= 2)
				{
					Scorn(p, st, now, "what " + (st.KnowsName ? Player.FirstName : "that stranger") + " did");
				}
				else if (st.Affection > 0)
				{
					Feel(p, st, -30, now, "what you did");
				}
			}
			else if (m.Emotion == "Joy" && DrawnToYou(p) > 0)
			{
				Feel(p, st, 10, now, "a kindness");
			}
		}

		public FlirtResult FlirtWithFolk(Folk f, bool grownUp, long now)
		{
			FlirtResult flirtResult = new FlirtResult();
			if (!Cfg.Romance || !grownUp)
			{
				return flirtResult;
			}
			Person p = PersonOfFolk(f.Key, f.Name, f.Woman, f.Town, now);   // the profile their thoughts come from
			PersonState personState = Store.Get(p);
			string text = f.Key + "|flirt|" + personState.Flirts + "|" + now / 30;
			PlayerRef you = Talk.RefFor(p, personState, Player);
			flirtResult.YouSay = Romance.PlayerLine(John, f.Woman, personState.Heart, text);
			personState.ContactAt = now;
			// what they think of him decides it (Regard: the same judgement as their thoughts about him on the ticker)
			if (p.Personality == null)
				p.Personality = f.Temper;
			string regard = Regard.OfFolk(f, personState, true);
			string why;
			Interest.Ask ask = AskFor(p, regard, personState, f.Town ?? CurrentTown, now, Romance.DrawnTo(f.Key, f.Woman, otherIsWoman: false, Cfg.SameSexRomance), SpokenFor(f.Key));
			string answer = PlayerAnswer(p, ask, flirtResult, out why);
			flirtResult.Answer = answer;
			flirtResult.Why = why;
			RememberFlirt(p, answer, why);   // it stays with them (Minds)
			string seen = (p.Personality ?? "no temper") + "; thinks of you: " + Interest.Explain(ask) + ChanceText(flirtResult) + " -> " + answer + " (" + why + ")";
			if (answer == "no" && !Interest.Soft(why))
			{
				flirtResult.Reply = Romance.Rebuff(p, why, text);
				Log("FLIRT " + f.Name + " (townsfolk; " + seen + "): " + flirtResult.YouSay + " -> " + flirtResult.Reply);
				return flirtResult;
			}
			personState.FlirtedAt = now;
			int num = 40 + Voice.Hash(f.Key + "|spark") % 30 + f.Like / 3 - f.Grudge;
			if (answer == "yes")
			{
				personState.Flirts++;
				flirtResult.Liked = true;
				flirtResult.Reply = Romance.Welcome(p, Math.Max(0, personState.Heart), you, text, why);
				flirtResult.Heart = Math.Max(0, personState.Heart);
				personState.Affection = Romance.Clamp(personState.Affection + 8 + num / 10, -100, 100);
				Effect(new EnrageEffect
				{
					PersonId = f.Key,
					Kind = "like",
					Amount = 5
				});
			}
			else if (answer == "maybe")
			{
				flirtResult.Reply = Romance.Unsure(p, text, why);
				personState.Affection = Romance.Clamp(personState.Affection + 3, -100, 100);
			}
			else
			{
				flirtResult.Reply = Romance.Rebuff(p, why, text);   // what put them off, weighing him up
				personState.Affection = Romance.Clamp(personState.Affection - 2, -100, 100);
			}
			personState.Heart = Math.Min(3, Romance.HeartFor(personState.Affection, personState.Flirts));
			Log("FLIRT " + f.Name + " (townsfolk; " + seen + "; affection " + personState.Affection + ", " + Romance.HeartNames[personState.Heart + 1] + "): " + flirtResult.YouSay + " -> " + flirtResult.Reply);
			return flirtResult;
		}

		/// One of the add-on's own people (a resident: a town regular, someone met before, or someone new now).
		public FlirtResult FlirtWithResident(Person p, long now)
		{
			FlirtResult flirtResult = new FlirtResult();
			if (!Cfg.Romance || !Romance.Adult(p))
				return flirtResult;
			PersonState personState = Store.Get(p);
			string text = p.Id + "|flirt|" + personState.Flirts + "|" + now / 30;
			PlayerRef you = Talk.RefFor(p, personState, Player);
			flirtResult.YouSay = Romance.PlayerLine(John, p.Woman, personState.Heart, text);
			personState.ContactAt = now;
			string town = p.Home ?? CurrentTown;
			InnerLife.Context day = ThoughtDay(town, p.Id);
			string regard = Regard.OfResident(personState, true, day.TownDeeds != null && day.TownDeeds.Count > 0, p.Personality);
			string why;
			Interest.Ask ask = AskFor(p, regard, personState, town, now, Romance.Suits(p, Cfg.SameSexRomance, John), SpokenFor(p.Id));
			string answer = PlayerAnswer(p, ask, flirtResult, out why);
			flirtResult.Answer = answer;
			flirtResult.Why = why;
			RememberFlirt(p, answer, why);   // it stays with them (Minds)
			if (why != "spam")
				personState.FlirtedAt = now;
			if (answer == "yes")
			{
				personState.Flirts++;
				flirtResult.Liked = true;
				flirtResult.Reply = Romance.Welcome(p, Math.Max(0, personState.Heart), you, text, why);
				flirtResult.Heart = Math.Max(0, personState.Heart);
				personState.Affection = Romance.Clamp(personState.Affection + 8, -100, 100);
			}
			else if (answer == "maybe")
			{
				flirtResult.Reply = Romance.Unsure(p, text, why);
				personState.Affection = Romance.Clamp(personState.Affection + 3, -100, 100);
			}
			else
			{
				flirtResult.Reply = Romance.Rebuff(p, why, text);
				if (Interest.Soft(why))
					personState.Affection = Romance.Clamp(personState.Affection - 2, -100, 100);
			}
			personState.Heart = Math.Min(3, Romance.HeartFor(personState.Affection, personState.Flirts));
			Log("FLIRT " + p.Name + " (resident, " + (p.Personality ?? "no temper") + "; thinks of you: " + Interest.Explain(ask) + ChanceText(flirtResult) + " -> " + answer + " (" + why + "); affection " + personState.Affection + ", " +
			    Romance.HeartNames[personState.Heart + 1] + "): " + flirtResult.YouSay + " -> " + flirtResult.Reply);
			return flirtResult;
		}

		/// The player's money in dollars, set by the game side before a flirt (-1 unknown).
		public int PlayerCash = -1;

		/// Whether the player greeted them first, set by the game side before a flirt: 1 yes, -1 no, 0 not known.
		public int GreetedFirst;

		private static readonly HashSet<string> GoodDeeds = new HashSet<string> { "mercy", "spared", "thiefcaught", "peace" };

		/// Everything that weighs when the player flirts with someone (Interest.Ask): what they think of him, who they are
		/// (the profile their thoughts come from), their day as their thoughts show it (a mood, the worry they're living
		/// through), and what he is in this town (his money, the bad and the good its people talk of).
		private Interest.Ask AskFor(Person p, string regard, PersonState st, string town, long now, bool drawn, bool taken)
		{
			InnerLife.Context day = ThoughtDay(town, p.Id);
			string kind = null;
			int stage = -1;
			bool good;
			bool worry = day.GameDay >= 0 && InnerLife.WorryNow(p, day.GameDay, out kind, out stage, out good);
			int goodHere = town == null ? 0 : Store.Deeds.Count(d => d.Town == town && GoodDeeds.Contains(d.Kind) && now - d.At >= 0 && now - d.At < 3 * 1440);
			return new Interest.Ask
			{
				Regard = regard, Drawn = drawn, Taken = taken, AtWorst = worry && stage == 2, Spam = st.FlirtedAt > 0 && now - st.FlirtedAt < 30, Temper = p.Personality,
				Mood = InnerLife.MoodOf(p, day, p.Id + "|flirtmood|" + day.GameDay), Who = p, Worry = worry ? kind : null, Cash = PlayerCash,
				BadHere = day.TownDeeds != null ? day.TownDeeds.Count : 0, GoodHere = goodHere, Saloon = NowPlace == "saloon",
				SaloonGirl = p.Woman && (NowPlace == "saloon" || InnerLife.TradeGroup(p.Trade) == "girl"), Id = p.Id, Day = day.GameDay, Greeted = GreetedFirst
			};
		}

		/// The answer to the player's flirting (Interest.ForPlayer: women by chance, men as before), and whether a night
		/// together can follow (Trysts.cs: a woman's yes, with Trysts on).
		private string PlayerAnswer(Person p, Interest.Ask ask, FlirtResult r, out string why)
		{
			int chance;
			string answer = Interest.ForPlayer(ask, Cfg.WillingSingle, Cfg.WillingTaken, out why, out chance);
			r.Chance = chance;
			r.Who = p;
			r.Tryst = Cfg.Trysts && answer == "yes" && p.Woman;
			r.TrystKind = Trysts.Kind(ask.SaloonGirl, Interest.Spoken(ask));
			r.Greeted = ask.Greeted;
			if (r.Tryst)
			{
				// her yes to the flirting isn't a yes to the night: that has its own roll, better if he greeted her first
				r.NightChance = Trysts.NightChance(r.TrystKind, ask.Greeted);
				r.Night = Trysts.NightYes(ask.Id ?? p.Id, ask.Day, r.NightChance);
			}
			return answer;
		}

		private static string ChanceText(FlirtResult r)
		{
			return (r.Chance >= 0 ? ", chance " + r.Chance + "% (" + r.TrystKind + ")" : "") + (r.Greeted != 0 ? (r.Greeted > 0 ? ", greeted first" : ", flirted cold") : "") +
			       (r.NightChance >= 0 ? ", the night " + r.NightChance + "% -> " + (r.Night ? "yes" : "no") : "");
		}

		/// After a night together (Trysts.cs): fonder of him (a little, for the saloon's women: it's their work), marked,
		/// and one of enRAGE's townsfolk likes him more there. Returns what she thinks after.
		public string Tryst(Person p, string kind, long now)
		{
			PersonState st = Store.Get(p);
			st.Affection = Romance.Clamp(st.Affection + (kind == "saloon" ? 4 : 15), -100, 100);
			st.Heart = Math.Min(3, Romance.HeartFor(st.Affection, st.Flirts));
			st.Mark("tryst:" + now / 1440);
			Store.Bump("trysts");
			if (p.Id != null && p.Id.StartsWith("folk:"))
				Effect(new EnrageEffect { PersonId = p.Id, Kind = "like", Amount = 5 });
			Log("TRYST " + p.Name + " (" + kind + "): an hour together; affection " + st.Affection + ", " + Romance.HeartNames[st.Heart + 1]);
			return Trysts.After(kind, p.Id + "|" + now / 60);
		}

		/// Spoken for: a sweetheart of their own (a warm romantic tie), someone they're courting, or one of enRAGE's people
		/// in an affair of the heart.
		public bool SpokenFor(string id)
		{
			return Store.Ties.Any(t => t.Has(id) && Ties.Sweethearts(t) && t.Warmth > 0) || Store.Links.Any(l => l.Kind == "courting" && (l.Id == id || l.OtherId == id)) ||
			       Store.Affairs.Any(x => x.Active && x.Involves(id));
		}

		/// Their mood today, as their thoughts show it (InnerLife.MoodOf).
		public string MoodToday(Person p, string town)
		{
			if (Cfg.MindLive && p != null && Shadow.Felt(p.Id) != null)
			{
				// the mind's mood (MindShadow.cs, Mood.cs) once it has felt something: sore or glad against their own baseline
				Traits t = Shadow.TraitsOf(TraitSource.From(p, p.Facets != null && p.Facets.Count > 0 ? "record" : p.Id != null && p.Id.StartsWith("folk:") ? "folk" : "resident"));
				string mind = Mood.SoreOrGlad(Shadow.MoodOf(p.Id, t, Store.LastGameMin), Mood.Baseline(t));
				if (mind != null)
					return mind;   // even by the mind: the day's ties and the weather still count, the old way
			}
			InnerLife.Context day = ThoughtDay(town, p.Id);
			return InnerLife.MoodOf(p, day, p.Id + "|mood|" + day.GameDay);
		}

		private void RomanceTick(IList<Person> roster, long now)
		{
			if (Store.RomanceAt == 0L || Store.RomanceAt > now)
			{
				Store.RomanceAt = now;
				return;
			}
			long num = now - Store.RomanceAt;
			if (num < 360)
			{
				return;
			}
			int num2 = (int)Math.Min(4L, num / 360);
			Store.RomanceAt = now;
			for (int i = 0; i < num2; i++)
			{
				if (Cfg.Romance)
				{
					PlayerHearts(now);
				}
				if (Cfg.NpcRomance)
				{
					MoveAffairs(now);
					NewCrushes(roster, now);
				}
			}
			Store.Affairs.RemoveAll((Affair a) => !a.Active && now - a.At > 43200);
		}

		private void PlayerHearts(long now)
		{
			foreach (KeyValuePair<string, PersonState> item in Store.People.ToList())
			{
				PersonState value = item.Value;
				if (value.Affection <= 0 && value.Heart == 0)
				{
					continue;
				}
				Person person = Find(item.Key);
				if (person == null)
				{
					continue;
				}
				if (!person.IsAlive)
				{
					value.Affection = 0;
					value.Heart = 0;
					continue;
				}
				if (value.Affection > 0 && now - value.ContactAt > 2880)
				{
					Feel(person, value, -1, now, "time apart");
				}
				if (John && value.Heart >= 2 && !value.KnowsWife && _rng.Next(100) < 4)
				{
					value.KnowsWife = true;
					Feel(person, value, -30, now, "heard he has a family");
					Thought(person.Id, "Bitter", "a man with a family back home", -2, 72);
				}
			}
		}

		private static int Need(int stage)
		{
			switch (stage)
			{
			case 1:
				return 10;
			case 2:
				return 30;
			case 3:
				return 70;
			case 4:
				return 150;
			case 5:
				return 240;
			default:
				return int.MaxValue;
			}
		}

		private static bool Pious(Person p)
		{
			if (p != null)
			{
				if (!(p.Devotion == "Devout"))
				{
					return p.Devotion == "Churchgoer";
				}
				return true;
			}
			return false;
		}

		private void MoveAffairs(long now)
		{
			foreach (Affair a in Store.Affairs.Where((Affair x) => x.Active).ToList())
			{
				Person person = Find(a.AId);
				Person person2 = ((a.BId != null) ? Find(a.BId) : null);
				if (person == null || (a.BId != null && person2 == null))
				{
					continue;
				}
				if (!person.IsAlive)
				{
					End(a, "died", now, person2, person);
				}
				else if (person2 != null && !person2.IsAlive)
				{
					End(a, "died", now, person, person2);
				}
				else
				{
					if (a.Stage >= 5)
					{
						continue;
					}
					if (person2 != null)
					{
						a.BLikes = Romance.Between(person2, person, Cfg.SameSexRomance);
					}
					int num = ((person2 != null) ? Romance.Between(person, person2, Cfg.SameSexRomance) : 60);
					a.Heat += 1 + (num + a.BLikes) / 80 + _rng.Next(2);
					if (a.Stage <= 1 && a.BLikes < 50 && _rng.Next(1000) < 25)
					{
						a.Ended = "faded";
						a.At = now;
						Log("ROMANCE " + a.AName + " & " + a.BName + ": faded");
						continue;
					}
					if (a.Stage == 2 && a.BLikes < 35 && _rng.Next(100) < 8)
					{
						End(a, "jilted", now, person, null);
						continue;
					}
					if (a.Stage >= 3)
					{
						int num2 = 1 + ((person.Personality == "Hothead" || person.Personality == "Proud") ? 1 : 0) + ((person2 != null && (person2.Personality == "Hothead" || person2.Personality == "Proud")) ? 1 : 0);
						if (_rng.Next(1000) < num2 * 4)
						{
							End(a, "quarrel", now, person, person2);
							continue;
						}
					}
					Affair affair = Store.Affairs.FirstOrDefault((Affair x) => x != a && x.Active && x.Stage >= 1 && x.Stage < 4 && SameHeart(x, a));
					if (affair != null && a.Stage >= 1 && a.Stage < 4)
					{
						RivalFight(a, affair, now);
						continue;
					}
					int num3 = a.Stage + 1;
					if (num3 == 4 && (a.BLikes < 55 || (!Pious(person) && !Pious(person2) && _rng.Next(100) >= 40)))
					{
						a.Heat = Math.Min(a.Heat, Need(4) - 1);
					}
					else if (num3 == 5 && WeddingPending(a))
					{
						a.Heat = Math.Min(a.Heat, Need(5) - 1);
					}
					else if (a.Heat >= Need(num3))
					{
						a.Stage = num3;
						a.At = now;
						Moved(a, person, person2, now);
					}
				}
			}
		}

		private static bool SameHeart(Affair x, Affair a)
		{
			if (x.AId == a.AId)
			{
				return false;
			}
			if (a.BId == null)
			{
				if (x.BId == null)
				{
					return x.BName == a.BName;
				}
				return false;
			}
			return x.BId == a.BId;
		}

		private void Moved(Affair a, Person pa, Person pb, long now)
		{
			string text = Person.FirstOf(a.BName);
			string text2 = Person.FirstOf(a.AName);
			Log("ROMANCE " + a.AName + " & " + a.BName + (a.Offstage ? (" (" + a.BLine + ")") : "") + ": " + Romance.StageNames[a.Stage]);
			switch (a.Stage)
			{
			case 2:
				Thought(a.AId, "Joyful", Voice.Pick(a.AId + "|court", "courting " + text, "walking out with " + text), 2, 48);
				if (pb != null)
				{
					Thought(pb.Id, "Joyful", text2 + "'s attentions", 1, 24);
				}
				History(a.AId, "COURTING " + a.BName);
				Announce(a, Romance.News(a, a.Stage, null), now, pa, pb);
				break;
			case 3:
				History(a.AId, "SWEETHEARTS with " + a.BName);
				if (pb != null)
				{
					History(pb.Id, "SWEETHEARTS with " + a.AName);
				}
				Announce(a, Romance.News(a, a.Stage, null), now, pa, pb);
				break;
			case 4:
				Thought(a.AId, "Joyful", Voice.Pick(a.AId + "|wedding", "the wedding", "the wedding day", "the wedding to come"), 3, 72);
				if (pb != null)
				{
					Thought(pb.Id, "Joyful", Voice.Pick(pb.Id + "|wedding", "the wedding", "the wedding day", "the wedding to come"), 3, 72);
				}
				History(a.AId, "ENGAGED to " + a.BName);
				if (pb != null)
				{
					History(pb.Id, "ENGAGED to " + a.AName);
				}
				Announce(a, Romance.News(a, a.Stage, null), now, pa, pb);
				ScheduleWedding(a, pa, pb, now);
				break;
			case 5:
				AddSpouse(a.AId, a.BName, a.BWoman, a.Town);
				if (pb != null)
				{
					AddSpouse(pb.Id, a.AName, a.AWoman, a.Town);
				}
				Effect(new EnrageEffect
				{
					PersonId = a.AId,
					Kind = "marry",
					OtherId = a.BId,
					OtherName = a.BName
				});
				Thought(a.AId, "Joyful", "married " + text, 4, 96);
				if (pb != null)
				{
					Effect(new EnrageEffect
					{
						PersonId = pb.Id,
						Kind = "marry",
						OtherId = a.AId,
						OtherName = a.AName
					});
					Thought(pb.Id, "Joyful", "married " + text2, 4, 96);
				}
				Announce(a, Romance.News(a, a.Stage, null), now, pa, pb);
				break;
			case 1:
				break;
			}
		}

		private void AddSpouse(string id, string name, bool woman, string town)
		{
			PersonState personState = Store.Find(id);
			Person person = Find(id);
			if (person != null)
			{
				if (personState == null)
				{
					personState = Store.Get(person);
				}
				if (personState.Family == null)
				{
					personState.Family = Family.For(person);
				}
				personState.Family.RemoveAll((Relative r) => r.Relation == "wife" || r.Relation == "husband");
				personState.Family.Insert(0, new Relative
				{
					Name = name,
					Relation = (woman ? "wife" : "husband"),
					Woman = woman,
					Town = (string.IsNullOrEmpty(town) ? "town" : town),
					Line = "keeps the house"
				});
			}
		}

		private void End(Affair a, string reason, long now, Person survivor, Person dead)
		{
			a.Ended = reason;
			a.At = now;
			Person person = Find(a.AId);
			Person person2 = ((a.BId != null) ? Find(a.BId) : null);
			Log("ROMANCE " + a.AName + " & " + a.BName + ": over (" + reason + ")");
			switch (reason)
			{
			case "jilted":
				Thought(a.AId, (person != null && person.Personality == "Proud") ? "Humiliated" : "Bitter", Person.FirstOf(a.BName) + " turning " + (a.AWoman ? "her" : "him") + " down", -2, 48);
				break;
			case "quarrel":
				Thought(a.AId, "Bitter", Voice.Pick(a.AId + "|fall", "the falling out with ", "the quarrel with ", "the break with ") + Person.FirstOf(a.BName), -2, 48);
				if (person2 != null)
				{
					Thought(person2.Id, "Bitter", "the falling out with " + Person.FirstOf(a.AName), -2, 48);
				}
				break;
			case "died":
				if (survivor != null && dead != null)
				{
					Thought(survivor.Id, "Grieving", dead.FirstName, -4, 120);
					History(survivor.Id, "LOST " + dead.Name);
					if (dead.KilledByPlayer)
					{
						string text = ((a.Stage < 5) ? ((a.Stage < 4) ? "sweetheart" : (dead.Woman ? "fiancee" : "fiance")) : (dead.Woman ? "wife" : "husband"));
						Remember(survivor.Id, "Grief", 80, "you killed " + survivor.His + " " + text + ", " + dead.Name, "you");
						HateChange(survivor.Id, 35);
						Log("ROMANCE " + survivor.Name + " blames you for " + dead.Name + ".");
					}
					a.AName = survivor.Name;
					a.BName = dead.Name;
				}
				break;
			}
			Announce(a, Romance.News(a, a.Stage, reason), now, person, person2);
		}

		private void RivalFight(Affair a, Affair rival, long now)
		{
			Person person = Find(a.AId);
			Person person2 = Find(rival.AId);
			if (person != null && person2 != null)
			{
				int num = person.Rank * 10 + Voice.Hash(person.Id + now) % 40 + ((person.Personality == "Hothead") ? 10 : 0);
				int num2 = person2.Rank * 10 + Voice.Hash(person2.Id + now) % 40 + ((person2.Personality == "Hothead") ? 10 : 0);
				Affair affair = ((num >= num2) ? rival : a);
				Affair obj = ((affair == a) ? rival : a);
				Person person3 = ((affair == a) ? person : person2);
				Person person4 = ((affair == a) ? person2 : person);
				affair.RivalId = person4.Id;
				affair.RivalName = person4.Name;
				obj.Heat += 6;
				Thought(person3.Id, "Humiliated", "losing " + Person.FirstOf(affair.BName) + " to " + person4.FirstName, -3, 72);
				Remember(person3.Id, "Anger", 50, "holds it against " + person4.Name + " (over " + Person.FirstOf(affair.BName) + ")", person4.Id);
				History(person3.Id, "LOST A FIGHT to " + person4.Name + " over " + affair.BName);
				History(person4.Id, "WON A FIGHT with " + person3.Name + " over " + affair.BName);
				End(affair, "rival", now, null, null);
			}
		}

		private void NewCrushes(IList<Person> roster, long now)
		{
			int num = Store.Affairs.Count((Affair affair2) => affair2.Active);
			if (num >= 40)
			{
				return;
			}
			HashSet<string> busy = new HashSet<string>(from id in Store.Affairs.Where((Affair affair2) => affair2.Active).SelectMany((Affair affair2) => new string[2] { affair2.AId, affair2.BId })
				where id != null
				select id);
			foreach (Person a in roster)
			{
				if (a == null || a.Id == null || !a.IsAlive || !Romance.Adult(a) || !Romance.Free(a) || busy.Contains(a.Id))
				{
					continue;
				}
				PersonState personState = Store.Find(a.Id);
				if (personState != null && personState.Heart >= 2)
				{
					continue;
				}
				int num2 = 3 + (a.Outgoing ? 2 : 0) + (a.Friendly ? 1 : 0) + Math.Max(0, (a.Facet("Humour") - 50) / 25) - (a.Surly ? 2 : 0) - ((a.Personality == "Cold") ? 2 : 0);
				if (num2 <= 0 || _rng.Next(2000) >= num2)
				{
					continue;
				}
				Affair affair = new Affair
				{
					AId = a.Id,
					AName = a.Name,
					AWoman = a.Woman,
					Stage = 0,
					Since = now,
					At = now,
					Town = a.Home
				};
				Person person = (from x in Acquainted(a)
					where !busy.Contains(x.Id) && !SweetOnPlayer(x.Id) && Romance.Between(a, x, Cfg.SameSexRomance) >= 45
					orderby Romance.Between(a, x, Cfg.SameSexRomance) + _rng.Next(20) descending
					select x).FirstOrDefault();
				if (person != null)
				{
					affair.BId = person.Id;
					affair.BName = person.Name;
					affair.BWoman = person.Woman;
					affair.BLikes = Romance.Between(person, a, Cfg.SameSexRomance);
					affair.Town = a.Home ?? person.Home;
				}
				else
				{
					if (_rng.Next(100) >= 60)
					{
						continue;
					}
					bool num3;
					switch (Romance.Orientation(a.Id, Cfg.SameSexRomance))
					{
					default:
						num3 = _rng.Next(2) == 0;
						break;
					case 1:
						num3 = a.Woman;
						break;
					case 0:
						num3 = !a.Woman;
						break;
					}
					bool woman = num3;
					Romance.Offstage(affair, a, woman, a.Id + "|" + now);
					if (string.IsNullOrEmpty(affair.Town))
					{
						affair.Town = Towns[_rng.Next(Towns.Length)];
					}
				}
				busy.Add(a.Id);
				if (affair.BId != null)
				{
					busy.Add(affair.BId);
				}
				Store.Affairs.Add(affair);
				Log("CRUSH " + a.Name + " on " + affair.BName + (affair.Offstage ? (" (" + affair.BLine + " in " + affair.Town + ")") : "") + ", likes back " + affair.BLikes);
				if (++num >= 40)
				{
					break;
				}
			}
		}

		private bool SweetOnPlayer(string id)
		{
			PersonState personState = Store.Find(id);
			if (personState != null)
			{
				return personState.Heart >= 2;
			}
			return false;
		}

		private IEnumerable<Person> Acquainted(Person a)
		{
			HashSet<string> ids = new HashSet<string>(a.FriendIds ?? new List<string>());
			Person person = Find(a.KinId);
			if (person != null)
			{
				foreach (string item in person.FriendIds ?? new List<string>())
				{
					ids.Add(item);
				}
			}
			foreach (Person value in _byId.Values)
			{
				if (!(value.Id == a.Id) && (ids.Contains(value.Id) || (value.FriendIds != null && value.FriendIds.Contains(a.Id)) || (!string.IsNullOrEmpty(a.GangId) && a.GangId == value.GangId) || (!string.IsNullOrEmpty(a.Home) && a.Home == value.Home)))
				{
					yield return value;
				}
			}
		}

		private void Announce(Affair a, string text, long now, Person pa, Person pb)
		{
			if (!string.IsNullOrEmpty(text) && !a.Said.Contains(a.Stage + "|" + a.Ended))
			{
				a.Said.Add(a.Stage + "|" + a.Ended);
				bool flag = (pa != null && Knows(pa)) || (pb != null && Knows(pb));
				bool flag2 = CurrentTown != null && (a.Town == CurrentTown || (pa != null && pa.Home == CurrentTown)) && _rng.Next(100) < 35;
				if (flag || flag2)
				{
					Push(new Toast
					{
						Kind = "romance",
						Title = "Word around town",
						PersonId = a.AId,
						Weight = (flag ? 28 : 18),
						Text = Short(text)
					});
				}
			}
		}

		public List<Gossip> RomanceGossip(Person speaker)
		{
			List<Gossip> list = new List<Gossip>();
			if (!Cfg.NpcRomance)
			{
				return list;
			}
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			foreach (KeyValuePair<Person, string> item in Hearers(speaker))
			{
				dictionary[item.Key.Id] = "my " + CallOf(speaker, item.Key) + " " + item.Key.FirstName;
			}
			foreach (Affair affair in Store.Affairs)
			{
				if (affair.Involves(speaker.Id))
				{
					continue;
				}
				string value;
				string text = (dictionary.TryGetValue(affair.AId, out value) ? affair.AName : ((affair.BId != null && dictionary.TryGetValue(affair.BId, out value)) ? affair.BName : null));
				if (text == null)
				{
					continue;
				}
				string text2 = (affair.Active ? Romance.Title(affair) : ((affair.Ended == "jilted") ? (affair.BName + " turned " + affair.AName + " down") : null));
				if (text2 != null)
				{
					string text3 = Voice.Gossip(text2, text, value, speaker.Id + affair.AId + affair.Stage);
					if (text3 != null)
					{
						list.Add(new Gossip
						{
							Key = "love:" + affair.AId + ":" + affair.BName + ":" + affair.Stage + ":" + affair.Ended,
							Line = text3
						});
					}
				}
			}
			return list;
		}

		private static string CallOf(Person speaker, Person other)
		{
			if (speaker.KinId == other.Id)
			{
				return Gendered(Inverse(speaker.KinRelation), other.Woman);
			}
			if (other.KinId == speaker.Id)
			{
				return Gendered(other.KinRelation ?? "kin", other.Woman);
			}
			return "friend";
		}

		public List<KeyValuePair<string, string>> CoupleLines(Person x, Person y, long now)
		{
			Affair affair = Store.Affairs.FirstOrDefault((Affair f) => f.Active && f.BId != null && ((f.AId == x.Id && f.BId == y.Id) || (f.AId == y.Id && f.BId == x.Id)));
			if (affair == null)
			{
				return null;
			}
			List<string> list = Romance.CoupleExchange(affair, affair.AId + now / 60);
			if (list == null)
			{
				return null;
			}
			Person person = ((x.Id == affair.AId) ? x : y);
			Person person2 = ((person == x) ? y : x);
			return new List<KeyValuePair<string, string>>
			{
				new KeyValuePair<string, string>(person.Name, list[0]),
				new KeyValuePair<string, string>(person2.Name, list[1])
			};
		}

		public Affair AffairOf(string id)
		{
			return Store.Affairs.FirstOrDefault((Affair a) => a.Active && a.Involves(id));
		}
	}
}
