// Town regulars in the game (see Core/Regulars.cs). Every two seconds in a town, the ordinary people the game has put
// in the street who have never been on screen since they appeared, who are 20 m or more off and out of sight, and whom
// enRAGE hasn't named or fingerprinted yet, are cast as the town's regulars: one met before, of the same build, comes
// back, dressed as they were; or, while the town has room, someone new becomes a regular and keeps an outfit of their
// own for good. Everyone around at load (or after a reload) counts as seen, so nobody changes clothes in front of the
// player. Never during story missions, never enRAGE's people, a script's people, the counter keepers or children.
using System;
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private readonly HashSet<int> _everSeen = new HashSet<int>();        // handles that have been on screen
        private readonly Dictionary<int, uint> _seenModel = new Dictionary<int, uint>();   // to tell a reused handle
        private readonly HashSet<int> _regChecked = new HashSet<int>();      // handles already considered
        private readonly Dictionary<uint, int> _outfitsOf = new Dictionary<uint, int>();
        private readonly HashSet<string> _metThisTime = new HashSet<string>();   // regular id | handle: counted once
        private readonly Dictionary<string, int> _regGoneAt = new Dictionary<string, int>();   // regular id -> when their last body streamed out
        private const int RegularGoneMs = 180000;   // a regular whose body went comes back no sooner than this (standing alone every body is free to cast, and one came back 36 times in an hour: 2026-10-29)
        private int _nextSeenSample, _nextRegulars, _nextSeenPrune;
        private bool _regPrimed, _regToldDress;
        private Action _aRegulars;

        private void RegularsStep(int now)
        {
            if (_aRegulars == null)
                _aRegulars = () => RegularsTick(_now);
            Safe("town regulars", _aRegulars);
        }

        private void RegularsTick(int now)
        {
            if (_regBlips.Count > 0)
                ExpireRegularBlips(now);
            if (!_cfg.Regulars || _engine == null || _store == null)
                return;
            Ped me = Game.Player.Character;
            if (now >= _nextSeenSample)
            {
                _nextSeenSample = now + 500;
                SampleSeen(me, now);
            }
            if (now < _nextRegulars)
                return;
            _nextRegulars = now + 2000;
            if (!_regPrimed)
            {
                // anyone around now may have been seen a moment ago (before a reload, say): never them
                _regPrimed = true;
                foreach (Ped p in World.GetAllPeds())
                    _regChecked.Add(p.Handle);
                return;
            }
            string town = _placeTown;
            if (town == null || MISC.GET_MISSION_FLAG())
                return;
            bool dress = _cfg.RegularsDress && _bridge.CanTellLooked;
            if (_cfg.RegularsDress && !dress && !_regToldDress)
            {
                _regToldDress = true;
                Log.Write("REGULARS: not dressed in their own clothes (enRAGE can't be asked whom it has looked at); names and lives are kept");
            }
            int cast = 0;
            long gameNow = Bridge.Now();
            foreach (Ped p in World.GetAllPeds())
            {
                if (cast >= 3)
                    break;
                int h = p.Handle;
                if (_regChecked.Contains(h) || _everSeen.Contains(h) || _resBind.ContainsKey(h))
                    continue;
                float d = p.Position.DistanceTo(me.Position);
                if (d > 150f)
                    continue;   // later, when they're nearer
                _regChecked.Add(h);
                if (d < 20f || ENTITY.IS_ENTITY_ON_SCREEN(h) || ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(me.Handle, h, 17))
                    continue;   // too close, or in sight: left as the game made them
                if (ENTITY.IS_ENTITY_A_MISSION_ENTITY(h) || ResidentWhy(p) != null || PostKindOf(p) != null || _bridge.Looked(h))
                    continue;
                uint model = ENTITY.GET_ENTITY_MODEL(h);
                bool woman = !PED.IS_PED_MALE(h);
                Resident r = Regulars.Back(_store.Residents, town, model, woman, x => _resBind.Values.Any(v => v.R == x) || RegularGone(x, now));
                bool fresh = r == null;
                if (fresh)
                {
                    int outfits = dress ? OutfitsOf(h, model) : Regulars.PerModel;
                    int outfit = Regulars.NewOutfit(_store.Residents, town, model, outfits, _cfg.RegularsPerTown, h + "|" + gameNow);
                    if (outfit < 0)
                        continue;   // the town is full, or this build has its share: a passer-by, as before
                    r = _engine.MeetResident(model, woman, town, ResidentTrade(p, woman), h + "|" + Game.GameTime, gameNow);
                    r.Regular = true;
                    r.Outfit = outfit;
                }
                _engine.KeepResident(r, gameNow);
                _resBind[h] = new ResBind { R = r, Model = model };
                if (dress)
                {
                    PED._EQUIP_META_PED_OUTFIT_PRESET(h, r.Outfit, false);
                    PED._UPDATE_PED_VARIATION(h, false, true, true, true, false);
                }
                cast++;
                PersonState st = _store.Find(r.Id);
                Log.Write("REGULAR " + (fresh ? "new" : "back") + ": " + r.Name + " (" + r.Trade + ") in " + town + (dress ? ", outfit " + r.Outfit : "") +
                          ", " + (int)d + " m off, unseen" + (st != null && st.Sightings > 0 ? ", met " + st.Sightings + " time" + (st.Sightings == 1 ? "" : "s") + " before" : ""));
            }
        }

        /// A regular whose last body streamed out within RegularGoneMs: left alone for now, so they don't hop from body to body.
        private bool RegularGone(Resident r, int now)
        {
            int at;
            return _regGoneAt.TryGetValue(r.Id, out at) && now - at < RegularGoneMs;
        }

        /// Notes who has been on screen; a handle the game has given to someone new (another model) starts over.
        private void SampleSeen(Ped me, int now)
        {
            foreach (Ped p in World.GetAllPeds())
            {
                int h = p.Handle;
                uint model = ENTITY.GET_ENTITY_MODEL(h);
                uint was;
                if (_seenModel.TryGetValue(h, out was) && was != model)
                {
                    _everSeen.Remove(h);
                    _regChecked.Remove(h);
                }
                _seenModel[h] = model;
                if (!_everSeen.Contains(h) && ENTITY.IS_ENTITY_ON_SCREEN(h) && p.Position.DistanceTo(me.Position) < 300f)
                    _everSeen.Add(h);
            }
            if (now >= _nextSeenPrune)
            {
                _nextSeenPrune = now + 10000;
                foreach (int h in _seenModel.Keys.Where(x => !ENTITY.DOES_ENTITY_EXIST(x)).ToList())
                {
                    _seenModel.Remove(h);
                    _everSeen.Remove(h);
                    _regChecked.Remove(h);
                }
                _metThisTime.RemoveWhere(k => !ENTITY.DOES_ENTITY_EXIST(int.Parse(k.Substring(k.LastIndexOf('|') + 1))));
            }
        }

        private int OutfitsOf(int h, uint model)
        {
            int n;
            if (!_outfitsOf.TryGetValue(model, out n))
            {
                n = PED.GET_NUM_META_PED_OUTFITS(h);
                _outfitsOf[model] = n;
            }
            return n;
        }

        private const long RegularMeetGapMin = 360;   // game hours apart before the same regular counts as met again

        /// The player is close to a regular: counted once each time they're met (for "him again" thoughts and the log).
        /// A meeting is one stay near them: a new body for the same regular in the same visit, or a look after a few
        /// minutes, doesn't count again, only coming back after six game hours away (Lex's players, 2026-10-28:
        /// "I know that face" on what was a first meeting).
        private int NoteRegularMet(Resident r, int handle, Person p)
        {
            PersonState st = _store.Get(p);
            long now = Bridge.Now();
            bool away = st.LastSeenAt == 0 || now - st.LastSeenAt >= RegularMeetGapMin || now < st.LastSeenAt;
            if (_metThisTime.Add(r.Id + "|" + handle) && away)
            {
                st.Sightings++;
                _engine.KeepResident(r, now);
            }
            st.LastSeenAt = now;
            return st.Sightings;
        }

        private readonly List<KeyValuePair<Blip, int>> _regBlips = new List<KeyValuePair<Blip, int>>();   // F7's marks (regulars, the awake), taken off at

        /// F7: a grey mark on the map, with their name, on each town regular around you for a minute, and the nearest
        /// one named on screen with how far and which way. A test aid, so a regular can be found and looked at again.
        private void MarkRegulars()
        {
            ClearRegularBlips();
            if (!_cfg.Regulars || _placeTown == null)
                return;
            Ped me = Game.Player.Character;
            var here = _resBind.Where(kv => kv.Value.R.Regular && !kv.Value.R.Dead && ENTITY.DOES_ENTITY_EXIST(kv.Key) && !ENTITY.IS_ENTITY_DEAD(kv.Key))
                .Select(kv => new { H = kv.Key, R = kv.Value.R, D = new Ped(kv.Key).Position.DistanceTo(me.Position) })
                .Where(x => x.D < 200f).OrderBy(x => x.D).Take(12).ToList();
            int until = Game.GameTime + 60000;
            foreach (var x in here)
            {
                Blip b = new Ped(x.H).AddBlip(BlipType.CompanionGray);
                b.Label = x.R.Name;
                _regBlips.Add(new KeyValuePair<Blip, int>(b, until));
            }
            Hint(here.Count == 0 ? "No town regulars around you right now."
                : "Nearest regular: " + here[0].R.Name + ", " + (int)here[0].D + " m " + Compass(me.Position, new Ped(here[0].H).Position) +
                  " (" + here.Count + " shown on your map for a minute)", 9000);
        }

        private void ExpireRegularBlips(int now)
        {
            foreach (KeyValuePair<Blip, int> kv in _regBlips.Where(x => now >= x.Value).ToList())
            {
                try
                {
                    if (kv.Key.Exists())
                        kv.Key.Delete();
                }
                catch
                {
                }
                _regBlips.Remove(kv);
            }
        }

        private void ClearRegularBlips()
        {
            ExpireRegularBlips(int.MaxValue);
        }

        /// Which way from one place to another: "north-east" (the map's +Y is north, +X east).
        private static string Compass(Vector3 from, Vector3 to)
        {
            double deg = Math.Atan2(to.X - from.X, to.Y - from.Y) * 180.0 / Math.PI;
            if (deg < 0)
                deg += 360;
            string[] names = { "north", "north-east", "east", "south-east", "south", "south-west", "west", "north-west" };
            return names[(int)Math.Round(deg / 45.0) % 8];
        }

        /// The regulars of the town you're in, for the F7 dump: the ones around now first.
        private string RegularsStatus()
        {
            if (!_cfg.Regulars)
                return "off";
            string town = _placeTown;
            string all = Regulars.Count(_store.Residents);
            if (town == null)
                return "kept: " + all + "; none around (no town here)";
            Ped me = Game.Player.Character;
            List<string> here = _resBind.Where(kv => kv.Value.R.Regular && kv.Value.R.Town == town && ENTITY.DOES_ENTITY_EXIST(kv.Key))
                .OrderBy(kv => new Ped(kv.Key).Position.DistanceTo(me.Position)).Take(8)
                .Select(kv =>
                {
                    PersonState st = _store.Find(kv.Value.R.Id);
                    return kv.Value.R.Name + " (" + kv.Value.R.Trade + ", outfit " + kv.Value.R.Outfit + ", met " + (st != null ? st.Sightings : 0) + ", " +
                           (int)new Ped(kv.Key).Position.DistanceTo(me.Position) + " m)";
                }).ToList();
            return "kept: " + all + "; around you in " + town + ": " + (here.Count == 0 ? "none yet" : string.Join("; ", here));
        }
    }
}
