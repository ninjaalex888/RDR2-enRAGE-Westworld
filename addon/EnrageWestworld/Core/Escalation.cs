// The ladder a quarrel climbs or comes down (roadmap 7; Lex, 2026-10-27: an argument may go all the way to a killing when
// the people's inner lives, histories and surroundings lead there, true to 1899, where most killings were sudden quarrels
// over drink, honour, money or land, at close range, often in a saloon at night between men with history, and many towns
// banned guns inside the limits; and de-escalation by feeling too: an apology, a joke, backing down, walking off, a friend
// stepping in, a drink offered). Built in the night run of 2026-10-28 (after item 14), Core only: nothing in the game
// calls it yet. The rungs are words, a shove, blows, a gun drawn, a shot; at each the hotter of the two pushes up or the
// cooler brings it down, from their traits (anger, self-control, assertiveness, risk, compassion, deference to the law),
// drink, what it's over, who is watching (face), the history between them, who was wronged, a friend at hand, the law at
// hand, the town's gun rule, whether either carries a gun (nobody draws what he doesn't carry, and drawing on an unarmed
// man was murder in every witness's eyes), and whether either is a woman (a man's hand past words against a woman is a
// tenth of his rate; a woman's quarrel stops at the shove). Seeded, so the same quarrel between the same two in the same
// place goes the same way. Reviewed by a subagent the same afternoon (review-escalation.md): the damping by the other
// man's cool, the shooter the one who pushed, the law ending a fistfight, the fair-fight rule, face for everyone and the
// cost of backing down before a crowd, the sex, the aggrieved side, the soft cap, and the clamps came from it. Plain C#,
// no game. mindtest EscalationTest holds the numbers. When wired, the Director calls Next a rung at a time with the
// context read afresh (the pistols on the belts, the law in the scan, the crowd, the friends within reach, the player).
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    /// The quarrel's surroundings, as the game would know them.
    public sealed class QuarrelContext
    {
        public string Place = "street";      // saloon street store church road home
        public int Hour = 12;
        public bool DrunkA, DrunkB;          // in his cups
        public bool ArmedA, ArmedB;          // carries a gun (nobody draws what he doesn't carry)
        public bool WomanA, WomanB;
        public bool LawNear;                 // a lawman within sight
        public bool GunsBanned;              // the town's rule: no guns inside the limits (the men who carry anyway are the ones it worries)
        public bool FriendOfA, FriendOfB;    // a friend at hand who might step in, or egg on
        public int Witnesses;                // who is watching (face is lost before a crowd)
        public int Warmth;                   // the pair's tie, -3..3 (friends cool, foes heat)
        public int History;                  // quarrels between them before
        public string Matter = "nothing";    // drink honour money land woman nothing (a look or a word over nothing was the honour matter of the age)
        public string Aggrieved;             // "a" or "b": the one wronged, when the ties say (he pushes harder); null when nobody knows
    }

    public static class Escalation
    {
        public static readonly string[] Rungs = { "words", "shove", "blows", "draw", "shoot" };
        /// How a quarrel comes down: who ends it and how.
        public static readonly string[] Ways = { "apology", "joke", "backdown", "walkoff", "friend", "drink", "beaten", "law", "shot" };

        public sealed class Step
        {
            public string From, To;     // the rungs; To null when it ended at From
            public string Ended;        // one of Ways when it ended here, else null
            public string By;           // "a" or "b": who pushed it up, or who brought it down; "friendA"/"friendB" whose friend stepped in; "law"
            public float Chance;        // the chance it went up from here (for the log and F7)
            public override string ToString() { return Ended != null ? From + " -> ended: " + Ended + " (" + By + ")" : From + " -> " + To + " (" + By + ")"; }
        }

        private static float Clamp(float v) { return v < 0f ? 0f : v > 1f ? 1f : v; }
        private static int Warmth(QuarrelContext c) { return Math.Max(-3, Math.Min(3, c.Warmth)); }
        private static int Seen(QuarrelContext c) { return Math.Max(0, Math.Min(c.Witnesses, 6)); }

        /// How far this one (side A or B) wants to take it from `rung`: a chance 0..1 of pushing to the next.
        public static float Heat(Traits t, bool sideA, QuarrelContext c, int rung)
        {
            if (t == null || c == null || rung < 0 || rung >= Rungs.Length - 1)
                return 0f;
            bool drunk = sideA ? c.DrunkA : c.DrunkB, armed = sideA ? c.ArmedA : c.ArmedB, friendNear = sideA ? c.FriendOfA : c.FriendOfB;
            bool woman = sideA ? c.WomanA : c.WomanB, otherWoman = sideA ? c.WomanB : c.WomanA, otherArmed = sideA ? c.ArmedB : c.ArmedA;
            bool gun = rung >= 2;
            if (woman && rung >= 1)
                return 0f;   // a woman's quarrel stops at the shove
            // each rung is harder than the last: a shove is cheap, a fist costs, a gun is another world, the shot the last step of it
            float h = rung == 0 ? 0.30f : rung == 1 ? 0.42f : rung == 2 ? 0.10f : 0.30f;
            float nature = gun ? 0.5f : 1f;   // a man's nature moves his fists more than his gun hand; the gun rungs are mostly the surroundings
            h += nature * (0.12f * t[Traits.Anger] + 0.07f * t[Traits.Assertiveness] + 0.07f * t[Traits.Risk] - 0.12f * t[Traits.SelfControl] - 0.05f * t[Traits.Compassion]);
            if (drunk)
                h += gun ? 0.06f : 0.15f;   // drink loosens the fists more than the trigger, and the trigger too (most killings were drunk men's)
            float matter = gun ? 0.5f : 1f;   // what it's over moves the fists more than the gun
            switch (c.Matter)
            {
                case "honour": case "woman": h += 0.10f * matter; break;
                case "land": case "money": h += 0.05f * matter; break;
                case "drink": h += 0.03f * matter; break;
                default: h += 0.03f * matter; break;   // a look, a word: nothing, and honour all the same
            }
            // face: before a crowd nobody backs down cheaply, and the forward can't at all (from words and a shove; a gun is past face)
            int seen = Seen(c);
            if (!gun && seen > 0)
            {
                h += 0.01f * seen;
                if (t[Traits.Assertiveness] > 0f)
                    h += 0.015f * seen * Math.Min(1.5f, t[Traits.Assertiveness]);
                if (friendNear)
                    h += 0.01f * Math.Min(seen, 3);   // a friend at the shoulder eggs on before a crowd
            }
            h += (gun ? 0.02f : 0.04f) * Math.Min(Math.Max(0, c.History), 4);
            // a friend's words sting less, and nobody draws on a friend; a foe's sting more
            int warmth = Warmth(c);
            h -= warmth > 0 ? (gun ? 0.15f : 0.08f) * warmth : (gun ? 0.03f : 0.08f) * warmth;
            if (c.Aggrieved == (sideA ? "a" : "b"))
                h += 0.08f;   // the one wronged pushes
            if (gun)
            {
                // a gun needs a gun; the law at hand, and the town's rule, hold the law-abiding back from it
                if (!armed)
                    return 0f;
                float law = 0f;
                if (c.LawNear)
                    law += 0.35f;
                if (c.GunsBanned)
                    law += 0.20f;
                if (law > 0f)
                    h -= law * Clamp(0.9f + 0.4f * t[Traits.Deference]);
                if (c.Place == "church")
                    h -= 0.3f;
                // the fair-fight rule: drawing on an unarmed man was murder in every witness's eyes; an armed one may fire back
                h -= otherArmed ? 0.05f * Clamp(0.5f - 0.5f * t[Traits.Risk]) : 0.10f * Clamp(0.5f + 0.3f * t[Traits.Compassion] + 0.3f * t[Traits.Honesty]);
                if (friendNear)
                    h -= 0.05f;   // a friend at the shoulder: less alone, less to prove
            }
            else if (c.LawNear)
                h -= 0.10f * Clamp(0.8f + 0.5f * t[Traits.Deference]);
            if (otherWoman && rung >= 1)
                h *= t[Traits.Compassion] < -0.5f && t[Traits.Anger] > 0.5f ? 0.33f : 0.1f;   // a man's hand against a woman
            // a soft cap: the hottest man still leaves room for the law, a friend and the other man to read
            return h <= 0f ? 0f : 1f - (float)Math.Exp(-h);
        }

        /// The pull down at `rung` for this one (side A or B): a chance 0..1 that they end it here.
        public static float Cool(Traits t, bool sideA, QuarrelContext c, int rung)
        {
            if (t == null || c == null)
                return 0.5f;
            bool drunk = sideA ? c.DrunkA : c.DrunkB, friendNear = sideA ? c.FriendOfA : c.FriendOfB;
            float k = rung == 0 ? 0.40f : rung == 1 ? 0.30f : rung == 2 ? 0.35f : 0.55f;   // a fight burns out; a gun drawn mostly ends in a standoff
            k += 0.10f * t[Traits.SelfControl] + 0.08f * t[Traits.Compassion] + 0.05f * t[Traits.Honesty] - 0.08f * t[Traits.Anger] + 0.05f * t[Traits.Anxiety];
            if (drunk)
                k -= 0.10f;
            if (friendNear)
                k += 0.15f;
            if (c.LawNear)
                k += 0.15f;
            if (c.Place == "church")
                k += 0.2f;
            if (c.Place == "saloon" && (c.Hour >= 20 || c.Hour < 4))
                k -= 0.05f;
            k -= 0.02f * Seen(c);   // backing down before a crowd costs
            k += 0.06f * Warmth(c);
            return Clamp(k);
        }

        /// How one who brings it down does it, from who they are and what is to hand.
        public static string WayDown(Traits t, bool friendStepped, QuarrelContext c, int rung, string seed)
        {
            if (friendStepped)
                return "friend";
            if (rung >= 2 && c.LawNear && Voice.Chance(seed + "|law", rung == 2 ? 50 : 60))
                return "law";   // the law breaks up a fistfight or a standoff in its sight
            if (rung == 2 && Voice.Chance(seed + "|beaten", 55))
                return "beaten";   // a fight ends with a man down more often than with a word
            if (t == null)
                return "walkoff";
            if (t[Traits.Compassion] + t[Traits.Honesty] > 0.6f && Voice.Chance(seed + "|sorry", 60))
                return "apology";
            if (t[Traits.Sociability] > 0.4f && rung <= 1 && Voice.Chance(seed + "|joke", 55))
                return "joke";
            if (c.Place == "saloon" && c.Warmth >= 0 && rung <= 1 && Voice.Chance(seed + "|drink", 40))
                return "drink";
            if (t[Traits.Anxiety] > 0.3f || t[Traits.Assertiveness] < -0.5f)
                return "backdown";
            return "walkoff";
        }

        // the rung goes before the word in every seed: Voice.Hash is FNV-1a, and two seeds that differ only in their last
        // character give rolls a fixed distance apart, so the draw rung's roll followed the blows rung's (found by probe, 17:09)
        /// The hotter side at a rung, ties by the seed.
        private static string Pusher(float ha, float hb, string seed, int rung)
        {
            return ha > hb ? "a" : hb > ha ? "b" : Voice.Chance(seed + "|" + rung + "|tie", 50) ? "a" : "b";
        }

        /// One step from `rung`: the hotter of the two pushes it up, damped by the other's cool, or a friend steps in, or
        /// the cooler brings it down. Nobody shoots who hasn't drawn; nobody draws without a gun; the shot is the one who
        /// pushed the draw to it. A rung past the last ends with the shot.
        public static Step Next(int rung, Traits a, Traits b, QuarrelContext c, string seed)
        {
            if (c == null)
                c = new QuarrelContext();
            rung = Math.Max(0, Math.Min(rung, Rungs.Length - 1));
            var s = new Step { From = Rungs[rung] };
            if (rung >= Rungs.Length - 1)
            {
                s.Ended = "shot";
                float sa = Heat(a, true, c, 3), sb = Heat(b, false, c, 3);
                s.By = sa <= 0f && sb <= 0f ? (c.ArmedA ? "a" : "b") : Pusher(sa, sb, seed, 3);   // the one who pushed the draw to a shot
                return s;
            }
            float ha = Heat(a, true, c, rung), hb = Heat(b, false, c, rung);
            float ka = Cool(a, true, c, rung), kb = Cool(b, false, c, rung);
            string pusher = Pusher(ha, hb, seed, rung);
            float up = Math.Max(ha, hb) * (1f - 0.5f * (pusher == "a" ? kb : ka));   // the other man's cool takes some of the heat out of it
            s.Chance = up;
            // a friend steps in before the hot one gets his way, when there is one and the quarrel is past words
            bool friend = (c.FriendOfA || c.FriendOfB) && rung >= 1 && Voice.Chance(seed + "|" + rung + "|friend", rung >= 2 ? 35 : 20);
            if (!friend && Voice.Hash(seed + "|" + rung + "|up") % 10000 < (int)(up * 10000f))
            {
                s.To = Rungs[rung + 1];
                s.By = pusher;
                return s;
            }
            string cooler = ka > kb ? "a" : kb > ka ? "b" : Voice.Chance(seed + "|" + rung + "|cool", 50) ? "a" : "b";
            s.Ended = WayDown(cooler == "a" ? a : b, friend, c, rung, seed + "|" + rung);
            if (friend)
                s.By = c.FriendOfA && c.FriendOfB ? (Voice.Chance(seed + "|" + rung + "|whose", 50) ? "friendA" : "friendB") : c.FriendOfA ? "friendA" : "friendB";
            else if (s.Ended == "law")
                s.By = "law";
            else if (s.Ended == "beaten")
                s.By = Pusher(Heat(a, true, c, 1), Heat(b, false, c, 1), seed, 1);   // the fists' heat, not the gun's; the game's own fight decides when wired
            else
                s.By = cooler;
            return s;
        }

        /// The whole ladder from words: every step until it ends (for the tests; the Director takes a rung at a time).
        public static List<Step> Run(Traits a, Traits b, QuarrelContext c, string seed)
        {
            var steps = new List<Step>(5);
            int rung = 0;
            for (int i = 0; i < Rungs.Length; i++)
            {
                Step s = Next(rung, a, b, c, seed);
                steps.Add(s);
                if (s.Ended != null)
                    break;
                rung++;
            }
            return steps;
        }

        /// The last rung a run reached, as its name.
        public static string Reached(List<Step> steps)
        {
            return steps == null || steps.Count == 0 ? "words" : steps[steps.Count - 1].From;
        }
    }
}
