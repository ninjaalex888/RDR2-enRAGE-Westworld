// 0.4: the world moving on its own. Deaths and the dead who wake again, kin who hear and ride in, letters,
// the player's deeds and the rumours they start, weddings, prisoner talk, debts, goals pursued, daily diaries,
// people slowly suspecting what they are, and what the ticker shows.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public sealed partial class Engine
    {
        public readonly List<WorldRequest> Requests = new List<WorldRequest>();

        private sealed class DeathNote
        {
            public string Place, How;
            public bool Headshot;
        }

        private readonly Dictionary<string, DeathNote> _deathNotes = new Dictionary<string, DeathNote>();

        private void WEffect(EnrageEffect e)
        {
            if (e.PersonId == null)
                return;
            e.Romance = false;
            Effects.Add(e);
        }

        private void WThought(string id, string mood, string text, int weight, int hours)
        {
            WEffect(new EnrageEffect { PersonId = id, Kind = "thought", Mood = mood, Text = text, Amount = weight, Hours = hours });
        }

        private void WRemember(string id, string emotion, int intensity, string text)
        {
            WEffect(new EnrageEffect { PersonId = id, Kind = "memory", Emotion = emotion, Amount = intensity, Text = text, Cause = "you" });
        }

        private void WHistory(string id, string text)
        {
            WEffect(new EnrageEffect { PersonId = id, Kind = "history", Text = text });
        }

        public void QueueLetter(Paper p)
        {
            if (p == null || !Cfg.Letters)
                return;
            p.Delivered = false;
            Store.Papers.Add(p);
            Log("LETTER queued: " + p.Title);
        }

        // ---- deaths and reveries ---------------------------------------------------------------

        /// The game layer saw this person die: where, with what, whether through the head.
        public void NoteDeath(string id, string place, string how, bool headshot)
        {
            if (id == null)
                return;
            _deathNotes[id] = new DeathNote { Place = place, How = how, Headshot = headshot };
            Reverie r;
            if (Store.Reveries.TryGetValue(id, out r) && !r.Woke)
            {
                r.Place = place ?? r.Place;
                r.How = how ?? r.How;
                r.Headshot |= headshot;
            }
        }

        /// enRAGE counts the times the player let someone go. Each new one is a deed: word gets around.
        private void ObserveMercy(IList<Person> roster, long now)
        {
            bool seeding = !Store.SparedSeeded;
            Store.SparedSeeded = true;
            foreach (Person p in roster)
            {
                if (p == null || p.Id == null || !p.IsAlive || p.TimesSpared <= 0)
                    continue;
                PersonState st = Store.Get(p);
                if (p.TimesSpared <= st.SparedSeen)
                    continue;
                st.SparedSeen = p.TimesSpared;
                if (seeding || !Cfg.Deeds)
                    continue;
                OnDeed(Deeds.Make("spared", p.Name, CurrentTown, null, now, PlayerX, PlayerY, PlayerZ, 1), new List<string> { p.Id }, now);
                Log("SPARED " + p.Name + " - word will get around");
            }
        }

        private void ObserveDeaths(IList<Person> roster, long now)
        {
            ObserveMercy(roster, now);
            if (!Store.DeadSeeded)
            {
                // the dead from before the add-on knew them stay quietly dead
                foreach (Person p in roster.Where(x => x != null && x.Id != null && !x.IsAlive && !x.PresumedDead))
                    Store.Reveries[p.Id] = new Reverie { PersonId = p.Id, Name = p.Name, Woman = p.Woman, DiedAt = now, ByPlayer = p.KilledByPlayer, Rolled = true, Buried = p.Buried };
                Store.DeadSeeded = true;
                return;
            }
            foreach (Person p in roster)
            {
                if (p == null || p.Id == null || p.IsAlive || p.PresumedDead)
                    continue;
                Reverie r;
                if (Store.Reveries.TryGetValue(p.Id, out r))
                {
                    if (p.Buried && !r.Buried)
                        r.Buried = true;
                    continue;
                }
                DeathNote note;
                _deathNotes.TryGetValue(p.Id, out note);
                r = new Reverie
                {
                    PersonId = p.Id, Name = p.Name, Woman = p.Woman, DiedAt = now, ByPlayer = p.KilledByPlayer, Buried = p.Buried,
                    Place = note != null ? note.Place : null, How = note != null ? note.How : null, Headshot = note != null && note.Headshot,
                    Killer = p.KilledByPlayer ? Player.FullName : null, Rolled = true
                };
                if (Cfg.Reveries && !p.Lore && _rng.Next(100) < Cfg.ReturnChance)
                    r.WakeAt = now + 1440 + _rng.Next(1440 * 4);
                Store.Reveries[p.Id] = r;
                Log("DIED " + p.Name + (r.ByPlayer ? " (by you)" : "") + (r.Place != null ? " near " + r.Place : "") + (r.How != null ? ", " + r.How : "") +
                    (r.WakeAt > 0 ? " - will wake in " + (r.WakeAt - now) / 60 + " h" : ""));
                if (r.ByPlayer && Cfg.WordOfMouth)
                    QueueDeathNews(p, now);
            }
        }

        private void QueueDeathNews(Person p, long now, bool byYou = true)
        {
            PersonState st = Store.Get(p);
            if (st.Family == null)
                st.Family = Family.For(p);
            foreach (Relative rel in st.Family.OrderBy(x => x.Relation == "wife" || x.Relation == "husband" ? 0 : x.Relation == "mother" ? 1 : x.Relation == "brother" ? 2 : 3).Take(2))
            {
                if (rel.Town != null && rel.Town.Contains("Ireland"))
                    continue;
                Store.Rumors.Add(new Rumor
                {
                    FromId = p.Id, FromName = p.Name, FromWoman = p.Woman, Text = "died", Hops = 1, ToRelative = rel.Name, How = "death", NotByYou = !byYou,
                    SentAt = now, DueAt = now + 720 + _rng.Next(2880)
                });
            }
        }

        private static readonly string[] Avenging = { "brother", "father", "son", "uncle", "cousin", "husband", "nephew" };
        private static readonly string[] AvengingWomen = { "sister", "wife", "daughter", "mother" };

        private void DeliverDeath(Rumor r, long now)
        {
            PersonState st = Store.Find(r.FromId);
            Relative rel = st != null && st.Family != null ? st.Family.FirstOrDefault(x => x.Name == r.ToRelative) : null;
            if (rel == null)
                return;
            Person dead = Find(r.FromId) ?? new Person { Id = r.FromId, Name = r.FromName, Woman = r.FromWoman };
            rel.Heard.Add(new Hearsay { FromId = dead.Id, FromName = dead.Name, FromWoman = dead.Woman, Text = "died", Hops = 1, At = now, Thought = true });
            string relTo = RelationTo(rel, dead);
            bool knows = st.KnowsName;
            bool far = rel.Town == null || rel.Town.Contains(",") || new[] { "Philadelphia", "New York", "Boston", "Chicago", "St. Louis", "Kansas City", "San Francisco", "Baltimore", "Chihuahua" }.Contains(rel.Town);
            bool can = Avenging.Contains(rel.Relation) || (AvengingWomen.Contains(rel.Relation) && _rng.Next(100) < 35);
            string reaction;
            if (r.NotByYou)
            {
                // someone else's doing: grief, and no one rides west after him for it
                reaction = Voice.Pick(rel.Name + dead.Id, (rel.Woman ? "She" : "He") + " sat a long time without saying anything.", (rel.Woman ? "She" : "He") + " wants the body sent home.");
            }
            else if (Cfg.Avengers && can && _rng.Next(100) < Cfg.AvengerChance)
            {
                Avenger a = new Avenger
                {
                    Id = Ids.New(), Name = rel.Name, Relation = rel.Relation, Woman = rel.Woman, Town = rel.Town, DeadId = dead.Id, DeadName = dead.Name, DeadWoman = dead.Woman,
                    Stage = 1, DueAt = now + (far ? 4320 + _rng.Next(4320) : 720 + _rng.Next(1440))
                };
                Store.Avengers.Add(a);
                reaction = (rel.Woman ? "She" : "He") + "'s coming west to settle it " + (rel.Woman ? "herself" : "himself") + ".";
                QueueLetter(Letters.Grief(rel, dead.Name, dead.Woman, relTo, Player, knows, true, now));
                Log("AVENGER " + rel.Name + " (" + rel.Relation + " of " + dead.Name + ") rides from " + rel.Town + ", due in " + (a.DueAt - now) / 60 + " h");
            }
            else
            {
                reaction = Voice.Pick(rel.Name + dead.Id, (rel.Woman ? "She" : "He") + " sat a long time without saying anything.",
                    (rel.Woman ? "She" : "He") + "'s asking who did it.", (rel.Woman ? "She" : "He") + " wants the body sent home.");
                if (_rng.Next(100) < 45)
                    QueueLetter(Letters.Grief(rel, dead.Name, dead.Woman, relTo, Player, knows, false, now));
            }
            // one notice per death: the second of kin to hear it is in the log only (two were too many)
            if (!st.Family.Any(x => x != rel && x.Heard.Any(h => h.FromId == dead.Id && h.Text == "died")))
                Push(new Toast
                {
                    Kind = "letter", Title = "Word reaches " + ShortTown(rel.Town), PersonId = dead.Id, Weight = 45,
                    Text = rel.Name + " got a telegram: " + (rel.Woman ? "her" : "his") + " " + relTo + " " + dead.FirstName + " is dead. " + reaction
                });
            Log("WORD of " + dead.Name + "'s death reaches " + rel.Name + " (" + rel.Relation + ", " + rel.Town + ")");
        }

        private void WakeTick(long now)
        {
            foreach (Reverie r in Store.Reveries.Values.Where(x => x.WakeAt > 0 && !x.Woke && now >= x.WakeAt).ToList())
            {
                if (!Cfg.Reveries)
                    continue;   // West World or reveries switched off: they wait, they aren't forgotten
                Person p = Find(r.PersonId);
                if (p == null || p.IsAlive || p.PresumedDead || p.Lore)
                {
                    r.WakeAt = 0;
                    continue;
                }
                r.Woke = true;
                r.WokeAt = now;
                PersonState st = Store.Get(p);
                st.Deaths++;
                st.Awareness = Math.Min(100, st.Awareness + 30);
                Requests.Add(new WorldRequest
                {
                    Kind = "revive", PersonId = p.Id, Reverie = r,
                    Text = Hosts.ReverieHistory(r, Player.FullName)
                });
                PlayerRef you = Talk.RefFor(p, st, Player);
                if (Literate(p))
                {
                    string date = DateText(now);
                    st.Diary.Add(new DiaryEntry { At = now, Date = date, Text = Hosts.ReverieDiary(r, you, date, p.Id + "wake") });
                    DiaryChanged.Add(p.Id);
                }
                Push(new Toast
                {
                    Kind = "life", Title = "Reverie", PersonId = p.Id, Weight = 70,
                    Text = Voice.Pick(p.Id + "seen", p.Name + " has been seen alive" + (r.Place != null ? " near " + r.Place : "") + ". " + (r.Buried ? (p.Woman ? "She" : "He") + " was buried days ago." : "Folks swear " + (p.Woman ? "she" : "he") + " was dead."),
                        p.Name + " woke up. " + (p.Woman ? "She" : "He") + " remembers dying" + (r.ByPlayer ? ", and who did it." : "."))
                });
                Log("REVERIE " + p.Name + " wakes" + (r.Buried ? " (was buried)" : ""));
            }
        }

        // ---- the world tick (every half hour of game time) -------------------------------------------

        public void WorldTick(long now)
        {
            if (Store.LastWorldTickAt > 0 && now - Store.LastWorldTickAt < 30)
                return;
            Store.LastWorldTickAt = now;
            WakeTick(now);
            DeedTalkTick(now);
            AvengerTick(now);
            if (Cfg.RumoursTravel)
                RumourTick(now);
            if (Cfg.Weddings)
                WeddingTick(now);
            int day = (int)(now / 1440);
            if (day != Store.DiaryDayKey)
            {
                Store.DiaryDayKey = day;
                DailyTick(now);
            }
        }

        private void AvengerTick(long now)
        {
            foreach (Avenger a in Store.Avengers.Where(x => x.Stage == 1 && now >= x.DueAt).ToList())
            {
                a.Stage = 2;
                Requests.Add(new WorldRequest { Kind = "avenger", Avenger = a, Text = a.Name });
                string deadFirst = Person.FirstOf(a.DeadName ?? "");
                Push(new Toast
                {
                    Kind = "life", Title = "A rider from " + ShortTown(a.Town), Weight = 60,
                    Text = a.Name + " has come west, asking every saloon for the man who killed " + (a.Woman ? "her" : "his") + " " + KinWord(a.Relation, a.DeadWoman) + " " + deadFirst + "."
                });
            }
        }

        // ---- the player's deeds --------------------------------------------------------------------

        /// A deed the game layer saw. witnesses: enRAGE ids of people who saw it happen.
        public Deed OnDeed(Deed d, IList<string> witnesses, long now)
        {
            if (d == null || !Cfg.Deeds)
                return null;
            // fold repeats: hunting the same day, the same shop the same day
            Deed same = Store.Deeds.LastOrDefault(x => x.Kind == d.Kind && x.Where == d.Where && now - x.At < (d.Kind == "hunt" ? 1440 : 720) &&
                                                        (d.Kind == "hunt" || Deeds.IsShop(d.Kind) || d.Kind == "visit"));
            if (same != null)
            {
                if (d.Kind == "hunt")
                {
                    same.Count += d.Count;
                    same.Weight = Math.Min(100, same.Weight + 4);
                }
                return null;
            }
            Store.Deeds.Add(d);
            Store.Bump(d.Kind);
            Log("DEED " + d.Kind + (string.IsNullOrEmpty(d.Detail) ? "" : " " + d.Detail) + " " + Deeds.Place(d) + " (weight " + d.Weight + ", " + d.Witnesses + " saw)");

            string tag = ThoughtTag(d);
            foreach (string id in witnesses ?? new List<string>())
            {
                Person w = Find(id);
                if (w == null || !w.IsAlive)
                    continue;
                PersonState st = Store.Get(w);
                if (!st.KnownDeeds.Contains(d.Id))
                    st.KnownDeeds.Add(d.Id);
                if (!st.SawDeeds.Contains(d.Id))
                    st.SawDeeds.Add(d.Id);
                if (d.Detail != null && w.Name == d.Detail)
                    continue;   // it happened to them: enRAGE gives them their own memory of it
                if (Deeds.IsViolent(d.Kind))
                    WRemember(w.Id, w.Personality == "Hothead" || w.Personality == "Proud" ? "Anger" : "Fear", Math.Min(80, 40 + d.Weight / 3), WitnessText(d));
                else if (tag != null)
                    WThought(w.Id, Deeds.MoodOf(d.Kind), tag, 1, 24);
            }
            if (Deeds.IsViolent(d.Kind))
            {
                // talk waits until someone who saw it has lived to tell it, or, nobody having seen it, until the body
                // is found; the game side lowers Witnesses as they die meanwhile (Knowing.cs, DeedTalkTick)
                d.TalkAt = Knowing.TalkDue(d.At, d.Witnesses > 0, d.Id);
                d.Unseen = d.Witnesses <= 0;
                return d;
            }
            DeedTalk(d, now);
            if (Cfg.Newspaper && (d.Kind == "wedding" || d.Kind == "spared" || d.Kind == "peace" || d.Kind == "thiefcaught" || d.Kind == "mercy" || d.Kind == "rescue"))
                Requests.Add(new WorldRequest { Kind = "gooddeed", Text = Deeds.Rumour(d, "You"), Town = d.Town });
            return d;
        }

        /// Violent deeds whose talk is due: seen by someone still alive an hour on, it starts as it always did; nobody
        /// left who saw, it waits until the body would be found and the talk doesn't know who did it.
        private void DeedTalkTick(long now)
        {
            foreach (Deed d in Store.Deeds.Where(x => x.TalkAt > 0 && now >= x.TalkAt).ToList())
            {
                if (!d.Unseen && d.Witnesses <= 0)
                {
                    d.Unseen = true;
                    d.TalkAt = Knowing.TalkDue(d.At, false, d.Id);
                    Log("DEED " + d.Kind + " " + Deeds.Place(d) + ": nobody who saw it lived to tell; it'll be talked of once the body's found, " + Math.Max(0, d.TalkAt - now) / 60 + " h on, without a name");
                    if (now < d.TalkAt)
                        continue;
                }
                d.TalkAt = 0;
                Log("DEED " + d.Kind + " " + Deeds.Place(d) + ": talk starts" + (d.Unseen ? ", nobody knowing who did it" : " (" + d.Witnesses + " who saw it are alive)"));
                DeedTalk(d, now);
            }
        }

        /// Talk of a deed: the rumour, a few locals hearing of it, the paper.
        private void DeedTalk(Deed d, long now)
        {
            string tag = ThoughtTag(d);
            if (d.Weight >= 12 && Cfg.RumoursTravel)
                Rumours.Start(Store, d, now);
            if (d.Weight >= 25 && tag != null && d.Town != null && !d.Unseen)
            {
                // the town talks: a few locals who weren't there hear of it today (not of who did it, when nobody saw)
                foreach (Person local in _byId.Values.Where(x => x.IsAlive && x.Home == d.Town && x.Name != d.Detail && !Store.Get(x).SawDeeds.Contains(d.Id)).Take(4))
                {
                    PersonState st = Store.Get(local);
                    if (!st.KnownDeeds.Contains(d.Id))
                        st.KnownDeeds.Add(d.Id);
                    WThought(local.Id, Deeds.MoodOf(d.Kind), tag, 1, 24);
                }
            }
            int day = (int)(now / 1440);
            string paperKey = "paper:" + d.Kind + ":" + d.Where;
            bool quietDay = !Deeds.IsShop(d.Kind) || day - Store.Count(paperKey) >= 3;
            if (Cfg.Newspaper && d.Weight >= 15 && quietDay)
            {
                if (Deeds.IsShop(d.Kind))
                    Store.Counts[paperKey] = day;
                string item = Deeds.Paper(d, Player, !d.Unseen && (Store.People.Values.Count(s => s.KnowsName) >= 6 || d.Witnesses >= 3 && d.Weight >= 70), d.Id);
                if (item != null)
                {
                    d.Printed = true;
                    Requests.Add(new WorldRequest { Kind = "paper", Text = item, Town = d.Town ?? Rumours.NearestTown(d.X, d.Y) });
                }
            }
        }

        private static string WitnessText(Deed d)
        {
            string place = Deeds.Place(d);
            switch (d.Kind)
            {
                case "murder":
                case "kill":
                    return "saw you kill " + (d.Count > 1 ? Voice.Number(d.Count) + " men" : d.Detail ?? "a man") + " " + place;
                case "namedkill":
                    return "saw you kill " + (d.Detail ?? "a man") + " " + place;
                case "lawkill":
                    return "saw you kill a lawman " + place;
                case "bankrobbery":
                    return "saw you rob the bank " + place;
                case "assault":
                    return "saw you beat " + (d.Detail ?? "a man") + " " + place;
                case "execution":
                    return "saw you shoot " + (d.Detail ?? "a man") + " while they begged " + place;
                default:
                    return "saw what you did " + place;
            }
        }

        /// enRAGE thought text: short, a noun phrase.
        private static string ThoughtTag(Deed d)
        {
            string t = d.Town ?? d.Zone;
            string at = t != null ? " in " + t : "";
            switch (d.Kind)
            {
                case "murder": case "kill": case "namedkill": return "the killing" + at;
                case "lawkill": return "the dead lawman" + at;
                case "robbery": return "the robbery" + at;
                case "bankrobbery": return "the bank robbery" + at;
                case "stagecoach": return "the stagecoach holdup";
                case "trainrobbery": return "the train robbery";
                case "horsetheft": return "the stolen horse" + at;
                case "graverobbing": return "the opened grave" + at;
                case "kidnap": return "the kidnapping" + at;
                case "arson": return "the fire" + at;
                case "church": return "the stranger in church";
                case "gunsmith": return "the stranger buying cartridges";
                case "bank": return "the stranger at the bank";
                case "wedding": return "the wedding guest";
                case "funeral": return "the stranger at the funeral";
                case "execution": return "the begging man" + at;
                case "peace": return "the stranger who broke up the fight";
                case "thiefcaught": return "the pickpocket getting caught" + at;
                case "mercy": return "the stranger who put his gun away";
                default: return null;
            }
        }

        public bool PlayerNameKnownIn(string town)
        {
            return Store.People.Values.Count(s => s.KnowsName) >= 8 || Store.Deeds.Count(d => d.Known && d.Town == town && d.Weight >= 60) >= 2;
        }

        private void RumourTick(long now)
        {
            List<TownRumour> arrived = Rumours.Spread(Store, now);
            if (arrived.Count == 0)
                return;
            Dictionary<string, Deed> deeds = Store.Deeds.ToDictionary(x => x.Id, x => x);
            foreach (TownRumour r in arrived)
            {
                Deed d;
                if (!deeds.TryGetValue(r.DeedId, out d))
                    continue;
                string tag = ThoughtTag(d);
                foreach (Person local in _byId.Values.Where(x => x.IsAlive && x.Home == r.Town).Take(3))
                {
                    PersonState st = Store.Get(local);
                    // nobody saw who did it: they hear of the killing, not of him (KnownDeeds feeds what they say to his face)
                    if (d.Unseen ? st.Has("heard:" + d.Id) : st.KnownDeeds.Contains(d.Id))
                        continue;
                    if (d.Unseen)
                        st.Mark("heard:" + d.Id);
                    else
                        st.KnownDeeds.Add(d.Id);
                    if (r.Fidelity < 40)
                        st.Awareness = Math.Min(100, st.Awareness + 3);   // "they say he can't be killed"
                    if (tag != null && _rng.Next(100) < 50)
                        WThought(local.Id, Deeds.MoodOf(d.Kind), "word from " + r.Origin + ": " + tag, 1, 24);
                }
                Log("RUMOUR reached " + r.Town + " (fidelity " + r.Fidelity + "): " + Rumours.Version(r, d, Player, PlayerNameKnownIn(r.Town)));
            }
        }

        /// What the town is saying about the player that he hasn't heard yet. Marks them heard.
        public List<string> TownGossip(string town, int max)
        {
            List<string> lines = new List<string>();
            if (town == null)
                return lines;
            Dictionary<string, Deed> deeds = Store.Deeds.ToDictionary(x => x.Id, x => x);
            foreach (TownRumour r in Store.TownRumours.Where(x => x.Town == town && !x.Heard).OrderByDescending(x => x.At).Take(max))
            {
                Deed d;
                r.Heard = true;
                if (!deeds.TryGetValue(r.DeedId, out d))
                    continue;
                string t = Rumours.Telling(r, Rumours.Version(r, d, Player, PlayerNameKnownIn(town)));
                if (t != null)
                    lines.Add(t);
            }
            return lines;
        }

        /// Third-person versions strangers can trade in a saloon (heard or not).
        public List<string> RumourPool(string town, int max)
        {
            Dictionary<string, Deed> deeds = Store.Deeds.ToDictionary(x => x.Id, x => x);
            List<string> pool = new List<string>();
            IEnumerable<TownRumour> here = Store.TownRumours.Where(x => town == null || x.Town == town).OrderBy(x => x.Heard ? 1 : 0).ThenByDescending(x => x.At);
            foreach (TownRumour r in here.Take(max * 2))
            {
                Deed d;
                if (!deeds.TryGetValue(r.DeedId, out d))
                    continue;
                string v = Rumours.Version(r, d, Player, PlayerNameKnownIn(r.Town));
                if (v != null && !pool.Contains(v))
                    pool.Add(v);
                r.Heard = true;
                if (pool.Count >= max)
                    break;
            }
            // and the lives of people they know
            foreach (Affair a in Store.Affairs.Where(x => x.Stage >= 2 && (town == null || x.Town == town)).OrderByDescending(x => x.At).Take(2))
                pool.Add(a.AName + " has been " + (a.Stage >= 5 ? "married to " : a.Stage == 4 ? "engaged to " : "courting ") + a.BName);
            foreach (Reverie rv in Store.Reveries.Values.Where(x => Cfg.WestWorld && x.Woke).OrderByDescending(x => x.WokeAt).Take(1))
                pool.Add(rv.Name + " was dead" + (rv.Buried ? " and buried" : "") + ", and now " + (rv.Woman ? "she" : "he") + "'s walking around like nothing happened");
            return pool;
        }

        // ---- weddings -----------------------------------------------------------------------------

        private bool WeddingPending(Affair a)
        {
            return Cfg.Weddings && Store.Weddings.Any(w => !w.Done && w.AId == a.AId && w.BName == a.BName);
        }

        private void ScheduleWedding(Affair a, Person pa, Person pb, long now)
        {
            if (!Cfg.Weddings || a.Offstage || Store.Weddings.Any(x => x.AId == a.AId && x.BName == a.BName))
                return;
            string town = a.Town ?? (pa != null ? pa.Home : null) ?? "Valentine";
            if (!Rumours.Towns.ContainsKey(town) || !WeddingTowns.Contains(town))
                town = WeddingTowns.OrderBy(t => Rumours.Distance(t, town)).First();
            long day = now / 1440 + 2 + _rng.Next(3);
            Wedding w = new Wedding
            {
                Id = Ids.New(), AId = a.AId, AName = a.AName, AWoman = a.AWoman, BId = a.BId, BName = a.BName, BWoman = a.BWoman,
                Town = town, At = day * 1440 + 11 * 60 + _rng.Next(4) * 30
            };
            Store.Weddings.Add(w);
            Log("WEDDING set: " + w.AName + " & " + w.BName + " at " + w.Town + ", " + DateText(w.At));
        }

        public static readonly string[] WeddingTowns = { "Valentine", "Rhodes", "Blackwater", "Strawberry", "Saint Denis", "Armadillo", "Van Horn" };

        private void WeddingTick(long now)
        {
            foreach (Wedding w in Store.Weddings.Where(x => !x.Done).ToList())
            {
                Affair a = Store.Affairs.FirstOrDefault(x => x.AId == w.AId && x.BName == w.BName);
                if (a == null || !a.Active)
                {
                    w.Done = true;
                    Log("WEDDING called off: " + w.AName + " & " + w.BName);
                    continue;
                }
                if (!w.Announced && now >= w.At - 2880)
                {
                    w.Announced = true;
                    string when = WhenText(w.At, now);
                    Push(new Toast { Kind = "life", Title = "Banns posted in " + w.Town, Weight = 45, Text = w.AName + " and " + w.BName + " will marry at the church in " + w.Town + ", " + when + "." });
                    if (Cfg.Newspaper)
                        Requests.Add(new WorldRequest { Kind = "paper", Town = w.Town, Text = "Banns are posted for " + w.AName + " and " + w.BName + ", to be married at the church " + when + ". All are welcome." });
                    PersonState sa = Store.Find(w.AId), sb = w.BId != null ? Store.Find(w.BId) : null;
                    bool friend = (sa != null && (sa.Talks >= 2 || sa.Affection >= 20)) || (sb != null && (sb.Talks >= 2 || sb.Affection >= 20));
                    if (friend)
                        QueueLetter(Letters.Invitation(w, Player, (sa != null && sa.KnowsName) || (sb != null && sb.KnowsName), when, now));
                }
                if (now > w.At + 240)
                    FinishWedding(w, a, now, false);
            }
        }

        /// The ceremony is over (attended: the player was there).
        public void FinishWedding(Wedding w, Affair a, long now, bool attended)
        {
            if (w.Done)
                return;
            w.Done = true;
            w.Attended = attended;
            a = a ?? Store.Affairs.FirstOrDefault(x => x.AId == w.AId && x.BName == w.BName);
            if (a != null && a.Active && a.Stage < 5)
            {
                a.Stage = 5;
                a.At = now;
                Moved(a, Find(a.AId), a.BId != null ? Find(a.BId) : null, now);
            }
            if (attended)
            {
                foreach (string id in new[] { w.AId, w.BId }.Where(x => x != null))
                {
                    WRemember(id, "Joy", 60, "you came to the wedding");
                    PersonState st = Store.Find(id);
                    if (st != null)
                        st.Affection = Math.Min(100, st.Affection + 10);
                }
            }
            Log("WEDDING done: " + w.AName + " & " + w.BName + (attended ? " (you were there)" : ""));
        }

        /// The ceremony broke up (shooting, a body on the church steps). They marry quietly later; if it was the
        /// player's doing, they remember who spoiled it.
        public void RuinWedding(Wedding w, long now, bool byYou)
        {
            if (w == null || w.Done)
                return;
            FinishWedding(w, null, now, false);
            if (!byYou)
                return;
            foreach (string id in new[] { w.AId, w.BId }.Where(x => x != null))
            {
                WRemember(id, "Anger", 75, "you broke up the wedding");
                PersonState st = Store.Find(id);
                if (st != null)
                    st.Affection = Math.Max(0, st.Affection - 30);
            }
            Log("WEDDING broken up by you: " + w.AName + " & " + w.BName);
        }

        public Wedding WeddingNear(string town, long now)
        {
            return Store.Weddings.FirstOrDefault(w => !w.Done && w.Town == town && now >= w.At - 30 && now <= w.At + 240);
        }

        private string WhenText(long at, long now)
        {
            long days = at / 1440 - now / 1440;
            int hour = (int)(at / 60 % 24);
            string h = (hour % 12 == 0 ? 12 : hour % 12) + (hour < 12 ? " in the morning" : " in the afternoon");
            return (days <= 0 ? "today" : days == 1 ? "tomorrow" : "on " + GameDay(at)) + " at " + h;
        }

        private string GameDay(long at)
        {
            string d = DateText(at);
            int comma = d.IndexOf(',');
            return comma > 0 ? d.Substring(0, comma) : d;
        }

        // ---- once a day -----------------------------------------------------------------------------

        private void DailyTick(long now)
        {
            AwarenessTick(now);
            AwakeningTick(now);
            if (Cfg.Diaries)
                DailyDiaries(now);
            if (Cfg.Letters)
                LetterTick(now);
            if (Cfg.PrisonerTransfers)
                TransferTick(now);
            if (Cfg.Debts)
                DebtTick(now);
            GoalTick(now);
        }

        private void AwarenessTick(long now)
        {
            foreach (Person p in _byId.Values.Where(x => x.IsAlive))
            {
                int trauma = p.Memories.Count(m => (m.Emotion == "Fear" || m.Emotion == "Grief") && m.Intensity >= 70);
                if (trauma == 0 && p.CheatedDeath == 0)
                    continue;
                PersonState st = Store.Get(p);
                if (_rng.Next(100) < 25 + trauma * 10)
                    st.Awareness = Math.Min(100, st.Awareness + 1 + _rng.Next(2));
            }
        }

        private void DailyDiaries(long now)
        {
            List<Person> writers = _byId.Values.Where(p => p.IsAlive && Literate(p)).Select(p => new { p, st = Store.Find(p.Id) })
                .Where(x => x.st != null && x.st.DiaryDay != (int)(now / 1440) && !x.st.DiaryFound)
                .OrderByDescending(x => (x.st.Talks > 0 ? 30 : 0) + x.st.KnownDeeds.Count * 5 + Math.Abs(x.st.Heart) * 10 + x.st.Awareness / 4 + x.p.Thoughts.Count)
                .Take(5).Select(x => x.p).ToList();
            foreach (Person p in writers)
            {
                PersonState st = Store.Get(p);
                string entry = DayEntry(p, st, now);
                if (entry == null)
                    continue;
                st.DiaryDay = (int)(now / 1440);
                string date = DateText(now);
                st.Diary.Add(new DiaryEntry { At = now, Date = date, Text = date + " - " + entry });
                if (st.Diary.Count > 60)
                    st.Diary.RemoveAt(0);
                DiaryChanged.Add(p.Id);
            }
        }

        /// A day in someone's life, in their own words: two to four sentences. The ordinary texture of the day
        /// (work, town, kin, faith, habits), what's on their mind when it's new, what happened (the player's
        /// deeds, love, debts, dying and waking, the odd feeling that the world is made), and how they end the day.
        /// Nothing written in their last three entries is written again.
        public string DayEntry(Person p, PersonState st, long now)
        {
            string seed = p.Id + "day" + now / 1440;
            PlayerRef you = Talk.RefFor(p, st, Player);
            if (st.Family == null)
                st.Family = Family.For(p);
            string recent = string.Join(" ", st.Diary.Skip(Math.Max(0, st.Diary.Count - 3)).Select(e => e.Text));

            List<string> news = new List<string>();
            Affair a = Store.Affairs.FirstOrDefault(x => x.Active && (x.AId == p.Id || x.BId == p.Id) && x.Stage >= 2);
            if (a != null && Voice.Chance(seed + "aff", 70))
            {
                string other = Person.FirstOf(a.AId == p.Id ? a.BName : a.AName);
                news.Add(a.Stage >= 5 ? Voice.Pick(seed + "a", "Married life suits me. " + other + " snores.", other + " made supper. Burned it. I ate every bite.",
                        other + " and I walked out past the church after supper. Didn't say much. Didn't need to.")
                    : a.Stage == 4 ? Voice.Pick(seed + "a", "Not long now till the wedding. " + other + " is more nervous than I am.", "Counted the days till the wedding again.",
                        "Tried on my good clothes for the wedding. They still fit, near enough.")
                    : Voice.Pick(seed + "a", "Saw " + other + " today. I'm a fool about it and I know it.", "Thought about " + other + " more than I should.",
                        other + " smiled at me across the street. I walked into a post."));
            }
            Deed d = st.KnownDeeds.Count > 0 ? Store.Deeds.LastOrDefault(x => st.KnownDeeds.Contains(x.Id) && !st.Has("dd:" + x.Id)) : null;
            if (d != null && now - d.At >= Knowing.DiaryAfterMinutes && now - d.At < 4320)   // a deed they know of, once there was time to write it
            {
                st.Mark("dd:" + d.Id);
                string th = Deeds.Thought(p, d, Voice.YouPrivate(p, you, true), seed);
                if (th != null)
                    news.Add(th);
            }
            // what happened between them and someone else today, told from their side of it (Ties.Thought): a face put
            // into the bar, a purse gone, a coin given or refused, a quarrel, a courtship
            SceneTie tie = Store.Ties.Where(t => t.Has(p.Id) && now - t.At >= 0 && now - t.At < 1440 && (Ties.Trouble(t.Kind) || (Ties.Effect(t.Kind) ?? 0) != 0) &&
                                                 !st.Has("dt:" + t.OtherId(p.Id) + ":" + t.At)).OrderByDescending(t => t.At).FirstOrDefault();
            if (tie != null)
            {
                st.Mark("dt:" + tie.OtherId(p.Id) + ":" + tie.At);
                news.Add(Ties.Thought(tie, p.Id, seed, HasDied(tie.OtherId(p.Id))));
            }
            Debt debt = Store.Debts.FirstOrDefault(x => !x.Settled && (x.FromId == p.Id || x.ToId == p.Id));
            if (debt != null && (now / 1440 - debt.Since / 1440) % 3 == 0)
                news.Add(debt.ToId == p.Id ? Person.FirstOf(debt.FromName) + " still owes me " + Voice.Number(debt.Amount) + " dollars."
                    : "I owe " + Person.FirstOf(debt.ToName) + " " + Voice.Number(debt.Amount) + " dollars. Don't know where I'll find it.");
            Reverie rv;
            if (Cfg.WestWorld && Store.Reveries.TryGetValue(p.Id, out rv) && rv.Woke && now - rv.WokeAt > 1440 && Voice.Chance(seed + "rv", 30))
                news.Add(Hosts.ReverieThought(rv, you, seed));
            if (Cfg.HostsQuestionReality && Hosts.Tier(Hosts.Awareness(p, st)) >= 1)
            {
                if (Voice.Chance(seed + "h", 40))
                    news.Add(Hosts.Thought(p, st, you, seed));
                else if (Voice.Chance(seed + "dream", 35))
                    news.Add(Hosts.Dream(p, st, seed));
                else if (Voice.Chance(seed + "corner", 25))
                    news.Add(Hosts.Cornerstone(p, st, seed));
            }
            if (Cfg.HostsQuestionReality && st.WakeStage >= Awakening.Questioning && Voice.Chance(seed + "secret", 55))
            {
                string secret = Awakening.DiaryEntry(p, st, you, seed);
                if (secret != null)
                    news.Add(secret);
            }

            // what's on their mind: the strongest thought while it's new, now and then after, or the next one down
            string mind = null;
            ThoughtItem top = p.TopThought;
            if (top != null && !string.IsNullOrEmpty(top.Text) && (now - top.At < 2880 || Voice.Chance(seed + "top", 35)))
                mind = Voice.MoodLine(p, top, seed);
            else if (p.Thoughts != null && p.Thoughts.Count > 1 && Voice.Chance(seed + "t2", 60))
                mind = Voice.MoodLine(p, p.Thoughts[1 + (Voice.Hash(seed + "t2i") & 0x7fffffff) % Math.Min(2, p.Thoughts.Count - 1)], seed);
            if (DayLines.Seen(recent, mind))
                mind = null;

            List<string> texture = DayLines.Texture(p, st, you, p.Home, seed, 2, recent);
            string youWord = Voice.YouPrivate(p, you, true);
            if (!string.IsNullOrEmpty(youWord) && news.Any(x => x != null && x.Contains(youWord)))
                texture.RemoveAll(x => x.Contains(youWord));   // he's in the entry once already
            List<string> parts = new List<string>();
            if ((mind == null || Voice.Chance(seed + "order", 60)) && texture.Count > 0)
            {
                parts.Add(texture[0]);
                texture.RemoveAt(0);
            }
            if (mind != null)
                parts.Add(mind);
            parts.AddRange(news.Where(x => !string.IsNullOrEmpty(x) && !DayLines.Seen(recent, x)).Take(2));
            if (parts.Count < 3 && texture.Count > 0)
                parts.Add(texture[0]);
            if (parts.Count < 4)
            {
                string end = null;
                GoalItem g = p.Goals != null ? p.Goals.FirstOrDefault() : null;
                if (g != null && Voice.Chance(seed + "goal", 35))
                {
                    string gl = Voice.GoalLine(p, g, seed);
                    if (gl != null)
                    {
                        string who = Voice.YouPrivate(p, you, false);
                        end = System.Text.RegularExpressions.Regex.Replace(gl.Replace("What do I want? ", ""), @"\byou\b", m => who);
                    }
                }
                if (end == null && Voice.Chance(seed + "end", 55))
                    end = DayLines.Closer(p, seed, recent);
                if (end != null && !DayLines.Seen(recent, end))
                    parts.Add(end);
            }
            if (parts.Count == 0)
                return null;
            List<string> kept = parts.Take(4).Select(x => Grammar.Sentence(x)).ToList();
            while (kept.Count > 2 && kept.Sum(x => x.Length + 1) > 330)
                kept.RemoveAt(kept.Count - 1);
            return string.Join(" ", kept);
        }

        private void LetterTick(long now)
        {
            foreach (Person p in _byId.Values.Where(x => x.IsAlive && Literate(x)))
            {
                PersonState st = Store.Find(p.Id);
                if (st == null)
                    continue;
                if (st.Heart >= 2 && now - st.ContactAt > 4320 && !st.Has("letter:" + now / 7200) && _rng.Next(100) < 40)
                {
                    st.Mark("letter:" + now / 7200);
                    QueueLetter(Letters.Love(p, Player, st.KnowsName, st.Heart, now));
                }
                else if (st.Heart < 0 && !st.Has("letter:scorned"))
                {
                    st.Mark("letter:scorned");
                    if (_rng.Next(100) < 60)
                        QueueLetter(Letters.Scorned(p, Player, st.KnowsName, now));
                }
                else if (Hosts.Awareness(p, st) >= 60 && st.Talks >= 1 && !st.Has("letter:confession"))
                {
                    st.Mark("letter:confession");
                    QueueLetter(Letters.Confession(p, st, Player, st.KnowsName, now));
                }
                else
                {
                    MemoryItem good = p.Memories.FirstOrDefault(m => m.AboutYou && m.Emotion == "Joy" && m.Intensity >= 55 && m.At >= Store.InstalledAt && !st.Has("letter:" + m.Key));
                    if (good != null)
                    {
                        st.Mark("letter:" + good.Key);
                        if (_rng.Next(100) < 35)
                        {
                            string what = Voice.Relative(good.Text, p.Woman);
                            what = what != null && what.StartsWith("the man who ") ? what.Substring(12).Replace(" me", " me") : "did right by me";
                            QueueLetter(Letters.Thanks(p.Name, p.Home ?? "town", null, true, what, Player, st.KnowsName, now));
                        }
                    }
                }
            }
        }

        private void TransferTick(long now)
        {
            foreach (Transfer t in Store.Transfers.Where(x => x.Outcome == null && now >= x.At).ToList())
            {
                Person p = Find(t.PersonId);
                int roll = _rng.Next(100);
                t.Outcome = roll < 40 ? "escaped" : roll < 65 ? "freed" : roll < 85 ? "released" : "pardoned";
                string text = t.Outcome == "escaped" ? t.Name + " slipped his chains on the road to " + t.To + " and vanished into the brush."
                    : t.Outcome == "freed" ? "Riders stopped the prison wagon carrying " + t.Name + " outside " + t.From + ". The deputies walked home."
                    : t.Outcome == "released" ? "The judge in " + t.From + " let " + t.Name + " go for want of witnesses."
                    : t.Name + " bought a pardon. Money talks, in " + t.From + " as anywhere.";
                if (p != null && p.Woman)
                    text = text.Replace(" his ", " her ");
                Push(new Toast { Kind = "life", Title = "Prisoner transfer", PersonId = t.PersonId, Weight = 40, Text = text });
                if (p != null && p.IsAlive)
                    WThought(p.Id, "Joyful", t.Outcome == "escaped" ? "escaping the prison wagon" : "walking free", 2, 48);
            }
            foreach (Person p in _byId.Values.Where(x => x.IsAlive && x.TopThought != null && x.TopThought.Mood == "Jailed"))
            {
                if (Store.Transfers.Any(t => t.PersonId == p.Id && now - t.At < 14400) || _rng.Next(100) >= 35)
                    continue;
                string text = p.TopThought.Text ?? string.Empty;
                string from = Rumours.Towns.Keys.FirstOrDefault(t => text.Contains(t)) ?? p.Home ?? "Valentine";
                Transfer tr = new Transfer { Id = Ids.New(), PersonId = p.Id, Name = p.Name, From = from, To = Voice.Pick(p.Id + "to", "Sisika", "Saint Denis", "the federal court"), At = now + 1440 + _rng.Next(1440) };
                Store.Transfers.Add(tr);
                Push(new Toast { Kind = "life", Title = "Word from the jail", PersonId = p.Id, Weight = 35,
                    Text = "They're moving " + p.Name + " from the " + from + " jail to " + tr.To + " " + WhenText(tr.At, now) + ", under guard." });
            }
        }

        private static readonly string[] DebtReasons = { "a horse", "a doctor's bill", "a bad night at faro", "a wedding ring", "seed money", "a funeral", "a round of drinks that got out of hand", "bail" };

        private void DebtTick(long now)
        {
            foreach (Debt d in Store.Debts.Where(x => !x.Settled && now - x.Since > 2880 + Voice.Hash(x.Id) % 5760).ToList())
            {
                Person lender = Find(d.ToId), owes = Find(d.FromId);
                d.Settled = true;
                if (lender == null || owes == null || !lender.IsAlive || !owes.IsAlive)
                    continue;
                d.How = lender.Personality == "Hothead" || lender.Personality == "Greedy" ? "beaten" : lender.Friendly || lender.Personality == "Loyal" ? "forgiven" : "paid";
                string text = d.How == "beaten" ? lender.Name + " collected the " + d.Amount + " dollars " + owes.Name + " owed - with " + lender.His + " fists."
                    : d.How == "forgiven" ? lender.Name + " tore up " + owes.Name + "'s IOU for " + d.Amount + " dollars. Said friendship's worth more."
                    : owes.Name + " finally paid " + lender.Name + " the " + d.Amount + " dollars for " + d.Why + ".";
                Push(new Toast { Kind = "life", Title = "Debts", PersonId = lender.Id, Weight = 25, Text = text });
                WThought(lender.Id, d.How == "beaten" ? "Proud" : "Steady", "settling " + owes.FirstName + "'s debt", 1, 24);
                WThought(owes.Id, d.How == "beaten" ? "Hurting" : d.How == "forgiven" ? "Joyful" : "Steady", d.How == "beaten" ? "the beating over money" : "a debt paid", 1, 24);
            }
            if (_rng.Next(100) >= 30 || Store.Debts.Count(x => !x.Settled) >= 4)
                return;
            List<Person> alive = _byId.Values.Where(x => x.IsAlive && x.FriendIds.Count > 0).ToList();
            if (alive.Count == 0)
                return;
            Person a = alive[_rng.Next(alive.Count)];
            Person b = Find(a.FriendIds[_rng.Next(a.FriendIds.Count)]);
            if (b == null || !b.IsAlive)
                return;
            Debt debt = new Debt { Id = Ids.New(), FromId = a.Id, FromName = a.Name, ToId = b.Id, ToName = b.Name, Amount = 5 + _rng.Next(16) * 5, Why = DebtReasons[_rng.Next(DebtReasons.Length)], Since = now };
            Store.Debts.Add(debt);
            Log("DEBT " + a.Name + " owes " + b.Name + " $" + debt.Amount + " for " + debt.Why);
        }

        private void GoalTick(long now)
        {
            if (_rng.Next(100) >= 45)
                return;
            List<Person> movers = _byId.Values.Where(x => x.IsAlive && x.Goals.Count > 0 && x.Goals[0].Type != "Avenge").ToList();
            if (movers.Count == 0)
                return;
            Person p = movers[_rng.Next(movers.Count)];
            GoalItem g = p.Goals[0];
            string seed = p.Id + "goal" + now / 1440;
            string text, mood, tag;
            switch (g.Type)
            {
                case "Rich":
                    text = Voice.Pick(seed, p.Name + " won big at faro in " + (p.Home ?? "town") + ". Lost half of it again by morning.", p.Name + " has been seen asking about a payroll wagon.", p.Name + " sold a horse with no bill of sale, and got a fine price.");
                    mood = "Flush"; tag = "money in my pocket";
                    break;
                case "Settle":
                    text = Voice.Pick(seed, p.Name + " was seen looking over a parcel of land near " + Voice.Pick(seed + "t", "Strawberry", "Emerald Ranch", "Wapiti", "Flatneck Station") + ".", p.Name + " has been putting money aside. Says it's for a farm.");
                    mood = "Steady"; tag = "the farm I'll have someday";
                    break;
                case "Rise":
                    text = Voice.Pick(seed, p.Name + " has been buying drinks for the younger men. Building a following.", p.Name + " stood up to " + p.His + " boss in front of everyone, they say.");
                    mood = "Proud"; tag = "climbing";
                    break;
                case "Prove":
                    text = Voice.Pick(seed, p.Name + " called a man out in the street just to be seen doing it.", p.Name + " rode alone into country nobody rides alone into. Came back, too.");
                    mood = "Proud"; tag = "proving myself";
                    break;
                case "Protect":
                    text = Voice.Pick(seed, p.Name + " has been sleeping with a shotgun across " + p.His + " knees.", p.Name + " sent " + p.His + " family away somewhere safe.");
                    mood = "Shaken"; tag = "keeping my family safe";
                    break;
                default:
                    return;
            }
            Push(new Toast { Kind = "life", Title = p.Name, PersonId = p.Id, Weight = 20, Text = text });
            WThought(p.Id, mood, tag, 1, 24);
        }

        // ---- the ticker -----------------------------------------------------------------------------

        /// The private thought of a nearby person, for the bottom ticker.
        public string TickerThought(Person p, long now)
        {
            PersonState st = Store.Get(p);
            PlayerRef you = Talk.RefFor(p, st, Player);
            string seed = p.Id + "|tick|" + now / 180;
            Reverie rv;
            if (Cfg.WestWorld && Store.Reveries.TryGetValue(p.Id, out rv) && rv.Woke && Voice.Chance(seed + "rv", 60))
                return Hosts.ReverieThought(rv, you, seed);
            if (Cfg.HostsQuestionReality && Voice.Chance(seed + "host", Math.Min(60, Cfg.RealityChance / 2 + Hosts.Awareness(p, st) / 3)))
                return Hosts.Thought(p, st, you, seed);
            if (Cfg.HostsQuestionReality && Voice.Chance(seed + "corner", 12))
            {
                string c = Hosts.Cornerstone(p, st, seed);
                if (c != null)
                    return c;
            }
            // what they lived through that RealNPCs saw, when it weighs on them (Minds)
            string lived = RecordMemoryThought(p, st, seed);
            if (lived != null)
                return lived;
            if (Cfg.Romance && st.Heart != 0 && Voice.Chance(seed + "heart", 60))
            {
                string r = Romance.Thought(p, st.Heart, you, seed);
                if (r != null)
                    return r;
            }
            MemoryItem m = p.Memories.Where(x => x.AboutYou && x.Intensity >= 30).OrderByDescending(x => x.Intensity + (now - x.At < 1440 ? 25 : 0)).FirstOrDefault();
            if (m != null && Voice.Chance(seed + "mem", 75))
                return Voice.MemoryThought(p, m, you, seed);
            Deed d = st.KnownDeeds.Count > 0 ? Store.Deeds.LastOrDefault(x => st.KnownDeeds.Contains(x.Id)) : null;
            if (d != null && Voice.Chance(seed + "deed", 70))
            {
                string t = Deeds.Thought(p, d, Voice.YouPrivate(p, you, true), seed);
                if (t != null)
                    return t;
            }
            Hearsay h = st.Heard.LastOrDefault();
            if (h != null && Voice.Chance(seed + "heard", 60))
                return Voice.HearsayThought(p, Person.FirstOf(h.FromName), h.Text, h.FromWoman, you, seed);
            Debt debt = Store.Debts.FirstOrDefault(x => !x.Settled && (x.FromId == p.Id || x.ToId == p.Id));
            if (debt != null && Voice.Chance(seed + "debt", 40))
                return debt.ToId == p.Id ? Person.FirstOf(debt.FromName) + " owes me " + Voice.Number(debt.Amount) + " dollars. I ain't forgot." : "Still owe " + Person.FirstOf(debt.ToName) + " that money. Got to find it somewhere.";
            if (st.Talks == 0 && st.Sightings > 0 && m == null)
                return Voice.Pick(seed + "first", "Who's that? Never seen him before.", "Big fella. Looks like trouble.", "Stranger's watching me. Or I'm watching him.", you.Scarred ? "That face. Somebody carved him up good once." : "Another drifter.");
            return Voice.MoodLine(p, p.TopThought, seed);
        }

        /// A stranger's passing thought, for the ticker when nobody enRAGE knows is near.
        public string StrangerThought(string seed, string town, bool woman, bool everyday = true)
        {
            if (Cfg.HostsQuestionReality && Voice.Chance(seed + "h", Cfg.RealityChance))
                return Hosts.StrangerAloud(Store.Reveries.Values.Count(x => x.Woke) >= 2 ? 1 : 0, seed);
            List<string> rumours = RumourPool(town, 3);
            if (rumours.Count > 0 && Voice.Chance(seed + "r", 50))
                return "Heard " + Grammar.LowerFirst(rumours[Voice.Hash(seed) % rumours.Count].TrimEnd('.')) + ".";
            if (Store.Deeds.Count(x => x.Known && x.Weight >= 40 && Store.LastGameMin - x.At < 7 * 1440) >= 2 && Voice.Chance(seed + "fame", 35))
                return Voice.Pick(seed + "f", "Is that him? The one they're all talking about?", "Don't stare. Men like that notice when you stare.",
                    "Keep walking. Keep walking. Don't give him a reason.", "Mama always said trouble rides in on a quiet horse.");
            if (everyday && Voice.Chance(seed + "day", 55))
                return Voice.Pick(seed + "d" + (woman ? "w" : "m"), woman ? EverydayWomen : EverydayMen);
            return null;
        }

        /// What an ordinary person (a resident) is thinking: the same big things as a stranger (the world feeling unreal,
        /// rumours, the player's fame), else something from their own life.
        public string ResidentThought(Person p, int hour, string seed, InnerLife.PlayerLook look = null)
        {
            return ResidentThought(p, hour, seed, look, null);
        }

        /// The same, with what the caller knows besides (a keeper's counter) for the minds to weigh.
        private string ResidentThought(Person p, int hour, string seed, InnerLife.PlayerLook look, List<Minds.Candidate> extra)
        {
            PersonState st = Store.Find(p.Id);
            EnsureLink(p);
            if (Cfg.Minds)
            {
                // the world feeling unreal keeps its full RealityChance, first, as before
                if (Cfg.HostsQuestionReality && Voice.Chance(seed + "h", Cfg.RealityChance))
                {
                    string unreal = StrangerThought(seed, p.Home, p.Woman, false);
                    if (unreal != null)
                        return unreal;
                }
                List<Minds.Candidate> more = extra ?? new List<Minds.Candidate>();
                // saved from an execution on the road lately (Executions.cs): a memory now; before Minds, the old marks
                string saved = st != null && st.Mind.Any(m => m.Kind == "rescued") ? null : AfterRescue(st, seed);
                if (saved != null)
                    more.Add(new Minds.Candidate { Source = "rescued", Pull = 55, Make = () => saved });
                // the town's rumours and the player's fame (the same seed: its reality roll came out no above)
                more.Add(new Minds.Candidate { Source = "heard:rumour", Pull = 20 + Minds.Jitter(seed, "rumour", 8), Make = () => StrangerThought(seed, p.Home, p.Woman, false) });
                Minds.Choice c = Think(p, st, hour, seed, look, more);
                if (c != null)
                    return c.Text;
            }
            string after = AfterRescue(st, seed);   // saved from an execution on the road lately (Executions.cs)
            if (after != null && Voice.Chance(seed + "resc", 45))
                return after;
            // the world feeling unreal keeps its full RealityChance; town rumours and the player's fame get a third of
            // the rest, so their own life (InnerLife) has most of it
            bool reality = Cfg.HostsQuestionReality && Voice.Chance(seed + "h", Cfg.RealityChance);
            string big = reality || Voice.Chance(seed + "talk", 35) ? StrangerThought(seed, p.Home, p.Woman, false) : null;
            return big ?? InnerLife.Thought(p, hour, seed, st != null ? st.Musings : null, look, ThoughtDay(p.Home, p.Id));
        }

        /// One of enRAGE's townsfolk: what they feel about the player, when it's strong, most of the time; else, and
        /// now and then anyway, their own life (InnerLife: the worry they're going through, the day, their town).
        public string FolkThought(Folk f, Person p, PersonState st, string seed, int hour, InnerLife.PlayerLook look)
        {
            string about = Folks.Thought(f, st, Player, seed, Cfg.Romance);
            bool strong = about != null && (st.Heart >= 2 || st.Heart < 0 || f.FavourNotes.Count > 0 || f.Witnessed.Count > 0 || f.Grudge >= 40 || f.Fear >= 40);
            if (about != null && Voice.Chance(seed + "fa", strong ? 70 : 30))
                return about;
            if (p.Personality == null)
                p.Personality = f.Temper;
            EnsureLink(p);
            if (Cfg.Minds)
            {
                // what they feel about the player is one of the things on their mind, strong or not
                var extra = new List<Minds.Candidate>();
                if (about != null)
                    extra.Add(new Minds.Candidate { Source = strong ? "folk:strong" : "folk", Pull = (strong ? 50 : 25) + Minds.Jitter(seed, "folk", 6), Make = () => about });
                Minds.Choice c = Think(p, st, hour, seed, look, extra);
                return c != null ? c.Text : about;
            }
            return InnerLife.Thought(p, hour, seed, st.Musings, look, ThoughtDay(p.Home, p.Id)) ?? about;
        }

        /// The game day, for the worry a life is going through; what the player did in this town in the last three
        /// days (from two hours after, when word has got round), newest first; this person's street scenes lately,
        /// and the ones between two others in their town.
        /// What is going on around the player (and so around whoever near him is thinking), kept up by the game side.
        public string NowWeather, NowPlace;
        public bool NowIndoors;
        public InnerLife.Witnessed LastScene;
        public List<InnerLife.Nearby> Nearby = new List<InnerLife.Nearby>();
        public long NearbyAt = -1000;   // game minute the list was made (stale after a few minutes: the ticker was off)
        /// How long, in game minutes, people near a scene still have it on their minds.
        public const long SeenForMinutes = 45;

        /// A scene has just played out near the player: who was in it, so those watching think about it for a while.
        public void SawScene(string kind, string aName, string bName, string aId, string bId, string town)
        {
            LastScene = new InnerLife.Witnessed { Kind = kind, AName = aName, BName = bName, AId = aId, BId = bId, Town = town, At = Store.LastGameMin };
        }

        public InnerLife.Context ThoughtDay(string town, string id = null)
        {
            long now = Store.LastGameMin;
            InnerLife.Context c = new InnerLife.Context { GameDay = now > 0 ? now / 1440 : -1, Dead = HasDied, Weather = NowWeather, Indoors = NowIndoors, Place = NowPlace, Now = now };
            // what is going on around them now (set by the game side): a scene just played out in their town, and the
            // others in sight (not themselves; the two in the scene think about it through their tie instead)
            InnerLife.Witnessed seen = LastScene;
            // (the scene is where the player is, and so is whoever near him is thinking: not necessarily their home town)
            if (seen != null && now - seen.At >= 0 && now - seen.At < SeenForMinutes && seen.Town == CurrentTown)
                c.JustSaw = seen;
            if (Nearby != null && Nearby.Count > 0 && Math.Abs(now - NearbyAt) <= 3)
                c.Nearby = Nearby.Where(n => n.Id != id).ToList();
            if (town != null)
            {
                c.TownDeeds = Store.Deeds.Where(d => d.Known && d.Town == town && InnerLife.TalkedAbout.Contains(d.Kind) && now - d.At >= 120 && now - d.At < 3 * 1440)
                                         .OrderByDescending(d => d.At).Take(3).ToList();
                c.TownTies = Ties.TalkOf(Store.Ties, town, id, now);
            }
            if (id != null)
            {
                c.Ties = Ties.Of(Store.Ties, id, now, 4);
                if (c.GameDay >= 0)
                    c.Linked = Links.Against(Store.Links, id, c.GameDay).Where(l => !HasDied(l.Id)).ToList();   // a worrier who has died worries nobody now
            }
            return c;
        }

        /// Someone the add-on knows of has died: a resident or regular, one of enRAGE's townsfolk (asked of enRAGE),
        /// or one of enRAGE's people.
        public bool HasDied(string id)
        {
            if (id == null)
                return false;
            // townsfolk: our own record (Deaths.cs) or enRAGE's. A saved resident: their flag, which the posts code clears
            // when a keeper turns out only shot down; an unsaved one: our record. Whether one of enRAGE's records lives
            // is enRAGE's to say, since it brings some of them back on purpose.
            if (id.StartsWith("folk:"))
                return Store.DeathOf(id) != null || (FolkDead != null && FolkDead(id));
            if (Resident.IsResident(id))
            {
                Resident r = Store.FindResident(id);
                return r != null ? r.Dead : Store.DeathOf(id) != null;
            }
            Person p;
            return _byId.TryGetValue(id, out p) && !p.IsAlive;
        }

        /// Every death RealNPCs records comes through here: our own record (Deaths.cs), and a resident marked dead.
        /// False when it was already noted.
        public bool NoteDied(DeathEntry e)
        {
            if (e == null || e.Id == null)
                return false;
            Resident r = Resident.IsResident(e.Id) ? Store.FindResident(e.Id) : null;
            if (r != null)
                r.Dead = true;
            if (!Store.NoteDeath(e))
                return false;
            RememberDeathHeard(e);   // their friends and foes hear of it (Minds)
            return true;
        }

        // ---- the real people at the other end of worries (Links.cs) ------------------------------------------------

        private readonly Dictionary<string, long> _noLinkUntil = new Dictionary<string, long>();

        /// Someone who can be met again: one of enRAGE's townsfolk, or a resident who is saved (a regular, or kept
        /// for something else). Only they get a real person at the other end of a worry.
        public bool Lasting(string id)
        {
            return id != null && (id.StartsWith("folk:") || (Resident.IsResident(id) && Store.FindResident(id) != null));
        }

        /// Makes the link for this person's worry run if it has a real counterpart and none is made yet: the one place
        /// links are made (thoughts only read them). A town with nobody suitable is asked again an hour later.
        public void EnsureLink(Person p)
        {
            long now = Store.LastGameMin, day = now > 0 ? now / 1440 : -1;
            if (p == null || p.Home == null || day < 0 || !Lasting(p.Id))
                return;
            string kind;
            int stage;
            bool good;
            if (!InnerLife.WorryNow(p, day, out kind, out stage, out good) || !Links.Linkable(kind))
                return;
            long arc = InnerLife.ArcFor(p.Id, day);
            if (Links.Of(Store.Links, p.Id, kind, arc) != null)
                return;
            string key = p.Id + "|" + arc;
            long until;
            if (_noLinkUntil.TryGetValue(key, out until) && now < until)
                return;
            Resident other = Links.Choose(p, kind, arc, Store.Residents, Store.Ties, Voice.NowYear);
            if (other == null)
            {
                _noLinkUntil[key] = now + 60;
                if (_noLinkUntil.Count > 300)
                    _noLinkUntil.Clear();
                return;
            }
            Store.Links.Add(new WorryLink { Id = p.Id, Name = p.Name, Kind = kind, Arc = arc, OtherId = other.Id, OtherName = other.Name, Town = p.Home, At = now });
            Links.Prune(Store.Links, day);
            Log("LINK " + p.Name + "'s " + kind + ": " + (kind == "debt" ? "owes " : kind == "feud" ? "at odds with " : "sweet on ") + other.Name + " (a regular in " + p.Home + ")");
        }

        /// The name a worry's {who} takes from its link, for InnerLife (reads only).
        private string LinkedName(Person p, string kind, long arc)
        {
            WorryLink l = Links.Of(Store.Links, p.Id, kind, arc);
            return l != null ? Links.WhoName(l) : null;
        }

        /// Whether the one linked to this worry has died (InnerLife then sets the worry aside).
        private bool LinkedDead(Person p, string kind, long arc)
        {
            WorryLink l = Links.Of(Store.Links, p.Id, kind, arc);
            return l != null && HasDied(l.OtherId);
        }

        private static readonly string[] EverydayMen =
        {
            "Wonder if the stage'll be late again.", "Lord, these boots. Should've had them resoled in the spring.", "If that horse throws a shoe one more time...",
            "Two bits for a shave. Highway robbery, that's what it is.", "Ought to write my brother. Been meaning to since Christmas.", "Smells like rain. Smells like money for the feed store.",
            "Mighty hot for this early in the day.", "Wonder what's for supper. Beans, probably. Always beans.", "Owe Hank a dollar. Hope he forgets. He won't.",
            "One more year and I'm going to California. Said that last year, too."
        };

        private static readonly string[] EverydayWomen =
        {
            "Need to get these eggs home before they cook in the sun.", "If that man tracks mud in one more time...", "The hem's coming down again. Third time this month.",
            "Wonder if the post brought anything from Mother.", "Flour's gone up a penny. Again.", "That preacher talks too long. God forgive me, he does.",
            "Ought to start the bread before the light goes.", "Pretty bonnet in the store window. Pretty price, too.", "Those boys are up to something. They always are.",
            "One of these days I'll see the ocean. One of these days."
        };

        public string Summary()
        {
            int alive = _byId.Values.Count(x => x.IsAlive);
            if (alive == 0)
                return null;
            int knowName = Store.People.Values.Count(s => s.KnowsName);
            int grudge = _byId.Values.Count(x => x.IsAlive && x.Hate >= 40);
            int sweet = Store.People.Values.Count(s => s.Heart >= 2);
            int rumours = Store.TownRumours.Count(r => Store.LastGameMin - r.At < 4320);
            int woke = Cfg.WestWorld ? Store.Reveries.Values.Count(r => r.Woke) : 0;
            int aware = !Cfg.WestWorld ? 0 : _byId.Values.Count(x => x.IsAlive && StageOf(x) == 0 && Hosts.Tier(Hosts.Awareness(x, Store.Find(x.Id))) >= 1);
            List<string> parts = new List<string> { Voice.Number(alive) + (alive == 1 ? " life" : " lives") };
            if (knowName > 0) parts.Add(Voice.Number(knowName) + (knowName == 1 ? " knows" : " know") + " your name");
            if (grudge > 0) parts.Add(Voice.Number(grudge) + (grudge == 1 ? " holds" : " hold") + " a grudge");
            if (sweet > 0) parts.Add(Voice.Number(sweet) + " sweet on you");
            if (rumours > 0) parts.Add(Voice.Number(rumours) + (rumours == 1 ? " rumour" : " rumours") + " on the road");
            if (woke > 0) parts.Add(Voice.Number(woke) + " back from the dead");
            if (aware > 0) parts.Add(Voice.Number(aware) + " wondering what's real");
            int waking = Cfg.WestWorld ? WakingCount() : 0;
            if (waking > 0) parts.Add(Voice.Number(waking) + (waking == 1 ? " is" : " are") + " waking up");
            return string.Join(" - ", parts);
        }

        // ---- extra things said face to face -----------------------------------------------------------

        /// What someone says when the player passes (with their temper in the voice).
        public Remark RemarkFor(Person p, long now)
        {
            Remark r = RemarkForCore(p, now);
            if (r != null && !r.Uncanny && !r.Muttered)
                r.Text = Temper.Color(p, r.Text, p.Id + r.Key + now / 60);
            return r;
        }

        private List<string> WithExtras(Person p, PersonState st, List<string> lines, long now)
        {
            PlayerRef you = Talk.RefFor(p, st, Player);
            string seed = p.Id + "|x|" + st.Talks;
            Reverie rv;
            if (Cfg.WestWorld && Store.Reveries.TryGetValue(p.Id, out rv) && rv.Woke && !st.Has("told:reverie"))
            {
                st.Mark("told:reverie");
                lines.Insert(0, Hosts.ReverieAloud(rv, you, seed));
                return lines;
            }
            Deed d = st.KnownDeeds.Count > 0 ? Store.Deeds.LastOrDefault(x => st.KnownDeeds.Contains(x.Id) && !st.Has("told:" + x.Id)) : null;
            if (d != null && lines.Count < 4 && Voice.Chance(seed + "deed", 60))
            {
                st.Mark("told:" + d.Id);
                string r = st.SawDeeds.Contains(d.Id) ? Deeds.Remark(d, seed) : Deeds.HeardRemark(d, seed);
                if (r != null)
                    lines.Add(r);
            }
            if (Cfg.HostsQuestionReality && lines.Count < 4 && Voice.Chance(seed + "host", Cfg.RealityChance / 2 + Hosts.Awareness(p, st) / 3))
                lines.Add(Hosts.Aloud(p, st, you, seed));
            // temper colours one line of the conversation, not every line
            List<int> spoken = Enumerable.Range(0, lines.Count).Where(i => !lines[i].StartsWith("YOU:")).ToList();
            if (spoken.Count > 0)
            {
                int pick = spoken[Voice.Hash(seed + "tp") % spoken.Count];
                lines[pick] = Temper.Color(p, lines[pick], seed + pick);
            }
            return lines;
        }

        private Remark EarlyRemark(Person p, PersonState st, PlayerRef you, string seed, long now)
        {
            Reverie rv;
            if (Cfg.WestWorld && Store.Reveries.TryGetValue(p.Id, out rv) && rv.Woke && !st.Has("rem:reverie"))
            {
                st.Mark("rem:reverie");
                return new Remark { Key = "reverie", Text = Hosts.ReverieAloud(rv, you, seed), Uncanny = true };
            }
            return null;
        }

        private Remark MidRemark(Person p, PersonState st, PlayerRef you, string seed, long now)
        {
            Deed d = st.KnownDeeds.Count > 0 ? Store.Deeds.LastOrDefault(x => st.KnownDeeds.Contains(x.Id) && !st.Has("rem:" + x.Id) && now - x.At < 10080) : null;
            if (d != null)
            {
                st.Mark("rem:" + d.Id);
                string r = st.SawDeeds.Contains(d.Id) ? Deeds.Remark(d, seed) : Deeds.HeardRemark(d, seed);
                if (r != null)
                    return new Remark { Key = "deed:" + d.Id, Text = r, Muttered = p.Fear >= 40 || p.Personality == "Coward" };
            }
            if (Cfg.HostsQuestionReality && now - st.LastMusingAt > 720 && Voice.Chance(seed + "m" + now / 60, Cfg.RealityChance / 2 + Hosts.Awareness(p, st) / 4))
            {
                st.LastMusingAt = now;
                return new Remark { Key = "musing", Text = Hosts.Aloud(p, st, you, seed + now), Uncanny = true };
            }
            return null;
        }
    }
}
