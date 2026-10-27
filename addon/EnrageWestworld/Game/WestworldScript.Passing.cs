// Acknowledgement in passing ([Scenes] Acknowledge; the choice and the words are Core/Passing.cs): two people with a
// tie between them (Ties.cs) who come within 5 m of each other near the player, one of them walking, acknowledge it:
// friends nod, tip a hat or wave, and one may say hello in their own voice; enemies give each other a hard look (the
// cold and the proud may spit); sweethearts look a beat too long. Each thinks something of it, over their head while
// you can see them. Looks and upper-body gestures only: nobody's feet are moved, nobody's task is cleared. One at a
// time, a pair once in 45 game minutes (Pacing "nod"), only people we may direct (MayDirect). Runs in its own guard
// ("passing"); nothing is saved but the pair's rest.
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
        private const float NodApart = 5f, NodTestApart = 10f, NodFromYou = 25f;
        private const int NodScanMs = 1500, NodKnownMs = 10000, NodGapMs = 8000, NodStaleMs = 8000;

        private sealed class NodWho
        {
            public uint Model;
            public int Until;
            public string Id, Name, Temper;   // Id null: nobody we know
        }

        private sealed class NodRun
        {
            public Ped A, B;
            public string AName, BName, Pair;
            public bool AWoman, BWoman;
            public Passing.Act Act;
            public int At, Step;
            public string Seed, Said;
        }

        private readonly Dictionary<int, NodWho> _nodWho = new Dictionary<int, NodWho>();
        private readonly HashSet<string> _nodIds = new HashSet<string>();
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
            if (now < _nextNodScan || now < _nextNod)
                return;
            _nextNodScan = now + NodScanMs;
            if (_director.Busy)
            {
                _nodWhyNot = "a street scene is going on (its thoughts have the ticker)";
                return;
            }
            if (_nodWho.Count > 300)
                foreach (int h in _nodWho.Where(kv => now - kv.Value.Until > 60000).Select(kv => kv.Key).ToList())
                    _nodWho.Remove(h);   // handles long gone
            Ped me = Game.Player.Character;
            if (me == null || !me.Exists() || me.IsDead)
                return;
            string seed;
            NodRun run = NodFind(me, now, false, out seed, out _nodWhyNot);
            if (run != null)
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

        /// The pair to acknowledge each other now: both known, a tie between them, close to each other (one of them on
        /// the move, unless it's a test), one of them in your view within 25 m; the nearest to you. Null, with why not.
        private NodRun NodFind(Ped me, int now, bool test, out string seed, out string whyNot)
        {
            seed = null;
            whyNot = null;
            var near = new List<Ped>();
            var who = new List<NodWho>();
            _nodIds.Clear();
            foreach (Ped p in World.GetAllPeds())
            {
                if (p == null || p.Handle == me.Handle || !p.Exists() || p.IsDead || p.Position.DistanceTo(me.Position) > NodFromYou + NodTestApart)
                    continue;
                int h = p.Handle;
                if (!PED.IS_PED_HUMAN(h) || Natives.StoryModel(p) || _director.Involves(h) || _wakeActs.Any(x => x.Handle == h) || InTalk(h) ||
                    PostKindOf(p) != null || IsHermit(h) || IsGatherer(h) || IsExecutionPed(h) || Speech.Busy(h))
                    continue;
                NodWho w = NodKnow(p, now);
                if (w.Id == null)
                    continue;
                near.Add(p);
                who.Add(w);
                _nodIds.Add(w.Id);
            }
            if (near.Count < 2)
            {
                whyNot = "fewer than two people we know near you";
                return null;
            }
            var ties = new Dictionary<string, SceneTie>();
            foreach (SceneTie t in _store.Ties)
                if (t.A != null && t.B != null && _nodIds.Contains(t.A) && _nodIds.Contains(t.B))
                    ties[NodPair(t.A, t.B)] = t;
            if (ties.Count == 0)
            {
                whyNot = near.Count + " we know near you, no ties between them";
                return null;
            }
            NodRun best = null;
            float bestFromYou = float.MaxValue;
            string why = "no pair with a tie close enough to each other";
            long g = _store.LastGameMin;
            for (int i = 0; i < near.Count; i++)
                for (int j = i + 1; j < near.Count; j++)
                {
                    SceneTie t;
                    string pair = NodPair(who[i].Id, who[j].Id);
                    if (who[i].Id == who[j].Id || !ties.TryGetValue(pair, out t))
                        continue;
                    Ped a = near[i], b = near[j];
                    float apart = a.Position.DistanceTo(b.Position);
                    if (apart > (test ? NodTestApart : NodApart) || apart < 0.8f)
                        continue;
                    if (!test && ENTITY.GET_ENTITY_SPEED(a.Handle) < 0.6f && ENTITY.GET_ENTITY_SPEED(b.Handle) < 0.6f)
                    {
                        why = "the pair close by are standing still (that's for a conversation)";
                        continue;
                    }
                    float fromYou = Math.Min(a.Position.DistanceTo(me.Position), b.Position.DistanceTo(me.Position));
                    if (fromYou > NodFromYou || fromYou >= bestFromYou || (!NodInView(me, a) && !NodInView(me, b)))
                        continue;
                    string pace, skipped;
                    string pseed = pair + "|" + (g / 45);
                    pace = Pacing.WhyNot(_store.Pace, "nod", null, pair, g, Voice.Hash(pseed + "|pace") % 100, test, out skipped);
                    if (pace != null)
                    {
                        why = "rests: " + pace;
                        continue;
                    }
                    string aWhy = MayDirectWhyNot(a), bWhy = MayDirectWhyNot(b);
                    if (aWhy != null && bWhy != null)
                    {
                        why = "we mayn't direct either (" + aWhy + ")";
                        continue;
                    }
                    bool romantic = Ties.Romantic(t.Kind);
                    string kind = Passing.KindOf(t.Warmth, romantic);
                    bool aFirst = Passing.AFirst(kind, who[i].Id, who[j].Id, who[i].Temper, who[j].Temper, t.Doer, pseed);
                    if (aWhy != null)
                        aFirst = false;   // the one we may direct acts; the other only thinks
                    else if (bWhy != null)
                        aFirst = true;
                    int x = aFirst ? i : j, y = aFirst ? j : i;
                    bool xWoman = !PED.IS_PED_MALE(near[x].Handle), yWoman = !PED.IS_PED_MALE(near[y].Handle);
                    string xFirst = Person.FirstOf(who[x].Name), yFirst = Person.FirstOf(who[y].Name);
                    best = new NodRun
                    {
                        A = near[x], B = near[y], AName = who[x].Name, BName = who[y].Name, AWoman = xWoman, BWoman = yWoman, Pair = pair, Seed = pseed,
                        Act = Passing.For(t.Warmth, romantic, xWoman, yWoman, who[x].Temper, who[y].Temper, xFirst, yFirst, pseed),
                        Said = skipped
                    };
                    bestFromYou = fromYou;
                    seed = pseed;
                }
            if (best == null)
                whyNot = why;
            return best;
        }

        private static string NodPair(string a, string b)
        {
            return string.CompareOrdinal(a, b) < 0 ? a + "|" + b : b + "|" + a;
        }

        private bool InTalk(int h)
        {
            return _talk != null && _talk.People != null && _talk.People.Any(p => p != null && p.Handle == h);
        }

        /// On your screen with nothing in the way.
        private static bool NodInView(Ped me, Ped p)
        {
            return ENTITY.IS_ENTITY_ON_SCREEN(p.Handle) && ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(me.Handle, p.Handle, 17);
        }

        private void NodStart(NodRun run, int now, bool test)
        {
            run.At = now;
            run.Step = 0;
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

        /// The acknowledgement, a beat at a time: eyes first, then the gesture (and a word), their thought, the other's
        /// answer, the other's thought. Anyone gone, down or taken by something else is let be.
        private void NodPlay(NodRun r, int now)
        {
            if (now - r.At > NodStaleMs + 4000 || r.A == null || !r.A.Exists() || r.A.IsDead)
            {
                _nod = null;
                return;
            }
            int t = now - r.At;
            bool bHere = r.B != null && r.B.Exists() && !r.B.IsDead;
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
                    }
                    r.Step = 2;
                    break;
                case 2:
                    if (t < 900)
                        return;
                    NodThought(r.A, r.AName, r.Act.AThought);
                    r.Step = 3;
                    break;
                case 3:
                    if (t < 1300)
                        return;
                    if (bHere && r.Act.Answer != "none")
                        NodGesture(r.B, r.A, r.Act.Answer, r.BWoman, r.Seed + "|b", r.BName);
                    r.Step = 4;
                    break;
                case 4:
                    if (t < 2900)
                        return;
                    if (bHere && r.Act.BThought != null)
                        NodThought(r.B, r.BName, r.Act.BThought);
                    _nod = null;
                    break;
            }
        }

        /// One upper-body gesture toward the other (their feet stay where they are); someone in a scenario only looks.
        private void NodGesture(Ped p, Ped toward, string gesture, bool woman, string seed, string name)
        {
            if (gesture == null || gesture == "none" || MayDirectWhyNot(p) != null || PED.IS_PED_USING_ANY_SCENARIO(p.Handle))
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
        /// they needn't be walking).
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
            skipped = "walking past each other" + (run.Said != null ? ", " + run.Said : "");
            NodStart(run, now, true);
            return null;
        }

        private void PassingAbort()
        {
            _nod = null;
        }

        /// F7: on or off, the counts by kind, the thoughts shown, the last, and why nothing's happening now.
        private string PassingStatus()
        {
            if (!_cfg.Acknowledge)
                return "off ([Scenes] Acknowledge)";
            return "on; this session: " + (_nodCount.Count == 0 ? "none yet" : string.Join(", ", _nodCount.Select(kv => kv.Key + " " + kv.Value))) +
                   "; thoughts shown " + _nodShown + ", out of view " + _nodUnseen + (_nodLast != null ? "; last: " + _nodLast : "") +
                   (_nod != null ? "; one going on" : _nodWhyNot != null ? "; now: " + _nodWhyNot : "") + "; known handles cached " + _nodWho.Count;
        }
    }
}
