// Choosing what to do (the mind design's step 6, daytime run 2026-10-27, item 11; Core only, nothing in the game calls
// it yet). When nothing has just happened to them, a person scores every action that's proper here and now and takes the
// best: what it does for their most pressing needs, how it suits their nature and mood, less its effort and risk, less
// a turn for what they did a moment ago, plus a little chance (more for the impulsive, less for the orderly). A feeling
// still strongly felt goes ahead of any of it, by its urge. Pairs from Reasons stay the Director's. Plain C#, no game.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    /// What the scorer is told about the moment (the needs and traits come separately).
    public sealed class ChoiceContext
    {
        public int Hour = 12, Weekday = 3;    // 0 Sunday
        public string Place;                  // saloon, church, store, hotel, home... (null: the street or the wilds)
        public bool Indoors, InTown = true;
        public string Weather;                // rain, storm, snow... or null
        public int Others;                    // people in sight of them
        public bool PlayerFeared;             // they've seen the player do violence lately and he's near
        public string Urge;                   // a feeling still strongly felt: its urge (Appraisal), else null
        public float FeelingLeft;             // how much of that feeling is left (Mood.Lasting)
        public string Last;                   // what they did a moment ago (an action's Id), or null
        public RecentActs Recent;             // what they did lately and when: a brief thing done lately serves its need less, since it was just answered and nothing but a meal moves the needs (night 2026-10-28: a man showed off twelve hours running when nothing told him he'd been seen)
        public long Now;                      // the game minute (for Recent's ages)
        public string TradeGroup;             // InnerLife.TradeGroup (the saloon habit is the working man's)
        public string Mood;                   // Mood.Words' first word: sore, glad, or null for even
        public bool Works = true;             // a working trade: the day's work is a duty, not only a want (gentry and ladies: false)
        public string WorkPlace;              // where their work is, if it's somewhere work is otherwise improper (a barkeep's saloon)
        public bool Woman, SaloonGirl;        // a respectable woman of 1899 didn't drink or play cards in a saloon; the saloon's own women did
        // night 2026-10-28 (item 5, the shadow): who is in sight
        public int TiesNear;                  // people in sight they have a warm tie with (a word or a nod to one of them)
        public bool FoeNear;                  // someone in sight they have a hostile tie with (stepping clear)
        public bool SaloonNear = true, ChurchNear = true, StoreNear = true;   // what the town has (false out in the wilds)
    }

    public sealed class Doing
    {
        public string Id, Words;              // "eat", "goes for something to eat"
        public float[] Serves;                // per need (NeedState order): how much doing it answers that need
        public float Effort, Risk;
        public Func<ChoiceContext, bool> Proper;          // never chosen where this says no
        public Func<Traits, float> Suits;                 // how well it fits who they are (about -0.3..0.3)
    }

    /// The last three brief things a person did and when (game minutes), newest first; the same thing again while
    /// it goes on refreshes its time and counts the run. In memory only, allocated once per person.
    public sealed class RecentActs
    {
        public readonly string[] Id = new string[3];
        public readonly long[] At = new long[3];
        public readonly int[] Run = new int[3];

        public void Note(string id, long now)
        {
            if (id == null)
                return;
            if (Id[0] == id)
            {
                At[0] = now;   // still at it
                Run[0] = Math.Min(Run[0] + 1, 99);
                return;
            }
            for (int k = 2; k > 0; k--)
            {
                Id[k] = Id[k - 1];
                At[k] = At[k - 1];
                Run[k] = Run[k - 1];
            }
            Id[0] = id;
            At[0] = now;
            Run[0] = 1;
        }

        public void Clear()
        {
            for (int k = 0; k < 3; k++)
            {
                Id[k] = null;
                At[k] = 0;
                Run[k] = 0;
            }
        }
    }

    public static class Choosing
    {
        public const float Interrupts = 0.65f;   // a feeling this strong goes ahead of any plan (Appraisal's strong)

        private static float T(Traits t, int i)
        {
            return t != null ? t[i] : 0f;
        }

        private static float[] N(params float[] v)
        {
            var a = new float[NeedState.Count];
            for (int i = 0; i < v.Length && i < a.Length; i++)
                a[i] = v[i];
            return a;
        }

        private static bool Meal(int h)
        {
            return h >= 6 && h < 9 || h >= 11 && h < 14 || h >= 17 && h < 21;
        }

        private static bool Sunday(ChoiceContext c)
        {
            return c.Weekday == 0 && c.Hour >= 7 && c.Hour < 13;
        }

        //                                              hunger rest warmth money safety company standing meaning vice
        public static readonly List<Doing> All = new List<Doing>
        {
            new Doing { Id = "stand", Words = "stands about", Serves = N(0, 0.15f), Effort = 0f, Risk = 0f, Proper = c => true, Suits = t => -0.1f * T(t, Traits.Curiosity) },
            new Doing { Id = "stroll", Words = "takes a stroll", Serves = N(0, 0, 0, 0, 0, 0.15f, 0, 0.2f), Effort = 0.1f, Risk = 0.02f, Proper = c => !c.Indoors && c.Weather != "storm" && (c.Hour >= 7 && c.Hour < 21), Suits = t => 0.12f * T(t, Traits.Curiosity) + 0.06f * T(t, Traits.Risk) },
            new Doing { Id = "eat", Words = "goes for something to eat", Serves = N(0.9f), Effort = 0.1f, Risk = 0f, Proper = c => c.Place != "church" && (Meal(c.Hour) || c.Place == "saloon" || c.Place == "hotel" || c.Place == "home"), Suits = t => 0f },
            new Doing { Id = "rest", Words = "sits down to rest", Serves = N(0, 0.6f, 0.1f), Effort = 0f, Risk = 0f, Proper = c => c.Hour < 19 || c.Place == "home" || !c.InTown, Suits = t => -0.08f * T(t, Traits.Order) },   // of an evening in a town, rest is at home
            new Doing { Id = "home", Words = "heads home", Serves = N(0.5f, 0.9f, 0.6f, 0, 0.5f, 0, 0, 0.1f), Effort = 0.15f, Risk = 0f, Proper = c => c.Place != "home" && c.InTown && (c.Hour >= 19 || c.Hour < 6), Suits = t => 0.1f * T(t, Traits.Order) - 0.06f * T(t, Traits.Sociability) },   // supper and bed
            new Doing { Id = "shelter", Words = "looks for a roof overhead", Serves = N(0, 0.1f, 1f), Effort = 0.1f, Risk = 0f, Proper = c => !c.Indoors && (c.Weather == "rain" || c.Weather == "storm" || c.Weather == "snow" || c.Weather == "dust"), Suits = t => 0.06f * T(t, Traits.Anxiety) },
            new Doing { Id = "work", Words = "gets on with the work", Serves = N(0, 0, 0, 0.9f, 0, 0, 0.2f, 0.2f), Effort = 0.35f, Risk = 0.02f, Proper = c => c.Weekday != 0 && c.Hour >= 7 && c.Hour < 18 && (c.Place != "church" && c.Place != "saloon" || c.WorkPlace != null && c.WorkPlace == c.Place), Suits = t => 0.15f * T(t, Traits.Order) + 0.05f * T(t, Traits.SelfControl) },
            new Doing { Id = "drink", Words = "has a drink", Serves = N(0, 0.1f, 0.1f, -0.1f, 0, 0.3f, 0, 0, 1f), Effort = 0.05f, Risk = 0.05f, Proper = c => (!c.Woman || c.SaloonGirl) && c.Place == "saloon" && !Sunday(c), Suits = t => -0.12f * T(t, Traits.SelfControl) - 0.1f * T(t, Traits.Piety) + 0.05f * T(t, Traits.Sociability) },
            new Doing { Id = "cards", Words = "joins the card game", Serves = N(0, 0, 0, 0.2f, 0, 0.3f, 0.2f, 0, 0.9f), Effort = 0.1f, Risk = 0.15f, Proper = c => (!c.Woman || c.SaloonGirl) && c.Place == "saloon" && !Sunday(c), Suits = t => 0.15f * T(t, Traits.Risk) - 0.1f * T(t, Traits.Piety) - 0.05f * T(t, Traits.SelfControl) },
            new Doing { Id = "pray", Words = "says a prayer", Serves = N(0, 0.05f, 0, 0, 0.1f, 0, 0, 1f), Effort = 0.05f, Risk = 0f, Proper = c => c.Place == "church" || Sunday(c) && c.Place != "saloon" || c.Place == "home", Suits = t => 0.25f * T(t, Traits.Piety) },
            new Doing { Id = "talk", Words = "strikes up a conversation", Serves = N(0, 0, 0, 0, 0.05f, 1f, 0.2f, 0.1f), Effort = 0.1f, Risk = 0.03f, Proper = c => c.Others > 0 && (c.Place != "church" || c.Weekday == 0 && c.Hour >= 11 && c.Hour < 14), Suits = t => 0.2f * T(t, Traits.Sociability) - 0.05f * T(t, Traits.Anxiety) },
            new Doing { Id = "watch", Words = "keeps a wary eye out", Serves = N(0, 0, 0, 0, 0.7f), Effort = 0.05f, Risk = 0f, Proper = c => true, Suits = t => 0.15f * T(t, Traits.Anxiety) - 0.05f * T(t, Traits.Trust) },
            new Doing { Id = "keepclear", Words = "keeps well clear of you", Serves = N(0, 0, 0, 0, 1f), Effort = 0.15f, Risk = 0f, Proper = c => c.PlayerFeared, Suits = t => 0.15f * T(t, Traits.Anxiety) - 0.1f * T(t, Traits.Assertiveness) },
            new Doing { Id = "showoff", Words = "makes sure to be seen", Serves = N(0, 0, 0, 0, 0, 0.2f, 1f), Effort = 0.15f, Risk = 0.05f, Proper = c => c.Others >= 2 && c.Place != "church", Suits = t => 0.15f * T(t, Traits.Assertiveness) - 0.12f * T(t, Traits.Honesty) },
            // night 2026-10-28 (item 5): the places of a town's day, and the people in sight
            new Doing { Id = "saloon", Words = "goes to the saloon", Serves = N(0.3f, 0.2f, 0.3f, 0, 0, 0.6f, 0.1f, 0, 0.6f), Effort = 0.15f, Risk = 0.04f,
                        Proper = c => c.Place != "saloon" && c.InTown && c.SaloonNear && (!c.Woman || c.SaloonGirl) && !Sunday(c) && (c.Hour >= 17 || c.Hour < 2 || c.Weekday == 6 && c.Hour >= 14),
                        Suits = t => 0.12f * T(t, Traits.Sociability) - 0.15f * T(t, Traits.Piety) - 0.05f * T(t, Traits.SelfControl) },   // 1899 is the Anti-Saloon League's decade: a church member in a saloon was the congregation's business
            new Doing { Id = "store", Words = "goes to the store", Serves = N(0.15f, 0, 0.25f, -0.1f, 0, 0.25f, 0.1f), Effort = 0.15f, Risk = 0f,   // provisions, not a meal; it costs; the cracker barrel was the town's second hub (the review)
                        Proper = c => c.Place != "store" && c.InTown && c.StoreNear && c.Weekday != 0 && c.Hour >= 7 && c.Hour < 20,
                        Suits = t => 0.06f * T(t, Traits.Order) },
            new Doing { Id = "church", Words = "goes to church", Serves = N(0, 0.1f, 0, 0, 0.1f, 0.3f, 0.15f, 1f), Effort = 0.15f, Risk = 0f,
                        Proper = c => c.Place != "church" && c.InTown && c.ChurchNear && (c.Weekday == 0 && c.Hour >= 9 && c.Hour < 12 || (c.Weekday == 0 || c.Weekday == 3) && c.Hour >= 18 && c.Hour < 21),   // the morning service, the Sunday evening service, the Wednesday prayer meeting (the review)
                        Suits = t => 0.3f * T(t, Traits.Piety) + 0.05f * T(t, Traits.Deference) },
            new Doing { Id = "greet", Words = "has a word with a friend", Serves = N(0, 0, 0, 0, 0.05f, 0.8f, 0.25f, 0.1f), Effort = 0.05f, Risk = 0f,
                        Proper = c => c.TiesNear > 0 && (c.Place != "church" || c.Weekday == 0 && c.Hour >= 11 && c.Hour < 14),
                        Suits = t => 0.15f * T(t, Traits.Sociability) + 0.08f * T(t, Traits.Compassion) },
            new Doing { Id = "steerclear", Words = "steps clear of someone they can't abide", Serves = N(0, 0, 0, 0, 0.6f, 0, -0.1f), Effort = 0.1f, Risk = 0f,   // being seen to step aside costs face in an honour culture (the review: Nisbett and Cohen)
                        Proper = c => c.FoeNear,
                        Suits = t => 0.1f * T(t, Traits.Anxiety) - 0.12f * T(t, Traits.Assertiveness) - 0.08f * T(t, Traits.Anger) },
        };

        // a feeling still strongly felt goes ahead of any plan, by its urge: an action of the list where one fits, else
        // "urge:<urge>" (what the feeling itself makes them do), with its words
        private static readonly Dictionary<string, string> UrgeAction = new Dictionary<string, string>
        {
            { "rest", "rest" }, { "watch", "watch" }, { "display", "showoff" }, { "share", "talk" }
        };
        private static readonly Dictionary<string, string> UrgeWords = new Dictionary<string, string>
        {
            { "confront", "squares up to whoever did it" }, { "intervene", "steps in" }, { "avoid", "gets well away from it" }, { "hide", "makes themselves scarce" },
            { "freeze", "goes still" }, { "withdraw", "keeps to themselves" }, { "help", "goes to help" }, { "amends", "tries to make it right" }, { "repay", "means to see it repaid" },
            { "disparage", "runs them down to anyone near" }, { "dismiss", "turns their back on it" }
        };

        // what's done for a while (doing it a moment ago is a reason to go on); the rest are brief (a reason not to again)
        private static readonly string[] Lasting = { "work", "rest", "home", "shelter", "pray", "saloon" };   // church is a service of an hour or two, not a day (the review)   // one drink or one game is brief; the saloon itself is what lasts (night 2026-10-28: a man drank 116 hours running in the town test)

        /// Carrying on with what they're doing: the choice that changes nothing (the shadow line counts it as none).
        public static bool CarriesOn(string id)
        {
            return id == null || id == "stand";
        }

        /// A thing done for a while (work, rest, home, the saloon...): the same choice an hour on is carrying on, not a new one.
        public static bool Continues(string id)
        {
            return id != null && Array.IndexOf(Lasting, id) >= 0;
        }

        /// The seed for one person's choice this hour: the same person, the same hour, the same roll (the Minds' way).
        public static string SeedFor(string id, long gameMinute)
        {
            return id + "|" + gameMinute / 60;
        }

        /// How much of what a brief thing serves is left to serve, after doing it lately: the need was just answered,
        /// though the needs model doesn't move for it (only a meal moves hunger). Satiation recovers with time, not
        /// with other things done in between: 0.4 of it within the hour, 0.7 within two and a half, 0.85 within four,
        /// all of it after; still at it (the same choice again and again) wears further, 0.7 a time. A lasting thing
        /// (work, the saloon) goes on; a wary eye, keeping clear and stepping clear don't satiate (vigilance holds while
        /// the danger does). Without Recent, what they did a moment ago (Last) counts as within the hour.
        public static float Answered(Doing d, ChoiceContext c)
        {
            if (Continues(d.Id) || CarriesOn(d.Id) || d.Id == "watch" || d.Id == "keepclear" || d.Id == "steerclear")
                return 1f;
            if (c.Recent == null)
                return d.Id == c.Last ? 0.4f : 1f;
            float left = 1f;
            for (int k = 0; k < 3; k++)
            {
                if (c.Recent.Id[k] != d.Id)
                    continue;
                long age = c.Now - c.Recent.At[k];
                float f = age <= 60 ? 0.4f : age <= 150 ? 0.7f : age <= 240 ? 0.85f : 1f;
                for (int r = 1; r < c.Recent.Run[k] && r < 4; r++)
                    f *= 0.7f;
                left *= f;
            }
            return left;
        }

        /// The working man's club: the trade groups whose evening was the saloon's (not the gentry's, the old men's or
        /// a woman's).
        public static bool SaloonMan(string tradeGroup)
        {
            return tradeGroup == "working" || tradeGroup == "rough" || tradeGroup == "card" || tradeGroup == "farm" || tradeGroup == "trapper" || tradeGroup == "town";
        }

        /// How much chance sways them: the impulsive more, the orderly less (0.1..1).
        public static float Impulse(Traits t)
        {
            return Math.Max(0.1f, Math.Min(1f, 0.5f - 0.18f * T(t, Traits.SelfControl) + 0.12f * T(t, Traits.Risk) - 0.08f * T(t, Traits.Order)));
        }

        /// One action's score now; float.NaN where it isn't proper. why: the need it serves most, for the log.
        public static float Score(Doing d, NeedState n, Traits t, ChoiceContext c, string seed, out string why)
        {
            why = null;
            if (!d.Proper(c))
                return float.NaN;
            float s = 0f, best = 0f, answered = Answered(d, c);
            for (int i = 0; i < NeedState.Count; i++)
            {
                float need = n != null ? n[i] : 0f;
                float v = d.Serves[i] * need * (float)Math.Sqrt(need) * 1.3f * answered;   // need to the power 1.5: a pressing need counts far more than a passing one, without one need taking the whole town (night 2026-10-28: squared had rest at 80% of an evening)
                if (i == NeedState.Vice && (n == null || n.ViceName == null || d.Id == "cards" && n.ViceName != "cards" || d.Id == "drink" && n.ViceName == "cards"))
                    v = 0f;   // a vice only pulls toward its own
                s += v;
                if (v > best)
                {
                    best = v;
                    why = NeedState.Names[i];
                }
            }
            s += d.Suits(t);
            if (d.Id == "work" && c.Works)
                s += 0.2f;   // the day's work is owed, whatever they'd rather (0.35 until the town test of 2026-10-28: work took 70% of a weekday morning)
            if (d.Id == "saloon" && c.Hour >= 19 && !c.Woman && SaloonMan(c.TradeGroup))
                s += 0.12f;   // the saloon of an evening, after supper: the working man's club of 1899, over and above his wants (not the gentry's, not the old men's)
            if (d.Id == "steerclear")
                s += 0.15f;   // a foe in sight: the needs don't see him, this does
            if (c.Mood == "sore" && (d.Id == "talk" || d.Id == "showoff"))
                s -= 0.1f;   // sore: less for company (a friend is still sought: greet stays)
            if (c.Mood == "glad" && (d.Id == "talk" || d.Id == "stroll" || d.Id == "greet"))
                s += 0.08f;
            s -= d.Effort * (n != null ? 0.3f + n[NeedState.Rest] : 0.3f);   // effort weighs more on the tired
            s -= d.Risk * (1f + 0.3f * T(t, Traits.Anxiety) - 0.3f * T(t, Traits.Risk));
            if (d.Id == c.Last)
                s += Array.IndexOf(Lasting, d.Id) >= 0 ? 0.3f : -0.25f;   // a day's work goes on; a word or a look isn't repeated straight off
            s += (Voice.Hash(seed + "|" + d.Id) % 1000 / 1000f - 0.5f) * 0.4f * Impulse(t);
            return s;
        }

        /// What they do next and why: the urge of a feeling still strongly felt first, else the best-scoring proper action.
        public static KeyValuePair<string, string> Choose(NeedState n, Traits t, ChoiceContext c, string seed)
        {
            string next;
            return Choose(n, t, c, seed, out next);
        }

        /// The same, with the runner-up (what they'd do next; null when nothing else fits or a feeling chose).
        public static KeyValuePair<string, string> Choose(NeedState n, Traits t, ChoiceContext c, string seed, out string next)
        {
            next = null;
            if (c.Urge != null && c.Urge != "none" && c.FeelingLeft >= Interrupts)
            {
                string urged;
                if (c.Urge == "avoid" && c.PlayerFeared)
                    urged = "keepclear";
                else if (!UrgeAction.TryGetValue(c.Urge, out urged))
                    urged = "urge:" + c.Urge;
                return new KeyValuePair<string, string>(urged, "a feeling (" + c.Urge + ")");
            }
            Doing top = null, second = null;
            float most = float.MinValue, secondMost = float.MinValue;
            string topWhy = null;
            foreach (Doing d in All)
            {
                string why;
                float s = Score(d, n, t, c, seed, out why);
                if (float.IsNaN(s))
                    continue;
                if (s > most)
                {
                    second = top;
                    secondMost = most;
                    most = s;
                    top = d;
                    topWhy = why;
                }
                else if (s > secondMost)
                {
                    second = d;
                    secondMost = s;
                }
            }
            next = second != null ? second.Id : null;
            return top == null ? new KeyValuePair<string, string>("stand", "nothing else fits") : new KeyValuePair<string, string>(top.Id, topWhy ?? "their nature");
        }

        public static string WordsOf(string id)
        {
            string w;
            if (id != null && id.StartsWith("urge:", StringComparison.Ordinal))
                return UrgeWords.TryGetValue(id.Substring(5), out w) ? w : id.Substring(5);
            foreach (Doing d in All)
                if (d.Id == id)
                    return d.Words;
            return id;
        }
    }
}
