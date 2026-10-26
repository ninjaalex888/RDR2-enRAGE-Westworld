// What is going on around a person right now, for a thought: something that just happened in front of them (a face
// put into the bar, a fistfight, a drunk on the floor, a thief), the other people in sight and what this person makes
// of them by their own temperament (envy of the well-dressed, worry for the drunk, fear of the hothead, contempt,
// curiosity), the weather on their skin when they're out in it, and the place they're in. Attention goes to what is
// near, new and threatening first (an event they just saw outranks the rain), and people judge others through their
// own nature, as people do. Small pleasures and memories (`Pleasure`) are a timeless frame of their own, so a life
// isn't all worry. All lines are original to this add-on.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static partial class InnerLife
    {
        /// Someone else in sight, as another person sees them (the game side fills it from the ticker's list).
        public sealed class Nearby
        {
            public string Id, Name, Temper;   // Name: the first name they go by
            public bool Woman, WellOff, Drunk;
            public string Known;              // what they're known for lately: "bully" (put a face in the bar), "thief" (caught at it), or null
        }

        /// A scene that just played out near the player (and so near whoever is thinking): the kind, who was in it.
        public sealed class Witnessed
        {
            public string Kind, AName, BName, AId, BId, Town;
            public long At;                   // game minute
        }

        /// The game's weather type, as it feels to someone out in it: rain, storm, snow, dust, fog, sun, grey, or null.
        public static string WeatherOf(uint weather)
        {
            switch (weather)
            {
                case 1420204096u: case 2572975940u: case 3878058453u: case 212278652u: case 1974067816u: return "rain";   // rain, drizzle, shower, sleet, hail
                case 3061285535u: case 2082228755u: case 839715153u: return "storm";                                     // thunder, thunderstorm, hurricane
                case 4021743606u: case 603685163u: case 669657108u: case 725623432u: case 2137137442u: case 1679686673u: return "snow";
                case 2977915153u: return "dust";                                                                          // sandstorm
                case 3592150529u: case 1500834021u: return "fog";                                                          // fog, misty
                case 1632247697u: case 4121459557u: return "sun";                                                          // sunny, high pressure
                case 3146353965u: case 433385945u: return "grey";                                                          // overcast, dark overcast
                default: return null;
            }
        }

        private static readonly Dictionary<string, string[]> ByWeather = new Dictionary<string, string[]>
        {
            { "rain", new[] {
                "Rain finds the one gap in a collar. Every time, the one gap.", "Boots full of water. Squelch, squelch, all the way home.",
                "Smell of wet dog and wet wool. Every soul in town smells the same today.", "Should've brought the oilskin. Said that this morning, too." } },
            { "storm", new[] {
                "Thunder like that, my mother used to cover the mirrors.", "Counted between the flash and the boom. Getting closer.",
                "Horses don't like it. Can't say I do." } },
            { "snow", new[] {
                "Can't feel my toes. Haven't for an hour. That's bad, isn't it.", "Snow muffles everything. Town sounds like it's holding its breath.",
                "Breath freezing in the beard. Old man's beard, at my age." } },
            { "dust", new[] {
                "Grit in my teeth, grit in my eyes. I'll be spitting dirt till Tuesday.", "Can't see the end of the street. Nor the man standing in it." } },
            { "fog", new[] {
                "Fog this thick, a man could walk right past his own brother.", "Everything wet and grey and close. Like being inside a cloud's pocket." } },
            { "sun", new[] {
                "Sun on the back of my neck like a warm hand.", "Squinting so long my face'll stay this way.",
                "Good drying weather. Ought to be hanging washing instead of standing here." } },
            { "grey", new[] {
                "Sky the colour of dishwater. Can't make up its mind.", "Grey day. Grey mood. One follows the other like a dog." } }
        };

        private static readonly Dictionary<string, string[]> ByPlace = new Dictionary<string, string[]>
        {
            { "saloon", new[] {
                "One drink. Then home. Said that two drinks ago.", "Piano's half a note out. Nobody minds after the third glass.",
                "Sawdust on the floor to soak up what nobody wants to look at.", "Every man in here is telling a story bigger than his life." } },
            { "church", new[] {
                "Lord, I don't ask much. Just a little less than I've been getting.", "The pew creaks every time I shift. Like it's keeping count.",
                "Preacher's looking right at me. Or I feel like he is. Same thing." } },
            { "store", new[] {
                "Prices up again. Molasses costs like medicine now.", "Write it in the book, I'll say. The book is getting long.",
                "Shopkeeper weighs with his thumb. Everybody knows. Nobody says." } },
            { "genstore", new[] {
                "Prices up again. Molasses costs like medicine now.", "Coffee, flour, lamp oil. Coffee, flour, lamp oil. Don't forget the lamp oil." } },
            { "hotel", new[] {
                "Paid for a bed and got a lumpy argument with a mattress.", "Walls thin enough to hear a man think next door.",
                "Clerk looks at your boots before your face. Decides the price by the boots." } },
            { "barber", new[] {
                "Sit still. Man with a razor at your neck, you sit still and agree with him.", "Smells of bay rum and other men's gossip in here." } },
            { "bath", new[] {
                "Hot water. Real hot water. Could weep.", "Third man in the same bathwater. Don't think about it. Don't think about it." } },
            { "photo", new[] {
                "Hold still for the camera, he says. I've held still my whole life.", "Folks pay to have their faces kept. Wonder who'd want to keep mine." } },
            { "fence", new[] {
                "Don't ask where any of it came from. That's the rule in here.", "Every watch in that case stopped at the minute somebody lost it." } },
            { "bank", new[] {
                "Banker smiles like he's counting your teeth for gold.", "Every coin in that vault was somebody's sweat. Never the banker's." } },
            { "doctor", new[] {
                "Smells of carbolic and bad news in here.", "Doctor says rest. Rest doesn't plough a field." } },
            { "sheriff", new[] {
                "Never stood in a lawman's office without feeling guilty of something.", "Wanted posters. Looking at the faces, half of them could be me." } },
            { "station", new[] {
                "Train's late. Train's always late, and always worth the wait, they say. They lie.", "Every train out of here is somebody's second chance." } },
            { "stable", new[] {
                "Horse smell and hay. Better company than most people.", "That grey mare watches me like she knows what I owe." } },
            { "tailor", new[] {
                "A good coat makes a man taller. I'll need a very good coat.", "Measured for a suit I can't afford. Tailor's tape around my middle like a judgement." } },
            { "gunsmith", new[] {
                "Rows of guns, all oiled and waiting. Waiting for what, I wonder.", "Man ahead of me bought shells for a rifle he doesn't own. Odd." } },
            { "theatre", new[] {
                "Paid a nickel to watch other folks pretend. Do it for free all week.", "The villain had the best lines. The villain always has the best lines." } }
        };

        // a scene that just played out in front of them, by kind ({a} started it, {b} had it done to him)
        private static readonly Dictionary<string, string[]> BySeen = new Dictionary<string, string[]>
        {
            { "rough", new[] {
                "Heard {b}'s face hit the wood from across the room. Sound like a dropped melon.", "{a} moved so fast. One moment talking, next {b}'s face is in the bar.",
                "Nobody stopped {a}. Nobody ever stops a man like {a}. I didn't either." } },
            { "brawl", new[] {
                "{a} and {b} going at it like dogs. Heart's still pounding and it wasn't even my fight.", "Blood on the boards. Somebody'll want to sweep that." } },
            { "drunk", new[] {
                "{a}, out cold on the floor, and him fixing to fight the world.", "Laughed at {a}. Shouldn't have. Could be me one day, a bottle further on." } },
            { "pickpocket", new[] {
                "Thief! Right in front of me. Hand in {b}'s coat, quick as a snake.", "Checking my pockets now. Twice. Everybody is." } },
            { "argue", new[] {
                "{a} and {b} going at each other. Pretended not to listen. Heard every word.", "Raised voices. The whole street leaned in." } },
            { "standoff", new[] {
                "Guns out, right there in the street. My knees haven't stopped." } },
            { "flirt", new[] {
                "{a} sweet-talking {b} in broad daylight. Some people have no shame. Good for them." } },
            { "propose", new[] {
                "{a} went down on one knee to {b}, right there. Never seen the like." } },
            { "snap", new[] {
                "{a} bit {b}'s head clean off. Something's eating at {a}." } },
            { "cheat", new[] {
                "{b} called {a} a cheat over the cards. Never seen chairs go back so fast." } },
            { "jealous", new[] {
                "Jealous men and loaded tempers. Saw the whole thing, and wished I hadn't." } },
            { "debt", new[] {
                "{a} calling in money from {b}, out in the open. That's a shame on both of them." } },
            { "charity", new[] {
                "{b} gave {a} money, just like that. Wish somebody'd give me money, just like that." } },
            { "scorn", new[] {
                "{b} turned {a} away cold. Could've spared a nickel. Could I have? Maybe." } }
        };

        // what one makes of someone else in sight, by their own nature ({name}: the other's first name)
        private static readonly string[] Envy =
        {
            "{name}'s coat cost more than my year. Wears it like it's nothing.", "Look at {name}. Never done an honest day's work and fed better than me.",
            "{name} has new boots. Where does {name} find the money? Not from working."
        };
        private static readonly string[] Pity =
        {
            "{name} is in the bottle again. Somebody ought to walk {name} home.", "Poor {name}. Drink gets its hooks in, and it never lets go."
        };
        private static readonly string[] Wary =
        {
            "Keep clear of {name}. Got that look again.", "{name} is spoiling for something. Not me. Not today."
        };
        // what the town knows of them lately: the one who put a man's face in the bar, the one caught with a hand in a coat
        private static readonly string[] KnownBully =
        {
            "That's {name}. Put a man's face through the bar top. Keep walking.", "Don't catch {name}'s eye. Folks who catch {name}'s eye end up picking teeth off the floor."
        };
        private static readonly string[] KnownBullyHot =
        {
            "So that's {name}. Let {name} try that with me. Just let {name} try."
        };
        private static readonly string[] KnownThief =
        {
            "{name}'s the one they caught with a hand in a coat. Hand on my purse.", "Count your change twice when {name}'s about, they say."
        };
        private static readonly Dictionary<string, string[]> Watching = new Dictionary<string, string[]>
        {
            { "Proud", new[] { "Look at {name}, preening. Some folks were born to be looked at, and know it.", "{name} talks like a man who's never been told no.",
                               "{name} nodded at me like a king to a stable boy. I'll remember the nod." } },
            { "Loyal", new[] { "{name} looks done in. Ought to ask after {name}.", "Haven't seen {name} smile in a week. Something's wrong at home.",
                               "If {name} needs a hand with that roof, I'll go Saturday. Nobody asked. Doesn't matter." } },
            { "Drifter", new[] { "{name} will be standing right there when I'm long gone. Rooted like a fence post.", "Wonder if {name} ever wanted to just walk away.",
                                 "{name} knows every name in this town. I know the names of three roads out." } },
            { "Cold", new[] { "{name} talks too much. Always has.", "{name} wants something. Folks only smile like that when they want something.",
                              "{name} cried at a funeral last month. Didn't know the man. Some folks just like to be seen." } },
            { "Hothead", new[] { "{name} keeps looking over here. Let {name} look. See what it gets.", "{name} laughed just now. At me? Better not have been at me." } },
            { "Greedy", new[] { "{name} owes somebody money. You can tell by the shoes.", "{name} would sell a sick mule as a racehorse. I'd know.",
                                "Wonder what {name} paid for that hat. Too much. Folks like {name} always pay too much." } },
            { "Coward", new[] { "{name} keeps looking at me. Did I do something? I didn't do anything.", "Stand behind {name} if trouble starts. {name} is wider than me." } },
            { "", new[] { "Wonder what {name} does all day. Never once seen {name} in a hurry.", "{name} laughs at everything. Must be nice.",
                         "{name} looks like {name}'s mother. Same chin. Same scowl.", "{name} has a new way of walking. Somebody's in love, or in debt.",
                         "{name} said good morning yesterday. Didn't today. What changed?" } }
        };

        // a mood that carries: what happened to them today spills over into everything else (mood-congruent thinking)
        private static readonly string[] Sore =
        {
            "Everything's grating today. Even the church bell sounds sour.", "Snapped at the dog. Dog didn't deserve it. Nobody does, today.",
            "Feel like a fist looking for a wall.", "Whole day's gone crooked. Like a picture nobody straightens."
        };
        private static readonly string[] Glad =
        {
            "Good day. Can't say why. Won't look at it too hard in case it runs off.", "Caught myself whistling. Haven't whistled since spring.",
            "Even the flies seem friendly today.", "Folks are kinder than I give them credit for. Some folks."
        };

        /// How today has left them: "sore" (roughed up, robbed, sent off empty-handed, made a fool of in drink, in the
        /// last six hours), "glad" (a coin given or taken gratefully, a kindness, a sweetheart, a quarrel mended), else
        /// null; with nothing of the kind, a foul day out of doors makes them sore one time in three.
        public static string MoodOf(Person p, Context day, string seed)
        {
            if (day == null)
                return null;
            SceneTie t = day.Ties != null && day.Now >= 0 ? day.Ties.FirstOrDefault(x => day.Now - x.At >= 0 && day.Now - x.At < 360) : null;
            if (t != null)
            {
                bool did = t.Did(p.Id);
                if ((t.Kind == "rough" || t.Kind == "pickpocket") && !did) return "sore";
                if (t.Kind == "scorn" && did) return "sore";      // asked, and was sent off
                if (t.Kind == "drunk" && did) return "sore";      // the morning after, and everybody grinning
                if (t.Kind == "charity" || t.Kind == "comfort" || t.Kind == "cheer" || t.Kind == "makeup" || t.Kind == "sweet" || t.Kind == "propose" || t.Kind == "flirt") return "glad";
            }
            if (!day.Indoors && (day.Weather == "rain" || day.Weather == "storm" || day.Weather == "snow" || day.Weather == "dust") && Voice.Chance(seed + "wmood", 33))
                return "sore";
            return null;
        }

        // small joys and old memories: the moments a life is mostly made of, between the worries
        private static readonly string[] Pleasure =
        {
            "Smell of bread from somewhere. Whoever's baking, God keep them.", "First coffee of the day. Burnt my tongue. Worth it.",
            "Got a tune stuck in my head and I don't even know the words.", "Found a dime in the dirt. Small luck is still luck.",
            "Pipe smoke, and for a moment I'm nine years old in my father's barn.", "My mother sang while she scrubbed. Haven't thought of that in years.",
            "A dog followed me half the length of the street. Nobody's dog. Everybody's friend.", "Clean shirt today. Feel like a different man. A slightly better one."
        };

        /// A thought about what is happening around them now, or null: something they just saw (by far the loudest),
        /// someone else in sight, the weather if they're out in it, the place they're in.
        public static string NowThought(Person p, string seed, List<string> told, Context day)
        {
            if (day == null)
                return null;
            var options = new List<Func<string>>();
            Witnessed s = day.JustSaw;
            bool saw = s != null && s.Kind != null && BySeen.ContainsKey(s.Kind) && s.AId != p.Id && s.BId != p.Id && s.AName != null && s.BName != null;
            if (saw)
                options.Add(() => Pick(BySeen[s.Kind], p, seed, told).Replace("{a}", Person.FirstOf(s.AName)).Replace("{b}", Person.FirstOf(s.BName)));
            List<Nearby> others = day.Nearby != null ? day.Nearby.Where(n => n.Id != p.Id && !string.IsNullOrEmpty(n.Name)).ToList() : null;
            if (others != null && others.Count > 0)
            {
                Nearby n = others[Voice.Hash(seed + "nb") % others.Count];
                options.Add(() => Pick(Regard(p, n), p, seed + "nb2", told).Replace("{name}", n.Name));
            }
            string mood = MoodOf(p, day, seed);
            if (mood != null)
                options.Add(() => Pick(mood == "sore" ? Sore : Glad, p, seed + "mood", told));
            if (!day.Indoors && day.Weather != null && ByWeather.ContainsKey(day.Weather))
                options.Add(() => Pick(ByWeather[day.Weather], p, seed, told));
            if (day.Indoors && day.Place != null && ByPlace.ContainsKey(day.Place))
                options.Add(() => Pick(ByPlace[day.Place], p, seed, told));
            if (options.Count == 0)
                return null;
            // something seen just now comes first two times in three; otherwise any of what's there
            int i = saw && Voice.Chance(seed + "loud", 66) ? 0 : Voice.Hash(seed + "now") % options.Count;
            return options[i]();
        }

        /// How this person sees that one: envy of money, pity for a drunk, fear of a hothead (for the timid), else
        /// through their own temperament.
        private static string[] Regard(Person p, Nearby n)
        {
            string t = p.Personality ?? "";
            if (n.Known == "bully")
                return t == "Hothead" || t == "Proud" ? KnownBullyHot : KnownBully;
            if (n.Known == "thief")
                return KnownThief;
            if (n.Drunk && t != "Cold" && t != "Greedy")
                return Pity;
            if (n.Temper == "Hothead" && (t == "Coward" || t == "Loyal"))
                return Wary;
            if (n.WellOff && (t == "Greedy" || t == "Drifter" || TradeGroup(p.Trade) == "working" || TradeGroup(p.Trade) == "farm"))
                return Envy;
            string[] pool;
            return Watching.TryGetValue(t, out pool) ? pool : Watching[""];
        }

        /// Every line of the "now" band and the small pleasures, for the tests.
        public static IEnumerable<string> AllNowLines()
        {
            return ByWeather.Values.SelectMany(x => x).Concat(ByPlace.Values.SelectMany(x => x)).Concat(BySeen.Values.SelectMany(x => x))
                .Concat(Envy).Concat(Pity).Concat(Wary).Concat(Watching.Values.SelectMany(x => x)).Concat(Pleasure)
                .Concat(KnownBully).Concat(KnownBullyHot).Concat(KnownThief).Concat(Sore).Concat(Glad);
        }
    }
}
