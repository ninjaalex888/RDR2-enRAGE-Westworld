// The supper invitation, the player version ([Encounters] SupperInvitation; the words are Core/Supper.cs; Lex's OK
// 2026-10-27 18:33). At dusk (17:30-21:00), out of the towns, with you on a road, a man on foot stands further along the road
// where you can't see him arrive. Within 30 m he waves and asks you to supper (subtitled); a moment later his thought
// over his head gives him away (so never with the ticker off). Stop by him on foot 3 s, or greet him (R): he leads you
// off the road toward "the house" (35 m to one side). Follow him on foot, 22 m from the road at his side or to the house,
// and the screen goes dark with your controls off: the host goes, the clock goes on to first light, you're set down
// 40-80 m off in a ditch, and last of all most of your cash is taken (Supper.Take, $250 at most). Ride on, a gun in
// your hand near him or on him, an antagonize, or let him walk off alone: he lets you go. Never in a mission, never
// with a lawman near, once a day; the dark never lasts past its watchdog (12 s), a death or a load in the dark takes
// nothing, and a test takes no money and leaves the clock and the save alone. In the encounter framework.
using System;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private sealed class SupperRun
        {
            public Ped Host;
            public uint Model;
            public string Phase = "spawn", Seed;   // spawn, waiting, invited, leading, dark, woke
            public Vector3 Home, Road;             // where he takes you (well off the track), and where he stood
            public int StartedAt, PhaseAt, StoppedSince, Tries, Taken, ThoughtAt;
            public bool Test, Thought, Woken, Robbed;   // Robbed: the money step ran (a test too), so the waking may be told
        }

        private SupperRun _supper;
        private int _suppers, _supperTaken, _supperDarkSince;
        private string _supperLast;

        private string SupperNotNow(bool test = false)
        {
            Ped me = Game.Player.Character;
            if (_bridge.TownOnly(me.Position) != null)
                return "you're in a town (he lives out on the roads)";
            if (!_cfg.Ticker)
                return "the ticker is off (his thought is what gives him away)";
            int clock = CLOCK.GET_CLOCK_HOURS(), minute = CLOCK.GET_CLOCK_MINUTES();
            if (!test && !Supper.Hour(clock, minute))
                return "it's " + clock.ToString("00") + ":" + minute.ToString("00") + " (he asks at dusk, half past five to nine)";
            if (!test && DoneToday("supper"))
                return "one asked you to supper already today";
            if (!test && LawmanNear(me, 60f))
                return "a lawman is near";
            Vector3 p = me.Position;
            if (!test && !PATHFIND.IS_POINT_ON_ROAD(p.X, p.Y, p.Z, 0))
                return "you're not on a road";
            return null;
        }

        private static bool LawmanNear(Ped me, float metres)
        {
            Vector3 at = me.Position;
            return World.GetAllPeds().Any(p => p != null && p.Exists() && !p.IsDead && p.Position.DistanceTo(at) < metres && Natives.IsLaw(p));
        }

        private void StartSupper(int now, bool test = false)
        {
            _supper = new SupperRun { StartedAt = now, PhaseAt = now, Test = test, Model = Spawner.ModelFor(null, false, _rng.Next()), Seed = "sp" + now };
            Log.Write("SUPPER begins: a host on the road at dusk" + (test ? " (a test: no money, no clock, nothing saved)" : ""));
        }

        private void SupperStep(int now)
        {
            SupperRun s = _supper;
            Ped me = Game.Player.Character;
            if (s.Phase == "dark" || s.Phase == "woke")
                return;   // stepped from SupperInTheDark: InWorld() is false while the screen is dark
            if (me == null || !me.Exists() || me.IsDead)
            {
                EndSupper("you died");
                return;
            }
            if (s.Phase == "spawn")
            {
                SupperSpawn(s, me, now);
                return;
            }
            if (s.Host == null || !s.Host.Exists() || s.Host.IsDead || PED.IS_PED_IN_COMBAT(s.Host.Handle, 0))
            {
                EndSupper(s.Host != null && s.Host.Exists() && s.Host.IsDead ? "he died" : "he was gone");
                return;
            }
            int h = s.Host.Handle;
            float d = s.Host.Position.DistanceTo(me.Position);
            bool onFoot = !PED.IS_PED_ON_MOUNT(me.Handle) && !PED.IS_PED_IN_ANY_VEHICLE(me.Handle, false);
            // aimed at him (aiming, never the lock-on alone: that is how you greet him), hurting him, or armed close by
            // close by once he's asked
            int aimedAt;
            bool aiming = PLAYER.IS_PLAYER_FREE_AIMING(Game.Player.Handle);
            if (aiming && (PLAYER.IS_PLAYER_FREE_AIMING_AT_ENTITY(Game.Player.Handle, h) || _aimedAt.TryGetValue(h, out aimedAt) && now - aimedAt < 1500) ||
                ENTITY.HAS_ENTITY_BEEN_DAMAGED_BY_ENTITY(h, me.Handle, true, false))
            {
                SupperLetGo(s, "a gun pointed his way");
                return;
            }
            if (s.Phase != "waiting" && d < 15f && WEAPON.IS_PED_ARMED(me.Handle, 4))
            {
                SupperLetGo(s, "armed close by");
                return;
            }
            if (!s.Thought && s.ThoughtAt != 0 && now >= s.ThoughtAt && d < 30f)
            {
                s.Thought = true;
                _ticker.Push("A stranger, thinking", Supper.HostThought(s.Seed), "near", h);   // the gap between his smile and his mind
            }
            switch (s.Phase)
            {
                case "waiting":
                    if (d < 30f && NodInView(me, s.Host))
                    {
                        TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, me.Handle, 2000, 0f, 0f, 0f);
                        TASK.TASK_LOOK_AT_ENTITY(h, me.Handle, -1, 0, 51, 0);
                        TravellerGesture(h, "happy_wave_fl_001");
                        Feed.Say("A stranger", Supper.Invite(s.Seed), Speech.When(h, "greet", me.Handle), h, toYou: true);
                        s.Phase = "invited";
                        s.PhaseAt = now;
                        s.ThoughtAt = now + 2500;   // his thought shows a moment after his smile
                        Log.Write("SUPPER: he asks you to supper (" + (int)d + " m)");
                    }
                    else if (now - s.StartedAt > 4 * 60000)
                    {
                        if (s.Host != null && s.Host.Exists() && !s.Host.IsDead)
                            TASK.TASK_WANDER_STANDARD(s.Host.Handle, 10f, 10);   // else he stood rooted for the rest of his stand-still (the night review)
                        EndSupper("you never came his way");
                    }
                    return;
                case "invited":
                    if (d < 4f && onFoot && ENTITY.GET_ENTITY_SPEED(me.Handle) < 0.4f && s.Thought)
                    {
                        if (s.StoppedSince == 0)
                            s.StoppedSince = now;
                        else if (now - s.StoppedSince > 3000)
                            SupperAccepted(s, now, "you stopped by him");
                    }
                    else
                        s.StoppedSince = 0;
                    if (s.Phase == "invited" && (d > 40f || now - s.PhaseAt > 60000))
                        SupperLetGo(s, d > 40f ? "you went on" : "you never answered");
                    return;
                case "leading":
                    bool atHouse = me.Position.DistanceTo(s.Home) < 8f && s.Host.Position.DistanceTo(s.Home) < 6f;
                    bool offRoad = me.Position.DistanceTo(s.Road) > 22f && d < 5f;   // well off the road at his side: the dark comes there
                    if (onFoot && (atHouse || offRoad))
                    {
                        SupperDark(s, now);
                        return;
                    }
                    if (d > 30f || !onFoot && d > 10f || now - s.PhaseAt > 60000)
                        SupperLetGo(s, d > 30f ? "you didn't follow" : !onFoot ? "you stayed in the saddle" : "you lingered");
                    return;
            }
        }

        /// A greeting (R) to the host is a yes to supper (WestworldScript.Life.cs GreetedSpecial); an antagonize (F), a no.
        private bool SupperGreeted(int handle, bool friendly)
        {
            SupperRun s = _supper;
            if (s == null || s.Host == null || s.Host.Handle != handle || s.Phase != "invited" && s.Phase != "waiting" && s.Phase != "leading")
                return false;
            if (!friendly)
            {
                SupperLetGo(s, "you antagonized him");
                return true;
            }
            if (s.Phase != "invited")
                return true;   // not asked yet, or leading already
            if (PED.IS_PED_ON_MOUNT(Game.Player.Character.Handle))
                return true;   // he heard you; he'll lead once you're on your feet (stop by him)
            if (!s.Thought)
            {
                s.Thought = true;   // his thought shows before he leads, whatever the timing
                _ticker.Push("A stranger, thinking", Supper.HostThought(s.Seed), "near", handle);
            }
            SupperAccepted(s, Game.GameTime, "you greeted him");
            return true;
        }

        private void SupperAccepted(SupperRun s, int now, string how)
        {
            int h = s.Host.Handle;
            Feed.Say("A stranger", Supper.Lead(s.Seed), Speech.When(h, "greet", Game.Player.Character.Handle), h, toYou: true);
            TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(h, s.Home.X, s.Home.Y, s.Home.Z, 1f, -1, 0.5f, 0, 0f);
            s.Phase = "leading";
            s.PhaseAt = now;
            Log.Write("SUPPER: " + how + "; he leads you off the road (" + (int)s.Host.Position.DistanceTo(s.Home) + " m)");
        }

        private void SupperLetGo(SupperRun s, string why)
        {
            Ped me = Game.Player.Character;
            if (s.Host != null && s.Host.Exists() && !s.Host.IsDead)
            {
                int h = s.Host.Handle;
                bool near = me != null && me.Exists() && s.Host.Position.DistanceTo(me.Position) < 20f;
                if (why == "a gun pointed his way" || why == "armed close by")
                {
                    TASK.TASK_HANDS_UP(h, 3000, me.Handle, -1, false);   // hands up, then off
                    if (near)
                        Feed.Say("A stranger", Supper.Backs(s.Seed), Speech.When(h, "scared", me.Handle), h, toYou: true);
                }
                else
                {
                    if (near && (s.Phase == "invited" || s.Phase == "leading"))
                        Feed.Say("A stranger", Supper.LetGo(s.Seed), Speech.When(h, "mild", me.Handle), h, toYou: true);
                    TASK.TASK_WANDER_STANDARD(h, 10f, 10);
                }
            }
            Log.Write("SUPPER: he lets you go (" + why + ")");
            EndSupper(why);
        }

        /// Into the dark: controls off, the screen faded; the rest is stepped from SupperInTheDark.
        private void SupperDark(SupperRun s, int now)
        {
            if (MISC.GET_MISSION_FLAG() || PED.IS_PED_IN_COMBAT(Game.Player.Character.Handle, 0))
            {
                SupperLetGo(s, "something came between");
                return;
            }
            PLAYER.SET_PLAYER_CONTROL(Game.Player.Handle, false, 0, false);
            CAM.DO_SCREEN_FADE_OUT(1500);
            _supperDarkSince = now;
            s.Phase = "dark";
            s.PhaseAt = now;
            Log.Write("SUPPER: you go along with him; supper, a drink, and the dark");
        }

        /// InWorld() is false while the screen is faded, so the dark is stepped from here (as the trysts' is): from Step's
        /// !InWorld branch and from Tick04 before its control check. In order, so the money is the last thing done and a
        /// fault before it takes nothing: the day marked done, the host gone, the clock on, you moved, then the money.
        private void SupperInTheDark()
        {
            if (_supper == null && _supperDarkSince == 0)
                return;
            SupperRun s = _supper;
            bool dark = s != null && (s.Phase == "dark" || s.Phase == "woke");
            if (Game.IsLoading)
            {
                if (dark || _supperDarkSince != 0)
                {
                    // a save loading: whatever was under way belongs to the world being left, so nothing more is done
                    // (the load brings its own light and controls)
                    Log.Write("SUPPER: a save is loading in the dark; dropped" + (s != null && s.Phase == "dark" ? ", nothing taken" : ""));
                    _supper = null;
                    _supperDarkSince = 0;
                }
                return;
            }
            if (Game.IsPaused)
                return;
            int now = Game.GameTime;
            Ped me = Game.Player.Character;
            if (dark && (me == null || !me.Exists() || me.IsDead))
            {
                EndSupper("you died in the dark" + (s.Phase == "dark" ? "; nothing taken" : ""));
                return;
            }
            if (_supperDarkSince != 0 && now - _supperDarkSince > 12000)
            {
                SupperLight("the watchdog: the dark lasted too long");
                return;
            }
            if (s == null)
                return;
            try
            {
                if (s.Phase == "dark" && (CAM.IS_SCREEN_FADED_OUT() || now - s.PhaseAt > 4000))
                {
                    s.Phase = "woke";   // first, so whatever follows can never run twice
                    s.PhaseAt = now;
                    if (!s.Test)
                        MarkDone("supper");   // before anything else: no second host this day, whatever happens next
                    if (s.Host != null && s.Host.Exists())
                    {
                        _encounterPeds.Remove(s.Host.Handle);
                        Spawner.Release(s.Host, true);   // spawned for this: gone, with the house nobody can find
                        s.Host = null;
                    }
                    if (!s.Test)
                        CLOCK.ADD_TO_CLOCK_TIME(Supper.HoursToDawn(CLOCK.GET_CLOCK_HOURS()), 0, 0);
                    Vector3 ditch;
                    if (SupperDitch(s.Home, out ditch))
                        ENTITY.SET_ENTITY_COORDS(me.Handle, ditch.X, ditch.Y, ditch.Z, false, false, false, false);
                    // the money, last
                    int cash = MONEY._MONEY_GET_CASH_BALANCE(), would = Supper.Take(cash, s.Seed);
                    Log.Write("SUPPER: the cash read " + cash + " (as cents, " + Cents(cash) + "); " + (s.Test ? "a test, so nothing taken (a real one would take " + would + ", " + Cents(would) + ")" : "taking " + would + " (" + Cents(would) + ")"));
                    if (!s.Test && would > 0)
                    {
                        s.Taken = would;
                        MONEY._MONEY_DECREMENT_CASH_BALANCE(would);
                        int after = MONEY._MONEY_GET_CASH_BALANCE();
                        if (cash - after != would)
                            Log.Write("SUPPER: CHECK THE UNITS: the cash went from " + cash + " to " + after + ", not down by " + would);
                    }
                    s.Robbed = true;
                    Log.Write("SUPPER: dark; " + Cents(s.Taken) + " taken" + (s.Test ? " (a test: nothing, the clock left alone)" : ", the clock on to first light") + ", you in a ditch" + (ditch == Vector3.Zero ? " (no dry spot near: where you stood)" : ""));
                }
                else if (s.Phase == "woke" && now - s.PhaseAt > 2500)
                {
                    if (ENTITY.IS_ENTITY_IN_WATER(me.Handle))
                        ENTITY.SET_ENTITY_COORDS(me.Handle, s.Home.X, s.Home.Y, s.Home.Z, false, false, false, false);   // never left in a creek
                    else
                        PED.SET_PED_TO_RAGDOLL(me.Handle, 1000, 1500, 0, false, false, "");   // coming round where you lay
                    EndSupper("came round at dawn");
                }
            }
            catch (Exception ex)
            {
                SupperLight("a fault: " + ex.Message);
            }
        }

        private static string Cents(int c)
        {
            return "$" + c / 100 + "." + (Math.Abs(c) % 100).ToString("00");
        }

        /// Light again and the controls back, whatever happened.
        private void SupperLight(string why)
        {
            try
            {
                CAM.DO_SCREEN_FADE_IN(1500);
                PLAYER.SET_PLAYER_CONTROL(Game.Player.Handle, true, 0, false);
            }
            catch
            {
            }
            _supperDarkSince = 0;
            if (why != null)
            {
                Log.Write("SUPPER: brought back (" + why + ")");
                EndSupper(why);
            }
        }

        /// The waking: what happened, told whatever ended the dark once it had happened (the waking step, the watchdog,
        /// a fault, shutdown).
        private void SupperWake(SupperRun s)
        {
            s.Woken = true;
            string line = Supper.Waking(Supper.Dollars(s.Taken), s.Seed);
            Hint(line, 10000);
            if (_cfg.Ticker)
                Feed.Notice("First light", line);   // the hint is one slot; this one stays in the feed
            _suppers++;
            _supperTaken += s.Taken;
            _supperLast = Cents(s.Taken) + " taken" + (s.Test ? " (a test)" : "");
            if (!s.Test)
            {
                Vector3 woke = Game.Player.Character.Position;
                string outcome = s.Taken > 0 ? "robbed" : "nothing to take";
                Safe("world events", () => RecordEvent("encounter:supper", null, "a man who asked you to supper", "player", "you", woke, 0, 0, 0, outcome));
            }
        }

        /// Somewhere 40-80 m from his "house": on the ground within 4 m of its height, level (under 0.9 m across 2 m),
        /// nothing overhead, dry.
        private unsafe bool SupperDitch(Vector3 home, out Vector3 at)
        {
            for (int i = 0; i < 12; i++)
            {
                double a = _rng.Next(360) * Math.PI / 180.0;
                float r = 40f + _rng.Next(40);
                Vector3 want = home + new Vector3((float)Math.Cos(a) * r, (float)Math.Sin(a) * r, 0f);
                if (!Natives.StandSpot(want, home.Z, out at) || Math.Abs(at.Z - home.Z) > 4f || !SupperDry(at))
                    continue;
                float lo = at.Z, hi = at.Z;
                bool level = true;
                foreach (Vector3 o in new[] { new Vector3(1f, 0f, 0f), new Vector3(-1f, 0f, 0f), new Vector3(0f, 1f, 0f), new Vector3(0f, -1f, 0f) })
                {
                    Vector3 g;
                    if (!Natives.StandSpot(at + o, at.Z, out g))
                    {
                        level = false;
                        break;
                    }
                    lo = Math.Min(lo, g.Z);
                    hi = Math.Max(hi, g.Z);
                }
                if (!level || hi - lo > 0.9f)
                    continue;
                if (Natives.Blocked(at + new Vector3(0f, 0f, 0.6f), at + new Vector3(0f, 0f, 4f), Game.Player.Character.Handle))
                    continue;   // a rock, a roof, a tree over it
                return true;
            }
            at = Vector3.Zero;
            return false;
        }

        private static unsafe bool SupperDry(Vector3 at)
        {
            long slot = 0;   // an 8-byte slot for the out-parameter
            return !(WATER.GET_WATER_HEIGHT(at.X, at.Y, at.Z + 2f, (float*)&slot) && *(float*)&slot > at.Z - 0.3f);
        }

        private void SupperSpawn(SupperRun s, Ped me, int now)
        {
            if (now - s.StartedAt > 20000 || s.Tries > 10)
            {
                EndSupper("nowhere on the road to put him");
                return;
            }
            if (!Spawner.Ready(s.Model))
                return;
            Vector3 at;
            if (!TravellerSpot(me, out at) && !EncounterSpot(me, 50f, 80f, out at))
            {
                s.Tries++;
                return;
            }
            // "the house": 35 m off the road, to one side, on dry ground
            double hd = ENTITY.GET_ENTITY_HEADING(me.Handle) * Math.PI / 180.0;
            float k = Voice.Hash(s.Seed) % 2 == 0 ? 35f : -35f;
            Vector3 home;
            if (!Natives.StandSpot(at + new Vector3((float)Math.Cos(hd) * k, (float)Math.Sin(hd) * k, 0f), at.Z, out home) || !SupperDry(home))
            {
                s.Tries++;
                return;
            }
            Ped m = Spawner.Spawn(s.Model, at, _rng.Next(360));
            if (m == null || ENTITY.IS_ENTITY_IN_WATER(m.Handle))
            {
                if (m != null)
                    Spawner.Release(m, true);
                s.Tries++;
                return;
            }
            int h = m.Handle;
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, true);
            PED.SET_PED_KEEP_TASK(h, true);
            WEAPON.REMOVE_ALL_PED_WEAPONS(h, true, true);
            TASK.TASK_STAND_STILL(h, 240000);
            s.Host = m;
            s.Home = home;
            s.Road = at;
            s.Phase = "waiting";
            s.PhaseAt = now;
            _encounterPeds.Add(h);
            Log.Write("SUPPER: he stands by the road " + (int)at.DistanceTo(me.Position) + " m off");
        }

        /// Over: the dark lifted if it was down, the waking told if it had happened, the host let go (spawned for this,
        /// so deleted unseen; seen, his orders and our hold dropped and he's the game's).
        private void EndSupper(string why)
        {
            SupperRun s = _supper;
            _supper = null;
            if (_supperDarkSince != 0)
                SupperLight(null);
            if (s == null)
                return;
            if (s.Phase == "woke" && !s.Woken && !s.Robbed)
                Log.Write("SUPPER: the dark ended before the robbery (nothing taken, nothing told)");   // a fault before the money step: no "pockets turned out" for a robbery that never was (the night review)
            if (s.Phase == "woke" && !s.Woken && s.Robbed)
            {
                try
                {
                    SupperWake(s);
                }
                catch (Exception ex)
                {
                    Log.Write("SUPPER: the waking failed: " + ex.Message);
                }
            }
            if (s.Host != null && s.Host.Exists())
            {
                int h = s.Host.Handle;
                _encounterPeds.Remove(h);
                if (s.Host.IsDead || !ENTITY.IS_ENTITY_ON_SCREEN(h))
                    Spawner.Release(s.Host, !s.Host.IsDead);
                else
                {
                    PED.SET_PED_KEEP_TASK(h, false);   // the wander or the hands up he was given runs out, and he's the game's
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, false);
                    Spawner.Release(s.Host, false);
                }
            }
            Log.Write("SUPPER over: " + why);
        }

        /// The add-on is switching itself off (too many errors): the light and the controls back, nothing more.
        private void SupperEmergencyLight()
        {
            if (_supperDarkSince == 0 && (_supper == null || _supper.Phase != "dark" && _supper.Phase != "woke"))
                return;
            try
            {
                CAM.DO_SCREEN_FADE_IN(500);
                PLAYER.SET_PLAYER_CONTROL(Game.Player.Handle, true, 0, false);
            }
            catch
            {
            }
            _supperDarkSince = 0;
        }

        private string SupperStatus()
        {
            return (_cfg.SupperInvitation ? "on, " + _cfg.SupperChance + "% a roll at dusk (17:30-21:00), out of the towns, once a day" : "off ([Encounters] SupperInvitation)") +
                   "; this session " + _suppers + ", " + Cents(_supperTaken) + " taken" + (_supper != null ? "; now: " + _supper.Phase : "") + (_supperLast != null ? "; last: " + _supperLast : "");
        }
    }
}
