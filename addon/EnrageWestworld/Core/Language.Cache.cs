// The language layer's offline half, the rest of it (item 8's leftovers, night run 2026-10-28): the cache key per
// person and situation, so the same person in the same state asks the model once; and the fallback rule, so a thought
// is never late or wrong: an answer that took over two seconds, or fails the check, gives the written line. Nothing
// here loads a model or opens a connection; nothing in the game calls it yet. Plain C#, no game.
using System;
using System.Text;

namespace EnrageWestworld.Core
{
    /// What the game gets back for a request: the line to show, and where it came from.
    public sealed class LanguageAnswer
    {
        public string Line;        // the thought to show
        public string From;        // "model", "cache", or "written" (the fallback)
        public string Why;         // for a fallback: "late (2,300 ms)", "check: a modern word", "no answer"
    }

    public static partial class Language
    {
        public const int LateMs = 2000;   // an answer slower than this is the written line's (the thought must come when the moment does)

        /// The cache key: the same person in the same situation asks once. The situation is what the prompt says of
        /// the moment (the feeling and whom, what shows, the need, the worry, the place, the hour by its part of the
        /// day, the weather, the mood) and not the names in sight or the memories' exact words (those change every
        /// minute and would defeat the cache without changing the thought much).
        public static string CacheKey(LanguageRequest r)
        {
            if (r == null)
                return null;
            var b = new StringBuilder(96);
            b.Append(r.Kind).Append('|').Append(r.Name).Append('|').Append(r.Feeling).Append('|').Append(r.Strength).Append('|').Append(r.About).Append('|').Append(r.Shows);
            b.Append('|').Append(r.Need).Append('|').Append(r.Worry).Append('|').Append(r.Place).Append('|').Append(DayPart(r.Hour)).Append('|').Append(r.Weather).Append('|').Append(r.Mood).Append('|').Append(r.Year);
            foreach (string h in r.Beliefs)
                b.Append('|').Append(h);   // a belief flipped is a new situation
            return b.ToString();
        }

        /// The part of the day the hour falls in, for the key (a thought at six and at seven is the same morning).
        public static string DayPart(int hour)
        {
            return hour < 5 ? "night" : hour < 9 ? "morning" : hour < 12 ? "forenoon" : hour < 14 ? "noon" : hour < 18 ? "afternoon" : hour < 22 ? "evening" : "night";
        }

        /// The fallback rule: the model's answer stands only if it came within LateMs and passes the check; else the
        /// written line, with why. `answer` null: no answer came.
        public static LanguageAnswer Resolve(LanguageRequest r, string answer, long elapsedMs, string writtenLine, bool fromCache = false)
        {
            var a = new LanguageAnswer();
            if (answer == null)
            {
                a.Line = writtenLine;
                a.From = "written";
                a.Why = "no answer";
                return a;
            }
            if (!fromCache && elapsedMs > LateMs)
            {
                a.Line = writtenLine;
                a.From = "written";
                a.Why = "late (" + elapsedMs.ToString("N0") + " ms)";
                return a;
            }
            string why = Check(r, answer);
            if (why != null)
            {
                a.Line = writtenLine;
                a.From = "written";
                a.Why = "check: " + why;
                return a;
            }
            a.Line = answer.Trim();
            a.From = fromCache ? "cache" : "model";
            return a;
        }
    }
}
