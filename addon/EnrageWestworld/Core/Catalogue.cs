// The catalogue (the mind design, step 5; daytime run 2026-10-27, Core only): one table of what the game is proven to
// do, so mapping a feeling to what shows never invents a behaviour. Each entry: its channel (gesture, emote, scenario,
// clip, voice line, movement task), the game's name for it, the feelings and urges it can show, the least strength it
// fits, who can do it (men, women, anyone; standing or not) and how it was proven (played in game, its set found in
// game, the voice banks that carry it). Entries come only from what a log or Lex's play has shown: the standing gesture
// sets and their clips the add-on plays, the Online emotes Story Mode has, the scenarios that started, the clips the
// scenes and despair play, the line names the banks carry (counts from amb_takes.txt), the tasks the scenes use. A
// feeling or urge nothing here can show is shown by the thought alone, and goes on the list of animations to look for
// (Gaps). Plain C#, no game.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public sealed class Expression
    {
        public string Channel, Name, Who, Posture, Proof;
        public string[] Feelings, Urges;
        public string Least;   // mild, moderate or strong: the least strength it fits

        /// Proven: played in game, or a line the voice banks carry. Last night's clips no log has shown playing yet are
        /// listed, marked, and left out until one does.
        public bool Proven
        {
            get { return Proof == null || !Proof.StartsWith("NOT YET SEEN"); }
        }

        public bool Fits(string feeling, string urge, string strength, bool woman, bool standing)
        {
            if (Who == "men" && woman || Who == "women" && !woman || Posture == "standing" && !standing)
                return false;
            if (Array.IndexOf(Feelings, feeling) < 0 && Array.IndexOf(Urges, urge) < 0)
                return false;
            return Rank(strength) >= Rank(Least);
        }

        private static int Rank(string s)
        {
            return s == "strong" ? 3 : s == "moderate" ? 2 : s == "mild" ? 1 : 0;
        }
    }

    public static class Catalogue
    {
        private static Expression E(string channel, string name, string feelings, string urges, string least, string who, string posture, string proof)
        {
            return new Expression
            {
                Channel = channel, Name = name, Feelings = feelings.Length == 0 ? new string[0] : feelings.Split(','), Urges = urges.Length == 0 ? new string[0] : urges.Split(','),
                Least = least, Who = who, Posture = posture, Proof = proof
            };
        }

        private const string Silent = "ai_gestures@gen_{sex}@standing@silent", Speaker = "ai_gestures@gen_{sex}@standing@speaker";
        private const string SetsFound = "set found in game (0.6.1 probe)", Played = "played in game";

        public static readonly Expression[] All =
        {
            // gestures (upper body, standing; the four standing sets found in game, 0.6.1)
            E("gesture", Silent + " / silent_neutral_hat_tip_l_001", "gratitude,joy,pride", "share,repay,display", "mild", "men", "standing", "NOT YET SEEN: last night's nods (the set was found, 0.6.1)"),
            E("gesture", Silent + " / silent_headnod_greet_f_001", "gratitude,joy,relief", "share,repay", "mild", "any", "standing", "NOT YET SEEN: last night's nods"),
            E("gesture", Silent + " / silent_neutral_wave_l_001", "joy,gratitude", "share", "mild", "any", "standing", "NOT YET SEEN: last night's nods"),
            E("gesture", Silent + " / silent_neutral_spit_f_001", "contempt,anger", "dismiss,disparage", "moderate", "men", "standing", "NOT YET SEEN: last night's nods and sights; men only"),
            E("gesture", Silent + " / head shake", "contempt,anger,shame", "dismiss,disparage", "mild", "any", "standing", Played + " (0.6.1, the stalker's head-shake on the upper body)"),
            E("gesture", "ai_gestures@gen_female@standing@speaker / surprised_gasp_f_001", "fear,pity", "freeze,avoid,help", "mild", "women", "standing", "NOT YET SEEN: last night's sights"),
            E("gesture", "ai_gestures@gen_male@standing@speaker / negative_disbelief_l_001", "contempt,anger,fear", "dismiss,watch", "mild", "men", "standing", "NOT YET SEEN: last night's sights"),
            E("gesture", Speaker + " / happy_cheer_f_001", "joy,gratitude,pride", "share,display", "moderate", "any", "standing", "NOT YET SEEN: last night's round"),
            E("gesture", Speaker + " / happy_laugh_f_001", "joy,contempt", "share,dismiss", "mild", "any", "standing", "NOT YET SEEN: last night's round"),
            E("gesture", Speaker + " / neutral_point_l_001", "anger,contempt", "confront,disparage", "moderate", "any", "standing", "NOT YET SEEN: last night's traveller"),
            // the few Online emotes Story Mode has (0.6.1 probe: 3 of 28 sets)
            E("emote", "script_mp@emotes@respectful_bow@male@unarmed@full", "gratitude,pride", "repay,display", "moderate", "any", "standing", Played + " (0.6.1, the stalker's acts)"),
            E("emote", "script_mp@emotes@pointlaugh@male@unarmed@full", "contempt,joy", "dismiss,disparage", "moderate", "any", "standing", Played + " (0.6.1)"),
            E("emote", "script_mp@emotes@prospector_jig@male@unarmed@full", "joy,pride", "share,display", "strong", "any", "standing", Played + " (0.6.1)"),
            // scenarios (started in game; gravedig, hammer and scrub failed and are left out)
            E("scenario", "WORLD_HUMAN_GRAVE_MOURNING_KNEEL", "grief,guilt,shame", "withdraw,amends", "moderate", "any", "any", Played + " (the Sabbath's kneel, the despair kneel)"),
            E("scenario", "WORLD_HUMAN_DRUNK_BAR_SLUMPED", "grief,shame", "withdraw,hide", "moderate", "men", "any", Played + " (the bar)"),
            E("scenario", "WORLD_HUMAN_CROUCH_INSPECT", "pity,grief", "help", "mild", "any", "any", "NOT YET SEEN: the visitor's crouch (last night)"),
            E("scenario", "WORLD_HUMAN_SIT_GROUND", "grief,shame,fear", "withdraw,rest", "moderate", "any", "any", "NOT YET SEEN: the fall's sit (last night)"),
            // clips the scenes and despair play
            E("clip", "mech_grapple / table_smash", "anger", "confront", "strong", "men", "standing", Played + " (faces into the bar)"),
            E("clip", "script_re@street_fight@v3", "anger", "confront,intervene", "strong", "men", "standing", Played + " (street fights)"),
            E("clip", "script_re@drunk_dueler", "anger,contempt", "confront", "moderate", "men", "standing", Played + " (the drunk's challenge)"),
            E("clip", "script_re@rally@cross_accident / sobbing_idle_a_leader", "grief,shame,fear", "withdraw", "strong", "any", "standing", Played + " (despair, 2026-10-27; sobs on a man and a woman)"),
            E("clip", "script_story@mry1@ig@ig6_jamie_attempts_suicide", "grief,shame", "withdraw", "strong", "any", "standing", Played + " (despair, 2026-10-27)"),
            E("clip", "ai_combat@damage@writhe@base / writhe_loop", "fear,grief", "freeze", "strong", "any", "any", "NOT YET SEEN: despair's knife writhe"),
            // movement tasks the scenes use, proven in play
            E("move", "TASK_SMART_FLEE_PED", "fear", "avoid", "moderate", "any", "any", "NOT YET SEEN: last night's visitor and robbery"),
            E("move", "TASK_GO_TO_ENTITY", "anger,pity,grief", "confront,intervene,help", "moderate", "any", "any", "NOT YET SEEN: last night's grudge walk"),
            E("move", "TASK_TURN_PED_TO_FACE_ENTITY", "anger,contempt,fear,pity", "confront,watch", "mild", "any", "any", Played + " (scenes, sights)"),
            E("move", "TASK_LOOK_AT_ENTITY", "fear,anger,contempt,envy,pity,joy", "watch,freeze", "mild", "any", "any", Played + " (nods, sights, wake acts)"),
            E("move", "TASK_HANDS_UP", "fear", "freeze", "strong", "any", "any", "NOT YET SEEN: last night's robbery"),
            E("move", "TASK_STAND_STILL", "fear,shame", "freeze", "moderate", "any", "any", Played + " (the animation rows hold people with it between clips, 2026-10-27)"),
            E("move", "TASK_WANDER_STANDARD", "contempt,shame,envy,guilt", "dismiss,avoid,withdraw,hide", "mild", "any", "any", Played + " (every hand-back)"),
            // the game's own recorded lines (how many of the 332 voices carry each, from amb_takes.txt)
            E("voice", "GENERIC_ANGRY_SHOUT", "anger", "confront", "strong", "any", "any", "voice banks carry it (Speech.Angry)"),
            E("voice", "CHALLENGE_THREATEN", "anger,contempt", "confront", "moderate", "any", "any", "297 of 332 voices"),
            E("voice", "GET_OUT", "anger,contempt", "confront,dismiss", "moderate", "any", "any", "317 of 332 voices"),
            E("voice", "SHAME_ON_YOU", "contempt,anger", "disparage,confront", "moderate", "any", "any", "302 of 332 voices"),
            E("voice", "DISMISSIVE_REACT", "contempt", "dismiss", "mild", "any", "any", "voice banks carry it (Speech.Mild)"),
            E("voice", "VOCAL_FX_DISAPPROVE", "contempt,envy", "dismiss,disparage", "mild", "any", "any", "179 of 332 voices (wordless)"),
            E("voice", "GENERIC_FRIGHTENED_HIGH", "fear", "avoid,freeze", "strong", "any", "any", "voice banks carry it (Speech.Scared)"),
            E("voice", "GENERIC_FRIGHTENED_MED", "fear", "avoid", "moderate", "any", "any", "voice banks carry it (Speech.Scared)"),
            E("voice", "SCARED_HELP", "fear", "avoid,freeze", "strong", "any", "any", "284 of 332 voices"),
            E("voice", "PLEAD", "fear", "freeze", "strong", "any", "any", "330 of 332 voices"),
            E("voice", "GENERIC_SHOCKED_DISBELIEF", "fear,grief", "freeze,watch", "moderate", "any", "any", "voice banks carry it; played (despair's disbelief)"),
            E("voice", "SORRY_TO_HEAR_THAT", "pity,grief", "help", "mild", "any", "any", "329 of 332 voices"),
            E("voice", "GENERIC_THANKS", "gratitude,relief", "repay", "mild", "any", "any", "329 of 332 voices"),
            E("voice", "RELIEVED_REACT", "relief", "rest,share", "moderate", "any", "any", "295 of 332 voices"),
            E("voice", "GENERIC_CHEER", "joy,pride", "share,display", "moderate", "any", "any", "276 of 332 voices"),
            E("voice", "COOL_IT", "pity,anger", "intervene", "moderate", "any", "any", "296 of 332 voices"),
            E("voice", "DISPUTE_INTERVENE_HARASSMENT", "anger,pity", "intervene", "moderate", "any", "any", "223 of 332 voices"),
            E("voice", "STORM_OFF", "shame,anger", "withdraw,avoid", "moderate", "any", "any", "216 of 332 voices"),
            E("voice", "IM_OUTTA_HERE", "fear,shame", "avoid", "moderate", "any", "any", "246 of 332 voices"),
            E("voice", "GENERIC_MOCK", "contempt,envy", "disparage,dismiss", "mild", "any", "any", "voice banks carry it; played (the waking voices)"),
            E("voice", "GOING_BADLY", "grief,fear", "withdraw", "moderate", "any", "any", "329 of 332 voices"),
            E("voice", "LEAVE_ME", "grief,shame", "withdraw", "moderate", "any", "any", "210 of 332 voices"),
            E("voice", "GREET_GENERAL_FAMILIAR", "joy,gratitude", "share", "mild", "any", "any", "328 of 332 voices"),
        };

        /// What could show this feeling with this urge, at this strength, for this person (standing or not).
        public static IEnumerable<Expression> For(string feeling, string urge, string strength, bool woman, bool standing)
        {
            return All.Where(x => x.Proven && x.Fits(feeling, urge, strength, woman, standing));
        }

        /// The feelings and urges nothing in the catalogue can show yet: the animations to look for.
        public static List<string> Gaps(IEnumerable<string> feelings, IEnumerable<string> urges)
        {
            var gaps = new List<string>();
            foreach (string f in feelings)
                if (f != "none" && !All.Any(x => x.Proven && Array.IndexOf(x.Feelings, f) >= 0 && x.Channel != "voice" && x.Channel != "move"))
                    gaps.Add("feeling " + f + " (no gesture, clip or scenario)");
            foreach (string u in urges)
                if (u != "none" && !All.Any(x => x.Proven && Array.IndexOf(x.Urges, u) >= 0))
                    gaps.Add("urge " + u);
            return gaps;
        }
    }
}
