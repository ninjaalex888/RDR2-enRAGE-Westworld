// What the add-on remembers about one enRAGE person (or townsfolk, keyed "folk:..."), saved per player.
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace EnrageWestworld.Core
{
    [DataContract]
    public sealed class PersonState
    {
        [DataMember] public string Id;
        [DataMember] public string Name;
        [DataMember] public bool KnowsName;
        [DataMember] public long NameAt;
        [DataMember] public int Talks;
        [DataMember] public long LastTalkAt;
        [DataMember] public List<string> Told = new List<string>();
        [DataMember] public List<string> Thought = new List<string>();
        [DataMember] public List<string> Spread = new List<string>();
        [DataMember] public List<Hearsay> Heard = new List<Hearsay>();
        [DataMember] public List<DiaryEntry> Diary = new List<DiaryEntry>();
        [DataMember] public int Sightings;
        [DataMember] public string FirstSeenTown;
        [DataMember] public long FirstSeenAt;
        [DataMember] public long LastSeenAt;
        [DataMember] public long LastRemarkAt;
        [DataMember] public List<Relative> Family;
        [DataMember] public int Affection;
        [DataMember] public int Heart;
        [DataMember] public long HeartAt;
        [DataMember] public int Flirts;
        [DataMember] public long FlirtedAt;
        [DataMember] public long ContactAt;
        [DataMember] public string JealousOf;
        [DataMember] public bool KnowsWife;

        // 0.4
        [DataMember] public int Awareness;                         // how much they suspect their world isn't real (0-100)
        [DataMember] public List<string> Musings = new List<string>();   // reality lines already said/thought
        [DataMember] public List<string> KnownDeeds = new List<string>(); // player deeds they've heard of
        [DataMember] public List<string> SawDeeds = new List<string>();   // ...and the ones they saw with their own eyes
        [DataMember] public int DiaryDay;                          // last in-game day a daily entry was written
        [DataMember] public bool DiaryFound;                       // the player took their diary
        [DataMember] public int Glitches;
        [DataMember] public long LastMusingAt;
        [DataMember] public int Deaths;                            // times died and woke again
        [DataMember] public int SparedSeen;                        // enRAGE's count of the times the player let them go, last we looked

        public bool Has(string topic)
        {
            return Told.Contains(topic);
        }

        public void Mark(string topic)
        {
            if (!Told.Contains(topic))
                Told.Add(topic);
        }

        internal void Fix()
        {
            if (Told == null) Told = new List<string>();
            if (Thought == null) Thought = new List<string>();
            if (Spread == null) Spread = new List<string>();
            if (Heard == null) Heard = new List<Hearsay>();
            if (Diary == null) Diary = new List<DiaryEntry>();
            if (Musings == null) Musings = new List<string>();
            if (KnownDeeds == null) KnownDeeds = new List<string>();
            if (SawDeeds == null) SawDeeds = new List<string>();
            if (Family == null)
                return;
            foreach (Relative r in Family)
                if (r.Heard == null)
                    r.Heard = new List<Hearsay>();
        }
    }
}
