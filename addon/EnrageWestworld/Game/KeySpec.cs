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
				// a digit is the key in the row above the letters ("0" is D0); Enum.Parse would read it as a number
				result.Key = name.Length == 1 && char.IsDigit(name[0]) ? Keys.D0 + (name[0] - '0') : (Keys)Enum.Parse(typeof(Keys), name, ignoreCase: true);
			}
			catch
			{
				Log.Write("'" + text + "' isn't a key name; using " + fallback.ToString() + ".");
				result = new KeySpec
				{
					Key = fallback
				};
			}
			return result;
		}

		public bool Matches(KeyEventArgs e)
		{
			if (e.KeyCode == Key && e.Control == Ctrl && e.Shift == Shift)
			{
				return e.Alt == Alt;
			}
			return false;
		}

		public override string ToString()
		{
			return (Ctrl ? "Ctrl+" : "") + (Shift ? "Shift+" : "") + (Alt ? "Alt+" : "") + Key;
		}
	}
}
