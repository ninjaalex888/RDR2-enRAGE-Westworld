// Sweethearts fall out, and make it up in public (overnight 2026-10-27, Tier 4): now and then two sweethearts who meet
// again have words (one of them hot-tempered or having a sore day: kept waiting on a Sunday, overlooked at the dance,
// the last to hear of a move to the city). The tie keeps that they were courting (SceneTie.Courted), so the next time
// they meet the cooler head comes with a small gift and says sorry where the street can see it; the other takes it or
// doesn't by their own nature (kind always, cold half the time, sharp a third). Taken: they made up (+2) and are
// sweethearts again; turned down: a brushoff, and it can be tried again another day. All lines are original.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static partial class Scenes
    {
        public const int TiffChance = 22;   // sweethearts past their first flirt with a hot head or a sore day between them: per meeting
        public const int MendChance = 70;   // sweethearts at odds: one comes to make it up, per meeting

        /// Sweethearts have words this time: past their first flirt (stage 1 on), one of them hot or sore, on the roll.
        public static bool TiffNow(bool hotOrSore, int stage, string seed)
        {
            return stage >= 1 && hotOrSore && Voice.Hash(seed + "|tiff") % 100 < TiffChance;
        }

        /// The other takes the gift: the kind always, the cold half the time, the sharp a third of the time.
        public static bool MendTaken(string manner, string seed)
        {
            int chance = manner == "kind" ? 100 : manner == "cold" ? 50 : 35;
            return Voice.Hash(seed + "|taken") % 100 < chance;
        }

        /// A falls out with B (A has the grievance); B walks off. Kind argue, so it counts as a quarrel.
        public static Scene Tiff(SceneCast c, string seed)
        {
            string a = N(c, 0, seed), b = N(c, 1, seed);
            Scene s = new Scene { Kind = "argue", Summary = "sweethearts had words in the street" };
            switch (Voice.Hash(seed + "|tt") % 4)
            {
                case 0:
                    s.Add(0, "I waited all Sunday afternoon, " + b + ". You never came.", "angry", "face")
                     .Add(1, "Something came up. I told you after.", "mild")
                     .Add(0, "Something always comes up with you.", "angry")
                     .Add(1, "If that's what you think of me, " + a + ", then go on thinking it.", "storm", "walkaway");
                    break;
                case 1:
                    s.Add(0, "You never looked my way once at the dance, " + b + ".", "angry", "face")
                     .Add(1, "I was dancing, " + a + ". That's what a dance is for.", "mild")
                     .Add(0, "With every soul in the county but me.", "angry")
                     .Add(1, "Then dance by yourself next time. I'm going home.", "storm", "walkaway");
                    break;
                case 2:
                    s.Add(0, "You told half the town our business, " + b + ".", "angry", "face")
                     .Add(1, "I told one person. One.", "mild")
                     .Add(0, "In a town this size one loose tongue does for the lot.", "angry")
                     .Add(1, "Then find somebody quieter to court, " + a + ".", "storm", "walkaway");
                    break;
                default:
                    s.Add(0, "Folks say you're thinking of leaving for the city, " + b + ".", "angry", "face")
                     .Add(1, "Folks say a deal of things. I might. I don't know yet.", "mild")
                     .Add(0, "And when were you going to tell me?", "angry")
                     .Add(1, "When I knew, " + a + ". Don't you shout at me in the street.", "storm", "walkaway");
                    break;
            }
            return s;
        }

        private static readonly string[] GiftsForHer =
        {
            "Brought you something. Peppermint sticks, the striped kind.",
            "I picked these down by the creek. Don't laugh.",
            "A ribbon. Blue. You said once you liked blue.",
            "It's a comb. Tortoiseshell, the man said.",
            "Lemon drops. You never let me buy you anything, so hush.",
            "A bit of lace. The woman at the store swore it was French."
        };

        private static readonly string[] GiftsForHim =
        {
            "Brought you a pie. Peach. Mind, it's still warm.",
            "Tobacco. The good kind, in a tin.",
            "Knitted you a scarf. It's crooked, but it's warm.",
            "A new pocketknife. Yours was a disgrace.",
            "Cigars. Two of them. Don't smoke them both at once.",
            "Darned your socks. Every pair. A word of thanks wouldn't kill you."
        };

        /// A comes to B with a gift and says sorry; B takes it (kind makeup) or doesn't (brushoff).
        public static Scene Mend(SceneCast c, string seed, bool taken)
        {
            string a = N(c, 0, seed), b = N(c, 1, seed);
            Scene s = new Scene { Kind = taken ? "makeup" : "brushoff", Summary = taken ? "sweethearts made it up in the street" : "a sweetheart's peace offering was turned down" };
            s.Add(0, Voice.Pick(seed + "mg", c.BWoman ? GiftsForHer : GiftsForHim), "flatter", "face")
             .Add(1, Voice.Pick(seed + "m1", "Oh. It's you, " + a + ".", "I didn't think you'd come, " + a + ".", "You've a cheek, " + a + ", showing your face to me.", "What's that, " + a + "?", "Well, look who's come crawling, " + a + "."), "mild")
             .Add(0, Voice.Pick(seed + "m2", "I was wrong, " + b + ". I've thought of nothing else since.", "I said things I didn't mean, " + b + ". I'm sorry for them.",
                                 "I'm sorry, " + b + ". There. I said it where folks can hear.", "I'm no hand at this, " + b + ". But I'm sorry."), "sorry");
            if (taken)
                s.Add(1, Voice.Pick(seed + "m3", "...You fool. Come here.", "All right, " + a + ". All right.", "It's lovely. And so are you, when you've the sense to say sorry.",
                                    "Well. I suppose I'll keep it. And you, " + a + ".", "Oh, " + a + ". You'd try a saint."), "cheer")
                 .Add(0, Voice.Pick(seed + "m4", "I'll mind my tongue from now on.", "Walk out with me Sunday? Say you will.", "I'll not be such a fool again. Not soon, anyhow."), "flatter");
            else
                s.Add(1, Voice.Pick(seed + "m3", "Keep it. It isn't a present I wanted.", "You can't buy your way back, " + a + ".", "Not here. Not now. Maybe not ever.", "Give it to somebody who's waiting, " + a + ". I'm not."), "storm", "walkaway")
                 .Add(0, Voice.Pick(seed + "m4", "...Well. I tried.", "...I'll leave it at your door, then."), "mild");
            return s;
        }

        public static IEnumerable<Scene> LoversSamples(SceneCast c)
        {
            for (int i = 0; i < 24; i++)
            {
                yield return Tiff(c, "lt" + i);
                yield return Mend(c, "lm" + i, true);
                yield return Mend(c, "lm" + i, false);
            }
        }
    }
}
