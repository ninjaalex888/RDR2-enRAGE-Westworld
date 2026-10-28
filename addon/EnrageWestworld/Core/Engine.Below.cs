// The voice below the floor's morning after (Core/Below.cs): the town's word, no names (night 2026-10-28, item 14).
using System;

namespace EnrageWestworld.Core
{
    public sealed partial class Engine
    {
        /// Knocking under the saloon floor was heard tonight: the town has a word of it next morning (07:00-09:00).
        public void BelowHeard(string town, long now)
        {
            if (town == null)
                return;
            long morning = (now / 1440 + (now % 1440 >= 7 * 60 ? 1 : 0)) * 1440 + 7 * 60 + Voice.Hash(town + "|below|" + now) % 120;   // the coming morning (a plea heard after midnight is already on the new day)
            Later(new LaterAct { Kind = "news", Title = "Word around " + ShortTown(town), At = now, Text = Below.Morning(town, town + now), DueAt = morning });
        }
    }
}
