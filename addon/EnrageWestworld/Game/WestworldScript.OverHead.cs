// Thoughts above the head (0.6.2, [Ticker] ThoughtsOverHeads; Lex: "move the subtitles to above the NPCs head in a
// smaller font to show their thoughts easily"): the ticker's thought of someone in view is drawn over that person, small,
// in a dark box, instead of on the bar at the bottom. The ticker still decides whose turn it is and for how long;
// anything that isn't one person's thought (news, the summary, a scene speaker's line), or a thinker who has gone off
// screen, stays on the bar.
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
        private const float ThoughtAboveHead = 0.62f;   // metres above the head bone: over our bars and enRAGE's name card
        private const float ThoughtWidth = 0.20f;       // share of the screen width one line may take
        private const int ThoughtLines = 4;

        private TickerLine _overHeadLaid, _overHeadCounted;
        private List<string> _overHeadParts = new List<string>();
        private float _overHeadScale, _overHeadWidth;
        private int _overHeadShown, _overHeadOnBar;
        private string _overHeadLast;
        private bool _overHeadDrawn;
        private Action _aOverHead;

        /// Draws the ticker's line over its thinker's head. False when it can't be (not one person's thought, the
        /// setting off, the person gone or off screen), and the bar shows it instead.
        private bool ThoughtOverHead(TickerLine line, int now)
        {
            if (!_cfg.ThoughtsOverHeads || line == null || line.Kind != "near" || line.Handle == 0 || string.IsNullOrEmpty(line.Text))
                return false;
            int h = line.Handle;
            Vector3 head = Vector3.Zero;
            float x = 0f, y = 0f;
            bool can = ENTITY.DOES_ENTITY_EXIST(h) && !ENTITY.IS_ENTITY_DEAD(h) && ENTITY.IS_ENTITY_ON_SCREEN(h);
            if (can)
            {
                head = PED.GET_PED_BONE_COORDS(h, 21030, 0f, 0f, 0f);
                can = ScreenAt(head + new Vector3(0f, 0f, ThoughtAboveHead), out x, out y);
            }
            if (!can)
            {
                CountOverHead(line, false);
                return false;
            }
            float scale = _cfg.ThoughtsOverHeadScale * Math.Max(0.75f, Feelings.SizeAt(head.DistanceTo(Game.Player.Character.Position)));
            if (line != _overHeadLaid || Math.Abs(scale - _overHeadScale) > 0.004f)
            {
                _overHeadLaid = line;
                _overHeadScale = scale;
                List<string> parts = Draw.Wrap("\"" + line.Text.Trim().Trim('"') + "\"", scale, ThoughtWidth);
                if (parts.Count > ThoughtLines)
                    parts = parts.Take(ThoughtLines - 1).Concat(new[] { Draw.Fit(string.Join(" ", parts.Skip(ThoughtLines - 1)), scale, ThoughtWidth) }).ToList();
                _overHeadParts = parts;
                _overHeadWidth = parts.Count == 0 ? 0f : parts.Max(p => Draw.Width(p, scale));
            }
            if (_overHeadParts.Count == 0)
                return false;
            float lineH = Draw.LineHeight(scale), padX = 0.006f, padY = 0.003f, w = _overHeadWidth;
            float top = y - lineH * _overHeadParts.Count;   // the box sits on the point and grows upward
            int age = Math.Max(0, now - line.ShownAt);
            int a = line.ShownAt > now ? 255 : Math.Min(255, 60 + age * 255 / 400);
            Draw.Rect(x - w / 2f - padX, top - padY, w + padX * 2f, lineH * _overHeadParts.Count + padY * 2f, 12, 10, 8, 150 * a / 255);
            Draw.Rect(x - w / 2f - padX, top - padY, w + padX * 2f, 0.0015f, 222, 182, 96, 200 * a / 255);   // the gold edge the bar has
            for (int i = 0; i < _overHeadParts.Count; i++)
                Draw.Text(_overHeadParts[i], x - Draw.Width(_overHeadParts[i], scale) / 2f, top + i * lineH, scale, 244, 238, 222, a);
            CountOverHead(line, true);
            return true;
        }

        /// Each line counted once, over a head or on the bar, for F7; the first over a head is logged.
        private void CountOverHead(TickerLine line, bool overHead)
        {
            if (line == _overHeadCounted)
                return;
            _overHeadCounted = line;
            if (!overHead)
            {
                _overHeadOnBar++;
                return;
            }
            _overHeadShown++;
            _overHeadLast = line.Label;
            if (_overHeadShown == 1)
                Log.Write("THOUGHTS over heads: the first, " + line.Label + ": " + line.Text);
        }

        /// F7: whether thoughts go over heads, and how many did this session.
        private string OverHeadStatus()
        {
            if (!_cfg.ThoughtsOverHeads)
                return "off ([Ticker] ThoughtsOverHeads): on the bar; people within " + (int)_cfg.TickerNearbyMetres + " m";
            return "over heads (size " + _cfg.ThoughtsOverHeadScale.ToString("0.00") + "), people within " + (int)_cfg.TickerNearbyMetres + " m; this session " + _overHeadShown +
                   " over a head, " + _overHeadOnBar + " on the bar (the thinker off screen)" + (_overHeadLast != null ? "; last over " + _overHeadLast : "");
        }
    }
}
