// Thoughts said aloud in their own recorded voice (Lex, 2026-10-30, option 1; Core/ThoughtVoice.cs). As a thought shows
// over the head of someone within 12 m whose voice the add-on knows, the model is asked which of that voice's own remarks
// fits it, if any; a pick is played in their voice, with its words in the lower subtitle box when the word list has them.
// Most thoughts stay unspoken (the model's 0). One every 20 s at most, each person once in four minutes; never someone
// already speaking or in a scene. Behind [Mind] ThoughtsAloud, with Language and MindLive.
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
        private sealed class PendingAloud
        {
            public string Name, Key, Key2, Bank, Thought;
            public bool Came1, Came2;
            public string Answer1, Answer2;
            public long Ms;
            public int Handle, AskedAt, Deadline;
            public List<AloudOption> Options;
        }

        private const float AloudMetres = 12f;
        private const int AloudGapMs = 20000, AloudEachMs = 240000, AloudWaitMs = 3000;
        private PendingAloud _aloud;
        private int _aloudNextAt, _aloudAsked, _aloudSaid, _aloudKept, _aloudLate, _aloudUnread, _aloudLogged;
        private string _aloudLast;
        private readonly Dictionary<string, int> _aloudAt = new Dictionary<string, int>();

        /// A thought just came up on someone near: ask whether they say any of it aloud.
        private void AloudAsk(TickerLine line, int now)
        {
            if (!_cfg.ThoughtsAloud || !_cfg.MindLive || !_cfg.Language || !_langReady || _engine == null || _aloud != null || line == null || line.Kind != "near" || line.Key == null || string.IsNullOrEmpty(line.Text))
                return;
            if (now < _aloudNextAt || Speech.Book == null)
                return;
            if (!Voice.Chance(line.Key + "|aloud|" + line.Text, 50))
                return;   // half the thoughts are never offered: most stay unspoken whatever the model would say
            int at;
            if (_aloudAt.TryGetValue(line.Key, out at) && now - at < AloudEachMs && now >= at)
                return;
            NearThought n = _near.FirstOrDefault(x => x.Key == line.Key);
            int h = n != null ? n.Handle : line.Handle;
            if (h == 0 || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h) || Speech.Busy(h) || _director.Involves(h) || Feed.Talking)
                return;
            if (new Ped(h).Position.DistanceTo(Game.Player.Character.Position) > AloudMetres)
                return;
            MindWho w;
            if (!_mindWho.TryGetValue(h, out w) || w.Id == null)
                return;
            string bank = Speech.BankOf(h);
            if (bank == null)
                return;   // a voice we can't be sure of: no list of its remarks to offer
            List<AloudOption> options = ThoughtVoice.Options(l => Speech.Book.Takes(bank, l), (l, t) => Speech.Book.Words(bank, l, t), w.Id + "|" + now / 60000);
            if (options.Count < 2)
                return;
            string why;
            LanguageRequest r = MindRequestOf(w, h, now, out why);
            if (r == null)
                return;
            r.Kind = "say";
            r.Thought = line.Text;
            r.Options = options.Select(o => o.Text).ToList();
            string key = "say|" + w.Id + "|" + Environment.TickCount;
            // asked twice: said aloud only when both answers name the same remark (the 2026-10-30 trial: an 8B model sure of a
            // fit answers alike each time; unsure, it wanders, and then it's better unsaid)
            if (!_engine.LanguageAnswers.AskPick(new LanguageAsk { Key = key, Name = w.Name, PersonId = w.Id, Request = r, AskedAtMs = Environment.TickCount }))
                return;
            _engine.LanguageAnswers.AskPick(new LanguageAsk { Key = key + "|2", Name = w.Name, PersonId = w.Id, Request = r, AskedAtMs = Environment.TickCount });
            _aloudAt[line.Key] = now;
            if (_aloudAt.Count > 200)
                _aloudAt.Clear();
            _aloudNextAt = now + AloudGapMs;
            _aloudAsked++;
            _aloud = new PendingAloud { Name = w.Name, Key = key, Key2 = key + "|2", Bank = bank, Thought = line.Text, Handle = h, AskedAt = now, Deadline = now + AloudWaitMs, Options = options };
        }

        /// Each tick: the answer, when it has come, played in their voice (or kept in).
        private void AloudStep(int now)
        {
            PendingAloud a = _aloud;
            if (a == null)
                return;
            string answer = null;
            long ms = 0;
            if (!a.Came1 && _engine != null && _engine.LanguageAnswers.TakePick(a.Key, out answer, out ms))
            {
                a.Came1 = true;
                a.Answer1 = answer;
                a.Ms = ms;
            }
            if (!a.Came2 && _engine != null && _engine.LanguageAnswers.TakePick(a.Key2, out answer, out ms))
            {
                a.Came2 = true;
                a.Answer2 = answer;
                a.Ms = Math.Max(a.Ms, ms);
            }
            bool came = a.Came1 && a.Came2;
            if (!came && now < a.Deadline && now >= a.AskedAt)
                return;
            _aloud = null;
            answer = a.Answer1;
            ms = a.Ms;
            if (!came)
            {
                _aloudLate++;
                Log.Detail("MIND aloud: " + a.Name + ": no answer in " + AloudWaitMs / 1000 + " s, kept in");
                return;
            }
            int idx = ThoughtVoice.Parse(answer, a.Options.Count), idx2 = ThoughtVoice.Parse(a.Answer2, a.Options.Count);
            string head = "MIND aloud: " + a.Name + " thinks \"" + a.Thought + "\" (" + a.Options.Count + " remarks offered, " + ms + " ms) -> ";
            Action<string> log = _aloudLogged++ < 40 ? (Action<string>)Log.Write : Log.Detail;
            if (idx == -2 || idx2 == -2)
            {
                _aloudKept++;
                log(head + "keeps it to themselves");
                return;
            }
            if (idx >= 0 && idx2 >= 0 && idx != idx2)
            {
                _aloudKept++;
                log(head + "unsure (" + a.Options[idx].Line + ", then " + a.Options[idx2].Line + "): kept in");
                return;
            }
            if (idx < 0)
            {
                _aloudUnread++;
                log(head + "\"" + (answer ?? "").Trim() + "\" names none of them: kept in");
                return;
            }
            int h = a.Handle;
            if (!ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h) || Speech.Busy(h) || _director.Involves(h))
            {
                log(head + "would have said " + a.Options[idx].Line + ", but they're busy now");
                return;
            }
            AloudOption o = a.Options[idx];
            bool played = o.Take > 0 ? Speech.PlayTake(h, o.Line, o.Take, 0) : Speech.Say(h, o.Line, 0);
            if (!played)
            {
                log(head + o.Line + ", but the game turned the voice down");
                return;
            }
            _aloudSaid++;
            _aloudLast = a.Name + ": " + (o.Words ?? o.Line);
            if (o.Words != null)
                SpeechCaption(Person.FirstOf(a.Name), o.Words, h, Math.Min(5000, 1500 + o.Words.Length * 55));
            log(head + "says " + o.Line + (o.Take > 0 ? " take " + o.Take : "") + (o.Words != null ? " (\"" + o.Words + "\")" : " (" + o.Text + ")"));
        }

        private string AloudStatus()
        {
            if (!_cfg.ThoughtsAloud)
                return "off ([Mind] ThoughtsAloud)";
            if (!(_cfg.MindLive && _cfg.Language))
                return "waiting on [Mind] Language and MindLive";
            return (_langReady ? "on" : "on, the server not ready") + ": " + _aloudAsked + " asked, " + _aloudSaid + " said aloud, " + _aloudKept + " kept in, " + _aloudLate + " late, " + _aloudUnread + " unreadable" +
                   (Speech.Book != null ? "; voices known " + Speech.Book.BankCount + ", words for " + Speech.Book.WordCount + " recordings" : "") + (_aloudLast != null ? "; last: " + _aloudLast : "");
        }
    }
}
