// The settings panel in the game. With ReplaceEnrageSettings on, Alt+F11 (enRAGE's key for its own settings) opens
// the RealNPCs panel instead: as soon as enRAGE's panel opens, it is closed again through enRAGE's own toggle and
// ours opens (pressed again, both close). SettingsKey and SettingsKey2 open it too, and close it again. While it is
// open every other key goes to the panel, so none of RealNPCs' own keys fire. For the keys enRAGE rebinds itself, the panel hands over to enRAGE's own.
// Only enRAGE 0.9.5.3 has the panel the takeover needs; on other versions Alt+F11 stays enRAGE's, and the player is
// told SettingsKey about 15 s into the world (once on the first run when the takeover works too).
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using EnrageWestworld.Core;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private SettingsPanel _panel;
        private KeySpec _settingsKey, _settingsKey2;
        private bool _settingsRequested;
        private bool _letEnrageShow;   // the player went to enRAGE's own panel: leave it be until it closes
        private int _keyNoticeAt;      // game time the settings-key notice is due (15 s into the world); 0 not yet set
        private bool _keyNoticed;      // given, or not needed, this session
        private string _keyNoticeCase; // which case applied, for the F7 dump
        private readonly Queue<KeyEventArgs> _panelKeys = new Queue<KeyEventArgs>();
        private Action _aSettings, _aKeyNotice;

        private SettingsPanel Panel
        {
            get
            {
                if (_panel == null)
                    _panel = new SettingsPanel(() => _bridge, _iniPath, Path.Combine(_dir, "backup"), ApplyOurSettings, UseEnragePanel, PanelAction);
                return _panel;
            }
        }

        /// Open, and working: if the panel ever switched itself off after errors, keys and the ticker are not held back.
        private bool PanelOpen { get { return _panel != null && _panel.Open && !_featureOff.Contains("settings panel"); } }

        private void SettingsStep(int now)
        {
            if (_aSettings == null)
                _aSettings = SettingsImpl;
            if (_aKeyNotice == null)
                _aKeyNotice = SettingsKeyNotice;
            if (!_keyNoticed && _cfg != null)
                Safe("settings key notice", _aKeyNotice);   // on its own: a fault here mustn't switch the panel off
            if (_panel != null && _panel.Open && _featureOff.Contains("settings panel"))
            {
                try
                {
                    _panel.Close();   // it failed too often: let go of the keys and the screen
                }
                catch
                {
                }
                Draw.FrameBudget = Draw.NormalBudget;
                return;
            }
            Safe("settings panel", _aSettings);
        }

        private void SettingsImpl()
        {
            if (_cfg == null)
                return;
            SettingsPanel panel = Panel;
            while (_panelKeys.Count > 0)
            {
                KeyEventArgs k = _panelKeys.Dequeue();
                if (panel.Open && !panel.Capturing && SettingsKeyHit(k))
                {
                    panel.Close();   // the key that opened it closes it (unless it's being set as a key)
                    Log.Write("SETTINGS panel closed (" + (k.Control ? "Ctrl+" : "") + (k.Shift ? "Shift+" : "") + (k.Alt ? "Alt+" : "") + KeySpec.Name(k.KeyCode) + ")");
                    continue;
                }
                panel.Key(k.KeyCode, k.Shift, k.Control, k.Alt);
            }
            bool theirsOpen = _bridge != null && _bridge.EnrageSettingsOpen();
            if (theirsOpen && !_letEnrageShow && _cfg.ReplaceEnrageSettings && _bridge.CanReplaceSettings)
            {
                _bridge.ToggleEnrageSettings();   // closed again through enRAGE's own toggle
                if (panel.Open)
                    panel.Close();
                else
                    OpenPanel();
            }
            if (!theirsOpen)
                _letEnrageShow = false;
            if (_settingsRequested)
            {
                _settingsRequested = false;
                if (panel.Open)
                    panel.Close();
                else
                    OpenPanel();
            }
            if (panel.Open)
            {
                panel.Tick();
                panel.Render();
            }
        }

        /// About 15 s into the world after loading, says which key opens the RealNPCs settings: every session when Alt+F11
        /// can't open them (enRAGE not found, or its panel couldn't be reached: only 0.9.5.3 has it), and once ever, on the
        /// first run, when it can.
        private void SettingsKeyNotice()
        {
            if (_keyNoticed)
                return;
            if (_keyNoticeAt == 0)
                _keyNoticeAt = _now + 15000;
            if (_now - _keyNoticeAt < 0)
                return;
            string key = SettingsKeysText(" or ");
            if (_bridge == null || !_bridge.CanReplaceSettings)
            {
                _keyNoticed = true;
                _keyNoticeCase = _bridge == null ? "told (enRAGE not found)" : !_bridge.Present ? "told (standing alone)" : "told (Alt+F11 can't open this panel)";
                string line = "RealNPCs settings: " + key + (_bridge != null && !_bridge.Present ? "." : ". With this enRAGE version, Alt+F11 opens enRAGE's own panel.");
                KeyNoticeOut(line);
                Log.Write("SETTINGS key: " + (_bridge == null ? "enRAGE not found" : !_bridge.Present ? "standing alone (no enRAGE panel to replace)" : "Alt+F11 can't open the RealNPCs panel with enRAGE " + _bridge.Version)
                          + "; told the player " + key + " opens it");
                return;
            }
            if (_store == null)
                return;   // Alt+F11 works: wait for the saved data, which knows whether this is the first run
            _keyNoticed = true;
            if (_store.Count("told:settings key") > 0)
            {
                _keyNoticeCase = "told before, not again";
                Log.Write("SETTINGS key: Alt+F11 " + (_cfg.ReplaceEnrageSettings ? "opens the RealNPCs panel" : "left to enRAGE (ReplaceEnrageSettings off)") + ", " + key + " too; told before, not again");
                return;
            }
            _store.Bump("told:settings key");
            _keyNoticeCase = "first run, told";
            KeyNoticeOut("RealNPCs settings: " + key + (_cfg.ReplaceEnrageSettings ? " (Alt+F11 opens them too)." : ". Alt+F11 opens enRAGE's own panel (ReplaceEnrageSettings is off)."));
            Log.Write("SETTINGS key: Alt+F11 " + (_cfg.ReplaceEnrageSettings ? "opens the RealNPCs panel" : "left to enRAGE (ReplaceEnrageSettings off)") + "; first run, told the player " + key);
        }

        /// SettingsKey or SettingsKey2 (a key set to None never matches).
        private bool SettingsKeyHit(KeyEventArgs e)
        {
            if (_settingsKey.Matches(e) || _settingsKey2.Matches(e))
                return true;
            // the key with an extra Shift or Ctrl held (sprinting, crouching, or one Windows thinks is still down after Alt+Tab)
            // counts too, unless that exact combination is another of RealNPCs' keys (players, 2026-10-30: Alt+R stopped working)
            if (!(_settingsKey.MatchesLoose(e) || _settingsKey2.MatchesLoose(e)))
                return false;
            if (_testKey.Matches(e) || _flirtKey.Matches(e) || _readerKey.Matches(e) || _tickerNextKey.Matches(e) || IsTestingKey(e))
                return false;
            if (!_looseKeyNoted)
            {
                _looseKeyNoted = true;
                Log.Write("SETTINGS key pressed with " + (e.Shift ? "Shift " : "") + (e.Control ? "Ctrl " : "") + "held too (sprinting, crouching, or a key Windows thinks is still down): taken as the settings key");
            }
            return true;
        }

        private bool _looseKeyNoted;

        /// The keys that open the panel, each named once (both set to the same key: just that one).
        private string SettingsKeysText(string joint)
        {
            string a = _settingsKey.ToString(), b = _settingsKey2.Key != Keys.None ? _settingsKey2.ToString() : null;
            return b == null || b == a ? a : a + joint + b;
        }

        /// For the F7 dump: the keys, what Alt+F11 does, and what the player was told.
        private string SettingsStatus()
        {
            string alt = _bridge == null ? "enRAGE not found" : !_bridge.Present ? "nothing (standing alone)" : !_bridge.CanReplaceSettings ? "stays with enRAGE " + _bridge.Version
                : _cfg.ReplaceEnrageSettings ? "opens this panel" : "left to enRAGE (ReplaceEnrageSettings off)";
            return "keys " + SettingsKeysText(" and ") + (_settingsKey2.Key == Keys.None ? " (no second key)" : "") + ", Alt+F11 " + alt
                   + ", notice " + (_keyNoticeCase ?? (_keyNoticed ? "given" : "not yet"));
        }

        /// Where the player will see it: the news, unless that's the ticker and nothing draws it (enRAGE not found yet,
        /// or the ticker off), then a notice on screen.
        private void KeyNoticeOut(string line)
        {
            if (_engine == null || (_cfg.NewsRoute == "ticker" && !_cfg.Ticker))
                Feed.Notice(null, line);
            else
                News(null, line);
        }

        private void OpenPanel()
        {
            string scripts = Path.GetDirectoryName(base.FileName);
            string game = GameDir();   // CasualHookups' settings too, when it's installed (WestworldScript.Hookups.cs)
            Panel.HookupsIniPath = game != null && File.Exists(Path.Combine(game, "CasualHookups.asi")) ? Path.Combine(game, "CasualHookups.ini") : null;
            Panel.GameDir = game;        // the Mod list page
            Panel.ScriptsDir = scripts;
            Panel.Show(_bridge != null ? _bridge.EnrageIniPath(scripts) : Path.Combine(scripts ?? string.Empty, "NemesisMod.ini"));
            Log.Write("SETTINGS panel opened");
        }

        /// Reads RealNPCs.ini again after the panel changed it, at once; gives the value now in force for a key.
        private string ApplyOurSettings(string key)
        {
            EnrageWestworld.Core.Settings fresh = EnrageWestworld.Core.Settings.Load(_iniPath, Log.Write);
            _iniStamp = EnrageWestworld.Core.Settings.Stamp(_iniPath);
            _cfg.CopyFrom(fresh.Gated());
            ApplySettings();
            return fresh.ValueText(key);
        }

        private void UseEnragePanel()
        {
            _letEnrageShow = true;
            if (_bridge != null && !_bridge.EnrageSettingsOpen())
                _bridge.ToggleEnrageSettings();
        }
    }
}
