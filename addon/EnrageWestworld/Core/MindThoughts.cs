// Thoughts from feelings and needs (the mind design, behind [Mind] MindLive; daytime run 2026-10-27). With MindLive on,
// a feeling still strongly felt (Appraisal, Mood.Lasting), then a pressing need (Needs), comes before the old bands of
// thought. The thought says what is really felt, even when the face hides it (Display): the gap between the two is what
// makes people read as human. Pools by feeling and by whom it's about: the player ({you}, as this person sees him),
// someone they have a tie with ({who}, a first name), or anyone else; a feeling about oneself (shame, guilt, pride) by
// whoever did it to them. No kin pools: nothing yet tells the mind who is kin. Grief has no pool for a stranger (it's
// felt only for someone cared for). Lex's brief: short fragments, one thing the body notices, words of 1899. Need
// lines name the rain only when it rains. All lines are original to this add-on. Plain C#, no game.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class MindThoughts
    {
        public static readonly string[] Whom = { "you", "stranger", "tie" };

        internal static readonly Dictionary<string, string[]> Pools = new Dictionary<string, string[]>
        {
            { "anger|you", new[] { "{you}. Thinks this town's his to knock about.", "Walked off like it was nothing.", "Jaw's so tight my teeth ache. Breathe. Not here." } },
            { "anger|stranger", new[] { "Don't know the name. I'll know the face.", "Hands shaking. Not from fear. Let it pass.", "Some folk need teaching. Some need it twice." } },
            { "anger|tie", new[] { "{who}. Of all the people in this town. {who}.", "I stood up for {who} once. Never again.", "Heat in my neck every time I think of {who}'s face." } },
            { "fear|you", new[] { "Don't look at {you}. Look at your boots.", "That iron on his hip. Don't give him a reason.", "Heart going like a rabbit's. Smile. Nod. Get home." } },
            { "fear|stranger", new[] { "Whatever that was, let it stay over there.", "Mouth's gone dry as chalk.", "Keep walking. Even pace. Don't look round." } },
            { "fear|tie", new[] { "{who}'s capable of anything now. I've seen it.", "Keep clear of {who}. Keep well clear.", "Won't sleep tonight for thinking of {who}." } },
            { "shame|you", new[] { "{you} saw it all. Of course he did.", "Couldn't meet his eye. Still can't.", "Ears burning. Cheeks too. Walk on." } },
            { "shame|stranger", new[] { "Everybody saw. Everybody.", "Brush off the coat. Brush off the rest of it.", "Chin up. Don't let them see it land." } },
            { "shame|tie", new[] { "{who} saw. {who} will tell it.", "Can't go in the store till {who}'s forgotten it.", "Of all people to see it. {who}." } },
            { "guilt|you", new[] { "He knows. Or he will. Same thing.", "Owe {you} something now. Don't know what.", "Did that in front of {you}. God help me." } },
            { "guilt|stranger", new[] { "Gave my word on that money. My word.", "Can't wash it off. Tried.", "Put it right. Somehow. Soon." } },
            { "guilt|tie", new[] { "{who} deserved better from me.", "Next time I see {who}, I'll make it right.", "Gave {who} my word. Broke it." } },
            { "grief|tie", new[] { "{who}. Gone. Just like that.", "{who} was just here. Just here.", "Never got round to telling {who}. Now I can't." } },
            { "pride|you", new[] { "{you} saw that. Good.", "Let him remember it.", "Stood my ground. Right under his nose, too." } },
            { "pride|stranger", new[] { "Not bad. Not bad at all.", "Walk a little taller today.", "Let them talk about that for a change." } },
            { "pride|tie", new[] { "{who} saw. Good.", "Bet {who} didn't think I had it in me.", "Put that in your pipe, {who}." } },
            { "gratitude|you", new[] { "Didn't have to do that. Did anyway.", "Owe {you} a kindness. Won't forget it.", "Strange sort. Kind, though. Wouldn't have guessed it." } },
            { "gratitude|stranger", new[] { "Still decent folk about. One, anyhow.", "Didn't expect that. Not from that quarter.", "Pass it on. That's what Mother said." } },
            { "gratitude|tie", new[] { "{who} stood by me. Won't forget it.", "Owe {who} more than thanks.", "Good friend, {who}. Better than I deserve." } },
            { "envy|you", new[] { "Fine horse. Finer than anything I'll ever sit.", "Money in his pocket and not a care.", "Some men just take what they want." } },
            { "envy|stranger", new[] { "Some folk are born lucky.", "What did they ever do to deserve it?", "Same street. Different lives." } },
            { "envy|tie", new[] { "{who} again. Always {who}.", "What's {who} got that I haven't? Luck. Just luck.", "Smile for {who}. Grit my teeth for me." } },
            { "pity|stranger", new[] { "Poor soul. Poor soul.", "Could be me, a bad winter from now.", "Somebody ought to help. Somebody." } },
            { "pity|tie", new[] { "Poor {who}. Never had much luck, and less today.", "Go see {who} later. Just to sit.", "{who} looked so small just then." } },
            { "contempt|you", new[] { "Big man with a gun. That's all he is.", "Ride on, mister. Nobody wants you here.", "Seen his kind before. Seen them hang, too." } },
            { "contempt|stranger", new[] { "Trash. Pure trash.", "Some folk aren't worth the spit.", "Look at that. A grown man." } },
            { "contempt|tie", new[] { "{who} always was a fool.", "Done with {who}. Done.", "{who}. Same as ever." } },
            { "relief|you", new[] { "{you}'s all right. Thank God.", "Thought {you} was done for.", "Legs like water. Sit a minute." } },
            { "relief|stranger", new[] { "Over. It's over.", "Close. Too close.", "Hands still shaking. Doesn't matter. It's over." } },
            { "relief|tie", new[] { "{who}'s all right. All right.", "Thought I'd lost {who}.", "Never so glad to see {who}'s ugly face." } },
            { "joy|you", new[] { "Fine day, all told.", "{you}, of all people. Well, well.", "Whistling. Who'd have thought." } },
            { "joy|stranger", new[] { "A stranger, and kind. Fancy that.", "Could kiss a mule today.", "Light on my feet all of a sudden." } },
            { "joy|tie", new[] { "Good old {who}.", "Have {who} round for supper. Soon.", "{who}. Couldn't ask better of anyone." } },
        };

        // what a pressing need sounds like (NeedState's order: hunger, rest, warmth, money, safety, company, standing,
        // meaning; the vice by its name); warmth in the rain or snow out of doors from Wet, the rest at any hour
        internal static readonly string[][] NeedPools =
        {
            new[] { "Stomach growling loud enough to hear across the street.", "Could eat a boot. Laces and all.", "Smell of frying from somewhere. Torture." },
            new[] { "Eyes like sand.", "Could sleep standing, like a horse.", "Bed. Any bed. A barn would do." },
            new[] { "Can't feel my fingers.", "Cold's got right into my bones.", "Wind goes straight through this coat." },
            new[] { "Count it again. Still short.", "Rent Saturday. Wages Saturday. Pray they meet.", "One bad week from the poorhouse." },
            new[] { "Keep something solid at my back.", "Every sound's a hammer cocking.", "Back to the wall. Eyes on the door." },
            new[] { "Nobody to talk to but the horse.", "Eating alone again.", "Wish somebody'd ask after me." },
            new[] { "They're laughing. I know they're laughing.", "Hold my head up. Hold it up.", "Nobody remembers. Everybody remembers." },
            new[] { "What's it all for. Up, work, bed, again.", "Church on Sunday. Questions on Monday.", "Same road there and back, every day God sends." },
        };
        private static readonly string[] Wet = { "Rain down my collar again.", "Boots soaked through. Feet like ice.", "Wet to the skin and no fire to dry at." };
        private static readonly string[] Drink = { "One drink. Just to steady the hands.", "Taste of whiskey already, and none had.", "Promised I'd stop. Promised myself." };
        private static readonly string[] Cards = { "Luck's due to turn. Has to be.", "One hand. Just one.", "Owe too much to stop now." };

        /// A thought for a feeling about someone (whom: you, stranger, tie), with {you} and {who} filled; `used` says a
        /// line was already thought today (the next in turn is tried). Null for none left, or no pool (grief for a stranger).
        public static string Line(string feeling, string whom, string seed, string you, string who, Func<string, bool> used = null)
        {
            string[] pool;
            if (!Pools.TryGetValue(feeling + "|" + whom, out pool) && !Pools.TryGetValue(feeling + "|stranger", out pool))
                return null;
            return Pick(pool, seed, used, you, who);
        }

        /// A thought for a pressing need (an index into NeedState; the vice by its name). weather: out of doors as it is
        /// (null under a roof or unknown), so a wet line comes only in the rain or snow.
        public static string NeedLine(int need, string vice, string seed, Func<string, bool> used = null, string weather = null)
        {
            string[] pool = need == NeedState.Vice ? (vice == "cards" ? Cards : Drink)
                : need == NeedState.Warmth && (weather == "rain" || weather == "storm" || weather == "snow") ? Wet
                : need >= 0 && need < NeedPools.Length ? NeedPools[need] : null;
            return pool == null ? null : Pick(pool, seed, used, null, null);
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
                line = line.Replace("{who}", who);
            }
            return line.Length > 0 && char.IsLower(line[0]) ? char.ToUpperInvariant(line[0]) + line.Substring(1) : line;
        }

        public static IEnumerable<string> AllLines()
        {
            foreach (var kv in Pools)
                foreach (string l in kv.Value)
                    yield return l;
            foreach (string[] p in NeedPools)
                foreach (string l in p)
                    yield return l;
            foreach (string l in Wet)
                yield return l;
            foreach (string l in Drink)
                yield return l;
            foreach (string l in Cards)
                yield return l;
        }
    }
}
