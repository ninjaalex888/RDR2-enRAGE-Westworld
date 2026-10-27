// Someone following the player at a distance: a person already near you (Adopt), or a stranger put behind you
// (TrySpawn). Never attacks. Stands and stares when he thinks he's unseen,
// freezes when looked at, then slips out of sight; turns up behind you again when you've ridden off.
// Stand still a while (StalkerStillSeconds) and he does what StalkerWhenStill says: circles you close, keeps his
// distance and stares, runs straight at you and stops a few steps short, or does odd things (Stalker.Acts.cs); or a
// different one each time (mix). Take a step and he runs back out
// (StalkerHideMin-Max), behind a tree or a corner if there is one. Never attacks; runs for good when threatened.
// Every distance and time comes from the [Stalkers] settings, read live.
using System;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    internal sealed partial class Stalker
    {
        private enum Mode { Follow, Stare, Watched, Hide, Circle, Retreat, Lurk, Still, Rush, Act, Flee }

        private const uint MoveLR = 1301263553u, MoveUD = 4255658384u, HorseMoveLR = 308778731u, HorseMoveUD = 1002303471u;

        private readonly Settings _cfg;   // the live settings: a change in the ini or the panel applies at once
        private readonly Random _rng = new Random();
        private Ped _ped;
        private Mode _mode = Mode.Follow;
        private int _endsAt, _nextThink, _nextMutter, _modeUntil, _nextRelocate, _nextOrder, _fleeSince;
        private int _startHealth;
        private bool _spoke;
        private int _stillSince;
        private Vector3 _stillAt, _goal;
        private float _circleRadius;
        private int _circleDir;
        private int _rushSince;
        private int _nextTry, _failModel, _failNoGround, _failInView, _failCreate, _failWater;

        /// Further than this and he's lost you: he turns up behind you again when nobody is looking.
        private float FarAway { get { return Math.Max(85f, _cfg.StalkerAppearMax + 15f); } }

        public readonly Fallen Revenant;     // null for a nameless stranger
        public readonly string Label;
        public bool Woman { get; private set; }
        public bool Done { get; private set; }
        public int Handle { get { return _ped != null ? _ped.Handle : 0; } }

        public Stalker(Fallen revenant, string label, bool woman, Settings cfg)
        {
            Revenant = revenant;
            Label = label;
            Woman = woman;
            _cfg = cfg;
            _endsAt = Game.GameTime + cfg.StalkerMinutes * 60000;
        }

        /// Why the attempts so far failed, for the log.
        public string Why
        {
            get
            {
                return "model not loaded " + _failModel + ", no walkable ground behind you " + _failNoGround + ", every spot in view " + _failInView +
                       ", game refused the spawn " + _failCreate + ", landed in water " + _failWater +
                       " (plain ground spots used so far: " + Natives.GroundFallbacks + ")";
            }
        }

        /// Tries to put him somewhere behind the player, out of sight. False until it works (model streaming etc.).
        public bool TrySpawn(uint model, Ped player)
        {
            int now = Game.GameTime;
            if (now < _nextTry)
                return false;
            _nextTry = now + 250;
            if (!Spawner.Ready(model))
            {
                _failModel++;
                return false;
            }
            Vector3 at;
            if (!HiddenSpotBehind(player, _cfg.StalkerAppearMin, _cfg.StalkerAppearMax, out at))
                return false;
            Ped p = Spawner.Spawn(model, at, 0f);
            if (p == null)
            {
                _failCreate++;
                return false;
            }
            _ped = p;
            int h = p.Handle;
            if (ENTITY.IS_ENTITY_IN_WATER(h))
            {
                _failWater++;
                Spawner.Release(p, true);
                _ped = null;
                return false;
            }
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, true);
            PED.SET_PED_KEEP_TASK(h, true);
            _startHealth = ENTITY.GET_ENTITY_HEALTH(h);
            _nextMutter = Game.GameTime + 25000 + _rng.Next(20000);
            _nextRelocate = Game.GameTime + 20000;
            _stillSince = Game.GameTime;
            _stillAt = player.Position;
            Log.Write("STALKER " + Label + " appears " + (int)at.DistanceTo(player.Position) + " m behind you" + (Revenant != null ? " (someone you killed, returned)" : ""));
            return true;
        }

        /// Someone already near you becomes the stalker: they stop what they're doing and stare, then follow. They are
        /// handed back to the game afterwards, never deleted.
        public void Adopt(Ped p, Ped player, string why, bool light = false)
        {
            _ped = p;
            int h = p.Handle;
            bool held = true;
            if (light)
                Spawner.BorrowLight(p);   // one of enRAGE's records (Lex's overnight brief): its own state left alone
            else
                held = Spawner.Borrow(p);
            TASK.CLEAR_PED_TASKS(h, true, false);
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, true);
            PED.SET_PED_KEEP_TASK(h, true);
            _startHealth = ENTITY.GET_ENTITY_HEALTH(h);
            int now = Game.GameTime;
            _nextMutter = now + 25000 + _rng.Next(20000);
            _nextRelocate = now + 20000;
            _stillSince = now;
            _stillAt = player.Position;
            _mode = Mode.Stare;
            _modeUntil = now + 6000 + _rng.Next(6000);
            Face(player, true);
            Log.Write("STALKER " + Label + " stops what they're doing and starts following you, " + (int)p.Position.DistanceTo(player.Position) + " m away (" + why + ")" +
                      (held ? "" : "; the game didn't let go of them, so they may wander off"));
        }

        public void Tick(Ped player, PlayerRef you, Action<string, string, bool> say)
        {
            if (Done)
                return;
            int now = Game.GameTime;
            if (_ped == null || !_ped.Exists() || _ped.IsDead)
            {
                Finish(false);
                return;
            }
            if (now < _nextThink)
                return;
            _nextThink = now + (QuickTicks ? 50 : 250);

            int h = _ped.Handle;
            Vector3 me = _ped.Position, pp = player.Position;
            float dist = me.DistanceTo(pp);
            bool onScreen = ENTITY.IS_ENTITY_ON_SCREEN(h);
            bool clear = ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(player.Handle, h, 17);
            bool watched = onScreen && clear && dist < 95f && Natives.Looking(me + new Vector3(0f, 0f, 1f), 28f);
            bool threatened = PLAYER.IS_PLAYER_FREE_AIMING_AT_ENTITY(Game.Player.Handle, h) || ENTITY.GET_ENTITY_HEALTH(h) < _startHealth;
            bool active = PlayerActive(player);
            if (active)
                _stillSince = now;
            bool close = _mode == Mode.Circle || _mode == Mode.Retreat || _mode == Mode.Rush || _mode == Mode.Act;   // he chose to be near: no "caught" flight

            if (_mode != Mode.Flee && (threatened || (dist < 7f && !close) || now > _endsAt))
            {
                if (dist < 9f && !_spoke && !threatened)
                {
                    _spoke = true;
                    say(Label, Revenant != null && Revenant.Returned == 0 ? Hosts.RevenantAloud(Revenant, you, Revenant.Id) : Hosts.StalkerCaught(Revenant != null, you, Label + now), Revenant != null);
                }
                StartFlee(player);
                return;
            }

            if (_mode != Mode.Flee && _mode != Mode.Circle && _mode != Mode.Rush && _mode != Mode.Still && _mode != Mode.Act && now - _stillSince >= _cfg.StalkerStillSeconds * 1000)
                StartStill(player, now);

            switch (_mode)
            {
                case Mode.Act:   // he wants to be seen: being watched changes nothing
                    if (active)
                        EndEerie(player, now);
                    else
                        EerieStep(player, now, onScreen && clear);
                    break;

                case Mode.Circle:
                    if (active)
                    {
                        StartRetreat(player, now);
                        break;
                    }
                    if (now >= _nextOrder)
                    {
                        _nextOrder = now + 1000;
                        CircleStep(player);
                    }
                    break;

                case Mode.Still:   // keeping his distance, staring, until you move
                    if (active)
                    {
                        _mode = Mode.Follow;
                        _nextOrder = 0;
                    }
                    else if (watched)
                        Watched(player, now);
                    break;

                case Mode.Rush:
                    if (active || now - _rushSince > 15000)
                    {
                        StartRetreat(player, now);
                        break;
                    }
                    if (_modeUntil == 0 && dist < 3.5f)
                    {
                        // stops a few steps short, face to face: he never lays a hand on you
                        TASK.CLEAR_PED_TASKS(h, true, false);
                        TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, player.Handle, 2000, 0f, 0f, 0f);
                        TASK.TASK_LOOK_AT_ENTITY(h, player.Handle, 7000, 0, 51, 0);
                        _modeUntil = now + 3500 + _rng.Next(2500);
                        _nextMutter = now;
                        Log.Write("STALKER " + Label + " stops a few steps short of you");
                    }
                    else if (_modeUntil > 0 && now > _modeUntil)
                        StartRetreat(player, now);
                    else if (_modeUntil == 0 && now >= _nextOrder)
                    {
                        _nextOrder = now + 700;
                        TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(h, pp.X, pp.Y, pp.Z, 3f, -1, 3f, 0, 0f);
                    }
                    break;

                case Mode.Retreat:
                    if (me.DistanceTo(_goal) < 3f || now > _modeUntil)
                    {
                        _mode = Mode.Lurk;
                        _modeUntil = now + 30000 + _rng.Next(30000);
                        Face(player, true);
                    }
                    break;

                case Mode.Lurk:
                    if (watched)
                    {
                        Watched(player, now);
                        break;
                    }
                    if (now > _modeUntil || dist > FarAway)
                    {
                        _mode = Mode.Follow;
                        _nextOrder = 0;
                    }
                    break;

                case Mode.Follow:
                    if (watched)
                    {
                        Watched(player, now);
                        break;
                    }
                    if (dist > FarAway && !onScreen && now >= _nextRelocate)
                    {
                        Relocate(player, now);
                        break;
                    }
                    if (dist <= _cfg.StalkerStareMetres)
                    {
                        _mode = Mode.Stare;
                        _modeUntil = now + 6000 + _rng.Next(8000);
                        Face(player, true);
                        break;
                    }
                    if (now >= _nextOrder)
                    {
                        _nextOrder = now + 2500;
                        Vector3 goal = pp + (me - pp).Normalized * _cfg.StalkerFollowMetres;
                        Vector3 safe;
                        if (Natives.StandSpot(goal, pp.Z, out safe))
                            goal = safe;
                        TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(h, goal.X, goal.Y, goal.Z, dist > 60f ? 2f : 1f, -1, 2f, 0, 0f);
                    }
                    break;

                case Mode.Stare:
                    if (watched)
                    {
                        Watched(player, now);
                        break;
                    }
                    if (dist > _cfg.StalkerStareMetres + 11 || now > _modeUntil)
                        _mode = Mode.Follow;
                    break;

                case Mode.Watched:
                    if (now > _modeUntil)
                        Hide(player, now);
                    break;

                case Mode.Hide:
                    if (now > _modeUntil || (!onScreen && dist > 30f))
                    {
                        _mode = Mode.Follow;
                        _nextOrder = 0;
                    }
                    break;

                case Mode.Flee:
                    if ((!onScreen && (dist > 30f || !clear)) || now - _fleeSince > 40000)
                    {
                        Finish(!onScreen);
                        return;
                    }
                    break;
            }

            if (_cfg.StalkerMutters && _mode != Mode.Flee && (!watched || _mode == Mode.Circle || _mode == Mode.Rush || _mode == Mode.Act) && dist < 50f && now >= _nextMutter)
            {
                _nextMutter = now + 40000 + _rng.Next(30000);
                say(Label, Hosts.StalkerMutter(Revenant, you, Revenant != null, Label + now / 1000), Revenant != null);
            }
        }

        private void Watched(Ped player, int now)
        {
            _mode = Mode.Watched;
            _modeUntil = now + 2200 + _rng.Next(2800);
            TASK.CLEAR_PED_TASKS(_ped.Handle, true, false);
            TASK.TASK_STAND_STILL(_ped.Handle, 6000);
            TASK.TASK_LOOK_AT_ENTITY(_ped.Handle, player.Handle, 6000, 0, 51, 0);
        }

        private void Face(Ped player, bool stare)
        {
            int h = _ped.Handle;
            TASK.CLEAR_PED_TASKS(h, true, false);
            TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, player.Handle, -1, 0f, 0f, 0f);
            TASK.TASK_LOOK_AT_ENTITY(h, player.Handle, -1, 0, 51, 0);
        }

        /// Walks to the nearest nearby spot the camera can't see.
        private void Hide(Ped player, int now)
        {
            _mode = Mode.Hide;
            _modeUntil = now + 9000;
            Vector3 me = _ped.Position;
            Vector3 cam = CAM.GET_FINAL_RENDERED_CAM_COORD();
            Vector3 best = Vector3.Zero;
            float bestD = float.MaxValue;
            for (int i = 0; i < 10; i++)
            {
                double a = i * Math.PI * 2 / 10 + _rng.NextDouble() * 0.3;
                float r = i % 2 == 0 ? 9f : 16f;
                Vector3 c = me + new Vector3((float)Math.Cos(a) * r, (float)Math.Sin(a) * r, 0f);
                Vector3 safe;
                if (!Natives.StandSpot(c, me.Z, out safe))
                    continue;
                if (!Natives.Blocked(cam, safe + new Vector3(0f, 0f, 1.2f), player.Handle))
                    continue;
                float d = safe.DistanceTo(me);
                if (d < bestD)
                {
                    bestD = d;
                    best = safe;
                }
            }
            TASK.CLEAR_PED_TASKS(_ped.Handle, true, false);
            if (bestD < float.MaxValue)
                TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(_ped.Handle, best.X, best.Y, best.Z, 1.6f, -1, 1f, 0, 0f);
            else
                TASK.TASK_WALK_AWAY(_ped.Handle, player.Handle);
        }

        /// Turns up behind the player again (only when nobody can see it happen).
        private void Relocate(Ped player, int now)
        {
            Vector3 at;
            _nextRelocate = now + 25000;
            if (!HiddenSpotBehind(player, _cfg.StalkerAppearMin * 0.85f, _cfg.StalkerAppearMax * 0.85f, out at))
                return;
            int h = _ped.Handle;
            TASK.CLEAR_PED_TASKS_IMMEDIATELY(h, true, true);
            ENTITY.SET_ENTITY_COORDS(h, at.X, at.Y, at.Z, false, false, false, false);
            _mode = Mode.Follow;
            _nextOrder = 0;
            Log.Detail("STALKER relocated behind you");
        }

        /// True when the player moved or steered since he was last seen still. The camera doesn't count.
        private bool PlayerActive(Ped player)
        {
            Vector3 pos = player.Position;
            float move = Math.Abs(PAD.GET_CONTROL_NORMAL(0, MoveLR)) + Math.Abs(PAD.GET_CONTROL_NORMAL(0, MoveUD)) +
                         Math.Abs(PAD.GET_CONTROL_NORMAL(0, HorseMoveLR)) + Math.Abs(PAD.GET_CONTROL_NORMAL(0, HorseMoveUD));
            if (pos.DistanceTo(_stillAt) <= 0.6f && move < 0.15f)
                return false;
            _stillAt = pos;
            return true;
        }

        /// The player has stood still long enough: whatever StalkerWhenStill says. The clock starts again after each.
        private void StartStill(Ped player, int now)
        {
            _stillSince = now;
            int dist = (int)_ped.Position.DistanceTo(player.Position);
            switch (_cfg.StalkerWhenStill == "mix" ? StalkerActs.Mix(_rng) : _cfg.StalkerWhenStill)
            {
                case "eerie":
                    StartEerie(player, now);
                    break;
                case "watch":
                    _mode = Mode.Still;
                    Face(player, true);
                    Log.Write("STALKER " + Label + " stops and watches you, " + dist + " m away");
                    break;
                case "rush":
                    _mode = Mode.Rush;
                    _modeUntil = 0;
                    _rushSince = now;
                    _nextOrder = 0;
                    TASK.CLEAR_PED_TASKS(_ped.Handle, true, false);
                    TASK.TASK_LOOK_AT_ENTITY(_ped.Handle, player.Handle, -1, 0, 51, 0);
                    Log.Write("STALKER " + Label + " runs straight at you (" + dist + " m away)");
                    break;
                default:
                    StartCircle(player, now);
                    break;
            }
        }

        /// The player has stood still long enough: run in close and start circling him.
        private void StartCircle(Ped player, int now)
        {
            _mode = Mode.Circle;
            _circleRadius = Math.Max(2.5f, _cfg.StalkerCircleMetres - 2f + (float)_rng.NextDouble() * 4f);
            _circleDir = _rng.Next(2) == 0 ? 1 : -1;
            _nextOrder = 0;
            _nextMutter = Math.Min(_nextMutter, now + 2500);
            TASK.CLEAR_PED_TASKS(_ped.Handle, true, false);
            TASK.TASK_LOOK_AT_ENTITY(_ped.Handle, player.Handle, -1, 0, 51, 0);
            Log.Write("STALKER " + Label + " closes in and circles you (" + (int)_ped.Position.DistanceTo(player.Position) + " m away)");
        }

        /// Runs to a point on the circle a little ahead of where he is now, so he keeps going round.
        private void CircleStep(Ped player)
        {
            Vector3 pp = player.Position, me = _ped.Position;
            double at = Math.Atan2(me.Y - pp.Y, me.X - pp.X) + _circleDir * 50.0 * Math.PI / 180.0;
            Vector3 goal = pp + new Vector3((float)Math.Cos(at) * _circleRadius, (float)Math.Sin(at) * _circleRadius, 0f);
            Vector3 safe;
            if (Natives.StandSpot(goal, pp.Z, out safe))
                goal = safe;
            TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(_ped.Handle, goal.X, goal.Y, goal.Z, 2.2f, -1, 0.5f, 0, 0f);
        }

        /// The player moved: run back out to 45-70 m, behind cover if there is any.
        private void StartRetreat(Ped player, int now)
        {
            bool hidden;
            Vector3 spot = CoverSpot(player, out hidden);
            _mode = Mode.Retreat;
            _modeUntil = now + 35000;
            _goal = spot;
            TASK.CLEAR_PED_TASKS(_ped.Handle, true, false);
            TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(_ped.Handle, spot.X, spot.Y, spot.Z, 3f, -1, 1f, 0, 0f);
            Log.Write("STALKER " + Label + " runs off " + (int)spot.DistanceTo(player.Position) + " m" + (hidden ? " to cover" : " (no cover found)"));
        }

        /// A spot 45-70 m from the player, mostly on the stalker's side of him. Best: hidden from the camera and
        /// the player with a clear view a few steps closer (just round a corner or behind a tree); then any hidden
        /// spot; then the first spot that can be stood on.
        private Vector3 CoverSpot(Ped player, out bool hidden)
        {
            Vector3 pp = player.Position, me = _ped.Position;
            Vector3 cam = CAM.GET_FINAL_RENDERED_CAM_COORD(), eyes = pp + new Vector3(0f, 0f, 1.6f), chest = new Vector3(0f, 0f, 1.2f);
            double away = Math.Atan2(me.Y - pp.Y, me.X - pp.X);
            Vector3 best = Vector3.Zero;
            int bestScore = -1;
            float bestD = float.MaxValue;
            for (int i = 0; i < 16; i++)
            {
                double a = away + (_rng.NextDouble() * 2 - 1) * Math.PI * 0.6;
                float d = _cfg.StalkerHideMin + (float)_rng.NextDouble() * (_cfg.StalkerHideMax - _cfg.StalkerHideMin);
                Vector3 c = pp + new Vector3((float)Math.Cos(a) * d, (float)Math.Sin(a) * d, 0f);
                Vector3 safe;
                if (!Natives.StandSpot(c, pp.Z, out safe))
                    continue;
                int score = 0;
                if (Natives.Blocked(cam, safe + chest, player.Handle) && Natives.Blocked(eyes, safe + chest, player.Handle))
                {
                    score = 1;
                    Vector3 peek = safe + (pp - safe).Normalized * 3f;
                    if (!Natives.Blocked(eyes, peek + chest, player.Handle))
                        score = 2;
                }
                float fromMe = safe.DistanceTo(me);
                if (score > bestScore || (score == bestScore && fromMe < bestD))
                {
                    bestScore = score;
                    bestD = fromMe;
                    best = safe;
                }
            }
            hidden = bestScore > 0;
            if (bestScore < 0)
                best = pp + (me - pp).Normalized * 55f;
            return best;
        }

        private void StartFlee(Ped player)
        {
            _mode = Mode.Flee;
            _fleeSince = Game.GameTime;
            int h = _ped.Handle;
            Spawner.StandUp(h);
            TASK.CLEAR_PED_TASKS(h, true, false);
            TASK.TASK_SMART_FLEE_PED(h, player.Handle, 120f, -1, 0, 3f, 0);
        }

        public void Finish(bool vanish)
        {
            if (Done)
                return;
            Done = true;
            if (_ped != null)
            {
                ReleaseAnims();
                bool seen = _ped.Exists() && ENTITY.IS_ENTITY_ON_SCREEN(_ped.Handle);
                Spawner.Release(_ped, vanish || !seen);
            }
            Log.Write("STALKER " + Label + " is gone");
        }

        private bool HiddenSpotBehind(Ped player, float min, float max, out Vector3 at)
        {
            Vector3 cam = CAM.GET_FINAL_RENDERED_CAM_COORD();
            for (int tries = 0; tries < 6; tries++)
            {
                float d = min + (float)_rng.NextDouble() * (max - min);
                Vector3 c = Natives.BehindCamera(player.Position, d, 70f, _rng);
                Vector3 safe;
                if (!Natives.StandSpot(c, player.Position.Z, out safe))
                {
                    _failNoGround++;
                    continue;
                }
                float sx = 0, sy = 0;
                bool visible = ScreenPoint(safe + new Vector3(0f, 0f, 1f), out sx, out sy) && sx > -0.05f && sx < 1.05f && sy > -0.05f && sy < 1.05f &&
                               !Natives.Blocked(cam, safe + new Vector3(0f, 0f, 1.2f), player.Handle);
                if (visible)
                {
                    _failInView++;
                    continue;
                }
                at = safe;
                return true;
            }
            at = Vector3.Zero;
            return false;
        }

        private static unsafe bool ScreenPoint(Vector3 w, out float x, out float y)
        {
            float sx = 0f, sy = 0f;
            bool ok = GRAPHICS.GET_SCREEN_COORD_FROM_WORLD_COORD(w.X, w.Y, w.Z, &sx, &sy);
            x = sx;
            y = sy;
            return ok;
        }
    }
}
