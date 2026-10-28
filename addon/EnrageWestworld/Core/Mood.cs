// Mood: the NPC mind's hours layer (the mind design, step 4; daytime run 2026-10-27, shadow mode). A feeling (Appraisal)
// fades over game minutes; while it lasts, and for hours after, it moves the mood: pleasant or not, keyed up or flat, in
// control or not (the PAD space, Mehrabian and Russell), each feeling's push taken from the ALMA model's table of the OCC
// emotions in PAD (Gebhard, 2005). The mood drifts back to the person's own baseline, set by their traits (Mehrabian,
// 1996: pleasure from extraversion, agreeableness and steadiness; arousal from openness and agreeableness, against
// steadiness; dominance from extraversion, openness and order, against agreeableness). Worked out in closed form when
// asked (nothing updates per frame): the baseline plus each remembered feeling's push, halved every MoodHalfLife.
// In memory only (MindShadow), capped, forgotten with the rest of the shadow. Plain C#, no game.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    /// One feeling, remembered for its push on the mood.
    public struct FeltAt
    {
        public int Kind;          // index into Feeling.Kinds
        public float Intensity;
        public long At;           // game minute
        public string About;      // whom (a name, "you"), for the log and F7
        public string AboutId;    // the same by id ("player"), or null; DoerId whoever did it (Feeling's), for the thought's words
        public string DoerId;
        public bool Masked;       // the face hid it (Display: masked or turned), so the thought may say the mask too (night 2026-10-28)
    }

    /// A mood: pleasant or not, keyed up or flat, in control or not, each -1..1.
    public struct PadMood
    {
        public float P, A, D;

        public PadMood(float p, float a, float d)
        {
            P = p;
            A = a;
            D = d;
        }

        public float DistanceTo(PadMood o)
        {
            return Math.Max(Math.Abs(P - o.P), Math.Max(Math.Abs(A - o.A), Math.Abs(D - o.D)));
        }

        public string Numbers()
        {
            return "pleasure " + P.ToString("+0.00;-0.00") + ", arousal " + A.ToString("+0.00;-0.00") + ", dominance " + D.ToString("+0.00;-0.00");
        }
    }

    public static class Mood
    {
        /// Game minutes for the mood's push to halve (three hours: an everyday upset is gone by the next morning).
        public const float MoodHalfLife = 180f;
        /// How much of a feeling's PAD direction moves the mood at full strength.
        public const float Push = 0.6f;

        // ALMA's OCC emotions in PAD (Gebhard 2005), in Feeling.Kinds order: fear, anger, shame, guilt (remorse), grief
        // (distress), pride, gratitude, envy (resentment), pity, contempt (reproach), relief, joy
        private static readonly float[,] Pad =
        {
            { -0.64f, 0.60f, -0.43f }, { -0.51f, 0.59f, 0.25f }, { -0.30f, 0.10f, -0.60f }, { -0.30f, 0.10f, -0.60f }, { -0.40f, -0.20f, -0.50f },
            { 0.40f, 0.30f, 0.30f }, { 0.40f, 0.20f, -0.30f }, { -0.20f, -0.30f, -0.20f }, { -0.40f, -0.20f, -0.50f }, { -0.30f, -0.10f, 0.40f },
            { 0.20f, -0.30f, 0.40f }, { 0.40f, 0.20f, 0.10f }
        };

        // how long each feeling itself lasts (game minutes to halve): fright and pity go quickly, grief lingers
        private static readonly float[] FeelingHalfLife = { 10f, 20f, 30f, 60f, 240f, 30f, 60f, 45f, 20f, 20f, 15f, 30f };
        // how long its push on the mood lasts (game minutes to halve): most by the next morning (MoodHalfLife), shame and
        // guilt twice as long, grief for days (a friend shot down is not forgotten by breakfast)
        private static readonly float[] MoodHalfLives = { MoodHalfLife, MoodHalfLife, 360f, 360f, 2880f, MoodHalfLife, MoodHalfLife, MoodHalfLife, MoodHalfLife, MoodHalfLife, MoodHalfLife, MoodHalfLife };

        /// How much of a remembered feeling still moves the mood now (0..1 of its full push).
        public static float Weight(FeltAt f, long now)
        {
            long age = now - f.At;
            if (age < 0 || f.Kind < 0 || f.Kind >= MoodHalfLives.Length)
                return 0f;
            return f.Intensity * (float)Math.Pow(0.5, age / MoodHalfLives[f.Kind]);
        }

        /// Their own resting mood, from their traits (Mehrabian 1996), kept modest (most people rest near the middle).
        public static PadMood Baseline(Traits t)
        {
            if (t == null)
                return new PadMood(0f, 0f, 0f);
            float o = t.Domain(0) / 3f, c = t.Domain(1) / 3f, e = t.Domain(2) / 3f, a = t.Domain(3) / 3f, steady = -t.Domain(4) / 3f;
            float p = 0.21f * e + 0.59f * a + 0.19f * steady, ar = 0.15f * o + 0.30f * a - 0.57f * steady, d = 0.25f * o + 0.17f * c + 0.60f * e - 0.32f * a;
            return new PadMood(Clamp(p, 0.5f), Clamp(ar, 0.5f), Clamp(d, 0.5f));
        }

        /// The mood now: the baseline and every remembered feeling's push, each halved every MoodHalfLife.
        public static PadMood Now(PadMood baseline, List<FeltAt> felt, long now)
        {
            float p = baseline.P, a = baseline.A, d = baseline.D;
            if (felt != null)
                for (int i = 0; i < felt.Count; i++)
                {
                    FeltAt f = felt[i];
                    float w = Push * Weight(f, now);   // a feeling from after now (a rewind) moves nothing
                    if (w <= 0f)
                        continue;
                    p += w * Pad[f.Kind, 0];
                    a += w * Pad[f.Kind, 1];
                    d += w * Pad[f.Kind, 2];
                }
            return new PadMood(Clamp(p, 1f), Clamp(a, 1f), Clamp(d, 1f));
        }

        /// How strongly a feeling is still felt now (it fades over minutes).
        public static float Lasting(FeltAt f, long now)
        {
            long age = now - f.At;
            if (age < 0 || f.Kind < 0 || f.Kind >= FeelingHalfLife.Length)
                return 0f;
            return f.Intensity * (float)Math.Pow(0.5, age / FeelingHalfLife[f.Kind]);
        }

        /// The mood in a word or two, against their own baseline: sore or glad (pleasure), keyed up or flat (arousal),
        /// cowed or bold (dominance); "even" when it's near their usual.
        public static string Words(PadMood now, PadMood baseline)
        {
            var w = new List<string>(3);
            float dp = now.P - baseline.P, da = now.A - baseline.A, dd = now.D - baseline.D;
            if (dp <= -0.12f) w.Add("sore");
            else if (dp >= 0.12f) w.Add("glad");
            if (da >= 0.15f) w.Add("keyed up");
            else if (da <= -0.15f) w.Add("flat");
            if (dd <= -0.15f) w.Add("cowed");
            else if (dd >= 0.15f) w.Add("bold");
            return w.Count == 0 ? "even" : string.Join(" and ", w);
        }

        /// The old two-word mood (InnerLife.MoodOf's "sore", "glad" or null) read from the mind's: for comparing them, and
        /// for MindLive.
        public static string SoreOrGlad(PadMood now, PadMood baseline)
        {
            float dp = now.P - baseline.P;
            return dp <= -0.12f || now.P <= -0.35f ? "sore" : dp >= 0.12f || now.P >= 0.35f ? "glad" : null;   // a sour nature is sore at rest; a sunny one glad
        }

        private static float Clamp(float v, float lim)
        {
            return v < -lim ? -lim : v > lim ? lim : v;
        }
    }
}
