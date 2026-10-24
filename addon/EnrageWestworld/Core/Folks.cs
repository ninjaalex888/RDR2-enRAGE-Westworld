// Private thoughts of townsfolk enRAGE has met (acquaintances), for the ticker.
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class Folks
    {
        public static string Thought(Folk f, PersonState st, PlayerRef player, string seed, bool romance)
        {
            PlayerRef you = new PlayerRef { FullName = player.FullName, Scarred = player.Scarred, KnownByName = st.KnowsName };
            Person shell = new Person { Id = f.Key, Name = f.Name, Woman = f.Woman };
            string who = Voice.YouPrivate(shell, you, false);
            if (romance && st.Heart >= 2)
                return Voice.Pick(seed, "There's " + who + " again. My heart's going like a rabbit.", "Don't look at him. Don't look at him. ...Looked.", "I hope he stops to talk. I hope he doesn't. I don't know what I hope.");
            if (romance && st.Heart < 0)
                return Voice.Pick(seed, "Of all the people to walk past.", "I hope he trips.");
            if (f.FavourNotes.Count > 0)
                return Voice.Pick(seed, Grammar.UpperFirst(who) + " did me a kindness once. I ain't forgot.", "There's " + who + ". Decent, for all the talk about him.");
            string saw = f.Witnessed.LastOrDefault();
            if (saw != null)
            {
                string w = Townsfolk.Witness(f, saw, seed);
                if (w != null)
                    return Voice.ToPrivate(w, shell, you);
            }
            if (f.Grudge >= 40)
                return Voice.Pick(seed, "Him again. Can't stand the sight of " + (st.KnowsName ? player.Surname : "him") + ".", "One day somebody'll give him what he's owed.");
            if (f.Fear >= 40)
                return Voice.Pick(seed, "Keep walking. Keep walking.", "Please don't stop here. Please don't stop here.");
            if (f.Like >= 25)
                return Voice.Pick(seed, "Good to see " + who + " back in town.", "Always tips his hat, that one.");
            if (f.Met)
                return Voice.Pick(seed, "I've talked to " + who + " before. Can't decide what I make of him.", "Him. We've spoken.");
            return null;
        }
    }
}
