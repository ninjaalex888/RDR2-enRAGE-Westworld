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
                "Heard {b}'s face hit the wood from across the room. Sound like a dropped melon.",
                "{a} moved so fast. One moment talking, next {b}'s face is in the bar.",
                "Nobody stopped {a}. Nobody ever stops a man like {a}. I didn't either.",
                "{a} had {b} by the collar before anybody blinked.",
                "Never heard a man make the sound {b} made. Hope I never do again.",
                "{a} wiped his hands after. Like he'd been gutting a fish.",
                "Whole room went quiet when {a} grabbed {b}. Then louder than before.",
                "Could've been me in {a}'s hands. Could've been anybody.",
                "{b} sat there after, holding his face. Nobody went to him at first.",
                "Barkeep just looked at {a}. Didn't say a word. Neither did I.",
                "{a} has a temper on him like a struck match.",
                "Blood on the wood where {b}'s face went. I keep looking at it.",
                "Should somebody fetch the sheriff for {a}? Not me. Not tonight.",
                "Glad I was sat at the far end, away from {a}.",
                "{b} won't be eating supper easy tonight." } },
            { "brawl", new[] {
                "{a} and {b} going at it like dogs. Heart's still pounding and it wasn't even my fight.",
                "Blood on the boards. Somebody'll want to sweep that.",
                "{a} and {b} rolling in the dirt like schoolboys.",
                "Somebody's hat went under a horse. Think it was {b}'s.",
                "{a} threw the first one. I'd swear to it.",
                "Never knew {b} had a punch like that in him.",
                "Folks cheered {a} and {b} on. Folks are no better than dogs, some days.",
                "Heard a tooth go. {a}'s or {b}'s, couldn't say.",
                "Took two men to pull {a} off {b}.",
                "My hands were fists the whole time and it wasn't even my quarrel.",
                "{b} got up slow. {a} got up grinning.",
                "Dust everywhere. {a} and {b} both brown with it.",
                "Ma would've thrown a bucket of water on {a} and {b} both.",
                "Whole street'll have it by supper. {a} and {b}." } },
            { "drunk", new[] {
                "{a}, out cold on the floor, and him fixing to fight the world.",
                "Laughed at {a}. Shouldn't have. Could be me one day, a bottle further on.",
                "{a} went down like a tree in a storm. Floor won that one.",
                "{a} wanted to fight the whole room. The whole room laughed.",
                "Smelled the whiskey on {a} from the door.",
                "{a} tried to stand, twice. Sat back down, twice.",
                "{b} just looked at {a} like a man looks at a dog that's been sick.",
                "Somebody ought to see {a} home before he hurts himself.",
                "Drink makes a fool of the best of them. {a}'s no best of them.",
                "{a} called {b} a coward and then fell on his face.",
                "Felt sorry for {a}, after the laughing stopped.",
                "{a}'ll wake on that floor and remember none of it. Lucky.",
                "Pa drank like {a} does. Never ended well.",
                "{b} walked off and left {a} to it. Can't blame {b}." } },
            { "pickpocket", new[] {
                "Thief! Right in front of me. Hand in {b}'s coat, quick as a snake.",
                "Checking my pockets now. Twice. Everybody is.",
                "Saw the hand go in {b}'s coat and come out quick.",
                "{a}'s fingers moved like a card sharp's.",
                "{b} never felt it. That's the frightening part.",
                "Thief, in daylight, bold as you like. {a}, of all people.",
                "My purse is in my hand now and staying there.",
                "{a} looked right at me after. Knew I'd seen.",
                "Should I tell {b}? Should I? Lord, what if {a} sees me telling.",
                "Moved to the other side of the street from {a}.",
                "{a} dressed so respectable, too.",
                "Can't trust a crowd now. Every elbow's a thief.",
                "{b} shouted after {a}. Too late. Always too late.",
                "Patted every pocket I've got. All there. Still feel robbed." } },
            { "argue", new[] {
                "{a} and {b} going at each other. Pretended not to listen. Heard every word.",
                "Raised voices. The whole street leaned in.",
                "{a} and {b} at it hammer and tongs.",
                "Heard {b}'s name and some words my mother wouldn't care for.",
                "{a}'s face was red as a brick.",
                "Folks pretend not to listen to {a} and {b}. Everybody listens.",
                "Whatever's between {a} and {b}, it's been brewing a while.",
                "Dog started barking when {a} raised their voice.",
                "{b} gave as good as got. Better, maybe.",
                "Hope {a} and {b} settle it with words and leave it there.",
                "Couldn't follow the half of it. Something about a fence, I think.",
                "{a} walked off muttering. {b} stood there shaking.",
                "Bad blood between {a} and {b}. Everybody knows it now.",
                "Glad it's not me {a}'s shouting at." } },
            { "standoff", new[] {
                "Guns out, right there in the street. My knees haven't stopped.",
                "Held my breath the whole time. Still holding it, nearly.",
                "Hands on their guns, {a} and {b}. My hands were in my pockets.",
                "Thought for sure somebody was going to die in that street.",
                "Children pulled indoors. Shutters banged. Then nothing.",
                "{a} looked like he meant it. {b} too.",
                "Sweat running down my back and it isn't even hot.",
                "Somebody said fetch the sheriff. Nobody went.",
                "When it ended, half the street let out a breath at once.",
                "{a} and {b}, staring like a pair of tomcats.",
                "Never seen a man's hand shake like {b}'s. Mine did too.",
                "Pride. All that for pride.",
                "Lord, I thought I'd be seeing a funeral this week." } },
            { "flirt", new[] {
                "{a} sweet-talking {b} in broad daylight. Some people have no shame. Good for them.",
                "{a} has a way with words. {b} seemed to think so.",
                "Saw {b} blush clear to the ears.",
                "Young folk. Were we ever that foolish? I was.",
                "{a} leaning on the post like a dandy for {b}'s sake.",
                "Smiled to myself, watching {a} and {b}. First smile today.",
                "Wonder if {b}'s people know about {a}.",
                "Spring in the air, and it's not even spring.",
                "{a} said something and {b} laughed like a bell.",
                "Reminded me of courting days. Long gone, those.",
                "{b} looked back over their shoulder at {a}. That's the sign.",
                "Gossip'll have {a} and {b} married by Sunday.",
                "Envy {a} a little. Just a little." } },
            { "propose", new[] {
                "{a} went down on one knee to {b}, right there. Never seen the like." } },
            { "snap", new[] {
                "{a} bit {b}'s head clean off. Something's eating at {a}.",
                "{a} bit {b}'s head off for no cause I could see.",
                "Something's eating {a} up from the inside.",
                "{b} only said good day. {a} near took it as a slap.",
                "Never seen {a} so short with anybody.",
                "Trouble at home, I'd guess. {a}'s got that look.",
                "{b} looked hurt. Walked off with their hat in their hands.",
                "Whatever's weighing on {a}, it's heavy.",
                "Gave {a} a wide berth after that.",
                "Poor {b}. Caught the end of somebody else's storm.",
                "{a}'s eyes were red. From drink or from worry.",
                "Should somebody ask after {a}? Somebody braver than me.",
                "Bad day for {a}. Worse for anyone near." } },
            { "cheat", new[] {
                "{b} called {a} a cheat over the cards. Never seen chairs go back so fast.",
                "{b} saw the card up {a}'s sleeve. Whole table saw it then.",
                "Chairs scraped back so fast I thought the floor would go.",
                "{a}'s been winning all week. Now we know why.",
                "Never sit at a table with {a}. Learned that free tonight.",
                "Money on the floor and nobody reaching for it.",
                "{b} said cheat out loud. Brave or foolish.",
                "{a} swore on his mother. His mother'd be ashamed.",
                "Barkeep had his hand under the bar the whole time.",
                "Thought guns would come out over {a}'s cards.",
                "Saw {a}'s hand shake picking up his money.",
                "Cards are the devil's prayer book, Ma said. {a} proved her right.",
                "Glad I lost early and went to the bar." } },
            { "jealous", new[] {
                "Jealous men and loaded tempers. Saw the whole thing, and wished I hadn't." } },
            { "debt", new[] {
                "{a} calling in money from {b}, out in the open. That's a shame on both of them.",
                "{a} calling in money from {b} in front of half the street.",
                "Money owed and tempers short. {a} and {b} both.",
                "{b} looked about ready to sink through the boards.",
                "Never borrow from {a}. Seen what it costs.",
                "{a} had a ledger out. A ledger. In the street.",
                "Whatever {b} owes {a}, it's more than money now.",
                "Glad my debts are only to the store.",
                "{b} promised Friday. {a} didn't look like they believed it.",
                "Hate to see neighbours fall out over dollars.",
                "{a}'s within their rights. Still ugly, doing it out here.",
                "Felt for {b}. Everybody's short this season.",
                "There'll be more words between {a} and {b} before this is done." } },
            { "charity", new[] {
                "{b} gave {a} money, just like that. Wish somebody'd give me money, just like that.",
                "{b} pressed money into {a}'s hand. Didn't make a show of it.",
                "Good to see. Don't see enough of it.",
                "{a} near cried, thanking {b}.",
                "Wish I'd been the one to help {a}. Wish I'd had it to give.",
                "{b}'s a better soul than most in this town.",
                "Felt warm watching {b} give. Felt cheap, too.",
                "{a} looked at the coin like it was gold. Maybe it was, to {a}.",
                "Kindness in the street. Rare as rain in August.",
                "Will remember {b} for that.",
                "{a}'s hard up. Didn't know how hard.",
                "Ought to do the same, next time I'm asked.",
                "{b} walked off before {a} could even finish thanking." } },
            { "scorn", new[] {
                "{b} turned {a} away cold. Could've spared a nickel. Could I have? Maybe.",
                "{b} sent {a} off like a stray dog. Ugly to watch.",
                "{a} only asked for a nickel. {b} acted like it was a horse.",
                "Shame burned on {a}'s face. Could see it from here.",
                "Hard times make hard people. {b}'s plenty hard.",
                "Should've given {a} something myself. Didn't.",
                "{b} has the money. Everyone knows {b} has the money.",
                "{a} walked off with their head down. Felt that in my chest.",
                "There but for the grace of God. {a} could be any of us.",
                "{b} laughed. Didn't need to laugh.",
                "Wonder what {a} will eat tonight.",
                "Turned away myself before {a} could ask me. Not proud of it.",
                "{b}'ll want kindness one day. Hope it's measured out the same." } }
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
                if (t.Kind == "charity" || t.Kind == "comfort" || t.Kind == "cheer" || t.Kind == "settled" || t.Kind == "makeup" || t.Kind == "sweet" || t.Kind == "propose" || t.Kind == "flirt") return "glad";
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
            string what;
            return NowThought(p, seed, told, day, out what);
        }

        /// The same, with what it was about: seen, nearby, mood, weather or place (null with no thought).
        public static string NowThought(Person p, string seed, List<string> told, Context day, out string what)
        {
            what = null;
            List<KeyValuePair<string, Func<string>>> parts = NowParts(p, seed, told, day);
            if (parts.Count == 0)
                return null;
            // something seen just now comes first two times in three; otherwise any of what's there
            int i = parts[0].Key == "seen" && Voice.Chance(seed + "loud", 66) ? 0 : Voice.Hash(seed + "now") % parts.Count;
            what = parts[i].Key;
            return parts[i].Value();
        }

        /// Each thing around them now that could be thought about, by name (seen, nearby, mood, weather, place), with
        /// the line made only when asked for. Minds (Minds.cs) weighs them one by one.
        public static List<KeyValuePair<string, Func<string>>> NowParts(Person p, string seed, List<string> told, Context day)
        {
            var parts = new List<KeyValuePair<string, Func<string>>>();
            if (day == null)
                return parts;
            var options = new List<Func<string>>();
            var names = new List<string>();
            Witnessed s = day.JustSaw;
            bool saw = s != null && s.Kind != null && BySeen.ContainsKey(s.Kind) && s.AId != p.Id && s.BId != p.Id && s.AName != null && s.BName != null;
            if (saw)
            {
                options.Add(() => Pick(BySeen[s.Kind], p, seed, told).Replace("{a}", Person.FirstOf(s.AName)).Replace("{b}", Person.FirstOf(s.BName)));
                names.Add("seen");
            }
            List<Nearby> others = day.Nearby != null ? day.Nearby.Where(n => n.Id != p.Id && !string.IsNullOrEmpty(n.Name)).ToList() : null;
            if (others != null && others.Count > 0)
            {
                Nearby n = others[Voice.Hash(seed + "nb") % others.Count];
                options.Add(() => Pick(Regard(p, n), p, seed + "nb2", told).Replace("{name}", n.Name));
                names.Add("nearby");
            }
            string mood = MoodOf(p, day, seed);
            if (mood != null)
            {
                options.Add(() => Pick(mood == "sore" ? Sore : Glad, p, seed + "mood", told));
                names.Add("mood");
            }
            if (!day.Indoors && day.Weather != null && ByWeather.ContainsKey(day.Weather))
            {
                options.Add(() => Pick(ByWeather[day.Weather], p, seed, told));
                names.Add("weather");
            }
            if (day.Indoors && day.Place != null && ByPlace.ContainsKey(day.Place))
            {
                options.Add(() => Pick(ByPlace[day.Place], p, seed, told));
                names.Add("place");
            }
            for (int i = 0; i < options.Count; i++)
                parts.Add(new KeyValuePair<string, Func<string>>(names[i], options[i]));
            return parts;
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
