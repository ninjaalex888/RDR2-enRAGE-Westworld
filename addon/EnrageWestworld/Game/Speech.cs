// The game's own voiced lines, played on any ped in that ped's real voice. Rockstar recorded a few hundred
// short reactions per voice (greetings, insults, fear, cheering, pleading, gossip...), so the add-on can't
// make a ped say new words, but it can pick the recorded line that fits what the subtitle says.
// Not every voice has every line: each set lists the best fits first, and SayAny remembers per model which
// lines worked and which didn't (failures are retried after a while: a line can fail just because the ped
// was busy). The sets come from which lines the ambient voices actually carry.
using System.Collections.Generic;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    internal static class Speech
    {
        private const ulong PlayAmbientSpeech = 0x8E04FEDD28D42462;
        private const int RetryFailedMs = 120000;
        private static readonly string[] Params = { "SPEECH_PARAMS_FORCE_NORMAL_CLEAR", "SPEECH_PARAMS_FORCE" };
        private static readonly HashSet<string> Worked = new HashSet<string>();
        private static readonly Dictionary<string, int> Failed = new Dictionary<string, int>();
        private static int _turn;

        public static readonly string[] Greet = { "GREET_GENERAL_STRANGER", "HOWS_IT_GOING", "GREET_GENERAL_FAMILIAR", "GENERIC_HOWDY", "GENERIC_HI", "CHAT_GREET" };
        public static readonly string[] Familiar = { "GREET_GENERAL_FAMILIAR", "HOWS_IT_GOING", "GREET_GENERAL_STRANGER" };
        public static readonly string[] Again = { "GREET_AGAIN" };   // "your memory fading? you already said hello": for the Awakening, not plain greetings
        public static readonly string[] Angry = { "GENERIC_ANGRY_SHOUT", "GENERIC_INSULT_HIGH_NEUTRAL", "WHATS_YOUR_PROBLEM", "GENERIC_ANGRY_REACTION", "GENERIC_CURSE_HIGH", "INSULT_RESPONSE", "GENERIC_INSULT_MED_NEUTRAL" };
        public static readonly string[] Mild = { "GENERIC_CURSE_MED", "DISMISSIVE_REACT", "CUT_THAT_OUT", "INSULT_RESPONSE", "GENERIC_INSULT_MED_NEUTRAL" };
        public static readonly string[] Scared = { "GENERIC_FRIGHTENED_HIGH", "SCARED_HELP", "GENERIC_FRIGHTENED_MED", "PANIC_HELP", "GENERIC_SHOCKED_HIGH" };
        public static readonly string[] Yes = { "GENERIC_YES", "GENERIC_AGREE", "RESPONSE_GENERIC" };
        public static readonly string[] No = { "GENERIC_NO", "DISMISSIVE_REACT", "GENERIC_DISMISS" };
        public static readonly string[] Thanks = { "GENERIC_THANKS", "GENERIC_THANKS_SHOUTED" };
        public static readonly string[] Welcome = { "GENERIC_THANKS_RESPONSE", "RESPONSE_GENERIC" };
        public static readonly string[] Bye = { "GENERIC_GOODBYE", "GOODBYE_START", "GENERIC_BYE" };
        public static readonly string[] Threat = { "CHALLENGE_THREATEN", "GET_OUT", "LEAVE_NOW", "DONT_BE_STUPID", "GENERIC_THREATEN" };
        public static readonly string[] ThreatArmed = { "CHALLENGE_THREATEN_ARMED", "CHALLENGE_THREATEN", "FINAL_WARNING" };
        public static readonly string[] Stare = { "PROVOKE_STARE", "WHATS_YOUR_PROBLEM", "GENERIC_SHOCKED_MED" };
        public static readonly string[] Won = { "WON_DISPUTE", "WON_FIGHT", "WON_DISPUTE_ESCALATED", "GENERIC_CHEER" };
        public static readonly string[] Mock = { "GENERIC_MOCK", "TAUNT_GEN_NEUTRAL", "TEASE_RESPONSE" };
        public static readonly string[] Cheer = { "GENERIC_CHEER_ON", "GENERIC_CHEER", "GENERIC_EXCITING_EVENT_COMMENT" };
        public static readonly string[] Shame = { "SHAME_ON_YOU", "SICK_BASTARD", "GENERIC_SHOCKED_DISBELIEF" };
        public static readonly string[] News = { "GENERIC_SHOCKING_EVENT_COMMENT", "GENERIC_EXCITING_EVENT_COMMENT", "COME_SEE_THIS", "WHATS_GOING_ON" };
        public static readonly string[] Respond = { "EVENT_COMMENT_RESPONSE", "RESPONSE_GENERIC", "CHAT_RESPONSE_A", "RESPONSE_A", "RESPONSE_B" };
        public static readonly string[] NoIdea = { "NO_IDEA", "RESPONSE_GENERIC" };
        public static readonly string[] Sorry = { "SORRY_TO_HEAR_THAT", "RESPONSE_GENERIC" };
        public static readonly string[] Plead = { "PLEAD", "DONT_KILL_ME", "GUN_BEG", "LEAVE_ME_PLEAD", "DONT_HURT_ME", "GENERIC_FRIGHTENED_HIGH" };
        public static readonly string[] Relieved = { "RELIEVED_REACT", "GENERIC_THANKS" };
        public static readonly string[] Flatter = { "CHAT_FLATTER", "GREET_FLIRTY", "GREET_GENERAL_FAMILIAR" };
        public static readonly string[] Storm = { "STORM_OFF", "IM_OUTTA_HERE", "GENERIC_CURSE_MED" };
        public static readonly string[] Intervene = { "DISPUTE_INTERVENE_DANGEROUS", "DISPUTE_INTERVENE_HARASSMENT", "COOL_IT", "STOP_THAT" };
        public static readonly string[] Defuse = { "DEFUSE_RESPONSE", "COOL_IT", "DONT_WANT_TROUBLE" };
        public static readonly string[] Drunk = { "DRUNK", "GENERIC_EXCITING_EVENT_COMMENT" };
        public static readonly string[] Thief = { "GET_THE_LAW", "PANIC_HELP", "LAW_HAIL", "SCARED_HELP" };
        public static readonly string[] Harmed = { "PLAYER_HARMED_TOWN", "WRONGED_RECENTLY", "TROUBLE_RECENTLY", "WAS_THIS_YOU", "SHAME_ON_YOU" };
        public static readonly string[] Helped = { "PLAYER_HELPED_TOWN", "HELPED_RECENTLY", "GENERIC_THANKS" };
        public static readonly string[] Weird = { "PLAYER_ACTING_WEIRD", "PLAYER_LOOKING_WEIRD", "WHATS_GOING_ON" };
        public static readonly string[] Uneasy = { "WHATS_GOING_ON", "GENERIC_SHOCKED_MED", "NO_IDEA", "GENERIC_SHOCKED_DISBELIEF" };   // the uncanny lines: someone unsettled by their own world
        public static readonly string[] FightTalk = { "MELEE_BRING_IT_ON", "FIGHT", "FIGHT_RESPONSE", "GENERIC_ANGRY_SHOUT" };
        public static readonly string[] Chat = { "HOWS_IT_GOING", "CHAT_GOOD_WEATHER", "COMMENT_WORKING_HARD", "GREET_GENERAL_FAMILIAR" };
        public static readonly string[] ChatBad = { "CHAT_BAD_WEATHER", "HOWS_IT_GOING" };
        public static readonly string[] ChatReply = { "GOING_WELL", "RESPONSE_GOOD_WEATHER", "RESPONSE_WORKING_HARD", "RESPONSE_GENERIC" };
        public static readonly string[] ChatReplyBad = { "GOING_BADLY", "RESPONSE_GENERIC" };
        public static readonly string[] Wanted = { "GET_THE_LAW", "WAS_THIS_YOU", "GENERIC_SHOCKED_HIGH" };
        public static readonly string[] Buy = { "GENERIC_BUY", "GENERIC_YES" };

        /// A greeting that fits the time of day.
        public static string[] GreetNow()
        {
            int h = CLOCK.GET_CLOCK_HOURS();
            if (h >= 4 && h < 12)
                return new[] { "GREET_MORNING", "GREET_GENERAL_STRANGER", "HOWS_IT_GOING", "GENERIC_HOWDY" };
            if (h >= 18 || h < 4)
                return new[] { "GREET_EVENING", "GREET_GENERAL_STRANGER", "HOWS_IT_GOING", "GENERIC_HOWDY" };
            return Greet;
        }

        /// The set a scene's voice tag stands for (null for no voice).
        public static string[] Set(string tag)
        {
            switch (tag)
            {
                case "greet": return GreetNow();
                case "familiar": return Familiar;
                case "again": return Again;
                case "angry": return Angry;
                case "mild": return Mild;
                case "scared": return Scared;
                case "yes": return Yes;
                case "no": return No;
                case "thanks": return Thanks;
                case "welcome": return Welcome;
                case "bye": return Bye;
                case "threat": return Threat;
                case "armed": return ThreatArmed;
                case "stare": return Stare;
                case "won": return Won;
                case "mock": return Mock;
                case "cheer": return Cheer;
                case "shame": return Shame;
                case "news": return News;
                case "respond": return Respond;
                case "noidea": return NoIdea;
                case "sorry": return Sorry;
                case "plead": return Plead;
                case "relieved": return Relieved;
                case "flatter": return Flatter;
                case "storm": return Storm;
                case "intervene": return Intervene;
                case "defuse": return Defuse;
                case "drunk": return Drunk;
                case "thief": return Thief;
                case "harmed": return Harmed;
                case "helped": return Helped;
                case "weird": return Weird;
                case "uneasy": return Uneasy;
                case "fight": return FightTalk;
                case "chat": return Chat;
                case "chatbad": return ChatBad;
                case "reply": return ChatReply;
                case "replybad": return ChatReplyBad;
                case "wanted": return Wanted;
                case "buy": return Buy;
                default: return null;
            }
        }

        /// One play request, nothing chosen for you: take 0 = random, 1..N = that recording; voice null = the
        /// ped's own. For the voice probe.
        public static unsafe bool Play(int ped, string line, int take, uint paramHash, string voice, int listener)
        {
            if (ped == 0 || !ENTITY.DOES_ENTITY_EXIST(ped))
                return false;
            ulong* p = stackalloc ulong[8];
            for (int i = 0; i < 8; i++)
                p[i] = 0;
            p[0] = Draw.CString(line);
            p[1] = voice != null ? Draw.CString(voice) : 0;
            p[2] = (ulong)take;
            p[3] = paramHash;
            p[4] = listener != 0 && ENTITY.DOES_ENTITY_EXIST(listener) ? (uint)listener : 0u;
            p[6] = 1;
            p[7] = 1;
            return Function.Call<bool>(PlayAmbientSpeech, ped, p);
        }

        public static unsafe bool Say(int ped, string line, int listener)
        {
            if (ped == 0 || string.IsNullOrEmpty(line) || !ENTITY.DOES_ENTITY_EXIST(ped) || ENTITY.IS_ENTITY_DEAD(ped))
                return false;
            ulong* p = stackalloc ulong[8];
            foreach (string prm in Params)
            {
                for (int variation = 0; variation < 2; variation++)
                {
                    for (int i = 0; i < 8; i++)
                        p[i] = 0;
                    p[0] = Draw.CString(line);
                    p[2] = (ulong)variation;   // 0 = a random take of this line
                    p[3] = Natives.Hash(prm);
                    p[4] = listener != 0 && ENTITY.DOES_ENTITY_EXIST(listener) ? (uint)listener : 0u;
                    p[6] = 1;
                    p[7] = 1;
                    if (Function.Call<bool>(PlayAmbientSpeech, ped, p))
                        return true;
                }
            }
            return false;
        }

        /// Plays a line from the set that this ped's voice has. The best few fits take turns, so the same
        /// person doesn't answer everything with the same words.
        public static bool SayAny(int ped, string[] set, int listener = 0)
        {
            if (set == null || set.Length == 0 || ped == 0 || !ENTITY.DOES_ENTITY_EXIST(ped) || ENTITY.IS_ENTITY_DEAD(ped))
                return false;
            uint model = ENTITY.GET_ENTITY_MODEL(ped);
            int now = Game.GameTime;
            int head = System.Math.Min(3, set.Length);
            int offset = (_turn++ & 0x7fffffff) % head;
            List<string> order = new List<string>(set.Length);
            for (int i = 0; i < head; i++)
                order.Add(set[(offset + i) % head]);
            for (int i = head; i < set.Length; i++)
                order.Add(set[i]);
            // lines known to work for this voice first, then the untried ones
            foreach (string line in order)
                if (Worked.Contains(model + "|" + line) && Say(ped, line, listener))
                    return true;
            foreach (string line in order)
            {
                string key = model + "|" + line;
                int failedAt;
                if (Worked.Contains(key) || (Failed.TryGetValue(key, out failedAt) && now - failedAt < RetryFailedMs && now >= failedAt))
                    continue;
                if (Say(ped, line, listener))
                {
                    Worked.Add(key);
                    Failed.Remove(key);
                    return true;
                }
                Failed[key] = now;
            }
            return false;
        }

        public static bool SayTag(int ped, string tag, int listener = 0)
        {
            string[] set = Set(tag);
            return set != null && SayAny(ped, set, listener);
        }

        // ---- known voices: a chosen take, and its words if someone wrote them down (see Core.VoiceBook) ----

        public static Core.VoiceBook Book;
        private static readonly Dictionary<uint, string> BankByModel = new Dictionary<uint, string>();

        /// The voice bank this ped certainly speaks with (its model has only one), or null.
        public static string BankOf(int ped)
        {
            if (Book == null || Book.BankCount == 0 || ped == 0 || !ENTITY.DOES_ENTITY_EXIST(ped))
                return null;
            uint model = ENTITY.GET_ENTITY_MODEL(ped);
            string bank;
            if (!BankByModel.TryGetValue(model, out bank))
                BankByModel[model] = bank = Book.SoleBank((NemesisSystem.Models.Name(model) ?? string.Empty).ToUpperInvariant());
            return bank;
        }

        /// Plays a recording from the set. When the ped's voice is known it's a chosen take, and `words` is
        /// what that take says if someone has written it down (else null).
        public static bool SayWords(int ped, string[] set, int listener, out string words)
        {
            words = null;
            string bank = BankOf(ped), line;
            int take;
            if (bank != null && Book.Pick(bank, set, _turn++, out line, out take, out words))
            {
                if (Play(ped, line, take, Natives.Hash(Params[0]), null, listener))
                    return true;
                words = null;
            }
            return SayAny(ped, set, listener);
        }

        /// A voice to play later, when its subtitle appears (Feed.Say's onShow); it returns the words actually
        /// spoken when they're known. Null when there's nothing to play. Skipped if the ped is gone or already
        /// speaking by then.
        public static System.Func<string> When(int ped, string tag, int listener = 0)
        {
            string[] set = Set(tag);
            if (ped == 0 || set == null)
                return null;
            return () =>
            {
                string words = null;
                if (!Busy(ped))
                    SayWords(ped, set, listener, out words);
                return words;
            };
        }

        /// A voice for a line the add-on wrote: the tone is read from the words and the speaker's feelings.
        public static System.Func<string> For(int ped, string line, int listener = 0, int hate = 0, int fear = 0, bool friendly = false, string fallback = null)
        {
            return When(ped, Core.Tone.Of(line, hate, fear, friendly) ?? fallback, listener);
        }

        public static bool Busy(int ped)
        {
            return ped != 0 && ENTITY.DOES_ENTITY_EXIST(ped) && AUDIO.IS_ANY_SPEECH_PLAYING(ped);
        }
    }
}
