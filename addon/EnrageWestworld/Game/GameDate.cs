// The game's date and clock in minutes, of our own (2026-10-29, roadmap 34 phase A: standing alone). Until now the
// add-on read enRAGE's GameDate and its clock delegate; every saved game minute (ties, memories, worries, the ledger)
// counts from this epoch, so the arithmetic is enRAGE's exactly: minutes since 1890-01-01, the day from the game's
// clock year, month (0-based) and day. Adapted from enRAGE's GameDate (its author permits reuse; credited here).
using System;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public static class GameDate
    {
        public static readonly DateTime Epoch = new DateTime(1890, 1, 1);

        public static readonly string[] Months = { "Jan", "Feb", "Mar", "Apr", "May", "Jun", "Jul", "Aug", "Sep", "Oct", "Nov", "Dec" };

        public static string Month(int month1)
        {
            return Months[Math.Max(1, Math.Min(12, month1)) - 1];
        }

        public static long ToMinutes(DateTime d)
        {
            return (long)(d - Epoch).TotalMinutes;
        }

        public static DateTime FromMinutes(long m)
        {
            return Epoch.AddMinutes(m);
        }

        /// year * 10000 + (month - 1) * 100 + day, as enRAGE keys its days.
        public static int DayKey(DateTime d)
        {
            return d.Year * 10000 + (d.Month - 1) * 100 + d.Day;
        }

        public static DateTime FromDayKey(int key)
        {
            int year = Math.Max(1, key / 10000);
            int month = Math.Max(1, Math.Min(12, key / 100 % 100 + 1));
            return new DateTime(year, month, Math.Max(1, Math.Min(DateTime.DaysInMonth(year, month), key % 100)));
        }

        /// Today's key from the game's clock (its month is 0-based, as the key wants it).
        public static int TodayKey()
        {
            return CLOCK.GET_CLOCK_YEAR() * 10000 + CLOCK.GET_CLOCK_MONTH() * 100 + CLOCK.GET_CLOCK_DAY_OF_MONTH();
        }

        private static int _minutesFrame = -1;
        private static long _minutesCache;

        /// The game minute now: the day's minutes since the epoch plus the clock's hour and minute, once a frame (what
        /// enRAGE's clock gave; with it present the bridge reads its delegate instead, so both count alike).
        public static long MinutesNow()
        {
            int frame = Game.FrameCount;
            if (frame == _minutesFrame)
                return _minutesCache;
            _minutesFrame = frame;
            try
            {
                _minutesCache = ToMinutes(FromDayKey(TodayKey())) + CLOCK.GET_CLOCK_HOURS() * 60 + CLOCK.GET_CLOCK_MINUTES();
            }
            catch
            {
                _minutesCache = 0;
            }
            return _minutesCache;
        }
    }
}
