// The supper invitation, the player version (the private design doc's macabre encounters, no. 3; Lex's OK 2026-10-27
// 18:33: "This ok and we can add it in"). At dusk, out of the towns, a too-friendly host on the road invites the player
// to supper at his place off the road. Follow him there on foot and the screen goes dark: supper, a drink, nothing
// after. He wakes in a ditch nearby at first light, his head pounding and most of his cash gone, which he can't undo.
// Ride on and the host lets him go. The game side does the host, the walk, the dark and the waking; this has the hours,
// how much is taken and the words. All lines are original to this add-on.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Supper
    {
        /// Dusk: from half past five to nine in the evening.
        public static bool Hour(int hour, int minute)
        {
            return hour >= 18 && hour < 21 || hour == 17 && minute >= 30;
        }

        /// How much is taken, in cents: most of the cash (three quarters to all of it, by the seed), never more than
        /// $250 (it should sting, not ruin a save); nothing from someone with under two dollars.
        public static int Take(int cashCents, string seed)
        {
            if (cashCents < 200)
                return 0;
            int share = 75 + Voice.Hash(seed + "|take") % 26;
            return Math.Min(25000, (int)((long)cashCents * share / 100));
        }

        /// Whole dollars for the words, rounded.
        public static int Dollars(int cents)
        {
            return (cents + 50) / 100;
        }

        /// Game hours from now until first light (six in the morning), at least two.
        public static int HoursToDawn(int hour)
        {
            int h = hour >= 6 ? 30 - hour : 6 - hour;
            return Math.Max(2, h);
        }

        public static string Invite(string seed)
        {
            return Voice.Pick(seed + "si",
                "Evening, friend! You look like a man who's missed his supper. There's stew on, and plenty of it.",
                "Light's going. No sense riding on hungry. Come up to the house, we've a place laid.",
                "Well now, a traveller! We don't see many. Supper's on the table, and you're welcome to it.",
                "Mister! Sit down to supper with us? The wife cooks for six and there's only the two of us.");
        }

        public static string Lead(string seed)
        {
            return Voice.Pick(seed + "sl", "This way. It's no distance.", "Just up here. You'll want a drink first, I'd wager.", "Follow along. The wife'll be glad of the company.");
        }

        public static string LetGo(string seed)
        {
            return Voice.Pick(seed + "sg", "Suit yourself. The latchstring's always out.", "Another time, then. We're always here.", "Your loss, friend. It's a fine stew.");
        }

        /// Aimed at: hands raised, he backs away.
        public static string Backs(string seed)
        {
            return Voice.Pick(seed + "sb", "Easy, mister! Only meant to feed you.", "Whoa, now. No call for that. I'm going.", "All right! All right. Supper's off.");
        }

        /// What he really thinks while he smiles (over his head once he's asked): robbery, nothing worse.
        public static string HostThought(string seed)
        {
            return Voice.Pick(seed + "st", "Good boots. Full pockets, likely.", "A few drops in his cup and he'll sleep till sunup.", "Easy, now. Let him come to the table.", "That coat's worth a month. The purse'll be worth more.");
        }

        /// What the player is told when he wakes ($ taken in whole dollars).
        public static string Waking(int dollars, string seed)
        {
            return dollars > 0
                ? Voice.Pick(seed + "sw", "You wake in a ditch at first light. The last you recall is his cup, and your pockets are $" + dollars + " lighter.",
                                          "Dawn. Cold mud under your cheek, a taste like copper, and $" + dollars + " gone from your coat. Whatever was in that cup, it wasn't coffee.",
                                          "First light. A ditch, a thick head, no host, no house in sight, and $" + dollars + " less than you had.")
                : Voice.Pick(seed + "sx", "You wake in a ditch at first light, the taste of his cup still in your mouth. Your pockets were turned out, and there was nothing to take.",
                                          "Dawn, and cold mud. Somebody turned out your pockets and left them empty, which they were already.");
        }

        public static IEnumerable<string> Lines()
        {
            for (int i = 0; i < 16; i++)
            {
                string s = "s" + i;
                yield return Invite(s);
                yield return Lead(s);
                yield return LetGo(s);
                yield return Backs(s);
                yield return HostThought(s);
                yield return Waking(i * 17 + 1, s);
                yield return Waking(0, s);
            }
        }
    }
}
