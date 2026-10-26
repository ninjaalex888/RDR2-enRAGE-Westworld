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
            // what they think of him (Regard): the same judgement their answer to his flirting comes from
            switch (Regard.OfFolk(f, st, romance))
            {
                case "smitten":
                    return Voice.Pick(seed, "There's " + who + " again. My heart's going like a rabbit.", "Don't look at him. Don't look at him. ...Looked.", "I hope he stops to talk. I hope he doesn't. I don't know what I hope.");
                case "scorned":
                    return Voice.Pick(seed, "Of all the people to walk past.", "I hope he trips.");
                case "grateful":
                    return Voice.Pick(seed, Grammar.UpperFirst(who) + " did me a kindness once. I ain't forgot.", "There's " + who + ". Decent, for all the talk about him.");
                case "witnessed":
                    return Voice.ToPrivate(Townsfolk.Witness(f, f.Witnessed.Last(), seed), shell, you);
                case "hostile":
                    return Voice.Pick(seed, "Him again. Can't stand the sight of " + (st.KnowsName ? player.Surname : "him") + ".", "One day somebody'll give him what he's owed.");
                case "afraid":
                    return Voice.Pick(seed, "Keep walking. Keep walking.", "Please don't stop here. Please don't stop here.");
                case "fond":
                    return Voice.Pick(seed, "Good to see " + who + " back in town.", "Always tips his hat, that one.");
                case "met":
                    return Voice.Pick(seed, "I've talked to " + who + " before. Can't decide what I make of him.", "Him. We've spoken.");
                default:
                    return null;
            }
        }
    }
}
