// Reads the tone of a line the add-on wrote, so the speaker can play a recorded game line that fits it.
// The game's lines never say our words; the subtitle carries the meaning and the voice carries the feeling.
// Returns a voice tag the game side maps to a set of the game's speech lines (see Speech.Set), or null
// when nothing fits well enough: silence is better than the wrong feeling.
using System.Text.RegularExpressions;

namespace EnrageWestworld.Core
{
    public static class Tone
    {
        private const RegexOptions Opt = RegexOptions.IgnoreCase | RegexOptions.CultureInvariant;

        private static readonly Regex Begging = new Regex(@"\b(don't (shoot|kill|hurt)|mercy|spare (me|us|my)|i beg|have pity|i got (kids|children|a family))\b", Opt);
        private static readonly Regex Thanking = new Regex(@"\b(thank(s| you| ye)?|obliged|grateful|bless you|much appreciated)\b", Opt);
        private static readonly Regex Threatening = new Regex(@"\b(i'll (kill|find|get|see you|gut|bury|hunt)|you'll (pay|hang|regret|swing)|watch your back|stay away from|get away from|i swear|next time i see|you're a dead man|your days are)\b", Opt);
        private static readonly Regex Cursing = new Regex(@"\b(bastard|devil|murderer|killer|coward|scum|vermin|snake|son of a|damn you|go to hell|filth|butcher)\b", Opt);
        private static readonly Regex Accusing = new Regex(@"\byou (killed|shot|murdered|robbed|took|did this|hanged|left)\b", Opt);
        private static readonly Regex Disgusted = new Regex(@"\b(grave ?robb\w*|rob(s|bing)? the dead|shame on you|no shame|(ought to|should) be ashamed|disgrace|sick in the head|no decent man)\b", Opt);
        private static readonly Regex Grieving = new Regex(@"\b(dead|died|buried|grave|funeral|widow(ed)?|lost (my|him|her|them|the)|miss (him|her|them)|mourn|rest (his|her) soul|god rest|passed on|never came home)\b", Opt);
        private static readonly Regex Parting = new Regex(@"^(good ?bye|so long|take care|be seeing you|farewell|good night|i (best|should|ought to) (be )?go|mind how you go|until next time|i'll be off)|\b(be seeing you|so long now|take care of yourself)\b", Opt);
        private static readonly Regex Greeting = new Regex(@"^(hello|howdy|hey there|hi\b|morning|evening|afternoon|good (morning|day|evening|afternoon)|well,? (well|if it isn't|look who)|mister\b|ma'am\b|sir\b)|\b(pleased to meet you|good to see you|nice to meet you)\b", Opt);
        private static readonly Regex Hearsay = new Regex(@"^(word is|they say|folks (say|are saying)|i hear|i heard|heard|people are saying|story goes|they're saying|did you hear)\b", Opt);
        private static readonly Regex BrushOff = new Regex(@"\b(not with you|leave me (be|alone)|go away|don't talk to me|nothing to (say|talk about)|none of your (business|concern)|i've got nothing for you)\b", Opt);
        private static readonly Regex Flirting = new Regex(@"\b(handsome|pretty|darling|sweetheart|honey|kiss|beautiful|charming|dance with|your eyes|sweet on|fine-looking|good-looking)\b", Opt);
        private static readonly Regex Gossiping = new Regex(@"\b(heard|they say|word is|folks (say|are saying)|did you hear|rumou?r|people are saying|i hear|story goes|they're saying)\b", Opt);
        private static readonly Regex Agreeing = new Regex(@"^(yes|yeah|aye|sure|of course|reckon so|i suppose|right you are|fair enough)\b", Opt);
        private static readonly Regex Refusing = new Regex(@"^(no[,.! ]|nope|never\b|not a chance|i don't think so)", Opt);
        private static readonly Regex Mocking = new Regex(@"\b(ha(h|ha)?!|you\? a|what a fool|look at you|pathetic|ain't you something)\b", Opt);
        private static readonly Regex GetAway = new Regex(@"\b(get away from me|keep away from me|get back from me|leave me alone|let me alone|don't come (any )?closer|come no closer|don't touch me)\b|^(get back|stay back|keep back|stay where you are)\b", Opt);
        private static readonly Regex Afraid = new Regex(@"\b(i'm (so )?(scared|frightened|afraid)|i (am|was) (so )?(scared|frightened))\b", Opt);
        private static readonly Regex Staring = new Regex(@"\b(stop (staring|looking at me)|what are you (looking|staring|gawking) at|quit (staring|gawking)|why are you (looking|staring) at me|you('re| are) staring|don't look at me)\b", Opt);
        private static readonly Regex Following = new Regex(@"\b(following me|been following|why are you following|stop following)\b", Opt);
        private static readonly Regex Heard = new Regex(@"^(what was that|did you hear (that|it)|hear that|listen!|who's there|is (someone|somebody|anyone|anybody) (there|out there))", Opt);
        private static readonly Regex TrailsOff = new Regex(@"(\.\.\.|\u2026|-|\u2013|\u2014)$", Opt);   // only at the very end: "Please... somebody" doesn't trail

        /// The voice tag for a line. hate and fear are how the speaker feels about whoever they're talking to
        /// (0-100); friendly is their nature. Order matters: the strongest feeling in the line wins.
        public static string Of(string line, int hate = 0, int fear = 0, bool friendly = false)
        {
            if (string.IsNullOrEmpty(line))
                return null;
            string s = line.Trim().Trim('"');
            if (Begging.IsMatch(s))
                return "plead";
            if (GetAway.IsMatch(s))
                return "getaway";   // before threats, which also match "get away from"
            if (Threatening.IsMatch(s))
                return fear >= 50 ? "scared" : hate >= 30 ? "threat" : "mild";
            if (Cursing.IsMatch(s))
                return "angry";
            if (Afraid.IsMatch(s))
                return "scared";
            if (Staring.IsMatch(s))
                return "staring";
            if (Following.IsMatch(s))
                return "following";
            if (Hearsay.IsMatch(s))
                return "news";   // passing on what others say, even when it's about a killing
            if (Heard.IsMatch(s))
                return s.IndexOf("there", System.StringComparison.OrdinalIgnoreCase) >= 0 ? "unseen" : "heard";   // "who's there?" calls out to someone
            if (Accusing.IsMatch(s))
                return hate >= 30 ? "shame" : "stare";
            if (BrushOff.IsMatch(s))
                return "no";
            if (Disgusted.IsMatch(s))
                return "shame";
            if (Thanking.IsMatch(s))
                return "thanks";
            if (Grieving.IsMatch(s))
                return "replybad";
            if (Parting.IsMatch(s))
                return "bye";
            if (Greeting.IsMatch(s))
                return hate >= 40 ? "stare" : "greet";
            if (Flirting.IsMatch(s))
                return "flatter";
            if (Mocking.IsMatch(s))
                return "mock";
            if (Gossiping.IsMatch(s))
                return "news";
            if (Agreeing.IsMatch(s))
                return "yes";
            if (Refusing.IsMatch(s))
                return "no";
            if (TrailsOff.IsMatch(s))
                return "trail";   // a sentence that stops partway
            if (hate >= 60)
                return "angry";
            if (hate >= 30)
                return "mild";
            if (fear >= 60)
                return "scared";
            if (friendly)
                return "chat";
            return null;
        }
    }
}
