// Core compiled ahead of time (0.6.3): .NET compiles each method the first time it runs, and that first run is a hitch
// the first time each thing happens in a session. Offline (scratchpad savebench, Lex's save, 2026-10-27): the engine's
// daily work took 56 ms the first time and 0.6-2 ms after, the rumours 4.8 (1.4), the first crushes 4.3 (0.07), the
// deliveries 5.3 (0.12). Core never touches the game (CLAUDE.md), so its methods (1,586 offline) are compiled on a pool
// thread right after the load: about half a second of compiling, none of it on the game's frame; the game thread only
// waits if it calls a method in the very moment it is being compiled. Game/ is left alone: compiling a method can run
// its type's static setup on that thread, and some of Game's call natives, which must stay on the game thread.
using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using EnrageWestworld.Core;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private int _prejitStarted;
        private volatile string _prejitDone;   // set by the pool thread, logged by the game thread
        private string _prejitStatus = "not started";

        /// Once a session, right after the first load.
        private void PrejitCore()
        {
            if (Interlocked.Exchange(ref _prejitStarted, 1) != 0)
                return;
            _prejitStatus = "compiling";
            try
            {
                ThreadPool.QueueUserWorkItem(_ => PrejitRun());
            }
            catch (Exception ex)
            {
                _prejitStatus = "not started (" + ex.Message + ")";
            }
        }

        /// On a pool thread: compiles only, never runs a method; catches everything (an exception here ends the game).
        private void PrejitRun()
        {
            try
            {
                long t0 = Stopwatch.GetTimestamp();
                int n = 0, failed = 0;
                const BindingFlags all = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
                foreach (Type ty in typeof(Engine).Assembly.GetTypes().Where(x => x.Namespace == "EnrageWestworld.Core" && !x.ContainsGenericParameters))
                {
                    foreach (MethodInfo mi in ty.GetMethods(all))
                    {
                        if (mi.IsAbstract || mi.ContainsGenericParameters)
                            continue;
                        try
                        {
                            RuntimeHelpers.PrepareMethod(mi.MethodHandle);
                            n++;
                        }
                        catch
                        {
                            failed++;
                        }
                    }
                }
                _prejitDone = n + " methods of Core compiled ahead in " + ((Stopwatch.GetTimestamp() - t0) * 1000 / Stopwatch.Frequency) + " ms on a pool thread" +
                              (failed > 0 ? " (" + failed + " couldn't be)" : "");
            }
            catch (Exception ex)
            {
                _prejitDone = "compiling Core ahead stopped: " + ex.Message;
            }
        }

        /// From Step: logs the pool thread's result once it's in.
        private void PrejitReport()
        {
            string done = _prejitDone;
            if (done == null)
                return;
            _prejitDone = null;
            _prejitStatus = done;
            Log.Write("PREJIT: " + done);
        }
    }
}
