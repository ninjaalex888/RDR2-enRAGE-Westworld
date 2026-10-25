// What an ordinary person is thinking, drawn from who they are rather than from one shared list: their trade,
// their town, their temperament, their faith and the hour; the worry their life is going through
// (InnerLife.Worries.cs); the day of the week, the meal they're missing, the year, and what the player did in
// their town lately (InnerLife.Days.cs). Each thought comes from one of those and is remembered so the same
// person doesn't think it again soon. All lines are original to this add-on.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static partial class InnerLife
    {
        /// What the game day brings, for a thought: set up by the engine (it knows the save) before asking.
        public sealed class Context
        {
            public long GameDay = -1;              // whole game days, for the worry a life is going through; -1 unknown
            public List<Deed> TownDeeds;           // the player's recent bad deeds in this person's town, newest first
        }

        /// Set by the game side from the clock: 0 Sunday .. 6 Saturday (-1 unknown), and the year (1907 in the epilogue).
        public static int Weekday = -1;
        public static int ClockYear = 1899;

        // ---- trade ----------------------------------------------------------------------------------

        private static readonly Dictionary<string, string[]> ByTrade = new Dictionary<string, string[]>
        {
            { "farm", new[] {
                "North fence is down again. Cattle know it before I do.", "If the well goes dry before August, I'm done.", "Hog got out. I'll find him by the smell.",
                "Hands are cracked to the bone. Salve costs more than the corn it saves.", "Rain by Thursday, the almanac says. The almanac lies.", "Hay's cut. Hay's turned. Hay's rotting if this weather holds.",
                "Mule kicked the milk pail over. Laughed so I wouldn't cry.", "Planting by the moon, like my father did. The moon never paid a bank.", "Smell of turned earth. Only honest smell there is." } },
            { "town", new[] {
                "Ought to settle that account at the general store before he asks.", "Same faces on the same corners. I could name every one in my sleep.",
                "Stage is late. Stage is always late, and always my fault.", "Church supper Sunday. Somebody'll bring that awful pie again.", "Rent's due Friday. Wages come Saturday. Math was never my strength.", "Another quiet day in {town}. I ought to be grateful.",
                "Barber holds a razor to your throat and tells you his politics. Nobody argues.", "New sign over the hotel. Same bedbugs under it.", "In {town}, everybody's business is everybody's business." } },
            { "old", new[] {
                "Knees say rain. Knees were right before the telegraph was.", "Used to be a man could ride from here to the river and not see a fence.", "Everyone's in such a hurry now. Where do they think they're going?",
                "I buried two wives and a brother. Still can't abide cold coffee.", "That boy's got my father's walk. Don't know whose boy he is.", "Bench is warm. Sun is out. That's a good day, at my age.",
                "Hold the paper at arm's length to read it. Arms are getting shorter.", "Outlived the doctor who said I'd never see fifty. Went to his funeral. Ate his widow's ham.", "Young folks walk so fast. I walked fast once. Got nowhere sooner." } },
            { "gentry", new[] {
                "Third letter to my lawyer. He answers like a man paid by the hour. He is.", "The dust in this town settles on everything. Even my opinion of it.", "Dinner at seven. I'll wear the grey.",
                "I did not come west for this. I came for the air. The air is mostly horses.", "One must be seen at church. One need not listen.", "If the bank holds, we hold. If it doesn't, I'll be selling china.",
                "Cook gave notice. Says the house is haunted. It's only my mother-in-law.", "Kid gloves in this dust. Absurd. Wouldn't be seen without them.", "Brought calling cards west. Nobody here to call on." } },
            { "working", new[] {
                "Back aches. Shift's got four hours yet.", "Foreman looked at me like I owed him something. I don't. Not till payday.", "A man could make a living at this if the wage was honest.",
                "Dinner pail's light. Pay is lighter.", "Soon as I've got fifty dollars saved, I'm out of here. Been saying fifty for a year.", "Boots wore through at the toe. Stuffing them with newsprint again.",
                "Blisters on the blisters. Palms like an old saddle.", "Foreman's whistle. Hear it in my sleep. Wake up already tired.", "Ate standing up again. Chairs are for men who own them." } },
            { "card", new[] {
                "Luck runs like a river. Mine's dry upstream.", "Man across the table blinks when he bluffs. Tonight I will own his watch.", "One more hand. One more hand and I'll walk away. Same as last night.",
                "Never play a man who's drinking water.", "Dealt a pair of sixes. Sixes are a poor man's promise.", "Owe the house more than I'll admit. Ace in the hole will fix that.",
                "Shuffle, cut, deal. Hands know it better than they know a prayer.", "Lost my watch to a man with a glass eye. He winked at me. I think.", "Marked deck in my coat. Never used it. A comfort, like a derringer." } },
            { "rough", new[] {
                "Nobody in this town looks at me twice. That's how I like it.", "Hungry. Always hungry. Hunger makes a man think things.", "Sheriff's eyes follow me. Let them. I've done nothing today.",
                "Owed twenty dollars. Collecting soon, one way or another.", "Pretty horse at the rail. Don't look at it. Don't look.", "Rain's coming. Better find a roof that isn't mine.",
                "Knife's gone dull. Whet it tonight. Not for anything. A dull knife's an insult.", "Caught my face in a shop window. Looked like trouble. Didn't mind.", "Slept in a hayloft. Itching in places I can't scratch in company." } },
            { "trapper", new[] {
                "Pelts are thin this year. Fox know something I don't.", "Haven't spoken to anyone in nine days. Town's loud.", "Trader will cheat me by a dime a pelt. Or a quarter. Depends on his mood.",
                "Smoke from the east means weather. Smell it? Weather.", "Traps want checking before dusk. Wolves were close.", "Boots stink of the creek. People step back. That's fine.",
                "Beaver's near gone from the creeks. Hats back east. Hats.", "Slept under a ledge with a fox two yards off. Neither of us minded.", "Town food sits like a stone. Miss my own fire." } },
            { "girl", new[] {
                "Rent's due. Rent's always due.", "Nobody in this town ever says my name in daylight.", "Feet are killing me. Shoes are killing me. The whole evening is killing me.",
                "Saving up. Another year, maybe two. I'll be somewhere that doesn't know me.", "Nice man came in last night. Left a good tip and a worse story.", "If the preacher looks at me one more time with that face...",
                "Lace torn again. Mend it by lamplight. Smile by lamplight too.", "Same tune on the piano every night. Could sing it backwards. Some nights I do.", "Boy asked me to marry him last night. Boys will promise anything on whiskey." } },
            { "soldier", new[] {
                "Orders are orders. Can't remember who gave this one.", "Drilled the same maneuver till it's the only thing my legs know.", "Fort food again. If this is duty, give me a different duty.",
                "Keep the line. Keep the line. Keep the line.", "Sergeant says don't think. Easy for a man who gets paid to.", "Boots wet, rifle heavy, and somebody back home still thinks I'm a hero.",
                "Rifle's cleaner than my bunk. Sergeant's priorities.", "Letter from home asks when I'll be done. Nobody tells a private anything.", "Saluted a horse in the dark. Thought it was the captain. Hope nobody saw." } },
            { "law", new[] {
                "Badge gets heavy on a hot day.", "Everyone in town knows my name and none of them wave.", "Quiet shift. Quiet shifts frighten me more than shooting.",
                "Wonder if I'd still be a good man without this star on my chest.", "Every drunk swears he's innocent. Some are.", "Keys on my belt, and not one opens the door I want.",
                "Posters curl in the sun faster than I catch the men on them.", "Coffee at the jail is worse than the cells. Prisoners say so. They're right.", "Half the faces in town I've arrested. Other half I will. Hard to make friends." } },
            { "plain", new[] {
                "Need to be somewhere. Can't remember where.", "Wonder if it'll rain.", "Hungry. Supper's two hours off, and I'm not paying for a second meal.",
                "Should've written Mother last month. Now it's a bigger letter.", "Heat makes everyone short-tempered. Me too.", "Who decided it should be so far to anywhere?",
                "Button's off my coat. Can't find it. Can't buy one. A pin, then.", "Wonder what folks in the city are doing right now. Something better.", "Long road, short money. Same as ever." } }
        };

        // ---- the hour -------------------------------------------------------------------------------

        private static readonly string[] Dawn =
        {
            "Early light on the rooftops. Quiet enough to hear my own coffee.", "Rooster's shouting at nobody. Rooster's been shouting since four.", "Cold this early. Joints complaining before I've stood up.",
            "Mist on the road. Never trust the first hour. It's too pretty.", "Smell of woodsmoke and horse. Mornings are honest at least."
        };

        private static readonly string[] Day =
        {
            "Sun's high. Shade's a luxury.", "Dust everywhere. In my teeth, in my tea.", "Noon bell. Everyone pretending they're not hungry.",
            "Heat sits on the street like a hat. Nobody wants to move.", "Long day and not half over."
        };

        private static readonly string[] Evening =
        {
            "Light's going gold. Nice for a minute, then it's work again.", "Supper smells drift up the street. Somebody has bacon.", "Lamps being lit down the row. First one's always the saloon.",
            "Cooler now. Now a man can think.", "Dogs bark, children called indoors. Evening, same as every one."
        };

        private static readonly string[] Night =
        {
            "Too dark to see my hand. Too quiet to hear my own thoughts.", "Piano's still going at the saloon. Tune never changes.", "Moon's up. Wolves are, too.",
            "Should be home by now. Should be a lot of things.", "Something about night makes the town look like a stage set after the play."
        };

        // ---- temperament, faith ---------------------------------------------------------------------

        private static readonly Dictionary<string, string[]> ByTemper = new Dictionary<string, string[]>
        {
            { "Hothead", new[] { "One more look from that fellow and I'll answer it with my fist.", "Wasn't raised to take an insult. Wasn't raised to forgive one either.", "Counting to ten. Never gets past six." } },
            { "Coward", new[] { "Is that man armed? He looks armed. They all look armed.", "Best not to be noticed. Best to be a fence post.", "Heard a shot last night. Haven't left the house the same since." } },
            { "Proud", new[] { "They don't know what I've done. They don't need to.", "I'd sooner starve than ask Hollis for a loan.", "A man's name is what he's got. Mine's still clean." } },
            { "Cold", new[] { "People talk too much. I listen. I remember.", "Don't need friends. Need a quiet room and a fair price.", "Smiled at him. It cost me nothing. It bought me plenty." } },
            { "Greedy", new[] { "If he'll pay four, he'll pay five. Ask for five.", "Dollar here, dollar there. A man gets rich by never being full.", "Count it again. Count it twice. It still isn't enough." } },
            { "Loyal", new[] { "Promised I'd look in on her. I will. I keep my promises.", "Blood or not, they're mine. Anyone says different answers to me.", "Never left a friend behind. Not going to start." } },
            { "Drifter", new[] { "Been here three weeks. Longer than most places. Feet are twitching.", "Every town's the same town in a different hat.", "Might head north. Might not. Depends on the wind." } }
        };

        private static readonly Dictionary<string, string[]> ByFaith = new Dictionary<string, string[]>
        {
            { "Devout", new[] { "Said grace on the porch this morning. Said it again for the horses.", "The Lord provides. He's slow about it, but He provides.", "Prayed for rain. Prayed for forgiveness. Mostly for the second." } },
            { "Godless", new[] { "Hear them singing from the church. Sounds nice. Wouldn't stay.", "If there's a Heaven, I hope it has better coffee.", "Preacher says everything's for a reason. I wish I could see it." } },
            { "Other", new[] { "Church on Sunday. Rest of the week I'm on my own.", "Said a prayer, mostly for habit. Nobody answered, which was fair.", "God and I are on speaking terms. Mostly I do the speaking." } }
        };

        // ---- towns ----------------------------------------------------------------------------------

        private static readonly Dictionary<string, string[]> ByTown = new Dictionary<string, string[]>
        {
            { "Valentine", new[] { "Smell of cattle on the wind. You get used to it. You never get fond of it.", "Stockyards bawling again. Never sleeps, this town.", "Another train, another load of beef. Nobody asks where it goes." } },
            { "Rhodes", new[] { "Gray or Braithwaite, doesn't matter. Everyone here picks a side and pays for it.", "Cotton and sugar and old grudges. That's Rhodes.", "Heard shooting out past the plantation. Heard it, didn't hear it, you understand." } },
            { "Saint Denis", new[] { "Trolley bell, horse cart, brass band. A man can't think in this town.", "Ships in the harbor. Somebody's getting rich off the cargo, and it isn't me.", "Streets so crowded a man could be robbed and never know whose hand." } },
            { "Blackwater", new[] { "Everything here costs twice. Even the quiet.", "Ferry horn means someone important arrived. Always someone important.", "New buildings every month. Same old people inside them." } },
            { "Strawberry", new[] { "Smell of cut pine. Sawdust in the soup.", "Mill whistle at six. Mill whistle at six. Mill whistle at six.", "Hills are pretty. Town's the kind of pretty where you keep your hand on your wallet." } },
            { "Annesburg", new[] { "Coal dust in the lungs, coal dust in the stew.", "Mine whistle. Count the shifts, count the dead, count again.", "Company owns the houses, the store, and half my opinions." } },
            { "Van Horn", new[] { "Dock air, bad whiskey, men who don't ask names. Suits some.", "Wouldn't walk the back streets after dark. Wouldn't walk the front ones either.", "Everyone in Van Horn is passing through. Only some are honest about it." } },
            { "Armadillo", new[] { "Dry as a bone. Even the gossip is dusty.", "Fever took half the street last year. We say little about it.", "Sun on the adobe. Shade is the only civilised thing out here." } },
            { "Tumbleweed", new[] { "Used to be a town. Now it's a place where things happened.", "Wind through the empty windows. Sounds like someone arguing in another room.", "Nobody stays. Those that do know why they can't leave." } }
        };

        // ---- the body, the mirror, the biases, the age, the mask (from the writing brief) ---------------

        /// What the body is saying right now: tiredness, hunger, a craving, weather on the skin.
        private static readonly string[] Body =
        {
            "Blister on the left heel, hot as a coin. Boots weren't made for this road.", "Stomach folded in on itself. Last real meal was Tuesday. Or Monday.",
            "Craving tobacco. Fingers keep patting the empty pocket.", "Wind straight through the coat. Bones aching like a bad tooth.", "Sweat down the spine. Collar like a wet rope.",
            "Eyes gritty from the dust. Can't think when they didn't sting.", "Back's one long knot. Slept on a floor again.", "Tongue stuck to the roof of my mouth. Water. Water."
        };

        /// How they think they look to everyone else.
        private static readonly string[] Mirror =
        {
            "Saw how the grocer looked at my sleeve. Frayed. He noticed. They always notice.", "Straighten up. Hat square. Let them think I own something.",
            "Laughed at the banker's joke a beat too late. Did they see?", "Wearing the good coat in this heat. The good coat says somebody.",
            "They go quiet when I pass. Respect or pity. Can't tell which is worse.", "Walk like there's somewhere to be. Nobody asks a busy man for a favour.",
            "That man's boots cost more than my horse. Doesn't mean he can ride."
        };

        /// Fear of losing, the eye that finds the threat first, the comfort of thinking the poor earned it.
        private static readonly string[] Bias =
        {
            "Count the coins twice. Once more. Still short.", "One sneer on Tuesday. Still tasting it. The compliment? Gone by supper.",
            "That beggar's young enough to work. Nobody handed me anything. Why him?", "Lose the horse, lose the job. Lose the job, lose the room. Lose the",
            "Every stranger's a tax collector until proven otherwise.", "Prices up a penny. Nobody says a word. I'll say it later, to the dog.",
            "Folks are poor for a reason. Say it often enough, it holds."
        };

        /// The age pressing in: smoke, disease, the law, the bank.
        private static readonly string[] Age =
        {
            "Smoke from the new railhead. Takes a little more sky every year.", "Heard the Pinkertons are asking questions again.",
            "Cough in the next room all night. Consumption, they say. Nobody says it loud.", "Cholera down the river. Boil the water. Boil everything.",
            "Telegraph poles marching in from the east. Soon there won't be a place left to hide.", "Mills, mines, machines. Hands like mine aren't worth what they were.",
            "Bank wants the note by spring. Spring is a hungry word.", "The law's got faster horses and longer arms now. Don't like where the wire leads."
        };

        /// What they show and what they feel.
        private static readonly string[] Mask =
        {
            "Smile. Nod. Wish he'd choke on his own coin.", "Polite as pie to the preacher. Thinking about his sister-in-law, God forgive me.", "Voice steady. Hands shaking in my pockets.",
            "Telling a joke to the table. Thinking about the letter in my coat.", "Everyone says I'm calm. I'm counting the exits.", "Tough face for the street. Terrified of the doctor on Thursday."
        };

        // ---- the stranger on the road ------------------------------------------------------------------

        /// What a person can see of the player right now.
        public sealed class PlayerLook
        {
            public bool Near;       // close enough to study
            public bool Armed;      // a gun in hand
            public bool Mounted;    // on a horse
            public int Seen;        // times this person has met him (a town regular), counting now
        }

        /// A town regular who has met him before, seeing him again.
        private static readonly string[] Again =
        {
            "Him again. Same hat. Same way of standing, like the street owes him money.",
            "Seen this one before. I don't forget a man who looks at me that long.",
            "Back in {town}, is he. I'd started to think I dreamed him.",
            "That stranger again. Nodded at me last time. Will he this time?",
            "I know that face. Know it from here. Every time, from right here.",
            "Third time, or fourth. I've stopped pretending I don't notice him."
        };

        private static readonly string[] SeenArmed =
        {
            "Gun hand never far from the gun. Not for show. For use.", "Holster's worn smooth at the edge. That's years of drawing.", "Doesn't lift the gun. Doesn't need to. The weight shows in how he stands."
        };

        private static readonly string[] SeenMounted =
        {
            "Sits that horse like a chair. Never been afraid of it, or never shows.", "Horse is better fed than half this street.", "Horse is watching me. Rider isn't. Somehow that's worse."
        };

        private static readonly string[] Seen =
        {
            "Doesn't blink enough.", "Carries his weight low, like a man ready to move.", "Smells of woodsmoke and horse.", "Eyes don't rest on one thing. Counts the exits like I do.",
            "Dust on him from three days' road, not three miles.", "Quiet. The kind of quiet that's decided something."
        };

        // ---- putting it together ------------------------------------------------------------------

        /// The trade group for a description such as "a farmhand" or "an old woman".
        public static string TradeGroup(string trade)
        {
            string t = (trade ?? string.Empty).ToLowerInvariant();
            if (t.Contains("soldier")) return "soldier";
            if (t.Contains("lawman")) return "law";
            if (t.Contains("farm")) return "farm";
            if (t.Contains("old")) return "old";
            if (t.Contains("lady") || t.Contains("gentleman")) return "gentry";
            if (t.Contains("working girl")) return "girl";
            if (t.Contains("labour") || t.Contains("working") || t.Contains("miner") || t.Contains("dock")) return "working";
            if (t.Contains("card")) return "card";
            if (t.Contains("rough") || t.Contains("hard-looking")) return "rough";
            if (t.Contains("trapper")) return "trapper";
            if (t.Contains("town")) return "town";
            return "plain";
        }

        private static string[] ForHour(int hour)
        {
            return hour >= 4 && hour < 8 ? Dawn : hour >= 8 && hour < 17 ? Day : hour >= 17 && hour < 21 ? Evening : Night;
        }

        /// A thought for this person at this hour. `told` remembers what they've thought lately (may be null); `day`
        /// brings the worry they're living through and the town's news (may be null: then only the timeless pools).
        public static string Thought(Person p, int hour, string seed, List<string> told, PlayerLook look = null, Context day = null)
        {
            // a regular who has met him before knows him again
            if (look != null && look.Near && look.Seen >= 2 && Voice.Hash(seed + "again") % 100 < 22)
                return Pick(Again, p, seed, told);
            // the stranger close by is worth a thought of its own
            if (look != null && look.Near && Voice.Hash(seed + "look") % 100 < 28)
                return Pick(look.Armed && Voice.Chance(seed + "la", 55) ? SeenArmed : look.Mounted && Voice.Chance(seed + "lm", 55) ? SeenMounted : Seen, p, seed, told);
            int roll = Voice.Hash(seed + "ch") % 100;
            // their own life first: the worry they're going through, what happened in town, the day and the year
            string own = null;
            if (roll < 24)
                own = day != null && day.GameDay >= 0 ? WorryThought(p, day.GameDay, seed, told) : null;
            else if (roll < 34)
                own = day != null && day.TownDeeds != null && day.TownDeeds.Count > 0 ? TownTalk(p, day.TownDeeds, seed, told) : null;
            else if (roll < 44)
            {
                string[] when = ForDay(hour, Weekday, seed);
                own = when != null ? Pick(when, p, seed, told) : null;
            }
            else if (roll < 49)
                own = Pick(ClockYear >= 1900 ? Era1907 : Era1899, p, seed, told);
            if (own != null)
                return own;
            if (roll < 49)
                roll = 49 + roll * 51 / 49;   // nothing of their own for this roll: one of the timeless frames instead
            string[] pool;
            if (roll < 61)
                pool = ByTrade[TradeGroup(p.Trade)];
            else if (roll < 67)
                pool = ForHour(hour);
            else if (roll < 74)
                pool = Body;
            else if (roll < 79)
                pool = Mirror;
            else if (roll < 83)
                pool = Bias;
            else if (roll < 86)
                pool = Age;
            else if (roll < 90)
                pool = Mask;
            else if (roll < 95 && p.Personality != null && ByTemper.ContainsKey(p.Personality))
                pool = ByTemper[p.Personality];
            else if (roll < 98 && p.Home != null && ByTown.ContainsKey(p.Home))
                pool = ByTown[p.Home];
            else
                pool = ByFaith.ContainsKey(p.Devotion ?? "") ? ByFaith[p.Devotion] : ByFaith["Other"];
            return Pick(pool, p, seed, told);
        }

        private static string Pick(string[] pool, Person p, string seed, List<string> told)
        {
            int start = Voice.Hash(seed) % pool.Length;
            for (int i = 0; i < pool.Length; i++)
            {
                string raw = pool[(start + i) % pool.Length];
                string key = "il" + Voice.Hash(raw);
                if (told != null && told.Contains(key))
                    continue;
                if (told != null)
                {
                    told.Add(key);
                    if (told.Count > 40)
                        told.RemoveAt(0);
                }
                return raw.Replace("{town}", string.IsNullOrEmpty(p.Home) ? "this town" : p.Home);
            }
            // every line of this pool thought lately: only this pool starts over (the list also holds other kinds of
            // lines, such as the reality musings already said, which must stay remembered)
            if (told != null)
                foreach (string raw in pool)
                    told.Remove("il" + Voice.Hash(raw));
            return pool[start].Replace("{town}", string.IsNullOrEmpty(p.Home) ? "this town" : p.Home);
        }
    }
}
