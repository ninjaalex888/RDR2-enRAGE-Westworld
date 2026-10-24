// What people say, think and write. Everything is built from enRAGE's own facts about the person:
// the past, the current mood, goals, and memories of what the player did.
//
// Two registers:
//   Spoken  - said to the player's face. The player is "you".
//   Private - thoughts and diaries. The player is "him": a name if they know it, else what he did to them
//             ("the man who let me go") or how he looks ("that stranger").
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace EnrageWestworld.Core
{
    public static class Voice
    {
        /// The in-game year, set by the game layer from enRAGE's clock.
        public static int NowYear = 1899;

        // ---- choosing ---------------------------------------------------------------------------

        /// Stable across runs, so the same person says the same kind of thing about the same fact.
        public static int Hash(string s)
        {
            unchecked
            {
                uint h = 2166136261;
                foreach (char c in s ?? string.Empty)
                {
                    h ^= c;
                    h *= 16777619;
                }
                return (int)(h & 0x7fffffff);
            }
        }

        public static string Pick(string seed, params string[] options)
        {
            return options.Length == 0 ? string.Empty : options[Hash(seed) % options.Length];
        }

        public static bool Chance(string seed, int percent)
        {
            return Hash(seed + "#") % 100 < percent;
        }

        // ---- the past ------------------------------------------------------------------------------

        private static readonly Regex TimeCue = new Regex(
            @"\b(young|boy|girl|child|small|kid|baby|years?|winters?|since|once|before I was|as a (boy|girl|young man|young woman)|at (six|seven|eight|nine|ten|eleven|twelve|thirteen|fourteen|fifteen|sixteen|seventeen|eighteen|nineteen|twenty|\d+))\b",
            RegexOptions.IgnoreCase);

        /// "Back in '82, I lost my savings at faro in Van Horn."
        public static string PastLine(Person p, PastItem e, string seed, HashSet<string> leads = null)
        {
            string s = Grammar.FirstPerson(e.Text, p.Woman);
            if (string.IsNullOrEmpty(s))
                return null;
            string when = WhenPhrase(p, e, s, seed);
            if (when != null && leads != null)
            {
                if (leads.Contains(when))
                    when = when.StartsWith("When I was a") ? (p.Woman ? "As a girl" : "As a boy") : "Later on";
                if (leads.Contains(when))
                    when = null;
                if (when != null)
                    leads.Add(when);
            }
            string line = when == null ? Grammar.Sentence(s) : Grammar.Sentence(when + ", " + Grammar.SoftLower(s));
            string tail = PastTail(p, e, seed);
            return tail == null ? line : line + " " + tail;
        }

        private static string WhenPhrase(Person p, PastItem e, string s, string seed)
        {
            if (e.Year <= 0 || e.Kind == "born" || TimeCue.IsMatch(s) || IsState(e.Text, s))
                return null;
            int age = p.BornYear > 0 ? e.Year - p.BornYear : -1;
            int ago = NowYear - e.Year;
            if (age >= 0 && age <= 12)
                return p.Woman ? "When I was a girl" : "When I was a boy";
            if (age > 12 && age < 18)
                return Pick(seed + "w", "When I was " + Number(age), "At " + Number(age));
            if (ago < 0)
                return null;
            if (ago == 0)
                return Pick(seed + "w", "This year", "Not long ago");
            if (ago == 1)
                return "Last year";
            if (ago <= 12)
                return Grammar.UpperFirst(Pick(seed + "w", Number(ago) + " years back", Number(ago) + " years ago"));
            return "Back in '" + (e.Year % 100).ToString("00");
        }

        /// "Keeps to the old ways", "His face is on a wanted poster", "Son of a Union veteran": not dated events.
        private static bool IsState(string original, string converted)
        {
            string w = (original ?? string.Empty).Trim().Split(' ')[0].ToLowerInvariant();
            if (w.Length > 3 && w.EndsWith("s") && !w.EndsWith("ss") && w != "was")
                return true;
            if (w == "can" || w == "hasn't" || w == "isn't" || w == "doesn't" || w == "won't" || w == "can't")
                return true;
            return converted.StartsWith("I'm ") || converted.StartsWith("I am ") || converted.StartsWith("I was a ") ||
                   converted.StartsWith("I was an ") || converted.Contains(" is ") || converted.Contains(" am ");
        }

        private static string PastTail(Person p, PastItem e, string seed)
        {
            if (!Chance(seed + "tail", 35))
                return null;
            switch (e.Kind)
            {
                case "crime":
                    if (p.Has("braggart"))
                        return Pick(seed, "Ask anybody.", "Wasn't the last time, neither.");
                    if (p.Has("honest") || p.Facet("Honesty") >= 65)
                        return Pick(seed, "Ain't proud of it.", "I've made my peace with it. Mostly.");
                    return Pick(seed, "Don't go spreading that around.", "That stays between us.");
                case "loss":
                    return Pick(seed, "Never really got over it.", "Some days I still think on it.", "Don't care to say more than that.");
                case "war":
                    return Pick(seed, "Don't care to talk about the rest.", "Saw things there I won't repeat.");
                case "faith":
                    if (p.Devotion == "Devout")
                        return "The Lord's been good to me since.";
                    if (p.Devotion == "Godless")
                        return "Didn't stick.";
                    return null;
                case "youth":
                    return Pick(seed, "Long time ago now.", "Seems like another life.");
                default:
                    return null;
            }
        }

        /// A secret told only to someone they trust: framed as a confidence.
        public static string Confide(Person p, PastItem e, string seed)
        {
            string s = Grammar.Sentence(Grammar.FirstPerson(e.Text, p.Woman));
            string lead = Pick(seed + "c", "I'll tell you something I don't tell many.", "Never told a soul this.",
                "Since you've been square with me...", "Between you and me:");
            return lead + " " + s;
        }

        /// A liar's cover for a past they don't want known.
        public static string Cover(Person p, PastItem e, string seed)
        {
            if (e.Kind == "crime")
                return Pick(seed, "Me? Never been on the wrong side of the law. Not once.",
                    "I've kept my nose clean my whole life. Ask anyone.",
                    "Never so much as stole a chicken.");
            if (e.Kind == "gang")
                return Pick(seed, "I don't ride with nobody. Never have.", "Gangs? Not me. I keep to myself.");
            return Pick(seed, "Not much to tell, really. Quiet life.", "Nothing worth telling.");
        }

        /// Later, the truth behind the cover.
        public static string Unmask(Person p, PastItem e, string seed)
        {
            string s = Grammar.Sentence(Grammar.FirstPerson(e.Text, p.Woman));
            return Pick(seed + "u", "What I told you before... it wasn't true.", "I lied to you, before.",
                "I wasn't straight with you when we met.") + " " + s;
        }

        // ---- who they are ---------------------------------------------------------------------------

        public static List<string> Intro(Person p, string seed)
        {
            List<string> lines = new List<string>();
            string nick = Nickname(p);
            string first;
            if (p.Surly)
                first = Pick(seed + "i", p.Name + ". That's all you need to know.", "Name's " + p.Name + ". Why do you ask?",
                    p.Name + ". Now what do you want?");
            else if (p.Friendly)
                first = Pick(seed + "i", "Name's " + p.Name + ". Pleased to meet you.", p.Name + ", at your service.",
                    "Howdy. " + p.Name + ".", "Well, hello. I'm " + p.Name + ".");
            else
                first = Pick(seed + "i", "Name's " + p.Name + ".", p.Name + ".", "They call me " + p.Name + ".");

            if (nick != null)
            {
                if (nick.StartsWith("of "))
                    first = first.Replace(p.Name, p.Name + ", " + nick);
                else if (p.Personality == "Proud" || p.Has("braggart"))
                    first += " Some call me " + nick + ". Maybe you've heard.";
                else if (!p.Surly)
                    first += " Folks call me " + nick + ".";
            }
            lines.Add(first);

            string work = WorkLine(p, seed);
            if (work != null)
                lines.Add(work);
            return lines;
        }

        private static string Nickname(Person p)
        {
            if (string.IsNullOrEmpty(p.Title))
                return null;
            string t = p.Title.Trim().Trim('"', '\'');
            return t.Length == 0 ? null : t;
        }

        private static readonly Dictionary<string, string[]> Work = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "Blacksmith", new[] { "I keep a forge{home}.", "I'm a smith{home}." } },
            { "Seamstress", new[] { "I take in sewing{home}.", "I sew for a living{home}." } },
            { "Schoolteacher", new[] { "I teach school{home}.", "I keep the schoolhouse{home}." } },
            { "Miner", new[] { "I work the mine{home}.", "I dig coal. Not much more to it." } },
            { "Lady", new[] { "I keep a house in Saint Denis.", "My family has interests in Lemoyne." } },
            { "Homesteader", new[] { "I'm proving up a homestead{home}.", "I've got a claim{home}." } },
            { "Gambler", new[] { "Cards, mostly. Faro when the table's honest.", "I play cards. For money." } },
            { "Farm wife", new[] { "My husband and I keep a farm{home}.", "I keep house on a farm{home}." } },
            { "Doctor", new[] { "I'm a doctor. If you're bleeding, I'm the one to see.", "I practise medicine{home}." } },
            { "Vagrant", new[] { "I don't do much of anything. World don't want much from me.", "I get by." } },
            { "Traveller", new[] { "Just passing through.", "I'm on my way someplace else." } },
            { "Trapper", new[] { "I trap furs.", "I run traplines up in the hills." } },
            { "Townsman", new[] { "I keep a store{home}.", "I'm in trade{home}." } },
            { "Tailor", new[] { "I'm a tailor{home}.", "I cut suits{home}." } },
            { "Showgirl", new[] { "I'm on the stage.", "I sing and dance for a living." } },
            { "Shopkeeper", new[] { "I keep a shop{home}.", "I run a store{home}." } },
            { "Saloon girl", new[] { "I work at the saloon.", "I keep the gentlemen company at the saloon." } },
            { "Rancher", new[] { "I run cattle{home}.", "I've got a spread{home}." } },
            { "Railroad man", new[] { "I work the railroad.", "I lay track for the railroad." } },
            { "Nurse", new[] { "I'm a nurse.", "I nurse the sick{home}." } },
            { "Nun", new[] { "I'm a sister of the church.", "I serve the Lord, and the poor." } },
            { "Market seller", new[] { "I sell at the market{home}.", "I keep a stall at the market." } },
            { "Laundress", new[] { "I take in washing{home}.", "I do laundry. Other folks' dirt." } },
            { "Hunter", new[] { "I hunt.", "I hunt and sell what I kill." } },
            { "Housemaid", new[] { "I'm in service.", "I keep house for a family{home}." } },
            { "Gentleman", new[] { "I have... interests.", "I'm a man of means. Mostly." } },
            { "Farmer", new[] { "I farm{home}.", "I work the land{home}." } },
            { "Drifter", new[] { "I drift.", "Here and there. Never long in one place." } },
            { "Cook", new[] { "I cook{home}.", "I cook. Best biscuits in the territory." } },
            { "Camp cook", new[] { "I cook for the camp.", "I keep the boys fed." } },
            { "Telegraph operator", new[] { "I work the telegraph{home}.", "I send the wires{home}. Hear everything first." } },
            { "Preacher's wife", new[] { "My husband preaches the Word.", "I follow my husband's ministry." } },
            { "Preacher", new[] { "I preach the Word.", "I'm a man of God." } },
            { "Milliner", new[] { "I make hats{home}.", "I trim hats for the ladies{home}." } },
            { "Lumberjack", new[] { "I fell timber.", "I work the logging camps." } },
            { "Labourer", new[] { "I work where there's work.", "I dig, I haul, I lift." } },
            { "Fisherman", new[] { "I fish.", "I fish the rivers." } },
            { "Dockworker", new[] { "I work the docks.", "I haul crates on the docks." } },
            { "Cowhand", new[] { "I'm a cowhand.", "I work cattle." } },
            { "Boarding", new[] { "I keep a boarding house{home}.", "I let rooms{home}." } }
        };

        private static string WorkLine(Person p, string seed)
        {
            string home = string.IsNullOrEmpty(p.Home) ? string.Empty : " in " + p.Home;
            if (p.IsSoldier)
                return Pick(seed + "k", "I serve at Fort Wallace.", "Army. Fort Wallace.");
            string[] options;
            if (p.IsOutlaw)
            {
                if (p.Personality == "Proud" || p.Has("braggart") || p.Personality == "Hothead")
                    return "I ride with " + Or(p.GangName, "some fellas") + ". Remember that.";
                // an honest trade is what an outlaw tells a stranger
                if (!string.IsNullOrEmpty(p.Trade) && Work.TryGetValue(p.Trade, out options))
                    return Pick(seed + "k", options).Replace("{home}", string.Empty);
                return Pick(seed + "k", "I get by.", "A little of this, a little of that.", "This and that. Don't ask.");
            }
            if (!string.IsNullOrEmpty(p.Trade) && Work.TryGetValue(p.Trade, out options))
                return Pick(seed + "k", options).Replace("{home}", home);
            return null;
        }

        public static string FamilyLine(Person p, string seed)
        {
            string spouse = string.IsNullOrEmpty(p.SpouseName) || p.SpouseName.StartsWith("a ") ? null : p.SpouseName;
            string partner = p.Woman ? "husband" : "wife";
            string kids = p.Children <= 0 ? null : p.Children == 1 ? "One little one." : Grammar.UpperFirst(Number(p.Children)) + " children.";
            string where = string.IsNullOrEmpty(p.Home) ? string.Empty : " back in " + p.Home;
            switch (p.Family)
            {
                case "married":
                    string m = spouse != null
                        ? Pick(seed + "f", "Got a " + partner + ", " + spouse + "," + where + ".", "My " + partner + ", " + spouse + ", keeps me honest.", spouse + "'s my " + partner + ". Best thing I ever did.")
                        : "Got a " + partner + where + ".";
                    m = m.Replace(",.", ".").Replace(", ,", ",");
                    if (kids != null)
                        m += " " + kids;
                    if (p.IsOutlaw && Chance(seed + "fo", 50))
                        m += " " + Pick(seed + "fx", "They think I'm in honest work.", "They don't know what I do out here.", "I send what I can.");
                    return m;
                case "widower":
                    return (spouse != null ? "Lost my " + partner + ", " + spouse + "." : "Lost my " + partner + ".") +
                           (kids != null ? " " + kids + (p.Children == 1 ? " I do the best I can by the little one." : " I do the best I can by them.") : " Ain't been the same since.");
                default:
                    if (p.Personality == "Drifter" || p.Trade == "Drifter")
                        return "Never married. Never stayed anywhere long enough.";
                    if (p.Sociability == "Introvert")
                        return Pick(seed + "f", "Never married. Suits me fine.", "Just me. Always has been.");
                    return Pick(seed + "f", "Never married. Came close, once.", "No wife, no kids. Not yet, anyway.".Replace("wife", p.Woman ? "husband" : "wife"));
            }
        }

        // ---- how they are today ----------------------------------------------------------------------

        private static readonly Dictionary<string, string[]> MoodFrames = new Dictionary<string, string[]>
        {
            { "Sick", new[] { "{X}. Feel like death warmed over.", "Can't shake it. {X}.", "{X}. Don't stand too close.", "{X}. Couldn't keep supper down.", "Still poorly. {X}." } },
            { "Hurting", new[] { "{X}. Hurts like the devil.", "{X}. I'll live.", "{X}. Hurts to breathe, some mornings.", "{X}. Whiskey helps. A little." } },
            { "Shaken", new[] { "Can't stop thinking about {x}.", "{X}... I can't shake it.", "{X}. It's eating at me.", "Woke up in a sweat. {X} again.", "{X}. My hands still shake when I think on it." } },
            { "Humiliated", new[] { "{X}. Don't laugh.", "Folks won't let me forget {x}.", "{X}. Not my finest hour.", "Somebody brought up {x} again. I left.", "{X}. I'd rather be shot." } },
            { "Proud", new[] { "{X}. I'm proud of that.", "Ask me about {x} sometime.", "Been a good week. {X}.", "{X}. Let them talk about that.", "Still grinning about {x}." } },
            { "Flush", new[] { "{X}. Drinks are on me. Maybe.", "{X}. Money in my pocket for once.", "{X}. Been a good week.", "{X}. Bought a new hat on the strength of it.", "Paid off a debt, thanks to {x}." } },
            { "Bitter", new[] { "{X}. Still sticks in my craw.", "Don't get me started on {x}.", "{X}. I ain't forgot.", "{X}. Somebody's going to pay for it.", "Chewed on {x} all day like bad tobacco." } },
            { "Steady", new[] { "Been thinking about {x}.", "Not much to tell. {X}, mostly.", "{X}. That's what's on my mind.", "{X}, and not much else.", "Turned {x} over in my head all evening." } },
            { "Joyful", new[] { "{X}. Life ain't so bad.", "Things are looking up. {X}.", "Can't complain. {X}.", "{X}. Whistled all the way home.", "{X}. Haven't felt this light in years." } },
            { "Grieving", new[] { "{X}. Some days that's all I think about.", "{X}... sorry. I'm poor company today.", "Not a good time. {X}.", "{X}. The house is too quiet.", "Dreamed about {x}. Woke up and remembered." } },
            { "Drunk", new[] { "{X}. Don't judge me, friend.", "{X}. Just one more.", "Hah. {X}.", "{X}. Or was it yesterday.", "Can't hold the pen straight. {X}." } },
            { "Jailed", new[] { "{X}. Never again.", "{X}. Law's got it in for me.", "{X}. Counted the bars again. Still nine.", "{X}. The beans in here would kill a mule." } },
            { "Homesick", new[] { "{X}. Can't stop thinking on it.", "Thinking about {x}. Wish I was home.", "{X}. Smelled bread baking and near cried.", "{X}. Some nights it's all I want." } }
        };

        // A sentence-shaped thought ("I broke my arm breaking a colt") gets these instead.
        private static readonly Dictionary<string, string[]> MoodSentenceFrames = new Dictionary<string, string[]>
        {
            { "Sick", new[] { "{S}. Still ain't right." } },
            { "Hurting", new[] { "{S}. Still hurts." } },
            { "Shaken", new[] { "{S}. Can't shake it.", "{S}. Still see it when I close my eyes." } },
            { "Humiliated", new[] { "{S}. Don't laugh." } },
            { "Proud", new[] { "{S}. Not bad, eh?" } },
            { "Flush", new[] { "{S}. Good week." } },
            { "Bitter", new[] { "{S}. Still sticks in my craw." } },
            { "Steady", new[] { "{S}." } },
            { "Joyful", new[] { "{S}. Life ain't so bad." } },
            { "Grieving", new[] { "{S}. Can't think of much else." } },
            { "Drunk", new[] { "{S}. Hah." } },
            { "Jailed", new[] { "{S}. Never again." } },
            { "Homesick", new[] { "{S}. Wish I was home." } }
        };

        // Thoughts about the player ("your face", "killing you") need their own frames when said to his face.
        private static readonly Dictionary<string, string[]> MoodYouFrames = new Dictionary<string, string[]>
        {
            { "Shaken", new[] { "I can't stop thinking about {x}.", "{X}. I see it at night." } },
            { "Bitter", new[] { "{X}. That's what's on my mind.", "You want to know what I think about? {X}." } },
            { "Steady", new[] { "Been thinking about {x}.", "{X}. I think about it a lot." } },
            { "Proud", new[] { "{X}. Every word true.", "{X}. Ain't ashamed of it." } }
        };

        private static readonly Regex YouWord = new Regex(@"\byou(r|rs|rself)?\b", RegexOptions.IgnoreCase);

        /// Small talk about whatever is on their mind right now.
        public static string MoodLine(Person p, ThoughtItem t, string seed)
        {
            if (t == null || string.IsNullOrEmpty(t.Text))
                return Pick(seed + "m", "Can't complain.", "Same as always.", "Getting by.");
            string mood = MoodFrames.ContainsKey(t.Mood ?? "") ? t.Mood : "Steady";
            string text = t.Text.Trim();
            string[] frames;
            if (YouWord.IsMatch(text) && MoodYouFrames.TryGetValue(mood, out frames))
                return Fill(Pick(seed + t.Text, frames), NounPhrase(text, p.Woman));

            if (StartsLikeSentence(text))
            {
                string s = Grammar.FirstPerson(text, p.Woman);
                return Grammar.Sentence(Pick(seed + t.Text, MoodSentenceFrames[mood]).Replace("{S}", s));
            }
            string np = NounPhrase(text, p.Woman);
            if (mood == "Homesick" && np.Equals("home", StringComparison.OrdinalIgnoreCase))
                return "Home. Wish I was there.";
            return Fill(Pick(seed + t.Text, MoodFrames[mood]), np);
        }

        private static bool StartsLikeSentence(string text)
        {
            string w = text.Split(' ')[0].ToLowerInvariant();
            return w == "he" || w == "she" || w == "nearly" || w == "laid" || w == "spent" || w == "got" || Grammar.StartsWithPastVerb(text);
        }

        /// "his dead brother" -> "my dead brother" (a noun phrase stays a noun phrase).
        public static string NounPhrase(string text, bool woman)
        {
            return Grammar.Phrase((text ?? string.Empty).Trim(), woman);
        }

        private static string Fill(string frame, string x)
        {
            if (string.IsNullOrEmpty(x))
                return frame;
            string upper = Grammar.UpperFirst(x);
            string lower = Grammar.SoftLower(x);
            return Grammar.Sentence(frame.Replace("{X}", upper).Replace("{x}", lower));
        }

        // ---- what they want ----------------------------------------------------------------------------

        public static string GoalLine(Person p, GoalItem g, string seed)
        {
            if (g == null || string.IsNullOrEmpty(g.Text))
                return null;
            switch (g.Type)
            {
                case "Avenge":
                    if (g.Target == "you")
                        return Pick(seed + "g", "What do I want? To see you laid out in a pine box.", "I mean to settle up with you, once and for all.");
                    break;
                case "Settle":
                    return Pick(seed + "g", "One day I'm getting out of this life. Going home.", "A little land, far off, where my name means nothing.");
                case "Rich":
                    return Pick(seed + "g", "One good haul. That's all I need. Then I hang up the gun.", "Gold. Enough that I never sleep with one eye open again.");
                case "Rise":
                    return Pick(seed + "g", "I won't be taking orders forever. Someday the men will answer to me.", "I aim to climb. You'll see.");
                case "Prove":
                    return Pick(seed + "g", "Everybody figures me for a nobody. They'll learn.", "Folks underestimate me. Not for long.");
                case "Protect":
                    return Pick(seed + "g", "All I want is to keep my family alive.", "My kin. Keeping them breathing. That's what matters.");
            }
            string first = g.Text.Split(' ')[0].ToLowerInvariant();
            string text = Grammar.FirstPerson(g.Text, p.Woman);
            if (text.StartsWith("I "))
                text = text.Substring(2);
            return first == "a" || first == "an" || first == "the"
                ? Grammar.Sentence("All I want is " + Grammar.SoftLower(text))
                : Grammar.Sentence("All I want is to " + Grammar.SoftLower(text));
        }

        // ---- the player, as this person sees him ----------------------------------------------------

        /// How they refer to the player in private: "Morgan", "the man who let me go", "that stranger".
        public static string YouPrivate(Person p, PlayerRef you, bool firstMention)
        {
            if (you.KnownByName)
                return firstMention ? you.FullName : you.Surname;
            string d = Relative(you.Deed, p.Woman);
            if (d != null)
                return d;
            if (!string.IsNullOrEmpty(you.Heard))
                return "the one " + you.Heard;
            return you.Scarred ? "that scarred stranger" : "that stranger";
        }

        private static readonly Regex Tail = new Regex(@"( - |, ).*$");

        /// enRAGE's memory text -> a description of the player: "you let him go" -> "the man who let me go".
        public static string Relative(string deed, bool woman)
        {
            if (string.IsNullOrEmpty(deed))
                return null;
            string d = Tail.Replace(deed.Trim(), string.Empty);
            string l = d.ToLowerInvariant();
            if (l.StartsWith("you "))
                return "the man who " + Grammar.Phrase(d.Substring(4), woman);
            if (l == "killed you")
                return "the man I killed";
            if (l == "owes you his life" || l == "owes you her life")
                return "the man I owe my life to";
            if (l.StartsWith("fought you"))
                return "the man I fought";
            if (l.Contains("in a fight with you"))
                return "the man I ran from";
            if (l.StartsWith("arrested in") && l.EndsWith("going for you"))
                return "the man I went for";
            Match m = Regex.Match(d, @"^(.*?)\byou\b(.*)$", RegexOptions.IgnoreCase);
            if (m.Success)
            {
                string before = Grammar.FirstPerson(m.Groups[1].Value.Trim(), woman);
                string after = m.Groups[2].Value.Trim();
                if (before.StartsWith("I "))
                    before = before.Substring(2);
                if (after.Length == 0)
                    return "the man I " + before;
                if (Regex.IsMatch(after, @"^(walk|go|run|ride|live|leave)\b"))
                    return "the man I " + before + " " + after.Split(' ')[0];
            }
            return null;
        }

        /// Swap "you/your" for the private description of the player.
        public static string ToPrivate(string spoken, Person p, PlayerRef you)
        {
            spoken = Regex.Replace(spoken, @"\b([Yy]ou)'ve\b", "$1 has");
            spoken = Regex.Replace(spoken, @"\b([Yy]ou)'re\b", "$1 is");
            spoken = Regex.Replace(spoken, @"\b([Yy]ou)'d\b", "$1 would");
            spoken = Regex.Replace(spoken, @"\b([Yy]ou) were\b", "$1 was");
            bool first = true;
            string who = null;
            return Regex.Replace(spoken, @"\b([Yy]ou(r|rs|rself)?)\b", m =>
            {
                string w = m.Value.ToLowerInvariant();
                if (who == null)
                    who = YouPrivate(p, you, true);
                string r = first ? who : (you.KnownByName ? you.Surname : "him");
                if (!first && !you.KnownByName && (who.StartsWith("the man") || who.StartsWith("the one")))
                    r = "him";
                first = false;
                if (w == "your")
                    r = r == "him" ? "his" : r + "'s";
                else if (w == "yours")
                    r = r == "him" ? "his" : r + "'s";
                else if (w == "yourself")
                    r = "himself";
                bool sentenceStart = m.Index == 0 || Regex.IsMatch(spoken.Substring(0, m.Index), @"[.!?]\s*$");
                return sentenceStart ? Grammar.UpperFirst(r) : r;
            });
        }

        // ---- memories of the player ----------------------------------------------------------------

        /// A few of enRAGE's memories leave the player out of the words; put him back in.
        public static string Normalize(string text)
        {
            string t = (text ?? string.Empty).Trim();
            if (Regex.IsMatch(t, @"^paid for (his|her) life$"))
                return t.Replace("paid for", "paid you for");
            if (t == "turned informer")
                return "turned informer for you";
            if (Regex.IsMatch(t, @"^(he|she) talked - gave up (his|her) own$"))
                return t + " to you";
            return t;
        }

        private static bool YouActed(MemoryItem m)
        {
            return Normalize(m.Text).StartsWith("you ", StringComparison.OrdinalIgnoreCase);
        }

        private static string Them(string relation)
        {
            switch ((relation ?? string.Empty).ToLowerInvariant())
            {
                case "brother": case "father": case "son": case "uncle": case "nephew": case "husband": case "grandfather": case "boy":
                    return "him";
                case "sister": case "mother": case "daughter": case "aunt": case "niece": case "wife": case "grandmother": case "girl":
                    return "her";
                default:
                    return "them";
            }
        }

        public static string SpokenLine(Person p, MemoryItem m)
        {
            string t = Normalize(m.Text);
            Match x;
            if ((x = Regex.Match(t, @"^you nearly killed (him|her) - (.+)$", RegexOptions.IgnoreCase)).Success)
                return "You nearly killed me. Left me with " + NounPhrase(x.Groups[2].Value, p.Woman) + ".";
            if ((x = Regex.Match(t, @"^you killed (his|her) ([\w-]+),? (.+?)( in front of (him|her))?$", RegexOptions.IgnoreCase)).Success)
                return "You killed my " + x.Groups[2].Value + ", " + Person.FirstOf(x.Groups[3].Value) +
                       (x.Groups[4].Success ? ". Right in front of me." : ".");
            if ((x = Regex.Match(t, @"^you robbed (him|her) - (\d+) times now$", RegexOptions.IgnoreCase)).Success)
                return "You've robbed me " + Number(int.Parse(x.Groups[2].Value)) + " times now.";
            if (Regex.IsMatch(t, @"^you knocked (him|her) cold - again$", RegexOptions.IgnoreCase))
                return "You knocked me cold. Again.";
            if (t == "killed you")
                return "I killed you. I watched you die. And here you stand.";
            if ((x = Regex.Match(t, @"^you killed (him|her)( near [^-]+?)? - (he|she) woke up again$")).Success)
                return "You killed me" + x.Groups[2].Value.TrimEnd() + ". And I woke up again.";
            if ((x = Regex.Match(t, @"^died( near [^-]+?)? - and woke up again$")).Success)
                return "I died" + x.Groups[1].Value.TrimEnd() + ". And I woke up again.";
            if ((x = Regex.Match(t, @"^saw you (.+)$")).Success && !t.Contains(" - "))
                return "I saw you " + x.Groups[1].Value + ".";
            if (t == "fell for you")
                return "I've gone and fallen for you.";
            if ((x = Regex.Match(t, @"^you broke (his|her) heart$")).Success)
                return "You broke my heart.";
            if (Regex.IsMatch(t, @"^(he|she) talked - gave up (his|her) own to you$"))
                return "I talked. Gave up my own to you.";
            return Grammar.Sentence(Grammar.FirstPerson(t, p.Woman));
        }

        private static string Lost(MemoryItem m)
        {
            Match x = Regex.Match(m.Text ?? string.Empty, @"killed (his|her) ([\w-]+)");
            return x.Success ? Them(x.Groups[2].Value) : "them";
        }

        /// Said to the player's face, coloured by how they feel about him now.
        public static string MemoryToYou(Person p, MemoryItem m, string seed)
        {
            string core = SpokenLine(p, m);
            string s = seed + m.Text;
            bool you = YouActed(m);
            if (p.Murderous && Bad(m.Emotion) && you)
                return core + " " + Pick(s, "You're a dead man.", "I'll see you in the ground for it.", "Next time I won't miss.");
            if (p.Vengeful && Bad(m.Emotion) && you)
                return core + " " + Pick(s, "This ain't over.", "I'm still owed for that.");
            switch (m.Emotion)
            {
                case "Joy":
                    if (!you)
                        return core + " " + Pick(s, "Felt good, I'll admit.", "Best day I've had in a while.");
                    if (m.Text.Contains("drink"))
                        return core + " " + Pick(s, "Next one's on me.", "Decent of you.");
                    return core + " " + Pick(s, "I won't forget it.", "Figure I owe you one.", "Didn't have to do that.");
                case "Anger":
                    if (!you)
                        return core + " " + Pick(s, "Won't happen twice.", "Should've done different.");
                    return core + " " + Pick(s, "I haven't forgotten.", "Someday you'll answer for it.", "Don't think I'll let it go.");
                case "Fear":
                    if (!you)
                        return core + " " + Pick(s, "If the boys find out, I'm dead.", "Don't you tell a soul.");
                    return core + " " + Pick(s, "I still see it when I close my eyes.", "Stay away from me.", "Please. Just go.");
                case "Shame":
                    if (!you)
                        return core + " " + Pick(s, "Ain't my proudest day.", "Don't rub it in.");
                    return core + " " + Pick(s, "I'd rather not talk about it.", "Ain't my proudest day.");
                case "Grief":
                    return core + " " + Pick(s, "I'll carry that to my grave.", "You took " + Lost(m) + " from me.");
                case "Pride":
                    if (m.Text == "killed you")
                        return core;
                    return core + " " + Pick(s, "And I'd do it again.", "Ain't ashamed of it.");
                default:
                    return core;
            }
        }

        /// To themselves: "That stranger let me go. Why would he do that?"
        public static string MemoryThought(Person p, MemoryItem m, PlayerRef you, string seed)
        {
            // never describe him by the very thing this thought is about ("the man who let me go let me go")
            if (you.Deed != null && Tail.Replace(you.Deed, string.Empty) == Tail.Replace(m.Text, string.Empty))
                you = new PlayerRef { FullName = you.FullName, KnownByName = you.KnownByName, Heard = you.Heard, Scarred = you.Scarred };
            string core = ToPrivate(SpokenLine(p, m), p, you);
            if (m.Text == "killed you")
                core = "I killed " + YouPrivate(p, you, true) + ". Watched him go down. Now he's walking around like nothing happened.";
            string s = seed + m.Text;
            bool did = YouActed(m);
            if (m.Text == "fell for you")
                return core + " " + Pick(s, "God help me.", "I can't stop thinking about him.", "Don't let it show. Don't let it show.");
            switch (m.Emotion)
            {
                case "Joy":
                    if (!did)
                        return core + " " + Pick(s, "Felt good.", "Folks will hear about it.");
                    return core + " " + Pick(s, "Why would he do that?", "Maybe he ain't all bad.", "I owe him. I hate owing anybody.");
                case "Anger":
                    if (!did)
                        return core + " " + Pick(s, "Never again.", "Should've stopped him.");
                    return core + " " + Pick(s, "One day I'll pay him back.", "I won't forget his face.", "He'll answer for it.");
                case "Fear":
                    if (!did)
                        return core + " " + Pick(s, "If the boys find out, I'm a dead man.", "Nobody can know.");
                    return core + " " + Pick(s, "I keep seeing his face.", "Next time he'll finish it.", "I can't sleep.");
                case "Shame":
                    return core + " " + Pick(s, "Can't look anyone in the eye.", "Nobody can know.", p.Woman ? "What kind of woman am I?" : "What kind of man am I?");
                case "Grief":
                    return core + " " + Pick(s, "I'll never forgive him.", "Nothing will bring " + Lost(m) + " back.");
                case "Pride":
                    return core + " " + Pick(s, "Let him come again.", "Folks will hear about it.");
                default:
                    return core;
            }
        }

        /// A diary entry, dated. Written the day it happened.
        public static string Diary(Person p, MemoryItem m, PlayerRef you, string date, string seed)
        {
            string core = MemoryThought(p, m, you, seed);
            string opener;
            if (!Bad(m.Emotion))
                opener = Pick(seed + "d" + m.Text, "", "Something happened today. ", "Good day, for once. ");
            else if (m.Intensity >= 70)
                opener = Pick(seed + "d" + m.Text, "My hand's still shaking. ", "I don't know how to write this. ");
            else
                opener = Pick(seed + "d" + m.Text, "", "Hard day. ", "Writing this down so I don't forget. ", "Can't sleep. ");
            return date + " - " + opener + core;
        }

        // ---- names ----------------------------------------------------------------------------------

        public static List<string> AskName(Person p, PlayerRef you, string seed)
        {
            string ask = p.Surly ? Pick(seed + "n", "You got a name?", "And who are you?")
                : Pick(seed + "n", "Didn't catch your name.", "And who might you be?", "What do folks call you?");
            string answer = "\"" + you.Surname + ". " + you.FullName + ".\"";
            string reply;
            if (p.Hate >= 40)
                reply = Pick(seed + "r", you.Surname + ". I'll remember that.", you.Surname + ". I'll be sure to remember it.");
            else if (p.Friendly)
                reply = Pick(seed + "r", "Pleasure, Mr. " + you.Surname + ".", "Good to know you, " + you.FirstName + ".");
            else
                reply = Pick(seed + "r", you.Surname + ". Alright.", you.Surname + ". Huh.");
            return new List<string> { ask, "YOU:" + answer, reply };
        }

        // ---- what others said ----------------------------------------------------------------------

        /// "My brother Elihu says you robbed him."
        public static string HearsayToYou(Person listener, string source, string sourceText, bool sourceWoman, string seed)
        {
            string claim = Retell(sourceText, sourceWoman);
            return Pick(seed + "h", source + " says " + claim + ".", "Heard what you did. " + source + " told me: " + claim + ".",
                "I know about you. " + source + " says " + claim + ".");
        }

        /// Private: "Elihu says that stranger robbed him."
        public static string HearsayThought(Person listener, string source, string sourceText, bool sourceWoman, PlayerRef you, string seed)
        {
            string claim = Retell(sourceText, sourceWoman);
            string t = ToPrivate(source + " says " + claim + ".", listener, you);
            return t + " " + Pick(seed + "ht", "I'll keep an eye out for him.", "If I ever see him...", "Hard to know what to believe.");
        }

        /// enRAGE's third-person memory, retold by someone else: "you let him go - again" -> "you let him go".
        public static string Retell(string text, bool woman)
        {
            string t = Normalize(text);
            if (t.StartsWith("he talked") || t.StartsWith("she talked"))
                t = t.Replace(" - ", ", ");
            else
                t = Tail.Replace(t, string.Empty);
            if (t.StartsWith("you ", StringComparison.OrdinalIgnoreCase) || t.StartsWith("he ") || t.StartsWith("she "))
                return t;
            // "Pvt. Maddox saw you kill a lawman": someone else's name already leads the claim
            if (t.Length > 1 && char.IsUpper(t[0]) && !t.StartsWith("I ") && !t.StartsWith("You "))
                return t;
            string pro = woman ? "she " : "he ";
            // "jailed in Rhodes for drunkenness" -> "he was jailed in Rhodes for drunkenness"
            if (Grammar.StartsPassive(t))
                return pro + "was " + t;
            // "stole your horse" -> "he stole your horse"
            return pro + t;
        }

        /// Puts a name in place of the teller's pronouns, for the next person to retell:
        /// "you robbed him" (Elihu) -> "you robbed Elihu"; "he fought you" -> "Elihu fought you".
        public static string Named(string text, string name, bool woman)
        {
            string subj = woman ? "she" : "he";
            string obj = woman ? "her" : "him";
            string pos = woman ? "her" : "his";
            List<string> words = (text ?? string.Empty).Split(' ').ToList();
            bool done = false;
            for (int i = 0; i < words.Count && !done; i++)
            {
                string w = words[i].ToLowerInvariant();
                string bare = w.TrimEnd(',', '.');
                string punct = w.Substring(bare.Length);
                if (i == 0 && bare == subj)
                {
                    words[i] = name + punct;
                    done = true;
                }
                else if (bare == obj && (woman ? !NextIsWord(words, i) : true) && bare != pos)
                {
                    words[i] = name + punct;
                    done = true;
                }
                else if (bare == pos && (!woman || NextIsWord(words, i)))
                {
                    words[i] = name + "'s" + punct;
                    done = true;
                }
            }
            return string.Join(" ", words);
        }

        private static bool NextIsWord(List<string> words, int i)
        {
            if (i + 1 >= words.Count || words[i].EndsWith(",") || words[i].EndsWith("."))
                return false;
            string n = words[i + 1].ToLowerInvariant();
            return n.Length > 0 && char.IsLetter(n[0]) && n != "up" && n != "out" && n != "off" && n != "go" && n != "a" && n != "an" &&
                   n != "the" && n != "for" && n != "to" && n != "in" && n != "by" && n != "with" && n != "like" && n != "again";
        }

        private static readonly KeyValuePair<Regex, string>[] Bigger =
        {
            new KeyValuePair<Regex, string>(new Regex(@"^you robbed ([\w']+)$"), "you robbed $1 blind"),
            new KeyValuePair<Regex, string>(new Regex(@"^you let ([\w']+) go$"), "you let $1 go, of all things"),
            new KeyValuePair<Regex, string>(new Regex(@"^you nearly killed ([\w']+)$"), "you left $1 for dead"),
            new KeyValuePair<Regex, string>(new Regex(@"^you knocked ([\w']+) out cold$"), "you beat $1 half to death"),
            new KeyValuePair<Regex, string>(new Regex(@"^you bought ([\w']+) a drink$"), "you stood $1 and the whole saloon a round"),
            new KeyValuePair<Regex, string>(new Regex(@"^you killed ([\w']+) (\w+)$"), "you gunned down $1 $2 in cold blood"),
            new KeyValuePair<Regex, string>(new Regex(@"^you tied ([\w']+) up like an animal$"), "you hogtied $1 and dragged him behind your horse"),
            new KeyValuePair<Regex, string>(new Regex(@"^you threatened ([\w']+) family$"), "you swore you'd kill $1 whole family"),
            new KeyValuePair<Regex, string>(new Regex(@"^you questioned ([\w']+)$"), "you tortured $1"),
        };

        /// Every retelling makes it a little bigger.
        public static string Exaggerate(string text)
        {
            foreach (KeyValuePair<Regex, string> b in Bigger)
                if (b.Key.IsMatch(text))
                    return b.Key.Replace(text, b.Value);
            return text;
        }

        /// Gossip about someone they know: "Heard Elihu was thrown from his horse."
        public static string Gossip(string eventTitle, string fullName, string callThem, string seed)
        {
            if (string.IsNullOrEmpty(eventTitle))
                return null;
            string t = eventTitle.Trim().TrimEnd('.');
            if (!string.IsNullOrEmpty(fullName))
                t = t.Replace(fullName, callThem);
            return Pick(seed + "gs", "You hear? " + t + ".", "Heard " + t + ".", "Word is, " + t + ".");
        }

        // ---- helpers ---------------------------------------------------------------------------------

        private static bool Bad(string emotion)
        {
            return emotion == "Anger" || emotion == "Fear" || emotion == "Shame" || emotion == "Grief";
        }

        private static string Or(string s, string fallback)
        {
            return string.IsNullOrEmpty(s) ? fallback : s;
        }

        private static readonly string[] Ones = { "zero", "one", "two", "three", "four", "five", "six", "seven", "eight", "nine", "ten",
            "eleven", "twelve", "thirteen", "fourteen", "fifteen", "sixteen", "seventeen", "eighteen", "nineteen" };
        private static readonly string[] Tens = { "", "", "twenty", "thirty", "forty", "fifty", "sixty", "seventy", "eighty", "ninety" };

        public static string Number(int n)
        {
            if (n < 0 || n >= 100)
                return n.ToString();
            if (n < 20)
                return Ones[n];
            return Tens[n / 10] + (n % 10 == 0 ? string.Empty : "-" + Ones[n % 10]);
        }
    }
}
