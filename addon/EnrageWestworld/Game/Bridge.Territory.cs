// enRAGE's own news list (NemesisMod._feed: its last 30 lines, each with the time it was added), read by name for the
// territory relay, with the two times it keeps territory news to itself (_worldCatchingUp after a load, _quietControl)
// and whether it prints its own copy on the subtitle line (Config.GrudgeMessages, from its [Menu] Messages).
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace EnrageWestworld.InGame
{
    internal sealed partial class Bridge
    {
        private bool _newsResolved;
        private FieldInfo _newsList, _catchingUp, _quietControl, _config;

        private void ResolveNews()
        {
            if (_newsResolved)
                return;
            _newsResolved = true;
            try
            {
                Type t = Mod.GetType();
                _newsList = t.GetField("_feed", Any);
                _catchingUp = t.GetField("_worldCatchingUp", Any);
                _quietControl = t.GetField("_quietControl", Any);
                _config = t.GetField("Config", Any);
            }
            catch (Exception ex)
            {
                Log.Write("enRAGE's news list couldn't be looked at: " + ex.Message);
            }
            Log.Write(_newsList != null ? "enRAGE's news list found: its territory changes can be passed on." : "enRAGE's news list (_feed) wasn't found: its territory changes stay its own.");
        }

        public bool CanReadNews
        {
            get
            {
                ResolveNews();
                return _newsList != null;
            }
        }

        /// enRAGE's news lines as (time, text), oldest first; empty if they can't be read.
        public List<KeyValuePair<int, string>> NewsLines()
        {
            ResolveNews();
            var lines = new List<KeyValuePair<int, string>>();
            IList items = _newsList != null ? _newsList.GetValue(Mod) as IList : null;
            if (items == null)
                return lines;
            foreach (object it in items)
            {
                if (it == null)
                    continue;
                Type t = it.GetType();
                FieldInfo a = t.GetField("Item1"), b = t.GetField("Item2");
                object at = a != null ? a.GetValue(it) : null, text = b != null ? b.GetValue(it) : null;
                if (at is int && text is string)
                    lines.Add(new KeyValuePair<int, string>((int)at, (string)text));
            }
            return lines;
        }

        /// enRAGE is keeping territory news to itself just now (catching the world up after a load, or quiet).
        public bool TerritoryHeld
        {
            get
            {
                ResolveNews();
                return Flag(_catchingUp) || Flag(_quietControl);
            }
        }

        /// enRAGE prints its own copy of territory news on the subtitle line ([Menu] Messages at its highest);
        /// null if that can't be read.
        public bool? PrintsOwnNews
        {
            get
            {
                ResolveNews();
                try
                {
                    object cfg = _config != null ? _config.GetValue(Mod) : null;
                    PropertyInfo p = cfg != null ? cfg.GetType().GetProperty("GrudgeMessages", Any) : null;
                    return p != null ? (bool?)(bool)p.GetValue(cfg, null) : null;
                }
                catch
                {
                    return null;
                }
            }
        }

        private bool Flag(FieldInfo f)
        {
            try
            {
                return f != null && (bool)f.GetValue(Mod);
            }
            catch
            {
                return false;
            }
        }
    }
}
