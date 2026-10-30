// The Director reads the quarrel ladder (roadmap 7; the night run of 2026-10-28, item 16; Core/Escalation.cs and
// Core/Scenes.Ladder.cs). Behind [Mind] MindLive and [Mind] QuarrelLadder (on): when a street scene between two people
// the mind knows would be an argue or a brawl, the ladder decides how far it goes and how it ends, from their traits
// (the shadow's cache) and the surroundings as the game reads them here: the place and the hour, who is in drink, the
// guns on their belts, who is a woman, a lawman within 30 m, a friend of either within 10 m who is free to step in (he
// joins as the cast's third when the ladder picks him; he needs a clear view of A), the crowd watching, the pair's tie and its history, who was
// wronged, and what it's over (the reason's matter, a worry between them, or the place's usual). The scene is played
// through the Director's acts as today (shove, fight, aim, holster); the guns come out only where BuildCast allows them
// (both armed, no law within 45 m, not a saloon), else the draw is a stare-down over a hand at the belt; the shot is never
// played (Scenes.Ladder.cs: the standoff breaks off, and the log says the ladder said a shot). With [Scenes] Standoffs
// off nobody draws. Off (MindLive or QuarrelLadder), exactly the fixed scenes. Log LADDER; F7 TEST ladder:.
using System;
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private int _ladderScenes, _ladderShots, _ladderFriends, _ladderGunsHeld, _ladderSkipped;
        private readonly Dictionary<string, int> _ladderTops = new Dictionary<string, int>();
        private readonly Dictionary<string, int> _ladderWays = new Dictionary<string, int>();
        private string _ladderLast, _ladderWhyNot;
        private int _ladderTestTurn, _ladderTests;
        private bool _ladderFailed;

        private bool LadderOn { get { return _cfg.MindLive && _cfg.QuarrelLadder; } }

        /// The argue or brawl `old` rebuilt from the ladder when the mind knows both; else `old` as it was. `people` is
        /// the scene's cast so far (the two): a friend who steps in is added to it. `matter`: the reason's, or null.
        private Scene LadderScene(Scene old, Ped first, Ped second, Someone doer, Someone done, SceneCast cast, string seed, string matter, List<Ped> people, int now, bool force = false)
        {
            Scene s = LadderSceneOf(old, first, second, doer, done, cast, seed, matter, people, now, force);
            // the ladder came to blows between two who are sat or leaning: the chair keeps them (Lex, 2026-10-30), so the
            // quarrel stays words
            if (s != null && s != old && s.Fight && (PED.IS_PED_USING_ANY_SCENARIO(first.Handle) || PED.IS_PED_USING_ANY_SCENARIO(second.Handle)))
            {
                Log.Write("LADDER would come to blows, but one of them is sat or leaning: words only");
                return old.Fight ? Scenes.Argue(cast, seed) : old;
            }
            return s;
        }

        private Scene LadderSceneOf(Scene old, Ped first, Ped second, Someone doer, Someone done, SceneCast cast, string seed, string matter, List<Ped> people, int now, bool force)
        {
            if (old == null || (!LadderOn && !force) || _ladderFailed || (old.Kind != "argue" && old.Kind != "brawl") || old.Cast != 2 || people == null || people.Count != 2 || _engine == null || _store == null)
                return old;
            try
            {
                _ladderWhyNot = null;
                Traits ta = LadderTraits(first.Handle, now), tb = LadderTraits(second.Handle, now);
                if (ta == null || tb == null)
                {
                    _ladderSkipped++;
                    _ladderWhyNot = "the mind doesn't know " + (ta == null ? cast.Names[0] ?? "the first" : cast.Names[1] ?? "the second") + " yet";
                    Log.Detail("LADDER left to the fixed scene: " + _ladderWhyNot);
                    // the reason let a woman's fight through for the ladder to judge (Reasons.MayFight, MindDecides); without it,
                    // the old coin decides, as before (the advisor, 2026-10-30)
                    if (old.Kind == "brawl" && (cast.AWoman || cast.BWoman) && !force &&
                        !Voice.Chance(seed + "|womanfight", (cast.AWoman ? Reasons.WomanFightPercent : 100) * (cast.BWoman ? Reasons.WomanFightPercent : 100) / 100))
                        return Scenes.Argue(cast, seed);
                    return old;
                }
                string aId = _mindWho[first.Handle].Id, bId = _mindWho[second.Handle].Id;
                SceneTie tie = _store.Ties.FirstOrDefault(t => t.Has(aId) && t.Has(bId));
                Vector3 mid = (first.Position + second.Position) * 0.5f;
                Ped me = Game.Player.Character;
                bool saloon = cast.Saloon;
                var q = new QuarrelContext
                {
                    Place = saloon ? "saloon" : _placeTown != null ? "street" : "road",
                    Hour = CLOCK.GET_CLOCK_HOURS(),
                    DrunkA = doer != null ? doer.Drunk : InHisCups(first, _mindWho[first.Handle].P),
                    DrunkB = done != null ? done.Drunk : InHisCups(second, _mindWho[second.Handle].P),
                    ArmedA = WEAPON.IS_PED_ARMED(first.Handle, 4),
                    ArmedB = WEAPON.IS_PED_ARMED(second.Handle, 4),
                    WomanA = cast.AWoman,
                    WomanB = cast.BWoman,
                    WomenFight = _cfg.WomenInFights,
                    LawNear = LawmanNear(first, 30f),
                    GunsBanned = false,   // no town's rule is known to the add-on (For Lex)
                    Warmth = tie != null ? tie.Warmth : 0,
                    History = tie != null && tie.Warmth < 0 ? Math.Min(4, tie.Count) : 0,
                    Aggrieved = tie != null && tie.Doer != null && Ties.Trouble(tie.Kind) ? (tie.Doer == aId ? "b" : tie.Doer == bId ? "a" : null) : null   // the trouble kinds only: a debt's or a brush-off's doer (item 17) wrongs nobody the ladder should favour
                };
                if (!_cfg.Standoffs)
                    q.ArmedA = q.ArmedB = false;   // [Scenes] Standoffs off: nobody draws, so the ladder stops at the fists
                // who is watching: everyone within 12 m of them but the two, the player among them when he's that close
                q.Witnesses = World.GetAllPeds().Count(p => p != null && p.Exists() && !p.IsDead && PED.IS_PED_HUMAN(p.Handle) && p.Handle != first.Handle && p.Handle != second.Handle &&
                                                             p.Handle != me.Handle && p.Position.DistanceTo(mid) < 12f) + (me.Position.DistanceTo(mid) < 12f ? 1 : 0);
                // what it's over: the reason's matter, else a debt or a feud between them, else the place's usual
                if (matter == null)
                {
                    // a debt between them is money only when A is the one owed (the words have A as the creditor); a feud is
                    // honour; courting between them never makes a woman the matter (they'd be quarrelling over her with each
                    // other); strangers owe each other nothing and share no fence (the review)
                    WorryLink link = _store.Links.FirstOrDefault(l => (l.Id == aId && l.OtherId == bId) || (l.Id == bId && l.OtherId == aId));
                    if (link != null && link.Kind == "debt" && link.Id == bId)
                        matter = "money";
                    else if (link != null && link.Kind == "feud")
                        matter = "honour";
                    if (matter == null)
                        matter = Scenes.MatterOf(saloon, q.DrunkA || q.DrunkB, seed, cast.Knows != null && (!cast.Knows[0] || !cast.Knows[1]));
                }
                q.Matter = matter;
                // a friend of either within 10 m who could step in (as the stand-up finds one): the warmer side's
                Candidate friend = LadderFriend(people, cast, mid, aId, bId, now, out int side);
                cast.Friend = friend != null ? side : -1;
                if (friend != null)
                {
                    q.FriendOfA = side == 0;
                    q.FriendOfB = side == 1;
                    cast.Names[2] = Person.FirstOf(friend.Who.Name);
                    cast.Tempers[2] = friend.Who.Temper;
                    cast.CWoman = friend.Who.Woman;
                }
                int start = old.Kind == "brawl" ? Scenes.BrawlStartRung : 0;
                List<Escalation.Step> steps;
                Scene s = Scenes.Quarrel(cast, seed, ta, tb, q, start, out steps);
                if (s.Cast == 3)
                {
                    people.Add(friend.Ped);
                    _ladderFriends++;
                }
                else if (friend != null)
                {
                    cast.Friend = -1;
                    cast.Names[2] = null;
                    cast.Tempers[2] = null;
                    cast.CWoman = false;
                }
                if (force)
                    _ladderTests++;
                string top = Escalation.Reached(steps);
                bool drew = top == "draw" || top == "shoot";
                string guns = drew && !(cast.Armed0 || cast.Armed1) ? "; guns stay holstered (" + (saloon ? "a saloon" : LawmanNear(first, 45f) ? "the law within 45 m" : "not both armed") + ")" : "";
                if (guns.Length > 0)
                    _ladderGunsHeld++;
                if (s.LadderNote != null)
                    _ladderShots++;
                _ladderScenes++;
                _ladderTops[top] = (_ladderTops.ContainsKey(top) ? _ladderTops[top] : 0) + 1;
                string way = steps[steps.Count - 1].Ended ?? "?";
                _ladderWays[way] = (_ladderWays.ContainsKey(way) ? _ladderWays[way] : 0) + 1;
                string an = cast.Names[0] ?? "A", bn = cast.Names[1] ?? "B";
                string run = string.Join(" -> ", steps.Select(st => st.Ended != null ? "ended: " + st.Ended + (st.Ended == "beaten" ? " (the fists decide)" : " (" + LadderSide(st.By, an, bn) + ")")
                                                                                     : st.To + " (" + LadderSide(st.By, an, bn) + ")"));
                string ctx = q.Place + " at " + q.Hour + (q.DrunkA && q.DrunkB ? ", both in drink" : q.DrunkA ? ", " + an + " in drink" : q.DrunkB ? ", " + bn + " in drink" : "") +
                             (q.ArmedA && q.ArmedB ? ", both armed" : q.ArmedA ? ", " + an + " armed" : q.ArmedB ? ", " + bn + " armed" : ", unarmed") + (q.LawNear ? ", the law near" : "") +
                             ", " + q.Witnesses + " watching" + (tie != null ? ", " + (tie.Warmth < 0 ? "foes" : tie.Warmth > 0 ? "friends" : "acquainted") + (q.History > 0 ? " (" + q.History + " quarrels before)" : "") : ", strangers") +
                             (q.Aggrieved != null ? ", " + (q.Aggrieved == "a" ? an : bn) + " the wronged" : "") + (friend != null ? ", a friend of " + (side == 0 ? an : bn) + " by" : "") + (_ladderFriendMind != null ? (friend != null ? " (the mind: " : ", no friend stepped in (the mind: ") + _ladderFriendMind + ")" : "") +
                             ", over " + matter + (start > 0 ? ", from the " + Escalation.Rungs[start] : "");
                _ladderLast = an + " & " + bn + ": " + Escalation.Rungs[start] + " -> " + run + " (" + s.Kind + ")";
                Log.Write("LADDER " + an + " & " + bn + " (" + ctx + "): " + Escalation.Rungs[start] + " -> " + run + "; played as " + (s.Kind == "argue" ? "an argue" : "a " + s.Kind) +
                          (old.Kind != s.Kind ? " (the fixed scene was " + (old.Kind == "argue" ? "an argue" : "a brawl") + ")" : "") + guns + (s.LadderNote != null ? "; " + s.LadderNote : ""));
                return s;
            }
            catch (Exception ex)
            {
                _ladderFailed = true;
                Log.Write("LADDER failed once, the fixed scenes from here: " + ex.Message);
                return old;
            }
        }

        private static string LadderSide(string by, string a, string b)
        {
            return by == "a" ? a : by == "b" ? b : by == "friendA" ? a + "'s friend" : by == "friendB" ? b + "'s friend" : by ?? "?";
        }

        /// The traits the shadow holds for this body, worked out now if it knows them but hasn't yet; null for nobody.
        private Traits LadderTraits(int h, int now)
        {
            // every quarrel from the mind (Lex, 2026-10-30: "it can break out between any 2+ NPCs if their inner thoughts lead
            // to it"): a body the cache last saw as nobody is looked at afresh (Nimrod Hatch, known since the morning, was
            // "not known yet" from a look ten seconds stale), and a passer-by who can be one of ours is made one on the spot
            MindWho w = MindKnown(h, now);
            if (w != null && w.Id == null)
            {
                Ped p = new Ped(h);
                if (_bridge.RecordAt(h) == null && _bridge.AcquaintanceAt(h) == null && ResidentEligible(p))
                    ResidentOf(p);
                _mindWho.Remove(h);
                w = MindKnown(h, now);
            }
            if (w == null || w.Id == null || w.Src == null || w.Model != ENTITY.GET_ENTITY_MODEL(h))
                return null;
            return w.Traits ?? (w.Traits = _engine.Shadow.TraitsOf(w.Src));
        }

        /// A friend of A or B watching within 10 m who could step in: named, free, on his feet, with a clear view of the
        /// pair, no first name shared with either (the labels must tell three apart); the warmer tie first. `side`: 0 a
        /// friend of A, 1 of B.
        private Candidate LadderFriend(List<Ped> people, SceneCast cast, Vector3 mid, string aId, string bId, int now, out int side)
        {
            side = -1;
            _ladderFriendMind = null;
            Ped me = Game.Player.Character;
            string an = cast.Names[0], bn = cast.Names[1];
            Candidate best = null;
            int bestWarmth = 0;
            bool mind = MindStandUpOn && !_standUpMindFailed;
            var friends = mind ? new List<Candidate>() : null;
            foreach (Candidate c in ReasonCandidates(me, now))
            {
                if (c.Ped.Handle == people[0].Handle || c.Ped.Handle == people[1].Handle || c.Who.Id == aId || c.Who.Id == bId || c.Ped.Position.DistanceTo(mid) >= 10f)
                    continue;
                string fn = Person.FirstOf(c.Who.Name);
                if ((an != null && fn == an) || (bn != null && fn == bn))
                    continue;
                int toA = LadderWarmth(c.Who.Id, aId), toB = LadderWarmth(c.Who.Id, bId);
                int warmth = Math.Max(toA, toB);
                if (warmth <= 0 || (friends == null && warmth <= bestWarmth))
                    continue;   // with the mind, every friend in reach is weighed, not only the warmest so far (the collision audit)
                if (PED.IS_PED_USING_ANY_SCENARIO(c.Ped.Handle) || !ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(c.Ped.Handle, people[0].Handle, 17))
                    continue;
                if (friends != null)
                    friends.Add(c);
                if (warmth <= bestWarmth)
                    continue;
                best = c;
                bestWarmth = warmth;
                side = toA >= toB ? 0 : 1;
            }
            if (friends != null && friends.Count > 0)
            {
                // with MindLive and MindStandUp, the friend who steps in is one whose own mind says to (Core/StepIn.cs, the
                // stand-up's road): the strongest of them; a friend whose mind only says watch doesn't, so no friend's rung
                string why;
                Candidate moved = LadderFriendByMind(people, friends, mid, aId, bId, now, out why);
                if (!_standUpMindFailed)
                {
                    _ladderFriendMind = why;
                    if (moved == null)
                    {
                        side = -1;
                        return null;
                    }
                    int toA = LadderWarmth(moved.Who.Id, aId), toB = LadderWarmth(moved.Who.Id, bId);
                    side = toA >= toB ? 0 : 1;
                    return moved;
                }
            }
            return best;
        }

        private string _ladderFriendMind;

        /// Of the friends near a quarrel, the one whose own appraisal of it (a brawl between his friend and another, Core/
        /// StepIn.Takes) says to step in, the strongest; null when none does. Only people the mind already knows.
        private Candidate LadderFriendByMind(List<Ped> people, List<Candidate> friends, Vector3 mid, string aId, string bId, int now, out string why)
        {
            why = null;
            try
            {
                var watching = new List<Onlooker>();
                var byHandle = new Dictionary<int, Candidate>();
                foreach (Candidate c in friends)
                {
                    MindWho w = MindKnown(c.Ped.Handle, now);
                    if (w == null || w.Id == null || w.Id != c.Who.Id || w.P == null)
                        continue;
                    Traits t = w.Traits ?? (w.Traits = _engine.Shadow.TraitsOf(w.Src));
                    if (t == null)
                        continue;
                    watching.Add(new Onlooker { P = w.P, T = t, Metres = c.Ped.Position.DistanceTo(mid), Drunk = c.Who.Drunk, WakeStage = c.Who.WakeStage, Handle = c.Ped.Handle });
                    byHandle[c.Ped.Handle] = c;
                }
                if (watching.Count == 0)
                {
                    why = "the mind knows none of the " + friends.Count + " friends near";
                    return null;
                }
                Someone a = SomeoneOf(people[0]), b = SomeoneOf(people[1]);
                Happening h = StepIn.Trouble("brawl", aId, a != null ? a.Name : null, bId, b != null ? b.Name : null, _placeTown, _engine.NowIndoors ? _engine.NowPlace : null, _store.LastGameMin, watching);
                List<StepInTake> takes = StepIn.Takes(h, watching, LadderWarmth, CrowdAt(mid, 12f, people), LawmanNear(people[0], 30f), aId + "|" + bId + "|" + _store.LastGameMin / 60);
                StepInTake took = StepIn.Who(takes);
                why = took != null ? StepIn.Words(took) : string.Join("; ", takes.Take(3).Select(StepIn.Words));
                return took != null ? byHandle[took.Who.Handle] : null;
            }
            catch (Exception ex)
            {
                _standUpMindFailed = true;
                Log.Write("LADDER the mind's part of the friend failed once, the warmest friend from here: " + ex.Message);
                why = null;
                return null;
            }
        }

        private int LadderWarmth(string x, string y)
        {
            if (x == null || y == null)
                return 0;
            SceneTie t = _store.Ties.FirstOrDefault(tt => tt.Has(x) && tt.Has(y));
            return t != null ? t.Warmth : 0;
        }

        private string LadderStatus()
        {
            if (!_cfg.MindLive || !_cfg.QuarrelLadder)
                return "off ([Mind] MindLive" + (_cfg.MindLive ? "" : " is off") + (_cfg.QuarrelLadder ? "" : "; QuarrelLadder is off") + "): the fixed argue and brawl scenes";
            if (_ladderFailed)
                return "FAILED once this session (see the log); the fixed scenes since";
            return _ladderScenes + " quarrel" + (_ladderScenes == 1 ? "" : "s") + " from the ladder" +
                   (_ladderScenes > 0 ? " (reached: " + string.Join(", ", _ladderTops.Select(kv => kv.Key + " " + kv.Value)) + "; ended by: " + string.Join(", ", _ladderWays.Select(kv => kv.Key + " " + kv.Value)) + ")" : "") +
                   ", a friend stepped in " + _ladderFriends + ", guns held " + _ladderGunsHeld + ", the shot said and not played " + _ladderShots + ", left to the fixed scene " + _ladderSkipped + (_ladderTests > 0 ? " (" + _ladderTests + " of them from the Testing row)" : "") +
                   (_ladderWhyNot != null ? " (last: " + _ladderWhyNot + ")" : "") + (_ladderLast != null ? "; last: " + _ladderLast : "");
        }
    }
}
