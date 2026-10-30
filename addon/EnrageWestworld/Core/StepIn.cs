// Who steps into trouble between two others, by their own minds (roadmap 39, the street reacts from the mind; night
// 2026-10-29, item 2; with [Mind] MindLive and [Mind] MindStandUp). Lex, 2026-10-29: "Ideally we want NPCs to dynamically
// react based on their inner psychology." Each onlooker the mind knows takes the trouble the way the shadow would take it
// once it had happened (Appraisal.From and Of, the one road): their own part in it (a friend of the one picked on, a
// friend of the bully, a stranger), the crowd watching (Appraisal.CrowdBar: each one's own chance falls as the crowd
// grows while the chance that somebody steps in rises; Philpot et al. 2020) and their own nature; 1899's display rules
// then say whether that urge may show here (Display.Shows: a woman of standing keeps her anger off her face, the law and
// the church hush it, drink loosens it). The one whose urge is to step in, and whose face may show it, steps in: the
// strongest feeling first, the nearer on a near tie. The bully's answer comes from his own mind too: faced before the
// street, anger holds his ground and fear or shame backs him down. Who may be moved is still the game side's
// (MayDirect), and how often still the pacing rule's rests (Lex, 2026-10-29: the mind decides, the roll goes, the rests
// stay). Pure: the same people and the same trouble give the same answer. Plain C#, no game.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    /// Someone watching trouble between two others, whom the mind already knows.
    public sealed class Onlooker
    {
        public Person P;         // who they are (someone the add-on already has an identity for)
        public Traits T;         // their nature (the shadow's)
        public float Metres;     // how far from the trouble
        public bool Drunk;       // in their cups
        public int WakeStage;    // the Awakening's stage (drink and waking loosen the display rules)
        public int Handle;       // the game side's, carried through
    }

    /// How one onlooker takes it, and whether it moves them to step in.
    public sealed class StepInTake
    {
        public Onlooker Who;
        public AppraisalEvent Event;   // null when it's nothing to them
        public Feeling Felt;           // null with no event
        public string Shows;           // what 1899 lets show of it here (Display.Ways)
        public bool StepsIn;
        public string WhyNot;          // when they don't, in a few words, for the log
    }

    public static class StepIn
    {
        /// Within this much of the strongest feeling, the nearer one is first to it.
        public const float NearTie = 0.05f;

        /// The trouble about to be played, as the happening the onlookers take it for: A (the bully in a rough, the one who
        /// squared up first in a brawl) and B, here, before them. Never recorded anywhere: the scene records its own when
        /// it plays.
        public static Happening Trouble(string sceneKind, string aId, string aName, string bId, string bName, string town, string place, long at, IEnumerable<Onlooker> watching)
        {
            var h = new Happening { Kind = "scene:" + sceneKind, DoerId = aId, DoerName = aName, TargetId = bId, TargetName = bName, Town = town, Place = place, At = at };
            if (watching != null)
                foreach (Onlooker o in watching)
                    if (o != null && o.P != null)
                        h.Witnesses.Add(new EventWitness { Id = o.P.Id, Name = o.P.Name, Metres = o.Metres, How = "saw" });
            return h;
        }

        /// A woman whose mind says step in does so this often, in percent (Lex, 2026-10-29: women get into fights half as
        /// often as men until the inner mind takes that over; a seeded roll, so the same trouble gives the same answer).
        public const int WomanStepsInPercent = 50;

        /// Each onlooker's take on it, in the order given. `crowd`: how many watch it in all (the onlookers here and the
        /// rest the add-on can't move; not the two, not the player); `law`: a lawman standing by; `seed`: this trouble's own.
        public static List<StepInTake> Takes(Happening trouble, IList<Onlooker> watching, Func<string, string, int> warmth, int crowd, bool law, string seed = null)
        {
            var takes = new List<StepInTake>();
            if (trouble == null || watching == null)
                return takes;
            bool rough = trouble.Kind == "scene:rough";
            foreach (Onlooker o in watching)
            {
                var take = new StepInTake { Who = o };
                takes.Add(take);
                if (o == null || o.P == null || o.P.Id == null || o.P.Id == trouble.DoerId || o.P.Id == trouble.TargetId)
                {
                    take.WhyNot = "not an onlooker";
                    continue;
                }
                // a rough is A's doing to B (the doer by the scene's own shape: the one road with the doer handed in, as
                // the tie the scene leaves will carry it); a brawl is each at the other
                AppraisalEvent e = Appraisal.From(trouble, o.P.Id, o.P.Woman, o.P.Home, warmth, rough ? trouble.DoerId : null, false, Math.Max(0, crowd));
                if (e == null)
                {
                    take.WhyNot = "nothing to them";
                    continue;
                }
                e.Drunk = o.Drunk;
                e.Law = law;
                e.WakeStage = o.WakeStage;
                take.Event = e;
                take.Felt = Appraisal.Of(e, o.T);
                take.Shows = Display.Shows(take.Felt, e, o.T);
                int toA = warmth != null ? warmth(o.P.Id, trouble.DoerId) : 0, toB = warmth != null ? warmth(o.P.Id, trouble.TargetId) : 0;
                if (take.Felt.Urge != "intervene")
                    take.WhyNot = take.Felt.Kind == "none" ? "felt nothing much" : "would " + take.Felt.Urge;
                else if (take.Shows == "masked")
                    take.WhyNot = "keeps it off their face";
                else if (rough && (toB < 0 || toA > 0))
                    take.WhyNot = toB < 0 ? "no friend of " + Person.FirstOf(trouble.TargetName) : "a friend of " + Person.FirstOf(trouble.DoerName);   // the words stand up for B against A
                else if (o.P.Woman && !Voice.Chance((seed ?? trouble.Kind + trouble.At + trouble.DoerId) + "|" + o.P.Id + "|woman", WomanStepsInPercent))
                    take.WhyNot = "a woman, half as likely for now";
                else
                    take.StepsIn = true;
            }
            return takes;
        }

        /// Who steps in: of those whose take says so, the strongest feeling; within NearTie of it, the nearer; then by id,
        /// so the same people give the same answer. Null: nobody.
        public static StepInTake Who(List<StepInTake> takes)
        {
            StepInTake best = null;
            if (takes == null)
                return null;
            foreach (StepInTake t in takes)
            {
                if (!t.StepsIn)
                    continue;
                if (best == null)
                {
                    best = t;
                    continue;
                }
                float d = t.Felt.Intensity - best.Felt.Intensity;
                if (d > NearTie || Math.Abs(d) <= NearTie && (t.Who.Metres < best.Who.Metres - 0.01f ||
                                                              Math.Abs(t.Who.Metres - best.Who.Metres) <= 0.01f && string.CompareOrdinal(t.Who.P.Id, best.Who.P.Id) < 0))
                    best = t;
            }
            return best;
        }

        /// How many of the takes would step in (the chosen one among them).
        public static int Willing(List<StepInTake> takes)
        {
            int n = 0;
            if (takes != null)
                foreach (StepInTake t in takes)
                    if (t.StepsIn)
                        n++;
            return n;
        }

        /// How the bully takes being faced before the street. A man stepping in is a challenge (words, and the threat of a
        /// fight); a woman's is a rebuke: the shame of being called out before the street, mostly his own, with half a
        /// man's threat in it (Lex, 2026-10-29: women may fight and be fought in this mod, half as likely as a man until the
        /// mind takes that over too). Null when there's nothing to go on.
        public static Feeling Faced(Person bully, Traits t, Person by, Func<string, string, int> warmth, int crowd, string place, string town, bool law, bool drunk, int wakeStage)
        {
            if (bully == null || by == null)
                return null;
            int toBy = warmth != null ? warmth(bully.Id, by.Id) : 0;
            var e = new AppraisalEvent
            {
                Kind = by.Woman && !bully.Woman ? "rebuke" : "challenge", Role = "self", Doer = toBy != 0 ? "known" : "stranger", WarmDoer = toBy, Place = place ?? "street",
                Woman = bully.Woman, WomenPresent = true, Company = bully.Home != null && bully.Home == town ? "neighbours" : "strangers", Witnesses = Math.Max(0, crowd),
                Drunk = drunk, Law = law, WakeStage = wakeStage, DoerName = by.Name, DoerId = by.Id, TargetName = bully.Name, TargetId = bully.Id
            };
            return Appraisal.Of(e, t);
        }

        /// Whether that feeling backs him down: fear, shame or guilt, or an urge to get away, hide or make it right. Anger,
        /// contempt or nothing much holds his ground: he waves them off and goes on.
        public static bool BacksDown(Feeling f)
        {
            if (f == null)
                return false;
            // the urge first: a shrug ("dismiss": mild shame in one who doesn't much care how he's seen) holds his ground
            if (f.Urge == "avoid" || f.Urge == "hide" || f.Urge == "withdraw" || f.Urge == "amends" || f.Urge == "freeze")
                return true;
            if (f.Urge == "dismiss" || f.Urge == "confront" || f.Urge == "disparage")
                return false;
            return f.Kind == "fear" || f.Kind == "shame" || f.Kind == "guilt";
        }

        /// One take in a few words, for the log: "Rosa Wadsworth: pity (strong), urge intervene, shows shown".
        public static string Words(StepInTake t)
        {
            if (t == null || t.Who == null || t.Who.P == null)
                return "nobody";
            string n = t.Who.P.Name ?? t.Who.P.Id;
            if (t.Felt == null)
                return n + ": " + (t.WhyNot ?? "nothing");
            return n + ": " + t.Felt.Kind + (t.Felt.Kind != "none" ? " (" + t.Felt.Strength + ")" : "") + ", urge " + t.Felt.Urge + ", shows " + t.Shows + (t.StepsIn ? "" : " (" + t.WhyNot + ")");
        }
    }
}
