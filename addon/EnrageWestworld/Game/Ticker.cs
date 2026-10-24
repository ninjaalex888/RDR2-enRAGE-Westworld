// Draws the bottom ticker: a translucent bar with "Label  text", one or two lines, centred.
using System;
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;

namespace EnrageWestworld.InGame
{
    internal static class Ticker
    {
        private static TickerLine _laidOut;
        private static float _width;
        private static List<string> _lines = new List<string>();
        private static float _scale;
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

            float lineH = Draw.LineHeight(scale);
            float padX = 0.008f, padY = 0.004f;
            float x = 0.5f - _width / 2f;
            float yTop = cfg.TickerY - (_lines.Count - 1) * lineH;
            float barH = _lines.Count * lineH + padY * 2f;

            int age = now - line.ShownAt;
            int fade = age < 0 ? 255 : Math.Min(255, 60 + age * 255 / 400);
            bool fresh = line.Fresh && age < 5000;

            Draw.Rect(x - padX, yTop - padY, _width + padX * 2f, barH, 12, 10, 8, 150);
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
                Draw.Text(label, x, yTop, scale, lr, lg, lb, fade);
            int tr = fresh ? 255 : 234, tg = fresh ? 250 : 228, tb = fresh ? 238 : 212;
            for (int i = 0; i < _lines.Count; i++)
                Draw.Text(_lines[i], x + _labelWidth, yTop + i * lineH, scale, tr, tg, tb, fade);
        }

        private static void Layout(TickerLine line, string label, float scale, float maxWidth)
        {
            _laidOut = line;
            _scale = scale;
            _maxWidth = maxWidth;
            _labelWidth = label.Length > 0 ? Draw.Width(label, scale) + Draw.Width("  ", scale) : 0f;
            bool quoted = line.Kind == "near" || line.Kind == "thought" || line.Kind == "diary";
            string text = quoted ? "\"" + line.Text.Trim('"') + "\"" : line.Text;
            float room = Math.Max(0.1f, maxWidth - _labelWidth);
            List<string> lines = Draw.Wrap(text, scale, room);
            if (lines.Count > 2)
            {
                string rest = string.Join(" ", lines.Skip(1));
                lines = new List<string> { lines[0], Draw.Fit(rest, scale, room) };
            }
            _lines = lines;
            _width = _labelWidth + (lines.Count == 0 ? 0f : lines.Max(l => Draw.Width(l, scale)));
        }
    }
}
