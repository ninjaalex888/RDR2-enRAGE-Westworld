// A greeting in passing from the mind (roadmap 39, deeper mind interactions; night 2026-10-29, item 6; with [Mind] MindLive
// and [Mind] MindPassing). The tie says what two people usually do when they pass; what the first one feels about the other
// lately, and their mood today, says what they do this time: sore at a friend over something lately, a nod and no word;
// afraid of them, eyes away and walk on; ashamed before them, eyes down; grateful, a warm word even to someone they barely
// know; with nothing felt about the other, a sore day keeps it to a nod and a glad one has a word for anyone they know.
// 1899's display rules hold here as everywhere: a woman's anger before the street is a cool nod, not a cut. The thought
// says what's really felt. All lines are original to this add-on.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static partial class Passing
    {
        /// How much of a feeling must be left to colour a greeting (Mood.Lasting).
        public const float MindLeft = 0.18f;

        /// Changes the act by what the first one (A) feels about the other (B) lately and A's mood, and says why (null:
        /// nothing changed). `felt`: A's feelings (the shadow's); `mood`: Mood.SoreOrGlad (sore, glad or null).
        public static string Minded(Act a, List<FeltAt> felt, string bId, string mood, bool aWoman, string bName, long now, string seed)
        {
            if (a == null || a.Kind == "hostile")
                return null;   // enemies already look hard at each other
            FeltAt best = default(FeltAt);
            float left = 0f;
            if (felt != null && bId != null)
                foreach (FeltAt f in felt)
                {
                    if (f.AboutId != bId && f.DoerId != bId)
                        continue;
                    float l = Mood.Lasting(f, now);
                    if (l > left)
                    {
                        left = l;
                        best = f;
                    }
                }
            string kind = left >= MindLeft ? Feeling.Kinds[best.Kind] : null;
            string b = string.IsNullOrEmpty(bName) ? "that one" : bName;
            switch (kind)
            {
                case "anger":
                case "contempt":
                case "envy":
                    // sore at them over something lately: a nod and no word (a woman's is a cool nod before the street; a man may not nod at all)
                    a.Gesture = aWoman || a.Kind == "sweet" ? "nod" : "none";
                    a.Answer = a.Answer == "none" ? "none" : "look";
                    a.Speaks = a.Talks = false;
                    a.LookMs = 1500;
                    return Think(a, "sore", b, seed, "sore at " + b + " lately (" + kind + ")");
                case "fear":
                    a.Gesture = "none";
                    a.Answer = "none";
                    a.Speaks = a.Talks = false;
                    a.LookMs = 500;
                    return Think(a, "afraid", b, seed, "afraid of " + b + " lately");
                case "shame":
                    if (best.DoerId != bId)
                        break;   // ashamed of something B had no part in: the greeting stands
                    a.Gesture = "none";
                    a.Speaks = a.Talks = false;
                    a.LookMs = 500;
                    return Think(a, "shamed", b, seed, "ashamed before " + b);
                case "gratitude":
                case "joy":
                    if (a.Kind == "known" || a.Kind == "warm")
                    {
                        a.Gesture = aWoman ? "wave" : "hattip";
                        if (a.Answer == "none")
                            a.Answer = "nod";
                        a.Speaks = a.Talks = true;
                        a.LookMs = 1800;
                        return Think(a, "grateful", b, seed, "grateful to " + b + " lately");
                    }
                    break;
            }
            if (kind != null)
                return null;
            // nothing felt about the other lately: the day's mood
            if (mood == "sore" && (a.Kind == "warm" || a.Kind == "known") && a.Speaks)
            {
                a.Speaks = a.Talks = false;
                if (a.Gesture == "hattip" || a.Gesture == "wave")
                    a.Gesture = "nod";
                return Think(a, "moodsore", b, seed, "a sore day: a nod, no talk");
            }
            if (mood == "glad" && a.Kind == "known" && !a.Speaks)
            {
                a.Speaks = a.Talks = true;
                if (a.Answer == "none")
                    a.Answer = "nod";
                return Think(a, "moodglad", b, seed, "a glad day: a word even for " + b);
            }
            return null;
        }

        private static string Think(Act a, string pool, string b, string seed, string why)
        {
            string[] p = MindPools[pool];
            a.BThinks = false;   // the thought is the first one's: it's their feeling
            a.Thought = p[Voice.Hash(seed + "|mind|" + pool) % p.Length].Replace("{b}", b);
            return why;
        }

        /// The thoughts of a greeting the mind changed (kept apart from Pools, whose count the tests hold).
        internal static readonly Dictionary<string, string[]> MindPools = new Dictionary<string, string[]>
        {
            { "sore", new[] { "Not today, {b}. I've not forgotten.", "A nod's all {b} gets from me this week.", "{b} knows what was done. Or ought to.", "Keep walking, {b}. Still sore about it.",
                              "Hard to smile at {b} with that still between us." } },
            { "afraid", new[] { "Don't look at {b}. Eyes on the road.", "Just keep walking. Maybe {b} didn't see me.", "Not {b}. Not today. Look at the boards.", "Walk on, and don't catch {b}'s eye.",
                                "{b}. My hands have gone cold. Keep moving." } },
            { "shamed", new[] { "Head down. {b} saw all of it.", "Can't look {b} in the face. Not yet.", "{b} was there. {b} remembers, sure as I do.", "Eyes down, and past. Past {b}, and on.",
                                "My ears are burning. {b} only has to look at me." } },
            { "grateful", new[] { "There's {b}. I owe {b} a kindness yet.", "{b}! A friendly face, and a good one.", "I'll not forget what {b} did for me.", "A word for {b}, at the least. More, when I can.",
                                  "{b}. I'd tip more than my hat if I had it to give." } },
            { "moodsore", new[] { "Not in the mood for talk. A nod will do.", "Nod and keep moving. Not today.", "{b}. Hm. Haven't the patience for chatter.", "A nod. That's all I've got in me today.",
                                  "Leave it at a nod, {b}. It's been that kind of day." } },
            { "moodglad", new[] { "A fine day to stop and say good day.", "Good to see {b} about.", "{b}! Might as well pass the time.", "The day's come good. A word for {b}, then.",
                                  "Feeling generous. Even {b} gets a good day." } },
        };

        public static IEnumerable<string> MindLines()
        {
            foreach (string[] p in MindPools.Values)
                foreach (string l in p)
                    yield return l;
        }
    }
}
