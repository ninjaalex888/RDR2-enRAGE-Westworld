// A stranded traveller asking the way (overnight 2026-10-27, Tier 2.9): by day on a country road, a man on
// foot waves the player down and asks the way to the nearest town. A greeting (or stopping with him a moment) is an
// answer: he works it out, points the way, thanks him and walks off that way. Antagonized, he backs off. Ridden past,
// he mutters and goes on. The game side does the spawning, the looking and the walking; this has the towns and the
// words. All lines are original to this add-on.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class Travellers
    {
        /// The towns a traveller asks for, at about the middle of each on the map (x, y): near enough to point the way.
        public static readonly Dictionary<string, float[]> Towns = new Dictionary<string, float[]>
        {
            { "Valentine", new[] { -280f, 790f } }, { "Rhodes", new[] { 1300f, -1300f } }, { "Saint Denis", new[] { 2620f, -1250f } },
            { "Strawberry", new[] { -1800f, -400f } }, { "Blackwater", new[] { -800f, -1300f } }, { "Annesburg", new[] { 2930f, 1330f } },
            { "Armadillo", new[] { -3660f, -2600f } }, { "Van Horn", new[] { 2970f, 570f } }, { "Emerald Ranch", new[] { 1420f, 330f } },
        };

        /// The nearest town to a point, and how far.
        public static string Nearest(float x, float y, out float metres, out float tx, out float ty)
        {
            string best = null;
            metres = float.MaxValue;
            tx = ty = 0f;
            foreach (KeyValuePair<string, float[]> t in Towns)
            {
                float dx = t.Value[0] - x, dy = t.Value[1] - y, d = (float)Math.Sqrt(dx * dx + dy * dy);
                if (d < metres)
                {
                    metres = d;
                    best = t.Key;
                    tx = t.Value[0];
                    ty = t.Value[1];
                }
            }
            return best;
        }

        public static bool Hour(int hour)
        {
            return hour >= 7 && hour < 19;
        }

        public static string Hail(string seed)
        {
            return Voice.Pick(seed + "th", "Mister! Hold up a moment!", "Say there! You, on the horse!", "Excuse me! Sir!");
        }

        public static string Ask(string town, string seed)
        {
            return Voice.Pick(seed + "ta", "Which way's " + town + "? Been walking since sunup and I'm turned clean around.",
                "Is this the road to " + town + "? Every fork looks the same out here.", "You know " + town + "? Lost my horse two days back and my sense of direction with it.");
        }

        /// He works it out from what the player says (we don't hear it), points and thanks him.
        public static string Thanks(string town, string seed)
        {
            return Voice.Pick(seed + "tt", "That way? Obliged, mister. I'd have walked to the coast.", "Past the ridge, then on to " + town + ". Thank you kindly.", "Right. That way. Bless you.");
        }

        public static string Refused(string seed)
        {
            return Voice.Pick(seed + "tr", "...Alright. Alright, I'm going.", "No call for that. I only asked.", "Fine. I'll find it myself.");
        }

        public static string Ignored(string seed)
        {
            return Voice.Pick(seed + "ti", "Thank you for nothing.", "Nobody stops out here.", "Suppose I'll follow the wheel ruts.");
        }

        // snakebit (Tier 4): about one in three sits by the road holding his leg and asks for help instead of the way;
        // stopping for him (a greeting, or standing by him) is seeing to the bite, and the town hears of it later
        public const int BittenChance = 30;

        public static string BittenHail(string seed)
        {
            return Voice.Pick(seed + "bh", "Mister! Over here! Snake got me!", "Help! Sir! I've been bit!", "You there! For the love of God, stop!");
        }

        public static string BittenAsk(string seed)
        {
            return Voice.Pick(seed + "ba", "Rattler. Got me above the boot. Leg won't hold me.", "Snake in the ditch grass. Leg's swelling like a melon.",
                              "Bit on the ankle. You got a knife? Whiskey? Anything?");
        }

        public static string BittenThanks(string town, string seed)
        {
            return Voice.Pick(seed + "bt", "Lord. Thank you. I thought I'd die out here.", "Tight as that? Good. I can make " + town + " on it, slow.",
                              "You're a Christian, mister, whatever else you are.");
        }

        public static string BittenRefused(string seed)
        {
            return Voice.Pick(seed + "br", "Leave me be, then! I'm snakebit, for God's sake!", "Go on. Go on and leave me.");
        }

        public static string BittenIgnored(string seed, bool mounted)
        {
            return Voice.Pick(seed + "bi", mounted ? "Rode right by. Rode right by me." : "Walked right by. Walked right by me.", "I'll crawl, then. Crawl to town.", "Leg's hot as a stove. Somebody else will come. Somebody.");
        }

        /// The town's word, some hours on, of the snakebit man the player stopped for.
        public static string BittenNews(string town, string seed, bool mounted)
        {
            return Voice.Pick(seed + "bn", "A snakebit man limped into " + town + " off the road. Says a stranger" + (mounted ? " on a horse" : "") + " stopped and saw to it.",
                              "Fellow came into " + town + " with a rattler bite bound tight. Says a " + (mounted ? "rider" : "stranger") + " stopped when nobody else would.");
        }

        // the one who never arrived (Tier 4, toward the supper invitation's NPC version): a lost traveller you rode past or ran
        // off, now and then, never reaches the town, and word goes round
        public const int MissingChance = 15;

        public static bool GoesMissing(string seed)
        {
            return Voice.Hash(seed + "|missing") % 100 < MissingChance;
        }

        public static string Missing(string town, string seed)
        {
            return Voice.Pick(seed + "tm", "A man walking to " + town + " never got there. They found his hat in a ditch, and that's all.",
                              "Nobody's seen the fellow who was asking the way to " + town + ". His bundle turned up by a farm gate.",
                              "Folk in " + town + " are talking of a stranger on foot who never arrived. Third this year, some say.");
        }

        /// Ridden past or run off, a snakebit man doesn't always make it: half the time (by the seed) the town hears so.
        public const int BittenDiesChance = 50;

        public static bool BittenDies(string seed)
        {
            return Voice.Hash(seed + "|dies") % 100 < BittenDiesChance;
        }

        public static string BittenDied(string town, string seed)
        {
            return Voice.Pick(seed + "bd", "They found a man dead near " + town + ", snakebit. He'd sat by the road a good while, by the look of it.",
                              "Stranger found dead in the ditch on the " + town + " road. Rattler bite. Folk wonder who passed him by.");
        }

        // a robbery on the road (Tier 4, the NPC version): about one in five is being held up by two of the region's outlaws
        // when you come on him; they take his money and go, unless you step in
        public const int RobbedChance = 20;

        public static string RobThreat(string seed)
        {
            return Voice.Pick(seed + "rt", "Purse. Slow, now.", "Turn out your pockets. All of them.", "Boots too, if there's money in 'em.", "Watch and chain. Don't make me ask twice.");
        }

        public static string RobPlead(string town, string seed)
        {
            return Voice.Pick(seed + "rp", "Please. It's all I've got.", "That's a month's wages. Please.", "I've a wife waiting in " + town + ". Please.", "Take it. Just don't shoot.");
        }

        public static string RobWarn(bool mounted, string seed)
        {
            return Voice.Pick(seed + "rw", mounted ? "Keep riding, mister. This don't concern you." : "Keep walking, mister. This don't concern you.", "Look the other way and keep going.");
        }

        public static string RobHandOver(string seed)
        {
            return Voice.Pick(seed + "rh", "Here. Here, take it.", "That's everything. I swear that's everything.");
        }

        public static string RobDone(string seed)
        {
            return Voice.Pick(seed + "rd", "Much obliged for the donation.", "Obliged. Don't follow us.", "Count to a hundred before you move.");
        }

        public static string RobThought(string seed)
        {
            return Voice.Pick(seed + "rx", "Not the watch. Please, not Pa's watch.", "Hands up. Keep them up. Don't look at their faces.", "Somebody ride by. Somebody.");
        }

        /// To the player, after they've gone with his money.
        public static string RobbedAfter(string seed)
        {
            return Voice.Pick(seed + "ra", "They took every cent. Every last cent.", "Took my money and my watch. You saw it.", "Well. I'm walking to town with empty pockets.");
        }

        /// To the player, once he's driven them off.
        public static string RobSaved(string seed)
        {
            return Voice.Pick(seed + "rs", "Lord. Thank you. I thought they'd shoot me after.", "You're a brave fool, mister. Thank you.", "That took sand, mister. Thank you.");
        }

        /// The town's word, some hours on.
        public static string RobNews(string town, bool saved, string seed)
        {
            return saved ? Voice.Pick(seed + "rn", "Word is a stranger drove off the road agents who had a man at gunpoint near " + town + ".", "Road agents tried a man outside " + town + ". Somebody came along and made them sorry.")
                         : Voice.Pick(seed + "rn", "Road agents held up a man on foot outside " + town + " and took all he had.", "Robbery on the " + town + " road in broad day. They took a poor man's wages.");
        }

        public static IEnumerable<string> Lines()
        {
            for (int i = 0; i < 9; i++)
            {
                string town = Towns.Keys.ElementAt(i % Towns.Count);
                yield return Hail("h" + i);
                yield return Ask(town, "a" + i);
                yield return Thanks(town, "t" + i);
                yield return Refused("r" + i);
                yield return Ignored("i" + i);
                yield return BittenHail("h" + i);
                yield return BittenAsk("a" + i);
                yield return BittenThanks(town, "t" + i);
                yield return BittenRefused("r" + i);
                yield return BittenIgnored("i" + i, i % 2 == 0);
                yield return BittenNews(town, "n" + i, i % 2 == 0);
                yield return BittenDied(town, "d" + i);
                yield return Missing(town, "m" + i);
                yield return RobThreat("rt" + i);
                yield return RobPlead(town, "rp" + i);
                yield return RobWarn(i % 2 == 0, "rw" + i);
                yield return RobHandOver("rh" + i);
                yield return RobDone("rd" + i);
                yield return RobThought("rx" + i);
                yield return RobbedAfter("ra" + i);
                yield return RobSaved("rs" + i);
                yield return RobNews(town, i % 2 == 0, "rn" + i);
            }
        }
    }

    public sealed partial class Engine
    {
        /// The player stopped for a snakebit man on the road: the nearest town hears of it two to five hours on.
        public void SnakebiteTold(string town, string text, long now)
        {
            long due = now + 120 + Voice.Hash(town + now) % 180;
            Later(new LaterAct { Kind = "news", Title = "Word around " + ShortTown(town), At = now, Text = text, DueAt = due });
            Log("TRAVELLER the town will hear of the snakebite in " + (due - now) + " game min");
        }

        /// A lost traveller you rode past or ran off: now and then (Travellers.GoesMissing) the town talks, a day or so on,
        /// of a stranger on foot who never arrived.
        public void TravellerMissing(string town, string seed, long now)
        {
            if (!Travellers.GoesMissing(seed))
                return;
            long due = now + 900 + Voice.Hash(seed + now) % 900;
            Later(new LaterAct { Kind = "news", Title = "Word around " + ShortTown(town), At = now, Text = Travellers.Missing(town, seed), DueAt = due });
            Log("TRAVELLER the one you passed won't arrive; the town will talk of it in " + (due - now) + " game min");
        }

        /// When the traveller was held up (or saved from it): the nearest town hears two to five hours on.
        public void RobberyTold(string town, string text, long now)
        {
            long due = now + 120 + Voice.Hash(text + now) % 180;
            Later(new LaterAct { Kind = "news", Title = "Word around " + ShortTown(town), At = now, Text = text, DueAt = due });
            Log("TRAVELLER the town will hear of the robbery in " + (due - now) + " game min");
        }

        /// Left by the road: half the time (Travellers.BittenDies) the town hears he was found dead, four to eight hours on.
        public void SnakebiteLeft(string town, string seed, long now)
        {
            if (!Travellers.BittenDies(seed))
            {
                Log("TRAVELLER the snakebit man makes it to town on his own");
                return;
            }
            long due = now + 240 + Voice.Hash(seed + now) % 240;
            Later(new LaterAct { Kind = "news", Title = "Word around " + ShortTown(town), At = now, Text = Travellers.BittenDied(town, seed), DueAt = due });
            Log("TRAVELLER the snakebit man won't make it; the town will hear in " + (due - now) + " game min");
        }
    }
}
