// A night together, off screen (0.6.3, Lex): after a woman says yes to the player's flirting, flirting with her again
// within two minutes takes them somewhere quieter. The screen goes dark and an hour passes; nothing is shown. These are
// the words said before and what she thinks after, by who she is: one of the saloon's women, a single woman, or one
// married or spoken for. To be widened later (roadmap: places, a price at the saloon, a husband who finds out).
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Trysts
    {
        public const int OfferMs = 120000;   // how long after her yes the second flirt still counts

        /// "saloon", "taken" or "single".
        public static string Kind(bool saloonGirl, bool spoken)
        {
            return saloonGirl ? "saloon" : spoken ? "taken" : "single";
        }

        /// What the player says, asking.
        public static string Ask(string seed)
        {
            return Voice.Pick(seed + "ta", "Somewhere quieter, maybe?", "Walk with me a while. Away from all these eyes.", "Care to keep me company a spell?");
        }

        /// Her yes, by who she is.
        public static string Yes(string kind, string seed)
        {
            switch (kind)
            {
                case "saloon": return Voice.Pick(seed + "ty", "Upstairs, then. Mind the third step, it creaks.", "Come on. I know a room with a lock on the door.");
                case "taken": return Voice.Pick(seed + "ty", "Not here. Somewhere nobody knows my face.", "Just this once. And you never saw me.");
                default: return Voice.Pick(seed + "ty", "Lead the way, before I think better of it.", "I was starting to think you'd never ask.");
            }
        }

        /// What she thinks after, on the ticker over her head.
        public static string After(string kind, string seed)
        {
            switch (kind)
            {
                case "saloon": return Voice.Pick(seed + "tf", "He never said a word about his troubles. They all have troubles.", "Gentle, for a man with that many scars.");
                case "taken": return Voice.Pick(seed + "tf", "Supper on the table by six. Nobody will ever know. Nobody.", "What have I done. Lord, what have I done.");
                default: return Voice.Pick(seed + "tf", "I'll be smiling at nothing all day.", "Mother would faint. Mother isn't here.");
            }
        }

        /// Every line, for the tests.
        public static IEnumerable<string> AllLines()
        {
            foreach (string k in new[] { "saloon", "taken", "single" })
                for (int i = 0; i < 12; i++)
                {
                    yield return Yes(k, "s" + i);
                    yield return After(k, "s" + i);
                }
            for (int i = 0; i < 12; i++)
                yield return Ask("s" + i);
        }
    }
}
