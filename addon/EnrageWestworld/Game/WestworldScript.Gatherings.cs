// The gathering, in the game (the hours, the ring, the creep and the lines are in Core/Encounters.cs). After midnight
// in the wilds, on a roll, four to seven of the awake stand in a ring 45-70 m off, out of sight, facing in: residents
// who are unravelling or awake, in the looks they were met in, made up with strangers. Within 30 m they all turn
// their heads to you. While none of them is on your screen they step closer, never nearer than a few metres; the
// moment one is seen, all of them stop dead. Step into the middle and they speak at once. Draw on one, shoot, or hit
// one and they scatter. They never attack, and killing one counts as a killing (Watch.cs lets their deaths through).
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
        private sealed class Gatherer
        {
            public Ped Ped;
            public Resident R;      // null for a stranger
            public uint Model;
            public bool Woman;
            public float X, Y;      // place in the ring, from the centre
        }

        private sealed class Gathering
        {
            public string Phase = "spawn";   // spawn, stand, watched, scatter
            public Vector3 Centre;
            public readonly List<Gatherer> Ring = new List<Gatherer>();
            public int StartedAt, PhaseAt, NextCreep, NextMurmur;
            public bool Moving, Spoke, Test;
        }

        private Gathering _gathering;

        private bool IsGatherer(int handle)
        {
            return _gathering != null && _gathering.Ring.Any(m => m.Ped != null && m.Ped.Handle == handle);
        }

        /// Why a gathering can't happen here and now; null when it can.
        private string GatherNotNow()
        {
            string town = _bridge.TownOnly(Game.Player.Character.Position);   // TownAt gives the region out in the wilds
            if (town != null)
                return "you're in " + town + " (only out in the wilds)";
            int hour = CLOCK.GET_CLOCK_HOURS(), minute = CLOCK.GET_CLOCK_MINUTES();
            if (!Encounters.GatherHours(hour, minute))
                return "it's " + hour.ToString("00") + ":" + minute.ToString("00") + " (from midnight to 03:30)";
            if (DoneToday("gather"))
                return "there was one already tonight";
            return null;
        }

        private bool StartGathering(int now, bool test = false)
        {
            Ped me = Game.Player.Character;
            Vector3 centre;
            if (!EncounterSpot(me, 45f, 70f, out centre))
            {
                Log.Write("ENCOUNTER gathering: nowhere out of sight for one");
                return false;
            }
            int n = Encounters.GatherSize(_rng.Next(100));
            Gathering g = new Gathering { Centre = centre, StartedAt = now, Test = test };
            // the awake first, as they looked when they were met; strangers make up the rest
            List<Resident> awake = _store.Residents.Where(r => !r.Dead && r.PostId == null && r.Cast == null && (_hermit == null || _hermit.R != r) && _engine.StageOf(r.Id) >= Awakening.Unravelling)
                .OrderBy(r => _rng.Next()).Take(n).ToList();
            for (int i = 0; i < n; i++)
            {
                Resident r = i < awake.Count ? awake[i] : null;
                bool woman = r != null ? r.Woman : _rng.Next(100) < 35;
                float dx, dy;
                Encounters.CirclePoint(i, n, Encounters.CircleRadius, out dx, out dy);
                g.Ring.Add(new Gatherer { R = r, Woman = woman, Model = r != null ? r.Model : Spawner.ModelFor(null, woman, _rng.Next()), X = dx, Y = dy });
            }
            _gathering = g;
            if (!test)
                Pacing.NoteBig(_store.Pace, _store.LastGameMin);   // a big moment: the quiet starts (Pacing.cs)
            Log.Write("ENCOUNTER gathering: " + n + " in a ring " + (int)centre.DistanceTo(me.Position) + " m away, " + awake.Count + " of them people who woke" +
                      (awake.Count > 0 ? " (" + string.Join(", ", awake.Select(r => r.Name)) + ")" : ""));
            return true;
        }

        private void GatherSpawn(int now)
        {
            Gathering g = _gathering;
            if (now - g.StartedAt > 25000)
            {
                EndGathering("they couldn't be put there");
                return;
            }
            if (now - g.StartedAt > 10000)
                foreach (Gatherer m in g.Ring)
                    if (m.R != null && m.Model == m.R.Model && !Spawner.Ready(m.Model))
                        m.Model = Spawner.ModelFor(null, m.Woman, _rng.Next());   // their own looks won't load: someone like them
            bool ready = true;
            foreach (Gatherer m in g.Ring)
                ready &= Spawner.Ready(m.Model);
            if (!ready || CampInView(g.Centre))
                return;   // never seen arriving
            foreach (Gatherer m in g.Ring)
            {
                float x = g.Centre.X + m.X, y = g.Centre.Y + m.Y;
                Vector3 at = new Vector3(x, y, Natives.GroundZ(x, y, g.Centre.Z));
                float heading = Encounters.Heading(-m.X, -m.Y);   // facing in
                Ped p = Spawner.Spawn(m.Model, at, heading);
                if (p == null)
                    continue;
                int h = p.Handle;
                if (ENTITY.IS_ENTITY_IN_WATER(h))
                {
                    Spawner.Release(p, true);
                    continue;
                }
                m.Ped = p;
                WEAPON.REMOVE_ALL_PED_WEAPONS(h, true, true);
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, true);
                PED.SET_PED_KEEP_TASK(h, true);
                ENTITY.SET_ENTITY_HEADING(h, heading);
                TASK.TASK_STAND_STILL(h, -1);
            }
            g.Ring.RemoveAll(m => m.Ped == null);
            if (g.Ring.Count < 3)
            {
                EndGathering("too few of them could stand there");
                return;
            }
            g.Phase = "stand";
            g.PhaseAt = now;
            Log.Write("ENCOUNTER gathering: standing, " + g.Ring.Count + " of them, " + (int)g.Centre.DistanceTo(Game.Player.Character.Position) + " m away");
        }

        private void GatherStep(int now)
        {
            Gathering g = _gathering;
            Ped me = Game.Player.Character;
            if (MISC.GET_MISSION_FLAG() || me.IsDead)
            {
                EndGathering(MISC.GET_MISSION_FLAG() ? "a mission started" : "you died");
                return;
            }
            if (g.Phase == "spawn")
            {
                GatherSpawn(now);
                return;
            }
            foreach (Gatherer m in g.Ring)
                if (m.R != null && !m.R.Dead && m.Ped.Exists() && m.Ped.IsDead)
                {
                    LedgerNote(m.Ped.Handle, m.R.Id, m.R.Name, "resident", null, null);   // marks them dead too (Engine.NoteDied)
                    m.R.Dead = true;
                    _engine.KeepResident(m.R, Bridge.Now());
                }
            List<Gatherer> live = g.Ring.Where(m => m.Ped.Exists() && !m.Ped.IsDead).ToList();
            if (live.Count == 0)
            {
                EndGathering("none of them is left");
                return;
            }
            Vector3 pos = me.Position;
            float toCentre = g.Centre.DistanceTo(pos);
            float nearest = live.Min(m => m.Ped.Position.DistanceTo(pos));
            if (Math.Min(toCentre, nearest) > 150f)
            {
                EndGathering("you rode away");
                return;
            }
            bool seen = live.Any(m => ENTITY.IS_ENTITY_ON_SCREEN(m.Ped.Handle) && ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(me.Handle, m.Ped.Handle, 17));
            if (g.Phase == "scatter")
            {
                if ((!seen && nearest > 40f) || now - g.PhaseAt > 45000)
                    EndGathering("they scattered");
                return;
            }
            if (now - g.StartedAt > 10 * 60000 && !seen)
            {
                EndGathering("the night moved on");
                return;
            }
            // draw on one, hurt one, or fire near them, and the ring breaks
            int aimedAt;
            bool threat = g.Ring.Any(m => m.Ped.Exists() && (m.Ped.IsDead || ENTITY.HAS_ENTITY_BEEN_DAMAGED_BY_ENTITY(m.Ped.Handle, me.Handle, true, false) ||
                                                              PLAYER.IS_PLAYER_FREE_AIMING_AT_ENTITY(Game.Player.Handle, m.Ped.Handle) ||
                                                              (_aimedAt.TryGetValue(m.Ped.Handle, out aimedAt) && now - aimedAt < 1500)));
            if (threat || (PED.IS_PED_SHOOTING(me.Handle) && nearest < 40f))
            {
                Scatter(g, live, me, now);
                return;
            }
            if (g.Phase == "stand")
            {
                if (toCentre >= 30f)
                    return;
                g.Phase = "watched";   // every head turns to you; the bodies stay facing in
                g.PhaseAt = now;
                g.NextCreep = now + 3000;
                g.NextMurmur = now + 5000;
                foreach (Gatherer m in live)
                    TASK.TASK_LOOK_AT_ENTITY(m.Ped.Handle, me.Handle, -1, 0, 51, 0);
                Log.Write("ENCOUNTER gathering: they turn their heads to you");
            }
            if (!g.Spoke && toCentre < 1.6f)
            {
                // in the middle of them: all of them at once
                g.Spoke = true;
                g.Moving = false;
                foreach (Gatherer m in live)
                {
                    int h = m.Ped.Handle;
                    TASK.CLEAR_PED_TASKS(h, true, false);
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, me.Handle, 3000, 0f, 0f, 0f);
                    TASK.TASK_LOOK_AT_ENTITY(h, me.Handle, -1, 0, 51, 0);
                    Speech.SayAny(h, Speech.Hum, me.Handle);   // a wordless sound from every one of them at once
                }
                Feed.Say("All of them, at once", Encounters.Pick(Encounters.GatherTogether, "gather|" + NightIndex()), toYou: true);
                Log.Write("ENCOUNTER gathering: you stood in the middle; they spoke at once");
                g.NextCreep = now + 5000;
                return;
            }
            if (seen)
            {
                if (g.Moving)
                {
                    // caught moving: every one of them stops dead, mid-step
                    g.Moving = false;
                    foreach (Gatherer m in live)
                    {
                        int h = m.Ped.Handle;
                        TASK.CLEAR_PED_TASKS_IMMEDIATELY(h, true, false);
                        TASK.TASK_STAND_STILL(h, -1);
                        TASK.TASK_LOOK_AT_ENTITY(h, me.Handle, -1, 0, 51, 0);
                    }
                }
            }
            else if (now >= g.NextCreep && nearest < 45f)
            {
                g.NextCreep = now + 1200;
                foreach (Gatherer m in live)
                {
                    Vector3 mp = m.Ped.Position;
                    float step = Encounters.CreepStep(mp.DistanceTo(pos));
                    if (step <= 0f)
                        continue;
                    Vector3 to = mp + (pos - mp).Normalized * step;
                    TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(m.Ped.Handle, to.X, to.Y, to.Z, Natives.Brisk, -1, 0.3f, 0, 0f);
                    g.Moving = true;
                }
            }
            if (now >= g.NextMurmur && nearest < 25f)
            {
                g.NextMurmur = now + 10000 + _rng.Next(6000);
                Gatherer m = live[_rng.Next(live.Count)];
                string who = m.R != null && m.Ped.Position.DistanceTo(pos) < 10f ? m.R.Name + ", low" : "One of them, low";
                Feed.Say(who, Encounters.Pick(Encounters.GatherMurmurs, "murmur|" + now / 10000), Speech.When(m.Ped.Handle, "hum"));
            }
        }

        private void Scatter(Gathering g, List<Gatherer> live, Ped me, int now)
        {
            g.Phase = "scatter";
            g.PhaseAt = now;
            g.Moving = false;
            foreach (Gatherer m in live)
            {
                int h = m.Ped.Handle;
                TASK.CLEAR_PED_TASKS(h, true, false);
                TASK.TASK_SMART_FLEE_PED(h, me.Handle, 200f, -1, 0, 3f, 0);
            }
            if (live.Count > 0)
                Feed.Say("The ring breaks", Encounters.Pick(Encounters.GatherScatter, "scatter|" + now / 1000), Speech.When(live[0].Ped.Handle, "scared", me.Handle), toYou: true);
            Log.Write("ENCOUNTER gathering: they scatter");
        }

        private void EndGathering(string why)
        {
            Gathering g = _gathering;
            _gathering = null;
            if (g == null)
                return;
            if (g.Phase != "spawn")
                Safe("world events", () => RecordEvent("encounter:gathering", null, "the awake", "player", "you", g.Centre, 0, 0, 0, why));
            Ped me = Game.Player.Character;
            foreach (Gatherer m in g.Ring)
            {
                if (m.Ped == null)
                    continue;
                // the living vanish only where nobody sees it; the dead stay where they fell
                bool alive = m.Ped.Exists() && !m.Ped.IsDead;
                bool seen = alive && ENTITY.IS_ENTITY_ON_SCREEN(m.Ped.Handle);
                Spawner.Release(m.Ped, alive && (!seen || m.Ped.Position.DistanceTo(me.Position) > 120f));
            }
            if (g.Phase != "spawn" && !g.Test)
                MarkDone("gather");   // nowhere to put them doesn't use up the night, nor does a test
            Log.Write("ENCOUNTER gathering: over (" + why + ")");
        }

        /// One of the ring on the ticker (the add-on's own are skipped there otherwise).
        private void GatherNear(Ped ped, float d)
        {
            Gatherer m = _gathering.Ring.FirstOrDefault(x => x.Ped != null && x.Ped.Handle == ped.Handle);
            if (m == null)
                return;
            long now = Bridge.Now();
            Resident r = m.R;
            PersonState st = r != null ? _store.Find(r.Id) : null;
            _near.Add(new NearThought
            {
                Handle = ped.Handle,
                Key = r != null ? r.Id : "ring:" + ped.Handle,
                Name = r != null ? r.Name : "One of the ring",
                Distance = d + 2f,
                Text = () => st != null ? Hosts.Thought(_engine.PersonOfResident(r), st, _engine.Player, r.Id + "|" + now / 180)
                                        : Encounters.Pick(Encounters.GatherThoughts, ped.Handle + "|" + now / 180)
            });
        }

        private string GatheringsStatus()
        {
            if (!_cfg.Gatherings)
                return "off";
            int awake = _store.Residents.Count(r => !r.Dead && r.PostId == null && _engine.StageOf(r.Id) >= Awakening.Unravelling);
            return _cfg.GatheringChance + "% per roll (00:00 - 03:30, the wilds), " + awake + " unravelling or awake to stand in one" +
                   (_gathering != null ? ", out: " + _gathering.Ring.Count + " (" + _gathering.Phase + ")" : "") + (DoneToday("gather") ? ", one already tonight" : "");
        }
    }
}
