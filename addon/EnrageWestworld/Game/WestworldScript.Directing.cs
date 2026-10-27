// Who RealNPCs may give orders to (Core/Directing.cs), with the game's and enRAGE's facts about them: the one check
// every feature that tasks, casts or gestures someone asks (street scenes, reactions, real-voice talk, waking acts, the
// rough test). enRAGE's full records take part like anyone else (Lex, 2026-10-26) unless enRAGE is driving them right
// now; the F7 line lists the records near you and what stops any of them.
using System;
using System.Collections.Generic;
using System.Linq;
using NemesisSystem;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private bool IsRecord(int h)
        {
            return _bridge != null && _bridge.RecordAt(h) != null;
        }

        /// Why we mayn't give this person orders now, or null if we may.
        private string MayDirectWhyNot(Ped p)
        {
            if (p == null || !p.Exists() || ENTITY.IS_ENTITY_DEAD(p.Handle))
                return "gone";
            int h = p.Handle;
            bool record = IsRecord(h);
            bool busy = PED.IS_PED_IN_COMBAT(h, 0) || PED.IS_PED_FLEEING(h) || PED.IS_PED_RAGDOLL(h) || PED.IS_PED_ON_MOUNT(h) || PED.IS_PED_IN_ANY_VEHICLE(h, false);
            return Directing.WhyNot(Natives.StoryModel(p), IsGrownUp(p), MISC.GET_MISSION_FLAG(), record, _cfg.EnragePeopleJoinIn,
                                    record && _bridge.EnrageFighter(h), record && _bridge.Interrogating, busy, Spawner.IsOurs(h), !record && CanDirect(p));
        }

        private bool MayDirect(Ped p)
        {
            return MayDirectWhyNot(p) == null;
        }

        private const int RecordStalkMs = 20 * 60000;
        private bool _stalkerRecord;
        private int _stalkerSince, _nextRecordCheck;

        /// Why one of enRAGE's records can't be the stalker now (or stay one), or null: the same rules as MayDirect, except
        /// that our own hold on them doesn't count, plus no enRAGE plan due near them or for their gang within the hour.
        private string RecordStalkWhyNot(Ped p)
        {
            if (p == null || !p.Exists() || ENTITY.IS_ENTITY_DEAD(p.Handle))
                return "gone";
            int h = p.Handle;
            NemesisRecord rec = _bridge.RecordAt(h);
            if (rec == null || !rec.IsAlive)
                return "not one of enRAGE's people now";
            bool busy = PED.IS_PED_IN_COMBAT(h, 0) || PED.IS_PED_FLEEING(h) || PED.IS_PED_ON_MOUNT(h) || PED.IS_PED_IN_ANY_VEHICLE(h, false);
            string why = Directing.WhyNot(Natives.StoryModel(p), IsGrownUp(p), MISC.GET_MISSION_FLAG(), true, _cfg.EnragePeopleJoinIn, _bridge.EnrageFighter(h), _bridge.Interrogating, busy, false, false);
            if (why != null)
                return why;
            long g = Bridge.Now();
            foreach (PlanInfo plan in _bridge.Plans())
                if (!plan.Done && plan.DueAt - g < 60 && g - plan.DueAt < Math.Max(60, plan.WindowMin) &&
                    ((plan.Gang != null && plan.Gang == rec.Gang) || plan.Pos.DistanceTo(p.Position) < 150f))
                    return "an enRAGE plan is due (" + (plan.Title ?? plan.Kind) + ")";
            return null;
        }

        private sealed class RecordFate
        {
            public NemesisRecord Rec;
            public string Name, History;
            public int DueAt;
        }

        private readonly List<RecordFate> _recordFates = new List<RecordFate>();
        private int _recordFatesBooked;
        private Action _aRecordFates;

        /// A record died of something RealNPCs did: booked in enRAGE five seconds on (enRAGE's own sync may mark the
        /// death first; then only the history is added).
        private void QueueRecordFate(NemesisRecord rec, string name, string history)
        {
            if (!_cfg.RecordFates || rec == null || _recordFates.Any(f => f.Rec == rec))
                return;
            _recordFates.Add(new RecordFate { Rec = rec, Name = name, History = history, DueAt = Game.GameTime + 5000 });
        }

        private void RecordFatesStep(int now)
        {
            if (_recordFates.Count == 0)
                return;
            if (_aRecordFates == null)
                _aRecordFates = () => RecordFatesTick(_now);
            Safe("record fates", _aRecordFates);
        }

        private void RecordFatesTick(int now)
        {
            for (int i = _recordFates.Count - 1; i >= 0; i--)
            {
                RecordFate f = _recordFates[i];
                if (now < f.DueAt)
                    continue;
                _recordFates.RemoveAt(i);
                bool marked = _bridge.BookDeath(f.Rec, f.History);
                _recordFatesBooked++;
                Log.Write("RECORD death booked: " + f.Name + ", " + f.History + (marked ? " (enRAGE hadn't marked the death: RealNPCs did)" : " (enRAGE saw it; its history says how)"));
            }
        }

        /// What one of enRAGE's records near you could get from RealNPCs now, and why not (F7 TEST records).
        private string RecordCould(Ped p)
        {
            string stalk = !_cfg.StalkerRecords ? "off" : RecordStalkWhyNot(p) ?? "yes";
            string despair = !_cfg.RecordFates ? "off" : _bridge.CanRevive ? "when awake and free" : "no (enRAGE's history and roster saving not found)";
            return "stalk " + stalk + "; despair " + despair + "; minds " + (_cfg.Minds && _cfg.RecordMinds ? "yes" : "off") + "; dress never (enRAGE knows them by their look)";
        }

        /// F7: enRAGE's records within 30 m: whether enRAGE marks them as mission entities, the role it has them in, and
        /// whether RealNPCs may direct them (and why not).
        private string RecordsStatus()
        {
            if (_bridge == null)
                return "enRAGE not found";
            Ped me = Game.Player.Character;
            var near = World.GetAllPeds().Where(p => p != null && p.Exists() && p.Handle != me.Handle && IsRecord(p.Handle) && p.Position.DistanceTo(me.Position) < 30f)
                                         .OrderBy(p => p.Position.DistanceTo(me.Position)).Take(8).ToList();
            if (near.Count == 0)
                return (_cfg.EnragePeopleJoinIn ? "they join in" : "they're left alone ([Scenes] EnragePeopleJoinIn off)") + "; " + _recordFatesBooked + " deaths booked this session; none within 30 m";
            return (_cfg.EnragePeopleJoinIn ? "they join in" : "they're left alone") + "; " + string.Join("; ", near.Select(p =>
            {
                string why = MayDirectWhyNot(p);
                return TalkName(p) + " (" + (int)p.Position.DistanceTo(me.Position) + " m, " + (ENTITY.IS_ENTITY_A_MISSION_ENTITY(p.Handle) ? "mission entity" : "not a mission entity") +
                       ", role " + (_bridge.RoleName(p.Handle) ?? "none") + "): " + (why == null ? "free" : why) + "; could: " + RecordCould(p);
            }));
        }
    }
}
