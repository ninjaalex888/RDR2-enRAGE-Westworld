// The Testing page's animation rows (0.6.3; Lex, 2026-10-27: the despair endings and each wake stage's body language).
// A search of the game's dump found authored suicide arcs from the story (Jamie's, a hideout leader's, the gunslinger's),
// collapses, grief, a poisoning and gestures for each stage, and all 23 of their sets are in Story Mode (WAKE anim sets).
// Whether a mission clip plays well on an ordinary person is another matter (it may slide from its scene's origin, wait
// for a prop, or run long), so before any of them goes into despair or the stages, these rows play each one, numbered,
// on the nearest free person and log whether it started, how long it ran and how far it moved them. A gun clip gets a
// revolver in their hand, never fired, taken back at the end. Nothing is noted, no tie, no memory.
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
        private sealed class AnimClip
        {
            public string Group, Dict, Clip, FDict, FClip;   // FDict/FClip: the women's set, where the game has one
            public int Flags;                                 // eScriptedAnimFlags as the rest of the add-on uses them: 0 once, 1 loop, 2 hold the last frame, 8|16 upper body
            public bool Gun;
        }

        private const int Once = 0, Loop = 1, Hold = 2, Upper = 8 | 16;

        private static AnimClip A(string group, string dict, string clip, int flags, bool gun = false, string fdict = null, string fclip = null)
        {
            return new AnimClip { Group = group, Dict = dict, Clip = clip, Flags = flags, Gun = gun, FDict = fdict, FClip = fclip };
        }

        private static readonly AnimClip[] DespairClips =
        {
            A("despair, gun", "script_story@mry1@ig@ig6_jamie_attempts_suicide", "ig6_i_dont_want_to_live_ig_jamie", Once, true),
            A("despair, gun", "script_story@mry1@ig@ig6_jamie_attempts_suicide", "ig6_gun_to_head_ig_jamie", Once, true),
            A("despair, gun", "script_story@mry1@ig@ig6_jamie_attempts_suicide", "ig6_jamie_loop_ig_jamie", Once, true),
            A("despair, gun", "script_story@mry1@ig@ig6_jamie_attempts_suicide", "ig6_jamie_loop3_ig_jamie", Once, true),
            A("despair, gun", "script_story@mry1@ig@ig6_jamie_attempts_suicide", "ig6_jamie_shoots_himself_ig_jamie", Hold, true),
            A("despair, gun", "script_hideout@shady_belle@leader", "base", Once, true),
            A("despair, gun", "script_hideout@shady_belle@leader", "action", Once, true),
            A("despair, gun", "script_hideout@shady_belle@leader", "suicide_shoot", Hold, true),
            A("despair, gun", "script_rc@gun3@ig@ig3_shoothimself", "dissarm_shoothimself_famousgunslinger_03", Hold, true),
            A("collapse", "ai_damage@ko@base@shot", "ko_shot_head", Hold),
            A("collapse", "ai_damage@ko@base", "ko_collapse", Hold),
            A("collapse", "mini_duel@base", "die_npc", Hold),
            A("collapse", "script_rc@gun3@ig@ig2_dueldroptoknee", "killbill_longdeath_rhonly_famousgunslinger_03", Hold),
            A("grief before", "script_re@rally@cross_accident", "sobbing_base_leader", Once),
            A("grief before", "script_re@rally@cross_accident", "sobbing_idle_a_leader", Once),
            A("grief before", "script_story@mob2@ig@ig_10_lady_praying", "kneel_light_sob_a_f_m_nbxtownfolk_01", Once),
            A("grief before", "script_story@mob2@ig@ig_10_lady_praying", "kneel_whail_pray_a_f_m_nbxtownfolk_01", Once),
            A("grief before", "script_common@grieving@player@unarmed@upper", "intro", Upper),
            A("grief before", "script_common@grieving@player@unarmed@upper", "loop", Upper),
            A("poison (to see)", "script_re@moonshine_camp@return_taste@trans", "trans_to_drink_male_a", Once),
            A("poison (to see)", "script_re@moonshine_camp@return_taste@trans", "die_male_a", Hold),
            A("poison (to see)", "script_re@poisoned@victim", "intro_vic", Once),
            A("poison (to see)", "script_re@poisoned@victim", "everything_burns_vic", Once),
            A("poison (to see)", "script_re@poisoned@victim", "im_going_vic", Once),
            A("poison (to see)", "script_re@poisoned@victim", "death_loop_vic", Once),
        };

        private const string MSilent = "ai_gestures@gen_male@standing@silent", FSilent = "ai_gestures@gen_female@standing@silent";
        private const string MSpeaker = "ai_gestures@gen_male@standing@speaker", FSpeaker = "ai_gestures@gen_female@standing@speaker";
        private const string MWander = "amb_wander@code_human_wander@male@generic", FWander = "amb_wander@code_human_wander@female@generic";
        private const string Unapproved = "script_common@gestures@unapproved", Madman = "script_special_ped@pdjbm_john_baptising_madman@ig1_speech_greets";

        private static readonly AnimClip[] StageClips =
        {
            A("1 stirring", MWander, "wipe_eyes_01", Upper, false, FWander, "wipe_eyes_02"),
            A("1 stirring", MWander, "touch_face_01", Upper, false, FWander, "touch_face_01"),
            A("1 stirring", MWander, "rub_neck_01", Upper, false, FWander, "rub_neck_01"),
            A("1 stirring", MSilent, "silent_confused_look_f_001", Upper, false, FSilent, "silent_confused_look_f_001"),
            A("2 questioning", MSilent, "silent_frustrated_lookup_f_001", Upper),
            A("2 questioning", Unapproved, "dont_know", Upper),
            A("2 questioning", Unapproved, "but_why", Upper),
            A("2 questioning", Unapproved, "is_this_it", Upper),
            A("2 questioning", "ai_react@point@base", "point_up_fwd", Upper),
            A("2 questioning", FSpeaker, "neutral_pointhead_l_001", Upper),
            A("3 unravelling", Madman, "greet_a_he's_watching_john_baptist", Once),
            A("3 unravelling", Madman, "greet_j_i_think_im_crazy_john_baptist", Once),
            A("3 unravelling", Madman, "greet_e_this_is_total_madness_john_baptist", Once),
            A("3 unravelling", Madman, "greet_d_im_not_coping_vry_well_john_baptist", Once),
            A("3 unravelling", "script_re@lost_man", "losing_my_mind", Once),
            A("3 unravelling", "script_re@lost_man", "i_ll_die_out_here", Once),
            A("3 unravelling", Unapproved, "despair", Upper),
            A("3 unravelling", Unapproved, "to_hell_with_it", Upper),
            A("3 unravelling", "mech_loco_m@generic@flinch@unarmed@idle", "flinch_high_a", Once),
            A("4 awake", MSpeaker, "neutral_get_attention_l_001", Upper, false, FSpeaker, "neutral_get_attention_l_001"),
            A("4 awake", MSpeaker, "calm_down_f_002", Upper),
            A("4 awake", MSpeaker, "defensive_plead_f_001", Upper, false, FSpeaker, "defensive_plead_f_001"),
            A("4 awake", "ai_gestures@gen_male@standing@speaker@no_hat", "silent_neutral_bow_l_001", Upper),
            A("4 awake", "mech_loco_m@generic@reaction@handsup@unarmed@normal", "intro", Once),
            A("4 awake", "mech_loco_m@generic@reaction@handsup@unarmed@normal", "loop", Loop),
        };

        private sealed class AnimTestRun
        {
            public int Handle, Idx, Phase, NextAt, Until, Started;
            public string Name, Which, Dict, Clip;
            public bool Woman, GunGiven, Started1, Flag0Tried;
            public AnimClip[] Clips;
            public Vector3 From;
            public readonly List<string> Results = new List<string>();
        }

        private AnimTestRun _animTest;
        private Action _aAnimTest;
        private static readonly uint TestRevolver = Natives.Hash("WEAPON_REVOLVER_CATTLEMAN");

        private string AnimTestStart(string which, int now)
        {
            if (_animTest != null)
                AnimTestEnd("a new run was started");
            Ped me = Game.Player.Character;
            Ped p = World.GetAllPeds().Where(x => x != null && x.Exists() && !x.IsDead && x.Handle != me.Handle && PED.IS_PED_HUMAN(x.Handle) && !Natives.StoryModel(x) &&
                                                  x.Position.DistanceTo(me.Position) < 12f && _bridge.RecordAt(x.Handle) == null && MayDirectWhyNot(x) == null && !NotFree(x.Handle))
                                     .OrderBy(x => x.Position.DistanceTo(me.Position)).FirstOrDefault();
            if (p == null)
                return "nobody free within 12 m (not one of enRAGE's people, not in a scene)";
            int h = p.Handle;
            Someone who = SomeoneOf(p);
            bool woman = !PED.IS_PED_MALE(h);
            _animTest = new AnimTestRun
            {
                Handle = h, Name = who != null ? who.Name : Grammar.UpperFirst(Director.Kind(p, woman)), Woman = woman, Which = which,
                Clips = which == "despair" ? DespairClips : StageClips, NextAt = now + 1600
            };
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, true);
            TASK.CLEAR_PED_TASKS(h, true, false);   // up from a seat or a scenario first
            TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, me.Handle, 1500, 0f, 0f, 0f);
            Log.Write("WAKE anim test (" + which + "): " + _animTest.Clips.Length + " clips on " + _animTest.Name + " (" + (woman ? "a woman" : "a man") + ", " +
                      (NemesisSystem.Models.Name(ENTITY.GET_ENTITY_MODEL(h)) ?? "?") + ")");
            return null;
        }

        /// From Tick04 while a run is on.
        private void AnimTestStep()
        {
            if (_animTest != null)
                Safe("anim test", _aAnimTest ?? (_aAnimTest = () => AnimTestTick(_now)));
        }

        private void AnimTestTick(int now)
        {
            AnimTestRun t = _animTest;
            int h = t.Handle;
            if (!ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h) || _missionPaused)
            {
                AnimTestEnd(_missionPaused ? "a mission started" : "they're gone");
                return;
            }
            if (Game.Player.Character.Position.DistanceTo(ENTITY.GET_ENTITY_COORDS(h, true, true)) > 40f)
            {
                AnimTestEnd("you went more than 40 m off");
                return;
            }
            if (now < t.NextAt)
                return;
            switch (t.Phase)
            {
                case 0:   // the next clip: its set asked for
                {
                    if (t.Idx >= t.Clips.Length)
                    {
                        AnimTestEnd(null);
                        return;
                    }
                    AnimClip c = t.Clips[t.Idx];
                    bool f = t.Woman && c.FDict != null;
                    t.Dict = f ? c.FDict : c.Dict;
                    t.Clip = f ? c.FClip : c.Clip;
                    STREAMING.REQUEST_ANIM_DICT(t.Dict);
                    if (c.Gun && !t.GunGiven)
                    {
                        WEAPON.GIVE_WEAPON_TO_PED(h, TestRevolver, 6, true, false, 0, false, 0.5f, 1f, GiveReason, true, 0f, false);
                        WEAPON.SET_PED_DROPS_WEAPONS_WHEN_DEAD(h, false);
                        t.GunGiven = true;
                    }
                    if (t.GunGiven)
                        WEAPON.SET_CURRENT_PED_WEAPON(h, c.Gun ? TestRevolver : Natives.Hash("WEAPON_UNARMED"), true, 0, false, false);   // in hand only for the gun clips
                    t.Phase = 1;
                    t.Until = now + 3000;
                    t.NextAt = now + 100;
                    return;
                }
                case 1:   // loaded: play it, numbered on screen
                {
                    AnimClip c = t.Clips[t.Idx];
                    if (!STREAMING.HAS_ANIM_DICT_LOADED(t.Dict))
                    {
                        if (now < t.Until)
                            return;
                        AnimTestResult(t, "the set didn't load");
                        return;
                    }
                    t.From = ENTITY.GET_ENTITY_COORDS(h, true, true);
                    TASK.TASK_PLAY_ANIM(h, t.Dict, t.Clip, 4f, -4f, -1, c.Flags, 0f, false, 0, false, "", false);
                    float secs = ENTITY.GET_ANIM_DURATION(t.Dict, t.Clip);
                    int ms = secs > 0f ? (int)(secs * 1000f) : 4000;
                    if (c.Flags == Loop)
                        ms = 4000;
                    t.Started = now;
                    t.Started1 = false;
                    t.Flag0Tried = false;
                    t.Until = now + Math.Min(ms, 12000) + 400;
                    t.Phase = 2;
                    t.NextAt = now + 300;
                    Hint("Animation " + (t.Idx + 1) + " of " + t.Clips.Length + " (" + c.Group + "): " + t.Clip + (secs > 0f ? ", " + secs.ToString("0.0") + " s" : ""), Math.Min(ms, 12000) + 1500);
                    return;
                }
                case 2:   // playing: did it start, and when is it over
                {
                    AnimClip c = t.Clips[t.Idx];
                    bool playing = ENTITY.IS_ENTITY_PLAYING_ANIM(h, t.Dict, t.Clip, 3);
                    if (playing)
                        t.Started1 = true;
                    else if (!t.Started1 && now - t.Started >= 300 && !t.Flag0Tried && c.Flags == Upper)
                    {
                        t.Flag0Tried = true;   // an upper-body clip the check can't see: over the whole body instead (as the stalker's gestures do)
                        TASK.TASK_PLAY_ANIM(h, t.Dict, t.Clip, 4f, -4f, -1, Once, 0f, false, 0, false, "", false);
                        t.NextAt = now + 300;
                        return;
                    }
                    if (now < t.Until && (playing || (!t.Started1 && now - t.Started < 1500)))
                    {
                        t.NextAt = now + 200;
                        return;
                    }
                    float moved = ENTITY.GET_ENTITY_COORDS(h, true, true).DistanceTo(t.From);
                    float secs = ENTITY.GET_ANIM_DURATION(t.Dict, t.Clip);
                    AnimTestResult(t, (t.Started1 ? "played" + (t.Flag0Tried ? " (over the whole body)" : "") : "didn't start") + ", " + secs.ToString("0.0") + " s long, ran " +
                                      ((now - t.Started) / 1000.0).ToString("0.0") + " s, moved them " + moved.ToString("0.0") + " m" + (moved >= 1f ? " (it carries them off: drop it or play it in place)" : ""));
                    return;
                }
            }
        }

        private void AnimTestResult(AnimTestRun t, string result)
        {
            AnimClip c = t.Clips[t.Idx];
            string line = "#" + (t.Idx + 1) + " (" + c.Group + ") " + t.Dict + " / " + t.Clip + ": " + result;
            t.Results.Add(line);
            Log.Write("WAKE anim " + line);
            TASK.CLEAR_PED_TASKS(t.Handle, true, false);   // back on their feet for the next
            STREAMING.REMOVE_ANIM_DICT(t.Dict);
            t.Idx++;
            t.Phase = 0;
            t.NextAt = Game.GameTime + 1300;
        }

        /// Over, or stopped: the revolver taken back, their orders let go, and they wander on.
        private void AnimTestEnd(string why)
        {
            AnimTestRun t = _animTest;
            _animTest = null;
            if (t == null)
                return;
            int h = t.Handle;
            if (ENTITY.DOES_ENTITY_EXIST(h))
            {
                if (t.GunGiven)
                {
                    WEAPON.REMOVE_WEAPON_FROM_PED(h, TestRevolver, true, TakeReason);
                    WEAPON.SET_CURRENT_PED_WEAPON(h, Natives.Hash("WEAPON_UNARMED"), true, 0, false, false);
                }
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, false);
                if (!ENTITY.IS_ENTITY_DEAD(h))
                {
                    TASK.CLEAR_PED_TASKS(h, true, false);
                    TASK.TASK_WANDER_STANDARD(h, 10f, 10);
                }
            }
            int played = t.Results.Count(r => r.Contains(": played"));
            Log.Write("WAKE anim test (" + t.Which + ") done on " + t.Name + ": " + played + " of " + t.Clips.Length + " played" + (why != null ? " (" + why + ")" : ""));
            Hint("Animations: " + played + " of " + t.Clips.Length + " played (each in the log, WAKE anim)" + (why != null ? "; " + why : "") + ".", 6000);
        }

        private string AnimTestStatus()
        {
            return _animTest != null ? "running (" + _animTest.Which + ") on " + _animTest.Name + ", clip " + (_animTest.Idx + 1) + " of " + _animTest.Clips.Length
                : "not running (the Testing page: Despair animations, Wake stage gestures)";
        }
    }
}
