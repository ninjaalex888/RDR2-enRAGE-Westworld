// Controller buttons for RealNPCs' keys (Lex, 2026-10-28: "for people who use controllers and not keyboard"). A binding
// is a button held and another pressed ("RB+DPadUp"), or one button alone. Names are the Xbox ones, with the
// PlayStation names read as the same buttons (R1 is RB, Cross is A...), and a raw game control ("INPUT_FRONTEND_RB")
// for anyone who needs one the list doesn't have. Each button is read through the game's frontend controls, which the
// settings panel already uses for the pad (the D-pad, A, B, X, LB and RB are proven there; the rest not yet).
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class PadChord
    {
        // button -> the game control it is read through
        private static readonly Dictionary<string, string> Controls = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "A", "INPUT_FRONTEND_ACCEPT" }, { "B", "INPUT_FRONTEND_CANCEL" }, { "X", "INPUT_FRONTEND_X" }, { "Y", "INPUT_FRONTEND_Y" },
            { "LB", "INPUT_FRONTEND_LB" }, { "RB", "INPUT_FRONTEND_RB" }, { "LT", "INPUT_FRONTEND_LT" }, { "RT", "INPUT_FRONTEND_RT" },
            { "LS", "INPUT_FRONTEND_LS" }, { "RS", "INPUT_FRONTEND_RS" }, { "Select", "INPUT_FRONTEND_SELECT" },
            { "DPadUp", "INPUT_FRONTEND_UP" }, { "DPadDown", "INPUT_FRONTEND_DOWN" }, { "DPadLeft", "INPUT_FRONTEND_LEFT" }, { "DPadRight", "INPUT_FRONTEND_RIGHT" }
        };

        // other names for the same buttons: PlayStation's, and plain words
        private static readonly Dictionary<string, string> Aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "Cross", "A" }, { "Circle", "B" }, { "Square", "X" }, { "Triangle", "Y" },
            { "L1", "LB" }, { "R1", "RB" }, { "L2", "LT" }, { "R2", "RT" }, { "L3", "LS" }, { "R3", "RS" },
            { "View", "Select" }, { "Back", "Select" }, { "Share", "Select" }, { "Touchpad", "Select" },
            { "Up", "DPadUp" }, { "Down", "DPadDown" }, { "Left", "DPadLeft" }, { "Right", "DPadRight" },
            { "D-PadUp", "DPadUp" }, { "D-PadDown", "DPadDown" }, { "D-PadLeft", "DPadLeft" }, { "D-PadRight", "DPadRight" }
        };

        /// The buttons the settings panel already reads from the pad, so known to work in game.
        public static readonly string[] Proven = { "A", "B", "X", "LB", "RB", "DPadUp", "DPadDown", "DPadLeft", "DPadRight" };

        /// Every button by its name here, for the game to listen to (the panel's capture, the log's "heard").
        public static IEnumerable<string> Buttons { get { return Controls.Keys; } }

        /// The game control a button is read through: one of ours, or a raw INPUT_ name as written. Null if unknown.
        public static string ControlOf(string button)
        {
            if (string.IsNullOrEmpty(button))
                return null;
            string name = Canon(button);
            string control;
            if (name != null && Controls.TryGetValue(name, out control))
                return control;
            return button.StartsWith("INPUT_", StringComparison.OrdinalIgnoreCase) ? button.ToUpperInvariant() : null;
        }

        /// A button's name as written here ("RB", "DPadUp"), whatever it was called; a raw INPUT_ name as it is; null if unknown.
        public static string Canon(string button)
        {
            if (string.IsNullOrEmpty(button))
                return null;
            string b = button.Trim().Replace(" ", "");
            string alias;
            if (Aliases.TryGetValue(b, out alias))
                b = alias;
            string known = Controls.Keys.FirstOrDefault(k => k.Equals(b, StringComparison.OrdinalIgnoreCase));
            if (known != null)
                return known;
            return b.StartsWith("INPUT_", StringComparison.OrdinalIgnoreCase) ? b.ToUpperInvariant() : null;
        }

        /// Reads "RB+DPadUp" (held, pressed), "DPadUp" (pressed alone) or "None". False with why when it can't be read.
        public static bool Parse(string text, out string hold, out string press, out string why)
        {
            hold = press = why = null;
            string t = (text ?? string.Empty).Trim();
            if (t.Length == 0 || t.Equals("None", StringComparison.OrdinalIgnoreCase))
                return true;   // no button
            string[] parts = t.Split('+').Select(x => x.Trim()).Where(x => x.Length > 0).ToArray();
            if (parts.Length > 2)
            {
                why = "'" + t + "' has more than two buttons (hold one, press another)";
                return false;
            }
            if (parts.Length == 0)
            {
                why = "'" + t + "' names no button";   // "+" alone (night 2026-10-28: it indexed parts[-1] and the add-on never started)
                return false;
            }
            string last = Canon(parts[parts.Length - 1]);
            if (last == null)
            {
                why = "'" + parts[parts.Length - 1] + "' is not a controller button";
                return false;
            }
            if (parts.Length == 2)
            {
                string first = Canon(parts[0]);
                if (first == null)
                {
                    why = "'" + parts[0] + "' is not a controller button";
                    return false;
                }
                if (first.Equals(last, StringComparison.OrdinalIgnoreCase))
                {
                    why = "'" + t + "' is one button twice";
                    return false;
                }
                hold = first;
            }
            press = last;
            return true;
        }

        /// How a binding is written in the ini and shown: "RB+DPadUp", "DPadUp", or "None".
        public static string Name(string hold, string press)
        {
            if (press == null)
                return "None";
            return hold != null ? hold + "+" + press : press;
        }

        /// The binding as written here, from whatever was typed; null if it can't be read.
        public static string Normal(string text)
        {
            string hold, press, why;
            return Parse(text, out hold, out press, out why) ? Name(hold, press) : null;
        }
    }
}
