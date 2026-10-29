// The Sabbath (overnight 2026-10-27, Tier 4): on a Sunday morning by the church, one of the devout kneels a while in the
// churchyard with a prayer of their own, and anyone lapsed or godless nearby who sees it feels watched. Thoughts only,
// over their heads; the kneel is the game's own graveside scenario, which ends by itself. All lines are original to this
// add-on (the opening words of the Lord's Prayer are the prayer book's).
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Sabbath
    {
        /// Sunday morning: from 08:00 to noon.
        public static bool Hour(int weekday, int clock)
        {
            return weekday == 0 && clock >= 8 && clock < 12;
        }

        /// Who kneels: the devout always, a churchgoer one Sunday in three (by the seed), never the lapsed or godless.
        public static bool Kneels(string devotion, string seed)
        {
            return devotion == "Devout" || (devotion == "Churchgoer" && Voice.Hash(seed + "|kneel") % 3 == 0);
        }

        /// Who feels watched: the lapsed and the godless.
        public static bool Watched(string devotion)
        {
            return devotion == "Lapsed" || devotion == "Godless";
        }

        private static readonly string[] Prayers =
        {
            "Forgive me Tuesday. You know the one.",
            "Keep the rain off the hay. And Pa off the bottle. In that order.",
            "Our Father, who art... Lord, my knees.",
            "Grateful for the week. Even Wednesday.",
            "I'm not asking for much. A little, maybe. You'll know how much.",
            "Let them talk. You and I know how it was.",
            "Watch over the ones I can't. That's all of them, near enough.",
            "Gravel in my knee. Good. Ought to cost something.",
            "Rain for the corn, Lord. Not so much for the roof.",
            "Make me better than I was on Tuesday."
        };

        private static readonly string[] Lapsed =
        {
            "Haven't been inside since Mother's funeral. Feels like the steeple knows it.",
            "Everybody in their Sunday best. Me in yesterday's shirt.",
            "They're all looking. Let them look.",
            "Ought to go in. Ought to do a lot of things.",
            "Used to know every hymn. Couldn't tell you one now.",
            "Mother would have my ear for walking past that door."
        };

        private static readonly string[] Godless =
        {
            "Kneeling in the dirt for a man in the sky. Well.",
            "Waste of a good morning, and they'll tell me I'm the fool.",
            "Bell's ringing. Ring away. I've got a horse to shoe.",
            "Prayed every night for a year once. The hay still burned."
        };

        public static string Prayer(string seed)
        {
            return Voice.Pick(seed + "|pr", Prayers);
        }

        public static string WatchedThought(string devotion, string seed)
        {
            return Voice.Pick(seed + "|wt", devotion == "Godless" ? Godless : Lapsed);
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string l in Prayers) yield return l;
            foreach (string l in Lapsed) yield return l;
            foreach (string l in Godless) yield return l;
        }
    }
}
