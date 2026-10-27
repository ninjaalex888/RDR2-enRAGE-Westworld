// Saving without a stutter (0.6.3; Lex's log, 2026-10-27: a 15-24 ms frame about once a minute "outside any feature",
// which was the 60 s save of the 1.6 MB arthur.json on the game thread). When: Core/Saving.cs (a moment nobody can see,
// else after a minute in play). How: the store is turned into JSON here, on the game thread, since that thread
// changes it every frame and it can't be read from another; the file is written and swapped in on a pool thread.
// The pool thread's rules: one write at a time (nothing new is turned into bytes while one is out); every synchronous
// save and every load waits for it (_saveLock), so a load never meets a half-swapped file, which Store.Load would take
// for a corrupt one and start fresh; a write still queued when a synchronous save comes is dropped (_saveGen), never
// written over the newer file; it touches only its bytes and the file, catches everything (an exception on a pool
// thread ends the game), and leaves its news in fields for the game thread to log (the log isn't written from two
// threads).
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using EnrageWestworld.Core;
using RDR2;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private readonly object _saveLock = new object();
        private MemoryStream _saveBuf;            // the save's bytes, reused; the pool thread reads them while _saveWriting
        private volatile bool _saveWriting;
        private int _saveGen;                     // under _saveLock
        private volatile string _saveError;       // set by the pool thread
        private volatile int _saveWriteUs = -1;   // the last background write's time, set by the pool thread
        private int _lastSaveAt, _playedSinceSave, _savesQuiet, _savesInPlay, _savesFailed, _saveKb;
        private int _clockLast, _clockStillAt;   // Step: the game clock last seen moving, and when (a still clock is a quiet moment)
        private bool _saveReport, _saveToldQuiet, _saveToldInPlay;
        private string _saveWhyNow = "", _saveWhy = "", _saveFile = "";
        private double _saveMs, _saveWarmMs = -1;
        private Action _aSave;

        /// From Step every tick: quiet says why nobody can see a slow frame now (paused, faded out, loading, the player
        /// down), null in play.
        private void SaveStep(string quiet)
        {
            if (_saveReport && !_saveWriting)
                SaveReport();
            if (_store == null || _player == null)
                return;
            if (quiet == null)
                _playedSinceSave++;
            int since = Environment.TickCount - _lastSaveAt;
            string why = Saving.WhyNow(quiet != null, since, _playedSinceSave, _saveWriting);
            if (why == null)
                return;
            _saveWhyNow = quiet ?? "in play, " + since / 1000 + " s without a quiet moment";
            Safe("save", _aSave ?? (_aSave = SaveInBackground));   // its own slot in the frame-time meter
        }

        private static string QuietWhy()
        {
            if (Game.IsLoading)
                return "loading";
            if (Game.IsPaused)
                return "paused";
            if (Game.IsScreenFadedOut)
                return "faded out";
            return "the player down";
        }

        private void SaveInBackground()
        {
            long t0 = Stopwatch.GetTimestamp();
            string path = StorePath;
            try
            {
                if (_saveBuf == null)
                    _saveBuf = new MemoryStream(1 << 20);
                _saveBuf.SetLength(0);
                _store.WriteTo(_saveBuf);
            }
            catch (Exception ex)
            {
                _savesFailed++;
                _lastSaveAt = Environment.TickCount;   // tried: not again every frame
                Log.Write("Couldn't save " + path + ": " + ex.Message);
                return;
            }
            _saveMs = (Stopwatch.GetTimestamp() - t0) * 1000.0 / Stopwatch.Frequency;
            _saveKb = (int)(_saveBuf.Length / 1024);
            _saveFile = Path.GetFileName(path);
            _saveWhy = _saveWhyNow;
            if (_saveWhy.StartsWith("in play"))
                _savesInPlay++;
            else
                _savesQuiet++;
            _lastSaveAt = Environment.TickCount;
            _playedSinceSave = 0;
            byte[] bytes = _saveBuf.GetBuffer();
            int count = (int)_saveBuf.Length;
            int gen;
            lock (_saveLock)
                gen = ++_saveGen;
            _saveWriteUs = -1;
            _saveWriting = true;
            _saveReport = true;
            try
            {
                ThreadPool.QueueUserWorkItem(_ => SaveWrite(path, bytes, count, gen));
            }
            catch
            {
                SaveWrite(path, bytes, count, gen);   // no pool thread to be had: written here, as before
            }
        }

        /// On a pool thread: the file only, never the store, the game or the log.
        private void SaveWrite(string path, byte[] bytes, int count, int gen)
        {
            try
            {
                lock (_saveLock)
                {
                    if (gen != _saveGen)
                        return;   // a synchronous save since (shutdown, a wipe, the character changing) wrote a newer one
                    long t0 = Stopwatch.GetTimestamp();
                    Store.WriteFile(path, bytes, count);
                    _saveWriteUs = (int)((Stopwatch.GetTimestamp() - t0) * 1000000 / Stopwatch.Frequency);
                }
            }
            catch (Exception ex)
            {
                _saveError = ex.Message;
            }
            finally
            {
                _saveWriting = false;
            }
        }

        /// Back on the game thread once a background write is done: the first of each kind in the log, the rest as detail.
        private void SaveReport()
        {
            _saveReport = false;
            string err = _saveError;
            if (err != null)
            {
                _saveError = null;
                _savesFailed++;
                Log.Write("Couldn't save " + _saveFile + ": " + err);
                return;
            }
            bool inPlay = _saveWhy.StartsWith("in play");
            string line = "SAVE written: " + _saveFile + ", " + _saveKb + " KB; " + _saveMs.ToString("0.0") + " ms on the game thread (" + _saveWhy + "), " +
                          (_saveWriteUs >= 0 ? (_saveWriteUs / 1000.0).ToString("0.0") + " ms writing it out in the background" : "not written: a newer save came first");
            if (inPlay ? _saveToldInPlay : _saveToldQuiet)
            {
                Log.Detail(line);
                return;
            }
            if (inPlay)
                _saveToldInPlay = true;
            else
                _saveToldQuiet = true;
            Log.Write(line + " (the first " + (inPlay ? "in play" : "while nobody could see") + " this session; the rest go to the detailed log)");
        }

        /// The synchronous save, for shutdown, a wipe, the character changing and too many errors: it waits for a
        /// background write still going, and drops one still queued (this one is newer).
        private void Save()
        {
            if (_store == null || _player == null)
                return;
            try
            {
                lock (_saveLock)
                {
                    _saveGen++;
                    _store.Save(StorePath);
                }
                _lastSaveAt = Environment.TickCount;
                _playedSinceSave = 0;
            }
            catch (Exception ex)
            {
                Log.Write("Couldn't save " + StorePath + ": " + ex.Message);
            }
        }

        /// Once a session, right after the load: the first time the store is turned into JSON costs about 50 ms (offline,
        /// 2026-10-27: .NET builds its writers then; 7 ms after), so it's paid here, inside the load's own long frame
        /// (150-400 ms, which the frame-time meter already lets settle), not in the first save.
        private void SaveWarm()
        {
            if (_saveWarmMs >= 0 || _store == null)
                return;
            long t0 = Stopwatch.GetTimestamp();
            try
            {
                _store.WriteTo(Stream.Null);
            }
            catch (Exception ex)
            {
                Log.Write("SAVE warm-up failed (the first save will be slower): " + ex.Message);
            }
            _saveWarmMs = (Stopwatch.GetTimestamp() - t0) * 1000.0 / Stopwatch.Frequency;
        }

        /// Before a load reads the file: a background write finishes first, and one still queued is dropped.
        private void SaveSettle()
        {
            lock (_saveLock)
                _saveGen++;
            _lastSaveAt = Environment.TickCount;
            _playedSinceSave = 0;
        }

        private string SaveStatus()
        {
            if (_store == null)
                return "nothing loaded yet";
            string last = _saveFile.Length == 0 ? "none since the load" :
                _saveWhy + ", " + _saveKb + " KB, " + _saveMs.ToString("0.0") + " ms on the game thread, " +
                (_saveWriting ? "still being written" : _saveWriteUs >= 0 ? (_saveWriteUs / 1000.0).ToString("0.0") + " ms writing it out in the background" : "not written");
            return (Environment.TickCount - _lastSaveAt) / 1000 + " s since the last save or load; the last background save: " + last + "; this session " + _savesQuiet +
                   " while nobody could see, " + _savesInPlay + " in play, " + _savesFailed + " failed; in play a save waits up to " + Saving.InPlayMs / 1000 +
                   " s for a quiet moment; warmed up at the load in " + _saveWarmMs.ToString("0.0") + " ms";
        }
    }
}
