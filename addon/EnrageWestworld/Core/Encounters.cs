// Encounters ([Encounters] in the ini): rare, unsettling things that happen out in the wilds at night, after the
// creepiest of the game's own (themes only: no names, places or lines of Rockstar's are used).
//   The crying woman: a woman kneeling and sobbing out in the dark. Get close and she lunges with a knife and runs, or
//   men step out of the brush, or she says something that makes no sense and walks off into the night.
// This file holds the decisions and the words; Game/WestworldScript.Encounters.cs plays them. All lines are original.
using System;

namespace EnrageWestworld.Core
{
    public static class Encounters
    {
        /// The night hours an encounter keeps (wrapping past midnight).
        public static bool Night(int hour, int from, int to)
        {
            return from <= to ? hour >= from && hour < to : hour >= from || hour < to;
        }

        /// What the crying woman does when the player comes close: "knife" (half the time), "ambush" or "dark".
        public static string CryingOutcome(int roll)
        {
            roll = ((roll % 100) + 100) % 100;
            return roll < 50 ? "knife" : roll < 85 ? "ambush" : "dark";
        }

        /// How many men step out of the brush.
        public static int AmbushMen(int roll)
        {
            return 2 + ((roll % 2) + 2) % 2;
        }

        public static readonly string[] Cries =
        {
            "Please... somebody, please.",
            "I can't find him. I can't find him anywhere.",
            "Don't leave me out here. Not again.",
            "It's so cold in the water.",
            "Is someone there? I hear you. I hear your horse."
        };

        public static readonly string[] KnifeTurn =
        {
            "There you are.",
            "Closer. A little closer.",
            "You always stop for me."
        };

        public static readonly string[] AmbushCall =
        {
            "This one stopped.",
            "Told you one would come."
        };

        public static readonly string[] IntoTheDark =
        {
            "You came last night too. You always come.",
            "Every night you stop. Every night you walk on.",
            "Go home. It starts again at dark."
        };

        public static string Pick(string[] pool, string seed)
        {
            return Voice.Pick(seed, pool);
        }
    }
}
