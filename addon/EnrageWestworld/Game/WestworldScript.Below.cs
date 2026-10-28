// The voice below the floor ([Encounters] VoiceBelow, West World; the words are Core/Below.cs): late at night (23:00-
// 04:00) in a saloon whose barkeep is a post of the town's, now and then (Pacing "below": once a night a town), a
// muffled plea comes up from somebody unseen under the boards behind the bar: a ped spawned below the floor, invisible,
// frozen, who pleads every 15-25 s while you're within 10 m, four times at most; each time a line really played, the
// barkeep's thought shows over his head (the stovepipe, the rats, then worse). Go behind the bar and the voice stops;
// the keeper thinks again, and next morning the town has a word of it. Stand before the barkeep a moment while it goes on
// and he has an answer for you. Nothing is ever found; nobody is harmed; the unseen one is deleted when it's over.
// Unknown until heard in game: whether a line from a ped 2.6 m under an interior's floor is audible (the game's portal
// occlusion), and at what level; nothing muffles it. The game's own white subtitle shows the plea's real words while
// [Output] Subtitles is game or both. Night run 2026-10-28, item 14 (Tier 4's 6).
using System;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private sealed class BelowRun
        {
            public Ped Keeper, Voice;
            public string KeeperName, Seed, Town;
            public Vector3 Boards;
            public int StartedAt, NextAt, Heard, AnsweredAt, OverSince, Silent;
            public bool Test, Stopped, Answered;
            public uint Model;
            public string Pending;           // a thought for the keeper, kept until he's on screen
        }

        private BelowRun _below;
        private int _belowRuns, _belowHeard, _belowLookAt;
        private string _belowLast;
        private Action _aBelow;

        private void BelowStep(int now)
        {
            if (_cfg == null)
                return;
            if (_aBelow == null)
                _aBelow = () => BelowTick(_now);
            Safe("voice below", _aBelow);
        }

        /// A save loaded, the wipe, a mission, the add-on stopping: whatever is under the boards goes, and no news of it.
        private void BelowAbort(string why)
        {
            if (_below != null)
                EndBelow(why, false);
        }

        /// Why not now: a saloon at night with its barkeep near, nobody else of ours going on.
        private string BelowNotNow(bool test, out Ped keeper, out Resident holder)
        {
            keeper = null;
            holder = null;
            if (!PostsOn)
                return "town posts are off (or a mission)";
            if (_placeTown == null)
                return "outside the towns";
            if (!_engine.NowIndoors || _engine.NowPlace != "saloon")
                return "not in a saloon";
            if (!test && !Below.Hour(CLOCK.GET_CLOCK_HOURS()))
                return "the hour (eleven at night to four)";
            if (MISC.GET_MISSION_FLAG())
                return "a mission";
            Ped me = Game.Player.Character;
            foreach (Ped p in World.GetAllPeds())
            {
                if (!p.Exists() || p.Handle == me.Handle || p.Position.DistanceTo(me.Position) > 14f)
                    continue;
                PostKind k = PostKindOf(p);
                if (k == null || k.Kind != "barkeep")
                    continue;
                holder = HolderAt(p, k);
                if (holder == null)
                    continue;
                keeper = p;
                break;
            }
            return keeper == null ? "no barkeep of the town's near" : null;
        }

        private void BelowTick(int now)
        {
            if (!_cfg.WestWorld || !_cfg.VoiceBelow)
            {
                BelowAbort("switched off");
                return;
            }
            if (_below != null)
            {
                BelowLive(_below, now);
                return;
            }
            if (now - _belowLookAt < 20000 && now >= _belowLookAt)
                return;
            _belowLookAt = now;
            Ped keeper; Resident holder;
            if (BelowNotNow(false, out keeper, out holder) != null)
                return;
            string skipped;
            if (Pacing.WhyNot(_store.Pace, "below", _placeTown, null, _store.LastGameMin, _rng.Next(100), false, out skipped) != null)
                return;
            BelowStart(keeper, holder, now, false);
        }

        /// The Testing row: now, whatever the hour.
        private string BelowFire(int now, out string said)
        {
            said = null;
            if (!_cfg.WestWorld || !_cfg.VoiceBelow)
                return "off ([Encounters] VoiceBelow, and West World)";
            if (_below != null)
                return "it's knocking already";
            Ped keeper; Resident holder;
            string not = BelowNotNow(true, out keeper, out holder);
            if (not != null)
                return not;
            BelowStart(keeper, holder, now, true);
            said = "Listen. Stand by the bar a while.";
            return null;
        }

        private void BelowStart(Ped keeper, Resident holder, int now, bool test)
        {
            var b = new BelowRun { Keeper = keeper, KeeperName = holder.Name, Seed = "bl" + now, Town = _placeTown, StartedAt = now, NextAt = now + 4000, Test = test, Model = Spawner.ModelFor(null, false, _rng.Next()) };
            Vector3 k = keeper.Position;
            double hd = ENTITY.GET_ENTITY_HEADING(keeper.Handle) * Math.PI / 180.0;
            b.Boards = k + new Vector3((float)Math.Sin(hd) * 1.2f, (float)-Math.Cos(hd) * 1.2f, 0f);   // a step behind him, behind the bar
            _below = b;
            _belowRuns++;
            if (!test)
                Pacing.Note(_store.Pace, "below", _placeTown, null, _store.LastGameMin);
            Log.Write("BELOW due in the " + _placeTown + " saloon: " + holder.Name + " keeps the bar" + (test ? " (a test)" : ""));
        }

        private void BelowLive(BelowRun b, int now)
        {
            Ped me = Game.Player.Character;
            if (me == null || !me.Exists() || me.IsDead || b.Keeper == null || !b.Keeper.Exists() || b.Keeper.IsDead || MISC.GET_MISSION_FLAG())
            {
                EndBelow("over (" + (b.Keeper == null || !b.Keeper.Exists() ? "the keeper's gone" : "a mission, or you died") + ")");
                return;
            }
            float d = me.Position.DistanceTo(b.Boards);
            if (d > 25f || now - b.StartedAt > 4 * 60000)
            {
                EndBelow(d > 25f ? "you went (" + b.Heard + " heard)" : "the night wore on (" + b.Heard + " heard)");
                return;
            }
            if (b.Pending != null && ENTITY.IS_ENTITY_ON_SCREEN(b.Keeper.Handle))
            {
                _ticker.Push(b.KeeperName + ", thinking", b.Pending, "near", b.Keeper.Handle);   // the keeper's thought, once he's in view
                b.Pending = null;
            }
            if (b.Voice == null)
            {
                if (now - b.StartedAt > 15000)
                {
                    EndBelow("the unseen one never came");
                    return;
                }
                if (!Spawner.Ready(b.Model))
                    return;
                Ped v = Spawner.Spawn(b.Model, b.Boards + new Vector3(0f, 0f, -2.6f), 0f);
                if (v == null)
                    return;
                int h = v.Handle;
                ENTITY.SET_ENTITY_VISIBLE(h, false);
                ENTITY.SET_ENTITY_COLLISION(h, false, false);
                ENTITY.FREEZE_ENTITY_POSITION(h, true);
                ENTITY.SET_ENTITY_INVINCIBLE(h, true);
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, true);
                PED.SET_PED_KEEP_TASK(h, true);
                WEAPON.REMOVE_ALL_PED_WEAPONS(h, true, true);
                b.Voice = v;
                Log.Write("BELOW the unseen one is under the boards (" + (int)d + " m from you)");
                return;
            }
            if (!b.Voice.Exists())
            {
                EndBelow("the unseen one was gone");
                return;
            }
            // behind the bar, over the boards (the keeper's side of the counter, or right on the spot): the voice stops
            Vector3 toMe = me.Position - b.Keeper.Position;
            double kh = ENTITY.GET_ENTITY_HEADING(b.Keeper.Handle) * Math.PI / 180.0;
            bool behind = toMe.X * (float)-Math.Sin(kh) + toMe.Y * (float)Math.Cos(kh) < 0f;   // the keeper faces the room; behind him is his side
            if (!b.Stopped && b.Heard > 0 && (d < 1.2f || behind && d < 2.2f))
            {
                if (b.OverSince == 0)
                    b.OverSince = now;
                else if (now - b.OverSince > 1500)
                {
                    b.Stopped = true;
                    b.Pending = Below.KeeperAfter(b.Seed);
                    b.NextAt = now + 20000;
                    Log.Write("BELOW you stood over the boards (" + d.ToString("0.0") + " m, " + (behind ? "behind the bar" : "on the spot") + "); it stopped (" + b.Heard + " heard)");
                }
            }
            else
                b.OverSince = 0;
            // a word with the keeper while it knocks: he has an answer
            if (!b.Answered && b.Heard > 0 && !b.Stopped && me.Position.DistanceTo(b.Keeper.Position) < 3f && Facing(me, b.Keeper.Position, 60f) && ENTITY.GET_ENTITY_SPEED(me.Handle) < 0.3f)
            {
                if (b.AnsweredAt == 0)
                    b.AnsweredAt = now;
                else if (now - b.AnsweredAt > 2500)
                {
                    b.Answered = true;
                    Feed.Say(b.KeeperName, Below.KeeperAnswer(b.Seed), Speech.When(b.Keeper.Handle, "mild", me.Handle), b.Keeper.Handle, toYou: true);
                    Log.Write("BELOW " + b.KeeperName + " has an answer for you");
                }
            }
            else
                b.AnsweredAt = 0;
            if (b.Stopped)
            {
                if (now >= b.NextAt)
                    EndBelow("quiet now (" + b.Heard + " heard)");
                return;
            }
            if (now < b.NextAt || d > 10f)
                return;
            if (b.Heard >= 4)
            {
                EndBelow("it fell quiet by itself (" + b.Heard + " heard)");
                return;
            }
            string line, words; int take;
            bool spoke = Speech.PlayFirst(b.Voice.Handle, Speech.Plead, 0, out line, out take, out words);
            if (!spoke)
            {
                // nothing played (the voice has no plea, or the game refused): try again, and give up after three
                b.Silent++;
                b.NextAt = now + 8000;
                Log.Detail("BELOW no plea would play (" + b.Silent + ")");
                if (b.Silent >= 3)
                    EndBelow("no voice would play (" + b.Heard + " heard)");
                return;
            }
            b.Pending = Below.KeeperThought(b.Heard, b.Seed);   // shown once he's on screen
            b.Heard++;
            _belowHeard++;
            b.NextAt = now + 15000 + _rng.Next(10000);
            Log.Write("BELOW a plea through the floor (" + line + "); " + b.KeeperName + " thinks of " + (b.Heard == 1 ? "the stovepipe" : "worse"));
        }

        private void EndBelow(string why, bool news = true)
        {
            BelowRun b = _below;
            _below = null;
            if (b == null)
                return;
            if (b.Voice != null && b.Voice.Exists())
                Spawner.Release(b.Voice, true);   // never seen: deleted
            if (news && !b.Test && b.Heard > 0)
                _engine.BelowHeard(b.Town, _store.LastGameMin);
            _belowLast = why;
            Log.Write("BELOW " + why);
        }

        private string BelowStatus()
        {
            if (!_cfg.WestWorld || !_cfg.VoiceBelow)
                return "off ([Encounters] VoiceBelow, West World)";
            return "on (23:00-04:00, a saloon with its barkeep, once a night a town); " + _belowRuns + " nights, " + _belowHeard + " pleas heard" + (_below != null ? "; now: " + _below.Heard + " heard" : "") + (_belowLast != null ? "; last: " + _belowLast : "");
        }
    }
}
