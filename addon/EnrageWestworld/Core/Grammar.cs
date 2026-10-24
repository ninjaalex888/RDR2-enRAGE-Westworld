// Turns enRAGE's short third-person phrases into first-person speech:
//   "Lost the farm he built to the bank"                 -> "I lost the farm I built to the bank"
//   "His uncle taught him to shoot with his own rifle"   -> "My uncle taught me to shoot with his own rifle"
//   "Born in a mining camp near Annesburg"               -> "I was born in a mining camp near Annesburg"
//   "Struck by lightning - he still flinches at thunder" -> "I was struck by lightning, I still flinch at thunder"
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace EnrageWestworld.Core
{
    public static class Grammar
    {
        // Verbs that start a passive phrase when a preposition/adverb follows: "Raised on a farm",
        // "Mauled by a bear", "Orphaned young". With an object they're active: "Shot a man", "Nursed fever patients".
        private static readonly HashSet<string> PassiveVerbs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "raised", "orphaned", "wounded", "captured", "born", "abandoned", "adopted", "baptized", "baptised",
            "jailed", "arrested", "beaten", "sent", "taken", "brought", "apprenticed", "indentured", "stabbed",
            "bitten", "robbed", "hanged", "drafted", "conscripted", "sold", "tricked", "cheated", "betrayed",
            "disciplined", "flogged", "whipped", "branded", "expelled", "fired", "sacked", "nursed", "schooled",
            "bred", "chased", "jilted", "widowed", "pardoned", "acquitted", "convicted", "sentenced", "deported",
            "shipped", "tarred", "lashed", "demoted", "promoted", "forced", "spared", "saved", "shot", "hurt",
            "struck", "crippled", "scalped", "burned", "burnt", "trampled", "mauled", "gored", "snakebit", "posted",
            "stationed", "caught", "kicked", "thrown", "knifed", "clubbed", "pistol-whipped", "trapped",
            "buried", "frozen", "left", "made", "named", "known", "held", "laid", "given", "put", "run", "ridden",
            "turned", "passed", "pushed", "dragged", "hunted", "hounded", "driven", "engaged", "christened"
        };

        // Passive even though an article follows: "Made a lieutenant", "Left an orphan at six", "Married off at fifteen".
        private static readonly Regex PassivePairs = new Regex(
            @"^(made (a |an |the )?(captain|sergeant|lieutenant|corporal|foreman|deputy|partner|ward|example|widow|widower|orphan|boss|chief|marshal|sheriff)\b|left (a |an )?(orphan|widow|widower|for dead|with nothing|to die|at the altar)\b|married off\b|given (up|away)\b|put (out|away|to work|in irons)\b)",
            RegexOptions.IgnoreCase);

        // Active even though a PassiveFollow word comes next: "Left home young", "Left town in a hurry".
        private static readonly Regex ActivePairs = new Regex(@"^(left (home|town|school|word|the)\b|sent (word|money|home money)\b)", RegexOptions.IgnoreCase);

        // Words that, right after one of the verbs above, make it passive ("Raised ON", "Orphaned YOUNG").
        private static readonly HashSet<string> PassiveFollow = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "on", "in", "at", "by", "to", "from", "into", "out", "up", "down", "off", "over", "under", "young",
            "east", "west", "north", "south", "away", "back", "and", "as", "for", "with", "after", "before", "near",
            "outside", "inside", "twice", "once", "alone", "dead", "home", "early", "late", "onto", "across",
            "through", "behind", "aside", "upriver", "downriver", "overboard", "loose"
        };

        // Particles of phrasal verbs: "Burned OUT a store" stays active when an object follows.
        private static readonly HashSet<string> Particles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "out", "up", "down", "off", "over", "back", "away"
        };

        // Third-person verbs at the start of a phrase -> first person.
        private static readonly Dictionary<string, string> ThirdPerson = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "is", "am" }, { "was", "was" }, { "has", "have" }, { "does", "do" }, { "goes", "go" }, { "owes", "owe" },
            { "keeps", "keep" }, { "hates", "hate" }, { "misses", "miss" }, { "carries", "carry" }, { "lives", "live" },
            { "drinks", "drink" }, { "gambles", "gamble" }, { "swears", "swear" }, { "prays", "pray" }, { "wants", "want" },
            { "wears", "wear" }, { "works", "work" }, { "writes", "write" }, { "sends", "send" }, { "visits", "visit" },
            { "says", "say" }, { "thinks", "think" }, { "knows", "know" }, { "holds", "hold" }, { "blames", "blame" },
            { "tells", "tell" }, { "sleeps", "sleep" }, { "rides", "ride" }, { "dreams", "dream" }, { "fears", "fear" },
            { "loves", "love" }, { "believes", "believe" }, { "reads", "read" }, { "can't", "can't" }, { "won't", "won't" },
            { "doesn't", "don't" }, { "isn't", "am not" }, { "hasn't", "haven't" }, { "wasn't", "wasn't" },
            { "still", "still" }, { "never", "never" }, { "always", "always" }, { "once", "once" }, { "nearly", "nearly" },
            { "almost", "almost" }, { "barely", "barely" }, { "finally", "finally" }, { "only", "only" },
            { "twice", "twice" }, { "later", "later" }, { "also", "also" }, { "speaks", "speak" }, { "talks", "talk" },
            { "sings", "sing" }, { "plays", "play" }, { "saves", "save" }, { "spends", "spend" }, { "sees", "see" },
            { "looks", "look" }, { "feels", "feel" }, { "teaches", "teach" }, { "watches", "watch" }, { "wishes", "wish" },
            { "buries", "bury" }, { "worries", "worry" }, { "can", "can" }, { "could", "could" }, { "would", "would" },
            { "will", "will" }, { "must", "must" }, { "should", "should" }, { "used", "used" }, { "had", "had" },
            { "did", "did" }, { "made", "made" }
        };

        private static readonly Dictionary<string, string> AfterI = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "is", "am" }, { "has", "have" }, { "does", "do" }, { "doesn't", "don't" }, { "isn't", "am not" },
            { "hasn't", "haven't" }, { "goes", "go" }
        };

        private static readonly HashSet<string> Adverbs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "still", "never", "always", "just", "often", "sometimes", "rarely", "really", "only", "also", "already",
            "nearly", "almost", "barely", "hardly", "ever", "once", "twice", "even", "since", "now"
        };

        private static readonly HashSet<string> Articles = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "a", "an", "the", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "one", "both",
            "some", "every", "no", "most", "all", "this", "that", "these", "those", "his", "her", "their", "its", "my"
        };

        // Nouns that make "A quiet boy, ..." a description of the person: "I was a quiet boy".
        private static readonly HashSet<string> PersonNouns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "boy", "girl", "child", "man", "woman", "youngster", "orphan", "kid", "lad", "lass", "son", "daughter",
            "drifter", "loner", "gambler", "drunk", "widow", "widower", "veteran", "soldier", "sailor", "preacher",
            "foundling", "runaway", "bastard", "cripple", "dandy", "hothead", "coward", "brawler", "thief", "outlaw"
        };

        private static readonly HashSet<string> KinNouns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "son", "daughter", "grandson", "granddaughter", "nephew", "niece", "child", "brother", "sister", "widow", "widower"
        };

        // Words after "her" that show it's an object ("beat her"), not a possessive ("her sister").
        private static readonly HashSet<string> NotNouns = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "and", "or", "but", "to", "for", "from", "with", "in", "on", "at", "by", "of", "off", "out", "up", "down",
            "away", "back", "again", "when", "while", "after", "before", "as", "like", "into", "that", "who", "until",
            "because", "so", "then", "over", "about", "around", "through", "once", "twice", "home", "alone", "dead", "go"
        };

        private static readonly HashSet<string> Determiners = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "a", "an", "the", "their", "his", "its", "my", "your", "our", "this", "that", "these", "those", "some", "any"
        };

        private static readonly HashSet<string> IrregularPast = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "took", "gave", "left", "made", "got", "found", "lost", "won", "ran", "rode", "fought", "saw", "came", "went",
            "fell", "broke", "stole", "shot", "hit", "kept", "held", "sold", "bought", "taught", "caught", "brought",
            "thought", "told", "said", "knew", "grew", "drew", "threw", "swore", "wore", "tore", "bore", "spent", "sent",
            "built", "dug", "hung", "struck", "stood", "began", "drank", "sang", "swam", "wrote", "rose", "drove", "froze",
            "chose", "spoke", "woke", "forgot", "met", "led", "fed", "fled", "bled", "slept", "swept", "felt", "dealt",
            "meant", "lent", "bent", "burnt", "learnt", "let", "put", "set", "cut", "quit", "split", "spread", "beat", "bet",
            "bit", "hid", "slid", "lit", "sat", "paid", "laid", "heard", "had", "did", "was", "were", "became", "forgave",
            "bound", "ground", "wound", "shook", "stuck", "sought", "slew", "lay", "sank", "rang", "sprang", "ate", "saved"
        };

        public static string FirstPerson(string phrase, bool woman)
        {
            string raw = (phrase ?? string.Empty).Replace("  ", " ").Trim().TrimEnd('.', ',', ';').Trim();
            if (raw.Length == 0)
                return raw;
            string[] segments = raw.Split(new[] { " - " }, StringSplitOptions.RemoveEmptyEntries);
            string result = Lead(segments[0].Trim(), woman);
            for (int i = 1; i < segments.Length; i++)
                result += ", " + Continuation(segments[i].Trim(), woman);
            return result;
        }

        private static string Lead(string s, bool woman)
        {
            // "A son born" -> "My son was born"
            Match born = Regex.Match(s, @"^an? (son|daughter|boy|girl) born\b(.*)$", RegexOptions.IgnoreCase);
            if (born.Success)
                return "My " + born.Groups[1].Value.ToLowerInvariant() + " was born" + Pronouns(born.Groups[2].Value, woman, true);

            string[] words = s.Split(' ');
            string w0 = words[0];
            string w0l = w0.ToLowerInvariant();
            string w1 = words.Length > 1 ? Bare(words[1]) : string.Empty;
            string w2 = words.Length > 2 ? Bare(words[2]) : string.Empty;
            string rest = s.Substring(w0.Length);

            // "He ..." / "She ..."
            if (w0l == (woman ? "she" : "he"))
                return "I" + Pronouns(rest, woman, true);

            // "you let him go" -> "You let me go" (the player is the subject; "you" stays "you")
            if (w0l == "you" || w0l == "you've" || w0l == "you'd")
                return UpperFirst(w0l) + Pronouns(rest, woman, false);

            // "His uncle taught him to shoot with his own rifle" -> "My uncle taught me to shoot with his own rifle"
            if (w0l == (woman ? "her" : "his"))
                return "My" + ObjectsOnly(rest, woman);

            // "Son of a Union veteran ..." -> "I'm the son of a Union veteran ..."
            if (KinNouns.Contains(w0l) && w1 == "of")
                return "I'm the " + Pronouns(LowerFirst(s), woman, true);

            // "A quiet boy, raised by his aunt" -> "I was a quiet boy, raised by my aunt"
            if ((w0l == "a" || w0l == "an") && words.Skip(1).Take(3).Any(x => PersonNouns.Contains(Bare(x))))
                return "I was " + Pronouns(LowerFirst(s), woman, true);

            // Someone else is the subject: "The war took his brothers", "Colm saved his life"
            if (Articles.Contains(w0l) || IsNamedSubject(words))
                return UpperFirst(Pronouns(s, woman, false));

            if (!ActivePairs.IsMatch(s) && (PassivePairs.IsMatch(s) || IsPassive(w0l, w1, w2)))
                return "I was " + Pronouns(LowerFirst(s), woman, true);

            string verb;
            if (ThirdPerson.TryGetValue(w0l, out verb))
                return "I " + verb + Pronouns(rest, woman, true);

            if (LooksThirdPersonVerb(w0l))
                return "I " + BaseForm(w0l) + Pronouns(rest, woman, true);

            return "I " + Pronouns(LowerFirst(s), woman, true);
        }

        // Later parts of a phrase, after " - ": "he still carries the marks", "and never told a soul", "nearly died".
        private static string Continuation(string s, bool woman)
        {
            string[] words = s.Split(' ');
            string w0l = words[0].ToLowerInvariant();
            if (w0l == (woman ? "she" : "he") || w0l == (woman ? "she's" : "he's") || w0l == (woman ? "she'd" : "he'd") || w0l == (woman ? "she'll" : "he'll"))
                return Pronouns(s, woman, true);
            if (w0l == "and" || w0l == "but" || w0l == "then" || w0l == "so" || w0l == "yet")
                return Pronouns(s, woman, true);
            if (Articles.Contains(w0l) || IsNamedSubject(words) || (words[0].Length > 0 && char.IsUpper(words[0][0]) && !LooksLikeVerb(w0l)))
                return Pronouns(s, woman, false);
            // a bare verb phrase continues the person's story: "nearly died" -> "and nearly died"
            string verb;
            if (ThirdPerson.TryGetValue(w0l, out verb) && !Adverbs.Contains(w0l))
                return "and " + verb + Pronouns(s.Substring(words[0].Length), woman, true);
            if (Adverbs.Contains(w0l) || IsPastVerb(w0l))
                return "and " + Pronouns(LowerFirst(s), woman, true);
            // anything else is a description: "the best shot in his family", "cowhands, gamblers and traveling salesmen"
            return Pronouns(LowerFirst(s), woman, true);
        }

        private static bool IsPastVerb(string w)
        {
            return (w.Length > 3 && w.EndsWith("ed")) || IrregularPast.Contains(w) || PassiveVerbs.Contains(w);
        }

        /// "won the fight with ...", "took a bullet ...": a phrase that starts with a past-tense verb.
        public static bool StartsWithPastVerb(string phrase)
        {
            string w = Bare((phrase ?? string.Empty).Trim().Split(' ')[0]);
            return w.Length > 1 && (IrregularPast.Contains(w) || (w.Length > 4 && w.EndsWith("ed")));
        }

        // Versions before 0.5.1 shortened "Pvt. Maddox" to "Pvt." in text they saved ("Pvt. saw you ...",
        // "Pvt.'s brother"). A title left without its name becomes a plain description: "a private".
        private static readonly Regex BareTitle = new Regex(@"\b(?:(a|an|the|A|An|The)\s+)?(Pvt|Cpl|Sgt|Lt|Capt|Cpt|Maj|Col|Gen|Dr|Rev|Prof|Mr|Mrs)\.(?='s\b|\s+[a-z])");

        private static readonly Dictionary<string, string> TitleWord = new Dictionary<string, string>
        {
            { "Pvt", "private" }, { "Cpl", "corporal" }, { "Sgt", "sergeant" }, { "Lt", "lieutenant" }, { "Capt", "captain" }, { "Cpt", "captain" },
            { "Maj", "major" }, { "Col", "colonel" }, { "Gen", "general" }, { "Dr", "doctor" }, { "Rev", "preacher" }, { "Prof", "professor" },
            { "Mr", "man" }, { "Mrs", "woman" }
        };

        /// "Maj. saw you kill a man" -> "a major saw you kill a man"; "Pvt.'s brother" -> "a private's brother".
        public static string FixBareTitles(string text)
        {
            if (string.IsNullOrEmpty(text) || text.IndexOf('.') < 0)
                return text;
            return BareTitle.Replace(text, m =>
            {
                string word = TitleWord[m.Groups[2].Value];
                if (!m.Groups[1].Success)
                    return "a " + word;
                string article = m.Groups[1].Value;
                return (article.ToLowerInvariant() == "the" ? article : (char.IsUpper(article[0]) ? "A" : "a")) + " " + word;
            });
        }

        /// True when a claim starts with a title FixBareTitles described ("a major saw ..."), so it already has its subject.
        public static bool StartsWithTitleWord(string text)
        {
            if (string.IsNullOrEmpty(text) || !text.StartsWith("a ", StringComparison.Ordinal))
                return false;
            int sp = text.IndexOf(' ', 2);
            string w = sp > 2 ? text.Substring(2, sp - 2) : text.Substring(2);
            if (w.EndsWith("'s"))
                return false;
            return TitleWord.ContainsValue(w);
        }

        /// "Arrested in Valentine ...", "Made a lieutenant ...": the phrase is passive and needs "was".
        public static bool StartsPassive(string phrase)
        {
            string s = (phrase ?? string.Empty).Trim();
            string[] words = s.Split(' ');
            string w0 = words[0].ToLowerInvariant();
            string w1 = words.Length > 1 ? Bare(words[1]) : string.Empty;
            string w2 = words.Length > 2 ? Bare(words[2]) : string.Empty;
            return !ActivePairs.IsMatch(s) && (PassivePairs.IsMatch(s) || IsPassive(w0, w1, w2));
        }

        private static bool IsPassive(string w0, string w1, string w2)
        {
            if (!PassiveVerbs.Contains(w0))
                return false;
            if (w0 == "caught" && w1.EndsWith("ing"))
                return true;
            if (w1.Length == 0)
                return true;
            if (!PassiveFollow.Contains(w1))
                return false;
            // phrasal verb with an object: "Burned out a store", "Ran off a rustler"
            if (Particles.Contains(w1) && (Articles.Contains(w2) || (w2.Length > 0 && char.IsLower(w2[0]) && !PassiveFollow.Contains(w2) && w2 != "of")))
                return false;
            return true;
        }

        // Converts the person's own pronouns, and makes the verb after "I" (or after "and" in the
        // person's own clause) agree: "I still carries" -> "I still carry", "and has never" -> "and have never".
        private static string Pronouns(string s, bool woman, bool subjectIsPerson)
        {
            string[] t = Tokenize(s);
            int pending = 0; // 1 = just after "I": any 3rd-person verb; 2 = just after "and": known verbs only
            for (int i = 0; i < t.Length; i++)
            {
                string w = t[i];
                if (w.Trim().Length == 0)
                    continue;
                if (!char.IsLetter(w[0]))
                {
                    pending = 0; // punctuation ends the clause
                    continue;
                }
                string lw = w.ToLowerInvariant();
                string rep = null;
                bool becomesI = false;
                if (!woman)
                {
                    if (lw == "himself") rep = "myself";
                    else if (lw == "him") rep = "me";
                    else if (lw == "his") rep = "my";
                    else if (subjectIsPerson && lw == "he") { rep = "I"; becomesI = true; }
                    else if (subjectIsPerson && lw == "he'd") rep = "I'd";
                    else if (subjectIsPerson && lw == "he's") rep = HasOrIs(t, i);
                    else if (subjectIsPerson && lw == "he'll") rep = "I'll";
                }
                else
                {
                    if (lw == "herself") rep = "myself";
                    else if (lw == "her") rep = NextIsNoun(t, i) ? "my" : "me";
                    else if (subjectIsPerson && lw == "she") { rep = "I"; becomesI = true; }
                    else if (subjectIsPerson && lw == "she'd") rep = "I'd";
                    else if (subjectIsPerson && lw == "she's") rep = HasOrIs(t, i);
                    else if (subjectIsPerson && lw == "she'll") rep = "I'll";
                }

                if (rep != null)
                {
                    t[i] = rep.StartsWith("I") ? rep : MatchCase(w, rep);
                    pending = becomesI ? 1 : 0;
                    continue;
                }

                if (pending != 0)
                {
                    if (Adverbs.Contains(lw))
                        continue;
                    string v;
                    if (AfterI.TryGetValue(lw, out v))
                        t[i] = MatchCase(w, v);
                    else if (pending == 1 && LooksThirdPersonVerb(lw))
                        t[i] = MatchCase(w, BaseForm(lw));
                    else if (pending == 2 && ThirdPerson.TryGetValue(lw, out v))
                        t[i] = MatchCase(w, v);
                    pending = 0;
                    continue;
                }

                if (lw == "i")
                    pending = 1;
                else if (lw == "and" && subjectIsPerson)
                    pending = 2;
            }
            return string.Concat(t);
        }

        // "he's owed", "he's scared": is + adjective, not has + participle.
        private static readonly HashSet<string> StateWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "owed", "scared", "tired", "married", "wanted", "worried", "convinced", "ashamed", "wounded", "hurt",
            "broke", "afraid", "sure", "dead", "drunk", "sober", "sick", "used", "supposed", "done", "finished", "engaged"
        };

        private static string HasOrIs(string[] t, int i)
        {
            string next = NextWord(t, i);
            if (StateWords.Contains(next))
                return "I'm";
            if (next == "never" || next == "ever" || next == "been" || next == "got" || next == "had" || next == "seen" ||
                next == "always" || next == "lost" || next == "made" || next == "killed" || next == "shot" || next == "taken" ||
                next.EndsWith("ed") || next.EndsWith("en"))
                return "I've";
            return "I'm";
        }

        /// A phrase where every he/his/him is the person themselves: "the day he finds you" -> "the day I find you".
        public static string Phrase(string text, bool woman)
        {
            return Pronouns(text ?? string.Empty, woman, true);
        }

        // Words a sentence may start with that are safe to lower-case after a lead-in ("Back in '82, my father ...").
        private static readonly HashSet<string> SoftStarts = new HashSet<string>(StringComparer.Ordinal)
        {
            "My", "A", "An", "The", "His", "Her", "Their", "Our", "Some", "Every", "That", "This", "These", "Those",
            "One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight", "Nine", "Ten", "Nothing", "Not", "Being",
            "Just", "Still", "Never", "Even", "Most", "All", "Both", "No", "Bad", "Good"
        };

        /// Lower-cases the first letter only when the first word is a plain sentence starter, never a name.
        public static string SoftLower(string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;
            string first = s.Split(' ', ',', '.')[0];
            return SoftStarts.Contains(first) ? char.ToLowerInvariant(s[0]) + s.Substring(1) : s;
        }

        // After "My father ...": only the person's object pronouns change ("beat him" -> "beat me").
        private static string ObjectsOnly(string s, bool woman)
        {
            string[] t = Tokenize(s);
            for (int i = 0; i < t.Length; i++)
            {
                string lw = t[i].ToLowerInvariant();
                if (!woman && lw == "him")
                    t[i] = "me";
                else if (woman && lw == "her" && !NextIsNoun(t, i))
                    t[i] = "me";
            }
            return string.Concat(t);
        }

        private static bool NextIsNoun(string[] t, int i)
        {
            string n = NextWord(t, i);
            return n.Length > 0 && char.IsLetter(n[0]) && !NotNouns.Contains(n) && !Determiners.Contains(n) && !IrregularPast.Contains(n);
        }

        private static string NextWord(string[] t, int i)
        {
            for (int j = i + 1; j < t.Length; j++)
            {
                if (t[j].Trim().Length == 0)
                    continue;
                return char.IsLetter(t[j][0]) ? t[j].ToLowerInvariant() : string.Empty;
            }
            return string.Empty;
        }

        private static string PrevWord(string[] t, int i)
        {
            for (int j = i - 1; j >= 0; j--)
            {
                if (t[j].Trim().Length == 0)
                    continue;
                return char.IsLetter(t[j][0]) ? t[j].ToLowerInvariant() : string.Empty;
            }
            return string.Empty;
        }

        // A capitalised name followed by a past-tense verb: "Colm saved his life", "Colm O'Driscoll took him in".
        private static bool IsNamedSubject(string[] words)
        {
            if (words.Length < 2)
                return false;
            string w0 = words[0];
            if (w0.Length == 0 || !char.IsUpper(w0[0]))
                return false;
            string w0l = w0.ToLowerInvariant();
            if (LooksLikeVerb(w0l) || ThirdPerson.ContainsKey(w0l))
                return false;
            int v = 1;
            if (words.Length > 2 && words[1].Length > 0 && char.IsUpper(words[1][0]))
                v = 2;
            string verb = Bare(words[v]);
            return verb.EndsWith("ed") || IrregularPast.Contains(verb);
        }

        private static bool LooksLikeVerb(string w)
        {
            return w.EndsWith("ed") || w.EndsWith("ing") || IrregularPast.Contains(w) || PassiveVerbs.Contains(w) ||
                   LooksThirdPersonVerb(w) || ThirdPerson.ContainsKey(w);
        }

        private static bool LooksThirdPersonVerb(string w)
        {
            return w.Length >= 4 && w.EndsWith("s") && !w.EndsWith("ss") && !w.EndsWith("us") && !w.EndsWith("is") &&
                   !w.EndsWith("'s") && w != "always" && w != "towns" && w != "years" && w != "times" && w != "days";
        }

        private static string BaseForm(string v)
        {
            if (v.EndsWith("ies") && v.Length > 4)
                return v.Substring(0, v.Length - 3) + "y";
            if (v.EndsWith("sses") || v.EndsWith("shes") || v.EndsWith("ches") || v.EndsWith("xes") || v.EndsWith("zes") || v.EndsWith("oes"))
                return v.Substring(0, v.Length - 2);
            return v.Substring(0, v.Length - 1);
        }

        // ---- small helpers -----------------------------------------------------------------

        private static string Bare(string w)
        {
            return (w ?? string.Empty).Trim(',', '.', ';', ':', '!', '?', '(', ')').ToLowerInvariant();
        }

        private static string MatchCase(string original, string replacement)
        {
            return original.Length > 0 && char.IsUpper(original[0]) ? UpperFirst(replacement) : replacement;
        }

        // Splits into words and separators so replacements keep spacing and punctuation.
        private static string[] Tokenize(string s)
        {
            return Regex.Split(s, @"([A-Za-z']+)").Where(x => x.Length > 0).ToArray();
        }

        public static string UpperFirst(string s)
        {
            return string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
        }

        public static string LowerFirst(string s)
        {
            if (string.IsNullOrEmpty(s))
                return s;
            string first = s.Split(' ')[0];
            if (first == "I" || first.StartsWith("I'") || (first.Length > 1 && char.IsUpper(first[1])))
                return s;
            return char.ToLowerInvariant(s[0]) + s.Substring(1);
        }

        public static string Sentence(string s)
        {
            s = (s ?? string.Empty).Trim();
            if (s.Length == 0)
                return s;
            s = UpperFirst(s);
            char last = s[s.Length - 1];
            return last == '.' || last == '!' || last == '?' ? s : s + ".";
        }

        public static string Article(string noun)
        {
            if (string.IsNullOrEmpty(noun))
                return noun;
            return ("aeiouAEIOU".IndexOf(noun[0]) >= 0 ? "an " : "a ") + noun;
        }

        public static string NumberWord(int n)
        {
            string[] w = { "no", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten", "eleven", "twelve" };
            return n >= 0 && n < w.Length ? w[n] : n.ToString();
        }
    }
}
