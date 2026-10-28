// A controller's buttons for RealNPCs' keys (Lex, 2026-10-28: "for people who use controllers and not keyboard";
// Core/PadChord.cs, [Controller]). While a controller is the last thing touched, a button held and another pressed does
// what a key does: opens the settings panel (from which every Testing row fires with A), the F7 status, the letters,
// the ticker's next line. Only in play: never while the game's pause menu or one of its menus (the satchel, the map, a
// catalogue: a UI app) is open, where the same buttons change tabs and move; never while our panel or the letters are
// open (they read the pad themselves). The held button must be down 200 ms first, so a quick tap of RB for cover and
// then the D-pad isn't taken for one. The log says each button the first time it's heard in a session, so which of the
// game's frontend controls answer in play can be seen (the panel proves the D-pad, A, B, X, LB and RB).
using System;
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private sealed class PadBind
        {
            public string Setting, What, Shown;
            public uint Hold, Press;
            public Action Fire;
        }

        private const int PadHoldMs = 200;
        private readonly List<PadBind> _padBinds = new List<PadBind>();
        private readonly Dictionary<uint, int> _padDownSince = new Dictionary<uint, int>();
        private readonly HashSet<string> _padHeard = new HashSet<string>();
        private List<KeyValuePair<string, uint>> _padButtons;
        private string _padLast, _padBlocked;
        private int _padFired, _padBlockedAt;
        private Action _aPad;

        /// The controller's bindings, from the settings (ApplySettings).
        private void BuildPadKeys()
        {
            _padBinds.Clear();
            if (_cfg == null)
                return;
            AddPad("PadSettings", "the settings panel", _cfg.PadSettings, () => _settingsRequested = true);
            AddPad("PadStatus", "the F7 status", _cfg.PadStatus, () => _testRequested = true);
            AddPad("PadLetters", "the letters", _cfg.PadLetters, () => _readerRequested = true);
            AddPad("PadTickerNext", "the ticker's next line", _cfg.PadTickerNext, () =>
            {
                if (_cfg.Ticker)
                    _tickerNextRequested = true;
            });
        }

        private void AddPad(string setting, string what, string text, Action fire)
        {
            string hold, press, why;
            if (!PadChord.Parse(text, out hold, out press, out why))
            {
                Log.Write("PAD " + setting + ": " + why + "; no button for " + what);
                return;
            }
            if (press == null)
                return;
            _padBinds.Add(new PadBind
            {
                Setting = setting, What = what, Shown = PadChord.Name(hold, press), Fire = fire,
                Hold = hold != null ? Natives.Hash(PadChord.ControlOf(hold)) : 0u, Press = Natives.Hash(PadChord.ControlOf(press))
            });
        }

        private void PadStep()
        {
            if (_cfg == null || !_cfg.PadKeys || _padBinds.Count == 0)
                return;
            if (_aPad == null)
                _aPad = () => PadTick(Game.GameTime);
            Safe("controller", _aPad);
        }

        private void PadTick(int now)
        {
            if (PAD.IS_USING_KEYBOARD_AND_MOUSE(0))
            {
                if (_padDownSince.Count > 0)
                    _padDownSince.Clear();
                return;
            }
            PadHeard();
            foreach (PadBind b in _padBinds)
            {
                if (b.Hold == 0)
                    continue;
                if (!PAD.IS_DISABLED_CONTROL_PRESSED(0, b.Hold))
                    _padDownSince.Remove(b.Hold);
                else if (!_padDownSince.ContainsKey(b.Hold))
                    _padDownSince[b.Hold] = now;
            }
            PadBind hit = null;
            foreach (PadBind b in _padBinds)
            {
                int since;
                if (b.Hold != 0 && PAD.IS_DISABLED_CONTROL_JUST_PRESSED(0, b.Press) && _padDownSince.TryGetValue(b.Hold, out since) && now - since >= PadHoldMs)
                {
                    hit = b;
                    break;
                }
            }
            if (hit == null)
                foreach (PadBind b in _padBinds)
                    if (b.Hold == 0 && PAD.IS_DISABLED_CONTROL_JUST_PRESSED(0, b.Press) && !_padBinds.Any(o => o.Hold != 0 && o.Press == b.Press && PAD.IS_DISABLED_CONTROL_PRESSED(0, o.Hold)))
                    {
                        hit = b;
                        break;
                    }
            if (hit == null)
                return;
            string not = PadNotNow();
            if (not != null)
            {
                _padBlocked = hit.Shown + " (" + hit.What + "): " + not;
                if (now - _padBlockedAt > 10000)
                {
                    _padBlockedAt = now;
                    Log.Write("PAD " + _padBlocked + ", so nothing");
                }
                return;
            }
            _padFired++;
            _padLast = hit.Shown + ": " + hit.What;
            Log.Write("PAD " + _padLast);
            hit.Fire();
        }

        /// Why a binding does nothing now (null if it may): the game's own menus read the same buttons.
        private string PadNotNow()
        {
            if (PanelOpen || _reader.Open)
                return "the settings or the letters are open";
            bool paused = HUD.IS_PAUSE_MENU_ACTIVE(), menu = UIAPPS.IS_ANY_UIAPP_ACTIVE(), control = PLAYER.IS_PLAYER_CONTROL_ON(Game.Player.Handle);
            return paused ? "you're in the pause menu" : menu ? "one of the game's menus is open" : !control ? "you have no control just now" : null;
        }

        /// Each button the first time it's heard this session (a test of which frontend controls answer in play).
        private void PadHeard()
        {
            if (_padButtons == null)
                _padButtons = PadChord.Buttons.Select(n => new KeyValuePair<string, uint>(n, Natives.Hash(PadChord.ControlOf(n)))).ToList();
            if (_padHeard.Count >= _padButtons.Count)
                return;
            foreach (KeyValuePair<string, uint> b in _padButtons)
                if (!_padHeard.Contains(b.Key) && PAD.IS_DISABLED_CONTROL_JUST_PRESSED(0, b.Value) && _padHeard.Add(b.Key))
                    Log.Write("PAD heard " + b.Key + " (" + PadChord.ControlOf(b.Key) + ")" + (PadChord.Proven.Contains(b.Key) ? "" : ", not proven before"));
        }

        /// F7: whether a controller is in use, the bindings, the last one used and which buttons answered.
        private string PadStatus()
        {
            if (_cfg == null || !_cfg.PadKeys)
                return "off ([Controller] PadKeys)";
            return (PAD.IS_USING_KEYBOARD_AND_MOUSE(0) ? "keyboard and mouse in use" : "a controller in use") + "; " +
                   (_padBinds.Count == 0 ? "no buttons set" : string.Join(", ", _padBinds.Select(b => b.Shown + " " + b.What))) +
                   "; used " + _padFired + " this session" + (_padLast != null ? " (last " + _padLast + ")" : "") +
                   (_padBlocked != null ? "; last held back: " + _padBlocked : "") +
                   "; heard: " + (_padHeard.Count == 0 ? "nothing yet" : string.Join(" ", _padHeard));
        }
    }
}
