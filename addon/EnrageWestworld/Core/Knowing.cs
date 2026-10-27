// Who can know what, and when (2026-10-26, a player's report through Lex). Before this, everything RealNPCs did with a
// new memory of the player happened in the same second: the diary entry was written (a man at an O'Driscoll camp had
// one swearing revenge for a kill 30 seconds old, found on him when he was killed too), word set off to his friends,
// and anyone at all with a memory of the player could fill the corner with it ("a stranger killed my friend", from
// people the player had never met). Now:
//   - a diary entry is written that evening, at least three game hours on, and only if the writer is still alive;
//   - word of mouth sets off only once the one who knows has lived another game hour (a witness killed at once tells
//     nobody);
//   - the corner's notices about people are only for people the player has met (Met), one per thing that happened a day;
//   - a violent deed nobody lived to tell of starts no talk until the body would be found, and the talk doesn't know
//     who did it (Deed.Unseen; Rumours.Version, Deeds.Paper).
// A game hour is about two real minutes, so these are hours in the world, not long waits at the screen.
using System.Runtime.Serialization;
using System.Text.RegularExpressions;

namespace EnrageWestworld.Core
{
    /// Something someone will do with a memory later: write it in their diary, or tell it.
    [DataContract]
    public sealed class LaterAct
    {
        [DataMember(EmitDefaultValue = false)] public string Kind;   // "diary", "word", or "news" (a notice for the corner, Title and Text)
        [DataMember(EmitDefaultValue = false)] public string Title;
        [DataMember(EmitDefaultValue = false)] public string PersonId;
        [DataMember(EmitDefaultValue = false)] public string Text, Emotion, Cause;
        [DataMember(EmitDefaultValue = false)] public int Intensity;
        [DataMember(EmitDefaultValue = false)] public long At;       // when the memory was made (game minutes)
        [DataMember(EmitDefaultValue = false)] public long DueAt;

        public MemoryItem Memory()
        {
            return new MemoryItem { Text = Text, Emotion = Emotion, Cause = Cause, Intensity = Intensity, At = At };
        }

        public static LaterAct Of(string kind, string personId, MemoryItem m, long dueAt)
        {
            return new LaterAct { Kind = kind, PersonId = personId, Text = m.Text, Emotion = m.Emotion, Cause = m.Cause, Intensity = m.Intensity, At = m.At, DueAt = dueAt };
        }
    }

    public static class Knowing
    {
        public const int Max = 400;                   // waiting acts kept (the oldest go first)
        public const int WordAfterMinutes = 60;       // a game hour alive before they tell anyone
        public const int DiaryAfterMinutes = 180;     // and three before the day's entry
        public const int TalkAfterMinutes = 60;       // a seen violent deed: talk starts once a witness has lived to tell it
        public const int FoundAfterMinutes = 360;     // an unseen one: the body is found 6 to 18 game hours on
        public const int FoundSpreadMinutes = 720;

        /// When the day's entry gets written: that evening (from 20:00), and never sooner than three hours after.
        public static long DiaryDue(long at, string seed)
        {
            long t = at + DiaryAfterMinutes;
            long day = t / 1440, minute = t % 1440;
            int jitter = Voice.Hash(seed ?? "") % 180;
            return minute < 20 * 60 ? day * 1440 + 20 * 60 + jitter : t + jitter % 60;
        }

        /// When a seen violent deed starts talk, and when an unseen one is found.
        public static long TalkDue(long at, bool seen, string seed)
        {
            return at + (seen ? TalkAfterMinutes : FoundAfterMinutes + Voice.Hash((seed ?? "") + "found") % FoundSpreadMinutes);
        }

        /// The player has met them: talked, seen them a few times, or a romance. From his side, not theirs: before,
        /// anyone with a memory of him counted, so strangers' grief filled the corner.
        public static bool Met(PersonState st)
        {
            return st != null && (st.Talks > 0 || st.Sightings >= 2 || st.Heart != 0);
        }

        private static readonly Regex Pronouns = new Regex(@"\b(his|her|him|he|she)\b");
        private static readonly Regex Spaces = new Regex(@"[\s,.;:!]+");

        /// What a memory or a piece of talk is about, so five friends grieving one man make one notice.
        public static string Topic(string text)
        {
            string t = (text ?? "").ToLowerInvariant();
            t = Pronouns.Replace(t, "");
            return Spaces.Replace(t, " ").Trim();
        }
    }
}
