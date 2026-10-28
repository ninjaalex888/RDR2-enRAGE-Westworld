// The levels of detail and the frame budget (the mind design's step 7; night run 2026-10-28, item 7; behind
// [Mind] MindLevels, off: on, the shadow's scan runs from this scheduler; off, exactly the scan as it was). Level 0,
// near: within 20 m and on screen, at most 20 people: a look every 750 ms (who they are, their traits, their needs,
// the MIND traits line); the appraisal and the choosing run as their own steps over everyone within 20 m, screen or
// not, as before. Level 1, about: the town around (within 120 m), who they are and their needs every 10 to 30 s each
// (a nobody half as often). Level 2, away: everyone saved elsewhere, a town a game day, worked out from id and day and
// never stored (counted, never written). Level 3, the untouched: nothing. A budget of work per frame: a frame stops
// handing out work when its share is spent and the rest waits for the next; hysteresis on level changes (3 m past a
// ring's edge to cross it, two seconds off screen before the near ring lets go, a full ring not entered unless nearer
// than its furthest by the margin), so nobody flickers between levels at a boundary. Keyed by the body's handle (who
// it is comes later, when their work runs). Reviewed before the commit (a subagent): the grace off screen, the ring's
// cap, the interval kept on the entry, lateness counted at the settle, the clock going back. Plain C#, no game.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public sealed class LevelEntry
    {
        public int Handle;            // the body (the game side's)
        public string Id;             // who, once known (null until their first work, or a nobody)
        public bool Nobody;           // looked up and nobody the add-on knows: visited half as often
        public float Metres;
        public bool InView;           // on screen (held two seconds after the last sight while near)
        public long InViewAt;         // when last seen on screen (ms)
        public int Level = Levels.Untouched;
        public long RanAt;            // when its work last ran (ms)
        public long ChangedAt;        // when its level last changed (ms)
        public long SeenAt;           // when the scan last saw them (ms)
        public long Due;              // when its next work is due (ms)
        public long IntervalMs;       // the about ring's own interval for this entry (10-30 s), fixed at creation
        public int Changes;           // level changes, for the test
        public bool Late;             // counted as late at the last settle
    }

    public sealed class Levels
    {
        public const int Near = 0, About = 1, Away = 2, Untouched = 3;
        public const float NearMetres = 20f, AboutMetres = 120f, Hysteresis = 3f;
        public const int NearMax = 20, NearAllMax = 40;
        public const int NearEveryMs = 750, AboutEveryMinMs = 10000, AboutEveryMaxMs = 30000, GraceMs = 2000;
        public const long AwayEveryGameMin = 1440;   // a town a game day
        public const int DefaultBudgetMicros = 200;  // the mind's share of a frame (the brief: 0.2 ms a frame with 20 near)

        private readonly Dictionary<int, LevelEntry> _people = new Dictionary<int, LevelEntry>();
        private readonly List<LevelEntry> _near = new List<LevelEntry>(), _about = new List<LevelEntry>(), _sort = new List<LevelEntry>(), _nearAll = new List<LevelEntry>();
        private readonly List<int> _drop = new List<int>();
        private readonly Dictionary<string, long> _awayDone = new Dictionary<string, long>();   // town -> the game day its away pass ran
        private long _frameSpent;
        private float _nearFurthest = float.MaxValue;   // the furthest in the near ring at the last settle (MaxValue: room)
        private int _nearCount;
        public long BudgetMicros = DefaultBudgetMicros;
        public int Cap = 2000;
        public long Spent { get; private set; }        // microseconds of mind work, all told
        public int Frames { get; private set; }
        public int Starved { get; private set; }       // visits late past twice their interval (counted at the settle, once per lateness)
        public int Ran { get; private set; }
        public long PeakMicros { get; private set; }   // the most one frame spent
        public int Deferred { get; private set; }      // someone due put off because the frame's share was spent
        public int Changes { get; private set; }       // level changes, all told
        public int Capped { get; private set; }        // kept out of a full near ring

        public int Count
        {
            get { return _people.Count; }
        }

        public int NearCount
        {
            get { return _near.Count; }
        }

        public int AboutCount
        {
            get { return _about.Count; }
        }

        /// The near ring, nearest first (after Settle): on screen within 20 m, at most NearMax.
        public List<LevelEntry> NearRing
        {
            get { return _near; }
        }

        /// Everyone within 20 m, on screen or not, nearest first (after Settle; at most NearAllMax): the appraisal's and
        /// the choosing's people, as the old scan gave them.
        public List<LevelEntry> NearAll
        {
            get { return _nearAll; }
        }

        public LevelEntry Of(int handle)
        {
            LevelEntry e;
            return _people.TryGetValue(handle, out e) ? e : null;
        }

        /// The about ring's interval for a handle (10-30 s, each their own, from the handle alone: no string).
        public static long IntervalFor(int handle)
        {
            uint x = (uint)handle * 2654435761u;
            x ^= x >> 13;
            x *= 0x5bd1e995u;
            x ^= x >> 15;
            return AboutEveryMinMs + (long)(x % (uint)(AboutEveryMaxMs - AboutEveryMinMs));
        }

        /// Tells the scheduler where someone is this scan (metres from the player, on screen or not). Their level
        /// follows with hysteresis: into the near ring under 20 m and on screen (only while the ring has room, or nearer
        /// than its furthest by 3 m), out of it past 23 m or two seconds off screen; into the about ring under 120 m,
        /// out past 123. Entries past the cap are refused.
        public LevelEntry Place(int handle, float metres, bool inView, long now)
        {
            LevelEntry e;
            if (!_people.TryGetValue(handle, out e))
            {
                if (_people.Count >= Cap)
                    return null;
                _people[handle] = e = new LevelEntry { Handle = handle, ChangedAt = now, Due = now, IntervalMs = IntervalFor(handle), InViewAt = -GraceMs - 1 };
            }
            e.Metres = metres;
            e.SeenAt = now;
            if (inView)
                e.InViewAt = now;
            e.InView = inView || e.Level == Near && now - e.InViewAt < GraceMs;   // the near ring holds on through a glance away
            int want;
            if (e.Level == Near)
                want = metres <= NearMetres + Hysteresis && e.InView ? Near : metres <= AboutMetres ? About : Untouched;
            else if (metres <= NearMetres && e.InView && (_nearCount < NearMax || metres + Hysteresis < _nearFurthest))
                want = Near;
            else
                want = metres <= AboutMetres + (e.Level == About ? Hysteresis : 0f) ? About : Untouched;
            if (want != e.Level)
                Change(e, want, now);
            return e;
        }

        // a level change: stepping up, the first work in the new level comes at once; stepping down, what's due stays due
        private void Change(LevelEntry e, int level, long now)
        {
            if (level < e.Level)
                e.Due = now;
            e.Level = level;
            e.ChangedAt = now;
            e.Changes++;
            Changes++;
        }

        /// After a scan: whoever the scan didn't see drops out (and is forgotten after a minute), the near ring is the
        /// nearest NearMax at level 0 (the rest are kept out, at about, without a level change), the about ring the
        /// rest; everyone within 20 m goes on NearAll; the late are counted.
        public void Settle(long now)
        {
            _near.Clear();
            _about.Clear();
            _sort.Clear();
            _nearAll.Clear();
            _drop.Clear();
            foreach (LevelEntry e in _people.Values)
            {
                if (e.SeenAt != now)
                {
                    if (e.Level != Untouched)
                        Change(e, Untouched, now);
                    if (now - e.SeenAt > 60000 || now < e.SeenAt)
                        _drop.Add(e.Handle);
                    continue;
                }
                if (e.Level == Near)
                    _sort.Add(e);
                else if (e.Level == About)
                    _about.Add(e);
                if (e.Metres <= NearMetres)
                    _nearAll.Add(e);
            }
            foreach (int h in _drop)
                _people.Remove(h);
            _sort.Sort(ByMetres);
            for (int i = 0; i < _sort.Count; i++)
            {
                if (i < NearMax)
                    _near.Add(_sort[i]);
                else
                {
                    _sort[i].Level = About;   // the ring is full: the furthest wait at about, no change counted, their due kept
                    _about.Add(_sort[i]);
                    Capped++;
                }
            }
            _nearCount = _near.Count;
            _nearFurthest = _near.Count >= NearMax ? _near[_near.Count - 1].Metres : float.MaxValue;
            _nearAll.Sort(ByMetres);
            if (_nearAll.Count > NearAllMax)
                _nearAll.RemoveRange(NearAllMax, _nearAll.Count - NearAllMax);
            // the late: a visit past twice its interval, counted once per lateness
            CountLate(_near, Near, now);
            CountLate(_about, About, now);
        }

        private static readonly Comparison<LevelEntry> ByMetres = (a, b) => a.Metres.CompareTo(b.Metres);

        private void CountLate(List<LevelEntry> ring, int level, long now)
        {
            for (int i = 0; i < ring.Count; i++)
            {
                LevelEntry e = ring[i];
                long since = now - Math.Max(e.RanAt, e.ChangedAt);
                bool late = e.RanAt != 0 && since > 2 * Interval(e, level);
                if (late && !e.Late)
                    Starved++;
                e.Late = late;
            }
        }

        /// The frame begins: its share of work is fresh.
        public void BeginFrame()
        {
            _frameSpent = 0;
            Frames++;
        }

        /// The next person at this level whose work is due, or null (nothing due, or the frame's share spent). The
        /// caller runs their work and reports its cost with Spend. Within a ring the longest-waiting goes first (one
        /// walk of the ring per call: with 20 near and a few hundred about, a few hundred compares a frame).
        public LevelEntry Next(int level, long now)
        {
            List<LevelEntry> ring = level == Near ? _near : _about;
            if (_frameSpent >= BudgetMicros)
            {
                for (int i = 0; i < ring.Count; i++)
                    if (ring[i].Level == level && IsDue(ring[i], now))
                    {
                        Deferred++;   // someone due, put off
                        break;
                    }
                return null;
            }
            LevelEntry best = null;
            for (int i = 0; i < ring.Count; i++)
            {
                LevelEntry e = ring[i];
                if (e.Level != level || !IsDue(e, now))
                    continue;
                if (best == null || e.Due < best.Due)
                    best = e;
            }
            if (best == null)
                return null;
            best.RanAt = now;
            best.Due = now + Interval(best, level);
            best.Late = false;
            Ran++;
            return best;
        }

        private static bool IsDue(LevelEntry e, long now)
        {
            return now >= e.Due || now < e.RanAt;   // the clock gone back: due
        }

        /// How often this person's work runs at this level: near every 750 ms; about every 10 to 30 s, their own; a
        /// nobody half as often.
        public static long Interval(LevelEntry e, int level)
        {
            return level == Near ? NearEveryMs : e.IntervalMs * (e.Nobody ? 2 : 1);
        }

        /// The cost of the work just run, in microseconds.
        public void Spend(long micros)
        {
            _frameSpent += micros;
            Spent += micros;
            if (_frameSpent > PeakMicros)
                PeakMicros = _frameSpent;
        }

        public long FrameSpent
        {
            get { return _frameSpent; }
        }

        public bool FrameHasRoom
        {
            get { return _frameSpent < BudgetMicros; }
        }

        /// Whether the away pass for a town is due (a town a game day), and marks it run.
        public bool AwayDue(string town, long gameMin)
        {
            if (town == null || gameMin < 0)
                return false;
            long day = gameMin / AwayEveryGameMin, last;
            if (_awayDone.TryGetValue(town, out last) && last == day)
                return false;
            _awayDone[town] = day;
            return true;
        }

        /// What one person away from the player does with the day, worked out from their id and the day, never stored:
        /// a worry that ended today (the arc moved on: InnerLife's own arithmetic, one string hash and no Person), the
        /// ties older than a week cooled (counted; warmth is saved, so nothing is written), a life event (one in fifty,
        /// from the same hash). For the accounting line only.
        public static void AwayDay(string id, long gameDay, int tiesOlderThanWeek, ref int worriesEnded, ref int tiesCooled, ref int lifeEvents)
        {
            if (id == null)
                return;
            long full = (long)(Voice.Hash(id + "|wo") & 0x7fffffff);   // the one allocation: the same hash ArcFor uses
            long h = full % InnerLife.WorryDays;
            if ((gameDay + h) / InnerLife.WorryDays != (gameDay - 1 + h) / InnerLife.WorryDays)
                worriesEnded++;
            tiesCooled += tiesOlderThanWeek;
            if ((full / InnerLife.WorryDays + gameDay * 7919) % 50 == 0)
                lifeEvents++;
        }

        public void Clear()
        {
            _people.Clear();
            _near.Clear();
            _about.Clear();
            _sort.Clear();
            _nearAll.Clear();
            _awayDone.Clear();
            _frameSpent = 0;
            _nearCount = 0;
            _nearFurthest = float.MaxValue;
        }
    }
}
