// The Testing page (overnight 2026-10-27, Tier 1.2 and 1.3): every interaction and encounter in Core/TestActions.cs
// fired on demand, by its key ([Testing], with the TestKeys master) or by Enter on its row in the settings panel (the
// panel closes, and it fires a second later). A key or a row only asks; the fire happens in the tick, where the rails
// are checked and what was skipped is logged. A test may skip the time, place and chance rolls and the rests, never
// the safety rules: no story missions, no story characters or children, one driver at a time (each feature's own
// MayDirect checks), borrowed people handed back, spawned ones cleaned up by the feature's own end.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private readonly List<KeyValuePair<TestActions.Entry, KeySpec>> _testKeySpecs = new List<KeyValuePair<TestActions.Entry, KeySpec>>();
        private string _fireDue, _fireHow, _lastFire, _lastFireHow, _lastFireResult, _lastFireSeed;
        private int _fireDueAt, _fireWaitUntil, _lastFireAt, _fires, _courtTurn, _pleaArmedUntil;
        private Action _aTesting;

        /// The Testing keys, from the settings (ApplySettings).
        private void BuildTestKeys()
        {
            _testKeySpecs.Clear();
            foreach (TestActions.Entry a in TestActions.All)
            {
                KeySpec k = KeySpec.Parse(_cfg.TestKeyOf(a), Keys.None);
                if (k.Key != Keys.None)
                    _testKeySpecs.Add(new KeyValuePair<TestActions.Entry, KeySpec>(a, k));
            }
        }

        /// A Testing key: asks for its fire (the tick does it) and says it was one.
        private bool TestKeyHit(KeyEventArgs e)
        {
            if (_cfg == null || !_cfg.TestKeys)
                return false;
            for (int i = 0; i < _testKeySpecs.Count; i++)
                if (_testKeySpecs[i].Value.Matches(e))
                {
                    AskFire(_testKeySpecs[i].Key.Id, "key " + _testKeySpecs[i].Value, 0);
                    return true;
                }
            return false;
        }

        private bool IsTestingKey(KeyEventArgs e)
        {
            return _cfg != null && _cfg.TestKeys && _testKeySpecs.Any(k => k.Value.Matches(e));
        }

        /// Asks for a fire `delay` ms from now (the panel's: a second, once it has closed).
        private void AskFire(string id, string how, int delay)
        {
            if (_engine == null || _store == null)
            {
                TestRefuse(id, "RealNPCs hasn't finished loading (it waits for enRAGE)");
                return;   // never kept for later: it would go off minutes after, whenever loading ends
            }
            _fireDue = id;
            _fireHow = how;
            _fireDueAt = Game.GameTime + delay;
            _fireWaitUntil = Game.GameTime + delay + 10000;   // waits up to 10 s for the player to have control again
        }

        private void TestingStep(int now)
        {
            if (_aTesting == null)
                _aTesting = () => TestingTick(_now);
            Safe("testing", _aTesting);
        }

        private void TestingTick(int now)
        {
            if (_fireDue == null || now < _fireDueAt)
                return;
            string id = _fireDue, how = _fireHow;
            if (!MISC.GET_MISSION_FLAG() && !PLAYER.IS_PLAYER_CONTROL_ON(Game.Player.Handle) && now < _fireWaitUntil)
                return;   // the panel just closed, or a cutscene: a moment more
            if (now - _fireDueAt > 15000)
            {
                _fireDue = null;
                TestRefuse(id, "it waited too long for you to have control");
                return;
            }
            _fireDue = null;
            TestFire(id, how, now);
        }

        /// One fire: the rails first (refused, with why), then the feature's own start path, logged with what it skipped.
        private void TestFire(string id, string how, int now)
        {
            TestActions.Entry a = TestActions.Find(id);
            string seed = "test|" + id + "|" + now;
            if (a != null && a.Id == "replay")
            {
                if (_lastFire == null)
                {
                    TestRefuse(id, "nothing has been fired yet this session");
                    return;
                }
                a = TestActions.Find(_lastFire);
                how = "replay of " + _lastFireHow;
                seed = _lastFireSeed ?? seed;   // the same seed: the same words, where the same people are there
            }
            if (a == null)
            {
                TestRefuse(id, "no such test");
                return;
            }
            string rail = TestRail();
            if (rail != null)
            {
                TestRefuse(a.Id, rail);
                return;
            }
            string skipped, said;
            string refused = TestDispatch(a, how, now, seed, out skipped, out said);
            if (refused != null)
            {
                TestRefuse(a.Id, refused);
                return;
            }
            _fires++;
            _lastFire = a.Id;
            _lastFireHow = how.StartsWith("replay of ") ? _lastFireHow : how;
            _lastFireAt = now;
            _lastFireSeed = seed;
            _lastFireResult = "started";
            Log.Write("TEST fire " + a.Id + ": started (" + how + (skipped != null ? "; skipped: " + skipped : string.Empty) + ")");
            if (said != null)
                Hint(said, 5000);
        }

        private void TestRefuse(string id, string why)
        {
            _lastFireResult = "refused: " + why;
            Log.Write("TEST fire " + id + ": refused: " + why);
            TestActions.Entry a = TestActions.Find(id);
            Hint((a != null ? a.Label : id) + ": not now. " + Grammar.UpperFirst(why.TrimEnd('.')) + ".", 5000);
        }

        /// The rules no test skips; null when none stands in the way.
        private string TestRail()
        {
            if (_engine == null || _store == null)
                return "RealNPCs hasn't finished loading (it waits for enRAGE)";
            if (MISC.GET_MISSION_FLAG())
                return "a story mission";
            if (Game.Player.Character.IsDead)
                return "you're dead";
            bool control = PLAYER.IS_PLAYER_CONTROL_ON(Game.Player.Handle);
            if (!control)
                return "you have no control just now";
            return null;
        }

        /// The feature's own start path for each action; null when it was started (what the test skipped in `skipped`,
        /// what to tell the player in `said`), else why not.
        private string TestDispatch(TestActions.Entry a, string how, int now, string seed, out string skipped, out string said)
        {
            skipped = said = null;
            bool asked;
            if (a.Id.StartsWith("scene:"))
            {
                skipped = "the reason, the chance meeting's wait and the rests (the trouble kinds keep their own settings)";
                return FireScene(a.Id.Substring("scene:".Length), now, seed, out said);
            }
            switch (a.Id)
            {
                case "crying":
                case "execution":
                case "hermit":
                case "gathering":
                    said = RequestEncounter(a.Id, how, out asked);
                    if (!asked)
                        return said;
                    skipped = a.Id == "crying" ? "the hour (22:00-04:00), the wilds, once a night, the roll"
                            : a.Id == "execution" ? "the hour (05:00-22:00), outside the towns, once a day, the roll"
                            : a.Id == "hermit" ? "once a day, the roll (not the town rule: camps stay)"
                            : "the hour (midnight to 03:30), the wilds, once a night, the roll";
                    return null;
                case "stalker":
                    if (_stalker != null)
                        return "a stalker is out already";
                    _stalkerRequested = true;   // the stalker's own path (WestworldScript.Hosts.cs), which says so itself
                    skipped = "the roll and the wait between rolls";
                    return null;
                case "wake":
                    _wakeRequested = true;   // the person you face, one stage (WestworldScript.Awakening.cs)
                    skipped = "nothing: it needs someone a few steps in front of you";
                    return null;
                case "realtalk":
                    skipped = "RealVoiceTalk, the wait between talks, the chance rolls of each kind";
                    return TalkTestStart(now);   // WestworldScript.Talk.cs: null when it started
                case "rough":
                    skipped = "the reason (a grudge or a temper); RoughHandling still counts";
                    return RoughTestStart(now);   // WestworldScript.Roughs.cs
                case "font":
                    _fontTestUntil = Game.GameTime + 20000;   // the subtitle line in each face (WestworldScript.Subtitles.cs)
                    Log.Write("FONT test: 1-" + FontFaces.Length + " are " + string.Join(", ", FontFaces) + "; say which matches the game's subtitles");
                    return null;
                case "plea":
                    if (!_cfg.Pleading)
                        return "Pleading is off";
                    _pleaArmedUntil = now + 60000;   // the next aim at someone pleads at once (PleadTick), within a minute
                    skipped = "the 25 s wait between pleas, and anyone who has pleaded before";
                    said = "Aim at someone within 30 m: they'll plead.";
                    return null;
                case "arrival":
                    string town = _placeTown ?? _town;
                    if (_bridge.TownOnly(Game.Player.Character.Position) == null || town == null)
                        return "there's no town here";
                    if (!QueueArrival(town, false, true))
                        return "ArrivalTalk is off, or nothing to say here";
                    skipped = "a fresh visit (12 game hours away) and the roll";
                    return null;
                case "letter":
                    return TestLetter(out said);
                case "nod":
                    return NodFire(now, out skipped);
                case "musing":
                    if (!_cfg.HostsQuestionReality)
                        return "HostsQuestionReality is off";
                    if (!StrangerMusing())
                        return "nobody 3-14 m off to say it";
                    skipped = "the wait between musings and RealityChance";
                    return null;
                case "glitch":
                    if (!_cfg.Glitches)
                        return "Glitches is off (or West World is)";
                    if (_glitch != null)
                        return "a glitch is going on already";
                    StartGlitch(now);
                    if (_glitch == null)
                        return "nobody 3-16 m off to glitch";
                    skipped = "the 7-15 minutes between glitches";
                    return null;
                case "disbelief":
                    if (!_cfg.WestWorld)
                        return "West World is off";
                    return TestDisbelief(now);
                case "showminds":
                    _cfg.ShowMinds = !_cfg.ShowMinds;   // for this session (the panel's Show minds row writes it to the ini)
                    said = "Show minds " + (_cfg.ShowMinds ? "on: a card over the people near you." : "off.");
                    return null;
                default:
                    return "not built yet";
            }
        }

        /// A street scene of this kind between the nearest named pair that fits, played now through the Director (as a
        /// test: no tie or memory is noted). The same building as a reason's scene (BuildSceneFor, AddMeet).
        private string FireScene(string kind, int now, string seed, out string said)
        {
            said = null;
            if (_director.Busy)
                return "a scene is going on already";
            if (_wedding != null || _plea != null || _theft != null)
                return "a wedding, a plea or a thief chase is going on";
            Ped me = Game.Player.Character;
            bool saloon = _placeKind == "saloon";
            List<Candidate> people = ReasonCandidates(me, now, true).OrderBy(c => c.Ped.Position.DistanceTo(me.Position)).Take(8).ToList();
            if (people.Count < 2)
                return "fewer than two named people free within " + (int)ReasonRange + " m";
            string courtAnswer = kind == "court" ? new[] { "yes", "maybe", "no" }[_courtTurn++ % 3] : null;
            var reasons = new List<string>();
            string last = null;
            foreach (Candidate a in people)
                foreach (Candidate b in people)
                {
                    if (a == b)
                        continue;
                    float apart = a.Ped.Position.DistanceTo(b.Ped.Position);
                    if (apart > 10f)
                    {
                        last = "the nearest pairs are over 10 m apart";
                        continue;
                    }
                    string wont = PairWont(kind, a.Who, b.Who) ?? SceneNeeds(kind, a.Who, b.Who);
                    if (wont != null)
                    {
                        last = wont;
                        if (!reasons.Contains(wont))
                            reasons.Add(wont);
                        continue;
                    }
                    SceneCast cast = BuildCast(a.Ped, b.Ped, null, saloon, me, now, seed);
                    if (kind == "hosts" && cast.HostTier < 0)
                        return "HostsQuestionReality is off";
                    if (cast.Off.Contains(kind))
                        return "its own setting is off";   // the player switched this kind of scene off: a test doesn't play it either
                    if (kind == "gossip" && cast.Rumours.Count == 0)
                        return "there's nothing to gossip about here yet (no rumours in this town)";
                    string cant = TroubleCant(kind, a.Ped, b.Ped, cast);
                    if (cant != null)
                    {
                        last = cant;
                        if (cant.EndsWith(" is off") || cant.StartsWith("this game"))
                            return cant;   // no other pair will change that
                        continue;
                    }
                    Scene s = BuildSceneFor(kind, a.Ped, b.Ped, a.Who, b.Who, cast, seed, apart > 2.6f, false, courtAnswer);
                    if (s == null)
                    {
                        last = "the words don't fit " + a.Who.Name + " (" + (kind.StartsWith("x:") ? "no worry at that point" : "nothing to say") + ")";
                        continue;
                    }
                    AddMeet(s, kind, a.Ped, b.Ped, apart);
                    StartScene(s, new List<Ped> { a.Ped, b.Ped }, cast, seed, now, true);
                    Log.Write("TEST scene " + kind + (courtAnswer != null ? " (" + courtAnswer + ")" : string.Empty) + ": " + a.Who.Name + " & " + b.Who.Name + ", " + (int)apart + " m apart, " +
                              (int)a.Ped.Position.DistanceTo(me.Position) + " m from you");
                    TestActions.Entry row = TestActions.Find("scene:" + kind);
                    said = (row != null ? row.Label : kind) + ": " + a.Who.Name + " and " + b.Who.Name + ".";
                    return null;
                }
            return "nobody near fits" + (reasons.Count > 0 ? " (" + string.Join("; ", reasons.Take(3)) + ")" : last != null ? " (" + last + ")" : string.Empty);
        }

        /// What a kind needs of the one who starts it, so the scene says what their own thoughts say: a worry at its
        /// worst for a snap, one that ended well or badly for the news, a link or a worry of that kind for the linked
        /// meetings, someone hurt lately for the tending. Null when they have it.
        private string SceneNeeds(string kind, Someone a, Someone b)
        {
            long day = Bridge.Now() / 1440;
            string wk = null;
            int ws = -1;
            bool wg = false;
            bool worry = a.P != null && InnerLife.WorryNow(a.P, day, out wk, out ws, out wg);
            switch (kind)
            {
                case "x:snap":
                    return worry && ws == 2 ? null : a.Name + "'s worry isn't at its worst today";
                case "x:goodnews":
                    return worry && ws == 3 && wg ? null : a.Name + "'s worry hasn't ended well today";
                case "x:badnews":
                    return worry && ws == 3 && !wg ? null : a.Name + "'s worry hasn't ended badly today";
                case "x:debt":
                case "x:feud":
                case "x:courting":
                    string k = kind.Substring(2);
                    return (worry && wk == k) || _store.Links.Any(l => l.Id == a.Id && l.Kind == k) ? null : a.Name + " has no " + k + " on their mind";
                case "tend":
                    return b.Hurt != null ? null : b.Name + " hasn't been hurt lately";
                default:
                    return null;
            }
        }

        /// Why a pair can't play this kind (who they are, not what's between them); null when they can.
        private string PairWont(string kind, Someone a, Someone b)
        {
            bool men = !a.Woman && !b.Woman;
            switch (kind)
            {
                case "brawl": case "rough": case "drunk": case "cheat": case "standoff":
                    return men ? null : "it wants two men";
                case "pickpocket":
                    return a.Woman ? "the thief's lines want a man" : null;
                case "flirt": case "propose": case "court": case "x:sweet": case "x:courting":
                    return a.Woman != b.Woman || _cfg.SameSexRomance ? null : "it wants a man and a woman";
                default:
                    return null;
            }
        }

        /// The letters waiting for the player, handed over now as if at the post office; with none waiting, a test letter
        /// of thanks from the person near him he has met the most.
        private string TestLetter(out string said)
        {
            said = null;
            if (!_cfg.Letters)
                return "Letters is off";
            string town = _placeTown ?? _town;
            // the letters waiting for anywhere, or for this town's post office; the ones held elsewhere stay there
            List<Paper> waiting = _store.Papers.Where(p => !p.Delivered && (p.Town == null || p.Town == town)).ToList();
            if (waiting.Count == 0)
            {
                // none: a note that says plainly what it is, dropped the next time the game loads (Store.Fix)
                Paper t = new Paper
                {
                    Id = "test-" + Ids.New(), Kind = "letter", Title = "A test letter", From = "RealNPCs",
                    Body = "A test letter, sent from the settings panel's Testing page. If you can read this, letters reach you.\n\nIt will be gone the next time the game loads.", At = Bridge.Now()
                };
                _store.Papers.Add(t);
                waiting.Add(t);
            }
            foreach (Paper p in waiting)
            {
                p.Delivered = true;
                p.Town = town;
                p.At = Bridge.Now();
            }
            said = (waiting.Count == 1 ? "A letter" : waiting.Count + " letters") + " for you. [" + _readerKey + "] to read.";
            Log.Write("LETTERS handed over for a test: " + string.Join(", ", waiting.Select(p => p.Title)));
            return null;
        }

        /// The nearest named person within 15 m says the game's line of disbelief, with a thought over their head.
        private string TestDisbelief(int now)
        {
            Ped me = Game.Player.Character;
            foreach (Ped p in World.GetAllPeds().Where(x => x != null && x.Exists() && !x.IsDead && x.Handle != me.Handle && x.Position.DistanceTo(me.Position) < 15f)
                                                 .OrderBy(x => x.Position.DistanceTo(me.Position)))
            {
                if (MayDirectWhyNot(p) != null)
                    continue;
                Someone who = SomeoneOf(p);
                if (who == null)
                    continue;
                if (!Disbelieve(p.Handle, who.Name, who.Woman, who.Id + "|test|" + now, "a test"))
                    return who.Name + " said it less than a minute ago";
                return null;
            }
            return "no named person within 15 m";
        }

        /// A test start that refused (the talk and rough tests): logged in full as before, and the reason handed back.
        private static string TestStartRefused(string prefix, string message)
        {
            string full = message.StartsWith(prefix) ? message : prefix + message;
            Log.Write(full);
            return full.Substring(prefix.Length).TrimEnd('.');
        }

        /// For the F7 dump: the keys set, and the last fire.
        private string TestingStatus()
        {
            string keys = _testKeySpecs.Count == 0 ? "none set" : string.Join(", ", _testKeySpecs.Select(k => k.Key.Id + " " + k.Value));
            return "TestKeys " + (_cfg.TestKeys ? "on" : "off") + "; " + TestActions.All.Count + " actions; keys: " + keys + "; " + _fires + " fired this session" +
                   (_lastFire != null ? "; last " + _lastFire + " (" + _lastFireHow + ", " + Math.Max(0, (_now - _lastFireAt) / 1000) + " s ago): " + _lastFireResult : _lastFireResult != null ? "; last: " + _lastFireResult : string.Empty) +
                   (_fireDue != null ? "; waiting: " + _fireDue : string.Empty);
        }
    }
}
