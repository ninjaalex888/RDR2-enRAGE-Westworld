// The night's own trouble and kindness (the NPC-life run, step 5; Lex, 2026-10-30): a man short of money and honest with
// nobody stops someone alone in the dark for their purse (who hands it over and who won't is theirs: the game layer
// asks their minds); and a friend gets a man who has drunk himself off his feet up and home. All lines are original to
// this add-on.
namespace EnrageWestworld.Core
{
    public static partial class Scenes
    {
        /// A (the robber) walks up to B in the dark and demands the money. hands: B gives it up; else B won't, and is knocked
        /// down for it. armed: A has a gun to show (the cast may draw), else a fist and a look.
        public static Scene Mugging(SceneCast c, string seed, bool hands, bool armed)
        {
            string b = N(c, 1, seed);
            Scene s = new Scene { Kind = "mugging", Summary = "someone was robbed in the dark" };
            s.Add(0, null, null, "meet");
            s.Add(0, Voice.Pick(seed + "mq", "Evening. Your money. Slow, now.", "Don't holler. Just empty your pockets.", "Purse. Now. Nobody needs to get hurt.",
                                "Quiet street, this. Let's keep it quiet. Hand it over."), "threat", armed ? "aim" : "face");
            s.Add(1, Voice.Pick(seed + "mp", "Please, mister, I've a family...", "Please. I'm a working man.", "Lord, not tonight..."), "plead");
            if (hands)
            {
                s.Add(1, Voice.Pick(seed + "mh", "Here. Take it. It's all I've got.", "Take it! Just take it and go.", "There. That's every cent."), "scared");
                s.Add(0, Voice.Pick(seed + "mk", "Smart. Now count to a hundred.", "Obliged. You never saw me.", "Wasn't so hard, was it?"), "mock", armed ? "holster" : null);
            }
            else
            {
                s.Add(1, Voice.Pick(seed + "mr", "Not one cent. Get your own.", "Find yourself another fool.", "I earned it. You didn't."), "angry");
                s.Add(0, Voice.Pick(seed + "mn", "Wrong choice, friend.", "Wrong answer.", "Stubborn fool."), "angry", "shove");
            }
            s.Add(0, null, null, "run");
            s.Add(1, Voice.Pick(seed + "mt", "Thief! Somebody stop him!", "Help! I've been robbed!", "Sheriff! Somebody fetch the sheriff!"), "scared");
            return s;
        }

        /// A (who has drunk too much) staggers and goes down; B, a friend, gets him up and takes him home.
        /// carry: he's out cold, and the friend carries him over the shoulder (the game's own carry, the Director's "carry";
        /// if it won't take, he's brought round and walked home); else he comes to and leans on the friend.
        public static Scene CarriedHome(SceneCast c, string seed, bool carry = false)
        {
            string a = N(c, 0, seed);
            Scene s = new Scene { Kind = "carried", Summary = "a friend took a drunk man home" };
            s.Add(0, Voice.Pick(seed + "ca", "...one more. Just the one more...", "Where'd the street go? It was here...", "I'm fine. I'm... finer than fine."), "drunk");
            s.Add(0, null, null, "passout", 2500);
            s.Add(1, null, null, "meet");
            s.Add(1, Voice.Pick(seed + "cb", "Come on, " + a + ". Up you get.", "Lord, " + a + ", not again.", "On your feet, you old fool. I've got you."), "mild", "face");
            if (carry)
            {
                s.Add(1, Voice.Pick(seed + "co", "Out cold. Alright, up you go.", "Dead to the world. Come here, then.", "Not walking tonight, are you? Up."), "mild", "carry");
                return s;
            }
            s.Add(0, null, null, "cometo");
            s.Add(0, Voice.Pick(seed + "cc", "...Is it morning?", "You're a good friend. The best.", "Don't tell my wife."), "drunk");
            s.Add(1, Voice.Pick(seed + "cd", "Home. Now. Lean on me.", "Your wife'll hear of it soon enough. Walk.", "Easy. One foot, then the other."), "mild", "homewith");
            return s;
        }
    }
}
