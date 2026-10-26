// Conversations in their real voices (roadmap 1), first slice: a test key (Shift+F7) has the nearest person and
// the nearest other one within 20 m of them (who walks over first if they aren't together) trade the game's own
// paired lines (Core/RealTalk.cs): one asks
// how the other is getting on, and the other answers with what their voice has. Each line is played by name in the
// speaker's own voice; the second only answers once the first has really stopped speaking. A subtitle appears as a
// line starts: its words when the word list has them (a model with one voice), else a caption if OverheardTalk is
// on. The log says what played, which take and for how long, to prove the game plays these lines on request.
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
        private const int TalkWalkMs = 15000;        // longest the walk over may take
        private const int TalkWaitMs = 8000;          // longest a line may take before the other answers anyway

        private sealed class VoiceTalk
        {
            public Ped[] People;
            public string[] Names;
            public TalkTurn[] Turns;
            public int Turn, At, SaidAt;
            public bool Speaking;               // a line has started and we're waiting for it to end
            public int WalkUntil;               // the second one is walking over (0: they're together)
            public List<string> Said = new List<string>();
        }

        private VoiceTalk _talk;
        private bool _talkTestRequested;
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
            if (t == null || now < t.At)
                return;
            string why = TalkBroken(t);
            if (why != null)
            {
                TalkEnd(t, "stopped: " + why);
                return;
            }
            if (t.WalkUntil > 0)
            {
                // the second one is on the way: nothing is said until they're face to face
                if (t.People[0].Position.DistanceTo(t.People[1].Position) > 2.4f)
                {
                    if (now > t.WalkUntil)
                        TalkEnd(t, "stopped: they never got close enough to talk");
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
                    TalkEnd(t, "over");
                return;
            }
            TalkTurn turn = t.Turns[t.Turn];
            Ped me = t.People[turn.Who], them = t.People[1 - turn.Who];
            string line, words;
            int take;
            if (!Speech.PlayFirst(me.Handle, turn.Lines, them.Handle, out line, out take, out words))
            {
                TalkEnd(t, "stopped: " + t.Names[turn.Who] + "'s voice has none of " + string.Join(", ", turn.Lines));
                return;
            }
            Feed.Overheard(t.Names[turn.Who], words, RealTalk.Caption(line), me.Handle);
            t.Said.Add(t.Names[turn.Who] + ": " + line + (take > 0 ? " take " + take : " (take chosen by the game)") +
                       (words != null ? " \"" + words + "\"" : ""));
            t.SaidAt = now;
            t.Speaking = true;
            t.At = now + 300;   // the voice takes a moment to register as playing
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

        private void TalkEnd(VoiceTalk t, string how)
        {
            Log.Write("TALK " + t.Names[0] + " & " + t.Names[1] + " " + how + ": " + (t.Said.Count > 0 ? string.Join(" | ", t.Said) : "nothing said"));
            if (_talk == t)
                _talk = null;
        }

        /// The test key: the nearest person and the nearest other one standing with them.
        private void TalkTestStart(int now)
        {
            if (_talk != null)
            {
                Log.Write("TALK test: a conversation is already going on.");
                return;
            }
            Ped me = Game.Player.Character;
            List<Ped> near = World.GetAllPeds().Where(p => p != null && p.Exists() && Natives.Bystander(p, me.Handle) && !Spawner.IsOurs(p.Handle) &&
                                                           !_director.Involves(p.Handle) && !Speech.Busy(p.Handle) && p.Position.DistanceTo(me.Position) < 25f)
                                                .OrderBy(p => p.Position.DistanceTo(me.Position)).ToList();
            // the nearest person, and whoever is nearest to them (a voice the word list knows first, to show its words)
            Ped a = near.OrderByDescending(p => Speech.BankOf(p.Handle) != null && near.Count > 1 ? 1 : 0).ThenBy(p => p.Position.DistanceTo(me.Position)).FirstOrDefault();
            Ped b = a == null ? null : near.Where(p => p.Handle != a.Handle && p.Position.DistanceTo(a.Position) <= TalkReach)
                                           .OrderBy(p => p.Position.DistanceTo(a.Position)).FirstOrDefault();
            if (a == null || b == null)
            {
                Log.Write("TALK test: need two people within " + TalkReach + " m of each other near you; " + near.Count + " around.");
                return;
            }
            bool apart = a.Position.DistanceTo(b.Position) > 2.4f;
            if (apart)
            {
                // the second one walks over, the first waits and watches them come
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
                At = now + (apart ? 500 : 900)
            };
            Log.Write("TALK test: " + _talk.Names[0] + " (" + Natives.ModelName(a) + ", voice " + (Speech.BankOf(a.Handle) ?? "not certain") + ") and " +
                      _talk.Names[1] + " (" + Natives.ModelName(b) + ", voice " + (Speech.BankOf(b.Handle) ?? "not certain") + "), " +
                      (int)a.Position.DistanceTo(b.Position) + " m apart" + (apart ? " (the second walks over)" : "") + ", " + (int)a.Position.DistanceTo(me.Position) + " m from you");
        }

        /// The two turn to each other (someone sitting or leaning just looks over).
        private static void TalkFace(Ped a, Ped b)
        {
            foreach (Ped p in new[] { a, b })
            {
                Ped other = p == a ? b : a;
                if (!PED.IS_PED_USING_ANY_SCENARIO(p.Handle))
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(p.Handle, other.Handle, 3000, 0f, 0f, 0f);
                TASK.TASK_LOOK_AT_ENTITY(p.Handle, other.Handle, 15000, 0, 51, 0);
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
