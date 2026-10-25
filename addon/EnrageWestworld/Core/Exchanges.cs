// Short exchanges between two people, written for the reason they have something to do with each other (Reasons.cs):
// someone at the worst of their trouble snaps when spoken to, someone whose trouble ended tells it (good or bad), a
// quarrel picks up again and is sometimes made up, sweethearts greet. Whoever's reason it is speaks to their own
// trouble, with its people named (the same names as their thoughts: InnerLife.WorryFill); the other one answers kindly,
// coldly or sharply, from their own temper and whether they're at the worst of their own trouble. How it went is the
// scene's kind, which is what it leaves between them (Ties.Effect). All lines are original to this add-on.
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class Exchanges
    {
        // ---- the one at the worst of it, spoken to ------------------------------------------------------

        private static readonly string[] Hello =
        {
            "Morning, {a}.", "{a}. Hot one today.", "You look done in, {a}.", "{a}! Haven't seen you about."
        };

        private static readonly Dictionary<string, string> Snap = new Dictionary<string, string>
        {
            { "debt", "Not now. I owe {who} more than I've got, and the day's coming." },
            { "fever", "Not now. {who}'s burning up, and the doctor wants two dollars to ride out." },
            { "letter", "Leave me be. Still nothing from {kin}. Not a word." },
            { "drought", "Don't say hot to me. Well's near dry and the sky won't give." },
            { "son", "Not today. No word from {who}. Not one, since {place}." },
            { "courting", "Leave off. I've a thing to ask somebody and no nerve to ask it." },
            { "feud", "If {who} walks by while you're talking, I won't answer for myself." },
            { "bank", "There's a bank notice nailed to my door. So no. It isn't a fine morning." },
            { "drink", "Third day dry. Talk to me in a week." },
            { "savings", "Somebody's been at my savings. And no, I won't say who I suspect." },
            { "tooth", "Can't talk. Tooth. Like a nail driven in my jaw." },
            { "secret", "Why? Who's been asking about me?" },
            { "law", "That rider at the livery. Who's he been talking to? You see him?" }
        };

        // ---- news, told -----------------------------------------------------------------------------------

        private static readonly Dictionary<string, string> GoodNews = new Dictionary<string, string>
        {
            { "debt", "Paid {who} off this morning. Every cent. I could dance." },
            { "fever", "Fever broke! {who} sat up and asked for eggs." },
            { "letter", "Letter came from {who}. Alive, well, and wordy as ever." },
            { "drought", "Hit sweet water at last. Six feet deeper, but I hit it." },
            { "son", "{who}'s home! Ate like three men and slept like a fourth." },
            { "courting", "{who} said yes. Don't tell anybody. Tell everybody." },
            { "feud", "Shook hands with {who}. Would you credit it?" },
            { "bank", "Bank gave me another year. A whole year." },
            { "drink", "Thirty days dry. Coffee's on me, if you'll have coffee." },
            { "savings", "Bought my ticket. {place}. I'm going." },
            { "tooth", "Barber had the tooth out. Look at that gap. Handsome, ain't it?" },
            { "secret", "Turns out nobody knew. Nobody. Slept like a baby." },
            { "law", "That Pinkerton's gone on the morning train. Good riddance." }
        };

        private static readonly Dictionary<string, string> BadNews = new Dictionary<string, string>
        {
            { "debt", "{who} took the mule. For the debt. Walked it right off my land." },
            { "fever", "We buried {who}. That's all there is to say about it." },
            { "letter", "Letter came. Black edge. {who} passed in the winter." },
            { "drought", "Sold the cow. Nothing else left to sell." },
            { "son", "{who} came home missing a leg. Smiles anyway. I can't." },
            { "courting", "{who}'s promised to somebody else. Heard it at the store." },
            { "feud", "Sheriff came about the fence. {who} lied to his face, and won." },
            { "bank", "Bank has the house. Strangers in my kitchen." },
            { "drink", "I'm back at it. Don't. I know." },
            { "savings", "Ticket money's gone. Every cent of it." },
            { "tooth", "Barber broke it. Took him three goes. Look at my jaw." },
            { "secret", "They know. All of them. You do too, don't you?" },
            { "law", "My old name's on a poster by the church." }
        };

        // ---- the other one's answer -------------------------------------------------------------------------

        private static readonly string[] KindToTrouble = { "Lord. I'm sorry, {a}. If there's anything at all.", "I'll say a prayer for it. For whatever that's worth." };
        private static readonly string[] ColdToTrouble = { "Everybody's got troubles.", "Well. Can't help you there." };
        private static readonly string[] SharpToTrouble = { "No call to bite my head off.", "Keep your troubles to yourself, then." };
        private static readonly string[] SnapBack = { "Then don't ask.", "Off with you, then." };

        private static readonly string[] KindToGood = { "That's the best thing I've heard all week, {a}.", "Ha! Good. Good for you. You've earned it." };
        private static readonly string[] ColdToGood = { "Good for you.", "Mm. Well. That's something." };
        private static readonly string[] SharpToGood = { "Some folks get all the luck.", "Must be nice." };

        private static readonly string[] KindToBad = { "I'm so sorry, {a}. Truly.", "Come by tonight. You shouldn't sit alone with that." };
        private static readonly string[] ColdToBad = { "That's hard. That's how it goes.", "Well. Life's like that." };
        private static readonly string[] SharpToBad = { "Told you how it'd go.", "Can't say I'm surprised." };

        // ---- a quarrel, again ---------------------------------------------------------------------------------

        private static readonly string[] QuarrelAgain = { "Still sore, {b}? I am.", "You've a nerve, standing there like nothing happened.", "Thought about what I said, {b}?" };
        private static readonly string[] MakeUp = { "I was wrong, {a}. There. I said it.", "Let it go, {a}. It ain't worth what it's costing us." };
        private static readonly string[] Accept = { "...Well. All right, then.", "Huh. All right. Buy me a drink and we'll call it square." };
        private static readonly string[] Refuse = { "Wrong don't begin to cover it.", "Sorry don't mend a thing." };
        private static readonly string[] Stand = { "Step aside, {a}, or I'll put you aside.", "One more word, {a}. Just the one." };
        private static readonly string[] Dare = { "Not here. Not today.", "Another time, {b}. Count on it." };

        // ---- sweethearts ----------------------------------------------------------------------------------------

        private static readonly string[] SweetHello = { "There you are, {b}.", "Afternoon, {b}. You look well.", "I was hoping I'd pass you." };
        private static readonly string[] SweetBack = { "Were you, now?", "I took the long way round. Don't ask why.", "You'll make me blush in the street." };

        /// How the other one answers: kind, cold or sharp, from their temper and whether they're at the worst of
        /// their own trouble. The same person answers the same way (it's who they are).
        public static string Manner(Someone b, bool bAtWorst)
        {
            if (bAtWorst || b.Temper == "Hothead" || b.Temper == "Proud")
                return "sharp";
            if (b.Temper == "Cold" || b.Temper == "Greedy" || b.Temper == "Drifter")
                return "cold";
            if (b.Temper == "Loyal" || b.Temper == "Coward")
                return "kind";
            return Voice.Chance(b.Id + "|manner", 55) ? "kind" : "cold";
        }

        /// The exchange for this reason ("x:snap", "x:goodnews", "x:badnews", "x:quarrel", "x:sweet"): A is the one whose
        /// reason it is, B the other. aName and bName are what they go by in the lines. Null if it can't be made.
        public static Scene Make(string kind, Someone a, Someone b, string aName, string bName, long day, bool bAtWorst, SceneTie tie, string seed)
        {
            string worry;
            int stage;
            bool good;
            InnerLife.WorryNow(a.P, day, out worry, out stage, out good);
            string manner = Manner(b, bAtWorst);
            System.Func<string, string> fill = line => InnerLife.WorryFill(a.P, day, line).Replace("{a}", aName).Replace("{b}", bName);
            Scene s = new Scene();
            switch (kind)
            {
                case "x:snap":
                    if (worry == null || !Snap.ContainsKey(worry))
                        return null;
                    s.Add(1, fill(Voice.Pick(seed + "h", Hello)), "familiar", "face").Add(0, fill(Snap[worry]), "replybad");
                    Answer(s, manner, fill, seed, KindToTrouble, ColdToTrouble, SharpToTrouble);
                    if (manner == "sharp")
                        s.Add(0, fill(Voice.Pick(seed + "sb", SnapBack)), "angry", "walkaway");
                    s.Kind = manner == "kind" ? "comfort" : manner == "cold" ? "brushoff" : "snap";
                    s.Summary = aName + " snapped at " + bName;
                    return s;
                case "x:goodnews":
                case "x:badnews":
                    bool glad = kind == "x:goodnews";
                    Dictionary<string, string> news = glad ? GoodNews : BadNews;
                    if (worry == null || !news.ContainsKey(worry))
                        return null;
                    s.Add(0, fill(news[worry]), glad ? "cheer" : "chatbad", "face");
                    if (glad)
                        Answer(s, manner, fill, seed, KindToGood, ColdToGood, SharpToGood);
                    else
                        Answer(s, manner, fill, seed, KindToBad, ColdToBad, SharpToBad);
                    s.Kind = manner == "kind" ? (glad ? "cheer" : "comfort") : manner == "cold" ? "brushoff" : (glad ? "envy" : "snap");
                    s.Summary = aName + " told " + bName + " the news";
                    return s;
                case "x:quarrel":
                    s.Add(0, fill(Voice.Pick(seed + "q", QuarrelAgain)), "angry", "face");
                    bool sorry = manner == "kind" || (manner == "cold" && tie != null && tie.Stage >= 2 && Voice.Chance(seed + "mk", 50));
                    if (sorry)
                    {
                        s.Add(1, fill(Voice.Pick(seed + "m", MakeUp)), "defuse");
                        bool takes = a.Temper != "Hothead" && a.Temper != "Proud" || Voice.Chance(seed + "ac", 30);
                        s.Add(0, fill(Voice.Pick(seed + "a", takes ? Accept : Refuse)), takes ? "relieved" : "angry", takes ? null : "walkaway");
                        s.Kind = takes ? "makeup" : "argue";
                    }
                    else
                    {
                        s.Add(1, fill(Voice.Pick(seed + "st", Stand)), "threat").Add(0, fill(Voice.Pick(seed + "d", Dare)), "angry", "walkaway");
                        s.Kind = "argue";
                    }
                    s.Summary = aName + " and " + bName + " at it again";
                    return s;
                case "x:sweet":
                    s.Add(0, fill(Voice.Pick(seed + "sw", SweetHello)), "flatter", "face").Add(1, fill(Voice.Pick(seed + "sbk", SweetBack)), "cheer");
                    s.Kind = "sweet";
                    s.Summary = aName + " and " + bName + ", sweet on each other";
                    return s;
            }
            return null;
        }

        private static void Answer(Scene s, string manner, System.Func<string, string> fill, string seed, string[] kind, string[] cold, string[] sharp)
        {
            string[] pool = manner == "kind" ? kind : manner == "cold" ? cold : sharp;
            s.Add(1, fill(Voice.Pick(seed + "ans", pool)), manner == "kind" ? "sorry" : manner == "cold" ? "mild" : "tut", manner == "cold" ? "walkaway" : null);
        }

        /// Every line, with the worry slots as they come, for the tests.
        public static IEnumerable<string> AllLines()
        {
            return Hello.Concat(Snap.Values).Concat(GoodNews.Values).Concat(BadNews.Values).Concat(KindToTrouble).Concat(ColdToTrouble).Concat(SharpToTrouble)
                .Concat(SnapBack).Concat(KindToGood).Concat(ColdToGood).Concat(SharpToGood).Concat(KindToBad).Concat(ColdToBad).Concat(SharpToBad)
                .Concat(QuarrelAgain).Concat(MakeUp).Concat(Accept).Concat(Refuse).Concat(Stand).Concat(Dare).Concat(SweetHello).Concat(SweetBack);
        }

        /// The worry kinds every reason needs lines for, for the tests.
        public static IEnumerable<string> WorryKinds()
        {
            return Snap.Keys;
        }
    }
}
