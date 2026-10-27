// The drinker after bad news ([Scenes] Vices; the why and the words are Core/Vices.cs): in a saloon from noon to 4 in the
// morning, someone named whose worry ended badly today, or who has just had word of a friend's death (a "heard" death
// memory come due in the last 12 game hours), drinks it away: a man standing free takes the game's drinking-drunk
// scenario where he is, anyone gets a drunk's voice, and their thought shows over their head; a friend within 5 m may
// come and say they're sorry (a "comfort" scene through the Director). They're drunk for the rest of the night
// (`Drinking`, which `InHisCups` reads, so the drunk's challenge and the pickpockets see it). Nothing saved. Pacing
// "drink" (55% a look every 30 s, an hour between, once a day for the same person).
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
        private const int DrinkLookMs = 30000;
        private readonly Dictionary<string, long> _drinkers = new Dictionary<string, long>();   // id -> game minute they're sober again
        private readonly Dictionary<int, long> _drunkVoices = new Dictionary<int, long>();      // handle -> when their voice sobers (SET_PED_IS_DRUNK back off)
        private int _nextDrink, _drinks, _drinkComforts, _drinkVoiceAt, _drinkVoice;
        private string _drinkLast, _drinkWhyNot;
        private Action _aDrinkVoice;

        /// Drinking the night away, by their lasting id (till 4 in the morning).
        private bool Drinking(string id)
        {
            long until;
            return id != null && _drinkers.TryGetValue(id, out until) && _store != null && _store.LastGameMin < until;
        }

        /// Someone in the saloon takes to drink after bad news. True if it started (with or without a friend's word).
        private bool DrinkScene(Ped me, int now, bool test, out string why)
        {
            why = null;
            if (!_cfg.Vices)
            {
                why = "[Scenes] Vices is off";
                return false;
            }
            if (_placeKind != "saloon")
            {
                why = "not in a saloon";
                return false;
            }
            int clock = CLOCK.GET_CLOCK_HOURS();
            if (!test && (now < _nextDrink || !(clock >= 12 || clock < 4)))
                return false;
            _nextDrink = now + DrinkLookMs;
            long g = _store.LastGameMin;
            string skipped0;
            string pre = Pacing.WhyNot(_store.Pace, "drink", _placeTown, null, g, _rng.Next(100), test, out skipped0);   // the rarity first: no scan for nothing
            if (pre != null)
            {
                why = _drinkWhyNot = "rests: " + pre;
                return false;
            }
            List<Candidate> people = ReasonCandidates(me, now, test).Where(c => c.Ped.Position.DistanceTo(me.Position) < 15f).ToList();
            Candidate drinker = null;
            string drinkWhy = null;
            foreach (Candidate c in people.OrderBy(x => x.Ped.Position.DistanceTo(me.Position)))
            {
                if (c.Who.P == null || Drinking(c.Who.Id))
                    continue;
                string wk;
                int ws;
                bool good;
                bool bad = InnerLife.WorryNow(c.Who.P, g / 1440, out wk, out ws, out good) && ws >= 3 && !good;
                string w = Vices.DrinkWhy(_engine.GriefFor(c.Who.Id, 720), bad, wk, c.Who.Temper) ?? (test ? "badnews:trouble" : null);
                if (w == null)
                    continue;
                drinker = c;
                drinkWhy = w;
                break;
            }
            if (drinker == null)
            {
                why = _drinkWhyNot = "nobody here with bad news to drink on";
                return false;
            }
            string skipped;
            string pace = Pacing.WhyNot(_store.Pace, "drink", _placeTown, drinker.Who.Id, g, 0, test, out skipped);
            skipped = skipped ?? skipped0;
            if (pace != null)
            {
                why = _drinkWhyNot = "rests: " + pace;
                return false;
            }
            Pacing.Note(_store.Pace, "drink", _placeTown, drinker.Who.Id, g);
            long sober = (g / 1440 + (g % 1440 >= 4 * 60 ? 1 : 0)) * 1440 + 4 * 60;   // the next 4 in the morning
            _drinkers[drinker.Who.Id] = sober;
            if (_drinkers.Count > 100)
                foreach (string k in _drinkers.Where(kv => kv.Value <= g).Select(kv => kv.Key).ToList())
                    _drinkers.Remove(k);
            int h = drinker.Ped.Handle;
            bool stance = false;
            if (!drinker.Who.Woman && !PED.IS_PED_USING_ANY_SCENARIO(h) && ENTITY.GET_ENTITY_SPEED(h) < 0.5f)
            {
                TASK.TASK_START_SCENARIO_IN_PLACE_HASH(h, DrinkingDrunk, 90000, true, 0u, -1f, false);   // where he stands, a minute and a half
                stance = true;
            }
            AUDIO.SET_PED_IS_DRUNK(h, true);
            _drunkVoices[h] = sober;
            _drinkVoice = h;
            _drinkVoiceAt = now + 20000 + _rng.Next(20000);
            string seed = drinker.Who.Id + "|drink|" + g / 1440;
            string thought = Vices.Thought(drinkWhy, drinker.Who.Temper, seed);
            if (NodInView(me, drinker.Ped))
                _ticker.Push(drinker.Who.Name + ", thinking", thought, "near", h);
            _drinks++;
            _drinkLast = drinker.Who.Name + " (" + drinkWhy + ")" + (test ? ", a test" : "");
            Log.Write("VICE drink: " + drinker.Who.Name + " drinks after " + (drinkWhy.StartsWith("grief:") ? "word of " + drinkWhy.Substring(6) + "'s death" : drinkWhy.StartsWith("loss:") ? "a death in the family (" + drinkWhy.Substring(5) + ")" : "bad news (" + drinkWhy.Substring(8) + ")") +
                      (stance ? ", the drinker's stance" : "") + "; thinks: " + thought + (test ? " (a test" + (skipped != null ? "; skipped: " + skipped : "") + ")" : ""));
            // a friend close by comes to say they're sorry
            Candidate friend = people.Where(o => o != drinker && o.Who.Id != drinker.Who.Id && o.Ped.Position.DistanceTo(drinker.Ped.Position) < 5f && TieWarmth(o.Who.Id, drinker.Who) > 0)
                                     .OrderBy(o => o.Ped.Position.DistanceTo(drinker.Ped.Position)).FirstOrDefault();
            if (friend != null && !_director.Busy)
            {
                SceneCast cast = BuildCast(drinker.Ped, friend.Ped, null, true, me, now, seed);
                Scene s = Vices.Comfort(cast, seed, drinkWhy);
                StartScene(s, new List<Ped> { drinker.Ped, friend.Ped }, cast, seed, now, test);
                if (_director.Busy)
                {
                    _drinkComforts++;
                    Log.Write("VICE drink: " + friend.Who.Name + " says they're sorry to " + drinker.Who.Name);
                }
            }
            return true;
        }

        /// The drunk voices whose night is over (or everyone's, `all`): sober again.
        private void SoberVoices(bool all)
        {
            if (_drunkVoices.Count == 0)
                return;
            long g = _store != null ? _store.LastGameMin : long.MaxValue;
            foreach (int h in _drunkVoices.Where(kv => all || g >= kv.Value || g < kv.Value - 1440).Select(kv => kv.Key).ToList())
            {
                if (ENTITY.DOES_ENTITY_EXIST(h))
                    AUDIO.SET_PED_IS_DRUNK(h, false);
                _drunkVoices.Remove(h);
            }
        }

        /// A while after they start, the drink in their voice: once.
        private void DrinkVoiceStep(int now)
        {
            if (_drinkVoice == 0 || now < _drinkVoiceAt)
                return;
            int h = _drinkVoice;
            _drinkVoice = 0;
            if (ENTITY.DOES_ENTITY_EXIST(h) && !ENTITY.IS_ENTITY_DEAD(h) && !_director.Involves(h) && !Speech.Busy(h))
                Speech.SayTag(h, "drunk", 0);
        }

        private void VicesReset()
        {
            SoberVoices(true);
            _drinkers.Clear();
            _drinkVoice = 0;
        }

        /// F7: on or off, who is drinking tonight, the count, the last, and why not lately.
        private string VicesStatus()
        {
            if (!_cfg.Vices)
                return "off ([Scenes] Vices)";
            long g = _store != null ? _store.LastGameMin : 0;
            return _drinks + " took to drink this session (" + _drinkComforts + " with a friend's word); drinking now: " + _drinkers.Count(kv => kv.Value > g) +
                   (_drinkLast != null ? "; last: " + _drinkLast : "") + (_drinkWhyNot != null ? "; the last look: " + _drinkWhyNot : "");
        }
    }
}
