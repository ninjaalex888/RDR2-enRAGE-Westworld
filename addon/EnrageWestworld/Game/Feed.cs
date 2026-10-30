using System;
using System.Collections.Generic;
using System.Text;
using EnrageWestworld.Core;
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

			public string Part;     // this turn's words, as written (a long line is split over several turns)

			public string Full;     // the whole line (it may be split over several subtitles)

			public object Group;    // the subtitles one line was split into

			public bool Spoken;     // said by someone (Say, SayFirst, Mutter), not a notice

			public int Ped;         // who says it, when known

			public bool ToYou;      // said to the player: the only lines that get a subtitle (SubtitleRule)

			public int Tries;       // times the game turned its voice down (a line said to you is tried again)
		}

		// A spoken line is its voice, at its turn in the queue. Only what's said to the player gets a subtitle, and only
		// while the voice is really playing (SubtitleRule). The game gives mods no way into its own dialogue subtitles,
		// so RealNPCs draws it the way the game does: in the same spot, in a dark box, in a serif face (DrawSubtitle;
		// Lex chose that over the white objective line). A notice is a toast at the top left of the screen (Toaster),
		// or the "Around town" box (Fallback).

		/// The face for the subtitles: the font test (Shift+F2) found $body the closest to the game's own (Lex, line 1).
		public static string SubtitleFace = "$body";

		// the box's top just under the game's own subtitle line, so the two never overlap (Lex, 2026-10-30, a screenshot: ours sat
		// on the game's), growing down to SubtitleLowest above the news bar; a longer one rises only as far as it must
		public const float SubtitleScale = 0.42f, SubtitleTop = 0.858f, SubtitleLowest = 0.946f, SubtitleWidth = 0.62f;

		private static string _subText;
		private static int _subFrom, _subUntil;
		private static bool _subOverheard;
		private static string _drawnNoted;   // the line showing was only overheard: a line said to the player may replace it

		/// [Output] Subtitles (on, unless "off").
		public static bool SubtitlesOn = true;

		/// [Output] Subtitles: game (ours only while the game's own Subtitles option is off, so one set shows at a time),
		/// ours (always), both, off (Lex, 2026-10-28: the game's white line for a recorded line can't be stopped).
		public static string SubtitleMode = "game";

		/// Whether RealNPCs' subtitle may show now, by the mode and the game's own option.
		public static bool SubtitlesAllowed()
		{
			if (!SubtitlesOn || SubtitleMode == "off")
				return false;
			if (SubtitleMode == "ours")
				return true;
			bool game = HUD.IS_SUBTITLE_PREFERENCE_SWITCHED_ON();
			return SubtitleMode == "both" ? game : !game;
		}

		/// For the F7 dump: how many subtitles have been shown, and the last.
		public static int SubtitlesShown;
		public static string LastSubtitle;

		private static object _voicedGroup;   // the line whose voice played: its later turns keep their subtitle

		/// Where a notice goes when the game won't take top-left updates (WestworldScript: the "Around town" box).
		public static Action<string, string> Fallback;

		/// Where a line goes when the speaker's real recorded words replace it on screen (the ticker).
		public static Action<string, string, int> Thinking;   // speaker, the written line, their ped (0 if unknown)

		private static object _dropGroup;

		private static readonly Queue<Line> _speech = new Queue<Line>();

		private static readonly Queue<Line> _first = new Queue<Line>();   // what the player asked for (a flirt): ahead of the rest

		private static Line _again;   // a line said to you whose voice the game turned down: tried again before anything else

		private const int VoiceTries = 4, VoiceRetryMs = 700;

		private static int _nextAt;

		private static int _lastShownAt = -100000;

		public static int SpeechMs = 5000;

		public static bool Busy
		{
			get
			{
				if (!Talking && Game.GameTime - _lastShownAt >= 3500)
				{
					return Game.GameTime < _nextAt;
				}
				return true;
			}
		}

		public static bool Talking => _speech.Count > 0 || _first.Count > 0 || _again != null;

		public static void Clear()
		{
			_speech.Clear();
			_first.Clear();
			_again = null;
		}

		/// A test key's answer, at once: what's queued to be said is dropped, and the answer is a top-left update.
		public static void Interrupt(string text, int ms = 4000)
		{
			_speech.Clear();
			_first.Clear();
			_again = null;
			_dropGroup = null;
			Notice(null, text);
			_lastShownAt = Game.GameTime;
			_nextAt = Game.GameTime + ms;
		}

		/// ahead: the player asked for it (a flirt), so it goes before everything already waiting, in the order given.
		public static void Say(string speaker, string text, Func<string> onShow = null, int ped = 0, bool toYou = false, bool ahead = false)
		{
			object group = new object();
			foreach (string item in Split(text, 150))
			{
				(ahead ? _first : _speech).Enqueue(new Line
				{
					Text = speaker + ": \"" + item + "\"",
					Part = item,
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
				items.Add(new Line { Text = speaker + ": \"" + item + "\"", Part = item, Ms = Duration(item), OnShow = onShow, Speaker = speaker, Full = text, Group = group, Spoken = true, Ped = ped });
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
				Line line = _again ?? Next();
				_again = null;
				if (line != null)
				{
					int ms = line.Ms;
					string words = null;
					bool played = false;
					if (line.OnShow != null)
					{
						try
						{
							// the voice plays now (Speech.LastPlayed says whether it really did); when its words are known
							// the written line goes to the ticker as the speaker's thought, and the rest of it is dropped
							Speech.LastPlayed = false;
							words = line.OnShow();
							played = Speech.LastPlayed;
							if (played)
								_voicedGroup = line.Group;
							else if (line.ToYou && ++line.Tries < VoiceTries)
							{
								// the game turned the voice down (the speaker busy a moment): try again shortly, before anything else
								_again = line;
								_nextAt = gameTime + VoiceRetryMs;
								return;
							}
							else if (line.ToYou)
								Log.Write("VOICE: " + line.Speaker + "'s line to you wasn't played (the game turned it down " + line.Tries + " times), so no subtitle");
							if (!string.IsNullOrEmpty(words))
							{
								ms = Duration(words);
								_dropGroup = line.Group;
								Thinking?.Invoke(line.Speaker, line.Full, line.Ped);
							}
						}
						catch (Exception ex)
						{
							Log.Write("Voice: " + ex.Message);
						}
					}
					string sub = line.ToYou ? SubtitleRule.Text(true, played, line.OnShow == null && line.Group != null && line.Group == _voicedGroup, SubtitlesAllowed(),
					                                            true, InView(line.Ped), line.Speaker, words, line.Part) : null;
					if (sub != null)
					{
						_subText = Clean(sub);
						_subOverheard = false;
						_subFrom = gameTime;
						_subUntil = gameTime + Math.Max(ms, Duration(sub));
						SubtitlesShown++;
						LastSubtitle = sub;
						Log.Write("SUBTITLE " + sub + " (" + (!string.IsNullOrEmpty(words) ? "the recorded words" : "the written line") + ")");
					}
					_lastShownAt = gameTime;
					_nextAt = gameTime + ms;
				}
			}
		}

		/// What someone says near the player that the game gives no subtitle of its own (a street scene, a hello, the doctor,
		/// the law chase): in the same box, the game's way, for ms (Lex, 2026-10-30: spoken lines as subtitles, never on the
		/// bar with the thoughts). Not while the subtitles are off, nor over a line said to the player still showing.
		public static bool Overheard(string speaker, string text, int ms)
		{
			if (!SubtitlesOn || SubtitleMode == "off" || string.IsNullOrEmpty(text))
				return false;
			int now = Game.GameTime;
			if (_subText != null && now < _subUntil && !_subOverheard)
				return false;
			string sub = (string.IsNullOrEmpty(speaker) ? "" : speaker + ": ") + "\"" + text.Trim() + "\"";
			_subText = Clean(sub);
			_subOverheard = true;
			_subFrom = now;
			_subUntil = now + ms;
			SubtitlesShown++;
			LastSubtitle = sub;
			return true;
		}

		/// Every frame: the subtitle showing now, the game's way: white serif text in a dark box, centred above the ticker,
		/// fading in and out. Nothing when none is showing.
		public static void DrawSubtitle(int now)
		{
			if (_subText == null || now >= _subUntil || now < _subFrom)
				return;
			int a = Math.Min(255, Math.Min((now - _subFrom) * 255 / 150, (_subUntil - now) * 255 / 250));
			if (a <= 0)
				return;
			float scale = SubtitleScale, lineH = Draw.LineHeight(scale);
			List<string> lines = Draw.Wrap(_subText, scale, SubtitleWidth);
			float w = 0f;
			foreach (string l in lines)
				w = Math.Max(w, Draw.Width(l, scale));
			float padX = 0.012f, padY = 0.006f, h = lines.Count * lineH, top = Math.Min(SubtitleTop, SubtitleLowest - h - padY * 2f);
			if (!ReferenceEquals(_drawnNoted, _subText))
			{
				_drawnNoted = _subText;   // once a subtitle: drawn, and where (2026-10-30: Lex couldn't see them)
				Log.Detail("SUBTITLE drawn: top " + top.ToString("0.000") + ", " + lines.Count + " line(s), " + _subText);
			}
			Draw.Rect(0.5f - w / 2f - padX, top, w + padX * 2f, h + padY * 2f, 0, 0, 0, a * 165 / 255);
			float y = top + padY;
			foreach (string l in lines)
			{
				Draw.Text(l, 0.5f - Draw.Width(l, scale) / 2f, y, scale, 255, 255, 255, a, SubtitleFace);   // each line centred
				y += lineH;
			}
		}

		/// The next line: what the player asked for first, then the rest; the rest of a line already replaced by the
		/// speaker's real words is skipped.
		private static Line Next()
		{
			while (true)
			{
				Queue<Line> q = _first.Count > 0 ? _first : _speech.Count > 0 ? _speech : null;
				if (q == null)
					return null;
				Line l = q.Dequeue();
				if (_dropGroup == null || l.Group != _dropGroup)
					return l;
			}
		}

		/// The speaker on screen with a clear line of sight to the player (or not known: then yes).
		private static bool InView(int ped)
		{
			return ped == 0 || (ENTITY.DOES_ENTITY_EXIST(ped) && ENTITY.IS_ENTITY_ON_SCREEN(ped) && ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(Game.Player.Character.Handle, ped, 17));
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
