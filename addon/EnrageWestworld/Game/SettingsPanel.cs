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
            public IniEntry E;        // null for a section heading
            public string Heading;
            public bool Theirs;       // a line of NemesisMod.ini, not of RealNPCs.ini
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

        private static readonly string[] OurOrder = { "WestWorld", "Stalkers", "Encounters", "Quiet", "Features", "World", "Scenes", "Romance", "Ticker", "Display", "Pacing", "Output" };
        private static readonly Dictionary<string, string> OurTitles = new Dictionary<string, string>
        {
            { "WestWorld", "West World" }, { "Stalkers", "Stalkers" }, { "Encounters", "Night encounters" }, { "Quiet", "Quiet (enRAGE)" }, { "Features", "People" }, { "World", "The world" }, { "Scenes", "Street scenes" },
            { "Romance", "Romance" }, { "Ticker", "The ticker" }, { "Display", "Bars and meter" }, { "Pacing", "Pacing" }, { "Output", "Notices and the panel" }
        };

        // a setting that only matters while another is on
        private static readonly Dictionary<string, string> Needs = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "FlirtPrompt", "Romance" }, { "NpcFlirting", "Drama" }, { "AwakeningDays", "AwakeningShare" }, { "AwakeningBreakdown", "AwakeningShare" }, { "AwakeningDespair", "AwakeningShare" },
            { "RealityChance", "HostsQuestionReality" }, { "ReturnChance", "Reveries" }, { "StrangerReturnChance", "Reveries" },
            { "DramaGapSeconds", "Drama" }, { "AvengerChance", "Avengers" }, { "QuietStillSeconds", "QuietWhenStill" },
            { "CryingWomanChance", "CryingWoman" }, { "HermitChance", "Hermits" }, { "GatheringChance", "Gatherings" },
            { "Hermits", "WestWorld" }, { "Gatherings", "WestWorld" }, { "RegularsPerTown", "Regulars" }, { "RegularsDress", "Regulars" },
            { "FeelingBarsMetres", "FeelingBars" }, { "FeelingBarsShowEmpty", "FeelingBars" }, { "RealVoiceTalkSeconds", "RealVoiceTalk" }, { "CrudeTalk", "RealVoiceTalk" }, { "Reactions", "Drama" }, { "EnragePeopleJoinIn", "Drama" }, { "RoughHandling", "Drama" }, { "NpcThieves", "Drama" }, { "DrunkChallenges", "Drama" }        };

        // sections whose first setting switches the whole section on or off
        private static readonly Dictionary<string, string> Masters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "WestWorld", "WestWorld" }, { "Stalkers", "Stalkers" }, { "Ticker", "Ticker" }, { "Encounters", "Encounters" }
        };

        private static readonly Dictionary<string, string[]> Choices = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            { "StalkerWhenStill", new[] { "mix", "eerie", "circle", "watch", "rush" } },
            { "NewsTo", new[] { "corner", "ticker", "gamefeed" } },
            { "Subtitles", new[] { "on", "off" } },
            { "WarNewsTo", new[] { "feed", "corner", "ticker" } },
            { "TownPostsAfterDeath", new[] { "survive", "successor" } }
        };

        private static readonly HashSet<string> OurKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "TestKey", "FlirtKey", "WakeKey", "ReaderKey", "SettingsKey", "SettingsKey2", "TickerNextKey", "StalkerKey" };

        // the order RealNPCs' keys are listed in on the Hotkeys page
        private static readonly string[] KeyOrder = { "ReaderKey", "FlirtKey", "WakeKey", "StalkerKey", "TickerNextKey", "TestKey", "SettingsKey", "SettingsKey2" };

        private static readonly Dictionary<string, string> Labels = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "WestWorld", "West World (master switch)" }, { "Stalkers", "Stalkers (master switch)" }, { "Ticker", "Ticker (on or off)" },
            { "TickerY", "Ticker height on screen" }, { "TickerGapSeconds", "Pause between thoughts (seconds)" }, { "Subtitles", "Subtitles for what's said to you" }, { "FlirtPrompt", "Flirt in the lock-on menu (H)" }, { "NpcFlirting", "People flirt and court each other" }, { "TerritoryNews", "enRAGE's territory changes as updates" }, { "RealVoiceTalk", "People talk in their real voices" }, { "RealVoiceTalkSeconds", "About every (seconds)" }, { "CrudeTalk", "Crude remarks (saloons, sweethearts)" }, { "RoughHandling", "Faces into the bar (rough handling)" }, { "Reactions", "People react to what's done and said to them" }, { "EnragePeopleJoinIn", "enRAGE's own people join in" }, { "NpcThieves", "Pickpockets among townsfolk" }, { "DrunkChallenges", "Drunks who call men out" }, { "ThoughtMarker", "Mark who is thinking" }, { "ThoughtSounds", "A small sound as a thought shows" }, { "NpcRomance", "NPC romance" }, { "RevealToEnrage", "Tell enRAGE what you learn" },
            { "RomanceAffectsEnrage", "Romance changes enRAGE's records" }, { "ReplaceEnrageSettings", "Alt+F11 opens this panel" },
            { "HostsQuestionReality", "People question reality" }, { "StalkerWhenStill", "When you stand still, the stalker" }, { "StalkerActMin", "He does odd things at least (metres)" }, { "StalkerActMax", "... and at most (metres)" },
            { "StalkerKey", "Make a stalker now" }, { "StalkerEveryMinutes", "Roll for a stalker every (real minutes)" },
            { "StalkerChance", "Chance on each roll (percent)" }, { "StalkerNearbyMetres", "Turn someone within (metres)" },
            { "StalkerStrangers", "A stranger if nobody near" },
            { "QuietWhenStill", "Hold enRAGE while you stand still" }, { "QuietStillSeconds", "Standing still for (seconds)" },
            { "QuietInCinematic", "Hold enRAGE in the cinematic camera" }, { "TownPosts", "One person at each counter, for good" },
            { "Regulars", "Each town keeps its own people" }, { "RegularsPerTown", "Regulars in each town" }, { "RegularsDress", "Regulars wear their own clothes" }, { "TownPostsAfterDeath", "When a counter keeper is shot down" },
            { "SelfDefenceNoBounty", "No bounty for self-defence" },
            { "FeelingBars", "Bars under people: fear, liking, anger" }, { "FeelingBarsMetres", "For people within (metres)" }, { "FeelingBarsShowEmpty", "Show the empty part of each bar" }, { "GrudgeMeter", "Grudge meter (top right)" },
            { "Encounters", "Night encounters (master switch)" }, { "EncounterEveryMinutes", "Roll for an encounter every (real minutes)" },
            { "CryingWoman", "The crying woman" }, { "CryingWomanChance", "Her chance on each roll (percent)" },
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
        private readonly List<Page> _pages = new List<Page>();
        private IniDoc _ours, _theirs;
        private string _theirsPath;
        private bool _backedUp;
        private int _page, _row, _scroll;
        private string _note = string.Empty;
        private Row _capture;
        private Row _pendingRow;
        private string _pendingValue;
        private int _pendingAt;

        public bool Open { get; private set; }
        /// Waiting for the key to set a hotkey to: every key is that key, even the one that opens and closes the panel.
        public bool Capturing { get { return _capture != null; } }

        public SettingsPanel(Func<Bridge> bridge, string oursPath, string backupDir, Func<string, string> applyOurs, Action useEnragePanel)
        {
            _bridgeOf = bridge;
            _oursPath = oursPath;
            _backupDir = backupDir;
            _applyOurs = applyOurs;
            _useEnragePanel = useEnragePanel;
        }

        // ---- opening and closing ------------------------------------------------------------------------

        public void Show(string enrageIniPath)
        {
            _theirsPath = enrageIniPath;
            Open = true;
            _capture = null;
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
            Draw.FrameBudget = Draw.NormalBudget;
        }

        private void Reload()
        {
            string keepTitle = _pages.Count > 0 && _page < _pages.Count ? _pages[_page].Title : null;
            _pages.Clear();
            Page keys = new Page { Title = "Hotkeys" };
            _ours = IniDoc.Read(_oursPath, true);
            if (_ours.Error == null)
            {
                IEnumerable<IniSection> ordered = OurOrder.Select(n => _ours.Sections.FirstOrDefault(x => x.Name.Equals(n, StringComparison.OrdinalIgnoreCase))).Where(s => s != null)
                    .Concat(_ours.Sections.Where(x => x.Name.Length > 0 && !OurOrder.Contains(x.Name, StringComparer.OrdinalIgnoreCase)));
                foreach (IniSection s in ordered)
                {
                    // the keys are all on the Hotkeys page instead
                    List<Row> rows = s.Entries.Where(e => !OurKeys.Contains(e.Key)).Select(e => new Row { E = e }).ToList();
                    if (rows.Count > 0)
                        _pages.Add(new Page { Title = OurTitles.ContainsKey(s.Name) ? OurTitles[s.Name] : s.Name, Rows = rows });
                }
                List<IniEntry> ourKeys = KeyOrder.Select(k => _ours.FindKey(k)).Where(e => e != null)
                    .Concat(_ours.Sections.SelectMany(s => s.Entries).Where(e => OurKeys.Contains(e.Key) && !KeyOrder.Contains(e.Key, StringComparer.OrdinalIgnoreCase)))
                    .ToList();
                if (ourKeys.Count > 0)
                {
                    keys.Rows.Add(new Row { Heading = "RealNPCs" });
                    keys.Rows.AddRange(ourKeys.Select(e => new Row { E = e }));
                }
            }
            _theirs = !string.IsNullOrEmpty(_theirsPath) && File.Exists(_theirsPath) ? IniDoc.Read(_theirsPath, false) : null;
            // enRAGE's settings only when we know which of its entries are its own bookkeeping, and can ask it to reload
            string why = _bridge == null ? "enRAGE hasn't been found yet (it takes a few seconds after loading a save)."
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
                if (page.Rows[j].E != null)
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
                if (page.Rows[j].E != null)
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
            bool any = page.Rows.Any(r => r.E != null);
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
            }
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
            IniEntry e = r.E;
            if (e == null)
                return;
            string off = OffBecause(r);
            if (off != null)
            {
                _note = off;
                return;
            }
            if (!r.Theirs && OurKeys.Contains(e.Key))
            {
                _capture = r;
                _note = "Waiting for a key: whatever you press next becomes " + Label(e.Key) + " (Ctrl, Shift or Alt with it is fine). Escape keeps " + e.Value + ".";
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

        private void Capture(Keys key, bool shift, bool ctrl, bool alt)
        {
            if (key == Keys.ShiftKey || key == Keys.ControlKey || key == Keys.Menu || key == Keys.LShiftKey || key == Keys.RShiftKey ||
                key == Keys.LControlKey || key == Keys.RControlKey || key == Keys.LMenu || key == Keys.RMenu)
                return;
            Row r = _capture;
            _capture = null;
            if (key == Keys.Escape || key == Keys.Back)
            {
                _note = Label(r.E.Key) + " stays " + r.E.Value + ".";
                return;
            }
            string spec = (ctrl ? "Ctrl+" : string.Empty) + (shift ? "Shift+" : string.Empty) + (alt ? "Alt+" : string.Empty) + KeySpec.Name(key);
            Write(r, spec);
            // enRAGE hears every key too: say so if this one is also one of its own
            IniSection keys = _theirs != null ? _theirs.Sections.FirstOrDefault(s => s.Name == "Keys") : null;
            IniEntry same = keys != null && !ctrl && !shift && !alt ? keys.Entries.FirstOrDefault(k => k.Value.Equals(key.ToString(), StringComparison.OrdinalIgnoreCase)) : null;
            if (same != null)
                _note += ". Careful: enRAGE uses " + key + " too (" + Label(same.Key) + "), so both will happen.";
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
            bool key = (!r.Theirs && OurKeys.Contains(e.Key)) || (r.Theirs && _bridge != null && _bridge.EnrageRebinds(e.Section, e.Key));
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
            IniDoc doc = r.Theirs ? _theirs : _ours;
            if (doc == null || !doc.Set(r.E, value))
                return;
            _pendingRow = r;
            _pendingValue = value;
            _pendingAt = Environment.TickCount;
            _note = Label(r.E.Key) + " = " + value;
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

        private void Write(Row r, string value)
        {
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
            if (r == null || r.E == null || r.Theirs || _ours == null)
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
            string keys = pad ? "D-pad choose and change   hold X: x10, or the next section   LB/RB page   A: set a key (then press it on the keyboard)   B: close"
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
            if (pad && PadInput())
                return;   // the pad closed it
            if (!pad)
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
            if (!page.Rows.Any(r => r.E != null))
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
                Draw.Text(Draw.Fit(Label(e.Key), RowScale, W * 0.58f), left + 0.006f, y, RowScale, lr, lg, lb);
                string v = _capture == r ? "press a key..." : e.Value.Length == 0 ? "(empty)" : e.Value;
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
            string section = chosen.Theirs ? "enRAGE [" + chosen.E.Section + "]. " : string.Empty;
            string help = section + (offWhy != null ? offWhy + " " : string.Empty) + (string.IsNullOrEmpty(chosen.E.Help) ? "No description." : chosen.E.Help);
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
