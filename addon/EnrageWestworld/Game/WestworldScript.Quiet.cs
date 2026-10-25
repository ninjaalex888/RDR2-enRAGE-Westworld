// Quiet while you stand still or watch through the cinematic camera ([Quiet] in the ini): RealNPCs asks enRAGE for
// its own calm period, a few seconds at a time, so enRAGE's tension stops building and no nemesis, gang or faction
// is sent after you. Fights already going on carry on. The calm is asked for in short pieces, so it runs out soon
// after you move on (enRAGE can't be told to end it early). Since enRAGE 0.9.5.3 its calm also holds back its
// planned peaceful events, so while a funeral or someone's habit (an evening at the saloon) is due near you, no calm
// is asked for and it goes ahead. Its gang jobs, and anything else, are still held.
using System;
using System.Linq;
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
        private string _quietWhy, _quietLetBy, _planDue;
        private int _nextPlanLook;
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
            // the cinematic view you turn on, or any other cinematic shot the game is showing (the log says which)
            bool mode = _cfg.QuietInCinematic && CAM._IS_IN_CINEMATIC_MODE();
            bool shot = _cfg.QuietInCinematic && !mode && CAM.IS_CINEMATIC_CAM_RENDERING();
            bool still = _cfg.QuietWhenStill && now - _quietStillSince >= _cfg.QuietStillSeconds * 1000;
            string why = mode ? "the cinematic camera is on" : shot ? "the cinematic camera is on (a cinematic shot)" : still ? "you're standing still" : null;
            if (why == null)
            {
                EndQuiet();
                return;
            }
            if (!_bridge.CanHoldEnrage)
                return;
            string due = PeacefulPlanDue(pos, now);
            if (due != _quietLetBy)
            {
                Log.Write(due != null ? "QUIET: not asked for while enRAGE's " + due + " is due here, so it can go ahead" : "QUIET: enRAGE's " + _quietLetBy + " is over or gone; the quiet can hold again");
                _quietLetBy = due;
            }
            if (due != null)
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

        /// A funeral or habit of enRAGE's due or going on within 250 m (null if none), looked at every 2 seconds.
        private string PeacefulPlanDue(Vector3 pos, int now)
        {
            if (now < _nextPlanLook)
                return _planDue;
            _nextPlanLook = now + 2000;
            long g = Bridge.Now();
            PlanInfo p = _bridge.Plans().FirstOrDefault(x => (x.Kind == "funeral" || x.Kind == "habit") && !x.Done && g >= x.DueAt - 5 && g <= x.DueAt + Math.Max(60, x.WindowMin) &&
                                                             x.Pos.DistanceTo2D(pos) < 250f);
            _planDue = p == null ? null : (p.Kind == "funeral" ? "funeral" : "planned event") + (string.IsNullOrEmpty(p.Title) ? "" : " (" + p.Title + ")");
            return _planDue;
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
