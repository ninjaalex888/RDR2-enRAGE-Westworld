// A mother asking around for her lost child (overnight 2026-10-27, Tier 4; the lines are Core/Scenes.LostChild.cs). By
// day (08:00-18:00) out of doors in a town, now and then a named woman in the street has lost sight of her child: her
// thought over her head, then she asks you, if you're within 7 m (she turns to you; subtitled), else the nearest named
// person within 8 m through the Director, who answers by their own nature. Nobody is spawned: an hour to three later
// the town has word the child turned up safe (a corner notice). Pacing "lostchild" (15% a look every 60 s, three game
// days in a town, a week for the same woman).
using System;
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private const int LostLookMs = 60000;
        private const float LostAskYou = 7f, LostAskOther = 8f;
        private int _nextLost, _lostAsked, _lostAskedYou;
        private string _lostLast, _lostWhyNot;

        private bool LostChildScene(Ped me, int now, bool test, out string why)
        {
            why = null;
            if (!_cfg.Drama)
            {
                why = "[World] Drama is off";
                return false;
            }
            if (_placeTown == null || _interiorType != 0)
            {
                why = "not out in a town's street";
                return false;
            }
            if (!test && (now < _nextLost || !LostChild.Hour(CLOCK.GET_CLOCK_HOURS())))
                return false;
            _nextLost = now + LostLookMs;
            long g = _store.LastGameMin;
            string skipped0;
            string pre = Pacing.WhyNot(_store.Pace, "lostchild", _placeTown, null, g, _rng.Next(100), test, out skipped0);
            if (pre != null)
            {
                why = _lostWhyNot = "rests: " + pre;
                return false;
            }
            List<Candidate> people = ReasonCandidates(me, now, test);
            string ignored;
            Candidate mother = people.Where(c => c.Who.Woman && c.Who.P != null && LostChild.CouldBeMother(c.Who.P) && !Director.OldModel(c.Ped) && c.Ped.Position.DistanceTo(me.Position) < 25f && NodInView(me, c.Ped) &&
                                                 (!PED.IS_PED_USING_ANY_SCENARIO(c.Ped.Handle) || test && !PED.IS_PED_SITTING(c.Ped.Handle)) &&   // a test may ask a woman standing about (Lex, 2026-10-27)
                                                 (test || Pacing.WhyNot(_store.Pace, "lostchild", null, c.Who.Id, g, 0, false, out ignored) == null))
                                     .OrderBy(c => c.Ped.Position.DistanceTo(me.Position)).FirstOrDefault();
            if (mother == null)
            {
                // what stood in the way, counted, as the fall's test does (Lex, 2026-10-27: she couldn't find a woman)
                var women = people.Where(c => c.Who.Woman).ToList();
                string counted = string.Join(", ", women.Select(c => c.Who.P == null || !LostChild.CouldBeMother(c.Who.P) || Director.OldModel(c.Ped) ? "not of a mother's age or trade"
                                                                   : c.Ped.Position.DistanceTo(me.Position) >= 25f ? "further than 25 m"
                                                                   : !NodInView(me, c.Ped) ? "out of view"
                                                                   : PED.IS_PED_SITTING(c.Ped.Handle) ? "sitting"
                                                                   : PED.IS_PED_USING_ANY_SCENARIO(c.Ped.Handle) ? "standing in one spot"
                                                                   : "asked lately")
                                                        .GroupBy(r => r).OrderByDescending(gr => gr.Count()).Select(gr => gr.Count() + " " + gr.Key));
                why = _lostWhyNot = "no woman out in the street near you" + (women.Count > 0 ? " (" + women.Count + " named women near: " + counted + ")" : " (no named women free near you)") +
                                    (test ? "; everyone within 25 m: " + CrowdCounted(me, 25f) : "");
                return false;
            }
            string seed = mother.Who.Id + "|lost|" + g / 1440;
            LostChild.Child child = LostChild.Of(mother.Who.Id);
            string first = Person.FirstOf(mother.Who.Name);
            int h = mother.Ped.Handle;
            bool you = mother.Ped.Position.DistanceTo(me.Position) < LostAskYou && !PED.IS_PED_ON_MOUNT(me.Handle) && !PED.IS_PED_IN_ANY_VEHICLE(me.Handle, false);
            string asked;
            if (test && PED.IS_PED_USING_ANY_SCENARIO(h))
                TASK.CLEAR_PED_TASKS(h, false, false);   // a test's woman standing about leaves what she was doing to ask
            if (you)
            {
                // she turns to you and asks; she isn't held, so she goes back to her own way once she's said it
                TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, me.Handle, 3500, 0f, 0f, 0f);
                TASK.TASK_LOOK_AT_ENTITY(h, me.Handle, 5000, 0, 51, 0);
                string line = LostChild.ToYou(child, seed);
                Feed.Say(first, line, Speech.When(h, "scared", me.Handle), h, toYou: true);
                NoteInScene(new List<Ped> { mother.Ped }, "lostchild", now);   // busy: no street scene takes her straight after
                WanderLater(h, now + 7000);
                _lostAskedYou++;
                asked = "you";
            }
            else
            {
                Candidate b = people.Where(c => c != mother && c.Who.Id != mother.Who.Id && c.Ped.Position.DistanceTo(mother.Ped.Position) < LostAskOther &&
                                                !Ties.Sweethearts(_store.Ties.FirstOrDefault(t => t.Has(c.Who.Id) && t.Has(mother.Who.Id))))   // not her sweetheart: a cold answer would end it
                                    .OrderBy(c => c.Ped.Position.DistanceTo(mother.Ped.Position)).FirstOrDefault();
                if (b == null)
                {
                    why = _lostWhyNot = "nobody near " + mother.Who.Name + " to ask";
                    return false;
                }
                SceneCast cast = BuildCast(mother.Ped, b.Ped, null, false, me, now, seed);
                string manner = Exchanges.Manner(b.Who, b.Who.Hurt != null || b.Who.Mood == "sore");
                Scene s = Scenes.Lost(cast, seed, child, manner);
                float apart = mother.Ped.Position.DistanceTo(b.Ped.Position);
                if (apart > 2.6f)
                    s.Beats.Insert(0, new Beat(0, null, null, "meet"));   // she goes to them
                StartScene(s, new List<Ped> { mother.Ped, b.Ped }, cast, seed, now, test);
                if (!_director.Busy)
                {
                    why = _lostWhyNot = "the Director wouldn't take the scene";
                    return false;
                }
                asked = b.Who.Name + " (" + manner + ")";
            }
            _ticker.Push(first + ", thinking", LostChild.Thought(child, seed), "near", h);
            Pacing.Note(_store.Pace, "lostchild", _placeTown, mother.Who.Id, g);
            if (!test)
                _engine.LostChildAsked(_placeTown, mother.Who.Name, LostChild.Found(child, mother.Who.Name, seed), g);
            _lostAsked++;
            _lostLast = mother.Who.Name + " asked " + asked + " after " + child.Name + (test ? " (a test)" : "");
            Log.Write("LOST CHILD " + mother.Who.Name + " asks " + asked + " after her " + child.Kind + " " + child.Name + " in " + _placeTown +
                      (test ? ", a test" + (skipped0 != null ? " (skipped: " + skipped0 + ")" : "") : "; the town will have word of " + child.Him + " later"));
            return true;
        }

        private string LostChildStatus()
        {
            return "asked " + _lostAsked + " this session (you " + _lostAskedYou + ")" + (_lostLast != null ? "; last: " + _lostLast : "") +
                   (_lostWhyNot != null ? "; the last look: " + _lostWhyNot : "") + "; by day " + (LostChild.Hour(CLOCK.GET_CLOCK_HOURS()) ? "now" : "not now");
        }
    }
}
