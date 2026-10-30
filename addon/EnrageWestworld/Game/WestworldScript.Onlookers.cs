// Onlookers act on their own feelings (roadmap 39, part 2; night 2026-10-29, item 3; the act is Core/Onlooking.cs;
// [Mind] MindOnlookers, on, gated by MindLive). The shadow takes every recorded happening person by person
// (MindAppraiseOne); a witness who only watched it, whose face may show what they feel, is noted here, and once the
// happening's last witness has been taken the strongest two (Onlooking.Pick) are queued. Only trouble between people and
// what the player did (Onlooking.ActsOn: never a pickpocket's or a lift's, which onlookers would give away), never one of
// the scene's own cast. One act a frame, in its own guard, 2.5 s after it was noted at the soonest (the scene is recorded
// as it starts: the trouble shows first) and 15 s at the latest: the street reactions' own check again at that moment
// (SightCant: a bystander, not the law, not at a counter, not in a scene, an act, a talk, a nod or speaking, and MayDirect),
// nobody sitting or leaning, down or past 30 m. Only moves already seen in game: a walk off (the scenes' walkaway, for
// fear shown, fear masked and disapproval alike; the scenes' flee is not yet seen working, so never); standing still (the reactions' held swallow); turning to face; the
// reactions' head-shake and bristle clips. Each ends by itself, so there is nothing to hand back. Log ONLOOK; F7 TEST
// onlookers:.
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
        private sealed class OnlookerAct
        {
            public int Handle, From, QueuedAt;   // QueuedAt: when the witness was taken
            public uint Model;
            public string Id, Name, Act, Why;
            public bool Woman;
            public Feeling Felt;
        }

        private const int OnlookQueueMost = 8, OnlookPendingMost = 12, OnlookStaleMs = 15000, OnlookDelayMs = 2500;
        private const float OnlookMetres = 30f;
        private readonly List<OnlookerAct> _onlookQueue = new List<OnlookerAct>();
        private readonly List<OnlookerAct> _onlookPending = new List<OnlookerAct>();   // the happening being taken: who might act
        private readonly List<string> _onlookIds = new List<string>();
        private readonly List<Feeling> _onlookFelt = new List<Feeling>();
        private readonly List<string> _onlookActs = new List<string>();
        private int _onlooked, _onlookSkipped, _onlookStale;
        private string _onlookLast, _onlookLastSkip;
        private Action _aOnlook;

        /// Onlookers act on what they feel: with [Mind] MindLive and [Mind] MindOnlookers.
        private bool OnlookersOn { get { return _cfg != null && _cfg.MindLive && _cfg.MindOnlookers; } }

        /// From MindAppraiseOne: someone who only watched it may act on it. Noted for the happening (capped); picked when
        /// the happening's last witness has been taken (OnlookFlush). Catches its own errors.
        private void OnlookNote(Happening ev, string id, MindWho w, Feeling f, AppraisalEvent ae)
        {
            try
            {
                if (!OnlookersOn || ev == null || f == null || ae == null || _onlookPending.Count >= OnlookPendingMost)
                    return;
                if (!Onlooking.ActsOn(ev.Kind) || id == ev.DoerId || id == ev.TargetId)
                    return;   // trouble between others, or something the player did; never the two it was between
                int h = MindHandleById(id);
                if (h == 0 || _director.Involves(h))
                    return;   // nobody near now, or one of the scene's own (the one who stepped in: a witness too)
                string act = Onlooking.ActFor(f, f.Shows, ae.Role, PED.IS_PED_USING_ANY_SCENARIO(h), ev.Place, w.Traits);
                if (act == null)
                    return;
                int from = ev.DoerId == "player" ? Game.Player.Character.Handle : MindHandleById(ev.DoerId);
                if (from == 0)
                    from = MindHandleById(ev.TargetId);
                _onlookPending.Add(new OnlookerAct { Handle = h, Model = ENTITY.GET_ENTITY_MODEL(h), From = from, QueuedAt = _now, Id = id, Name = w.Name, Woman = w.P != null && w.P.Woman, Act = act, Felt = f,
                                                     Why = ev.Kind + (ev.DoerName != null ? " (" + (ev.DoerId == "player" ? "you" : ev.DoerName) + (ev.TargetName != null ? " and " + ev.TargetName : "") + ")" : "") });
            }
            catch (Exception ex)
            {
                Log.Write("ONLOOK noting failed: " + ex.Message);
                _onlookPending.Clear();
            }
        }

        /// The happening's last witness has been taken: the strongest two who'd act are queued, the rest let be.
        private void OnlookFlush(int now)
        {
            if (_onlookPending.Count == 0)
                return;
            try
            {
                _onlookIds.Clear();
                _onlookFelt.Clear();
                _onlookActs.Clear();
                foreach (OnlookerAct a in _onlookPending)
                {
                    _onlookIds.Add(a.Id);
                    _onlookFelt.Add(a.Felt);
                    _onlookActs.Add(a.Act);
                }
                foreach (int i in Onlooking.Pick(_onlookIds, _onlookFelt, _onlookActs))
                    if (_onlookQueue.Count < OnlookQueueMost)
                        _onlookQueue.Add(_onlookPending[i]);
            }
            catch (Exception ex)
            {
                Log.Write("ONLOOK picking failed: " + ex.Message);   // its own: never the shadow's guard
            }
            finally
            {
                _onlookPending.Clear();
            }
        }

        /// One act a frame, from the mind's tick, in its own guard.
        private void OnlookStep(int now)
        {
            if (_onlookQueue.Count == 0)
                return;
            if (_aOnlook == null)
                _aOnlook = () => OnlookPlay(_now);
            Safe("mind onlookers", _aOnlook);
        }

        private void OnlookPlay(int now)
        {
            OnlookerAct a = _onlookQueue[0];
            if (!OnlookersOn)
            {
                _onlookQueue.Clear();
                return;
            }
            if (now >= a.QueuedAt && now - a.QueuedAt < OnlookDelayMs)
                return;   // the trouble shows first: it waits, first in line
            _onlookQueue.RemoveAt(0);
            if (now - a.QueuedAt > OnlookStaleMs || now < a.QueuedAt)
            {
                _onlookStale++;
                return;
            }
            int h = a.Handle;
            Ped me = Game.Player.Character;
            if (!ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h) || ENTITY.GET_ENTITY_MODEL(h) != a.Model)
                return;   // gone, or the handle is someone else's now
            Ped p = new Ped(h);
            string skip = me != null ? SightCant(p, me) : "no player";   // the street reactions' own check: a bystander, not busy, MayDirect
            if (skip == null)
            {
                if (PED.IS_PED_USING_ANY_SCENARIO(h)) skip = "sitting or leaning";
                else if (PED.IS_PED_RAGDOLL(h)) skip = "down";
                else if (me == null || p.Position.DistanceTo(me.Position) > OnlookMetres) skip = "too far off";
            }
            if (skip != null)
            {
                _onlookSkipped++;
                _onlookLastSkip = a.Name + " (" + skip + ")";
                Log.Detail("ONLOOK " + a.Name + " left alone (" + skip + ")");
                return;
            }
            int from = a.From != 0 && ENTITY.DOES_ENTITY_EXIST(a.From) ? a.From : 0;
            string did;
            switch (a.Act)
            {
                case "leave":
                    if (from != 0)
                        TASK.TASK_LOOK_AT_ENTITY(h, from, 1500, 0, 51, 0);
                    TASK.TASK_WANDER_STANDARD(h, 10f, 10);   // the scenes' walkaway (their flee isn't yet seen working: the catalogue)
                    did = "walks off from it";
                    break;
                case "disapprove":
                    {
                        // a shake of the head (the reactions' own clip, upper body) and off about their business
                        string dd = Reactions.Dict(a.Woman), dc = Reactions.Clip("dismiss", a.Woman, a.Id + a.QueuedAt);
                        if (dc != null && STREAMING.HAS_ANIM_DICT_LOADED(dd))
                            TASK.TASK_PLAY_ANIM(h, dd, dc, 4f, -4f, -1, 8 | 16, 0f, false, 0, false, "", false);
                        else
                            STREAMING.REQUEST_ANIM_DICT(dd);
                        TASK.TASK_WANDER_STANDARD(h, 10f, 10);
                        did = "walks off from it, disapproving";
                        break;
                    }
                case "slip":
                    // masked fear: no hurry, nothing shown; a look away from it and off about their business
                    TASK.TASK_WANDER_STANDARD(h, 10f, 10);   // the scenes' walkaway
                    did = "slips away from it";
                    break;
                case "freeze":
                    if (from != 0)
                        TASK.TASK_LOOK_AT_ENTITY(h, from, Onlooking.HoldMs("freeze"), 0, 51, 0);
                    TASK.TASK_STAND_STILL(h, Onlooking.HoldMs("freeze"));
                    did = "stands rooted";
                    break;
                case "watch":
                    if (from == 0)
                    {
                        _onlookSkipped++;
                        _onlookLastSkip = a.Name + " (nothing to watch: the one who did it is gone)";
                        return;
                    }
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, from, 3000, 0f, 0f, 0f);
                    TASK.TASK_LOOK_AT_ENTITY(h, from, Onlooking.HoldMs("watch"), 0, 51, 0);
                    did = "turns to watch it";
                    break;
                default:   // dismiss, bristle: the reactions' clips, on the upper body
                    {
                        string dict = Reactions.Dict(a.Woman), clip = Reactions.Clip(a.Act, a.Woman, a.Id + a.QueuedAt);
                        if (clip == null || !STREAMING.HAS_ANIM_DICT_LOADED(dict))
                        {
                            STREAMING.REQUEST_ANIM_DICT(dict);
                            _onlookSkipped++;
                            _onlookLastSkip = a.Name + " (" + dict + " not loaded yet)";
                            return;
                        }
                        if (from != 0)
                            TASK.TASK_LOOK_AT_ENTITY(h, from, 2500, 0, 51, 0);
                        TASK.TASK_PLAY_ANIM(h, dict, clip, 4f, -4f, -1, 8 | 16, 0f, false, 0, false, "", false);
                        did = (a.Act == "dismiss" ? "shakes their head at it" : "bristles at it") + " (" + clip + ")";
                        break;
                    }
            }
            _onlooked++;
            _onlookLast = a.Name + " " + did;
            Log.Write("ONLOOK " + a.Name + " " + did + " (" + a.Felt.Kind + " " + a.Felt.Strength + ", urge " + a.Felt.Urge + ", shows " + a.Felt.Shows + ") <- " + a.Why);
        }

        /// The handle of someone known to the mind by id, alive near the player this second (the scan's), or 0.
        private int MindHandleById(string id)
        {
            if (id == null)
                return 0;
            foreach (int h in _mindNear)
            {
                MindWho w;
                if (_mindWho.TryGetValue(h, out w) && w.Id == id)
                    return ENTITY.DOES_ENTITY_EXIST(h) && !ENTITY.IS_ENTITY_DEAD(h) ? h : 0;
            }
            return 0;
        }

        /// A load, a rewind: nothing queued from before acts after.
        private void OnlookForget()
        {
            _onlookQueue.Clear();
            _onlookPending.Clear();
        }

        /// F7: on or off, how many acted and were let be, the last.
        private string OnlookersStatus()
        {
            if (_cfg == null || !_cfg.MindLive || !_cfg.MindOnlookers || !_cfg.MindShadow)
                return "off ([Mind] " + (_cfg == null || !_cfg.MindLive ? "MindLive" : !_cfg.MindShadow ? "MindShadow" : "MindOnlookers") + "): onlookers only turn their heads";
            return _onlooked + " acted this session (at most " + Onlooking.MostAScene + " a happening), " + _onlookSkipped + " let be, " + _onlookStale + " too late; queued " + _onlookQueue.Count +
                   (_onlookLast != null ? "; last: " + _onlookLast : "") + (_onlookLastSkip != null ? "; last let be: " + _onlookLastSkip : "");
        }
    }
}
