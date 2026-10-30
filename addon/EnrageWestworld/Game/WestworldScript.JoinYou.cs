// Onlookers join a fistfight of the player's (Lex, 2026-10-30: "me starting a brawl near others, to see if they join in").
// The same weighing as a street fight's (Core/JoinIn.cs), with the player on one side and the one he's at blows with on the
// other: a friend of that man, or someone against the player, may come at him; someone fond of him may take his side; a
// stranger seldom, a drunk hothead in a saloon now and then; nobody with the law at hand. Fists only. Those who come at him
// came at him (the self-defence rules hold). When he's done fighting they leave off. Behind [Mind] MindJoinFights and
// MindLive.
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
        private int _youFightNextAt, _youFightFoe, _youFightLastAt, _youJoined;
        private readonly List<int> _youJoiners = new List<int>();

        private void JoinYouStep(int now)
        {
            if (!_cfg.MindLive || !_cfg.MindJoinFights || _joinFailed || now < _youFightNextAt)
                return;
            _youFightNextAt = now + JoinEveryMs;
            Ped me = Game.Player.Character;
            int foe = MeleeFoe(me);
            if (foe == 0)
            {
                if (_youJoiners.Count > 0 && now - _youFightLastAt > 6000)
                {
                    foreach (int h in _youJoiners)
                        if (ENTITY.DOES_ENTITY_EXIST(h) && !ENTITY.IS_ENTITY_DEAD(h) && PED.IS_PED_IN_COMBAT(h, 0))
                            TASK.CLEAR_PED_TASKS(h, true, false);   // he's done fighting: so are they
                    Log.Detail("BRAWL your fight is over: " + _youJoiners.Count + " who joined leave off");
                    _youJoiners.Clear();
                    _youFightFoe = 0;
                }
                return;
            }
            _youFightLastAt = now;
            if (foe != _youFightFoe)
            {
                _youFightFoe = foe;
                _youJoiners.Clear();
            }
            if (_youJoiners.Count >= JoinIn.MostJoiners)
                return;
            try
            {
                JoinYouLook(me, foe, now);
            }
            catch (Exception ex)
            {
                _joinFailed = true;
                Log.Write("BRAWL joining your fight failed once, off for this session: " + ex.Message);
            }
        }

        /// The one the player is trading blows with now, within 4 m, or 0.
        private static int MeleeFoe(Ped me)
        {
            if (!PED.IS_PED_IN_MELEE_COMBAT(me.Handle))
                return 0;
            Vector3 at = me.Position;
            foreach (Ped p in World.GetAllPeds())
            {
                if (p == null || p.Handle == me.Handle || p.Position.DistanceTo(at) > 4f || p.IsDead || !PED.IS_PED_HUMAN(p.Handle))
                    continue;
                if (PED.IS_PED_IN_MELEE_COMBAT(p.Handle) && PED.IS_PED_IN_COMBAT(p.Handle, me.Handle))
                    return p.Handle;
            }
            return 0;
        }

        private void JoinYouLook(Ped me, int foe, int now)
        {
            Vector3 mid = me.Position;
            MindWho fw = MindKnown(foe, now);
            var sides = new[]
            {
                new List<Fighter> { new Fighter { Id = "player", Woman = false } },
                new List<Fighter> { new Fighter { Id = fw != null ? fw.Id : null, Woman = !PED.IS_PED_MALE(foe) } }
            };
            var looking = new List<Ped>();
            int witnesses = 0;
            bool law = false;
            foreach (Ped p in World.GetAllPeds())
            {
                if (p == null || p.Position.DistanceTo(mid) > JoinMetres + 18f)
                    continue;
                int h = p.Handle;
                if (h == me.Handle || h == foe || !p.Exists() || p.IsDead || !PED.IS_PED_HUMAN(h))
                    continue;
                if (Natives.IsLaw(p))
                {
                    if (p.Position.DistanceTo(mid) < 30f)
                        law = true;
                    continue;
                }
                if (p.Position.DistanceTo(mid) > JoinMetres || _youJoiners.Contains(h))
                    continue;
                witnesses++;
                if (Spawner.IsOurs(h) || Natives.StoryModel(p) || !IsGrownUp(p) || PED.IS_PED_ON_MOUNT(h) || PED.IS_PED_IN_ANY_VEHICLE(h, false) ||
                    PED.IS_PED_USING_ANY_SCENARIO(h) || PED.IS_PED_IN_COMBAT(h, 0) || PED.IS_PED_FLEEING(h) || _director.Involves(h) || MayDirectWhyNot(p) != null)
                    continue;
                looking.Add(p);
            }
            if (looking.Count == 0)
                return;
            // how each feels about the player: fond, against him or afraid (as the sights read it), as a tie of +2 or -2
            var regard = new Dictionary<string, int>();
            JoinTake best = null;
            Ped bestPed = null;
            string bestName = null;
            foreach (Ped p in looking)
            {
                MindWho w = MindKnown(p.Handle, now);
                if (w == null || w.Id == null || w.Src == null)
                    continue;
                string r = SightRegard(p, w.Id);
                regard[w.Id] = r == "fond" ? 2 : r == "hostile" ? -2 : 0;
                if (r == "afraid")
                    continue;   // whoever has seen what he does stays out of it
                Traits t = w.Traits ?? (w.Traits = _engine.Shadow.TraitsOf(w.Src));
                Func<string, string, int> warm = (x, y) => y == "player" ? (regard.ContainsKey(x) ? regard[x] : 0) : LadderWarmth(x, y);
                JoinTake take = JoinIn.Weigh(w.Id, t, !PED.IS_PED_MALE(p.Handle), InHisCups(p, w.P), sides, warm, witnesses, law, _placeKind == "saloon",
                                             _cfg.WomenInFights, _youJoiners.Count, "you|" + foe + "|" + _youJoiners.Count + "|" + w.Id);
                if (take.Side >= 0 && (best == null || take.Score > best.Score))
                {
                    best = take;
                    bestPed = p;
                    bestName = w.Name;
                }
            }
            if (best == null)
                return;
            int target = best.Side == 0 ? foe : me.Handle;   // side 0 is the player's: he goes for the other man; side 1 comes at the player
            int h2 = bestPed.Handle;
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h2, false);
            WEAPON.SET_CURRENT_PED_WEAPON(h2, Natives.Hash("WEAPON_UNARMED"), true, 0, false, false);
            PED.SET_PED_COMBAT_ATTRIBUTES(h2, 5, true);   // fists
            PED.SET_PED_COMBAT_ATTRIBUTES(h2, 46, true);
            TASK.TASK_COMBAT_PED(h2, target, 0, 16);
            _youJoiners.Add(h2);
            _youJoined++;
            _joinTotal++;
            string foeName = fw != null && fw.Name != null ? fw.Name : "the man you're fighting";
            Log.Write("BRAWL joins your fight: " + bestName + (best.Side == 0 ? " takes your side and goes for " + foeName : " takes " + foeName + "'s side and comes at you") +
                      " (" + best.Why + "; " + witnesses + " watching" + (_placeKind == "saloon" ? ", a saloon" : "") + ")");
        }
    }
}
