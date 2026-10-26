// enRAGE's territory changes as the game's top-left updates ([Output] TerritoryNews; Core/Territory.cs). enRAGE prints
// them itself as white text on the subtitle line while its [Menu] Messages is at its highest; RealNPCs reads them from
// enRAGE's own news list every 2 s (Bridge.Territory.cs) and posts each new one where gang and faction news goes
// (WarNewsTo: the top-left updates by default). Lines already in the list at load, and any while enRAGE is catching
// the world up or keeping territory quiet, are skipped, as enRAGE skips them.
using System;
using System.Collections.Generic;
using EnrageWestworld.Core;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private readonly HashSet<string> _territorySeen = new HashSet<string>();
        private int _nextTerritory, _territoryRelayed;
        private bool _territoryPrimed;
        private string _territoryLast;
        private Action _aTerritory;

        private void TerritoryStep(int now)
        {
            if (_bridge == null || _cfg == null || !_cfg.TerritoryNews || now < _nextTerritory)
                return;
            _nextTerritory = now + 2000;
            if (_aTerritory == null)
                _aTerritory = TerritoryTick;
            Safe("territory news", _aTerritory);
        }

        private void TerritoryTick()
        {
            if (!_bridge.CanReadNews)
                return;
            List<KeyValuePair<int, string>> lines = _bridge.NewsLines();
            bool held = _bridge.TerritoryHeld;
            if (!_territoryPrimed)
            {
                bool? own = _bridge.PrintsOwnNews;
                Log.Write("TERRITORY: passing enRAGE's territory changes on to " + _cfg.WarRoute + (own == true ? "; enRAGE also prints its own white copy while its [Menu] Messages is 3 (2 or 4 in NemesisMod.ini stops that)" : ""));
            }
            foreach (KeyValuePair<int, string> l in lines)
            {
                if (!_territorySeen.Add(l.Key + "|" + l.Value) || !_territoryPrimed || held)
                    continue;   // seen before, already there at load, or enRAGE is keeping it quiet
                string text = Territory.FromFeed(l.Value);
                if (text == null)
                    continue;
                string war = _cfg.WarRoute;
                if (war == "feed")
                    Feed.Notice("Territory", text);
                else if (war == "corner")
                    NewsPush("Territory", text, "news");
                else
                    _ticker.Push("Territory", text, "news");
                _territoryRelayed++;
                _territoryLast = text;
                Log.Write("TERRITORY (" + war + "): " + text);
            }
            _territoryPrimed = true;
            if (_territorySeen.Count > 300)
            {
                _territorySeen.Clear();   // keep only what's still in enRAGE's list (all of it handled by now)
                foreach (KeyValuePair<int, string> l in lines)
                    _territorySeen.Add(l.Key + "|" + l.Value);
            }
        }

        /// F7: on or off, whether enRAGE's list can be read, how many passed on, and whether enRAGE prints its own too.
        private string TerritoryStatus()
        {
            if (!_cfg.TerritoryNews)
                return "off ([Output] TerritoryNews)";
            if (_bridge == null)
                return "on; enRAGE not found";
            bool? own = _bridge.PrintsOwnNews;
            return "on, to " + _cfg.WarRoute + "; enRAGE's news list " + (_bridge.CanReadNews ? "found" : "NOT found") + "; " + _territoryRelayed + " passed on" +
                   (_territoryLast != null ? ", last: " + _territoryLast : "") + (own == true ? "; enRAGE prints its own white copy too ([Menu] Messages 3)" : own == false ? "; enRAGE prints none of its own" : "");
        }
    }
}
