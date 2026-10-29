// The Testing page's gunshot row (0.6.3; Lex, 2026-10-27: despair's shot made no sound, twice). Despair fires with
// SHOOT_SINGLE_BULLET_BETWEEN_COORDS from the gun hand into the head, and their death proves nothing, since health 0
// follows anyway. This row fires six kinds of shot at the ground a step in front of the nearest free person, numbered on
// screen a few seconds apart, so Lex can say which he heard, and logs what each did: their own gun's trigger
// (_SHOOT_TRIGGER_AT_COORDS, not named in the .NET API), a bullet from their hand as despair fires it, one from a metre
// beside them with the gun's set loaded, the same owned by nobody, the trigger again with the gun aimed first, and the
// game's shoot task. Nobody is hurt; the revolver is theirs only for the run.
// What Lex heard (2026-10-27, three runs on Dorsey Castle): 1, 5 and 6, never 2, 3 or 4. A bullet made in the air by
// SHOOT_SINGLE_BULLET_BETWEEN_COORDS is silent in RDR2 wherever it starts and whoever owns it; a gun fired by its own
// trigger or by the shoot task is heard. So despair's shot and the execution's last bullet pull the trigger first
// (GunTrigger). The game's own readings told nothing: the ammo never fell, "shot recently" was false for every shot
// heard and IS_PED_SHOOTING true only for the task, so the row logs only that and leaves the rest to the ear.
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
        private const ulong ShootTrigger = 0x4C57F27D1554E6B0;      // _SHOOT_TRIGGER_AT_COORDS(ped, x, y, z, p4, p5 = -1 in R*'s scripts, p6, p7): "triggers a gunshot" (the natives database)

        private static readonly string[] GunshotWays =
        {
            "their own gun's trigger",
            "a bullet from their hand (as despair fires it)",
            "a bullet from a metre beside them",
            "a bullet from a metre beside them, owned by nobody",
            "their own gun's trigger, aimed first",
            "the game's shoot task"
        };

        private sealed class GunshotRun
        {
            public int Handle, Idx, Phase, NextAt, FiredAt;
            public string Name;
            public bool GunGiven, Shooting;
            public Vector3 Spot;
            public readonly List<string> Results = new List<string>();
        }

        private GunshotRun _gunTest;
        private Action _aGunTest;

        private string GunshotTestStart(int now)
        {
            if (_gunTest != null)
                GunshotTestEnd("a new run was started");
            if (_animTest != null)
                return "an animation run is going on";
            Ped me = Game.Player.Character;
            Ped p = World.GetAllPeds().Where(x => x != null && x.Exists() && !x.IsDead && x.Handle != me.Handle && PED.IS_PED_HUMAN(x.Handle) && !Natives.StoryModel(x) &&
                                                  x.Position.DistanceTo(me.Position) < 12f && x.Position.DistanceTo(me.Position) >= 2f && IsGrownUp(x) &&
                                                  _bridge.RecordAt(x.Handle) == null && MayDirectWhyNot(x) == null && !NotFree(x.Handle))
                                     .OrderBy(x => x.Position.DistanceTo(me.Position)).FirstOrDefault();
            if (p == null)
                return "nobody free 2-12 m off (not one of enRAGE's people, not in a scene)";
            int h = p.Handle;
            Someone who = SomeoneOf(p);
            _gunTest = new GunshotRun { Handle = h, Name = who != null ? who.Name : Grammar.UpperFirst(Director.Kind(p, !PED.IS_PED_MALE(h))), NextAt = now + 1800 };
            if (ENTITY.IS_ENTITY_A_MISSION_ENTITY(h))
                Spawner.BorrowLight(p);
            else
                Spawner.Borrow(p);
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, true);   // the shots don't send them running
            TASK.CLEAR_PED_TASKS(h, true, false);
            // the spot: the ground a step and a half in front of them, on the side away from you
            Vector3 pos = p.Position, away = pos - me.Position;
            away.Z = 0f;
            if (away.Length() < 0.1f)
                away = new Vector3(0f, 1f, 0f);
            away.Normalize();
            _gunTest.Spot = pos + away * 1.5f + new Vector3(0f, 0f, -0.95f);
            TASK.TASK_TURN_PED_TO_FACE_COORD(h, _gunTest.Spot.X, _gunTest.Spot.Y, _gunTest.Spot.Z, 1500);
            WEAPON.GIVE_WEAPON_TO_PED(h, TestRevolver, 12, true, false, 0, false, 0.5f, 1f, GiveReason, true, 0f, false);   // forced into the hand
            WEAPON.SET_PED_DROPS_WEAPONS_WHEN_DEAD(h, false);
            WEAPON.SET_CURRENT_PED_WEAPON(h, TestRevolver, true, 0, false, false);
            WEAPON._REQUEST_WEAPON_ASSET(TestRevolver, 31, false);   // for the bullets fired from beside them
            _gunTest.GunGiven = true;
            Log.Write("GUNSHOT test: " + GunshotWays.Length + " ways on " + _gunTest.Name + " (" + (Models.Name(ENTITY.GET_ENTITY_MODEL(h)) ?? "?") + "), at the ground " +
                      _gunTest.Spot.DistanceTo(pos).ToString("0.0") + " m ahead of them");
            Hint("Gunshots: six ways, a few seconds apart, at the ground in front of " + _gunTest.Name + ". Note which you hear.", 4000);
            return null;
        }

        /// From Tick04 while a run is on.
        private void GunshotTestStep()
        {
            if (_gunTest != null)
                Safe("gunshot test", _aGunTest ?? (_aGunTest = () => GunshotTestTick(_now)));
        }

        private void GunshotTestTick(int now)
        {
            GunshotRun t = _gunTest;
            int h = t.Handle;
            if (!ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h) || _missionPaused)
            {
                GunshotTestEnd(_missionPaused ? "a mission started" : "they're gone");
                return;
            }
            if (Game.Player.Character.Position.DistanceTo(ENTITY.GET_ENTITY_COORDS(h, true, true)) > 40f)
            {
                GunshotTestEnd("you went more than 40 m off");
                return;
            }
            if (t.Phase == 1)
            {
                // watched every frame after each shot (0.6 s; 2 s for the aimed trigger and the shoot task, which raise the gun
                // first); the ear is the real test (see the top)
                t.Shooting |= PED.IS_PED_SHOOTING(h);
                if (now - t.FiredAt < (t.Idx >= 4 ? 2000 : 600))
                    return;
                string line = "#" + (t.Idx + 1) + " (" + GunshotWays[t.Idx] + "): " + (t.Shooting ? "shooting seen" : "no shooting seen") +
                              ", the gun in hand: " + (WEAPON.GET_CURRENT_PED_WEAPON_ENTITY_INDEX(h, 0) != 0 ? "yes" : "NO");
                t.Results.Add(line);
                Log.Write("GUNSHOT " + line);
                TASK.CLEAR_PED_TASKS(h, true, false);
                WEAPON.SET_CURRENT_PED_WEAPON(h, TestRevolver, true, 0, false, false);   // the clear may have put it away
                t.Idx++;
                t.Phase = 0;
                t.NextAt = now + 3400;
                return;
            }
            if (now < t.NextAt)
                return;
            if (t.Idx >= GunshotWays.Length)
            {
                GunshotTestEnd(null);
                return;
            }
            if (t.Phase == 2)
            {
                Fire(t, h, now);   // the aim had its moment
                return;
            }
            WEAPON.SET_PED_AMMO(h, TestRevolver, 12);
            WEAPON.SET_AMMO_IN_CLIP(h, TestRevolver, 6);
            Hint("Gunshot " + (t.Idx + 1) + " of " + GunshotWays.Length + ": " + GunshotWays[t.Idx] + ". Did you hear it?", 3800);
            if (t.Idx == 4)
            {
                TASK.TASK_AIM_GUN_AT_COORD(h, t.Spot.X, t.Spot.Y, t.Spot.Z, 3000, false, false);   // aimed first, the trigger a second later
                t.Phase = 2;
                t.NextAt = now + 1100;
                return;
            }
            Fire(t, h, now);
        }

        private void Fire(GunshotRun t, int h, int now)
        {
            Vector3 s = t.Spot, pos = ENTITY.GET_ENTITY_COORDS(h, true, true);
            Vector3 hand = PED.GET_PED_BONE_COORDS(h, RightHandBone, 0f, 0f, 0f);
            if (hand.DistanceTo(pos) > 2f)
                hand = pos + new Vector3(0f, 0f, 0.3f);
            Vector3 side = Vector3.Cross(s - pos, new Vector3(0f, 0f, 1f));
            side.Z = 0f;
            if (side.Length() < 0.1f)
                side = new Vector3(1f, 0f, 0f);
            side.Normalize();
            Vector3 beside = pos + side + new Vector3(0f, 0f, 0.3f);   // a metre to their side, clear of anyone's body
            t.Shooting = false;
            switch (t.Idx)
            {
                case 0:
                case 4:
                    Function.Call(ShootTrigger, h, s.X, s.Y, s.Z, 0, -1f, 0, 0f);
                    break;
                case 1:
                    MISC.SHOOT_SINGLE_BULLET_BETWEEN_COORDS(hand.X, hand.Y, hand.Z, s.X, s.Y, s.Z, 500, true, TestRevolver, h, true, false, 1000f, false);
                    break;
                case 2:
                    MISC.SHOOT_SINGLE_BULLET_BETWEEN_COORDS(beside.X, beside.Y, beside.Z, s.X, s.Y, s.Z, 500, true, TestRevolver, h, true, false, 1000f, false);
                    break;
                case 3:
                    MISC.SHOOT_SINGLE_BULLET_BETWEEN_COORDS(beside.X, beside.Y, beside.Z, s.X, s.Y, s.Z, 500, true, TestRevolver, 0, true, false, 1000f, false);
                    break;
                case 5:
                    TASK.TASK_SHOOT_AT_COORD(h, s.X, s.Y, s.Z, 1200, Natives.Hash("FIRING_PATTERN_SINGLE_SHOT"), 0);
                    break;
            }
            t.FiredAt = now;
            t.Phase = 1;
        }

        /// Over, or stopped: the revolver taken back, their orders let go, and they wander on.
        private void GunshotTestEnd(string why)
        {
            GunshotRun t = _gunTest;
            _gunTest = null;
            if (t == null)
                return;
            int h = t.Handle;
            if (t.GunGiven && ENTITY.DOES_ENTITY_EXIST(h) && !ENTITY.IS_ENTITY_DEAD(h))
            {
                WEAPON.REMOVE_WEAPON_FROM_PED(h, TestRevolver, true, TakeReason);
                WEAPON.SET_CURRENT_PED_WEAPON(h, Natives.Hash("WEAPON_UNARMED"), true, 0, false, false);
            }
            Spawner.Release(new Ped(h), false);   // handed back: events unblocked, orders cleared, wandering (never deleted)
            Log.Write("GUNSHOT test done on " + t.Name + ": " + t.Results.Count + " of " + GunshotWays.Length + " tried" + (why != null ? " (" + why + ")" : ""));
            Hint("Gunshots over" + (why != null ? " (" + why + ")" : "") + ". Which numbers did you hear? (each in the log, GUNSHOT)", 7000);
        }

        /// The gun in their hand fired at a point by its own trigger, the one kind of shot we make that is heard (see the top):
        /// a round in the cylinder first. False when there's no gun in hand to fire (weapon 0: their ammo left alone).
        private bool GunTrigger(int h, Vector3 at, uint weapon)
        {
            if (WEAPON.GET_CURRENT_PED_WEAPON_ENTITY_INDEX(h, 0) == 0)
                return false;
            if (weapon != 0)
            {
                WEAPON.SET_PED_AMMO(h, weapon, Math.Max(WEAPON.GET_AMMO_IN_PED_WEAPON(h, weapon), 6));
                WEAPON.SET_AMMO_IN_CLIP(h, weapon, 6);   // as the row had it when it was heard
            }
            Function.Call(ShootTrigger, h, at.X, at.Y, at.Z, 0, -1f, 0, 0f);   // p4 0, p5 -1 as R*'s scripts, p6 0, p7 0: as heard in the row
            return true;
        }

        private string GunshotTestStatus()
        {
            return _gunTest != null ? "running on " + _gunTest.Name + ", shot " + (_gunTest.Idx + 1) + " of " + GunshotWays.Length : "not running (the Testing page: Gunshots at the ground)";
        }
    }
}
