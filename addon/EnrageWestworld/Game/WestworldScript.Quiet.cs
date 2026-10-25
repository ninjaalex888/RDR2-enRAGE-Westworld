// Quiet while you stand still or watch through the cinematic camera ([Quiet] in the ini): RealNPCs asks enRAGE for
// its own calm period, a few seconds at a time, so enRAGE's tension stops building and no nemesis, gang or faction
// is sent after you. Fights already going on carry on. The calm is asked for in short pieces, so it runs out soon
// after you move on (enRAGE can't be told to end it early).
using System;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private const int QuietPieceSeconds = 10;     // asked for each time...
        private const int QuietRenewMs = 7000;        // ...and asked again this often while it should last
        private const uint QuietMoveLR = 1301263553u, QuietMoveUD = 4255658384u, QuietHorseLR = 308778731u, QuietHorseUD = 1002303471u;

        private Vector3 _quietAt;
        private int _quietStillSince, _quietRenewAt, _nextQuietCheck;
        private string _quietWhy;
        private bool _quietWarned;
        private Action _aQuiet;

        private void QuietStep(int now)
        {
            if (_aQuiet == null)
                _aQuiet = () => QuietTick(_now);
            Safe("enrage quiet", _aQuiet);
        }

        private void QuietTick(int now)
        {
            if (now < _nextQuietCheck)
                return;
            _nextQuietCheck = now + 250;
            if (_bridge == null || (!_cfg.QuietWhenStill && !_cfg.QuietInCinematic))
            {
                EndQuiet();
                return;
            }
            Ped me = Game.Player.Character;
            if (me == null || !me.Exists() || me.IsDead)
                return;
            Vector3 pos = me.Position;
            float move = Math.Abs(PAD.GET_CONTROL_NORMAL(0, QuietMoveLR)) + Math.Abs(PAD.GET_CONTROL_NORMAL(0, QuietMoveUD)) +
                         Math.Abs(PAD.GET_CONTROL_NORMAL(0, QuietHorseLR)) + Math.Abs(PAD.GET_CONTROL_NORMAL(0, QuietHorseUD));
            if (_quietStillSince == 0 || pos.DistanceTo(_quietAt) > 0.4f || move > 0.15f)
            {
                _quietStillSince = now;   // moving (on foot or on horseback); turning the camera doesn't count
                _quietAt = pos;
            }
            bool cinematic = _cfg.QuietInCinematic && (CAM._IS_IN_CINEMATIC_MODE() || CAM.IS_CINEMATIC_CAM_RENDERING());
            bool still = _cfg.QuietWhenStill && now - _quietStillSince >= _cfg.QuietStillSeconds * 1000;
            string why = cinematic ? "the cinematic camera is on" : still ? "you're standing still" : null;
            if (why == null)
            {
                EndQuiet();
                return;
            }
            if (!_bridge.CanHoldEnrage)
                return;
            if (why != _quietWhy)
            {
                _quietRenewAt = 0;
                Log.Write("QUIET: " + why + ": enRAGE's tension and ambushes are held" + (_quietWhy != null ? "" : " until you move on"));
                if (!_bridge.EnrageCalmOn && !_quietWarned)
                {
                    _quietWarned = true;
                    Log.Write("QUIET: enRAGE's own calm is switched off ([Calm] Enabled = False in NemesisMod.ini), so it ignores this; turn it on for the quiet to work.");
                }
                _quietWhy = why;
            }
            if (now >= _quietRenewAt)
            {
                _quietRenewAt = now + QuietRenewMs;
                _bridge.HoldEnrage(QuietPieceSeconds, "RealNPCs: " + why);
            }
        }

        private void EndQuiet()
        {
            if (_quietWhy == null)
                return;
            Log.Write("QUIET: over (" + _quietWhy.Replace("you're ", "no longer ").Replace("the cinematic camera is on", "the cinematic camera is off") +
                      "); enRAGE carries on within " + QuietPieceSeconds + " seconds");
            _quietWhy = null;
        }
    }
}
