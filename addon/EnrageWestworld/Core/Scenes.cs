// Little plays between strangers near the player: a flirt, a quarrel, a brawl, a proposal, a jealous
// husband, a cheat at cards, a debt called in, gossip, two people wondering about their world, a eulogy,
// a wedding - and (Scenes.Frontier.cs) standoffs, drunk singing, tall tales, the paper read aloud, horse
// trading, a snake-oil pitch, a posse forming, a widow, small talk and a pickpocket. Each beat is one line
// (and/or an action) for one of the cast; the game layer stages it, and the player can step into it.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public sealed class Beat
    {
        public int Who;          // 0 = A, 1 = B, 2 = C
        public string Line;      // subtitle, may be null for a pure action
        public string Voice;     // the game's voiced line set (see Speech.Set): greet, angry, mild, scared, cheer, mock, plead...
        public string Act;       // face, approach, shove, fight, endfight, walkaway, flee, kneel, stand, mourn, aim, holster,
                                 // drunk, flask, smoke, run, faceplayer, lookplayer, whittle, stoic, cb:<callback>
        public int Ms;           // extra pause after the beat
        public int To = -1;      // who it's said to or done at (-1: the other of A and B, or A for C)

        public Beat(int who, string line, string voice = null, string act = null, int ms = 0)
        {
            Who = who;
            Line = line;
            Voice = voice;
            Act = act;
            Ms = ms;
        }
    }

    public sealed class Scene
    {
        public string Kind;
        public int Cast = 2;
        public bool Fight;
        public string Summary;   // for gossip afterwards: "two men came to blows"
        public bool NoteAtCue;   // its tie and memories wait for the scene's own cue (a cb: act), not its start
        public readonly List<Beat> Beats = new List<Beat>();

        public Scene Add(int who, string line, string voice = null, string act = null, int ms = 0)
        {
            Beats.Add(new Beat(who, line, voice, act, ms));
            return this;
        }
    }

    public sealed class SceneCast
    {
        public bool AWoman, BWoman, CWoman;
        public string Town;
        public bool Saloon;
        public bool Night;
        public int HostTier;     // how strange the town feels (0-2)
        public List<string> Rumours = new List<string>();   // third-person rumours that can be gossiped
        public bool HasCrowd;                               // a third bystander (index 2) to heckle, cheer or call the law
        public bool OldTimer;                               // A looks old enough to tell tall tales
        public bool Armed0, Armed1;                         // carrying guns: a standoff draws them
        public string Legend;                               // the player's legend as this town tells it, if he has one
        public bool PlayerNear;                             // the player is close enough to be noticed
        public List<string> PaperItems = new List<string>(); // the latest small items from the papers
        public string CrimeRumour;                          // a crime here lately, third person
        public string CrimeDesc;                            // how the witnesses describe whoever did it
        public bool CrimeSeen;                              // somebody saw the culprit's face
        public string DeadName, DeadLabel;                  // a stranger the player killed here, for the widow
        public bool DeadWoman, DeadSeen, DeadWillReturn;
        public HashSet<string> Off = new HashSet<string>(); // kinds switched off in the ini
        public string[] Names = new string[3];              // the names A, B and C really go by (enRAGE's, or a resident's own); null: made up
        public string[] Tempers = new string[3];            // A, B and C's temperaments (Reactions.Listener); null: none known
    }

    public static partial class Scenes
    {
        private static readonly string[] Men = { "Hank", "Silas", "Amos", "Clem", "Jed", "Earl", "Walt", "Otis", "Lyle", "Abner", "Virgil", "Rufus", "Elmer", "Cyrus", "Ezra", "Wade", "Floyd", "Homer" };
        private static readonly string[] Women = { "Martha", "Ada", "Clara", "Ruth", "Hattie", "Lottie", "Mabel", "Nell", "Pearl", "Ida", "Etta", "Della", "Josie", "Minnie", "Opal", "Cora" };

        public static string FirstName(bool woman, string seed)
        {
            return woman ? Voice.Pick(seed + "fw", Women) : Voice.Pick(seed + "fm", Men);
        }

        /// Chooses a scene that fits the cast and the place. Null when nothing fits.
        public static Scene Choose(SceneCast c, string seed, bool allowFight)
        {
            List<KeyValuePair<string, int>> w = new List<KeyValuePair<string, int>>();
            bool mixed = c.AWoman != c.BWoman;
            bool twoMen = !c.AWoman && !c.BWoman;
            if (mixed)
                w.Add(new KeyValuePair<string, int>("flirt", c.Saloon ? 14 : 18));
            w.Add(new KeyValuePair<string, int>("argue", 16));
            if (twoMen && allowFight)
                w.Add(new KeyValuePair<string, int>("brawl", c.Saloon ? 10 : 5));
            if (mixed && !c.Night)
                w.Add(new KeyValuePair<string, int>("propose", 3));
            if (c.Saloon && twoMen)
                w.Add(new KeyValuePair<string, int>("cheat", 10));
            w.Add(new KeyValuePair<string, int>("debt", 7));
            if (c.Rumours.Count > 0)
                w.Add(new KeyValuePair<string, int>("gossip", c.Saloon ? 30 : 18));
            w.Add(new KeyValuePair<string, int>("hosts", c.HostTier >= 2 ? 16 : c.HostTier == 1 ? 9 : c.Night ? 6 : 3));
            FrontierWeights(c, w, twoMen, allowFight);
            w.RemoveAll(x => c.Off.Contains(x.Key));
            if (w.Count == 0)
                return null;
            int total = w.Sum(x => x.Value);
            int roll = Voice.Hash(seed + "kind") % Math.Max(1, total);
            string kind = w[w.Count - 1].Key;
            foreach (KeyValuePair<string, int> x in w)
            {
                if (roll < x.Value)
                {
                    kind = x.Key;
                    break;
                }
                roll -= x.Value;
            }
            return Make(kind, c, seed);
        }

        public static Scene Make(string kind, SceneCast c, string seed)
        {
            switch (kind)
            {
                case "flirt": return Flirt(c, seed);
                case "argue": return Argue(c, seed);
                case "brawl": return Brawl(c, seed);
                case "propose": return Propose(c, seed);
                case "cheat": return Cheat(c, seed);
                case "debt": return Debt(c, seed);
                case "gossip": return c.Rumours.Count > 0 ? Gossip(c, seed) : Argue(c, seed);
                case "hosts": return HostTalk(c, seed);
                case "jealous": return Jealous(c, seed);
                default: return MakeFrontier(kind, c, seed);
            }
        }

        // the man is A unless A is the woman of a mixed pair. The name they really go by, when they have one, so the
        // lines say the same name as the label over them; else one made up from the seed (as the director does)
        internal static string N(SceneCast c, int who, string seed)
        {
            string real = c.Names != null && who < c.Names.Length ? c.Names[who] : null;
            if (!string.IsNullOrEmpty(real))
                return real;
            bool woman = who == 0 ? c.AWoman : who == 1 ? c.BWoman : c.CWoman;
            return FirstName(woman, seed + who);
        }

        public static Scene Flirt(SceneCast c, string seed)
        {
            int man = c.AWoman ? 1 : 0, woman = 1 - man;
            string he = N(c, man, seed), she = N(c, woman, seed);
            Scene s = new Scene { Kind = "flirt", Summary = "a couple courting in the street" };
            switch (Voice.Hash(seed) % 5)
            {
                case 0:
                    s.Add(man, "Evening, Miss. You look like trouble.", "greet", "face").Add(woman, "And you look like a man who'd enjoy some.")
                     .Add(man, "Walk with me a spell?").Add(woman, "Maybe. If you behave.", "yes");
                    break;
                case 1:
                    s.Add(woman, "You've been staring, " + he + ".", null, "face").Add(man, "Can't help it. You're the prettiest thing in " + (c.Town ?? "this county") + ".")
                     .Add(woman, "That ain't saying much.").Add(man, "Then let me say it somewhere nicer.").Add(woman, "...Supper. You're paying.", "yes");
                    break;
                case 2:
                    s.Add(man, "I picked these for you, " + she + ".", "greet", "face").Add(woman, "Weeds, " + he + ". You picked me weeds.")
                     .Add(man, "Pretty weeds.").Add(woman, "...They are, at that.");
                    break;
                case 3:
                    s.Add(woman, c.Saloon ? "Buy a lady a drink?" : "Carry these for a lady?", null, "face").Add(man, c.Saloon ? "I'll buy her two, if she laughs at my jokes." : "Carry 'em to the end of the world, ma'am.")
                     .Add(woman, c.Saloon ? "Depends on the jokes." : "Just to the end of the street will do.").Add(man, "A start's a start.", "yes");
                    break;
                default:
                    s.Add(man, "Still mad at me, " + she + "?", null, "face").Add(woman, "Furious.").Add(man, "Furious enough to dance with me Saturday?")
                     .Add(woman, "...Wear a clean shirt this time.", "yes");
                    break;
            }
            return s;
        }

        public static Scene Argue(SceneCast c, string seed)
        {
            string a = N(c, 0, seed), b = N(c, 1, seed);
            Scene s = new Scene { Kind = "argue", Summary = "a shouting match in the street" };
            switch (Voice.Hash(seed) % 6)
            {
                case 0:
                    s.Add(0, "You owe me an apology, " + b + ".", "mild", "face").Add(1, "I owe you nothing.").Add(0, "You told the whole street my business!", "angry")
                     .Add(1, "Then keep your business quieter!", "angry").Add(0, "One of these days...").Add(1, "One of these days what?", "stare").Add(0, null, null, "walkaway");
                    break;
                case 1:
                    s.Add(0, "Where's my two dollars, " + b + "?", "mild", "face").Add(1, "I said Friday.").Add(0, "It IS Friday!", "angry").Add(1, "...Next Friday.")
                     .Add(0, "You're a weasel, " + b + ". A weasel in a hat.", "angry").Add(1, null, null, "walkaway");
                    break;
                case 2:
                    s.Add(0, "I saw you with my " + (c.BWoman ? "husband" : (c.AWoman ? "husband" : "wife")) + ".", "angry", "face").Add(1, "We were talking.")
                     .Add(0, "Talk with your hands a lot, do you?", "angry").Add(1, "Ask " + (c.AWoman ? "him" : "her") + " yourself, if you're so sure.").Add(0, "I will!", "angry", "walkaway");
                    break;
                case 3:
                    s.Add(0, "Your dog dug up my whole garden.", "mild", "face").Add(1, "Ain't my dog.").Add(0, "It follows you home every night!")
                     .Add(1, "Lots of things follow me home. Don't make 'em mine.", "no").Add(0, "Insufferable.", "angry", "walkaway");
                    break;
                case 4:
                    s.Add(0, "You said this yesterday.", null, "face").Add(1, "I never said it before in my life.").Add(0, "Word for word, " + b + "! Same words!", "angry")
                     .Add(1, "...You feeling all right?", "mild").Add(0, "No. No, I don't think I am.", null, "walkaway");
                    break;
                default:
                    s.Add(0, "That's my spot.", "mild", "face").Add(1, "Don't see your name on it.").Add(0, "You'll see my boot on you in a minute.", "angry")
                     .Add(1, "Try it.", "threat").Add(0, "...Not worth it.", "mild", "walkaway");
                    break;
            }
            return s;
        }

        public static Scene Brawl(SceneCast c, string seed)
        {
            string a = N(c, 0, seed), b = N(c, 1, seed);
            Scene s = new Scene { Kind = "brawl", Fight = true, Summary = "two men came to blows" };
            switch (Voice.Hash(seed) % 3)
            {
                case 0:
                    s.Add(0, "Say that again, " + b + ". I dare you.", "angry", "face").Add(1, "I said your mother was a mule and your father knew it.", "angry")
                     .Add(0, null, "angry", "fight", 9000);
                    break;
                case 1:
                    s.Add(0, "You've been cheating me on the weights all year.", "angry", "face").Add(1, "Prove it.").Add(0, "I'll prove it on your face!", "angry", "fight", 9000);
                    break;
                default:
                    s.Add(1, "Get out of my way.", "mild", "face").Add(0, "Make me.", "threat").Add(1, "Gladly.", "angry", "fight", 9000);
                    break;
            }
            s.Add(0, null, null, "endfight").Add(0, Voice.Pick(seed + "w", "Stay down.", "That's for the weights.", "Next time I won't be so gentle."), "won")
             .Add(1, Voice.Pick(seed + "l", "Alright! Alright...", "This ain't over.", "Lucky punch."), "mild", "walkaway");
            return s;
        }

        public static Scene Propose(SceneCast c, string seed)
        {
            int man = c.AWoman ? 1 : 0, woman = 1 - man;
            string he = N(c, man, seed), she = N(c, woman, seed);
            Scene s = new Scene { Kind = "propose", Summary = "a man asked a woman to marry him, right there in the street" };
            s.Add(man, she + "... I been carrying this ring around for a month.", null, "face").Add(man, null, null, "kneel", 1500)
             .Add(woman, he + ", get up, people are looking!").Add(man, "Let 'em look. Will you marry me?");
            if (Voice.Chance(seed + "yes", 75))
                s.Add(woman, "Yes. Yes, you fool!", "yes").Add(man, null, null, "stand").Add(man, "She said yes! You hear that? She said yes!", "won");
            else
                s.Add(woman, "Oh, " + he + ". I can't. I'm sorry.", "no", "walkaway").Add(man, null, null, "stand").Add(man, "...Right. Right.", null, "walkaway");
            return s;
        }

        public static Scene Jealous(SceneCast c, string seed)
        {
            // A courts B; C (B's partner) walks in
            string a = N(c, 0, seed), b = N(c, 1, seed), x = N(c, 2, seed);
            Scene s = new Scene { Kind = "jealous", Cast = 3, Summary = "a jealous " + (c.CWoman ? "wife" : "husband") + " caught somebody sweet-talking" };
            s.Add(0, "You've got the loveliest laugh in " + (c.Town ?? "the county") + ", " + b + ".", null, "face").Add(1, "Oh, stop.")
             .Add(2, "Get away from " + (c.BWoman ? "her" : "him") + ".", "angry", "approach").Add(0, "Who's asking?").Add(2, (c.BWoman ? "Her husband" : "His wife") + ".", "threat")
             .Add(1, x + ", it ain't what it looks like!", "scared").Add(2, null, null, "shove").Add(0, "...I was just leaving.", "scared", "walkaway");
            return s;
        }

        public static Scene Cheat(SceneCast c, string seed)
        {
            string a = N(c, 0, seed), b = N(c, 1, seed);
            Scene s = new Scene { Kind = "cheat", Summary = "a card cheat was called out" };
            s.Add(0, "That's the second ace of spades I've seen tonight, friend.", "mild", "face").Add(1, "Deck's got four aces.").Add(0, "Not up your sleeve, it don't.", "angry")
             .Add(1, "You calling me a cheat?", "threat").Add(0, "I'm calling you a liar, too.", "angry");
            if (!c.AWoman && !c.BWoman && Voice.Chance(seed + "f", 40))
            {
                s.Fight = true;
                s.Add(1, null, "angry", "fight", 8000).Add(0, null, null, "endfight").Add(0, "Keep your winnings. Keep your teeth, too, while you can.", "won", "walkaway");
            }
            else if (c.Saloon && Voice.Chance(seed + "out", 50))
            {
                // thrown out (Tier 4): in a saloon, half the time it's the cheat who goes, told to by the man he cheated
                s.Add(0, Voice.Pick(seed + "o1", "Out. And leave the pot you never won.", "Get out of this saloon. Now. Before the rest of the table hears of it.",
                                    "Walk out that door, " + b + ", or be carried."), "angry")
                 .Add(1, Voice.Pick(seed + "o2", "...Keep it. Crooked table anyhow.", "Fine. I know when I'm not wanted.", "This town's got no sense of humour."), "mild", "walkaway");
                s.Summary = "a card cheat was shown the door";
            }
            else
            {
                s.Add(1, "...Take your money and get out of my sight.", "mild").Add(0, "Gladly.", null, "walkaway");
            }
            return s;
        }

        public static Scene Debt(SceneCast c, string seed)
        {
            string a = N(c, 0, seed), b = N(c, 1, seed);
            int amount = 10 + Voice.Hash(seed + "amt") % 8 * 5;
            Scene s = new Scene { Kind = "debt", Summary = "a man called in a debt of " + amount + " dollars" };
            s.Add(0, b + ". You know why I'm here.", "mild", "approach").Add(1, "I need more time.", "scared").Add(0, "You had time. " + Grammar.UpperFirst(Voice.Number(amount)) + " dollars, " + b + ".")
             .Add(1, "I got five. Take five.", "scared");
            switch (Voice.Hash(seed) % 3)
            {
                case 0:
                    s.Add(0, "Five now. The rest by Sunday. Or I take the mule.", "threat").Add(1, "...Sunday.", "mild").Add(0, null, null, "walkaway");
                    break;
                case 1:
                    s.Add(0, "Five dollars don't cover the interest.", "angry").Add(0, null, null, "shove").Add(1, "Alright! Sunday! I swear on my mother!", "scared");
                    break;
                default:
                    s.Add(0, "...Keep it. Buy your kids some shoes. But this is the last time, " + b + ".", "mild", "walkaway").Add(1, "God bless you. God bless you.", "thanks");
                    break;
            }
            return s;
        }

        public static Scene Gossip(SceneCast c, string seed)
        {
            string rumour = c.Rumours[Voice.Hash(seed + "r") % c.Rumours.Count];
            List<string> lines = Deeds.Gossip(rumour, c.Town, seed);
            Scene s = new Scene { Kind = "gossip", Summary = "town gossip" };
            for (int i = 0; i < lines.Count; i++)
                s.Add(i % 2, lines[i], i == 0 ? null : (i == 1 ? "yes" : null), i == 0 ? "face" : null);
            return s;
        }

        public static Scene HostTalk(SceneCast c, string seed)
        {
            Scene s = new Scene { Kind = "hosts", Summary = "two people talking strangely about the world" };
            bool first = true;
            foreach (KeyValuePair<int, string> l in Hosts.Conversation(seed))
            {
                s.Add(l.Key, l.Value, null, first ? "face" : null, 500);
                first = false;
            }
            return s;
        }

        // ---- ceremonies -------------------------------------------------------------------------------

        public static List<KeyValuePair<string, string>> Vows(Wedding w, string seed)
        {
            string a = Person.FirstOf(w.AName), b = Person.FirstOf(w.BName);
            string partnerA = w.AWoman ? "wife" : "husband", partnerB = w.BWoman ? "wife" : "husband";
            return new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string, string>("Preacher", "Dearly beloved, we are gathered here in the sight of God to join this man and this woman.".Replace("this man and this woman", w.AWoman == w.BWoman ? "these two souls" : "this man and this woman")),
                new KeyValuePair<string, string>("Preacher", "Do you, " + a + ", take " + b + " to be your lawful wedded " + partnerB + "?"),
                new KeyValuePair<string, string>(w.AName, Voice.Pick(seed + "a", "I do.", "I do. Lord help me, I do.", "I surely do.")),
                new KeyValuePair<string, string>("Preacher", "And do you, " + b + ", take " + a + " to be your lawful wedded " + partnerA + "?"),
                new KeyValuePair<string, string>(w.BName, Voice.Pick(seed + "b", "I do.", "...I do.", "I do, and about time.")),
                new KeyValuePair<string, string>("Preacher", "Then by the power vested in me, I pronounce you joined. You may kiss."),
            };
        }

        public static List<string> Eulogy(string deadName, Person dead, bool killedByPlayer, bool playerHere, string seed)
        {
            string first = Person.FirstOf(deadName ?? "him");
            bool woman = dead != null && dead.Woman;
            string he = woman ? "she" : "he", his = woman ? "her" : "his", him = woman ? "her" : "him";
            List<string> lines = new List<string>();
            lines.Add(Voice.Pick(seed + "1", first + " wasn't a good " + (woman ? "woman" : "man") + ". But " + he + " was ours.",
                "Ashes to ashes. The rest is up to God.", "Rest easy, " + first + ". Nobody can hurt you now."));
            PastItem past = dead != null ? dead.Past.Where(p => p.Public && !string.IsNullOrEmpty(p.Text) && p.Kind != "born" && Grammar.StartsWithPastVerb(p.Text))
                .OrderBy(p => Voice.Hash(seed + p.Text)).FirstOrDefault() : null;
            if (past != null)
            {
                string deed = Grammar.LowerFirst(past.Text.Trim().TrimEnd('.'));
                lines.Add(Voice.Pick(seed + "2", "I'll always remember the time " + he + " " + deed + ". That was " + first + ".",
                    "Folks forget " + he + " was young once. " + Grammar.UpperFirst(he) + " " + deed + ", you know."));
            }
            else
            {
                lines.Add(Voice.Pick(seed + "2", first + " taught me to shoot. Said everybody ought to know how.", "Never once saw " + him + " turn down a friend in need."));
            }
            if (killedByPlayer && playerHere)
                lines.Add(Voice.Pick(seed + "3", "...That's him. Over there. The one who did it.", "Have you no shame, coming here?", "Look who came to watch us bury " + him + "."));
            else if (killedByPlayer)
                lines.Add("Whoever did this... I'll find " + Voice.Pick(seed + "4", "him", "them") + ". On " + first + "'s grave, I swear it.");
            else
                lines.Add(Voice.Pick(seed + "5", "Goodbye, " + first + ".", "Save " + his + " seat for me, Lord. I won't be long."));
            return lines;
        }
    }
}
