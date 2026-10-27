// Standing up to a bully (roadmap 21, small kindnesses; overnight 2026-10-27, Tier 2.4): in a rough or a brawl, someone
// watching steps in before the blow lands. A friend of the one being picked on, or a steady man (Loyal or Proud, never a
// Coward) who can't abide it. The bully backs off (the scene ends early, as words) or tells him to keep out and goes on.
// C is the one who steps in, A the bully, B the one he picked on. Whether he backs off comes from A's temper and C's.
// (Built tonight: backs off, ignores him. Not built: the bully turning on the one who stepped in, which would need the
// Director to recast a fight already going; OVERNIGHT.md.) All lines are original to this add-on.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static partial class Scenes
    {
        /// Whether this watcher steps in for B against A: a friend of B (and no friend of A) mostly does; a steady man
        /// (Loyal or Proud) who isn't a friend of A now and then; never a Coward, never an enemy of B.
        public static bool StandsUp(int warmthToB, int warmthToA, string temper, bool woman, string seed)
        {
            if (temper == "Coward" || warmthToB < 0 || warmthToA > 0)
                return false;
            if (warmthToB > 0)
                return Voice.Chance(seed + "|friend", 70);
            return !woman && (temper == "Loyal" || temper == "Proud") && Voice.Chance(seed + "|steady", 35);
        }

        /// Whether the bully backs off: a Hothead seldom does, the cold and the proud half the time, most men do.
        public static bool BacksOff(string bullyTemper, string standerTemper, bool standerWoman, string seed)
        {
            int p = bullyTemper == "Hothead" ? 40 : bullyTemper == "Proud" || bullyTemper == "Cold" ? 55 : 75;
            if (!standerWoman && (standerTemper == "Proud" || standerTemper == "Hothead"))
                p += 10;   // a man who looks like he means it
            return Voice.Chance(seed + "|backs", p);
        }

        /// Puts the one who steps in into a rough or a brawl, just before the blow: their word, and the bully's answer.
        /// backsOff: the bully goes, B thanks them, and the rest of the scene is dropped (it was words in the end:
        /// kind "argue"). Else the bully waves them off and the scene goes on as it was. False if there's no blow to
        /// stand between.
        public static bool StandUp(Scene s, SceneCast c, string seed, bool backsOff)
        {
            int at = s.Beats.FindIndex(x => x.Act == "slam" || x.Act == "fight");
            if (at < 0)
                return false;
            string a = N(c, 0, seed), b = N(c, 1, seed), cn = N(c, 2, seed);
            var add = new List<Beat>
            {
                new Beat(2, null, null, "cb:stoodup"),
                new Beat(2, Voice.Pick(seed + "su", "That's enough, " + a + ". Leave " + b + " be.", a + "! Let go of " + b + ". Now.", "Not here, " + a + ". Not while I'm standing here.",
                                       "Touch " + b + " again and you'll answer to me."), "intervene", "face")
            };
            if (backsOff)
            {
                add.Add(new Beat(0, Voice.Pick(seed + "sb", "...Fine. " + b + " ain't worth it.", "This ain't over, " + b + ".", "Keep your nose out, " + cn + ". ...Fine."), "storm", "walkaway"));
                add.Add(new Beat(1, Voice.Pick(seed + "st", "Thank you, " + cn + ". I owe you.", cn + ". Thank God.", "Obliged, " + cn + ". He'd have done it."), "thanks"));
                s.Beats.RemoveRange(at, s.Beats.Count - at);
                s.Beats.AddRange(add);
                s.Kind = "argue";   // it never came to blows
                s.Summary = "someone stepped in and the bully backed off";
            }
            else
            {
                add.Add(new Beat(0, Voice.Pick(seed + "si", "Keep walking, " + cn + ".", "Ain't your quarrel, " + cn + ". Never was.", "Mind your own, " + cn + ", or you're next."), "angry", "face"));
                s.Beats.InsertRange(at, add);
                s.Summary += "; someone tried to step in";
            }
            s.Cast = 3;
            return true;
        }

        /// Every stand-up, for the tests: both kinds of trouble and both outcomes.
        public static IEnumerable<Scene> StandUpSamples(SceneCast c)
        {
            for (int i = 0; i < 12; i++)
                foreach (bool backs in new[] { true, false })
                {
                    Scene r = Rough(c, "su" + i, i % 2 == 0, i % 3 == 0 ? "sharp" : i % 3 == 1 ? "cold" : "kind");
                    if (StandUp(r, c, "su" + i, backs))
                        yield return r;
                    Scene f = Make("brawl", c, "sb" + i);
                    if (f != null && StandUp(f, c, "sb" + i, backs))
                        yield return f;
                }
        }
    }
}
