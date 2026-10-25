using System;
using System.Collections.Generic;
using System.Text;
using RDR2;
using RDR2.UI;

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
		}

		/// Where a line goes when the speaker's real recorded words replace it on screen (the ticker).
		public static Action<string, string> Thinking;

		private static object _dropGroup;

		private static readonly Queue<Line> _speech = new Queue<Line>();

		private static readonly Queue<Line> _news = new Queue<Line>();

		private static int _nextAt;

		private static int _lastShownAt = -100000;

		public static int SpeechMs = 5000;

		public static bool UseGameFeed;

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
			_news.Clear();
		}

		/// Shows a line right now, ahead of everything queued (for the test keys, so their answer isn't stuck behind other subtitles).
		public static void Interrupt(string text, int ms = 4000)
		{
			_speech.Clear();
			_news.Clear();
			_dropGroup = null;
			Screen.PrintSubtitle(Clean(text));
			_lastShownAt = Game.GameTime;
			_nextAt = Game.GameTime + ms;
		}

		public static void Say(string speaker, string text, Func<string> onShow = null)
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
					Group = group
				});
				onShow = null;
			}
		}

		/// Like Say, but ahead of everything already queued (an outburst can't wait its turn).
		public static void SayFirst(string speaker, string text, Func<string> onShow = null)
		{
			object group = new object();
			List<Line> items = new List<Line>();
			foreach (string item in Split(text, 150))
			{
				items.Add(new Line { Text = speaker + ": \"" + item + "\"", Ms = Duration(item), OnShow = onShow, Speaker = speaker, Full = text, Group = group });
				onShow = null;
			}
			Line[] rest = _speech.ToArray();
			_speech.Clear();
			foreach (Line l in items)
				_speech.Enqueue(l);
			foreach (Line l in rest)
				_speech.Enqueue(l);
		}

		/// onShow: a sound to play as it appears (a wordless one: what's muttered isn't for anyone to hear).
		public static void Mutter(string speaker, bool woman, string text, Func<string> onShow = null)
		{
			_speech.Enqueue(new Line
			{
				Text = speaker + ", under " + (woman ? "her" : "his") + " breath: \"" + text + "\"",
				Ms = Duration(text),
				OnShow = onShow
			});
		}

		public static void Notice(string title, string text)
		{
			if (UseGameFeed && Toaster.Post(title, text))
			{
				return;
			}
			foreach (string item in Split(string.IsNullOrEmpty(title) ? text : (title + ": " + text), 190))
			{
				_news.Enqueue(new Line
				{
					Text = item,
					Ms = Duration(item) + 1000
				});
			}
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
				Line line = ((_speech.Count > 0) ? _speech.Dequeue() : ((_news.Count > 0) ? _news.Dequeue() : null));
				if (line != null)
				{
					string text = line.Text;
					int ms = line.Ms;
					if (line.OnShow != null)
					{
						try
						{
							string words = line.OnShow();
							if (!string.IsNullOrEmpty(words))
							{
								text = line.Speaker + ": \"" + words + "\"";
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
					Screen.PrintSubtitle(Clean(text));
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
