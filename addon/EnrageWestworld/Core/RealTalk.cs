// Conversations in their real voices (roadmap 1): two people trade the game's own recorded lines, the ones it pairs
// for NPCs talking to each other, played by name in each speaker's own voice. Nothing of the game's is kept here:
// only line names and, for overheard talk, short captions in the add-on's own words (one per kind of line, never a
// paraphrase of a particular recording). The words themselves come only from the player's own voices\words.txt.
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public sealed class TalkTurn
    {
        public int Who;           // 0 the one who starts, 1 the other
        public string[] Lines;    // the first of these the speaker's voice has is played
    }

    public static class RealTalk
    {
        /// Asking how the other is getting on, and their answer.
        public static readonly TalkTurn[] HowsItGoing =
        {
            new TalkTurn { Who = 0, Lines = new[] { "HOWS_IT_GOING" } },
            new TalkTurn { Who = 1, Lines = new[] { "RESPONSE_WORKING_HARD", "RESPONSE_GENERIC", "RESPONSE_IDLING" } }
        };

        private static readonly Dictionary<string, string> Captions = new Dictionary<string, string>
        {
            { "HOWS_IT_GOING", "[asks how they're getting on]" },
            { "RESPONSE_WORKING_HARD", "[says work keeps them busy]" },
            { "RESPONSE_GENERIC", "[agrees]" },
            { "RESPONSE_IDLING", "[says not to mind them]" }
        };

        /// The caption for a line, in the add-on's own words.
        public static string Caption(string line)
        {
            string c;
            return line != null && Captions.TryGetValue(line, out c) ? c : "[says a few words]";
        }

        /// Every caption, for the tests and the overlap checks.
        public static IEnumerable<string> AllLines()
        {
            return Captions.Values.Concat(new[] { "[says a few words]" });
        }
    }
}
