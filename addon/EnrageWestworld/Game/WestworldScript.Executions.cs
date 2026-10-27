// An execution on the road (Core/Executions.cs; [Encounters] Executions). Out of the towns, a roll once a day: one of
// the nearest town's regulars (a man, saved with his model and outfit, not walking about as himself just now) is
// spawned out of sight 45-70 m off, kneeling, two men behind him with guns on him: the creditor himself and a hired
// man, two hired men, the man he's feuding with and his brother, or the region's outlaws, as his worry says. A mark on
// the map. Come near and he calls out to you, they warn you off (said to you, so the reason reaches you), and his
// thought shows over his head. Shoot or rush them and they fight you (self-defence, as with the crying woman's men);
// kill or drive them off and he's saved (Engine.Rescued: the town hears, he thinks of it for days). Wait, or ride off,
// and they shoot him (the people ledger notes it by their hand; Engine.Executed: his kin, and the town once he's found).
// Never seen him at all in four minutes: it ends quietly and nobody dies. The test: the settings panel's Encounters
// page, "Stage an execution nearby". Not yet seen in game.
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
        private sealed class Execution
        {
            public string Phase = "spawn";   // spawn, held, fight, last, shot, saved, leaving
            public Resident R, BossR;
            public Executions.Plan Plan;
            public Ped Captive;
            public readonly List<Ped> Men = new List<Ped>();
            public readonly List<uint> MenModels = new List<uint>();
            public Blip Mark;
            public string Town, Region;
            public int StartedAt, NoticedAt, ShootAt, NextLine, PhaseUntil, Tries, AimSince, Lines;
            public bool Noticed, Warned, Called, Thought, Test;
        }

        private const string ExecKneel = "WORLD_HUMAN_GRAVE_MOURNING_KNEEL";
        private const int ExecWaitMs = 40000, ExecAfterWarnMs = 25000, ExecUnseenMs = 240000;
        private Execution _exec;
        private bool _execRequested;
        private KeySpec _execKey;
        private int _execCount;
        private string _execLast;

        private bool IsExecutionPed(int h)
        {
            Execution e = _exec;
            return e != null && ((e.Captive != null && e.Captive.Handle == h) || e.Men.Any(m => m != null && m.Handle == h));
        }

        /// Why there can't be one here and now; null when there can.
        private string ExecutionNotNow()
        {
            string town = _bridge.TownOnly(Game.Player.Character.Position);
            if (town != null)
                return "you're in " + town + " (only away from the towns)";
            int hour = CLOCK.GET_CLOCK_HOURS();
            if (hour < 5 || hour >= 22)
                return "it's " + hour.ToString("00") + ":00 (from 05:00 to 22:00)";
            if (DoneToday("execution"))
                return "there was one already today";
            return null;
        }

        /// A regular of that town to put on his knees: a man, saved with his look, not bound to anyone in the world
        /// now; those whose worry gives a reason first. Null if there's nobody.
        private Resident ExecutionCaptive(string town)
        {
            HashSet<Resident> bound = new HashSet<Resident>(_resBind.Values.Select(b => b.R));
            Func<Resident, bool> fits = x => x.Regular && !x.Dead && !x.Fled && x.PostId == null && !x.Woman && x.Model != 0 && !bound.Contains(x);
            List<Resident> pool = _store.Residents.Where(x => fits(x) && x.Town == town).ToList();
            if (pool.Count == 0)
                pool = _store.Residents.Where(fits).ToList();
            if (pool.Count == 0)
                return null;
            string seed = Bridge.Now() / 1440 + "|exec";
            return pool.OrderByDescending(x => _engine.ExecutionPlan(x, town, x.Id + seed).Reason != "witness" ? 1 : 0).ThenBy(x => Voice.Hash(x.Id + seed)).First();
        }

        private void StartExecution(int now, bool test)
        {
            Ped me = Game.Player.Character;
            string region = _bridge.TownAt(me.Position);
            string town = Rumours.NearestTown(me.Position.X, me.Position.Y) ?? region;
            Resident r = ExecutionCaptive(town);
            if (r == null)
            {
                Log.Write("EXECUTION: nobody to put there (no town regulars saved yet)");
                return;
            }
            string seed = r.Id + "|" + Bridge.Now() / 1440;
            Executions.Plan plan = _engine.ExecutionPlan(r, region ?? town, seed);
            Execution e = new Execution { R = r, Plan = plan, Town = town ?? r.Town, Region = region, StartedAt = now, Test = test };   // Town: nearest where it happens, for the news
            if (plan.BossId != null)
            {
                HashSet<Resident> bound = new HashSet<Resident>(_resBind.Values.Select(b => b.R));
                Resident boss = _store.Residents.FirstOrDefault(x => x.Id == plan.BossId);
                if (boss != null && boss.Regular && !boss.Dead && !boss.Fled && boss.PostId == null && !boss.Woman && boss.Model != 0 && !bound.Contains(boss) && boss != r)
                    e.BossR = boss;   // the man himself, as the town knows him
            }
            if (plan.Captors == "boss" && e.BossR == null)
                plan.Captors = "hired";   // he isn't someone we can put there: he sent men
            if (plan.Captors == "outlaws")
            {
                string[] models = Executions.GangModels(plan.GangKey);
                int n = 2 + (_rng.Next(100) < 30 ? 1 : 0);
                for (int i = 0; i < n; i++)
                    e.MenModels.Add(Natives.Hash(models[_rng.Next(models.Length)]));
            }
            else
            {
                if (e.BossR != null)
                    e.MenModels.Add(e.BossR.Model);
                while (e.MenModels.Count < 2)
                    e.MenModels.Add(Spawner.ModelFor(null, false, _rng.Next()));
            }
            _exec = e;
            Log.Write("EXECUTION begins" + (test ? " (test)" : "") + ": " + r.Name + " (" + r.Trade + " of " + r.Town + "), " + plan.Reason + " (" + plan.Boss + "), held by " +
                      (e.BossR != null ? e.BossR.Name + " and a hired man" : plan.Captors == "outlaws" ? e.MenModels.Count + " of " + plan.Gang : plan.Captors == "kin" ? "the " + plan.Boss + " men" : "two men " + plan.Boss + " hired"));
        }

        private void ExecutionStep(int now)
        {
            Execution e = _exec;
            if (e.Phase == "spawn")
            {
                ExecutionSpawn(e, now);
                return;
            }
            Ped me = Game.Player.Character;
            foreach (Ped m in e.Men)
                if (m != null && m.Exists() && !m.IsDead)
                    _cameAtYouSeen[m.Handle] = now;   // men holding a man at gunpoint: shooting them is self-defence, the whole time
            bool captiveThere = e.Captive != null && e.Captive.Exists();
            bool captiveAlive = captiveThere && !e.Captive.IsDead;
            List<Ped> men = e.Men.Where(m => m != null && m.Exists() && !m.IsDead).ToList();
            float d = e.Captive != null && e.Captive.Exists() ? e.Captive.Position.DistanceTo(me.Position) : 999f;
            if (!captiveThere && e.Phase != "shot" && e.Phase != "leaving")
            {
                EndExecution("he's gone from the world");   // not seen to die: nothing comes of it
                return;
            }
            if (!captiveAlive && (e.Phase == "held" || e.Phase == "fight" || e.Phase == "last"))
            {
                ExecutionCaptiveDied(e, now);
                return;
            }
            if (!captiveAlive && e.Phase == "saved")
            {
                Log.Write("EXECUTION " + e.R.Name + ": died after he was saved (the ledger has how)");
                e.Phase = "leaving";
                e.PhaseUntil = now + 30000;
                return;
            }
            switch (e.Phase)
            {
                case "held":
                    ExecutionHeld(e, men, d, now);
                    break;
                case "fight":
                    // saved once none of them is left standing near him, or three minutes on whatever became of them
                    if (men.All(m => m.Position.DistanceTo(e.Captive.Position) > 70f) || now >= e.PhaseUntil)
                        ExecutionSaved(e, now);
                    break;
                case "last":
                    if (now >= e.PhaseUntil)
                        ExecutionShoot(e, men, now);
                    break;
                case "shot":
                    if (now >= e.PhaseUntil)
                        ExecutionAfter(e, men, now);
                    break;
                case "saved":
                    if (now >= e.PhaseUntil)
                    {
                        if (e.Captive.Exists() && !e.Captive.IsDead)
                            TASK.TASK_WANDER_STANDARD(e.Captive.Handle, 10f, 10);
                        e.Phase = "leaving";
                        e.PhaseUntil = now + 30000;
                    }
                    break;
                case "leaving":
                    if (now >= e.PhaseUntil || men.All(m => m.Position.DistanceTo(me.Position) > 150f) && (e.Captive == null || !e.Captive.Exists() || e.Captive.IsDead || e.Captive.Position.DistanceTo(me.Position) > 80f))
                        EndExecution("over");
                    break;
            }
        }

        private void ExecutionSpawn(Execution e, int now)
        {
            if (now - e.StartedAt > 20000 || e.Tries > 8)
            {
                EndExecution("nowhere hidden to put them");
                return;
            }
            if (!Spawner.Ready(e.R.Model) || e.MenModels.Any(m => !Spawner.Ready(m)))
                return;
            Ped me = Game.Player.Character;
            Vector3 at;
            if (!EncounterSpot(me, 45f, 70f, out at))
            {
                e.Tries++;
                return;
            }
            float heading = (float)_rng.NextDouble() * 360f;
            Ped c = Spawner.Spawn(e.R.Model, at, heading);
            if (c == null || ENTITY.IS_ENTITY_IN_WATER(c.Handle))
            {
                if (c != null)
                    Spawner.Release(c, true);
                e.Tries++;
                return;
            }
            e.Captive = c;
            int h = c.Handle;
            PED._EQUIP_META_PED_OUTFIT_PRESET(h, e.R.Outfit, false);   // himself, as the town knows him
            PED._UPDATE_PED_VARIATION(h, false, true, true, true, false);
            _resBind[h] = new ResBind { R = e.R, Model = e.R.Model };
            _encounterPeds.Add(h);
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, true);
            PED.SET_PED_KEEP_TASK(h, true);
            TASK.TASK_START_SCENARIO_IN_PLACE_HASH(h, Natives.Hash(ExecKneel), -1, true, 0u, -1f, false);
            double rad = heading * Math.PI / 180.0;
            Vector3 back = new Vector3((float)Math.Sin(rad), (float)-Math.Cos(rad), 0f);   // behind him
            Vector3 side = new Vector3(back.Y, -back.X, 0f);
            for (int i = 0; i < e.MenModels.Count; i++)
            {
                Vector3 spot = at + back * 2.6f + side * ((i - (e.MenModels.Count - 1) / 2f) * 1.6f);
                Vector3 ground;
                if (Natives.StandSpot(spot, at.Z, out ground))
                    spot = ground;
                Ped m = Spawner.Spawn(e.MenModels[i], spot, heading);
                if (m == null)
                    continue;
                int mh = m.Handle;
                if (i == 0 && e.BossR != null)
                {
                    PED._EQUIP_META_PED_OUTFIT_PRESET(mh, e.BossR.Outfit, false);
                    PED._UPDATE_PED_VARIATION(mh, false, true, true, true, false);
                    _resBind[mh] = new ResBind { R = e.BossR, Model = e.BossR.Model };
                }
                m.GiveWeapon(eWeapon.RevolverCattleman, 60, eWeaponAttachPoint.PistolR, true);
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(mh, true);
                PED.SET_PED_KEEP_TASK(mh, true);
                TASK.TASK_AIM_GUN_AT_ENTITY(mh, h, -1, false, 0);
                _encounterPeds.Add(mh);
                _cameAtYouSeen[mh] = now;
                e.Men.Add(m);
            }
            if (e.Men.Count == 0)
            {
                EndExecution("the men couldn't be put there");
                return;
            }
            try
            {
                e.Mark = c.AddBlip(BlipType.WhiteDot);
                e.Mark.Label = "A man in trouble";
            }
            catch
            {
            }
            e.Phase = "held";
            e.NextLine = now + 2500;
            Log.Write("EXECUTION " + e.R.Name + ": on his knees " + (int)at.DistanceTo(me.Position) + " m away, " + e.Men.Count + " men behind him");
        }

        private unsafe void ExecutionHeld(Execution e, List<Ped> men, float d, int now)
        {
            Ped me = Game.Player.Character;
            int ch = e.Captive.Handle;
            bool seen = ENTITY.IS_ENTITY_ON_SCREEN(ch) && ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(me.Handle, ch, 17);
            // he steps in: a shot at them, a gun on one of them a moment, or right up to them
            ulong slot = 0;   // out-parameters take 8 bytes
            int aimed = PLAYER.GET_ENTITY_PLAYER_IS_FREE_AIMING_AT(Game.Player.Handle, (int*)&slot) ? (int)slot : 0;
            bool aiming = aimed != 0 && men.Any(m => m.Handle == aimed);
            e.AimSince = aiming ? (e.AimSince == 0 ? now : e.AimSince) : 0;
            if (men.Count < e.Men.Count || men.Any(m => ENTITY.HAS_ENTITY_BEEN_DAMAGED_BY_ENTITY(m.Handle, me.Handle, true, false)) ||
                (aiming && now - e.AimSince > 1200) || men.Any(m => m.Position.DistanceTo(me.Position) < 6f))
            {
                foreach (Ped m in men)
                {
                    MakeHostile(m, now);
                    TASK.TASK_COMBAT_PED(m.Handle, me.Handle, 0, 16);
                }
                e.Phase = "fight";
                e.PhaseUntil = now + 180000;
                Log.Write("EXECUTION " + e.R.Name + ": you stepped in; " + men.Count + " men fight you");
                return;
            }
            if (!e.Noticed)
            {
                if (d < 40f || (d < 75f && seen))
                {
                    e.Noticed = true;
                    e.NoticedAt = now;
                    e.ShootAt = now + ExecWaitMs;
                    _engine.ExecutionSeen(e.R, Bridge.Now());
                    Log.Write("EXECUTION " + e.R.Name + ": you found them, " + (int)d + " m off");
                }
                else if (now - e.StartedAt > (e.Test ? 2 * ExecUnseenMs : ExecUnseenMs))
                {
                    EndExecution("you never came near; nothing came of it");
                    return;
                }
            }
            if (e.Noticed)
            {
                if (!e.Called && d < 60f)
                {
                    e.Called = true;
                    Feed.Say(e.R.Name, Executions.CallsToYou(e.R.Id + now), Speech.When(ch, "scared", me.Handle), ch, toYou: true);
                }
                if (!e.Warned && d < 28f && men.Count > 0)
                {
                    e.Warned = true;
                    e.ShootAt = Math.Min(e.ShootAt, now + ExecAfterWarnMs);
                    Ped m = men[0];
                    Feed.Say(ExecManName(e, m), Executions.WarnYou(e.Plan, e.R.Id + now), Speech.When(m.Handle, "threat", me.Handle), m.Handle, toYou: true);
                }
                if (!e.Thought && d < 30f && seen)
                {
                    e.Thought = true;
                    _ticker.Push(e.R.Name + ", thinking", Executions.During(e.Plan, e.R.Id), "near", ch);
                }
                if (d > 110f)
                    e.ShootAt = Math.Min(e.ShootAt, now);   // he rode on
            }
            if (now >= e.NextLine && men.Count > 0)
            {
                // overheard: the men and him, by turns
                e.NextLine = now + 6000 + _rng.Next(2500);
                string seed = e.R.Id + "|" + e.Lines;
                if (e.Lines++ % 2 == 0)
                {
                    Ped m = men[e.Lines / 2 % men.Count];
                    Feed.Say(ExecManName(e, m), Executions.Threat(e.Plan, seed), Speech.When(m.Handle, "threat", ch), m.Handle);
                }
                else
                    Feed.Say(e.R.Name, Executions.Plead(e.Plan, seed), Speech.When(ch, "plead", men[0].Handle), ch);
            }
            if (e.Noticed && now >= e.ShootAt)
            {
                Feed.Say(e.R.Name, Executions.LastWords(e.R.Id), Speech.When(ch, "plead", men.Count > 0 ? men[0].Handle : me.Handle), ch);
                e.Phase = "last";
                e.PhaseUntil = now + 2200;
            }
        }

        private static string ExecManName(Execution e, Ped m)
        {
            return e.BossR != null && e.Men.Count > 0 && e.Men[0] == m ? e.BossR.Name : "A gunman";
        }

        private void ExecutionShoot(Execution e, List<Ped> men, int now)
        {
            int ch = e.Captive.Handle;
            Ped shooter = men.FirstOrDefault();
            Vector3 head = e.Captive.Position + new Vector3(0f, 0f, 0.55f);
            if (shooter != null)
            {
                Vector3 from = shooter.Position + new Vector3(0f, 0f, 1.3f);
                MISC.SHOOT_SINGLE_BULLET_BETWEEN_COORDS(from.X, from.Y, from.Z, head.X, head.Y, head.Z, 500, true, Natives.Hash("WEAPON_REVOLVER_CATTLEMAN"), shooter.Handle, true, false, 1000f, false);
            }
            if (!ENTITY.IS_ENTITY_DEAD(ch))
                ENTITY.SET_ENTITY_HEALTH(ch, 0, shooter != null ? shooter.Handle : ch);
            LedgerNote(ch, e.R.Id, e.R.Name, "resident", "a revolver", "another");
            _engine.Executed(e.R, e.Plan, e.Town, Bridge.Now());
            if (e.Mark != null && e.Mark.Exists())
                e.Mark.Delete();
            e.Phase = "shot";
            e.PhaseUntil = now + 2500;
            _execCount++;
            _execLast = e.R.Name + ": shot (" + e.Plan.Reason + ")";
            Log.Write("EXECUTION " + e.R.Name + ": they shot him");
        }

        private void ExecutionAfter(Execution e, List<Ped> men, int now)
        {
            Ped me = Game.Player.Character;
            if (men.Count > 0)
                Feed.Say(ExecManName(e, men[0]), Executions.Done(e.Plan, e.R.Id), Speech.When(men[0].Handle, "storm", me.Handle), men[0].Handle);
            foreach (Ped m in men)
            {
                TASK.CLEAR_PED_TASKS(m.Handle, false, false);
                Vector3 away = m.Position + (m.Position - me.Position).Normalized * 150f;
                TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(m.Handle, away.X, away.Y, away.Z, 1.2f, -1, 1f, 0, 0f);
            }
            e.Phase = "leaving";
            e.PhaseUntil = now + 90000;
        }

        private void ExecutionSaved(Execution e, int now)
        {
            Ped me = Game.Player.Character;
            int ch = e.Captive.Handle;
            TASK.CLEAR_PED_TASKS(ch, false, false);
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(ch, false);
            TASK.TASK_TURN_PED_TO_FACE_ENTITY(ch, me.Handle, 3000, 0f, 0f, 0f);
            TASK.TASK_LOOK_AT_ENTITY(ch, me.Handle, 8000, 0, 51, 0);
            Feed.Say(e.R.Name, Executions.Thanks(e.Plan, e.R.Id), Speech.When(ch, "thanks", me.Handle), ch, toYou: true);
            Vector3 p = e.Captive.Position;
            _engine.Rescued(e.R, e.Plan, e.Region ?? e.Town, ZoneAt(p), p.X, p.Y, p.Z, Bridge.Now());
            if (e.Mark != null && e.Mark.Exists())
                e.Mark.Delete();
            e.Phase = "saved";
            e.PhaseUntil = now + 8000;
            _execCount++;
            _execLast = e.R.Name + ": saved (" + e.Plan.Reason + ")";
            Log.Write("EXECUTION " + e.R.Name + ": you saved him");
        }

        /// He died before they shot him on purpose: in the fight, by their hand or the player's (the player's is a
        /// killing like any other, the deeds see to that).
        private void ExecutionCaptiveDied(Execution e, int now)
        {
            int ch = e.Captive != null ? e.Captive.Handle : 0;
            bool byYou = ch != 0 && ENTITY.DOES_ENTITY_EXIST(ch) && PED.GET_PED_SOURCE_OF_DEATH(ch) == Game.Player.Character.Handle;
            if (!byYou)
                _engine.Executed(e.R, e.Plan, e.Town, Bridge.Now());
            if (e.Mark != null && e.Mark.Exists())
                e.Mark.Delete();
            Log.Write("EXECUTION " + e.R.Name + ": died " + (byYou ? "by your hand" : "in it") + " (" + e.Phase + ")");
            _execLast = e.R.Name + ": died " + (byYou ? "by your hand" : "in it");
            e.Phase = "leaving";
            e.PhaseUntil = now + 60000;
        }

        private void EndExecution(string why)
        {
            Execution e = _exec;
            _exec = null;
            if (e == null)
                return;
            Ped me = Game.Player.Character;
            try
            {
                if (e.Mark != null && e.Mark.Exists())
                    e.Mark.Delete();
            }
            catch
            {
            }
            foreach (Ped p in new[] { e.Captive }.Concat(e.Men))
            {
                if (p == null)
                    continue;
                // the living vanish only where nobody sees it, and never one enRAGE has looked at; the dead stay where they fell
                bool alive = p.Exists() && !p.IsDead;
                bool seen = alive && ENTITY.IS_ENTITY_ON_SCREEN(p.Handle);
                bool enrageKnows = alive && _bridge != null && _bridge.Looked(p.Handle);
                Spawner.Release(p, alive && !enrageKnows && (!seen || p.Position.DistanceTo(me.Position) > 120f));
            }
            if (e.Phase != "spawn")
                MarkDone("execution");   // nowhere to put them doesn't use up the day
            Log.Write("EXECUTION " + e.R.Name + ": over (" + why + ")");
        }

        /// On the ticker: his thought over his head while he kneels (the men have none).
        private void ExecutionNear(Ped ped, float d)
        {
            Execution e = _exec;
            if (e == null || e.Captive == null || ped.Handle != e.Captive.Handle || e.Phase != "held" && e.Phase != "fight")
                return;
            Resident r = e.R;
            long now = Bridge.Now();
            _near.Add(new NearThought
            {
                Handle = ped.Handle,
                Key = r.Id,
                Name = r.Name + ", on his knees",
                Distance = d,
                Text = () => Executions.During(e.Plan, r.Id + "|" + now / 3)
            });
        }

        private string ExecutionsStatus()
        {
            if (!_cfg.Executions)
                return "off";
            return _cfg.ExecutionChance + "% (05:00-22:00, away from the towns)" + (DoneToday("execution") ? ", done today" : "") +
                   (_exec != null ? "; going on: " + _exec.R.Name + " (" + _exec.Phase + ", " + _exec.Plan.Reason + ")" : "") + "; this session " + _execCount + (_execLast != null ? ", last " + _execLast : "");
        }
    }
}
