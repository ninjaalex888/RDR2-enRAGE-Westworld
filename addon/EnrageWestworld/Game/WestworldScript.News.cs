// "Around town": the news, rumours, life updates, gang news and what people elsewhere think of you, one item at a
// time in a small box in the top right, under the "Sore at you" panel ([Output] NewsTo = corner, the default). The
// bottom ticker then keeps only the thoughts of the people you can see, and the person thinking gets a small gold
// mark over them (ThoughtMarker). Each item stays a few seconds, longer for a longer one, and its time doesn't run
// while a menu hides it; with nothing new the box goes away.
using System;
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private const string NewsTitle = "Around town";
        private const int NewsQueueMax = 6;

        private sealed class NewsItem
        {
            public string Label, Text;
            public bool Quoted;   // someone's own words: a thought or a diary line
        }

        private readonly List<NewsItem> _newsQueue = new List<NewsItem>();
        private NewsItem _newsShown;
        private List<string> _newsLines;
        private int _newsShownAt, _newsUntil, _newsLastFrame;
        private float _meterBottom;   // where the "Sore at you" panel ended this frame (0: not drawn)
        private Action _aNews;

        /// A new item for the box: shown after the ones already waiting (at most NewsQueueMax wait; the oldest goes).
        private void NewsPush(string label, string text, string kind)
        {
            text = text != null ? text.Trim() : string.Empty;
            if (text.Length == 0)
                return;
            if ((_newsShown != null && _newsShown.Text == text) || _newsQueue.Any(n => n.Text == text))
                return;
            _newsQueue.Add(new NewsItem { Label = label, Text = text, Quoted = kind == "thought" || kind == "diary" });
            while (_newsQueue.Count > NewsQueueMax)
                _newsQueue.RemoveAt(0);
        }

        private void NewsStep(int now)
        {
            if (_aNews == null)
                _aNews = () => NewsFrame(_now);
            Safe("news", _aNews);
        }

        private void NewsFrame(int now)
        {
            int since = _newsLastFrame == 0 ? 0 : now - _newsLastFrame;
            _newsLastFrame = now;
            if (_newsShown == null && _newsQueue.Count == 0)
                return;
            if (PanelOpen || _reader.Open || !TickerVisible(now))
            {
                _newsUntil += since;   // hidden: the item keeps the time it had left
                return;
            }
            if (_newsShown != null && now >= _newsUntil)
                _newsShown = null;
            if (_newsShown == null)
            {
                if (_newsQueue.Count == 0)
                    return;
                _newsShown = _newsQueue[0];
                _newsQueue.RemoveAt(0);
                _newsLines = null;
                _newsShownAt = now;
                _newsUntil = now + Math.Min(16000, 7000 + _newsShown.Text.Length * 45);
            }
            DrawNews(now);
        }

        private void DrawNews(int now)
        {
            const float title = 0.26f, body = 0.23f, pad = 0.006f;
            float x = MeterX, w = MeterW, room = w - pad * 2f;
            float y = _meterBottom > 0f ? _meterBottom + 0.008f : MeterY;
            if (_newsLines == null)
            {
                string text = _newsShown.Quoted ? "\"" + _newsShown.Text.Trim('"') + "\"" : _newsShown.Text;
                _newsLines = Draw.Wrap(text, body, room);
                if (_newsLines.Count > 5)
                    _newsLines = _newsLines.Take(4).Concat(new[] { Draw.Fit(string.Join(" ", _newsLines.Skip(4)), body, room) }).ToList();
            }
            bool label = !string.IsNullOrEmpty(_newsShown.Label);
            float height = pad * 2f + Draw.LineHeight(title) + 0.003f + (label ? Draw.LineHeight(body) : 0f) + _newsLines.Count * Draw.LineHeight(body);
            int a = Math.Max(0, Math.Min(255, Math.Min(now - _newsShownAt, _newsUntil - now) * 255 / 300));   // fades in and out
            Draw.Rect(x, y, w, height, 12, 10, 8, 150 * a / 255);
            Draw.Text(NewsTitle, x + pad, y + pad, title, 236, 214, 160, a);
            float ty = y + pad + Draw.LineHeight(title) + 0.003f;
            if (label)
            {
                Draw.Text(Draw.Fit(_newsShown.Label, body, room), x + pad, ty, body, 205, 150, 92, a);
                ty += Draw.LineHeight(body);
            }
            foreach (string line in _newsLines)
            {
                Draw.Text(line, x + pad, ty, body, 234, 228, 212, a);
                ty += Draw.LineHeight(body);
            }
        }

        /// The name to show for someone: the one enRAGE has given them (on its name card) when it has, else ours, so the
        /// ticker, the scenes and the card never call the same person two different names.
        private string ShownName(int handle, string ours)
        {
            string enrages = _bridge != null ? _bridge.EnrageNameAt(handle) : null;
            return enrages != null && enrages.Trim().Length > 0 ? enrages : ours;
        }

        /// A small gold mark over the person whose thought is on the ticker: just under their feeling bars if they
        /// have any, else where the bars would start.
        private void DrawThinkingMark(TickerLine line)
        {
            if (line == null || line.Kind != "near" || line.Handle == 0 || !ENTITY.DOES_ENTITY_EXIST(line.Handle) ||
                ENTITY.IS_ENTITY_DEAD(line.Handle) || !ENTITY.IS_ENTITY_ON_SCREEN(line.Handle))
                return;
            Vector3 head = PED.GET_PED_BONE_COORDS(line.Handle, 21030, 0f, 0f, 0f);
            float x, y;
            if (!ScreenAt(head + new Vector3(0f, 0f, BarsAboveHead), out x, out y))
                return;
            float size = Feelings.SizeAt(head.DistanceTo(Game.Player.Character.Position));
            Felt felt = _cfg.FeelingBars ? _felt.FirstOrDefault(f => f.Ped.Handle == line.Handle) : null;
            // under the bars, else a little below where enRAGE's name card sits, so the card doesn't cover it
            y += felt != null ? felt.Bars.Count * (0.0055f + 0.004f) * size + 0.004f : 0.012f * size;
            const string mark = "...";
            float s = 0.5f * size;
            Draw.Text(mark, x - Draw.Width(mark, s) / 2f, y - Draw.LineHeight(s) * 0.45f, s, 236, 196, 104, 245);
        }
    }
}
