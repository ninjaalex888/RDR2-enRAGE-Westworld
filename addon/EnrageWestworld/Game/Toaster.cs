using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
	internal static class Toaster
	{
		private sealed class Live
		{
			public IntPtr Duration;

			public IntPtr Data;

			public IntPtr[] Strings;

			public int At;
		}

		private const ulong PostSampleToast = 2803616216400160553uL;

		private const ulong VarString = 18055593637008912569uL;

		private static readonly Queue<KeyValuePair<string, string>> _queue = new Queue<KeyValuePair<string, string>>();

		private static readonly List<Live> _live = new List<Live>();

		private static int _nextAt;

		private static bool _broken;

		public static bool Post(string title, string text)
		{
			if (_broken)
			{
				return false;
			}
			if (_queue.Count >= 6)
			{
				_queue.Dequeue();
			}
			_queue.Enqueue(new KeyValuePair<string, string>(title ?? string.Empty, text ?? string.Empty));
			return true;
		}

		public static void Tick()
		{
			int gameTime = Game.GameTime;
			for (int num = _live.Count - 1; num >= 0; num--)
			{
				if (gameTime - _live[num].At > 300000 || gameTime < _live[num].At)
				{
					Free(_live[num]);
					_live.RemoveAt(num);
				}
			}
			if (_broken || _queue.Count == 0 || gameTime < _nextAt)
			{
				return;
			}
			KeyValuePair<string, string> keyValuePair = _queue.Dequeue();
			_nextAt = gameTime + 7500;   // the next toast waits for this one to go
			Live live = new Live
			{
				At = gameTime,
				Strings = new IntPtr[3]
			};
			try
			{
				live.Duration = Zeroed(56);
				live.Data = Zeroed(64);
				live.Strings[0] = Utf8("LITERAL_STRING");
				live.Strings[1] = Utf8(Feed.Clean(keyValuePair.Key));
				live.Strings[2] = Utf8(Feed.Clean(keyValuePair.Value));
				Marshal.WriteInt64(live.Duration, 0, 7000L);   // 7 s on screen (4 s was too quick to read)
				ulong val = Function.Call<ulong>(18055593637008912569uL, new InputArgument[3]
				{
					10,
					new InputArgument((ulong)live.Strings[0].ToInt64()),
					new InputArgument((ulong)live.Strings[1].ToInt64())
				});
				ulong val2 = Function.Call<ulong>(18055593637008912569uL, new InputArgument[3]
				{
					10,
					new InputArgument((ulong)live.Strings[0].ToInt64()),
					new InputArgument((ulong)live.Strings[2].ToInt64())
				});
				Marshal.WriteInt64(live.Data, 8, (long)val);
				Marshal.WriteInt64(live.Data, 16, (long)val2);
				Marshal.WriteInt64(live.Data, 32, MISC.GET_HASH_KEY("generic_textures"));
				Marshal.WriteInt64(live.Data, 40, MISC.GET_HASH_KEY("tick"));
				Marshal.WriteInt64(live.Data, 48, MISC.GET_HASH_KEY("COLOR_WHITE"));
				Function.Call(2803616216400160553uL, new InputArgument((ulong)live.Duration.ToInt64()), new InputArgument((ulong)live.Data.ToInt64()), 1, 1);
				_live.Add(live);
			}
			catch (Exception ex)
			{
				_broken = true;
				Free(live);
				Log.Write("Game feed toast failed, using the \"Around town\" box from now on: " + ex.Message);
				Feed.Notice(keyValuePair.Key, keyValuePair.Value);   // broken now, so this goes to Feed.Fallback
			}
		}

		private static IntPtr Zeroed(int bytes)
		{
			IntPtr intPtr = Marshal.AllocHGlobal(bytes);
			for (int i = 0; i < bytes; i += 8)
			{
				Marshal.WriteInt64(intPtr, i, 0L);
			}
			return intPtr;
		}

		private static IntPtr Utf8(string s)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(s);
			IntPtr intPtr = Marshal.AllocHGlobal(bytes.Length + 1);
			Marshal.Copy(bytes, 0, intPtr, bytes.Length);
			Marshal.WriteByte(intPtr, bytes.Length, 0);
			return intPtr;
		}

		private static void Free(Live l)
		{
			if (l.Duration != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(l.Duration);
			}
			if (l.Data != IntPtr.Zero)
			{
				Marshal.FreeHGlobal(l.Data);
			}
			if (l.Strings == null)
			{
				return;
			}
			IntPtr[] strings = l.Strings;
			foreach (IntPtr intPtr in strings)
			{
				if (intPtr != IntPtr.Zero)
				{
					Marshal.FreeHGlobal(intPtr);
				}
			}
		}
	}
}
