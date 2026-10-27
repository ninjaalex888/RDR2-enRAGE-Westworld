// The NPC mind in shadow mode (daytime run 2026-10-27): what the mind works out for people near the player, kept in
// memory only (never saved), capped, and forgotten when an older save is loaded (Engine.ForgetThinking, from the
// store's rewind) or a new one is. It reads only people who already have an identity; it never makes anyone, rolls the
// engine's Random or writes to the store. With [Mind] MindLive off, nothing it works out reaches the screen.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public sealed class MindShadow
    {
        public const int Cap = 600;

        // id|temper -> their traits (worked out once; the same person always gets the same ones)
        private readonly Dictionary<string, Traits> _traits = new Dictionary<string, Traits>();

        /// Goes up each time it all is forgotten, so the game side can drop what it keeps by handle too.
        public int Generation { get; private set; }
        public int TraitsMade { get; private set; }
        public int Cleared { get; private set; }

        /// Their traits, from the cache or worked out now. Null for nobody.
        public Traits TraitsOf(TraitSource s)
        {
            if (s == null || string.IsNullOrEmpty(s.Id))
                return null;
            string key = s.Id + "|" + (s.Temper ?? "-");
            Traits t;
            if (_traits.TryGetValue(key, out t))
                return t;
            if (_traits.Count >= Cap)
                _traits.Clear();
            t = Traits.Of(s);
            _traits[key] = t;
            TraitsMade++;
            return t;
        }

        public int Count
        {
            get { return _traits.Count; }
        }

        /// Everything forgotten (an older save loaded, the clock stepped back, a new save).
        public void Clear()
        {
            _traits.Clear();
            Generation++;
            Cleared++;
        }
    }
}
