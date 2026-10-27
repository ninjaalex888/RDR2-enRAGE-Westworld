// Subtitles ([Output] Subtitles, Core/SubtitleRule.cs): what people say to the player shows while their voice plays,
// drawn the game's way (Feed.DrawSubtitle, every frame from Frame); nothing else does (Feed.Tick). The font test
// (Shift+F2) shows the game's subtitle line in each candidate face for 20 s, numbered, to find the game's own.
// The game gives mods no way into its own dialogue subtitles: a probe (2026-10-26) played one greeting eight ways (to the player, to another person, to nobody; the
// speech struct's sync flag and its two unnamed flags; the scripted-speech natives) and the game subtitled none.
// Here: the one-time log of the game's own Subtitles option, which our subtitles follow too, and the F7 line.
using System;
using System.Windows.Forms;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private bool _subNoted;
        private Action _aSubtitles, _aSubDraw;
        private int _fontTestUntil;
        private static readonly string[] FontFaces = { "$body", "$body1", "$body2", "$body3", "$body4", "$body5", "$title", "$title1", "$title2", "$journal_text", "$ledger", "$catalog1" };

        /// Every frame: the subtitle showing (and the font test while it runs), when the screen is ours to draw on.
        private void SubtitleDraw(int now)
        {
            if (_aSubDraw == null)
                _aSubDraw = () =>
                {
                    if (_now < _fontTestUntil)
                        FontTest();
                    else
                        Feed.DrawSubtitle(_now);
                };
            if (!PanelOpen && !_reader.Open && TickerVisible(now))
                Safe("subtitles", _aSubDraw);
        }

        /// Shift+F2: the game's subtitle line in each face, numbered, for 20 s (say which matches the game's).
        private void FontTest()
        {
            float y = 0.12f, lineH = Draw.LineHeight(Feed.SubtitleScale) * 1.25f;
            for (int i = 0; i < FontFaces.Length; i++)
            {
                Draw.Rect(0.18f, y + i * lineH - 0.003f, 0.64f, lineH, 0, 0, 0, 150);
                Draw.Text((i + 1) + "  I've had better days, that's for sure.", 0.2f, y + i * lineH, Feed.SubtitleScale, 255, 255, 255, 255, FontFaces[i]);
            }
        }

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
