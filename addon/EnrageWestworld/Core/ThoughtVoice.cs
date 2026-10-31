// Thoughts said aloud in their own recorded voice (Lex, 2026-10-30: "NPCs create sentences on their own based on their
// inner thoughts, using their own voicelines"; option 1, before any made voice). The game can't say new words, so the
// model is shown the thought and the remarks that person's voice really has, each by its words where the local word list
// has them, else by what it's for, and picks the one that fits, or none: then they keep it to themselves. The meanings
// are ours, one short phrase a line. Plain C#, no game.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace EnrageWestworld.Core
{
    public sealed class AloudOption
    {
        public string Line;     // the game's line name
        public int Take;        // the recording (1..N), 0 for the game's choice
        public string Text;     // what the model is shown: the words, or what the line is for
        public string Words;    // the words, when known
    }

    public static class ThoughtVoice
    {
        public const int MostOptions = 10;

        /// What each remark is for, in a few plain words: the lines a person might say to nobody in particular.
        public static readonly Dictionary<string, string> Meanings = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            { "GENERIC_CURSE_MED", "a curse under the breath" }, { "GENERIC_CURSE_HIGH", "a loud curse" },
            { "GENERIC_ANGRY_REACTION", "an angry outburst" }, { "WHATS_YOUR_PROBLEM", "asking what someone's problem is" },
            { "DISMISSIVE_REACT", "a dismissive remark" }, { "GENERIC_MOCK", "mockery" }, { "TEASE_RESPONSE", "teasing" },
            { "GENERIC_SHOCKED_MED", "taken aback" }, { "GENERIC_SHOCKED_HIGH", "badly shocked" }, { "GENERIC_SHOCKED_DISBELIEF", "disbelief" },
            { "GENERIC_FRIGHTENED_MED", "frightened" }, { "WHATS_GOING_ON", "asking what's going on" }, { "NO_IDEA", "at a loss" },
            { "GENERIC_THANKS", "thankful" }, { "RELIEVED_REACT", "relieved" }, { "GENERIC_CHEER", "a cheer" }, { "WON_DISPUTE", "pleased to have won" },
            { "SHAME_ON_YOU", "shame on someone" }, { "SICK_BASTARD", "disgusted at someone" },
            { "GOING_BADLY", "things are going badly" }, { "GOING_WELL", "things are going well" },
            { "CHAT_BAD_WEATHER", "grumbling about the weather" }, { "CHAT_GOOD_WEATHER", "pleased with the weather" }, { "COMMENT_WORKING_HARD", "about hard work" },
            { "GENERIC_SHOCKING_EVENT_COMMENT", "remarking on something shocking" }, { "GENERIC_EXCITING_EVENT_COMMENT", "remarking on something exciting" },
            { "STORM_OFF", "fed up and leaving" }, { "IM_OUTTA_HERE", "done with this place" }, { "DONT_WANT_TROUBLE", "wanting no trouble" },
            { "SORRY_TO_HEAR_THAT", "sorry for someone" }, { "CUT_THAT_OUT", "telling someone to quit it" },
            { "DRUNK", "drunken talk" }, { "CONVO_TAIL_OFF", "a sentence that trails away" },
            { "VOCAL_FX_DISAPPROVE", "a disapproving sound, no words" }, { "VOCAL_FX_RESPOND", "a thoughtful hum, no words" }, { "VOCAL_FX_SURPRISED", "a surprised sound, no words" },
        };

        /// The remarks this voice has, best first: those whose words are known (a take of each, by the seed), then the rest by
        /// what they're for; at most MostOptions, varied by the seed.
        public static List<AloudOption> Options(Func<string, int> takes, Func<string, int, string> words, string seed)
        {
            var worded = new List<AloudOption>();
            var plain = new List<AloudOption>();
            foreach (KeyValuePair<string, string> kv in Meanings)
            {
                int n = takes(kv.Key);
                if (n <= 0)
                    continue;
                var known = new List<int>();
                for (int t = 1; t <= n; t++)
                    if (words(kv.Key, t) != null)
                        known.Add(t);
                if (known.Count > 0)
                {
                    int take = known[Voice.Hash(seed + "|" + kv.Key) % known.Count];
                    string w = words(kv.Key, take);
                    worded.Add(new AloudOption { Line = kv.Key, Take = take, Words = w, Text = "says \"" + w + "\"" });
                }
                else
                    plain.Add(new AloudOption { Line = kv.Key, Take = 0, Text = kv.Value });
            }
            Comparison<AloudOption> bySeed = (a, b) => (Voice.Hash(seed + a.Line) % 997).CompareTo(Voice.Hash(seed + b.Line) % 997);
            worded.Sort(bySeed);
            plain.Sort(bySeed);
            var o = new List<AloudOption>(MostOptions);
            foreach (AloudOption a in worded)
                if (o.Count < MostOptions)
                    o.Add(a);
            foreach (AloudOption a in plain)
                if (o.Count < MostOptions)
                    o.Add(a);
            return o;
        }

        public static string Brief(LanguageRequest r)
        {
            int year = r != null && r.Year > 0 ? r.Year : 1899;
            return "You decide whether a person living in " + year + ", in the American West and South, says something aloud about what they are thinking, " +
                   "and which of their own recorded remarks fits it. They can say only one of the remarks listed. Choose one only if it truly fits the thought " +
                   "and the person, so that someone hearing it would catch the feeling behind it. Private memories, longing, grief and plans stay unspoken: " +
                   "then the answer is 0. Most thoughts stay unspoken. Answer with the number alone.";
        }

        public static string Prompt(LanguageRequest r)
        {
            var b = new StringBuilder(Language.Describe(r));
            b.Append("Their thought just now: ").Append(r.Thought ?? "").Append('\n');
            b.Append("What they might say aloud:\n0. nothing: they keep it to themselves\n");
            for (int i = 0; i < r.Options.Count; i++)
                b.Append(i + 1).Append(". ").Append(r.Options[i]).Append('\n');
            b.Append("Which do they say? The number alone.");
            return b.ToString();
        }

        public static string Body(string model, LanguageRequest r)
        {
            var b = new StringBuilder(1536);
            b.Append("{\"model\":").Append(Language.JsonString(model)).Append(",\"system\":").Append(Language.JsonString(Brief(r))).Append(",\"prompt\":").Append(Language.JsonString(Prompt(r)));
            b.Append(",\"stream\":false,\"keep_alive\":\"30m\",\"options\":{\"num_ctx\":1536,\"temperature\":").Append(0.4.ToString("0.00", CultureInfo.InvariantCulture))
             .Append(",\"num_predict\":4,\"stop\":[\"\\n\"]}}");
            return b.ToString();
        }

        private static readonly Regex FirstNumber = new Regex(@"\d+", RegexOptions.CultureInvariant);

        /// The remark the answer names (0-based); -2 when it says none (they keep it in); -1 when it can't be read.
        public static int Parse(string answer, int count)
        {
            if (string.IsNullOrWhiteSpace(answer))
                return -1;
            string a = answer.Trim();
            Match m = FirstNumber.Match(a.Length > 12 ? a.Substring(0, 12) : a);
            int n;
            if (!m.Success || !int.TryParse(m.Value, NumberStyles.None, CultureInfo.InvariantCulture, out n))
                return -1;
            if (n == 0)
                return -2;
            return n >= 1 && n <= count ? n - 1 : -1;
        }
    }
}
