using System;
using System.Collections.Generic;
using System.Text;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
	internal static class Feed
	{
		private sealed class Line
		{
			public string Text;

			public int Ms;

			// The speaker's recorded voice, played as the subtitle appears. If it returns the words the
			// recording says, those are shown instead, and the add-on's own line goes to Thinking.
			public Func<string> OnShow;

			public string Speaker;

			public string Full;     // the whole line (it may be split over several subtitles)

			public object Group;    // the subtitles one line was split into

			public bool Spoken;     // said by someone (Say, SayFirst, Mutter), not a notice

			public int Ped;         // who says it, when known

			public bool ToYou;      // said to the player (the game may show its own subtitle for it)
		}

		// Nothing here is drawn on screen any more: the white subtitles were the game's objective line
		// (Screen.PrintSubtitle), and showed words the voice wasn't saying. A spoken line is its voice, at its turn in the
		// queue; a notice is a toast at the top left of the screen (Toaster), or the "Around town" box (Fallback).

		/// Where a notice goes when the game won't take top-left updates (WestworldScript: the "Around town" box).
		public static Action<string, string> Fallback;

		/// Where a line goes when the speaker's real recorded words replace it on screen (the ticker).
		public static Action<string, string> Thinking;

		private static object _dropGroup;

		private static readonly Queue<Line> _speech = new Queue<Line>();

		private static int _nextAt;

		private static int _lastShownAt = -100000;

		public static int SpeechMs = 5000;

		public static bool Busy
		{
			get
			{
				if (_speech.Count <= 0 && Game.GameTime - _lastShownAt >= 3500)
				{
					return Game.GameTime < _nextAt;
				}
				return true;
			}
		}

		public static bool Talking => _speech.Count > 0;

		public static void Clear()
		{
			_speech.Clear();
		}

		/// A test key's answer, at once: what's queued to be said is dropped, and the answer is a top-left update.
		public static void Interrupt(string text, int ms = 4000)
		{
			_speech.Clear();
			_dropGroup = null;
			Notice(null, text);
			_lastShownAt = Game.GameTime;
			_nextAt = Game.GameTime + ms;
		}

		public static void Say(string speaker, string text, Func<string> onShow = null, int ped = 0, bool toYou = false)
		{
			object group = new object();
			foreach (string item in Split(text, 150))
			{
				_speech.Enqueue(new Line
				{
					Text = speaker + ": \"" + item + "\"",
					Ms = Duration(item),
					OnShow = onShow,
					Speaker = speaker,
					Full = text,
					Group = group,
					Spoken = true,
					Ped = ped,
					ToYou = toYou
				});
				onShow = null;
			}
		}

		/// Like Say, but ahead of everything already queued (an outburst can't wait its turn).
		public static void SayFirst(string speaker, string text, Func<string> onShow = null, int ped = 0)
		{
			object group = new object();
			List<Line> items = new List<Line>();
			foreach (string item in Split(text, 150))
			{
				items.Add(new Line { Text = speaker + ": \"" + item + "\"", Ms = Duration(item), OnShow = onShow, Speaker = speaker, Full = text, Group = group, Spoken = true, Ped = ped });
				onShow = null;
			}
			Line[] rest = _speech.ToArray();
			_speech.Clear();
			foreach (Line l in items)
				_speech.Enqueue(l);
			foreach (Line l in rest)
				_speech.Enqueue(l);
		}

		/// A recording that has just started playing (a conversation in real voices). Nothing is shown for it; it holds
		/// the queue for about as long as it lasts, so no other line is voiced over it.
		public static void Overheard(string speaker, string words, string caption, int ped)
		{
			string text = !string.IsNullOrEmpty(words) ? words : caption;
			if (text == null || text.Length == 0)
				return;
			Line[] rest = _speech.ToArray();
			_speech.Clear();
			_speech.Enqueue(new Line { Text = speaker + ": " + text, Ms = Duration(text), Speaker = speaker, Spoken = true, Ped = ped });
			foreach (Line l in rest)
				_speech.Enqueue(l);
			_nextAt = 0;
		}

		/// onShow: a sound to play as it appears (a wordless one: what's muttered isn't for anyone to hear).
		public static void Mutter(string speaker, bool woman, string text, Func<string> onShow = null)
		{
			_speech.Enqueue(new Line
			{
				Text = speaker + ", under " + (woman ? "her" : "his") + " breath: \"" + text + "\"",
				Ms = Duration(text),
				OnShow = onShow,
				Spoken = true
			});
		}

		/// A notice (news, a hint, a test key's answer): a toast at the top left of the screen, or the "Around town" box
		/// when the game won't take toasts. Never white text.
		public static void Notice(string title, string text)
		{
			if (text == null || text.Length == 0)
				return;
			if (Toaster.Post(title, text))
				return;
			Fallback?.Invoke(title, text);
		}

		public static void Tick()
		{
			Toaster.Tick();
			int gameTime = Game.GameTime;
			if (gameTime >= _nextAt)
			{
				while (_speech.Count > 0 && _dropGroup != null && _speech.Peek().Group == _dropGroup)
				{
					_speech.Dequeue();   // the rest of a line already replaced by the speaker's real words
				}
				Line line = _speech.Count > 0 ? _speech.Dequeue() : null;
				if (line != null)
				{
					int ms = line.Ms;
					if (line.OnShow != null)
					{
						try
						{
							// the voice plays now; when its words are known the written line goes to the ticker as the
							// speaker's thought, and the rest of it (split over several turns) is dropped
							string words = line.OnShow();
							if (!string.IsNullOrEmpty(words))
							{
								ms = Duration(words);
								_dropGroup = line.Group;
								Thinking?.Invoke(line.Speaker, line.Full);
							}
						}
						catch (Exception ex)
						{
							Log.Write("Voice: " + ex.Message);
						}
					}
					_lastShownAt = gameTime;
					_nextAt = gameTime + ms;
				}
			}
		}

		private static int Duration(string text)
		{
			return Math.Max(SpeechMs - 1000, Math.Min(SpeechMs + 4000, 2500 + text.Length * 45));
		}

		private static IEnumerable<string> Split(string text, int max)
		{
			text = (text ?? string.Empty).Trim();
			while (text.Length > max)
			{
				int cut = -1;
				for (int num = Math.Min(max, text.Length - 1); num > max / 3; num--)
				{
					if ((text[num] == '.' || text[num] == '?' || text[num] == '!') && num + 1 < text.Length && text[num + 1] == ' ')
					{
						cut = num + 1;
						break;
					}
				}
				if (cut < 0)
				{
					cut = Math.Max(text.LastIndexOf(' ', max), max / 2);
				}
				yield return text.Substring(0, cut).Trim();
				text = text.Substring(cut).Trim();
			}
			if (text.Length > 0)
			{
				yield return text;
			}
		}

		public static string Clean(string s)
		{
			StringBuilder stringBuilder = new StringBuilder(s.Length);
			foreach (char c in s)
			{
				switch (c)
				{
				case '‘':
				case '’':
					stringBuilder.Append('\'');
					break;
				case '“':
				case '”':
					stringBuilder.Append('"');
					break;
				case '–':
				case '—':
					stringBuilder.Append('-');
					break;
				default:
					stringBuilder.Append(c);
					break;
				case '~':
					break;
				}
			}
			return stringBuilder.ToString();
		}
	}
}
