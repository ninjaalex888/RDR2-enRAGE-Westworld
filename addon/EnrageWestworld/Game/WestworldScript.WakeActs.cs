// The Awakening on screen. A person who has started to wake behaves differently when the player is near, and the
// behaviour follows what a mind in that state might do:
//   1 stirring     double-takes: a glance a moment too long, a stop and a stare, looking round at nothing
//   2 questioning  experiments: standing perfectly still, pacing the same steps, walking out to see where the town
//                  ends, asking a neighbour if they feel it too
//   3 unravelling  distress: freezing mid-step, erratic pacing, shouting at the sky, kneeling, running with no goal
//   4 awake        a lasting state: they come to you, talk to you (who is writing this, is there an outside), then
//                  end in one of: pleading with you, trying to leave town, trying to wake someone, refusing to
//                  get up; and, if the setting allows, being unable to bear it (a gun that fits them, a knife, or
//                  a wall run at: Core/Despair.cs)
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
            public string Method, Weapon;   // despair: gun, wall or knife; the weapon to fetch (null: their own)
            public uint Arm;                // despair: the weapon in their hand
            public bool Given, Done, Clip;  // despair: we gave the weapon; the death was booked; the blade clip played
            public NemesisRecord Rec;       // despair: one of enRAGE's records (its death is written into its record)
            public int Sub, RunFrom;
            public float TopSpeed;
            public Vector3 WallAt, WallDir;
            public Person P;
            public PersonState St;
            public Resident Res;
        }

        private readonly List<WakeAct> _wakeActs = new List<WakeAct>();
        private readonly Dictionary<int, int> _wakeCool = new Dictionary<int, int>();
        private readonly HashSet<int> _selfHarmed = new HashSet<int>();
        private readonly HashSet<string> _despairWhyTold = new HashSet<string>();
        private readonly Dictionary<string, int> _despairCount = new Dictionary<string, int>();
        private string _despairLast;
        private int _bladeSet = -1;   // the blade hit-react set: -1 not looked for yet, 0 missing, 1 there
        // one roll per person: "rolled2" since 0.6.2, so everyone who had one when only the armed could (and so knelt)
        // gets a roll with the new endings
        private const string DespairMark = "despair:rolled2";
        private const string BladeDict = "mech_melee@blade@_male@_ambient@_healthy@_hit_reacts@stationary";
        private const string BladeClip = "hit_from_front_torso_impact_front_heavy_v1";
        private int _writheSet = -1;   // the game's wounded-on-the-ground loop: -1 not looked for yet, 0 missing, 1 there
        private const string WritheDict = "ai_combat@damage@writhe@base";
        private const string WritheClip = "writhe_loop";
        private const int RightHandBone = 22798;   // SKEL_R_HAND
        private const uint GiveReason = 0x2CD419DC, TakeReason = 0xF77DE93D;   // the game's default reasons for giving and taking a weapon
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
                passive = !MayDirect(ped);   // enRAGE's people act it out too, unless enRAGE has them busy (Lex, 2026-10-26)
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
            if (res.PostId != null)
                passive = true;   // a counter's person stays at the counter
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

        /// Does this unravelling or awake person break down this time, rather than keep it together (AwakeningBreakdown)?
        private bool Breaks()
        {
            return _rng.Next(100) < _cfg.AwakeningBreakdown;
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
                    case Awakening.Unravelling: return Breaks() ? "shout" : "stare";
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
                    if (Breaks())
                        return sitting ? (r < 50 ? "shout" : "kneel") : r < 35 ? "shout" : r < 70 ? "kneel" : "run";   // falling apart
                    return sitting ? "stare" : r < 50 ? "pace" : r < 80 ? "freeze" : "stare";   // holding it together, just
                default:
                    return r < 92 ? "awake" : "stare";
            }
        }

        // ---- the loop -------------------------------------------------------------------------------------

        private void WakeActsTick(int now)
        {
            if (_missionPaused || _cfg == null || !_cfg.WestWorld)
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
                    if (why == "ended")
                        DespairDied(act);
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
            bool passive = !MayDirect(ped) || PostKindOf(ped) != null;
            string kind;
            int r = _rng.Next(100);
            switch (st.WakeStage)
            {
                case Awakening.Stirring: kind = passive ? "stare" : r < 50 ? "lookaround" : "stare"; break;
                case Awakening.Questioning: kind = passive ? "lookaround" : r < 35 ? "edge" : r < 70 ? "ask" : "pace"; break;
                case Awakening.Unravelling: kind = passive ? "shout" : Breaks() ? (r < 30 ? "kneel" : r < 60 ? "run" : "shout") : (r < 50 ? "pace" : "freeze"); break;
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
                    Feed.Mutter(p.Name, p.Woman, lost, Speech.When(act.Handle, "trail"));   // the sentence stops partway
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
                            Feed.Mutter(act.Name, act.P.Woman, sob, Speech.When(act.Handle, "hum"));
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
                        if (act.Stage >= Awakening.Awake && Disbelieve(h, act.Name, act.P.Woman, act.P.Id + "|db|" + now / 60000, "faces you"))
                            act.PhaseUntil = now + 2600;   // the words of the plea wait for it
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
                default:   // and despair's last steps, 4 to 6
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
                ending = Breaks() ? (r < 50 ? "escape" : "kneel") : (r < 55 ? "wake" : "plead");
            }
            if (DespairRoll(act, me, dist))
                ending = "despair";
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
                {
                    // how: a gun if they carry one or are the kind to keep one, else a wall to run at, else a knife
                    // (Core/Despair.cs); nothing is put in their hand until the last moment (StepDespair)
                    Ped ped = new Ped(h);
                    bool armed = WEAPON.IS_PED_ARMED(h, 4);
                    Vector3 wall, dir;
                    bool hasWall = DespairWall(ped, me, out wall, out dir);
                    act.Method = Despair.Method(act.P, armed, hasWall, act.P.Id);
                    act.Rec = _bridge.RecordAt(h);   // one of enRAGE's people: booked in its record when it's over
                    act.WallAt = wall;
                    act.WallDir = dir;
                    act.Weapon = act.Method == "gun" ? (armed ? null : Despair.Gun(act.P)) : act.Method == "knife" ? Despair.Knife : null;
                    DespairSets();
                    if (act.Method == "knife" && _bladeSet == 1)
                        STREAMING.REQUEST_ANIM_DICT(BladeDict);   // asked for now: it played once in two when asked at the last step
                    if (act.Method == "knife" && _writheSet == 1)
                        STREAMING.REQUEST_ANIM_DICT(WritheDict);
                    TASK.CLEAR_PED_TASKS(h, true, false);
                    TASK.TASK_STAND_STILL(h, 4500);
                    // the last words said looking down at their own hands (Lex's P29)
                    double hd = ENTITY.GET_ENTITY_HEADING(h) * Math.PI / 180.0;
                    Vector3 hands = ped.Position + new Vector3((float)-Math.Sin(hd) * 0.45f, (float)Math.Cos(hd) * 0.45f, -0.35f);
                    TASK.TASK_LOOK_AT_COORD(h, hands.X, hands.Y, hands.Z, 4000, 0, 51, 0);
                    act.Until = now + 30000;
                    DespairLastWords(act, me);
                    act.PhaseUntil = now + 3400;
                    Log.Write("WAKE despair: " + act.Name + " can't bear it: " + DespairHow(act) + (hasWall ? " (a wall " + Flat(wall, ped.Position).ToString("0.0") + " m off)" : " (no wall to run at)") +
                              (armed ? ", armed" : ""));
                    break;
                }
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
                        if (act.Res != null && act.Stage >= Awakening.Awake)
                            _engine.Flee(act.Res, Bridge.Now());   // gone from town for good: a camp in the wilds, one day (WestworldScript.Hermits.cs)
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
                            Feed.Mutter(act.Name, act.P.Woman, sob, Speech.When(act.Handle, "hum"));
                    }
                    break;
                case "despair":
                    StepDespair(act, me, now);
                    break;
            }
        }

        // ---- the rare breakdown --------------------------------------------------------------------------------

        /// One roll for each awake person, once their encounter with the player is over, whatever they carry. Why it
        /// isn't rolled is logged once for each person and reason.
        private bool DespairRoll(WakeAct act, Ped me, float dist)
        {
            string why = DespairWhyNot(act, dist);
            if (why != null)
            {
                if (_despairWhyTold.Add(act.P.Id + "|" + why))
                    Log.Write("WAKE despair: not for " + act.Name + " (" + why + ")");
                return false;
            }
            act.St.Mark(DespairMark);
            bool yes = _rng.Next(100) < _cfg.AwakeningDespair;
            Log.Write("WAKE despair roll: " + act.Name + (yes ? " can't bear it" : " bears it") + " (" + _cfg.AwakeningDespair + "% chance, once for each person)");
            return yes;
        }

        /// Why this awake person won't end it here, or null. RealNPCs' residents and enRAGE's townsfolk (Lex, 2026-10-26:
        /// enRAGE marks a townsperson dead however they die, and blames the player only when the game names him as the
        /// cause, or no one and he had hurt them; here they are their own cause), never enRAGE's records, which it runs;
        /// never a counter keeper, a story character or a child, nor in a story mission; not with the player near
        /// enough to stop them, or aiming at them.
        private string DespairWhyNot(WakeAct act, float dist)
        {
            int h = act.Handle;
            if (_cfg.AwakeningDespair <= 0)
                return "AwakeningDespair is 0";
            // enRAGE's records too since Lex relaxed the rule (2026-10-26, with its author's permission): only while
            // enRAGE isn't driving them (act.Passive, from MayDirect)
            if (act.Res == null && _bridge.RecordAt(h) == null && _bridge.AcquaintanceAt(h) == null)
                return "not someone we know";
            if (act.Res != null && act.Res.PostId != null)
                return "keeps a counter";
            if (act.Passive)
                return "can't be directed now";
            // harm and despair of enRAGE's full records only through the record-safe path that books the death in enRAGE
            // (RecordFates: its history, and IsAlive if it missed the death; Lex's overnight brief, Tier 1.4)
            NemesisRecord despairRec = _bridge.RecordAt(h);
            if (despairRec != null && (!_cfg.RecordFates || !_bridge.CanBookDeath))
                return "one of enRAGE's records (" + (!_cfg.RecordFates ? "RecordFates is off" : "enRAGE's history and roster saving weren't found") + ")";
            string held = despairRec != null ? RecordFreeWhyNot(new Ped(h), despairRec) : null;
            if (held != null)
                return "one of enRAGE's records, not free of it (" + held + ")";
            if (act.St.Has(DespairMark))
                return "already had their one chance";
            Ped ped = new Ped(h);
            if (Natives.StoryModel(ped) || !IsGrownUp(ped))
                return "a story character, or not grown";
            if (MISC.GET_MISSION_FLAG())
                return "a story mission";
            if (ENTITY.IS_ENTITY_A_MISSION_ENTITY(h) && despairRec == null)
                return "a mission character";   // enRAGE marks its own people so; whether it has a hold on them is asked above
            if (dist < 3f)
                return "you were " + dist.ToString("0.0") + " m off, near enough to stop them";
            if (PLAYER.IS_PLAYER_FREE_AIMING_AT_ENTITY(Game.Player.Handle, h))
                return "you were aiming at them";
            return null;
        }

        /// Phase 3: the last words. 4: the act itself (each way has its own steps and the act a deadline, since
        /// nothing else stops it now: WhyEnded). 5: limp, then dead.
        private void StepDespair(WakeAct act, Ped me, int now)
        {
            int h = act.Handle;
            if (act.Phase == 3)
            {
                if (now < act.PhaseUntil)
                    return;
                string cant = MayDirectWhyNot(new Ped(h)) ?? (act.Rec != null ? RecordFreeWhyNot(new Ped(h), act.Rec) : null);
                if (cant != null)
                {
                    Log.Write("WAKE despair: " + act.Name + " is let be at the last moment (" + cant + ")");
                    act.Until = now;   // the act ends at the next look ("finished"); their one chance is spent
                    return;
                }
                act.Phase = 4;
                act.Sub = 0;
                act.Until = now + (act.Method == "wall" ? 20000 : 9000);
                _selfHarmed.Add(h);   // theirs alone: never booked as the player's doing
                if (act.Method == "wall")
                    DespairRunStart(act, now);
                else
                    DespairArm(act, now);
                return;
            }
            if (act.Phase == 4)
            {
                if (act.Method == "wall")
                    DespairRun(act, now);
                else if (now >= act.PhaseUntil)
                {
                    if (act.Method == "knife")
                        DespairKnife(act, now);
                    else if (act.Sub == 1)
                        DespairAimed(act, now);
                    else
                        DespairShot(act, now);
                }
                return;
            }
            if (act.Phase == 5 && now >= act.PhaseUntil && act.Method == "knife" && act.Sub == 1)
            {
                // on the ground after the blade: the game's wounded writhe for a few seconds, then still
                act.Sub = 2;
                bool loaded = _writheSet == 1 && STREAMING.HAS_ANIM_DICT_LOADED(WritheDict);
                if (loaded)
                    TASK.TASK_PLAY_ANIM(h, WritheDict, WritheClip, 4f, -4f, -1, 1, 0f, false, 0, false, "", false);
                act.PhaseUntil = now + (loaded ? 3500 : 0);
                act.Clip = loaded;   // its check logged once, a second in
                act.Until = Math.Max(act.Until, now + 6000);
                Log.Detail("WAKE despair: " + act.Name + (loaded ? " writhes" : " lies still (no writhe set)"));
                return;
            }
            if (act.Phase == 5 && act.Method == "knife" && act.Sub == 2 && act.PhaseUntil > 0 && now >= act.PhaseUntil - 2500 && act.Clip)
            {
                act.Clip = false;   // logged once, a second into the writhe
                Log.Write("WAKE despair: " + act.Name + "'s writhe " + (ENTITY.IS_ENTITY_PLAYING_ANIM(h, WritheDict, WritheClip, 3) ? "played" : "didn't play"));
            }
            if (act.Phase == 5 && now >= act.PhaseUntil)
            {
                act.Phase = 6;
                if (!ENTITY.IS_ENTITY_DEAD(h) || PED.IS_PED_INCAPACITATED(h))
                    ENTITY.SET_ENTITY_HEALTH(h, 0, h);   // out cold isn't dead: the game would let them get up
                act.Until = now + 1500;   // dead by the next look (WhyEnded "ended" books it), or let go
            }
        }

        /// The gun or the knife into their hand, only now, at the last moment, so nobody is ever left standing there
        /// holding one we gave them; a moment's hold before it's used.
        private void DespairArm(WakeAct act, int now)
        {
            int h = act.Handle;
            uint w = 0;
            if (act.Method == "gun" && act.Weapon == null)
            {
                w = WEAPON.GET_BEST_PED_WEAPON(h, false, false);
                if (w == 0 || w == Natives.Hash("WEAPON_UNARMED"))
                    act.Weapon = Despair.Gun(act.P);   // armed with nothing we can find: a gun that fits them
            }
            if (act.Weapon != null)
            {
                w = Natives.Hash(act.Weapon);
                if (!WEAPON.HAS_PED_GOT_WEAPON(h, w, 0, false))
                {
                    WEAPON.GIVE_WEAPON_TO_PED(h, w, act.Method == "gun" ? 6 : 1, true, false, 0, false, 0.5f, 1f, GiveReason, true, 0f, false);
                    WEAPON.SET_PED_DROPS_WEAPONS_WHEN_DEAD(h, false);   // nothing of ours to pick up off them
                    act.Given = true;
                }
            }
            act.Arm = w;
            if (act.Method == "gun")
            {
                // drawn with the game's own draw (not snapped into the hand), then raised toward the ground a step in
                // front of them, so the arm comes up and the barrel shows (Lex's P29; there's no self-harm animation)
                WEAPON.SET_CURRENT_PED_WEAPON(h, w, false, 0, false, false);
                Vector3 pos = new Ped(h).Position;
                double hd = ENTITY.GET_ENTITY_HEADING(h) * Math.PI / 180.0;
                Vector3 ground = pos + new Vector3((float)-Math.Sin(hd) * 0.9f, (float)Math.Cos(hd) * 0.9f, -0.95f);
                TASK.TASK_AIM_GUN_AT_COORD(h, ground.X, ground.Y, ground.Z, 4000, false, false);
                act.Sub = 1;
                act.PhaseUntil = now + 1100;   // a look at how the aim is going, then the hold
                return;
            }
            WEAPON.SET_CURRENT_PED_WEAPON(h, w, true, 0, false, false);
            TASK.TASK_STAND_STILL(h, 3000);
            act.RunFrom = now;   // the knife may wait a moment for its set
            act.PhaseUntil = now + 1400;
        }

        /// The gun, after its first second: whether the aim took (logged), then held a moment longer before the shot.
        private void DespairAimed(WakeAct act, int now)
        {
            int h = act.Handle;
            int status = TASK.GET_SCRIPT_TASK_STATUS(h, Natives.Hash("SCRIPT_TASK_AIM_GUN_AT_COORD"), true);
            bool inHand = WEAPON.GET_CURRENT_PED_WEAPON_ENTITY_INDEX(h, 0) != 0;
            Log.Write("WAKE despair: " + act.Name + (status == 0 || status == 1 ? " aims it at the ground" : " didn't take the aim (task status " + status + ")") +
                      ", the gun " + (inHand ? "in hand" : "not in hand"));
            act.Sub = 2;
            act.PhaseUntil = now + 1500;
        }

        /// The gun: one shot, from the gun hand into the head.
        private void DespairShot(WakeAct act, int now)
        {
            int h = act.Handle;
            Vector3 pos = new Ped(h).Position;
            double heading = ENTITY.GET_ENTITY_HEADING(h) * Math.PI / 180.0;
            Vector3 head = PED.GET_PED_BONE_COORDS(h, 21030, 0f, 0f, 0f);
            if (head.DistanceTo(pos) > 2f)
                head = pos + new Vector3(0f, 0f, 0.62f);
            Vector3 hand = PED.GET_PED_BONE_COORDS(h, RightHandBone, 0f, 0f, 0f);
            Vector3 from = hand.DistanceTo(pos) < 2f ? hand : head + new Vector3((float)-Math.Sin(heading) * 0.35f, (float)Math.Cos(heading) * 0.35f, 0f);
            MISC.SHOOT_SINGLE_BULLET_BETWEEN_COORDS(from.X, from.Y, from.Z, head.X, head.Y, head.Z, 500, true, act.Arm, h, true, false, 1000f, false);
            act.Phase = 5;
            act.PhaseUntil = now + 1500;
        }

        /// The knife: the game's own reeling from a blade to the body (when the set is there), the wound, and down.
        private void DespairKnife(WakeAct act, int now)
        {
            int h = act.Handle;
            if (act.Sub == 0)
            {
                if (_bladeSet == 1 && !STREAMING.HAS_ANIM_DICT_LOADED(BladeDict) && now - act.RunFrom < 2000)
                {
                    STREAMING.REQUEST_ANIM_DICT(BladeDict);   // a moment more for the set to load
                    act.PhaseUntil = now + 100;
                    return;
                }
                act.Sub = 1;
                act.Clip = _bladeSet == 1 && STREAMING.HAS_ANIM_DICT_LOADED(BladeDict);
                if (act.Clip)
                    TASK.TASK_PLAY_ANIM(h, BladeDict, BladeClip, 8f, -4f, -1, 0, 0f, false, 0, false, "", false);
                PED.APPLY_DAMAGE_TO_PED(h, 60, 0, 0, h);
                act.PhaseUntil = now + (act.Clip ? 700 : 200);
                return;
            }
            Log.Write("WAKE despair: " + act.Name + "'s knife: " + (act.Clip ? (ENTITY.IS_ENTITY_PLAYING_ANIM(h, BladeDict, BladeClip, 3) ? "the blade reel played" : "the blade reel didn't play") : "no blade reel (set not loaded)") + ", then down");
            PED.SET_PED_TO_RAGDOLL(h, 1500, 2500, 0, false, false, "");
            act.Phase = 5;
            act.PhaseUntil = now + 1800;   // then the writhe (StepDespair), if the set is there
            act.Clip = true;                // the writhe's check is logged once
        }

        /// The wall: a few stiff steps toward it if it's far, then a run straight at it.
        private void DespairRunStart(WakeAct act, int now)
        {
            int h = act.Handle;
            if (Flat(new Ped(h).Position, act.WallAt) > 4.5f)
            {
                Vector3 to = act.WallAt - act.WallDir * 3.2f;
                TASK.TASK_GO_STRAIGHT_TO_COORD(h, to.X, to.Y, to.Z, 1f, 9000, 40000f, 0f, 0);
                act.Sub = 0;
                act.PhaseUntil = now + 9000;
            }
            else
                DespairRunGo(act, now);
        }

        private void DespairRunGo(WakeAct act, int now)
        {
            Vector3 past = act.WallAt + act.WallDir * 1.5f;
            TASK.TASK_GO_STRAIGHT_TO_COORD(act.Handle, past.X, past.Y, past.Z, 3f, 5000, 40000f, 0f, 0);
            act.Sub = 1;
            act.RunFrom = now;
            act.TopSpeed = 0f;
            act.PhaseUntil = now + 5000;
        }

        /// Watched closely while they run: into the wall, and limp; or, if they never get there, limp where they stand.
        private void DespairRun(WakeAct act, int now)
        {
            int h = act.Handle;
            act.NextStep = now + 50;
            float d = Flat(new Ped(h).Position, act.WallAt);
            if (act.Sub == 0)
            {
                if (d <= 3.6f || now >= act.PhaseUntil)
                    DespairRunGo(act, now);
                return;
            }
            float speed = ENTITY.GET_ENTITY_SPEED(h);
            act.TopSpeed = Math.Max(act.TopSpeed, speed);
            if (d <= 0.75f || (now - act.RunFrom > 900 && speed < 0.8f && d <= 1.8f))
            {
                // limp as they meet it, the run carrying them in, with a push for the throw
                PED.SET_PED_TO_RAGDOLL(h, 6000, 9000, 0, false, false, "");
                ENTITY.APPLY_FORCE_TO_ENTITY(h, 1, act.WallDir.X * 3f, act.WallDir.Y * 3f, 0.5f, 0f, 0f, 0f, 0, false, true, true, false, true);
                PED.APPLY_DAMAGE_TO_PED(h, 60, 0, 0, h);
                Log.Write("WAKE despair: " + act.Name + " ran into the wall (" + act.TopSpeed.ToString("0.0") + " m/s at most, " + d.ToString("0.0") + " m from it)");
                act.Phase = 5;
                act.PhaseUntil = now + 1600;
                return;
            }
            if (now >= act.PhaseUntil || (now - act.RunFrom > 1500 && speed < 0.3f))
            {
                Log.Write("WAKE despair: " + act.Name + " never reached the wall (" + d.ToString("0.0") + " m short); went limp where they stood");
                PED.SET_PED_TO_RAGDOLL(h, 6000, 9000, 0, false, false, "");
                act.Phase = 5;
                act.PhaseUntil = now + 1200;
            }
        }

        /// A wall to run at: 2.5-10 m off at chest height, met square on, nothing low in the way, and the player not
        /// on the line to it.
        private bool DespairWall(Ped p, Ped me, out Vector3 at, out Vector3 dir)
        {
            at = dir = Vector3.Zero;
            Vector3 pos = p.Position, mp = me.Position, hit, normal;
            float floor = Natives.Probe(pos, new Vector3(pos.X, pos.Y, pos.Z - 2.5f), p.Handle, out hit, out normal) ? hit.Z : pos.Z - 1f;
            float best = float.MaxValue;
            for (int i = 0; i < 16; i++)
            {
                float a = i * 22.5f * (float)Math.PI / 180f, dx = -(float)Math.Sin(a), dy = (float)Math.Cos(a);
                Vector3 chest = new Vector3(pos.X, pos.Y, floor + 1.4f);
                if (!Natives.Probe(chest, new Vector3(chest.X + dx * 10f, chest.Y + dy * 10f, chest.Z), p.Handle, out hit, out normal) || Math.Abs(normal.Z) > 0.35f)
                    continue;
                float d = Flat(hit, chest);
                if (d < 2.5f || d > 10f || -(dx * normal.X + dy * normal.Y) < 0.6f)
                    continue;   // too near for a run, too far, or a glancing wall
                Vector3 knee = new Vector3(pos.X, pos.Y, floor + 0.5f), kh, kn;
                if (Natives.Probe(knee, new Vector3(knee.X + dx * (d - 0.5f), knee.Y + dy * (d - 0.5f), knee.Z), p.Handle, out kh, out kn))
                    continue;   // a rail, a crate or a step in the way
                float t = Math.Max(0f, Math.Min(d, (mp.X - pos.X) * dx + (mp.Y - pos.Y) * dy));
                if (Flat(mp, new Vector3(pos.X + dx * t, pos.Y + dy * t, mp.Z)) < 2f)
                    continue;   // the player is on the line
                float score = Math.Abs(d - 6f);
                if (score < best)
                {
                    best = score;
                    at = new Vector3(hit.X, hit.Y, pos.Z);
                    dir = new Vector3(dx, dy, 0f);
                }
            }
            return best < float.MaxValue;
        }

        /// Whether the blade set exists, looked for once and logged.
        private void DespairSets()
        {
            if (_bladeSet >= 0)
                return;
            _bladeSet = STREAMING.DOES_ANIM_DICT_EXIST(BladeDict) ? 1 : 0;
            _writheSet = STREAMING.DOES_ANIM_DICT_EXIST(WritheDict) ? 1 : 0;
            Log.Write("WAKE despair sets: the blade reel (" + BladeDict + ") " + (_bladeSet == 1 ? "is there" : "is MISSING, so the knife goes straight to the fall") +
                      "; the writhe (" + WritheDict + ") " + (_writheSet == 1 ? "is there" : "is MISSING, so they lie still"));
        }

        private static string DespairHow(WakeAct act)
        {
            return act.Method == "wall" ? "running at a wall" : act.Method == "knife" ? "a knife" : "a gun, " + (act.Weapon == null ? "their own" : Despair.Words(act.Weapon));
        }

        /// The same in a record's history, in his or her own words ("with a gun of his own").
        private static string DespairHistory(WakeAct act)
        {
            string their = act.P != null && act.P.Woman ? "her" : "his";
            return act.Method == "wall" ? "running headlong at a wall" : act.Method == "knife" ? "with a knife" : "with " + (act.Weapon == null ? "a gun of " + their + " own" : Despair.Words(act.Weapon));
        }

        /// The death booked once, however it came (the shot can kill before the last step).
        private void DespairDied(WakeAct act)
        {
            if (act.Done || act.Ending != "despair")
                return;
            act.Done = true;
            if (act.Res != null)
            {
                act.Res.Dead = true;
                _engine.KeepResident(act.Res, Bridge.Now());
            }
            NemesisRecord rec = act.Rec ?? _bridge.RecordAt(act.Handle);
            LedgerNote(act.Handle, act.P.Id, act.Name, act.Res != null ? "resident" : rec != null ? "record" : "folk", DespairHow(act), "self");   // known dead: no knocked-out test
            if (rec != null)
            {
                string town = ENTITY.DOES_ENTITY_EXIST(act.Handle) ? _bridge.TownAt(new Ped(act.Handle).Position) : null;
                QueueRecordFate(rec, act.Handle, act.Name, "Died by " + (act.P.Woman ? "her" : "his") + " own hand, " + DespairHistory(act) + ((town ?? _placeTown) != null ? ", in " + (town ?? _placeTown) : string.Empty) + ".");
            }
            string how = act.Method ?? "gun";
            int c;
            _despairCount.TryGetValue(how, out c);
            _despairCount[how] = c + 1;
            _despairLast = act.Name + " (" + DespairHow(act) + ")";
            Log.Write("WAKE despair: " + act.Name + (act.Res != null ? " (a resident)" : act.P != null && act.P.Id != null && act.P.Id.StartsWith("folk:") ? " (one of enRAGE's townsfolk)" : " (one of enRAGE's people)") + " could not bear it and ended their own life (" + DespairHow(act) + ")");
        }

        private sealed class AwakeNear
        {
            public int Handle;
            public string Name, Way, Mark;
            public float Dist;
            public bool May;
        }

        private readonly List<AwakeNear> _awakeNear = new List<AwakeNear>();   // the last F7's, for MarkAwake

        /// F7: the setting, the blade set, this session's by way, and the awake within 30 m with which way they are and
        /// what would happen.
        private string DespairStatus()
        {
            DespairSets();
            Ped me = Game.Player.Character;
            var near = new List<string>();
            _awakeNear.Clear();
            foreach (Ped p in World.GetAllPeds().Where(x => x != null && x.Exists()).OrderBy(x => x.Position.DistanceTo(me.Position)))
            {
                if (p == null || !p.Exists() || p.Handle == me.Handle || ENTITY.IS_ENTITY_DEAD(p.Handle) || !PED.IS_PED_HUMAN(p.Handle))
                    continue;
                float dist = p.Position.DistanceTo(me.Position);
                Person who;
                PersonState st;
                Resident res;
                bool passive;
                if (dist > 30f || !WakeIdentity(p, false, out who, out st, out res, out passive) || st.WakeStage < Awakening.Awake)
                    continue;
                bool armed = WEAPON.IS_PED_ARMED(p.Handle, 4);
                Vector3 w, d;
                bool wall = DespairWall(p, me, out w, out d);
                string method = Despair.Method(who, armed, wall, who.Id);
                bool record = res == null && _bridge.RecordAt(p.Handle) != null, folk = res == null && !record;   // WakeIdentity knows records, townsfolk and residents
                bool may = (res == null || res.PostId == null) && !st.Has(DespairMark);
                string verdict = res != null && res.PostId != null ? "keeps a counter: never" : st.Has(DespairMark) ? "had their chance"
                               : (folk ? "townsfolk, " : record ? "enRAGE's, " : "") + (passive ? "can't be directed now; " : "") + "would use " + method + (wall ? " (wall " + Flat(w, p.Position).ToString("0") + " m)" : "");
                string way = Way(me.Position, p.Position);
                near.Add(who.Name + " (" + (int)dist + " m " + way + ", " + verdict + ")");
                _awakeNear.Add(new AwakeNear { Handle = p.Handle, Name = who.Name, Way = way, Dist = dist, May = may,
                                               Mark = may ? method : res != null && res.PostId != null ? "never (counter)" : "had their chance" });
            }
            return "AwakeningDespair " + _cfg.AwakeningDespair + "% (one roll each); blade reel " + (_bladeSet == 1 ? "there" : "MISSING") + ", writhe " + (_writheSet == 1 ? "there" : "MISSING") + "; this session " +
                   (_despairCount.Count == 0 ? "none" : string.Join(", ", _despairCount.Select(kv => kv.Key + " " + kv.Value))) + (_despairLast != null ? ", last " + _despairLast : "") +
                   "; awake within 30 m: " + (near.Count == 0 ? "none" : string.Join("; ", near.Take(8)));
        }

        /// F7: a white mark on the map on each fully awake person within 30 m for a minute, labelled with their name and
        /// what they'd do ("Esther Lowry: knife", "Mart Cannon: had their chance"), and the nearest who could end it
        /// named on screen with which way to look. A test aid, so they can be found.
        private void MarkAwake()
        {
            if (_awakeNear.Count == 0)
                return;
            int until = Game.GameTime + 60000;
            foreach (AwakeNear a in _awakeNear.Take(10))
            {
                if (!ENTITY.DOES_ENTITY_EXIST(a.Handle))
                    continue;
                Blip b = new Ped(a.Handle).AddBlip(BlipType.WhiteDot);
                b.Label = a.Name + ": " + a.Mark;
                _regBlips.Add(new KeyValuePair<Blip, int>(b, until));   // taken off with the regulars' marks
            }
            AwakeNear first = _awakeNear.FirstOrDefault(a => a.May);
            Hint(first != null ? "Awake, and could end it: " + first.Name + " (" + first.Mark + "), " + (int)first.Dist + " m " + first.Way + ". " + _awakeNear.Count + " awake shown on your map for a minute."
                               : "Awake near you, none who could end it: " + _awakeNear.Count + " shown on your map for a minute.", 9000);
            Log.Write("WAKE marked " + _awakeNear.Count + " awake on the map" + (first != null ? "; nearest who could: " + first.Name + ", " + (int)first.Dist + " m " + first.Way : ""));
        }

        /// Which way someone is from where the camera looks ("ahead", "behind you to the left"), and "above you" or
        /// "below you" when they're a floor away.
        private static string Way(Vector3 from, Vector3 to)
        {
            double bearing = Math.Atan2(to.X - from.X, to.Y - from.Y) * 180.0 / Math.PI;   // clockwise from north
            double rel = ((bearing + CAM.GET_GAMEPLAY_CAM_ROT(2).Z) % 360.0 + 360.0) % 360.0;   // headings turn the other way
            string[] names = { "ahead", "ahead to your right", "to your right", "behind you to the right", "behind you", "behind you to the left", "to your left", "ahead to your left" };
            float dz = to.Z - from.Z;
            return names[(int)Math.Round(rel / 45.0) % 8] + (dz > 2.5f ? ", above you" : dz < -2.5f ? ", below you" : "");
        }

        // ---- shared ----------------------------------------------------------------------------------------------

        /// Why an act has to stop now, or null.
        private string WhyEnded(WakeAct act, Ped me, int now)
        {
            if (!ENTITY.DOES_ENTITY_EXIST(act.Handle)) return "gone";
            if (ENTITY.IS_ENTITY_DEAD(act.Handle))
            {
                if (act.Ending != "despair" || act.Phase < 4)
                    return "died";
                if (PED.IS_PED_INCAPACITATED(act.Handle))
                    ENTITY.SET_ENTITY_HEALTH(act.Handle, 0, act.Handle);   // the act's own end: only out cold would let them up again
                return "ended";
            }
            if (act.Phase >= 4 && act.Ending == "despair") return now >= act.Until ? "it didn't happen" : null;   // the last moments play out, to a deadline
            if (now >= act.Until) return act.Kind == "awake" && act.Phase == 0 ? "never reached you" : "finished";
            if (PED.IS_PED_IN_COMBAT(act.Handle, 0)) return "got into a fight";
            if (PED.IS_PED_RAGDOLL(act.Handle) && act.Kind != "run") return "was knocked down";
            if (PLAYER.IS_PLAYER_FREE_AIMING_AT_ENTITY(Game.Player.Handle, act.Handle)) return "you aimed at them";
            if (new Ped(act.Handle).Position.DistanceTo(me.Position) > (act.Kind == "awake" ? 80f : 60f)) return "too far away";
            return null;
        }

        private readonly Dictionary<int, int> _disbeliefAt = new Dictionary<int, int>();
        private int _disbeliefSaid;
        private string _disbeliefLast;

        /// Their last words, voiced (Lex): one of ten of the game's own lines that fit (Speech.LastWords), the first tried
        /// picked for the person, then the rest in turn until their voice has one. The written "last" line is the subtitle
        /// when the recording's own words aren't known.
        private void DespairLastWords(WakeAct act, Ped me)
        {
            string line = WakeBeats.Aloud("last", act.Stage, act.P, act.St, _engine.Player, act.P.Id + "|last");
            int start = (int)((uint)Voice.Hash(act.P.Id + "|lastwords") % (uint)Speech.LastWords.Length);
            string[] order = Speech.LastWords.Skip(start).Concat(Speech.LastWords.Take(start)).ToArray();
            if (string.IsNullOrEmpty(line))
                Speech.SayAny(act.Handle, order, me.Handle);
            else
                Feed.SayFirst(act.Name, line, Speech.WhenSet(act.Handle, order, me.Handle), act.Handle);
            Log.Write("WAKE despair: " + act.Name + "'s last words, voiced from " + order[0] + " first" + (line != null ? ": " + line : ""));
        }

        /// Someone fully awake at what can't be: the game's own disbelief line in their voice (a voice without it gets a
        /// shocked one), and their thought, in our words, over their head. At most once a minute each.
        private bool Disbelieve(int h, string name, bool woman, string seed, string why, bool sawDeath = false)
        {
            int now = Game.GameTime, at;
            if (_disbeliefAt.TryGetValue(h, out at) && now - at < 60000)
                return false;
            _disbeliefAt[h] = now;
            bool voiced = Speech.SayAny(h, Speech.Disbelief) || Speech.SayAny(h, Speech.DisbeliefElse);
            string thought = sawDeath ? WakeBeats.SawDeath(seed) : WakeBeats.Disbelief(seed);
            _ticker.Push(name + ", thinking", thought, "near", h);
            _disbeliefSaid++;
            _disbeliefLast = name + " (" + why + ")";
            Log.Write("WAKE disbelief: " + name + " " + why + (voiced ? "" : " (no voice line played)") + ": " + thought);
            return true;
        }

        /// A death near the fully awake: the nearest one awake who can see it says so (the ledger calls this).
        private void AwakeSawDeath(int body, Vector3 at)
        {
            if (_cfg == null || !_cfg.WestWorld || _store == null)
                return;
            Ped me = Game.Player.Character;
            foreach (Ped p in World.GetAllPeds().Where(x => x != null && x.Exists() && x.Handle != body && x.Handle != me.Handle && !ENTITY.IS_ENTITY_DEAD(x.Handle) &&
                                                           PED.IS_PED_HUMAN(x.Handle) && x.Position.DistanceTo(at) < 20f).OrderBy(x => x.Position.DistanceTo(at)))
            {
                Person who;
                PersonState st;
                Resident res;
                bool passive;
                if (!WakeIdentity(p, false, out who, out st, out res, out passive) || st.WakeStage < Awakening.Awake || !ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(p.Handle, body, 17))
                    continue;
                if (Disbelieve(p.Handle, who.Name, who.Woman, who.Id + "|sd|" + Game.GameTime / 60000, "saw someone die", true))
                    return;
            }
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
            Feed.SayFirst(act.Name, line, tag == null ? null : Speech.For(act.Handle, line, me.Handle, act.P.Hate, act.P.Fear, act.P.Friendly, tag), act.Handle);
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
            if (act.Given && !ENTITY.IS_ENTITY_DEAD(h))
                WEAPON.REMOVE_WEAPON_FROM_PED(h, Natives.Hash(act.Weapon), true, TakeReason);   // never left holding what we gave them
            else if (act.Ending == "despair" && act.Method == "gun" && act.Phase >= 4 && !ENTITY.IS_ENTITY_DEAD(h))
                WEAPON.SET_CURRENT_PED_WEAPON(h, Natives.Hash("WEAPON_UNARMED"), true, 0, false, false);   // stopped with their own gun out: put away
            if (!act.Passive && act.Kind != "glance" && act.Kind != "lookaround" && !ENTITY.IS_ENTITY_DEAD(h))
                TASK.CLEAR_PED_TASKS(h, true, false);
        }

        /// Gives everyone back, now (a mission started, or the add-on is stopping).
        private void WakeActsAbort()
        {
            foreach (WakeAct act in _wakeActs)
                EndWakeAct(act);
            _wakeActs.Clear();
            if (_bladeSet == 1)
                STREAMING.REMOVE_ANIM_DICT(BladeDict);   // the sets despair asked for go back too
            if (_writheSet == 1)
                STREAMING.REMOVE_ANIM_DICT(WritheDict);
        }
    }
}
