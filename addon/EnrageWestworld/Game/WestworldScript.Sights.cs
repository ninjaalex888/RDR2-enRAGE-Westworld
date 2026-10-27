// Reactions to a sight ([Scenes] SightReactions; the feelings and words are Core/Sights.cs): people near you notice
// how you look and take it by their nature. Bloodied: you lost a good part of your health in one go lately and haven't
// been in the water since (no native reads blood on clothes); drunk: the game's own drunkenness; filthy:
// the game's dirt level (its scale is unproven, so it's logged); soaked: you were in water up to your middle within the
// last half game hour (not in the rain, when everyone is). Up to two people within 10 m who can see you, one after the
// other, look, gesture (upper body), may say something in their own voice where the feeling has words for it, and
// think it over their heads. Once per person and state a game day, and 20 s between reactions. Also: a man walking
// off bloodied from a street fight gets one onlooker's look and thought. Runs in its own guard ("sights").
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
        private const ulong GetPedDirtLevel = 0x0105FEE8F9091255;   // _GET_PED_DIRT_LEVEL(ped, useCompositeLayer): not named in the .NET API
        private const float SightNear = 10f, FilthyAt = 0.6f, SoakedLevel = 0.45f;
        private const int SightSampleMs = 500, SightLookMs = 1000, SightGapMs = 20000, BloodiedMinutes = 120, SoakedMinutes = 30;

        private sealed class SightRun
        {
            public Ped By, At;          // the onlooker, and who they look at
            public string Name, State, Key;
            public bool Woman;
            public Sights.Look Look;
            public int Start, Step;
            public string Seed;
        }

        private readonly List<SightRun> _sights = new List<SightRun>();
        private readonly Dictionary<string, long> _sightSeen = new Dictionary<string, long>();   // person|state -> game minute (not saved)
        private readonly HashSet<int> _bloodiedSeen = new HashSet<int>();                       // fighters already looked at
        private int _sightNextSample, _sightNextLook, _sightNext, _sightHealth = -1, _sightCount, _sightShown;
        private long _hurtAt = -100000, _wetAt = -100000;
        private float _dirt, _dirtMax, _drunkness;
        private bool _drunk, _sightProbed, _sightSets;
        private string _sightLast, _sightState, _sightWhyNot;
        private Action _aSights;

        private void SightsStep(int now)
        {
            if (!_cfg.SightReactions || _store == null)
                return;
            if (_aSights == null)
                _aSights = () => SightsTick(_now);
            Safe("sights", _aSights);
        }

        private void SightsTick(int now)
        {
            Ped me = Game.Player.Character;
            if (me == null || !me.Exists() || me.IsDead)
                return;
            if (now >= _sightNextSample)
            {
                _sightNextSample = now + SightSampleMs;
                SightSample(me);
            }
            for (int i = _sights.Count - 1; i >= 0; i--)
                if (SightPlay(_sights[i], now))
                    _sights.RemoveAt(i);
            if (_sights.Count > 0 || now < _sightNextLook || now < _sightNext)
                return;
            _sightNextLook = now + SightLookMs;
            if (_director.Busy)
            {
                _sightWhyNot = "a street scene is going on";
                return;
            }
            if (_sightState != null)
                SightLookForYou(me, now, _sightState, false);
            else
                SightLookForBloodied(me, now);
        }

        /// What the player looks like now: hurt lately (and not washed), drunk, filthy, soaked.
        private void SightSample(Ped me)
        {
            int h = me.Handle;
            long g = _store.LastGameMin;
            int health = ENTITY.GET_ENTITY_HEALTH(h), max = Math.Max(1, ENTITY.GET_ENTITY_MAX_HEALTH(h, false));
            if (_sightHealth >= 0 && _sightHealth - health >= max * 0.12f)
                _hurtAt = g;   // a good part of his health in one go: a beating, a bullet, a fall
            _sightHealth = health;
            bool inWater = PED.IS_PED_SWIMMING(h) || (ENTITY.IS_ENTITY_IN_WATER(h) && ENTITY.GET_ENTITY_SUBMERGED_LEVEL(h) >= SoakedLevel);
            if (inWater)
            {
                _wetAt = g;
                _hurtAt = -100000;   // the water washes the blood off
            }
            _drunk = PED._IS_PED_DRUNK(h);
            _drunkness = PED._GET_PED_DRUNKNESS(h);
            _dirt = Function.Call<float>(GetPedDirtLevel, h, false);
            if (_dirt > _dirtMax)
                _dirtMax = _dirt;
            if (!_sightProbed)
            {
                _sightProbed = true;
                _sightSets = STREAMING.DOES_ANIM_DICT_EXIST(Gestures.MaleSilent) && STREAMING.DOES_ANIM_DICT_EXIST(Gestures.FemaleSilent);
                Log.Write("SIGHT probe: dirt level " + _dirt.ToString("0.00") + " (filthy from " + FilthyAt + "), drunk " + _drunk + " (" + _drunkness.ToString("0.00") + "), health " + health + " of " + max +
                          ", the gesture sets " + (_sightSets ? "yes" : "NO"));
            }
            string w = _engine != null ? _engine.NowWeather : null;
            bool raining = w == "rain" || w == "storm";
            bool soaked = !inWater && !raining && g - _wetAt >= 0 && g - _wetAt < SoakedMinutes;
            bool bloodied = g - _hurtAt >= 0 && g - _hurtAt < BloodiedMinutes;
            bool filthy = _dirt >= FilthyAt && _dirt <= 1.5f;   // a reading over 1.5 means another scale than we think: none till it's known
            string was = _sightState;
            _sightState = Sights.StateOf(bloodied, _drunk || _drunkness > 0.25f, filthy, soaked);
            if (_sightState != was)
                Log.Detail("SIGHT you look " + (_sightState ?? "ordinary") + " now (dirt " + _dirt.ToString("0.00") + ", drunkness " + _drunkness.ToString("0.00") + ")");
        }

        /// Up to two people within 10 m who can see you, nearest first, react to how you look.
        private void SightLookForYou(Ped me, int now, string state, bool test)
        {
            long g = _store.LastGameMin;
            var found = new List<Ped>();
            foreach (Ped p in World.GetAllPeds())
            {
                if (p == null || p.Handle == me.Handle || !p.Exists() || p.IsDead || p.Position.DistanceTo(me.Position) > SightNear)
                    continue;
                if (SightCant(p, me) != null || !ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(p.Handle, me.Handle, 17) || !Facing(p, me.Position, 75f))
                    continue;
                found.Add(p);
            }
            if (found.Count == 0)
            {
                _sightWhyNot = "nobody within " + SightNear + " m who can see you";
                return;
            }
            string skipped;
            string why = Pacing.WhyNot(_store.Pace, "sight", null, null, g, _rng.Next(100), test, out skipped);
            if (why != null)
            {
                _sightWhyNot = "rests: " + why;
                return;
            }
            int n = 0;
            foreach (Ped p in found.OrderBy(x => x.Position.DistanceTo(me.Position)))
            {
                string id, name, temper;
                bool known = LastingId(p, out id, out name, out temper);
                string key = (known ? id : "h:" + p.Handle) + "|" + state;
                long at;
                if (!test && _sightSeen.TryGetValue(key, out at) && g - at >= 0 && g - at < 1440)
                    continue;   // each person reacts to each state at most daily
                bool woman = !PED.IS_PED_MALE(p.Handle);
                string seed = key + "|" + (g / 1440);
                var run = new SightRun
                {
                    By = p, At = me, Name = SightName(p, known ? name : null, woman), State = state, Key = key, Woman = woman, Seed = seed,
                    Look = Sights.For(state, temper, seed), Start = now + n * 1600, Step = 0
                };
                _sights.Add(run);
                _sightSeen[key] = g;
                Log.Write("SIGHT " + run.Name + ": you look " + state + " -> " + run.Look.Feeling + " (" + (temper ?? "no temper") + "; " + run.Look.Gesture +
                          (run.Look.Voice != null ? ", " + run.Look.Voice[0] : "") + ", " + (int)p.Position.DistanceTo(me.Position) + " m)" + (test ? ", a test" + (skipped != null ? " (skipped: " + skipped + ")" : "") : ""));
                if (++n >= 2)
                    break;
            }
            if (n == 0)
            {
                _sightWhyNot = "everyone who can see you has already reacted to that today";
                return;
            }
            Pacing.Note(_store.Pace, "sight", null, null, g);
            _sightCount += n;
            _sightNext = now + SightGapMs;
            _sightLast = state + " (" + string.Join(", ", _sights.Select(s => s.Name + " " + s.Look.Feeling)) + ")" + (test ? ", a test" : "");
            SightSetsLoad();
        }

        /// A man walking off bloodied from a street fight in the last 15 minutes: one onlooker who can see him.
        private void SightLookForBloodied(Ped me, int now)
        {
            foreach (KeyValuePair<int, int> kv in _restMsOf)
            {
                int h = kv.Key, at;
                if (kv.Value != FoughtRestMs || _bloodiedSeen.Contains(h) || !_sceneAtHandle.TryGetValue(h, out at) || now - at > FoughtRestMs || now - at < 20000)
                    continue;
                if (!ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h) || _director.Involves(h))
                    continue;
                Ped f = new Ped(h);
                if (f.Position.DistanceTo(me.Position) > 25f || ENTITY.GET_ENTITY_HEALTH(h) >= 0.85f * Math.Max(1, ENTITY.GET_ENTITY_MAX_HEALTH(h, false)))
                    continue;
                Ped by = World.GetAllPeds().Where(p => p != null && p.Exists() && !p.IsDead && p.Handle != me.Handle && p.Handle != h && p.Position.DistanceTo(f.Position) < SightNear &&
                                                       !(_restMsOf.ContainsKey(p.Handle) && _restMsOf[p.Handle] == FoughtRestMs) && SightCant(p, me) == null &&
                                                       ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(p.Handle, h, 17) && Facing(p, f.Position, 80f))
                                       .OrderBy(p => p.Position.DistanceTo(f.Position)).FirstOrDefault();
                if (by == null)
                    continue;
                _bloodiedSeen.Add(h);
                string fid, fname, ftemper, id, name, temper;
                bool fknown = LastingId(f, out fid, out fname, out ftemper);
                bool known = LastingId(by, out id, out name, out temper);
                bool woman = !PED.IS_PED_MALE(by.Handle);
                string seed = (known ? id : "h:" + by.Handle) + "|" + h + "|" + now;
                string who = fknown ? Person.FirstOf(fname) : null;
                var run = new SightRun { By = by, At = f, Name = SightName(by, known ? name : null, woman), State = "bloodied (another)", Woman = woman, Seed = seed, Look = Sights.ForOther(temper, who, seed), Start = now };
                _sights.Add(run);
                _sightCount++;
                _sightNext = now + SightGapMs;
                _sightLast = "bloodied " + (who ?? "fighter") + " (" + run.Name + " " + run.Look.Feeling + ")";
                Log.Write("SIGHT " + run.Name + ": " + (who ?? "the fighter") + " walks off bloodied -> " + run.Look.Feeling + " (" + (temper ?? "no temper") + ")");
                SightSetsLoad();
                return;
            }
            _sightWhyNot = "you look ordinary, and nobody bloodied from a fight is near";
        }

        private void SightSetsLoad()
        {
            foreach (string d in new[] { Gestures.MaleSilent, Gestures.FemaleSilent })
                if (!STREAMING.HAS_ANIM_DICT_LOADED(d))
                    STREAMING.REQUEST_ANIM_DICT(d);
        }

        /// Why this person can't react now, or null: a bystander (not the law, not fighting or fleeing), one we may
        /// direct, not in a scene, an act, a talk or at a counter, and not speaking.
        private string SightCant(Ped p, Ped me)
        {
            int h = p.Handle;
            if (!PED.IS_PED_HUMAN(h) || !Natives.Bystander(p, me.Handle, IsRecord(h)))
                return "not a bystander";
            if (_director.Involves(h) || _wakeActs.Any(x => x.Handle == h) || InTalk(h) || PostKindOf(p) != null || IsHermit(h) || IsGatherer(h) || IsExecutionPed(h) || Speech.Busy(h))
                return "busy";
            if (_sights.Any(s => s.By != null && s.By.Handle == h))
                return "reacting already";
            return MayDirectWhyNot(p);
        }

        /// Turned toward a point, within this many degrees.
        private static bool Facing(Ped p, Vector3 at, float degrees)
        {
            double hd = ENTITY.GET_ENTITY_HEADING(p.Handle) * Math.PI / 180.0;
            Vector3 rel = at - p.Position;
            double len = Math.Sqrt(rel.X * rel.X + rel.Y * rel.Y);
            if (len < 0.5)
                return true;
            double fwd = (-rel.X * Math.Sin(hd) + rel.Y * Math.Cos(hd)) / len;   // heading 0 faces north and turns anticlockwise
            return fwd >= Math.Cos(degrees * Math.PI / 180.0);
        }

        /// A name for the ticker's label: the one we know them by, else how they look ("A farmhand").
        private static string SightName(Ped p, string known, bool woman)
        {
            return known ?? Grammar.UpperFirst(Director.Kind(p, woman));
        }

        /// One reaction, a beat at a time: the look, the gesture and any word, then the thought. True when done.
        private bool SightPlay(SightRun r, int now)
        {
            if (now < r.Start)
                return false;
            if (now - r.Start > 8000 || r.By == null || !r.By.Exists() || r.By.IsDead || r.At == null || !r.At.Exists() || MayDirectWhyNot(r.By) != null)
                return true;
            int t = now - r.Start, h = r.By.Handle;
            switch (r.Step)
            {
                case 0:
                    TASK.TASK_LOOK_AT_ENTITY(h, r.At.Handle, r.Look.LookMs, 0, 51, 0);
                    r.Step = 1;
                    return false;
                case 1:
                    if (t < 400)
                        return false;
                    string clip = Sights.Clip(r.Look.Gesture, r.Woman, r.Seed);
                    string dict = r.Woman ? Gestures.FemaleSilent : Gestures.MaleSilent;
                    if (clip != null && !PED.IS_PED_USING_ANY_SCENARIO(h) && STREAMING.HAS_ANIM_DICT_LOADED(dict))
                    {
                        TASK.TASK_PLAY_ANIM(h, dict, clip, 4f, -4f, -1, 8 | 16, 0f, false, 0, false, "", false);
                        Log.Detail("SIGHT " + r.Name + " plays " + clip);
                    }
                    if (r.Look.Voice != null && r.By.Position.DistanceTo(r.At.Position) < 8f)
                    {
                        string line, words;
                        int take;
                        bool spoke = Speech.PlayFirst(h, r.Look.Voice, r.At.Handle, out line, out take, out words);
                        Log.Detail("SIGHT " + r.Name + (spoke ? " says " + line : ": " + r.Look.Voice[0] + " didn't play"));
                    }
                    r.Step = 2;
                    return false;
                default:
                    if (t < 900)
                        return false;
                    if (!string.IsNullOrEmpty(r.Look.Thought) && NodInView(Game.Player.Character, r.By))
                    {
                        _sightShown++;
                        _ticker.Push(r.Name + ", thinking", r.Look.Thought, "near", h);
                        Log.Write("SIGHT thought, " + r.Name + ": " + r.Look.Thought);
                    }
                    return true;
            }
        }

        /// The Testing page: the nearest who can see you react now, to how you look, or (looking ordinary) to the
        /// next state in turn, as if you looked it.
        private string SightFire(int now, out string skipped)
        {
            skipped = null;
            if (!_cfg.SightReactions)
                return "[Scenes] SightReactions is off";
            if (_sights.Count > 0)
                return "a reaction is going on already";
            string state = _sightState;
            if (state == null)
            {
                state = Sights.States[_sightTestTurn++ % Sights.States.Length];
                skipped = "how you look (as if " + state + ")";
            }
            SightLookForYou(Game.Player.Character, now, state, true);
            return _sights.Count == 0 ? _sightWhyNot ?? "nobody reacted" : null;
        }

        private int _sightTestTurn;

        private void SightsAbort()
        {
            _sights.Clear();
        }

        /// F7: how you look now and why, the readings, the reactions this session and the last.
        private string SightsStatus()
        {
            if (!_cfg.SightReactions)
                return "off ([Scenes] SightReactions)";
            long g = _store != null ? _store.LastGameMin : 0;
            return "you look " + (_sightState ?? "ordinary") + "; hurt " + (g - _hurtAt < 100000 ? (g - _hurtAt) + " game min ago" : "not lately") +
                   ", in the water " + (g - _wetAt < 100000 ? (g - _wetAt) + " game min ago" : "not lately") + ", drunk " + _drunk + " (" + _drunkness.ToString("0.00") + ")" +
                   ", dirt " + _dirt.ToString("0.00") + " (the most seen " + _dirtMax.ToString("0.00") + "; filthy from " + FilthyAt + ")" +
                   "; gesture sets " + (!_sightProbed ? "not looked yet" : _sightSets ? "yes" : "NO") + "; " + _sightCount + " reactions, " + _sightShown + " thoughts shown" +
                   (_sightLast != null ? "; last: " + _sightLast : "") + (_sightWhyNot != null ? "; now: " + _sightWhyNot : "");
        }
    }
}
