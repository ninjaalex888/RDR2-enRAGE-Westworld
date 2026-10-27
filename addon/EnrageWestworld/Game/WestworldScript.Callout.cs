// The noon callout ([Scenes] Standoffs; the scene is Core/Scenes.Callout.cs): between half past eleven and half past
// twelve, out of doors in a town, two named men with bad blood (Scenes.CalloutWorthy) 5-14 m apart, one of them in your
// view: the hotter head calls the other out, the Director plays it as a standoff (guns only when both are armed, no
// lawman is near and it's not a saloon, BuildCast's rule), someone watching may call out, and the cooler head backs
// down. Nobody fires. Pacing "callout" (40% a look every 30 s; once a day anywhere, once a day a town, a pair every two
// days).
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
        private const int CalloutLookMs = 30000;
        private int _nextCallout, _callouts;
        private string _calloutLast, _calloutWhyNot;

        private bool CalloutScene(Ped me, int now, bool test, out string why)
        {
            why = null;
            if (!_cfg.Standoffs)
            {
                why = "[Scenes] Standoffs is off";
                return false;
            }
            if (_placeTown == null || _interiorType != 0)
            {
                why = "not out in a town's street";
                return false;
            }
            int clock = CLOCK.GET_CLOCK_HOURS() * 60 + CLOCK.GET_CLOCK_MINUTES();
            if (!test && (now < _nextCallout || clock < 11 * 60 + 30 || clock > 12 * 60 + 30))
                return false;
            _nextCallout = now + CalloutLookMs;
            long g = _store.LastGameMin;
            string skipped0;
            string pre = Pacing.WhyNot(_store.Pace, "callout", _placeTown, null, g, _rng.Next(100), test, out skipped0);
            if (pre != null)
            {
                why = _calloutWhyNot = "rests: " + pre;
                return false;
            }
            List<Candidate> pool = ReasonCandidates(me, now, test).Where(c => !c.Who.Woman).ToList();
            Candidate caller = null, called = null;
            float best = float.MaxValue;
            for (int i = 0; i < pool.Count; i++)
                for (int j = 0; j < pool.Count; j++)
                {
                    if (i == j || pool[i].Who.Id == pool[j].Who.Id)
                        continue;
                    Candidate a = pool[i], b = pool[j];
                    float apart = a.Ped.Position.DistanceTo(b.Ped.Position);
                    if (apart < (test ? 2f : 5f) || apart > 14f || (!NodInView(me, a.Ped) && !NodInView(me, b.Ped)))
                        continue;
                    SceneTie t = _store.Ties.FirstOrDefault(x => x.Has(a.Who.Id) && x.Has(b.Who.Id));
                    if (t == null || !Scenes.CalloutWorthy(t.Warmth, t.Stage, t.Kind, g - t.At, a.Who.Temper))
                        continue;
                    if (Scenes.Heat(a.Who.Temper) < Scenes.Heat(b.Who.Temper))
                        continue;   // the hotter head does the calling (the pair is looked at both ways round)
                    float fromYou = Math.Min(a.Ped.Position.DistanceTo(me.Position), b.Ped.Position.DistanceTo(me.Position));
                    if (fromYou >= best)
                        continue;
                    caller = a;
                    called = b;
                    best = fromYou;
                }
            if (caller == null)
            {
                why = _calloutWhyNot = "no two pool here with bad blood enough, the right distance apart";
                return false;
            }
            string pair = NodPair(caller.Who.Id, called.Who.Id), skipped;
            string pace = Pacing.WhyNot(_store.Pace, "callout", _placeTown, pair, g, 0, test, out skipped);
            if (pace != null)
            {
                why = _calloutWhyNot = "rests: " + pace;
                return false;
            }
            string seed = pair + "|callout|" + g / 1440;
            Vector3 mid = (caller.Ped.Position + called.Ped.Position) * 0.5f;
            Ped crowd = World.GetAllPeds().Where(p => p != null && p.Exists() && !p.IsDead && p.Handle != me.Handle && p.Handle != caller.Ped.Handle && p.Handle != called.Ped.Handle &&
                                                     p.Position.DistanceTo(mid) < 14f && Natives.Bystander(p, me.Handle, IsRecord(p.Handle)) && !Spawner.IsOurs(p.Handle) &&
                                                     MayDirectWhyNot(p) == null && !NotFree(p.Handle))
                                          .OrderBy(p => p.Position.DistanceTo(mid)).FirstOrDefault();
            SceneCast cast = BuildCast(caller.Ped, called.Ped, crowd, false, me, now, seed);
            if (cast.Off.Contains("standoff"))
            {
                why = _calloutWhyNot = "standoffs are off";
                return false;
            }
            bool bBacks = Scenes.CalloutBBacks(caller.Who.Temper, called.Who.Temper, seed);
            Scene s = Scenes.Callout(cast, seed, bBacks);
            var people = new List<Ped> { caller.Ped, called.Ped };
            if (s.Cast > 2 && crowd != null)
                people.Add(crowd);
            StartScene(s, people, cast, seed, now, test);
            if (!_director.Busy)
            {
                why = _calloutWhyNot = "the Director wouldn't take the scene";
                return false;
            }
            Pacing.Note(_store.Pace, "callout", _placeTown, pair, g);
            _callouts++;
            string backed = bBacks ? called.Who.Name : caller.Who.Name;
            _calloutLast = caller.Who.Name + " called out " + called.Who.Name + "; " + backed + " backed down" + (test ? " (a test)" : "");
            Log.Write("CALLOUT " + caller.Who.Name + " & " + called.Who.Name + " at noon in " + _placeTown + ": " + backed + " backed down (" + (cast.Armed0 ? "guns drawn" : "no guns") +
                      (crowd != null ? ", someone calls out" : "") + ", " + (int)caller.Ped.Position.DistanceTo(called.Ped.Position) + " m apart)" +
                      (test ? ", a test" + ((skipped ?? skipped0) != null ? " (skipped: " + (skipped ?? skipped0) + ")" : "") : ""));
            return true;
        }

        private string CalloutStatus()
        {
            if (!_cfg.Standoffs)
                return "off ([Scenes] Standoffs)";
            return _callouts + " this session" + (_calloutLast != null ? "; last: " + _calloutLast : "") + (_calloutWhyNot != null ? "; the last look: " + _calloutWhyNot : "");
        }
    }
}
