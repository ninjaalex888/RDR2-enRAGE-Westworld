// How someone takes the player's flirting: from what they think of him (Regard, the same judgement as their thoughts
// about him) and, weighing as much, from who they are: what someone of their nature, faith and troubles looks for in
// a man, set against what he is here (his money, the good and the bad the town says he did). Their day tips it (sore
// from a bad turn, glad from a good one; a worry at its worst means not today), and someone spoken for says no. No
// dice: the same person in the same state answers the same way, and the reason they give is the one that weighed most.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class Interest
    {
        public sealed class Ask
        {
            public string Regard;      // Regard.Of...
            public bool Drawn;         // drawn to his sex, near enough his age
            public bool Taken;         // a sweetheart or a spouse of their own
            public bool AtWorst;       // a worry of their own at its worst today
            public bool Spam;          // he flirted a moment ago
            public string Temper;      // their temperament (else Who.Personality)
            public string Mood;        // "sore", "glad" or null (InnerLife.MoodOf)
            public Person Who;         // the rest of them: faith, sociability, quirks, family, humour (may be null)
            public string Worry;       // the worry they're living through, if any (InnerLife.WorryNow)
            public int Cash = -1;      // the player's money in dollars (-1 unknown)
            public int BadHere;        // the bad deeds of his the town still talks about
            public int GoodHere;       // ...and the good: mercy, a thief caught, peace kept
            public bool Saloon;        // asked in a saloon
            public bool SaloonGirl;    // the saloon's own: a working girl, or any woman drinking where he is
            public string Id;          // who, for the day's roll (ForPlayer)
            public long Day = -1;      // the game day, for the day's roll
            public int Greeted;        // 1: he greeted her first (R, a few minutes before); -1: he flirted cold; 0: not known
        }

        /// Married or spoken for: a sweetheart, someone courting them, an affair, or a husband.
        public static bool Spoken(Ask a)
        {
            return a.Taken || (a.Who != null && (a.Who.Family == "married" || a.Who.SpouseName != null));
        }

        /// How much what someone thinks of the player tips their odds, in percent.
        public static int RegardTilt(string regard)
        {
            switch (regard)
            {
                case "smitten": return 30;
                case "grateful": case "fond": return 15;
                case "met": return 5;
                case "wary": return -10;
                case "witnessed": case "afraid": return -15;
                case "hostile": return -35;
                default: return 0;
            }
        }

        /// The player's flirting with a woman (Lex, 2026-10-26: he kills so many in self-defence that fear had every
        /// woman saying no). One of the saloon's women always goes along; any other woman by a roll: single ones likely
        /// (single, percent), the married or spoken for less so (taken), with what she thinks of him, her nature and her
        /// day tipping it (5 points a weight) rather than deciding it. Only having spurned him, flirting again a moment
        /// later, or not being drawn to him at all is a flat no. The roll holds all game day for the same woman. Men are
        /// answered as before (Answer). chance: the percent it came to, -1 for the old rule.
        public const int GreetTilt = 15;   // percent, for a greeting first (or against, flirting cold)

        public static string ForPlayer(Ask a, int single, int taken, out string why, out int chance)
        {
            chance = -1;
            if (a.Who == null || !a.Who.Woman)
                return Answer(a, out why);
            if (a.Spam)
                return No("spam", out why);
            if (a.Regard == "scorned")
                return No("scorned", out why);
            if (a.SaloonGirl)
            {
                chance = 100;
                why = "saloon";
                return "yes";
            }
            if (!a.Drawn)
                return No("notdrawn", out why);
            bool spoken = Spoken(a);
            List<KeyValuePair<string, int>> taste = Taste(a);
            // greeting her first counts, and flirting cold counts against (Lex, 2026-10-26: greet, then flirt, then ask)
            int tilt = RegardTilt(a.Regard) + taste.Sum(t => t.Value) * 5 + (a.Mood == "glad" ? 5 : a.Mood == "sore" ? -5 : 0) - (a.AtWorst ? 15 : 0) + a.Greeted * GreetTilt;
            chance = spoken ? Math.Max(3, Math.Min(50, taken + tilt)) : Math.Max(10, Math.Min(95, single + tilt));
            int roll = (int)((uint)Voice.Hash((a.Id ?? a.Who.Id) + "|willing|" + a.Day) % 100u);
            if (roll < chance)
            {
                why = a.Regard == "smitten" ? "smitten" : spoken ? "stolen" : taste.Where(t => t.Value > 0).OrderByDescending(t => t.Value).Select(t => t.Key).FirstOrDefault() ?? a.Regard;
                return "yes";
            }
            if (roll < chance + 15)
            {
                why = taste.Any(t => t.Key == "shy") ? "shy" : taste.Any(t => t.Key == "troubled") ? "troubled" : a.Regard;
                return "maybe";
            }
            if (spoken)
                return No(a.Worry == "letter" ? "elsewhere" : "married", out why);
            switch (a.Regard)
            {
                case "hostile": return No("hate", out why);
                case "afraid": return No("fear", out why);
                case "witnessed": case "wary": return No("badrep", out why);
            }
            string worst = taste.Where(t => t.Value < 0).OrderBy(t => t.Value).Select(t => t.Key).FirstOrDefault();
            return No(worst ?? (a.Mood == "sore" ? "sore" : a.AtWorst ? "busy" : "cold"), out why);
        }

        /// "yes", "maybe" or "no"; why: the reason the answer is given by (Romance's reasons).
        public static string Answer(Ask a, out string why)
        {
            if (a.Spam)
                return No("spam", out why);
            if (!a.Drawn)
                return No("notdrawn", out why);
            switch (a.Regard)
            {
                case "scorned": return No("scorned", out why);
                case "hostile": return No("hate", out why);
                case "afraid": return No("fear", out why);
                case "witnessed": return No("badrep", out why);
            }
            bool smitten = a.Regard == "smitten";
            if (smitten)
            {
                why = "smitten";
                return "yes";
            }
            if (a.Taken)
                return No(a.Worry == "letter" ? "elsewhere" : "married", out why);
            if (a.AtWorst)
                return No("busy", out why);
            List<KeyValuePair<string, int>> taste = Taste(a);
            int warmth = Warmth(a.Regard) + taste.Sum(t => t.Value) + (a.Mood == "glad" ? 1 : a.Mood == "sore" ? -1 : 0);
            if (warmth >= 3)
            {
                why = taste.Where(t => t.Value > 0).OrderByDescending(t => t.Value).Select(t => t.Key).FirstOrDefault() ?? a.Regard;
                return "yes";
            }
            if (warmth >= 1)
            {
                why = taste.Any(t => t.Key == "shy") ? "shy" : taste.Any(t => t.Key == "troubled") ? "troubled" : a.Regard;
                return "maybe";
            }
            string worst = taste.Where(t => t.Value < 0).OrderBy(t => t.Value).Select(t => t.Key).FirstOrDefault();
            return No(worst ?? (a.Mood == "sore" ? "sore" : a.Regard == "wary" ? "badrep" : "cold"), out why);
        }

        /// What weighs with this person, each with how much: their nature, faith, ways and troubles, against what the
        /// player is here. Positive draws them to him, negative puts them off.
        public static List<KeyValuePair<string, int>> Taste(Ask a)
        {
            var t = new List<KeyValuePair<string, int>>();
            Person p = a.Who;
            bool rich = a.Cash >= 500, flush = a.Cash >= 300, broke = a.Cash >= 0 && a.Cash < 50, bad = a.BadHere > 0;
            switch (a.Temper ?? (p != null ? p.Personality : null))
            {
                case "Hothead": if (bad) Add(t, "danger", 1); break;              // a man with a name for trouble
                case "Coward": Add(t, bad ? "frightened" : "shy", bad ? -2 : -1); break;
                case "Proud": Add(t, rich ? "money" : "beneath", rich ? 1 : -1); break;   // someone with means
                case "Cold": if (!rich) Add(t, "cold", -1); break;               // hard to win, unless he's of use
                case "Greedy": if (flush) Add(t, "money", 2); else if (broke) Add(t, "poor", -1); break;
                case "Loyal": if (a.GoodHere > 0) Add(t, "decent", 2); else if (bad) Add(t, "killer", -1); break;
                case "Drifter": Add(t, "restless", 1); break;                     // a man who doesn't stay put
            }
            if (p != null)
            {
                if ((p.Devotion == "Devout" || p.Devotion == "Churchgoer") && bad)
                    Add(t, "pious", p.Devotion == "Devout" ? -2 : -1);
                if (p.Sociability == "Outgoing")
                    Add(t, "outgoing", 1);
                else if (p.Sociability == "Introvert" && a.Regard == "stranger")
                    Add(t, "shy", -1);
                if (p.Quirks.Contains("honest") && bad)
                    Add(t, "killer", -1);
                if (p.Quirks.Contains("drunk") && a.Saloon)
                    Add(t, "tipsy", 1);
                if (p.Family != null && p.Family.StartsWith("widow"))
                    Add(t, "lonely", 1);
                if (p.Facet("Humour") >= 60)
                    Add(t, "playful", 1);
            }
            switch (a.Worry)
            {
                case "debt": case "savings": case "bank": case "cards": Add(t, flush ? "money" : "troubled", flush ? 1 : -1); break;   // money on their mind
                case "feud": Add(t, "troubled", -1); break;
                case "secret": case "law": Add(t, "wary", -1); break;   // keeping their head down
                case "letter": Add(t, "elsewhere", -1); break;           // waiting on someone far off
            }
            return t;
        }

        /// For the log: the regard, then each thing that weighed, and their day.
        public static string Explain(Ask a)
        {
            var parts = new List<string> { a.Regard };
            parts.AddRange(Taste(a).Select(t => t.Key + (t.Value > 0 ? " +" : " ") + t.Value));
            if (a.Mood != null) parts.Add(a.Mood + " today");
            if (a.AtWorst) parts.Add("a worry at its worst");
            if (a.Taken) parts.Add("spoken for");
            return string.Join(", ", parts);
        }

        private static readonly HashSet<string> Hard = new HashSet<string> { "spam", "notdrawn", "scorned", "hate", "fear", "badrep", "married", "busy" };

        /// A no that came from weighing them up (cold, sore, frightened...), not one that was never in question.
        public static bool Soft(string why)
        {
            return !Hard.Contains(why ?? "");
        }

        private static void Add(List<KeyValuePair<string, int>> t, string why, int by)
        {
            t.Add(new KeyValuePair<string, int>(why, by));
        }

        private static string No(string reason, out string why)
        {
            why = reason;
            return "no";
        }

        private static int Warmth(string regard)
        {
            switch (regard)
            {
                case "grateful":
                case "fond": return 3;
                case "met":
                case "stranger": return 1;
                default: return 0;   // wary
            }
        }
    }
}
