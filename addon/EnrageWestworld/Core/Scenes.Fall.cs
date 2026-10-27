// A fall and a helping hand (roadmap 21, small kindnesses; overnight 2026-10-27, Tier 2.3): someone walking trips and
// goes down; the one nearest who saw it helps them up, or doesn't. Whether they do comes from what's between them and
// who they are: a friend always, an enemy never (and may say so), the kind stop, the cold walk on, the sharp help
// gruffly or walk on. B is the one who fell, A the one who saw. All lines are original to this add-on.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static partial class Scenes
    {
        public static readonly string[] FallAnswers = { "help", "gruff", "pass", "sneer" };

        /// Whether A helps B up: a warm tie always, a hostile one never (sneering at a Hothead's or Proud man's
        /// worst); else by A's manner (Exchanges.Manner): kind help, cold pass, sharp help gruffly or pass.
        public static string FallAnswer(int? warmth, string manner, string temper, string seed)
        {
            if (warmth > 0)
                return manner == "sharp" ? "gruff" : "help";
            if (warmth < 0)
                return temper == "Hothead" || temper == "Proud" || temper == "Cold" ? "sneer" : "pass";
            switch (manner)
            {
                case "kind": return "help";
                case "cold": return Voice.Chance(seed + "|cold", 20) ? "help" : "pass";   // even the cold stop now and then
                default: return Voice.Chance(seed + "|sharp", 60) ? "gruff" : "pass";
            }
        }

        /// The fall and what follows. old: no tumble, they sit down hard instead.
        public static Scene Fall(SceneCast c, string seed, string answer, bool old)
        {
            string a = N(c, 0, seed), b = N(c, 1, seed);
            bool help = answer == "help" || answer == "gruff";
            Scene s = new Scene
            {
                Kind = help ? "help" : "passby",
                Summary = help ? "someone took a fall and was helped up" : "someone took a fall and was left to get up alone"
            };
            if (!old)
                s.Add(1, null, null, "fall");
            s.Add(1, old ? Voice.Pick(seed + "fo", "Easy, old bones. Easy.", "Down I go. Like a sack of meal.", "Oh, my hip. Oh, my hip.")
                         : Voice.Pick(seed + "fy", "Ow! Blast it.", "Oh! My ankle.", "Of all the clumsy...", "Ow. That's my knee, that is.", "Whoa, whoa... oof."), "scared", "sitdown");
            switch (answer)
            {
                case "help":
                    s.Add(0, Voice.Pick(seed + "hm", "Easy there, " + b + ". I've got you.", "Hold on, " + b + ". Don't try to stand yet.", b + "! Are you hurt?"), "mild", "meet")
                     .Add(0, null, null, "kneel")
                     .Add(0, Voice.Pick(seed + "hk", "Anything broken?", "Let me see that ankle.", "Can you move it? Slowly."), "mild")
                     .Add(1, Voice.Pick(seed + "hb", "Just my pride, I think.", "I'll live. Hurts like the devil.", "Nothing broken. Just shook."), "mild")
                     .Add(0, null, null, "stand")
                     .Add(1, null, null, "getup")
                     .Add(1, Voice.Pick(seed + "ht", "Thank you kindly, " + a + ".", "Obliged to you, " + a + ". Truly.", "Bless you, " + a + "."), "thanks")
                     .Add(0, Voice.Pick(seed + "hw", "Mind your feet, now.", "Take it slow the rest of the way.", "Get that looked at."), "mild", "walkaway");
                    break;
                case "gruff":
                    s.Add(0, Voice.Pick(seed + "gm", "Up you get, " + b + ". Come on.", "On your feet, " + b + ". You're in the way."), "mild", "meet")
                     .Add(1, null, null, "getup")
                     .Add(0, Voice.Pick(seed + "gw", "Watch your feet next time.", "Lord, the whole street saw that."), "mild")
                     .Add(1, Voice.Pick(seed + "gt", "...Thanks.", "Didn't need the lecture. But thanks."), "thanks")
                     .Add(0, null, null, "walkaway");
                    break;
                case "sneer":
                    s.Add(0, null, null, "face", 900)
                     .Add(0, Voice.Pick(seed + "sm", "Serves you right, " + b + ".", "Mind you don't dent the road, " + b + "."), "mock", "walkaway")
                     .Add(1, null, null, "getup")
                     .Add(1, Voice.Pick(seed + "sb", a + ". Of course it was " + a + ".", "Laugh, then. I'll remember it."), "angry");
                    break;
                default:   // pass
                    s.Add(0, null, null, "face", 1200)
                     .Add(0, null, null, "walkaway", 2500)
                     .Add(1, Voice.Pick(seed + "pb", "Nobody, then. Fine.", "Don't all rush at once."), "mild", "getup")
                     .Add(1, Voice.Pick(seed + "pt", "Thank you for nothing.", "Walked right past me."), "angry");
                    break;
            }
            return s;
        }

        /// What the one who fell says to the player when he gives them a hand up (said to him, subtitled).
        public static string FallThanksYou(bool nobodyElse, string seed)
        {
            return nobodyElse ? Voice.Pick(seed + "ty", "Thank you, mister. Nobody else stopped.", "Obliged. Most folks just walk on by.", "Kind of you. Kinder than some.")
                              : Voice.Pick(seed + "ty", "Thank you, mister. I'm all right now.", "Obliged to you. Just a tumble.", "Kind of you. I'll be fine.");
        }

        /// Every fall scene, for the tests.
        public static IEnumerable<Scene> FallSamples(SceneCast c)
        {
            for (int i = 0; i < 12; i++)
                foreach (string ans in FallAnswers)
                    foreach (bool old in new[] { false, true })
                        yield return Fall(c, "f" + i, ans, old);
        }
    }
}
