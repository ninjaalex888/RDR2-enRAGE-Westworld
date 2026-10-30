// The thanks a plan to repay a kindness comes to (Plans.cs): A finds B and says so, with a small something. All lines are
// original to this add-on.
namespace EnrageWestworld.Core
{
    public static partial class Scenes
    {
        /// A thanks B for a kindness done them lately.
        public static Scene Thanks(SceneCast c, string seed)
        {
            string b = N(c, 1, seed);
            Scene s = new Scene { Kind = "thanks", Summary = "someone came to say thank you" };
            s.Add(0, Voice.Pick(seed + "ta", b + ". I've been meaning to find you.", "There you are, " + b + ". Been looking for you.", b + "! A minute, if you would."), "greet", "face");
            s.Add(1, Voice.Pick(seed + "tb", "Oh? What for?", "Something wrong?", "I'm listening."), "mild");
            s.Add(0, Voice.Pick(seed + "tc", "For the other day. You didn't have to, and you did.", "You stood by me when nobody else would.", "What you did for me. I won't forget it."), "thanks");
            s.Add(0, Voice.Pick(seed + "td", "Here. It isn't much. Take it.", "Supper's on me, next you're by.", "Any time you want a hand with something, ask."), "mild");
            s.Add(1, Voice.Pick(seed + "te", "Glad I was there, is all.", "...Well. Thank you kindly.", "Wasn't nothing. But I'm glad of it."), "thanks");
            return s;
        }
    }
}
