// How someone takes the player's flirting: from what they think of him (Regard, the same judgement as their thoughts
// about him), whether they're drawn to men at all, whether they're spoken for, their day (sore from a bad turn, glad
// from a good one, a worry at its worst) and their nature. No dice: the same person in the same state answers the
// same way.
namespace EnrageWestworld.Core
{
    public static class Interest
    {
        public sealed class Ask
        {
            public string Regard;      // Regard.Of...
            public bool Drawn;         // drawn to his sex, near enough his age
            public bool Taken;         // a sweetheart or a spouse of their own
            public bool AtWorst;       // a worry of their own at its worst today
            public bool Spam;          // he flirted a moment ago
            public string Temper;      // their temperament, or null
            public string Mood;        // "sore", "glad" or null (InnerLife.MoodOf)
        }

        /// "yes", "maybe" or "no"; why: the reason the answer is given by (Romance.Rebuff's reasons for a no, else the regard).
        public static string Answer(Ask a, out string why)
        {
            if (a.Spam)
                return No("spam", out why);
            if (!a.Drawn)
                return No("notdrawn", out why);
            switch (a.Regard)
            {
                case "scorned": return No("scorned", out why);
                case "hostile": return No("hate", out why);
                case "afraid": return No("fear", out why);
                case "witnessed": return No("badrep", out why);
            }
            bool smitten = a.Regard == "smitten";
            if (a.Taken && !smitten)
                return No("married", out why);
            if (a.AtWorst && !smitten)
                return No("busy", out why);
            int warmth = Warmth(a.Regard) + Lean(a.Temper) + (a.Mood == "glad" ? 1 : a.Mood == "sore" ? -1 : 0);
            why = a.Regard;
            if (warmth >= 3)
                return "yes";
            if (warmth >= 1)
                return "maybe";
            return No(a.Mood == "sore" ? "sore" : a.Regard == "wary" ? "badrep" : "cold", out why);
        }

        private static string No(string reason, out string why)
        {
            why = reason;
            return "no";
        }

        private static int Warmth(string regard)
        {
            switch (regard)
            {
                case "smitten": return 5;
                case "grateful":
                case "fond": return 3;
                case "met":
                case "stranger": return 1;
                default: return 0;   // wary
            }
        }

        /// The bold and the restless are readier; the cold and the proud harder to win.
        private static int Lean(string temper)
        {
            return temper == "Hothead" || temper == "Drifter" ? 1 : temper == "Cold" || temper == "Proud" ? -1 : 0;
        }
    }
}
