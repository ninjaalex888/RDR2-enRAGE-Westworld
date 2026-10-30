// Acknowledgement in passing (roadmap 17a; overnight 2026-10-27, Tier 2.1): two people with a tie who pass each other
// acknowledge it the way people do. Friends: a nod, a hat tip or a wave, often a word. Enemies: a hard look and nothing
// said (the cold, the proud and the hotheaded may spit), and the other looks back. Sweethearts: a look that stays a
// beat too long. People who only know each other: a nod. One of the two thinks something of it, in their own temper and
// true to what they did: the one who greeted, the one greeted back, the one stared at. The game side does the looking,
// the gesture and the voice; this says what. All lines are original to this add-on.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static partial class Passing
    {
        public sealed class Act
        {
            public string Kind;       // warm, sweet, hostile, known
            public string Gesture;    // hattip, nod, wave, sweet, spit, none (a look only)
            public string Answer;     // the other's gesture back: nod, wave, sweet, look (hostile: a look back), none
            public bool Speaks;       // the first says a greeting in their own voice
            public bool Talks;        // and the other answers it, and they part with a word each (Lex, 2026-10-29: hello and goodbye in their voices)
            public int LookMs;        // how long the first one's eyes stay on the other
            public bool BThinks;      // the thought is the other's (else the first one's)
            public string Thought;
        }

        public static readonly string[] Kinds = { "warm", "sweet", "hostile", "known" };

        public static string KindOf(int warmth, bool romantic)
        {
            return romantic && warmth > 0 ? "sweet" : warmth > 0 ? "warm" : warmth < 0 ? "hostile" : "known";
        }

        /// A tie two people would acknowledge in passing: not one where only one of them knows (a lifted purse: the mark
        /// never knew who).
        public static bool Acknowledged(string tieKind)
        {
            return tieKind != "lift";
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
                    a.Speaks = true;   // friends always pass the time of day (Lex, 2026-10-29); was 60%
                    a.Answer = a.Answer == "none" ? "nod" : a.Answer;
                    a.Talks = true;
                    a.LookMs = 1800;
                    break;
                case "sweet":
                    a.Gesture = "sweet";
                    a.Answer = "sweet";
                    a.Speaks = Voice.Hash(seed + "|sp") % 100 < 35;   // shy, more often than not
                    a.LookMs = 3500;                                   // the look that stays a beat too long
                    break;
                case "hostile":
                    a.Gesture = Spitter(aTemper) && !aWoman && r < 30 ? "spit" : "none";   // a woman's answer to an enemy is the cut, not a spit (item 10)
                    a.Answer = "look";   // the other feels it and looks back
                    a.Speaks = false;
                    a.LookMs = 2200;
                    break;
                default:
                    a.Gesture = "nod";
                    a.Answer = Voice.Hash(seed + "|ans") % 100 < 50 ? "nod" : "none";
                    a.Speaks = Voice.Hash(seed + "|sp") % 100 < 70;   // was 25%: people who know each other say so (Lex, 2026-10-29)
                    if (a.Speaks)
                    {
                        a.Answer = "nod";
                        a.Talks = true;
                    }
                    a.LookMs = 1200;
                    break;
            }
            // one thought, the first one's or the other's (never the other's when they didn't answer: they never noticed)
            a.BThinks = a.Answer != "none" && Voice.Hash(seed + "|bt") % 100 < 45;
            a.Thought = a.BThinks ? Thought(kind, a.Gesture == "spit" ? "spat" : kind == "hostile" ? "stared" : "answered", a.Gesture, bTemper, aName, seed + "|b")
                                  : Thought(kind, "did", a.Gesture, aTemper, bName, seed + "|a");
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

        /// One line for someone who just passed {b}: by what's between them, their part (did: greeted, stared, spat;
        /// answered: greeted back; stared: was stared at; spat: was spat at), the gesture (a hat tip is thought of as a
        /// hat tip) and their temper.
        public static string Thought(string kind, string role, string gesture, string temper, string other, string seed)
        {
            string g = Minds.TemperGroup(temper);
            var pool = new List<string>();
            string[] p;
            if (Pools.TryGetValue(kind + "|" + role + "|" + g, out p) || Pools.TryGetValue(kind + "|" + role, out p))
                pool.AddRange(p);
            if (gesture != null && Pools.TryGetValue(kind + "|" + role + "|" + gesture, out p))
                pool.AddRange(p);
            if (pool.Count == 0)
                pool.AddRange(Pools["known|did"]);
            return pool[Voice.Hash(seed) % pool.Count].Replace("{b}", string.IsNullOrEmpty(other) ? "that one" : other);
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
                    return null;   // "none" and "look": the eyes only
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

        /// The other's answer to a greeting, in their own recorded voice: the first line their voice has.
        public static readonly string[] GreetingBack = { "GREET_GENERAL_FAMILIAR", "HOWS_IT_GOING", "GREET_GENERAL_STRANGER" };

        /// Parting, in their own recorded voice.
        public static readonly string[] Goodbye = { "GENERIC_GOODBYE", "GOODBYE_START", "GOODBYE" };

        /// The words of the hello and the goodbye for the ticker, in our own words, never a transcript of the recording (Lex,
        /// 2026-10-29: "NPCs who know each other use their voicelines to say Hi and Goodbye"). {o}: the other's first name.
        public static string HelloWords(int hour, string other, string seed)
        {
            string w = hour >= 5 && hour < 12 ? Voice.Pick(seed + "|hw", "Morning, {o}.", "Morning to you, {o}.", "{o}. Fine morning.")
                     : hour >= 18 && hour < 23 ? Voice.Pick(seed + "|hw", "Evening, {o}.", "Evening, {o}. Long day?", "{o}. Getting dark early.")
                     : Voice.Pick(seed + "|hw", "Afternoon, {o}.", "How do, {o}.", "{o}. Been a while.");
            return w.Replace("{o}", other ?? "friend");
        }

        public static string HelloBack(string other, string seed)
        {
            return Voice.Pick(seed + "|hb", "{o}.", "And you, {o}.", "Well now, {o}.", "You're looking well, {o}.").Replace("{o}", other ?? "friend");
        }

        public static string ByeWords(string seed)
        {
            return Voice.Pick(seed + "|bw", "I'll let you get on.", "Watch the road home.", "Give my best at home.", "Good day, then.");
        }

        public static string ByeBack(string seed)
        {
            return Voice.Pick(seed + "|bb", "And you.", "Good day.", "So long.", "Same to you.");
        }

        public static IEnumerable<string> HelloLines()
        {
            for (int i = 0; i < 12; i++)
                foreach (int hour in new[] { 8, 14, 20 })
                {
                    yield return HelloWords(hour, "Abe", "pw" + i);
                    yield return HelloBack("Ruth", "pw" + i);
                    yield return ByeWords("pw" + i);
                    yield return ByeBack("pw" + i);
                }
        }

        // keys: kind|role[|temper group or gesture]; a gesture's lines join the role's own
        internal static readonly Dictionary<string, string[]> Pools = new Dictionary<string, string[]>
        {
            { "warm|did", new[] { "There's {b}. Always a kind word, that one.", "Good to see {b} about.", "{b} looks well. That's something.",
                                  "{b}. One friendly face on this street, anyhow.", "Must ask after {b}'s people, next time we stop." } },
            { "warm|did|hattip", new[] { "Tipped my hat to {b}. Felt neighbourly.", "Hat to {b}. Old habit. Good one.", "Touched my brim to {b}, as a man should.", "Brim to {b}. Hand knew to do it before I did." } },
            { "warm|did|wave", new[] { "Waved to {b}. Small thing. It matters.", "Raised a hand to {b}. Pleasant, that.", "Waved at {b} across the street. Like old times.", "Waved to {b} with my hat in my hand. {b} waved back. Enough said." } },
            { "warm|did|cold", new[] { "{b}. Worth staying friendly with.", "A good day to {b}. Costs nothing.", "{b} likes me. Might be useful, one day.", "Nodded to {b}. A nod's cheap, and {b} remembers them." } },
            { "warm|did|bold", new[] { "{b}. Good sort. Knows where they stand with me.", "Need a few on my side, in this town. {b}'s one.", "{b} would stand by me in a fight. I'd hope.", "{b}. One I'd want behind me, not in front." } },
            { "warm|did|fearful", new[] { "Glad {b} doesn't hold anything against me.", "{b}'s friendly. One less thing to fret about.", "{b}'s friendly to me. Can't think why. Won't ask.", "{b} smiled back. Didn't have to. Hold onto that." } },
            { "warm|answered", new[] { "{b} greeted me first. I'll take that.", "A good day from {b}. Good.", "{b} always has a greeting for me.", "{b} called out first. Made the street a shade warmer." } },
            { "warm|answered|fearful", new[] { "{b} spoke to me. Nothing went wrong. Good.", "{b} was friendly. Breathe out.", "{b} greeted me first. My heart going like a rabbit's. Over a greeting.", "{b} said my name in the street. Kindly. I checked twice." } },
            { "sweet|did", new[] { "{b}. Don't stare. Too late.", "Heart jumped when {b} looked over.", "Walk on. Walk on. Don't look back at {b}.",
                                   "If {b} had stopped, what would I have said?", "Smell of {b}'s soap on the air. Or I imagined it." } },
            { "sweet|answered", new[] { "{b} saw me. Smiled, I think. I think so.", "{b} looked at me. Right at me.", "{b} looked a long while. That means something. Doesn't it?", "{b} looked over and I forgot where I was walking." } },
            { "hostile|did", new[] { "Still sour, {b} and me.", "Didn't nod to {b}. Wasn't going to.", "Looked right through {b}. Let {b} feel it.", "Passed {b} close enough to smell the pomade. Said nothing." } },
            { "hostile|did|bold", new[] { "{b}. Look me in the eye, then.", "Go on, {b}. Say something.", "Walk past me like that again, {b}.", "Didn't move an inch for {b}. Let {b} go round." } },
            { "hostile|did|fearful", new[] { "Couldn't help staring at {b}. Fool thing to do.", "{b}'s still sore. Can feel it from here.", "Shouldn't have caught {b}'s eye. Shouldn't have.", "{b} went by. My mouth went dry. Over nothing. Over {b}." } },
            { "hostile|did|cold", new[] { "{b}. I keep the account.", "Not worth a nod, {b}.", "{b} can stew.", "{b}. Marked in the book. Walk on." } },
            { "hostile|did|spit", new[] { "Spat at {b}'s feet. Felt good, too.", "Spat in the dust where {b} could see. Childish. Satisfying.", "{b} saw me spit. Good.", "Spat as {b} passed. Dust took it. {b} took it too." } },
            { "hostile|stared", new[] { "{b} gave me that look again.", "There goes {b}, nose in the air.", "{b} staring like I owe money. I don't.", "{b} looked me up and down like a horse at auction." } },
            { "hostile|stared|bold", new[] { "{b} stared. Let 'em stare.", "{b} wants trouble. {b} can have it.", "{b} giving me the eye. Let {b} try something.", "{b} stared. I stared longer. {b} looked away first." } },
            { "hostile|stared|fearful", new[] { "Don't look at {b}. Eyes on the boards.", "Heart going. Just {b}, walking past.", "{b}'s eyes on me. Keep walking. Keep walking.", "{b}'s eyes on my back. Can feel them like a hand." } },
            { "hostile|stared|cold", new[] { "Saw {b}. {b} saw me. That's all it needs.", "{b}'s glaring. Nothing new.", "{b} stares. Costs me nothing.", "{b} stared. Let {b} spend the effort. I'll spend none." } },
            { "hostile|spat", new[] { "{b} spat as I went by. I'll remember it.", "Spat on the boards, did {b}? Fine.", "{b}'s spit, an inch from my boot.", "{b} spat and missed. Next time {b} won't bother missing." } },
            { "known|did", new[] { "{b}. Know the face, at least.", "Nod to {b}. That's all we are, nods.", "Seen {b} about. Never said much.", "{b}. Same as ever." } },
            { "known|answered", new[] { "{b} greeted me. I answered. That's about all we ever do.", "{b}. Polite, at least.", "A greeting from {b}, one back from me. That's us.", "{b} nodded. I nodded. Ten years of that, near enough." } },
        };

        public static IEnumerable<string> AllLines()
        {
            return Pools.Values.SelectMany(x => x);
        }
    }
}
