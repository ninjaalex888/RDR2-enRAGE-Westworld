// Encounters in the game (see Core/Encounters.cs). Every EncounterEveryMinutes of play, when the time and place are
// right, a roll for each kind that's switched on; at most one runs at a time, never during a mission, a stalker, a
// street scene, a wedding, a plea or a theft, and never indoors, wanted, fighting or in water.
//   The crying woman: spawned out of sight 30-55 m off in the wilds at night, kneeling and sobbing, calling for help
//   in her own recorded voice. Within a few steps (or with a gun on her) she lunges with a knife and runs, and later
//   cries again somewhere else; or two or three men step out of the brush and attack; or she says something that
//   makes no sense and walks off into the dark. Anyone who attacks is hostile to the player from the start, so
//   fighting them off is self-defence (see Watch.cs).
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
        private sealed class Crying
        {
            public string Phase = "spawn";   // spawn, crying, knife, fled, crying2, ambush, dark, leaving
            public Ped Woman;
            public readonly List<Ped> Men = new List<Ped>();
            public uint WomanModel, ManModel;
            public uint OwnGroup;
            public int StartedAt, NextCry, PhaseUntil, Tries;
            public bool Moved, Test;
        }

        private static readonly string[] HelpLines = { "SCARED_HELP", "PANIC_HELP", "GENERIC_FRIGHTENED_HIGH" };
        private const string Kneel = "WORLD_HUMAN_GRAVE_MOURNING_KNEEL";

        private Crying _crying;
        private readonly HashSet<int> _encounterPeds = new HashSet<int>();   // everyone the crying woman's encounter made, kept after it ends so a death is always judged the same way
        private int _nextEncounterRoll, _nextEncounterTick;
        private uint _hostileGroup;
        private string _encounterWait;
        private string _encounterRequested;   // crying, hermit or gathering, from the Testing page (the execution has _execRequested)
        private Action _aEncounters;

        private void EncountersStep(int now)
        {
            if (_aEncounters == null)
                _aEncounters = () => EncountersTick(_now);
            Safe("encounters", _aEncounters);
        }

        private bool EncounterLive { get { return _crying != null || _hermit != null || _gathering != null || _exec != null || _visitor != null || _traveller != null || _campfire != null || _supper != null; } }

        /// What's going on, for the log and the stalker key.
        private string EncounterWhat
        {
            get
            {
                if (_crying != null)
                    return "the crying woman is out";
                if (_visitor != null)
                    return "a figure is abroad in the dark";
                if (_traveller != null)
                    return "a traveller is on the road";
                if (_campfire != null)
                    return "strangers sit at a fire";
                if (_supper != null)
                    return "a man on the road has supper waiting";
                if (_exec != null)
                    return "a man is held at gunpoint nearby";
                if (_hermit != null)
                    return _hermit.R.Name + " is at " + Their(_hermit.R) + " camp nearby";
                return _gathering != null ? "a gathering is standing out there" : null;
            }
        }

        private void EncountersTick(int now)
        {
            if (now < _nextEncounterTick)
                return;
            _nextEncounterTick = now + (EncounterLive ? 250 : 1000);
            _encounterPeds.RemoveWhere(x => !ENTITY.DOES_ENTITY_EXIST(x));
            if (_crying != null)
                CryingStep(now);
            if (_gathering != null)
                GatherStep(now);
            if (_exec != null)
                ExecutionStep(now);
            if (_visitor != null)
                VisitorStep(now);
            if (_traveller != null)
                TravellerStep(now);
            if (_campfire != null)
                CampfireStep(now);
            if (_supper != null)
                SupperStep(now);
            VisitorBodyStep();   // a body left behind goes once nobody can see (WestworldScript.Visitor.cs)
            HermitStep(now);   // a hermit at his camp, when you come near one
            string asked = _execRequested ? "execution" : _encounterRequested;
            if (asked != null)
            {
                // the Testing page: one staged nearby now, if nothing stands in the way
                _execRequested = false;
                _encounterRequested = null;
                string block = EncounterKindLive(asked) ? "one is out already (" + EncounterWhat + ")"
                    : EncountersFull ? EncountersLive + " encounters are going on (at most " + _cfg.EncountersAtOnce + ")" : EncounterBlocker();
                if (block != null)
                {
                    Log.Write("ENCOUNTER test, " + asked + ": not now: " + block);
                    Hint("Not now: " + block + ".", 5000);
                }
                else
                    StageEncounter(asked, now);
            }
            if (EncountersFull || !_cfg.Encounters)
                return;   // up to [Encounters] EncountersAtOnce at once, the stalker among them (Lex, 2026-10-27: it was one)
            int every = _cfg.EncounterEveryMinutes * 60000;
            if (_nextEncounterRoll == 0 || _nextEncounterRoll - now > every)
            {
                _nextEncounterRoll = now + every;   // the first, or the interval was made shorter
                return;
            }
            if (now < _nextEncounterRoll)
                return;
            _nextEncounterRoll = now + every;
            // since 2026-10-27 a big moment's quiet no longer stops a roll (it held them to a single one); each kind keeps its
            // own rests (once a night, once a day) and its chance
            string why = EncounterBlocker();
            if (why != null)
            {
                _encounterWait = why;
                Log.Write("Encounter roll skipped: " + why);
                return;
            }
            // each kind that isn't out already, in turn from a different one each roll; at most one starts a roll, so several
            // arrive one by one, not together
            List<string> notNow = new List<string>();   // each kind that's on but can't happen here and now, and why
            int first = _encounterTurn++ & 0x7fffffff;
            for (int i = 0; i < 8; i++)
            {
                if (TryEncounter((first + i) % 8, notNow, now))
                {
                    _encounterWait = null;
                    return;
                }
            }
            _encounterWait = notNow.Count > 0 ? string.Join("; ", notNow) : null;
            if (_encounterWait != null)
                Log.Write("Encounter roll skipped: " + _encounterWait);
        }

        private int _encounterTurn;

        /// One kind's chance on this roll: true when it started. A kind already out is passed over (one of each, for now).
        private bool TryEncounter(int kind, List<string> notNow, int now)
        {
            string not, skipped;
            switch (kind)
            {
                case 0:
                    if (!_cfg.CryingWoman || _crying != null)
                        return false;
                    not = CryingNotNow();
                    if (not != null)
                    {
                        notNow.Add("the crying woman: " + not);
                        return false;
                    }
                    if (!EncounterRoll("the crying woman", _cfg.CryingWomanChance))
                        return false;
                    StartCrying(now);
                    return true;
                case 1:
                    if (!_cfg.Executions || _exec != null)
                        return false;
                    not = ExecutionNotNow();
                    if (not != null)
                    {
                        notNow.Add("an execution on the road: " + not);
                        return false;
                    }
                    if (!EncounterRoll("an execution on the road", _cfg.ExecutionChance))
                        return false;
                    StartExecution(now, false);
                    return _exec != null;
                case 2:
                    if (!_cfg.Hermits || _hermit != null)
                        return false;
                    not = HermitNotNow();
                    if (not != null)
                    {
                        notNow.Add("a hermit's camp: " + not);
                        return false;
                    }
                    return EncounterRoll("a hermit's camp", _cfg.HermitChance) && NewCamp(now);
                case 3:
                    if (!_cfg.NightVisitor || _visitor != null)
                        return false;
                    not = VisitorNotNow() ?? Pacing.WhyNot(_store.Pace, "visitor", _placeTown, null, _store.LastGameMin, 0, false, out skipped, true);
                    if (not != null)
                    {
                        notNow.Add("the night visitor: " + not);
                        return false;
                    }
                    if (!EncounterRoll("the night visitor", _cfg.NightVisitorChance))
                        return false;
                    StartVisitor(now);   // Pacing notes it once it's really out (VisitorSpawn)
                    return true;
                case 4:
                    if (!_cfg.Travellers || _traveller != null)
                        return false;
                    not = TravellerNotNow() ?? Pacing.WhyNot(_store.Pace, "traveller", null, null, _store.LastGameMin, 0, false, out skipped, true);
                    if (not != null)
                    {
                        notNow.Add("a traveller: " + not);
                        return false;
                    }
                    if (!EncounterRoll("a traveller asking the way", _cfg.TravellerChance))
                        return false;
                    StartTraveller(now);   // Pacing notes it once he's on the road (TravellerSpawn)
                    return true;
                case 5:
                    if (!_cfg.SupperInvitation || _supper != null)
                        return false;
                    not = SupperNotNow();
                    if (not != null)
                    {
                        notNow.Add("the supper invitation: " + not);
                        return false;
                    }
                    if (!EncounterRoll("the supper invitation", _cfg.SupperChance))
                        return false;
                    StartSupper(now);
                    return true;
                case 6:
                    if (!_cfg.Gatherings || _gathering != null)
                        return false;
                    not = GatherNotNow();
                    if (not != null)
                    {
                        notNow.Add("a gathering: " + not);
                        return false;
                    }
                    return EncounterRoll("a gathering", _cfg.GatheringChance) && StartGathering(now);
                default:
                    if (!_cfg.Campfires || _campfire != null)
                        return false;
                    not = CampfireNotNow() ?? Pacing.WhyNot(_store.Pace, "campfire", null, null, _store.LastGameMin, 0, false, out skipped, true);
                    if (not != null)
                    {
                        notNow.Add("strangers at a campfire: " + not);
                        return false;
                    }
                    if (!EncounterRoll("strangers at a campfire", _cfg.CampfireChance))
                        return false;
                    StartCampfire(now);   // Pacing notes it once the fire is lit (CampfireSpawn)
                    return true;
            }
        }

        /// How many are going on: the encounters, a hermit only while he's taken up with you, and the stalker.
        private int EncountersLive
        {
            get
            {
                return (_crying != null ? 1 : 0) + (_exec != null ? 1 : 0) + (HermitWithYou() ? 1 : 0) + (_gathering != null ? 1 : 0) + (_visitor != null ? 1 : 0) +
                       (_traveller != null ? 1 : 0) + (_supper != null ? 1 : 0) + (_campfire != null ? 1 : 0) + (_stalker != null ? 1 : 0);
            }
        }

        private bool EncountersFull { get { return EncountersLive >= _cfg.EncountersAtOnce; } }

        private bool EncounterKindLive(string kind)
        {
            switch (kind)
            {
                case "execution": return _exec != null;
                case "crying": return _crying != null;
                case "hermit": return _hermit != null;
                case "visitor": return _visitor != null;
                case "traveller": return _traveller != null;
                case "supper": return _supper != null;
                case "campfire": return _campfire != null;
                case "gathering": return _gathering != null;
                default: return false;
            }
        }

        /// The Testing page's key or row for an encounter: asked for now, or why not. What to say back.
        private string RequestEncounter(string kind, string how, out bool asked)
        {
            asked = false;
            string off = !_cfg.Encounters ? "Encounters are off ([Encounters] Encounters)."
                : kind == "execution" ? (_cfg.Executions ? null : "Executions are off ([Encounters] Executions).")
                : kind == "crying" ? (_cfg.CryingWoman ? null : "The crying woman is off ([Encounters] CryingWoman).")
                : kind == "hermit" ? (_cfg.Hermits ? null : "Hermits are off ([Encounters] Hermits; they need West World on too).")
                : kind == "visitor" ? (_cfg.NightVisitor ? null : "The night visitor is off ([Encounters] NightVisitor).")
                : kind == "traveller" ? (_cfg.Travellers ? null : "Travellers are off ([Encounters] Travellers).")
                : kind == "supper" ? (_cfg.SupperInvitation ? null : "The supper invitation is off ([Encounters] SupperInvitation).")
                : kind == "campfire" ? (_cfg.Campfires ? null : "Campfires are off ([Encounters] Campfires).")
                : _cfg.Gatherings ? null : "Gatherings are off ([Encounters] Gatherings; they need West World on too).";
            if (off != null)
                return off;
            if (EncounterKindLive(kind) || _execRequested || _encounterRequested != null)
                return "One is going on already" + (EncounterWhat != null ? " (" + EncounterWhat + ")" : string.Empty) + ". Let it finish first.";   // a second press used to queue another
            if (EncountersFull)
                return EncountersLive + " encounters are going on already (at most " + _cfg.EncountersAtOnce + ", [Encounters] EncountersAtOnce). Let one finish first.";
            if (kind == "execution")
                _execRequested = true;
            else
                _encounterRequested = kind;
            asked = true;
            Log.Write("ENCOUNTER test, " + kind + " (" + how + "): staging one near you");
            switch (kind)
            {
                case "execution":
                    return "An execution will be staged near you, out of sight. Look for the mark on the map.";
                case "crying":
                    return "A woman will be crying somewhere near you, out of sight. Listen for her.";
                case "hermit":
                    return "A hermit's camp will be made near you, out of sight. Go looking for it.";
                case "visitor":
                    return "Something will be out in the dark near you, in town. Go looking, and don't look away.";
                case "traveller":
                    return "Somebody on foot will be waiting further along the road. Ride on and see.";
                case "supper":
                    return "A man will be waiting further along the road with supper on. A test takes no money.";
                case "campfire":
                    return "A fire will be lit near you, out of sight, with men round it. Go and see.";
                default:
                    return "The awake will gather somewhere near you, out of sight.";
            }
        }

        private string KeyOf(string kind)
        {
            if (kind == "visitor" || kind == "traveller" || kind == "supper" || kind == "campfire")
                return "its Testing page row";
            KeySpec k = kind == "execution" ? _execKey : kind == "crying" ? _cryingKey : kind == "hermit" ? _hermitKey : _gatherKey;
            return k.ToString();
        }

        /// A test: the encounter now, whatever the hour and wherever you are (a hermit's camp, which stays, only outside the towns).
        private void StageEncounter(string kind, int now)
        {
            switch (kind)
            {
                case "execution":
                    StartExecution(now, true);
                    break;
                case "crying":
                    StartCrying(now, true);
                    break;
                case "traveller":
                    string notT = TravellerNotNow(true);
                    if (notT != null)
                    {
                        Log.Write("ENCOUNTER test, traveller: not now: " + notT);
                        Hint("Not now: " + notT + ".", 5000);
                    }
                    else
                        StartTraveller(now, true);
                    break;
                case "campfire":
                    string notC = CampfireNotNow(true);
                    if (notC != null)
                    {
                        Log.Write("ENCOUNTER test, campfire: not now: " + notC);
                        Hint("Not now: " + notC + ".", 5000);
                    }
                    else
                        StartCampfire(now, true);
                    break;
                case "supper":
                    string notS = SupperNotNow(true);
                    if (notS != null)
                    {
                        Log.Write("ENCOUNTER test, supper: not now: " + notS);
                        Hint("Not now: " + notS + ".", 5000);
                    }
                    else
                        StartSupper(now, true);
                    break;
                case "visitor":
                    string notV = VisitorNotNow(true);
                    if (notV != null)
                    {
                        Log.Write("ENCOUNTER test, visitor: not now: " + notV);
                        Hint("Not now: " + notV + ".", 5000);
                    }
                    else
                        StartVisitor(now, true);
                    break;
                case "hermit":
                    string not = HermitNotNow(true);
                    if (not != null)
                    {
                        Log.Write("ENCOUNTER test, hermit: not now: " + not);
                        Hint("No camp now: " + not + ".", 5000);
                    }
                    else if (!NewCamp(now, true))
                        Hint("Nowhere out of sight for a camp here. Try somewhere more open.", 5000);
                    break;
                default:
                    if (!StartGathering(now, true))
                        Hint("Nowhere out of sight for them here. Try somewhere more open.", 5000);
                    break;
            }
        }

        private bool EncounterRoll(string what, int chance)
        {
            bool hit = _rng.Next(100) < chance;
            Log.Write("Encounter roll, " + what + " (" + chance + "%): " + (hit ? "yes" : "no"));
            return hit;
        }

        /// Why the crying woman can't come here and now; null when she can.
        private string CryingNotNow()
        {
            string town = _bridge.TownOnly(Game.Player.Character.Position);   // TownAt gives the region out in the wilds
            if (town != null)
                return "you're in " + town + " (she's only out in the wilds)";
            int hour = CLOCK.GET_CLOCK_HOURS();
            if (!Encounters.Night(hour, 22, 4))
                return "it's " + hour.ToString("00") + ":00 (she's out from 22:00 to 04:00)";
            if (DoneToday("crying"))
                return "she was out already tonight";
            return null;
        }

        /// What keeps an encounter from starting now, in words; null when nothing does.
        private string EncounterBlocker()
        {
            string b = StalkBlocker();
            if (b != null)
                return b;
            if (_director.Busy || _wedding != null || _plea != null || _theft != null)
                return "something else is going on";
            if (Game.Player.Character.IsDead)
                return "you're dead";
            return null;
        }

        /// Encounters keep to once a night: nights run noon to noon, so ten at night and two in the morning are the same one.
        private static int NightIndex()
        {
            return (int)((Bridge.Now() - 720) / 1440);
        }

        private bool DoneToday(string kind)
        {
            int c;
            return _store.Counts.TryGetValue("enc:" + kind, out c) && c == NightIndex();
        }

        private void MarkDone(string kind)
        {
            _store.Counts["enc:" + kind] = NightIndex();
        }

        // ---- the crying woman ---------------------------------------------------------------------------

        private void StartCrying(int now, bool test = false)
        {
            _crying = new Crying
            {
                StartedAt = now,
                Test = test,
                WomanModel = Spawner.ModelFor(null, true, _rng.Next()),
                ManModel = Spawner.ModelFor(null, false, _rng.Next())
            };
            if (!test)
                Pacing.NoteBig(_store.Pace, _store.LastGameMin);   // a big moment: the quiet starts (a test's doesn't count)
            Log.Write("ENCOUNTER crying woman: begins, game time " + Clock(Bridge.Now()) + (test ? " (a test)" : string.Empty));
        }

        private void CryingStep(int now)
        {
            Crying c = _crying;
            Ped me = Game.Player.Character;
            if (MISC.GET_MISSION_FLAG() || me.IsDead)
            {
                EndCrying(MISC.GET_MISSION_FLAG() ? "a mission started" : "you died");
                return;
            }
            if (c.Phase == "spawn")
            {
                CryingSpawn(now);
                return;
            }
            Ped w = c.Woman;
            if (w == null || !w.Exists() || w.IsDead)
            {
                if (c.Phase != "ambush" || c.Men.All(m => m == null || !m.Exists() || m.IsDead))
                {
                    EndCrying(w != null && w.Exists() && w.IsDead ? "she's dead" : "she's gone");
                    return;
                }
            }
            // in an ambush the men are what matters, not the woman running off
            Ped near = c.Phase == "ambush"
                ? c.Men.Where(m => m != null && m.Exists() && !m.IsDead).OrderBy(m => m.Position.DistanceTo(me.Position)).FirstOrDefault()
                : w;
            float dist = near != null && near.Exists() ? near.Position.DistanceTo(me.Position) : 999f;
            bool waiting = c.Phase == "crying" || c.Phase == "crying2";
            if (dist > 160f || (waiting && now - c.StartedAt > 8 * 60000))
            {
                EndCrying(dist > 160f ? "you rode away" : "the night moved on");
                return;
            }
            int h = w != null && w.Exists() ? w.Handle : 0;
            bool seen = h != 0 && ENTITY.IS_ENTITY_ON_SCREEN(h) && ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(me.Handle, h, 17);
            switch (c.Phase)
            {
                case "crying":
                case "crying2":
                {
                    Spawner.Ready(c.ManModel);   // keeps the men's model streaming in, in case she isn't alone
                    if (now >= c.NextCry)
                    {
                        c.NextCry = now + 7000 + _rng.Next(5000);
                        Speech.Say(h, HelpLines[_rng.Next(HelpLines.Length)], 0);
                        if (dist < 90f)
                            Feed.Say(dist > 15f ? "A woman, somewhere in the dark" : "The crying woman", Encounters.Pick(Encounters.Cries, h + "|" + now / 9000), toYou: true);
                    }
                    bool aimed = PLAYER.IS_PLAYER_FREE_AIMING_AT_ENTITY(Game.Player.Handle, h);
                    if (dist < 6f || (dist < 25f && aimed))
                    {
                        string outcome = c.Phase == "crying2" ? "dark" : Encounters.CryingOutcome(_rng.Next(100));
                        if (outcome == "ambush" && !Spawner.Ready(c.ManModel))
                            outcome = "knife";   // the men weren't ready: she manages on her own
                        CryingOutcome(c, outcome, now);
                    }
                    break;
                }
                case "knife":
                    if (now >= c.PhaseUntil)
                    {
                        // the lunge is over: she runs for the trees. Out of the fight first, or the combat AI pulls her
                        // straight back in; shooting her as she runs is still self-defence (_cameAtYouSeen)
                        PED.SET_PED_COMBAT_ATTRIBUTES(h, 5, false);
                        PED.SET_PED_COMBAT_ATTRIBUTES(h, 46, false);
                        PED.SET_PED_RELATIONSHIP_GROUP_HASH(h, c.OwnGroup);
                        PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, true);
                        TASK.CLEAR_PED_TASKS(h, true, false);
                        TASK.TASK_SMART_FLEE_PED(h, me.Handle, 300f, -1, 0, 3f, 0);
                        c.Phase = "fled";
                        c.PhaseUntil = now + 30000;
                    }
                    break;
                case "fled":
                    if ((!seen && dist > 35f) || now >= c.PhaseUntil)
                    {
                        if (c.Moved || seen)
                        {
                            EndCrying(c.Moved ? "she ran off for good" : "she ran off");   // never moved in front of him
                            return;
                        }
                        Vector3 at;
                        if (!EncounterSpot(me, 50f, 80f, out at))
                        {
                            EndCrying("she ran off");
                            return;
                        }
                        // somewhere else in the dark, as if nothing happened
                        TASK.CLEAR_PED_TASKS_IMMEDIATELY(h, true, true);
                        WEAPON.REMOVE_ALL_PED_WEAPONS(h, true, true);
                        ENTITY.SET_ENTITY_COORDS(h, at.X, at.Y, at.Z, false, false, false, false);
                        PED.SET_PED_RELATIONSHIP_GROUP_HASH(h, c.OwnGroup);
                        PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, true);
                        TASK.TASK_START_SCENARIO_IN_PLACE_HASH(h, Natives.Hash(Kneel), -1, true, 0u, -1f, false);
                        c.Moved = true;
                        c.Phase = "crying2";
                        c.NextCry = now + 4000;
                        Log.Write("ENCOUNTER crying woman: crying again somewhere else, " + (int)at.DistanceTo(me.Position) + " m away");
                    }
                    break;
                case "ambush":
                    if (c.Men.All(m => m == null || !m.Exists() || m.IsDead))
                    {
                        EndCrying("the men are down");
                        return;
                    }
                    if (now - c.PhaseUntil > 4 * 60000)
                    {
                        EndCrying("the fight went on too long");
                        return;
                    }
                    if (h != 0 && !seen && !w.IsDead && w.Position.DistanceTo(me.Position) > 60f)
                    {
                        Spawner.Release(w, true);   // she got away
                        c.Woman = null;
                    }
                    break;
                case "dark":
                    if (now >= c.PhaseUntil)
                    {
                        Vector3 away = w.Position + (w.Position - me.Position).Normalized * 70f;
                        TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(h, away.X, away.Y, away.Z, Natives.Brisk, -1, 1f, 0, 0f);
                        c.Phase = "leaving";
                        c.PhaseUntil = now + 40000;
                    }
                    break;
                case "leaving":
                    if ((!seen && dist > 30f) || now >= c.PhaseUntil)
                    {
                        EndCrying("she walked into the dark");
                        return;
                    }
                    break;
            }
        }

        private void CryingSpawn(int now)
        {
            Crying c = _crying;
            Ped me = Game.Player.Character;
            if (now - c.StartedAt > 20000 || c.Tries > 8)
            {
                EndCrying("nowhere hidden to put her");
                return;
            }
            if (!Spawner.Ready(c.WomanModel))
                return;
            Vector3 at;
            if (!EncounterSpot(me, 30f, 55f, out at))   // near enough to hear
            {
                c.Tries++;
                return;
            }
            Ped w = Spawner.Spawn(c.WomanModel, at, _rng.Next(360));
            if (w == null)
            {
                c.Tries++;
                return;
            }
            int h = w.Handle;
            if (ENTITY.IS_ENTITY_IN_WATER(h))
            {
                Spawner.Release(w, true);
                c.Tries++;
                return;
            }
            c.Woman = w;
            _encounterPeds.Add(h);
            c.OwnGroup = PED.GET_PED_RELATIONSHIP_GROUP_HASH(h);
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, true);
            PED.SET_PED_KEEP_TASK(h, true);
            TASK.TASK_START_SCENARIO_IN_PLACE_HASH(h, Natives.Hash(Kneel), -1, true, 0u, -1f, false);
            c.Phase = "crying";
            c.NextCry = now + 1500;
            Log.Write("ENCOUNTER crying woman: sobbing " + (int)at.DistanceTo(me.Position) + " m away");
        }

        private void CryingOutcome(Crying c, string outcome, int now)
        {
            Ped me = Game.Player.Character;
            int h = c.Woman.Handle;
            TASK.CLEAR_PED_TASKS_IMMEDIATELY(h, true, true);
            switch (outcome)
            {
                case "knife":
                    MakeHostile(c.Woman, now);
                    c.Woman.GiveWeapon(eWeapon.MeleeKnife, 1, eWeaponAttachPoint.Knife, true);
                    TASK.TASK_COMBAT_PED(h, me.Handle, 0, 16);
                    Feed.Say("The crying woman", Encounters.Pick(Encounters.KnifeTurn, h + "k"), Speech.When(h, "mock", me.Handle), toYou: true);
                    c.Phase = "knife";
                    c.PhaseUntil = now + 4000;
                    break;
                case "ambush":
                {
                    int n = Encounters.AmbushMen(_rng.Next(100));
                    for (int i = 0; i < n; i++)
                    {
                        Vector3 at;
                        if (!EncounterSpot(me, 14f, 26f, out at))
                            continue;
                        Ped m = Spawner.Spawn(c.ManModel, at, 0f);
                        if (m == null)
                            continue;
                        m.GiveWeapon(eWeapon.RevolverCattleman, 60, eWeaponAttachPoint.PistolR, true);
                        MakeHostile(m, now);
                        TASK.TASK_COMBAT_PED(m.Handle, me.Handle, 0, 16);
                        c.Men.Add(m);
                        _encounterPeds.Add(m.Handle);
                    }
                    if (c.Men.Count == 0)
                    {
                        CryingOutcome(c, "knife", now);   // nowhere for them to come from: she manages on her own
                        return;
                    }
                    Feed.Say("The crying woman", Encounters.Pick(Encounters.AmbushCall, h + "a"), Speech.When(h, "mock", me.Handle), toYou: true);
                    _cameAtYouSeen[h] = now;   // she brought them: shooting her as she runs is self-defence too
                    PED.SET_PED_KEEP_TASK(h, true);
                    TASK.TASK_SMART_FLEE_PED(h, me.Handle, 300f, -1, 0, 3f, 0);
                    c.Phase = "ambush";
                    c.PhaseUntil = now;
                    break;
                }
                default:
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, me.Handle, 3000, 0f, 0f, 0f);
                    TASK.TASK_LOOK_AT_ENTITY(h, me.Handle, 6000, 0, 51, 0);
                    Feed.Say("The crying woman", Encounters.Pick(Encounters.IntoTheDark, h + "d"), Speech.When(h, "hum", me.Handle), toYou: true);   // no words of the game's would fit
                    c.Phase = "dark";
                    c.PhaseUntil = now + 3500;
                    outcome = "dark";
                    break;
            }
            Log.Write("ENCOUNTER crying woman: " + (outcome == "ambush" ? "ambush (" + c.Men.Count + " men)" : outcome == "dark" ? "walks into the dark" : "knife"));
        }

        /// Hostile to the player from the start: fighting them off is self-defence, at once.
        private unsafe void MakeHostile(Ped p, int now)
        {
            int h = p.Handle;
            int me = Game.Player.Character.Handle;
            if (_hostileGroup == 0)
            {
                ulong slot = 0;   // out-parameters take 8 bytes
                PED.ADD_RELATIONSHIP_GROUP("REALNPCS_HOSTILE", (uint*)&slot);
                _hostileGroup = (uint)slot;
            }
            uint playerGroup = PED.GET_PED_RELATIONSHIP_GROUP_HASH(me);
            PED.SET_RELATIONSHIP_BETWEEN_GROUPS(5, _hostileGroup, playerGroup);
            PED.SET_RELATIONSHIP_BETWEEN_GROUPS(5, playerGroup, _hostileGroup);
            PED.SET_PED_RELATIONSHIP_GROUP_HASH(h, _hostileGroup);
            PED.SET_PED_COMBAT_ATTRIBUTES(h, 5, true);    // fights even against the armed
            PED.SET_PED_COMBAT_ATTRIBUTES(h, 46, true);   // always fights
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, false);
            _cameAtYouSeen[h] = now;
        }

        private void EndCrying(string why)
        {
            Crying c = _crying;
            _crying = null;
            if (c == null)
                return;
            if (c.Woman != null && c.Woman.Exists() && c.Phase != "spawn")
                Safe("world events", () => RecordEvent("encounter:crying", null, "a woman crying in the dark", "player", "you", c.Woman.Position, c.Woman.Handle, c.Woman.Handle, 0, why));
            Ped me = Game.Player.Character;
            foreach (Ped p in new[] { c.Woman }.Concat(c.Men))
            {
                if (p == null)
                    continue;
                // the living vanish only where nobody sees it; the dead stay where they fell
                bool alive = p.Exists() && !p.IsDead;
                bool seen = alive && ENTITY.IS_ENTITY_ON_SCREEN(p.Handle);
                Spawner.Release(p, alive && (!seen || p.Position.DistanceTo(me.Position) > 120f));
            }
            if (c.Phase != "spawn" && !c.Test)
                MarkDone("crying");   // nowhere to put her doesn't use up the night, nor does a test
            Log.Write("ENCOUNTER crying woman: over (" + why + ")");
        }

        /// A spot behind the camera between min and max metres from the player, on open ground the camera can't
        /// see right now: somewhere for someone to be without anyone seeing them arrive.
        private bool EncounterSpot(Ped player, float min, float max, out Vector3 at)
        {
            Vector3 cam = CAM.GET_FINAL_RENDERED_CAM_COORD();
            for (int tries = 0; tries < 8; tries++)
            {
                float d = min + (float)_rng.NextDouble() * (max - min);
                Vector3 c = Natives.BehindCamera(player.Position, d, 100f, _rng);
                Vector3 safe;
                if (!Natives.StandSpot(c, player.Position.Z, out safe))
                    continue;
                if (!Natives.Blocked(cam, safe + new Vector3(0f, 0f, 1.2f), player.Handle) && Natives.Looking(safe + new Vector3(0f, 0f, 1f), 50f))
                    continue;
                at = safe;
                return true;
            }
            at = Vector3.Zero;
            return false;
        }

        /// Ends whatever encounter is running (a mission started, the add-on is stopping).
        private void EndEncounters(string why)
        {
            if (_crying != null)
                EndCrying(why);
            if (_hermit != null)
                EndHermit("left (" + why + ")");
            if (_gathering != null)
                EndGathering(why);
            if (_exec != null)
                EndExecution(why);
            if (_visitor != null)
                EndVisitor(why);
            if (_traveller != null)
                EndTraveller(why);
            if (_campfire != null)
                EndCampfire(why);
            if (_supper != null || _supperDarkSince != 0)
                EndSupper(why);   // the dark lifted and the controls given back, if it was down
            VisitorLeftClear();   // a mission or shutdown: what it left goes with it
        }

        /// One line for the F7 dump.
        private string EncountersStatus()
        {
            if (!_cfg.Encounters)
                return "off";
            int now = Game.GameTime;
            List<string> on = new List<string>();
            if (_crying != null) on.Add("the crying woman (" + _crying.Phase + ")");
            if (_exec != null) on.Add("an execution (" + _exec.Phase + ")");
            if (_hermit != null) on.Add("a hermit (" + _hermit.Phase + (HermitWithYou() ? ", with you" : ", at his fire") + ")");
            if (_gathering != null) on.Add("a gathering (" + _gathering.Phase + ")");
            if (_visitor != null) on.Add("the night visitor");
            if (_traveller != null) on.Add("a traveller");
            if (_campfire != null) on.Add("a campfire (" + _campfire.Phase + ")");
            if (_supper != null) on.Add("the supper invitation (" + _supper.Phase + ")");
            if (_stalker != null) on.Add("a stalker");
            string live = (on.Count > 0 ? string.Join(", ", on) : "none") + " (" + EncountersLive + " counted, at most " + _cfg.EncountersAtOnce + ")";
            return "live: " + live + ", next roll in " + Math.Max(0, (_nextEncounterRoll - now) / 1000) + " s every " + _cfg.EncounterEveryMinutes + " min" +
                   ", crying woman " + (_cfg.CryingWoman ? _cfg.CryingWomanChance + "% (10 pm - 4 am, the wilds)" + (DoneToday("crying") ? ", done today" : "") : "off") +
                   ", executions " + ExecutionsStatus() +
                   ", hermits " + HermitsStatus() +
                   ", gatherings " + GatheringsStatus() +
                   ", supper " + SupperStatus() +
                   ", campfires " + CampfireStatus() +
                   (_encounterWait != null ? ", last roll skipped: " + _encounterWait : "");
        }
    }
}
