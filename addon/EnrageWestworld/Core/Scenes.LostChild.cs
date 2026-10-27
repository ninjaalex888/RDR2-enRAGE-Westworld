// A mother asking around for her lost child (overnight 2026-10-27, Tier 4): by day in a town, a woman out in the street
// has lost sight of her little boy or girl. She asks whoever is near (who answer by their own nature: a kind soul offers
// to look, the cold say no, the sharp tell her to mind her young ones better) or the player if he's close, then goes on
// calling the child's name. No child is spawned: an hour to three later the town has word the child turned up safe,
// somewhere a child would be. All lines are original to this add-on.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class LostChild
    {
        /// By day: from 08:00 to 18:00.
        public static bool Hour(int clock)
        {
            return clock >= 8 && clock < 18;
        }

        /// The child, all from the seed: a boy or a girl, a name, an age, something to know them by.
        public sealed class Child
        {
            public bool Boy;
            public string Name, Mark;
            public int Age;
            public string He { get { return Boy ? "he" : "she"; } }
            public string Him { get { return Boy ? "him" : "her"; } }
            public string Kind { get { return Boy ? "boy" : "girl"; } }
            public string Years { get { return new[] { "five", "six", "seven", "eight", "nine" }[(Age - 5) % 5] + " years old"; } }
        }

        private static readonly string[] BoyMarks = { "a brown cap too big for him", "a patch on the knee of his trousers", "a red kerchief round his neck", "hair the color of straw", "a slingshot in his back pocket" };
        private static readonly string[] GirlMarks = { "a blue ribbon in her hair", "a yellow pinafore", "a rag doll she won't put down", "freckles, ever so many freckles", "a green gingham dress" };

        /// Her own child: the same one InnerLife gives her everywhere (the fever worry's child, by her id), and the rest
        /// from her id too, so it's the same child each time.
        public static Child Of(string personId)
        {
            bool girl = Voice.Hash(personId + "|cw") % 2 == 0;
            string[] marks = girl ? GirlMarks : BoyMarks;
            return new Child { Boy = !girl, Name = Residents.FirstName(girl, personId + "|child"), Mark = marks[Voice.Hash(personId + "|m") % marks.Length], Age = 5 + Voice.Hash(personId + "|age") % 5 };
        }

        /// A woman who'd have a young child: of an age for it (or of no known age), and not of a trade with no family.
        public static bool CouldBeMother(Person p)
        {
            int age = Romance.AgeOf(p);
            return p != null && p.Woman && (age < 0 || (age >= 22 && age <= 50)) && InnerLife.HasFamily(p);
        }

        internal static string Up(string s)
        {
            return string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
        }

        /// What she thinks as it begins (over her head).
        public static string Thought(Child k, string seed)
        {
            return Voice.Pick(seed + "lt", "Only turned my back a minute. A minute.", k.Name + ". Where are you. Where are you.", "The river. Lord, not the river.",
                              "The horses. " + Up(k.He) + "'s always round the horses.", "Don't cry. Crying won't find " + k.Him + ".", "Somebody's seen " + k.Him + ". Somebody must have.");
        }

        /// To the player, said to his face (subtitled).
        public static string ToYou(Child k, string seed)
        {
            return Voice.Pick(seed + "ly", "Mister, please. Have you seen a little " + k.Kind + "? " + k.Name + ". " + Up(k.Mark) + ".",
                              "Have you seen my " + k.Kind + ", mister? " + k.Name + ", " + k.Years + ". " + Up(k.He) + " was right beside me.",
                              "Please, sir. " + k.Name + "'s wandered off. " + Up(k.Years) + ", " + k.Mark + ".");
        }

        /// The town's word, an hour to three later: where the child turned up.
        public static string Found(Child k, string mother, string seed)
        {
            string who = mother + "'s " + k.Kind + " " + k.Name;
            return Voice.Pick(seed + "lf", who + " turned up asleep in the hayloft over the livery, none the worse.",
                              who + " was found down by the creek, wet to the knees and not one bit sorry.",
                              who + " was found in the church, under a pew, eating an apple nobody paid for.",
                              who + " followed a peddler's wagon out of town. A farmer brought " + k.Him + " home.",
                              who + " was at the depot the whole time, watching for the train.",
                              who + " was found asleep in the church loft, with the hymn books for a pillow.",
                              who + " turned up at the blacksmith's, working the bellows like " + k.He + "'d been hired.");
        }

        public static IEnumerable<Child> Samples()
        {
            for (int i = 0; i < 60; i++)
                yield return Of("res:k" + i);
        }
    }

    public static partial class Scenes
    {
        /// A (the mother) asks B; B answers by manner (kind, cold, sharp); she goes on calling the child's name. Kind
        /// "comfort" when B is kind (+1), else "brushoff".
        public static Scene Lost(SceneCast c, string seed, LostChild.Child k, string manner)
        {
            string a = N(c, 0, seed), b = N(c, 1, seed);
            Scene s = new Scene { Kind = manner == "kind" ? "comfort" : "brushoff", Summary = "a mother asking around for her child" };
            s.Add(0, Voice.Pick(seed + "la", "Have you seen my " + k.Kind + ", " + b + "? " + k.Name + ". " + LostChild.Up(k.Years) + ".",
                                b + ", have you seen " + k.Name + "? " + (k.Boy ? "He" : "She") + " was right beside me.",
                                "Please, " + b + ". " + k.Name + "'s wandered off. " + (k.Boy ? "He" : "She") + "'s got " + k.Mark + "."), "scared", "face");
            if (manner == "kind")
                s.Add(1, Voice.Pick(seed + "lk", "No, " + a + ", but I'll look down by the creek. Don't you fret.", "Haven't, " + a + ". I'll ask at the livery. They turn up.",
                                    "Lord. I'll look along the back lots, " + a + ". Go on, keep asking."), "sorry")
                 .Add(0, Voice.Pick(seed + "lt2", "Bless you. Bless you.", "Thank you. Oh, thank you."), "thanks");
            else if (manner == "cold")
                s.Add(1, Voice.Pick(seed + "lc", "Can't say I have, " + a + ".", "No. Sorry, " + a + ".", "Not that I noticed."), "mild");
            else
                s.Add(1, Voice.Pick(seed + "ls", "Ought to keep a closer eye on " + k.Him + ", " + a + ".", "Not my business to mind other folks' young ones.",
                                    "If " + k.He + " were mine " + k.He + "'d know better than to wander."), "tut");
            s.Add(0, Voice.Pick(seed + "lcall", k.Name + "! " + k.Name + ", you come here this minute!", k.Name + "! Can you hear me? " + k.Name + "!"), "scared", "walkaway");
            return s;
        }

        public static IEnumerable<Scene> LostSamples(SceneCast c)
        {
            int i = 0;
            foreach (LostChild.Child k in LostChild.Samples())
                foreach (string m in new[] { "kind", "cold", "sharp" })
                    yield return Lost(c, "ls" + i++, k, m);
        }
    }

    public sealed partial class Engine
    {
        /// A mother asked around after her child: the town has word an hour to three later that the child turned up.
        public void LostChildAsked(string town, string mother, string found, long now)
        {
            long due = now + 60 + Voice.Hash(mother + now) % 120;
            Later(new LaterAct { Kind = "news", Title = "Word around " + ShortTown(town), At = now, Text = found, DueAt = due });
            Log("LOST CHILD the town will have word in " + (due - now) + " game min");
        }
    }
}
