// Who came at you, as enRAGE sees it: the people it saw start a fight with you when you hadn't started it (no gun on
// them, no shots near them, no crime of yours under way), and the ones its town law saw swing first. RealNPCs only
// reads these, by name, to tell self-defence from murder; if they can't be found it decides for itself.
using System;
using System.Collections.Generic;
using System.Reflection;

namespace EnrageWestworld.InGame
{
    internal sealed partial class Bridge
    {
        private bool _defenceResolved;
        private FieldInfo _cameAtYouField, _selfDefenceField;
        private MethodInfo _roleOf;

        // enRAGE's fighters, the ones it marks red on the map and counts as a fair fight: its nemesis and his men,
        // hired guns, bounty hunters, patrols, war skirmishes, gang camps and rivals
        private static readonly HashSet<string> FighterRoles = new HashSet<string> { "Active", "Crew", "HiredGun", "Patrol", "Skirmish", "Hunter", "Rival", "Camp" };

        /// One of enRAGE's fighters (red on the map). Read from enRAGE's own role for the ped.
        public bool EnrageFighter(int handle)
        {
            ResolveDefence();
            try
            {
                object role = _roleOf != null ? _roleOf.Invoke(Mod, new object[] { handle }) : null;
                return role != null && FighterRoles.Contains(role.ToString());
            }
            catch
            {
                return false;
            }
        }

        private void ResolveDefence()
        {
            if (_defenceResolved)
                return;
            _defenceResolved = true;
            try
            {
                _cameAtYouField = Mod.GetType().GetField("_cameAtYou", Any);
                _selfDefenceField = Mod.GetType().GetField("_selfDefence", Any);
                _roleOf = Mod.GetType().GetMethod("RoleOf", Any, null, new[] { typeof(int) }, null);
            }
            catch (Exception ex)
            {
                Log.Write("enRAGE's record of who came at you couldn't be looked at: " + ex.Message);
            }
            Log.Write(_cameAtYouField != null || _selfDefenceField != null
                ? "enRAGE's record of who came at you found" + (_roleOf != null ? ", and of its fighters (red on the map)" : ", but not of its fighters") + ": kills in self-defence aren't noted as crimes."
                : "enRAGE's record of who came at you wasn't found: RealNPCs judges self-defence on its own.");
        }

        /// enRAGE saw this person come at you (and you hadn't started it).
        public bool EnrageSaysCameAtYou(int handle)
        {
            ResolveDefence();
            return Holds(_cameAtYouField, handle) || Holds(_selfDefenceField, handle);
        }

        private bool Holds(FieldInfo f, int handle)
        {
            try
            {
                HashSet<int> set = f != null ? f.GetValue(Mod) as HashSet<int> : null;
                return set != null && set.Contains(handle);
            }
            catch
            {
                return false;
            }
        }
    }
}
