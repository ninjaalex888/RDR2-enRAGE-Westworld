// Draws the bottom ticker: a translucent bar with "Label  text", centred, always ONE line high. The game draws
// its subtitles just above it, so the bar never grows upward: text a little too long is set slightly smaller,
// and text much too long is shown in parts, one after the other, in the same one-line bar.
using System;
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;

namespace EnrageWestworld.InGame
{
    internal static class Ticker
    {
        private const int MaxParts = 3;
        private const float SmallestScale = 0.86f;   // never shrink text below this share of TickerScale

        private static TickerLine _laidOut;
        private static float _width;
        private static List<string> _parts = new List<string>();
        private static float _scale, _drawScale;
        private static float _maxWidth;
        private static float _labelWidth;

        public static void Render(TickerLine line, Settings cfg, int now)
        {
            if (line == null || string.IsNullOrEmpty(line.Text))
                return;
            float scale = cfg.TickerScale;
            float maxWidth = cfg.TickerWidth;
            string label = line.Label ?? string.Empty;
            if (line != _laidOut || Math.Abs(scale - _scale) > 0.001f || Math.Abs(maxWidth - _maxWidth) > 0.001f)
                Layout(line, label, scale, maxWidth);

            float lineH = Draw.LineHeight(_drawScale);
            float padX = 0.008f, padY = 0.004f;
            float x = 0.5f - _width / 2f;
            float yTop = cfg.TickerY;

            // which part is showing: the line's time on the ticker is shared between its parts
            int age = Math.Max(0, now - line.ShownAt);
            int partMs = Math.Max(2500, cfg.TickerSeconds * 1000 / Math.Max(1, _parts.Count));
            int part = _parts.Count <= 1 ? 0 : Math.Min(_parts.Count - 1, age / partMs);
            int partAge = age - part * partMs;
            int fade = line.ShownAt > now ? 255 : Math.Min(255, 60 + partAge * 255 / 400);
            bool fresh = line.Fresh && age < 5000;

            Draw.Rect(x - padX, yTop - padY, _width + padX * 2f, lineH + padY * 2f, 12, 10, 8, 150);
            Draw.Rect(x - padX, yTop - padY, _width + padX * 2f, 0.0018f, fresh ? 222 : 150, fresh ? 182 : 122, fresh ? 96 : 70, fresh ? 230 : 160);

            int lr, lg, lb;
            switch (line.Kind)
            {
                case "near":
                case "thought":
                case "diary":
                    lr = 222; lg = 182; lb = 96;
                    break;
                case "summary":
                    lr = 168; lg = 150; lb = 118;
                    break;
                default:
                    lr = 205; lg = 150; lb = 92;
                    break;
            }
            if (label.Length > 0)
                Draw.Text(label, x, yTop, _drawScale, lr, lg, lb, line.ShownAt > now ? 255 : Math.Min(255, 60 + age * 255 / 400));
            int tr = fresh ? 255 : 234, tg = fresh ? 250 : 228, tb = fresh ? 238 : 212;
            if (_parts.Count > 0)
                Draw.Text(_parts[part], x + _labelWidth, yTop, _drawScale, tr, tg, tb, fade);
        }

        private static void Layout(TickerLine line, string label, float scale, float maxWidth)
        {
            _laidOut = line;
            _scale = scale;
            _maxWidth = maxWidth;
            bool quoted = line.Kind == "near" || line.Kind == "thought" || line.Kind == "diary";
            string text = quoted ? "\"" + line.Text.Trim('"') + "\"" : line.Text;

            // a little too long: set it a touch smaller so it fits on the one line
            _drawScale = scale;
            for (float s = scale; s >= scale * SmallestScale - 0.0001f; s -= scale * 0.035f)
            {
                _drawScale = s;
                if (Width(label, s) + Draw.Width(text, s) <= maxWidth)
                    break;
            }
            _labelWidth = Width(label, _drawScale);
            float room = Math.Max(0.1f, maxWidth - _labelWidth);
            List<string> parts = Draw.Wrap(text, _drawScale, room);
            if (parts.Count > 1)
            {
                // much too long: the original size again, in parts shown one after another
                _drawScale = scale;
                _labelWidth = Width(label, scale);
                room = Math.Max(0.1f, maxWidth - _labelWidth);
                parts = Draw.Wrap(text, scale, Math.Max(0.1f, room - Draw.Width(" ...", scale)));
                if (parts.Count > MaxParts)
                    parts = parts.Take(MaxParts - 1).Concat(new[] { Draw.Fit(string.Join(" ", parts.Skip(MaxParts - 1)), scale, room) }).ToList();
                for (int i = 0; i < parts.Count - 1; i++)
                    parts[i] += " ...";
            }
            _parts = parts;
            _width = _labelWidth + (parts.Count == 0 ? 0f : parts.Max(l => Draw.Width(l, _drawScale)));
        }

        private static float Width(string label, float scale)
        {
            return label.Length > 0 ? Draw.Width(label, scale) + Draw.Width("  ", scale) : 0f;
        }
    }
}
