// The NPC mind in shadow mode (daytime run 2026-10-27): what the mind works out for people near the player, kept in
// memory only (never saved), capped, and forgotten when an older save is loaded (Engine.ForgetThinking, from the
// store's rewind) or a new one is. It reads only people who already have an identity; it never makes anyone, rolls the
// engine's Random or writes to the store. With [Mind] MindLive off, nothing it works out reaches the screen.
using System;
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

        // ---- feelings and mood (step 4) ------------------------------------------------------------------

        public const int FeltPerPerson = 8, EventQueueCap = 32;
        // id -> the feelings they had lately (the newest last), for their mood
        private readonly Dictionary<string, List<FeltAt>> _felt = new Dictionary<string, List<FeltAt>>();
        private readonly Queue<string> _feltOrder = new Queue<string>();
        // what happened lately, waiting for the mind's own step to appraise it (filled by a hook in Engine.NoteEvent)
        private readonly Queue<Happening> _events = new Queue<Happening>();
        public int EventsQueued { get; private set; }
        public int EventsDropped { get; private set; }
        public int FeelingsNoted { get; private set; }

        /// A happening for the mind to appraise; capped (the oldest goes). Never throws.
        public void Queue(Happening e)
        {
            if (e == null)
                return;
            if (_events.Count >= EventQueueCap)
            {
                _events.Dequeue();
                EventsDropped++;
            }
            _events.Enqueue(e);
            EventsQueued++;
        }

        /// The next happening to appraise, or null.
        public Happening Next()
        {
            return _events.Count > 0 ? _events.Dequeue() : null;
        }

        public int Waiting
        {
            get { return _events.Count; }
        }

        /// Told of each feeling kept, by id (the Engine drops that person's cached thought, so it can come first at once).
        public Action<string> OnFeel;

        /// A feeling had now, kept for their mood (at most FeltPerPerson a person, 600 people).
        public void Feel(string id, Feeling f, long now)
        {
            if (id == null || f == null || f.Kind == "none")
                return;
            int k = Array.IndexOf(Feeling.Kinds, f.Kind);
            if (k < 0)
                return;
            List<FeltAt> list;
            if (!_felt.TryGetValue(id, out list))
            {
                if (_felt.Count >= Cap && _feltOrder.Count > 0)
                    _felt.Remove(_feltOrder.Dequeue());   // the person who first felt something goes, not everyone
                _felt[id] = list = new List<FeltAt>(FeltPerPerson);
                _feltOrder.Enqueue(id);
            }
            if (list.Count >= FeltPerPerson)
            {
                // the feeling with the least left in it goes (a grief outweighs the week's slights)
                int least = 0;
                for (int i = 1; i < list.Count; i++)
                    if (Mood.Weight(list[i], now) < Mood.Weight(list[least], now))
                        least = i;
                list.RemoveAt(least);
            }
            list.Add(new FeltAt { Kind = k, Intensity = f.Intensity, At = now, About = f.About, AboutId = f.AboutId, DoerId = f.DoerId });
            FeelingsNoted++;
            if (OnFeel != null)
                OnFeel(id);
        }

        /// What they felt lately (null: nothing).
        public List<FeltAt> Felt(string id)
        {
            List<FeltAt> list;
            return id != null && _felt.TryGetValue(id, out list) ? list : null;
        }

        /// Their mood now, from their traits and what they felt lately.
        public PadMood MoodOf(string id, Traits t, long now)
        {
            return Mood.Now(Mood.Baseline(t), Felt(id), now);
        }

        public int PeopleFeeling
        {
            get { return _felt.Count; }
        }

        /// Everything forgotten (an older save loaded, the clock stepped back, a new save).
        public void Clear()
        {
            _traits.Clear();
            _felt.Clear();
            _feltOrder.Clear();
            _events.Clear();
            Generation++;
            Cleared++;
        }
    }
}
