// The townsfolk gestures that go with a line said in a given tone, for men and for women: the ai_gestures sets Story
// Mode has (all four found in game in 0.6.1; the head-shake, thank-you, raised hand and spit seen playing). The game
// side plays them on the upper body while the line is said, so a quarrel has pointing and head-shaking in it, a
// greeting a wave or a hat tip, a joke a laugh.
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Gestures
    {
        public const string MaleSilent = "ai_gestures@gen_male@standing@silent", MaleSpeaker = "ai_gestures@gen_male@standing@speaker",
                            FemaleSilent = "ai_gestures@gen_female@standing@silent", FemaleSpeaker = "ai_gestures@gen_female@standing@speaker";

        public static readonly string[] Dicts = { MaleSilent, MaleSpeaker, FemaleSilent, FemaleSpeaker };

        // "S|clip" from the silent set, "P|clip" from the speaker set
        private static readonly Dictionary<string, string[]> Men = new Dictionary<string, string[]>
        {
            { "angry", new[] { "P|angry_point_l_001", "P|angry_point_r_001", "P|angry_dismiss_f_001", "P|negative_headshake_f_001", "P|damn_it_f_002", "S|silent_aggressive_taunt_f_001" } },
            { "mild", new[] { "P|neutral_shrug_f_001", "P|positive_nod_f_001", "S|silent_neutral_shrug_f_001" } },
            { "merry", new[] { "P|happy_laugh_f_001", "P|happy_laugh_f_002", "P|happy_cheer_f_001" } },
            { "greet", new[] { "S|silent_casual_greet_l_001", "S|silent_neutral_hat_tip_l_001", "P|neutral_wave_fl_001", "P|thank_you_f_001" } },
            { "afraid", new[] { "P|defensive_plead_f_001", "S|silent_might_flee_nervous_f_001", "S|silent_confused_look_f_001" } },
            { "odd", new[] { "S|silent_confused_look_f_002", "S|silent_alert_lookaround_f_001", "S|silent_frustrated_lookup_f_001" } }
        };

        private static readonly Dictionary<string, string[]> Women = new Dictionary<string, string[]>
        {
            { "angry", new[] { "P|angry_point_l_003", "P|angry_point_r_003", "P|angry_dismiss_f_001", "P|aggressive_no_l_001", "P|damn_it_f_001", "S|silent_negative_headshake_f_001" } },
            { "mild", new[] { "P|neutral_shrug_f_001", "P|positive_nod_f_001", "S|silent_neutral_shrug_f_003" } },
            { "merry", new[] { "P|happy_laugh_f_001", "P|happy_chuckle_f_001", "P|happy_cheer_f_001" } },
            { "greet", new[] { "S|silent_neutral_greet_f_002", "S|silent_flirty_greet_l_001", "P|neutral_wave_fl_002" } },
            { "afraid", new[] { "P|defensive_plead_f_001", "S|silent_might_flee_nervous_f_001", "S|silent_surprised_gasp_l_001" } },
            { "odd", new[] { "S|silent_confused_look_f_001", "S|silent_avoid_lookaround_f_001" } }
        };

        /// The kind of gesture a line's tone calls for (the voice tags scene lines carry).
        public static string Mood(string tone)
        {
            switch (tone)
            {
                case "angry": case "threat": case "storm": case "no": case "thief": case "fight": case "replybad": case "tut": return "angry";
                case "mock": case "cheer": case "won": case "drunk": return "merry";
                case "greet": case "thanks": case "yes": case "familiar": case "welcome": case "flatter": case "again": return "greet";
                case "scared": case "shame": case "plead": case "chatbad": return "afraid";
                case "stare": case "weird": case "uneasy": return "odd";
                default: return "mild";
            }
        }

        /// A gesture for a line in this tone: the animation set, with the clip in `clip`.
        public static string For(string tone, bool woman, string seed, out string clip)
        {
            string[] pool = (woman ? Women : Men)[Mood(tone)];
            string pick = Voice.Pick(seed + "g", pool);
            clip = pick.Substring(2);
            bool silent = pick[0] == 'S';
            return woman ? (silent ? FemaleSilent : FemaleSpeaker) : (silent ? MaleSilent : MaleSpeaker);
        }

        /// Every set and clip, for the tests.
        public static IEnumerable<string> All()
        {
            foreach (KeyValuePair<string, string[]> kv in Men)
                foreach (string c in kv.Value)
                    yield return (c[0] == 'S' ? MaleSilent : MaleSpeaker) + " / " + c.Substring(2);
            foreach (KeyValuePair<string, string[]> kv in Women)
                foreach (string c in kv.Value)
                    yield return (c[0] == 'S' ? FemaleSilent : FemaleSpeaker) + " / " + c.Substring(2);
        }
    }
}
