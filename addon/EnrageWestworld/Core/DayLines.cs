// The ordinary texture of someone's day, for their diary: their work, their town, their kin, their faith,
// their habits, their body, and the line they end the day on. Everything is chosen from what enRAGE says
// about the person (trade, gang, home, family, devotion, quirks, scars, personality) and the day's seed,
// so two people rarely write the same day, and one person rarely writes the same line twice in a row.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class DayLines
    {
        private static readonly string[] Outlaw =
        {
            "Cleaned my guns twice. Nobody told me to.", "Sat lookout on the ridge all afternoon. Nothing but hawks.",
            "The boys counted the take again. Came up short again. Somebody's skimming.", "Changed horses at the old barn. Can't be too careful.",
            "Rode wide around {town}. Too many deputies of late.", "Spent the day waiting on word that never came.",
            "Played cards with the boys for matchsticks. Lost anyway.", "Saw a wanted poster with a face a lot like mine. Tore it down.",
            "Buried a strongbox where nobody will find it. I hope I can."
        };

        private static readonly string[] Swindler =
        {
            "Lifted a watch off a drummer at the station. Gold-plated. Figures.", "Sold a widow a cure-all made of creek water and molasses. She thanked me.",
            "Ran the shell game outside the saloon till a deputy took an interest.", "Told a farmer I was a land agent. He believed every word."
        };

        private static readonly string[] Soldier =
        {
            "Drill in the morning, drill in the afternoon. The lieutenant likes the sound of his own voice.", "Stood picket till my feet went numb.",
            "Polished brass nobody will ever look at.", "Rations were weevils with a little bread around them.", "Rode patrol along the river. Saw nobody. Wrote a report about it."
        };

        private static readonly string[] OldSoldier =
        {
            "Heard a bugle in my sleep again.", "Still wake at reveille. All these years and I still wake at reveille.",
            "A boy asked me about the war. I told him about the food.", "Cleaned my old service revolver. Don't know why I keep it."
        };

        private static readonly string[] Hunter =
        {
            "Checked the traps at dawn. Two muskrats and an empty jaw where a fox chewed free.", "Tracked a buck half the day and lost him at the creek.",
            "Fleshed hides till my hands cracked.", "Sold pelts in {town}. The trader cheated me and smiled about it.",
            "Saw cougar sign by the ridge. Slept with the rifle.", "Wolves singing all night. Close."
        };

        private static readonly string[] Ranch =
        {
            "Rode fence from sunup. Found the break by the dry wash.", "Two calves came early. Both lived.", "Lost a steer at the river crossing.",
            "The new mare threw me twice. She'll be saddle-broke by Sunday or I will be.", "Branded till my arm ached. The smell won't wash out.",
            "Drove strays down out of the hills. One old cow fought me every step."
        };

        private static readonly string[] Farm =
        {
            "Hoed the beans. The weeds are winning.", "Mended the fence the cow keeps finding her way through.",
            "Took eggs to {town} and got half what they're worth.", "Prayed for rain. Got wind.", "Slaughtered the old hog. Smokehouse is full, for now."
        };

        private static readonly string[] Smith =
        {
            "Shod six horses and one mule with opinions.", "Burned my forearm on the forge. Same place as last time.",
            "Fixed a wagon axle for a man who swore he'd pay Friday.", "Made nails all day. My ears are still ringing."
        };

        private static readonly string[] Shop =
        {
            "Counted the till twice. Came out even, which is a first.", "A woman spent an hour choosing between two bolts of cloth and bought neither.",
            "Restocked the tinned peaches. Nobody buys tinned peaches.", "Gave a boy a peppermint for sweeping the porch. Best money I spent all week."
        };

        private static readonly string[] Doctor =
        {
            "Pulled a tooth, set an arm and lanced something I won't describe.", "Sat up with a feverish child. She'll live, I think.",
            "Ran out of laudanum again.", "Dug a bullet out of a man who swore he'd shot himself cleaning his gun. In the back."
        };

        private static readonly string[] Preacher =
        {
            "Wrote Sunday's sermon. Then wrote it again.", "Visited the sick. Comforted who I could.", "Half the pews were empty. The saloon was full.",
            "Buried a man nobody came to mourn. Said the words anyway."
        };

        private static readonly string[] River =
        {
            "Hauled crates till my back gave out.", "Fish weren't biting. Neither was the foreman, for once.", "Fog on the river all morning. Couldn't see my own hands.",
            "A steamboat came in loud and left louder."
        };

        private static readonly string[] Labour =
        {
            "Twelve hours of hard labour. Came home too tired to eat.", "The foreman docked us for drinking water. In this heat.",
            "A man lost two fingers today. The work didn't stop.", "Paid out on Saturday. Most of it's already spoken for."
        };

        private static readonly string[] Gambler =
        {
            "Won forty dollars at faro. Lost thirty-five at poker. Call it a living.", "Caught a man dealing seconds. Said nothing. Took his money later, honest.",
            "Bad cards all night. Good whiskey, though.", "A greenhorn bet his horse on a pair of sixes. I've got a horse now."
        };

        private static readonly string[] Saloon =
        {
            "Danced till my feet ached. Kept the tips in my boot.", "A drunk proposed to me. Third one this week.",
            "Mopped up after a brawl. Nobody even remembers what it was about.", "Sang the same four songs all night. They clapped every time."
        };

        private static readonly string[] Scout =
        {
            "Rode out ahead and found fresh tracks heading north.", "Read the sign at the river. Three riders, one horse lame.", "Mapped a pass nobody uses. Somebody will, now."
        };

        private static readonly string[] Drifter =
        {
            "Walked into {town}, walked out again. Nothing for me there.", "Camped by the water. Fire wouldn't take.", "Did odd work for a farmer. He paid in beans.",
            "Slept in a barn. The owner didn't mind, or didn't notice.", "Long day. Short supper.", "Mended my boots with wire. They'll hold. Maybe."
        };

        private static readonly Dictionary<string, string[]> TownLines = new Dictionary<string, string[]>
        {
            { "Valentine", new[] { "Valentine stinks of cattle. Always has.", "Another fight outside the saloon in Valentine. Same two men as last week." } },
            { "Rhodes", new[] { "Rhodes is all red mud and old grudges.", "Somebody in Rhodes is always feuding with somebody else in Rhodes." } },
            { "Saint Denis", new[] { "Saint Denis never sleeps. I'd give anything for a night's quiet.", "Saw a horseless carriage in Saint Denis. Nearly ran over a nun." } },
            { "Strawberry", new[] { "The mayor of Strawberry gave another speech. Nobody listened.", "Pine smoke over Strawberry all day." } },
            { "Blackwater", new[] { "Blackwater's got more lawmen than honest men these days.", "Folks in Blackwater still lock their doors. They remember." } },
            { "Annesburg", new[] { "Coal dust on everything in Annesburg. Even the bread.", "The mine whistle in Annesburg. You stop hearing it, then you don't." } },
            { "Armadillo", new[] { "Armadillo's dying slow, like a dog in the sun.", "The wind brought sand into everything in Armadillo." } },
            { "Tumbleweed", new[] { "Tumbleweed is quiet. Too quiet, if you ask me.", "Nothing in Tumbleweed but heat and the sheriff." } },
            { "Van Horn", new[] { "Van Horn's the kind of place you keep your back to the wall.", "Somebody got knifed in Van Horn. Nobody saw a thing, as usual." } },
            { "Lagras", new[] { "The mosquitoes in Lagras could carry off a child.", "Gators in the shallows at Lagras. Watched them watch me." } },
            { "Emerald Ranch", new[] { "Emerald Ranch smells of money and manure.", "Quiet at Emerald Ranch. Suits me." } }
        };

        private static readonly string[] LetterTails =
        {
            "Read it twice.", "Says the price of flour is up again.", "Asks when I'm coming home. Didn't answer.",
            "Couldn't make out half the words. Didn't matter.", "Folded it small and kept it in my hat.", "All the news from home. None of it good, none of it bad."
        };

        private static readonly Dictionary<string, string[]> Closers = new Dictionary<string, string[]>
        {
            { "Hothead", new[] { "Somebody's going to cross me tomorrow. I can feel it.", "Near came to blows over a spilled drink. Near.", "Broke a chair. It was looking at me funny." } },
            { "Coward", new[] { "Barred the door twice.", "Heard hoofbeats in the night and didn't sleep after.", "Kept to the back roads. Safer." } },
            { "Proud", new[] { "Nobody in this territory does it better than me. Nobody.", "A man tipped his hat to me in the street. As he should.", "Let them talk. They'll remember my name." } },
            { "Cold", new[] { "Nothing worth writing. Nothing worth feeling.", "Watched a man weep in the street. Felt nothing.", "Slept well. I always do." } },
            { "Greedy", new[] { "Counted my money. Not enough. Never enough.", "Every man I meet is either a mark or a waste of time.", "There's a fortune out there with my name on it." } },
            { "Loyal", new[] { "Stood by my friends today. Always will.", "A man asked me to sell out my own. I told him where to go.", "My word's my word. That's all I've got." } },
            { "Drifter", new[] { "Thinking about moving on again.", "Itchy feet. Always itchy feet.", "Wonder what's past the next ridge." } }
        };

        private static readonly string[] PlainClosers =
        {
            "Tomorrow's another day.", "Lamp's running low. That's enough for tonight.", "Bed.", "Nothing else to tell.", "Wind's picking up. Time to sleep."
        };

        /// Up to n lines about the day, each from a different strand, none already in the last few entries.
        public static List<string> Texture(Person p, PersonState st, PlayerRef you, string town, string seed, int n, string recent)
        {
            List<KeyValuePair<int, Func<string>>> strands = new List<KeyValuePair<int, Func<string>>>
            {
                new KeyValuePair<int, Func<string>>(40, () => Work(p, town, seed)),
                new KeyValuePair<int, Func<string>>(12, () => Town(p, town, seed)),
                new KeyValuePair<int, Func<string>>(20, () => Kin(p, st, seed)),
                new KeyValuePair<int, Func<string>>(10, () => Faith(p, seed)),
                new KeyValuePair<int, Func<string>>(14, () => Habit(p, seed)),
                new KeyValuePair<int, Func<string>>(6, () => Body(p, seed)),
                new KeyValuePair<int, Func<string>>(8, () => Stranger(p, st, you, seed))
            };
            List<string> lines = new List<string>();
            int salt = 0;
            while (lines.Count < n && strands.Count > 0 && salt < 12)
            {
                int total = strands.Sum(s => s.Key);
                int roll = (Voice.Hash(seed + "tex" + salt++) & 0x7fffffff) % Math.Max(1, total);
                int i = 0;
                for (; i < strands.Count - 1 && roll >= strands[i].Key; i++)
                    roll -= strands[i].Key;
                string line = strands[i].Value();
                strands.RemoveAt(i);
                if (!string.IsNullOrEmpty(line) && !Seen(recent, line))
                    lines.Add(line);
            }
            return lines;
        }

        /// The line the day ends on: their nature, or nothing much.
        public static string Closer(Person p, string seed, string recent)
        {
            string[] pool;
            if (p.Personality == null || !Closers.TryGetValue(p.Personality, out pool))
                pool = PlainClosers;
            for (int i = 0; i < 3; i++)
            {
                string line = Voice.Pick(seed + "close" + i, pool);
                if (!Seen(recent, line))
                    return line;
            }
            return null;
        }

        /// True when this line (or its start) was written in the recent entries.
        public static bool Seen(string recent, string line)
        {
            if (string.IsNullOrEmpty(recent) || string.IsNullOrEmpty(line))
                return false;
            string probe = line.Length > 30 ? line.Substring(0, 30) : line;
            return recent.IndexOf(probe, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string Work(Person p, string town, string seed)
        {
            string t = (p.Trade ?? string.Empty).ToLowerInvariant();
            string[] pool;
            if (t.Contains("pickpocket") || t.Contains("con man"))
                pool = Swindler;
            else if (t.Contains("ex-soldier"))
                pool = OldSoldier;
            else if (p.IsSoldier || t.Contains("soldier"))
                pool = Soldier;
            else if (t.Contains("trapper") || t.Contains("hunter"))
                pool = Hunter;
            else if (t.Contains("rancher") || t.Contains("cowhand") || t.Contains("ranch") || t.Contains("drover") || t.Contains("vaquero") || t.Contains("horse breaker"))
                pool = Ranch;
            else if (t.Contains("farm"))
                pool = Farm;
            else if (t.Contains("blacksmith"))
                pool = Smith;
            else if (t.Contains("shopkeeper") || t.Contains("clerk") || t.Contains("tailor"))
                pool = Shop;
            else if (t.Contains("doctor"))
                pool = Doctor;
            else if (t.Contains("preacher"))
                pool = Preacher;
            else if (t.Contains("dock") || t.Contains("fisher"))
                pool = River;
            else if (t.Contains("miner") || t.Contains("lumber") || t.Contains("railroad"))
                pool = Labour;
            else if (t.Contains("gambler"))
                pool = Gambler;
            else if (t.Contains("saloon"))
                pool = Saloon;
            else if (t.Contains("scout"))
                pool = Scout;
            else if (p.IsOutlaw || t.Contains("outlaw") || t.Contains("thief") || t.Contains("rustler"))
                pool = Outlaw;
            else
                pool = Drifter;
            return Voice.Pick(seed + "work", pool).Replace("{town}", town ?? p.Home ?? "town");
        }

        private static string Town(Person p, string town, string seed)
        {
            string where = town ?? p.Home;
            string[] pool;
            return where != null && TownLines.TryGetValue(where, out pool) ? Voice.Pick(seed + "town", pool) : null;
        }

        private static string Kin(Person p, PersonState st, string seed)
        {
            List<Relative> family = st != null ? st.Family : null;
            Relative spouse = family != null ? family.FirstOrDefault(x => x.Relation == "wife" || x.Relation == "husband") : null;
            if (spouse != null && Voice.Chance(seed + "sp", 50))
            {
                string s = spouse.FirstName;
                if (p.Children > 0 && Voice.Chance(seed + "kids", 50))
                    return Voice.Pick(seed + "kid", "The little ones had a fever. Sat up all night with them.", "Taught the eldest to tie a bowline. Took all afternoon.",
                        "The children fought over a marble till " + s + " took it away from all of them.");
                return Voice.Pick(seed + "spouse", s + " burned the beans. I ate them anyway and said they were fine.",
                    "Argued with " + s + " about money. Then we made up.", s + " laughed at something I said. Haven't heard that in a while.",
                    "Wrote to " + s + ". Didn't say how much I miss " + (spouse.Woman ? "her" : "him") + ". Should have.");
            }
            if (p.Family == "widower" || p.Family == "widow" || p.Family == "widowed")
                return Voice.Pick(seed + "wid", "Talked to my late " + (p.Woman ? "husband" : "wife") + "'s picture again.", "Set two plates at supper without thinking.");
            Relative r = family != null ? family.Where(x => x.Relation != "wife" && x.Relation != "husband" && x.Town != null).OrderBy(x => Voice.Hash(seed + x.Name)).FirstOrDefault() : null;
            if (r == null)
                return null;
            string town = r.Town.Contains(",") ? r.Town.Substring(0, r.Town.IndexOf(',')) : r.Town;
            if (Voice.Chance(seed + "lt", 55))
                return "A letter from my " + r.Relation + " " + r.FirstName + " in " + town + ". " + Voice.Pick(seed + "lt2", LetterTails);
            return p.IsOutlaw
                ? Voice.Pick(seed + "wr", "Wrote to my " + r.Relation + " in " + town + ". Didn't tell " + (r.Woman ? "her" : "him") + " the half of it.",
                    "Started a letter to my " + r.Relation + ". Burned it.")
                : Voice.Pick(seed + "wr", "Wrote to my " + r.Relation + " in " + town + ".", "Thought about my " + r.Relation + " " + r.FirstName + " today. Should write more.");
        }

        private static string Faith(Person p, string seed)
        {
            switch (p.Devotion)
            {
                case "Devout": return Voice.Pick(seed + "fa", "Read from the Psalms before bed.", "Prayed long tonight. Felt heard, for once.", "Gave what I could to the collection. The Lord sees.");
                case "Churchgoer": return Voice.Pick(seed + "fa", "Sat through the sermon thinking about supper.", "Sang loud in church. Off-key, but loud.");
                case "Lapsed": return Voice.Pick(seed + "fa", "Walked past the church. Didn't go in.", "Caught myself praying. Old habits.");
                case "Godless": return Voice.Pick(seed + "fa", "The preacher came round for alms. Gave him a nickel to go away.", "Heaven's a story for children. Hell's the territory.");
                default: return null;
            }
        }

        private static string Habit(Person p, string seed)
        {
            List<string> lines = new List<string>();
            if (p.Has("drunk"))
                lines.Add(Voice.Pick(seed + "dr", "Drank more than I meant to. Again.", "Woke up with my boots on and my hat missing."));
            if (p.Has("gambler"))
                lines.Add(Voice.Pick(seed + "ga", "Lost two dollars at faro. Won one back. Call it even.", "Dice in my pocket, all day, itching."));
            if (p.Has("braggart"))
                lines.Add(Voice.Pick(seed + "br", "Told the story about the bear again. It gets bigger every time.", "Nobody believed me about the six men. It was five, but still."));
            if (p.Has("traitor"))
                lines.Add(Voice.Pick(seed + "tr", "Had a quiet word with a man in a gray coat. Nobody needs to know.", "Counted the money from the man in the gray coat. Thirty pieces, near enough."));
            if (p.Has("honest"))
                lines.Add(Voice.Pick(seed + "ho", "Found a dollar on the boardwalk. Asked around till I found whose it was.", "Told a man his horse was lame before he bought it. Lost the sale. Slept fine."));
            if (p.Has("superstitious"))
                lines.Add(Voice.Pick(seed + "su", "Saw a black cat by the well. Spat over my shoulder three times.", "An owl on the roof at noon. Nothing good comes of that."));
            if (p.Has("family man"))
                lines.Add(Voice.Pick(seed + "fm", "Bought a ribbon for my girl. She'll pretend she don't like it.", "Carved a little horse for the youngest."));
            if (!string.IsNullOrEmpty(p.Pet))
                lines.Add(Voice.Pick(seed + "pe", "The " + p.Pet.ToLowerInvariant() + " followed me all day. Best company I've had.", "Fed the " + p.Pet.ToLowerInvariant() + " better than I fed myself."));
            return lines.Count == 0 ? null : lines[(Voice.Hash(seed + "habit") & 0x7fffffff) % lines.Count];
        }

        private static string Body(Person p, string seed)
        {
            if (p.Scars != null && p.Scars.Count > 0)
                return Voice.Pick(seed + "sc", "The scar aches when the weather turns.", "Caught my reflection in a window. The scar's the first thing anybody sees.");
            if (p.AgeIn(Voice.NowYear) >= 55)
                return Voice.Pick(seed + "age", "My knees told me about the rain before the sky did.", "Getting old. The young ones ride past like I'm a fence post.");
            return null;
        }

        private static string Stranger(Person p, PersonState st, PlayerRef you, string seed)
        {
            if (st == null || you == null || (st.Talks == 0 && !st.KnowsName && st.KnownDeeds.Count == 0))
                return null;
            string who = Voice.YouPrivate(p, you, true);
            return Grammar.Sentence(Voice.Pick(seed + "you", "Thought about " + who + " again. Can't say why", "Wondered where " + who + " is tonight",
                "Somebody said " + who + " was seen on the road. I looked up like a fool"));
        }
    }
}
