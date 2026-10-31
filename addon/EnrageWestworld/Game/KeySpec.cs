using System;
using System.Linq;
using System.Windows.Forms;

namespace EnrageWestworld.InGame
{
	internal struct KeySpec
	{
		public Keys Key;

		public bool Ctrl;

		public bool Shift;

		public bool Alt;

		public static KeySpec Parse(string text, Keys fallback)
		{
			KeySpec result = new KeySpec
			{
				Key = fallback
			};
			try
			{
				string[] array = (from x in (text ?? string.Empty).Split('+')
					select x.Trim() into x
					where x.Length > 0
					select x).ToArray();
				if (array.Length == 0)
				{
					return result;
				}
				result.Ctrl = array.Any((string x) => x.Equals("Ctrl", StringComparison.OrdinalIgnoreCase) || x.Equals("Control", StringComparison.OrdinalIgnoreCase));
				result.Shift = array.Any((string x) => x.Equals("Shift", StringComparison.OrdinalIgnoreCase));
				result.Alt = array.Any((string x) => x.Equals("Alt", StringComparison.OrdinalIgnoreCase));
				string name = array.Last();
				Keys symbol;
				// a digit is the key in the row above the letters ("0" is D0); Enum.Parse would read it as a number.
				// A symbol is its key on an English keyboard ("]" is OemCloseBrackets).
				if (name.Length == 1 && char.IsDigit(name[0]))
					result.Key = Keys.D0 + (name[0] - '0');
				else if (name.Length == 1 && Symbols.TryGetValue(name[0], out symbol))
					result.Key = symbol;
				else
					result.Key = (Keys)Enum.Parse(typeof(Keys), name, ignoreCase: true);
			}
			catch
			{
				Log.Write("'" + text + "' is not a recognised key; using " + fallback.ToString() + ".");
				result = new KeySpec
				{
					Key = fallback
				};
			}
			return result;
		}

		/// The key as pressed, modifiers and all. A key set to None (no key) never matches.
		public bool Matches(KeyEventArgs e)
		{
			if (Key != Keys.None && e.KeyCode == Key && e.Control == Ctrl && e.Shift == Shift)
			{
				return e.Alt == Alt;
			}
			return false;
		}

		/// The key with at least the modifiers it asks for (an extra Shift or Ctrl held doesn't stop it; an extra Alt does).
		public bool MatchesLoose(KeyEventArgs e)
		{
			return Key != Keys.None && e.KeyCode == Key && e.Alt == Alt && (!Ctrl || e.Control) && (!Shift || e.Shift);
		}

		public override string ToString()
		{
			return (Ctrl ? "Ctrl+" : "") + (Shift ? "Shift+" : "") + (Alt ? "Alt+" : "") + Name(Key);
		}

		private static readonly System.Collections.Generic.Dictionary<char, Keys> Symbols = new System.Collections.Generic.Dictionary<char, Keys>
		{
			{ '[', Keys.OemOpenBrackets }, { ']', Keys.OemCloseBrackets }, { '-', Keys.OemMinus }, { ',', Keys.Oemcomma },
			{ '.', Keys.OemPeriod }, { '/', Keys.OemQuestion }, { '\\', Keys.OemPipe }, { '\'', Keys.OemQuotes }, { '`', Keys.Oemtilde }
		};

		/// How a key is written in the ini and shown: "0" and "]" rather than D0 and OemCloseBrackets.
		public static string Name(Keys key)
		{
			if (key >= Keys.D0 && key <= Keys.D9)
				return ((char)('0' + (key - Keys.D0))).ToString();
			foreach (System.Collections.Generic.KeyValuePair<char, Keys> s in Symbols)
				if (s.Value == key)
					return s.Key.ToString();
			return key.ToString();
		}
	}
}
