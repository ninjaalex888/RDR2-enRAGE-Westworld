// Acknowledgement in passing ([Scenes] Acknowledge; the choice and the words are Core/Passing.cs): two people with a
// tie between them (Ties.cs) coming toward each other near the player, one of them walking, acknowledge it as they
// pass: friends nod, tip a hat or wave, and one may say hello in their own voice; enemies give each other a hard look
// (the cold and the proud may spit); sweethearts look a beat too long. One of them thinks something of it, over their
// head while you can see them. Looks and upper-body gestures only: nobody's feet are moved, nobody's task is cleared.
// A single nod plays at once, 20 s apart, a pair once in 45 game minutes (Pacing "nod"), only people we may direct (MayDirect), and
// let go the moment a street scene or a conversation takes either of them. Runs in its own guard ("passing"); nothing
// is saved but the pair's rest.
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
        private const float NodApart = 7f, NodTestApart = 10f, NodFromYou = 25f, NodFacing = 75f;
        private const int NodScanMs = 1000, NodKnownMs = 10000, NodGapMs = 20000, NodStaleMs = 8000;

        private sealed class NodWho
        {
            public uint Model;
            public int Until;
            public string Id, Name, Temper;   // Id null: nobody we know
        }

        private sealed class NodRun
        {
            public Ped A, B;
            public string AId, BId, AName, BName, Pair;
            public bool AWoman, BWoman;
            public Passing.Act Act;
            public int At, Step;
            public string Seed, Said;
            public bool Test;               // the Testing row's nod: nothing told on (after item 14)
            public bool Spoke;              // the first one's greeting played (the answer and the goodbyes follow only then)
        }

        private readonly Dictionary<int, NodWho> _nodWho = new Dictionary<int, NodWho>();
        private readonly HashSet<string> _nodIds = new HashSet<string>();
        private readonly Dictionary<string, float> _nodApart = new Dictionary<string, float>();      // pair -> how far apart at the last look
        private readonly Dictionary<string, float> _nodApartNow = new Dictionary<string, float>();
        private NodRun _nod;
        private int _nextNodScan, _nextNod, _nodShown, _nodUnseen;
        private readonly Dictionary<string, int> _nodCount = new Dictionary<string, int>();
        private string _nodLast, _nodWhyNot;
        private Action _aNod;

        private void PassingStep(int now)
        {
            if (!_cfg.Acknowledge || _store == null)
                return;
            if (_aNod == null)
                _aNod = () => PassingTick(_now);
            Safe("passing", _aNod);
        }

        private void PassingTick(int now)
        {
            if (_nod != null)
            {
                NodPlay(_nod, now);
                return;
            }
            if (now < _nextNodScan)
                return;
            _nextNodScan = now + NodScanMs;
            if (_nodWho.Count > 300)
                foreach (int h in _nodWho.Where(kv => now - kv.Value.Until > 60000).Select(kv => kv.Key).ToList())
                    _nodWho.Remove(h);   // handles long gone
            Ped me = Game.Player.Character;
            if (me == null || !me.Exists() || me.IsDead)
                return;
            string seed;
            // the look runs every second even while resting, so "closing in" is known when the rest is over
            NodRun run = NodFind(me, now, false, out seed, out _nodWhyNot);
            if (run == null)
                return;
            if (now < _nextNod)
            {
                _nodWhyNot = "a nod " + (NodGapMs - (_nextNod - now)) / 1000 + " s ago";
                return;
            }
            if (_director.Busy)
            {
                _nodWhyNot = "a street scene is going on (its thoughts have the ticker)";
                return;
            }
            NodStart(run, now, false);
        }

        /// Who this person is, if they're someone with lasting ties (enRAGE's people, its townsfolk, our regulars and
        /// saved residents): looked up once, and again every 10 s or when the handle's model changes.
        private NodWho NodKnow(Ped p, int now)
        {
            int h = p.Handle;
            uint model = ENTITY.GET_ENTITY_MODEL(h);
            NodWho w;
            if (_nodWho.TryGetValue(h, out w) && w.Model == model && now < w.Until)
                return w;
            if (w == null)
            {
                w = new NodWho();
                _nodWho[h] = w;
            }
            w.Model = model;
            w.Until = now + NodKnownMs;
            string id, name, temper;
            if (LastingId(p, out id, out name, out temper))
            {
                w.Id = id;
                w.Name = name;
                w.Temper = temper;
            }
            else
                w.Id = w.Name = w.Temper = null;
            return w;
        }

        /// The pair to acknowledge each other now: both known, a tie between them, coming toward each other within 7 m
        /// (one of them on the move; a test only needs them within 10 m), the one who acts facing the other, one of them
        /// in your view within 25 m; the nearest to you. Null, with why not.
        private NodRun NodFind(Ped me, int now, bool test, out string seed, out string whyNot)
        {
            seed = null;
            whyNot = null;
            var near = new List<Ped>();
            var who = new List<NodWho>();
            var pos = new List<Vector3>();
            _nodIds.Clear();
            Vector3 mine = me.Position;
            foreach (Ped p in World.GetAllPeds())
            {
                if (p == null || p.Handle == me.Handle || !p.Exists() || p.IsDead)
                    continue;
                Vector3 at = p.Position;
                if (at.DistanceTo(mine) > NodFromYou + NodTestApart)
                    continue;
                int h = p.Handle;
                if (!PED.IS_PED_HUMAN(h) || Natives.StoryModel(p) || _director.Involves(h) || _wakeActs.Any(x => x.Handle == h) || InTalk(h) ||
                    PostKindOf(p) != null || IsHermit(h) || IsGatherer(h) || IsExecutionPed(h) || Speech.Busy(h) || IsAcknowledging(h))
                    continue;
                NodWho w = NodKnow(p, now);
                if (w.Id == null)
                    continue;
                near.Add(p);
                who.Add(w);
                pos.Add(at);
                _nodIds.Add(w.Id);
            }
            _nodApartNow.Clear();
            if (near.Count < 2)
            {
                whyNot = "fewer than two people we know near you";
                _nodApart.Clear();
                return null;
            }
            var ties = new Dictionary<string, SceneTie>();
            foreach (SceneTie t in _store.Ties)
                if (t.A != null && t.B != null && _nodIds.Contains(t.A) && _nodIds.Contains(t.B) && Passing.Acknowledged(t.Kind))
                    ties[NodPair(t.A, t.B)] = t;
            if (ties.Count == 0)
            {
                whyNot = near.Count + " we know near you, no ties between them";
                _nodApart.Clear();
                return null;
            }
            NodRun best = null;
            float bestFromYou = float.MaxValue;
            string why = "no pair with a tie coming toward each other";
            long g = _store.LastGameMin;
            for (int i = 0; i < near.Count; i++)
                for (int j = i + 1; j < near.Count; j++)
                {
                    float apart = pos[i].DistanceTo(pos[j]);
                    if (apart > (test ? NodTestApart : NodApart) || apart < 0.8f || who[i].Id == who[j].Id)
                        continue;
                    string pair = NodPair(who[i].Id, who[j].Id);
                    SceneTie t;
                    if (!ties.TryGetValue(pair, out t))
                        continue;
                    float before;
                    bool closing = _nodApart.TryGetValue(pair, out before) && apart < before - 0.3f;
                    _nodApartNow[pair] = apart;
                    Ped a = near[i], b = near[j];
                    if (!test)
                    {
                        if (ENTITY.GET_ENTITY_SPEED(a.Handle) < 0.6f && ENTITY.GET_ENTITY_SPEED(b.Handle) < 0.6f)
                        {
                            why = "the pair close by are standing still (that's for a conversation)";
                            continue;
                        }
                        if (!closing)
                        {
                            why = "the pair close by aren't coming toward each other (walking together, or already past)";
                            continue;
                        }
                    }
                    float fromYou = Math.Min(pos[i].DistanceTo(mine), pos[j].DistanceTo(mine));
                    if (fromYou > NodFromYou || fromYou >= bestFromYou || (!NodInView(me, a) && !NodInView(me, b)))
                        continue;
                    string skipped;
                    string pseed = pair + "|" + (g / 45);
                    string pace = Pacing.WhyNot(_store.Pace, "nod", null, pair, g, Voice.Hash(pseed + "|pace") % 100, test, out skipped);
                    if (pace != null)
                    {
                        why = "rests: " + pace;
                        continue;
                    }
                    bool aMay = MayDirectWhyNot(a) == null && Facing(a, pos[j], NodFacing), bMay = MayDirectWhyNot(b) == null && Facing(b, pos[i], NodFacing);
                    if (!aMay && !bMay)
                    {
                        why = "neither may be directed and facing the other";
                        continue;
                    }
                    bool romantic = Ties.Sweethearts(t);
                    string kind = Passing.KindOf(t.Warmth, romantic);
                    bool aFirst = Passing.AFirst(kind, who[i].Id, who[j].Id, who[i].Temper, who[j].Temper, t.Doer, pseed);
                    if (!aMay)
                        aFirst = false;   // the one we may direct, facing the other, acts
                    else if (!bMay)
                        aFirst = true;
                    int x = aFirst ? i : j, y = aFirst ? j : i;
                    bool xWoman = !PED.IS_PED_MALE(near[x].Handle), yWoman = !PED.IS_PED_MALE(near[y].Handle);
                    best = new NodRun
                    {
                        A = near[x], B = near[y], AId = who[x].Id, BId = who[y].Id, AName = who[x].Name, BName = who[y].Name, AWoman = xWoman, BWoman = yWoman, Pair = pair, Seed = pseed,
                        Act = Passing.For(t.Warmth, romantic, xWoman, yWoman, who[x].Temper, who[y].Temper, Person.FirstOf(who[x].Name), Person.FirstOf(who[y].Name), pseed),
                        Said = skipped
                    };
                    bestFromYou = fromYou;
                    seed = pseed;
                }
            // what's kept for the next look: the pairs seen this time
            _nodApart.Clear();
            foreach (KeyValuePair<string, float> kv in _nodApartNow)
                _nodApart[kv.Key] = kv.Value;
            if (best == null)
                whyNot = why;
            else if (_cfg.MindLive && _cfg.MindPassing)
                PassingFromMind(best);   // the greeting by what the first one feels about the other lately and their day (Core/Passing.Mind.cs)
            return best;
        }

        private int _passMinded, _passMindedAt;
        private string _passMindedLast, _passMindedPair;

        /// With MindLive and MindPassing: what the first one feels about the other lately, and their mood, change the greeting
        /// the tie gives. Only someone the shadow already knows; its own catch (the tie's greeting stands on a fault).
        private void PassingFromMind(NodRun r)
        {
            try
            {
                MindWho w;
                if (r == null || r.A == null || !_mindWho.TryGetValue(r.A.Handle, out w) || w.Id == null || w.Id != r.AId || w.Traits == null)
                    return;
                long g = _store.LastGameMin;
                string mood = Passing.DayMood(_engine.Shadow.MoodOf(w.Id, w.Traits, g), Mood.Baseline(w.Traits));   // against their own nature: a sour one isn't sore at a friend every day
                string why = Passing.Minded(r.Act, _engine.Shadow.Felt(w.Id), r.BId, mood, r.AWoman, Person.FirstOf(r.BName), g, r.Seed);
                if (why == null)
                    return;
                if (r.Pair == _passMindedPair && _now - _passMindedAt < 60000 && _now >= _passMindedAt)
                    return;   // the same pair found again within the minute (the scan runs every second): counted and logged once
                _passMindedPair = r.Pair;
                _passMindedAt = _now;
                _passMinded++;
                _passMindedLast = r.AName + ": " + why;
                Log.Detail("NOD the mind: " + r.AName + " passing " + r.BName + ": " + why + " (" + r.Act.Gesture + (r.Act.Speaks ? ", a word" : ", no word") + ")");
            }
            catch (Exception ex)
            {
                Log.Write("NOD the mind's part failed: " + ex.Message);
            }
        }

        private static string NodPair(string a, string b)
        {
            return string.CompareOrdinal(a, b) < 0 ? a + "|" + b : b + "|" + a;
        }

        private bool InTalk(int h)
        {
            if (_talk == null || _talk.People == null)
                return false;
            for (int i = 0; i < _talk.People.Length; i++)
                if (_talk.People[i] != null && _talk.People[i].Handle == h)
                    return true;
            return false;
        }

        /// Busy with a nod or a reaction to a sight (WestworldScript.Sights.cs): the street scenes, the talk and
        /// the falls leave them be till it's done, as these leave the others' people be.
        /// Taken by something else of ours already: a street scene, a waking act, a conversation, a nod, a sight, a walk-up.
        private bool NotFree(int h)
        {
            return _director.Involves(h) || IsAcknowledging(h) || InTalk(h) || _wakeActs.Any(x => x.Handle == h);
        }

        private bool IsAcknowledging(int h)
        {
            if (_animTest != null && _animTest.Handle == h)
                return true;   // the Testing page's animation rows have them (WestworldScript.AnimTest.cs)
            if (IsOnlooking(h))
                return true;   // acting on something they just saw (WestworldScript.Onlookers.cs)
            if (IsAcknowledgingYou(h))
                return true;   // acknowledging the player in passing (WestworldScript.ToYou.cs)
            if (IsRescuer(h))
                return true;   // walking over to someone in despair (WestworldScript.Rescue.cs)
            if (_gunTest != null && _gunTest.Handle == h)
                return true;   // and its gunshot row (WestworldScript.Gunshot.cs)
            if (_grudgeWalk != null && _grudgeWalk.Ped != null && _grudgeWalk.Ped.Handle == h)
                return true;
            if (_kinWalk != null && _kinWalk.Ped != null && _kinWalk.Ped.Handle == h)
                return true;   // kin walking up to the player (WestworldScript.Life.cs)
            if (PatientWalking(h))
                return true;   // on the way to the doctor's and back (WestworldScript.Patient.cs)
            if (_nod != null && ((_nod.A != null && _nod.A.Handle == h) || (_nod.B != null && _nod.B.Handle == h)))
                return true;
            for (int i = 0; i < _sights.Count; i++)
                if (_sights[i].By != null && _sights[i].By.Handle == h)
                    return true;
            return false;
        }

        /// On your screen with nothing in the way.
        private static bool NodInView(Ped me, Ped p)
        {
            return ENTITY.IS_ENTITY_ON_SCREEN(p.Handle) && ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(me.Handle, p.Handle, 17);
        }

        /// The greeting's word in passing, for the memories (Engine.RememberPassing: MindLive only, one way, the speaker's). Its
        /// own catch: a fault here never costs the nod.
        private void NodWord(NodRun r)
        {
            try
            {
                if (_engine == null || r.AId == null || r.BId == null)
                    return;
                _engine.RememberPassing(new Minds.Party { Id = r.AId, Name = r.AName }, new Minds.Party { Id = r.BId, Name = r.BName });
            }
            catch (Exception ex)
            {
                if (!_nodWordFailed)
                    Log.Write("NOD the word in passing failed: " + ex.Message);
                _nodWordFailed = true;
            }
        }
        private bool _nodWordFailed;

        private void NodStart(NodRun run, int now, bool test)
        {
            // the handles are who we think they are (a handle the game reused within the cache's 10 s isn't)
            string aId, bId, n, tp;
            if (!LastingId(run.A, out aId, out n, out tp) || aId != run.AId || !LastingId(run.B, out bId, out n, out tp) || bId != run.BId)
            {
                _nodWhyNot = "someone wasn't who they were a moment ago (a reused handle)";
                _nodWho.Remove(run.A.Handle);
                _nodWho.Remove(run.B.Handle);
                return;
            }
            run.At = now;
            run.Step = 0;
            run.Test = test;
            _nod = run;
            _nextNod = now + NodGapMs;
            Pacing.Note(_store.Pace, "nod", null, run.Pair, _store.LastGameMin);   // a test too: the pair rests either way
            int c;
            _nodCount.TryGetValue(run.Act.Kind, out c);
            _nodCount[run.Act.Kind] = c + 1;
            foreach (string d in new[] { Gestures.MaleSilent, Gestures.FemaleSilent })
                if (!STREAMING.HAS_ANIM_DICT_LOADED(d))
                    STREAMING.REQUEST_ANIM_DICT(d);
            _nodLast = run.AName + " & " + run.BName + ": " + run.Act.Kind + (test ? " (a test)" : "");
            Log.Write("NOD " + run.AName + " & " + run.BName + ": " + run.Act.Kind + " (" + run.Act.Gesture + ", the answer " + run.Act.Answer +
                      (run.Act.Speaks ? ", a word" : "") + ", " + run.A.Position.DistanceTo(run.B.Position).ToString("0.0") + " m apart, " +
                      (int)run.A.Position.DistanceTo(Game.Player.Character.Position) + " m from you)" + (test ? ", a test" + (run.Said != null ? " (skipped: " + run.Said + ")" : "") : ""));
        }

        /// Still ours to play with: there, alive, and not taken by a street scene or a conversation since.
        private bool NodStill(Ped p)
        {
            return p != null && p.Exists() && !p.IsDead && !_director.Involves(p.Handle) && !InTalk(p.Handle);
        }

        /// The acknowledgement, a beat at a time: eyes first, then the gesture (and a word), the other's answer, then the
        /// one thought. Anyone gone, down or taken by something else is let be from then on.
        private void NodPlay(NodRun r, int now)
        {
            if (now - r.At > NodStaleMs + 4000 || !NodStill(r.A))
            {
                _nod = null;
                return;
            }
            int t = now - r.At;
            bool bHere = NodStill(r.B);
            switch (r.Step)
            {
                case 0:
                    if (MayDirect(r.A))
                        TASK.TASK_LOOK_AT_ENTITY(r.A.Handle, r.B.Handle, r.Act.LookMs, 0, 51, 0);
                    if (bHere && r.Act.Answer != "none" && MayDirect(r.B))
                        TASK.TASK_LOOK_AT_ENTITY(r.B.Handle, r.A.Handle, Math.Max(1200, r.Act.LookMs - 400), 0, 51, 0);
                    r.Step = 1;
                    break;
                case 1:
                    if (t < 450)
                        return;
                    NodGesture(r.A, r.B, r.Act.Gesture, r.AWoman, r.Seed + "|a", r.AName);
                    if (r.Act.Speaks && MayDirect(r.A))
                    {
                        string line, words;
                        int take;
                        bool spoke = Speech.PlayFirst(r.A.Handle, Passing.Greeting(CLOCK.GET_CLOCK_HOURS()), bHere ? r.B.Handle : 0, out line, out take, out words);
                        Log.Detail("NOD " + r.AName + (spoke ? " says " + line + (take > 0 ? " take " + take : "") : ": the greeting didn't play"));
                        if (spoke)
                            NodCaption(r.A, r.AName, Passing.HelloWords(CLOCK.GET_CLOCK_HOURS(), Person.FirstOf(r.BName), r.Seed));
                        r.Spoke = spoke;
                        if (spoke && bHere && !r.Test)
                            NodWord(r);   // a word in passing tells the freshest thing worth telling (memory v1, MindLive; the engine says nothing with it off)
                    }
                    r.Step = 2;
                    break;
                case 2:
                    if (t < 1300)
                        return;
                    if (bHere && r.Act.Answer != "none")
                        NodGesture(r.B, r.A, r.Act.Answer, r.BWoman, r.Seed + "|b", r.BName);
                    if (bHere && r.Act.Talks && r.Spoke)
                        NodSay(r.B, r.BName, r.A, Passing.GreetingBack, Passing.HelloBack(Person.FirstOf(r.AName), r.Seed), "answers");
                    r.Step = 3;
                    break;
                case 3:
                    if (t < (r.Act.BThinks ? 2600 : 1600))
                        return;
                    if (!r.Act.BThinks)
                        NodThought(r.A, r.AName, r.Act.Thought);
                    else if (bHere)
                        NodThought(r.B, r.BName, r.Act.Thought);
                    if (!(r.Act.Talks && r.Spoke))
                        _nod = null;
                    r.Step = 4;
                    break;
                case 4:
                    // parting: a word each (Lex, 2026-10-29: "Say Hi and Goodbye to each other when greeting and saying goodbye")
                    if (t < 4800)
                        return;
                    NodSay(r.A, r.AName, bHere ? r.B : null, Passing.Goodbye, Passing.ByeWords(r.Seed), "parts");
                    r.Step = 5;
                    break;
                case 5:
                    if (t < 6300)
                        return;
                    if (bHere)
                        NodSay(r.B, r.BName, r.A, Passing.Goodbye, Passing.ByeBack(r.Seed), "parts");
                    _nod = null;
                    break;
            }
        }

        /// One upper-body gesture toward the other (their feet stay where they are); someone in a scenario only looks.
        /// One of them says something in their own recorded voice (the first line their voice has of `lines`), and the
        /// words show on the ticker while they're within earshot.
        private void NodSay(Ped p, string name, Ped to, string[] lines, string words, string what)
        {
            if (p == null || !p.Exists() || p.IsDead || !MayDirect(p))
                return;
            string line, said;
            int take;
            bool spoke = Speech.PlayFirst(p.Handle, lines, to != null && to.Exists() ? to.Handle : 0, out line, out take, out said);
            Log.Detail("NOD " + name + " " + what + (spoke ? ": " + line + (take > 0 ? " take " + take : "") : ": nothing played"));
            if (spoke)
                NodCaption(p, name, words);
        }

        /// The words of a hello or a goodbye on the ticker (our own words), while the speaker is within ScenesOnTickerMetres.
        private void NodCaption(Ped p, string name, string words)
        {
            if (string.IsNullOrEmpty(words) || p == null || !p.Exists() || !_cfg.ScenesOnTicker || !_cfg.Ticker)
                return;
            if (p.Position.DistanceTo(Game.Player.Character.Position) > _cfg.ScenesOnTickerMetres)
                return;
            SpeechCaption(Person.FirstOf(name), words, p.Handle, Math.Min(5000, 1500 + words.Length * 55));
        }

        private void NodGesture(Ped p, Ped toward, string gesture, bool woman, string seed, string name)
        {
            if (gesture == null || gesture == "none" || gesture == "look" || MayDirectWhyNot(p) != null || PED.IS_PED_USING_ANY_SCENARIO(p.Handle))
                return;
            string dict = woman ? Gestures.FemaleSilent : Gestures.MaleSilent;
            if (!STREAMING.HAS_ANIM_DICT_LOADED(dict))
            {
                STREAMING.REQUEST_ANIM_DICT(dict);
                Log.Detail("NOD " + name + ": " + dict + " not loaded yet, only a look");
                return;
            }
            bool left = false;
            if (toward != null && toward.Exists())
            {
                // heading 0 is north and turns anticlockwise: the right hand points along (cos h, sin h)
                double hd = ENTITY.GET_ENTITY_HEADING(p.Handle) * Math.PI / 180.0;
                Vector3 rel = toward.Position - p.Position;
                left = rel.X * Math.Cos(hd) + rel.Y * Math.Sin(hd) < 0;
            }
            string clip = Passing.Clip(gesture, woman, left, seed);
            if (clip == null)
                return;
            TASK.TASK_PLAY_ANIM(p.Handle, dict, clip, 4f, -4f, -1, 8 | 16, 0f, false, 0, false, "", false);
            Log.Detail("NOD " + name + " plays " + clip);
        }

        /// A thought over their head, only while you can see them.
        private void NodThought(Ped p, string name, string thought)
        {
            if (thought == null || thought.Length == 0)
                return;
            Ped me = Game.Player.Character;
            if (p == null || !p.Exists() || !NodInView(me, p))
            {
                _nodUnseen++;
                Log.Detail("NOD thought not shown (" + name + " out of view): " + thought);
                return;
            }
            _nodShown++;
            _ticker.Push(name + ", thinking", thought, "near", p.Handle);
            Log.Write("NOD thought, " + name + ": " + thought);
        }

        /// The Testing page: the nearest pair with a tie within 10 m of each other, now (the rest and the roll skipped;
        /// they needn't be walking toward each other).
        private string NodFire(int now, out string skipped)
        {
            skipped = null;
            if (!_cfg.Acknowledge)
                return "[Scenes] Acknowledge is off";
            if (_nod != null)
                return "one is going on already";
            string seed, why;
            NodRun run = NodFind(Game.Player.Character, now, true, out seed, out why);
            if (run == null)
                return why;
            skipped = "walking toward each other" + (run.Said != null ? ", " + run.Said : "");
            NodStart(run, now, true);
            return _nod == null ? _nodWhyNot : null;
        }

        private void PassingAbort()
        {
            _nod = null;
            _nodApart.Clear();
        }

        /// F7: on or off, the counts by kind, the thoughts shown, the last, and why nothing's happening now.
        private string PassingStatus()
        {
            if (!_cfg.Acknowledge)
                return "off ([Scenes] Acknowledge)";
            return "on; this session: " + (_nodCount.Count == 0 ? "none yet" : string.Join(", ", _nodCount.Select(kv => kv.Key + " " + kv.Value))) +
                   "; thoughts shown " + _nodShown + ", out of view " + _nodUnseen + (_nodLast != null ? "; last: " + _nodLast : "") +
                   (_nod != null ? "; one going on" : _nodWhyNot != null ? "; now: " + _nodWhyNot : "") + "; known handles cached " + _nodWho.Count +
                   (_cfg.MindLive && _cfg.MindPassing ? "; the mind changed " + _passMinded + " greetings" + (_passMindedLast != null ? " (last: " + _passMindedLast + ")" : "") : "");
        }
    }
}
