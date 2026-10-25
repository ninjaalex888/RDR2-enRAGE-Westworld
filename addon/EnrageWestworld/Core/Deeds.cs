// What the player does and where he goes, turned into words: the rumour it starts, what people privately
// make of it, what they say to his face, what strangers whisper, and the small newspaper item it becomes.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class Deeds
    {
        private sealed class Kind
        {
            public int Weight;
            public string Mood;          // enRAGE thought mood for people who hear of it
            public bool Violent;
            public bool Shop;
            public string Rumour;        // {who} {detail} {place} {n}
            public string[] Thoughts;    // private, {who}=how they think of him
            public string[] Remarks;     // to his face
            public string Paper;         // newspaper item, or null
        }

        private static readonly Dictionary<string, Kind> Kinds = new Dictionary<string, Kind>
        {
            { "murder", new Kind { Weight = 70, Mood = "Shaken", Violent = true, Rumour = "{who} shot {detail} dead {place}",
                Thoughts = new[] { "Heard {who} shot {detail} dead {place}. Just like that.", "{Who} killed {detail} {place}. Could've been anybody. Could've been me." },
                Remarks = new[] { "I know what you did {place}.", "Folks are saying you killed {detail}. I believe them." },
                Paper = "{detail} was shot dead {place}. Witnesses describe the killer as {desc}." } },
            { "lawkill", new Kind { Weight = 85, Mood = "Shaken", Violent = true, Rumour = "{who} killed {detail} {place}",
                Thoughts = new[] { "{Who} killed a lawman {place}. They'll hang him. If they can catch him.", "Killing a lawman. Either he's mad or he don't care. Maybe both." },
                Remarks = new[] { "You're the one who killed that lawman.", "There's a deputy {place} who won't be going home. Folks say that's on you." },
                Paper = "A lawman was killed {place}. The territory offers a reward for {desc}." } },
            { "kill", new Kind { Weight = 45, Mood = "Shaken", Violent = true, Rumour = "{who} killed {detail} {place}",
                Thoughts = new[] { "{Who} killed {detail} {place}. Self-defence, they say. They always say that.", "Another body {place}. That stranger leaves a trail of them." },
                Remarks = new[] { "Heard there was killing {place}. Heard it was you.", "You've got blood on your boots, friend." } } },
            { "namedkill", new Kind { Weight = 60, Mood = "Bitter", Violent = true, Rumour = "{who} killed {detail} {place}",
                Thoughts = new[] { "{Who} killed {detail}. I knew {first}. Not well. Well enough.", "So {detail} is dead. {Who} did it, they say.", "{First} is in the ground and {who} is walking around free." },
                Remarks = new[] { "You killed {detail}. Don't bother denying it.", "{detail} had people, you know. People who'll remember.", "I saw what you did to {first}. I'll see it till I die.", "{First} never stood a chance against you, did he?" } } },
            { "robbery", new Kind { Weight = 55, Mood = "Bitter", Rumour = "{who} robbed {detail} {place}",
                Thoughts = new[] { "{Who} robbed {detail} {place}. Bold as brass.", "Lock the door tonight. {Who} is out robbing folk {place}." },
                Remarks = new[] { "I'll keep my distance, if it's all the same. I heard what happened {place}.", "You the one who robbed {detail}?" },
                Paper = "{detail} was robbed {place} by {desc}." } },
            { "bankrobbery", new Kind { Weight = 90, Mood = "Shaken", Violent = true, Rumour = "{who} robbed the bank {place}",
                Thoughts = new[] { "They robbed the bank {place}. Everyone's savings, gone. Mine too, near enough.", "{Who} took the bank {place}. You don't come back from that." },
                Remarks = new[] { "Heard you made a withdrawal {place}. With a gun.", "The bank {place}. That was you, wasn't it?" },
                Paper = "THE BANK {placeup} WAS ROBBED by {desc}. The Pinkerton agency has been called." } },
            { "stagecoach", new Kind { Weight = 70, Mood = "Bitter", Violent = true, Rumour = "{who} held up a stagecoach {place}",
                Thoughts = new[] { "Stage got held up {place}. Won't ride the coach till they catch him." },
                Remarks = new[] { "You rob coaches for a living, I hear." },
                Paper = "A stagecoach was held up {place}. The passengers describe {desc}." } },
            { "trainrobbery", new Kind { Weight = 85, Mood = "Shaken", Violent = true, Rumour = "{who} robbed the train {place}",
                Thoughts = new[] { "Somebody robbed the train {place}. Bold. Stupid, but bold.", "Train robbers {place}. The railroad will want blood." },
                Remarks = new[] { "Railroad men are asking about a train job {place}. Asking about somebody like you." },
                Paper = "A train was robbed {place}. The railway company offers a reward for {desc}." } },
            { "theft", new Kind { Weight = 30, Mood = "Bitter", Rumour = "{who} helped himself to what wasn't his {place}",
                Thoughts = new[] { "{Who} is a thief, they say. Caught at it {place}." },
                Remarks = new[] { "Keep your hands out of other folks' things.", "Heard you've got sticky fingers." } } },
            { "horsetheft", new Kind { Weight = 45, Mood = "Bitter", Rumour = "{who} stole a horse {place}",
                Thoughts = new[] { "Horse thief. {Who} stole a horse {place}. They used to hang men for that." },
                Remarks = new[] { "You stay away from my horse, you hear?", "That horse you rode in on. Yours, is it?" },
                Paper = "A horse was stolen {place}. The owner asks for its return, and {desc} to be brought to justice." } },
            { "assault", new Kind { Weight = 35, Mood = "Bitter", Violent = true, Rumour = "{who} beat {detail} senseless {place}",
                Thoughts = new[] { "{Who} beat {detail} half to death {place}. Over nothing, I heard." },
                Remarks = new[] { "Heard you like using your fists.", "You the one who beat that fella {place}?" } } },
            { "looting", new Kind { Weight = 25, Mood = "Bitter", Rumour = "{who} went through a dead man's pockets {place}",
                Thoughts = new[] { "Picking the pockets of the dead. What kind of man does that?" },
                Remarks = new[] { "You rob the dead too, I hear." } } },
            { "graverobbing", new Kind { Weight = 60, Mood = "Shaken", Rumour = "{who} dug up a grave {place}",
                Thoughts = new[] { "Somebody dug up a grave {place}. God help us. What's he looking for down there?" },
                Remarks = new[] { "Grave robber. I can smell the dirt on you." },
                Paper = "A grave was found opened {place}. The sexton is beside himself." } },
            { "kidnap", new Kind { Weight = 65, Mood = "Shaken", Violent = true, Rumour = "{who} hogtied {detail} and carried them off {place}",
                Thoughts = new[] { "{Who} trussed up {detail} like a calf and rode off with them. Folks saw it {place}." },
                Remarks = new[] { "You carried somebody off {place}. Hogtied. I heard." } } },
            { "arson", new Kind { Weight = 70, Mood = "Shaken", Violent = true, Rumour = "{who} set a fire {place}",
                Thoughts = new[] { "Fire {place}. They say a stranger set it." },
                Paper = "A fire was set {place}. Suspicion falls on {desc}." } },
            { "cheating", new Kind { Weight = 30, Mood = "Bitter", Rumour = "{who} was caught cheating at cards {place}",
                Thoughts = new[] { "Card cheat. Caught at it {place}. I'll not sit at a table with him." },
                Remarks = new[] { "You won't be playing cards at my table, cheat." } } },
            { "hunt", new Kind { Weight = 10, Mood = "Steady", Rumour = "{who} came in from {zone} with {n}",
                Thoughts = new[] { "Saw {who} ride in with {n}. Good hunter, whatever else he is.", "{Who} has been out hunting. {N}. Blood to the elbows." },
                Remarks = new[] { "You do some hunting, I see.", "That's a fair haul you brought in." } } },
            { "gunsmith", new Kind { Weight = 15, Shop = true, Rumour = "{who} bought enough cartridges for a small war at the gunsmith {place}",
                Thoughts = new[] { "{Who} cleared out half the gunsmith {place}. Expecting trouble. Or bringing it.", "Saw {who} at the gunsmith. Bought more shells than a man needs for rabbits." },
                Remarks = new[] { "Saw you at the gunsmith. Expecting trouble?", "That's a lot of ammunition you're carrying." },
                Paper = "A stranger was seen leaving the gunsmith {place} with enough cartridges to start a war." } },
            { "genstore", new Kind { Weight = 4, Shop = true, Rumour = "{who} bought supplies at the store {place}",
                Thoughts = new[] { "{Who} came into the store {place}. Paid in coin. Polite enough." },
                Remarks = new[] { "Saw you at the store. Stocking up?" } } },
            { "church", new Kind { Weight = 20, Shop = true, Mood = "Steady", Rumour = "{who} was seen in church {place}",
                Thoughts = new[] { "{Who} came to church {place}. Sat at the back. Didn't sing.", "Saw that stranger in church. Didn't figure him for the praying kind." },
                Remarks = new[] { "Saw you at church. Didn't figure you for the praying kind.", "The Lord takes all sorts, I suppose. Even you." },
                Paper = "Worshippers {place} were surprised by a hard-looking stranger who sat through the whole service." } },
            { "saloon", new Kind { Weight = 8, Shop = true, Rumour = "{who} was drinking at the saloon {place}",
                Thoughts = new[] { "{Who} was in the saloon {place}. Drank alone. Watched the door.", "That stranger drinks like a man trying to forget something." },
                Remarks = new[] { "You were in the saloon. Drink with anybody, or just the bottle?" } } },
            { "bath", new Kind { Weight = 10, Shop = true, Rumour = "{who} took a bath {place}",
                Thoughts = new[] { "{Who} took a bath {place}. The water was black, I heard." },
                Remarks = new[] { "You clean up all right, I'll give you that." } } },
            { "doctor", new Kind { Weight = 12, Shop = true, Rumour = "{who} went to see the doctor {place}",
                Thoughts = new[] { "{Who} was at the doctor's {place}. Coughing, they said. Or bleeding.", "Even hard men need the doctor sometimes." },
                Remarks = new[] { "Saw you leaving the doctor's. You all right?" } } },
            { "barber", new Kind { Weight = 8, Shop = true, Rumour = "{who} got himself a shave {place}",
                Thoughts = new[] { "{Who} got a shave {place}. Barber said his hands never stopped moving." },
                Remarks = new[] { "New haircut? Suits you." } } },
            { "bank", new Kind { Weight = 14, Shop = true, Rumour = "{who} spent a long while in the bank {place}",
                Thoughts = new[] { "{Who} was in the bank {place}. Looking at the vault more than the teller.", "Strangers who linger in banks make me nervous." },
                Remarks = new[] { "Saw you at the bank. Making a deposit? Or studying the doors?" },
                Paper = "Bank clerks {place} report a stranger who showed an unusual interest in the building." } },
            { "sheriff", new Kind { Weight = 18, Shop = true, Rumour = "{who} walked straight into the sheriff's office {place}",
                Thoughts = new[] { "{Who} walked into the sheriff's office {place} bold as you like. Came out again, too." },
                Remarks = new[] { "Saw you visiting the law. Turning yourself in, or turning somebody else in?" } } },
            { "hotel", new Kind { Weight = 6, Shop = true, Rumour = "{who} took a room {place}",
                Thoughts = new[] { "{Who} took a room {place}. Paid up front. Didn't sign a real name, I bet." } } },
            { "theatre", new Kind { Weight = 20, Shop = true, Rumour = "{who} was at the show {place}",
                Thoughts = new[] { "Saw {who} at the show {place}. Laughed in the wrong places." },
                Remarks = new[] { "Enjoy the show? I saw you there." } } },
            { "station", new Kind { Weight = 6, Shop = true, Rumour = "{who} was at the station {place}",
                Thoughts = new[] { "{Who} was at the station {place}. Leaving town, I hope." } } },
            { "tailor", new Kind { Weight = 8, Shop = true, Rumour = "{who} bought new clothes {place}",
                Thoughts = new[] { "{Who} bought new clothes {place}. Fancy. A wolf in a nice coat is still a wolf." },
                Remarks = new[] { "New coat? Looks expensive." } } },
            { "fence", new Kind { Weight = 30, Shop = true, Mood = "Bitter", Rumour = "{who} was seen going into the fence's {place}",
                Thoughts = new[] { "{Who} sells to the fence {place}. Stolen goods, all of it." },
                Remarks = new[] { "I know where you sell your goods. Everybody does." } } },
            { "photo", new Kind { Weight = 10, Shop = true, Rumour = "{who} had his likeness taken {place}",
                Thoughts = new[] { "{Who} had his picture made {place}. Vain, or wants to be remembered." } } },
            { "stable", new Kind { Weight = 6, Shop = true, Rumour = "{who} was looking over the horses {place}",
                Thoughts = new[] { "{Who} knows horses. Saw him at the stable {place}." } } },
            { "store", new Kind { Weight = 6, Shop = true, Rumour = "{who} was shopping {place}",
                Thoughts = new[] { "{Who} was in the shops {place}. Money to spend, apparently." } } },
            { "flirt", new Kind { Weight = 12, Rumour = "{who} was sweet-talking {detail} {place}",
                Thoughts = new[] { "{Who} was sweet-talking {detail} {place}. In broad daylight.", "Saw {who} making eyes at {detail}. Somebody ought to warn them." },
                Remarks = new[] { "Saw you sweet-talking {detail}. Charming, ain't you." } } },
            { "wedding", new Kind { Weight = 30, Mood = "Joyful", Rumour = "{who} came to the wedding of {detail} {place}",
                Thoughts = new[] { "{Who} came to {detail}'s wedding {place}. Stood at the back with his hat in his hands." },
                Remarks = new[] { "You were at the wedding. That was decent of you." } } },
            { "funeral", new Kind { Weight = 30, Mood = "Grieving", Rumour = "{who} stood at {detail}'s grave {place}",
                Thoughts = new[] { "{Who} came to {detail}'s funeral {place}. Guilt, or respect. Hard to say which." },
                Remarks = new[] { "Saw you at the graveside. Strange place for a man like you." } } },
            { "spared", new Kind { Weight = 30, Mood = "Steady", Rumour = "{who} let {detail} go {place}",
                Thoughts = new[] { "{Who} had {detail} dead to rights {place} and let them go. Why?" },
                Remarks = new[] { "You let {detail} live. Folks noticed." } } },
            { "peace", new Kind { Weight = 16, Mood = "Steady", Rumour = "{who} broke up {detail} {place}",
                Thoughts = new[] { "{Who} stepped between two men about to kill each other {place}. Brave, or touched in the head.", "Heard {who} broke up {detail} {place}. Blessed are the peacemakers." },
                Remarks = new[] { "You're the one who broke up {detail}. Folks noticed.", "Blessed are the peacemakers, mister. Even the ones wearing a Colt." },
                Paper = "A stranger stepped into {detail} {place} and sent both men home without a shot fired." } },
            { "thiefcaught", new Kind { Weight = 22, Mood = "Steady", Rumour = "{who} ran down a pickpocket {place}",
                Thoughts = new[] { "{Who} ran down a pickpocket {place} and gave the purse back. Didn't even keep a dollar.", "A thief got caught {place}. By {who}, of all people." },
                Remarks = new[] { "Heard you caught that pickpocket. Wish you'd been around when they got me.", "You ran that thief down like a hound. Remind me to stay on your good side." },
                Paper = "A pickpocket working {place} met his match when a stranger ran him down and returned the stolen goods to their owner." } },
            { "mercy", new Kind { Weight = 20, Mood = "Steady", Rumour = "{who} had a gun on {detail} {place} and let them live",
                Thoughts = new[] { "{Who} held a gun on {detail} {place}. Then put it away. Strange man.", "They say {detail} begged and {who} listened. Didn't think he had it in him." },
                Remarks = new[] { "Heard you had a gun on {detail} and let them walk. Mercy's rare out here.", "You let that poor soul live. Folks are talking about it." } } },
            { "execution", new Kind { Weight = 90, Mood = "Shaken", Violent = true, Rumour = "{who} shot {detail} while they begged for mercy {place}",
                Thoughts = new[] { "{Who} shot {detail} while they begged {place}. What kind of animal does that?", "They say {detail} begged. Begged, and {who} shot anyway." },
                Remarks = new[] { "You shot somebody begging for their life. I hope you hear it every night.", "Folks say you don't stop when they beg. Is that true?" },
                Paper = "{detail} was shot dead {place} while pleading for mercy. Witnesses describe the killer as {desc}." } },
            { "visit", new Kind { Weight = 2, Rumour = "{who} rode into {town}",
                Thoughts = new[] { "{Who} is back in {town}.", "That stranger rode into {town} again." } } }
        };

        public static bool Known(string kind)
        {
            return kind != null && Kinds.ContainsKey(kind);
        }

        public static bool IsShop(string kind)
        {
            Kind k;
            return kind != null && Kinds.TryGetValue(kind, out k) && k.Shop;
        }

        /// What killing someone counts as, or null when it isn't a crime at all: they came at you (or were hostile to
        /// you) before you hurt them, so it was self-defence and nobody remembers it as a killing. A lawman is always
        /// a lawman, and shooting someone who begged is an execution, whoever started it. cameAtYou is decided at the
        /// first hit; fighting may also mean they only fought back after it (then it's a killing, not a murder).
        public static string KillKind(bool law, bool thief, bool begged, bool cameAtYou, bool fighting)
        {
            if (law)
                return "lawkill";
            if (begged)
                return "execution";
            if (cameAtYou)
                return null;
            if (thief || fighting)
                return "kill";
            return "murder";
        }

        public static bool IsViolent(string kind)
        {
            Kind k;
            return kind != null && Kinds.TryGetValue(kind, out k) && k.Violent;
        }

        public static string MoodOf(string kind)
        {
            Kind k;
            return kind != null && Kinds.TryGetValue(kind, out k) ? k.Mood ?? "Steady" : "Steady";
        }

        public static Deed Make(string kind, string detail, string town, string zone, long at, float x, float y, float z, int witnesses, int count = 1)
        {
            Kind k;
            int weight = Kinds.TryGetValue(kind, out k) ? k.Weight : 5;
            if (witnesses > 0 && k != null && k.Violent)
                weight += Math.Min(15, witnesses * 3);
            if (count > 1)
                weight += Math.Min(20, (count - 1) * 8);
            return new Deed
            {
                Id = Ids.New(), Kind = kind, Detail = detail, Town = town, Zone = zone, At = at,
                X = x, Y = y, Z = z, Witnesses = witnesses, Weight = Math.Min(100, weight), Count = Math.Max(1, count)
            };
        }

        // ---- words ---------------------------------------------------------------------------------

        public static string Place(Deed d)
        {
            if (!string.IsNullOrEmpty(d.Town))
                return "in " + d.Town;
            if (!string.IsNullOrEmpty(d.Zone))
                return "near " + d.Zone;
            return "out on the road";
        }

        private static string Count(Deed d)
        {
            if (d.Kind == "hunt")
            {
                string what = string.IsNullOrEmpty(d.Detail) ? "pelt" : d.Detail;
                return d.Count == 1 ? Grammar.Article(what) : Voice.Number(d.Count) + " " + Plural(what);
            }
            return Voice.Number(d.Count);
        }

        private static string Plural(string w)
        {
            if (w.EndsWith("s") || w == "deer" || w == "elk" || w == "moose" || w == "bison" || w == "sheep")
                return w;
            if (w.EndsWith("y") && !w.EndsWith("ey"))
                return w.Substring(0, w.Length - 1) + "ies";
            if (w.EndsWith("x") || w.EndsWith("ch"))
                return w + "es";
            if (w == "wolf")
                return "wolves";
            return w + "s";
        }

        private static string Detail(Deed d)
        {
            string det = string.IsNullOrEmpty(d.Detail) ? "a man" : d.Detail;
            if (d.Count > 1 && (d.Kind == "murder" || d.Kind == "kill"))
                return Voice.Number(d.Count) + " " + (det.Contains("woman") ? "people" : "men");
            return det;
        }

        private static string Fill(string template, Deed d, string who)
        {
            string place = Place(d);
            string first = Person.FirstOf(d.Detail ?? "him");
            string s = template.Replace("{detail}", Detail(d)).Replace("{first}", first).Replace("{First}", Grammar.UpperFirst(first)).Replace("{place}", place).Replace("{placeup}", place.ToUpperInvariant())
                .Replace("{town}", d.Town ?? d.Zone ?? "town").Replace("{zone}", d.Zone ?? "the hills")
                .Replace("{n}", Count(d)).Replace("{N}", Grammar.UpperFirst(Count(d)));
            if (who != null)
                s = s.Replace("{Who}", Grammar.UpperFirst(who)).Replace("{who}", who);
            return s;
        }

        /// Third person, for rumours and gossip: "the stranger robbed the bank in Rhodes".
        public static string Rumour(Deed d, string who)
        {
            Kind k;
            return Kinds.TryGetValue(d.Kind, out k) ? Fill(k.Rumour, d, who ?? "a stranger") : null;
        }

        /// What someone privately makes of it. who = how they think of the player ("that stranger", "Morgan").
        public static string Thought(Person p, Deed d, string who, string seed)
        {
            Kind k;
            if (!Kinds.TryGetValue(d.Kind, out k) || k.Thoughts == null || k.Thoughts.Length == 0)
                return null;
            string t = Fill(Voice.Pick(seed + d.Id, k.Thoughts), d, who);
            string tail = k.Violent ? Reaction(p, seed + d.Id) : null;
            return tail == null ? t : t + " " + tail;
        }

        private static string Reaction(Person p, string seed)
        {
            if (p == null || !Voice.Chance(seed + "r", 55))
                return null;
            switch (p.Personality)
            {
                case "Hothead": return Voice.Pick(seed, "Somebody ought to put him down.", "Let him try that with me.");
                case "Coward": return Voice.Pick(seed, "I'll stay off the roads a while.", "I'm keeping my head down.");
                case "Cold": return Voice.Pick(seed, "Not my concern. Not yet.", "Noted.");
                case "Proud": return Voice.Pick(seed, "He wouldn't dare try it on me.", "Men like that always meet a better man eventually.");
                case "Greedy": return Voice.Pick(seed, "There'll be a bounty on him. A big one.", "Somebody's going to get rich bringing him in.");
                case "Loyal": return Voice.Pick(seed, "If he comes near my people...", "I'll warn the others.");
                case "Drifter": return Voice.Pick(seed, "Maybe it's time I moved on.", "Country's getting crowded with his kind.");
            }
            return p.Devotion == "Devout" ? "Lord, keep him far from us." : null;
        }

        /// Said to his face by someone who saw or heard.
        public static string Remark(Deed d, string seed)
        {
            Kind k;
            if (!Kinds.TryGetValue(d.Kind, out k) || k.Remarks == null || k.Remarks.Length == 0)
                return null;
            return Fill(Voice.Pick(seed + d.Id, k.Remarks), d, "you");
        }

        /// Said to his face by someone who only heard about it.
        public static string HeardRemark(Deed d, string seed)
        {
            string core = Rumour(d, "you");
            if (core == null)
                return null;
            core = core.Replace("you was ", "you were ").Replace(" himself", " yourself").Replace(" his ", " your ");
            Kind k;
            bool grave = Kinds.TryGetValue(d.Kind, out k) && (k.Violent || k.Weight >= 25);
            if (!grave)
                return Voice.Pick(seed + d.Id, "Heard " + core + ".", "Somebody said " + core + ". Didn't figure you for it.", "Word gets around. " + Grammar.UpperFirst(core) + ", I hear.");
            switch (Voice.Hash(seed + d.Id) % 4)
            {
                case 0: return "Folks are saying " + core + ". That true?";
                case 1: return "Heard " + core + ". Tell me it ain't so.";
                case 2: return "Word is " + core + ".";
                default: return "They say " + core + ". I don't know what to believe.";
            }
        }

        /// A small newspaper item, if it's the kind of thing papers print.
        public static string Paper(Deed d, PlayerRef you, bool named, string seed)
        {
            Kind k;
            if (!Kinds.TryGetValue(d.Kind, out k) || k.Paper == null)
                return null;
            string desc = named ? "a man answering to the name of " + you.FullName
                : you.Scarred ? Voice.Pick(seed, "a scarred man on a dark horse", "a lean man with a scarred face")
                : Voice.Pick(seed, "a big man in a worn hat", "a rough-looking stranger", "a tall man who gave no name");
            return Grammar.Sentence(Fill(k.Paper, d, null).Replace("{desc}", desc));
        }

        /// One stranger telling another.
        public static List<string> Gossip(string rumour, string town, string seed)
        {
            string r = Grammar.SoftLower(rumour.TrimEnd('.'));
            switch (Voice.Hash(seed) % 4)
            {
                case 0:
                    return new List<string> { "You hear? " + Grammar.UpperFirst(r) + ".", Voice.Pick(seed + "b", "No!", "Lord.", "Huh. Can't say I'm surprised."), "That's what they're saying" + (town != null ? " in " + town : "") + "." };
                case 1:
                    return new List<string> { "Word is " + r + ".", "Who told you that?", Voice.Pick(seed + "c", "Everybody's telling it.", "Fella who saw it.", "Never you mind who.") };
                case 2:
                    return new List<string> { "They say " + r + ".", Voice.Pick(seed + "d", "I heard it different.", "I heard worse."), "Well. It grows every time somebody tells it." };
                default:
                    return new List<string> { Grammar.UpperFirst(r) + ". Swear to God.", "You're making that up.", "Ask anybody." };
            }
        }
    }
}
