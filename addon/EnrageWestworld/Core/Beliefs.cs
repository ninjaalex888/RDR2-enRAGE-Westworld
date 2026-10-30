// Beliefs and the self v1 (the mind design's step 10; night run 2026-10-28, item 6; Core, shadow: nothing acts on it).
// A short list per person with a confidence 0..1: the world ("things are as they seem"), the town ("the law here is
// for folk like me", "a decent place"), others (the player: dangerous, kind, a liar) and the self ("an honest man", "I
// can look after myself", "somebody here"). The baseline is built from traits, trade and memories when asked, never
// stored; what appraisals have moved lives in memory (MindShadow, capped, cleared with the rest) as deltas on the
// baseline, with the bits of which beliefs were held at the last look, so a flip the baseline causes (a memory arriving
// or going, the Awakening moving) is seen too. An everyday appraisal moves a belief 0.05 at most, a memory 0.08 (0.2 in
// all per belief); a belief flips only past the threshold, and on one event only for a killing seen. Belief
// perseverance: a chip against a belief held or doubted with confidence weighs less (Lord, Ross and Lepper). Credit
// sticks more than blame for the bold (Miller and Ross). Dissonance (a memory against a strong belief) takes one of three
// roads by nature: the honest or pious take it onto the self, the anxious and the curious move what they believe of the
// town and others first, the rest rewrite the memory in their own favour (the drift's "he had it coming" slant; v1 only
// names the road). Read-only against Regard (the game side notes where they disagree); with [Mind] MindWakes the Awakening
// reads "as it seems" (Engine.Doubt.cs, night 2026-10-29).
// Known in v1: "the law here is for folk like me" moves only at the baseline (no appraisal carries a lawman as the doer
// yet); the buried man walking past ("revived") is appraised in play since 2026-10-29 (WestworldScript.Doubt.cs);
// race and ethnicity, the first-order fact of 1899 standing, have no field (trade stands in). Plain C#, no game.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public sealed class BeliefContext
    {
        public int WakeStage;                 // the Awakening's stage (read only)
        public float Standing;                // by trade (Beliefs.StandingOf): the law's and the town's regard, -0.2..0.2
        public List<MindMemory> Memories;     // read only
    }

    public static class Beliefs
    {
        // ids in order; Words says each in the person's own voice
        public static readonly string[] Ids = { "world:asitseems", "town:lawforme", "town:decent", "player:dangerous", "player:kind", "player:liar", "self:honest", "self:lookafter", "self:somebody" };
        public static readonly string[] Words = { "things are as they seem", "the law here is for folk like me", "a decent place, this", "that stranger is a dangerous man", "that stranger is a kind man", "that stranger is a liar", "I'm an honest man", "I can look after myself", "I'm somebody here" };
        public const int Count = 9;
        public const int World = 0, LawForMe = 1, Decent = 2, Dangerous = 3, Kind = 4, Liar = 5, Honest = 6, LookAfter = 7, Somebody = 8;
        public const float EverydayMax = 0.05f;   // the most one everyday appraisal moves a belief
        public const float MemoryMax = 0.08f;     // the most one memory moves the baseline, 0.2 in all per belief
        public const float Flip = 0.5f;           // held above, doubted below

        private static float T(Traits t, int i)
        {
            return t != null ? t[i] : 0f;
        }

        private static float Clamp(float x)
        {
            return x < 0f ? 0f : x > 1f ? 1f : x;
        }

        private static float Cap(float x, float cap)
        {
            return x < -cap ? -cap : x > cap ? cap : x;
        }

        /// The town's and the law's regard by trade group (1899; trade stands in for what the age really sorted people
        /// by): the gentry, the law and soldiers up; hard-looking men, gamblers, the saloon's women and trappers down;
        /// farmhands and labourers a little down; the rest level.
        public static float StandingOf(string tradeGroup)
        {
            switch (tradeGroup)
            {
                case "gentry": return 0.15f;
                case "law": case "soldier": return 0.2f;
                case "rough": case "card": return -0.2f;
                case "girl": case "trapper": return -0.15f;
                case "farm": case "working": return -0.05f;
                default: return 0f;
            }
        }

        /// The baseline, from who they are and what they've lived through (never stored; the same inputs, the same beliefs).
        public static float[] Baseline(Person p, Traits t, BeliefContext c)
        {
            var b = new float[Count];
            Fill(b, p, t, c);
            return b;
        }

        /// The same into an array the caller keeps (no allocation).
        public static void Fill(float[] b, Person p, Traits t, BeliefContext c)
        {
            float trust = T(t, Traits.Trust), anx = T(t, Traits.Anxiety), cur = T(t, Traits.Curiosity), order = T(t, Traits.Order), defer = T(t, Traits.Deference);
            float honesty = T(t, Traits.Honesty), piety = T(t, Traits.Piety), assert = T(t, Traits.Assertiveness), risk = T(t, Traits.Risk), soc = T(t, Traits.Sociability);
            int stage = c != null ? c.WakeStage : 0;
            float standing = c != null ? c.Standing : 0f;
            b[World] = Clamp(0.9f - 0.04f * anx - 0.04f * cur + 0.03f * order - 0.2f * stage);   // the awake have begun to doubt
            b[LawForMe] = Clamp(0.6f + 0.1f * defer + 0.05f * trust + standing - (p != null && p.Woman ? 0.05f : 0f));
            b[Decent] = Clamp(0.75f + 0.08f * trust - 0.05f * anx);
            b[Dangerous] = Clamp(0.3f + 0.08f * anx - 0.06f * trust);
            b[Kind] = Clamp(0.2f + 0.05f * trust);
            b[Liar] = Clamp(0.2f - 0.05f * trust + 0.03f * anx);
            b[Honest] = Clamp(0.65f + 0.12f * honesty + 0.05f * piety + 0.03f * defer);
            b[LookAfter] = Clamp(0.55f + 0.12f * assert + 0.06f * risk - 0.1f * anx);
            b[Somebody] = Clamp(0.55f + 0.1f * assert + 0.05f * soc + standing);
            // what they've lived through: the player's deeds seen, their own part in wrongs, a death seen; each memory
            // moves a belief MemoryMax at most and all of them together 0.2, so no one memory flips anything on its own
            if (c != null && c.Memories != null)
            {
                float dangerous = 0f, liar = 0f, kind = 0f, decent = 0f, honest = 0f;
                foreach (MindMemory m in c.Memories)
                {
                    if (m == null)
                        continue;
                    bool you = Minds.AboutYou(m);
                    if (you && m.Kind == "deed:violence")
                        dangerous += MemoryMax;
                    else if (you && m.Kind == "deed:theft")
                        liar += MemoryMax;
                    else if (you && m.Kind == "deed:kindness")
                        kind += MemoryMax;
                    if (m.Kind == "death" && m.Sub == "seen")
                        decent -= MemoryMax;   // a death seen in the street (the game's people's; the player's killings reach it through the appraisal)
                    if (m.How == "did" && m.Role == "doer" && (m.Kind == "theft" || m.Kind == "blows"))
                        honest -= MemoryMax;   // one's own theft or rough handling (a brawl is mutual: no doer)
                }
                b[Dangerous] = Clamp(b[Dangerous] + Math.Min(0.2f, dangerous));
                b[Liar] = Clamp(b[Liar] + Math.Min(0.2f, liar));
                b[Kind] = Clamp(b[Kind] + Math.Min(0.2f, kind));
                b[Decent] = Clamp(b[Decent] + Math.Max(-0.2f, decent));
                b[Honest] = Clamp(b[Honest] + Math.Max(-0.2f, honest));
            }
        }

        /// What an appraised feeling moves, as deltas added to `moved` (the person's in-memory deltas, Count long);
        /// true when anything moved. An everyday event moves a belief EverydayMax at most; a killing seen may move more
        /// and can flip a belief on its own. A chip against what they hold (or doubt) with confidence weighs less, the
        /// surer the less (belief perseverance); `baseline` may be null (no perseverance then). The deltas never run
        /// past -1..1. `t` may be null.
        public static bool Move(float[] moved, Feeling f, AppraisalEvent e, Traits t, float[] baseline)
        {
            if (moved == null || f == null || e == null || f.Kind == "none")
                return false;
            float i = Math.Max(0f, Math.Min(1f, f.Intensity));
            bool bold = T(t, Traits.Assertiveness) > 0.5f || T(t, Traits.Risk) > 0.5f;
            bool bigEvent = e.Kind == "killing" || e.Kind == "revived";
            float cap = bigEvent ? 0.5f : EverydayMax;
            float[] bl = bigEvent ? null : baseline;   // a killing seen or a buried man walking past isn't argued with: no perseverance against it
            bool any = false;
            switch (f.Kind)
            {
                case "guilt":
                    any |= Add(moved, bl, Honest, Cap(-0.1f * i, cap));   // guilt chips "an honest man"
                    break;
                case "fear":
                    if (bold)
                        any |= Add(moved, bl, LookAfter, Cap(-0.08f * i, cap));   // fear in the bold chips "I can look after myself"
                    if (e.Doer == "player")
                        any |= Add(moved, bl, Dangerous, Cap(0.1f * i, cap));
                    if (e.Kind == "uncanny" || e.Kind == "revived")
                        any |= Add(moved, bl, World, Cap(e.Kind == "revived" ? -0.6f : -0.08f * i, cap));   // the revived dead flip "as it seems" on their own; the bell and the loops chip it
                    if (e.Kind == "killing")
                        any |= Add(moved, bl, Decent, Cap(-0.3f * i, cap));
                    break;
                case "anger":
                    if (e.Doer == "player")
                        any |= Add(moved, bl, Dangerous, Cap(0.04f * i, cap));   // anger says hostile more than dangerous; half weight (the review)
                    if (e.Doer == "lawman")
                        any |= Add(moved, bl, LawForMe, Cap(-0.1f * i, cap));
                    break;
                case "gratitude": case "joy": case "relief":
                    if (e.Doer == "player")
                        any |= Add(moved, bl, Kind, Cap(0.1f * i, cap));
                    any |= Add(moved, bl, Decent, Cap(0.02f * i, cap));   // a good turn keeps "a decent place" (beliefs are kept up by what agrees with them, not only worn by what doesn't)
                    any |= Add(moved, bl, World, Cap(0.02f * i, cap));
                    break;
                case "shame":
                    any |= Add(moved, bl, Somebody, Cap((bold ? -0.03f : -0.06f) * i, cap));   // blame sticks less than credit for the bold (the self-serving bias)
                    break;
                case "pride":
                    any |= Add(moved, bl, Somebody, Cap(0.06f * i, cap));
                    any |= Add(moved, bl, LookAfter, Cap(0.04f * i, cap));
                    any |= Add(moved, bl, Honest, Cap(0.02f * i, cap));
                    break;
                case "grief":
                    if (e.Kind == "killing")
                        any |= Add(moved, bl, Decent, Cap(-0.3f * i, cap));
                    break;
                case "contempt":
                    if (e.Doer == "player")
                        any |= Add(moved, bl, Liar, Cap(0.04f * i, cap));
                    break;
            }
            if (e.Kind == "theft" && e.Doer == "player")
                any |= Add(moved, bl, Liar, Cap(0.05f * i, cap));
            return any;
        }

        // one delta onto a belief: against a confident belief it weighs less (0.2 of itself at 0.9 or 0.1); the sum stays in -1..1
        private static bool Add(float[] moved, float[] baseline, int i, float delta)
        {
            if (delta == 0f)
                return false;
            if (baseline != null)
            {
                float now = Of(baseline, moved, i);
                bool against = delta < 0f ? now >= Flip : now < Flip;
                if (against)
                    delta *= Math.Max(0.2f, 1f - Math.Abs(now - Flip) * 2f);
            }
            moved[i] = Math.Max(-1f, Math.Min(1f, moved[i] + delta));
            return true;
        }

        /// A belief now: its baseline plus what's moved it, 0..1.
        public static float Of(float[] baseline, float[] moved, int i)
        {
            return Clamp((baseline != null ? baseline[i] : 0.5f) + (moved != null ? moved[i] : 0f));
        }

        public static bool Holds(float confidence)
        {
            return confidence >= Flip;
        }

        /// Which beliefs hold, as bits (bit i for belief i), for the record of what held at the last look.
        public static int HeldBits(float[] baseline, float[] moved)
        {
            int bits = 0;
            for (int i = 0; i < Count; i++)
                if (Holds(Of(baseline, moved, i)))
                    bits |= 1 << i;
            return bits;
        }

        /// The flips between two looks, in words ("now that stranger is a dangerous man, no longer things are as they
        /// seem"), or null for none.
        public static string Flips(int was, int now)
        {
            if (was == now)
                return null;
            string s = null;
            for (int i = 0; i < Count; i++)
            {
                bool a = (was & (1 << i)) != 0, b = (now & (1 << i)) != 0;
                if (a == b)
                    continue;
                s = (s == null ? "" : s + ", ") + (b ? "now " : "no longer ") + Words[i];
            }
            return s;
        }

        /// How this person settles a memory against a strong belief: "self" (the honest or the pious, the guilt-prone:
        /// it comes onto the self-belief), "others" (the anxious and the curious move what they believe of the town and
        /// others first), "rewrite" (the rest: the memory bends in their own favour, the drift's "he had it coming").
        public static string Road(Traits t)
        {
            if (T(t, Traits.Honesty) > 1f || T(t, Traits.Piety) > 1f)
                return "self";
            if (T(t, Traits.Anxiety) > 1f || T(t, Traits.Curiosity) > 1f)
                return "others";
            return "rewrite";
        }

        /// Dissonance: a memory against a strong belief (0.7 or more): the road this person takes, or null when nothing
        /// contradicts.
        public static string Dissonance(MindMemory m, float[] beliefs, Traits t)
        {
            if (m == null || beliefs == null)
                return null;
            bool against = (Minds.AboutYou(m) && m.Kind == "deed:kindness" && beliefs[Dangerous] >= 0.7f) ||
                           (Minds.AboutYou(m) && m.Kind == "deed:violence" && beliefs[Kind] >= 0.7f) ||
                           (m.How == "did" && m.Role == "doer" && (m.Kind == "theft" || m.Kind == "blows") && beliefs[Honest] >= 0.7f) ||
                           (m.Kind == "death" && m.Sub == "seen" && beliefs[Decent] >= 0.7f);
            return against ? Road(t) : null;
        }

        /// How one who rewrites (Road "rewrite") reads a memory against a strong belief (Dissonance): one's own theft or
        /// blows against "an honest man" is deserved, the player's kindness against "dangerous" a trick, his violence
        /// against "kind" provoked, a death seen against "a decent place" outsiders' doing. Null when the memory stands
        /// (no belief against it, or a nature that takes it onto the self or moves what it believes of others instead).
        public static string Slant(MindMemory m, float[] beliefs, Traits t)
        {
            if (Dissonance(m, beliefs, t) != "rewrite")
                return null;
            if (Minds.AboutYou(m) && m.Kind == "deed:kindness")
                return "trick";
            if (Minds.AboutYou(m) && m.Kind == "deed:violence")
                return "provoked";
            if (m.How == "did" && m.Role == "doer" && (m.Kind == "theft" || m.Kind == "blows"))
                return "deserved";
            if (m.Kind == "death" && m.Sub == "seen")
                return "outsiders";
            return null;
        }

        /// The beliefs in words for the log: the held ones, the doubted ones.
        public static string Describe(float[] baseline, float[] moved)
        {
            var held = new List<string>();
            var doubted = new List<string>();
            for (int i = 0; i < Count; i++)
            {
                float v = Of(baseline, moved, i);
                if (i >= Dangerous && i <= Liar && v < 0.35f)
                    continue;   // the player: only what they've come to think
                (Holds(v) ? held : doubted).Add(Words[i] + " " + v.ToString("0.00"));
            }
            return "holds " + (held.Count > 0 ? string.Join(", ", held) : "nothing much") + (doubted.Count > 0 ? "; doubts " + string.Join(", ", doubted) : "");
        }

        /// Where the belief about the world and the Awakening disagree (the belief doubts it while the stage is 0 or 1,
        /// or holds it at stage 3 or 4), and where the belief about the player and Regard do (fond but dangerous, hostile
        /// but kind). For the log only; nothing drives either yet.
        public static string Disagreements(float[] baseline, float[] moved, int wakeStage, string regard)
        {
            var parts = new List<string>();
            float world = Of(baseline, moved, World);
            if (!Holds(world) && wakeStage <= 1)
                parts.Add("doubts the world while the Awakening says asleep");
            if (Holds(world) && wakeStage >= 3)
                parts.Add("holds the world as it seems while the Awakening says unravelling");
            float dangerous = Of(baseline, moved, Dangerous), kind = Of(baseline, moved, Kind);
            if (regard == "fond" && dangerous >= 0.6f)
                parts.Add("Regard says fond, the belief says dangerous");
            if (regard == "hostile" && kind >= 0.6f)
                parts.Add("Regard says hostile, the belief says kind");
            return parts.Count == 0 ? null : string.Join("; ", parts);
        }
    }
}
