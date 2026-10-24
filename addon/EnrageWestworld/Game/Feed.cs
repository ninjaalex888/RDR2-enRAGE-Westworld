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
		}

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

		public static void Say(string speaker, string text)
		{
			foreach (string item in Split(text, 150))
			{
				_speech.Enqueue(new Line
				{
					Text = speaker + ": \"" + item + "\"",
					Ms = Duration(item)
				});
			}
		}

		public static void Mutter(string speaker, bool woman, string text)
		{
			_speech.Enqueue(new Line
			{
				Text = speaker + ", under " + (woman ? "her" : "his") + " breath: \"" + text + "\"",
				Ms = Duration(text)
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
				Line line = ((_speech.Count > 0) ? _speech.Dequeue() : ((_news.Count > 0) ? _news.Dequeue() : null));
				if (line != null)
				{
					Screen.PrintSubtitle(Clean(line.Text));
					_lastShownAt = gameTime;
					_nextAt = gameTime + line.Ms;
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
