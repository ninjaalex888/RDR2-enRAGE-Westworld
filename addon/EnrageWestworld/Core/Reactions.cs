// How people take what is done and said to them in a street scene. The one spoken to answers with their body, by
// the tone of the line and their own nature: a hothead bristles at a threat, a coward flinches, the cold shake their
// heads, the kind look down when they're shamed. A man whose face has just gone into the bar reels. Every clip is
// from the townsfolk's silent gesture sets (Gestures.MaleSilent, Gestures.FemaleSilent), played on the upper body.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Reactions
    {
        /// The tone the Director passes for the hurt reaction after a slam (not a line's tone).
        public const string HurtTone = "!hurt";

        public static readonly string[] Kinds = { "bristle", "flinch", "dismiss", "shrug", "nod", "puzzled", "shamed", "hurt" };

        private static readonly Dictionary<string, string[]> Men = new Dictionary<string, string[]>
        {
            { "bristle", new[] { "silent_aggressive_taunt_f_001", "silent_negative_disagree_l_001", "silent_negative_disagree_r_001", "silent_angry_victimised_f_001" } },
            { "flinch", new[] { "silent_might_flee_nervous_f_001", "silent_might_flee_nervous_l_001", "silent_confused_protect_f_001" } },
            { "dismiss", new[] { "silent_negative_headshake_f_002", "silent_negative_headshake_f_003", "silent_frustrated_lookaround_f_001" } },
            { "shrug", new[] { "silent_neutral_shrug_f_001", "silent_neutral_shrug_l_001", "silent_neutral_shrug_r_001" } },
            { "nod", new[] { "silent_positive_headonod_f_001", "silent_headnod_greet_f_001", "silent_headnod_greet_f_002" } },
            { "puzzled", new[] { "silent_confused_look_f_001", "silent_confused_look_f_002", "silent_frustrated_lookup_f_001" } },
            { "shamed", new[] { "aknwoledge_timid_look_down_f_001", "aknwoledge_timid_look_down_f_002", "aknwoledge_timid_look_down_f_004" } },
            { "hurt", new[] { "silent_wince_l_004", "silent_wince_r_004" } }
        };

        private static readonly Dictionary<string, string[]> Women = new Dictionary<string, string[]>
        {
            { "bristle", new[] { "silent_negative_dismiss_f_001", "silent_negative_dismiss_l_002", "silent_negative_spit_f_001" } },
            { "flinch", new[] { "silent_might_flee_nervous_f_001", "silent_might_flee_nervous_l_001", "silent_surprised_gasp_l_001" } },
            { "dismiss", new[] { "silent_negative_headshake_f_001", "silent_negative_headshake_f_002", "silent_negative_dismiss_f_002" } },
            { "shrug", new[] { "silent_neutral_shrug_f_003" } },
            { "nod", new[] { "silent_positive_headonod_f_001", "silent_headnod_greet_f_001", "silent_headnod_greet_f_003" } },
            { "puzzled", new[] { "silent_confused_look_f_001", "silent_confused_look_f_002", "silent_confused_punctuate_f_001" } },
            { "shamed", new[] { "aknwoledge_timid_look_down_f_001", "aknwoledge_timid_look_down_f_002", "silent_avoid_lookaround_f_001" } },
            { "hurt", new[] { "silent_surprised_gasp_l_002", "silent_surprised_gasp_r_002", "silent_might_flee_nervous_r_001" } }
        };

        /// How the one spoken to takes a line said in this tone (a Speech tag), by their temperament (null: none in
        /// particular): a reaction kind, or null when there's nothing to see.
        public static string Listener(string tone, string temper, string seed)
        {
            if (tone == null)
                return null;
            if (tone == HurtTone)
                return "hurt";
            bool hot = temper == "Hothead" || temper == "Proud", timid = temper == "Coward", cold = temper == "Cold" || temper == "Greedy" || temper == "Drifter",
                 kind = temper == "Loyal";
            string mood = Gestures.Mood(tone);
            if (tone == "mock" || tone == "won")
                return hot ? "bristle" : timid || kind ? "shamed" : "dismiss";   // laughed at, or beaten
            if (tone == "shame")
                return hot ? "bristle" : cold ? "dismiss" : "shamed";
            if (tone == "drunk")
                return hot ? "dismiss" : timid ? "flinch" : "puzzled";
            if (mood == "angry")
                return hot ? "bristle" : timid ? "flinch" : cold ? "dismiss" : kind ? (Voice.Chance(seed + "rk", 50) ? "flinch" : "shamed")
                     : Voice.Pick(seed + "ra", "bristle", "flinch", "dismiss");
            if (mood == "merry")
                return cold ? null : "nod";   // a cheer or a good word: taken with a nod, or not at all by the cold
            if (mood == "greet")
                return cold && !Voice.Chance(seed + "rg", 50) ? null : "nod";
            if (mood == "afraid")
                return cold || hot ? "dismiss" : "puzzled";   // the other one is frightened or hurting: contempt, or concern
            if (mood == "odd")
                return "puzzled";
            return Voice.Chance(seed + "rm", 45) ? Voice.Pick(seed + "rmk", "shrug", "nod") : null;   // the everyday: now and then
        }

        /// The clip for a reaction kind, from that sex's silent gesture set (Dict).
        public static string Clip(string kind, bool woman, string seed)
        {
            string[] pool;
            if (kind == null || !(woman ? Women : Men).TryGetValue(kind, out pool))
                return null;
            return Voice.Pick(seed + "rc", pool);
        }

        public static string Dict(bool woman)
        {
            return woman ? Gestures.FemaleSilent : Gestures.MaleSilent;
        }

        /// What it looks like, for the log: "bristles", "flinches", ...
        public static string Verb(string kind)
        {
            switch (kind)
            {
                case "bristle": return "bristles";
                case "flinch": return "flinches";
                case "dismiss": return "shakes their head";
                case "shrug": return "shrugs";
                case "nod": return "nods";
                case "puzzled": return "looks puzzled";
                case "shamed": return "looks down";
                case "hurt": return "reels";
                default: return kind ?? "does nothing";
            }
        }

        /// Every clip by sex, for the tests: "m|clip" and "f|clip".
        public static IEnumerable<string> All()
        {
            foreach (KeyValuePair<string, string[]> kv in Men)
                foreach (string c in kv.Value)
                    yield return "m|" + c;
            foreach (KeyValuePair<string, string[]> kv in Women)
                foreach (string c in kv.Value)
                    yield return "f|" + c;
        }
    }
}
