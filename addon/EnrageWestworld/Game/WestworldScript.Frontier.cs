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
            public Ped Lawman;            // a lawman who gave chase (LawChases)
            public bool PlayerMark;       // your own pocket (the lift, WestworldScript.Lift.cs)
            public int Amount;            // what he took from you, in cents
            public Blip Blip;             // his red mark on the map (the lift)
            public int LawSince, LawLookAt;
            public bool LawHeld;          // brought down by the law: held, waiting to be resolved
            public int LawHeldAt;
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
                Feed.Say(l.Label, l.Line, l.Ped != 0 ? Speech.When(l.Ped, voice, Game.Player.Character.Handle) : null, toYou: true);
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
                    NoteDeedMinds(_engine.OnDeed(Deeds.Make("peace", kind == "standoff" ? "a standoff" : "a fight", _bridge.TownAt(pos), ZoneAt(pos), g, pos.X, pos.Y, pos.Z,
                        CountPeopleNear(pos, 0, 25f)), WitnessesNear(pos, 0, 25f), g), pos, 0);
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
            MarkThief(thief, Game.GameTime);   // a red dot too, fair game for the catching (2026-10-29)
            _thieves.Add(thief.Handle);
            Log.Write("PICKPOCKET at work" + (_placeTown != null ? " in " + _placeTown : "") + ": catch him (lasso, hogtie, tackle) for the reward of a good name");
        }

        private void TheftTick(int now)
        {
            LiftStep(now);   // a thief who lifts your own money (WestworldScript.Lift.cs)
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
            if (LawChaseStep(t, me, now))
                return;
            if (now - t.Started > 120000 || dist > 160f)
                ResolveTheft(t, "escaped", now);
        }

        // ---- a lawman runs him down (Lex, 2026-10-29: the game's police chased and tackled a man; "could we get this into the mod?") ----

        private int _lawChases, _lawCatches;
        private string _lawChaseLast;

        /// The law joins the chase: a lawman within 70 m, on his feet and not after you, runs the thief down; within arm's reach
        /// the thief goes down (a tackle) and the lawman holds him for arrest. True while it has the theft in hand.
        private bool LawChaseStep(Theft t, Ped me, int now)
        {
            if (!_cfg.LawChases)
                return false;
            int h = t.Thief.Handle;
            if (t.Lawman == null)
            {
                if (now - t.Started < 2500 || now < t.LawLookAt)
                    return false;
                t.LawLookAt = now + 2000;
                Ped law = World.GetAllPeds().Where(p => p != null && p.Exists() && !p.IsDead && p.Handle != me.Handle && Natives.IsLaw(p) && !PED.IS_PED_ON_MOUNT(p.Handle) &&
                                                        !PED.IS_PED_IN_ANY_VEHICLE(p.Handle, false) && !PED.IS_PED_IN_COMBAT(p.Handle, me.Handle) && p.Position.DistanceTo(t.Thief.Position) < 70f)
                                            .OrderBy(p => p.Position.DistanceTo(t.Thief.Position)).FirstOrDefault();
                if (law == null)
                    return false;
                t.Lawman = law;
                t.LawSince = now;
                _lawChases++;
                TASK.CLEAR_PED_TASKS(law.Handle, true, false);
                TASK.TASK_GO_TO_ENTITY(law.Handle, h, -1, 3.0f, 1.2f, 0f, 0);   // at a run
                LawSay(law, "The deputy", LawChase.Shout(t.Started + "|law"), "SHOUT_STOP");
                Log.Write("LAWCHASE a lawman " + (int)law.Position.DistanceTo(t.Thief.Position) + " m off gives chase to the pickpocket");
                return false;
            }
            Ped l = t.Lawman;
            if (!l.Exists() || l.IsDead || PED.IS_PED_IN_COMBAT(l.Handle, me.Handle))
            {
                t.Lawman = null;
                t.LawLookAt = now + 600000;   // no second lawman: the player has it from here
                return false;
            }
            float d = l.Position.DistanceTo(t.Thief.Position);
            if (!t.LawHeld)
            {
                if (d < 1.8f)
                {
                    // the tackle: down he goes, and the lawman stands over him
                    PED.SET_PED_TO_RAGDOLL(h, 4000, 6000, 0, false, false, "");
                    TASK.CLEAR_PED_TASKS(l.Handle, true, false);
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(l.Handle, h, 6000, 0f, 0f, 0f);
                    t.LawHeld = true;
                    t.LawHeldAt = now;
                    LawSay(l, "The deputy", LawChase.Got(t.Started + "|got"), "WON_FIGHT");
                    Log.Write("LAWCHASE the lawman brought the pickpocket down after " + (now - t.LawSince) / 1000 + " s");
                    return true;
                }
                if (now - t.LawSince > 45000)
                {
                    Log.Write("LAWCHASE the lawman lost him");
                    TASK.CLEAR_PED_TASKS(l.Handle, true, false);
                    t.Lawman = null;
                    t.LawLookAt = now + 600000;
                    return false;
                }
                if ((now - t.LawSince) % 4000 < 60)
                    TASK.TASK_GO_TO_ENTITY(l.Handle, h, -1, 3.0f, 1.2f, 0f, 0);   // the thief changes course: the order again
                return false;
            }
            if (now - t.LawHeldAt > 2500)
            {
                _theft = null;
                _thieves.Remove(h);
                _lawCatches++;
                Ped mark = t.Mark;
                if (mark != null && mark.Exists() && !mark.IsDead && mark.Position.DistanceTo(me.Position) < 90f)
                    SayLater(1200, t.MarkName ?? (t.MarkWoman ? "The woman" : "The man"), LawChase.Thanks(t.MarkWoman, t.Started + "|thx"), mark.Handle, "thanks");
                if (t.Thief.Exists() && !t.Thief.IsDead)
                {
                    TASK.CLEAR_PED_TASKS(h, true, false);
                    TASK.TASK_HANDS_UP(h, 15000, l.Handle, -1, false);   // caught: hands up while he's taken
                }
                TASK.TASK_LOOK_AT_ENTITY(l.Handle, h, 15000, 0, 51, 0);
                WanderLater(l.Handle, now + 15000);
                _lawChaseLast = "caught" + (t.Town != null ? " in " + t.Town : "");
                if (t.Blip != null && t.Blip.Exists())
                    t.Blip.Delete();
                LiftResolved(t, "law");
                Log.Write("LAWCHASE caught: the pickpocket held by the law" + (t.Town != null ? " in " + t.Town : ""));
                return true;
            }
            return true;
        }

        /// A line said aloud in the chase: the speaker's own voice (the first of `voice` their bank has), our words on the ticker.
        private void LawSay(Ped p, string label, string words, string voice)
        {
            if (p == null || !p.Exists())
                return;
            string line, said;
            int take;
            Speech.PlayFirst(p.Handle, new[] { voice, "GENERIC_SHOCKED_HIGH", "GENERIC_CURSE_HIGH" }, 0, out line, out take, out said);
            if (_cfg.ScenesOnTicker && _cfg.Ticker && p.Position.DistanceTo(Game.Player.Character.Position) <= _cfg.ScenesOnTickerMetres)
                SpeechCaption(label, words, p.Handle, Math.Min(5000, 1500 + words.Length * 55));
            Log.Write("LAWCHASE   " + label + ": " + words + (line != null ? " (" + line + ")" : " (no voice)"));
        }

        private string LawChaseStatus()
        {
            return (_cfg.LawChases ? "on" : "off ([Scenes] LawChases)") + "; " + _lawChases + " chases this session, " + _lawCatches + " caught" + (_lawChaseLast != null ? "; last: " + _lawChaseLast : "");
        }

        private void ResolveTheft(Theft t, string how, int now)
        {
            _theft = null;
            if (t.Blip != null && t.Blip.Exists())
                t.Blip.Delete();
            LiftResolved(t, how);
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
                NoteDeedMinds(_engine.OnDeed(Deeds.Make("thiefcaught", null, _bridge.TownAt(pos) ?? t.Town, ZoneAt(pos), g, pos.X, pos.Y, pos.Z, CountPeopleNear(pos, 0, 30f)), WitnessesNear(pos, 0, 30f), g), pos, 0);
            }
            Log.Write("PICKPOCKET " + how + " by you");
        }

        // ---- drawing on a stranger ---------------------------------------------------------------------

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
                        NoteDeedMinds(_engine.OnDeed(Deeds.Make("mercy", Grammar.LowerFirst(p.Label), _bridge.TownAt(pos), ZoneAt(pos), g, pos.X, pos.Y, pos.Z, CountPeopleNear(pos, p.Ped.Handle, 30f)),
                            WitnessesNear(pos, p.Ped.Handle, 30f), g), pos, p.Ped.Handle);
                    }
                    Log.Write("MERCY: you lowered your gun on " + p.Plea.Name);
                }
                else if (now - p.At > 20000)
                {
                    _plea = null;
                }
                return;
            }
            bool armed = now < _pleaArmedUntil;   // the Testing page's plea: no wait, and someone who pleaded before may again
            if (target == 0 || (now < _pleadCooldown && !armed))
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
            if (now - _aimSince < (armed ? 300 : 1200))
                return;
            if (!ENTITY.DOES_ENTITY_EXIST(target) || !ENTITY.IS_ENTITY_A_PED(target))
            {
                _aimTarget = 0;
                return;
            }
            Ped ped = new Ped(target);
            if ((_pleadedAt.ContainsKey(target) && !armed) || !PED.IS_PED_HUMAN(target) || !Natives.Bystander(ped, me.Handle) || Natives.StoryModel(ped) || Spawner.IsOurs(target) || _bridge.RecordAt(target) != null ||
                ped.Position.DistanceTo(me.Position) > 30f || PED.IS_PED_IN_COMBAT(me.Handle, 0) || _director.Involves(target) ||
                _bridge.EnrageHandlesAtGunpoint(ped, me, 30f))   // one of enRAGE's outlaws: he's enRAGE's to handle (Bridge.Gunpoint.cs)
            {
                _aimTarget = 0;
                _pleadCooldown = now + 1500;
                return;
            }
            bool woman = !PED.IS_PED_MALE(target);
            string label = Grammar.UpperFirst(Director.Kind(ped, woman));
            Plea plea = Pleas.For(woman, Grammar.LowerFirst(label), _placeTown, TownHostTier(), target + "|" + Bridge.Now() / 1440);
            _plea = new Pleading { Ped = ped, Plea = plea, Label = label, Woman = woman, At = now, LastAimedAt = now, Health = ENTITY.GET_ENTITY_HEALTH(target) };
            if (armed)
            {
                _pleaArmedUntil = 0;
                Log.Write("TEST plea: " + plea.Name + " pleads (" + label + ")");
            }
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

        private bool QueueArrival(string town, bool firstVisit, bool force = false)
        {
            if (!_cfg.ArrivalTalk || _engine == null || MISC.GET_MISSION_FLAG())
                return false;
            bool wanted = LAW.GET_BOUNTY(Game.Player.Handle) > 0;
            ArrivalTalk t = _engine.Arrival(town, wanted, firstVisit, TownHostTier(), Bridge.Now(), town + "|" + Bridge.Now() / 60);
            if (t == null || (!force && _rng.Next(100) >= (t.Mood == "greet" ? 40 : 70)))
                return false;   // force: a test skips the roll
            _arrival = new PendingArrival { At = Game.GameTime + (force ? 500 : 3000 + _rng.Next(4000)), Talk = t, Town = town };
            return true;
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
