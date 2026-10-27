// The noon callout (overnight 2026-10-27, Tier 2.10): about noon in a town street, one man summons another he has bad
// blood with, they square up across the street (guns drawn only when both carry one and no lawman is near, as in a
// standoff; else they stare it out), the street looks on, and one backs down. Nobody fires. A calls B out. All lines
// are original to this add-on.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static partial class Scenes
    {
        /// Which of the two backs down: the cooler head (Coward, then the plain, the cold, the proud, the hothead last);
        /// between equals, the seed. True: B backs down.
        public static bool CalloutBBacks(string aTemper, string bTemper, string seed)
        {
            int ra = Heat(aTemper), rb = Heat(bTemper);
            if (ra != rb)
                return rb < ra;
            return Voice.Hash(seed + "|backs") % 2 == 0;
        }

        internal static int Heat(string temper)
        {
            switch (temper)
            {
                case "Coward": return 0;
                case "Cold": return 2;
                case "Proud": return 3;
                case "Hothead": return 4;
                default: return 1;
            }
        }

        /// Whether these two have it in them to call each other out: bad blood past its first quarrel, or a hothead
        /// stung by a fresh slight.
        public static bool CalloutWorthy(int warmth, int stage, string tieKind, long ageMinutes, string aTemper)
        {
            if (warmth < 0 && stage >= 1)
                return true;
            bool slight = tieKind == "scorn" || tieKind == "brushoff" || tieKind == "faced" || tieKind == "passby" || tieKind == "argue";
            return aTemper == "Hothead" && slight && warmth <= 0 && ageMinutes >= 0 && ageMinutes < 1440;
        }

        public static Scene Callout(SceneCast c, string seed, bool bBacks)
        {
            string a = N(c, 0, seed), b = N(c, 1, seed);
            Scene s = new Scene { Kind = "standoff", Cast = c.HasCrowd ? 3 : 2, Summary = "a man called another out into the street at noon" };
            s.Add(0, Voice.Pick(seed + "co", b + "! Out here, where folks can see.", "Noon, " + b + ". You and me. Out in the street.", b + ". You know what this is about. Step out."), "threat", "face")
             .Add(1, Voice.Pick(seed + "cb", "Here I am, " + a + ".", "You sure about this, " + a + "?", "About time, " + a + "."), "stare")
             .Add(0, null, c.Armed0 ? "armed" : null, "aim", 300).Add(1, null, null, "aim", 300);
            if (c.HasCrowd)
                s.Add(2, Voice.Pick(seed + "cc", "Somebody fetch the sheriff!", "Get off the street, children!", "Not at noon, boys. Not with the church right there.", "Easy now. Easy."),
                      Voice.Chance(seed + "cv", 50) ? "intervene" : "cheer");
            s.Add(0, Voice.Pick(seed + "c1", "Been coming a long while, this.", "Say your piece, " + b + ".", "Whole town's watching. Make it count."), "threat")
             .Add(1, Voice.Pick(seed + "c2", "Ain't a word left in me for you.", "Talk's cheap, " + a + ".", "Go on, then."), "stare");
            int backer = bBacks ? 1 : 0, other = 1 - backer;
            string bn = bBacks ? b : a, on = bBacks ? a : b;
            s.Add(backer, Voice.Pick(seed + "cx", "...Ain't worth dying over.", "Not today, " + on + ". Not for this.", "You can have the street."), "storm", "holster")
             .Add(other, Voice.Pick(seed + "cw", "That's what I figured.", "Go on home, " + bn + ".", "Smart. Smarter than I took you for."), "won", "holster")
             .Add(backer, null, null, "walkaway");
            return s;
        }

        public static IEnumerable<Scene> CalloutSamples(SceneCast c)
        {
            for (int i = 0; i < 12; i++)
                foreach (bool b in new[] { false, true })
                    yield return Callout(c, "co" + i, b);
        }
    }
}
