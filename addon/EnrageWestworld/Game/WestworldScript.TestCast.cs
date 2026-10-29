// The test cast in the game (Lex, 2026-10-27, roadmap 32; the parts and their fitting in Core/TestCast.cs). The Testing
// page's "Bring the test cast here" makes the people the tests need out of your sight, 14-26 m behind you, and they
// stroll about that spot (TASK_WANDER_IN_AREA, 3.5 m: the preacher's listener must be within 8 m of him, the old men within 10 m of each other); "Let the test cast go" takes them away. They're kept in the save:
// the same people come each time (their saved build, outfit and identity), and one who died is replaced. Their bodies
// are mission entities, so the game keeps them while you're about, but nobody holds them (Spawner.SpawnCast): every
// feature may take them as it would a townsperson, by its own MayDirect checks, and once it has let them go and they've
// strayed 10 m they're walked back to their spot. Bodies go when you're 250 m off (unseen), at a mission start, a load,
// the wipe and shutdown; the identities stay. Not built: the cast made again by themselves when you come back to town.
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
        private sealed class TestCastBody
        {
            public TestCast.Part Part;
            public Resident R;
            public Ped Ped;
            public int RoamedAt;
        }

        private sealed class TestCastRun
        {
            public Vector3 Home;
            public string Town;
            public readonly List<TestCast.Part> Due = new List<TestCast.Part>();
            public readonly List<string> Failed = new List<string>();
            public int StartedAt, ModelSince, ModelTry, Made, Back, Here;
            public Resident Saved;          // the living one of the part now being made, looked up once
            public string SavedFor;
        }

        // old men's builds the game has (from the API's model list; the first that streams in is used, in this order)
        private static readonly string[] TestCastOldMen = { "a_m_o_sdupperclass_01", "a_m_o_blwupperclass_01", "a_m_o_lagtownfolk_01", "a_m_o_btchillbilly_01" };
        private const float TestCastRoam = 3.5f, TestCastStray = 10f, TestCastGone = 250f;

        private readonly Dictionary<string, TestCastBody> _testCast = new Dictionary<string, TestCastBody>();   // by part
        private TestCastRun _testCastRun;
        private Vector3 _testCastHome;
        private int _nextTestCastLook, _testCastBrought;
        private string _testCastLast;
        private Action _aTestCast;

        private void TestCastStep(int now)
        {
            if (_testCastRun == null && _testCast.Count == 0)
            {
                TestCastReturnStep(now);   // the cast kept in the save come back by themselves in their town
                return;
            }
            if (_aTestCast == null)
                _aTestCast = () => TestCastTick(_now);
            Safe("test cast", _aTestCast);
        }

        private int _testCastReturnLookAt, _testCastReturns;
        private string _testCastReturnTown;
        private Action _aTestCastReturn;

        /// Every 20 s with nobody of the cast about: in a town with living cast members in the save (made there by a
        /// bring), once per visit to that town, they're brought back out of sight the way the Testing row brings them
        /// (only those in the save: nobody is made anew). Roadmap 32's "made again by themselves when you come back".
        private void TestCastReturnStep(int now)
        {
            if (_cfg == null || !_cfg.TestCastReturns || _store == null)
                return;
            if (_placeTown == null)
            {
                _testCastReturnTown = null;   // leaving the town: next time counts as a new visit
                return;
            }
            if (_placeTown == _testCastReturnTown || (now - _testCastReturnLookAt < 20000 && now >= _testCastReturnLookAt))
                return;
            _testCastReturnLookAt = now;
            if (_aTestCastReturn == null)
                _aTestCastReturn = TestCastReturnImpl;
            Safe("test cast", _aTestCastReturn);
        }

        private void TestCastReturnImpl()
        {
            int now = _now;
            if (_interiorType != 0 || MISC.GET_MISSION_FLAG())
                return;
            string town = _placeTown;
            int living = _store.Residents.Count(x => x.Cast != null && !x.Dead && x.Town == town);
            if (living == 0)
            {
                _testCastReturnTown = town;   // nothing to bring here: not again this visit
                return;
            }
            Ped me = Game.Player.Character;
            string said;
            string why = TestCastBring(me, now, out said, true);
            if (why != null)
            {
                Log.Detail("TESTCAST not back by themselves in " + town + ": " + why);
                return;   // tried again in 20 s (nowhere hidden, for one)
            }
            _testCastReturnTown = town;
            _testCastReturns++;
            Log.Write("TESTCAST back by themselves in " + town + ": " + living + " of the cast live here");
        }

        /// The Testing row: the test cast brought near you (made, or brought back from the save). Null when it started.
        private string TestCastBring(Ped me, int now, out string said, bool onlySaved = false)
        {
            said = null;
            if (_testCastRun != null)
                return "the test cast is on its way already";
            if (_placeTown == null)
                return "there's no town here (they're townsfolk)";
            if (_interiorType != 0)
                return "you're indoors (they come to a street)";
            // out of sight behind you; a narrow street may have nowhere hidden (Lex was refused three times running,
            // 2026-10-28): further out, then in the open near you
            Vector3 home;
            bool inSight = false;
            // the row: 10 m in front of you (Lex, 2026-10-29: "spawn 10m in front of the main character now instead of behind
            // them"); coming back by themselves: out of sight, as before
            if (!onlySaved && TestCastFrontSpot(me, out home))
                inSight = true;
            else if (!EncounterSpot(me, 14f, 26f, out home) && !EncounterSpot(me, 8f, 40f, out home))
            {
                if (!TestCastOpenSpot(me, out home))
                    return "nowhere to stand them within 18 m of you";
                inSight = true;
            }
            var run = new TestCastRun { Home = home, Town = _placeTown, StartedAt = now, ModelSince = now };
            foreach (TestCast.Part part in TestCast.Parts)
            {
                TestCastBody b;
                if (_testCast.TryGetValue(part.Id, out b) && TestCastAlive(b) && b.Ped.Position.DistanceTo(me.Position) < 60f)
                {
                    run.Here++;
                    if (MayDirectWhyNot(b.Ped) == null && !NotFree(b.Ped.Handle))
                        TestCastRoamAt(b, home, now);   // already about: they walk over
                    continue;
                }
                if (b != null)
                    TestCastDrop(b, "brought again elsewhere");
                if (onlySaved && !_store.Residents.Any(x => x.Cast == part.Id && !x.Dead && x.Town == _placeTown))
                    continue;   // coming back by themselves: only those who live here, nobody made anew
                run.Due.Add(part);
            }
            if (run.Due.Count == 0 && run.Here == 0)
                return "nobody of the cast lives here";
            _testCastHome = home;
            _testCastRun = run;
            said = "The test cast is coming: " + TestCast.Parts.Length + " people, " + (int)home.DistanceTo(me.Position) + " m " + Compass(me.Position, home) + " of you.";
            Log.Write("TESTCAST bring: " + run.Due.Count + " to make or bring back from the save, " + run.Here + " here already; their spot " + (int)home.DistanceTo(me.Position) + " m " +
                      Compass(me.Position, home) + " of you in " + _placeTown + (inSight ? " (nowhere hidden from you: in sight)" : ""));
            return null;
        }

        /// A spot to stand on 10-18 m from you, any way round (never indoors: StandSpot), for when nowhere is hidden.
        /// Ground 10 m in front of you, or a little to either side when that's a wall or a drop.
        private bool TestCastFrontSpot(Ped me, out Vector3 at)
        {
            Vector3 f = me.ForwardVector;
            double heading = Math.Atan2(f.Y, f.X);
            foreach (double off in new[] { 0.0, 0.35, -0.35, 0.7, -0.7, 1.05, -1.05 })
            {
                double a = heading + off;
                if (Natives.StandSpot(me.Position + new Vector3((float)Math.Cos(a) * 10f, (float)Math.Sin(a) * 10f, 0f), me.Position.Z, out at) && Math.Abs(at.Z - me.Position.Z) < 3f)
                    return true;
            }
            at = Vector3.Zero;
            return false;
        }

        private bool TestCastOpenSpot(Ped me, out Vector3 at)
        {
            for (int i = 0; i < 12; i++)
            {
                double a = _rng.NextDouble() * 2 * Math.PI;
                float d = 10f + (float)_rng.NextDouble() * 8f;
                if (Natives.StandSpot(me.Position + new Vector3((float)Math.Cos(a) * d, (float)Math.Sin(a) * d, 0f), me.Position.Z, out at) &&
                    Math.Abs(at.Z - me.Position.Z) < 3f)
                    return true;
            }
            at = Vector3.Zero;
            return false;
        }

        /// The Testing row: the test cast taken away (kept in the save).
        private string TestCastGo(out string said)
        {
            said = null;
            if (_testCastRun == null && _testCast.Count == 0)
                return "the test cast isn't here";
            TestCastBody busy = _testCast.Values.FirstOrDefault(b => TestCastAlive(b) && (NotFree(b.Ped.Handle) || Spawner.IsBorrowed(b.Ped.Handle)));
            if (busy != null)
                return busy.R.Name + " is in a scene or a test (let it finish)";
            int n = _testCast.Count;
            TestCastClear("let go from the Testing page", false);
            said = "The test cast is gone (" + n + "); they're kept in the save for next time.";
            return null;
        }

        /// Every body taken away, the identities kept: a mission start, a load, the wipe, shutdown (all of them, in sight
        /// or not: one left walking off after a reload looked like the cast and wasn't, Lex 2026-10-28), or the Testing row
        /// (those in sight walk off).
        private void TestCastClear(string why, bool vanish = true)
        {
            if (_testCastRun != null)
                Log.Write("TESTCAST bring stopped: " + why);
            _testCastRun = null;
            foreach (TestCastBody b in _testCast.Values.ToList())
                TestCastDrop(b, why, vanish);
        }

        /// For a test's refusal: the cast would bring who it lacked, when they're not about.
        private string TestCastHint(string who)
        {
            return _testCast.Values.Any(TestCastAlive) ? "" : "; the Testing page's \"Bring the test cast here\" brings " + who;
        }

        private static bool TestCastAlive(TestCastBody b)
        {
            return b.Ped != null && b.Ped.Exists() && !b.Ped.IsDead && Spawner.IsCast(b.Ped.Handle);
        }

        /// One body gone: deleted unseen; in sight, they walk off and the game takes them over; dead, they lie where
        /// they fell (the people ledger notes the death from the binding, which stays until it has).
        private void TestCastDrop(TestCastBody b, string why, bool vanish = false)
        {
            _testCast.Remove(b.Part.Id);
            Ped p = b.Ped;
            if (p == null || !p.Exists())
            {
                Spawner.ReleaseCast(p, false);
                return;
            }
            int h = p.Handle;
            bool dead = p.IsDead, seen = !vanish && ENTITY.IS_ENTITY_ON_SCREEN(h);
            ResBind rb;
            if (!dead && _resBind.TryGetValue(h, out rb) && rb.R == b.R)
                _resBind.Remove(h);   // the same person may be made again elsewhere: never two bodies with one name
            if (!dead && seen && Spawner.IsCast(h) && !Spawner.IsBorrowed(h))
                TASK.TASK_WANDER_STANDARD(h, 10f, 10);
            Spawner.ReleaseCast(p, !dead && !seen);
            Log.Write("TESTCAST " + b.R.Name + " (" + b.Part.Id + "): " + why + (dead ? ", died" : seen ? ", walks off" : ", taken away unseen"));
        }

        private void TestCastRoamAt(TestCastBody b, Vector3 home, int now)
        {
            TASK.TASK_WANDER_IN_AREA(b.Ped.Handle, home.X, home.Y, home.Z, TestCastRoam, 2f, 6f, 0);
            b.RoamedAt = now;
        }

        private void TestCastTick(int now)
        {
            Ped me = Game.Player.Character;
            if (_testCastRun != null)
                TestCastMake(_testCastRun, me, now);
            if (now < _nextTestCastLook)
                return;
            _nextTestCastLook = now + 2000;
            foreach (TestCastBody b in _testCast.Values.ToList())
            {
                if (b.Ped == null || !b.Ped.Exists())
                {
                    TestCastDrop(b, "gone from the world");
                    continue;
                }
                if (b.Ped.IsDead && KnockedOutWhy(b.Ped.Handle) == null)   // out cold isn't dead (the ledger's rule; 2026-10-29: the bully knocked out in a brawl was "died" here)
                {
                    TestCastDrop(b, "the next bring makes someone new for the part");
                    continue;
                }
                float d = b.Ped.Position.DistanceTo(me.Position);
                if (d > TestCastGone && !ENTITY.IS_ENTITY_ON_SCREEN(b.Ped.Handle))
                {
                    TestCastDrop(b, "you went on (" + (int)d + " m)");
                    continue;
                }
                // let go by a scene or a test and walked off: back to their spot once nobody has them (every 20 s at most)
                if (now - b.RoamedAt > 20000 && b.Ped.Position.DistanceTo(_testCastHome) > TestCastStray && MayDirectWhyNot(b.Ped) == null &&
                    !NotFree(b.Ped.Handle) && !PED.IS_PED_USING_ANY_SCENARIO(b.Ped.Handle))
                    TestCastRoamAt(b, _testCastHome, now);
            }
        }

        /// One member a step: their build streamed in, then made (a new resident fitted to the part) or brought back
        /// (the living one of the part in the save, in their own build and outfit), bound and set strolling.
        private void TestCastMake(TestCastRun run, Ped me, int now)
        {
            if (MISC.GET_MISSION_FLAG() || run.Due.Count == 0 || now - run.StartedAt > 60000)
            {
                TestCastDone(run, me);
                return;
            }
            TestCast.Part part = run.Due[0];
            if (run.SavedFor != part.Id)
            {
                run.SavedFor = part.Id;
                run.Saved = _store.Residents.Where(x => x.Cast == part.Id && !x.Dead).OrderByDescending(x => x.MetAt).FirstOrDefault();
            }
            Resident saved = run.Saved;
            bool back = saved != null && saved.Model != 0 && run.ModelTry < 2;   // a saved build that won't stream in: someone new
            uint model = back ? saved.Model
                       : part.Old ? Natives.Hash(TestCastOldMen[run.ModelTry % TestCastOldMen.Length])
                       : Spawner.ModelFor(run.Town, part.Woman, (int)Voice.Hash(part.Id + "|" + run.Town) + run.ModelTry);
            if (!Spawner.Ready(model))
            {
                if (now - run.ModelSince > 4000)
                {
                    run.ModelSince = now;
                    if (++run.ModelTry > 5)
                    {
                        run.Failed.Add(part.Id + " (no build of theirs streamed in)");
                        run.Due.RemoveAt(0);
                        run.ModelTry = 0;
                    }
                }
                return;
            }
            double a = Voice.Hash(part.Id + "|a") % 360 * Math.PI / 180.0;
            float r = 1.5f + Voice.Hash(part.Id + "|r") % 25 / 10f;   // 1.5-3.9 m from the spot, each part their own place
            Vector3 at;
            if (!Natives.StandSpot(run.Home + new Vector3((float)Math.Cos(a) * r, (float)Math.Sin(a) * r, 0f), run.Home.Z, out at))
                at = run.Home;
            Ped p = Spawner.SpawnCast(model, at, Voice.Hash(part.Id + "|h") % 360);
            if (p == null)
                return;   // a fade, or the game not ready: the next step
            int h = p.Handle;
            bool woman = !PED.IS_PED_MALE(h);
            long gameNow = Bridge.Now();
            Resident res = back ? saved : null;
            if (res == null)
            {
                string seed = "cast|" + part.Id + "|" + h + "|" + Game.GameTime;
                res = _engine.MeetResident(model, woman, run.Town, ResidentTrade(p, woman), seed, gameNow);
                if (saved != null && !saved.Dead && saved.Cast == part.Id)
                    saved.Cast = null;   // a living member whose build never streamed in: an ordinary resident now, not a second one of the part kept for good (the night review)
                int outfits = PED.GET_NUM_META_PED_OUTFITS(h);
                res.Outfit = outfits > 0 ? (int)(Voice.Hash(seed + "|o") % outfits) : 0;
                TestCast.Fit(res, part, Voice.NowYear);
            }
            PED._EQUIP_META_PED_OUTFIT_PRESET(h, res.Outfit, false);   // their own clothes, the same each time they come
            PED._UPDATE_PED_VARIATION(h, false, true, true, true, false);
            foreach (int old in _resBind.Where(kv => kv.Value.R == res).Select(kv => kv.Key).ToList())
                _resBind.Remove(old);   // a body of theirs the game still has somewhere: a passer-by now
            res.Town = run.Town;
            _engine.ForgetPerson(res.Id);   // Fit and the town change came after MeetResident may have cached their Person (the night review)
            _engine.KeepResident(res, gameNow);
            _resBind[h] = new ResBind { R = res, Model = model };
            var body = new TestCastBody { Part = part, R = res, Ped = p };
            _testCast[part.Id] = body;
            TestCastRoamAt(body, run.Home, now);
            if (back)
                run.Back++;
            else
                run.Made++;
            string fit = TestCast.WhyNotFit(res, part);
            Log.Write("TESTCAST " + (back ? "back" : "new") + ": " + res.Name + ", " + part.What + "; " + res.Trade + ", " + (Voice.NowYear - res.BornYear) + ", " + (res.Personality ?? "plain") +
                      ", " + res.Devotion + ", outfit " + res.Outfit + ", " + (int)at.DistanceTo(me.Position) + " m from you" + (fit != null ? "; DOESN'T FIT THE PART: " + fit : ""));
            run.Due.RemoveAt(0);
            run.ModelTry = 0;
            run.ModelSince = now;
        }

        private void TestCastDone(TestCastRun run, Ped me)
        {
            _testCastRun = null;
            _testCastBrought++;
            run.Failed.AddRange(run.Due.Select(x => x.Id + " (not made in time)"));
            _testCastLast = run.Made + " new, " + run.Back + " back from the save, " + run.Here + " here already" + (run.Failed.Count > 0 ? "; not made: " + string.Join(", ", run.Failed) : "");
            Log.Write("TESTCAST brought: " + _testCastLast);
            int until = Game.GameTime + 60000;
            foreach (TestCastBody b in _testCast.Values)
            {
                if (!TestCastAlive(b))
                    continue;
                Blip bl = b.Ped.AddBlip(BlipType.CompanionGray);
                bl.Label = b.R.Name;
                _regBlips.Add(new KeyValuePair<Blip, int>(bl, until));
            }
            Hint("The test cast is here: " + _testCast.Count + " people, " + (int)_testCastHome.DistanceTo(me.Position) + " m " + Compass(me.Position, _testCastHome) + " of you (on your map for a minute)" +
                 (run.Failed.Count > 0 ? "; " + run.Failed.Count + " couldn't be made (see the log)" : "") + ".", 8000);
        }

        /// F7: who's here, what each is free for, and whether enRAGE has taken any of them for one of its own.
        private string TestCastStatus()
        {
            Ped me = Game.Player.Character;
            int saved = _store != null ? _store.Residents.Count(x => x.Cast != null && !x.Dead) : 0, died = _store != null ? _store.Residents.Count(x => x.Cast != null && x.Dead) : 0;
            string here = string.Join("; ", _testCast.Values.Select(b =>
            {
                if (!TestCastAlive(b))
                    return b.R.Name + " (" + b.Part.Id + "): gone";
                int h = b.Ped.Handle;
                FolkRef ac = _bridge.AcquaintanceAt(h, true);
                string named = ac == null ? _bridge.EnrageNameAt(h, true) : null;
                string why = MayDirectWhyNot(b.Ped) ?? (NotFree(h) ? "in a scene or a test" : "free");
                return b.R.Name + " (" + b.Part.Id + ", " + (int)b.Ped.Position.DistanceTo(me.Position) + " m): " + why +
                       (ac != null ? "; enRAGE takes them for its " + ac.Name + " (RealNPCs keeps them as " + b.R.Name + ")"
                        : named != null ? "; enRAGE calls them " + named : _bridge.CanTellLooked && _bridge.Looked(h) ? "; enRAGE has looked at them" : "") +
                       (TestCast.WhyNotFit(b.R, b.Part) is string bad ? "; doesn't fit: " + bad : "");
            }));
            return "brought " + _testCastBrought + " time" + (_testCastBrought == 1 ? "" : "s") + " this session" + (_testCastLast != null ? " (last: " + _testCastLast + ")" : "") +
                   (_testCastRun != null ? "; on the way: " + _testCastRun.Due.Count + " to make" : "") + "; " + saved + " in the save" + (died > 0 ? ", " + died + " died" : "") + (_cfg != null && _cfg.TestCastReturns ? " (they come back by themselves in their town: " + _testCastReturns + " times this session)" : " (they stay away until brought: [Testing] TestCastReturns off)") +
                   "; here: " + (here.Length > 0 ? here : "nobody");
        }
    }
}
