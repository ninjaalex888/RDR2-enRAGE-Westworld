// Who joins a fistfight already going, and on whose side (the NPC-life run, Lex 2026-10-30: a brawl "can break out between
// any 2+ NPCs if their inner thoughts lead to it"; and before that, after the bully test: crowds joining, taking sides,
// chains of reactions). The mind doc's crowd rule holds: most watch, few join, fewer the bigger the crowd; the ones who do
// are a fighter's friends, the ones who hate a fighter, and the hot-headed and the drunk, a saloon's most of all. Nobody
// joins with the law at hand. A man's hand against a woman at a tenth of his rate, as the quarrel ladder has it (Lex's
// word, 2026-10-30: 1899); a woman joins only with WomenInFights, at half. Each joiner is a fighter like the first two,
// so a friend of the man he goes for may join in turn: that is how a fight spreads. Seeded, so the same people in the same
// fight answer the same way. Plain C#, no game; the game layer (WestworldScript.JoinIn.cs) asks it every few seconds.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    /// One onlooker's answer: the side they take (0: with the first fighter's side, against the other; 1 the reverse),
    /// whom they go for, and why, for the log.
    public sealed class JoinTake
    {
        public int Side = -1;
        public int Target = -1;     // index into that side's opponents
        public float Score, Bar;
        public string Why;
    }

    /// Someone already in the fight, as the onlooker sees them.
    public sealed class Fighter
    {
        public string Id;          // null: nobody the mind knows
        public bool Woman;
    }

    public static class JoinIn
    {
        /// At most this many join one fight (two who started it and these: a saloon brawl, not a riot).
        public const int MostJoiners = 3;

        /// Whether this onlooker joins, and on whose side. sides[0] and sides[1]: who is fighting on each side; warmth(x, y):
        /// the tie between two ids (-3..3, 0 none); witnesses: how many watch; saloon: a saloon's fights draw the drunk and
        /// the hot; joined: how many have joined already.
        public static JoinTake Weigh(string id, Traits t, bool woman, bool drunk, IList<Fighter>[] sides, Func<string, string, int> warmth,
                                     int witnesses, bool lawNear, bool saloon, bool womenFight, int joined, string seed)
        {
            var no = new JoinTake();
            if (t == null || sides == null || sides.Length != 2 || joined >= MostJoiners)
                return no;
            if (lawNear)
            {
                no.Why = "the law at hand";
                return no;
            }
            if (woman && !womenFight)
            {
                no.Why = "a woman, and WomenInFights is off";
                return no;
            }
            float Z(int i) => t.Z[i];
            // the heat they bring to any fight: temper, the push to act, the taste for risk, drink; held back by self-control
            float heat = 0.10f * Z(Traits.Anger) + 0.07f * Z(Traits.Assertiveness) + 0.05f * Z(Traits.Risk) - 0.08f * Z(Traits.SelfControl) + (drunk ? 0.12f : 0f) + (saloon ? 0.04f : 0f);
            JoinTake best = no;
            for (int s = 0; s < 2; s++)
            {
                IList<Fighter> mine = sides[s], theirs = sides[1 - s];
                if (theirs.Count == 0)
                    continue;
                int friend = 0, foe = 0, target = 0;   // the one they hate most over there, else the one who started on that side
                foreach (Fighter f in mine)
                    if (f.Id != null && id != null)
                        friend = Math.Max(friend, warmth(id, f.Id));
                for (int i = 0; i < theirs.Count; i++)
                {
                    int w = theirs[i].Id != null && id != null ? warmth(id, theirs[i].Id) : 0;
                    if (w < foe)
                    {
                        foe = w;
                        target = i;
                    }
                }
                // with nobody hated over there, a man goes for a man before a woman when he can
                if (foe == 0 && !woman && theirs[target].Woman)
                    for (int i = 0; i < theirs.Count; i++)
                        if (!theirs[i].Woman)
                        {
                            target = i;
                            break;
                        }
                float tie = friend > 0 ? 0.22f + 0.08f * friend : 0f;
                if (foe < 0)
                    tie = Math.Max(tie, 0.18f + 0.08f * -foe);
                float score = tie + heat;
                // a stranger to both sides must clear the crowd's bar and more; a friend or a foe is moved by their own
                float bar = tie > 0f ? 0.25f : Appraisal.CrowdBar(witnesses) + 0.25f;
                if (score <= bar)
                {
                    if (best.Side < 0 && score > best.Score)
                        best = new JoinTake { Score = score, Bar = bar, Why = Words(friend, foe, heat, drunk) + ": not enough to join" };
                    continue;
                }
                float chance = Math.Min(0.8f, (score - bar) * 2.5f);
                if (woman)
                    chance *= 0.5f;   // half as often as a man, as for a quarrel (WomenInFights)
                if (!woman && theirs[target].Woman)
                    chance *= 0.1f;   // a man's hand against a woman, at a tenth (1899; Lex, 2026-10-30)
                bool goes = Voice.Chance(seed + "|join|" + s, (int)Math.Round(chance * 100));
                var take = new JoinTake { Side = goes ? s : -1, Target = target, Score = score, Bar = bar, Why = Words(friend, foe, heat, drunk) + " (" + (int)Math.Round(chance * 100) + "%)" };
                if (goes && (best.Side < 0 || score > best.Score))
                    best = take;
                else if (best.Side < 0 && score > best.Score)
                    best = take;
            }
            return best;
        }

        private static string Words(int friend, int foe, float heat, bool drunk)
        {
            var w = new List<string>();
            if (friend > 0)
                w.Add("a friend on that side");
            if (foe < 0)
                w.Add("bad blood with the other");
            if (heat >= 0.15f)
                w.Add("hot-headed");
            if (drunk)
                w.Add("drunk");
            return w.Count > 0 ? string.Join(", ", w) : "no stake in it";
        }
    }
}
