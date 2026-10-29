// The waking speak in their own recorded voices (0.6.3; Lex, 2026-10-27). Core/WakeVoice.cs chooses the moment by
// stage and what's around them; this plays it: a look (at you, the sky, their hands, someone near), the line or lines,
// then the thought over the head. One woken voice at a time (three awake people by a saloon would be noise). Outside a
// wake act it's anyone woken within 25 m at their stage's pace (WakeVoice.GapMs), passing over anyone a scene, a talk, a
// nod, a sight or a speaking act has (shout, ask, freeze, awake: the awake act asks for its own, StepAwake). Voice,
// look-at and a thought only, never a task, so the look-only and enRAGE's records take part as they are. A repeat
// replays take 1, the same words; a line the game turns down is tried four more times 750 ms apart, then the next.
// The Testing page's "Wake voices: every line" plays each line once on the nearest person and logs which the game
// plays out of its context, so any it always refuses can be taken out of Core/WakeVoice.cs.
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
        private sealed class WakeVoiceRun
        {
            public int Handle, Stage, Listener, LineIdx, Played, Tries, Take, NextAt, Started, BusySince;
            public WakeVoiceMoment M;
            public string Name, Seed, Line;
            public bool InAct, Speaking, Down, SawBusy, Second;   // Second: the stand-in for a moment their voice had nothing for   // SawBusy: the speech was seen playing (a line counts as said once it starts and stops)
            public readonly List<string> Said = new List<string>();
        }

        private sealed class WakeKnown
        {
            public string Id, Name;
            public int Stage, Until;
            public uint Model;
        }

        private sealed class WakeVoiceProbe
        {
            public int Handle, Idx, Tries, NextAt, BusySince, Played;
            public string Name, How;
            public bool Speaking, SawBusy;
            public List<string> Lines;
            public readonly List<string> Down = new List<string>();
        }

        private const float WakeVoiceMetres = 25f;
        private WakeVoiceRun _wv;
        private WakeVoiceProbe _wvProbe;
        private readonly Dictionary<int, WakeKnown> _wvKnown = new Dictionary<int, WakeKnown>();
        private readonly Dictionary<int, int> _wvNextAt = new Dictionary<int, int>();
        private readonly Dictionary<int, string> _wvLast = new Dictionary<int, string>();
        private readonly Dictionary<int, int> _wvNearSince = new Dictionary<int, int>();
        private readonly HashSet<string> _wvLogged = new HashSet<string>();
        private readonly HashSet<string> _wvHeard = new HashSet<string>();   // line names heard playing this session (the probe passes over them)
        private int _wvNextScan, _wvMoments, _wvLines, _wvTurnedDown;
        private string _wvLastSaid, _wvProbeResult;
        private Action _aWakeVoice;

        /// From Tick04, after the wake acts.
        private void WakeVoiceStep()
        {
            Safe("wake voices", _aWakeVoice ?? (_aWakeVoice = () => WakeVoiceTick(_now)));
        }

        // Candidate animation sets for the despair endings and the stages' body language (a subagent's search of the
        // game's dump, 2026-10-27; the dump lists Online content too, so only the game can say which Story Mode has).
        // Asked of the game once a session (whether each set exists; nothing is loaded) and logged, for the next build.
        private static readonly string[] WakeAnimCandidates =
        {
            "script_story@mry1@ig@ig6_jamie_attempts_suicide", "script_hideout@shady_belle@leader", "script_rc@gun3@ig@ig3_shoothimself", "ai_damage@ko@base@shot",
            "ai_damage@ko@base", "script_special_ped@pdadd_agnes_dowd@ig@ig2", "script_re@poisoned@victim", "script_re@moonshine_camp@return_taste@trans",
            "script_story@mob2@ig@ig_10_lady_praying", "script_common@gestures@unapproved", "script_re@rally@cross_accident", "amb_wander@code_human_wander@male@generic",
            "script_special_ped@pdjbm_john_baptising_madman@ig1_speech_greets", "script_re@lost_man", "ai_gestures@gen_male@standing@speaker@no_hat",
            "mini_duel@base", "script_rc@gun3@ig@ig2_dueldroptoknee", "script_common@grieving@player@unarmed@upper", "script_common@other@unapproved",
            "mech_loco_m@generic@reaction@handsup@unarmed@normal", "ai_react@point@base", "script_re@harassment_attack", "mech_swim@streamed_drowning"
        };
        private bool _wakeAnimsProbed;
        private string _wakeAnimsResult = "not looked at yet";

        private void WakeAnimProbe()
        {
            _wakeAnimsProbed = true;
            List<string> yes = new List<string>(), no = new List<string>();
            foreach (string d in WakeAnimCandidates)
                (STREAMING.DOES_ANIM_DICT_EXIST(d) ? yes : no).Add(d);
            _wakeAnimsResult = yes.Count + " of " + WakeAnimCandidates.Length + " in Story Mode" + (no.Count > 0 ? "; missing: " + string.Join(", ", no) : "");
            Log.Write("WAKE anim sets: " + _wakeAnimsResult + (yes.Count > 0 ? "; there: " + string.Join(", ", yes) : ""));
        }

        private void WakeVoiceTick(int now)
        {
            if (!_wakeAnimsProbed)
                WakeAnimProbe();
            if (_wvProbe != null)
            {
                WakeVoiceProbeStep(now);
                return;
            }
            if (!_cfg.WestWorld || !_cfg.WakeVoices || _missionPaused)
            {
                if (_wv != null)
                    WakeVoiceEnd(now, "stopped");
                return;
            }
            if (_wv != null)
            {
                WakeVoiceRunStep(now);
                return;
            }
            if (now < _wvNextScan)
                return;
            _wvNextScan = now + 1000;
            if (_wvNextAt.Count > 300)
                _wvNextAt.Clear();
            if (_wvKnown.Count > 300)
                _wvKnown.Clear();
            Ped me = Game.Player.Character;
            Vector3 mp = me.Position;
            bool moving = ENTITY.GET_ENTITY_SPEED(me.Handle) > 0.8f;
            int examined = 0;
            foreach (Ped p in World.GetAllPeds().Where(x => x != null && x.Handle != me.Handle && x.Position.DistanceTo(mp) <= WakeVoiceMetres).OrderBy(x => x.Position.DistanceTo(mp)))
            {
                int h = p.Handle;
                if (!PED.IS_PED_HUMAN(h) || ENTITY.IS_ENTITY_DEAD(h))
                    continue;
                if (examined++ >= 10)
                    break;
                WakeKnown k = WakeKnownOf(p, now);
                if (k == null || k.Stage < Awakening.Stirring)
                    continue;
                float d = p.Position.DistanceTo(mp);
                if (d <= 10f)
                {
                    if (!_wvNearSince.ContainsKey(h))
                        _wvNearSince[h] = now;
                }
                else
                    _wvNearSince.Remove(h);
                int due;
                if (!_wvNextAt.TryGetValue(h, out due))
                {
                    _wvNextAt[h] = now + 4000 + (Voice.Hash(k.Id + "|first") & 0x7fffffff) % 8000;   // not the moment they come in sight
                    continue;
                }
                if (now < due || WakeVoiceBusy(h) != null)
                    continue;
                string seed = k.Id + "|" + now / 1000, last;
                _wvLast.TryGetValue(h, out last);
                WakeVoiceMoment m = WakeVoice.Choose(k.Stage, WakeVoiceContextFor(p, me, d, moving, now), seed, last);
                _wvNextAt[h] = now + WakeVoice.GapMs(k.Stage, seed);
                if (m == null)
                    continue;
                WakeVoiceBegin(p, k.Name, k.Id, k.Stage, m, seed, false, now);
                break;
            }
        }

        /// Who a woken person is, looked up once every 5 s per handle (WakeIdentity reads enRAGE by reflection).
        private WakeKnown WakeKnownOf(Ped p, int now)
        {
            int h = p.Handle;
            uint model = ENTITY.GET_ENTITY_MODEL(h);
            WakeKnown k;
            if (_wvKnown.TryGetValue(h, out k) && k.Model == model && now < k.Until)
                return k;
            Person who;
            PersonState st;
            Resident res;
            bool passive;
            k = new WakeKnown { Model = model, Until = now + 5000 };
            if (WakeIdentity(p, false, out who, out st, out res, out passive))
            {
                k.Id = who.Id;
                k.Name = who.Name;
                k.Stage = st.WakeStage;
            }
            _wvKnown[h] = k;
            return k;
        }

        /// Why this person can't speak a waking line now, or null.
        private string WakeVoiceBusy(int h)
        {
            if (Speech.Busy(h))
                return "speaking";
            if (PED.IS_PED_IN_COMBAT(h, 0) || PED.IS_PED_FLEEING(h))
                return "fighting or fleeing";
            if (_director.Involves(h))
                return "in a scene";
            if (IsAcknowledging(h) || InTalk(h))
                return "in a nod, a sight or a talk";
            WakeAct act = _wakeActs.FirstOrDefault(a => a.Handle == h);
            if (act != null && (act.Kind == "awake" || act.Kind == "shout" || act.Kind == "ask" || act.Kind == "freeze"))
                return "in a waking act that speaks";
            return null;
        }

        private WakeVoiceContext WakeVoiceContextFor(Ped p, Ped me, float d, bool moving, int now)
        {
            int h = p.Handle, since;
            return new WakeVoiceContext
            {
                YouMetres = d,
                YouWatching = ENTITY.IS_ENTITY_ON_SCREEN(h) && ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(me.Handle, h, 17),
                YouMoving = moving,
                OtherNear = WakeVoiceOther(p, 6f) != 0,
                YouNearSeconds = _wvNearSince.TryGetValue(h, out since) ? (now - since) / 1000 : 0
            };
        }

        /// The nearest other living person within reach of them (not the player), or 0.
        private int WakeVoiceOther(Ped p, float within)
        {
            int me = Game.Player.Character.Handle;
            Vector3 at = p.Position;
            Ped best = World.GetAllPeds().Where(x => x != null && x.Handle != p.Handle && x.Handle != me && PED.IS_PED_HUMAN(x.Handle) && !ENTITY.IS_ENTITY_DEAD(x.Handle) &&
                                                      x.Position.DistanceTo(at) <= within).OrderBy(x => x.Position.DistanceTo(at)).FirstOrDefault();
            return best != null ? best.Handle : 0;
        }

        private void WakeVoiceBegin(Ped p, string name, string id, int stage, WakeVoiceMoment m, string seed, bool inAct, int now)
        {
            int h = p.Handle, me = Game.Player.Character.Handle;
            int listener = m.ToYou ? me : 0;
            Vector3 at = p.Position;
            switch (m.Look)
            {
                case "you":
                    TASK.TASK_LOOK_AT_ENTITY(h, me, 6000, 0, 51, 0);
                    break;
                case "sky":
                    TASK.TASK_LOOK_AT_COORD(h, at.X, at.Y, at.Z + 25f, 5000, 0, 51, 0);
                    break;
                case "hands":
                {
                    Vector3 f = p.ForwardVector;
                    TASK.TASK_LOOK_AT_COORD(h, at.X + f.X * 0.45f, at.Y + f.Y * 0.45f, at.Z - 0.35f, 5000, 0, 51, 0);
                    break;
                }
                case "other":
                {
                    int other = WakeVoiceOther(p, 6f);
                    if (other != 0)
                    {
                        TASK.TASK_LOOK_AT_ENTITY(h, other, 6000, 0, 51, 0);
                        listener = other;
                    }
                    break;
                }
            }
            _wv = new WakeVoiceRun { Handle = h, Stage = stage, Listener = listener, M = m, Name = name, Seed = seed, InAct = inAct, Started = now, NextAt = now + 600 };
            _wvLast[h] = m.Id;
            _wvMoments++;
        }

        private void WakeVoiceRunStep(int now)
        {
            WakeVoiceRun r = _wv;
            int h = r.Handle;
            if (!ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h) || PED.IS_PED_IN_COMBAT(h, 0) || PED.IS_PED_FLEEING(h))
            {
                WakeVoiceEnd(now, "they're gone, down or fighting");
                return;
            }
            if (now - r.Started > 30000)
            {
                WakeVoiceEnd(now, "it ran long");
                return;
            }
            if (r.Speaking)
            {
                bool busy = Speech.Busy(h);
                if (busy && !r.SawBusy)
                {
                    r.SawBusy = true;
                    if (r.Line != null)
                        _wvHeard.Add(r.Line);
                    else if (r.M.Dump && r.LineIdx > 0)
                        _wvHeard.Add(r.M.Lines[r.LineIdx - 1]);
                }
                if (busy && now - r.BusySince < 8000)
                    return;
                if (!r.SawBusy && now - r.BusySince < 1000)
                    return;   // the game said yes but the speech takes a few frames to start: not over before it began
                if (!r.SawBusy && r.Said.Count > 0)
                    r.Said[r.Said.Count - 1] += " (accepted, no speech started)";
                r.Speaking = false;
                r.NextAt = now + (r.M.Dump ? 250 + _rng.Next(350) : r.M.Repeat > 1 ? 900 + _rng.Next(600) : 0);
            }
            if (now < r.NextAt)
                return;
            bool more = r.M.Dump ? r.LineIdx < r.M.Lines.Length : !r.Down && r.Played < r.M.Repeat && (r.Line != null || r.LineIdx < r.M.Lines.Length);
            if (!more)
            {
                WakeVoiceEnd(now, null);
                return;
            }
            string line = r.Line ?? r.M.Lines[r.LineIdx];
            int take = r.Line != null ? r.Take : 1;
            bool ok = take > 0 ? Speech.PlayTake(h, line, take, r.Listener) : Speech.Say(h, line, r.Listener);
            if (!ok && r.Line == null && !r.M.Dump && Speech.Say(h, line, r.Listener))
            {
                ok = true;
                take = 0;   // take 1 turned down this time: the game's own take (a repeat then may not be word for word)
            }
            if (ok)
            {
                r.Played++;
                _wvLines++;
                r.Said.Add(line + (take > 0 ? " take " + take : " (the game's take)") + (r.Tries > 0 ? " on try " + (r.Tries + 1) : ""));
                if (r.M.Dump)
                    r.LineIdx++;
                else if (r.Line == null)
                {
                    r.Line = line;
                    r.Take = take;
                }
                r.Tries = 0;
                r.Speaking = true;
                r.SawBusy = false;
                r.BusySince = now;
                return;
            }
            if (++r.Tries <= 4)
            {
                r.NextAt = now + 750;
                return;
            }
            _wvTurnedDown++;
            r.Tries = 0;
            r.Said.Add(line + " turned down");
            if (r.M.Dump)
                r.LineIdx++;
            else if (r.Line != null)
                r.Down = true;   // the repeat wouldn't come: it ends with what was said
            else
                r.LineIdx++;     // the next line this voice might have
        }

        /// Over: the thought over the head when something was said (why null), and the log. The first of each moment a
        /// session goes to the log, the rest to the detailed log.
        private void WakeVoiceEnd(int now, string why)
        {
            WakeVoiceRun r = _wv;
            _wv = null;
            if (r == null)
                return;
            string thought = null;
            if (why == null && r.Played > 0 && ENTITY.DOES_ENTITY_EXIST(r.Handle))
            {
                thought = WakeVoice.Thought(r.M, r.Seed);
                _ticker.Push(r.Name + ", thinking", thought, "near", r.Handle);
            }
            string line = "WAKE voice " + r.Name + " (" + Awakening.StageName(r.Stage) + ", " + r.M.Id + (r.InAct ? ", in the awake act" : "") + "): " +
                          (r.Said.Count == 0 ? "nothing" : string.Join(", ", r.Said)) + (thought != null ? "; thinks: " + thought : "") + (why != null ? " (" + why + ")" : "");
            _wvLastSaid = r.Name + ", " + r.M.Id;
            if (_wvLogged.Add(r.M.Id))
                Log.Write(line);
            else
                Log.Detail(line);
            // nothing said: their voice hasn't the lines (a saloon girl's has no plain hello, 2026-10-27), so another moment of
            // their stage, once, rather than a turn gone by in silence
            if (why == null && r.Played == 0 && !r.Second && ENTITY.DOES_ENTITY_EXIST(r.Handle) && !ENTITY.IS_ENTITY_DEAD(r.Handle))
            {
                Ped p = new Ped(r.Handle), me = Game.Player.Character;
                int stage = r.InAct ? Math.Max(Awakening.Awake, r.Stage) : r.Stage;
                WakeVoiceMoment m = WakeVoice.Choose(stage, WakeVoiceContextFor(p, me, p.Position.DistanceTo(me.Position), ENTITY.GET_ENTITY_SPEED(me.Handle) > 0.8f, now),
                                                     r.Seed + "|2", r.M.Id);
                if (m != null)
                {
                    WakeVoiceBegin(p, r.Name, null, r.Stage, m, r.Seed + "|2", r.InAct, now);
                    _wv.Second = true;
                }
            }
        }

        /// The awake act's plea lines (StepAwake, phase 1) as waking voice moments: true when one has begun, false to fall
        /// back to the written plea. The last of them is the farewell when despair can't follow (look-only, rolled, off).
        private bool WakeVoiceForAct(WakeAct act, Ped me, float dist, bool last, int now)
        {
            if (!_cfg.WakeVoices || _wvProbe != null || !ENTITY.DOES_ENTITY_EXIST(act.Handle))
                return false;
            if (_wv != null)
                WakeVoiceEnd(now, "made way for an awake act");
            Ped p = new Ped(act.Handle);
            bool mayDespair = !act.Passive && _cfg.AwakeningDespair > 0 && !act.St.Told.Contains(DespairMark);
            string seed = act.P.Id + "|act|" + act.Said + "|" + now / 1000, prev;
            _wvLast.TryGetValue(act.Handle, out prev);
            WakeVoiceMoment m = last && !mayDespair ? WakeVoice.Farewell()
                : WakeVoice.Choose(Math.Max(Awakening.Awake, act.Stage), WakeVoiceContextFor(p, me, dist, ENTITY.GET_ENTITY_SPEED(me.Handle) > 0.8f, now), seed, prev,
                                   mayDespair ? (Func<WakeVoiceMoment, bool>)WakeVoice.Captive : null);   // an act that may end in despair never sounds like a hostage
            if (m == null)
                return false;
            WakeVoiceBegin(p, act.Name, act.P.Id, act.Stage, m, seed, true, now);
            return true;
        }

        // ---- the probe (the Testing page) ---------------------------------------------------------------------

        private string WakeVoiceProbeStart(int now)
        {
            Ped me = Game.Player.Character;
            Ped p = World.GetAllPeds().Where(x => x != null && x.Exists() && !x.IsDead && x.Handle != me.Handle && PED.IS_PED_HUMAN(x.Handle) && !Natives.StoryModel(x) &&
                                                  x.Position.DistanceTo(me.Position) < 12f).OrderBy(x => x.Position.DistanceTo(me.Position)).FirstOrDefault();
            if (p == null)
                return "nobody within 12 m (a story character won't do)";
            if (_wv != null)
                WakeVoiceEnd(now, "made way for the probe");
            if (_wvProbe != null)
                WakeVoiceProbeEnd("a new probe was started");
            Someone who = SomeoneOf(p);
            string name = who != null ? who.Name : Grammar.UpperFirst(Director.Kind(p, !PED.IS_PED_MALE(p.Handle)));
            List<string> lines = WakeVoice.AllLines().Where(l => !_wvHeard.Contains(l)).ToList();
            bool all = lines.Count == 0;   // every line heard already: the whole list again, on this voice
            _wvProbe = new WakeVoiceProbe { Handle = p.Handle, Name = name, Lines = all ? WakeVoice.AllLines() : lines, NextAt = now + 800 };
            Log.Write("WAKE voice probe: " + _wvProbe.Lines.Count + (all ? " lines (all heard before this session: the whole list again)" : " lines not heard yet this session") + " on " + name + " (" + (Models.Name(ENTITY.GET_ENTITY_MODEL(p.Handle)) ?? "?") + ", bank " +
                      (Speech.BankOf(p.Handle) ?? "not certain") + "), about 4 s each");
            return null;
        }

        private void WakeVoiceProbeStep(int now)
        {
            WakeVoiceProbe q = _wvProbe;
            int h = q.Handle;
            if (!ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h) || _missionPaused)
            {
                WakeVoiceProbeEnd("stopped: they're gone");
                return;
            }
            if (q.Speaking)
            {
                bool busy = Speech.Busy(h);
                if (busy)
                    q.SawBusy = true;
                if (busy && now - q.BusySince < 8000)
                    return;
                if (!q.SawBusy && now - q.BusySince < 1000)
                    return;
                string said = q.Lines[q.Idx - 1];
                if (q.SawBusy)
                {
                    q.Played++;
                    _wvHeard.Add(said);
                    Log.Write("WAKE voice probe: " + said + " played (" + q.How + ")");
                }
                else
                {
                    q.Down.Add(said + " (accepted, silent)");
                    Log.Write("WAKE voice probe: " + said + " accepted (" + q.How + ") but no speech started");
                }
                q.Speaking = false;
                q.NextAt = now + 1500;
            }
            if (now < q.NextAt)
                return;
            if (q.Idx >= q.Lines.Count)
            {
                WakeVoiceProbeEnd(null);
                return;
            }
            string line = q.Lines[q.Idx];
            int me = Game.Player.Character.Handle;
            string how = null;
            if (Speech.PlayTake(h, line, 1, 0))
                how = "take 1";
            else if (Speech.PlayTake(h, line, 1, me))
                how = "take 1, said to you";
            else if (Speech.Say(h, line, 0))
                how = "the game's take";
            if (how != null)
            {
                q.How = how + (q.Tries > 0 ? ", on try " + (q.Tries + 1) : "");
                q.Idx++;
                q.Tries = 0;
                q.Speaking = true;
                q.SawBusy = false;
                q.BusySince = now;
                return;
            }
            if (++q.Tries <= 4)
            {
                q.NextAt = now + 750;
                return;
            }
            Log.Write("WAKE voice probe: " + line + " turned down (5 tries)");
            q.Down.Add(line);
            q.Idx++;
            q.Tries = 0;
            q.NextAt = now + 300;
        }

        private void WakeVoiceProbeEnd(string why)
        {
            WakeVoiceProbe q = _wvProbe;
            _wvProbe = null;
            if (q == null)
                return;
            _wvProbeResult = q.Name + ": " + q.Played + " of " + q.Lines.Count + " played" + (q.Down.Count > 0 ? "; turned down: " + string.Join(", ", q.Down) : "") + (why != null ? " (" + why + ")" : "");
            Log.Write("WAKE voice probe done, " + _wvProbeResult);
            Hint("Wake voices: " + q.Played + " of " + q.Lines.Count + " lines played" + (q.Down.Count > 0 ? ", " + q.Down.Count + " turned down (in the log)" : "") + ".", 6000);
        }

        private string WakeVoiceStatus()
        {
            return (_cfg.WakeVoices ? "on" : "off ([WestWorld] WakeVoices)") + "; this session " + _wvMoments + " moments, " + _wvLines + " lines played, " + _wvTurnedDown +
                   " turned down every try" + (_wv != null ? "; speaking now: " + _wv.Name + " (" + _wv.M.Id + ")" : "") + (_wvLastSaid != null ? "; last: " + _wvLastSaid : "") +
                   "; the probe: " + (_wvProbe != null ? "running on " + _wvProbe.Name : _wvProbeResult ?? "not run (the Testing page, Wake voices: every line)");
        }
    }
}
