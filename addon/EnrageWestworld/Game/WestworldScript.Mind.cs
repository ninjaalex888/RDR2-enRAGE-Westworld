// The NPC mind in shadow mode (daytime run 2026-10-27; the mind design, steps 2-4): for people near the player who
// already have an identity (enRAGE's records and townsfolk, residents bound to their body), the mind works out their
// traits (Core/Traits.cs) and reports them: the log (MIND lines, at most 20 a minute and one per person per event), F7
// (TEST mind shadow) and a line on the Show minds cards (a testing tool). It reads only: no task, animation, outfit or
// damage, nothing written to enRAGE and nothing to the save. It never makes anyone (no ResidentOf, MeetResident,
// PersonOfFolk or Store.Get): someone with no identity is passed over. What it decides reaches play only with
// [Mind] MindLive. Cost: one look at the world's peds a second (within 20 m), then two people a frame from that list,
// cached by handle; the card and F7 read only that cache.
using System;
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using NemesisSystem;
using RDR2;
using RDR2.Math;
using RDR2.Native;
using Traits = EnrageWestworld.Core.Traits;   // enRAGE has a Traits of its own

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private sealed class MindWho
        {
            public string Id, Name, Kind;   // Id null: nobody the add-on knows (looked at again after a while)
            public TraitSource Src;
            public Traits Traits;           // worked out when first looked at (the engine's cache by id), kept here so a look allocates nothing
            public uint Model;
            public int CheckedAt;
            public bool Told;               // their MIND line is written this session
        }

        private const float MindMetres = 20f;          // the mind's near ring (the doc's level 0)
        private const int MindWhoMs = 10000;           // nobody we know: looked up again after this
        private const int MindKnownMs = 30000;         // somebody: again after this (the game reuses a handle for another of the same model)
        private const int MindLinesAMinute = 20;
        private readonly Dictionary<int, MindWho> _mindWho = new Dictionary<int, MindWho>();
        private readonly List<int> _mindNear = new List<int>();
        private readonly List<KeyValuePair<int, float>> _mindScan = new List<KeyValuePair<int, float>>();
        private readonly HashSet<string> _mindTraitsTold = new HashSet<string>();
        private int _mindNextScan, _mindCursor, _mindGen = -1, _mindLogMinuteAt, _mindLogCount, _mindLogHeld, _mindLooks, _mindLogged;
        private Engine _mindEngine;
        private Action _aMindShadow;

        private void MindShadowStep(int now)
        {
            if (_cfg == null || !_cfg.MindShadow || _engine == null || _store == null)
                return;
            if (_aMindShadow == null)
                _aMindShadow = () => MindShadowTick(_now);
            Safe("mind shadow", _aMindShadow);
        }

        private void MindShadowTick(int now)
        {
            if (_mindEngine != _engine || _engine.Shadow.Generation != _mindGen)
                MindShadowForget();   // a new save, or an older one loaded: what was worked out goes
            if (now >= _mindNextScan)
            {
                _mindNextScan = now + 1000;
                MindScan();
                return;   // the scan's frame does nothing else
            }
            for (int k = 0; k < 2 && _mindCursor < _mindNear.Count; k++)
                MindLook(_mindNear[_mindCursor++], now);
        }

        private void MindShadowForget()
        {
            _mindEngine = _engine;
            _mindGen = _engine.Shadow.Generation;
            _mindWho.Clear();
            _mindNear.Clear();
            _mindTraitsTold.Clear();
            _mindCursor = 0;
        }

        /// The living people within MindMetres, nearest first (reused lists; the world's ped array is the one allocation).
        private void MindScan()
        {
            Ped me = Game.Player.Character;
            _mindNear.Clear();
            _mindScan.Clear();
            _mindCursor = 0;
            if (me == null || !me.Exists())
                return;
            Vector3 at = me.Position;
            foreach (Ped p in World.GetAllPeds())
            {
                int h = p.Handle;
                if (h == me.Handle)
                    continue;
                float d = p.Position.DistanceTo(at);
                if (d > MindMetres || ENTITY.IS_ENTITY_DEAD(h) || !PED.IS_PED_HUMAN(h))
                    continue;
                _mindScan.Add(new KeyValuePair<int, float>(h, d));
            }
            _mindScan.Sort((a, b) => a.Value.CompareTo(b.Value));
            for (int i = 0; i < _mindScan.Count; i++)
                _mindNear.Add(_mindScan[i].Key);
            if (_mindWho.Count > 400)
                _mindWho.Clear();   // handles come and go; the cache is rebuilt from who's near
        }

        private void MindLook(int h, int now)
        {
            MindWho w = ENTITY.DOES_ENTITY_EXIST(h) ? MindKnown(h, now) : null;   // gone since the scan: passed over
            if (w == null || w.Id == null)
                return;
            _mindLooks++;
            Traits t = w.Traits ?? (w.Traits = _engine.Shadow.TraitsOf(w.Src));
            if (t == null || w.Told || _mindTraitsTold.Contains(w.Id))
                return;
            // one line a person a session, marked told only once it's written (the 20 a minute may hold it back); an
            // enRAGE townsperson with no temper has what would be shared on the same line (a dry run: nothing written)
            string share = !w.Src.TemperDecided && t.Label != null ? "; share (dry run): enRAGE gave no temper, RealNPCs would give " + t.Label + ", nothing written" : "";
            if (MindLog(now, "MIND traits: " + w.Name + " (" + w.Kind + ", " + (w.Src.Temper ?? (w.Src.TemperDecided ? "plain" : "no temper given")) + "): " + t.Describe() +
                        (t.Nudged ? "; moved to fit their temper" : "") + (t.Mismatch ? "; THE TEMPER DOESN'T READ BACK" : "") + share))
            {
                w.Told = true;
                if (_mindTraitsTold.Count < 2000)
                    _mindTraitsTold.Add(w.Id);
            }
        }

        /// Who this body is to the mind, from what the add-on already knows; cached by handle, looked up again when the
        /// model changes, after 10 s for nobody and 30 s for somebody. Never makes anyone.
        private MindWho MindKnown(int h, int now)
        {
            uint model = ENTITY.GET_ENTITY_MODEL(h);
            MindWho w;
            if (_mindWho.TryGetValue(h, out w) && w.Model == model && now - w.CheckedAt < (w.Id == null ? MindWhoMs : MindKnownMs))
                return w;
            MindWho was = w;
            w = new MindWho { Model = model, CheckedAt = now };
            _mindWho[h] = w;
            Ped p = new Ped(h);
            if (Natives.StoryModel(p) || !IsGrownUp(p))
                return w;
            NemesisRecord rec = _bridge.RecordAt(h);
            if (rec != null)
            {
                if (!rec.IsAlive)
                    return w;
                Person rp = PersonOf(rec);
                w.Id = rp.Id;
                w.Name = rp.Name;
                w.Kind = "record";
                w.Src = TraitSource.From(rp, "record");
            }
            else
            {
                Acquaintance ac = _bridge.AcquaintanceAt(h);
                if (ac != null && !string.IsNullOrEmpty(ac.Name))
                {
                    Folk f = Snapshot.Of(ac);
                    var fp = new Person { Id = f.Key, Name = f.Name, Woman = f.Woman, Home = f.Town, Personality = f.Temper };   // not PersonOfFolk: that makes their state
                    w.Id = f.Key;
                    w.Name = f.Name;
                    w.Kind = "folk";
                    w.Src = TraitSource.From(fp, "folk");
                }
                else
                {
                    ResBind b;
                    if (_resBind.TryGetValue(h, out b) && b.R != null && !b.R.Dead)
                    {
                        Person res = _engine.PersonOfResident(b.R);
                        w.Id = res.Id;
                        w.Name = res.Name;
                        w.Kind = "resident";
                        w.Src = TraitSource.From(res, "resident");
                    }
                }
            }
            if (was != null && was.Id != null && was.Id == w.Id)
                w.Told = was.Told;   // the same person looked up again
            return w;
        }

        /// A MIND line, within the budget (20 a minute); the rest are counted and said in F7. True when written.
        private bool MindLog(int now, string line)
        {
            if (now - _mindLogMinuteAt >= 60000 || now < _mindLogMinuteAt)
            {
                _mindLogMinuteAt = now;
                _mindLogCount = 0;
            }
            if (_mindLogCount >= MindLinesAMinute)
            {
                _mindLogHeld++;
                return false;
            }
            _mindLogCount++;
            _mindLogged++;
            Log.Write(line);
            return true;
        }

        /// The Show minds card's line for this body: their traits in words and the temper they read as. Only what the
        /// mind's own step already worked out (no lookup here, nothing the card's own guard could trip over).
        private string MindCardLine(int h)
        {
            try
            {
                MindWho w;
                if (_cfg == null || !_cfg.MindShadow || !_mindWho.TryGetValue(h, out w) || w.Id == null || w.Traits == null)
                    return null;
                return "mind: " + w.Traits.Words(2) + (w.Traits.Label != null ? " (" + w.Traits.Label.ToLowerInvariant() + ")" : "");
            }
            catch
            {
                return null;
            }
        }

        /// F7: what the shadow has done, and the nearest three it knows, in words (from its cache; never throws).
        private string MindShadowStatus()
        {
            try
            {
                if (!_cfg.MindShadow)
                    return "off ([Mind] MindShadow)";
                var near = new List<string>();
                foreach (int h in _mindNear)
                {
                    if (near.Count >= 3)
                        break;
                    MindWho w;
                    if (!_mindWho.TryGetValue(h, out w) || w.Id == null || w.Traits == null)
                        continue;
                    near.Add(w.Name + ", " + w.Traits.Words(2) + (w.Traits.Label != null ? " (reads as " + w.Traits.Label + ")" : " (plain)"));
                }
                return "on, " + (_cfg.MindLive ? "LIVE" : "reporting only") + "; " + _mindNear.Count + " people within " + (int)MindMetres + " m, " + _mindLooks + " looks, traits worked out for " +
                       (_engine != null ? _engine.Shadow.TraitsMade + " (" + _engine.Shadow.Count + " kept)" : "nobody") + ", " + _mindLogged + " MIND lines, " + _mindLogHeld + " held back by the 20 a minute" +
                       (near.Count > 0 ? "; near: " + string.Join("; ", near) : "; nobody near it knows");
            }
            catch (Exception ex)
            {
                return "couldn't say (" + ex.Message + ")";
            }
        }
    }
}
