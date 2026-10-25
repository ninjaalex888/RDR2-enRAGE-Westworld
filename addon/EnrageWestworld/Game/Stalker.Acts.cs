// The eerie acts (StalkerWhenStill = eerie, or mix): he comes to about StalkerActMetres from you, faces you and does
// odd things from the game's own emotes and scenarios (Core/StalkerActs.cs), one after another with a stare between,
// moving round you now and then, until you move. A gesture may be done twice exactly alike, stop dead partway, or
// go slow. Two play on not being seen: he creeps closer while he's off your screen and stops dead when you look
// (creep), or he is standing somewhere else round you each time you look back (shift). Each act is checked: an animation that doesn't load or play, or a scenario that doesn't start, is tried
// once more and then dropped for the session, and the log says which.
using System;
using System.Collections.Generic;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    internal sealed partial class Stalker
    {
        private enum ActStep { Go, Pause, Load, Face, Play, Back, Creep, Shift }

        // acts that didn't play, kept apart for men and women (a man's animation may not play on a woman)
        private static readonly HashSet<string> BadForMen = new HashSet<string>(), BadForWomen = new HashSet<string>();
        private static readonly Dictionary<string, int> ActFails = new Dictionary<string, int>();
        private readonly HashSet<string> _dicts = new HashSet<string>();
        private readonly List<string> _recentActs = new List<string>();
        private ActStep _step;
        private EerieAct _act;
        private string _dict, _clip, _twist;
        private int _stepUntil, _checkAt, _actsSinceMove, _moveAfter, _repeats, _clipMs, _freezeAt, _unfreezeAt, _shifts, _nextShiftAt;
        private bool _checked, _frozen, _actWoman, _moving, _foundSinceShift;

        /// Creeping or shifting: he reacts to the camera within a frame or two, not a quarter second later.
        private bool QuickTicks { get { return _mode == Mode.Act && (_step == ActStep.Creep || _step == ActStep.Shift); } }
        private float _frozeAt;

        private void StartEerie(Ped player, int now)
        {
            _mode = Mode.Act;
            _act = null;
            _actsSinceMove = 0;
            _moveAfter = 3 + _rng.Next(2);
            _nextMutter = Math.Min(_nextMutter, now + 8000);
            int h = _ped.Handle;
            TASK.CLEAR_PED_TASKS(h, true, false);
            TASK.TASK_LOOK_AT_ENTITY(h, player.Handle, -1, 0, 51, 0);
            Vector3 me = _ped.Position, pp = player.Position;
            GoToActSpot(player, now, Math.Atan2(me.Y - pp.Y, me.X - pp.X));
            Log.Write("STALKER " + Label + " comes to about " + _cfg.StalkerActMetres + " m and starts doing odd things (" + (int)me.DistanceTo(pp) + " m away)");
        }

        /// The player moved: whatever he was doing stops at once (a scenario ends normally, so its prop isn't left hanging).
        private void EndEerie(Ped player, int now)
        {
            int h = _ped.Handle;
            if (PED.IS_PED_USING_ANY_SCENARIO(h))
                TASK.CLEAR_PED_TASKS(h, true, false);
            else
                TASK.CLEAR_PED_TASKS_IMMEDIATELY(h, true, true);
            _frozen = false;
            if (_step == ActStep.Creep)
                PED.SET_PED_STEALTH_MOVEMENT(h, false, 0, 0);
            StartRetreat(player, now);
        }

        private void EerieStep(Ped player, int now, bool seen)
        {
            int h = _ped.Handle;
            Vector3 me = _ped.Position, pp = player.Position;
            switch (_step)
            {
                case ActStep.Go:
                    if (me.DistanceTo(_goal) < 1.5f || now > _stepUntil)
                    {
                        Pause(player, now, 1500 + _rng.Next(2000));
                        break;
                    }
                    if (now >= _nextOrder)
                    {
                        // runs while far off (the player won't stand still for a long walk), walks the last stretch
                        _nextOrder = now + 1500;
                        float speed = me.DistanceTo(_goal) > 15f ? 2.2f : 1f;
                        TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(h, _goal.X, _goal.Y, _goal.Z, speed, -1, 0.5f, 0, 0f);
                    }
                    break;

                case ActStep.Pause:
                    if (now < _stepUntil)
                        break;
                    if (_actsSinceMove >= _moveAfter)
                    {
                        // somewhere else round you, then on with it
                        _actsSinceMove = 0;
                        _moveAfter = 3 + _rng.Next(2);
                        double at = Math.Atan2(me.Y - pp.Y, me.X - pp.X) + (_rng.Next(2) == 0 ? 1 : -1) * (50 + _rng.Next(50)) * Math.PI / 180.0;
                        GoToActSpot(player, now, at);
                        Log.Detail("STALKER " + Label + " moves to another spot round you");
                        break;
                    }
                    NextAct(player, now);
                    break;

                case ActStep.Load:
                    if (STREAMING.HAS_ANIM_DICT_LOADED(_dict))
                    {
                        float secs = ENTITY.GET_ANIM_DURATION(_dict, _clip);
                        if (secs <= 0f)
                        {
                            Failed(player, now, "has no clip " + _clip, false);
                            break;
                        }
                        _clipMs = (int)(secs > 120f ? secs : secs * 1000f);   // seconds, or already milliseconds
                        Face(player, now);
                    }
                    else if (now > _stepUntil)
                        Failed(player, now, "didn't load", false);
                    break;

                case ActStep.Face:
                    if (now > _stepUntil || Math.Abs(Turn(ENTITY.GET_ENTITY_HEADING(h), HeadingTo(me, pp))) < 20f)
                    {
                        if (_act.Scenario != null)
                            PlayScenario(player, now);
                        else
                            PlayEmote(player, now);
                    }
                    break;

                case ActStep.Play:
                    if (_act.Scenario != null)
                    {
                        if (!_checked && now >= _checkAt)
                        {
                            _checked = true;
                            if (!PED.IS_PED_USING_ANY_SCENARIO(h))
                            {
                                Failed(player, now, "didn't start", true);
                                break;
                            }
                            Played();
                        }
                        if (now >= _stepUntil)
                        {
                            TASK.CLEAR_PED_TASKS(h, true, false);
                            Pause(player, now, 2500 + _rng.Next(2500));
                        }
                        break;
                    }
                    bool playing = ENTITY.IS_ENTITY_PLAYING_ANIM(h, _dict, _clip, 3);
                    if (!_checked && now >= _checkAt)
                    {
                        _checked = true;
                        if (!playing)
                        {
                            Failed(player, now, "didn't play", true);
                            break;
                        }
                        Played();
                        if (_twist == "slow")
                            ENTITY._SET_ENTITY_ANIM_SPEED(h, _dict, _clip, 0.4f);
                    }
                    if (_freezeAt > 0 && now >= _freezeAt && playing)
                    {
                        _freezeAt = 0;
                        _frozen = true;
                        _frozeAt = ENTITY._GET_ENTITY_ANIM_CURRENT_TIME(h, _dict, _clip);
                        ENTITY._SET_ENTITY_ANIM_SPEED(h, _dict, _clip, 0f);
                    }
                    if (_frozen && now >= _unfreezeAt)
                    {
                        _frozen = false;
                        float t = ENTITY._GET_ENTITY_ANIM_CURRENT_TIME(h, _dict, _clip);
                        Log.Write("STALKER freeze: animation at " + _frozeAt.ToString("0.00") + ", " + t.ToString("0.00") + " when let go" +
                                  (Math.Abs(t - _frozeAt) < 0.03f ? " (it held still)" : " (it kept moving: freezing doesn't work this way)"));
                        ENTITY._SET_ENTITY_ANIM_SPEED(h, _dict, _clip, 1f);
                    }
                    if (_checked && !_frozen && (!playing || now >= _stepUntil))
                    {
                        if (_repeats > 0)
                        {
                            _repeats--;   // the same gesture again, exactly
                            StartAnim(player, now);
                            _checked = false;
                        }
                        else
                            Pause(player, now, 1500 + _rng.Next(3000));
                    }
                    break;

                case ActStep.Back:
                    if (!_checked && now >= _checkAt)
                    {
                        _checked = true;   // looks back over the shoulder at you
                        TASK.TASK_LOOK_AT_ENTITY(h, player.Handle, -1, 0, 51, 0);
                    }
                    if (now >= _stepUntil)
                        Pause(player, now, 1000 + _rng.Next(1500));
                    break;

                case ActStep.Creep:
                    if (!_checked && now >= _checkAt)
                    {
                        _checked = true;
                        Log.Write("STALKER creeping " + (PED.GET_PED_STEALTH_MOVEMENT(h) ? "crouched" : "upright (the crouch didn't take)"));
                    }
                    float dist = me.DistanceTo(pp);
                    if (dist <= 4.5f || now > _stepUntil)
                    {
                        if (_moving)
                            TASK.CLEAR_PED_TASKS_IMMEDIATELY(h, true, false);
                        PED.SET_PED_STEALTH_MOVEMENT(h, false, 0, 0);
                        _moving = false;
                        Log.Write("STALKER " + Label + (dist <= 4.5f ? " has crept up to " + (int)dist + " m from you" : " stops creeping, " + (int)dist + " m away"));
                        Pause(player, now, 4000 + _rng.Next(3000));
                        break;
                    }
                    if (seen)
                    {
                        if (_moving)
                        {
                            // caught moving: stops dead, mid-step
                            _moving = false;
                            TASK.CLEAR_PED_TASKS_IMMEDIATELY(h, true, false);
                            TASK.TASK_STAND_STILL(h, 20000);
                            TASK.TASK_LOOK_AT_ENTITY(h, player.Handle, -1, 0, 51, 0);
                        }
                    }
                    else if (!_moving || now >= _nextOrder)
                    {
                        _moving = true;
                        _nextOrder = now + 1000;
                        Vector3 near = pp + (me - pp).Normalized * 4f;
                        TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(h, near.X, near.Y, near.Z, 1f, -1, 0.5f, 0, 0f);
                    }
                    break;

                case ActStep.Shift:
                    if (seen && !_foundSinceShift)
                    {
                        _foundSinceShift = true;   // you found him: he stares a moment before he goes again
                        _nextShiftAt = now + 2000 + _rng.Next(2500);
                    }
                    if (now > _stepUntil || (_shifts == 0 && _foundSinceShift && now >= _nextShiftAt))
                    {
                        Pause(player, now, 2500 + _rng.Next(2500));
                        break;
                    }
                    if (_shifts > 0 && !seen && _foundSinceShift && now >= _nextShiftAt)
                    {
                        _nextShiftAt = now + 1000;
                        Vector3 at;
                        if (ShiftSpot(player, out at))
                        {
                            double turned = Math.Abs(Turn(HeadingTo(pp, me), HeadingTo(pp, at)));
                            TASK.CLEAR_PED_TASKS_IMMEDIATELY(h, true, true);
                            ENTITY.SET_ENTITY_COORDS(h, at.X, at.Y, at.Z, false, false, false, false);
                            if (ENTITY.IS_ENTITY_IN_WATER(h))
                            {
                                ENTITY.SET_ENTITY_COORDS(h, me.X, me.Y, me.Z, false, false, false, false);
                                break;
                            }
                            ENTITY.SET_ENTITY_HEADING(h, HeadingTo(at, pp));
                            TASK.TASK_STAND_STILL(h, 40000);
                            TASK.TASK_LOOK_AT_ENTITY(h, player.Handle, -1, 0, 51, 0);
                            _shifts--;
                            _foundSinceShift = false;
                            Log.Write("STALKER " + Label + " is somewhere else when you look back: " + (int)at.DistanceTo(pp) + " m from you, " + (int)turned + " degrees round");
                        }
                    }
                    break;
            }
        }

        private void StartCreep(Ped player, int now)
        {
            int h = _ped.Handle;
            TASK.CLEAR_PED_TASKS(h, true, false);
            TASK.TASK_LOOK_AT_ENTITY(h, player.Handle, -1, 0, 51, 0);
            PED.SET_PED_STEALTH_MOVEMENT(h, true, 0, 0);   // crouched, if the game takes it (the log says)
            _step = ActStep.Creep;
            _stepUntil = now + 30000 + _rng.Next(15000);
            _moving = false;
            _nextOrder = 0;
            _checkAt = now + 1500;
            _checked = false;
            Log.Write("STALKER " + Label + " " + _act.Does + " (" + (int)_ped.Position.DistanceTo(player.Position) + " m away)");
        }

        private void StartShift(Ped player, int now)
        {
            int h = _ped.Handle;
            TASK.TASK_STAND_STILL(h, 40000);
            TASK.TASK_LOOK_AT_ENTITY(h, player.Handle, -1, 0, 51, 0);
            _step = ActStep.Shift;
            _shifts = 1 + _rng.Next(3);
            _foundSinceShift = true;   // he's standing in plain view: the first look away is his chance
            _nextShiftAt = now + 800;
            _stepUntil = now + 40000;
            Log.Write("STALKER " + Label + " " + _act.Does + " (up to " + _shifts + " times)");
        }

        /// A spot round the player at about the act distance, well round from where he is, that neither the camera
        /// nor the player can see right now: he can be put there without anyone seeing it happen.
        private bool ShiftSpot(Ped player, out Vector3 at)
        {
            Vector3 pp = player.Position, me = _ped.Position;
            Vector3 cam = CAM.GET_FINAL_RENDERED_CAM_COORD();
            double from = Math.Atan2(me.Y - pp.Y, me.X - pp.X);
            for (int i = 0; i < 10; i++)
            {
                double a = from + (_rng.Next(2) == 0 ? 1 : -1) * (60 + _rng.Next(121)) * Math.PI / 180.0;
                float r = Math.Max(5f, _cfg.StalkerActMetres - 3f + (float)_rng.NextDouble() * 6f);
                Vector3 c = pp + new Vector3((float)Math.Cos(a) * r, (float)Math.Sin(a) * r, 0f);
                Vector3 safe;
                if (!Natives.StandSpot(c, pp.Z, out safe))
                    continue;
                float sx, sy;
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

        private void GoToActSpot(Ped player, int now, double angle)
        {
            Vector3 pp = player.Position;
            float r = Math.Max(4f, _cfg.StalkerActMetres - 2f + (float)_rng.NextDouble() * 4f);
            Vector3 goal = pp + new Vector3((float)Math.Cos(angle) * r, (float)Math.Sin(angle) * r, 0f);
            Vector3 safe;
            if (Natives.StandSpot(goal, pp.Z, out safe))
                goal = safe;
            _goal = goal;
            _step = ActStep.Go;
            _stepUntil = now + 30000;
            _nextOrder = 0;
        }

        /// Faces you and stares for a moment.
        private void Pause(Ped player, int now, int ms)
        {
            int h = _ped.Handle;
            _step = ActStep.Pause;
            _stepUntil = now + ms;
            TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, player.Handle, ms, 0f, 0f, 0f);
            TASK.TASK_LOOK_AT_ENTITY(h, player.Handle, -1, 0, 51, 0);
        }

        private void Face(Ped player, int now)
        {
            _step = ActStep.Face;
            _stepUntil = now + 2500;
            TASK.TASK_TURN_PED_TO_FACE_ENTITY(_ped.Handle, player.Handle, 2500, 0f, 0f, 0f);
        }

        private void NextAct(Ped player, int now)
        {
            int h = _ped.Handle;
            bool woman = !PED.IS_PED_MALE(h);
            _actWoman = woman;
            _act = StalkerActs.Choose(woman, Natives.Night(), _recentActs, woman ? BadForWomen : BadForMen, _rng);
            if (_act == null)
            {
                Pause(player, now, 6000);   // nothing left that plays: he just stares
                return;
            }
            _recentActs.Add(_act.Key);
            if (_recentActs.Count > 6)
                _recentActs.RemoveAt(0);
            _actsSinceMove++;
            if (_act.Emote != null)
            {
                _dict = _act.Dict(woman);
                _clip = _act.Clips[_rng.Next(_act.Clips.Length)];
                _twist = StalkerActs.Twist(_rng);
                if (!STREAMING.DOES_ANIM_DICT_EXIST(_dict))
                {
                    Failed(player, now, "isn't in the game (" + _dict + ")", false);
                    return;
                }
                STREAMING.REQUEST_ANIM_DICT(_dict);
                _dicts.Add(_dict);
                _step = ActStep.Load;
                _stepUntil = now + 3000;
            }
            else if (_act.Scenario != null)
                Face(player, now);
            else if (_act.Special == "creep")
                StartCreep(player, now);
            else if (_act.Special == "shift")
                StartShift(player, now);
            else
            {
                // turns his back on you, and later looks over his shoulder
                TASK.CLEAR_PED_TASKS(h, true, false);
                TASK.TASK_CLEAR_LOOK_AT(h);
                TASK.TASK_ACHIEVE_HEADING(h, HeadingTo(_ped.Position, player.Position) + 180f, 3000);
                _step = ActStep.Back;
                _stepUntil = now + 8000 + _rng.Next(6000);
                _checkAt = now + 3500 + _rng.Next(3000);
                _checked = false;
                Log.Write("STALKER " + Label + " " + _act.Does);
            }
        }

        private void PlayScenario(Ped player, int now)
        {
            int h = _ped.Handle;
            TASK.CLEAR_PED_TASKS(h, true, false);
            TASK.TASK_START_SCENARIO_IN_PLACE_HASH(h, Natives.Hash(_act.Scenario), _act.Ms, true, 0u, -1f, false);
            TASK.TASK_LOOK_AT_ENTITY(h, player.Handle, -1, 0, 51, 0);
            _step = ActStep.Play;
            _checkAt = now + 2000;
            _checked = false;
            _stepUntil = now + _act.Ms + 1500;
            Log.Write("STALKER " + Label + " " + _act.Does + " (" + _act.Scenario + ")");
        }

        private void PlayEmote(Ped player, int now)
        {
            _repeats = _twist == "repeat" ? 1 + _rng.Next(2) : 0;
            StartAnim(player, now);
            _checked = false;
            int freezeMs = 2500 + _rng.Next(3500);
            _freezeAt = _twist == "freeze" ? now + (int)(_clipMs * (0.3 + _rng.NextDouble() * 0.35)) : 0;
            _unfreezeAt = _freezeAt + freezeMs;
            if (_twist == "freeze")
                _stepUntil += freezeMs;
            string how = _twist == "repeat" ? (_repeats == 1 ? ", twice, exactly alike" : ", three times, exactly alike")
                       : _twist == "freeze" ? ", stopping dead partway" : _twist == "slow" ? ", slowly" : "";
            Log.Write("STALKER " + Label + " " + _act.Does + how + " (" + _dict + " / " + _clip + ")");
        }

        private void StartAnim(Ped player, int now)
        {
            int h = _ped.Handle;
            TASK.TASK_PLAY_ANIM(h, _dict, _clip, 4f, -4f, -1, 0, 0f, false, 0, false, "", false);
            TASK.TASK_LOOK_AT_ENTITY(h, player.Handle, -1, 0, 51, 0);
            _step = ActStep.Play;
            _checkAt = now + 600;
            _frozen = false;
            _stepUntil = now + (int)(_clipMs / (_twist == "slow" ? 0.4f : 1f)) + 1500;
        }

        private void Played()
        {
            ActFails.Remove(FailKey);
        }

        /// It didn't work: tried once more on a later turn, then dropped for the session (at once if the game hasn't got it).
        private void Failed(Ped player, int now, string why, bool retry)
        {
            int n;
            ActFails.TryGetValue(FailKey, out n);
            n = retry ? n + 1 : 2;
            ActFails[FailKey] = n;
            if (n >= 2)
            {
                (_actWoman ? BadForWomen : BadForMen).Add(_act.Key);
                Log.Write("STALKER act '" + _act.Key + "' " + why + ": dropped for " + (_actWoman ? "women" : "men") + " for this session");
            }
            else
                Log.Write("STALKER act '" + _act.Key + "' " + why + " (tried again later)");
            if (_act.Scenario != null)
                TASK.CLEAR_PED_TASKS(_ped.Handle, true, false);
            Pause(player, now, 800);
        }

        private string FailKey { get { return _act.Key + (_actWoman ? "/f" : "/m"); } }

        private void ReleaseAnims()
        {
            foreach (string d in _dicts)
                STREAMING.REMOVE_ANIM_DICT(d);
            _dicts.Clear();
        }

        /// The game's heading (degrees, 0 = north, turning left) from a to b.
        private static float HeadingTo(Vector3 a, Vector3 b)
        {
            float deg = (float)(Math.Atan2(-(b.X - a.X), b.Y - a.Y) * 180.0 / Math.PI);
            return deg < 0f ? deg + 360f : deg;
        }

        /// The shortest turn from one heading to another, -180..180.
        private static float Turn(float from, float to)
        {
            float d = (to - from) % 360f;
            if (d > 180f) d -= 360f;
            if (d < -180f) d += 360f;
            return d;
        }
    }
}
