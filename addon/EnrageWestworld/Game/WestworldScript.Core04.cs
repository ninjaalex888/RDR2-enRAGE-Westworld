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
        private Func<string, bool> _inViewId;

        /// The handle of someone in view near you (RefreshNear's list, once a second), by their id; 0 if not.
        private int NearHandleOf(string id)
        {
            if (id == null)
                return 0;
            for (int i = 0; i < _near.Count; i++)
                if (_near[i].Key == id && _near[i].Handle != 0)
                    return _near[i].Handle;
            return 0;
        }

        /// Game minutes between a new thought for a counter keeper or a resident: 45, about the 90 real seconds the
        /// ticker waits before asking again (it was 600, so the same thought came back for 20 minutes).
        private const long ThoughtEvery = 45;

        /// Closer than this, a person is right beside you: every other ticker turn goes to them (TickerBrain).
        private const float BesideMetres = 3.5f;
        private readonly Dictionary<string, int> _thoughtSoundAt = new Dictionary<string, int>();
        private readonly Reader _reader = new Reader();
        private readonly Director _director = new Director();
        private readonly Queue<Keys> _readerKeys = new Queue<Keys>();
        private KeySpec _readerKey;
        private bool _readerRequested;
        private KeySpec _tickerNextKey;
        private bool _tickerNextRequested;
        private string _iniPath;
        private DateTime _iniStamp;
        private int _nextNear, _nextIni, _nextUi, _controlOffAt = -1;
        private bool _uiOpen;
        private string _hint;          // a prompt shown above the ticker ("[N] take the diary")
        private int _hintUntil;
        private int _now, _nextTickerUpdate;
        private TickerLine _tickerLine;
        private readonly Dictionary<string, int> _featureErrors = new Dictionary<string, int>();
        private readonly HashSet<string> _featureOff = new HashSet<string>();
        private Action _aFrame, _aReader, _aEvents, _aRequests, _aIni, _aPlaces, _aHosts, _aStalker, _aDrama, _aCourtThoughts, _aCeremony, _aCast, _aPickups, _aMission,
            _aPlead, _aTheft, _aArrival, _aLater, _aVoices, _aWake, _aWakeActs;
        private bool _missionPaused;

        /// Runs one feature; if it keeps throwing, only that feature is switched off (not the whole add-on).
        private void Safe(string feature, Action a)
        {
            if (_featureOff.Contains(feature))
                return;
            // timed for the frame-time meter (WestworldScript.Perf.cs): its own time, nested features subtracted
            bool timed = _cfg == null || _cfg.PerfMeter;
            PerfSlot slot = timed ? PerfSlotOf(feature) : null;
            int depth = 0;
            long start = timed ? PerfBegin(out depth) : 0;
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
            finally
            {
                if (timed)
                    PerfEnd(slot, start, depth);
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
            _aCourtThoughts = () => CourtThoughtsDue(_now);
            _aCeremony = () => Ceremonies(_now);
            _aCast = () => CastTick(_now);
            _aPickups = () => Pickups(_now);
            _aMission = () => MissionPause(_now);
            _aPlead = () => PleadTick(_now);
            _aTheft = () => TheftTick(_now);
            _aArrival = () => ArrivalTick(_now);
            _aLater = () => LaterTick(_now);
            _iniPath = Path.Combine(_dir, "RealNPCs.ini");
            _iniStamp = EnrageWestworld.Core.Settings.Stamp(_iniPath);
            ApplySettings();
            LoadVoices();
            Feed.Thinking = (who, text, ped) => _ticker.Push(who + ", thinking", text, "near", ped);
            Log.Write("0.4 features: ticker " + (_cfg.Ticker ? "on" : "off") + ", news to " + _cfg.NewsRoute + ", reader key " + _readerKey +
                      ", reveries " + (_cfg.Reveries ? _cfg.ReturnChance + "%" : "off") + ", stalkers " + (_cfg.Stalkers ? _cfg.StalkerChance + "% every " + _cfg.StalkerEveryMinutes + " min" : "off") + ".");
            Log.Write("West World " + (_cfg.WestWorld ? "on (awakening " + _cfg.AwakeningShare + "%, breakdown " + _cfg.AwakeningBreakdown + "%, despair " + _cfg.AwakeningDespair + "%)" : "off") +
                      ", Stalkers " + (_cfg.Stalkers ? "on (" + _cfg.StalkerChance + "% every " + _cfg.StalkerEveryMinutes + " min, key " + _stalkerKey + ", when you stand still: " + _cfg.StalkerWhenStill + ")" : "off") + ".");
            Log.Write("0.5 features: scene reactions " + (_cfg.SceneReactions ? "on" : "off") + ", standoffs " + (_cfg.Standoffs ? "on" : "off") + ", songs " + (_cfg.Songs ? "on" : "off") +
                      ", tall tales " + (_cfg.TallTales ? "on" : "off") + ", pickpockets " + (_cfg.Pickpockets ? "on" : "off") + ", pleading " + (_cfg.Pleading ? "on" : "off") +
                      ", arrivals " + (_cfg.ArrivalTalk ? "on" : "off") + ", mourners " + (_cfg.Mourners ? "on" : "off") + ", posses " + (_cfg.PosseTalk ? "on" : "off") + ".");
        }

        private void ApplySettings()
        {
            Log.Debug = _cfg.DebugLog;
            Feed.SpeechMs = Math.Max(3, _cfg.SubtitleSeconds) * 1000;
            Feed.Fallback = (title, text) => NewsPush(title, text, "news");   // when the game won't take top-left updates
            Feed.SubtitlesOn = _cfg.Subtitles != "off";
            _ticker.SlotMs = _cfg.TickerSeconds * 1000;
            _ticker.GapMs = _cfg.TickerGapSeconds * 1000;
            _testKey = KeySpec.Parse(_cfg.TestKey, Keys.F7);
            _flirtKey = KeySpec.Parse(_cfg.FlirtKey, Keys.F2);
            _readerKey = KeySpec.Parse(_cfg.ReaderKey, Keys.N);
            _stalkerKey = KeySpec.Parse(_cfg.StalkerKey, Keys.OemCloseBrackets);
            _execKey = KeySpec.Parse(_cfg.ExecutionKey, Keys.OemOpenBrackets);
            _cryingKey = KeySpec.Parse(_cfg.CryingWomanKey, Keys.None);   // the Testing page's keys: none unless the player sets one
            _hermitKey = KeySpec.Parse(_cfg.HermitKey, Keys.None);
            _gatherKey = KeySpec.Parse(_cfg.GatheringKey, Keys.None);
            _wakeKey = KeySpec.Parse(_cfg.WakeKey, Keys.Y);
            _settingsKey = KeySpec.Parse(_cfg.SettingsKey, Keys.F9);
            _settingsKey2 = KeySpec.Parse(_cfg.SettingsKey2, Keys.None);   // None (or nothing): no second key
            _tickerNextKey = KeySpec.Parse(_cfg.TickerNextKey, Keys.T);
            BuildTestKeys();   // the Testing page's keys ([Testing], WestworldScript.Testing.cs)
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
            _cfg.CopyFrom(fresh.Gated());
            ApplySettings();
            Log.Write("Settings reloaded from the ini.");
            News("RealNPCs", "settings reloaded.", "news");
        }

        private void Shutdown04()
        {
            PassingAbort();
            WanderLaterClear();
            SightsReset("shutdown");
            try
            {
                FlirtPromptEnd();
                _director.Abort("stopping");
                EndEncounters("the add-on stopped");
                WakeActsAbort();
                GrudgesAbort();      // someone walking up to have it out with you stops (their walk order cleared)
                SoberVoices(true);   // the drink goes out of every voice we put it in
                WakeVoiceEnd(_now, "the add-on stopped");
                AnimTestEnd("the add-on stopped");
                GunshotTestEnd("the add-on stopped");
                TrystAbort();
                if (_rough != null)
                    RoughEnd(_rough, "stopped: the add-on stopped", _now);
                if (_stalker != null)
                    _stalker.Finish(true);
                Spawner.CleanupAll();
                ClearRegularBlips();
                // the ticker's text is not freed here: text sent to the game this frame is drawn after the script has
                // stopped, and freeing it under the game is the likeliest cause of a crash seen on an Insert reload
                // (0.6.1). It's a few kilobytes, kept until the game closes.
            }
            catch (Exception ex)
            {
                Log.Write("Cleanup: " + ex.Message);
            }
        }

        // ---- keys ---------------------------------------------------------------------------------

        private Keys _lastHotkey;
        private int _lastHotkeyAt;
        private bool _hotkeyLetGo;

        /// Windows repeats KeyDown while a key is held. The add-on's own keys act once per press; a quick second
        /// press counts once the key has been let go (T tapped through a long ticker line).
        private bool HeldKeyRepeat(KeyEventArgs e)
        {
            if (_cfg == null || !(_testKey.Matches(e) || _flirtKey.Matches(e) || _readerKey.Matches(e) || IsTestingKey(e) || SettingsKeyHit(e) || _tickerNextKey.Matches(e)))
                return false;
            int t = Environment.TickCount;
            bool repeat = e.KeyCode == _lastHotkey && !_hotkeyLetGo && t - _lastHotkeyAt >= 0 && t - _lastHotkeyAt < 600;
            _lastHotkey = e.KeyCode;
            _lastHotkeyAt = t;
            _hotkeyLetGo = false;
            return repeat;
        }

        private void OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == _lastHotkey)
                _hotkeyLetGo = true;
        }

        private bool KeyDown04(KeyEventArgs e)
        {
            if (PanelOpen)
            {
                _panelKeys.Enqueue(e);   // while the settings are open, every key is theirs
                return true;
            }
            if (SettingsKeyHit(e))
            {
                _settingsRequested = true;
                return true;
            }
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
            // the Testing page's keys (the stalker, waking someone, the encounters, the talk and rough tests...): each asks
            // for its fire, which the tick does (WestworldScript.Testing.cs)
            if (TestKeyHit(e))
                return true;
            if (_tickerNextKey.Matches(e) && _cfg.Ticker)
            {
                _tickerNextRequested = true;
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
                    // over the thinker's head when they're in view (Lex, 2026-10-27); every one of these is about you (what
                    // they remember of you, heard of you, your name, their heart, a sighting, kin's letters), so one far off
                    // goes to the "Around town" box as before (Lex: far-off thoughts there only when they're about you)
                    int h = NearHandleOf(t.PersonId);
                    if (h != 0)
                    {
                        _ticker.Push(t.Title + ", thinking", t.Text, "near", h);
                        return;
                    }
                    label = t.Title;
                    text = t.Text;
                    break;
                case "diary":
                    label = t.Title;
                    text = t.Text;
                    break;
                case "world":
                    // gang and faction news: the game's own top-left updates (by default), the "Around town" box, or the ticker
                    string war = _cfg.WarRoute;
                    if (war == "feed")
                        Feed.Notice(t.Title, t.Text);
                    else if (war == "corner")
                        NewsPush(t.Title, t.Text, "news");
                    else
                        _ticker.Push(t.Title, t.Text, "news");
                    Log.Write("WORLD NEWS (" + war + "): " + t.Title + ": " + t.Text);
                    return;
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
            string route = _cfg.NewsRoute;   // corner, ticker or gamefeed (Settings.NewsRoute): never white text
            if (route == "corner")
            {
                NewsPush(label, text, kind);   // the "Around town" box (WestworldScript.News.cs)
                return;
            }
            if (route == "ticker")
            {
                _ticker.Push(label, text, kind);
                return;
            }
            // a toast at the top left of the screen (Toaster)
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
            FeelingsStep(now);
            ShowMindsStep(now);   // the Testing page's cards over people (WestworldScript.ShowMinds.cs)
            WakeTagsStep(now);    // and its wake stages over heads (WestworldScript.WakeTags.cs)
            NewsStep(now);
            SubtitleDraw(now);
        }

        private void FrameImpl(int now)
        {
            bool askNext = _tickerNextRequested;   // the ticker key: only counts while the ticker is showing
            _tickerNextRequested = false;
            if (PanelOpen)
                return;   // the settings panel draws itself; no ticker under it
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
            if (askNext && !Ticker.NextPart(_tickerLine, _cfg, now))
            {
                _ticker.Skip();
                _nextTickerUpdate = 0;
            }
            if (now >= _nextTickerUpdate || _tickerLine == null)
            {
                _nextTickerUpdate = now + 200;
                TickerLine was = _tickerLine;
                _tickerLine = _ticker.Update(now, _near, _cfg.NewsRoute == "corner" ? null : (Func<string>)(() => _engine.Summary()));   // the news has its own box then
                if (_tickerLine != was)
                    Safe("thought sound", () => ThoughtSound(_tickerLine, now));
            }
            TickerLine line = _tickerLine;
            // a thought goes over its thinker's head (WestworldScript.OverHead.cs), under its own guard, and with
            // ThoughtsOverHeads on never on the bar (Lex, 2026-10-27): one whose thinker is off screen waits, and after
            // a moment the ticker moves on; anything else (the news, the summary) on the bar
            _overHeadDrawn = false;
            if (_aOverHead == null)
                _aOverHead = () => _overHeadDrawn = DespairOverHead(_now) || ThoughtOverHead(_tickerLine, _now);   // despair's words first, while they last (WestworldScript.Despair.cs)
            Safe("thoughts over heads", _aOverHead);
            if (!_overHeadDrawn && OverHeadOnly(line, now))
            {
                _ticker.Skip();
                _nextTickerUpdate = 0;
            }
            else if (!_overHeadDrawn && !IsOverHeadKind(line))
            {
                Ticker.Render(line, _cfg, now);
                if (_cfg.ThoughtMarker)
                    DrawThinkingMark(line);
            }
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

        /// The day of the week and the year, for what people think about (InnerLife). In the epilogue the clock may
        /// still say 1899, so John means 1907 either way.
        private static void ReadClock()
        {
            InnerLife.Weekday = CLOCK.GET_CLOCK_DAY_OF_WEEK();
            int year = CLOCK.GET_CLOCK_YEAR();
            InnerLife.ClockYear = IsJohn() && year < 1900 ? 1907 : year;
        }

        /// What the residents near you are living through, for the F7 dump.
        private string WorriesStatus()
        {
            Ped me = Game.Player.Character;
            long day = _store.LastGameMin > 0 ? _store.LastGameMin / 1440 : -1;
            List<string> parts = _resBind.Where(kv => ENTITY.DOES_ENTITY_EXIST(kv.Key) && !kv.Value.R.Dead && new Ped(kv.Key).Position.DistanceTo(me.Position) < 40f)
                .Take(6).Select(kv => kv.Value.R.Name + ": " + InnerLife.WorryStatus(_engine.PersonOfResident(kv.Value.R), day)).ToList();
            return "weekday " + InnerLife.Weekday + ", year " + InnerLife.ClockYear + "; " + (parts.Count == 0 ? "nobody with a name near" : string.Join("; ", parts));
        }

        /// Who the ticker counts as near you, nearest first, and what each would think right now, for the F7 dump.
        /// The number is the ticker's ranking distance (counter keepers +2 m, enRAGE's townsfolk +3, passers-by +10).
        private string NearStatus()
        {
            if (_near.Count == 0)
                return "nobody (the ticker shows the news)";
            return string.Join("; ", _near.OrderBy(n => n.Distance).Take(8).Select(n =>
            {
                string t;
                try
                {
                    t = n.Text != null ? n.Text() : null;
                }
                catch (Exception ex)
                {
                    t = "FAILED: " + ex.GetType().Name + ": " + ex.Message;
                }
                return n.Name + " (" + (int)n.Distance + "): " + (string.IsNullOrEmpty(t) ? "(nothing)" : t);
            }));
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
            ReadClock();
            List<KeyValuePair<Ped, float>> strangers = new List<KeyValuePair<Ped, float>>();
            foreach (Ped ped in World.GetAllPeds())
            {
                int h = ped.Handle;
                if (IsHermit(h))
                {
                    float hd = ped.Position.DistanceTo(pos);
                    if (hd <= r && !ENTITY.IS_ENTITY_DEAD(h))
                        HermitNear(ped, hd);
                    continue;
                }
                if (IsGatherer(h))
                {
                    float gd = ped.Position.DistanceTo(pos);
                    if (gd <= r && !ENTITY.IS_ENTITY_DEAD(h))
                        GatherNear(ped, gd);
                    continue;
                }
                if (IsExecutionPed(h))
                {
                    float ed = ped.Position.DistanceTo(pos);
                    if (ed <= r && !ENTITY.IS_ENTITY_DEAD(h))
                        ExecutionNear(ped, ed);
                    continue;
                }
                if (h == me.Handle || Spawner.IsOurs(h))
                    continue;
                float d = ped.Position.DistanceTo(pos);
                if (d > r || ENTITY.IS_ENTITY_DEAD(h) || !PED.IS_PED_HUMAN(h))
                    continue;
                // only people you can see think on the ticker, so it's always clear whose thought it is
                bool seen = ENTITY.IS_ENTITY_ON_SCREEN(h) && ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(me.Handle, h, 17);
                NemesisRecord rec = _bridge.RecordAt(h);
                if (rec != null)
                {
                    if (!rec.IsAlive || !seen)
                        continue;
                    Person p = PersonOf(rec);
                    _near.Add(new NearThought { Key = p.Id, Name = p.Name, Handle = h, Distance = d, Beside = d < BesideMetres, Text = () => _engine.TickerThought(p, now) });
                    continue;
                }
                // right beside you: a new thought every 15 game minutes (30 s), to match the ticker's quicker turn
                long every = d < BesideMetres ? 15 : ThoughtEvery;
                Acquaintance a = _bridge.AcquaintanceAt(h);
                if (a != null && !string.IsNullOrEmpty(a.Name))
                {
                    if (!seen)
                        continue;
                    Folk f = Snapshot.Of(a);
                    Person fp = _engine.PersonOfFolk(f.Key, f.Name, f.Woman, f.Town, now);
                    PersonState st = _store.Get(fp);
                    float fd = d;
                    _near.Add(new NearThought
                    {
                        Key = f.Key,
                        Handle = h,
                        Name = f.Name,
                        Distance = d + 3f,
                        Beside = d < BesideMetres,
                        Text = () => _cfg.WestWorld && st.WakeStage > 0 ? Hosts.Thought(fp, st, _engine.Player, f.Key + now / 180)
                            : _engine.FolkThought(f, fp, st, f.Key + "|" + now / every, CLOCK.GET_CLOCK_HOURS(), Look(me, fd))
                    });
                    continue;
                }
                // the person at a counter: one person for good, who knows you
                PostKind pk = _cfg.TownPosts ? Posts.Of(Natives.ModelName(ped)) : null;
                if (pk != null)
                {
                    Resident holder = HolderAt(ped, pk);
                    if (holder != null)
                    {
                        NotePostVisit(holder, d);   // a visit counts whether or not you're looking at them
                        float near = d;
                        if (seen)
                            _near.Add(new NearThought
                            {
                                Key = holder.Id,
                                Handle = h,
                                Name = holder.Name + ", " + pk.Role,
                                Distance = d + 2f,
                                Beside = d < BesideMetres,
                                Text = () =>
                                {
                                    PersonState hs = _store.Find(holder.Id);
                                    return _cfg.WestWorld && hs != null && hs.WakeStage > 0 ? Hosts.Thought(_engine.PersonOfResident(holder), hs, _engine.Player, holder.Id + "|" + now / 180)
                                        : _engine.PostThought(holder, near < 10f, CLOCK.GET_CLOCK_HOURS(), holder.Id + "|" + now / every, Look(me, near));
                                }
                            });
                    }
                    continue;
                }
                if (seen && d < 12f && !PED.IS_PED_IN_COMBAT(h, 0) && !PED.IS_PED_FLEEING(h) && ResidentEligible(ped))
                    strangers.Add(new KeyValuePair<Ped, float>(ped, d));
            }
            // the three nearest ordinary people get turns too (behind anyone with a name, unless right beside you)
            bool named = _near.Count > 0;
            foreach (KeyValuePair<Ped, float> s in strangers.OrderBy(x => x.Value).Take(3))
            {
                Ped stranger = s.Key;
                float sd = s.Value;
                bool woman = !PED.IS_PED_MALE(stranger.Handle);
                Resident res = ResidentOf(stranger);
                if (res != null)
                {
                    // an ordinary person, but with a name, a trade and a town: and a mind that may be waking. A town
                    // regular counts each time you meet them, and may know you again.
                    Person rp = _engine.PersonOfResident(res);
                    int met = res.Regular ? NoteRegularMet(res, stranger.Handle, rp) : 0;
                    long every = sd < BesideMetres ? 15 : ThoughtEvery;
                    _near.Add(new NearThought
                    {
                        Key = res.Id,
                        Name = ShownName(stranger.Handle, res.Name),
                        Handle = stranger.Handle,
                        Distance = sd + 10f,
                        Beside = sd < BesideMetres,
                        Text = () =>
                        {
                            PersonState rst = _store.Find(res.Id);
                            InnerLife.PlayerLook look = Look(me, sd);
                            look.Seen = met;
                            return _cfg.WestWorld && rst != null && rst.WakeStage > 0 ? Hosts.Thought(rp, rst, _engine.Player, res.Id + "|" + now / 180) : _engine.ResidentThought(rp, CLOCK.GET_CLOCK_HOURS(), res.Id + "|" + now / every, look);
                        }
                    });
                }
                else if (!named && _near.Count == 0)
                {
                    // someone who can't be a resident: only when nobody else is near, as before
                    string key = "stranger:" + stranger.Handle;
                    string town = _placeTown;
                    _near.Add(new NearThought { Key = key, Name = Grammar.UpperFirst(Director.Kind(stranger, woman)), Handle = stranger.Handle, Distance = sd + 10f, Text = () => _engine.StrangerThought(key + now / 600, town, woman) });
                }
            }
            NoteNearby();
        }

        private int _nextNearby;

        /// Every few seconds: the named people in view, as the others see them (InnerLife.Nearby), so their thoughts can
        /// turn to each other: envy of the well-dressed, pity for the drunk, a wary eye on the hothead (InnerLife.Now.cs).
        private void NoteNearby()
        {
            int t = Game.GameTime;
            if (t < _nextNearby)
                return;
            _nextNearby = t + 5000;
            var list = new List<InnerLife.Nearby>();
            foreach (NearThought n in _near.Where(x => x.Handle != 0 && x.Key != null && !x.Key.StartsWith("stranger:")).Take(6))
            {
                Ped p = new Ped(n.Handle);
                if (!p.Exists())
                    continue;
                Someone who = SomeoneOf(p);
                if (who == null)
                    continue;
                // what they're known for this week: putting a man's face in the bar, or being caught at a pocket
                long g = Bridge.Now();
                string known = _store.Ties.Any(x => x.Did(who.Id) && x.Kind == "rough" && g - x.At >= 0 && g - x.At < 7 * 1440L) ? "bully"
                             : _store.Ties.Any(x => x.Did(who.Id) && x.Kind == "pickpocket" && g - x.At >= 0 && g - x.At < 7 * 1440L) ? "thief" : null;
                list.Add(new InnerLife.Nearby { Id = who.Id, Name = Person.FirstOf(who.Name), Temper = who.Temper, Woman = who.Woman, WellOff = who.WellOff, Drunk = who.Drunk, Known = known });
            }
            _engine.Nearby = list;
            _engine.NearbyAt = _store.LastGameMin;   // the same clock ThoughtDay reads (Bridge.Now() runs ahead of it)
        }

        /// What someone this far off can see of the player, for their thought about him.
        private static InnerLife.PlayerLook Look(Ped me, float dist)
        {
            return new InnerLife.PlayerLook { Near = dist < 10f, Armed = WEAPON.IS_PED_ARMED(me.Handle, 4), Mounted = PED.IS_PED_ON_MOUNT(me.Handle) };
        }

        /// A person's thought just came up on the ticker: if they're within a few steps, a small wordless sound (a breath,
        /// a "hm"), at most once every three minutes each. The words stay on the ticker; nothing is said aloud.
        private void ThoughtSound(TickerLine line, int now)
        {
            if (!_cfg.ThoughtSounds || line == null || line.Kind != "near" || line.Key == null)
                return;
            NearThought n = _near.FirstOrDefault(x => x.Key == line.Key);
            if (n == null || n.Handle == 0 || !ENTITY.DOES_ENTITY_EXIST(n.Handle) || ENTITY.IS_ENTITY_DEAD(n.Handle) || Speech.Busy(n.Handle))
                return;
            if (new Ped(n.Handle).Position.DistanceTo(Game.Player.Character.Position) > 8f)
                return;
            int at;
            if (_thoughtSoundAt.TryGetValue(line.Key, out at) && now - at < 180000 && now >= at)
                return;
            if (_thoughtSoundAt.Count > 200)
                foreach (string k in _thoughtSoundAt.Where(kv => now - kv.Value > 180000 || now < kv.Value).Select(kv => kv.Key).ToList())
                    _thoughtSoundAt.Remove(k);
            _thoughtSoundAt[line.Key] = now;
            Speech.SayAny(n.Handle, Speech.Hum);
        }

        /// A story mission started: give back everyone the add-on borrowed or made, and keep out of the way until
        /// it's over. The ticker, deeds and diaries carry on.
        private void MissionPause(int now)
        {
            if (_missionPaused)
                return;
            _missionPaused = true;
            _director.Abort("a mission started");
            EndEncounters("a mission started");
            WakeActsAbort();
            AnimTestEnd("a mission started");
            GunshotTestEnd("a mission started");
            TrystAbort();
            PassingAbort();
            WanderLaterClear();
            SightsAbort();
            GrudgesAbort();
            SoberVoices(true);
            if (_rough != null)
                RoughEnd(_rough, "stopped: a mission started", now);
            _glitch = null;
            if (_stalker != null)
            {
                Fallen f = _stalker.Revenant;
                _stalker.Finish(false);
                if (f != null && !f.Done)
                    f.ReturnAt = Bridge.Now() + 240;
                _stalker = null;
                _stalkerRecord = false;
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
            TestingStep(now);   // a test asked for by its key or the panel (refused in a mission, with why)
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
            LedgerStep(now);   // deaths are noted whether or not the player has control (not in story missions)
            DeedEyesStep(now);   // who saw a killing is still alive to tell it (WestworldScript.Watch.cs)
            RecordFatesStep(now);   // a record's death RealNPCs caused, written into its record (WestworldScript.Directing.cs)
            TrystWatchdog(now);   // never left dark (outside the trysts' own guard)
            TrystStep(now);       // runs with the player's controls off while it's dark
            HookupsStep(now);     // CasualHookups' scene may take the controls too (WestworldScript.Hookups.cs)
            if (!PLAYER.IS_PLAYER_CONTROL_ON(Game.Player.Handle))
                return;
            QuietStep(now);
            PostsStep(now);
            RegularsStep(now);
            EncountersStep(now);
            TalkStep(now);
            SubtitleStep(now);
            TerritoryStep(now);
            FlirtPromptStep();
            RoughStep(now);
            Safe("pleading", _aPlead);
            Safe("pickpockets", _aTheft);
            Safe("arrivals", _aArrival);
            Safe("hosts", _aHosts);
            Safe("stalker", _aStalker);
            Safe("voices", _aVoices);
            Safe("awakening", _aWake);
            Safe("wakeacts", _aWakeActs);
            WakeVoiceStep();   // the waking say their own lines oddly (WestworldScript.WakeVoice.cs)
            AnimTestStep();    // the Testing page's animation rows, while one runs (WestworldScript.AnimTest.cs)
            GunshotTestStep(); // and its gunshot row (WestworldScript.Gunshot.cs)
            Safe("scenes", _aDrama);
            Safe("court thoughts", _aCourtThoughts);
            PassingStep(now);   // people who know each other acknowledge it as they pass (WestworldScript.Passing.cs)
            SightsStep(now);    // people notice how you look (WestworldScript.Sights.cs)
            GrudgesStep(now);   // someone grieving one you killed (WestworldScript.Grudges.cs)
            MindShadowStep(now);   // the mind in shadow mode: traits, needs, feelings and mood, reported only (WestworldScript.Mind.cs)
            if (_drinkVoice != 0 || _drunkVoices.Count > 0)
                Safe("vices", _aDrinkVoice ?? (_aDrinkVoice = () => { DrinkVoiceStep(_now); SoberVoices(false); }));   // a drinker's voice, and sober again at 4 (WestworldScript.Vices.cs)
            Safe("ceremonies", _aCeremony);
            Safe("kin", _aCast);
            Safe("pickups", _aPickups);
        }
    }
}
