// A plain snapshot of one enRAGE person, decoupled from enRAGE's own types so the voice and
// knowledge code can be tested without the game. Filled in game by Game/Snapshot.cs.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public sealed class Person
    {
        public string Id;
        public string Name;              // "Elihu Starling"
        public string Title;             // nickname: "the Rattler", "Dead-Eye", "of Thieves' Landing"
        public string GangId;            // enRAGE faction id: "odriscoll", "army", null
        public string GangName;          // "the O'Driscoll Boys"
        public string Trade;             // "Trapper", "Outlaw", ...
        public string Birthplace;        // "Saint Denis, Lemoyne"
        public string Home;              // the in-game town this person calls home, if known ("Rhodes")
        public string Family;            // married / single / widower
        public string SpouseName;        // from a "Married ..." past line, if any
        public int Children;
        public string KinId;
        public string KinRelation;       // "brother", "cousin", ...
        public string KinName;           // resolved from KinId when the kin is on the roster
        public List<string> FriendIds = new List<string>();
        public List<string> FriendNames = new List<string>();
        public int BornYear;
        public bool Woman;
        public bool IsAlive = true;
        public int Rank;
        public string Personality;       // Hothead, Coward, Proud, Cold, Greedy, Loyal, Drifter (or null)
        public string Disposition;       // Friendly, Neutral, Surly
        public string Sociability;       // Introvert, Outgoing
        public string Devotion;          // Devout, Churchgoer, Lapsed, Godless
        public string Smarts;            // enRAGE's Smarts enum name
        public string Pet;
        public List<string> Quirks = new List<string>();   // drunk gambler braggart traitor honest superstitious sadist "family man"
        public List<string> Scars = new List<string>();    // descriptions, newest last
        public int Hate;
        public int Fear;
        public bool Vengeful;
        public bool Murderous;
        public int TimesGreeted;
        public int TimesSpared;
        public int TimesRobbed;
        public int YourNotoriety;
        public bool OwesYou;
        public bool KilledYou;
        public bool KilledByPlayer;       // dead, and the player did it
        public bool PresumedDead;         // enRAGE thinks he's dead; he may turn up again
        public long PresumedDeadAt;       // enRAGE's clock when it last presumed him dead (a come-back, its own or ours, is dated by it)
        public bool Buried;
        public int CheatedDeath;          // times he was left for dead and lived
        public string KillLocation;       // where enRAGE first met him / he fell
        public uint ModelHash;
        public bool Lore;                 // a story character enRAGE protects ("lore_" ids)
        public Dictionary<string, int> Facets = new Dictionary<string, int>();
        public Dictionary<string, int> Values = new Dictionary<string, int>();
        public Dictionary<string, int> Needs = new Dictionary<string, int>();
        public List<PastItem> Past = new List<PastItem>();
        public List<MemoryItem> Memories = new List<MemoryItem>();
        public List<ThoughtItem> Thoughts = new List<ThoughtItem>();
        public List<GoalItem> Goals = new List<GoalItem>();
        public List<string> RecentHistory = new List<string>(); // newest last, e.g. "Elihu Starling was thrown from his horse"

        public int Facet(string name)
        {
            int v;
            return Facets != null && Facets.TryGetValue(name, out v) ? v : 50;
        }

        public int Value(string name)
        {
            int v;
            return Values != null && Values.TryGetValue(name, out v) ? v : 0;
        }

        public int Need(string name)
        {
            int v;
            return Needs != null && Needs.TryGetValue(name, out v) ? v : 0;
        }

        public bool Has(string quirk)
        {
            return Quirks != null && Quirks.Contains(quirk);
        }

        public string FirstName
        {
            get { return string.IsNullOrEmpty(Name) ? "stranger" : FirstOf(Name); }
        }

        /// What friends call someone: "Elihu" for Elihu Starling, but "Pvt. Maddox" for Pvt. Maddox
        /// (a rank or title is never a first name).
        public static string FirstOf(string name)
        {
            if (string.IsNullOrEmpty(name))
                return name ?? string.Empty;
            string[] parts = name.Trim().Split(' ');
            if (parts.Length > 1 && IsRank(parts[0]))
                return parts[0] + " " + parts[parts.Length - 1];
            return parts[0];
        }

        public string Surname
        {
            get
            {
                if (string.IsNullOrEmpty(Name))
                    return string.Empty;
                string[] parts = Name.Split(' ');
                return parts.Length > 1 ? parts[parts.Length - 1] : string.Empty;
            }
        }

        public static bool IsRank(string word)
        {
            switch ((word ?? string.Empty).TrimEnd('.'))
            {
                case "Colonel": case "Col": case "Captain": case "Capt": case "Cpt": case "Sergeant": case "Sgt": case "Lieutenant": case "Lt":
                case "Major": case "Maj": case "Private": case "Pvt": case "Corporal": case "Cpl": case "General": case "Gen":
                case "Sheriff": case "Deputy": case "Marshal": case "Doctor": case "Dr": case "Reverend": case "Rev": case "Father": case "Sister":
                case "Mr": case "Mrs": case "Miss": case "Judge": case "Mayor": case "Professor": case "Prof":
                    return true;
                default:
                    return false;
            }
        }

        public int AgeIn(int year)
        {
            return BornYear > 0 ? year - BornYear : 35;
        }

        public string He { get { return Woman ? "she" : "he"; } }
        public string Him { get { return Woman ? "her" : "him"; } }
        public string His { get { return Woman ? "her" : "his"; } }
        public bool IsOutlaw { get { return !string.IsNullOrEmpty(GangId) && GangId != "army"; } }
        public bool IsSoldier { get { return GangId == "army"; } }

        public bool Friendly { get { return Disposition == "Friendly"; } }
        public bool Surly { get { return Disposition == "Surly"; } }
        public bool Outgoing { get { return Sociability == "Outgoing"; } }

        /// The mood behind the strongest current thought ("Steady" when nothing is on their mind).
        public ThoughtItem TopThought
        {
            get { return Thoughts == null ? null : Thoughts.FirstOrDefault(); }
        }

        public string Mood
        {
            get { ThoughtItem t = TopThought; return t != null ? t.Mood : "Steady"; }
        }
    }

    public sealed class PastItem
    {
        public int Index;                // position in enRAGE's Past list (so the game layer can mark it known)
        public int Year;
        public int Month;
        public string Text;
        public string Kind;              // youth work gang adult code crime loss faith war born family ...
        public bool Public;
        public bool Known;               // the player already knows this (enRAGE's own flag)

        public bool Secret
        {
            get { return !Public && (Kind == "crime" || Kind == "code" || Kind == "gang"); }
        }
    }

    public sealed class MemoryItem
    {
        public string Text;
        public string Emotion;           // Fear, Anger, Shame, Grief, Joy, Pride
        public int Intensity;
        public string Cause;             // "you" when the player caused it, another person's id, or null
        public long At;                  // enRAGE game minutes
        public bool LongTerm;

        /// enRAGE's default cause is "you", so a few memories carry it without being about the player.
        public bool AboutYou
        {
            get
            {
                if (Cause != "you" || string.IsNullOrEmpty(Text))
                    return false;
                string t = Text.ToLowerInvariant();
                return !(t.StartsWith("he leads ") || t.StartsWith("made ") || t.StartsWith("moved up"));
            }
        }

        public string Key { get { return Emotion + "|" + At + "|" + Text; } }
    }

    public sealed class ThoughtItem
    {
        public string Mood;
        public string Text;
        public int Weight;
        public long At;
    }

    public sealed class GoalItem
    {
        public string Type;              // Avenge, Prove, Rise, Rich, Settle, Protect
        public string Target;
        public string Text;
    }

    /// How one particular person knows the player.
    public sealed class PlayerRef
    {
        public string FullName = "Arthur Morgan";
        public bool KnownByName;
        public string Deed;              // strongest memory about the player, enRAGE's words ("you let him go")
        public string Heard;             // what they've only heard about the player, as a relative clause ("who robbed Elihu")
        public bool Scarred;             // John Marston's face

        public string FirstName { get { return FullName.Split(' ')[0]; } }
        public string Surname { get { string[] p = FullName.Split(' '); return p[p.Length - 1]; } }

        public static PlayerRef For(bool john)
        {
            return new PlayerRef { FullName = john ? "John Marston" : "Arthur Morgan", Scarred = john };
        }
    }
}
