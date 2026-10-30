// The Awakening from the mind's doubts, the game side (night 2026-10-29, item 4; Core/Engine.Doubt.cs; [Mind] MindWakes, on,
// gated by MindLive and West World). Two things: when the shadow sees "things are as they seem" give way in someone
// (MindBelieve), they stir (Engine.StirFromDoubt); and every 2 s, someone our own death ledger has dead who walks near
// you again (one of enRAGE's records it brought back, a townsperson it missed) is seen by anyone near who saw them die,
// by their own memory of it: the revived dead appraised (fear: most back away from him, a hard fright roots them to the
// spot), kept for their mood, their beliefs moved (the belief breaks on its own, so they stir), and the act played through
// the onlookers' queue (standing rooted, slipping off, walking away, watching him: WestworldScript.Onlookers.cs). Once a
// pair a session, two a look at most. Reads the ledger and memories only; the Awakening's stage is written as it always
// was. Log AWAKENING from doubt, MIND revived; F7 TEST awakening from doubt:.
using System;
using System.Collections.Generic;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private const int RevivedLookMs = 2000;
        private readonly HashSet<string> _revivedNoticed = new HashSet<string>();
        private readonly HashSet<string> _doubtStirred = new HashSet<string>();   // once a person a session (no hovering about 0.5 for a stage a day)
        private int _revivedNextAt, _doubtStirs, _doubtHeld, _revivedSeen;
        private string _doubtLast, _revivedLast;

        private bool DoubtActsOn { get { return _cfg != null && _cfg.MindLive && _cfg.MindWakes && _cfg.WestWorld; } }

        /// From MindBelieve: "things are as they seem" has just given way in them. With MindWakes they stir.
        private void DoubtGaveWay(MindWho w, string id)
        {
            try
            {
                if (!DoubtActsOn || w == null || w.P == null || _doubtStirred.Contains(id))
                    return;
                if (_doubtStirred.Count < 1000)
                    _doubtStirred.Add(id);
                string why;
                int stage = _engine.StirFromDoubt(w.P, _store.LastGameMin, out why);
                if (stage > 0)
                {
                    _doubtStirs++;
                    _doubtLast = w.Name + " to " + Awakening.StageName(stage);
                    Log.Write("AWAKENING from doubt: " + w.Name + " (stage " + stage + ", " + Awakening.StageName(stage) + "): things are no longer as they seem to them");
                }
                else
                {
                    _doubtHeld++;
                    Log.Detail("AWAKENING from doubt: " + w.Name + " not moved (" + why + ")");
                }
            }
            catch (Exception ex)
            {
                Log.Write("AWAKENING from doubt failed: " + ex.Message);
            }
        }

        /// Every 2 s with the mind: the revived dead walking near you, seen by those who saw them die.
        private void RevivedStep(int now)
        {
            if (!DoubtActsOn || now < _revivedNextAt)
                return;
            _revivedNextAt = now + RevivedLookMs;
            try
            {
                long g = _store.LastGameMin;
                int noticed = 0;
                foreach (int rh in _mindNear)
                {
                    MindWho r;
                    if (!_mindWho.TryGetValue(rh, out r) || r.Id == null || Resident.IsResident(r.Id) || !ENTITY.DOES_ENTITY_EXIST(rh) || ENTITY.IS_ENTITY_DEAD(rh))
                        continue;   // a resident dead on our ledger is never seen again: one walking is a keeper shot and survived (the review)
                    DeathEntry d = _store.DeathOf(r.Id);
                    if (d == null || g - d.At < 10)
                        continue;   // not dead on our own ledger, or the death a moment ago (the body still falling)
                    foreach (int wh in _mindNear)
                    {
                        if (noticed >= 2)
                            return;
                        MindWho w;
                        if (wh == rh || !_mindWho.TryGetValue(wh, out w) || w.Id == null || w.Traits == null || w.P == null || w.Id == r.Id || !ENTITY.DOES_ENTITY_EXIST(wh) || ENTITY.IS_ENTITY_DEAD(wh) || w.Model != ENTITY.GET_ENTITY_MODEL(wh))
                            continue;
                        string key = w.Id + "|" + r.Id;
                        if (_revivedNoticed.Contains(key) || !Engine.SawThemDie(_store.Find(w.Id), r.Id))
                            continue;
                        if (_revivedNoticed.Count >= 500)
                            return;   // full: nothing more this session rather than the same pair every 2 s
                        _revivedNoticed.Add(key);
                        noticed++;
                        PersonState ws = _store.Find(w.Id);
                        AppraisalEvent ae = Engine.RevivedSeen(w.P, _engine.NowIndoors ? _engine.NowPlace : null, _placeTown, Math.Max(0, _mindNear.Count - 2), ws != null ? ws.WakeStage : 0, r.Id, r.Name);
                        Feeling f = Appraisal.Of(ae, w.Traits);
                        f.Shows = Display.Shows(f, ae, w.Traits);
                        _engine.Shadow.Feel(w.Id, f, g);
                        _revivedSeen++;
                        long days = (g - d.At) / 1440;
                        _revivedLast = w.Name + " saw " + r.Name + " walking";
                        MindLog(now, "MIND revived: " + w.Name + " sees " + r.Name + ", whom they saw die " + (days <= 0 ? "today" : days == 1 ? "yesterday" : days + " days ago") + ": " + f.Kind + " (" + f.Strength + "), urge " + f.Urge + ", shows " + f.Shows);
                        MindBelieve(w, w.Id, f, ae, now);   // the belief breaks on its own (Beliefs.Move: the revived), so they stir (DoubtGaveWay)
                        string act = _cfg.MindOnlookers ? Onlooking.ActFor(f, f.Shows, "onlooker", PED.IS_PED_USING_ANY_SCENARIO(wh), ae.Place, w.Traits) : null;
                        if (act != null && _onlookQueue.Count < OnlookQueueMost)
                            _onlookQueue.Add(new OnlookerAct { Handle = wh, Model = ENTITY.GET_ENTITY_MODEL(wh), From = rh, QueuedAt = now - OnlookDelayMs, Id = w.Id, Name = w.Name, Woman = w.P.Woman, Act = act, Felt = f,
                                                               Why = "the revived dead: " + r.Name });
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Write("MIND revived failed: " + ex.Message);
                _revivedNextAt = now + 60000;   // a minute's rest, then again
            }
        }

        private void DoubtForget()
        {
            _revivedNoticed.Clear();
            _doubtStirred.Clear();
        }

        /// F7: on or off, how many stirred from doubt, the revived seen.
        private string DoubtStatus()
        {
            if (!DoubtActsOn)
                return "off ([Mind] MindLive, MindWakes and [West World] WestWorld all needed): the Awakening by its own roll";
            return _doubtStirs + " stirred from doubt this session" + (_doubtLast != null ? " (last: " + _doubtLast + ")" : "") + ", " + _doubtHeld + " whose doubt moved nothing (no state kept, moved today, awake); " +
                   _revivedSeen + " times the revived dead were seen by someone who saw them die" + (_revivedLast != null ? " (last: " + _revivedLast + ")" : "");
        }
    }
}
