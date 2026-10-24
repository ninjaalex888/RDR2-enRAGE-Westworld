// Someone following the player at a distance. Never attacks. Stands and stares when he thinks he's unseen,
// freezes when looked at, then slips out of sight; turns up behind you again when you've ridden off;
// runs when approached or threatened, and vanishes as soon as nobody is looking.
using System;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    internal sealed class Stalker
    {
        private enum Mode { Follow, Stare, Watched, Hide, Flee }

        private readonly Random _rng = new Random();
        private Ped _ped;
        private Mode _mode = Mode.Follow;
        private int _endsAt, _nextThink, _nextMutter, _modeUntil, _nextRelocate, _nextOrder, _fleeSince;
        private int _startHealth;
        private bool _spoke;

        public readonly Fallen Revenant;     // null for a nameless stranger
        public readonly string Label;
        public bool Woman { get; private set; }
        public bool Done { get; private set; }
        public int Handle { get { return _ped != null ? _ped.Handle : 0; } }

        public Stalker(Fallen revenant, string label, bool woman, int minutes)
        {
            Revenant = revenant;
            Label = label;
            Woman = woman;
            _endsAt = Game.GameTime + minutes * 60000;
        }

        /// Tries to put him somewhere behind the player, out of sight. False until it works (model streaming etc.).
        public bool TrySpawn(uint model, Ped player)
        {
            if (!Spawner.Ready(model))
                return false;
            Vector3 at;
            if (!HiddenSpotBehind(player, 45f, 70f, out at))
                return false;
            Ped p = Spawner.Spawn(model, at, 0f);
            if (p == null)
                return false;
            _ped = p;
            int h = p.Handle;
            if (ENTITY.IS_ENTITY_IN_WATER(h))
            {
                Spawner.Release(p, true);
                _ped = null;
                return false;
            }
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, true);
            PED.SET_PED_KEEP_TASK(h, true);
            _startHealth = ENTITY.GET_ENTITY_HEALTH(h);
            _nextMutter = Game.GameTime + 25000 + _rng.Next(20000);
            _nextRelocate = Game.GameTime + 20000;
            Log.Write("STALKER " + Label + " appears " + (int)at.DistanceTo(player.Position) + " m behind you" + (Revenant != null ? " (back from the dead)" : ""));
            return true;
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
            _nextThink = now + 250;

            int h = _ped.Handle;
            Vector3 me = _ped.Position, pp = player.Position;
            float dist = me.DistanceTo(pp);
            bool onScreen = ENTITY.IS_ENTITY_ON_SCREEN(h);
            bool clear = ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(player.Handle, h, 17);
            bool watched = onScreen && clear && dist < 95f && Natives.Looking(me + new Vector3(0f, 0f, 1f), 28f);
            bool threatened = PLAYER.IS_PLAYER_FREE_AIMING_AT_ENTITY(Game.Player.Handle, h) || ENTITY.GET_ENTITY_HEALTH(h) < _startHealth;

            if (_mode != Mode.Flee && (threatened || dist < 7f || now > _endsAt))
            {
                if (dist < 9f && !_spoke && !threatened)
                {
                    _spoke = true;
                    say(Label, Revenant != null && Revenant.Returned == 0 ? Hosts.RevenantAloud(Revenant, you, Revenant.Id) : Hosts.StalkerCaught(Revenant != null, you, Label + now), Revenant != null);
                }
                StartFlee(player);
                return;
            }

            switch (_mode)
            {
                case Mode.Follow:
                    if (watched)
                    {
                        Watched(player, now);
                        break;
                    }
                    if (dist > 85f && !onScreen && now >= _nextRelocate)
                    {
                        Relocate(player, now);
                        break;
                    }
                    if (dist <= 34f)
                    {
                        _mode = Mode.Stare;
                        _modeUntil = now + 6000 + _rng.Next(8000);
                        Face(player, true);
                        break;
                    }
                    if (now >= _nextOrder)
                    {
                        _nextOrder = now + 2500;
                        Vector3 goal = pp + (me - pp).Normalized * 26f;
                        Vector3 safe;
                        if (Natives.SafeCoord(goal, out safe))
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
                    if (dist > 45f || now > _modeUntil)
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

            if (_mode != Mode.Flee && !watched && dist < 50f && now >= _nextMutter)
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
                if (!Natives.SafeCoord(c, out safe))
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
            if (!HiddenSpotBehind(player, 38f, 58f, out at))
                return;
            int h = _ped.Handle;
            TASK.CLEAR_PED_TASKS_IMMEDIATELY(h, true, true);
            ENTITY.SET_ENTITY_COORDS(h, at.X, at.Y, at.Z, false, false, false, false);
            _mode = Mode.Follow;
            _nextOrder = 0;
            Log.Detail("STALKER relocated behind you");
        }

        private void StartFlee(Ped player)
        {
            _mode = Mode.Flee;
            _fleeSince = Game.GameTime;
            int h = _ped.Handle;
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
                if (!Natives.SafeCoord(c, out safe))
                    continue;
                float sx = 0, sy = 0;
                bool visible = ScreenPoint(safe + new Vector3(0f, 0f, 1f), out sx, out sy) && sx > -0.05f && sx < 1.05f && sy > -0.05f && sy < 1.05f &&
                               !Natives.Blocked(cam, safe + new Vector3(0f, 0f, 1.2f), player.Handle);
                if (visible)
                    continue;
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
