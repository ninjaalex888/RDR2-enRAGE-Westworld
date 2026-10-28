// Words of 1899, checked (daytime run 2026-10-27, item 8): the words and phrases certainly too modern for anyone of the
// time to say or think, and the doubtful ones (old enough, but rare in 1899 speech or in a sense that came later) for Lex
// to judge. Used by the line tools (daytime\harness\linetools) over every pool, and by the language layer's check of a
// model's answer (Language.Check). Matched between letter boundaries (so "okay;" and "--okay--" are caught); a phrase
// that has an honest 1899 use ("deal with it", "screw up your nerve", "sucks his pipe", "the bank's closure") is doubtful,
// not modern; "my bad" only as the phrase (". , !"). O.K. was in American use long before 1899: doubtful.
// Plain C#, no game.
using System;
using System.Text.RegularExpressions;

namespace EnrageWestworld.Core
{
    public static class PeriodWords
    {
        public static readonly string[] Modern =
        {
            "okay", "teenager", "teenagers", "awesome", "stressed out", "stressful", "lifestyle", "freak out", "freaked out", "ripoff", "it sucks", "that sucks",
            "basically", "literally", "gotta", "no problem", "no worries", "boyfriend", "girlfriend", "mom", "mommy", "kiddo", "cool it", "chill out", "you guys",
            "hi there", "yeah right", "big deal.", "big deal!", "my bad.", "my bad,", "my bad!", "for real!", "no way!", "must be nice", "weirdo", "mindset", "vibe",
            "whatever.", "seriously?", "pissed off"
        };

        public static readonly string[] Doubtful =
        {
            "yeah", "guy", "guys", "kid", "kids", "hello", "hi", "bye", "bye-bye", "weekend", "date", "dating", "babe", "boss", "gonna", "wanna", "ok", "o.k.", "fella", "fellas",
            "deal", "folks", "honestly", "stuff", "nice", "fun", "job", "fired", "cop", "cops", "pal", "buddy", "totally", "move on", "nope", "yep", "dunno",
            "kinda", "sorta", "anyways", "hang out", "stressed", "closure", "screwed up", "rip off", "deal with it",
        };

        private static readonly Regex[] ModernRx = Array.ConvertAll(Modern, Rx);
        private static readonly Regex[] DoubtfulRx = Array.ConvertAll(Doubtful, Rx);

        private static Regex Rx(string w)
        {
            return new Regex(@"(?<!\p{L})" + Regex.Escape(w) + (char.IsLetter(w[w.Length - 1]) ? @"(?!\p{L})" : ""), RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
        }

        /// The first certainly-modern word or phrase in the text, or null.
        public static string ModernIn(string line)
        {
            return FirstIn(line, ModernRx, Modern);
        }

        /// The first doubtful word in the text, or null.
        public static string DoubtfulIn(string line)
        {
            return FirstIn(line, DoubtfulRx, Doubtful);
        }

        private static string FirstIn(string line, Regex[] rx, string[] words)
        {
            for (int i = 0; line != null && i < rx.Length; i++)
                if (rx[i].IsMatch(line))
                    return words[i];
            return null;
        }
    }
}
