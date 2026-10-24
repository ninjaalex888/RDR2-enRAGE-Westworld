// enRAGE's own settings panel (Alt+F11), reached by name, so RealNPCs can show its larger panel in its place:
// whether it is open, opening and closing it with its own toggle, asking enRAGE to reload NemesisMod.ini after a
// change (the same reload its panel uses), which entries it keeps out of sight, and which keys it rebinds itself.
// If any of these can't be found, RealNPCs leaves Alt+F11 to enRAGE and says so in the log.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace EnrageWestworld.InGame
{
    internal sealed partial class Bridge
    {
        private bool _panelResolved;
        private object _enragePanel;
        private PropertyInfo _panelIsOpen;
        private MethodInfo _panelToggle, _panelHides;
        private FieldInfo _panelReload, _panelPath, _panelKeys;

        private void ResolvePanel()
        {
            if (_panelResolved)
                return;
            _panelResolved = true;
            List<string> missing = new List<string>();
            try
            {
                FieldInfo f = Mod.GetType().GetField("_iniPanel", Any);
                _enragePanel = f != null ? f.GetValue(Mod) : null;
                if (_enragePanel != null)
                {
                    Type t = _enragePanel.GetType();
                    _panelIsOpen = t.GetProperty("IsOpen", Any);
                    _panelToggle = t.GetMethod("Toggle", Any, null, Type.EmptyTypes, null);
                    _panelReload = t.GetField("_reload", Any);
                    _panelPath = t.GetField("_path", Any);
                    _panelHides = t.GetMethod("Internal", Any, null, new[] { typeof(string), typeof(string) }, null);
                    _panelKeys = t.GetField("Bindable", Any);
                }
            }
            catch (Exception ex)
            {
                Log.Write("enRAGE's settings panel couldn't be looked at: " + ex.Message);
            }
            if (_enragePanel == null) missing.Add("the panel");
            if (_panelIsOpen == null) missing.Add("IsOpen");
            if (_panelToggle == null) missing.Add("Toggle");
            if (_panelReload == null) missing.Add("its reload");
            if (_panelPath == null) missing.Add("the ini path");
            if (_panelHides == null) missing.Add("its hidden-entry list (everything will show)");
            Log.Write(missing.Count == 0 ? "enRAGE's settings panel found: Alt+F11 can open the RealNPCs panel in its place."
                : "enRAGE's settings panel: missing " + string.Join(", ", missing) + (CanReplaceSettings ? "." : "; Alt+F11 stays with enRAGE."));
        }

        /// enRAGE's list of the entries it keeps out of sight was found (without it, its pages aren't shown).
        public bool EnrageHidesKnown
        {
            get
            {
                ResolvePanel();
                return _panelHides != null;
            }
        }

        /// enRAGE can be asked to reload its settings after a change (without it, its pages are read-only).
        public bool CanReloadEnrage
        {
            get
            {
                ResolvePanel();
                return _enragePanel != null && _panelReload != null;
            }
        }

        /// Everything needed to stand in for enRAGE's panel was found.
        public bool CanReplaceSettings
        {
            get
            {
                ResolvePanel();
                return _enragePanel != null && _panelIsOpen != null && _panelToggle != null && _panelReload != null;
            }
        }

        public bool EnrageSettingsOpen()
        {
            ResolvePanel();
            try
            {
                return _panelIsOpen != null && _enragePanel != null && (bool)_panelIsOpen.GetValue(_enragePanel, null);
            }
            catch
            {
                return false;
            }
        }

        /// Opens or closes enRAGE's own panel, through its own toggle.
        public void ToggleEnrageSettings()
        {
            ResolvePanel();
            try
            {
                if (_panelToggle != null && _enragePanel != null)
                    _panelToggle.Invoke(_enragePanel, null);
            }
            catch (Exception ex)
            {
                Log.Write("enRAGE's settings panel couldn't be toggled: " + ex.Message);
            }
        }

        /// Asks enRAGE to read NemesisMod.ini again, the way its own panel does after a change. False if it couldn't.
        public bool ReloadEnrageSettings()
        {
            ResolvePanel();
            try
            {
                Action reload = _panelReload != null && _enragePanel != null ? _panelReload.GetValue(_enragePanel) as Action : null;
                if (reload == null)
                    return false;
                reload();
                return true;
            }
            catch (Exception ex)
            {
                Log.Write("enRAGE couldn't reload its settings: " + ex.Message);
                return false;
            }
        }

        /// Where NemesisMod.ini is: from enRAGE's panel, else next to this add-on in the scripts folder.
        public string EnrageIniPath(string scriptsFolder)
        {
            ResolvePanel();
            try
            {
                string p = _panelPath != null && _enragePanel != null ? _panelPath.GetValue(_enragePanel) as string : null;
                if (!string.IsNullOrEmpty(p))
                    return p;
            }
            catch
            {
            }
            return Path.Combine(scriptsFolder ?? string.Empty, "NemesisMod.ini");
        }

        /// An entry enRAGE keeps out of its own panel (its bookkeeping and debug values): kept out of ours too.
        public bool EnrageHides(string section, string key)
        {
            ResolvePanel();
            try
            {
                if (_panelHides != null)
                    return (bool)_panelHides.Invoke(null, new object[] { section, key });
            }
            catch
            {
            }
            return section == "Meta" || section == "Debug";
        }

        /// A key enRAGE rebinds in its own panel (it checks the key against its other keys there).
        public bool EnrageRebinds(string section, string key)
        {
            ResolvePanel();
            try
            {
                HashSet<string> keys = _panelKeys != null ? _panelKeys.GetValue(null) as HashSet<string> : null;
                return section == "Keys" && keys != null && keys.Contains(key);
            }
            catch
            {
                return false;
            }
        }
    }
}
