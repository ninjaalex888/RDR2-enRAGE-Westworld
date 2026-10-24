// Letters that find the player: from the kin of people he killed or spared, from sweethearts, from the dead
// who came back, from people who suspect the world is made, from couples inviting him to the wedding.
// They wait at a post office until he comes to town.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Letters
    {
        private static string Salute(PlayerRef you, bool knows, string fallback)
        {
            return knows ? "Mr. " + you.Surname + "," : fallback;
        }

        private static Paper Make(string title, string from, string fromId, string body, long now)
        {
            return new Paper { Id = Ids.New(), Kind = "letter", Title = title, From = from, FromId = fromId, Body = body, At = now };
        }

        /// From the kin of someone the player killed.
        public static Paper Grief(Relative r, string deadName, bool deadWoman, string relationToDead, PlayerRef you, bool knowsName, bool avenging, long now)
        {
            string seed = r.Name + deadName;
            string first = Person.FirstOf(deadName ?? "");
            string kin = relationToDead;   // "my son", "my brother"
            string salute = Salute(you, knowsName, Voice.Pick(seed, "To the man who killed my " + kin + ",", "To whoever you are,", "Sir,"));
            List<string> body = new List<string> { salute, string.Empty };
            if (avenging)
            {
                body.Add(Voice.Pick(seed + "a",
                    "They wired me from " + (r.Town == "Saint Denis" ? "the territory" : "out west") + " to say my " + kin + " " + first + " is dead, and that you did it.",
                    "I have read the telegram eleven times. It says " + first + " is dead. It says a man " + (knowsName ? "named " + you.Surname : "nobody can name") + " did it."));
                body.Add(Voice.Pick(seed + "b",
                    "I am leaving " + r.Town + " on the first train. I am not a violent man. I will become one for as long as it takes.",
                    "By the time you read this I will be on the road. I know your face from their descriptions. I will know it anywhere.",
                    "Pray, if you are the praying kind. I have stopped."));
            }
            else
            {
                body.Add(Voice.Pick(seed + "a",
                    first + " was my " + kin + ". I do not know what " + (deadWoman ? "she" : "he") + " did to you. I only know what you did to " + (deadWoman ? "her" : "him") + ".",
                    "I am told you killed my " + kin + ", " + first + ". I am told it was quick. I would like to believe that, at least.",
                    "There is a chair at our table nobody will sit in again. I wanted you to know that. I wanted somebody to carry it besides me."));
                body.Add(Voice.Pick(seed + "b",
                    r.Woman ? "I will not curse you. My mother taught me better. But I will not forgive you either." : "I am too old to come after you. Be grateful for that.",
                    "If there is a God, you will meet " + (deadWoman ? "her" : "him") + " again. I hope " + (deadWoman ? "she" : "he") + " is kinder to you than you were.",
                    "I hope you dream about it. I do, every night, and I wasn't even there."));
            }
            body.Add(string.Empty);
            body.Add(r.Name + ", " + r.Town);
            return Make("A letter from " + r.Name + ", " + r.Town, r.Name, null, string.Join("\n", body), now);
        }

        /// From someone (or their kin) the player was good to.
        public static Paper Thanks(string writer, string writerTown, string about, bool aboutSelf, string what, PlayerRef you, bool knowsName, long now)
        {
            string seed = writer + what;
            string salute = Salute(you, knowsName, "To the stranger who " + what + ",");
            string line = aboutSelf
                ? Voice.Pick(seed, "I never thanked you properly. So: thank you. You didn't have to " + Grammar.LowerFirst(Bare(what)) + ". Most wouldn't have.",
                    "I've told everyone I know what you did. Some of them don't believe me. I don't care.")
                : Voice.Pick(seed, "My " + about + " wrote to tell me you " + what + ". I pray for you every night now, whoever you are.",
                    "I have never met you and I owe you everything. My " + about + " is alive because of you.");
            string body = salute + "\n\n" + line + "\n\n" + Voice.Pick(seed + "e", "If you are ever in " + writerTown + ", there is a hot meal and a bed for you.", "God keep you.", "With gratitude,") + "\n\n" + writer;
            return Make("A letter from " + writer, writer, null, body, now);
        }

        private static string Bare(string what)
        {
            // "let my son go" -> "let him go" is good enough; keep the phrase as is
            return what;
        }

        public static Paper Love(Person p, PlayerRef you, bool knowsName, int heart, long now)
        {
            string seed = p.Id + heart + now / 4320;
            string name = knowsName ? you.FirstName : "stranger";
            string body = Voice.Pick(seed, "Dear " + name + ",", "My " + name + ",", name + " -") + "\n\n" +
                Voice.Pick(seed + "1", "It's been days since I saw you and I keep looking down the road like a fool.",
                    "I wrote you three letters and burned two. This is the one that's left, so it'll have to do.",
                    "I don't know where you sleep at night. I hope it's somewhere safe. I hope you think of me there.",
                    "Mama says a man like you is trouble. I told her I know. I told her I don't care.",
                    "I pressed a flower in this. If it fell out, go back and find it.",
                    "The town is dull as ditchwater without you. Even the drunks are behaving.") + " " +
                Voice.Pick(seed + "2", "Come back to " + (p.Home ?? "town") + " soon.", "Don't you dare get yourself killed.", heart >= 4 ? "You have my heart. Mind you don't break it." : "Maybe next time you'll stay for supper.") +
                "\n\n" + Voice.Pick(seed + "3", "Yours,", "Fondly,", "Always,") + "\n" + p.FirstName;
            return Make("A letter from " + p.Name, p.Name, p.Id, body, now);
        }

        public static Paper Scorned(Person p, PlayerRef you, bool knowsName, long now)
        {
            string body = Salute(you, knowsName, "You,") + "\n\n" +
                Voice.Pick(p.Id + "sc", "I won't waste much ink on you. You've wasted enough of my time already.",
                    "I hear you've been sweet-talking half the territory. I hope they see you for what you are quicker than I did.",
                    "I burned your letters. Then I realised you never sent me any.") +
                "\n\n" + Voice.Pick(p.Id + "sc2", "Don't come back to " + (p.Home ?? "town") + ".", "Go to hell.", "Goodbye.") + "\n" + p.FirstName;
            return Make("A letter from " + p.Name, p.Name, p.Id, body, now);
        }

        /// A host who suspects the truth writes to the one man who seems to come from outside.
        public static Paper Confession(Person p, PersonState st, PlayerRef you, bool knowsName, long now)
        {
            string seed = p.Id + "conf" + now / 10080;
            string body = Salute(you, knowsName, "To the rider who comes back,") + "\n\n" +
                "I have to tell someone and you are the only one who seems to be outside of it. " +
                Hosts.Thought(p, st, you, seed) + " " +
                Voice.Pick(seed + "2", "I've started marking the days on the wall. Somebody keeps washing the marks off.",
                    "Yesterday the man at the store asked me the same question four times. Same words. He didn't know.",
                    "If you know what we are, please. Just tell me. I can bear it. I think.") +
                "\n\nBurn this.\n" + p.FirstName;
            return Make("An unsigned letter, smudged", p.Name, p.Id, body, now);
        }

        public static Paper RevenantNote(Fallen f, PlayerRef you, long now)
        {
            return new Paper { Id = Ids.New(), Kind = "note", Title = "A note in an unsteady hand", From = f.Name ?? "unknown", Body = Hosts.RevenantNote(f, you, f.Id + now), At = now };
        }

        public static Paper Invitation(Wedding w, PlayerRef you, bool knowsName, string when, long now)
        {
            string body = Salute(you, knowsName, "Friend,") + "\n\n" +
                w.AName + " and " + w.BName + " are to be married at the church in " + w.Town + ", " + when + ". " +
                Voice.Pick(w.Id, "It would mean a great deal if you came.", "Come as you are. Leave the guns at the door.", "There'll be cake, and a fiddler, and not much else.") +
                "\n\n" + Person.FirstOf(w.AName) + " & " + Person.FirstOf(w.BName);
            return Make("A wedding invitation", w.AName, w.AId, body, now);
        }

        /// A tip from someone who owes the player.
        public static Paper Tip(Person p, string plan, PlayerRef you, bool knowsName, long now)
        {
            string body = Salute(you, knowsName, "Friend,") + "\n\n" +
                Voice.Pick(p.Id + plan, "I owe you, so here it is and then we're square. ", "Figured you should hear this before anybody else does. ") +
                Grammar.Sentence(plan) + "\n\n" + Voice.Pick(p.Id + "t", "Burn this.", "We never spoke.", "Good luck.") + "\n" + p.FirstName;
            return Make("A letter from " + p.Name, p.Name, p.Id, body, now);
        }
    }
}
