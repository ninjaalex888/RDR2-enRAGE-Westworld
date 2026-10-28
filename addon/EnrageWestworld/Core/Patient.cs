// The doctor's patient (OVERNIGHT.md Tier 4, item 16; built in the night run of 2026-10-28, item 14): someone whose
// child has the fever (the "fever" worry, InnerLife.Worries) walks to the doctor's and back, their thoughts over their
// head on the way: hope going, the doctor's word inside, and what's left of it coming home, by the worry's stage. The
// game side does the walking; this has the hours and the words. All lines are original to this add-on.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Patient
    {
        /// The doctor's hours: 08:00 to 18:00.
        public static bool Hour(int clock)
        {
            return clock >= 8 && clock < 18;
        }

        /// Whether this worry takes someone to the doctor: the fever, while it's on (stages 1 and 2: worse, the crisis).
        public static bool Takes(string worryKind, int stage)
        {
            return worryKind == "fever" && (stage == 1 || stage == 2);
        }

        /// On the way there ({who}: the child's first name).
        public static string Going(int stage, string seed)
        {
            return stage >= 2
                ? Voice.Pick(seed + "|g2", "Hotter than yesterday. Doc has to see {who} today. Has to.", "Walk faster. {who} was talking to nobody at dawn.", "If he says the word hospital I'll carry {who} there myself.")
                : Voice.Pick(seed + "|g1", "Doc'll know what to do for {who}. He has to.", "Three days of it now. Doc'll have a powder.", "Mind the mud. Can't go in to the doctor's with mud on me. {who}'d laugh.");
        }

        /// Inside, at the doctor's (the keeper's word in their head).
        public static string Inside(int stage, string seed)
        {
            return stage >= 2
                ? Voice.Pick(seed + "|i2", "Cool cloths, he says. Cool cloths and wait. Wait for what?", "He went quiet when I said the shaking. I saw him go quiet.", "A dollar for a bottle of something brown. God make it work.")
                : Voice.Pick(seed + "|i1", "Keep {who} warm and give this twice a day. Twice a day. I can do that.", "He says it's going round. Half the town's children. That's something.", "Says it'll break by Sunday. I'll hold him to Sunday.");
        }

        /// Coming home.
        public static string Back(int stage, string seed)
        {
            return stage >= 2
                ? Voice.Pick(seed + "|b2", "Bottle in my coat. Walk, don't run. Running frightens {who}.", "If it's no better by dark I'll go back and bang on his door.", "He didn't say it would pass. He didn't say it wouldn't.")
                : Voice.Pick(seed + "|b1", "Twice a day. {who}'ll pull faces at it.", "Going round, he says. Then it'll go round and out again.", "Sunday. Hold on to Sunday.");
        }

        /// One line for the log and the town: why they went.
        public static string Why(string who, int stage)
        {
            return who + "'s fever" + (stage >= 2 ? " (the crisis)" : "");
        }

        public static IEnumerable<string> Lines()
        {
            for (int i = 0; i < 8; i++)
                for (int st = 1; st <= 2; st++)
                {
                    string s = "pt" + i;
                    yield return Going(st, s);
                    yield return Inside(st, s);
                    yield return Back(st, s);
                }
        }
    }
}
