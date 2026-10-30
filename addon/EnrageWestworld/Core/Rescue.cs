// Despair's other side (roadmap 3, Lex 2026-10-27: despair shouldn't only be an ending; someone who sees it coming, a friend
// or a stranger moved to pity, steps in with a few words and they come away, and the one saved remembers who stopped them;
// night 2026-10-29, item 6d; with [Mind] MindLive and [Mind]
// MindRescue). Whoever is near when the last words begin takes it in by their own nature (the appraisal: someone about to
// do themselves harm, nobody's doing, all need): a friend moved to pity, or a stranger moved to it more than a little,
// walks over and says it to their face. Whether they're heeded is the despairing one's: a friend's word more often than a
// stranger's. Pure; plain C#, no game. All lines are original to this add-on.
using System;

namespace EnrageWestworld.Core
{
    public static class Rescue
    {
        /// A friend's word is heeded this often, a stranger's this often, in percent (a seeded roll).
        public const int FriendHeeded = 80, StrangerHeeded = 55;

        /// Someone near the despairing, taking it in: about to do themselves harm, nobody's doing, all need.
        public static AppraisalEvent SeenBy(bool woman, int warmTo, string place, bool neighbours, int witnesses, string id, string name)
        {
            return new AppraisalEvent
            {
                Kind = "despair", Role = warmTo > 0 ? "friend" : "onlooker", Doer = "nobody", WarmTarget = warmTo, Place = place ?? "street", Woman = woman, WomenPresent = true,
                Company = neighbours ? "neighbours" : "strangers", Witnesses = Math.Max(0, witnesses), TargetId = id, TargetName = name
            };
        }

        /// Whether it moves them to go to them: pity with the urge to help; for a stranger more than a little of it.
        public static bool Moves(Feeling f, int warmTo)
        {
            if (f == null || f.Kind != "pity" || f.Urge != "help" || warmTo < 0)
                return false;
            return f.Intensity >= (warmTo > 0 ? Appraisal.Felt : Appraisal.Moderate);
        }

        /// Whether the despairing one heeds them (seeded: the same two, the same answer).
        public static bool Heeded(bool friend, string seed)
        {
            return Voice.Chance(seed + "|heeded", friend ? FriendHeeded : StrangerHeeded);
        }

        /// What the one who comes says, by whether they're a close friend, their sex, and what they call the other (a first
        /// name, "ma'am", "friend", or null for nothing).
        public static string Plea(bool friend, bool saverWoman, string name, string seed)
        {
            string n = string.IsNullOrEmpty(name) ? "friend" : name;
            string to = string.IsNullOrEmpty(name) ? "" : ", " + name;
            if (friend)
                return saverWoman
                    ? Voice.Pick(seed + "|plea", n + ". " + n + ", look at me. No. Come away with me now.", "Oh, " + n + ". Not this. Give me your hand.", n + ", dear. Whatever it is, we'll bear it. Come home.", "Look at me, " + n + ". I'm here. I'm not going anywhere.")
                    : Voice.Pick(seed + "|plea", n + ". " + n + ", look at me. Don't. Come away with me.", "Put it out of your head, " + n + ". Come on. Walk with me.", n + "! No. Not like this. Come here.", "Easy, " + n + ". It's me. Whatever it is, it'll keep till morning.");
            return saverWoman
                ? Voice.Pick(seed + "|plea", "Here now. Here. Nothing in this world is worth that.", "Come away from there. Sit with me a while.", "No. No, you mustn't. Take my hand.", "Whatever's happened, there's somebody who'd grieve. Come away.")
                : Voice.Pick(seed + "|plea", "Here now" + to + ". Nothing's worth that. Walk with me a while.", "Easy. Easy now. Come and sit down a spell.", "Look at me" + to + ". Just look at me. I'm not going anywhere.", "Whatever it is" + to + ", it'll look different in the morning. Come on away.");
        }

        /// Their answer: heeded, they come away; not, they won't.
        public static string Answer(bool heeded, string seed)
        {
            return heeded
                ? Voice.Pick(seed + "|answer", "...I don't know what came over me.", "...All right. All right. Just for a while.", "I couldn't stand it. I couldn't stand any more of it.", "...You won't tell anyone?", "...I'm so tired. So tired.")
                : Voice.Pick(seed + "|answer", "Let me alone. Please. Just let me alone.", "You don't understand. Nobody does.", "Go away. Go on, go.", "Don't. Don't touch me.");
        }

        /// The one who came, after: their thought.
        public static string After(bool heeded, string name, string seed)
        {
            if (name == null || name.Length == 0)
                return heeded
                    ? Voice.Pick(seed + "|after", "Got there. Thank God I got there.", "My hands are still shaking.", "Another minute and I'd have been too late.", "A stranger. And I nearly walked on by.")
                    : Voice.Pick(seed + "|after", "Wouldn't listen. Wouldn't so much as look at me.", "I tried. Lord knows I tried.", "Should have said it better. Should have said something else.");
            return heeded
                ? Voice.Pick(seed + "|after", "Got there. Thank God I got there.", "My hands are still shaking. But " + name + " is here.", "Another minute and I'd have been too late.", "I'll look in on " + name + " every day, now.")
                : Voice.Pick(seed + "|after", "Wouldn't listen. Wouldn't so much as look at me.", "I tried. Lord knows I tried.", "Couldn't reach " + name + ". Nobody could have.", "Should have said it better. Should have said something else.");
        }
    }
}
