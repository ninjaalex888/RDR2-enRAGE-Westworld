// Needs and concerns: the NPC mind's hours-and-days layers (the mind design, step 3; daytime run 2026-10-27, shadow
// mode). Needs are how hard each of the body's and the heart's wants presses now (0 none, 1 nothing else matters):
// hunger, rest, warmth and dry, money for the week, safety, company, standing, meaning, and a vice's pull for those
// who have one. Worked out when asked, never stored: from the hour (meals and sleep by trade), the weekday (payday, the
// rent), the weather and the place, their trade and money, the worry they carry, their traits, and what has just
// happened to them (their fresh memories). Off screen, people are taken to keep their day (they ate at noon), the doc's
// "settled quietly". Concerns are the one to three live goals they carry: the worry first (InnerLife's 14, read as they
// are: nothing they produce changes), a worry link's other end, an enRAGE record's goal. Plain C#, no game.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    /// What the needs are worked out from, beyond the person and their traits.
    public sealed class NeedContext
    {
        public int Hour, Minute;                // the game clock
        public int Weekday = -1;                // 0 Sunday .. 6 Saturday; -1 unknown
        public long GameDay = -1;
        public long Now = -1;                   // game minute, for how old their memories are
        public string Weather;                  // rain storm snow dust fog sun grey, or null
        public bool Indoors;
        public string Place;                    // saloon church store... (null: the street or the wilds)
        public bool Drunk;
        public int Others;                      // people in sight of them (0: alone)
        public bool Feared;                     // someone they fear is near (the player they saw do violence, a foe)
        public int WakeStage;                   // the Awakening (0-4)
        public long AteAt = -1;                 // the game minute they last ate, when it's known (Choosing's eat); -1: their meal hours alone
        public List<MindMemory> Memories;       // theirs, if they keep any (null for the unsaved)
    }

    /// The nine, each 0..1.
    public sealed class NeedState
    {
        public const int Hunger = 0, Rest = 1, Warmth = 2, Money = 3, Safety = 4, Company = 5, Standing = 6, Meaning = 7, Vice = 8, Count = 9;
        public static readonly string[] Names = { "hunger", "rest", "warmth and dry", "money for the week", "safety", "company", "standing", "meaning", "a vice's pull" };
        // how a need sounds when it presses (for the log, the card and, later, thoughts)
        public static readonly string[] Pressing = { "hungry", "tired", "cold and wet", "short of money", "afraid for themselves", "lonely", "stung in their pride", "searching for a reason", "craving" };
        public readonly float[] V = new float[Count];
        public string ViceName;                 // drink or cards, when they have one

        public float this[int i] { get { return V[i]; } }

        /// The need pressing hardest, its index (-1 when none presses past 0.5).
        public int Top(float over = 0.5f)
        {
            int best = -1;
            float top = over;
            for (int i = 0; i < Count; i++)
                if (V[i] > top)
                {
                    top = V[i];
                    best = i;
                }
            return best;
        }

        /// "hungry and tired" (the two pressing hardest past 0.5), or "nothing pressing".
        public string Words()
        {
            int a = Top(), b = -1;
            if (a < 0)
                return "nothing pressing";
            float top = 0.5f;
            for (int i = 0; i < Count; i++)
                if (i != a && V[i] > top)
                {
                    top = V[i];
                    b = i;
                }
            string first = a == Vice && ViceName != null ? "craving " + ViceName : Pressing[a];
            string second = b < 0 ? null : b == Vice && ViceName != null ? "craving " + ViceName : Pressing[b];
            return second == null ? first : first + " and " + second;
        }

        public string Numbers()
        {
            var parts = new string[Count];
            for (int i = 0; i < Count; i++)
                parts[i] = Names[i].Split(' ')[0] + " " + V[i].ToString("0.0");
            return string.Join(", ", parts);
        }
    }

    /// A live goal.
    public sealed class Concern
    {
        public string Kind;      // worry:<kind>, link:<kind>, goal:<type>
        public int Stage;        // a worry's stage (0 it begins, 1 worse, 2 the crisis, 3 how it ended); -1 none
        public bool Good;        // a worry that ends (or ended) well
        public float Urgency;    // 0..1
        public string Words;     // "the bank's note", "a sick child"
    }

    public static class Needs
    {
        // meals by trade: country people eat earlier (breakfast, dinner at noon, supper)
        private static readonly int[] TownMeals = { 7, 12, 18 }, CountryMeals = { 5, 11, 17 };
        private static readonly string[] Poor = { "farm", "working", "girl", "trapper", "rough", "card", "old" };   // "plain" (no trade known) counts as middling
        private static readonly string[] Comfortable = { "gentry" };

        /// The needs now. Pure: the same inputs give the same needs.
        public static NeedState Of(Person p, Traits t, NeedContext c)
        {
            var n = new NeedState();
            if (p == null || c == null)
                return n;
            string g = InnerLife.TradeGroup(p.Trade);
            bool country = g == "farm" || g == "trapper" || g == "old";
            float hour = c.Hour + c.Minute / 60f;
            float z(int i) => t != null ? t.Z[i] : 0f;
            // hunger: since the last meal of their day, a little over the hours; low just after eating; asleep, flat. A meal
            // they're known to have eaten since (AteAt) counts too (Lex, 2026-10-27: eating lowers hunger)
            int[] meals = country ? CountryMeals : TownMeals;
            float since = HoursSinceMeal(hour, meals);
            if (c.AteAt >= 0 && c.Now >= c.AteAt)
                since = Math.Min(since, (c.Now - c.AteAt) / 60f);
            float wakeAt = country ? 5f : 6f;
            if (hour < meals[0] + 0.5f && since > hour + 24f - 22f)
                since -= (2f + wakeAt) * 0.75f;   // before the first meal: the night's sleep counts a quarter (hunger is entrained to mealtimes and suppressed in sleep: the review, Cummings 2001), so a man wakes at about 0.6, not "nothing else matters"
            bool asleep = hour >= 22f || hour < wakeAt;
            n.V[NeedState.Hunger] = asleep ? 0.25f : Clamp(0.08f + since * (Array.IndexOf(Poor, g) >= 0 ? 0.125f : 0.10f));   // 0.16 an hour until 2026-10-28: everyone in town sat at 0.85+ for two hours before every meal (the psychologist's review); the poor feel it more
            // rest: the hours awake (up at 5 or 6), worst before dawn; drink tires
            float wake = country ? 5f : 6f, up = hour >= wake ? hour - wake : hour + 24f - wake;
            n.V[NeedState.Rest] = Clamp(0.05f + Math.Max(0f, up - 3f) * 0.055f + (c.Drunk ? 0.15f : 0f));
            // warmth and dry: the weather out of doors, the night, less under a roof
            float wet = c.Indoors ? 0f : c.Weather == "storm" ? 0.75f : c.Weather == "rain" ? 0.55f : c.Weather == "snow" ? 0.7f : c.Weather == "fog" ? 0.2f : c.Weather == "dust" ? 0.25f : 0.05f;
            float night = !c.Indoors && (hour >= 21f || hour < 6f) ? 0.15f : 0f;
            n.V[NeedState.Warmth] = Clamp(wet + night);
            // money for the week: the poorer the trade the more; payday Saturday, the rent at the week's end; the worry
            float baseMoney = Array.IndexOf(Comfortable, g) >= 0 ? 0.08f : Array.IndexOf(Poor, g) >= 0 ? 0.32f : 0.2f;
            if (c.Weekday >= 0 && Array.IndexOf(Comfortable, g) < 0)
                baseMoney += c.Weekday == 6 ? -0.12f : c.Weekday == 0 ? -0.06f : 0.03f * c.Weekday;   // Monday to Friday it tightens; Saturday's wages ease it
            n.V[NeedState.Money] = Clamp(baseMoney - 0.06f * z(Traits.Honesty));   // the sly feel the pinch more
            // safety: violence near them lately, night out of doors, someone they fear; anxiety sees more of it
            float danger = 0.05f + night * 0.6f + (c.Feared ? 0.35f : 0f);
            // standing, company, meaning: who they are first
            float standing = 0.15f + 0.08f * z(Traits.Assertiveness) - 0.06f * z(Traits.Honesty);
            float company = 0.2f + 0.1f * z(Traits.Sociability) + (c.Others == 0 ? 0.15f : c.Others >= 4 ? -0.1f : 0f) + (p.Family == "widower" ? 0.15f : 0f);
            float meaning = 0.12f + 0.06f * z(Traits.Piety) + (c.Weekday == 0 ? 0.08f * Math.Max(0f, z(Traits.Piety)) : 0f) + 0.16f * Math.Max(0, Math.Min(4, c.WakeStage));
            // what just happened to them (their memories, fresher weighs more)
            if (c.Memories != null && c.Now >= 0)
                foreach (MindMemory m in c.Memories)
                {
                    long at = Math.Max(m.At, m.DueAt);
                    if (at > c.Now)
                        continue;   // word hasn't reached them yet
                    float hours = (c.Now - at) / 60f;
                    bool toThem = m.Role == "done" || m.How == "did" && m.Role == null;
                    switch (m.Kind)
                    {
                        case "deed:violence":
                        case "blows":
                            if (m.How == "saw" || toThem)
                                danger += (toThem ? 0.5f : 0.4f) * Half(hours, 2f);
                            if (m.Kind == "blows" && m.Role == "done")
                                standing += 0.3f * Half(hours, 10f);
                            break;
                        case "theft":
                        case "deed:theft":
                            if (m.Role == "done" || m.Sub == "robbery")
                                n.V[NeedState.Money] = Or(n.V[NeedState.Money], 0.3f * Half(hours, 24f));
                            if (m.Sub == "robbery" && (m.Role == "done" || toThem))
                                danger += 0.4f * Half(hours, 6f);   // held up at gunpoint: the door barred that night (the psychologist's review, 2026-10-28)
                            break;
                        case "slight":
                        case "quarrel":
                            if (m.Role == "done" || m.How == "did")
                                standing += 0.3f * Half(hours, 8f);
                            break;
                        case "death":
                            float grief = m.Sub == "friend" ? 1f : m.Sub == "known" ? 0.4f : 0.2f;
                            company += 0.25f * grief * Half(hours, 72f);
                            meaning += 0.3f * grief * Half(hours, 72f);
                            if (m.How == "saw")
                                danger += 0.35f * Half(hours, 2f);
                            break;
                        case "warmth":
                            company -= 0.15f * Half(hours, 6f);
                            break;
                    }
                }
            n.V[NeedState.Safety] = Clamp(danger * (1f + 0.3f * z(Traits.Anxiety)));
            n.V[NeedState.Standing] = Clamp(standing);
            n.V[NeedState.Company] = Clamp(company);
            n.V[NeedState.Meaning] = Clamp(meaning);
            // a vice: drink or cards, for those who have one; it pulls in the evening, near a saloon, after bad news
            string vice = ViceOf(p, c.GameDay);
            if (vice != null)
            {
                n.ViceName = vice;
                bool evening = hour >= 17f || hour < 2f;
                float pull = 0.2f + (evening ? 0.25f : 0f) + (c.Place == "saloon" ? 0.2f : 0f) + (c.Drunk ? 0.15f : 0f) - 0.08f * z(Traits.SelfControl);
                string wk; int ws; bool wg;
                if (c.GameDay >= 0 && InnerLife.WorryNow(p, c.GameDay, out wk, out ws, out wg) && ws == 3 && !wg)
                    pull += 0.2f;   // a worry that ended badly: the bottle or the table calls louder
                n.V[NeedState.Vice] = Clamp(pull);
            }
            // the worry presses on money where it's about money
            string kind; int stage; bool good;
            if (c.GameDay >= 0 && InnerLife.WorryNow(p, c.GameDay, out kind, out stage, out good) && stage < 3 && Array.IndexOf(MoneyWorries, kind) >= 0)
                n.V[NeedState.Money] = Or(n.V[NeedState.Money], 0.1f * (stage + 1));
            return n;
        }

        private static readonly string[] MoneyWorries = { "debt", "bank", "cards", "savings", "drought" };

        /// Their vice, or null: drink for a drinker (a quirk, or the drink worry), cards for a gambler (a quirk, or the
        /// cards worry).
        public static string ViceOf(Person p, long gameDay)
        {
            if (p == null)
                return null;
            if (p.Quirks != null && p.Quirks.Contains("drunk"))
                return "drink";
            if (p.Quirks != null && p.Quirks.Contains("gambler"))
                return "cards";
            string kind; int stage; bool good;
            if (gameDay >= 0 && InnerLife.WorryNow(p, gameDay, out kind, out stage, out good))
            {
                if (kind == "drink")
                    return "drink";
                if (kind == "cards")
                    return "cards";
            }
            return null;
        }

        // hours since the last meal of their day (the last meal before now, wrapping to yesterday's supper)
        private static float HoursSinceMeal(float hour, int[] meals)
        {
            float last = -1f;
            foreach (int m in meals)
                if (hour >= m + 0.5f)
                    last = m + 0.5f;
            return last < 0f ? hour + 24f - (meals[meals.Length - 1] + 0.5f) : hour - last;
        }

        // two pressures together: the second takes its share of what the first left (they near the top, never slam into it)
        private static float Or(float a, float b)
        {
            return Clamp(1f - (1f - Clamp(a)) * (1f - Clamp(b)));
        }

        private static float Half(float hours, float halfLife)
        {
            return hours < 0f ? 1f : (float)Math.Pow(0.5, hours / halfLife);
        }

        private static float Clamp(float v)
        {
            return v < 0f ? 0f : v > 1f ? 1f : v;
        }

        // ---- concerns ------------------------------------------------------------------------------------

        /// What each worry is about, in a few words ("worried about the bank's note").
        public static readonly Dictionary<string, string> WorryWords = new Dictionary<string, string>
        {
            { "debt", "a debt" }, { "fever", "a sick child" }, { "letter", "a letter that doesn't come" }, { "drought", "the dry spell" }, { "son", "their son away" },
            { "courting", "a courtship" }, { "feud", "a feud with a neighbour" }, { "bank", "the bank's note" }, { "drink", "the drink" }, { "savings", "money put by" },
            { "tooth", "a bad tooth" }, { "secret", "a secret" }, { "law", "the law" }, { "cards", "card debts and the rent" }
        };
        private static readonly string[] StageWords = { "it begins", "worse", "the crisis", "" };

        /// The one to three live goals they carry, the most urgent first: the worry (as InnerLife has it, unchanged),
        /// the person at a worry link's other end, an enRAGE record's goals.
        public static List<Concern> Of(Person p, long gameDay, List<WorryLink> linked = null)
        {
            var list = new List<Concern>(3);
            if (p == null)
                return list;
            string kind; int stage; bool good;
            if (gameDay >= 0 && InnerLife.WorryNow(p, gameDay, out kind, out stage, out good))
            {
                string what;
                if (!WorryWords.TryGetValue(kind, out what))
                    what = kind;
                list.Add(new Concern
                {
                    Kind = "worry:" + kind, Stage = stage, Good = good, Urgency = stage == 0 ? 0.35f : stage == 1 ? 0.55f : stage == 2 ? 0.85f : good ? 0.15f : 0.6f,
                    Words = stage == 3 ? (good ? "relieved about " : "grieving over ") + what : what + (StageWords[stage].Length > 0 ? " (" + StageWords[stage] + ")" : "")
                });
            }
            if (linked != null)
                foreach (WorryLink l in linked)
                {
                    if (l == null || l.Ended)
                        continue;
                    string who = Person.FirstOf(l.Name);
                    list.Add(new Concern
                    {
                        Kind = "link:" + l.Kind, Stage = -1, Urgency = 0.4f,
                        Words = l.Kind == "debt" ? who + "'s debt to them" : l.Kind == "feud" ? who + "'s quarrel with them" : l.Kind == "courting" ? who + "'s courting" : who + "'s trouble"
                    });
                }
            if (p.Goals != null)
                foreach (GoalItem gi in p.Goals)
                {
                    if (gi == null || gi.Type == null)
                        continue;
                    list.Add(new Concern { Kind = "goal:" + gi.Type, Stage = -1, Urgency = gi.Type == "Avenge" ? 0.75f : 0.45f, Words = GoalWords(gi) });
                }
            list.Sort((a, b) => b.Urgency.CompareTo(a.Urgency));
            if (list.Count > 3)
                list.RemoveRange(3, list.Count - 3);   // the three most urgent: nobody carries more at once
            return list;
        }

        private static string GoalWords(GoalItem g)
        {
            string target = string.IsNullOrEmpty(g.Target) ? null : g.Target == "player" || g.Target == "you" ? "you" : Person.FirstOf(g.Target);
            switch (g.Type)
            {
                case "Avenge": return "getting even" + (target != null ? " with " + target : "");
                case "Prove": return "proving themselves" + (target != null ? " to " + target : "");
                case "Rise": return "rising in the world";
                case "Rich": return "getting rich";
                case "Settle": return "settling down";
                case "Protect": return "keeping " + (target ?? "their own") + " safe";
                default: return g.Type.ToLowerInvariant();
            }
        }

        /// "worried about the bank's note (the crisis)", for the log and F7.
        public static string Words(List<Concern> list)
        {
            if (list == null || list.Count < 1)
                return "no live worry";
            Concern c = list[0];
            return (c.Kind.StartsWith("worry:") && c.Stage < 3 ? "worried about " : c.Kind.StartsWith("link:") ? "minding " : c.Kind.StartsWith("goal:") ? "set on " : "") + c.Words +
                   (list.Count > 1 ? ", and " + (list.Count - 1) + " more" : "");
        }
    }
}
