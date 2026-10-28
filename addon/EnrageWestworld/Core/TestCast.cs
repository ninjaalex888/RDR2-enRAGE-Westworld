// The test cast (Lex, 2026-10-27, roadmap 32): the people the tests that need someone in particular are often short of
// (a woman of a mother's age, two old men, a devout preacher and a listener, three men for a bully), made on demand by
// the Testing page and kept in the save for our testing. Each is a resident like any other (a name, a temper, a mind),
// fitted to their part so the test's own rules take them, and marked (`Resident.Cast`, the part) so the story never
// uses them: never recast on a passer-by, never a town's regular, a worry's other end, an execution's captive, one of
// a gathering or a hermit, and never dropped from the save to make room.
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class TestCast
    {
        public sealed class Part
        {
            public string Id, What;
            public bool Woman, Old;
            public int Age;
            public string Devotion, Personality;   // null: as they come
        }

        public static readonly Part[] Parts =
        {
            new Part { Id = "mother", What = "a woman of a mother's age (the lost child)", Woman = true, Age = 31 },
            new Part { Id = "oldtimer", What = "an old man (the old days)", Old = true, Age = 68 },
            new Part { Id = "oldtimer2", What = "another old man (the old days)", Old = true, Age = 73 },
            new Part { Id = "preacher", What = "a devout man (the street preacher)", Age = 46, Devotion = "Devout" },
            new Part { Id = "listener", What = "a churchgoing woman (the preacher's listener)", Woman = true, Age = 38, Devotion = "Churchgoer" },
            new Part { Id = "bully", What = "a hothead (the bully)", Age = 29, Personality = "Hothead" },
            new Part { Id = "bullied", What = "a plain man (the one he picks on)", Age = 34 },
            new Part { Id = "stander", What = "a loyal man (the one who stands up to him)", Age = 41, Personality = "Loyal" }
        };

        public static Part Find(string id)
        {
            return id == null ? null : Parts.FirstOrDefault(p => p.Id == id);
        }

        /// Fits a new resident to their part: marked, their age, faith and temper as the part needs, and a trade a
        /// mother can have (the game names it from the build; a rough woman or a saloon girl couldn't be one).
        public static void Fit(Resident r, Part part, int year)
        {
            r.Cast = part.Id;
            r.BornYear = year - part.Age;
            if (part.Devotion != null)
                r.Devotion = part.Devotion;
            if (part.Personality != null)
                r.Personality = part.Personality;
            else if (part.Id == "bullied" && r.Personality == "Hothead")
                r.Personality = null;   // he's the one picked on: a hothead would start it himself
            if (part.Id == "mother" && !InnerLife.HasFamily(Residents.ToPerson(r)))
                r.Trade = "a townswoman";
        }

        /// Why this resident doesn't fit their part, by the tests' own rules (null if they do): the game logs it.
        public static string WhyNotFit(Resident r, Part part)
        {
            if (r == null || part == null)
                return "nobody";
            if (r.Cast != part.Id)
                return "not marked for the part";
            if (r.Woman != part.Woman)
                return part.Woman ? "not a woman" : "not a man";
            Person p = Residents.ToPerson(r);
            if (part.Id == "mother" && !LostChild.CouldBeMother(p))
                return "the lost child won't take her (" + Romance.AgeOf(p) + ", " + r.Trade + ")";
            if (part.Old != (r.Trade == "an old-timer"))
                return part.Old ? "not an old man's build (" + r.Trade + ")" : "an old man's build";
            if (part.Devotion != null && r.Devotion != part.Devotion)
                return "of the wrong faith (" + r.Devotion + ")";
            if (part.Id == "preacher" && !Scenes.WouldPreach(r.Devotion, r.Id))
                return "wouldn't preach";
            if (part.Personality != null && r.Personality != part.Personality)
                return "of the wrong temper (" + (r.Personality ?? "plain") + ")";
            return null;
        }
    }
}
