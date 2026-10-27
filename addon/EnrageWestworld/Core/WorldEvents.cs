// What happened, and who saw it (overnight 2026-10-27, Tier 1.5): one record for every street scene, encounter, deed
// of the player's and death RealNPCs knows of: its kind, who did what to whom, where and when (the game minute), how
// it came out, and the witnesses with how close they were and whether they saw it or only heard it. A witness is
// someone with a clear line of sight within 30 m, or within earshot of something loud (60 m for a shot, 25 m for a
// shout or a fight); nobody counts who couldn't have seen or heard it. Ties, deeds and the "just saw" memory keep
// working as they did; this is the plain record the Minds work turns into memories, and what F7 shows. Saved, capped
// at 400 (the oldest go), rolled back with an older save.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace EnrageWestworld.Core
{
    [DataContract]
    public sealed class Happening
    {
        [DataMember(Name = "k")] public string Kind;          // scene:<kind>, deed:<kind>, death, encounter:<kind>
        [DataMember(Name = "d")] public string DoerId;
        [DataMember(Name = "dn")] public string DoerName;     // a name, or how they looked ("a farmhand", "you")
        [DataMember(Name = "t")] public string TargetId;
        [DataMember(Name = "tn")] public string TargetName;
        [DataMember(Name = "w")] public string Town;
        [DataMember(Name = "p")] public string Place;         // saloon, church... (null: the street or the wilds)
        [DataMember(Name = "x")] public float X;
        [DataMember(Name = "y")] public float Y;
        [DataMember(Name = "z")] public float Z;
        [DataMember(Name = "a")] public long At;              // game minute (Store.LastGameMin)
        [DataMember(Name = "o")] public string Outcome;       // how it came out, in a word or two (null while unknown)
        [DataMember(Name = "s")] public List<EventWitness> Witnesses = new List<EventWitness>();
    }

    [DataContract]
    public sealed class EventWitness
    {
        [DataMember(Name = "i")] public string Id;            // a lasting id, or null for someone who can't be met again
        [DataMember(Name = "n")] public string Name;          // their name, or how they looked
        [DataMember(Name = "m")] public float Metres;
        [DataMember(Name = "h")] public string How;           // saw, heard
    }

    public static class WorldEvents
    {
        public const int Max = 400;
        public const float SightMetres = 30f, ShotMetres = 60f, ShoutMetres = 25f;

        /// Whether someone this far off saw or heard it, and how: "saw", "heard" or null. `loud`: shot, shout or quiet.
        public static string How(float metres, bool lineOfSight, string loud)
        {
            if (metres < 0)
                return null;
            if (lineOfSight && metres <= SightMetres)
                return "saw";
            if (loud == "shot" && metres <= ShotMetres)
                return "heard";
            if (loud == "shout" && metres <= ShoutMetres)
                return "heard";
            return null;
        }

        /// How loud a kind of event is, for earshot: a shot, a shout (a fight, a quarrel, a cry), or quiet.
        public static string Loudness(string kind)
        {
            if (kind == null)
                return "quiet";
            string k = kind.Contains(":") ? kind.Substring(kind.IndexOf(':') + 1) : kind;
            switch (k)
            {
                case "murder": case "kill": case "lawkill": case "namedkill": case "bankrobbery": case "stagecoach": case "trainrobbery": case "execution": case "gun": case "standoff":
                    return "shot";
                case "brawl": case "rough": case "argue": case "drunk": case "pickpocket": case "snap": case "crying": case "assault": case "robbery": case "death":
                    return "shout";
                default:
                    return "quiet";
            }
        }

        /// Keeps an event: the newest last, the oldest dropped past the cap. The event kept.
        public static Happening Note(List<Happening> all, Happening e)
        {
            if (all == null || e == null)
                return e;
            all.Add(e);
            if (all.Count > Max)
                all.RemoveRange(0, all.Count - Max);
            return e;
        }

        /// One line for the log and F7: "scene:brawl Floyd -> Silas in Valentine (saloon), 3 saw, 2 heard: shove".
        public static string Describe(Happening e)
        {
            if (e == null)
                return "nothing";
            int saw = e.Witnesses.Count(w => w.How == "saw"), heard = e.Witnesses.Count(w => w.How == "heard");
            return e.Kind + " " + (e.DoerName ?? "someone") + (e.TargetName != null ? " -> " + e.TargetName : string.Empty) +
                   (e.Town != null ? " in " + e.Town : string.Empty) + (e.Place != null ? " (" + e.Place + ")" : string.Empty) +
                   ", " + saw + " saw, " + heard + " heard" + (e.Outcome != null ? ": " + e.Outcome : string.Empty);
        }
    }
}
