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

        /// Passing on a piece of news or gossip (CHAT_RESPONSE_A..E), and the other's reaction to it: the answer with the
        /// same letter (RESPONSE_A..E), else a general one. The letters are tried in a different order each time.
        public static readonly TalkTurn[] Gossip =
        {
            new TalkTurn { Who = 0, Lines = new[] { "CHAT_RESPONSE_A", "CHAT_RESPONSE_B", "CHAT_RESPONSE_C", "CHAT_RESPONSE_D", "CHAT_RESPONSE_E" } },
            new TalkTurn { Who = 1, Lines = new[] { "RESPONSE_{letter}", "RESPONSE_GENERIC" } }
        };

        /// Two people who know each other saying hello.
        public static readonly TalkTurn[] Greeting =
        {
            new TalkTurn { Who = 0, Lines = new[] { "GREET_GENERAL_FAMILIAR", "GREET_GENERAL_STRANGER" } },
            new TalkTurn { Who = 1, Lines = new[] { "GREET_GENERAL_FAMILIAR", "GREET_GENERAL_STRANGER" } }
        };

        /// Remarking on a fine day, and the other agreeing (only in fine weather, by daylight).
        public static readonly TalkTurn[] GoodWeather =
        {
            new TalkTurn { Who = 0, Lines = new[] { "CHAT_GOOD_WEATHER" } },
            new TalkTurn { Who = 1, Lines = new[] { "RESPONSE_GOOD_WEATHER", "RESPONSE_GENERIC" } }
        };

        /// Grumbling about the weather, and the other answering (only in rain, storm or snow).
        public static readonly TalkTurn[] BadWeather =
        {
            new TalkTurn { Who = 0, Lines = new[] { "CHAT_BAD_WEATHER" } },
            new TalkTurn { Who = 1, Lines = new[] { "RESPONSE_BAD_WEATHER", "RESPONSE_GENERIC" } }
        };

        /// The kinds a test conversation tries, in order, until one opens.
        public static readonly TalkTurn[][] Kinds = { HowsItGoing, Gossip, Greeting };

        // the game's weather types (RDR2.WeatherType), sorted by what people would say about them
        private static readonly HashSet<uint> Fine = new HashSet<uint> { 1632247697u, 4121459557u, 1840358669u };   // sunny, high pressure, clearing
        private static readonly HashSet<uint> Foul = new HashSet<uint>
        {
            1420204096u, 3061285535u, 2082228755u, 2572975940u, 3878058453u, 212278652u, 1974067816u,   // rain, thunder, thunderstorm, drizzle, shower, sleet, hail
            4021743606u, 603685163u, 669657108u, 725623432u, 2137137442u, 2977915153u, 839715153u      // snow, light snow, blizzard, whiteout, ground blizzard, sandstorm, hurricane
        };

        /// "fine", "foul" or null (overcast, fog: nothing worth remarking on) for the game's weather type.
        public static string WeatherKind(uint weather)
        {
            return Fine.Contains(weather) ? "fine" : Foul.Contains(weather) ? "foul" : null;
        }

        /// The kinds that fit now, in the order to try them: the weather first when it's worth a word, then the rest
        /// in an order that changes with the seed, so the same opening doesn't always come first.
        public static List<TalkTurn[]> KindsFor(string weather, int hour, string seed)
        {
            List<TalkTurn[]> kinds = new List<TalkTurn[]>();
            if (weather == "foul")
                kinds.Add(BadWeather);
            else if (weather == "fine" && hour >= 7 && hour < 19)
                kinds.Add(GoodWeather);
            List<TalkTurn[]> rest = new List<TalkTurn[]> { HowsItGoing, Gossip, Greeting };
            int start = Voice.Hash(seed + "kind") % rest.Count;
            for (int i = 0; i < rest.Count; i++)
                kinds.Add(rest[(start + i) % rest.Count]);
            return kinds;
        }

        /// The lines for a turn, after what was said before it: a reaction answers the letter of the gossip it follows.
        public static string[] LinesFor(TalkTurn turn, string before)
        {
            string letter = before != null && before.StartsWith("CHAT_RESPONSE_") ? before.Substring("CHAT_RESPONSE_".Length) : null;
            return turn.Lines.Where(l => letter != null || !l.Contains("{letter}"))
                             .Select(l => letter != null ? l.Replace("{letter}", letter) : l).ToArray();
        }

        /// The gossip letters in a different order each time (seeded), so the same news isn't always first.
        public static string[] Shuffled(string[] lines, string seed)
        {
            return lines.OrderBy(l => Voice.Hash(seed + l)).ToArray();
        }

        private static readonly Dictionary<string, string> Captions = new Dictionary<string, string>
        {
            { "HOWS_IT_GOING", "[asks how they're getting on]" },
            { "RESPONSE_WORKING_HARD", "[says work keeps them busy]" },
            { "RESPONSE_GENERIC", "[agrees]" },
            { "RESPONSE_IDLING", "[says not to mind them]" },
            { "GREET_GENERAL_FAMILIAR", "[says hello like an old friend]" },
            { "GREET_GENERAL_STRANGER", "[says hello]" },
            { "CHAT_GOOD_WEATHER", "[remarks on the fine weather]" },
            { "RESPONSE_GOOD_WEATHER", "[agrees it's a fine day]" },
            { "CHAT_BAD_WEATHER", "[grumbles about the weather]" },
            { "RESPONSE_BAD_WEATHER", "[answers about the weather]" }
        };

        /// The caption for a line, in the add-on's own words.
        public static string Caption(string line)
        {
            string c;
            if (line != null && Captions.TryGetValue(line, out c))
                return c;
            if (line != null && line.StartsWith("CHAT_RESPONSE_"))
                return "[passes on some news]";
            if (line != null && line.StartsWith("RESPONSE_"))
                return "[reacts]";
            return "[says a few words]";
        }

        /// Every caption, for the tests and the overlap checks.
        public static IEnumerable<string> AllLines()
        {
            return Captions.Values.Concat(new[] { "[passes on some news]", "[reacts]", "[says a few words]" });
        }
    }
}
