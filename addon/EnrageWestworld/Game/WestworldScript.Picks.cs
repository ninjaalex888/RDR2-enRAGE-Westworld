// The model picks what someone near does (Lex, 2026-10-30: "let it pick between a few safe options and go from there";
// Core/Language.Pick.cs holds the prompt and the reading of the answer). When the mind's choice changes for someone
// close by, it isn't acted on at once: the best three of the proper actions (and a plan's own step, when the one it's
// about is near) go to the model with who they are and how they feel, and what it picks is done. It waits 2.5 s at most;
// late, empty or unreadable, the mind's own choice is done, as before. Only with the server answering, only the nearest
// few, never a choice a strong feeling made. Behind [Mind] Language, LanguagePicks and MindLive.
using System;
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private sealed class PendingPick
        {
            public MindWho W;
            public int Handle, Deadline, AskedAt;
            public string Key, Mind;
            public List<string> Ids, Words;
            public Plan Plan;
        }

        private const float PickMetres = 15f;
        private const int PickWaitMs = 2500, PicksAtOnce = 4, PicksLoud = 40;
        private readonly List<PendingPick> _pending = new List<PendingPick>();
        private int _picksAsked, _picksTaken, _picksSame, _picksLate, _picksUnread, _picksPlan, _picksLogged;
        private string _pickLast;

        private bool PicksOn { get { return _cfg.MindLive && _cfg.Language && _cfg.LanguagePicks && _langReady && _engine != null; } }

        /// From MindChooseFor, when someone near has a new choice: the model is asked to pick, and true (it's acted on when the
        /// answer comes, or the wait is up); false to act on the mind's choice now.
        private bool PickAsk(MindWho w, int h, int now, ChoiceContext c)
        {
            if (!PicksOn || w == null || w.Id == null || w.Choice == null || _pending.Count >= PicksAtOnce || w.Choice.StartsWith("urge:", StringComparison.Ordinal))
                return false;
            if (w.ChoiceWhy != null && w.ChoiceWhy.StartsWith("a feeling", StringComparison.Ordinal))
                return false;   // a feeling strongly felt decides, not the model
            if (_pending.Any(x => x.Handle == h) || IsMindActing(h))
                return false;
            Ped p = new Ped(h);
            if (!p.Exists() || p.Position.DistanceTo(Game.Player.Character.Position) > PickMetres || MindActWhyNot(p, h) != null)
                return false;
            List<KeyValuePair<string, string>> ranked = Choosing.Ranked(w.Needs, w.Traits, c, Choosing.SeedFor(w.Id, _store.LastGameMin), 3);
            var ids = new List<string>();
            var words = new List<string>();
            foreach (KeyValuePair<string, string> kv in ranked)
            {
                ids.Add(kv.Key);
                words.Add(Choosing.WordsOf(kv.Key));
            }
            if (!ids.Contains(w.Choice))
            {
                ids.Insert(0, w.Choice);   // the mind's own is always among them
                words.Insert(0, Choosing.WordsOf(w.Choice));
            }
            int th;
            float td;
            Plan plan = PlanSeekable(w.Id, h, now, out th, out td);
            if (plan != null && ids.Count < Language.MostOptions + 1)
            {
                ids.Add("plan:seek");
                words.Add("goes to find " + Person.FirstOf(plan.TargetName ?? "them") + ", " + (int)td + " m off, meaning to " + Plans.Words(plan));
            }
            while (ids.Count > Language.MostOptions)
            {
                int drop = ids.Count - 2;   // keep the mind's own and the plan's step: the lowest of the rest goes
                if (ids[drop] == w.Choice)
                    drop--;
                ids.RemoveAt(drop);
                words.RemoveAt(drop);
            }
            if (ids.Count < 2)
                return false;
            string why;
            LanguageRequest r = MindRequestOf(w, h, now, out why);
            if (r == null)
                return false;
            r.Kind = "pick";
            r.Options = words;
            string key = "pick|" + w.Id + "|" + Environment.TickCount + "|" + _picksAsked;
            if (!_engine.LanguageAnswers.AskPick(new LanguageAsk { Key = key, Name = w.Name, PersonId = w.Id, Request = r, AskedAtMs = Environment.TickCount }))
                return false;
            _picksAsked++;
            _pending.Add(new PendingPick { W = w, Handle = h, Key = key, Mind = w.Choice, Ids = ids, Words = words, Plan = ids.Contains("plan:seek") ? plan : null, AskedAt = now, Deadline = now + PickWaitMs });
            return true;
        }

        /// Each tick: the picks that came back, or whose wait is up, acted on.
        private void PicksStep(int now)
        {
            for (int i = _pending.Count - 1; i >= 0; i--)
            {
                PendingPick pp = _pending[i];
                string answer = null;
                long ms = 0;
                bool came = _engine != null && _engine.LanguageAnswers.TakePick(pp.Key, out answer, out ms);
                if (!came && now < pp.Deadline && now >= pp.AskedAt)
                    continue;
                _pending.RemoveAt(i);
                if (!came)
                {
                    answer = null;
                    ms = now - pp.AskedAt;
                }
                try
                {
                    PickResolve(pp, came, answer, ms, now);
                }
                catch (Exception ex)
                {
                    Log.Write("MIND pick failed for " + pp.W.Name + ": " + ex.Message);
                }
            }
        }

        private void PickResolve(PendingPick pp, bool came, string answer, long ms, int now)
        {
            MindWho w = pp.W;
            int h = pp.Handle;
            if (!ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h) || w.Choice != pp.Mind)
                return;   // gone, or the mind has moved on since
            int idx = came ? Language.ParsePick(answer, pp.Ids.Count) : -1;
            string offered = string.Join(" | ", pp.Words.Select((x, k) => (k + 1) + " " + x));
            string outcome;
            if (!came)
            {
                _picksLate++;
                outcome = "no answer in " + PickWaitMs / 1000.0 + " s: the mind's own";
            }
            else if (idx < 0)
            {
                _picksUnread++;
                outcome = "\"" + (answer ?? "").Trim() + "\" names none of them: the mind's own";
            }
            else
            {
                _picksTaken++;
                outcome = (idx + 1) + " (" + pp.Words[idx] + ")" + (pp.Ids[idx] == pp.Mind ? ", the mind's own too" : ", not the mind's (" + Choosing.WordsOf(pp.Mind) + ")");
            }
            (_picksLogged++ < PicksLoud ? (Action<string>)Log.Write : Log.Detail)("MIND pick: " + w.Name + " offered " + offered + " -> " + outcome + ", " + ms + " ms");
            string picked = idx >= 0 ? pp.Ids[idx] : pp.Mind;
            _pickLast = w.Name + ": " + (idx >= 0 ? pp.Words[idx] : Choosing.WordsOf(pp.Mind) + " (the mind's)");
            if (picked == pp.Mind)
            {
                if (idx >= 0)
                    _picksSame++;
                MindActOn(w, h, now);
                return;
            }
            if (picked == "plan:seek")
            {
                int t;
                float d;
                Plan p = PlanSeekable(w.Id, h, now, out t, out d);
                Ped hp = new Ped(h);
                if (p != null && p == pp.Plan && MindActWhyNot(hp, h) == null && !IsMindActing(h))
                {
                    _picksPlan++;
                    PlanSeekNow(p, h, t, d, now, "the model's pick");
                    return;
                }
                MindActOn(w, h, now);   // the one it's about has gone: the mind's own
                return;
            }
            // the model's pick in place of the mind's: the choice, what they did lately and why
            if (w.Recent != null && w.Recent.Id[0] == pp.Mind)
                w.Recent.Id[0] = picked;
            w.Choice = picked;
            w.ChoiceWhy = "the model's pick, over " + Choosing.WordsOf(pp.Mind);
            w.ChoiceTold = false;
            MindActOn(w, h, now);
        }

        private string PicksStatus()
        {
            if (!_cfg.LanguagePicks)
                return "off ([Mind] LanguagePicks)";
            if (!(_cfg.MindLive && _cfg.Language))
                return "waiting on [Mind] Language and MindLive";
            string store = _engine != null ? ", store: " + _engine.LanguageAnswers.PicksAsked + " queued, " + _engine.LanguageAnswers.PicksAnswered + " answered, " + _engine.LanguageAnswers.PicksFailed + " failed, " +
                           _engine.LanguageAnswers.PicksDropped + " dropped" : "";
            return (_langReady ? "on" : "on, the server not ready") + ": " + _picksAsked + " asked this session, " + _picksTaken + " picked (" + _picksSame + " the mind's own, " + _picksPlan + " a plan's step), " +
                   _picksLate + " late, " + _picksUnread + " unreadable, " + _pending.Count + " waiting" + store + (_pickLast != null ? "; last: " + _pickLast : "");
        }
    }
}
