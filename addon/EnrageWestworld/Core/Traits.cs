// Traits v1: the NPC mind's slowest layer (the mind design, step 2; daytime run 2026-10-27, shadow mode).
// Five broad traits with two facets each, honesty-humility, and three dispositions of the period: piety, deference to
// the law and an appetite for risk. Each is a z-score (0 the town's middle, 1 a standard deviation), worked out from the
// person's id and what is already decided about them, and never stored: the same person always gets the same traits.
// What's decided wins: enRAGE's seven tempers stand for trait profiles (the mind doc's table), so a person's traits are
// drawn from the part of the trait space their temper names (`Score`, `LabelOf`), and their temper reads back from them;
// enRAGE's records import their ten facets; residents keep their faith, sociability and disposition. Small pulls by
// trade (under 0.3 SD) and age (up to 0.5 SD from 20 to 60, the steadying that comes with years). Plain C#, no game.
using System;
using System.Collections.Generic;
using System.Text;

namespace EnrageWestworld.Core
{
    /// What is already known about a person, for working out their traits.
    public sealed class TraitSource
    {
        public string Id;
        public string Kind;                  // resident, folk (enRAGE's townsfolk), record (enRAGE's records), other
        public string Temper;                // one of the seven, or null for a plain middling person
        public bool TemperDecided = true;    // false: nobody has given them a temper yet (an enRAGE townsperson with none)
        public bool Woman;
        public string Trade;
        public int Age = -1;                 // -1: a steady age from the id, as the worries use
        public string Devotion, Sociability, Disposition, Smarts;
        public Dictionary<string, int> Facets, Values;   // enRAGE's, its records only

        /// From the plain Person the rest of the add-on uses; `kind` as above.
        public static TraitSource From(Person p, string kind)
        {
            if (p == null)
                return null;
            return new TraitSource
            {
                Id = p.Id, Kind = kind, Temper = Traits.Known(p.Personality) ? p.Personality : null, TemperDecided = kind != "folk" || Traits.Known(p.Personality),
                Woman = p.Woman, Trade = p.Trade, Age = p.BornYear > 0 ? Voice.NowYear - p.BornYear : -1,
                Devotion = p.Devotion, Sociability = p.Sociability, Disposition = p.Disposition, Smarts = p.Smarts,
                Facets = p.Facets != null && p.Facets.Count > 0 ? p.Facets : null, Values = p.Values != null && p.Values.Count > 0 ? p.Values : null
            };
        }
    }

    /// One person's traits.
    public sealed class Traits
    {
        public const int Curiosity = 0, Imagination = 1, Order = 2, SelfControl = 3, Sociability = 4, Assertiveness = 5,
                         Compassion = 6, Trust = 7, Anxiety = 8, Anger = 9, Honesty = 10, Piety = 11, Deference = 12, Risk = 13, Count = 14;
        public static readonly string[] Names = { "curiosity", "imagination", "order", "self-control", "sociability", "assertiveness", "compassion", "trust", "anxiety", "anger", "honesty-humility", "piety", "deference to the law", "appetite for risk" };
        public static readonly string[] Domains = { "openness", "conscientiousness", "extraversion", "agreeableness", "neuroticism" };
        public static readonly string[] Tempers = { "Hothead", "Coward", "Proud", "Cold", "Greedy", "Loyal", "Drifter" };
        // how high and how low each reads, in words of the time (not enRAGE's words for its facets)
        private static readonly string[] High = { "curious", "fanciful", "orderly", "steady", "sociable", "outspoken", "soft-hearted", "trusting", "anxious", "short-tempered", "honest", "pious", "law-abiding", "reckless" };
        private static readonly string[] Low = { "incurious", "matter-of-fact", "slapdash", "rash", "solitary", "meek", "hard-hearted", "suspicious", "calm", "even-tempered", "sly", "godless", "no friend of the law", "cautious" };

        /// How strongly a trait's two facets share their broad trait (their correlation, about 0.45).
        public const float FacetR = 0.45f;
        /// The temper's mark: a temper reads back when its score is the highest and at least this high.
        public const float LabelAt = 1.08f;
        /// How far from the middle a trait must be to be named in words (1.4: about one person in six on each).
        public const float WordsAt = 1.4f;
        // small evening-out so that, with nothing decided, each temper comes up about as often as the game hands it out
        // (Loyal's three traits lean together and would come up a third more often)
        private static readonly float[] Even = { -0.03f, -0.02f, 0f, 0f, -0.06f, 0.13f, 0f };
        private const int MaxTries = 600;

        public readonly float[] Z = new float[Count];
        public string Label;                  // the temper they read as (null: plain); always their own when one was decided
        public string Kind;
        public int Tries;                     // how many draws it took to land in their temper's part of the space
        public bool Nudged;                   // no draw landed there (an enRAGE record whose facets say otherwise): moved in
        public Dictionary<string, int> Values;   // enRAGE's six values, its records only

        public float this[int i] { get { return Z[i]; } }

        /// A broad trait (0 openness .. 4 neuroticism), scaled back to a standard deviation of about 1.
        public float Domain(int d)
        {
            return (Z[2 * d] + Z[2 * d + 1]) / (float)Math.Sqrt(2f + 2f * FacetR);
        }

        public static bool Known(string temper)
        {
            return Array.IndexOf(Tempers, temper) >= 0;
        }

        // ---- the tempers as profiles (the mind doc's table) -------------------------------------------

        // each temper: (trait, weight) pairs; the score is their weighted sum over its length, about a standard deviation
        private static readonly int[][] SigT =
        {
            new[] { Anger, Compassion, SelfControl },          // Hothead: high anger, low compassion, low self-control
            new[] { Anxiety, Assertiveness, Risk },            // Coward: high anxiety, low assertiveness
            new[] { Assertiveness, Honesty },                  // Proud: high assertiveness, low humility
            new[] { Compassion, Sociability },                 // Cold: low compassion, low sociability
            new[] { Honesty, Trust },                          // Greedy: low honesty-humility
            new[] { Trust, Compassion, Order },                // Loyal: high trust, compassion, order
            new[] { Curiosity, Order, Deference }              // Drifter: high curiosity, low order, low deference
        };
        private static readonly float[][] SigW =
        {
            new[] { 1f, -0.5f, -0.6f },
            new[] { 1f, -0.8f, -0.3f },
            new[] { 1f, -0.7f },
            new[] { -1f, -0.9f },
            new[] { -1f, -0.3f },
            new[] { 1f, 0.8f, 0.5f },
            new[] { 0.8f, -0.8f, -0.8f }
        };
        // how each trait is built from 19 independent draws: the five broad traits (0-4) and each trait's own part (5-18).
        // A facet is a share of its broad trait and its own part; the four on their own lean a little on the broad ones
        // (honesty-humility on agreeableness; piety on order and kindness; deference on order and against openness; risk
        // against anxiety and with openness), as they do in studies of personality.
        private const int Factors = 19;
        private static readonly float[,] Load = MakeLoad();
        // each temper's score is scaled by its own spread in the town (worked out from the loadings), so each comes up
        // about as often as the next when nothing is decided
        private static readonly float[] SigLen = MakeLens();

        private static float[,] MakeLoad()
        {
            var l = new float[Count, Factors];
            float sa = (float)Math.Sqrt(FacetR), sb = (float)Math.Sqrt(1f - FacetR);
            for (int i = 0; i < 10; i++)
                l[i, i / 2] = sa;
            // anger leans against agreeableness and conscientiousness, as angry hostility does in the NEO's data
            l[Anger, 3] = -0.3f;
            l[Anger, 1] = -0.2f;
            l[Honesty, 3] = 0.3f;
            l[Piety, 1] = 0.2f;
            l[Piety, 3] = 0.15f;
            l[Deference, 1] = 0.25f;
            l[Deference, 0] = -0.2f;
            // risk: against anxiety's own part, with anger's, against conscientiousness, with extraversion and openness
            l[Risk, 5 + Anxiety] = -0.3f;
            l[Risk, 5 + Anger] = 0.15f;
            l[Risk, 1] = -0.2f;
            l[Risk, 2] = 0.2f;
            l[Risk, 0] = 0.15f;
            for (int i = 0; i < Count; i++)
            {
                float shared = 0f;
                for (int k = 0; k < Factors; k++)
                    if (k != 5 + i)
                        shared += l[i, k] * l[i, k];
                l[i, 5 + i] = (float)Math.Sqrt(1f - shared);   // each trait's own part fills it out to a standard deviation of 1
            }
            return l;
        }

        private static float[] MakeLens()
        {
            var len = new float[SigW.Length];
            for (int t = 0; t < SigW.Length; t++)
            {
                double v = 0;
                for (int k = 0; k < Factors; k++)
                {
                    double s = 0;
                    for (int i = 0; i < SigT[t].Length; i++)
                        s += SigW[t][i] * Load[SigT[t][i], k];
                    v += s * s;
                }
                len[t] = (float)Math.Sqrt(v);
            }
            return len;
        }

        /// How far these traits lean toward a temper (index into Tempers), in standard deviations.
        public static float Score(int temper, float[] z)
        {
            float s = 0f;
            int[] t = SigT[temper];
            float[] w = SigW[temper];
            for (int i = 0; i < t.Length; i++)
                s += w[i] * z[t[i]];
            return s / SigLen[temper];
        }

        /// The temper these traits read as: the one they lean toward most, if it's at least LabelAt; else null (plain).
        public static string LabelOf(float[] z)
        {
            int best = -1;
            float top = float.MinValue;
            for (int t = 0; t < Tempers.Length; t++)
            {
                float s = Score(t, z) - Even[t];
                if (s > top)
                {
                    top = s;
                    best = t;
                }
            }
            return top >= LabelAt ? Tempers[best] : null;
        }

        // ---- working them out ---------------------------------------------------------------------------

        /// The traits for this person. Pure: the same source always gives the same traits. In three steps, so a person
        /// stays the same person when something small changes (enRAGE moving a record's facet after a memory, the year
        /// turning to 1907): (1) the draw, chosen by their id and temper alone, each try on its own stream; (2) what's
        /// decided added to it (faith, sociability, disposition, smarts, trade, age, sex; a record's facets instead of
        /// the first four and the trade, which enRAGE's facets already carry), and held to its side (a churchgoer never
        /// reads godless); (3) if that carried them off their temper, moved back along its profile until it reads again.
        public static Traits Of(TraitSource s)
        {
            var r = new Traits { Kind = s.Kind, Values = s.Values };
            ulong seed = Hash64("traits|" + s.Id);
            float[] z = r.Z, f = new float[Factors];
            string want = s.Temper;
            int tryNo = 0;
            for (; ; tryNo++)
            {
                ulong state = seed + (ulong)tryNo * 0xD1B54A32D192ED03UL;
                state = Next(ref state);   // each try from its own point: no two tries share their numbers
                Draw(state, f, z);
                if (!s.TemperDecided || LabelOf(z) == want || tryNo >= MaxTries - 1)
                    break;
            }
            r.Tries = tryNo + 1;
            int age = s.Age >= 0 ? s.Age : 20 + Voice.Hash(s.Id + "|age") % 45;
            float[] pull = Pulls(s, age, s.Facets == null);
            for (int i = 0; i < Count; i++)
                z[i] += pull[i];
            if (s.Facets != null)
                Import(s.Facets, s.Values, z);
            Hold(s, z);
            if (s.TemperDecided && LabelOf(z) != want)
            {
                r.Nudged = true;
                Nudge(z, want, s);
            }
            r.Label = LabelOf(z);
            if (!s.TemperDecided)
                return r;
            r.Mismatch = r.Label != want;   // never seen in the tests; logged if it ever is
            return r;
        }

        /// The temper didn't read back even after moving (the tests count it; never expected).
        public bool Mismatch;

        /// What's decided keeps its side: a churchgoer's piety never reads godless, an outgoing man never solitary, a
        /// friendly one never hard-hearted (enRAGE's records are left to their facets).
        private static void Hold(TraitSource s, float[] z)
        {
            if (s.Facets != null)
                return;
            switch (s.Devotion)
            {
                case "Devout": z[Piety] = Math.Max(z[Piety], 0f); break;
                case "Churchgoer": z[Piety] = Math.Max(z[Piety], -0.8f); break;
                case "Lapsed": z[Piety] = Math.Min(Math.Max(z[Piety], -1.2f), 0.8f); break;
                case "Godless": z[Piety] = Math.Min(z[Piety], -0.3f); break;
            }
            if (s.Sociability == "Outgoing") z[Sociability] = Math.Max(z[Sociability], -0.5f);
            else if (s.Sociability == "Introvert") z[Sociability] = Math.Min(z[Sociability], 0.5f);
            if (s.Disposition == "Friendly") z[Compassion] = Math.Max(z[Compassion], -0.6f);
            else if (s.Disposition == "Surly") z[Compassion] = Math.Min(z[Compassion], 0.6f);
        }

        /// Pulls that are already decided, in standard deviations: faith, sociability, disposition, smarts and trade
        /// (`all`: not for enRAGE's records, whose facets carry them), age and sex.
        private static float[] Pulls(TraitSource s, int age, bool all)
        {
            var p = new float[Count];
            if (all)
            switch (s.Devotion)
            {
                case "Devout": p[Piety] += 1.1f; break;
                case "Churchgoer": p[Piety] += 0.35f; break;
                case "Lapsed": p[Piety] -= 0.4f; break;
                case "Godless": p[Piety] -= 1.2f; break;
            }
            if (all)
            {
                if (s.Sociability == "Outgoing") p[Sociability] += 0.55f;
                else if (s.Sociability == "Introvert") p[Sociability] -= 0.55f;
                if (s.Disposition == "Friendly") { p[Compassion] += 0.35f; p[Anger] -= 0.2f; }
                else if (s.Disposition == "Surly") { p[Compassion] -= 0.35f; p[Anger] += 0.3f; }
                if (s.Smarts == "Sharp") p[Curiosity] += 0.3f;
                else if (s.Smarts == "Dim") p[Curiosity] -= 0.3f;
                float[] trade = TradePull(TradeOf(s.Trade));
                for (int i = 0; i < Count; i++)
                    p[i] += trade[i];
            }
            // the years steady most people: more orderly, calmer, kinder, warier of risk (from 20 to 60, linear)
            float a = Math.Max(0f, Math.Min(1f, (age - 20) / 40f)) - 0.5f;
            for (int i = 0; i < Count; i++)
                p[i] += a * AgeChange[i];
            if (s.Woman)
            {
                p[Anxiety] += 0.2f;
                p[Compassion] += 0.2f;
                p[Risk] -= 0.2f;
            }
            return p;
        }

        /// The change from 20 to 60, in standard deviations (the maturity seen in long studies of personality).
        public static readonly float[] AgeChange = { -0.15f, -0.2f, 0.4f, 0.45f, -0.15f, 0.15f, 0.2f, 0.15f, -0.35f, -0.4f, 0.3f, 0.2f, 0.25f, -0.45f };

        /// A trade's group for its pulls: InnerLife's groups, plus enRAGE's trades.
        public static string TradeOf(string trade)
        {
            string t = (trade ?? string.Empty).ToLowerInvariant();
            if (t.Contains("outlaw") || t.Contains("bandit") || t.Contains("gunslinger")) return "rough";
            if (t.Contains("sheriff") || t.Contains("deputy") || t.Contains("marshal")) return "law";
            if (t.Contains("rancher") || t.Contains("homestead")) return "farm";
            if (t.Contains("schoolteacher") || t.Contains("doctor") || t.Contains("banker") || t.Contains("nun")) return "gentry";
            if (t.Contains("hunter")) return "trapper";
            return InnerLife.TradeGroup(trade);
        }

        /// What a trade asks of a person, and so who tends to end up in it: small, never 0.3 SD.
        public static float[] TradePull(string group)
        {
            var p = new float[Count];
            switch (group)
            {
                case "law": p[Deference] += 0.25f; p[Order] += 0.2f; p[Risk] += 0.1f; break;
                case "soldier": p[Deference] += 0.25f; p[Order] += 0.2f; p[Risk] += 0.15f; break;
                case "card": p[Risk] += 0.25f; p[Order] -= 0.15f; p[Honesty] -= 0.15f; break;
                case "rough": p[Risk] += 0.2f; p[Deference] -= 0.2f; p[Anger] += 0.15f; break;
                case "gentry": p[Order] += 0.15f; p[Assertiveness] += 0.15f; p[Deference] += 0.15f; break;
                case "farm": p[Order] += 0.1f; p[Piety] += 0.15f; p[Curiosity] -= 0.1f; break;
                case "trapper": p[Sociability] -= 0.25f; p[Risk] += 0.15f; break;
                case "girl": p[Sociability] += 0.2f; break;
                case "working": p[SelfControl] += 0.05f; p[Deference] -= 0.05f; break;
                case "town": p[Deference] += 0.1f; p[Sociability] += 0.1f; break;
            }
            return p;
        }

        // a draw: the 19 independent parts, then each trait from its loadings
        private static void Draw(ulong state, float[] f, float[] z)
        {
            for (int k = 0; k < Factors; k++)
                f[k] = Normal(ref state);
            for (int i = 0; i < Count; i++)
            {
                float s = 0f;
                for (int k = 0; k < Factors; k++)
                    s += Load[i, k] * f[k];
                z[i] = s;
            }
        }

        // enRAGE's records: its ten facets (0-100 around 50; a temper moves one 10-35, faith and disposition more, then
        // +-10 of noise) carry over where they say the same thing, scaled by their spread among records (about 12 points
        // a standard deviation), mixed with the person's own draw where a facet says only part of a trait; its Law value
        // for deference. Its other values (God, Money, Honour) are worked out from the facets, so they aren't added again.
        private static float Fa(Dictionary<string, int> f, string name)
        {
            int n;
            return f.TryGetValue(name, out n) ? (n - 50) / 12f : 0f;
        }

        private static void Import(Dictionary<string, int> f, Dictionary<string, int> v, float[] z)
        {
            z[Anger] = Mix(z[Anger], 0.6f, Fa(f, "Anger"), 0.8f);
            z[Anxiety] = Mix(z[Anxiety], 0.75f, -Fa(f, "Bravery"), 0.6f);
            z[Assertiveness] = Mix(z[Assertiveness], 0.7f, 0.5f * Fa(f, "Bravery") + 0.5f * Fa(f, "Pride") + 0.3f * Fa(f, "Ambition"), 0.6f);
            z[Honesty] = Mix(z[Honesty], 0.6f, 0.6f * Fa(f, "Honesty") - 0.5f * Fa(f, "Greed") - 0.3f * Fa(f, "Pride"), 0.7f);
            z[Trust] = Mix(z[Trust], 0.75f, Fa(f, "Loyalty"), 0.6f);
            z[Compassion] = Mix(z[Compassion], 0.65f, -0.8f * Fa(f, "Cruelty") + 0.3f * Fa(f, "Loyalty"), 0.7f);
            z[Piety] = Mix(z[Piety], 0.5f, Fa(f, "Piety") * 0.5f, 0.85f);   // faith moves Piety 35 points: about 1.5 SD
            z[Sociability] = Mix(z[Sociability], 0.85f, Fa(f, "Humour"), 0.5f);
            z[Order] = Mix(z[Order], 0.9f, Fa(f, "Ambition"), 0.4f);
            z[Risk] = Mix(z[Risk], 0.85f, Fa(f, "Bravery"), 0.5f);
            int law;
            if (v != null && v.TryGetValue("Law", out law))
                z[Deference] = Mix(z[Deference], 0.8f, law / 2.5f, 0.6f);
        }

        private static float Mix(float own, float wOwn, float theirs, float wTheirs)
        {
            return (own * wOwn + theirs * wTheirs) / (float)Math.Sqrt(wOwn * wOwn + wTheirs * wTheirs);
        }

        // moves the traits along the temper's own profile (or, for the plain, away from whatever they lean to) until
        // they read as it, keeping what's decided on its side at each step
        private static void Nudge(float[] z, string want, TraitSource s)
        {
            for (int step = 0; step < 200 && LabelOf(z) != want; step++)
            {
                Hold(s, z);
                if (LabelOf(z) == want)
                    break;
                if (want != null)
                {
                    int t = Array.IndexOf(Tempers, want);
                    for (int i = 0; i < SigT[t].Length; i++)
                        z[SigT[t][i]] += 0.15f * SigW[t][i] / SigLen[t];
                }
                else
                {
                    string got = LabelOf(z);
                    int t = Array.IndexOf(Tempers, got);
                    for (int i = 0; i < SigT[t].Length; i++)
                        z[SigT[t][i]] -= 0.15f * SigW[t][i] / SigLen[t];
                }
            }
        }

        // ---- words --------------------------------------------------------------------------------------

        /// The two or three things most marked about them, in plain words ("short-tempered, rash and suspicious"); a
        /// person with nothing past WordsAt standard deviations is "middling in most things".
        public string Words(int most = 3)
        {
            var idx = new List<int>();
            for (int i = 0; i < Count; i++)
                if (Math.Abs(Z[i]) >= WordsAt)
                    idx.Add(i);
            idx.Sort((a, b) => Math.Abs(Z[b]).CompareTo(Math.Abs(Z[a])));
            if (idx.Count == 0)
                return "middling in most things";
            var sb = new StringBuilder();
            int n = Math.Min(most, idx.Count);
            for (int k = 0; k < n; k++)
            {
                if (k > 0)
                    sb.Append(k == n - 1 ? " and " : ", ");
                int i = idx[k];
                sb.Append(Z[i] > 0 ? High[i] : i == Honesty ? (Z[Assertiveness] > 0.5f ? "boastful" : "grasping") : Low[i]);   // low honesty-humility: the vain or the greedy
            }
            return sb.ToString();
        }

        /// One line for the log and F7: the words, the temper read back, and the broad five.
        public string Describe()
        {
            return Words() + (Label != null ? " (reads as " + Label + ")" : " (plain)") + "; O " + Domain(0).ToString("0.0") + " C " + Domain(1).ToString("0.0") +
                   " E " + Domain(2).ToString("0.0") + " A " + Domain(3).ToString("0.0") + " N " + Domain(4).ToString("0.0") + ", honesty " + Z[Honesty].ToString("0.0") +
                   ", piety " + Z[Piety].ToString("0.0") + ", law " + Z[Deference].ToString("0.0") + ", risk " + Z[Risk].ToString("0.0");
        }

        // ---- numbers -----------------------------------------------------------------------------------

        public static ulong Hash64(string s)
        {
            unchecked
            {
                ulong h = 14695981039346656037UL;
                foreach (char ch in s ?? string.Empty)
                {
                    h ^= ch;
                    h *= 1099511628211UL;
                }
                return h;
            }
        }

        private static ulong Next(ref ulong state)
        {
            unchecked
            {
                ulong x = state += 0x9E3779B97F4A7C15UL;   // splitmix64
                x = (x ^ (x >> 30)) * 0xBF58476D1CE4E5B9UL;
                x = (x ^ (x >> 27)) * 0x94D049BB133111EBUL;
                return x ^ (x >> 31);
            }
        }

        // a standard normal from the sum of six uniforms (bell-shaped, no tails past 4.2)
        private static float Normal(ref ulong state)
        {
            double s = 0;
            for (int i = 0; i < 6; i++)
                s += (Next(ref state) >> 11) * (1.0 / 9007199254740992.0);
            return (float)((s - 3.0) * Math.Sqrt(2.0));
        }
    }
}
