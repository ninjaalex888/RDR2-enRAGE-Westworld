// The language layer's request from a real person's state (the mind design's step 9, the next piece; night run
// 2026-10-28, after item 14): what the shadow holds for someone (traits, mood, the feeling with most left in it, their
// needs and concerns, their memories, who is in sight, where and when) assembled into a LanguageRequest, the same way
// each time, with nothing the mind doesn't know. The game side will hand it its cached state; nothing here calls the
// game or a model. Plain C#, no game.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static partial class Language
    {
        public const int MemoriesInRequest = 2, NamesInRequest = 4;

        /// A request for one person now. `feeling`: the feeling with most left in it, or null for none; `left` how much
        /// (Mood.Lasting), which gives the strength; `about` whom it's about as this person would name them ("you" for the
        /// player, a first name, "a stranger"); `youAs` how they think of the player (used when about is "you"); `now`
        /// the game minute; `avoidLine` the written line the fallback would give.
        public static LanguageRequest RequestFor(Person p, Traits t, PadMood mood, FeltAt? feeling, float left, string about, string youAs,
                                                 NeedState needs, List<Concern> concerns, List<MindMemory> memories, IEnumerable<string> nearFirstNames,
                                                 string town, string place, int hour, int year, string weather, long now, string avoidLine)
        {
            var r = new LanguageRequest
            {
                Kind = "thought",
                Name = p != null ? p.Name : null,
                Woman = p != null && p.Woman,
                Age = p != null ? p.AgeIn(year) : 0,
                Trade = p != null ? p.Trade : null,
                Town = town,
                Place = place,
                Hour = hour,
                Year = year,
                Weather = weather,
                TraitWords = t != null ? t.Words(2) : null,
                Mood = t != null ? Mood.Words(mood, Mood.Baseline(t)) : "even",
                AvoidLine = avoidLine
            };
            if (feeling.HasValue && feeling.Value.Kind >= 0 && feeling.Value.Kind < Feeling.Kinds.Length && left >= Appraisal.Felt)
            {
                FeltAt f = feeling.Value;
                r.Feeling = Feeling.Kinds[f.Kind];
                r.Strength = Appraisal.StrengthOf(Math.Min(1f, left));
                r.About = about ?? f.About;
                r.Shows = f.Masked ? "masked" : "shown";
                if (r.About == "you")
                    r.YouAs = youAs;
            }
            else
            {
                int top = needs != null ? needs.Top(0.5f) : -1;
                if (top >= 0)
                    r.Need = NeedState.Pressing[top];
            }
            if (concerns != null && concerns.Count > 0)
            {
                string w = Needs.Words(concerns);
                if (!string.IsNullOrEmpty(w))
                {
                    int cut = w.IndexOf("; ", StringComparison.Ordinal);
                    r.Worry = cut > 0 ? w.Substring(0, cut) : w;   // the first concern alone
                }
            }
            if (memories != null)
            {
                // the two strongest, in plain words
                var picked = new List<MindMemory>();
                foreach (MindMemory m in memories)
                {
                    if (m == null)
                        continue;
                    int at = picked.Count;
                    for (int i = 0; i < picked.Count; i++)
                        if (m.Strength > picked[i].Strength)
                        {
                            at = i;
                            break;
                        }
                    picked.Insert(at, m);
                    if (picked.Count > MemoriesInRequest)
                        picked.RemoveAt(picked.Count - 1);
                }
                foreach (MindMemory m in picked)
                {
                    string d = Minds.DescribeMemory(m, now);
                    if (!string.IsNullOrEmpty(d))
                        r.Memories.Add(d);
                }
            }
            if (nearFirstNames != null)
                foreach (string n in nearFirstNames)
                {
                    if (string.IsNullOrEmpty(n) || r.Near.Contains(n))
                        continue;
                    r.Near.Add(n);
                    if (r.Near.Count >= NamesInRequest)
                        break;
                }
            return r;
        }
    }
}
