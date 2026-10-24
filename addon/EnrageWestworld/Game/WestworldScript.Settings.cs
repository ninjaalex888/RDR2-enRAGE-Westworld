// The settings panel in the game. With ReplaceEnrageSettings on, Alt+F11 (enRAGE's key for its own settings) opens
// the RealNPCs panel instead: as soon as enRAGE's panel opens, it is closed again through enRAGE's own toggle and
// ours opens (pressed again, both close). SettingsKey opens it too. While it is open every key goes to the panel,
// so none of RealNPCs' own keys fire. For the keys enRAGE rebinds itself, the panel hands over to enRAGE's own.
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
        private KeySpec _settingsKey;
        private bool _settingsRequested;
        private bool _letEnrageShow;   // the player went to enRAGE's own panel: leave it be until it closes
        private readonly Queue<KeyEventArgs> _panelKeys = new Queue<KeyEventArgs>();
        private Action _aSettings;

        private SettingsPanel Panel
        {
            get
            {
                if (_panel == null)
                    _panel = new SettingsPanel(() => _bridge, _iniPath, Path.Combine(_dir, "backup"), ApplyOurSettings, UseEnragePanel);
                return _panel;
            }
        }

        /// Open, and working: if the panel ever switched itself off after errors, keys and the ticker are not held back.
        private bool PanelOpen { get { return _panel != null && _panel.Open && !_featureOff.Contains("settings panel"); } }

        private void SettingsStep(int now)
        {
            if (_aSettings == null)
                _aSettings = SettingsImpl;
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

        private void OpenPanel()
        {
            string scripts = Path.GetDirectoryName(base.FileName);
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
