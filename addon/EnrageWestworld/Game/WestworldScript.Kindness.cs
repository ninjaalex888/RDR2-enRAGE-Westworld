// A fall and a helping hand ([Scenes] Kindnesses; the scene is Core/Scenes.Fall.cs): now and then someone walking
// near you, out of doors and on level ground, trips. The nearest named person who can see it helps them up or walks
// past, by what's between them and who they are; the Director plays it, and its tie and memories are noted once they
// get up (Director.Happened). Before anyone reaches them, you can give them a hand up yourself: walk up within 2.5 m
// while they're down and they thank you (said to you, subtitled), think better of you and remember it. Rare (Pacing
// "fall": a look every 20 s at 6%, 150 game minutes between falls, 600 in one town, a day for the same person).
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
        private const int FallLookMs = 20000, FallWatchMs = 15000;
        private const float FallFromYou = 14f, FallSeen = 9f, HandUpMetres = 2.5f;

        private sealed class FallWatch
        {
            public Ped Fallen, By;
            public string Name, Answer, Seed;
            public Someone Who;
            public bool Woman;
            public int Until;
        }

        private FallWatch _fallWatch;
        private int _nextFall, _handUps;
        private readonly Dictionary<string, int> _fallCount = new Dictionary<string, int>();
        private string _fallLast, _fallWhyNot;

        /// Someone near you takes a fall, and the one who sees it helps or doesn't. True if a scene started.
        private bool FallScene(Ped me, int now, bool test, out string why)
        {
            why = null;
            if (!_cfg.Kindnesses)
            {
                why = "[Scenes] Kindnesses is off";
                return false;
            }
            if (!test && now < _nextFall)
                return false;
            _nextFall = now + FallLookMs;
            List<Candidate> people = ReasonCandidates(me, now, test);
            Candidate faller = people.Where(c => FallCant(c.Ped, me, test) == null).OrderBy(c => c.Ped.Position.DistanceTo(me.Position)).FirstOrDefault();
            if (faller == null)
            {
                why = _fallWhyNot = "nobody walking on level ground in view within " + FallFromYou + " m" + (people.Count > 0 ? " (" + people.Count + " named people near)" : "");
                return false;
            }
            Candidate by = people.Where(c => c != faller && c.Who.Id != faller.Who.Id && c.Ped.Position.DistanceTo(faller.Ped.Position) < FallSeen &&
                                             ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(c.Ped.Handle, faller.Ped.Handle, 17))
                                 .OrderByDescending(c => _store.Ties.Any(t => t.Has(c.Who.Id) && t.Has(faller.Who.Id)) ? 1 : 0)
                                 .ThenBy(c => c.Ped.Position.DistanceTo(faller.Ped.Position)).FirstOrDefault();
            if (by == null)
            {
                why = _fallWhyNot = "nobody near " + faller.Who.Name + " to see it";
                return false;
            }
            long g = _store.LastGameMin;
            string skipped;
            string pace = Pacing.WhyNot(_store.Pace, "fall", _placeTown, faller.Who.Id, g, _rng.Next(100), test, out skipped);
            if (pace != null)
            {
                why = _fallWhyNot = "rests: " + pace;
                return false;
            }
            SceneTie tie = _store.Ties.FirstOrDefault(t => t.Has(by.Who.Id) && t.Has(faller.Who.Id));
            string seed = by.Who.Id + "|" + faller.Who.Id + "|" + g / 60;
            string answer = Scenes.FallAnswer(tie != null ? (int?)tie.Warmth : null, Exchanges.Manner(by.Who, by.Who.Hurt != null), by.Who.Temper, seed);
            bool old = Director.OldModel(faller.Ped);
            SceneCast cast = BuildCast(by.Ped, faller.Ped, null, false, me, now, seed);
            Scene s = Scenes.Fall(cast, seed, answer, old);
            StartScene(s, new List<Ped> { by.Ped, faller.Ped }, cast, seed, now, test);
            if (!_director.Busy)
            {
                why = _fallWhyNot = "the Director wouldn't take the scene";
                return false;
            }
            Pacing.Note(_store.Pace, "fall", _placeTown, faller.Who.Id, g);
            _fallWatch = new FallWatch { Fallen = faller.Ped, By = by.Ped, Name = faller.Who.Name, Who = faller.Who, Woman = faller.Who.Woman, Answer = answer, Seed = seed, Until = now + FallWatchMs };
            int c0;
            _fallCount.TryGetValue(answer, out c0);
            _fallCount[answer] = c0 + 1;
            _fallLast = faller.Who.Name + " fell; " + by.Who.Name + " " + FallVerb(answer) + (test ? " (a test)" : "");
            Log.Write("KIND fall: " + faller.Who.Name + " fell" + (old ? " (sat down hard)" : "") + "; " + by.Who.Name + " " + FallVerb(answer) + " (" +
                      (tie != null ? (tie.Warmth > 0 ? "a friend" : tie.Warmth < 0 ? "no friend" : "they know each other") : "strangers") + ", " + (by.Who.Temper ?? "no temper") + ", " +
                      (int)faller.Ped.Position.DistanceTo(me.Position) + " m from you)" + (test ? ", a test" + (skipped != null ? " (skipped: " + skipped + ")" : "") : ""));
            return true;
        }

        private static string FallVerb(string answer)
        {
            return answer == "help" ? "helps them up" : answer == "gruff" ? "hauls them up, gruffly" : answer == "sneer" ? "sneers and walks on" : "walks past";
        }

        /// Why this person can't take a fall here and now, or null: walking (any pace short of a run), in your view within
        /// 14 m, outdoors, on level ground (no stairs, steps or slopes: the ground within 0.7 m all round within 25 cm),
        /// not in water or a scenario, not one of enRAGE's records (enRAGE runs them), and grown.
        private string FallCant(Ped p, Ped me, bool test)
        {
            int h = p.Handle;
            float speed = ENTITY.GET_ENTITY_SPEED(h);
            if (!test && (speed < 0.8f || speed > 2.6f))
                return "not walking";
            if (p.Position.DistanceTo(me.Position) > FallFromYou || !NodInView(me, p))
                return "out of view";
            if (IsRecord(h) || PED.IS_PED_USING_ANY_SCENARIO(h) || ENTITY.IS_ENTITY_IN_WATER(h) || INTERIOR.GET_INTERIOR_FROM_ENTITY(h) != 0 || !IsGrownUp(p))
                return "not here";
            Vector3 pos = p.Position;
            float mid = GroundBelow(pos.X, pos.Y, pos.Z + 0.5f);
            if (mid < -500f || pos.Z - mid > 1.4f)
                return "no ground under them";
            foreach (float[] d in new[] { new[] { 0.7f, 0f }, new[] { -0.7f, 0f }, new[] { 0f, 0.7f }, new[] { 0f, -0.7f } })
            {
                float z = GroundBelow(pos.X + d[0], pos.Y + d[1], pos.Z + 0.5f);
                if (z < -500f || Math.Abs(z - mid) > 0.25f)
                    return "not level ground";
            }
            return null;
        }

        /// The ground height just below a point (from a little above it, so a roof or an awning overhead doesn't count),
        /// or -1000 if none.
        private static unsafe float GroundBelow(float x, float y, float from)
        {
            ulong slot = 0;   // a full 8-byte slot for the float out-parameter
            if (!MISC.GET_GROUND_Z_FOR_3D_COORD(x, y, from, (float*)&slot, false))
                return -1000f;
            float g = *(float*)&slot;
            return g < -200f || g > from ? -1000f : g;
        }

        /// While a fall is playing: if you reach them while they're still down, before the one who saw it has got to
        /// them (or when that one is walking on), you're the one who helps them up.
        private void FallWatchStep(Ped me, int now)
        {
            FallWatch w = _fallWatch;
            if (w == null)
                return;
            if (now > w.Until || w.Fallen == null || !w.Fallen.Exists() || w.Fallen.IsDead)
            {
                _fallWatch = null;
                return;
            }
            if (me.Position.DistanceTo(w.Fallen.Position) > HandUpMetres || !PED.IS_PED_USING_ANY_SCENARIO(w.Fallen.Handle) && !PED.IS_PED_RAGDOLL(w.Fallen.Handle))
                return;
            bool helperClose = (w.Answer == "help" || w.Answer == "gruff") && w.By != null && w.By.Exists() && w.By.Position.DistanceTo(w.Fallen.Position) < 3f;
            if (helperClose)
                return;   // the one helping is there already
            _fallWatch = null;
            bool nobodyElse = w.Answer == "pass" || w.Answer == "sneer";
            _director.Abort("you gave " + w.Name + " a hand up");   // the end gets them up off the ground
            if (w.By != null && w.By.Exists() && (w.Answer == "help" || w.Answer == "gruff"))
                TASK.TASK_WANDER_STANDARD(w.By.Handle, 10f, 10);   // the one who was coming goes on their way
            TASK.TASK_TURN_PED_TO_FACE_ENTITY(w.Fallen.Handle, me.Handle, 3000, 0f, 0f, 0f);
            TASK.TASK_LOOK_AT_ENTITY(w.Fallen.Handle, me.Handle, 4000, 0, 51, 0);
            Feed.Say(w.Name, Scenes.FallThanksYou(nobodyElse, w.Seed), Speech.When(w.Fallen.Handle, "thanks", me.Handle), w.Fallen.Handle, toYou: true);
            ResBind rb;
            if (_resBind.TryGetValue(w.Fallen.Handle, out rb) && !rb.R.Dead && !_store.Residents.Contains(rb.R))
            {
                _engine.KeepResident(rb.R, Bridge.Now());   // kept now, so the kindness is remembered
                Log.Write("KIND " + rb.R.Name + " is kept now, so your kindness is remembered");
            }
            if (w.Who.P != null)
                _engine.HelpedUp(w.Who.P, nobodyElse, Bridge.Now());
            if (w.Who.Id != null && w.Who.Id.StartsWith("folk:"))
                _bridge.LikeMore(w.Who.Id.Substring(5), 3);   // enRAGE's own liking for you, a little (an audited write)
            _handUps++;
            _fallLast = w.Name + " fell; you gave them a hand up" + (nobodyElse ? " (nobody else stopped)" : "");
            Log.Write("KIND fall: you gave " + w.Name + " a hand up" + (nobodyElse ? " (nobody else stopped)" : " (before the one coming got there)"));
        }

        /// F7: on or off, the falls this session by how they ended, your hands up, the last, and why nothing now.
        private string KindnessStatus()
        {
            if (!_cfg.Kindnesses)
                return "off ([Scenes] Kindnesses)";
            return "on; falls this session: " + (_fallCount.Count == 0 ? "none yet" : string.Join(", ", _fallCount.Select(kv => kv.Key + " " + kv.Value))) + "; you helped " + _handUps +
                   (_fallLast != null ? "; last: " + _fallLast : "") + (_fallWatch != null ? "; one going on" : _fallWhyNot != null ? "; now: " + _fallWhyNot : "");
        }
    }
}
