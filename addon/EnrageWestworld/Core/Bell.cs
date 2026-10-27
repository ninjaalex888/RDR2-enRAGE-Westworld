// The bell at three (overnight 2026-10-27, Tier 4, West World): at three in the morning in a town with a church, the
// awake (wake stage 2 and on) near the player stop and look toward the church at a bell only they hear; anyone not awake
// beside them wonders what they're staring at. No sound is played: the player hears nothing, which is the point.
// Thoughts only, by wake stage. All lines are original to this add-on.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Bell
    {
        /// From 03:00 to 03:15 game time.
        public static bool Hour(int clock, int minute)
        {
            return clock == 3 && minute < 15;
        }

        /// Who hears it: the questioning, the unravelling and the awake.
        public static bool Hears(int wakeStage)
        {
            return wakeStage >= 2;
        }

        private static readonly string[] Questioning =
        {
            "A bell. At three. Who rings a bell at three?",
            "Church bell. Counted three. Then a fourth that wasn't there.",
            "Nobody else is turning round. Nobody else hears it.",
            "Three strokes, clear as day. And the bell rope tied up, I'd swear to it."
        };

        private static readonly string[] Unravelling =
        {
            "Bell again. Same as last night. Same as every night.",
            "Nobody's in that church. I looked. Still it rings.",
            "Every night at three. And every morning nobody remembers.",
            "I counted the strokes again. Always three. Never a fourth. Why three?"
        };

        private static readonly string[] Awake =
        {
            "Three o'clock. The bell for the ones who know.",
            "They ring it to see who wakes. I'm awake.",
            "Ring all you like. I'm not going back to sleep.",
            "There it is. I'd miss it now, if it stopped."
        };

        private static readonly string[] Puzzled =
        {
            "Why's {who} staring at the church at this hour?",
            "{who}'s gone still as a post. Looking at nothing.",
            "What's {who} listening for? There's nothing to hear.",
            "{who}'s turned to the church like it called a name."
        };

        public static string Thought(int wakeStage, string seed)
        {
            return Voice.Pick(seed + "|bell", wakeStage >= 4 ? Awake : wakeStage == 3 ? Unravelling : Questioning);
        }

        /// One not awake, beside one who is: they hear nothing, and wonder.
        public static string PuzzledBy(string who, string seed)
        {
            return Voice.Pick(seed + "|puz", Puzzled).Replace("{who}", who ?? "that one");
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string l in Questioning) yield return l;
            foreach (string l in Unravelling) yield return l;
            foreach (string l in Awake) yield return l;
            foreach (string l in Puzzled) yield return l.Replace("{who}", "Abe");
        }
    }
}
