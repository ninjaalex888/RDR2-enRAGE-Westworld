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
        [DataMember] public string Id;
        [DataMember] public string Kind;      // kill, murder, lawkill, theft, robbery, horsetheft, assault, looting, hunt, shop, church, saloon...
        [DataMember] public string Detail;    // what/who: "gunsmith", "a deputy", "Elihu Starling", "deer"
        [DataMember] public string Town;
        [DataMember] public string Zone;
        [DataMember] public long At;
        [DataMember] public float X, Y, Z;
        [DataMember] public int Witnesses;
        [DataMember] public int Weight;       // how much talk it makes, 0-100
        [DataMember] public int Count = 1;    // folded together: "three pelts", "two men"
        [DataMember] public string VictimId;
        [DataMember] public bool Printed;     // went to the newspaper
        [DataMember] public bool Spread;      // started a rumour

        public string Where { get { return Town ?? Zone; } }
    }

    [DataContract]
    public sealed class TownRumour
    {
        [DataMember] public string Id;
        [DataMember] public string DeedId;
        [DataMember] public string Origin;    // town where it happened
        [DataMember] public string Town;      // town it has reached
        [DataMember] public string Text;      // this town's version, third person: "a stranger shot two men outside the saloon"
        [DataMember] public int Fidelity;     // 100 = what happened; lower = bent out of shape
        [DataMember] public int Hops;
        [DataMember] public long At;
        [DataMember] public long NextAt;
        [DataMember] public bool Heard;       // the player has heard this version
    }

    [DataContract]
    public sealed class Paper
    {
        [DataMember] public string Id;
        [DataMember] public string Kind;      // diary, letter, telegram, clipping, note
        [DataMember] public string Title;
        [DataMember] public string From;
        [DataMember] public string FromId;
        [DataMember] public string Body;
        [DataMember] public long At;
        [DataMember] public string Town;      // letters: the post office holding it
        [DataMember] public bool Delivered;
        [DataMember] public bool Read;
    }

    [DataContract]
    public sealed class Fallen
    {
        [DataMember] public string Id;
        [DataMember] public uint Model;
        [DataMember] public bool Woman;
        [DataMember] public string Name;      // if enRAGE had named them
        [DataMember] public string Label;     // "a ranch hand"
        [DataMember] public string Town;
        [DataMember] public string Zone;
        [DataMember] public float X, Y, Z;
        [DataMember] public string Weapon;    // "a revolver"
        [DataMember] public bool Headshot;
        [DataMember] public long At;
        [DataMember] public bool Rolled;      // the return chance has been rolled
        [DataMember] public long ReturnAt;    // 0 = stays dead
        [DataMember] public int Returned;
        [DataMember] public bool Done;
        [DataMember] public int Seen;         // people who saw it happen
        [DataMember] public bool Begged;      // shot while pleading for their life
        [DataMember] public bool Mourned;     // their widow (or brother) has grieved on screen
    }

    [DataContract]
    public sealed class Reverie
    {
        [DataMember] public string PersonId;
        [DataMember] public string Name;
        [DataMember] public bool Woman;
        [DataMember] public long DiedAt;
        [DataMember] public string Place;
        [DataMember] public string How;       // "a revolver", "a knife"
        [DataMember] public bool Headshot;
        [DataMember] public bool ByPlayer;
        [DataMember] public string Killer;    // "Arthur Morgan", "the O'Driscoll Boys"
        [DataMember] public bool Rolled;
        [DataMember] public long WakeAt;      // 0 = stays dead
        [DataMember] public bool Woke;
        [DataMember] public long WokeAt;
        [DataMember] public bool Buried;
    }

    [DataContract]
    public sealed class Avenger
    {
        [DataMember] public string Id;
        [DataMember] public string Name;
        [DataMember] public string Relation;  // relation to the dead: brother, son...
        [DataMember] public bool Woman;
        [DataMember] public string Town;      // where they ride in from
        [DataMember] public string DeadId;
        [DataMember] public string DeadName;
        [DataMember] public bool DeadWoman;
        [DataMember] public long DueAt;
        [DataMember] public int Stage;        // 0 letter, 1 riding, 2 arrived, 3 done
        [DataMember] public string RecordId;
    }

    [DataContract]
    public sealed class Wedding
    {
        [DataMember] public string Id;
        [DataMember] public string AId, AName, BId, BName;
        [DataMember] public bool AWoman, BWoman;
        [DataMember] public string Town;
        [DataMember] public long At;          // the ceremony starts
        [DataMember] public bool Announced;
        [DataMember] public bool Done;
        [DataMember] public bool Attended;
    }

    [DataContract]
    public sealed class Transfer
    {
        [DataMember] public string Id;
        [DataMember] public string PersonId;
        [DataMember] public string Name;
        [DataMember] public string From;
        [DataMember] public string To;
        [DataMember] public long At;
        [DataMember] public string Outcome;   // null until it happens: arrived, escaped, freed
        [DataMember] public bool Told;
    }

    [DataContract]
    public sealed class Debt
    {
        [DataMember] public string Id;
        [DataMember] public string FromId, FromName, ToId, ToName;
        [DataMember] public int Amount;
        [DataMember] public string Why;
        [DataMember] public long Since;
        [DataMember] public bool Settled;
        [DataMember] public string How;       // paid, beaten, forgiven
    }

    public static class Ids
    {
        public static string New()
        {
            return Guid.NewGuid().ToString("N").Substring(0, 10);
        }
    }
}
