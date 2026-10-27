// The real person at the other end of a worry: the man owed in a debt, the neighbour in a feud, the one being courted.
// A link is made once for a run of worry days (InnerLife.WorryDays), to a town regular, and only for someone who can
// be met again (a regular, a saved resident, one of enRAGE's townsfolk); someone they already have history with is
// chosen first (a debt scene, a quarrel, a courtship). Then the worrier's thoughts name them, and they think about it
// from their side. Debt and feud lines speak of a man, so those counterparts are men; courting asks for the other
// sex and a fitting age. All lines are original to this add-on.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace EnrageWestworld.Core
{
    [DataContract]
    public sealed class WorryLink
    {
        [DataMember] public string Id, Name;          // the one worrying
        [DataMember] public string Kind;              // debt, feud or courting
        [DataMember] public long Arc;                 // the run of worry days it belongs to
        [DataMember] public string OtherId, OtherName;
        [DataMember] public string Town;
        [DataMember] public long At;                  // game minute made
        [DataMember] public bool Ended;               // its ending (stage 3) has been played between them: not again
    }

    public static class Links
    {
        public const int Max = 400;

        /// The worries that have a real person at the other end.
        public static bool Linkable(string kind)
        {
            return kind == "debt" || kind == "feud" || kind == "courting";
        }

        public static WorryLink Of(IEnumerable<WorryLink> all, string id, string kind, long arc)
        {
            return all.FirstOrDefault(l => l.Id == id && l.Kind == kind && l.Arc == arc);
        }

        /// What the worrier calls them: the surname for a debt or a feud ("Owe Whitlow eleven dollars"), the first
        /// name for a sweetheart.
        public static string WhoName(WorryLink l)
        {
            if (string.IsNullOrEmpty(l.OtherName))
                return null;
            return l.Kind == "courting" ? Person.FirstOf(l.OtherName) : l.OtherName.Split(' ').Last();
        }

        /// The counterpart for this person's worry: a living regular of their town (never a counter keeper, never
        /// themselves), a man for a debt or a feud, the other sex and a fitting age for courting. Someone they already
        /// have history with comes first. Null if the town has nobody suitable yet.
        public static Resident Choose(Person p, string kind, long arc, IEnumerable<Resident> residents, IEnumerable<SceneTie> ties, int year)
        {
            int myAge = p.BornYear > 0 ? year - p.BornYear : 20 + Voice.Hash(p.Id + "|age") % 45;
            List<Resident> fit = residents.Where(r => r.Regular && !r.Dead && !r.Fled && r.PostId == null && r.Town == p.Home && r.Id != p.Id &&
                (kind == "courting"
                    ? r.Woman != p.Woman && InnerLife.TradeGroup(r.Trade) != "old" && Math.Abs(Age(r, year) - myAge) <= 15 && Age(r, year) < 46
                    : !r.Woman)).ToList();
            if (fit.Count == 0)
                return null;
            // history first: a debt with the one they had words over money with, a feud with an enemy, courting with a sweetheart
            List<SceneTie> mine = ties.Where(t => t.Has(p.Id)).OrderByDescending(t => t.At).ToList();
            foreach (SceneTie t in mine)
            {
                bool fits = kind == "debt" ? t.Kind == "debt" : kind == "feud" ? t.Warmth < 0 : Ties.Sweethearts(t);
                Resident known = fits ? fit.FirstOrDefault(r => t.Has(r.Id)) : null;
                if (known != null)
                    return known;
            }
            return fit.OrderBy(r => r.Id).ElementAt(Voice.Hash(p.Id + "|link|" + kind + arc) % fit.Count);
        }

        private static int Age(Resident r, int year)
        {
            return r.BornYear > 0 ? year - r.BornYear : 35;
        }

        /// The links where this person is the one at the other end, while the worrier's run is still on.
        public static List<WorryLink> Against(IEnumerable<WorryLink> all, string id, long gameDay)
        {
            return all.Where(l => l.OtherId == id && InnerLife.ArcFor(l.Id, gameDay) == l.Arc).ToList();
        }

        /// Keeps the links for the current run and the one before (its ending is still thought about), under Max.
        public static void Prune(List<WorryLink> all, long gameDay)
        {
            all.RemoveAll(l => InnerLife.ArcFor(l.Id, gameDay) - l.Arc > 1);
            while (all.Count > Max)
                all.Remove(all.OrderBy(l => l.At).First());
        }

        // ---- the other side ---------------------------------------------------------------------------------

        private static readonly string[] Owed =
        {
            "{name} owes me eleven dollars. Saw {name} buying tobacco this morning, bold as brass.",
            "Have to call in what {name} owes me. Hate doing it. Doing it anyway."
        };
        private static readonly string[] OwedPaid = { "{name} paid up. Every cent. Didn't think I'd see it." };
        private static readonly string[] OwedTook = { "Took {name}'s mule for the debt. Feels like stealing, and the law says it ain't." };

        private static readonly string[] Neighbour =
        {
            "{name} swears I moved that fence post. I'll swear on a Bible I didn't.",
            "{name} glares at me over the fence like I owe rent on the air."
        };
        private static readonly string[] NeighbourMended = { "Shook hands with {name}. Strange. Like a stone out of my boot." };
        private static readonly string[] NeighbourBitter = { "{name} told the sheriff a tale about that fence. I'll not forget it." };

        private static readonly string[] Courted =
        {
            "{name} keeps finding reasons to pass my door. I've noticed. I let it go on.",
            "{name} went red as a brick when I said good morning."
        };
        private static readonly string[] CourtedYes = { "Said yes to {name}. Felt like jumping a creek." };
        private static readonly string[] CourtedNo = { "Had to tell {name} no. Kind as I could. Still felt cruel." };

        /// What the one at the other end thinks about it, by the worrier's first name, as the worry goes on and ends.
        public static string OtherThought(WorryLink l, long gameDay, string seed)
        {
            int stage = InnerLife.StageFor(l.Id, gameDay);
            bool good = InnerLife.EndsWellFor(l.Id, l.Kind, l.Arc);
            string[] pool;
            switch (l.Kind)
            {
                case "debt": pool = stage < 3 ? Owed : good ? OwedPaid : OwedTook; break;
                case "feud": pool = stage < 3 ? Neighbour : good ? NeighbourMended : NeighbourBitter; break;
                default: pool = stage < 3 ? Courted : good ? CourtedYes : CourtedNo; break;
            }
            return Voice.Pick(seed + "lk", pool).Replace("{name}", Person.FirstOf(l.Name));
        }

        /// Every line, for the tests.
        public static IEnumerable<string> AllLines()
        {
            return Owed.Concat(OwedPaid).Concat(OwedTook).Concat(Neighbour).Concat(NeighbourMended).Concat(NeighbourBitter).Concat(Courted).Concat(CourtedYes).Concat(CourtedNo);
        }
    }
}
