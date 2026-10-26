// Whether the game's lock-on menu offers "Flirt" for the person the player is locked on to: a living grown-up who
// isn't the player, close enough to talk (8 m, as the flirt itself asks), not fighting, never one of the story's own
// people, nobody a mission holds unless enRAGE knows them, and never during a story mission.
namespace EnrageWestworld.Core
{
    public static class FlirtPromptRule
    {
        public const float Reach = 8f;

        public static bool Offer(bool settingOn, bool romanceOn, bool human, bool player, bool alive, bool grownUp, bool storyModel,
                                 bool missionEntity, bool enrageKnows, float distance, bool inCombat, bool inMission)
        {
            return settingOn && romanceOn && human && !player && alive && grownUp && !storyModel && (!missionEntity || enrageKnows) &&
                   distance <= Reach && !inCombat && !inMission;
        }
    }
}
