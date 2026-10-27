// Acknowledgement in passing (roadmap 17a; overnight 2026-10-27, Tier 2.1): two people with a tie who pass each other
// acknowledge it the way people do. Friends: a nod or a hat tip, often a word. Enemies: a hard look and nothing said
// (the cold and the proud may spit). Sweethearts: a look that stays a beat too long. People who only know each other:
// a nod. Each thinks something of it, in their own temper. The game side does the looking, the gesture and the voice;
// this says what. All lines are original to this add-on.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class Passing
    {
        public sealed class Act
        {
            public string Kind;       // warm, sweet, hostile, known
            public string Gesture;    // hattip, nod, wave, sweet, spit, none (a look only)
            public string Answer;     // the other's gesture back: nod, wave, sweet, none
            public bool Speaks;       // the first says a greeting in their own voice
            public int LookMs;        // how long the first one's eyes stay on the other
            public string AThought, BThought;
        }

        public static readonly string[] Kinds = { "warm", "sweet", "hostile", "known" };

        public static string KindOf(int warmth, bool romantic)
        {
            return romantic && warmth > 0 ? "sweet" : warmth > 0 ? "warm" : warmth < 0 ? "hostile" : "known";
        }

        /// What passes between them: by the tie (its warmth and kind), their sexes and tempers, and a seed.
        public static Act For(int warmth, bool romantic, bool aWoman, bool bWoman, string aTemper, string bTemper, string aName, string bName, string seed)
        {
            string kind = KindOf(warmth, romantic);
            var a = new Act { Kind = kind };
            int r = Voice.Hash(seed + "|g") % 100;
            switch (kind)
            {
                case "warm":
                    a.Gesture = aWoman ? (r < 50 ? "wave" : "nod") : (r < 60 ? "hattip" : "nod");
                    a.Answer = Voice.Hash(seed + "|ans") % 100 < 70 ? (bWoman && r % 2 == 0 ? "wave" : "nod") : "none";
                    a.Speaks = Voice.Hash(seed + "|sp") % 100 < 60;
                    a.LookMs = 1800;
                    break;
                case "sweet":
                    a.Gesture = "sweet";
                    a.Answer = "sweet";
                    a.Speaks = Voice.Hash(seed + "|sp") % 100 < 35;   // shy, more often than not
                    a.LookMs = 3500;                                   // the look that stays a beat too long
                    break;
                case "hostile":
                    a.Gesture = Spitter(aTemper) && r < 30 ? "spit" : "none";
                    a.Answer = "none";
                    a.Speaks = false;
                    a.LookMs = 2200;
                    break;
                default:
                    a.Gesture = "nod";
                    a.Answer = Voice.Hash(seed + "|ans") % 100 < 50 ? "nod" : "none";
                    a.Speaks = Voice.Hash(seed + "|sp") % 100 < 25;
                    a.LookMs = 1200;
                    break;
            }
            a.AThought = Thought(kind, aTemper, bName, seed + "|a");
            if (Voice.Hash(seed + "|bt") % 100 < 55)
                a.BThought = a.Gesture == "spit" ? Spat(aName, seed + "|b") : Thought(kind, bTemper, aName, seed + "|b");
            return a;
        }

        /// The cold, the proud and the hotheaded may spit as an enemy goes by; the rest only look.
        public static bool Spitter(string temper)
        {
            return temper == "Cold" || temper == "Proud" || temper == "Hothead";
        }

        /// The first of two to act: an enemy's look comes from the hotter head, else from whoever did the other a
        /// wrong; otherwise it's who happens to look up first (the seed).
        public static bool AFirst(string kind, string aId, string bId, string aTemper, string bTemper, string doer, string seed)
        {
            if (kind == "hostile")
            {
                bool aHot = aTemper == "Hothead" || aTemper == "Proud", bHot = bTemper == "Hothead" || bTemper == "Proud";
                if (aHot != bHot)
                    return aHot;
                if (doer != null && (doer == aId || doer == bId))
                    return doer == aId;
            }
            return Voice.Hash(seed + "|first") % 2 == 0;
        }

        /// One line for someone who just passed {b}, by what's between them and their temper.
        public static string Thought(string kind, string temper, string other, string seed)
        {
            string[] pool = PoolFor(kind, temper);
            return Fill(pool[Voice.Hash(seed) % pool.Length], other);
        }

        /// The one spat at, as they go on.
        public static string Spat(string other, string seed)
        {
            string[] pool = Pools["hostile:spat"];
            return Fill(pool[Voice.Hash(seed) % pool.Length], other);
        }

        private static string Fill(string line, string other)
        {
            return line.Replace("{b}", string.IsNullOrEmpty(other) ? "that one" : other);
        }

        private static string[] PoolFor(string kind, string temper)
        {
            string g = Minds.TemperGroup(temper);
            string[] pool;
            if (Pools.TryGetValue(kind + "|" + g, out pool) || Pools.TryGetValue(kind, out pool))
                return pool;
            return Pools["known"];
        }

        /// The gesture's clip in the standing silent set for that sex (ai_gestures@gen_male|gen_female@standing@silent,
        /// checked against the game's own list): toward the side the other is on where the clip has both sides.
        public static string Clip(string gesture, bool woman, bool left, string seed)
        {
            string[] set;
            switch (gesture)
            {
                case "hattip":
                    set = woman ? NodClips(true) : new[] { "silent_neutral_hat_tip_l_001", "silent_neutral_hat_tip_r_001", "silent_neutral_hat_tip_l_004", "silent_neutral_hat_tip_r_004" };
                    break;
                case "nod":
                    set = NodClips(woman);
                    break;
                case "wave":
                    set = woman ? new[] { "silent_neutral_wave_l_001", "silent_neutral_wave_r_001" } : new[] { "silent_casual_greet_l_001", "silent_casual_greet_r_001" };
                    break;
                case "sweet":
                    set = woman ? new[] { "silent_flirty_greet_l_001", "silent_flirty_greet_r_001" } : new[] { "silent_positive_headonod_f_001", "silent_neutral_hat_tip_l_001", "silent_neutral_hat_tip_r_001" };
                    break;
                case "spit":
                    set = woman ? new[] { "silent_negative_spit_f_001" } : new[] { "silent_neutral_spit_f_001" };
                    break;
                default:
                    return null;
            }
            string side = left ? "_l_" : "_r_";
            if (set.Any(c => c.Contains("_l_")) && set.Any(c => c.Contains("_r_")))
                set = set.Where(c => c.Contains(side) || !(c.Contains("_l_") || c.Contains("_r_"))).ToArray();
            return set[Voice.Hash(seed + "|clip") % set.Length];
        }

        private static string[] NodClips(bool woman)
        {
            return woman ? new[] { "silent_headnod_greet_f_001", "silent_headnod_greet_f_003", "silent_positive_headonod_f_001" }
                         : new[] { "silent_headnod_greet_f_001", "silent_headnod_greet_f_002", "silent_headnod_greet_f_003", "silent_positive_headonod_f_001" };
        }

        /// What the first one says, by the hour, in their own recorded voice: the first line their voice has.
        public static string[] Greeting(int hour)
        {
            const string familiar = "GREET_GENERAL_FAMILIAR";
            if (hour >= 5 && hour < 12)
                return new[] { "GREET_MORNING", familiar, "HOWS_IT_GOING" };
            if (hour >= 18 && hour < 23)
                return new[] { "GREET_EVENING", familiar, "HOWS_IT_GOING" };
            return new[] { familiar, "HOWS_IT_GOING" };
        }

        internal static readonly Dictionary<string, string[]> Pools = new Dictionary<string, string[]>
        {
            { "warm", new[] {
                "There's {b}. Always a kind word, that one.", "Tipped my hat to {b}. Felt neighbourly.", "{b} nodded first. I'll take that.",
                "Good to see {b} about.", "{b} looks well. That's something.", "A wave from {b}. Small thing. It matters.",
                "{b}. One friendly face on this street, anyhow.", "Must ask after {b}'s people, next time we stop." } },
            { "warm|cold", new[] { "{b}. Worth staying friendly with.", "Nod to {b}. Costs nothing.", "{b} likes me. Might be useful, one day." } },
            { "warm|bold", new[] { "{b}. Good sort. Knows where they stand with me.", "Need a few on my side, in this town. {b}'s one.", "{b} would stand by me in a fight. I'd hope." } },
            { "warm|fearful", new[] { "{b} smiled at me. One less thing to fret about.", "Glad {b} doesn't hold anything against me.", "{b} nodded. I nodded. Nothing went wrong." } },
            { "sweet", new[] {
                "{b}. Don't stare. Too late.", "Heart jumped when {b} looked over.", "{b} saw me. Smiled, I think. I think so.",
                "Walk on. Walk on. Don't look back at {b}.", "If {b} had stopped, what would I have said?", "Smell of {b}'s soap on the air. Or I imagined it.",
                "{b} looked away first. That means something. Doesn't it?" } },
            { "hostile|bold", new[] { "{b}. Look me in the eye, then.", "Go on, {b}. Say something.", "{b} looked away first. Good.", "Walk past me like that again, {b}." } },
            { "hostile|fearful", new[] { "Don't look at {b}. Eyes on the boards.", "{b}'s still sore. Can feel it from here.", "Crossed the street for {b} again.", "Heart going. Just {b}, walking past." } },
            { "hostile|cold", new[] { "{b}. I keep the account.", "Not worth a nod, {b}.", "{b} can stew.", "Saw {b}. {b} saw me. That's all it needs." } },
            { "hostile", new[] { "{b} gave me that look again.", "Still sour, {b} and me.", "There goes {b}, nose in the air.", "Didn't nod to {b}. Wasn't going to." } },
            { "hostile:spat", new[] { "{b} spat as I went by. I'll remember it.", "Spat on the boards, did {b}? Fine.", "{b}'s spit, an inch from my boot." } },
            { "known", new[] { "{b}. Know the face, at least.", "Nod to {b}. That's all we are, nods.", "Seen {b} about. Never said much.", "{b}. Same as ever." } },
        };

        public static IEnumerable<string> AllLines()
        {
            return Pools.Values.SelectMany(x => x);
        }
    }
}
