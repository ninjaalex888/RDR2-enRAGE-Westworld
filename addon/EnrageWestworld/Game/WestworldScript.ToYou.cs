// Passing the player, from the mind, the game side (night 2026-10-29, item 6f; Core/Passing.ToYou.cs; with [Mind] MindLive
// and [Mind] MindPassing). Every 1.5 s at most one person near the player whom the mind knows (the same body: its model
// checked), who feels something about him from a while ago, facing him within 9 m, in his view and free (the street
// reactions' own check, SightCant), shows it, spaced as the passing nods are: the look first, the gesture 450 ms on, the
// thought at 1.6 s; a spoken greeting (the familiar set) when grateful, at most one each game day. Busy for every other feature while
// it plays (IsAcknowledging). Once a person in five minutes. The player is never tasked. Log (detail) NOD you:.
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
        private sealed class YouNod
        {
            public int Handle, At;
            public string Id, Name, Gesture, Thought;
            public bool Woman, Speaks, GestureDone, ThoughtDone;
        }

        private const float YouNodMetres = 9f;
        private const int YouNodEveryMs = 1500, YouNodAgainMs = 300000, YouNodGestureMs = 450, YouNodThoughtMs = 1600, YouNodBusyMs = 3500;
        private static readonly string[] YouGreet = { "GREET_GENERAL_FAMILIAR", "HOWS_IT_GOING" };
        private readonly Dictionary<string, int> _youNodAt = new Dictionary<string, int>();
        private readonly Dictionary<string, long> _youSpokeDay = new Dictionary<string, long>();
        private YouNod _youNod;
        private int _youNodNext, _youNods;
        private string _youNodLast;

        /// Someone acknowledging the player now (busy for everything else).
        private bool IsAcknowledgingYou(int h)
        {
            return _youNod != null && _youNod.Handle == h;
        }

        /// From the mind's tick: the one acknowledging the player plays on; else, at most every 1.5 s, the next.
        private void YouNodStep(int now)
        {
            if (_cfg == null || !_cfg.MindLive || !_cfg.MindPassing)
            {
                _youNod = null;
                return;
            }
            try
            {
                Ped me = Game.Player.Character;
                if (_youNod != null)
                {
                    YouNodPlay(me, now);
                    return;
                }
                if (now < _youNodNext || me == null || !me.Exists())
                    return;
                _youNodNext = now + YouNodEveryMs;
                long g = _store.LastGameMin;
                foreach (int h in _mindNear)
                {
                    MindWho w;
                    if (_director.Involves(h) || !_mindWho.TryGetValue(h, out w) || w.Id == null || w.P == null || w.Traits == null || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h) ||
                        w.Model != ENTITY.GET_ENTITY_MODEL(h))
                        continue;   // a handle the game gave to someone else since (the review of 6f)
                    int at;
                    if (_youNodAt.TryGetValue(w.Id, out at) && now - at >= 0 && now - at < YouNodAgainMs)
                        continue;
                    long day;
                    bool spoke = _youSpokeDay.TryGetValue(w.Id, out day) && day == g / 1440;
                    Passing.Act a = Passing.ToYou(_engine.Shadow.Felt(w.Id), w.P.Woman, w.Src != null ? w.Src.Temper : null, g, w.Id + "|" + (g / 60), spoke);   // the cheap test first
                    if (a == null)
                        continue;
                    Ped p = new Ped(h);
                    if (p.Position.DistanceTo(me.Position) > YouNodMetres || !Facing(p, me.Position, 70f) || !NodInView(me, p) || SightCant(p, me) != null)
                        continue;
                    if (_youNodAt.Count > 500)
                        foreach (string k in _youNodAt.Where(kv => now - kv.Value >= YouNodAgainMs || now < kv.Value).Select(kv => kv.Key).ToList())
                            _youNodAt.Remove(k);   // only the cooldowns that have run out
                    _youNodAt[w.Id] = now;
                    if (a.Speaks)
                    {
                        if (_youSpokeDay.Count > 500)
                            _youSpokeDay.Clear();
                        _youSpokeDay[w.Id] = g / 1440;
                    }
                    if (a.LookMs > 0)
                        TASK.TASK_LOOK_AT_ENTITY(h, me.Handle, a.LookMs, 0, 51, 0);
                    _youNod = new YouNod { Handle = h, At = now, Id = w.Id, Name = w.Name, Gesture = a.Gesture, Thought = a.Thought, Woman = w.P.Woman, Speaks = a.Speaks };
                    _youNods++;
                    _youNodLast = w.Name + " (" + a.Kind.Substring(4) + ")";
                    Log.Detail("NOD you: " + w.Name + " " + a.Kind.Substring(4) + " about you: " + a.Gesture + (a.Speaks ? " and a greeting" : "") + ", looks " + a.LookMs + " ms");
                    return;   // one a look
                }
            }
            catch (Exception ex)
            {
                Log.Write("NOD you failed: " + ex.Message);
                _youNod = null;
                _youNodNext = now + 60000;
            }
        }

        /// The acknowledgement, spaced: the gesture (and a greeting) a moment after the look, the thought after that.
        private void YouNodPlay(Ped me, int now)
        {
            YouNod n = _youNod;
            if (!ENTITY.DOES_ENTITY_EXIST(n.Handle) || ENTITY.IS_ENTITY_DEAD(n.Handle) || now - n.At > YouNodBusyMs || now < n.At)
            {
                _youNod = null;
                return;
            }
            Ped p = new Ped(n.Handle);
            if (!n.GestureDone && now - n.At >= YouNodGestureMs)
            {
                n.GestureDone = true;
                if (MayDirectWhyNot(p) == null)
                {
                    NodGesture(p, me, n.Gesture, n.Woman, n.Id + "|you", n.Name);
                    if (n.Speaks)
                        Speech.SayAny(n.Handle, YouGreet, me.Handle);
                }
            }
            if (!n.ThoughtDone && now - n.At >= YouNodThoughtMs)
            {
                n.ThoughtDone = true;
                NodThought(p, n.Name, n.Thought);
            }
        }
    }
}
