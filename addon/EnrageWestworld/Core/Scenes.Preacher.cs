// A street preacher on a Sunday afternoon (overnight 2026-10-27, Tier 4): one of the devout stands in the street and
// preaches, and someone passing answers by their own faith: the devout with an amen, the churchgoer with a nod and a
// word, the lapsed uneasy, the godless with a jeer. The preacher answers in kind. A is the preacher, B the one who
// answers. All lines are original to this add-on.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static partial class Scenes
    {
        public static readonly string[] Devotions = { "Devout", "Churchgoer", "Lapsed", "Godless" };

        /// Who preaches: only the devout (a churchgoer now and then).
        public static bool WouldPreach(string devotion, string seed)
        {
            return devotion == "Devout" || (devotion == "Churchgoer" && Voice.Chance(seed + "|preach", 25));
        }

        public static Scene Preacher(SceneCast c, string seed, string answerDevotion)
        {
            string a = N(c, 0, seed), b = N(c, 1, seed);
            Scene s = new Scene { Kind = answerDevotion == "Godless" ? "argue" : "smalltalk", Summary = "someone preached in the street on a Sunday" };
            s.Add(0, Voice.Pick(seed + "p1", "Hear me, friends! The Lord keeps a ledger, and every name is in it!", "Brothers and sisters, it's the Sabbath. Have you thought on your souls today?",
                                "Repent, I say! The road is wide that leads to ruin, and this street is wide enough!"), "cheer", "face")
             .Add(0, Voice.Pick(seed + "p2", "Not a sparrow falls but He sees it. Nor a dollar spent in that saloon.", "The whiskey's free of a Sunday, they say. Nothing's free. Nothing.",
                                "A man can run from the law. He can't outrun the Lord."), "mild");
            switch (answerDevotion)
            {
                case "Devout":
                    s.Add(1, Voice.Pick(seed + "b1", "Amen, " + a + "! Amen!", "Preach it, " + a + ". Preach it.", "Praise be. Say it again, " + a + "."), "cheer")
                     .Add(0, Voice.Pick(seed + "a1", "Bless you, " + b + ". Bring a friend next Sunday.", "There's one who listens! The Lord sees you, " + b + "."), "mild");
                    break;
                case "Churchgoer":
                    s.Add(1, Voice.Pick(seed + "b2", "Fine words, " + a + ". I was at service this morning.", "Can't argue with any of that.", "Save some for the pastor, " + a + "."), "mild")
                     .Add(0, Voice.Pick(seed + "a2", "Then you know it's true. Spread it.", "The pastor has his flock. I have the street."), "mild");
                    break;
                case "Lapsed":
                    s.Add(1, Voice.Pick(seed + "b3", "...Haven't been to church in a while.", "Ma used to talk like that. Don't, " + a + ".", "Not today. Please, not today."), "mild")
                     .Add(0, Voice.Pick(seed + "a3", "The door's never shut, " + b + ". Not even to you.", "It's never too late. That's the whole of it."), "mild");
                    break;
                default:
                    s.Add(1, Voice.Pick(seed + "b4", "Preach it somewhere else, " + a + ". Some of us are working.", "Your Lord never paid my rent.", "Save your breath for the hymns."), "mock")
                     .Add(0, Voice.Pick(seed + "a4", "You'll be in my prayers tonight, " + b + ", whether you want them or no.", "Mock, then. He hears the mocking too."), "angry");
                    break;
            }
            return s;
        }

        public static IEnumerable<Scene> PreacherSamples(SceneCast c)
        {
            for (int i = 0; i < 12; i++)
                foreach (string d in Devotions)
                    yield return Preacher(c, "pr" + i, d);
        }
    }
}
