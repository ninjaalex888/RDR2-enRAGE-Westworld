// Conversations in their real voices (roadmap 1): two people near you trade the game's own paired lines
// (Core/RealTalk.cs), each line played by name in the speaker's own voice, the answer only once the first has really
// stopped speaking. Every RealVoiceTalkSeconds or so, two people already standing together (within 4 m), at least
// one of them in plain view, have a word: about the weather when it's worth one, how the other is getting on, a piece
// of news, or a hello; and when the moment is right, about the riders going by (at once, not waiting), a story about
// the town, a sweetheart's compliment, or a crude remark (a saloon of an evening, or sweethearts at night; CrudeTalk).
// Each rests five minutes afterwards and the pair fifteen. A test key (Shift+F7) does the same
// at once for the two closest to each other, the second walking over if they're apart. Nothing is drawn as they
// talk (RealNPCs draws no subtitles; the game shows its own where it does).
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
        private const float TalkApart = 4f;          // two people this close stand together; further, the talk ends
        private const float TalkReach = 20f;         // the test fetches the second one from up to this far
        private const float TalkNear = 15f;          // only people this close to you talk (further off, their voices are lost)
        private const int TalkWalkMs = 15000;        // longest the walk over may take
        private const int TalkWaitMs = 8000;         // longest a line may take before the other answers anyway
        private const int TalkPersonRestMs = 300000, TalkPairRestMs = 900000;
        private const int TalkLookEveryMs = 4000;    // how often to look for two people to talk, once it's time
        private const int TalkHoldMs = 9000;         // how long one hold keeps them facing each other (renewed each line)
        private const float TalkStillSpeed = 0.6f;   // slower than this (m/s) counts as standing, for the automatic talks
        private const float RidersNear = 40f;        // riders this close to you are going by
        private const float RidersTogether = 12f;    // ...and riding together: three or more this close to one another
        private const int RiderTalkRestMs = 180000;  // after remarking on riders, the next such remark waits this long
        // The game sometimes turns a line down for a moment though the voice has it (seen in a busy saloon: the same pair
        // refused, then played 7 s later; one saloon girl refused a greeting her sisters' voice played): try again.
        private const int TalkRetries = 4;           // tries after the first, before giving up on a line
        private const int TalkRetryMs = 750;         // between tries

        private sealed class VoiceTalk
        {
            public Ped[] People;
            public string[] Names;
            public TalkTurn[] Turns;
            public int Turn, At, SaidAt;
            public bool Speaking;               // a line has started and we're waiting for it to end
            public int WalkUntil;               // the second one is walking over (0: they're together)
            public Vector3 WalkFrom;            // where the walker set off, for the log if they never arrive
            public List<string> Said = new List<string>();
            public string Last;                 // the line played before this turn (a reaction answers its letter)
            public string Moment = "";          // what the moment held, for the log ("riders going by, saloon, 22:00")
            public bool Opened;                 // the first line played: from then on there's no trying another kind
            public bool Test;                   // started with the test key
            public bool Riders;                 // riders were going by when it started
            public int Tries;                   // times the game has turned down the line due now
        }

        private VoiceTalk _talk;
        private bool _talkTestRequested;
        private int _nextTalk, _nextRiderLook, _nextRiderTalk;
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
                        TalkEnd(t, "stopped: they never got close enough to talk (still " + (int)t.People[0].Position.DistanceTo(t.People[1].Position) + " m apart; " + t.Names[1] +
                                   " moved " + (int)t.People[1].Position.DistanceTo(t.WalkFrom) + " m" + (PED.IS_PED_USING_ANY_SCENARIO(t.People[1].Handle) ? ", in a scenario" : "") + ")", now);
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
                    if (TalkTryAgain(t, now))
                        return;
                    TalkEnd(t, "stopped: neither voice would play an opening line that fits, " + (TalkRetries + 1) + " tries" + TalkWhoSpoke(t), now);
                    return;
                }
                me = t.People[turn.Who];
            }
            else
            {
                string[] lines = RealTalk.LinesFor(turn, t.Last);
                if (!Speech.PlayFirst(me.Handle, lines, them.Handle, out line, out take, out words))
                {
                    if (TalkTryAgain(t, now))
                        return;
                    TalkEnd(t, "stopped: " + t.Names[turn.Who] + "'s voice wouldn't play any of " + string.Join(", ", lines) + ", " + (TalkRetries + 1) + " tries" + TalkWhoSpoke(t), now);
                    return;
                }
            }
            int tries = t.Tries;
            t.Tries = 0;
            t.Last = line;
            TalkFace(t.People[0], t.People[1]);   // keep them facing each other, and standing, through the answer
            Feed.Overheard(t.Names[turn.Who], words, RealTalk.Caption(line), me.Handle);
            t.Said.Add(t.Names[turn.Who] + ": " + line + (take > 0 ? " take " + take : " (take chosen by the game)") +
                       (words != null ? " \"" + words + "\"" : "") + (tries > 0 ? " (on try " + (tries + 1) + ")" : ""));
            t.SaidAt = now;
            t.Speaking = true;
            t.At = now + 300;   // the voice takes a moment to register as playing
        }

        /// The game turned the line down: wait a moment and try again, a few times, holding them meanwhile.
        private bool TalkTryAgain(VoiceTalk t, int now)
        {
            if (t.Tries >= TalkRetries)
                return false;
            if (t.Tries++ == 0)
                Log.Write("TALK " + t.Names[0] + " & " + t.Names[1] + ": the game turned down the " + (t.Opened ? "answer" : "opening") + TalkWhoSpoke(t) + "; trying again");
            TalkFace(t.People[0], t.People[1]);
            t.At = now + TalkRetryMs;
            return true;
        }

        /// Who of the two was already speaking a line of their own, for the log ("" if neither).
        private static string TalkWhoSpoke(VoiceTalk t)
        {
            var busy = Enumerable.Range(0, 2).Where(i => t.People[i] != null && t.People[i].Exists() && Speech.Busy(t.People[i].Handle)).Select(i => t.Names[i]).ToList();
            return busy.Count == 0 ? "" : " (" + string.Join(" and ", busy) + (busy.Count > 1 ? " were" : " was") + " speaking)";
        }

        /// Plays the first opening line either of them has: each kind that fits the weather and the hour, with one and
        /// then the other starting. The conversation takes the kind and the order that worked.
        private bool TalkOpen(VoiceTalk t, out TalkTurn turn, out string line, out int take, out string words)
        {
            TalkMoment moment = new TalkMoment
            {
                Weather = RealTalk.WeatherKind(MISC._GET_PREV_WEATHER_TYPE_HASH_NAME()),
                Hour = CLOCK.GET_CLOCK_HOURS(),
                Saloon = _placeKind == "saloon",
                Sweethearts = TalkSweethearts(t.People[0], t.People[1]),
                Riders = t.Riders,
                Stories = RealTalk.StoryCodes(_placeTown),
                Crude = _cfg.CrudeTalk,
                Sure = t.Test
            };
            t.Moment = string.Join(", ", new[] { moment.Riders ? "riders going by" : null, moment.Saloon ? "saloon" : null, moment.Sweethearts ? "sweethearts" : null,
                moment.Stories.Length > 0 ? "town " + string.Join("/", moment.Stories) : null, moment.Weather != null ? moment.Weather + " weather" : null, moment.Hour + ":00" }.Where(s => s != null));
            foreach (TalkTurn[] kind in RealTalk.KindsFor(moment, t.People[0].Handle + "|" + _now / 60000))
                foreach (bool swap in new[] { false, true })
                {
                    if (swap)
                    {
                        Array.Reverse(t.People);
                        Array.Reverse(t.Names);
                    }
                    Ped me = t.People[0], them = t.People[1];
                    if (!RealTalk.Fits(kind, !PED.IS_PED_MALE(me.Handle), !PED.IS_PED_MALE(them.Handle)))
                        continue;
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
            Log.Write("TALK " + t.Names[0] + " & " + t.Names[1] + " " + how + (t.Moment.Length > 0 ? " (" + t.Moment + ")" : "") + ": " + (t.Said.Count > 0 ? string.Join(" | ", t.Said) : "nothing said"));
            foreach (Ped p in t.People)
                if (p != null)
                    _talkedAt[p.Handle] = now;
            if (t.People[0] != null && t.People[1] != null)
                _pairTalkedAt[TalkPairKey(t.People[0], t.People[1])] = now;
            // a word exchanged is remembered between them, as any small talk is
            if (how == "over" && t.People.All(p => p != null && p.Exists()))
            {
                NoteSceneTie("smalltalk", t.People.ToList());
                NoteMinds("smalltalk", t.People.ToList(), null);   // the missing braces (night 2026-10-28): a talk cut short was remembered as small talk had
            }
            if (_talkedAt.Count > 200)
                foreach (int h in _talkedAt.Where(kv => now - kv.Value > TalkPersonRestMs || now < kv.Value).Select(kv => kv.Key).ToList())
                    _talkedAt.Remove(h);
            if (_talk == t)
                _talk = null;
        }

        /// The two have a romance between them: a warm romantic tie, or one is courting the other now (a current link).
        private bool TalkSweethearts(Ped a, Ped b)
        {
            Someone x = SomeoneOf(a), y = SomeoneOf(b);
            if (x == null || y == null)
                return false;
            SceneTie tie = _store.Ties.FirstOrDefault(s => s.Has(x.Id) && s.Has(y.Id));
            if (tie != null && tie.Warmth > 0 && Ties.Sweethearts(tie))
                return true;
            long day = Bridge.Now() / 1440;
            return _store.Links.Any(l => l.Kind == "courting" && ((l.Id == x.Id && l.OtherId == y.Id) || (l.Id == y.Id && l.OtherId == x.Id)) && InnerLife.ArcFor(l.Id, day) == l.Arc);
        }

        /// A band of riders going by: three or more people on horseback, moving, riding together (within RidersTogether
        /// of one another), within RidersNear of you. Two farmers trotting past aren't a band, and the line is about a band.
        private static bool RidersPassing(Ped me)
        {
            var riders = new List<Vector3>();
            foreach (Ped p in World.GetAllPeds())
            {
                if (p == null || !p.Exists() || p.Handle == me.Handle || ENTITY.IS_ENTITY_DEAD(p.Handle) || !PED.IS_PED_HUMAN(p.Handle) || !PED.IS_PED_ON_MOUNT(p.Handle))
                    continue;
                int horse = PED.GET_MOUNT(p.Handle);
                if (horse != 0 && ENTITY.GET_ENTITY_SPEED(horse) > 1.5f && p.Position.DistanceTo(me.Position) < RidersNear)
                    riders.Add(p.Position);
            }
            return riders.Any(r => riders.Count(o => o.DistanceTo(r) < RidersTogether) >= 3);   // counts the rider itself
        }

        private static string TalkPairKey(Ped a, Ped b)
        {
            return Math.Min(a.Handle, b.Handle) + "|" + Math.Max(a.Handle, b.Handle);
        }

        /// People near you who could talk: ordinary bystanders, not the game's story characters, not in a scene, an act
        /// of waking or one of enRAGE's fights, and not speaking already.
        private List<Ped> TalkCandidates(Ped me)
        {
            return World.GetAllPeds().Where(p => p != null && p.Exists() && p.Position.DistanceTo(me.Position) < TalkNear && TalkLeftOut(p, me) == null).ToList();
        }

        /// Why this person can't be in a conversation now, or null if they can.
        private string TalkLeftOut(Ped p, Ped me)
        {
            int h = p.Handle;
            if (!Natives.Bystander(p, me.Handle, IsRecord(h)))
                return Natives.OtherScripts(h) ? "a mission or mod character" : Natives.IsLaw(p) ? "the law" : PED.IS_PED_IN_COMBAT(h, 0) || PED.IS_PED_FLEEING(h) ? "fighting or fleeing" : "not a bystander";
            if (Spawner.IsOurs(h))
                return "one of RealNPCs' own";
            if (Natives.StoryModel(p))
                return "one of the story's own people";
            if (_director.Involves(h))
                return "in a street scene";
            if (Speech.Busy(h))
                return "speaking";
            if (_wakeActs.Any(w => w.Handle == h))
                return "in a waking act";
            if (IsAcknowledging(h))
                return "greeting someone, or reacting to a sight";
            if (_bridge != null && _bridge.EnrageFighter(h))
                return "one of enRAGE's fighters";
            return null;
        }

        /// For the test key: everyone within reach, nearest first, and why anyone is left out.
        private void TalkTestRoll(Ped me)
        {
            var all = World.GetAllPeds().Where(p => p != null && p.Exists() && p.Handle != me.Handle && PED.IS_PED_HUMAN(p.Handle) && !ENTITY.IS_ENTITY_DEAD(p.Handle) && p.Position.DistanceTo(me.Position) < TalkNear)
                                        .OrderBy(p => p.Position.DistanceTo(me.Position)).Take(10).ToList();
            Log.Write("TALK test: around you: " + (all.Count == 0 ? "nobody" : string.Join("; ", all.Select(p =>
            {
                string why = TalkLeftOut(p, me);
                return TalkName(p) + " (" + (PED.IS_PED_MALE(p.Handle) ? "m" : "f") + ", " + Natives.ModelName(p) + ", " + (int)p.Position.DistanceTo(me.Position) + " m" + (why != null ? ", left out: " + why : "") + ")";
            }))));
        }

        /// Now and then, two people already standing together near you have a word, where you can see it; and when a
        /// band of riders goes by, two of them remark on it straight away.
        private void TalkAuto(int now)
        {
            if (!_cfg.RealVoiceTalk || _director.Busy || Feed.Talking)
                return;
            bool due = now >= _nextTalk;
            if (!due && now < _nextRiderLook)
                return;
            Ped me = Game.Player.Character;
            _nextRiderLook = now + 2000;
            bool riders = now >= _nextRiderTalk && RidersPassing(me);
            if (!due && !riders)
                return;
            if (due)
                _nextTalk = now + TalkLookEveryMs;
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
            TalkBegin(a, b, now, false, riders);
            if (riders)
                _nextRiderTalk = now + RiderTalkRestMs;
            int gap = _cfg.RealVoiceTalkSeconds * 1000;
            int next = now + gap * 7 / 10 + Voice.Hash(a.Handle + "|" + now) % Math.Max(1, gap * 6 / 10);   // 70% to 130% of the gap
            _nextTalk = due ? next : Math.Max(_nextTalk, now + 30000);   // a remark on riders doesn't use up the usual turn
        }

        private static bool TalkSeen(Ped me, Ped p)
        {
            return ENTITY.IS_ENTITY_ON_SCREEN(p.Handle) && ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(me.Handle, p.Handle, 17);
        }

        /// The test key: the pair nearest you, two already standing together first, the second walking over if they're apart.
        /// A test (the Testing page): null when it started, else why not (logged as before).
        private string TalkTestStart(int now)
        {
            if (_talk != null)
            {
                return TestStartRefused("TALK test: ", "TALK test: a conversation is already going on.");
            }
            Ped me = Game.Player.Character;
            List<Ped> near = TalkCandidates(me);
            TalkTestRoll(me);
            int onClock = CLOCK.GET_CLOCK_HOURS();
            bool saloonEvening = _cfg.CrudeTalk && _placeKind == "saloon" && (onClock >= 18 || onClock < 4);
            Ped a = null, b = null;
            float best = float.MaxValue;
            for (int i = 0; i < near.Count; i++)
                for (int j = i + 1; j < near.Count; j++)
                {
                    float apart = near[i].Position.DistanceTo(near[j].Position);
                    if (apart > TalkReach)
                        continue;
                    // a pair already together that the moment has something for comes first: sweethearts, or a man and a woman
                    // in a saloon of an evening (not a pair that has to walk over: in a crowded room the walk often fails)
                    bool suits = apart <= TalkApart && (TalkSweethearts(near[i], near[j]) || (saloonEvening && PED.IS_PED_MALE(near[i].Handle) != PED.IS_PED_MALE(near[j].Handle)));
                    float score = Math.Min(near[i].Position.DistanceTo(me.Position), near[j].Position.DistanceTo(me.Position)) + (apart > TalkApart ? 10f : 0f) - (suits ? 8f : 0f);
                    if (score < best)
                    {
                        best = score;
                        a = near[i];
                        b = near[j];
                    }
                }
            if (a == null)
            {
                return TestStartRefused("TALK test: ", "TALK test: no two people within " + TalkNear + " m of you are within " + TalkReach + " m of one another (" + near.Count + " around).");
            }
            TalkBegin(a, b, now, true, RidersPassing(me));
            return null;
        }

        /// Starts a conversation between two people: they face each other, or (the test only) one walks over first.
        private void TalkBegin(Ped a, Ped b, int now, bool test, bool riders = false)
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
                WalkFrom = b.Position,
                At = now + (apart ? 500 : 900),
                Test = test,
                Riders = riders
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
