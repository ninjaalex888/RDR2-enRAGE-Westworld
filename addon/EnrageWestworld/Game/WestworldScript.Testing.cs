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
        private string _fireDue, _fireHow, _lastFire, _lastFireHow, _lastFireResult;
        private int _fireDueAt, _fireWaitUntil, _lastFireAt, _fires;
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
            _fireDue = null;
            TestFire(id, how, now);
        }

        /// One fire: the rails first (refused, with why), then the feature's own start path, logged with what it skipped.
        private void TestFire(string id, string how, int now)
        {
            TestActions.Entry a = TestActions.Find(id);
            if (a != null && a.Id == "replay")
            {
                if (_lastFire == null)
                {
                    TestRefuse(id, "nothing has been fired yet this session");
                    return;
                }
                a = TestActions.Find(_lastFire);
                how = "replay of " + _lastFireHow;
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
            string refused = TestDispatch(a, how, now, out skipped, out said);
            if (refused != null)
            {
                TestRefuse(a.Id, refused);
                return;
            }
            _fires++;
            _lastFire = a.Id;
            _lastFireHow = how.StartsWith("replay of ") ? _lastFireHow : how;
            _lastFireAt = now;
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
            Hint((a != null ? a.Label : id) + ": not now, " + why + ".", 5000);
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
            if (!PLAYER.IS_PLAYER_CONTROL_ON(Game.Player.Handle))
                return "you have no control just now";
            return null;
        }

        /// The feature's own start path for each action; null when it was started (what the test skipped in `skipped`,
        /// what to tell the player in `said`), else why not.
        private string TestDispatch(TestActions.Entry a, string how, int now, out string skipped, out string said)
        {
            skipped = said = null;
            bool asked;
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
                    _stalkerRequested = true;   // the stalker's own path (WestworldScript.Hosts.cs)
                    skipped = "the roll and the wait between rolls";
                    said = "A stalker: someone near you, or a stranger if nobody near can be.";
                    return null;
                case "wake":
                    _wakeRequested = true;   // the person you face, one stage (WestworldScript.Awakening.cs)
                    skipped = "nothing: it needs someone a few steps in front of you";
                    return null;
                case "realtalk":
                    _talkTestRequested = true;   // WestworldScript.Talk.cs
                    skipped = "RealVoiceTalk, the wait between talks, the chance rolls of each kind";
                    return null;
                case "rough":
                    _roughTestRequested = true;   // WestworldScript.Roughs.cs
                    skipped = "the reason (a grudge or a temper); RoughHandling still counts";
                    return null;
                case "font":
                    _fontTestUntil = Game.GameTime + 20000;   // the subtitle line in each face (WestworldScript.Subtitles.cs)
                    Log.Write("FONT test: 1-" + FontFaces.Length + " are " + string.Join(", ", FontFaces) + "; say which matches the game's subtitles");
                    return null;
                case "showminds":
                    _cfg.ShowMinds = !_cfg.ShowMinds;   // for this session (the panel's Show minds row writes it to the ini)
                    said = "Show minds " + (_cfg.ShowMinds ? "on: a card over the people near you." : "off.");
                    return null;
                default:
                    return "not built yet";
            }
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
