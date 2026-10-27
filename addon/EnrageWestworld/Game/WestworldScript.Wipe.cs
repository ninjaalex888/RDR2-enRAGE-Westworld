// The settings panel's "Wipe every NPC's memory" (0.6.3, for testing; Lex: a full reset of what NPCs remember, in the
// Alt+F panel, with a second Enter to be sure). Everything RealNPCs keeps of the people in this world, for the character
// being played, is forgotten: who they are, what they know and feel about the player, their ties, links, worries' marks,
// deaths and the rest of the save. A copy of the save goes to RealNPCs\backup first. enRAGE's own memory is untouched.
// It works the way switching character does (a fresh store and engine, LoadStore), after the features holding people
// have let them go.
using System;
using System.IO;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        /// An action row of the settings panel, by its id: what to say back.
        private string PanelAction(string id)
        {
            switch (id)
            {
                case "wipe-memories":
                    return WipeMemories();
                case "stage-execution":
                    if (!_cfg.Executions)
                        return "Executions are off: turn them on above first.";
                    _execRequested = true;   // started by the encounters' tick, once the panel is closed
                    return "Close the panel: a man will be held at gunpoint somewhere near you, out of sight (a mark on the map).";
                default:
                    return "Nothing to do for " + id + ".";
            }
        }

        private string WipeMemories()
        {
            if (_store == null || _engine == null)
                return "Nothing to wipe yet: RealNPCs hasn't finished loading (it waits for enRAGE).";
            if (MISC.GET_MISSION_FLAG())
                return "Not during a story mission.";
            string path = StorePath, backup;
            try
            {
                Save();
                string kept = Path.Combine(_dir, "backup");
                Directory.CreateDirectory(kept);
                backup = Path.Combine(kept, Path.GetFileNameWithoutExtension(path) + ".before-wipe-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".json");
                if (File.Exists(path))
                    File.Copy(path, backup, true);
            }
            catch (Exception ex)
            {
                return "Couldn't keep a copy of the save first, so nothing was wiped: " + ex.Message;
            }
            int people = _store.People.Count, residents = _store.Residents.Count, ties = _store.Ties.Count, deaths = _store.Deaths != null ? _store.Deaths.Count : 0;
            // everyone the features hold, let go first
            _director.Abort("memories wiped");
            EndEncounters("memories wiped");
            WakeActsAbort();
            TrystAbort();
            if (_rough != null)
                RoughEnd(_rough, "stopped: memories wiped", _now);
            ClearRegularBlips();
            // and what the game side remembers of who is who
            _resBind.Clear();
            _bodyWas.Clear();
            _known.Clear();
            _deadNoted.Clear();
            _downed.Clear();
            _shareDue.Clear();
            _unshareDue.Clear();
            _courtThoughts.Clear();
            _disbeliefAt.Clear();
            _wakeCool.Clear();
            try
            {
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch (Exception ex)
            {
                return "Couldn't clear the save (" + ex.Message + "); the copy is " + backup + ".";
            }
            LoadStore();   // a fresh store and engine, as when the character changes
            Save();
            Log.Write("WIPE: everything RealNPCs remembered of the people in this world is gone (" + people + " people, " + residents + " residents, " + ties + " ties, " +
                      deaths + " deaths); the old save is " + backup);
            return "Wiped: " + people + " people, " + residents + " residents, " + ties + " ties and " + deaths + " deaths forgotten. enRAGE's own memory is untouched; the old save is in RealNPCs\\backup.";
        }
    }
}
