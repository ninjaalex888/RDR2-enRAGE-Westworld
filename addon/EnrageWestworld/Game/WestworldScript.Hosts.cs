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
        private long _stalkAt = -1;
        private int _stalkDay = -1;
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

        private void StalkerTick(int now)
        {
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
                        _stalkerPendingUntil = 0;
                    else if (now > _stalkerPendingUntil)
                    {
                        if (_stalker.Revenant != null)
                            _stalker.Revenant.ReturnAt = Bridge.Now() + 120;
                        _stalker = null;
                        _stalkerPendingUntil = 0;
                        _stalkerCooldown = now + 60000;
                        Log.Detail("STALKER couldn't find a hidden spot or the model didn't load; trying later");
                    }
                    return;
                }
                _stalker.Tick(Game.Player.Character, _engine.Player, StalkerSay);
                return;
            }
            if (!_cfg.Stalkers || now < _nextStalkCheck || now < _stalkerCooldown)
                return;
            _nextStalkCheck = now + 15000;
            Ped me = Game.Player.Character;
            if (MISC.GET_MISSION_FLAG() || _interiorType != 0 || PED.IS_PED_IN_COMBAT(me.Handle, 0) || PLAYER.GET_PLAYER_WANTED_LEVEL(Game.Player.Handle) > 0 ||
                ENTITY.IS_ENTITY_IN_WATER(me.Handle))
                return;
            long gameMin = Bridge.Now();
            Fallen due = _store.Fallen.FirstOrDefault(f => !f.Done && f.ReturnAt > 0 && gameMin >= f.ReturnAt);
            if (due != null)
            {
                due.ReturnAt = 0;
                BeginStalker(due, due.Model, due.Woman, due.Name ?? "A familiar " + (due.Woman ? "woman" : "man"), now);
                return;
            }
            int day = (int)(gameMin / 1440);
            if (day != _stalkDay)
            {
                _stalkDay = day;
                if (_store.LastStalkDay != day && _rng.Next(100) < _cfg.StalkerChance)
                {
                    int minute = (18 * 60 + _rng.Next(10 * 60)) % (24 * 60);
                    _stalkAt = day * 1440L + minute + (minute < 18 * 60 ? 1440 : 0);
                    Log.Write("Stalker roll (" + _cfg.StalkerChance + "%): tonight, around " + (minute / 60).ToString("00") + ":" + (minute % 60).ToString("00") + " game time.");
                }
                else
                {
                    _stalkAt = -1;
                    Log.Detail("Stalker roll (" + _cfg.StalkerChance + "%): not tonight.");
                }
            }
            int hour = CLOCK.GET_CLOCK_HOURS();
            if (_stalkAt > 0 && gameMin >= _stalkAt && (hour >= 18 || hour < 5))
            {
                _stalkAt = -1;
                _store.LastStalkDay = day;
                bool woman = _rng.Next(5) == 0;
                BeginStalker(null, Spawner.ModelFor(null, woman, _rng.Next()), woman, "Someone", now);
            }
        }

        private void BeginStalker(Fallen f, uint model, bool woman, string label, int now)
        {
            _stalkerFallen = f;
            _stalkerModel = model;
            _stalkerWoman = woman;
            _stalkerLabel = label;
            _stalker = new Stalker(f, label, woman, Math.Max(2, _cfg.StalkerMinutes - _rng.Next(Math.Max(1, _cfg.StalkerMinutes / 3))));
            _stalkerPendingUntil = now + 25000;
            Log.Write("STALKER begins: " + label + (f != null ? " (killed " + (f.Town ?? f.Zone ?? "somewhere") + ")" : ""));
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
