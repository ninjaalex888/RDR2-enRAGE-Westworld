// A crowd makes a ring round a street fight (the NPC-life run, step 5, the fight ring; Lex, 2026-10-30: crowds joining in,
// one thing drawing the next). While two people trade blows, up to six of those within 20 m who aren't of a fearful nature
// (the mind's anxiety), free and on their feet, walk over and stand round it, about 4.5 m out, facing in, and cheer it on
// now and then; close enough that a friend of one may yet join in (WestworldScript.JoinIn.cs). When the fight is over they
// go back to their own ways. Behind [Scenes] FightCrowds and MindLive.
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
        private const int RingMost = 6;
        private const float RingFrom = 20f, RingRadius = 4.5f;
        private string _ringKey;
        private readonly List<int> _ring = new List<int>();
        private int _ringNextCheer, _ringNextLook, _rings;

        private void RingStep(int now)
        {
            List<int> a = null, b = null;
            bool on = _cfg.FightCrowds && _cfg.MindLive && _director.FightSides(out a, out b);
            if (!on)
            {
                if (_ring.Count > 0)
                    RingBreak();
                return;
            }
            string key = a[0] + "|" + b[0];
            if (key != _ringKey)
            {
                RingBreak();
                _ringKey = key;
                _ringNextLook = now;
            }
            if (!ENTITY.DOES_ENTITY_EXIST(a[0]) || !ENTITY.DOES_ENTITY_EXIST(b[0]))
                return;
            Vector3 mid = (ENTITY.GET_ENTITY_COORDS(a[0], true, true) + ENTITY.GET_ENTITY_COORDS(b[0], true, true)) * 0.5f;
            if (now >= _ringNextLook && _ring.Count < RingMost)
            {
                _ringNextLook = now + 2000;
                RingGather(mid, now);
            }
            if (now >= _ringNextCheer && _ring.Count > 0)
            {
                _ringNextCheer = now + 2500 + _rng.Next(2500);
                int h = _ring[_rng.Next(_ring.Count)];
                if (ENTITY.DOES_ENTITY_EXIST(h) && !ENTITY.IS_ENTITY_DEAD(h) && !_director.Involves(h) && ENTITY.GET_ENTITY_COORDS(h, true, true).DistanceTo(mid) < RingRadius + 3f)
                    Speech.SayTag(h, _rng.Next(3) == 0 ? "mock" : "cheer", a[0]);
            }
        }

        private void RingGather(Vector3 mid, int now)
        {
            Ped me = Game.Player.Character;
            var taken = new List<Ped>();
            foreach (Ped p in World.GetAllPeds())
            {
                if (_ring.Count + taken.Count >= RingMost)
                    break;
                if (p == null || p.Position.DistanceTo(mid) > RingFrom)
                    continue;   // the distance first
                int h = p.Handle;
                if (h == me.Handle || _ring.Contains(h) || p.IsDead || !PED.IS_PED_HUMAN(h) || Natives.IsLaw(p) || PED.IS_PED_USING_ANY_SCENARIO(h) || PED.IS_PED_IN_COMBAT(h, 0) ||
                    PED.IS_PED_FLEEING(h) || _director.Involves(h) || MindActWhyNot(p, h) != null)
                    continue;
                Traits t = TraitsAt(p, now);
                if (t == null || t.Z[Traits.Anxiety] > 0.4f)
                    continue;   // the mind's: nobody it doesn't know, and the fearful keep away
                taken.Add(p);
            }
            foreach (Ped p in taken)
            {
                int i = _ring.Count;
                _ring.Add(p.Handle);
                double ang = (i * 60 + Voice.Hash(_ringKey ?? "") % 60) * Math.PI / 180.0;
                Vector3 spot = mid + new Vector3((float)Math.Cos(ang), (float)Math.Sin(ang), 0f) * RingRadius;
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(p.Handle, true);   // the fight doesn't send them off: they came to see it
                TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(p.Handle, spot.X, spot.Y, spot.Z, 1.3f, 10000, 0.8f, 0, 0f);
                TASK.TASK_LOOK_AT_ENTITY(p.Handle, _director.CastAt(0) != null ? _director.CastAt(0).Handle : 0, 20000, 0, 51, 0);
            }
            if (taken.Count > 0)
            {
                if (_ring.Count == taken.Count)
                    _rings++;
                Log.Write("RING " + taken.Count + " gather round the fight (" + _ring.Count + " in the ring)");
            }
        }

        private void RingBreak()
        {
            foreach (int h in _ring)
                if (ENTITY.DOES_ENTITY_EXIST(h) && !ENTITY.IS_ENTITY_DEAD(h) && !_director.Involves(h))
                {
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, false);
                    TASK.TASK_WANDER_STANDARD(h, 10f, 10);
                }
            if (_ring.Count > 0)
                Log.Detail("RING breaks up (" + _ring.Count + ")");
            _ring.Clear();
            _ringKey = null;
        }

        private string RingStatus()
        {
            return !_cfg.FightCrowds ? "off ([Scenes] FightCrowds)" : _rings + " fights drew a ring this session" + (_ring.Count > 0 ? "; one now (" + _ring.Count + ")" : "");
        }
    }
}
