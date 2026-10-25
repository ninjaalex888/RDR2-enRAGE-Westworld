// What the day itself brings to mind: the meal that's late or the one just eaten, the small hours, payday and
// bath night, Sunday collars, wash day; the year it is (1899, with the century about to turn, or the epilogue's
// 1907, motorcars and telephones); and what the player did in this town lately, as the town tells it (nobody
// names him). All lines are original to this add-on.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static partial class InnerLife
    {
        // ---- the clock ----------------------------------------------------------------------------------

        private static readonly string[] Breakfast =
        {
            "Coffee's bitter and the bacon's burnt. Best part of the day, still.", "Belly growling before the rooster. Biscuit in my coat for later.",
            "Cold hands round a hot cup. Steam on my face. A minute of peace."
        };

        private static readonly string[] Midday =
        {
            "Noon. Belly's been complaining since ten.", "Dinner pail's got bread and a pickle. Could eat the pail.", "Smell of somebody's stew through a window. Torture, that."
        };

        private static readonly string[] Supper =
        {
            "Beans tonight. Beans tomorrow. Beans for ever and ever, amen.", "Cornbread somewhere down the row. Walking slower to keep the smell.",
            "Feet up after supper. A minute. Then the chores."
        };

        private static readonly string[] LateNight =
        {
            "Eyes heavy as horseshoes. Bed's a mile off, and a narrow one.", "Lamp oil's low. Read the same line four times. Bed, then."
        };

        private static readonly string[] SmallHours =
        {
            "Two in the morning and wide awake. Know every crack in that ceiling.", "Can't sleep. Counting what I owe instead of sheep. Sheep are cheaper.",
            "Whole town's asleep but me and the dogs."
        };

        // ---- the week -----------------------------------------------------------------------------------

        private static readonly string[] Sunday =
        {
            "Sunday collar's choking me. Preacher'll go long. He always goes long.", "Sunday. No work, they say. Nobody told the cow.",
            "Church bell. Half the town walking one way, the other half pretending not to hear."
        };

        private static readonly string[] Monday = { "Monday. Back aches just knowing it.", "Wash day. Lye on the hands, steam in the face." };

        private static readonly string[] Friday = { "Rent's due tomorrow. Counting it again doesn't make it more." };

        private static readonly string[] Saturday =
        {
            "Saturday. Pay in my pocket, for an hour or two.", "Bath night. Water's grey by the third one in. I'm the third one in.",
            "Saturday crowd in town. Everybody pretending they came for flour."
        };

        /// What the hour or the day of the week brings to mind, or null when nothing in particular does.
        private static string[] ForDay(int hour, int weekday, string seed)
        {
            string[] need = hour >= 5 && hour < 8 ? Breakfast : hour >= 11 && hour < 14 ? Midday : hour >= 17 && hour < 20 ? Supper
                          : hour >= 21 ? LateNight : hour < 4 ? SmallHours : null;
            string[] day = hour < 7 || hour >= 21 ? null : weekday == 0 ? Sunday : weekday == 1 ? Monday : weekday == 5 ? Friday : weekday == 6 ? Saturday : null;
            if (need != null && day != null)
                return Voice.Chance(seed + "wd", 50) ? need : day;
            return need ?? day;
        }

        // ---- the year -----------------------------------------------------------------------------------

        private static readonly string[] Era1899 =
        {
            "Next year the century turns. Preacher says it means something. Means new calendars.",
            "Nephew came back from Cuba with the shakes and a medal. Keeps the medal in a sock.",
            "Men selling their farms to chase gold in the Klondike. Gold or frostbite. Mostly frostbite.",
            "Fella rode a bicycle down the street in {town}. Horses lost their minds. So did I.",
            "War in the Philippines now. Never heard of the place a year ago. Now it's in the paper every day."
        };

        private static readonly string[] Era1907 =
        {
            "Motorcar came through {town} this morning. The stink of it hung on after it was gone.",
            "Telephone wire strung to the store. Ring it and somebody in Saint Denis answers. Unnatural.",
            "Paper says they're still rebuilding San Francisco. Whole city shook to the ground.",
            "Roosevelt in the paper again, grinning like he owns the country. Maybe he does.",
            "Moving pictures in Saint Denis. Five cents to watch a train come right at you.",
            "The old gangs are gone. Hanged or old. Roads are safer. Roads are duller.",
            "Two brothers from Ohio built a machine that flies. Paper says so. Paper says lots."
        };

        // ---- the town's talk ----------------------------------------------------------------------------

        private static readonly string[] TalkMurder =
        {
            "Man shot dead in {town}. Blood's scrubbed off the boards. Still walk round the spot.",
            "Somebody killed a man here and walked off calm. That's what folks keep saying. Calm.",
            "Locking the door at night since the killing. Never locked it before. Key's stiff."
        };

        private static readonly string[] TalkKill =
        {
            "Gunfight in {town}. Dead man's still the talk of the store.", "Shooting in the street lately. Kept the children in. Kept myself in, too."
        };

        private static readonly string[] TalkLaw =
        {
            "Lawman shot in {town}. Sheriff's jumpy. Looks at every stranger like a debt.", "Deputy's widow at the store today. Bought nothing. Just stood there."
        };

        private static readonly string[] TalkRobbery =
        {
            "Store got robbed. Prices went up the next day. Somebody always pays twice.", "Robbery in broad day. A gun in a man's face over a till. Over tin money."
        };

        private static readonly string[] TalkTheft =
        {
            "Things going missing round {town}. Checking my pockets more than my watch.",
            "Somebody went through a dead man's pockets right there in the street. Picked him like a chicken."
        };

        private static readonly string[] TalkHorse =
        {
            "Horse stolen off the rail. Man walked home crying. Grown man. Horse was all he had."
        };

        private static readonly string[] TalkAssault =
        {
            "Fella got beat in the street. Teeth in the dirt. Nobody stepped in. I didn't either.",
            "Bruises on the man who got knocked down, purple as plums. A stranger did it, they say."
        };

        /// The town's talk about the newest of the player's recent deeds here (he's never named: a stranger did it).
        private static string TownTalk(Person p, List<Deed> deeds, string seed, List<string> told)
        {
            string[] pool;
            switch (deeds[0].Kind)
            {
                case "murder": pool = TalkMurder; break;
                case "lawkill": pool = TalkLaw; break;
                case "robbery": pool = TalkRobbery; break;
                case "theft":
                case "looting": pool = TalkTheft; break;
                case "horsetheft": pool = TalkHorse; break;
                case "assault": pool = TalkAssault; break;
                default: pool = TalkKill; break;
            }
            return Pick(pool, p, seed, told);
        }

        /// The kinds of the player's deeds a town talks about for a few days.
        public static readonly HashSet<string> TalkedAbout = new HashSet<string> { "kill", "murder", "lawkill", "robbery", "theft", "looting", "horsetheft", "assault" };

        /// Every line of the day, the year and the town's talk, for the tests.
        public static IEnumerable<string> AllDayLines()
        {
            foreach (string[] pool in new[] { Breakfast, Midday, Supper, LateNight, SmallHours, Sunday, Monday, Friday, Saturday, Era1899, Era1907,
                                              TalkMurder, TalkKill, TalkLaw, TalkRobbery, TalkTheft, TalkHorse, TalkAssault, Again })
                foreach (string line in pool)
                    yield return line;
        }
    }
}
