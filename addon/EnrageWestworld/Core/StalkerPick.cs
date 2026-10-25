// Who near you becomes the stalker: the one with the most history with you. enRAGE's townsfolk who know you come
// first (a grudge counts most, then what they saw you do, fear, favours and liking), then residents by what they
// saw and heard of you and how often you talked; a passer-by with no history at all comes last.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class StalkerPick
    {
        /// folk: enRAGE's record of a townsperson (null if not one); st: what RealNPCs remembers of them (null if
        /// nothing); resident: they have a name and a trade of RealNPCs' own.
        public static int Score(Folk folk, PersonState st, bool resident)
        {
            int s = resident ? 10 : 0;
            if (folk != null)
                s += 40 + folk.Grudge / 2 + folk.Fear / 3 + folk.Like / 5 + folk.Favours * 3 + (folk.Witnessed?.Count ?? 0) * 8 +
                     (folk.Met ? 10 : 0) + (folk.Reported ? 6 : 0);
            if (st != null)
                s += (st.SawDeeds?.Count ?? 0) * 6 + (st.KnownDeeds?.Count ?? 0) * 2 + st.Talks * 3 + Math.Min(10, st.Sightings) +
                     (st.KnowsName ? 5 : 0) + st.WakeStage * 3;
            return s;
        }

        /// What that history is, for the log.
        public static string Why(Folk folk, PersonState st)
        {
            List<string> w = new List<string>();
            if (folk != null)
            {
                w.Add("one of enRAGE's townsfolk");
                if (folk.Grudge > 0) w.Add("holds a grudge (" + folk.Grudge + ")");
                if (folk.Witnessed != null && folk.Witnessed.Count > 0) w.Add("saw you: " + string.Join(", ", folk.Witnessed.Take(3)));
                if (folk.Fear > 20) w.Add("afraid of you (" + folk.Fear + ")");
                if (folk.Reported) w.Add("reported you");
                if (folk.Met) w.Add("has met you");
                if (folk.Favours > 0) w.Add("favours " + folk.Favours);
            }
            if (st != null)
            {
                if (st.SawDeeds != null && st.SawDeeds.Count > 0) w.Add("saw " + st.SawDeeds.Count + " of your deeds");
                if (st.KnownDeeds != null && st.KnownDeeds.Count > 0) w.Add("heard of " + st.KnownDeeds.Count);
                if (st.Talks > 0) w.Add("talked with you " + st.Talks + (st.Talks == 1 ? " time" : " times"));
                if (st.WakeStage > 0) w.Add("waking, stage " + st.WakeStage);
            }
            return w.Count == 0 ? "no history with you" : string.Join(", ", w);
        }
    }
}
