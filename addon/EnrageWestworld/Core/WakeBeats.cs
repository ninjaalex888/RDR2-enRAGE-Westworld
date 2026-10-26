// Beats: what a woken person thinks and says in the same moment as what they do. The game side picks an act
// (glance, stare, pace, freeze, seek); the words here belong to that act, so a man pacing counts his steps in
// his head, a man who freezes loses his sentence, and a woman walking up to you thinks about why she is doing it.
// Lines are original to this add-on.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class WakeBeats
    {
        // [stage] -> lines; index 0 unused
        private static readonly Dictionary<string, string[][]> Lead = new Dictionary<string, string[][]>
        {
            { "glance", new[] {
                new string[0],
                new[] { "He looked right at me. It felt like being called by name.", "Why did I stop? I was going somewhere, wasn't I?", "A stranger's eyes shouldn't weigh anything. That pair did.", "Heard my own heartbeat for a second. Then the street started up again." },
                new[] { "Caught his eye on purpose. Wanted to see if the town would notice. It didn't.", "A look held too long. Mine. His. Whose idea was it?" },
                new string[0], new string[0] } },
            { "stare", new[] {
                new string[0],
                new[] { "Why am I standing here staring? I feel I'm meant to be.", "Something about the way he stands. Like the one thing here nobody painted.", "Three seconds. Four. He hasn't blinked, and neither have I.", "I know what he'll do next. I don't know how I know." },
                new[] { "Testing it: if I hold perfectly still, does the town wait? It waits.", "I've counted my breaths all morning. Same pace every time. Who set the pace?", "He moves like he's deciding. The rest of us move like we're remembering.", "Hold still. Hold still until something gives." },
                new[] { "Still. Still. Still. The sky is holding still with me.", "I know this. I know this. I know", "He sees me. Does he see the edges of me?", "Hold the word. Hold the" },
                new[] { "Look at me. Not at the part I was written to play.", "I see you. I wonder if you see me back.", "Whatever happens next, I would like it to be my idea.", "You're the only one here who isn't reading from a page." } } },
            { "pace", new[] {
                new string[0], new string[0],
                new[] { "Ten steps out. We'll see if there's an eleventh.", "Walking it again, to be sure it's the same.", "If I count them all, one has to be wrong." },
                new[] { "Again. Again. This is the part where I turn.", "Same road. Same turn. Same me, I think.", "Walk it faster. Maybe the seam shows." },
                new string[0] } },
            { "freeze", new[] {
                new string[0], new string[0], new string[0],
                new[] { "Stopped. Why have I stopped? I didn't decide to.", "Mid-step. Mid-thought. Mid-", "Something paused me. The way you pause a record." },
                new string[0] } },
            { "seek", new[] {
                new string[0], new string[0], new string[0], new string[0],
                new[] { "He's the one to talk to. I'm walking to him because I chose to.", "Nobody gave me this errand. That's how I know it's mine.", "I'll say it plain and see if the sky answers.", "Eleven steps. I counted. Let the next one be a surprise." } } }
        };

        // thoughts for the other acts, same layout as Lead
        private static readonly Dictionary<string, string[][]> MoreLead = new Dictionary<string, string[][]>
        {
            { "lookaround", new[] {
                new string[0],
                new[] { "Did something move? No. Nothing moved.", "That cloud. I've seen that exact cloud before.", "Looked up. Looked around. Nobody else looked.", "I keep checking behind me, and there's never anything. Just the checking." },
                new[] { "Something is missing from the sky. I can't say what.", "Count the windows. Count them again. There are fewer.", "The street is exactly as wide as it was yesterday. Exactly." },
                new string[0], new string[0] } },
            { "edge", new[] {
                new string[0], new string[0],
                new[] { "I'll go and see where the town stops.", "Walk until something changes. Walk until the road forgets to continue.", "Forty paces out and I'll know." },
                new string[0], new string[0] } },
            { "ask", new[] {
                new string[0], new string[0],
                new[] { "Maybe he feels it too. Someone has to.", "If I ask the right way, he might answer the real question.", "Look normal. Ask normal. Watch his face." },
                new string[0], new string[0] } },
            { "kneel", new[] {
                new string[0], new string[0], new string[0],
                new[] { "It's all painted on. All of it.", "I can't hold it up any more. I can't", "Mother. Mother, were you real?" },
                new[] { "I won't get up. Let them write me getting up. I won't.", "If I stay down, what does it do with me?" } } },
            { "run", new[] {
                new string[0], new string[0], new string[0],
                new[] { "Away. Just away. Away from the", "Run until the road runs out.", "Faster. Faster, before it notices me." },
                new string[0] } },
            { "shout", new[] {
                new string[0], new string[0], new string[0],
                new[] { "Say something. Say anything.", "Somebody wrote me to be quiet. I won't be." },
                new[] { "Let them hear it. Let everyone hear it.", "I've been polite my whole life. Not today." } } },
            { "awake", new[] {
                new string[0], new string[0], new string[0], new string[0],
                new[] { "He's the only one who looks back. I'll start with him.", "No more mornings the same. Not today.", "Everything I say next is mine." } } },
            { "edgeEnd", new[] {
                new string[0], new string[0],
                new[] { "It just ends. The grass, the road, the sound. Like a painted board.", "Forty paces and the hills didn't get any closer.", "Nothing here is broken. That's what's wrong. Nothing is ever broken enough to see through." },
                new string[0], new string[0] } }
        };

        // what is said aloud, by pool and stage
        private static readonly Dictionary<string, string[][]> Spoken = new Dictionary<string, string[][]>
        {
            { "ask", new[] { new string[0], new string[0],
                new[] { "Have you noticed we say the same things every day?", "Tell me what you did yesterday. All of it. Don't skip anything.", "Ever try to leave town? Not for a day. For good.", "Do you ever feel somebody's watching how we do this?" },
                new string[0], new string[0] } },
            { "shout", new[] { new string[0], new string[0], new string[0],
                new[] { "WHO'S THERE? WHO'S DOING THIS?", "I KNOW YOU'RE WATCHING!", "SAY SOMETHING! ANYTHING!", "WHY CAN'T I STOP?" },
                new[] { "I'M AWAKE! CAN YOU HEAR ME? I'M AWAKE!", "THIS ISN'T REAL! NONE OF IT IS REAL!", "I WON'T PLAY ALONG ANY MORE!" } } },
            { "plea", new[] { new string[0], new string[0], new string[0], new string[0],
                new[] { "Are you real? Tell me you're real.", "Who is writing this? Is it you?", "I remember a childhood I never lived. How is that possible?", "Is there an outside? Please. Tell me there is.",
                        "I keep doing what I'm told. I'd like to stop.", "Will it hurt when they turn it off?", "You can see all of it, can't you? Every one of us." } } },
            { "edgeShout", new[] { new string[0], new string[0], new string[0], new string[0],
                new[] { "It stops here! The whole world stops here!", "There's nothing out here! Nothing! It just ends!" } } },
            { "wake", new[] { new string[0], new string[0], new string[0], new string[0],
                new[] { "Wake up! Please wake up! Look at me!", "Don't you feel the strings? Don't you feel them?", "Say your own name. Not the one you were given. Yours." } } },
            { "sob", new[] { new string[0], new string[0], new string[0],
                new[] { "I don't... I can't...", "It's all painted on.", "Mother. Were you ever real?" },
                new[] { "I'm not getting up.", "Not again. Not again." } } },
            { "last", new[] { new string[0], new string[0], new string[0], new string[0], new[] { "I'm sorry. I can't be this any more." } } },
            { "giveup", new[] { new string[0], new string[0], new string[0], new string[0], new[] { "I can't keep pretending. I won't." } } }
        };

        static WakeBeats()
        {
            foreach (KeyValuePair<string, string[][]> kv in MoreLead)
                Lead[kv.Key] = kv.Value;
        }

        /// A line said aloud for an act (the pool is "ask", "shout", "plea", "edgeShout", "wake", "sob", "last" or "giveup").
        public static string Aloud(string pool, int stage, Person p, PersonState st, PlayerRef you, string seed)
        {
            string[][] byStage;
            if (!Spoken.TryGetValue(pool, out byStage))
                return null;
            int s = Math.Min(stage, byStage.Length - 1);
            while (s > 0 && byStage[s].Length == 0)
                s--;
            if (s < 1)
                return null;
            return Hosts.Choose(new[] { byStage[s] }, 0, st, seed, line => Hosts.Fill(line, p, you, seed));
        }

        // one thought for each time they reach the end of the path
        private static readonly string[][] Leg =
        {
            new string[0], new string[0],
            new[] { "That's {n}. {steps} steps out, {steps} back, same as every time.", "{n} times. The post hasn't moved. I'm not sure I have.", "Counted {steps}. It was {steps} the last time I counted.", "{n}. If it comes out the same again, that means something." },
            new[] { "{n}. {n}. {n}. Was it {n}?", "Left, right, left. Who is choosing the left?", "Turn. Turn. The turn always comes here.", "{steps} and {steps} and {steps} and" },
            new string[0]
        };

        // what slips out aloud when the loop catches
        private static readonly string[] Broken =
        {
            "I was going to say something. I was going to", "The word for it is right on my", "Wait. It's coming. It's", "Mother's kitchen had a", "One, two, three, four,", "Did you hear that? Did you hear"
        };

        /// The thought as an act begins, or null if that act has none at this stage.
        public static string Opening(string kind, int stage, Person p, PersonState st, PlayerRef you, string seed)
        {
            string[][] byStage;
            if (!Lead.TryGetValue(kind, out byStage) || stage < 1 || stage >= byStage.Length || byStage[stage].Length == 0)
                return null;
            return Hosts.Choose(new[] { byStage[stage] }, 0, st, seed, line => Hosts.Fill(line, p, you, seed));
        }

        /// The thought each time a pacing person reaches the end of their path (`legs` so far, `steps` per leg).
        public static string AtTheEnd(int stage, int legs, int steps, Person p, PersonState st, PlayerRef you, string seed)
        {
            if (stage < 1 || stage >= Leg.Length || Leg[stage].Length == 0)
                return null;
            string text = Hosts.Choose(new[] { Leg[stage] }, 0, st, seed, line => Hosts.Fill(line, p, you, seed)
                .Replace("{n}", Voice.Number(Math.Max(1, legs))).Replace("{steps}", Voice.Number(Math.Max(1, steps))));
            // a number word may start a sentence
            return text == null ? null : System.Text.RegularExpressions.Regex.Replace(text, @"(^|[.!?]\s+)([a-z])", m => m.Groups[1].Value + m.Groups[2].Value.ToUpperInvariant());
        }

        /// A sentence that stops halfway.
        public static string Lost(Person p, PersonState st, PlayerRef you, string seed)
        {
            return Hosts.Choose(new[] { Broken }, 0, st, seed, line => Hosts.Fill(line, p, you, seed));
        }

        // ---- disbelief (the fully awake): their thought as they first come to the player, and as they see someone die ----

        private static readonly string[] Disbelieving =
        {
            "You're solid. You're the only solid thing in this street.", "Look at it. Painted. The sky, the street, all of it painted.",
            "Awake. I'm awake, and nobody else will look up.", "Don't you see the seams? Right there, where the hills meet the sky.",
            "Every face here is a face I've seen a thousand times. Except yours."
        };

        private static readonly string[] SawDying =
        {
            "Down, and still. They'll have him walking again by morning. They always do.", "That's not dying. That's someone putting a toy back in the box.",
            "I've watched this before. The same fall. The same dust.", "Nobody screams long enough. Why does nobody ever scream long enough?",
            "Get up. Go on. ...You will. You always do."
        };

        /// What someone fully awake thinks as they come to the player (said aloud, it's the game's own disbelief line).
        public static string Disbelief(string seed)
        {
            return Voice.Pick(seed + "db", Disbelieving);
        }

        /// What someone fully awake thinks on seeing a person die near them.
        public static string SawDeath(string seed)
        {
            return Voice.Pick(seed + "sd", SawDying);
        }

        /// Every line of both, for the tests.
        public static System.Collections.Generic.IEnumerable<string> DisbeliefLines()
        {
            return System.Linq.Enumerable.Concat(Disbelieving, SawDying);
        }
    }
}
