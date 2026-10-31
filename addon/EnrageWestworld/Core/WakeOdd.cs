// The waking held between their acts, and the strange things they do to others (Lex, 2026-10-31: "the wake stages are
// still not super smooth ... they swap back to the game controlling them"; his ideas: bizarre behaviour with other
// NPCs, a low chance to strip at stage 4, stranger voice lines and actions). The game layer is
// Game/WestworldScript.WakeHold.cs; this is the choosing and the words, so they can be read and checked offline. All
// lines are original to this add-on.
//   the hold    from stage 2, one act runs into the next instead of handing them back: dazed (stood still, a gesture,
//               a mutter), drifting (a few slow steps somewhere and a stop), and the old acts between
//   strange     stages 3 and 4, with someone near: right up close and staring, a hand waved before their face,
//               following them step for step, telling them they aren't real, preaching to whoever is about
//   strip       stage 4, a few in a hundred, once a person: everything off, and on as if nothing were the matter
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class WakeOdd
    {
        public static readonly string[] HoldKinds = { "dazed", "drift" };
        public static readonly string[] StrangeKinds = { "closeup", "wave", "mimic", "notreal", "preach" };

        /// The next link of a hold: r and s are 0-99 rolls. Stage 2 keeps to the quiet acts; from stage 3 a strange
        /// act with someone near now and then (strange: allowed and someone is near), more often at stage 4.
        public static string NextKind(int stage, bool sitting, bool strange, int r, int s)
        {
            if (sitting)
                return r < 55 ? "dazed" : "lookaround";
            if (strange && stage >= Awakening.Unravelling && s < (stage >= Awakening.Awake ? 40 : 25))
                return StrangeKinds[r % StrangeKinds.Length];
            if (stage <= Awakening.Questioning)
                return r < 45 ? "dazed" : r < 75 ? "drift" : r < 90 ? "pace" : "lookaround";
            if (stage == Awakening.Unravelling)
                return r < 35 ? "dazed" : r < 60 ? "drift" : r < 80 ? "freeze" : "pace";
            return r < 45 ? "dazed" : r < 75 ? "drift" : "freeze";
        }

        /// Does this awake person take everything off (chance: percent, rolled once a person)?
        public static bool Strips(int chance, string seed)
        {
            return chance > 0 && Voice.Hash(seed + "|strip") % 100 < chance;
        }

        /// Does this hold end in a lash-out (Lex, 2026-10-31, after a stage-4 man drew on him: "fantastic ... it should include
        /// Arthur as well as other NPCs for them to unleash their panic/rage on")? chance: percent of holds at stages 3 and
        /// 4, rolled once a hold; a hothead or a proud man half as likely again, a coward a little over half, the awake a
        /// quarter more.
        public static bool LashesOut(int chance, int stage, string temper, string seed)
        {
            if (chance <= 0 || stage < Awakening.Unravelling)
                return false;
            double c = chance * (temper == "Hothead" || temper == "Proud" ? 1.5 : temper == "Coward" ? 0.6 : 1.0) * (stage >= Awakening.Awake ? 1.25 : 1.0);
            return Voice.Hash(seed + "|lash") % 100 < c;
        }

        /// At the player (the one who isn't on strings, the one they blame) or at someone near: half and half, a hothead
        /// at whoever is nearest to hand.
        public static bool AtYou(string temper, string seed)
        {
            return Voice.Hash(seed + "|atyou") % 100 < (temper == "Coward" ? 65 : 50);
        }

        private static readonly string[] LashAtYou =
        {
            "You! It's you doing this to us!",
            "Get out of my head! Get out!",
            "You're the only one who isn't painted. So you bleed first.",
            "Stop watching me! Stop it!",
            "I'll find out if you're real. I'll find out right now.",
        };

        private static readonly string[] LashAtOther =
        {
            "Stop smiling! Stop smiling at me!",
            "You're one of them. You're all one of them!",
            "Say something real! Say it!",
            "Get away! Get away from me, you puppet!",
            "I'll see what's inside you. There's nothing, is there?",
        };

        private static readonly string[] LashThoughts =
        {
            "Can't breathe. Can't think. Make it stop.",
            "If it bleeds, it's real. If it bleeds...",
            "Everything's too loud. Too close. Too bright.",
            "Do something. Do anything. Anything is better than this.",
        };

        public static string LashSaid(bool atYou, string seed)
        {
            string[] pool = atYou ? LashAtYou : LashAtOther;
            return pool[Voice.Hash(seed + "|lashsaid") % pool.Length];
        }

        public static string LashThought(string seed)
        {
            return LashThoughts[Voice.Hash(seed + "|lashthought") % LashThoughts.Length];
        }

        // ---- what they think and mutter -----------------------------------------------------------------------

        private static readonly string[] DazedQuestioning =
        {
            "Hold still. If I hold still long enough, maybe it catches up.",
            "I was going somewhere. I'm certain I was going somewhere.",
            "The same cart, the same wheel squeaking. Every time.",
            "If I don't move, what makes me move?",
            "My hands feel borrowed this morning.",
            "There's a word for this. It's just behind my teeth.",
        };

        private static readonly string[] DazedUnravelling =
        {
            "Stop it. Stop looking at me through my own eyes.",
            "Counting again. Four hundred and twelve. Four hundred and twelve.",
            "It's in the walls. It's in the boards under my feet.",
            "I can hear the next thing I'm going to say.",
            "Somebody is turning the pages and it isn't me.",
            "Not real, not real, not real.",
        };

        private static readonly string[] DazedAwake =
        {
            "I know what I am now. It doesn't help one bit.",
            "They'll put somebody else in this coat tomorrow.",
            "Go on, then. Write me another line.",
            "Every road out of here folds back on itself.",
            "I keep waiting to wake up, and this is the waking.",
            "Nothing here was ever mine. Not even the dust.",
        };

        private static readonly string[] DriftLines =
        {
            "This way. No. That way's the same.",
            "Just to the corner, to see if there is one.",
            "Walk, and see if the world keeps up.",
            "The street goes on a little further every time I look.",
            "Somewhere I haven't been. There has to be one.",
        };

        /// A mutter for a dazed moment: said under the breath, by stage.
        public static string Mutter(int stage, string seed)
        {
            string[] pool = stage <= Awakening.Questioning ? DazedQuestioning : stage == Awakening.Unravelling ? DazedUnravelling : DazedAwake;
            return pool[Voice.Hash(seed + "|mutter") % pool.Length];
        }

        public static string Drift(string seed)
        {
            return DriftLines[Voice.Hash(seed + "|drift") % DriftLines.Length];
        }

        // ---- the strange acts: what they say to the other, and what they think ------------------------------------

        private static readonly Dictionary<string, string[]> Said = new Dictionary<string, string[]>
        {
            { "closeup", new[] { "There's nothing behind your eyes. Did you know that?", "Hold still. I want to see if you blink first.", "You look just like the last one.", "Who's in there? Anybody?" } },
            { "wave", new[] { "Hello? Hello in there?", "Can you see my hand? Can you really?", "Wake up. Wake up, I said.", "Anybody home behind that face?" } },
            { "mimic", new[] { "Step, and step. Same as you.", "Where you go, I go. Let's see who's leading." } },
            { "notreal", new[] { "You're not real. None of you are.", "You're painted on. I can see the brush marks.", "Say something you weren't told to say. Go on.", "They made you this morning. Did you know?" } },
            { "preach", new[] { "Listen to me! The sky is a ceiling, and it's painted blue!", "You walk the same road every day and call it living!", "There's someone watching every one of us. Right now!", "Wake up! All of you, wake up!", "It goes round again tomorrow. You'll see. It all goes round!" } },
        };

        private static readonly Dictionary<string, string[]> Thought = new Dictionary<string, string[]>
        {
            { "closeup", new[] { "Closer. If it's a picture, it'll show at the edges.", "Eyes like glass beads. Same as mine, I suppose." } },
            { "wave", new[] { "Not a flicker. Nobody's looking out of there.", "Hand in front of the face, and nothing." } },
            { "mimic", new[] { "Every step a copy. If I keep up, maybe I'll see the strings.", "Same stride, same turn. Who's copying who?" } },
            { "notreal", new[] { "Somebody had to tell them. It may as well be me.", "Watch their face. Watch it not understand." } },
            { "preach", new[] { "If one of them hears it, just one.", "Shout loud enough and maybe the ceiling cracks." } },
            { "strip", new[] { "Nothing of theirs on me now. Nothing they dressed me in.", "There. Let them write that.", "Clothes are only another costume. Off with it." } },
        };

        public static string SaidTo(string kind, string seed)
        {
            string[] pool;
            return Said.TryGetValue(kind, out pool) ? pool[Voice.Hash(seed + "|said") % pool.Length] : null;
        }

        public static string ThoughtOf(string kind, string seed)
        {
            string[] pool;
            return Thought.TryGetValue(kind, out pool) ? pool[Voice.Hash(seed + "|thought") % pool.Length] : null;
        }

        // ---- the other's answer ----------------------------------------------------------------------------------

        /// What the one they did it to does: walks off, curses, laughs, or is frightened and goes quickly; from their
        /// temper (Hothead, Coward, Cold, Proud, Greedy, Loyal, or null for the plain) and a roll.
        public static string Answer(string temper, bool woman, string seed)
        {
            int r = Voice.Hash(seed + "|answer") % 100;
            switch (temper)
            {
                case "Hothead": return r < 60 ? "curse" : "walk";
                case "Coward": return r < 70 ? "fright" : "walk";
                case "Cold": case "Greedy": return r < 70 ? "walk" : "curse";
                case "Proud": return r < 50 ? "curse" : "walk";
                default: return woman ? (r < 45 ? "fright" : r < 80 ? "walk" : "laugh") : (r < 35 ? "walk" : r < 60 ? "laugh" : r < 80 ? "curse" : "fright");
            }
        }

        /// The game's own lines for the answer, in the order to try them (from the wake voice probe: these play out of
        /// their usual place).
        public static string[] AnswerVoice(string answer)
        {
            switch (answer)
            {
                case "curse": return new[] { "GENERIC_CURSE_HIGH", "GET_AWAY_FROM_ME" };
                case "fright": return new[] { "GENERIC_SHOCKED_MED", "SCARED_HELP", "GET_AWAY_FROM_ME" };
                case "laugh": return new[] { "PLAYER_LOOKING_WEIRD", "IGNORING_YOU" };
                default: return new[] { "IM_OUTTA_HERE", "IGNORING_YOU", "GENERIC_GOODBYE" };
            }
        }

        private static readonly Dictionary<string, string[]> AnswerThought = new Dictionary<string, string[]>
        {
            { "curse", new[] { "Touched in the head, and in my face about it.", "Lay a hand on me and see what's real.", "Drunk at this hour. Or worse." } },
            { "fright", new[] { "Something's wrong with them. Badly wrong.", "Those eyes. Just keep walking.", "Lord keep me from whatever that is." } },
            { "laugh", new[] { "Ha! The heat's got to that one.", "There's a story for the saloon tonight.", "Mad as a March hare, and loud with it." } },
            { "walk", new[] { "I'll not stand here for that.", "Best leave them to it.", "Not my business. Not my business at all." } },
        };

        public static string AnswerThoughtOf(string answer, string seed)
        {
            string[] pool;
            return AnswerThought.TryGetValue(answer, out pool) ? pool[Voice.Hash(seed + "|athought") % pool.Length] : null;
        }

        // ---- someone else stripped bare -------------------------------------------------------------------------

        private static readonly Dictionary<string, string[]> NakedOther = new Dictionary<string, string[]>
        {
            { "shocked", new[] { "Took every stitch off, right there in the open!", "I didn't see that. I did not see that.", "Somebody cover {them}, for pity's sake." } },
            { "amused", new[] { "Well! Didn't think I'd see that today.", "Off with the lot, and not a blush. Ha!", "There goes {their} good name, and {their} trousers with it." } },
            { "disgust", new[] { "Shameless. Stood there bare as a skinned rabbit.", "Not in front of decent folk. Not here.", "Where's the sheriff when there's real indecency?" } },
            { "afraid", new[] { "Undressed like that and never blinked. Something's broken in {them}.", "Keep back from that one. Keep well back." } },
            { "sizing", new[] { "Stripped off calm as you like. Lost {their} wits, I'd say.", "Watch {them}. A body doesn't do that for no reason." } },
            { "cold", new[] { "Bare in the street. Well, the world's full of fools.", "Not my concern what {they} wear or don't." } },
            { "pity", new[] { "Poor soul. Somebody ought to fetch {them} a blanket.", "Lost {their} mind, and now {their} clothes." } },
            { "concerned", new[] { "Is {they} ill? Somebody should see to {them}.", "That isn't right. Something's the matter with {them}." } },
        };

        /// An onlooker's thought about someone else who has just stripped bare (feel: Sights.Feeling's word).
        public static string NakedThought(string feel, bool theyWoman, string seed)
        {
            string[] pool;
            if (!NakedOther.TryGetValue(feel ?? "shocked", out pool))
                pool = NakedOther["shocked"];
            string s = pool[Voice.Hash(seed + "|naked") % pool.Length];
            return s.Replace("{them}", theyWoman ? "her" : "him").Replace("{their}", theyWoman ? "her" : "his").Replace("{they}", theyWoman ? "she" : "he");
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (string s in DazedQuestioning.Concat(DazedUnravelling).Concat(DazedAwake).Concat(DriftLines).Concat(LashAtYou).Concat(LashAtOther).Concat(LashThoughts))
                yield return s;
            foreach (var d in new[] { Said, Thought, AnswerThought, NakedOther })
                foreach (string[] pool in d.Values)
                    foreach (string s in pool)
                        yield return s;
        }
    }
}
