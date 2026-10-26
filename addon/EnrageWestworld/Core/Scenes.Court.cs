// One person making eyes at another in the street (Reasons.Courtship): A, taken with B, tries their luck, and B answers
// by what they make of A (Interest, as with the player): yes, not sure, or no, and the reason that weighed most comes
// through in the words. A yes or a maybe starts a romance (the scene's kind is "flirt", which the tie remembers as
// romantic: the next meetings go on to sweet words, then a proposal); a no is a brush-off.
namespace EnrageWestworld.Core
{
    public static partial class Scenes
    {
        public static Scene Court(SceneCast c, string seed, string answer, string why)
        {
            string a = N(c, 0, seed), b = N(c, 1, seed);
            bool shy = why == "shy";
            Scene s = new Scene { Kind = answer == "no" ? "brushoff" : "flirt", Summary = answer == "no" ? "someone tried their luck and was sent off" : "someone caught someone's eye" };
            // A tries their luck
            string open = c.Saloon
                ? Voice.Pick(seed + "co", "Can I stand you a drink, " + b + "?", "This seat taken, " + b + "? It is now.", "I've been watching you laugh all night, " + b + ". Had to come and see why.")
                : Voice.Pick(seed + "co", "Afternoon, " + b + ". Don't believe we've been properly introduced.", "That's a fine hat, " + b + ". Suits you.",
                             b + ", is it? I've seen you about. Been meaning to say good day.", "Walk with me a spell, " + b + "? I'm going your way. Whichever way that is.");
            s.Add(0, open, "greet", "face");
            // B's answer, by what weighed most
            string reply, voice = "mild";
            if (answer == "yes")
            {
                voice = "flatter";
                switch (why)
                {
                    case "money": reply = Voice.Pick(seed + "cy", "Well, now. A " + (c.AWoman ? "woman" : "man") + " who dresses like that can stand me anything.", "You're doing well for yourself, " + a + ". I noticed."); break;
                    case "danger": reply = Voice.Pick(seed + "cy", "Folks say to steer clear of you, " + a + ". I never did like being told."); break;
                    case "restless": reply = Voice.Pick(seed + "cy", "You've got the look of somewhere else about you, " + a + ". Show me."); break;
                    case "lonely": reply = Voice.Pick(seed + "cy", "Nobody's asked me anything in a long while, " + a + ". Go on, then."); break;
                    case "tipsy": reply = Voice.Pick(seed + "cy", "One drink. Then we'll see about the rest."); break;
                    default: reply = Voice.Pick(seed + "cy", "Well. Good day yourself, " + a + ".", "Maybe you can. Maybe I'll let you.", "Took you long enough to ask, " + a + "."); break;
                }
            }
            else if (answer == "maybe")
            {
                reply = shy ? Voice.Pick(seed + "cm", "Oh. I... thank you, " + a + ".", "I don't... nobody says that to me.")
                      : why == "troubled" ? Voice.Pick(seed + "cm", "That's kind, " + a + ". I've a lot on my mind just now, is all.")
                      : Voice.Pick(seed + "cm", "Is that so, " + a + "?", "We'll see, " + a + ". We'll see.", "Hm. Ask me again sometime.");
                voice = shy ? "scared" : "respond";
            }
            else
            {
                voice = why == "frightened" || why == "killer" || why == "fear" ? "scared" : "no";
                switch (why)
                {
                    case "married": reply = Voice.Pick(seed + "cn", "I'm spoken for, " + a + ". You know that."); break;
                    case "elsewhere": reply = Voice.Pick(seed + "cn", "There's someone else, " + a + ". Somewhere."); break;
                    case "busy": reply = Voice.Pick(seed + "cn", "Not today, " + a + ". I can't. Not today."); break;
                    case "frightened": case "killer": reply = Voice.Pick(seed + "cn", "Folks talk about you, " + a + ". I'd sooner you stood back."); break;
                    case "pious": reply = Voice.Pick(seed + "cn", "Mind your soul before my company, " + a + "."); break;
                    case "poor": reply = Voice.Pick(seed + "cn", "Come back when you can pay for the drink, " + a + "."); break;
                    case "beneath": reply = Voice.Pick(seed + "cn", "I'm particular, " + a + ". Very particular."); break;
                    case "sore": reply = Voice.Pick(seed + "cn", "Wrong day for it, " + a + "."); break;
                    case "shy": reply = Voice.Pick(seed + "cn", "I... no. I couldn't. Excuse me."); break;
                    default: reply = Voice.Pick(seed + "cn", "I don't think so, " + a + ".", "No. But thank you for asking.", "Go on, now."); break;
                }
            }
            s.Add(1, reply, voice);
            // and A takes it
            if (answer == "yes")
                s.Add(0, Voice.Pick(seed + "ca", "Saturday, then.", "Then I'll see you at the dance.", "I'll hold you to that."), "flatter");
            else if (answer == "maybe")
                s.Add(0, Voice.Pick(seed + "ca", "I'll take a maybe.", "A maybe's a start."), "mild");
            else
                s.Add(0, Voice.Pick(seed + "ca", "...Right. Good day, then.", "Can't blame a body for trying."), "mild", "walkaway");
            return s;
        }

        /// Every courtship's lines, for the tests: each answer with each reason it can be given for.
        public static System.Collections.Generic.IEnumerable<Scene> CourtSamples(SceneCast c)
        {
            foreach (string why in new[] { "met", "stranger", "money", "danger", "restless", "lonely", "tipsy", "outgoing", "playful" })
                yield return Court(c, why + "y", "yes", why);
            foreach (string why in new[] { "stranger", "shy", "troubled" })
                yield return Court(c, why + "m", "maybe", why);
            foreach (string why in new[] { "married", "elsewhere", "busy", "frightened", "killer", "pious", "poor", "beneath", "sore", "shy", "cold", "notdrawn", "wary", "badrep" })
                yield return Court(c, why + "n", "no", why);
        }
    }
}
