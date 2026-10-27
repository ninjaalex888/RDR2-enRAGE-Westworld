// Vices and habits (roadmap 19; overnight 2026-10-27, Tier 2.6, the first: the drinker after bad news). Someone in a
// saloon whose worry ended badly today, or who has just had word of a friend's death, drinks the evening away: the
// drinker's stance, a drunk's voice, what they think as they drink, and a friend beside them may say they're sorry.
// They're drunk for the rest of the night, which the other scenes take as it comes (a drunk's challenge, a pocket
// picked). The game side does the drinking; this says why and what. All lines are original to this add-on.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class Vices
    {
        /// Why this person would drink tonight, or null: grief for a friend first (by name; it takes anyone), then a
        /// worry that ended badly today (its kind; not the Coward, who goes home and hides instead).
        public static string DrinkWhy(string griefFor, bool worryEndedBadly, string worryKind, string temper)
        {
            if (griefFor != null)
                return "grief:" + griefFor;
            if (worryEndedBadly && temper != "Coward")
                return "badnews:" + (worryKind ?? "trouble");
            return null;
        }

        /// What they think with a glass in front of them (by why and temper), first name of the dead filled in.
        public static string Thought(string why, string temper, string seed)
        {
            string[] pool;
            string g = Minds.TemperGroup(temper), who = null;
            if (why != null && why.StartsWith("grief:"))
            {
                who = why.Substring(6);
                pool = g == "bold" ? GriefBold : g == "cold" ? GriefCold : Grief;
            }
            else
                pool = g == "bold" ? BadNewsBold : g == "cold" ? BadNewsCold : BadNews;
            return pool[Voice.Hash(seed + "|vice") % pool.Length].Replace("{who}", who ?? "them");
        }

        /// A friend beside them says they're sorry (the Director's scene: A the drinker, B the friend). Kind "comfort".
        public static Scene Comfort(SceneCast c, string seed, string why)
        {
            string a = Scenes.N(c, 0, seed);
            bool grief = why != null && why.StartsWith("grief:");
            string who = grief ? why.Substring(6) : null;
            Scene s = new Scene { Kind = "comfort", Summary = "a friend sat with someone drinking away bad news" };
            s.Add(1, grief ? Voice.Pick(seed + "cg", "Heard about " + who + ", " + a + ". I'm sorry.", "Sorry about " + who + ". Truly.", a + ". I heard about " + who + ". Sit, I'll stand you the next.")
                           : Voice.Pick(seed + "cb", "Sorry to hear it, " + a + ".", "Heard how it went, " + a + ". Rotten luck.", "Bad day, " + a + "? I heard."), "sorry", "face")
             .Add(0, grief ? Voice.Pick(seed + "ca", "...Yeah. Thanks.", "Can't believe it. Can't.", "Don't. Just drink with me.")
                           : Voice.Pick(seed + "cr", "...Yeah. Thanks.", "Don't want to talk about it.", "Pour me another and we'll call it even."), "drunk")
             .Add(1, Voice.Pick(seed + "cw", "Go easy, now.", "I'll see you home after.", "Here's to better days."), "mild");
            return s;
        }

        private static readonly string[] Grief = { "Another. For {who}. And one after that.", "{who}'s gone. Whiskey doesn't help. Pour it anyway.",
                                                   "Keep seeing {who}'s face. Glass is empty again.", "Should've said something to {who}. Last time. Should've." };
        private static readonly string[] GriefBold = { "Somebody'll pay for {who}. After this glass.", "Drink to {who}. Then find out who." };
        private static readonly string[] GriefCold = { "{who}. Well. Everybody goes.", "Don't feel it yet. Maybe the whiskey will." };
        private static readonly string[] BadNews = { "Pour. Don't talk. Pour.", "Drink till it's somebody else's trouble.", "Gone wrong. All of it. Another.",
                                                     "Face hot. Hands cold. Glass empty." };
        private static readonly string[] BadNewsBold = { "Somebody's going to hear about this. Not tonight.", "Drink first. Break something after." };
        private static readonly string[] BadNewsCold = { "Lost that one. Drink, then think.", "Numbers don't lie. Whiskey does. Better company." };

        public static IEnumerable<string> AllLines()
        {
            return Grief.Concat(GriefBold).Concat(GriefCold).Concat(BadNews).Concat(BadNewsBold).Concat(BadNewsCold);
        }
    }
}
