// The mind's lean on the street's chance meetings (roadmap 18, moods that carry: "a mood ... colours thoughts, words and
// reasons"; roadmap 30, encounters people do; night 2026-10-29, item 6e; with [Mind] MindLive and [Mind] MindMeetings).
// The reasons say what could pass between two people; their moods today, and how much they want company, lean which of
// those reasons the street plays: two sore people come to words sooner than to small talk, two glad ones the other way,
// and someone lonely stops for a word. A lean only, on a reason that exists: nobody quarrels who had no cause, nobody chats
// who had no tie or reason to. Pure; plain C#, no game.
using System;

namespace EnrageWestworld.Core
{
    public static partial class Reasons
    {
        /// The kinds of reason that are talk between people on good terms, and the kinds that are trouble.
        public static bool Social(string kind)
        {
            switch (kind)
            {
                case "smalltalk": case "gossip": case "x:sweet": case "flirt": case "propose": case "condole": case "tend": case "mend": case "x:goodnews":
                    return true;
                default:
                    return false;
            }
        }

        public static bool Trouble(string kind)
        {
            switch (kind)
            {
                case "argue": case "x:quarrel": case "brawl": case "rough": case "x:snap": case "drunk":
                    return true;
                default:
                    return false;
            }
        }

        /// How much the two's day leans this reason's strength (it ranks the pairs near the player): each sore one -8 for
        /// talk and +8 for trouble, each glad one +6 and -6; the lonelier of the two up to +8 for talk. `aMood`, `bMood`:
        /// Passing.DayMood (sore, glad or null); `company`: their need for company, 0..1 (NeedState), or -1 unknown.
        public static int MoodLean(string kind, string aMood, string bMood, float aCompany, float bCompany)
        {
            bool social = Social(kind), trouble = Trouble(kind);
            if (!social && !trouble)
                return 0;
            int sore = (aMood == "sore" ? 1 : 0) + (bMood == "sore" ? 1 : 0), glad = (aMood == "glad" ? 1 : 0) + (bMood == "glad" ? 1 : 0);
            if (trouble)
                return 8 * sore - 6 * glad;
            float lonely = Math.Max(0f, Math.Max(aCompany, bCompany));
            return -8 * sore + 6 * glad + (int)Math.Round(8f * Math.Min(1f, lonely));
        }
    }
}
