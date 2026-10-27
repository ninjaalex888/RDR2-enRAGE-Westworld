// What the add-on remembers about one enRAGE person (or townsfolk, keyed "folk:..."), saved per player.
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace EnrageWestworld.Core
{
    [DataContract]
    public sealed class PersonState
    {
        [DataMember(EmitDefaultValue = false)] public string Id;
        [DataMember(EmitDefaultValue = false)] public string Name;
        [DataMember(EmitDefaultValue = false)] public bool KnowsName;
        [DataMember(EmitDefaultValue = false)] public long NameAt;
        [DataMember(EmitDefaultValue = false)] public int Talks;
        [DataMember(EmitDefaultValue = false)] public long LastTalkAt;
        [DataMember(EmitDefaultValue = false)] public List<string> Told = new List<string>();
        [DataMember(EmitDefaultValue = false)] public List<string> Thought = new List<string>();
        [DataMember(EmitDefaultValue = false)] public List<string> Spread = new List<string>();
        [DataMember(EmitDefaultValue = false)] public List<Hearsay> Heard = new List<Hearsay>();
        [DataMember(EmitDefaultValue = false)] public List<DiaryEntry> Diary = new List<DiaryEntry>();
        [DataMember(EmitDefaultValue = false)] public int Sightings;
        [DataMember(EmitDefaultValue = false)] public string FirstSeenTown;
        [DataMember(EmitDefaultValue = false)] public long FirstSeenAt;
        [DataMember(EmitDefaultValue = false)] public long LastSeenAt;
        [DataMember(EmitDefaultValue = false)] public long LastRemarkAt;
        [DataMember(EmitDefaultValue = false)] public List<Relative> Family;
        [DataMember(EmitDefaultValue = false)] public int Affection;
        [DataMember(EmitDefaultValue = false)] public int Heart;
        [DataMember(EmitDefaultValue = false)] public long HeartAt;
        [DataMember(EmitDefaultValue = false)] public int Flirts;
        [DataMember(EmitDefaultValue = false)] public long FlirtedAt;
        [DataMember(EmitDefaultValue = false)] public long ContactAt;
        [DataMember(EmitDefaultValue = false)] public string JealousOf;
        [DataMember(EmitDefaultValue = false)] public bool KnowsWife;

        // 0.4
        [DataMember(EmitDefaultValue = false)] public int Awareness;                         // how much they suspect their world isn't real (0-100)
        [DataMember(EmitDefaultValue = false)] public List<string> Musings = new List<string>();   // reality lines already said/thought
        [DataMember(EmitDefaultValue = false)] public List<string> KnownDeeds = new List<string>(); // player deeds they've heard of
        [DataMember(EmitDefaultValue = false)] public List<string> SawDeeds = new List<string>();   // ...and the ones they saw with their own eyes
        [DataMember(EmitDefaultValue = false)] public int DiaryDay;                          // last in-game day a daily entry was written
        [DataMember(EmitDefaultValue = false)] public bool DiaryFound;                       // the player took their diary
        [DataMember(EmitDefaultValue = false)] public int Glitches;
        [DataMember(EmitDefaultValue = false)] public long LastMusingAt;
        [DataMember(EmitDefaultValue = false)] public int Deaths;                            // times died and woke again
        [DataMember(EmitDefaultValue = false)] public int SparedSeen;                        // enRAGE's count of the times the player let them go, last we looked

        // 0.6: the Awakening
        [DataMember(EmitDefaultValue = false)] public int WakeStage;                         // 0 asleep, 1 stirring, 2 questioning, 3 unravelling, 4 awake
        [DataMember(EmitDefaultValue = false)] public long WakeStageAt;                      // game minute the current stage began
        [DataMember(EmitDefaultValue = false)] public bool WakeRolled;                       // the chance of starting already stirring has been rolled for this person
        [DataMember(EmitDefaultValue = false)] public List<MindMemory> Mind = new List<MindMemory>(); // what they lived through and may think about again (Minds.cs)

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
            if (Mind == null) Mind = new List<MindMemory>();
            Mind.RemoveAll(m => m == null);
            if (Family == null)
                return;
            foreach (Relative r in Family)
                if (r.Heard == null)
                    r.Heard = new List<Hearsay>();
        }
    }
}
