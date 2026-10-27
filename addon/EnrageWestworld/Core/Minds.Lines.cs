// The words for memories (Minds.cs): by what it was (a group, or an exact scene kind first), how they knew it (did,
// saw), their part in it (doer, done), how far they've got with it (fresh, chewing, letting go) and, for blows and
// quarrels, their temperament (bold, fearful, cold, plain), so a coward and a hothead who saw the same fight never
// think the same line. "near": someone in it is in sight. Slots: {who} the other one (did), {a} and {b} the two
// seen, {town}. A name is as this person knows it: a first name, or how they looked ("a farmhand"). Short, one detail,
// the outside and the inside apart (Lex's writing brief). All lines are original to this add-on.
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static partial class Minds
    {
        internal static readonly Dictionary<string, string[]> Pools = new Dictionary<string, string[]>
        {
            // ---- blows: a brawl both were in -----------------------------------------------------------
            { "blows|did|fresh|bold", new[] { "Knuckles split on {who}'s jaw. Felt good. God help me, it felt good.", "{who} swung first. I swung last. That's the whole of it." } },
            { "blows|did|chewing|bold", new[] { "Still taste blood from that business with {who}. Copper and dirt.", "Folks step aside for me now. That's what a fight buys." } },
            { "blows|did|letting|bold", new[] { "Me and {who}, fists in the street. Seems a long way off now.", "Bruise on my knuckles has gone yellow. So has my temper." } },
            { "blows|did|fresh|fearful", new[] { "Hands won't stop shaking. {who} could've killed me.", "I hit {who}. Me. Don't know what came over me." } },
            { "blows|did|chewing|fearful", new[] { "Keeping to the back lanes. {who} might still be looking.", "Every loud voice and I flinch. Since {who}." } },
            { "blows|did|letting|fearful", new[] { "Haven't seen {who} in days. Thank the Lord for small mercies.", "Ribs barely ache now. The fright takes longer." } },
            { "blows|did|fresh|cold", new[] { "{who} wanted teaching. Lesson's done. Back to work.", "Wasted a good hour on {who}'s fists. Time is money." } },
            { "blows|did|chewing|cold", new[] { "Tore a good coat brawling with {who}. Somebody pays for the cloth.", "Men keep talking of the fight with {who}. Let them. Talk is cheap." } },
            { "blows|did|letting|cold", new[] { "{who}. Hardly think of it. Hardly.", "Knuckles healed. The grudge kept in a drawer, for later." } },
            { "blows|did|fresh|plain", new[] { "Brawling in the street like a boy. With {who}, of all people.", "Lip's split. Pride too. {who} won't let me forget it." } },
            { "blows|did|chewing|plain", new[] { "Ought to make it right with {who}. Ought to. Won't today.", "Mother would've had words about that fight. I can hear her yet." } },
            { "blows|did|letting|plain", new[] { "{who} and me. Mostly behind us now.", "A fight's a fight. Mostly forgotten, like last week's weather." } },
            { "blows|did|near", new[] { "There's {who}. Look away. Or don't.", "{who}'s across the way. My fists remember before I do." } },

            // ---- a face put into the bar: the one it was done to, and the one who did it ------------------
            { "rough|did:done|fresh|bold", new[] { "Face into the bar, the whole room watching. {who} won't live long enough to brag.", "Taste of varnish and blood. {who} will answer for that." } },
            { "rough|did:done|chewing|bold", new[] { "Nose still swollen. Every man in the saloon saw. Every one.", "Keep touching the knife at my belt. Thinking of {who}. Only thinking." } },
            { "rough|did:done|letting|bold", new[] { "Nose set crooked. A reminder of {who} every time I shave.", "Let {who} think it's forgot. Let him think it." } },
            { "rough|did:done|fresh|fearful", new[] { "My face. The bar. {who} didn't even look angry. That's the worst of it.", "Ears still ringing. Don't go back in there. Don't." } },
            { "rough|did:done|chewing|fearful", new[] { "Took the long way round so I wouldn't pass {who}.", "Can't take a drink without tasting that bar top." } },
            { "rough|did:done|letting|fearful", new[] { "I drink at home now. Quieter. Safer. Since {who}.", "Bruise is gone. I still sit facing the door." } },
            { "rough|did:done|fresh|cold", new[] { "{who} put my face in the bar. Noted. Everything is noted.", "Split lip. Loose tooth. I'll set a price on each." } },
            { "rough|did:done|chewing|cold", new[] { "{who} owes me a tooth. I keep accounts.", "Others laughed. I remember who laughed." } },
            { "rough|did:done|letting|cold", new[] { "That business with {who}. It'll keep. Debts keep.", "Not angry. Patient. There's a difference." } },
            { "rough|did:done|fresh|plain", new[] { "Slammed my head on the bar like a sack of meal. {who}, of all men.", "Can't hear right in the left ear since {who} had his go." } },
            { "rough|did:done|chewing|plain", new[] { "Everyone saw. All {town} will know by supper.", "Ought to tell the sheriff about {who}. He'd only laugh." } },
            { "rough|did:done|letting|plain", new[] { "Swelling's gone down. The shame is slower.", "Haven't been back to that saloon. Don't miss it. Much." } },
            { "rough|did:done|near", new[] { "{who}'s here. My face goes hot all over again.", "There's {who}. Keep the bar between us." } },
            { "rough|did:doer|fresh", new[] { "Put {who}'s face in the bar. He'll mind his mouth now.", "Heard {who}'s nose go. Didn't care for the sound. Didn't stop." } },
            { "rough|did:doer|chewing", new[] { "Wrist still sore from holding {who} down.", "A few men looked at me different after {who}. Good." } },
            { "rough|did:doer|letting", new[] { "{who}. Hardly recall what he said to set me off.", "Shouldn't have done {who} like that. Shouldn't have." } },
            { "rough|did:doer|near", new[] { "{who} won't meet my eye. That's as it should be.", "There's {who}, nose and all." } },

            // ---- a fight seen ------------------------------------------------------------------------------
            { "blows|saw|fresh|bold", new[] { "{a} and {b}, going at it. Neither would last a minute with me.", "Watched {a} and {b} scrap. Itched to join in." } },
            { "blows|saw|chewing|bold", new[] { "{a} fights like a mule kicks. All noise.", "Still thinking how I'd have put {b} down quicker." } },
            { "blows|saw|letting|bold", new[] { "That set-to between {a} and {b}. Poor show, all told.", "Town's quiet again since {a} and {b}. Too quiet." } },
            { "blows|saw|fresh|fearful", new[] { "{a} and {b}, fists up in the road. Kept my head down. Kept walking.", "Blood on the boards. Heart going like a rabbit's." } },
            { "blows|saw|chewing|fearful", new[] { "Saw what {a} did to {b}. Don't cross {a}. Not ever.", "Keep seeing that fight when I shut my eyes." } },
            { "blows|saw|letting|fearful", new[] { "Town's calm again. For now. Since {a} and {b}.", "Thought of that fight less today. Getting better." } },
            { "blows|saw|fresh|cold", new[] { "{a} and {b} brawling. Bad for business, brawling.", "Somebody will pay for that broken chair. Not me." } },
            { "blows|saw|chewing|cold", new[] { "{a} lost a day's wage to that fight. Fool.", "Watched {b} after. Limping. Weak, and the whole street saw it." } },
            { "blows|saw|letting|cold", new[] { "{a} and {b}. Old news. Nobody profits by old news.", "Fights come and go. Accounts stay." } },
            { "blows|saw|fresh|plain", new[] { "{a} and {b} going at it like dogs. Grown men.", "Never seen {a} so red in the face. Or {b} so pale." } },
            { "blows|saw|chewing|plain", new[] { "Wonder what set {a} and {b} off. Somebody always knows.", "Wiped a fleck of blood off my boot. Not mine." } },
            { "blows|saw|letting|plain", new[] { "That business with {a} and {b}. Half the town's forgot.", "Fists in the street. Seems a long while back." } },
            { "rough|saw|fresh|bold", new[] { "{a} put {b}'s face in the bar. I'd have done it cleaner.", "Saw {b} go down on the bar top. Didn't look away. Didn't want to." } },
            { "rough|saw|fresh|fearful", new[] { "{a} took {b} by the neck and slammed him down. Just like that.", "The sound {b}'s head made on the bar. Can't stop hearing it." } },
            { "rough|saw|fresh|cold", new[] { "{a} broke {b} on the bar. Barman will want paying for the glass.", "{b} had it coming, likely. Most do." } },
            { "rough|saw|fresh|plain", new[] { "{a} slammed {b}'s face down on the bar. Glasses jumped.", "Saw {b} go down. Nobody lifted a hand. Nor did I." } },

            // ---- words between them ----------------------------------------------------------------------
            { "quarrel|did|fresh|bold", new[] { "{who} spoke to me like I was dirt. Won't happen twice.", "Should've hit {who}. Words weren't enough." } },
            { "quarrel|did|chewing|bold", new[] { "Keep thinking of things I should've said to {who}. Good ones, too.", "{who} better not look at me sideways today." } },
            { "quarrel|did|letting|bold", new[] { "{who} and that mouth. Not worth the trouble.", "Hard words with {who}. Seems smaller now." } },
            { "quarrel|did|fresh|fearful", new[] { "Raised my voice to {who}. Why did I do that.", "{who} looked fit to kill. Over a few words." } },
            { "quarrel|did|chewing|fearful", new[] { "Crossed the street when I saw {who}. I know what that makes me.", "Keep going over what I said to {who}. Every word worse." } },
            { "quarrel|did|letting|fearful", new[] { "{who} nodded to me today. Nodded. Maybe it's over.", "Words with {who}, and nothing came of it. Nothing yet." } },
            { "quarrel|did|fresh|cold", new[] { "{who} shouted. I didn't. I won.", "Let {who} shout. Shouting's free. Grudges aren't." } },
            { "quarrel|did|chewing|cold", new[] { "{who} thinks the matter's closed. It isn't.", "Remember every word {who} said. Every one." } },
            { "quarrel|did|letting|cold", new[] { "{who}. Small mind, loud voice.", "That quarrel. Filed away with the rest." } },
            { "quarrel|did|fresh|plain", new[] { "Words with {who}. Stomach's in a knot.", "Shouldn't have said that to {who}. True, though." } },
            { "quarrel|did|chewing|plain", new[] { "Ought to make peace with {who}. Not yet. Not yet.", "Kept hearing {who}'s voice while I worked." } },
            { "quarrel|did|letting|plain", new[] { "Had words with {who}. Can't rightly recall what about.", "{who} and me. Same as ever, near enough." } },
            { "quarrel|did|near", new[] { "{who}. Don't catch that eye.", "There's {who}, pretending not to see me." } },
            { "quarrel|saw|fresh", new[] { "{a} and {b} going at each other. Half the street listening.", "Heard {a} shout at {b}. Everyone heard." } },
            { "quarrel|saw|chewing", new[] { "{a} and {b}. That's been coming a while.", "Somebody ought to talk sense into {a}." } },
            { "quarrel|saw|letting", new[] { "{a} and {b} have quieted down. For now.", "That old quarrel, {a} and {b}. Town's tired of it." } },

            // ---- a slight: turned away, sent off, avoided -------------------------------------------------
            { "slight|did|fresh", new[] { "Cold words between me and {who}.", "{who} wouldn't hear me out." } },
            { "slight|did|chewing", new[] { "What has {who} got against me? Can't think.", "Cheeks still burn when I think of {who}." } },
            { "slight|did|letting", new[] { "{who} can keep that nose in the air.", "Small thing, {who}'s slight. Small things stick." } },
            { "slight|did:done|fresh", new[] { "{who} looked right through me. Like I wasn't there.", "Turned away. {who} turned me away." } },
            { "slight|did:doer|fresh", new[] { "Sent {who} off with nothing. Felt bad after. A little.", "Couldn't face {who} today. Walked the other way." } },
            { "slight|did:doer|chewing", new[] { "Was I too hard on {who}? Maybe.", "{who} looked hurt. Nothing I can do about that." } },
            { "slight|did:doer|letting", new[] { "{who}. Hardly think of it now.", "Some days you've nothing to give. That was one." } },
            { "slight|did|near", new[] { "{who}'s here. Wonderful.", "There's {who}. Chin up. Walk on." } },
            { "slight|saw|fresh", new[] { "{b} gave {a} the cold shoulder. Plain as day.", "Saw {a} turned away. Poor thing." } },

            // ---- a hand in a coat ------------------------------------------------------------------------
            { "pickpocket|did:doer|fresh", new[] { "Caught with my hand in {who}'s coat. Like a schoolboy.", "Ran from {who}. Lungs still burning." } },
            { "pickpocket|did:doer|chewing", new[] { "Every lawman looks at me twice now. Every one.", "{who} will have told the whole town by now." } },
            { "pickpocket|did:doer|letting", new[] { "{who} has stopped telling it. Or I've stopped listening.", "Lord forgive me, it was only a purse. I never got it." } },
            { "pickpocket|did:done|fresh", new[] { "{who} had a hand in my coat. In broad daylight.", "Caught {who} at my pocket. Heart's still racing." } },
            { "pickpocket|did:done|chewing", new[] { "Keep patting my coat. Still there. Still there.", "Should've held {who} for the law. Should've." } },
            { "pickpocket|did:done|letting", new[] { "Sewed a new pocket inside the coat. Thanks to {who}.", "{who} and the light fingers. Tell everybody I meet." } },
            { "pickpocket|did|near", new[] { "That's {who}. Hand on my purse.", "There's {who}. Watching me back, I'll bet." } },
            { "lift|did:doer|fresh", new[] { "{who}'s purse, light as a feather. Easy. Too easy.", "Hand in {who}'s coat and out again. Heart in my mouth." } },
            { "lift|did:doer|chewing", new[] { "Spent {who}'s money already. Gone like smoke.", "{who} hasn't missed it yet. Or has, and said nothing." } },
            { "lift|did:doer|letting", new[] { "{who} never knew. Or never said.", "Lord forgive me, it was only a purse." } },
            { "lift|did:done|fresh", new[] { "Purse gone. Somewhere between the store and here.", "Coins gone from my coat. Didn't feel a thing." } },
            { "lift|did:done|chewing", new[] { "Somebody in {town} has my money and a clean face.", "Keep looking at folk's hands now. Every one of them." } },
            { "lift|did:done|letting", new[] { "Lost a purse in {town}. Lesson learned, dear as it was.", "Money's gone. Stopped looking for it in every face." } },
            { "pickpocket|saw|fresh", new[] { "{a} caught with a hand in {b}'s coat. Bold as brass.", "Saw {b} grab {a}'s wrist. Right at the pocket." } },
            { "pickpocket|saw|chewing", new[] { "Watching {a}'s hands from now on.", "Patted my coat twice since. {a}'s doing." } },
            { "pickpocket|saw|letting", new[] { "{a}, the light-fingered one. Town won't forget.", "That business with {a}'s fingers. Old news." } },

            // ---- a drunk calling a man out -------------------------------------------------------------
            { "drunk|did:doer|fresh", new[] { "Head's splitting. Did I call {who} out? I think I did.", "Woke on the boards. Whiskey and sawdust." } },
            { "drunk|did:doer|chewing", new[] { "{who} has me down for a fool. Can't say he's wrong.", "Swearing off drink. Again." } },
            { "drunk|did:doer|letting", new[] { "That night with {who}. Best forgot.", "Few days dry. Hands shake less." } },
            { "drunk|did:doer|near", new[] { "{who}'s here. Look at the floor. Same floor I lay on.", "There's {who}. Does he remember? Course he does." } },
            { "drunk|did:done|fresh", new[] { "{who}, drunk as a lord, calling me out. Then flat on his face.", "Had to laugh at {who}. Had to." } },
            { "drunk|did:done|chewing", new[] { "Wonder if {who} remembers any of it. Doubt it.", "Smell of {who}'s breath. Still in my nose." } },
            { "drunk|did:done|letting", new[] { "{who}'s sober now, they say. For now.", "Poor {who}. Whiskey's a hard master." } },
            { "drunk|saw|fresh", new[] { "{a} called {b} out, then fell flat. Whole saloon laughed.", "Drunk as a fiddler, {a}. Sad to watch." } },
            { "drunk|saw|chewing", new[] { "{a} will pay for that whiskey twice. Once at the bar.", "Somebody ought to keep {a} from the bottle." } },
            { "drunk|saw|letting", new[] { "{a}'s been quieter since that night.", "Whiskey makes fools of better men than {a}." } },

            // ---- warmth: a kind word, a smile, a quarrel mended ------------------------------------------
            { "warmth|did|fresh", new[] { "{who} smiled at me. Can't stop thinking of it.", "Warm in the chest. {who}'s doing." } },
            { "warmth|did|chewing", new[] { "Wonder if {who} thinks of me. Hope so.", "Straightened my collar twice. In case {who} comes by." } },
            { "warmth|did|letting", new[] { "{who}. A pleasant thing to think on.", "Kind words with {who}. Small, but it carried me." } },
            { "warmth|did|near", new[] { "There's {who}. Stand up straight.", "{who}'s near. Act natural. Natural." } },
            { "propose|did|fresh", new[] { "Asked {who}. Asked outright. Lord, my knees.", "Said it to {who}. Said all of it. Can't take it back now." } },
            { "propose|did|chewing", new[] { "Keep turning over {who}'s face when I asked.", "Ring money. Need ring money. For {who}." } },
            { "makeup|did|fresh", new[] { "Made it right with {who}. Weight off my chest.", "Shook hands with {who}. Like a stone rolled off." } },
            { "makeup|did|chewing", new[] { "Easier to walk past {who}'s door now.", "Odd, not being sore at {who}. Feels like a missing tooth." } },
            { "charity|did:doer|fresh", new[] { "Gave {who} what I could. Not much. Something.", "Coin to {who}. Felt it leave the pocket. Felt better for it." } },
            { "charity|did:doer|chewing", new[] { "Hope {who} ate today.", "{who} had the look of someone who hadn't eaten. I knew that look once." } },
            { "charity|did:doer|letting", new[] { "Giving to {who} did me more good than {who}, maybe.", "{who}. Wonder how it's going for them." } },
            { "charity|did:done|fresh", new[] { "{who} gave me a coin. Didn't have to.", "Somebody in {town} still has a heart. {who}." } },
            { "charity|did:done|chewing", new[] { "Bread from {who}'s coin. Best I've had in a week.", "Ought to pay {who} back. Will. Someday." } },
            { "charity|did:done|letting", new[] { "{who}. Won't forget it. Won't.", "One kind face in {town}. {who}'s." } },
            { "scorn|did:done|fresh", new[] { "Asked {who} for a coin. Got a look like I was dirt.", "{who} told me to find work. As if I hadn't looked." } },
            { "scorn|did:doer|fresh", new[] { "{who} wanted a coin. Should get work like the rest of us.", "Sent {who} off. If I give to one, I give to all." } },
            { "warmth|saw|fresh", new[] { "{a} and {b}, sweet as pie. Nice to see, for once.", "Saw {a} make {b} laugh. Haven't laughed like that in years." } },
            { "warmth|saw|chewing", new[] { "{a} and {b}. Wedding by summer, mark me.", "Wonder what {a} sees in {b}. Or the other way." } },
            { "warmth|saw|letting", new[] { "{a} and {b}. Old news, sweet news.", "Folk still talk of {a} and {b}. Let them." } },

            // ---- talk ----------------------------------------------------------------------------------
            { "talk|did|fresh", new[] { "Passed the time with {who}. Nothing said, much. Nice, though.", "{who} talks like a creek runs. Never stops." } },
            { "talk|did|chewing", new[] { "{who} said something that stuck. Can't think what.", "Must remember what {who} told me." } },
            { "talk|did|letting", new[] { "Talked with {who} a while back. Pleasant enough.", "{who}. Good for a word in passing." } },
            { "talk|did|near", new[] { "{who} again. Bound to talk my ear off.", "There's {who}. Nod and keep moving." } },
            { "talk|saw|fresh", new[] { "{a} and {b}, jawing again.", "Wonder what {a} and {b} find to talk about." } },
            { "gossip|did|fresh", new[] { "{who} had news. Not all of it true, I'd wager.", "Swapped stories with {who}. Gave a little, got more." } },
        };

        /// Every memory line, for the tests and the overlap checks.
        public static IEnumerable<string> AllLines()
        {
            return Pools.Values.SelectMany(x => x);
        }
    }
}
