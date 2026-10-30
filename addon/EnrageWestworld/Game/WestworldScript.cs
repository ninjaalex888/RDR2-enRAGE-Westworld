using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
	public partial class WestworldScript : Script
	{
		private const uint EventPromptTriggered = 1387172233u;

		private const int PromptGreet = 3;

		private const int PromptInsult = 4;

		private const int PromptInsultAlt = 17;

		private const int GreetDelayMs = 3200;

		private readonly string _dir;

		private readonly string _version;

		private readonly Settings _cfg;

		private Bridge _bridge;

		private Engine _engine;

		private Store _store;

		private string _player;

		private bool _off;

		private bool _oldCopy;

		private int _oldNotes;

		private int _nextOldNote;

		private bool _testRequested;

		private bool _snapshotFailed;

		private readonly int _loadedAt;

		private int _nextFind;

		private int _nextObserve;

		private int _nextRemarks;

		private int _nextThought;

		private int _nextLife;

		private int _nextNotice;

		private int _nextLifeNotice;

		private int _nextTown;

		private int _errors;

		private int _errorWindow;

		private int _lastRemarkAt = -100000;

		private string _town;

		private readonly List<KeyValuePair<int, int>> _greets = new List<KeyValuePair<int, int>>();

		private readonly List<KeyValuePair<int, int>> _insults = new List<KeyValuePair<int, int>>();

		private readonly Dictionary<string, int> _remarked = new Dictionary<string, int>();

		private readonly Dictionary<string, int> _talked = new Dictionary<string, int>();

		private readonly List<Toast> _lifeQueue = new List<Toast>();

		private Dictionary<string, RecordRef> _recById = new Dictionary<string, RecordRef>();

		private KeySpec _testKey;

		private KeySpec _flirtKey;

		private bool _flirtRequested;

		private int _flirtSaidUntil;   // a flirt's two lines are being said: another press waits for them

		private readonly Random _rng = new Random();

		private static readonly Regex Childlike = new Regex("kid|child|teen|jack|(?<!cow)boy|(?<!show|saloon|cow|bar)girl");

		private int _nextAmbient;

		private int _lastCoupleAt = -1000000;

		private int _lastStrangersAt = -1000000;

		private bool _flirtHinted;

		private readonly List<KeyValuePair<object, RecordRef>> _pendingPlans = new List<KeyValuePair<object, RecordRef>>();

		private string StorePath => Path.Combine(_dir, _player + ".json");

		public WestworldScript()
		{
			_version = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
			string games = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Rockstar Games", "Red Dead Redemption 2");
			_dir = Path.Combine(games, "RealNPCs");
			string copied = CopyOldData(Path.Combine(games, "enRAGE-Westworld"), _dir);   // before anything creates the new folder
			try
			{
				Directory.CreateDirectory(Path.Combine(_dir, "diaries"));
			}
			catch
			{
			}
			Log.Open(Path.Combine(_dir, "RealNPCs.log"));
			if (copied != null)
			{
				Log.Write(copied);
			}
			string oldDll = Path.Combine(Path.GetDirectoryName(base.FileName) ?? string.Empty, "EnrageWestworld.dll");
			if (File.Exists(oldDll))
			{
				// the add-on before the rename: two copies would both direct the same people
				_off = true;
				_oldCopy = true;
				Log.Write("STOPPED: EnrageWestworld.dll (the old name of this add-on) is still in the scripts folder. Delete it; RealNPCs stays off until then.");
			}
			_cfg = EnrageWestworld.Core.Settings.Load(Path.Combine(_dir, "RealNPCs.ini"), Log.Write).Gated();   // the master switches applied; the ini keeps the player's own values
			Log.Debug = _cfg.DebugLog;
			Feed.SpeechMs = Math.Max(3, _cfg.SubtitleSeconds) * 1000;
			_testKey = KeySpec.Parse(_cfg.TestKey, Keys.F7);
			_flirtKey = KeySpec.Parse(_cfg.FlirtKey, Keys.None);
			Setup04();
			_loadedAt = Environment.TickCount;
			string[] obj2 = new string[9] { "RealNPCs ", _version, " loaded (", base.FileName, "). Test key: ", null, null, null, null };
			KeySpec testKey = _testKey;
			obj2[5] = testKey.ToString();
			obj2[6] = ", flirt key: ";
			testKey = _flirtKey;
			obj2[7] = testKey.ToString();
			obj2[8] = ".";
			Log.Write(string.Concat(obj2));
			base.Interval = 0;
			base.Tick += OnTick;
			base.KeyDown += OnKeyDown;
			base.KeyUp += OnKeyUp;
			base.Aborted += OnAborted;
		}

		private void OnTick(object sender, EventArgs e)
		{
			if (_off)
			{
				if (_oldCopy && _oldNotes < 4 && Game.GameTime >= _nextOldNote && Game.GameTime > 8000)
				{
					_oldNotes++;
					_nextOldNote = Game.GameTime + 15000;
					RDR2.UI.Screen.PrintSubtitle("RealNPCs: please delete EnrageWestworld.dll from the scripts folder. It is this add-on under its old name, and two copies can't run together. RealNPCs is off until then.");
				}
				return;
			}
			long stepStart = System.Diagnostics.Stopwatch.GetTimestamp();   // the frame-time meter's total (WestworldScript.Perf.cs)
			try
			{
				Step();
			}
			catch (Exception ex)
			{
				int tickCount = Environment.TickCount;
				if (tickCount - _errorWindow > 60000)
				{
					_errorWindow = tickCount;
					_errors = 0;
				}
				if (++_errors == 1)
				{
					Log.Write("ERROR " + ex);
				}
				if (_errors > 30)
				{
					_off = true;
					Log.Write("Too many errors in a minute - the add-on switched itself off until the game restarts. Details above.");
					try
					{
						SupperEmergencyLight();   // never left dark or without controls once nothing steps the dark any more
						TrystAbort();
						PanelTimeRestore("the add-on switched itself off");
					}
					catch
					{
					}
					try
					{
						Save();
						return;
					}
					catch
					{
						return;
					}
				}
			}
			finally
			{
				PerfFrame(System.Diagnostics.Stopwatch.GetTimestamp() - stepStart);
			}
		}

		private void Step()
		{
			PanelTimeStep();   // the world slowed while the settings panel is open, and back the moment it isn't (WestworldScript.PanelTime.cs)
			if (!InWorld())
			{
				TrystInTheDark();
				SupperInTheDark();   // the supper invitation's dark, likewise (WestworldScript.Supper.cs)
				if (_recordFates.Count > 0 && Game.IsLoading)
					RecordFatesLoading();   // a save may be loading: no pending record death is booked after it (WestworldScript.Directing.cs)
				SaveStep(QuietWhy());   // nobody can see a slow frame now: the time to save (WestworldScript.Saving.cs)
				_perfUnseen = true;
				return;
			}
			int gameTime = Game.GameTime;
			PrejitReport();
			int pd;   // the frame-time meter's slots for what runs here outside any Safe (WestworldScript.Perf.cs)
			long pt = StepBegin(out pd);
			Feed.Tick();
			Frame(gameTime);
			PadStep();   // a controller's buttons for our keys (WestworldScript.Pad.cs)
			SettingsStep(gameTime);
			StepEnd("frame and panel (step)", pt, pd);
			if (_engine == null)
			{
				if (gameTime >= _nextFind && Environment.TickCount - _loadedAt >= 8000)
				{
					_nextFind = gameTime + 3000;
					Start();
				}
				return;
			}
			if (_testRequested)
			{
				_testRequested = false;
				pt = StepBegin(out pd);
				Test();
				StepEnd("F7 dump (step)", pt, pd);
			}
			if (_flirtRequested)
			{
				_flirtRequested = false;
				if (_cfg.Romance)
				{
					pt = StepBegin(out pd);
					Flirt();
					StepEnd("flirt (step)", pt, pd);
				}
			}
			pt = StepBegin(out pd);
			ReaderInput();
			PollWorldEvents(gameTime);
			PollGreets(gameTime);
			Greets(gameTime);
			Insults(gameTime);
			StepEnd("greets and events (step)", pt, pd);
			if (gameTime >= _nextObserve)
			{
				_nextObserve = gameTime + 5000;
				pt = StepBegin(out pd);
				Observe();
				StepEnd("observe enRAGE's people (step)", pt, pd);
			}
			pt = StepBegin(out pd);
			ApplyEffects();
			WorldRequests();
			if (!Feed.Busy)
			{
				RevealPlans();
			}
			StepEnd("effects and plans (step)", pt, pd);
			pt = StepBegin(out pd);
			Tick04(gameTime);
			StepEnd("tick04 (step)", pt, pd);
			bool flag = PLAYER.IS_PLAYER_CONTROL_ON(Game.Player.Handle);
			if (_cfg.Remarks && flag && gameTime >= _nextRemarks)
			{
				_nextRemarks = gameTime + 1000;
				pt = StepBegin(out pd);
				Remarks(gameTime);
				StepEnd("remarks (step)", pt, pd);
			}
			if (flag && (_cfg.AmbientFlirting || _cfg.NpcRomance) && gameTime >= _nextAmbient)
			{
				_nextAmbient = gameTime + 2000;
				pt = StepBegin(out pd);
				Ambient(gameTime);
				StepEnd("ambient (step)", pt, pd);
			}
			if (_cfg.Thoughts && flag && gameTime >= _nextThought)
			{
				_nextThought = gameTime + (int)((double)(_cfg.ThoughtEverySeconds * 1000) * (0.75 + _rng.NextDouble() * 0.5));
				pt = StepBegin(out pd);
				// all about you: people in view first (over their heads), else anyone who knows you (the "Around town" box; Route)
				Toast toast = _engine.NextThought(Bridge.Now(), _inViewId ?? (_inViewId = id => NearHandleOf(id) != 0)) ?? _engine.NextThought(Bridge.Now());
				StepEnd("thoughts about you (step)", pt, pd);
				if (toast != null)
				{
					_engine.Out.Add(toast);
				}
			}
			if (_cfg.LifeUpdates && gameTime >= _nextLife)
			{
				_nextLife = gameTime + 10000;
				pt = StepBegin(out pd);
				Life();
				StepEnd("life (step)", pt, pd);
			}
			if (flag)
			{
				pt = StepBegin(out pd);
				Notices(gameTime);
				StepEnd("notices (step)", pt, pd);
			}
			// A game clock that has stood still half a second while we still tick counts as a quiet moment too, in case a
			// menu or photo mode keeps our ticks going (the pause menu stopped them altogether, 2026-10-27).
			if (gameTime != _clockLast)
			{
				_clockLast = gameTime;
				_clockStillAt = Environment.TickCount;
			}
			bool still = Environment.TickCount - _clockStillAt >= 500;
			if (still)
				_perfUnseen = true;
			SaveStep(still ? "the game clock stopped" : null);   // in play: only after a minute with no quiet moment (WestworldScript.Saving.cs)
		}

		private static bool InWorld()
		{
			if (Game.IsLoading || Game.IsPaused || Game.IsScreenFadedOut)
			{
				return false;
			}
			Ped character = Game.Player.Character;
			if (character != null && character.Exists())
			{
				return !character.IsDead;
			}
			return false;
		}

		private void Start()
		{
			if (_bridge == null)
			{
				_bridge = Bridge.Find();
			}
			if (_bridge == null)
			{
				// standing alone (roadmap 34, 2026-10-29): at once when enRAGE isn't installed (no NemesisMod.dll beside us), and
				// after the two minutes it was given when it is installed but never came up (a load failure of its own)
				bool installed = EnrageInstalled();
				if (installed && Environment.TickCount - _loadedAt <= 120000)
				{
					return;
				}
				_bridge = Bridge.Absent();
				Log.Write((installed ? "enRAGE is installed but isn't running (not among Script Hook .NET's scripts after two minutes)" : "enRAGE isn't installed (no NemesisMod.dll in the scripts folder)")
				          + ": RealNPCs stands alone. Its own people, ties, memories, street scenes, minds and letters run; nothing of enRAGE's (its nemeses and their"
				          + " records, its townsfolk's names, the feeling bars, the grudge meter, interrogation, revives, avengers, its newspaper items and its settings page) until it is.");
			}
			if (_bridge.Present && !_bridge.Usable)
			{
				_off = true;
				Feed.Notice("RealNPCs " + _version, "This enRAGE version isn't supported yet (see RealNPCs.log).");
				return;
			}
			List<RecordRef> list = _bridge.Present ? _bridge.Roster() : new List<RecordRef>();
			if (list != null && (list.Count != 0 || !_bridge.Present) && Bridge.Now() > 0)
			{
				LoadStore();
				Observe();
				int num = _store.People.Values.Count((PersonState s) => s.Talks > 0 || s.Heard.Count > 0);
				Log.Write("Ready: " + (_bridge.Present ? "enRAGE " + _bridge.Version + ", " + list.Count + " people" : "standing alone (enRAGE not " + (EnrageInstalled() ? "running" : "installed") + ")")
				          + ", playing as " + PlayerName() + ", " + _store.People.Count + " people in the add-on's memory.");
				try
				{
					Stalker.ProbeActs();
				}
				catch (Exception ex)
				{
					Log.Write("STALKER acts: couldn't look up the animation sets (" + ex.Message + "); each is checked when first used");
				}
				Log.Write("REGULARS: " + (_cfg.Regulars ? "kept: " + Regulars.Count(_store.Residents) : "off") + "; " + _store.Residents.Count + " residents saved (up to " + Store.MaxResidents + ")");
				Log.Write("CLOCK: year " + CLOCK.GET_CLOCK_YEAR() + ", day of the week " + CLOCK.GET_CLOCK_DAY_OF_WEEK() + " (thoughts take 0 as Sunday), " +
					CLOCK.GET_CLOCK_DAY_OF_MONTH() + "/" + (CLOCK.GET_CLOCK_MONTH() + 1) + ", " + CLOCK.GET_CLOCK_HOURS() + ":" + CLOCK.GET_CLOCK_MINUTES().ToString("00") + (IsJohn() ? ", playing John" : ""));
				Feed.Notice("RealNPCs " + _version, _bridge.Present ? "listening. " + list.Count + " people in the world" + ((num > 0) ? (", " + num + " who know you.") : ".")
					: "standing alone: enRAGE isn't " + (EnrageInstalled() ? "running" : "installed") + ", so its people and features are off. Everything of RealNPCs' own runs.");
			}
		}

		/// Whether enRAGE's DLL is in the scripts folder beside ours (installed, whether or not it came up).
		private bool EnrageInstalled()
		{
			try
			{
				string scripts = Path.GetDirectoryName(base.FileName);
				return scripts != null && File.Exists(Path.Combine(scripts, "NemesisMod.dll"));
			}
			catch
			{
				return false;
			}
		}

		private static bool IsJohn()
		{
			try
			{
				return Models.Name(Game.Player.Character) == "player_three";
			}
			catch
			{
				return false;
			}
		}

		private string PlayerName()
		{
			if (!IsJohn())
			{
				return "Arthur Morgan";
			}
			return "John Marston";
		}

		private void LoadStore()
		{
			bool flag = IsJohn();
			_player = (flag ? "john" : "arthur");
			SaveSettle();   // a save still being written in the background finishes first (WestworldScript.Saving.cs)
			_store = Store.Load(StorePath, Log.Write);
			_saveRefusedTold = false;   // a store that could not be read says so once again (WestworldScript.Saving.cs)
			_store.Player = _player;
			SaveWarm();     // the first save's one-off cost, paid inside the load's long frame
			if (_recordFates.Count > 0)
			{
				Log.Write("RECORD death not booked: " + _recordFates.Count + " still waiting when a save loaded, left to enRAGE");
				_recordFates.Clear();
			}
			PrejitCore();   // Core's first-run compiling, on a pool thread (WestworldScript.Prejit.cs)
			_perfFrames = 0;   // loading the save is a long frame by nature (400 ms at Ready, 2026-10-27): the meter settles again from here
			PassingAbort();
			TestCastClear("a save loaded");
			WanderLaterClear();
			SightsReset("a save loaded");
			VicesReset();
			BelowAbort("a save loaded");
			PatientAbort("a save loaded");
			GrudgesAbort();
			KinAbort();
			_fallWatch = null;
			try
			{
				// the save's size at load, and what grows in it (each capped)
				long bytes = File.Exists(StorePath) ? new FileInfo(StorePath).Length : 0;
				Log.Write("SAVE " + Path.GetFileName(StorePath) + ": " + (bytes / 1024) + " KB; " + _store.People.Count + " people, " + _store.Residents.Count + " residents, " + _store.Ties.Count + " ties, " +
				          _store.Deaths.Count + " deaths, " + _store.WorldLog.Count + " events, " + _store.People.Values.Sum(st => st.Mind.Count) + " memories");
			}
			catch (Exception ex)
			{
				Log.Write("SAVE size unknown: " + ex.Message);
			}
			_engine = new Engine(_store, PlayerRef.For(flag), _cfg, Environment.TickCount)
			{
				DateText = (long m) => GameDate.FromMinutes(m).ToString("MMMM d, yyyy", CultureInfo.InvariantCulture),
				Log = Log.Write,
				Detail = Log.Detail,
				John = flag,
				IntelFor = IntelFor,
				FolkDead = (string key) => _bridge != null && _bridge.FolkDead(key)
			};
		}

		/// 0.6 renamed the add-on (enRAGE Westworld became RealNPCs). Copies the old data folder (saves, diaries, voice files,
		/// settings) to the new one, once, when the new one doesn't exist yet. The old folder is left as it was.
		private static string CopyOldData(string from, string to)
		{
			try
			{
				if (Directory.Exists(to) || !Directory.Exists(from))
				{
					return null;
				}
				int n = 0;
				foreach (string file in Directory.GetFiles(from, "*", SearchOption.AllDirectories))
				{
					string rel = file.Substring(from.Length).TrimStart('\\', '/');
					if (rel.EndsWith(".log", StringComparison.OrdinalIgnoreCase))
					{
						continue;
					}
					if (rel.Equals("enRAGE-Westworld.ini", StringComparison.OrdinalIgnoreCase))
					{
						rel = "RealNPCs.ini";
					}
					string dest = Path.Combine(to, rel);
					Directory.CreateDirectory(Path.GetDirectoryName(dest));
					File.Copy(file, dest, overwrite: false);
					n++;
				}
				return "Renamed to RealNPCs: copied " + n + " files (saves, diaries, settings) from the old enRAGE-Westworld folder, which is left as it was.";
			}
			catch (Exception ex)
			{
				return "Renamed to RealNPCs, but the old enRAGE-Westworld folder couldn't be copied: " + ex.Message;
			}
		}

		private void OnAborted(object sender, EventArgs e)
		{
			Shutdown04();
			Save();
			Log.Write("Stopped.");
		}

		private void Observe()
		{
			if ((IsJohn() ? "john" : "arthur") != _player)
			{
				Save();
				LoadStore();
				Log.Write("Player character changed: now " + PlayerName() + ".");
			}
			int od;   // its parts on the frame-time meter (the whole took 15.7 ms every 5 s, 2026-10-27)
			long ot = StepBegin(out od);
			List<RecordRef> list = _bridge.Present ? _bridge.Roster() : new List<RecordRef>();
			StepEnd("observe: enRAGE's roster (step)", ot, od);
			long num = Bridge.Now();
			if (list == null || num <= 0)
			{
				return;
			}
			Voice.NowYear = GameDate.FromMinutes(num).Year;
			Dictionary<string, RecordRef> dictionary = new Dictionary<string, RecordRef>();
			foreach (RecordRef item in list)
			{
				if (item != null && item.Id != null && !dictionary.ContainsKey(item.Id))
				{
					dictionary[item.Id] = item;
				}
			}
			_recById = dictionary;
			List<Person> list2 = new List<Person>(list.Count);
			ot = StepBegin(out od);
			foreach (RecordRef value in dictionary.Values)
			{
				try
				{
					list2.Add(Snapshot.Of(value, _bridge, dictionary, num));
				}
				catch (Exception ex)
				{
					if (!_snapshotFailed)
					{
						Log.Write("Couldn't read " + value.Name + " (" + ex.GetType().Name + ": " + ex.Message + ") - is this a newer enRAGE? Further failures are not logged.");
					}
					_snapshotFailed = true;
				}
			}
			StepEnd("observe: a snapshot of each (step)", ot, od);
			Vector3 position = Game.Player.Character.Position;
			_engine.PlayerX = position.X;
			_engine.PlayerY = position.Y;
			_engine.PlayerZ = position.Z;
			ot = StepBegin(out od);
			_engine.Observe(_bridge.Present ? list2 : null, num);   // null: no roster known (standing alone), its work waits
			StepEnd("observe: the engine (step)", ot, od);
			ot = StepBegin(out od);
			WriteDiaries();
			StepEnd("observe: diaries (step)", ot, od);
		}

		private Person PersonOf(RecordRef r)
		{
			return Snapshot.Of(r, _bridge, _recById, Bridge.Now());
		}

		// when the player last greeted each ped (game time), for flirting after a greeting
		private readonly Dictionary<int, int> _greetedAt = new Dictionary<int, int>();
		private const int GreetCountsMs = 180000;

		/// 1 if he greeted them in the last three minutes, else -1 (he flirted cold).
		private int GreetedFirst(int ped)
		{
			int at;
			return _greetedAt.TryGetValue(ped, out at) && Game.GameTime - at <= GreetCountsMs ? 1 : -1;
		}

		private unsafe void PollGreets(int now)
		{
			if (!_cfg.Conversations && !_cfg.Romance && !_cfg.SceneReactions)
			{
				return;
			}
			int num = SCRIPTS.GET_NUMBER_OF_EVENTS(0);
			if (num <= 0)
			{
				return;
			}
			ulong* ptr = stackalloc ulong[10];
			for (int i = 0; i < num; i++)
			{
				if (SCRIPTS.GET_EVENT_AT_INDEX(0, i) != 1387172233)
				{
					continue;
				}
				for (int j = 0; j < 10; j++)
				{
					ptr[j] = 0uL;
				}
				if (SCRIPTS.GET_EVENT_DATA(0, i, ptr, 10))
				{
					int num2 = (int)(*ptr);
					int target = (int)ptr[2];
					int wait = _director.Involves(target) ? 900 : 3200;   // stepping into a scene gets an answer quickly
					if (num2 == 3 && target != 0)
					{
						_greetedAt[target] = now;   // greeted before a flirt counts for it (Interest.GreetTilt, Trysts.NightChance)
						if (_greetedAt.Count > 60)
							_greetedAt.Remove(_greetedAt.OrderBy(kv => kv.Value).First().Key);
					}
					if (num2 == 3 && target != 0 && !_greets.Any((KeyValuePair<int, int> g) => g.Key == target))
					{
						_greets.Add(new KeyValuePair<int, int>(target, now + wait));
					}
					else if ((num2 == 4 || num2 == 17) && target != 0 && !_insults.Any((KeyValuePair<int, int> g) => g.Key == target))
					{
						_insults.Add(new KeyValuePair<int, int>(target, now + wait));
					}
				}
			}
		}

		private void Greets(int now)
		{
			for (int num = _greets.Count - 1; num >= 0; num--)
			{
				if (now >= _greets[num].Value)
				{
					int key = _greets[num].Key;
					_greets.RemoveAt(num);
					if (ENTITY.DOES_ENTITY_EXIST(key) && !ENTITY.IS_ENTITY_DEAD(key) && SceneIntrusion(key, friendly: true))
					{
						continue;
					}
					if (ENTITY.DOES_ENTITY_EXIST(key) && !ENTITY.IS_ENTITY_DEAD(key) && !PED.IS_PED_IN_COMBAT(key, 0) && !(new Ped(key).Position.DistanceTo(Game.Player.Character.Position) > 15f) && !Feed.Talking)
					{
						if (GreetedSpecial(key))
						{
							continue;
						}
						RecordRef nemesisRecord = _bridge.RecordAt(key);
						if (_cfg.Conversations)
						{
							if (nemesisRecord != null && nemesisRecord.IsAlive)
							{
								TalkTo(nemesisRecord, key);
							}
							else
							{
								FolkRef acquaintance = _bridge.AcquaintanceAt(key);
								if (acquaintance != null)
								{
									TalkTo(acquaintance, key);
								}
							}
						}
					}
				}
			}
		}

		private bool TalkedRecently(string id)
		{
			int gameTime = Game.GameTime;
			if (_talked.TryGetValue(id, out var value) && gameTime - value < 20000)
			{
				return true;
			}
			_talked[id] = gameTime;
			return false;
		}

		private void TalkTo(RecordRef rec, int ped)
		{
			if (TalkedRecently(rec.Id))
			{
				return;
			}
			Person person = PersonOf(rec);
			TalkContext ctx = new TalkContext
			{
				Seed = Environment.TickCount.ToString(),
				Town = _town,
				Gossip = GossipFor(person)
			};
			List<string> lines = _engine.Greet(person, Bridge.Now(), ctx);
			Speak(person.Name, lines, ped, person.Hate, person.Fear, person.Friendly);
			if (_cfg.RevealToEnrage)
			{
				int num = Snapshot.MarkKnown(rec, person);
				if (num > 0)
				{
					_bridge.RosterChanged();   // a direct write: enRAGE saves it now (CLAUDE.md, "Writing to enRAGE")
					Log.Write("Marked " + num + " of " + person.Name + "'s past as known in enRAGE.");
				}
			}
			_remarked[person.Id] = Game.GameTime;
		}

		private void TalkTo(FolkRef a, int ped)
		{
			Folk folk = Snapshot.Of(a);
			if (!TalkedRecently(folk.Key))
			{
				PersonState st = _store.Get(new Person
				{
					Id = folk.Key,
					Name = folk.Name,
					Woman = folk.Woman
				});
				List<string> list = Townsfolk.Lines(folk, st, _engine.Player, Environment.TickCount.ToString(), _cfg.Romance);
				if (list.Count != 0)
				{
					Log.Write("TALK " + folk.Name + " (townsfolk): " + string.Join(" | ", list));
					Speak(folk.Name, list, ped, folk.Grudge, 0, false);
					_remarked[folk.Key] = Game.GameTime;
				}
			}
		}

		/// A conversation, line by line. Up to three lines get a recorded voice that fits their tone, the
		/// player's own lines in his voice. Not the opening greeting: the game has already answered that one.
		private void Speak(string speaker, List<string> lines, int ped = 0, int hate = 0, int fear = 0, bool friendly = false)
		{
			string firstName = _engine.Player.FirstName;
			int me = Game.Player.Character.Handle;
			int voiced = 0;
			for (int i = 0; i < lines.Count; i++)
			{
				string line = lines[i];
				bool you = line.StartsWith("YOU:");
				string text = you ? line.Substring(4).Trim().Trim('"') : line;
				Func<string> voice = null;
				if (ped != 0 && voiced < 3)
				{
					string tag = you ? Tone.Of(text) : Tone.Of(text, hate, fear, friendly);
					if (i == 0 && !you && tag == "greet")
					{
						tag = null;
					}
					voice = you ? Speech.When(me, tag, ped) : Speech.When(ped, tag, me);
					if (voice != null)
					{
						voiced++;
					}
				}
				Feed.Say(you ? firstName : speaker, text, voice, toYou: !you);   // Arthur's own lines get no subtitle
			}
		}

		private List<Gossip> GossipFor(Person p)
		{
			List<Gossip> list = new List<Gossip>();
			long num = Bridge.Now();
			List<LifeEvent> list2 = _bridge.RecentEvents(num, 14400, 0);
			if (list2 == null)
			{
				return list;
			}
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			if (p.KinId != null && _recById.TryGetValue(p.KinId, out var value))
			{
				dictionary[p.KinId] = "my " + RelationOf(p.KinRelation, value) + " " + Person.FirstOf(value.Name);
			}
			foreach (RecordRef value4 in _recById.Values)
			{
				if (value4.KinId == p.Id && !dictionary.ContainsKey(value4.Id))
				{
					dictionary[value4.Id] = "my " + (value4.KinRelation ?? "kin") + " " + Person.FirstOf(value4.Name);
				}
			}
			foreach (string friendId in p.FriendIds)
			{
				if (_recById.TryGetValue(friendId, out var value2) && !dictionary.ContainsKey(friendId))
				{
					dictionary[friendId] = "my friend " + Person.FirstOf(value2.Name);
				}
			}
			int num2 = list2.Count - 1;
			while (num2 >= 0 && list.Count < 3)
			{
				LifeEvent worldEvent = list2[num2];
				if (worldEvent != null && worldEvent.Title != null && worldEvent.People != null)
				{
					foreach (LifePart person in worldEvent.People)
					{
						if (person != null && person.Id != null && !(person.Id == p.Id) && dictionary.TryGetValue(person.Id, out var value3))
						{
							string text = Voice.Gossip(worldEvent.Title, person.Name, value3, p.Id + worldEvent.Id);
							if (text != null)
							{
								list.Add(new Gossip
								{
									Key = worldEvent.Id,
									Line = text
								});
							}
							break;
						}
					}
				}
				num2--;
			}
			return list;
		}

		private static string RelationOf(string pIsToKin, RecordRef kin)
		{
			switch (pIsToKin)
			{
			case "father":
				if (!Models.Female(kin.ModelHash))
				{
					return "son";
				}
				return "daughter";
			case "son":
				return "father";
			case "uncle":
				return "nephew";
			case "nephew":
				return "uncle";
			case null:
				return "kin";
			default:
				return pIsToKin;
			}
		}

		private void Remarks(int now)
		{
			Ped character = Game.Player.Character;
			Vector3 position = character.Position;
			if (now >= _nextTown)
			{
				_nextTown = now + 5000;
				_town = _bridge.TownAt(position);
				_engine.CurrentTown = _town;
			}
			long now2 = Bridge.Now();
			bool flag = !Feed.Busy && now - _lastRemarkAt >= _cfg.RemarkGapSeconds * 1000;
			float num = Math.Max(_cfg.RemarkDistance, 3f);
			Ped[] allPeds = World.GetAllPeds();
			foreach (Ped ped in allPeds)
			{
				int handle = ped.Handle;
				if (handle == character.Handle)
				{
					continue;
				}
				float num2 = ped.Position.DistanceTo(position);
				if (num2 > 18f || ENTITY.IS_ENTITY_DEAD(handle) || PED.IS_PED_IN_COMBAT(handle, 0))
				{
					continue;
				}
				RecordRef nemesisRecord = _bridge.RecordAt(handle);
				if (nemesisRecord != null)
				{
					if (!nemesisRecord.IsAlive)
					{
						continue;
					}
					Person person = PersonOf(nemesisRecord);
					_engine.Sighted(person, now2, _town);
					if (flag && !(num2 > num) && !Recently(person.Id, now) && !PED.IS_PED_FLEEING(handle) && ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(handle, character.Handle, 17))
					{
						Remark remark = _engine.RemarkFor(person, now2);
						if (remark != null)
						{
							Show(person.Name, person.Woman, remark, Speech.For(handle, remark.Text, character.Handle, person.Hate, person.Fear, person.Friendly, remark.Uncanny ? "uneasy" : null), handle);
							_remarked[person.Id] = now;
							_lastRemarkAt = now;
							flag = false;
						}
					}
				}
				else
				{
					if (!flag || num2 > num)
					{
						continue;
					}
					FolkRef acquaintance = _bridge.AcquaintanceAt(handle);
					if (acquaintance == null)
					{
						continue;
					}
					Folk folk = Snapshot.Of(acquaintance);
					if (!Recently(folk.Key, now) && ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(handle, character.Handle, 17))
					{
						PersonState personState = _store.Get(new Person
						{
							Id = folk.Key,
							Name = folk.Name,
							Woman = folk.Woman
						});
						Remark remark2 = Townsfolk.RemarkFor(folk, personState, folk.Key + personState.Told.Count, _cfg.Romance, _engine.Player);
						if (remark2 != null)
						{
							Show(folk.Name, folk.Woman, remark2, Speech.For(handle, remark2.Text, character.Handle, folk.Grudge, 0, false, remark2.Uncanny ? "uneasy" : null));
							_remarked[folk.Key] = now;
							_lastRemarkAt = now;
							flag = false;
						}
					}
				}
			}
		}

		private bool Recently(string id, int now)
		{
			if (_remarked.TryGetValue(id, out var value))
			{
				return now - value < _cfg.RemarkSamePersonSeconds * 1000;
			}
			return false;
		}

		/// A passing remark. Muttered ones get a wordless sound at most; the rest get the speaker's recorded voice when one
		/// fits (uncanny ones an unsettled one).
		private static void Show(string name, bool woman, Remark r, Func<string> voice = null, int ped = 0)
		{
			Log.Write("REMARK " + name + (r.Muttered ? " (muttered)" : "") + ": " + r.Text);
			if (r.Muttered)
			{
				Feed.Mutter(name, woman, r.Text, Speech.When(ped, "hum"));
			}
			else
			{
				Feed.Say(name, r.Text, voice, toYou: true);
			}
		}

		private void Life()
		{
			List<LifeEvent> list = _bridge.RecentEvents(Bridge.Now(), 2880, 40);
			if (list == null)
			{
				return;
			}
			for (int i = 0; i < list.Count; i++)
			{
				LifeEvent worldEvent = list[i];
				if (worldEvent != null && worldEvent.Id != null && !worldEvent.Id.StartsWith("ww_") && !worldEvent.IsPast)
				{
					Toast toast = _engine.LifeUpdate(worldEvent);
					if (toast != null)
					{
						_lifeQueue.Add(toast);
					}
				}
			}
		}

		private void Notices(int now)
		{
			if (Feed.Busy || now < _nextNotice)
			{
				return;
			}
			Toast toast = null;
			if (_engine.Out.Count > 0)
			{
				toast = _engine.Out.OrderByDescending((Toast x) => x.Weight).First();
				_engine.Out.Remove(toast);
				if (_engine.Out.Count > 8)
				{
					_engine.Out.RemoveRange(0, _engine.Out.Count - 8);
				}
			}
			else if (_lifeQueue.Count > 0 && now >= _nextLifeNotice)
			{
				toast = _lifeQueue[0];
				_lifeQueue.RemoveAt(0);
				_nextLifeNotice = now + _cfg.LifeUpdateGapSeconds * 1000;
			}
			if (toast != null)
			{
				_nextNotice = now + Math.Max(10, _cfg.ToastGapSeconds) * 1000;
				Route(toast);
			}
		}

		private bool PersonWoman(string id)
		{
			return ((id != null) ? _engine.Find(id) : null)?.Woman ?? false;
		}

		private string NameOf(string id)
		{
			return ((id != null) ? _engine.Find(id) : null)?.Name;
		}

		private void WriteDiaries()
		{
			if (_engine.DiaryChanged.Count == 0)
			{
				return;
			}
			foreach (string item in _engine.DiaryChanged.ToList())
			{
				PersonState personState = _store.Find(item);
				if (personState == null)
				{
					continue;
				}
				try
				{
					string text = new string((personState.Name ?? item).Select((char c) => (!Path.GetInvalidFileNameChars().Contains(c)) ? c : '_').ToArray());
					string path = Path.Combine(_dir, "diaries", _player + " - " + text + ".txt");
					StringBuilder stringBuilder = new StringBuilder();
					stringBuilder.AppendLine("The diary of " + personState.Name);
					stringBuilder.AppendLine();
					foreach (DiaryEntry item2 in personState.Diary)
					{
						stringBuilder.AppendLine(item2.Text);
						stringBuilder.AppendLine();
					}
					File.WriteAllText(path, stringBuilder.ToString(), Encoding.UTF8);
				}
				catch (Exception ex)
				{
					Log.Write("Couldn't write a diary: " + ex.Message);
				}
			}
			_engine.DiaryChanged.Clear();
		}

		private void OnKeyDown(object sender, KeyEventArgs e)
		{
			if (HeldKeyRepeat(e))
			{
				return;
			}
			if (KeyDown04(e))
			{
				return;
			}
			// Shift+F2, Shift+F7 and Shift+F9 are the Testing page's keys now ([Testing] TestFontKey, TestTalkKey, TestRoughKey)
			if (_testKey.Matches(e))
			{
				_testRequested = true;
			}
			else if (_flirtKey.Matches(e))
			{
				_flirtRequested = true;
			}
		}
		/// A status for the F7 dump that must never cost the lines after it (the night review, 2026-10-28: Test() runs outside any Safe).
		private static string Guarded(Func<string> status)
		{
			try
			{
				return status();
			}
			catch (Exception ex)
			{
				return "failed: " + ex.Message;
			}
		}


		private void Test()
		{
			Ped character = Game.Player.Character;
			RecordRef nemesisRecord = null;
			int recordHandle = 0;   // so the thought goes over his head, not in a notice (Lex, 2026-10-27)
			float num = 40f;
			Ped[] allPeds = World.GetAllPeds();
			foreach (Ped ped in allPeds)
			{
				if (ped.Handle == character.Handle)
				{
					continue;
				}
				float num2 = ped.Position.DistanceTo(character.Position);
				if (!(num2 >= num))
				{
					RecordRef nemesisRecord2 = _bridge.RecordAt(ped.Handle);
					if (nemesisRecord2 != null)
					{
						nemesisRecord = nemesisRecord2;
						recordHandle = ped.Handle;
						num = num2;
					}
				}
			}
			Log.Write("TEST: scene " + (_director.CurrentKind ?? "none") + ", plea " + (_plea != null ? _plea.Plea.Name : "none") + ", pickpocket " + (_theft != null ? "running" : "none") +
				", arrival queued " + (_arrival != null) + ", scene reactions " + (_cfg.SceneReactions ? "on" : "off") +
				", host tier " + TownHostTier() + ", mourners due " + _store.Fallen.Count(f => !f.Mourned && f.Returned == 0) + ", bounty " + LAW.GET_BOUNTY(Game.Player.Handle));
			Log.Write("TEST 0.4: ticker " + (_cfg.Ticker ? "on" : "off") + ", news to " + _cfg.NewsRoute + ", enRAGE menu open " + _uiOpen +
				", stalker " + (_stalker != null ? _stalker.Label : "none") + ", scene " + (_director.Busy ? "playing" : "none") + ", town " + (_placeTown ?? "-") + ", place " + (_placeKind ?? "-") +
				", deeds " + _store.Deeds.Count + ", rumours " + _store.TownRumours.Count + ", papers " + _store.Papers.Count(x => x.Delivered) + "/" + _store.Papers.Count +
				", fallen " + _store.Fallen.Count + ", reveries " + _store.Reveries.Values.Count(x => x.Woke) + "/" + _store.Reveries.Count + ", avengers " + _store.Avengers.Count + ", weddings " + _store.Weddings.Count);
			Log.Write("TEST stalker: " + StalkerStatus());
			Log.Write("TEST stalker acts: " + Stalker.ActsStatus);
			Log.Write("TEST worries: " + WorriesStatus());
			Log.Write("TEST mind: " + MindsStatus());
			Log.Write("TEST thought variety: " + (_cfg.ThoughtVariety ? "on; frames held back within the hour " + _engine.FramesHeld + ", choices with a source's pull spread " + _engine.SourcesSpread : "off ([Mind] ThoughtVariety): the old choice"));
			Log.Write("TEST language: " + LanguageStatus());
			Log.Write("TEST mind shadow: " + MindShadowStatus());
			Log.Write("TEST mind choices: " + MindChoicesStatus());
			Log.Write("TEST mind beliefs: " + MindBeliefsStatus());
			Log.Write("TEST mind levels: " + MindLevelsStatus());
			Log.Write("TEST ladder: " + LadderStatus());
			Log.Write("TEST joining fights: " + JoinStatus());
			Log.Write("TEST minds acting: " + MindActsStatus());
			Log.Write("TEST after fights: " + AftermathStatus());
			Log.Write("TEST gangs: " + GangStatus());
			Log.Write("TEST night: " + NightStatus());
			Log.Write("TEST rings: " + RingStatus());
			Log.Write("TEST horse thieves: " + HorseStatus());
			Log.Write("TEST perf: " + PerfStatus() + "; Core compiled ahead: " + _prejitStatus);
			Log.Write("TEST save: " + SaveStatus());
			Log.Write("TEST wake voices: " + WakeVoiceStatus());
			Log.Write("TEST wake anim sets: " + _wakeAnimsResult);
			Log.Write("TEST anim test: " + AnimTestStatus());
			Log.Write("TEST gunshot test: " + GunshotTestStatus());
			Log.Write("TEST testing: " + TestingStatus());
			Log.Write("TEST show minds: " + ShowMindsStatus());
			Log.Write("TEST name cards: " + NameCardsStatus());
			Log.Write("TEST law chase: " + LawChaseStatus());
			Log.Write("TEST lift: " + LiftStatus());
			Log.Write("TEST wake stages: " + WakeTagsStatus());
			Log.Write("TEST events: " + EventsStatus());
			Log.Write("TEST pacing: " + (_store != null ? Pacing.Status(_store.Pace, _store.LastGameMin) : "not loaded"));
			Log.Write("TEST near: " + NearStatus());
			Log.Write("TEST regulars: " + RegularsStatus());
			MarkRegulars();
			Log.Write("TEST ties: " + TiesStatus());
			Log.Write("TEST reasons: " + ReasonsStatus());
			Log.Write("TEST links: " + LinksStatus());
			Log.Write("TEST posts: " + PostsStatus());
			Log.Write("TEST encounters: " + EncountersStatus());
			Log.Write("TEST subtitles: " + SubtitlesStatus());
			Log.Write("TEST territory: " + TerritoryStatus());
			Log.Write("TEST flirt prompt: " + FlirtPromptStatus());
			Log.Write("TEST rough: " + RoughStatus());
			Log.Write("TEST reactions: " + ReactionsStatus());
			Log.Write("TEST passing: " + PassingStatus());
			Log.Write("TEST sights: " + SightsStatus());
			Log.Write("TEST kindness: " + KindnessStatus());
			Log.Write("TEST onlookers: " + OnlookersStatus());
			Log.Write("TEST awakening from doubt: " + DoubtStatus());
			Log.Write("TEST rescue: " + RescueStatus());
			Log.Write("TEST mind acts: " + MindActsTotals());
			Log.Write("TEST vices: " + VicesStatus());
			Log.Write("TEST grudges: " + GrudgesStatus());
			Log.Write("TEST kin: " + KinStatus());
			Log.Write("TEST own looks: " + OwnLooksStatus());
			Log.Write("TEST callout: " + CalloutStatus());
			Log.Write("TEST visitor: " + VisitorStatus());
			Log.Write("TEST traveller: " + TravellerStatus());
			Log.Write("TEST campfire: " + CampfireStatus());
			Log.Write("TEST supper: " + Guarded(SupperStatus));
			Log.Write("TEST preacher and old days: " + PreacherStatus());
			Log.Write("TEST lost child: " + LostChildStatus());
			Log.Write("TEST doctor's patient: " + PatientStatus());
			Log.Write("TEST test cast: " + Guarded(TestCastStatus));
			Log.Write("TEST controller: " + Guarded(PadStatus));
			Log.Write("TEST sabbath: " + SabbathStatus());
			Log.Write("TEST bell: " + BellStatus());
			Log.Write("TEST voice below: " + BelowStatus());
			Log.Write("TEST card cheats: " + _thrownOuts + " shown the door and thought of it outside this session");
			Log.Write("TEST condolences: " + _condoles + " this session" + (_condoleLast != null ? "; last: " + _condoleLast : ""));
			Log.Write("TEST trouble: " + TroubleStatus());
			Log.Write("TEST now: " + NowStatus());
			Log.Write("TEST records: " + RecordsStatus());
			Log.Write("TEST romance: " + RomanceStatus());
			Log.Write("TEST trysts: " + TrystStatus());
			Log.Write("TEST casual hookups: " + HookupsStatus());
			Log.Write("TEST thoughts: " + OverHeadStatus());
			Log.Write("TEST despair: " + DespairStatus());
			Log.Write("TEST disbelief: " + _disbeliefSaid + " this session" + (_disbeliefLast != null ? ", last " + _disbeliefLast : "") + " (GENERIC_SHOCKED_DISBELIEF, else a shocked line)");
			Log.Write("TEST ledger: " + LedgerStatus());
			MarkAwake();
			MarkRecords();   // after the awake marks, so its hint is the one shown when records are near
			Log.Write("TEST settings: " + SettingsStatus());
			if (nemesisRecord == null)
			{
				int num3 = _store.People.Values.Count((PersonState s) => s.Talks > 0 || s.Heard.Count > 0);
				// standing alone the engine's summary is of enRAGE's roster, which is empty: count our own townsfolk instead (Lex, 2026-10-29: "0 people in the world")
				string summary = (_bridge.Present ? _engine.Summary() : null)
					?? (_bridge.Present ? _recById.Count + " people in the world" : _store.Residents.Count((Resident r) => !r.Dead) + " townsfolk of its own, " + Regulars.Count(_store.Residents) + " of them town regulars (standing alone, no enRAGE)");
				Feed.Notice("RealNPCs " + _version, "running. " + summary + ". " + num3 + " who know you, " + _store.TownRumours.Count + " rumours, " + _store.Papers.Count(x => x.Delivered) + " papers in your satchel." + (_bridge.Present ? " Nobody enRAGE knows is close by." : ""));
				News("RealNPCs", "test: the ticker works. " + summary + ".");
				return;
			}
			Person person = PersonOf(nemesisRecord);
			PersonState personState = _store.Get(person);
			string thought = _engine.TickerThought(person, Bridge.Now());
			_ticker.Push(person.Name + ", thinking", thought, "near", recordHandle);   // over his head if he's in view, else passed over (never a notice)
			_ticker.Refresh(person.Id);
			Log.Write("TEST " + person.Name + " (" + num.ToString("0") + " m): " + Describe(person, personState) + ", awareness " + Hosts.Awareness(person, personState) + ", knows " + personState.KnownDeeds.Count + " deeds; thinks: " + thought);
		}

		private static string Describe(Person p, PersonState st)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.Append(p.Woman ? "woman" : "man").Append(", born ").Append(p.BornYear)
				.Append(", ")
				.Append(p.Trade ?? "?")
				.Append(", ")
				.Append(p.GangName ?? "no outfit")
				.Append(", home ")
				.Append(p.Home ?? "?")
				.Append(", ")
				.Append(p.Disposition)
				.Append("/")
				.Append(p.Sociability)
				.Append("/")
				.Append(p.Personality ?? "?")
				.Append(", hate ")
				.Append(p.Hate)
				.Append(", fear ")
				.Append(p.Fear)
				.Append(", mood ")
				.Append(p.Mood)
				.Append(" | talks ")
				.Append(st.Talks)
				.Append(", knows your name ")
				.Append(st.KnowsName)
				.Append(", trust ")
				.Append(Talk.Trust(p, st))
				.Append(", heard ")
				.Append(st.Heard.Count)
				.Append(", diary ")
				.Append(st.Diary.Count)
				.Append(", told: ")
				.Append(string.Join(" ", st.Told))
				.Append(" | romance: ")
				.Append(Romance.HeartNames[st.Heart + 1])
				.Append(", affection ")
				.Append(st.Affection)
				.Append(", flirts ")
				.Append(st.Flirts);
			foreach (MemoryItem memory in p.Memories)
			{
				stringBuilder.Append("\r\n    memory ").Append(memory.Emotion).Append(" ")
					.Append(memory.Intensity)
					.Append(memory.AboutYou ? " (you)" : "")
					.Append(": ")
					.Append(memory.Text);
			}
			foreach (PastItem item in p.Past)
			{
				stringBuilder.Append("\r\n    past ").Append(item.Year).Append(" ")
					.Append(item.Kind)
					.Append(item.Public ? " public" : "")
					.Append(item.Known ? " known" : "")
					.Append(": ")
					.Append(item.Text);
			}
			foreach (Relative item2 in st.Family ?? new List<Relative>())
			{
				stringBuilder.Append("\r\n    family ").Append(item2.Relation).Append(" ")
					.Append(item2.Name)
					.Append(", ")
					.Append(item2.Town)
					.Append(" (heard ")
					.Append(item2.Heard.Count)
					.Append(")");
			}
			return stringBuilder.ToString();
		}

		private unsafe void Flirt()
		{
			Ped character = Game.Player.Character;
			int num = 0;
			if (!PLAYER.IS_PLAYER_FREE_AIMING(Game.Player.Handle))
			{
				PLAYER.GET_PLAYER_INTERACTION_TARGET_ENTITY(Game.Player.Handle, &num, p2: false, p3: false);
			}
			Ped ped = ((num != 0) ? new Ped(num) : NearestFacing(character, 4f));
			if (Game.GameTime < _flirtSaidUntil)
			{
				Log.Write("FLIRT ignored: the last one is still being said.");
				return;
			}
			if (ped == null)
			{
				Log.Write("FLIRT: nobody to flirt with (lock on to someone, or face them within 4 m).");
				if (!_flirtHinted)
				{
					_flirtHinted = true;
					KeySpec flirtKey = _flirtKey;
					Feed.Notice("RealNPCs", "look at someone the way you would to greet them, then press " + flirtKey.ToString() + " to flirt.");
				}
				return;
			}
			num = ped.Handle;
			if (!ENTITY.DOES_ENTITY_EXIST(num) || ENTITY.IS_ENTITY_DEAD(num) || !PED.IS_PED_HUMAN(num) || PED.IS_PED_A_PLAYER(num) || ped.Position.DistanceTo(character.Position) > 8f || PED.IS_PED_IN_COMBAT(num, 0))
			{
				Log.Write("FLIRT ignored: " + (ped.Position.DistanceTo(character.Position) > 8f ? "too far off (" + (int)ped.Position.DistanceTo(character.Position) + " m)" : PED.IS_PED_IN_COMBAT(num, 0) ? "they're fighting" : "not someone to flirt with") + ".");
				return;
			}
			if (!IsGrownUp(ped))
			{
				Log.Write("FLIRT ignored: not a grown-up.");
				return;
			}
			if (Natives.StoryModel(ped))
			{
				Log.Write("FLIRT ignored: one of the story's own people.");   // never anyone the story holds
				return;
			}
			if (_hookAsk)
			{
				// the Hook up prompt (G): the ask, never another flirt (Lex, 2026-10-29); H only ever flirts
				_hookAsk = false;
				if (!TrystAsked(ped))
					Log.Write("TRYST Hook up pressed, but she isn't willing now (or it timed out)");
				return;
			}
			long now = Bridge.Now();
			_engine.PlayerCash = MONEY._MONEY_GET_CASH_BALANCE() / 100;   // what the greedy and the proud look at
			_engine.GreetedFirst = GreetedFirst(num);   // greeting her first counts (Lex, 2026-10-26)
			RecordRef nemesisRecord = _bridge.RecordAt(num);
			if (nemesisRecord != null && nemesisRecord.IsAlive)
			{
				Person person = PersonOf(nemesisRecord);
				if (!Romance.Adult(person))
				{
					Log.Write("FLIRT ignored: " + person.Name + " isn't a grown-up (or enRAGE has no birth year).");
					return;
				}
				FlirtResult r = _engine.FlirtWith(person, now, Watchers(character, num));
				SayFlirt(r, person.Name, num, person.Hate);
				LookAt(num, character.Handle);
				ApplyEffects();
				NoteFlirt(ped, person.Name);
				return;
			}
			FolkRef acquaintance = _bridge.AcquaintanceAt(num);
			if (acquaintance != null)
			{
				Folk folk = Snapshot.Of(acquaintance);
				SayFlirt(_engine.FlirtWithFolk(folk, grownUp: true, now), folk.Name, num, folk.Grudge);
				LookAt(num, character.Handle);
				ApplyEffects();
				NoteFlirt(ped, folk.Name);
			}
			else if (!Natives.OtherScripts(num))
			{
				// one of the add-on's own people: what they think of him decides it, as for enRAGE's (Regard, Interest)
				Resident res = ResidentEligible(ped) ? ResidentOf(ped) : null;
				if (res != null)
				{
					string shown = ShownName(num, res.Name);
					SayFlirt(_engine.FlirtWithResident(_engine.PersonOfResident(res), now), shown, num, 0);
					LookAt(num, character.Handle);
					NoteFlirt(ped, shown);
					return;
				}
				bool flag = !PED.IS_PED_MALE(num);
				bool drawn = Romance.DrawnTo("ped" + num, flag, otherIsWoman: false, _cfg.SameSexRomance);
				string seed = num + "|" + Game.GameTime / 20000;
				string reply = Romance.StrangerReply(flag, drawn, seed);
				string said = flag ? "A woman" : "A man";
				Feed.DropAhead();
				Feed.Say(_engine.Player.FirstName, Romance.PlayerLine(_engine.John, flag, 0, seed), FlirtVoice(_engine.Player.FirstName, Speech.WhenFirst(character.Handle, FlirtVoices.You(flag, seed), num)), ahead: true);
				Feed.Say(said, reply, FlirtVoice(said, Speech.WhenFirst(num, FlirtVoices.Reply(new Person { Woman = flag }, drawn ? "yes" : "no", drawn ? null : "notdrawn", 0, seed), character.Handle)), toYou: true, ahead: true);
				_flirtSaidUntil = Game.GameTime + 7000;
				LookAt(num, character.Handle);
				NoteFlirt(ped, Director.Kind(ped, flag));
			}
		}

		private void SayFlirt(FlirtResult r, string name, int ped, int hate)
		{
			if (r == null || r.YouSay == null)
			{
				return;
			}
			int me = Game.Player.Character.Handle;
			// several of the game's lines for each answer and reason, in an order that changes by person and every ten
			// game minutes (Core/FlirtVoices.cs); the first her voice has plays
			string seed = (r.Who != null ? r.Who.Id : ped.ToString()) + "|" + Bridge.Now() / 10;
			bool toWoman = r.Who != null ? r.Who.Woman : !PED.IS_PED_MALE(ped);
			Feed.DropAhead();   // what's left of the last flirt isn't said over this one
			// the player asked for this: it goes ahead of what's waiting; Arthur's own line gets no subtitle, the reply does
			Feed.Say(_engine.Player.FirstName, r.YouSay, FlirtVoice(_engine.Player.FirstName, Speech.WhenFirst(me, FlirtVoices.You(toWoman, seed), ped)), ahead: true);
			if (r.Reply != null)
			{
				Func<string> voice = r.Answer != null ? Speech.WhenFirst(ped, FlirtVoices.Reply(r.Who, r.Answer, r.Why, r.Heart, seed), me)
					// the old rule had no answer: a voice that fits the words
					: Speech.For(ped, r.Reply, me, hate, fallback: "respond");
				Feed.Say(name, r.Reply, FlirtVoice(name, voice), toYou: true, ahead: true);
			}
			_flirtSaidUntil = Game.GameTime + 7000;
			TrystOffered(r, name, ped);
			foreach (KeyValuePair<Person, string> jealou in r.Jealous)
			{
				Feed.Mutter(jealou.Key.Name, jealou.Key.Woman, jealou.Value);
			}
		}

		/// A flirting voice that says in the log which line played (FLIRT voice:), to learn which lines Arthur's voice and
		/// the women's really have; Feed tries a line said to you again if it didn't start, and each try is logged.
		private static Func<string> FlirtVoice(string who, Func<string> voice)
		{
			if (voice == null)
				return null;
			return () =>
			{
				Speech.LastLine = null;
				string words = voice();
				Log.Write("FLIRT voice: " + who + ": " + (Speech.LastPlayed ? Speech.LastLine ?? "a line" : "none of its lines played"));
				return words;
			};
		}

		private static Ped NearestFacing(Ped me, float range)
		{
			Ped result = null;
			float num = range;
			Ped[] allPeds = World.GetAllPeds();
			foreach (Ped ped in allPeds)
			{
				int handle = ped.Handle;
				if (handle != me.Handle && PED.IS_PED_HUMAN(handle) && !ENTITY.IS_ENTITY_DEAD(handle))
				{
					float num2 = ped.Position.DistanceTo(me.Position);
					if (num2 < num && PED.IS_PED_FACING_PED(me.Handle, handle, 50f))
					{
						result = ped;
						num = num2;
					}
				}
			}
			return result;
		}

		private static bool IsGrownUp(Ped p)
		{
			if (p == null || !p.Exists() || PED._IS_PED_CHILD(p.Handle))
			{
				return false;
			}
			string input = (Models.Name((uint)p.Model.Hash) ?? string.Empty).ToLowerInvariant();
			return !Childlike.IsMatch(input);
		}

		private List<Person> Watchers(Ped me, int except)
		{
			List<Person> list = new List<Person>();
			Ped[] allPeds = World.GetAllPeds();
			foreach (Ped ped in allPeds)
			{
				int handle = ped.Handle;
				if (handle != me.Handle && handle != except && !ENTITY.IS_ENTITY_DEAD(handle) && !(ped.Position.DistanceTo(me.Position) > 25f))
				{
					RecordRef nemesisRecord = _bridge.RecordAt(handle);
					if (nemesisRecord != null && nemesisRecord.IsAlive && ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(handle, me.Handle, 17))
					{
						list.Add(PersonOf(nemesisRecord));
					}
				}
			}
			return list;
		}

		private static void LookAt(int ped, int target)
		{
			try
			{
				TASK.TASK_LOOK_AT_ENTITY(ped, target, 3000, 0, 51, 0);
			}
			catch
			{
			}
		}

		private void Insults(int now)
		{
			for (int num = _insults.Count - 1; num >= 0; num--)
			{
				if (now >= _insults[num].Value)
				{
					int key = _insults[num].Key;
					_insults.RemoveAt(num);
					if (ENTITY.DOES_ENTITY_EXIST(key) && !ENTITY.IS_ENTITY_DEAD(key) && SceneIntrusion(key, friendly: false))
					{
						continue;
					}
					if (TravellerGreeted(key, false))
					{
						continue;   // antagonizing the traveller asking the way: he backs off
					}
					if (SupperGreeted(key, false))
					{
						continue;   // antagonizing the man with supper on: he lets you go
					}
					if (_cfg.Romance && ENTITY.DOES_ENTITY_EXIST(key) && !ENTITY.IS_ENTITY_DEAD(key))
					{
						RecordRef nemesisRecord = _bridge.RecordAt(key);
						if (nemesisRecord != null && nemesisRecord.IsAlive)
						{
							Person person = PersonOf(nemesisRecord);
							string text = _engine.Insulted(person, Bridge.Now());
							if (text != null && !Feed.Talking)
							{
								Feed.Say(person.Name, text, Speech.For(key, text, Game.Player.Character.Handle, person.Hate, person.Fear, fallback: "mild"), toYou: true);
							}
							ApplyEffects();
						}
					}
				}
			}
		}

		private void ApplyEffects()
		{
			if (_engine == null || _engine.Effects.Count == 0)
			{
				return;
			}
			List<EnrageEffect> list = _engine.Effects.ToList();
			// residents live only in this add-on: nothing about them is ever written to enRAGE
			list.RemoveAll(x => Resident.IsResident(x.PersonId) || Resident.IsResident(x.OtherId));
			_engine.Effects.Clear();
			if (!_bridge.CanWrite)
			{
				return;
			}
			bool flag = false;
			foreach (EnrageEffect item in list)
			{
				if (item.Romance && !_cfg.RomanceAffectsEnrage)
				{
					continue;
				}
				RecordRef value;
				if (item.Kind == "like")
				{
					_bridge.LikeMore(item.PersonId.StartsWith("folk:") ? item.PersonId.Substring(5) : item.PersonId, item.Amount);
				}
				else if (item.PersonId != null && _recById.TryGetValue(item.PersonId, out value) && value != null)
				{
					switch (item.Kind)
					{
					case "thought":
						flag |= _bridge.AddThought(value, item.Mood, item.Text, item.Amount, item.Hours);
						break;
					case "memory":
						flag |= _bridge.Remember(value, item.Emotion, item.Amount, item.Text, item.Cause);
						break;
					case "history":
						flag |= _bridge.History(value, item.Text);
						break;
					case "hate":
						value.AddHate(item.Amount);
						flag = true;
						break;
					case "marry":
						Marry(value, item);
						flag = true;
						break;
					}
					Log.Detail("ENRAGE " + value.Name + ": " + item);
				}
			}
			if (flag)
			{
				_bridge.RosterChanged();
			}
		}

		private void Marry(RecordRef r, EnrageEffect e)
		{
			DateTime dateTime = GameDate.FromMinutes(Bridge.Now());
			string friendId = e.OtherId != null && _recById.ContainsKey(e.OtherId) ? e.OtherId : null;
			Typed.MarryApply(r.Raw, e.OtherName, friendId, dateTime.Year, dateTime.Month);   // the writes themselves (Typed.cs)
			Log.Write("MARRIED " + r.Name + " and " + e.OtherName + " (written into enRAGE's record)");
		}

		private string[] IntelFor(Person p)
		{
			if (!_cfg.RomanceAffectsEnrage || !_recById.TryGetValue(p.Id, out var value))
			{
				return null;
			}
			object plan;
			string[] array = _bridge.Intel(value, out plan);
			if (plan != null)
			{
				_pendingPlans.Add(new KeyValuePair<object, RecordRef>(plan, value));
			}
			Log.Write("INTEL from " + p.Name + ": " + ((array == null) ? "nothing to tell" : ((array[0] ?? "-") + " / " + (array[1] ?? "-"))));
			return array;
		}

		private void RevealPlans()
		{
			if (_pendingPlans.Count == 0)
			{
				return;
			}
			foreach (KeyValuePair<object, RecordRef> pendingPlan in _pendingPlans)
			{
				_bridge.RevealPlan(pendingPlan.Key, pendingPlan.Value);
			}
			_pendingPlans.Clear();
		}

		private void Ambient(int now)
		{
			if (Feed.Busy)
			{
				return;
			}
			Ped character = Game.Player.Character;
			Vector3 position = character.Position;
			List<Ped> list = new List<Ped>();
			Ped[] allPeds = World.GetAllPeds();
			foreach (Ped ped in allPeds)
			{
				int handle = ped.Handle;
				if (handle != character.Handle && !ENTITY.IS_ENTITY_DEAD(handle) && PED.IS_PED_HUMAN(handle) && !PED.IS_PED_IN_COMBAT(handle, 0) && ped.Position.DistanceTo(position) <= 18f)
				{
					list.Add(ped);
				}
			}
			if (list.Count < 2)
			{
				return;
			}
			if (_cfg.NpcRomance && now - _lastCoupleAt > 120000)
			{
				List<KeyValuePair<Ped, RecordRef>> list2 = new List<KeyValuePair<Ped, RecordRef>>();
				foreach (Ped item in list)
				{
					RecordRef nemesisRecord = _bridge.RecordAt(item.Handle);
					if (nemesisRecord != null && nemesisRecord.IsAlive)
					{
						list2.Add(new KeyValuePair<Ped, RecordRef>(item, nemesisRecord));
					}
				}
				for (int j = 0; j < list2.Count; j++)
				{
					for (int k = j + 1; k < list2.Count; k++)
					{
						if (list2[j].Key.Position.DistanceTo(list2[k].Key.Position) > 6f)
						{
							continue;
						}
						List<KeyValuePair<string, string>> list3 = _engine.CoupleLines(PersonOf(list2[j].Value), PersonOf(list2[k].Value), Bridge.Now());
						if (list3 == null)
						{
							continue;
						}
						int a = list2[j].Key.Handle, b = list2[k].Key.Handle;
						string aName = list2[j].Value.Name;
						foreach (KeyValuePair<string, string> item2 in list3)
						{
							bool first = item2.Key == aName;
							Feed.Say(item2.Key, item2.Value, Speech.For(first ? a : b, item2.Value, first ? b : a, fallback: "chat"));
						}
						LookAt(list2[j].Key.Handle, list2[k].Key.Handle);
						LookAt(list2[k].Key.Handle, list2[j].Key.Handle);
						_lastCoupleAt = now;
						Log.Write("COUPLE " + list3[0].Key + " & " + list3[1].Key);
						return;
					}
				}
			}
			if (!_cfg.AmbientFlirting || now - _lastStrangersAt < _cfg.AmbientFlirtSeconds * 1000 || _rng.Next(100) >= 20)
			{
				return;
			}
			List<Ped> list4 = list.Where((Ped p) => ENTITY.GET_ENTITY_SPEED(p.Handle) < 0.6f && IsGrownUp(p) && (!Natives.OtherScripts(p.Handle) || _bridge.AcquaintanceAt(p.Handle) != null)).ToList();
			for (int num = 0; num < list4.Count; num++)
			{
				for (int num2 = num + 1; num2 < list4.Count; num2++)
				{
					Ped ped2 = list4[num];
					Ped ped3 = list4[num2];
					if (ped2.Position.DistanceTo(ped3.Position) > 2.5f)
					{
						continue;
					}
					bool flag = PED.IS_PED_MALE(ped2.Handle);
					bool flag2 = PED.IS_PED_MALE(ped3.Handle);
					if (flag != flag2 || _cfg.SameSexRomance)
					{
						if (!flag && flag2 && _rng.Next(2) == 0)
						{
							Ped ped4 = ped2;
							ped2 = ped3;
							ped3 = ped4;
							flag = true;
							flag2 = false;
						}
						List<string> list5 = Romance.Strangers(flag, flag2, ped2.Handle + "|" + ped3.Handle + "|" + now / 600000);
						Feed.Say(Label(ped2, flag, second: false), list5[0], Speech.For(ped2.Handle, list5[0], ped3.Handle, fallback: "flatter"));
						Feed.Say(Label(ped3, flag2, flag == flag2), list5[1], Speech.For(ped3.Handle, list5[1], ped2.Handle, fallback: "respond"));
						LookAt(ped2.Handle, ped3.Handle);
						LookAt(ped3.Handle, ped2.Handle);
						_lastStrangersAt = now;
						return;
					}
				}
			}
		}

		private string Label(Ped p, bool man, bool second)
		{
			FolkRef acquaintance = _bridge.AcquaintanceAt(p.Handle);
			if (acquaintance != null && !string.IsNullOrEmpty(acquaintance.Name))
			{
				return acquaintance.FullName;
			}
			if (second)
			{
				if (!man)
				{
					return "Another woman";
				}
				return "Another man";
			}
			if (!man)
			{
				return "A woman";
			}
			return "A man";
		}
	}
}
