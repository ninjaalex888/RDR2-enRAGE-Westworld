// Who RealNPCs may give orders to (Core/Directing.cs), with the game's and enRAGE's facts about them: the one check
// every feature that tasks, casts or gestures someone asks (street scenes, reactions, real-voice talk, waking acts, the
// rough test). enRAGE's full records take part like anyone else (Lex, 2026-10-26) unless enRAGE is driving them right
// now; the F7 line lists the records near you and what stops any of them.
using System;
using System.Collections.Generic;
using System.Linq;
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
            RecordRef rec = _bridge.RecordAt(h);
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
        private string RecordFreeWhyNot(Ped p, RecordRef rec)
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
            public RecordRef Rec;
            public string Id, Name, History;
            public int Handle, DueAt, Since, Last, Watched;   // Watched: ms with the player in control since (enRAGE's world loop runs then)
            public long GameAt, PresumedAt;
            public bool WasPresumed;                          // "presumed dead" already when he died (a man come back): only a new one counts
            public bool Interrupted;                          // a loading screen or a jump in the clock came between: never marked dead after one
            public long ClockLast;                            // the live game minute at the last look (Bridge.Now)
        }

        private const int FateWatchMs = 30000, FateGiveUpMs = 600000;

        private readonly List<RecordFate> _recordFates = new List<RecordFate>();
        private int _recordFatesBooked;
        private Action _aRecordFates;

        /// A record died of something RealNPCs did: enRAGE resolves its people's deaths itself (and may decide one lived),
        /// so we wait for it. Looked at every second from 5 s on: enRAGE marked the death -> our history line only;
        /// enRAGE decided he lived, or he got up -> nothing; still alive on its books after 30 s of the player in control
        /// (or 10 minutes in all) and the body down or gone -> enRAGE missed it: IsAlive too.
        private void QueueRecordFate(RecordRef rec, int handle, string name, string history)
        {
            if (!_cfg.RecordFates || rec == null || _recordFates.Any(f => f.Id == rec.Id))
                return;
            int now = Game.GameTime;
            _recordFates.Add(new RecordFate { Rec = rec, Id = rec.Id, Handle = handle, Name = name, History = history, DueAt = now + 5000, Since = now, Last = now, GameAt = _store.LastGameMin, ClockLast = Bridge.Now(),
                                              WasPresumed = rec.PresumedDead, PresumedAt = rec.PresumedDeadAt });
        }

        /// A loading screen (Step's quiet branch): a save may be loading, after which enRAGE's roster is that save's, so
        /// nothing still waiting is ever booked (a living man in that save must never be written dead).
        private void RecordFatesLoading()
        {
            for (int i = 0; i < _recordFates.Count; i++)
                _recordFates[i].Interrupted = true;
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
                    f.Watched += Math.Min(now - f.Last, 1000);   // a fade or a load in between counts for nothing (enRAGE settles after them)
                f.Last = now;
                if (now < f.DueAt)
                    continue;
                f.DueAt = now + 1000;
                string drop = null, booked = null;
                RecordRef rec = _bridge.RecordById(f.Id);   // re-found: a save point restored swaps the roster (never the old object: a booking into a record no longer on the roster writes nothing)
                bool there = ENTITY.DOES_ENTITY_EXIST(f.Handle);
                long clock = Bridge.Now();
                if (clock - f.ClockLast > 30)
                    f.Interrupted = true;   // the clock jumped (a save loaded, fast travel, sleep): enRAGE's roster may be another day's
                f.ClockLast = clock;
                if (_store.LastGameMin < f.GameAt || clock < f.GameAt)
                    drop = "the clock went back (a save loaded)";
                else if (f.Interrupted)
                    drop = "a loading screen or a jump in the clock came between (a save loaded, fast travel, sleep): left to enRAGE, never booked after one";
                else if (rec == null)
                    drop = "not on enRAGE's roster any more";
                else if (rec.PresumedDead && (!f.WasPresumed || rec.PresumedDeadAt != f.PresumedAt))
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

        /// F7: enRAGE's records within 100 m on the map for a minute, each labelled free or why not, and a hint naming the
        /// nearest free one (Lex, 2026-10-27: the records list was only in the log, so there was no finding one to test).
        private void MarkRecords()
        {
            if (_bridge == null)
                return;
            Ped me = Game.Player.Character;
            var near = World.GetAllPeds().Where(p => p != null && p.Exists() && p.Handle != me.Handle && !ENTITY.IS_ENTITY_DEAD(p.Handle) && IsRecord(p.Handle) &&
                                                     p.Position.DistanceTo(me.Position) < 100f)
                                         .OrderBy(p => p.Position.DistanceTo(me.Position)).Take(10).ToList();
            if (near.Count == 0)
            {
                Log.Write("RECORDS marked: none of enRAGE's records within 100 m");
                return;
            }
            int until = Game.GameTime + 60000;
            Ped free = null;
            var said = new List<string>();
            foreach (Ped p in near)
            {
                string why = MayDirectWhyNot(p) ?? RecordFreeWhyNot(p, _bridge.RecordAt(p.Handle));
                Blip b = p.AddBlip(BlipType.WhiteDot);
                b.Label = TalkName(p) + " (enRAGE): " + (why ?? "free");
                _regBlips.Add(new KeyValuePair<Blip, int>(b, until));   // taken off with the regulars' marks
                said.Add(TalkName(p) + " " + (int)p.Position.DistanceTo(me.Position) + " m: " + (why ?? "free"));
                if (why == null && free == null)
                    free = p;
            }
            Hint(free != null ? "enRAGE's records near you: " + near.Count + " on your map for a minute. Nearest free: " + TalkName(free) + ", " +
                                (int)free.Position.DistanceTo(me.Position) + " m " + Way(me.Position, free.Position) + "."
                              : "enRAGE's records near you: " + near.Count + " on your map for a minute, none free now (enRAGE has a use for them).", 9000);
            Log.Write("RECORDS marked " + near.Count + " on the map: " + string.Join("; ", said));
        }

        /// F7: enRAGE's records within 30 m: whether enRAGE marks them as mission entities, the role it has them in, and
        /// whether RealNPCs may direct them (and why not).
        private string RecordsStatus()
        {
            if (_bridge == null || !_bridge.Present)
                return "enRAGE not " + (_bridge == null ? "found" : "installed (standing alone)");
            Ped me = Game.Player.Character;
            var near = World.GetAllPeds().Where(p => p != null && p.Exists() && p.Handle != me.Handle && IsRecord(p.Handle) && p.Position.DistanceTo(me.Position) < 30f)
                                         .OrderBy(p => p.Position.DistanceTo(me.Position)).Take(8).ToList();
            if (near.Count == 0)
                return (_cfg.EnragePeopleJoinIn ? "they join in" : "they're left alone ([Scenes] EnragePeopleJoinIn off)") + "; " + _recordFatesBooked + " deaths booked this session; none within 30 m";
            return (_cfg.EnragePeopleJoinIn ? "they join in" : "they're left alone") + "; " + string.Join("; ", near.Select(p =>
            {
                string why = MayDirectWhyNot(p);
                return TalkName(p) + " (" + (int)p.Position.DistanceTo(me.Position) + " m, " + (Natives.OtherScripts(p.Handle) ? "mission entity" : "not a mission entity") +
                       ", role " + (_bridge.RoleName(p.Handle) ?? "none") + "): " + (why == null ? "free" : why) + "; could: " + RecordCould(p);
            }));
        }
    }
}
