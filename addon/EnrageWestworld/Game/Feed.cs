using System;
using System.Collections.Generic;
using System.Text;
using RDR2;
using RDR2.Native;
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

			public bool Spoken;     // said by someone (Say, SayFirst, Mutter), not a notice

			public int Ped;         // who says it, when known: no subtitle while they're out of sight

			public bool ToYou;      // said to the player: keeps its subtitle on "matched" even without known words

			public bool Real;       // a recording already playing, with its known words (Overheard)

			public bool Caption;    // a recording already playing, captioned in the add-on's own words (Overheard)
		}

		/// Overheard real-voice talk without known words gets a caption in the add-on's own words when this is on
		/// ([Output] OverheardTalk = captions); otherwise none.
		public static bool CaptionsOn;

		/// Which spoken lines get a white subtitle ([Output] Subtitles): "matched" only when the speaker's recorded
		/// words are known (voices\words.txt), "on" every line, "off" none. The voice plays either way.
		public static string SubtitleMode = "matched";

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

		/// A recording that has just started playing (a conversation in real voices): its words when the word list has
		/// them, else a caption. Shown at once, ahead of the queue, since the voice is already speaking.
		public static void Overheard(string speaker, string words, string caption, int ped)
		{
			bool real = !string.IsNullOrEmpty(words);
			string text = real ? words : caption;
			if (text == null || text.Length == 0)
				return;   // nothing to show for it
			Line[] rest = _speech.ToArray();
			_speech.Clear();
			_speech.Enqueue(new Line { Text = speaker + ": " + (real ? "\"" + text + "\"" : text), Ms = Duration(text), Speaker = speaker, Spoken = true, Ped = ped, Real = real, Caption = !real });
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
					bool real = false;   // the subtitle shows what the recording really says
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
								real = true;
								Thinking?.Invoke(line.Speaker, line.Full);
							}
						}
						catch (Exception ex)
						{
							Log.Write("Voice: " + ex.Message);
						}
					}
					if (ShowsSubtitle(line, real))
						Screen.PrintSubtitle(Clean(text));
					_lastShownAt = gameTime;
					_nextAt = gameTime + ms;
				}
			}
		}

		/// Whether a line gets its white subtitle: notices always; a spoken line by SubtitleMode, and only while the
		/// speaker (when known) is on screen and in plain view, so it's always clear who is talking.
		private static bool ShowsSubtitle(Line line, bool real)
		{
			if (!line.Spoken)
				return true;
			real = real || line.Real;
			if (SubtitleMode == "off" || (line.Caption && !CaptionsOn) || (SubtitleMode != "on" && !real && !line.ToYou && !line.Caption))
				return false;   // on "matched", what's said to you keeps its subtitle; only overheard talk needs its real words
			int ped = line.Ped;
			return ped == 0 || (ENTITY.DOES_ENTITY_EXIST(ped) && ENTITY.IS_ENTITY_ON_SCREEN(ped) &&
			                    ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(Game.Player.Character.Handle, ped, 17));
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
