// A stranded traveller asking the way (overnight 2026-10-27, Tier 2.9): by day on a country road, a man on
// foot waves the player down and asks the way to the nearest town. A greeting (or stopping with him a moment) is an
// answer: he works it out, points the way, thanks him and walks off that way. Antagonized, he backs off. Ridden past,
// he mutters and goes on. The game side does the spawning, the looking and the walking; this has the towns and the
// words. All lines are original to this add-on.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class Travellers
    {
        /// The towns a traveller asks for, at about the middle of each on the map (x, y): near enough to point the way.
        public static readonly Dictionary<string, float[]> Towns = new Dictionary<string, float[]>
        {
            { "Valentine", new[] { -280f, 790f } }, { "Rhodes", new[] { 1300f, -1300f } }, { "Saint Denis", new[] { 2620f, -1250f } },
            { "Strawberry", new[] { -1800f, -400f } }, { "Blackwater", new[] { -800f, -1300f } }, { "Annesburg", new[] { 2930f, 1330f } },
            { "Armadillo", new[] { -3660f, -2600f } }, { "Van Horn", new[] { 2970f, 570f } }, { "Emerald Ranch", new[] { 1420f, 330f } },
        };

        /// The nearest town to a point, and how far.
        public static string Nearest(float x, float y, out float metres, out float tx, out float ty)
        {
            string best = null;
            metres = float.MaxValue;
            tx = ty = 0f;
            foreach (KeyValuePair<string, float[]> t in Towns)
            {
                float dx = t.Value[0] - x, dy = t.Value[1] - y, d = (float)Math.Sqrt(dx * dx + dy * dy);
                if (d < metres)
                {
                    metres = d;
                    best = t.Key;
                    tx = t.Value[0];
                    ty = t.Value[1];
                }
            }
            return best;
        }

        public static bool Hour(int hour)
        {
            return hour >= 7 && hour < 19;
        }

        public static string Hail(string seed)
        {
            return Voice.Pick(seed + "th", "Mister! Hold up a moment!", "Say there! You, on the horse!", "Excuse me! Sir!");
        }

        public static string Ask(string town, string seed)
        {
            return Voice.Pick(seed + "ta", "Which way's " + town + "? Been walking since sunup and I'm turned clean around.",
                "Is this the road to " + town + "? Every fork looks the same out here.", "You know " + town + "? Lost my horse two days back and my sense of direction with it.");
        }

        /// He works it out from what the player says (we don't hear it), points and thanks him.
        public static string Thanks(string town, string seed)
        {
            return Voice.Pick(seed + "tt", "That way? Obliged, mister. I'd have walked to the coast.", "Past the ridge, then on to " + town + ". Thank you kindly.", "Right. That way. Bless you.");
        }

        public static string Refused(string seed)
        {
            return Voice.Pick(seed + "tr", "...Alright. Alright, I'm going.", "No call for that. I only asked.", "Fine. I'll find it myself.");
        }

        public static string Ignored(string seed)
        {
            return Voice.Pick(seed + "ti", "Thank you for nothing.", "Nobody stops out here.", "Suppose I'll follow the wheel ruts.");
        }

        public static IEnumerable<string> Lines()
        {
            for (int i = 0; i < 9; i++)
            {
                string town = Towns.Keys.ElementAt(i % Towns.Count);
                yield return Hail("h" + i);
                yield return Ask(town, "a" + i);
                yield return Thanks(town, "t" + i);
                yield return Refused("r" + i);
                yield return Ignored("i" + i);
            }
        }
    }
}
