// Frontier scenes: the moments every western has. Two men square off in the street, a drunk murders an old
// song, an old-timer spins a tall tale (sometimes about the player), somebody reads the paper aloud, a horse
// gets haggled over, a snake-oil man works a crowd, a posse forms after a killing, a widow grieves for a
// man the player shot, neighbours make small talk, a pickpocket goes to work.
// The dialogue is original. The songs and the few quotations are public domain (traditional cowboy songs,
// 19th-century hymns and parlour songs, Shakespeare, Poe, Dickinson, The Virginian of 1902, Barnum), and
// the rest leans on the genre's stock phrases and period slang rather than lines from any film.
// The player can step into any of these: see Intrude().
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static partial class Scenes
    {
        private static void FrontierWeights(SceneCast c, List<KeyValuePair<string, int>> w, bool twoMen, bool allowFight)
        {
            w.Add(new KeyValuePair<string, int>("smalltalk", c.Saloon ? 6 : 12));
            if (twoMen)
                w.Add(new KeyValuePair<string, int>("standoff", c.Night ? 7 : c.Saloon ? 4 : 5));
            if (c.Saloon || c.Night)
                w.Add(new KeyValuePair<string, int>("song", c.Saloon && c.Night ? 14 : c.Saloon ? 8 : 3));
            w.Add(new KeyValuePair<string, int>("talltale", (c.OldTimer ? 12 : 4) + (c.Legend != null ? 6 : 0)));
            if (!c.Night)
                w.Add(new KeyValuePair<string, int>("paper", c.PaperItems.Count > 0 ? 10 : 4));
            if (!c.Night && !c.Saloon)
                w.Add(new KeyValuePair<string, int>("haggle", 6));
            if (!c.Night && !c.Saloon && c.Town != null)
                w.Add(new KeyValuePair<string, int>("pitch", c.HasCrowd ? 5 : 3));
            if (!c.Night && !c.Saloon && c.Town != null && !c.AWoman)
                w.Add(new KeyValuePair<string, int>("pickpocket", 10));   // was 3 (Lex, 2026-10-29: "an exciting feature to show case")
        }

        private static Scene MakeFrontier(string kind, SceneCast c, string seed)
        {
            switch (kind)
            {
                case "standoff": return Standoff(c, seed, true);
                case "song": return Song(c, seed);
                case "talltale": return TallTale(c, seed);
                case "paper": return ReadPaper(c, seed);
                case "haggle": return Haggle(c, seed);
                case "pitch": return Pitch(c, seed);
                case "posse": return c.CrimeRumour != null ? Posse(c, seed) : null;
                case "mourn": return c.DeadName != null ? Mourn(c, seed) : null;
                case "smalltalk": return SmallTalk(c, seed);
                case "pickpocket": return Pickpocket(c, seed);
            }
            return null;
        }

        // ---- the standoff ---------------------------------------------------------------------------

        public static Scene Standoff(SceneCast c, string seed, bool allowFight)
        {
            string a = N(c, 0, seed), b = N(c, 1, seed);
            Scene s = new Scene { Kind = "standoff", Cast = c.HasCrowd ? 3 : 2, Summary = "two men squared off in the street" };
            switch (Voice.Hash(seed + "open") % 6)
            {
                case 0:
                    s.Add(0, "This town ain't big enough for the both of us, " + b + ".", "threat", "face").Add(1, "Then you'd best start packing.", "stare");
                    break;
                case 1:
                    s.Add(0, "You've been telling folks I'm yellow.", "threat", "face").Add(1, "I been telling folks the truth.", "stare");
                    break;
                case 2:
                    s.Add(0, "Say that again, you mangy cur.", "angry", "face").Add(1, "When you call me that, smile.", "stare");
                    break;
                case 3:
                    s.Add(0, "Who in this town's got the sand to face me?", "threat", "face").Add(1, "I'm your huckleberry.", "stare");
                    break;
                case 4:
                    s.Add(0, "You called me a liar in front of my own kin, " + b + ".", "angry", "face").Add(1, "Then your kin heard right.", "stare");
                    break;
                default:
                    s.Add(0, "Fill your hand, " + b + ".", "armed", "face").Add(1, "You sure you want to die in this much mud?", "stare");
                    break;
            }
            s.Add(0, null, c.Armed0 ? "armed" : null, "aim", 300).Add(1, null, null, "aim", 300);
            if (c.HasCrowd)
                s.Add(2, Voice.Pick(seed + "crowd", "Somebody fetch the sheriff!", "Lord, not again. Get inside, Myrtle!", "Two bits on the tall one.",
                    "Easy, boys! Easy!"), Voice.Chance(seed + "cv", 50) ? "intervene" : "cheer");
            s.Add(0, Voice.Pick(seed + "t1", "Boot Hill's full of men who talked like you.", "Last chance to walk away.", "My hand's steadier than yours, " + b + ". Look at it shake.",
                "Nobody has to die today."));
            s.Add(1, Voice.Pick(seed + "t2", "Then you've got a shovel. Good. You'll need it.", "I was about to say the same.", "That's the whiskey shaking, not me.",
                "Somebody always does."));
            int roll = Voice.Hash(seed + "end") % 100;
            if (roll < 50)
            {
                s.Add(1, Voice.Pick(seed + "back", "...Ain't worth hanging for.", "Another day, " + a + ". Another day.", "Fine. You win. This time."), "mild", "holster")
                 .Add(0, Voice.Pick(seed + "won", "That's what I thought.", "Go on, then. Run home.", "Smart man."), "won", "holster")
                 .Add(1, null, "storm", "walkaway");
            }
            else if (roll < 70 || !allowFight)
            {
                s.Add(0, "...Another day, then.", null, "holster").Add(1, "Another day.", null, "holster").Add(0, null, null, "walkaway").Add(1, null, null, "walkaway");
            }
            else
            {
                s.Fight = true;
                s.Add(0, Voice.Pick(seed + "fists", "Put it away. Let's settle this like men.", "Guns are for cowards. Come here."), "fight", "holster")
                 .Add(1, null, null, "holster").Add(1, "Gladly.", "fight", "fight", 9000)
                 .Add(0, null, null, "endfight").Add(0, Voice.Pick(seed + "w", "Stay down.", "Next time, I won't be so gentle.", "Now we're square."), "won")
                 .Add(1, Voice.Pick(seed + "l", "...This ain't over.", "Lucky punch.", "Alright! Alright."), "mild", "walkaway");
            }
            return s;
        }

        // ---- a drunk sings ------------------------------------------------------------------------------

        // Public domain: traditional cowboy songs, and parlour songs and hymns from before 1900.
        private static readonly string[][] Songs =
        {
            new[] { "Oh my darling, oh my darling, oh my darling Clementine...", "You are lost and gone forever, dreadful sorry, Clementine!" },
            new[] { "Oh, give me a home where the buffalo roam...", "...where the deer and the antelope play!" },
            new[] { "From this valley they say you are going...", "We will miss your bright eyes and sweet smile..." },
            new[] { "As I walked out in the streets of Laredo...", "...as I walked out in Laredo one day..." },
            new[] { "Oh, Shenandoah, I long to see you...", "Away, you rolling river..." },
            new[] { "Oh, bury me not on the lone prairie...", "...where the coyotes howl and the wind blows free!" },
            new[] { "Whoopee ti yi yo, git along, little dogies...", "It's your misfortune and none of my own!" },
            new[] { "Come along, boys, and listen to my tale...", "I'll tell you of my troubles on the old Chisholm Trail!" },
            new[] { "Oh, don't you remember sweet Betsy from Pike...", "...who crossed the wide prairie with her lover Ike?" },
            new[] { "Jesse James was a lad who killed many a man...", "He robbed the Glendale train!" }
        };

        public static Scene Song(SceneCast c, string seed)
        {
            string a = N(c, 0, seed), b = N(c, 1, seed);
            Scene s = new Scene { Kind = "song", Summary = "a drunk singing his heart out" };
            if (c.HostTier >= 1 && Voice.Chance(seed + "dream", 30))
            {
                s.Add(0, "(singing) Beautiful dreamer, wake unto me...", "drunk", "face").Add(0, "(singing) Starlight and dewdrops are waiting for thee...", null, null, 600)
                 .Add(1, "Pretty.").Add(0, "Every time I sing that, it feels like somebody's listening. From very far off.", null, null, 400)
                 .Add(1, "...Sing something else.", "mild");
                return s;
            }
            string[] song = Songs[Voice.Hash(seed + "song") % Songs.Length];
            s.Add(0, "(singing) " + song[0], "drunk", "face").Add(0, "(singing) " + song[1], null, null, 500);
            switch (Voice.Hash(seed + "react") % 6)
            {
                case 0:
                    s.Add(1, "Lord, make it stop.", "mock").Add(0, "Everybody's a critic!", "drunk");
                    break;
                case 1:
                    s.Add(1, "You sing like a gut-shot mule, " + a + ".", "mock").Add(0, "And you dance like one. Another round!", "drunk", "flask");
                    break;
                case 2:
                    s.Add(1, "(singing along) ...dreadful sorry!", "cheer").Add(0, "That ain't even the right song! ...Beautiful, though.", "drunk");
                    break;
                case 3:
                    s.Add(0, "How's the rest of it go? Something about a mule...", "drunk").Add(1, "There ain't a mule in it.", "noidea").Add(0, "There's a mule in every song if you sing it right.");
                    break;
                case 4:
                    s.Add(1, "That's the saddest thing I ever heard, and I've buried two wives.", "mock").Add(0, "Then you'll love the second verse.", "drunk");
                    break;
                default:
                    s.Add(1, "Sit down before you fall down.", "mild").Add(0, "I'll fall down when I'm good and ready!", "drunk", "flask");
                    break;
            }
            return s;
        }

        // ---- tall tales --------------------------------------------------------------------------------

        private static readonly string[] Tales =
        {
            "Winter of '71, it got so cold the words froze coming out of your mouth. Had to thaw 'em over the stove come spring to hear what anybody'd said.",
            "I once rode a catfish clear across the Lannahechee. Big as a mule and twice as ornery.",
            "Knew a man out in New Austin so mean the rattlesnakes moved out of his county.",
            "Lightning hit my rifle once. Every shot after that come out crackling and blue.",
            "Knew a fella so quick he could blow out the lamp and be in bed before the room got dark.",
            "Wrestled a grizzly up in the mountains once. Gentleman's agreement: he kept the fish, I kept my arm.",
            "There was a horse out by Strawberry could count to ten. Couldn't add worth a damn, though.",
            "My pa dug a well so deep he could hear folks in China arguing about the weather.",
            "Seen a twister pick up a whole church out by the Heartlands. Set it down a mile off. Congregation never missed a hymn.",
            "Out in the desert the heat got so bad the corn popped right on the stalk. Mules thought it was snow and froze to death."
        };

        public static Scene TallTale(SceneCast c, string seed)
        {
            string a = N(c, 0, seed), b = N(c, 1, seed);
            Scene s = new Scene { Kind = "talltale", Summary = "an old-timer telling tall tales" };
            if (c.Legend != null && Voice.Chance(seed + "legend", 60))
            {
                string legend = Grammar.SoftLower(c.Legend.TrimEnd('.'));
                s.Add(0, Voice.Pick(seed + "lo", "You hear about that stranger? ", "Let me tell you about a man I heard of. ", "Now this is true, every word. ") + Grammar.UpperFirst(legend) + ".", "news", "face")
                 .Add(1, Voice.Pick(seed + "lr", "Shoot. Folks'll say anything.", "Heard it was twice that many.", "That ain't natural."), "respond")
                 .Add(0, Voice.Pick(seed + "lp", "My cousin's wife's brother seen it.", "Seen it with my own eyes. Well. Near enough.", "God's truth."));
                if (c.PlayerNear)
                    s.Add(1, Voice.Pick(seed + "near", "...Like him? Over there?", "Hey. Don't that fella look like..."), "weird", "lookplayer")
                     .Add(0, Voice.Pick(seed + "np", "Don't point, you fool!", "Hush. Don't look at him."), "scared");
                return s;
            }
            if (c.OldTimer && Voice.Chance(seed + "west", 15))
            {
                s.Add(0, "Some newspaperman back East said 'Go West, young man.'", null, "face").Add(1, "And?")
                 .Add(0, "And I went. Fifty years ago. Still waiting for him to tell me what comes next.").Add(1, "Ha!", "mock");
                return s;
            }
            s.Add(0, Voice.Pick(seed + "tale", Tales), null, c.Saloon ? "face" : "whittle", 400);
            switch (Voice.Hash(seed + "react") % 5)
            {
                case 0: s.Add(1, "And I'm the Queen of England.", "mock").Add(0, "Pleasure to meet you, Your Majesty."); break;
                case 1: s.Add(1, "Last week you told it was a bear.", "mock").Add(0, "It was a bear. A bear AND a catfish. Let me finish."); break;
                case 2: s.Add(1, "No!", "news").Add(0, "Swear on my mother's grave."); break;
                case 3: s.Add(1, "Uh huh. And where's that rifle now?", "mock").Add(0, "Lent it to a fella. Never saw him again. Lightning, probably."); break;
                default: s.Add(1, "...You ever tell the truth, " + a + "?", "mild").Add(0, "Once. Nobody believed me, so I quit."); break;
            }
            return s;
        }

        // ---- the paper, read aloud ---------------------------------------------------------------------

        private static readonly string[] Headlines =
        {
            "the railroad's laying track clear to the Heartlands by spring",
            "a fella in Saint Denis built a carriage that runs without a horse. Runs on lamp oil",
            "there's gold up in the Grizzlies again",
            "the Governor's promising to clean up the territory. Again",
            "the price of beef is down and the price of everything else is up",
            "folks back East are paying a nickel to watch pictures that move",
            "the Pinkertons swear the territory'll be clean by the new century",
            "a woman in Blackwater gave birth to triplets, and all three are already louder than her husband"
        };

        public static Scene ReadPaper(SceneCast c, string seed)
        {
            string a = N(c, 0, seed), b = N(c, 1, seed);
            Scene s = new Scene { Kind = "paper", Summary = "somebody reading the paper aloud" };
            bool real = c.PaperItems.Count > 0 && Voice.Chance(seed + "real", 75);
            if (real)
            {
                string item = c.PaperItems[Voice.Hash(seed + "item") % c.PaperItems.Count].Trim();
                string low = item.ToLowerInvariant();
                s.Add(0, "Listen to this. It says here: \"" + item.TrimEnd('.') + ".\"", "news", "face");
                // "without a shot fired" is good news, and a story from this very town wasn't somewhere else
                bool here = c.Town != null && low.Contains(c.Town.ToLowerInvariant());
                bool violent = (low.Contains("shot") || low.Contains("killed") || low.Contains("dead") || low.Contains("murder")) &&
                               !low.Contains("without a shot") && !low.Contains("not a shot") && !low.Contains("no shot");
                if (violent)
                    s.Add(1, Voice.Pick(seed + "v", "What's this world coming to?", here ? "Right here in town, too." : "Thank God it wasn't here.", "Lord have mercy."), "shame");
                else if (low.Contains("rob") || low.Contains("stole") || low.Contains("theft"))
                    s.Add(1, Voice.Pick(seed + "r", "Serves the bank right.", "Lock your doors tonight.", "Wasn't me, before you ask."), "respond");
                else
                    s.Add(1, Voice.Pick(seed + "g", "Well, I'll be.", "Huh. Some decency left after all.", "Don't believe everything you read."), "respond");
                if (c.PlayerNear && (low.Contains("stranger") || low.Contains("man in a") || low.Contains("tall man") || low.Contains("answering to")))
                    s.Add(1, Voice.Pick(seed + "near", "...Says he wore a hat like that one.", "Hey. Ain't that—"), "weird", "lookplayer")
                     .Add(0, Voice.Pick(seed + "np", "Keep reading. Don't look up.", "Hush."), "scared");
                return s;
            }
            s.Add(0, "Says here " + Voice.Pick(seed + "head", Headlines) + ".", "news", "face");
            s.Add(1, Voice.Pick(seed + "hr", "Huh.", "There's always gold in the Grizzlies, to hear the paper tell it.", "And you believe that?", "Read me the funny pages, " + a + ". I can't stand the news."), "respond");
            if (Voice.Chance(seed + "more", 40))
                s.Add(0, Voice.Pick(seed + "m", "Mark my words, the whole country'll be different by nineteen hundred.", "Says here it'll rain. Says that every week."));
            return s;
        }

        // ---- horse trading -----------------------------------------------------------------------------

        public static Scene Haggle(SceneCast c, string seed)
        {
            string a = N(c, 0, seed), b = N(c, 1, seed);
            string[][] goods =
            {
                new[] { "Finest mare in the county", "She's got one eye.", "Sees twice as good out of it." },
                new[] { "Genuine Lancaster repeater, barely fired", "The stock's held together with twine.", "Twine's the best part. Holds better than walnut." },
                new[] { "Hand-tooled saddle, came all the way from Mexico", "Came by way of a dead man, more like.", "He didn't need it anymore. That's what I call a bargain." },
                new[] { "A map to a lost Spanish mine", "If it's lost, how'd you get a map?", "That's what makes it valuable." },
                new[] { "This here mule", "He bit me twice while you were talking.", "Means he likes you." }
            };
            string[] g = goods[Voice.Hash(seed + "g") % goods.Length];
            int ask = 30 + Voice.Hash(seed + "ask") % 5 * 5;
            Scene s = new Scene { Kind = "haggle", Summary = "two men haggling over a trade" };
            s.Add(0, g[0] + ". " + Grammar.UpperFirst(Voice.Number(ask)) + " dollars.", "greet", "face").Add(1, Grammar.UpperFirst(Voice.Number(ask)) + "? " + g[1], "mock")
             .Add(0, g[2]).Add(1, Grammar.UpperFirst(Voice.Number(ask / 2)) + ".", "no").Add(0, Grammar.UpperFirst(Voice.Number(ask - 5)) + ", and I'm robbing myself.", "mild");
            if (Voice.Chance(seed + "deal", 60))
                s.Add(1, Grammar.UpperFirst(Voice.Number(ask - 10)) + ", and you're robbing me.").Add(0, "...Done. Pleasure doing business.", "yes");
            else
                s.Add(1, "Keep it.", "storm", "walkaway").Add(0, Grammar.UpperFirst(Voice.Number(ask - 10)) + "! " + Grammar.UpperFirst(Voice.Number(ask - 10)) + "! Come back!", "mild");
            return s;
        }

        // ---- the snake-oil man --------------------------------------------------------------------------

        public static Scene Pitch(SceneCast c, string seed)
        {
            string[] tonics = { "Doctor Thaddeus's Miracle Tonic", "Professor Bartholomew's Elixir of Vigor", "Swamp Root Bitters", "Doctor Pettigrew's Lightning Liniment" };
            string tonic = Voice.Pick(seed + "t", tonics);
            Scene s = new Scene { Kind = "pitch", Cast = c.HasCrowd ? 3 : 2, Summary = "a snake-oil man working the crowd" };
            s.Add(0, "Step right up! " + tonic + "! Cures the rheumatism, the gout, the ague and the common lie!", "greet", "face")
             .Add(1, "What's in it?", "respond").Add(0, "Science, friend. Pure distilled science.")
             .Add(1, Voice.Pick(seed + "smell", "Smells like turpentine.", "Smells like my uncle's still.", "There's a dead fly in that one."), "mock")
             .Add(0, Voice.Pick(seed + "back", "That's how you know it's working!", "Extra protein, no charge!", "The finest tonics always smell a little."));
            if (c.HasCrowd)
                s.Add(2, Voice.Pick(seed + "plant", "It cured my mother's warts! And her temper!", "I was dead three days and one sip brought me back!", "I'll take two!"), "cheer");
            s.Add(1, Voice.Chance(seed + "sucker", 50) ? "There's a sucker born every minute." : "...Give me one bottle. For my horse.", Voice.Chance(seed + "sv", 50) ? "mock" : "buy");
            return s;
        }

        // ---- a posse forms -----------------------------------------------------------------------------

        public static Scene Posse(SceneCast c, string seed)
        {
            string a = N(c, 0, seed), b = N(c, 1, seed);
            Scene s = new Scene { Kind = "posse", Summary = "men talking about forming a posse" };
            string crime = Grammar.SoftLower(c.CrimeRumour.TrimEnd('.'));
            s.Add(0, Voice.Pick(seed + "o", "Sheriff's swearing in riders tonight.", "They're getting up a posse.", "Law wants men who can shoot."), "news", "face")
             .Add(1, Voice.Pick(seed + "w", "On account of ", "After ", "Because ") + crime + "?", "respond")
             .Add(0, "That's right." + (c.CrimeDesc != null ? " Witnesses say " + c.CrimeDesc + "." : ""))
             .Add(1, Voice.Pick(seed + "ride", "I'll ride. A man's got to look out for his own.", "Not me. I got a family.", "Posse's just a word for a crowd that'll get shot."),
                Voice.Chance(seed + "rv", 50) ? "yes" : "no");
            if (c.PlayerNear && c.CrimeSeen)
                s.Add(0, Voice.Pick(seed + "look", "...Don't look now.", "Easy. Don't turn around."), "scared", "lookplayer")
                 .Add(1, "Is that—", "weird", "lookplayer").Add(0, "Shut your mouth and walk.", "scared", "walkaway").Add(1, null, null, "walkaway");
            return s;
        }

        // ---- the widow -----------------------------------------------------------------------------------

        private static string JobOf(string label, bool woman)
        {
            string l = label ?? string.Empty;
            string he = woman ? "she" : "he";
            if (l.Contains("labourer") || l.Contains("working woman")) return he + " worked twelve hours a day for the railroad";
            if (l.Contains("farm")) return he + " worked the Downes place, sunup to sundown";
            if (l.Contains("miner")) return he + " went down the mine every morning and came up every night";
            if (l.Contains("dockhand")) return he + " worked the docks";
            if (l.Contains("card player")) return he + " never could leave the cards alone, but " + he + " always came home";
            if (l.Contains("roughneck") || l.Contains("hard-looking")) return he + " was rough, but " + he + " was mine";
            if (l.Contains("trapper")) return he + " ran a trapline up in the hills";
            if (l.Contains("gentleman") || l.Contains("lady")) return he + " was respectable, whatever they say now";
            if (l.Contains("towns")) return he + " kept the books at the feed store";
            if (l.Contains("lawman")) return he + " wore the badge eleven years";
            return he + " never hurt a soul";
        }

        public static Scene Mourn(SceneCast c, string seed)
        {
            string a = N(c, 0, seed), b = N(c, 1, seed);
            string dead = Person.FirstOf(c.DeadName ?? (c.DeadWoman ? "Mary" : "Tom"));
            string rel = c.DeadWoman ? (c.AWoman ? "sister" : "wife") : (c.AWoman ? "husband" : "brother");
            string him = c.DeadWoman ? "her" : "him", he = c.DeadWoman ? "she" : "he";
            Scene s = new Scene { Kind = "mourn", Summary = "someone grieving a " + rel + " who was shot" };
            s.Add(1, "I'm so sorry, " + a + ".", "sorry", "face")
             .Add(0, Grammar.UpperFirst(he) + " went out " + Voice.Pick(seed + "out", "for tobacco", "to see a man about a horse", "for flour", "to collect " + (c.DeadWoman ? "her" : "his") + " wages") +
                     ". Never came back.", null, c.AWoman ? "mourn" : null)
             .Add(0, dead + "... " + JobOf(c.DeadLabel, c.DeadWoman) + ".", null, null, 500)
             .Add(1, "Do they know who did it?", "respond");
            if (c.DeadSeen && c.PlayerNear)
            {
                s.Add(0, "A stranger. Tall. A hat like...", null, "lookplayer", 600)
                 .Add(0, Voice.Pick(seed + "you", "...like his. It's him. I know it's him.", "That's him. Lord help me, that's the one."), "shame", "lookplayer")
                 .Add(1, "Don't, " + a + ". Don't look at him. Come away.", "scared", "walkaway").Add(0, null, null, "walkaway");
                return s;
            }
            s.Add(0, c.DeadSeen ? Voice.Pick(seed + "seen", "A stranger, they say. Rode off like it was nothing.", "Folks saw a stranger. Nobody stopped him.")
                                : Voice.Pick(seed + "unseen", "Nobody saw. Nobody ever sees.", "Sheriff says it was a stranger passing through. As if that helps."));
            if (c.DeadWillReturn && c.HostTier >= 1)
                s.Add(0, "Last night I dreamed " + he + " walked in the door like nothing happened. Hung up " + (c.DeadWoman ? "her" : "his") + " hat. Said 'again'.", "weird", null, 600)
                 .Add(1, "...Let's get you home.", "sorry");
            else
                s.Add(1, Voice.Pick(seed + "c", "Come on. Let's get you home.", "The Lord giveth, " + a + ", and the Lord taketh away.", "We'll see " + him + " buried right."), "sorry");
            return s;
        }

        // ---- small talk ---------------------------------------------------------------------------------

        private static readonly string[][] Small =
        {
            new[] { "Hot one.", "Gonna be hotter tomorrow." },
            new[] { "Rain's coming.", "Good. Ground needs it." },
            new[] { "Working hard?", "Hardly working." },
            new[] { "How's the missus?", "Mean as ever, thank God." },
            new[] { "Stage was late again.", "Stage is always late. Late's its schedule." },
            new[] { "You hear the Hendersons are moving to California?", "Everybody's moving to California. Nobody ever comes back to say how it is." },
            new[] { "That roan of yours is looking fat.", "She's expecting. So's my wife. It's been a long spring." },
            new[] { "Cold for this time of year.", "Every year's cold for this time of year." }
        };

        public static Scene SmallTalk(SceneCast c, string seed)
        {
            Scene s = new Scene { Kind = "smalltalk", Summary = "neighbours passing the time of day" };
            int i = Voice.Hash(seed + "small") % Small.Length;
            string[] x = Small[i];
            bool bad = i == 1 || i == 7;
            s.Add(0, x[0], bad ? "chatbad" : "chat", "face").Add(1, x[1], bad ? "replybad" : "reply");
            if (c.HostTier >= 1 && Voice.Chance(seed + "loop", 35))
            {
                s.Add(0, "...You said that yesterday. Same words.", "weird", null, 500).Add(1, "Did I?", null, null, 900).Add(1, x[1], bad ? "replybad" : "reply")
                 .Add(0, "Never mind. Never mind.", "bye", "walkaway");
                return s;
            }
            if (Voice.Chance(seed + "more", 50))
                s.Add(0, Voice.Pick(seed + "m", "Well. Can't stand here all day.", "Give my best to your folks.", "See you Sunday."), "bye").Add(1, Voice.Pick(seed + "m2", "Take care now.", "Sunday."), "bye");
            return s;
        }

        // ---- the pickpocket ------------------------------------------------------------------------------

        public static Scene Pickpocket(SceneCast c, string seed)
        {
            string b = N(c, 1, seed);
            string what = c.BWoman ? "purse" : Voice.Pick(seed + "w", "wallet", "watch", "billfold");
            Scene s = new Scene { Kind = "pickpocket", Summary = "a pickpocket at work" };
            s.Add(0, null, null, "approach", 200)
             .Add(0, Voice.Pick(seed + "o", "Pardon me, friend. Ain't you Mabel's cousin?", "Beg pardon. Thought you were somebody I knew.", "Lovely day, ain't it?"), "greet")
             .Add(1, Voice.Pick(seed + "r", "Don't believe so.", "Can't say I am.", "Mm."), "no")
             .Add(0, "My mistake.", null, "run")
             .Add(0, null, null, "cb:thief")
             .Add(1, "Hey! My " + what + "! Thief! THIEF!", "thief", null, 200)
             .Add(1, "Stop him, mister! He's got everything I own!", "scared", "faceplayer");
            return s;
        }

        // ---- the player steps in ------------------------------------------------------------------------

        /// The player greeted (friendly) or antagonized one of the cast (who) in a running scene. Returns the
        /// beats that replace what was left of it. outcome: "peace" (he stopped a fight or a standoff),
        /// "egged" (he egged them on), "sided" (he took a side), or null.
        public static Scene Intrude(string kind, int who, bool friendly, SceneCast c, bool fighting, string seed, out string outcome)
        {
            outcome = null;
            int other = who == 0 ? 1 : 0;
            string a = N(c, 0, seed), b = N(c, 1, seed);
            string nameOf = who == 0 ? a : b, otherName = who == 0 ? b : a;
            Scene s = new Scene { Kind = kind, Cast = c.HasCrowd ? 3 : 2 };
            switch (kind)
            {
                case "argue":
                case "debt":
                case "haggle":
                case "cheat":
                {
                    if (who > 1)
                    {
                        s.Add(0, friendly ? "Alright, alright. We'll settle it later." : "Who asked you?", friendly ? "defuse" : "angry").Add(0, null, null, "walkaway").Add(1, null, null, "walkaway");
                        return s;
                    }
                    int w = friendly ? who : other, l = 1 - w;
                    string wn = w == 0 ? a : b, ln = l == 0 ? a : b;
                    outcome = "sided";
                    s.Add(w, Voice.Pick(seed + "w", "Hear that, " + ln + "? Even the stranger knows I'm right.", "There. A man of sense. Pay up, " + ln + ".", "See? Ask anybody."), "won", "faceplayer")
                     .Add(l, Voice.Pick(seed + "l", "Nobody asked you, mister.", "Stay out of it, stranger.", "You don't know the half of it."), "angry", "faceplayer")
                     .Add(l, Voice.Pick(seed + "l2", "...This ain't over, " + wn + ".", "Fine. FINE.", "Two against one. Real brave."), "storm", "walkaway");
                    return s;
                }
                case "brawl":
                    if (friendly)
                    {
                        outcome = "peace";
                        if (fighting)
                            s.Add(0, null, null, "endfight");
                        s.Add(0, Voice.Pick(seed + "p0", "Alright! Alright. We're done.", "...Fine. He ain't worth it anyhow."), "defuse")
                         .Add(1, Voice.Pick(seed + "p1", "Lucky for you this fella stepped in.", "Next time there won't be nobody to save you."), "mild", "walkaway").Add(0, null, null, "walkaway");
                    }
                    else
                    {
                        outcome = "egged";
                        s.Add(who > 1 ? 0 : who, Voice.Pick(seed + "e", "You want some too, stranger?!", "Stay out of it or you're next!"), "fight", "faceplayer");
                        if (c.HasCrowd)
                            s.Add(2, "Fight! Fight!", "cheer");
                        if (!fighting)
                            s.Add(0, null, "angry", "fight", 8000).Add(0, null, null, "endfight").Add(0, "Stay down.", "won").Add(1, "...Lucky punch.", "mild", "walkaway");
                    }
                    return s;
                case "standoff":
                    if (friendly)
                    {
                        outcome = "peace";
                        s.Add(0, null, null, "holster").Add(1, null, null, "holster")
                         .Add(other, Voice.Pick(seed + "p", "...Lucky for you this fella came along, " + nameOf + ".", "Another day, then."), "defuse")
                         .Add(who, Voice.Pick(seed + "q", "Lucky for you.", "Another day."), "mild").Add(0, null, null, "walkaway").Add(1, null, null, "walkaway");
                    }
                    else
                    {
                        outcome = "egged";
                        s.Add(0, "You heard him. Let's settle it.", "fight", "holster").Add(1, null, null, "holster").Add(1, "Gladly.", "fight", "fight", 9000)
                         .Add(0, null, null, "endfight").Add(0, "Stay down.", "won").Add(1, "...This ain't over.", "mild", "walkaway");
                        if (c.HasCrowd)
                            s.Beats.Insert(2, new Beat(2, "Here we go!", "cheer"));
                    }
                    return s;
                case "flirt":
                case "propose":
                {
                    int man = c.AWoman ? 1 : 0, woman = 1 - man;
                    string he = man == 0 ? a : b;
                    if (friendly)
                        s.Add(man, "Do you mind? We're having a moment here.", "mild", "faceplayer").Add(woman, "Don't be rude, " + he + ". ...Afternoon, mister.", "flatter");
                    else
                        s.Add(man, "You got something to say to the lady?", "threat", "faceplayer").Add(woman, "Leave it, " + he + ". He ain't worth it.", "mild", "walkaway").Add(man, null, null, "walkaway");
                    return s;
                }
                case "jealous":
                    s.Add(2, "This ain't your business, stranger!", "angry", "faceplayer").Add(0, "...I was just leaving.", "scared", "walkaway");
                    return s;
                case "gossip":
                case "paper":
                case "talltale":
                case "smalltalk":
                    if (friendly)
                    {
                        s.Add(who > 1 ? 0 : who, Voice.Pick(seed + "f", "Afternoon. You hear the news?", "Care to join us? We don't bite.", "Pull up, stranger. We were just jawing."), "greet", "faceplayer");
                        if (c.Legend != null && Voice.Chance(seed + "legend", 60))
                            s.Add(who > 1 ? 1 : other, "...Say. You ain't him, are you? The one they talk about?", "weird", "lookplayer")
                             .Add(who > 1 ? 0 : who, "Don't be stupid. Beg pardon, mister.", "scared");
                    }
                    else
                    {
                        s.Add(who > 1 ? 0 : who, Voice.Pick(seed + "h", "Mind your own business.", "Nobody's talking to you.", "Move along."), "mild", "faceplayer").Add(0, null, null, "walkaway").Add(1, null, null, "walkaway");
                    }
                    return s;
                case "song":
                    if (friendly)
                        s.Add(0, "Sing with me, friend! ...No? Nobody ever sings with me.", "drunk", "faceplayer");
                    else
                        s.Add(0, "Everybody's a critic!", "mock", "faceplayer").Add(1, "He's got a point, " + a + ".", "mock");
                    return s;
                case "pitch":
                    if (friendly)
                        s.Add(0, "A discerning gentleman! For you, one dollar, not two!", "buy", "faceplayer").Add(1, "He's got two dollars' worth of sense, then.", "mock");
                    else
                        s.Add(0, "Sir, you are frightening the customers.", "mild", "faceplayer").Add(1, "Ha! He's right, though.", "mock");
                    return s;
                case "posse":
                    if (friendly)
                        s.Add(0, "...Evening.", "scared", "faceplayer").Add(0, null, null, "walkaway").Add(1, null, null, "walkaway");
                    else
                        s.Add(0, null, "scared", "flee").Add(1, null, "scared", "flee");
                    return s;
                case "mourn":
                {
                    string dead = Person.FirstOf(c.DeadName ?? "him");
                    if (friendly && !(c.DeadSeen && c.PlayerNear))
                        s.Add(0, "Did you know him? " + dead + "? ...He was a good man. Don't let anybody tell you different.", "sorry", "faceplayer").Add(1, "Come on, " + a + ". Let's get you home.", "sorry", "walkaway").Add(0, null, null, "walkaway");
                    else if (friendly)
                        s.Add(0, "You. You have the nerve to speak to me.", "shame", "faceplayer").Add(1, "Come away, " + a + ". Come away.", "scared", "walkaway").Add(0, null, null, "walkaway");
                    else
                        s.Add(0, "Haven't you done enough?", "shame", "faceplayer").Add(0, null, null, "walkaway").Add(1, null, null, "walkaway");
                    return s;
                }
                case "hosts":
                    if (friendly)
                        s.Add(0, Voice.Pick(seed + "g", "You. You're not like us, are you? You can leave whenever you want.", "You hear us, don't you? Really hear us. Nobody else does."), "weird", "faceplayer")
                         .Add(1, "Don't. They don't like it when we notice.", "scared");
                    else
                        s.Add(1, "He's one of the ones who come and go. Don't look at him.", "scared", "walkaway").Add(0, null, null, "walkaway");
                    return s;
                default:
                    s.Add(who > 1 ? 0 : who, friendly ? "Afternoon." : "Mind your business.", friendly ? "greet" : "mild", "faceplayer");
                    return s;
            }
        }
    }
}
