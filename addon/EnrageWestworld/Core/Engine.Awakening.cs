// The Awakening, run once a game day: a share of the living is quietly chosen to wake (leaning toward the
// most aware), and each woken person moves through the stages as days pass. See Core/Awakening.cs.
// It covers enRAGE's people and the residents (everyone else, see Residents.cs): a resident rolls the
// share once, when they are first met.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public sealed partial class Engine
    {
        private readonly Dictionary<string, Person> _resPeople = new Dictionary<string, Person>();

        /// How many of the living have begun to wake (any stage), enRAGE's people and residents together.
        public int WakingCount()
        {
            return Store.People.Values.Count(s => s.WakeStage > 0 && !Gone(s.Id));
        }

        /// Dead, or a story character: not part of the Awakening any more.
        private bool Gone(string id)
        {
            Person p;
            if (_byId.TryGetValue(id, out p))
                return !p.IsAlive || p.Lore;
            Resident r = Store.FindResident(id);
            return r != null && r.Dead;
        }

        /// The Person for anyone the add-on keeps a state for: an enRAGE person, a resident, or an enRAGE townsperson.
        private Person PersonForState(PersonState st)
        {
            Person p;
            if (_byId.TryGetValue(st.Id, out p))
                return p;
            Resident r = Store.FindResident(st.Id);
            return r != null ? PersonOfResident(r) : new Person { Id = st.Id, Name = st.Name ?? "someone" };
        }

        /// enRAGE's named townsfolk are known by the add-on as "folk:" ids. The share who start already stirring is
        /// rolled once, the first time they are looked at.
        public Person PersonOfFolk(string key, string name, bool woman, string town, long now)
        {
            Person p = new Person { Id = key, Name = name, Woman = woman, Home = town };
            PersonState st = Store.Get(p);
            if (!st.WakeRolled)
            {
                st.WakeRolled = true;
                if (Cfg.AwakeningShare > 0 && _rng.NextDouble() * 100.0 < Cfg.AwakeningShare)
                    Advance(p, st, now);
            }
            return p;
        }

        public int StageOf(Person p)
        {
            return StageOf(p.Id);
        }

        public int StageOf(string id)
        {
            PersonState st = Store.Find(id);
            return st != null ? st.WakeStage : 0;
        }

        // ---- residents ------------------------------------------------------------------------------

        /// The plain Person for a resident (kept so the same object is used every time).
        public Person PersonOfResident(Resident r)
        {
            Person p;
            if (!_resPeople.TryGetValue(r.Id, out p) || p.Name != r.Name)
                _resPeople[r.Id] = p = Residents.ToPerson(r);
            p.IsAlive = !r.Dead;
            return p;
        }

        /// Drops the cached Person for a resident whose facts changed under the same name (the test cast's Fit sets the
        /// age, faith, temper and trade after MeetResident may already have cached them: the night review, 2026-10-28).
        public void ForgetPerson(string id)
        {
            if (id != null)
                _resPeople.Remove(id);
        }

        /// Makes a resident for an ordinary person who has just been asked about. Not saved until something happens
        /// to them, except that the share of residents who start already waking is rolled here, and those are kept.
        public Resident MeetResident(uint model, bool woman, string town, string trade, string seed, long now)
        {
            Resident r = Residents.Make(Resident.Prefix + (++Store.NextResident), seed, model, woman, town, trade, now, Voice.NowYear);
            if (Cfg.AwakeningShare > 0 && _rng.NextDouble() * 100.0 < Cfg.AwakeningShare)
            {
                Advance(PersonOfResident(r), Store.Get(PersonOfResident(r)), now);
                Store.KeepResident(r, now);
            }
            return r;
        }

        /// Something happened to this resident (they were talked to, woke a stage...): they are saved from now on.
        public void KeepResident(Resident r, long now)
        {
            Store.KeepResident(r, now);
        }

        // ---- the daily tick -----------------------------------------------------------------------------

        private void AwakeningTick(long now)
        {
            if (!Cfg.WestWorld)
                return;
            long perStage = Math.Max(1, Cfg.AwakeningDays) * 1440L;
            List<Person> living = _byId.Values.Where(p => p.IsAlive && !p.Lore).ToList();
            foreach (Person p in living)
                AdvanceIfDue(p, now, perStage);
            // everyone who has begun to wake, whoever they are (enRAGE people already advanced above)
            foreach (PersonState st in Store.People.Values.Where(s => s.WakeStage >= Awakening.Stirring && s.WakeStage < Awakening.Awake && !Gone(s.Id) && !_byId.ContainsKey(s.Id)).ToList())
                AdvanceIfDue(PersonForState(st), now, perStage);
            if (living.Count == 0 || Cfg.AwakeningShare <= 0)
                return;   // no one new wakes; those already waking carry on
            int target = (int)Math.Ceiling(living.Count * Cfg.AwakeningShare / 100.0);
            int current = living.Count(p => StageOf(p) > 0);
            while (current < target)
            {
                // quietly chosen, leaning toward those who already suspect the most
                Person pick = living.Where(p => StageOf(p) == 0)
                    .OrderByDescending(p => Hosts.Awareness(p, Store.Find(p.Id)) + _rng.Next(25) + WakeLean(p.Id)).FirstOrDefault();   // with MindWakes, what shook them leans it (Engine.Doubt.cs; 0 otherwise)
                if (pick == null)
                    break;
                Advance(pick, Store.Get(pick), now);
                current++;
            }
        }

        private void AdvanceIfDue(Person p, long now, long perStage)
        {
            PersonState st = Store.Find(p.Id);
            if (st != null && st.WakeStage >= Awakening.Stirring && st.WakeStage < Awakening.Awake && now - st.WakeStageAt >= perStage && _rng.Next(100) < WakeChance(p.Id))   // 60 as before; with MindWakes, from 30 to 95 by what shook or steadied them (Engine.Doubt.cs)
                Advance(p, st, now);
        }

        /// Wakes someone a stage further (the first stage if still asleep). Returns the new stage.
        public int Advance(Person p, PersonState st, long now)
        {
            if (st.WakeStage >= Awakening.Awake)
                return st.WakeStage;
            st.WakeStage++;
            st.WakeStageAt = now;
            Log("AWAKENING " + p.Name + (Resident.IsResident(p.Id) ? " (a resident" + (string.IsNullOrEmpty(p.Home) ? "" : " of " + p.Home) + ")" : "") + ": stage " + st.WakeStage + " (" + Awakening.StageName(st.WakeStage) + ")");
            return st.WakeStage;
        }

        /// For testing: wakes the person a stage further right now.
        public int Awaken(Person p, long now)
        {
            return Advance(p, Store.Get(p), now);
        }

        /// The first name of the living one they're closest to: the warmest tie (warmth 2 or more, or a courtship) of the last 60 game
        /// days, the newest of equals; null when there's nobody (despair's last thoughts, DespairWords).
        public string Dearest(string id)
        {
            if (id == null || Store.Ties == null)
                return null;
            long now = Store.LastGameMin;
            SceneTie t = Store.Ties.Where(x => x.Has(id) && (x.Warmth >= 2 || Ties.Sweethearts(x)) && now - x.At >= 0 && now - x.At < 60 * 1440L && !HasDied(x.A == id ? x.B : x.A))
                                   .OrderByDescending(x => x.Warmth).ThenByDescending(x => x.At).FirstOrDefault();
            if (t == null)
                return null;
            string name = Person.FirstOf(t.A == id ? t.BName : t.AName);
            return string.IsNullOrEmpty(name) ? null : name;
        }
    }
}
