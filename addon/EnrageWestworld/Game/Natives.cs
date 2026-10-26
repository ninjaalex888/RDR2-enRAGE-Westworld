// Small wrappers around natives that need care: Vector3 out-parameters are 24 bytes in RDR2 (each float on
// an 8-byte stride), so they are read from a ulong buffer, never through the API's 16-byte Vector3.
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using NemesisSystem;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    internal static class Natives
    {
        private static readonly Dictionary<string, uint> Hashes = new Dictionary<string, uint>();
        private static readonly Regex Childlike = new Regex("kid|child|teen|(?<!cow)boy|(?<!show|saloon|cow|bar)girl");
        private static readonly string[] LawWords = { "law", "police", "sheriff", "sherriff", "deputy", "marshal", "pinkerton", "ranger", "army", "soldier" };
        private static uint _copGroup;

        /// Jenkins one-at-a-time, lower case: the game's GET_HASH_KEY.
        public static uint Hash(string s)
        {
            uint h;
            if (Hashes.TryGetValue(s, out h))
                return h;
            unchecked
            {
                h = 0;
                foreach (char ch in s.ToLowerInvariant())
                {
                    h += ch;
                    h += h << 10;
                    h ^= h >> 6;
                }
                h += h << 3;
                h ^= h >> 11;
                h += h << 15;
            }
            Hashes[s] = h;
            return h;
        }

        private static int _safeProbes;
        public static int GroundFallbacks;

        /// The game's safe spot for a ped near `near`. In 0.5.4 every call in town failed, so the answer is read in
        /// both layouts (padded 24-byte and packed 12-byte) and the first few raw results are logged.
        public static unsafe bool SafeCoord(Vector3 near, out Vector3 found)
        {
            ulong* buf = stackalloc ulong[4];
            for (int i = 0; i < 4; i++)
                buf[i] = 0;
            bool ok = PATHFIND.GET_SAFE_COORD_FOR_PED(near.X, near.Y, near.Z, true, (Vector3*)buf, 0);
            float* f = (float*)buf;
            Vector3 padded = new Vector3(f[0], f[2], f[4]), packed = new Vector3(f[0], f[1], f[2]);
            if (_safeProbes < 3)
            {
                _safeProbes++;
                Log.Write("SAFECOORD probe: ok " + ok + ", near " + V(near) + ", raw " + f[0].ToString("F1") + " " + f[1].ToString("F1") + " " +
                          f[2].ToString("F1") + " " + f[3].ToString("F1") + " " + f[4].ToString("F1") + " " + f[5].ToString("F1"));
            }
            found = near;
            if (!ok)
                return false;
            if (padded.DistanceTo(near) < 60f)
                found = padded;
            else if (packed.DistanceTo(near) < 60f)
                found = packed;
            else
                return false;
            return true;
        }

        /// SafeCoord, or failing that the ground at (x, y): searched first from just above `level` (so roofs
        /// overhead are skipped), then from just above `near`. Never a spot inside a building.
        public static unsafe bool StandSpot(Vector3 near, float level, out Vector3 found)
        {
            if (SafeCoord(near, out found))
                return true;
            float[] starts = { level + 3f, near.Z + 3f };
            foreach (float start in starts)
            {
                ulong slot = 0;   // a full 8-byte slot for the float out-parameter
                if (!MISC.GET_GROUND_Z_FOR_3D_COORD(near.X, near.Y, start, (float*)&slot, false))
                    continue;
                float g = *(float*)&slot;
                if (g < -200f || Math.Abs(g - level) > 15f || INTERIOR.GET_INTERIOR_AT_COORDS(near.X, near.Y, g + 1f) != 0)
                    continue;
                found = new Vector3(near.X, near.Y, g);
                GroundFallbacks++;
                return true;
            }
            found = near;
            return false;
        }

        private static string V(Vector3 v)
        {
            return v.X.ToString("F1") + " " + v.Y.ToString("F1") + " " + v.Z.ToString("F1");
        }

        public static unsafe bool GroundZ(float x, float y, float zHint, out float z)
        {
            float g = 0f;
            bool ok = MISC.GET_GROUND_Z_FOR_3D_COORD(x, y, zHint + 50f, &g, false);
            z = ok ? g : zHint;
            return ok;
        }

        /// True when something solid is between a and b.
        public static unsafe bool Blocked(Vector3 a, Vector3 b, int ignore)
        {
            int handle = SHAPETEST.START_EXPENSIVE_SYNCHRONOUS_SHAPE_TEST_LOS_PROBE(a.X, a.Y, a.Z, b.X, b.Y, b.Z, 1 | 16, ignore, 7);
            // every out-parameter gets a full 8-byte slot (or 24 bytes for a vector), whatever the game writes
            ulong* slots = stackalloc ulong[10];
            for (int i = 0; i < 10; i++)
                slots[i] = 0;
            SHAPETEST.GET_SHAPE_TEST_RESULT(handle, (bool*)slots, (Vector3*)(slots + 1), (Vector3*)(slots + 5), (int*)(slots + 9));
            return (uint)slots[0] != 0;
        }

        /// A ray from a to b: true and where it struck, and the surface's facing there, when something solid is in the way.
        public static unsafe bool Probe(Vector3 a, Vector3 b, int ignore, out Vector3 hit, out Vector3 normal)
        {
            int handle = SHAPETEST.START_EXPENSIVE_SYNCHRONOUS_SHAPE_TEST_LOS_PROBE(a.X, a.Y, a.Z, b.X, b.Y, b.Z, 1 | 16, ignore, 7);
            ulong* slots = stackalloc ulong[10];
            for (int i = 0; i < 10; i++)
                slots[i] = 0;
            SHAPETEST.GET_SHAPE_TEST_RESULT(handle, (bool*)slots, (Vector3*)(slots + 1), (Vector3*)(slots + 5), (int*)(slots + 9));
            // the game writes each vector as three 8-byte slots (x, y, z), not as a packed Vector3
            float* f = (float*)slots;
            hit = new Vector3(f[2], f[4], f[6]);
            normal = new Vector3(f[10], f[12], f[14]);
            return (uint)slots[0] != 0;
        }

        /// The interior the entity stands in, as the game's interior type hash ("val_saloon_int"), or 0.
        public static unsafe uint InteriorType(int entity)
        {
            int interior = INTERIOR.GET_INTERIOR_FROM_ENTITY(entity);
            if (interior == 0)
                return 0;
            ulong* slots = stackalloc ulong[5];
            for (int i = 0; i < 5; i++)
                slots[i] = 0;
            INTERIOR.GET_INTERIOR_LOCATION_AND_NAMEHASH(interior, (Vector3*)slots, (uint*)(slots + 4));
            return (uint)slots[4];
        }

        public static unsafe int LastBone(int ped)
        {
            ulong bone = 0;
            return PED.GET_PED_LAST_DAMAGE_BONE(ped, (int*)&bone) ? (int)(uint)bone : 0;
        }

        public static bool IsHuman(int ped)
        {
            return PED.IS_PED_HUMAN(ped);
        }

        public static bool IsChild(Ped p)
        {
            if (p == null)
                return false;
            try
            {
                if (PED._IS_PED_CHILD(p.Handle))
                    return true;
            }
            catch
            {
            }
            return Childlike.IsMatch(Models.Name(p));
        }

        public static bool IsLaw(Ped p)
        {
            string name = Models.Name(p);
            foreach (string w in LawWords)
                if (name.Contains(w))
                    return true;
            if (_copGroup == 0)
                _copGroup = Hash("REL_COP");
            return PED.GET_PED_RELATIONSHIP_GROUP_HASH(p.Handle) == _copGroup;
        }

        /// A grown-up, living human who isn't the player, a lawman or busy fighting, and who no script owns
        /// (a story mission, a random encounter, enRAGE or another mod): only those are ever borrowed for a scene.
        /// missionOk: one of enRAGE's records, which enRAGE may mark as a mission entity (that flag doesn't stop us for them).
        public static bool Bystander(Ped p, int player, bool missionOk = false)
        {
            if (p == null || !p.Exists())
                return false;
            int h = p.Handle;
            return h != player && !ENTITY.IS_ENTITY_DEAD(h) && PED.IS_PED_HUMAN(h) && (missionOk || !ENTITY.IS_ENTITY_A_MISSION_ENTITY(h)) && !PED.IS_PED_IN_COMBAT(h, 0) &&
                   !PED.IS_PED_A_PLAYER(h) && !PED.IS_PED_FLEEING(h) && !IsChild(p) && !IsLaw(p);
        }

        /// Story and unique characters (cutscene models, named strangers): never brought back as revenants.
        public static bool StoryModel(Ped p)
        {
            string m = ModelName(p);
            return m.StartsWith("cs_") || m.StartsWith("u_") || m.StartsWith("mp_") || m.StartsWith("player_") || m.StartsWith("mes_") || m.StartsWith("msp_");
        }

        public static string ModelName(Ped p)
        {
            return p == null ? string.Empty : Models.Name(p);
        }

        /// Is the camera pointed at this spot (within the given half-angle, degrees)?
        public static bool Looking(Vector3 at, float halfAngle)
        {
            Vector3 cam = CAM.GET_GAMEPLAY_CAM_COORD();
            Vector3 rot = CAM.GET_GAMEPLAY_CAM_ROT(2);
            double pitch = rot.X * Math.PI / 180.0, yaw = rot.Z * Math.PI / 180.0;
            Vector3 dir = new Vector3((float)(-Math.Sin(yaw) * Math.Cos(pitch)), (float)(Math.Cos(yaw) * Math.Cos(pitch)), (float)Math.Sin(pitch));
            Vector3 to = at - cam;
            float len = to.Length();
            if (len < 0.5f)
                return true;
            float dot = (to.X * dir.X + to.Y * dir.Y + to.Z * dir.Z) / len;
            return dot >= (float)Math.Cos(halfAngle * Math.PI / 180.0);
        }

        /// The point dist metres behind where the camera looks, on the ground there (hillsides included);
        /// at the player's height if the ground there can't be found.
        public static Vector3 BehindCamera(Vector3 player, float dist, float spreadDegrees, Random rng)
        {
            Vector3 rot = CAM.GET_GAMEPLAY_CAM_ROT(2);
            double yaw = (rot.Z + 180.0 + (rng.NextDouble() * 2 - 1) * spreadDegrees) * Math.PI / 180.0;
            float x = player.X + (float)(-Math.Sin(yaw) * dist), y = player.Y + (float)(Math.Cos(yaw) * dist);
            return new Vector3(x, y, GroundZ(x, y, player.Z));
        }

        /// Ground height at (x, y), searching down from well above `near`; `near` itself if nothing sensible is found.
        public static unsafe float GroundZ(float x, float y, float near)
        {
            ulong slot = 0;   // a full 8-byte slot for the float out-parameter
            if (MISC.GET_GROUND_Z_FOR_3D_COORD(x, y, near + 50f, (float*)&slot, false))
            {
                float g = *(float*)&slot;
                if (g > -200f && Math.Abs(g - near) < 60f)
                    return g;
            }
            return near;
        }

        public static string Clock()
        {
            int h = CLOCK.GET_CLOCK_HOURS();
            return h < 5 ? "night" : h < 12 ? "morning" : h < 17 ? "afternoon" : h < 21 ? "evening" : "night";
        }

        public static bool Night()
        {
            int h = CLOCK.GET_CLOCK_HOURS();
            return h >= 21 || h < 5;
        }
    }
}
