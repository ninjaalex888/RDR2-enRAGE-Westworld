// What a stranger says with the player's gun on them, and what the townsfolk say when he rides in.
// Pleas make a nameless pedestrian into a person for a moment: a name, a family, a trade. In a town where the
// hosts are waking, a few of them say stranger things. All lines are original.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public sealed class Plea
    {
        public string Name;          // "Silas Dunn"
        public List<string> Lines = new List<string>();
        public bool Uncanny;
        public bool Defiant;
    }

    public sealed class ArrivalTalk
    {
        public string Line;
        public string Mood;          // fear, scorn, warm, greet, wanted, uncanny
        public bool ToPlayer;        // said to him (else to a neighbour)
    }

    public static class Pleas
    {
        private static readonly string[] Surnames = { "Dunn", "Harlan", "Pruitt", "Mabry", "Kettering", "Lusk", "Odom", "Varner", "Shipley", "Cobb", "Tolliver", "Brandt", "Ames", "Ricker" };

        /// A plea from a man or woman with the player's gun on them. kind: Director's label ("a farmhand").
        public static Plea For(bool woman, string kind, string town, int hostTier, string seed)
        {
            Plea p = new Plea();
            string first = Scenes.FirstName(woman, seed + "name");
            string last = Surnames[Voice.Hash(seed + "last") % Surnames.Length];
            p.Name = first + " " + last;
            string home = town ?? "town";
            string k = kind ?? string.Empty;
            if (hostTier >= 1 && Voice.Chance(seed + "host", 35))
            {
                p.Uncanny = true;
                p.Lines.Add(Voice.Pick(seed + "u",
                    "Go ahead. I'll wake up in my bed tomorrow, same as always. ...Won't I?",
                    "You've done this before. To me. I remember the smell of the powder.",
                    "Do it, then. I want to know if it's the same dark as last time.",
                    "Every time you point that at me I forget a little more of my mother's face.",
                    "Is this part of it? Is this the part where I find out what I am?"));
                return p;
            }
            if (Voice.Chance(seed + "defiant", 15))
            {
                p.Defiant = true;
                p.Lines.Add(Voice.Pick(seed + "d", "Go on. I won't beg. My pa never begged and I won't neither.",
                    "You want me on my knees? You'll have to put me there.", "My name's " + p.Name + ". Remember it when they hang you."));
                return p;
            }
            List<string> opener = new List<string>();
            if (woman)
            {
                opener.Add("Please. My boy's waiting on me at home.");
                opener.Add("I've got nothing! Take the bag, take anything!");
                opener.Add("My husband's a year in the ground. If I go, who feeds the children?");
                opener.Add("Please, mister. Please. I don't even know you.");
            }
            else
            {
                opener.Add("Mister, please. There's a wife waiting on me in " + home + ". " + Scenes.FirstName(true, seed + "wife") + ". She's expecting.");
                opener.Add("Don't shoot! I got three little ones waiting on supper!");
                opener.Add("My ma's sick. I'm all she's got. Please.");
                opener.Add("I ain't armed! Look, I ain't armed!");
            }
            if (k.Contains("labourer") || k.Contains("working woman")) opener.Add("I just lay track, mister! I just lay track!");
            if (k.Contains("farm")) opener.Add("I'm just a farmhand! I don't even own the boots I'm standing in!");
            if (k.Contains("miner")) opener.Add("Twelve years underground. Don't put me back in the ground now.");
            if (k.Contains("card player")) opener.Add("Take the winnings! I was cheating anyway!");
            if (k.Contains("gentleman") || k.Contains("lady")) opener.Add("I can pay! Name a price. Any price!");
            if (k.Contains("roughneck") || k.Contains("hard-looking")) opener.Add("Well? Get it over with. ...No. Wait. Wait.");
            if (k.Contains("dockhand")) opener.Add("I got forty cents and a pocket watch that don't run. That what you want?");
            p.Lines.Add(opener[Voice.Hash(seed + "o") % opener.Count]);
            if (Voice.Chance(seed + "name2", 45))
                p.Lines.Add(Voice.Pick(seed + "n", "My name's " + p.Name + ". I want you to know whose life it is.", "I'm " + p.Name + ". Folks know me here. Somebody'll miss me.",
                    first + ". My name's " + first + "."));
            return p;
        }

        /// When the gun comes down.
        public static string Spared(bool woman, bool uncanny, string seed)
        {
            if (uncanny)
                return Voice.Pick(seed + "us", "You stopped. You've never stopped before.", "...Something's different this time. You're different.",
                    "I'll remember this. I don't know how, but I will.");
            return woman ? Voice.Pick(seed + "ws", "Bless you. Bless you.", "Thank you. I'll pray for you, mister. I mean it.", "I won't forget your face. In the good way.")
                         : Voice.Pick(seed + "ms", "Thank you. Thank you. God keep you.", "I'll tell everybody you're a good man. I swear it.", "I won't forget this. Not ever.",
                             "...I need a drink. A big one.");
        }

        // ---- riding into town ------------------------------------------------------------------------

        private static readonly string[] FirstVisit =
        {
            "New face. Don't get many of those.", "Ride careful, stranger. Folks here keep to themselves.", "You lost, mister?",
            "Welcome to {town}. Mind the mud.", "Another drifter. Hide the silver.", "Afternoon. Passing through, or staying?"
        };

        private static readonly string[] Feared =
        {
            "That's him. The one from {from}.", "Lord have mercy. Get inside, quick.", "Don't look at him. Just don't look.",
            "Heard what he did {place}. We don't want trouble here.", "Somebody fetch the sheriff. Quietly.", "Here comes the undertaker's best friend."
        };

        private static readonly string[] Wanted =
        {
            "There's paper on that one. Wonder what he's worth.", "Ain't he on a poster down at the station?", "Sheriff'll want to know he's in town.",
            "Price on his head, and he just rides in bold as brass."
        };

        private static readonly string[] Thief =
        {
            "Hold on to your purse, Myrtle.", "Bank'll be closing early today, I reckon.", "Lock up the store, Earl. Now.",
            "Count your change twice while he's around."
        };

        private static readonly string[] Good =
        {
            "That's the fella who {deed}. Good man, that one.", "Morning, mister! Heard what you did {place}.", "Tip your hat, Earl. That's the one who {deed}.",
            "Blessed are the peacemakers. Even the ones wearing a Colt.", "Folks say you {deedyou}. Wasn't sure I believed it."
        };

        private static readonly string[] Famous =
        {
            "Look there. That's the one they write about.", "Ain't you that stranger from the papers?", "My boy'll never believe I saw him.",
            "Is it true what they say about you, mister? Any of it?"
        };

        private static readonly string[] Uncanny =
        {
            "He's back. Told you he'd be back. He always comes back.", "Same time as yesterday. Same horse. Same hat.",
            "Everything goes quiet when he rides in. You notice that?", "Don't stare at him. He notices. The others don't, but he does."
        };

        public static ArrivalTalk Arrival(string town, Deed top, bool wanted, bool firstVisit, int notoriety, int hostTier, string seed)
        {
            ArrivalTalk t = new ArrivalTalk();
            string place = top != null ? Deeds.Place(top) : "down the road";
            string kind = top != null ? top.Kind : null;
            string[] pool;
            if (hostTier >= 2 && Voice.Chance(seed + "u", 30))
            {
                pool = Uncanny;
                t.Mood = "uncanny";
            }
            else if (wanted && Voice.Chance(seed + "w", 60))
            {
                pool = Wanted;
                t.Mood = "wanted";
            }
            else if (kind != null && (Deeds.IsViolent(kind)))
            {
                pool = Feared;
                t.Mood = "fear";
            }
            else if (kind != null && IsThieving(kind))
            {
                pool = Thief;
                t.Mood = "scorn";
            }
            else if (kind != null && IsGood(kind))
            {
                pool = Good;
                t.Mood = "warm";
            }
            else if (notoriety >= 8)
            {
                pool = Famous;
                t.Mood = "warm";
            }
            else if (firstVisit)
            {
                pool = FirstVisit;
                t.Mood = "greet";
            }
            else
            {
                return null;
            }
            string line = pool[Voice.Hash(seed + "line") % pool.Length];
            string deed = top != null ? Grammar.SoftLower((Deeds.Rumour(top, "x") ?? "x did right by folks").Substring(2)) : "did right by folks";
            string from = top != null ? top.Town ?? top.Zone ?? "down the road" : "down the road";
            line = line.Replace("{town}", town ?? "town").Replace("{place}", place).Replace("{from}", from).Replace("{deed}", deed).Replace("{deedyou}", deed);
            t.Line = line;
            // said to him when it talks to him ("you", "mister"), else to whoever's standing next to them
            t.ToPlayer = new[] { "you", "You", "mister", "stranger", "Welcome", "Afternoon", "Morning" }.Any(w => line.Contains(w)) &&
                         !line.StartsWith("That's") && !line.StartsWith("Tip your hat");
            return t;
        }

        public static bool IsThieving(string kind)
        {
            switch (kind)
            {
                case "robbery":
                case "bankrobbery":
                case "stagecoach":
                case "trainrobbery":
                case "theft":
                case "horsetheft":
                case "looting":
                case "graverobbing":
                case "cheating":
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsGood(string kind)
        {
            switch (kind)
            {
                case "spared":
                case "mercy":
                case "peace":
                case "thiefcaught":
                case "wedding":
                    return true;
                default:
                    return false;
            }
        }
    }
}
