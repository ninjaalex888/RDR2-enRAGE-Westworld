// Why two people near the player would have something to do with each other right now, from who they are: what is
// between them already (a SceneTie, and how far its story has gone), the worry each is living through (InnerLife),
// their temperament, whether they're waking, and what the player did in their town. The strongest reason picks the
// scene and who starts it; with no reason there is no scene (the game side keeps a rare chance meeting for a town
// where nobody has a history yet). A pair's story moves on each time they meet instead of repeating: a quarrel goes
// on, then comes to blows between two hot-tempered men; courting goes on until one of them asks.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    /// One person near the player, as the reasons see them.
    public sealed class Someone
    {
        public string Id, Name, Temper;
        public bool Woman;
        public Person P;          // for the worry they're living through
        public int WakeStage;
    }

    public sealed class Reason
    {
        public string Kind;       // the scene to play: argue, brawl, flirt, propose, debt, smalltalk, gossip, hosts
        public int Strength;      // the strongest reason near the player wins
        public string Why;        // for the log and the F7 dump
        public bool BFirst;       // the second person starts it (A in the scene)
    }

    public static class Reasons
    {
        /// Game minutes before the same two people have another scene (about four real minutes).
        public const long PairRest = 120;

        /// Everything the place adds: is it a saloon, is it night, are there rumours to pass on, did the player do
        /// something in this town lately that people would talk about.
        public sealed class Place
        {
            public bool Saloon, Night, Rumours, DeedsHere;
        }

        /// The strongest reason between these two, either way round; null if they have none (or had their scene
        /// together too lately).
        public static Reason Between(Someone a, Someone b, SceneTie tie, long gameDay, long now, Place where)
        {
            if (a == null || b == null || a.Id == b.Id)
                return null;
            bool mixed = a.Woman != b.Woman, twoMen = !a.Woman && !b.Woman;
            if (tie != null)
            {
                long rest = tie.Kind == "brawl" ? 1440 : PairRest;   // after a fight, a day before the next thing between them
                if (now - tie.At >= 0 && now - tie.At < rest)
                    return null;   // they've just had words, or a walk together
                if (tie.Warmth < 0)
                {
                    bool hot = a.Temper == "Hothead" || b.Temper == "Hothead" || a.Temper == "Proud" || b.Temper == "Proud";
                    bool blows = twoMen && tie.Stage >= 1 && (hot || tie.Warmth <= -3 || tie.Stage >= 2);
                    // the first time words; then it picks up again (and may be made up: Exchanges); two hot men, blows
                    return new Reason { Kind = blows ? "brawl" : tie.Stage >= 1 || tie.Kind != "argue" ? "x:quarrel" : "argue", Strength = 70 - 10 * tie.Warmth,
                                        Why = blows ? "their quarrel comes to blows" : "their quarrel picks up again" };
                }
                if (tie.Warmth > 0 && mixed && Ties.Romantic(tie.Kind))
                {
                    bool ask = tie.Warmth >= 2 && tie.Stage >= 2 && !where.Night;
                    return new Reason { Kind = ask ? "propose" : tie.Stage == 0 ? "flirt" : "x:sweet", Strength = 60 + 10 * tie.Warmth,
                                        Why = ask ? "they've been courting long enough for one to ask" : "they're sweet on each other" };
                }
                if (tie.Warmth > 0)
                    return new Reason { Kind = "smalltalk", Strength = 35, Why = "they're on good terms" };
                return new Reason { Kind = where.Rumours ? "gossip" : "smalltalk", Strength = 30, Why = "they know each other" };
            }
            Reason ab = Own(a, b, gameDay, where, mixed, twoMen), ba = Own(b, a, gameDay, where, mixed, twoMen);
            if (ba != null && (ab == null || ba.Strength > ab.Strength))
            {
                ba.BFirst = !ba.BFirst;   // b's own reason: b's side of it comes first
                return ba;
            }
            return ab;
        }

        /// A reason that comes from one person's own life (x), with the other (y) as whoever is there.
        private static Reason Own(Someone x, Someone y, long gameDay, Place where, bool mixed, bool twoMen)
        {
            string kind;
            int stage;
            bool good;
            bool worry = InnerLife.WorryNow(x.P, gameDay, out kind, out stage, out good);
            // owing money, and somebody they owe close by: the other one calls it in (A in the debt scene)
            if (worry && kind == "debt" && stage >= 1 && stage <= 2)
                return new Reason { Kind = "debt", Strength = 50, BFirst = true, Why = x.Name + " owes money, and it's pressing" };
            if (worry && kind == "courting" && mixed && stage <= 1)
                return new Reason { Kind = "flirt", Strength = 45, Why = x.Name + " is sweet on somebody" };
            // at the worst of it: spoken to, they snap, and say what's wrong (Exchanges)
            if (worry && stage == 2)
            {
                bool hot = x.Temper == "Hothead" || x.Temper == "Proud";
                return new Reason { Kind = "x:snap", Strength = hot ? 55 : 45, Why = x.Name + " is at the worst of it (" + kind + ")" };
            }
            if (x.WakeStage >= 2 && y.WakeStage >= 1)
                return new Reason { Kind = "hosts", Strength = 45, Why = "both are waking" };
            if (worry && stage == 3)
                return new Reason { Kind = good ? "x:goodnews" : "x:badnews", Strength = 42, Why = x.Name + "'s " + kind + " ended " + (good ? "well" : "badly") + ", and they say so" };
            if (where.DeedsHere && where.Rumours)
                return new Reason { Kind = "gossip", Strength = 40, Why = "the town is talking about what the stranger did" };
            if (x.Temper == "Hothead" && twoMen && where.Saloon)
                return new Reason { Kind = "argue", Strength = 30, Why = x.Name + " is a hothead with drink in him" };
            return null;
        }
    }
}
