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
            public bool Woman, Test;
            public int Until, Start, CloseSince;
            public float YouFrom;   // how far you were when they fell: a hand up means coming to them
        }

        private FallWatch _fallWatch;
        private int _fallWander, _fallWanderAt;
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
            long g = _store.LastGameMin;
            string skipped;
            string pace = Pacing.WhyNot(_store.Pace, "fall", _placeTown, null, g, _rng.Next(100), test, out skipped);   // the rarity first: no scan for nothing
            if (pace != null)
            {
                why = _fallWhyNot = "rests: " + pace;
                return false;
            }
            List<Candidate> people = ReasonCandidates(me, now, test);
            Candidate faller = people.Where(c => c.Ped.Position.DistanceTo(me.Position) >= 3f && FallCant(c.Ped, me, test) == null)
                                     .OrderBy(c => c.Ped.Position.DistanceTo(me.Position)).FirstOrDefault();   // not right at your feet
            if (faller == null)
            {
                // what stood in the way, counted (Lex, 2026-10-27: "uneven ground" said for even ground; the old message
                // named only the ground whatever it was)
                string counted = string.Join(", ", people.Select(c => c.Ped.Position.DistanceTo(me.Position) < 3f ? "at your feet" : FallCant(c.Ped, me, test))
                                                         .GroupBy(r => r).OrderByDescending(gr => gr.Count()).Select(gr => gr.Count() + " " + gr.Key));
                why = _fallWhyNot = "nobody fits within " + FallFromYou + " m" + (people.Count > 0 ? " (" + people.Count + " named people near: " + counted + ")" : " (no named people free near you)") +
                                    "; everyone within " + FallFromYou + " m: " + CrowdCounted(me, FallFromYou);
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
            string skipped2;
            pace = Pacing.WhyNot(_store.Pace, "fall", _placeTown, faller.Who.Id, g, 0, test, out skipped2);   // and not the same person twice in a day
            if (pace != null)
            {
                why = _fallWhyNot = "rests: " + pace;
                return false;
            }
            // with MindLive and MindHelps: of those who saw it whom the mind knows, the one most moved to help is the one who
            // sees it, and their answer is their own feeling's (Scenes.FallAnswerOf); else the nearest, by the old pick
            Feeling byFelt = null;
            float byCompassion = 0f;
            string mindWhy = null;
            bool byMind = by != null && _cfg.MindLive && _cfg.MindHelps && FallByMind(people, faller, ref by, now, out byFelt, out byCompassion, out mindWhy);
            SceneTie tie = _store.Ties.FirstOrDefault(t => t.Has(by.Who.Id) && t.Has(faller.Who.Id));
            string seed = by.Who.Id + "|" + faller.Who.Id + "|" + g / 60;
            string answer = byMind ? Scenes.FallAnswerOf(byFelt, tie != null ? (int?)tie.Warmth : null, by.Who.Temper, byCompassion, seed)
                                   : Scenes.FallAnswer(tie != null ? (int?)tie.Warmth : null, Exchanges.Manner(by.Who, by.Who.Hurt != null), by.Who.Temper, seed);
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
            _fallWatch = new FallWatch { Fallen = faller.Ped, By = by.Ped, Name = faller.Who.Name, Who = faller.Who, Woman = faller.Who.Woman, Answer = answer, Seed = seed, Until = now + FallWatchMs,
                                         Start = now, YouFrom = faller.Ped.Position.DistanceTo(me.Position), Test = test };
            int c0;
            _fallCount.TryGetValue(answer, out c0);
            _fallCount[answer] = c0 + 1;
            _fallLast = faller.Who.Name + " fell; " + by.Who.Name + " " + FallVerb(answer) + (test ? " (a test)" : "");
            Log.Write("KIND fall: " + faller.Who.Name + " fell" + (old ? " (sat down hard)" : "") + "; " + by.Who.Name + " " + FallVerb(answer) + " (" +
                      (tie != null ? (tie.Warmth > 0 ? "a friend" : tie.Warmth < 0 ? "no friend" : "they know each other") : "strangers") + ", " + (by.Who.Temper ?? "no temper") + ", " +
                      (int)faller.Ped.Position.DistanceTo(me.Position) + " m from you)" + (byMind ? "; the mind: " + mindWhy : "") + (test ? ", a test" + (skipped != null ? " (skipped: " + skipped + ")" : "") : ""));
            return true;
        }

        /// Who sees the fall by their own minds: each of those near enough to see it whom the mind already knows takes it
        /// (Scenes.FallSeenBy: nobody's doing, someone needing a hand) and the one most moved to help is the one; when none
        /// is, the old pick stays and gives their own answer if the mind knows them. False: the mind knows none of them, or
        /// its part failed (logged once): the old way.
        private bool FallByMind(List<Candidate> people, Candidate faller, ref Candidate by, int now, out Feeling felt, out float compassion, out string why)
        {
            felt = null;
            compassion = 0f;
            why = null;
            if (_fallMindFailed)
                return false;
            try
            {
                string place = _engine.NowIndoors ? _engine.NowPlace : null;
                int seen = 0, bestWarm = 0;
                bool frail = faller.Who.Woman || Director.OldModel(faller.Ped);   // a woman or an old person down: more hands offered in 1899
                int seenNear = people.Count(c => c != faller && c.Ped.Position.DistanceTo(faller.Ped.Position) < FallSeen);
                Candidate best = null, nearest = by;
                Feeling bestF = null, nearF = null;
                float bestC = 0f, nearC = 0f;
                foreach (Candidate c in people)
                {
                    if (c == faller || c.Who.Id == faller.Who.Id || c.Ped.Position.DistanceTo(faller.Ped.Position) >= FallSeen || !ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(c.Ped.Handle, faller.Ped.Handle, 17))
                        continue;
                    seen++;
                    MindWho w = MindKnown(c.Ped.Handle, now);   // only people the add-on already knows (never makes anyone)
                    if (w != null && w.Id == null && c.Who.Id != null)
                    {
                        _mindWho.Remove(c.Ped.Handle);   // made a resident a moment ago by the same scan, while the cache had them as nobody
                        w = MindKnown(c.Ped.Handle, now);
                    }
                    if (w == null || w.Id == null || w.Id != c.Who.Id || w.P == null)
                        continue;
                    Traits t = w.Traits ?? (w.Traits = _engine.Shadow.TraitsOf(w.Src));
                    if (t == null)
                        continue;
                    int warm = TieWarmth(c.Who.Id, faller.Who);
                    AppraisalEvent e = Scenes.FallSeenBy(w.P.Woman, warm, place, w.P.Home != null && w.P.Home == _placeTown, Math.Max(0, seenNear - 1), c.Who.Drunk, c.Who.WakeStage, faller.Who.Id, faller.Who.Name, frail);
                    Feeling f = Appraisal.Of(e, t);
                    if (c == nearest)
                    {
                        nearF = f;
                        nearC = t.Z[Traits.Compassion];
                    }
                    if (warm < 0 || f.Kind != "pity" || f.Urge != "help")
                        continue;   // a foe is never the one who comes to help (the tie's own answer for him: a sneer or past)
                    float d = bestF == null ? 1f : f.Intensity - bestF.Intensity;
                    bool better = bestF == null || d > StepIn.NearTie ||
                                  Math.Abs(d) <= StepIn.NearTie && (warm > bestWarm || warm == bestWarm && c.Ped.Position.DistanceTo(faller.Ped.Position) < best.Ped.Position.DistanceTo(faller.Ped.Position));   // near a tie: a friend first, then the nearer
                    if (better)
                    {
                        best = c;
                        bestF = f;
                        bestC = t.Z[Traits.Compassion];
                        bestWarm = warm;
                    }
                }
                if (best != null)
                {
                    by = best;
                    felt = bestF;
                    compassion = bestC;
                    why = best.Who.Name + ": pity (" + bestF.Strength + "), urge help, of " + seen + " who saw it";
                    return true;
                }
                if (nearF == null)
                    return false;   // the mind doesn't know the one nearest: the old way
                felt = nearF;
                compassion = nearC;
                why = "nobody moved to help of " + seen + " who saw it; " + nearest.Who.Name + " felt " + nearF.Kind + (nearF.Kind != "none" ? " (" + nearF.Strength + ")" : "");
                return true;
            }
            catch (Exception ex)
            {
                _fallMindFailed = true;
                Log.Write("KIND the mind's part of the fall failed once, the old way from here: " + ex.Message);
                felt = null;
                return false;
            }
        }

        private bool _fallMindFailed;

        private static string FallVerb(string answer)
        {
            return answer == "help" ? "helps them up" : answer == "gruff" ? "hauls them up, gruffly" : answer == "sneer" ? "sneers and walks on" : "walks past";
        }

        /// Why this person can't take a fall here and now, or null: walking (any pace short of a run), in your view within
        /// 14 m, outdoors, on level enough ground (no stairs or slopes: the ground 0.7 m round them within 40 cm at three of
        /// four points, so a boardwalk's edge passes; Lex, 2026-10-27: 25 cm at all four turned down even streets),
        /// not in water or a scenario, not one of enRAGE's records (enRAGE runs them), and grown.
        private string FallCant(Ped p, Ped me, bool test)
        {
            int h = p.Handle;
            float speed = ENTITY.GET_ENTITY_SPEED(h);
            if (!test && (speed < 0.8f || speed > 2.6f))
                return "not walking";
            if (p.Position.DistanceTo(me.Position) > FallFromYou)
                return "further than " + FallFromYou + " m";
            if (!ENTITY.IS_ENTITY_ON_SCREEN(h))
                return "not on your screen";
            if (!ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(me.Handle, h, 17))
                return "something between you";
            if (IsRecord(h))
                return "one of enRAGE's records";
            if (PED.IS_PED_SITTING(h))
                return "sitting";
            if (PED.IS_PED_USING_ANY_SCENARIO(h) && !test)
                return "standing, leaning or at work in one spot";   // a test may tip over someone standing about (the tumble breaks the scenario; Lex, 2026-10-27)
            if (ENTITY.IS_ENTITY_IN_WATER(h) || INTERIOR.GET_INTERIOR_FROM_ENTITY(h) != 0 || !IsGrownUp(p))
                return "indoors or in water";
            Vector3 pos = p.Position;
            float mid = GroundBelow(pos.X, pos.Y, pos.Z + 0.5f);
            if (mid < -500f || pos.Z - mid > 1.4f)
                return "no ground under them";
            int off = 0;
            foreach (float[] d in new[] { new[] { 0.7f, 0f }, new[] { -0.7f, 0f }, new[] { 0f, 0.7f }, new[] { 0f, -0.7f } })
            {
                float z = GroundBelow(pos.X + d[0], pos.Y + d[1], pos.Z + 0.5f);
                if (z < -500f || Math.Abs(z - mid) > 0.4f)
                    off++;
            }
            return off > 1 ? "on stairs or a slope" : null;
        }

        /// Everyone within the radius of you, counted by what keeps them out of the street scenes (for a refusal): the
        /// game's own scripts' people (mission entities, never directed), lawmen, children, riding or in a vehicle,
        /// fighting or fleeing, enRAGE's busy people, and the rest (free to take part).
        private string CrowdCounted(Ped me, float radius)
        {
            var why = new Dictionary<string, int>();
            Action<string> add = k => why[k] = why.TryGetValue(k, out int n) ? n + 1 : 1;
            foreach (Ped p in World.GetAllPeds())
            {
                if (p == null || !p.Exists() || p.Handle == me.Handle || p.IsDead || !PED.IS_PED_HUMAN(p.Handle) || p.Position.DistanceTo(me.Position) > radius)
                    continue;
                int h = p.Handle;
                if (Natives.IsChild(p))
                    add("children");
                else if (Natives.IsLaw(p))
                    add("lawmen");
                else if (PED.IS_PED_ON_MOUNT(h) || PED.IS_PED_IN_ANY_VEHICLE(h, false))
                    add("riding or in a wagon");
                else if (PED.IS_PED_IN_COMBAT(h, 0) || PED.IS_PED_FLEEING(h))
                    add("fighting or fleeing");
                else if (Natives.OtherScripts(h) && !IsRecord(h))
                    add("the game's own scripts' people");
                else if (MayDirectWhyNot(p) != null)
                    add("busy (enRAGE's, a scene, a story)");
                else
                    add("free");
            }
            return why.Count == 0 ? "nobody" : string.Join(", ", why.OrderByDescending(kv => kv.Value).Select(kv => kv.Value + " " + kv.Key));
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
            // a hand up: they're sat on the ground (not mid-tumble), you're on foot, and you came to them (or stood by them)
            float d = me.Position.DistanceTo(w.Fallen.Position);
            if (d > HandUpMetres || now - w.Start < 3000 || !PED.IS_PED_USING_ANY_SCENARIO(w.Fallen.Handle) || PED.IS_PED_ON_MOUNT(me.Handle) || PED.IS_PED_IN_ANY_VEHICLE(me.Handle, false))
            {
                w.CloseSince = 0;
                return;
            }
            if (w.CloseSince == 0)
                w.CloseSince = now;
            if (w.YouFrom - d < 1.5f && now - w.CloseSince < 1500)
                return;
            bool helperClose = (w.Answer == "help" || w.Answer == "gruff") && w.By != null && w.By.Exists() && w.By.Position.DistanceTo(w.Fallen.Position) < 3f;
            if (helperClose)
                return;   // the one helping is there already
            _fallWatch = null;
            bool nobodyElse = w.Answer == "pass" || w.Answer == "sneer";
            if (nobodyElse && now - w.Start > 9000 && _pendingNote != null && _pendingNote.S.Kind == "passby")
            {
                // the one who saw it had walked past already: that stands, whoever helped them up after
                Pending p = _pendingNote;
                _pendingNote = null;
                NoteHappened(p, "passby");
            }
            _director.Abort("you gave " + w.Name + " a hand up");   // the end gets them up off the ground
            if (w.By != null && w.By.Exists() && (w.Answer == "help" || w.Answer == "gruff"))
                TASK.TASK_WANDER_STANDARD(w.By.Handle, 10f, 10);   // the one who was coming goes on their way
            TASK.TASK_TURN_PED_TO_FACE_ENTITY(w.Fallen.Handle, me.Handle, 3000, 0f, 0f, 0f);
            TASK.TASK_LOOK_AT_ENTITY(w.Fallen.Handle, me.Handle, 4000, 0, 51, 0);
            Feed.Say(w.Name, Scenes.FallThanksYou(nobodyElse, w.Seed), Speech.When(w.Fallen.Handle, "thanks", me.Handle), w.Fallen.Handle, toYou: true);
            _fallWander = w.Fallen.Handle;   // and on their way, once they've said it
            _fallWanderAt = now + 4000;
            _handUps++;
            _fallLast = w.Name + " fell; you gave them a hand up" + (nobodyElse ? " (nobody else stopped)" : "") + (w.Test ? " (a test)" : "");
            Log.Write("KIND fall: you gave " + w.Name + " a hand up" + (nobodyElse ? " (nobody else stopped)" : " (before the one coming got there)") + (w.Test ? ", a test: nothing kept" : ""));
            if (w.Test)
                return;   // a test keeps nobody and changes nobody's mind
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
        }

        /// The one you helped up goes on their way a moment after thanking you.
        private void FallWanderStep(int now)
        {
            if (_fallWander == 0 || now < _fallWanderAt)
                return;
            int h = _fallWander;
            _fallWander = 0;
            if (ENTITY.DOES_ENTITY_EXIST(h) && !ENTITY.IS_ENTITY_DEAD(h) && !_director.Involves(h) && !PED.IS_PED_USING_ANY_SCENARIO(h))
                TASK.TASK_WANDER_STANDARD(h, 10f, 10);
        }

        /// F7: on or off, the falls this session by how they ended, your hands up, the last, and why nothing now.
        private string KindnessStatus()
        {
            if (!_cfg.Kindnesses)
                return "off ([Scenes] Kindnesses)";
            return "on; falls this session: " + (_fallCount.Count == 0 ? "none yet" : string.Join(", ", _fallCount.Select(kv => kv.Key + " " + kv.Value))) + "; you helped " + _handUps +
                   (_fallLast != null ? "; last: " + _fallLast : "") + (_fallWatch != null ? "; one going on" : _fallWhyNot != null ? "; now: " + _fallWhyNot : "") +
                   "; standing up to a bully: " + StandUpStatus() + "; rounds: " + RoundStatus();
        }
    }
}
