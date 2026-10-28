// What enRAGE keeps on how people feel about you, by name and only read: a nemesis's hate and his fear (enRAGE's own
// EffectiveFear), one of its townsfolk's fear, liking and grudge, and for any stranger the grudge he is building
// (its _provoked scores) and his unease (its own UneaseOf). Also enRAGE's nemesis line on that 0-100 scale
// (Balance.GsThreshold), the nemesis after you now, and whether enRAGE's own debug overlay is up (it draws bars of
// its own then). For the bars and the meter (WestworldScript.Feelings.cs); if any of it can't be found, they stay off
// and the log says so.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using EnrageWestworld.Core;
using NemesisSystem;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    internal sealed partial class Bridge
    {
        private bool _feelResolved;
        private MethodInfo _effectiveFear, _uneaseOf;
        private FieldInfo _provokedField, _overlayField, _lineField;
        private PropertyInfo _activeRecord;
        private FieldInfo _provScore, _provRecord, _provSettled, _provAt;   // on enRAGE's Provocation, found from the first one seen

        /// Everything the bars and the meter read was found.
        public bool FeelingsKnown
        {
            get
            {
                ResolveFeelings();
                return _provokedField != null && _effectiveFear != null && _uneaseOf != null;
            }
        }

        /// enRAGE's own debug overlay is up, with bars of its own.
        public bool EnrageOverlayOn()
        {
            ResolveFeelings();
            try
            {
                return _overlayField != null && (bool)_overlayField.GetValue(Mod);
            }
            catch
            {
                return false;
            }
        }

        /// enRAGE's nemesis line on the 0-100 grudge scale (0 if unknown).
        public int GrudgeLine()
        {
            ResolveFeelings();
            try
            {
                return _lineField != null ? Convert.ToInt32(_lineField.GetValue(null)) : 0;
            }
            catch
            {
                return 0;
            }
        }

        /// How this person feels about you, 0..100 each; false if enRAGE has nothing on them.
        public bool FeelingsOf(int handle, out float fear, out float like, out float anger)
        {
            fear = like = anger = 0f;
            if (!FeelingsKnown)
                return false;
            try
            {
                NemesisRecord rec = RecordAt(handle);
                if (rec != null)
                {
                    fear = Convert.ToSingle(_effectiveFear.Invoke(Mod, new object[] { rec }));
                    anger = rec.Hate;
                }
                else
                {
                    Acquaintance a = AcquaintanceAt(handle);
                    object p = ProvocationOf(handle);
                    fear = Math.Max(a != null ? a.Fear : 0, Convert.ToSingle(_uneaseOf.Invoke(Mod, new object[] { handle })));
                    like = a != null ? a.Like : 0;
                    anger = Math.Max(a != null ? a.Grudge : 0, p != null ? ProvScore(p) : 0f);
                }
                return fear > 0f || like > 0f || anger > 0f;
            }
            catch
            {
                return false;
            }
        }

        /// Strangers near you whose grudge against you is still building (no nemesis yet, not let go), and the nemesis
        /// after you now. Names are left for the game side.
        public List<GrudgeRow> GrudgesAgainstYou(Vector3 me, float range)
        {
            List<GrudgeRow> rows = new List<GrudgeRow>();
            if (!FeelingsKnown)
                return rows;
            try
            {
                IDictionary all = _provokedField.GetValue(Mod) as IDictionary;
                if (all != null)
                    foreach (DictionaryEntry e in all)
                    {
                        int h = (int)e.Key;
                        object p = e.Value;
                        if (p == null || !ProvFields(p) || _provRecord.GetValue(p) != null || (bool)_provSettled.GetValue(p))
                            continue;
                        float score = ProvScore(p);
                        if (score <= 0f || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h) ||
                            ENTITY.GET_ENTITY_COORDS(h, true, true).DistanceTo(me) > range)
                            continue;
                        rows.Add(new GrudgeRow { Handle = h, Score = score, At = (int)_provAt.GetValue(p) });
                    }
                NemesisRecord active = _activeRecord != null ? _activeRecord.GetValue(Mod, null) as NemesisRecord : null;
                if (active != null && active.IsAlive)
                    rows.Add(new GrudgeRow { Who = active.Name, Score = active.Hate, Nemesis = true });
            }
            catch (Exception ex)
            {
                Fail("GrudgesAgainstYou", ex);
            }
            return rows;
        }

        /// The name enRAGE has given this person, if it has (its _named list, the one Bridge.Looks reads).
        public string EnrageNameAt(int handle, bool evenCast = false)
        {
            if (!evenCast && Spawner.IsCast(handle))
                return null;   // the test cast go by their own names (AcquaintanceAt)
            ResolveLooks();
            try
            {
                IDictionary named = _namedField != null ? _namedField.GetValue(Mod) as IDictionary : null;
                object n = named != null && named.Contains(handle) ? named[handle] : null;
                FieldInfo name = n != null ? n.GetType().GetField("Name", Any) : null;
                return name != null ? name.GetValue(n) as string : null;
            }
            catch
            {
                return null;
            }
        }

        private object ProvocationOf(int handle)
        {
            IDictionary all = _provokedField.GetValue(Mod) as IDictionary;
            return all != null && all.Contains(handle) ? all[handle] : null;
        }

        private float ProvScore(object p)
        {
            return ProvFields(p) ? Convert.ToSingle(_provScore.GetValue(p)) : 0f;
        }

        private bool ProvFields(object p)
        {
            if (_provScore == null)
            {
                Type t = p.GetType();
                _provScore = t.GetField("Score", Any);
                _provRecord = t.GetField("Record", Any);
                _provSettled = t.GetField("Settled", Any);
                _provAt = t.GetField("LastActionAt", Any);
            }
            return _provScore != null && _provRecord != null && _provSettled != null && _provAt != null;
        }

        private void ResolveFeelings()
        {
            if (_feelResolved)
                return;
            _feelResolved = true;
            try
            {
                Type t = Mod.GetType();
                _effectiveFear = t.GetMethod("EffectiveFear", Any, null, new[] { typeof(NemesisRecord) }, null);
                _uneaseOf = t.GetMethod("UneaseOf", Any, null, new[] { typeof(int) }, null);
                _provokedField = t.GetField("_provoked", Any);
                _overlayField = t.GetField("_debugOn", Any);
                _activeRecord = t.GetProperty("ActiveRecord", Any);
                Type balance = t.Assembly.GetType("NemesisSystem.Balance", false);
                _lineField = balance != null ? balance.GetField("GsThreshold", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) : null;
            }
            catch (Exception ex)
            {
                Log.Write("enRAGE's record of how people feel about you couldn't be looked at: " + ex.Message);
            }
            Log.Write(_provokedField != null && _effectiveFear != null && _uneaseOf != null
                ? "enRAGE's record of how people feel about you found: the bars and the grudge meter can show" + (_lineField != null ? "." : " (without the nemesis line on the meter).")
                : "enRAGE's record of how people feel about you wasn't found: the bars and the grudge meter stay off.");
        }
    }
}
