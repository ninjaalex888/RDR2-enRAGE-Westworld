// The ladder a quarrel climbs or comes down (roadmap 7; Lex, 2026-10-27: an argument may go all the way to a killing when
// the people's inner lives, histories and surroundings lead there, true to 1899, where most killings were sudden quarrels
// over drink, honour, money or land, at close range, often in a saloon at night between men with history, and many towns
// banned guns inside the limits; and de-escalation by feeling too: an apology, a joke, backing down, walking off, a friend
// stepping in, a drink offered). Built in the night run of 2026-10-28 (after item 14), Core only: nothing in the game
// calls it yet. The rungs are words, a shove, blows, a gun drawn, a shot; at each the hotter of the two pushes up or the
// cooler brings it down, from their traits (anger, self-control, assertiveness, risk, compassion, deference to the law),
// drink, what it's over, who is watching (face), the history between them, a friend at hand, the law at hand, the town's
// gun rule, and whether either carries a gun. Seeded, so the same quarrel between the same two in the same place goes the
// same way. Plain C#, no game. mindtest EscalationTest holds the numbers.
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
        public bool LawNear;                 // a lawman within sight
        public bool GunsBanned;              // the town's rule: no guns inside the limits
        public bool FriendOfA, FriendOfB;    // a friend at hand who might step in
        public int Witnesses;                // who is watching (face is lost before a crowd)
        public int Warmth;                   // the pair's tie, -3..3 (friends cool, foes heat)
        public int History;                  // quarrels between them before
        public string Matter = "nothing";    // drink honour money land woman nothing
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
            public string By;           // "a", "b" or "friend": who pushed it up, or who brought it down
            public override string ToString() { return Ended != null ? From + " -> ended: " + Ended + " (" + By + ")" : From + " -> " + To + " (" + By + ")"; }
        }

        private static float Clamp(float v) { return v < 0f ? 0f : v > 1f ? 1f : v; }

        /// How far this one wants to take it from `rung`: a chance 0..1 of pushing to the next.
        public static float Heat(Traits t, bool drunk, bool armed, bool friendNear, QuarrelContext c, int rung)
        {
            if (t == null || rung >= Rungs.Length - 1)
                return 0f;
            // each rung is harder than the last: a shove is cheap, a fist costs, a gun is another world, the shot the last step of it
            float h = rung == 0 ? 0.30f : rung == 1 ? 0.42f : rung == 2 ? 0.03f : 0.12f;
            bool gun = rung >= 2;
            float nature = gun ? 0.6f : 1f;   // a man's nature moves his fists more than his gun hand; the gun rungs are mostly the surroundings
            h += nature * (0.12f * t[Traits.Anger] + 0.07f * t[Traits.Assertiveness] + 0.07f * t[Traits.Risk] - 0.12f * t[Traits.SelfControl] - 0.05f * t[Traits.Compassion]);
            if (drunk)
                h += gun ? 0.03f : 0.15f;   // drink loosens the fists more than the trigger
            float matter = gun ? 0.5f : 1f;   // what it's over moves the fists more than the gun
            switch (c.Matter)
            {
                case "honour": case "woman": h += 0.10f * matter; break;
                case "land": case "money": h += 0.05f * matter; break;
                case "drink": h += 0.03f * matter; break;
            }
            // face: before a crowd the proud can't back down (from words and a shove; a gun is past face)
            int seen = Math.Min(c.Witnesses, 6);
            if (!gun && seen > 0 && t[Traits.Assertiveness] > 0f)
                h += 0.015f * seen * Math.Min(1.5f, t[Traits.Assertiveness]);
            h += (gun ? 0.02f : 0.04f) * Math.Min(c.History, 4);
            // a friend's words sting less, and nobody draws on a friend; a foe's sting more
            h -= c.Warmth > 0 ? (gun ? 0.15f : 0.08f) * c.Warmth : (gun ? 0.03f : 0.08f) * c.Warmth;
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
            }
            else if (c.LawNear)
                h -= 0.10f * Clamp(0.8f + 0.5f * t[Traits.Deference]);
            if (friendNear && rung >= 2)
                h -= 0.05f;   // a friend at the shoulder: less alone, less to prove
            return Clamp(h);
        }

        /// The pull down at `rung` for this one: a chance 0..1 that they end it here.
        public static float Cool(Traits t, bool drunk, bool friendNear, QuarrelContext c, int rung)
        {
            if (t == null)
                return 0.5f;
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
            k += 0.06f * c.Warmth;
            return Clamp(k);
        }

        /// How one who brings it down does it, from who they are and what is to hand.
        public static string WayDown(Traits t, bool friendStepped, QuarrelContext c, int rung, string seed)
        {
            if (friendStepped)
                return "friend";
            if (rung == 2 && Voice.Chance(seed + "|beaten", 55))
                return "beaten";   // a fight ends with a man down more often than with a word
            if (rung >= 3 && c.LawNear && Voice.Chance(seed + "|law", 60))
                return "law";
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

        /// One step from `rung`: the hotter of the two pushes it up, or the cooler brings it down. Nobody shoots who
        /// hasn't drawn; nobody draws without a gun; a quarrel between two who are neither hot nor cool enough
        /// ends with a walk-off.
        public static Step Next(int rung, Traits a, Traits b, QuarrelContext c, string seed)
        {
            var s = new Step { From = Rungs[rung] };
            if (rung >= Rungs.Length - 1)
            {
                s.Ended = "shot";
                s.By = c.ArmedA && !c.ArmedB ? "a" : c.ArmedB && !c.ArmedA ? "b" : Voice.Chance(seed + "|who", 50) ? "a" : "b";
                return s;
            }
            float ha = Heat(a, c.DrunkA, c.ArmedA, c.FriendOfA, c, rung), hb = Heat(b, c.DrunkB, c.ArmedB, c.FriendOfB, c, rung);
            float ka = Cool(a, c.DrunkA, c.FriendOfA, c, rung), kb = Cool(b, c.DrunkB, c.FriendOfB, c, rung);
            string pusher = ha >= hb ? "a" : "b";
            float up = Math.Max(ha, hb) * (1f - 0.5f * Math.Min(ka, kb));   // the cooler one can still take the heat out of it
            // a friend steps in before the hot one gets his way, when there is one and the quarrel is past words
            bool friend = (c.FriendOfA || c.FriendOfB) && rung >= 1 && Voice.Chance(seed + "|friend" + rung, rung >= 2 ? 35 : 20);
            if (!friend && Voice.Chance(seed + "|up" + rung, (int)Math.Round(up * 100f)))
            {
                s.To = Rungs[rung + 1];
                s.By = pusher;
                return s;
            }
            string cooler = ka >= kb ? "a" : "b";
            s.Ended = WayDown(cooler == "a" ? a : b, friend, c, rung, seed + "|" + rung);
            s.By = friend ? "friend" : s.Ended == "beaten" ? pusher : cooler;
            return s;
        }

        /// The whole ladder from words: every step until it ends.
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
