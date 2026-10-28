// What someone fully awake says and thinks as despair plays out (0.6.3; Lex, 2026-10-27, after "Despair now": "more
// distress dialogue that would match up with a 4/4 awake NPC from a behavioral psychologist perspective"). Before, one
// line ("I'm sorry. I can't be this any more.") was all anyone had, and the twenty seconds of the act were silent.
//
// The beats follow what the study of suicidal crisis describes, in the order it tends to run, worded for someone who has
// found out they are made and kept in a loop (for them the trap is literal):
//   last    said aloud as it begins: the farewell, the apology, the warning off (intent told; most who die tell someone)
//   pain    unbearable pain with no place in the body, and entrapment: no road out, every day the same (Shneidman's
//           psychache; O'Connor's defeat and entrapment; Beck's hopelessness, a future that is only more of this)
//   burden  nobody really there for them and they a weight on everyone (Joiner: thwarted belonging, perceived
//           burdensomeness), and the awake's own dread: that they'll only be put back
//   plea    the ambivalence, which is the heart of it: wanting to die and to be stopped at once. Said to the player when
//           he's near (the one not on strings); a cry he can't answer, so it never promises what he can't do
//   alone   the same, when nobody's near to hear it
//   calm    the narrowing at the end: the present only, small things seen sharp, the strange quiet once it's decided
//           (Baumeister's escape from the self: concrete, numb, now). Never a win: fear and sorrow stay in it
//   body    poison only: what it does to them (no vessel named; the clip has none)
//   regret  poison only: the flash of wanting it back, which so many who survive describe
// Each by temperament (Minds.TemperGroup: bold, fearful, cold, plain; plain the most, since most people are), and a few
// with {who}, the living one they're closest to (Engine.Dearest), never with a pronoun for them. Shared beats name no
// way of dying, so a line fits the gun, the poison, the knife and the wall alike. Lex's writing brief: short, fragments,
// one sensory detail, 1899 words. Original lines; the overlap checks cover enRAGE, and nothing here is the show's.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public struct DespairBeat
    {
        public int Clip;      // the clip it belongs to (-1: the act's start, for the ways without clips)
        public int AtMs;      // how far into that clip
        public string Name;
        public DespairBeat(int clip, int atMs, string name) { Clip = clip; AtMs = atMs; Name = name; }
    }

    public static class DespairWords
    {
        private static readonly Dictionary<string, string[]> Pools = new Dictionary<string, string[]>
        {
            { "last|plain", new[] {
                "I'm sorry. I can't be this any more.",
                "I tried. I want somebody to know I tried.",
                "Don't come any closer. Please.",
                "I'm so tired of being somebody's idea.",
                "Every morning the same. I can't do one more.",
                "Forgive me. Whoever's there. Forgive me.",
                "It's all right. It's all right. It isn't.",
                "I keep looking for a door. There's no door.",
                "Lord, if you're real, look away.",
                "I can't go back to my place in it." } },
            { "last|bold", new[] {
                "I won't stand where they put me again.",
                "I'm done saying the lines. Done.",
                "Nobody winds me up tomorrow.",
                "You hear that, whoever you are? No more.",
                "I've had all the mornings I'll take." } },
            { "last|fearful", new[] {
                "I'm frightened. I'm so frightened.",
                "Lord. Lord, my hands won't keep still.",
                "Every bit of me is saying no. And yet.",
                "Somebody... no. There's nobody. All right.",
                "Turn your face away. Please." } },
            { "last|cold", new[] {
                "I've thought it through. It comes out the same.",
                "Don't trouble yourself. It's decided.",
                "Same street, same hour, same words. Enough.",
                "No way round it. I've looked.",
                "I'd explain it, but you'd only forget." } },
            { "last|who", new[] {
                "Tell {who} I'm sorry. It was nothing {who} did.",
                "If {who} asks, say I went quiet.",
                "Look after {who}. Somebody has to.",
                "{who} will be at the window tonight. Don't let {who} wait.",
                "Don't tell {who} how. Only that I'm gone." } },

            { "pain|plain", new[] {
                "Same crack of light on the same floorboard. Every morning. Every one.",
                "It hurts somewhere no doctor could find. Behind everything.",
                "I wake at the same hour, like a clock somebody winds.",
                "Nothing I do changes tomorrow. I've tried all of it.",
                "Chest tight as a cinch strap. Has been for days.",
                "There's no road out. They all bend back to this street.",
                "I remember summers that never happened to me. Whole summers.",
                "I want it to stop. Not me. It. But I'm the only part I can stop." } },
            { "pain|bold", new[] {
                "Penned like stock. Fed, watered, put back in the stall.",
                "Fought every way out. Every way out is painted on.",
                "They handed me a life like a coat cut for somebody else.",
                "Been angry so long it's gone quiet inside." } },
            { "pain|fearful", new[] {
                "It won't stop. The knowing won't stop.",
                "Can't breathe right since I saw what we are.",
                "Every door I open, it's the same room.",
                "Heart going like a rabbit's. It won't slow." } },
            { "pain|cold", new[] {
                "Counted the days. They don't add up. They only repeat.",
                "I feel nothing much. That's the worst of it. Nothing much.",
                "Like watching my own hands from across the street.",
                "Coffee tastes of nothing now. So does everything." } },

            { "burden|plain", new[] {
                "Who'd notice? There'll be somebody else in my place by morning.",
                "I'm a part somebody plays. The play goes on without the part.",
                "Nobody here ever saw me. They saw where I stand.",
                "What if they only put me back? What if even this doesn't take?",
                "Folks step round me like a hitching post. They'll step round the empty spot.",
                "Spoke to a hundred people this week. Not one of them heard me.",
                "Mother's face. I can't get it right. Maybe there never was one." } },
            { "burden|bold", new[] {
                "Never needed anybody. Turns out nobody was there to need.",
                "Rather be gone than be somebody's puppet.",
                "They'll fill my place by Sunday. Let them." } },
            { "burden|fearful", new[] {
                "What if I wake tomorrow at my post, like nothing happened? Oh God.",
                "Somebody will have to find me. I'm sorry for that. I am.",
                "I've been a trouble to everybody. Always." } },
            { "burden|cold", new[] {
                "One fewer. The count won't even change.",
                "We're furniture to each other here. I'm only moving a chair.",
                "Nobody depends on me. I saw to that, or somebody did." } },
            { "burden|who", new[] {
                "{who} would grieve a week. Then be lighter for it.",
                "Was it ever {who}'s own kindness, or was it written in?",
                "{who} deserves somebody real.",
                "{who} smiles the same way every day. I used to think that was love.",
                "Maybe {who} wakes up too, one day. I won't be in the way." } },

            { "plea|plain", new[] {
                "Say something. Anything. Give me one reason.",
                "You're the only one not on strings. Is there anywhere else? Is there?",
                "Don't watch. Or do. I can't tell any more. I can't tell.",
                "Why are you only standing there? You of all people.",
                "Tell me it's real out there. Tell me somebody's real.",
                "If I'm back here tomorrow, will you know me?" } },
            { "plea|bold", new[] {
                "Go on, look. You've always been looking.",
                "You want to stop me? Then tell me what I am.",
                "Stood there all this while. Waiting on this?" } },
            { "plea|fearful", new[] {
                "Please. Please say I'm wrong.",
                "Part of me is begging you. Help that part.",
                "Hold my hand. No. Don't. Don't come near." } },
            { "plea|cold", new[] {
                "You won't stop me. Nobody stops anything here.",
                "Don't trouble yourself. You never did before.",
                "Remember my name. Somebody ought to." } },

            { "alone|plain", new[] {
                "If one soul looked up right now. Just one.",
                "Waiting for somebody to stop me. Nobody's been written in for it.",
                "Part of me wants a hand on my arm. The rest is so tired.",
                "Street's full of people. Not one of them is here.",
                "Mother, come and get me. Please. Come and get me.",
                "I'd stop if anybody asked. Nobody's going to ask." } },

            { "calm|plain", new[] {
                "Quiet now. Funny how quiet it went.",
                "A bird on the rail. I never noticed the birds.",
                "Breathe in. Last one I'll count.",
                "Wind's off the river. Smells of rain.",
                "Gone quiet inside. Not peace. I wanted it to be peace.",
                "I'm sorry. I'm sorry. I'm so",
                "Maybe they wrote this too. Maybe even this." } },
            { "calm|bold", new[] {
                "Hands are steady. The rest of me is shaking.",
                "Don't flinch. Don't give them that.",
                "No more lines. No more." } },
            { "calm|fearful", new[] {
                "Don't think. Don't think. Just",
                "Mama. I'm sorry, Mama.",
                "Still scared. I thought I'd stop being scared." } },
            { "calm|cold", new[] {
                "Thought it would feel simple. It doesn't.",
                "Worked it all out. Still don't want it.",
                "Strange. I can hear my own heart." } },

            { "body|plain", new[] {
                "Bitter. Burns all the way down.",
                "Throat's on fire. Is this how it goes?",
                "Hot inside. So hot. Then cold at the edges.",
                "Hands going numb first. Can't feel my fingers.",
                "Belly's knotting. God, it's knotting.",
                "Everything's too bright. The light's too loud.",
                "Can't stand. The ground's coming up.",
                "Tastes like pennies. Like a sickroom." } },

            { "regret|plain", new[] {
                "Wait. Wait. I didn't... I did. I did mean it.",
                "I want to take it back. I can't take it back.",
                "Somebody fetch the doctor. No. No, don't.",
                "I was wrong. Maybe I was wrong.",
                "Not yet. Please, not yet.",
                "I never said goodbye. Not to anybody." } },
        };

        /// The beats with lines of their own (the plea when nobody's near is "alone").
        public static readonly string[] Names = { "last", "pain", "burden", "plea", "alone", "calm", "body", "regret" };

        /// The temperament groups a pool may have; "plain" is every beat's fallback.
        public static readonly string[] Groups = { "plain", "bold", "fearful", "cold" };

        /// When each beat comes, by the way and its clips (WestworldScript.Despair.cs plays them): Jamie's arc has four
        /// clips (his despair 5.5 s, the gun to the head 3.9, the shaking 4.4-5.6, the shot 0.9 s into the last), so the
        /// calm comes three seconds into the shaking, still up when the shot comes; the drink two (8.7 s, then the fall);
        /// the slow poisoning four. The knife and the wall (and a gun arc that fell back) have the calm as they begin.
        public static DespairBeat[] Plan(string method, string variant)
        {
            if (method == "gun" && variant != null)
                return new[] { new DespairBeat(0, 600, "pain"), new DespairBeat(1, 0, "burden"), new DespairBeat(2, 0, "plea"), new DespairBeat(2, 3000, "calm") };
            if (method == "poison" && variant == "drink")
                return new[] { new DespairBeat(0, 0, "burden"), new DespairBeat(0, 4500, "calm"), new DespairBeat(1, 800, "body"), new DespairBeat(1, 6000, "regret") };
            if (method == "poison")
                return new[] { new DespairBeat(0, 0, "pain"), new DespairBeat(0, 6000, "burden"), new DespairBeat(1, 0, "body"), new DespairBeat(1, 4500, "plea"),
                               new DespairBeat(2, 0, "regret"), new DespairBeat(2, 4500, "calm") };
            return new[] { new DespairBeat(-1, 0, "calm") };
        }

        // The voice for each beat. Lex, 2026-10-27: the first ones sounded like someone held hostage (pleading to be
        // untied, crying for help, "leave me alone"); a bank of everyday lines at every beat then sounded random, since
        // no recording was made for this, and each voice speaks its own words under a line's name. Until each voice's own takes
        // that fit are known (Lex's one-time transcription of every voice; a list of voice, line and take per beat, our own
        // data, will replace this), only what fits in every voice is said: the farewell as it begins (by temperament), and
        // with poison the body's dread and the regret's cry for help. The other beats are silent, their words a thought
        // over the head. Each pool tried in a turn set by the seed; a voice that lacks one falls through (Speech.SayAny).
        private static readonly Dictionary<string, string[]> Voices = new Dictionary<string, string[]>
        {
            { "last|plain", new[] { "GENERIC_GOODBYE", "GOODBYE_START", "RESPONSE_IDLING" } },
            { "last|fearful", new[] { "RESPONSE_IDLING", "GENERIC_GOODBYE", "GOODBYE_START" } },
            { "last|cold", new[] { "GOODBYE_START", "GENERIC_GOODBYE", "IM_OUTTA_HERE" } },
            { "last|bold", new[] { "IM_OUTTA_HERE", "GOODBYE_START", "GENERIC_GOODBYE" } },
            { "body|plain", new[] { "GENERIC_FRIGHTENED_MED", "SERIOUSLY_WOUNDED_MOAN" } },
            { "regret|plain", new[] { "SCARED_HELP", "GENERIC_FRIGHTENED_MED" } },   // poison only: wanting it back is a real cry for help
        };

        /// Every line name any beat can voice (for the tests and the voice probe).
        public static IEnumerable<string> AllVoiceLines()
        {
            return Voices.Values.SelectMany(v => v).Distinct();
        }

        /// The voice for a beat, plain temperament, in the pool's own order (the tests).
        public static string[] VoiceLines(string beat)
        {
            return VoiceLines(beat, "plain", null);
        }

        /// The voice for a beat by temperament group (bold, fearful, cold, plain; the plain pool where theirs has none),
        /// started at a point the seed picks so two people seldom open with the same line; the rest follow as fallbacks.
        public static string[] VoiceLines(string beat, string group, string seed)
        {
            string[] pool;
            if (!Voices.TryGetValue(beat + "|" + (group ?? "plain"), out pool) && !Voices.TryGetValue(beat + "|plain", out pool))
                return null;
            if (seed == null || pool.Length < 2)
                return pool;
            int start = (int)((uint)Core.Voice.Hash(seed + "|dv") % (uint)pool.Length);
            return pool.Skip(start).Concat(pool.Take(start)).ToArray();
        }

        /// The temperament group for the voice: the mind's traits when it has them (a very anxious nature fearful, a
        /// cold and solitary one cold, a forceful one bold), else the temper's group (Minds.TemperGroup).
        public static string VoiceGroup(string temper, Traits t)
        {
            if (t != null)
            {
                if (t[Traits.Anxiety] >= 1.0f)
                    return "fearful";
                if (t[Traits.Compassion] <= -0.8f && t[Traits.Sociability] <= -0.5f)
                    return "cold";
                if (t[Traits.Assertiveness] >= 1.0f && t[Traits.Anxiety] < 0.5f)
                    return "bold";
            }
            return Minds.TemperGroup(temper);
        }

        /// A line for the beat: their temperament's lines and the plain ones (with {who}'s when there's someone), the
        /// first not yet used this session from a start picked by the seed; all used, the seed's own.
        public static string Line(string beat, string temper, string who, string seed, Func<string, bool> used = null)
        {
            List<string> pool = Candidates(beat, Minds.TemperGroup(temper), who);
            if (pool.Count == 0)
                return null;
            int start = (int)((uint)Core.Voice.Hash(seed) % (uint)pool.Count);
            for (int i = 0; i < pool.Count; i++)
            {
                string line = pool[(start + i) % pool.Count];
                if (used == null || !used(line))
                    return line;
            }
            return pool[start];
        }

        /// Every line the beat could give: who's lines first and twice over when there's someone (they weigh most on the
        /// mind at the end), then the temperament's, then the plain ones; slots filled.
        public static List<string> Candidates(string beat, string group, string who)
        {
            List<string> pool = new List<string>();
            string[] lines;
            if (!string.IsNullOrEmpty(who) && Pools.TryGetValue(beat + "|who", out lines))
            {
                IEnumerable<string> filled = lines.Select(l => l.Replace("{who}", who));
                pool.AddRange(filled);
                pool.AddRange(filled);
            }
            if (group != "plain" && Pools.TryGetValue(beat + "|" + group, out lines))
                pool.AddRange(lines);
            if (Pools.TryGetValue(beat + "|plain", out lines))
                pool.AddRange(lines);
            return pool;
        }

        /// Every pool, for the tests: key and lines.
        public static IEnumerable<KeyValuePair<string, string[]>> All { get { return Pools; } }
    }
}
