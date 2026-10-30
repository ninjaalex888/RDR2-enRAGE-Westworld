// Bars for the people around you (fear of you, fondness, anger) and a corner meter for the strangers whose grudge is
// growing ([Display] in the ini; drawn by Game/WestworldScript.Feelings.cs). The numbers are enRAGE's own, 0 to
// 100, read by the Bridge; only the choosing, the order and the words are here. All words are original to this add-on.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public sealed class FeelingBar
    {
        public string Label;
        public float Share;   // 0..1 of the bar filled
        public int R, G, B;
    }

    public sealed class GrudgeRow
    {
        public int Handle;     // the ped, 0 if unknown
        public string Who;     // the name to show (filled by the game side)
        public float Score;    // 0..100: a stranger's grudge, or a nemesis's hate
        public bool Nemesis;   // already a nemesis, and after you now
        public int At;         // when it last grew (game time, ms), for the order among equals
    }

    public static class Feelings
    {
        public const string Title = "Sore at you";

        /// Below this a feeling is only noise, and gets no bar.
        public const float ShowFrom = 3f;

        /// The bars for one person: fear of you, fondness for you ("Liking" until Lex's word, 2026-10-30), anger at you; only those worth showing, in that order.
        public static List<FeelingBar> Bars(float fear, float like, float anger)
        {
            List<FeelingBar> bars = new List<FeelingBar>();
            Add(bars, "Fear", fear, 120, 160, 225);
            Add(bars, "Fondness", like, 110, 190, 110);
            Add(bars, "Anger", anger, 215, 75, 55);
            return bars;
        }

        private static void Add(List<FeelingBar> bars, string label, float value, int r, int g, int b)
        {
            if (value >= ShowFrom)
                bars.Add(new FeelingBar { Label = label, Share = Math.Min(1f, value / 100f), R = r, G = g, B = b });
        }

        /// The meter's rows: the nemesis after you first, then the strangers closest to the line, newest first among
        /// equals; at most `max`, and none with nothing against you.
        public static List<GrudgeRow> Meter(IEnumerable<GrudgeRow> rows, int max)
        {
            return rows.Where(r => r != null && r.Score > 0f).OrderByDescending(r => r.Nemesis).ThenByDescending(r => r.Score)
                       .ThenByDescending(r => r.At).Take(max).ToList();
        }

        /// Where the line that turns a stranger into a nemesis sits along a bar (0..1), or -1 if it isn't known.
        public static float LineAt(int threshold)
        {
            return threshold > 0 && threshold <= 100 ? threshold / 100f : -1f;
        }

        public static string RowText(GrudgeRow r)
        {
            return r.Nemesis ? r.Who + ", out to get you" : r.Who;
        }

        /// How big the bars are drawn at this distance: full size close by, smaller further off.
        public static float SizeAt(float metres)
        {
            return Math.Max(0.6f, Math.Min(1f, 1.15f - metres / 25f));
        }

        /// Every word shown, for the tests.
        public static IEnumerable<string> AllLines()
        {
            return new[] { Title, "Fear", "Fondness", "Anger", "{name}, out to get you" };
        }
    }
}
