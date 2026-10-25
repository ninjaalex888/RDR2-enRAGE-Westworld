// Who enRAGE deals with at gunpoint: the outlaws it lets you question (its gang men, and men in an outfit's colours),
// asked of enRAGE's own test, by name, read only. Since 0.9.5.3 such a man won't give in unless he fears you,
// and the men riding with him take aim at you, so RealNPCs' plea at gunpoint (Frontier.cs) leaves him to enRAGE.
// If the test can't be found, the plea goes on as before and the log says so.
using System;
using System.Reflection;
using RDR2;

namespace EnrageWestworld.InGame
{
    internal sealed partial class Bridge
    {
        private bool _gunpointResolved;
        private MethodInfo _interrogable;
        private FieldInfo _interroNow;

        /// enRAGE would question this man (one of its outlaws), or is questioning someone right now.
        public bool EnrageHandlesAtGunpoint(Ped p, Ped player, float range)
        {
            ResolveGunpoint();
            try
            {
                if (_interroNow != null && _interroNow.GetValue(Mod) != null)
                    return true;
                return _interrogable != null && (bool)_interrogable.Invoke(Mod, new object[] { p, player, range });
            }
            catch
            {
                return false;
            }
        }

        private void ResolveGunpoint()
        {
            if (_gunpointResolved)
                return;
            _gunpointResolved = true;
            try
            {
                _interrogable = Mod.GetType().GetMethod("Interrogable", Any, null, new[] { typeof(Ped), typeof(Ped), typeof(float) }, null);
                _interroNow = Mod.GetType().GetField("_interro", Any);
            }
            catch (Exception ex)
            {
                Log.Write("enRAGE's questioning couldn't be looked at: " + ex.Message);
            }
            Log.Write(_interrogable != null
                ? "enRAGE's questioning found: a man it can question is left to it at gunpoint, with no plea from RealNPCs."
                : "enRAGE's questioning wasn't found: RealNPCs' plea at gunpoint may overlap it.");
        }
    }
}
