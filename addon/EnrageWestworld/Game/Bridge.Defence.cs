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

        private void ResolveDefence()
        {
            if (_defenceResolved)
                return;
            _defenceResolved = true;
            try
            {
                _cameAtYouField = Mod.GetType().GetField("_cameAtYou", Any);
                _selfDefenceField = Mod.GetType().GetField("_selfDefence", Any);
            }
            catch (Exception ex)
            {
                Log.Write("enRAGE's record of who came at you couldn't be looked at: " + ex.Message);
            }
            Log.Write(_cameAtYouField != null || _selfDefenceField != null
                ? "enRAGE's record of who came at you found: kills in self-defence aren't noted as crimes."
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
