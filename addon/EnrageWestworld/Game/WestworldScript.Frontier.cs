// The player in the middle of other people's lives: stepping into a scene (taking a side, breaking up a fight,
// defusing a standoff, egging it on), chasing down a pickpocket, holding a gun on a stranger who begs for
// their life, and riding into a town that has heard of him.
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
        private sealed class Later
        {
            public int At;
            public string Label;
            public string Line;
            public int Ped;
            public string Voice;
            public bool Speak;       // also read aloud (uncanny lines)
            public bool Woman;
        }

        private sealed class Theft
        {
            public Ped Thief, Mark;
            public bool MarkWoman;
            public string MarkName;
            public string Town;
            public int Started;
            public int ThiefHealth;
        }

        private sealed class Pleading
        {
            public Ped Ped;
            public Plea Plea;
            public string Label;
            public bool Woman;
            public int At, LastAimedAt;
            public int Health;
        }

        private sealed class PendingArrival
        {
            public int At;
            public ArrivalTalk Talk;
            public string Town;
        }

        private readonly List<Later> _later = new List<Later>();
        private readonly HashSet<int> _thieves = new HashSet<int>();
        private readonly Dictionary<int, int> _pleadedAt = new Dictionary<int, int>();
        private readonly Dictionary<int, string> _pleadNames = new Dictionary<int, string>();
        private readonly HashSet<string> _posseFor = new HashSet<string>();
        private Theft _theft;
        private Pleading _plea;
        private PendingArrival _arrival;
        private int _aimTarget, _aimSince, _pleadCooldown, _nextSpecial;

        /// How strange the world feels to its strangers: the more of enRAGE's people have woken, the stranger.
        private int TownHostTier()
        {
            if (!_cfg.HostsQuestionReality || _store == null)
                return -1;
            int woke = _store.Reveries.Values.Count(r => r.Woke) + _store.Fallen.Count(f => f.Returned > 0);
            return woke >= 3 ? 2 : woke >= 1 ? 1 : 0;
        }

        // ---- lines said a moment later ----------------------------------------------------------------

        private void SayLater(int delayMs, string label, string line, int ped, string voice, bool speak = false, bool woman = false)
        {
            _later.Add(new Later { At = Game.GameTime + delayMs, Label = label, Line = line, Ped = ped, Voice = voice, Speak = speak, Woman = woman });
        }

        private void LaterTick(int now)
        {
            for (int i = 0; i < _later.Count; i++)
            {
                Later l = _later[i];
                if (now < l.At)
                    continue;
                _later.RemoveAt(i--);
                if (l.Ped != 0 && (!ENTITY.DOES_ENTITY_EXIST(l.Ped) || ENTITY.IS_ENTITY_DEAD(l.Ped)))
                    continue;
                // the speaker's own recorded line; an uncanny one (Speak) gets an unsettled one when no other was chosen
                string voice = l.Voice ?? (l.Speak ? "uneasy" : null);
                Feed.Say(l.Label, l.Line, l.Ped != 0 ? Speech.When(l.Ped, voice, Game.Player.Character.Handle) : null);
            }
        }

        // ---- stepping into a scene -----------------------------------------------------------------------

        /// The player greeted or antagonized someone in the scene running near him. True when the scene took it.
        private bool SceneIntrusion(int handle, bool friendly)
        {
            if (_cfg == null || !_cfg.SceneReactions || _engine == null || !_director.Busy || !_director.Involves(handle))
                return false;
            try
            {
                string kind;
                string outcome = _director.Intrude(handle, friendly, out kind);
                if (kind == null)
                    return false;
                if (outcome == "peace" && _cfg.Deeds)
                {
                    Vector3 pos = Game.Player.Character.Position;
                    long g = Bridge.Now();
                    _engine.OnDeed(Deeds.Make("peace", kind == "standoff" ? "a standoff" : "a fight", _bridge.TownAt(pos), ZoneAt(pos), g, pos.X, pos.Y, pos.Z,
                        CountPeopleNear(pos, 0, 25f)), WitnessesNear(pos, 0, 25f), g);
                }
                return true;
            }
            catch (Exception ex)
            {
                Log.Write("Scene reaction failed: " + ex.Message);
                return false;
            }
        }

        // ---- the pickpocket ---------------------------------------------------------------------------

        private void StartTheft()
        {
            Ped thief = _director.CastAt(0), mark = _director.CastAt(1);
            if (thief == null || mark == null || !thief.Exists() || !mark.Exists())
                return;
            _theft = new Theft
            {
                Thief = thief, Mark = mark, MarkWoman = !PED.IS_PED_MALE(mark.Handle), MarkName = _director.NameAt(1), Town = _placeTown,
                Started = Game.GameTime, ThiefHealth = Math.Max(1, ENTITY.GET_ENTITY_HEALTH(thief.Handle))
            };
            _thieves.Add(thief.Handle);
            Log.Write("PICKPOCKET at work" + (_placeTown != null ? " in " + _placeTown : "") + ": catch him (lasso, hogtie, tackle) for the reward of a good name");
        }

        private void TheftTick(int now)
        {
            Theft t = _theft;
            if (t == null)
                return;
            if (!t.Thief.Exists())
            {
                _theft = null;
                return;
            }
            Ped me = Game.Player.Character;
            int h = t.Thief.Handle;
            bool dead = t.Thief.IsDead;
            float dist = t.Thief.Position.DistanceTo(me.Position);
            bool caught = !dead && (PED.IS_PED_HOGTIED(h) || PED.IS_PED_LASSOED(h) || PED.IS_PED_INCAPACITATED(h) ||
                                    ((PED.IS_PED_RAGDOLL(h) || ENTITY.GET_ENTITY_HEALTH(h) < t.ThiefHealth * 0.6f) && dist < 6f));
            if (dead || caught)
            {
                ResolveTheft(t, dead ? "killed" : "caught", now);
                return;
            }
            if (now - t.Started > 120000 || dist > 160f)
                ResolveTheft(t, "escaped", now);
        }

        private void ResolveTheft(Theft t, string how, int now)
        {
            _theft = null;
            if (how != "killed" && t.Thief != null)
                _thieves.Remove(t.Thief.Handle);   // the game reuses handles; don't brand some later stranger a thief
            Ped me = Game.Player.Character;
            bool markHere = t.Mark != null && t.Mark.Exists() && !t.Mark.IsDead && t.Mark.Position.DistanceTo(me.Position) < 90f;
            string label = t.MarkName ?? (t.MarkWoman ? "The woman" : "The man");
            string seed = t.Started + how;
            if (how == "escaped")
            {
                if (markHere)
                    SayLater(500, label, Voice.Pick(seed, "Gone. Every cent I had.", "Well, that's that. Lord, what'll I tell them at home?", "Nobody lifted a finger. Not one of you."),
                        t.Mark.Handle, "mild");
                Log.Write("PICKPOCKET got away");
                return;
            }
            if (markHere)
            {
                TASK.TASK_GO_TO_ENTITY(t.Mark.Handle, me.Handle, 15000, 2.5f, 1.6f, 0f, 0);
                string line = how == "killed"
                    ? Voice.Pick(seed, "Lord... you didn't have to kill him. But thank you. Thank you.", "That's my " + (t.MarkWoman ? "purse" : "wallet") + ". On a dead man. Thank you, I think.")
                    : Voice.Pick(seed, "You caught him! God bless you, mister!", "I'll tell everybody in " + (t.Town ?? "town") + " what you did.",
                        "Thank you, stranger. There's still some decency left in this world.");
                SayLater(3000, label, line, t.Mark.Handle, "thanks");
            }
            if (_cfg.Deeds && how == "caught")   // a dead thief is a killing, and the killing tells its own story
            {
                Vector3 pos = me.Position;
                long g = Bridge.Now();
                _engine.OnDeed(Deeds.Make("thiefcaught", null, _bridge.TownAt(pos) ?? t.Town, ZoneAt(pos), g, pos.X, pos.Y, pos.Z, CountPeopleNear(pos, 0, 30f)), WitnessesNear(pos, 0, 30f), g);
            }
            Log.Write("PICKPOCKET " + how + " by you");
        }

        // ---- a gun on a stranger -------------------------------------------------------------------------

        private unsafe void PleadTick(int now)
        {
            if (!_cfg.Pleading)
                return;
            Ped me = Game.Player.Character;
            int player = Game.Player.Handle;
            int target = 0;
            if (PLAYER.IS_PLAYER_FREE_AIMING(player))
            {
                ulong slot = 0;
                if (PLAYER.GET_ENTITY_PLAYER_IS_FREE_AIMING_AT(player, (int*)&slot))
                    target = (int)(uint)slot;
                if (target == 0)
                {
                    slot = 0;
                    if (PLAYER.GET_PLAYER_TARGET_ENTITY(player, (int*)&slot))
                        target = (int)(uint)slot;
                }
            }
            Pleading p = _plea;
            if (p != null)
            {
                if (!p.Ped.Exists() || p.Ped.IsDead)
                {
                    _plea = null;   // shot: OnKilled writes it down
                    return;
                }
                if (target == p.Ped.Handle)
                {
                    p.LastAimedAt = now;
                }
                else if (now - p.LastAimedAt > 2500 && now - p.At > 3000)
                {
                    _plea = null;
                    if (ENTITY.GET_ENTITY_HEALTH(p.Ped.Handle) < p.Health - 5)
                        return;   // he shot them and they lived: that's no mercy
                    string line = Pleas.Spared(p.Woman, p.Plea.Uncanny, p.Ped.Handle + "s");
                    SayLater(200, p.Label, line, p.Ped.Handle, "relieved", p.Plea.Uncanny, p.Woman);
                    if (_cfg.Deeds)
                    {
                        Vector3 pos = p.Ped.Position;
                        long g = Bridge.Now();
                        _engine.OnDeed(Deeds.Make("mercy", Grammar.LowerFirst(p.Label), _bridge.TownAt(pos), ZoneAt(pos), g, pos.X, pos.Y, pos.Z, CountPeopleNear(pos, p.Ped.Handle, 30f)),
                            WitnessesNear(pos, p.Ped.Handle, 30f), g);
                    }
                    Log.Write("MERCY: you lowered your gun on " + p.Plea.Name);
                }
                else if (now - p.At > 20000)
                {
                    _plea = null;
                }
                return;
            }
            if (target == 0 || now < _pleadCooldown)
            {
                _aimTarget = 0;
                return;
            }
            if (target != _aimTarget)
            {
                _aimTarget = target;
                _aimSince = now;
                return;
            }
            if (now - _aimSince < 1200)
                return;
            if (!ENTITY.DOES_ENTITY_EXIST(target) || !ENTITY.IS_ENTITY_A_PED(target))
            {
                _aimTarget = 0;
                return;
            }
            Ped ped = new Ped(target);
            if (_pleadedAt.ContainsKey(target) || !PED.IS_PED_HUMAN(target) || !Natives.Bystander(ped, me.Handle) || Spawner.IsOurs(target) || _bridge.RecordAt(target) != null ||
                ped.Position.DistanceTo(me.Position) > 30f || PED.IS_PED_IN_COMBAT(me.Handle, 0) || _director.Involves(target))
            {
                _aimTarget = 0;
                _pleadCooldown = now + 1500;
                return;
            }
            bool woman = !PED.IS_PED_MALE(target);
            string label = Grammar.UpperFirst(Director.Kind(ped, woman));
            Plea plea = Pleas.For(woman, Grammar.LowerFirst(label), _placeTown, TownHostTier(), target + "|" + Bridge.Now() / 1440);
            _plea = new Pleading { Ped = ped, Plea = plea, Label = label, Woman = woman, At = now, LastAimedAt = now, Health = ENTITY.GET_ENTITY_HEALTH(target) };
            _pleadedAt[target] = now;
            _pleadNames[target] = plea.Name;
            _pleadCooldown = now + 25000;
            if (!plea.Defiant)
                TASK.TASK_HANDS_UP(target, 12000, me.Handle, -1, false);
            if (_pleadedAt.Count > 100)
                foreach (int old in _pleadedAt.Where(kv => now - kv.Value > 600000 || now < kv.Value).Select(kv => kv.Key).ToList())
                {
                    _pleadedAt.Remove(old);
                    _pleadNames.Remove(old);
                }
            for (int i = 0; i < plea.Lines.Count; i++)
                SayLater(150 + i * 2600, label, plea.Lines[i], target, i == 0 ? (plea.Defiant ? "threat" : "plead") : null, plea.Uncanny, woman);
            Log.Write("PLEA " + plea.Name + (plea.Uncanny ? " (a host)" : "") + ": " + string.Join(" | ", plea.Lines));
        }

        /// Was this ped begging when he died? (shot within half a minute of pleading)
        private bool DiedBegging(int handle, int now, out string name)
        {
            name = null;
            int at;
            if (!_pleadedAt.TryGetValue(handle, out at) || now - at > 30000)
                return false;
            _pleadNames.TryGetValue(handle, out name);
            return true;
        }

        // ---- riding into town -----------------------------------------------------------------------

        private void QueueArrival(string town, bool firstVisit)
        {
            if (!_cfg.ArrivalTalk || _engine == null || MISC.GET_MISSION_FLAG())
                return;
            bool wanted = LAW.GET_BOUNTY(Game.Player.Handle) > 0;
            ArrivalTalk t = _engine.Arrival(town, wanted, firstVisit, TownHostTier(), Bridge.Now(), town + "|" + Bridge.Now() / 60);
            if (t == null || _rng.Next(100) >= (t.Mood == "greet" ? 40 : 70))
                return;
            _arrival = new PendingArrival { At = Game.GameTime + 3000 + _rng.Next(4000), Talk = t, Town = town };
        }

        private void ArrivalTick(int now)
        {
            PendingArrival a = _arrival;
            if (a == null || now < a.At)
                return;
            _arrival = null;
            if (_placeTown != a.Town || Feed.Talking)
                return;
            Ped speaker = Bystander(5f, 26f, true);
            if (speaker == null)
                return;
            bool woman = !PED.IS_PED_MALE(speaker.Handle);
            string kind = Grammar.UpperFirst(Director.Kind(speaker, woman));
            string label = a.Talk.ToPlayer ? kind : kind + Voice.Pick(now.ToString(), ", to " + (woman ? "her" : "his") + " neighbour", ", under " + (woman ? "her" : "his") + " breath", ", to nobody");
            string voice;
            switch (a.Talk.Mood)
            {
                case "fear": voice = _rng.Next(2) == 0 ? "harmed" : "scared"; break;
                case "scorn": voice = "mock"; break;
                case "warm": voice = _rng.Next(2) == 0 ? "helped" : "familiar"; break;
                case "wanted": voice = "wanted"; break;
                case "uncanny": voice = "weird"; break;
                default: voice = "greet"; break;
            }
            TASK.TASK_LOOK_AT_ENTITY(speaker.Handle, Game.Player.Character.Handle, 6000, 0, 51, 0);
            SayLater(0, label, a.Talk.Line, speaker.Handle, voice, a.Talk.Mood == "uncanny", woman);
            Log.Write("ARRIVAL in " + a.Town + " (" + a.Talk.Mood + "): " + a.Talk.Line);
        }
    }
}
