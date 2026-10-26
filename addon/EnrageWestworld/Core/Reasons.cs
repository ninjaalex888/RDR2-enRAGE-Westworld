// Why two people near the player would have something to do with each other right now, from who they are: what is
// between them already (a SceneTie, and how far its story has gone), the worry each is living through (InnerLife),
// their temperament, whether they're waking, and what the player did in their town. The strongest reason picks the
// scene and who starts it; with no reason there is no scene (the game side keeps a rare chance meeting for a town
// where nobody has a history yet). A pair's story moves on each time they meet instead of repeating: a quarrel goes
// on, then comes to blows between two hot-tempered men; courting goes on until one of them asks.
// The trouble the game shows between people comes from here too (Scenes.Trouble.cs): a grudge between two men, or a
// hothead with drink in him, can end in one smashing the other's face into the nearest table or bar ("rough"; the game
// side checks there's a surface to hand, else it stays a fistfight or words); someone short of money or light of
// finger robs a well-off or drunk mark ("lift" in passing, "pickpocket" when the mark catches it at once); a drunk in a
// saloon at night calls a man out ("drunk"). Each from who they are, never just from who is standing closest.
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
        public bool WellOff;      // dressed like money: a gentleman, a lady (a mark worth robbing)
        public bool Drunk;        // in his cups: slumped at the bar, drinking himself silly (a mark, or a man who calls people out)
        public string Hurt;       // just had it done to them (the last hour and a half): "rough" (a face in the bar) or "robbed", else null
    }

    public sealed class Reason
    {
        public string Kind;       // the scene to play: argue, brawl, flirt, propose, debt, smalltalk, gossip, hosts
        public int Strength;      // the strongest reason near the player wins
        public string Why;        // for the log and the F7 dump
        public bool BFirst;       // the second person starts it (A in the scene)
        public string Fallback;   // the scene instead, if the game side can't play this one (no surface by him for "rough")
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
        public static Reason Between(Someone a, Someone b, SceneTie tie, long gameDay, long now, Place where, WorryLink link = null)
        {
            if (a == null || b == null || a.Id == b.Id)
                return null;
            bool mixed = a.Woman != b.Woman, twoMen = !a.Woman && !b.Woman;
            if (tie != null)
            {
                long rest = tie.Kind == "brawl" ? 1440 : PairRest;   // after a fight, a day before the next thing between them
                if (now - tie.At >= 0 && now - tie.At < rest)
                    return null;   // they've just had words, or a walk together
            }
            // one is the real person at the other end of the other's worry: the strongest reason there is (Links.cs)
            if (link != null && Links.Linkable(link.Kind))
            {
                string[] what = { "it begins", "it's worse", "it's at the worst", "it's ended" };
                int stage = InnerLife.StageFor(link.Id, gameDay);
                return new Reason { Kind = "x:" + link.Kind, Strength = 85, BFirst = link.Id == b.Id,
                                    Why = link.Name + "'s " + link.Kind + " with " + link.OtherName + " (" + what[stage] + ")" };
            }
            if (tie != null)
            {
                // the one it was done to (a face in the bar, a hand in his coat) sees the one who did it: the timid
                // keep clear of him from then on; the rest have it out, as with any other quarrel (below)
                if (tie.Warmth < 0 && (tie.Kind == "rough" || tie.Kind == "pickpocket" || tie.Kind == "avoid") && tie.Doer != null)
                {
                    Someone victim = tie.Doer == a.Id ? b : tie.Doer == b.Id ? a : null;
                    if (victim != null && Exchanges.Manner(victim, false) == "kind")
                        return new Reason { Kind = "avoid", Strength = 50, BFirst = tie.Doer == b.Id,
                                            Why = victim.Name + " keeps clear of " + (victim == a ? b.Name : a.Name) + " since that business" };
                }
                if (tie.Warmth < 0)
                {
                    bool hot = a.Temper == "Hothead" || b.Temper == "Hothead" || a.Temper == "Proud" || b.Temper == "Proud";
                    bool blows = twoMen && tie.Stage >= 1 && (hot || tie.Warmth <= -3 || tie.Stage >= 2);
                    // blows between a hothead (or a proud or a cold man) and the man he hates: sometimes he grabs him and
                    // smashes his face into whatever is at hand instead of trading punches
                    if (blows && (Rough(a.Temper) || Rough(b.Temper)) && Voice.Hash(a.Id + b.Id + tie.Count + "rough") % 100 < RoughChance)
                        return new Reason { Kind = "rough", Fallback = "brawl", Strength = 72 - 10 * tie.Warmth, BFirst = !Rough(a.Temper),
                                            Why = "their quarrel comes to blows, and " + (Rough(a.Temper) ? a.Name : b.Name) + " has the temper to do it rough" };
                    // the first time words; then it picks up again (and may be made up: Exchanges); two hot men, blows
                    return new Reason { Kind = blows ? "brawl" : tie.Stage >= 1 || tie.Kind != "argue" ? "x:quarrel" : "argue", Strength = 70 - 10 * tie.Warmth,
                                        Why = blows ? "their quarrel comes to blows" : "their quarrel picks up again" };
                }
                // a friend (or sweetheart) of someone who has just been roughed up or robbed goes to them: a small kindness
                if (tie.Warmth > 0 && (a.Hurt != null || b.Hurt != null))
                {
                    Someone hurt = a.Hurt != null ? a : b, friend = hurt == a ? b : a;
                    return new Reason { Kind = "tend", Strength = 58, BFirst = friend == b, Why = friend.Name + " goes to " + hurt.Name + ", who was just " + (hurt.Hurt == "rough" ? "roughed up" : "robbed") };
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
            // every reason that fits, and the strongest wins (the first of equals, so the old order still decides ties)
            var all = new List<Reason>();
            // a debt is called in, and courting done, only with the real person at the other end (a link, above): never
            // with whoever happens to be standing there, which would contradict their own thoughts. At the worst of it
            // (or any other worry): spoken to, they snap, and say what's wrong (Exchanges)
            if (worry && stage == 2)
            {
                bool hot = x.Temper == "Hothead" || x.Temper == "Proud";
                all.Add(new Reason { Kind = "x:snap", Strength = hot ? 55 : 45, Why = x.Name + " is at the worst of it (" + kind + ")" });
            }
            if (x.WakeStage >= 2 && y.WakeStage >= 1)
                all.Add(new Reason { Kind = "hosts", Strength = 45, Why = "both are waking" });
            if (worry && stage == 3)
                all.Add(new Reason { Kind = good ? "x:goodnews" : "x:badnews", Strength = 42, Why = x.Name + "'s " + kind + " ended " + (good ? "well" : "badly") + ", and they say so" });
            if (where.DeedsHere && where.Rumours)
                all.Add(new Reason { Kind = "gossip", Strength = 40, Why = "the town is talking about what the stranger did" });
            // a thief and a mark: someone short of money, or light of finger by nature, near someone worth robbing;
            // driven by money trouble at its worst it outweighs even a temper
            string need = Thief(x, gameDay);
            if (need != null && (y.WellOff || y.Drunk) && x.Id != y.Id)
            {
                bool caught = !x.Woman && !y.Drunk && Voice.Hash(x.Id + y.Id + gameDay + "caught") % 100 < (Alert(y.Temper) ? 55 : 30) - (x.Temper == "Drifter" ? 20 : 0);
                all.Add(new Reason { Kind = caught ? "pickpocket" : "lift", Fallback = caught ? "lift" : null, Strength = need == Desperate && worry && stage == 2 ? 47 : 43,
                                     Why = x.Name + " " + need + ", and " + y.Name + (y.Drunk ? " is in his cups" : " looks like money") + (caught ? "; the mark feels it" : "") });
            }
            // the same want in an honest person: they ask instead of taking (the well-off, never the drunk)
            string asks = Beggar(x, gameDay);
            if (asks != null && y.WellOff && !y.Drunk && x.Id != y.Id && need == null)
                all.Add(new Reason { Kind = "beg", Strength = 41, Why = x.Name + " " + asks + ", and asks " + y.Name + " for a coin" });
            // a drunk in a saloon at night calls a man out, and falls down before a blow is struck
            if (x.Drunk && twoMen && where.Saloon && where.Night && x.Temper != "Coward")
                all.Add(new Reason { Kind = "drunk", Strength = 44, Why = x.Name + " is drunk and spoiling for a fight" });
            if (x.Temper == "Hothead" && twoMen && where.Saloon)
            {
                // now and then a hothead with drink in him doesn't stop at words with whoever is in his way
                if (Voice.Hash(x.Id + y.Id + gameDay + "srough") % 100 < StrangerRoughChance)
                    all.Add(new Reason { Kind = "rough", Fallback = "argue", Strength = 32, Why = x.Name + " is a hothead with drink in him, and " + y.Name + " is in his way" });
                else
                    all.Add(new Reason { Kind = "argue", Strength = 30, Why = x.Name + " is a hothead with drink in him" });
            }
            Reason best = null;
            foreach (Reason r in all)
                if (best == null || r.Strength > best.Strength)
                    best = r;
            return best;
        }

        private const string Desperate = "is desperate for money";

        /// Percent of fistfights between enemies that a rough man turns into a face into the table instead.
        public const int RoughChance = 45;
        /// Percent of a saloon hothead's run-ins with a stranger that go that far.
        public const int StrangerRoughChance = 15;

        /// A temper that grabs a man and uses the furniture on him.
        public static bool Rough(string temper)
        {
            return temper == "Hothead" || temper == "Proud" || temper == "Cold";
        }

        /// Quick to feel a hand in his coat: the proud, the cold, the hot-tempered.
        public static bool Alert(string temper)
        {
            return temper == "Proud" || temper == "Cold" || temper == "Hothead";
        }

        /// Why this person would ask a stranger for money today, or null: money trouble, and too honest (or too loyal,
        /// or too timid) to steal. Not every day: a day is an asking day for some of them, from their id and the day.
        public static string Beggar(Someone x, long gameDay)
        {
            if (x == null || x.P == null || x.Temper == "Greedy" || x.Temper == "Proud")
                return null;   // the greedy take, and the proud would sooner starve than ask
            string kind;
            int stage;
            bool good;
            if (!InnerLife.WorryNow(x.P, gameDay, out kind, out stage, out good) || stage < 1 || stage > 2 ||
                (kind != "debt" && kind != "savings" && kind != "bank" && kind != "drink"))
                return null;
            bool honest = x.Temper == "Loyal" || x.Temper == "Coward" || x.P.Has("honest") || x.P.Facet("Honesty") >= 60;
            if (!honest || Voice.Hash(x.Id + gameDay + "beg") % 100 >= 35)
                return null;
            return kind == "drink" ? "has drunk the rent" : "has come to the end of the money";
        }

        /// Why this person would pick a pocket today, or null: by nature (greedy, a drifter, dishonest), or driven
        /// to it by money trouble at its worst. Never the loyal or the honest; most days not even the light-fingered
        /// (a day is a thieving day for a few of them, from their id and the day, so it isn't every time they pass).
        public static string Thief(Someone x, long gameDay)
        {
            if (x == null || x.Temper == "Loyal" || (x.P != null && x.P.Has("honest")))
                return null;
            string kind;
            int stage;
            bool good;
            bool money = x.P != null && InnerLife.WorryNow(x.P, gameDay, out kind, out stage, out good) && stage >= 1 && stage <= 2 &&
                         (kind == "debt" || kind == "savings" || kind == "bank" || kind == "drink");
            bool nature = x.Temper == "Greedy" || x.Temper == "Drifter" || (x.P != null && x.P.Facet("Honesty") < 30);
            if (!money && !nature)
                return null;
            int chance = (money ? 30 : 0) + (nature ? 25 : 0);
            if (Voice.Hash(x.Id + gameDay + "thief") % 100 >= chance)
                return null;
            return money ? Desperate : "has light fingers";
        }
    }
}
