// The towns and regions of the map, of our own (2026-10-29, roadmap 34 phase A, step 2: standing alone). Until now the
// add-on asked enRAGE where the player was (its TownAt, GetZoneName and TownCore); with enRAGE absent these answer, and
// with it present enRAGE's own answers still win (Bridge falls back here only when its method wasn't found). The zone
// hashes are the game's (_GET_MAP_ZONE_AT_COORDS, type 1 for towns and districts, 10 for the regions); the nine towns
// with a doctor's and a saloon's door, the town core radii and Saint Denis's centre are enRAGE's tables, adapted with
// credit (its author permits reuse). Nothing here calls enRAGE.
using System.Collections.Generic;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    internal static class Places
    {
        /// The game's zone hashes and the names the add-on uses for them (towns and districts first, then the regions).
        public static readonly Dictionary<uint, string> ZoneNames = new Dictionary<uint, string>
        {
            { 1654810713u, "Aguasdulces" }, { 201158410u, "Aguasdulces" }, { 3087833527u, "Aguasdulces" },
            { 7359335u, "Annesburg" }, { 3550472498u, "Armadillo" }, { 2586580314u, "Beecher's Hope" }, { 1053078005u, "Blackwater" },
            { 1778899666u, "Braithwaite Manor" }, { 2347551651u, "Butcher Creek" }, { 1862420670u, "Caliga Hall" },
            { 2443661614u, "Cornwall Kerosene & Tar" }, { 3821916002u, "Emerald Ranch" }, { 406627834u, "Lagras" }, { 1299204683u, "Manicato" },
            { 1463094051u, "Manzanita Post" }, { 2046780049u, "Rhodes" }, { 2147354003u, "Sisika Penitentiary" }, { 3529426767u, "Saint Denis" },
            { 427683330u, "Strawberry" }, { 2770008149u, "Tumbleweed" }, { 459833523u, "Valentine" }, { 2126321341u, "Van Horn" },
            { 3422345262u, "Wallace Station" }, { 1663398575u, "Wapiti" },
            { 2025841068u, "Bayou Nwa" }, { 822658194u, "Big Valley" }, { 1308232528u, "Bluewater Marsh" }, { 4186119282u, "Cholla Springs" },
            { 1835499550u, "Cumberland Forest" }, { 426773653u, "Diez Coronas" }, { 2228727054u, "Gaptooth Ridge" }, { 476637847u, "Great Plains" },
            { 4174810561u, "the Grizzlies" }, { 1645618177u, "the Grizzlies" }, { 3782438103u, "Guarma" }, { 131399519u, "the Heartlands" },
            { 892930832u, "Hennigan's Stead" }, { 2975011176u, "Perdido" }, { 1453836102u, "Punta Orgullo" }, { 2148975167u, "Rio Bravo" },
            { 178647645u, "Roanoke Ridge" }, { 3430691604u, "Scarlett Meadows" }, { 1684533001u, "Tall Trees" }
        };

        /// The towns proper (the ones with a doctor's door, a saloon's door, or both; the others above are districts).
        public static readonly Dictionary<string, Vector3?[]> TownSpots = new Dictionary<string, Vector3?[]>
        {
            { "Valentine", new Vector3?[] { new Vector3(-287.55f, 812.09f, 119.25f), new Vector3(-313.51f, 805.81f, 118.98f) } },
            { "Saint Denis", new Vector3?[] { new Vector3(2732.4f, -1233.83f, 50.7f), new Vector3(2792.37f, -1168.41f, 47.93f) } },
            { "Rhodes", new Vector3?[] { new Vector3(1372.46f, -1305.12f, 77.75f), new Vector3(1340.17f, -1374.82f, 80.48f) } },
            { "Blackwater", new Vector3?[] { new Vector3(-785.69f, -1306.2f, 43.81f), new Vector3(-817.59f, -1319.09f, 43.68f) } },
            { "Armadillo", new Vector3?[] { new Vector3(-3738.99f, -2636.93f, -12.4f), new Vector3(-3699.81f, -2594.41f, -13.32f) } },
            { "Tumbleweed", new Vector3?[] { new Vector3(-5528.01f, -2952.85f, -0.5f), new Vector3(-5518.48f, -2906.51f, -1.75f) } },
            { "Strawberry", new Vector3?[] { new Vector3(-1808.89f, -434f, 158.72f), null } },
            { "Annesburg", new Vector3?[] { new Vector3(2922.97f, 1356.31f, 44.71f), null } },
            { "Van Horn", new Vector3?[] { null, new Vector3(2947.84f, 528.06f, 45.34f) } }
        };

        /// How far from the centre a town's core reaches (110 m where a town isn't listed).
        public static readonly Dictionary<string, float> TownCoreRadius = new Dictionary<string, float>
        {
            { "Valentine", 130f }, { "Saint Denis", 290f }, { "Rhodes", 120f }, { "Blackwater", 150f }, { "Armadillo", 110f },
            { "Tumbleweed", 90f }, { "Strawberry", 100f }, { "Annesburg", 120f }, { "Van Horn", 100f }
        };

        /// A centre set by hand where the midpoint of the two doors would miss it.
        public static readonly Dictionary<string, Vector3> TownCentre = new Dictionary<string, Vector3>
        {
            { "Saint Denis", new Vector3(2660f, -1240f, 47f) }
        };

        /// The town at this spot (one of the nine), else null.
        public static string TownAt(Vector3 pos)
        {
            uint key = ZONE._GET_MAP_ZONE_AT_COORDS(pos.X, pos.Y, pos.Z, 1);
            string name;
            return ZoneNames.TryGetValue(key, out name) && TownSpots.ContainsKey(name) ? name : null;
        }

        /// The town, district or region at this spot, "the wilds" where the map names none.
        public static string ZoneName(Vector3 pos)
        {
            string name;
            if (ZoneNames.TryGetValue(ZONE._GET_MAP_ZONE_AT_COORDS(pos.X, pos.Y, pos.Z, 1), out name))
                return name;
            if (ZoneNames.TryGetValue(ZONE._GET_MAP_ZONE_AT_COORDS(pos.X, pos.Y, pos.Z, 10), out name))
                return name;
            return "the wilds";
        }

        /// A town's centre and the radius of its core; false for a name that isn't one of the nine.
        public static bool TownCore(string town, out Vector3 centre, out float radius)
        {
            centre = Vector3.Zero;
            radius = 0f;
            Vector3?[] doors;
            if (town == null || !TownSpots.TryGetValue(town, out doors))
                return false;
            Vector3 set;
            if (TownCentre.TryGetValue(town, out set))
                centre = set;
            else if (doors[0].HasValue && doors[1].HasValue)
                centre = (doors[0].Value + doors[1].Value) * 0.5f;
            else
                centre = doors[0] ?? doors[1].Value;
            float r;
            radius = TownCoreRadius.TryGetValue(town, out r) ? r : 110f;
            return true;
        }
    }
}
