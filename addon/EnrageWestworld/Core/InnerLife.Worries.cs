// The worry a person's life is going through: a debt, a sick child, a letter that doesn't come, a dry well, a
// son gone off to work, a courtship, a feud with a neighbour, the bank's note, the drink, money saved for a
// ticket, a bad tooth, a secret, the law. Each runs WorryDays game days (it begins, gets worse, comes to a head,
// ends well or badly), then another begins, and a new one now and then remembers how the last one ended.
// Nothing is saved: it all follows from who they are and the game day, so a counter keeper met again days later
// is further along, and anyone met for the first time is somewhere in the middle of something. The people in it
// are named once per person (the same child, the same son) or once per worry (this creditor, this sweetheart).
// All lines are original to this add-on.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static partial class InnerLife
    {
        public const int WorryDays = 8;

        private sealed class WorryKind
        {
            public string Key;
            public Func<Person, string, int, bool> Fits;   // the person, their trade group, their age
            public int GoodChance = 55;                   // percent that it ends well
            public string[] Begin, Worse, Crisis, Good, Bad, AfterGood, AfterBad;
        }

        private static readonly string[] NoFamily = { "trapper", "rough", "girl", "soldier", "card" };
        private static readonly string[] Settled = { "farm", "town", "old", "gentry", "working", "plain" };

        private static readonly WorryKind[] Worries =
        {
            new WorryKind
            {
                Key = "debt", Fits = (p, g, age) => true,
                Begin = new[] {
                    "Owe {who} eleven dollars. He asks about it kindly. Kindly is worse.",
                    "Borrowed from {who} for the seed. Seed's in the ground. Money's gone.",
                    "{who} lifted his hat outside the store. A greeting with a sum attached." },
                Worse = new[] {
                    "{who} asked after my health. Means he's asking after his money.",
                    "Counted the jar. Four dollars and a button. Owe {who} eleven.",
                    "Took the long way round to miss {who}'s porch. Third time this week." },
                Crisis = new[] {
                    "{who} wants it by Saturday. Said it plain, with a hand on my shoulder.",
                    "Could sell the saddle. Could ask my brother. Could leave town by night. Saturday's close." },
                Good = new[] {
                    "Paid {who} in full. Walked out lighter. Bought a cigar I can't afford.",
                    "Square with {who}. Slept the whole night. First time since spring." },
                Bad = new[] {
                    "Couldn't pay. {who} took the mule instead. Stood in the yard and watched it go.",
                    "{who} has my watch now. Keep reaching for it. Pocket's empty." },
                AfterGood = new[] { "Don't owe a soul in this town. Say it out loud sometimes, to hear it." },
                AfterBad = new[] { "Still see my mule in {who}'s field. He works it harder than I did." }
            },
            new WorryKind
            {
                Key = "fever", GoodChance = 65, Fits = (p, g, age) => age >= 22 && age <= 55 && !NoFamily.Contains(g),
                Begin = new[] {
                    "{who} coughed all night. Small cough. Small coughs grow.",
                    "{who} won't eat. Pushed the bread round the plate like a chore.",
                    "Felt {who}'s forehead before dawn. Warm. Told myself it was the blanket." },
                Worse = new[] {
                    "Fever's up. {who}'s cheeks look painted on.",
                    "Sat up with {who} till the lamp ran dry. Counting breaths.",
                    "Doctor wants two dollars to ride out. Two dollars." },
                Crisis = new[] {
                    "Doctor came. Looked a long time. Said tonight will tell.",
                    "Prayed over {who}'s bed. Bargained, really. Offered everything I've got." },
                Good = new[] {
                    "Fever broke at first light. {who} asked for eggs. I laughed like a fool.",
                    "{who}'s up and about. Thin as a rail and loud as ever. Thank God." },
                Bad = new[] {
                    "Buried {who} on the rise behind the house. Ground was hard. Dug anyway.",
                    "Small coffin. Smaller than you'd think. Carpenter wouldn't take money." },
                AfterGood = new[] {
                    "Still feel {who}'s forehead every night. Habit now. Can't stop.",
                    "Heard {who} cough in the yard and my heart stopped. Just the dust. Just the dust." },
                AfterBad = new[] {
                    "Set one plate too many again. Caught myself with it in my hand.",
                    "Somebody's child laughed in the street. Had to stop and hold the rail." }
            },
            new WorryKind
            {
                Key = "letter", Fits = (p, g, age) => true,
                Begin = new[] {
                    "No letter from {kin}. Clerk shook his head before I asked.",
                    "Wrote {kin} a month back. Paper and a stamp. Cost me a meal.",
                    "Every time the stage comes in I find a reason to be by the post office." },
                Worse = new[] {
                    "Second month. Nothing from {who}. Letters get lost. People do too.",
                    "Wrote {who} again. Shorter this time. Didn't say I was worried. Said it twice.",
                    "Postmaster knows my face now. Says nothing today before I open my mouth." },
                Crisis = new[] {
                    "Dreamed {who} was knocking at the door. Woke, and the door was open.",
                    "If {who} were dead, somebody would write. Wouldn't they write?" },
                Good = new[] {
                    "Letter from {who}! Well and fat. Three pages about a dog. Read it four times.",
                    "{who} wrote at last. Moved twice, never thought to say. Don't care. Alive." },
                Bad = new[] {
                    "Letter came. Black edge on the envelope. Didn't need to open it. Opened it anyway.",
                    "Stranger's hand on the envelope. {who} passed in the winter. They waited till now to say." },
                AfterGood = new[] { "Keep {who}'s letter in my coat. Paper's going soft at the folds." },
                AfterBad = new[] { "Still walk past the post office. Nothing coming. Walk past anyway." }
            },
            new WorryKind
            {
                Key = "drought", Fits = (p, g, age) => g == "farm",
                Begin = new[] {
                    "Well's down two feet. Dropped a stone and counted. Too long.",
                    "Corn's curling at the edges. Corn does that when it's thirsty. So do I.",
                    "Sky the color of a tin plate. Not a cloud since Sunday." },
                Worse = new[] {
                    "Hauling water from the creek. Two buckets, forty trips. The corn drinks it and asks for more.",
                    "Cow's ribs are showing. Mine too, if anybody looked.",
                    "Neighbour's well went dry. He came asking. I said no. Hated myself by supper." },
                Crisis = new[] {
                    "Dig the well deeper or sell the cow. Can't do both. Can't do neither.",
                    "Lay awake listening for rain. Heard a horse. Heard my own heart. No rain." },
                Good = new[] {
                    "Rain! Stood in it with my hat off like a fool. Mud to the ankles. Best day this year.",
                    "Dug six feet more and hit sweet water. Drank till my belly hurt." },
                Bad = new[] {
                    "Sold the cow to a man from {town}. Didn't haggle. Couldn't look at her.",
                    "Corn's dead. Walked the rows anyway. Like visiting a grave." },
                AfterGood = new[] { "Every cloud, I look up. Can't help it now." },
                AfterBad = new[] { "Field's bare where the corn was. Wind takes a little more topsoil every day." }
            },
            new WorryKind
            {
                Key = "son", GoodChance = 60, Fits = (p, g, age) => age >= 42 && g != "trapper" && g != "soldier" && g != "girl",
                Begin = new[] {
                    "{who} left for {place} in my good boots. Didn't ask. I'd have said yes.",
                    "Supper's too quiet with {who} gone to {place}.",
                    "Kept {who}'s chair by the stove. Nobody sits in it." },
                Worse = new[] {
                    "No word from {who}. Men get hurt out there and nobody writes the families.",
                    "Heard there was trouble at {place}. Heard it third-hand. Third-hand's enough.",
                    "Dreamed {who} was small again, in the creek, calling me. Woke in a sweat." },
                Crisis = new[] {
                    "Asked a man off the stage if he knew {who}. He didn't. Looked at me with pity. Pity's free.",
                    "Writing to {place} again. Third letter. Pressed so hard the pen tore the paper." },
                Good = new[] {
                    "{who}'s home! Taller. Broader. Smells of smoke and tobacco. Ate like three men.",
                    "{who} sent five dollars from {place}, and a note: don't fret. I fretted." },
                Bad = new[] {
                    "{who} came home on a cart. Leg's gone below the knee. Smiled at me anyway.",
                    "Man from {place} brought {who}'s things in a sack. Boots. A comb. My letters, unopened." },
                AfterGood = new[] { "{who} sleeps till noon now. Let him. He earned it." },
                AfterBad = new[] { "Keep {who}'s comb on the shelf. Dust it Sundays." }
            },
            new WorryKind
            {
                Key = "courting", Fits = (p, g, age) => age < 36 && g != "old" && g != "soldier",
                Begin = new[] {
                    "{who} smiled at me by the store. Or at somebody behind me. Turned round. Nobody there.",
                    "Wore my Sunday things on a Tuesday. In case I pass {who}. Passed nobody.",
                    "Can't stop hearing {who}'s laugh. Like a pump handle. I love it." },
                Worse = new[] {
                    "Walked past {who}'s house three times. Fourth time the dog knew me.",
                    "Said fine weather to {who}. It was raining. Wanted to die.",
                    "Somebody with good teeth was talking to {who} after church. Hate them already." },
                Crisis = new[] {
                    "Going to ask {who} to walk out Sunday. Practised on the horse. Horse said nothing.",
                    "Wrote {who} a letter. Burned it. Wrote it again. It's in my pocket, burning there." },
                Good = new[] {
                    "{who} said yes. Yes! Took the long way home so nobody'd see me grinning.",
                    "Walked out with {who} by the river. Didn't say one clever thing. Didn't need to." },
                Bad = new[] {
                    "{who} is promised to somebody from {town}. Heard it at the store, between the flour and the lamp oil.",
                    "Asked {who}. Got a kind no. The kind ones are the worst kind." },
                AfterGood = new[] { "{who} pressed a flower into my hand. Keep it flat in the Bible." },
                AfterBad = new[] { "Saw {who} in town with that other one. Nodded. Felt my face burn." }
            },
            new WorryKind
            {
                Key = "feud", Fits = (p, g, age) => Settled.Contains(g),
                Begin = new[] {
                    "{who} moved the fence post again. A foot onto my side. A foot is a foot.",
                    "{who}'s dog was in my hens. Three gone. Dog's fat. Fat dogs tell you things.",
                    "{who} didn't nod to me at church. I noticed. Everybody noticed me noticing." },
                Worse = new[] {
                    "Words with {who} over the fence. Said things my mother wouldn't credit.",
                    "{who} told the store I'm slow to pay. I'm not slow. I'm careful.",
                    "Moved the post back in the night. Felt like a thief on my own land." },
                Crisis = new[] {
                    "{who} stood in the road with his arms folded. I stood with mine. Sun went down on us both.",
                    "Next word from {who} gets answered with my hands. Mother would weep. Let her." },
                Good = new[] {
                    "Shook hands with {who}. Neither of us said sorry. Shook anyway.",
                    "{who}'s boy fell in the creek. I pulled him out. {who} brought a pie. That's that, then." },
                Bad = new[] {
                    "Not speaking to {who}. Won't again. Buy my nails in another town to miss his face.",
                    "Sheriff came about the fence. {who} lied. I lied better. Neither of us won." },
                AfterGood = new[] { "Lent {who} my saw. Got it back sharper. Strange world." },
                AfterBad = new[] { "{who}'s chimney smoke drifts over my roof. Even that galls me." }
            },
            new WorryKind
            {
                Key = "bank", Fits = (p, g, age) => g == "farm" || g == "town" || g == "old" || g == "gentry",
                Begin = new[] {
                    "Bank's note comes due in the spring. Spring always comes. That's the trouble with it.",
                    "Man from the bank rode past the house slow. Counting my windows.",
                    "Signed that paper in a good year. Every year since has been a bad one." },
                Worse = new[] {
                    "Letter from the bank. Polite. Polite letters are the ones with teeth.",
                    "Sold the clock to pay the interest. House is quieter without the tick. Worse.",
                    "Banker shakes my hand like he's weighing it." },
                Crisis = new[] {
                    "Notice nailed to the door. Read it three times. The words don't change.",
                    "Banker gave me to the end of the month. I thanked him. Thanked him. Fool." },
                Good = new[] {
                    "Bank gave me another year. Walked out into the sun and couldn't feel my feet.",
                    "Paid the note. Banker looked disappointed. Best face I've seen all year." },
                Bad = new[] {
                    "Bank has the house. Carried the stove out myself. Wouldn't let them touch it.",
                    "Strangers in my kitchen window. Bank sold it before the ink was dry." },
                AfterGood = new[] { "Keep the bank's receipt in my boot. Nowhere else is safe." },
                AfterBad = new[] { "Renting a room over the store. Ceiling's low. Hit my head every morning. Deserve it." }
            },
            new WorryKind
            {
                Key = "drink", GoodChance = 45, Fits = (p, g, age) => g != "soldier",
                Begin = new[] {
                    "Promised I'd quit the drink. Three days dry. Hands are telling everyone.",
                    "Walked past the saloon twice. Third time I'll go in. Counting is how it starts.",
                    "Mouth tastes of pennies. Hands won't hold still. Day three." },
                Worse = new[] {
                    "Dreamed of whiskey. Woke and could smell it. Swear it was in the room.",
                    "Beer smell from the saloon door. Crossed the street like it was a mad dog.",
                    "Keep my hands in my pockets. They give me away." },
                Crisis = new[] {
                    "One drink. One. A man can have one. That's what I said last time.",
                    "Bottle under the floorboard. Know which board. Haven't touched it. Haven't stopped thinking of it." },
                Good = new[] {
                    "Thirty days dry. Traded the bottle under the floor for a sack of coffee.",
                    "Clear head in the morning. Forgot what that was like. Sky looks bluer. Probably isn't." },
                Bad = new[] {
                    "Back at it. Woke in the alley with straw in my hair and somebody else's coat.",
                    "Drank the week's money in one night. Can't remember which night." },
                AfterGood = new[] { "Saloon piano going. Still know every song. Don't go in." },
                AfterBad = new[] { "Hair of the dog. Then the whole dog. Same as ever." }
            },
            new WorryKind
            {
                // the gambler and the rent (Tier 4): lost at the table, the room's rent due to his landlady
                Key = "cards", GoodChance = 40, Fits = (p, g, age) => !p.Woman && (g == "card" || g == "rough" || (g == "working" && (p.Personality == "Greedy" || p.Personality == "Drifter"))),
                Begin = new[] {
                    "Lost eleven dollars at the table last night. Rent's due Friday. Mrs. {who} counts the days.",
                    "Two pair. Two pair, and he turned over three nines. Can still see them.",
                    "Rent money's in my boot. Card game's in the back room. Only one of them is sense." },
                Worse = new[] {
                    "Lost again. One hand to win it back, I said. Seven hands.",
                    "Mrs. {who} knocked twice this morning. Kept still as a stone. She knew.",
                    "Sold my good boots for a stake. Feet know it." },
                Crisis = new[] {
                    "Rent's three days late. Mrs. {who} has a nephew. Big lad. Bigger every day.",
                    "Last dollar on the table tonight. Win, and I sleep indoors." },
                Good = new[] {
                    "Drew to an inside straight. Paid Mrs. {who} in silver and she smiled. Once.",
                    "Quit the table while I was ahead. First time in my life. Rent's paid." },
                Bad = new[] {
                    "Mrs. {who} put my things out in the street. Hat on top, like a joke.",
                    "My room's let to a drummer from Ohio. I'm sleeping at the livery." },
                AfterGood = new[] { "Deck of cards on the bar. Didn't touch it. Didn't." },
                AfterBad = new[] { "Livery loft's not so bad. Horses don't cheat at cards." }
            },
            new WorryKind
            {
                Key = "savings", GoodChance = 50, Fits = (p, g, age) => g == "working" || g == "girl" || g == "town" || g == "plain" || g == "card" || g == "trapper" || g == "farm",
                Begin = new[] {
                    "Saving for a ticket to {place}. Coffee can under the bed. Two dollars and change.",
                    "{place}. Say it under my breath while I work. Sounds like a church bell.",
                    "Skipped supper. Ten cents more in the can. {place} is made of skipped suppers." },
                Worse = new[] {
                    "Can's lighter than I left it. Counted twice. Somebody knows where I keep it.",
                    "Fare's gone up a dollar. Railroad must've heard I was coming.",
                    "Halfway to {place}. Halfway's the hardest. Can't go back. Can't go on." },
                Crisis = new[] {
                    "Enough in the can for the ticket. Or the doctor. Or the rent. Pick one.",
                    "Ticket agent asked one way, and I couldn't answer him." },
                Good = new[] {
                    "Bought the ticket. {place}. Holding it like it's warm.",
                    "Counted the last dollar into the can. It's enough. It's actually enough." },
                Bad = new[] {
                    "Can's empty. Lid's on the floor. Whoever it was, they knew.",
                    "Spent the ticket money on the doctor. {place} will still be there. Will I?" },
                AfterGood = new[] { "Ticket's in my coat. Haven't gone yet. Can't say why." },
                AfterBad = new[] { "Started a new can. Same bed. Same dream. Smaller can." }
            },
            new WorryKind
            {
                Key = "tooth", GoodChance = 60, Fits = (p, g, age) => true,
                Begin = new[] {
                    "Tooth on the left side throbbing like a second heart.",
                    "Chewing on the right side only. Bread's become a puzzle.",
                    "Clove oil and whiskey on the tooth. The whiskey worked better. On me, not the tooth." },
                Worse = new[] {
                    "Jaw's swollen. Folks ask if I've been in a fight. Wish I had. Something to hit.",
                    "Tooth woke me at three. Lay there hating everything with a shape.",
                    "Barber pulls teeth for a dollar. Seen him do it. Wouldn't let him trim a hedge." },
                Crisis = new[] {
                    "Tomorrow the barber, the pliers and the bottle. Tonight just the bottle.",
                    "Tied a string to the doorknob. Looked at it an hour. Untied it." },
                Good = new[] {
                    "Barber pulled it clean. Spat blood and laughed. Pain's gone. A gap's a fine thing.",
                    "Swelling's down. Ate a whole steak on the left side out of spite." },
                Bad = new[] {
                    "Barber broke it. Took him three tries. I took the bottle.",
                    "Lost the tooth and half my nerve. Jaw still aches when the weather turns." },
                AfterGood = new[] { "Tongue keeps finding the gap. Like a dog at a hole in the fence." },
                AfterBad = new[] { "Whistle through the gap when I talk. Children laugh. Let them." }
            },
            new WorryKind
            {
                Key = "secret", Fits = (p, g, age) => g != "soldier" && g != "law",
                Begin = new[] {
                    "Took two dollars from the till. Meant to put it back by now.",
                    "Nobody here knows the name I was born with. Want it kept that way.",
                    "Somebody from my old town came through today. Kept my hat low. Did he look twice?" },
                Worse = new[] {
                    "{town} is small. Small towns have long memories.",
                    "Storekeeper counted the till twice in front of me. Looked at me the second time.",
                    "Signed my old name on a receipt by mistake. Scratched it out till the paper tore." },
                Crisis = new[] {
                    "Man at the station asked had I ever lived in Missouri. Said no. Said it too fast.",
                    "Could tell them myself before somebody else does. Could. Won't." },
                Good = new[] {
                    "Put the money back. Nobody knows. Nobody will. Slept like a stone.",
                    "The man from my old town rode on. Didn't know me. God bless bad eyes." },
                Bad = new[] {
                    "They know. Heard it in how the preacher said good morning.",
                    "Lost my place over it. Didn't argue. Had no argument to make." },
                AfterGood = new[] { "Every stranger off the stage, I look at the face first. Every one." },
                AfterBad = new[] { "Folks are polite now. Polite and careful, like I'm a horse that kicked once." }
            },
            new WorryKind
            {
                Key = "law", Fits = (p, g, age) => g == "rough" || g == "card" || g == "trapper" || p.Personality == "Drifter",
                Begin = new[] {
                    "Poster on the sheriff's wall looks a bit like me. Cut my hair. Looks less like me now.",
                    "Rider asking questions at the livery. Good boots. Not from here.",
                    "Sleep with my boots on. Old habit. Habits keep a body alive." },
                Worse = new[] {
                    "Pinkerton in the saloon. Didn't drink his beer. Watched the door.",
                    "Sheriff said my name like he was tasting it.",
                    "Moved the saddlebags by the back door. Just in case. Just in case is a full-time job." },
                Crisis = new[] {
                    "Horse saddled behind the stable. Could ride tonight. Could ride every night.",
                    "New poster up. Better drawing. Somebody's been talking." },
                Good = new[] {
                    "Pinkerton left on the morning train. Watched him go from under my hat.",
                    "Poster's been papered over with a circus bill. Long live the circus." },
                Bad = new[] {
                    "Night in the sheriff's cell. Let me go. Wrote my name down. Wrote my face down too.",
                    "My old name on a poster by the church. Somebody knows. Somebody knows." },
                AfterGood = new[] { "Took my boots off to sleep last night. First time in a year. Didn't sleep anyway." },
                AfterBad = new[] { "Everybody's looking. Maybe nobody is. That's the trouble. Can't tell anymore." }
            }
        };

        private static readonly string[] Kin = { "my sister", "my brother", "Mother", "my cousin", "my brother", "my sister" };
        private static readonly string[] SonWent = { "the mines at Annesburg", "the railroad camps", "the army", "the lumber camps" };
        private static readonly string[] SavingFor = { "California", "Oregon", "San Francisco", "Chicago", "Seattle", "Montana" };

        // ---- which worry, how far along -----------------------------------------------------------------

        private static List<WorryKind> FitsFor(Person p)
        {
            string g = TradeGroup(p.Trade);
            // the calendar their birth year came from; someone without one (enRAGE's townsfolk) gets a steady 20 to 64
            int age = p.BornYear > 0 ? Voice.NowYear - p.BornYear : 20 + Voice.Hash(p.Id + "|age") % 45;
            return Worries.Where(w => w.Fits(p, g, age)).ToList();
        }

        /// The worry for this run of days; the one before it is different (unless only one fits).
        private static WorryKind KindAt(Person p, List<WorryKind> fits, long arc)
        {
            int raw = Voice.Hash(p.Id + "|wk" + arc) % fits.Count;
            int before = Voice.Hash(p.Id + "|wk" + (arc - 1)) % fits.Count;
            return fits[raw == before ? (raw + 1) % fits.Count : raw];
        }

        private static bool EndsWell(Person p, WorryKind k, long arc)
        {
            return Voice.Hash(p.Id + "|wg" + arc) % 100 < k.GoodChance;
        }

        /// Which run of WorryDays this game day falls in for this person, and the day within it (0..WorryDays-1).
        private static long ArcOf(Person p, long gameDay, out int dayIn)
        {
            long day = gameDay + Voice.Hash(p.Id + "|wo") % WorryDays;
            dayIn = (int)(day % WorryDays);
            return day / WorryDays;
        }

        /// 0 it begins (days 1-2), 1 worse (3-4), 2 the crisis (5-6), 3 how it ended (7-8).
        private static int StageOf(int dayIn)
        {
            return dayIn < 2 ? 0 : dayIn < 4 ? 1 : dayIn < 6 ? 2 : 3;
        }

        /// The person at the heart of it, and where: named the same way every time.
        private static string Fill(string line, Person p, WorryKind k, long arc)
        {
            if (line == null)
                return null;
            string who = null, kin = null, place = null;
            string linked = Counterpart != null && (k.Key == "debt" || k.Key == "feud" || k.Key == "courting") ? Counterpart(p, k.Key, arc) : null;
            switch (linked != null ? "linked" : k.Key)
            {
                case "linked": who = linked; break;
                case "debt": who = Residents.Surname(p.Id + "|cr" + arc); break;
                case "feud": who = Residents.Surname(p.Id + "|ne" + arc); break;
                case "fever": who = Residents.FirstName(Voice.Hash(p.Id + "|cw") % 2 == 0, p.Id + "|child"); break;
                case "son":
                    who = Residents.FirstName(false, p.Id + "|son");
                    place = SonWent[Voice.Hash(p.Id + "|sw") % SonWent.Length];
                    break;
                case "courting": who = Residents.FirstName(!p.Woman, p.Id + "|be" + arc); break;
                case "savings": place = SavingFor[Voice.Hash(p.Id + "|sf" + arc) % SavingFor.Length]; break;
                case "cards": who = Residents.Surname(p.Id + "|ll" + arc); break;   // his landlady
                case "letter":
                    string rel = Kin[Voice.Hash(p.Id + "|kin") % Kin.Length];
                    if (rel == "Mother")
                        who = kin = "Mother";
                    else
                    {
                        bool woman = rel == "my sister" || (rel == "my cousin" && Voice.Hash(p.Id + "|kw") % 2 == 0);
                        who = Residents.FirstName(woman, p.Id + "|kin");
                        kin = rel + " " + who;
                    }
                    break;
            }
            if (who != null)
                line = line.Replace("{who}", who);
            if (kin != null)
                line = line.Replace("{kin}", kin);
            if (place != null)
                line = line.Replace("{place}", place);
            return line;
        }

        /// A thought about the worry this person is living through today, or null if nothing fits them.
        public static string WorryThought(Person p, long gameDay, string seed, List<string> told)
        {
            if (p == null || p.Id == null || gameDay < 0)
                return null;
            List<WorryKind> fits = FitsFor(p);
            if (fits.Count == 0)
                return null;
            int dayIn;
            long arc = ArcOf(p, gameDay, out dayIn);
            int stage = StageOf(dayIn);
            if (stage == 0 && Voice.Chance(seed + "wa", 25))
            {
                // a new worry, and the last one still on their mind
                WorryKind last = KindAt(p, fits, arc - 1);
                if (LinkedDead(p, last, arc - 1))
                    return null;
                return Fill(Pick(EndsWell(p, last, arc - 1) ? last.AfterGood : last.AfterBad, p, seed, told), p, last, arc - 1);
            }
            WorryKind k = KindAt(p, fits, arc);
            if (LinkedDead(p, k, arc))
                return null;
            string[] pool = stage == 0 ? k.Begin : stage == 1 ? k.Worse : stage == 2 ? k.Crisis : EndsWell(p, k, arc) ? k.Good : k.Bad;
            return Fill(Pick(pool, p, seed, told), p, k, arc);
        }

        /// The worry this person is living through today: its kind ("debt"), the stage (0 it begins, 1 worse, 2 the
        /// crisis, 3 how it ended) and whether it ends well. False if nothing fits them.
        public static bool WorryNow(Person p, long gameDay, out string kind, out int stage, out bool good)
        {
            kind = null;
            stage = 0;
            good = false;
            if (p == null || p.Id == null || gameDay < 0)
                return false;
            List<WorryKind> fits = FitsFor(p);
            if (fits.Count == 0)
                return false;
            int dayIn;
            long arc = ArcOf(p, gameDay, out dayIn);
            WorryKind k = KindAt(p, fits, arc);
            if (LinkedDead(p, k, arc))
                return false;   // set aside: no scenes over it either
            kind = k.Key;
            stage = StageOf(dayIn);
            good = EndsWell(p, k, arc);
            return true;
        }

        /// The real person at the other end of a worry (a creditor, a neighbour, a sweetheart), when the engine has
        /// linked one (Links.cs): the name to put for {who}, or null for one made up. Only reads; never makes a link.
        public static Func<Person, string, long, string> Counterpart;

        /// Whether the real person linked to this worry has died since (the engine knows). A worry over someone dead
        /// is set aside for the rest of its run: no thoughts, no scenes. Their name stays, for anything already said.
        public static Func<Person, string, long, bool> CounterpartDead;

        private static bool LinkedDead(Person p, WorryKind k, long arc)
        {
            return CounterpartDead != null && k != null && (k.Key == "debt" || k.Key == "feud" || k.Key == "courting") && CounterpartDead(p, k.Key, arc);
        }

        /// Which run of WorryDays this game day is in for this person (by their id alone).
        public static long ArcFor(string id, long gameDay)
        {
            int dayIn;
            return ArcOf(new Person { Id = id }, gameDay, out dayIn);
        }

        /// The stage of this person's worry today (0 it begins .. 3 how it ended), by their id alone.
        public static int StageFor(string id, long gameDay)
        {
            int dayIn;
            ArcOf(new Person { Id = id }, gameDay, out dayIn);
            return StageOf(dayIn);
        }

        /// Whether a worry of this kind, in this run, ends well for this person (by their id alone).
        public static bool EndsWellFor(string id, string kind, long arc)
        {
            WorryKind k = Worries.FirstOrDefault(w => w.Key == kind);
            return k != null && EndsWell(new Person { Id = id }, k, arc);
        }

        /// A line about the worry this person is living through today, with its people put in ({who}, {kin},
        /// {place}): the same names their own thoughts use.
        public static string WorryFill(Person p, long gameDay, string line)
        {
            if (p == null || p.Id == null || gameDay < 0 || line == null)
                return line;
            List<WorryKind> fits = FitsFor(p);
            if (fits.Count == 0)
                return line;
            int dayIn;
            long arc = ArcOf(p, gameDay, out dayIn);
            return Fill(line, p, KindAt(p, fits, arc), arc);
        }

        /// What this person is going through, for the log and the F7 dump: "debt (Pruitt), worse, day 4 of 8".
        public static string WorryStatus(Person p, long gameDay)
        {
            if (p == null || p.Id == null || gameDay < 0)
                return "none";
            List<WorryKind> fits = FitsFor(p);
            if (fits.Count == 0)
                return "none";
            int dayIn;
            long arc = ArcOf(p, gameDay, out dayIn);
            WorryKind k = KindAt(p, fits, arc);
            int stage = StageOf(dayIn);
            string who = Fill("{who}|{place}", p, k, arc).Replace("{who}", "").Replace("{place}", "").Trim('|');
            string[] names = { "it begins", "worse", "the crisis", EndsWell(p, k, arc) ? "it ended well" : "it ended badly" };
            return k.Key + (who.Length > 0 ? " (" + who.Replace("|", ", ") + ")" : "") + ", " + names[stage] + ", day " + (dayIn + 1) + " of " + WorryDays;
        }

        /// Every worry line, with each kind's key, for the tests.
        public static IEnumerable<KeyValuePair<string, string>> AllWorryLines()
        {
            foreach (WorryKind k in Worries)
                foreach (string[] pool in new[] { k.Begin, k.Worse, k.Crisis, k.Good, k.Bad, k.AfterGood, k.AfterBad })
                    foreach (string line in pool)
                        yield return new KeyValuePair<string, string>(k.Key, line);
        }
    }
}
