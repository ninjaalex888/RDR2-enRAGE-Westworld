// enRAGE's own calm period (its Grace method, the one it uses after a load, a fast travel or a train ride), reached
// by name, so RealNPCs can ask for quiet while you stand still or watch through the cinematic camera. While it lasts,
// enRAGE's tension doesn't build and enRAGE sends nothing after you: it checks its calm before every ambush, raid,
// war patrol, road ambush and faction response. If the method can't be found, only this feature stays off.
using System;
using System.Reflection;

namespace EnrageWestworld.InGame
{
    internal sealed partial class Bridge
    {
        private bool _calmResolved;
        private MethodInfo _grace;
        private object _enrageConfig;
        private FieldInfo _calmEnabled;

        private void ResolveCalm()
        {
            if (_calmResolved)
                return;
            _calmResolved = true;
            if (Mod == null)
                return;   // standing alone: nothing of enRAGE's to look at
            try
            {
                _grace = Mod.GetType().GetMethod("Grace", Any, null, new[] { typeof(int), typeof(string) }, null);
                FieldInfo cfg = Mod.GetType().GetField("Config", Any);
                _enrageConfig = cfg != null ? cfg.GetValue(Mod) : null;
                _calmEnabled = _enrageConfig != null ? _enrageConfig.GetType().GetField("CalmEnabled", Any) : null;
            }
            catch (Exception ex)
            {
                Log.Write("enRAGE's calm period couldn't be looked at: " + ex.Message);
            }
            Log.Write(_grace != null ? "enRAGE's calm period found: RealNPCs can hold enRAGE's tension and ambushes while you stand still."
                : "enRAGE's calm period (Grace) wasn't found: holding enRAGE while you stand still is off.");
        }

        public bool CanHoldEnrage
        {
            get
            {
                ResolveCalm();
                return _grace != null;
            }
        }

        /// enRAGE's own [Calm] Enabled: with it off, enRAGE ignores every calm period, its own and ours.
        public bool EnrageCalmOn
        {
            get
            {
                ResolveCalm();
                try
                {
                    return _calmEnabled == null || _enrageConfig == null || (bool)_calmEnabled.GetValue(_enrageConfig);
                }
                catch
                {
                    return true;
                }
            }
        }

        /// Asks enRAGE for quiet for this many seconds (it only ever lengthens its calm, never shortens it).
        public bool HoldEnrage(int seconds, string why)
        {
            ResolveCalm();
            if (_grace == null)
                return false;
            try
            {
                _grace.Invoke(Mod, new object[] { seconds, why });
                return true;
            }
            catch (Exception ex)
            {
                Log.Write("enRAGE's calm period couldn't be asked for: " + (ex.InnerException ?? ex).Message);
                return false;
            }
        }
    }
}
