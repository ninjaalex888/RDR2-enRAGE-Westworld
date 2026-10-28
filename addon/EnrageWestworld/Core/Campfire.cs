// Strangers at a campfire (OVERNIGHT.md Tier 4, item 3; built in the night run of 2026-10-28, item 14): at night in the
// wilds, two or three men sit round a fire off the road. One sees you and calls you over: coffee, and a short tale in
// our own words while you sit. Ride on and he lets you be. The NPC version: a traveller walking the dark comes in to
// the fire and is made room for. The game side does the fire, the spawning and the sitting; this has the hours and the
// words. All lines are original to this add-on.
using System;

namespace EnrageWestworld.Core
{
    public static class Campfire
    {
        public const int JoinerChance = 40;   // percent: a traveller comes in from the dark while you sit

        /// Night, 20:00 to 04:00.
        public static bool Hour(int clock)
        {
            return clock >= 20 || clock < 4;
        }

        /// Called over, from the fire (said to you).
        public static string Hail(string seed)
        {
            return Voice.Pick(seed + "|hail", "Evening, friend. Coffee's hot, if you've a cup.", "Come in off the road, mister. Fire's big enough for one more.",
                              "You're welcome to sit, stranger. We don't bite, and the coffee's near drinkable.", "Cold night to be riding. Come and sit a spell.");
        }

        /// When you sit down (said to you); alone at the fire, nobody to pass the pot.
        public static string Welcome(string seed, bool alone = false)
        {
            return alone ? Voice.Pick(seed + "|welcome", "Sit easy. Nobody here's anybody.", "Help yourself. It's boiled twice but it's hot.")
                         : Voice.Pick(seed + "|welcome", "There now. Pass him the pot, Clem.", "Sit easy. Nobody here's anybody.", "Help yourself. It's boiled twice but it's hot.");
        }

        /// Which of the five tales this seed tells.
        public static int TaleIndex(string seed)
        {
            return (int)(Voice.Hash(seed + "|tale") % 5);
        }

        /// The tale, three or four lines, by the one who called you over; the others' answers between (Answer).
        public static string[] Tale(string seed)
        {
            switch (TaleIndex(seed))
            {
                case 0:
                    return new[] { "Had a mule once would only cross a river backwards. Swear to God.",
                                   "Took her over the Dakota that way three times. Folks stopped on the bank to watch.",
                                   "Sold her to a preacher in the end. Told him she was pious. Wouldn't turn her back on the water, see.",
                                   "He wrote me a year on. Said she'd drowned him twice and he'd forgiven her both times." };
                case 1:
                    return new[] { "Winter of '86 I wintered in a dugout with a man called Perkins and a dog.",
                                   "Snow to the roof by Christmas. We ate the dog's share before we ate ours, and he never forgave us.",
                                   "Come the thaw Perkins walked out and never said a word. Not goodbye, not nothing.",
                                   "Dog went with him. That's the part that stings." };
                case 2:
                    return new[] { "Rode with a fella who could tell the weather by his teeth. Never wrong, not once.",
                                   "Lost the teeth in a card game in Blackwater. Put them up against a saddle.",
                                   "After that he was wrong about the weather every day of his life.",
                                   "I think on that sometimes. What else a man puts up without knowing it." };
                case 3:
                    return new[] { "This fire's on the spot where my partner's buried. Don't look like that, he'd have wanted company.",
                                   "Fell off a ridge not two hundred yards from here, looking at an eagle.",
                                   "I come by every fall and brew a pot. He liked it strong and he liked to talk.",
                                   "So I talk. You're doing him a kindness, sitting there." };
                default:
                    return new[] { "Saw a town once that wasn't on any map. Just appeared at dusk, lamps lit.",
                                   "Had a drink there. Decent whiskey. Barkeep knew my name, and I'd never said it.",
                                   "Woke in the morning in a field. No town. Not a nail, not a stone.",
                                   "Still had the glass in my hand, though. Got it in my saddlebag now." };
            }
        }

        /// What the others at the fire say between the tale's lines (a grunt, a laugh, a doubt), by which line; the
        /// mule only when it's the mule's tale.
        public static string Answer(int line, string seed, int tale = -1)
        {
            switch (line)
            {
                case 0: return tale == 0 ? Voice.Pick(seed + "|a0", "Here he goes.", "Not the mule again.", "Let him tell it.") : Voice.Pick(seed + "|a0", "Here he goes.", "Let him tell it.", "This one's true, near enough.");
                case 1: return Voice.Pick(seed + "|a1", "That's a lie, and a good one.", "I was there. It's half true.", "Go on.");
                default: return Voice.Pick(seed + "|a2", "Huh.", "Well.", "Pour the man another.");
            }
        }

        /// When you get up to go, or the tale is told and you're still sat (said to you).
        public static string Farewell(string seed)
        {
            return Voice.Pick(seed + "|bye", "Road's that way, friend. Mind the dark.", "Obliged for the company. Ride easy.", "Fire'll be here if you come back this way.");
        }

        /// Ridden past after the hail (overheard, or his thought).
        public static string Ignored(string seed)
        {
            return Voice.Pick(seed + "|ign", "Please yourself.", "Proud sort, that one.", "More coffee for us.");
        }

        /// A gun on them (said to you).
        public static string Backs(string seed)
        {
            return Voice.Pick(seed + "|gun", "Easy, now. It's a coffee pot, not a strongbox.", "Point that elsewhere, mister. We're nobody.");
        }

        /// The NPC version: a traveller comes in from the dark and the host makes room (overheard).
        public static string Joined(string seed)
        {
            return Voice.Pick(seed + "|join", "Room for one more. Sit, before you drop.", "Evening. Come in to the light, we can't see you.", "There's coffee, if you can call it that. Sit.");
        }

        public static string JoinerThanks(string seed)
        {
            return Voice.Pick(seed + "|jt", "Obliged. Walked since noon.", "God bless you. I'd about given up on a fire.", "I'll take that kindly.");
        }

        /// Every line, for the tests.
        public static System.Collections.Generic.IEnumerable<string> Lines()
        {
            for (int i = 0; i < 12; i++)
            {
                string s = "cf" + i;
                yield return Hail(s);
                yield return Welcome(s);
                foreach (string t in Tale(s))
                    yield return t;
                yield return Answer(0, s);
                yield return Answer(1, s);
                yield return Answer(2, s);
                yield return Farewell(s);
                yield return Ignored(s);
                yield return Backs(s);
                yield return Joined(s);
                yield return JoinerThanks(s);
            }
        }
    }
}
