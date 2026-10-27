// Pacing (overnight 2026-10-27, Tier 2.0): the one place that says whether something may happen now, so the world
// stays ordinary most of the time and an encounter still surprises after many hours of play. Each kind has a rarity
// (a chance per look, given the conditions are right), rests (since the last of its kind anywhere, at this place, for
// this person or pair), and is small (a nod, a look) or big (an encounter). Big moments have a budget per night (game
// noon to noon) and a quiet stretch after each, so two never come close together. A test fire skips all of it, and
// says so. Times are game minutes (one real minute is about 30 of them). Saved with the store, rolled back with it.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace EnrageWestworld.Core
{
    [DataContract]
    public sealed class PaceState
    {
        [DataMember(Name = "k", EmitDefaultValue = false)] public Dictionary<string, long> LastKind = new Dictionary<string, long>();
        [DataMember(Name = "p", EmitDefaultValue = false)] public Dictionary<string, long> LastPlace = new Dictionary<string, long>();     // "kind|place"
        [DataMember(Name = "w", EmitDefaultValue = false)] public Dictionary<string, long> LastWho = new Dictionary<string, long>();       // "kind|person or pair"
        [DataMember(Name = "b", EmitDefaultValue = false)] public long BigAt = -100000;
        [DataMember(Name = "n", EmitDefaultValue = false)] public long Night = -1;
        [DataMember(Name = "c", EmitDefaultValue = false)] public int BigTonight;

        internal void Fix()
        {
            if (LastKind == null) LastKind = new Dictionary<string, long>();
            if (LastPlace == null) LastPlace = new Dictionary<string, long>();
            if (LastWho == null) LastWho = new Dictionary<string, long>();
        }
    }

    public static class Pacing
    {
        public sealed class Rule
        {
            public string Kind;
            public bool Big;
            public int Chance;       // percent per look, given the conditions are right
            public long KindRest, PlaceRest, WhoRest;   // game minutes

            public Rule(string kind, bool big, int chance, long kindRest, long placeRest, long whoRest)
            {
                Kind = kind;
                Big = big;
                Chance = chance;
                KindRest = kindRest;
                PlaceRest = placeRest;
                WhoRest = whoRest;
            }
        }

        public const int BigPerNight = 2;          // big moments in one game night (noon to noon)
        public const long QuietAfterBig = 600;     // game minutes (about 20 real) after a big moment before another
        public const int MaxKept = 600;            // entries kept in each table (the oldest go)

        public static readonly Dictionary<string, Rule> Rules = new Dictionary<string, Rule>
        {
            // small: everyday moments; frequent, but never the same people again soon
            { "nod", new Rule("nod", false, 60, 0, 0, 45) },                       // per pair
            { "sight", new Rule("sight", false, 40, 30, 0, 90) },                  // "who" is the state: an hour and a half before the same again
            { "fall", new Rule("fall", false, 6, 150, 600, 1440) },
            { "standup", new Rule("standup", false, 45, 60, 0, 720) },
            { "round", new Rule("round", false, 25, 240, 1440, 1440) },            // per saloon a night
            { "drink", new Rule("drink", false, 55, 60, 300, 1440) },              // a saloon: one drinker in five hours or so
            { "grudge", new Rule("grudge", false, 30, 180, 0, 1440) },             // per pair a day
            { "callout", new Rule("callout", false, 40, 1440, 1440, 2880) },       // a noon callout: once a day at most
            { "preacher", new Rule("preacher", false, 35, 0, 1440, 2880) },       // a Sunday's street preacher: once a town a day, a person every two
            { "lostchild", new Rule("lostchild", false, 15, 720, 4320, 129600) },  // a mother asking after her child: three days a town, a season a woman
            { "sabbath", new Rule("sabbath", false, 30, 120, 120, 10080) },       // a Sunday morning's kneel by the church: two hours between, a week a person
            { "bell", new Rule("bell", false, 60, 1200, 1440, 0) },              // the bell at three: a night between, a town a night
            // big: encounters; rare, budgeted, with quiet between
            { "visitor", new Rule("visitor", true, 18, 2880, 2880, 0) },
            { "traveller", new Rule("traveller", false, 20, 900, 0, 0) },          // a man asking the way is no big moment
        };

        /// Why this kind can't happen here and now, or null when it may. `roll` is 0-99 (seeded by the caller). With
        /// `test`, nothing stands in the way, and `skipped` says what would have.
        /// ignoreBig: the quiet after a big moment and the night's budget don't apply, only the kind's own rests (the
        /// encounters, which may run several at once since 2026-10-27, Lex: up to [Encounters] EncountersAtOnce).
        public static string WhyNot(PaceState s, string kind, string place, string who, long now, int roll, bool test, out string skipped, bool ignoreBig = false)
        {
            skipped = null;
            Rule r;
            if (s == null || !Rules.TryGetValue(kind, out r))
                return null;
            string why = Rest(s, r, place, who, now, ignoreBig) ?? (roll >= r.Chance ? "the roll (" + roll + " of " + r.Chance + "%)" : null);
            if (test)
            {
                skipped = why;
                return null;
            }
            return why;
        }

        private static string Rest(PaceState s, Rule r, string place, string who, long now, bool ignoreBig = false)
        {
            long t;
            if (r.KindRest > 0 && s.LastKind.TryGetValue(r.Kind, out t) && now - t >= 0 && now - t < r.KindRest)
                return "a " + r.Kind + " " + (now - t) + " min ago (rests " + r.KindRest + ")";
            if (r.PlaceRest > 0 && place != null && s.LastPlace.TryGetValue(r.Kind + "|" + place, out t) && now - t >= 0 && now - t < r.PlaceRest)
                return "a " + r.Kind + " here " + (now - t) + " min ago";
            if (r.WhoRest > 0 && who != null && s.LastWho.TryGetValue(r.Kind + "|" + who, out t) && now - t >= 0 && now - t < r.WhoRest)
                return "them, " + (now - t) + " min ago";
            if (r.Big && !ignoreBig)
            {
                if (now - s.BigAt >= 0 && now - s.BigAt < QuietAfterBig)
                    return "quiet after the last big moment (" + (now - s.BigAt) + " of " + QuietAfterBig + " min)";
                if (NightOf(now) == s.Night && s.BigTonight >= BigPerNight)
                    return "tonight's " + BigPerNight + " big moments are spent";
            }
            return null;
        }

        /// It happened: its rests start now (a test too, so a test doesn't leave the world free for another at once).
        public static void Note(PaceState s, string kind, string place, string who, long now)
        {
            Rule r;
            if (s == null || !Rules.TryGetValue(kind, out r))
                return;
            s.LastKind[kind] = now;
            if (place != null)
                Put(s.LastPlace, kind + "|" + place, now);
            if (who != null)
                Put(s.LastWho, kind + "|" + who, now);
            if (r.Big)
            {
                long night = NightOf(now);
                if (night != s.Night)
                {
                    s.Night = night;
                    s.BigTonight = 0;
                }
                s.BigTonight++;
                s.BigAt = now;
            }
        }

        /// A big moment from elsewhere (the crying woman, an execution, a gathering): it starts the quiet and counts
        /// against the night, so tonight's encounters and the older ones share one budget.
        public static void NoteBig(PaceState s, long now)
        {
            if (s == null)
                return;
            long night = NightOf(now);
            if (night != s.Night)
            {
                s.Night = night;
                s.BigTonight = 0;
            }
            s.BigTonight++;
            s.BigAt = now;
        }

        /// The quiet and the night's budget alone, for big moments decided elsewhere; null when one may come.
        public static string BigWhyNot(PaceState s, long now)
        {
            if (s == null)
                return null;
            if (now - s.BigAt >= 0 && now - s.BigAt < QuietAfterBig)
                return "quiet after the last big moment (" + (now - s.BigAt) + " of " + QuietAfterBig + " min)";
            if (NightOf(now) == s.Night && s.BigTonight >= BigPerNight)
                return "tonight's " + BigPerNight + " big moments are spent";
            return null;
        }

        private static void Put(Dictionary<string, long> d, string key, long now)
        {
            d[key] = now;
            if (d.Count > MaxKept)
                foreach (string k in d.OrderBy(kv => kv.Value).Take(d.Count - MaxKept).Select(kv => kv.Key).ToList())
                    d.Remove(k);
        }

        /// Nights run noon to noon, so ten at night and two in the morning are the same one.
        public static long NightOf(long now)
        {
            return (now - 720) / 1440;
        }

        /// A reload to an earlier save: what came after the cut never happened.
        public static void Rewind(PaceState s, long cut)
        {
            if (s == null)
                return;
            foreach (Dictionary<string, long> d in new[] { s.LastKind, s.LastPlace, s.LastWho })
                foreach (string k in d.Where(kv => kv.Value > cut).Select(kv => kv.Key).ToList())
                    d.Remove(k);
            if (s.BigAt > cut)
            {
                s.BigAt = -100000;
                s.BigTonight = Math.Max(0, s.BigTonight - 1);
            }
        }

        /// For F7: what is resting now and for how long.
        public static string Status(PaceState s, long now)
        {
            if (s == null)
                return "none";
            string big = BigWhyNot(s, now);
            var resting = Rules.Values.Select(r => new { r, t = s.LastKind.TryGetValue(r.Kind, out long t) ? t : long.MinValue })
                .Where(x => x.t != long.MinValue && x.r.KindRest > 0 && now - x.t < x.r.KindRest).Select(x => x.r.Kind + " " + (x.r.KindRest - (now - x.t)) + " min").ToList();
            return "big moments " + (big ?? "may come") + "; " + s.BigTonight + " tonight; resting: " + (resting.Count == 0 ? "nothing" : string.Join(", ", resting));
        }
    }
}
