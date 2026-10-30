// The settings panel slows the world while it's open (Lex, 2026-10-27: "the same mechanic the game uses for slowing
// down time when Tab is held down or B is pressed"). The game's own wheels slow the world through the engine's time
// scale; HUD._ENABLE_REDUCED_MENU_TIME_SCALE only lets the game's own menus (the weapon wheel, the satchel) do it, so
// the panel sets the time scale itself (MISC.SET_TIME_SCALE, [Output] PanelTimeScale, 0.25 as it comes) every frame it's
// open, and puts it back to 1 the frame it isn't: closed, a mission, a load, the panel switched off after a fault, the
// add-on stopping. Only ever back to 1 when it was the panel that slowed it. Never in a story mission.
using System;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private bool _panelSlowed, _panelClockPaused;

        /// Every tick, in or out of the world: slow while the panel is open, normal the moment it isn't.
        private void PanelTimeStep()
        {
            bool want = PanelOpen && _cfg != null && _cfg.PanelSlowsTime && !Game.IsLoading && !MISC.GET_MISSION_FLAG();
            if (want)
            {
                MISC.SET_TIME_SCALE(_cfg.PanelTimeScale);
                if (_cfg.PanelTimeScale <= 0.001f && !_panelClockPaused)
                {
                    CLOCK.PAUSE_CLOCK(true, 0);   // paused: the clock too, as the game's own pause (Lex, 2026-10-30)
                    _panelClockPaused = true;
                }
                if (!_panelSlowed)
                    Log.Detail("PANEL time: " + (_cfg.PanelTimeScale <= 0.001f ? "paused" : "slowed to " + _cfg.PanelTimeScale.ToString("0.00")) + " while the settings panel is open");
                _panelSlowed = true;
            }
            else if (_panelSlowed)
                PanelTimeRestore("the panel closed");
        }

        /// Normal speed again, if the panel had slowed it (whatever else is going on: called from every way out).
        private void PanelTimeRestore(string why)
        {
            if (!_panelSlowed)
                return;
            try
            {
                MISC.SET_TIME_SCALE(1f);
                if (_panelClockPaused)
                    CLOCK.PAUSE_CLOCK(false, 0);
            }
            catch
            {
            }
            _panelSlowed = false;
            _panelClockPaused = false;
            Log.Detail("PANEL time: normal speed (" + why + ")");
        }
    }
}
