// How a fully awake person who can't bear it ends it (the game side, WestworldScript.WakeActs.cs, plays it out): with a
// gun if they carry one or are the kind who keeps one, else by running at a wall when there's one to run at, else with a
// knife. Decided by who they are (their trade, sex and age, hashed with their id), so the same person would do it the
// same way; only the wall depends on where they're standing.
namespace EnrageWestworld.Core
{
    public static class Despair
    {
        public const string Knife = "WEAPON_MELEE_KNIFE";

        /// "gun", "wall" or "knife". armed: they have a gun on them already; wallNear: a wall to run at, close enough.
        public static string Method(Person p, bool armed, bool wallNear, string seed)
        {
            if (armed)
                return "gun";
            if (Voice.Chance(seed + "dg", OwnsGun(p) ? 70 : 25))
                return "gun";
            return wallNear && Voice.Chance(seed + "dw", 60) ? "wall" : "knife";
        }

        /// The kind who'd keep a gun about them or in the house: farmers, trappers, hard men, card players, the old,
        /// soldiers and the law, and gentlemen and working men.
        public static bool OwnsGun(Person p)
        {
            string g = InnerLife.TradeGroup(p != null ? p.Trade : null);
            bool woman = p != null && p.Woman;
            return g == "farm" || g == "trapper" || g == "rough" || g == "card" || g == "old" || g == "soldier" || g == "law" ||
                   (!woman && (g == "gentry" || g == "working"));
        }

        /// The gun that fits them, when they had to fetch one (the game's weapon name).
        public static string Gun(Person p)
        {
            string g = InnerLife.TradeGroup(p != null ? p.Trade : null);
            bool woman = p != null && p.Woman;
            int age = Romance.AgeOf(p);
            if (g == "card")
                return "WEAPON_PISTOL_VOLCANIC";
            if (g == "gentry" || g == "soldier" || g == "law")
                return woman ? "WEAPON_REVOLVER_DOUBLEACTION" : "WEAPON_REVOLVER_SCHOFIELD";
            if (g == "rough")
                return "WEAPON_REVOLVER_LEMAT";
            if (g == "old" || age >= 55 || g == "farm" || g == "trapper" || g == "working")
                return "WEAPON_REVOLVER_CATTLEMAN";
            return "WEAPON_REVOLVER_DOUBLEACTION";
        }

        /// The weapon in words, for the log.
        public static string Words(string weapon)
        {
            switch (weapon)
            {
                case "WEAPON_PISTOL_VOLCANIC": return "the repeating pistol (Volcanic)";
                case "WEAPON_REVOLVER_DOUBLEACTION": return "the small revolver (double-action)";
                case "WEAPON_REVOLVER_SCHOFIELD": return "the army revolver (Schofield)";
                case "WEAPON_REVOLVER_LEMAT": return "the nine-shot revolver (LeMat)";
                case "WEAPON_REVOLVER_CATTLEMAN": return "the plain revolver (Cattleman)";
                case Knife: return "a knife";
                default: return "their own gun";
            }
        }
    }
}
