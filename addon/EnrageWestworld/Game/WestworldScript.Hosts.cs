// Hosts in the street: strangers who say odd things about their world (aloud, and with the Windows voice),
// rare glitches (someone freezes and stares, or says the same greeting three times exactly alike), and the
// stalker - a stranger, or someone the player killed who came back.
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
        private sealed class StalkSlot { public long At; public bool Day, Test; }
        private readonly System.Collections.Generic.List<StalkSlot> _stalkSlots = new System.Collections.Generic.List<StalkSlot>();
        private int _stalkDay = -1;          // the stalker "day" last rolled for (days run 05:00 to 05:00)
        private string _stalkWait;           // what a due stalker is waiting for, as last logged
        private bool _stalkerRequested, _stalkerIsTest, _stalkerIsDay;
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
            List<Ped> pool = World.GetAllPeds().Where(p => Natives.Bystander(p, me.Handle) && !Spawner.IsOurs(p.Handle) && _bridge.RecordAt(p.Handle) == null &&
                                                         !PED.IS_PED_IN_ANY_VEHICLE(p.Handle, false)).ToList();
            pool = pool.Where(p =>
            {
                float d = p.Position.DistanceTo(me.Position);
                return d >= min && d <= max && (!mustSee || ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(me.Handle, p.Handle, 17));
            }).ToList();
            return pool.Count == 0 ? null : pool[_rng.Next(pool.Count)];
        }

        private void StrangerMusing()
        {
            Ped p = Bystander(3f, 14f, true);
            if (p == null)
                return;
            bool woman = !PED.IS_PED_MALE(p.Handle);
            int tier = _store.Reveries.Values.Count(r => r.Woke) >= 2 || _store.Fallen.Count(f => f.Returned > 0) >= 1 ? 1 : 0;
            string line = Hosts.StrangerAloud(tier, p.Handle + "|" + Game.GameTime / 60000);
            string label = Grammar.UpperFirst(Director.Kind(p, woman));
            TASK.TASK_LOOK_AT_ENTITY(p.Handle, Game.Player.Character.Handle, 5000, 0, 51, 0);
            Feed.Say(label + ", to nobody in particular", line);
            Tts.Say(line, woman, Natives.Night() ? 0.8f : 0.9f, true, "ped" + p.Handle);
            Log.Write("MUSING " + label + ": " + line);
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
                    Feed.Say(g.Name, line);
                    Tts.Say(line, !PED.IS_PED_MALE(h), 0.8f, true, "ped" + h);
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
                    Feed.Say(g.Name, line);
                    Tts.Say(line, !PED.IS_PED_MALE(h), 0.8f, true, "ped" + h);
                    g.Step = 99;
                }
            }
            g.Step++;
        }

        // ---- the stalker ------------------------------------------------------------------------------

        /// Stalker nights run from 05:00 to 05:00, so the small hours belong to the evening before.
        private static int StalkNight(long gameMin)
        {
            return (int)((gameMin - 300) / 1440);
        }

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
                return "the screen is fading or loading";
            return null;
        }

        private void StalkerTick(int now)
        {
            if (_stalkerRequested)
            {
                _stalkerRequested = false;
                if (_stalker != null)
                {
                    Log.Write("STALKER test: " + _stalker.Label + " is already following you.");
                }
                else
                {
                    _stalkSlots.RemoveAll(s => s.Test);
                    _stalkSlots.Add(new StalkSlot { At = Math.Max(1, Bridge.Now()), Test = true });
                    _stalkerCooldown = 0;
                    _nextStalkCheck = 0;
                    _stalkWait = null;
                    Log.Write("STALKER test (" + _stalkerKey + "): someone will start following you as soon as nothing is in the way.");
                    News("Westworld", "stalker test: someone will start following you soon. Go about your business, and check behind you from time to time.", "news");
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
                        if (_stalker.Revenant == null && !_stalkerIsTest)
                        {
                            if (_stalkerIsDay)
                                _store.LastDayStalkDay = StalkNight(Bridge.Now());
                            else
                                _store.LastStalkDay = StalkNight(Bridge.Now());
                        }
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
                            _stalkSlots.Add(new StalkSlot { At = Math.Max(1, Bridge.Now()), Day = _stalkerIsDay, Test = _stalkerIsTest });
                        }
                        _stalker = null;
                        _stalkerPendingUntil = 0;
                        _stalkerCooldown = now + waitMin * 60000;
                    }
                    return;
                }
                _stalker.Tick(Game.Player.Character, _engine.Player, StalkerSay);
                return;
            }
            if ((!_cfg.Stalkers && !_stalkSlots.Any(s => s.Test)) || now < _nextStalkCheck || now < _stalkerCooldown)
                return;
            _nextStalkCheck = now + 5000;
            long gameMin = Bridge.Now();
            if (gameMin <= 0)
                return;
            string blocked = StalkBlocker();
            if (_cfg.Stalkers && blocked == null)
            {
                Fallen due = _store.Fallen.FirstOrDefault(f => !f.Done && f.ReturnAt > 0 && gameMin >= f.ReturnAt);
                if (due != null)
                {
                    due.ReturnAt = 0;
                    BeginStalker(due, due.Model, due.Woman, due.Name ?? "A familiar " + (due.Woman ? "woman" : "man"), now, false, false);
                    return;
                }
            }
            int hour = CLOCK.GET_CLOCK_HOURS();
            bool daylight = hour >= 5 && hour < 18;
            int night = StalkNight(gameMin);
            if (_cfg.Stalkers && night != _stalkDay)
            {
                _stalkDay = night;
                // a daytime stalker who never got a clear moment doesn't carry over into the evening
                if (_stalkSlots.RemoveAll(s => s.Day && !s.Test) > 0)
                    Log.Write("A daytime stalker never got a clear moment before dusk; dropped.");
                RollStalkers(night, gameMin, daylight);
            }
            if (_stalkSlots.Count == 0)
                return;
            string firstWait = null;
            foreach (StalkSlot s in _stalkSlots.OrderByDescending(x => x.Test).ThenBy(x => x.At).ToList())
            {
                if (gameMin < s.At)
                    continue;
                if (s.Day && !s.Test && !daylight)
                {
                    _stalkSlots.Remove(s);
                    Log.Write("A daytime stalker never got a clear moment before dusk; dropped.");
                    continue;
                }
                string wait = blocked ?? (!s.Test && !s.Day && daylight ? "it's daytime, and tonight's stalker comes out between 18:00 and 05:00" : null);
                if (wait != null)
                {
                    if (firstWait == null)
                        firstWait = wait;
                    continue;
                }
                _stalkWait = null;
                _stalkSlots.Remove(s);
                bool woman = _rng.Next(5) == 0;
                BeginStalker(null, Spawner.ModelFor(null, woman, _rng.Next()), woman, "Someone", now, s.Test, s.Day);
                return;
            }
            if (firstWait != null && firstWait != _stalkWait)
            {
                _stalkWait = firstWait;
                Log.Write("STALKER due, waiting: " + firstWait + ".");
                if (_stalkSlots.Any(s => s.Test))
                    News("Westworld", "stalker test: waiting, " + firstWait + ".", "news");
            }
        }

        /// Once per in-game day: none, a daytime stalker, a night stalker, or both.
        private void RollStalkers(int night, long gameMin, bool daylight)
        {
            if (_store.LastStalkDay == night || _store.LastDayStalkDay == night)
            {
                Log.Detail("Stalker roll: someone already followed you this day.");
                return;
            }
            if (_rng.Next(100) >= _cfg.StalkerChance)
            {
                Log.Write("Stalker roll (" + _cfg.StalkerChance + "%): none today.");
                return;
            }
            int wd = _cfg.StalkerDayOnly, wn = _cfg.StalkerNightOnly, wb = _cfg.StalkerBoth;
            if (wd + wn + wb <= 0)
                wn = 1;
            int r = _rng.Next(wd + wn + wb);
            bool day = r < wd || r >= wd + wn;          // day only, or both
            bool nite = r >= wd;                        // night only, or both
            string what = day && nite ? "one by day and one at night" : day ? "a daytime stalker" : "a night stalker";
            string msg = "Stalker roll (" + _cfg.StalkerChance + "%): " + what;
            if (day)
            {
                if (daylight)
                {
                    // 06:00 to 16:59
                    long at = night * 1440L + 300 + 60 + _rng.Next(11 * 60);
                    _stalkSlots.Add(new StalkSlot { At = at, Day = true });
                    msg += "; the daytime one around " + Clock(at) + (gameMin >= at ? " (due now)" : "");
                }
                else
                {
                    msg += "; the daytime part has already passed";
                }
            }
            if (nite && !_stalkSlots.Any(s => !s.Day && !s.Test))
            {
                // 18:00 to 03:59
                long at = night * 1440L + 300 + 13 * 60 + _rng.Next(10 * 60);
                _stalkSlots.Add(new StalkSlot { At = at });
                msg += "; the night one around " + Clock(at) + (gameMin >= at ? " (due now)" : "");
            }
            Log.Write(msg + ".");
        }

        private void BeginStalker(Fallen f, uint model, bool woman, string label, int now, bool test, bool day)
        {
            _stalkerFallen = f;
            _stalkerModel = model;
            _stalkerWoman = woman;
            _stalkerLabel = label;
            _stalkerIsTest = test;
            _stalkerIsDay = day;
            _stalker = new Stalker(f, label, woman, _cfg.StalkerMinutes);
            _stalkerPendingUntil = now + 25000;
            Log.Write("STALKER begins: " + label + (f != null ? " (killed " + (f.Town ?? f.Zone ?? "somewhere") + ")" : "") + (test ? " (test)" : day ? " (daytime)" : f == null ? " (night)" : "") +
                      ", game time " + Clock(Bridge.Now()));
        }

        /// One line for the F7 test dump.
        private string StalkerStatus()
        {
            long g = Bridge.Now();
            string due;
            if (_stalker != null)
                due = (_stalker.Handle == 0 ? "about to appear: " : "following you: ") + _stalker.Label;
            else if (_stalkSlots.Count > 0)
                due = string.Join("; ", _stalkSlots.OrderBy(s => s.At).Select(s => (s.Test ? "test" : s.Day ? "day" : "night") + " stalker " +
                    (g >= s.At ? "due now" : "due at " + Clock(s.At) + (StalkNight(s.At) != StalkNight(g) ? " on a later night" : ""))).ToArray());
            else
            {
                int d = StalkNight(g);
                due = _store.LastStalkDay == d || _store.LastDayStalkDay == d ? "already came today" : "none waiting";
            }
            string blocked = StalkBlocker();
            return (_cfg.Stalkers ? _cfg.StalkerChance + "% a day (day/night/both " + _cfg.StalkerDayOnly + "/" + _cfg.StalkerNightOnly + "/" + _cfg.StalkerBoth + ")" : "off") + ", " + due + ", game time " + Clock(g) +
                   ", in the way now: " + (blocked ?? "nothing") + ", failed spawns " + _stalkFails + ", test key " + _stalkerKey;
        }

        private void EndStalker(int now)
        {
            Fallen f = _stalker.Revenant;
            _stalker = null;
            _stalkerCooldown = now + 600000;
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
            Feed.Say(label + ", somewhere behind you", line);
            Tts.Say(line, _stalkerWoman, 0.55f, true, label);
            Log.Write("STALKER " + label + ": " + line);
        }
    }
}
