// Minds (Minds.cs) for the engine: memories written when something happens, and a thought chosen by what pulls
// hardest on this person now. Memories are kept only for people who can be met again (Lasting: enRAGE's townsfolk,
// saved residents); everyone else thinks from their worry, the moment and the old pools through the same choice.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public sealed partial class Engine
    {
        /// A line for the debug log only (Log.Detail on the game side): what each thought came from.
        public Action<string> Detail = delegate
        {
        };

        // id -> the seed last thought with and what came of it: the ticker and F7 asking again get the same thought,
        // and a memory is worn once
        private readonly Dictionary<string, KeyValuePair<string, Minds.Choice>> _thought = new Dictionary<string, KeyValuePair<string, Minds.Choice>>();
        // the worry's wear (not saved): id -> its arc, times thought, when last
        private readonly Dictionary<string, long[]> _worryWear = new Dictionary<string, long[]>();
        // what each has thought today (not saved): nobody thinks the same line twice in a day; a line already used
        // passes the turn to the next thing on their mind
        private readonly Dictionary<string, KeyValuePair<long, HashSet<string>>> _today = new Dictionary<string, KeyValuePair<long, HashSet<string>>>();

        // ---- remembering ---------------------------------------------------------------------------------

        /// Where someone's memories are kept: only for someone who can be met again (never a bare Store.Get for
        /// someone unsaved, which would leave an entry in the save for good).
        private PersonState MindOf(string id, string name)
        {
            if (!Lasting(id))
                return null;
            PersonState st = Store.Find(id);
            if (st != null)
                return st;
            if (id.StartsWith("folk:"))
                return Store.Get(new Person { Id = id, Name = name });
            Resident r = Store.FindResident(id);
            return r != null ? Store.Get(PersonOfResident(r)) : null;
        }

        /// A scene between a and b (a first, as the scene was built: the one who did it in a one-sided scene): each
        /// remembers being in it, and whoever was watching remembers seeing it.
        public void RememberScene(string kind, Minds.Party a, Minds.Party b, IList<Minds.Party> watchers, string town, string place)
        {
            if (!Cfg.Minds || a == null || b == null)
                return;
            string group = Minds.GroupOf(kind);
            if (group == null)
                return;
            long now = Store.LastGameMin;
            bool oneSided = Minds.OneSided(kind);
            string key = kind + ":" + (a.Id ?? a.Name) + ":" + (b.Id ?? b.Name) + ":" + now / 30;
            RememberDid(a, b, kind, group, oneSided ? "doer" : null, town, place, now, key);
            RememberDid(b, a, kind, group, oneSided ? "done" : null, town, place, now, key);
            if (watchers == null || kind == "lift")
                return;   // nobody saw the lift
            foreach (Minds.Party w in watchers)
                if (w != null && w.Id != null && w.Id != a.Id && w.Id != b.Id)
                    RememberSaw(w, a, b, kind, group, town, place, now, key);
        }

        private void RememberDid(Minds.Party self, Minds.Party other, string kind, string group, string role, string town, string place, long now, string key)
        {
            PersonState st = MindOf(self.Id, self.Name);
            if (st == null)
                return;
            // in a scene they hear each other's names; the mark of a lift never knew who
            bool knows = !(kind == "lift" && role == "done");
            string name = !knows ? null : other.Name != null ? Person.FirstOf(other.Name) : other.Label ?? "somebody";
            MindMemory m = Minds.Note(st.Mind, new MindMemory
            {
                Kind = group, Sub = kind, Who = knows ? new List<string> { other.Id } : null, Names = knows ? new List<string> { name } : null,
                Town = town, Place = place, At = now, How = "did", Role = role,
                Feel = Minds.FeelOf(kind, "did", role), Strength = Minds.StrengthOf(group, kind, "did", role), Key = key
            }, now);
            Detail("MIND " + (self.Name ?? self.Id) + " remembers " + kind + (role != null ? " (" + role + ")" : string.Empty) + " with " + (name ?? "someone unknown") +
                   " (" + m.Strength + ", " + st.Mind.Count + " kept)");
        }

        private void RememberSaw(Minds.Party w, Minds.Party a, Minds.Party b, string kind, string group, string town, string place, long now, string key)
        {
            PersonState st = MindOf(w.Id, w.Name);
            if (st == null)
                return;
            string an = KnownAs(w, a), bn = KnownAs(w, b);
            if (an == bn && (an.StartsWith("a ") || an.StartsWith("an ")))
                bn = "another " + an.Substring(an.IndexOf(' ') + 1);   // two strangers who look alike: a farmhand and another farmhand
            MindMemory m = Minds.Note(st.Mind, new MindMemory
            {
                Kind = group, Sub = kind, Who = new List<string> { a.Id, b.Id }, Names = new List<string> { an, bn },
                Town = town, Place = place, At = now, How = "saw",
                Feel = Minds.FeelOf(kind, "saw", null), Strength = Minds.StrengthOf(group, kind, "saw", null), Key = key
            }, now);
            Detail("MIND " + (w.Name ?? w.Id) + " saw " + kind + " between " + an + " and " + bn + " (" + m.Strength + ", " + st.Mind.Count + " kept)");
        }

        /// How one person knows another: by first name if they're neighbours (the same town) or have a tie, else by how
        /// they looked.
        private string KnownAs(Minds.Party w, Minds.Party x)
        {
            if (x.Name == null)
                return x.Label ?? "somebody";
            bool knows = x.Label == null || (w.Home != null && w.Home == x.Home) ||
                         (x.Id != null && Store.Ties.Any(t => (t.A == w.Id && t.B == x.Id) || (t.A == x.Id && t.B == w.Id)));
            return knows ? Person.FirstOf(x.Name) : x.Label;
        }

        // ---- thinking ------------------------------------------------------------------------------------

        /// What this person is thinking now, by what pulls hardest on them (Minds on): the line and where it came from.
        /// `extra`: what the caller knows besides (a townsperson's feeling about the player, a keeper's counter).
        public Minds.Choice Think(Person p, PersonState st, int hour, string seed, InnerLife.PlayerLook look, List<Minds.Candidate> extra)
        {
            // new code under the ticker: a fault here gives the old way for that thought, and is logged once
            try
            {
                return ThinkImpl(p, st, hour, seed, look, extra);
            }
            catch (Exception ex)
            {
                if (!_thinkFailed)
                    Log("MIND thinking failed for " + p.Name + ", so the old way for now: " + ex);
                _thinkFailed = true;
                return null;
            }
        }

        private bool _thinkFailed;
        private long _thinkClock = -1;   // the latest game minute thought at: earlier means an older save was loaded

        /// An older save was loaded (or the clock stepped back): what was thought after it hasn't happened either.
        internal void ForgetThinking()
        {
            _thought.Clear();
            _today.Clear();
            _worryWear.Clear();
        }

        private Minds.Choice ThinkImpl(Person p, PersonState st, int hour, string seed, InnerLife.PlayerLook look, List<Minds.Candidate> extra)
        {
            long now = Store.LastGameMin;
            if (now < _thinkClock - 5)
                ForgetThinking();
            _thinkClock = now;
            KeyValuePair<string, Minds.Choice> was;
            if (_thought.TryGetValue(p.Id, out was) && was.Key == seed)
                return was.Value;
            InnerLife.Context day = ThoughtDay(p.Home, p.Id);
            List<Minds.Candidate> all = Candidates(p, st, hour, seed, look, day, now);
            if (extra != null)
                all.AddRange(extra);
            Minds.Choice c = null;
            HashSet<string> said = SaidToday(p.Id, now);
            foreach (Minds.Candidate k in Minds.Order(all, seed))
            {
                string text = k.Make();
                if (string.IsNullOrEmpty(text) || said.Contains(text))
                    continue;
                said.Add(text);
                c = new Minds.Choice { Text = text, Source = k.Source, Pull = (int)Math.Round(k.Pull) };
                if (k.M != null)
                {
                    k.M.Times++;
                    k.M.LastAt = now;
                }
                else if (k.Source.StartsWith("worry:"))
                    WearWorry(p.Id, day.GameDay, now);
                break;
            }
            if (_thought.Count > 3000)
                _thought.Clear();
            _thought[p.Id] = new KeyValuePair<string, Minds.Choice>(seed, c);
            if (c != null)
                Detail("THOUGHT " + p.Name + ": \"" + c.Text + "\" <- " + c.Source + " (" + c.Pull + ")");
            return c;
        }

        /// Everything that might be on their mind now, with its pull; nothing is worn or made (for the F7 dump too).
        public List<Minds.Candidate> Candidates(Person p, PersonState st, int hour, string seed, InnerLife.PlayerLook look, InnerLife.Context day, long now)
        {
            var c = new List<Minds.Candidate>();
            List<string> told = st != null ? st.Musings : null;   // the old pools' repeats; a memory's are its own
            // what they lived through
            if (st != null && st.Mind != null)
                foreach (MindMemory m in st.Mind)
                {
                    MindMemory mm = m;
                    // saved from an execution: its own words (Executions.After), for the three days they last
                    Func<string> make = m.Kind == "rescued" ? (Func<string>)(() => AfterRescue(st, seed + "|" + mm.Times)) : () => Minds.Line(mm, p, now, day, seed);
                    c.Add(new Minds.Candidate
                    {
                        Source = "memory:" + m.Sub + "/" + m.How + (m.How == "did" && m.Role != null ? ":" + m.Role : string.Empty),
                        Pull = Minds.Pull(m, now, day, p.Personality), M = m, Make = make
                    });
                }
            // the worry they're living through
            string wk;
            int ws;
            bool wg;
            if (day.GameDay >= 0 && InnerLife.WorryNow(p, day.GameDay, out wk, out ws, out wg))
            {
                long[] wear = WorryWearOf(p.Id, day.GameDay);
                double pull = Minds.WorryPull(ws) - 3 * wear[1] - (wear[1] > 0 && now - wear[2] >= 0 && now - wear[2] < 90 ? 40 : 0);
                c.Add(new Minds.Candidate { Source = "worry:" + wk, Pull = pull, Make = () => InnerLife.WorryThought(p, day.GameDay, seed, told) });
            }
            // a tie from before they kept memories of it, and the other end of someone's worry
            if (day.Ties != null && day.Ties.Count > 0)
            {
                SceneTie t = day.Ties[0];
                string other = t.OtherId(p.Id);
                bool dead = day.IsDead(other);
                bool remembered = st != null && st.Mind.Any(m => m.How == "did" && m.Who != null && m.Who.Contains(other));
                if (!remembered || dead)
                    c.Add(new Minds.Candidate
                    {
                        Source = dead ? "tie:dead" : "tie", Pull = dead ? 45 : 30 * Math.Pow(0.5, Math.Max(0, now - t.At) / 1440.0),
                        Make = () => Ties.Thought(t, p.Id, seed, dead)
                    });
            }
            if (day.Linked != null && day.Linked.Count > 0)
            {
                WorryLink l = day.Linked[Voice.Hash(seed + "lw") % day.Linked.Count];
                c.Add(new Minds.Candidate { Source = "linked:" + l.Kind, Pull = 26, Make = () => Links.OtherThought(l, day.GameDay, seed) });
            }
            // what the town is saying
            if (day.TownTies != null && day.TownTies.Count > 0)
            {
                SceneTie tt = day.TownTies[Voice.Hash(seed + "twp") % Math.Min(3, day.TownTies.Count)];
                c.Add(new Minds.Candidate
                {
                    Source = "heard:" + tt.Kind, Pull = 22 * Math.Pow(0.5, Math.Max(0, now - tt.At) / 1440.0),
                    Make = () => Ties.TownThought(tt, p.Home, seed, day.IsDead(tt.A), day.IsDead(tt.B))
                });
            }
            if (day.TownDeeds != null && day.TownDeeds.Count > 0)
            {
                Deed d = day.TownDeeds[0];
                c.Add(new Minds.Candidate
                {
                    Source = "heard:" + d.Kind, Pull = 24 * Math.Pow(0.5, Math.Max(0, now - d.At) / 1440.0),
                    Make = () => InnerLife.TownTalkLine(p, day.TownDeeds, seed, told)
                });
            }
            // what is around them now (a scene they kept a memory of is thought of through the memory)
            foreach (KeyValuePair<string, Func<string>> part in InnerLife.NowParts(p, seed, told, day))
            {
                double pull;
                switch (part.Key)
                {
                    case "seen":
                        InnerLife.Witnessed s = day.JustSaw;
                        if (st != null && s.AId != null && s.BId != null &&
                            st.Mind.Any(m => m.How == "saw" && m.Who != null && m.Who.Contains(s.AId) && m.Who.Contains(s.BId) && Math.Abs(m.At - s.At) <= 60))
                            continue;
                        pull = 45 - Math.Max(0, now - s.At) / 2.0;
                        break;
                    case "nearby": pull = 18; break;
                    case "mood": pull = 21; break;
                    case "weather": pull = day.Weather == "rain" || day.Weather == "storm" || day.Weather == "snow" || day.Weather == "dust" ? 22 : 15; break;
                    case "place": pull = 17; break;
                    default: pull = 12; break;
                }
                c.Add(new Minds.Candidate { Source = "now:" + part.Key, Pull = pull + Minds.Jitter(seed, part.Key, 4), Make = part.Value });
            }
            // the player, close by
            if (look != null && look.Near)
            {
                bool again = look.Seen >= 2;
                c.Add(new Minds.Candidate { Source = again ? "player:again" : "player:look", Pull = (again ? 34 : 28) + Minds.Jitter(seed, "player", 6), Make = () => InnerLife.AboutPlayer(p, seed, told, look, again) });
            }
            // the day, the year, and the old timeless frames: low, taking turns
            c.Add(new Minds.Candidate { Source = "day", Pull = 16 + Minds.Jitter(seed, "day", 8), Make = () => InnerLife.DayLine(p, hour, seed, told) });
            c.Add(new Minds.Candidate { Source = "era", Pull = 10 + Minds.Jitter(seed, "era", 8), Make = () => InnerLife.EraLine(p, seed, told) });
            int roll = 58 + Voice.Hash(seed + "ch") % 42;
            string pool = InnerLife.FillerPool(p, roll);
            c.Add(new Minds.Candidate { Source = "filler:" + pool, Pull = 14 + Minds.Jitter(seed, "filler", 10), Make = () => { string ignored; return InnerLife.Filler(p, hour, seed, told, roll, out ignored); } });
            return c;
        }

        private HashSet<string> SaidToday(string id, long now)
        {
            long d = now / 1440;
            KeyValuePair<long, HashSet<string>> t;
            if (!_today.TryGetValue(id, out t) || t.Key != d)
            {
                if (_today.Count > 3000)
                    _today.Clear();
                _today[id] = t = new KeyValuePair<long, HashSet<string>>(d, new HashSet<string>());
            }
            return t.Value;
        }

        private long[] WorryWearOf(string id, long gameDay)
        {
            long arc = InnerLife.ArcFor(id, gameDay);
            long[] w;
            if (!_worryWear.TryGetValue(id, out w) || w[0] != arc)
                _worryWear[id] = w = new[] { arc, 0L, 0L };
            return w;
        }

        private void WearWorry(string id, long gameDay, long now)
        {
            long[] w = WorryWearOf(id, gameDay);
            w[1]++;
            w[2] = now;
        }

        /// For the F7 dump: what is on this person's mind now, strongest first, nothing worn.
        public string MindStatus(Person p, int hour, string seed, InnerLife.PlayerLook look)
        {
            PersonState st = Store.Find(p.Id);
            long now = Store.LastGameMin;
            InnerLife.Context day = ThoughtDay(p.Home, p.Id);
            List<Minds.Candidate> all = Candidates(p, st, hour, seed, look, day, now);
            return p.Name + " (" + (p.Personality ?? "plain") + ", " + (st != null ? st.Mind.Count : 0) + " memories): " +
                   string.Join("; ", all.OrderByDescending(k => k.Pull).Take(5).Select(k => k.Source + " " + (int)Math.Round(k.Pull)));
        }
    }
}
