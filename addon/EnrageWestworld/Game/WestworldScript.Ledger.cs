// The people ledger's record of deaths (step 1 of the mind design, the private Claude Doc "RealNPCs: the NPC Mind").
// Every death of someone RealNPCs knows near the player goes into Store.Deaths: who, when, where, how and by whose hand.
// A scan every half second remembers who the known people are while they live, so a body enRAGE has already let go of
// still has a name, and follows enRAGE's knocked-out rule, so nobody out cold is buried. A townsperson enRAGE still
// hasn't marked dead 4 s later is marked so for it ([Mind] ShareWithEnrage): enRAGE notices deaths only in a sync every
// 2 s while the body is there, and missed Pope McCrae when the area unloaded a second after he died.
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
        private sealed class Known
        {
            public string Id, Name, Kind;
            public uint Model;
            public int CheckedAt;
        }

        private sealed class LedgerDue
        {
            public string Id, Name;
            public int At;
        }

        private const float LedgerRange = 150f;
        private const int ShareAfterMs = 4000, UnshareAfterMs = 30000, NobodyRecheckMs = 10000;

        private readonly Dictionary<int, Known> _known = new Dictionary<int, Known>();
        private readonly HashSet<int> _deadNoted = new HashSet<int>();
        private readonly List<LedgerDue> _shareDue = new List<LedgerDue>(), _unshareDue = new List<LedgerDue>();
        private int _nextLedger, _ledgerNoted, _ledgerShared, _ledgerUnshared;

        // the knocked-out probe: anyone near the player the game has down but enRAGE's rule calls only out (fists, a gun
        // butt, incapacitated), followed until they get up, are gone, or die where they lie; to learn what the game does
        private sealed class Downed
        {
            public string Name, Why;
            public int Since, Left;
        }

        private readonly Dictionary<int, Downed> _downed = new Dictionary<int, Downed>();
        private int _cameTo, _diedDown, _goneDown;
        private DeathEntry _ledgerLast;
        private Action _aLedger;

        private void LedgerStep(int now)
        {
            if (_aLedger == null)
                _aLedger = () => LedgerTick(_now);
            Safe("people ledger", _aLedger);
        }

        private void LedgerTick(int now)
        {
            if (_engine == null || _store == null || _bridge == null || now < _nextLedger)
                return;
            _nextLedger = now + 500;
            if (MISC.GET_MISSION_FLAG())
                return;   // story missions are left alone, deaths in them too
            Ped me = Game.Player.Character;
            Vector3 at = me.Position;
            foreach (Ped p in World.GetAllPeds())
            {
                if (p == null || !p.Exists() || p.Handle == me.Handle || !PED.IS_PED_HUMAN(p.Handle) || p.Position.DistanceTo(at) > LedgerRange)
                    continue;
                int h = p.Handle;
                if (p.Position.DistanceTo(at) < 60f)
                    WatchDowned(p, now);
                Known k = KnownAt(p, now);
                if (k == null || _deadNoted.Contains(h) || !ENTITY.IS_ENTITY_DEAD(h) || KnockedOutWhy(h) != null)
                    continue;   // out cold is looked at again next time: they may be killed where they lie
                _deadNoted.Add(h);
                LedgerNote(h, k.Id, k.Name, k.Kind, null, null);
            }
            foreach (int h in _known.Keys.Where(x => !ENTITY.DOES_ENTITY_EXIST(x)).ToList())
            {
                _known.Remove(h);
                _deadNoted.Remove(h);
            }
            foreach (int h in _downed.Keys.Where(x => !ENTITY.DOES_ENTITY_EXIST(x)).ToList())
            {
                Downed d = _downed[h];
                _downed.Remove(h);
                _goneDown++;
                Log.Write("LEDGER lost " + d.Name + ": down " + (now - d.Since) / 1000 + " s (" + d.Why + "), gone from the world before getting up");
            }
            LedgerSharesDue(now);
        }

        /// Who this ped is, if RealNPCs knows them: a resident (not a counter's keeper, whose posts code says what became
        /// of them), one of enRAGE's records, or one of its townsfolk. Looked up once per handle (again when the model
        /// changes, or every 10 s while they're nobody, since they may be named later); never children or story characters.
        private Known KnownAt(Ped p, int now)
        {
            int h = p.Handle;
            uint model = ENTITY.GET_ENTITY_MODEL(h);
            Known k;
            if (_known.TryGetValue(h, out k) && k.Model == model && (k.Id != null || now - k.CheckedAt < NobodyRecheckMs))
                return k.Id != null ? k : null;
            k = new Known { Model = model, CheckedAt = now };
            _known[h] = k;
            if (Natives.StoryModel(p) || Natives.IsChild(p) || PostKindOf(p) != null)
                return null;
            ResBind rb;
            if (_resBind.TryGetValue(h, out rb) && rb.Model == model)
            {
                if (rb.R.PostId == null)
                {
                    k.Id = rb.R.Id;
                    k.Name = rb.R.Name;
                    k.Kind = "resident";
                }
                return k.Id != null ? k : null;
            }
            NemesisRecord rec = _bridge.RecordAt(h);
            if (rec != null)
            {
                k.Id = rec.Id;
                k.Name = rec.Name;
                k.Kind = "record";
                return k;
            }
            Acquaintance a = _bridge.AcquaintanceAt(h);
            if (a != null && !string.IsNullOrEmpty(a.Name))
            {
                Folk f = Snapshot.Of(a);
                k.Id = f.Key;
                k.Name = f.Name;
                k.Kind = "folk";
                return k;
            }
            return null;
        }

        /// The knocked-out probe for one ped: noted when they go down out cold, then whether they get up or die there.
        private void WatchDowned(Ped p, int now)
        {
            int h = p.Handle;
            Downed d;
            bool down = ENTITY.IS_ENTITY_DEAD(h) || PED.IS_PED_INCAPACITATED(h);
            string why = down ? (ENTITY.IS_ENTITY_DEAD(h) ? KnockedOutWhy(h) : "incapacitated") : null;
            if (_downed.TryGetValue(h, out d))
            {
                if (!down)
                {
                    _downed.Remove(h);
                    _cameTo++;
                    Log.Write("LEDGER came to: " + d.Name + " got up after " + (now - d.Since) / 1000 + " s (" + d.Why + ")");
                }
                else if (why == null)
                {
                    _downed.Remove(h);
                    _diedDown++;
                    Log.Write("LEDGER died where they lay: " + d.Name + ", down " + (now - d.Since) / 1000 + " s (" + d.Why + ")");
                }
                return;
            }
            if (why == null || Natives.IsChild(p))
                return;
            Known k;
            string name = _known.TryGetValue(h, out k) && k.Id != null ? k.Name : Director.Kind(p, !PED.IS_PED_MALE(h));
            int left = PED._GET_INCAPACITATION_TIME_REMAINING(h);
            _downed[h] = new Downed { Name = name, Why = why, Since = now, Left = left };
            Log.Write("LEDGER out cold: " + name + " (" + why + "; the game's incapacitation time left " + left + ")");
        }

        /// Why a ped the game calls dead is only knocked out (enRAGE's rule, DeathLedger.KnockedOut), or null if dead.
        private string KnockedOutWhy(int h)
        {
            bool incapacitated = PED.IS_PED_INCAPACITATED(h);
            bool gunButt = _bridge != null && _bridge.KnockedOutByGunButt(new Ped(h));
            uint cause = PED.GET_PED_CAUSE_OF_DEATH(h);
            uint group = cause != 0 ? WEAPON.GET_WEAPONTYPE_GROUP(cause) : 0u;
            if (!DeathLedger.KnockedOut(incapacitated, gunButt, group))
                return null;
            return incapacitated ? "incapacitated" : gunButt ? "a gun butt" : "fists";
        }

        /// The one way a death goes into the ledger from the game side (Engine.NoteDied also marks a resident dead). how
        /// and by come from the game when not given. A townsperson's death is checked against enRAGE 4 s later.
        private void LedgerNote(int h, string id, string name, string kind, string how, string by)
        {
            if (id == null || _engine == null)
                return;
            ResBind rb;
            if (_resBind.TryGetValue(h, out rb) && rb.R.Id == id)
                rb.R.Dead = true;   // the bound one, saved or not (Engine.NoteDied finds only the saved)
            Ped me = Game.Player.Character;
            bool there = ENTITY.DOES_ENTITY_EXIST(h);
            Vector3 pos = there ? new Ped(h).Position : me.Position;
            int source = there ? PED.GET_PED_SOURCE_OF_DEATH(h) : 0;
            if (how == null && there)
            {
                uint cause = PED.GET_PED_CAUSE_OF_DEATH(h);
                how = cause != 0 ? Weapons.Phrase(cause) : null;
            }
            if (by == null)
                by = DeathLedger.ByOf(source, me.Handle, h);
            Known killer;
            DeathEntry e = new DeathEntry
            {
                Id = id, Name = name, Kind = kind, At = Bridge.Now(), Town = _bridge.TownAt(pos) ?? ZoneAt(pos), X = pos.X, Y = pos.Y, Z = pos.Z, How = how, By = by,
                ById = by == "another" && _known.TryGetValue(source, out killer) ? killer.Id : null,
                Seen = there && ENTITY.IS_ENTITY_ON_SCREEN(h) && pos.DistanceTo(me.Position) < 60f
            };
            if (!_engine.NoteDied(e))
                return;
            // one of enRAGE's people dead after one of our street fights (by another's hand, not the player's): its record
            // says how (RecordFates)
            int sceneAt;
            if (kind == "record" && there && by == "another" && _sceneAtHandle.TryGetValue(h, out sceneAt) && Game.GameTime - sceneAt < 120000)
            {
                NemesisRecord fought = _bridge.RecordAt(h);
                if (fought != null)
                    QueueRecordFate(fought, name, "Died after a street fight" + (e.Town != null ? ", in " + e.Town : string.Empty) + ".");
            }
            _ledgerNoted++;
            _ledgerLast = e;
            Log.Write("LEDGER died: " + DeathLedger.Describe(e));
            if (there)
            {
                AwakeSawDeath(h, pos);   // the fully awake who see it say so
                NoteDeathMinds(h, e);    // and whoever saw it remembers it (Minds)
            }
            if (kind == "folk")
                _shareDue.Add(new LedgerDue { Id = id, Name = name, At = Game.GameTime + ShareAfterMs });
        }

        /// Townsfolk deaths enRAGE hasn't marked by now are marked for it; deaths a reload undid are taken back from it.
        private void LedgerSharesDue(int now)
        {
            if (_store.RewoundShares != null && _store.RewoundShares.Count > 0)
            {
                foreach (string id in _store.RewoundShares)
                    _unshareDue.Add(new LedgerDue { Id = id, At = now + UnshareAfterMs });
                _store.RewoundShares.Clear();
            }
            for (int i = _shareDue.Count - 1; i >= 0; i--)
            {
                LedgerDue d = _shareDue[i];
                if (now < d.At)
                    continue;
                _shareDue.RemoveAt(i);
                if (!_cfg.ShareWithEnrage)
                    continue;
                if (_bridge.FolkDead(d.Id))
                {
                    Log.Detail("LEDGER enRAGE saw " + d.Name + " die itself");
                    continue;
                }
                if (!_bridge.MarkFolkDead(d.Id, true))
                    continue;
                DeathEntry e = _store.DeathOf(d.Id);
                if (e != null)
                    e.SharedWithEnrage = true;
                _ledgerShared++;
                Log.Write("LEDGER told enRAGE: " + d.Name + " died (enRAGE hadn't seen it)");
            }
            for (int i = _unshareDue.Count - 1; i >= 0; i--)
            {
                LedgerDue d = _unshareDue[i];
                if (now < d.At)
                    continue;
                _unshareDue.RemoveAt(i);
                // enRAGE's own save point usually puts its people back on a reload; only if it didn't, and our ledger no
                // longer has the death, is the mark we made taken back
                if (_store.DeathOf(d.Id) == null && _bridge.FolkDead(d.Id) && _bridge.MarkFolkDead(d.Id, false))
                {
                    _ledgerUnshared++;
                    Log.Write("LEDGER took back from enRAGE: " + d.Id + "'s death (a reload undid it)");
                }
            }
        }

        /// F7: what the ledger holds, what this session added and shared, the last death, the known people near.
        private string LedgerStatus()
        {
            if (_store == null)
                return "not ready";
            Vector3 at = Game.Player.Character.Position;
            int near = _known.Count(kv => kv.Value.Id != null && ENTITY.DOES_ENTITY_EXIST(kv.Key) && new Ped(kv.Key).Position.DistanceTo(at) < 50f);
            return "knocked out now " + _downed.Count + ", got up " + _cameTo + ", died where they lay " + _diedDown + ", gone while down " + _goneDown +
                   "; deaths kept " + DeathLedger.Counts(_store.Deaths) + "; this session noted " + _ledgerNoted + ", told enRAGE " + _ledgerShared + ", taken back " + _ledgerUnshared +
                   " (ShareWithEnrage " + (_cfg.ShareWithEnrage ? "on" : "off") + "); waiting " + _shareDue.Count + "; known people within 50 m " + near + "; last: " + DeathLedger.Describe(_ledgerLast);
        }
    }
}
