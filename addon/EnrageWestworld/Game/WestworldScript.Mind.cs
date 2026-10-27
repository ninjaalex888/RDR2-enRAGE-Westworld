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
            public Person P;                // as the add-on knows them (for the worry, the goals)
            public NeedState Needs;         // worked out when looked at, again after NeedsMs (step 3)
            public List<Concern> Concerns;
            public int NeedsAt;
        }

        private const int NeedsMs = 30000;   // needs move by the game hour: worked out again every 30 s (15 game minutes)

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
            MindAppraiseStep(now);   // one person a frame from the happening being appraised
        }

        private int _mindFelt, _mindEvCursor, _mindEvStale;
        private string _mindLastFelt;
        private Happening _mindEv;            // the happening being appraised, one person a frame
        private string _mindEvDoer;           // who really did it (the pair's tie), or null
        private int _mindEvSaw;               // how many saw it (not those who only heard, not the player)
        private readonly List<KeyValuePair<string, bool>> _mindEvPeople = new List<KeyValuePair<string, bool>>();   // id, only heard it

        /// Step 4: each person in a happening, or who saw or heard it, whom the mind knows near the player now, appraises
        /// it (Core's Appraisal): a feeling, its strength and its urge, kept for their mood (in memory), stamped with the
        /// happening's own time. One person a frame; one MIND line per person per event, within the 20 a minute (the old
        /// two-word mood is worked out only for a line that will be written, and the same line says where it and the
        /// mind's disagree). A happening more than 10 game minutes old when its turn comes (a mission, a fade) is dropped:
        /// whoever is near by then didn't live it. Nobody is tasked; with MindLive off nothing reaches play.
        private void MindAppraiseStep(int now)
        {
            long g = _store.LastGameMin;
            if (_mindEv == null || _mindEvCursor >= _mindEvPeople.Count)
            {
                _mindEv = _engine.Shadow.Next();
                if (_mindEv == null)
                    return;
                if (g - _mindEv.At > 10 || g < _mindEv.At)
                {
                    _mindEvStale++;
                    _mindEv = null;
                    return;
                }
                MindAppraiseStart(_mindEv);
                return;
            }
            KeyValuePair<string, bool> who = _mindEvPeople[_mindEvCursor++];
            MindAppraiseOne(_mindEv, who.Key, who.Value, now);
        }

        private void MindAppraiseStart(Happening ev)
        {
            _mindEvPeople.Clear();
            _mindEvCursor = 0;
            _mindEvSaw = 0;
            _mindEvDoer = null;
            // who did it to whom: the scene's cast is by who speaks first, so the pair's tie says (one-sided kinds)
            if (ev.DoerId != null && ev.TargetId != null)
                foreach (SceneTie t in _store.Ties)
                    if (t.Has(ev.DoerId) && t.Has(ev.TargetId))
                    {
                        _mindEvDoer = t.Doer;
                        break;
                    }
            var seen = new HashSet<string>();
            Action<string, bool> add = (id, heard) =>
            {
                if (id != null && id != "player" && !(ev.Kind == "death" && id == ev.TargetId) && seen.Add(id))
                    _mindEvPeople.Add(new KeyValuePair<string, bool>(id, heard));
            };
            add(ev.TargetId, false);
            add(ev.DoerId, false);
            if (ev.Witnesses != null)
                foreach (EventWitness wt in ev.Witnesses)
                {
                    if (wt.How == "saw" && wt.Id != "player")
                        _mindEvSaw++;
                    add(wt.Id, wt.How != "saw");
                }
        }

        private void MindAppraiseOne(Happening ev, string id, bool heardOnly, int now)
        {
            MindWho w = MindWhoById(id);
            if (w == null || w.Traits == null || w.P == null)
                return;   // only people near the player now whom the mind already knows (the doc's level 0)
            Func<string, string, int> warmth = (x, y) =>
            {
                foreach (SceneTie t in _store.Ties)
                    if (t.Has(x) && t.Has(y))
                        return t.Warmth;
                return 0;
            };
            AppraisalEvent ae = Appraisal.From(ev, id, w.P.Woman, w.P.Home, warmth, _mindEvDoer, heardOnly, _mindEvSaw);
            if (ae == null)
                return;
            Feeling f = Appraisal.Of(ae, w.Traits);
            if (f.Kind == "none")
                return;
            _engine.Shadow.Feel(id, f, ev.At);
            _mindFelt++;
            string about = ae.Doer == "player" && f.About == ev.DoerName ? "you" : f.About;
            _mindLastFelt = w.Name + ": " + f.Kind + (about != null ? " at " + about : "");
            if (!MindLogRoom(now))
            {
                _mindLogHeld++;
                return;
            }
            long g = _store.LastGameMin;
            PadMood b = Mood.Baseline(w.Traits), m = _engine.Shadow.MoodOf(id, w.Traits, g);
            string mind = Mood.SoreOrGlad(m, b), old = _cfg.MindLive ? null : _engine.MoodToday(w.P, w.P.Home ?? _placeTown);
            MindLog(now, "MIND " + w.Name + " (" + (w.Src.Temper ?? "plain") + "): " + f.Kind + " (" + f.Strength + ")" + (about != null ? " at " + about : "") + ", urge " + f.Urge + ", shows " + Display.Shows(f, ae, w.Traits) + " (" + MindCanShow(f, w) + ")" +
                         " <- " + ev.Kind + " (" + ae.Role + (heardOnly ? ", only heard it" : "") + ", " + ae.Witnesses + " saw); mood " + Mood.Words(m, b) +
                         (!_cfg.MindLive && old != mind ? "; the old mood says " + (old ?? "nothing") + ", the mind's " + (mind ?? "nothing") : ""));
        }

        /// What the game is proven able to show of it (Core/Catalogue.cs), its first entry by name, or "the thought alone".
        private static string MindCanShow(Feeling f, MindWho w)
        {
            foreach (Expression x in Catalogue.For(f.Kind, f.Urge, f.Strength, w.P.Woman, true))
                return x.Channel + " " + (x.Name.Contains("/") ? x.Name.Substring(x.Name.LastIndexOf('/') + 1).Trim() : x.Name);
            return "the thought alone";
        }

        /// Whether a MIND line could be written now (the 20 a minute), without writing one.
        private bool MindLogRoom(int now)
        {
            return now - _mindLogMinuteAt >= 60000 || now < _mindLogMinuteAt || _mindLogCount < MindLinesAMinute;
        }

        /// Someone known to the mind by id, alive and near the player this second (in this second's scan).
        private MindWho MindWhoById(string id)
        {
            foreach (int h in _mindNear)
            {
                MindWho w;
                if (_mindWho.TryGetValue(h, out w) && w.Id == id)
                    return ENTITY.DOES_ENTITY_EXIST(h) && !ENTITY.IS_ENTITY_DEAD(h) ? w : null;
            }
            return null;
        }

        private void MindShadowForget()
        {
            _mindEngine = _engine;
            _mindGen = _engine.Shadow.Generation;
            _mindWho.Clear();
            _mindNear.Clear();
            _mindTraitsTold.Clear();
            _mindCursor = 0;
            _mindEv = null;
            _mindEvPeople.Clear();
            _mindFelt = 0;
            _mindLastFelt = null;   // nothing from a rewound future in F7
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
            if (t == null)
                return;
            if (w.Needs == null || now - w.NeedsAt >= NeedsMs || now < w.NeedsAt)
                MindNeeds(w, h, now);
            if (w.Told || _mindTraitsTold.Contains(w.Id))
                return;
            // one line a person a session, marked told only once it's written (the 20 a minute may hold it back); an
            // enRAGE townsperson with no temper has what would be shared on the same line (a dry run: nothing written)
            string share = !w.Src.TemperDecided && t.Label != null ? "; share (dry run): enRAGE gave no temper, RealNPCs would give " + t.Label + ", nothing written" : "";
            if (MindLog(now, "MIND traits: " + w.Name + " (" + w.Kind + ", " + (w.Src.Temper ?? (w.Src.TemperDecided ? "plain" : "no temper given")) + "): " + t.Describe() +
                        (t.Nudged ? "; moved to fit their temper" : "") + (t.Mismatch ? "; THE TEMPER DOESN'T READ BACK" : "") + share +
                        "; needs: " + w.Needs.Words() + " (" + w.Needs.Numbers() + "); " + Needs.Words(w.Concerns)))
            {
                w.Told = true;
                if (_mindTraitsTold.Count < 2000)
                    _mindTraitsTold.Add(w.Id);
            }
        }

        /// Their needs and concerns now (step 3): the clock, the weather and the place as the add-on reads them, their
        /// memories (read only, never made), whether they're in their cups, whether they saw the player do violence.
        private void MindNeeds(MindWho w, int h, int now)
        {
            w.NeedsAt = now;
            long g = _store.LastGameMin;
            PersonState st = _store.Find(w.Id);
            var c = new NeedContext
            {
                Hour = CLOCK.GET_CLOCK_HOURS(), Minute = CLOCK.GET_CLOCK_MINUTES(), Weekday = InnerLife.Weekday, GameDay = g > 0 ? g / 1440 : -1, Now = g,
                Weather = _engine.NowWeather, Indoors = _engine.NowIndoors, Place = _engine.NowPlace, Others = Math.Max(0, _mindNear.Count - 1),
                Drunk = InHisCups(new Ped(h), w.P), Feared = _engine.SawYouViolent(w.Id, 3), WakeStage = st != null ? st.WakeStage : 0, Memories = st != null ? st.Mind : null
            };
            w.Needs = Needs.Of(w.P, w.Traits, c);
            w.Concerns = Needs.Of(w.P, c.GameDay, c.GameDay >= 0 ? Links.Against(_store.Links, w.Id, c.GameDay) : null);
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
                w.P = rp;
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
                    w.P = fp;
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
                        w.P = res;
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

        /// Their mood against their own baseline and the last thing they felt ("sore and keyed up, last felt anger at you"),
        /// or "even" with nothing felt lately.
        private string MindFeelingWords(MindWho w)
        {
            long g = _store != null ? _store.LastGameMin : 0;
            List<FeltAt> felt = _engine.Shadow.Felt(w.Id);
            PadMood b = Mood.Baseline(w.Traits), m = Mood.Now(b, felt, g);
            string mood = Mood.Words(m, b);
            if (felt == null || felt.Count == 0)
                return mood;
            FeltAt last = felt[felt.Count - 1];
            return mood + ", last felt " + Feeling.Kinds[last.Kind] + (last.About != null ? " at " + last.About : "") + (g - last.At >= 0 ? " " + MindAgo(g - last.At) : "");
        }

        private static string MindAgo(long minutes)
        {
            return minutes < 60 ? minutes + " min ago" : minutes < 1440 ? minutes / 60 + " h ago" : minutes / 1440 + " d ago";
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
                return "mind: " + w.Traits.Words(2) + (w.Traits.Label != null ? " (" + w.Traits.Label.ToLowerInvariant() + ")" : "") + "; " + MindFeelingWords(w) +
                       (w.Needs != null ? "; " + w.Needs.Words() : "") + (w.Concerns != null && w.Concerns.Count > 0 ? "; " + w.Concerns[0].Words : "");
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
                    // Lex's example: "Earl Adair, proud and anxious, sore, worried about the bank, last felt anger at Arthur"
                    near.Add(w.Name + ", " + w.Traits.Words(2) + (w.Traits.Label != null ? " (reads as " + w.Traits.Label + ")" : " (plain)") + ", " + MindFeelingWords(w) +
                             (w.Needs != null ? ", " + w.Needs.Words() : "") + (w.Concerns != null ? ", " + Needs.Words(w.Concerns) : ""));
                }
                return "on, " + (_cfg.MindLive ? "LIVE (sore and glad from the mind)" : "reporting only") + "; " + _mindNear.Count + " people within " + (int)MindMetres + " m, " + _mindLooks + " looks, traits worked out for " +
                       (_engine != null ? _engine.Shadow.TraitsMade + " (" + _engine.Shadow.Count + " kept)" : "nobody") + ", " + _mindLogged + " MIND lines, " + _mindLogHeld + " held back by the 20 a minute; events appraised " + _mindFelt + " feelings from " + _engine.Shadow.EventsQueued + " happenings (" + _engine.Shadow.Waiting + " waiting, " + _engine.Shadow.EventsDropped + " dropped from a full queue, " + _mindEvStale + " too old by their turn), last: " + (_mindLastFelt ?? "none") +
                       (near.Count > 0 ? "; near: " + string.Join("; ", near) : "; nobody near it knows");
            }
            catch (Exception ex)
            {
                return "couldn't say (" + ex.Message + ")";
            }
        }
    }
}
