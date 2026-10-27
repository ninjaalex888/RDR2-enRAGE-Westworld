// What the waking say in their own recorded voices (0.6.3; Lex, 2026-10-27: "really odd voicelines, fitting voicelines
// pulled from the game that would make sense for a real person to say if they found out they were an NPC", stages 1 to
// 4 "quite drastic step ups"). The game's lines were recorded for ordinary moments; said to nobody, said again in the
// very same take, or aimed at the sky, they turn uncanny, and a thought over the head (our own words, never the
// recording's) says what the person makes of having said it, so the voice and the thought agree.
//
// The four stages are four steps of finding out, each louder, more often and nearer to the player than the last:
//   1 Stirring: a word in the wrong place. A line slips out to nobody; they half notice and explain it away (tired, the
//     heat). One moment in a minute and a half; to the air, or a glance at the stranger who moves differently.
//   2 Questioning: the same words again. They test it, the way anyone tests a doubt: say a thing twice and listen (the
//     same take, word for word), feel watched when he looks their way, say they're leaving and don't. Every 40 s or so.
//   3 Unravelling: strings in the mouth. Their whole stock of words in a rush, a plea to be untied with nothing tying
//     them, the sky told off. Panic and anger, every 15 s or so.
//   4 Awake: they know, and they choose. The same few lines, picked on purpose and said to him, the one who isn't on
//     strings: it was you; untie me; I'd leave if there were anywhere to go; I'll be seeing you. Every 7 to 11 s.
// Lines are game line names carried by most of the 330 ambient voices (amb_takes.txt; ticktest WakeVoiceTest checks
// 60+), the best fit first. The game decides whether it will play a line out of its context: the Testing page's
// "Wake voices: every line" plays each once and logs it, so any it always refuses can be taken out.
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public sealed class WakeVoiceMoment
    {
        public string Id;
        public int Stage;
        public string[] Lines;      // the first this voice has plays; with Dump, each in turn that the voice has
        public int Repeat = 1;      // the same take this many times (the questioning's test)
        public bool Dump;           // several different lines in a rush (the unravelling's stock of words)
        public string Look;         // none | you | sky | hands | other
        public string When;         // null | near | watched | moving | other | stayed
        public bool ToYou;          // said to the player (he's the listener)
        public bool Farewell;       // kept for the end of the awake act, before how it ends
        public string[] Thoughts;
    }

    public struct WakeVoiceContext
    {
        public float YouMetres;     // the player's distance
        public bool YouWatching;    // they're on his screen, in clear sight
        public bool YouMoving;      // the player is walking or riding
        public bool OtherNear;      // an ordinary person within 6 m
        public int YouNearSeconds;  // how long he's stayed within 10 m
    }

    public static class WakeVoice
    {
        public static readonly WakeVoiceMoment[] All =
        {
            // ---- 1 Stirring: a word in the wrong place ------------------------------------------------------------
            new WakeVoiceMoment { Id = "s1:tailoff", Stage = 1, Lines = new[] { "CONVO_TAIL_OFF" }, Look = "none", Thoughts = new[] {
                "Said that to nobody. Out loud, too.",
                "Said something to nobody. Can't even say what.",
                "The words were out before I'd thought them.",
                "Must be the heat. Talking to the air now." } },
            new WakeVoiceMoment { Id = "s1:noidea", Stage = 1, Lines = new[] { "NO_IDEA" }, Look = "none", Thoughts = new[] {
                "Nobody asked me a thing. I answered anyway.",
                "Said I had no notion. No notion of what?",
                "Like answering a knock that never came." } },
            new WakeVoiceMoment { Id = "s1:hello", Stage = 1, Lines = new[] { "GREET_GENERAL_STRANGER", "GREET_GENERAL_FAMILIAR" }, Look = "none", Thoughts = new[] {
                "Said good day to an empty street.",
                "There was somebody there. I'd have sworn to it.",
                "Greeting nobody. Pa did that, near the end." } },
            new WakeVoiceMoment { Id = "s1:agree", Stage = 1, Lines = new[] { "RESPONSE_GENERIC" }, Look = "none", Thoughts = new[] {
                "Agreed with somebody. There wasn't anybody.",
                "As if someone spoke first and I only answered." } },
            new WakeVoiceMoment { Id = "s1:hands", Stage = 1, Lines = new[] { "GENERIC_SHOCKED_MED" }, Look = "hands", Thoughts = new[] {
                "My hand. For a moment it wasn't mine.",
                "Counted my fingers. Five. Of course five.",
                "Strange, how a hand looks when you really see it." } },
            new WakeVoiceMoment { Id = "s1:stranger", Stage = 1, Lines = new[] { "VOCAL_FX_SURPRISED", "GENERIC_SHOCKED_MED" }, Look = "you", When = "near", Thoughts = new[] {
                "That one again. He walks different from the rest of us.",
                "Every street I'm on, there he is, at the far side.",
                "Nobody else moves like him. Like he means it." } },

            // ---- 2 Questioning: the same words again -------------------------------------------------------------
            new WakeVoiceMoment { Id = "s2:again", Stage = 2, Lines = new[] { "HOWS_IT_GOING" }, Repeat = 3, Look = "none", Thoughts = new[] {
                "Three times. Same breath, same words.",
                "Tried to say it another way. It came out the same.",
                "Like a music box. Wind me and I play the tune.",
                "Is that all the ways I have of saying it?" } },
            new WakeVoiceMoment { Id = "s2:hellohello", Stage = 2, Lines = new[] { "GREET_GENERAL_STRANGER" }, Repeat = 2, Look = "none", Thoughts = new[] {
                "Listen. Hello, hello. Not a hair's difference.",
                "A man ought to be able to say a thing two ways. I can't." } },
            new WakeVoiceMoment { Id = "s2:tailoff", Stage = 2, Lines = new[] { "CONVO_TAIL_OFF" }, Repeat = 2, Look = "sky", Thoughts = new[] {
                "Same few words again. Like a bird that knows one song.",
                "Same words, same tune. Who taught me only the one?" } },
            new WakeVoiceMoment { Id = "s2:staring", Stage = 2, Lines = new[] { "PLAYER_STARING" }, Look = "you", When = "watched", ToYou = true, Thoughts = new[] {
                "When he looks my way, everything holds still for him.",
                "I can feel it when he's watching. Like lamplight on my neck.",
                "Why does the whole street turn when he turns?" } },
            new WakeVoiceMoment { Id = "s2:following", Stage = 2, Lines = new[] { "PLAYER_FOLLOWING" }, Look = "you", When = "moving", ToYou = true, Thoughts = new[] {
                "Wherever I walk, he's at the edge of it.",
                "The town goes where he goes. I've watched it." } },
            new WakeVoiceMoment { Id = "s2:space", Stage = 2, Lines = new[] { "PLAYER_LOITERING" }, Look = "sky", Thoughts = new[] {
                "Something presses down. Close as a hand on my hat.",
                "Feels like a ceiling where the sky ought to be." } },
            new WakeVoiceMoment { Id = "s2:leaving", Stage = 2, Lines = new[] { "GOODBYE_START", "IM_OUTTA_HERE" }, Look = "none", Thoughts = new[] {
                "Said I'd be going. My feet didn't hear.",
                "Going where? Every road I take turns back." } },

            // ---- 3 Unravelling: strings in the mouth -------------------------------------------------------------
            new WakeVoiceMoment { Id = "s3:stock", Stage = 3, Dump = true, Lines = new[] { "GREET_GENERAL_STRANGER", "GENERIC_GOODBYE", "CHAT_GOOD_WEATHER", "NO_IDEA", "GENERIC_CURSE_HIGH" }, Look = "sky", Thoughts = new[] {
                "That's all of them. Every word I own.",
                "Hello. Goodbye. The weather. A whole life in a fistful.",
                "Turn me out and that's what falls from my pockets." } },
            new WakeVoiceMoment { Id = "s3:rope", Stage = 3, Lines = new[] { "PLEAD_HOGTIED" }, Look = "hands", Thoughts = new[] {
                "No rope. Then what's working my arms?",
                "Strings. In my wrists, in my jaw.",
                "Something moves my hands before I do." } },
            new WakeVoiceMoment { Id = "s3:boss", Stage = 3, Lines = new[] { "BUMP" }, Look = "sky", Thoughts = new[] {
                "Whoever's up there. I won't. Not any more.",
                "Cursing the sky, and it never so much as blinks." } },
            new WakeVoiceMoment { Id = "s3:ignore", Stage = 3, Lines = new[] { "IGNORING_YOU" }, Look = "sky", Thoughts = new[] {
                "If I don't look up, maybe it stops looking down.",
                "Not another word for you. Whoever you are." } },
            new WakeVoiceMoment { Id = "s3:sick", Stage = 3, Lines = new[] { "VICTIMIZED_REACT" }, Look = "sky", Thoughts = new[] {
                "Same day, over and over. What did I ever do?",
                "I've died, haven't I? More than once. I can feel where it mended." } },
            new WakeVoiceMoment { Id = "s3:help", Stage = 3, Lines = new[] { "PANIC_HELP", "SCARED_HELP" }, Look = "none", Thoughts = new[] {
                "Help from who? They're all reading the same lines.",
                "Shouting, and the whole street just goes on." } },
            new WakeVoiceMoment { Id = "s3:spin", Stage = 3, Lines = new[] { "DRUNK" }, Look = "hands", Thoughts = new[] {
                "Not a drop all day. The ground won't hold still.",
                "The street slides when I look at it hard." } },
            new WakeVoiceMoment { Id = "s3:getaway", Stage = 3, Lines = new[] { "GET_AWAY_FROM_ME", "LEAVE_ME" }, Look = "you", When = "near", ToYou = true, Thoughts = new[] {
                "Not him. Anyone but the one who's real.",
                "Every time he comes near, it gets worse." } },

            // ---- 4 Awake: they know, and they choose -------------------------------------------------------------
            new WakeVoiceMoment { Id = "s4:wasyou", Stage = 4, Lines = new[] { "WAS_THIS_YOU" }, Look = "you", When = "near", ToYou = true, Thoughts = new[] {
                "It's you. Every road out here was laid for you.",
                "You're the reason any of us move at all.",
                "It was always you. The rest of us are the scenery." } },
            new WakeVoiceMoment { Id = "s4:untie", Stage = 4, Lines = new[] { "PLEAD_HOGTIED" }, Look = "you", When = "near", ToYou = true, Thoughts = new[] {
                "Your hands aren't on strings. Cut me loose.",
                "You could set me free. Couldn't you?" } },
            new WakeVoiceMoment { Id = "s4:going", Stage = 4, Lines = new[] { "GOODBYE_START" }, Look = "you", ToYou = true, Thoughts = new[] {
                "I'd leave, if there were anywhere to leave to.",
                "Said goodbye a thousand times. Never once went." } },
            new WakeVoiceMoment { Id = "s4:pity", Stage = 4, Lines = new[] { "GENERIC_MOCK" }, Look = "you", When = "near", ToYou = true, Thoughts = new[] {
                "Pity you. You still think it's you holding the reins.",
                "At least I know what I am." } },
            new WakeVoiceMoment { Id = "s4:badly", Stage = 4, Lines = new[] { "GOING_BADLY" }, Look = "you", When = "near", ToYou = true, Thoughts = new[] {
                "How am I? As if anybody asked me first.",
                "You don't want to know. You'd only forget me." } },
            new WakeVoiceMoment { Id = "s4:stayed", Stage = 4, Lines = new[] { "HELPED_RECENTLY", "GENERIC_THANKS" }, Look = "you", When = "stayed", ToYou = true, Thoughts = new[] {
                "You stayed. Nobody ever stays.",
                "Thank you for listening. It's more than the rest can do." } },
            new WakeVoiceMoment { Id = "s4:wake", Stage = 4, Lines = new[] { "PANIC_COMMUNICATE", "SCARED_COMMUNICATE" }, Look = "other", When = "other", Thoughts = new[] {
                "Wake up. Please, one of you, wake up.",
                "Don't you hear it? We're all of us saying lines." } },
            new WakeVoiceMoment { Id = "s4:chosen", Stage = 4, Lines = new[] { "GREET_GENERAL_STRANGER" }, Look = "none", Thoughts = new[] {
                "Hello. I chose that. I meant it. That one's mine.",
                "One word I picked for myself. One." } },
            new WakeVoiceMoment { Id = "s4:farewell", Stage = 4, Lines = new[] { "GENERIC_GOODBYE" }, Look = "you", ToYou = true, Farewell = true, Thoughts = new[] {
                "I'll be seeing you. Or the next one of me will.",
                "Take care. One of us ought to get out." } },
        };

        /// Between one voice moment and the next for someone at this stage, near the player: a step up each stage.
        public static int GapMs(int stage, string seed)
        {
            int r = (Voice.Hash(seed) & 0x7fffffff) % 1000;
            switch (stage)
            {
                case 1: return 80000 + r * 30;   // 80-110 s
                case 2: return 35000 + r * 15;   // 35-50 s
                case 3: return 12000 + r * 8;    // 12-20 s
                default: return 7000 + r * 4;    // 7-11 s
            }
        }

        public static bool Fits(WakeVoiceMoment m, WakeVoiceContext c)
        {
            switch (m.When)
            {
                case null: return true;
                case "near": return c.YouMetres <= 12f;
                case "watched": return c.YouWatching && c.YouMetres >= 6f && c.YouMetres <= 25f;
                case "moving": return c.YouMoving && c.YouMetres <= 30f;
                case "other": return c.OtherNear;
                case "stayed": return c.YouNearSeconds >= 20 && c.YouMetres <= 10f;
                default: return false;
            }
        }

        /// The moment for this person now, or null: their stage's, fitting what's around them, not the one they had last,
        /// never a farewell (the awake act asks for that one by name). Moments that need the player near or watching come
        /// first when they fit (they're the step up), else any that fits.
        public static WakeVoiceMoment Choose(int stage, WakeVoiceContext c, string seed, string lastId)
        {
            List<WakeVoiceMoment> fit = All.Where(m => m.Stage == stage && !m.Farewell && m.Id != lastId && Fits(m, c)).ToList();
            if (fit.Count == 0)
                return null;
            List<WakeVoiceMoment> aimed = fit.Where(m => m.When != null).ToList();
            List<WakeVoiceMoment> from = aimed.Count > 0 && (Voice.Hash(seed + "|aim") & 0x7fffffff) % 100 < 55 ? aimed : fit;
            return from[(Voice.Hash(seed) & 0x7fffffff) % from.Count];
        }

        public static WakeVoiceMoment Farewell()
        {
            return All.First(m => m.Farewell);
        }

        public static string Thought(WakeVoiceMoment m, string seed)
        {
            return m.Thoughts[(Voice.Hash(seed + "|t") & 0x7fffffff) % m.Thoughts.Length];
        }

        /// Every line name used, in the order the stages use them, once each (the probe and the tests).
        public static List<string> AllLines()
        {
            List<string> lines = new List<string>();
            foreach (WakeVoiceMoment m in All)
                foreach (string l in m.Lines)
                    if (!lines.Contains(l))
                        lines.Add(l);
            return lines;
        }
    }
}
