// A quarrel played from the ladder (roadmap 7; the night run of 2026-10-28, item 16; Core/Escalation.cs worked out the
// ladder itself the afternoon before, unwired). Behind [Mind] MindLive and [Mind] QuarrelLadder the game side builds an
// argue or a brawl between two people the mind knows through here instead of the fixed scripts: the ladder is run from
// its start rung with the pair's traits and the surroundings as the game reads them, and each step becomes beats the
// Director already knows how to play (face, shove, fight and endfight, aim and holster, approach, walkaway). The words
// open by what it's over; the one who pushes a rung speaks and acts it; the one who brings it down ends it his way (an
// apology, a joke, backing down, walking off, a friend stepping in as the cast's third, a drink in a saloon, the fists'
// own verdict, the law in sight). The shot is NEVER played: a run that reaches it ends as a standoff that breaks off,
// and the scene says so (LadderNote), because nothing of the mind damages anybody yet (For Lex). The scene's Kind is the
// top rung reached (argue for words or a shove, brawl for blows, standoff for a gun drawn), so the ties, the memories,
// the plain record and the record fates all flow as they do for the fixed scenes. Off, nothing here runs.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static partial class Scenes
    {
        /// A brawl reason starts the ladder here (the blows rung): Reasons already read the tempers and the drink that made it a
        /// brawl, so the words, the shove and the fists are given, and the ladder decides how it ends (a man down, the law,
        /// a friend, an apology) and whether a gun comes out. An argue starts at words (0). At 1 (the shove) the ladder
        /// called the fists too, and a brawl reason came to blows only a third of the time (mindtest, 2026-10-29), a week
        /// after Lex asked for brawls often; the knob is here (For Lex).
        public const int BrawlStartRung = 2;

        /// What a quarrel is over, when the pair's own business doesn't say: a saloon's are drink and honour, the
        /// street's a look or a word over nothing, and money now and then.
        public static string MatterOf(bool saloon, bool drunk, string seed, bool strangers = false)
        {
            int r = Voice.Hash(seed + "|matter") % 100;
            if (strangers)
                return saloon ? (r < 50 ? "drink" : r < 80 ? "honour" : "nothing") : (r < 60 ? "nothing" : "honour");   // strangers owe each other nothing and share no fence (the review)
            if (saloon)
                return r < 40 ? "drink" : r < 70 ? "honour" : r < 85 ? "money" : "nothing";
            return r < 45 ? "nothing" : r < 75 ? "honour" : r < 90 ? "money" : "land";
        }

        /// The matter a worry between two people makes of their quarrel (a debt is money, a feud honour, courting a woman).
        public static string MatterOfWorry(string worryKind)
        {
            switch (worryKind)
            {
                case "debt": case "bank": case "cards": case "savings": return "money";
                case "feud": return "honour";
                case "courting": return "woman";
                case "drought": case "land": return "land";
                case "drink": return "drink";
                default: return null;
            }
        }

        /// The quarrel from the ladder. `startRung`: 0 words (an argue), 1 the shove (a brawl reason). `steps` gets the
        /// run for the log. A woman never goes past the shove and nobody draws what he doesn't carry (Escalation's own
        /// rules); the friend way needs the cast's third (`c.Friend` 0 or 1: whose friend), else it's a walk-off.
        public static Scene Quarrel(SceneCast c, string seed, Traits ta, Traits tb, QuarrelContext q, int startRung, out List<Escalation.Step> steps)
        {
            if (q == null)
                q = new QuarrelContext();
            if (c.Friend < 0)
                q.FriendOfA = q.FriendOfB = false;   // nobody to step in: the maths mustn't pick the friend way
            else if (c.Friend == 0)
                q.FriendOfB = false;
            else
                q.FriendOfA = false;
            steps = Escalation.RunFrom(startRung, ta, tb, q, seed + "|ladder");
            string top = Escalation.Reached(steps);
            Escalation.Step last = steps[steps.Count - 1];
            bool shot = last.Ended == "shot";
            if (shot)
                top = "draw";   // the shot isn't played: the standoff breaks off
            bool blows = top == "blows" || top == "draw";
            bool drew = top == "draw";
            bool twoMen = !c.AWoman && !c.BWoman;
            string a = N(c, 0, seed), b = N(c, 1, seed);
            Scene s = new Scene
            {
                Kind = drew ? "standoff" : blows ? "brawl" : "argue",
                Fight = blows,
                Cast = 2,
                Summary = drew ? (twoMen ? "two men squared off" : "two people squared off") + (q.Place == "saloon" ? " in the saloon" : " in the street")
                        : blows ? (twoMen ? "two men came to blows" : "two people came to blows") : "hard words" + (q.Place == "saloon" ? " in the saloon" : " in the street"),
                Ladder = steps
            };
            if (shot)
                s.LadderNote = "the ladder said a shot (" + Side(last.By, c.Names[0] ?? a, c.Names[1] ?? b, c) + "); not played, the standoff breaks off (the mind damages nobody yet)";
            // the words: what it's over
            string[] open = Opening(q.Matter, seed, a, b);
            s.Add(0, open[0], open[3], "face").Add(1, open[1], "no").Add(0, open[2], "angry");
            // the rungs already climbed before the ladder was asked (a brawl reason starts at the blows): A, who opened,
            // did them
            for (int r = 1; r <= startRung && r < Escalation.Rungs.Length; r++)
                Up(s, new Escalation.Step { From = Escalation.Rungs[r - 1], To = Escalation.Rungs[r], By = "a" }, c, seed, a, b, q);   // A: the reason's doer, who opened the words (the review: the aggrieved side belongs to the maths)
            // every beat of the ladder's names its people, not the winner and the loser: the Director's endfight swaps the cast by who
            // won, and a beat that follows its people swaps with them (Beat.Follows); only the fists' own verdict (beaten) is
            // written for the winner at 0 and the loser at 1 (the review)
            var fixedBeats = new List<Beat>();
            foreach (Escalation.Step st in steps)
            {
                if (st.To != null)
                    Up(s, st, c, seed, a, b, q);
                else if (st.Ended != null && !shot)
                {
                    int before = s.Beats.Count;
                    Down(s, st, c, seed, a, b, q, twoMen);
                    if (st.Ended == "beaten")
                        for (int i = before; i < s.Beats.Count; i++)
                            fixedBeats.Add(s.Beats[i]);
                }
            }
            if (shot)
            {
                // the draw played; the shot doesn't: the one who would have fired thinks better of it, and both put up
                int w = Who(last.By);
                s.Add(w, Voice.Pick(seed + "|noshot|" + w, "...Not today. Not for this.", "...No. Not over this.", "Go on. Walk, before I think better of it.",
                                    "...I'd hang for you, and you ain't worth the rope."), "mild", "holster")
                 .Add(1 - w, Voice.Pick(seed + "|noshot|other", "Another day, then.", "...Another day.", "Smart.", "I'll remember this."), "mild", "holster")
                 .Add(w, null, null, "walkaway").Add(1 - w, null, null, "walkaway");
            }
            foreach (Beat bt in s.Beats)
                bt.Follows = !fixedBeats.Contains(bt);
            return s;
        }

        private static int Who(string by)
        {
            return by == "b" || by == "friendB" ? 1 : 0;
        }

        private static string Side(string by, string a, string b, SceneCast c)
        {
            return by == "a" ? a : by == "b" ? b : by == "friendA" ? "a friend of " + a : by == "friendB" ? "a friend of " + b : by ?? "?";
        }

        /// The opening exchange: A opens, B answers, A presses; the fourth is A's opening voice.
        private static string[] Opening(string matter, string seed, string a, string b)
        {
            string k = seed + "|open|" + matter;
            switch (matter)
            {
                case "honour":
                    return Voice.Chance(k, 50)
                        ? new[] { "You called me a liar before the whole room, " + b + ".", "I called it as I saw it.", "Then you saw wrong, and you'll say so.", "angry" }
                        : new[] { "I hear you've been talking about me all over town.", "I say what I like.", "Not about me, you don't.", "threat" };
                case "woman":
                    return Voice.Chance(k, 50)
                        ? new[] { "Keep away from her, " + b + ". You've been told.", "She can talk to who she pleases.", "Not to you.", "threat" }
                        : new[] { "I saw you at her gate last night.", "A man can walk where he likes.", "Not past that gate, he can't.", "angry" };
                case "money":
                    return Voice.Chance(k, 50)
                        ? new[] { "You owe me four dollars, " + b + ", and I've waited long enough.", "You'll have it when I have it.", "I've heard that since Easter.", "mild" }
                        : new[] { "That hog was mine, and you sold it as yours.", "Prove it was.", "Half the county knows that hog.", "angry" };
                case "land":
                    return Voice.Chance(k, 50)
                        ? new[] { "Your fence is ten feet over my line.", "The line's where my father put it.", "Your father couldn't read a survey either.", "mild" }
                        : new[] { "You turned your cattle onto my water again.", "Water runs where it runs.", "And my grass goes with it.", "angry" };
                case "drink":
                    return Voice.Chance(k, 50)
                        ? new[] { "That was my glass, " + b + ".", "Was it. It's empty now.", "Then you'll be filling it.", "angry" }
                        : new[] { "You're leaning on me, " + b + ". Stand up straight or sit down.", "I'll lean where I like.", "Not on me, you won't.", "mild" };
                default:
                    return Voice.Chance(k, 50)
                        ? new[] { "You got something to say to me?", "Didn't say a word.", "You looked it.", "mild" }
                        : new[] { "Watch where you're walking.", "You walked into me.", "I'd say not.", "angry" };
            }
        }

        /// A rung climbed: the pusher speaks and does it.
        private static void Up(Scene s, Escalation.Step st, SceneCast c, string seed, string a, string b, QuarrelContext q)
        {
            int w = Who(st.By);
            string me = w == 0 ? a : b, other = w == 0 ? b : a;
            string k = seed + "|up|" + st.To + "|" + w;
            switch (st.To)
            {
                case "shove":
                    s.Add(w, Voice.Pick(k, "Out of my road!", "I said move!", "Say it again, close up!", "You want it, you'll have it!"), "angry", "shove");
                    s.Add(1 - w, Voice.Pick(k + "|answer", "Put your hands on me once more and I'll break them.", "That's the last time you touch me.", "You've done it now.",
                                             "Easy with those hands, " + me + "."), "angry");
                    break;
                case "blows":
                    s.Add(w, Voice.Pick(k, "That's it. Come here!", "I've heard enough out of you.", "Put up your hands!", "Right. Right, then."), "angry", "fight", 9000);
                    s.Add(0, null, null, "endfight");
                    break;
                case "draw":
                    // the gun comes out only where the cast may draw (BuildCast's rule: both armed, no law near, not a
                    // saloon); otherwise the same words over a hand drifting to the belt (the aim act then only faces)
                    bool gun = w == 0 ? c.Armed0 : c.Armed1;
                    s.Add(w, Voice.Pick(k, "Go on. Reach for it.", "You've got a gun on you. Use it or shut your mouth.", "Say it again with your hand on that iron.",
                                        "I've looked down a barrel before, " + other + ". Yours don't scare me."), gun ? "armed" : "threat", "aim", 300);
                    s.Add(1 - w, Voice.Pick(k + "|answer", "You first.", "Steady, now. Steady.", "They'd stretch your neck for it, " + me + ".", "Think hard before you do."), "stare", "aim", 300);
                    int wait = Voice.Hash(k + "|wait|pair") % 3;   // one roll picks the pair, so the answer answers (the review)
                    s.Add(w, new[] { "Last chance to walk away.", "Nobody has to die today.", "Well?" }[wait], null);
                    s.Add(1 - w, new[] { "I was about to say the same.", "Somebody always does.", "Well what?" }[wait], null);
                    break;
            }
        }

        /// The quarrel brought down at a rung, the way the cooler one (or a friend, or the law in sight) does it.
        private static void Down(Scene s, Escalation.Step st, SceneCast c, string seed, string a, string b, QuarrelContext q, bool twoMen)
        {
            int rung = Array.IndexOf(Escalation.Rungs, st.From);
            bool drawn = rung >= 3;
            string way = st.Ended;
            if (way == "friend" && c.Friend < 0)
                way = "walkoff";   // nobody to step in after all
            int w = way == "friend" ? (st.By == "friendA" ? 0 : 1) : Who(st.By);   // for friend: whose friend; he calms his own man
            string me = w == 0 ? a : b, other = w == 0 ? b : a;
            string own = c.Names != null && w < c.Names.Length && !string.IsNullOrEmpty(c.Names[w]) ? c.Names[w] : me;   // the friend knows his own man by name whatever the other does (the review)
            string k = seed + "|down|" + way + "|" + rung;
            if (drawn && way != "law")
            {
                // guns up before a word of it (the law way holsters as it goes)
                s.Add(0, null, null, "holster").Add(1, null, null, "holster");
            }
            switch (way)
            {
                case "apology":
                    s.Add(w, Voice.Pick(k, "...I spoke out of turn. I'm sorry for it.", "All right. That was wrong of me. I take it back.", "I had no call to say that. Forget it.",
                                        "My temper got the better of me. I'm sorry, " + other + "."), "sorry");
                    s.Add(1 - w, Voice.Pick(k + "|answer", "...Forget it, then.", "See that it stays took back.", "Hm. All right.", "Don't let it happen again."), "mild", "walkaway");
                    break;
                case "joke":
                    s.Add(w, twoMen
                            ? Voice.Pick(k, "Lord, listen to the pair of us. My wife would laugh herself sick.", "If you hit me, do it gentle. I've got a bad back.",
                                         "You're too ugly to hit, " + other + ". I'd only improve you.", "Let's not. I've only got the one good shirt.")
                            : Voice.Pick(k, "Lord, listen to the pair of us. The whole street's enjoying it.", "Let's not. I've only got the one good shirt.",
                                         "I've had this argument with my mother and lost. I know when I'm beat.", "We'll be the talk of the church supper at this rate."), "mild");
                    s.Add(1 - w, Voice.Pick(k + "|answer", "...Ha. Get out of here.", "You're a damned fool, " + me + ".", "Hm. Off with you, then.", "Lucky for you I'm thirsty."), "mild", "walkaway");
                    break;
                case "backdown":
                    s.Add(w, Voice.Pick(k, "...All right. All right, you win.", "I don't want trouble. Leave it there.", "Suit yourself, then.", "...Fine. I was wrong."), "mild");
                    s.Add(1 - w, Voice.Pick(k + "|answer", "That's what I thought.", "Then we understand each other.", "Good. Remember it.", "Go on, then."), "won");
                    s.Add(w, null, null, "walkaway");
                    break;
                case "walkoff":
                    s.Add(w, Voice.Pick(k, "I've no more time for this.", "Talk to the wind. I'm done.", "You ain't worth the trouble.", "Say what you like. I'm going."), "storm", "walkaway");
                    break;
                case "friend":
                    // the friend (the cast's third) comes between them and talks his man down; the hot one goes with him
                    s.Add(2, null, null, "approach");
                    Beat word = new Beat(2, Voice.Pick(k, "Easy, " + own + ". Easy. It ain't worth it.", "Leave it, " + own + ". Come on, leave it.", "That's enough, " + own + ". Not here.",
                                                        own + ". Walk with me. Now."), "intervene", "faceone") { To = w };
                    s.Beats.Add(word);
                    s.Add(w, Voice.Pick(k + "|answer", "...All right. I'm coming.", "Keep him off me, then.", "Another time, " + other + ".", "...Let go of my arm. I'm going."), "mild", "walkaway");
                    s.Add(2, null, null, "walkaway");
                    s.Cast = 3;
                    break;
                case "drink":
                    s.Add(w, Voice.Pick(k, "Ah, hell. Let me stand you one and we'll forget it.", "Sit down, " + other + ". I'll buy. Call it settled.",
                                        "This is thirsty work. Have a drink on me and we're square.", "Pour the man a whiskey and I'll pay. There."), "mild");
                    s.Add(1 - w, Voice.Pick(k + "|answer", "...Make it a double.", "Well. I never refused a drink yet.", "Hm. All right. Just the one.", "Now you're talking sense."), "cheer");
                    break;
                case "beaten":
                    // after endfight the Director has the winner at 0, whatever the ladder's By said (the fists decide)
                    s.Add(0, Voice.Pick(k, "Stay down.", "That'll learn you.", "Had enough?", "Mind your tongue next time."), "won");
                    s.Add(1, Voice.Pick(k + "|loser", "...Enough. I've had enough.", "Lucky punch.", "This ain't over.", "...My damned jaw."), "mild", "walkaway");
                    break;
                case "law":
                    s.Add(w, Voice.Pick(k, "The law's watching. Not here.", "Sheriff's looking this way. Leave it.", "There's the sheriff. Leave it, " + other + ".",
                                        "Not under the sheriff's nose."), "mild", drawn ? "holster" : null);
                    if (drawn)
                        s.Add(1 - w, null, null, "holster");
                    s.Add(w, null, null, "walkaway").Add(1 - w, null, null, "walkaway");
                    break;
                default:
                    s.Add(w, null, null, "walkaway");
                    break;
            }
        }

        /// Every line the ladder's scenes can say, for the line tools and the overlap checks.
        public static IEnumerable<string> AllQuarrelLines()
        {
            var cast = new SceneCast { Town = "Valentine" };
            cast.Names[0] = "Abe"; cast.Names[1] = "Hollis"; cast.Names[2] = "Cyrus";
            // a hot man and a cool one, so the runs climb and come down every way there is
            var hot = new Traits(); hot.Z[Traits.Anger] = 2f; hot.Z[Traits.Risk] = 1.5f; hot.Z[Traits.SelfControl] = -1.5f; hot.Z[Traits.Assertiveness] = 1f;
            var cool = new Traits(); cool.Z[Traits.Anger] = -1f; cool.Z[Traits.SelfControl] = 1.5f; cool.Z[Traits.Compassion] = 1f; cool.Z[Traits.Honesty] = 1f; cool.Z[Traits.Sociability] = 1f; cool.Z[Traits.Anxiety] = 1f;
            var seen = new HashSet<string>();
            foreach (string matter in new[] { "honour", "woman", "money", "land", "drink", "nothing" })
                for (int i = 0; i < 160; i++)
                {
                    List<Escalation.Step> steps;
                    bool saloon = i % 2 == 0;
                    var q = new QuarrelContext { Matter = matter, Place = saloon ? "saloon" : "street", Hour = saloon ? 23 : 15, DrunkA = i % 3 == 0, DrunkB = i % 5 == 0, ArmedA = i % 4 != 3, ArmedB = i % 4 != 0,
                                                 LawNear = i % 6 == 0, FriendOfA = true, FriendOfB = true, Witnesses = i % 7, Warmth = (i % 7) - 3, History = i % 4, Aggrieved = i % 3 == 0 ? "b" : null };
                    cast.Friend = (i % 3) - 1;
                    cast.Armed0 = cast.Armed1 = i % 2 == 1;
                    bool swap = i % 8 >= 4;
                    Scene s = Quarrel(cast, "ql" + matter + i, swap ? cool : hot, swap ? hot : cool, q, i % 3 == 2 ? BrawlStartRung : i % 3, out steps);
                    foreach (Beat bt in s.Beats)
                        if (bt.Line != null && seen.Add(bt.Line))
                            yield return bt.Line;
                }
        }
    }
}
