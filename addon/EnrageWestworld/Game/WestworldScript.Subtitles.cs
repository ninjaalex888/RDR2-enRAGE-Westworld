// Subtitles ([Output] Subtitles, Core/SubtitleRule.cs): what people say to the player shows on the game's subtitle
// line while their voice plays; nothing else does (Feed.Tick). The game gives mods no way into its own dialogue
// subtitles: a probe (2026-10-26) played one greeting eight ways (to the player, to another person, to nobody; the
// speech struct's sync flag and its two unnamed flags; the scripted-speech natives) and the game subtitled none.
// Here: the one-time log of the game's own Subtitles option, which our subtitles follow too, and the F7 line.
using System;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private bool _subNoted;
        private Action _aSubtitles;

        private void SubtitleStep(int now)
        {
            if (_aSubtitles == null)
                _aSubtitles = SubtitleNote;
            if (!_subNoted)
                Safe("subtitles", _aSubtitles);
        }

        private void SubtitleNote()
        {
            _subNoted = true;
            Log.Write("SUBTITLES: " + (_cfg.Subtitles == "off" ? "off ([Output] Subtitles)" : "on, for what's said to you while the voice plays") + "; the game's own Subtitles option is " +
                      (HUD.IS_SUBTITLE_PREFERENCE_SWITCHED_ON() ? "on" : "OFF, so none show (turn it on in the game's settings)"));
        }

        /// F7: the setting, the game's own option, and the subtitles shown so far.
        private string SubtitlesStatus()
        {
            return (_cfg.Subtitles == "off" ? "off" : "on") + "; the game's own option " + (HUD.IS_SUBTITLE_PREFERENCE_SWITCHED_ON() ? "on" : "OFF") + "; " + Feed.SubtitlesShown + " shown" +
                   (Feed.LastSubtitle != null ? ", last: " + Feed.LastSubtitle : "") + "; news to " + _cfg.NewsRoute + ", faction news to " + _cfg.WarRoute;
        }
    }
}
