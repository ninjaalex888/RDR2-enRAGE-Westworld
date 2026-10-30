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
using System.Diagnostics;
using System.Linq;
using EnrageWestworld.Core;
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
            // the shadow's choosing (night 2026-10-28, item 5): what they'd do now, worked out once a minute, never acted on
            public string TradeGroup;       // InnerLife.TradeGroup(P.Trade), worked out once (the night review: it allocates)
            public string Choice, ChoiceWhy, ChoiceNext, ChoiceHeld;
            public RecentActs Recent;       // the last three brief things and when (a brief thing done lately serves less)
            public bool ChoiceTold;         // its MIND would line is written (a held one is written on the next look while the choice stands)
            public int ChoseAt;
            public long AteAt = -1;         // the shadow's own meal (the choice was "eat"), so hunger falls for it
        }

        private const int ChooseMs = 60000, ChoiceLinesAMinute = 5;
        private int _mindChoosesAbout;   // choices worked out for the about ring (MindLevels on; after item 14)
        private int _mindChooseCursor, _mindChoices, _mindChoiceChanges, _mindChoiceLines, _mindChoiceHeld, _mindChoiceMinuteAt, _mindChoiceMinuteCount;
        private string _mindLastChoice;

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
            if (_cfg.MindLevels)
            {
                MindLevelsTick(now);   // the same work, handed out by the levels scheduler within a budget a frame (item 7)
                return;
            }
            if (now >= _mindNextScan)
            {
                _mindNextScan = now + 1000;
                MindScan();
                return;   // the scan's frame does nothing else
            }
            for (int k = 0; k < 2 && _mindCursor < _mindNear.Count; k++)
                MindLook(_mindNear[_mindCursor++], now);
            MindAppraiseStep(now);   // one person a frame from the happening being appraised
            OnlookStep(now);   // and one onlooker's act a frame, with MindLive and MindOnlookers (WestworldScript.Onlookers.cs)
            RevivedStep(now);   // every 2 s: a buried man walking past those who saw him die (MindWakes; WestworldScript.Doubt.cs)
            if (_aMindChoose == null)
                _aMindChoose = () => MindChooseStep(_now);
            Safe("mind choosing", _aMindChoose);   // one person a frame, each once a minute: what they would do now (shadow; nobody is tasked); its own guard, so a fault in it can't stop the traits, needs and feelings
        }

        private Action _aMindChoose;

        // ---- the levels of detail and the frame budget (step 7, night 2026-10-28; Core/Levels.cs; [Mind] MindLevels) ----

        private Levels _mindLevels;
        private readonly List<string> _mindAwayTowns = new List<string>();
        private readonly Dictionary<string, int> _mindAwayOld = new Dictionary<string, int>();   // id -> ties older than a week, built once a town pass
        private readonly List<int> _mindWhoDrop = new List<int>();
        private string _mindAwayTown, _mindAwayLast;
        private int _mindAwayCursor, _mindAwayPeople, _mindAwayWorries, _mindAwayTies, _mindAwayLife, _mindAwayPasses, _mindAwayTownCursor, _mindAwayPollAt;
        private long _mindAwayDay = -1, _mindScanMicros, _mindScanPeak;
        private const int AwayPeopleAFrame = 20;

        private static long Micros(long since)
        {
            return (Stopwatch.GetTimestamp() - since) * 1000000L / Stopwatch.Frequency;
        }

        /// The shadow's frame with the scheduler: the scan places everyone within 130 m (once a second, on screen or
        /// not; timed, reported in F7), then each frame hands out the work that's due, longest-waiting first, until the
        /// frame's share of time (200 microseconds) is spent: the near ring's looks (who they are, traits, needs, the
        /// MIND traits line) every 750 ms, the appraisal and the choosing as before (level 0's work, ahead of the
        /// rings' below), the about ring's looks every 10-30 s each, then the away pass (the people saved elsewhere, a
        /// town a game day, counted only). The rest waits for the next frame.
        private void MindLevelsTick(int now)
        {
            if (_mindLevels == null)
                _mindLevels = new Levels();
            if (now >= _mindNextScan)
            {
                _mindNextScan = now + 1000;
                long ts = Stopwatch.GetTimestamp();
                MindScanLevels(now);
                _mindScanMicros = Micros(ts);
                if (_mindScanMicros > _mindScanPeak)
                    _mindScanPeak = _mindScanMicros;
                return;   // the scan's frame does nothing else
            }
            _mindLevels.BeginFrame();
            LevelEntry e;
            long t0;
            while ((e = _mindLevels.Next(Levels.Near, now)) != null)
            {
                t0 = Stopwatch.GetTimestamp();
                MindLook(e.Handle, now);
                MindNoteWho(e);
                _mindLevels.Spend(Micros(t0));
            }
            if (_mindLevels.FrameHasRoom)
            {
                t0 = Stopwatch.GetTimestamp();
                MindAppraiseStep(now);
                OnlookStep(now);
                RevivedStep(now);
                _mindLevels.Spend(Micros(t0));
            }
            if (_mindLevels.FrameHasRoom)
            {
                t0 = Stopwatch.GetTimestamp();
                if (_aMindChoose == null)
                    _aMindChoose = () => MindChooseStep(_now);
                Safe("mind choosing", _aMindChoose);
                _mindLevels.Spend(Micros(t0));
            }
            while ((e = _mindLevels.Next(Levels.About, now)) != null)
            {
                t0 = Stopwatch.GetTimestamp();
                MindAbout(e, now);
                _mindLevels.Spend(Micros(t0));
            }
            if (_mindLevels.FrameHasRoom)
            {
                t0 = Stopwatch.GetTimestamp();
                MindAwayStep(now);
                _mindLevels.Spend(Micros(t0));
            }
        }

        /// The entry learns who its body is (or that it's nobody, visited half as often); a reused handle is re-read.
        private void MindNoteWho(LevelEntry e)
        {
            MindWho w;
            if (_mindWho.TryGetValue(e.Handle, out w))
            {
                e.Id = w.Id;
                e.Nobody = w.Id == null;
            }
        }

        /// The scan under the scheduler: everyone human and alive within the about ring and a little past it, placed by
        /// distance and (within the near ring and its margin) whether they're on screen (the screen test alone, as the
        /// cards use; walls aren't checked); everyone within 20 m, on screen or not, becomes _mindNear, nearest first,
        /// so the appraisal, the choosing, F7 and the cards read it as the old scan gave it. The cache of who is who
        /// is pruned of handles not seen for a minute, never wiped (a wipe threw away every choice and need).
        private void MindScanLevels(int now)
        {
            Ped me = Game.Player.Character;
            _mindNear.Clear();
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
                if (d > Levels.AboutMetres + 10f || ENTITY.IS_ENTITY_DEAD(h) || !PED.IS_PED_HUMAN(h))
                    continue;
                bool inView = d <= Levels.NearMetres + Levels.Hysteresis && ENTITY.IS_ENTITY_ON_SCREEN(h);
                _mindLevels.Place(h, d, inView, now);
            }
            _mindLevels.Settle(now);
            List<LevelEntry> near = _mindLevels.NearAll;
            for (int i = 0; i < near.Count; i++)
                _mindNear.Add(near[i].Handle);
            if (_mindWho.Count > 400)
            {
                _mindWhoDrop.Clear();
                foreach (KeyValuePair<int, MindWho> kv in _mindWho)
                    if (_mindLevels.Of(kv.Key) == null && (now - kv.Value.CheckedAt > 60000 || now < kv.Value.CheckedAt))
                        _mindWhoDrop.Add(kv.Key);
                foreach (int h in _mindWhoDrop)
                    _mindWho.Remove(h);
                if (_mindWho.Count > 1500)
                    _mindWho.Clear();   // never in practice: a safety stop
            }
        }

        /// The about ring: who they are and their needs and mood, no line (level 1: the town around). A nobody is
        /// visited half as often (the ring is mostly the game's own walkers).
        private void MindAbout(LevelEntry e, int now)
        {
            MindWho w = ENTITY.DOES_ENTITY_EXIST(e.Handle) ? MindKnown(e.Handle, now) : null;
            e.Id = w != null ? w.Id : null;
            e.Nobody = w == null || w.Id == null;
            if (w == null || w.Id == null)
                return;
            Traits t = w.Traits ?? (w.Traits = _engine.Shadow.TraitsOf(w.Src));
            if (t == null)
                return;
            if (w.Needs == null || now - w.NeedsAt >= NeedsMs || now < w.NeedsAt)
                MindNeeds(w, e.Handle, now);
            // the about ring chooses too (after item 14): what they'd do now from their needs, once a minute at most, no line
            if (w.Needs != null && w.P != null && (w.ChoseAt == 0 || now - w.ChoseAt >= ChooseMs || now < w.ChoseAt) && !ENTITY.IS_ENTITY_DEAD(e.Handle))
            {
                _aboutChooseWho = w;
                _aboutChooseHandle = e.Handle;
                _aboutChooseNow = now;
                if (_aAboutChoose == null)
                    _aAboutChoose = () => MindChooseFor(_aboutChooseWho, _aboutChooseHandle, _aboutChooseNow, true);
                Safe("mind choosing", _aAboutChoose);   // the same guard as the near ring's (a fault switches the choosing off, not the levels)
                _aboutChooseWho = null;
            }
        }
        private MindWho _aboutChooseWho;
        private int _aboutChooseHandle, _aboutChooseNow;
        private Action _aAboutChoose;

        /// The away pass (level 2): one time a game day a town, the residents saved for it, a few a frame: a worry ended,
        /// ties older than a week cooled, a life event, all worked out from id and day and only counted; one MIND away
        /// line a town a day, nothing written.
        private void MindAwayStep(int now)
        {
            long g = _store.LastGameMin;
            if (g <= 0 || _store.Residents == null)
                return;
            long day = g / 1440;
            if (_mindAwayTown == null)
            {
                if (now - _mindAwayPollAt < 1000 && now >= _mindAwayPollAt)
                    return;   // which town is due is asked once a second, not every frame
                _mindAwayPollAt = now;
                if (day != _mindAwayDay)
                {
                    // the towns of the saved residents, listed afresh each game day
                    _mindAwayDay = day;
                    _mindAwayTowns.Clear();
                    _mindAwayTownCursor = 0;
                    foreach (Resident r in _store.Residents)
                        if (r != null && r.Town != null && !r.Dead && !_mindAwayTowns.Contains(r.Town))
                            _mindAwayTowns.Add(r.Town);
                }
                if (_mindAwayTowns.Count == 0 || _mindAwayTownCursor >= _mindAwayTowns.Count)
                    return;   // every town's pass is done for the day
                string town = _mindAwayTowns[_mindAwayTownCursor++];
                if (!_mindLevels.AwayDue(town, g))
                    return;
                _mindAwayTown = town;
                _mindAwayCursor = 0;
                _mindAwayPeople = _mindAwayWorries = _mindAwayTies = _mindAwayLife = 0;
                // the ties older than a week, counted per person in one walk (not a walk of the ties per resident)
                _mindAwayOld.Clear();
                foreach (SceneTie t in _store.Ties)
                {
                    if (t == null || g - t.At <= 7 * 1440)
                        continue;
                    int n;
                    if (t.A != null)
                        _mindAwayOld[t.A] = _mindAwayOld.TryGetValue(t.A, out n) ? n + 1 : 1;
                    if (t.B != null)
                        _mindAwayOld[t.B] = _mindAwayOld.TryGetValue(t.B, out n) ? n + 1 : 1;
                }
                return;
            }
            int done = 0;
            List<Resident> rs = _store.Residents;
            while (_mindAwayCursor < rs.Count && done < AwayPeopleAFrame)
            {
                Resident r = rs[_mindAwayCursor++];
                if (r == null || r.Dead || r.Town != _mindAwayTown || r.Id == null)
                    continue;
                done++;
                _mindAwayPeople++;
                int old;
                _mindAwayOld.TryGetValue(r.Id, out old);
                Levels.AwayDay(r.Id, day, old, ref _mindAwayWorries, ref _mindAwayTies, ref _mindAwayLife);
            }
            if (_mindAwayCursor >= rs.Count)
            {
                _mindAwayPasses++;
                _mindAwayLast = _mindAwayTown + ", " + _mindAwayPeople + " people: " + _mindAwayWorries + " worries ended, " + _mindAwayTies + " ties cooled, " + _mindAwayLife + " life events; nothing written";
                if (_mindAwayPeople > 0)
                    MindLog(now, "MIND away: " + _mindAwayLast);
                _mindAwayTown = null;
            }
        }

        /// F7: the scheduler and its budget.
        private string MindLevelsStatus()
        {
            try
            {
                if (_cfg == null || !_cfg.MindShadow)
                    return "off ([Mind] MindShadow)";
                if (!_cfg.MindLevels || _mindLevels == null)
                    return "off ([Mind] MindLevels; the scan as before: everyone within " + (int)MindMetres + " m, two looks a frame)";
                Levels l = _mindLevels;
                return "on; " + l.Count + " placed, near " + l.NearCount + " (on screen within " + (int)Levels.NearMetres + " m, at most " + Levels.NearMax + "; " + l.NearAll.Count + " within 20 m in all), about " + l.AboutCount + " (within " + (int)Levels.AboutMetres + " m); " +
                       l.Ran + " visits, " + l.Deferred + " put off, " + l.Starved + " late, " + l.Changes + " level changes, " + l.Capped + " kept out of a full ring; budget " + l.BudgetMicros + " us a working frame, used " + (l.Frames > 0 ? l.Spent / l.Frames : 0) + " us on average, peak " + l.PeakMicros + "; the scan " + _mindScanMicros + " us (peak " + _mindScanPeak + ")" +
                       "; about-ring choices " + _mindChoosesAbout + "; away passes " + _mindAwayPasses + (_mindAwayLast != null ? ", last: " + _mindAwayLast : "") + (_mindAwayTown != null ? ", now " + _mindAwayTown + " (" + _mindAwayCursor + " of " + (_store.Residents != null ? _store.Residents.Count : 0) + " read)" : "");
            }
            catch (Exception ex)
            {
                return "couldn't say (" + ex.Message + ")";
            }
        }

        /// Choosing what to do, in shadow (the mind design's step 6; Core/Choosing.cs): for one person near a frame, once
        /// a minute each, every action open to them now scored against their needs, nature, mood and any feeling still
        /// running; a line only when the choice changes, at most five a minute inside the 20 MIND lines, with "but
        /// enRAGE has him" (or whatever else stands in the way) where MayDirect would refuse. Nobody is tasked.
        private void MindChooseStep(int now)
        {
            if (_mindNear.Count == 0)
                return;
            if (_mindChooseCursor >= _mindNear.Count)
                _mindChooseCursor = 0;
            int h = _mindNear[_mindChooseCursor++];
            MindWho w;
            if (!_mindWho.TryGetValue(h, out w) || w.Id == null || w.Traits == null || w.Needs == null || w.P == null)
                return;
            if (w.ChoseAt != 0 && now - w.ChoseAt < ChooseMs && now >= w.ChoseAt)
                return;
            if (!ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h))
                return;
            MindChooseFor(w, h, now, false);
        }

        /// One person's choice now (the shadow's choosing): their needs, nature, mood, the strongest feeling and, for the
        /// near ring, who is in sight. `quiet`: the about ring (MindLevels on; after item 14): out of view nobody is "in
        /// sight", and no line is written (the near ring's look writes it when they come near and the choice stands).
        private void MindChooseFor(MindWho w, int h, int now, bool quiet)
        {
            w.ChoseAt = now;
            long g = _store.LastGameMin;
            string tg = w.TradeGroup ?? (w.TradeGroup = InnerLife.TradeGroup(w.P.Trade));
            if (w.Recent == null)
                w.Recent = new RecentActs();
            var c = new ChoiceContext
            {
                // the about ring (quiet) is scored out of doors with nobody by: the player's room and crowd aren't theirs 120 m off (the late review)
                Hour = CLOCK.GET_CLOCK_HOURS(), Weekday = InnerLife.Weekday, Place = !quiet && _engine.NowIndoors ? _engine.NowPlace : null, Indoors = !quiet && _engine.NowIndoors,
                InTown = _placeTown != null, Weather = _engine.NowWeather, Others = quiet ? 0 : Math.Max(0, _mindNear.Count - 1), PlayerFeared = _engine.SawYouViolent(w.Id, 3),
                Last = w.Choice, Recent = w.Recent, Now = g, TradeGroup = tg, Works = tg != "gentry" && !(w.P.Woman && tg == "town"), WorkPlace = tg == "girl" ? "saloon" : null, Woman = w.P.Woman, SaloonGirl = tg == "girl"
            };
            FeltAt best;
            float left;
            if (_engine.Shadow.Strongest(w.Id, g, out best, out left))
            {
                c.Urge = best.Urge;
                c.FeelingLeft = left;
            }
            PadMood m = _engine.Shadow.MoodOf(w.Id, w.Traits, g);
            string mw = Mood.Words(m, Mood.Baseline(w.Traits));   // against their own baseline: a sour nature at rest is "even" here (the review)
            c.Mood = mw.StartsWith("sore", StringComparison.Ordinal) ? "sore" : mw.StartsWith("glad", StringComparison.Ordinal) ? "glad" : null;
            // who is in sight: a warm tie to nod to, a foe to step clear of (the store's ties, read only; the ties walked
            // once, each matched against the few near). The doer of a one-sided trouble has no foe in his victim.
            if (!quiet)
            foreach (SceneTie t in _store.Ties)
            {
                if (!t.Has(w.Id) || t.Warmth == 0)
                    continue;
                string other = t.OtherId(w.Id);
                if (other == null || other == w.Id)
                    continue;
                bool near = false;
                foreach (int oh in _mindNear)
                {
                    MindWho o;
                    if (oh != h && _mindWho.TryGetValue(oh, out o) && o.Id == other)
                    {
                        near = true;
                        break;
                    }
                }
                if (!near)
                    continue;
                if (t.Warmth > 0)
                    c.TiesNear++;
                else if (!(Ties.Trouble(t.Kind) && t.Did(w.Id)))
                    c.FoeNear = true;
            }
            string next;
            KeyValuePair<string, string> ch = Choosing.Choose(w.Needs, w.Traits, c, Choosing.SeedFor(w.Id, g), out next);
            _mindChoices++;
            w.Recent.Note(ch.Key, g);
            if (ch.Key == "eat")
                w.AteAt = g;   // the shadow's own meal: hunger falls for it at the next needs
            bool changed = ch.Key != w.Choice;
            if (changed)
            {
                w.Choice = ch.Key;
                w.ChoiceWhy = ch.Value;
                w.ChoiceNext = next;
                w.ChoiceTold = false;
                w.ChoiceHeld = null;
                _mindChoiceChanges++;
                if (!quiet)
                    _mindLastChoice = w.Name + " -> " + Choosing.WordsOf(ch.Key);   // F7's last choice is someone near, not the about ring's
            }
            if (quiet)
            {
                _mindChoosesAbout++;
                return;   // out of view: no line (written when they come near, if the choice still stands)
            }
            if (w.ChoiceTold || Choosing.CarriesOn(w.Choice))
                return;   // said already, or carrying on, which isn't news
            if (now - _mindChoiceMinuteAt >= 60000 || now < _mindChoiceMinuteAt)
            {
                _mindChoiceMinuteAt = now;
                _mindChoiceMinuteCount = 0;
            }
            if (_mindChoiceMinuteCount >= ChoiceLinesAMinute || !MindLogRoom(now))
            {
                if (changed)
                    _mindChoiceHeld++;   // written on the next look if the choice still stands
                return;
            }
            w.ChoiceHeld = MayDirectWhyNot(new Ped(h));   // only for a line that will be written (natives and a Ped)
            if (MindLog(now, "MIND would: " + w.Name + " -> " + Choosing.WordsOf(w.Choice) + " (" + w.ChoiceWhy + (w.ChoiceNext != null ? "; next: " + Choosing.WordsOf(w.ChoiceNext) : "") + ")" +
                        (w.ChoiceHeld != null ? "; but " + (w.ChoiceHeld.IndexOf("enRAGE", StringComparison.OrdinalIgnoreCase) >= 0 ? "enRAGE has him (" + w.ChoiceHeld + ")" : w.ChoiceHeld) : "")))
            {
                w.ChoiceTold = true;
                _mindChoiceMinuteCount++;
                _mindChoiceLines++;
            }
        }

        // ---- beliefs and the self (step 10, night 2026-10-28; Core/Beliefs.cs): shadow, read-only against the Awakening and Regard ----

        private int _mindBeliefLines, _mindBeliefFlips, _mindBeliefHeld;
        private string _mindLastBelief, _mindBeliefFailed;
        private readonly float[] _mindBaseline = new float[Beliefs.Count];
        private readonly BeliefContext _mindBeliefCtx = new BeliefContext();

        /// The appraisal moves this person's beliefs (in memory); a belief that flips since the last look (by the move, or
        /// by the baseline: a memory come or gone, the Awakening) gets a MIND believes line (inside the 20), with where it
        /// disagrees with the Awakening's stage and with Regard, and the road they'd take with a memory against it.
        /// Nothing drives the Awakening or Regard from it. The first look sets what holds and can't flip anything.
        private void MindBelieve(MindWho w, string id, Feeling f, AppraisalEvent ae, int now)
        {
            try
            {
                PersonState st = _store.Find(id);
                if (w.TradeGroup == null)
                    w.TradeGroup = InnerLife.TradeGroup(w.P.Trade);
                _mindBeliefCtx.WakeStage = st != null ? st.WakeStage : 0;
                _mindBeliefCtx.Memories = st != null ? st.Mind : null;
                _mindBeliefCtx.Standing = Beliefs.StandingOf(w.TradeGroup);
                Beliefs.Fill(_mindBaseline, w.P, w.Traits, _mindBeliefCtx);
                BeliefState bs = _engine.Shadow.BeliefsOf(id);
                if (!bs.Seen)
                {
                    bs.Held = Beliefs.HeldBits(_mindBaseline, bs.Moved);
                    bs.Seen = true;
                }
                float worldWas = bs.Moved[Beliefs.World];
                bool moved = _engine.Shadow.Believe(id, f, ae, w.Traits, _mindBaseline);
                int held = Beliefs.HeldBits(_mindBaseline, bs.Moved);
                if (held == bs.Held)
                    return;
                int was = bs.Held;
                bs.Held = held;
                _mindBeliefFlips++;
                if ((was & (1 << Beliefs.World)) != 0 && (held & (1 << Beliefs.World)) == 0 && bs.Moved[Beliefs.World] < worldWas - 1e-4f)
                    DoubtGaveWay(w, id);   // only when this feeling moved it down: a stage gained lowers the baseline too (the review of item 4: the Awakening fed itself)   // "things are as they seem" gave way: with MindWakes they stir (WestworldScript.Doubt.cs)
                string flipped = Beliefs.Flips(was, held);
                _mindLastBelief = w.Name + ": " + flipped;
                if (!MindLogRoom(now))
                {
                    _mindBeliefHeld++;
                    return;
                }
                string regard = st != null ? Regard.OfPerson(w.P, st, false) : null;
                string dis = Beliefs.Disagreements(_mindBaseline, bs.Moved, _mindBeliefCtx.WakeStage, regard);
                if (MindLog(now, "MIND believes: " + w.Name + " (" + (w.Src.Temper ?? "plain") + "): " + flipped + " <- " + (moved ? f.Kind + " at " + ae.Kind : "a memory or the Awakening (the baseline moved)") +
                            "; " + Beliefs.Describe(_mindBaseline, bs.Moved) + (dis != null ? "; disagrees: " + dis : "") + "; a memory against it they'd " + Beliefs.Road(w.Traits) + (_cfg.MindLive && _cfg.MindWakes ? " (the Awakening reads 'as it seems'; nothing drives Regard from this)" : " (nothing drives the Awakening or Regard from this)")))
                    _mindBeliefLines++;
            }
            catch (Exception ex)
            {
                if (_mindBeliefFailed == null)
                {
                    _mindBeliefFailed = ex.Message;
                    Log.Write("MIND believes failed: " + ex.Message);   // once; F7 says so
                }
            }
        }

        /// F7: the shadow's beliefs, for the nearest three it knows.
        private string MindBeliefsStatus()
        {
            try
            {
                if (_cfg == null || !_cfg.MindShadow)
                    return "off ([Mind] MindShadow)";
                var near = new List<string>();
                foreach (int h in _mindNear)
                {
                    if (near.Count >= 3)
                        break;
                    MindWho w;
                    if (!_mindWho.TryGetValue(h, out w) || w.Id == null || w.Traits == null || w.P == null)
                        continue;
                    PersonState st = _store.Find(w.Id);
                    if (w.TradeGroup == null)
                        w.TradeGroup = InnerLife.TradeGroup(w.P.Trade);
                    var ctx = new BeliefContext { WakeStage = st != null ? st.WakeStage : 0, Memories = st != null ? st.Mind : null, Standing = Beliefs.StandingOf(w.TradeGroup) };
                    float[] baseline = Beliefs.Baseline(w.P, w.Traits, ctx);
                    BeliefState bs = _engine.Shadow.BeliefsOf(w.Id);
                    string regard = st != null ? Regard.OfPerson(w.P, st, false) : null;
                    string dis = Beliefs.Disagreements(baseline, bs.Moved, ctx.WakeStage, regard);
                    near.Add(w.Name + " " + Beliefs.Describe(baseline, bs.Moved) + (dis != null ? " (disagrees: " + dis + ")" : "") + (w.Kind == "folk" ? " (enRAGE's townsperson: no trade, Regard reads their card only)" : ""));
                }
                return "shadow only; " + _engine.Shadow.BeliefsMoved + " moves for " + _engine.Shadow.PeopleBelieving + " people, " + _mindBeliefFlips + " flips, " + _mindBeliefLines + " MIND believes lines, " + _mindBeliefHeld + " held back by the 20 a minute" +
                       (_mindBeliefFailed != null ? "; FAILED once: " + _mindBeliefFailed : "") + "; last: " + (_mindLastBelief ?? "none") + (near.Count > 0 ? "; near: " + string.Join("; ", near) : "");
            }
            catch (Exception ex)
            {
                return "couldn't say (" + ex.Message + ")";
            }
        }

        /// F7: the shadow's choosing.
        private string MindChoicesStatus()
        {
            try
            {
                if (_cfg == null || !_cfg.MindShadow)
                    return "off ([Mind] MindShadow)";
                var near = new List<string>();
                foreach (int h in _mindNear)
                {
                    if (near.Count >= 4)
                        break;
                    MindWho w;
                    if (_mindWho.TryGetValue(h, out w) && w.Id != null && w.Choice != null)
                        near.Add(w.Name + " would " + Choosing.WordsOf(w.Choice) + " (" + w.ChoiceWhy + (w.ChoiceNext != null ? "; next " + Choosing.WordsOf(w.ChoiceNext) : "") + (w.ChoiceHeld != null ? "; but " + w.ChoiceHeld : "") + ")");
                }
                return "shadow only, nobody tasked; " + _mindChoices + " choices worked out (once a minute each), " + _mindChoiceChanges + " changed, " + _mindChoiceLines + " MIND would lines, " + _mindChoiceHeld + " held back (5 a minute); last: " + (_mindLastChoice ?? "none") +
                       (near.Count > 0 ? "; near: " + string.Join("; ", near) : "");
            }
            catch (Exception ex)
            {
                return "couldn't say (" + ex.Message + ")";
            }
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
                OnlookFlush(_now);   // the last happening's witnesses are all taken: its strongest two may act
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
            _mindEvDoer = _engine.DoerOf(ev);   // who did it to whom: the pair's tie (Core/Engine.Appraise.cs, the one road the tests take too)
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
            AppraisalEvent ae = _engine.AppraiseFor(ev, w.P, heardOnly, _mindEvSaw);   // the doer and the warmth from the ties (Core/Engine.Appraise.cs)
            if (ae == null)
                return;
            Feeling f = Appraisal.Of(ae, w.Traits);
            if (f.Kind == "none")
                return;
            f.Shows = Display.Shows(f, ae, w.Traits);   // kept with the feeling, so a masked one's thought can say the mask too
            _engine.Shadow.Feel(id, f, ev.At);
            OnlookNote(ev, id, w, f, ae);   // someone who only watched it may act on it (MindOnlookers; queued, capped)
            LanguageAskAhead(w, id, now);   // the language layer asks the model now, so the line is ready when the thought comes (WestworldScript.Language.cs)
            _mindFelt++;
            string about = ae.Doer == "player" && f.About == ev.DoerName ? "you" : f.About;
            _mindLastFelt = w.Name + ": " + f.Kind + (about != null ? " at " + about : "");
            if (!MindLogRoom(now))
            {
                _mindLogHeld++;
                MindBelieve(w, id, f, ae, now);   // what they believe moves whether or not the line is written
                return;
            }
            long g = _store.LastGameMin;
            PadMood b = Mood.Baseline(w.Traits), m = _engine.Shadow.MoodOf(id, w.Traits, g);
            string mind = Mood.SoreOrGlad(m, b), old = _cfg.MindLive ? null : _engine.MoodToday(w.P, w.P.Home ?? _placeTown);
            MindLog(now, "MIND " + w.Name + " (" + (w.Src.Temper ?? "plain") + "): " + f.Kind + " (" + f.Strength + ")" + (about != null ? " at " + about : "") + ", urge " + f.Urge + ", shows " + f.Shows + " (" + MindCanShow(f, w) + ")" +
                         " <- " + ev.Kind + " (" + ae.Role + (heardOnly ? ", only heard it" : "") + ", " + ae.Witnesses + " saw); mood " + Mood.Words(m, b) +
                         (!_cfg.MindLive && old != mind ? "; the old mood says " + (old ?? "nothing") + ", the mind's " + (mind ?? "nothing") : ""));
            MindBelieve(w, id, f, ae, now);   // and what they believe moves a little (step 10, shadow), after the feeling's own line
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
            if (_mindLevels != null)
                _mindLevels.Clear();
            _mindAwayTown = null;
            _mindAwayTowns.Clear();
            _mindAwayDay = -1;
            _mindTraitsTold.Clear();
            _mindCursor = 0;
            _mindEv = null;
            _mindEvPeople.Clear();
            OnlookForget();
            DoubtForget();
            _mindFelt = 0;
            _mindLastFelt = null;   // nothing from a rewound future in F7
            _mindChooseCursor = 0;
            _mindLastChoice = null;
            _mindLastBelief = null;   // nor a flip from one
            _mindBeliefFlips = 0;
            _mindBeliefLines = 0;
            _mindBeliefHeld = 0;
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
                Drunk = InHisCups(new Ped(h), w.P), Feared = _engine.SawYouViolent(w.Id, 3), WakeStage = st != null ? st.WakeStage : 0, Memories = st != null ? st.Mind : null,
                AteAt = w.AteAt,   // the shadow's own meal, if its choice was to eat
                Recent = w.Recent  // and its other brief acts: a rest rests, a talk keeps company (after item 14)
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
            RecordRef rec = _bridge.RecordAt(h);
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
                FolkRef ac = _bridge.AcquaintanceAt(h);
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
                    if (_resBind.TryGetValue(h, out b) && b.R != null && !b.R.Dead && b.Model == model)   // handles are reused: the model check every other binding site makes (the night review)
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
            {
                // the same person looked up again: what the shadow worked out for them carries over (the night review:
                // without this every choice and need went every 30 s)
                w.Told = was.Told;
                w.Traits = was.Traits;
                w.Needs = was.Needs;
                w.Concerns = was.Concerns;
                w.NeedsAt = was.NeedsAt;
                w.Choice = was.Choice;
                w.ChoiceWhy = was.ChoiceWhy;
                w.ChoiceNext = was.ChoiceNext;
                w.ChoiceHeld = was.ChoiceHeld;
                w.ChoiceTold = was.ChoiceTold;
                w.Recent = was.Recent;
                w.ChoseAt = was.ChoseAt;
                w.AteAt = was.AteAt;
                w.TradeGroup = was.TradeGroup;
            }
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
                       (w.Needs != null ? "; " + w.Needs.Words() : "") + (w.Concerns != null && w.Concerns.Count > 0 ? "; " + w.Concerns[0].Words : "") +
                       (w.Choice != null && !Choosing.CarriesOn(w.Choice) ? "; would: " + Choosing.WordsOf(w.Choice) : "");
            }
            catch
            {
                return null;
            }
        }

        /// F7: what the shadow has done, and the nearest three it knows, in words (from its cache; never throws).
        // the Testing row "mind:request" (after item 14; the language layer's offline half): the request the model would
        // get for the nearest known person, written to the log line by line as LANGUAGE lines (not MIND: the 20 a minute
        // are the shadow's own); nothing is asked of any model, nothing is kept
        private int _mindRequests;
        private string _mindRequestLast;

        private string MindRequestFire(int now, out string said)
        {
            said = null;
            try
            {
                return MindRequestFireImpl(now, out said);
            }
            catch (Exception ex)
            {
                return "the request failed: " + ex.Message;   // its own catch (the review): a fault here costs the keypress, not the Testing page
            }
        }

        private string MindRequestFireImpl(int now, out string said)
        {
            said = null;
            if (_cfg == null || !_cfg.MindShadow || _engine == null)
                return "[Mind] MindShadow is off";
            MindWho w = null;
            int handle = 0;
            foreach (int h in _mindNear)
            {
                MindWho k = ENTITY.DOES_ENTITY_EXIST(h) ? MindKnown(h, now) : null;
                if (k != null && k.Id != null && k.P != null)
                {
                    w = k;
                    handle = h;
                    break;
                }
            }
            if (w == null)
                return "nobody RealNPCs knows within " + (int)MindMetres + " m";
            string why;
            LanguageRequest r = MindRequestOf(w, handle, now, out why);
            if (r == null)
                return why;
            Log.Write("LANGUAGE request for " + w.Name + " (key " + Language.CacheKey(r) + "):");
            foreach (string line in Language.Prompt(r).Split('\n'))
                if (line.Trim().Length > 0)
                    Log.Write("LANGUAGE   " + line.TrimEnd('\r'));
            Log.Write("LANGUAGE   the written fallback: " + (r.AvoidLine ?? "(none: with MindLive off the old thought stands, and it isn't worked out here)"));
            _mindRequests++;
            _mindRequestLast = w.Name;
            said = "the language request for " + w.Name + " is in the log (LANGUAGE lines)";
            return null;
        }

        /// The language request for someone the shadow knows, from its cached state (the Testing row and the language
        /// layer's ask-ahead share it): traits, needs, the beliefs held now, the first names in sight they'd call by name.
        private LanguageRequest MindRequestOf(MindWho w, int handle, int now, out string why)
        {
            why = null;
            if (w.Traits == null)
                w.Traits = _engine.Shadow.TraitsOf(w.Src);
            if (w.Traits == null)
            {
                why = w.Name + "'s traits couldn't be worked out";
                return null;
            }
            if (w.Needs == null || now - w.NeedsAt >= NeedsMs || now < w.NeedsAt)
                MindNeeds(w, handle, now);
            // what they hold now: the baseline from who they are and what they remember, plus what the appraisals moved
            PersonState st = _store.Find(w.Id);
            if (w.TradeGroup == null)
                w.TradeGroup = InnerLife.TradeGroup(w.P.Trade);
            _mindBeliefCtx.WakeStage = st != null ? st.WakeStage : 0;
            _mindBeliefCtx.Memories = st != null ? st.Mind : null;
            _mindBeliefCtx.Standing = Beliefs.StandingOf(w.TradeGroup);
            Beliefs.Fill(_mindBaseline, w.P, w.Traits, _mindBeliefCtx);
            BeliefState bs = _engine.Shadow.BeliefsPeek(w.Id);   // a peek: a read-only row makes nothing (the review)
            var held = new float[_mindBaseline.Length];
            for (int i = 0; i < held.Length; i++)
                held[i] = Beliefs.Of(_mindBaseline, bs != null ? bs.Moved : null, i);
            // the first names in sight: looked up afresh (a reused handle's old name never goes in), never their own under
            // another handle, and only faces they'd call by name, a tie or the same home town (a stranger is "mister": item 10)
            var near = new List<string>();
            foreach (int h in _mindNear)
            {
                if (h == handle || !ENTITY.DOES_ENTITY_EXIST(h))
                    continue;
                MindWho k = MindKnown(h, now);
                if (k == null || k.Id == null || k.Id == w.Id || k.Name == null || k.P == null)
                    continue;
                bool sameTown = !string.IsNullOrEmpty(k.P.Home) && k.P.Home == w.P.Home, tied = false;
                if (!sameTown)
                    foreach (SceneTie t in _store.Ties)
                        if (t.Has(w.Id) && t.Has(k.Id))
                        {
                            tied = true;
                            break;
                        }
                if (!sameTown && !tied)
                    continue;
                near.Add(Person.FirstOf(k.Name));
                if (near.Count >= Language.NamesInRequest)
                    break;
            }
            LanguageRequest r = _engine.LanguageRequestFor(w.P, w.Traits, w.Needs, w.Concerns, near, _bridge.TownOnly(Game.Player.Character.Position), _engine.NowPlace,
                                                           CLOCK.GET_CLOCK_HOURS(), CLOCK.GET_CLOCK_YEAR(), _engine.NowWeather, held);
            if (r == null)
                why = "no request could be built for " + w.Name;
            return r;
        }

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
                       (_engine != null ? _engine.Shadow.TraitsMade + " (" + _engine.Shadow.Count + " kept)" : "nobody") + ", " + _mindLogged + " MIND lines, " + _mindLogHeld + " held back by the 20 a minute; events appraised " + _mindFelt + " feelings from " + _engine.Shadow.EventsQueued + " happenings (" + _engine.Shadow.Waiting + " waiting, " + _engine.Shadow.EventsDropped + " dropped from a full queue, " + _mindEvStale + " too old by their turn), last: " + (_mindLastFelt ?? "none") + "; memories told on " + _engine.MemoriesTold + " (" + _engine.MemoriesToldInPassing + " in passing), placed " + _engine.MemoriesPlaced + ", drifted readings " + _engine.MemoriesDrifted + ", slanted readings " + _engine.MemoriesSlanted +
                       (near.Count > 0 ? "; near: " + string.Join("; ", near) : "; nobody near it knows") +
                       "; language requests written " + _mindRequests + (_mindRequestLast != null ? " (last " + _mindRequestLast + ")" : string.Empty);
            }
            catch (Exception ex)
            {
                return "couldn't say (" + ex.Message + ")";
            }
        }
    }
}
