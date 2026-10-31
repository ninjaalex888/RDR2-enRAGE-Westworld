// The waking held, and stranger (Lex, 2026-10-31, in game: the stages "don't do those animations, and it seems they swap
// back to the game controlling them"). Four parts, each its own [WestWorld] switch; the words and choices are
// Core/WakeOdd.cs.
//   WakeHold      from stage 2, an act that finishes runs straight into the next (Core WakeOdd.NextKind) instead of
//                 handing them back, for WakeHoldMinutes; then they're let go for a minute. Let go at once for a
//                 fight, a gun drawn or fired, the law after you, a lawman's work, a scene, or you more than 30 m off.
//   WakeGestures  the stage clips the Testing page's "Wake stage gestures" row plays (WestworldScript.AnimTest.cs
//                 StageClips), now used in the acts: upper-body ones over standing and pleading, the whole-body
//                 mission clips only while they stand. A clip that doesn't start twice, or carries them more than
//                 1.2 m, is dropped for the session (WAKE gesture dropped in the log).
//   WakeStrange   stages 3 and 4, someone near: close up and staring, a hand waved before the face, following step
//                 for step, "you're not real", preaching to whoever is about. The other answers by their temper
//                 (walks off, curses, laughs, takes fright) in their own voice, and thinks something of it.
//   strip         WakeStripChance percent of the awake, once a person ("wake:strip" in their state): every piece of
//                 clothing off (REMOVE_TAG_FROM_META_PED by category, as the clothing mods do), never enRAGE's
//                 people (it knows them by their clothes). Up to three who see it react as to a naked man (Sights),
//                 with their own thought. The game dresses them again when it next makes them.
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
        private const int HoldGapMs = 60000;
        private const string StripMark = "wake:strip";
        // held back (2026-10-31): the first strip seen in game froze the game two seconds after the Testing row fired it
        // (the log stops as REMOVE_TAG_FROM_META_PED ran on an ambient man in Saint Denis). Nothing strips, whatever
        // WakeStripChance says, until the cause is found and it's seen working.
        private static readonly bool StripHeldBack = true;
        private const ulong RemoveTagFromMetaPed = 0xD710A5007C2AC539;   // REMOVE_TAG_FROM_META_PED(ped, category, 0), by hash like the cursor
        private const ulong UpdatePedVariation = 0xCC8CA3E88256E58F;     // _UPDATE_PED_VARIATION(ped, false, true, true, true, false)
        private readonly HashSet<string> _gestBad = new HashSet<string>();
        private readonly HashSet<string> _gestDicts = new HashSet<string>();
        private int _holds, _holdLinks, _gestPlayed, _gestFailed, _strangeActs, _strips, _strangeTest;
        private readonly Dictionary<string, int> _answers = new Dictionary<string, int>();
        private string _holdLast, _strangeLast, _stripLast;

        // the categories that come off (Sights' tables: the torso, the legs, and what isn't the body); the body, hair and
        // beard stay, and anything the tables don't know is left alone
        private static readonly HashSet<uint> BodyParts = new HashSet<uint>
        {
            0x378AD10C, 0x864B03AE, 0xEA24B45E, 0x96EDAE5C, 0x0B3966C9, 0x823687F5, 0x0EAF8BBE, 0x15D3C7F2, 0xB6B63737, 0xF8016BCA, 0xECC8B25A, 0x8E84A2AA
        };

        private static bool OddKind(string kind)
        {
            return kind == "dazed" || kind == "drift" || kind == "strip" || kind == "lashout" || WakeOdd.StrangeKinds.Contains(kind);
        }

        // ---- the hold -------------------------------------------------------------------------------------------

        /// Why a finished act may not run into another now, or null.
        private string HoldWhyNot(WakeAct act, Ped me, int now)
        {
            if (!_cfg.WakeHold) return "off";
            if (act.Kind == "lashout") return "lashed out";
            if (act.Passive) return "only looks and speaks";
            if (act.Stage < Awakening.Questioning) return "only stirring";
            if (act.Kind == "awake" && (act.Ending == "despair" || act.Ending == "escape")) return "gone their own way";
            if (act.HoldUntil > 0 && now >= act.HoldUntil) return "held long enough";
            int h = act.Handle;
            Ped ped = new Ped(h);
            if (!CanDirect(ped)) return "busy with something of their own";
            if (_missionPaused || _director.Involves(h)) return "a scene or a mission";
            if (PLAYER.GET_PLAYER_WANTED_LEVEL(Game.Player.Handle) > 0 || LawBusy(ped)) return "the law";
            if (PED.IS_PED_SHOOTING(me.Handle) || PLAYER.IS_PLAYER_FREE_AIMING(Game.Player.Handle)) return "a gun out";
            if (ped.Position.DistanceTo(me.Position) > 30f) return "you went off";
            return null;
        }

        /// A finished act: the next link of the hold put in its place, or false (handed back as before).
        private bool WakeHoldNext(int i, WakeAct act, Ped me, int now)
        {
            string why = HoldWhyNot(act, me, now);
            if (why != null)
            {
                act.HoldWhy = why;
                return false;
            }
            int h = act.Handle;
            bool sitting = PED.IS_PED_USING_ANY_SCENARIO(h);
            int r = _rng.Next(100), s = _rng.Next(100);
            string kind;
            if (!act.LashRolled && act.Stage >= Awakening.Unravelling)
            {
                act.LashRolled = true;   // once a hold: does this one end with them turning on someone (WakeLashOut)?
                if (WakeOdd.LashesOut(_cfg.WakeLashOut, act.Stage, act.P.Personality, act.P.Id + "|" + now / 60000))
                    act.LashAt = act.Links + 2 + _rng.Next(4);
            }
            if (act.LashAt > 0 && act.Links + 1 >= act.LashAt && !sitting)
                kind = "lashout";
            else if (act.Stage >= Awakening.Awake && !act.Stripped && !act.St.Has(StripMark) && _bridge.RecordAt(h) == null && !sitting && s < 30 &&
                !StripHeldBack && WakeOdd.Strips(_cfg.WakeStripChance, act.P.Id))
                kind = "strip";
            else
                kind = WakeOdd.NextKind(act.Stage, sitting, _cfg.WakeStrange && OtherFor(act, me) != null, r, s);
            if (kind == act.Kind && kind != "dazed")
                kind = "dazed";   // not the same act twice running
            WakeAct next = NextLink(act, kind, now);
            if (!sitting)
                TASK.CLEAR_PED_TASKS(h, true, false);
            if (!BeginAct(next, new Ped(h), me, now))
            {
                next.Kind = "dazed";
                BeginAct(next, new Ped(h), me, now);
            }
            if (!OddKind(next.Kind))
                Think(next, WakeBeats.Opening(next.Kind, next.Stage, next.P, next.St, _engine.Player, next.P.Id + "|" + next.Kind + "|" + now / 1000));
            _wakeActs[i] = next;
            if (act.Links == 0)
            {
                _holds++;
                Log.Write("WAKE hold: " + act.Name + " (" + Awakening.StageName(act.Stage) + ") stays strange after " + act.Kind + ": " + next.Kind + ", up to " + _cfg.WakeHoldMinutes + " min");
            }
            else
                Log.Detail("WAKE hold: " + act.Name + " link " + next.Links + ": " + next.Kind);
            _holdLinks++;
            _holdLast = act.Name + " " + next.Kind;
            return true;
        }

        /// Dangers that let a held person go mid-act: their events are blocked, so the game won't move them for these itself.
        private string HoldDanger(WakeAct act, Ped me)
        {
            if (PLAYER.GET_PLAYER_WANTED_LEVEL(Game.Player.Handle) > 0) return "the law is after you";
            if (PLAYER.IS_PLAYER_FREE_AIMING(Game.Player.Handle) || PED.IS_PED_SHOOTING(me.Handle)) return "a gun out";
            Vector3 at = new Ped(act.Handle).Position;
            if (PED.IS_ANY_PED_SHOOTING_IN_AREA(at.X - 40f, at.Y - 40f, at.Z - 15f, at.X + 40f, at.Y + 40f, at.Z + 15f, false, false)) return "gunfire near";
            if (_director.Involves(act.Handle) || _missionPaused) return "a scene or a mission";
            return null;
        }

        /// A hold over, however it ended: a minute before they act again, and one line for it.
        private void HoldEnded(WakeAct act, string why)
        {
            int now = Game.GameTime;
            _wakeCool[act.Handle] = now + HoldGapMs;
            Log.Write("WAKE hold ended: " + act.Name + " after " + act.Links + " link" + (act.Links == 1 ? "" : "s") + ", " + (now - act.HoldStart) / 1000 + " s (" + (act.HoldWhy ?? why ?? "stopped") + ")");
        }

        /// The next act of a hold, for the same person: their hold kept (the events still blocked), everything else fresh.
        private WakeAct NextLink(WakeAct act, string kind, int now)
        {
            int h = act.Handle;
            return new WakeAct
            {
                Handle = h, Kind = kind, Stage = act.Stage, Started = now, NextStep = now + 400, Name = act.Name, P = act.P, St = act.St, Res = act.Res, Passive = false,
                A = new Ped(h).Position, Blocked = act.Blocked, Stripped = act.Stripped, Links = act.Links + 1, LashRolled = act.LashRolled, LashAt = act.LashAt,
                HoldStart = act.Links == 0 ? act.Started : act.HoldStart,
                HoldUntil = act.HoldUntil > 0 ? act.HoldUntil : now + _cfg.WakeHoldMinutes * 60000
            };
        }

        // ---- the new acts ---------------------------------------------------------------------------------------

        private bool BeginOdd(WakeAct act, Ped ped, Ped me, int now)
        {
            int h = act.Handle;
            string seed = act.P.Id + "|" + act.Kind + "|" + now / 1000;
            switch (act.Kind)
            {
                case "dazed":
                {
                    Take(act);
                    if (!PED.IS_PED_USING_ANY_SCENARIO(h))
                        TASK.TASK_STAND_STILL(h, 14000);
                    Vector3 at = DazedPoint(act, ped.Position);
                    LookAtCoord(h, at.X, at.Y, at.Z, 6000);
                    act.Until = now + 9000 + _rng.Next(6000);
                    act.NextMutter = now + 2500 + _rng.Next(2500);
                    WakeGesture(act, now + 1200, !PED.IS_PED_USING_ANY_SCENARIO(h));
                    return true;
                }
                case "drift":
                {
                    Vector3 goal = PointAround(act.A, 5f + (float)_rng.NextDouble() * 6f, act.A.Z);
                    if (goal.DistanceTo(act.A) < 2f)
                        return false;
                    Take(act);
                    act.Goal = goal;
                    TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(h, goal.X, goal.Y, goal.Z, act.Stage >= Awakening.Unravelling ? 0.9f : 0.75f, 15000, 1f, 0, 0f);
                    act.Until = now + 16000;
                    if (_rng.Next(100) < 45)
                        Think(act, WakeOdd.Drift(seed));
                    return true;
                }
                case "strip":
                {
                    if (StripHeldBack)
                        return false;   // held back: it froze the game
                    Take(act);
                    TASK.TASK_STAND_STILL(h, 9000);
                    LookAtCoord(h, ped.Position.X, ped.Position.Y, ped.Position.Z - 1f, 3000);   // down at themselves
                    act.Until = now + 9000;
                    act.Phase = 0;
                    act.PhaseUntil = now + 1800;
                    return true;
                }
                case "lashout":
                    return BeginLashOut(act, ped, me, now);
                case "preach":
                {
                    Take(act);
                    TASK.TASK_STAND_STILL(h, 22000);
                    act.Until = now + 20000;
                    act.Lines = 3;
                    act.PhaseUntil = now + 600;
                    Think(act, WakeOdd.ThoughtOf("preach", seed));
                    return true;
                }
                default:   // closeup, wave, mimic, notreal: someone near, gone up to
                {
                    Ped other = OtherFor(act, me);
                    if (other == null)
                        return false;
                    Take(act);
                    act.Other = other.Handle;
                    if (act.Kind == "mimic")
                    {
                        TASK.TASK_FOLLOW_TO_OFFSET_OF_ENTITY(h, act.Other, 0f, -1.4f, 0f, 1.0f, -1, 0.6f, true, false, false, false, false, false);
                        act.Until = now + 16000;
                        act.PhaseUntil = now + 13000;   // then the other turns round
                    }
                    else
                    {
                        TASK.TASK_GO_TO_ENTITY(h, act.Other, 12000, act.Kind == "closeup" ? 0.8f : 1.2f, 1.0f, 0f, 0);
                        act.Until = now + 20000;
                    }
                    Think(act, WakeOdd.ThoughtOf(act.Kind, seed));
                    return true;
                }
            }
        }

        private void StepOdd(WakeAct act, Ped me, Vector3 pos, float dist, int now)
        {
            int h = act.Handle;
            string seed = act.P.Id + "|" + act.Kind + "|" + act.Links + "|" + now / 1000;
            switch (act.Kind)
            {
                case "dazed":
                    if (act.NextMutter > 0 && now >= act.NextMutter)
                    {
                        act.NextMutter = 0;
                        string m = WakeOdd.Mutter(act.Stage, seed);
                        if (_rng.Next(100) < 55)
                        {
                            Func<string> v = Speech.When(h, "trail");   // a sound under the breath, and the words in the box under the top one
                            if (v != null && !Speech.Busy(h))
                                v();
                            SpeechCaption(Person.FirstOf(act.Name) + ", under " + (act.P.Woman ? "her" : "his") + " breath", m, h, Math.Min(6000, 1800 + m.Length * 50));
                        }
                        else
                            Think(act, m);
                    }
                    break;
                case "drift":
                    if (pos.DistanceTo(act.Goal) < 1.5f && act.Phase == 0)
                    {
                        act.Phase = 1;
                        TASK.TASK_STAND_STILL(h, 4000);
                        LookAtCoord(h, pos.X, pos.Y, pos.Z + 20f, 3000);
                        act.Until = Math.Min(act.Until, now + 4000);
                        WakeGesture(act, now + 600, false);
                    }
                    break;
                case "strip":
                    if (act.Phase == 0 && now >= act.PhaseUntil)
                    {
                        act.Phase = 1;
                        WakeStrip(act, me, now);
                    }
                    break;
                case "lashout":
                    StepLashOut(act, me, now);
                    break;
                case "preach":
                    if (act.Said < act.Lines && now >= act.PhaseUntil)
                    {
                        string line = WakeOdd.SaidTo("preach", seed + act.Said);
                        act.Said++;
                        ActSay(act, line, me, "angry");
                        act.PhaseUntil = now + 5000 + (line != null ? line.Length * 40 : 0);
                        WakeGesture(act, now + 300, true);
                        if (act.Said == 1)
                            PreachHeard(act, pos, now);
                    }
                    break;
                default:
                    StepStrange(act, me, pos, now, seed);
                    break;
            }
        }

        /// Up close to someone: once there, the act (the stare, the wave, the words), then the other's answer.
        private void StepStrange(WakeAct act, Ped me, Vector3 pos, int now, string seed)
        {
            int h = act.Handle;
            if (!ENTITY.DOES_ENTITY_EXIST(act.Other) || ENTITY.IS_ENTITY_DEAD(act.Other))
            {
                act.Until = now;
                return;
            }
            Ped other = new Ped(act.Other);
            float d = pos.DistanceTo(other.Position);
            if (act.Phase == 0)
            {
                bool there = act.Kind == "mimic" ? now >= act.PhaseUntil : d < (act.Kind == "closeup" ? 1.3f : 1.8f);
                if (!there)
                {
                    if (act.Kind != "mimic" && now - act.Started > 12000)
                        act.Until = now;   // never got there
                    return;
                }
                act.Phase = 1;
                if (act.Kind != "mimic")
                {
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, act.Other, 2000, 0f, 0f, 0f);
                    TASK.TASK_LOOK_AT_ENTITY(h, act.Other, 8000, 0, 51, 0);
                }
                else
                {
                    TASK.CLEAR_PED_TASKS(h, true, false);
                    TASK.TASK_STAND_STILL(h, 5000);
                }
                if (act.Kind == "wave")
                    WakeGesture(act, now + 500, true, "neutral_get_attention_l_001");
                string said = WakeOdd.SaidTo(act.Kind, seed);
                if (said != null && (act.Kind != "closeup" || _rng.Next(100) < 60))
                    ActSay(act, said, me, act.Kind == "notreal" ? "plead" : "chat");
                act.AnswerAt = now + (act.Kind == "closeup" ? 3500 : 2200);
                act.Until = now + 9000;
                _strangeActs++;
                _strangeLast = act.Name + " " + act.Kind;
                Log.Write("WAKE strange: " + act.Name + " (" + Awakening.StageName(act.Stage) + ") " + StrangeWhat(act.Kind) + " " + OtherName(other) + (said != null ? ": " + said : ""));
                return;
            }
            if (act.Phase == 1 && act.AnswerAt > 0 && now >= act.AnswerAt)
            {
                act.AnswerAt = 0;
                act.Phase = 2;
                Answer(act, other, now);
            }
        }

        private static string StrangeWhat(string kind)
        {
            switch (kind)
            {
                case "closeup": return "stands right up close and stares at";
                case "wave": return "waves a hand before the face of";
                case "mimic": return "follows step for step behind";
                case "notreal": return "tells someone they aren't real:";
                default: return kind;
            }
        }

        private string OtherName(Ped p)
        {
            string id, name, temper;
            return LastingId(p, out id, out name, out temper) && name != null ? name : Director.Kind(p, !PED.IS_PED_MALE(p.Handle));
        }

        /// The one they did it to answers, by their temper: a look, their own line, a thought, and off if they go.
        private void Answer(WakeAct act, Ped other, int now)
        {
            int o = other.Handle;
            if (MayDirectWhyNot(other) != null || _director.Involves(o))
                return;
            string id, name, temper;
            bool known = LastingId(other, out id, out name, out temper);
            string seed = (known ? id : "h:" + o) + "|" + act.P.Id + "|" + now / 1000;
            if (!known)
                temper = Sights.StrangerTemper(seed);
            bool woman = !PED.IS_PED_MALE(o);
            string answer = WakeOdd.Answer(temper, woman, seed);
            TASK.TASK_LOOK_AT_ENTITY(o, act.Handle, 3000, 0, 51, 0);
            string line = null, take2, words;
            int take;
            bool spoke = !Speech.Busy(o) && Speech.PlayFirst(o, WakeOdd.AnswerVoice(answer), act.Handle, out line, out take, out words);
            take2 = spoke ? line : "nothing said";
            string thought = WakeOdd.AnswerThoughtOf(answer, seed);
            if (thought != null)
                _ticker.Push((known && name != null ? name : Grammar.UpperFirst(Director.Kind(other, woman))) + ", thinking", thought, "near", o);
            if ((answer == "walk" || answer == "fright") && !PED.IS_PED_USING_ANY_SCENARIO(o))
            {
                Vector3 away = other.Position - new Ped(act.Handle).Position;
                away.Z = 0f;
                Vector3 to = other.Position + (away.Length() < 0.5f ? new Vector3(0f, 1f, 0f) : away.Normalized) * 16f;
                TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(o, to.X, to.Y, to.Z, answer == "fright" ? 2.2f : 1.3f, 12000, 1f, 0, 0f);
            }
            int n;
            _answers.TryGetValue(answer, out n);
            _answers[answer] = n + 1;
            Log.Write("WAKE strange: " + (known && name != null ? name : Director.Kind(other, woman)) + " answers " + act.Name + ": " + answer + " (" + (temper ?? "plain") + "; " + take2 + ")");
        }

        /// The preacher's first words: up to three within 14 m who can see them look over, and each answers.
        private void PreachHeard(WakeAct act, Vector3 pos, int now)
        {
            Ped me = Game.Player.Character;
            int n = 0;
            foreach (Ped p in World.GetAllPeds().Where(x => x != null && x.Exists() && !x.IsDead && x.Handle != act.Handle && x.Handle != me.Handle && x.Position.DistanceTo(pos) < 14f)
                                                .OrderBy(x => x.Position.DistanceTo(pos)))
            {
                if (SightCant(p, me) != null || !ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(p.Handle, act.Handle, 17))
                    continue;
                Answer(act, p, now + n);
                if (++n >= 3)
                    break;
            }
            _strangeActs++;
            _strangeLast = act.Name + " preach (" + n + " heard)";
            Log.Write("WAKE strange: " + act.Name + " (" + Awakening.StageName(act.Stage) + ") preaches to whoever is about; " + n + " heard it");
        }

        /// Someone near enough to bother: grown, free, not the law, not a counter's keeper, not in anything of ours.
        private Ped OtherFor(WakeAct act, Ped me)
        {
            Vector3 at = new Ped(act.Handle).Position;
            foreach (Ped p in World.GetAllPeds().Where(x => x != null && x.Exists() && !x.IsDead && x.Handle != act.Handle && x.Handle != me.Handle && x.Position.DistanceTo(at) < 14f)
                                                .OrderBy(x => x.Position.DistanceTo(at)))
            {
                int h = p.Handle;
                if (!PED.IS_PED_HUMAN(h) || !IsGrownUp(p) || Natives.IsLaw(p) || Natives.StoryModel(p) || PostKindOf(p) != null || PED.IS_PED_ON_MOUNT(h) ||
                    PED.IS_PED_IN_ANY_VEHICLE(h, false) || MayDirectWhyNot(p) != null || NotFree(h) || Speech.Busy(h))
                    continue;
                return p;
            }
            return null;
        }

        private Vector3 DazedPoint(WakeAct act, Vector3 pos)
        {
            double heading = ENTITY.GET_ENTITY_HEADING(act.Handle) * Math.PI / 180.0;
            Vector3 fwd = new Vector3((float)-Math.Sin(heading), (float)Math.Cos(heading), 0f);
            switch (_rng.Next(4))
            {
                case 0: return pos + new Vector3(0f, 0f, 30f) + fwd * 3f;    // the sky
                case 1: return pos + fwd * 0.6f - new Vector3(0f, 0f, 1.2f);  // their hands, the ground
                case 2: return pos + fwd * 25f + new Vector3(0f, 0f, 1.5f);   // far off, at nothing
                default: return pos + new Vector3(fwd.Y, -fwd.X, 0f) * 6f + new Vector3(0f, 0f, 1.6f);
            }
        }

        // ---- turning on someone ---------------------------------------------------------------------------------

        private readonly Dictionary<int, int> _wakeAttackers = new Dictionary<int, int>();   // someone waking who turned on another -> whom (fair game: WestworldScript.Watch.cs)
        private int _lashOuts, _lashAtYou;
        private string _lashLast;

        /// The hold's breaking point (WakeLashOut): at you or at someone near, with their fists while unravelling, their own
        /// gun when awake and carrying one. Their events unblocked: from here the game's own fighting takes it.
        private bool BeginLashOut(WakeAct act, Ped ped, Ped me, int now)
        {
            int h = act.Handle;
            string seed = act.P.Id + "|lash|" + now / 1000;
            bool test = _lashTestAtYou != 0;
            bool atYou = test ? _lashTestAtYou > 0 : WakeOdd.AtYou(act.P.Personality, seed);
            _lashTestAtYou = 0;
            Ped other = atYou ? null : OtherFor(act, me);
            if (other == null)
            {
                if (ped.Position.DistanceTo(me.Position) > 20f || PLAYER.IS_PLAYER_DEAD(Game.Player.Handle))
                    return false;
                atYou = true;   // nobody else to hand: you
            }
            int target = atYou ? me.Handle : other.Handle;
            uint gun = WEAPON.GET_BEST_PED_WEAPON(h, false, false);
            bool armed = act.Stage >= Awakening.Awake && gun != 0 && gun != Natives.Hash("WEAPON_UNARMED");
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, false);
            act.Blocked = false;
            if (armed)
            {
                WEAPON.SET_CURRENT_PED_WEAPON(h, gun, true, 0, false, false);
                PED.SET_PED_COMBAT_ATTRIBUTES(h, 5, false);
            }
            else
            {
                WEAPON.SET_CURRENT_PED_WEAPON(h, Natives.Hash("WEAPON_UNARMED"), true, 0, false, false);
                PED.SET_PED_COMBAT_ATTRIBUTES(h, 5, true);   // fists
                PED.SET_PED_COMBAT_ATTRIBUTES(h, 46, true);
            }
            TASK.TASK_COMBAT_PED(h, target, 0, 16);
            act.Target = target;
            if (!atYou)
            {
                act.Other = target;
                _wakeAttackers[h] = target;
            }
            act.Until = now + 45000;
            act.PhaseUntil = now + 5000;   // a moment for the fight to take before "calmed" can count
            string said = WakeOdd.LashSaid(atYou, seed);
            ActSay(act, said, me, "angry", atYou);
            Think(act, WakeOdd.LashThought(seed));
            _lashOuts++;
            if (atYou)
                _lashAtYou++;
            string whom = atYou ? "you" : OtherName(other);
            _lashLast = act.Name + " at " + whom + (armed ? " (a gun)" : " (fists)");
            Log.Write("WAKE lash out: " + act.Name + " (" + Awakening.StageName(act.Stage) + (act.P.Personality != null ? ", " + act.P.Personality : "") + ") turns on " + whom +
                      (armed ? " with their gun" : " with their fists") + ": " + said + (atYou ? "" : " (fair game to stop)"));
            return true;
        }

        /// Over when the one they went for is down or gone, when they've stopped fighting, or after 45 s.
        private void StepLashOut(WakeAct act, Ped me, int now)
        {
            int h = act.Handle;
            if (act.Target == 0 || !ENTITY.DOES_ENTITY_EXIST(act.Target) || ENTITY.IS_ENTITY_DEAD(act.Target))
            {
                act.Until = now;
                return;
            }
            if (now >= act.PhaseUntil && !PED.IS_PED_IN_COMBAT(h, 0))
                act.Until = now;   // it went out of them
        }

        /// No longer fair game; still fighting, they're left to it (their orders kept) rather than stopped mid-swing.
        private void LashOutEnded(WakeAct act)
        {
            int h = act.Handle;
            _wakeAttackers.Remove(h);
            if (ENTITY.DOES_ENTITY_EXIST(h) && !ENTITY.IS_ENTITY_DEAD(h) && PED.IS_PED_IN_COMBAT(h, 0))
                act.LeaveOrders = true;
            if (_wakeAttackers.Count > 20)
                foreach (int k in _wakeAttackers.Keys.Where(k => !_wakeActs.Any(a => a.Handle == k)).ToList())
                    _wakeAttackers.Remove(k);
        }

        private int _lashTestAtYou;   // the Testing row's turn: 1 at you, -1 at someone else, 0 the roll

        // ---- stripping bare ---------------------------------------------------------------------------------------

        private void WakeStrip(WakeAct act, Ped me, int now)
        {
            if (StripHeldBack)
                return;
            int h = act.Handle;
            act.St.Mark(StripMark);
            act.Stripped = true;
            string before, after;
            DressOf(h, out before);
            var cats = new List<uint>();
            int n = PED._GET_NUM_COMPONENTS_IN_PED(h);
            for (int i = 0; i < n && i < Looks.MostPieces; i++)
            {
                uint c;
                try
                {
                    c = (uint)PED._0x9B90842304C938A7((ulong)h, (ulong)i, 0);
                }
                catch
                {
                    break;
                }
                string nm;
                if (!BodyParts.Contains(c) && (Torso.ContainsKey(c) || Legs.ContainsKey(c) || NotCover.TryGetValue(c, out nm)) && !cats.Contains(c))
                    cats.Add(c);
            }
            foreach (uint c in cats)
                Function.Call(RemoveTagFromMetaPed, h, c, 0);
            Function.Call(UpdatePedVariation, h, false, true, true, true, false);
            string dress = DressOf(h, out after);
            _strips++;
            _stripLast = act.Name + " (" + (dress ?? "still dressed") + ")";
            Log.Write("WAKE strip: " + act.Name + " takes every stitch off: " + cats.Count + " pieces (" + (before ?? "?") + ") -> " + (dress ?? "still dressed") + (after != null ? " (" + after + ")" : ""));
            Think(act, WakeOdd.ThoughtOf("strip", act.P.Id + "|strip"));
            // up to three who can see it, as at a naked man (Sights), with a thought of their own about it
            int seen = 0;
            Vector3 pos = new Ped(h).Position;
            foreach (Ped p in World.GetAllPeds().Where(x => _cfg.SightReactions && x != null && x.Exists() && !x.IsDead && x.Handle != h && x.Handle != me.Handle && x.Position.DistanceTo(pos) < 14f)
                                                .OrderBy(x => x.Position.DistanceTo(pos)))
            {
                if (SightCant(p, me) != null || !ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(p.Handle, h, 17))
                    continue;
                string id, name, temper;
                bool known = LastingId(p, out id, out name, out temper);
                bool woman = !PED.IS_PED_MALE(p.Handle);
                string seed = (known ? id : "h:" + p.Handle) + "|strip|" + act.P.Id;
                if (!known)
                    temper = Sights.StrangerTemper(seed);
                Sights.Look look = Sights.For("naked", temper, null, seed, woman);
                look.Thought = WakeOdd.NakedThought(look.Feeling, act.P.Woman, seed);
                _sights.Add(new SightRun { By = p, At = new Ped(h), Name = SightName(p, known ? name : null, woman), State = "naked (another)", Woman = woman, Seed = seed, Look = look, Start = now + seen * 1400 });
                Log.Write("SIGHT " + SightName(p, known ? name : null, woman) + ": " + act.Name + " stripped bare -> " + look.Feeling + " (" + (temper ?? "plain") + ")");
                if (++seen >= 3)
                    break;
            }
            if (seen > 0)
                SightSetsLoad();
        }

        // ---- gestures ------------------------------------------------------------------------------------------------

        /// A stage clip for this act, soon (at): whole-body clips only when they stand (standing), upper-body ones over
        /// anything; want: a clip by name (the wave).
        private void WakeGesture(WakeAct act, int at, bool standing, string want = null)
        {
            if (!_cfg.WakeGestures || act.GestPhase != 0)
                return;
            bool woman = act.P.Woman;
            string group = act.Stage + " ";
            var pool = new List<AnimClip>();
            foreach (AnimClip c in StageClips)
            {
                if (want != null ? c.Clip != want : !c.Group.StartsWith(group, StringComparison.Ordinal))
                    continue;
                if (c.Flags != Upper && !standing)
                    continue;
                bool gendered = c.Dict.Contains("gen_male") || c.Dict.Contains("@male@") || c.Dict.StartsWith("mech_loco_m@", StringComparison.Ordinal);
                if (woman && gendered && c.FDict == null)
                    continue;
                string clip = woman && c.FDict != null ? c.FClip : c.Clip;
                if (_gestBad.Contains(clip))
                    continue;
                pool.Add(c);
            }
            if (pool.Count == 0)
                return;
            AnimClip pick = pool[_rng.Next(pool.Count)];
            bool f = woman && pick.FDict != null;
            act.GestDict = f ? pick.FDict : pick.Dict;
            act.GestClip = f ? pick.FClip : pick.Clip;
            act.GestFlags = pick.Flags;
            act.GestAt = at;
            act.GestPhase = 1;
            STREAMING.REQUEST_ANIM_DICT(act.GestDict);
            _gestDicts.Add(act.GestDict);
        }

        private void WakeGestureStep(WakeAct act, int now)
        {
            if (act.GestPhase == 0 || now < act.GestAt)
                return;
            int h = act.Handle;
            switch (act.GestPhase)
            {
                case 1:   // loaded: play it
                    if (!STREAMING.HAS_ANIM_DICT_LOADED(act.GestDict))
                    {
                        if (now - act.GestAt > 3000)
                            GestureFailed(act, "the set didn't load");
                        return;
                    }
                    act.GestFrom = new Ped(h).Position;
                    TASK.TASK_PLAY_ANIM(h, act.GestDict, act.GestClip, 4f, -4f, -1, act.GestFlags, 0f, false, 0, false, "", false);
                    float secs = ENTITY.GET_ANIM_DURATION(act.GestDict, act.GestClip);
                    act.GestStarted = now;
                    act.GestUntil = now + Math.Min(secs > 0f ? (int)(secs * 1000f) : 4000, act.GestFlags == Loop ? 4000 : 10000) + 300;
                    act.GestPhase = 2;
                    act.GestSeen = false;
                    return;
                case 2:   // playing: did it start, did it carry them off
                {
                    bool playing = ENTITY.IS_ENTITY_PLAYING_ANIM(h, act.GestDict, act.GestClip, 3);
                    if (playing && !act.GestSeen)
                    {
                        act.GestSeen = true;
                        _gestPlayed++;
                        Log.Detail("WAKE gesture: " + act.Name + " " + act.GestClip);
                    }
                    if (!act.GestSeen && now - act.GestStarted >= 400)
                    {
                        if (!act.GestRetried && act.GestFlags == Upper)
                        {
                            act.GestRetried = true;   // an upper-body clip the check can't see: over the whole body, as the test row does
                            act.GestFlags = Once;
                            act.GestPhase = 1;
                            return;
                        }
                        GestureFailed(act, "didn't start");
                        return;
                    }
                    if (now < act.GestUntil && playing)
                        return;
                    float moved = new Ped(h).Position.DistanceTo(act.GestFrom);
                    if (moved > 1.2f && act.GestFlags != Upper && act.Blocked && act.Kind != "drift" && act.Kind != "pace")   // only a whole-body clip can carry them, and only someone we hold still is measured (2026-10-31: a look-round walked 3.3 m)
                    {
                        _gestBad.Add(act.GestClip);
                        Log.Write("WAKE gesture dropped: " + act.GestClip + " carried " + act.Name + " " + moved.ToString("0.0") + " m (dropped for the session)");
                    }
                    if (act.GestFlags != Upper && (act.Kind == "dazed" || act.Kind == "freeze" || act.Kind == "stare" || act.Kind == "preach" || act.Kind == "strip" || act.Kind == "shout") && act.Until - now > 800 && !PED.IS_PED_USING_ANY_SCENARIO(h))
                        TASK.TASK_STAND_STILL(h, act.Until - now);   // a whole-body clip ends their orders: held where they stand for the rest of the act
                    if (act.GestFlags == Loop)
                        TASK.STOP_ANIM_TASK(h, act.GestDict, act.GestClip, -4f);
                    act.GestPhase = 0;
                    return;
                }
            }
        }

        private void GestureFailed(WakeAct act, string why)
        {
            act.GestPhase = 0;
            _gestFailed++;
            if (act.GestRetried || why != "didn't start")
            {
                if (_gestBad.Add(act.GestClip))
                    Log.Write("WAKE gesture dropped: " + act.GestClip + " (" + act.GestDict + ") " + why + " on " + act.Name + " (dropped for the session)");
            }
            else
                Log.Detail("WAKE gesture: " + act.GestClip + " " + why + " on " + act.Name);
        }

        // ---- the Testing row ----------------------------------------------------------------------------------------

        /// The next strange act in turn on the nearest person unravelling or awake within 15 m (stripping: awake, and
        /// whatever the roll and their mark).
        private string WakeStrangeTest(int now)
        {
            if (!_cfg.WestWorld)
                return "West World is off";
            Ped me = Game.Player.Character;
            string[] order = WakeOdd.StrangeKinds.Concat(new[] { "lashout at you", "lashout at another", "strip", "dazed" }).ToArray();
            string turn = order[_strangeTest % order.Length], kind = turn.StartsWith("lashout", StringComparison.Ordinal) ? "lashout" : turn;
            foreach (Ped ped in World.GetAllPeds().Where(x => x != null && x.Exists() && !x.IsDead && x.Handle != me.Handle && PED.IS_PED_HUMAN(x.Handle) && x.Position.DistanceTo(me.Position) < 15f)
                                                 .OrderBy(x => x.Position.DistanceTo(me.Position)))
            {
                Person p;
                PersonState st;
                Resident res;
                bool passive;
                if (_director.Involves(ped.Handle) || !WakeIdentity(ped, true, out p, out st, out res, out passive) || passive || st.WakeStage < Awakening.Unravelling)
                    continue;
                if (kind == "strip" && StripHeldBack)
                {
                    _strangeTest++;
                    return "stripping is held back: it froze the game the first time it ran (2026-10-31); press it again for the next";
                }
                if (kind == "strip" && (st.WakeStage < Awakening.Awake || _bridge.RecordAt(ped.Handle) != null))
                {
                    _strangeTest++;   // on to the next turn, as it says
                    return "stripping needs someone awake (stage 4) who isn't one of enRAGE's people; press it again for the next";
                }
                WakeAct old = _wakeActs.FirstOrDefault(a => a.Handle == ped.Handle);
                if (old != null)
                {
                    EndWakeAct(old, "the Testing row");
                    _wakeActs.Remove(old);
                }
                _lashTestAtYou = turn == "lashout at you" ? 1 : turn == "lashout at another" ? -1 : 0;
                _strangeTest++;
                var act = new WakeAct
                {
                    Handle = ped.Handle, Kind = kind, Stage = st.WakeStage, Started = now, NextStep = now + 400, Name = p.Name, P = p, St = st, Res = res, A = ped.Position,
                    HoldStart = now, HoldUntil = now + _cfg.WakeHoldMinutes * 60000, Links = 1
                };
                TASK.CLEAR_PED_TASKS(ped.Handle, true, false);
                if (!BeginAct(act, ped, me, now))
                    return turn + " needs someone else free within 14 m of " + p.Name;
                _wakeActs.Add(act);
                _wakeCool[ped.Handle] = now + 20000;
                Log.Write("WAKE strange test: " + p.Name + " (" + Awakening.StageName(st.WakeStage) + ") " + turn + (kind == "lashout" ? "" : ", then held"));
                return null;
            }
            return "nobody unravelling or awake within 15 m whose movement is ours (Y wakes the one you face; " + turn + " was next, skipped)";
        }

        private string WakeHoldStatus()
        {
            var ans = _answers.Count == 0 ? "none" : string.Join(", ", _answers.OrderByDescending(kv => kv.Value).Select(kv => kv.Key + " " + kv.Value));
            return (_cfg.WakeHold ? "on, " + _cfg.WakeHoldMinutes + " min" : "off ([WestWorld] WakeHold)") + "; this session " + _holds + " held (" + _holdLinks + " links), last " + (_holdLast ?? "none") +
                   "; gestures " + (_cfg.WakeGestures ? _gestPlayed + " played, " + _gestFailed + " didn't" + (_gestBad.Count > 0 ? ", dropped: " + string.Join(", ", _gestBad) : "") : "off") +
                   "; strange acts " + (_cfg.WakeStrange ? _strangeActs + " (answers: " + ans + "), last " + (_strangeLast ?? "none") : "off") +
                   "; stripped " + _strips + " (" + _cfg.WakeStripChance + "% of the awake)" + (_stripLast != null ? ", last " + _stripLast : "") +
                   "; lash-outs " + _lashOuts + " (" + _lashAtYou + " at you; " + _cfg.WakeLashOut + "% of holds at stages 3-4)" + (_lashLast != null ? ", last " + _lashLast : "") +
                   "; now held: " + (_wakeActs.Count(a => a.Links > 0) == 0 ? "nobody" : string.Join(", ", _wakeActs.Where(a => a.Links > 0).Select(a => a.Name + " " + a.Kind)));
        }

        private void WakeGesturesUnload()
        {
            foreach (string d in _gestDicts)
                STREAMING.REMOVE_ANIM_DICT(d);
            _gestDicts.Clear();
        }
    }
}
