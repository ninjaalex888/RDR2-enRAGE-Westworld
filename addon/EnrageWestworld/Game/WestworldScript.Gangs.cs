// Rival gangs who meet near the player fight it out (Lex, 2026-10-30: "the enrage mod does this a bit with the rival
// gangs attacking each other if we can build on that"; staged shootouts for now, the MindsEye to drive them in time). Every
// 10 s, men of two different gangs within 90 m of the player (Factions; never the Wapiti, the story's people, or men enRAGE
// has busy; the army and the Pinkertons are one side): with a chance, more where enRAGE has lately reported a war or a
// clash between the two, the nearest of each go toward the other and shout, a moment's standoff with guns on each other,
// then every man of both within 50 m fights the nearest of the other side, until a side is beaten or gone, or a minute.
// Behind [World] GangShootouts.
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
        private sealed class GangFight
        {
            public string A, B;
            public List<int> SideA = new List<int>(), SideB = new List<int>();
            public int LeadA, LeadB;
            public int Phase, At, Until, Started, DeadA, DeadB;
        }

        private GangFight _gang;
        private int _gangNextAt, _gangFights;
        private string _gangLast;
        private readonly Dictionary<string, long> _gangTried = new Dictionary<string, long>();
        private const float GangScanMetres = 90f, GangJoinMetres = 50f;

        private static string GangSide(string member)
        {
            return member == "Soldier" || member == "Pinkerton" ? "the law" : member;
        }

        private static string Many(string member)
        {
            switch (member)
            {
                case "Lemoyne Raider": return "Raiders";
                case "Night Folk": return "Night Folk";
                case "Bronte man": return "Bronte's men";
                case "Braithwaite man": return "Braithwaite men";
                case "Laramie": return "Laramie boys";
                case "Soldier": return "soldiers";
                default: return member + "s";
            }
        }

        private void GangStep(int now)
        {
            if (!_cfg.GangShootouts || MISC.GET_MISSION_FLAG())
            {
                if (_gang != null)
                    GangEnd("a mission or the switch");
                return;
            }
            if (_gang != null)
            {
                try
                {
                    GangTick(now);
                }
                catch (Exception ex)
                {
                    _gang = null;
                    Log.Detail("GANG failed: " + ex.Message);
                }
                return;
            }
            if (now < _gangNextAt)
                return;
            _gangNextAt = now + 10000;
            Ped me = Game.Player.Character;
            Vector3 at = me.Position;
            var byGang = new Dictionary<string, List<Ped>>();
            foreach (Ped p in World.GetAllPeds())
            {
                if (p == null || p.Position.DistanceTo(at) > GangScanMetres)
                    continue;   // the distance first
                int h = p.Handle;
                if (h == me.Handle || !p.Exists() || p.IsDead || !PED.IS_PED_HUMAN(h) || Natives.StoryModel(p) || PED.IS_PED_IN_COMBAT(h, 0) || _director.Involves(h))
                    continue;
                string g = Factions.MemberOf(ENTITY.GET_ENTITY_MODEL(h), PED.GET_PED_RELATIONSHIP_GROUP_HASH(h));
                if (g == null || g == "Wapiti" || MayDirectWhyNot(p) != null)
                    continue;
                string side = GangSide(g);
                List<Ped> list;
                if (!byGang.TryGetValue(side, out list))
                    byGang[side] = list = new List<Ped>();
                list.Add(p);
            }
            if (byGang.Count < 2)
                return;
            // the two sides with men nearest each other
            string bestA = null, bestB = null;
            Ped pa = null, pb = null;
            float best = GangJoinMetres;   // the nearest two of the two sides within 50 m of each other, or no meeting
            var sides = byGang.Keys.ToList();
            for (int i = 0; i < sides.Count; i++)
                for (int j = i + 1; j < sides.Count; j++)
                    foreach (Ped x in byGang[sides[i]])
                        foreach (Ped y in byGang[sides[j]])
                        {
                            float d = x.Position.DistanceTo(y.Position);
                            if (d < best)
                            {
                                best = d;
                                bestA = sides[i];
                                bestB = sides[j];
                                pa = x;
                                pb = y;
                            }
                        }
            if (pa == null)
                return;
            long g0 = _store.LastGameMin;
            string key = string.CompareOrdinal(bestA, bestB) < 0 ? bestA + "|" + bestB : bestB + "|" + bestA;
            long tried;
            if (_gangTried.TryGetValue(key, out tried) && g0 - tried >= 0 && g0 - tried < 30)
                return;   // these two passed each other by lately: not again for half a game hour
            _gangTried[key] = g0;
            bool war = _engine.AtWar(bestA, bestB, g0);
            int chance = war ? 70 : 30;
            if (!Voice.Chance(key + "|" + g0 / 30, chance))
            {
                Log.Detail("GANG " + Many(bestA) + " and " + Many(bestB) + " pass each other by (" + chance + "%" + (war ? ", at war" : "") + ")");
                return;
            }
            var f = new GangFight { A = bestA, B = bestB, LeadA = pa.Handle, LeadB = pb.Handle, Phase = 0, Started = now, At = now, Until = now + 75000 };
            // only those of each side within reach of the meeting: a shootout of men 90 m apart ended before it began (Rhodes)
            Vector3 meet = (pa.Position + pb.Position) * 0.5f;
            f.SideA.AddRange(byGang[bestA].Where(p => p.Position.DistanceTo(meet) < GangJoinMetres).Select(p => p.Handle));
            f.SideB.AddRange(byGang[bestB].Where(p => p.Position.DistanceTo(meet) < GangJoinMetres).Select(p => p.Handle));
            _gang = f;
            _gangFights++;
            _gangLast = Many(bestA) + " against " + Many(bestB);
            Log.Write("GANG " + Many(bestA) + " (" + f.SideA.Count + ") and " + Many(bestB) + " (" + f.SideB.Count + ") meet " + (int)pa.Position.DistanceTo(at) + " m from you" +
                      (war ? ", at war by enRAGE's word" : "") + ": a shootout");
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(pa.Handle, true);
            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(pb.Handle, true);
            TASK.TASK_GO_TO_ENTITY(pa.Handle, pb.Handle, 12000, 2f, 16f, 0f, 0);
            TASK.TASK_GO_TO_ENTITY(pb.Handle, pa.Handle, 12000, 2f, 16f, 0f, 0);
        }

        private void GangTick(int now)
        {
            GangFight f = _gang;
            Ped me = Game.Player.Character;
            bool aOk = GangAlive(f.LeadA), bOk = GangAlive(f.LeadB);
            if (now > f.Until)
            {
                GangEnd("a minute's shooting");
                return;
            }
            if (f.Phase < 2 && (!aOk || !bOk))
                f.Phase = 2;   // a lead down before the word: it's begun
            if (f.Phase == 0)
            {
                // closing in, then the words
                float d = aOk && bOk ? ENTITY.GET_ENTITY_COORDS(f.LeadA, true, true).DistanceTo(ENTITY.GET_ENTITY_COORDS(f.LeadB, true, true)) : 0f;
                if (d > 22f && now - f.Started < 12000)
                    return;
                f.Phase = 1;
                f.At = now + 3500;
                string seed = f.LeadA + "|" + f.LeadB;
                GangShout(f.LeadA, Voice.Pick(seed + "|a", Many(f.B) + "! This is our ground!", "Well now. " + Many(f.B) + ", a long way from home.", "Turn around and ride, " + Many(f.B) + ".", "You're on the wrong road, friend."), f.LeadB);
                GangShout(f.LeadB, Voice.Pick(seed + "|b", "Make us.", "Ground's whoever can hold it.", "Big talk for so few.", "Draw, then."), f.LeadA);
                foreach (int h in new[] { f.LeadA, f.LeadB })
                {
                    uint gun = WEAPON.GET_BEST_PED_WEAPON(h, false, false);
                    if (gun != 0 && gun != Natives.Hash("WEAPON_UNARMED"))
                        WEAPON.SET_CURRENT_PED_WEAPON(h, gun, true, 0, false, false);
                }
                TASK.TASK_AIM_GUN_AT_ENTITY(f.LeadA, f.LeadB, 4000, false, 0);
                TASK.TASK_AIM_GUN_AT_ENTITY(f.LeadB, f.LeadA, 4000, false, 0);
                return;
            }
            if (f.Phase == 1)
            {
                if (now < f.At)
                    return;
                f.Phase = 2;
                f.At = now;
                Log.Write("GANG the shooting starts: " + Many(f.A) + " against " + Many(f.B));
            }
            // the fight: everyone of both within 50 m of the leads goes for the nearest of the other side; looked at every 2 s
            if (now < f.At)
                return;
            f.At = now + 2000;
            Vector3 mid = ENTITY.GET_ENTITY_COORDS(aOk ? f.LeadA : f.LeadB, true, true);
            List<int> a = f.SideA.Where(h => GangAlive(h) && ENTITY.GET_ENTITY_COORDS(h, true, true).DistanceTo(mid) < GangJoinMetres).ToList();
            List<int> b = f.SideB.Where(h => GangAlive(h) && ENTITY.GET_ENTITY_COORDS(h, true, true).DistanceTo(mid) < GangJoinMetres).ToList();
            f.DeadA = f.SideA.Count(h => ENTITY.DOES_ENTITY_EXIST(h) && ENTITY.IS_ENTITY_DEAD(h));
            f.DeadB = f.SideB.Count(h => ENTITY.DOES_ENTITY_EXIST(h) && ENTITY.IS_ENTITY_DEAD(h));
            if (a.Count == 0 || b.Count == 0)
            {
                GangEnd(a.Count == 0 ? Many(f.B) + " hold the ground" : Many(f.A) + " hold the ground");
                return;
            }
            GangOrder(a, b);
            GangOrder(b, a);
        }

        private static void GangOrder(List<int> mine, List<int> theirs)
        {
            foreach (int h in mine)
            {
                if (PED.IS_PED_IN_COMBAT(h, 0))
                    continue;   // already at someone (the game's AI picks well once it's going)
                Vector3 p = ENTITY.GET_ENTITY_COORDS(h, true, true);
                int target = theirs.OrderBy(x => ENTITY.GET_ENTITY_COORDS(x, true, true).DistanceTo(p)).First();
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, false);
                uint gun = WEAPON.GET_BEST_PED_WEAPON(h, false, false);
                if (gun != 0 && gun != Natives.Hash("WEAPON_UNARMED"))
                    WEAPON.SET_CURRENT_PED_WEAPON(h, gun, true, 0, false, false);
                PED.SET_PED_COMBAT_ATTRIBUTES(h, 46, true);   // fights it out
                TASK.TASK_COMBAT_PED(h, target, 0, 16);
            }
        }

        private void GangShout(int h, string line, int to)
        {
            if (!GangAlive(h))
                return;   // shot before he said it
            Speech.SayTag(h, "angry", to);
            if (_cfg.ScenesOnTicker && _cfg.Ticker && ENTITY.GET_ENTITY_COORDS(h, true, true).DistanceTo(Game.Player.Character.Position) <= _cfg.ScenesOnTickerMetres)
                SpeechCaption(Grammar.UpperFirst(Factions.AOne(Factions.MemberOf(ENTITY.GET_ENTITY_MODEL(h), PED.GET_PED_RELATIONSHIP_GROUP_HASH(h))) ?? "A man"), line, h, 3500);
            Log.Write("GANG   " + line);
        }

        private void GangEnd(string why)
        {
            GangFight f = _gang;
            _gang = null;
            if (f == null)
                return;
            _gangNextAt = Game.GameTime + 300000;   // one shootout in five minutes at most
            foreach (int h in f.SideA.Concat(f.SideB))
                if (ENTITY.DOES_ENTITY_EXIST(h) && !ENTITY.IS_ENTITY_DEAD(h))
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, false);
            Log.Write("GANG over: " + why + " (" + Many(f.A) + " lost " + f.DeadA + ", " + Many(f.B) + " lost " + f.DeadB + ")");
        }

        private static bool GangAlive(int h)
        {
            return h != 0 && ENTITY.DOES_ENTITY_EXIST(h) && !ENTITY.IS_ENTITY_DEAD(h);
        }

        /// F7: the gangs' shootouts this session.
        private string GangStatus()
        {
            if (!_cfg.GangShootouts)
                return "off ([World] GangShootouts)";
            return _gangFights + " shootout" + (_gangFights == 1 ? "" : "s") + " between gangs this session" + (_gangLast != null ? " (last: " + _gangLast + ")" : "") + (_gang != null ? "; one going on" : "");
        }
    }
}
