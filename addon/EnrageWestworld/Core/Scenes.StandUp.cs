// Standing up to a bully (roadmap 21, small kindnesses; overnight 2026-10-27, Tier 2.4): in a rough or a brawl, someone
// watching steps in before the blow lands. A friend of the one being picked on, or a steady man (Loyal or Proud, never a
// Coward) who can't abide it. The bully backs off (the scene ends early, as words) or tells him to keep out and goes on.
// C is the one who steps in, A the bully, B the one he picked on. Whether he backs off comes from A's temper and C's.
// (Built tonight: backs off, ignores him. Not built: the bully turning on the one who stepped in, which would need the
// Director to recast a fight already going; OVERNIGHT.md.) Night 2026-10-29: with the mind deciding, a bully who holds his
// ground may see the one who stepped in take the other away instead (TakesAway). All lines are original to this add-on.
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
            // a friend 80% and a steady stranger 50% since 2026-10-28 (Lex: the bully often; were 70 and 35)
            if (warmthToB > 0)
                return Voice.Chance(seed + "|friend", 80);
            return !woman && (temper == "Loyal" || temper == "Proud") && Voice.Chance(seed + "|steady", 50);
        }

        /// Whether the bully backs off: a Hothead seldom does, the cold and the proud half the time, most men do.
        public static bool BacksOff(string bullyTemper, string standerTemper, bool standerWoman, string seed)
        {
            int p = bullyTemper == "Hothead" ? 40 : bullyTemper == "Proud" || bullyTemper == "Cold" ? 55 : 75;
            if (!standerWoman && (standerTemper == "Proud" || standerTemper == "Hothead"))
                p += 10;   // a man who looks like he means it
            return Voice.Chance(seed + "|backs", p);
        }

        /// Puts the one who steps in into a rough or a brawl, just before the blow: their word, and the answer. In a rough
        /// A is the bully and B the one picked on; a brawl can be begun by either, so there they break it up between the
        /// two. far: C walks over first. backsOff: the bully goes (in a brawl, A gives it up), B thanks C to C's face, and
        /// the rest of the scene is dropped (it was words in the end: kind "argue", noted when C speaks up). Else the
        /// bully waves them off and the scene goes on as it was. False if there's no blow to stand between.
        /// Whether the one who stepped in takes the one picked on away when the bully holds his ground (night 2026-10-29,
        /// after item 6f; the mind's path only): a friend of B six times in ten, a stranger a little over a third (seeded).
        public static bool TakesAway(bool friendOfB, string seed)
        {
            return Voice.Chance(seed + "|takeaway", friendOfB ? 60 : 35);
        }

        public static bool StandUp(Scene s, SceneCast c, string seed, bool backsOff, bool far = false, bool takeAway = false, bool? cKnowsB = null)
        {
            int at = s.Beats.FindIndex(x => x.Act == "slam" || x.Act == "fight");
            if (at < 0)
                return false;
            bool rough = s.Kind == "rough";
            string a = N(c, 0, seed), cn = N(c, 2, seed);
            // B as A knows him (Knows[1], from the cast's ties), and as C does when the game says (cKnowsB, C's own tie; null:
            // as A), spoken to (a stranger "mister" or "ma'am") or spoken of to a third (a stranger "the man" or "the lady")
            string real = c.Names != null && c.Names.Length > 1 ? c.Names[1] : null;
            string bName = !string.IsNullOrEmpty(real) ? real : FirstName(c.BWoman, seed + 1);
            string bToA = N(c, 1, seed), bOfA = NThird(c, 1, seed);
            string bToC = cKnowsB == null ? bToA : cKnowsB.Value ? bName : c.BWoman ? "ma'am" : "mister";
            string bOfC = cKnowsB == null ? bOfA : cKnowsB.Value ? bName : c.BWoman ? "the lady" : "the man";
            var add = new List<Beat>();
            if (far)
                add.Add(new Beat(2, null, null, "approach"));
            add.Add(new Beat(2, null, null, "cb:stoodup"));
            // a woman who steps in shames them; she doesn't offer to fight (item 10)
            add.Add(new Beat(2, c.CWoman ? (rough ? Voice.Pick(seed + "su", "Shame on you, " + a + ". Leave " + bOfC + " be.", a + "! For shame. Let " + bOfC + " alone.", "Not in the street, " + a + ". Not before everyone.")
                                                   : Voice.Pick(seed + "sx", "Shame on the both of you!", "Not here, " + a + ". Not today. " + LostChild.Up(bToC) + ", you too.", "For shame. The pair of you, in the open street."))
                                         : (rough ? Voice.Pick(seed + "su", "That's enough, " + a + ". Leave " + bOfC + " be.", a + "! Leave " + bOfC + " alone.", "Not here, " + a + ". Not while I'm standing here.",
                                                   "Leave off " + bOfC + ", or deal with me.")
                                                  : Voice.Pick(seed + "sx", "That's enough! The both of you!", "Not here, " + a + ". Not today. " + LostChild.Up(bToC) + ", you too.", "Put your hands down, the pair of you.")),
                             "intervene", "face"));
            if (backsOff)
            {
                add.Add(new Beat(0, c.CWoman ? Voice.Pick(seed + "sb", "...Fine. Not in front of a lady.", "This ain't over, " + bToA + ". Beg your pardon, ma'am.", "...Fine. " + LostChild.Up(bOfA) + " ain't worth it.")
                                          : rough ? Voice.Pick(seed + "sb", "...Fine. " + LostChild.Up(bOfA) + " ain't worth it.", "This ain't over, " + bToA + ".", "Keep your nose out, " + cn + ". ...Fine.")
                                          : Voice.Pick(seed + "sy", "...Fine. Not worth the skinned knuckles.", "Another time, " + bToA + ".", "You're lucky " + NThird(c, 2, seed) + " came by."), "storm", "walkaway"));
                add.Add(new Beat(1, null, null, "faceone") { To = 2 });
                add.Add(new Beat(1, rough ? Voice.Pick(seed + "st", "Thank you, " + cn + ". I owe you.", cn + ". Thank God.", "Obliged, " + cn + ". He'd have done it.")
                                          : Voice.Pick(seed + "sz", "Obliged, " + cn + ". " + (c.AWoman ? "She" : "He") + " had it coming, but obliged.", "Thank you, " + cn + ". I'd have lost my temper."), "thanks") { To = 2 });
                s.Beats.RemoveRange(at, s.Beats.Count - at);
                s.Beats.AddRange(add);
                s.Kind = "argue";   // it never came to blows
                s.NoteAtCue = true; // noted when they speak up (a walk-over that never arrives leaves nothing)
                s.Summary = rough ? "someone stepped in and the bully backed off" : "someone broke up a fight before it started";
            }
            else if (takeAway)
            {
                // the bully holds his ground, and the one who stepped in takes the one picked on away instead: the fight never
                // comes (night 2026-10-29: before, he was waved off and the fight went on as if he had said nothing)
                add.Add(new Beat(0, c.CWoman ? Voice.Pick(seed + "si", "This is no business for a lady, " + cn + ".", "Go on home, " + cn + ". This ain't for you to see.")
                                          : rough ? Voice.Pick(seed + "si", "Keep walking, " + cn + ".", "Ain't your quarrel, " + cn + ". Never was.", "Mind your own, " + cn + ", or you're next.")
                                          : Voice.Pick(seed + "sw", "Step aside, " + cn + ".", "This is ours to settle, " + cn + "."), "angry", "face") { To = 2 });   // said to the one who stepped in
                // C speaks to B by C's own knowing (bToC: a friend by name, a stranger mister or ma'am), A by his (bToA)
                add.Add(new Beat(2, null, null, "faceone") { To = 1 });
                add.Add(new Beat(2, c.CWoman ? Voice.Pick(seed + "ta", "Come with me, " + bToC + ". Now. Don't look back at " + (c.AWoman ? "her" : "him") + ".", "Leave it, " + bToC + ". Walk with me.", bToC + ", come away. " + (c.AWoman ? "She" : "He") + " isn't worth one more word.")
                                          : Voice.Pick(seed + "ta", "Come on, " + bToC + ". " + (c.AWoman ? "She" : "He") + " ain't worth it.", "Walk away, " + bToC + ". Walk with me.", "Leave it, " + bToC + ". We're going."), "mild") { To = 1 });
                // the two leave together, away from A (the Director's "leavewith": both walk to one spot past C, side by side)
                add.Add(new Beat(2, null, null, "leavewith") { To = 0 });
                add.Add(new Beat(1, null, null, "leavewith") { To = 0 });
                string hide = c.CWoman ? "Go on, hide behind her skirts." : cKnowsB == true ? "Go on, then. Hide behind your friend." : "Go on, then. Let him lead you off.";
                add.Add(new Beat(0, rough ? Voice.Pick(seed + "tj", "That's right. Run along, the pair of you.", hide, "Next time, " + bToA + ". Next time.")
                                          : Voice.Pick(seed + "tj", "Go on, then. Run.", "That's right, " + bToA + ". Walk away."), "mock") { To = 1 });
                s.Beats.RemoveRange(at, s.Beats.Count - at);
                s.Beats.AddRange(add);
                s.Kind = "argue";   // it never came to blows
                s.NoteAtCue = true;
                s.Summary = rough ? "someone stepped in and took the one picked on away" : "someone stepped in and took one of them away before it came to blows";
                s.Cast = 3;
                return true;
            }
            else
            {
                add.Add(new Beat(0, c.CWoman ? Voice.Pick(seed + "si", "This is no business for a lady, " + cn + ".", "Go on home, " + cn + ". This ain't for you to see.")
                                          : rough ? Voice.Pick(seed + "si", "Keep walking, " + cn + ".", "Ain't your quarrel, " + cn + ". Never was.", "Mind your own, " + cn + ", or you're next.")
                                          : Voice.Pick(seed + "sw", "Step aside, " + cn + ".", "This is ours to settle, " + cn + "."), "angry", "face"));
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
