// The night visitor ([Encounters] NightVisitor; the words are Core/Visitor.cs): after midnight in a town, out of
// doors with no lawman within 60 m, a pale figure (a spawned man, half seen through) crouches over a body (a spawned
// one, laid dead; never anyone real) 30-50 m from you where you can't see it. It rises and stares when you come within 25 m;
// the moment you look away it's gone; 8-20 s later it stands 12-20 m behind you, out of sight; seen, and looked away
// from again (or 40 s unseen), it's gone for good. Someone out late within 15 m who sees it first runs. Next morning
// the town speaks of a body nobody can account for, and the body is gone (taken away unseen once you're 150 m off). It
// can't be hurt and never fights: aimed at or touched, it's gone. Once a night (MarkDone), a big moment (Pacing).
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
        private sealed class VisitorRun
        {
            public Ped Figure, Body;
            public uint FigureModel, BodyModel;
            public Vector3 At;
            public string Phase = "spawn", Town;   // spawn, crouched, staring, away, behind
            public int StartedAt, PhaseAt, SeenSince, Tries;
            public bool Test, Fled;
        }

        private VisitorRun _visitor;
        private readonly List<Ped> _visitorLeft = new List<Ped>();   // the body (or a figure in view) left behind once it's over, taken away unseen
        private int _visitors;
        private string _visitorLast;

        /// Why the night visitor can't come here and now; null when it can.
        private string VisitorNotNow(bool test = false)
        {
            string town = _bridge.TownOnly(Game.Player.Character.Position);
            if (town == null)
                return "you're out in the wilds (it keeps to the towns)";
            if (_interiorType != 0)
                return "you're indoors";
            int clock = CLOCK.GET_CLOCK_HOURS();
            if (!test && !Visitor.Hour(clock))
                return "it's " + clock.ToString("00") + ":00 (it comes after midnight, before four)";
            if (!test && DoneToday("visitor"))
                return "it came already tonight";
            if (World.GetAllPeds().Any(p => p != null && p.Exists() && !p.IsDead && Natives.IsLaw(p) && p.Position.DistanceTo(Game.Player.Character.Position) < 60f))
                return "a lawman is near";
            return null;
        }

        private void StartVisitor(int now, bool test = false)
        {
            _visitor = new VisitorRun
            {
                StartedAt = now, PhaseAt = now, Test = test, Town = _bridge.TownOnly(Game.Player.Character.Position),
                FigureModel = Spawner.ModelFor(null, false, _rng.Next()), BodyModel = Spawner.ModelFor(null, _rng.Next(4) == 0, _rng.Next())
            };
            Log.Write("VISITOR begins in " + (_visitor.Town ?? "town") + ", game time " + Clock(Bridge.Now()) + (test ? " (a test)" : ""));
        }

        private void VisitorStep(int now)
        {
            VisitorRun v = _visitor;
            Ped me = Game.Player.Character;
            if (me == null || !me.Exists() || me.IsDead)
            {
                EndVisitor("you died");
                return;
            }
            string gone = VisitorGoneWhy(v, me, now);
            if (gone != null)
            {
                EndVisitor(gone);
                return;
            }
            switch (v.Phase)
            {
                case "spawn":
                    VisitorSpawn(v, me, now);
                    return;
                case "crouched":
                    if (!VisitorThere(v))
                        return;
                    VisitorWitness(v, me, now);
                    if (me.Position.DistanceTo(v.Figure.Position) < 25f)
                    {
                        TASK.CLEAR_PED_TASKS(v.Figure.Handle, true, false);
                        TASK.TASK_TURN_PED_TO_FACE_ENTITY(v.Figure.Handle, me.Handle, 2500, 0f, 0f, 0f);
                        TASK.TASK_LOOK_AT_ENTITY(v.Figure.Handle, me.Handle, -1, 0, 51, 0);
                        v.Phase = "staring";
                        v.PhaseAt = now;
                        v.SeenSince = 0;
                        Log.Write("VISITOR rises and stares at you (" + (int)me.Position.DistanceTo(v.Figure.Position) + " m)");
                    }
                    else if (now - v.StartedAt > 6 * 60000)
                        EndVisitor("you never came near");
                    return;
                case "staring":
                    if (!VisitorThere(v))
                        return;
                    if (VisitorSeen(me, v.Figure))
                        v.SeenSince = v.SeenSince == 0 ? now : v.SeenSince;
                    else if (v.SeenSince != 0 && now - v.PhaseAt > 1500)
                    {
                        // you looked away: gone
                        Spawner.Release(v.Figure, true);
                        v.Figure = null;
                        v.Phase = "away";
                        v.PhaseAt = now + 8000 + _rng.Next(12000);
                        Log.Write("VISITOR: you looked away, and it's gone");
                    }
                    if (v.Phase == "staring" && now - v.PhaseAt > 60000)
                        EndVisitor(v.SeenSince != 0 ? "it stared a minute and you never looked away" : "it stood a minute where you couldn't see it");
                    return;
                case "away":
                    if (now < v.PhaseAt)
                        return;
                    Vector3 at;
                    if (!Spawner.Ready(v.FigureModel) || !EncounterSpot(me, 12f, 20f, out at))
                    {
                        if (++v.Tries > 12)
                            EndVisitor("nowhere behind you to stand");
                        return;
                    }
                    Ped f = Spawner.Spawn(v.FigureModel, at, VisitorHeading(at, me.Position));
                    if (f == null)
                    {
                        if (++v.Tries > 12)
                            EndVisitor("nowhere behind you to stand");
                        return;
                    }
                    VisitorDress(f);
                    TASK.TASK_LOOK_AT_ENTITY(f.Handle, me.Handle, -1, 0, 51, 0);
                    TASK.TASK_STAND_STILL(f.Handle, 60000);
                    v.Figure = f;
                    v.Phase = "behind";
                    v.PhaseAt = now;
                    v.SeenSince = 0;
                    Log.Write("VISITOR stands behind you, " + (int)at.DistanceTo(me.Position) + " m off");
                    return;
                case "behind":
                    if (!VisitorThere(v))
                        return;
                    if (VisitorSeen(me, v.Figure))
                    {
                        if (v.SeenSince == 0)
                        {
                            v.SeenSince = now;
                            Log.Write("VISITOR: you saw it behind you");
                        }
                    }
                    else if ((v.SeenSince != 0 && now - v.SeenSince > 1500) || now - v.PhaseAt > 40000)
                        EndVisitor(v.SeenSince != 0 ? "you looked away again, and it was gone" : "it stood behind you and you never turned");
                    return;
            }
        }

        /// Why it's over whatever it was doing: you went far from where it was (a fast travel, a respawn), into another
        /// town or indoors, the night turned (not for a test), or eight minutes in all.
        private string VisitorGoneWhy(VisitorRun v, Ped me, int now)
        {
            if (now - v.StartedAt > 8 * 60000)
                return "eight minutes passed";
            if (v.Phase == "spawn")
                return null;
            if (me.Position.DistanceTo(v.At) > 90f)
                return "you went far from it";
            string town = _bridge.TownOnly(me.Position);
            if (town != v.Town || _interiorType != 0)
                return "you left the street";
            if (!v.Test && !Visitor.Hour(CLOCK.GET_CLOCK_HOURS()))
                return "the night turned";
            return null;
        }

        /// The figure and the body, set out of sight: the body laid dead, the figure crouched over it.
        private void VisitorSpawn(VisitorRun v, Ped me, int now)
        {
            if (now - v.StartedAt > 20000 || v.Tries > 8)
            {
                EndVisitor("nowhere hidden to set it");
                return;
            }
            if (!Spawner.Ready(v.FigureModel) || !Spawner.Ready(v.BodyModel))
                return;
            Vector3 at;
            if (!EncounterSpot(me, 30f, 50f, out at) || World.GetAllPeds().Any(p => p != null && p.Exists() && p.Handle != me.Handle && !p.IsDead && p.Position.DistanceTo(at) < 8f))
            {
                v.Tries++;
                return;
            }
            Ped body = Spawner.Spawn(v.BodyModel, at, _rng.Next(360));
            if (body == null)
            {
                v.Tries++;
                return;
            }
            ENTITY.SET_ENTITY_HEALTH(body.Handle, 0, 0);   // laid dead where it lies
            Vector3 fat = at + new Vector3(0.9f, 0.4f, 0f);
            Ped f = Spawner.Spawn(v.FigureModel, fat, VisitorHeading(fat, at));
            if (f == null)
            {
                Spawner.Release(body, true);
                v.Tries++;
                return;
            }
            VisitorDress(f);
            TASK.TASK_START_SCENARIO_IN_PLACE_HASH(f.Handle, Director.Crouch, -1, true, 0u, -1f, false);
            v.Body = body;
            v.Figure = f;
            v.At = at;
            v.Phase = "crouched";
            v.PhaseAt = now;
            if (!v.Test)
                Pacing.Note(_store.Pace, "visitor", v.Town, null, _store.LastGameMin);   // out now: a big moment, counted once
            _encounterPeds.Add(f.Handle);
            _encounterPeds.Add(body.Handle);
            Log.Write("VISITOR crouched over a body " + (int)at.DistanceTo(me.Position) + " m off, where you can't see it");
        }

        /// The heading that faces from one point to another (0 north, anticlockwise).
        private static float VisitorHeading(Vector3 from, Vector3 to)
        {
            return (float)(Math.Atan2(-(to.X - from.X), to.Y - from.Y) * 180.0 / Math.PI);
        }

        /// Half seen through, untouchable, deaf to the world.
        private static void VisitorDress(Ped f)
        {
            int h = f.Handle;
            ENTITY.SET_ENTITY_ALPHA(h, 170, false);
            ENTITY.SET_ENTITY_INVINCIBLE(h, true);
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, true);
            PED.SET_PED_KEEP_TASK(h, true);
            WEAPON.REMOVE_ALL_PED_WEAPONS(h, true, true);
        }

        /// Still there to play with; aimed at, touched or shot at, it's simply gone (it never fights).
        private bool VisitorThere(VisitorRun v)
        {
            if (v.Figure == null || !v.Figure.Exists())
            {
                EndVisitor("it was gone");
                return false;
            }
            int h = v.Figure.Handle;
            if (PLAYER.IS_PLAYER_FREE_AIMING_AT_ENTITY(Game.Player.Handle, h) || ENTITY.HAS_ENTITY_BEEN_DAMAGED_BY_ANY_PED(h) || PED.IS_PED_RAGDOLL(h))
            {
                EndVisitor("you went for it, and it was gone");
                return false;
            }
            return true;
        }

        private static bool VisitorSeen(Ped me, Ped f)
        {
            return f != null && f.Exists() && ENTITY.IS_ENTITY_ON_SCREEN(f.Handle) && ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(me.Handle, f.Handle, 17);
        }

        /// Someone out late who comes upon it first, within 15 m with sight of it: they run.
        private void VisitorWitness(VisitorRun v, Ped me, int now)
        {
            if (v.Fled)
                return;
            foreach (Ped p in World.GetAllPeds())
            {
                if (p == null || !p.Exists() || p.IsDead || p.Handle == me.Handle || Spawner.IsOurs(p.Handle) || p.Position.DistanceTo(v.Figure.Position) > 15f ||
                    !Natives.Bystander(p, me.Handle, IsRecord(p.Handle)) || MayDirectWhyNot(p) != null || NotFree(p.Handle) || !ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(p.Handle, v.Figure.Handle, 17))
                    continue;
                v.Fled = true;
                TASK.TASK_SMART_FLEE_PED(p.Handle, v.Figure.Handle, 60f, 20000, 0, 3f, 0);
                Speech.SayTag(p.Handle, "scared", 0);
                if (NodInView(me, p))
                    _ticker.Push(Grammar.UpperFirst(Director.Kind(p, !PED.IS_PED_MALE(p.Handle))) + ", thinking", Visitor.Fled(p.Handle + "|" + now), "near", p.Handle);
                Log.Write("VISITOR: " + Director.Kind(p, !PED.IS_PED_MALE(p.Handle)) + " out late came upon it, and ran");
                return;
            }
        }

        private void EndVisitor(string why)
        {
            VisitorRun v = _visitor;
            _visitor = null;
            if (v == null)
                return;
            Ped me = Game.Player.Character;
            if (v.Figure != null && v.Figure.Exists())
            {
                if (VisitorSeen(me, v.Figure))
                {
                    TASK.TASK_WANDER_STANDARD(v.Figure.Handle, 10f, 10);   // in view: it walks off, and is gone once nobody sees
                    _visitorLeft.Add(v.Figure);
                }
                else
                    Spawner.Release(v.Figure, true);   // it was never there
            }
            if (v.Body != null && v.Body.Exists())
            {
                if (v.Phase != "spawn")
                    Safe("world events", () => RecordEvent("encounter:visitor", null, "a pale figure over a body", "player", "you", v.Body.Position, v.Body.Handle, v.Body.Handle, 0, why));
                bool seen = ENTITY.IS_ENTITY_ON_SCREEN(v.Body.Handle) && me != null && me.Exists() && v.Body.Position.DistanceTo(me.Position) < 150f;
                if (seen)
                    _visitorLeft.Add(v.Body);   // taken away once you've gone and can't see
                else
                    Spawner.Release(v.Body, true);
            }
            if (v.Phase != "spawn")
            {
                if (!v.Test)
                {
                    MarkDone("visitor");
                    _engine.VisitorSeen(v.Town, Bridge.Now(), v.Fled);   // the town's talk next morning: of a body and a figure only if someone ran from it
                }
                _visitors++;
                _visitorLast = (v.Town ?? "town") + ": " + why + (v.Test ? " (a test)" : "");
            }
            Log.Write("VISITOR over (" + why + ")");
        }

        /// The body left behind goes once nobody can see it go (you 150 m off or out of sight).
        private void VisitorBodyStep()
        {
            if (_visitorLeft.Count == 0)
                return;
            Ped me = Game.Player.Character;
            for (int i = _visitorLeft.Count - 1; i >= 0; i--)
            {
                Ped b = _visitorLeft[i];
                if (b == null || !b.Exists())
                {
                    _visitorLeft.RemoveAt(i);
                    continue;
                }
                if (me != null && me.Exists() && (b.Position.DistanceTo(me.Position) > 150f || !ENTITY.IS_ENTITY_ON_SCREEN(b.Handle)))
                {
                    Spawner.Release(b, true);
                    _visitorLeft.RemoveAt(i);
                    Log.Detail("VISITOR: gone, as if it never was");
                }
            }
        }

        /// A mission or shutdown: everything it left goes with it.
        private void VisitorLeftClear()
        {
            foreach (Ped b in _visitorLeft)
                if (b != null && b.Exists())
                    Spawner.Release(b, true);
            _visitorLeft.Clear();
        }

        private string VisitorStatus()
        {
            if (!_cfg.NightVisitor)
                return "off ([Encounters] NightVisitor)";
            return (_visitor != null ? "out now (" + _visitor.Phase + ")" : "not out") + "; " + _visitors + " this session" + (_visitorLast != null ? "; last: " + _visitorLast : "") +
                   "; now: " + (VisitorNotNow() ?? "it could come");
        }
    }
}
