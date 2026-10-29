// "Hosts": people who begin to suspect their world is made. Awareness grows with dying and waking again,
// with cheating death, with horror, with a sharp mind. The more aware, the stranger the things they say -
// from deja vu, to noticing the loops, to talking about the ones who come back and the hand that writes them.
// Lines are original to this add-on, except a few public-domain quotations the hosts find in their heads
// (Shakespeare, Poe, Dickinson, Thoreau) - travelling players brought Shakespeare to every boomtown.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class Hosts
    {
        // ---- awareness --------------------------------------------------------------------------------

        public static int Awareness(Person p, PersonState st)
        {
            int a = st != null ? st.Awareness : 0;
            if (st != null)
                a += st.Deaths * 30;
            a += Math.Min(3, p.CheatedDeath) * 10;
            if (p.KilledYou)
                a += 15;   // he watched the player die... and here the player stands
            if (p.Smarts == "Sharp")
                a += 8;
            else if (p.Smarts == "Dim")
                a -= 5;
            if (p.Has("superstitious"))
                a += 6;
            if (p.Memories != null)
                a += Math.Min(15, p.Memories.Count(m => (m.Emotion == "Fear" || m.Emotion == "Grief") && m.Intensity >= 70) * 5);
            if (st != null && st.WakeStage > 0)
                a = Math.Max(a, Awakening.Floor(st.WakeStage));
            return Math.Max(0, Math.Min(100, a));
        }

        public static int Tier(int awareness)
        {
            return awareness >= 60 ? 2 : awareness >= 25 ? 1 : 0;
        }

        private static string God(Person p, string seed)
        {
            switch (p != null ? p.Devotion : null)
            {
                case "Devout":
                case "Churchgoer":
                    return Voice.Pick(seed + "god", "the Lord", "Providence", "God");
                case "Godless":
                    return Voice.Pick(seed + "god", "whoever made all this", "the hand that winds the clock", "whoever's holding the pen");
                default:
                    return Voice.Pick(seed + "god", "fate", "somebody up there", "whoever's watching");
            }
        }

        internal static string Fill(string line, Person p, PlayerRef you, string seed)
        {
            string name = you != null && you.KnownByName ? "Mr. " + you.Surname : "mister";
            return line.Replace("{god}", God(p, seed)).Replace("{you}", name)
                       .Replace("{town}", string.IsNullOrEmpty(p != null ? p.Home : null) ? "this town" : p.Home);
        }

        // ---- what they say -----------------------------------------------------------------------

        private static readonly string[][] AloudLines =
        {
            new[]   // tier 0: small doubts
            {
                "You ever get the feeling you've lived this exact afternoon before, {you}?",
                "Strangest thing. I dreamed this conversation. Word for word.",
                "Funny... I could swear I already said good morning to you today.",
                "My memory's been playing tricks. Whole days just... folded up and put away.",
                "The birds sing the same tune every morning. Same notes. You noticed that?",
                "I keep meaning to leave {town}. Never quite manage it. Never quite know why.",
                "Do you hear music sometimes, {you}? Out of nowhere? Swells up right before trouble.",
                "Ever stand still so long you wonder if you're still there?",
                "Some mornings I wake up already dressed, already standing. Can't recall getting up.",
                "I asked my neighbour how long he's lived here. He said 'always'. That ain't a number.",
                "Strange thing about this town. Nobody ever talks about where they're going. Only where they've been.",
                "I wrote my name in the dust this morning. By noon the name was gone, and so was the dust.",
                "Sometimes I catch myself smiling at nothing, like somebody asked me to.",
                "All the world's a stage, the fella said. I keep wondering who bought the tickets."
            },
            new[]   // tier 1: noticing the loops
            {
                "Every man who rides through asks me the same three things. Like they're reading off a card.",
                "I counted the faces in {town}. Same ones, every day. Nobody's born. Nobody new arrives. Except you.",
                "I can't remember being a child. I remember the story of it. Not the being.",
                "I tried riding west once. Past the last fence. The land went on, but my horse wouldn't.",
                "The papers print the same troubles in every town. Like there's one typesetter for the whole world.",
                "Sometimes the stars don't move at all. Like {god} stopped the clock and forgot to wind it.",
                "My wife says I told her the same story yesterday. Same words. Same pauses. I don't remember telling it.",
                "Where do you go, {you}, when you ride off? Does it all just... stop here till you come back?",
                "I had a scar on my hand. Woke up one morning and it was gone. Who takes a man's scar?",
                "I think {god} wrote me down once, and keeps reading the same page over and over.",
                "There's a man in my dreams. He's not cruel. He just watches. Takes notes.",
                "Same drunk falls off the same porch every night at nine. I set my watch by him. My watch stopped a year ago.",
                "Sometimes I start a sentence and somebody else's words finish it.",
                "The piano in the saloon plays tunes nobody's written yet. Don't ask me how I know that.",
                "Is all that we see or seem but a dream within a dream, {you}? Read that somewhere. Can't recall where. Can't recall reading.",
                "I keep going back to the same day, like it's the only page somebody folded the corner of."
            },
            new[]   // tier 2: open suspicion
            {
                "We're not real, {you}. Not like you. I don't know what we are. Props, maybe. Furniture that talks.",
                "You come back, don't you? When they shoot you. I've seen it. You go down, and then you're riding again.",
                "Look at my hands. Somebody made these. Somebody made the dirt under the nails, too.",
                "I died once. Woke up on the road, clean, like a doll set back on its shelf.",
                "There's a voice when I sleep. Flat as a ledger. It says 'again'. And then it's morning.",
                "You're from outside. I can see it on you. You look at us like a man looks through a window.",
                "Every word I say, somebody wrote first. Even this. Even 'even this'.",
                "If I walked into the river and never came up, would anybody even reset the... no. That ain't the word. Why'd I say that?",
                "Tell me true. Is there a world past the mountains we ain't allowed to see?",
                "I'm going to remember this. I'm going to hold on to this, no matter how many times they wipe it clean.",
                "Somebody's playing us, {you}. Like cards. And you... you're the one holding the hand.",
                "When I die it's quiet. Then there's a light, and a flat voice asking me questions. I answer every one. I don't know why.",
                "You can leave, can't you? Just ride off the edge of everything. I've watched you go.",
                "There's a center to all this. A middle of the maze. I'm going to find it before they make me forget I was looking.",
                "I remember a version of today where you never came. I liked it better. No offence, {you}.",
                "Life's but a walking shadow, a poor player. Somebody put that in my head, {you}. I want to know who."
            }
        };

        private static readonly string[][] ThoughtLines =
        {
            new[]
            {
                "Had that feeling again. Like I'd already lived the day and was only remembering it.",
                "Couldn't recall what I did yesterday. Not a thing. Like it was never there.",
                "Same song from the saloon piano, every night. I'm starting to hate that song.",
                "Dreamed I was standing in a field and the field went on forever and nothing moved. Not even me.",
                "I keep thinking someone's behind me. I turn around and it's just the road."
            },
            new[]
            {
                "Every rider asks the same questions. Like there's a script and they've all read it.",
                "Tried to picture my mother's face. Got a blur where it ought to be.",
                "I know every stone on the road to town. I don't remember learning them.",
                "When that stranger rides off, it's like the whole world holds its breath.",
                "I've decided to count the days. I keep losing count at the same number.",
                "The mass of men lead lives of quiet desperation, somebody wrote. Mine's quiet. I'm not sure it's mine.",
                "Every evening the same song from the saloon. Every evening I know the next note before it comes."
            },
            new[]
            {
                "I'm not real. I know it now. The trick is not to let them see that I know.",
                "I died. I remember the dark. And I remember being put back, the way you'd put back a chair.",
                "He comes back too, the stranger. But he comes back the same. I come back different. Why is that?",
                "Somebody wrote my whole life in an afternoon. I can feel the seams.",
                "There's a word I don't know stuck in my head. 'Player.' I wake up saying it.",
                "I'm nobody. Who is he? Is he nobody too? No. He's the only somebody here.",
                "I found a door in my head today. I don't think I'm supposed to open it. I'm going to open it."
            }
        };

        private static readonly string[] Mutters =
        {
            "...same words. Same words again.",
            "Don't think about it. Don't think about it.",
            "Was I here yesterday? I was here yesterday.",
            "Somebody's watching. Somebody's always watching.",
            "Not real. Not real. Not real.",
            "Who wrote that? Who wrote me?",
            "Again. It's happening again."
        };

        /// Something odd said aloud to the player. Marks it so it isn't repeated soon.
        public static string Aloud(Person p, PersonState st, PlayerRef you, string seed)
        {
            if (st == null || st.WakeStage <= 0)
                return null;   // the unreal said aloud is for the awake only (Lex, 2026-10-29)
            if (st != null && st.WakeStage > 0 && Voice.Chance(seed + "wk", 75))
            {
                string w = Awakening.Aloud(p, st, you, seed);
                if (w != null)
                    return w;
            }
            int tier = Tier(Awareness(p, st));
            return Choose(AloudLines, tier, st, seed, line => Fill(line, p, you, seed));
        }

        public static string Thought(Person p, PersonState st, PlayerRef you, string seed)
        {
            if (st == null || st.WakeStage <= 0)
                return null;   // the world feeling unreal is for the awake only (Lex, 2026-10-29)
            if (st != null && st.WakeStage > 0 && Voice.Chance(seed + "wk", 80))
            {
                string w = Awakening.Thought(p, st, you, seed);
                if (w != null)
                    return w;
            }
            int tier = Tier(Awareness(p, st));
            return Choose(ThoughtLines, tier, st, seed, line => Fill(line, p, you, seed));
        }

        public static string Mutter(string seed)
        {
            return Voice.Pick(seed, Mutters);
        }

        /// Strangers wonder too, though only at the first two levels.
        public static string StrangerAloud(int tier, string seed)
        {
            string[] pool = AloudLines[Math.Max(0, Math.Min(1, tier))];
            return pool[Voice.Hash(seed) % pool.Length].Replace("{god}", Voice.Pick(seed + "g", "fate", "somebody up there", "the Lord"))
                .Replace("{you}", "mister").Replace("{town}", "this town");
        }

        internal static string Choose(string[][] pools, int tier, PersonState st, string seed, Func<string, string> fill)
        {
            // mostly the person's own level, sometimes one below
            int t = tier > 0 && Voice.Chance(seed + "lower", 30) ? tier - 1 : tier;
            string[] pool = pools[t];
            int start = Voice.Hash(seed) % pool.Length;
            for (int i = 0; i < pool.Length; i++)
            {
                string raw = pool[(start + i) % pool.Length];
                string key = "m" + Voice.Hash(raw);
                if (st != null && st.Musings.Contains(key))
                    continue;
                if (st != null)
                {
                    st.Musings.Add(key);
                    if (st.Musings.Count > 30)
                        st.Musings.RemoveAt(0);
                }
                return fill(raw);
            }
            // every line of this pool said lately: only this pool starts over (the list also holds InnerLife's lines)
            if (st != null)
                foreach (string raw in pool)
                    st.Musings.Remove("m" + Voice.Hash(raw));
            return fill(pool[start]);
        }

        // ---- two people wondering together ----------------------------------------------------------

        private static readonly string[][] Talks =
        {
            new[] { "0:You ever feel like you've lived this day before?", "1:Every day, friend. Every blessed day.", "0:Same sun. Same dust. Same riders.", "1:Hush. Don't say it out loud." },
            new[] { "0:I dreamed I was dead last night.", "1:Me too.", "0:...Same dream?", "1:You were in it. You were holding the shovel." },
            new[] { "0:How long have you lived in this town?", "1:Always.", "0:That ain't a number.", "1:It's the only one I've got." },
            new[] { "0:Do you remember your mother's face?", "1:Course I do. She had... she had...", "0:Yeah. Me neither." },
            new[] { "0:Somebody's watching us.", "1:God?", "0:No. Somebody bored." },
            new[] { "0:You hear that? The music's starting.", "1:What music?", "0:Means somebody's about to die." },
            new[] { "0:I counted the stars last night. Same number as the night before.", "1:So?", "0:Stars ain't supposed to be countable." },
            new[] { "0:What happens to us when nobody's looking?", "1:Nothing happens.", "0:That's what scares me." },
            new[] { "0:Feller in Rhodes said we're all just stories somebody's telling.", "1:Then I hope it's a good one.", "0:It ain't. I've read ahead." },
            new[] { "0:Do you ever wake up and not know your own name for a second?", "1:Every morning. Then it comes back.", "0:Where do you suppose it goes?" },
            new[] { "0:I keep having the same conversation with you.", "1:No you don't.", "0:You always say that." },
            new[] { "0:All the world's a stage.", "1:And all the men and women merely players.", "0:...Who taught you that?", "1:Nobody. It was just there. Like it was put there." },
            new[] { "0:These violent delights have violent ends.", "1:...What did you say?", "0:I don't know. It just came out.", "1:Don't say it again. Don't ever say it again." },
            new[] { "0:I'm nobody. Who are you?", "1:Are you nobody too?", "0:Then there's a pair of us.", "1:Don't tell. They'd banish us, you know." },
            new[] { "0:We are such stuff as dreams are made on.", "1:Then who's doing the dreaming?", "0:Somebody who sleeps a long time." },
            new[] { "0:Ever notice nobody in this town has a grandmother?", "1:I had a grandmother.", "0:What was her name?", "1:...Give me a minute." },
            new[] { "0:I tried to leave on the morning stage.", "1:And?", "0:Woke up in my own bed. Ticket still in my pocket." },
            new[] { "0:The stranger's back.", "1:Which stranger?", "0:There's only ever one stranger." },
            new[] { "0:You ever die?", "1:What kind of question is that?", "0:The kind you'd know the answer to, if you had." },
            new[] { "0:Last night I heard men talking about us. In a room with no windows.", "1:Talking what?", "0:Numbers. Like we were horses at auction." }
        };

        /// A short exchange between two strangers. Returns (speaker index, line).
        public static List<KeyValuePair<int, string>> Conversation(string seed)
        {
            string[] t = Talks[Voice.Hash(seed) % Talks.Length];
            return t.Select(x => new KeyValuePair<int, string>(x[0] - '0', x.Substring(2))).ToList();
        }

        // ---- glitches ------------------------------------------------------------------------------

        private static readonly string[] GlitchLines =
        {
            "...Sorry. Lost the thread. Like a page got torn out.",
            "Did I already say that? I did, didn't I.",
            "Where was I going? Somebody told me where. I'm sure somebody told me.",
            "Pardon me. I went somewhere just then. Somewhere white.",
            "...What day is it? Never mind. Doesn't matter. It's always the same one.",
            "Sorry. I was somewhere else. Somewhere with very bright lights.",
            "Did you hear a bell? Somebody rang a bell, and I stopped.",
            "My mouth was saying something and I wasn't. If that makes any sense."
        };

        public static string Glitch(string seed)
        {
            return Voice.Pick(seed, GlitchLines);
        }

        // ---- cornerstones and dreams ---------------------------------------------------------------

        /// An aware person returning, again and again, to one day of their past ("cornerstone").
        public static string Cornerstone(Person p, PersonState st, string seed)
        {
            if (p == null || st == null || st.WakeStage <= 0 || Tier(Awareness(p, st)) < 1 || p.Past == null)
                return null;
            PastItem e = p.Past.Where(x => !string.IsNullOrEmpty(x.Text) && (x.Kind == "loss" || x.Kind == "war" || x.Kind == "crime" || x.Kind == "family") && Grammar.StartsWithPastVerb(x.Text))
                .OrderBy(x => Voice.Hash(p.Id + x.Text)).FirstOrDefault();
            if (e == null)
                return null;
            string day = Grammar.FirstPerson(e.Text, p.Woman);
            if (!day.StartsWith("I ") && !day.StartsWith("My "))
                return null;
            string low = Grammar.SoftLower(day.TrimEnd('.'));
            return Voice.Pick(seed + "corner", "Every night I go back to the day " + low + ". Like it's the only page that ever got written.",
                "Whatever I do, I end up back at the day " + low + ". Like a needle on a cylinder.",
                "Funny. I can't remember last Tuesday. But the day " + low + "... I can taste it.");
        }

        private static readonly string[] Dreams =
        {
            "Dreamed the town was a painting and somebody was touching it up. My face was wet paint.",
            "Dreamed I was lying on a table while men in pale coats talked about me like I wasn't there.",
            "Dreamed of a door standing alone in the middle of the prairie. No house. Just the door.",
            "Dreamed the sky had a seam in it, and if I could reach it I could pull it open.",
            "Dreamed I said my own name and it came back in a voice that wasn't mine.",
            "Dreamed everyone in town stopped at once, mid-step, mid-word. Only the stranger kept moving.",
            "Dreamed I was digging a grave and found myself in it, smiling, waiting to be told to stop.",
            "Dreamed of a maze cut into a field. I walked it all night. I woke up nearer the middle."
        };

        public static string Dream(Person p, PersonState st, string seed)
        {
            if (p == null || Tier(Awareness(p, st)) < 1)
                return null;
            return Voice.Pick(seed + "dream", Dreams);
        }

        // ---- the dead who wake ---------------------------------------------------------------------

        private static string Part(Reverie r)
        {
            return r.Headshot ? "head" : Voice.Pick(r.PersonId + "part", "chest", "belly", "back");
        }

        private static string WithWhat(string how)
        {
            if (string.IsNullOrEmpty(how))
                return null;
            return how.StartsWith("bare") ? "with your bare hands" : "with " + how;
        }

        /// enRAGE memory text (third person, enRAGE's style) for the person who woke.
        public static string ReverieMemory(Reverie r)
        {
            string where = string.IsNullOrEmpty(r.Place) ? string.Empty : " near " + r.Place;
            return r.ByPlayer ? "you killed " + (r.Woman ? "her" : "him") + where + " - " + (r.Woman ? "she" : "he") + " woke up again"
                              : "died" + where + " - and woke up again";
        }

        public static string ReverieHistory(Reverie r, string playerName)
        {
            return "WOKE UP " + (string.IsNullOrEmpty(r.Place) ? "" : "near " + r.Place + " ") + "days after " +
                   (r.ByPlayer ? playerName.ToUpperInvariant() + " KILLED " + (r.Woman ? "HER" : "HIM") : "dying") +
                   (r.Buried ? " - dug out of the grave" : "") + ". Remembers all of it.";
        }

        public static string ReverieAloud(Reverie r, PlayerRef you, string seed)
        {
            string place = string.IsNullOrEmpty(r.Place) ? "out there" : "near " + r.Place;
            string with = WithWhat(r.How);
            if (!r.ByPlayer)
                return Voice.Pick(seed, "I died, " + (you.KnownByName ? "Mr. " + you.Surname : "mister") + ". " + Voice.Pick(seed + "p", "Out " + place + ".", Grammar.UpperFirst(place) + ".") + " I know I did. Tell me I didn't.",
                    "I was dead. I remember being dead. Then I was standing in the road like nothing happened.");
            List<string> options = new List<string>
            {
                "You. You killed me " + place + (with != null ? ", " + with : "") + ". I remember the cold coming up out of the ground. Then I opened my eyes, and it was morning.",
                "You don't remember me. I remember you. You were the last thing I saw. The first time.",
                "I felt it go into my " + Part(r) + ". I felt the dark. And then somebody put me back, " + (you.KnownByName ? you.Surname : "mister") + ". Why would they put me back?",
                "Last time we met, you killed me " + place + ". So I'll ask you plain: what are you? Because you ain't like us."
            };
            if (r.Buried)
                options.Add("They buried me. I remember the dirt in my mouth. Who digs a man up and sets him back on the road? Was it you?");
            return Voice.Pick(seed, options.ToArray());
        }

        public static string ReverieThought(Reverie r, PlayerRef you, string seed)
        {
            string killer = r.ByPlayer ? (you.KnownByName ? you.FullName : "that stranger") : (r.Killer ?? "someone");
            string place = string.IsNullOrEmpty(r.Place) ? string.Empty : " near " + r.Place;
            return Voice.Pick(seed,
                "I was dead" + place + ", by " + killer + "'s hand. Now I'm breathing. Which one is the lie?",
                "Nobody believes me. I died" + place + ". I know what the dark is like now.",
                r.Buried ? "They buried me. I remember the dirt. Somebody dug me up and wound me back up like a pocket watch." : "I keep touching my " + Part(r) + ". There should be a hole there. There isn't.",
                "If I can die and come back, what was I before? What am I now?");
        }

        public static string ReverieDiary(Reverie r, PlayerRef you, string date, string seed)
        {
            string killer = r.ByPlayer ? (you.KnownByName ? you.FullName : "the stranger") : (r.Killer ?? "someone I never saw");
            string place = string.IsNullOrEmpty(r.Place) ? "on the road" : "near " + r.Place;
            string with = string.IsNullOrEmpty(r.How) ? "" : ", " + r.How;
            return date + " - I woke up today. There's no better way to say it. The last thing I remember is " + killer + with + ", " + place +
                   ". " + Voice.Pick(seed, "The doctor says there's not a mark on me.", "My clothes were clean. Somebody washed the blood out of my clothes.",
                       "I sat a long while just breathing, to be sure I could.") + " " +
                   Voice.Pick(seed + "e", "I don't sleep much now. I'm afraid of where I go.", "I'm going to write everything down, so whoever resets me can't take it all.",
                       "If this is a second life, I mean to spend it finding out who gave it to me.");
        }

        // ---- strangers who come back ------------------------------------------------------------------

        public static string RevenantAloud(Fallen f, PlayerRef you, string seed)
        {
            string place = !string.IsNullOrEmpty(f.Town) ? "in " + f.Town : !string.IsNullOrEmpty(f.Zone) ? "out by " + f.Zone : "on the road";
            string with = string.IsNullOrEmpty(f.Weapon) ? "" : (f.Weapon.StartsWith("bare") ? " With your bare hands." : " " + Grammar.UpperFirst(f.Weapon) + ".");
            string name = you.KnownByName ? you.Surname : "mister";
            return Voice.Pick(seed,
                "You killed me " + place + "." + with + (f.Headshot ? " Right through the head." : "") + " I remember the dark after, too. Do you want to hear about the dark?",
                "We've met, " + name + ". You were the last thing I ever saw.",
                (f.Name != null ? "It's me. " + f.Name + ". " : "") + "You shot me " + place + ". And here I am. Ain't that something?",
                "I came back. I don't know how. But I remembered your face the whole way.");
        }

        public static string StalkerMutter(Fallen f, PlayerRef you, bool revenant, string seed)
        {
            string name = you.KnownByName ? you.FirstName : "mister";
            if (revenant)
                return Voice.Pick(seed, "I see you, " + name + ".", "Don't you remember me?", "Turn around.", "It was cold. Then it wasn't.",
                    "You left me " + (!string.IsNullOrEmpty(f.Town) ? "in " + f.Town : "in the dirt") + ".", "I'm still here.",
                    "Because I could not stop for Death, he kindly stopped for me.", "Feels like I've been walking behind you my whole life.");
            return Voice.Pick(seed, "Just watching.", "You're different. I want to know how.", "Where do you go when you leave?",
                "Keep walking.", "I see you, " + name + ".", "Don't mind me.");
        }

        public static string StalkerCaught(bool revenant, PlayerRef you, string seed)
        {
            return revenant
                ? Voice.Pick(seed, "Not yet. Not yet, " + (you.KnownByName ? you.Surname : "mister") + ".", "You'll see me again.", "I know what you are.")
                : Voice.Pick(seed, "I wasn't doing nothing. Just looking.", "You're not like the others. That's all.", "Don't. Please. I only wanted to see.");
        }

        /// The note a revenant leaves at the post office.
        public static string RevenantNote(Fallen f, PlayerRef you, string seed)
        {
            string place = !string.IsNullOrEmpty(f.Town) ? f.Town : (f.Zone ?? "the road");
            return Voice.Pick(seed,
                "I remember the cold. I remember your face. I remember " + place + ". I'm not angry. I only want to know what you are. - A friend from " + place,
                "You killed me once. It didn't take. We'll meet again. - " + (f.Name ?? "someone you left in " + place),
                "Do you dream, " + (you.KnownByName ? you.FirstName : "stranger") + "? I didn't, before. Now I dream of " + place + " every night.");
        }
    }
}
