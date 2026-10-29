// Strangers at a campfire ([Encounters] Campfires; the words are Core/Campfire.cs): at night (20:00-04:00) out of the
// towns, two or three men sit round a fire 40-70 m off where you can't see it being made. Within 25 m in view, one of
// them calls you over (subtitled). Stand by the fire 3 s on foot and he welcomes you and tells a short tale, the others
// answering, four lines over twenty seconds; then he bids you good night. Ride on after the hail and he mutters. A gun on
// them: they beg off and it ends there. Now and then (JoinerChance) a traveller walks in from the dark while you
// sit and is made room for: the NPC version. Released (unseen) when it's over and you're 80 m off or two minutes have
// passed. Never two together, in the encounter framework (Pacing "campfire"). Night run 2026-10-28, item 14 (Tier 4's 3).
using System;
using System.Collections.Generic;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private sealed class CampfireRun
        {
            public readonly List<Ped> Sitters = new List<Ped>();
            public readonly List<uint> Models = new List<uint>();
            public int Fire;                 // the campfire object, 0 for none
            public Vector3 At;               // the fire
            public string Phase = "spawn", Seed;   // spawn, waiting, hailed, sat, telling, parting, done
            public int StartedAt, PhaseAt, StoppedSince, Tries, Line, NextLineAt;
            public bool Test, Joiner, JoinerDue, SitChecked;
            public Ped Walker;               // the traveller who comes in from the dark (the NPC version)
            public uint WalkerModel;
            public string[] Tale;
        }

        private CampfireRun _campfire;
        private int _campfires, _campfireSat, _campfireJoined, _campfireTests;
        private string _campfireLast;
        private const string CampfireSit = "WORLD_CAMP_FIRE_SIT_GROUND";

        /// Why not here and now: a town, the hour, a camp of ours already out.
        private string CampfireNotNow(bool test = false)
        {
            if (_placeTown != null)
                return "in a town (" + _placeTown + ")";
            if (!test && !Campfire.Hour(CLOCK.GET_CLOCK_HOURS()))
                return "the hour (eight in the evening to four)";
            if (_hermit != null)
                return "a hermit's camp is out";
            return null;
        }

        private void StartCampfire(int now, bool test = false)
        {
            int seed = _rng.Next();
            var c = new CampfireRun { StartedAt = now, PhaseAt = now, Test = test, Seed = "cf" + now, Joiner = test ? _campfireTests++ % 2 == 1 : _rng.Next(100) < Campfire.JoinerChance };
            int men = 2 + (_rng.Next(3) == 0 ? 1 : 0);
            for (int i = 0; i < men; i++)
                c.Models.Add(Spawner.ModelFor(null, false, seed + i * 7919));
            c.WalkerModel = Spawner.ModelFor(null, false, seed + 31);
            if (c.Joiner)
                Spawner.Ready(c.WalkerModel);   // asked for now, so the walker is ready when the tale is
            c.Tale = Campfire.Tale(c.Seed);
            _campfire = c;
            Log.Write("CAMPFIRE due: " + men + " men" + (c.Joiner ? ", a traveller to come in" : "") + (test ? " (a test)" : ""));
        }

        private unsafe void CampfireSpawn(CampfireRun c, Ped me, int now)
        {
            if (now - c.StartedAt > 20000 || c.Tries > 10)
            {
                EndCampfire("the fire found no place out of view");
                return;
            }
            foreach (uint m in c.Models)
                if (!Spawner.Ready(m))
                    return;
            uint fire = 0;
            bool fireReady = false;
            foreach (string m in FireModels)   // the hermit's
            {
                fire = Natives.Hash(m);
                if (!STREAMING.IS_MODEL_VALID(fire))
                    continue;
                STREAMING.REQUEST_MODEL(fire, false);
                fireReady = STREAMING.HAS_MODEL_LOADED(fire);
                break;
            }
            if (!fireReady && now - c.StartedAt < 6000)
                return;
            Vector3 at;
            if (!EncounterSpot(me, 40f, 70f, out at))
            {
                c.Tries++;
                return;
            }
            c.At = at;
            for (int i = 0; i < c.Models.Count; i++)
            {
                double ang = (i * 360.0 / c.Models.Count + 20) * Math.PI / 180.0;
                Vector3 pos = at + new Vector3((float)Math.Cos(ang) * 1.5f, (float)Math.Sin(ang) * 1.5f, 0f);
                float heading = (float)((ang * 180.0 / Math.PI + 90.0) % 360.0);   // facing the fire (the game's forward is (-sin h, cos h): the review)
                Ped p = Spawner.Spawn(c.Models[i], pos, heading);
                if (p == null)
                    continue;
                int h = p.Handle;
                WEAPON.REMOVE_ALL_PED_WEAPONS(h, true, true);
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, true);
                PED.SET_PED_KEEP_TASK(h, true);
                TASK.TASK_START_SCENARIO_IN_PLACE_HASH(h, Natives.Hash(CampfireSit), 600000, true, 0u, -1f, false);
                c.Sitters.Add(p);
                _encounterPeds.Add(h);
            }
            if (c.Sitters.Count == 0)
            {
                c.Tries++;
                return;   // no man would spawn: another spot next tick (no fire is made before the men are)
            }
            if (fireReady)
            {
                c.Fire = OBJECT.CREATE_OBJECT(fire, at.X, at.Y, at.Z, false, false, false, false, false);
                if (c.Fire != 0)
                {
                    OBJECT.PLACE_OBJECT_ON_GROUND_PROPERLY(c.Fire, false);
                    ENTITY.SET_ENTITY_AS_MISSION_ENTITY(c.Fire, true, true);
                }
                STREAMING.SET_MODEL_AS_NO_LONGER_NEEDED(fire);
            }
            if (!c.Test)
                Pacing.Note(_store.Pace, "campfire", null, null, _store.LastGameMin);
            c.Phase = "waiting";
            c.PhaseAt = now;
            Log.Write("CAMPFIRE lit " + (int)at.DistanceTo(me.Position) + " m off: " + c.Sitters.Count + " men" + (c.Fire != 0 ? "" : " (no fire: the campfire model didn't load)"));
        }

        private void CampfireStep(int now)
        {
            CampfireRun c = _campfire;
            Ped me = Game.Player.Character;
            if (me == null || !me.Exists() || me.IsDead)
            {
                EndCampfire("you died");
                return;
            }
            if (c.Phase == "spawn")
            {
                CampfireSpawn(c, me, now);
                return;
            }
            Ped host = c.Sitters.Count > 0 ? c.Sitters[0] : null;
            if (host == null || !host.Exists() || host.IsDead)
            {
                EndCampfire(host != null && host.Exists() && host.IsDead ? "the host died" : "they were gone");
                return;
            }
            int h = host.Handle;
            float d = c.At.DistanceTo(me.Position);
            if (!c.SitChecked && now - c.PhaseAt > 2500 && c.Phase == "waiting")
            {
                // the camp-fire sit wasn't proven to start in place (the hermit found the same): the plain sit instead
                c.SitChecked = true;
                if (!PED.IS_PED_USING_ANY_SCENARIO(h))
                {
                    foreach (Ped p in c.Sitters)
                        if (p != null && p.Exists())
                            TASK.TASK_START_SCENARIO_IN_PLACE_HASH(p.Handle, Natives.Hash("WORLD_HUMAN_SIT_GROUND"), 600000, true, 0u, -1f, false);
                    Log.Detail("CAMPFIRE the camp-fire sit didn't take; the plain sit instead");
                }
            }
            bool aimed = false;
            if (d < 25f)
            {
                foreach (Ped p in c.Sitters)
                    if (p != null && p.Exists() && PLAYER.IS_PLAYER_FREE_AIMING_AT_ENTITY(Game.Player.Handle, p.Handle))
                        aimed = true;
                if (c.Walker != null && c.Walker.Exists() && PLAYER.IS_PLAYER_FREE_AIMING_AT_ENTITY(Game.Player.Handle, c.Walker.Handle))
                    aimed = true;
            }
            if (aimed && c.Phase != "done")
            {
                Feed.Say("A man at the fire", Campfire.Backs(c.Seed), Speech.When(h, "scared", me.Handle), h, toYou: true);
                Log.Write("CAMPFIRE a gun on them: they beg off");
                c.Phase = "done";
                c.PhaseAt = now;
                return;
            }
            if (c.Joiner && !c.JoinerDue && now - c.StartedAt > 20000 && (c.Phase == "sat" || c.Phase == "telling" || c.Phase == "parting") && d < 40f)
                CampfireJoiner(c, me, now);
            if (c.Walker != null && c.Walker.Exists() && !c.Walker.IsDead && c.Walker.Position.DistanceTo(c.At) < 3f && !PED.IS_PED_USING_ANY_SCENARIO(c.Walker.Handle) && !c.JoinerDue)
            {
                c.JoinerDue = true;   // he's arrived and sat: the host makes room
                TASK.TASK_START_SCENARIO_IN_PLACE_HASH(c.Walker.Handle, Natives.Hash(CampfireSit), 600000, true, 0u, -1f, false);
                Feed.Say("A man at the fire", Campfire.Joined(c.Seed), Speech.When(h, "greet", c.Walker.Handle), h);
                Feed.Say("A traveller", Campfire.JoinerThanks(c.Seed), Speech.When(c.Walker.Handle, "thanks", h), c.Walker.Handle);
                _campfireJoined++;
                Log.Write("CAMPFIRE a traveller came in from the dark and was made room for");
            }
            switch (c.Phase)
            {
                case "waiting":
                    if (d < 25f && NodInView(me, host))
                    {
                        TASK.TASK_LOOK_AT_ENTITY(h, me.Handle, -1, 0, 51, 0);
                        Feed.Say("A man at the fire", Campfire.Hail(c.Seed), Speech.When(h, "greet", me.Handle), h, toYou: true);
                        c.Phase = "hailed";
                        c.PhaseAt = now;
                        Log.Write("CAMPFIRE calls you over (" + (int)d + " m)");
                    }
                    else if (now - c.StartedAt > 5 * 60000)
                        EndCampfire("you never came their way");
                    return;
                case "hailed":
                    if (d < 5f && ENTITY.GET_ENTITY_SPEED(me.Handle) < 0.4f && !PED.IS_PED_ON_MOUNT(me.Handle) && !PED.IS_PED_IN_ANY_VEHICLE(me.Handle, false))
                    {
                        if (c.StoppedSince == 0)
                            c.StoppedSince = now;
                        else if (now - c.StoppedSince > 3000)
                        {
                            Feed.Say("A man at the fire", Campfire.Welcome(c.Seed, c.Sitters.Count < 2), Speech.When(h, "mild", me.Handle), h, toYou: true);
                            c.Phase = "sat";
                            c.PhaseAt = now;
                            c.NextLineAt = now + 5000;
                            c.Line = 0;
                            _campfireSat++;
                            Log.Write("CAMPFIRE you sat down; the tale begins");
                        }
                    }
                    else
                        c.StoppedSince = 0;
                    if (c.Phase == "hailed" && (d > 60f || now - c.PhaseAt > 90000))
                    {
                        Feed.Say("A man at the fire", Campfire.Ignored(c.Seed), Speech.When(h, "mild", 0), h);
                        c.Phase = "done";
                        c.PhaseAt = now;
                        Log.Write("CAMPFIRE you rode on; " + (d > 60f ? "out of earshot" : "too long") );
                    }
                    return;
                case "sat":
                case "telling":
                    if (d > 12f)
                    {
                        Feed.Say("A man at the fire", Campfire.Farewell(c.Seed), Speech.When(h, "greet", me.Handle), h, toYou: true);
                        c.Phase = "done";
                        c.PhaseAt = now;
                        Log.Write("CAMPFIRE you got up and went; he bids you good night");
                        return;
                    }
                    if (now >= c.NextLineAt)
                    {
                        if (c.Line < c.Tale.Length)
                        {
                            Feed.Say("A man at the fire", c.Tale[c.Line], Speech.When(h, "mild", me.Handle), h, toYou: true);
                            if (c.Line < 3 && c.Sitters.Count > 1 && c.Sitters[1] != null && c.Sitters[1].Exists())
                                Feed.Say("Another at the fire", Campfire.Answer(c.Line, c.Seed, Campfire.TaleIndex(c.Seed)), Speech.When(c.Sitters[1].Handle, c.Line == 1 ? "mock" : "mild", h), c.Sitters[1].Handle);
                            c.Line++;
                            c.NextLineAt = now + 6000;
                            c.Phase = "telling";
                        }
                        else
                        {
                            Feed.Say("A man at the fire", Campfire.Farewell(c.Seed), Speech.When(h, "greet", me.Handle), h, toYou: true);
                            c.Phase = "parting";
                            c.PhaseAt = now;
                            Log.Write("CAMPFIRE the tale told; he bids you good night");
                        }
                    }
                    return;
                case "parting":
                case "done":
                    if (d > 80f || now - c.PhaseAt > 120000)
                        EndCampfire(c.Phase == "parting" ? "the fire burns on behind you" : "over");
                    return;
            }
        }

        /// The NPC version: a traveller spawned 35-60 m out of sight walks in to the fire.
        private void CampfireJoiner(CampfireRun c, Ped me, int now)
        {
            if (!Spawner.Ready(c.WalkerModel))
                return;
            Vector3 at;
            if (!EncounterSpot(me, 35f, 60f, out at))
                return;
            Ped w = Spawner.Spawn(c.WalkerModel, at, 0f);
            if (w == null)
                return;
            int h = w.Handle;
            WEAPON.REMOVE_ALL_PED_WEAPONS(h, true, true);
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, true);
            PED.SET_PED_KEEP_TASK(h, true);
            double ang = (20.0 + 180.0 / Math.Max(1, c.Sitters.Count)) * Math.PI / 180.0;   // the middle of the first gap between the men
            Vector3 seat = c.At + new Vector3((float)Math.Cos(ang) * 1.5f, (float)Math.Sin(ang) * 1.5f, 0f);
            TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(h, seat.X, seat.Y, seat.Z, Natives.Brisk, 60000, 1f, 0, (float)((ang * 180.0 / Math.PI + 90.0) % 360.0));
            c.Walker = w;
            c.Joiner = false;   // once
            _encounterPeds.Add(h);
            Log.Write("CAMPFIRE a traveller is walking in from the dark (" + (int)at.DistanceTo(c.At) + " m)");
        }

        private unsafe void EndCampfire(string why)
        {
            CampfireRun c = _campfire;
            _campfire = null;
            if (c == null)
                return;
            Ped me = Game.Player.Character;
            var all = new List<Ped>(c.Sitters);
            if (c.Walker != null)
                all.Add(c.Walker);
            foreach (Ped p in all)
            {
                if (p == null || !p.Exists())
                    continue;   // the handle stays in _encounterPeds, so a death after is judged as any of ours
                bool seen = me != null && me.Exists() && ENTITY.IS_ENTITY_ON_SCREEN(p.Handle) && ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(me.Handle, p.Handle, 17);
                Spawner.Release(p, !seen && !p.IsDead);   // deleted unseen; seen, let go to the game with their sit kept (the dead stay)
            }
            if (c.Fire != 0 && ENTITY.DOES_ENTITY_EXIST(c.Fire))
            {
                int f = c.Fire;
                bool seen = me != null && me.Exists() && ENTITY.IS_ENTITY_ON_SCREEN(f) && me.Position.DistanceTo(c.At) < 150f;
                if (!seen)
                {
                    ENTITY.SET_ENTITY_AS_MISSION_ENTITY(f, true, true);
                    OBJECT.DELETE_OBJECT(&f);
                }
                else
                    ENTITY.SET_OBJECT_AS_NO_LONGER_NEEDED(&f);   // in sight: the game keeps it for now
            }
            _campfireLast = why;
            if (c.Phase != "spawn")
                _campfires++;
            int focus = c.Sitters.Count > 0 && c.Sitters[0] != null && c.Sitters[0].Exists() ? c.Sitters[0].Handle : 0;
            if (!c.Test && c.Phase != "spawn")
                Safe("world events", () => RecordEvent("encounter:campfire", null, "strangers at a fire", "player", "you", c.At, focus, focus, 0, why));
            Log.Write("CAMPFIRE over: " + why);
        }

        private string CampfireStatus()
        {
            if (!_cfg.Campfires)
                return "off ([Encounters] Campfires)";
            return _cfg.CampfireChance + "% (8 pm to 4 am, the wilds), " + _campfires + " so far, " + _campfireSat + " sat at, " + _campfireJoined + " travellers came in" +
                   (_campfire != null ? "; now: " + _campfire.Phase : "") + (_campfireLast != null ? "; last: " + _campfireLast : "");
        }
    }
}
