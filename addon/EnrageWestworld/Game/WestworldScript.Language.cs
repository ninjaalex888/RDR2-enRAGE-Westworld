// The language layer's game half (2026-10-29, after Lex approved the GPU setup; roadmap 15; the mind design's step 9;
// Core/Language.Client.cs holds the store, the queue and the JSON). With [Mind] Language on (and MindLive), the mind's
// own thoughts are worded by a local model: when a feeling is kept for someone near you, the request for them is queued
// at once (LanguageAskAhead), a worker thread asks Ollama on this machine (LanguageUrl, LanguageModel; nothing leaves
// the PC), and the answer goes through Language.Check; a line that passes is kept by its situation (the cache key), so
// when the thought comes (Engine.MindLive's Worded, through LanguageSwap) the model's line shows if one is kept, else the
// written line, which is asked for then. The worker touches no game native; it logs through Log (locked) and settles
// into the Core store (locked). The server is checked once on the first start (its model list) and every minute while
// it fails; five failures in a row back off a minute. F7 TEST language:; the Testing row "mind:ask" (People) asks for
// the nearest known person now whatever the setting, the answer to the log. Off, nothing here runs but the row.
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private Thread _langThread;
        private volatile bool _langStop;
        private volatile string _langServer = "not checked";   // "ready (model)", "not reachable: ...", "the model isn't pulled: ..."
        private volatile bool _langReady;
        private int _langNextStep, _langFailStreak, _langBackoffUntil, _langChecks, _langAsks;
        private const int LanguageTimeoutMs = 8000, LanguageBackoffMs = 60000;
        private int _langLoud, _langLoudShown;   // the first 25 answers and swaps a session to the main log (the prototype's look, 2026-10-30)

        /// Once a second from the tick: the engine's hand set, the worker started when the setting is on, the server checked
        /// again while it isn't answering.
        private void LanguageStep(int now)
        {
            if (now < _langNextStep)
                return;
            _langNextStep = now + 1000;
            try
            {
                if (_engine != null && _engine.LanguageSwap == null)
                    _engine.LanguageSwap = LanguageSwap;
                if (_cfg.Language && _langThread == null)
                    LanguageStart();
            }
            catch (Exception ex)
            {
                Log.Write("LANGUAGE step failed: " + ex.Message);
            }
        }

        private void LanguageStart()
        {
            if (_langThread != null)
                return;
            _langStop = false;
            _langThread = new Thread(LanguageWork) { IsBackground = true, Name = "RealNPCs language", Priority = ThreadPriority.BelowNormal };
            _langThread.Start();
            Log.Write("LANGUAGE worker started: " + _cfg.LanguageUrl + ", model " + _cfg.LanguageModel + (_cfg.Language ? "" : " ([Mind] Language is off: only the Testing row asks)"));
        }

        private void LanguageStop()
        {
            Thread t = _langThread;
            if (t == null)
                return;
            _langStop = true;
            try { t.Join(500); } catch { }
            _langThread = null;
        }

        /// The engine's hand (Engine.MindLive's Worded): the model's checked line for this person's situation now, if one
        /// is kept; else asks for it and gives null, so the written line shows now and the model's the next time.
        private string LanguageSwap(Person p, string source, string written)
        {
            if (p == null || p.Id == null || _engine == null || !_cfg.Language)
                return null;
            int handle;
            MindWho w = MindWhoWithHandle(p.Id, out handle);
            if (w == null)
                return null;
            string why;
            LanguageRequest r = MindRequestOf(w, handle, Game.GameTime, out why);
            if (r == null)
                return null;
            string key = Language.CacheKey(r);
            string kept = _engine.LanguageAnswers.Cached(key);
            if (kept != null && kept != written)
            {
                _engine.LanguageAnswers.Shown("model", kept);
                (_langLoudShown++ < 25 ? (Action<string>)Log.Write : Log.Detail)("LANGUAGE shown for " + w.Name + " (" + source + "): \"" + kept + "\" in place of \"" + written + "\"");
                return kept;
            }
            _engine.LanguageAnswers.Shown("written", written);
            LanguageAsk(w, r, written ?? r.AvoidLine, false);
            return null;
        }

        /// A feeling was just kept for someone near you: the request for them goes to the worker now, so the line is ready
        /// when the thought comes.
        private void LanguageAskAhead(MindWho w, string id, int now)
        {
            if (!_cfg.Language || !_cfg.MindLive || w == null || id == null || _engine == null)
                return;
            try
            {
                int handle;
                if (MindWhoWithHandle(id, out handle) == null)
                    return;
                string why;
                LanguageRequest r = MindRequestOf(w, handle, now, out why);
                if (r != null)
                    LanguageAsk(w, r, r.AvoidLine, false);
            }
            catch (Exception ex)
            {
                Log.Detail("LANGUAGE ask-ahead failed: " + ex.Message);
            }
        }

        private bool LanguageAsk(MindWho w, LanguageRequest r, string written, bool force)
        {
            string key = Language.CacheKey(r) + (force ? "|ask" + Environment.TickCount : "");
            var ask = new LanguageAsk { Key = key, Name = w.Name, PersonId = w.Id, Request = r, Written = written, AskedAtMs = Environment.TickCount };
            if (!_engine.LanguageAnswers.Ask(ask))
                return false;
            _langAsks++;
            if (_langThread == null)
                LanguageStart();
            Log.Detail("LANGUAGE asked for " + w.Name + " (key " + Language.CacheKey(r) + ")");
            return true;
        }

        private MindWho MindWhoWithHandle(string id, out int handle)
        {
            handle = 0;
            foreach (int h in _mindNear)
            {
                MindWho w;
                if (_mindWho.TryGetValue(h, out w) && w.Id == id && w.P != null && ENTITY.DOES_ENTITY_EXIST(h) && !ENTITY.IS_ENTITY_DEAD(h))
                {
                    handle = h;
                    return w;
                }
            }
            return null;
        }

        /// The Testing row: the nearest known person's request asked of the model now, whatever the setting; the answer to
        /// the log a moment later.
        private string LanguageAskFire(int now, out string said)
        {
            said = null;
            if (_cfg == null || !_cfg.MindShadow || _engine == null)
                return "[Mind] MindShadow is off";
            MindWho w = null;
            int handle = 0;
            foreach (int h in _mindNear)
            {
                MindWho k = ENTITY.DOES_ENTITY_EXIST(h) ? MindKnown(h, now) : null;
                if (k != null && k.Id != null && k.P != null)
                {
                    w = k;
                    handle = h;
                    break;
                }
            }
            if (w == null)
                return "nobody RealNPCs knows within " + (int)MindMetres + " m";
            string why;
            LanguageRequest r = MindRequestOf(w, handle, now, out why);
            if (r == null)
                return why;
            if (!LanguageAsk(w, r, r.AvoidLine, true))
                return "the queue is full (" + LanguageStore.MaxQueue + " waiting)";
            said = "asked the model for " + w.Name + "'s thought; the answer comes to the log (LANGUAGE) in a moment";
            return null;
        }

        // ---- the worker thread: no game natives here --------------------------------------------------------------

        private void LanguageWork()
        {
            LanguageCheckServer();
            while (!_langStop)
            {
                try
                {
                    int now = Environment.TickCount;
                    if (!_langReady && now - _langBackoffUntil >= 0)
                    {
                        LanguageCheckServer();
                        if (!_langReady)
                            _langBackoffUntil = now + LanguageBackoffMs;
                    }
                    LanguageAsk ask = _engine != null ? _engine.LanguageAnswers.Next() : null;
                    if (ask == null)
                    {
                        Thread.Sleep(60);
                        continue;
                    }
                    if (!_langReady)
                    {
                        _engine.LanguageAnswers.Settle(ask, null, 0);
                        continue;
                    }
                    var sw = Stopwatch.StartNew();
                    string json = LanguagePost(_cfg.LanguageUrl + "/api/generate", Language.RequestBody(_cfg.LanguageModel, ask.Request));
                    sw.Stop();
                    string answer = Language.ParseResponse(json);
                    LanguageAnswer a = _engine.LanguageAnswers.Settle(ask, answer, sw.ElapsedMilliseconds);
                    _langFailStreak = 0;
                    if (a != null)
                        (_langLoud++ < 25 ? (Action<string>)Log.Write : Log.Detail)("LANGUAGE answer for " + ask.Name + " in " + sw.ElapsedMilliseconds + " ms: \"" + (Language.Tidy(answer) ?? "(none)") + "\"" +
                                   (a.From == "written" ? " -> held (" + a.Why + "), the written line stands" : " -> kept"));
                }
                catch (Exception ex)
                {
                    _langFailStreak++;
                    if (_langFailStreak <= 3)
                        Log.Write("LANGUAGE the model didn't answer: " + ex.Message);
                    if (_langFailStreak >= 5)
                    {
                        _langReady = false;
                        _langServer = "not answering: " + ex.Message;
                        _langBackoffUntil = Environment.TickCount + LanguageBackoffMs;
                        _langFailStreak = 0;
                        Log.Write("LANGUAGE five failures in a row: the written lines for a minute, then the server is checked again");
                    }
                }
            }
        }

        /// GET the server's model list: ready when it answers and holds the model named in the settings.
        private void LanguageCheckServer()
        {
            _langChecks++;
            try
            {
                var req = (HttpWebRequest)WebRequest.Create(_cfg.LanguageUrl + "/api/tags");
                req.Method = "GET";
                req.Timeout = 3000;
                req.ReadWriteTimeout = 3000;
                using (var resp = (HttpWebResponse)req.GetResponse())
                using (var reader = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                {
                    string body = reader.ReadToEnd();
                    string model = _cfg.LanguageModel;
                    bool has = body.IndexOf("\"name\":\"" + model + "\"", StringComparison.Ordinal) >= 0 ||
                               (model.IndexOf(':') < 0 && body.IndexOf("\"name\":\"" + model + ":", StringComparison.Ordinal) >= 0);
                    if (has)
                    {
                        _langReady = true;
                        _langServer = "ready (" + model + ")";
                        Log.Write("LANGUAGE server ready at " + _cfg.LanguageUrl + " with " + model);
                    }
                    else
                    {
                        _langReady = false;
                        _langServer = "the model isn't pulled: " + model + " (ollama pull " + model + ")";
                        Log.Write("LANGUAGE server at " + _cfg.LanguageUrl + " doesn't hold " + model + ": run `ollama pull " + model + "`");
                    }
                }
            }
            catch (Exception ex)
            {
                _langReady = false;
                _langServer = "not reachable: " + ex.Message;
                if (_langChecks <= 2)
                    Log.Write("LANGUAGE server not reachable at " + _cfg.LanguageUrl + " (" + ex.Message + "); the written lines until it is");
            }
        }

        private static string LanguagePost(string url, string body)
        {
            var req = (HttpWebRequest)WebRequest.Create(url);
            req.Method = "POST";
            req.ContentType = "application/json";
            req.Timeout = LanguageTimeoutMs;
            req.ReadWriteTimeout = LanguageTimeoutMs;
            byte[] bytes = Encoding.UTF8.GetBytes(body);
            req.ContentLength = bytes.Length;
            using (Stream s = req.GetRequestStream())
                s.Write(bytes, 0, bytes.Length);
            using (var resp = (HttpWebResponse)req.GetResponse())
            using (var reader = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                return reader.ReadToEnd();
        }

        private string LanguageStatus()
        {
            if (_engine == null)
                return "no engine yet";
            string store = _engine.LanguageAnswers.Status();
            if (!_cfg.Language)
                return "off ([Mind] Language; the written lines): server " + _langServer + "; " + store;
            return "on (" + (_cfg.MindLive ? "with MindLive" : "but MindLive is off, so no mind thought to word") + "); server " + _langServer + " (" + _cfg.LanguageUrl + ", " + _cfg.LanguageModel + "), checked " + _langChecks +
                   (_langThread != null ? ", the worker running" : ", no worker") + "; " + store;
        }
    }
}
