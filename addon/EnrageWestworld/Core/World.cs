// Saved records added in 0.4: the player's deeds and the rumours they start, papers he carries (diaries,
// letters, clippings), the dead who may wake again, relatives riding in, weddings, prisoner transfers, debts.
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace EnrageWestworld.Core
{
    [DataContract]
    public sealed class Deed
    {
        [DataMember(EmitDefaultValue = false)] public string Id;
        [DataMember(EmitDefaultValue = false)] public string Kind;      // kill, murder, lawkill, theft, robbery, horsetheft, assault, looting, hunt, shop, church, saloon...
        [DataMember(EmitDefaultValue = false)] public string Detail;    // what/who: "gunsmith", "a deputy", "Elihu Starling", "deer"
        [DataMember(EmitDefaultValue = false)] public string Town;
        [DataMember(EmitDefaultValue = false)] public string Zone;
        [DataMember(EmitDefaultValue = false)] public long At;
        [DataMember(EmitDefaultValue = false)] public float X, Y, Z;
        [DataMember(EmitDefaultValue = false)] public int Witnesses;
        [DataMember(EmitDefaultValue = false)] public int Weight;       // how much talk it makes, 0-100
        [DataMember(EmitDefaultValue = false)] public int Count = 1;    // folded together: "three pelts", "two men"
        [DataMember(EmitDefaultValue = false)] public string VictimId;
        [DataMember(EmitDefaultValue = false)] public bool Printed;     // went to the newspaper
        [DataMember(EmitDefaultValue = false)] public bool Spread;      // started a rumour
        [DataMember(EmitDefaultValue = false)] public long TalkAt;      // a violent deed: when talk of it starts (0: started, or never waited; Knowing.cs)
        [DataMember(EmitDefaultValue = false)] public bool Unseen;      // nobody lived to tell who did it: the talk doesn't know

        public string Where { get { return Town ?? Zone; } }

        /// The town can know it was him: its talk has started and someone lived to tell who did it (Knowing.cs).
        public bool Known { get { return !Unseen && TalkAt == 0; } }
    }

    [DataContract]
    public sealed class TownRumour
    {
        [DataMember(EmitDefaultValue = false)] public string Id;
        [DataMember(EmitDefaultValue = false)] public string DeedId;
        [DataMember(EmitDefaultValue = false)] public string Origin;    // town where it happened
        [DataMember(EmitDefaultValue = false)] public string Town;      // town it has reached
        [DataMember(EmitDefaultValue = false)] public string Text;      // this town's version, third person: "a stranger shot two men outside the saloon"
        [DataMember(EmitDefaultValue = false)] public int Fidelity;     // 100 = what happened; lower = bent out of shape
        [DataMember(EmitDefaultValue = false)] public int Hops;
        [DataMember(EmitDefaultValue = false)] public long At;
        [DataMember(EmitDefaultValue = false)] public long NextAt;
        [DataMember(EmitDefaultValue = false)] public bool Heard;       // the player has heard this version
    }

    [DataContract]
    public sealed class Paper
    {
        [DataMember(EmitDefaultValue = false)] public string Id;
        [DataMember(EmitDefaultValue = false)] public string Kind;      // diary, letter, telegram, clipping, note
        [DataMember(EmitDefaultValue = false)] public string Title;
        [DataMember(EmitDefaultValue = false)] public string From;
        [DataMember(EmitDefaultValue = false)] public string FromId;
        [DataMember(EmitDefaultValue = false)] public string Body;
        [DataMember(EmitDefaultValue = false)] public long At;
        [DataMember(EmitDefaultValue = false)] public string Town;      // letters: the post office holding it
        [DataMember(EmitDefaultValue = false)] public bool Delivered;
        [DataMember(EmitDefaultValue = false)] public bool Read;
    }

    [DataContract]
    public sealed class Fallen
    {
        [DataMember(EmitDefaultValue = false)] public string Id;
        [DataMember(EmitDefaultValue = false)] public uint Model;
        [DataMember(EmitDefaultValue = false)] public bool Woman;
        [DataMember(EmitDefaultValue = false)] public string Name;      // if enRAGE had named them
        [DataMember(EmitDefaultValue = false)] public string Label;     // "a ranch hand"
        [DataMember(EmitDefaultValue = false)] public string Town;
        [DataMember(EmitDefaultValue = false)] public string Zone;
        [DataMember(EmitDefaultValue = false)] public float X, Y, Z;
        [DataMember(EmitDefaultValue = false)] public string Weapon;    // "a revolver"
        [DataMember(EmitDefaultValue = false)] public bool Headshot;
        [DataMember(EmitDefaultValue = false)] public long At;
        [DataMember(EmitDefaultValue = false)] public bool Rolled;      // the return chance has been rolled
        [DataMember(EmitDefaultValue = false)] public long ReturnAt;    // 0 = stays dead
        [DataMember(EmitDefaultValue = false)] public int Returned;
        [DataMember(EmitDefaultValue = false)] public bool Done;
        [DataMember(EmitDefaultValue = false)] public int Seen;         // people who saw it happen
        [DataMember(EmitDefaultValue = false)] public bool Begged;      // shot while pleading for their life
        [DataMember(EmitDefaultValue = false)] public bool Mourned;     // their widow (or brother) has grieved on screen
    }

    [DataContract]
    public sealed class Reverie
    {
        [DataMember(EmitDefaultValue = false)] public string PersonId;
        [DataMember(EmitDefaultValue = false)] public string Name;
        [DataMember(EmitDefaultValue = false)] public bool Woman;
        [DataMember(EmitDefaultValue = false)] public long DiedAt;
        [DataMember(EmitDefaultValue = false)] public string Place;
        [DataMember(EmitDefaultValue = false)] public string How;       // "a revolver", "a knife"
        [DataMember(EmitDefaultValue = false)] public bool Headshot;
        [DataMember(EmitDefaultValue = false)] public bool ByPlayer;
        [DataMember(EmitDefaultValue = false)] public string Killer;    // "Arthur Morgan", "the O'Driscoll Boys"
        [DataMember(EmitDefaultValue = false)] public bool Rolled;
        [DataMember(EmitDefaultValue = false)] public long WakeAt;      // 0 = stays dead
        [DataMember(EmitDefaultValue = false)] public bool Woke;
        [DataMember(EmitDefaultValue = false)] public long WokeAt;
        [DataMember(EmitDefaultValue = false)] public bool Buried;
    }

    [DataContract]
    public sealed class Avenger
    {
        [DataMember(EmitDefaultValue = false)] public string Id;
        [DataMember(EmitDefaultValue = false)] public string Name;
        [DataMember(EmitDefaultValue = false)] public string Relation;  // relation to the dead: brother, son...
        [DataMember(EmitDefaultValue = false)] public bool Woman;
        [DataMember(EmitDefaultValue = false)] public string Town;      // where they ride in from
        [DataMember(EmitDefaultValue = false)] public string DeadId;
        [DataMember(EmitDefaultValue = false)] public string DeadName;
        [DataMember(EmitDefaultValue = false)] public bool DeadWoman;
        [DataMember(EmitDefaultValue = false)] public long DueAt;
        [DataMember(EmitDefaultValue = false)] public int Stage;        // 0 letter, 1 riding, 2 arrived, 3 done
        [DataMember(EmitDefaultValue = false)] public string RecordId;
    }

    [DataContract]
    public sealed class Wedding
    {
        [DataMember(EmitDefaultValue = false)] public string Id;
        [DataMember(EmitDefaultValue = false)] public string AId, AName, BId, BName;
        [DataMember(EmitDefaultValue = false)] public bool AWoman, BWoman;
        [DataMember(EmitDefaultValue = false)] public string Town;
        [DataMember(EmitDefaultValue = false)] public long At;          // the ceremony starts
        [DataMember(EmitDefaultValue = false)] public bool Announced;
        [DataMember(EmitDefaultValue = false)] public bool Done;
        [DataMember(EmitDefaultValue = false)] public bool Attended;
        [DataMember(EmitDefaultValue = false)] public bool Ruined;      // broken up (RuinWedding): they marry quietly later, and nobody toasts it
    }

    [DataContract]
    public sealed class Transfer
    {
        [DataMember(EmitDefaultValue = false)] public string Id;
        [DataMember(EmitDefaultValue = false)] public string PersonId;
        [DataMember(EmitDefaultValue = false)] public string Name;
        [DataMember(EmitDefaultValue = false)] public string From;
        [DataMember(EmitDefaultValue = false)] public string To;
        [DataMember(EmitDefaultValue = false)] public long At;
        [DataMember(EmitDefaultValue = false)] public string Outcome;   // null until it happens: arrived, escaped, freed
        [DataMember(EmitDefaultValue = false)] public bool Told;
    }

    [DataContract]
    public sealed class Debt
    {
        [DataMember(EmitDefaultValue = false)] public string Id;
        [DataMember(EmitDefaultValue = false)] public string FromId, FromName, ToId, ToName;
        [DataMember(EmitDefaultValue = false)] public int Amount;
        [DataMember(EmitDefaultValue = false)] public string Why;
        [DataMember(EmitDefaultValue = false)] public long Since;
        [DataMember(EmitDefaultValue = false)] public bool Settled;
        [DataMember(EmitDefaultValue = false)] public string How;       // paid, beaten, forgiven
    }

    public static class Ids
    {
        /// Where new ids come from: the game's is a GUID; the offline harnesses set a counter, so their goldens hold byte for
        /// byte (2026-10-29: the frame guard across people made a line's text decide the next choice, and lines seeded by a
        /// deed's or a rumour's id differed run to run). Null: the GUID.
        public static Func<string> Source;

        public static string New()
        {
            Func<string> s = Source;
            return s != null ? s() : Guid.NewGuid().ToString("N").Substring(0, 10);
        }
    }
}
