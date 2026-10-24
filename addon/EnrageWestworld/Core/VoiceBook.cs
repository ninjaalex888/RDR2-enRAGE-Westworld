// What the game's voices can say, and the words of the recordings someone has written down.
// Two optional files in the add-on's data folder (voices\), neither shipped with the add-on:
//   banks.txt  one voice bank per line: BANK<tab>LINE:takes,LINE:takes,...  (which recordings each voice has)
//   words.txt  BANK|LINE|TAKE|the words spoken, one recording per line; # starts a comment
// A voice bank is named after the ped model it belongs to (0465_A_M_M_VALTOWNFOLK_01_WHITE_01). The game
// can't say which bank a ped is using, so only models with exactly one bank are trusted: for those the
// bank is certain, and the add-on can play a chosen take and show its real words.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace EnrageWestworld.Core
{
    public sealed class VoiceBook
    {
        private static readonly Regex BankModel = new Regex(@"^\d+_(.+?)_[A-Z]+_\d+$");

        private readonly Dictionary<string, Dictionary<string, int>> _takes = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, List<string>> _banksByModel = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, string> _words = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public int BankCount { get { return _takes.Count; } }
        public int WordCount { get { return _words.Count; } }

        public static VoiceBook Load(string banksPath, string wordsPath, Action<string> log)
        {
            VoiceBook b = new VoiceBook();
            try
            {
                if (File.Exists(banksPath))
                    b.ReadBanks(File.ReadAllLines(banksPath));
                if (File.Exists(wordsPath))
                    b.ReadWords(File.ReadAllLines(wordsPath), log);
            }
            catch (Exception ex)
            {
                log?.Invoke("Voices: couldn't read the voice files: " + ex.Message);
            }
            return b;
        }

        public void ReadBanks(IEnumerable<string> lines)
        {
            foreach (string raw in lines)
            {
                int tab = raw.IndexOf('\t');
                if (tab <= 0)
                    continue;
                string bank = raw.Substring(0, tab).Trim();
                Dictionary<string, int> takes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
                foreach (string part in raw.Substring(tab + 1).Split(','))
                {
                    int colon = part.LastIndexOf(':');
                    int n;
                    if (colon > 0 && int.TryParse(part.Substring(colon + 1), out n) && n > 0)
                        takes[part.Substring(0, colon).Trim()] = n;
                }
                _takes[bank] = takes;
                Match m = BankModel.Match(bank);
                if (!m.Success)
                    continue;
                List<string> list;
                if (!_banksByModel.TryGetValue(m.Groups[1].Value, out list))
                    _banksByModel[m.Groups[1].Value] = list = new List<string>();
                list.Add(bank);
            }
        }

        public void ReadWords(IEnumerable<string> lines, Action<string> log)
        {
            int bad = 0;
            foreach (string raw in lines)
            {
                string s = raw.Trim();
                if (s.Length == 0 || s.StartsWith("#"))
                    continue;
                string[] f = s.Split(new[] { '|' }, 4);
                int take;
                if (f.Length < 4 || !int.TryParse(f[2].Trim(), out take) || f[3].Trim().Length == 0)
                {
                    bad++;
                    continue;
                }
                _words[Key(f[0], f[1], take)] = f[3].Trim();
                // the bank list misses lines whose names nobody has decoded yet; a written-down take proves it exists
                Dictionary<string, int> takes;
                string bank = f[0].Trim(), line = f[1].Trim();
                if (!_takes.TryGetValue(bank, out takes))
                    continue;
                int had;
                if (!takes.TryGetValue(line, out had) || had < take)
                    takes[line] = take;
            }
            if (bad > 0)
                log?.Invoke("Voices: skipped " + bad + " line(s) in words.txt that aren't BANK|LINE|TAKE|words.");
        }

        /// The one bank this model's voice comes from, or null if there are several (or none).
        public string SoleBank(string model)
        {
            List<string> list;
            return model != null && _banksByModel.TryGetValue(model, out list) && list.Count == 1 ? list[0] : null;
        }

        public int Takes(string bank, string line)
        {
            Dictionary<string, int> t;
            int n;
            return bank != null && line != null && _takes.TryGetValue(bank, out t) && t.TryGetValue(line, out n) ? n : 0;
        }

        public string Words(string bank, string line, int take)
        {
            string w;
            return _words.TryGetValue(Key(bank, line, take), out w) ? w : null;
        }

        /// A recording from the set for this bank: one whose words are known if there is any (they take turns),
        /// otherwise a random take of the first line in the set the bank has. False if the bank has none.
        public bool Pick(string bank, string[] set, int turn, out string line, out int take, out string words)
        {
            line = null;
            take = 0;
            words = null;
            List<KeyValuePair<string, int>> known = new List<KeyValuePair<string, int>>();
            string first = null;
            foreach (string l in set)
            {
                int n = Takes(bank, l);
                if (n == 0)
                    continue;
                if (first == null)
                    first = l;
                for (int t = 1; t <= n; t++)
                    if (Words(bank, l, t) != null)
                        known.Add(new KeyValuePair<string, int>(l, t));
            }
            if (known.Count > 0)
            {
                KeyValuePair<string, int> k = known[(turn & 0x7fffffff) % known.Count];
                line = k.Key;
                take = k.Value;
                words = Words(bank, line, take);
                return true;
            }
            if (first == null)
                return false;
            line = first;
            take = 1 + (turn & 0x7fffffff) % Takes(bank, first);
            return true;
        }


        private static string Key(string bank, string line, int take)
        {
            return bank.Trim() + "|" + line.Trim() + "|" + take;
        }
    }
}
