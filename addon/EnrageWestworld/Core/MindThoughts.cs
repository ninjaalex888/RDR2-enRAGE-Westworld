// Thoughts from feelings and needs (the mind design, behind [Mind] MindLive; daytime run 2026-10-27, filled out in the
// night run of 2026-10-28). With MindLive on, a feeling still strongly felt (Appraisal, Mood.Lasting), then a pressing
// need (Needs), comes before the old bands of thought. The thought says what is really felt, even when the face hides
// it (Display): the gap between the two is what makes people read as human; when the face masks it, a `|masked` pool
// says the mask too ("Smile. Let him think it didn't land."). Pools by feeling and by whom it's about: the player
// ({you}, as this person sees him), someone they have a tie with ({who}, a first name), or anyone else; a feeling about
// oneself (shame, guilt, pride) by whoever did it to them. Each pool has twelve lines of different forms: an
// observation, a judgment, self-talk, a question, a plan, a regret, one thing bringing back another. No kin pools: no
// two living people in the world are known kin (Resident.Kin names a dead counter keeper's predecessor; enRAGE's KinId
// an avenger's dead). Grief has no pool for a stranger (it's felt only for someone cared for). Need lines by trade
// group and temper group where it tells (a farmhand's hunger isn't a clerk's), the plain pool for the rest; the rain
// only when it rains. Lex's brief: short fragments, one thing the body notices, words of 1899 America. All lines are
// original to this add-on. Plain C#, no game.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class MindThoughts
    {
        public static readonly string[] Whom = { "you", "stranger", "tie" };
        public const int PoolMin = 12, NeedMin = 10;

        internal static readonly Dictionary<string, string[]> Pools = new Dictionary<string, string[]>
        {
            { "anger|you", new[] {
                "{you}. Thinks this town's his to knock about.",
                "Shoulder still smarts where he put his hand. Walked off like it was nothing.",
                "Jaw's so tight my teeth ache. Breathe. Not here.",
                "Who does {you} think he is? Who?",
                "Next time he comes through that door I'll be standing by it.",
                "Should have said something. Stood there like a post.",
                "Same look my father's foreman had. Same hands.",
                "Blood's up. Count ten. Count it slow.",
                "Mud on his boots from somewhere else. Let him take it back there.",
                "Nobody lays a hand on me. Not in this town. Not him.",
                "Hands won't stop shaking. Not fear. Not fear.",
                "Let him laugh. The street's long and I know every yard of it." } },
            { "anger|you|masked", new[] {
                "Smile. Let him think it didn't land.",
                "Nod at him. Nod and keep the hands at my sides.",
                "Face says nothing. Teeth say plenty, behind the lips.",
                "Pleasant as Sunday. Inside, a dog on a chain.",
                "Nod to him, civil. The hand at my side wants other work.",
                "Answer civil. Count to ten in my head. Count to twenty.",
                "Smile for the street. Not here. Later, maybe. Later.",
                "A smile for him like a coat I put on. It doesn't fit." } },
            { "anger|stranger", new[] {
                "Don't know the name. I'll know the face.",
                "Hands shaking. Not from fear. Let it pass.",
                "Pulse in my ears like a drum. Hold still.",
                "What gives a man like that the right?",
                "Find out where he drinks. Then we'll see.",
                "Walked on. Should have turned. Too late now.",
                "That voice. Heard a voice like it the day the bank took the farm.",
                "Teeth set so hard they hurt.",
                "Somebody ought to take that one down a peg.",
                "Let it go. Can't. Let it go.",
                "Fists in my pockets so nobody sees them.",
                "Trouble like that doesn't stay put. It comes back around." } },
            { "anger|stranger|masked", new[] {
                "Nod, smile, walk on. Nobody need know what I'd like to do.",
                "Face like a shut door. Keep it shut.",
                "Easy. Easy. Let the face lie for me.",
                "Pleasant word for him. The other words stay down.",
                "Breathe through the nose. Keep the jaw loose. Nobody sees the jaw.",
                "Say good day the way the preacher would. Think what the devil would.",
                "Still hands. Still face. The fist stays in the pocket where it lives.",
                "Look past him, not at him. Looking at him would start it." } },
            { "anger|tie", new[] {
                "{who}, of all people. Hand's shaking on the counter.",
                "I stood up for {who} once. Never again.",
                "Heat in my neck every time I think of {who}'s face.",
                "What did I ever do to {who}? What?",
                "Next time {who} wants a favor, I'll be busy.",
                "Should have seen it coming with {who}. Didn't.",
                "Years of knowing {who}. Years. Thrown over a trifle.",
                "{who} knew. {who} knew and did it anyway.",
                "Blood's up. Swallow it while the others are looking.",
                "Cross the street before {who} does. Save us both the trouble.",
                "Ears hot. Hands cold. {who}.",
                "Thought better of {who}. More fool me." } },
            { "anger|tie|masked", new[] {
                "Smile at {who}. Let {him} think it went by me.",
                "Pass {who} the salt like nothing happened. Nothing happened.",
                "Civil to {who}. Civil as a church. Inside, a wasp in a jar.",
                "Keep my face. {who} doesn't get the pleasure of mine.",
                "Good morning to {who}, and mean every word of the weather.",
                "Help {who} with the gate. Hands on the gate, not on {him}.",
                "Keep the voice low for {who}. Low voice, red face. {who} won't look up.",
                "{who} talks and I nod. The nod costs more than {he}'ll ever know." } },
            { "fear|you", new[] {
                "Don't look at {you}. Look at your boots.",
                "That iron on his hip. Don't give him a reason.",
                "Heart going like a rabbit's. Smile. Nod. Get home.",
                "What's {you} after? What have I got that he'd want?",
                "Around the corner, then the back way home. Quick.",
                "Should've stayed in this morning. Knew it. Felt it.",
                "The way he carries himself. Like the men who came for the horses that time.",
                "Mouth dry. Palms wet. Don't let it show.",
                "{you}'s eyes don't blink enough.",
                "Blood on his cuff. Whose?",
                "Stay by somebody. Anybody. Don't be alone with him.",
                "Knees. Hold, knees." } },
            { "fear|you|masked", new[] {
                "Steady voice. Steady hands. He mustn't see them shake.",
                "Laugh along. Laugh and edge toward the door.",
                "Chin up and say good day. Inside, running already.",
                "Bluster. Puff up. He's the kind that smells fear.",
                "Joke with him. Folk who joke aren't afraid. Keep joking.",
                "Busy with the hands. Busy folk aren't watched. Stay busy.",
                "Steady look at him, one moment. Enough to pass for nerve.",
                "Feet want the door. Feet can wait. Face first." } },
            { "fear|stranger", new[] {
                "Something's wrong over there. Feet want to run before I know why.",
                "Mouth's gone dry as chalk.",
                "Keep walking. Even pace. Don't turn around.",
                "Was that meant for me? Was it?",
                "Inside. Bolt the door. Wait it out.",
                "Should've carried the shotgun today. Should have.",
                "That shout. Same as the night the barn went up.",
                "Shaking. Hide it. Hide the hands.",
                "Trouble has a smell. Smelling it now.",
                "Not my business. Make it not my business.",
                "Pulse in my throat like a bird.",
                "Back to the wall. Count the ways out. Two." } },
            { "fear|stranger|masked", new[] {
                "Keep the face still. Nobody sees a thing.",
                "Whistle. Men who whistle aren't afraid. Whistle.",
                "Stand like a man who means to stay. Then leave.",
                "Light another one. Hands busy, hands steady.",
                "Lean on the post like it's any afternoon. Knees say otherwise.",
                "Count the coins in the pocket. Something to do with the hands.",
                "Talk about the weather to whoever's nearest. Loud. Steady.",
                "Say good evening like the road's empty. Listen to it like it isn't." } },
            { "fear|tie", new[] {
                "{who}'s capable of anything now. I've seen it.",
                "Keep clear of {who}. Skin crawls when {he}'s near.",
                "Door bolted twice tonight. {who} knows where I sleep.",
                "What's got into {who}? What's {he} become?",
                "Around by the church, not past {who}'s place. Longer, safer.",
                "Should have seen it in {who} years back. The signs were there.",
                "{who}'s voice just now. Like a dog before it bites.",
                "Hands cold. {who} did that.",
                "Don't be alone with {who}. Not any more.",
                "Pray {who} forgets me. Pray hard.",
                "Breath won't come right. {who}. Of all people.",
                "Never thought I'd fear {who}. Never." } },
            { "shame|you", new[] {
                "{you} did that to me, in front of everybody. Face hot as a branding iron.",
                "Couldn't meet his eye. Still can't.",
                "Ears burning. Cheeks too. Walk on.",
                "Why didn't I say something? Why?",
                "Out of town till it's forgotten. A week. A month.",
                "Stood there and took it. Took it.",
                "Same feeling as the schoolroom. Same hot face.",
                "Hands won't keep still. Put them in the pockets.",
                "Everybody heard. The whole street.",
                "Small. He made me small, and they watched.",
                "Don't look up. Don't look at any of them.",
                "Won't show my face in the saloon tonight." } },
            { "shame|you|masked", new[] {
                "Smile. Let him think it didn't sting.",
                "Brush the coat. Slow. Like a man who meant to sit down.",
                "Laugh it off. Laugh loud. Inside, something's crawling.",
                "Chin up and say nothing. The face can lie if the gut can't.",
                "Nod to him like it was nothing. It was everything. Nod anyhow.",
                "Tidy the collar. A tidy collar never had its face in the dirt.",
                "Say something about the price of flour. Anything. Fill the quiet.",
                "Pass him at a walk. The legs want a gallop." } },
            { "shame|stranger", new[] {
                "Everybody saw. Everybody.",
                "Brush off the coat. Brush off the rest of it.",
                "Chin up. Don't let them see it land.",
                "What must they think of me now?",
                "Home by the back lane. Nobody on the back lane.",
                "Should have held my tongue. Should have held a lot of things.",
                "Hot face. The same hot face as when Pa caught me at the jar.",
                "Neck's burning under the collar.",
                "A grown man, and I let that happen.",
                "They'll be telling it at supper. Every table.",
                "Eyes on the ground. Count the stones.",
                "Sleep won't come tonight. I know it." } },
            { "shame|stranger|masked", new[] {
                "Smile like it was a joke. It was a joke. Say it was.",
                "Straighten the hat. Steady. Nothing happened to anybody.",
                "Walk slow. Running is what shame looks like.",
                "Pleasant face. Burning ears. Nobody looks at ears.",
                "Look at the sky like there's weather coming. Let them look up too.",
                "Busy with the horse. Somebody with a horse to see to can't be laughed at.",
                "Hum a hymn. Nobody laughs at a body humming a hymn.",
                "Say good day to the next one along. Ordinary. Ordinary. Ordinary." } },
            { "shame|tie", new[] {
                "{who} did it loud, so the whole store would hear. It heard.",
                "Can't go in the store till {who}'s forgotten it.",
                "{who}, before everybody. Neck's burning under the collar.",
                "How do I look {who} in the eye after that?",
                "Keep clear of {who} a while. A good while.",
                "Should never have let {who} see me like that.",
                "Felt like a boy again, caught out. {who} the schoolmaster.",
                "Hands shaking on the counter. {who} saw them.",
                "{who} will tell it. {who} tells everything.",
                "Low as the floorboards, and {who} standing over it.",
                "Face hot. Don't look at {who}. Don't.",
                "Years of standing in this town. {who} took them in a minute." } },
            { "shame|tie|masked", new[] {
                "Smile at {who}. Say good day. Die a little.",
                "Steady hands for {who}. Shaking ones in the pockets.",
                "Laugh with {who}. Inside, the floor's gone.",
                "Face says all's well. {who} mustn't see otherwise.",
                "Ask {who} after the family. Talk of children covers anything.",
                "Nod at {who}'s story and hear none of it. The ears are burning too loud.",
                "Keep the hat on for {who}. A hat hides half a face.",
                "Pour {who} a cup. A hand that's pouring isn't seen to shake." } },
            { "guilt|you", new[] {
                "{you} trusted me with it. Trusted me.",
                "Took from {you} what wasn't mine. Hands won't keep still.",
                "Wronged {you}, and he never knew it. That's the part that gnaws.",
                "What would {you} say if he knew? What?",
                "Make it right with {you}. Somehow. Soon.",
                "Should have owned it then and there.",
                "Shook his hand on it. Can still feel the hand I lied to.",
                "Stomach turned over. Guilt's a stone you swallow.",
                "{you} was square with me. I wasn't.",
                "Can't look at {you} without the old thing rising.",
                "Lord, I knew better.",
                "Sleep wouldn't come. {you} in the dark, every time I shut my eyes." } },
            { "guilt|stranger", new[] {
                "Gave my word on that money. My word.",
                "Can't wash it off. Tried.",
                "Put it right. Somehow. Soon.",
                "What kind of man does a thing like that?",
                "First light, I go back and own it.",
                "Knew better. Did it anyhow.",
                "Mother would have known from my face. She always did.",
                "Stomach like a fist.",
                "Nobody saw. God saw.",
                "The money's gone and the wrong's still here.",
                "Every honest face in the street is an accusation.",
                "Pray on it. Praying's cheap. Pay on it." } },
            { "guilt|tie", new[] {
                "{who} deserved better from me. Supper sits like a stone.",
                "Next time I see {who}, I'll make it right.",
                "Gave {who} my word. Broke it.",
                "How do I face {who} now?",
                "Go to {who} tomorrow. Say it plain.",
                "Should have stood by {who}. Stood back instead.",
                "{who}'s face when it happened. Can't unsee it.",
                "Throat tight every time somebody says {who}'s name.",
                "Owe {who} the truth and more.",
                "{who} never did me a wrong. Can't say the same back.",
                "Lord knows {who} didn't have it coming.",
                "Not sleeping till it's squared with {who}." } },
            { "grief|tie", new[] {
                "{who}. Gone. Just like that.",
                "{who} was just here. Just here.",
                "Never did tell {who}. Now I can't.",
                "What do I do with {who}'s chair?",
                "Go and sit with {who}'s people after. Somebody should.",
                "Should have gone by {who}'s more. Meant to.",
                "Ears ringing like after a shot. {who}.",
                "Hands don't know what to do. Hold the hat.",
                "{who} laughed at my stories. Who laughs now?",
                "Cold all through, and the sun out.",
                "Say the name. {who}. Say it while it's still a name.",
                "Everything in this street still says {who}." } },
            { "grief|tie|masked", new[] {
                "Dry eyes for the street. Wet ones for later.",
                "Nod at the news. Nod. Fall apart at home.",
                "Say the right words about {who}. The real ones won't come.",
                "Work. Work the hands and the rest keeps quiet.",
                "Tell them {who} is in a better place. Tell it steady. Steady.",
                "Pour for the mourners. Steady hand on the pot. {who} liked it strong.",
                "Nod when they say how sudden. Sudden. The word sits like a stone.",
                "Laugh at the joke at the counter. {who} would have laughed. Laugh for both." } },
            { "pride|you", new[] {
                "{you} saw that. Good.",
                "Let him remember it. Hand's steady as a rail.",
                "Stood my ground. Right under his nose, too.",
                "Who's the big man now?",
                "Tell it at the saloon tonight. Twice.",
                "Took me long enough to find my spine. Found it.",
                "Pa would have nodded. Just once. That was his way.",
                "Chest a size bigger. Shirt feels it.",
                "{you} won't try that twice.",
                "Walked away taller than I came.",
                "Steady hands. Where did those come from?",
                "Let the whole street have seen it. Let them." } },
            { "pride|stranger", new[] {
                "Not bad. Chest's a size bigger. Not bad at all.",
                "Walk a little taller today.",
                "Let them talk about that for a change.",
                "Who'd have thought it of me? Who?",
                "Tell the wife. Leave out the shaking.",
                "Could have done it years ago. Should have.",
                "Same feeling as the day the first calf came. Mine.",
                "Hat sits different on a man's head after that.",
                "Somebody here, after all.",
                "The street knows my name tonight.",
                "Hands steady. Voice steady. Me.",
                "Earned the drink I'm about to buy." } },
            { "pride|tie", new[] {
                "{who} saw. Back's straighter for it.",
                "Bet {who} didn't think I had it in me.",
                "Put that in your pipe, {who}.",
                "What's {who} saying about me now? Something new.",
                "Stand {who} a drink. Let {him} see the hand that did it.",
                "Years of {who} looking down at me. Over.",
                "Mother always said {who} would see one day. Today.",
                "Warm in the chest. Warm as a stove.",
                "{who} nodded. {who} never nods.",
                "Let {who} tell it. {who} tells it better than I would.",
                "Hands still tingling.",
                "Not the small one in this town any more, {who}." } },
            { "gratitude|you", new[] {
                "Didn't have to do that. Did anyway.",
                "Owe {you} a kindness. Hand's still warm from his grip.",
                "Strange sort. Kind, though. Wouldn't have guessed it.",
                "Why would a man like that stop for me?",
                "Stand him a drink if I see him again. Two.",
                "Judged him by the gun. Wrong. Should know better by now.",
                "Nobody's stopped for me since Pa died. Then him.",
                "Throat tight. Nod. Can't say it.",
                "Mud to the knee and {you} the only one that moved.",
                "Won't forget his face. Not that face.",
                "A good turn from a hard man. The world's got surprises left.",
                "Thank him properly next time. Properly." } },
            { "gratitude|stranger", new[] {
                "Still decent folk about. One, anyhow.",
                "Didn't expect that. Not from that quarter.",
                "Pass it on. That's what Mother said.",
                "What did I do to deserve that? Nothing.",
                "Find out their name. A kindness should have a name.",
                "Nearly waved them off. Glad I didn't.",
                "Like the preacher's widow with the bread, that winter.",
                "Warm in the chest and the wind cold.",
                "A stranger, and kind. Mark the day.",
                "Somebody raised them right.",
                "Eyes stinging. The dust. Say it's the dust.",
                "Owe the world one now. Pay it Tuesday." } },
            { "gratitude|tie", new[] {
                "{who} stood by me. Won't forget it.",
                "Owe {who} more than thanks. Throat's too tight to say even that.",
                "Good friend, {who}. Better than I deserve.",
                "What would I do without {who}? Don't want to know.",
                "Have {who}'s fence mended by Saturday. Say nothing. Just do it.",
                "Never thanked {who} right the last time either.",
                "{who} was there when Ma was dying too. There again.",
                "Hand still warm from {who}'s grip.",
                "Friends like {who} don't come twice.",
                "{who} never asks for anything. That's the thing.",
                "Lord, keep {who}.",
                "Tell {who}. Plain words. Before it's too late to say them." } },
            { "envy|you", new[] {
                "Fine horse. Finer than anything I'll ever sit.",
                "Money in his pocket and not a care.",
                "Fine saddle, too. Never sat leather like that. Never will.",
                "Why him and not me? Why?",
                "Work a year and still not have his boots. Work anyway.",
                "Could have gone west myself once. Didn't.",
                "My brother had a horse like that. Lost it to cards.",
                "Sour taste in the mouth. Swallow it.",
                "{you} takes what he wants. Some men do.",
                "Same two hands as me. Different luck.",
                "Look away. Looking only makes it worse.",
                "He'll lose it all one day. Men like that do. Don't they?" } },
            { "envy|stranger", new[] {
                "Some folk are born lucky.",
                "What did they ever do to deserve it?",
                "Same mud on his boots as mine. Different pockets.",
                "Where's my luck? Where'd it go?",
                "Save harder. Save and never catch up.",
                "Married the wrong trade. Knew it the first winter.",
                "Smell the new leather from here. My pa smelled like that once.",
                "Sour in the mouth. Like bad milk.",
                "Easy to smile with money in the bank.",
                "Luck's not fair. Never said it was.",
                "Mind my own plate. Mine's full enough. Nearly.",
                "Watch them go by. Everybody watches them go by." } },
            { "envy|stranger|masked", new[] {
                "Nod and say how fine. Mean it about as far as the teeth.",
                "Smile for the lucky. Grit the back teeth for me.",
                "Good for them. Say it twice. Third time might take.",
                "Pleasant face. Sour heart. The face wins today.",
                "Admire the horse out loud. Price it in my head. Price myself after.",
                "Congratulate. Shake the hand. Hold it a breath too long, wanting what's in it.",
                "Ask where they got it, friendly. Ask like I could get one too.",
                "Smile at the luck. Chew it over at supper like gristle." } },
            { "envy|tie", new[] {
                "{who} again. Always {who}.",
                "What's {who} got that I haven't? Luck. Just luck.",
                "Smile for {who}. Grit my teeth for me.",
                "Why does it always fall to {who}?",
                "Work twice as hard as {who}. Still end up second.",
                "Should have taken that job {who} took. Was offered it first.",
                "{who} and I started even. Same schoolroom. Same mud.",
                "Sour in the mouth every time {who}'s name comes up.",
                "{who} doesn't even see it. That's the worst.",
                "Good for {who}. Suppose I mean it. Mostly.",
                "Bite the tongue. {who}'s still a friend. Still.",
                "One day the luck turns. Maybe not {who}'s. Maybe mine." } },
            { "envy|tie|masked", new[] {
                "Clap {who} on the back. Mean about half of it.",
                "Say well done to {who}. Say it with the teeth together.",
                "Smile for {who}. The eyes do what they like.",
                "Shake {who}'s hand. Hold the other one behind my back.",
                "Tell {who} nobody deserved it more. Name three who did, in my head.",
                "Ask {who} how it feels. Listen. Hate the listening.",
                "Raise a glass to {who}. Drink the glass down fast.",
                "Nod along at {who}'s good fortune. The nod's worn out by evening." } },
            { "pity|stranger", new[] {
                "Poor soul. Poor soul. Cheekbones like a starved dog's.",
                "Could be me, a bad winter from now.",
                "Somebody ought to help. Somebody.",
                "Where are their people? Nobody's got nobody.",
                "Leave a coin where they'll find it. No fuss.",
                "Walked past the last one. Not this one.",
                "Same look the Swede had the week before the fever got him.",
                "Chest aches to see it.",
                "No shoes to speak of. In this weather.",
                "There but for the grace of God.",
                "Hard world. Harder for some.",
                "Say a word to them. Costs nothing. Say it." } },
            { "pity|tie", new[] {
                "Poor {who}. Never had much luck, and less today.",
                "Go see {who} later. Just to sit.",
                "{who} looked so small just then.",
                "Is there a thing to be done for {who}? By anybody?",
                "Take {who} some supper tonight. Don't ask. Take it.",
                "Should have looked in on {who} last week. Knew things were bad.",
                "{who} used to laugh the loudest of any of us.",
                "Throat tight, watching {who}.",
                "{who} would never ask. So don't make {him}.",
                "Hard to watch {who} like that.",
                "Lord, go easy on {who}.",
                "Sit with {who} a while. Nobody should carry that alone." } },
            { "contempt|you", new[] {
                "Big man with a gun. Smells of horse and bad whiskey. That's all he is.",
                "Seen his kind before. Seen them hang, too.",
                "Loud. Men with nothing in them are always loud.",
                "Who does he think he's frightening? Children?",
                "Give him the road and the back of my head.",
                "Wasted a good day's thinking on him already.",
                "My uncle had that same strut. Died in a ditch.",
                "Lip curls by itself. Let it.",
                "{you}. Not worth the cartridge.",
                "All hat and no manners.",
                "Trouble like him blows through. The town stays.",
                "Don't look at him. Looking's more than he's owed." } },
            { "contempt|you|masked", new[] {
                "Good day to him. Civil as you like. Not an ounce more.",
                "Keep the lip still. He'd take the curl for fear.",
                "Polite. Polite costs nothing. He costs nothing too.",
                "Nod like he's a man. Know better.",
                "Say yes sir to him. The sir is for the street, not for him.",
                "Let him talk. Smile at the right places. Hear none of it.",
                "Good day and a nod. It's what you give any man. He'll take it for the real thing.",
                "Ask after his health like I'd mind the answer." } },
            { "contempt|stranger", new[] {
                "Trash. Pure trash.",
                "Some folk aren't worth the spit.",
                "Look at that. A grown man.",
                "Who raised that? Wolves?",
                "Steer wide of that one. Wide.",
                "Gave him the time of day once. Won't again.",
                "Same kind that hung around the depot drunk when I was a boy.",
                "Trash. Hold my breath till he's past.",
                "Loud, drunk and proud of it.",
                "A dog has more sense of place.",
                "Not worth a second look. Don't give him one.",
                "The town's got one of those in every street." } },
            { "contempt|tie", new[] {
                "{who} always was a fool.",
                "Done with {who}. Done.",
                "{who}. Same as ever. Still can't abide the smell of {him}.",
                "What did I ever see in {who}?",
                "Cross the street next time. Save the breath.",
                "Should have known {who} for what {he} is years back.",
                "{who}'s father was the same. Apple and tree.",
                "Lip curls at {who}'s name now.",
                "{who} doesn't know a thing and says it twice.",
                "Civil to {who}. Nothing more. Never more.",
                "Small man, {who}. Smaller every year.",
                "Let {who} talk. Talk's all {who} has." } },
            { "relief|you", new[] {
                "Breathing. That'll do.",
                "Thought it was done for me. {you} let it go.",
                "Legs like water. Sit a minute.",
                "Why'd he stop? Why? Don't care. He stopped.",
                "Home. Straight home. Lock it.",
                "Nearly said the wrong thing to {you}. Nearly.",
                "Like the time the ice held. Just held.",
                "Sweat going cold down the back.",
                "{you}'s gone on. Gone on. Thank God.",
                "Still here. Still standing.",
                "Never so glad to see a man's back.",
                "Breathe. Then breathe again." } },
            { "relief|stranger", new[] {
                "Over. It's over.",
                "Close. Too close.",
                "Hands still shaking. Doesn't matter. It's over.",
                "Was that it? Is it done?",
                "Sit down somewhere. Then go home.",
                "One more step this morning and it would have been me.",
                "Like the fever breaking that winter. Just gone.",
                "Knees have gone to water.",
                "Sky's the same sky. Looks different now.",
                "Thank the Lord. Twice over.",
                "Could laugh. Could cry. Do neither.",
                "Say nothing to anybody. Just breathe." } },
            { "relief|tie", new[] {
                "{who}'s all right. All right. Knees have gone to water.",
                "Thought I'd lost {who}.",
                "Never so glad to see {who}'s ugly face.",
                "What would I have done without {who}? Don't ask.",
                "Go put a hand on {who}'s shoulder. Just to be sure.",
                "Should never have let {who} go alone.",
                "Same as the night {who}'s boy came home from the war.",
                "Hands shaking. {who}'s all right. Shaking anyway.",
                "{who}. Here. Breathing. Enough.",
                "Lord, I'll pay you back for {who}. Somehow.",
                "Sit down before I fall down. {who}'s fine.",
                "Didn't know I was holding my breath till {who} spoke." } },
            { "joy|you", new[] {
                "Fine day, all told. Sun on my face and nothing owing.",
                "{you}, of all people. Well, well.",
                "Whistling. Who'd have thought.",
                "When did a stranger last make a day better? Today.",
                "Tell this one at supper.",
                "Nearly went the other way this morning. Glad I didn't.",
                "Like the first warm day after a long winter.",
                "Light on my feet. Boots feel new.",
                "{you}. A good one, under the dust.",
                "Laughed out loud at nothing. {you}'s doing.",
                "Smile won't come off. Let it stay.",
                "Good day. Good day. Say it again." } },
            { "joy|stranger", new[] {
                "Laughed out loud in the street. Folk looked. Let them.",
                "Could kiss a mule today.",
                "Light on my feet all of a sudden.",
                "What's got into me? Don't care.",
                "Buy the good tobacco today. Why not.",
                "Nearly missed it. Nearly stayed in.",
                "Like the day the rain came after the long dry.",
                "Grin so wide the face aches.",
                "A kind turn, and the whole street brighter for it.",
                "Hum. Hum the hymn. The quick one.",
                "World's not all bad. Not today.",
                "Sun's warmer than it was an hour ago. Swear it is." } },
            { "joy|tie", new[] {
                "Good old {who}. Could hug the man, whiskers and all.",
                "Have {who} over for supper. Soon.",
                "{who}. Couldn't ask better of anyone.",
                "What did I do to deserve a friend like {who}?",
                "Walk {who} home. Talk the whole way.",
                "Should see {who} more. Will.",
                "{who} and I, like boys again for a minute.",
                "Chest full and warm. {who} did that.",
                "{who}'s laugh. Best sound in this town.",
                "Luck's not all bad with {who} in it.",
                "Grinning like a fool. {who}'s fault.",
                "Tell {who} what {he} means. One of these days. Why not today." } },
        };

        // what a pressing need sounds like (NeedState's order: hunger, rest, warmth, money, safety, company, standing,
        // meaning; the vice by its name); warmth in the rain or snow out of doors from Wet, the rest at any hour
        internal static readonly string[][] NeedPools =
        {
            new[] { "Stomach growling loud enough to hear across the street.", "Could eat a boot. Laces and all.", "Smell of frying from somewhere. Torture.",
                    "When did I last eat? Yesterday? Yesterday.", "Bread and a bit of bacon. Soon. Please.", "Should have taken the biscuit when it was offered.",
                    "Hollow all the way down.", "Every window with a stove behind it is a cruelty.", "Supper's a long way off. Longer every minute.", "Teeth want something to do." },
            new[] { "Eyes like sand.", "Could sleep standing, like a horse.", "Bed. Any bed. A barn would do.",
                    "How long since I slept the night through?", "Finish this and lie down. Just this.", "Shouldn't have sat up so late. Never learn.",
                    "Legs like lead. Head like a stone.", "Yawning so wide my jaw cracks.", "World's gone soft at the edges.", "Lean on something. Anything. Just a minute." },
            new[] { "Can't feel my fingers.", "Cold's got right into my bones.", "Wind goes straight through this coat.",
                    "Will it ever be warm again? Ever?", "Find a fire. Any fire.", "Should've worn the other coat. The one with the lining.",
                    "Toes gone. Can't tell if they're there.", "Teeth going like a telegraph key.", "Breath hangs in front of me like smoke.", "Cold enough to crack a stone." },
            new[] { "Count it again. Still short.", "Rent Saturday. Wages Saturday. Pray they meet.", "One bad week from the poorhouse.",
                    "How did the others manage it? How?", "Sell the watch. Pa's watch. Maybe.", "Shouldn't have stood that round last week.",
                    "Pockets light as a prayer.", "Every coin spent twice over in my head already.", "Store man's face when I ask for credit. Dread it.", "Money. Money. Everything comes back to money." },
            new[] { "Keep something solid at my back.", "Every sound's a hammer cocking.", "Back to the wall. Eyes on the door.",
                    "Who's that? Who's that behind?", "Home before dark. No matter what.", "Shouldn't have come this way. Not after what happened.",
                    "Hair on my neck standing up.", "Hands won't stay out of my pockets.", "Every stranger's face is a question.", "Not safe. Nowhere's safe. Not any more." },
            new[] { "Nobody to talk to but the horse.", "Eating alone again.", "Wish somebody'd ask after me.",
                    "When did anybody last say my name kindly?", "Go to the saloon. Not for the drink. For the voices.", "Should have written to my sister. Months now.",
                    "House so quiet the clock sounds like a hammer.", "Empty chair across the table every night.", "A dog would do. Even a dog.", "Lonesome as a fence post." },
            new[] { "They're laughing. I know they're laughing.", "Hold my head up. Hold it up.", "Nobody remembers. Everybody remembers.",
                    "What do they say when I've gone by? What?", "Earn it back. Day by day. Earn it back.", "Should never have let it get that far.",
                    "Ears burning again. Can't stop them.", "Every nod feels like a favor.", "Small in this town. Smaller every day.", "Walk like a man they still respect. Pretend." },
            new[] { "What's it all for. Up, work, bed, again.", "Church on Sunday. Questions on Monday.", "Same road there and back, every day God sends.",
                    "Is this the whole of it? Is it?", "Do one thing today that matters. One.", "Should have gone west when I was young. Should have gone somewhere.",
                    "Hollow place behind the ribs. Not hunger.", "The years went somewhere. Can't find them.", "Everybody walking like they know why.", "Hands busy, heart idle." },
        };

        // the same needs where the trade (InnerLife.TradeGroup) or the temper group (Minds.TemperGroup) tells: tried
        // first, then the plain pool. Keys: "<need>|<trade group>" or "<need>|<temper group>".
        internal static readonly Dictionary<string, string[]> NeedByGroup = new Dictionary<string, string[]>
        {
            { "hunger|farm", new[] { "Could eat the horse's oats and thank him.", "Belly empty and the field half done.", "Dinner pail's been empty since noon.", "Smell of the neighbour's bacon over the fence. Cruel, that." } },
            { "hunger|working", new[] { "Belly empty and the whistle hours off.", "Shift's long on an empty gut.", "Bread's gone. Hours of hauling left.", "Counting the minutes to the pail. Counting them twice." } },
            { "hunger|gentry", new[] { "Dinner's late. The cook will hear of it.", "A gnawing. Unbecoming, and there it is.", "One does not say hungry. One says the hour is late.", "Luncheon was a cup of beef tea. It has not lasted." } },
            { "rest|working", new[] { "Back's done in. Rest of me's following.", "Shoulders don't come down any more.", "Ten hours of it and the hands still shake.", "Sit down and I'll not get up. So don't sit down." } },
            { "rest|farm", new[] { "Up before the birds again. Birds had the sense to stay in.", "Every bone knows what time I rose.", "Field's half done and so am I.", "Sleep comes in the saddle if I let it. Don't let it." } },
            { "rest|old", new[] { "Old bones want a chair.", "Tired in a way sleep doesn't mend. Not any more.", "Knees say sit. The day says not yet.", "Stairs again. Used to take them two at a go." } },
            { "warmth|trapper", new[] { "Cold like this kills slow. Keep moving.", "Fingers stiff on the knife. Bad sign.", "Breath freezing on the collar. Ten more miles of this.", "Snow over the boot tops. Build the fire early tonight." } },
            { "warmth|working", new[] { "Cold tools in cold hands. Skin'll tear.", "Stand by the forge a minute. Just a minute.", "Coal's for the boiler, not the men. Foreman said so.", "Wind through the shed wall like it isn't there." } },
            { "money|farm", new[] { "Seed's bought and the rain's not. Pray.", "Bank man's coming out Tuesday. Nothing to show him.", "One more bad harvest and the place is theirs.", "Tax man and the seed man both want paying. The corn's not asked." } },
            { "money|town", new[] { "The rent, the coal, the boots for the boy. Pick one.", "Store credit's run out and so has the month.", "The butcher looks at me kindly now. That's the worst sign.", "Payday's Friday and Friday's a long way off." } },
            { "money|card", new[] { "One good hand puts it right. One.", "The landlady counts the days louder than I do.", "Two dollars between me and the street. Two.", "Stake money's rent money. Only one of them can be." } },
            { "money|gentry", new[] { "The accounts don't balance. Unthinkable, and here they are.", "Appearances cost. Appearances must be kept.", "The tailor's bill again. One will have to be unwell when he calls.", "Sold the second horse. Told them it went lame." } },
            { "safety|fearful", new[] { "Hide. Somewhere small. Somewhere dark.", "Shaking so hard the teeth knock.", "Don't look at anybody. Don't be seen.", "Every footstep behind me is the one." } },
            { "safety|bold", new[] { "Let them come. Let them try.", "Hand near the belt. Just in case.", "Boots stay on tonight. Whatever comes, I meet it standing.", "Back to the wall and the door in view. Old habit." } },
            { "safety|cold", new[] { "Trust nobody. Never did. Less now.", "Count the men in the room. Count the ways out.", "Nobody gets near enough to matter.", "The gun's clean. That's what matters tonight." } },
            { "company|cold", new[] { "Don't need anybody. Keep saying it.", "Alone suits me. Mostly. Tonight, less.", "Talk's cheap and I don't buy.", "A dog would do. A dog doesn't ask." } },
            { "company|bold", new[] { "Where is everybody? A man wants an audience.", "Quiet's for the dead. Find somebody to talk at.", "Nobody to argue with. Poor evening.", "A room with nobody in it is a waste of a room." } },
            { "company|old", new[] { "Everybody I'd want to talk to is in the ground.", "Young folk walk right past. Like I was a fence.", "Used to be a full table. Used to be.", "Somebody to say good morning to. That's all. That's a lot." } },
            { "standing|bold", new[] { "I'll make them remember who I am.", "Nobody looks down at me. Nobody.", "Respect. It's owed. I'll collect.", "Walked past me without so much as a nod. That gets learned." } },
            { "standing|fearful", new[] { "Don't make it worse. Don't say a word.", "Keep small. Small gets forgotten.", "If I'm quiet enough they'll forget I was there.", "Head down. Hat down. Through the crowd and gone." } },
            { "standing|gentry", new[] { "One cannot be seen like this. One simply cannot.", "What the town says of one is all one has.", "One is looked at. One must be worth the looking.", "They will talk. Let them have nothing to say." } },
            { "meaning|old", new[] { "Outlived my use. Outlived most things.", "What's left to do? Wait. Wait's a poor trade.", "Put better men in the ground than me. Why them, not me?", "Church bell's for somebody younger these days." } },
            { "meaning|soldier", new[] { "Marched for something once. Can't recall what.", "The war gave a man a reason. Peace took it back.", "Took orders for ten years. Nobody gives them now, and I miss it.", "Saw good men die for a flag. The flag's in a drawer now." } },
            { "meaning|farm", new[] { "Plant, reap, plant. The land doesn't ask why.", "Dirt under the nails and nothing to show the Lord.", "Hail took it in an hour. Twelve months in an hour.", "The land was my father's. Will it be anybody's after me?" } },
        };
        private static readonly string[] Wet = { "Rain down my collar again.", "Boots soaked through. Feet like ice.", "Wet to the skin and no fire to dry at.",
                                                 "Will this rain ever stop? Will it?", "Get under something. Anything with a roof.", "Should have waited it out at the store.",
                                                 "Hat's a gutter. Shirt's a sponge.", "Water running down my spine. Every drop counts.", "Wet wool. Smell of it. Hate it.", "Even the horse looks miserable." };
        private static readonly string[] Drink = { "One drink. Just to steady the hands.", "Taste of whiskey already, and none had.", "Promised I'd stop. Promised myself.",
                                                   "What's one more? What's one?", "Saloon's on the way home. Everything's on the way home.", "Should never have had the first. Years ago.",
                                                   "Smell of the barrel and I'm thirsty as sand.", "Hands shake till the first glass. Then they don't.", "Tell the wife I was kept late. Again.", "Thirst with a mind of its own." };
        private static readonly string[] Cards = { "Luck's due to turn. Has to be.", "One hand. Just one.", "Owe too much to stop now.",
                                                   "What if the next hand's the one? What then?", "Sit down for an hour. Just an hour.", "Should have quit when I was up. Always say that.",
                                                   "Sound of the cards shuffling. Like rain. Like money.", "Fingers itch for the deck.", "Rent money's burning a hole.", "Tonight the cards fall right. Feel it." };
        private static readonly string[] NeedKeys = { "hunger", "rest", "warmth", "money", "safety", "company", "standing", "meaning" };

        /// A thought for a feeling about someone (whom: you, stranger, tie), with {you} and {who} filled; `used` says a
        /// line was already thought today (the next in turn is tried). `masked`: the face hides it (Display), so a line
        /// that says the mask too is tried first. Null for none left, or no pool (grief for a stranger).
        public static string Line(string feeling, string whom, string seed, string you, string who, Func<string, bool> used = null, bool masked = false)
        {
            string[] pool;
            if (masked && Pools.TryGetValue(feeling + "|" + whom + "|masked", out pool))
            {
                string line = Pick(pool, seed, used, you, who);
                if (line != null)
                    return line;
            }
            if (!Pools.TryGetValue(feeling + "|" + whom, out pool) && !Pools.TryGetValue(feeling + "|stranger", out pool))
                return null;
            return Pick(pool, seed, used, you, who);
        }

        /// A thought for a pressing need (an index into NeedState; the vice by its name). weather: out of doors as it is
        /// (null under a roof or unknown), so a wet line comes only in the rain or snow. `tradeGroup` (InnerLife.TradeGroup)
        /// and `temperGroup` (Minds.TemperGroup): their own lines first where the need has them, then everyone's.
        public static string NeedLine(int need, string vice, string seed, Func<string, bool> used = null, string weather = null, string tradeGroup = null, string temperGroup = null)
        {
            string[] pool = need == NeedState.Vice ? (vice == "cards" ? Cards : Drink)
                : need == NeedState.Warmth && (weather == "rain" || weather == "storm" || weather == "snow") ? Wet
                : need >= 0 && need < NeedPools.Length ? NeedPools[need] : null;
            if (pool == null)
                return null;
            if (need >= 0 && need < NeedKeys.Length && pool != Wet)
            {
                string[] own;
                if (tradeGroup != null && NeedByGroup.TryGetValue(NeedKeys[need] + "|" + tradeGroup, out own) || temperGroup != null && NeedByGroup.TryGetValue(NeedKeys[need] + "|" + temperGroup, out own))
                {
                    string line = Pick(own, seed, used, null, null);
                    if (line != null)
                        return line;
                }
            }
            return Pick(pool, seed, used, null, null);
        }

        private static string Pick(string[] pool, string seed, Func<string, bool> used, string you, string who)
        {
            int start = Voice.Hash(seed + "|mt") % pool.Length;
            for (int k = 0; k < pool.Length; k++)
            {
                string line = Fill(pool[(start + k) % pool.Length], you, who);
                if (line != null && (used == null || !used(line)))
                    return line;
            }
            return null;
        }

        private static string Fill(string line, string you, string who)
        {
            if (line.Contains("{you}"))
            {
                if (string.IsNullOrEmpty(you))
                    return null;
                line = line.Replace("{you}", you);
            }
            if (line.Contains("{who}"))
            {
                if (string.IsNullOrEmpty(who))
                    return null;
                // he or she of the one named, by the first name (item 10's rule; the slots {he} {him} {his}), as Minds.Fill has it
                string first = Person.FirstOf(who);
                line = Ties.Pronouns(line, Residents.IsWomansName(first) || Scenes.IsWomansName(first)).Replace("{who}", who);
            }
            return Minds.Sentences(line);   // a capital at every sentence, so a slot that opens one ("... {you} ...") reads right (the late review)
        }

        /// Every pool with its key, for the tests (the feeling pools, then need|group, then wet, drink, cards).
        public static IEnumerable<KeyValuePair<string, string[]>> AllPools()
        {
            foreach (var kv in Pools)
                yield return kv;
            for (int i = 0; i < NeedPools.Length; i++)
                yield return new KeyValuePair<string, string[]>("need|" + NeedKeys[i], NeedPools[i]);
            foreach (var kv in NeedByGroup)
                yield return new KeyValuePair<string, string[]>("need|" + kv.Key, kv.Value);
            yield return new KeyValuePair<string, string[]>("need|wet", Wet);
            yield return new KeyValuePair<string, string[]>("need|drink", Drink);
            yield return new KeyValuePair<string, string[]>("need|cards", Cards);
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (var kv in AllPools())
                foreach (string l in kv.Value)
                    yield return l;
        }
    }
}
