// The odd things a stalker does while you stand still (StalkerWhenStill = eerie, or mix): the game's own gestures,
// emotes and scenarios, by their names in the game's files, and a few that play on not being seen (creeping closer,
// standing somewhere else when you look back). The game side plays them, checks that each really played, and drops
// any that don't. Nothing here draws or fires a weapon: he never attacks.
// Being in the rdr3_discoveries dumps doesn't mean Story Mode has it: most of Online's emotes aren't there (22 of 28
// were missing in game, 0.6.0). The game side checks every animation set at load and logs which are missing.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public sealed class EerieAct
    {
        public string Key;          // short name, for the log and the list of ones that didn't play
        public string Does;         // what it looks like, for the log
        public string Emote;        // an Online emote that Story Mode has too (script_mp@emotes@<name>@male@unarmed@full)...
        public string[] Clips;
        public string Gesture;     // ...or a townsfolk gesture (ai_gestures@gen_male@standing@<Gesture>, gen_female for women),
        public string[] ClipsF;     // with Clips for men and ClipsF for women
        public string Scenario;     // ...or a scenario played where he stands, facing you
        public int Ms;              // a scenario's length
        public string Special;      // ...or "back" (turns his back on you), "creep" (closer while you can't see him),
                                    // "shift" (somewhere else each time you look back)
        public char Sex = 'b';      // 'b' anyone, 'm' men only, 'f' women only (the game has no other version)
        public bool NightOnly;

        /// An animation (a gesture or an emote), not a scenario or a special.
        public bool Anim { get { return Emote != null || Gesture != null; } }

        public string Dict(bool woman)
        {
            if (Gesture != null)
                return "ai_gestures@gen_" + (woman ? "female" : "male") + "@standing@" + Gesture;
            return Emote == null ? null : "script_mp@emotes@" + Emote + "@male@unarmed@full";   // the men's version, for anyone
        }

        public string[] ClipsFor(bool woman)
        {
            return Gesture != null && woman ? ClipsF : Clips;
        }
    }

    public static class StalkerActs
    {
        private static readonly string[] One = { "fullbody" };

        public static readonly EerieAct[] All =
        {
            // the only Online emotes Story Mode has, of the 28 tried in game
            E("bow", "bows to you", "respectful_bow"),
            E("pointlaugh", "points at you and laughs", "pointlaugh"),
            E("jig", "dances a jig", "prospector_jig", "fullbody", "fullbody_alt1"),

            // the gestures townsfolk make as they talk, aimed at you (the _f_ clips face front; _l_ and _r_ only where
            // that's all there is, as for a hat tip). Null: that sex has no clip for it.
            G("tiphat", "tips a hat to you", "silent", "silent_neutral_hat_tip_l_001 silent_neutral_hat_tip_r_001 silent_neutral_hat_tip_l_004 silent_neutral_hat_tip_r_004", null),
            G("farwave", "raises a hand to you from where it stands", "silent", "silent_greet_distance_l_001 silent_greet_distance_r_001", "silent_greet_distance_l_001 silent_greet_distance_r_001"),
            G("salute", "salutes you", "silent", "silent_casual_salute_l_001 silent_casual_salute_r_001", "silent_sarcastic_salute_l_001 silent_sarcastic_salute_r_001"),
            G("chin", "lifts the chin at you", "silent", "silent_neutral_chin_up_f_001", "silent_neutral_chin_up_f_001"),
            G("spit", "spits, eyes on you the whole time", "silent", "silent_neutral_spit_f_001 silent_neutral_spit_f_002", "silent_negative_spit_f_001"),
            G("headshake", "shakes the head at you", "silent", "silent_negative_headshake_f_002 silent_negative_headshake_f_003", "silent_negative_headshake_f_001 silent_negative_headshake_f_002 silent_negative_headshake_f_003"),
            G("lookround", "looks all round, as if someone called", "silent", "silent_alert_lookaround_f_001 silent_alert_lookaround_f_002", "silent_avoid_lookaround_f_001 silent_avoid_lookaround_f_004"),
            G("cantplace", "looks at you as if it can't place you", "silent", "silent_confused_look_f_001 silent_confused_look_f_002", "silent_confused_look_f_001 silent_confused_look_f_002"),
            G("sky", "looks up at the sky, then back at you", "silent", "silent_frustrated_lookup_f_001 silent_frustrated_lookup_f_002", null),
            G("start", "starts, as if you'd come from nowhere", "silent", "silent_surprised_react_f_001 silent_surprised_react_f_002 silent_surprised_react_f_003", "silent_surprised_react_f_002"),
            G("taunt", "taunts you without taking a step", "silent", "silent_aggressive_taunt_f_001", null),
            G("hush", "holds up a hand for quiet", "speaker", "neutral_halt_quiet_l_001 neutral_halt_quiet_r_001", null),
            G("beckon", "waves to get your attention", "speaker", "neutral_get_attention_l_001 neutral_get_attention_r_001 neutral_get_attention_l_002 neutral_get_attention_r_002", "neutral_get_attention_l_001 neutral_get_attention_r_001 neutral_get_attention_l_002 neutral_get_attention_r_002"),
            G("waveat", "waves at you", "speaker", "neutral_wave_fl_001 neutral_wave_fr_001 neutral_wave_fl_002 neutral_wave_fr_002", "neutral_wave_fl_002 neutral_wave_fr_002"),
            G("point", "points at you", "speaker", null, "neutral_point_fl_002 neutral_point_fr_002"),
            G("laugh", "laughs at nothing", "speaker", "happy_laugh_f_001 happy_laugh_f_002 happy_laugh_f_003", "happy_laugh_f_001 happy_laugh_f_002 happy_chuckle_f_001"),
            G("cheer", "cheers at nothing", "speaker", "happy_cheer_f_001 happy_cheer_f_002 happy_cheer_f_003", "happy_cheer_f_001 happy_cheer_f_002 happy_cheer_f_003"),
            G("nod", "nods at you, as if you'd agreed to something", "speaker", "positive_nod_f_001 positive_nod_f_002 positive_nod_f_003", "positive_nod_f_001 positive_nod_f_002 positive_nod_f_005"),
            G("thanks", "thanks you for nothing you did", "speaker", "thank_you_f_001 thank_you_f_002", null),
            G("cuckoo", "taps a finger to the head", "speaker", null, "neutral_pointhead_l_001 neutral_pointhead_r_001"),
            G("calm", "motions you to stay calm", "speaker", "calm_down_f_002 calm_down_f_003", null),

            S("gravedig", "digs a grave", "WORLD_HUMAN_GRAVEDIG", 22000, 'm'),
            S("scrub", "kneels and scrubs blood from the ground", "WORLD_HUMAN_TWNREC_SCRUB_BLOOD_FLOOR_BUCKET", 18000, 'b'),
            S("mourn", "kneels as if at a graveside", "WORLD_HUMAN_GRAVE_MOURNING_KNEEL", 15000, 'b'),
            S("mourner", "stands like a mourner at a burial", "WORLD_HUMAN_GRAVE_MOURNING", 14000, 'b'),
            S("tracks", "crouches and studies the ground where you walked", "WORLD_HUMAN_CROUCH_INSPECT", 11000, 'b'),
            S("notebook", "writes in a notebook", "WORLD_HUMAN_WRITE_NOTEBOOK", 15000, 'b'),
            S("sketch", "sits and sketches you", "WORLD_HUMAN_SIT_GROUND_SKETCHING", 18000, 'f'),
            S("whittle", "sits and whittles", "WORLD_HUMAN_SIT_GROUND_WHITTLE", 18000, 'm'),
            S("hammer", "kneels and hammers at the ground", "WORLD_HUMAN_HAMMER_KNEEL", 13000, 'm'),
            S("dead", "lies down in the dirt like the dead", "WORLD_HUMAN_SLEEP_GROUND_ARM", 16000, 'b'),
            S("dance", "dances alone", "WORLD_HUMAN_DANCING", 15000, 'f'),
            Night(S("lantern", "holds a lantern up toward you", "WORLD_HUMAN_GUARD_LANTERN_NERVOUS", 15000, 'm')),
            S("mirror", "studies her face in a pocket mirror", "WORLD_HUMAN_POCKET_MIRROR", 11000, 'f'),
            S("statue", "stands like a statue", "WORLD_HUMAN_STARE_STOIC", 11000, 'b'),
            S("smoke", "smokes, much too fast", "WORLD_HUMAN_SMOKE_NERVOUS_STRESSED", 15000, 'm'),

            new EerieAct { Key = "back", Does = "turns its back on you, then looks over the shoulder", Special = "back" },
            new EerieAct { Key = "creep", Does = "comes closer whenever you look away, and stops dead when you look", Special = "creep" },
            new EerieAct { Key = "shift", Does = "stands somewhere else each time you look back", Special = "shift" }
        };

        /// The next act for this stalker: one the game has for a man or a woman, at this hour, not one of the last few,
        /// and none that failed to play. Null when nothing is left.
        public static EerieAct Choose(bool woman, bool night, IList<string> recent, ICollection<string> bad, Random rng)
        {
            List<EerieAct> ok = All.Where(a => (a.Sex == 'b' || a.Sex == (woman ? 'f' : 'm')) && (night || !a.NightOnly) &&
                                               (bad == null || !bad.Contains(a.Key))).ToList();
            List<EerieAct> fresh = ok.Where(a => recent == null || !recent.Contains(a.Key)).ToList();
            if (fresh.Count == 0)
                fresh = ok;
            if (fresh.Count == 0)
                return null;
            // a quarter of the time something odd happens when you aren't looking (creep, shift) or he turns away; of the
            // rest, three in five are gestures (they're short), the others scenarios. An empty pool gives its turn to the next.
            List<EerieAct> specials = fresh.Where(a => a.Special != null).ToList(), emotes = fresh.Where(a => a.Anim).ToList(),
                           scenarios = fresh.Where(a => a.Scenario != null).ToList();
            int r = rng.Next(100);
            List<EerieAct>[] order = r < 25 ? new[] { specials, emotes, scenarios } : r < 70 ? new[] { emotes, scenarios, specials } : new[] { scenarios, emotes, specials };
            List<EerieAct> pool = order.First(p => p.Count > 0);
            return pool[rng.Next(pool.Count)];
        }

        /// How an emote is played: "" as it is, "repeat" (the same gesture again, exactly), "freeze" (stops dead
        /// partway, then carries on) or "slow".
        public static string Twist(Random rng)
        {
            int r = rng.Next(100);
            return r < 40 ? "" : r < 65 ? "repeat" : r < 85 ? "freeze" : "slow";
        }

        /// StalkerWhenStill = mix: what he does this time.
        public static string Mix(Random rng)
        {
            int r = rng.Next(100);
            return r < 60 ? "eerie" : r < 80 ? "circle" : r < 90 ? "watch" : "rush";
        }

        private static EerieAct E(string key, string does, string emote, params string[] clips)
        {
            return new EerieAct { Key = key, Does = does, Emote = emote, Clips = clips.Length > 0 ? clips : One };
        }

        /// A townsfolk gesture: clips for men and for women, space-separated; null where that sex has none.
        private static EerieAct G(string key, string does, string sub, string men, string women)
        {
            return new EerieAct
            {
                Key = key, Does = does, Gesture = sub,
                Clips = men == null ? null : men.Split(' '),
                ClipsF = women == null ? null : women.Split(' '),
                Sex = men == null ? 'f' : women == null ? 'm' : 'b'
            };
        }

        private static EerieAct S(string key, string does, string scenario, int ms, char sex)
        {
            return new EerieAct { Key = key, Does = does, Scenario = scenario, Ms = ms, Sex = sex };
        }

        private static EerieAct Night(EerieAct a)
        {
            a.NightOnly = true;
            return a;
        }
    }
}
