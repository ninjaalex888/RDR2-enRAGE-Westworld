// Word of the player's deeds travels town to town, a hop a day or so, and bends further with every mile:
// numbers grow, his name blurs, he gets bigger, meaner, harder to kill. Each town keeps its own version.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class Rumours
    {
        // rough centres, only used for distances between towns
        public static readonly Dictionary<string, float[]> Towns = new Dictionary<string, float[]>
        {
            { "Valentine", new[] { -281f, 790f } }, { "Rhodes", new[] { 1330f, -1310f } }, { "Saint Denis", new[] { 2630f, -1290f } },
            { "Blackwater", new[] { -810f, -1320f } }, { "Strawberry", new[] { -1790f, -390f } }, { "Annesburg", new[] { 2930f, 1290f } },
            { "Van Horn", new[] { 2960f, 560f } }, { "Armadillo", new[] { -3680f, -2620f } }, { "Tumbleweed", new[] { -5510f, -2940f } },
            { "Emerald Ranch", new[] { 1430f, 320f } }, { "Lagras", new[] { 2125f, -605f } }, { "Colter", new[] { -1350f, 2400f } },
            { "Thieves' Landing", new[] { -1400f, -2300f } }, { "Wapiti", new[] { 450f, 2220f } }, { "Manzanita Post", new[] { -1950f, -1600f } },
            { "Flatneck Station", new[] { -340f, -350f } }, { "Riggs Station", new[] { -1100f, -560f } }, { "Butcher Creek", new[] { 2500f, 800f } },
            { "Benedict Point", new[] { -5250f, -3450f } }
        };

        private static readonly string[] Major = { "Valentine", "Rhodes", "Saint Denis", "Blackwater", "Strawberry", "Annesburg", "Armadillo", "Van Horn", "Tumbleweed" };

        public static float Distance(string a, string b)
        {
            float[] pa, pb;
            if (a == null || b == null || !Towns.TryGetValue(a, out pa) || !Towns.TryGetValue(b, out pb))
                return 99999f;
            float dx = pa[0] - pb[0], dy = pa[1] - pb[1];
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        public static string NearestTown(float x, float y)
        {
            string best = null;
            float bd = float.MaxValue;
            foreach (KeyValuePair<string, float[]> t in Towns)
            {
                float dx = t.Value[0] - x, dy = t.Value[1] - y;
                float d = dx * dx + dy * dy;
                if (d < bd)
                {
                    bd = d;
                    best = t.Key;
                }
            }
            return best;
        }

        private static int MaxHops(Deed d)
        {
            return d.Weight >= 60 ? 4 : d.Weight >= 30 ? 2 : d.Weight >= 12 ? 1 : 0;
        }

        /// A deed becomes talk in its own town.
        public static TownRumour Start(Store s, Deed d, long now)
        {
            string town = d.Town ?? NearestTown(d.X, d.Y);
            if (town == null || d.Spread)
                return null;
            d.Spread = true;
            TownRumour r = new TownRumour
            {
                Id = Ids.New(), DeedId = d.Id, Origin = town, Town = town, Hops = 0, At = now,
                Fidelity = d.Witnesses > 0 ? 100 : 85, NextAt = now + 720 + (int)(Voice.Hash(d.Id) % 720)
            };
            s.TownRumours.Add(r);
            return r;
        }

        /// Moves talk on to the next towns. Returns the rumours that arrived somewhere new.
        public static List<TownRumour> Spread(Store s, long now)
        {
            List<TownRumour> arrived = new List<TownRumour>();
            Dictionary<string, Deed> deeds = s.Deeds.ToDictionary(d => d.Id, d => d);
            foreach (TownRumour r in s.TownRumours.ToList())
            {
                Deed d;
                if (r.NextAt <= 0 || now < r.NextAt || !deeds.TryGetValue(r.DeedId, out d))
                    continue;
                r.NextAt = 0;   // spreads once, outward
                if (r.Hops >= MaxHops(d))
                    continue;
                IEnumerable<string> next3 = Towns.Keys.Where(t => t != r.Town).OrderBy(t => Distance(r.Town, t)).Take(3);
                IEnumerable<string> bigTowns = Major.Where(t => t != r.Town && Distance(r.Town, t) < 3200f).OrderBy(t => Distance(r.Town, t)).Take(2);
                foreach (string town in next3.Concat(bigTowns).Distinct().ToList())
                {
                    float dist = Distance(r.Town, town);
                    if (dist > 3200f || s.TownRumours.Any(x => x.DeedId == r.DeedId && x.Town == town))
                        continue;
                    int loss = 6 + (int)(dist / 200f) + (int)(Voice.Hash(r.Id + town) % 7);
                    TownRumour next = new TownRumour
                    {
                        Id = Ids.New(), DeedId = r.DeedId, Origin = r.Origin, Town = town, Hops = r.Hops + 1,
                        Fidelity = Math.Max(5, r.Fidelity - loss), At = now, NextAt = now + 900 + (int)(dist / 3f)
                    };
                    s.TownRumours.Add(next);
                    arrived.Add(next);
                }
            }
            return arrived;
        }

        /// This town's version of what happened, third person.
        public static string Version(TownRumour r, Deed d, PlayerRef you, bool nameKnown)
        {
            string seed = r.Id;
            int f = r.Fidelity;
            Deed told = new Deed
            {
                Id = d.Id, Kind = d.Kind, Detail = d.Detail, Town = d.Town, Zone = d.Zone, Witnesses = d.Witnesses, Count = d.Count, Weight = d.Weight, Unseen = d.Unseen
            };
            if (f < 75 && (Deeds.IsViolent(d.Kind) || d.Kind == "hunt"))
                told.Count = f < 35 ? Math.Max(d.Count * 4, 6) : f < 55 ? d.Count * 3 : d.Count + 1;
            // nobody lived to tell who did it (Knowing.cs): the talk never names him, however it grows in the telling
            string who = d.Unseen ? Voice.Pick(seed + "u", "somebody", "someone nobody saw", "whoever it was") : Who(you, nameKnown, f, seed);
            string text = Deeds.Rumour(told, who);
            if (text == null)
                return null;
            if (f < 75 && Deeds.IsViolent(d.Kind))
                text = text.Replace(" killed ", " gunned down ");
            if (d.Unseen)
                return text;
            if (f < 55)
                text += " " + Voice.Pick(seed + "x", "in broad daylight", "and never even blinked", "and rode off laughing", "and nobody lifted a finger");
            if (f < 35)
                text += Voice.Pick(seed + "y", ". They say bullets go right through him", ". They say he can't be killed", ". Some say he's the devil himself",
                    ". They say he's been dead twice already");
            return text;
        }

        private static string Who(PlayerRef you, bool nameKnown, int f, string seed)
        {
            if (f >= 80 && nameKnown)
                return you.FullName;
            if (f >= 60)
                return nameKnown ? Voice.Pick(seed, "a man called " + you.Surname, "that " + you.Surname + " fella") : you.Scarred ? "a scarred stranger" : Voice.Pick(seed, "a stranger", "a big fella in a worn hat");
            if (f >= 40)
                return nameKnown ? Voice.Pick(seed, "a man called " + Blur(you.Surname, seed) + ", or something like it", "some outlaw named " + Blur(you.Surname, seed))
                    : Voice.Pick(seed, "some outlaw", "a gunman nobody knew", "a giant of a man");
            return Voice.Pick(seed, "a devil on a black horse", "the man they can't kill", "a whole gang of them", "a ghost in a gray hat");
        }

        private static string Blur(string surname, string seed)
        {
            if (string.IsNullOrEmpty(surname))
                return "Smith";
            switch (surname)
            {
                case "Morgan": return Voice.Pick(seed, "Morton", "Morrison", "Mogan");
                case "Marston": return Voice.Pick(seed, "Marsden", "Marlston", "Martin");
            }
            return surname.Substring(0, Math.Min(3, surname.Length)) + "son";
        }

        /// How a local tells it: "Word from Valentine: ..." when it came from elsewhere.
        public static string Telling(TownRumour r, string version)
        {
            if (string.IsNullOrEmpty(version))
                return null;
            string v = Grammar.SoftLower(version.TrimEnd('.'));
            if (r.Town == r.Origin)
                return Grammar.Sentence(Voice.Pick(r.Id, "They're saying " + v, "Word around town: " + v, "Folks here say " + v));
            return Grammar.Sentence(Voice.Pick(r.Id, "Word from " + r.Origin + ": " + v, "They say over in " + r.Origin + ", " + v,
                "News came up from " + r.Origin + ": " + v));
        }
    }
}
