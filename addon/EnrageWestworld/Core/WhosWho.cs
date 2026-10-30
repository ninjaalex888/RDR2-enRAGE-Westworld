// Who's who (night 2026-10-29, the backlog's 6b; roadmap 24): a page in the reader of the people you've met, read from
// what is already kept (who they are, how they take you, who they are tied to), the most recent first. Read only: it
// saves nothing and makes nobody (a person is listed only if the store already holds them); built anew each time the
// reader opens, never kept among the papers. With [Mind] MindLive and [Display] WhosWho. And what the mind feels about you
// lately, as the greetings to you read it (Passing.ToYou), for those the shadow holds this session. All lines are original
// to this add-on.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EnrageWestworld.Core
{
    public static class WhosWho
    {
        public const int Most = 40;
        public const int Fond = 50, Warm = 20, Cool = -15, Bitter = -50;   // Warm as Regard's "likes him" (20)

        /// Met: spoken with, greeted, flirted with, touched by something you did, or saw you do something.
        public static bool Met(PersonState st)
        {
            return st != null && (st.Talks > 0 || st.KnowsName || st.Flirts > 0 || st.ContactAt > 0 || (st.SawDeeds != null && st.SawDeeds.Count > 0));
        }

        /// The page's text, or null when you've met nobody yet.
        public static string Body(Store store, long now, Func<string, List<FeltAt>> felt = null)
        {
            if (store == null || store.People == null)
                return null;
            var all = store.People.Values.Where(Met).ToList();
            // someone with neither a name you know nor anything kept of them would only be "someone": counted, not listed
            var listed = all.Where(st => NameOf(store, st) != null || ResidentOf(store, st) != null)
                            .OrderByDescending(Last).ThenBy(st => st.Id, StringComparer.Ordinal).ToList();
            if (all.Count == 0)
                return null;
            var b = new StringBuilder();
            b.Append(listed.Count > 0 ? "The people you have met, the most recent first." : "The people you have met.");
            foreach (PersonState st in listed.Take(Most))
            {
                b.Append("\n\n");
                b.Append(Entry(store, st, now, felt));
            }
            // counted: only those who never told a name (one who told it, with nothing of them kept, is left out unsaid)
            int more = listed.Count - Math.Min(Most, listed.Count), faceless = all.Count(st => !st.KnowsName && ResidentOf(store, st) == null);
            if (more > 0)
                b.Append("\n\n" + more + " more, longer ago.");
            if (faceless > 0)
                b.Append("\n\n" + Grammar.UpperFirst((more > 0 ? "and " : "") +
                         (listed.Count == 0 ? (faceless == 1 ? "one person whose name you never learned." : faceless + " people whose names you never learned.")
                                            : (faceless == 1 ? "one other whose name you never learned." : faceless + " others whose names you never learned."))));
            return b.ToString();
        }

        private static long Last(PersonState st)
        {
            return Math.Max(st.LastSeenAt, Math.Max(st.LastTalkAt, Math.Max(st.ContactAt, st.FlirtedAt)));
        }

        private static Resident ResidentOf(Store store, PersonState st)
        {
            return Resident.IsResident(st.Id) ? store.FindResident(st.Id) : null;
        }

        /// The name, only when they told it.
        private static string NameOf(Store store, PersonState st)
        {
            if (!st.KnowsName)
                return null;
            if (!string.IsNullOrEmpty(st.Name))
                return st.Name;
            Resident r = ResidentOf(store, st);
            return r != null && !string.IsNullOrEmpty(r.Name) ? r.Name : null;
        }

        /// One person: who they are, when you first and last saw them, how they take you, and who they are tied to.
        public static string Entry(Store store, PersonState st, long now, Func<string, List<FeltAt>> felt = null)
        {
            Resident r = ResidentOf(store, st);
            bool dead = (r != null && r.Dead) || store.DeathOf(st.Id) != null;
            string what = r != null ? Describe(r) : null;
            string name = NameOf(store, st);
            var b = new StringBuilder();
            if (name != null)
                b.Append(name + (what != null ? ", " + what : ""));
            else if (st.KnowsName)
                b.Append(Grammar.UpperFirst(what ?? "someone"));   // told it once, and it isn't kept
            else
                b.Append(Grammar.UpperFirst(what ?? "someone") + ", who never gave you a name");
            b.Append(dead ? " (dead)." : ".");
            // when first seen: in a town by our own count (FirstSeenAt), else when something first asked who they were
            long first = st.FirstSeenAt > 0 ? st.FirstSeenAt : r != null ? r.MetAt : 0;
            long last = Last(st);
            if (first > 0)
                b.Append(" First seen " + Ago(now, first) + (last > 0 && Day(last) > Day(first) ? ", last " + Ago(now, last) : "") + ".");
            string you = Towards(st, dead);
            if (you != null)
                b.Append(" " + you);
            string lately = dead || felt == null ? null : Lately(felt(st.Id), now);
            if (lately != null)
                b.Append(" " + lately);
            string ties = TiesOf(store, st.Id, dead);
            if (ties != null)
                b.Append(" " + ties);
            return b.ToString();
        }

        /// "a hot-tempered farmhand of Valentine": the temper, the trade and the town, as much as is known.
        private static string Describe(Resident r)
        {
            string trade = !string.IsNullOrEmpty(r.Trade) ? r.Trade : r.Woman ? "a woman" : "a man";
            string temper = Temper(r.Personality);
            if (temper != null)
            {
                string bare = trade.StartsWith("a ") ? trade.Substring(2) : trade.StartsWith("an ") ? trade.Substring(3) : trade;
                trade = (StartsVowel(temper) ? "an " : "a ") + temper + " " + bare;
            }
            return trade + (!string.IsNullOrEmpty(r.Town) ? " of " + r.Town : "");
        }

        private static bool StartsVowel(string s)
        {
            return s.Length > 0 && "aeiou".IndexOf(char.ToLowerInvariant(s[0])) >= 0;
        }

        private static string Temper(string personality)
        {
            switch (personality)
            {
                case "Hothead": return "hot-tempered";
                case "Coward": return "timid";
                case "Proud": return "proud";
                case "Cold": return "cold";
                case "Greedy": return "grasping";
                case "Loyal": return "steadfast";
                case "Drifter": return "restless";
                default: return null;
            }
        }

        /// How they take you, from their own standing towards you, and what has passed between you (the past for the dead).
        private static string Towards(PersonState st, bool dead)
        {
            var parts = new List<string>();
            string was = dead ? "was " : "";
            if (st.Affection >= Fond) parts.Add(was + "fond of you");
            else if (st.Affection >= Warm) parts.Add(was + "well disposed towards you");
            else if (st.Affection <= Bitter) parts.Add(was + "bitter against you");
            else if (st.Affection <= Cool) parts.Add(was + "cool towards you");
            if (st.Talks > 0) parts.Add(st.Talks == 1 ? "you spoke once" : dead ? "you spoke " + Times(st.Talks) : "you have spoken " + Times(st.Talks));
            if (st.Flirts > 0) parts.Add(st.Flirts == 1 ? "you flirted once" : dead ? "you flirted " + Times(st.Flirts) : "you have flirted " + Times(st.Flirts));
            if (st.SawDeeds != null && st.SawDeeds.Count > 0) parts.Add(st.SawDeeds.Count == 1 ? "saw something you did" : (dead ? "saw your doings " : "has seen your doings ") + Times(st.SawDeeds.Count));
            if (parts.Count == 0)
                return null;
            return Grammar.UpperFirst(string.Join("; ", parts)) + ".";
        }

        /// The mind's strongest feeling about you still lasting, by the greetings' own rule (Passing.AboutB for "player",
        /// Mood.Lasting at least Passing.MindLeft): "Lately sore at you.", "Lately very grateful to you."
        public static string Lately(List<FeltAt> felt, long now)
        {
            if (felt == null)
                return null;
            float left = 0f;
            string kind = null;
            foreach (FeltAt f in felt)
            {
                if (!Passing.AboutB(f, "player"))
                    continue;
                float l = Mood.Lasting(f, now);
                if (l > left)
                {
                    left = l;
                    kind = Feeling.Kinds[f.Kind];
                }
            }
            if (kind == null || left < Passing.MindLeft)
                return null;
            string what = kind == "anger" ? "sore at you" : kind == "contempt" ? "scornful of you" : kind == "envy" ? "envious of you"
                        : kind == "fear" ? "afraid of you" : kind == "gratitude" ? "grateful to you" : kind == "shame" ? "ashamed before you" : null;
            if (what == null)
                return null;
            return "Lately " + (left >= Appraisal.Strong ? "very " : "") + what + ".";
        }

        private static string Times(int n)
        {
            return n == 2 ? "twice" : n == 3 ? "three times" : n < 10 ? n + " times" : "many times";
        }

        /// Up to three ties to others, the strongest first: "A friend of Ben Cole; bad blood with a man in Valentine." The
        /// other is named only if you know their name yourself.
        public static string TiesOf(Store store, string id, bool dead = false)
        {
            if (store.Ties == null)
                return null;
            var mine = store.Ties.Where(t => t.Has(id) && t.Warmth != 0)
                                 .OrderByDescending(t => Math.Abs(t.Warmth)).ThenByDescending(t => t.At).Take(3).ToList();
            if (mine.Count == 0)
                return null;
            var parts = new List<string>();
            foreach (SceneTie t in mine)
            {
                string otherId = t.A == id ? t.B : t.A;
                string other = OtherName(store, otherId, t.A == id ? t.BName : t.AName, t.Town);
                string was = dead ? "was " : "";
                parts.Add(t.Warmth >= 2 ? was + "a friend of " + other : t.Warmth == 1 ? was + "on good terms with " + other
                        : t.Warmth == -1 ? was + "at odds with " + other : (dead ? "there was bad blood with " : "bad blood with ") + other);
            }
            return Grammar.UpperFirst(string.Join("; ", parts)) + ".";
        }

        private static string OtherName(Store store, string otherId, string tieName, string town)
        {
            PersonState o;
            if (otherId != null && store.People != null && store.People.TryGetValue(otherId, out o) && o.KnowsName)
            {
                if (!string.IsNullOrEmpty(tieName))
                    return tieName;
                if (!string.IsNullOrEmpty(o.Name))
                    return o.Name;   // the tie kept no name, but you know it
            }
            Resident r = Resident.IsResident(otherId) ? store.FindResident(otherId) : null;
            string who = r != null && r.Woman ? "a woman" : r != null ? "a man" : "someone";
            return who + (!string.IsNullOrEmpty(town) ? " in " + town : "");
        }

        private static long Day(long minute)
        {
            return minute / 1440;
        }

        /// "today", "yesterday", "4 days ago", "3 weeks ago", by the game's calendar days.
        public static string Ago(long now, long then)
        {
            long days = Math.Max(0, Day(now) - Day(then));
            if (days == 0) return "today";
            if (days == 1) return "yesterday";
            if (days < 14) return days + " days ago";
            if (days < 60) return days / 7 + " weeks ago";
            return days / 30 + " months ago";
        }
    }
}
