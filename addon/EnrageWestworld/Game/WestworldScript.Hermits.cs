// The hermits who woke, in the game (see Core/Engine.Hermits.cs; the lines and his next move are in Core/Encounters.cs).
// A roll out in the wilds gives the next one who fled (or one made for it) a camp 50-80 m off, out of sight, saved.
// From then on, whenever the player comes within 120 m of a living hermit's camp and the spot is out of view, he is
// there: sitting by a fire talking back to the voice, or asleep beside it from midnight to five. He gets up and
// watches as you come; closer and he warns you off in his own recorded voice; keep your distance and he tells you how
// it was; crowd him, draw on him or hurt him and he runs, and is back at his fire another time. Never armed.
// Killing him is a killing like any other (Watch.cs lets his death through).
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
        private sealed class HermitOut
        {
            public Resident R;
            public Ped Ped;
            public int Fire;
            public uint Model;
            public string Phase = "spawn";   // spawn, then Encounters.HermitNext's: camp, watch, warn, talk, flee
            public int StartedAt, PhaseAt, NextLine, NextTurn, Talks, Warned;
            public bool Asleep;
        }

        private static readonly string[] FireModels = { "p_campfire01x", "p_campfire05x" };
        private const string SitScenario = "WORLD_CAMP_FIRE_SIT_GROUND", SleepScenario = "WORLD_CAMP_FIRE_LAY_GROUND_SIDE";
        private const float CampNear = 120f, CampFar = 180f;

        private HermitOut _hermit;
        private int _nextCampCheck;
        private string _newCampId;   // a camp just made: its hermit shouts once from the trees, so you know it's there
        private readonly HashSet<string> _hermitAway = new HashSet<string>();   // ran from you: not back at his fire until you've left and come back

        private static Vector3 CampPos(Resident r)
        {
            return new Vector3(r.CampX, r.CampY, r.CampZ);
        }

        private bool IsHermit(int handle)
        {
            return _hermit != null && _hermit.Ped != null && _hermit.Ped.Handle == handle;
        }

        /// "his" or "her".
        private static string Their(Resident r)
        {
            return r.Woman ? "her" : "his";
        }

        /// The camp is somewhere the camera could see someone appear.
        private bool CampInView(Vector3 camp)
        {
            Vector3 head = camp + new Vector3(0f, 0f, 1.2f);
            return Natives.Looking(head, 55f) && !Natives.Blocked(CAM.GET_FINAL_RENDERED_CAM_COORD(), head, Game.Player.Character.Handle);
        }

        // ---- a new camp -------------------------------------------------------------------------------

        /// Why a new hermit's camp can't turn up here and now; null when it can.
        private string HermitNotNow()
        {
            string town = _bridge.TownAt(Game.Player.Character.Position);
            if (town != null)
                return "you're in " + town + " (camps are only out in the wilds)";
            if (DoneToday("hermit"))
                return "one turned up already today";
            if (_engine.Camps().Count >= Engine.MaxCamps)
                return "there are " + Engine.MaxCamps + " camps already";
            return null;
        }

        private bool NewCamp(int now)
        {
            Ped me = Game.Player.Character;
            Vector3 at;
            if (!EncounterSpot(me, 50f, 80f, out at))
            {
                Log.Write("ENCOUNTER hermit: nowhere out of sight for a camp");
                return false;
            }
            long g = Bridge.Now();
            Resident r = _engine.Homeless();
            if (r == null)
            {
                // nobody has fled yet: someone who did, before you came by
                bool woman = _rng.Next(100) < 25;
                string town = PostTowns[_rng.Next(PostTowns.Length)];
                r = _engine.MakeHermit(Spawner.ModelFor(null, woman, _rng.Next()), woman, town, woman ? "a townswoman" : "a townsman", "hermit|" + now, g);
            }
            _engine.SetCamp(r, at.X, at.Y, at.Z, g);
            MarkDone("hermit");
            _nextCampCheck = 0;
            _newCampId = r.Id;
            Log.Write("ENCOUNTER hermit: " + r.Name + ", who walked out of " + (r.Town ?? "town") + ", makes camp " + (int)at.DistanceTo(me.Position) + " m away");
            return true;
        }

        // ---- at the camp ------------------------------------------------------------------------------

        private void HermitStep(int now)
        {
            if (_hermit != null)
            {
                if (!_cfg.Hermits)
                {
                    EndHermit("went quiet (hermits were turned off)");
                    return;
                }
                HermitLive(now);
                return;
            }
            if (!_cfg.Hermits || _crying != null || now < _nextCampCheck)
                return;
            _nextCampCheck = now + 3000;
            Ped me = Game.Player.Character;
            if (_interiorType != 0 || me.IsDead || !Spawner.SafeToSpawn())
                return;
            Vector3 pos = me.Position;
            List<Resident> camps = _engine.Camps();
            _hermitAway.RemoveWhere(id => !camps.Any(c => c.Id == id && CampPos(c).DistanceTo(pos) <= CampFar));
            Resident best = null;
            float bestD = CampNear;
            foreach (Resident c in camps)
            {
                float d = CampPos(c).DistanceTo(pos);
                if (d < bestD && !_hermitAway.Contains(c.Id))
                {
                    best = c;
                    bestD = d;
                }
            }
            if (best == null || bestD < 25f || CampInView(CampPos(best)))
                return;   // he's never seen arriving
            _hermit = new HermitOut { R = best, Model = best.Model, StartedAt = now };
        }

        private unsafe void HermitSpawn(int now)
        {
            HermitOut o = _hermit;
            Vector3 camp = CampPos(o.R);
            if (now - o.StartedAt > 20000 || CampInView(camp))
            {
                _hermit = null;   // try again another time
                return;
            }
            if (now - o.StartedAt > 8000 && o.Model == o.R.Model)
                o.Model = Spawner.ModelFor(null, o.R.Woman, Voice.Hash(o.R.Id));   // his own looks won't load: someone like him
            bool fireReady = false;
            uint fire = 0;
            foreach (string m in FireModels)
            {
                fire = Natives.Hash(m);
                if (!STREAMING.IS_MODEL_VALID(fire))
                    continue;
                STREAMING.REQUEST_MODEL(fire, false);
                fireReady = STREAMING.HAS_MODEL_LOADED(fire);
                break;
            }
            if (!Spawner.Ready(o.Model) || (!fireReady && now - o.StartedAt < 5000))
                return;
            float heading = (Voice.Hash(o.R.Id + "|h") & 0x7fffffff) % 360;
            double rad = heading * Math.PI / 180.0;
            Vector3 toFire = new Vector3((float)-Math.Sin(rad), (float)Math.Cos(rad), 0f) * 1.4f;
            Ped p = Spawner.Spawn(o.Model, camp, heading);
            if (p == null)
                return;
            int h = p.Handle;
            o.Ped = p;
            WEAPON.REMOVE_ALL_PED_WEAPONS(h, true, true);
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, true);
            PED.SET_PED_KEEP_TASK(h, true);
            if (fireReady)
            {
                Vector3 f = camp + toFire;
                o.Fire = OBJECT.CREATE_OBJECT(fire, f.X, f.Y, f.Z, false, false, false, false, false);
                if (o.Fire != 0)
                {
                    OBJECT.PLACE_OBJECT_ON_GROUND_PROPERLY(o.Fire, false);
                    ENTITY.SET_ENTITY_AS_MISSION_ENTITY(o.Fire, true, true);
                }
                STREAMING.SET_MODEL_AS_NO_LONGER_NEEDED(fire);
            }
            o.Asleep = Encounters.HermitSleeps(CLOCK.GET_CLOCK_HOURS());
            HermitEnter(o, "camp", now);
            Log.Write("ENCOUNTER hermit: " + o.R.Name + " at " + Their(o.R) + " camp, " + (int)camp.DistanceTo(Game.Player.Character.Position) + " m away" +
                      (o.Asleep ? ", asleep" : "") + (o.Fire != 0 ? "" : " (no fire: the campfire model didn't load)"));
        }

        private void HermitLive(int now)
        {
            HermitOut o = _hermit;
            Ped me = Game.Player.Character;
            if (MISC.GET_MISSION_FLAG())
            {
                EndHermit("left (a mission started)");
                return;
            }
            if (o.Phase == "spawn")
            {
                HermitSpawn(now);
                return;
            }
            Ped p = o.Ped;
            if (p == null || !p.Exists())
            {
                EndHermit("is gone");
                return;
            }
            int h = p.Handle;
            if (p.IsDead)
            {
                o.R.Dead = true;
                _engine.KeepResident(o.R, Bridge.Now());
                EndHermit("is dead");
                return;
            }
            float dist = p.Position.DistanceTo(me.Position);
            bool seen = ENTITY.IS_ENTITY_ON_SCREEN(h) && ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(me.Handle, h, 17);
            if (dist > CampFar && (!seen || dist > 250f))
            {
                EndHermit("stays at " + Their(o.R) + " camp (you rode on)");
                return;
            }
            int aimedAt;
            bool aimed = PLAYER.IS_PLAYER_FREE_AIMING_AT_ENTITY(Game.Player.Handle, h) || (_aimedAt.TryGetValue(h, out aimedAt) && now - aimedAt < 1500);
            bool hurt = ENTITY.HAS_ENTITY_BEEN_DAMAGED_BY_ENTITY(h, me.Handle, true, false);
            if (o.Phase == "flee")
            {
                if ((!seen && dist > 50f) || dist > 120f)
                    EndHermit("ran off");
                return;
            }
            string next = Encounters.HermitNext(o.Phase, dist, now - o.PhaseAt, aimed, hurt, o.Talks);
            if (o.Asleep && o.Phase == "camp" && next == "watch" && dist >= 10f && !aimed && !hurt)
                next = "camp";   // asleep: he only wakes when you're right by him
            if (next != o.Phase)
                HermitEnter(o, next, now);
            HermitInPhase(o, me, dist, now);
        }

        private void HermitEnter(HermitOut o, string phase, int now)
        {
            int h = o.Ped.Handle;
            int me = Game.Player.Character.Handle;
            string was = o.Phase;
            o.Phase = phase;
            o.PhaseAt = now;
            o.NextTurn = 0;
            switch (phase)
            {
                case "camp":
                    TASK.CLEAR_PED_TASKS(h, true, false);
                    TASK.TASK_START_SCENARIO_IN_PLACE_HASH(h, Natives.Hash(o.Asleep ? SleepScenario : SitScenario), -1, true, 0u, -1f, false);
                    o.NextLine = now + 4000;
                    break;
                case "watch":
                    TASK.CLEAR_PED_TASKS(h, true, false);
                    if (o.Asleep)
                    {
                        o.Asleep = false;   // woken with a start
                        Speech.Say(h, Speech.Scared[0], me);
                    }
                    break;
                case "warn":
                    o.Warned = 0;
                    o.NextLine = now;
                    break;
                case "talk":
                    o.NextLine = now;
                    break;
                case "flee":
                    _hermitAway.Add(o.R.Id);
                    TASK.CLEAR_PED_TASKS(h, true, false);
                    TASK.TASK_SMART_FLEE_PED(h, me, 150f, -1, 0, 3f, 0);
                    Feed.Say(o.R.Name, Encounters.Pick(Encounters.HermitFlee, o.R.Id + "|f|" + now / 1000), Speech.When(h, "scared", me));
                    break;
            }
            Log.Detail("ENCOUNTER hermit: " + o.R.Name + " " + was + " -> " + phase);
        }

        private void HermitInPhase(HermitOut o, Ped me, float dist, int now)
        {
            int h = o.Ped.Handle;
            if (o.Phase != "camp" && now >= o.NextTurn)
            {
                // on his feet: he keeps you in front of him
                o.NextTurn = now + 4000;
                TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, me.Handle, 3500, 0f, 0f, 0f);
                TASK.TASK_LOOK_AT_ENTITY(h, me.Handle, 4500, 0, 51, 0);
            }
            if (now < o.NextLine)
                return;
            switch (o.Phase)
            {
                case "camp":
                    o.NextLine = now + 14000 + _rng.Next(8000);
                    if (o.R.Id == _newCampId && !o.Asleep && dist < 90f)
                    {
                        _newCampId = null;   // once: shouting at the voice, loud enough to carry
                        Feed.Say("Someone, off in the trees", Encounters.Pick(Encounters.HermitMutters, o.R.Id + "|shout"), Speech.When(h, "angry"));
                        break;
                    }
                    if (!o.Asleep && dist < 35f)
                        Feed.Say(dist > 15f ? "Someone by a fire" : o.R.Name, Encounters.Pick(Encounters.HermitMutters, o.R.Id + "|m|" + now / 14000), Speech.When(h, "uneasy"));
                    break;
                case "warn":
                    if (o.Warned >= 3)
                        return;
                    o.Warned++;
                    o.NextLine = now + 6000;
                    Feed.Say(o.R.Name, Encounters.Pick(Encounters.HermitWarns, o.R.Id + "|w|" + o.Warned + "|" + now / 60000), Speech.When(h, "threat", me.Handle));
                    break;
                case "talk":
                {
                    o.Talks++;
                    o.NextLine = now + 8000;
                    PersonState st = _store.Get(_engine.PersonOfResident(o.R));
                    string line = Encounters.HermitTalkNext(o.R.Id, o.R.Town, st);
                    _engine.KeepResident(o.R, Bridge.Now());
                    Feed.Say(o.R.Name, line, Speech.For(h, line, me.Handle, fear: 30));
                    break;
                }
            }
        }

        private unsafe void EndHermit(string why)
        {
            HermitOut o = _hermit;
            _hermit = null;
            if (o == null)
                return;
            Ped me = Game.Player.Character;
            if (o.Ped != null)
            {
                // the living vanish only where nobody sees it; the dead stay where they fell
                bool alive = o.Ped.Exists() && !o.Ped.IsDead;
                bool seen = alive && ENTITY.IS_ENTITY_ON_SCREEN(o.Ped.Handle);
                Spawner.Release(o.Ped, alive && (!seen || o.Ped.Position.DistanceTo(me.Position) > 120f));
            }
            int f = o.Fire;
            if (f != 0 && ENTITY.DOES_ENTITY_EXIST(f))
            {
                if (ENTITY.IS_ENTITY_ON_SCREEN(f) && ENTITY.GET_ENTITY_COORDS(f, true, false).DistanceTo(me.Position) < 120f)
                    ENTITY.SET_OBJECT_AS_NO_LONGER_NEEDED(&f);
                else
                    OBJECT.DELETE_OBJECT(&f);
            }
            if (o.Ped != null)
                Log.Write("ENCOUNTER hermit: " + o.R.Name + " " + why);
        }

        /// The hermit on the ticker (the add-on's own are skipped there otherwise).
        private void HermitNear(Ped ped, float d)
        {
            if (_hermit.Asleep)
                return;   // nothing on the ticker while he sleeps
            Resident r = _hermit.R;
            long now = Bridge.Now();
            _near.Add(new NearThought
            {
                Key = r.Id,
                Name = Encounters.HermitLabel(r.Name),
                Distance = d + 2f,
                Text = () =>
                {
                    PersonState st = _store.Find(r.Id);
                    if (st == null || (now / 180) % 2 == 0)
                        return Encounters.Pick(Encounters.HermitThoughts, r.Id + "|" + now / 180);
                    return Hosts.Thought(_engine.PersonOfResident(r), st, _engine.Player, r.Id + "|" + now / 180);
                }
            });
        }

        private string HermitsStatus()
        {
            if (!_cfg.Hermits)
                return "off";
            Vector3 pos = Game.Player.Character.Position;
            List<Resident> camps = _engine.Camps();
            int waiting = _store.Residents.Count(x => x.Fled && !x.Dead && !x.HasCamp);
            string nearest = camps.Count == 0 ? "" : ", nearest " + (int)camps.Min(c => CampPos(c).DistanceTo(pos)) + " m";
            return _cfg.HermitChance + "% per roll, " + camps.Count + " camp(s)" + nearest + ", " + waiting + " fled and waiting for one" +
                   (_hermit != null ? ", out: " + _hermit.R.Name + " (" + _hermit.Phase + ")" : "") + (DoneToday("hermit") ? ", a camp turned up today" : "");
        }
    }
}
