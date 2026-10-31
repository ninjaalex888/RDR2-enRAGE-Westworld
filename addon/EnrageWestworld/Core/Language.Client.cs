// The language layer's game half, the part with no game in it (2026-10-29, after Lex approved the GPU setup; roadmap 15;
// the mind design's step 9). The mind decides (the feeling, whom, what shows); a local model only words it; the written
// line is always the fallback. What's here: the store of answers by cache key (the same person in the same situation
// asks once), the queue of requests waiting for the worker, the settling of an answer through Check and Resolve, the
// counts F7 shows, and the plain JSON the local server (Ollama's /api/generate) takes and gives, written and read by hand
// so nothing is referenced. Thread-safe: the worker thread settles, the game thread asks and reads, under one lock.
// Nothing here opens a connection: Game/WestworldScript.Language.cs does, and tells this what came back.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace EnrageWestworld.Core
{
    /// One request waiting for the worker.
    public sealed class LanguageAsk
    {
        public string Key, Name, PersonId;
        public LanguageRequest Request;
        public string Written;        // the written line the fallback would give (the model must not copy it)
        public long AskedAtMs;        // the clock when it was queued (the caller's, Environment.TickCount)
    }

    /// The answers kept, the asks waiting, and the counts: one of these on the Engine.
    public sealed class LanguageStore
    {
        public const int MaxAnswers = 2000, MaxQueue = 24;
        private readonly object _lock = new object();
        private readonly Dictionary<string, string> _answers = new Dictionary<string, string>(StringComparer.Ordinal);
        private readonly Dictionary<string, long> _asked = new Dictionary<string, long>(StringComparer.Ordinal);   // keys asked and not yet settled, with when
        private readonly Queue<LanguageAsk> _queue = new Queue<LanguageAsk>();
        public int Asked, Answered, Failed, Late, Held, ShownModel, ShownCache, ShownWritten, Dropped;
        // the model's picks (Language.Pick.cs): their own queue, served first, and their answers by key until taken
        public const int MostPicksWaiting = 6;
        private readonly Queue<LanguageAsk> _picks = new Queue<LanguageAsk>();
        private readonly Dictionary<string, KeyValuePair<string, long>> _picked = new Dictionary<string, KeyValuePair<string, long>>(StringComparer.Ordinal);
        public int PicksAsked, PicksAnswered, PicksFailed, PicksDropped;

        /// Queues a pick (the worker takes these before any thought). False when too many are waiting.
        public bool AskPick(LanguageAsk ask)
        {
            if (ask == null || ask.Key == null || ask.Request == null)
                return false;
            lock (_lock)
            {
                if (_picks.Count >= MostPicksWaiting)
                {
                    PicksDropped++;
                    return false;
                }
                _picks.Enqueue(ask);
                PicksAsked++;
                return true;
            }
        }

        /// The worker's answer to a pick (null: none came), kept until the game takes it.
        public void SettlePick(LanguageAsk ask, string answer, long elapsedMs)
        {
            if (ask == null)
                return;
            lock (_lock)
            {
                if (answer == null)
                    PicksFailed++;
                else
                    PicksAnswered++;
                if (_picked.Count >= 64)
                    _picked.Clear();   // answers nobody came for (their moment passed)
                _picked[ask.Key] = new KeyValuePair<string, long>(answer, elapsedMs);
            }
        }

        /// The answer to a pick, once it has come: true and the answer (maybe null) with how long it took; it's forgotten then.
        public bool TakePick(string key, out string answer, out long elapsedMs)
        {
            answer = null;
            elapsedMs = 0;
            lock (_lock)
            {
                KeyValuePair<string, long> a;
                if (key == null || !_picked.TryGetValue(key, out a))
                    return false;
                _picked.Remove(key);
                answer = a.Key;
                elapsedMs = a.Value;
                return true;
            }
        }
        public long AnswerMsTotal;
        public string LastWhy, LastShown, LastAnswer;
        public readonly Dictionary<string, int> HeldWhy = new Dictionary<string, int>(StringComparer.Ordinal);

        /// A checked answer for this key, if one is kept.
        public string Cached(string key)
        {
            if (key == null)
                return null;
            lock (_lock)
            {
                string a;
                return _answers.TryGetValue(key, out a) ? a : null;
            }
        }

        /// Queues an ask unless its key is already kept, already waiting, or the queue is full (then the newest is
        /// dropped: a thought the moment has passed for isn't worth the model's time). True if queued.
        public bool Ask(LanguageAsk ask)
        {
            if (ask == null || ask.Key == null || ask.Request == null)
                return false;
            lock (_lock)
            {
                if (_answers.ContainsKey(ask.Key) || _asked.ContainsKey(ask.Key))
                    return false;
                if (_queue.Count >= MaxQueue)
                {
                    Dropped++;
                    return false;
                }
                if (ask.Request.AvoidLine == null)
                    ask.Request.AvoidLine = ask.Written;   // the check's "copies the written line" needs it on the request
                _asked[ask.Key] = ask.AskedAtMs;
                _queue.Enqueue(ask);
                Asked++;
                return true;
            }
        }

        /// The next ask for the worker, or null.
        public LanguageAsk Next()
        {
            lock (_lock)
                return _picks.Count > 0 ? _picks.Dequeue() : _queue.Count > 0 ? _queue.Dequeue() : null;
        }

        public int Waiting
        {
            get { lock (_lock) return _queue.Count; }
        }

        /// What came back (or didn't: null) for an ask, in `elapsedMs`: through Check and Resolve; a line that passes is kept
        /// for its key whether or not it came in time (the key is the situation, and the next time the moment comes the
        /// line is ready). Returns the resolved answer, for the log.
        public LanguageAnswer Settle(LanguageAsk ask, string answer, long elapsedMs)
        {
            if (ask == null)
                return null;
            string clean = answer == null ? null : Language.Tidy(answer);
            LanguageAnswer a = Language.Resolve(ask.Request, clean, elapsedMs, ask.Written);
            lock (_lock)
            {
                _asked.Remove(ask.Key);
                if (answer == null)
                    Failed++;
                else
                {
                    Answered++;
                    AnswerMsTotal += Math.Max(0, elapsedMs);
                    LastAnswer = clean;
                }
                if (a.From == "written" && answer != null && a.Why != null && !a.Why.StartsWith("late", StringComparison.Ordinal))
                {
                    Held++;
                    string why = a.Why;
                    int paren = why.IndexOf(" (", StringComparison.Ordinal);
                    string kind = paren > 0 ? why.Substring(0, paren) : why;
                    int n;
                    HeldWhy.TryGetValue(kind, out n);
                    HeldWhy[kind] = n + 1;
                    LastWhy = why;
                }
                else if (a.From == "written" && a.Why != null && a.Why.StartsWith("late", StringComparison.Ordinal))
                {
                    Late++;
                    LastWhy = a.Why;
                }
                // kept when it passed the check, late or not
                if (clean != null && Language.Check(ask.Request, clean) == null)
                {
                    if (_answers.Count >= MaxAnswers)
                        _answers.Clear();
                    _answers[ask.Key] = clean;
                }
            }
            return a;
        }

        /// Counts what was shown.
        public void Shown(string from, string line)
        {
            lock (_lock)
            {
                if (from == "model") ShownModel++;
                else if (from == "cache") ShownCache++;
                else ShownWritten++;
                LastShown = line;
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                _answers.Clear();
                _asked.Clear();
                _queue.Clear();
            }
        }

        public string Status()
        {
            lock (_lock)
            {
                var b = new StringBuilder();
                b.Append("asked ").Append(Asked).Append(", answered ").Append(Answered);
                if (Answered > 0)
                    b.Append(" (").Append(AnswerMsTotal / Answered).Append(" ms each)");
                b.Append(", no answer ").Append(Failed).Append(", late ").Append(Late).Append(", held by the check ").Append(Held);
                if (HeldWhy.Count > 0)
                {
                    b.Append(" (");
                    bool first = true;
                    foreach (KeyValuePair<string, int> kv in HeldWhy)
                    {
                        if (!first) b.Append(", ");
                        first = false;
                        b.Append(kv.Key).Append(' ').Append(kv.Value);
                    }
                    b.Append(')');
                }
                b.Append("; kept ").Append(_answers.Count).Append(", waiting ").Append(_queue.Count).Append(", dropped ").Append(Dropped);
                b.Append("; shown from the model ").Append(ShownModel).Append(", from the cache ").Append(ShownCache).Append(", the written line ").Append(ShownWritten);
                if (LastShown != null)
                    b.Append("; last shown: \"").Append(LastShown).Append('"');
                if (LastWhy != null)
                    b.Append("; last held: ").Append(LastWhy);
                return b.ToString();
            }
        }
    }

    public static partial class Language
    {
        /// An answer as the model gives it, made fit for the check: outer quotation marks off, a trailing stop kept, one
        /// line of it (the first; a second paragraph is the model talking on), whitespace tidied.
        public static string Tidy(string answer)
        {
            if (answer == null)
                return null;
            string a = answer.Trim();
            int nl = a.IndexOfAny(new[] { '\n', '\r' });
            if (nl > 0)
                a = a.Substring(0, nl).Trim();
            while (a.Length >= 2 && (a[0] == '"' || a[0] == '“') && (a[a.Length - 1] == '"' || a[a.Length - 1] == '”'))
                a = a.Substring(1, a.Length - 2).Trim();
            if (a.StartsWith("Thought:", StringComparison.OrdinalIgnoreCase))
                a = a.Substring(8).Trim();
            return a.Replace("  ", " ");
        }

        /// The body for Ollama's /api/generate: the brief as the system prompt, the request as the prompt, no streaming,
        /// a short answer. Written by hand: the add-on references no JSON library.
        public static string RequestBody(string model, LanguageRequest r, double temperature = 0.9, int maxTokens = 40)
        {
            var b = new StringBuilder(1024);
            b.Append("{\"model\":").Append(JsonString(model)).Append(",\"system\":").Append(JsonString(Brief(r))).Append(",\"prompt\":").Append(JsonString(Prompt(r)));
            b.Append(",\"stream\":false,\"keep_alive\":\"30m\",\"options\":{\"num_ctx\":1024,\"temperature\":").Append(temperature.ToString("0.00", CultureInfo.InvariantCulture))
             .Append(",\"num_predict\":").Append(maxTokens).Append(",\"stop\":[\"\\n\\n\"]}}");
            return b.ToString();
        }

        public static string JsonString(string s)
        {
            var b = new StringBuilder((s ?? string.Empty).Length + 2);
            b.Append('"');
            foreach (char c in s ?? string.Empty)
            {
                switch (c)
                {
                    case '"': b.Append("\\\""); break;
                    case '\\': b.Append("\\\\"); break;
                    case '\n': b.Append("\\n"); break;
                    case '\r': b.Append("\\r"); break;
                    case '\t': b.Append("\\t"); break;
                    default:
                        if (c < 0x20)
                            b.Append("\\u").Append(((int)c).ToString("x4"));
                        else
                            b.Append(c);
                        break;
                }
            }
            b.Append('"');
            return b.ToString();
        }

        /// The "response" string out of the server's JSON (null when there is none); the escapes undone.
        public static string ParseResponse(string json)
        {
            return JsonField(json, "response");
        }

        /// A top-level string field of a flat JSON object, by name; null when missing or not a string.
        public static string JsonField(string json, string name)
        {
            if (json == null || name == null)
                return null;
            string tag = "\"" + name + "\":";
            int i = json.IndexOf(tag, StringComparison.Ordinal);
            if (i < 0)
                return null;
            i += tag.Length;
            while (i < json.Length && char.IsWhiteSpace(json[i]))
                i++;
            if (i >= json.Length || json[i] != '"')
                return null;
            i++;
            var b = new StringBuilder();
            while (i < json.Length)
            {
                char c = json[i++];
                if (c == '"')
                    return b.ToString();
                if (c != '\\')
                {
                    b.Append(c);
                    continue;
                }
                if (i >= json.Length)
                    break;
                char e = json[i++];
                switch (e)
                {
                    case 'n': b.Append('\n'); break;
                    case 'r': b.Append('\r'); break;
                    case 't': b.Append('\t'); break;
                    case 'b': b.Append('\b'); break;
                    case 'f': b.Append('\f'); break;
                    case 'u':
                        if (i + 4 <= json.Length)
                        {
                            int code;
                            if (int.TryParse(json.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code))
                                b.Append((char)code);
                            i += 4;
                        }
                        break;
                    default: b.Append(e); break;   // \" \\ \/
                }
            }
            return null;
        }
    }
}
