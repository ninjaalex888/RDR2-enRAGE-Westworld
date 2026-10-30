// Passing the player, from the mind (roadmap 39; night 2026-10-29, item 6f; with [Mind] MindLive and [Mind] MindPassing):
// someone who feels something about the player, from what they saw or lived through with him a while ago, shows it when he
// passes, as they show it to each other (Passing.Minded): grateful, a hat tip or a wave and a greeting (one a game day; a
// nod after); sore at him, a hard look (a spit at his feet from the cold, the proud and the hotheaded, never a woman), unless
// they fear him too, when it's a sullen look away; afraid of him, a glance and away; shamed by him, eyes down and no look
// at all. Kept off their face (masked), only the thought says it. Only a feeling some time old (MindAfter): what he did a
// moment ago is the onlookers' (Onlooking), not this. Pure; plain C#, no game. All lines are original to this add-on.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static partial class Passing
    {
        /// A feeling about the player must be at least this old, in game minutes, to show as he passes (the review of 6f:
        /// what he did a moment ago is the onlookers' to answer).
        public const long MindAfter = 15;

        /// What someone does as the player passes, by what they feel about him; null for nothing. `spokeToday`: they gave
        /// him a spoken greeting already this game day (a nod after that).
        public static Act ToYou(List<FeltAt> felt, bool woman, string temper, long now, string seed, bool spokeToday = false)
        {
            if (felt == null)
                return null;
            FeltAt best = default(FeltAt);
            float left = 0f, fear = 0f;
            foreach (FeltAt f in felt)
            {
                if (!AboutB(f, "player") || now - f.At < MindAfter)
                    continue;
                float l = Mood.Lasting(f, now);
                if (f.Kind >= 0 && f.Kind < Feeling.Kinds.Length && Feeling.Kinds[f.Kind] == "fear" && l > fear)
                    fear = l;
                if (l > left)
                {
                    left = l;
                    best = f;
                }
            }
            if (left < MindLeft)
                return null;
            string kind = Feeling.Kinds[best.Kind];
            if ((kind == "anger" || kind == "contempt") && fear >= MindLeft)
                kind = "sullen";   // sore at him and afraid of him too: nobody spits at an armed man he fears
            var a = new Act { Kind = "you:" + kind, Answer = "none", BThinks = false };
            string pool;
            switch (kind)
            {
                case "gratitude":
                    a.Gesture = spokeToday ? "nod" : woman ? "wave" : "hattip";
                    a.Speaks = !spokeToday;   // one spoken greeting a game day, as manners had it; a nod after
                    a.LookMs = 2200;
                    pool = "grateful";
                    break;
                case "anger":
                case "contempt":
                    a.Gesture = Spitter(temper) && !woman && Voice.Hash(seed + "|spit") % 100 < 30 ? "spit" : "none";
                    a.LookMs = 2600;
                    pool = "sore";
                    break;
                case "sullen":
                    a.Gesture = "none";
                    a.LookMs = 700;
                    pool = "sullen";
                    break;
                case "fear":
                    a.Gesture = "none";
                    a.LookMs = 600;
                    pool = "afraid";
                    break;
                case "shame":
                    a.Gesture = "none";
                    a.LookMs = 0;   // eyes down: no look at him at all
                    pool = "shamed";
                    break;
                default:
                    return null;   // envy and the rest: nobody shows the stranger those as he passes
            }
            if (best.Masked)
            {
                // kept off the face: they look as they would at anyone; only the thought says it
                a.Gesture = "none";
                a.Speaks = false;
                a.LookMs = Math.Min(a.LookMs, 1000);
            }
            string[] p = YouPools[pool];
            a.Thought = p[Voice.Hash(seed + "|you|" + pool) % p.Length];
            return a;
        }

        /// Their thoughts as the player passes (kept apart from Pools, whose count the tests hold).
        internal static readonly Dictionary<string, string[]> YouPools = new Dictionary<string, string[]>
        {
            { "grateful", new[] { "That's him. The stranger who stood by me.", "I owe that man more than a good day.", "There he goes. Some folk you don't forget.", "Him again. Good. Whatever he is, he did right by me.",
                                  "Wish I had more to give him than a nod and a word.", "A kindness like that, from a stranger. I think of it yet.", "There's a man I'd stand a drink, if he'd take it." } },
            { "sore", new[] { "Keep walking, mister. I'll not forget it.", "Him. I haven't forgotten. I won't.", "Look at him, walking about like he owns the street.", "One of these days somebody'll settle his account.",
                              "Go on, stranger. Stare all you like. I remember.", "There he goes, and not a care in the world.", "If looks did the work, he'd be in the ground." } },
            { "sullen", new[] { "Look away. Just look away from him.", "I'd say something. I won't. Not to him.", "Swallow it. Swallow it and walk on.", "He knows what he did. Let it lie. For now.",
                                "Not worth a bullet in the gut. Walk on.", "Teeth gritted, eyes down. That's how a man lives with it." } },
            { "afraid", new[] { "Don't look at him. Don't let him see you looking.", "Him. Walk quicker. Walk quicker.", "My heart's going like a hammer. Just walk.", "He's looking this way. Is he looking this way?",
                                "Keep your head down and he'll pass.", "Hands where he can see them. Lord, why did I think that.", "Let him go by. Just let him go by." } },
            { "shamed", new[] { "He saw it all. I can't meet his eye.", "The stranger. Of all the people to see me so.", "Eyes on my boots till he's gone by.", "He knows. He was there. Walk on.",
                                "Can't bear the way he'd look at me.", "My face is hot. He'll think it's the sun.", "Walk on. He's forgotten it. He must have." } },
        };

        public static IEnumerable<string> YouLines()
        {
            foreach (string[] p in YouPools.Values)
                foreach (string l in p)
                    yield return l;
        }
    }
}
