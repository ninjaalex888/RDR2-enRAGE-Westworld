// What a street scene leaves between two people: a tie, kept once per pair, with the latest kind of scene, how warm
// or hostile it has been, how many times, and when. Kept only between people who can be met again (town regulars,
// residents already saved for something else, enRAGE's townsfolk), so a tie never saves anyone by itself. The two
// think about each other by name for a few days (InnerLife), and the rest of their town talks about it.
// All lines are original to this add-on.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace EnrageWestworld.Core
{
    [DataContract]
    public sealed class SceneTie
    {
        [DataMember] public string A, B;           // ids: res:N for a resident, folk:... for one of enRAGE's townsfolk
        [DataMember] public string AName, BName;   // their full names, so a thought never has to look them up
        [DataMember] public string Town;
        [DataMember] public string Kind;           // the latest scene between them: argue, brawl, flirt, smalltalk...
        [DataMember] public int Warmth;            // the scenes added up: warm above 0, hostile below (-3..3)
        [DataMember] public int Count;             // scenes between them
        [DataMember] public long At;               // game minute of the latest
        [DataMember] public int Stage;             // how far a story between them has gone (follow-up scenes)

        public bool Has(string id)
        {
            return A == id || B == id;
        }

        public string Other(string id)
        {
            return A == id ? BName : AName;
        }
    }

    public static class Ties
    {
        public const int Max = 300;

        /// What a scene does to the two in it: +1 warm, -1 hostile, 0 they know each other a little better; null
        /// for a scene that leaves nothing between them (a song, a tale, a pitch to a crowd, the player's business).
        public static int? Effect(string kind)
        {
            switch (kind)
            {
                case "argue": case "brawl": case "cheat": case "debt": case "standoff":
                    return -1;
                case "flirt": case "propose":
                    return 1;
                case "smalltalk": case "gossip": case "haggle": case "hosts":
                    return 0;
                default:
                    return null;
            }
        }

        /// Notes a scene between two people: the tie between them, made or carried on. Null if the scene leaves none.
        public static SceneTie Note(List<SceneTie> all, string aId, string aName, string bId, string bName, string town, string kind, long now)
        {
            int? effect = Effect(kind);
            if (effect == null || aId == null || bId == null || aId == bId)
                return null;
            SceneTie t = all.FirstOrDefault(x => x.Has(aId) && x.Has(bId));
            if (t == null)
            {
                t = new SceneTie { A = aId, B = bId };
                all.Add(t);
            }
            t.AName = t.A == aId ? aName : bName;
            t.BName = t.A == aId ? bName : aName;
            t.Town = town ?? t.Town;
            t.Kind = kind;
            t.Warmth = Math.Max(-3, Math.Min(3, t.Warmth + effect.Value));
            t.Count++;
            t.At = now;
            while (all.Count > Max)
                all.Remove(all.OrderBy(x => x.At).First());
            return t;
        }

        /// This person's ties from the last few days, newest first.
        public static List<SceneTie> Of(IEnumerable<SceneTie> all, string id, long now, int days)
        {
            return all.Where(t => t.Has(id) && now - t.At >= 0 && now - t.At < days * 1440L).OrderByDescending(t => t.At).ToList();
        }

        /// The town's talk: ties in this town from the last two days worth talking about (warm or hostile), not
        /// the thinker's own, newest first.
        public static List<SceneTie> TalkOf(IEnumerable<SceneTie> all, string town, string notId, long now)
        {
            return all.Where(t => t.Town == town && t.Warmth != 0 && !t.Has(notId) && now - t.At >= 0 && now - t.At < 2 * 1440L)
                      .OrderByDescending(t => t.At).ToList();
        }

        // ---- what they think of each other --------------------------------------------------------------

        private static readonly string[] Quarrel =
        {
            "Shouldn't have said that to {other}. Or should have said worse. Can't decide which.",
            "Words with {other}. Keep having the argument again in my head, and winning it this time.",
            "{other} looked at me like I was dirt on a boot. I'll remember that face."
        };

        private static readonly string[] Blows =
        {
            "Jaw still aches. {other} hits like a mule kicks.",
            "Knuckles split from {other}'s teeth. Worth it. Mostly.",
            "Came to blows with {other}. In the street, like boys. Can't look anybody in the eye."
        };

        private static readonly string[] Money =
        {
            "{other} and the money. Always the money between us now.",
            "Saw {other} buy a round like a rich man. With whose money, I wonder."
        };

        private static readonly string[] Sweet =
        {
            "{other} laughed at what I said. Twice. I've been counting.",
            "Thinking about {other} again. Burned the coffee doing it.",
            "Wonder if {other} thinks of me at all. Probably not. Maybe."
        };

        private static readonly string[] Known =
        {
            "Talked with {other} a good while. Easy company. Rare, that.",
            "{other} said a thing I keep turning over. Clever, for a talker."
        };

        /// A thought about someone they had a scene with lately, by first name.
        public static string Thought(SceneTie t, string id, string seed)
        {
            string[] pool = t.Kind == "brawl" ? Blows : t.Kind == "debt" ? Money : t.Warmth < 0 ? Quarrel : t.Warmth > 0 ? Sweet : Known;
            return Voice.Pick(seed + "tie", pool).Replace("{other}", Person.FirstOf(t.Other(id)));
        }

        // ---- what the town says ---------------------------------------------------------------------------

        private static readonly string[] TownHostile =
        {
            "{a} and {b} were shouting at each other in the street. Half of {town} heard it.",
            "{a} won't stand in the same room as {b} now. Ask either one why, you get a different story."
        };

        private static readonly string[] TownBlows =
        {
            "{a} and {b} came to blows. Over nothing, they say. It's never nothing.",
            "Somebody pulled {a} off {b} before it got worse. Blood on the boardwalk."
        };

        private static readonly string[] TownWarm =
        {
            "{a} has been seen walking with {b}. Folks talk. Folks always talk.",
            "{a} and {b}, stood close as two spoons. Give it a month."
        };

        /// The town's talk about a tie between two others, with both their full names.
        public static string TownThought(SceneTie t, string town, string seed)
        {
            string[] pool = t.Kind == "brawl" ? TownBlows : t.Warmth > 0 ? TownWarm : TownHostile;
            return Voice.Pick(seed + "tt", pool).Replace("{a}", t.AName).Replace("{b}", t.BName).Replace("{town}", town ?? "town");
        }

        /// Every line, for the tests.
        public static IEnumerable<string> AllLines()
        {
            return Quarrel.Concat(Blows).Concat(Money).Concat(Sweet).Concat(Known).Concat(TownHostile).Concat(TownBlows).Concat(TownWarm);
        }
    }
}
