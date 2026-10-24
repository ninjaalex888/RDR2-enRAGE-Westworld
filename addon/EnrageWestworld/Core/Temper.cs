// Personality in the voice: the same line said by a hothead, a coward, a proud man, a cold one, a greedy
// one, a loyal one or a drifter comes out a little differently. Applied sparingly to spoken lines.
using System.Text.RegularExpressions;

namespace EnrageWestworld.Core
{
    public static class Temper
    {
        private static readonly Regex FirstSentence = new Regex(@"^(.+?[.!?])(\s|$)");

        public static string Color(Person p, string line, string seed)
        {
            if (p == null || string.IsNullOrEmpty(line) || line.StartsWith("YOU:") || line.Split(' ').Length < 6 || line.EndsWith("?") || line.EndsWith("..."))
                return line;
            if (!Voice.Chance(seed + "temper", 35))
                return line;
            string t = line.TrimEnd();
            switch (p.Personality)
            {
                case "Hothead":
                    return Voice.Chance(seed + "h", 50) ? Exclaim(t) : Voice.Pick(seed, "Damn it, ", "Hell, ", "Listen here. ") + Grammar.LowerFirst(t);
                case "Coward":
                    return Voice.Chance(seed + "c", 50) ? Stammer(t) : t + Voice.Pick(seed, " Please.", " I ain't looking for trouble.", " Don't hurt me.");
                case "Proud":
                    return t + Voice.Pick(seed, " Remember that.", " Not that you'd understand.", " Mark my words.");
                case "Cold":
                    Match m = FirstSentence.Match(t);
                    return m.Success && m.Groups[1].Value.Length < t.Length ? m.Groups[1].Value : t;
                case "Greedy":
                    return t + Voice.Pick(seed, " Time's money.", " What's in it for me, though?", " Nothing's free, friend.");
                case "Loyal":
                    return t + Voice.Pick(seed, " On my word.", " The boys would say the same.", " I keep my promises.");
                case "Drifter":
                    return Voice.Chance(seed + "d", 50) ? "Way I see it, " + Grammar.LowerFirst(t) : t + Voice.Pick(seed, " Then again, I won't be here long.", " Road's calling, anyhow.");
            }
            if (p.Has("braggart"))
                return t + " Ask anybody.";
            if (p.Has("superstitious"))
                return t + Voice.Pick(seed, " Knock wood.", " God forbid.");
            if (p.Has("gambler"))
                return t + " I'd bet on it.";
            if (p.Has("honest"))
                return t + " That's the truth.";
            if (p.Friendly)
                return t.EndsWith(".") ? t.Substring(0, t.Length - 1) + ", friend." : t;
            return line;
        }

        private static string Exclaim(string t)
        {
            return t.EndsWith(".") ? t.Substring(0, t.Length - 1) + "!" : t;
        }

        private static string Stammer(string t)
        {
            int sp = t.IndexOf(' ');
            if (sp <= 0 || sp > 8)
                return t;
            string first = t.Substring(0, sp);
            return first + "... " + Grammar.LowerFirst(t);
        }
    }
}
