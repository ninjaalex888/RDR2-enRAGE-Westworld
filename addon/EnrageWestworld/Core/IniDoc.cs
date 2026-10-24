// A line-faithful view of an ini file, for editing settings in game. It reads sections, keys, values and what each
// one is for, and changes only the value part of one line: spacing, comments, line endings and the text encoding
// stay exactly as they were. Two layouts are understood:
//   RealNPCs.ini   "Key = value   ; what it does"   (the description follows the value)
//   NemesisMod.ini "; what it does" lines, then "Key=value"   (the description comes first; the whole rest is the value)
// Saving goes through a temporary file that then replaces the original, so a half-written file is never left behind.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace EnrageWestworld.Core
{
    public sealed class IniEntry
    {
        public string Section;
        public string Key;
        public string Value;
        public string Help;
        internal int Line;
    }

    public sealed class IniSection
    {
        public string Name;
        public readonly List<IniEntry> Entries = new List<IniEntry>();
    }

    public sealed class IniDoc
    {
        public readonly string Path;
        public readonly bool HelpAfterValue;
        public readonly List<IniSection> Sections = new List<IniSection>();
        public string Error;

        private List<string> _lines = new List<string>();
        private string _newline = "\r\n";
        private bool _endsWithNewline = true;
        private bool _bom;

        private IniDoc(string path, bool helpAfterValue)
        {
            Path = path;
            HelpAfterValue = helpAfterValue;
        }

        public static IniDoc Read(string path, bool helpAfterValue)
        {
            IniDoc d = new IniDoc(path, helpAfterValue);
            try
            {
                byte[] bytes = File.ReadAllBytes(path);
                d._bom = bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF;
                string text = new UTF8Encoding(false).GetString(bytes, d._bom ? 3 : 0, bytes.Length - (d._bom ? 3 : 0));
                d._newline = text.Contains("\r\n") ? "\r\n" : "\n";
                d._endsWithNewline = text.EndsWith("\n");
                d._lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None).ToList();
                if (d._endsWithNewline && d._lines.Count > 0)
                    d._lines.RemoveAt(d._lines.Count - 1);
                d.Parse();
            }
            catch (Exception ex)
            {
                d.Error = ex.Message;
            }
            return d;
        }

        private void Parse()
        {
            Sections.Clear();
            IniSection section = new IniSection { Name = string.Empty };   // keys before the first [section]
            Sections.Add(section);
            List<string> above = new List<string>();
            for (int i = 0; i < _lines.Count; i++)
            {
                string t = _lines[i].Trim();
                if (t.Length == 0)
                {
                    above.Clear();
                    continue;
                }
                if (t.StartsWith(";") || t.StartsWith("#"))
                {
                    above.Add(t.TrimStart(';', '#').Trim());
                    continue;
                }
                if (t.StartsWith("[") && t.EndsWith("]"))
                {
                    section = new IniSection { Name = t.Substring(1, t.Length - 2).Trim() };
                    Sections.Add(section);
                    above.Clear();
                    continue;
                }
                int eq = t.IndexOf('=');
                if (eq <= 0)
                    continue;
                string key = t.Substring(0, eq).Trim();
                string rest = t.Substring(eq + 1);
                string value, help;
                if (HelpAfterValue)
                {
                    int cut = rest.IndexOf(" ;", StringComparison.Ordinal);
                    value = (cut >= 0 ? rest.Substring(0, cut) : rest).Trim();
                    help = cut >= 0 ? rest.Substring(cut + 2).Trim() : string.Join(" ", above);
                    if (value.StartsWith(";"))
                        value = string.Empty;
                }
                else
                {
                    value = rest.Trim();
                    help = string.Join(" ", above);
                }
                section.Entries.Add(new IniEntry { Section = section.Name, Key = key, Value = value, Help = help, Line = i });
                above.Clear();
            }
        }

        public IniEntry Find(string section, string key)
        {
            IniSection s = Sections.FirstOrDefault(x => string.Equals(x.Name, section, StringComparison.OrdinalIgnoreCase));
            return s == null ? null : s.Entries.FirstOrDefault(e => string.Equals(e.Key, key, StringComparison.OrdinalIgnoreCase));
        }

        /// Any entry with this key, whichever section it is in.
        public IniEntry FindKey(string key)
        {
            return Sections.SelectMany(s => s.Entries).FirstOrDefault(e => string.Equals(e.Key, key, StringComparison.OrdinalIgnoreCase));
        }

        /// Puts a new value into the entry's line, keeping everything around the value as it was. Not saved yet.
        public bool Set(IniEntry e, string value)
        {
            if (e == null || e.Line < 0 || e.Line >= _lines.Count || value == null || value.Contains("\n"))
                return false;
            string line = _lines[e.Line];
            int eq = line.IndexOf('=');
            if (eq < 0)
                return false;
            string after = line.Substring(eq + 1);
            string valuePart = after, tail = string.Empty;
            if (HelpAfterValue)
            {
                int cut = after.IndexOf(" ;", StringComparison.Ordinal);
                if (cut >= 0)
                {
                    valuePart = after.Substring(0, cut);
                    tail = after.Substring(cut);
                }
            }
            string lead = valuePart.Substring(0, valuePart.Length - valuePart.TrimStart().Length);
            string trail = valuePart.Substring(valuePart.TrimEnd().Length);
            if (valuePart.Trim().Length == 0)
            {
                // an empty value ("VoiceMan =    ; ..."): one space before the new value, the rest after it
                lead = valuePart.Length > 0 ? " " : string.Empty;
                trail = valuePart.Length > 1 ? valuePart.Substring(1) : string.Empty;
            }
            _lines[e.Line] = line.Substring(0, eq + 1) + lead + value + trail + tail;
            e.Value = value;
            return true;
        }

        /// Writes the file back (same encoding, same line endings) through a temporary file. Null when it worked.
        public string Save()
        {
            string tmp = Path + ".realnpcs-tmp";
            try
            {
                string text = string.Join(_newline, _lines) + (_endsWithNewline ? _newline : string.Empty);
                File.WriteAllText(tmp, text, new UTF8Encoding(_bom));
                if (File.Exists(Path))
                    File.Replace(tmp, Path, null);
                else
                    File.Move(tmp, Path);
                return null;
            }
            catch (Exception ex)
            {
                try
                {
                    if (File.Exists(tmp))
                        File.Delete(tmp);
                }
                catch
                {
                }
                return ex.Message;
            }
        }

        // ---- stepping a value ---------------------------------------------------------------------------

        /// The next value up or down (dir +1 / -1; big = ten steps) for true/false and numbers. Null for text.
        public static string Step(string value, int dir, bool big)
        {
            string v = (value ?? string.Empty).Trim();
            if (v.Equals("true", StringComparison.OrdinalIgnoreCase) || v.Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                bool on = v.Equals("true", StringComparison.OrdinalIgnoreCase);
                bool lower = v == v.ToLowerInvariant();
                string s = (!on).ToString();
                return lower ? s.ToLowerInvariant() : s;
            }
            int n = big ? 10 : 1;
            int i;
            if (int.TryParse(v, NumberStyles.Integer, CultureInfo.InvariantCulture, out i))
            {
                int a = Math.Abs(i);
                int step = a < 20 ? 1 : a < 100 ? 5 : a < 500 ? 25 : 100;
                return (i + dir * step * n).ToString(CultureInfo.InvariantCulture);
            }
            float f;
            if (float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out f))
            {
                float a = Math.Abs(f);
                float step = a < 1f ? 0.01f : a < 10f ? 0.1f : a < 100f ? 1f : 5f;
                double next = Math.Round(f + dir * step * n, 3);
                return next.ToString("0.###", CultureInfo.InvariantCulture);
            }
            return null;
        }
    }
}
