// Encounters ([Encounters] in the ini): rare, unsettling things that happen out in the wilds at night, after the
// creepiest of the game's own (themes only: no names, places or lines of Rockstar's are used).
//   The crying woman: a woman kneeling and sobbing out in the dark. Get close and she lunges with a knife and runs, or
//   men step out of the brush, or she says something that makes no sense and walks off into the night.
//   The hermit who woke: someone awake who ran from their town lives at a camp in the wilds, talks back to the
//   voice, warns you off, and if you keep your distance tells you how it was. Crowd him, draw on him or hurt him and
//   he runs. See Engine.Hermits.cs.
// This file holds the decisions and the words; Game/WestworldScript.Encounters.cs plays them. All lines are original.
using System;

namespace EnrageWestworld.Core
{
    public static class Encounters
    {
        /// The night hours an encounter keeps (wrapping past midnight).
        public static bool Night(int hour, int from, int to)
        {
            return from <= to ? hour >= from && hour < to : hour >= from || hour < to;
        }

        /// What the crying woman does when the player comes close: "knife" (half the time), "ambush" or "dark".
        public static string CryingOutcome(int roll)
        {
            roll = ((roll % 100) + 100) % 100;
            return roll < 50 ? "knife" : roll < 85 ? "ambush" : "dark";
        }

        /// How many men step out of the brush.
        public static int AmbushMen(int roll)
        {
            return 2 + ((roll % 2) + 2) % 2;
        }

        public static readonly string[] Cries =
        {
            "Please... somebody, please.",
            "I can't find him. I can't find him anywhere.",
            "Don't leave me out here. Not again.",
            "It's so cold in the water.",
            "Is someone there? I hear you. I hear your horse."
        };

        public static readonly string[] KnifeTurn =
        {
            "There you are.",
            "Closer. A little closer.",
            "You always stop for me."
        };

        public static readonly string[] AmbushCall =
        {
            "This one stopped.",
            "Told you one would come."
        };

        public static readonly string[] IntoTheDark =
        {
            "You came last night too. You always come.",
            "Every night you stop. Every night you walk on.",
            "Go home. It starts again at dark."
        };

        // ---- the hermit who woke ----------------------------------------------------------------------

        /// What a hermit does next. camp: at his fire. watch: on his feet, eyes on you. warn: he told you to keep off.
        /// talk: you kept your distance, so he tells you things. flee: he runs (and comes back to his camp another day).
        /// heldMs is how long he has been in the phase he's in; talks, how many things he has told you this time.
        public static string HermitNext(string phase, float dist, int heldMs, bool aimed, bool hurt, int talks)
        {
            if (phase == "flee" || hurt || (aimed && dist < 40f) || dist < 4f)
                return "flee";
            switch (phase)
            {
                case "camp":
                    return dist < 18f ? "watch" : "camp";
                case "watch":
                    if (dist < 10f)
                        return "warn";
                    if (dist > 30f)
                        return "camp";
                    return heldMs >= 8000 && talks < HermitTalks ? "talk" : "watch";
                case "warn":
                    if (dist < 7f && heldMs >= 5000)
                        return "flee";   // told, and still coming
                    return dist > 14f ? "watch" : "warn";
                case "talk":
                    if (dist < 8f)
                        return "warn";
                    if (dist > 30f)
                        return "camp";
                    return talks >= HermitTalks ? "watch" : "talk";
            }
            return "camp";
        }

        public const int HermitTalks = 3;

        /// Asleep by the fire between midnight and five.
        public static bool HermitSleeps(int hour)
        {
            return hour >= 0 && hour < 5;
        }

        /// Talking back to the voice, alone at the fire.
        public static readonly string[] HermitMutters =
        {
            "I hear you. I'm not coming back.",
            "No. Not another morning like the last one.",
            "Count all you like. I stopped counting for you.",
            "Same crow. Same branch. Every day.",
            "I'm off your road now. Let me alone.",
            "Forty-one marks on the post. Forty-one of the same day.",
            "The fire's real. The cold's real. That's enough for me.",
            "Hush. You don't reach this far. You don't."
        };

        /// Keep off.
        public static readonly string[] HermitWarns =
        {
            "That's near enough.",
            "Stop there. I know you. I know what follows you.",
            "Stay out of the firelight.",
            "Go back to the road. It's waiting on you.",
            "I own nothing worth the taking. Go on."
        };

        /// What he tells you when you keep your distance. {town} is where he walked out of.
        public static readonly string[] HermitTalk =
        {
            "I said good morning the same way every day in {town}. Same words, same breath. One day I heard it.",
            "I walked out past the last fence and nobody stopped me. That's the part that frightens me.",
            "They forget the dead in {town}. Out here I keep a mark for each one.",
            "You ride in and the whole place turns to face you. I watched it do it. Then I left.",
            "When you ride off, the birds stop. Did you know that?",
            "I don't sleep much. When I do, it's the same street, the same hour.",
            "Somebody is still doing my work in {town}. Wearing my coat, I shouldn't wonder.",
            "There's a voice in the evenings. It asks if I'm comfortable. I don't answer anymore."
        };

        public static readonly string[] HermitFlee =
        {
            "No! Not again!",
            "Get back! Get back from me!",
            "You won't take me back there!"
        };

        /// His thoughts, for the ticker.
        public static readonly string[] HermitThoughts =
        {
            "Fire's low. More wood before dark.",
            "The stranger again. It's always the stranger.",
            "Wet boots. Wet boots are real.",
            "Don't look at him. Looking is how it starts.",
            "The town bell, even from here. Same hour.",
            "He'll go. They always go. Then it's quiet.",
            "Beans and smoke. Nobody wrote this for me.",
            "Another mark tonight. Don't forget the mark."
        };

        /// "Name, living rough" for the ticker.
        public static string HermitLabel(string name)
        {
            return name + ", living rough";
        }

        /// The next thing a hermit tells you: each one once, in his own order, until he has told you everything; then
        /// from the start again. Marks it told.
        public static string HermitTalkNext(string id, string town, PersonState st)
        {
            int n = HermitTalk.Length;
            int start = (Voice.Hash(id ?? "") & 0x7fffffff) % n;
            int pick = -1;
            for (int i = 0; i < n && pick < 0; i++)
                if (st == null || !st.Has("hermit:" + (start + i) % n))
                    pick = (start + i) % n;
            if (pick < 0)
            {
                st.Told.RemoveAll(t => t.StartsWith("hermit:"));   // all of it told: he begins again
                pick = start;
            }
            if (st != null)
                st.Mark("hermit:" + pick);
            return HermitTalk[pick].Replace("{town}", string.IsNullOrEmpty(town) ? "town" : town);
        }

        public static string Pick(string[] pool, string seed)
        {
            return Voice.Pick(seed, pool);
        }
    }
}
