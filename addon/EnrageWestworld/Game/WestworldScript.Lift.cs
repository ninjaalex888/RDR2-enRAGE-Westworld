// A thief lifts your money (Lex, 2026-10-29, after the game's own: "an NPC that I bumped into took money from me and ran
// off... this should be able to happen anywhere"; [Scenes] Lifts). Anywhere there are people (a town, a camp, a road; Lex: "not just in towns"), a man near you walks into you, says his
// pardon, and in the bump takes a little of your cash; a moment later he runs. He is the pickpocket from then on
// (the same Theft as a street pickpocket's, with you as the one robbed): run him down, lasso, hogtie or tackle him and
// you get it back; a lawman near may chase and bring him down for you (LawChases), and hands it back; if he gets away
// it's gone. A look every 60 s while you're on foot, Pacing "lift" (25%, two game hours between, a town
// every four), never in a mission, never with a scene or a theft already on. The Testing row starts one now.
using System;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private Ped _lifter;
        private int _lifterSince, _liftLookAt, _lifts, _liftsBack;
        private uint _lifterModel;
        private Ped _liftRun;
        private int _liftRunAt, _liftRunTake;
        private string _liftLast;

        /// From the pickpockets' tick: the lift's own steps (the walk into you), and a look for a new one now and then.
        private void LiftStep(int now)
        {
            if (_liftRun != null && now >= _liftRunAt)
            {
                Ped r = _liftRun;
                _liftRun = null;
                if (r.Exists() && !r.IsDead)
                {
                    // a red dot, as the game's own pickpocket is: hostile to you, so catching him (or worse) is no crime
                    // (Lex, 2026-10-29: "he should be a red dot so if the main character catches him, he can kill him or
                    // beat him up with no Wanted bounty, as well as easier to track on the minimap")
                    MarkThief(r, Game.GameTime);
                    TASK.CLEAR_PED_TASKS(r.Handle, true, false);
                    TASK.TASK_SMART_FLEE_PED(r.Handle, Game.Player.Character.Handle, 200f, -1, 0, 3f, 0);
                }
                Feed.Notice("Pickpocketed", "Your pocket's lighter by " + Cents(_liftRunTake) + ". The man who bumped you is running.");
            }
            if (_lifter != null)
            {
                LiftApproach(now);
                return;
            }
            if (!_cfg.Lifts || _theft != null || now < _liftLookAt)
                return;
            _liftLookAt = now + 60000;
            string why;
            LiftStart(now, false, out why);
        }

        /// Someone near is sent to bump into you. Null when one started, else why not.
        private string LiftStart(int now, bool test, out string why)
        {
            why = null;
            Ped me = Game.Player.Character;
            if (!test && !_cfg.Lifts)
                return why = "off ([Scenes] Lifts)";
            if (_lifter != null || _theft != null)
                return why = "a thief is at work already";
            if (MISC.GET_MISSION_FLAG() || PED.IS_PED_ON_MOUNT(me.Handle) || PED.IS_PED_IN_ANY_VEHICLE(me.Handle, false))
                return why = "on a horse, in a wagon or in a mission";
            if (MONEY._MONEY_GET_CASH_BALANCE() < 200)
                return why = "your pockets are near empty (under $2)";
            long g = _store.LastGameMin;
            string skipped;
            string pace = Pacing.WhyNot(_store.Pace, "lift", _placeTown, null, g, _rng.Next(100), test, out skipped);
            if (pace != null)
                return why = pace;
            Ped pick = World.GetAllPeds().Where(p => Natives.Bystander(p, me.Handle) && !Spawner.IsOurs(p.Handle) && !PED.IS_PED_IN_ANY_VEHICLE(p.Handle, false) &&
                                                     !PED.IS_PED_ON_MOUNT(p.Handle) && PED.IS_PED_MALE(p.Handle) && IsGrownUp(p) && !Natives.StoryModel(p) && !NotFree(p.Handle) &&
                                                     MayDirectWhyNot(p) == null && _bridge.RecordAt(p.Handle) == null && PostKindOf(p) == null &&
                                                     p.Position.DistanceTo(me.Position) > 4f && p.Position.DistanceTo(me.Position) < 22f)
                                         .OrderBy(p => p.Position.DistanceTo(me.Position)).FirstOrDefault();
            if (pick == null)
                return why = "no man near (4-22 m) free to send";
            _lifter = pick;
            _lifterSince = now;
            _lifterModel = ENTITY.GET_ENTITY_MODEL(pick.Handle);
            Pacing.Note(_store.Pace, "lift", _placeTown, null, g);
            NoteInScene(new[] { pick }, "lift", now);
            if (PED.IS_PED_USING_ANY_SCENARIO(pick.Handle))
                TASK.CLEAR_PED_TASKS(pick.Handle, true, false);
            TASK.TASK_GO_TO_ENTITY(pick.Handle, me.Handle, -1, 1.3f, 0.4f, 0f, 0);   // an ordinary walk: a man going his way
            Log.Write("LIFT a man " + (int)pick.Position.DistanceTo(me.Position) + " m off walks into your path" + (test ? " (a test; skipped: " + skipped + ")" : ""));
            return null;
        }

        /// The walk into you, the bump and the lift, then the run.
        private void LiftApproach(int now)
        {
            Ped me = Game.Player.Character;
            Ped p = _lifter;
            if (p == null || !p.Exists() || p.IsDead || ENTITY.GET_ENTITY_MODEL(p.Handle) != _lifterModel || now - _lifterSince > 30000 || _director.Involves(p.Handle))
            {
                if (p != null && p.Exists() && !p.IsDead)
                    WanderLater(p.Handle, now);
                _lifter = null;
                Log.Write("LIFT given up: he never reached you");
                return;
            }
            if ((now - _lifterSince) % 3000 < 60)
                TASK.TASK_GO_TO_ENTITY(p.Handle, me.Handle, -1, 1.3f, 0.4f, 0f, 0);
            if (p.Position.DistanceTo(me.Position) > 1.3f)
                return;
            // the bump: his pardon, your cash
            int cash = MONEY._MONEY_GET_CASH_BALANCE();
            int take = Math.Max(100, Math.Min(2500, cash / 5));   // a fifth of it, $1 to $25 (cents)
            take = Math.Min(take, cash);
            MONEY._MONEY_DECREMENT_CASH_BALANCE(take);
            int h = p.Handle;
            _lifter = null;
            _lifts++;
            string sorry = Voice.Pick(now.ToString(), "Pardon me, mister.", "Beg your pardon.", "Excuse me. Clumsy of me.");
            string line, said;
            int t;
            Speech.PlayFirst(h, new[] { "GENERIC_SORRY", "BUMP", "GENERIC_EXCUSE_ME" }, me.Handle, out line, out t, out said);
            if (_cfg.ScenesOnTicker && _cfg.Ticker)
                _ticker.Say(Grammar.UpperFirst(Director.Kind(p, false)), sorry, h, 2500);
            _theft = new Theft
            {
                Thief = p, Mark = null, MarkName = null, Town = _placeTown, Started = now, ThiefHealth = Math.Max(1, ENTITY.GET_ENTITY_HEALTH(h)),
                PlayerMark = true, Amount = take
            };
            _thieves.Add(h);
            _liftRun = p;
            _liftRunAt = now + 1200;   // a moment's pause, then he runs
            _liftRunTake = take;
            Log.Write("LIFT the man bumped you and took " + Cents(take) + " (cash was " + Cents(cash) + ")" + (line != null ? "; he said " + line : ""));
        }

        /// The money back, or not, when the theft of your own pocket ends (from ResolveTheft and the law's catch).
        private void LiftResolved(Theft t, string how)
        {
            if (!t.PlayerMark)
                return;
            if (how == "escaped")
            {
                Feed.Notice("Pickpocketed", "He got away with your " + Cents(t.Amount) + ".");
                _liftLast = "got away with " + Cents(t.Amount);
            }
            else
            {
                MONEY._MONEY_INCREMENT_CASH_BALANCE(t.Amount, 0);
                _liftsBack++;
                Feed.Notice("Your money back", how == "law" ? "The deputy hands you back your " + Cents(t.Amount) + "." : "You take back your " + Cents(t.Amount) + ".");
                _liftLast = (how == "law" ? "the law caught him" : how == "killed" ? "killed" : "caught") + ", " + Cents(t.Amount) + " back";
            }
            Log.Write("LIFT " + _liftLast);
        }

        /// The thief as a hostile on the map: a red blip, his relationship to you hate (the game's law takes no notice of what
        /// you do to him), and counted as one who came at you for the add-on's own self-defence rule. He still runs: no combat
        /// attributes are set.
        private unsafe void MarkThief(Ped p, int now)
        {
            int h = p.Handle;
            int me = Game.Player.Character.Handle;
            if (_hostileGroup == 0)
            {
                ulong slot = 0;   // out-parameters take 8 bytes
                PED.ADD_RELATIONSHIP_GROUP("REALNPCS_HOSTILE", (uint*)&slot);
                _hostileGroup = (uint)slot;
            }
            uint playerGroup = PED.GET_PED_RELATIONSHIP_GROUP_HASH(me);
            PED.SET_RELATIONSHIP_BETWEEN_GROUPS(5, _hostileGroup, playerGroup);
            PED.SET_RELATIONSHIP_BETWEEN_GROUPS(5, playerGroup, _hostileGroup);
            PED.SET_PED_RELATIONSHIP_GROUP_HASH(h, _hostileGroup);
            _cameAtYouSeen[h] = now;
            if (_theft != null && _theft.Thief != null && _theft.Thief.Handle == h)
            {
                try
                {
                    _theft.Blip = p.AddBlip(BlipType.WhiteDot);
                    MAP.BLIP_ADD_MODIFIER(_theft.Blip.Handle, Natives.Hash("BLIP_MODIFIER_ENEMY"));   // red, as an enemy's
                    MAP.BLIP_ADD_MODIFIER(_theft.Blip.Handle, Natives.Hash("BLIP_MODIFIER_ENEMY_FLEEING"));
                    _theft.Blip.Label = "Pickpocket";
                }
                catch (Exception ex)
                {
                    Log.Write("LIFT the red mark failed: " + ex.Message);
                }
            }
        }

        private string LiftStatus()
        {
            return (_cfg.Lifts ? "on" : "off ([Scenes] Lifts)") + "; " + _lifts + " lifts this session, " + _liftsBack + " got back" + (_lifter != null ? "; a man walking into you now" : "") + (_liftLast != null ? "; last: " + _liftLast : "");
        }
    }
}
