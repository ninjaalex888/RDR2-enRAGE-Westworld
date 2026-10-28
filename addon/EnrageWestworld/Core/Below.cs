// The voice below the floor (OVERNIGHT.md Tier 4, item 6; built in the night run of 2026-10-28, item 14; West World):
// late at night in a saloon, now and then, a muffled plea comes up through the boards behind the bar from somebody
// unseen below. The barkeep hears it and says nothing; his thoughts give it away. Nothing is ever found: the voice stops
// when you go behind the bar, and the keeper has an answer for everything if you stand before him. The game side does the
// unseen body, the plea and the thoughts; this has the hours and the words. All lines are original to this add-on.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Below
    {
        /// Late: 23:00 to 04:00.
        public static bool Hour(int clock)
        {
            return clock >= 23 || clock < 4;
        }

        /// The keeper's thought while it knocks (over his head, in view). The first is the plainest; the later ones slip.
        public static string KeeperThought(int heard, string seed)
        {
            switch (Math.Min(heard, 3))
            {
                case 0: return Voice.Pick(seed + "|k0", "The stovepipe. It's the stovepipe.", "Rats under the boards again. Big ones.", "Wind in the cellar. Always was.");
                case 1: return Voice.Pick(seed + "|k1", "He'll quiet down. He always does by morning.", "Don't look at the boards. Don't look at the boards.", "Told them to keep him fed. Told them twice.");
                case 2: return Voice.Pick(seed + "|k2", "Not my doing. I only pour the drinks.", "One more night. Then it's not my concern.", "If he asks I'll say it's the stovepipe. Everybody says the stovepipe.");
                default: return Voice.Pick(seed + "|k3", "I know that voice. God help me, I know it.", "They said he'd be gone by Sunday. That was three days back.", "Count the bottles. Count them again. Don't listen.");
            }
        }

        /// What the keeper says if you stand before him while it goes on, said to you.
        public static string KeeperAnswer(string seed)
        {
            return Voice.Pick(seed + "|ans", "That? Stovepipe ticks as it cools, mister. Old building.", "Rats. We've a man coming in the week.", "I don't hear anything. You've had enough, maybe.", "Cellar settles at night. Nothing to it.");
        }

        /// The keeper's thought once you've gone behind the bar and the voice has stopped.
        public static string KeeperAfter(string seed)
        {
            return Voice.Pick(seed + "|aft", "Quiet now. Thank God. Quiet now.", "He heard me say the stovepipe, down there. Went quiet. Good man.", "Not tonight. Not with a stranger in the room.");
        }

        /// What you'd think, standing over it, if the ticker gives you a thought of your own (not used; kept for the diary).
        public static string Overheard(string seed)
        {
            return Voice.Pick(seed + "|ovr", "a knocking under the boards, slow, like somebody counting", "three knocks, a pause, three knocks", "a voice through the floor, too muffled for words");
        }

        /// The town's word next morning, if it's talked of at all (a news line, no names).
        public static string Morning(string town, string seed)
        {
            return Voice.Pick(seed + "|morn", "Somebody says there was a voice under the saloon floor last night. The barkeep says the stovepipe.",
                              "Word is a man's kept under the saloon in " + town + ". Word is also that it's rats. Take your pick.",
                              "The saloon's cellar was making a noise last night, they say. Nobody went down to look.");
        }

        public static IEnumerable<string> Lines()
        {
            for (int i = 0; i < 8; i++)
            {
                string s = "bl" + i;
                for (int k = 0; k < 4; k++)
                    yield return KeeperThought(k, s);
                yield return KeeperAnswer(s);
                yield return KeeperAfter(s);
                yield return Overheard(s);
                yield return Morning("Valentine", s);
            }
        }
    }
}
