// enRAGE's territory changes, as it writes them in its own news list: a tag, the sentence, then the gang's share of
// the district before and after in brackets. RealNPCs passes the sentence on where gang and faction news goes
// (WestworldScript.Territory.cs): the game's top-left updates, by default.
using System;
using System.Text.RegularExpressions;

namespace EnrageWestworld.Core
{
    public static class Territory
    {
        private const string Tag = "TERRITORY ";
        private static readonly Regex Shares = new Regex(@"\s*\(\s*-?\d+\s+to\s+-?\d+\s*\)\s*$");

        /// The sentence, if this news line is a territory change; else null.
        public static string FromFeed(string line)
        {
            if (line == null || !line.StartsWith(Tag, StringComparison.Ordinal))
                return null;
            string s = Shares.Replace(line.Substring(Tag.Length), "").Trim();
            return s.Length == 0 ? null : s;
        }
    }
}
