// Our own record of who has died: step 1 of the mind design (the people ledger's first facts). Who, when, where, how and
// by whose hand, for RealNPCs' residents, enRAGE's townsfolk and its records alike. enRAGE keeps its own record; this one
// is what RealNPCs believes, so a death enRAGE never saw (the area unloaded a second after) still counts here.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace EnrageWestworld.Core
{
    [DataContract]
    public sealed class DeathEntry
    {
        [DataMember] public string Id;      // res:..., folk:..., or an enRAGE record's id
        [DataMember] public string Name;
        [DataMember] public string Kind;    // resident, folk, record
        [DataMember] public long At;        // game minutes
        [DataMember] public string Town;
        [DataMember] public float X, Y, Z;
        [DataMember] public string How;     // "a revolver", "running at a wall"... or null
        [DataMember] public string By;      // player, self, another, unknown
        [DataMember] public string ById;    // the one who did it, when we know them
        [DataMember] public bool Seen;      // in the player's view when it happened
        [DataMember] public bool SharedWithEnrage;   // we marked enRAGE's townsperson dead: it hadn't seen it
    }

    public static class DeathLedger
    {
        public const int Max = 1500;
        public const long SameDeathMinutes = 120;   // two notes of one person this close together are one death

        // the weapon groups enRAGE takes for knocked out, not dead (its LooksKnockedOut): fists, and the other it names
        public const uint UnarmedGroup = 2685387236u, OtherKnockoutGroup = 308416707u;

        /// The game reports someone who is only senseless as dead. enRAGE's rule, followed so both mods agree: whoever is
        /// incapacitated, laid low by a gun butt (enRAGE's own test), or downed by fists (or the other weapon group it
        /// names) is senseless and still alive.
        public static bool KnockedOut(bool incapacitated, bool gunButt, uint weaponGroup)
        {
            return incapacitated || gunButt || weaponGroup == UnarmedGroup || weaponGroup == OtherKnockoutGroup;
        }

        /// The latest death noted for this person, or null.
        public static DeathEntry Of(List<DeathEntry> list, string id)
        {
            if (list == null || id == null)
                return null;
            for (int i = list.Count - 1; i >= 0; i--)
                if (list[i] != null && list[i].Id == id)
                    return list[i];
            return null;
        }

        /// Adds a death, unless it's one already noted (the same person within SameDeathMinutes): then what the first
        /// note didn't know is filled in from this one. True if it was added.
        public static bool Note(List<DeathEntry> list, DeathEntry e)
        {
            if (list == null || e == null || string.IsNullOrEmpty(e.Id))
                return false;
            DeathEntry last = Of(list, e.Id);
            if (last != null && Math.Abs(e.At - last.At) <= SameDeathMinutes)
            {
                if (last.How == null)
                    last.How = e.How;
                if (last.By == null || last.By == "unknown")
                {
                    last.By = e.By;
                    last.ById = e.ById;
                }
                if (last.Town == null)
                    last.Town = e.Town;
                last.Seen |= e.Seen;
                return false;
            }
            list.Add(e);
            if (list.Count > Max)
                list.RemoveRange(0, list.Count - Max);
            return true;
        }

        /// Whose hand it was, from the game's source of death: the player's, their own, someone else's, or not known.
        public static string ByOf(int source, int player, int self)
        {
            if (source == 0)
                return "unknown";
            if (source == player)
                return "player";
            return source == self ? "self" : "another";
        }

        /// A death in words, for the log and F7.
        public static string Describe(DeathEntry e)
        {
            if (e == null)
                return "none";
            string by = e.By == "player" ? "by your hand" : e.By == "self" ? "by their own hand" : e.By == "another" ? "by someone else's hand" : "by no hand we know";
            return (e.Name ?? e.Id) + " (" + (e.Kind ?? "someone") + ", " + (e.How ?? "how unknown") + ", " + by + (e.Town != null ? ", " + e.Town : "") +
                   (e.Seen ? ", in view" : "") + (e.SharedWithEnrage ? ", told to enRAGE" : "") + ")";
        }

        /// How many deaths are kept, by kind, for F7.
        public static string Counts(List<DeathEntry> list)
        {
            if (list == null || list.Count < 1)
                return "none";
            return list.Count + " (" + string.Join(", ", list.GroupBy(d => d.Kind ?? "someone").OrderByDescending(g => g.Count()).Select(g => g.Key + " " + g.Count())) + ")";
        }
    }
}
