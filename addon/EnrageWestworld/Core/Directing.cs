// Who RealNPCs may give orders to (one driver at a time), as one rule: never one of the story's own people, a child,
// or anyone during a story mission; one of enRAGE's full records whenever enRAGE isn't driving them right now (Lex,
// 2026-10-26: "relax all rules relating to using enrage full records, everything from the thoughts to gestures"),
// and only while [Scenes] EnragePeopleJoinIn is on; anyone else when they're free (CanDirect: not a mission's or
// another script's, not held by one of our features, not fighting, fleeing, riding or swimming).
namespace EnrageWestworld.Core
{
    public static class Directing
    {
        /// Why they mayn't be directed now, or null if they may.
        public static string WhyNot(bool storyModel, bool grownUp, bool inMission, bool record, bool recordsJoinIn, bool fighterRole,
                                    bool interrogating, bool busy, bool ours, bool canDirect)
        {
            if (inMission)
                return "during a story mission";
            if (storyModel)
                return "one of the story's own people";
            if (!grownUp)
                return "not a grown-up";
            if (record)
            {
                if (!recordsJoinIn)
                    return "one of enRAGE's people ([Scenes] EnragePeopleJoinIn is off)";
                if (fighterRole)
                    return "enRAGE has them in a fight or a job";
                if (interrogating)
                    return "enRAGE is questioning someone";
                if (busy)
                    return "busy (fighting, fleeing, riding)";
                if (ours)
                    return "held by another of RealNPCs' features";
                return null;   // enRAGE may mark its people as mission entities: for them that flag doesn't stop us
            }
            return canDirect ? null : "can't be directed now";
        }
    }
}
