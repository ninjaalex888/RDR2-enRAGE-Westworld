// A street preacher on a Sunday afternoon, and two old men arguing about the old days (overnight 2026-10-27, Tier 4;
// the scenes are Core/Scenes.Preacher.cs and Core/Scenes.OldDays.cs). The preacher: on a Sunday (InnerLife.Weekday 0)
// from 13:00 to 17:00 out of doors in a town, a named devout person (a churchgoer now and then) preaches through the
// Director, and someone named within 8 m answers by their own faith. Pacing "preacher" (35% a look every 60 s, once a
// town a day). The old men come from the chance meetings (WestworldScript.Life.cs): two old men, 45%.
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
        private const int PreacherLookMs = 60000;
        private int _nextPreacher, _preachers, _oldDays;
        private string _preacherLast, _preacherWhyNot;

        private bool PreacherScene(Ped me, int now, bool test, out string why)
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
            int clock = CLOCK.GET_CLOCK_HOURS();
            if (!test && (now < _nextPreacher || InnerLife.Weekday != 0 || clock < 13 || clock >= 17))
                return false;
            _nextPreacher = now + PreacherLookMs;
            long g = _store.LastGameMin;
            string skipped0;
            string pre = Pacing.WhyNot(_store.Pace, "preacher", _placeTown, null, g, _rng.Next(100), test, out skipped0);
            if (pre != null)
            {
                why = _preacherWhyNot = "rests: " + pre;
                return false;
            }
            List<Candidate> people = ReasonCandidates(me, now, test);
            Candidate preacher = people.Where(c => c.Who.P != null && (test || Scenes.WouldPreach(c.Who.P.Devotion, c.Who.Id + "|" + g / 1440)) && NodInView(me, c.Ped) &&
                                                   !PED.IS_PED_USING_ANY_SCENARIO(c.Ped.Handle))
                                       .OrderBy(c => c.Ped.Position.DistanceTo(me.Position)).FirstOrDefault();
            if (preacher == null)
            {
                why = _preacherWhyNot = "nobody devout out in the street";
                return false;
            }
            Candidate b = people.Where(c => c != preacher && c.Who.Id != preacher.Who.Id && c.Ped.Position.DistanceTo(preacher.Ped.Position) < 8f)
                                .OrderBy(c => c.Ped.Position.DistanceTo(preacher.Ped.Position)).FirstOrDefault();
            if (b == null)
            {
                why = _preacherWhyNot = "nobody near " + preacher.Who.Name + " to hear it";
                return false;
            }
            string seed = preacher.Who.Id + "|preach|" + g / 1440;
            SceneCast cast = BuildCast(preacher.Ped, b.Ped, null, false, me, now, seed);
            string faith = b.Who.P != null && b.Who.P.Devotion != null ? b.Who.P.Devotion : "Churchgoer";
            Scene s = Scenes.Preacher(cast, seed, faith);
            StartScene(s, new List<Ped> { preacher.Ped, b.Ped }, cast, seed, now, test);
            if (!_director.Busy)
            {
                why = _preacherWhyNot = "the Director wouldn't take the scene";
                return false;
            }
            Pacing.Note(_store.Pace, "preacher", _placeTown, null, g);
            _preachers++;
            _preacherLast = preacher.Who.Name + " preached; " + b.Who.Name + " (" + faith + ") answered" + (test ? " (a test)" : "");
            Log.Write("PREACHER " + preacher.Who.Name + " preaches in " + _placeTown + "; " + b.Who.Name + " answers (" + faith + ")" + (test ? ", a test" + (skipped0 != null ? " (skipped: " + skipped0 + ")" : "") : ""));
            return true;
        }

        /// The Testing page: the nearest two named old men within 10 m argue about the old days now.
        private string OldDaysFire(int now)
        {
            Ped me = Game.Player.Character;
            List<Candidate> old = ReasonCandidates(me, now, true).Where(c => !c.Who.Woman && Director.OldModel(c.Ped)).OrderBy(c => c.Ped.Position.DistanceTo(me.Position)).ToList();
            for (int i = 0; i < old.Count; i++)
                for (int j = i + 1; j < old.Count; j++)
                {
                    if (old[i].Ped.Position.DistanceTo(old[j].Ped.Position) > 10f)
                        continue;
                    string seed = old[i].Who.Id + "|" + old[j].Who.Id + "|" + now;
                    SceneCast cast = BuildCast(old[i].Ped, old[j].Ped, null, _placeKind == "saloon", me, now, seed);
                    Scene s = Scenes.OldDays(cast, seed);
                    StartScene(s, new List<Ped> { old[i].Ped, old[j].Ped }, cast, seed, now, true);
                    if (!_director.Busy)
                        return "the Director wouldn't take the scene";
                    _oldDays++;
                    Log.Write("OLD DAYS " + old[i].Who.Name + " & " + old[j].Who.Name + ": " + s.Beats[0].Line + " (a test)");
                    return null;
                }
            return old.Count < 2 ? "fewer than two named old men near you" : "the old men near you are too far apart";
        }

        private string PreacherStatus()
        {
            return "preachers " + _preachers + ", old days " + _oldDays + " this session" + (_preacherLast != null ? "; last preacher: " + _preacherLast : "") +
                   (_preacherWhyNot != null ? "; the last look: " + _preacherWhyNot : "") + "; today is " + (InnerLife.Weekday == 0 ? "Sunday" : "not Sunday");
        }
    }
}
