// Whether to put back a bounty or wanted level that a fight in self-defence gave the player (SelfDefenceNoBounty).
// The game layer keeps the times and the bounty from before the fight; this decides, so it can be tested offline.
namespace EnrageWestworld.Core
{
    public enum DefenceAct
    {
        None,       // no fight with anyone who came at him (or it's over): nothing to do
        Mission,    // a story mission sets its own law: hands off
        Moved,      // he's far from where the fight began, or in another state: take today's bounty as the new start
        Tainted,    // he also hurt someone who hadn't come at him, or did another crime: the game's call stands
        Off,        // the setting is off
        Keep,       // nothing has risen
        Restore     // put the bounty back to what it was (and clear a new wanted level)
    }

    public static class DefenceLaw
    {
        public const int WindowMs = 90000;     // after the last hit or kill on someone who came at him
        public const int LookbackMs = 300000;  // an offence or crime this long before the fight still counts against it

        public static DefenceAct Decide(int now, int bounty, bool wanted, int since, int lastDefenceAt, int bountyBase, bool wantedBefore,
                                        int lastOffenceAt, int lastCrimeAt, bool mission, bool enabled, bool movedAway, out bool clearWanted)
        {
            clearWanted = false;
            if (mission)
                return DefenceAct.Mission;
            if (since < 0 || now - lastDefenceAt >= WindowMs)
                return DefenceAct.None;
            if (movedAway)
                return DefenceAct.Moved;
            if (lastOffenceAt >= since - LookbackMs || lastCrimeAt >= since - LookbackMs)
                return DefenceAct.Tainted;
            if (!enabled)
                return DefenceAct.Off;
            clearWanted = wanted && !wantedBefore;
            return bounty > bountyBase || clearWanted ? DefenceAct.Restore : DefenceAct.Keep;
        }
    }
}
