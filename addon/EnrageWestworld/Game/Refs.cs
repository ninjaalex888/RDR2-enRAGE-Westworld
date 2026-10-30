// enRAGE's people as the add-on holds them (2026-10-29, roadmap 34 phase A: standing alone). RecordRef stands for one
// of enRAGE's records (its NemesisRecord) and FolkRef for one of its townsfolk (its Acquaintance), each a thin handle
// on the raw object with the members the add-on reads, named as before (Id, Name, IsAlive, Hate...), so the call sites
// changed only their type names. The members read through Typed (Game/Typed.cs), the one file whose methods carry
// enRAGE's types, compiled only when called: with NemesisMod.dll absent nothing here or elsewhere names a type of
// enRAGE's, and the add-on loads (daytime/harness/loadtest proves it). The bridge hands out one ref per raw object
// (a weak table), so reference equality holds as it did.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.InGame
{
    internal sealed class RecordRef
    {
        public readonly object Raw;
        public RecordRef(object raw) { Raw = raw; }

        public string Id { get { return Typed.RecId(Raw); } }
        public string Name { get { return Typed.RecName(Raw); } }
        public string Title { get { return Typed.RecTitle(Raw); } }
        public string KinId { get { return Typed.RecKinId(Raw); } }
        public string KinRelation { get { return Typed.RecKinRelation(Raw); } }
        public string Gang { get { return Typed.RecGang(Raw); } }
        public uint ModelHash { get { return Typed.RecModelHash(Raw); } }
        public bool IsAlive { get { return Typed.RecIsAlive(Raw); } }
        public bool PresumedDead { get { return Typed.RecPresumedDead(Raw); } }
        public bool KilledByPlayer { get { return Typed.RecKilledByPlayer(Raw); } }
        public long PresumedDeadAt { get { return Typed.RecPresumedDeadAt(Raw); } }
        public int Hate { get { return Typed.RecHate(Raw); } set { Typed.RecSetHate(Raw, value); } }
        public void AddHate(int v) { Typed.RecAddHate(Raw, v); }   // enRAGE's own method (it clamps and touches)
        public int Fear { get { return Typed.RecFear(Raw); } }
        public Dictionary<string, int> Facets { get { return Typed.RecFacets(Raw); } }
    }

    internal sealed class FolkRef
    {
        public readonly object Raw;
        public FolkRef(object raw) { Raw = raw; }

        public string Id { get { return Typed.AcqId(Raw); } }
        public string Name { get { return Typed.AcqName(Raw); } }           // as enRAGE names them to the player (its Name)
        public string FullName { get { return Typed.AcqFullName(Raw); } }
        public void SetName(string name) { Typed.AcqSetName(Raw, name); }
        public string Town { get { return Typed.AcqTown(Raw); } }
        public string Temper { get { return Typed.AcqTemper(Raw); } }
        public uint Model { get { return Typed.AcqModel(Raw); } }
        public bool Met { get { return Typed.AcqMet(Raw); } }
        public bool Dead { get { return Typed.AcqDead(Raw); } }
        public int Like { get { return Typed.AcqLike(Raw); } }
        public int Grudge { get { return Typed.AcqGrudge(Raw); } }
        public int Fear { get { return Typed.AcqFear(Raw); } }
        public List<string> Witnessed { get { return Typed.AcqWitnessed(Raw); } }
        public List<string> FavourNotes { get { return Typed.AcqFavourNotes(Raw); } }
    }
}
