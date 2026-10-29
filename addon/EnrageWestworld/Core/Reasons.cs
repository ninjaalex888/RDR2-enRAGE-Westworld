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
        public bool Taken;        // spoken for: a sweetheart, someone they're courting, an affair of enRAGE's
        public string Mood;       // their day: "sore", "glad" or null (InnerLife.MoodOf)
        public string Grief;      // the first name of a friend whose death they've had word of in the last three days, else null
        public long GriefAt;      // the game minute that word reached them
        public string GriefId;    // the dead friend's id (null when unknown)
        public Func<string, bool> HeardOf;   // whether word of this death (by the dead one's id) has reached them; null: not asked
        public bool Known;        // known lately for a face in the bar or a hand in a coat
    }

    public sealed class Reason
    {
        public string Kind;       // the scene to play: argue, brawl, flirt, propose, debt, smalltalk, gossip, hosts
        public int Strength;      // the strongest reason near the player wins
        public string Why;        // for the log and the F7 dump
        public bool BFirst;       // the second person starts it (A in the scene)
        public string Fallback;   // the scene instead, if the game side can't play this one (no surface by him for "rough")
        public string Matter;     // what the quarrel is over, when the reason knows (drink, money, honour, woman, land); null: the game side works it out (Scenes.Ladder.cs)
    }

    public static class Reasons
    {
        /// Game minutes before the same two people have another scene (about four real minutes).
        public const long PairRest = 120;
        public const long HostileRest = 45;   // a quarrel picks up again sooner than a friendship does (Lex: fights were too rare to see)

        /// Everything the place adds: is it a saloon, is it night, are there rumours to pass on, did the player do
        /// something in this town lately that people would talk about.
        public sealed class Place
        {
            public bool Saloon, Night, Rumours, DeedsHere;
            public bool Courting;   // [Romance] NpcFlirting: people make eyes at each other
            public bool SameSex;    // [Romance] SameSexRomance
            public float Trouble = 1f;   // [Scenes] TroubleWeight: every chance of blows or a quarrel scaled by it (Lex, 2026-10-28)
        }

        /// A percent chance scaled by the trouble weight, 0-100.
        public static int Weighted(int pct, float weight)
        {
            return Math.Max(0, Math.Min(100, (int)Math.Round(pct * weight)));
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
                long rest = tie.Kind == "brawl" ? 1440 : tie.Warmth < 0 ? HostileRest : PairRest;   // after a fight, a day before the next thing between them
                if (now - tie.At >= 0 && now - tie.At < rest)
                    return null;   // they've just had words, or a walk together
            }
            // one is the real person at the other end of the other's worry: the strongest reason there is (Links.cs)
            if (link != null && Links.Linkable(link.Kind))
            {
                string[] what = { "it begins", "it's worse", "it's at the worst", "it's ended" };
                int stage = InnerLife.StageFor(link.Id, gameDay);
                if (!(stage == 3 && link.Ended))   // its ending has been had out between them already
                    return new Reason { Kind = "x:" + link.Kind, Strength = 85, BFirst = link.Id == b.Id,
                                        Why = link.Name + "'s " + link.Kind + " with " + link.OtherName + " (" + what[stage] + ")" };
            }
            if (tie != null)
            {
                // sweethearts at odds since they last met: the cooler head comes to make it up, with a small gift, where
                // the street can see it (Scenes.Mend)
                if (Ties.Cooled(tie) && (mixed || where.SameSex) && where.Courting)
                {
                    if (Voice.Hash(PairKey(a, b) + tie.Count + "mend") % 100 >= Scenes.MendChance)
                        return null;   // at odds, and neither ready to say sorry yet: they keep apart for now
                    bool bBrings = Hot(a) && !Hot(b);
                    return new Reason { Kind = "mend", Strength = 64, BFirst = bBrings,
                                        Why = (bBrings ? b.Name : a.Name) + " comes to make it up with " + (bBrings ? a.Name : b.Name) + ", with a small gift" };
                }
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
                    bool hot = Hot(a) || Hot(b);
                    // the first quarrel can come to blows too, between two hot men or with drink in one of them
                    bool blows = twoMen && ((tie.Stage >= 1 && (hot || tie.Warmth <= -3 || tie.Stage >= 2)) || (Hot(a) && Hot(b)) || (hot && (a.Drunk || b.Drunk)));
                    // blows between a hothead (or a proud or a cold man) and the man he hates: sometimes he grabs him and
                    // smashes his face into whatever is at hand instead of trading punches
                    if (blows && (Rough(a.Temper) || Rough(b.Temper)) && Voice.Hash(a.Id + b.Id + tie.Count + "rough") % 100 < RoughChance)
                        return new Reason { Kind = "rough", Fallback = "brawl", Strength = 72 - 10 * tie.Warmth, BFirst = !Rough(a.Temper),
                                            Why = "their quarrel comes to blows, and " + (Rough(a.Temper) ? a.Name : b.Name) + " has the temper to do it rough" };
                    // the first time words; then it picks up again (and may be made up: Exchanges); two hot men, blows
                    return new Reason { Kind = blows ? "brawl" : tie.Stage >= 1 || tie.Kind != "argue" ? "x:quarrel" : "argue", Strength = 70 - 10 * tie.Warmth,
                                        Why = blows ? "their quarrel comes to blows" : "their quarrel picks up again" };
                }
                // a friend of someone grieving (word of a friend's death in the last three days) offers sympathy, the first
                // time they meet after the news reached them (Scenes.Condole; the grieving one is A)
                if (Ties.Friends(tie) && (a.Grief != null || b.Grief != null))
                {
                    Someone grieving = a.Grief != null ? a : b, friend = grieving == a ? b : a;
                    // the friend must have had the word too: nobody offers sympathy for a death they haven't heard of
                    bool friendKnows = grieving.GriefId == null || friend.HeardOf == null || friend.HeardOf(grieving.GriefId);
                    if (tie.At < grieving.GriefAt && friendKnows)
                        return new Reason { Kind = "condole", Strength = 62, BFirst = grieving == b, Why = friend.Name + " offers " + grieving.Name + " sympathy for " + grieving.Grief };
                }
                // a friend (or sweetheart) of someone who has just been roughed up or robbed goes to them: a small kindness
                if (Ties.Friends(tie) && (a.Hurt != null || b.Hurt != null))
                {
                    Someone hurt = a.Hurt != null ? a : b, friend = hurt == a ? b : a;
                    return new Reason { Kind = "tend", Strength = 58, BFirst = friend == b, Why = friend.Name + " goes to " + hurt.Name + ", who was just " + (hurt.Hurt == "rough" ? "roughed up" : "robbed") };
                }
                if (tie.Warmth > 0 && (mixed || where.SameSex) && Ties.Sweethearts(tie))
                {
                    bool ask = tie.Warmth >= 2 && tie.Stage >= 2 && !where.Night;
                    // now and then sweethearts have words: a hot head, or a sore day, in one of them (Scenes.Tiff)
                    bool aCross = Hot(a) || a.Mood == "sore", bCross = Hot(b) || b.Mood == "sore";
                    if (!ask && where.Courting && Scenes.TiffNow(aCross || bCross, tie.Stage, PairKey(a, b) + tie.Count))
                        return new Reason { Kind = "tiff", Strength = 60, BFirst = !aCross,
                                            Why = "sweethearts, and " + (aCross ? a.Name : b.Name) + " has a grievance" };
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

        /// The two ids in a fixed order, so a roll between them is the same whichever comes first in a scan.
        private static string PairKey(Someone a, Someone b)
        {
            return string.CompareOrdinal(a.Id, b.Id) < 0 ? a.Id + "|" + b.Id : b.Id + "|" + a.Id;
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
                bool hot = Hot(x);
                // a hot man at the worst of it snaps at a man who won't take it: it comes to blows
                if (hot && twoMen && Hot(y) && Voice.Chance(x.Id + gameDay + "snapblows", Weighted(35, where.Trouble)))   // was 20 (Lex, 2026-10-28: brawls often)
                    all.Add(new Reason { Kind = "brawl", Strength = 56, Why = x.Name + " is at the worst of it (" + kind + ") and snaps at " + y.Name + ", who won't take it" });
                else
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
            // drink and temper in a saloon, any hour (at night only until Lex asked for saloon fights by day too, 2026-10-28):
            // two men, one of them hot, one of them drunk, come to blows (a coward only gets words); now and then the hot
            // one does it rough
            if (twoMen && where.Saloon && Hot(x) && (x.Drunk || y.Drunk) && Voice.Chance(x.Id + gameDay + "drinktemper", Weighted(DrinkTemperChance, where.Trouble)))
            {
                if (y.Temper == "Coward")
                    all.Add(new Reason { Kind = "argue", Strength = 46, Matter = "drink", Why = x.Name + " has drink and temper in him, and " + y.Name + " won't stand up to it" });
                else if (Voice.Hash(x.Id + y.Id + gameDay + "drough") % 100 < RoughChance)
                    all.Add(new Reason { Kind = "rough", Fallback = "brawl", Strength = 57, Matter = "drink", Why = x.Name + " has drink and temper in him, and does it rough" });
                else
                    all.Add(new Reason { Kind = "brawl", Strength = 56, Matter = "drink", Why = x.Name + " has drink and temper in him, and " + y.Name + " answers it" });
            }
            else if (x.Temper == "Hothead" && twoMen && where.Saloon && where.Trouble > 0f)
            {
                // now and then a hothead with drink in him doesn't stop at words with whoever is in his way; at night he
                // wins the turn over most things (Lex: fights were too rare to see)
                int night = where.Night ? 14 : 0;
                if (Voice.Hash(x.Id + gameDay + "srough") % 100 < Weighted(StrangerRoughChance, where.Trouble))
                    all.Add(new Reason { Kind = "rough", Fallback = "argue", Strength = 32 + night, Matter = "drink", Why = x.Name + " is a hothead with drink in him, and " + y.Name + " is in his way" });
                else
                    all.Add(new Reason { Kind = "argue", Strength = 30 + night, Matter = "drink", Why = x.Name + " is a hothead with drink in him" });
            }
            // two hot-blooded men in each other's way come to blows now and then, by day or night (Lex, 2026-10-28: brawls
            // weren't happening; a reason always came first, so the chance meeting's brawl seldom got a turn). A roll per
            // pair and day, until the minds choose it themselves; strong enough to come before a snap or gossip
            // Two hot men (or a hot and a cold one) at the full chance; one hot man at half; two ordinary men at a quarter
            // (Lex, 2026-10-28, at the highest weight in Valentine: no fights, since two hot men together are rare in a
            // thin town and the weight had no pair to work on); never a coward
            if (twoMen && x.Temper != "Coward" && y.Temper != "Coward")
            {
                bool hx = Hot(x), hy = Hot(y);
                int full = where.Saloon ? HotBloodSaloonChance : HotBloodStreetChance;
                int pct = hx && (hy || y.Temper == "Cold") || hy && x.Temper == "Cold" ? full : hx || hy ? full / 2 : full / 4;
                if (Voice.Chance(PairKey(x, y) + gameDay + "hotblood", Weighted(pct, where.Trouble)))
                    all.Add(new Reason { Kind = "brawl", Strength = where.Saloon ? 50 : 47,
                                         Why = hx && hy ? x.Name + " and " + y.Name + " are both hot-blooded, and neither will give way"
                                             : hx || hy ? (hx ? x.Name : y.Name) + " has a temper, and " + (hx ? y.Name : x.Name) + " won't be pushed"
                                             : x.Name + " and " + y.Name + " rub each other the wrong way, and neither will give way" });
            }
            // someone free and taken with the one beside them makes eyes at them; the answer is the other's own
            Reason court = Courtship(x, y, gameDay, where);
            if (court != null)
                all.Add(court);
            Reason best = null;
            foreach (Reason r in all)
                if (best == null || r.Strength > best.Strength)
                    best = r;
            return best;
        }

        /// Percent of days a free person, taken with someone beside them, tries their luck (from both ids and the day).
        public const int CourtChance = 12;

        /// x, free, drawn to y and not against them (x's own answer, judged as the player is: Interest, a yes or a maybe:
        /// it takes some nerve, but a maybe is enough to try), tries their luck; y answers by what y makes of x. Kind "court:answer:why". Never anyone spoken for or under age, nor more
        /// than 15 years apart; only between people with no tie yet (Between calls Own only then).
        public static Reason Courtship(Someone x, Someone y, long gameDay, Place where)
        {
            if (where == null || !where.Courting || x == null || y == null || x.Id == y.Id || x.P == null || y.P == null || x.Taken)
                return null;
            int ax = Romance.AgeOf(x.P), ay = Romance.AgeOf(y.P);
            if (ax < 18 || ay < 18 || System.Math.Abs(ax - ay) > 15 || !Romance.DrawnTo(x.Id, x.Woman, y.Woman, where.SameSex))
                return null;
            if (Voice.Hash(x.Id + "|" + y.Id + "|" + gameDay + "|court") % 100 >= CourtChance)
                return null;   // not today
            string mine, why;
            if (Interest.Answer(ViewOf(x, y, gameDay, where), out mine) == "no")
                return null;   // x isn't taken with y
            string answer = Interest.Answer(ViewOf(y, x, gameDay, where), out why);
            // a romance beginning outweighs an everyday snap (45); a rebuff is a smaller thing to see
            return new Reason { Kind = "court:" + answer + ":" + why, Strength = answer == "yes" ? 52 : answer == "maybe" ? 48 : 42,
                                Why = x.Name + " is taken with " + y.Name + ", and " + y.Name + (answer == "yes" ? " is with " + x.Name + " too" : answer == "maybe" ? " isn't sure" : " isn't (" + why + ")") };
        }

        /// How "me" sees "them" as a sweetheart (Interest.Ask), as the player is seen: no tie yet, so a stranger; their
        /// worry and mood, their nature; them dressed like money or not, known lately for trouble or not.
        public static Interest.Ask ViewOf(Someone me, Someone them, long gameDay, Place where)
        {
            string kind = null;
            int stage = -1;
            bool good;
            bool worry = me.P != null && InnerLife.WorryNow(me.P, gameDay, out kind, out stage, out good);
            return new Interest.Ask
            {
                Regard = "stranger", Drawn = Romance.DrawnTo(me.Id, me.Woman, them.Woman, where != null && where.SameSex), Taken = me.Taken, AtWorst = worry && stage == 2,
                Temper = me.Temper, Mood = me.Mood, Who = me.P, Worry = worry ? kind : null, Cash = them.WellOff ? 600 : 150, BadHere = them.Known ? 1 : 0,
                Saloon = where != null && where.Saloon
            };
        }

        private const string Desperate = "is desperate for money";

        /// Percent of fistfights between enemies that a rough man turns into a face into the table instead.
        public const int RoughChance = 45;
        public const int DrinkTemperChance = 50;   // a hothead, a night: drink and temper in the saloon come to blows (rolled per man, not per pair); 35 until Lex asked for brawls often (2026-10-28)

        /// Quick to anger: a hothead or a proud man.
        public static bool Hot(Someone s)
        {
            return s != null && (s.Temper == "Hothead" || s.Temper == "Proud");
        }
        /// Percent of a saloon hothead's run-ins with a stranger that go that far.
        public const int StrangerRoughChance = 15;
        /// Percent of a day's meetings between two hot-blooded men that come to blows (2026-10-28).
        public const int HotBloodSaloonChance = 35, HotBloodStreetChance = 15;

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
                (kind != "debt" && kind != "savings" && kind != "bank" && kind != "drink" && kind != "cards"))
                return null;
            bool honest = x.Temper == "Loyal" || x.Temper == "Coward" || x.P.Has("honest") || x.P.Facet("Honesty") >= 60;
            if (!honest || Voice.Hash(x.Id + gameDay + "beg") % 100 >= 35)
                return null;
            return kind == "drink" ? "has drunk the rent" : kind == "cards" ? "has gambled the rent away" : "has come to the end of the money";
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
                         (kind == "debt" || kind == "savings" || kind == "bank" || kind == "drink" || kind == "cards");
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
