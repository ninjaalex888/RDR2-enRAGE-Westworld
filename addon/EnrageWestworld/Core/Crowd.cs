// A bystander's word on a scene they're watching: one person nearby who has a name calls out once, from who they are
// to the two in it (a friend of one sides with them, an enemy of one takes the other side) and their own temper (the
// kind try to calm it, the cold want it gone, the sharp egg it on). A quarrel draws the most, a fight a cheer or a
// cry for help, a courtship a smile or a tut, bad news a murmur; small talk and gossip draw nothing. All lines are
// original to this add-on.
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public sealed class CrowdLine
    {
        public string Line;
        public string Voice;
    }

    public static class Crowd
    {
        private static readonly string[] SideWithB = { "Don't let {a} talk to you like that, {b}.", "{b}'s in the right of it and everybody knows it.", "Stand your ground, {b}. {a}'s all wind and no weather.", "Half this street heard what {a} said, {b}. Don't you take it." };
        private static readonly string[] SideWithA = { "Tell {b} straight, {a}. Somebody ought to.", "Go on, {a}. You've held your tongue long enough.", "Say your piece, {a}. {b}'s had this coming since spring.", "We're all behind you, {a}. {b} knows it, too." };
        private static readonly string[] Calm = { "Leave it, the pair of you. Not in the street.", "Come on, {a}. It ain't worth it.", "Leave it be, {b}. There's children watching from the porch.", "Come on, now. Shake hands and go get a drink, the both of you." };
        private static readonly string[] Cold = { "Take it somewhere else.", "Somebody fetch the sheriff before it gets out of hand.", "Some of us have work to get to.", "Shout at each other in your own yard, not mine." };
        private static readonly string[] Sharp = { "Settle it or shift, the pair of you. You're blocking the road.", "Louder, {a}. They can't hear you in Rhodes.", "Is this a quarrel or a quilting bee? Somebody throw a punch.", "Somebody sell tickets. {a} and {b} are putting on a show." };

        private static readonly string[] FightCheer = { "Hit him, {b}! Hit him!", "Two dollars on {a}!", "Mind his left, {a}! His left!", "Knock him into the trough, {b}!" };
        private static readonly string[] FightFear = { "Somebody stop them!", "Lord, somebody pull them apart!", "Fetch the sheriff! They'll kill each other!", "Stop it! Stop it, there's blood on the boards!" };

        private static readonly string[] WarmKind = { "About time, the pair of you.", "Well, look at that. Good for you, {a}.", "Somebody tell the preacher to brush his coat.", "Look at {a}, red as a ribbon. Bless the two of them." };
        private static readonly string[] WarmCold = { "In broad daylight. Lord.", "Some of us are trying to eat.", "Take it off the street. There's a church for that.", "My mother would have fetched a bucket of water." };

        private static readonly string[] SadKind = { "Poor soul.", "God keep you, {a}.", "Hard luck, {a}. We're all sorry for it.", "Let {a} be a while. Nobody needs to stare." };

        /// What kind of scene this is to the people watching: "quarrel", "fight", "warm", "sad", or null for one that
        /// draws no word (small talk, gossip, the paper, a song).
        public static string Mood(string kind)
        {
            switch (kind)
            {
                case "argue": case "snap": case "envy": case "debt": case "standoff": case "cheat": case "jealous": return "quarrel";
                case "brawl": case "rough": return "fight";
                case "drunk": return null;     // a man face down on the floor: the barkeep has his say, nobody takes a side
                case "avoid": return null;   // nobody makes a speech of a man crossing the street
                case "flirt": case "sweet": case "propose": case "makeup": case "cheer": return "warm";
                case "comfort": return "sad";
                default: return null;
            }
        }

        /// How often someone watching speaks up, by the scene's mood (percent).
        public static int Chance(string mood)
        {
            return mood == "quarrel" || mood == "fight" ? 60 : mood == "warm" ? 40 : mood == "sad" ? 30 : 0;
        }

        /// The bystander's line, or null if they'd say nothing. warmToA / warmToB: what's between them and A or B
        /// (above 0 friendly, below hostile, 0 nothing); manner: kind, cold or sharp (Exchanges.Manner).
        public static CrowdLine Word(string kind, int warmToA, int warmToB, string manner, string aName, string bName, bool twoMen, string seed)
        {
            string mood = Mood(kind);
            string[] pool = null;
            string voice = "mild";
            switch (mood)
            {
                case "quarrel":
                    // a friend of both tries to calm it, an enemy of both eggs it on; otherwise they take a side
                    if (warmToA > 0 && warmToB > 0) { pool = Calm; voice = "defuse"; }
                    else if (warmToA < 0 && warmToB < 0) { pool = Sharp; voice = "mock"; }
                    else if (warmToB > 0 || warmToA < 0) { pool = SideWithB; voice = "angry"; }
                    else if (warmToA > 0 || warmToB < 0) { pool = SideWithA; voice = "angry"; }
                    else if (manner == "kind") { pool = Calm; voice = "defuse"; }
                    else if (manner == "cold") { pool = Cold; voice = "tut"; }
                    else { pool = Sharp; voice = "mock"; }
                    break;
                case "fight":
                    pool = manner == "kind" ? FightFear : twoMen ? FightCheer : FightFear;
                    voice = manner == "kind" ? "scared" : "cheer";
                    break;
                case "warm":
                    if (manner == "kind" || warmToA > 0 || warmToB > 0) { pool = WarmKind; voice = "cheer"; }
                    else { pool = WarmCold; voice = "tut"; }
                    break;
                case "sad":
                    if (manner == "kind" || warmToA > 0) { pool = SadKind; voice = "sorry"; }
                    break;
            }
            if (pool == null)
                return null;
            return new CrowdLine { Line = Voice.Pick(seed + "crowd", pool).Replace("{a}", aName).Replace("{b}", bName), Voice = voice };
        }

        /// Every line, for the tests.
        public static IEnumerable<string> AllLines()
        {
            return SideWithB.Concat(SideWithA).Concat(Calm).Concat(Cold).Concat(Sharp).Concat(FightCheer).Concat(FightFear).Concat(WarmKind).Concat(WarmCold).Concat(SadKind);
        }
    }
}
