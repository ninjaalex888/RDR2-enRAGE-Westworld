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
            public bool Backs, Test, Rough;
        }

        private StandUpNote _standUp;
        private int _standUps, _standUpBacked;
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
            Candidate by = null;
            foreach (Candidate c in ReasonCandidates(me, now, test).Where(x => x.Ped.Handle != people[0].Handle && x.Ped.Handle != people[1].Handle && x.Who.Id != a.Id && x.Who.Id != b.Id &&
                                                                              x.Ped.Position.DistanceTo(mid) < 10f && Person.FirstOf(x.Who.Name) != an && Person.FirstOf(x.Who.Name) != bn)
                                                             .OrderByDescending(x => TieWarmth(x.Who.Id, b) > 0 ? 1 : 0).ThenBy(x => x.Ped.Position.DistanceTo(mid)))
            {
                int toB = TieWarmth(c.Who.Id, b), toA = TieWarmth(c.Who.Id, a);
                bool would = test ? c.Who.Temper != "Coward" && toB >= 0 && toA <= 0 : Scenes.StandsUp(toB, toA, c.Who.Temper, c.Who.Woman, seed + "|" + c.Who.Id);
                if (!would || PED.IS_PED_USING_ANY_SCENARIO(c.Ped.Handle) || !ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(c.Ped.Handle, people[1].Handle, 17))
                    continue;   // not someone sat at a table or leaning on the bar: they'd have to get up first
                by = c;
                break;
            }
            if (by == null)
            {
                why = _standUpWhyNot = "nobody watching who'd step in";
                return false;
            }
            string skipped;
            string pace = Pacing.WhyNot(_store.Pace, "standup", _placeTown, by.Who.Id, g, Voice.Hash(seed + "|standup") % 100, test, out skipped);
            if (pace != null)
            {
                why = _standUpWhyNot = "rests: " + pace;
                return false;
            }
            bool backs = Scenes.BacksOff(a.Temper, by.Who.Temper, by.Who.Woman, seed);
            bool rough = s.Kind == "rough";
            bool far = by.Ped.Position.DistanceTo(people[0].Position) > 3.5f;
            cast.Names[2] = Person.FirstOf(by.Who.Name);
            cast.Tempers[2] = by.Who.Temper;
            cast.CWoman = by.Who.Woman;
            if (!Scenes.StandUp(s, cast, seed, backs, far))
            {
                cast.Names[2] = null;
                cast.Tempers[2] = null;
                why = _standUpWhyNot = "no blow in this scene to stand between";
                return false;
            }
            people.Add(by.Ped);
            Pacing.Note(_store.Pace, "standup", _placeTown, by.Who.Id, g);
            _standUp = new StandUpNote { C = by.Ped, A = people[0], B = people[1], CName = by.Who.Name, AName = a.Name, BName = b.Name, Backs = backs, Test = test, Rough = rough };
            _standUpLast = by.Who.Name + " for " + b.Name + " against " + a.Name + ": " + (backs ? "he backed off" : "he went on") + (test ? " (a test)" : "");
            Log.Write("KIND stood up: " + by.Who.Name + " steps in for " + b.Name + " against " + a.Name + " -> " + (backs ? "backs off" : "tells them to keep out") + " (" +
                      (TieWarmth(by.Who.Id, b) > 0 ? "a friend of " + Person.FirstOf(b.Name) : "a " + (by.Who.Temper ?? "plain") + " " + (by.Who.Woman ? "woman" : "man")) + ", " + s.Kind + ")" +
                      (test ? ", a test" + (skipped != null ? " (skipped: " + skipped + ")" : "") : ""));
            return true;
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
            if (n.Backs)
            {
                _standUpBacked++;
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
            return _standUps + " stepped in this session, " + _standUpBacked + " times the bully backed off" + (_standUpLast != null ? "; last: " + _standUpLast : "") +
                   (_standUpWhyNot != null ? "; the last time nobody did: " + _standUpWhyNot : "");
        }
    }
}
