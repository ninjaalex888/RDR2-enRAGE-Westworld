// Hosts in the street: strangers who say odd things about their world (aloud, and with the Windows voice),
// rare glitches (someone freezes and stares, or says the same greeting three times exactly alike), and the
// stalker - someone near you with a history with you, a stranger, or someone the player killed who came back.
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
        private sealed class GlitchRun
        {
            public Ped Ped;
            public string Kind;
            public int Step;
            public int NextAt;
            public string Name;
        }

        private Stalker _stalker;
        private Fallen _stalkerFallen;
        private uint _stalkerModel;
        private string _stalkerLabel;
        private bool _stalkerWoman;
        private int _stalkerPendingUntil;
        private int _nextMusing = -1, _nextGlitch = -1, _nextStalkCheck, _stalkerCooldown;
        private int _nextStalkRoll;          // the next roll for a stalker (game ms: real time of play)
        private int _stalkDueSince;          // a roll hit, or the key was pressed, at this time; 0 = none due
        private bool _stalkDueTest;          // ...by the key: no waiting long for someone to turn
        private string _stalkWait;           // what a due stalker is waiting for, as last logged
        private bool _stalkerRequested, _stalkerIsTest;
        private int _stalkFails;
        private KeySpec _stalkerKey;
        private GlitchRun _glitch;

        private void HostsTick(int now)
        {
            if (_glitch != null)
            {
                GlitchStep(now);
                return;
            }
            if (!_cfg.HostsQuestionReality || Feed.Busy || _director.Busy)
                return;
            if (_nextMusing < 0)
                _nextMusing = now + 60000 + _rng.Next(60000);
            if (now >= _nextMusing)
            {
                int interval = Math.Max(60000, Math.Min(1200000, 240000 * 15 / Math.Max(1, _cfg.RealityChance)));
                _nextMusing = now + interval / 2 + _rng.Next(interval);
                StrangerMusing();
            }
            if (_cfg.Glitches)
            {
                if (_nextGlitch < 0)
                    _nextGlitch = now + 300000 + _rng.Next(300000);
                if (now >= _nextGlitch)
                {
                    _nextGlitch = now + 420000 + _rng.Next(480000);
                    StartGlitch(now);
                }
            }
        }

        /// A bystander, close and in sight, who isn't one of enRAGE's people.
        private Ped Bystander(float min, float max, bool mustSee)
        {
            Ped me = Game.Player.Character;
            List<Ped> pool = World.GetAllPeds().Where(p => Natives.Bystander(p, me.Handle) && !Spawner.IsOurs(p.Handle) && _bridge.RecordAt(p.Handle) == null && !_wakeActs.Any(w => w.Handle == p.Handle) &&
                                                         !PED.IS_PED_IN_ANY_VEHICLE(p.Handle, false)).ToList();
            pool = pool.Where(p =>
            {
                float d = p.Position.DistanceTo(me.Position);
                return d >= min && d <= max && (!mustSee || ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(me.Handle, p.Handle, 17));
            }).ToList();
            return pool.Count == 0 ? null : pool[_rng.Next(pool.Count)];
        }

        private bool StrangerMusing()
        {
            // only someone who has begun to wake mutters about the world (Lex, 2026-10-29: West World thoughts not for the unawake)
            Ped me = Game.Player.Character;
            Ped p = World.GetAllPeds().Where(x => Natives.Bystander(x, me.Handle) && !Spawner.IsOurs(x.Handle) && x.Position.DistanceTo(me.Position) >= 3f && x.Position.DistanceTo(me.Position) <= 14f &&
                                                  _resBind.ContainsKey(x.Handle) && !_resBind[x.Handle].R.Dead && _engine.StageOf(_resBind[x.Handle].R.Id) > 0 &&
                                                  ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(me.Handle, x.Handle, 17)).OrderBy(x => _rng.Next()).FirstOrDefault();
            if (p == null)
                return false;
            bool woman = !PED.IS_PED_MALE(p.Handle);
            int tier = _store.Reveries.Values.Count(r => r.Woke) >= 2 || _store.Fallen.Count(f => f.Returned > 0) >= 1 ? 1 : 0;
            string line = Hosts.StrangerAloud(tier, p.Handle + "|" + Game.GameTime / 60000);
            string label = Grammar.UpperFirst(Director.Kind(p, woman));
            TASK.TASK_LOOK_AT_ENTITY(p.Handle, Game.Player.Character.Handle, 5000, 0, 51, 0);
            Feed.Say(label + ", to nobody in particular", line, Speech.For(p.Handle, line, 0, 0, 0, false, "uneasy"), p.Handle);   // their own voice, unsettled
            Log.Write("MUSING " + label + ": " + line);
            return true;
        }

        private void StartGlitch(int now)
        {
            Ped p = Bystander(3f, 16f, true);
            if (p == null)
                return;
            bool woman = !PED.IS_PED_MALE(p.Handle);
            _glitch = new GlitchRun { Ped = p, Kind = _rng.Next(2) == 0 ? "loop" : "freeze", NextAt = now, Name = Grammar.UpperFirst(Director.Kind(p, woman)) };
            Log.Write("GLITCH " + _glitch.Kind + ": " + _glitch.Name);
        }

        private void GlitchStep(int now)
        {
            GlitchRun g = _glitch;
            if (g.Ped == null || !g.Ped.Exists() || g.Ped.IsDead || g.Step > 6)
            {
                _glitch = null;
                return;
            }
            if (now < g.NextAt)
                return;
            int h = g.Ped.Handle, me = Game.Player.Character.Handle;
            if (g.Kind == "loop")
            {
                if (g.Step == 0)
                {
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, me, 6000, 0f, 0f, 0f);
                    TASK.TASK_LOOK_AT_ENTITY(h, me, 8000, 0, 51, 0);
                }
                if (g.Step < 3)
                {
                    // the same line, three times over, exactly alike
                    Speech.Say(h, "GREET_GENERAL_STRANGER", me);
                    g.NextAt = now + 1500;
                }
                else
                {
                    string line = Hosts.Glitch(h + "|g");
                    Feed.Say(g.Name, line, Speech.When(h, "uneasy", me), h);
                    g.Step = 99;
                }
            }
            else
            {
                if (g.Step == 0)
                {
                    TASK.CLEAR_PED_TASKS(h, true, false);
                    TASK.TASK_STAND_STILL(h, 5000);
                    TASK.TASK_LOOK_AT_ENTITY(h, me, 6000, 0, 51, 0);
                    g.NextAt = now + 4500;
                }
                else
                {
                    string line = Hosts.Glitch(h + "|f");
                    Feed.Say(g.Name, line, Speech.When(h, "uneasy", me), h);
                    g.Step = 99;
                }
            }
            g.Step++;
        }

        // ---- the stalker ------------------------------------------------------------------------------
        // Every StalkerEveryMinutes of play, a roll at StalkerChance. On a hit, someone near you (StalkerNearbyMetres)
        // becomes the stalker: the one with the most history with you (StalkerPick), never one of enRAGE's own people,
        // whom enRAGE runs itself. If nobody near can be turned for a few minutes, a stranger turns up behind you
        // instead (StalkerStrangers). The key (StalkerKey, 0) does the same at once, without the roll.

        private static string Clock(long gameMin)
        {
            int m = (int)(((gameMin % 1440) + 1440) % 1440);
            return (m / 60).ToString("00") + ":" + (m % 60).ToString("00");
        }

        /// What keeps a stalker from turning up right now, in words for the log; null when nothing does.
        private string StalkBlocker()
        {
            Ped me = Game.Player.Character;
            if (MISC.GET_MISSION_FLAG())
                return "a story mission is running";
            if (_interiorType != 0)
                return "you're indoors";
            if (PLAYER.GET_PLAYER_WANTED_LEVEL(Game.Player.Handle) > 0 || LAW.IS_LAW_INCIDENT_ACTIVE(Game.Player.Handle))
                return "lawmen are hunting you";
            if (PED.IS_PED_IN_COMBAT(me.Handle, 0))
                return "you're fighting someone";
            if (ENTITY.IS_ENTITY_IN_WATER(me.Handle))
                return "you're in the water";
            if (!Spawner.SafeToSpawn())
                return "the screen is going dark or loading";
            if (EncountersFull)
                return EncountersLive + " encounters are going on already (at most " + _cfg.EncountersAtOnce + ", [Encounters] EncountersAtOnce)";   // a stalker and the encounters share the count (Lex, 2026-10-27: up to 5 at once); before, any encounter kept a stalker away and the other way round
            return null;
        }

        /// A hermit only stands in a stalker's way while he's taken up with you (watching you come, warning, talking,
        /// running) or you're at his fire. Sat by it out of sight he isn't (Lex, 2026-10-27: in Valentine the ] key waited
        /// on Jacob Redmond's camp, 145 m off, the whole time: a camp counts as "out" within 150 m).
        private bool HermitWithYou()
        {
            HermitOut o = _hermit;
            if (o == null || o.Phase == "spawn")
                return false;
            if (o.Phase != "camp")
                return true;
            return o.Ped != null && o.Ped.Exists() && o.Ped.Position.DistanceTo(Game.Player.Character.Position) < 40f;
        }

        private void StalkerTick(int now)
        {
            if (_stalkerRequested)
            {
                _stalkerRequested = false;
                if (_stalker != null)
                {
                    Log.Write("STALKER key: " + _stalker.Label + " is already following you.");
                    News("Stalkers", _stalker.Label + " is already following you.", "news");
                }
                else
                {
                    _stalkDueSince = now;
                    _stalkDueTest = true;
                    _stalkerCooldown = 0;
                    _nextStalkCheck = 0;
                    _stalkWait = null;
                    Log.Write("STALKER key (" + _stalkerKey + "): someone near you will start following you as soon as nothing is in the way.");
                    News("Stalkers", "someone near you will start following you. Go about your business, and check behind you from time to time.", "news");
                }
            }
            if (_stalker != null)
            {
                if (_stalker.Done)
                {
                    EndStalker(now);
                    return;
                }
                if (_stalkerPendingUntil > 0 && _stalker.Handle == 0)
                {
                    if (_stalker.TrySpawn(_stalkerModel, Game.Player.Character))
                    {
                        _stalkerPendingUntil = 0;
                        _stalkFails = 0;
                    }
                    else if (now > _stalkerPendingUntil)
                    {
                        _stalkFails++;
                        int waitMin = Math.Min(10, 1 << Math.Min(4, _stalkFails - 1));
                        Log.Write("STALKER couldn't appear (" + _stalker.Why + "); trying again in " + waitMin + (waitMin == 1 ? " minute." : " minutes."));
                        if (_stalker.Revenant != null)
                            _stalker.Revenant.ReturnAt = Bridge.Now() + 1;
                        else
                        {
                            _stalkDueSince = now;   // still due: someone near may be turned by then
                            _stalkDueTest = _stalkerIsTest;
                        }
                        _stalker = null;
                        _stalkerPendingUntil = 0;
                        _stalkerCooldown = now + waitMin * 60000;
                    }
                    return;
                }
                if (_stalkerRecord && now >= _nextRecordCheck)
                {
                    // one of enRAGE's people: looked at every second, and handed back the moment enRAGE has a use for them
                    _nextRecordCheck = now + 1000;
                    Ped sp = _stalker.Handle != 0 ? new Ped(_stalker.Handle) : null;
                    string back = sp == null || !sp.Exists() ? "gone" : RecordStalkWhyNot(sp, _stalker.Fleeing) ?? (now - _stalkerSince > RecordStalkMs ? "his twenty minutes are up" : null);
                    if (back != null)
                    {
                        if (sp != null && EnragesOrders(back))
                            Spawner.LeaveTasks(sp.Handle);   // enRAGE's new orders for him stand
                        Log.Write("STALKER " + _stalkerLabel + ": one of enRAGE's people, handed back (" + back + ")");
                        _stalker.Finish(false);
                        EndStalker(now);
                        return;
                    }
                }
                _stalker.Tick(Game.Player.Character, _engine.Player, StalkerSay);
                return;
            }
            if (now < _nextStalkCheck || now < _stalkerCooldown)
                return;
            _nextStalkCheck = now + 5000;
            long gameMin = Bridge.Now();
            if (gameMin <= 0)
                return;
            string blocked = StalkBlocker();
            if (_cfg.Stalkers && _cfg.WestWorld && blocked == null)
            {
                // someone you killed, back as a stalker: a West World thing
                Fallen due = _store.Fallen.FirstOrDefault(f => !f.Done && f.ReturnAt > 0 && gameMin >= f.ReturnAt);
                if (due != null)
                {
                    due.ReturnAt = 0;
                    BeginStalker(due, due.Model, due.Woman, due.Name ?? "A familiar " + (due.Woman ? "woman" : "man"), now, false);
                    return;
                }
            }

            // the roll, every StalkerEveryMinutes of play
            int every = _cfg.StalkerEveryMinutes * 60000;
            if (_cfg.Stalkers && _stalkDueSince == 0)
            {
                if (_nextStalkRoll == 0 || _nextStalkRoll - now > every)
                    _nextStalkRoll = now + every;   // the first one, or the interval was made shorter
                else if (now >= _nextStalkRoll)
                {
                    _nextStalkRoll = now + every;
                    bool hit = _rng.Next(100) < _cfg.StalkerChance;
                    Log.Write("Stalker roll (" + _cfg.StalkerChance + "% every " + _cfg.StalkerEveryMinutes + " min): " + (hit ? "someone near you will start following you." : "nobody this time."));
                    if (hit)
                    {
                        _stalkDueSince = now;
                        _stalkDueTest = false;
                        _stalkWait = null;
                    }
                }
            }
            if (_stalkDueSince == 0)
                return;
            if (!_cfg.Stalkers && !_stalkDueTest)
            {
                _stalkDueSince = 0;   // switched off while one was due
                return;
            }

            string wait = blocked;
            if (wait == null)
            {
                string why;
                Ped who = StalkerCandidate(out why);
                if (who != null)
                {
                    TurnStalker(who, why, now);
                    return;
                }
                // nobody near can be: the key brings a stranger at once; a roll waits a few minutes for someone first
                if (_stalkDueTest || (_cfg.StalkerStrangers && now - _stalkDueSince > 180000))
                {
                    Log.Write("STALKER: nobody near you can follow you (" + why + "); a stranger turns up behind you instead.");
                    bool woman = _rng.Next(5) == 0;
                    bool test = _stalkDueTest;
                    _stalkDueSince = 0;
                    _stalkWait = null;
                    BeginStalker(null, Spawner.ModelFor(null, woman, _rng.Next()), woman, "Someone", now, test);
                    return;
                }
                if (!_cfg.StalkerStrangers && now - _stalkDueSince > every)
                {
                    Log.Write("STALKER: nobody near you could follow you before the next roll (" + why + "); dropped.");
                    _stalkDueSince = 0;
                    _stalkWait = null;
                    return;
                }
                wait = "nobody near you who could follow you (" + why + ")";
            }
            if (wait != _stalkWait)
            {
                _stalkWait = wait;
                Log.Write("STALKER due, waiting: " + wait + ".");
                if (_stalkDueTest)
                    News("Stalkers", "stalker: waiting, " + wait + ".", "news");
            }
        }

        /// The person near you with the most history with you who can be turned, or null (why says what was there).
        private Ped StalkerCandidate(out string why)
        {
            Ped me = Game.Player.Character;
            Vector3 pos = me.Position;
            float max = _cfg.StalkerNearbyMetres;
            Ped best = null;
            int bestScore = -1;
            float bestD = 0f;
            string bestWhy = null;
            int there = 0, close = 0, enrage = 0, busy = 0, folkHeld = 0, folkBusy = 0;
            foreach (Ped ped in World.GetAllPeds())
            {
                int h = ped.Handle;
                if (h == me.Handle || Spawner.IsOurs(h) || !PED.IS_PED_HUMAN(h) || ENTITY.IS_ENTITY_DEAD(h) || PED.IS_PED_A_PLAYER(h))
                    continue;
                float d = ped.Position.DistanceTo(pos);
                if (d > max)
                    continue;
                there++;
                if (d < 12f)
                {
                    close++;   // any nearer and he'd be "caught" at once
                    continue;
                }
                RecordRef rec = _bridge.RecordAt(h);
                if (rec != null)
                {
                    // one of enRAGE's people, while it has no use for them (StalkerRecords); one who hates you is likeliest
                    string no = !_cfg.StalkerRecords ? "StalkerRecords off" : RecordStalkWhyNot(ped);
                    if (no != null || Natives.InteriorType(h) != 0 || _director.Involves(h) || _wakeActs.Any(w => w.Handle == h))
                    {
                        enrage++;
                        continue;
                    }
                    Person rp = PersonOf(rec);
                    PersonState rst = _store.Find(rp.Id);
                    int rscore = StalkerPick.Score(null, rst, false) + Math.Max(0, rp.Hate) / 2 + 5;
                    if (rscore > bestScore || (rscore == bestScore && d < bestD))
                    {
                        best = ped;
                        bestScore = rscore;
                        bestD = d;
                        bestWhy = rp.Name + ": one of enRAGE's own, hate " + rp.Hate + ", score " + rscore;
                    }
                    continue;
                }
                if (Natives.IsChild(ped) || Natives.StoryModel(ped) || Natives.IsLaw(ped) || !IsGrownUp(ped) || !CanDirect(ped) ||
                    Natives.InteriorType(h) != 0 || _director.Involves(h) || _wakeActs.Any(w => w.Handle == h))
                {
                    busy++;
                    if (_bridge.AcquaintanceAt(h) != null)
                    {
                        // a townsperson who knows you, passed over: the log says why (the regulars enRAGE puts in town are its own)
                        if (ENTITY.IS_ENTITY_A_MISSION_ENTITY(h))
                            folkHeld++;
                        else
                            folkBusy++;
                    }
                    continue;
                }
                Folk folk = null;
                PersonState st = null;
                Resident res = null;
                FolkRef a = _bridge.AcquaintanceAt(h);
                if (a != null && !string.IsNullOrEmpty(a.Name))
                {
                    folk = Snapshot.Of(a);
                    st = _store.Find(folk.Key);
                }
                else
                {
                    ResBind b;
                    if (_resBind.TryGetValue(h, out b) && b.Model == ENTITY.GET_ENTITY_MODEL(h))
                        res = b.R;   // only someone who already has a name: a new one is made for the one chosen
                    st = res != null ? _store.Find(res.Id) : null;
                }
                int score = StalkerPick.Score(folk, st, res != null);
                if (score > bestScore || (score == bestScore && d < bestD))
                {
                    best = ped;
                    bestScore = score;
                    bestD = d;
                    bestWhy = (folk != null ? folk.Name : res != null ? res.Name : "a passer-by") + ": " + StalkerPick.Why(folk, st) + ", score " + score;
                }
            }
            string skipped = folkHeld + folkBusy == 0 ? "" : "; passed over " + (folkHeld + folkBusy) + " of enRAGE's townsfolk who know you (" +
                             (folkHeld > 0 ? folkHeld + " held by enRAGE" : "") + (folkHeld > 0 && folkBusy > 0 ? ", " : "") + (folkBusy > 0 ? folkBusy + " busy, mounted or indoors" : "") + ")";
            why = best != null ? bestWhy + skipped
                : there == 0 ? "nobody within " + (int)max + " m"
                : there + " within " + (int)max + " m: " + close + " too close, " + enrage + " of enRAGE's own, " + busy + " busy, indoors, lawmen or children" + skipped;
            return best;
        }

        /// Someone near you becomes the stalker.
        private void TurnStalker(Ped who, string why, int now)
        {
            int h = who.Handle;
            bool woman = !PED.IS_PED_MALE(h);
            string label = null;
            FolkRef a = _bridge.AcquaintanceAt(h);
            if (a != null && !string.IsNullOrEmpty(a.Name))
                label = a.FullName ?? a.Name;
            else
            {
                Resident res = ResidentOf(who);   // a passer-by gets a name now; being followed by them is worth keeping
                label = res != null ? res.Name : null;
            }
            _stalkerFallen = null;
            _stalkerWoman = woman;
            _stalkerLabel = label ?? Grammar.UpperFirst(Director.Kind(who, woman));
            _stalkerIsTest = _stalkDueTest;
            _stalkerPendingUntil = 0;
            _stalkDueSince = 0;
            _stalkWait = null;
            _stalkFails = 0;
            RecordRef rec = _bridge.RecordAt(h);
            if (rec != null)
                _stalkerLabel = PersonOf(rec).Name;
            _stalkerRecord = rec != null;
            _stalkerSince = now;
            _stalker = new Stalker(null, _stalkerLabel, woman, _cfg);
            _stalker.Adopt(who, Game.Player.Character, why, rec != null);
            Log.Write("STALKER begins: " + _stalkerLabel + (_stalkerIsTest ? " (key)" : "") + ", game time " + Clock(Bridge.Now()));
        }

        private void BeginStalker(Fallen f, uint model, bool woman, string label, int now, bool test)
        {
            _stalkerFallen = f;
            _stalkerRecord = false;
            _stalkerModel = model;
            _stalkerWoman = woman;
            _stalkerLabel = label;
            _stalkerIsTest = test;
            _stalker = new Stalker(f, label, woman, _cfg);
            _stalkerPendingUntil = now + 25000;
            Log.Write("STALKER begins: " + label + (f != null ? " (killed " + (f.Town ?? f.Zone ?? "somewhere") + ")" : " (a stranger)") + (test ? " (key)" : "") +
                      ", game time " + Clock(Bridge.Now()));
        }

        /// One line for the F7 test dump.
        private string StalkerStatus()
        {
            int now = Game.GameTime;
            string due;
            if (_stalker != null)
                due = (_stalker.Handle == 0 ? "about to appear: " : "following you: ") + _stalker.Label;
            else if (_stalkDueSince > 0)
                due = "due" + (_stalkDueTest ? " (key)" : "") + ", waiting " + (now - _stalkDueSince) / 1000 + " s" + (_stalkWait != null ? ": " + _stalkWait : "");
            else
                due = _cfg.Stalkers && _nextStalkRoll > 0 ? "next roll in " + Math.Max(0, (_nextStalkRoll - now) / 1000) + " s" : "none waiting";
            string blocked = StalkBlocker();
            return (_cfg.Stalkers ? _cfg.StalkerChance + "% every " + _cfg.StalkerEveryMinutes + " min, someone within " + _cfg.StalkerNearbyMetres + " m" + (_cfg.StalkerStrangers ? " (else a stranger)" : "") : "off") +
                   ", " + due + ", game time " + Clock(Bridge.Now()) + ", in the way now: " + (blocked ?? "nothing") + ", failed spawns " + _stalkFails + ", key " + _stalkerKey;
        }

        private void EndStalker(int now)
        {
            Fallen f = _stalker.Revenant;
            _stalker = null;
            _stalkerRecord = false;
            _stalkerCooldown = now + 60000;
            _nextStalkRoll = now + _cfg.StalkerEveryMinutes * 60000;   // the next roll counts from when this one ends
            if (f != null)
            {
                f.Returned++;
                long g = Bridge.Now();
                if (f.Returned < 3 && _rng.Next(100) < 30)
                    f.ReturnAt = g + 1440 * (2 + _rng.Next(4));
                else
                    f.Done = f.Returned >= 3 || _rng.Next(100) < 50;
                if (_cfg.Letters && f.Returned == 1 && _rng.Next(100) < 60)
                    _engine.QueueLetter(Letters.RevenantNote(f, _engine.Player, g));
                News("Revenant", (f.Name ?? "Someone you killed" + (f.Town != null ? " in " + f.Town : "")) + " has been following you.");
            }
        }

        private void StalkerSay(string label, string line, bool revenant)
        {
            // his own voice, from wherever he is: a recorded line that fits, else a taunt
            int h = _stalker != null ? _stalker.Handle : 0;
            Feed.Say(label + ", somewhere behind you", line, h != 0 ? Speech.For(h, line, Game.Player.Character.Handle, 40, 0, false, "mock") : null, toYou: true);
            Log.Write("STALKER " + label + ": " + line);
        }
    }
}
