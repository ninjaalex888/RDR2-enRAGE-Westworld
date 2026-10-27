// A round bought ([Scenes] Kindnesses; the scene is Core/Scenes.Round.cs): of an evening (17:00 to 02:00) in a saloon,
// someone named with cause to be glad stands the house a drink, through the Director: their call, the room cheers (up
// to four within 6 m look over, cheer in their own voice and, if not sat or leaning, raise a hand), one of them thanks
// them. If you're at the bar within 4 m you're stood one too (said to you, subtitled) and they think the better of you
// for drinking it. A tie and a memory with each of the lasting people treated. Once a saloon a game night at most
// (Pacing "round": 25% a look every 30 s, four game hours between rounds anywhere).
using System;
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private const int RoundLookMs = 30000;
        private const float RoundBar = 6f, RoundYou = 4f;

        private sealed class RoundNote
        {
            public Ped Buyer, B;
            public string Name, Seed;
            public Someone Who;
            public bool Test;
        }

        private RoundNote _round;
        private int _nextRound, _rounds, _roundsYou;
        private string _roundLast, _roundWhyNot;

        /// Bad blood with either of the newlyweds: no toast from them.
        private bool AgainstCouple(string id, Wedding w)
        {
            return _store.Ties.Any(t => t.Has(id) && t.Warmth < 0 && (t.Has(w.AId) || (w.BId != null && t.Has(w.BId))));
        }

        /// Someone in the saloon stands the house a drink. True if a scene started.
        private bool RoundScene(Ped me, int now, bool test, out string why)
        {
            why = null;
            if (!_cfg.Kindnesses)
            {
                why = "[Scenes] Kindnesses is off";
                return false;
            }
            if (_placeKind != "saloon")
            {
                why = "not in a saloon";
                return false;
            }
            int clock = CLOCK.GET_CLOCK_HOURS();
            if (!test && (now < _nextRound || !(clock >= 17 || clock < 2)))
                return false;
            _nextRound = now + RoundLookMs;
            long g = _store.LastGameMin;
            string skipped0;
            string pre = Pacing.WhyNot(_store.Pace, "round", _placeTown, null, g, _rng.Next(100), test, out skipped0);   // the rarity first: no scan for nothing
            if (pre != null)
            {
                why = _roundWhyNot = "rests: " + pre;
                return false;
            }
            List<Candidate> people = ReasonCandidates(me, now, test).Where(c => c.Ped.Position.DistanceTo(me.Position) < 15f).ToList();
            Candidate buyer = null;
            string roundWhy = null;
            int rank = 9;
            Wedding wed = _store.Weddings.FirstOrDefault(w => w != null && w.Done && !w.Ruined && w.Town == _placeTown && g - w.At >= 0 && g / 1440 == w.At / 1440
                && _store.Affairs.Any(a => a.AId == w.AId && a.BName == w.BName && a.Active && a.Stage >= 5));   // held, not called off
            string couple = wed != null ? Person.FirstOf(wed.AName) + " and " + Person.FirstOf(wed.BName) : null;   // wed in this town today: a toast
            foreach (Candidate c in people)
            {
                if (c.Who.P == null)
                    continue;
                string wk;
                int ws;
                bool good;
                bool news = InnerLife.WorryNow(c.Who.P, g / 1440, out wk, out ws, out good) && ws >= 3 && good;
                string w = couple != null && c.Who.Temper != "Greedy" && c.Who.Id != wed.AId && c.Who.Id != wed.BId && !AgainstCouple(c.Who.Id, wed) ? "wedding" :   // not the newlyweds themselves
                            Scenes.RoundWhy(news, c.Who.Mood, c.Who.P.Outgoing, c.Who.WellOff, c.Who.Temper) ?? (test ? "glad" : null);
                int r = Array.IndexOf(Scenes.RoundWhys, w);
                if (w == null || r >= rank || !people.Any(o => o != c && o.Ped.Position.DistanceTo(c.Ped.Position) < RoundBar && TieWarmth(o.Who.Id, c.Who) >= 0))
                    continue;
                buyer = c;
                roundWhy = w;
                rank = r;
            }
            if (buyer == null)
            {
                why = _roundWhyNot = "nobody here with cause to stand the house a drink";
                return false;
            }
            bool toast = roundWhy == "wedding";
            Candidate b = people.Where(o => o != buyer && o.Who.Id != buyer.Who.Id && o.Ped.Position.DistanceTo(buyer.Ped.Position) < RoundBar && TieWarmth(o.Who.Id, buyer.Who) >= 0 &&
                                            (!toast || (o.Who.Id != wed.AId && o.Who.Id != wed.BId && !AgainstCouple(o.Who.Id, wed))))
                                .OrderByDescending(o => TieWarmth(o.Who.Id, buyer.Who) > 0 ? 1 : 0).ThenBy(o => o.Ped.Position.DistanceTo(buyer.Ped.Position)).FirstOrDefault();
            if (b == null)
            {
                why = _roundWhyNot = "nobody by " + buyer.Who.Name + " to drink to it";
                return false;
            }
            string skipped;
            string pace = Pacing.WhyNot(_store.Pace, "round", _placeTown, buyer.Who.Id, g, 0, test, out skipped);
            skipped = skipped ?? skipped0;
            if (pace != null)
            {
                why = _roundWhyNot = "rests: " + pace;
                return false;
            }
            string seed = buyer.Who.Id + "|round|" + g / 60;
            SceneCast cast = BuildCast(buyer.Ped, b.Ped, null, true, me, now, seed);
            Scene s = Scenes.Round(cast, seed, roundWhy, couple);
            _round = new RoundNote { Buyer = buyer.Ped, B = b.Ped, Name = buyer.Who.Name, Who = buyer.Who, Seed = seed, Test = test };
            StartScene(s, new List<Ped> { buyer.Ped, b.Ped }, cast, seed, now, test);
            if (!_director.Busy)
            {
                _round = null;
                why = _roundWhyNot = "the Director wouldn't take the scene";
                return false;
            }
            foreach (string d in new[] { Gestures.MaleSpeaker, Gestures.FemaleSpeaker })
                if (!STREAMING.HAS_ANIM_DICT_LOADED(d))
                    STREAMING.REQUEST_ANIM_DICT(d);
            Pacing.Note(_store.Pace, "round", _placeTown, buyer.Who.Id, g);
            _rounds++;
            _roundLast = buyer.Who.Name + " (" + roundWhy + ")" + (test ? ", a test" : "");
            Log.Write("KIND round: " + buyer.Who.Name + " stands the house a drink (" + roundWhy + "; " + b.Who.Name + " thanks them)" + (test ? ", a test" + (skipped != null ? " (skipped: " + skipped + ")" : "") : ""));
            return true;
        }

        /// The moment the round is called (the scene's "cb:round"): the room cheers, the ties and the player's glass.
        private void RoundGoesAround()
        {
            RoundNote n = _round;
            _round = null;
            if (n == null || n.Buyer == null || !n.Buyer.Exists())
                return;
            Ped me = Game.Player.Character;
            string bId, bName;
            bool lasting = LastingId(n.Buyer, out bId, out bName);
            long g = Bridge.Now();
            int cheered = 0;
            foreach (Ped p in World.GetAllPeds().Where(x => x != null && x.Exists() && !x.IsDead && x.Handle != me.Handle && x.Handle != n.Buyer.Handle && (n.B == null || x.Handle != n.B.Handle) &&
                                                           x.Position.DistanceTo(n.Buyer.Position) < RoundBar).OrderBy(x => x.Position.DistanceTo(n.Buyer.Position)))
            {
                if (cheered >= 4)
                    break;
                int h = p.Handle;
                if (!PED.IS_PED_HUMAN(h) || !Natives.Bystander(p, me.Handle, IsRecord(h)) || NotFree(h) || MayDirectWhyNot(p) != null || Speech.Busy(h))
                    continue;
                string pid, pname;
                bool plasting = LastingId(p, out pid, out pname);
                if (plasting && n.Who != null && TieWarmth(pid, n.Who) < 0)
                    continue;   // no cheer from someone with a grudge against him (and no tie written over theirs)
                TASK.TASK_LOOK_AT_ENTITY(h, n.Buyer.Handle, 4000, 0, 51, 0);
                bool woman = !PED.IS_PED_MALE(h);
                string dict = woman ? Gestures.FemaleSpeaker : Gestures.MaleSpeaker;
                if (!PED.IS_PED_USING_ANY_SCENARIO(h) && STREAMING.HAS_ANIM_DICT_LOADED(dict))
                {
                    string[] clips = woman ? new[] { "happy_cheer_f_001", "happy_cheer_f_002", "happy_cheer_f_003", "happy_laugh_f_001" } : new[] { "happy_cheer_f_001", "happy_cheer_f_002", "happy_cheer_f_003", "happy_laugh_f_002" };
                    TASK.TASK_PLAY_ANIM(h, dict, clips[Voice.Hash(n.Seed + h) % clips.Length], 4f, -4f, -1, 8 | 16, 0f, false, 0, false, "", false);
                }
                if (cheered < 2)
                    Speech.SayTag(h, "cheer", n.Buyer.Handle);   // two voices are a cheer; four are a din
                cheered++;
                if (!n.Test && lasting && plasting)
                {
                    SceneTie t = Ties.Note(_store.Ties, bId, bName, pid, pname, _placeTown, "round", g);
                    if (t != null)
                        Log.Detail("TIE " + t.AName + " & " + t.BName + ": round");
                }
            }
            bool you = me.Position.DistanceTo(n.Buyer.Position) < RoundYou;
            if (you)
            {
                Feed.Say(Person.FirstOf(n.Name), Scenes.RoundToYou(n.Seed), Speech.When(n.Buyer.Handle, "cheer", me.Handle), n.Buyer.Handle, toYou: true);
                if (!n.Test && n.Who.P != null)
                    _engine.StoodYouADrink(n.Who.P);
                _roundsYou++;
            }
            Log.Write("KIND round: " + n.Name + "'s round goes around; " + cheered + " cheer" + (you ? "; you're stood one too" : ""));
        }

        private string RoundStatus()
        {
            return _rounds + " rounds this session, " + _roundsYou + " with you at the bar" + (_roundLast != null ? "; last: " + _roundLast : "") + (_roundWhyNot != null ? "; the last look: " + _roundWhyNot : "");
        }
    }
}
