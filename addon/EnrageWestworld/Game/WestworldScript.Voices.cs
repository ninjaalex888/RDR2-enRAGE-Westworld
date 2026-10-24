// The game's voices: which recordings each voice has (voices\banks.txt) and, where someone has them, the
// words of those recordings (voices\words.txt). Both are optional files in the add-on's data folder and are
// not shipped; without them the add-on still plays tone-matched lines and shows its own subtitle.
// words.txt is reloaded while the game runs.
using System;
using System.IO;
using EnrageWestworld.Core;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private int _nextVoiceCheck;
        private string _voicesDir;
        private DateTime _wordsStamp;

        private void LoadVoices()
        {
            _voicesDir = Path.Combine(_dir, "voices");
            string words = Path.Combine(_voicesDir, "words.txt");
            Speech.Book = VoiceBook.Load(Path.Combine(_voicesDir, "banks.txt"), words, Log.Write);
            _wordsStamp = File.Exists(words) ? File.GetLastWriteTimeUtc(words) : DateTime.MinValue;
            if (Speech.Book.BankCount > 0)
                Log.Write("Voices: " + Speech.Book.BankCount + " voice banks, words for " + Speech.Book.WordCount + " recordings.");
        }

        private void VoicesTick(int now)
        {
            if (now < _nextVoiceCheck || _voicesDir == null)
                return;
            _nextVoiceCheck = now + 3000;
            string words = Path.Combine(_voicesDir, "words.txt");
            DateTime stamp = File.Exists(words) ? File.GetLastWriteTimeUtc(words) : DateTime.MinValue;
            if (stamp != _wordsStamp)
            {
                LoadVoices();
                News("Westworld", "voice words reloaded: " + Speech.Book.WordCount + " recordings.", "news");
            }
        }
    }
}
