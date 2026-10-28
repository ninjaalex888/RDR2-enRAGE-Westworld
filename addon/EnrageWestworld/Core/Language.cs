// The language layer's offline half (the mind design's step 9, daytime run 2026-10-27): what a local model would be
// asked and the check every answer must pass before it's shown. Nothing here loads a model or opens a connection; the
// game doesn't call it yet. The mind decides (the feeling, whom it's about, whether it shows, the place); the model only
// words it, and the written lines stay the fallback for a slow or failed answer. Plain C#, no game.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EnrageWestworld.Core
{
    /// Everything the model is told about one person at one moment; built from the mind's state, never from free text.
    public sealed class LanguageRequest
    {
        public string Kind = "thought";      // thought (diaries and letters later)
        public string Name;                  // their own full name
        public bool Woman;
        public int Age;                      // 0 unknown
        public string Trade;                 // "a farmhand"
        public string Town;                  // Valentine, or null out in the wilds
        public string Place;                 // saloon, church, store... or null (the street)
        public int Hour = 12, Year = 1899;
        public string Weather;               // rain, sun... or null
        public string TraitWords;            // Traits.Words: "outspoken and quick to take offence"
        public string Mood;                  // Mood.Words: "sore and keyed up"
        public string Feeling, Strength;     // anger, strong (or null: no feeling, the need or the worry speaks)
        public string About;                 // whom: "you" (the player), a first name, "a stranger", or null
        public string YouAs;                 // how they think of the player ("the stranger in the black hat"), when About is "you"
        public string Shows;                 // shown, softened, masked, turned (Display)
        public string Need;                  // the most pressing need, in words ("hungry"), or null
        public string Worry;                 // Needs.Words of the first concern, or null
        public List<string> Memories = new List<string>();   // one or two, in plain words ("saw Earl Platt knocked down in the saloon, yesterday")
        public List<string> Near = new List<string>();       // first names of those in sight
        public string AvoidLine;             // the written line the fallback would give (the model must not copy it)

        /// Every name the answer may use: their own, whom it's about, those near, the town, any in the memories.
        public HashSet<string> Names()
        {
            var n = new HashSet<string>(StringComparer.Ordinal);
            Action<string> add = s => { if (!string.IsNullOrEmpty(s)) foreach (Match m in Regex.Matches(s, @"\b\p{Lu}\p{Ll}+(?:'\p{Ll}+)?\b")) n.Add(m.Value); };
            add(Name);
            add(About);
            add(YouAs);
            add(Town);
            add(Worry);
            foreach (string s in Memories) add(s);
            foreach (string s in Near) add(s);
            return n;
        }
    }

    public static class Language
    {
        public const int MaxChars = 110, MaxSentences = 4;

        /// Lex's writing brief for thoughts, as the model is told it.
        public static readonly string Brief =
            "You write one private thought for a person in 1899 in the American West. Short fragments, the way people think, not speak. " +
            "One thing the body notices or the eye falls on. Plain words of the time: no modern words or phrases. Never name anyone you were not given. " +
            "The thought says what is really felt, even when the face hides it. Never mention games, players, characters, stories or being written. " +
            "At most " + MaxSentences + " short sentences and " + MaxChars + " characters. Answer with the thought alone: no quotation marks, no name before it, nothing else.";

        /// The request in words (the prompt's second half; the brief is the first). The same request gives the same text.
        public static string Prompt(LanguageRequest r)
        {
            var b = new StringBuilder();
            b.Append("Who: ").Append(r.Name ?? "someone").Append(", ").Append(r.Woman ? "a woman" : "a man");
            if (r.Age > 0) b.Append(" of ").Append(r.Age);
            if (!string.IsNullOrEmpty(r.Trade)) b.Append(", ").Append(r.Trade);
            b.Append(".\n");
            if (!string.IsNullOrEmpty(r.TraitWords)) b.Append("Nature: ").Append(r.TraitWords).Append(".\n");
            b.Append("Where: ").Append(r.Place ?? "the street").Append(r.Town != null ? " in " + r.Town : " away from any town").Append(", ")
             .Append(HourWords(r.Hour)).Append(", ").Append(r.Year).Append(r.Weather != null ? ", " + r.Weather : "").Append(".\n");
            if (!string.IsNullOrEmpty(r.Mood)) b.Append("Mood: ").Append(r.Mood).Append(".\n");
            if (!string.IsNullOrEmpty(r.Feeling))
            {
                b.Append("Feeling: ").Append(r.Feeling).Append(r.Strength != null ? " (" + r.Strength + ")" : "");
                if (r.About == "you")
                    b.Append(", about ").Append(r.YouAs ?? "the stranger");
                else if (!string.IsNullOrEmpty(r.About))
                    b.Append(", about ").Append(r.About);
                if (!string.IsNullOrEmpty(r.Shows) && r.Shows != "shown")
                    b.Append("; their face ").Append(r.Shows == "masked" ? "hides it" : r.Shows == "softened" ? "shows less of it" : "shows something else");
                b.Append(".\n");
            }
            if (!string.IsNullOrEmpty(r.Need)) b.Append("The body: ").Append(r.Need).Append(".\n");
            if (!string.IsNullOrEmpty(r.Worry)) b.Append("On their mind: ").Append(r.Worry).Append(".\n");
            foreach (string m in r.Memories.Take(2)) b.Append("Remembers: ").Append(m).Append(".\n");
            if (r.Near.Count > 0) b.Append("In sight: ").Append(string.Join(", ", r.Near.Take(4))).Append(".\n");
            b.Append("Write the thought.");
            return b.ToString();
        }

        private static string HourWords(int h)
        {
            return h < 5 ? "the small hours" : h < 9 ? "early morning" : h < 12 ? "morning" : h < 14 ? "midday" : h < 18 ? "afternoon" : h < 21 ? "evening" : "night";
        }

        // common capitalised words a thought may use without being given them (sentence starts are allowed anyway)
        private static readonly HashSet<string> Common = new HashSet<string>(StringComparer.Ordinal)
        {
            "I", "God", "Lord", "Jesus", "Christ", "Mother", "Father", "Ma", "Pa", "Mama", "Papa", "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday",
            "Christmas", "Easter", "Heaven", "Hell", "Bible", "Mister", "Mr", "Mrs", "Miss", "Doc", "Sheriff", "Marshal", "Reverend", "Pastor", "Father's", "Mother's", "God's",
            "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December", "American", "Union", "Rebs", "Yankee", "Yankees"
        };

        // words that break the frame or the period whatever the line
        private static readonly string[] Frame = { "npc", "video game", "computer", "programmed", "simulation", "story mode", "as an ai", "language model", "assistant", "prompt", "a character in", "this story", "storyline", "narrator" };

        /// Whether an answer may be shown; why not otherwise (the first reason). Null: it passes.
        public static string Check(LanguageRequest r, string answer)
        {
            if (string.IsNullOrWhiteSpace(answer))
                return "empty";
            string a = answer.Trim();
            if (a.Length > MaxChars)
                return "too long (" + a.Length + " characters)";
            if (a.IndexOf('\n') >= 0 || a.IndexOf('\r') >= 0)
                return "more than one line";
            if (a.IndexOfAny(new[] { '"', '“', '”', '*', '#', '<', '>', '_', '~', '`', '[', ']' }) >= 0)
                return "quotes or markup";
            if (a.Any(ch => ch > 0x2000 && ch != '’' && ch != '—' && ch != '–'))
                return "a symbol or emoji";
            if (Regex.Matches(a, @"[.!?]+(\s|$)").Count > MaxSentences)
                return "more than " + MaxSentences + " sentences";
            if (Regex.IsMatch(a, @"^\p{Lu}\p{Ll}+( \p{Lu}\p{Ll}+)?\s*:"))
                return "a name before it";
            string low = " " + a.ToLowerInvariant() + " ";
            foreach (string f in Frame)
                if (Regex.IsMatch(low, @"[^a-z]" + Regex.Escape(f) + @"[^a-z]"))
                    return "breaks the frame (" + f + ")";
            string modern = PeriodWords.ModernIn(a);
            if (modern != null)
                return "a modern word (" + modern + ")";
            HashSet<string> names = r != null ? r.Names() : new HashSet<string>();
            foreach (Match m in Regex.Matches(a, @"(?<![.!?]\s)(?<!^)\b\p{Lu}\p{Ll}+(?:'\p{Ll}+)?\b"))
            {
                string w = m.Value;
                if (Common.Contains(w) || names.Contains(w) || names.Contains(w.Replace("'s", "")))
                    continue;
                return "a name it wasn't given (" + w + ")";
            }
            if (r != null && !string.IsNullOrEmpty(r.AvoidLine) && Same(a, r.AvoidLine))
                return "copies the written line";
            return null;
        }

        private static bool Same(string a, string b)
        {
            Func<string, string> norm = s => Regex.Replace(s.ToLowerInvariant(), @"[^a-z ]", "").Trim();
            return norm(a) == norm(b);
        }
    }
}
