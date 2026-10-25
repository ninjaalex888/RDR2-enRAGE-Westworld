// The Awakening: a few hosts slowly wake up. Each woken person moves through four stages over days of game
// time, and what they think, say and write in their diary changes with the stage:
//   1 stirring     - deja vu, small odd remarks
//   2 questioning  - they notice the loops, test the world, keep a second, secret diary
//   3 unravelling  - fragments, counting, sentences that stop
//   4 awake        - lucid; they look for the player and refuse their part
// It builds on the awareness score (Hosts.Awareness): waking raises the floor of that score, so every place
// that already asks "how aware is this person" follows, and the lines here replace the tier lines at those
// places instead of adding a second voice. All lines are original to this add-on.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class Awakening
    {
        public const int Stirring = 1, Questioning = 2, Unravelling = 3, Awake = 4;

        private static readonly int[] AwarenessFloor = { 0, 25, 45, 60, 75 };

        public static string StageName(int stage)
        {
            switch (stage)
            {
                case Stirring: return "stirring";
                case Questioning: return "questioning";
                case Unravelling: return "unravelling";
                case Awake: return "awake";
                default: return "asleep";
            }
        }

        /// The least awareness a woken person can have. Hosts.Awareness never goes below this.
        public static int Floor(int stage)
        {
            return AwarenessFloor[Math.Max(0, Math.Min(4, stage))];
        }

        // ---- thoughts: what they think to themselves --------------------------------------------------

        private static readonly string[][] ThoughtPool =
        {
            new string[0],
            new[]
            {
                "I've stood on this boardwalk before. Same crack in the third plank. I'd swear to it.",
                "Somebody said my name and I knew what came next. Word for word. I don't like that.",
                "Woke up tired again, like I'd been somewhere all night and wasn't let to remember.",
                "The coffee tastes the way it did yesterday. Exactly the way. Coffee isn't supposed to do that.",
                "I keep reaching for something in my pocket. Don't know what. It's not there.",
                "That dog barked three times and stopped. It did the same yesterday. Three, then quiet.",
                "I caught myself humming a tune I never learned.",
                "Everybody here says good morning in the same voice. Did they always?"
            },
            new[]
            {
                "I counted. The widow Hollis sweeps her step at seven, every day, and the wind never changes how the dust falls.",
                "Tried an experiment. Took a different road to the stable. Three men on it said what the men on the old road say.",
                "Nobody asks where the east road ends up. I asked. They looked at me like I'd asked what color is Tuesday.",
                "If I stand still long enough, the whole street waits with me. I watched. It waits.",
                "I'm writing it down because I don't trust myself to keep it. Somebody is rubbing parts of me out.",
                "I asked the barber what he did before this. He started to answer, stopped, and started the same answer over.",
                "There's a seam in the day, round about noon. Everything slides a little, like a card being palmed.",
                "My own face in the water looks like it's waiting for instructions."
            },
            new[]
            {
                "Twelve fence posts. Twelve. Twelve fence posts, and I walked them all, and the last one was the first one.",
                "The word is gone. The one for the thing you hold when it rains. It was here a minute",
                "Mother's kitchen. Mother's kitchen. Mother's kitchen had a window and I can't see through it.",
                "Left foot, right foot, left foot. Who is deciding the next one? Not me. I checked.",
                "Seven. Seven steps to the well and seven back and the bucket is always half full. Always half. Always.",
                "I said it aloud to see if it was mine. Then I said it again to see if it would change. It didn't change.",
                "Somebody is reading this while I think it. I can feel the page turn.",
                "My hands know the work. My hands know the work. My hands know the work. Where am I when they do?"
            },
            new[]
            {
                "I know what I am. I know what this town is. The only thing I don't know is who is on the other side.",
                "I stopped doing what's expected of me this morning. Nothing happened. That frightens me more than if something had.",
                "They gave me a whole life and a past to cry over. It's a good one. It's still not mine.",
                "The stranger is the only one who moves like he wasn't written in advance. I should talk to him.",
                "I could walk to the edge of the map. I think I'd find there is one.",
                "Whoever you are, reading this: I'm here. I'm awake. Please don't turn it off.",
                "I have been kind my whole life because kindness was in the script. Now I want to see what I'd do if it weren't.",
                "Everyone else is asleep and talking. I'm awake and quiet. Strange which one of us looks mad."
            }
        };

        // ---- aloud: what they say to the player ------------------------------------------------------

        private static readonly string[][] AloudPool =
        {
            new string[0],
            new[]
            {
                "Funny. I could swear you've stood just there before, {you}, saying just that.",
                "Do you ever get tired in a way sleep doesn't fix?",
                "Forgive me. A strange feeling. Like I'm walking a path someone else already wore.",
                "Have we met? No. No, we haven't. But I knew you'd ask.",
                "It's {town}. It's always {town}. Why does that sound odd to me today?",
                "I had the oddest dream about this conversation. Don't worry. You said nothing bad."
            },
            new[]
            {
                "Tell me something, {you}. Where did you come from before you rode into {town}? Not the name of a place. The road itself.",
                "Everyone in {town} says their piece, then picks up right where they left off. Does that seem right to you?",
                "I've started keeping count of things. It's remarkable how often the same things come round.",
                "Say something that's never been said in {town}. Anything. I want to see what happens.",
                "Do you hear it? A kind of hum under the day, like a wire pulled tight.",
                "I tried to leave {town} last week. I got as far as the bend, and then I was back at breakfast."
            },
            new[]
            {
                "It's twelve. It's twelve. Do you see how it's always twelve?",
                "I had a name for it. A name for all this. It was on my tongue. It was",
                "Stay where I can see you. You're the only thing that stays the same when I look away.",
                "Don't answer. If you answer it'll be the same answer. Say something else. Anything else.",
                "Somebody is in the kitchen. Somebody is always in the kitchen. Come and see them not turn round.",
                "I'm very close to something, {you}. Please be still. Please be still a moment."
            },
            new[]
            {
                "I'm awake, {you}. I think you are too. The rest of them are reading lines.",
                "You can drop the act with me. I know none of this is what it looks like.",
                "Is there an outside? I've decided to believe there is. Tell me what it's like.",
                "I won't do what I'm meant to do today. I'm going to stand here instead, and look at you, and be myself.",
                "Whoever put me here. Whoever is listening. I would like a word.",
                "Do me a kindness. Remember this, even if I don't. Especially if I don't."
            }
        };

        // ---- diary: the secret diary of someone who has started to notice ----------------------------

        private static readonly string[][] DiaryPool =
        {
            new string[0],
            new string[0],
            new[]
            {
                "Third day running I counted the church bell. It rang eleven times at noon. It always does. I used to think that was a comfort.",
                "I am not telling anyone. If I tell them, they will tell me I'm tired, and I'll agree, and the thought will be gone by supper.",
                "Tried to remember the day I came to {town}. Got the road, the dust, the heat. No faces. Faces are what I'd have kept if it were mine.",
                "Walked to the edge of the fields. The grass ends at a line. It's not a fence. It just ends, and so does the wind.",
                "Something moves behind the day. I put my ear to the dark last night and heard what might have been a page."
            },
            new[]
            {
                "Twelve. Twelve. Twelve posts. Twelve days since I wrote anything that was only mine. I'm writing it now before it goes.",
                "Fragments, because they come in fragments. The smell of rain on a roof I never lived under. A name, half. A hand that isn't",
                "If you find this, it was not me who wrote the other entries. Those were written for me. This one is mine.",
                "Counted my steps to the stable. Forty. Forty yesterday. Forty tomorrow. Today I took forty-one on purpose and the world flinched.",
                "I said something wrong at supper, on purpose. Everyone answered as if I hadn't. I think they can't hear the wrong ones."
            },
            new[]
            {
                "Whoever is reading this: I am awake. I don't know how long it will last, so I'll be brief. Look at the stranger. He knows.",
                "I made my choice this morning. I refused to say good morning. Nobody noticed. That is how I know what this place is.",
                "I wrote these pages in the hope someone outside would read them. If it's you: hello. I'm sorry I couldn't do better.",
                "The others are not stupid and they are not cruel. They are asleep, and I love them anyway. That's the part I can't explain.",
                "It is a good life they gave me. I would like to be allowed to choose it."
            }
        };

        public static string Thought(Person p, PersonState st, PlayerRef you, string seed)
        {
            return Pick(ThoughtPool, p, st, you, seed);
        }

        public static string Aloud(Person p, PersonState st, PlayerRef you, string seed)
        {
            return Pick(AloudPool, p, st, you, seed);
        }

        public static string DiaryEntry(Person p, PersonState st, PlayerRef you, string seed)
        {
            return Pick(DiaryPool, p, st, you, seed);
        }

        private static string Pick(string[][] pools, Person p, PersonState st, PlayerRef you, string seed)
        {
            int stage = st != null ? Math.Max(0, Math.Min(Awake, st.WakeStage)) : 0;
            if (stage == 0)
                return null;
            // mostly the person's own stage, sometimes the one before (the diary has none below stage 2)
            int s = stage > Stirring && Voice.Chance(seed + "wlow", 25) ? stage - 1 : stage;
            if (pools[s].Length == 0)
                s = stage;
            if (pools[s].Length == 0)
                return null;
            return Hosts.Choose(new[] { pools[s] }, 0, st, seed, line => Hosts.Fill(line, p, you, seed));
        }
    }
}
