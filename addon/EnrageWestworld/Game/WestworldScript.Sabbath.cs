// The Sabbath (overnight 2026-10-27, Tier 4; the lines are Core/Sabbath.cs). On a Sunday morning (08:00 to noon) in a
// town with a church we know (the Churches table), within 60 m of it: now and then one of the devout (a churchgoer one
// Sunday in three) within 25 m of you and in view kneels a while in place (the game's graveside kneel scenario, 25 s, which ends
// by itself; nobody is held) with a prayer over their head; someone lapsed or godless within 15 m of them who can see it
// feels watched, 4 s later. Pacing "sabbath" (30% a look every 45 s, two game hours between, a week for the same person).
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
        private const int SabbathLookMs = 45000, SabbathKneelMs = 25000;
        private int _nextSabbath, _kneels, _watched, _sabbathWatchAt;
        private Ped _sabbathWatcher;
        private string _sabbathWatchLine, _sabbathWatchName, _sabbathLast, _sabbathWhyNot;

        private bool SabbathScene(Ped me, int now, bool test, out string why)
        {
            why = null;
            if (!_cfg.Drama)
            {
                why = "[World] Drama is off";
                return false;
            }
            Vector3 church;
            if (_placeTown == null || _interiorType != 0 || !Churches.TryGetValue(_placeTown, out church))
            {
                why = "not out of doors in a town with a church";
                return false;
            }
            if (!test && (now < _nextSabbath || !Sabbath.Hour(InnerLife.Weekday, CLOCK.GET_CLOCK_HOURS())))
                return false;
            _nextSabbath = now + SabbathLookMs;
            if (church.DistanceTo2D(me.Position) > 60f)
            {
                why = _sabbathWhyNot = "not by the church (" + (int)church.DistanceTo2D(me.Position) + " m)";
                return false;
            }
            string plan = PeacefulPlanDue(church, now);
            if (plan != null)
            {
                why = _sabbathWhyNot = "enRAGE has " + plan + " by the church";   // never kneel through one of its funerals
                return false;
            }
            long g = _store.LastGameMin;
            string skipped0, ignored;
            string pre = Pacing.WhyNot(_store.Pace, "sabbath", _placeTown, null, g, _rng.Next(100), test, out skipped0);
            if (pre != null)
            {
                why = _sabbathWhyNot = "rests: " + pre;
                return false;
            }
            List<Candidate> people = ReasonCandidates(me, now, test);
            string day = "|" + g / 1440;
            Candidate kneeler = people.Where(c => c.Who.P != null && Sabbath.Kneels(c.Who.P.Devotion ?? (test ? "Devout" : null), c.Who.Id + day) &&
                                                  c.Ped.Position.DistanceTo(me.Position) < 25f && c.Ped.Position.DistanceTo2D(church) < 60f && NodInView(me, c.Ped) &&
                                                  !PED.IS_PED_USING_ANY_SCENARIO(c.Ped.Handle) && MayDirectWhyNot(c.Ped) == null &&
                                                  (test || Pacing.WhyNot(_store.Pace, "sabbath", null, c.Who.Id, g, 0, false, out ignored) == null))
                                      .OrderBy(c => c.Ped.Position.DistanceTo(me.Position)).FirstOrDefault();
            if (kneeler == null)
            {
                why = _sabbathWhyNot = "nobody devout by the church near you";
                return false;
            }
            int h = kneeler.Ped.Handle;
            TASK.CLEAR_PED_TASKS(h, true, false);
            TASK.TASK_START_SCENARIO_IN_PLACE_HASH(h, Natives.Hash(Kneel), SabbathKneelMs, true, 0u, -1f, false);
            string seed = kneeler.Who.Id + day;
            string first = Person.FirstOf(kneeler.Who.Name);
            _ticker.Push(first + ", thinking", Sabbath.Prayer(seed), "near", h);
            NoteInScene(new List<Ped> { kneeler.Ped }, "sabbath", now);   // busy: no street scene takes them off their knees
            WanderLater(h, now + SabbathKneelMs + 1500);
            Pacing.Note(_store.Pace, "sabbath", _placeTown, kneeler.Who.Id, g);
            _kneels++;
            // someone lapsed or godless who sees it feels it, a moment later
            Candidate w = people.Where(c => c != kneeler && c.Who.P != null && Sabbath.Watched(c.Who.P.Devotion) && c.Ped.Position.DistanceTo(kneeler.Ped.Position) < 15f &&
                                            ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(c.Ped.Handle, h, 17))
                                .OrderBy(c => c.Ped.Position.DistanceTo(kneeler.Ped.Position)).FirstOrDefault();
            if (w != null)
            {
                _sabbathWatcher = w.Ped;
                _sabbathWatchName = Person.FirstOf(w.Who.Name);
                _sabbathWatchLine = Sabbath.WatchedThought(w.Who.P.Devotion, w.Who.Id + day);
                _sabbathWatchAt = now + 4000;
            }
            _sabbathLast = kneeler.Who.Name + " knelt by the church" + (w != null ? "; " + w.Who.Name + " (" + w.Who.P.Devotion + ") saw it" : "") + (test ? " (a test)" : "");
            Log.Write("SABBATH " + kneeler.Who.Name + " kneels by the church in " + _placeTown + (w != null ? "; " + w.Who.Name + " (" + w.Who.P.Devotion + ") feels watched" : "") +
                      (test ? ", a test" + (skipped0 != null ? " (skipped: " + skipped0 + ")" : "") : ""));
            return true;
        }

        /// The one who saw it: their thought, once, if they're still there and in your view.
        private void SabbathWatchStep(int now)
        {
            if (_sabbathWatcher == null || now < _sabbathWatchAt)
                return;
            Ped p = _sabbathWatcher;
            _sabbathWatcher = null;
            if (now - _sabbathWatchAt < 15000 && p.Exists() && !p.IsDead && NodInView(Game.Player.Character, p))   // not long after
            {
                _ticker.Push(_sabbathWatchName + ", thinking", _sabbathWatchLine, "near", p.Handle);
                _watched++;
            }
        }

        // ---- let go after a short act of ours (the Sabbath's kneel, the lost child's mother) --------------------------

        private readonly List<KeyValuePair<int, int>> _wanderLater = new List<KeyValuePair<int, int>>();   // handle, when

        private void WanderLater(int h, int at)
        {
            _wanderLater.Add(new KeyValuePair<int, int>(h, at));
        }

        /// From DramaTick: anyone due who's still standing about, not taken by anything else, goes on their way.
        private void WanderLaterStep(int now)
        {
            for (int i = _wanderLater.Count - 1; i >= 0; i--)
            {
                if (now < _wanderLater[i].Value)
                    continue;
                int h = _wanderLater[i].Key;
                _wanderLater.RemoveAt(i);
                if (ENTITY.DOES_ENTITY_EXIST(h) && !ENTITY.IS_ENTITY_DEAD(h) && !NotFree(h) && !PED.IS_PED_USING_ANY_SCENARIO(h))
                    TASK.TASK_WANDER_STANDARD(h, 10f, 10);
            }
        }

        private void WanderLaterClear()
        {
            _wanderLater.Clear();
        }

        private string SabbathStatus()
        {
            return "kneeling " + _kneels + ", watched " + _watched + " this session" + (_sabbathLast != null ? "; last: " + _sabbathLast : "") +
                   (_sabbathWhyNot != null ? "; the last look: " + _sabbathWhyNot : "") + "; " + (Sabbath.Hour(InnerLife.Weekday, CLOCK.GET_CLOCK_HOURS()) ? "a Sunday morning" : "not a Sunday morning");
        }
    }
}
