// People the add-on puts into the world (stalkers, revenants, wedding parties). Never blocks: the model is
// requested and the spawn simply returns null until it has streamed in. Never during missions or fades.
// Everything spawned is remembered and removed when the add-on stops. People already in the world who are taken
// over for a while (a townsperson turned stalker) count as ours too, but are only ever handed back, never deleted.
using System.Collections.Generic;
using System.Linq;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    internal static class Spawner
    {
        private static readonly HashSet<int> Ours = new HashSet<int>();
        private static readonly HashSet<int> Borrowed = new HashSet<int>();

        /// Takes over someone already in the world, so the game keeps them and nothing else of ours directs them.
        /// True if the game now treats them as ours (a mission entity).
        public static bool Borrow(Ped p)
        {
            int h = p.Handle;
            ENTITY.SET_ENTITY_AS_MISSION_ENTITY(h, true, true);
            Ours.Add(h);
            Borrowed.Add(h);
            return ENTITY.IS_ENTITY_A_MISSION_ENTITY(h);
        }

        public static bool IsBorrowed(int handle)
        {
            return Borrowed.Contains(handle);
        }

        /// Hands someone borrowed back to the game, going about their day again.
        private static unsafe void GiveBack(int h)
        {
            Ours.Remove(h);
            Borrowed.Remove(h);
            if (!ENTITY.DOES_ENTITY_EXIST(h))
                return;
            StandUp(h);
            TASK.CLEAR_PED_TASKS(h, true, false);
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, false);
            PED.SET_PED_KEEP_TASK(h, false);
            if (!ENTITY.IS_ENTITY_DEAD(h))
                TASK.TASK_WANDER_STANDARD(h, 10f, 10);
            ENTITY.SET_PED_AS_NO_LONGER_NEEDED(&h);
        }

        public static bool SafeToSpawn()
        {
            return !MISC.GET_MISSION_FLAG() && !CAM.IS_SCREEN_FADED_OUT() && !CAM.IS_SCREEN_FADING_OUT() && !Game.IsLoading;
        }

        /// Starts streaming a model; true once it is ready.
        public static bool Ready(uint model)
        {
            if (!STREAMING.IS_MODEL_VALID(model) || !STREAMING.IS_MODEL_A_PED(model))
                return false;
            if (STREAMING.HAS_MODEL_LOADED(model))
                return true;
            STREAMING.REQUEST_MODEL(model, false);
            return false;
        }

        public static Ped Spawn(uint model, Vector3 pos, float heading)
        {
            if (!SafeToSpawn() || !Ready(model))
                return null;
            STREAMING.REQUEST_COLLISION_AT_COORD(pos.X, pos.Y, pos.Z);
            Ped p = World.CreatePed((PedHash)model, pos, heading);
            STREAMING.SET_MODEL_AS_NO_LONGER_NEEDED(model);
            if (p == null || !p.Exists())
                return null;
            ENTITY.SET_ENTITY_AS_MISSION_ENTITY(p.Handle, true, true);
            Ours.Add(p.Handle);
            return p;
        }

        public static bool IsOurs(int handle)
        {
            return Ours.Contains(handle);
        }

        /// Deletes the ped (when nobody can see it vanish) or hands it back to the game. Someone borrowed is always
        /// handed back.
        public static unsafe void Release(Ped p, bool delete)
        {
            if (p == null)
                return;
            int h = p.Handle;
            if (Borrowed.Contains(h))
            {
                GiveBack(h);
                return;
            }
            Ours.Remove(h);
            if (!ENTITY.DOES_ENTITY_EXIST(h))
                return;
            if (delete)
            {
                p.Delete();
            }
            else
            {
                StandUp(h);
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, false);
                ENTITY.SET_PED_AS_NO_LONGER_NEEDED(&h);
            }
        }

        /// Out of a crouch or a stealth walk (the stalker's creep), so nobody goes back to the world still crouching.
        public static void StandUp(int h)
        {
            if (PED.GET_PED_CROUCH_MOVEMENT(h))
                PED._SET_PED_CROUCH_MOVEMENT(h, false, 0, false);
            if (PED.GET_PED_STEALTH_MOVEMENT(h))
                PED.SET_PED_STEALTH_MOVEMENT(h, false, 0, 0);
        }

        public static void CleanupAll()
        {
            foreach (int h in Ours.ToList())
            {
                try
                {
                    if (Borrowed.Contains(h))
                        GiveBack(h);   // someone who was already in the world: never deleted
                    else if (ENTITY.DOES_ENTITY_EXIST(h))
                        new Ped(h).Delete();
                }
                catch
                {
                }
            }
            Ours.Clear();
            Borrowed.Clear();
        }

        // ---- choosing a model ----------------------------------------------------------------------

        private static readonly Dictionary<string, string[]> TownMen = new Dictionary<string, string[]>
        {
            { "Valentine", new[] { "a_m_m_valtownfolk_01", "a_m_m_valtownfolk_02", "a_m_m_valfarmer_01", "a_m_m_vallaborer_01" } },
            { "Rhodes", new[] { "a_m_m_rhdtownfolk_01", "a_m_m_rhdtownfolk_02", "a_m_m_rhdupperclass_01" } },
            { "Saint Denis", new[] { "a_m_m_middlesdtownfolk_01", "a_m_m_middlesdtownfolk_02", "a_m_m_lowersdtownfolk_01", "a_m_m_nbxupperclass_01" } },
            { "Blackwater", new[] { "a_m_m_blwtownfolk_01", "a_m_m_blwupperclass_01", "a_m_m_blwlaborer_01" } },
            { "Strawberry", new[] { "a_m_m_strtownfolk_01", "a_m_m_strlaborer_01" } },
            { "Annesburg", new[] { "a_m_m_asbtownfolk_01", "a_m_m_asbminer_01" } },
            { "Armadillo", new[] { "a_m_m_armtownfolk_01", "a_m_m_armtownfolk_02" } },
            { "Tumbleweed", new[] { "a_m_m_tumtownfolk_01", "a_m_m_tumtownfolk_02" } },
            { "Van Horn", new[] { "a_m_m_vhttownfolk_01" } }
        };

        private static readonly Dictionary<string, string[]> TownWomen = new Dictionary<string, string[]>
        {
            { "Valentine", new[] { "a_f_m_valtownfolk_01" } },
            { "Rhodes", new[] { "a_f_m_rhdtownfolk_01", "a_f_m_rhdtownfolk_02", "a_f_m_rhdupperclass_01" } },
            { "Saint Denis", new[] { "a_f_m_middlesdtownfolk_01", "a_f_m_middlesdtownfolk_02", "a_f_m_lowersdtownfolk_01", "a_f_m_nbxupperclass_01" } },
            { "Blackwater", new[] { "a_f_m_blwtownfolk_01", "a_f_m_blwtownfolk_02", "a_f_m_blwupperclass_01" } },
            { "Strawberry", new[] { "a_f_m_strtownfolk_01" } },
            { "Annesburg", new[] { "a_f_m_asbtownfolk_01" } },
            { "Armadillo", new[] { "a_f_m_armtownfolk_01", "a_f_m_armtownfolk_02" } },
            { "Tumbleweed", new[] { "a_f_m_tumtownfolk_01", "a_f_m_tumtownfolk_02" } },
            { "Van Horn", new[] { "a_f_m_vhttownfolk_01" } }
        };

        private static readonly string[] RoughMen = { "a_m_m_griroughtravellers_01", "a_m_m_bynroughtravellers_01", "a_m_m_htlroughtravellers_01", "a_m_m_huntertravelers_cool_01", "a_m_m_rkrroughtravellers_01" };
        private static readonly string[] RoughWomen = { "a_f_m_roughtravellers_01" };

        public static uint ModelFor(string town, bool woman, int seed)
        {
            string[] pool;
            if (town != null && (woman ? TownWomen : TownMen).TryGetValue(town, out pool))
                return Natives.Hash(pool[(seed & 0x7fffffff) % pool.Length]);
            pool = woman ? RoughWomen : RoughMen;
            return Natives.Hash(pool[(seed & 0x7fffffff) % pool.Length]);
        }
    }
}
