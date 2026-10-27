// An execution on the road (2026-10-26, Lex: more NPC-on-NPC scenes like the game's own, where two men were about to
// execute Zeke Townsend, but with the people RealNPCs keeps). One of a nearby town's regulars kneels at gunpoint out on
// a road, for a reason from his own life: the worry he's living through (InnerLife.WorryNow) names it, and the same
// name and sum his thoughts use are the ones the men say (InnerLife.WorryFill, the eleven dollars), so what he thinks,
// what they say and what happens agree. Who holds the guns (Lex: all of these): for a debt, the creditor himself when
// he's a real person of the town, with a hired man, else two hired men he sent; for a feud, the other man and his
// brother; for talking to the law, the region's outlaws; with no worry that fits, outlaws who caught him seeing what
// he shouldn't. Ride on and they go through with it. All the words are RealNPCs' own.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Executions
    {
        public sealed class Plan
        {
            public string Reason;     // debt, feud, informer, witness
            public string Boss;       // the creditor, the other family's name, or the gang
            public string BossId;     // a real person behind it (a regular), when there is one
            public string Captors;    // boss (him and a hired man), hired, kin, outlaws
            public string Gang;       // the region's outlaws, for informer and witness
            public string GangKey;    // odriscolls, raiders, murfrees: the models (the game side)
        }

        /// The plan for him: worry is his worry kind today (or null), who its other person as his thoughts name them,
        /// whoId that person's id when they're real, region the town or region it happens in.
        public static Plan For(string worry, string who, string whoId, string region, string seed)
        {
            string key = GangOf(region);
            string gang = key == "raiders" ? "the Lemoyne Raiders" : key == "murfrees" ? "the Murfree Brood" : "the O'Driscolls";
            switch (worry)
            {
                case "debt":
                    return new Plan { Reason = "debt", Boss = who ?? Residents.Surname(seed + "b"), BossId = whoId, Captors = whoId != null ? "boss" : "hired" };
                case "feud":
                    return new Plan { Reason = "feud", Boss = who ?? Residents.Surname(seed + "b"), BossId = whoId, Captors = "kin" };
                case "law":
                case "secret":
                    return new Plan { Reason = "informer", Boss = gang, Captors = "outlaws", Gang = gang, GangKey = key };
                default:
                    return new Plan { Reason = "witness", Boss = gang, Captors = "outlaws", Gang = gang, GangKey = key };
            }
        }

        private static readonly string[] Lemoyne = { "Saint Denis", "Rhodes", "Lagras", "Braithwaite", "Bayou Nwa", "Scarlett Meadows", "Bluewater Marsh", "Lemoyne", "Caliga" };
        private static readonly string[] Roanoke = { "Annesburg", "Van Horn", "Roanoke", "Butcher Creek", "Brandywine" };

        /// Whose outlaws ride where: the Lemoyne Raiders in Lemoyne, the Murfree Brood in Roanoke, the O'Driscolls elsewhere.
        public static string GangOf(string region)
        {
            string r = region ?? "";
            foreach (string s in Lemoyne)
                if (r.Contains(s))
                    return "raiders";
            foreach (string s in Roanoke)
                if (r.Contains(s))
                    return "murfrees";
            return "odriscolls";
        }

        /// The game's models for each, the ones enRAGE spawns for those outfits.
        public static string[] GangModels(string key)
        {
            switch (key)
            {
                case "raiders": return new[] { "g_m_y_uniexconfeds_01", "g_m_y_uniexconfeds_02", "g_m_o_uniexconfeds_01" };
                case "murfrees": return new[] { "g_m_m_unimountainmen_01" };
                default: return new[] { "g_m_m_uniduster_01", "g_m_m_uniduster_02", "g_m_m_uniduster_03" };
            }
        }

        /// What the men call the one they're holding to the player: "between him and Mr. Yates".
        public static string BossWord(Plan p)
        {
            return p.Reason == "debt" ? "Mr. " + p.Boss : p.Reason == "feud" ? "the " + p.Boss + " family" : p.Gang;
        }

        // the men to him, overheard
        public static string Threat(Plan p, string seed)
        {
            switch (p.Reason)
            {
                case "debt":
                    return Fill(Voice.Pick(seed + "et", "{boss} said you'd beg. He said you'd promise. Go on, then.", "Eleven dollars. A man dying for eleven dollars.",
                        "Mr. {boss} gave you all spring. Spring's over.", "You don't pay, you don't stand. That's how it works."), p);
                case "feud":
                    return Fill(Voice.Pick(seed + "et", "You moved that fence post one time too many.", "Should've let the dog be. Should've let all of it be.",
                        "My family's put up with you twenty years. No more.", "Say you're sorry. Go on. It won't help."), p);
                case "informer":
                    return Fill(Voice.Pick(seed + "et", "Somebody talked to the law. Somebody sweating, just like you.", "{gang} don't forget a loose mouth.",
                        "You told the sheriff where we sleep. Now you'll sleep."), p);
                default:
                    return Fill(Voice.Pick(seed + "et", "You saw what you saw. That's all there is to it.", "Wrong road, wrong hour, friend.",
                        "Nothing personal. You just got eyes."), p);
            }
        }

        // him to them, overheard
        public static string Plead(Plan p, string seed)
        {
            switch (p.Reason)
            {
                case "debt":
                    return Fill(Voice.Pick(seed + "ep", "I'll have it Saturday! Saturday, I promise you!", "Tell {boss} I'll work it off. Any work at all. Please.",
                        "It's eleven dollars! It's only eleven dollars!"), p);
                case "feud":
                    return Voice.Pick(seed + "ep", "It was never about the fence! Please!", "I'll sell the place and leave! I'll leave tonight!", "Think of my children, for God's sake.");
                case "informer":
                    return Voice.Pick(seed + "ep", "I never said a word! Not one word!", "They had me in a cell! What was I to do?", "There's a wife waiting on me. Please. A wife and two little ones.");
                default:
                    return Voice.Pick(seed + "ep", "I never saw a thing! My eyes were shut the whole time!", "I'll go east. You'll never hear of me again.", "Please. I'm nobody. I'm nobody at all.");
            }
        }

        // one of the men to the player, as he comes near: said to him, so it carries the reason to him
        public static string WarnYou(Plan p, string seed)
        {
            switch (p.Reason)
            {
                case "debt":
                    return Fill(Voice.Pick(seed + "ew", "Ride on, mister. This is between him and {bossword}.", "Keep moving. This is debt business, and it's paid today."), p);
                case "feud":
                    return Voice.Pick(seed + "ew", "Family matter, stranger. Keep riding.", "This ain't yours. Turn around.");
                default:
                    return Fill(Voice.Pick(seed + "ew", "{gangadj} business. You didn't see us.", "Ride on, or kneel down next to him."), p);
            }
        }

        // him to the player
        public static string CallsToYou(string seed)
        {
            return Voice.Pick(seed + "ec", "Mister! Mister, they're going to kill me!", "Please, sir! Help me!", "For the love of God, don't ride on!");
        }

        public static string LastWords(string seed)
        {
            return Voice.Pick(seed + "el", "Wait. Wait, please...", "Tell my wife I...", "Lord. I'm not ready.");
        }

        // the men to each other, after
        public static string Done(Plan p, string seed)
        {
            if (p.Captors == "boss" || p.Captors == "kin")
                return Voice.Pick(seed + "ed", "Not a word of this. Not to anyone.", "It's done. Let's go home.");
            return Voice.Pick(seed + "ed", "That's done.", "Leave him for the crows.", "Let's go before somebody comes.");
        }

        // him to the player, saved
        public static string Thanks(Plan p, string seed)
        {
            switch (p.Reason)
            {
                case "debt":
                    return Fill(Voice.Pick(seed + "eth", "I'll pay {boss}. Every cent. Thank you, mister. Thank you.", "Eleven dollars. I nearly died for eleven dollars. Thank you."), p);
                case "feud":
                    return Voice.Pick(seed + "eth", "They'd have done it. They really would have done it. Thank you.", "You came out of nowhere. Lord, you came out of nowhere.");
                case "informer":
                    return Voice.Pick(seed + "eth", "They'll come again. But not today. Thank you.", "I owe you my life, mister. I don't know how to pay that.");
                default:
                    return Voice.Pick(seed + "eth", "I'm going home, and I'm never taking this road again.", "I'll light a candle for you. Every Sunday I live.");
            }
        }

        // what he thinks, kneeling (over his head)
        public static string During(Plan p, string seed)
        {
            switch (p.Reason)
            {
                case "debt":
                    return Fill(Voice.Pick(seed + "edu", "Eleven dollars. Not like this. Not over eleven dollars.", "{boss} sent them. {boss}, who shook my hand at church."), p);
                case "feud":
                    return Voice.Pick(seed + "edu", "The children. Who'll see to the children.", "Twenty years of that fence. It comes to this.");
                case "informer":
                    return Voice.Pick(seed + "edu", "Should've kept my mouth shut. Should've kept my mouth shut.", "The sheriff swore nobody would know.");
                default:
                    return Voice.Pick(seed + "edu", "Don't look at their faces. Don't look.", "I only took the short way home.");
            }
        }

        // what he thinks for a few days after he was saved
        public static string After(Plan p, string seed)
        {
            if (p != null && p.Reason == "debt" && Voice.Chance(seed + "ead", 35))
                return Fill(Voice.Pick(seed + "eab", "Paid {boss} today. Every cent. Couldn't look him in the eye.", "Every time I pass {boss}'s door my knees go."), p);
            return Voice.Pick(seed + "ea", "Still feel the barrel at the back of my head.", "Knelt in the dirt and begged. Can't stop seeing it.",
                "A stranger rode up and I'm alive. Just like that.", "Slept with the lamp lit again. Every sound's a horse.");
        }

        /// What the town hears once he's found, for the "Around town" box.
        public static string Found(Plan p, string name, string town, string seed)
        {
            string where = town != null ? " on the " + town + " road" : " by the road";
            switch (p.Reason)
            {
                case "debt": return name + " was found shot dead" + where + ". Folks say it was over a debt to " + p.Boss + ".";
                case "feud": return name + " was found shot" + where + ". Nobody says the " + p.Boss + " name out loud.";
                case "informer": return name + " was found shot" + where + ". They say he'd talked to the law.";
                default: return name + " was found shot dead" + where + ". Nobody knows why.";
            }
        }

        private static string Fill(string s, Plan p)
        {
            string adj = p.GangKey == "raiders" ? "Raider" : p.GangKey == "murfrees" ? "Murfree" : "O'Driscoll";
            return s.Replace("{bossword}", BossWord(p)).Replace("{boss}", p.Boss ?? "").Replace("{gangadj}", adj).Replace("{gang}", Grammar.UpperFirst(p.Gang ?? "the gang"));
        }

        /// Every line, for the tests.
        public static IEnumerable<string> AllLines()
        {
            foreach (string reason in new[] { "debt", "feud", "law", null })
                for (int i = 0; i < 16; i++)
                {
                    Plan p = For(reason, "Yates", reason == "debt" ? "res:y" : null, i % 2 == 0 ? "Rhodes" : "Valentine", "s" + i);
                    string s = "s" + i;
                    yield return Threat(p, s);
                    yield return Plead(p, s);
                    yield return WarnYou(p, s);
                    yield return Done(p, s);
                    yield return Thanks(p, s);
                    yield return During(p, s);
                    yield return After(p, s);
                    yield return Found(p, "Milo Hubbard", "Valentine", s);
                }
            for (int i = 0; i < 12; i++)
            {
                yield return CallsToYou("s" + i);
                yield return LastWords("s" + i);
            }
        }
    }
}
