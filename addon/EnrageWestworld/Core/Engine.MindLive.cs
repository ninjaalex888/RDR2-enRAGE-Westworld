// The mind's own thoughts, behind [Mind] MindLive (daytime run 2026-10-27; the mind design's thoughts from feelings).
// With MindLive on, a feeling still felt at least moderately (Mood.Lasting) comes forward, in words of what is really
// felt (MindThoughts), about whom it's about (the player, someone they have a tie with, matched by id, or a stranger);
// a pressing need (Needs, 0.85 or more) comes forward some of the time (25%), so the body speaks without crowding out the
// rest. Off, nothing here runs and every thought is as it was. Both thought paths ask it: the old roll
// (InnerLife.MindFirst, set here; a feeling comes first as often as it's still felt, Lasting x 100 in 100) and the
// Minds' weighing (a candidate pulling 40 + 50 x Lasting, a need 38). Nobody thinks the same line twice in a game day
// (SaidToday, shared with the Minds). Reads only: nothing saved, nothing made (the trait cache is in memory, capped).
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public sealed partial class Engine
    {
        private bool _mindSayFailed;

        /// Hooks the old roll to the mind (InnerLife.MindFirst); it says nothing unless MindLive is on. A thought it gives
        /// is noted as said today; a feeling kept drops that person's cached thought, so it can come at once.
        private void HookMindFirst()
        {
            InnerLife.MindFirst = (p, hour, seed, told, day) =>
            {
                if (Cfg == null || !Cfg.MindLive || p == null || p.Id == null)
                    return default(KeyValuePair<string, string>);
                long now = Store.LastGameMin;
                float pull;
                KeyValuePair<string, string> say = MindSay(p, hour, seed, day, now, true, out pull);
                if (say.Key != null)
                    SaidToday(p.Id, now).Add(say.Key);
                return say;
            };
            Shadow.OnFeel = id =>
            {
                if (Cfg != null && Cfg.MindLive)
                    _thought.Remove(id);
            };
        }

        /// The mind's thought for this person now (the line and its source), or none; for the F7 dump and the tests.
        public KeyValuePair<string, string> MindFirstThought(Person p, int hour, string seed, InnerLife.Context day)
        {
            float pull;
            return MindSay(p, hour, seed, day, Store.LastGameMin, false, out pull);
        }

        /// roll: the old path's (a feeling comes first as often as it's still felt); pull: the Minds' weight for it. New
        /// code under the ticker: a fault gives nothing (every thought as it was) and is logged once.
        public int MemoriesSlanted;   // readings slanted by a belief (Beliefs.Slant), for F7
        private readonly float[] _beliefBase = new float[Beliefs.Count];
        private readonly BeliefContext _beliefCtx = new BeliefContext();

        /// How this memory reads against what this person believes (Beliefs.Slant): null for most. With MindLive only.
        /// Reads the shadow's traits and belief deltas; nothing is written.
        public string SlantOf(Person p, PersonState st, MindMemory m)
        {
            if (m == null || p == null || p.Id == null)
                return null;
            try
            {
                Traits tr = Shadow.TraitsOf(TraitSource.From(p, p.Facets != null && p.Facets.Count > 0 ? "record" : p.Id.StartsWith("folk:", StringComparison.Ordinal) ? "folk" : "resident"));
                if (tr == null)
                    return null;
                _beliefCtx.WakeStage = st != null ? st.WakeStage : 0;
                _beliefCtx.Memories = st != null ? st.Mind : null;
                _beliefCtx.Standing = Beliefs.StandingOf(InnerLife.TradeGroup(p.Trade));
                Beliefs.Fill(_beliefBase, p, tr, _beliefCtx);
                BeliefState bs = Shadow.BeliefsOf(p.Id);
                for (int i = 0; i < Beliefs.Count; i++)
                    _beliefBase[i] = Beliefs.Of(_beliefBase, bs.Moved, i);
                return Beliefs.Slant(m, _beliefBase, tr);
            }
            catch
            {
                return null;   // a fault here costs nothing but the slant
            }
        }

        private KeyValuePair<string, string> MindSay(Person p, int hour, string seed, InnerLife.Context day, long now, bool roll, out float pull, bool request = false)
        {
            pull = 0f;
            if (Cfg == null || !Cfg.MindLive || p == null || p.Id == null)
                return default(KeyValuePair<string, string>);
            try
            {
                return MindSayImpl(p, hour, seed, day, now, roll, out pull, request);
            }
            catch (Exception ex)
            {
                if (!_mindSayFailed)
                    Log("MIND live thought failed for " + p.Name + ", so the old way: " + ex);
                _mindSayFailed = true;
                pull = 0f;
                return default(KeyValuePair<string, string>);
            }
        }

        private KeyValuePair<string, string> MindSayImpl(Person p, int hour, string seed, InnerLife.Context day, long now, bool roll, out float pull, bool request = false)
        {
            pull = 0f;
            HashSet<string> said = SaidToday(p.Id, now);
            Func<string, bool> used = said.Contains;
            // a feeling still felt at least moderately
            List<FeltAt> felt = Shadow.Felt(p.Id);
            if (felt != null && felt.Count > 0)
            {
                FeltAt best = felt[0];
                float most = 0f;
                foreach (FeltAt f in felt)
                {
                    float l = Mood.Lasting(f, now);
                    if (l > most)
                    {
                        most = l;
                        best = f;
                    }
                }
                if (most >= Appraisal.Moderate && (!roll || Voice.Chance(seed + "|mindfeel", (int)Math.Round(Math.Min(1f, most) * 100f))))
                {
                    string kind = Feeling.Kinds[best.Kind], whom, who, you;
                    WhomOf(p, best, kind, day, out whom, out who, out you);
                    string line = MindThoughts.Line(kind, whom, seed, you, who, used, best.Masked);   // all of its pool said today: the other thoughts; a masked feeling says the mask too
                    if (line != null)
                    {
                        pull = 40f + 50f * Math.Min(1f, most);
                        return new KeyValuePair<string, string>(line, "mind:feeling:" + kind);
                    }
                }
            }
            // a pressing need, some of the time (a language request asks for the line itself, no roll: the review)
            if (!request && !Voice.Chance(seed + "|mindneed", 25))
                return default(KeyValuePair<string, string>);
            Traits tr = Shadow.TraitsOf(TraitSource.From(p, p.Facets != null && p.Facets.Count > 0 ? "record" : p.Id.StartsWith("folk:", StringComparison.Ordinal) ? "folk" : "resident"));
            PersonState ps = Store.Find(p.Id);
            var c = new NeedContext
            {
                Hour = hour, Minute = (int)(((now % 60) + 60) % 60), Weekday = InnerLife.Weekday, GameDay = day != null ? day.GameDay : now / 1440, Now = now,
                Weather = day != null ? day.Weather : NowWeather, Indoors = day != null ? day.Indoors : NowIndoors, Place = day != null ? day.Place : NowPlace,
                Others = day != null && day.Nearby != null ? day.Nearby.Count : 1, Feared = SawYouViolent(p.Id, 3),
                WakeStage = ps != null ? ps.WakeStage : 0, Memories = ps != null ? ps.Mind : null
            };
            NeedState n = Needs.Of(p, tr, c);
            int top = n.Top(0.85f);
            if (top < 0)
                return default(KeyValuePair<string, string>);
            string need = MindThoughts.NeedLine(top, n.ViceName, seed, used, c.Indoors ? null : c.Weather, InnerLife.TradeGroup(p.Trade), Minds.TemperGroup(p.Personality));
            if (need == null)
                return default(KeyValuePair<string, string>);
            pull = 38f;
            return new KeyValuePair<string, string>(need, "mind:need:" + NeedState.Names[top]);
        }

        // feelings toward someone that a foe's name doesn't fit ("Good friend, {who}"): a foe's kindness is a stranger's
        private static readonly string[] WarmKinds = { "gratitude", "joy", "relief", "grief", "pity" };

        /// Whose words: "you" (the player, as this person sees him), "tie" (someone they have a scene tie with, by id; a
        /// first name), else "stranger". A feeling about oneself (shame, guilt, pride) is by whoever did it, if anyone.
        /// The language request for this person now, from the shadow's state (Language.RequestFor; nothing asks a model
        /// yet: the Testing row "mind:request" and the tests; after item 14): the feeling with most left in it and whom
        /// it's about, as the live thought has them, else the pressing need; the mind's own written line as the one the
        /// fallback would give (MindLive on; off, none, since the old thought isn't worked out here). `beliefs`: what they
        /// hold now (Beliefs.Of for each), or null. Reads only.
        public LanguageRequest LanguageRequestFor(Person p, Traits t, NeedState needs, List<Concern> concerns, IEnumerable<string> nearFirstNames,
                                                  string town, string place, int hour, int year, string weather, float[] beliefs)
        {
            if (p == null || p.Id == null || t == null)
                return null;
            long now = Store.LastGameMin;
            PersonState st = Store.Find(p.Id);
            FeltAt best;
            float left;
            FeltAt? feeling = null;
            string about = null, you = null;
            if (Shadow.Strongest(p.Id, now, out best, out left) && left >= Appraisal.Felt)
            {
                string kind = Feeling.Kinds[best.Kind], whom, who;
                WhomOf(p, best, kind, null, out whom, out who, out you);
                string other = best.AboutId ?? best.DoerId;
                about = whom == "you" ? "you" : whom == "tie" ? who : other != null && other != p.Id ? "a stranger" : null;   // one's own shame or the uncanny is about nobody (the review)
                feeling = best;
            }
            else
                left = 0f;
            float pull;
            KeyValuePair<string, string> say = MindSay(p, hour, p.Id + "|request|" + now / 45, null, now, false, out pull, true);   // the line the fallback would give: the need's too, without its roll
            return Language.RequestFor(p, t, Shadow.MoodOf(p.Id, t, now), feeling, left, about, you, needs, concerns, st != null ? st.Mind : null, nearFirstNames,
                                       town, place, hour, year, weather, now, say.Key, beliefs);
        }

        private void WhomOf(Person p, FeltAt f, string kind, InnerLife.Context day, out string whom, out string who, out string you)
        {
            whom = "stranger";
            who = null;
            you = null;
            string about = f.AboutId ?? (kind == "shame" || kind == "guilt" || kind == "pride" ? f.DoerId : null);
            if (about == null || about == p.Id)
                return;
            if (about == "player")
            {
                PersonState st = Store.Find(p.Id);
                whom = "you";
                you = Voice.YouPrivate(p, st != null ? Talk.RefFor(p, st, Player) : Player, true);
                return;
            }
            SceneTie t = TieBetween(p.Id, about, day != null ? day.Ties : null) ?? TieBetween(p.Id, about, Store.Ties);
            if (t == null || t.Warmth < 0 && Array.IndexOf(WarmKinds, kind) >= 0)
                return;
            whom = "tie";
            who = Person.FirstOf(t.A == p.Id ? t.BName : t.AName);
        }

        private static SceneTie TieBetween(string a, string b, List<SceneTie> ties)
        {
            if (ties == null)
                return null;
            foreach (SceneTie t in ties)
                if (t.Has(a) && t.Has(b))
                    return t;
            return null;
        }
    }
}
