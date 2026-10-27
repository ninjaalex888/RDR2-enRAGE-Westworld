// Reactions to a sight (roadmap 17d; overnight 2026-10-27, Tier 2.2): people notice how someone looks. The player
// bloodied (hurt lately and not washed since), filthy, drunk or soaked; another man walking off bloodied from a street
// fight. What they make of it comes from their temper: the timid are afraid, the hard size him up, the cold look past
// him, the plain are concerned or amused. The feeling picks the look, the gesture, whether a word is said in their
// own recorded voice (only where the feeling agrees with it), and the thought. All lines are original to this add-on.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class Sights
    {
        public static readonly string[] States = { "bloodied", "drunk", "filthy", "soaked" };   // in order of what's noticed first

        public sealed class Look
        {
            public string State, Feeling;
            public string Gesture;    // surprised, headshake, spit, dismiss, none
            public string[] Voice;    // line names to try, or null (the feeling has no words for it)
            public int LookMs;
            public string Thought;
        }

        /// What someone makes of the sight, by their temper (Minds.TemperGroup) and a seed for the plain.
        public static string Feeling(string state, string temper, string seed)
        {
            string g = Minds.TemperGroup(temper);
            bool coin = Voice.Hash(seed + "|feel") % 2 == 0;
            switch (state)
            {
                case "bloodied":
                    return g == "fearful" ? "afraid" : g == "bold" ? "sizing" : g == "cold" ? "cold" : "concerned";
                case "filthy":
                    return g == "cold" || g == "fearful" ? "disgust" : g == "bold" ? "amused" : coin ? "pity" : "amused";
                case "drunk":
                    return g == "fearful" ? "afraid" : g == "cold" ? "disgust" : g == "bold" ? "amused" : coin ? "disgust" : "amused";
                default:   // soaked
                    return g == "fearful" ? "concerned" : g == "bold" || g == "cold" ? "amused" : coin ? "concerned" : "amused";
            }
        }

        /// The whole reaction to the player.
        public static Look For(string state, string temper, string seed)
        {
            string feel = Feeling(state, temper, seed);
            var l = new Look { State = state, Feeling = feel };
            int r = Voice.Hash(seed + "|g") % 100;
            switch (feel)
            {
                case "afraid":
                    l.Gesture = "surprised";
                    l.LookMs = 1200;   // a glance, then eyes down
                    break;
                case "sizing":
                    l.Gesture = "none";
                    l.LookMs = 3000;
                    l.Voice = r < 50 ? new[] { "GREET_BLOODY" } : null;
                    break;
                case "cold":
                    l.Gesture = r < 40 ? "dismiss" : "none";
                    l.LookMs = 1500;
                    break;
                case "concerned":
                    l.Gesture = "surprised";
                    l.LookMs = 2500;
                    l.Voice = state == "bloodied" && r < 60 ? new[] { "GREET_BLOODY" } : null;
                    break;
                case "disgust":
                    l.Gesture = Passing.Spitter(temper) && r < 30 ? "spit" : "headshake";
                    l.LookMs = 2000;
                    l.Voice = r < 50 ? new[] { "VOCAL_FX_DISAPPROVE" } : null;
                    break;
                case "pity":
                    l.Gesture = "headshake";
                    l.LookMs = 2200;
                    break;
                default:   // amused
                    l.Gesture = "dismiss";
                    l.LookMs = 2200;
                    l.Voice = r < 55 ? (state == "filthy" ? new[] { "GREET_MUDDY", "PLAYER_LOOKING_WEIRD" } : new[] { "PLAYER_LOOKING_WEIRD" }) : null;
                    break;
            }
            l.Thought = Line(state + "|" + feel, null, seed);
            return l;
        }

        /// An onlooker's reaction to another man bloodied from a street fight, by name where they know it.
        public static Look ForOther(string temper, string who, string seed)
        {
            string feel = Feeling("bloodied", temper, seed);
            return new Look
            {
                State = "bloodied", Feeling = feel, LookMs = feel == "afraid" ? 1200 : 2400, Gesture = feel == "afraid" || feel == "concerned" ? "surprised" : feel == "cold" ? "dismiss" : "none",
                Thought = Line("other|" + feel, who, seed)
            };
        }

        private static string Line(string key, string who, string seed)
        {
            string[] pool;
            if (!Pools.TryGetValue(key, out pool))
                return null;
            return pool[Voice.Hash(seed + "|line") % pool.Length].Replace("{who}", string.IsNullOrEmpty(who) ? "that fellow" : who);
        }

        /// The gesture's clip in the standing silent set for that sex (checked against the game's own list).
        public static string Clip(string gesture, bool woman, string seed)
        {
            string[] set;
            switch (gesture)
            {
                case "surprised":
                    set = woman ? new[] { "silent_surprised_gasp_l_001", "silent_surprised_gasp_r_001", "silent_surprised_gasp_l_002", "silent_surprised_gasp_r_002", "silent_surprised_react_f_002" }
                                : new[] { "silent_surprised_react_f_001", "silent_surprised_react_f_002", "silent_surprised_react_f_003" };
                    break;
                case "headshake":
                    set = woman ? new[] { "silent_negative_headshake_f_001", "silent_negative_headshake_f_002", "silent_negative_headshake_f_003", "silent_negative_headshake_f_005" }
                                : new[] { "silent_negative_headshake_f_002", "silent_negative_headshake_f_003", "silent_frustrated_headshake_f_001", "silent_frustrated_headshake_f_003" };
                    break;
                case "spit":
                    set = woman ? new[] { "silent_negative_spit_f_001" } : new[] { "silent_neutral_spit_f_001", "silent_neutral_spit_f_002" };
                    break;
                case "dismiss":
                    set = woman ? new[] { "silent_negative_dismiss_f_001", "silent_negative_dismiss_f_002" } : new[] { "silent_neutral_dismiss_f_002" };
                    break;
                default:
                    return null;
            }
            return set[Voice.Hash(seed + "|clip") % set.Length];
        }

        /// The first state the player is in, in the order people notice them, or null.
        public static string StateOf(bool bloodied, bool drunk, bool filthy, bool soaked)
        {
            return bloodied ? "bloodied" : drunk ? "drunk" : filthy ? "filthy" : soaked ? "soaked" : null;
        }

        internal static readonly Dictionary<string, string[]> Pools = new Dictionary<string, string[]>
        {
            { "bloodied|afraid", new[] { "Blood on his shirt. Don't look. Don't look.", "Whose blood is that? Not his, I'd wager.", "Walk faster. He's been in something.",
                                         "Red to the elbow. Lord keep me out of his way.", "Smell of iron off him. Stay on the far side." } },
            { "bloodied|sizing", new[] { "Took a beating, or gave one. Gave one, by the look.", "Blood on his knuckles. Hard man.", "Somebody got the worst of that. Wasn't him.",
                                         "Fresh blood. Wonder what the other fellow looks like.", "Bleeding and still walking easy. Hm." } },
            { "bloodied|cold", new[] { "Blood on him. Not my concern.", "Bleeding on the boardwalk. Somebody'll have to scrub that.", "Whatever he's been at, he can keep it.",
                                       "Another one who settles things with his fists." } },
            { "bloodied|concerned", new[] { "He's bleeding. Ought to see the doctor.", "That cut wants stitching. Someone should tell him.", "Blood at his collar. Hope he's all right.",
                                            "Poor devil's been hurt. Walking like it's nothing.", "Somebody's been at him. Wish I had a clean rag." } },
            { "filthy|disgust", new[] { "Mud to the knees. Like something dragged from a ditch.", "Smell reached me before he did.", "Filth on him. In town, among decent folk.",
                                        "Hasn't seen a basin in a week, that one.", "Tracking muck all down the boards." } },
            { "filthy|amused", new[] { "Looks like he wrestled a hog. And lost.", "Half the county on his boots.", "Ha. Fell off his horse into a wallow, I'd say.",
                                       "Mud to the eyebrows. There's a picture." } },
            { "filthy|pity", new[] { "Hard road, by the look of him.", "Covered in dirt. Must be sleeping rough.", "He could do with a bath and a hot meal.",
                                     "Been working, or riding hard. Poor man." } },
            { "drunk|afraid", new[] { "Drunk, and armed. Keep clear.", "Weaving about with a gun on his hip. Lord.", "Smell of whiskey off him. Don't catch his eye.",
                                      "A drunk man is a dangerous man. Ma always said so." } },
            { "drunk|disgust", new[] { "Stinking of drink. Can barely stand.", "Drunk as a lord, and no lord.", "The bottle's got him. Shameful.",
                                       "Reeling about like that. Has he no people?" } },
            { "drunk|amused", new[] { "Ha. He's had a few.", "Walking like the street's a ship's deck.", "Somebody's been generous at the bar.", "Steady there, friend. Steady." } },
            { "soaked|amused", new[] { "Wet through. Fell in the river, did he?", "Dripping like a drowned cat.", "Took a swim in his clothes. Ha.",
                                       "Water squelching in his boots. Can hear it from here." } },
            { "soaked|concerned", new[] { "Soaked to the skin. He'll catch his death.", "Wet through. Somebody get that man by a stove.", "Dripping. Went in the river after something, must have.",
                                          "He'll take a chill like that." } },
            { "other|afraid", new[] { "{who}'s bleeding. I saw how it started.", "Blood on {who}'s face. Don't stare.", "{who} took a beating. Could've been anyone.", "Keep walking. {who}'s still angry." } },
            { "other|sizing", new[] { "{who} got the worst of it.", "{who}'ll think twice next time.", "Split lip on {who}. Earned it, I'd say.", "{who} can take a punch, I'll give him that." } },
            { "other|cold", new[] { "{who} brought it on himself.", "Blood on {who}. Fool.", "{who} should learn to hold his tongue.", "Not my fight. Not my blood." } },
            { "other|concerned", new[] { "{who}'s hurt. Somebody fetch the doctor.", "Poor {who}. That eye'll swell shut.", "{who}'s bleeding. Should I say something?", "Hope {who} gets home all right." } },
        };

        public static IEnumerable<string> AllLines()
        {
            return Pools.Values.SelectMany(x => x);
        }
    }
}
