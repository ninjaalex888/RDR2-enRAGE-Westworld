// A round bought (roadmap 21, small kindnesses; overnight 2026-10-27, Tier 2.5): of an evening in a saloon, someone with
// cause to be glad (a worry that ended well today, a glad mood, or an outgoing man with money) stands the house a drink;
// those at the bar cheer, one of them thanks him, and if the player is at the bar he's stood one too. A is the one
// buying, B the one who thanks him. All lines are original to this add-on.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static partial class Scenes
    {
        public static readonly string[] RoundWhys = { "wedding", "goodnews", "glad", "flush" };   // a wedding toast first (Tier 4: an enRAGE wedding in this town today)

        /// Why this person would buy the house a drink tonight, or null: good news today first, then a glad mood, then
        /// an outgoing nature and money in the pocket (never the Greedy, who'd sooner keep it).
        /// The same, for a woman: a respectable woman didn't stand the house a round; the saloon's own women might (item 10).
        public static string RoundWhy(bool goodNewsToday, string mood, bool outgoing, bool wellOff, string temper, bool woman, bool saloonWoman)
        {
            if (woman && !saloonWoman)
                return null;
            return RoundWhy(goodNewsToday, mood, outgoing, wellOff, temper);
        }

        public static string RoundWhy(bool goodNewsToday, string mood, bool outgoing, bool wellOff, string temper)
        {
            if (temper == "Greedy")
                return null;
            if (goodNewsToday)
                return "goodnews";
            if (mood == "glad")
                return "glad";
            return outgoing && wellOff ? "flush" : null;
        }

        public static Scene Round(SceneCast c, string seed, string why, string couple = null)
        {
            string a = N(c, 0, seed);
            string pair = string.IsNullOrEmpty(couple) ? "the newlyweds" : couple;
            Scene s = new Scene { Kind = "round", Summary = why == "wedding" ? "someone stood the house a round for the newlyweds" : "someone stood the house a round" };
            string call = why == "wedding" ? Voice.Pick(seed + "rw", "A round for " + pair + "! Married this very day!", "Drinks all round! To " + pair + ", and long life to them!", "Fill 'em up! I mean to drink to " + pair + ", wed this very day.", "Glasses up! " + LostChild.Up(pair) + " are wed, and the whiskey's on me.")
                        : why == "goodnews" ? Voice.Pick(seed + "rg", "A round for the house! Good news came today, and I mean to drink to it.", "Drinks for everybody! Something finally went right.")
                        : why == "glad" ? Voice.Pick(seed + "rl", "Set 'em up, barkeep. One for everybody.", "Drinks all round! Don't ask why. Just drink.", "Everybody's glass, on me. It's been that kind of a day.")
                        : Voice.Pick(seed + "rf", "Put the room's drinks on my account tonight.", "A round for the house, and keep the change.");
            s.Add(0, call, "cheer", "face")
             .Add(0, null, null, "cb:round")
             .Add(1, why == "wedding" ? Voice.Pick(seed + "rx", "To " + pair + "! And to you, " + a + ", for the whiskey.", "Long life to them. Thank you, " + a + ".", "To the two of them. And to you, " + a + ", for once in your life.")
                                     : Voice.Pick(seed + "rt", "Well, I'll drink to that. Thank you kindly, " + a + ".", "Generous of you, " + a + ". Your health.",
                                a + ", you're " + (c.AWoman ? "a lady" : "a gentleman") + ". To you."), "thanks", "face")
             .Add(0, why == "wedding" ? Voice.Pick(seed + "ry", "May they put up with each other forty years.", "Here's to a full table and a quiet house.", "And may the first one be a boy who minds his mother.")
                                     : Voice.Pick(seed + "rr", "Drink up before I come to my senses.", "To better days. All of us.", "Don't thank me. Thank the Lord, for once."), "mild");
            return s;
        }

        /// What the one buying says to the player at the bar (said to him, subtitled).
        public static string RoundToYou(string seed)
        {
            return Voice.Pick(seed + "ry", "You too, mister. Drink up.", "And one for the stranger at the bar.", "That means you as well, friend.");
        }

        /// Every round, for the tests.
        public static IEnumerable<Scene> RoundSamples(SceneCast c)
        {
            for (int i = 0; i < 12; i++)
                foreach (string why in RoundWhys)
                {
                    yield return Round(c, "r" + i, why);
                    if (why == "wedding")
                        yield return Round(c, "r" + i, why, "Ruth and Eli");
                }
        }
    }
}
