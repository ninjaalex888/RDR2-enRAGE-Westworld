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
        private string RecordStalkWhyNot(Ped p, bool ourFlight = false)
        {
            if (p == null || !p.Exists() || ENTITY.IS_ENTITY_DEAD(p.Handle))
                return "gone";
            int h = p.Handle;
            NemesisRecord rec = _bridge.RecordAt(h);
            if (rec == null || !rec.IsAlive)
                return "not one of enRAGE's people now";
            bool busy = PED.IS_PED_IN_COMBAT(h, 0) || (PED.IS_PED_FLEEING(h) && !ourFlight) || PED.IS_PED_ON_MOUNT(h) || PED.IS_PED_IN_ANY_VEHICLE(h, false);
            string why = Directing.WhyNot(Natives.StoryModel(p), IsGrownUp(p), MISC.GET_MISSION_FLAG(), true, _cfg.EnragePeopleJoinIn, _bridge.EnrageFighter(h), _bridge.Interrogating, busy, false, false);
            if (why != null)
                return why;
            return RecordFreeWhyNot(p, rec);
        }

        /// Free of enRAGE altogether: in no role of its own but a world nemesis going about (a town man coming for you,
        /// a sighting, a survivor are enRAGE's to steer), and no plan of its due within the hour that's his, his gang's or
        /// near him. For what RealNPCs may do with a record beyond a scene: be the stalker, despair.
        private string RecordFreeWhyNot(Ped p, NemesisRecord rec)
        {
            if (p == null || rec == null)
                return "not one of enRAGE's people now";
            string role = _bridge.RoleName(p.Handle);
            if (role != null && role != "None" && role != "WorldNemesis")
                return "enRAGE has him as " + role;
            long g = Bridge.Now();
            foreach (PlanInfo plan in _bridge.Plans())
                if (!plan.Done && plan.DueAt - g < 60 && g - plan.DueAt < Math.Max(60, plan.WindowMin) &&
                    ((plan.LeaderId != null && plan.LeaderId == rec.Id) || (plan.Gang != null && plan.Gang == rec.Gang) || plan.Pos.DistanceTo(p.Position) < 150f))
                    return "an enRAGE plan is due (" + (plan.Title ?? plan.Kind) + ")";
            return null;
        }

        /// enRAGE has just given them orders of its own (a fighter's role, an interrogation, a fight): the hand-back
        /// leaves them alone. Gone, twenty minutes up, or a plan due: handed back to go about their day.
        private static bool EnragesOrders(string why)
        {
            return why != null && (why.StartsWith("enRAGE") || why.Contains("fight") || why.Contains("interrogat") || why.Contains("busy"));
        }

        private sealed class RecordFate
        {
            public NemesisRecord Rec;
            public string Id, Name, History;
            public int Handle, DueAt, Since, Last, Watched;   // Watched: ms with the player in control since (enRAGE's world loop runs then)
            public long GameAt;
        }

        private const int FateWatchMs = 30000, FateGiveUpMs = 600000;

        private readonly List<RecordFate> _recordFates = new List<RecordFate>();
        private int _recordFatesBooked;
        private Action _aRecordFates;

        /// A record died of something RealNPCs did: enRAGE resolves its people's deaths itself (and may decide one lived),
        /// so we wait for it. Looked at every second from 5 s on: enRAGE marked the death -> our history line only;
        /// enRAGE decided he lived, or he got up -> nothing; still alive on its books after 30 s of the player in control
        /// (or 10 minutes in all) and the body down or gone -> enRAGE missed it: IsAlive too.
        private void QueueRecordFate(NemesisRecord rec, int handle, string name, string history)
        {
            if (!_cfg.RecordFates || rec == null || _recordFates.Any(f => f.Id == rec.Id))
                return;
            int now = Game.GameTime;
            _recordFates.Add(new RecordFate { Rec = rec, Id = rec.Id, Handle = handle, Name = name, History = history, DueAt = now + 5000, Since = now, Last = now, GameAt = _store.LastGameMin });
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
            bool control = PLAYER.IS_PLAYER_CONTROL_ON(Game.Player.Handle);
            for (int i = _recordFates.Count - 1; i >= 0; i--)
            {
                RecordFate f = _recordFates[i];
                if (control)
                    f.Watched += now - f.Last;
                f.Last = now;
                if (now < f.DueAt)
                    continue;
                f.DueAt = now + 1000;
                string drop = null, booked = null;
                NemesisRecord rec = _bridge.RecordById(f.Id) ?? f.Rec;   // re-found: a save point restored swaps the roster
                bool there = ENTITY.DOES_ENTITY_EXIST(f.Handle);
                if (_store.LastGameMin < f.GameAt)
                    drop = "the clock went back (a save loaded)";
                else if (rec == null)
                    drop = "not on enRAGE's roster any more";
                else if (rec.PresumedDead)
                    drop = "enRAGE decided he lived (presumed dead): left to it";
                else if (!rec.IsAlive)
                {
                    drop = _bridge.BookDeath(rec, f.History, false);   // enRAGE resolved the death itself: our line says how
                    booked = "resolved";
                }
                else if (there && !ENTITY.IS_ENTITY_DEAD(f.Handle))
                    drop = "on their feet again: not dead after all";
                else if (there && KnockedOutWhy(f.Handle) != null && now - f.Since < FateGiveUpMs)
                    continue;   // only out cold so far
                else if (f.Watched >= FateWatchMs || now - f.Since >= FateGiveUpMs)
                {
                    drop = _bridge.BookDeath(rec, f.History, true);   // enRAGE never resolved it
                    booked = "marked";
                }
                else
                    continue;
                _recordFates.RemoveAt(i);
                if (drop != null)
                {
                    Log.Write("RECORD death not booked: " + f.Name + ": " + drop);
                    continue;
                }
                _recordFatesBooked++;
                Log.Write("RECORD death booked: " + f.Name + ", " + f.History + (booked == "marked" ? " (enRAGE never resolved the death: RealNPCs marked it)" : " (enRAGE resolved it; its history says how)"));
            }
        }

        /// What one of enRAGE's records near you could get from RealNPCs now, and why not (F7 TEST records).
        private string RecordCould(Ped p)
        {
            string stalk = !_cfg.StalkerRecords ? "off" : RecordStalkWhyNot(p) ?? "yes";
            string free = RecordFreeWhyNot(p, _bridge.RecordAt(p.Handle));
            string despair = !_cfg.RecordFates ? "off" : !_bridge.CanBookDeath ? "no (enRAGE's history and roster saving not found)" : free != null ? "not now: " + free : "when awake and free";
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
