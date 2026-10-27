// Condolences in the street (overnight 2026-10-27, Tier 4, after "the widow's first day out"): someone who has had word
// of a friend's death in the last three days (a heard or seen `death` memory, Engine.GriefFor) meets a friend of their
// own, who offers sympathy; the grieving answer by their own nature: thanks, a wish to be let be, or a sharp word. Kind
// "comfort" (+1), once a day between the two. All lines are original to this add-on.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static partial class Scenes
    {
        /// A (the friend) offers B (the grieving) sympathy for `dead` (a first name, or null: "your loss"); B answers by
        /// manner (kind, cold, sharp); A's last word.
        public static Scene Condole(SceneCast c, string seed, string dead, string manner)
        {
            string a = N(c, 0, seed), b = N(c, 1, seed);
            Scene s = new Scene { Kind = "comfort", Summary = "a friend offered sympathy in the street" };
            s.Add(0, dead != null
                     ? Voice.Pick(seed + "co", "I was sorry to hear about " + dead + ", " + b + ". Truly sorry.", b + ". I heard about " + dead + ". I don't know what to say.",
                                  "Word came about " + dead + ", " + b + ". If I'd known sooner I'd have come by.")
                     : Voice.Pick(seed + "co", "I was sorry to hear of your loss, " + b + ".", b + ". I heard. I'm so sorry."), "sorry", "face");
            if (manner == "kind")
                s.Add(1, Voice.Pick(seed + "ck", "Thank you, " + a + ". It doesn't seem real yet.", "Thank you. I keep setting a place at the table.",
                                    "That's kind, " + a + ". Folks have been kind."), "sorry")
                 .Add(0, Voice.Pick(seed + "cl", "If there's anything at all, you only have to ask.", "I'll bring supper by tomorrow. No arguing."), "mild");
            else if (manner == "cold")
                s.Add(1, Voice.Pick(seed + "cc", "Thank you. I'd rather not speak of it, just now.", "Everybody's sorry, " + a + ". It doesn't change anything."), "mild")
                 .Add(0, Voice.Pick(seed + "cl2", "I understand. I'm here, is all.", "All right. All right."), "mild", "walkaway");
            else
                s.Add(1, Voice.Pick(seed + "cs", "Don't, " + a + ". Not here. I can't.", "Sorry doesn't bring anybody back."), "tut")
                 .Add(0, Voice.Pick(seed + "cl3", "...I'll leave you be.", "Forgive me. I'll go."), "mild", "walkaway");
            return s;
        }

        public static IEnumerable<Scene> CondoleSamples(SceneCast c)
        {
            for (int i = 0; i < 20; i++)
                foreach (string m in new[] { "kind", "cold", "sharp" })
                {
                    yield return Condole(c, "cd" + i, "Abe", m);
                    yield return Condole(c, "cd" + i, null, m);
                }
        }
    }
}
