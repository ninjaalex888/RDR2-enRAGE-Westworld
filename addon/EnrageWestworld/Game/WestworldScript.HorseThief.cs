// A horse thief (the NPC-life run, step 5; Lex, 2026-10-30). In a town, out of doors, with no lawman within 40 m: a man
// whose mind leans to it (little honesty, an appetite for risk, money short) or one already known for thieving (a
// pickpocket, a mugger, a horse thief: roadmap 28) walks to a horse standing with nobody on it, not the player's, gets up
// and rides off. Its owner (the last who rode it), if near, shouts after him. The thief is marked a horse thief, carries
// the thief's red dot and is fair game. At most once a town in two game hours, a roll of the mind's when one fits.
// Behind [Scenes] HorseThieves and MindLive.
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
        private const string HorseThiefMark = "horsethief";
        private sealed class HorseTheft
        {
            public Ped Thief, Horse, Owner;
            public string Name;
            public int Started, Phase;   // 0 walking to it and up, 1 away
        }

        private HorseTheft _horseTheft;
        private int _horseNextAt, _horseThefts;
        private readonly Dictionary<string, long> _horseTried = new Dictionary<string, long>();

        private bool HorseThiefScene(Ped me, int now)
        {
            if (!_cfg.HorseThieves || !_cfg.MindLive || _horseTheft != null || now < _horseNextAt || _placeTown == null || _interiorType != 0 || _theft != null)
                return false;
            _horseNextAt = now + 30000;
            long g = _store.LastGameMin;
            long last;
            if (_horseTried.TryGetValue(_placeTown, out last) && g - last >= 0 && g - last < 120)
                return false;
            if (LawmanNear(me, 40f))
                return false;
            int mine = PED._GET_LAST_MOUNT(me.Handle);
            // horses standing with nobody on them, near the player
            var horses = new List<Ped>();
            var riders = new HashSet<int>();
            foreach (Ped p in World.GetAllPeds())
            {
                if (p == null || p.Position.DistanceTo(me.Position) > 40f || p.IsDead)
                    continue;
                if (PED.IS_PED_HUMAN(p.Handle))
                {
                    if (PED.IS_PED_ON_MOUNT(p.Handle))
                        riders.Add(PED.GET_MOUNT(p.Handle));
                    continue;
                }
                string m = Natives.ModelName(p);
                if (m.StartsWith("a_c_horse", StringComparison.OrdinalIgnoreCase) && p.Handle != mine && ENTITY.GET_ENTITY_SPEED(p.Handle) < 0.3f)
                    horses.Add(p);
            }
            horses.RemoveAll(h => riders.Contains(h.Handle));
            if (horses.Count == 0)
                return false;
            Candidate thief = null;
            Ped horse = null;
            foreach (Candidate c in ReasonCandidates(me, now))
            {
                if (c.Who.Woman || PED.IS_PED_USING_ANY_SCENARIO(c.Ped.Handle) || PED.IS_PED_ON_MOUNT(c.Ped.Handle))
                    continue;
                PersonState st = _store.Find(c.Who.Id);
                bool known = st != null && (st.Has(PickpocketMark) || st.Has(MuggerMark) || st.Has(HorseThiefMark));
                MindWho w = MindKnown(c.Ped.Handle, now);
                Traits t = TraitsAt(c.Ped, now);
                bool leans = t != null && t.Z[Traits.Honesty] < -0.6f && t.Z[Traits.Risk] > 0.3f && w != null && w.Needs != null && w.Needs[NeedState.Money] >= 0.5f;
                if (!known && !leans)
                    continue;
                Ped near = horses.Where(h => h.Position.DistanceTo(c.Ped.Position) < 25f && PED._GET_LAST_MOUNT(c.Ped.Handle) != h.Handle).OrderBy(h => h.Position.DistanceTo(c.Ped.Position)).FirstOrDefault();
                if (near != null)
                {
                    thief = c;
                    horse = near;
                    break;
                }
            }
            if (thief == null)
                return false;
            _horseTried[_placeTown] = g;
            string seed = thief.Ped.Handle + "|" + horse.Handle + "|" + g / 120;
            if (!Voice.Chance(seed + "|horse", 45))
            {
                Log.Detail("HORSE " + thief.Who.Name + " eyes a horse, and leaves it be");
                return false;
            }
            Ped owner = World.GetAllPeds().FirstOrDefault(p => p != null && p.Handle != thief.Ped.Handle && p.Handle != me.Handle && PED.IS_PED_HUMAN(p.Handle) && !p.IsDead &&
                                                               p.Position.DistanceTo(horse.Position) < 60f && PED._GET_LAST_MOUNT(p.Handle) == horse.Handle);
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(thief.Ped.Handle, true);
            TASK.TASK_MOUNT_ANIMAL(thief.Ped.Handle, horse.Handle, 20000, -1, 1.6f, 1, 0, 0);
            _horseTheft = new HorseTheft { Thief = thief.Ped, Horse = horse, Owner = owner, Name = thief.Who.Name, Started = now };
            PersonState ts = _store.Find(thief.Who.Id);
            if (ts != null)
                ts.Mark(HorseThiefMark);
            NoteInScene(new[] { thief.Ped }, "lift", now);
            Log.Write("HORSE " + thief.Who.Name + " goes for a horse that isn't his, " + (int)horse.Position.DistanceTo(me.Position) + " m from you" + (owner != null ? " (its owner near)" : ""));
            return true;
        }

        private void HorseThiefStep(Ped me, int now)
        {
            HorseTheft t = _horseTheft;
            if (t == null)
                return;
            if (!t.Thief.Exists() || t.Thief.IsDead || !t.Horse.Exists() || t.Horse.IsDead || now - t.Started > 90000)
            {
                _horseTheft = null;
                return;
            }
            if (t.Phase == 0)
            {
                if (PED.IS_PED_ON_MOUNT(t.Thief.Handle) && PED.GET_MOUNT(t.Thief.Handle) == t.Horse.Handle)
                {
                    t.Phase = 1;
                    _horseThefts++;
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(t.Thief.Handle, false);
                    TASK.TASK_SMART_FLEE_PED(t.Thief.Handle, me.Handle, 400f, -1, 0, 3f, 0);   // away at a gallop
                    MarkThief(t.Thief, now);   // the thief's red dot, fair game
                    _thieves.Add(t.Thief.Handle);
                    if (t.Owner != null && t.Owner.Exists() && !t.Owner.IsDead)
                    {
                        Speech.SayTag(t.Owner.Handle, "thief", t.Thief.Handle);
                        string line = Voice.Pick(t.Started + "|own", "Horse thief! That's my horse!", "Stop him! He's got my horse!", "Hey! Get off her! Thief!");
                        if (_cfg.ScenesOnTicker && _cfg.Ticker && t.Owner.Position.DistanceTo(me.Position) <= _cfg.ScenesOnTickerMetres)
                            SpeechCaption("The owner", line, t.Owner.Handle, 3000);
                        Log.Write("HORSE   " + line);
                    }
                    Log.Write("HORSE " + t.Name + " rides off on it: a red dot, fair game");
                    return;
                }
                if (now - t.Started > 25000)
                {
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(t.Thief.Handle, false);
                    Log.Write("HORSE " + t.Name + " never got up on it");
                    _horseTheft = null;
                }
                return;
            }
            if (t.Thief.Position.DistanceTo(me.Position) > 200f)
                _horseTheft = null;   // gone
        }

        private string HorseStatus()
        {
            return !_cfg.HorseThieves ? "off ([Scenes] HorseThieves)" : _horseThefts + " horses stolen near you this session" + (_horseTheft != null ? "; one under way" : "");
        }
    }
}
