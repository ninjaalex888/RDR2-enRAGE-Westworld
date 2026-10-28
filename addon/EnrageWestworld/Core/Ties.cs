// What a street scene leaves between two people: a tie, kept once per pair, with the latest kind of scene, how warm
// or hostile it has been, how many times, and when. Kept only between people who can be met again (town regulars,
// residents already saved for something else, enRAGE's townsfolk), so a tie never saves anyone by itself. The two
// think about each other by name for a few days (InnerLife), and the rest of their town talks about it.
// All lines are original to this add-on.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace EnrageWestworld.Core
{
    [DataContract]
    public sealed class SceneTie
    {
        [DataMember(EmitDefaultValue = false)] public string A, B;           // ids: res:N for a resident, folk:... for one of enRAGE's townsfolk
        [DataMember(EmitDefaultValue = false)] public string AName, BName;   // their full names, so a thought never has to look them up
        [DataMember(EmitDefaultValue = false)] public string Town;
        [DataMember(EmitDefaultValue = false)] public string Kind;           // the latest scene between them: argue, brawl, flirt, smalltalk...
        [DataMember(EmitDefaultValue = false)] public int Warmth;            // the scenes added up: warm above 0, hostile below (-3..3)
        [DataMember(EmitDefaultValue = false)] public int Count;             // scenes between them
        [DataMember(EmitDefaultValue = false)] public long At;               // game minute of the latest
        [DataMember(EmitDefaultValue = false)] public int Stage;             // how far a story between them has gone (follow-up scenes)
        [DataMember(EmitDefaultValue = false)] public string Doer;           // for trouble one did to the other (rough, pickpocket, lift, drunk): who did it
        [DataMember(EmitDefaultValue = false)] public bool Courted;          // they were sweethearts (a flirt, sweet words, a proposal): a quarrel since leaves them at odds (Scenes.Mend)

        /// This one did it to the other (the attacker, the thief, the drunk who called him out).
        public bool Did(string id)
        {
            return Doer != null && Doer == id;
        }

        public bool Has(string id)
        {
            return A == id || B == id;
        }

        public string Other(string id)
        {
            return A == id ? BName : AName;
        }

        public string OtherId(string id)
        {
            return A == id ? B : A;
        }
    }

    public static class Ties
    {
        public const int Max = 300;

        /// What a scene does to the two in it: warmer above 0, colder below, 0 they know each other a little better;
        /// null for a scene that leaves nothing between them (a song, a tale, a pitch to a crowd, the player's
        /// business). The exchanges (Exchanges.cs) end as comfort, brushoff, snap, cheer, envy, makeup or sweet.
        public static int? Effect(string kind)
        {
            switch (kind)
            {
                case "argue": case "brawl": case "cheat": case "debt": case "standoff": case "snap": case "envy": case "drunk":
                    return -1;
                case "rough": case "pickpocket":
                    return -2;   // a face into the bar, or a hand caught in a coat, isn't soon forgotten
                case "lift":
                    return 0;    // the mark never saw who: nothing between them that he knows of (the thief knows)
                case "flirt": case "propose": case "sweet": case "comfort": case "cheer": case "charity": case "help": case "defend": case "round": case "apology": case "settled":
                    return 1;
                case "scorn": case "passby": case "faced":
                    return -1;   // asked for a coin and sent off with a flea in the ear; left lying in the street
                case "makeup":
                    return 2;   // a quarrel mended counts for more than one more kind word
                case "smalltalk": case "gossip": case "haggle": case "hosts": case "brushoff": case "avoid":
                    return 0;
                default:
                    return null;
            }
        }

        /// Friends, as opposed to two people one kind word apart: warmth 2 or more, or warm with two scenes between them
        /// (night 2026-10-28, item 10: one hand up or one round bought had everyone grieving, drinking over, condoling
        /// and taking up grudges for a man they'd met once).
        public static bool Friends(SceneTie t)
        {
            return t != null && (t.Warmth >= 2 || t.Warmth > 0 && t.Count >= 2);
        }

        /// Courting, as opposed to a friendly word.
        public static bool Romantic(string kind)
        {
            return kind == "flirt" || kind == "propose" || kind == "sweet";
        }

        /// Sweethearts now: their last scene a romantic one, or the making up after a quarrel (Scenes.Mend).
        public static bool Sweethearts(SceneTie t)
        {
            return t != null && (Romantic(t.Kind) || (t.Courted && t.Kind == "makeup"));
        }

        /// Sweethearts at odds: courting once, and their last meeting cross words, not yet past mending. (A gift turned
        /// down, or a courtship refused, is a brushoff: that ends it.)
        public static bool Cooled(SceneTie t)
        {
            return t != null && t.Courted && t.Warmth >= -1 && (t.Kind == "argue" || t.Kind == "snap" || t.Kind == "envy");
        }

        /// Notes a scene between two people: the tie between them, made or carried on. Null if the scene leaves none.
        public static SceneTie Note(List<SceneTie> all, string aId, string aName, string bId, string bName, string town, string kind, long now)
        {
            return Note(all, aId, aName, bId, bName, town, kind, now, Trouble(kind) ? aId : null);
        }

        /// A scene where one did something to the other: A (the first in the scene) is the one who did it.
        public static bool Trouble(string kind)
        {
            return kind == "rough" || kind == "pickpocket" || kind == "lift" || kind == "drunk" || kind == "charity" || kind == "scorn" ||   // (the asking is A's doing too)
                   kind == "help" || kind == "passby" ||   // A helped B up, or walked past B
                   kind == "defend" || kind == "faced" ||  // A stood up for B; A faced B down
                   kind == "round" ||                      // A stood B a drink
                   kind == "settled";                      // A paid B what was owed, in the street
        }

        public static SceneTie Note(List<SceneTie> all, string aId, string aName, string bId, string bName, string town, string kind, long now, string doer)
        {
            int? effect = Effect(kind);
            if (effect == null || aId == null || bId == null || aId == bId)
                return null;
            SceneTie t = all.FirstOrDefault(x => x.Has(aId) && x.Has(bId));
            if (t == null)
            {
                t = new SceneTie { A = aId, B = bId };
                all.Add(t);
            }
            bool keepRomance = Sweethearts(t) && !Romantic(kind) && effect.Value > 0;   // a kindness between sweethearts: still sweethearts
            // small talk, the paper, a nod between sweethearts (or sweethearts at odds) leaves what's between them standing
            bool keepKind = keepRomance || ((Sweethearts(t) || Cooled(t)) && effect.Value == 0 && kind != "brushoff");
            if (Romantic(kind) || Romantic(t.Kind))
                t.Courted = true;
            if (t.Count > 0 && !keepKind)
                t.Stage++;   // they meet again: their story moves on (Reasons)
            t.AName = t.A == aId ? aName : bName;
            t.BName = t.A == aId ? bName : aName;
            t.Town = town ?? t.Town;
            if (!keepKind)
                t.Kind = kind;
            if (doer != null || kind != "avoid")
                t.Doer = doer;   // keeping clear of him doesn't change who did what to whom
            t.Warmth = Math.Max(-3, Math.Min(3, t.Warmth + effect.Value));
            if (kind == "settled" && t.Warmth < 1)
                t.Warmth = 1;   // squared up: whatever the calling in left between them, it's mended
            t.Count++;
            t.At = now;
            while (all.Count > Max)
                all.Remove(all.OrderBy(x => x.At).First());
            return t;
        }

        /// This person's ties from the last few days, newest first.
        public static List<SceneTie> Of(IEnumerable<SceneTie> all, string id, long now, int days)
        {
            return all.Where(t => t.Has(id) && now - t.At >= 0 && now - t.At < days * 1440L).OrderByDescending(t => t.At).ToList();
        }

        /// The town's talk: ties in this town from the last two days worth talking about (warm or hostile), not
        /// the thinker's own, newest first.
        public static List<SceneTie> TalkOf(IEnumerable<SceneTie> all, string town, string notId, long now)
        {
            return all.Where(t => t.Town == town && t.Kind != "lift" && (t.Warmth < 0 || Romantic(t.Kind) || t.Kind == "makeup" || t.Kind == "charity" || t.Kind == "help" || t.Kind == "round" || t.Kind == "settled") && !t.Has(notId) && now - t.At >= 0 && now - t.At < 2 * 1440L)
                      .OrderByDescending(t => t.At).ToList();
        }

        // ---- what they think of each other --------------------------------------------------------------

        private static readonly string[] Quarrel =
        {
            "Shouldn't have said that to {other}. Or should have said worse. Can't decide which.",
            "Words with {other}. Keep having the argument again in my head, and winning it this time.",
            "{other} looked at me like I was dirt on a boot. I'll remember that face.",
            "{other} got the final say. I've had a hundred since, all in my head.",
            "Still hot in the face over {other}. Supper went cold while I stewed.",
            "Should've walked away from {other}. Didn't. Never do.",
            "{other}'s voice in my ears all afternoon. Like a fly at a window.",
            "Half the street heard me and {other}. Let them.",
            "I was right. {other} was louder. That's not the same thing.",
            "Tossed and turned over {other}. The pillow's no help.",
            "Next time {other} starts, I'll keep my mouth shut. I won't, but I'll mean to.",
            "{other} said I'd always been a fool. Always. Like {he}'d kept count.",
            "Words with {other} again. Same words. Same mud.",
            "Kicked the stove over {other}. The stove didn't deserve it.",
            "Thought of the right answer an hour later. {other} would've been struck dumb by it.",
            "{other} pointed at me. Pointed. Like a schoolmarm.",
            "Ma used to say hold your temper. {other} makes it slippery.",
            "Can't see {other}'s hat in a crowd without my jaw going tight.",
            "Whole quarrel over nothing, with {other}. Nothing's the worst kind.",
            "{other} won't let it lie. Neither will I, it seems.",
            "Gave {other} a piece of my mind. Haven't much to spare.",
            "Told the dog about {other}. The dog took my side.",
            "Bit my tongue with {other}. Then let go of it.",
            "Should I go to {other} and settle it? Pride says no. Pride's loud today.",
            "{other}'s face when I said it. Almost worth the rest.",
            "Ears still ringing from {other}'s shouting. Or mine.",
            "Saw {other} whispering after. About me, I'd bet a dollar.",
            "There's a stone in my boot, and its name is {other}.",
            "Cleaned the lamp chimney twice, thinking of {other}. It was clean the first time.",
            "{other} thinks {he} won. Let {him} think it."
        };

        private static readonly string[] Blows =
        {
            "Jaw still aches. {other} hits like a mule kicks.",
            "Knuckles split from {other}'s teeth. Worth it. Mostly.",
            "Came to blows with {other}. In the street, like boys. Can't look anybody in the eye.",
            "Lip's split. {other}'s eye is worse. That's something.",
            "Can't close my right hand. {other}'s skull is hard as a stump.",
            "Washed {other}'s blood off my knuckles. Some of it was mine.",
            "Rolled in the dirt with {other}. Grown men. Lord.",
            "{other} fights dirty. Thumb in the eye. I'll know next time.",
            "Ribs ache when I breathe. Every breath, {other}.",
            "Somebody pulled us apart, me and {other}. Good thing, maybe.",
            "Hat's ruined. Stepped on in the fight with {other}.",
            "{other} got the first one in. I got the last. I'll call that even.",
            "Tooth's loose. Keep touching it with my tongue. Keep thinking of {other}.",
            "Felt good, hitting {other}. Doesn't feel good now.",
            "Shirt torn at the shoulder. {other} owes me a shirt.",
            "Everyone saw me and {other} go at it. Everyone will tell it wrong.",
            "My hands still shake from it. Not fear of {other}. Not quite.",
            "{other} bled on my boots. Can't get the stain out.",
            "Should've kept my fists in my pockets. {other} wouldn't have.",
            "Raw meat on my eye, like Pa used to. Thinking of {other}.",
            "{other} dropped like a felled post. Got up like a mule.",
            "Ear's thick as a biscuit. {other}'s doing.",
            "Last time I fought, I was fourteen. {other} made me fourteen again.",
            "Saw {other} limping after. Don't feel as sorry as I should.",
            "Won't drink where {other} drinks. Not for a while.",
            "Swollen knuckles on the coffee cup. Every sip, {other}.",
            "Fighting {other} settled nothing. Never does. Felt like it would.",
            "Wonder if {other}'s lying awake too.",
            "Taste of blood all morning. Iron and dust and {other}.",
            "Next time {other} raises a hand, I'll be ready. Or gone."
        };

        private static readonly string[] Money =
        {
            "{other} and the money. Always the money between us now.",
            "Saw {other} buy a round like a rich man. With whose money, I wonder.",
            "{other} owes me, and walks around like the sun owes {him} a living.",
            "Counted what {other} owes. Counted it again. Same number.",
            "Lent {other} money. Should've lent {him} a rope.",
            "Every time {other} buys tobacco I add it up. That's my tobacco.",
            "{other} says next week. Said next week last month.",
            "Money sours everything. Sours me and {other}, anyhow.",
            "Would've given {other} the shirt off my back. Now I want the shirt back.",
            "{other} won't meet my eye since the money.",
            "Could use what {other} owes me. Rent's due Friday.",
            "{other}'s good for it. I keep telling myself. {other}'s good for it.",
            "Saw {other} at the card table. With my money, likely.",
            "Never lend to friends, Pa said. Should've listened. {other}.",
            "{other} talked fine words when {he} borrowed. Quiet now.",
            "Ink on the IOU's faded. {other}'s memory's faded faster.",
            "Five dollars is five dollars, {other}. Even to you.",
            "Lie awake doing sums. Every sum ends in {other}.",
            "{other} bought a new hat. A new hat.",
            "Might have to ask {other} plain. Hate asking.",
            "{other} paid half. Half's a start. Half's an insult.",
            "Money between neighbours is a fence nobody built on purpose. Me and {other}.",
            "{other} crossed the street when I came. Money does that.",
            "Should I let {other} keep it? Buy myself some peace?",
            "If {other} pays, I'll stand {him} a whiskey. If.",
            "Told the storekeeper about {other}'s debt. Shouldn't have. Did.",
            "Wonder if {other} even remembers. They remember everything else.",
            "Every dollar {other} spends is a dollar of mine walking by.",
            "Money's money, and {other}'s {other}. Hard to keep the two apart.",
            "{other} owes me money and an honest word. Neither's come."
        };

        private static readonly string[] Sweet =
        {
            "{other} laughed at what I said. Twice. I've been counting.",
            "Thinking about {other} again. Burned the coffee doing it.",
            "Wonder if {other} thinks of me at all. Probably not. Maybe.",
            "Wore my good collar on the chance of seeing {other}.",
            "{other} said my name. Just my name. Heard it all afternoon.",
            "Walked past {other}'s door twice. On purpose. Don't tell anyone.",
            "Wonder what {other} looks like first thing in the morning.",
            "Every bell on the street, I look up. In case it's {other}.",
            "{other} smells of lavender and something else. Can't place it.",
            "Practised what I'd say to {other}. Said none of it.",
            "Ma would say I'm mooning. Ma would be right. {other}.",
            "{other} touched my sleeve. I keep looking at the sleeve.",
            "Bought a ribbon. Haven't the nerve to give it to {other}.",
            "Can't eat. Can't sleep. Can think of {other}, though.",
            "{other} laughed at my bad joke. My worst one.",
            "Would {other} wait for the likes of me? Doubt it. Hope it.",
            "Heart going like a hammer when {other} passed.",
            "Wrote {other}'s name in the dust on the window. Rubbed it out fast.",
            "{other} has a freckle by the ear. Left ear. I counted.",
            "Sunday I'll sit where I can see {other}. The preacher can manage.",
            "{other} said 'good day' different to me. I'm sure of it.",
            "Picked wildflowers. Stood there holding them like a fool. No {other}.",
            "When {other} smiles the whole street goes quiet. In my head, anyhow.",
            "Thought about {other} so long the coffee went cold. Again.",
            "Could I make {other} happy? I'd try. Lord, I'd try.",
            "Saw {other} with somebody else. Only talking. Only talking.",
            "{other}'s handwriting on a note. Kept the note.",
            "Asked after {other} at the store. Too casual. Fooled nobody.",
            "{other}. Just the name's enough to set me grinning.",
            "{other} waved. At me. Looked behind me to be sure."
        };

        private static readonly string[] Known =
        {
            "Talked with {other} a good while. Easy company. Rare, that.",
            "{other} said a thing I keep turning over. Clever, for a talker.",
            "{other} and I had a good long jaw. Hours went by like minutes.",
            "{other} knows a thing or two about horses. Didn't expect that.",
            "Laughed with {other}. Haven't laughed like that since spring.",
            "{other} told me about {his} people back east. Felt like I'd met them.",
            "Good to have someone to pass the time with. {other}, of all people.",
            "{other} remembered my name. Most don't bother.",
            "Talked weather with {other}. Then talked everything else.",
            "{other} has a dry way of saying things. Took me a while to catch on.",
            "Easy, talking to {other}. Like an old boot.",
            "{other} said {he}'d buy the next coffee. I'll hold {him} to it.",
            "Never knew {other} had a brother in the army.",
            "{other} talks with {his} hands. Nearly lost my hat.",
            "Nice, being asked my opinion. {other} asked twice.",
            "{other} and me see the town the same way. Rare.",
            "Bumped into {other} again. Twice in a week. Getting to be a habit.",
            "{other} said the same thing Ma used to say. Gave me a turn.",
            "Wonder what {other} makes of me. Hope it's decent.",
            "{other} walked me halfway home, talking. Didn't notice the cold.",
            "Told {other} something I hadn't told anybody. Strange.",
            "{other} doesn't gossip. I noticed that. I like that.",
            "Talked about nothing with {other}. Best kind of nothing.",
            "{other} reads. Quoted a book at me. Couldn't tell you which.",
            "Might ask {other} to supper. Might not. Might.",
            "{other} has a laugh like a door with a bad hinge. I like it.",
            "{other} knows everybody. Told me who's who in this town.",
            "Good to know a face like {other}'s on this street.",
            "{other} asked how I was. Waited for the answer, too.",
            "{other}'s easy company, and that's worth something out here."
        };

        private static readonly string[] Kindness =
        {
            "{other} asked after my trouble. Meant it, too. Didn't expect that.",
            "{other} was glad for me. Truly glad. There's few would be.",
            "{other} brought me soup. Soup. I near wept into it.",
            "{other} said it'd come right. Said it like {he} believed it.",
            "Didn't ask for help. {other} gave it anyway.",
            "{other} sat with me a while. Didn't say much. Didn't need to.",
            "I won't forget {other} stopping. Not for a long time.",
            "{other}'s kindness has a weight to it. Good weight.",
            "Tried to pay {other} back. {other} waved it off.",
            "Funny how one kind word from {other} turns a whole day.",
            "{other} remembered my trouble. Asked after it a week on.",
            "Plenty walked by. {other} didn't.",
            "{other} put a hand on my shoulder. Still feel it there.",
            "Owe {other} more than coffee. Coffee's a start.",
            "{other} has troubles of {his} own. Still made room for mine.",
            "There's good people yet. {other}'s one.",
            "Ma would've liked {other}. That's the highest I've got.",
            "{other} said I'd done right. Hadn't heard that in a while.",
            "Will look out for {other} from now on. That's how it goes.",
            "{other} made me laugh when I'd no business laughing.",
            "Thought I was alone in it. {other} showed me otherwise.",
            "Kindness from {other}. Didn't know where to put my hands.",
            "{other} gave me {his} time. Rich folk don't give that.",
            "Wrote {other}'s name in my book. So I won't forget.",
            "{other} didn't make a show of it. Just did it.",
            "Felt foolish taking {other}'s help. Felt worse turning it down.",
            "{other} has kind eyes. Never noticed before today.",
            "Pray {other}'s day goes easy. They made mine easier.",
            "Small thing {other} did. Biggest thing all week.",
            "Next time someone's down, I'll do as {other} did."
        };

        private static readonly string[] MadeUp =
        {
            "Made it up with {other}. Like setting down a sack of feed I'd carried a month.",
            "{other} said sorry first. I'd have said it second. Probably.",
            "Shook hands with {other}. Firm grip. Meant it.",
            "{other} and I are square now. Feels like a window opened.",
            "Laughed with {other} today. After all that. Laughed.",
            "Took both of us being stubborn, and both of us giving in. {other} first.",
            "Should've made it up with {other} months back. Wasted months.",
            "{other} said {he} was sorry. I said I was sorrier. We'll argue about that instead.",
            "Bought {other} a drink. Drank it together. Like before.",
            "Lighter on my feet since {other} and I settled it.",
            "Ma always said never let the sun go down on it. Took a few sundowns with {other}.",
            "{other}'s not so bad. Never was, really.",
            "Mended fences with {other}. Real fence still needs mending, too.",
            "Feels strange, being easy with {other} again. Good strange.",
            "Grudges are heavy. Set mine down with {other}.",
            "{other} remembered my birthday after all that. Didn't expect it.",
            "Pride cost me {other} a while. Not anymore.",
            "Saw {other} on the street and smiled. Didn't have to think about it.",
            "{other} and me will have words again, someday. Fine. We'll mend them again.",
            "Sat with {other} at supper. Talked like the quarrel never was.",
            "Glad I swallowed it with {other}. Tasted bad. Went down fine.",
            "The town'll have to find something else to talk about. Me and {other} are done fighting.",
            "Hand on {other}'s shoulder. Theirs on mine. That was all it took.",
            "Shouldn't have taken so long. {other} knows it. I know it.",
            "Back to borrowing {other}'s tools. Missed that.",
            "Wrote {other} a note. Tore it up. Said it to {his} face instead.",
            "Slept the whole night through. First since the quarrel with {other}.",
            "Some quarrels end with a shot. Mine with {other} ended with coffee. Thank the Lord.",
            "{other} grinned at me like old times. Old times are back.",
            "Peace with {other}. Small word. Big thing."
        };

        private static readonly string[] Shrugged =
        {
            "Told {other} my news. Got a shrug for it. I'll remember the shrug.",
            "{other} couldn't get away from me fast enough. Fine. Fine.",
            "Thought {other} would be glad for me. {other} looked at the clock.",
            "{other} yawned while I talked. Yawned.",
            "Tried to tell {other} my troubles. Might as well've told the hitching post.",
            "{other} had somewhere to be. Always somewhere to be.",
            "Small thing, {other} not caring. Sits heavy all the same.",
            "Won't share my news with {other} again.",
            "{other} said 'that's nice' and turned away. That's nice.",
            "Funny who listens and who doesn't. {other} doesn't.",
            "{other} talked over me. Twice. Didn't notice, I think.",
            "Should've known better than to tell {other} anything.",
            "{other} has ears for gossip and none for me.",
            "Felt a fool, going on to {other} like that.",
            "{other} looked past me at somebody better.",
            "Cold, {other}. Cold as a creek in March.",
            "Kept my news to myself after that. {other} taught me.",
            "Thought we were friendlier than that, {other} and me.",
            "{other} nodded the way you nod at a dog.",
            "One word from {other}. One. Then gone.",
            "Wanted {other} to ask more. {other} never asks.",
            "{other}'s mind was miles off. Not on me.",
            "Told {other} straight out. Got a shrug. A shrug.",
            "Won't hold it against {other}. Will remember it, though.",
            "Some folks are kind and some are busy. {other}'s busy.",
            "Felt smaller after talking to {other}.",
            "{other} meant no harm. That almost makes it worse.",
            "Walked off from {other} quicker than I meant to.",
            "Felt it in my throat, how little {other} cared.",
            "Should've told the barber instead of {other}. He at least listens."
        };

        // trouble one did to the other: the one who did it, and the one it was done to
        private static readonly string[] RoughDid =
        {
            "Put {other}'s face into the bar. Felt good for about a minute.",
            "Hand still smells of {other}'s hair oil. Should've washed it.",
            "Folks give me room since that business with {other}. I like the room.",
            "{other} had it coming. Said so to anyone who'd listen.",
            "Barkeep gave me a look after what I did to {other}. Let him look.",
            "Knuckles still white from gripping {other}'s collar.",
            "Didn't think. Just grabbed {other}. Thinking came after.",
            "{other}'s face hit the wood loud as a gunshot. Can still hear it.",
            "Might've gone too far with {other}. Might not've gone far enough.",
            "{other} won't run {his} mouth for a while. Can't, with that lip.",
            "Woke up thinking of {other}'s face going down. Not proud. Not sorry.",
            "If the law asks, {other} slipped.",
            "My temper got the better of me with {other}. Got the better of {other} too.",
            "Felt the bar shake when {other} hit it. Felt it in my arm all night.",
            "Pa would've taken the strap to me for what I did to {other}.",
            "Can't take it back. Wouldn't, with {other}.",
            "{other} looked so surprised. That's the part I keep seeing.",
            "Drinks taste different since {other}. Don't know why.",
            "Should I square it with {other}? Not likely.",
            "{other} had friends in that room. Nobody moved. Remember that.",
            "Seen {other} since. They looked at the floor. Good.",
            "Don't feel like a big man. Felt like one for a minute, with {other}.",
            "Sheriff's been asking about me and {other}. Keeping my head down.",
            "{other} had a mouth. I had a quicker hand.",
            "Thought about it all night. {other} started it. I'm near sure.",
            "If {other}'s brother comes looking, I'll be ready.",
            "Can't settle. Keep seeing the blood on the wood from {other}'s nose.",
            "Washed my hands three times. Still feel {other}'s collar in them.",
            "Hope {other} thinks twice now. Before opening that mouth.",
            "{other}'s hat rolled under a table. Nobody picked it up."
        };
        private static readonly string[] RoughDone =
        {
            "Can't chew on the left side since {other} put my face in the bar. Every bite, I think of {other}.",
            "Everybody saw. Everybody saw {other} do it, and nobody said a word.",
            "I'll be standing with my back to the wall from now on. {other} taught me that much.",
            "Nose won't stop throbbing. {other} did that.",
            "Mirror's no friend this week. {other}'s handiwork all over my face.",
            "Flinch when anybody comes up fast now. {other}'s gift.",
            "{other} grabbed me like a sack of meal. Like I was nothing.",
            "Eye's swollen shut. The other one sees {other} everywhere.",
            "Didn't even see it coming. {other} was just there, and then the wood was.",
            "Folks look at my face and then look away. {other} put that look there.",
            "Can't drink in that saloon now. Not with {other} in it.",
            "Lay awake planning what I'd do to {other}. Won't do any of it.",
            "Lip split in two places. Every word stings. Every word, {other}.",
            "Ma would cry to see me. Good thing she can't. {other}.",
            "The worst of it isn't the hurt. It's how quick {other} was.",
            "Nobody stopped {other}. Not one of them.",
            "Tasted varnish and blood for a day after. {other}.",
            "Want to be angry at {other}. Mostly I'm afraid.",
            "Doctor says nothing's broke. {other} meant it to be.",
            "Walked the long way round to miss {other}. Felt like a dog doing it.",
            "Somebody should do to {other} what {other} did to me.",
            "Shame's worse than the bruise. {other} gave me both.",
            "Heard {other} laughing about it. Laughing.",
            "Couldn't sleep for the ache. Or for thinking of {other}.",
            "Don't know what I said to set {other} off. Doesn't matter now.",
            "Every time a door bangs I'm back there with {other}'s hand on my neck.",
            "There'll be a day for {other}. There will.",
            "Can still feel {other}'s grip on the back of my collar.",
            "Hat's still on that saloon floor. Not going back for it. {other}.",
            "Spat pink all morning. {other}."
        };
        private static readonly string[] LiftDid =
        {
            "{other} never felt a thing. Some folks walk around asking to be robbed.",
            "Three dollars and a lucky rabbit's foot off {other}. Wasn't lucky for {other}.",
            "Saw {other} patting {his} coat. Didn't look back. You never look back."
        };
        private static readonly string[] LiftDone =
        {
            "Had money this morning. Somewhere between here and the store it walked off.",
            "Somebody had a hand in my coat. I keep going over every face I passed.",
            "Lost my money. Or it was took. Either way I'm a fool, and I'll eat beans till Saturday."
        };
        private static readonly string[] CaughtDid =
        {
            "{other} saw my face. Has to have. Best stay off that street a while.",
            "Should've let {other} be. Should've picked an easier mark."
        };
        private static readonly string[] CaughtDone =
        {
            "I'd know {other}'s face anywhere now. Had a hand right in my pocket.",
            "{other}. Bold as brass, in broad day. I'll count my change twice around {other}.",
            "Felt the tug, turned round, and there was {other}. Hand in my coat.",
            "{other} had the nerve to smile when I caught {him}.",
            "Keep my money in my boot now. Thanks to {other}.",
            "Thought {other} was decent. So much for thinking.",
            "Told the whole street about {other}. Every word true.",
            "{other}'s fingers were quick. Not quick enough.",
            "Pat my pocket every few steps now. Can't help it. {other}.",
            "Ought to have called the sheriff on {other}. Still might.",
            "{other} looked me in the eye with {his} hand in my pocket. Bold as a crow.",
            "Lost nothing but my trust. {other} took that clean.",
            "Next time I see {other}, I'll cross the street and count to ten.",
            "{other} said it was a mistake. Hand slipped. Into my coat.",
            "Can't stand in a crowd now without thinking of {other}.",
            "Wonder how many {other} got before me.",
            "Watch {other}'s hands now, every time. Every time.",
            "Felt sick after catching {other}. Like I'd been the one at fault.",
            "Pockets sewn shut. Never thought I'd need to. {other}.",
            "{other} goes to church. Sits three pews ahead. Thief.",
            "Caught {other} fair. Folks believed me, too.",
            "Will tell my children about {other}. Watch your pockets, I'll say.",
            "{other}'s face when I grabbed that wrist. Almost funny.",
            "Ain't the money. It's that {other} thought I wouldn't notice.",
            "Saw {other} at the store after. Counted my change twice.",
            "Somebody ought to warn the next fool about {other}. I'm doing it.",
            "Had my whole week's pay in that coat. {other} knew it, too.",
            "Can't decide if I'm angrier at {other} or at me for being easy.",
            "{other} ran. That says all there is to say.",
            "{other}'s hand was cold. That's what I remember. Cold."
        };
        private static readonly string[] AvoidDid =
        {
            "{other} saw me coming and found somewhere else to be. Good.",
            "{other} won't look me in the eye now. Suits me."
        };
        private static readonly string[] AvoidDone =
        {
            "Went the long way round so as not to pass {other}. Coward's walk. Still walking it.",
            "Saw {other} and my hands started shaking. Hate that they do that.",
            "Took the alley so I'd not pass {other}. Felt like a cat dodging a dog.",
            "Saw {other}'s hat and turned round. Bought nothing I came for.",
            "Not going near {other}. Not after last time.",
            "My legs go wrong when {other}'s close. Can't help it.",
            "Sat with my back to the wall in case {other} came in.",
            "The store's out of my way now. {other} drinks across from it.",
            "Waited till {other} rode off before I came out.",
            "Hate that {other} decides where I walk.",
            "Heard {other}'s laugh down the street. Went home.",
            "Some day I'll walk right past {other}. Not today.",
            "Kept my eyes on my boots when {other} went by.",
            "{other} doesn't even know I'm hiding. That's the worst part.",
            "If {other} says a word to me I'll freeze. I know I will.",
            "Ma says hold your head up. Ma never met {other}.",
            "Heart thumps if I catch sight of {other}. Like a rabbit.",
            "Missed the post office again. {other} was outside it.",
            "Hate being afraid. Hate {other} more for it.",
            "Took my coffee and went. {other} came in.",
            "Wish I had the nerve to look {other} in the eye.",
            "A grown soul hiding from {other}. Fine sight I make.",
            "Practise what I'd say to {other}. Never say it.",
            "Saw {other} with friends. Walked faster.",
            "Dreamed of {other} last night. Woke up sweating.",
            "Avoiding {other} has me late for everything.",
            "One day I'll stop crossing the street for {other}. One day.",
            "{other} owns this end of town now, as far as my feet are concerned.",
            "Let {other} have the boardwalk. I'll take the mud.",
            "Counted the steps home. Two hundred, the long way. {other}."
        };
        // {other}: the one who fell (for the one who helped or walked past), or the one who helped or walked past
        private static readonly string[] HelpDid = { "{other} took a fall and I helped {him} up. Glad I was there.", "Hope {other}'s mending. Went down hard." };
        private static readonly string[] HelpDone = {
            "{other} picked me up when I fell. I owe {other} for that.",
            "{other} stopped. Plenty didn't.",
            "{other} brushed the dirt off my coat. Like Ma would.",
            "Knee's sore, pride's sorer. {other} was kind about both.",
            "Could've lain there an hour. {other} came in a minute.",
            "{other}'s hand was steady. Mine wasn't.",
            "Thanked {other} twice. Should've thanked {him} three times.",
            "Wonder what I'd have done if {other} hadn't come.",
            "{other} didn't laugh. Others did. {other} didn't.",
            "Feel I owe {other} a pie. Maybe two.",
            "Tumbled in the street like a fool. {other} made it not matter.",
            "Will look out for {other}, should {he} ever fall.",
            "{other} didn't ask anything for it. Just went on.",
            "Ankle's swelling. {other} said to keep it up. I will.",
            "Kind of {other}. The town's full of folk who wouldn't.",
            "Told Ma about {other} in a letter. She'll like that.",
            "{other}'s face when {he} reached me. Worried. For me.",
            "Didn't know {other} could lift like that.",
            "Will remember {other}'s name now. Didn't before.",
            "Ashamed of falling. Grateful to {other}. Both at once.",
            "{other} stayed till I could stand. That's a friend.",
            "Hope I'd do as {other} did, if it was {him} down there.",
            "{other} picked up my hat too. Dusted it. Small thing. Big thing.",
            "Every time I see {other} now, I'll think of the street, and the hand.",
            "Somebody laughed while I lay there. Then {other} came.",
            "Feel steadier for knowing {other}'s about.",
            "{other} made sure I got home. Wouldn't take a coin.",
            "There's good in this town. Its name's {other}, today.",
            "{other}'s grip was warm. I was cold right through.",
            "Might bake for {other}. Might just say it again. Thank you."
        };
        private static readonly string[] PassDid = { "{other} took a tumble. Not my affair.", "{other} looked at me like I'd done something. I hadn't." };
        private static readonly string[] PassDone = {
            "{other} stepped right past me. Me lying in the street.",
            "I won't forget {other} walking on by.",
            "{other} looked right at me lying there. Then looked at the sky.",
            "Didn't ask {other} for much. A hand. That's all.",
            "{other} stepped round me like a puddle.",
            "Won't forget {other}'s boots going by my face.",
            "Thought better of {other}. Thought wrong.",
            "If {other} ever falls, I'll stop. Just to show {him}.",
            "Lay in the dirt and watched {other} walk off.",
            "Funny how quick {other} remembered somewhere to be.",
            "{other} saw me. Don't tell me {he} didn't.",
            "Would it have cost {other} anything? A minute. A minute.",
            "Still feel the street's chill. And {other}'s back.",
            "Told everyone how {other} walked on by. Told it plain.",
            "{other} tips {his} hat to me now like nothing happened.",
            "My knee healed. What {other} did hasn't.",
            "Some folks are all Sunday and no Monday. {other}.",
            "Picked myself up. No thanks to {other}.",
            "Ma always said you learn who folks are when you fall. {other}.",
            "Can't look at {other} the same way. Won't try.",
            "Watched {other}'s shadow go by. Didn't even slow.",
            "Hope {other} slept well. I didn't.",
            "There's a list I keep: who stopped, who didn't. {other} didn't.",
            "A stranger would've stopped. {other} knows me. Didn't.",
            "Smile at {other}? I'll smile. I'll remember too.",
            "{other} could've said a word. Even a word.",
            "Still got the scrape on my palm. Still got {other}'s back in my eye.",
            "{other} walked past me like I was part of the road.",
            "Dusted myself off while {other} turned the corner.",
            "There's a kind of quiet that's a lie. {other}'s, walking by."
        };
        // standing up to a bully: {other} the one picked on (for the one who stepped in), or the one who stepped in; faced: the bully, or the one who faced him
        private static readonly string[] DefendDid = { "Stood up for {other}. Hands shook after. Worth it.", "Couldn't stand by and watch it done to {other}." };
        private static readonly string[] DefendDone = {
            "{other} stepped in for me. Didn't have to.",
            "Owe {other}. Plain as that.",
            "{other} stood between me and it. Just stood there. Didn't have to.",
            "Thought I was done for. Then {other}'s voice.",
            "Don't know if I'd have done the same for {other}. Should've. Will.",
            "{other}'s a braver soul than me. Found that out the hard way.",
            "Owe {other} my teeth, likely.",
            "Hands still shaking. Not from fear now. From {other} stepping in.",
            "{other} said three words and the bully went. Three words.",
            "Will stand by {other}, come what may. That's settled.",
            "Never had anyone stand up for me before {other}.",
            "{other} didn't make a show of it after. Just went on.",
            "Can walk this street again. Thanks to {other}.",
            "Told {other} I owe {him}. {other} said nonsense. I owe {him}.",
            "Saw the look {other} gave him. Didn't know {other} had that in {him}.",
            "Ma always said find your people. Found one. {other}.",
            "Won't forget the sound of {other} saying enough.",
            "Shame I needed it. Glad {other} gave it.",
            "{other}'s name is good in my house from now on.",
            "If anybody comes for {other}, they'll find me there.",
            "{other} took a risk for me. Folks don't, much.",
            "Lay awake thinking of {other} stepping in. Good thoughts, for once.",
            "Bought {other} a drink. Not near enough.",
            "Don't feel as small since {other} stood up.",
            "{other} looked at me after like it was nothing. It wasn't nothing.",
            "Wish I'd thanked {other} better. Choked on it.",
            "Never liked {other} much before. Changed my mind.",
            "{other} didn't even know me well. Stood up anyway.",
            "Keep thinking what would've happened without {other}. Don't like the picture.",
            "{other}'s voice was steady. Mine still isn't."
        };
        private static readonly string[] FacedDid = { "Told {other} to his face. About time somebody did.", "{other} looked at me like murder. Let him." };
        private static readonly string[] RoundDid = { "{other} drank my health tonight. Worth every cent.", "Stood {other} a drink. Felt like a rich man, for once.", "Stood the saloon a round. {other} thanked me twice." };
        private static readonly string[] RoundDone = { "{other} stood me a drink. I'll remember that.", "Drank to {other}'s health. Meant it, too.", "{other} bought me a drink. And everyone else." };
        private static readonly string[] SettledDid = { "Paid {other} every cent, out in the street where folks could see.", "{other} said it out loud: square. I'll sleep tonight.", "Owe {other} nothing now. Strange, how light a body feels.", "Square with {other}. Folks saw it done.", "{other} counted it twice. Let them. It's all there." };
        private static readonly string[] SettledDone = { "{other} paid me back. Every cent. I'd have bet the other way.", "Counted {other}'s money twice. All there.", "Didn't think I'd see that money again. {other} surprised me.", "{other} owes me nothing now. Strange, after all that.", "Took {other}'s coins in the street. Felt near sorry." };
        private static readonly string[] TownSettled = { "{a} paid {b} back in the street, every cent, with folks watching.", "Heard {a} squared up with {b} in front of half the town.", "{a} paid what was owed. {b} said so out loud. Good for {a}.", "{a} squared up with {b} where the whole street could watch.", "Money changed hands between {a} and {b} today. Square, they say." };
        private static readonly string[] TownRound = { "{a} stood the whole saloon a round. Folks'll be friends with {a} till Sunday.", "{a} bought for the house, {b} says. Wish I'd been there.", "{a} bought the house a drink last night. {b} hasn't stopped saying so." };
        private static readonly string[] FacedDone = { "{other} put {his} nose in my business. I won't forget it.", "{other}, playing the hero. In front of half the street." };

        private static readonly string[] CharityDid =
        {
            "{other} gave me money. Didn't even make me ask twice. There's good in folks yet.",
            "Bread tonight, thanks to {other}. Swore I'd never ask. Swear a lot of things."
        };
        private static readonly string[] CharityDone =
        {
            "Gave {other} a dollar. Felt good, then foolish, then good again.",
            "{other} looked so ashamed to ask. I know that look. Wore it myself, once."
        };
        private static readonly string[] ScornDid =
        {
            "Asked {other} for a nickel. Got told to work. I'd work, if there was work.",
            "Won't ask {other} for anything again. Won't ask anybody. Rather go hungry."
        };
        private static readonly string[] ScornDone =
        {
            "{other} asked me for money, in the street. The nerve. ...Still thinking about it, though.",
            "Should've given {other} something. A nickel's nothing. Too late now."
        };
        private static readonly string[] DrunkDid =
        {
            "Don't remember much past the third glass. Woke up with sawdust in my mouth. Did I call {other} out?",
            "Folks keep grinning at me. Something about {other}, and the floor. I'll not ask."
        };
        private static readonly string[] DrunkDone =
        {
            "{other} wanted to fight me. Couldn't find his own feet. Almost felt sorry for him.",
            "Poor {other}. Drink'll do that. Somebody ought to take that bottle away."
        };

        // the other one has died since
        private static readonly string[] DeadFoe =
        {
            "Last thing I ever gave {other} was trouble. Now {other}'s dead, and I can't take it back.",
            "{other}'s in the ground, and I'm still angry. Angry at the dead. What does that make me?"
        };
        private static readonly string[] DeadFriend =
        {
            "{other}'s dead. I keep turning round to tell {other} things.",
            "Can't walk past where {other} used to stand. Go the long way now."
        };
        private static readonly string[] DeadKnown =
        {
            "Talked with {other} only the other day. Now {other}'s gone. That's how quick it is.",
            "{other} nodded to me every morning for years. The street's a nod short now.",
            "Heard {other} was buried Tuesday. Didn't know {other} well. Still took my hat off."
        };

        /// A thought about someone they had a scene with lately, by first name; about their death if they've died since.
        /// He or she for the other person in a line ({he} {him} {his} {himself}; the slots are capitalised when the
        /// line starts with them). The Ties pools said "they" of a named person until the night of 2026-10-28, which read
        /// modern; a person of 1899 is he or she.
        public static string Pronouns(string line, bool woman)
        {
            if (line == null || line.IndexOf('{') < 0)
                return line;
            line = line.Replace("{he}", woman ? "she" : "he").Replace("{him}", woman ? "her" : "him").Replace("{his}", woman ? "her" : "his").Replace("{himself}", woman ? "herself" : "himself");
            return line.Replace("{He}", woman ? "She" : "He").Replace("{His}", woman ? "Her" : "His");
        }

        /// Whether the other end of a tie is a woman, by their first name when nothing better is known (the tie carries
        /// names, not sex; the add-on's and the scenes' women's names are known lists).
        public static bool OtherIsWoman(SceneTie t, string id)
        {
            string name = t != null ? t.Other(id) : null;
            if (name == null)
                return false;
            string first = Person.FirstOf(name);
            return Residents.IsWomansName(first) || Scenes.IsWomansName(first);
        }

        public static string Thought(SceneTie t, string id, string seed, bool otherDead = false, bool? otherWoman = null)
        {
            if (otherDead)
                return Pronouns(Voice.Pick(seed + "tdead", t.Warmth < 0 ? DeadFoe : t.Warmth > 0 ? DeadFriend : DeadKnown).Replace("{other}", Person.FirstOf(t.Other(id))), otherWoman ?? OtherIsWoman(t, id));
            if (t.Kind == "avoid" && t.Doer != null)
                return Pronouns(Voice.Pick(seed + "avoid", t.Did(id) ? AvoidDid : AvoidDone).Replace("{other}", Person.FirstOf(t.Other(id))), otherWoman ?? OtherIsWoman(t, id));
            if (Trouble(t.Kind) && t.Doer != null)
            {
                bool did = t.Did(id);
                string[] trouble = t.Kind == "rough" ? (did ? RoughDid : RoughDone) : t.Kind == "lift" ? (did ? LiftDid : LiftDone)
                                 : t.Kind == "pickpocket" ? (did ? CaughtDid : CaughtDone) : t.Kind == "charity" ? (did ? CharityDid : CharityDone)
                                 : t.Kind == "scorn" ? (did ? ScornDid : ScornDone) : t.Kind == "help" ? (did ? HelpDid : HelpDone)
                                 : t.Kind == "passby" ? (did ? PassDid : PassDone) : t.Kind == "defend" ? (did ? DefendDid : DefendDone)
                                 : t.Kind == "faced" ? (did ? FacedDid : FacedDone) : t.Kind == "round" ? (did ? RoundDid : RoundDone) : t.Kind == "settled" ? (did ? SettledDid : SettledDone) : (did ? DrunkDid : DrunkDone);
                return Pronouns(Voice.Pick(seed + "trouble", trouble).Replace("{other}", Person.FirstOf(t.Other(id))), otherWoman ?? OtherIsWoman(t, id));
            }
            string[] pool = t.Kind == "brawl" ? Blows : t.Kind == "debt" ? Money : t.Kind == "makeup" ? MadeUp : t.Kind == "brushoff" ? Shrugged
                          : t.Kind == "comfort" || t.Kind == "cheer" || t.Kind == "apology" ? Kindness : t.Warmth < 0 ? Quarrel : Romantic(t.Kind) ? Sweet : Known;
            return Pronouns(Voice.Pick(seed + "tie", pool).Replace("{other}", Person.FirstOf(t.Other(id))), otherWoman ?? OtherIsWoman(t, id));
        }

        // ---- what the town says ---------------------------------------------------------------------------

        private static readonly string[] TownHostile =
        {
            "{a} and {b} were shouting at each other in the street. Half of {town} heard it.",
            "{a} won't stand in the same room as {b} now. Ask either one why, you get a different story."
        };

        private static readonly string[] TownBlows =
        {
            "{a} and {b} came to blows. Over nothing, they say. It's never nothing.",
            "Somebody pulled {a} off {b} before it got worse. Blood on the boardwalk."
        };

        // {a} did it, {b} had it done to him
        private static readonly string[] TownRough =
        {
            "{a} grabbed {b} by the collar and put {b}'s face through the bar top. Barkeep's still picking up teeth.",
            "Did you see what {a} did to {b}? Never seen a man move that fast with his hands full."
        };
        private static readonly string[] TownCaught =
        {
            "{b} caught {a} with a hand in a coat pocket. In the open street. Some nerve.",
            "Watch your pockets round {a}. {b} found out the hard way."
        };
        private static readonly string[] TownDrunk =
        {
            "{a} called {b} out, drunk as a lord, and went down like a felled tree before a fist was raised.",
            "{a} wanted to fight {b}. Fought the floor instead. The floor won."
        };

        private static readonly string[] TownCharity =
        {
            "{b} gave {a} money in the street, plain as day. Folks will be lining up now.",
            "Saw {b} press a coin on {a}. Didn't think {b} had it in them."
        };
        private static readonly string[] TownScorn =
        {
            "{a} asked {b} for a nickel and got sent off like a stray dog.",
            "{b} wouldn't spare {a} a penny. And {b} with a gold watch chain."
        };

        private static readonly string[] TownHelp =
        {
            "{b} took a fall in the street and {a} was there before anybody. Helped {b} right up.",
            "Saw {a} pick {b} up out of the dirt. Dusted {b} off and all."
        };
        private static readonly string[] TownPassby =
        {
            "{b} fell flat in the street and {a} walked right past. Folks noticed.",
            "{a} stepped round {b} like a dropped hat. With half the street watching."
        };

        private static readonly string[] TownWarm =
        {
            "{a} has been seen walking with {b}. Folks talk. Folks always talk.",
            "{a} and {b}, stood close as two spoons. Give it a month."
        };

        private static readonly string[] TownMadeUp =
        {
            "{a} and {b} made it up, would you credit it. Shook on it outside the store.",
            "{a} and {b} are on speaking terms again. Town's lost a good quarrel.",
            "Saw {a} and {b} share a bench outside the store. A month ago they'd have shared a cell."
        };

        // one of the two has died since: {a} is the one left, {b} the dead ({af}, {bf}: first names, said again)
        private static readonly string[] TownDeadFoe =
        {
            "Everybody knew {a} couldn't stand {b}. Now {bf}'s dead, and folks are careful what they say about it.",
            "They say {b} never got over that business with {a}. Now {bf}'s buried, and {af} won't speak of it."
        };
        private static readonly string[] TownDeadWarm =
        {
            "{a} was sweet on {b}, and now {bf}'s gone. Somebody ought to look in on {af}.",
            "{a} and {b} were walking out, and now {bf}'s in the churchyard. {af} hasn't been seen since the burying.",
            "Folk were picking a wedding hat for {a} and {b}. Now it's a black one, for {bf}."
        };
        private static readonly string[] TownDeadMended =
        {
            "{a} and {b} only just made it up, and now {bf}'s gone. Glad they got to, I suppose.",
            "{a} and {b} shook hands at last, and a week later {bf} was gone. The handshake's what {af} has left.",
            "{a} buried {b} a friend. Six months back they weren't speaking. Strange, the timing."
        };

        /// The town's talk about a tie between two others, with both their full names; deadA/deadB: that one has died since.
        public static string TownThought(SceneTie t, string town, string seed, bool deadA = false, bool deadB = false)
        {
            if (deadA || deadB)
            {
                string left = deadB ? t.AName : t.BName, gone = deadB ? t.BName : t.AName;
                string[] dead = t.Kind == "makeup" ? TownDeadMended : Romantic(t.Kind) ? TownDeadWarm : TownDeadFoe;
                return Voice.Pick(seed + "ttd", dead).Replace("{af}", Person.FirstOf(left)).Replace("{bf}", Person.FirstOf(gone)).Replace("{a}", left).Replace("{b}", gone);
            }
            string[] pool = t.Kind == "brawl" ? TownBlows : t.Kind == "makeup" ? TownMadeUp : Romantic(t.Kind) ? TownWarm
                          : t.Kind == "rough" ? TownRough : t.Kind == "pickpocket" ? TownCaught : t.Kind == "drunk" ? TownDrunk
                          : t.Kind == "charity" ? TownCharity : t.Kind == "scorn" ? TownScorn : t.Kind == "help" ? TownHelp : t.Kind == "passby" ? TownPassby : t.Kind == "round" ? TownRound : t.Kind == "settled" ? TownSettled : TownHostile;
            // in the trouble lines {a} did it: the one who did, whichever way round the tie was first made
            bool swap = Trouble(t.Kind) && t.Doer != null && t.Doer == t.B;
            string an = swap ? t.BName : t.AName, bn = swap ? t.AName : t.BName;
            return Voice.Pick(seed + "tt", pool).Replace("{a}", an).Replace("{b}", bn).Replace("{town}", town ?? "town");
        }

        /// Every line, for the tests.
        public static IEnumerable<string> AllLines()
        {
            return Quarrel.Concat(Blows).Concat(Money).Concat(Sweet).Concat(Known).Concat(Kindness).Concat(MadeUp).Concat(Shrugged)
                .Concat(RoughDid).Concat(RoughDone).Concat(LiftDid).Concat(LiftDone).Concat(CaughtDid).Concat(CaughtDone).Concat(DrunkDid).Concat(DrunkDone).Concat(AvoidDid).Concat(AvoidDone)
                .Concat(CharityDid).Concat(CharityDone).Concat(ScornDid).Concat(ScornDone).Concat(TownCharity).Concat(TownScorn)
                .Concat(TownRough).Concat(TownCaught).Concat(TownDrunk).Concat(HelpDid).Concat(HelpDone).Concat(PassDid).Concat(PassDone).Concat(TownHelp).Concat(TownPassby).Concat(DefendDid).Concat(DefendDone).Concat(FacedDid).Concat(FacedDone).Concat(RoundDid).Concat(RoundDone).Concat(TownRound).Concat(SettledDid).Concat(SettledDone).Concat(TownSettled)
                .Concat(DeadFoe).Concat(DeadFriend).Concat(DeadKnown)
                .Concat(TownHostile).Concat(TownBlows).Concat(TownWarm).Concat(TownMadeUp)
                .Concat(TownDeadFoe).Concat(TownDeadWarm).Concat(TownDeadMended);
        }
    }
}
