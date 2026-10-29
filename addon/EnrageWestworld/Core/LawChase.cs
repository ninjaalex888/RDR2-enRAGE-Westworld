// A lawman runs a thief down (2026-10-29, Lex, after watching the game's own police chase and tackle a man in Saint
// Denis: "Could we get this NPC interaction into the mod?"). When one of RealNPCs' pickpockets runs and a lawman is
// near, he gives chase, and close enough he brings the thief down and holds him for arrest. The game side does the
// running and the tackle; this has the words. All lines are original to this add-on.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class LawChase
    {
        /// The lawman, setting off after him.
        public static string Shout(string seed)
        {
            return Voice.Pick(seed + "|ls", "Stop right there! Stop, thief!", "You! Hold it! In the name of the law!", "Somebody stop that man! Out of my way!");
        }

        /// The lawman, with the thief down.
        public static string Got(string seed)
        {
            return Voice.Pick(seed + "|lg", "Got you. You'll see the inside of a cell for this.", "Stay down. Stay down, I said.", "That's enough running for one day.");
        }

        /// The one robbed, to the lawman.
        public static string Thanks(bool woman, string seed)
        {
            return Voice.Pick(seed + "|lt", "Thank you, deputy. I thought it was gone for good.", "Bless you, officer. Bless you.",
                woman ? "My purse. Oh, thank heaven. Thank you, sir." : "My wallet. Thank you, deputy. Hang him high.");
        }

        public static IEnumerable<string> Lines()
        {
            for (int i = 0; i < 8; i++)
            {
                yield return Shout("lc" + i);
                yield return Got("lc" + i);
                yield return Thanks(i % 2 == 0, "lc" + i);
            }
        }
    }
}
