// Scenes that happen for a reason (Core/Reasons.cs): among the people near the player who have names (enRAGE's
// townsfolk, residents and regulars), the two with the strongest reason to have something to do with each other, close
// to each other and where the player can see at least one of them. The reason picks the scene and who starts it; if
// they're a few steps apart, one turns to the other and the other walks over first. Anyone who has just had a scene
// sits the next two real minutes out, and a pair's story moves on each time (SceneTie.Stage).
using System;
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using NemesisSystem;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private const float ReasonRange = 16f;     // from the player
        private const float ReasonApart = 7f;      // the two at most this far from each other
        private const float LinkedApart = 18f;     // ...or this far, when one is at the other end of the other's worry
        private const int PersonRestMs = 120000;   // real time before someone is in another scene
        private const int FoughtRestMs = 900000;   // ...or after coming to blows (a brawl, a rough, a drunk's challenge)
        private readonly Dictionary<int, int> _sceneAtHandle = new Dictionary<int, int>();
        private readonly Dictionary<int, int> _restMsOf = new Dictionary<int, int>();   // a longer rest than PersonRestMs, by handle
        private readonly Dictionary<int, int> _foughtWith = new Dictionary<int, int>();   // the two who came to blows, each to the other
        private int _nextChance;
        private static readonly uint DrinkingDrunk = Natives.Hash("WORLD_HUMAN_DRINKING_DRUNK");   // the game's drinking-drunk scenario (also the drinker's, Vices)

        private sealed class Candidate
        {
            public Ped Ped;
            public Someone Who;
        }

        private sealed class PairReason
        {
            public Candidate A, B;
            public Reason Why;
            public float Apart;
        }

        /// The people near the player who could be in a scene for a reason: named, free, not busy with anything else.
        private List<Candidate> ReasonCandidates(Ped me, int now, bool test = false)
        {
            var list = new List<Candidate>();
            foreach (Ped p in World.GetAllPeds())
            {
                int h = p.Handle;
                if (!Natives.Bystander(p, me.Handle, IsRecord(h)) || p.Position.DistanceTo(me.Position) > ReasonRange || Spawner.IsOurs(h) ||
                    MayDirectWhyNot(p) != null ||   // never the story's own people, grown-ups only; enRAGE's people unless it has them busy
                    PostKindOf(p) != null || IsHermit(h) || IsGatherer(h) || IsExecutionPed(h) || _wakeActs.Any(w => w.Handle == h) || IsAcknowledging(h) || InTalk(h) ||
                    PED.IS_PED_IN_ANY_VEHICLE(h, false) || PED.IS_PED_ON_MOUNT(h))
                    continue;
                if (!test && Resting(h, now))
                    continue;   // a test skips the rests (it logs that it did)
                Someone who = SomeoneOf(p);
                if (who != null)
                    list.Add(new Candidate { Ped = p, Who = who });
            }
            return list;
        }

        /// Who this person is to the reasons: one of enRAGE's townsfolk, or a resident (made on the spot if need be).
        private Someone SomeoneOf(Ped p)
        {
            int h = p.Handle;
            NemesisRecord rec = _bridge.RecordAt(h);
            if (rec != null)
            {
                // one of enRAGE's people: its own name, temper and record (Lex, 2026-10-26: they join in)
                if (!rec.IsAlive)
                    return null;
                Person who = PersonOf(rec);
                PersonState rs = _store.Find(who.Id);
                return Heart(new Someone { Id = who.Id, Name = ShownName(h, who.Name), Woman = who.Woman, Temper = who.Personality, P = who, WakeStage = rs != null ? rs.WakeStage : 0,
                                     WellOff = WellOff(p), Drunk = InHisCups(p, who), Hurt = HurtLately(who.Id) });
            }
            Acquaintance ac = _bridge.AcquaintanceAt(h);
            if (ac != null && !string.IsNullOrEmpty(ac.Name))
            {
                Folk f = Snapshot.Of(ac);
                Person fp = _engine.PersonOfFolk(f.Key, f.Name, f.Woman, f.Town, Bridge.Now());
                if (fp.Personality == null)
                    fp.Personality = f.Temper;
                PersonState st = _store.Find(f.Key);
                return Heart(new Someone { Id = f.Key, Name = f.Name, Woman = f.Woman, Temper = f.Temper, P = fp, WakeStage = st != null ? st.WakeStage : 0, WellOff = WellOff(p), Drunk = InHisCups(p, fp), Hurt = HurtLately(f.Key) });
            }
            if (!ResidentEligible(p))
                return null;
            Resident r = ResidentOf(p);
            if (r == null)
                return null;
            Person rp = _engine.PersonOfResident(r);
            return Heart(new Someone { Id = r.Id, Name = ShownName(h, r.Name), Woman = r.Woman, Temper = r.Personality, P = rp, WakeStage = _engine.StageOf(r.Id), WellOff = WellOff(p), Drunk = InHisCups(p, rp), Hurt = HurtLately(r.Id) });
        }

        /// What a street scene's reasons need to know of someone's heart and day: spoken for, their mood today, and
        /// whether they're known lately for a face in the bar or a hand in a coat (Reasons.Courtship).
        internal Someone Heart(Someone s)
        {
            if (s == null)
                return null;
            long g = Bridge.Now();
            s.Taken = _engine.SpokenFor(s.Id);
            s.Known = _store.Ties.Any(t => t.Doer == s.Id && (t.Kind == "rough" || t.Kind == "pickpocket") && g - t.At >= 0 && g - t.At < 3 * 1440);
            s.Mood = s.P != null ? _engine.MoodToday(s.P, s.P.Home ?? _placeTown) : null;
            return s;
        }

        /// Had it done to them in the last hour and a half (game time): a face put into the bar, or their pocket picked
        /// (and they know it: a lift only once it was noticed, which the tie can't tell, so a caught pickpocket only).
        private string HurtLately(string id)
        {
            long g = Bridge.Now();
            SceneTie t = _store.Ties.Where(x => x.Has(id) && x.Doer != null && !x.Did(id) && (x.Kind == "rough" || x.Kind == "pickpocket") && g - x.At >= 0 && g - x.At < 90)
                                    .OrderByDescending(x => x.At).FirstOrDefault();
            return t == null ? null : t.Kind == "rough" ? "rough" : "robbed";
        }

        /// Dressed like money, by the game's own model: a gentleman or a lady (Director.Kind).
        private static bool WellOff(Ped p)
        {
            string kind = Director.Kind(p, !PED.IS_PED_MALE(p.Handle));
            return kind == "a gentleman" || kind == "a lady";
        }

        /// In his cups: slumped over the bar or drinking himself silly (the game's drunk scenarios), or in a saloon at
        /// night with a drinking worry at its worst.
        private bool InHisCups(Ped p, Person who)
        {
            if (PED.IS_PED_USING_ANY_SCENARIO(p.Handle))
            {
                uint type = (uint)TASK._GET_SCENARIO_POINT_TYPE_PED_IS_USING(p.Handle);
                uint slumped = Natives.Hash("WORLD_HUMAN_DRUNK_BAR_SLUMPED"), reeling = DrinkingDrunk;
                if (type == slumped || type == reeling || PED.IS_PED_USING_SCENARIO_HASH(p.Handle, slumped) || PED.IS_PED_USING_SCENARIO_HASH(p.Handle, reeling))
                    return true;
            }
            if (who != null && Drinking(who.Id))
                return true;   // drinking away bad news tonight (WestworldScript.Vices.cs)
            string kind;
            int stage;
            bool good;
            int clock = CLOCK.GET_CLOCK_HOURS();
            if (_placeKind != "saloon" || !(clock >= 20 || clock < 4) || who == null)
                return false;
            return who.Has("drunk") || (InnerLife.WorryNow(who, Bridge.Now() / 1440, out kind, out stage, out good) && kind == "drink" && stage >= 1);   // a known drinker, or a drinking worry
        }

        /// Every pair close enough to each other with a reason, strongest first.
        private List<PairReason> PairReasons(List<Candidate> people, bool saloon)
        {
            long now = Bridge.Now(), day = now > 0 ? now / 1440 : -1;
            Reasons.Place where = new Reasons.Place
            {
                Saloon = saloon, Night = Natives.Night(), Courting = _cfg.NpcFlirting && _cfg.Romance, SameSex = _cfg.SameSexRomance,
                Rumours = (!saloon || _cfg.SaloonGossip) && _engine.RumourPool(_placeTown, 1).Count > 0,
                DeedsHere = _placeTown != null && _engine.ThoughtDay(_placeTown).TownDeeds.Count > 0
            };
            var found = new List<PairReason>();
            for (int i = 0; i < people.Count; i++)
                for (int j = i + 1; j < people.Count; j++)
                {
                    float apart = people[i].Ped.Position.DistanceTo(people[j].Ped.Position);
                    Someone a = people[i].Who, b = people[j].Who;
                    // one is the real person at the other end of the other's worry: they'll cross a street for it
                    WorryLink link = _store.Links.FirstOrDefault(l => ((l.Id == a.Id && l.OtherId == b.Id) || (l.Id == b.Id && l.OtherId == a.Id)) && InnerLife.ArcFor(l.Id, day) == l.Arc);
                    if (apart > (link != null ? LinkedApart : ReasonApart))
                        continue;
                    SceneTie tie = _store.Ties.FirstOrDefault(t => t.Has(a.Id) && t.Has(b.Id));
                    Reason why = Reasons.Between(a, b, tie, day, now, where, link);
                    if (why != null)
                        found.Add(new PairReason { A = people[i], B = people[j], Why = why, Apart = apart });
                }
            // the strongest reason, but close together and close to the player counts: a scene 5 m off is one he sees
            Vector3 mp = Game.Player.Character.Position;
            return found.OrderByDescending(x => x.Why.Strength - x.Apart * 2f - Math.Min(x.A.Ped.Position.DistanceTo(mp), x.B.Ped.Position.DistanceTo(mp)) * 2f).ToList();
        }

        /// The strongest reason near the player that he can see, played as a scene. False if there's none.
        private bool ReasonScene(Ped me, bool saloon, int now)
        {
            List<Candidate> people = ReasonCandidates(me, now);
            if (people.Count < 2)
                return false;
            foreach (PairReason pr in PairReasons(people, saloon).Take(4))
            {
                // where the player can see it: at least one of the two on screen and in plain view
                bool seen = new[] { pr.A.Ped, pr.B.Ped }.Any(p => ENTITY.IS_ENTITY_ON_SCREEN(p.Handle) && ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(me.Handle, p.Handle, 17));
                if (!seen)
                    continue;
                Ped first = pr.Why.BFirst ? pr.B.Ped : pr.A.Ped, second = pr.Why.BFirst ? pr.A.Ped : pr.B.Ped;
                string seed = first.Handle + "|" + second.Handle + "|" + now / 60000;
                SceneCast cast = BuildCast(first, second, null, saloon, me, now, seed);
                string kind = pr.Why.Kind;
                Someone doer = pr.Why.BFirst ? pr.B.Who : pr.A.Who, done = pr.Why.BFirst ? pr.A.Who : pr.B.Who;
                // the trouble scenes (Scenes.Trouble.cs): each needs its setting, and the rough move a surface by the victim;
                // when one can't be played the reason falls back (a fistfight, or words) or the pair is passed over
                string cant = TroubleCant(kind, first, second, cast);
                if (cant != null)
                {
                    Log.Detail("REASON " + doer.Name + " & " + done.Name + ": " + kind + " can't be played (" + cant + ")" + (pr.Why.Fallback != null ? ", so " + pr.Why.Fallback : ""));
                    if (pr.Why.Fallback == null || TroubleCant(pr.Why.Fallback, first, second, cast) != null)
                        continue;
                    kind = pr.Why.Fallback;
                }
                if (cast.Off.Contains(kind) || (kind == "debt" && !_cfg.Debts) || (kind == "hosts" && cast.HostTier < 0) || (kind == "gossip" && cast.Rumours.Count == 0))
                    continue;
                Scene s = BuildSceneFor(kind, first, second, doer, done, cast, seed, pr.Apart > 2.6f, true);
                if (s == null)
                    continue;
                AddMeet(s, kind, first, second, pr.Apart);
                Log.Write("REASON " + (pr.Why.BFirst ? pr.B.Who.Name + " & " + pr.A.Who.Name : pr.A.Who.Name + " & " + pr.B.Who.Name) + ": " + pr.Why.Why + " -> " + kind +
                          " (strength " + pr.Why.Strength + ", " + (int)pr.Apart + " m apart, " + (int)pr.A.Ped.Position.DistanceTo(me.Position) + " m from you)");
                List<Ped> cast2 = new List<Ped> { first, second };
                AddBarkeep(s, cast2, cast, seed, saloon);
                string stood;
                StandUpFor(s, cast2, cast, seed, now, false, out stood);   // someone may step in before the blow (WestworldScript.StandUp.cs)
                AddBystander(s, cast2, cast, seed, now);
                StartScene(s, cast2, cast, seed, now);
                return true;
            }
            return false;
        }

        /// The scene for a kind between first and second (the one who does it first): the trouble kinds, a courtship, an
        /// exchange (the second one's answer from their own state), or Scenes.Make; null when this pair can't have it.
        /// `note`: a courtship is noted (its tie and thoughts); a test doesn't. Shared by ReasonScene and the Testing page.
        private Scene BuildSceneFor(string kind, Ped first, Ped second, Someone doer, Someone done, SceneCast cast, string seed, bool apart, bool note, string courtAnswer = null)
        {
            if (kind == "avoid")
                return Scenes.Avoid(cast, seed);
            if (kind == "tiff" || kind == "mend")
            {
                // sweethearts (Scenes.Lovers.cs): words, or one making it up with a gift that the other takes or doesn't
                // by their own nature; a test takes turns, taken then not
                bool taken = kind == "mend" && (note ? Scenes.MendTaken(Exchanges.Manner(done, done.Hurt != null || done.Mood == "sore"), seed) : _mendTurn++ % 2 == 0);
                if (note)
                {
                    if (kind == "tiff")
                        _tiffs++;
                    else if (taken)
                        _mendsTaken++;
                    else
                        _mendsRefused++;
                    _loversLast = doer.Name + " & " + done.Name + ": " + (kind == "tiff" ? "words" : taken ? "made it up" : "a gift turned down");
                    Log.Write("ROMANCE " + _loversLast);
                }
                return kind == "tiff" ? Scenes.Tiff(cast, seed) : Scenes.Mend(cast, seed, taken);
            }
            if (kind == "tend")
                return Scenes.Tend(cast, seed, done.Hurt ?? "rough");
            if (kind == "beg")
            {
                // the answer comes from the asked one's own nature (Exchanges.Manner); the scene's kind is how it went
                long bday = Bridge.Now() / 1440;
                string bk;
                int bst;
                bool bg;
                return Scenes.Beg(cast, seed, Exchanges.Manner(done, (InnerLife.WorryNow(done.P, bday, out bk, out bst, out bg) && bst == 2) || done.Hurt != null));
            }
            if (kind == "rough" || kind == "lift" || kind == "drunk")
            {
                long tday = Bridge.Now() / 1440;
                string wk;
                int wst;
                bool wg;
                string answer = Exchanges.Manner(done, (InnerLife.WorryNow(done.P, tday, out wk, out wst, out wg) && wst == 2) || done.Hurt != null);
                SceneTie bad = _store.Ties.FirstOrDefault(t => t.Has(doer.Id) && t.Has(done.Id));
                // a lift is noticed too late now and then: by the alert, more often (the thief is long gone by then)
                return kind == "rough" ? Scenes.Rough(cast, seed, bad != null && bad.Warmth < 0, answer)
                     : kind == "lift" ? Scenes.Lift(cast, seed, Voice.Chance(seed + "late", Reasons.Alert(done.Temper) ? 60 : 35))
                     : Scenes.Drunk(cast, seed, answer);
            }
            if (kind.StartsWith("court:") || (kind == "court" && courtAnswer != null))
            {
                // one taken with the other: the other's answer, and what weighed, are in the kind (Reasons.Courtship)
                string[] parts = kind == "court" ? new[] { "court", courtAnswer } : kind.Split(':');
                Scene c = Scenes.Court(cast, seed, parts[1], parts.Length > 2 ? parts[2] : null);
                if (note)
                {
                    NoteCourt(doer, done, parts[1], parts.Length > 2 ? parts[2] : null);
                    CourtSeen(first, second, doer, done, parts[1], parts.Length > 2 ? parts[2] : null, apart, seed);
                }
                return c;
            }
            if (kind.StartsWith("x:"))
            {
                // an exchange written for the reason (Exchanges.cs): the second one's answer comes from their own state
                long day = Bridge.Now() / 1440;
                string wk;
                int wst;
                bool wg;
                bool atWorst = (InnerLife.WorryNow(done.P, day, out wk, out wst, out wg) && wst == 2) || done.Hurt != null;   // sore from today, too
                SceneTie tie = _store.Ties.FirstOrDefault(t => t.Has(doer.Id) && t.Has(done.Id));
                string fn = cast.Names[0] ?? Person.FirstOf(doer.Name), sn = cast.Names[1] ?? Person.FirstOf(done.Name);
                return kind == "x:debt" || kind == "x:feud" || kind == "x:courting"
                    ? Exchanges.MakeLinked(kind, doer, done, fn, sn, day, seed)   // the worrier (A) and the one at the other end (B)
                    : Exchanges.Make(kind, doer, done, fn, sn, day, atWorst, tie, seed);
            }
            return Scenes.Make(kind, cast, seed);
        }

        /// Two people apart: the one who does it walks over for trouble (the attacker, the thief, the drunk, the beggar,
        /// the friend, the suitor), else one turns to the other and the other walks over, and not a word until they're
        /// face to face (the Director's "meet" waits for it, and gives up if they never get there).
        private static void AddMeet(Scene s, string kind, Ped first, Ped second, float apart)
        {
            if (apart <= 2.6f)
                return;
            if (kind == "rough" || kind == "lift" || kind == "drunk" || kind == "beg" || kind == "tend" || kind == "mend" || kind.StartsWith("court"))
            {
                s.Beats.Insert(0, new Beat(0, null, null, "meet"));
                return;
            }
            // the second walks, unless they sit or lean and the first doesn't: then the first does (a seated man was too
            // often left waiting for)
            int walker = PED.IS_PED_USING_ANY_SCENARIO(second.Handle) && !PED.IS_PED_USING_ANY_SCENARIO(first.Handle) ? 0 : 1;
            s.Beats.Insert(0, new Beat(walker, null, null, "meet"));
            s.Beats.Insert(0, new Beat(1 - walker, null, null, "face"));
        }

        /// In a saloon, the barkeep has his say when it gets rough (a face in the bar, fists, a drunk on the floor): he
        /// joins the scene as its third person with one line, after the trouble itself.
        private void AddBarkeep(Scene s, List<Ped> people, SceneCast cast, string seed, bool saloon)
        {
            string line = saloon && s.Cast == 2 && people.Count == 2 ? Scenes.Barkeep(s.Kind, seed) : null;
            if (line == null)
                return;
            Ped me = Game.Player.Character;
            Ped keep = World.GetAllPeds().FirstOrDefault(p => p != null && p.Exists() && !ENTITY.IS_ENTITY_DEAD(p.Handle) && PostKindOf(p) != null && PostKindOf(p).Role == "the barkeep" &&
                                                           p.Position.DistanceTo(people[0].Position) < 18f && !_director.Involves(p.Handle));
            if (keep == null)
                return;
            string cant = Natives.StoryModel(keep) ? "one of the story's own people" : !IsGrownUp(keep) ? "not a grown-up" : !CanDirect(keep) ? "can't be directed now" : null;
            if (cant != null)
            {
                Log.Write("BARKEEP left out (" + cant + ") of the " + s.Kind);   // the casting rules: never a story character, adults only, CanDirect
                return;
            }
            string after = s.Kind == "rough" ? "slam" : s.Kind == "brawl" ? "endfight" : "passout";
            int at = s.Beats.FindIndex(b => b.Act == after);
            if (at < 0)
                return;
            s.Beats.Insert(at + 1, new Beat(2, line, "angry"));
            s.Cast = 3;
            people.Add(keep);
            Resident holder = HolderAt(keep, PostKindOf(keep));
            cast.Names[2] = holder != null ? Person.FirstOf(holder.Name) : null;
            Log.Write("BARKEEP " + (cast.Names[2] ?? "the barkeep") + " has his say about the " + s.Kind);
        }

        /// Why a trouble scene can't be played with these two now, or null if it can (other kinds: null).
        private string TroubleCant(string kind, Ped first, Ped second, SceneCast cast)
        {
            switch (kind)
            {
                case "rough":
                    if (!_cfg.RoughHandling) return "RoughHandling is off";
                    return RoughCanPlay(second) ? null : "no table, bar or wall by him that the move fits";
                case "lift":
                    return _cfg.NpcThieves ? null : "NpcThieves is off";
                case "pickpocket":
                    // caught in the act (the older scene): the robbed one cries thief and he bolts for the player to run down;
                    // one of those at a time, and its lines want a man
                    if (!_cfg.NpcThieves || !_cfg.Pickpockets) return "NpcThieves or Pickpockets is off";
                    return _theft != null ? "a thief is already running" : cast.AWoman ? "the lines want a man thief" : null;
                case "drunk":
                    if (!_cfg.DrunkChallenges) return "DrunkChallenges is off";
                    return STREAMING.DOES_ANIM_DICT_EXIST(Director.DrunkDict) ? null : "this game is missing the drunk-dueler animation set";
                default:
                    return null;
            }
        }

        /// One named person watching close by may call out once (Crowd.cs): they join the scene as its third person,
        /// with a single line after the second thing said, chosen from what's between them and the two (a friend of
        /// one sides with them) and their own temper. Only for scenes of two that draw a word (a quarrel, a fight, a
        /// courtship, bad news).
        private void AddBystander(Scene s, List<Ped> people, SceneCast cast, string seed, int now)
        {
            string mood = Crowd.Mood(s.Kind);
            if (s.Cast != 2 || people.Count != 2 || mood == null || !Voice.Chance(seed + "by", Crowd.Chance(mood)))
                return;
            Ped me = Game.Player.Character;
            Vector3 mid = (people[0].Position + people[1].Position) * 0.5f;
            Someone a = SomeoneOf(people[0]), b = SomeoneOf(people[1]);
            string an = cast.Names[0] ?? (a != null ? Person.FirstOf(a.Name) : "friend"), bn = cast.Names[1] ?? (b != null ? Person.FirstOf(b.Name) : "friend");
            // not someone of the same first name as either: the labels over the lines must tell the three apart
            List<Candidate> watching = ReasonCandidates(me, now).Where(c => c.Ped.Handle != people[0].Handle && c.Ped.Handle != people[1].Handle &&
                                                                          c.Ped.Position.DistanceTo(mid) < 10f && Person.FirstOf(c.Who.Name) != an && Person.FirstOf(c.Who.Name) != bn).ToList();
            if (watching.Count == 0)
                return;
            // someone with history with either of them speaks first; else whoever is nearest
            Candidate by = watching.OrderByDescending(c => TieWarmth(c.Who.Id, a) != 0 || TieWarmth(c.Who.Id, b) != 0 ? 1 : 0)
                                   .ThenBy(c => c.Ped.Position.DistanceTo(mid)).First();
            CrowdLine word = Crowd.Word(s.Kind, TieWarmth(by.Who.Id, a), TieWarmth(by.Who.Id, b), Exchanges.Manner(by.Who, false), an, bn, !cast.AWoman && !cast.BWoman, seed);
            if (word == null)
                return;
            int spoken = 0, at = s.Beats.Count;
            for (int i = 0; i < s.Beats.Count; i++)
                if (s.Beats[i].Line != null && ++spoken == 2)
                {
                    at = i + 1;
                    break;
                }
            s.Beats.Insert(at, new Beat(2, word.Line, word.Voice));
            s.Cast = 3;
            people.Add(by.Ped);
            cast.Names[2] = Person.FirstOf(by.Who.Name);
            cast.Tempers[2] = by.Who.Temper;
            cast.CWoman = by.Who.Woman;
            Log.Write("BYSTANDER " + by.Who.Name + " watches " + an + " & " + bn + " (" + s.Kind + ") and calls out");
        }

        private int TieWarmth(string id, Someone other)
        {
            if (id == null || other == null)
                return 0;
            SceneTie t = _store.Ties.FirstOrDefault(x => x.Has(id) && x.Has(other.Id));
            return t != null ? t.Warmth : 0;
        }

        /// Everyone else close by turns to look at a scene (a head turn only, nothing more), up to six of them.
        private void Onlookers(List<Ped> people)
        {
            Ped me = Game.Player.Character;
            Ped first = people[0];
            int n = 0;
            foreach (Ped p in World.GetAllPeds())
            {
                if (n >= 6)
                    break;
                int h = p.Handle;
                if (people.Any(x => x.Handle == h) || !Natives.Bystander(p, me.Handle, IsRecord(h)) || Spawner.IsOurs(h) || PED.IS_PED_IN_ANY_VEHICLE(h, false) ||
                    p.Position.DistanceTo(first.Position) > 12f)
                    continue;
                TASK.TASK_LOOK_AT_ENTITY(h, first.Handle, 9000, 0, 51, 0);
                n++;
            }
        }

        /// Notes everyone in a scene, so they sit the next PersonRestMs out; the two who come to blows sit FoughtRestMs out.
        private void NoteInScene(IList<Ped> people, string kind, int now)
        {
            bool blows = kind == "brawl" || kind == "rough" || kind == "drunk";
            if (blows && people.Count >= 2 && people[0] != null && people[1] != null)
            {
                _foughtWith[people[0].Handle] = people[1].Handle;
                _foughtWith[people[1].Handle] = people[0].Handle;
            }
            for (int i = 0; i < people.Count; i++)
            {
                Ped p = people[i];
                if (p == null)
                    continue;
                _sceneAtHandle[p.Handle] = now;
                if (blows && i < 2)
                    _restMsOf[p.Handle] = FoughtRestMs;
                else
                    _restMsOf.Remove(p.Handle);
            }
            if (_sceneAtHandle.Count > 200)
                foreach (int h in _sceneAtHandle.Where(kv => !Resting(kv.Key, now)).Select(kv => kv.Key).ToList())
                {
                    _sceneAtHandle.Remove(h);
                    _restMsOf.Remove(h);
                    _foughtWith.Remove(h);
                }
        }

        /// Sitting out after a scene: PersonRestMs, or longer for the two who fought.
        private bool Resting(int h, int now)
        {
            int at, ms;
            if (!_sceneAtHandle.TryGetValue(h, out at) || now < at)
                return false;
            if (!_restMsOf.TryGetValue(h, out ms))
                ms = PersonRestMs;
            return now - at < ms;
        }

        /// The worries with a real person at the other end, in the town you're in (all towns elsewhere): who, the
        /// stage, and whether either of them is around (within 60 m), for the F7 dump.
        private string LinksStatus()
        {
            long day = Bridge.Now() / 1440;
            List<WorryLink> current = _store.Links.Where(l => InnerLife.ArcFor(l.Id, day) == l.Arc && (_placeTown == null || l.Town == _placeTown)).ToList();
            if (current.Count == 0)
                return "none current" + (_placeTown != null ? " in " + _placeTown : "") + " (" + _store.Links.Count + " kept)";
            Ped me = Game.Player.Character;
            HashSet<string> around = new HashSet<string>(_resBind.Where(kv => ENTITY.DOES_ENTITY_EXIST(kv.Key) && new Ped(kv.Key).Position.DistanceTo(me.Position) < 60f).Select(kv => kv.Value.R.Id));
            foreach (Ped p in World.GetAllPeds().Where(p => p.Position.DistanceTo(me.Position) < 60f))
            {
                Acquaintance ac = _bridge.AcquaintanceAt(p.Handle);
                if (ac != null && !string.IsNullOrEmpty(ac.Name))
                    around.Add(Snapshot.Of(ac).Key);
            }
            string[] stages = { "it begins", "worse", "the crisis", "ended" };
            return current.Count + " current; " + string.Join("; ", current.Take(8).Select(l => l.Name + " " + (l.Kind == "debt" ? "owes" : l.Kind == "feud" ? "is at odds with" : "is sweet on") + " " +
                l.OtherName + " (" + stages[InnerLife.StageFor(l.Id, day)] + (around.Contains(l.Id) ? "; " + Person.FirstOf(l.Name) + " is here" : "") +
                (around.Contains(l.OtherId) ? "; " + Person.FirstOf(l.OtherName) + " is here" : "") + ")"));
        }

        /// The reasons near you right now, for the F7 dump (seen: on screen and in plain view).
        private string ReasonsStatus()
        {
            Ped me = Game.Player.Character;
            int now = Game.GameTime;
            int fought = _restMsOf.Keys.Count(h => Resting(h, now));
            string sat = fought > 0 ? fought + " who came to blows sitting it out (" + FoughtRestMs / 60000 + " min); " : "";
            List<Candidate> people = ReasonCandidates(me, now);
            if (people.Count < 2)
                return sat + people.Count + " named people free near you; no pairs";
            List<PairReason> found = PairReasons(people, _placeKind == "saloon");
            if (found.Count == 0)
                return sat + people.Count + " named people free near you; none has a reason with anyone close by (a chance meeting now and then)";
            return sat + people.Count + " named people free near you; " + string.Join("; ", found.Take(5).Select(pr =>
                pr.A.Who.Name + " & " + pr.B.Who.Name + ": " + pr.Why.Why + " -> " + pr.Why.Kind + " (" + pr.Why.Strength + ", " + (int)pr.Apart + " m apart" +
                (new[] { pr.A.Ped, pr.B.Ped }.Any(p => ENTITY.IS_ENTITY_ON_SCREEN(p.Handle)) ? ", in view" : ", out of view") + ")"));
        }
    }
}
