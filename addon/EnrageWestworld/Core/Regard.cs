// What someone thinks of the player, as one word: the judgement their thoughts about him come from (Folks.Thought for
// enRAGE's townsfolk) and the one their answer to his flirting comes from (Interest), so the two always agree. The
// first that holds: smitten, scorned, grateful (he did them a kindness), witnessed (they saw or heard what he did),
// hostile, afraid, fond, wary (the town's talk of what he did, never having met him), met, stranger.
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class Regard
    {
        /// One of enRAGE's townsfolk, in Folks.Thought's order (romance: hearts count).
        public static string OfFolk(Folk f, PersonState st, bool romance)
        {
            if (romance && st != null && st.Heart >= 2)
                return "smitten";
            if (romance && st != null && st.Heart < 0)
                return "scorned";
            if (f.FavourNotes.Count > 0)
                return "grateful";
            string saw = f.Witnessed.LastOrDefault();
            if (saw != null && Townsfolk.Recognised(saw))
                return "witnessed";
            if (f.Grudge >= 40)
                return "hostile";
            if (f.Fear >= 40)
                return "afraid";
            if (f.Like >= 25)
                return "fond";
            return f.Met ? "met" : "stranger";
        }

        /// One of enRAGE's people (a record): its hate and fear of him, and how warm the add-on's dealings have been.
        public static string OfPerson(Person p, PersonState st, bool romance)
        {
            if (romance && st != null && st.Heart >= 2)
                return "smitten";
            if (romance && st != null && st.Heart < 0)
                return "scorned";
            if (p.Hate >= 30 || p.Vengeful || p.Murderous)
                return "hostile";
            if (p.Fear >= 50)
                return "afraid";
            if (p.Friendly || (st != null && st.Affection >= 20))
                return "fond";
            return st != null && st.Talks > 0 ? "met" : "stranger";
        }

        /// One of the add-on's own people (a resident): hearts and warmth from the add-on's dealings, the town's talk of
        /// what he did here lately (badTalk), how often they've talked or seen him.
        public static string OfResident(PersonState st, bool romance, bool badTalk, string temper)
        {
            if (romance && st != null && st.Heart >= 2)
                return "smitten";
            if (romance && st != null && st.Heart < 0)
                return "scorned";
            if (st != null && st.Affection >= 20)
                return "fond";
            if (badTalk && (st == null || st.Talks == 0))
                return temper == "Coward" ? "afraid" : "wary";
            return st != null && (st.Talks > 0 || st.Sightings >= 2) ? "met" : "stranger";
        }
    }
}
