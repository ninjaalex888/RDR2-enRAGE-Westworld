// A stranded traveller asking the way ([Encounters] Travellers; the words and towns are Core/Travellers.cs): by day
// (07:00-19:00), out of the towns, with you on a road, a man on foot stands on the road 60-110 m ahead where you can't
// see him arrive (or, failing that, behind you). Within 30 m he hails you (subtitled), within 8 m he asks the way to the
// nearest town. Greet him (R), or stop by him 3 s: he works it out, turns to the town, points, thanks you and walks off
// toward it. Antagonize him: he backs off. Ride on: he mutters and goes on. Released (gone unseen) when it's over and
// you're 120 m off or can't see him. Only one, in the encounter framework (Pacing "traveller").
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
        private sealed class TravellerRun
        {
            public Ped Man;
            public uint Model;
            public string Phase = "spawn", Town, Seed;   // spawn, waiting, hailed, asked, going, done
            public float TownX, TownY;
            public int StartedAt, PhaseAt, StoppedSince, Tries;
            public bool Test, Bitten;   // Bitten: snakebit, sitting by the road, asking for help (Tier 4)
            public bool Helped, Left, Mounted;   // the snakebit man seen to, or left; whether you were on a horse
            // Robbed: held up by two of the region's outlaws when you come on him (Tier 4)
            public bool Robbed, Noticed, Warned, Thought, Saved;
            public readonly List<Ped> Agents = new List<Ped>();   // the two road agents
            public readonly List<uint> AgentModels = new List<uint>();
            public int AimSince, RobAt, NextLine, Lines;
            public string GoWhat;   // what the log says when he walks on, after his word to you
        }

        private TravellerRun _traveller;
        private int _travellers, _travellerAnswered, _travellerTests, _bitten, _bittenHelped, _robbed, _robSaved;
        private string _travellerLast;

        private string TravellerNotNow(bool test = false)
        {
            Ped me = Game.Player.Character;
            if (_bridge.TownOnly(me.Position) != null)
                return "you're in a town (he's out on the roads)";
            int clock = CLOCK.GET_CLOCK_HOURS();
            if (!test && !Travellers.Hour(clock))
                return "it's " + clock.ToString("00") + ":00 (he walks by day, 07:00 to 19:00)";
            if (!test && DoneToday("traveller"))
                return "one asked the way already today";
            Vector3 p = me.Position;
            if (!PATHFIND.IS_POINT_ON_ROAD(p.X, p.Y, p.Z, 0) && !test)
                return "you're not on a road";
            return null;
        }

        private void StartTraveller(int now, bool test = false)
        {
            Vector3 p = Game.Player.Character.Position;
            float metres, tx, ty;
            string town = Travellers.Nearest(p.X, p.Y, out metres, out tx, out ty);
            // a test takes turns: lost, snakebit, robbed; else by the roll
            int turn = test ? _travellerTests++ % 3 : -1, roll = _rng.Next(100);
            bool bitten = test ? turn == 1 : roll < Travellers.BittenChance;
            bool robbed = test ? turn == 2 : !bitten && roll < Travellers.BittenChance + Travellers.RobbedChance;
            _traveller = new TravellerRun { StartedAt = now, PhaseAt = now, Test = test, Town = town, TownX = tx, TownY = ty, Model = Spawner.ModelFor(null, false, _rng.Next()), Seed = "tr" + now, Bitten = bitten, Robbed = robbed };
            if (robbed)
            {
                string[] models = Executions.GangModels(Executions.GangOf(_bridge.TownAt(p) ?? town));
                for (int i = 0; i < 2; i++)
                    _traveller.AgentModels.Add(Natives.Hash(models[_rng.Next(models.Length)]));
            }
            Log.Write("TRAVELLER begins: one on foot, bound for " + town + " (" + (int)(metres / 1000) + " km off)" + (bitten ? ", snakebit" : robbed ? ", held up by two road agents" : "") + (test ? " (a test)" : ""));
        }

        private void TravellerStep(int now)
        {
            TravellerRun t = _traveller;
            Ped me = Game.Player.Character;
            if (me == null || !me.Exists() || me.IsDead)
            {
                EndTraveller("you died");
                return;
            }
            if (t.Phase == "spawn")
            {
                TravellerSpawn(t, me, now);
                return;
            }
            if (t.Man == null || !t.Man.Exists() || t.Man.IsDead || (!t.Robbed && PED.IS_PED_IN_COMBAT(t.Man.Handle, 0)))
            {
                EndTraveller(t.Man != null && t.Man.Exists() && t.Man.IsDead ? "he died" : "he was gone");
                return;
            }
            int h = t.Man.Handle;
            float d = t.Man.Position.DistanceTo(me.Position);
            if (t.Robbed && (t.Phase == "robbing" || t.Phase == "fight" || t.Phase == "pausing"))
            {
                RobberyStep(t, me, d, now);
                return;
            }
            switch (t.Phase)
            {
                case "waiting":
                    if (d < 30f && NodInView(me, t.Man))
                    {
                        if (!t.Bitten)
                        {
                            TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, me.Handle, 2000, 0f, 0f, 0f);
                            TravellerGesture(h, "happy_wave_fl_001");
                        }
                        TASK.TASK_LOOK_AT_ENTITY(h, me.Handle, -1, 0, 51, 0);
                        Feed.Say("A traveller", t.Bitten ? Travellers.BittenHail(t.Seed) : Travellers.Hail(t.Seed), Speech.When(h, t.Bitten ? "scared" : "greet", me.Handle), h, toYou: true);
                        t.Phase = "hailed";
                        t.PhaseAt = now;
                        Log.Write("TRAVELLER hails you (" + (int)d + " m)");
                    }
                    else if (now - t.StartedAt > 4 * 60000)
                        EndTraveller("you never came his way");
                    return;
                case "hailed":
                    if (d < 8f)
                    {
                        Feed.Say("A traveller", t.Bitten ? Travellers.BittenAsk(t.Seed) : Travellers.Ask(t.Town, t.Seed), Speech.When(h, t.Bitten ? "scared" : "mild", me.Handle), h, toYou: true);
                        t.Phase = "asked";
                        t.PhaseAt = now;
                        Log.Write(t.Bitten ? "TRAVELLER asks you for help: snakebit" : "TRAVELLER asks you the way to " + t.Town);
                    }
                    else if (d > 60f || now - t.PhaseAt > 60000)
                        TravellerIgnored(t, now);
                    return;
                case "asked":
                    // stopping by him a while is an answer, as a greeting is (TravellerGreeted)
                    if (d < 4f && ENTITY.GET_ENTITY_SPEED(me.Handle) < 0.4f)
                    {
                        if (t.StoppedSince == 0)
                            t.StoppedSince = now;
                        else if (now - t.StoppedSince > 3000)
                            TravellerAnswered(t, now, "you stopped and told him");
                    }
                    else
                        t.StoppedSince = 0;
                    if (t.Phase == "asked" && (d > 30f || now - t.PhaseAt > 30000))
                        TravellerIgnored(t, now);
                    return;
                case "going":
                case "done":
                    if (d > 120f || (!NodInView(me, t.Man) && now - t.PhaseAt > 15000) || now - t.PhaseAt > 90000)
                        EndTraveller(t.Phase == "going" ? "he went on his way" : "he went on");
                    return;
            }
        }

        // ---- a robbery on the road (Tier 4): the traveller held up by two of the region's outlaws --------------------

        /// Two road agents facing him with their guns out; his hands up. False if neither could be put there.
        private bool RobberySpawn(TravellerRun t, Vector3 at, int now)
        {
            int vh = t.Man.Handle;
            float heading = ENTITY.GET_ENTITY_HEADING(vh);
            double rad = heading * Math.PI / 180.0;
            Vector3 front = new Vector3((float)-Math.Sin(rad), (float)Math.Cos(rad), 0f), side = new Vector3(front.Y, -front.X, 0f);
            for (int i = 0; i < t.AgentModels.Count; i++)
            {
                Vector3 spot = at + front * 2.8f + side * (i == 0 ? -0.9f : 0.9f);
                Vector3 ground;
                if (Natives.StandSpot(spot, at.Z, out ground))
                    spot = ground;
                Ped m = Spawner.Spawn(t.AgentModels[i], spot, heading + 180f);
                if (m == null)
                    continue;
                int mh = m.Handle;
                m.GiveWeapon(eWeapon.RevolverCattleman, 60, eWeaponAttachPoint.PistolR, true);
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(mh, true);
                PED.SET_PED_KEEP_TASK(mh, true);
                TASK.TASK_AIM_GUN_AT_ENTITY(mh, vh, -1, false, 0);
                _encounterPeds.Add(mh);
                _cameAtYouSeen[mh] = now;   // men robbing a man at gunpoint: shooting them is self-defence
                t.Agents.Add(m);
            }
            if (t.Agents.Count == 0)
                return false;
            TASK.TASK_HANDS_UP(vh, 240000, t.Agents[0].Handle, -1, false);
            t.NextLine = now + 2500;
            Log.Write("TRAVELLER held up by " + t.Agents.Count + " road agents, " + (int)at.DistanceTo(Game.Player.Character.Position) + " m off");
            return true;
        }

        private unsafe void RobberyStep(TravellerRun t, Ped me, float d, int now)
        {
            int vh = t.Man.Handle;
            List<Ped> men = t.Agents.Where(m => m != null && m.Exists() && !m.IsDead).ToList();
            if (t.Phase != "pausing")
                foreach (Ped m in men)
                    _cameAtYouSeen[m.Handle] = now;
            if (t.Phase == "pausing")
            {
                if (now >= t.RobAt)
                    RobberyGo(t, now, t.GoWhat);   // he's had his word with you: now he walks on
                return;
            }
            if (t.Phase == "fight")
            {
                // he's saved once they're all down, or the living have run (far from him and from you); three minutes of
                // it and he slips away with nothing said
                if (men.All(m => m.Position.DistanceTo(t.Man.Position) > 70f && m.Position.DistanceTo(me.Position) > 70f))
                {
                    TASK.CLEAR_PED_TASKS(vh, false, false);
                    t.Saved = true;
                    _robSaved++;
                    if (d < 40f)
                    {
                        TASK.TASK_TURN_PED_TO_FACE_ENTITY(vh, me.Handle, 3000, 0f, 0f, 0f);
                        Feed.Say("A traveller", Travellers.RobSaved(t.Seed), Speech.When(vh, "thanks", me.Handle), vh, toYou: true);
                        RobberyPause(t, now, "you drove them off; he thanks you and goes on toward " + t.Town);
                    }
                    else
                        RobberyGo(t, now, "you drove them off; he goes on toward " + t.Town);
                }
                else if (now - t.PhaseAt > 180000)
                    EndTraveller("the fight went on; he slipped away");
                return;
            }
            // you step in: a shot at them, a gun on one of them a moment, or right up to them
            ulong slot = 0, slot2 = 0;   // out-parameters take 8 bytes
            int aimed = PLAYER.GET_ENTITY_PLAYER_IS_FREE_AIMING_AT(Game.Player.Handle, (int*)&slot) ? (int)slot : 0;
            if (aimed == 0 && PLAYER.GET_PLAYER_TARGET_ENTITY(Game.Player.Handle, (int*)&slot2))
                aimed = (int)slot2;   // lock-on aim counts as well
            bool aiming = aimed != 0 && men.Any(m => m.Handle == aimed);
            t.AimSince = aiming ? (t.AimSince == 0 ? now : t.AimSince) : 0;
            // one of them killed by you (not by a passer-by or a lawman), one hurt by you, a gun on one a moment, or you
            // right up to them slowed down: riding on past, as they told you to, isn't stepping in
            bool killedOne = t.Agents.Any(a => a != null && a.Exists() && a.IsDead && PED.GET_PED_SOURCE_OF_DEATH(a.Handle) == me.Handle);
            bool upTo = ENTITY.GET_ENTITY_SPEED(me.Handle) < 2f && men.Any(m => m.Position.DistanceTo(me.Position) < 4f);
            if (killedOne || men.Any(m => ENTITY.HAS_ENTITY_BEEN_DAMAGED_BY_ENTITY(m.Handle, me.Handle, true, false)) ||
                (aiming && now - t.AimSince > 1200) || upTo)
            {
                foreach (Ped m in men)
                {
                    MakeHostile(m, now);
                    TASK.TASK_COMBAT_PED(m.Handle, me.Handle, 0, 16);
                }
                t.Noticed = true;
                t.Phase = "fight";
                t.PhaseAt = now;
                Log.Write("TRAVELLER: you stepped in; " + men.Count + " road agents fight you");
                return;
            }
            bool seen = ENTITY.IS_ENTITY_ON_SCREEN(vh) && ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(me.Handle, vh, 17);
            if (!t.Noticed)
            {
                if (d < 40f || (d < 75f && seen))
                {
                    t.Noticed = true;
                    t.RobAt = now + 20000;
                    Log.Write("TRAVELLER: you came on a robbery, " + (int)d + " m off");
                }
                else if (now - t.StartedAt > 4 * 60000)
                {
                    EndTraveller("you never came near; they were gone with his money");
                    return;
                }
                return;
            }
            if (!t.Warned && d < 28f && men.Count > 0)
            {
                t.Warned = true;
                t.RobAt = Math.Min(t.RobAt, now + 10000);
                Feed.Say("A road agent", Travellers.RobWarn(PED.IS_PED_ON_MOUNT(me.Handle), t.Seed), Speech.When(men[0].Handle, "threat", me.Handle), men[0].Handle, toYou: true);
            }
            if (!t.Thought && d < 30f && seen)
            {
                t.Thought = true;
                _ticker.Push("A traveller, thinking", Travellers.RobThought(t.Seed), "near", vh);
            }
            if (now >= t.NextLine && men.Count > 0)
            {
                // overheard: the men and him, by turns
                t.NextLine = now + 5500 + _rng.Next(2000);
                string seed = t.Seed + "|" + t.Lines;
                if (t.Lines++ % 2 == 0)
                    Feed.Say("A road agent", Travellers.RobThreat(seed), Speech.When(men[0].Handle, "threat", vh), men[0].Handle);
                else
                    Feed.Say("A traveller", Travellers.RobPlead(t.Town, seed), Speech.When(vh, "plead", men[0].Handle), vh);
            }
            if (now >= t.RobAt || men.Count == 0)
            {
                // they have what they came for, and go; he lowers his hands, and tells you if you're near
                Feed.Say("A traveller", Travellers.RobHandOver(t.Seed), Speech.When(vh, "scared", men.Count > 0 ? men[0].Handle : me.Handle), vh);
                if (men.Count > 0)
                    Feed.Say("A road agent", Travellers.RobDone(t.Seed), Speech.When(men[0].Handle, "storm", vh), men[0].Handle);
                foreach (Ped m in men)
                {
                    TASK.CLEAR_PED_TASKS(m.Handle, false, false);
                    Vector3 away = m.Position + (m.Position - me.Position).Normalized * 150f;
                    TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(m.Handle, away.X, away.Y, away.Z, 2f, -1, 1f, 0, 0f);
                    PED.SET_PED_KEEP_TASK(m.Handle, false);
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(m.Handle, false);   // free to answer if they're shot at as they go
                }
                TASK.CLEAR_PED_TASKS(vh, false, false);
                if (d < 30f)
                {
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(vh, me.Handle, 3000, 0f, 0f, 0f);
                    Feed.Say("A traveller", Travellers.RobbedAfter(t.Seed), Speech.When(vh, "mild", me.Handle), vh, toYou: true);
                    RobberyPause(t, now, "they took his money and went; he tells you, and goes on toward " + t.Town);
                }
                else
                    RobberyGo(t, now, "they took his money and went; he goes on toward " + t.Town);
            }
        }

        /// He has his word with you to your face first, then walks on (RobberyStep's "pausing").
        private void RobberyPause(TravellerRun t, int now, string what)
        {
            t.Phase = "pausing";
            t.RobAt = now + 3500;
            t.GoWhat = what;
        }

        /// He walks on toward the town (as the one shown the way does).
        private void RobberyGo(TravellerRun t, int now, string what)
        {
            int h = t.Man.Handle;
            Vector3 town = new Vector3(t.TownX, t.TownY, t.Man.Position.Z);
            Vector3 dir = town - t.Man.Position;
            float len = Math.Max(1f, (float)Math.Sqrt(dir.X * dir.X + dir.Y * dir.Y));
            Vector3 go = t.Man.Position + new Vector3(dir.X / len * 120f, dir.Y / len * 120f, 0f);
            TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(h, go.X, go.Y, go.Z, 1f, 90000, 2f, 0, 0f);
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, false);
            t.Phase = "going";
            t.PhaseAt = now;
            Log.Write("TRAVELLER: " + what);
        }

        /// Somewhere along the road before you, where he can't be seen arriving; else behind.
        private void TravellerSpawn(TravellerRun t, Ped me, int now)
        {
            if (now - t.StartedAt > 20000 || t.Tries > 10)
            {
                EndTraveller("nowhere on the road to put him");
                return;
            }
            if (!Spawner.Ready(t.Model) || t.AgentModels.Any(mm => !Spawner.Ready(mm)))
                return;
            Vector3 at;
            if (!TravellerSpot(me, out at) && !EncounterSpot(me, 50f, 80f, out at))
            {
                t.Tries++;
                return;
            }
            if (t.Bitten || t.Robbed)
            {
                // by the road, not in it: to one side (across the way you face, roughly across the road); a hold-up well
                // off it, so riding on past as they tell you keeps your distance
                double hd = ENTITY.GET_ENTITY_HEADING(me.Handle) * Math.PI / 180.0;
                float k = (Voice.Hash(t.Seed) % 2 == 0 ? 1f : -1f) * (t.Robbed ? 9f : 3.5f);
                Vector3 side;
                if (Natives.StandSpot(at + new Vector3((float)Math.Cos(hd) * k, (float)Math.Sin(hd) * k, 0f), at.Z, out side))
                    at = side;
            }
            Ped m = Spawner.Spawn(t.Model, at, _rng.Next(360));
            if (m == null || ENTITY.IS_ENTITY_IN_WATER(m.Handle))
            {
                if (m != null)
                    Spawner.Release(m, true);
                t.Tries++;
                return;
            }
            int h = m.Handle;
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, true);
            PED.SET_PED_KEEP_TASK(h, true);
            WEAPON.REMOVE_ALL_PED_WEAPONS(h, true, true);
            if (t.Bitten)
                TASK.TASK_START_SCENARIO_IN_PLACE_HASH(h, Natives.Hash("WORLD_HUMAN_SIT_GROUND"), 240000, true, 0u, -1f, false);   // sat by the road, holding his leg
            else
                TASK.TASK_STAND_STILL(h, 240000);
            t.Man = m;
            if (t.Robbed && !RobberySpawn(t, at, now))
            {
                Spawner.Release(m, true);   // the road agents couldn't be put there: try again elsewhere
                t.Man = null;
                t.Tries++;
                return;
            }
            if (!t.Test)
                Pacing.Note(_store.Pace, "traveller", null, null, _store.LastGameMin);
            t.Phase = t.Robbed ? "robbing" : "waiting";
            t.PhaseAt = now;
            _encounterPeds.Add(h);
            Log.Write("TRAVELLER set out " + (int)at.DistanceTo(me.Position) + " m off");
        }

        /// A road point 60-110 m before you (the way you face), hidden from the camera.
        private unsafe bool TravellerSpot(Ped me, out Vector3 at)
        {
            at = Vector3.Zero;
            Vector3 cam = CAM.GET_FINAL_RENDERED_CAM_COORD();
            double hd = ENTITY.GET_ENTITY_HEADING(me.Handle) * Math.PI / 180.0;
            for (int i = 0; i < 6; i++)
            {
                float ahead = 60f + i * 10f;
                Vector3 want = me.Position + new Vector3((float)(-Math.Sin(hd) * ahead), (float)(Math.Cos(hd) * ahead), 0f);
                ulong* slots = stackalloc ulong[4];
                for (int k = 0; k < 4; k++)
                    slots[k] = 0;
                if (!PATHFIND.GET_NTH_CLOSEST_VEHICLE_NODE(want.X, want.Y, want.Z, 1, (Vector3*)slots, 1, 3f, 0))
                    continue;
                float* f = (float*)slots;   // the game writes a vector as three 8-byte slots (x, y, z)
                Vector3 node = new Vector3(f[0], f[2], f[4]);
                if (node.DistanceTo(me.Position) < 50f || node.DistanceTo(me.Position) > 130f)
                    continue;
                Vector3 safe;
                if (!Natives.StandSpot(node, me.Position.Z, out safe))
                    continue;
                if (!Natives.Blocked(cam, safe + new Vector3(0f, 0f, 1.2f), me.Handle) && Natives.Looking(safe + new Vector3(0f, 0f, 1f), 50f))
                    continue;   // you'd see him appear
                at = safe;
                return true;
            }
            return false;
        }

        /// A greeting (R) or an antagonize (F) aimed at him, from the game's own interaction events: an answer, or not.
        private bool TravellerGreeted(int handle, bool friendly)
        {
            TravellerRun t = _traveller;
            if (t == null || t.Man == null || t.Man.Handle != handle || t.Phase == "spawn" || t.Phase == "going" || t.Phase == "done" || t.Phase == "robbing" || t.Phase == "fight")
                return false;
            if (friendly)
                TravellerAnswered(t, Game.GameTime, "you greeted him");
            else
            {
                Ped me = Game.Player.Character;
                Feed.Say("A traveller", t.Bitten ? Travellers.BittenRefused(t.Seed) : Travellers.Refused(t.Seed), Speech.When(handle, "scared", me.Handle), handle, toYou: true);
                if (!t.Bitten)
                    TASK.TASK_SMART_FLEE_PED(handle, me.Handle, 25f, 8000, 0, 2f, 0);   // the snakebit can't run: he stays where he sat
                t.Left = true;   // run off: he may not make it, or arrive (EndTraveller)
                t.Mounted = PED.IS_PED_ON_MOUNT(me.Handle);
                t.Phase = "done";
                t.PhaseAt = Game.GameTime;
                Log.Write("TRAVELLER: you ran him off");
            }
            return true;
        }

        private void TravellerAnswered(TravellerRun t, int now, string how)
        {
            Ped me = Game.Player.Character;
            int h = t.Man.Handle;
            Vector3 town = new Vector3(t.TownX, t.TownY, t.Man.Position.Z);
            if (t.Bitten)
            {
                // stopping for him is seeing to the bite: he gets up and thanks you, and the town hears of it later
                TASK.CLEAR_PED_TASKS(h, true, false);
                Feed.Say("A traveller", Travellers.BittenThanks(t.Town, t.Seed), Speech.When(h, "thanks", me.Handle), h, toYou: true);
                t.Helped = true;   // the town hears of it when it's over, if he's alive then (EndTraveller)
                t.Mounted = PED.IS_PED_ON_MOUNT(me.Handle);
                _bittenHelped++;
            }
            else
            {
                TASK.TASK_TURN_PED_TO_FACE_COORD(h, town.X, town.Y, town.Z, 1500);
                TravellerGesture(h, Voice.Hash(t.Seed) % 2 == 0 ? "neutral_point_l_001" : "neutral_point_r_001");
                Feed.Say("A traveller", Travellers.Thanks(t.Town, t.Seed), Speech.When(h, "thanks", me.Handle), h, toYou: true);
            }
            // a little way toward the town, then on his own
            Vector3 dir = town - t.Man.Position;
            float len = Math.Max(1f, (float)Math.Sqrt(dir.X * dir.X + dir.Y * dir.Y));
            Vector3 go = t.Man.Position + new Vector3(dir.X / len * 120f, dir.Y / len * 120f, 0f);
            TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(h, go.X, go.Y, go.Z, 1f, 90000, 2f, 0, 0f);
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, false);
            t.Phase = "going";
            t.PhaseAt = now;
            _travellerAnswered++;
            Log.Write("TRAVELLER: " + how + (t.Bitten ? "; you see to the bite; he gets up, thanks you and limps off toward " + t.Town : "; he points toward " + t.Town + ", thanks you and walks that way"));
        }

        private void TravellerIgnored(TravellerRun t, int now)
        {
            int h = t.Man.Handle;
            Speech.SayTag(h, "mild", 0);
            if (!t.Bitten)
            {
                TASK.TASK_WANDER_STANDARD(h, 10f, 10);   // the snakebit stays sat where he is
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, false);
            }
            if (t.Man.Position.DistanceTo(Game.Player.Character.Position) < 20f && NodInView(Game.Player.Character, t.Man))
                _ticker.Push("A traveller, thinking", t.Bitten ? Travellers.BittenIgnored(t.Seed, PED.IS_PED_ON_MOUNT(Game.Player.Character.Handle)) : Travellers.Ignored(t.Seed), "near", h);
            t.Left = true;   // he may not make it, or arrive (EndTraveller)
            t.Mounted = PED.IS_PED_ON_MOUNT(Game.Player.Character.Handle);
            t.Phase = "done";
            t.PhaseAt = now;
            Log.Write("TRAVELLER: you rode on; he goes on alone");
        }

        private void TravellerGesture(int h, string clip)
        {
            string dict = Gestures.MaleSpeaker;
            if (!STREAMING.HAS_ANIM_DICT_LOADED(dict))
            {
                STREAMING.REQUEST_ANIM_DICT(dict);
                return;
            }
            if (!PED.IS_PED_USING_ANY_SCENARIO(h))
                TASK.TASK_PLAY_ANIM(h, dict, clip, 4f, -4f, -1, 8 | 16, 0f, false, 0, false, "", false);
        }

        private void EndTraveller(string why)
        {
            TravellerRun t = _traveller;
            _traveller = null;
            if (t == null)
                return;
            bool alive = t.Man != null && t.Man.Exists() && !t.Man.IsDead;
            if (t.Robbed && !t.Test && alive && (t.Phase == "going" || t.Phase == "pausing"))
                _engine.RobberyTold(t.Town, Travellers.RobNews(t.Town, t.Saved, t.Seed), _store.LastGameMin);   // the town hears, if he lived to tell it
            foreach (Ped rm in t.Agents)
            {
                if (rm == null)
                    continue;
                // the living go only where nobody sees it, and never one enRAGE has looked at; the dead stay where they fell
                bool rAlive = rm.Exists() && !rm.IsDead;
                bool rSeen = rAlive && ENTITY.IS_ENTITY_ON_SCREEN(rm.Handle);
                bool known = rAlive && _bridge != null && _bridge.Looked(rm.Handle);
                bool delete = rAlive && !known && (!rSeen || rm.Position.DistanceTo(Game.Player.Character.Position) > 120f);
                if (delete)
                    _cameAtYouSeen.Remove(rm.Handle);   // a handle the game may give someone else
                else if (rAlive)
                {
                    TASK.CLEAR_PED_TASKS(rm.Handle, false, false);   // left in the world: not still aiming at him
                    PED.SET_PED_KEEP_TASK(rm.Handle, false);
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(rm.Handle, false);
                }
                Spawner.Release(rm, delete);
            }
            if (!t.Bitten && !t.Robbed && !t.Test && alive && t.Left)
                _engine.TravellerMissing(t.Town, t.Seed, _store.LastGameMin);   // the one you passed by, now and then, never arrives
            if (t.Bitten && !t.Test && alive && (t.Helped || t.Left))
            {
                // the town's word, only for a man alive when you left him: he limped in, or (half the time) he didn't
                if (t.Helped)
                    _engine.SnakebiteTold(t.Town, Travellers.BittenNews(t.Town, t.Seed, t.Mounted), _store.LastGameMin);
                else
                    _engine.SnakebiteLeft(t.Town, t.Seed, _store.LastGameMin);
            }
            if (t.Man != null && t.Man.Exists())
            {
                if (t.Phase != "spawn")
                    Safe("world events", () => RecordEvent(t.Bitten ? "encounter:snakebite" : t.Robbed ? "encounter:robbery" : "encounter:traveller", null,
                                                           t.Bitten ? "a snakebit man on the road" : t.Robbed ? "road agents" : "a traveller asking the way",
                                                           t.Robbed ? null : "player", t.Robbed ? "a traveller" : "you", t.Man.Position, t.Man.Handle, t.Man.Handle, 0, why));
                bool seen = ENTITY.IS_ENTITY_ON_SCREEN(t.Man.Handle) && t.Man.Position.DistanceTo(Game.Player.Character.Position) < 120f;
                if (!t.Man.IsDead && seen)
                {
                    if (t.Robbed && (t.Phase == "robbing" || t.Phase == "fight"))
                        TASK.CLEAR_PED_TASKS(t.Man.Handle, false, false);   // hands down: it's over
                    PED.SET_PED_KEEP_TASK(t.Man.Handle, false);
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(t.Man.Handle, false);
                }
                Spawner.Release(t.Man, !t.Man.IsDead && !seen);   // the living go where nobody sees; the dead stay
            }
            if (t.Phase != "spawn")
            {
                if (!t.Test)
                    MarkDone("traveller");
                _travellers++;
                if (t.Bitten)
                    _bitten++;
                if (t.Robbed)
                    _robbed++;
                _travellerLast = "bound for " + t.Town + ": " + why + (t.Test ? " (a test)" : "");
            }
            Log.Write("TRAVELLER over (" + why + ")");
        }

        private string TravellerStatus()
        {
            if (!_cfg.Travellers)
                return "off ([Encounters] Travellers)";
            return (_traveller != null ? "out now (" + _traveller.Phase + ", bound for " + _traveller.Town + (_traveller.Bitten ? ", snakebit" : "") + ")" : "not out") + "; " + _travellers + " this session, " + _travellerAnswered + " answered" +
                   " (snakebit " + _bitten + ", helped " + _bittenHelped + "; robbed " + _robbed + ", saved " + _robSaved + ")" +
                   (_travellerLast != null ? "; last: " + _travellerLast : "") + "; now: " + (TravellerNotNow() ?? "one could come");
        }
    }
}
