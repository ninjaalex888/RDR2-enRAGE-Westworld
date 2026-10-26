// The trouble the game shows between people, played out between two of them near the player, each for a reason
// (Reasons.cs): a rough customer grabs a man and smashes his face into whatever is at hand, a bar, a table, a wall
// ("rough"); someone short of money or light of finger lifts a well-off mark's purse in passing, unnoticed or noticed
// too late ("lift"); a drunk calls a man out, squares up, and falls on his face before a punch is thrown ("drunk").
// The moves are the game's own (the Director's "slam", "challenge", "passout" and "cometo"); the words are this
// add-on's. Afterwards the two are tied by it (Ties.cs, with who did what), and think about it.
// All lines are original to this add-on.
namespace EnrageWestworld.Core
{
    public static partial class Scenes
    {
        /// A rough customer and his victim. grudge: there's bad blood between them (a quarrel gone on), else he's a
        /// hothead picking on whoever is near. answer: how the victim takes it ("sharp" swings back, "cold" walks off
        /// promising more, "kind" backs off; Exchanges.Manner).
        public static Scene Rough(SceneCast c, string seed, bool grudge, string answer)
        {
            string a = N(c, 0, seed), b = N(c, 1, seed);
            Scene s = new Scene { Kind = "rough", Summary = "a man grabbed another and smashed his face into the nearest thing" };
            if (grudge)
            {
                switch (Voice.Hash(seed + "rg") % 3)
                {
                    case 0:
                        s.Add(0, b + ". I told you what'd happen if I heard my name in your mouth again.", "angry", "face")
                         .Add(1, "It's a free country, " + a + ".", "mild");
                        break;
                    case 1:
                        s.Add(0, "Still telling folks I'm a cheat, " + b + "?", "angry", "face")
                         .Add(1, "Only the ones who ask.", "mild");
                        break;
                    default:
                        s.Add(1, "Thought you'd have the sense to drink somewhere else, " + a + ".", "mild", "face")
                         .Add(0, "And I thought you'd have the sense to shut up.", "angry");
                        break;
                }
            }
            else
            {
                switch (Voice.Hash(seed + "rs") % 3)
                {
                    case 0:
                        s.Add(0, "You're in my spot.", "angry", "face").Add(1, "There's room enough for two, friend.", "mild");
                        break;
                    case 1:
                        s.Add(0, "What are you looking at?", "angry", "face").Add(1, "Nothing at all. Just minding my drink.", "mild");
                        break;
                    default:
                        s.Add(0, "You laughing at me?", "angry", "face").Add(1, "I wasn't even looking at you.", "mild");
                        break;
                }
            }
            s.Add(0, null, null, "slam")
             .Add(1, Voice.Pick(seed + "hurt", "My nose... God, my nose!", "Agh! You've broke my teeth!", "Lord... I can't see straight."), "scared", "hurt")
             .Add(0, Voice.Pick(seed + "brag", "Next time it's the window.", "Say something else. Go on.", "That's the only warning you get."), "threat");
            switch (answer)
            {
                case "sharp":
                    s.Add(1, Voice.Pick(seed + "back", "You'll answer for that!", "Nobody does that to me!"), "angry", "fight", 8000)
                     .Add(0, null, null, "endfight")
                     .Add(0, Voice.Pick(seed + "won", "Had enough?", "Stay down this time."), "won")
                     .Add(1, Voice.Pick(seed + "lost", "This ain't finished.", "...Alright. Alright."), "mild", "walkaway");
                    break;
                case "cold":
                    s.Add(1, Voice.Pick(seed + "cold", "That was a bad mistake, " + a + ". You'll learn how bad.", "I'll remember this. Every bit of it."), "threat", "walkaway")
                     .Add(0, null, null, "walkaway");
                    break;
                default:
                    s.Add(1, Voice.Pick(seed + "back off", "Easy! I'm leaving. I'm leaving.", "I'll go quiet. Look, I'm going quiet."), "scared", "flee")
                     .Add(0, Voice.Pick(seed + "after", "That's what I thought.", "Go on. Run home."), "mock", "walkaway");
                    break;
            }
            return s;
        }

        /// A purse lifted in passing: the thief brushes past the mark with a word and walks on. noticed: the mark
        /// finds it gone a moment later, after the thief is away; otherwise nothing is ever said.
        public static Scene Lift(SceneCast c, string seed, bool noticed)
        {
            string what = c.BWoman ? "purse" : Voice.Pick(seed + "lw", "billfold", "pocketbook", "watch");
            Scene s = new Scene { Kind = "lift", Summary = "someone's pocket was picked" };
            s.Add(0, null, null, "approach", 200)
             .Add(0, Voice.Pick(seed + "lp", "Beg pardon. Crowded today.", "Pardon me, friend.", "Sorry, sorry. My fault."), "greet")
             .Add(1, Voice.Pick(seed + "lr", "Mind yourself.", "Watch where you're going.", "Mm."), "mild")
             .Add(0, null, null, "walkaway", 4000);
            if (noticed)
                s.Add(1, Voice.Pick(seed + "ln", "Hey... my " + what + "? Where's my " + what + "?", "My " + what + "! Somebody's had my " + what + "!", "Now where... it was right here. It was right here."), "scared", "lookplayer");
            return s;
        }

        /// A drunk calls a man out and goes down before a punch is thrown. answer: how the other takes it.
        public static Scene Drunk(SceneCast c, string seed, string answer)
        {
            string a = N(c, 0, seed), b = N(c, 1, seed);
            Scene s = new Scene { Kind = "drunk", Summary = "a drunk called a man out and fell on his face" };
            s.Add(0, Voice.Pick(seed + "dc", "You. Yeah, you. Outside. You and me.", "I seen you. Lookin' at me. Outside, mister.", "Think you're better'n me? Step outside and say it."), "drunk", "challenge")
             .Add(1, answer == "sharp" ? Voice.Pick(seed + "da", "Fine. Let's go.", "You want it, you've got it.")
                   : answer == "cold" ? Voice.Pick(seed + "dx", "No.", "Sit down before you fall down.")
                   : Voice.Pick(seed + "dk", "Go home, " + a + ". You can't hardly stand.", "Easy, friend. Nobody's looking at you."), answer == "sharp" ? "threat" : "mild")
             .Add(0, Voice.Pick(seed + "dp", "Put 'em up. Put 'em... hold still, why don't you...", "I'll whip you so hard your... your... hold on.", "Right. Right. Here it comes..."), "drunk")
             .Add(0, null, null, "passout", 2500)
             .Add(1, Voice.Pick(seed + "dm", "Out like a lamp.", "So much for the big fight.", "Somebody fetch him a blanket."), "mock")
             .Add(1, null, null, "walkaway", 3000)
             .Add(0, null, null, "cometo", 1500)
             .Add(0, Voice.Pick(seed + "dw", "Where'd he go? I had him...", "Did I win?", "...Floor's moving."), "drunk");
            return s;
        }

        /// The one it was done to sees the one who did it, and finds somewhere else to be (the timid learn): A did it,
        /// B goes.
        public static Scene Avoid(SceneCast c, string seed)
        {
            string a = N(c, 0, seed), b = N(c, 1, seed);
            Scene s = new Scene { Kind = "avoid", Summary = "a man crossed the street to keep clear of another" };
            s.Add(1, Voice.Pick(seed + "av", "Not him. Not today.", "...I'll come back later.", "Lord, it's " + a + "."), "scared")
             .Add(0, Voice.Pick(seed + "as", "That's right, " + b + ". Keep walking.", "Something wrong, " + b + "?", "Go on. Scurry."), "mock", "face")
             .Add(1, null, null, "walkaway");
            return s;
        }

        /// Someone honest and down on their luck asks a well-off stranger for a coin (A asks, B answers by their own
        /// nature). The scene's kind is how it went: "charity" (given) or "scorn" (refused), which the tie remembers.
        public static Scene Beg(SceneCast c, string seed, string answer)
        {
            string a = N(c, 0, seed), b = N(c, 1, seed);
            bool gives = answer == "kind";
            Scene s = new Scene { Kind = gives ? "charity" : "scorn", Summary = gives ? "someone gave a stranger money in the street" : "someone asked a stranger for money and was sent off" };
            s.Add(0, null, null, "approach", 200)
             .Add(0, Voice.Pick(seed + "bg", "Beg pardon. Could you spare a coin? I'd not ask if I had another way.", "Sorry to trouble you. A nickel, for bread. I'll pay it back, I swear.",
                                 "Excuse me. I've never asked anybody for anything before today."), "plead", "face");
            if (gives)
                s.Add(1, Voice.Pick(seed + "bk", "Here. Get yourself something hot.", "Put that away. Take this instead.", "No need to pay it back. Just pass it on someday."), "mild")
                 .Add(0, Voice.Pick(seed + "bt", "God bless you. I mean it.", "...Thank you. I won't forget it."), "thanks");
            else if (answer == "sharp")
                s.Add(1, Voice.Pick(seed + "bs", "Keep away from me! Go and earn it like the rest of us.", "I don't carry money for the likes of you."), "angry")
                 .Add(0, Voice.Pick(seed + "bh", "I only asked.", "...I had work. Till the mill shut."), "mild", "walkaway");
            else
                s.Add(1, Voice.Pick(seed + "bc", "No.", "I gave at church.", "Try the preacher."), "mild", "walkaway")
                 .Add(0, Voice.Pick(seed + "bw", "...Course not.", "Thank you anyway."), "mild", "walkaway");
            return s;
        }

        /// A friend goes to someone who has just been roughed up ("rough") or robbed ("robbed"): A tends, B was hurt.
        /// Its kind is "comfort", which warms the tie and is remembered as a kindness.
        public static Scene Tend(SceneCast c, string seed, string hurt)
        {
            string a = N(c, 0, seed), b = N(c, 1, seed);
            Scene s = new Scene { Kind = "comfort", Summary = "someone saw to a friend who'd had a bad time of it" };
            if (hurt == "rough")
                s.Add(0, Voice.Pick(seed + "tn", "Hold still, " + b + ". Let me look at that.", b + "! Lord, your face. Sit down a minute."), "mild", "face")
                 .Add(1, Voice.Pick(seed + "tb", "It's nothing. ...It's not nothing.", "Don't fuss. I'll live."), "mild")
                 .Add(0, Voice.Pick(seed + "tw", "Come on. I'll walk you home.", "Cold water and a clean rag, that's what you need."), "mild");
            else
                s.Add(0, Voice.Pick(seed + "rn", b + ", you look like you lost a dollar and found a nickel.", "What's wrong, " + b + "? You're white as milk."), "mild", "face")
                 .Add(1, Voice.Pick(seed + "rb", "Somebody had my money. Every cent.", "Robbed. In broad day. Never felt a thing."), "scared")
                 .Add(0, Voice.Pick(seed + "rw", "Come and eat with us tonight. No arguing.", "Here. It's not much, but it'll get you to Saturday."), "mild");
            return s;
        }

        /// The barkeep's word when trouble breaks out in his saloon (C in the scene), or null for a kind he lets be.
        public static string Barkeep(string kind, string seed)
        {
            switch (kind)
            {
                case "rough": return Voice.Pick(seed + "bk", "Not in my saloon! Take it outside or I fetch the sheriff!", "That's my bar you're breaking! Out, the both of you!");
                case "brawl": return Voice.Pick(seed + "bk", "Take it outside! The both of you!", "Not on my floor, gentlemen. Outside!");
                case "drunk": return Voice.Pick(seed + "bk", "Somebody drag him out before he bleeds on my floor.", "That's the last drink he gets in here.");
                default: return null;
            }
        }

        /// Every line the three can say, with names filled, for the tests.
        public static System.Collections.Generic.IEnumerable<Scene> TroubleSamples(SceneCast c)
        {
            for (int i = 0; i < 24; i++)
            {
                string seed = "t" + i;
                foreach (string answer in new[] { "sharp", "cold", "kind" })
                {
                    yield return Rough(c, seed, i % 2 == 0, answer);
                    yield return Drunk(c, seed, answer);
                }
                yield return Lift(c, seed, i % 2 == 0);
                yield return Avoid(c, seed);
                yield return Tend(c, seed, i % 2 == 0 ? "rough" : "robbed");
                foreach (string answer in new[] { "sharp", "cold", "kind" })
                    yield return Beg(c, seed, answer);
            }
        }
    }
}
