// enRAGE-Westworld.log next to the ini. Thread-safe (the voice thread can write too).
using System;
using System.IO;

namespace EnrageWestworld.InGame
{
    internal static class Log
    {
        private static readonly object Gate = new object();
        private static string _path;

        public static bool Debug;

        public static void Open(string path)
        {
            _path = path;
            try
            {
                if (File.Exists(path) && new FileInfo(path).Length > 2 * 1024 * 1024)
                    File.Delete(path);
                File.AppendAllText(path, "\r\n==== " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + " ====\r\n");
            }
            catch
            {
            }
        }

        public static void Write(string line)
        {
            if (_path == null)
                return;
            lock (Gate)
            {
                try
                {
                    File.AppendAllText(_path, DateTime.Now.ToString("HH:mm:ss") + " " + line + "\r\n");
                }
                catch
                {
                }
            }
        }

        public static void Detail(string line)
        {
            if (Debug)
                Write(line);
        }
    }
}
