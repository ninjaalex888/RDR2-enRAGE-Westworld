// Standing up to a bully ([Scenes] Kindnesses; the choice and the words are Core/Scenes.StandUp.cs): when a rough or a
// brawl is built, someone named watching within 10 m may step in before the blow: a friend of the one being picked on,
// or a steady man who can't abide it. The bully backs off (the scene ends as words) or waves them off and goes on. The
// one who stepped in is the scene's third (C), so the Director plays them like any bystander. When they speak (the
// scene's "cb:stoodup"), the ties are noted (defend with the one they stood up for, faced with the bully) and the
// three remember it. Pacing "standup" (45%, an hour between, the same person twice a day at most).
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
        private sealed class StandUpNote
        {
            public Ped C, A, B;
            public string CName, AName, BName;
            public bool Backs, Test, Rough, TookAway;
        }

        private StandUpNote _standUp;
        private int _standUps, _standUpBacked, _standUpTestTurn;
        private string _standUpLast, _standUpWhyNot;

        /// Someone steps into this rough or brawl before the blow, or nobody does. True if they did (the scene now has
        /// three, and the words to go with it).
        private bool StandUpFor(Scene s, List<Ped> people, SceneCast cast, string seed, int now, bool test, out string why)
        {
            why = null;
            if (!_cfg.Kindnesses)
            {
                why = "[Scenes] Kindnesses is off";
                return false;
            }
            if ((s.Kind != "rough" && s.Kind != "brawl") || s.Cast != 2 || people.Count != 2)
            {
                why = "not a rough or a brawl between two";
                return false;
            }
            Ped me = Game.Player.Character;
            Vector3 mid = (people[0].Position + people[1].Position) * 0.5f;
            Someone a = SomeoneOf(people[0]), b = SomeoneOf(people[1]);
            if (a == null || b == null)
            {
                why = "the two aren't people we know";
                return false;
            }
            string an = cast.Names[0] ?? Person.FirstOf(a.Name), bn = cast.Names[1] ?? Person.FirstOf(b.Name);
            long g = _store.LastGameMin;
            // those who could step in: within 10 m of the pair, not sat at a table or leaning on the bar (they'd have to get
            // up first), with B in sight, a first name unlike the two's; a friend of B first, then the nearest
            List<Candidate> near = ReasonCandidates(me, now, test).Where(x => x.Ped.Handle != people[0].Handle && x.Ped.Handle != people[1].Handle && x.Who.Id != a.Id && x.Who.Id != b.Id &&
                                                                              x.Ped.Position.DistanceTo(mid) < 10f && Person.FirstOf(x.Who.Name) != an && Person.FirstOf(x.Who.Name) != bn &&
                                                                              !PED.IS_PED_USING_ANY_SCENARIO(x.Ped.Handle) && ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(x.Ped.Handle, people[1].Handle, 17))
                                                                 .OrderByDescending(x => TieWarmth(x.Who.Id, b) > 0 ? 1 : 0).ThenBy(x => x.Ped.Position.DistanceTo(mid)).ToList();
            // with MindLive and MindStandUp, who steps in comes from their own minds (Core/StepIn.cs); else the old roll
            bool mind = MindStandUpOn && !_standUpMindFailed;
            int crowd = mind ? CrowdAt(mid, StandUpCrowdMetres, people) : 0;   // the crowd and the law only for the mind (the review: two scans thrown away otherwise)
            string place = _engine.NowIndoors ? _engine.NowPlace : null;
            bool law = mind && LawmanNear(people[0], 25f);
            Candidate by = null;
            StepInTake took = null;
            string mindWhy = null;
            if (mind)
            {
                by = StandUpByMind(s.Kind, a, b, near, mid, crowd, place, law, now, seed, out took, out mindWhy);
                if (_standUpMindFailed)
                    mind = false;   // its part failed (logged once): the roll, as before
            }
            bool testPick = false;
            if (by == null && (!mind || test))
                foreach (Candidate c in near)
                {
                    int toB = TieWarmth(c.Who.Id, b), toA = TieWarmth(c.Who.Id, a);
                    if (test ? c.Who.Temper != "Coward" && toB >= 0 && toA <= 0 : Scenes.StandsUp(toB, toA, c.Who.Temper, c.Who.Woman, seed + "|" + c.Who.Id))
                    {
                        by = c;
                        testPick = mind;   // a test the mind found nobody for takes the test's own pick
                        break;
                    }
                }
            if (by == null)
            {
                if (mind)
                    _standUpNobody++;
                why = _standUpWhyNot = mind ? "nobody's own mind moved them to step in (" + mindWhy + ")" : "nobody watching who'd step in";
                return false;
            }
            string skipped;
            // the mind decides, so its pick isn't rolled again (Lex, 2026-10-29); the rests still hold
            string pace = Pacing.WhyNot(_store.Pace, "standup", _placeTown, by.Who.Id, g, took != null ? 0 : Voice.Hash(seed + "|standup") % 100, test, out skipped);
            if (pace != null)
            {
                why = _standUpWhyNot = "rests: " + pace;
                return false;
            }
            // whether the bully backs down: from his own mind when it knows him (anger holds his ground, fear or shame backs
            // him down), else the old roll; a test takes turns, the fight first (Lex, 2026-10-28: a plain bully backs off
            // three times in four, so a test seldom showed the fight it was fixed for), and says what his mind would do
            bool mindBacks = false;
            string bullyMind = null;
            bool bullyKnown = mind && BullyFaced(people[0], a, by, crowd, place, law, now, out mindBacks, out bullyMind);
            bool backs = test ? _standUpTestTurn++ % 2 == 1 : bullyKnown ? mindBacks : Scenes.BacksOff(a.Temper, by.Who.Temper, by.Who.Woman, seed);
            bool rough = s.Kind == "rough";
            bool far = by.Ped.Position.DistanceTo(people[0].Position) > 3.5f;
            // with the mind deciding: when the bully holds his ground, the one who stepped in may take the one picked on away
            // (a test takes turns: the fight first, then the backing off, as before)
            bool cFriend = TieWarmth(by.Who.Id, b) > 0;
            bool takeAway = took != null && !backs && !test && Scenes.TakesAway(cFriend, seed + "|" + by.Who.Id);
            cast.Names[2] = Person.FirstOf(by.Who.Name);
            cast.Tempers[2] = by.Who.Temper;
            cast.CWoman = by.Who.Woman;
            if (!Scenes.StandUp(s, cast, seed, backs, far, takeAway, cFriend))
            {
                cast.Names[2] = null;
                cast.Tempers[2] = null;
                why = _standUpWhyNot = "no blow in this scene to stand between";
                return false;
            }
            people.Add(by.Ped);
            if (took != null)
                _standUpByMind++;   // counted once the scene has it (tests too)
            if (bullyKnown && !test)
                _standUpBullyMind++;
            Pacing.Note(_store.Pace, "standup", _placeTown, by.Who.Id, g);
            _standUp = new StandUpNote { C = by.Ped, A = people[0], B = people[1], CName = by.Who.Name, AName = a.Name, BName = b.Name, Backs = backs, Test = test, Rough = rough, TookAway = takeAway };
            _standUpLast = by.Who.Name + " for " + b.Name + " against " + a.Name + ": " + (backs ? "he backed off" : takeAway ? "they took " + Person.FirstOf(b.Name) + " away" : "he went on") + (test ? " (a test)" : "");
            Log.Write("KIND stood up: " + by.Who.Name + " steps in for " + b.Name + " against " + a.Name + " -> " + (backs ? "backs off" : takeAway ? "holds his ground, so they take " + Person.FirstOf(b.Name) + " away" : "tells them to keep out") + " (" +
                      (TieWarmth(by.Who.Id, b) > 0 ? "a friend of " + Person.FirstOf(b.Name) : "a " + (by.Who.Temper ?? "plain") + " " + (by.Who.Woman ? "woman" : "man")) + ", " + s.Kind + ")" +
                      (took != null ? "; the mind: " + mindWhy : testPick ? "; the mind: nobody would (" + mindWhy + "), so the test's own pick" : "") +
                      (bullyKnown ? "; " + Person.FirstOf(a.Name) + "'s mind: " + bullyMind : "") +
                      (test ? ", a test (it takes turns: the fight goes on, then he backs off)" + (skipped != null ? " (skipped: " + skipped + ")" : "") : ""));
            return true;
        }

        private const float StandUpCrowdMetres = 15f;   // everyone this near the trouble watches it, whether or not RealNPCs could move them
        private int _standUpByMind, _standUpNobody, _standUpBullyMind, _standUpTookAway;
        private bool _standUpMindFailed;

        /// Who steps in comes from their own minds: with [Mind] MindLive and [Mind] MindStandUp (roadmap 39).
        private bool MindStandUpOn { get { return _cfg.MindLive && _cfg.MindStandUp; } }

        /// Who steps in by their own minds (Core/StepIn.cs): each of those near enough whom the mind already knows takes the
        /// trouble as the shadow would once it had happened, and the strongest whose urge is to step in, and whose face may
        /// show it, does. Null when nobody would (`why` says what the first few felt). The mind's part failing is logged
        /// once and leaves the roll from then on.
        private Candidate StandUpByMind(string kind, Someone a, Someone b, List<Candidate> near, Vector3 mid, int crowd, string place, bool law, int now, string seed, out StepInTake took, out string why)
        {
            took = null;
            why = null;
            try
            {
                var watching = new List<Onlooker>();
                var byHandle = new Dictionary<int, Candidate>();
                foreach (Candidate c in near)
                {
                    MindWho w = MindKnown(c.Ped.Handle, now);   // only people the add-on already knows (never makes anyone)
                    if (w != null && w.Id == null && c.Who.Id != null)
                    {
                        // made a resident a moment ago by this same scan, while the cache still had them as nobody (the review): looked up again
                        _mindWho.Remove(c.Ped.Handle);
                        w = MindKnown(c.Ped.Handle, now);
                    }
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
                    why = near.Count == 0 ? crowd + " watching, none of them near enough and free" : "the mind knows none of the " + near.Count + " near enough";
                    return null;
                }
                Happening h = StepIn.Trouble(kind, a.Id, a.Name, b.Id, b.Name, _placeTown, place, _store.LastGameMin, watching);
                List<StepInTake> takes = StepIn.Takes(h, watching, _engine.WarmthBetween, crowd, law, seed);
                took = StepIn.Who(takes);
                if (took == null)
                {
                    why = crowd + " watching: " + string.Join("; ", takes.Take(3).Select(StepIn.Words)) + (takes.Count > 3 ? "; and " + (takes.Count - 3) + " more" : "");
                    return null;
                }
                why = StepIn.Words(took) + "; " + StepIn.Willing(takes) + " of the " + takes.Count + " the mind knows would, " + crowd + " watching";
                return byHandle[took.Who.Handle];
            }
            catch (Exception ex)
            {
                _standUpMindFailed = true;
                Log.Write("KIND the mind's part of standing up failed once, the old roll from here: " + ex.Message);
                took = null;
                why = "the mind's part failed";
                return null;
            }
        }

        /// How the bully takes being faced, by his own mind (Core/StepIn.Faced): anger holds his ground, fear or shame backs
        /// him down. False when the mind doesn't know him (then the old roll).
        private bool BullyFaced(Ped bully, Someone a, Candidate by, int crowd, string place, bool law, int now, out bool backs, out string words)
        {
            backs = false;
            words = null;
            try
            {
                MindWho w = MindKnown(bully.Handle, now);
                if (w == null || w.Id == null || w.Id != a.Id || w.P == null)
                    return false;
                Traits t = w.Traits ?? (w.Traits = _engine.Shadow.TraitsOf(w.Src));
                if (t == null)
                    return false;
                Person byP = by.Who.P ?? new Person { Id = by.Who.Id, Name = by.Who.Name, Woman = by.Who.Woman };
                Feeling f = StepIn.Faced(w.P, t, byP, _engine.WarmthBetween, crowd, place, _placeTown, law, a.Drunk, a.WakeStage);
                if (f == null)
                    return false;
                backs = StepIn.BacksDown(f);
                words = f.Kind + (f.Kind != "none" ? " (" + f.Strength + ")" : "") + ", urge " + f.Urge + ": " + (backs ? "backs down" : "holds his ground");
                return true;
            }
            catch (Exception ex)
            {
                _standUpMindFailed = true;
                Log.Write("KIND the mind's part of standing up failed once, the old roll from here: " + ex.Message);
                return false;
            }
        }

        /// How many living people are within this many metres of a spot, but for these and the player: the crowd watching.
        private static int CrowdAt(Vector3 at, float metres, List<Ped> not)
        {
            int n = 0, me = Game.Player.Character.Handle;
            foreach (Ped p in World.GetAllPeds())
            {
                int h = p.Handle;
                if (h == me || p.Position.DistanceTo(at) > metres || ENTITY.IS_ENTITY_DEAD(h) || !PED.IS_PED_HUMAN(h))
                    continue;
                bool theirs = false;
                foreach (Ped x in not)
                    if (x != null && x.Handle == h)
                        theirs = true;
                if (!theirs)
                    n++;
            }
            return n;
        }

        /// The moment they speak up (the scene's "cb:stoodup"): the ties and the memories, between people who can be
        /// met again; a test notes nothing.
        private void StoodUp()
        {
            StandUpNote n = _standUp;
            _standUp = null;
            if (n == null)
                return;
            _standUps++;
            if (n.Backs || n.TookAway)
            {
                if (n.Backs)
                    _standUpBacked++;
                else
                    _standUpTookAway++;
                // it came to words: the two who squared up are noted now (not at the start: a walk-over that never got
                // there leaves nothing), and sit the fight's rest out, so "this ain't over" isn't two minutes later
                Pending p = _pendingNote;
                if (p != null && p.S.NoteAtCue)
                {
                    _pendingNote = null;
                    if (!n.Test)
                        NoteHappened(p, "argue");
                }
                int at = _now;
                foreach (Ped x in new[] { n.A, n.B })
                    if (x != null)
                    {
                        _sceneAtHandle[x.Handle] = at;
                        _restMsOf[x.Handle] = FoughtRestMs;
                    }
            }
            if (n.Test || !n.Rough)
                return;   // a test notes nothing; breaking up a brawl takes nobody's side
            string cId, cName, aId, aName, bId, bName;
            bool c = LastingId(n.C, out cId, out cName), a = LastingId(n.A, out aId, out aName), b = LastingId(n.B, out bId, out bName);
            long g = Bridge.Now();
            if (c && b)
            {
                SceneTie t = Ties.Note(_store.Ties, cId, cName, bId, bName, _placeTown, "defend", g);
                if (t != null)
                    Log.Write("TIE " + t.AName + " & " + t.BName + ": defend (" + (t.Warmth > 0 ? "warm" : t.Warmth < 0 ? "hostile" : "acquainted") + ", " + t.Count + " scenes between them)");
            }
            if (c && a)
            {
                SceneTie t = Ties.Note(_store.Ties, cId, cName, aId, aName, _placeTown, "faced", g);
                if (t != null)
                    Log.Write("TIE " + t.AName + " & " + t.BName + ": faced (" + (t.Warmth > 0 ? "warm" : t.Warmth < 0 ? "hostile" : "acquainted") + ", " + t.Count + " scenes between them)");
            }
            Safe("minds", () =>
            {
                string place = _engine.NowIndoors ? _engine.NowPlace : null;
                Minds.Party pc = PartyOf(n.C, false), pa = PartyOf(n.A, false), pb = PartyOf(n.B, false);
                if (pc != null && pb != null)
                    _engine.RememberScene("defend", pc, pb, null, _placeTown, place);
                if (pc != null && pa != null)
                    _engine.RememberScene("faced", pc, pa, null, _placeTown, place);
            });
        }

        /// F7: how often someone stepped in this session, how often the bully backed off, the last, and why not lately.
        private string StandUpStatus()
        {
            if (!_cfg.Kindnesses)
                return "off ([Scenes] Kindnesses)";
            string who = !_cfg.MindLive || !_cfg.MindStandUp ? "who steps in by the roll ([Mind] " + (!_cfg.MindLive ? "MindLive" : "MindStandUp") + " off)"
                       : _standUpMindFailed ? "who steps in by the roll (the mind's part FAILED this session)"
                       : "who steps in from their minds: " + _standUpByMind + " chosen so, " + _standUpNobody + " times nobody's mind moved them, the bully's own mind " + _standUpBullyMind + " times";
            return _standUps + " stepped in this session, " + _standUpBacked + " times the bully backed off, " + _standUpTookAway + " times they took the one picked on away; " + who + (_standUpLast != null ? "; last: " + _standUpLast : "") +
                   (_standUpWhyNot != null ? "; the last time nobody did: " + _standUpWhyNot : "");
        }
    }
}
