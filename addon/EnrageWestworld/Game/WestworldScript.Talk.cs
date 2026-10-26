// Conversations in their real voices (roadmap 1): two people near you trade the game's own paired lines
// (Core/RealTalk.cs), each line played by name in the speaker's own voice, the answer only once the first has really
// stopped speaking. Every RealVoiceTalkSeconds or so, two people already standing together (within 4 m), at least
// one of them in plain view, have a word: about the weather when it's worth one, how the other is getting on, a piece
// of news, or a hello. Each rests five minutes afterwards and the pair fifteen. A test key (Shift+F7) does the same
// at once for the two closest to each other, the second walking over if they're apart. A subtitle appears as a line
// starts: its words when the word list has them (a model with one voice), else a caption if OverheardTalk is on.
// The log says what played, which take and for how long.
using System;
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;
using System.Windows.Forms;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private static readonly KeySpec TalkTestKey = KeySpec.Parse("Shift+F7", Keys.F7);
        private const float TalkApart = 4f;          // two people this close stand together; further, the talk ends
        private const float TalkReach = 20f;         // the test fetches the second one from up to this far
        private const float TalkNear = 15f;          // only people this close to you talk (further off, their voices are lost)
        private const int TalkWalkMs = 15000;        // longest the walk over may take
        private const int TalkWaitMs = 8000;         // longest a line may take before the other answers anyway
        private const int TalkPersonRestMs = 300000, TalkPairRestMs = 900000;
        private const int TalkLookEveryMs = 4000;    // how often to look for two people to talk, once it's time
        private const int TalkHoldMs = 9000;         // how long one hold keeps them facing each other (renewed each line)
        private const float TalkStillSpeed = 0.6f;   // slower than this (m/s) counts as standing, for the automatic talks

        private sealed class VoiceTalk
        {
            public Ped[] People;
            public string[] Names;
            public TalkTurn[] Turns;
            public int Turn, At, SaidAt;
            public bool Speaking;               // a line has started and we're waiting for it to end
            public int WalkUntil;               // the second one is walking over (0: they're together)
            public List<string> Said = new List<string>();
            public string Last;                 // the line played before this turn (a reaction answers its letter)
            public bool Opened;                 // the first line played: from then on there's no trying another kind
            public bool Test;                   // started with the test key
        }

        private VoiceTalk _talk;
        private bool _talkTestRequested;
        private int _nextTalk;
        private readonly Dictionary<int, int> _talkedAt = new Dictionary<int, int>();
        private readonly Dictionary<string, int> _pairTalkedAt = new Dictionary<string, int>();
        private Action _aTalk;

        private void TalkStep(int now)
        {
            if (_aTalk == null)
                _aTalk = () => TalkTick(_now);
            Safe("real-voice talk", _aTalk);
        }

        private void TalkTick(int now)
        {
            if (_talkTestRequested)
            {
                _talkTestRequested = false;
                TalkTestStart(now);
            }
            VoiceTalk t = _talk;
            if (t == null)
            {
                TalkAuto(now);
                return;
            }
            if (now < t.At)
                return;
            string why = TalkBroken(t);
            if (why != null)
            {
                TalkEnd(t, "stopped: " + why, now);
                return;
            }
            if (t.WalkUntil > 0)
            {
                // the second one is on the way: nothing is said until they're face to face
                if (t.People[0].Position.DistanceTo(t.People[1].Position) > TalkApart - 1f)
                {
                    if (now > t.WalkUntil)
                        TalkEnd(t, "stopped: they never got close enough to talk", now);
                    else
                        t.At = now + 200;
                    return;
                }
                t.WalkUntil = 0;
                TalkFace(t.People[0], t.People[1]);
                t.At = now + 900;   // let them turn to each other first
                return;
            }
            if (t.Speaking)
            {
                // the line that's playing: wait until the voice has stopped (or too long has passed)
                Ped who = t.People[t.Turns[t.Turn].Who];
                if (Speech.Busy(who.Handle) && now - t.SaidAt < TalkWaitMs)
                {
                    t.At = now + 100;
                    return;
                }
                t.Said[t.Said.Count - 1] += ", " + (now - t.SaidAt) / 100 / 10f + " s" + (now - t.SaidAt >= TalkWaitMs ? " (still going)" : "");
                t.Speaking = false;
                t.Turn++;
                t.At = now + 350;   // a breath before the answer
                if (t.Turn >= t.Turns.Length)
                    TalkEnd(t, "over", now);
                return;
            }
            TalkTurn turn = t.Turns[t.Turn];
            Ped me = t.People[turn.Who], them = t.People[1 - turn.Who];
            string line, words;
            int take;
            if (!t.Opened)
            {
                // the opening decides the conversation: try each kind that fits now, with either of them starting
                if (!TalkOpen(t, out turn, out line, out take, out words))
                {
                    TalkEnd(t, "stopped: neither voice has an opening line that fits", now);
                    return;
                }
                me = t.People[turn.Who];
            }
            else
            {
                string[] lines = RealTalk.LinesFor(turn, t.Last);
                if (!Speech.PlayFirst(me.Handle, lines, them.Handle, out line, out take, out words))
                {
                    TalkEnd(t, "stopped: " + t.Names[turn.Who] + "'s voice has none of " + string.Join(", ", lines), now);
                    return;
                }
            }
            t.Last = line;
            TalkFace(t.People[0], t.People[1]);   // keep them facing each other, and standing, through the answer
            Feed.Overheard(t.Names[turn.Who], words, RealTalk.Caption(line), me.Handle);
            t.Said.Add(t.Names[turn.Who] + ": " + line + (take > 0 ? " take " + take : " (take chosen by the game)") +
                       (words != null ? " \"" + words + "\"" : ""));
            t.SaidAt = now;
            t.Speaking = true;
            t.At = now + 300;   // the voice takes a moment to register as playing
        }

        /// Plays the first opening line either of them has: each kind that fits the weather and the hour, with one and
        /// then the other starting. The conversation takes the kind and the order that worked.
        private bool TalkOpen(VoiceTalk t, out TalkTurn turn, out string line, out int take, out string words)
        {
            string weather = RealTalk.WeatherKind(MISC._GET_PREV_WEATHER_TYPE_HASH_NAME());
            foreach (TalkTurn[] kind in RealTalk.KindsFor(weather, CLOCK.GET_CLOCK_HOURS(), t.People[0].Handle + "|" + _now / 60000))
                foreach (bool swap in new[] { false, true })
                {
                    if (swap)
                    {
                        Array.Reverse(t.People);
                        Array.Reverse(t.Names);
                    }
                    Ped me = t.People[0], them = t.People[1];
                    string[] lines = kind == RealTalk.Gossip ? RealTalk.Shuffled(kind[0].Lines, me.Handle + "|" + _now / 60000) : kind[0].Lines;
                    if (Speech.PlayFirst(me.Handle, lines, them.Handle, out line, out take, out words))
                    {
                        t.Turns = kind;
                        t.Opened = true;
                        turn = kind[0];
                        return true;
                    }
                }
            turn = null;
            line = words = null;
            take = 0;
            return false;
        }

        /// Why the two can't go on: gone, dead, apart, fighting or fleeing; null if they can.
        private string TalkBroken(VoiceTalk t)
        {
            foreach (Ped p in t.People)
            {
                bool gone = p == null || !p.Exists() || ENTITY.IS_ENTITY_DEAD(p.Handle);
                if (gone)
                    return "someone is gone";
                if (PED.IS_PED_IN_COMBAT(p.Handle, 0) || PED.IS_PED_FLEEING(p.Handle))
                    return "someone is fighting or running";
            }
            return t.WalkUntil == 0 && t.People[0].Position.DistanceTo(t.People[1].Position) > TalkApart + 1f ? "they drifted apart" : null;
        }

        private void TalkEnd(VoiceTalk t, string how, int now)
        {
            // release them: their own reactions on again, and the hold that kept them facing each other ended
            foreach (Ped p in t.People)
                if (p != null && p.Exists())
                {
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(p.Handle, false);
                    if (!PED.IS_PED_USING_ANY_SCENARIO(p.Handle) && !ENTITY.IS_ENTITY_DEAD(p.Handle))
                        TASK.CLEAR_PED_TASKS(p.Handle, true, false);
                }
            Log.Write("TALK " + t.Names[0] + " & " + t.Names[1] + " " + how + ": " + (t.Said.Count > 0 ? string.Join(" | ", t.Said) : "nothing said"));
            foreach (Ped p in t.People)
                if (p != null)
                    _talkedAt[p.Handle] = now;
            if (t.People[0] != null && t.People[1] != null)
                _pairTalkedAt[TalkPairKey(t.People[0], t.People[1])] = now;
            // a word exchanged is remembered between them, as any small talk is
            if (how == "over" && t.People.All(p => p != null && p.Exists()))
                NoteSceneTie(new Scene { Kind = "smalltalk" }, t.People.ToList());
            if (_talkedAt.Count > 200)
                foreach (int h in _talkedAt.Where(kv => now - kv.Value > TalkPersonRestMs || now < kv.Value).Select(kv => kv.Key).ToList())
                    _talkedAt.Remove(h);
            if (_talk == t)
                _talk = null;
        }

        private static string TalkPairKey(Ped a, Ped b)
        {
            return Math.Min(a.Handle, b.Handle) + "|" + Math.Max(a.Handle, b.Handle);
        }

        /// People near you who could talk: ordinary bystanders, not the game's story characters, not in a scene, an act
        /// of waking or one of enRAGE's fights, and not speaking already.
        private List<Ped> TalkCandidates(Ped me)
        {
            return World.GetAllPeds().Where(p => p != null && p.Exists() && Natives.Bystander(p, me.Handle) && !Spawner.IsOurs(p.Handle) &&
                                                 !Natives.StoryModel(p) && !_director.Involves(p.Handle) && !Speech.Busy(p.Handle) &&
                                                 !_wakeActs.Any(w => w.Handle == p.Handle) && !(_bridge != null && _bridge.EnrageFighter(p.Handle)) &&
                                                 p.Position.DistanceTo(me.Position) < TalkNear).ToList();
        }

        /// Now and then, two people already standing together near you have a word, where you can see it.
        private void TalkAuto(int now)
        {
            if (!_cfg.RealVoiceTalk || now < _nextTalk || _director.Busy || Feed.Talking)
                return;
            _nextTalk = now + TalkLookEveryMs;
            Ped me = Game.Player.Character;
            int rest;
            List<Ped> near = TalkCandidates(me).Where(p => !(_talkedAt.TryGetValue(p.Handle, out rest) && now - rest < TalkPersonRestMs && now >= rest)).ToList();
            Ped a = null, b = null;
            float best = float.MaxValue;
            for (int i = 0; i < near.Count; i++)
                for (int j = i + 1; j < near.Count; j++)
                {
                    Ped x = near[i], y = near[j];
                    if (x.Position.DistanceTo(y.Position) > TalkApart || ENTITY.GET_ENTITY_SPEED(x.Handle) > TalkStillSpeed || ENTITY.GET_ENTITY_SPEED(y.Handle) > TalkStillSpeed)
                        continue;   // not two passers-by crossing: two people standing together
                    if (_pairTalkedAt.TryGetValue(TalkPairKey(x, y), out rest) && now - rest < TalkPairRestMs && now >= rest)
                        continue;
                    float d = Math.Min(x.Position.DistanceTo(me.Position), y.Position.DistanceTo(me.Position));
                    if (d < best && (TalkSeen(me, x) || TalkSeen(me, y)))
                    {
                        best = d;
                        a = x;
                        b = y;
                    }
                }
            if (a == null)
                return;
            TalkBegin(a, b, now, false);
            int gap = _cfg.RealVoiceTalkSeconds * 1000;
            _nextTalk = now + gap * 7 / 10 + Voice.Hash(a.Handle + "|" + now) % Math.Max(1, gap * 6 / 10);   // 70% to 130% of the gap
        }

        private static bool TalkSeen(Ped me, Ped p)
        {
            return ENTITY.IS_ENTITY_ON_SCREEN(p.Handle) && ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(me.Handle, p.Handle, 17);
        }

        /// The test key: the pair nearest you, two already standing together first, the second walking over if they're apart.
        private void TalkTestStart(int now)
        {
            if (_talk != null)
            {
                Log.Write("TALK test: a conversation is already going on.");
                return;
            }
            Ped me = Game.Player.Character;
            List<Ped> near = TalkCandidates(me);
            Ped a = null, b = null;
            float best = float.MaxValue;
            for (int i = 0; i < near.Count; i++)
                for (int j = i + 1; j < near.Count; j++)
                {
                    float apart = near[i].Position.DistanceTo(near[j].Position);
                    if (apart > TalkReach)
                        continue;
                    float score = Math.Min(near[i].Position.DistanceTo(me.Position), near[j].Position.DistanceTo(me.Position)) + (apart > TalkApart ? 10f : 0f);
                    if (score < best)
                    {
                        best = score;
                        a = near[i];
                        b = near[j];
                    }
                }
            if (a == null)
            {
                Log.Write("TALK test: no two people within " + TalkNear + " m of you are within " + TalkReach + " m of one another (" + near.Count + " around).");
                return;
            }
            TalkBegin(a, b, now, true);
        }

        /// Starts a conversation between two people: they face each other, or (the test only) one walks over first.
        private void TalkBegin(Ped a, Ped b, int now, bool test)
        {
            Ped me = Game.Player.Character;
            if (Speech.BankOf(b.Handle) != null && Speech.BankOf(a.Handle) == null)
            {
                Ped s = a;   // a voice the word list may know speaks first, to show its words
                a = b;
                b = s;
            }
            bool apart = a.Position.DistanceTo(b.Position) > TalkApart;   // within 4 m they talk where they stand
            // held for the conversation, as a street scene holds its cast: the game's own reactions don't pull them away
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(a.Handle, true);
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(b.Handle, true);
            if (apart)
            {
                // one walks over, the other waits and watches them come; whoever isn't busy at something walks, and
                // someone leaning or working is let go of it first (else they never set off)
                if (PED.IS_PED_USING_ANY_SCENARIO(b.Handle) && !PED.IS_PED_USING_ANY_SCENARIO(a.Handle))
                {
                    Ped s = a;
                    a = b;
                    b = s;
                }
                if (PED.IS_PED_USING_ANY_SCENARIO(b.Handle))
                    TASK.CLEAR_PED_TASKS(b.Handle, true, false);
                TASK.TASK_GO_TO_ENTITY(b.Handle, a.Handle, TalkWalkMs, 1.4f, 1.3f, 0f, 0);
                TASK.TASK_LOOK_AT_ENTITY(a.Handle, b.Handle, TalkWalkMs, 0, 51, 0);
            }
            else
                TalkFace(a, b);
            _talk = new VoiceTalk
            {
                People = new[] { a, b },
                Names = new[] { TalkName(a), TalkName(b) },
                Turns = RealTalk.HowsItGoing,
                WalkUntil = apart ? now + TalkWalkMs : 0,
                At = now + (apart ? 500 : 900),
                Test = test
            };
            Log.Write("TALK " + (test ? "test" : "begins") + ": " + _talk.Names[0] + " (" + Natives.ModelName(a) + ", voice " + (Speech.BankOf(a.Handle) ?? "not certain") + ") and " +
                      _talk.Names[1] + " (" + Natives.ModelName(b) + ", voice " + (Speech.BankOf(b.Handle) ?? "not certain") + "), " +
                      (int)a.Position.DistanceTo(b.Position) + " m apart" + (apart ? " (the second walks over)" : "") + ", " + (int)a.Position.DistanceTo(me.Position) + " m from you");
        }

        /// The two turn to each other and stay facing, standing, for TalkHoldMs (someone sitting or leaning just looks over).
        private static void TalkFace(Ped a, Ped b)
        {
            foreach (Ped p in new[] { a, b })
            {
                Ped other = p == a ? b : a;
                if (!PED.IS_PED_USING_ANY_SCENARIO(p.Handle))
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(p.Handle, other.Handle, TalkHoldMs, 0f, 0f, 0f);
                TASK.TASK_LOOK_AT_ENTITY(p.Handle, other.Handle, TalkHoldMs, 0, 51, 0);
            }
        }

        /// A speaker's name for the subtitle: enRAGE's or ours, else what they look like.
        private string TalkName(Ped p)
        {
            string full = FullNameOf(p);
            return full != null ? Person.FirstOf(full) : Grammar.UpperFirst(Director.Kind(p, !PED.IS_PED_MALE(p.Handle)));
        }
    }
}
