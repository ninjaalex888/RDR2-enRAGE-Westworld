// What the player does, read from the game: who he hurts and kills (and with what), what he loots, which
// crimes are seen, which towns and shops he walks into. Turned into deeds, deaths for the reveries,
// diaries to pick up, letters to collect, and the town talk waiting for him when he rides in.
using System;
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using NemesisSystem;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private const uint EventDamaged = 402722103u;
        private const uint EventLootComplete = 1376140891u;
        private const uint EventCrimeConfirmed = 1924269094u;

        private sealed class Hit
        {
            public int At;
            public uint Weapon;
            public bool Fighting;   // the victim was fighting the player when hit
            public bool Done;
            public string RecordId; // who enRAGE says they are (read while alive: enRAGE forgets dead peds fast)
            public string AcqName;
        }

        private sealed class Pickup
        {
            public string PersonId, Name;
            public Vector3 Pos;
            public float Radius;
            public int Until;
            public string From;     // "body" or "camp"
        }

        private readonly Dictionary<int, Hit> _hits = new Dictionary<int, Hit>();
        private readonly HashSet<string> _campOffered = new HashSet<string>();
        private Pickup _pickup;
        private int _nextPlaces, _nextDeaths, _lastCrimeAt;
        private string _lastCrimeKind;
        private uint _interiorType;
        private string _placeKind;
        private string _placeTown;
        private List<CampInfo> _camps;
        private Dictionary<uint, string> _interiorKinds;

        private static readonly string[] PostTowns = { "Valentine", "Rhodes", "Saint Denis", "Blackwater", "Strawberry", "Annesburg", "Van Horn", "Armadillo", "Tumbleweed", "Emerald Ranch", "Flatneck Station", "Riggs Station", "Benedict Point", "Lagras" };

        // ---- game events (every frame: they only live for one) ------------------------------------

        private unsafe void PollWorldEventsImpl(int now)
        {
            int count = SCRIPTS.GET_NUMBER_OF_EVENTS(0);
            if (count <= 0)
                return;
            int player = Game.Player.Character.Handle;
            ulong* p = stackalloc ulong[12];
            for (int i = 0; i < count; i++)
            {
                uint ev = SCRIPTS.GET_EVENT_AT_INDEX(0, i);
                if (ev != EventDamaged && ev != EventLootComplete && ev != EventCrimeConfirmed)
                    continue;
                for (int j = 0; j < 12; j++)
                    p[j] = 0;
                int size = ev == EventDamaged ? 9 : 3;
                if (!SCRIPTS.GET_EVENT_DATA(0, i, p, size))
                    continue;
                try
                {
                    if (ev == EventDamaged)
                    {
                        int victim = (int)p[0];
                        if ((int)p[1] == player && victim != 0 && victim != player)
                        {
                            Hit h;
                            if (!_hits.TryGetValue(victim, out h))
                            {
                                h = new Hit { Fighting = WasFight(victim, player) };
                                if (PED.IS_PED_HUMAN(victim))
                                {
                                    NemesisRecord who = _bridge.RecordAt(victim);
                                    h.RecordId = who != null ? who.Id : null;
                                    Acquaintance acq = who == null ? _bridge.AcquaintanceAt(victim) : null;
                                    h.AcqName = acq != null && !string.IsNullOrEmpty(acq.Name) ? acq.FullName : null;
                                }
                                _hits[victim] = h;
                            }
                            h.At = now;
                            h.Weapon = (uint)p[2];
                        }
                    }
                    else if (ev == EventLootComplete)
                    {
                        if ((int)p[0] == player && (p[2] & 0xFFFFFFFF) != 0)
                            OnLooted((int)p[1], now);
                    }
                    else if ((int)p[1] == player)
                    {
                        OnCrime((uint)p[0], (int)p[2], now);
                    }
                }
                catch (Exception ex)
                {
                    Log.Detail("event " + ev + " failed: " + ex.Message);
                }
            }
        }

        /// A fight, not a murder: they were fighting him, or armed and fighting anyone, or hostile to him
        /// (rival gangs, the law hunting him), or it's a story mission.
        private static bool WasFight(int victim, int player)
        {
            if (PED.IS_PED_IN_COMBAT(victim, player) || MISC.GET_MISSION_FLAG())
                return true;
            if (PED.IS_PED_IN_COMBAT(victim, 0) && WEAPON.IS_PED_ARMED(victim, 4))
                return true;
            return PED.GET_RELATIONSHIP_BETWEEN_PEDS(victim, player) == 5;   // hate: rival gangs and the like
        }

        /// Checks the people the player hurt: who died, how.
        private void Deaths(int now)
        {
            if (now < _nextDeaths || _hits.Count == 0)
                return;
            _nextDeaths = now + 400;
            int player = Game.Player.Character.Handle;
            foreach (KeyValuePair<int, Hit> kv in _hits.ToList())
            {
                int h = kv.Key;
                Hit hit = kv.Value;
                if (hit.Done || now - hit.At > 20000 || !ENTITY.DOES_ENTITY_EXIST(h))
                {
                    if (now - hit.At > 20000 || !ENTITY.DOES_ENTITY_EXIST(h))
                        _hits.Remove(h);
                    continue;
                }
                if (!ENTITY.IS_ENTITY_DEAD(h) || now - hit.At < 300)
                    continue;
                hit.Done = true;
                int source = PED.GET_PED_SOURCE_OF_DEATH(h);
                if (source != player && source != 0 && now - hit.At > 3000)
                    continue;
                uint cause = PED.GET_PED_CAUSE_OF_DEATH(h);
                if (!hit.Fighting && (MISC.GET_MISSION_FLAG() || PED.IS_PED_IN_COMBAT(h, player)))
                    hit.Fighting = true;   // a mission, or it turned into a fight after the first shot
                OnKilled(new Ped(h), hit, cause != 0 ? cause : hit.Weapon, now);
            }
        }

        private void OnKilled(Ped victim, Hit hit, uint weapon, int now)
        {
            int h = victim.Handle;
            if (Spawner.IsOurs(h))
                return;
            Vector3 pos = victim.Position;
            string town = _bridge.TownAt(pos);
            string zone = ZoneAt(pos);
            long gameMin = Bridge.Now();
            if (!PED.IS_PED_HUMAN(h))
            {
                if (_cfg.Deeds && ENTITY.GET_IS_ANIMAL(h))
                {
                    string animal = AnimalName(victim);
                    if (animal != null)
                        _engine.OnDeed(Deeds.Make("hunt", animal, town, zone, gameMin, pos.X, pos.Y, pos.Z, 0), null, gameMin);
                }
                return;
            }
            if (Natives.IsChild(victim))
                return;
            string how = Weapons.Phrase(weapon);
            int bone = Natives.LastBone(h);
            bool headshot = bone == 21030 || bone == 31086 || ((BoneID)bone).ToString().Contains("HEAD");
            string place = town ?? zone;
            List<string> witnesses = WitnessesNear(pos, h, 35f);
            int seen = CountPeopleNear(pos, h, 35f);
            NemesisRecord rec = null;
            if (hit.RecordId == null || !_recById.TryGetValue(hit.RecordId, out rec))
                rec = _bridge.RecordAt(h);
            if (rec != null)
            {
                _engine.NoteDeath(rec.Id, place, how, headshot);
                if (_cfg.Deeds)
                    _engine.OnDeed(Deeds.Make("namedkill", rec.Name, town, zone, gameMin, pos.X, pos.Y, pos.Z, seen), witnesses, gameMin);
                Log.Write("KILLED " + rec.Name + (how != null ? " with " + how : "") + (headshot ? " (head)" : "") + " near " + place);
                return;
            }
            bool woman = !PED.IS_PED_MALE(h);
            bool law = Natives.IsLaw(victim);
            Acquaintance a = _bridge.AcquaintanceAt(h);
            string known = a != null && !string.IsNullOrEmpty(a.Name) ? a.FullName : hit.AcqName;
            string label = law ? "a lawman" : Director.Kind(victim, woman);
            bool thief = _thieves.Remove(h);
            string beggarName = null;
            bool begged = !thief && DiedBegging(h, now, out beggarName);
            if (begged && known == null)
                known = beggarName;
            if (_cfg.Deeds)
            {
                string kind = law ? "lawkill" : thief ? "kill" : begged ? "execution" : hit.Fighting ? "kill" : "murder";
                Deed d = Deeds.Make(kind, thief ? "a pickpocket" : begged ? label : known ?? label, town, zone, gameMin, pos.X, pos.Y, pos.Z, seen);
                Deed last = _store.Deeds.LastOrDefault();
                if (last != null && last.Kind == kind && gameMin - last.At <= 10 && last.Where == d.Where)
                {
                    last.Count++;   // two men in the same fight
                    last.Weight = Math.Min(100, last.Weight + 8);
                }
                else
                {
                    _engine.OnDeed(d, witnesses, gameMin);
                }
            }
            // story missions and story characters stay out of the reveries: nobody from a mission walks again
            if (_cfg.Reveries && !MISC.GET_MISSION_FLAG() && !Natives.StoryModel(victim))
            {
                Fallen f = new Fallen
                {
                    Id = Ids.New(), Model = ENTITY.GET_ENTITY_MODEL(h), Woman = woman, Name = known,
                    Label = label, Town = town, Zone = zone, X = pos.X, Y = pos.Y, Z = pos.Z, Weapon = how, Headshot = headshot, At = gameMin, Rolled = true,
                    Seen = seen, Begged = begged
                };
                if (!law && _rng.Next(100) < _cfg.StrangerReturnChance)
                    f.ReturnAt = gameMin + 1440 + _rng.Next(1440 * 5);
                _store.Fallen.Add(f);
                Log.Detail("FALLEN " + (f.Name ?? f.Label) + " near " + place + (f.ReturnAt > 0 ? " - will come back" : ""));
            }
        }

        private void OnLooted(int looted, int now)
        {
            if (looted == 0 || !ENTITY.DOES_ENTITY_EXIST(looted) || !PED.IS_PED_HUMAN(looted))
                return;
            NemesisRecord rec = _bridge.RecordAt(looted);
            if (rec != null && _cfg.FindDiaries)
            {
                PersonState st = _store.Find(rec.Id);
                if (st != null && st.Diary.Count > 0 && !st.DiaryFound)
                {
                    _pickup = new Pickup { PersonId = rec.Id, Name = rec.Name, Pos = new Ped(looted).Position, Radius = 5f, Until = now + 30000, From = "body" };
                    Hint("You find a diary on " + rec.Name + ".  [" + _readerKey + "] take it", 30000);
                }
            }
        }

        private void OnCrime(uint crime, int witness, int now)
        {
            string kind = CrimeKind(crime);
            if (kind == null || !_cfg.Deeds)
                return;
            if (kind == _lastCrimeKind && now - _lastCrimeAt < 120000)
                return;
            _lastCrimeKind = kind;
            _lastCrimeAt = now;
            Ped me = Game.Player.Character;
            Vector3 pos = me.Position;
            long gameMin = Bridge.Now();
            string detail = kind == "robbery" ? (_placeKind != null ? "the " + PlaceName(_placeKind) : Voice.Pick(now.ToString(), "a traveller", "a farmer", "a townsman"))
                : kind == "assault" || kind == "kidnap" ? "a stranger" : null;
            int seen = Math.Max(witness != 0 ? 1 : 0, CountPeopleNear(pos, 0, 35f));
            _engine.OnDeed(Deeds.Make(kind, detail, _bridge.TownAt(pos), ZoneAt(pos), gameMin, pos.X, pos.Y, pos.Z, seen), WitnessesNear(pos, 0, 35f), gameMin);
        }

        private readonly Dictionary<string, long> _flirtNoted = new Dictionary<string, long>();

        /// Sweet-talking someone is a deed too: whoever saw it talks, and it travels.
        private void NoteFlirt(Ped ped, string who)
        {
            if (!_cfg.Deeds || _engine == null || ped == null || who == null)
                return;
            long g = Bridge.Now();
            long last;
            if (_flirtNoted.TryGetValue(who, out last) && g - last < 240)
                return;
            _flirtNoted[who] = g;
            Vector3 pos = ped.Position;
            _engine.OnDeed(Deeds.Make("flirt", who, _bridge.TownAt(pos), ZoneAt(pos), g, pos.X, pos.Y, pos.Z, CountPeopleNear(pos, ped.Handle, 20f)),
                WitnessesNear(pos, ped.Handle, 20f), g);
        }

        private static string CrimeKind(uint crime)
        {
            switch (crime)
            {
                case 0x72ADE410: return "theft";
                case 0xBE3A5838: return "horsetheft";
                case 0x82F7E4A2: return "horsetheft";
                case 0xA3BEDE4C: return "robbery";
                case 0x6A1ADE3D: return "bankrobbery";
                case 0xFC738E61: return "stagecoach";
                case 0x647D2A5A: return "trainrobbery";
                case 0x0BADC882: return "assault";
                case 0x5098CC5A: return "assault";
                case 0x55AD2BEB: return "looting";
                case 0x971EA5AF: return "graverobbing";
                case 0x98F908DB: return "kidnap";
                case 0x68134DC7: return "arson";
                case 0xA2FF1145: return "cheating";
                case 0xA54C77E0: return "theft";
                default: return null;
            }
        }

        private List<string> WitnessesNear(Vector3 pos, int except, float radius)
        {
            List<string> ids = new List<string>();
            foreach (Ped p in World.GetAllPeds())
            {
                int h = p.Handle;
                if (h == except || h == Game.Player.Character.Handle || ENTITY.IS_ENTITY_DEAD(h) || p.Position.DistanceTo(pos) > radius)
                    continue;
                NemesisRecord r = _bridge.RecordAt(h);
                if (r != null && r.IsAlive && r.Id != null)
                    ids.Add(r.Id);
            }
            return ids;
        }

        private static int CountPeopleNear(Vector3 pos, int except, float radius)
        {
            int n = 0;
            int me = Game.Player.Character.Handle;
            foreach (Ped p in World.GetAllPeds())
            {
                int h = p.Handle;
                if (h != except && h != me && !ENTITY.IS_ENTITY_DEAD(h) && PED.IS_PED_HUMAN(h) && p.Position.DistanceTo(pos) <= radius)
                    n++;
            }
            return n;
        }

        private static string AnimalName(Ped p)
        {
            string m = Natives.ModelName(p);
            if (!m.StartsWith("a_c_") || m.Contains("horse") || m.Contains("dog") || m.Contains("cat_") || m.Contains("fish"))
                return null;
            string n = m.Substring(4);
            int u = n.IndexOf('_');
            n = u > 0 ? n.Substring(0, u) : n;
            switch (n)
            {
                case "buck": return "buck";
                case "bearblack": return "black bear";
                case "bighornram": return "bighorn ram";
                case "cranewhooping": return "whooping crane";
                case "goosecanada": return "goose";
                case "turkeywild": return "wild turkey";
                case "snakeblacktailrattle": case "snakeferdelance": case "snakeredboa": case "snakewater": return "snake";
                case "lionmangy": return "mountain lion";
                case "cougar": case "panther": return "big cat";
                case "boarlegendary": return "boar";
                default: return n;
            }
        }

        private string ZoneAt(Vector3 pos)
        {
            string z = _bridge.ZoneName(pos);
            return z == "the wilds" ? null : z;
        }

        // ---- places ---------------------------------------------------------------------------------

        private void Places(int now)
        {
            Deaths(now);
            if (now < _nextPlaces)
                return;
            _nextPlaces = now + 1500;
            Ped me = Game.Player.Character;
            Vector3 pos = me.Position;
            string town = _bridge.TownAt(pos);
            if (town != _placeTown)
            {
                string old = _placeTown;
                _placeTown = town;
                _town = town;
                _engine.CurrentTown = town;
                if (town != null)
                    EnteredTown(town, old);
            }
            uint interior = Natives.InteriorType(me.Handle);
            if (interior != _interiorType)
            {
                _interiorType = interior;
                _placeKind = interior != 0 ? InteriorKind(interior) : null;
                if (_placeKind != null && _cfg.Deeds)
                {
                    long g = Bridge.Now();
                    _engine.OnDeed(Deeds.Make(_placeKind, null, town, ZoneAt(pos), g, pos.X, pos.Y, pos.Z, CountPeopleNear(pos, 0, 25f)), WitnessesNear(pos, 0, 25f), g);
                }
            }
            if (_cfg.FindDiaries && interior == 0)
                CampDiary(pos, now);
        }

        private void EnteredTown(string town, string from)
        {
            long now = Bridge.Now();
            long last;
            bool firstVisit = !_store.TownVisits.ContainsKey(town);
            bool fresh = !_store.TownVisits.TryGetValue(town, out last) || now - last > 720;
            _store.TownVisits[town] = now;
            bool settlement = Rumours.Towns.ContainsKey(town);   // enRAGE also names regions ("the Heartlands"); only towns get a welcome
            if (fresh && settlement)
                QueueArrival(town, firstVisit);
            if (fresh && settlement && _cfg.Deeds)
            {
                Vector3 pos = Game.Player.Character.Position;
                _engine.OnDeed(Deeds.Make("visit", town, town, null, now, pos.X, pos.Y, pos.Z, 0), null, now);
            }
            if (_cfg.RumoursTravel)
                foreach (string line in _engine.TownGossip(town, 2))
                    News("In " + town, line);
            if (_cfg.Letters && PostTowns.Contains(town))
            {
                List<Paper> waiting = _store.Papers.Where(p => !p.Delivered && (p.Town == null || p.Town == town)).ToList();
                if (waiting.Count > 0)
                {
                    foreach (Paper p in waiting)
                    {
                        p.Delivered = true;
                        p.Town = town;
                        p.At = now;
                    }
                    string what = waiting.Count == 1 ? "a letter" : Voice.Number(waiting.Count) + " letters";
                    News("Post office", "The clerk in " + town + " had " + what + " for you" + (waiting.Count == 1 && waiting[0].From != null ? ", from " + waiting[0].From : "") + ". [" + _readerKey + "] to read.");
                    Log.Write("LETTERS collected in " + town + ": " + string.Join(", ", waiting.Select(p => p.Title)));
                }
            }
        }

        private string InteriorKind(uint type)
        {
            if (_interiorKinds == null)
            {
                _interiorKinds = new Dictionary<uint, string>();
                Action<string, string[]> add = (kind, names) =>
                {
                    foreach (string n in names)
                        _interiorKinds[Natives.Hash(n)] = kind;
                };
                add("saloon", new[] { "arm_saloon_int", "bla_saloon_int", "eme_saloon_int", "rho_saloon_int", "tum_saloon_int", "val_saloon_int", "val_saloon2_int", "van_saloon_int", "new_saloon01_int_main", "new_saloon02_int", "new_gamble_int" });
                add("genstore", new[] { "arm_genstore_int", "bla_genstore_int", "eme_genstore_int", "rho_genstore_int", "str_genst_int", "tum_genstore_int", "val_genstore", "new_grocery_int", "dis_tradepost_int" });
                add("gunsmith", new[] { "ann_gunsmith_int", "rho_gunsmith_int", "tum_gunsmith_int", "val_gunsmith_int_main", "new_gunshop_int" });
                add("bank", new[] { "arm_bank_int", "bla_bank_int", "rho_bank_int", "val_bank_int_main", "new_com_bank_int" });
                add("sheriff", new[] { "ann_jail_int", "arm_sheriff_int", "bla_jail_int", "rho_sheriff_int", "str_jailpost_int", "tum_jail_int", "val_jail_int", "new_police_int", "bur_calaboose_int", "old_jailsmth_int" });
                add("doctor", new[] { "str_doctor_int", "val_doctor_int", "new_res_doctor_int", "new_pharmacy_int" });
                add("barber", new[] { "bla_barber_int", "new_barber_int" });
                add("bath", new[] { "ann_roomnbath_int", "tum_roomnbath_int", "van_roomnbath_int" });
                add("hotel", new[] { "val_hotel_int_main", "van_hotel_int", "new_hotel_3rd_floor_int" });
                add("station", new[] { "arm_trainstation_int", "bla_train_int", "rho_train_int", "val_trainstn_int", "wal_trainstation_int", "ann_depot_int", "eme_depot_int", "van_depot_int", "swa_depot01_int", "new_traindepot_int", "the_ticketoffice_int", "rig_station_int" });
                add("tailor", new[] { "bla_tailor_int", "new_tailor01_int_main" });
                add("fence", new[] { "bla_fenceshop_int", "rho_fenceshop_int", "van_fenceshop_int", "new_shady_shop_int" });
                add("theatre", new[] { "bla_theater_int", "new_theater_int", "val_magiclantern_int", "new_magic_int" });
                add("photo", new[] { "bla_photo_int", "new_photo_int" });
                add("church", new[] { "dis_1rmchurch_int" });
                add("stable", new[] { "bra_stable_int", "old_stable_int", "val_stable_int", "jum_horseshop_int", "hrs_02_shop01_int" });
                add("store", new[] { "new_exotic_int", "van_exoticgoods_int", "new_art_int" });
            }
            string k;
            return _interiorKinds.TryGetValue(type, out k) ? k : null;
        }

        private static string PlaceName(string kind)
        {
            switch (kind)
            {
                case "genstore": return "general store";
                case "gunsmith": return "gunsmith";
                case "bank": return "bank";
                case "saloon": return "saloon";
                case "tailor": return "tailor";
                case "doctor": return "doctor";
                case "barber": return "barber";
                case "fence": return "fence";
                case "station": return "station";
                case "store": return "shop";
                default: return kind;
            }
        }

        // ---- diaries -------------------------------------------------------------------------------

        private void CampDiary(Vector3 pos, int now)
        {
            if (_camps == null)
                _camps = _bridge.Camps();
            if (_camps.Count == 0 || (_pickup != null && now < _pickup.Until))
                return;
            CampInfo camp = _camps.FirstOrDefault(c => c.Pos.DistanceTo2D(pos) < 22f && Math.Abs(c.Pos.Z - pos.Z) < 15f);
            if (camp == null || camp.Gang == null)
                return;
            string key = camp.Id + "|" + Bridge.Now() / 1440;
            if (_campOffered.Contains(key) || PED.IS_PED_IN_COMBAT(Game.Player.Character.Handle, 0))
                return;
            _campOffered.Add(key);
            NemesisRecord owner = (_bridge.Roster() ?? new List<NemesisRecord>()).Where(r => r != null && r.Gang == camp.Gang && r.Id != null)
                .Select(r => new { r, st = _store.Find(r.Id) }).Where(x => x.st != null && x.st.Diary.Count >= 2 && !x.st.DiaryFound)
                .OrderBy(x => x.r.IsAlive ? 1 : 0).Select(x => x.r).FirstOrDefault();
            if (owner == null)
                return;
            _pickup = new Pickup { PersonId = owner.Id, Name = owner.Name, Pos = camp.Pos, Radius = 30f, Until = now + 60000, From = "camp" };
            Hint("Among the bedrolls lies a worn diary.  [" + _readerKey + "] take it", 20000);
        }

        private void Pickups(int now)
        {
            if (_pickup != null && (now > _pickup.Until || Game.Player.Character.Position.DistanceTo(_pickup.Pos) > _pickup.Radius + 10f))
            {
                _pickup = null;
                _hint = null;
            }
        }

        /// The reader key with a diary at hand takes it, and opens it.
        private bool TakePickup()
        {
            Pickup p = _pickup;
            if (p == null || Game.GameTime > p.Until || Game.Player.Character.Position.DistanceTo(p.Pos) > p.Radius)
                return false;
            _pickup = null;
            _hint = null;
            PersonState st = _store.Find(p.PersonId);
            if (st == null || st.Diary.Count == 0)
                return false;
            st.DiaryFound = true;
            Paper diary = new Paper
            {
                Id = Ids.New(), Kind = "diary", Title = "The diary of " + p.Name, From = p.Name, FromId = p.PersonId,
                Body = string.Join("\n\n", st.Diary.Select(d => d.Text)), At = Bridge.Now(), Delivered = true
            };
            _store.Papers.Add(diary);
            Log.Write("DIARY taken from " + p.Name + "'s " + p.From + " (" + st.Diary.Count + " entries)");
            News("Papers", "You took " + p.Name + "'s diary.");
            if (_cfg.Deeds && p.From == "body")
            {
                Vector3 pos = Game.Player.Character.Position;
                long g = Bridge.Now();
                _engine.OnDeed(Deeds.Make("looting", null, _bridge.TownAt(pos), ZoneAt(pos), g, pos.X, pos.Y, pos.Z, CountPeopleNear(pos, 0, 30f)), WitnessesNear(pos, 0, 30f), g);
            }
            List<Paper> papers = _store.Papers.Where(x => x.Delivered).OrderByDescending(x => x.At).ToList();
            _reader.Show(papers, papers.IndexOf(diary));
            return true;
        }
    }
}
