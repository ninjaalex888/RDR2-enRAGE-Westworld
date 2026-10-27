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
            public bool Test;
        }

        private TravellerRun _traveller;
        private int _travellers, _travellerAnswered;
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
            _traveller = new TravellerRun { StartedAt = now, PhaseAt = now, Test = test, Town = town, TownX = tx, TownY = ty, Model = Spawner.ModelFor(null, false, _rng.Next()), Seed = "tr" + now };
            Log.Write("TRAVELLER begins: one on foot, bound for " + town + " (" + (int)(metres / 1000) + " km off)" + (test ? " (a test)" : ""));
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
            if (t.Man == null || !t.Man.Exists() || t.Man.IsDead || PED.IS_PED_IN_COMBAT(t.Man.Handle, 0))
            {
                EndTraveller(t.Man != null && t.Man.Exists() && t.Man.IsDead ? "he died" : "he was gone");
                return;
            }
            int h = t.Man.Handle;
            float d = t.Man.Position.DistanceTo(me.Position);
            switch (t.Phase)
            {
                case "waiting":
                    if (d < 30f && NodInView(me, t.Man))
                    {
                        TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, me.Handle, 2000, 0f, 0f, 0f);
                        TASK.TASK_LOOK_AT_ENTITY(h, me.Handle, -1, 0, 51, 0);
                        TravellerGesture(h, "happy_wave_fl_001");
                        Feed.Say("A traveller", Travellers.Hail(t.Seed), Speech.When(h, "greet", me.Handle), h, toYou: true);
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
                        Feed.Say("A traveller", Travellers.Ask(t.Town, t.Seed), Speech.When(h, "mild", me.Handle), h, toYou: true);
                        t.Phase = "asked";
                        t.PhaseAt = now;
                        Log.Write("TRAVELLER asks you the way to " + t.Town);
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

        /// Somewhere along the road before you, where he can't be seen arriving; else behind.
        private void TravellerSpawn(TravellerRun t, Ped me, int now)
        {
            if (now - t.StartedAt > 20000 || t.Tries > 10)
            {
                EndTraveller("nowhere on the road to put him");
                return;
            }
            if (!Spawner.Ready(t.Model))
                return;
            Vector3 at;
            if (!TravellerSpot(me, out at) && !EncounterSpot(me, 50f, 80f, out at))
            {
                t.Tries++;
                return;
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
            TASK.TASK_STAND_STILL(h, 240000);
            t.Man = m;
            t.Phase = "waiting";
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
            if (t == null || t.Man == null || t.Man.Handle != handle || t.Phase == "spawn" || t.Phase == "going" || t.Phase == "done")
                return false;
            if (friendly)
                TravellerAnswered(t, Game.GameTime, "you greeted him");
            else
            {
                Ped me = Game.Player.Character;
                Feed.Say("A traveller", Travellers.Refused(t.Seed), Speech.When(handle, "scared", me.Handle), handle, toYou: true);
                TASK.TASK_SMART_FLEE_PED(handle, me.Handle, 25f, 8000, 0, 2f, 0);
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
            TASK.TASK_TURN_PED_TO_FACE_COORD(h, town.X, town.Y, town.Z, 1500);
            TravellerGesture(h, Voice.Hash(t.Seed) % 2 == 0 ? "neutral_point_l_001" : "neutral_point_r_001");
            Feed.Say("A traveller", Travellers.Thanks(t.Town, t.Seed), Speech.When(h, "thanks", me.Handle), h, toYou: true);
            // a little way toward the town, then on his own
            Vector3 dir = town - t.Man.Position;
            float len = Math.Max(1f, (float)Math.Sqrt(dir.X * dir.X + dir.Y * dir.Y));
            Vector3 go = t.Man.Position + new Vector3(dir.X / len * 120f, dir.Y / len * 120f, 0f);
            TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(h, go.X, go.Y, go.Z, 1f, 90000, 2f, 0, 0f);
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, false);
            t.Phase = "going";
            t.PhaseAt = now;
            _travellerAnswered++;
            Log.Write("TRAVELLER: " + how + "; he points toward " + t.Town + ", thanks you and walks that way");
        }

        private void TravellerIgnored(TravellerRun t, int now)
        {
            int h = t.Man.Handle;
            Speech.SayTag(h, "mild", 0);
            TASK.TASK_WANDER_STANDARD(h, 10f, 10);
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, false);
            if (t.Man.Position.DistanceTo(Game.Player.Character.Position) < 20f && NodInView(Game.Player.Character, t.Man))
                _ticker.Push("A traveller, thinking", Travellers.Ignored(t.Seed), "near", h);
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
            if (t.Man != null && t.Man.Exists())
            {
                if (t.Phase != "spawn")
                    Safe("world events", () => RecordEvent("encounter:traveller", null, "a traveller asking the way", "player", "you", t.Man.Position, t.Man.Handle, t.Man.Handle, 0, why));
                bool seen = ENTITY.IS_ENTITY_ON_SCREEN(t.Man.Handle) && t.Man.Position.DistanceTo(Game.Player.Character.Position) < 120f;
                Spawner.Release(t.Man, !t.Man.IsDead && !seen);   // the living go where nobody sees; the dead stay
            }
            if (t.Phase != "spawn")
            {
                if (!t.Test)
                    MarkDone("traveller");
                _travellers++;
                _travellerLast = "bound for " + t.Town + ": " + why + (t.Test ? " (a test)" : "");
            }
            Log.Write("TRAVELLER over (" + why + ")");
        }

        private string TravellerStatus()
        {
            if (!_cfg.Travellers)
                return "off ([Encounters] Travellers)";
            return (_traveller != null ? "out now (" + _traveller.Phase + ", bound for " + _traveller.Town + ")" : "not out") + "; " + _travellers + " this session, " + _travellerAnswered + " shown the way" +
                   (_travellerLast != null ? "; last: " + _travellerLast : "") + "; now: " + (TravellerNotNow() ?? "one could come");
        }
    }
}
