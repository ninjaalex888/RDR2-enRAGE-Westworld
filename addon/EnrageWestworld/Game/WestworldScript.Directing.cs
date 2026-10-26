// Who RealNPCs may give orders to (Core/Directing.cs), with the game's and enRAGE's facts about them: the one check
// every feature that tasks, casts or gestures someone asks (street scenes, reactions, real-voice talk, waking acts, the
// rough test). enRAGE's full records take part like anyone else (Lex, 2026-10-26) unless enRAGE is driving them right
// now; the F7 line lists the records near you and what stops any of them.
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private bool IsRecord(int h)
        {
            return _bridge != null && _bridge.RecordAt(h) != null;
        }

        /// Why we mayn't give this person orders now, or null if we may.
        private string MayDirectWhyNot(Ped p)
        {
            if (p == null || !p.Exists() || ENTITY.IS_ENTITY_DEAD(p.Handle))
                return "gone";
            int h = p.Handle;
            bool record = IsRecord(h);
            bool busy = PED.IS_PED_IN_COMBAT(h, 0) || PED.IS_PED_FLEEING(h) || PED.IS_PED_RAGDOLL(h) || PED.IS_PED_ON_MOUNT(h) || PED.IS_PED_IN_ANY_VEHICLE(h, false);
            return Directing.WhyNot(Natives.StoryModel(p), IsGrownUp(p), MISC.GET_MISSION_FLAG(), record, _cfg.EnragePeopleJoinIn,
                                    record && _bridge.EnrageFighter(h), record && _bridge.Interrogating, busy, Spawner.IsOurs(h), !record && CanDirect(p));
        }

        private bool MayDirect(Ped p)
        {
            return MayDirectWhyNot(p) == null;
        }

        /// F7: enRAGE's records within 30 m: whether enRAGE marks them as mission entities, the role it has them in, and
        /// whether RealNPCs may direct them (and why not).
        private string RecordsStatus()
        {
            if (_bridge == null)
                return "enRAGE not found";
            Ped me = Game.Player.Character;
            var near = World.GetAllPeds().Where(p => p != null && p.Exists() && p.Handle != me.Handle && IsRecord(p.Handle) && p.Position.DistanceTo(me.Position) < 30f)
                                         .OrderBy(p => p.Position.DistanceTo(me.Position)).Take(8).ToList();
            if (near.Count == 0)
                return (_cfg.EnragePeopleJoinIn ? "they join in" : "they're left alone ([Scenes] EnragePeopleJoinIn off)") + "; none within 30 m";
            return (_cfg.EnragePeopleJoinIn ? "they join in" : "they're left alone") + "; " + string.Join("; ", near.Select(p =>
            {
                string why = MayDirectWhyNot(p);
                return TalkName(p) + " (" + (int)p.Position.DistanceTo(me.Position) + " m, " + (ENTITY.IS_ENTITY_A_MISSION_ENTITY(p.Handle) ? "mission entity" : "not a mission entity") +
                       ", role " + (_bridge.RoleName(p.Handle) ?? "none") + "): " + (why == null ? "free" : why);
            }));
        }
    }
}
