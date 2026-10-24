// The Awakening on screen. A person who has started to wake behaves differently when the player is near, and the
// behaviour follows what a mind in that state might do:
//   1 stirring     double-takes: a glance a moment too long, a stop and a stare, looking round at nothing
//   2 questioning  experiments: standing perfectly still, pacing the same steps, walking out to see where the town
//                  ends, asking a neighbour if they feel it too
//   3 unravelling  distress: freezing mid-step, erratic pacing, shouting at the sky, kneeling, running with no goal
//   4 awake        a lasting state: they come to you, talk to you (who is writing this, is there an outside), then
//                  end in one of: pleading with you, trying to leave town, trying to wake someone, refusing to
//                  get up; and, rarely and only if the setting allows, being unable to bear it
// Who is directed: ordinary people no script owns are moved about. enRAGE's own people and script-owned peds (the
// soldiers in its camps, for example) are only given the passive part - head turns, thoughts, a voice - because
// their movement belongs to someone else. Nothing happens during story missions, in a scene, or in danger, and
// everyone is handed back: events unblocked, tasks cleared. Each act's thoughts and speech come from WakeBeats.
using System;
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using NemesisSystem;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private sealed class WakeAct
        {
            public int Handle;
            public string Kind;
            public int Stage;
            public int Started, Until, NextStep, PauseUntil, NextPause, NextThought, PhaseUntil;
            public int Phase, Said, Lines, Legs, Steps, LookIdx, Targets, Other;
            public Vector3 A, B, Goal;
            public bool ToB, Arrived, Blocked, Issued, Passive;
            public string Ending;
            public string Name;
            public Person P;
            public PersonState St;
            public Resident Res;
        }

        private readonly List<WakeAct> _wakeActs = new List<WakeAct>();
        private readonly Dictionary<int, int> _wakeCool = new Dictionary<int, int>();
        private readonly HashSet<int> _selfHarmed = new HashSet<int>();
        private int _nextWakeScan, _lastWakeActAt;
        private const int MaxWakeActs = 3;

        // ---- who, and how far we may go -----------------------------------------------------------------

        /// Who a nearby person is, if they have begun to wake. passive: only head turns, thoughts and a voice,
        /// because their movement is not ours (enRAGE's own people, script-owned peds).
        private bool WakeIdentity(Ped ped, bool mayBind, out Person p, out PersonState st, out Resident res, out bool passive)
        {
            p = null;
            st = null;
            res = null;
            int h = ped.Handle;
            passive = !CanDirect(ped);
            NemesisRecord rec = _bridge.RecordAt(h);
            if (rec != null)
            {
                if (!rec.IsAlive)
                    return false;
                passive = true;   // enRAGE's encounters and camps give its people their orders
                p = PersonOf(rec);
                st = _store.Find(p.Id);
                return st != null && st.WakeStage > 0;
            }
            Acquaintance a = _bridge.AcquaintanceAt(h);
            if (a != null)
            {
                if (string.IsNullOrEmpty(a.Name) || Natives.StoryModel(ped))
                    return false;
                Folk f = Snapshot.Of(a);
                p = _engine.PersonOfFolk(f.Key, f.Name, f.Woman, f.Town, Bridge.Now());
                st = _store.Find(p.Id);
                return st != null && st.WakeStage > 0;
            }
            ResBind b;
            if (_resBind.TryGetValue(h, out b) && b.Model == ENTITY.GET_ENTITY_MODEL(h))
                res = b.R;
            else if (mayBind)
                res = ResidentOf(ped);
            if (res == null || res.Dead)
                return false;
            p = _engine.PersonOfResident(res);
            st = _store.Find(res.Id);
            return st != null && st.WakeStage > 0;
        }

        /// Free to be moved about: nobody else's, not busy, not in danger.
        private bool CanDirect(Ped ped)
        {
            int h = ped.Handle;
            return !ENTITY.IS_ENTITY_DEAD(h) && !PED.IS_PED_IN_COMBAT(h, 0) && !PED.IS_PED_FLEEING(h) && !PED.IS_PED_RAGDOLL(h) && !PED.IS_PED_ON_MOUNT(h) &&
                   !PED.IS_PED_IN_ANY_VEHICLE(h, false) && !PED.IS_PED_SWIMMING(h) && !ENTITY.IS_ENTITY_IN_WATER(h) && !ENTITY.IS_ENTITY_A_MISSION_ENTITY(h) && !Spawner.IsOurs(h);
        }

        private bool LawBusy(Ped ped)
        {
            return Natives.IsLaw(ped) && (PLAYER.GET_PLAYER_WANTED_LEVEL(Game.Player.Handle) > 0 || LAW.IS_LAW_INCIDENT_ACTIVE(Game.Player.Handle));
        }

        private string ChooseWakeAct(int stage, bool sitting, bool passive)
        {
            int r = _rng.Next(100);
            if (passive)
            {
                switch (stage)
                {
                    case Awakening.Stirring: return r < 25 ? "glance" : r < 65 ? "lookaround" : "stare";
                    case Awakening.Questioning: return r < 55 ? "stare" : "lookaround";
                    case Awakening.Unravelling: return r < 60 ? "shout" : "stare";
                    default: return r < 85 ? "awake" : "shout";
                }
            }
            switch (stage)
            {
                case Awakening.Stirring:
                    return sitting ? (r < 40 ? "glance" : "lookaround") : r < 20 ? "glance" : r < 55 ? "lookaround" : "stare";
                case Awakening.Questioning:
                    return sitting ? (r < 50 ? "stare" : "lookaround") : r < 15 ? "stare" : r < 45 ? "pace" : r < 65 ? "edge" : r < 85 ? "ask" : "lookaround";
                case Awakening.Unravelling:
                    return sitting ? (r < 50 ? "shout" : "kneel") : r < 20 ? "pace" : r < 35 ? "freeze" : r < 55 ? "shout" : r < 75 ? "kneel" : "run";
                default:
                    return r < 92 ? "awake" : "stare";
            }
        }

        // ---- the loop -------------------------------------------------------------------------------------

        private void WakeActsTick(int now)
        {
            if (_missionPaused || _cfg == null || _cfg.AwakeningShare <= 0)
            {
                if (_wakeActs.Count > 0)
                    WakeActsAbort();
                return;
            }
            Ped me = Game.Player.Character;
            for (int i = _wakeActs.Count - 1; i >= 0; i--)
            {
                WakeAct act = _wakeActs[i];
                string why = WhyEnded(act, me, now);
                if (why != null)
                {
                    EndWakeAct(act, why);
                    _wakeActs.RemoveAt(i);
                    continue;
                }
                if (now >= act.NextStep)
                {
                    act.NextStep = now + 400;
                    StepWakeAct(act, me, now);
                }
            }
            if (now < _nextWakeScan)
                return;
            _nextWakeScan = now + 2500;
            if (_wakeActs.Count >= MaxWakeActs || _director.Busy || now - _lastWakeActAt < 3000 || !PLAYER.IS_PLAYER_CONTROL_ON(Game.Player.Handle))
                return;
            int examined = 0, bound = 0;
            foreach (Ped ped in World.GetAllPeds().Where(x => x.Handle != me.Handle && x.Position.DistanceTo(me.Position) <= 30f).OrderBy(x => x.Position.DistanceTo(me.Position)))
            {
                if (examined++ >= 8)
                    break;
                int h = ped.Handle;
                int cool;
                if ((_wakeCool.TryGetValue(h, out cool) && now < cool) || _wakeActs.Any(a => a.Handle == h) || !PED.IS_PED_HUMAN(h) || ENTITY.IS_ENTITY_DEAD(h) ||
                    PED.IS_PED_IN_COMBAT(h, 0) || PED.IS_PED_FLEEING(h) || LawBusy(ped))
                    continue;
                bool known = _resBind.ContainsKey(h);
                if (!known && bound >= 3)
                    continue;
                Person p;
                PersonState st;
                Resident res;
                bool passive;
                if (!WakeIdentity(ped, true, out p, out st, out res, out passive))
                {
                    if (!known)
                        bound++;
                    continue;
                }
                string kind = ChooseWakeAct(st.WakeStage, !passive && PED.IS_PED_USING_ANY_SCENARIO(h), passive);
                _wakeCool[h] = now + (kind == "awake" ? 120000 : 15000 + _rng.Next(20000));
                if (_wakeCool.Count > 200)
                    _wakeCool.Clear();
                StartWakeAct(ped, p, st, res, passive, kind, me, now);
                break;
            }
        }

        /// The most telling act of a stage, shown straight away (the wake key). Ends whatever they were doing.
        private void WakeDemo(int handle, Person p, PersonState st, Resident res, Ped me, int now)
        {
            if (!ENTITY.DOES_ENTITY_EXIST(handle) || _missionPaused)
                return;
            Ped ped = new Ped(handle);
            if (LawBusy(ped))
            {
                Log.Write("WAKE act: " + p.Name + " is a lawman and you are wanted; not now");
                return;
            }
            WakeAct old = _wakeActs.FirstOrDefault(a => a.Handle == handle);
            if (old != null)
            {
                EndWakeAct(old, "replaced by the next stage");
                _wakeActs.Remove(old);
            }
            bool passive = !CanDirect(ped) || _bridge.RecordAt(handle) != null;
            string kind;
            int r = _rng.Next(100);
            switch (st.WakeStage)
            {
                case Awakening.Stirring: kind = passive ? "stare" : r < 50 ? "lookaround" : "stare"; break;
                case Awakening.Questioning: kind = passive ? "lookaround" : r < 35 ? "edge" : r < 70 ? "ask" : "pace"; break;
                case Awakening.Unravelling: kind = passive ? "shout" : r < 30 ? "kneel" : r < 60 ? "run" : "shout"; break;
                default: kind = "awake"; break;
            }
            _wakeCool[handle] = now + (kind == "awake" ? 120000 : 20000);
            StartWakeAct(ped, p, st, res, passive, kind, me, now);
        }

        private void StartWakeAct(Ped ped, Person p, PersonState st, Resident res, bool passive, string kind, Ped me, int now)
        {
            int h = ped.Handle;
            WakeAct act = new WakeAct { Handle = h, Kind = kind, Stage = st.WakeStage, Started = now, NextStep = now + 400, Name = p.Name, P = p, St = st, Res = res, Passive = passive, A = ped.Position };
            if (passive && (kind == "pace" || kind == "freeze" || kind == "edge" || kind == "ask" || kind == "kneel" || kind == "run"))
                act.Kind = "stare";
            if (!BeginAct(act, ped, me, now))
            {
                act.Kind = "stare";
                BeginAct(act, ped, me, now);
            }
            string seed = p.Id + "|" + act.Kind + "|" + now / 1000;
            Think(act, WakeBeats.Opening(act.Kind, st.WakeStage, p, st, _engine.Player, seed));
            if (act.Kind == "stare")
                act.NextThought = now + 5500;
            if (act.Kind == "freeze")
            {
                string lost = WakeBeats.Lost(p, st, _engine.Player, seed + "lost");
                if (lost != null)
                    Feed.Mutter(p.Name, p.Woman, lost);
            }
            _wakeActs.Add(act);
            _lastWakeActAt = now;
            Log.Write("WAKE act: " + p.Name + " (" + Awakening.StageName(st.WakeStage) + ") " + act.Kind + (passive ? " [passive]" : "") + ", " + (int)ped.Position.DistanceTo(me.Position) + " m away");
        }

        /// Sets the act going. False if it can't be done here (no room to walk, nobody to talk to): the caller falls back.
        private bool BeginAct(WakeAct act, Ped ped, Ped me, int now)
        {
            int h = act.Handle;
            switch (act.Kind)
            {
                case "glance":
                    TASK.TASK_LOOK_AT_ENTITY(h, me.Handle, 4500, 0, 51, 0);
                    act.Until = now + 4500;
                    return true;
                case "lookaround":
                    act.Until = now + 8000;
                    act.NextStep = now;
                    return true;
                case "stare":
                    if (!act.Passive)
                    {
                        Take(act);
                        if (!PED.IS_PED_USING_ANY_SCENARIO(h))
                        {
                            TASK.CLEAR_PED_TASKS(h, true, false);
                            TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, me.Handle, 2500, 0f, 0f, 0f);
                        }
                    }
                    TASK.TASK_LOOK_AT_ENTITY(h, me.Handle, 9000, 0, 51, 0);
                    act.Until = now + 9000;
                    return true;
                case "freeze":
                    Take(act);
                    TASK.CLEAR_PED_TASKS(h, true, false);
                    TASK.TASK_STAND_STILL(h, 12000);
                    act.Until = now + 12000;
                    return true;
                case "pace":
                {
                    Take(act);
                    float len = act.Stage >= Awakening.Unravelling ? 5f : 8f;
                    act.B = PointAround(act.A, len, act.A.Z);
                    act.Until = now + (act.Stage >= Awakening.Unravelling ? 35000 : 22000);
                    act.NextPause = now + 6000;
                    act.Steps = (int)(len / 0.75f);
                    act.NextStep = now;
                    act.ToB = true;
                    return true;
                }
                case "edge":
                {
                    Vector3 goal;
                    if (!FarPoint(act.A, 38f, out goal))
                        return false;
                    Take(act);
                    act.Goal = goal;
                    TASK.CLEAR_PED_TASKS(h, true, false);
                    TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(h, goal.X, goal.Y, goal.Z, 1.3f, 30000, 1.5f, 0, 0f);
                    act.Until = now + 45000;
                    return true;
                }
                case "ask":
                {
                    Ped other = NearbyPerson(act.A, h, me.Handle, 12f);
                    if (other == null)
                        return false;
                    Take(act);
                    act.Other = other.Handle;
                    TASK.CLEAR_PED_TASKS(h, true, false);
                    TASK.TASK_GO_TO_ENTITY(h, act.Other, 12000, 2.5f, 1.0f, 0f, 0);
                    act.Until = now + 22000;
                    return true;
                }
                case "shout":
                {
                    Vector3 sky = act.A + new Vector3(0f, 0f, 25f);
                    TASK.TASK_LOOK_AT_COORD(h, sky.X, sky.Y, sky.Z, 5000, 0, 51, 0);
                    if (!act.Passive)
                    {
                        Take(act);
                        TASK.CLEAR_PED_TASKS(h, true, false);
                        TASK.TASK_STAND_STILL(h, 5000);
                    }
                    act.Until = now + 5500;
                    ActSay(act, WakeBeats.Aloud("shout", act.Stage, act.P, act.St, _engine.Player, act.P.Id + "|shout|" + now / 1000), me, "angry");
                    return true;
                }
                case "kneel":
                    Take(act);
                    TASK.CLEAR_PED_TASKS(h, true, false);
                    TASK.TASK_START_SCENARIO_IN_PLACE_HASH(h, Natives.Hash("WORLD_HUMAN_GRAVE_MOURNING_KNEEL"), 14000, true, 0u, -1f, false);
                    act.Until = now + 14000;
                    act.NextThought = now + 4500;
                    return true;
                case "run":
                    Take(act);
                    TASK.CLEAR_PED_TASKS(h, true, false);
                    if (!NextRunLeg(act))
                        return false;
                    act.Until = now + 30000;
                    return true;
                case "awake":
                    if (!act.Passive)
                    {
                        Take(act);
                        TASK.CLEAR_PED_TASKS(h, true, false);
                        TASK.TASK_GO_TO_ENTITY(h, me.Handle, 20000, 4f, 1.2f, 0f, 0);
                    }
                    act.Phase = 0;
                    act.PhaseUntil = now + 16000;
                    act.Lines = 2 + _rng.Next(2);
                    act.Until = now + 100000;
                    return true;
                default:
                    return false;
            }
        }

        // ---- geometry -------------------------------------------------------------------------------------

        private Vector3 PointAround(Vector3 from, float len, float level)
        {
            double ang = _rng.NextDouble() * Math.PI * 2;
            Vector3 b = from + new Vector3((float)Math.Cos(ang) * len, (float)Math.Sin(ang) * len, 0f);
            Vector3 stand;
            return Natives.StandSpot(b, level, out stand) ? stand : from;
        }

        /// A walkable spot about `dist` away in a random direction.
        private bool FarPoint(Vector3 from, float dist, out Vector3 goal)
        {
            for (int i = 0; i < 6; i++)
            {
                double ang = _rng.NextDouble() * Math.PI * 2;
                Vector3 g = from + new Vector3((float)Math.Cos(ang) * dist, (float)Math.Sin(ang) * dist, 0f);
                if (Natives.StandSpot(g, from.Z, out goal))
                    return true;
            }
            goal = from;
            return false;
        }

        private bool NextRunLeg(WakeAct act)
        {
            Vector3 goal;
            if (!FarPoint(new Ped(act.Handle).Position, 14f + (float)_rng.NextDouble() * 6f, out goal))
                return false;
            act.Goal = goal;
            act.Targets++;
            act.NextPause = Game.GameTime + 7000;
            TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(act.Handle, goal.X, goal.Y, goal.Z, 3f, 8000, 1.5f, 0, 0f);
            return true;
        }

        private Ped NearbyPerson(Vector3 around, int not1, int not2, float within)
        {
            Ped best = null;
            float bestD = within;
            foreach (Ped p in World.GetAllPeds())
            {
                if (p.Handle == not1 || p.Handle == not2 || !PED.IS_PED_HUMAN(p.Handle) || ENTITY.IS_ENTITY_DEAD(p.Handle) || Spawner.IsOurs(p.Handle) || Natives.IsChild(p))
                    continue;
                float d = p.Position.DistanceTo(around);
                if (d > 2f && d < bestD)
                {
                    best = p;
                    bestD = d;
                }
            }
            return best;
        }

        // ---- stepping an act --------------------------------------------------------------------------------

        private void StepWakeAct(WakeAct act, Ped me, int now)
        {
            int h = act.Handle;
            Vector3 pos = new Ped(h).Position;
            float dist = pos.DistanceTo(me.Position);
            switch (act.Kind)
            {
                case "lookaround":
                {
                    double heading = ENTITY.GET_ENTITY_HEADING(h) * Math.PI / 180.0;
                    Vector3 fwd = new Vector3((float)-Math.Sin(heading), (float)Math.Cos(heading), 0f);
                    Vector3 side = new Vector3(fwd.Y, -fwd.X, 0f);
                    Vector3 at;
                    switch (act.LookIdx++ % 4)
                    {
                        case 0: at = pos + new Vector3(0f, 0f, 25f) + fwd * 4f; break;
                        case 1: at = pos - fwd * 8f + new Vector3(0f, 0f, 1.2f); break;
                        case 2: at = pos + side * 8f + new Vector3(0f, 0f, 1.2f); break;
                        default: at = pos - side * 8f + new Vector3(0f, 0f, 1.2f); break;
                    }
                    TASK.TASK_LOOK_AT_COORD(h, at.X, at.Y, at.Z, 2100, 0, 51, 0);
                    act.NextStep = now + 2200;
                    break;
                }
                case "stare":
                    if (act.NextThought > 0 && now >= act.NextThought)
                    {
                        act.NextThought = 0;
                        Think(act, WakeBeats.Opening("stare", act.Stage, act.P, act.St, _engine.Player, act.P.Id + "|stare2|" + now / 1000));
                    }
                    if (dist > 25f)
                        act.Until = now;
                    break;
                case "pace":
                    StepPace(act, pos, now);
                    break;
                case "edge":
                    if (act.Phase == 0 && pos.DistanceTo(act.Goal) < 2.5f)
                    {
                        act.Phase = 1;
                        Vector3 beyond = act.Goal + (act.Goal - act.A).Normalized * 40f;
                        TASK.CLEAR_PED_TASKS(h, true, false);
                        TASK.TASK_STAND_STILL(h, 9000);
                        TASK.TASK_LOOK_AT_COORD(h, beyond.X, beyond.Y, beyond.Z + 2f, 9000, 0, 51, 0);
                        Think(act, WakeBeats.Opening("edgeEnd", act.Stage, act.P, act.St, _engine.Player, act.P.Id + "|edgeEnd|" + now / 1000));
                        act.Until = Math.Min(act.Until, now + 9500);
                    }
                    break;
                case "ask":
                    if (act.Phase == 0)
                    {
                        if (!ENTITY.DOES_ENTITY_EXIST(act.Other) || ENTITY.IS_ENTITY_DEAD(act.Other))
                        {
                            act.Until = now;
                            break;
                        }
                        if (pos.DistanceTo(new Ped(act.Other).Position) < 3.4f)
                        {
                            act.Phase = 1;
                            TASK.CLEAR_PED_TASKS(h, true, false);
                            TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, act.Other, 3000, 0f, 0f, 0f);
                            TASK.TASK_LOOK_AT_ENTITY(act.Other, h, 6000, 0, 51, 0);
                            ActSay(act, WakeBeats.Aloud("ask", act.Stage, act.P, act.St, _engine.Player, act.P.Id + "|ask|" + now / 1000), me, "chat");
                            act.Until = now + 7500;
                        }
                    }
                    break;
                case "kneel":
                    if (act.NextThought > 0 && now >= act.NextThought)
                    {
                        act.NextThought = 0;
                        string sob = WakeBeats.Aloud("sob", act.Stage, act.P, act.St, _engine.Player, act.P.Id + "|sob|" + now / 1000);
                        if (sob != null)
                            Feed.Mutter(act.Name, act.P.Woman, sob);
                        Think(act, WakeBeats.Opening("kneel", act.Stage, act.P, act.St, _engine.Player, act.P.Id + "|kneel2|" + now / 1000));
                    }
                    break;
                case "run":
                    if (pos.DistanceTo(act.Goal) < 2.5f || now >= act.NextPause)
                    {
                        if (act.Targets >= 3 || !NextRunLeg(act))
                            act.Until = now;
                        else
                            Think(act, WakeBeats.Opening("run", act.Stage, act.P, act.St, _engine.Player, act.P.Id + "|run|" + act.Targets + "|" + now / 1000));
                    }
                    break;
                case "awake":
                    StepAwake(act, me, pos, dist, now);
                    break;
            }
        }

        private void StepPace(WakeAct act, Vector3 pos, int now)
        {
            int h = act.Handle;
            if (now < act.PauseUntil)
                return;
            if (act.Stage >= Awakening.Unravelling && now >= act.NextPause)
            {
                // stuck for a moment, as if the loop caught
                act.NextPause = now + 5000 + _rng.Next(4000);
                act.PauseUntil = now + 1800;
                act.Issued = false;
                TASK.CLEAR_PED_TASKS(h, true, false);
                TASK.TASK_STAND_STILL(h, 1800);
                return;
            }
            Vector3 goal = act.ToB ? act.B : act.A;
            if (act.Issued && pos.DistanceTo(goal) < 1.4f)
            {
                act.Legs++;
                if (act.Stage >= Awakening.Unravelling)
                    act.B = PointAround(act.A, 4f + (float)_rng.NextDouble() * 4f, act.A.Z);   // no longer even: a different way each time
                else
                    act.ToB = !act.ToB;
                act.Issued = false;
                goal = act.ToB ? act.B : act.A;
                if (_rng.Next(100) < 80)
                    Think(act, WakeBeats.AtTheEnd(act.Stage, act.Legs, act.Steps, act.P, act.St, _engine.Player, act.P.Id + "|leg|" + act.Legs + "|" + now / 1000));
            }
            if (!act.Issued)
            {
                act.Issued = true;
                TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(h, goal.X, goal.Y, goal.Z, act.Stage >= Awakening.Unravelling ? 1.8f : 1.0f, 8000, 0.8f, 0, 0f);
            }
        }

        // ---- the awake ----------------------------------------------------------------------------------------

        private void StepAwake(WakeAct act, Ped me, Vector3 pos, float dist, int now)
        {
            int h = act.Handle;
            switch (act.Phase)
            {
                case 0:   // coming to you
                    if ((!act.Passive && dist < 6f) || (act.Passive && dist < 20f) || now > act.PhaseUntil)
                    {
                        if (act.Passive && dist >= 20f)
                        {
                            act.Until = now;
                            break;
                        }
                        act.Phase = 1;
                        if (!act.Passive)
                            TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, me.Handle, 3000, 0f, 0f, 0f);
                        TASK.TASK_LOOK_AT_ENTITY(h, me.Handle, 30000, 0, 51, 0);
                        act.PhaseUntil = now + 900;
                    }
                    break;
                case 1:   // saying it
                    if (now < act.PhaseUntil)
                        break;
                    if (act.Said < act.Lines)
                    {
                        string line = WakeBeats.Aloud("plea", act.Stage, act.P, act.St, _engine.Player, act.P.Id + "|plea|" + act.Said + "|" + now / 1000);
                        act.Said++;
                        if (line != null)
                        {
                            ActSay(act, line, me, "plead");
                            act.PhaseUntil = now + 5500 + line.Length * 45;
                        }
                        break;
                    }
                    act.Phase = 2;
                    break;
                case 2:   // how it ends
                    ChooseEnding(act, me, pos, dist, now);
                    break;
                case 3:
                    StepEnding(act, me, pos, dist, now);
                    break;
            }
        }

        private void ChooseEnding(WakeAct act, Ped me, Vector3 pos, float dist, int now)
        {
            int h = act.Handle;
            string ending = "plead";
            if (!act.Passive)
            {
                int r = _rng.Next(100);
                ending = r < 30 ? "escape" : r < 55 ? "wake" : r < 80 ? "kneel" : "plead";
                if (DespairRoll(act, me, dist))
                    ending = WEAPON.IS_PED_ARMED(h, 4) ? "despair" : "giveup";
            }
            act.Ending = ending;
            act.Phase = 3;
            act.Arrived = false;
            act.PhaseUntil = now;
            switch (ending)
            {
                case "plead":
                {
                    act.Until = now + 9000;
                    string line = WakeBeats.Aloud("plea", act.Stage, act.P, act.St, _engine.Player, act.P.Id + "|plea-end|" + now / 1000);
                    if (line != null)
                        ActSay(act, line, me, "plead");
                    break;
                }
                case "escape":
                {
                    Vector3 away = pos + (pos - me.Position).Normalized * 55f, goal;
                    if (!Natives.StandSpot(away, pos.Z, out goal) && !FarPoint(pos, 50f, out goal))
                    {
                        act.Ending = "plead";
                        act.Until = now + 6000;
                        break;
                    }
                    act.Goal = goal;
                    TASK.CLEAR_PED_TASKS(h, true, false);
                    TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(h, goal.X, goal.Y, goal.Z, 3f, 30000, 2f, 0, 0f);
                    Think(act, WakeBeats.Opening("run", Awakening.Unravelling, act.P, act.St, _engine.Player, act.P.Id + "|esc|" + now / 1000));
                    act.Until = now + 40000;
                    break;
                }
                case "wake":
                {
                    Ped other = NearbyPerson(pos, h, me.Handle, 20f);
                    if (other == null)
                    {
                        act.Ending = "plead";
                        act.Until = now + 6000;
                        break;
                    }
                    act.Other = other.Handle;
                    TASK.CLEAR_PED_TASKS(h, true, false);
                    TASK.TASK_GO_TO_ENTITY(h, act.Other, 14000, 2f, 1.6f, 0f, 0);
                    act.Until = now + 26000;
                    break;
                }
                case "kneel":
                case "giveup":
                    TASK.CLEAR_PED_TASKS(h, true, false);
                    TASK.TASK_START_SCENARIO_IN_PLACE_HASH(h, Natives.Hash("WORLD_HUMAN_GRAVE_MOURNING_KNEEL"), 25000, true, 0u, -1f, false);
                    act.Until = now + 25000;
                    act.NextThought = now + 3500;
                    break;
                case "despair":
                    TASK.CLEAR_PED_TASKS(h, true, false);
                    TASK.TASK_STAND_STILL(h, 4500);
                    act.Until = now + 12000;
                    ActSay(act, WakeBeats.Aloud("last", act.Stage, act.P, act.St, _engine.Player, act.P.Id + "|last"), me, null);
                    act.PhaseUntil = now + 3400;
                    break;
            }
        }

        private void StepEnding(WakeAct act, Ped me, Vector3 pos, float dist, int now)
        {
            int h = act.Handle;
            switch (act.Ending)
            {
                case "escape":
                    if (!act.Arrived && pos.DistanceTo(act.Goal) < 3f)
                    {
                        act.Arrived = true;
                        TASK.CLEAR_PED_TASKS(h, true, false);
                        TASK.TASK_STAND_STILL(h, 7000);
                        ActSay(act, WakeBeats.Aloud("edgeShout", act.Stage, act.P, act.St, _engine.Player, act.P.Id + "|edge|" + now / 1000), me, "angry");
                        act.Until = now + 7000;
                    }
                    break;
                case "wake":
                    if (!act.Arrived && ENTITY.DOES_ENTITY_EXIST(act.Other) && pos.DistanceTo(new Ped(act.Other).Position) < 3.4f)
                    {
                        act.Arrived = true;
                        TASK.CLEAR_PED_TASKS(h, true, false);
                        TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, act.Other, 4000, 0f, 0f, 0f);
                        TASK.TASK_LOOK_AT_ENTITY(act.Other, h, 8000, 0, 51, 0);
                        ActSay(act, WakeBeats.Aloud("wake", act.Stage, act.P, act.St, _engine.Player, act.P.Id + "|wake|" + now / 1000), me, "scared");
                        act.Until = now + 8500;
                    }
                    break;
                case "kneel":
                case "giveup":
                    if (act.NextThought > 0 && now >= act.NextThought)
                    {
                        act.NextThought = 0;
                        string pool = act.Ending == "giveup" ? "giveup" : "sob";
                        string sob = WakeBeats.Aloud(pool, act.Stage, act.P, act.St, _engine.Player, act.P.Id + "|" + pool + "|" + now / 1000);
                        if (sob != null)
                            Feed.Mutter(act.Name, act.P.Woman, sob);
                    }
                    break;
                case "despair":
                    StepDespair(act, me, now);
                    break;
            }
        }

        // ---- the rare breakdown --------------------------------------------------------------------------------

        /// Rolled once for each awake person, at the end of their encounter, and only for an ordinary person nobody
        /// else owns, with no one near enough to stop them.
        private bool DespairRoll(WakeAct act, Ped me, float dist)
        {
            if (act.Res == null || act.Passive || _cfg.AwakeningDespair <= 0 || act.St.Has("despair:rolled") || dist < 3f || MISC.GET_MISSION_FLAG() ||
                PLAYER.IS_PLAYER_FREE_AIMING_AT_ENTITY(Game.Player.Handle, act.Handle) || ENTITY.IS_ENTITY_A_MISSION_ENTITY(act.Handle))
                return false;
            act.St.Mark("despair:rolled");
            return _rng.Next(100) < _cfg.AwakeningDespair;
        }

        private unsafe void StepDespair(WakeAct act, Ped me, int now)
        {
            int h = act.Handle;
            if (act.Phase == 3 && now >= act.PhaseUntil)
            {
                act.Phase = 4;
                ulong slot = 0;
                WEAPON.GET_CURRENT_PED_WEAPON(h, (uint*)&slot, true, 0, false);
                uint weapon = (uint)slot;
                Vector3 pos = new Ped(h).Position;
                double heading = ENTITY.GET_ENTITY_HEADING(h) * Math.PI / 180.0;
                Vector3 head = pos + new Vector3(0f, 0f, 0.62f);
                Vector3 from = head + new Vector3((float)-Math.Sin(heading) * 0.35f, (float)Math.Cos(heading) * 0.35f, 0f);
                _selfHarmed.Add(h);   // theirs alone: never booked as the player's doing
                MISC.SHOOT_SINGLE_BULLET_BETWEEN_COORDS(from.X, from.Y, from.Z, head.X, head.Y, head.Z, 500, true, weapon, h, true, false, 1000f, false);
                act.PhaseUntil = now + 1500;
            }
            else if (act.Phase == 4 && now >= act.PhaseUntil)
            {
                act.Phase = 5;
                if (!ENTITY.IS_ENTITY_DEAD(h))
                    ENTITY.SET_ENTITY_HEALTH(h, 0, h);
                if (act.Res != null)
                {
                    act.Res.Dead = true;
                    _engine.KeepResident(act.Res, Bridge.Now());
                }
                Log.Write("WAKE despair: " + act.Name + " (a resident) could not bear it and ended their own life");
                act.Until = now;
            }
        }

        // ---- shared ----------------------------------------------------------------------------------------------

        /// Why an act has to stop now, or null.
        private string WhyEnded(WakeAct act, Ped me, int now)
        {
            if (!ENTITY.DOES_ENTITY_EXIST(act.Handle)) return "gone";
            if (ENTITY.IS_ENTITY_DEAD(act.Handle)) return act.Ending == "despair" ? "ended" : "died";
            if (act.Phase >= 4 && act.Ending == "despair") return null;   // let the last moments play out
            if (now >= act.Until) return act.Kind == "awake" && act.Phase == 0 ? "never reached you" : "finished";
            if (PED.IS_PED_IN_COMBAT(act.Handle, 0)) return "got into a fight";
            if (PED.IS_PED_RAGDOLL(act.Handle) && act.Kind != "run") return "was knocked down";
            if (PLAYER.IS_PLAYER_FREE_AIMING_AT_ENTITY(Game.Player.Handle, act.Handle)) return "you aimed at them";
            if (new Ped(act.Handle).Position.DistanceTo(me.Position) > (act.Kind == "awake" ? 80f : 60f)) return "too far away";
            return null;
        }

        /// What the person thinks as they do it: on the ticker, in the thinking style.
        private void Think(WakeAct act, string text)
        {
            if (string.IsNullOrEmpty(text))
                return;
            Notice(act.Name, text, "thought", act.P.Woman ? "herself" : "himself");
        }

        /// Something said aloud, ahead of the subtitle queue, in their own voice when the game has a fitting line.
        private void ActSay(WakeAct act, string line, Ped me, string tag)
        {
            if (string.IsNullOrEmpty(line))
                return;
            Feed.SayFirst(act.Name, line, tag == null ? null : Speech.For(act.Handle, line, me.Handle, act.P.Hate, act.P.Fear, act.P.Friendly, tag));
        }

        /// Keeps ordinary reactions from interrupting the act.
        private void Take(WakeAct act)
        {
            if (act.Passive)
                return;
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(act.Handle, true);
            act.Blocked = true;
        }

        private void EndWakeAct(WakeAct act, string why = null)
        {
            if (why != null)
                Log.Write("WAKE act ended: " + act.Name + " " + act.Kind + (act.Ending != null ? " -> " + act.Ending : "") + " (" + why + ")");
            int h = act.Handle;
            if (!ENTITY.DOES_ENTITY_EXIST(h))
                return;
            if (act.Blocked)
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, false);
            if (!act.Passive && act.Kind != "glance" && act.Kind != "lookaround" && !ENTITY.IS_ENTITY_DEAD(h) && act.Ending != "despair")
                TASK.CLEAR_PED_TASKS(h, true, false);
        }

        /// Gives everyone back, now (a mission started, or the add-on is stopping).
        private void WakeActsAbort()
        {
            foreach (WakeAct act in _wakeActs)
                EndWakeAct(act);
            _wakeActs.Clear();
        }
    }
}
