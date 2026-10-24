// 0.4 plumbing for the script: settings (and reloading them while the game runs), where notices go, the
// bottom ticker and the papers reader drawn every frame, and the order the new features run in.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using EnrageWestworld.Core;
using NemesisSystem;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private readonly TickerBrain _ticker = new TickerBrain();
        private readonly List<NearThought> _near = new List<NearThought>();
        private readonly Reader _reader = new Reader();
        private readonly Director _director = new Director();
        private readonly Queue<Keys> _readerKeys = new Queue<Keys>();
        private KeySpec _readerKey;
        private bool _readerRequested;
        private string _iniPath;
        private DateTime _iniStamp;
        private int _nextNear, _nextIni, _nextUi, _controlOffAt = -1;
        private bool _uiOpen;
        private string _hint;          // a prompt shown above the ticker ("[F5] take the diary")
        private int _hintUntil;
        private int _now, _nextTickerUpdate;
        private TickerLine _tickerLine;
        private readonly Dictionary<string, int> _featureErrors = new Dictionary<string, int>();
        private readonly HashSet<string> _featureOff = new HashSet<string>();
        private Action _aFrame, _aReader, _aEvents, _aRequests, _aIni, _aPlaces, _aHosts, _aStalker, _aDrama, _aCeremony, _aCast, _aPickups, _aMission,
            _aPlead, _aTheft, _aArrival, _aLater, _aVoices, _aWake, _aWakeActs;
        private bool _missionPaused;

        /// Runs one feature; if it keeps throwing, only that feature is switched off (not the whole add-on).
        private void Safe(string feature, Action a)
        {
            if (_featureOff.Contains(feature))
                return;
            try
            {
                a();
            }
            catch (Exception ex)
            {
                int n;
                _featureErrors.TryGetValue(feature, out n);
                _featureErrors[feature] = ++n;
                if (n == 1)
                    Log.Write("ERROR in " + feature + ": " + ex);
                if (n >= 15)
                {
                    _featureOff.Add(feature);
                    Log.Write(feature + " kept failing and is switched off until the game restarts (details above). Everything else carries on.");
                }
            }
        }

        private void Setup04()
        {
            _aFrame = () => FrameImpl(_now);
            _aReader = ReaderInputImpl;
            _aEvents = () => PollWorldEventsImpl(_now);
            _aRequests = WorldRequestsImpl;
            _aIni = () => IniTick(_now);
            _aPlaces = () => Places(_now);
            _aHosts = () => HostsTick(_now);
            _aStalker = () => StalkerTick(_now);
            _aVoices = () => VoicesTick(_now);
            _aWake = () => WakeTick(_now);
            _aWakeActs = () => WakeActsTick(_now);
            _aDrama = () => DramaTick(_now);
            _aCeremony = () => Ceremonies(_now);
            _aCast = () => CastTick(_now);
            _aPickups = () => Pickups(_now);
            _aMission = () => MissionPause(_now);
            _aPlead = () => PleadTick(_now);
            _aTheft = () => TheftTick(_now);
            _aArrival = () => ArrivalTick(_now);
            _aLater = () => LaterTick(_now);
            _iniPath = Path.Combine(_dir, "enRAGE-Westworld.ini");
            _iniStamp = EnrageWestworld.Core.Settings.Stamp(_iniPath);
            ApplySettings();
            LoadVoices();
            Feed.Thinking = (who, text) => _ticker.Push(who + ", thinking", text, "near");
            Log.Write("0.4 features: ticker " + (_cfg.Ticker ? "on" : "off") + ", news to " + _cfg.NewsRoute + ", reader key " + _readerKey +
                      ", reveries " + (_cfg.Reveries ? _cfg.ReturnChance + "%" : "off") + ", stalkers " + (_cfg.Stalkers ? _cfg.StalkerChance + "%/day" : "off") +
                      ", voice " + (_cfg.SpokenVoice ? "on" : "off") + ".");
            Log.Write("0.5 features: scene reactions " + (_cfg.SceneReactions ? "on" : "off") + ", standoffs " + (_cfg.Standoffs ? "on" : "off") + ", songs " + (_cfg.Songs ? "on" : "off") +
                      ", tall tales " + (_cfg.TallTales ? "on" : "off") + ", pickpockets " + (_cfg.Pickpockets ? "on" : "off") + ", pleading " + (_cfg.Pleading ? "on" : "off") +
                      ", arrivals " + (_cfg.ArrivalTalk ? "on" : "off") + ", mourners " + (_cfg.Mourners ? "on" : "off") + ", posses " + (_cfg.PosseTalk ? "on" : "off") + ".");
        }

        private void ApplySettings()
        {
            Log.Debug = _cfg.DebugLog;
            Feed.SpeechMs = Math.Max(3, _cfg.SubtitleSeconds) * 1000;
            Feed.UseGameFeed = _cfg.NewsRoute == "gamefeed";
            Tts.Enabled = _cfg.SpokenVoice;
            Tts.Volume = _cfg.VoiceVolume;
            Tts.ManVoice = _cfg.VoiceMan ?? string.Empty;
            Tts.WomanVoice = _cfg.VoiceWoman ?? string.Empty;
            Tts.Variety = _cfg.VoiceVariety;
            _ticker.SlotMs = _cfg.TickerSeconds * 1000;
            _testKey = KeySpec.Parse(_cfg.TestKey, Keys.F7);
            _flirtKey = KeySpec.Parse(_cfg.FlirtKey, Keys.F6);
            _readerKey = KeySpec.Parse(_cfg.ReaderKey, Keys.F5);
            // Shift + the test key calls a stalker right away (Ctrl+Shift if the test key already uses Shift)
            _stalkerKey = new KeySpec { Key = _testKey.Key, Ctrl = _testKey.Ctrl || _testKey.Shift, Shift = true, Alt = _testKey.Alt };
            _wakeKey = KeySpec.Parse(_cfg.WakeKey, Keys.G);
        }

        private void IniTick(int now)
        {
            if (!_cfg.ReloadIni || now < _nextIni)
                return;
            _nextIni = now + 3000;
            DateTime stamp = EnrageWestworld.Core.Settings.Stamp(_iniPath);
            if (stamp == _iniStamp || stamp == DateTime.MinValue)
                return;
            _iniStamp = stamp;
            EnrageWestworld.Core.Settings fresh = EnrageWestworld.Core.Settings.Load(_iniPath, Log.Write);
            _iniStamp = EnrageWestworld.Core.Settings.Stamp(_iniPath);
            _cfg.CopyFrom(fresh);
            ApplySettings();
            Log.Write("Settings reloaded from the ini.");
            News("Westworld", "settings reloaded.", "news");
        }

        private void Shutdown04()
        {
            try
            {
                _director.Abort("stopping");
                WakeActsAbort();
                if (_stalker != null)
                    _stalker.Finish(true);
                Spawner.CleanupAll();
                Tts.Stop();
                Draw.FreeAll();
            }
            catch (Exception ex)
            {
                Log.Write("Cleanup: " + ex.Message);
            }
        }

        // ---- keys ---------------------------------------------------------------------------------

        private Keys _lastHotkey;
        private int _lastHotkeyAt;

        /// Windows repeats KeyDown while a key is held. The add-on's own keys act once per press.
        private bool HeldKeyRepeat(KeyEventArgs e)
        {
            if (_cfg == null || !(_testKey.Matches(e) || _flirtKey.Matches(e) || _readerKey.Matches(e) || _stalkerKey.Matches(e) || _wakeKey.Matches(e)))
                return false;
            int t = Environment.TickCount;
            bool repeat = e.KeyCode == _lastHotkey && t - _lastHotkeyAt >= 0 && t - _lastHotkeyAt < 600;
            _lastHotkey = e.KeyCode;
            _lastHotkeyAt = t;
            return repeat;
        }

        private bool KeyDown04(KeyEventArgs e)
        {
            if (_reader.Open)
            {
                if (_readerKey.Matches(e))
                    _readerKeys.Enqueue(Keys.Escape);
                else
                    _readerKeys.Enqueue(e.KeyCode);
                return true;
            }
            if (_readerKey.Matches(e))
            {
                _readerRequested = true;
                return true;
            }
            if (_stalkerKey.Matches(e))
            {
                _stalkerRequested = true;
                return true;
            }
            if (_wakeKey.Matches(e))
            {
                _wakeRequested = true;
                return true;
            }
            return false;
        }

        private void ReaderInput()
        {
            Safe("reader", _aReader);
        }

        private void PollWorldEvents(int now)
        {
            _now = now;
            if (_engine != null)
                Safe("events", _aEvents);
        }

        private void WorldRequests()
        {
            Safe("world requests", _aRequests);
        }

        private void ReaderInputImpl()
        {
            while (_readerKeys.Count > 0)
                _reader.Key(_readerKeys.Dequeue());
            if (!_readerRequested)
                return;
            _readerRequested = false;
            if (_uiOpen)
                return;
            if (TakePickup())
                return;
            List<Paper> papers = _store != null ? _store.Papers.Where(p => p.Delivered).OrderByDescending(p => p.At).ToList() : new List<Paper>();
            int unread = papers.FindIndex(p => !p.Read);
            _reader.Show(papers, unread >= 0 ? unread : 0);
        }

        // ---- where notices go ---------------------------------------------------------------------

        private void Route(Toast t)
        {
            string label, text;
            string kind = t.Kind;
            switch (t.Kind)
            {
                case "thought":
                    label = t.Title;
                    text = t.Text;
                    break;
                case "diary":
                    label = t.Title;
                    text = t.Text;
                    break;
                case "world":
                    // gang and faction news: the game's pop-up on the right, apart from the ticker
                    if (string.Equals(_cfg.WarNewsTo, "right", StringComparison.OrdinalIgnoreCase) && Toaster.Post(t.Title, t.Text))
                    {
                        Log.Write("WORLD NEWS (right): " + t.Title + ": " + t.Text);
                        return;
                    }
                    label = t.Title;
                    text = t.Text;
                    kind = "news";
                    break;
                case "life":
                    label = t.Title == NameOf(t.PersonId) ? null : t.Title;
                    text = t.Text;
                    kind = "news";
                    break;
                default:
                    label = t.Title;
                    text = t.Text;
                    kind = "news";
                    break;
            }
            Notice(label, text, kind, t.Kind == "thought" ? (PersonWoman(t.PersonId) ? "herself" : "himself") : null);
        }

        /// One notice, to wherever the player wants them. self: "himself"/"herself" for a private thought.
        private void Notice(string label, string text, string kind, string self = null)
        {
            if (string.IsNullOrEmpty(text))
                return;
            string route = _cfg.NewsRoute;
            if (route == "ticker")
            {
                _ticker.Push(label, text, kind);
                if (!_cfg.NoticesAsSubtitles)
                    return;
            }
            if (route == "enrage" && _bridge != null)
            {
                _bridge.EnrageNotice((label != null ? label + (self != null ? ", to " + self : "") + ": " : "") + (kind == "thought" ? "\"" + text + "\"" : text));
                return;
            }
            if (kind == "thought")
                Feed.Notice(label + ", to " + (self ?? "himself"), "\"" + text + "\"");
            else if (kind == "diary")
                Feed.Notice(label, "\"" + text + "\"");
            else
                Feed.Notice(label, text);
        }

        private void News(string label, string text, string kind = "news")
        {
            Notice(label, text, kind);
        }

        private void Hint(string text, int ms)
        {
            _hint = text;
            _hintUntil = Game.GameTime + ms;
            if (!_cfg.Ticker)
                Feed.Notice(null, text);
        }

        // ---- every frame ---------------------------------------------------------------------------

        private void Frame(int now)
        {
            _now = now;
            Safe("ticker", _aFrame);
        }

        private void FrameImpl(int now)
        {
            if (_reader.Open)
            {
                _reader.Render();
                return;
            }
            if (_engine == null || !_cfg.Ticker)
                return;
            if (!TickerVisible(now))
                return;
            if (now >= _nextNear)
            {
                _nextNear = now + 1000;
                RefreshNear();
            }
            if (now >= _nextTickerUpdate || _tickerLine == null)
            {
                _nextTickerUpdate = now + 200;
                _tickerLine = _ticker.Update(now, _near, () => _engine.Summary());
            }
            TickerLine line = _tickerLine;
            Ticker.Render(line, _cfg, now);
            if (_hint != null && now < _hintUntil)
            {
                float s = _cfg.TickerScale * 0.9f;
                float w = Draw.Width(_hint, s);
                float y = _cfg.TickerY - Draw.LineHeight(_cfg.TickerScale) * 1.4f - (line != null && line.Text != null && line.Text.Length > 90 ? Draw.LineHeight(_cfg.TickerScale) : 0f);
                Draw.Rect(0.5f - w / 2f - 0.006f, y - 0.003f, w + 0.012f, Draw.LineHeight(s) + 0.006f, 12, 10, 8, 170);
                Draw.Text(_hint, 0.5f - w / 2f, y, s, 236, 214, 160);
            }
        }

        private bool TickerVisible(int now)
        {
            if (HUD.IS_PAUSE_MENU_ACTIVE() || HUD.IS_HUD_HIDDEN() || CAM.IS_SCREEN_FADING_OUT())
                return false;
            if (!PLAYER.IS_PLAYER_CONTROL_ON(Game.Player.Handle))
            {
                if (_controlOffAt < 0)
                    _controlOffAt = now;
                if (now - _controlOffAt > 1500)
                    return false;
            }
            else
            {
                _controlOffAt = -1;
            }
            if (_bridge != null && now >= _nextUi)
            {
                _nextUi = now + 250;
                _uiOpen = _bridge.UiOpen();
            }
            return !_uiOpen;
        }

        private void RefreshNear()
        {
            _near.Clear();
            Ped me = Game.Player.Character;
            if (me == null || !me.Exists())
                return;
            Vector3 pos = me.Position;
            float r = _cfg.TickerNearbyMetres;
            long now = Bridge.Now();
            Ped stranger = null;
            float strangerDist = 99f;
            foreach (Ped ped in World.GetAllPeds())
            {
                int h = ped.Handle;
                if (h == me.Handle || Spawner.IsOurs(h))
                    continue;
                float d = ped.Position.DistanceTo(pos);
                if (d > r || ENTITY.IS_ENTITY_DEAD(h) || !PED.IS_PED_HUMAN(h))
                    continue;
                NemesisRecord rec = _bridge.RecordAt(h);
                if (rec != null)
                {
                    if (!rec.IsAlive)
                        continue;
                    Person p = PersonOf(rec);
                    _near.Add(new NearThought { Key = p.Id, Name = p.Name, Distance = d, Text = () => _engine.TickerThought(p, now) });
                    continue;
                }
                Acquaintance a = _bridge.AcquaintanceAt(h);
                if (a != null && !string.IsNullOrEmpty(a.Name))
                {
                    Folk f = Snapshot.Of(a);
                    Person fp = _engine.PersonOfFolk(f.Key, f.Name, f.Woman, f.Town, now);
                    PersonState st = _store.Get(fp);
                    _near.Add(new NearThought
                    {
                        Key = f.Key,
                        Name = f.Name,
                        Distance = d + 3f,
                        Text = () => st.WakeStage > 0 ? Hosts.Thought(fp, st, _engine.Player, f.Key + now / 180) : Folks.Thought(f, st, _engine.Player, f.Key + now / 180, _cfg.Romance)
                    });
                    continue;
                }
                if (d < 12f && (stranger == null || d < strangerDist) && !PED.IS_PED_IN_COMBAT(h, 0) && !PED.IS_PED_FLEEING(h) && ResidentEligible(ped))
                {
                    stranger = ped;
                    strangerDist = d;
                }
            }
            // nobody with a name close by: a passing stranger's thought, now and then
            if (_near.Count == 0 && stranger != null)
            {
                bool woman = !PED.IS_PED_MALE(stranger.Handle);
                string label = Grammar.UpperFirst(Director.Kind(stranger, woman));
                string key = "stranger:" + stranger.Handle;
                string town = _placeTown;
                Resident res = ResidentOf(stranger);
                if (res != null)
                {
                    // an ordinary person, but with a name, a trade and a town: and a mind that may be waking
                    Person rp = _engine.PersonOfResident(res);
                    key = res.Id;
                    label = res.Name;
                    _near.Add(new NearThought
                    {
                        Key = key,
                        Name = label,
                        Distance = strangerDist + 10f,
                        Text = () =>
                        {
                            PersonState rst = _store.Find(res.Id);
                            return rst != null && rst.WakeStage > 0 ? Hosts.Thought(rp, rst, _engine.Player, res.Id + "|" + now / 180) : _engine.ResidentThought(rp, CLOCK.GET_CLOCK_HOURS(), res.Id + "|" + now / 600, new InnerLife.PlayerLook { Near = strangerDist < 10f, Armed = WEAPON.IS_PED_ARMED(me.Handle, 4), Mounted = PED.IS_PED_ON_MOUNT(me.Handle) });
                        }
                    });
                    return;
                }
                _near.Add(new NearThought { Key = key, Name = label, Distance = strangerDist + 10f, Text = () => _engine.StrangerThought(key + now / 600, town, woman) });
            }
        }

        /// A story mission started: give back everyone the add-on borrowed or made, and keep out of the way until
        /// it's over. The ticker, deeds and diaries carry on.
        private void MissionPause(int now)
        {
            if (_missionPaused)
                return;
            _missionPaused = true;
            _director.Abort("a mission started");
            WakeActsAbort();
            _glitch = null;
            if (_stalker != null)
            {
                Fallen f = _stalker.Revenant;
                _stalker.Finish(false);
                if (f != null && !f.Done)
                    f.ReturnAt = Bridge.Now() + 240;
                _stalker = null;
                _stalkerPendingUntil = 0;
                _stalkerCooldown = now + 120000;
            }
            if (_wedding != null)
                EndWeddingRun("a mission started");
            _casts.Clear();
            _plea = null;
            _theft = null;
            _arrival = null;
            _later.Clear();
            Log.Write("A mission started: scenes, stalkers and ceremonies wait until it's over.");
        }

        // ---- the order things run in ------------------------------------------------------------------

        private void Tick04(int now)
        {
            _now = now;
            Safe("settings reload", _aIni);
            Safe("places", _aPlaces);
            Tts.Enabled = _cfg.SpokenVoice;
            if (MISC.GET_MISSION_FLAG())
            {
                Safe("mission pause", _aMission);
                return;
            }
            if (_missionPaused)
            {
                _missionPaused = false;
                Log.Detail("The mission is over: scenes, stalkers and ceremonies carry on.");
            }
            Safe("lines", _aLater);
            if (!PLAYER.IS_PLAYER_CONTROL_ON(Game.Player.Handle))
                return;
            Safe("pleading", _aPlead);
            Safe("pickpockets", _aTheft);
            Safe("arrivals", _aArrival);
            Safe("hosts", _aHosts);
            Safe("stalker", _aStalker);
            Safe("voices", _aVoices);
            Safe("awakening", _aWake);
            Safe("wakeacts", _aWakeActs);
            Safe("scenes", _aDrama);
            Safe("ceremonies", _aCeremony);
            Safe("kin", _aCast);
            Safe("pickups", _aPickups);
        }
    }
}
