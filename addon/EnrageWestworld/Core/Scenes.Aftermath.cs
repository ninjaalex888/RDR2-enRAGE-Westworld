// What follows a street fight (the NPC-life run, Lex 2026-10-30: "consequences that chain"): a lawman walks over to the
// one left standing and has a word, answered by the man's nature. The friend who tends the loser is Scenes.Tend. All
// lines are original to this add-on.
namespace EnrageWestworld.Core
{
    public static partial class Scenes
    {
        /// A lawman (A) has a word with the winner of a fight (B): what happened, B's answer by temper (Minds.TemperGroup:
        /// bold, fearful, cold, plain), and the warning. A walks on after.
        public static Scene LawWord(SceneCast c, string seed, string temper)
        {
            string b = N(c, 1, seed);
            Scene s = new Scene { Kind = "lawword", Summary = "the law had a word with the man left standing" };
            s.Add(0, null, null, "meet");
            s.Add(0, Voice.Pick(seed + "lq", "That's enough! What's all this?", "Break it up! Who started this?", "You there. Stand easy and don't move.",
                                "Alright, " + b + ". Let's hear it."), "angry", "face");
            string g = Minds.TemperGroup(temper);
            string answer = g == "bold" ? Voice.Pick(seed + "la", "He had it coming, deputy.", "Ask him. He ran his mouth first.", "I finished it. Didn't start it.")
                          : g == "fearful" ? Voice.Pick(seed + "la", "He started it, sir, I swear it!", "I only stood up for myself. Ask anybody.", "I didn't want any trouble. Honest.")
                          : g == "cold" ? Voice.Pick(seed + "la", "A difference of opinion. It's settled.", "Nothing that concerns the law.", "He's alive. That's more than he deserved.")
                          : Voice.Pick(seed + "la", "Just a disagreement, deputy. It's over now.", "Got out of hand, is all.", "We had words. Then we had more than words.");
            s.Add(1, answer, g == "fearful" ? "scared" : g == "bold" ? "angry" : "mild");
            s.Add(0, Voice.Pick(seed + "lw", "Any more of it and you'll cool your heels in a cell.", "I see you swing again, you'll answer to the judge.",
                                "Go on home and sleep it off.", "This is a decent town. Keep it that way."), "angry", "walkaway");
            return s;
        }
    }
}
