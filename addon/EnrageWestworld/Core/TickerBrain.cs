// Decides what the bottom ticker says. People near the player come first (their private thought about him);
// a brand-new notice gets one turn in between; with nobody near it rotates a summary and the latest news, or, with
// no summary given (news shown elsewhere), says nothing. GapMs puts a short blank between one line and the next.
// Pure logic: the game layer draws Current and feeds in who is near.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public sealed class TickerLine
    {
        public string Label;   // "Elihu Starling", "News", "RealNPCs"
        public string Text;
        public string Kind;    // near, news, summary
        public string Key;     // person key for near lines
        public int Handle;     // the thinker's ped for near lines, 0 if unknown (the game side marks them)
        public int ShownAt;    // game ms
        public bool Fresh;     // a notice shown for the first time (drawn brighter)
    }

    public sealed class NearThought
    {
        public string Key;
        public string Name;
        public float Distance;
        public int Handle;          // the game's handle for the person, 0 if none (for a sound as the thought shows)
        public Func<string> Text;   // computed lazily, at most every ThoughtCacheMs per person
        public bool Beside;         // right next to the player: every other turn, and a new thought every BesideCacheMs
    }

    public sealed class TickerBrain
    {
        public int SlotMs = 12000;
        public int ThoughtCacheMs = 90000;
        public int BesideCacheMs = 30000;
        public int HistorySize = 14;
        public int GapMs;             // a blank this long between one line and the next (0: none)

        private readonly List<TickerLine> _fresh = new List<TickerLine>();
        private readonly List<TickerLine> _history = new List<TickerLine>();
        private readonly Dictionary<string, KeyValuePair<int, string>> _thoughts = new Dictionary<string, KeyValuePair<int, string>>();
        private int _nextAt;
        private bool _skipped;
        private int _rotation;        // index into history while nobody is near
        private int _nearTurn;        // which near person is next
        private bool _summaryNext = true;
        private string _lastNearKey;
        private TickerLine _prev;     // the last line shown, through any blank after it

        public TickerLine Current { get; private set; }

        public int FreshCount { get { return _fresh.Count; } }

        /// A new notice: it gets a turn soon and then joins the rotation.
        public void Push(string label, string text, string kind = "news", int handle = 0)
        {
            if (string.IsNullOrEmpty(text))
                return;
            text = text.Trim();
            if (_fresh.Any(x => x.Text == text) || (Current != null && Current.Text == text))
                return;
            _fresh.Add(new TickerLine { Label = label, Text = text, Kind = kind, Fresh = true, Handle = handle });
            if (_fresh.Count > 6)
                _fresh.RemoveAt(0);
        }

        /// The player asked for the next line now (TickerNextKey). From someone's thought it moves on to the news,
        /// so asking again near the same person doesn't bring back the same thought.
        public void Skip()
        {
            _nextAt = 0;
            _skipped = true;
        }

        public void Clear()
        {
            _fresh.Clear();
            _history.Clear();
            _thoughts.Clear();
            Current = null;
            _prev = null;
            _nextAt = 0;
        }

        /// Call a few times a second. near: people close to the player, any order.
        public TickerLine Update(int now, IList<NearThought> near, Func<string> summary)
        {
            bool skip = _skipped;
            _skipped = false;
            List<NearThought> close = (near ?? new List<NearThought>()).Where(n => n != null && n.Key != null)
                .OrderBy(n => n.Distance).ToList();
            string nearest = close.Count > 0 ? close[0].Key : null;

            // someone new walked up: switch to them straight away (after a short minimum), with a little
            // hysteresis so two people at the same distance don't flicker
            bool newcomer = false;
            if (!skip && nearest != null && nearest != _lastNearKey && (Current == null || now - Current.ShownAt >= 3000))
            {
                NearThought shown = close.FirstOrDefault(c => c.Key == _lastNearKey);
                newcomer = shown == null || close[0].Distance + 2f < shown.Distance;
            }
            if (nearest == null)
                _lastNearKey = null;
            if (now < _nextAt && !newcomer && !(skip && Current == null))
                return Current;   // the line still showing, or the blank after it
            if (Current != null && GapMs > 0 && !newcomer && !skip)
            {
                Current = null;   // a short blank before the next line, so they don't run together
                _nextAt = now + GapMs;
                return null;
            }
            if (newcomer)
            {
                _lastNearKey = nearest;
                _nearTurn = 0;
            }

            TickerLine next = null;
            // the ticker key on someone's thought moves on to the news, when the news is on the ticker at all
            bool toNews = skip && _prev != null && _prev.Kind == "near" && (_fresh.Count > 0 || _history.Count > 0 || summary != null);
            if (close.Count > 0 && !toNews)
            {
                if (!newcomer && _fresh.Count > 0 && (_prev == null || _prev.Kind == "near"))
                    next = TakeFresh();
                // the person right beside the player gets every other turn
                NearThought beside = close.FirstOrDefault(n => n.Beside);
                if (next == null && beside != null && !(_prev != null && _prev.Kind == "near" && _prev.Key == beside.Key))
                    next = NearLine(beside, now);
                for (int i = 0; next == null && i < close.Count; i++)
                {
                    NearThought who = close[(_nearTurn + i) % close.Count];
                    next = NearLine(who, now);
                    if (next != null)
                        _nearTurn = _nearTurn + i + 1;
                }
            }
            if (next == null)
            {
                if (_fresh.Count > 0)
                {
                    next = TakeFresh();
                }
                else if (_summaryNext || _history.Count == 0)
                {
                    string s = summary != null ? summary() : null;
                    next = string.IsNullOrEmpty(s) ? null : new TickerLine { Label = "RealNPCs", Text = s, Kind = "summary" };
                    _summaryNext = false;
                }
                else
                {
                    TickerLine h = _history[_history.Count - 1 - (_rotation % _history.Count)];
                    _rotation++;
                    if (_rotation % Math.Max(1, Math.Min(_history.Count, 4)) == 0)
                        _summaryNext = true;
                    next = new TickerLine { Label = h.Label, Text = h.Text, Kind = h.Kind };
                }
            }

            if (next != null && next != Current)
            {
                next.ShownAt = now;
                Current = _prev = next;
                _nextAt = now + (next.Kind == "summary" ? Math.Max(4000, SlotMs * 2 / 3) : SlotMs) + Math.Min(6000, next.Text.Length * 25);
            }
            else
            {
                if (next == null)
                    Current = null;   // nothing to say: the bar goes away rather than keep an old line up
                _nextAt = now + 2000;
            }
            return Current;
        }

        private TickerLine TakeFresh()
        {
            TickerLine t = _fresh[0];
            _fresh.RemoveAt(0);
            _history.Add(new TickerLine { Label = t.Label, Text = t.Text, Kind = t.Kind });
            if (_history.Count > HistorySize)
                _history.RemoveAt(0);
            _rotation = 0;
            return t;
        }

        private readonly Dictionary<string, KeyValuePair<int, string>> _shown = new Dictionary<string, KeyValuePair<int, string>>();

        private TickerLine NearLine(NearThought n, int now)
        {
            TickerLine line = NearLineRaw(n, now);
            if (line == null)
                return null;
            // the same thought again so soon: give the slot to the news instead
            KeyValuePair<int, string> last;
            if (_shown.TryGetValue(n.Key, out last) && last.Value == line.Text && now - last.Key < (n.Beside ? BesideCacheMs : ThoughtCacheMs))
                return null;
            _shown[n.Key] = new KeyValuePair<int, string>(now, line.Text);
            if (_shown.Count > 200)
                _shown.Remove(_shown.OrderBy(kv => kv.Value.Key).First().Key);
            return line;
        }

        private TickerLine NearLineRaw(NearThought n, int now)
        {
            KeyValuePair<int, string> cached;
            string text = null;
            if (_thoughts.TryGetValue(n.Key, out cached) && now - cached.Key < (n.Beside ? BesideCacheMs : ThoughtCacheMs) && now >= cached.Key)
            {
                text = cached.Value;
            }
            else
            {
                try
                {
                    text = n.Text != null ? n.Text() : null;
                }
                catch
                {
                    text = null;
                }
                _thoughts[n.Key] = new KeyValuePair<int, string>(now, text);
                if (_thoughts.Count > 200)
                    _thoughts.Remove(_thoughts.OrderBy(kv => kv.Value.Key).First().Key);
            }
            if (string.IsNullOrEmpty(text))
                return null;
            return new TickerLine { Label = n.Name, Text = text, Kind = "near", Key = n.Key, Handle = n.Handle };
        }

        /// Forget a cached thought (something just happened to this person).
        public void Refresh(string key)
        {
            if (key != null)
                _thoughts.Remove(key);
        }
    }
}
