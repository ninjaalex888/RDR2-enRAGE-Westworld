// The game's own recorded lines for flirting, by what she answers and why (2026-10-26, Lex: not much voice in the
// flirting, "add in a lot more voice dialogue options the NPCs can choose from"). The game can't say new words, so the
// variety is in the line names: several for each answer and reason, each fitting the written reply in spirit, tried in
// a different order for each person and day; the first one her voice has plays (Speech.WhenFirst) and the rest are the
// fallbacks. Picked by what the lines say in the one voice transcribed locally (never quoted here). FLATTER_RESPONSE and
// PERV_RESPONSE aren't in the decoded bank list but are carried and played in game (2026-10-25). The player's lines
// start with the game's second-greeting ones (the GREET_SECOND_* names, which no townsfolk voice lists, so they are
// probably the player's own); whether Arthur's voice has them shows in the log (FLIRT voice:).
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class FlirtVoices
    {
        // a yes, by what drew her
        private static readonly string[] YesSaloon = { "PERV_RESPONSE", "GREET_FLIRTY", "FLATTER_RESPONSE", "GENERIC_YES" };
        private static readonly string[] YesTipsy = { "PERV_RESPONSE", "GREET_FLIRTY", "GENERIC_CHEER" };
        private static readonly string[] YesStolen = { "FLATTER_RESPONSE", "GREET_FLIRTY", "GENERIC_THANKS" };
        private static readonly string[] YesMoney = { "FLATTER_RESPONSE", "GREET_FLIRTY", "CHAT_FLATTER" };
        private static readonly string[] YesBold = { "PERV_RESPONSE", "FLATTER_RESPONSE", "GREET_FLIRTY" };   // danger, restless
        private static readonly string[] YesWarm = { "CHAT_FLATTER", "FLATTER_RESPONSE", "GENERIC_THANKS" };   // decent, lonely
        private static readonly string[] YesSweet = { "PERV_RESPONSE", "GREET_FLIRTY", "CHAT_FLATTER", "FLATTER_RESPONSE" };   // smitten, or fond of him already
        private static readonly string[] YesShy = { "GENERIC_THANKS", "FLATTER_RESPONSE", "VOCAL_FX_RESPOND" };
        private static readonly string[] YesAny = { "FLATTER_RESPONSE", "GREET_FLIRTY", "CHAT_FLATTER", "PERV_RESPONSE", "GENERIC_THANKS" };
        private static readonly string[] YesTail = { "GREET_FLIRTY", "FLATTER_RESPONSE", "GENERIC_THANKS", "GREET_GENERAL_FAMILIAR", "VOCAL_FX_RESPOND" };

        // a maybe
        private static readonly string[] MaybeShy = { "GENERIC_THANKS", "NO_IDEA", "VOCAL_FX_SURPRISED" };
        private static readonly string[] MaybeTroubled = { "GENERIC_THANKS", "GOING_BADLY", "CONVO_TAIL_OFF" };
        private static readonly string[] MaybeAny = { "RESPONSE_GENERIC", "FLATTER_RESPONSE", "NO_IDEA", "EVENT_COMMENT_RESPONSE", "VOCAL_FX_RESPOND" };
        private static readonly string[] MaybeTail = { "RESPONSE_GENERIC", "VOCAL_FX_RESPOND", "GENERIC_THANKS" };

        // a no, by its reason
        private static readonly string[] NoMarried = { "GENERIC_NO", "SHAME_ON_YOU", "DEFUSE_RESPONSE" };
        private static readonly string[] NoElsewhere = { "GENERIC_NO", "DISMISSIVE_REACT", "CONVO_TAIL_OFF" };
        private static readonly string[] NoHate = { "WHATS_YOUR_PROBLEM", "INSULT_RESPONSE", "LEAVE_ME", "SHAME_ON_YOU" };   // hate, killer, badrep
        private static readonly string[] NoFear = { "GET_AWAY_FROM_ME", "LEAVE_ME", "GENERIC_FRIGHTENED_MED", "GENERIC_FRIGHTENED_HIGH" };
        // too many at once: a tease, not a cold shoulder (Lex, 2026-10-30: IGNORING_YOU sounded like a no to a willing woman)
        private static readonly string[] NoSpam = { "TEASE_RESPONSE", "CUT_THAT_OUT", "STOP_THAT" };
        private static readonly string[] NoScorned = { "IGNORING_YOU", "DISMISSIVE_REACT", "TEASE_RESPONSE" };
        private static readonly string[] NoBeneath = { "GENERIC_MOCK", "DEFUSE_RESPONSE", "DISMISSIVE_REACT" };   // beneath, poor
        private static readonly string[] NoPious = { "SHAME_ON_YOU", "INSULT_RESPONSE", "GENERIC_NO" };
        private static readonly string[] NoBusy = { "DEFUSE_RESPONSE", "GOODBYE_START", "GOING_BADLY", "GENERIC_NO" };   // troubled, busy, sore
        private static readonly string[] NoWary = { "GENERIC_NO", "GOODBYE_START", "DEFUSE_RESPONSE" };
        private static readonly string[] NoShy = { "GENERIC_NO", "NO_IDEA", "VOCAL_FX_SURPRISED" };
        private static readonly string[] NoManNotDrawn = { "WHATS_YOUR_PROBLEM", "INSULT_RESPONSE", "DISMISSIVE_REACT" };
        private static readonly string[] NoAny = { "DEFUSE_RESPONSE", "DISMISSIVE_REACT", "GENERIC_NO", "IGNORING_YOU", "TEASE_RESPONSE" };
        private static readonly string[] NoTail = { "GENERIC_NO", "DISMISSIVE_REACT", "VOCAL_FX_DISAPPROVE" };

        // the night: her yes to "somewhere quieter", by who she is
        private static readonly string[] NightSaloon = { "PERV_RESPONSE", "GENERIC_YES", "GREET_FLIRTY" };
        private static readonly string[] NightTaken = { "GENERIC_YES", "FLATTER_RESPONSE", "VOCAL_FX_RESPOND" };
        private static readonly string[] NightSingle = { "GENERIC_YES", "PERV_RESPONSE", "FLATTER_RESPONSE", "GREET_FLIRTY" };
        private static readonly string[] NightTail = { "GENERIC_YES", "GREET_FLIRTY", "RESPONSE_GENERIC" };

        // the player, sweet-talking
        private static readonly string[] YouToWoman = { "GREET_SECOND_COMPLIMENT_CONV", "GREET_SECOND_FEMALE_CONV", "CHAT_FLATTER", "GREET_FLIRTY" };
        private static readonly string[] YouToMan = { "CHAT_FLATTER", "GREET_FLIRTY", "GREET_GENERAL_FAMILIAR" };
        private static readonly string[] YouTail = { "GREET_FLIRTY", "GREET_GENERAL_FAMILIAR" };

        /// Her answer to the player's flirting: answer yes|maybe|no, why its reason (Interest), heart how fond of him she
        /// already is. p may be null (a stranger); seed varies the order by person and day.
        public static string[] Reply(Person p, string answer, string why, int heart, string seed)
        {
            bool shy = p != null && p.Sociability == "Introvert";
            string[] pool, tail;
            if (answer == "yes")
            {
                tail = YesTail;
                pool = why == "saloon" ? YesSaloon : why == "tipsy" ? YesTipsy : why == "stolen" ? YesStolen : why == "money" ? YesMoney
                     : why == "danger" || why == "restless" ? YesBold : why == "decent" || why == "lonely" ? YesWarm
                     : why == "smitten" || heart >= 3 ? YesSweet : shy ? YesShy : YesAny;
            }
            else if (answer == "maybe")
            {
                tail = MaybeTail;
                pool = why == "shy" ? MaybeShy : why == "troubled" ? MaybeTroubled : MaybeAny;
            }
            else
            {
                tail = NoTail;
                switch (why)
                {
                    case "married": pool = NoMarried; break;
                    case "elsewhere": pool = NoElsewhere; break;
                    case "hate": case "killer": case "badrep": pool = NoHate; break;
                    case "fear": case "frightened": pool = NoFear; break;
                    case "spam": pool = NoSpam; break;
                    case "scorned": pool = NoScorned; break;
                    case "beneath": case "poor": pool = NoBeneath; break;
                    case "pious": pool = NoPious; break;
                    case "troubled": case "busy": case "sore": pool = NoBusy; break;
                    case "wary": pool = NoWary; break;
                    case "shy": pool = NoShy; break;
                    case "notdrawn": pool = p != null && !p.Woman ? NoManNotDrawn : NoAny; break;
                    default: pool = NoAny; break;
                }
            }
            return Order(pool, tail, seed + "|fv");
        }

        /// Her yes when he asks her somewhere quieter (Trysts.Kind: saloon, taken, single).
        public static string[] Night(string kind, string seed)
        {
            return Order(kind == "saloon" ? NightSaloon : kind == "taken" ? NightTaken : NightSingle, NightTail, seed + "|fn");
        }

        /// The player's own line as he flirts or asks.
        public static string[] You(bool toWoman, string seed)
        {
            return Order(toWoman ? YouToWoman : YouToMan, YouTail, seed + "|fy");
        }

        /// The pool from a place picked by the seed, round to its start, then the tail's lines not already in it.
        public static string[] Order(string[] pool, string[] tail, string seed)
        {
            List<string> o = new List<string>(pool.Length + tail.Length);
            int start = pool.Length == 0 ? 0 : Voice.Hash(seed) % pool.Length;
            for (int i = 0; i < pool.Length; i++)
                o.Add(pool[(start + i) % pool.Length]);
            foreach (string l in tail)
                if (!o.Contains(l))
                    o.Add(l);
            return o.ToArray();
        }

        /// Every set, for the tests: (what it's for, the lines).
        public static IEnumerable<KeyValuePair<string, string[]>> AllSets()
        {
            Person woman = new Person { Woman = true }, man = new Person { Woman = false }, shy = new Person { Woman = true, Sociability = "Introvert" };
            foreach (string why in new[] { "saloon", "tipsy", "stolen", "money", "danger", "restless", "decent", "lonely", "smitten", "met", null })
                foreach (Person p in new[] { woman, shy, null })
                    yield return new KeyValuePair<string, string[]>("yes:" + why, Reply(p, "yes", why, 0, "s"));
            yield return new KeyValuePair<string, string[]>("yes:fond", Reply(woman, "yes", "met", 3, "s"));
            foreach (string why in new[] { "shy", "troubled", null })
                yield return new KeyValuePair<string, string[]>("maybe:" + why, Reply(woman, "maybe", why, 0, "s"));
            foreach (string why in new[] { "married", "elsewhere", "hate", "killer", "badrep", "fear", "frightened", "spam", "scorned", "beneath", "poor", "pious", "troubled", "busy", "sore", "wary", "shy", "notdrawn", "cold", null })
                foreach (Person p in new[] { woman, man })
                    yield return new KeyValuePair<string, string[]>("no:" + why, Reply(p, "no", why, 0, "s"));
            foreach (string k in new[] { "saloon", "taken", "single" })
                yield return new KeyValuePair<string, string[]>("night:" + k, Night(k, "s"));
            yield return new KeyValuePair<string, string[]>("you:woman", You(true, "s"));
            yield return new KeyValuePair<string, string[]>("you:man", You(false, "s"));
        }
    }
}
