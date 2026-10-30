// The law's search fades while it hasn't found you (Lex, 2026-10-30: "if the law hasn't found the main character, the
// wanted search can start dropping. Currently, the wanted status doesn't start dropping until the main character leaves
// the red area"). [World] LawSearchFades: while he is wanted outside a story mission and no lawman has had sight of
// him for LawSearchFadeSeconds (none within 40 m with a clear line to him, none fighting him), the game's wanted score is
// let down a fifth every two seconds, and after as long again unseen the search is called off. The bounty is the game's
// and stays. Whether the game takes the score down this way is not yet seen: each step is logged (LAW search) with the
// score before and after, so the first test says.
using System;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private int _lawUnseenSince = -1, _lawFadeNext, _lawFadeSteps, _lawSeenCheckAt;
        private bool _lawFadeNoted;

        /// Every scan (400 ms) from the defence scan, with the shown wanted state; the sight check once a second.
        private void LawFadeStep(int now, bool wanted)
        {
            int me = Game.Player.Handle;
            if (!_cfg.LawSearchFades || !wanted || MISC.GET_MISSION_FLAG())
            {
                if (_lawUnseenSince >= 0 && _lawFadeSteps > 0 && !wanted)
                    Log.Write("LAW search: over after " + (now - _lawUnseenSince) / 1000 + " s unseen (" + _lawFadeSteps + " steps down)");
                _lawUnseenSince = -1;
                _lawFadeSteps = 0;
                _lawFadeNoted = false;
                return;
            }
            if (now - _lawSeenCheckAt >= 1000)
            {
                _lawSeenCheckAt = now;
                if (LawSeesYou())
                {
                    if (_lawFadeSteps > 0)
                        Log.Write("LAW search: a lawman has you in sight again; the search holds");
                    _lawUnseenSince = now;
                    _lawFadeSteps = 0;
                    _lawFadeNoted = false;
                    return;
                }
                if (_lawUnseenSince < 0)
                    _lawUnseenSince = now;
            }
            if (_lawUnseenSince < 0)
                return;
            int unseen = now - _lawUnseenSince, after = (int)(_cfg.LawSearchFadeSeconds * 1000f);
            if (unseen < after || now < _lawFadeNext)
                return;
            _lawFadeNext = now + 2000;
            if (unseen >= after * 2)
            {
                PLAYER.CLEAR_PLAYER_WANTED_LEVEL(me);
                LAW.CLEAR_WANTED_SCORE(me);
                _lawFadeSteps++;
                Log.Write("LAW search: called off after " + unseen / 1000 + " s unseen");
                return;
            }
            int score = LAW.GET_WANTED_SCORE(me);
            int down = Math.Max(0, score - Math.Max(1, score / 5));
            LAW.SET_WANTED_SCORE(me, down);
            _lawFadeSteps++;
            if (!_lawFadeNoted)
            {
                _lawFadeNoted = true;
                Log.Write("LAW search: unseen for " + unseen / 1000 + " s, the search fades: score " + score + " -> " + down + ", reads " + LAW.GET_WANTED_SCORE(me));
            }
        }

        /// A lawman within 40 m with a clear line to him, or any lawman within 120 m fighting him.
        private bool LawSeesYou()
        {
            Ped my = Game.Player.Character;
            Vector3 at = my.Position;
            foreach (Ped p in World.GetAllPeds())
            {
                if (p == null || p.Handle == my.Handle)
                    continue;
                float d = p.Position.DistanceTo(at);
                if (d > 120f || !p.Exists() || p.IsDead || !Natives.IsLaw(p))
                    continue;   // the distance first: a far body needs no more natives
                if (PED.IS_PED_IN_COMBAT(p.Handle, my.Handle))
                    return true;
                if (d <= 40f && ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(p.Handle, my.Handle, 17))
                    return true;
            }
            return false;
        }
    }
}
