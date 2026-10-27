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

        /// Whether her yes to his flirting is a yes to the night too, in percent (Lex, 2026-10-26: greet her, then flirt,
        /// then ask; flirting cold does worse). greeted: 1 he greeted her first, -1 he didn't, 0 not known.
        public static int NightChance(string kind, int greeted)
        {
            switch (kind)
            {
                case "saloon": return greeted < 0 ? 80 : 100;
                case "taken": return greeted > 0 ? 70 : greeted < 0 ? 40 : 55;
                default: return greeted > 0 ? 85 : greeted < 0 ? 55 : 70;
            }
        }

        /// The day's roll for the night, the same all day for her.
        public static bool NightYes(string id, long day, int chance)
        {
            return (uint)Voice.Hash((id ?? "") + "|night|" + day) % 100u < chance;
        }

        /// Her no to the night, when she liked the flirting well enough: a cold approach hears about it.
        public static string NotTonight(string kind, int greeted, string seed)
        {
            if (greeted < 0)
                return Voice.Pick(seed + "tn", "You didn't so much as say good evening. Not tonight.", "Manners first, mister. Then we'll see.",
                    "A woman likes to be greeted before she's asked a thing like that.");
            switch (kind)
            {
                case "saloon": return Voice.Pick(seed + "tn", "Not tonight. I'm spoken for till the piano stops.", "Another night, cowboy. Tonight I'm tired.");
                case "taken": return Voice.Pick(seed + "tn", "I can't. Lord knows I'd like to, but I can't.", "Not tonight. Not with him home.");
                default: return Voice.Pick(seed + "tn", "Slow down. I said I liked you, not that I'd follow you anywhere.", "Another time, maybe. Not tonight.");
            }
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
                    yield return NotTonight(k, 1, "s" + i);
                    yield return NotTonight(k, -1, "s" + i);
                }
            for (int i = 0; i < 12; i++)
                yield return Ask("s" + i);
        }
    }
}
