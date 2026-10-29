// Minds (Minds.cs) for the engine: memories written when something happens, and a thought chosen by what pulls
// hardest on this person now. Memories are kept only for people who can be met again (Lasting: enRAGE's townsfolk,
// saved residents); everyone else thinks from their worry, the moment and the old pools through the same choice.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

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
        // enRAGE's dossier for its records, worn by use (not saved): id|source|day -> times it won
        private readonly Dictionary<string, int> _dossierWear = new Dictionary<string, int>();
        // what each has thought today (not saved): nobody thinks the same line twice in a day; a line already used
        // passes the turn to the next thing on their mind
        private readonly Dictionary<string, KeyValuePair<long, HashSet<string>>> _today = new Dictionary<string, KeyValuePair<long, HashSet<string>>>();

        // ---- what happened, and who saw it (WorldEvents.cs) ---------------------------------------------------

        /// Writes down an event (its time is the store's clock), logged in detail.
        public Happening NoteEvent(Happening e)
        {
            if (e == null)
                return null;
            e.At = Store.LastGameMin;
            WorldEvents.Note(Store.WorldLog, e);
            Detail("EVENT " + WorldEvents.Describe(e));
            if (Cfg.MindLive)
            {
                try
                {
                    FillSpots(e);   // the memories written of it a moment ago get its spot (Minds.Memory.cs; a fault here never costs the event)
                }
                catch (Exception ex)
                {
                    if (!_spotFailed)
                        Log("MIND placing a memory failed: " + ex.Message);
                    _spotFailed = true;
                }
            }
            if (Cfg.MindShadow)
            {
                try
                {
                    Shadow.Queue(e);   // the mind's own step appraises it (MindShadow.cs, WestworldScript.Mind.cs)
                }
                catch
                {
                    // a fault in the mind never costs the event
                }
            }
            return e;
        }

        // ---- remembering ---------------------------------------------------------------------------------

        /// Where someone's memories are kept: only for someone who can be met again (never a bare Store.Get for
        /// someone unsaved, which would leave an entry in the save for good).
        private PersonState MindOf(string id, string name)
        {
            if (!CanRemember(id))
                return null;
            Person rec;
            if (id != null && _byId.TryGetValue(id, out rec))
                return Store.Get(rec);   // one of enRAGE's records: its state is kept already
            PersonState st = Store.Find(id);
            if (st != null)
                return st;
            if (id.StartsWith("folk:"))
                return Store.Get(new Person { Id = id, Name = name });
            Resident r = Store.FindResident(id);
            return r != null ? Store.Get(PersonOfResident(r)) : null;
        }

        /// Who keeps memories: enRAGE's townsfolk, saved residents, and enRAGE's own records.
        public bool CanRemember(string id)
        {
            return id != null && (Lasting(id) || _byId.ContainsKey(id));
        }

        /// One of enRAGE's records: a memory that pulls hard enough (over 40) is what they think of on the ticker,
        /// after a reverie and the world feeling unreal (TickerThought). Null when nothing does.
        public string RecordMemoryThought(Person p, PersonState st, string seed)
        {
            if (!Cfg.Minds || st == null || st.Mind.Count == 0)
                return null;
            int hour = (int)(Store.LastGameMin % 1440 / 60);
            Minds.Choice c = Think(p, st, hour, seed, null, null, memoriesOver: 40);
            return c != null ? c.Text : null;
        }

        /// The first name of a friend whose death this person has had word of within the last `minutes` (a "heard"
        /// friend's death come due), or null.
        public string GriefFor(string id, long minutes)
        {
            long at;
            return GriefFor(id, minutes, out at);
        }

        /// The same, with the game minute the word reached them.
        public string GriefFor(string id, long minutes, out long at)
        {
            string deadId;
            return GriefFor(id, minutes, out at, out deadId);
        }

        /// Whether word of this death has reached this person (a death memory of them come due, heard or seen).
        public bool HeardOfDeath(string id, string deadId)
        {
            PersonState st = id != null && deadId != null ? Store.Find(id) : null;
            if (st == null || st.Mind == null)
                return false;
            long now = Store.LastGameMin;
            foreach (MindMemory m in st.Mind)
                if (m.Kind == "death" && m.Who != null && m.Who.Contains(deadId) && Math.Max(m.At, m.DueAt) <= now)
                    return true;
            return false;
        }

        /// The same, with the dead one's id.
        public string GriefFor(string id, long minutes, out long at, out string deadId)
        {
            at = 0;
            deadId = null;
            PersonState st = id != null ? Store.Find(id) : null;
            if (st == null || st.Mind == null)
                return null;
            long now = Store.LastGameMin;
            MindMemory m = st.Mind.Where(x => x.Kind == "death" && x.Sub == "friend" && Math.Max(x.At, x.DueAt) <= now && now - Math.Max(x.At, x.DueAt) < minutes)
                                  .OrderByDescending(x => Math.Max(x.At, x.DueAt)).FirstOrDefault();
            if (m != null)
            {
                at = Math.Max(m.At, m.DueAt);
                deadId = m.Who != null && m.Who.Count > 0 ? m.Who[0] : null;
            }
            return m != null ? m.NameAt(0) : null;
        }

        /// The first name of a friend of this person's whom the player killed (our own death record says "by the player"),
        /// whose death they've had word of in the last `days`, and not yet held against him to his face; with the dead
        /// one's id. Null if none.
        public string YourKillGrieved(string id, int days, out string deadId)
        {
            deadId = null;
            PersonState st = id != null ? Store.Find(id) : null;
            if (st == null || st.Mind == null)
                return null;
            long now = Store.LastGameMin;
            foreach (MindMemory m in st.Mind.Where(x => x.Kind == "death" && x.Sub == "friend" && x.Who != null && x.Who.Count > 0 && Math.Max(x.At, x.DueAt) <= now &&
                                                        now - x.At < days * 1440L).OrderByDescending(x => x.At))
            {
                DeathEntry d = Store.DeathOf(m.Who[0]);
                if (d == null || d.By != "player" || st.Has("grudge:" + m.Who[0]))
                    continue;
                // they can only hold it against him if they know it was him: they saw him do violence lately, or the
                // town's talk of that killing has started and names nobody unseen (Knowing.cs); and they know his face
                bool saw = st.Mind.Any(x => x.Kind == "deed:violence" && Minds.AboutYou(x) && x.From == null && now - x.At >= 0 && now - x.At < days * 1440L);
                bool told = Store.Deeds.Any(x => x.VictimId == m.Who[0] && x.Known);
                if (!(saw || told) || !(Knowing.Met(st) || saw))
                    continue;
                deadId = m.Who[0];
                return m.NameAt(0);
            }
            return null;
        }

        /// Whether this person has already had it out over this (a mark; saved).
        public bool Grudged(string id, string what)
        {
            PersonState st = id != null ? Store.Find(id) : null;
            return st != null && st.Has("grudge:" + what);
        }

        /// They've said it to his face (or stared it): never again for that death.
        public void MarkGrudged(string id, string deadId)
        {
            PersonState st = id != null ? Store.Find(id) : null;
            if (st != null && deadId != null)
                st.Mark("grudge:" + deadId);
        }

        /// This person remembers seeing the player do violence in the last `days` game days (a killing, an assault).
        public bool SawYouViolent(string id, int days)   // (a deed only told of doesn't count: From must be null)
        {
            PersonState st = id != null ? Store.Find(id) : null;
            if (st == null || st.Mind == null)
                return false;
            long now = Store.LastGameMin;
            return st.Mind.Any(m => m.Kind == "deed:violence" && Minds.AboutYou(m) && m.From == null && now - m.At >= 0 && now - m.At < days * 1440L);
        }

        /// One of enRAGE's records on the ticker (Minds and RecordMinds on): our memories of what they lived through,
        /// weighed with enRAGE's own record of them: what they feel about the player (its memories of him, raised by
        /// hate and fear), the deeds of his they know, what they've heard, a debt, their goal, a first sight of him, their
        /// mood. Whatever pulls hardest is the thought, so it agrees with enRAGE's dossier. Null: the old way.
        public string RecordThought(Person p, PersonState st, PlayerRef you, string seed, long now)
        {
            if (!Cfg.Minds || !Cfg.RecordMinds || p == null || st == null)
                return null;
            var extra = new List<Minds.Candidate>();
            Func<string, double> j = src => Minds.Jitter(seed, src, 6);
            if (Cfg.Romance && st.Heart != 0)
                extra.Add(new Minds.Candidate { Source = "enrage:heart", Pull = 40 + j("heart"), Make = () => Romance.Thought(p, st.Heart, you, seed) });
            MemoryItem m = p.Memories.Where(x => x.AboutYou && x.Intensity >= 30).OrderByDescending(x => x.Intensity + (now - x.At < 1440 ? 25 : 0)).FirstOrDefault();
            if (m != null)
                extra.Add(new Minds.Candidate { Source = "enrage:memory", Pull = 18 + m.Intensity / 2 + (now - m.At < 1440 ? 15 : 0) + Math.Max(p.Hate, p.Fear) / 6 + j("mem"), Make = () => Voice.MemoryThought(p, m, you, seed) });
            Deed d = st.KnownDeeds.Count > 0 ? Store.Deeds.LastOrDefault(x => st.KnownDeeds.Contains(x.Id)) : null;
            if (d != null)
                extra.Add(new Minds.Candidate { Source = "enrage:deed", Pull = 32 + j("deed"), Make = () => Deeds.Thought(p, d, Voice.YouPrivate(p, you, true), seed) });
            Hearsay h = st.Heard.LastOrDefault();
            if (h != null)
                extra.Add(new Minds.Candidate { Source = "enrage:heard", Pull = 26 + j("heard"), Make = () => Voice.HearsayThought(p, Person.FirstOf(h.FromName), h.Text, h.FromWoman, you, seed) });
            Debt debt = Store.Debts.FirstOrDefault(x => !x.Settled && (x.FromId == p.Id || x.ToId == p.Id));
            if (debt != null)
                extra.Add(new Minds.Candidate { Source = "enrage:debt", Pull = 28 + j("debt"), Make = () => DebtThought(debt, p) });
            GoalItem g = p.Goals.FirstOrDefault();
            if (g != null)
                extra.Add(new Minds.Candidate { Source = "enrage:goal:" + g.Type, Pull = (g.Type == "Avenge" && g.Target == "you" && p.Hate >= 40 ? 42 : 22) + j("goal"), Make = () => Voice.GoalLine(p, g, seed) });
            if (st.Talks == 0 && st.Sightings > 0 && p.Hate == 0 && st.KnownDeeds.Count == 0 && !p.Memories.Any(x => x.AboutYou))
                extra.Add(new Minds.Candidate { Source = "enrage:first sight", Pull = 22 + j("first"), Make = () => FirstSight(you, seed) });
            extra.Add(new Minds.Candidate { Source = "enrage:mood", Pull = 14 + j("mood"), Make = () => Voice.MoodLine(p, p.TopThought, seed) });
            // enRAGE's dossier wears as memories do: each time one of its parts wins today it pulls 6 less, so the same
            // source doesn't hold the ticker for hours (not saved; a new game day starts fresh)
            long day = now / 1440;
            foreach (Minds.Candidate x in extra)
            {
                int worn;
                if (_dossierWear.TryGetValue(p.Id + "|" + x.Source + "|" + day, out worn))
                    x.Pull -= 6 * worn;
            }
            KeyValuePair<string, Minds.Choice> was;
            bool cached = _thought.TryGetValue(p.Id, out was) && was.Key == seed;
            Minds.Choice c = Think(p, st, (int)(now % 1440 / 60), seed, null, extra, -1, true);
            if (c != null && !cached && c.Source != null && c.Source.StartsWith("enrage:"))
            {
                if (_dossierWear.Count > 3000)
                    _dossierWear.Clear();
                string key = p.Id + "|" + c.Source + "|" + day;
                int n;
                _dossierWear.TryGetValue(key, out n);
                _dossierWear[key] = n + 1;
            }
            return c != null ? c.Text : null;
        }

        // ---- memory v1 (night 2026-10-28; Minds.Memory.cs), behind MindLive ---------------------------------

        public int MemoriesTold { get; private set; }     // told on between people who talked
        public int MemoriesToldInPassing { get; private set; }   // of those, told with a word in passing (a nod with a greeting; after item 14)
        public int MemoriesPlaced { get; private set; }   // given the spot they happened at
        public int MemoriesDrifted { get; private set; }  // read as drifted or gone to gist (a thought from one)
        private readonly HashSet<string> _toldSet = new HashSet<string>();   // teller>listener>key: nothing told twice to the same person
        private readonly List<MindMemory> _here = new List<MindMemory>();

        /// The memories of everyone in a happening or who saw it get its spot, when they were written in the same minute.
        private void FillSpots(Happening e)
        {
            if (e == null)
                return;
            if (e.DoerId != null && e.DoerId != "player")
                FillSpotOf(e.DoerId, e);
            if (e.TargetId != null && e.TargetId != "player")
                FillSpotOf(e.TargetId, e);
            if (e.Witnesses != null)
                foreach (EventWitness w in e.Witnesses)
                    if (w.Id != null && w.Id != "player")
                        FillSpotOf(w.Id, e);
        }

        private void FillSpotOf(string id, Happening e)
        {
            PersonState st = Store.Find(id);   // only someone who already has a state: nobody is made for it
            if (st != null && st.Mind != null)
                MemoriesPlaced += Minds.FillSpot(st.Mind, e, id);
        }

        /// After a talk between two people: the thing most worth telling passes from each to the other as a memory heard
        /// from them (Minds.WorthTelling, Minds.Retold). Only between people who already have a state (Store.Find); never
        /// through NoteEvent or the saved events log; nothing told twice to the same person (an in-memory set, capped).
        private void Retell(Minds.Party teller, Minds.Party listener, long now, string how = null)
        {
            if (teller == null || listener == null || teller.Id == null || listener.Id == null || teller.Id == listener.Id)
                return;
            PersonState ts = Store.Find(teller.Id), ls = Store.Find(listener.Id);
            if (ts == null || ls == null || ts.Mind == null || ls.Mind == null)
                return;
            string head = teller.Id + ">" + listener.Id + ">";
            MindMemory m = Minds.WorthTelling(ts.Mind, ls.Mind, listener.Id, now, x => _toldSet.Contains(head + x.Key), id => ListenerKnows(listener.Id, id));
            if (m == null)
                return;
            MindMemory told = Minds.Retold(m, teller.Id, Person.FirstOf(teller.Name ?? "somebody"), now);
            if (how != null)
                told.Strength = Math.Max(10, told.Strength * 7 / 10);   // a word in passing is half-heard: weaker again than a talk's telling
            MindMemory r = Minds.Note(ls.Mind, told, now);
            if (!ls.Mind.Contains(r))
                return;   // it didn't stay (a full mind, and a told thing goes first): nothing was told, as far as anyone will know
            if (_toldSet.Count > 20000)
                _toldSet.Clear();
            _toldSet.Add(head + m.Key);
            m.Told++;
            MemoriesTold++;
            if (how != null)
                MemoriesToldInPassing++;
            Detail("MIND told" + (how != null ? " " + how : string.Empty) + ": " + (teller.Name ?? teller.Id) + " tells " + (listener.Name ?? listener.Id) + " of " + Minds.DescribeMemory(r, now, Person.FirstOf(teller.Name ?? "somebody")));
        }

        /// A word in passing (memory v1's telling at a nod, after item 14; MindLive): the one who greets tells the other
        /// the freshest thing worth telling, one way only (a greeting in passing is no talk: the other only nods back),
        /// by the same rules as a talk's retelling. Nothing with MindLive off.
        public void RememberPassing(Minds.Party speaker, Minds.Party other)
        {
            if (!Cfg.Minds || !Cfg.MindLive || speaker == null || other == null || speaker.Id == null || other.Id == null)
                return;
            try
            {
                // a greeting in passing carries news only some of the time (the review's psychologist: a 1.3 s "Morning" isn't a
                // talk): a warm tie stops for a word one time in two, an acquaintance one in five; and what's heard is half-heard
                long now = Store.LastGameMin;
                SceneTie t = TieBetween(speaker.Id, other.Id, Store.Ties);
                int chance = t != null && t.Warmth >= 2 ? 50 : 20;
                if (!Voice.Chance(speaker.Id + "|" + other.Id + "|passing|" + now / 60, chance))
                    return;
                Retell(speaker, other, now, "in passing");
            }
            catch (Exception ex)
            {
                if (!_passingFailed)
                    Log("MIND the word in passing failed: " + ex);
                _passingFailed = true;
            }
        }

        /// Someone whose name a memory never loses: a tie of this person's, or one of the dead.
        private bool KnownId(string id)
        {
            if (id == null)
                return false;
            foreach (SceneTie t in Store.Ties)
                if (t.Has(id))
                    return true;
            return HasDied(id);
        }

        /// Someone this person already holds against (a hostile tie): their id and first name, or null: the man a told
        /// story's unnamed doer drifts toward.
        private string[] FoeOf(string readerId)
        {
            if (readerId == null)
                return null;
            foreach (SceneTie t in Store.Ties)
                if (t.Warmth < 0 && t.Has(readerId))
                {
                    string other = t.A == readerId ? t.BName : t.AName, otherId = t.OtherId(readerId);
                    if (other == null || otherId == null || HasDied(otherId))
                        continue;   // a dead man isn't named as a fresh doer
                    return new[] { otherId, Person.FirstOf(other) };
                }
            return null;
        }

        /// Whether the listener has a tie with someone: news of them is worth more to the listener.
        private bool ListenerKnows(string listenerId, string id)
        {
            foreach (SceneTie t in Store.Ties)
                if (t.Has(listenerId) && t.Has(id))
                    return true;
            return false;
        }

        /// The teller's first name for a told memory's words.
        private string TellerName(MindMemory m)
        {
            if (m == null || m.From == null)
                return null;
            PersonState st = Store.Find(m.From);
            if (st != null && st.Name != null)
                return Person.FirstOf(st.Name);
            Resident r = Store.FindResident(m.From);
            return r != null && r.Name != null ? Person.FirstOf(r.Name) : null;
        }

        /// A scene between a and b (a first, as the scene was built: the one who did it in a one-sided scene): each
        /// remembers being in it, and whoever was watching remembers seeing it. After a talk (MindLive on), each tells the
        /// other the thing most worth telling (memory v1).
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
            if (Cfg.MindLive && Minds.TalkKind(kind))
            {
                try
                {
                    Retell(a, b, now);
                    Retell(b, a, now);
                }
                catch (Exception ex)
                {
                    if (!_retellFailed)
                        Log("MIND retelling failed: " + ex);
                    _retellFailed = true;
                }
            }
            if (watchers == null || kind == "lift")
                return;   // nobody saw the lift
            foreach (Minds.Party w in watchers)
                if (w != null && w.Id != null && w.Id != a.Id && w.Id != b.Id)
                    RememberSaw(w, a, b, kind, group, town, place, now, key);
        }

        /// One of the player's deeds, seen by those around (`watchers`: in clear sight of him when it happened).
        public void RememberDeed(Deed d, IList<Minds.Party> watchers, string place)
        {
            if (!Cfg.Minds || d == null || watchers == null)
                return;
            string group = Minds.DeedGroup(d.Kind);
            if (group == null)
                return;
            long now = Store.LastGameMin;
            foreach (Minds.Party w in watchers)
            {
                PersonState st = w != null ? MindOf(w.Id, w.Name) : null;
                if (st == null)
                    continue;
                MindMemory m = Minds.Note(st.Mind, new MindMemory
                {
                    Kind = group, Sub = d.Kind, Who = new List<string> { "player" }, Town = d.Town, Place = place, At = now, How = "saw",
                    Feel = Minds.DeedFeel(d.Kind), Strength = Minds.DeedStrength(d.Kind), Key = "deed:" + d.Id
                }, now);
                Detail("MIND " + (w.Name ?? w.Id) + " saw you: " + d.Kind + " (" + m.Strength + ", " + st.Mind.Count + " kept)");
            }
        }

        /// A death RealNPCs noted: whoever had a tie or a worry link with them hears of it an hour or a few later (a
        /// friend grieves for weeks, a foe or someone known for days).
        private void RememberDeathHeard(DeathEntry e)
        {
            if (!Cfg.Minds || e == null || e.Id == null)
                return;
            long now = Store.LastGameMin;
            var told = new Dictionary<string, string>();   // id -> friend, foe, known
            var names = new Dictionary<string, string>();
            foreach (SceneTie t in Store.Ties.Where(t => t.A == e.Id || t.B == e.Id))
            {
                string other = t.OtherId(e.Id);
                if (other != null && !told.ContainsKey(other))
                {
                    told[other] = Ties.Friends(t) ? "friend" : t.Warmth < 0 ? "foe" : "known";   // a friend's death is grieved; a man met once is only known (item 10)
                    names[other] = other == t.A ? t.AName : t.BName;
                }
            }
            foreach (WorryLink l in Store.Links.Where(l => l.Id == e.Id || l.OtherId == e.Id))
            {
                string other = l.Id == e.Id ? l.OtherId : l.Id;
                if (other != null && !told.ContainsKey(other))
                    told[other] = l.Kind == "feud" ? "foe" : l.Kind == "courting" ? "friend" : "known";
            }
            foreach (KeyValuePair<string, string> kv in told)
            {
                if (HasDied(kv.Key))
                    continue;
                string nm;
                PersonState st = MindOf(kv.Key, names.TryGetValue(kv.Key, out nm) ? nm : null);
                if (st == null)
                    continue;
                string sub = kv.Value;
                MindMemory m = Minds.Note(st.Mind, new MindMemory
                {
                    Kind = "death", Sub = sub, How = "heard", Who = new List<string> { e.Id }, Names = new List<string> { Person.FirstOf(e.Name) ?? "somebody" },
                    Town = e.Town, At = now, DueAt = now + 60 + Voice.Hash(kv.Key + "|" + e.Id) % 180,
                    Feel = sub == "friend" ? -3 : sub == "foe" ? 0 : -1, Strength = sub == "friend" ? 72 : sub == "foe" ? 40 : 46, Key = "death:" + e.Id
                }, now);
                Detail("MIND " + (st.Name ?? kv.Key) + " will hear " + e.Name + " has died (" + sub + ", in " + (m.DueAt - now) + " min)");
            }
        }

        /// A death seen by those around (not one of the player's own: those are remembered as his deeds).
        public void RememberDeathSaw(DeathEntry e, IList<Minds.Party> watchers)
        {
            if (!Cfg.Minds || e == null || watchers == null || e.By == "player")
                return;
            long now = Store.LastGameMin;
            var dead = new Minds.Party { Id = e.Id, Name = e.Name, Home = e.Town };
            foreach (Minds.Party w in watchers)
            {
                PersonState st = w != null && w.Id != e.Id ? MindOf(w.Id, w.Name) : null;
                if (st == null)
                    continue;
                string name = KnownAs(w, dead);
                MindMemory m = Minds.Note(st.Mind, new MindMemory
                {
                    Kind = "death", Sub = "seen", How = "saw", Who = new List<string> { e.Id }, Names = new List<string> { name }, Town = e.Town, At = now,
                    Feel = -2, Strength = 70, Key = "death:" + e.Id
                }, now);
                Detail("MIND " + (w.Name ?? w.Id) + " saw " + name + " die (" + m.Strength + ", " + st.Mind.Count + " kept)");
            }
        }

        /// The player flirted with them: they remember it by their answer (Minds on; lasting people only).
        public void RememberFlirt(Person p, string answer, string why)
        {
            if (!Cfg.Minds || p == null || why == "spam" || answer == null)
                return;
            PersonState st = MindOf(p.Id, p.Name);
            if (st == null)
                return;
            long now = Store.LastGameMin;
            string sub = Minds.FlirtSub(answer, why);
            MindMemory m = Minds.Note(st.Mind, new MindMemory
            {
                Kind = "flirted", Sub = sub, How = "did", Who = new List<string> { "player" }, Town = p.Home, At = now,
                Feel = sub == "flirt:yes" ? 2 : sub == "flirt:maybe" ? 1 : sub == "flirt:afraid" ? -2 : -1,
                Strength = sub == "flirt:yes" ? 58 : sub == "flirt:afraid" ? 52 : sub == "flirt:maybe" ? 46 : 42, Key = "flirt:" + now / 60
            }, now);
            Detail("MIND " + p.Name + " remembers your flirting: " + sub + " (" + why + "; " + m.Strength + ", " + st.Mind.Count + " kept)");
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
        public Minds.Choice Think(Person p, PersonState st, int hour, string seed, InnerLife.PlayerLook look, List<Minds.Candidate> extra, int memoriesOver = -1, bool onlyOwn = false)
        {
            // new code under the ticker: a fault here gives the old way for that thought, and is logged once
            try
            {
                return ThinkImpl(p, st, hour, seed, look, extra, memoriesOver, onlyOwn);
            }
            catch (Exception ex)
            {
                if (!_thinkFailed)
                    Log("MIND thinking failed for " + p.Name + ", so the old way for now: " + ex);
                _thinkFailed = true;
                return null;
            }
        }

        private bool _thinkFailed, _retellFailed, _spotFailed, _passingFailed;
        private long _thinkClock = -1;   // the latest game minute thought at: earlier means an older save was loaded

        // Thoughts that vary ([Mind] ThoughtVariety; Lex, 2026-10-29: "a lot of the same types of thoughts": in his log the
        // sameness was the same frame worn by different people, "Next word from X gets answered with my hands" four times
        // over, and worries 42% of everything shown). Two rules at the one place every thought passes: no two people think
        // the same frame (the line with its names stripped) within an hour, whoever thinks it; and a source that gave the
        // last few thoughts pulls less, so the weather, the place, the trade and the small day get their turn. In memory,
        // capped, cleared with the rest; off, exactly the old choice.
        public const int FrameMinutes = 60, SpreadWindow = 8;
        public const double SpreadPerHit = 0.35;   // each of the last eight thoughts from the same source divides its pull by (1 + this)
        private readonly Dictionary<string, long> _recentFrames = new Dictionary<string, long>();
        private readonly List<KeyValuePair<string, long>> _recentSources = new List<KeyValuePair<string, long>>();
        public int FramesHeld, SourcesSpread;   // for F7: frames held back within the hour, choices where a source's pull was spread
        private static readonly Regex NameWord = new Regex(@"\b[A-Z][a-z']+\b");

        /// A thought's frame: the line with every name (any capitalised word) as X, lowercased, so "Next word from Abe" and
        /// "Next word from Dalton" are the same frame.
        public static string FrameKey(string text)
        {
            return text == null ? string.Empty : NameWord.Replace(text, "X").ToLowerInvariant();
        }

        /// The source's family: "filler:body" and "filler:hour" are both the filler, "worry:debt" the worries.
        public static string SourceGroup(string source)
        {
            if (string.IsNullOrEmpty(source))
                return string.Empty;
            int i = source.IndexOf(':');
            return i < 0 ? source : source.Substring(0, i);
        }

        private bool FrameHeard(string text, long now)
        {
            long at;
            return _recentFrames.TryGetValue(FrameKey(text), out at) && now - at >= 0 && now - at < FrameMinutes;
        }

        private void NoteShown(string source, string text, long now)
        {
            if (_recentFrames.Count > 2000)
                _recentFrames.Clear();
            _recentFrames[FrameKey(text)] = now;
            _recentSources.Add(new KeyValuePair<string, long>(SourceGroup(source), now));
            while (_recentSources.Count > SpreadWindow)
                _recentSources.RemoveAt(0);
        }

        /// A thought that comes before the Minds' own choice (the world feeling unreal, what one of enRAGE's townsfolk feels
        /// about the player), through the same frame rule: held back (null) when anyone thought its frame within the hour,
        /// else noted as shown. Off, the text as it was.
        private string Varied(string source, string text)
        {
            if (text == null || Cfg == null || !Cfg.ThoughtVariety)
                return text;
            long now = Store.LastGameMin;
            if (FrameHeard(text, now))
            {
                FramesHeld++;
                return null;
            }
            NoteShown(source, text, now);
            return text;
        }

        /// The candidates with each source's pull divided by how many of the last few thoughts (within the hour) came from
        /// its family; the candidates themselves are left as they were.
        private List<Minds.Candidate> Spread(List<Minds.Candidate> all, long now)
        {
            if (_recentSources.Count == 0)
                return all;
            var spread = new List<Minds.Candidate>(all.Count);
            bool any = false;
            foreach (Minds.Candidate k in all)
            {
                if (k == null)
                    continue;
                string g = SourceGroup(k.Source);
                int hits = 0;
                foreach (KeyValuePair<string, long> r in _recentSources)
                    if (r.Key == g && now - r.Value >= 0 && now - r.Value < FrameMinutes)
                        hits++;
                if (hits == 0)
                {
                    spread.Add(k);
                    continue;
                }
                any = true;
                spread.Add(new Minds.Candidate { Source = k.Source, Make = k.Make, M = k.M, Pull = k.Pull / (1.0 + SpreadPerHit * hits) });
            }
            if (any)
                SourcesSpread++;
            return spread;
        }

        /// An older save was loaded (or the clock stepped back): what was thought after it hasn't happened either.
        internal void ForgetThinking()
        {
            _thought.Clear();
            _today.Clear();
            _recentFrames.Clear();
            _recentSources.Clear();
            _worryWear.Clear();
            _dossierWear.Clear();
            _toldSet.Clear();
            Shadow.Clear();
        }

        /// The mind in shadow mode (MindShadow.cs): in memory only, forgotten with the thinking above.
        public readonly MindShadow Shadow = new MindShadow();

        /// memoriesOver: only memories pulling harder than this (for enRAGE's records); -1 everything.
        private Minds.Choice ThinkImpl(Person p, PersonState st, int hour, string seed, InnerLife.PlayerLook look, List<Minds.Candidate> extra, int memoriesOver, bool onlyOwn)
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
            if (onlyOwn)
                all = all.Where(k => k.M != null).ToList();   // a record: our memories and what the caller brings (enRAGE's dossier), nothing else
            if (extra != null)
                all.AddRange(extra);
            if (memoriesOver >= 0)
                all = all.Where(k => k.M != null && k.Pull > memoriesOver).ToList();
            Minds.Choice c = null;
            HashSet<string> said = SaidToday(p.Id, now);
            bool variety = Cfg != null && Cfg.ThoughtVariety;
            List<Minds.Candidate> ordered = Minds.Order(variety ? Spread(all, now) : all, seed);
            for (int pass = 0; pass < 2 && c == null; pass++)
            {
                // the first pass holds back a frame anyone thought within the hour (ThoughtVariety); the second takes it
                // rather than nothing
                foreach (Minds.Candidate k in ordered)
                {
                    string text = k.Make();
                    if (string.IsNullOrEmpty(text) || said.Contains(text))
                        continue;
                    if (pass == 0 && variety && FrameHeard(text, now))
                    {
                        FramesHeld++;
                        continue;
                    }
                    said.Add(text);
                    c = new Minds.Choice { Text = text, Source = k.Source, Pull = (int)Math.Round(k.Pull) };
                    if (k.M != null)
                    {
                        k.M.Times++;
                        k.M.LastAt = now;
                    }
                    else if (k.Source.StartsWith("worry:"))
                        WearWorry(p.Id, day.GameDay, now);
                    if (variety)
                        NoteShown(k.Source, text, now);
                    break;
                }
            }
            if (_thought.Count > 3000)
                _thought.Clear();
            if (c != null || memoriesOver < 0)
                _thought[p.Id] = new KeyValuePair<string, Minds.Choice>(seed, c);   // a record with nothing strong enough thinks enRAGE's way
            if (c != null)
                Detail("THOUGHT " + p.Name + ": \"" + c.Text + "\" <- " + c.Source + " (" + c.Pull + ")");
            return c;
        }

        /// Everything that might be on their mind now, with its pull; nothing is worn or made (for the F7 dump too).
        public List<Minds.Candidate> Candidates(Person p, PersonState st, int hour, string seed, InnerLife.PlayerLook look, InnerLife.Context day, long now)
        {
            var c = new List<Minds.Candidate>();
            List<string> told = st != null ? st.Musings : null;   // the old pools' repeats; a memory's are its own
            if (Cfg.MindLive)
            {
                // the mind's own say (Engine.MindLive.cs): a feeling still felt pulls hardest, a pressing need above the old pools
                float pull;
                KeyValuePair<string, string> mf = MindSay(p, hour, seed, day, now, false, out pull);
                if (mf.Key != null)
                {
                    string mline = mf.Key;
                    c.Add(new Minds.Candidate { Source = mf.Value, Pull = pull, Make = () => mline });
                }
            }
            // what they lived through
            if (st != null && st.Mind != null)
                foreach (MindMemory m in st.Mind)
                {
                    if (m.DueAt > now)
                        continue;   // word of it hasn't reached them yet
                    MindMemory mm = m;
                    bool deed = Minds.AboutYou(m), youNear = look != null && look.Near;
                    // memory v1 (MindLive): the memory as they have it now (drifted, gone to gist), and a thing told in its own words
                    if (Cfg.MindLive)
                    {
                        MindMemory dr = Minds.Drifted(m, p.Id, now, KnownId, FoeOf);
                        if (!ReferenceEquals(dr, m))
                        {
                            mm = dr;
                            MemoriesDrifted++;
                        }
                        mm.Slant = SlantOf(p, st, mm);   // read against what they believe (Beliefs.Slant); worked out each read, never written
                        if (mm.Slant != null)
                            MemoriesSlanted++;
                    }
                    // saved from an execution: its own words (Executions.After), for the three days they last; the
                    // player's deeds: him as they know him
                    Func<string> make = m.Kind == "rescued" ? (Func<string>)(() => AfterRescue(st, seed + "|" + mm.Times))
                        : Cfg.MindLive && m.From != null ? () => Minds.ToldLine(mm, p, seed, deed ? Voice.YouPrivate(p, Talk.RefFor(p, st, Player), true) : null, TellerName(m)) ?? Minds.Line(mm, p, now, day, seed)
                        : deed ? () => Minds.Line(mm, p, now, day, seed, Voice.YouPrivate(p, Talk.RefFor(p, st, Player), true), youNear)
                        : (Func<string>)(() => Minds.Line(mm, p, now, day, seed));
                    c.Add(new Minds.Candidate
                    {
                        Source = "memory:" + m.Sub + "/" + m.How + (m.How == "did" && m.Role != null ? ":" + m.Role : string.Empty),
                        Pull = Minds.Pull(m, now, day, p.Personality) + (deed && youNear ? 20 : 0), M = m, Make = make
                    });
                }
            // standing where something happened (memory v1, MindLive): the nearest memory with a spot within 15 m of the
            // player (whoever is thinking is near him) comes back with its "here" words
            if (Cfg.MindLive && st != null && st.Mind != null && (PlayerX != 0f || PlayerY != 0f))
            {
                Minds.NearHere(st.Mind, PlayerX, PlayerY, PlayerZ, _here);
                if (_here.Count > 0)
                {
                    MindMemory hm = _here[0], hd = Minds.Drifted(hm, p.Id, now, KnownId, FoeOf);
                    if (hm.DueAt <= now)
                        c.Add(new Minds.Candidate { Source = "memory:here", Pull = Minds.Pull(hm, now, day, p.Personality) + 15, M = hm, Make = () => Minds.HereLine(hd, p, seed, null) });
                }
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
                bool mourned = st != null && st.Mind.Any(m => m.Kind == "death" && m.Who != null && m.Who.Contains(other));   // grief is the memory's now
                if ((!remembered || dead) && !mourned)
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

        /// The Testing page's Show minds card (four short lines): who they are, their mood and worry, their ties and
        /// memories, and what pulls hardest on them now. Nothing is worn or made.
        public string[] MindCard(Person p, int hour)
        {
            long now = Store.LastGameMin, day = now / 1440;
            PersonState st = Store.Find(p.Id);
            InnerLife.Context ctx = ThoughtDay(p.Home, p.Id);
            string mood = InnerLife.MoodOf(p, ctx, p.Id + "|card") ?? "even";
            string worry = now > 0 ? InnerLife.WorryStatus(p, day) : "none";
            int warm = 0, hostile = 0;
            foreach (SceneTie t in Store.Ties)
                if (t.Has(p.Id))
                {
                    if (t.Warmth > 0)
                        warm++;
                    else if (t.Warmth < 0)
                        hostile++;
                }
            string top = "the old mix (Minds off)";
            if (Cfg.Minds)
            {
                Minds.Candidate best = Candidates(p, st, hour, p.Id + "|card|" + now / 45, null, ctx, now).OrderByDescending(k => k.Pull).FirstOrDefault();
                top = best != null ? best.Source + " (" + (int)Math.Round(best.Pull) + ")" : "nothing much";
            }
            MindMemory last = st != null && st.Mind.Count > 0 ? st.Mind.OrderByDescending(m => m.At).First() : null;
            return new[]
            {
                p.Name + " (" + (p.Personality ?? "plain") + (p.Home != null ? ", of " + p.Home : string.Empty) + ")",
                "mood " + mood + "; worry " + worry,
                "ties " + warm + " warm, " + hostile + " hostile; " + (st != null ? st.Mind.Count : 0) + (st != null && st.Mind.Count == 1 ? " memory" : " memories") + (last != null ? ", last " + last.Sub + " (" + last.How + ")" : string.Empty),
                "on their mind: " + top
            };
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
