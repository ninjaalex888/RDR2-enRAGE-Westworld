// Onlookers who join a fistfight going on near the player, on a side, from their minds (Core/JoinIn.cs): a friend of
// someone getting beaten, someone with bad blood against a fighter, a drunk hothead in a saloon. A single joiner each look, every few
// seconds, so a fight spreads the way it would: the friend of the man just set upon may be next (the NPC-life run, Lex
// 2026-10-30: "it can break out between any 2+ NPCs if their inner thoughts lead to it"). Behind [Mind] MindLive and
// MindJoinFights.
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
        private const int JoinEveryMs = 2500;
        private const float JoinMetres = 12f;
        private int _joinNextAt, _joinTotal;
        private bool _joinFailed;
        private string _joinLast;

        private void JoinStep(int now)
        {
            if (!_cfg.MindLive || !_cfg.MindJoinFights || _joinFailed || now < _joinNextAt)
                return;
            _joinNextAt = now + JoinEveryMs;
            List<int> a, b;
            if (!_director.FightSides(out a, out b) || _director.JoinerCount >= JoinIn.MostJoiners)
                return;
            try
            {
                JoinLook(a, b, now);
            }
            catch (Exception ex)
            {
                _joinFailed = true;
                Log.Write("BRAWL joining failed once, off for this session: " + ex.Message);
            }
        }

        private void JoinLook(List<int> a, List<int> b, int now)
        {
            Ped me = Game.Player.Character;
            if (!ENTITY.DOES_ENTITY_EXIST(a[0]))
                return;
            Vector3 mid = ENTITY.GET_ENTITY_COORDS(a[0], true, true);
            var sides = new[] { a.Select(FighterOf).ToList(), b.Select(FighterOf).ToList() };
            var looking = new List<Ped>();
            int witnesses = 0;
            bool law = false;
            foreach (Ped p in World.GetAllPeds())
            {
                if (p == null || p.Position.DistanceTo(mid) > JoinMetres + 18f)
                    continue;   // the distance first
                int h = p.Handle;
                if (h == me.Handle || !p.Exists() || p.IsDead || !PED.IS_PED_HUMAN(h))
                    continue;
                if (Natives.IsLaw(p))
                {
                    if (p.Position.DistanceTo(mid) < 30f)
                        law = true;
                    continue;
                }
                if (p.Position.DistanceTo(mid) > JoinMetres || _director.Involves(h))
                    continue;
                witnesses++;
                if (Spawner.IsOurs(h) || Natives.StoryModel(p) || !IsGrownUp(p) || PED.IS_PED_ON_MOUNT(h) || PED.IS_PED_IN_ANY_VEHICLE(h, false) ||
                    PED.IS_PED_USING_ANY_SCENARIO(h) || PED.IS_PED_IN_COMBAT(h, 0) || PED.IS_PED_FLEEING(h) || MayDirectWhyNot(p) != null)
                    continue;   // the sat and the leaning stay put (pulled off a bar they went into a T pose)
                looking.Add(p);
            }
            if (looking.Count == 0)
                return;
            string fight = a[0] + "|" + b[0] + "|" + (a.Count + b.Count);
            JoinTake best = null;
            Ped bestPed = null;
            string bestName = null;
            foreach (Ped p in looking)
            {
                MindWho w = MindKnown(p.Handle, now);
                if (w == null || w.Id == null || w.Src == null)
                    continue;   // the mind decides who joins: nobody it doesn't know
                Traits t = w.Traits ?? (w.Traits = _engine.Shadow.TraitsOf(w.Src));
                JoinTake take = JoinIn.Weigh(w.Id, t, !PED.IS_PED_MALE(p.Handle), InHisCups(p, w.P), sides, LadderWarmth, witnesses, law, _placeKind == "saloon",
                                             _cfg.WomenInFights, _director.JoinerCount, fight + "|" + w.Id);
                if (take.Side >= 0 && (best == null || take.Score > best.Score))
                {
                    best = take;
                    bestPed = p;
                    bestName = w.Name;
                }
                else if (take.Why != null && take.Score > 0.2f)
                    Log.Detail("BRAWL " + w.Name + " stays out: " + take.Why);
            }
            if (best == null)
                return;
            List<int> theirs = best.Side == 0 ? b : a;
            int target = theirs[Math.Max(0, Math.Min(theirs.Count - 1, best.Target))];
            if (!_director.AddFighter(bestPed.Handle, best.Side, target))
                return;
            _joinTotal++;
            string with = NameOfFighter(best.Side == 0 ? a[0] : b[0]), at = NameOfFighter(target);
            _joinLast = bestName + " for " + with + ", at " + at;
            Log.Write("BRAWL joins: " + bestName + " takes " + with + "'s side and goes for " + at + " (" + best.Why + "; " + witnesses + " watching" + (_placeKind == "saloon" ? ", a saloon" : "") + ")");
            Safe("join tie", () => NoteSceneTie("brawl", new List<Ped> { bestPed, new Ped(target) }));
        }

        private Fighter FighterOf(int h)
        {
            MindWho w = ENTITY.DOES_ENTITY_EXIST(h) ? MindKnown(h, Game.GameTime) : null;
            return new Fighter { Id = w != null ? w.Id : null, Woman = ENTITY.DOES_ENTITY_EXIST(h) && !PED.IS_PED_MALE(h) };
        }

        private string NameOfFighter(int h)
        {
            MindWho w = ENTITY.DOES_ENTITY_EXIST(h) ? MindKnown(h, Game.GameTime) : null;
            return w != null && w.Name != null ? w.Name : "a stranger";
        }

        /// F7: who joined fights this session.
        private string JoinStatus()
        {
            if (!_cfg.MindLive || !_cfg.MindJoinFights)
                return "off ([Mind] MindJoinFights, with MindLive)";
            return _joinFailed ? "FAILED once this session (see the log)" : _joinTotal + " joined a fight this session" + (_joinLast != null ? " (last: " + _joinLast + ")" : "");
        }
    }
}
