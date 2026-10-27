// Minds (0.6.3, M1; Lex's brief 2026-10-26, roadmap 4 and 5): people think about whatever is most on their mind.
// Each lasting person keeps a few memories of what they lived through (a scene they were in, one they saw), written
// when it happens. A thought is chosen by attention: every memory, the worry they're living through, what is around
// them now and the old timeless pools each get a pull, the strongest wins, and the thought carries its cause. The
// engine side (Engine.Minds.cs) gathers the candidates; this file holds the memory, the pull and the choice. All lines
// (Minds.Lines.cs) are original to this add-on.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace EnrageWestworld.Core
{
    /// One thing a person lived through and may think about again. Short names keep the save small.
    [DataContract]
    public sealed class MindMemory
    {
        [DataMember(Name = "k")] public string Kind;          // the group: blows, quarrel, slight, theft, drunk, warmth, talk
        [DataMember(Name = "s")] public string Sub;           // the exact scene kind: brawl, rough, argue, flirt...
        [DataMember(Name = "w")] public List<string> Who;     // the others in it, by id (null for someone unknown)
        [DataMember(Name = "n")] public List<string> Names;   // ...as this person knows them: a first name, or how they looked
        [DataMember(Name = "t")] public string Town;
        [DataMember(Name = "p")] public string Place;         // saloon, church, store... (null: the street)
        [DataMember(Name = "a")] public long At;              // game minute (Store.LastGameMin)
        [DataMember(Name = "h")] public string How;           // did, saw, heard
        [DataMember(Name = "r")] public string Role;          // when one of the two did it to the other: doer, done; else null
        [DataMember(Name = "f")] public int Feel;             // -3 .. 3
        [DataMember(Name = "g")] public int Strength;         // 0 .. 100
        [DataMember(Name = "x")] public int Times;            // times thought about since
        [DataMember(Name = "l")] public long LastAt;          // when last thought about (game minute; 0 never)
        [DataMember(Name = "y")] public string Key;           // the same thing noted twice is one memory
        [DataMember(Name = "u")] public long DueAt;           // heard of: not on their mind until word reaches them (0: at once)

        public string WhoAt(int i)
        {
            return Who != null && i < Who.Count ? Who[i] : null;
        }

        public string NameAt(int i)
        {
            return Names != null && i < Names.Count ? Names[i] : null;
        }
    }

    public static partial class Minds
    {
        public const int Max = 12;   // memories a person keeps; the weakest goes

        /// What a thought came from and how strongly it pulled, for the log and the tests.
        public sealed class Choice
        {
            public string Text, Source;
            public int Pull;
        }

        /// Something that might be thought about now: where it comes from, how hard it pulls, and the line, made only
        /// if it's chosen.
        public sealed class Candidate
        {
            public string Source;
            public double Pull;
            public Func<string> Make;
            public MindMemory M;             // a memory, worn when it's thought
        }

        /// Someone in or around something that happened, as the game side knows them.
        public sealed class Party
        {
            public string Id;            // a lasting id (folk:, res:), or null for someone who can't be met again
            public string Name;          // their full name, or null
            public string Home;          // their town
            public string Label;         // how they look, for someone who doesn't know their name ("a farmhand")
        }

        // ---- what a scene is to a memory ----------------------------------------------------------------

        /// A scene kind's group, or null for one nobody remembers (songs, tales, posses...).
        public static string GroupOf(string kind)
        {
            switch (kind)
            {
                case "brawl": case "rough": case "fight":
                    return "blows";
                case "argue": case "cheat": case "debt": case "standoff": case "snap": case "envy": case "faced":
                    return "quarrel";
                case "brushoff": case "scorn": case "avoid": case "passby":
                    return "slight";
                case "pickpocket": case "lift":
                    return "theft";
                case "drunk":
                    return "drunk";
                case "flirt": case "propose": case "sweet": case "comfort": case "cheer": case "charity": case "makeup": case "help": case "defend": case "round": case "apology":
                    return "warmth";
                case "smalltalk": case "gossip": case "haggle": case "hosts":
                    return "talk";
                default:
                    return null;
            }
        }

        /// One of the player's deeds, as a memory's group: deed:violence, deed:theft, deed:kindness; null for the rest
        /// (visits, hunting, a wedding...).
        public static string DeedGroup(string kind)
        {
            switch (kind)
            {
                case "murder": case "kill": case "lawkill": case "namedkill": case "assault": case "bankrobbery": case "stagecoach": case "trainrobbery":
                    return "deed:violence";
                case "robbery": case "theft": case "horsetheft": case "looting": case "graverobbing":
                    return "deed:theft";
                case "mercy": case "peace": case "thiefcaught": case "spared":
                    return "deed:kindness";
                default:
                    return null;
            }
        }

        /// How a deed of the player's seen with their own eyes stays with someone, and how it felt.
        public static int DeedStrength(string kind)
        {
            switch (kind)
            {
                case "lawkill": case "bankrobbery": case "trainrobbery": return 88;
                case "murder": case "namedkill": case "stagecoach": return 82;
                case "kill": return 66;
                case "assault": return 55;
                case "robbery": case "graverobbing": return 60;
                case "horsetheft": return 50;
                case "theft": case "looting": return 44;
                default: return 48;   // a kindness
            }
        }

        public static int DeedFeel(string kind)
        {
            string g = DeedGroup(kind);
            return g == "deed:violence" ? (kind == "kill" || kind == "assault" ? -2 : -3) : g == "deed:theft" ? -2 : 2;
        }

        /// Whether one of the two did it to the other (the first in the scene did it), rather than both together.
        public static bool OneSided(string kind)
        {
            return kind == "rough" || kind == "pickpocket" || kind == "lift" || kind == "drunk" || kind == "charity" || kind == "scorn" || kind == "avoid" ||
                   kind == "help" || kind == "passby" || kind == "defend" || kind == "faced" || kind == "round";
        }

        /// How much it stays with someone, by group and by how they knew it.
        public static int StrengthOf(string group, string sub, string how, string role)
        {
            int s;
            switch (group)
            {
                case "blows": s = sub == "rough" ? 75 : 68; break;
                case "theft": s = sub == "pickpocket" ? 62 : 50; break;
                case "quarrel": s = 46; break;
                case "slight": s = 36; break;
                case "drunk": s = 44; break;
                case "warmth": s = sub == "propose" || sub == "makeup" ? 62 : 48; break;
                case "talk": s = 22; break;
                default: s = 30; break;
            }
            if (how == "saw")
                s = s * 2 / 3;   // it happened to someone else
            if (role == "done")
                s += 8;          // it happened to them
            return Math.Min(100, s);
        }

        /// How it felt, -3 .. 3: the scene's own weight (Ties.Effect), sharper for whoever it was done to.
        public static int FeelOf(string sub, string how, string role)
        {
            int e = Ties.Effect(sub) ?? 0;
            if (how == "saw")
                return Math.Sign(e);
            if (role == "done")
                return Math.Max(-3, Math.Min(3, e < 0 ? e - 1 : e + (sub == "charity" || sub == "help" ? 1 : 0)));
            if (role == "doer")
                return e < 0 ? -1 : e;   // a bully or a thief doesn't feel it the way the other did
            return Math.Max(-3, Math.Min(3, e));
        }

        /// Game minutes for a memory to lose half its pull.
        public static double HalfLife(MindMemory m)
        {
            double h;
            switch (m.Kind)
            {
                case "talk": h = 360; break;
                case "quarrel": h = 1080; break;
                case "slight": h = 720; break;
                case "blows": h = 2160; break;
                case "theft": h = 2160; break;
                case "drunk": h = 1080; break;
                case "warmth": h = 1440; break;
                case "rescued": h = 2160; break;
                case "deed:violence": h = 4320; break;   // a killing seen: days
                case "deed:theft": h = 2880; break;
                case "deed:kindness": h = 2160; break;
                case "death": h = m.Sub == "friend" ? 30240 : m.Sub == "seen" ? 4320 : 7200; break;   // a friend: three weeks
                default: h = 1440; break;
            }
            return m.How == "heard" && m.Kind != "death" ? Math.Min(h, 1440) : h;
        }

        // ---- keeping them ---------------------------------------------------------------------------------

        /// Adds a memory to someone's list: the same thing noted twice is one (the stronger kept), and over the cap the
        /// weakest by strength and age goes. The memory that was kept.
        public static MindMemory Note(List<MindMemory> mind, MindMemory m, long now)
        {
            MindMemory same = m.Key != null ? mind.FirstOrDefault(x => x.Key == m.Key) : null;
            if (same != null)
            {
                same.Strength = Math.Max(same.Strength, m.Strength);
                same.DueAt = Math.Min(same.DueAt, m.DueAt);   // seen it with their own eyes: no waiting for word of it
                if (Math.Abs(m.Feel) > Math.Abs(same.Feel))
                    same.Feel = m.Feel;
                return same;
            }
            mind.Add(m);
            while (mind.Count > Max)
                mind.Remove(mind.OrderBy(x => Keep(x, now)).First());
            return m;
        }

        /// What a memory is worth keeping: its strength as time has worn it.
        private static double Keep(MindMemory m, long now)
        {
            return m.Strength * Math.Pow(0.5, Math.Max(0, now - m.At) / HalfLife(m));
        }

        // ---- attention ------------------------------------------------------------------------------------

        /// How hard a memory pulls now: strength, faded by age, plus what brings it back (someone in it in sight, the
        /// same kind of place, near the hour it happened), weighed by temperament, less the times it's been thought.
        public static double Pull(MindMemory m, long now, InnerLife.Context day, string temper)
        {
            double age = Math.Max(0, now - m.At);   // never from the future: that would weigh more than now
            double p = m.Strength * Math.Pow(0.5, age / HalfLife(m));
            if (InSight(m, day))
                p += 20;
            if (day != null && m.Place != null && m.Place == day.Place)
                p += 10;
            long minute = ((now % 1440) + 1440) % 1440, then = ((m.At % 1440) + 1440) % 1440;
            if (Math.Min(Math.Abs(minute - then), 1440 - Math.Abs(minute - then)) <= 90 && age >= 600)
                p += 6;   // this hour yesterday
            p *= TemperWeight(m, temper);
            p -= 8 * m.Times;
            if (m.Times > 0 && now - m.LastAt >= 0 && now - m.LastAt < 90)
                p -= 40;   // just thought of it
            return p;
        }

        /// Someone in it is in sight now.
        public static bool InSight(MindMemory m, InnerLife.Context day)
        {
            return day != null && day.Nearby != null && m.Who != null && day.Nearby.Any(n => n.Id != null && m.Who.Contains(n.Id));
        }

        /// What weighs more on each temperament: the timid feel threats, the greedy money, the loyal their people, the
        /// hot-tempered every slight; the cold feel all of it less, and a drifter cares less who was kind.
        public static double TemperWeight(MindMemory m, string temper)
        {
            switch (temper)
            {
                case "Coward":
                    return m.Kind == "blows" || m.Kind == "quarrel" || m.Kind == "theft" || m.Kind == "deed:violence" || (m.Kind == "death" && m.Sub == "seen") ? 1.3 : 1.0;
                case "Greedy":
                    return m.Kind == "theft" || m.Kind == "deed:theft" || m.Sub == "debt" || m.Sub == "charity" ? 1.3 : 1.0;
                case "Loyal":
                    return m.Kind == "warmth" || m.Kind == "rescued" || m.Kind == "deed:kindness" || (m.Kind == "death" && m.Sub == "friend") || m.Feel > 0 ? 1.3 : 1.0;
                case "Hothead": case "Proud":
                    return m.Kind == "quarrel" || m.Kind == "slight" || m.Kind == "blows" || m.Kind == "deed:violence" ? 1.3 : 1.0;
                case "Cold":
                    return 0.8;
                case "Drifter":
                    return m.Kind == "warmth" ? 0.8 : 1.0;
                default:
                    return 1.0;
            }
        }

        /// Worry by its stage (0 begins, 1 worse, 2 at its worst, 3 the end).
        public static double WorryPull(int stage)
        {
            return stage <= 0 ? 28 : stage == 1 ? 36 : stage == 2 ? 55 : 40;
        }

        /// A little seeded jitter, so the fillers take turns.
        public static double Jitter(string seed, string source, int span)
        {
            return Voice.Hash(seed + "|j|" + source) % (span + 1);
        }

        /// The strongest pull, or now and then the one just behind it (within 15%), so a person isn't a machine. The
        /// candidates in the order they'd be tried (a line that can't be made falls to the next).
        public static List<Candidate> Order(List<Candidate> all, string seed)
        {
            List<Candidate> sorted = all.Where(c => c != null && c.Make != null).OrderByDescending(c => c.Pull).ThenBy(c => c.Source, StringComparer.Ordinal).ToList();
            if (sorted.Count >= 2 && sorted[0].Pull > 0 && sorted[1].Pull >= sorted[0].Pull * 0.85 && Voice.Hash(seed + "|ru") % 100 < 30)
            {
                Candidate second = sorted[1];
                sorted.RemoveAt(1);
                sorted.Insert(0, second);
            }
            return sorted;
        }

        // ---- the words --------------------------------------------------------------------------------------

        /// fresh: the first two times, the same day; chewing: after that, and all the next day; letting go: from two
        /// days on (a bruise can't fade in an afternoon, however often it's thought of).
        public static string Phase(MindMemory m, long now)
        {
            long age = Math.Max(0, now - m.At);
            if (age < 1440)
                return m.Times < 2 ? "fresh" : "chewing";
            return age < 2 * 1440 ? "chewing" : "letting";
        }

        /// Temperaments that see the same fight differently: bold (Hothead, Proud), fearful (Coward), cold (Cold,
        /// Greedy), plain (the rest).
        public static string TemperGroup(string temper)
        {
            switch (temper)
            {
                case "Hothead": case "Proud": return "bold";
                case "Coward": return "fearful";
                case "Cold": case "Greedy": return "cold";
                default: return "plain";
            }
        }

        /// The line for a memory: by its group (or the exact kind first), how they knew it, their part in it, how far
        /// they've got with it and their temperament; with someone in it in sight, now and then a line for that. The
        /// same memory steps through its pool as it's thought again, so it isn't said the same way twice running.
        public static string Line(MindMemory m, Person p, long now, InnerLife.Context day, string seed, string you = null, bool youNear = false)
        {
            string phase = Phase(m, now);
            string tg = TemperGroup(p.Personality);
            string role = m.How == "did" && m.Role != null ? ":" + m.Role : string.Empty;
            string[] pool = null;
            if (((m.How == "did" && InSight(m, day)) || (AboutYou(m) && youNear)) && Voice.Chance(seed + "|near", 55))
                pool = Find(m, role, "near", tg);
            // their own part at this phase, then the plain words at this phase, then the nearest other phase
            string alt = phase == "fresh" ? "chewing" : phase == "chewing" ? "fresh" : "chewing";
            if (pool == null)
                pool = Find(m, role, phase, tg) ?? Find(m, string.Empty, phase, tg) ?? Find(m, role, alt, tg) ?? Find(m, string.Empty, alt, tg) ??
                       Find(m, role, "fresh", tg) ?? Find(m, string.Empty, "fresh", tg);
            if (pool == null || pool.Length == 0)
                return null;
            string raw = pool[(Voice.Hash(m.Key + "|" + p.Id) + m.Times) % pool.Length];
            return Fill(raw, m, p, you);
        }

        /// A memory of something the player did.
        public static bool IsDeed(MindMemory m)
        {
            return m.Kind != null && m.Kind.StartsWith("deed:");
        }

        /// A memory with the player in it (one of his deeds, or his flirting): worded with {you}, and back when he's near.
        public static bool AboutYou(MindMemory m)
        {
            return m.Who != null && m.Who.Contains("player");
        }

        /// The kind of answer a flirt got, for its memory: yes, maybe, no, or afraid (a no from fear of him).
        public static string FlirtSub(string answer, string why)
        {
            if (answer == "yes" || answer == "maybe")
                return "flirt:" + answer;
            return why == "afraid" || why == "fear" || why == "witnessed" || why == "hostile" || why == "hate" ? "flirt:afraid" : "flirt:no";
        }

        private static string[] Find(MindMemory m, string role, string phase, string tg)
        {
            string[] pool;
            foreach (string head in new[] { m.Sub, m.Kind })
            {
                string key = head + "|" + m.How + role + "|" + phase;
                if (head != null && (Pools.TryGetValue(key + "|" + tg, out pool) || Pools.TryGetValue(key, out pool)))
                    return pool;
            }
            return null;
        }

        /// The names as this person knows them; a sentence never starts small ("a farmhand" first is "A farmhand").
        public static string Fill(string raw, MindMemory m, Person p, string you = null)
        {
            string who = m.NameAt(0) ?? "somebody", a = m.NameAt(0) ?? "somebody", b = m.NameAt(1) ?? "somebody";
            string s = raw.Replace("{who}", who).Replace("{a}", a).Replace("{b}", b).Replace("{you}", you ?? "that stranger")
                          .Replace("{town}", string.IsNullOrEmpty(m.Town ?? p.Home) ? "this town" : m.Town ?? p.Home);
            return Sentences(s);
        }

        /// Capitals at the start of each sentence.
        public static string Sentences(string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;
            char[] c = s.ToCharArray();
            bool start = true;
            for (int i = 0; i < c.Length; i++)
            {
                if (start && char.IsLetter(c[i]))
                {
                    c[i] = char.ToUpperInvariant(c[i]);
                    start = false;
                }
                else if (c[i] == '.' || c[i] == '!' || c[i] == '?')
                    start = true;
                else if (!char.IsWhiteSpace(c[i]) && c[i] != '"' && c[i] != '\'')
                    start = false;
            }
            return new string(c);
        }

        /// What someone they don't know by name looked like, from a short trade or kind ("a farmhand", "a lady").
        public static string LabelOf(string trade, bool woman)
        {
            if (string.IsNullOrEmpty(trade))
                return woman ? "a woman" : "a fellow";
            string t = trade.Trim();
            if (t.StartsWith("a ", StringComparison.OrdinalIgnoreCase) || t.StartsWith("an ", StringComparison.OrdinalIgnoreCase))
                return Grammar.LowerFirst(t);
            return (woman ? "a woman, " : "a fellow, ") + Grammar.LowerFirst(t);
        }
    }
}
