// The papers the player carries: diaries taken from the dead, letters collected at post offices, clippings.
// A parchment page in the middle of the screen; arrows turn pages and flip between papers.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using EnrageWestworld.Core;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    internal sealed class Reader
    {
        private const float X = 0.2f, Y = 0.1f, W = 0.6f, H = 0.8f;
        private const float BodyScale = 0.34f, TitleScale = 0.46f, SmallScale = 0.26f;

        private List<Paper> _docs = new List<Paper>();
        private int _doc, _page;
        private List<List<string>> _pages = new List<List<string>>();
        private float _laidOutAspect;

        public bool Open { get; private set; }

        public void Show(List<Paper> docs, int index)
        {
            _docs = docs ?? new List<Paper>();
            _doc = Math.Max(0, Math.Min(index, _docs.Count - 1));
            _page = 0;
            Open = true;
            _padArmed = false;   // the buttons that opened it count for nothing until they're let go
            Layout();
        }

        // the pad (Lex, 2026-10-28: controllers): the D-pad turns pages and papers, LB and RB too, B or A closes
        private const uint PadUp = 1662638961u, PadDown = 97156178u, PadLeft = 2791226036u, PadRight = 3736290067u, PadLB = 3901091606u, PadRB = 398377320u,
                           PadAccept = 3350541322u, PadCancel = 359624985u;
        private static readonly uint[] PadAll = { PadUp, PadDown, PadLeft, PadRight, PadLB, PadRB, PadAccept, PadCancel };
        private bool _padArmed;

        private void PadInput()
        {
            if (!_padArmed)
            {
                _padArmed = !PadAll.Any(c => PAD.IS_DISABLED_CONTROL_PRESSED(0, c));
                return;
            }
            if (PAD.IS_DISABLED_CONTROL_JUST_PRESSED(0, PadCancel) || PAD.IS_DISABLED_CONTROL_JUST_PRESSED(0, PadAccept)) Key(Keys.Escape);
            else if (PAD.IS_DISABLED_CONTROL_JUST_PRESSED(0, PadRight) || PAD.IS_DISABLED_CONTROL_JUST_PRESSED(0, PadRB)) Key(Keys.Right);
            else if (PAD.IS_DISABLED_CONTROL_JUST_PRESSED(0, PadLeft) || PAD.IS_DISABLED_CONTROL_JUST_PRESSED(0, PadLB)) Key(Keys.Left);
            else if (PAD.IS_DISABLED_CONTROL_JUST_PRESSED(0, PadDown)) Key(Keys.Down);
            else if (PAD.IS_DISABLED_CONTROL_JUST_PRESSED(0, PadUp)) Key(Keys.Up);
        }

        public void Close()
        {
            Open = false;
        }

        public bool Key(Keys k)
        {
            if (!Open)
                return false;
            switch (k)
            {
                case Keys.Right:
                case Keys.PageDown:
                case Keys.D:
                    if (_page < _pages.Count - 1)
                        _page++;
                    else if (_doc < _docs.Count - 1)
                    {
                        _doc++;
                        _page = 0;
                        Layout();
                    }
                    return true;
                case Keys.Left:
                case Keys.PageUp:
                case Keys.A:
                    if (_page > 0)
                        _page--;
                    else if (_doc > 0)
                    {
                        _doc--;
                        Layout();
                        _page = Math.Max(0, _pages.Count - 1);
                    }
                    return true;
                case Keys.Down:
                case Keys.S:
                    if (_doc < _docs.Count - 1)
                    {
                        _doc++;
                        _page = 0;
                        Layout();
                    }
                    return true;
                case Keys.Up:
                case Keys.W:
                    if (_doc > 0)
                    {
                        _doc--;
                        _page = 0;
                        Layout();
                    }
                    return true;
                case Keys.Escape:
                case Keys.Back:
                case Keys.Enter:
                    Close();
                    return true;
            }
            return false;
        }

        private void Layout()
        {
            _pages = new List<List<string>>();
            _laidOutAspect = Draw.Aspect;
            if (_docs.Count == 0)
            {
                _pages.Add(new List<string> { "You carry no letters, diaries or clippings yet.", "", "Letters wait at post offices in town. Diaries can be found on the dead, and in their camps." });
                return;
            }
            Paper p = _docs[_doc];
            p.Read = true;
            float lineH = Draw.LineHeight(BodyScale) * 1.05f;
            int perPage = Math.Max(4, (int)((H - 0.17f) / lineH));
            List<string> lines = Draw.Wrap(p.Body ?? string.Empty, BodyScale, W - 0.07f);
            for (int i = 0; i < lines.Count; i += perPage)
                _pages.Add(lines.Skip(i).Take(perPage).ToList());
            if (_pages.Count == 0)
                _pages.Add(new List<string>());
        }

        public void Render()
        {
            if (!Open)
                return;
            PAD.DISABLE_ALL_CONTROL_ACTIONS(0);
            bool pad = !PAD.IS_USING_KEYBOARD_AND_MOUSE(0);
            if (pad)
            {
                PadInput();
                if (!Open)
                    return;   // the pad closed it
            }
            if (Math.Abs(Draw.Aspect - _laidOutAspect) > 0.01f)
                Layout();
            Draw.Rect(0f, 0f, 1f, 1f, 0, 0, 0, 120);
            Draw.Rect(X, Y, W, H, 228, 216, 188, 245);
            Draw.Rect(X, Y, W, 0.004f, 120, 90, 50, 255);
            Draw.Rect(X, Y + H - 0.004f, W, 0.004f, 120, 90, 50, 255);
            string title = _docs.Count == 0 ? "Your papers" : _docs[_doc].Title ?? "A paper";
            Draw.Text(Draw.Fit(title, TitleScale, W - 0.06f), X + 0.03f, Y + 0.025f, TitleScale, 60, 38, 20);
            if (_docs.Count > 0)
            {
                Paper p = _docs[_doc];
                string sub = (p.Kind == "diary" ? "Diary" : p.Kind == "clipping" ? "Newspaper clipping" : p.Kind == "note" ? "Note" : "Letter") +
                             (string.IsNullOrEmpty(p.From) ? "" : "  -  " + p.From);
                Draw.Text(sub, X + 0.03f, Y + 0.025f + Draw.LineHeight(TitleScale), SmallScale, 110, 84, 56);
            }
            float lineH = Draw.LineHeight(BodyScale) * 1.05f;
            float y = Y + 0.115f;
            List<string> page = _pages[Math.Max(0, Math.Min(_page, _pages.Count - 1))];
            foreach (string line in page)
            {
                Draw.Text(line, X + 0.035f, y, BodyScale, 40, 30, 22);
                y += lineH;
            }
            string foot = (_docs.Count > 1 ? "Paper " + (_doc + 1) + " of " + _docs.Count + "     " : "") +
                          (_pages.Count > 1 ? "Page " + (_page + 1) + " of " + _pages.Count + "     " : "") +
                          (pad ? "D-pad left/right: turn     up/down: next paper     B: close" : "Left/Right: turn     Up/Down: next paper     Backspace: close");
            Draw.Text(foot, X + 0.03f, Y + H - 0.04f, SmallScale, 110, 84, 56);
        }
    }
}
