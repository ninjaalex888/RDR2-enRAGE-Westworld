// Despair acted out with the game's own clips (0.6.3; Lex, 2026-10-27, after the Testing page's animation rows: both gun
// arcs, "Poison, yes"). The sets are the story's and the world's own, all in Story Mode (WAKE anim sets), tried on a man
// and a woman in game: Jamie's arc (his despair, the gun to his head, the shaking, the shot) and a hideout
// leader's (the wait, the gun up, the shot); the poisoning as a long drink and the fall, or slowly (sick, burning, gone).
// Before every way, the last words are said sobbing (a man's sobbing from a roadside accident, which a woman plays as
// well). The gun is forced into the hand just before (clearing orders puts it away, and someone with no holster loses
// it), the shot fired from the gun hand into the head at a point of the last clip (logged, to be tuned), and a clip that
// won't start falls back to the way before (the aim at the ground; for poison, going limp). Core/Despair.cs decides the
// way, the arc and the poisoning, by the person.
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
        private sealed class DespairClip
        {
            public string Dict, Clip;
            public int MaxMs, Flags;
            public float ShotAt = -1f;   // the shot at this point of the clip (0-1), the gun arcs' last clip
        }

        private const string JamieDict = "script_story@mry1@ig@ig6_jamie_attempts_suicide", LeaderDict = "script_hideout@shady_belle@leader";
        private const string DrinkDict = "script_re@moonshine_camp@return_taste@trans", SickDict = "script_re@poisoned@victim";
        private const string SobDict = "script_re@rally@cross_accident", SobClip = "sobbing_idle_a_leader";

        private static DespairClip DC(string dict, string clip, int maxMs, int flags = 0, float shotAt = -1f)
        {
            return new DespairClip { Dict = dict, Clip = clip, MaxMs = maxMs, Flags = flags, ShotAt = shotAt };
        }

        /// The sobbing and every despair set asked for ahead (an awake act beginning): asked for and checked in the same
        /// frame, the first despair of a session never sobbed.
        private void DespairPreload()
        {
            foreach (string d in new[] { SobDict, JamieDict, LeaderDict, DrinkDict, SickDict })
                STREAMING.REQUEST_ANIM_DICT(d);
        }

        /// The clips for a way, in order (null: the way has none, the wall and the knife).
        private static DespairClip[] DespairSequence(string method, string variant, string seed)
        {
            if (method == "gun" && variant == "jamie")
                return new[]
                {
                    DC(JamieDict, "ig6_i_dont_want_to_live_ig_jamie", 5500),
                    DC(JamieDict, "ig6_gun_to_head_ig_jamie", 3900),
                    DC(JamieDict, Voice.Chance(seed + "loop", 50) ? "ig6_jamie_loop_ig_jamie" : "ig6_jamie_loop3_ig_jamie", 5600),
                    DC(JamieDict, "ig6_jamie_shoots_himself_ig_jamie", 3000, 2, 0.30f)
                };
            if (method == "gun")
                return new[]
                {
                    DC(LeaderDict, "base", 4900),
                    DC(LeaderDict, "action", 7000),
                    DC(LeaderDict, "suicide_shoot", 2600, 2, 0.35f)
                };
            if (method == "poison" && variant == "drink")
                return new[] { DC(DrinkDict, "trans_to_drink_male_a", 8700), DC(DrinkDict, "die_male_a", 13000, 2) };
            if (method == "poison")
                return new[] { DC(SickDict, "intro_vic", 11000), DC(SickDict, "everything_burns_vic", 8300), DC(SickDict, "im_going_vic", 8100), DC(SickDict, "death_loop_vic", 2000, 2) };
            return null;
        }

        /// The despair ending set going (ChooseEnding, or the Testing page's row with a way forced): the way, the arc or
        /// the poisoning, the clips asked for now, and the last words said sobbing (else looking down at their hands).
        private void BeginDespair(WakeAct act, Ped me, int now, string forced = null)
        {
            int h = act.Handle;
            Ped ped = new Ped(h);
            bool armed = WEAPON.IS_PED_ARMED(h, 4);
            Vector3 wall, dir;
            bool hasWall = DespairWall(ped, me, out wall, out dir);
            string method = forced != null ? forced.Split(':')[0] : Despair.Method(act.P, armed, hasWall, act.P.Id);
            if (method == "wall" && !hasWall)
                method = "knife";
            act.Method = method;
            act.Variant = forced != null && forced.Contains(":") ? forced.Split(':')[1]
                : method == "gun" ? Despair.GunArc(act.P, act.P.Id) : method == "poison" ? Despair.PoisonWay(act.P, act.P.Id) : null;
            act.Seq = DespairSequence(method, act.Variant, act.P.Id);
            act.Rec = _bridge.RecordAt(h);   // one of enRAGE's people: booked in its record when it's over
            act.WallAt = wall;
            act.WallDir = dir;
            act.Weapon = method == "gun" ? (armed ? null : Despair.Gun(act.P)) : method == "knife" ? Despair.Knife : null;
            DespairSets();
            if (method == "knife" && _bladeSet == 1)
                STREAMING.REQUEST_ANIM_DICT(BladeDict);   // asked for now: it played once in two when asked at the last step
            if (method == "knife" && _writheSet == 1)
                STREAMING.REQUEST_ANIM_DICT(WritheDict);
            if (act.Seq != null)
                foreach (string d in act.Seq.Select(c => c.Dict).Distinct())
                    STREAMING.REQUEST_ANIM_DICT(d);
            STREAMING.REQUEST_ANIM_DICT(SobDict);
            TASK.CLEAR_PED_TASKS(h, true, false);
            bool sob = STREAMING.HAS_ANIM_DICT_LOADED(SobDict);
            act.Sobbed = sob;   // else the last words' steps start it once the set is in (StepDespair)
            if (sob)
                TASK.TASK_PLAY_ANIM(h, SobDict, SobClip, 4f, -4f, -1, 0, 0f, false, 0, false, "", false);   // the last words, sobbing
            else
            {
                // the last words said looking down at their own hands (Lex's P29), while the sobbing set loads
                TASK.TASK_STAND_STILL(h, 4500);
                double hd = ENTITY.GET_ENTITY_HEADING(h) * Math.PI / 180.0;
                Vector3 hands = ped.Position + new Vector3((float)-Math.Sin(hd) * 0.45f, (float)Math.Cos(hd) * 0.45f, -0.35f);
                TASK.TASK_LOOK_AT_COORD(h, hands.X, hands.Y, hands.Z, 4000, 0, 51, 0);
            }
            act.Until = now + 30000;
            DespairLastWords(act, me);
            act.PhaseUntil = now + (sob ? 4500 : 3400);
            Log.Write("WAKE despair: " + act.Name + " can't bear it: " + DespairHow(act) + (act.Seq != null ? " (" + act.Seq.Length + " clips)" : "") +
                      (hasWall ? " (a wall " + Flat(wall, ped.Position).ToString("0.0") + " m off)" : " (no wall to run at)") + (armed ? ", armed" : "") + (sob ? ", sobbing" : "") +
                      (forced != null ? " (a test)" : ""));
        }

        /// Phase 4 for a way with clips: the gun forced into the hand first, then the clips one after another.
        private void DespairSeqStart(WakeAct act, int now)
        {
            act.Sub = 10;
            act.ClipIdx = 0;
            act.ClipOn = false;
            act.ClipAt = now;
            act.ShotDone = false;
            act.Until = now + act.Seq.Sum(c => c.MaxMs) + 8000;
            if (act.Method == "gun")
            {
                DespairHand(act);
                act.PhaseUntil = now + 500;   // a moment for it to reach the hand
            }
            else
                act.PhaseUntil = now + 100;
        }

        /// The gun into the hand, forced: their own if they carry one, else one that fits them, given only now.
        private void DespairHand(WakeAct act)
        {
            int h = act.Handle;
            uint w = 0;
            if (act.Weapon == null)
            {
                w = WEAPON.GET_BEST_PED_WEAPON(h, false, false);
                if (w == 0 || w == Natives.Hash("WEAPON_UNARMED"))
                    act.Weapon = Despair.Gun(act.P);
            }
            if (act.Weapon != null)
            {
                w = Natives.Hash(act.Weapon);
                if (!WEAPON.HAS_PED_GOT_WEAPON(h, w, 0, false))
                    act.Given = true;
            }
            WEAPON.GIVE_WEAPON_TO_PED(h, w, 6, true, false, 0, false, 0.5f, 1f, GiveReason, true, 0f, false);   // forced into the hand
            WEAPON.SET_PED_DROPS_WEAPONS_WHEN_DEAD(h, false);
            WEAPON.SET_CURRENT_PED_WEAPON(h, w, true, 0, false, false);
            act.Arm = w;
        }

        /// One step of the clips: each played when its set is in, the shot at its point of the last gun clip, the next when
        /// it's over; at the end, poison goes limp and dies. A clip that won't play falls back to the way before.
        private void DespairSeq(WakeAct act, int now)
        {
            int h = act.Handle;
            DespairClip c = act.Seq[act.ClipIdx];
            if (!act.ClipOn)
            {
                if (!STREAMING.HAS_ANIM_DICT_LOADED(c.Dict))
                {
                    STREAMING.REQUEST_ANIM_DICT(c.Dict);
                    if (now - act.ClipAt > 2500)
                    {
                        DespairSeqFallback(act, now, c.Clip + "'s set didn't load");
                        return;
                    }
                    act.PhaseUntil = now + 100;
                    return;
                }
                if (act.Method == "gun" && act.ClipIdx == 0 && WEAPON.GET_CURRENT_PED_WEAPON_ENTITY_INDEX(h, 0) == 0)
                    DespairHand(act);   // the sobbing put it away
                TASK.TASK_PLAY_ANIM(h, c.Dict, c.Clip, 4f, -4f, -1, c.Flags, 0f, false, 0, false, "", false);
                act.ClipOn = true;
                act.ClipSeen = false;
                act.ClipRetried = false;
                act.ClipAt = now;
                act.PhaseUntil = now + 100;
                return;
            }
            bool playing = ENTITY.IS_ENTITY_PLAYING_ANIM(h, c.Dict, c.Clip, 3);
            if (playing)
                act.ClipSeen = true;
            else if (!act.ClipSeen && now - act.ClipAt > 1500)
            {
                if (!act.ClipRetried)
                {
                    act.ClipRetried = true;
                    Log.Write("WAKE despair: " + act.Name + "'s " + c.Clip + " didn't start (" + (PED.IS_PED_USING_ANY_SCENARIO(h) ? "in a scenario, " : "") +
                              "the play task's status " + TASK.GET_SCRIPT_TASK_STATUS(h, PlayAnimTask, true) + "); once more");
                    TASK.CLEAR_PED_TASKS_IMMEDIATELY(h, false, true);
                    if (act.Method == "gun")
                        DespairHand(act);   // the clear put it away
                    TASK.TASK_PLAY_ANIM(h, c.Dict, c.Clip, 8f, -4f, -1, c.Flags, 0f, false, 0, false, "", false);
                    act.ClipAt = now;
                    act.PhaseUntil = now + 100;
                    return;
                }
                DespairSeqFallback(act, now, c.Clip + " didn't start, twice");
                return;
            }
            if (c.ShotAt >= 0f && !act.ShotDone && playing)
            {
                float at = ENTITY._GET_ENTITY_ANIM_CURRENT_TIME(h, c.Dict, c.Clip);
                if (at >= c.ShotAt)
                {
                    act.ShotDone = true;
                    Log.Write("WAKE despair: " + act.Name + "'s shot at " + (at * 100f).ToString("0") + "% of " + c.Clip + " (set for " + (c.ShotAt * 100f).ToString("0") + "%)" +
                              ", the gun " + (WEAPON.GET_CURRENT_PED_WEAPON_ENTITY_INDEX(h, 0) != 0 ? "in hand" : "NOT in hand"));
                    DespairShot(act, now);   // from the gun hand into the head: Phase 5
                    return;
                }
                if (now - act.ClipAt < c.MaxMs)
                {
                    act.PhaseUntil = now;   // the next step (50 ms) looks again
                    return;
                }
            }
            if (now - act.ClipAt < c.MaxMs && (playing || !act.ClipSeen))
            {
                act.PhaseUntil = now + 100;
                return;
            }
            act.ClipIdx++;
            act.ClipOn = false;
            act.ClipAt = now;
            if (act.ClipIdx < act.Seq.Length)
            {
                act.PhaseUntil = now + 50;
                return;
            }
            // the last clip over
            if (act.Method == "gun" && !act.ShotDone)
            {
                act.ShotDone = true;
                Log.Write("WAKE despair: " + act.Name + "'s shot after the last clip (its point never came)");
                DespairShot(act, now);
                return;
            }
            Log.Write("WAKE despair: " + act.Name + "'s " + DespairHow(act) + ": " + act.Seq.Length + " clips played, then down");
            PED.SET_PED_TO_RAGDOLL(h, 1500, 2500, 0, false, false, "");
            act.Phase = 5;
            act.PhaseUntil = now + 1200;
        }

        /// A clip that won't play: the way before (the gun aimed at the ground and fired; poison, limp where they stand).
        private void DespairSeqFallback(WakeAct act, int now, string why)
        {
            Log.Write("WAKE despair: " + act.Name + ": " + why + "; the way before instead");
            act.Seq = null;
            act.Sub = 0;
            if (act.Method == "gun")
            {
                DespairArm(act, now);   // the aim at the ground, then the shot (DespairAimed, DespairShot)
                return;
            }
            PED.SET_PED_TO_RAGDOLL(act.Handle, 1500, 2500, 0, false, false, "");
            act.Phase = 5;
            act.PhaseUntil = now + 1500;
        }

        // ---- the Testing page: despair now, each way in turn --------------------------------------------------------

        private static readonly string[] DespairTestWays = { "gun:jamie", "gun:leader", "poison:drink", "poison:slow", "knife", "wall" };
        private int _despairTestTurn;

        /// The nearest free person RealNPCs knows (a resident or one of enRAGE's townsfolk; never a record, a story
        /// character, a child or a counter's keeper) straight to the despair ending, the next way in turn; no roll, no mark.
        private string DespairNow(int now)
        {
            if (!_cfg.WestWorld)
                return "West World is off";
            Ped me = Game.Player.Character;
            foreach (Ped p in World.GetAllPeds().Where(x => x != null && x.Exists() && !x.IsDead && x.Handle != me.Handle && PED.IS_PED_HUMAN(x.Handle) &&
                                                             x.Position.DistanceTo(me.Position) < 12f && x.Position.DistanceTo(me.Position) >= 3f)
                                                 .OrderBy(x => x.Position.DistanceTo(me.Position)))
            {
                int h = p.Handle;
                if (_bridge.RecordAt(h) != null || Natives.StoryModel(p) || !IsGrownUp(p) || MayDirectWhyNot(p) != null || NotFree(h))
                    continue;
                Person who;
                PersonState st;
                Resident r2;
                bool passive;
                WakeIdentity(p, true, out who, out st, out r2, out passive);   // not woken yet is fine: a test wakes them all the way
                if (who == null)
                    continue;
                if (st == null)
                    st = _store.Get(who);
                if (st.WakeStage < Awakening.Awake)
                {
                    st.WakeStage = Awakening.Awake;
                    st.WakeStageAt = Bridge.Now();
                }
                string way = DespairTestWays[_despairTestTurn++ % DespairTestWays.Length];
                WakeAct act = new WakeAct { Handle = h, Kind = "awake", Stage = Awakening.Awake, Started = now, NextStep = now + 400, Name = who.Name, P = who, St = st, Res = r2,
                                            A = p.Position, Ending = "despair", Phase = 3 };
                Take(act);
                _wakeActs.RemoveAll(a => a.Handle == h);
                _wakeActs.Add(act);
                BeginDespair(act, me, now, way);
                return null;
            }
            return "nobody free within 3-12 m whom RealNPCs knows (a resident or one of enRAGE's townsfolk; not a record, a story character or a counter's keeper)";
        }
    }
}
