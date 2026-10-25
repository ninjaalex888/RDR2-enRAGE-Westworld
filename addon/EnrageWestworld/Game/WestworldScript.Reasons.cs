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
        private const int PersonRestMs = 120000;   // real time before someone is in another scene
        private readonly Dictionary<int, int> _sceneAtHandle = new Dictionary<int, int>();
        private int _nextChance;

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
        private List<Candidate> ReasonCandidates(Ped me, int now)
        {
            var list = new List<Candidate>();
            foreach (Ped p in World.GetAllPeds())
            {
                int h = p.Handle;
                if (!Natives.Bystander(p, me.Handle) || p.Position.DistanceTo(me.Position) > ReasonRange || Spawner.IsOurs(h) || _bridge.RecordAt(h) != null ||
                    PostKindOf(p) != null || IsHermit(h) || IsGatherer(h) || _wakeActs.Any(w => w.Handle == h) ||
                    PED.IS_PED_IN_ANY_VEHICLE(h, false) || PED.IS_PED_ON_MOUNT(h))
                    continue;
                int at;
                if (_sceneAtHandle.TryGetValue(h, out at) && now - at < PersonRestMs && now >= at)
                    continue;
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
            Acquaintance ac = _bridge.AcquaintanceAt(h);
            if (ac != null && !string.IsNullOrEmpty(ac.Name))
            {
                Folk f = Snapshot.Of(ac);
                Person fp = _engine.PersonOfFolk(f.Key, f.Name, f.Woman, f.Town, Bridge.Now());
                if (fp.Personality == null)
                    fp.Personality = f.Temper;
                PersonState st = _store.Find(f.Key);
                return new Someone { Id = f.Key, Name = f.Name, Woman = f.Woman, Temper = f.Temper, P = fp, WakeStage = st != null ? st.WakeStage : 0 };
            }
            if (!ResidentEligible(p))
                return null;
            Resident r = ResidentOf(p);
            if (r == null)
                return null;
            return new Someone { Id = r.Id, Name = r.Name, Woman = r.Woman, Temper = r.Personality, P = _engine.PersonOfResident(r), WakeStage = _engine.StageOf(r.Id) };
        }

        /// Every pair close enough to each other with a reason, strongest first.
        private List<PairReason> PairReasons(List<Candidate> people, bool saloon)
        {
            long now = Bridge.Now(), day = now > 0 ? now / 1440 : -1;
            Reasons.Place where = new Reasons.Place
            {
                Saloon = saloon, Night = Natives.Night(),
                Rumours = (!saloon || _cfg.SaloonGossip) && _engine.RumourPool(_placeTown, 1).Count > 0,
                DeedsHere = _placeTown != null && _engine.ThoughtDay(_placeTown).TownDeeds.Count > 0
            };
            var found = new List<PairReason>();
            for (int i = 0; i < people.Count; i++)
                for (int j = i + 1; j < people.Count; j++)
                {
                    float apart = people[i].Ped.Position.DistanceTo(people[j].Ped.Position);
                    if (apart > ReasonApart)
                        continue;
                    Someone a = people[i].Who, b = people[j].Who;
                    SceneTie tie = _store.Ties.FirstOrDefault(t => t.Has(a.Id) && t.Has(b.Id));
                    Reason why = Reasons.Between(a, b, tie, day, now, where);
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
                if (cast.Off.Contains(kind) || (kind == "debt" && !_cfg.Debts) || (kind == "hosts" && cast.HostTier < 0) || (kind == "gossip" && cast.Rumours.Count == 0))
                    continue;
                Scene s;
                if (kind.StartsWith("x:"))
                {
                    // an exchange written for the reason (Exchanges.cs): the second one's answer comes from their own state
                    Someone fw = pr.Why.BFirst ? pr.B.Who : pr.A.Who, sw = pr.Why.BFirst ? pr.A.Who : pr.B.Who;
                    long day = Bridge.Now() / 1440;
                    string wk;
                    int wst;
                    bool wg;
                    bool atWorst = InnerLife.WorryNow(sw.P, day, out wk, out wst, out wg) && wst == 2;
                    SceneTie tie = _store.Ties.FirstOrDefault(t => t.Has(fw.Id) && t.Has(sw.Id));
                    s = Exchanges.Make(kind, fw, sw, cast.Names[0] ?? Person.FirstOf(fw.Name), cast.Names[1] ?? Person.FirstOf(sw.Name), day, atWorst, tie, seed);
                }
                else
                    s = Scenes.Make(kind, cast, seed);
                if (s == null)
                    continue;
                if (pr.Apart > 2.6f)
                {
                    // a few steps apart: they turn to each other, and the second walks over before a word is said
                    int walk = (int)Math.Max(0f, pr.Apart / 1.3f * 1000f - 2600f);
                    s.Beats.Insert(0, new Beat(1, null, null, "approach", Math.Min(4000, walk)));
                    s.Beats.Insert(0, new Beat(0, null, null, "face"));
                }
                Log.Write("REASON " + (pr.Why.BFirst ? pr.B.Who.Name + " & " + pr.A.Who.Name : pr.A.Who.Name + " & " + pr.B.Who.Name) + ": " + pr.Why.Why + " -> " + kind +
                          " (strength " + pr.Why.Strength + ", " + (int)pr.Apart + " m apart, " + (int)pr.A.Ped.Position.DistanceTo(me.Position) + " m from you)");
                StartScene(s, new List<Ped> { first, second }, cast, seed, now);
                return true;
            }
            return false;
        }

        /// Notes everyone in a scene, so they sit the next PersonRestMs out.
        private void NoteInScene(IEnumerable<Ped> people, int now)
        {
            foreach (Ped p in people)
                if (p != null)
                    _sceneAtHandle[p.Handle] = now;
            if (_sceneAtHandle.Count > 200)
                foreach (int h in _sceneAtHandle.Where(kv => now - kv.Value > PersonRestMs || now < kv.Value).Select(kv => kv.Key).ToList())
                    _sceneAtHandle.Remove(h);
        }

        /// The reasons near you right now, for the F7 dump (seen: on screen and in plain view).
        private string ReasonsStatus()
        {
            Ped me = Game.Player.Character;
            List<Candidate> people = ReasonCandidates(me, Game.GameTime);
            if (people.Count < 2)
                return people.Count + " named people free near you; no pairs";
            List<PairReason> found = PairReasons(people, _placeKind == "saloon");
            if (found.Count == 0)
                return people.Count + " named people free near you; none has a reason with anyone close by (a chance meeting now and then)";
            return people.Count + " named people free near you; " + string.Join("; ", found.Take(5).Select(pr =>
                pr.A.Who.Name + " & " + pr.B.Who.Name + ": " + pr.Why.Why + " -> " + pr.Why.Kind + " (" + pr.Why.Strength + ", " + (int)pr.Apart + " m apart" +
                (new[] { pr.A.Ped, pr.B.Ped }.Any(p => ENTITY.IS_ENTITY_ON_SCREEN(p.Handle)) ? ", in view" : ", out of view") + ")"));
        }
    }
}
