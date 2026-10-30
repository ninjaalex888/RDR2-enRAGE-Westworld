// The RealNPCs settings panel: every RealNPCs setting and every enRAGE setting on one larger, easier-to-read panel.
// Pages: West World, Stalkers and the rest of RealNPCs first, then Hotkeys (every key of RealNPCs and of enRAGE, in
// one place), then enRAGE (all of NemesisMod.ini on one page, section by section under small headings). A setting
// whose master switch is off is greyed out and can't be changed until the switch is on. Changes are written to the
// ini at once (only that one value; see IniDoc) and take effect: RealNPCs reloads its settings, and enRAGE is asked
// to reload its own. If enRAGE corrects a value it doesn't accept, the panel shows what it kept. NemesisMod.ini is
// copied to RealNPCs\backup once per session before the first change.
// Keys: Up / Down choose (Shift: the next section), Left / Right change (Shift: ten steps), Q / E or Page Up / Down
// turn the page, Tab jumps between RealNPCs and enRAGE, Enter sets a key, Backspace or Escape closes.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using EnrageWestworld.Core;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    internal sealed class SettingsPanel
    {
        private sealed class Row
        {
            public IniEntry E;        // null for a section heading or an action
            public string Heading;
            public bool Theirs;       // a line of NemesisMod.ini, not of RealNPCs.ini
            public bool Hookups;      // a line of CasualHookups.ini (another mod's, in the game folder)
            public string Label, Help;   // our own words for another mod's setting
            public string Action, ActionLabel, ActionAsk;   // a row that does something on Enter, after a second Enter
            public bool Fire;         // a Testing row (Core/TestActions.cs): Enter sets its key, as on every key row; Space fires it now and closes the panel (a gamepad's A fires it)
            public bool Pad;          // a controller binding ([Controller]): A or Enter waits for a button held and another pressed
        }

        private static bool Pickable(Row r)
        {
            return r.E != null || r.Action != null;
        }

        private sealed class Page
        {
            public string Title;
            public bool Enrage;
            public string Message;   // a page with nothing to change, only this to say
            public List<Row> Rows = new List<Row>();
        }

        // the pad's menu buttons (used only while a pad is the last thing touched, so keys don't count twice)
        private const uint PadUp = 1662638961u, PadDown = 97156178u, PadLeft = 2791226036u, PadRight = 3736290067u, PadLB = 3901091606u, PadRB = 398377320u,
                           PadAccept = 3350541322u, PadCancel = 359624985u, PadX = 1840825903u;
        private const uint WheelUp = 1652558994u, WheelDown = 2346611779u;   // the mouse wheel (the game's CursorScrollUp / CursorScrollDown)
        private const int WriteAfterMs = 450;   // holding Left or Right: written once it settles, not on every step

        // text sizes: about half as large again as enRAGE's own panel
        private const float TitleScale = 0.46f, PageScale = 0.38f, RowScale = 0.285f, SmallScale = 0.245f;
        private const float X = 0.16f, Y = 0.065f, W = 0.68f, H = 0.87f;
        private const int VisibleRows = 16;
        private const int PanelBudget = 120;

        private static readonly string[] OurOrder = { "WestWorld", "Stalkers", "Encounters", "Quiet", "Features", "World", "Scenes", "Romance", "Mind", "Ticker", "Display", "Pacing", "Output" };
        private static readonly Dictionary<string, string> OurTitles = new Dictionary<string, string>
        {
            { "WestWorld", "West World" }, { "Stalkers", "Stalkers" }, { "Encounters", "Encounters" }, { "Quiet", "Quiet (enRAGE)" }, { "Features", "People" }, { "World", "The world" }, { "Scenes", "Street scenes" },
            { "Romance", "Romance" }, { "Mind", "The mind" }, { "Ticker", "The ticker" }, { "Display", "Names, bars and meter" }, { "Pacing", "Pacing" }, { "Output", "Notices and the panel" }
        };

        // a setting that only matters while another is on
        private static readonly Dictionary<string, string> Needs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "FlirtPrompt", "Romance" }, { "Trysts", "Romance" }, { "CasualHookupsDecides", "Trysts" }, { "WillingSingle", "Romance" }, { "WillingTaken", "Romance" }, { "NpcFlirting", "Drama" }, { "AwakeningDays", "AwakeningShare" }, { "AwakeningBreakdown", "AwakeningShare" }, { "AwakeningDespair", "AwakeningShare" },
            { "RealityChance", "HostsQuestionReality" }, { "ReturnChance", "Reveries" }, { "StrangerReturnChance", "Reveries" },
            { "DramaGapSeconds", "Drama" }, { "AvengerChance", "Avengers" }, { "AvengersAtOnce", "Avengers" }, { "AvengerGapHours", "Avengers" }, { "QuietStillSeconds", "QuietWhenStill" },
            { "CryingWomanChance", "CryingWoman" }, { "ExecutionChance", "Executions" }, { "HermitChance", "Hermits" }, { "GatheringChance", "Gatherings" },
            { "Hermits", "WestWorld" }, { "Gatherings", "WestWorld" }, { "RegularsPerTown", "Regulars" }, { "RegularsDress", "Regulars" },
            { "NameCardsMetres", "NameCards" }, { "NameCardsKnownMetres", "NameCards" }, { "NameCardsBesideEnrage", "NameCards" }, { "NameCardsSecondLine", "NameCards" },
            { "ScenesOnTickerMetres", "ScenesOnTicker" },
            { "FeelingBarsMetres", "FeelingBars" }, { "FeelingBarsShowEmpty", "FeelingBars" }, { "RealVoiceTalkSeconds", "RealVoiceTalk" }, { "CrudeTalk", "RealVoiceTalk" }, { "Reactions", "Drama" }, { "EnragePeopleJoinIn", "Drama" }, { "ThoughtsOverHeadScale", "ThoughtsOverHeads" }, { "RoughHandling", "Drama" }, { "NpcThieves", "Drama" }, { "DrunkChallenges", "Drama" }        };

        // sections whose first setting switches the whole section on or off
        private static readonly Dictionary<string, string> Masters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "WestWorld", "WestWorld" }, { "Stalkers", "Stalkers" }, { "Ticker", "Ticker" }, { "Encounters", "Encounters" }
        };

        private static readonly Dictionary<string, string[]> Choices = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "StalkerWhenStill", new[] { "mix", "eerie", "circle", "watch", "rush" } },
            { "NewsTo", new[] { "corner", "ticker", "gamefeed" } },
            { "Subtitles", new[] { "game", "ours", "both", "off" } },
            { "NameCards", new[] { "off", "known", "everyone" } },
            { "WarNewsTo", new[] { "feed", "corner", "ticker" } },
            { "TownPostsAfterDeath", new[] { "survive", "successor" } }
        };

        private static readonly HashSet<string> OurKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "TestKey", "FlirtKey", "WakeKey", "ReaderKey", "SettingsKey", "SettingsKey2", "TickerNextKey", "StalkerKey", "ExecutionKey", "CryingWomanKey", "HermitKey", "GatheringKey" };

        // the order RealNPCs' keys are listed in on the Hotkeys page
        private static readonly string[] KeyOrder = { "ReaderKey", "FlirtKey", "TickerNextKey", "TestKey", "SettingsKey", "SettingsKey2" };

        // the keys that make something happen now are the Testing page's (Core/TestActions.cs, [Testing]); Delete leaves
        // one of those, or the second settings key, without a key
        private static bool IsTestKey(string key)
        {
            return TestActions.ByKey(key) != null;
        }

        // [Controller]: a controller's buttons for our keys (WestworldScript.Pad.cs), each with its own words on the Hotkeys page
        private static readonly Dictionary<string, string> PadRows = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "PadSettings", "Open these settings" }, { "PadStatus", "Status (as F7)" }, { "PadLetters", "Letters" }, { "PadTickerNext", "Next ticker line" }
        };
        private static readonly string[] PadHolds = { "LB", "RB", "LT", "RT", "LS", "RS", "Select" };
        private List<KeyValuePair<string, uint>> _padButtons;
        private Row _padCapture;
        private bool _padCaptureArmed;

        private static bool IsOurKey(string key)
        {
            return OurKeys.Contains(key) || IsTestKey(key);
        }

        private static bool Clearable(string key)
        {
            return IsTestKey(key) || "SettingsKey2".Equals(key, StringComparison.OrdinalIgnoreCase);
        }

        private static readonly Dictionary<string, string> Labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "WestWorld", "West World (master switch)" }, { "Stalkers", "Stalkers (master switch)" }, { "Ticker", "Ticker (on or off)" },
            { "LawChases", "A lawman runs a pickpocket down" }, { "Lifts", "A thief lifts your money in town" },
            { "ErrandPace", "Walking pace on errands (1 walk, 2 brisk, 3 run)" },
            { "ScenesOnTicker", "What people say near you, as subtitles" }, { "ScenesOnTickerMetres", "For people within (metres)" },
            { "TickerY", "Ticker height on screen" }, { "TickerGapSeconds", "Pause between thoughts (seconds)" }, { "Subtitles", "Subtitles for what's said to you" }, { "TroubleWeight", "Fights and quarrels: how often (1 as built)" }, { "MindLevels", "The mind's work spread by distance (in testing)" }, { "QuarrelLadder", "Quarrels climb or cool by the ladder (with MindLive)" }, { "ThoughtVariety", "Thoughts vary: no frame twice an hour, sources take turns" }, { "Language", "Thoughts worded by a local model (with MindLive; in testing)" }, { "CastRelativesSpawn", "Kin made out of sight when the street has nobody free" }, { "LanguageUrl", "The local model server" }, { "LanguageModel", "The model it runs" }, { "Campfires", "Strangers at a campfire at night" }, { "CampfireChance", "Campfire chance (percent a roll)" }, { "VoiceBelow", "The voice below the saloon floor (West World)" }, { "DoctorsPatient", "Someone walks to the doctor for a child's fever" }, { "TestCastReturns", "The test cast come back by themselves in their town" }, { "FlirtPrompt", "Flirt in the lock-on menu (H)" }, { "NpcFlirting", "People flirt and court each other" }, { "TerritoryNews", "enRAGE's territory changes as updates" }, { "RealVoiceTalk", "People talk in their real voices" }, { "RealVoiceTalkSeconds", "About every (seconds)" }, { "CrudeTalk", "Crude remarks (saloons, sweethearts)" }, { "RoughHandling", "Faces into the bar (rough handling)" }, { "Reactions", "People react to what's done and said to them" }, { "Acknowledge", "People who know each other nod as they pass" }, { "SightReactions", "People react to how you look" }, { "Kindnesses", "Small kindnesses (a hand up, a round, standing up)" }, { "Vices", "Vices (drinking away bad news)" }, { "Grudges", "Grudges that travel (friends take up a wrong)" }, { "NightVisitor", "The night visitor (in town, after midnight)" }, { "NightVisitorChance", "Night visitor chance per roll (percent)" }, { "Travellers", "Travellers asking the way (on the roads)" }, { "TravellerChance", "Traveller chance per roll (percent)" }, { "SupperInvitation", "The supper invitation (at dusk, on the roads)" }, { "SupperChance", "Supper invitation chance per roll (percent)" }, { "RecordMinds", "enRAGE's people think from its own record" }, { "StalkerRecords", "enRAGE's people may be the stalker" }, { "RecordFates", "Deaths RealNPCs caused written into enRAGE" }, { "EnragePeopleJoinIn", "enRAGE's own people join in" }, { "NpcThieves", "Pickpockets among townsfolk" }, { "DrunkChallenges", "Drunks who call men out" }, { "ThoughtMarker", "Mark who is thinking" }, { "ThoughtsOverHeads", "Thoughts above their heads" }, { "ThoughtsOverHeadScale", "Size of thoughts above heads" }, { "ShareWithEnrage", "Tell enRAGE of deaths it missed" }, { "Minds", "People think about what's on their minds" }, { "MindShadow", "The mind underneath, reported only (log, F7, cards)" }, { "MindLive", "The mind's moods reach the screen" }, { "PerfMeter", "Time each part of RealNPCs (F7: TEST perf)" }, { "DebugLog", "Detailed log (for testing)" }, { "ShowMinds", "Show minds (a card over people near you)" }, { "ShowWakeStages", "Wake stages over heads" }, { "WakeVoices", "The waking speak their own lines oddly" }, { "EncountersAtOnce", "Encounters going on at once (a stalker too)" }, { "PanelSlowsTime", "Slow the world while this panel is open" }, { "PanelTimeScale", "How slow (1 normal, 0.25 a quarter)" }, { "Trysts", "A night together (off screen)" }, { "CasualHookupsDecides", "CasualHookups only for women who said yes" }, { "WillingSingle", "Single women willing (percent)" }, { "WillingTaken", "Married women willing (percent)" }, { "ThoughtSounds", "A small sound as a thought shows" }, { "NpcRomance", "NPC romance" }, { "RevealToEnrage", "Tell enRAGE what you learn" },
            { "RomanceAffectsEnrage", "Romance changes enRAGE's records" }, { "ReplaceEnrageSettings", "Alt+F11 opens this panel" },
            { "HostsQuestionReality", "People question reality" }, { "StalkerWhenStill", "When you stand still, the stalker" }, { "StalkerActMin", "He does odd things at least (metres)" }, { "StalkerActMax", "... and at most (metres)" },
            { "StalkerKey", "Make a stalker now" }, { "ExecutionKey", "Stage an execution nearby" }, { "CryingWomanKey", "Bring out the crying woman" }, { "HermitKey", "Make a hermit's camp nearby" }, { "GatheringKey", "Stand a gathering nearby" }, { "StalkerEveryMinutes", "Roll for a stalker every (real minutes)" },
            { "StalkerChance", "Chance on each roll (percent)" }, { "StalkerNearbyMetres", "Turn someone within (metres)" },
            { "StalkerStrangers", "A stranger if nobody near" },
            { "QuietWhenStill", "Hold enRAGE while you stand still" }, { "QuietStillSeconds", "Standing still for (seconds)" },
            { "QuietInCinematic", "Hold enRAGE in the cinematic camera" }, { "TownPosts", "One person at each counter, for good" },
            { "Regulars", "Each town keeps its own people" }, { "RegularsPerTown", "Regulars in each town" }, { "RegularsDress", "Regulars wear their own clothes" }, { "TownPostsAfterDeath", "When a counter keeper is shot down" },
            { "SelfDefenceNoBounty", "No bounty for self-defence" },
            { "NameCards", "Names over heads (off, known, everyone)" }, { "NameCardsMetres", "Strangers' names within (metres)" }, { "NameCardsKnownMetres", "Known people's names within (metres)" }, { "NameCardsBesideEnrage", "Ours beside enRAGE's own cards too" }, { "NameCardsSecondLine", "Personality line under the names" },
            { "FeelingBars", "Bars under people: fear, liking, anger" }, { "FeelingBarsMetres", "For people within (metres)" }, { "FeelingBarsShowEmpty", "Show the empty part of each bar" }, { "GrudgeMeter", "Grudge meter (top right)" },
            { "Encounters", "Encounters (master switch)" }, { "EncounterEveryMinutes", "Roll for an encounter every (real minutes)" },
            { "CryingWoman", "The crying woman" }, { "CryingWomanChance", "Her chance on each roll (percent)" }, { "Executions", "Executions on the road" }, { "ExecutionChance", "An execution's chance on each roll (percent)" },
            { "Hermits", "Hermits who woke" }, { "HermitChance", "A new camp's chance (percent)" },
            { "Gatherings", "Gatherings of the awake" }, { "GatheringChance", "A gathering's chance (percent)" },
            { "ReaderKey", "Letters, diaries and clippings" }, { "FlirtKey", "Flirt" }, { "WakeKey", "Wake the person you face" },
            { "TickerNextKey", "Next line on the ticker" }, { "TestKey", "Status line and log dump" }, { "SettingsKey", "Open or close this panel" }, { "SettingsKey2", "... or this key" }
        };

        private readonly Func<Bridge> _bridgeOf;
        private Bridge _bridge { get { return _bridgeOf(); } }   // enRAGE may be found after the panel is made
        private readonly string _oursPath, _backupDir;
        private readonly Func<string, string> _applyOurs;   // reloads RealNPCs' settings; gives the value now in force for a key
        private readonly Action _useEnragePanel;           // hands over to enRAGE's own panel (for the keys it rebinds itself)
        private readonly Func<string, string> _runAction;   // an action row's work, by its id; returns what to say
        private Row _confirm;                               // an action row waiting for its second Enter
        private readonly List<Page> _pages = new List<Page>();
        private IniDoc _ours, _theirs, _hookups;
        private string _theirsPath;
        private bool _backedUp, _hookupsBackedUp;

        /// CasualHookups.ini when CasualHookups.asi is installed (set before Show), else null.
        public string HookupsIniPath;

        // CasualHookups' own settings (another mod), on the Romance page in our own words; its three chances add up to 100
        private const string HookupsG = "INPUT_INTERACT_OPTION1", HookupsB = "INPUT_INTERACT_HORSE_BRUSH";
        private static readonly string[] HookupsChances = { "notInterested", "givesInImmediately", "needsSecondFlirt" };
        private static readonly string[][] HookupsRows =
        {
            new[] { "PromptKey", "Its Flirt key", "The key for its Flirt in the lock-on menu: B, or G as it comes. enRAGE's lock-on prompts use G too, so with G both answer it, and RealNPCs can't hold it for women who haven't said yes." },
            new[] { "notInterested", "She turns you down (percent)", "Its own chance that she refuses outright. With RealNPCs deciding, 0: she has already said yes to you." },
            new[] { "givesInImmediately", "She says yes at once (percent)", "Its own chance that it starts at the first try. The three chances add up to 100, and the panel keeps them that way." },
            new[] { "needsSecondFlirt", "She wants asking twice (percent)", "Its own chance that she needs a second try first." },
            new[] { "startingPosition", "Position it starts in (1-3)", "Which of its three positions it opens with, numbered as in its own ini." },
            new[] { "enableVocalizations", "Her voice during it", "Her sounds during the scene." },
            new[] { "femaleStaysNaked", "She stays undressed after", "Off: her clothes come back when it's over." },
            new[] { "invincibleWoman", "She can't be hurt after", "Off: she can be harmed once it's over." },
            new[] { "agedTextures", "Older women look their age", "Undressed, older or sickly women look it." },
            new[] { "playerStaysNaked", "You stay undressed after", "On: you dress again at your clothes, with its Restore Outfit." },
            new[] { "playerStaysClothed", "You never undress", "For trouble with missing arms or legs after." },
            new[] { "skipPlayerRelaxing", "Skip your rest after", "On: straight back to the game when it's over. She still rests, and can still be talked to." },
        };
        private int _page, _row, _scroll;
        private string _note = string.Empty;
        private Row _capture;
        private Row _pendingRow;
        private string _pendingValue;
        private int _pendingAt;

        public bool Open { get; private set; }
        /// Waiting for the key to set a hotkey to: every key is that key, even the one that opens and closes the panel.
        public bool Capturing { get { return _capture != null; } }

        public SettingsPanel(Func<Bridge> bridge, string oursPath, string backupDir, Func<string, string> applyOurs, Action useEnragePanel, Func<string, string> runAction = null)
        {
            _bridgeOf = bridge;
            _oursPath = oursPath;
            _backupDir = backupDir;
            _applyOurs = applyOurs;
            _useEnragePanel = useEnragePanel;
            _runAction = runAction;
        }

        // ---- opening and closing ------------------------------------------------------------------------

        public void Show(string enrageIniPath)
        {
            _theirsPath = enrageIniPath;
            Open = true;
            _capture = null;
            _padCapture = null;
            _note = string.Empty;
            Draw.FrameBudget = PanelBudget;
            Reload();
            _page = Math.Max(0, Math.Min(_page, _pages.Count - 1));
            _row = _pages.Count > 0 ? Selectable(_pages[_page], 0) : 0;
            _scroll = 0;
        }

        public void Close()
        {
            Flush();
            Open = false;
            _capture = null;
            _padCapture = null;
            Draw.FrameBudget = Draw.NormalBudget;
        }

        private void Reload()
        {
            string keepTitle = _pages.Count > 0 && _page < _pages.Count ? _pages[_page].Title : null;
            _pages.Clear();
            Page keys = new Page { Title = "Hotkeys" };
            Page testing = new Page { Title = "Testing" };
            _ours = IniDoc.Read(_oursPath, true);
            if (_ours.Error == null)
            {
                IEnumerable<IniSection> ordered = OurOrder.Select(n => _ours.Sections.FirstOrDefault(x => x.Name.Equals(n, StringComparison.OrdinalIgnoreCase))).Where(s => s != null)
                    .Concat(_ours.Sections.Where(x => x.Name.Length > 0 && !OurOrder.Contains(x.Name, StringComparer.OrdinalIgnoreCase)));
                foreach (IniSection s in ordered)
                {
                    if (s.Name.Equals("Testing", StringComparison.OrdinalIgnoreCase) || s.Name.Equals("Controller", StringComparison.OrdinalIgnoreCase))
                        continue;   // the Testing page is built from its catalogue, below; the controller's buttons are on the Hotkeys page
                    // the keys are all on the Hotkeys page instead
                    List<Row> rows = s.Entries.Where(e => !IsOurKey(e.Key)).Select(e => new Row { E = e }).ToList();
                    if (rows.Count > 0)
                        _pages.Add(new Page { Title = OurTitles.ContainsKey(s.Name) ? OurTitles[s.Name] : s.Name, Rows = rows });
                }
                List<IniEntry> ourKeys = KeyOrder.Select(k => _ours.FindKey(k)).Where(e => e != null)
                    .Concat(_ours.Sections.SelectMany(s => s.Entries).Where(e => OurKeys.Contains(e.Key) && !KeyOrder.Contains(e.Key, StringComparer.OrdinalIgnoreCase) && !IsTestKey(e.Key)))
                    .ToList();
                if (ourKeys.Count > 0)
                {
                    keys.Rows.Add(new Row { Heading = "RealNPCs" });
                    keys.Rows.AddRange(ourKeys.Select(e => new Row { E = e }));
                }
                IniSection padSection = _ours.Sections.FirstOrDefault(x => x.Name.Equals("Controller", StringComparison.OrdinalIgnoreCase));
                if (padSection != null && padSection.Entries.Count > 0)
                {
                    keys.Rows.Add(new Row { Heading = "RealNPCs on a controller" });
                    foreach (IniEntry e in padSection.Entries)
                    {
                        string label;
                        if (PadRows.TryGetValue(e.Key, out label))
                            keys.Rows.Add(new Row { E = e, Pad = true, Label = label,
                                                    Help = "A (or Enter) sets it: hold LB, RB, a trigger or a stick and press another button. B keeps it; Y leaves it with none. Everything on the Testing page fires from there with A." });
                        else
                            keys.Rows.Add(new Row { E = e });
                    }
                }
                // Testing: one row per action in the catalogue, by group. Enter fires it now (the panel closes, it fires a
                // second later); Right or Left sets its key, Delete leaves it without one
                foreach (string group in TestActions.Groups)
                {
                    List<Row> rows = TestActions.All.Where(a => a.Group == group).Select(a => new { a, e = _ours.FindKey(a.KeySetting) }).Where(x => x.e != null)
                        .Select(x => new Row { E = x.e, Fire = true, Action = "fire:" + x.a.Id, ActionLabel = x.a.Label,
                                               Help = (x.a.Needs != null ? "Needs: " + x.a.Needs + ". " : string.Empty) + "Enter fires it now (the panel closes first). Right sets its key; Delete leaves it without one." })
                        .ToList();
                    if (rows.Count == 0)
                        continue;
                    testing.Rows.Add(new Row { Heading = group });
                    testing.Rows.AddRange(rows);
                }
                IniEntry master = _ours.FindKey("TestKeys"), minds = _ours.FindKey("ShowMinds"), stages = _ours.FindKey("ShowWakeStages");
                if (master != null || minds != null || stages != null)
                {
                    testing.Rows.Add(new Row { Heading = "Switches" });
                    if (master != null)
                        testing.Rows.Add(new Row { E = master });
                    if (minds != null)
                        testing.Rows.Add(new Row { E = minds });
                    if (stages != null)
                        testing.Rows.Add(new Row { E = stages });
                }
                if (_runAction != null)
                {
                    testing.Rows.Add(new Row { Heading = "Memory" });
                    testing.Rows.Add(new Row { Action = "wipe-memories", ActionLabel = "Wipe every NPC's memory",
                                               Help = "Forget everything RealNPCs keeps of the people in this world, for this character. A copy of the save is kept; enRAGE's memory stays. Enter, then Enter again.",
                                               ActionAsk = "Wipe everything RealNPCs remembers of the people in this world (who they are, what they know and feel about you, their ties, their deaths)? enRAGE's own memory stays. A copy of the save is kept. Press Enter again to wipe; any other key keeps it all." });
                }
                // CasualHookups' settings with ours on romance, when it's installed
                _hookups = !string.IsNullOrEmpty(HookupsIniPath) && File.Exists(HookupsIniPath) ? IniDoc.Read(HookupsIniPath, false) : null;
                Page romance = _pages.FirstOrDefault(p => p.Title == OurTitles["Romance"]);
                if (_hookups != null && _hookups.Error == null && romance != null)
                {
                    List<Row> rows = HookupsRows.Select(d => new { d, e = _hookups.FindKey(d[0]) }).Where(x => x.e != null)
                        .Select(x => new Row { E = x.e, Hookups = true, Label = x.d[1], Help = x.d[2] }).ToList();
                    if (rows.Count > 0)
                    {
                        romance.Rows.Add(new Row { Heading = "CasualHookups (another mod)" });
                        romance.Rows.AddRange(rows);
                    }
                }
            }
            _theirs = !string.IsNullOrEmpty(_theirsPath) && File.Exists(_theirsPath) ? IniDoc.Read(_theirsPath, false) : null;
            // enRAGE's settings only when we know which of its entries are its own bookkeeping, and can ask it to reload
            string why = _bridge == null ? "enRAGE hasn't been found yet (it takes a few seconds after loading a save)."
                : !_bridge.Present ? "enRAGE isn't installed (RealNPCs is standing alone)."
                : !_bridge.EnrageHidesKnown || !_bridge.CanReloadEnrage ? "this version of enRAGE isn't laid out the way RealNPCs expects (the log says what was missing). Its own panel still works: set ReplaceEnrageSettings to false."
                : _theirs == null || _theirs.Error != null ? "enRAGE's settings didn't load from its ini file" + (_theirs != null && _theirs.Error != null ? ": " + _theirs.Error : ".") : null;
            Page enrage = new Page { Title = "enRAGE", Enrage = true };
            if (why != null)
                enrage.Message = "enRAGE's settings can't be shown right now: " + why;
            else
            {
                // all of NemesisMod.ini on one page, each section under its own heading; its keys on the Hotkeys page
                foreach (IniSection s in _theirs.Sections.Where(x => x.Name.Length > 0))
                {
                    List<IniEntry> entries = s.Entries.Where(e => _bridge == null || !_bridge.EnrageHides(s.Name, e.Key)).ToList();
                    if (entries.Count == 0)
                        continue;
                    bool isKeys = s.Name.Equals("Keys", StringComparison.OrdinalIgnoreCase);
                    Page into = isKeys ? keys : enrage;
                    into.Rows.Add(new Row { Heading = isKeys ? "enRAGE" : s.Name });
                    into.Rows.AddRange(entries.Select(e => new Row { E = e, Theirs = true }));
                }
            }
            if (keys.Rows.Count > 0)
                _pages.Add(keys);
            if (testing.Rows.Count > 0)
                _pages.Add(testing);
            _pages.Add(enrage);
            if (keepTitle != null)
            {
                int i = _pages.FindIndex(p => p.Title == keepTitle);
                if (i >= 0)
                    _page = i;
            }
            _page = Math.Max(0, Math.Min(_page, _pages.Count - 1));
            if (_pages.Count > 0)
                _row = Selectable(_pages[_page], _row);
        }

        // ---- moving about ------------------------------------------------------------------------------

        /// The nearest row at or after i that can be chosen (headings can't), wrapping round.
        private static int Selectable(Page page, int i)
        {
            int n = page.Rows.Count;
            if (n == 0)
                return 0;
            i = Math.Max(0, Math.Min(i, n - 1));
            for (int k = 0; k < n; k++)
            {
                int j = (i + k) % n;
                if (Pickable(page.Rows[j]))
                    return j;
            }
            return 0;
        }

        /// The next row that can be chosen, up or down, wrapping round.
        private static int Step(Page page, int i, int dir)
        {
            int n = page.Rows.Count;
            for (int k = 1; k <= n; k++)
            {
                int j = ((i + dir * k) % n + n) % n;
                if (Pickable(page.Rows[j]))
                    return j;
            }
            return i;
        }

        /// The first setting of the next section (dir 1) or of this one, then the one before (dir -1), wrapping round.
        private static int Section(Page page, int i, int dir)
        {
            List<int> heads = Enumerable.Range(0, page.Rows.Count).Where(j => page.Rows[j].Heading != null).ToList();
            if (heads.Count == 0)
                return dir > 0 ? Selectable(page, page.Rows.Count - 1) : Selectable(page, 0);
            int cur = heads.FindLastIndex(h => h < i);   // the section this row is in (-1: before the first heading)
            int target;
            if (dir > 0)
            {
                int nx = heads.FindIndex(h => h > i);
                target = nx >= 0 ? heads[nx] : heads[0];
            }
            else if (cur >= 0 && i > Selectable(page, heads[cur] + 1))
                target = heads[cur];   // back to the top of this section first
            else
                target = heads[((cur < 0 ? 0 : cur) - 1 + heads.Count) % heads.Count];
            return Selectable(page, target + 1);
        }

        // ---- keys -------------------------------------------------------------------------------------

        public void Key(Keys key, bool shift, bool ctrl, bool alt)
        {
            if (!Open)
                return;
            if (_capture != null)
            {
                Capture(key, shift, ctrl, alt);
                return;
            }
            if (_padCapture != null)
            {
                if (key == Keys.Escape || key == Keys.Back)
                {
                    _note = Name(_padCapture) + " stays " + _padCapture.E.Value + ".";
                    _padCapture = null;
                }
                else if (key == Keys.Delete)
                {
                    Row pr = _padCapture;
                    _padCapture = null;
                    PadWrite(pr, "None");
                }
                return;   // the buttons come from the controller (PadCaptureStep)
            }
            if (_confirm != null && key != Keys.Enter)
            {
                _confirm = null;   // anything but a second Enter keeps it all
                _note = "Nothing was wiped.";
                if (key == Keys.Back || key == Keys.Escape)
                    return;
            }
            if (key == Keys.Back || key == Keys.Escape)
            {
                Close();
                return;
            }
            if (_pages.Count == 0)
                return;
            bool changing = key == Keys.Left || key == Keys.A || key == Keys.Right || key == Keys.D;
            if (!changing)
                Flush();   // moving on: a value still settling is written first
            Page page = _pages[_page];
            bool any = page.Rows.Any(Pickable);
            if (!any && key != Keys.Q && key != Keys.E && key != Keys.PageUp && key != Keys.PageDown && key != Keys.Tab)
                return;
            switch (key)
            {
                case Keys.Up:
                case Keys.W:
                    _row = shift ? Section(page, _row, -1) : Step(page, _row, -1);
                    break;
                case Keys.Down:
                case Keys.S:
                    _row = shift ? Section(page, _row, 1) : Step(page, _row, 1);
                    break;
                case Keys.Q:
                case Keys.PageUp:
                    TurnTo((_page + _pages.Count - 1) % _pages.Count);
                    break;
                case Keys.E:
                case Keys.PageDown:
                    TurnTo((_page + 1) % _pages.Count);
                    break;
                case Keys.Tab:
                {
                    int target = page.Enrage ? 0 : _pages.FindIndex(p => p.Enrage);
                    if (target >= 0)
                        TurnTo(target);
                    break;
                }
                case Keys.Left:
                case Keys.A:
                    Change(page.Rows[_row], -1, shift);
                    break;
                case Keys.Right:
                case Keys.D:
                    Change(page.Rows[_row], 1, shift);
                    break;
                case Keys.Enter:
                    Enter(page.Rows[_row]);
                    break;
                case Keys.Space:
                    if (page.Rows[_row].Fire)
                        FireNow(page.Rows[_row]);
                    break;
                case Keys.Delete:
                    if (page.Rows[_row].Fire && page.Rows[_row].E != null)
                        Write(page.Rows[_row], "None");
                    break;
            }
        }

        /// Keys that move the player or the panel: never a key of ours unless with a modifier.
        private static bool MovesSomething(Keys key)
        {
            switch (key)
            {
                case Keys.Up: case Keys.Down: case Keys.Left: case Keys.Right: case Keys.W: case Keys.A: case Keys.S: case Keys.D:
                case Keys.Q: case Keys.E: case Keys.Tab: case Keys.PageUp: case Keys.PageDown: case Keys.Enter: case Keys.Space:
                    return true;
                default:
                    return false;
            }
        }

        private readonly Dictionary<string, string> _keyShown = new Dictionary<string, string>();

        /// A Testing row's key as shown: worked out once per value (never parsed each frame); an empty value is the
        /// action's default, as the game side reads it.
        private string KeyShown(Row r)
        {
            string v = r.E != null ? r.E.Value : null;
            if (string.IsNullOrWhiteSpace(v))
            {
                TestActions.Entry te = r.E != null ? TestActions.ByKey(r.E.Key) : null;
                v = te != null ? te.DefaultKey : "None";
            }
            string shown;
            if (!_keyShown.TryGetValue(v, out shown))
                _keyShown[v] = shown = Normal(v) ?? "no key";
            return shown;
        }

        /// The key a setting has now: its value, or for a Testing key left blank, its action's default.
        private static string KeyNow(IniEntry e)
        {
            if (!string.IsNullOrWhiteSpace(e.Value))
                return e.Value;
            TestActions.Entry te = TestActions.ByKey(e.Key);
            return te != null ? te.DefaultKey : "None";
        }

        /// Another of RealNPCs' keys already on this one (a blank Testing key counts as its default), or null.
        private IniEntry OurKeyHolder(string spec, IniEntry self)
        {
            string want = Normal(spec);
            if (want == null || _ours == null)
                return null;
            return _ours.Sections.SelectMany(x => x.Entries).FirstOrDefault(k => k != self && IsOurKey(k.Key) && Normal(KeyNow(k)) == want);
        }

        /// Whose key this already is, or null: another of RealNPCs' keys, or one of enRAGE's ([Keys] in NemesisMod.ini).
        private string KeyOwner(string spec, IniEntry self)
        {
            string want = Normal(spec);
            if (want == null)
                return null;
            if (_ours != null)
                foreach (IniEntry k in _ours.Sections.SelectMany(x => x.Entries))
                    if (k != self && IsOurKey(k.Key) && Normal(KeyNow(k)) == want)
                        return "RealNPCs' " + Label(k.Key);
            return EnrageKeyOwner(spec);
        }

        /// One of enRAGE's keyboard keys on this one ([Keys] in NemesisMod.ini, the ones it rebinds itself), or null; the
        /// telegraph key is meant to be shared (T: near a clerk, both happen), so only warned about after.
        private string EnrageKeyOwner(string spec)
        {
            string want = Normal(spec);
            IniSection theirs = want != null && _theirs != null ? _theirs.Sections.FirstOrDefault(x => x.Name.Equals("Keys", StringComparison.OrdinalIgnoreCase)) : null;
            if (theirs != null)
                foreach (IniEntry k in theirs.Entries)
                {
                    if (_bridge == null || !_bridge.EnrageRebinds("Keys", k.Key) || k.Key.IndexOf("Telegraph", StringComparison.OrdinalIgnoreCase) >= 0)
                        continue;
                    if (Normal(k.Value) == want)
                        return "enRAGE's " + Label(k.Key);
                }
            return null;
        }

        /// A key as KeySpec reads it ("Shift+F7"), or null for none.
        private static string Normal(string text)
        {
            if (string.IsNullOrWhiteSpace(text) || text.Trim().Equals("None", StringComparison.OrdinalIgnoreCase))
                return null;
            KeySpec k = KeySpec.Parse(text, Keys.None);
            return k.Key == Keys.None ? null : k.ToString();
        }

        private void TurnTo(int page)
        {
            _page = page;
            _row = Selectable(_pages[_page], 0);
            _scroll = 0;
            _note = string.Empty;
        }

        private void Enter(Row r)
        {
            if (r.Pad)
            {
                _padCapture = r;
                _padCaptureArmed = false;   // the A that started it counts for nothing until it's let go
                _note = "On the controller: hold LB, RB, a trigger or a stick, and press another button. B keeps " + r.E.Value + "; Y leaves it with none.";
                return;
            }
            if (r.Fire)
            {
                // Enter sets the key, as on every other key row (Lex, 2026-10-27: Enter closed the panel); a gamepad can't
                // set a keyboard key, so its A fires the test, as Space does
                if (r.E == null || !PAD.IS_USING_KEYBOARD_AND_MOUSE(0))
                {
                    FireNow(r);
                    return;
                }
                _capture = r;
                _note = "Waiting for a key: whatever you press next fires " + r.ActionLabel + " (Ctrl, Shift or Alt with it is fine). Escape keeps " + KeyShown(r) +
                        ". Delete leaves it without a key. Space fires it now.";
                return;
            }
            if (r.Action != null)
            {
                if (_confirm != r)
                {
                    _confirm = r;
                    _note = r.ActionAsk;
                    return;
                }
                _confirm = null;
                try
                {
                    _note = _runAction != null ? _runAction(r.Action) : "Nothing to do.";
                }
                catch (Exception ex)
                {
                    _note = "That didn't work: " + ex.Message;
                }
                return;
            }
            if (_confirm != null)
            {
                _confirm = null;
                _note = "Nothing was wiped.";
            }
            IniEntry e = r.E;
            if (e == null)
                return;
            string off = OffBecause(r);
            if (off != null)
            {
                _note = off;
                return;
            }
            if (!r.Theirs && IsOurKey(e.Key))
            {
                _capture = r;
                _note = "Waiting for a key: whatever you press next becomes " + Label(e.Key) + " (Ctrl, Shift or Alt with it is fine). Escape keeps " + e.Value + "." +
                        (Clearable(e.Key) ? " Delete leaves it without a key." : string.Empty);
                return;
            }
            if (r.Theirs && _bridge != null && _bridge.EnrageRebinds(e.Section, e.Key))
            {
                // enRAGE checks its keys against each other itself: let it
                Close();
                _useEnragePanel();
                return;
            }
            _note = "Left and Right change this one.";
        }

        /// A Testing row's test, now: it fires a second after the panel is gone.
        private void FireNow(Row r)
        {
            try
            {
                _note = _runAction != null ? _runAction(r.Action) : "Nothing to do.";
            }
            catch (Exception ex)
            {
                _note = "That didn't work: " + ex.Message;
                return;
            }
            Close();
        }

        private void Capture(Keys key, bool shift, bool ctrl, bool alt)
        {
            if (key == Keys.ShiftKey || key == Keys.ControlKey || key == Keys.Menu || key == Keys.LShiftKey || key == Keys.RShiftKey ||
                key == Keys.LControlKey || key == Keys.RControlKey || key == Keys.LMenu || key == Keys.RMenu)
                return;
            Row r = _capture;
            _capture = null;
            if (key == Keys.Escape || key == Keys.Back)
            {
                _note = Label(r.E.Key) + " stays " + KeyNow(r.E) + ".";
                return;
            }
            if (key == Keys.Delete && Clearable(r.E.Key))
            {
                Write(r, "None");   // KeySpec reads None as no key at all
                return;
            }
            if (key == Keys.Space && r.Fire && !ctrl && !shift && !alt)
            {
                FireNow(r);   // the note while waiting says Space fires it
                return;
            }
            if (!ctrl && !shift && !alt && MovesSomething(key))
            {
                _capture = r;   // still waiting: that one moves you or the panel
                _note = KeySpec.Name(key) + " moves you or the panel; press another key (with Ctrl, Shift or Alt if you like), or Escape.";
                return;
            }
            string spec = (ctrl ? "Ctrl+" : string.Empty) + (shift ? "Shift+" : string.Empty) + (alt ? "Alt+" : string.Empty) + KeySpec.Name(key);
            // another of RealNPCs' keys already on it: the key moves here and that one is left without (Lex, 2026-10-27: setting
            // a Testing row to [ was refused as the execution's); one that must keep a key (F7, the flirt key...) is never taken
            IniEntry holder = OurKeyHolder(spec, r.E);
            string enrage = EnrageKeyOwner(spec);
            if (holder != null && enrage == null)
            {
                if (!Clearable(holder.Key))
                {
                    _note = spec + " is " + Label(holder.Key) + ", which needs a key of its own: give it another first. " + Label(r.E.Key) + " stays " + KeyNow(r.E) + ".";
                    return;
                }
                TestActions.Entry held = TestActions.ByKey(holder.Key);
                string from = held != null ? held.Label : Label(holder.Key), fromKey = holder.Key;
                _ours.Set(holder, "None");   // saved with this one: Write saves the whole file and reloads the settings
                Write(r, spec);
                _note += "; taken from " + from + ", which has no key now";
                Log.Write("SETTINGS RealNPCs " + fromKey + " = None (its key, " + spec + ", went to " + r.E.Key + ")");
                return;
            }
            // one of enRAGE's keys is refused, naming it (its keys are enRAGE's to change)
            string owner = KeyOwner(spec, r.E);
            if (owner != null)
            {
                _note = spec + " is taken: " + owner + ". " + Label(r.E.Key) + " stays " + KeyNow(r.E) + ".";
                return;
            }
            Write(r, spec);
            // enRAGE hears every key too: say so if this one is also one of its own
            IniSection keys = _theirs != null ? _theirs.Sections.FirstOrDefault(s => s.Name == "Keys") : null;
            IniEntry same = keys != null && !ctrl && !shift && !alt ? keys.Entries.FirstOrDefault(k => k.Value.Equals(key.ToString(), StringComparison.OrdinalIgnoreCase)) : null;
            if (same != null)
                _note += ". Careful: enRAGE uses " + key + " too (" + Label(same.Key) + "), so both will happen.";
        }

        /// A controller binding being set: once every button is let go, a held shoulder button, trigger, stick or Select
        /// and another pressed make it. B alone keeps it; Y alone leaves it with none; a button alone is the game's.
        private void PadCaptureStep()
        {
            if (_padButtons == null)
                _padButtons = PadChord.Buttons.Select(n => new KeyValuePair<string, uint>(n, Natives.Hash(PadChord.ControlOf(n)))).ToList();
            if (!_padCaptureArmed)
            {
                _padCaptureArmed = !_padButtons.Any(b => PAD.IS_DISABLED_CONTROL_PRESSED(0, b.Value));
                return;
            }
            string held = PadHolds.FirstOrDefault(h => PAD.IS_DISABLED_CONTROL_PRESSED(0, _padButtons.First(b => b.Key == h).Value));
            string press = _padButtons.Where(b => b.Key != held && PAD.IS_DISABLED_CONTROL_JUST_PRESSED(0, b.Value)).Select(b => b.Key).FirstOrDefault();
            if (press == null)
                return;
            Row r = _padCapture;
            if (held == null)
            {
                if (press == "B")
                {
                    _padCapture = null;
                    _note = Name(r) + " stays " + r.E.Value + ".";
                }
                else if (press == "Y")
                {
                    _padCapture = null;
                    PadWrite(r, "None");
                }
                else if (!PadHolds.Contains(press))
                    _note = press + " alone is the game's: hold LB, RB, a trigger or a stick and press " + press + " with it. B keeps " + r.E.Value + ".";
                return;
            }
            _padCapture = null;
            PadWrite(r, PadChord.Name(held, press));
        }

        /// A controller binding saved; another of ours on the same buttons is left with none (as a key moves over).
        private void PadWrite(Row r, string chord)
        {
            IniEntry other = chord == "None" ? null : _ours.Sections.SelectMany(s => s.Entries)
                .FirstOrDefault(x => x != r.E && PadRows.ContainsKey(x.Key) && chord.Equals(PadChord.Normal(x.Value), StringComparison.OrdinalIgnoreCase));
            if (other != null)
                _ours.Set(other, "None");   // saved with this one
            Write(r, chord);
            if (other != null)
            {
                _note += "; taken from " + PadRows[other.Key] + ", which has no button now";
                Log.Write("SETTINGS RealNPCs " + other.Key + " = None (its buttons, " + chord + ", went to " + r.E.Key + ")");
            }
        }

        private void Change(Row r, int dir, bool big)
        {
            IniEntry e = r.E;
            if (e == null)
                return;
            string off = OffBecause(r);
            if (off != null)
            {
                _note = off;
                return;
            }
            if (r.Hookups)
            {
                string step = HookupsNext(e, dir, big);
                if (step != null)
                    Stage(r, step);
                return;
            }
            if (r.Pad)
            {
                _note = "A or Enter sets it from the controller.";
                return;
            }
            if (r.Fire)
            {
                // a Testing row has no value to step: Enter sets its key, Space fires it
                _note = PAD.IS_USING_KEYBOARD_AND_MOUSE(0) ? "Enter sets a key for " + r.ActionLabel + "; Space fires it now; Delete leaves it without a key."
                    : "A fires " + r.ActionLabel + " now; set a key for it with the keyboard (Enter).";
                return;
            }
            bool key = (!r.Theirs && IsOurKey(e.Key)) || (r.Theirs && _bridge != null && _bridge.EnrageRebinds(e.Section, e.Key));
            if (key)
            {
                _note = "Press Enter to set this key.";   // a key like 0 must not be stepped like a number
                return;
            }
            string next;
            string[] choice;
            if (!r.Theirs && Choices.TryGetValue(e.Key, out choice))
            {
                int i = Array.FindIndex(choice, c => c.Equals(e.Value, StringComparison.OrdinalIgnoreCase));
                next = choice[((i < 0 ? 0 : i) + dir + choice.Length) % choice.Length];
            }
            else
                next = IniDoc.Step(e.Value, dir, big);
            if (next == null)
            {
                _note = Label(e.Key) + " is text: type it in " + (r.Theirs ? "NemesisMod.ini" : "RealNPCs.ini") + ".";
                return;
            }
            Stage(r, next);
        }

        // ---- writing ----------------------------------------------------------------------------------------

        /// Shows the new value at once; it is written when it settles (no change for a moment), or on moving on.
        private void Stage(Row r, string value)
        {
            if (_pendingRow != null && _pendingRow.E != r.E)
                Flush();
            IniDoc doc = DocOf(r);
            if (doc == null || !doc.Set(r.E, value))
                return;
            _pendingRow = r;
            _pendingValue = value;
            _pendingAt = Environment.TickCount;
            _note = Name(r) + " = " + Shown(r, value);
        }

        /// Writes the value waiting to be written, if any.
        public void Flush()
        {
            if (_pendingRow == null)
                return;
            Row r = _pendingRow;
            string value = _pendingValue;
            _pendingRow = null;
            _pendingValue = null;
            Write(r, value);
        }

        /// Called every frame: writes a value once it has settled.
        public void Tick()
        {
            if (_pendingRow != null && unchecked(Environment.TickCount - _pendingAt) >= WriteAfterMs)
                Flush();
        }

        private IniDoc DocOf(Row r)
        {
            return r.Hookups ? _hookups : r.Theirs ? _theirs : _ours;
        }

        private void Write(Row r, string value)
        {
            if (r.Hookups)
            {
                WriteHookups(r, value);
                return;
            }
            IniEntry e = r.E;
            IniDoc doc = r.Theirs ? _theirs : _ours;
            if (doc == null || e == null)
                return;
            if (r.Theirs && !BackUp())
            {
                Reload();   // nothing was written: show the file as it is
                return;
            }
            if (!doc.Set(e, value))
            {
                _note = "Couldn't change " + Label(e.Key) + ".";
                return;
            }
            string err = doc.Save();
            if (err != null)
            {
                _note = "Couldn't save: " + err;
                Reload();
                return;
            }
            string kept;
            if (r.Theirs)
            {
                bool reloaded = _bridge != null && _bridge.ReloadEnrageSettings();
                Reload();
                IniEntry now = _theirs != null ? _theirs.Find(e.Section, e.Key) : null;
                kept = now != null ? now.Value : value;
                _note = !reloaded ? Label(e.Key) + " = " + value + " (saved; enRAGE picks it up when it next reads its settings)"
                    : kept == value ? Label(e.Key) + " = " + value : Label(e.Key) + ": enRAGE kept " + kept;
                Log.Write("SETTINGS enRAGE [" + e.Section + "] " + e.Key + " = " + kept);
                return;
            }
            kept = _applyOurs(e.Key);
            if (kept != null && !kept.Equals(value, StringComparison.OrdinalIgnoreCase))
            {
                // outside what RealNPCs can use: show (and keep) what it actually runs with
                doc.Set(e, kept);
                doc.Save();
                _applyOurs(e.Key);
                _note = Label(e.Key) + ": " + value + " is out of range, so it is " + kept;
            }
            else
                _note = Label(e.Key) + " = " + value;
            Reload();
            Log.Write("SETTINGS RealNPCs [" + e.Section + "] " + e.Key + " = " + (kept ?? value));
            if (e.Key.Equals("NameCards", StringComparison.OrdinalIgnoreCase) && string.Equals(kept ?? value, "off", StringComparison.OrdinalIgnoreCase))
                EnrageNameCardsOff();
        }

        /// Names over heads off hides enRAGE's own cards too: with enRAGE running, the cards a player sees are its, ours
        /// standing aside for them (Lex, 2026-10-30: "I set Names over heads to off but I still see the nameplates"). Its
        /// [Menu] NameCards to 0, the copy of NemesisMod.ini first, as any enRAGE row. Only this way: turning ours back on
        /// leaves enRAGE's off, and ours draw.
        private void EnrageNameCardsOff()
        {
            IniEntry theirs = _theirs != null && _theirs.Error == null ? _theirs.Find("Menu", "NameCards") : null;
            if (theirs == null || (theirs.Value ?? "").Trim() == "0")
                return;
            if (!BackUp())
            {
                _note = "Names over heads = off; enRAGE's own names stay on: " + _note;
                return;
            }
            string err = _theirs.Set(theirs, "0") ? _theirs.Save() : "couldn't change it";
            if (err != null)
            {
                _note = "Names over heads = off; couldn't turn off enRAGE's own: " + err;
                Reload();
                return;
            }
            bool reloaded = _bridge != null && _bridge.ReloadEnrageSettings();
            Reload();
            _note = "Names over heads = off, enRAGE's own too" + (reloaded ? "" : " (saved; enRAGE picks it up when it next reads its settings)");
            Log.Write("SETTINGS enRAGE [Menu] NameCards = 0 (RealNPCs' Names over heads turned off)");
        }

        /// A line of CasualHookups.ini: the three chances kept at 100 between them, a copy of the file first.
        private void WriteHookups(Row r, string value)
        {
            IniEntry e = r.E;
            if (_hookups == null || e == null)
                return;
            if (!BackUpHookups())
            {
                Reload();
                return;
            }
            if (!_hookups.Set(e, value))
            {
                _note = "Couldn't change " + Name(r) + ".";
                return;
            }
            string also = BalanceChances(e.Key);
            string err = _hookups.Save();
            if (err != null)
            {
                _note = "Couldn't save CasualHookups.ini: " + err;
                Reload();
                return;
            }
            _note = Name(r) + " = " + Shown(r, value) + also + ". CasualHookups may only read it when the game starts.";
            Log.Write("SETTINGS CasualHookups [" + e.Section + "] " + e.Key + " = " + value + also);
            Reload();
        }

        /// Its next value: B or G for its key, 1-3 for the position, 0-100 for a chance, true or false for the rest.
        private static string HookupsNext(IniEntry e, int dir, bool big)
        {
            string v = (e.Value ?? string.Empty).Trim();
            if (e.Key.Equals("PromptKey", StringComparison.OrdinalIgnoreCase))
                return v.Equals(HookupsB, StringComparison.OrdinalIgnoreCase) ? HookupsG : HookupsB;
            int n;
            if (e.Key.Equals("startingPosition", StringComparison.OrdinalIgnoreCase))
                return ((((int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out n) ? n : 1) - 1 + dir) % 3 + 3) % 3 + 1).ToString(CultureInfo.InvariantCulture);
            string next = IniDoc.Step(v, dir, big);
            if (next != null && HookupsChances.Contains(e.Key, StringComparer.OrdinalIgnoreCase) && int.TryParse(next, NumberStyles.Integer, CultureInfo.InvariantCulture, out n))
                next = Math.Max(0, Math.Min(100, n)).ToString(CultureInfo.InvariantCulture);
            return next;
        }

        /// Its ini says the three chances add up to 100: the changed one stays, and "yes at once" takes up the difference
        /// ("asking twice" when it was "yes at once" that changed), then the third if that isn't enough. Returns what else
        /// it changed, for the note.
        private string BalanceChances(string changed)
        {
            int i = Array.FindIndex(HookupsChances, k => k.Equals(changed, StringComparison.OrdinalIgnoreCase));
            if (i < 0)
                return "";
            IniEntry[] e = HookupsChances.Select(k => _hookups.FindKey(k)).ToArray();
            if (e.Any(x => x == null))
                return "";
            int[] v = e.Select(x => { int n; return int.TryParse(x.Value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out n) ? Math.Max(0, Math.Min(100, n)) : 0; }).ToArray();
            int a = i == 1 ? 2 : 1, b = 3 - i - a, rest = 100 - v[i];   // a "yes at once" takes up what the others give, first
            int na = rest - v[b], nb = v[b];
            if (na < 0)
            {
                na = 0;
                nb = rest;
            }
            string also = "";
            foreach (int j in new[] { a, b })
            {
                string want = (j == a ? na : nb).ToString(CultureInfo.InvariantCulture);
                if (e[j].Value.Trim() != want && _hookups.Set(e[j], want))
                    also += ", " + HookupsRows.First(d => d[0] == HookupsChances[j])[1].Replace(" (percent)", "") + " " + want;
            }
            return also.Length > 0 ? " (so" + also.Substring(1) + ")" : "";
        }

        /// One copy of CasualHookups.ini per session, before the first change.
        private bool BackUpHookups()
        {
            if (_hookupsBackedUp)
                return true;
            try
            {
                Directory.CreateDirectory(_backupDir);
                string to = Path.Combine(_backupDir, "CasualHookups " + DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss", CultureInfo.InvariantCulture) + ".ini");
                File.Copy(HookupsIniPath, to, false);
                _hookupsBackedUp = true;
                Log.Write("SETTINGS backed up CasualHookups.ini to " + to);
                return true;
            }
            catch (Exception ex)
            {
                _note = "Couldn't back up CasualHookups.ini first, so nothing was changed: " + ex.Message;
                return false;
            }
        }

        /// A row's name on the panel: ours from Labels, another mod's in our own words.
        private static string Name(Row r)
        {
            return r.Label ?? Label(r.E.Key);
        }

        /// A value as shown: CasualHookups' key by the key it is.
        private static string Shown(Row r, string value)
        {
            if (r.Hookups && r.E.Key.Equals("PromptKey", StringComparison.OrdinalIgnoreCase))
                return value.Trim().Equals(HookupsB, StringComparison.OrdinalIgnoreCase) ? "B" : value.Trim().Equals(HookupsG, StringComparison.OrdinalIgnoreCase) ? "G" : value;
            return value;
        }

        /// One copy of NemesisMod.ini per session, before the first change.
        private bool BackUp()
        {
            if (_backedUp)
                return true;
            try
            {
                Directory.CreateDirectory(_backupDir);
                string to = Path.Combine(_backupDir, "NemesisMod " + DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss", CultureInfo.InvariantCulture) + ".ini");
                File.Copy(_theirsPath, to, false);
                _backedUp = true;
                Log.Write("SETTINGS backed up NemesisMod.ini to " + to);
                return true;
            }
            catch (Exception ex)
            {
                _note = "Couldn't back up NemesisMod.ini first, so nothing was changed: " + ex.Message;
                return false;
            }
        }

        // ---- what depends on what ----------------------------------------------------------------------

        /// Why this setting can't be changed right now (its switch is off), or null.
        private string OffBecause(Row r)
        {
            if (r == null || r.E == null || r.Theirs || r.Hookups || _ours == null)
                return null;
            string parent = Parent(r.E.Section, r.E.Key);
            int guard = 0;
            while (parent != null && guard++ < 4)
            {
                IniEntry p = _ours.FindKey(parent);
                if (p != null && !IsOn(p.Value))
                    return "Turn on \"" + Label(parent) + "\" first.";
                parent = Parent(p != null ? p.Section : r.E.Section, parent);
            }
            return null;
        }

        private static string Parent(string section, string key)
        {
            string p;
            if (Needs.TryGetValue(key, out p))
                return p;
            string master;
            if (Masters.TryGetValue(section ?? string.Empty, out master) && !master.Equals(key, StringComparison.OrdinalIgnoreCase))
                return master;
            return null;
        }

        private static bool IsOn(string value)
        {
            string v = (value ?? string.Empty).Trim();
            if (v.Equals("true", StringComparison.OrdinalIgnoreCase))
                return true;
            float f;
            return float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out f) && f > 0f;
        }

        private static string Label(string key)
        {
            string l;
            if (Labels.TryGetValue(key, out l))
                return l;
            TestActions.Entry te = TestActions.ByKey(key);
            if (te != null)
                return te.Label;
            // "StalkerAppearMin" -> "Stalker appear min"
            string spaced = Regex.Replace(key, "(?<=[a-z0-9])(?=[A-Z])|(?<=[A-Z])(?=[A-Z][a-z])", " ");
            string[] words = spaced.Split(' ');
            for (int i = 1; i < words.Length; i++)
                if (!(words[i].Length > 1 && words[i].ToUpperInvariant() == words[i]))
                    words[i] = words[i].ToLowerInvariant();
            return string.Join(" ", words);
        }

        // ---- drawing ----------------------------------------------------------------------------------------

        /// The pad's menu buttons, while a pad is in use: D-pad choose and change (hold X for ten steps), LB / RB turn the
        /// page, A sets a key, B closes. True when the panel was closed.
        private bool PadInput()
        {
            if (PAD.IS_DISABLED_CONTROL_JUST_PRESSED(0, PadCancel))
            {
                if (_capture != null)
                    Key(Keys.Escape, false, false, false);
                else
                    Close();
                return !Open;
            }
            bool big = PAD.IS_DISABLED_CONTROL_PRESSED(0, PadX);
            if (PAD.IS_DISABLED_CONTROL_JUST_PRESSED(0, PadUp)) Key(Keys.Up, big, false, false);
            if (PAD.IS_DISABLED_CONTROL_JUST_PRESSED(0, PadDown)) Key(Keys.Down, big, false, false);
            if (PAD.IS_DISABLED_CONTROL_JUST_PRESSED(0, PadLeft)) Key(Keys.Left, big, false, false);
            if (PAD.IS_DISABLED_CONTROL_JUST_PRESSED(0, PadRight)) Key(Keys.Right, big, false, false);
            if (PAD.IS_DISABLED_CONTROL_JUST_PRESSED(0, PadLB)) Key(Keys.Q, false, false, false);
            if (PAD.IS_DISABLED_CONTROL_JUST_PRESSED(0, PadRB)) Key(Keys.E, false, false, false);
            if (PAD.IS_DISABLED_CONTROL_JUST_PRESSED(0, PadAccept)) Key(Keys.Enter, false, false, false);
            return !Open;
        }

        /// The mouse wheel, on keyboard and mouse: each notch chooses the row above or below, as Up/Down do (not while
        /// a key is being set).
        private void WheelInput()
        {
            if (_capture != null)
                return;
            if (PAD.IS_DISABLED_CONTROL_JUST_PRESSED(0, WheelUp))
                Key(Keys.Up, false, false, false);
            else if (PAD.IS_DISABLED_CONTROL_JUST_PRESSED(0, WheelDown))
                Key(Keys.Down, false, false, false);
        }

        private void DrawHints(float left, bool pad)
        {
            if (_note.Length > 0)
                Draw.Text(Draw.Fit(_note, SmallScale, W - 0.05f), left, Y + H - 0.072f, SmallScale, 236, 196, 120);
            bool testing = _pages.Count > 0 && _page < _pages.Count && _pages[_page].Rows.Any(x => x.Fire);
            string keys = pad ? (testing ? "D-pad choose   bumpers turn the page   A: fire it now (set keys with the keyboard)   B: close"
                                         : "D-pad choose and change   hold X: x10, or the next section   LB/RB page   A: set a key (then press it on the keyboard)   B: close")
                : testing ? "Up/Down or wheel choose   Q/E page   Enter: set a key   Space: fire it now   Delete: no key   Backspace: close"
                : "Up/Down or wheel choose   Left/Right change   Shift: x10, or the next section   Q/E page   Tab: RealNPCs / enRAGE   Enter: set a key   Backspace: close";
            Draw.Text(Draw.Fit(keys, SmallScale * 0.94f, W - 0.04f), left, Y + H - 0.04f, SmallScale * 0.94f, 140, 128, 110);
        }

        public void Render()
        {
            if (!Open)
                return;
            PAD.DISABLE_ALL_CONTROL_ACTIONS(0);
            HUD.HIDE_HUD_AND_RADAR_THIS_FRAME();
            bool pad = !PAD.IS_USING_KEYBOARD_AND_MOUSE(0);
            if (_padCapture != null)
                PadCaptureStep();   // the controller's buttons are being set: they don't move the panel
            else if (pad && PadInput())
                return;   // the pad closed it
            else if (!pad)
                WheelInput();
            Draw.Rect(0f, 0f, 1f, 1f, 0, 0, 0, 110);
            Draw.Rect(X, Y, W, H, 18, 15, 12, 238);
            Draw.Rect(X, Y, W, 0.004f, 196, 156, 92, 255);
            Draw.Rect(X, Y + H - 0.004f, W, 0.004f, 196, 156, 92, 255);

            float left = X + 0.022f, right = X + W - 0.022f;
            Draw.Text("RealNPCs settings", left, Y + 0.016f, TitleScale, 236, 222, 196);
            string where = _pages.Count == 0 ? "" : "page " + (_page + 1) + " of " + _pages.Count;
            Draw.Text(where, right - Draw.Width(where, SmallScale), Y + 0.03f, SmallScale, 150, 138, 118);
            if (_pages.Count == 0)
            {
                Draw.Text("Couldn't read the settings files. The log says why.", left, Y + 0.12f, RowScale, 220, 120, 100);
                return;
            }

            Page page = _pages[_page];
            float pageY = Y + 0.016f + Draw.LineHeight(TitleScale) + 0.006f;
            Draw.Text(page.Title, left, pageY, PageScale, page.Enrage ? 214 : 226, page.Enrage ? 160 : 186, page.Enrage ? 120 : 104);
            string prev = _pages[(_page + _pages.Count - 1) % _pages.Count].Title, next = _pages[(_page + 1) % _pages.Count].Title;
            string turn = "Q  " + prev + "      E  " + next;
            Draw.Text(Draw.Fit(turn, SmallScale, W * 0.55f), right - Math.Min(Draw.Width(turn, SmallScale), W * 0.55f), pageY + 0.012f, SmallScale, 150, 138, 118);

            float rowH = Draw.LineHeight(RowScale) * 1.22f;
            float top = pageY + Draw.LineHeight(PageScale) + 0.014f;
            Draw.Rect(left - 0.006f, top - 0.006f, W - 0.032f, 0.0015f, 120, 98, 66, 200);
            if (!page.Rows.Any(Pickable))
            {
                float my = top + 0.01f;
                foreach (string line in Draw.Wrap(page.Message ?? string.Empty, RowScale, W - 0.05f).Take(6))
                {
                    Draw.Text(line, left, my, RowScale, 214, 176, 130);
                    my += Draw.LineHeight(RowScale) * 1.15f;
                }
                DrawHints(left, pad);
                return;
            }
            // keep the chosen row in view, and its section's heading with it when it's the first in the section
            if (_row < _scroll)
                _scroll = _row;
            if (_row > 0 && page.Rows[_row - 1].Heading != null && _row - 1 < _scroll)
                _scroll = _row - 1;
            if (_row >= _scroll + VisibleRows)
                _scroll = _row - VisibleRows + 1;
            for (int i = _scroll; i < page.Rows.Count && i < _scroll + VisibleRows; i++)
            {
                Row r = page.Rows[i];
                float y = top + (i - _scroll) * rowH;
                if (r.Action != null)
                {
                    // something to do, on Enter (twice)
                    bool onRow = i == _row, asking = _confirm == r;
                    if (onRow)
                    {
                        Draw.Rect(left - 0.006f, y - 0.002f, W - 0.032f, rowH, 70, 54, 32, 215);
                        Draw.Rect(left - 0.006f, y - 0.002f, 0.003f, rowH, 222, 182, 96, 255);
                    }
                    Draw.Text(Draw.Fit(r.ActionLabel, RowScale, W * 0.58f), left + 0.006f, y, RowScale, onRow ? 255 : 226, onRow ? 246 : 216, onRow ? 230 : 196);
                    // a Testing row shows its key (Enter sets it, Space fires it); others ask for Enter, twice for the memory wipe
                    string act = r.Fire ? (_capture == r ? "press a key..." : KeyShown(r)) : asking ? "Enter again to confirm" : "Enter";
                    Draw.Text(act, right - 0.012f - Draw.Width(act, RowScale), y, RowScale, asking ? 236 : 200, asking ? 120 : 170, asking ? 96 : 120);
                    continue;
                }
                if (r.E == null)
                {
                    // a section's heading
                    Draw.Text(Draw.Fit(r.Heading, SmallScale * 1.1f, W * 0.6f), left + 0.002f, y + rowH * 0.12f, SmallScale * 1.1f, 196, 156, 92);
                    Draw.Rect(left + 0.004f + Math.Min(Draw.Width(r.Heading, SmallScale * 1.1f), W * 0.6f) + 0.01f, y + rowH * 0.5f, W * 0.5f, 0.0012f, 120, 98, 66, 160);
                    continue;
                }
                IniEntry e = r.E;
                bool sel = i == _row;
                bool off = OffBecause(r) != null;
                if (sel)
                {
                    Draw.Rect(left - 0.006f, y - 0.002f, W - 0.032f, rowH, 70, 54, 32, 215);
                    Draw.Rect(left - 0.006f, y - 0.002f, 0.003f, rowH, 222, 182, 96, 255);
                }
                int lr = off ? 105 : sel ? 255 : 226, lg = off ? 98 : sel ? 246 : 216, lb = off ? 90 : sel ? 230 : 196;
                Draw.Text(Draw.Fit(Name(r), RowScale, W * 0.58f), left + 0.006f, y, RowScale, lr, lg, lb);
                string v = _capture == r ? "press a key..." : _padCapture == r ? "hold one, press another..." : e.Value.Length == 0 ? "(empty)" : Shown(r, e.Value);
                v = Draw.Fit(v, RowScale, W * 0.3f);
                int vr, vg, vb;
                if (off) { vr = 105; vg = 98; vb = 90; }
                else if (v.Equals("true", StringComparison.OrdinalIgnoreCase)) { vr = 150; vg = 206; vb = 122; }
                else if (v.Equals("false", StringComparison.OrdinalIgnoreCase)) { vr = 170; vg = 128; vb = 112; }
                else { vr = 230; vg = 194; vb = 112; }
                Draw.Text(v, right - 0.012f - Draw.Width(v, RowScale), y, RowScale, vr, vg, vb);
            }
            if (page.Rows.Count > VisibleRows)
            {
                float trackH = VisibleRows * rowH;
                float knobH = Math.Max(0.02f, trackH * VisibleRows / page.Rows.Count);
                float knobY = top + (trackH - knobH) * _scroll / Math.Max(1, page.Rows.Count - VisibleRows);
                Draw.Rect(right - 0.004f, top, 0.003f, trackH, 60, 50, 40, 200);
                Draw.Rect(right - 0.004f, knobY, 0.003f, knobH, 196, 156, 92, 255);
            }

            // what the selected setting does
            float helpY = top + VisibleRows * rowH + 0.012f;
            Draw.Rect(left - 0.006f, helpY - 0.006f, W - 0.032f, 0.0015f, 120, 98, 66, 200);
            Row chosen = page.Rows[Selectable(page, _row)];
            string offWhy = OffBecause(chosen);
            string section = chosen.Theirs ? "enRAGE [" + chosen.E.Section + "]. " : chosen.Hookups ? "CasualHookups, another mod; it may only read this when the game starts. " : string.Empty;
            // an action row has no line of the ini behind it (E is null): its own words, or how to run it
            string own = chosen.Help ?? (chosen.E == null ? (chosen.Action != null ? "Enter, then Enter again, to do it." : string.Empty)
                : string.IsNullOrEmpty(chosen.E.Help) ? "No description." : chosen.E.Help);
            string help = section + (offWhy != null ? offWhy + " " : string.Empty) + own;
            float helpH = Draw.LineHeight(SmallScale) * 1.08f;
            foreach (string line in Draw.Wrap(help, SmallScale, W - 0.05f).Take(3))
            {
                Draw.Text(line, left, helpY, SmallScale, 196, 184, 162);
                helpY += helpH;
            }
            DrawHints(left, pad);
        }
    }
}
