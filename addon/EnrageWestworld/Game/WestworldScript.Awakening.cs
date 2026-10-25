// Residents and the Awakening, in the game.
// Residents (Core/Residents.cs) are everyone who isn't one of enRAGE's people. Something that asks "who is this?"
// (the ticker, the wake key) gets a resident for an ordinary adult nearby; the person keeps that identity as long
// as the game keeps them, and the resident is saved once something happens to them.
// The wake key (Y by default, WakeKey in the ini) wakes the person you're facing by one stage right now, shows what
// they say at that stage and logs it. The real thing runs once a game day in the engine (Core/Engine.Awakening.cs).
using System;
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using NemesisSystem;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private sealed class ResBind
        {
            public Resident R;
            public uint Model;
        }

        private KeySpec _wakeKey;
        private bool _wakeRequested;
        private readonly Dictionary<int, ResBind> _resBind = new Dictionary<int, ResBind>();
        private int _nextResPrune;

        // ---- residents ------------------------------------------------------------------------------

        /// Anyone the add-on may give an identity to (a name, thoughts, a mind that can wake): any adult human, soldiers,
        /// lawmen and script-owned peds included. Whether they may be moved about is a separate question (CanDirect).
        private bool ResidentEligible(Ped ped)
        {
            return ResidentWhy(ped) == null;
        }

        /// Why this person can't be a resident (null if they can).
        private string ResidentWhy(Ped ped)
        {
            if (ped == null || !ped.Exists())
                return "gone";
            int h = ped.Handle;
            if (Spawner.IsOurs(h)) return "one of the add-on's own";
            if (ENTITY.IS_ENTITY_DEAD(h)) return "dead";
            if (!PED.IS_PED_HUMAN(h)) return "not human";
            if (PED.IS_PED_A_PLAYER(h)) return "a player";
            if (Natives.IsChild(ped)) return "a child";
            bool post = PostsOn && PostKindOf(ped) != null;   // a counter's person: unique to the game, one person to us
            if (!post && Natives.StoryModel(ped)) return "a story or unique character";
            if (!post && !IsGrownUp(ped)) return "not a grown-up";
            if (_bridge.RecordAt(h) != null) return "one of enRAGE's people";
            if (_bridge.AcquaintanceAt(h) != null) return "one of enRAGE's townsfolk";
            return null;
        }

        /// The resident this person is, made if need be. Null if they can't be one.
        private Resident ResidentOf(Ped ped)
        {
            if (ped == null || !ped.Exists())
                return null;
            int h = ped.Handle;
            uint model = ENTITY.GET_ENTITY_MODEL(h);
            ResBind b;
            if (_resBind.TryGetValue(h, out b))
            {
                if (b.Model == model)
                    return b.R;
                _resBind.Remove(h);   // the game reused the handle for someone else
            }
            PostKind postKind = PostKindOf(ped);
            if (postKind != null)
                return ResidentEligible(ped) ? HolderAt(ped, postKind) : null;   // the post's one person, never recast
            if (!ResidentEligible(ped))
                return null;
            bool woman = !PED.IS_PED_MALE(h);
            string where = _placeTown ?? _town;
            // someone who woke here before, of the same build, may turn up again in this body
            Resident back = _rng.Next(100) < 35
                ? _store.Residents.FirstOrDefault(x => !x.Dead && x.Model == model && x.Woman == woman && x.Town == where && _engine.StageOf(x.Id) > 0 && !_resBind.Values.Any(v => v.R == x))
                : null;
            if (back != null)
            {
                _resBind[h] = new ResBind { R = back, Model = model };
                return back;
            }
            Resident r = _engine.MeetResident(model, woman, where, ResidentTrade(ped, woman), h + "|" + Game.GameTime, Bridge.Now());
            _resBind[h] = new ResBind { R = r, Model = model };
            return r;
        }

        /// What to call them: soldiers and lawmen have their own, the rest by how they look.
        private static string ResidentTrade(Ped ped, bool woman)
        {
            string m = Natives.ModelName(ped);
            if (m.Contains("army") || m.Contains("soldier")) return "a soldier";
            if (Natives.IsLaw(ped)) return "a lawman";
            return Director.Kind(ped, woman);
        }

        /// Forgets people who have gone; a resident who dies is marked so.
        private void ResidentsPrune(int now)
        {
            if (now < _nextResPrune)
                return;
            _nextResPrune = now + 5000;
            List<int> gone = new List<int>();
            foreach (KeyValuePair<int, ResBind> kv in _resBind)
            {
                int h = kv.Key;
                if (!ENTITY.DOES_ENTITY_EXIST(h))
                    gone.Add(h);
                else if (ENTITY.IS_ENTITY_DEAD(h))
                {
                    if (kv.Value.R.PostId == null)
                        kv.Value.R.Dead = true;   // a post's person is vacated by the posts code, which notes how they died
                    gone.Add(h);
                }
            }
            foreach (int h in gone)
                _resBind.Remove(h);
        }

        // ---- the wake key ---------------------------------------------------------------------------

        private void WakeNote(string text)
        {
            Log.Write("WAKE key: " + text);
            Feed.Interrupt("West World: " + text, 4500);
        }

        private static bool Near(Ped me, Ped p)
        {
            if (p.Handle == me.Handle || ENTITY.IS_ENTITY_DEAD(p.Handle))
                return false;
            float d = p.Position.DistanceTo(me.Position);
            return d <= 5f || (d <= 10f && PED.IS_PED_FACING_PED(me.Handle, p.Handle, 70f));
        }

        /// The enRAGE person to wake: whoever the game would greet, else the nearest within 10 m that you're roughly
        /// facing (any distance up to 5 m).
        private unsafe NemesisRecord WakeTarget(Ped me, out int handle)
        {
            handle = 0;
            int t = 0;
            if (!PLAYER.IS_PLAYER_FREE_AIMING(Game.Player.Handle))
                PLAYER.GET_PLAYER_INTERACTION_TARGET_ENTITY(Game.Player.Handle, &t, p2: false, p3: false);
            if (t != 0 && ENTITY.DOES_ENTITY_EXIST(t))
            {
                NemesisRecord r = _bridge.RecordAt(t);
                if (r != null && r.IsAlive)
                {
                    handle = t;
                    return r;
                }
            }
            NemesisRecord best = null;
            float bestD = float.MaxValue;
            foreach (Ped p in World.GetAllPeds())
            {
                if (!Near(me, p))
                    continue;
                NemesisRecord r = _bridge.RecordAt(p.Handle);
                float d = p.Position.DistanceTo(me.Position);
                if (r != null && r.IsAlive && d < bestD)
                {
                    best = r;
                    bestD = d;
                    handle = p.Handle;
                }
            }
            return best;
        }

        /// One of enRAGE's named townsfolk close enough.
        private Ped NearestFolk(Ped me, out Folk folk)
        {
            folk = null;
            Ped best = null;
            float bestD = float.MaxValue;
            foreach (Ped p in World.GetAllPeds())
            {
                if (!Near(me, p) || Natives.StoryModel(p))
                    continue;
                Acquaintance a = _bridge.AcquaintanceAt(p.Handle);
                float d = p.Position.DistanceTo(me.Position);
                if (a != null && !string.IsNullOrEmpty(a.Name) && d < bestD)
                {
                    best = p;
                    bestD = d;
                    folk = Snapshot.Of(a);
                }
            }
            return best;
        }

        /// Anyone else close enough: the nearest ordinary adult.
        private Ped ResidentCandidate(Ped me)
        {
            Ped best = null;
            float bestD = float.MaxValue;
            foreach (Ped p in World.GetAllPeds())
            {
                if (!Near(me, p) || !ResidentEligible(p))
                    continue;
                float d = p.Position.DistanceTo(me.Position);
                if (d < bestD)
                {
                    best = p;
                    bestD = d;
                }
            }
            return best;
        }

        private void WakeTick(int now)
        {
            ResidentsPrune(now);
            if (!_wakeRequested)
                return;
            _wakeRequested = false;
            if (!_cfg.WestWorld)
            {
                WakeNote("West World is switched off (WestWorld in RealNPCs.ini, or the West World page of the settings).");
                return;
            }
            Ped me = Game.Player.Character;
            int handle;
            Person p = null;
            Resident res = null;
            NemesisRecord rec = WakeTarget(me, out handle);
            if (rec != null)
                p = PersonOf(rec);
            else
            {
                Folk folk;
                Ped folkPed = NearestFolk(me, out folk);
                if (folkPed != null)
                {
                    p = _engine.PersonOfFolk(folk.Key, folk.Name, folk.Woman, folk.Town, Bridge.Now());
                    handle = folkPed.Handle;
                }
                else
                {
                    Ped other = ResidentCandidate(me);
                    if (other != null)
                    {
                        res = ResidentOf(other);
                        handle = other.Handle;
                        if (res != null)
                            p = _engine.PersonOfResident(res);
                    }
                }
            }
            if (p == null)
            {
                WakeNote("nobody is close enough. Stand within a few steps of someone, facing them, then press " + _wakeKey + ". (The log says what it saw.)");
                int n = 0;
                foreach (Ped near in World.GetAllPeds())
                {
                    float d = near.Position.DistanceTo(me.Position);
                    if (near.Handle == me.Handle || d > 15f || n >= 8)
                        continue;
                    n++;
                    string why = ResidentWhy(near);
                    Log.Write("WAKE key saw " + Natives.ModelName(near) + " " + (int)d + " m away" + (d > 5f && !PED.IS_PED_FACING_PED(me.Handle, near.Handle, 70f) ? ", not facing" : "") + ": " + (why == null ? "fine" : why));
                }
                if (n == 0)
                    Log.Write("WAKE key saw nobody within 15 m.");
                return;
            }
            PersonState st = _store.Get(p);
            string who = res != null ? p.Name + ", " + p.Trade + " from " + (p.Home ?? "around here") : p.Name;
            if (st.WakeStage >= Awakening.Awake)
            {
                WakeNote(who + " is already fully awake.");
                return;
            }
            int stage = _engine.Awaken(p, Bridge.Now());
            if (res != null)
                _engine.KeepResident(res, Bridge.Now());
            string line = Awakening.Aloud(p, st, _engine.Player, p.Id + "|wake|" + Environment.TickCount) ?? Awakening.Thought(p, st, _engine.Player, p.Id + "|wake");
            WakeNote(who + " is " + Awakening.StageName(stage) + " (stage " + stage + " of 4).");
            WakeDemo(handle, p, st, res, me, now);
            if (line != null && stage < Awakening.Unravelling)   // the later stages speak in their own act
                Feed.Say(p.Name, line, Speech.For(handle, line, me.Handle, p.Hate, p.Fear, p.Friendly, "chat"));
        }
    }
}
