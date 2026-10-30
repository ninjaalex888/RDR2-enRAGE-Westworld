// Despair's other side, the game side (night 2026-10-29, item 6d; Core/Rescue.cs; [Mind] MindRescue, on, gated by MindLive;
// [Mind] RescueChance). When the last words begin (despair's phase 3, nothing in their hand yet), anyone near whom the
// mind already knows (the shadow's own scan: nobody is made for it) takes it in (Rescue.SeenBy through the appraisal): the
// one most moved to go to them walks over while the last words are held (a friend first on a near tie), if they get there
// in time (RescueChance), and says it to their face; the despairing one heeds them or not (Rescue.Heeded). Heeded, the
// act ends there (its deadline brought to now) and both go on their way; the two keep it (a comfort tie and the memory of
// it). Not heeded, or the one coming taken up, too slow or the despairing one not ours to direct any more, the act goes on
// as before and the one who came goes on their way. The one walking over counts as busy for every other feature
// (IsAcknowledging). Only proven moves: the walk over, turning to face, the words on the bar (no recorded voice: the game
// has no line that comforts). Log WAKE despair ... goes to / talked down / wouldn't be talked down.
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
        private const float RescueMetres = 12f, RescueStrangerMetres = 8f, RescueNear = 1.8f;
        private const int RescueWalkMostMs = 12000, RescueSpeakMs = 3800, RescueAnswerMs = 3200;
        private int _rescued, _rescueRefused, _rescueTried, _rescueHesitated;
        private string _rescueLast;

        /// Someone walking over to a despairing person now (busy for everything else).
        private bool IsRescuer(int h)
        {
            if (h == 0)
                return false;
            foreach (WakeAct a in _wakeActs)
                if (a.SaverHandle == h)
                    return true;
            return false;
        }

        /// From despair's phase 3: true while someone is coming to them or speaking with them (the last words held).
        private bool RescueStep(WakeAct act, Ped me, int now)
        {
            try
            {
                if (_cfg == null || !_cfg.MindLive || !_cfg.MindRescue)
                    return false;
                if (!act.RescueTried)
                {
                    act.RescueTried = true;
                    RescueFind(act, me, now);
                }
                if (act.SaverHandle == 0)
                    return false;
                int s = act.SaverHandle, h = act.Handle;
                if (!ENTITY.DOES_ENTITY_EXIST(s) || ENTITY.IS_ENTITY_DEAD(s) || MayDirectWhyNot(new Ped(s)) != null)
                    return RescueGiveUp(act, now, "was taken up");
                string theirs = RescueTheirsWhyNot(act);
                if (theirs != null)
                    return RescueGiveUp(act, now, "turned back: " + act.Name + " isn't ours to direct now (" + theirs + ")");
                Ped saver = new Ped(s), them = new Ped(h);
                float d = saver.Position.DistanceTo(them.Position);
                if (act.SaverSpokeAt == 0)
                {
                    if (d > RescueNear)
                    {
                        if (now > act.SaverUntil)
                            return RescueGiveUp(act, now, "never got there (still " + d.ToString("0.0") + " m off)");
                        return true;   // still walking over; the last words go on
                    }
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(s, h, 4000, 0f, 0f, 0f);
                    TASK.TASK_LOOK_AT_ENTITY(s, h, 8000, 0, 51, 0);
                    TASK.TASK_LOOK_AT_ENTITY(h, s, 8000, 0, 51, 0);
                    string call = act.SaverClose ? Person.FirstOf(act.Name) : act.P.Woman ? (act.SaverWoman ? null : "ma'am") : (act.SaverWoman ? null : "friend");
                    SpeechCaption(act.SaverName, Rescue.Plea(act.SaverClose, act.SaverWoman, call, act.SaverId + "|" + act.P.Id), s, RescueSpeakMs);
                    act.SaverSpokeAt = now;
                    return true;
                }
                if (now - act.SaverSpokeAt < RescueSpeakMs)
                    return true;
                if (!act.SaverAnswered)
                {
                    act.SaverAnswered = true;
                    act.SaverHeeded = Rescue.Heeded(act.SaverFriend, act.SaverId + "|" + act.P.Id);
                    SpeechCaption(act.Name, Rescue.Answer(act.SaverHeeded, act.P.Id + "|" + act.SaverId), h, RescueAnswerMs);
                    if (act.SaverHeeded)
                    {
                        TASK.CLEAR_PED_TASKS(h, true, false);   // the sobbing stops; they turn to the one who came
                        TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, s, 3000, 0f, 0f, 0f);
                    }
                    return true;
                }
                if (now - act.SaverSpokeAt < RescueSpeakMs + RescueAnswerMs)
                    return true;
                if (!act.SaverHeeded)
                {
                    _rescueRefused++;
                    _rescueLast = act.SaverName + " couldn't talk " + act.Name + " down";
                    Log.Write("WAKE despair: " + act.Name + " wouldn't be talked down by " + act.SaverName + "; it goes on");
                    return RescueResume(act, now);
                }
                _rescued++;
                _rescueLast = act.SaverName + " talked " + act.Name + " down";
                Log.Write("WAKE despair: " + act.SaverName + " talked " + act.Name + " down (" + (act.SaverFriend ? "a friend" : "a stranger") + "): they come away");
                SpeechCaption(act.SaverName + ", thinking", Rescue.After(true, act.SaverClose ? Person.FirstOf(act.Name) : null, act.SaverId), s, 4000);
                RescueRemember(act, saver, them);
                act.Until = now;   // the act ends at the next look ("finished"); EndWakeAct sends both on their way (RescueRelease)
                return true;
            }
            catch (Exception ex)
            {
                Log.Write("WAKE despair: the rescue failed (" + ex.Message + "); the act goes on");
                RescueLetGo(act);
                return false;
            }
        }

        /// Whether the despairing one may be given orders now (as the act's own last check asks: WakeActs' StepDespair).
        private string RescueTheirsWhyNot(WakeAct act)
        {
            Ped p = new Ped(act.Handle);
            return MayDirectWhyNot(p) ?? (act.Rec != null ? RecordFreeWhyNot(p, act.Rec) : null);
        }

        private bool RescueGiveUp(WakeAct act, int now, string why)
        {
            Log.Write("WAKE despair: " + (act.SaverName ?? "the one coming") + " " + why + "; it goes on");
            return RescueResume(act, now);
        }

        /// The act goes on: the one who came goes on their way, and the sobbing takes up again for a moment.
        private bool RescueResume(WakeAct act, int now)
        {
            RescueLetGo(act);
            if (act.Sobbed && STREAMING.HAS_ANIM_DICT_LOADED(SobDict) && ENTITY.DOES_ENTITY_EXIST(act.Handle) && !ENTITY.IS_ENTITY_DEAD(act.Handle) && RescueTheirsWhyNot(act) == null)
            {
                TASK.TASK_PLAY_ANIM(act.Handle, SobDict, SobClip, 4f, -4f, -1, 0, 0f, false, 0, false, "", false);
                act.PhaseUntil = now + 2500;
            }
            else
                act.PhaseUntil = now;
            return false;
        }

        /// The one who came (or was coming) is let go, on their way.
        private void RescueLetGo(WakeAct act)
        {
            int s = act.SaverHandle;
            act.SaverHandle = 0;
            if (s != 0 && ENTITY.DOES_ENTITY_EXIST(s) && !ENTITY.IS_ENTITY_DEAD(s) && MayDirectWhyNot(new Ped(s)) == null)
                TASK.TASK_WANDER_STANDARD(s, 10f, 10);
        }

        /// From EndWakeAct: whoever was coming to them goes on their way, however the act ended.
        private void RescueRelease(WakeAct act)
        {
            try
            {
                if (act.SaverHandle != 0)
                    RescueLetGo(act);
            }
            catch
            {
            }
        }

        /// Who comes, if anyone: of those near enough who see it, whom the mind already knows (its own scan: nobody made),
        /// the one most moved; then whether they get there in time (RescueChance).
        private void RescueFind(WakeAct act, Ped me, int now)
        {
            Ped them = new Ped(act.Handle);
            string place = _engine.NowIndoors ? _engine.NowPlace : null;
            MindWho best = null;
            int bestH = 0, bestWarm = 0, near = 0;
            Feeling bestF = null;
            foreach (int c in _mindNear)
            {
                MindWho w;
                if (c == act.Handle || !_mindWho.TryGetValue(c, out w) || w.Id == null || w.Id == act.P.Id || w.P == null || !ENTITY.DOES_ENTITY_EXIST(c) || ENTITY.IS_ENTITY_DEAD(c))
                    continue;
                Ped p = new Ped(c);
                float dist = p.Position.DistanceTo(them.Position);
                if (dist >= RescueMetres || SightCant(p, me) != null || PED.IS_PED_USING_ANY_SCENARIO(c) || !ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(c, act.Handle, 17))
                    continue;   // the street reactions' own check (a bystander, not busy, MayDirect), on their feet, in sight of them
                near++;
                Traits t = w.Traits ?? (w.Traits = _engine.Shadow.TraitsOf(w.Src));
                if (t == null)
                    continue;
                int warm = _engine.WarmthBetween(w.Id, act.P.Id);
                if (warm <= 0 && dist > RescueStrangerMetres)
                    continue;   // a stranger notices the last words only close by; a friend from further
                AppraisalEvent e = Rescue.SeenBy(w.P.Woman, warm, place, w.P.Home != null && w.P.Home == _placeTown, Math.Max(0, _mindNear.Count - 2), act.P.Id, act.Name);
                Feeling f = Appraisal.Of(e, t);
                if (!Rescue.Moves(f, warm))
                    continue;
                float dd = bestF == null ? 1f : f.Intensity - bestF.Intensity;
                if (bestF == null || dd > StepIn.NearTie || Math.Abs(dd) <= StepIn.NearTie && warm > bestWarm)
                {
                    best = w;
                    bestH = c;
                    bestF = f;
                    bestWarm = warm;
                }
            }
            _rescueTried++;
            if (best == null)
            {
                Log.Detail("WAKE despair: nobody near " + act.Name + " was moved to go to them (" + near + " who could see it)");
                return;
            }
            if (!Voice.Chance(act.P.Id + "|" + best.Id + "|rescuechance", _cfg.RescueChance))
            {
                _rescueHesitated++;
                Log.Write("WAKE despair: " + best.Name + " is moved to go to " + act.Name + " but hesitates too long");
                return;
            }
            act.SaverHandle = bestH;
            act.SaverId = best.Id;
            act.SaverName = best.Name;
            act.SaverFriend = bestWarm > 0;
            act.SaverClose = bestWarm >= 2;   // the intimate words only for a real friendship (the review of 6d)
            act.SaverWoman = best.P.Woman;
            float d = new Ped(bestH).Position.DistanceTo(them.Position);
            act.SaverUntil = now + Math.Min(RescueWalkMostMs, 3000 + (int)(d * 700f));
            TASK.TASK_GO_TO_ENTITY(bestH, act.Handle, 20000, Natives.Brisk, 1.2f, 0f, 0);
            Log.Write("WAKE despair: " + best.Name + " sees it coming and goes to " + act.Name + " (" + (act.SaverFriend ? "a friend" : "a stranger") + ", pity " + bestF.Strength + ", " + d.ToString("0.0") + " m off)");
        }

        /// The two keep it: the tie (a comfort: B, the one who came, comforted A) and the memory of it; both are kept from now
        /// on (an ordinary passer-by who talks someone down is someone now).
        private void RescueRemember(WakeAct act, Ped saver, Ped them)
        {
            try
            {
                Minds.Party pt = PartyOf(them, true), ps = PartyOf(saver, true);   // kept: so LastingId knows them
                string sId, sName, tId, tName;
                if (!LastingId(saver, out sId, out sName) || !LastingId(them, out tId, out tName))
                {
                    Log.Detail("WAKE despair: the rescue left no tie (one of the two can't be met again)");
                    return;
                }
                SceneTie t = Ties.Note(_store.Ties, tId, tName, sId, sName, _placeTown, "comfort", Bridge.Now());
                if (t != null)
                    Log.Write("TIE " + t.AName + " & " + t.BName + ": comfort (" + (t.Warmth > 0 ? "warm" : t.Warmth < 0 ? "hostile" : "acquainted") + ", " + t.Count + " scenes between them)");
                if (pt != null && ps != null)
                    _engine.RememberScene("comfort", pt, ps, null, _placeTown, _engine.NowIndoors ? _engine.NowPlace : null);
            }
            catch (Exception ex)
            {
                Log.Write("WAKE despair: keeping the rescue failed: " + ex.Message);
            }
        }

        /// F7: on or off, how many were talked down, refused, hesitated, looked for.
        private string RescueStatus()
        {
            if (_cfg == null || !_cfg.MindLive || !_cfg.MindRescue)
                return "off ([Mind] MindRescue or MindLive): nobody steps in";
            return _rescued + " talked down this session, " + _rescueRefused + " wouldn't be, " + _rescueHesitated + " times the one moved hesitated too long (RescueChance " + _cfg.RescueChance + "%), " +
                   _rescueTried + " despairs looked at" + (_rescueLast != null ? "; last: " + _rescueLast : "");
        }
    }
}
