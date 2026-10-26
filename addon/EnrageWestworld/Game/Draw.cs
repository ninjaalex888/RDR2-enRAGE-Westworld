// Screen text and boxes, the same way enRAGE draws its own menus (so it works wherever enRAGE's do):
// _BG_SET_TEXT_SCALE / _BG_SET_TEXT_COLOR, then VAR_STRING("LITERAL_STRING", text) into _BG_DISPLAY_TEXT.
// Strings handed to the game are pinned in unmanaged memory and kept for a while after their last use,
// because the game reads them later in the frame.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    internal static class Draw
    {
        private const ulong VarString = 0xFA925AC00EB830B9;
        private const ulong DisplayText = 0x16794E044C9EFB58;

        /// Most text pieces we draw in one frame. The game has a limit shared with enRAGE; stay well below it. The settings
        /// panel raises it while open (enRAGE draws none of its menus then).
        public const int NormalBudget = 48;
        public static int FrameBudget = NormalBudget;

        private sealed class Pinned
        {
            public IntPtr Ptr;
            public int LastFrame;
        }

        private static readonly Dictionary<string, Pinned> Strings = new Dictionary<string, Pinned>();
        private static int _sweepAt;
        private static int _frame = -1;
        private static int _used;
        private static float _aspect = 16f / 9f;
        private static int _aspectAt;

        public static string Font = "$body";

        // ---- strings ------------------------------------------------------------------------------

        private static ulong Pin(string text)
        {
            int frame = Game.FrameCount;
            Pinned p;
            if (!Strings.TryGetValue(text, out p))
            {
                byte[] bytes = Encoding.UTF8.GetBytes(text);
                IntPtr ptr = Marshal.AllocHGlobal(bytes.Length + 1);
                Marshal.Copy(bytes, 0, ptr, bytes.Length);
                Marshal.WriteByte(ptr, bytes.Length, 0);
                p = new Pinned { Ptr = ptr };
                Strings[text] = p;
            }
            p.LastFrame = frame;
            if (frame >= _sweepAt)
            {
                _sweepAt = frame + 600;
                foreach (string old in Strings.Where(kv => frame - kv.Value.LastFrame > 300).Select(kv => kv.Key).ToList())
                {
                    Marshal.FreeHGlobal(Strings[old].Ptr);
                    Strings.Remove(old);
                }
            }
            return (ulong)p.Ptr.ToInt64();
        }

        /// A pinned C string for natives that take const char* (speech lines, animation names...).
        public static ulong CString(string text)
        {
            return Pin(text ?? string.Empty);
        }

        public static void FreeAll()
        {
            foreach (Pinned p in Strings.Values)
                Marshal.FreeHGlobal(p.Ptr);
            Strings.Clear();
        }

        /// The game's text markup uses ~ < >; plain text must not contain them.
        public static string Safe(string s)
        {
            if (string.IsNullOrEmpty(s))
                return string.Empty;
            StringBuilder b = new StringBuilder(s.Length);
            foreach (char c in s)
            {
                switch (c)
                {
                    case '~':
                        break;
                    case '<':
                        b.Append('(');
                        break;
                    case '>':
                        b.Append(')');
                        break;
                    case '‘':
                    case '’':
                        b.Append('\'');
                        break;
                    case '“':
                    case '”':
                        b.Append('"');
                        break;
                    case '–':
                    case '—':
                        b.Append('-');
                        break;
                    default:
                        b.Append(c < ' ' ? ' ' : c);
                        break;
                }
            }
            return b.ToString();
        }

        // ---- frame -------------------------------------------------------------------------------

        private static bool Take(int n)
        {
            int frame = Game.FrameCount;
            if (frame != _frame)
            {
                _frame = frame;
                _used = 0;
            }
            if (_used + n > FrameBudget)
                return false;
            _used += n;
            return true;
        }

        /// Width / height of the game window (16:9 = 1.78).
        public static unsafe float Aspect
        {
            get
            {
                int now = Environment.TickCount;
                if (now - _aspectAt > 2000 || now < _aspectAt)
                {
                    _aspectAt = now;
                    try
                    {
                        int x = 0, y = 0;
                        GRAPHICS.GET_SCREEN_RESOLUTION(&x, &y);
                        if (x > 0 && y > 0)
                        {
                            float a = (float)x / y;
                            if (a > 0.9f && a < 4.5f)
                                _aspect = a;
                        }
                    }
                    catch
                    {
                    }
                }
                return _aspect;
            }
        }

        // ---- drawing -----------------------------------------------------------------------------

        /// Text with its top-left corner at (x, y), in screen fractions.
        public static void Text(string text, float x, float y, float scale, int r, int g, int b, int a = 255, string face = null)
        {
            if (string.IsNullOrEmpty(text) || a <= 0 || !Take(1))
                return;
            text = Safe(text);
            UIDEBUG._BG_SET_TEXT_SCALE(scale, scale);
            UIDEBUG._BG_SET_TEXT_COLOR(r, g, b, a);
            ulong inner = Function.Call<ulong>(VarString, 10, new InputArgument(Pin("LITERAL_STRING")),
                new InputArgument(Pin("<FONT FACE='" + (face ?? Font) + "'>" + text + "</FONT>")));
            ulong v = Function.Call<ulong>(VarString, 42, new InputArgument(Pin("COLOR_STRING")), 0, new InputArgument(inner));
            Function.Call(DisplayText, new InputArgument(v), x, y);
        }

        /// A filled box given by its top-left corner and size.
        public static void Rect(float x, float y, float w, float h, int r, int g, int b, int a)
        {
            if (a <= 0 || w <= 0f || h <= 0f)
                return;
            GRAPHICS.DRAW_RECT(x + w / 2f, y + h / 2f, w, h, r, g, b, a, true, false);
        }

        // ---- measuring ---------------------------------------------------------------------------

        public static float LineHeight(float scale)
        {
            return scale * 0.082f;
        }

        /// Estimated width of text in screen fractions (no font metrics are exposed, so this is a table).
        public static float Width(string s, float scale)
        {
            if (string.IsNullOrEmpty(s))
                return 0f;
            float units = 0f;
            foreach (char c in s)
            {
                if (char.IsUpper(c))
                    units += c == 'I' ? 0.45f : (c == 'M' || c == 'W') ? 1.25f : (c == 'J' || c == 'L' || c == 'F') ? 0.8f : 0.95f;
                else if (char.IsDigit(c))
                    units += 0.82f;
                else
                    switch (c)
                    {
                        case '\'':
                        case ',':
                        case '.':
                        case ':':
                        case ';':
                        case 'i':
                        case 'j':
                        case 'l':
                        case '|':
                        case '!':
                            units += 0.36f;
                            break;
                        case ' ':
                        case '(':
                        case ')':
                        case '-':
                        case 'f':
                        case 'r':
                        case 't':
                        case '"':
                            units += 0.5f;
                            break;
                        case 'm':
                        case 'w':
                            units += 1.15f;
                            break;
                        default:
                            units += 0.78f;
                            break;
                    }
            }
            return units * scale * 0.0222f * (16f / 9f) / Aspect;
        }

        /// Breaks text into lines no wider than maxWidth.
        public static List<string> Wrap(string text, float scale, float maxWidth)
        {
            List<string> lines = new List<string>();
            if (string.IsNullOrEmpty(text))
                return lines;
            foreach (string para in text.Replace("\r", string.Empty).Split('\n'))
            {
                string line = string.Empty;
                foreach (string word in para.Split(' '))
                {
                    if (word.Length == 0)
                        continue;
                    string next = line.Length == 0 ? word : line + " " + word;
                    if (line.Length > 0 && Width(next, scale) > maxWidth)
                    {
                        lines.Add(line);
                        line = word;
                    }
                    else
                    {
                        line = next;
                    }
                }
                lines.Add(line);
            }
            return lines;
        }

        /// Cuts text to fit maxWidth, ending with "...".
        public static string Fit(string text, float scale, float maxWidth)
        {
            if (Width(text, scale) <= maxWidth)
                return text;
            string t = text;
            while (t.Length > 4 && Width(t + "...", scale) > maxWidth)
            {
                int cut = t.LastIndexOf(' ');
                t = cut > t.Length / 2 ? t.Substring(0, cut) : t.Substring(0, t.Length - 1);
            }
            return t.TrimEnd(',', ';', ':', ' ') + "...";
        }
    }
}
