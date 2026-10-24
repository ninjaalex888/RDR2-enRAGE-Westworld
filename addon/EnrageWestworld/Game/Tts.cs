// Reads lines aloud with a Windows speech voice (SAPI), on its own background thread so the game never
// waits for it. Late-bound COM: no extra assemblies. If SAPI is missing (Proton, stripped Windows) it logs
// once and goes quiet.
// Any installed SAPI voice can be used (VoiceMan / VoiceWoman in the ini match part of a voice's name; the
// log lists what's installed). With several voices of a kind, each speaker keeps one of them, and gets
// their own pitch and pace, so the same person always sounds the same.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Threading;

namespace EnrageWestworld.InGame
{
    internal static class Tts
    {
        private sealed class Item
        {
            public string Text;
            public bool Woman;
            public int Volume;
            public bool Uncanny;
            public string Speaker;
        }

        private sealed class VoiceInfo
        {
            public object Token;
            public string Name;
            public bool Woman;
        }

        private static readonly object Gate = new object();
        private static readonly Queue<Item> Queue = new Queue<Item>();
        private static readonly AutoResetEvent Signal = new AutoResetEvent(false);
        private static Thread _thread;
        private static volatile bool _stop;
        private static volatile bool _failed;
        private static volatile bool _speaking;

        public static bool Enabled = true;
        public static int Volume = 80;
        public static volatile string ManVoice = string.Empty;    // part of a voice's name, or empty for any man's voice
        public static volatile string WomanVoice = string.Empty;
        public static volatile bool Variety = true;               // each speaker keeps their own voice, pitch and pace

        public static bool Speaking { get { return _speaking || QueueCount > 0; } }

        private static int QueueCount
        {
            get
            {
                lock (Gate)
                    return Queue.Count;
            }
        }

        /// distanceFactor 0..1 scales the volume (far away = quieter). speaker: who is talking (keeps their voice).
        public static void Say(string text, bool woman, float distanceFactor = 1f, bool uncanny = false, string speaker = null)
        {
            if (!Enabled || _failed || string.IsNullOrEmpty(text) || Volume <= 0)
                return;
            int vol = (int)Math.Round(Volume * Math.Max(0.15f, Math.Min(1f, distanceFactor)));
            lock (Gate)
            {
                if (Queue.Count >= 3)
                    return;
                Queue.Enqueue(new Item { Text = text, Woman = woman, Volume = vol, Uncanny = uncanny, Speaker = speaker });
            }
            EnsureThread();
            Signal.Set();
        }

        public static void Stop()
        {
            _stop = true;
            lock (Gate)
                Queue.Clear();
            Signal.Set();
        }

        private static void EnsureThread()
        {
            if (_thread != null && _thread.IsAlive)
                return;
            _stop = false;
            _thread = new Thread(Run) { IsBackground = true, Name = "Westworld voice", Priority = ThreadPriority.BelowNormal };
            _thread.SetApartmentState(ApartmentState.STA);
            _thread.Start();
        }

        private static void Run()
        {
            try
            {
                Speak();
            }
            catch (Exception ex)
            {
                // never let anything escape this thread: an unhandled exception here would take the game down
                if (!(ex is ThreadAbortException))
                    Log.Write("Voice: stopped (" + ex.Message + ").");
            }
        }

        private static void Speak()
        {
            object voice;
            Type type;
            List<VoiceInfo> voices;
            object fallback;
            try
            {
                type = Type.GetTypeFromProgID("SAPI.SpVoice", false);
                if (type == null)
                    throw new InvalidOperationException("SAPI.SpVoice is not registered");
                voice = Activator.CreateInstance(type);
                fallback = Get(voice, "Voice");
                voices = AllVoices(voice);
                Log.Write("Voice: Windows speech ready. Installed voices: " +
                          (voices.Count == 0 ? "(none listed)" : string.Join(", ", voices.Select(v => v.Name + (v.Woman ? " (woman)" : " (man)")))) +
                          ". Pick one with VoiceMan / VoiceWoman in the ini.");
            }
            catch (Exception ex)
            {
                _failed = true;
                Log.Write("Voice: Windows speech isn't available (" + (ex.InnerException ?? ex).Message + ") - spoken lines stay as subtitles.");
                return;
            }
            while (!_stop)
            {
                Signal.WaitOne(2000);
                while (!_stop)
                {
                    Item item;
                    lock (Gate)
                    {
                        if (Queue.Count == 0)
                            break;
                        item = Queue.Dequeue();
                    }
                    try
                    {
                        _speaking = true;
                        int seed = Hash(item.Speaker ?? item.Text);
                        object v = Pick(voices, item.Woman, item.Speaker != null && Variety ? seed : 0) ?? fallback;
                        if (v != null)
                            SetRef(voice, "Voice", v);
                        Set(voice, "Volume", item.Volume);
                        int pitch = item.Uncanny ? -4 : 0;
                        int rate = item.Uncanny ? -2 : -1;
                        if (Variety && item.Speaker != null)
                        {
                            pitch += (seed >> 8) % 7 - 3;    // -3..+3
                            rate += (seed >> 4) % 3 - 1;     // a little slower or quicker
                        }
                        Set(voice, "Rate", Math.Max(-4, Math.Min(2, rate)));
                        string text = SecurityElement.Escape(item.Text) ?? string.Empty;
                        if (pitch != 0)
                            text = "<pitch middle=\"" + Math.Max(-8, Math.Min(8, pitch)) + "\">" + text + "</pitch>";
                        // 8 = the text is SAPI XML, 1 = don't block; then wait here in small steps so Stop() is heard quickly
                        type.InvokeMember("Speak", BindingFlags.InvokeMethod, null, voice, new object[] { text, 8 | 1 });
                        for (int waited = 0; waited < 30000 && !_stop; waited += 200)
                        {
                            object done = type.InvokeMember("WaitUntilDone", BindingFlags.InvokeMethod, null, voice, new object[] { 200 });
                            if (done is bool && (bool)done)
                                break;
                        }
                        if (_stop)
                            type.InvokeMember("Speak", BindingFlags.InvokeMethod, null, voice, new object[] { string.Empty, 2 });   // 2 = purge what's playing
                    }
                    catch (Exception ex)
                    {
                        Log.Write("Voice: couldn't speak (" + (ex.InnerException ?? ex).Message + ").");
                    }
                    finally
                    {
                        _speaking = false;
                    }
                }
            }
        }

        private static int Hash(string s)
        {
            unchecked
            {
                int h = 17;
                foreach (char c in s ?? string.Empty)
                    h = h * 31 + c;
                return h & 0x7fffffff;
            }
        }

        /// The voice for this line: the named one if the ini names one, else one of the right sex (the same one
        /// for the same speaker), else any.
        private static object Pick(List<VoiceInfo> voices, bool woman, int seed)
        {
            if (voices == null || voices.Count == 0)
                return null;
            string wanted = ((woman ? WomanVoice : ManVoice) ?? string.Empty).Trim();
            List<VoiceInfo> pool = wanted.Length > 0
                ? voices.Where(v => v.Name.IndexOf(wanted, StringComparison.OrdinalIgnoreCase) >= 0).ToList()
                : new List<VoiceInfo>();
            if (pool.Count == 0)
                pool = voices.Where(v => v.Woman == woman).ToList();
            if (pool.Count == 0)
                return null;
            return pool[seed % pool.Count].Token;
        }

        private static List<VoiceInfo> AllVoices(object voice)
        {
            List<VoiceInfo> list = new List<VoiceInfo>();
            try
            {
                object tokens = voice.GetType().InvokeMember("GetVoices", BindingFlags.InvokeMethod, null, voice, new object[] { string.Empty, string.Empty });
                int count = (int)tokens.GetType().InvokeMember("Count", BindingFlags.GetProperty, null, tokens, null);
                for (int i = 0; i < count; i++)
                {
                    try
                    {
                        object token = tokens.GetType().InvokeMember("Item", BindingFlags.InvokeMethod, null, tokens, new object[] { i });
                        string name = token.GetType().InvokeMember("GetDescription", BindingFlags.InvokeMethod, null, token, new object[] { 0 }) as string ?? "voice " + i;
                        string gender = token.GetType().InvokeMember("GetAttribute", BindingFlags.InvokeMethod, null, token, new object[] { "Gender" }) as string ?? string.Empty;
                        list.Add(new VoiceInfo { Token = token, Name = name, Woman = gender.Equals("Female", StringComparison.OrdinalIgnoreCase) });
                    }
                    catch
                    {
                    }
                }
            }
            catch
            {
            }
            return list;
        }

        private static object Get(object o, string name)
        {
            try
            {
                return o.GetType().InvokeMember(name, BindingFlags.GetProperty, null, o, null);
            }
            catch
            {
                return null;
            }
        }

        /// COM object-valued properties want "put by reference" (VBScript's Set); fall back to a plain put.
        private static void SetRef(object o, string name, object value)
        {
            try
            {
                o.GetType().InvokeMember(name, BindingFlags.PutRefDispProperty, null, o, new[] { value });
            }
            catch
            {
                Set(o, name, value);
            }
        }

        private static void Set(object o, string name, object value)
        {
            o.GetType().InvokeMember(name, BindingFlags.SetProperty, null, o, new[] { value });
        }
    }
}
