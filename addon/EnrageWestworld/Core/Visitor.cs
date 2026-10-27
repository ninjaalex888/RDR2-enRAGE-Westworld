// The night visitor (roadmap 16, night encounters; overnight 2026-10-27, Tier 2.8): after midnight in a town's dark
// corner, a pale figure crouched over a body. It rises and stares when the player comes near, is gone the moment he
// looks away, turns up once behind him, and is gone. Come morning the body is gone too, and the town speaks of a body
// nobody can account for. Someone out late may come upon it first and run. The game side does the figure, the body
// and the looking; this has the words and the timing. All lines are original to this add-on.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class Visitor
    {
        /// The hours it keeps: after midnight, before four.
        public static bool Hour(int hour)
        {
            return hour >= 0 && hour < 4;
        }

        /// What the town says next morning (no names: nobody knows whose body, and nobody saw the figure clearly).
        public static string Morning(string town, string seed)
        {
            string t = string.IsNullOrEmpty(town) ? "town" : town;
            return Voice.Pick(seed + "vm",
                "Somebody saw a body behind the buildings last night. By sunup there was nothing there. Not even blood.",
                "Talk in " + t + " of a dead man in the dark and somebody bent over him. Gone by morning, both of them.",
                "A body in the alley, they say, and a pale thing crouched over it. Sheriff went to look. Found nothing.");
        }

        /// What someone out late thinks as they run from it.
        public static string Fled(string seed)
        {
            return Voice.Pick(seed + "vf", "Somebody's over that body. Not somebody. Something.", "Don't look back. Lord, don't look back.", "Its face. White as a plate.");
        }

        public static IEnumerable<string> Lines()
        {
            for (int i = 0; i < 12; i++)
            {
                yield return Morning(i % 2 == 0 ? "Valentine" : null, "m" + i);
                yield return Fled("f" + i);
            }
        }
    }
}
