// Plans over days, in the game (the NPC-life run, step 6; Core/Plans.cs). Made each game hour from the ties and the debt
// links (here, not in the Engine, so the Core goldens keep what they test); a plan's holder, near the player with the one
// it's about also near, goes to find them; the street's reasons lean to the scene the plan wants (PlanLean, from
// PairReasons), and a scene of that kind between them settles it (PlanSettled, from NoteHappened); now and then the holder
// thinks of it over their head; and the Show minds card says it. Behind [Mind] MindPlans and MindLive.
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
        private long _plansMadeAt = -1;
        private int _planSeekAt, _planThoughtAt, _plansMade, _plansDone, _planSeeks;
        private readonly Dictionary<string, int> _planSoughtAt = new Dictionary<string, int>();   // holder|target -> when they last set off
        private bool PlansOn { get { return _cfg.MindLive && _cfg.MindPlans && _store != null && _store.Plans != null; } }

        private void PlansStep(Ped me, int now)
        {
            if (!PlansOn)
                return;
            long g = _store.LastGameMin;
            if (_plansMadeAt < 0 || g - _plansMadeAt >= 60 || g < _plansMadeAt)
            {
                _plansMadeAt = g;
                List<string> said = Plans.Make(_store.Plans, _store.Ties, _store.Links, g, id => _store.Find(id) != null);
                foreach (string line in said)
                {
                    if (!line.StartsWith("let go"))
                        _plansMade++;
                    Log.Write("PLAN " + line);
                }
            }
            if (now >= _planSeekAt)
            {
                _planSeekAt = now + 5000;
                PlanSeek(me, now);
            }
            if (now >= _planThoughtAt)
            {
                _planThoughtAt = now + 45000;
                PlanThink(me, now);
            }
        }

        /// The handle of someone the mind knows by id, among the people near the player now (0 if not near).
        private int PlanNearHandle(string id)
        {
            if (id == null)
                return 0;
            foreach (int h in _mindNear)
            {
                MindWho w;
                if (_mindWho.TryGetValue(h, out w) && w.Id == id && ENTITY.DOES_ENTITY_EXIST(h) && !ENTITY.IS_ENTITY_DEAD(h))
                    return h;
            }
            return 0;
        }

        /// A holder near with the one their plan is about also near: they go to find them (once in two minutes).
        private void PlanSeek(Ped me, int now)
        {
            foreach (Plan p in _store.Plans.Where(x => !x.Done))
            {
                int h = PlanNearHandle(p.HolderId), t = h != 0 ? PlanNearHandle(p.TargetId) : 0;
                if (h == 0 || t == 0)
                    continue;
                float d = ENTITY.GET_ENTITY_COORDS(h, true, true).DistanceTo(ENTITY.GET_ENTITY_COORDS(t, true, true));
                if (d < 3f || d > 40f)
                    continue;
                string key = p.HolderId + "|" + p.TargetId;
                int last;
                if (_planSoughtAt.TryGetValue(key, out last) && now - last < 120000 && now >= last)
                    continue;
                Ped hp = new Ped(h);
                if (MindActWhyNot(hp, h) != null || IsMindActing(h))
                    continue;
                _planSoughtAt[key] = now;
                TASK.TASK_GO_TO_ENTITY(h, t, 25000, 1.6f, 2.2f, 0f, 0);
                _planSeeks++;
                Log.Write("PLAN " + p.HolderName + " goes to find " + p.TargetName + " (to " + Plans.Words(p) + ", " + (int)d + " m off)");
                return;   // one a look
            }
        }

        /// Now and then, someone near with a plan thinks of it, over their head.
        private void PlanThink(Ped me, int now)
        {
            foreach (Plan p in _store.Plans.Where(x => !x.Done))
            {
                int h = PlanNearHandle(p.HolderId);
                if (h == 0 || !NodInView(me, new Ped(h)))
                    continue;
                string line = Plans.Thought(p, p.HolderId + "|" + p.Kind + "|" + _store.LastGameMin / 60);
                _ticker.Push(p.HolderName + ", thinking", line, "near", h);
                Log.Write("PLAN thought, " + p.HolderName + ": " + line);
                return;
            }
        }

        /// At the store with a plan to court someone or to thank them: something picked out to give, said over their head.
        private void PlanAtStore(int h, int now)
        {
            if (!PlansOn)
                return;
            MindWho w;
            if (!_mindWho.TryGetValue(h, out w) || w.Id == null)
                return;
            Plan p = _store.Plans.FirstOrDefault(x => !x.Done && x.HolderId == w.Id && (x.Kind == "court" || x.Kind == "repay"));
            if (p == null || p.Gift)
                return;
            p.Gift = true;
            string line = Plans.GiftThought(p, p.HolderId + "|gift|" + _store.LastGameMin / 60);
            _ticker.Push(p.HolderName + ", thinking", line, "near", h);
            Log.Write("PLAN " + p.HolderName + " picks out something at the store for " + p.TargetName + " (to " + Plans.Words(p) + "): " + line);
        }

        /// From PairReasons: a plan between these two leans the street to the scene it wants, or makes a reason where there was
        /// none (the holder starts it).
        private Reason PlanLean(Reason why, Someone a, Someone b)
        {
            if (!PlansOn)
                return why;
            Plan p = Plans.Active(_store.Plans, a.Id, b.Id);
            if (p == null)
                return why;
            bool holderIsB = p.HolderId == b.Id;
            string want = Plans.SceneFor(p.Kind);
            if (why == null && p.Kind == "paydebt")
                return null;   // the debt's own meetings (its worry's link) play the debtor's side; a plan only brings them together
            if (why == null)
                return new Reason { Kind = want, Strength = 55, BFirst = holderIsB, Why = p.HolderName + " means to " + Plans.Words(p), Matter = p.Kind == "geteven" ? "honour" : p.Kind == "paydebt" ? "money" : null };
            if (Plans.Settles(p.Kind, why.Kind))
            {
                why.Strength += 20;   // the street was going there anyway: sooner
                why.Why += "; and " + p.HolderName + " means to " + Plans.Words(p);
            }
            return why;
        }

        /// From NoteHappened: a scene of the kind a plan wants, between its two, settles it.
        private void PlanSettled(List<Ped> people, string kind)
        {
            if (!PlansOn || people == null || people.Count < 2)
                return;
            string aId, aName, bId, bName;
            if (!LastingId(people[0], out aId, out aName) || !LastingId(people[1], out bId, out bName))
                return;
            Plan p = Plans.Active(_store.Plans, aId, bId);
            if (p == null || !Plans.Settles(p.Kind, kind))
                return;
            p.Done = true;
            p.Outcome = kind;
            _plansDone++;
            Log.Write("PLAN done: " + p.HolderName + " meant to " + Plans.Words(p) + ", and did (" + kind + ")");
        }

        /// The Show minds card's line: what they mean to do.
        private string PlanCardLine(string id)
        {
            if (!PlansOn || id == null)
                return null;
            Plan p = _store.Plans.FirstOrDefault(x => !x.Done && x.HolderId == id);
            if (p == null)
                return null;
            long left = Math.Max(0, (p.Due - _store.LastGameMin) / 1440 + 1);
            return "plan: " + Plans.Words(p) + " (" + left + " day" + (left == 1 ? "" : "s") + " left)";
        }

        private string PlansStatus()
        {
            if (!PlansOn)
                return "off ([Mind] MindPlans, with MindLive)";
            int active = _store.Plans.Count(p => !p.Done);
            return active + " plans afoot (" + string.Join(", ", _store.Plans.Where(p => !p.Done).GroupBy(p => p.Kind).Select(g => g.Key + " " + g.Count())) + "); this session " +
                   _plansMade + " made, " + _plansDone + " carried out, " + _planSeeks + " set off to find someone";
        }
    }
}
