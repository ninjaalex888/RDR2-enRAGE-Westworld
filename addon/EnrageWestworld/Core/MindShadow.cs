// The NPC mind in shadow mode (daytime run 2026-10-27): what the mind works out for people near the player, kept in
// memory only (never saved), capped, and forgotten when an older save is loaded (Engine.ForgetThinking, from the
// store's rewind) or a new one is. It reads only people who already have an identity; it never makes anyone, rolls the
// engine's Random or writes to the store. With [Mind] MindLive off, nothing it works out reaches the screen.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    /// One person's beliefs in the shadow: what has moved them and which held at the last look (night 2026-10-28, item 6).
    public sealed class BeliefState
    {
        public readonly float[] Moved = new float[Beliefs.Count];
        public int Held;        // bits (Beliefs.HeldBits) at the last look
        public bool Seen;       // looked at once (the first look sets Held and can't flip anything)
    }

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
            list.Add(new FeltAt { Kind = k, Intensity = f.Intensity, At = now, About = f.About, AboutId = f.AboutId, DoerId = f.DoerId, Masked = f.Shows == "masked" || f.Shows == "turned", Urge = f.Urge });
            FeelingsNoted++;
            if (OnFeel != null)
                OnFeel(id);
        }

        /// The feeling with most left in it now and how much (Mood.Lasting), for the shadow's choosing; false for none.
        public bool Strongest(string id, long now, out FeltAt best, out float left)
        {
            best = default(FeltAt);
            left = 0f;
            List<FeltAt> list = Felt(id);
            if (list == null)
                return false;
            foreach (FeltAt f in list)
            {
                float l = Mood.Lasting(f, now);
                if (l > left)
                {
                    left = l;
                    best = f;
                }
            }
            return left > 0f;
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

        // ---- beliefs (step 10, night 2026-10-28) ----------------------------------------------------------

        // id -> what appraisals have moved each belief by (deltas on Beliefs.Baseline) and which held at the last look, in memory only
        private readonly Dictionary<string, BeliefState> _beliefs = new Dictionary<string, BeliefState>();
        private readonly Queue<string> _beliefOrder = new Queue<string>();
        public int BeliefsMoved { get; private set; }

        /// This person's belief state (made on first ask; capped at Cap people, the first to believe going first).
        public BeliefState BeliefsOf(string id)
        {
            BeliefState d;
            if (id == null)
                return new BeliefState();
            if (_beliefs.TryGetValue(id, out d))
                return d;
            if (_beliefs.Count >= Cap && _beliefOrder.Count > 0)
                _beliefs.Remove(_beliefOrder.Dequeue());
            _beliefs[id] = d = new BeliefState();
            _beliefOrder.Enqueue(id);
            return d;
        }

        /// A feeling moves what they believe (Beliefs.Move), in memory; true when anything moved.
        public bool Believe(string id, Feeling f, AppraisalEvent e, Traits t, float[] baseline)
        {
            if (id == null || f == null || f.Kind == "none" || e == null)
                return false;
            if (!Beliefs.Move(BeliefsOf(id).Moved, f, e, t, baseline))
                return false;
            BeliefsMoved++;
            return true;
        }

        public int PeopleBelieving
        {
            get { return _beliefs.Count; }
        }

        /// Everything forgotten (an older save loaded, the clock stepped back, a new save).
        public void Clear()
        {
            _traits.Clear();
            _felt.Clear();
            _feltOrder.Clear();
            _events.Clear();
            _beliefs.Clear();
            _beliefOrder.Clear();
            Generation++;
            Cleared++;
        }
    }
}
