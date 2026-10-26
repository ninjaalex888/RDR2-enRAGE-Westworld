// When a spoken line gets a subtitle, and what it says. The game gives mods no way into its own dialogue subtitles,
// so it goes on the game's subtitle line (the objective line, Screen.PrintSubtitle), and only for what's said to the
// player, only while the speaker's voice is really playing (or the rest of a line whose voice played), with the
// Subtitles setting and the game's own Subtitles option both on, and only while the speaker, when known, is in plain
// view. Talk the player only overhears never gets one, nor does a line nobody voices.
namespace EnrageWestworld.Core
{
    public static class SubtitleRule
    {
        /// The subtitle to show, or null for none. words: what the recording says, when the word list knows it (shown
        /// instead of the written line, so the subtitle says what is heard).
        public static string Text(bool toYou, bool voicePlayed, bool restOfVoiced, bool settingOn, bool gameOption, bool speakerInView,
                                  string speaker, string words, string line)
        {
            if (!toYou || !settingOn || !gameOption || !speakerInView || !(voicePlayed || restOfVoiced))
                return null;
            string say = !string.IsNullOrEmpty(words) ? words : line;
            if (string.IsNullOrEmpty(say))
                return null;
            return (string.IsNullOrEmpty(speaker) ? "" : speaker + ": ") + "\"" + say + "\"";
        }
    }
}
