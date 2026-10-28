using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using EnrageWestworld.Core;
using NemesisSystem;

namespace EnrageWestworld.InGame
{
	internal static class Snapshot
	{
		private static readonly string[] Towns = new string[20]
		{
			"Saint Denis", "Valentine", "Rhodes", "Strawberry", "Annesburg", "Blackwater", "Armadillo", "Tumbleweed", "Van Horn", "Lagras",
			"Emerald Ranch", "Thieves' Landing", "Wapiti", "Colter", "Manzanita Post", "Riggs Station", "Butcher Creek", "MacFarlane's Ranch", "Benedict Point", "Flatneck Station"
		};

		[MethodImpl(MethodImplOptions.NoInlining)]
		public static Person Of(NemesisRecord r, Bridge bridge, Dictionary<string, NemesisRecord> byId, long now)
		{
			Person person = new Person
			{
				Id = r.Id,
				Name = r.Name,
				Title = r.Title,
				GangId = r.Gang,
				GangName = bridge.GangName(r.Gang),
				Trade = r.Trade,
				Birthplace = r.Birthplace,
				Family = r.Family,
				Children = r.Children,
				KinId = r.KinId,
				KinRelation = r.KinRelation,
				FriendIds = ((r.FriendIds != null) ? new List<string>(r.FriendIds) : new List<string>()),
				BornYear = r.BornYear,
				Woman = Models.Female(r.ModelHash),
				IsAlive = (r.IsAlive && !r.PresumedDead),
				Rank = r.Rank,
				Personality = (r.Personality.HasValue ? r.Personality.Value.ToString() : null),
				Disposition = r.Disposition.ToString(),
				Sociability = r.Sociability.ToString(),
				Devotion = r.Devotion.ToString(),
				Smarts = r.Smarts.ToString(),
				Pet = r.Pet,
				Quirks = ((r.Quirks != null) ? new List<string>(r.Quirks) : new List<string>()),
				Scars = ((r.Scars != null) ? (from s in r.Scars
					where s != null && s.Description != null
					select s.Description).ToList() : new List<string>()),
				Hate = r.FeltHate,
				Fear = r.Fear,
				TimesGreeted = r.TimesGreeted,
				TimesSpared = r.TimesSpared,
				TimesRobbed = r.TimesRobbed,
				YourNotoriety = r.YourNotoriety,
				OwesYou = r.OwesYou,
				KilledYou = (r.TimesKilledPlayer > 0),
				KilledByPlayer = r.KilledByPlayer,
				PresumedDead = r.PresumedDead,
				PresumedDeadAt = r.PresumedDeadAt,
				Buried = r.Buried,
				CheatedDeath = r.TimesCheatedDeath,
				KillLocation = r.KillLocation,
				ModelHash = r.ModelHash,
				Lore = (r.Id != null && r.Id.StartsWith("lore_")),
				Facets = ((r.Facets != null) ? new Dictionary<string, int>(r.Facets) : new Dictionary<string, int>()),
				Values = ((r.Values != null) ? new Dictionary<string, int>(r.Values) : new Dictionary<string, int>()),
				Needs = ((r.Needs != null) ? new Dictionary<string, int>(r.Needs) : new Dictionary<string, int>())
			};
			person.Murderous = r.FeltHate >= Hate.MurderousAt;
			person.Vengeful = !person.Murderous && r.FeltHate >= Hate.VengefulAt;
			if (r.KinId != null && byId.TryGetValue(r.KinId, out var value))
			{
				person.KinName = value.Name;
			}
			foreach (string friendId in person.FriendIds)
			{
				if (byId.TryGetValue(friendId, out var value2))
				{
					person.FriendNames.Add(value2.Name);
				}
			}
			if (r.Past != null)
			{
				for (int num = 0; num < r.Past.Count; num++)
				{
					PastEntry pastEntry = r.Past[num];
					if (pastEntry != null && !string.IsNullOrEmpty(pastEntry.Text))
					{
						person.Past.Add(new PastItem
						{
							Index = num,
							Year = pastEntry.Year,
							Month = pastEntry.Month,
							Text = pastEntry.Text,
							Kind = pastEntry.Kind,
							Public = pastEntry.Public,
							Known = pastEntry.Known
						});
						if (pastEntry.Text.StartsWith("Married ") && person.SpouseName == null)
						{
							person.SpouseName = pastEntry.Text.Substring(8).Trim();
						}
					}
				}
			}
			if (r.Memories != null)
			{
				foreach (Memory memory in r.Memories)
				{
					if (memory != null && !string.IsNullOrEmpty(memory.Text))
					{
						person.Memories.Add(new MemoryItem
						{
							Text = memory.Text,
							Emotion = memory.Emotion,
							Intensity = memory.Intensity,
							Cause = memory.Cause,
							At = memory.At,
							LongTerm = memory.LongTerm
						});
					}
				}
			}
			if (r.Thoughts != null)
			{
				foreach (Thought item in from x in r.Thoughts
					where x != null && x.Until > now
					orderby Math.Abs(x.Weight) descending, x.At descending
					select x)
				{
					person.Thoughts.Add(new ThoughtItem
					{
						Mood = item.Mood,
						Text = item.Text,
						Weight = item.Weight,
						At = item.At
					});
				}
			}
			if (r.Goals != null)
			{
				foreach (Goal goal in r.Goals)
				{
					if (goal != null)
					{
						person.Goals.Add(new GoalItem
						{
							Type = goal.Type,
							Target = goal.Target,
							Text = goal.Text
						});
					}
				}
			}
			if (r.History != null)
			{
				foreach (HistoryEntry item2 in r.History.Skip(Math.Max(0, r.History.Count - 6)))
				{
					if (item2 != null && !item2.Secret)
					{
						person.RecentHistory.Add(item2.Text);
					}
				}
			}
			person.Home = HomeOf(r);
			return person;
		}

		private static string HomeOf(NemesisRecord r)
		{
			string text = r.Birthplace ?? string.Empty;
			string[] towns = Towns;
			foreach (string text2 in towns)
			{
				if (text.StartsWith(text2, StringComparison.OrdinalIgnoreCase))
				{
					return text2;
				}
			}
			if (r.Past != null)
			{
				foreach (PastEntry item in from x in r.Past
					where x != null && x.Text != null
					orderby (!(x.Kind == "work")) ? 1 : 0
					select x)
				{
					towns = Towns;
					foreach (string text3 in towns)
					{
						if (item.Text.Contains(" in " + text3) || item.Text.Contains(" outside " + text3) || item.Text.Contains(" around " + text3) || item.Text.Contains(" near " + text3))
						{
							return text3;
						}
					}
				}
			}
			if (r.Habits != null)
			{
				string text4 = (from h in r.Habits
					where h != null && !string.IsNullOrEmpty(h.Town)
					group h by h.Town into g
					orderby g.Count() descending
					select g.Key).FirstOrDefault();
				if (text4 != null)
				{
					return text4;
				}
			}
			return null;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public static int MarkKnown(NemesisRecord r, Person p)
		{
			int num = 0;
			if (r.Past == null)
			{
				return 0;
			}
			foreach (PastItem item in p.Past)
			{
				if (item.Known && item.Index >= 0 && item.Index < r.Past.Count)
				{
					PastEntry pastEntry = r.Past[item.Index];
					if (pastEntry != null && !pastEntry.Known && pastEntry.Text == item.Text)
					{
						pastEntry.Known = true;
						num++;
					}
				}
			}
			return num;
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public static Folk Of(Acquaintance a)
		{
			return new Folk
			{
				Id = a.Id,
				Name = a.FullName,
				Town = a.Town,
				Temper = a.Temper,
				Woman = Models.Female(a.Model),
				Met = a.Met,
				Reported = a.Reported,
				KeptQuiet = a.KeptQuiet,
				Grudge = a.Grudge,
				Fear = a.Fear,
				Like = a.Like,
				Favours = a.Favours,
				Witnessed = ((a.Witnessed != null) ? new List<string>(a.Witnessed) : new List<string>()),
				FavourNotes = ((a.FavourNotes != null) ? new List<string>(a.FavourNotes) : new List<string>())
			};
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		public static LifeEvent Of(WorldEvent e)
		{
			LifeEvent lifeEvent = new LifeEvent
			{
				Id = e.Id,
				Kind = e.Kind,
				Title = e.Title,
				Heard = e.Heard,
				GameMin = e.GameMin
			};
			if (e.People != null)
			{
				foreach (WorldPart person in e.People)
				{
					if (person != null)
					{
						lifeEvent.People.Add(new LifePart
						{
							Id = person.Id,
							Name = person.Name,
							Outcome = person.Outcome,
							Effects = ((person.Effects != null) ? new List<string>(person.Effects) : new List<string>())
						});
					}
				}
			}
			return lifeEvent;
		}
	}
}
