// Reactions to a sight ([Scenes] SightReactions; the feelings and words are Core/Sights.cs): people near you notice
// how you look and take it by what they think of you and their nature. Bloodied: you lost a good part of your health
// in one go lately and haven't been in the water since (no native reads blood on clothes); drunk: the game's own
// drunkenness (not remarked in a saloon); filthy: the game's dirt level (its scale is checked at the first reading;
// trail dirt is remarked only in the city and the smart towns unless he's caked); soaked: in water up to the middle
// within the last half game hour (not in the rain, when everyone is). Up to two people within 10 m who can see you, one
// after the other, look, gesture (upper body), may say something to you in their own voice where the feeling has words
// for it (subtitled, as anything said to you is), and think it over their heads. Rare: a minute of play between
// reactions, three minutes for the same state, three a state in a town a day, once per person and state a day. Also: a
// man walking off bloodied from a street fight gets one onlooker's look and thought. Runs in its own guard ("sights").
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
        private const ulong GetPedDirtLevel = 0x0105FEE8F9091255;   // _GET_PED_DIRT_LEVEL(ped, useCompositeLayer) -> float (the natives database); not named in the .NET API
        private const float SightNear = 10f, FilthyAt = 0.6f, SoakedLevel = 0.45f;
        private const int SightSampleMs = 500, DirtSampleMs = 5000, SightLookMs = 1000, SightGapMs = 20000, BloodiedMinutes = 120, SoakedMinutes = 30, SightsPerTown = 3;

        private sealed class SightRun
        {
            public Ped By, At;          // the onlooker, and who they look at
            public string Name, State, Key;
            public bool Woman, ToYou;
            public Sights.Look Look;
            public int Start, Step;
            public string Seed;
        }

        private readonly List<SightRun> _sights = new List<SightRun>();
        private readonly Dictionary<string, long> _sightSeen = new Dictionary<string, long>();   // person|state -> game minute (not saved)
        private readonly Dictionary<string, int> _sightTown = new Dictionary<string, int>();     // state|town|day -> reactions
        private readonly Dictionary<int, int> _bloodiedSeen = new Dictionary<int, int>();        // a fighter's handle -> the scene they were looked at after
        private int _sightNextSample, _sightNextDirt, _sightNextLook, _sightNext, _sightHealth = -1, _sightCount, _sightShown, _sightTestTurn;
        private long _hurtAt = -100000, _wetAt = -100000;
        private float _dirt, _dirtMax, _drunkness;
        private bool _drunk, _sightProbed, _sightSets, _dirtOff;
        private Vector3 _sightWas;
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
            {
                SightsReset("you died");
                return;
            }
            if (now >= _sightNextSample)
            {
                _sightNextSample = now + SightSampleMs;
                SightSample(me, now);
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
            if (_sightState != null && Sights.Remarkable(_sightState, _placeKind, _placeTown, _dirt))
                SightLookForYou(me, now, _sightState, false);
            else
                SightLookForBloodied(me, now);
        }

        /// What the player looks like now: hurt lately (and not washed), drunk, filthy, soaked.
        private void SightSample(Ped me, int now)
        {
            int h = me.Handle;
            long g = _store.LastGameMin;
            Vector3 at = me.Position;
            if (_sightHealth >= 0 && _sightWas.DistanceTo(at) > 150f)
                SightsReset("a long way from where you were (fast travel, a respawn, a load)");
            _sightWas = at;
            int health = ENTITY.GET_ENTITY_HEALTH(h), max = Math.Max(1, ENTITY.GET_ENTITY_MAX_HEALTH(h, false));
            if (_sightHealth >= 0 && health > 0 && _sightHealth - health >= max * 0.12f)
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
            if (!_sightProbed)
            {
                _sightProbed = true;
                _sightSets = STREAMING.DOES_ANIM_DICT_EXIST(Gestures.MaleSilent) && STREAMING.DOES_ANIM_DICT_EXIST(Gestures.FemaleSilent);
                float first = Function.Call<float>(GetPedDirtLevel, h, false);
                _dirtOff = float.IsNaN(first) || first < 0f || first > 1.5f;   // not the 0-1 reading we expect: left alone from now on
                _dirt = _dirtOff ? 0f : first;
                Log.Write("SIGHT probe: dirt level " + first.ToString("0.000") + (_dirtOff ? " (not a 0-1 reading: filthy is off this session)" : " (filthy from " + FilthyAt + "; 0.9 outside the city)") +
                          ", drunk " + _drunk + " (" + _drunkness.ToString("0.00") + "), health " + health + " of " + max + ", the gesture sets " + (_sightSets ? "yes" : "NO"));
                _sightNextDirt = now + DirtSampleMs;
            }
            else if (!_dirtOff && now >= _sightNextDirt)
            {
                _sightNextDirt = now + DirtSampleMs;
                float d = Function.Call<float>(GetPedDirtLevel, h, false);
                if (float.IsNaN(d) || d < 0f || d > 1.5f)
                {
                    _dirtOff = true;
                    _dirt = 0f;
                    Log.Write("SIGHT: the dirt level read " + d.ToString("0.000") + ", not a 0-1 reading: filthy is off this session");
                }
                else
                    _dirt = d;
                if (_dirt > _dirtMax)
                    _dirtMax = _dirt;
            }
            string w = _engine != null ? _engine.NowWeather : null;
            bool raining = w == "rain" || w == "storm";
            bool soaked = !inWater && !raining && g - _wetAt >= 0 && g - _wetAt < SoakedMinutes;
            bool bloodied = g - _hurtAt >= 0 && g - _hurtAt < BloodiedMinutes;
            string was = _sightState;
            _sightState = Sights.StateOf(bloodied, _drunk || _drunkness > 0.25f, !_dirtOff && _dirt >= FilthyAt, soaked);
            if (_sightState != was)
                Log.Detail("SIGHT you look " + (_sightState ?? "ordinary") + " now (dirt " + _dirt.ToString("0.00") + ", drunkness " + _drunkness.ToString("0.00") + ")");
        }

        /// Forgets how you looked and who reacted (death, a load, a long jump, the memory wipe, shutdown).
        private void SightsReset(string why)
        {
            bool any = _sightState != null || _sights.Count > 0 || _hurtAt > -100000;
            _sights.Clear();
            _sightHealth = -1;
            _hurtAt = _wetAt = -100000;
            _sightState = null;
            if (why != "you died" || any)
                Log.Detail("SIGHT reset: " + why);
        }

        /// What someone already thinks of you, for how they take the sight: afraid (enRAGE's fear of you, or a killing
        /// of yours they saw in the last three days), hostile (a grudge or hate), fond, or null.
        private string SightRegard(Ped p, string id)
        {
            NemesisRecord rec = _bridge.RecordAt(p.Handle);
            if (rec != null)
            {
                Person rp = PersonOf(rec);
                return rp.Fear >= 40 ? "afraid" : rp.Hate >= 50 ? "hostile" : null;
            }
            if (id != null && _engine.SawYouViolent(id, 3))
                return "afraid";
            Acquaintance ac = _bridge.AcquaintanceAt(p.Handle);
            if (ac != null)
                return ac.Fear >= 40 ? "afraid" : ac.Grudge >= 40 ? "hostile" : ac.Like >= 40 ? "fond" : null;
            return null;
        }

        /// Up to two people within 10 m who can see you, nearest first, react to how you look.
        private void SightLookForYou(Ped me, int now, string state, bool test)
        {
            long g = _store.LastGameMin;
            string townKey = state + "|" + (_placeTown ?? "the wilds") + "|" + g / 1440;
            int inTown;
            _sightTown.TryGetValue(townKey, out inTown);
            if (!test && inTown >= SightsPerTown)
            {
                _sightWhyNot = "this town has seen you " + state + " enough today";
                return;
            }
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
            // the roll holds for half a game hour in a town, so looking again every second doesn't make it come sooner
            string why = Pacing.WhyNot(_store.Pace, "sight", null, state, g, Voice.Hash(townKey + "|" + g / 30) % 100, test, out skipped);
            if (why != null)
            {
                _sightWhyNot = "rests: " + why;
                return;
            }
            if (_sightSeen.Count > 500)
                foreach (string k in _sightSeen.Where(kv => g - kv.Value > 1440 || g < kv.Value).Select(kv => kv.Key).ToList())
                    _sightSeen.Remove(k);
            int n = 0;
            foreach (Ped p in found.OrderBy(x => x.Position.DistanceTo(me.Position)))
            {
                string id, name, temper;
                bool known = LastingId(p, out id, out name, out temper);
                string key = (known ? id : "h:" + p.Handle + ":" + ENTITY.GET_ENTITY_MODEL(p.Handle)) + "|" + state;
                long at;
                if (!test && _sightSeen.TryGetValue(key, out at) && g - at >= 0 && g - at < 1440)
                    continue;   // each person reacts to each state at most daily
                bool woman = !PED.IS_PED_MALE(p.Handle);
                string seed = key + "|" + (g / 1440);
                if (!known)
                    temper = Sights.StrangerTemper(seed);
                string regard = SightRegard(p, known ? id : null);
                var run = new SightRun
                {
                    By = p, At = me, ToYou = true, Name = SightName(p, known ? name : null, woman), State = state, Key = key, Woman = woman, Seed = seed,
                    Look = Sights.For(state, temper, regard, seed), Start = now + n * 1600, Step = 0
                };
                _sights.Add(run);
                _sightSeen[key] = g;
                Log.Write("SIGHT " + run.Name + ": you look " + state + " -> " + run.Look.Feeling + " (" + (temper ?? "plain") + (regard != null ? ", " + regard + " of you" : "") + "; " + run.Look.Gesture +
                          (run.Look.Voice != null ? ", " + run.Look.Voice[0] : "") + ", " + (int)p.Position.DistanceTo(me.Position) + " m)" + (test ? ", a test" + (skipped != null ? " (skipped: " + skipped + ")" : "") : ""));
                if (++n >= 2)
                    break;
            }
            if (n == 0)
            {
                _sightWhyNot = "everyone who can see you has already reacted to that today";
                return;
            }
            Pacing.Note(_store.Pace, "sight", null, state, g);
            _sightTown[townKey] = inTown + 1;
            if (_sightTown.Count > 200)
                _sightTown.Clear();
            _sightCount += n;
            _sightNext = now + SightGapMs;
            _sightLast = state + " (" + string.Join(", ", _sights.Select(s => s.Name + " " + s.Look.Feeling)) + ")" + (test ? ", a test" : "");
            SightSetsLoad();
        }

        /// A man walking off bloodied from a street fight in the last 15 minutes: one onlooker who can see him.
        private void SightLookForBloodied(Ped me, int now)
        {
            if (_bloodiedSeen.Count > 100)
                foreach (int k in _bloodiedSeen.Keys.Where(k => !_sceneAtHandle.ContainsKey(k)).ToList())
                    _bloodiedSeen.Remove(k);
            foreach (KeyValuePair<int, int> kv in _restMsOf)
            {
                int h = kv.Key, at, seenAfter;
                if (kv.Value != FoughtRestMs || !_sceneAtHandle.TryGetValue(h, out at) || now - at > FoughtRestMs || now - at < 20000)
                    continue;
                if (_bloodiedSeen.TryGetValue(h, out seenAfter) && seenAfter == at)
                    continue;   // looked at after this fight already
                if (!ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h) || _director.Involves(h))
                    continue;
                Ped f = new Ped(h);
                if (f.Position.DistanceTo(me.Position) > 25f || ENTITY.GET_ENTITY_HEALTH(h) >= 0.85f * Math.Max(1, ENTITY.GET_ENTITY_MAX_HEALTH(h, false)))
                    continue;
                _bloodiedSeen[h] = at;   // one look for it, found or not (no scan every second for a quarter hour)
                Ped by = null;
                float bestD = SightNear;
                foreach (Ped p in World.GetAllPeds())
                {
                    if (p == null || !p.Exists() || p.IsDead || p.Handle == me.Handle || p.Handle == h)
                        continue;
                    float d = p.Position.DistanceTo(f.Position);
                    int pr;
                    if (d >= bestD || (_restMsOf.TryGetValue(p.Handle, out pr) && pr == FoughtRestMs) || SightCant(p, me) != null ||
                        !ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(p.Handle, h, 17) || !Facing(p, f.Position, 80f))
                        continue;
                    by = p;
                    bestD = d;
                }
                if (by == null)
                    continue;
                string fid, fname, ftemper, id, name, temper;
                bool fknown = LastingId(f, out fid, out fname, out ftemper);
                bool known = LastingId(by, out id, out name, out temper);
                bool woman = !PED.IS_PED_MALE(by.Handle);
                string seed = (known ? id : "h:" + by.Handle) + "|" + h + "|" + now;
                if (!known)
                    temper = Sights.StrangerTemper(seed);
                string who = fknown ? Person.FirstOf(fname) : null;
                var run = new SightRun { By = by, At = f, Name = SightName(by, known ? name : null, woman), State = "bloodied (another)", Woman = woman, Seed = seed, Look = Sights.ForOther(temper, who, seed), Start = now };
                _sights.Add(run);
                _sightCount++;
                _sightNext = now + SightGapMs;
                _sightLast = "bloodied " + (who ?? "fighter") + " (" + run.Name + " " + run.Look.Feeling + ")";
                Log.Write("SIGHT " + run.Name + ": " + (who ?? "the fighter") + " walks off bloodied -> " + run.Look.Feeling + " (" + (temper ?? "plain") + ")");
                SightSetsLoad();
                return;
            }
            _sightWhyNot = _sightState != null ? "you look " + _sightState + ", nothing to remark on here" : "you look ordinary, and nobody bloodied from a fight is near";
        }

        private void SightSetsLoad()
        {
            foreach (string d in new[] { Gestures.MaleSilent, Gestures.FemaleSilent })
                if (!STREAMING.HAS_ANIM_DICT_LOADED(d))
                    STREAMING.REQUEST_ANIM_DICT(d);
        }

        /// Why this person can't react now, or null: a bystander (not the law, not fighting or fleeing), one we may
        /// direct, not in a scene, an act, a talk, a nod or at a counter, and not speaking.
        private string SightCant(Ped p, Ped me)
        {
            int h = p.Handle;
            if (!PED.IS_PED_HUMAN(h) || !Natives.Bystander(p, me.Handle, IsRecord(h)))
                return "not a bystander";
            if (_director.Involves(h) || _wakeActs.Any(x => x.Handle == h) || InTalk(h) || PostKindOf(p) != null || IsHermit(h) || IsGatherer(h) || IsExecutionPed(h) ||
                Speech.Busy(h) || IsAcknowledging(h))
                return "busy";
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

        /// One reaction, a beat at a time: the look, the gesture and any word, then the thought. True when done. Asked
        /// whether we may still direct them only as each beat starts, and let go the moment a scene or a talk takes them.
        private bool SightPlay(SightRun r, int now)
        {
            if (now < r.Start)
                return false;
            if (now - r.Start > 8000 || r.By == null || !r.By.Exists() || r.By.IsDead || r.At == null || !r.At.Exists() || _director.Involves(r.By.Handle) || InTalk(r.By.Handle))
                return true;
            int t = now - r.Start, h = r.By.Handle;
            switch (r.Step)
            {
                case 0:
                    if (MayDirectWhyNot(r.By) != null)
                        return true;
                    TASK.TASK_LOOK_AT_ENTITY(h, r.At.Handle, r.Look.LookMs, 0, 51, 0);
                    r.Step = 1;
                    return false;
                case 1:
                    if (t < 400)
                        return false;
                    r.Step = 2;
                    if (MayDirectWhyNot(r.By) != null)
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
                        if (r.ToYou && r.Look.Spoken != null)
                            Feed.Say(r.Name, r.Look.Spoken, Speech.WhenFirst(h, r.Look.Voice, r.At.Handle), h, toYou: true);   // said to you: subtitled while the voice plays
                        else
                        {
                            string line, words;
                            int take;
                            bool spoke = Speech.PlayFirst(h, r.Look.Voice, r.At.Handle, out line, out take, out words);   // a wordless sound
                            Log.Detail("SIGHT " + r.Name + (spoke ? " makes " + line : ": " + r.Look.Voice[0] + " didn't play"));
                        }
                    }
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
            return "you look " + (_sightState ?? "ordinary") + "; hurt " + (_hurtAt > -100000 ? (g - _hurtAt) + " game min ago" : "not lately") +
                   ", in the water " + (_wetAt > -100000 ? (g - _wetAt) + " game min ago" : "not lately") + ", drunk " + _drunk + " (" + _drunkness.ToString("0.00") + ")" +
                   ", dirt " + (_dirtOff ? "off (not a 0-1 reading)" : _dirt.ToString("0.00") + " (the most seen " + _dirtMax.ToString("0.00") + "; filthy from " + FilthyAt + ", 0.9 outside the city)") +
                   "; gesture sets " + (!_sightProbed ? "not looked yet" : _sightSets ? "yes" : "NO") + "; " + _sightCount + " reactions, " + _sightShown + " thoughts shown" +
                   (_sightLast != null ? "; last: " + _sightLast : "") + (_sightWhyNot != null ? "; now: " + _sightWhyNot : "");
        }
    }
}
