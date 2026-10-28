// The one road from a recorded happening to a person's appraisal (night 2026-10-28, after the second late review): who did
// it to whom comes from the pair's tie (a scene's cast is by who speaks first; the tie's Doer says who did the thing, and
// for the scenes whose tie carries no doer Appraisal.From takes it from the cast), the warmth between two people from
// the ties, and the event from Appraisal.From. The game side (MindAppraiseStart, MindAppraiseOne) and the offline tests
// (EventsTest, ReplayTest, Replay2Test, ShadowDayTest) all come this way, so a drift between what the ties record and
// what the appraisal assumes fails offline (two reviews of the same afternoon found three such drifts that the tests
// couldn't see, because they handed the doer in by hand). Reads only.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public sealed partial class Engine
    {
        /// Who really did it, for a one-sided scene: the pair's tie's Doer, or null when the pair has no tie or it says nothing.
        public string DoerOf(Happening ev)
        {
            if (ev == null || ev.DoerId == null || ev.TargetId == null || Store == null || Store.Ties == null)
                return null;
            foreach (SceneTie t in Store.Ties)
                if (t.Has(ev.DoerId) && t.Has(ev.TargetId))
                    return t.Doer;
            return null;
        }

        /// The warmth between two people by their tie (-3..3), 0 with none.
        public int WarmthBetween(string x, string y)
        {
            if (x == null || y == null || Store == null || Store.Ties == null)
                return 0;
            foreach (SceneTie t in Store.Ties)
                if (t.Has(x) && t.Has(y))
                    return t.Warmth;
            return 0;
        }

        private Func<string, string, int> _warmthBetween;

        /// The event this person appraises from a recorded happening, or null when it's nothing to them (Appraisal.From
        /// with the doer and the warmth from the ties). `sawCount`: those who saw it, not those who only heard, not the player.
        public AppraisalEvent AppraiseFor(Happening ev, Person p, bool heardOnly, int sawCount)
        {
            if (ev == null || p == null || p.Id == null)
                return null;
            if (_warmthBetween == null)
                _warmthBetween = WarmthBetween;
            return Appraisal.From(ev, p.Id, p.Woman, p.Home, _warmthBetween, DoerOf(ev), heardOnly, sawCount);
        }

        /// How many of a happening's witnesses saw it (not heard it, not the player).
        public static int SawCount(Happening ev)
        {
            int n = 0;
            if (ev != null && ev.Witnesses != null)
                foreach (EventWitness w in ev.Witnesses)
                    if (w.How == "saw" && w.Id != "player")
                        n++;
            return n;
        }
    }
}
