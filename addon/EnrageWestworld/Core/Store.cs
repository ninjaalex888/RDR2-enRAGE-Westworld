// The add-on's save file, one per player character (arthur.json / john.json), next to the ini.
// DataContract JSON: fields missing from an older file come back null, so Fix() fills them in.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace EnrageWestworld.Core
{
    [DataContract]
    public sealed class Store
    {
        [DataMember] public int Version = 2;
        [DataMember] public string Player = "arthur";
        [DataMember] public long InstalledAt;
        [DataMember] public long LastGameMin;
        [DataMember] public Dictionary<string, PersonState> People = new Dictionary<string, PersonState>();
        [DataMember] public List<Rumor> Rumors = new List<Rumor>();
        [DataMember] public List<string> Announced = new List<string>();
        [DataMember] public List<Affair> Affairs = new List<Affair>();
        [DataMember] public long RomanceAt;

        // 0.4
        [DataMember] public List<Deed> Deeds = new List<Deed>();
        [DataMember] public List<TownRumour> TownRumours = new List<TownRumour>();
        [DataMember] public List<Paper> Papers = new List<Paper>();
        [DataMember] public List<Fallen> Fallen = new List<Fallen>();
        [DataMember] public Dictionary<string, Reverie> Reveries = new Dictionary<string, Reverie>();
        [DataMember] public List<Avenger> Avengers = new List<Avenger>();
        [DataMember] public List<Wedding> Weddings = new List<Wedding>();
        [DataMember] public List<Transfer> Transfers = new List<Transfer>();
        [DataMember] public List<Debt> Debts = new List<Debt>();
        [DataMember] public Dictionary<string, int> Counts = new Dictionary<string, int>();
        [DataMember] public Dictionary<string, long> TownVisits = new Dictionary<string, long>();
        [DataMember] public int LastStalkDay;
        [DataMember] public int LastDayStalkDay = -1;
        [DataMember] public long LastLetterAt;
        [DataMember] public long LastWorldTickAt;
        [DataMember] public bool DeadSeeded;
        [DataMember] public bool SparedSeeded;                 // enRAGE's mercy counts read once, quietly
        [DataMember] public int DiaryDayKey;
        [DataMember] public List<Resident> Residents = new List<Resident>();   // ordinary people something has happened to (see Residents.cs)
        [DataMember] public int NextResident;
        [DataMember] public List<Post> Posts = new List<Post>();   // the counters with one person each, who pass them on (see Posts.cs)

        public long WasAt;

        public PersonState Get(Person p)
        {
            PersonState st;
            if (!People.TryGetValue(p.Id, out st))
            {
                st = new PersonState { Id = p.Id, Name = p.Name };
                People[p.Id] = st;
            }
            st.Name = p.Name;
            return st;
        }

        public const int MaxResidents = 400;

        public Resident FindResident(string id)
        {
            return Residents.FirstOrDefault(r => r.Id == id);
        }

        /// Saves a resident something has happened to. Over the limit, the oldest untouched ones go first.
        public void KeepResident(Resident r, long now)
        {
            r.TouchedAt = now;
            if (!Residents.Contains(r))
                Residents.Add(r);
            while (Residents.Count > MaxResidents)
            {
                // someone keeping a post, or a hermit living at a camp, is never dropped
                Resident drop = Residents.Where(x => x != r && !((x.PostId != null || x.HasCamp) && !x.Dead))
                    .OrderBy(x => { PersonState s = Find(x.Id); return s != null ? s.WakeStage : 0; }).ThenBy(x => x.TouchedAt).First();
                Residents.Remove(drop);
                People.Remove(drop.Id);
            }
        }

        public PersonState Find(string id)
        {
            PersonState st;
            return id != null && People.TryGetValue(id, out st) ? st : null;
        }

        public int Count(string what)
        {
            int n;
            return Counts.TryGetValue(what, out n) ? n : 0;
        }

        public void Bump(string what, int by = 1)
        {
            Counts[what] = Count(what) + by;
        }

        /// An older save was loaded (game time went back): forget what "hasn't happened yet".
        public int Rewind(long now)
        {
            WasAt = LastGameMin;
            long cut = now + 30;
            int n = Rumors.RemoveAll(r => r.SentAt > cut);
            n += Affairs.RemoveAll(a => a.Since > cut);
            if (RomanceAt > now)
                RomanceAt = now;
            foreach (PersonState st in People.Values)
            {
                n += st.Heard.RemoveAll(h => h.At > cut);
                n += st.Diary.RemoveAll(d => d.At > cut);
                if (st.NameAt > cut)
                {
                    st.KnowsName = false;
                    st.NameAt = 0;
                    n++;
                }
                foreach (Relative r in st.Family ?? new List<Relative>())
                    n += r.Heard.RemoveAll(h => h.At > cut);
            }
            n += Deeds.RemoveAll(d => d.At > cut);
            n += TownRumours.RemoveAll(r => r.At > cut);
            n += Papers.RemoveAll(p => p.At > cut);
            n += Fallen.RemoveAll(f => f.At > cut);
            foreach (string id in Reveries.Where(kv => kv.Value.DiedAt > cut).Select(kv => kv.Key).ToList())
            {
                Reveries.Remove(id);
                n++;
            }
            n += Avengers.RemoveAll(a => a.Stage < 2 && a.DueAt > cut + 10080);
            n += Weddings.RemoveAll(w => !w.Done && w.At > cut + 20160);
            n += Debts.RemoveAll(d => d.Since > cut);
            if (LastWorldTickAt > now)
                LastWorldTickAt = now;
            LastGameMin = now;
            return n;
        }

        private static DataContractJsonSerializer Serializer()
        {
            return new DataContractJsonSerializer(typeof(Store), new DataContractJsonSerializerSettings { UseSimpleDictionaryFormat = true });
        }

        public static Store Load(string path, Action<string> log)
        {
            try
            {
                if (File.Exists(path))
                {
                    using (FileStream stream = File.OpenRead(path))
                    {
                        Store s = (Store)Serializer().ReadObject(stream);
                        if (s != null)
                        {
                            s.Fix();
                            return s;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                log?.Invoke("Couldn't read " + path + " (" + ex.Message + ") - starting fresh, old file kept as .bad");
                try
                {
                    File.Copy(path, path + ".bad", true);
                }
                catch
                {
                }
            }
            Store fresh = new Store();
            fresh.Fix();
            return fresh;
        }

        public void Save(string path)
        {
            Trim();
            string tmp = path + ".tmp";
            using (FileStream stream = File.Create(tmp))
                Serializer().WriteObject(stream, this);
            if (File.Exists(path))
                File.Replace(tmp, path, null);
            else
                File.Move(tmp, path);
        }

        /// Keeps the file from growing without end.
        private void Trim()
        {
            Cap(Deeds, 250);
            Cap(TownRumours, 160);
            Cap(Fallen, 150);
            Cap(Debts, 60);
            Cap(Transfers, 30);
            Weddings.RemoveAll(w => w.Done && LastGameMin - w.At > 43200);
            Avengers.RemoveAll(a => a.Stage == 3 && LastGameMin - a.DueAt > 43200);
            List<Paper> kept = Papers.Where(p => p.Delivered).ToList();
            if (kept.Count > 120)
                Papers.RemoveAll(p => p.Delivered && kept.IndexOf(p) < kept.Count - 120);
            if (Reveries.Count > 200)
                foreach (string id in Reveries.OrderBy(kv => kv.Value.DiedAt).Take(Reveries.Count - 200).Select(kv => kv.Key).ToList())
                    Reveries.Remove(id);
        }

        private static void Cap<T>(List<T> list, int max)
        {
            if (list.Count > max)
                list.RemoveRange(0, list.Count - max);
        }

        private void Fix()
        {
            if (People == null) People = new Dictionary<string, PersonState>();
            if (Rumors == null) Rumors = new List<Rumor>();
            if (Announced == null) Announced = new List<string>();
            if (Affairs == null) Affairs = new List<Affair>();
            if (Deeds == null) Deeds = new List<Deed>();
            if (TownRumours == null) TownRumours = new List<TownRumour>();
            if (Papers == null) Papers = new List<Paper>();
            if (Fallen == null) Fallen = new List<Fallen>();
            if (Reveries == null) Reveries = new Dictionary<string, Reverie>();
            if (Avengers == null) Avengers = new List<Avenger>();
            if (Weddings == null) Weddings = new List<Wedding>();
            if (Transfers == null) Transfers = new List<Transfer>();
            if (Debts == null) Debts = new List<Debt>();
            if (Counts == null) Counts = new Dictionary<string, int>();
            if (TownVisits == null) TownVisits = new Dictionary<string, long>();
            if (Residents == null) Residents = new List<Resident>();
            if (Posts == null) Posts = new List<Post>();
            foreach (Post p in Posts)
            {
                if (p.Former == null)
                    p.Former = new List<string>();
                if (p.SawFall == null)
                    p.SawFall = new List<string>();
            }
            foreach (Affair a in Affairs)
                if (a.Said == null)
                    a.Said = new List<string>();
            foreach (PersonState st in People.Values)
                st.Fix();
            foreach (Deed d in Deeds)
                if (d.Count <= 0)
                    d.Count = 1;
        }
    }
}
