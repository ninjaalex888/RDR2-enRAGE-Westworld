// The mind acts on its everyday choices (the NPC-life run, step 2; Lex, 2026-10-30: "start with the mind acting on
// choices"). Until now Choosing.Choose's answer was only written to the log ("MIND would: ... heads home"); here a changed
// choice of someone near and in view becomes what they do: a stroll, the nearest of the game's own spots for a rest, a
// meal, a drink, the card table or the work (a bench, a chair, the bar, a work point: the world's scenario points, which
// already fit where they stand), a walk off home or out of the weather, a walk clear of you or of someone they can't
// abide, a walk over to a friend or someone to talk to (a quiet talk may follow: two standing together), a wary eye on you.
// Gently: never someone sat or leaning, busy in a scene, held by enRAGE or the story; one new act every 3 s at most, six
// acting at once, and each let go to their own ways when it's done. Behind [Mind] MindLive and MindActs.
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
        private sealed class MindAct
        {
            public int Handle;
            public string Kind, Name;
            public int Until, Started, LookAt;
            public Vector3 To;
            public int Toward;          // the one they walk to (talk, greet), 0 none
            public bool Spot;           // sent to the nearest scenario point
            public string Arrive;       // at the place: "spot" (take the nearest seat or the bar), "browse" (a while at the counter), null
            public bool Arrived;
        }

        private readonly List<MindAct> _mindActs = new List<MindAct>();
        private const int MindActsAtOnce = 10, MindActGapMs = 1500;   // 6 and 3 s until 2026-10-30 (Lex: more at once)
        private int _mindActNextAt, _mindActsDone, _mindActsSkipped;
        private string _mindActLast;
        private readonly Dictionary<string, int> _mindActCount = new Dictionary<string, int>();

        private bool IsMindActing(int h)
        {
            for (int i = 0; i < _mindActs.Count; i++)
                if (_mindActs[i].Handle == h)
                    return true;
            return false;
        }

        /// A choice just changed for someone near and in view (MindChooseFor): what they do about it, if anything.
        private void MindActOn(MindWho w, int h, int now)
        {
            if (!_cfg.MindLive || !_cfg.MindActs || w.Choice == null || now < _mindActNextAt || _mindActs.Count >= MindActsAtOnce || IsMindActing(h))
                return;
            string kind = w.Choice;
            if (kind == "stand" || kind == "showoff" || Choosing.CarriesOn(kind))
                return;
            Ped p = new Ped(h);
            Ped me = Game.Player.Character;
            string why = MindActWhyNot(p, h);
            if (why != null)
            {
                _mindActsSkipped++;
                Log.Detail("MIND acts not: " + w.Name + " -> " + Choosing.WordsOf(kind) + " (" + why + ")");
                return;
            }
            Vector3 at = p.Position;
            var a = new MindAct { Handle = h, Kind = kind, Name = w.Name, Started = now, Until = now + 60000 };
            string did;
            switch (kind)
            {
                case "stroll":
                    TASK.TASK_WANDER_STANDARD(h, 10f, 10);
                    a.Until = now + 30000;
                    did = "takes a stroll";
                    break;
                case "rest": case "eat": case "drink": case "cards": case "work": case "pray":
                    // the nearest of the world's own spots: a bench or a porch outside, a chair or the bar within, a work point by
                    // the store; it may be none, and then nothing happens (Rockstar's own call: radius, then how long)
                    if (!TASK.DOES_SCENARIO_EXIST_IN_AREA(at, 15f, true, 0, false))
                    {
                        _mindActsSkipped++;
                        Log.Detail("MIND acts not: " + w.Name + " -> " + Choosing.WordsOf(kind) + " (no free spot within 15 m)");
                        return;
                    }
                    TASK._TASK_USE_NEAREST_SCENARIO_TO_COORD(h, at, 15f, 90000, false, true, false, false);
                    a.Spot = true;
                    a.LookAt = now + 6000;   // which spot they took, for the log
                    a.Until = now + 90000;
                    did = Choosing.WordsOf(kind) + " (the nearest spot)";
                    break;
                case "saloon": case "store": case "church":
                    if (MindToPlace(a, w, kind, at, now, out did))
                        break;
                    goto case "home";   // no place known in this town yet: off out of the scene, as before
                case "home": case "shelter":
                    {
                        // off out of the scene, away from you, along their own way (seeded by who they are)
                        Vector3 away = at - me.Position;
                        away.Z = 0f;
                        double turn = (Voice.Hash(w.Id + "|home") % 120 - 60) * Math.PI / 180.0;
                        Vector3 dir = away.Length() < 0.5f ? new Vector3(1f, 0f, 0f) : away.Normalized;
                        dir = new Vector3((float)(dir.X * Math.Cos(turn) - dir.Y * Math.Sin(turn)), (float)(dir.X * Math.Sin(turn) + dir.Y * Math.Cos(turn)), 0f);
                        a.To = at + dir * 60f;
                        TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(h, a.To.X, a.To.Y, a.To.Z, 1f, -1, 1f, 0, 0f);
                        a.Until = now + 60000;
                        did = Choosing.WordsOf(kind) + " (walks off)";
                        break;
                    }
                case "keepclear":
                    {
                        Vector3 away = at - me.Position;
                        away.Z = 0f;
                        Vector3 dir = away.Length() < 0.5f ? new Vector3(0f, 1f, 0f) : away.Normalized;
                        a.To = at + dir * 20f;
                        TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(h, a.To.X, a.To.Y, a.To.Z, 1f, -1, 1f, 0, 0f);
                        a.Until = now + 25000;
                        did = "keeps well clear of you";
                        break;
                    }
                case "steerclear":
                    {
                        int foe = MindFoeNear(w, h);
                        if (foe == 0)
                            return;
                        Vector3 away = at - ENTITY.GET_ENTITY_COORDS(foe, true, true);
                        away.Z = 0f;
                        Vector3 dir = away.Length() < 0.5f ? new Vector3(0f, 1f, 0f) : away.Normalized;
                        a.To = at + dir * 15f;
                        TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(h, a.To.X, a.To.Y, a.To.Z, 1f, -1, 1f, 0, 0f);
                        a.Until = now + 25000;
                        did = "steps clear of " + (MindNameOf(foe) ?? "someone they can't abide");
                        break;
                    }
                case "greet": case "talk":
                    {
                        int to = kind == "greet" ? MindFriendNear(w, h) : MindSomeoneToTalkTo(h, at);
                        if (to == 0)
                            return;
                        a.Toward = to;
                        TASK.TASK_GO_TO_ENTITY(h, to, 20000, 1f, 1.4f, 0f, 0);
                        a.Until = now + 20000;
                        did = (kind == "greet" ? "goes over to " : "goes to talk to ") + (MindNameOf(to) ?? "someone");
                        break;
                    }
                case "watch":
                    if (!_engine.SawYouViolent(w.Id, 3))
                        return;
                    TASK.TASK_LOOK_AT_ENTITY(h, me.Handle, 8000, 0, 51, 0);
                    a.Until = now + 8000;
                    did = "keeps a wary eye on you";
                    break;
                default:
                    return;
            }
            _mindActs.Add(a);
            _mindActNextAt = now + MindActGapMs;
            _mindActsDone++;
            int n;
            _mindActCount.TryGetValue(kind, out n);
            _mindActCount[kind] = n + 1;
            _mindActLast = w.Name + " " + did;
            Log.Write("MIND acts: " + w.Name + " " + did + " (" + w.ChoiceWhy + ", " + (int)at.DistanceTo(me.Position) + " m from you)");
        }

        /// Why this body mayn't be moved by its own choice now, or null.
        private string MindActWhyNot(Ped p, int h)
        {
            if (!p.Exists() || p.IsDead)
                return "gone";
            if (Natives.StoryModel(p) || !IsGrownUp(p) || Natives.IsLaw(p))
                return "the story's, a child or the law";
            if (PED.IS_PED_USING_ANY_SCENARIO(h))
                return "sat, leaning or at something already";
            if (PED.IS_PED_ON_MOUNT(h) || PED.IS_PED_IN_ANY_VEHICLE(h, false) || PED.IS_PED_IN_COMBAT(h, 0) || PED.IS_PED_FLEEING(h) || PED.IS_PED_RAGDOLL(h))
                return "riding, fighting or running";
            if (_director.Involves(h) || _director.Leaving(h) || InTalk(h) || IsAcknowledging(h) || IsOnlooking(h) || IsRescuer(h) || IsAcknowledgingYou(h) ||
                _wakeActs.Any(x => x.Handle == h) || Spawner.IsOurs(h) || (_theft != null && _theft.Thief != null && _theft.Thief.Handle == h))
                return "busy with something of RealNPCs'";
            return MayDirectWhyNot(p);
        }

        /// Each frame's look at those acting: done, gone or arrived, they're let go to their own ways.
        private void MindActsStep(int now)
        {
            for (int i = _mindActs.Count - 1; i >= 0; i--)
            {
                MindAct a = _mindActs[i];
                int h = a.Handle;
                bool gone = !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h);
                if (!gone && a.LookAt > 0 && now >= a.LookAt)
                {
                    a.LookAt = 0;
                    if (a.Spot)
                        Log.Detail("MIND acts: " + a.Name + (PED.IS_PED_USING_ANY_SCENARIO(h) ? " took a spot (scenario type " + TASK._GET_SCENARIO_POINT_TYPE_PED_IS_USING(h) + ")" : " found no spot to take"));
                }
                bool arrived = !gone && a.Toward != 0 && ENTITY.DOES_ENTITY_EXIST(a.Toward) && ENTITY.GET_ENTITY_COORDS(h, true, true).DistanceTo(ENTITY.GET_ENTITY_COORDS(a.Toward, true, true)) < 2f;
                if (arrived)
                {
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, a.Toward, 2000, 0f, 0f, 0f);   // face to face: a talk may start between them
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(a.Toward, h, 2000, 0f, 0f, 0f);
                }
                bool there = !gone && a.To != default(Vector3) && ENTITY.GET_ENTITY_COORDS(h, true, true).DistanceTo(a.To) < 3f;
                if (there && a.Arrive != null && !a.Arrived && !_director.Involves(h))
                {
                    // at the place: a seat, the bar rail or a pew nearby, or a while at the counter, then their own way
                    a.Arrived = true;
                    Vector3 p = ENTITY.GET_ENTITY_COORDS(h, true, true);
                    if (a.Arrive == "spot" && TASK.DOES_SCENARIO_EXIST_IN_AREA(p, 8f, true, 0, false))
                        TASK._TASK_USE_NEAREST_SCENARIO_TO_COORD(h, p, 8f, 150000, false, true, false, false);
                    else
                        TASK.TASK_STAND_STILL(h, 30000);
                    a.Until = now + (a.Arrive == "spot" ? 150000 : 30000);
                    Log.Detail("MIND acts: " + a.Name + " is at the " + a.Kind + (PED.IS_PED_USING_ANY_SCENARIO(h) ? ", and took a spot" : ""));
                    continue;
                }
                if (a.Arrived && !gone && now < a.Until && !_director.Involves(h))
                    continue;   // a while there
                if (gone || arrived || there || now >= a.Until || _director.Involves(h))
                {
                    _mindActs.RemoveAt(i);
                    if (!gone && there && (a.Kind == "home" || a.Kind == "shelter" || a.Kind == "saloon" || a.Kind == "store" || a.Kind == "church"))
                        TASK.TASK_WANDER_STANDARD(h, 10f, 10);   // on their way, beyond the scene
                }
            }
        }

        /// To the real place in this town: the bar (the barkeep's spot), the store's counter (the keeper's), the church's door
        /// (Churches); false when none is known here, or it's far.
        private bool MindToPlace(MindAct a, MindWho w, string kind, Vector3 at, int now, out string did)
        {
            did = null;
            Vector3 to;
            string where;
            if (kind == "church")
            {
                if (_placeTown == null || !Churches.TryGetValue(_placeTown, out to))
                    return false;
                where = "the church";
            }
            else
            {
                PlaceSpot s = PlaceSpots.Find(_store.Spots, _placeTown, PlaceSpots.KindFor(kind), at.X, at.Y);
                if (s == null)
                    return false;
                to = new Vector3(s.X, s.Y, s.Z);
                where = kind == "saloon" ? "the bar" : "the store's counter";
            }
            float d = to.DistanceTo(at);
            if (d > 150f || d < 3f)
                return false;
            a.To = to;
            a.Arrive = kind == "store" ? "browse" : "spot";
            a.Until = now + 90000;
            TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(a.Handle, to.X, to.Y, to.Z, 1f, -1, 2.5f, 0, 0f);
            did = Choosing.WordsOf(kind) + " (" + where + ", " + (int)d + " m)";
            return true;
        }

        private int MindFoeNear(MindWho w, int h)
        {
            foreach (int oh in _mindNear)
            {
                MindWho o;
                if (oh == h || !_mindWho.TryGetValue(oh, out o) || o.Id == null)
                    continue;
                SceneTie t = _store.Ties.FirstOrDefault(x => x.Has(w.Id) && x.Has(o.Id));
                if (t != null && t.Warmth < 0 && ENTITY.DOES_ENTITY_EXIST(oh))
                    return oh;
            }
            return 0;
        }

        private int MindFriendNear(MindWho w, int h)
        {
            int best = 0, warm = 0;
            foreach (int oh in _mindNear)
            {
                MindWho o;
                if (oh == h || !_mindWho.TryGetValue(oh, out o) || o.Id == null || !ENTITY.DOES_ENTITY_EXIST(oh) || _director.Involves(oh))
                    continue;
                SceneTie t = _store.Ties.FirstOrDefault(x => x.Has(w.Id) && x.Has(o.Id));
                if (t != null && t.Warmth > warm)
                {
                    warm = t.Warmth;
                    best = oh;
                }
            }
            return best;
        }

        private int MindSomeoneToTalkTo(int h, Vector3 at)
        {
            int best = 0;
            float bestD = 12f;
            foreach (int oh in _mindNear)
            {
                if (oh == h || !ENTITY.DOES_ENTITY_EXIST(oh) || ENTITY.IS_ENTITY_DEAD(oh) || _director.Involves(oh) || IsMindActing(oh) || PED.IS_PED_IN_COMBAT(oh, 0))
                    continue;
                float d = ENTITY.GET_ENTITY_COORDS(oh, true, true).DistanceTo(at);
                if (d > 2.5f && d < bestD)
                {
                    bestD = d;
                    best = oh;
                }
            }
            return best;
        }

        private string MindNameOf(int h)
        {
            MindWho o;
            return _mindWho.TryGetValue(h, out o) && o.Name != null ? o.Name : null;
        }

        /// F7: what the minds did this session.
        private string MindActsStatus()
        {
            if (!_cfg.MindLive || !_cfg.MindActs)
                return "off ([Mind] MindActs, with MindLive)";
            return _mindActsDone + " acted on their choices this session (" + (_mindActCount.Count == 0 ? "none yet" : string.Join(", ", _mindActCount.OrderByDescending(kv => kv.Value).Select(kv => kv.Key + " " + kv.Value))) +
                   "), " + _mindActs.Count + " acting now, " + _mindActsSkipped + " passed over" + (_mindActLast != null ? "; last: " + _mindActLast : "");
        }
    }
}
