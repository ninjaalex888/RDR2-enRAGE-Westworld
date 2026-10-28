// The words for memory v1 (night 2026-10-28, item 4; behind [Mind] MindLive): what someone told of a thing thinks of
// it ({from}: the teller's first name; {a} and {b}: the two in it as the teller had them; {you}: the player as this
// person sees him), and what the spot where something happened brings back when they stand on it. Lex's brief: short,
// one thing noticed, words of 1899 America. All lines are original to this add-on.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static partial class Minds
    {
        /// Scene kinds where two people talk, so one may tell the other something (Engine.RememberScene hears them end).
        public static bool TalkKind(string kind)
        {
            return kind == "smalltalk" || kind == "gossip" || kind == "comfort" || kind == "cheer" || kind == "round" || kind == "hosts" || kind == "haggle";
        }

        internal static readonly Dictionary<string, string[]> ToldPools = new Dictionary<string, string[]>
        {
            { "told|blows", new[] {
                "{from} says {a} and {b} came to fists over something. Wish I'd been there to see it.",
                "Word from {from}: trouble between {a} and {b}. Wasn't there. Can picture it.",
                "{from} tells it one way. {a} will tell it another.",
                "{a} and {b}, {from} says. Those two were bound to, sooner or later.",
                "Heard it from {from}, with the hands going, the way {from} tells anything.",
                "Trouble between {a} and {b}, and me at home with the lamp. Always the way." } },
            { "told|quarrel", new[] {
                "{from} says there were words between {a} and {b}. Over what, {from} didn't say.",
                "Words between {a} and {b}, so {from} says. There's more to that story.",
                "{from} heard every word of it, to hear {from} tell it.",
                "{a} and {b} at odds. {from} couldn't wait to pass that on.",
                "So {a} and {b} aren't speaking. {from} had it from somebody who was there.",
                "Hard words, {from} says. Hard words are cheap in this town." } },
            { "told|slight", new[] {
                "{from} says {a} passed {b} by in the street without a word. Small town. Long memory.",
                "{a} wouldn't so much as nod to {b}, {from} says. Sounds like {a}.",
                "{from} made a long story of it. {a}, {b}, and a look that could curdle milk.",
                "Heard from {from} that {a} walked right past {b}. Could be nothing. Won't stay nothing.",
                "{a} snubbed {b}, says {from}. {from} snubs people every day and never notices.",
                "Small thing, the way {from} told it. Small things are what this town lives on." } },
            { "told|theft", new[] {
                "{from} says {b} had a purse lifted. Hand on my own pocket since.",
                "Light fingers in town, {from} says. Count the change twice.",
                "{a} caught at it, {from} says. Always thought {a} looked hungry around other people's things.",
                "Word from {from}: somebody's been thieving. Door bolted tonight.",
                "{from} says {b} lost a week's wages to a thief. Could've been any of us.",
                "A thief about, says {from}. {from} says a lot. This one I believe." } },
            { "told|drunk", new[] {
                "{from} says {a} was roaring drunk and bothering folk. Not the first time.",
                "{a} in his cups again, {from} says. Somebody ought to see him home one night.",
                "Drunk and loud, {from} says. {a} is a quiet man sober. Drink finds the other one.",
                "{from} tells it funny. Wasn't funny for {b}, I'd guess.",
                "Heard from {from} that {a} made a show of himself. The saloon's had worse.",
                "{a} drunk again. {from} told it with a long face. {from} drinks too." } },
            { "told|warmth", new[] {
                "{from} says {a} did {b} a good turn. There's decent folk yet.",
                "{a} and {b}, {from} says, thick as thieves lately. Good for them.",
                "A kindness in the street, {from} says. Those don't get told on as fast as the other kind.",
                "{from} says {a} stood {b} a drink and the whole room cheered. Wish I'd had one.",
                "Heard from {from} that {a} helped {b} up out of the mud. Would {from} have? Would I?",
                "{a} and {b} made it up, {from} says. Took them long enough." } },
            { "told|deed:violence", new[] {
                "{from} says {you} drew on a man in the street. {from} was white telling it.",
                "Trouble in town, {from} says, and a stranger at the bottom of it. Keep the children in.",
                "{from} saw it, or says so. A man down and {you} standing over him.",
                "Word from {from} is {you} is dangerous. Word's enough for me. Keep clear.",
                "{from} told it twice and it got worse the second time. The stranger, a gun, a man down.",
                "Bad business, {from} says. In our street. Won't sleep sound tonight." } },
            { "told|deed:theft", new[] {
                "{from} says {you} robbed somebody in broad day. Brazen.",
                "Somebody robbed, {from} says, and a stranger's hand in it. Lock the till.",
                "{from} says the stranger took what wasn't his and walked off whistling. Can picture it.",
                "Heard from {from}: {you} is a thief, whatever else. Count the spoons.",
                "Thieving, {from} says. {from} would know; {from} counts everybody's money.",
                "{you} took a purse off somebody, {from} says. Somebody could've been me." } },
            { "told|deed:kindness", new[] {
                "{from} says {you} stepped in for a stranger. Not what I'd have guessed, from the look of him.",
                "A kindness from the stranger, {from} says. People surprise you. Some of them.",
                "{from} can't say a bad word about {you} now. {from} said plenty last week.",
                "Heard from {from} the stranger did a decent thing. Hold the judgment a day or two.",
                "{you} helped a man up, {from} says. Rough hands, {from} says, but he stopped.",
                "{from} tells it like a sermon. Maybe it is one." } },
        };

        internal static readonly Dictionary<string, string[]> HerePools = new Dictionary<string, string[]>
        {
            { "here|blows", new[] { "Right here. This is where it happened.", "Same spot. Still see where they went down.", "Walk past this spot every day now and think of it.", "Here. The dust hasn't forgotten either." } },
            { "here|quarrel", new[] { "This is where the words flew.", "Right here they had it out. Walls remember.", "Stood just about here when it started.", "Can't pass this spot without hearing it again." } },
            { "here|theft", new[] { "Here's where the purse went. Hand to the pocket, every time.", "This spot. Watch the hands near you.", "Stood right here and never felt a thing. Never will again.", "Here. Somebody's quick fingers. Keep the coat buttoned." } },
            { "here|warmth", new[] { "Right here. A good turn, this spot.", "This is where it happened. The good kind.", "Pass this spot and feel a little better. Odd.", "Here's where {who} did right by me." } },
            { "here|death", new[] { "Here. This is where {who} went down.", "This spot. Can't stand on it long.", "Right here. The ground doesn't look any different. It should.", "Walk around this spot. Can't say why. Can." } },
            { "here|deed:violence", new[] { "Here's where the stranger did it.", "This spot. Still see it.", "Right here. Keep walking.", "Can't pass this spot since." } },
            { "here|deed:theft", new[] { "This is where the stranger took it.", "Right here. Pocket's lighter for standing here.", "Here. Hand on the purse.", "This spot. Trust nobody on it." } },
            { "here|drunk", new[] { "Here's where he made his show.", "This spot. Still smells of it, I swear.", "Right here. The whole street watched.", "Here. A man was less than himself here." } },
            { "here|slight", new[] { "Right here. The look that went through me.", "This spot. Walked past me here like I was a post.", "Here. Still feel it, standing here.", "Here's where the cut came." } },
        };

        /// The line for a memory that came by telling: by its group, stepping through its pool as it's thought again.
        public static string ToldLine(MindMemory m, Person p, string seed, string you, string fromFirstName)
        {
            string[] pool;
            if (m == null || m.Kind == null || !ToldPools.TryGetValue("told|" + m.Kind, out pool))
                return null;
            string raw = pool[(Voice.Hash(m.Key + "|" + p.Id + "|told") + m.Times) % pool.Length];
            return Fill(raw.Replace("{from}", fromFirstName ?? "somebody"), m, p, you);
        }

        /// The line for a memory whose spot they're standing on.
        public static string HereLine(MindMemory m, Person p, string seed, string you)
        {
            string[] pool;
            if (m == null || m.Kind == null || !HerePools.TryGetValue("here|" + m.Kind, out pool))
                return null;
            string raw = pool[(Voice.Hash(m.Key + "|" + p.Id + "|here") + m.Times) % pool.Length];
            return Fill(raw, m, p, you);
        }

        /// In plain words, for the log and F7: "told by Abe: a brawl between Earl and Hollis in Valentine, 2 days ago (sure 0.56; told on once)".
        public static string DescribeMemory(MindMemory m, long now, string fromFirstName = null)
        {
            if (m == null)
                return "nothing";
            string who = m.Names != null && m.Names.Count > 0 ? string.Join(" and ", m.Names) : "somebody";
            long ago = System.Math.Max(0, now - m.At);
            string when = ago < 90 ? "just now" : ago < 1440 ? (ago / 60) + " h ago" : (ago / 1440) + " days ago";
            string how = m.From != null ? "told by " + (fromFirstName ?? m.From) : m.How;
            return how + ": " + (m.Sub ?? m.Kind) + " (" + who + ")" + (m.Town != null ? " in " + m.Town : "") + ", " + when + " (sure " + Sure(m).ToString("0.00") +
                   (m.Removes > 0 ? ", " + m.Removes + " removes" : "") + (m.Told > 0 ? ", told on " + m.Told + (m.Told == 1 ? " time" : " times") : "") + (HasSpot(m) ? ", a spot" : "") + ")";
        }

        public static IEnumerable<string> ToldAndHereLines()
        {
            foreach (var kv in ToldPools)
                foreach (string l in kv.Value)
                    yield return l;
            foreach (var kv in HerePools)
                foreach (string l in kv.Value)
                    yield return l;
        }
    }
}
