// Where a town's places are (the NPC-life run, step 2; Lex, 2026-10-30: people who choose the saloon, the store or church
// walk into the real building). Learned, not typed in: wherever a town's counter keeper is seen at work (the barkeep at
// the bar, the store keeper at the counter: Posts) is where that place is, kept in the save so it's known next time.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace EnrageWestworld.Core
{
    [DataContract]
    public sealed class PlaceSpot
    {
        [DataMember(EmitDefaultValue = false)] public string Town;
        [DataMember(EmitDefaultValue = false)] public string Kind;   // the post's kind: barkeep, store, gunsmith, doctor...
        [DataMember(EmitDefaultValue = false)] public string Model;  // the keeper's model: a town with two saloons has two barkeeps
        [DataMember(EmitDefaultValue = false)] public float X, Y, Z;
        [DataMember(EmitDefaultValue = false)] public long SeenAt;
    }

    public static class PlaceSpots
    {
        /// The post kind that stands for a place someone chooses to go (Choosing): the saloon is the bar, the store its counter.
        public static string KindFor(string choice)
        {
            switch (choice)
            {
                case "saloon": case "drink": case "cards": return "barkeep";
                case "store": return "store";
                default: return null;
            }
        }

        /// The nearest place of this kind in the town to (x, y): Valentine has two saloons.
        public static PlaceSpot Find(List<PlaceSpot> spots, string town, string kind, float x, float y)
        {
            return spots == null || town == null || kind == null ? null
                 : spots.Where(s => s.Town == town && s.Kind == kind).OrderBy(s => (s.X - x) * (s.X - x) + (s.Y - y) * (s.Y - y)).FirstOrDefault();
        }

        /// Notes where a keeper stands at work; true when it's new or has moved more than 3 m (a spot of the save).
        public static bool Note(List<PlaceSpot> spots, string town, string kind, string model, float x, float y, float z, long now)
        {
            if (spots == null || town == null || kind == null)
                return false;
            PlaceSpot s = spots.FirstOrDefault(p => p.Town == town && p.Kind == kind && p.Model == model);
            if (s == null)
            {
                spots.Add(new PlaceSpot { Town = town, Kind = kind, Model = model, X = x, Y = y, Z = z, SeenAt = now });
                return true;
            }
            float dx = s.X - x, dy = s.Y - y;
            s.SeenAt = now;
            if (dx * dx + dy * dy < 9f)
                return false;
            s.X = x;
            s.Y = y;
            s.Z = z;
            return true;
        }
    }
}
