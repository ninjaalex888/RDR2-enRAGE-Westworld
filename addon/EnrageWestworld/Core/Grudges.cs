// Grudges that travel (roadmap 20; overnight 2026-10-27, Tier 2.7): a wrong done to someone is taken up by their
// friends. Between people: C, a friend of B, comes upon A, who wronged B in the last three days (a face into the bar, a
// hand in a pocket, a fistfight, a scorn, walking past while B lay in the street, facing B down), and has it out with
// him. How A takes it is his manner: the sharp argue (two hotheaded men come to blows), the cold walk off, the kind say
// they're sorry and C cools. With the player: someone whose friend he killed comes up to him in town and says so to his
// face (the hot and the proud), or only stares and goes (the rest); nothing violent from our side. All lines are
// original to this add-on.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class Grudges
    {
        /// The wrongs a friend would take up, with how C names them to A's face (A did it to {b}).
        public static readonly Dictionary<string, string[]> Wrongs = new Dictionary<string, string[]>
        {
            { "rough", new[] { "You put {b}'s face through the bar. I heard all about it.", "{b} can't chew for the teeth you loosened." } },
            { "pickpocket", new[] { "Hand in {b}'s coat, they say. In broad day.", "You robbed {b}. Everybody knows it." } },
            { "brawl", new[] { "You beat on {b}. Thought nobody'd mind?", "{b}'s still got your knuckles on his face." } },
            { "scorn", new[] { "{b} asked you for a nickel and you treated them like a dog.", "Heard how you sent {b} off. Proud of that?" } },
            { "passby", new[] { "{b} was lying in the street and you stepped round them.", "You walked past {b} like a dropped hat." } },
            { "faced", new[] { "Made a show of {b} in the street, did you?", "You shamed {b} in front of the whole street." } },
        };

        public static bool Wrong(string tieKind)
        {
            return tieKind != null && Wrongs.ContainsKey(tieKind);
        }

        /// The answer, by the wrongdoer's manner (Exchanges.Manner): sharp, cold or kind.
        public static readonly string[] Answers = { "sharp", "cold", "kind" };

        /// C (A in the scene, the friend) has it out with the wrongdoer (B in the scene) over what was done to `victim`.
        /// sharp: words, and blows when both are hot-tempered men (kind "brawl", else "argue"); cold: he walks off
        /// ("brushoff"); kind: he says he's sorry and the friend cools ("apology").
        public static Scene Confront(SceneCast c, string seed, string wrong, string victim, string manner, bool blows)
        {
            string a = Scenes.N(c, 0, seed), b = Scenes.N(c, 1, seed);
            string[] said;
            if (!Wrongs.TryGetValue(wrong, out said))
                said = Wrongs["faced"];
            string opener = Voice.Pick(seed + "go", said).Replace("{b}", victim);
            Scene s = new Scene { Kind = "argue", Summary = "someone had it out with a man over what he did to their friend" };
            s.Add(0, b + ". A word.", "angry", "face")
             .Add(0, opener, "angry");
            switch (manner)
            {
                case "sharp":
                    s.Add(1, Voice.Pick(seed + "gs", "None of yours, is it?", victim + " had it coming. So do you, keep talking.", "Mind your own business, " + a + "."), "angry");
                    if (blows)
                    {
                        s.Kind = "brawl";
                        s.Fight = true;
                        s.Add(0, Voice.Pick(seed + "gf", "This is my business.", "That's what I hoped you'd say."), "angry", "fight", 9000)
                         .Add(0, null, null, "endfight")
                         .Add(0, Voice.Pick(seed + "gw", "That's for " + victim + ".", "Stay away from " + victim + "."), "won")
                         .Add(1, Voice.Pick(seed + "gl", "This ain't over.", "...Alright. Alright."), "mild", "walkaway");
                    }
                    else
                        s.Add(0, Voice.Pick(seed + "ga", "Go near " + victim + " again and see what it is to me.", "Everyone in this town will know what you are."), "threat")
                         .Add(1, null, null, "walkaway");
                    break;
                case "cold":
                    s.Kind = "brushoff";
                    s.Add(1, Voice.Pick(seed + "gc", "Don't know what you heard. Don't care.", "Talk to somebody who's listening."), "mild", "walkaway")
                     .Add(0, Voice.Pick(seed + "gx", "That's right. Walk.", "Coward."), "angry");
                    break;
                default:
                    s.Kind = "apology";
                    s.Add(1, Voice.Pick(seed + "gk", "...You're right. I'm not proud of it.", "I was wrong. Tell " + victim + " I said so.", "Wasn't myself that day. I'm sorry for it."), "sorry")
                     .Add(0, Voice.Pick(seed + "gy", "Tell " + victim + " yourself.", "Well. See you do better.", "...Alright. Alright, then."), "mild");
                    break;
            }
            return s;
        }

        /// What someone whose friend the player killed says to his face (subtitled), by temper.
        public static string ToYourFace(string temper, string dead, string seed)
        {
            string[] pool = temper == "Hothead"
                ? new[] { "You killed " + dead + ". Don't pretend you didn't.", "You put " + dead + " in the ground. One day, mister. One day.", "I know your face. I know what you did to " + dead + "." }
                : new[] { "I called " + dead + " friend. I won't forget it, and neither will this town.", "You're the one who killed " + dead + ". Thought you should hear it said.", "Look at me. " + dead + " had a family. Look at me." };
            return Voice.Pick(seed + "yf", pool);
        }

        /// What the rest think, staring at him from a distance.
        public static string Stare(string temper, string dead, string seed)
        {
            string[] pool = temper == "Coward"
                ? new[] { "That's him. That's who killed " + dead + ". Keep walking.", "Don't let him see you looking. Not after " + dead + "." }
                : new[] { "There he goes. " + dead + "'s blood on his hands, and walking about free.", "Killed " + dead + ", and buys his tobacco same as anyone." };
            return Voice.Pick(seed + "ys", pool);
        }

        /// Every confrontation, for the tests.
        public static IEnumerable<Scene> Samples(SceneCast c)
        {
            for (int i = 0; i < 8; i++)
                foreach (string wrong in Wrongs.Keys)
                    foreach (string m in Answers)
                        foreach (bool blows in new[] { false, true })
                            yield return Confront(c, "g" + i, wrong, "Bill", m, blows);
        }

        public static IEnumerable<string> Lines()
        {
            for (int i = 0; i < 6; i++)
                foreach (string t in new[] { "Hothead", "Proud", "Coward", null })
                {
                    yield return ToYourFace(t, "Ezra", "f" + i);
                    yield return Stare(t, "Ezra", "s" + i);
                }
        }
    }
}
