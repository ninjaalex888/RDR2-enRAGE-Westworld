// A body's look as a fingerprint (night 2026-10-29, item 5; roadmap 34, phase A's second half: identity by look of our own,
// standing alone). The model and every piece the game has dressed the body in, with its tints, hashed: the same body met
// again reads the same, so the same person can be known in it (Core: Store.ResidentByLook). Adapted from enRAGE's
// Fingerprint (NemesisMod, its author permits reuse; the "full" print it keys its own prints by): the same natives, 8-byte
// out slots, the pieces sorted and hashed by FNV-1a with a separator between them. Null when the body isn't ready to be
// read (not yet rendered) or wears nothing readable, as enRAGE leaves an empty print unused.
using System;
using System.Collections.Generic;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    internal static class Looks
    {
        public const int MostPieces = 64;

        /// The look of this body, "MODEL-HASH" in hex, or null.
        public static unsafe string Of(Ped p)
        {
            if (p == null || !p.Exists() || p.IsDead)
                return null;
            int h = p.Handle;
            if (!PED.IS_PED_READY_TO_RENDER(h))
                return null;
            int n = PED._GET_NUM_COMPONENTS_IN_PED(h);
            ulong* guids = stackalloc ulong[4];
            ulong* tint = stackalloc ulong[4];
            var parts = new List<string>();
            for (int i = 0; i < n && i < MostPieces; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    guids[j] = 0;
                    tint[j] = 0;
                }
                if (!PED.GET_META_PED_ASSET_GUIDS(h, i, (uint*)guids, (uint*)(guids + 1), (uint*)(guids + 2), (uint*)(guids + 3)))
                    continue;
                PED.GET_META_PED_ASSET_TINT(h, i, (uint*)tint, (int*)(tint + 1), (int*)(tint + 2), (int*)(tint + 3));
                uint category = 0;
                try
                {
                    category = (uint)PED._0x9B90842304C938A7((ulong)h, (ulong)i, 0);   // the piece's category
                }
                catch
                {
                }
                uint drawable = (uint)guids[0], albedo = (uint)guids[1];
                if (drawable == 0 && albedo == 0)
                    continue;
                parts.Add(category.ToString("X8") + ":" + drawable.ToString("X8") + "/" + albedo.ToString("X8") + " " + ((uint)tint[0]).ToString("X8") + "." + (int)tint[1] + "." + (int)tint[2] + "." + (int)tint[3]);
            }
            if (parts.Count == 0)
                return null;
            parts.Sort(StringComparer.Ordinal);
            return ((uint)ENTITY.GET_ENTITY_MODEL(h)).ToString("X8") + "-" + Fnv(parts).ToString("X8");
        }

        private static uint Fnv(List<string> parts)
        {
            uint x = 2166136261u;
            foreach (string part in parts)
            {
                foreach (char c in part)
                {
                    x ^= c;
                    x *= 16777619u;
                }
                x ^= '|';
                x *= 16777619u;
            }
            return x;
        }
    }
}
