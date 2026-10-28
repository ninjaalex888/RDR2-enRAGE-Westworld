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
        public string Mood;                   // Mood.Words' first word: sore, glad, or null for even
        public bool Works = true;             // a working trade: the day's work is a duty, not only a want (gentry and ladies: false)
    }

    public sealed class Doing
    {
        public string Id, Words;              // "eat", "goes for something to eat"
        public float[] Serves;                // per need (NeedState order): how much doing it answers that need
        public float Effort, Risk;
        public Func<ChoiceContext, bool> Proper;          // never chosen where this says no
        public Func<Traits, float> Suits;                 // how well it fits who they are (about -0.3..0.3)
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
            new Doing { Id = "eat", Words = "goes for something to eat", Serves = N(1f), Effort = 0.1f, Risk = 0f, Proper = c => c.Place != "church" && (Meal(c.Hour) || c.Place == "saloon" || c.Place == "hotel" || c.Place == "home"), Suits = t => 0f },
            new Doing { Id = "rest", Words = "sits down to rest", Serves = N(0, 0.8f, 0.1f), Effort = 0f, Risk = 0f, Proper = c => true, Suits = t => -0.08f * T(t, Traits.Order) },
            new Doing { Id = "home", Words = "heads home", Serves = N(0.2f, 0.9f, 0.6f, 0, 0.5f, 0, 0, 0.1f), Effort = 0.15f, Risk = 0f, Proper = c => c.Place != "home" && c.InTown && (c.Hour >= 19 || c.Hour < 6), Suits = t => 0.1f * T(t, Traits.Order) - 0.06f * T(t, Traits.Sociability) },
            new Doing { Id = "shelter", Words = "looks for a roof overhead", Serves = N(0, 0.1f, 1f), Effort = 0.1f, Risk = 0f, Proper = c => !c.Indoors && (c.Weather == "rain" || c.Weather == "storm" || c.Weather == "snow" || c.Weather == "dust"), Suits = t => 0.06f * T(t, Traits.Anxiety) },
            new Doing { Id = "work", Words = "gets on with the work", Serves = N(0, 0, 0, 0.9f, 0, 0, 0.2f, 0.2f), Effort = 0.35f, Risk = 0.02f, Proper = c => c.Weekday != 0 && c.Hour >= 7 && c.Hour < 18 && c.Place != "church" && c.Place != "saloon", Suits = t => 0.15f * T(t, Traits.Order) + 0.05f * T(t, Traits.SelfControl) },
            new Doing { Id = "drink", Words = "has a drink", Serves = N(0, 0.1f, 0.1f, -0.1f, 0, 0.3f, 0, 0, 1f), Effort = 0.05f, Risk = 0.05f, Proper = c => c.Place == "saloon" && !Sunday(c) || c.Place == null && c.Hour >= 17 && !Sunday(c) && c.InTown, Suits = t => -0.12f * T(t, Traits.SelfControl) - 0.1f * T(t, Traits.Piety) + 0.05f * T(t, Traits.Sociability) },
            new Doing { Id = "cards", Words = "joins the card game", Serves = N(0, 0, 0, 0.2f, 0, 0.3f, 0.2f, 0, 0.9f), Effort = 0.1f, Risk = 0.15f, Proper = c => c.Place == "saloon" && !Sunday(c), Suits = t => 0.15f * T(t, Traits.Risk) - 0.1f * T(t, Traits.Piety) - 0.05f * T(t, Traits.SelfControl) },
            new Doing { Id = "pray", Words = "says a prayer", Serves = N(0, 0.05f, 0, 0, 0.1f, 0, 0, 1f), Effort = 0.05f, Risk = 0f, Proper = c => c.Place == "church" || Sunday(c) && c.Place != "saloon" || c.Place == "home", Suits = t => 0.25f * T(t, Traits.Piety) },
            new Doing { Id = "talk", Words = "strikes up a conversation", Serves = N(0, 0, 0, 0, 0.05f, 1f, 0.2f, 0.1f), Effort = 0.1f, Risk = 0.03f, Proper = c => c.Others > 0 && c.Place != "church", Suits = t => 0.2f * T(t, Traits.Sociability) - 0.05f * T(t, Traits.Anxiety) },
            new Doing { Id = "watch", Words = "keeps a wary eye out", Serves = N(0, 0, 0, 0, 0.7f), Effort = 0.05f, Risk = 0f, Proper = c => true, Suits = t => 0.15f * T(t, Traits.Anxiety) - 0.05f * T(t, Traits.Trust) },
            new Doing { Id = "keepclear", Words = "keeps well clear of you", Serves = N(0, 0, 0, 0, 1f), Effort = 0.15f, Risk = 0f, Proper = c => c.PlayerFeared, Suits = t => 0.15f * T(t, Traits.Anxiety) - 0.1f * T(t, Traits.Assertiveness) },
            new Doing { Id = "showoff", Words = "makes sure to be seen", Serves = N(0, 0, 0, 0, 0, 0.2f, 1f), Effort = 0.15f, Risk = 0.05f, Proper = c => c.Others >= 2 && c.Place != "church", Suits = t => 0.15f * T(t, Traits.Assertiveness) - 0.12f * T(t, Traits.Honesty) },
        };

        // a feeling still strongly felt goes ahead of any plan, by its urge: what it makes them do
        private static readonly Dictionary<string, string> UrgeAction = new Dictionary<string, string>
        {
            { "confront", "confront" }, { "intervene", "step in" }, { "avoid", "keep clear" }, { "hide", "hide" }, { "freeze", "freeze" }, { "withdraw", "withdraw" },
            { "help", "help" }, { "amends", "make amends" }, { "repay", "repay" }, { "share", "tell someone" }, { "disparage", "talk about it" }, { "watch", "watch it" },
            { "display", "show it" }, { "rest", "sit down" }
        };

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
            float s = 0f, best = 0f;
            for (int i = 0; i < NeedState.Count; i++)
            {
                float need = n != null ? n[i] : 0f;
                float v = d.Serves[i] * need * need * 1.6f;   // squared: a pressing need counts far more than a passing one
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
                s += 0.35f;   // the day's work is owed, whatever they'd rather
            if (c.Mood == "sore" && (d.Id == "talk" || d.Id == "showoff"))
                s -= 0.1f;   // sore: less for company
            if (c.Mood == "glad" && (d.Id == "talk" || d.Id == "stroll"))
                s += 0.08f;
            s -= d.Effort * (n != null ? 0.3f + n[NeedState.Rest] : 0.3f);   // effort weighs more on the tired
            s -= d.Risk * (1f + 0.3f * T(t, Traits.Anxiety) - 0.3f * T(t, Traits.Risk));
            if (d.Id == c.Last)
                s -= 0.25f;   // not the same thing again straight off
            s += (Voice.Hash(seed + "|" + d.Id) % 1000 / 1000f - 0.5f) * 0.4f * Impulse(t);
            return s;
        }

        /// What they do next and why: the urge of a feeling still strongly felt first, else the best-scoring proper action.
        public static KeyValuePair<string, string> Choose(NeedState n, Traits t, ChoiceContext c, string seed)
        {
            string urged;
            if (c.Urge != null && c.FeelingLeft >= Interrupts && UrgeAction.TryGetValue(c.Urge, out urged))
                return new KeyValuePair<string, string>(urged, "a feeling (" + c.Urge + ")");
            Doing top = null;
            float most = float.MinValue;
            string topWhy = null;
            foreach (Doing d in All)
            {
                string why;
                float s = Score(d, n, t, c, seed, out why);
                if (float.IsNaN(s) || s <= most)
                    continue;
                most = s;
                top = d;
                topWhy = why;
            }
            return top == null ? new KeyValuePair<string, string>("stand", "nothing else fits") : new KeyValuePair<string, string>(top.Id, topWhy ?? "their nature");
        }

        public static string WordsOf(string id)
        {
            foreach (Doing d in All)
                if (d.Id == id)
                    return d.Words;
            return id;
        }
    }
}
