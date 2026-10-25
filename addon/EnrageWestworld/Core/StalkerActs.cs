// The odd things a stalker does while you stand still (StalkerWhenStill = eerie, or mix): the game's own emotes and
// scenarios, by their names in the game's files. The game side plays them, checks that each really played, and drops
// any that don't. Nothing here draws or fires a weapon: he never attacks.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public sealed class EerieAct
    {
        public string Key;          // short name, for the log and the list of ones that didn't play
        public string Does;         // what it looks like, for the log
        public string Emote;        // an emote played as an animation (script_mp@emotes@<name>@male@unarmed@full)...
        public string[] Clips;
        public bool WomanDict;      // the game has a women's version of this emote
        public string Scenario;     // ...or a scenario played where he stands, facing you
        public int Ms;              // a scenario's length
        public bool TurnAway;       // ...or he turns his back on you
        public char Sex = 'b';      // 'b' anyone, 'm' men only, 'f' women only (the game has no other version)
        public bool NightOnly;

        public string Dict(bool woman)
        {
            return Emote == null ? null : "script_mp@emotes@" + Emote + (woman && WomanDict ? "@female" : "@male") + "@unarmed@full";
        }
    }

    public static class StalkerActs
    {
        private static readonly string[] One = { "fullbody" };

        public static readonly EerieAct[] All =
        {
            E("watching", "points two fingers at the eyes, then at you", "im_watching_you", "fullbody", "fullbody_alt1"),
            E("hush", "puts a finger to the lips", "hush_your_mouth", "fullbody", "fullbody_alt_1", "fullbody_alt_2"),
            E("wave", "waves at you", "gentlewave", "fullbody", "fullbody_alt1", "fullbody_alt2"),
            E("smallwave", "gives a small wave", "subtle_wave"),
            E("beckon", "beckons you closer", "beckon", "fullbody", "fullbody_alt1"),
            E("comehere", "waves you over", "get_over_here"),
            E("follow", "waves for you to follow", "follow_me", "fullbody", "fullbody_alt1"),
            E("spooky", "makes a ghost-story gesture", "spooky"),
            E("scare", "lunges as if to give you a fright, from where it stands", "scare"),
            E("hypnosis", "swings a pocket watch at you, slowly", "hypnosis_pocket_watch"),
            E("wagfinger", "wags a finger at you", "wagfinger"),
            E("distance", "shades the eyes and looks far past you", "look_distance"),
            E("yonder", "looks off at something behind you", "look_yonder"),
            E("cuckoo", "circles a finger by the temple", "cuckoo"),
            Night(E("howl", "howls", "howl")),
            E("tiphat", "tips a hat to you", "tiphat"),
            E("bow", "bows to you", "respectful_bow"),
            E("prayer", "prays", "prayer", "fullbody", "fullbody_alt1"),
            E("sniff", "sniffs the air, like a dog on a trail", "sniffing"),
            E("snarl", "snarls like a cougar", "cougar_snarl"),
            E("laugh", "laughs", "jovial_laugh"),
            E("pointlaugh", "points at you and laughs", "pointlaugh"),
            E("applause", "applauds you", "applause"),
            Woman(E("notes", "takes notes, looking up at you between lines", "take_notes", "fullbody", "fullbody_alt1")),
            Woman(E("watch", "checks a pocket watch", "check_pocket_watch")),
            E("jig", "dances a jig", "prospector_jig", "fullbody", "fullbody_alt1"),
            E("shuffle", "shuffles in a little dance", "shuffle"),
            E("listen", "cups a hand to the ear, listening to you", "listen"),

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

            new EerieAct { Key = "back", Does = "turns its back on you, then looks over the shoulder", TurnAway = true }
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
            // scenarios take longer, so emotes come up a little more often
            List<EerieAct> emotes = fresh.Where(a => a.Emote != null).ToList(), others = fresh.Where(a => a.Emote == null).ToList();
            List<EerieAct> pool = emotes.Count > 0 && (others.Count == 0 || rng.Next(100) < 60) ? emotes : others;
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

        private static EerieAct S(string key, string does, string scenario, int ms, char sex)
        {
            return new EerieAct { Key = key, Does = does, Scenario = scenario, Ms = ms, Sex = sex };
        }

        private static EerieAct Night(EerieAct a)
        {
            a.NightOnly = true;
            return a;
        }

        private static EerieAct Woman(EerieAct a)
        {
            a.WomanDict = true;
            return a;
        }
    }
}
