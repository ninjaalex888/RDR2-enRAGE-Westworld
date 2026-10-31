// The bell at three (overnight 2026-10-27, Tier 4; the lines are Core/Bell.cs). With West World on, from 03:00 to 03:15
// out of doors in a town with a church we know (the Churches table), once a night: up to three of the awake (wake stage
// 2 and on) within 30 m of you and in view look toward the church for 6 s (a look-at, nothing more: nobody is held or
// tasked), each with a thought by their stage; the nearest not awake within 6 m of one of them wonders what they're
// staring at. No sound: the player hears nothing. Pacing "bell" (a night between, a town a night).
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
        private const int BellLookMs = 20000;
        private int _nextBell, _bells, _bellHeard;
        private string _bellLast, _bellWhyNot;

        private bool BellScene(Ped me, int now, bool test, out string why)
        {
            why = null;
            if (!_cfg.WestWorld)
            {
                why = "West World is off";
                return false;
            }
            Vector3 church;
            if (_placeTown == null || _interiorType != 0 || !Churches.TryGetValue(_placeTown, out church))
            {
                why = "not out of doors in a town with a church";
                return false;
            }
            if (!test && (now < _nextBell || !Bell.Hour(CLOCK.GET_CLOCK_HOURS(), CLOCK.GET_CLOCK_MINUTES())))
                return false;
            _nextBell = now + BellLookMs;
            long g = _store.LastGameMin;
            string skipped0;
            string pre = Pacing.WhyNot(_store.Pace, "bell", _placeTown, null, g, _rng.Next(100), test, out skipped0);
            if (pre != null)
            {
                why = _bellWhyNot = "rests: " + pre;
                return false;
            }
            List<Candidate> people = ReasonCandidates(me, now, test).Where(c => c.Ped.Position.DistanceTo(me.Position) < 30f && NodInView(me, c.Ped)).ToList();
            List<Candidate> hear = people.Where(c => Bell.Hears(c.Who.WakeStage)).OrderBy(c => c.Ped.Position.DistanceTo(me.Position)).Take(3).ToList();
            if (hear.Count == 0)
            {
                why = _bellWhyNot = "nobody awake near you";
                return false;
            }
            foreach (Candidate c in hear)
            {
                LookAtCoord(c.Ped.Handle, church.X, church.Y, church.Z + 8f, 6000);
                _ticker.Push(Person.FirstOf(c.Who.Name) + ", thinking", Bell.Thought(c.Who.WakeStage, c.Who.Id + "|" + g / 1440), "near", c.Ped.Handle);
            }
            Candidate first = hear[0];
            Candidate deaf = people.Where(c => !Bell.Hears(c.Who.WakeStage) && hear.Any(x => x.Ped.Position.DistanceTo(c.Ped.Position) < 6f))
                                   .OrderBy(c => c.Ped.Position.DistanceTo(first.Ped.Position)).FirstOrDefault();
            if (deaf != null)
            {
                Candidate near = hear.OrderBy(x => x.Ped.Position.DistanceTo(deaf.Ped.Position)).First();
                TASK.TASK_LOOK_AT_ENTITY(deaf.Ped.Handle, near.Ped.Handle, 4000, 0, 51, 0);
                _ticker.Push(Person.FirstOf(deaf.Who.Name) + ", thinking", Bell.PuzzledBy(Person.FirstOf(near.Who.Name), deaf.Who.Id + "|" + g / 1440), "near", deaf.Ped.Handle);
            }
            Pacing.Note(_store.Pace, "bell", _placeTown, null, g);
            _bells++;
            _bellHeard += hear.Count;
            _bellLast = string.Join(", ", hear.Select(c => c.Who.Name)) + " heard it in " + _placeTown + (deaf != null ? "; " + deaf.Who.Name + " didn't" : "") + (test ? " (a test)" : "");
            Log.Write("BELL at three in " + _placeTown + ": " + string.Join(", ", hear.Select(c => c.Who.Name + " (stage " + c.Who.WakeStage + ")")) + " look toward the church" +
                      (deaf != null ? "; " + deaf.Who.Name + " hears nothing" : "") + (test ? ", a test" + (skipped0 != null ? " (skipped: " + skipped0 + ")" : "") : ""));
            return true;
        }

        private string BellStatus()
        {
            if (!_cfg.WestWorld)
                return "off (West World)";
            return _bells + " this session, " + _bellHeard + " heard it" + (_bellLast != null ? "; last: " + _bellLast : "") + (_bellWhyNot != null ? "; the last look: " + _bellWhyNot : "") +
                   "; " + (Bell.Hour(CLOCK.GET_CLOCK_HOURS(), CLOCK.GET_CLOCK_MINUTES()) ? "it's three" : "not three");
        }
    }
}
