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
        public string Town;                  // Valentine, or null away from the towns
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
        public List<string> Memories = new List<string>();   // one or two, in plain words (Episodes.Describe)
        public List<string> Near = new List<string>();       // first names of those in sight
        public List<string> Beliefs = new List<string>();    // what they hold, in their own words (Beliefs.Words: "that stranger is a dangerous man"), up to three (after item 14)
        public string AvoidLine;             // the written line the fallback would give (the model must not copy it)
        public List<string> Options = new List<string>();    // Kind "pick": what they might do, in words (Language.Pick.cs); "say": the remarks their voice has (ThoughtVoice.cs)
        public string Thought;               // Kind "say": the thought showing now

        /// Every name the answer may use: their own, whom it's about, those near, the town, any in the memories.
        public HashSet<string> Names()
        {
            var n = new HashSet<string>(StringComparer.Ordinal);
            Action<string> add = s => { if (!string.IsNullOrEmpty(s)) foreach (Match m in Language.Capital.Matches(s)) n.Add(m.Value); };
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

    public static partial class Language
    {
        public const int MaxChars = 110, MaxSentences = 4;

        internal static readonly Regex Capital = new Regex(@"\b\p{Lu}\p{Ll}+(?:'\p{Ll}+)?\b", RegexOptions.CultureInvariant);
        private static readonly Regex Sentences = new Regex(@"[.!?]+(\s|$)", RegexOptions.CultureInvariant);
        private static readonly Regex Speaker = new Regex(@"^\p{Lu}\p{Ll}+( \p{Lu}\p{Ll}+)?\s*:", RegexOptions.CultureInvariant);
        private static readonly Regex Titles = new Regex(@"\b(Mr|Mrs|Dr|St|Mt)\.", RegexOptions.CultureInvariant);
        private static readonly Regex Starts = new Regex(@"(^|[.!?]\s+)(\p{Lu}\p{Ll}+(?:'\p{Ll}+)?)", RegexOptions.CultureInvariant);
        private static readonly Regex Inner = new Regex(@"(?<![.!?]\s)(?<!^)\b\p{Lu}\p{Ll}+(?:'\p{Ll}+)?\b", RegexOptions.CultureInvariant);
        private static readonly Regex Words = new Regex(@"\p{L}+", RegexOptions.CultureInvariant);

        /// Three thoughts of the right shape, shown to the model (the 2026-10-29 measurement: told a character limit alone, a
        /// local model ran long 463 times in 500 and leaned florid; a word cap it can count and examples it can copy the shape
        /// of are what it follows). An answer too like one of them is held (Check: "copies an example").
        public static readonly string[] Examples =
        {
            "Rain on the hat brim. Hollis talking big again. Let him.",
            "Note's due Friday. Hands won't keep still.",
            "Boots wet through. The stove, and nobody talking.",
        };

        public const int MaxWords = 20;

        /// Lex's writing brief for thoughts, as the model is told it, for the year and place of the request.
        public static string Brief(LanguageRequest r)
        {
            int year = r != null && r.Year > 0 ? r.Year : 1899;
            return "You write one private thought for a person living in " + year + ", in the American West and South. Short fragments, the way people think, not speak: " +
                   "two or three clipped sentences, " + MaxWords + " words at most, " + MaxChars + " characters at most. One thing the body notices or the eye falls on. " +
                   "Plain words of the time: no modern words or phrases, no poetry, no similes. Never name anyone you were not given. " +
                   "The thought says what is really felt, even when the face hides it. Never mention video games, being in a story, or being written. " +
                   "The shape, not the words, of these: " + Examples[0] + " | " + Examples[1] + " | " + Examples[2] + " " +
                   "Answer with the thought alone: no quotation marks, no name before it, nothing else.";
        }

        /// The request in words (the prompt's second half; the brief is the first). The same request gives the same text.
        public static string Prompt(LanguageRequest r)
        {
            return Describe(r) + "Write the thought.";
        }

        /// Who, where, how they feel and what's on their mind, a line each: the request without its ask (a thought's, a pick's).
        public static string Describe(LanguageRequest r)
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
            if (r.Beliefs.Count > 0) b.Append("Holds: ").Append(string.Join("; ", r.Beliefs.Take(3))).Append(".\n");
            if (r.Near.Count > 0) b.Append("In sight: ").Append(string.Join(", ", r.Near.Take(4))).Append(".\n");
            return b.ToString();
        }

        private static string HourWords(int h)
        {
            return h < 5 ? "the small hours" : h < 9 ? "early morning" : h < 12 ? "morning" : h < 14 ? "midday" : h < 18 ? "afternoon" : h < 21 ? "evening" : "night";
        }

        // capitalised words a thought may use without being given them: faith, family, days and months, titles, the
        // places of the game's map and the country around it, the peoples a person of 1899 would name
        private static readonly HashSet<string> Common = new HashSet<string>(StringComparer.Ordinal)
        {
            "I", "God", "Lord", "Jesus", "Christ", "Almighty", "Providence", "Sabbath", "Devil", "Satan", "Saviour", "Savior", "Scripture", "Heaven", "Hell", "Bible", "Gospel", "Amen",
            "Mother", "Father", "Ma", "Pa", "Mama", "Papa", "Grandma", "Grandpa",
            "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday", "Christmas", "Easter",
            "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December",
            "Mister", "Mr", "Mrs", "Miss", "Ma'am", "Sir", "Doc", "Doctor", "Sheriff", "Marshal", "Deputy", "Reverend", "Pastor", "Preacher", "Judge", "Colonel", "Captain", "Major", "General", "Sergeant",
            "Pinkerton", "Pinkertons", "Union", "Confederate", "Confederacy", "Rebs", "Yankee", "Yankees",
            "American", "Americans", "Irish", "Italian", "Italians", "Sicilian", "Sicilians", "German", "Germans", "French", "Creole", "Mexican", "Mexicans", "Chinese", "English", "Scotch", "Swede", "Swedes", "Dutch", "Jew", "Jews", "Indian", "Indians",
            "Valentine", "Rhodes", "Strawberry", "Blackwater", "Armadillo", "Tumbleweed", "Annesburg", "Emerald", "Van", "Horn", "Saint", "Denis", "Lagras", "Colter", "Thieves", "Landing",
            "Lemoyne", "Hanover", "Elizabeth", "Austin", "Ambarino", "Heartlands", "Bayou", "Nwa", "Texas", "Mexico", "Louisiana", "Mississippi", "California", "Chicago", "New", "York", "Orleans", "West", "East", "North", "South", "Lannahechee", "Dakota", "Kamassa", "Grizzlies"
        };

        // words that break the frame whatever the line (a plural counts too)
        private static readonly string[] Frame = { "npc", "video game", "computer", "programmed", "simulation", "story mode", "as an ai", "language model", "characters in a", "in-game", "roleplay", "role-play", "narrator", "storyline" };
        private static readonly Regex[] FrameRx = Array.ConvertAll(Frame, f => new Regex(@"(?<!\p{L})" + Regex.Escape(f) + @"s?(?!\p{L})", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant));

        /// Whether an answer may be shown; why not otherwise (the first reason). Null: it passes.
        public static string Check(LanguageRequest r, string answer)
        {
            if (string.IsNullOrWhiteSpace(answer))
                return "empty";
            string a = answer.Trim().Replace("…", "...");
            if (a.Length > MaxChars)
                return "too long (" + a.Length + " characters)";
            if (a.IndexOf('\n') >= 0 || a.IndexOf('\r') >= 0)
                return "more than one line";
            if (a.IndexOfAny(new[] { '"', '“', '”', '*', '#', '<', '>', '_', '~', '`', '[', ']' }) >= 0)
                return "quotes or markup";
            if (a.Any(ch => ch > 0x2000 && ch != '’' && ch != '—' && ch != '–'))
                return "a symbol or emoji";
            string plain = Titles.Replace(a, "$1");   // "Mr. Hale" is one sentence, not two
            if (Sentences.Matches(plain).Count > MaxSentences)
                return "more than " + MaxSentences + " sentences";
            if (Speaker.IsMatch(a))
                return "a name before it";
            for (int i = 0; i < FrameRx.Length; i++)
                if (FrameRx[i].IsMatch(a))
                    return "breaks the frame (" + Frame[i] + ")";
            string modern = PeriodWords.ModernIn(a);
            if (modern != null)
                return "a modern word (" + modern + ")";
            HashSet<string> names = r != null ? r.Names() : new HashSet<string>();
            Func<string, bool> allowed = w => Common.Contains(w) || names.Contains(w) || w.EndsWith("'s", StringComparison.Ordinal) && (names.Contains(w.Substring(0, w.Length - 2)) || Common.Contains(w.Substring(0, w.Length - 2)));
            foreach (Match m in Inner.Matches(plain))
                if (!allowed(m.Value))
                    return "a name it wasn't given (" + m.Value + ")";
            // a sentence may start with a name too: one of the names the add-on gives people, not given here
            foreach (Match m in Starts.Matches(plain))
            {
                string w = m.Groups[2].Value, bare = w.EndsWith("'s", StringComparison.Ordinal) ? w.Substring(0, w.Length - 2) : w;
                if (!allowed(w) && (Residents.IsName(bare) || Scenes.IsName(bare)))
                    return "a name it wasn't given (" + w + ")";
            }
            if (r != null && !string.IsNullOrEmpty(r.AvoidLine) && Alike(a, r.AvoidLine) >= 0.8)
                return "copies the written line";
            foreach (string e in Examples)
                if (Alike(a, e) >= 0.8)
                    return "copies an example";
            if (Words.Matches(a).Count > MaxWords + 4)
                return "too many words (" + Words.Matches(a).Count + ")";
            return null;
        }

        /// How alike two lines are by their words (the line tools' measure: the share of words in common).
        public static double Alike(string a, string b)
        {
            var x = new HashSet<string>(Words.Matches(a.ToLowerInvariant()).Cast<Match>().Select(m => m.Value));
            var y = new HashSet<string>(Words.Matches(b.ToLowerInvariant()).Cast<Match>().Select(m => m.Value));
            if (x.Count == 0 || y.Count == 0)
                return 0;
            int both = x.Count(y.Contains);
            return (double)both / Math.Min(x.Count, y.Count);
        }
    }
}
