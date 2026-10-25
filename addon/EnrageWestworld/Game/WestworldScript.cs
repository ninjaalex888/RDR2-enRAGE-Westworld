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
using NemesisSystem;
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

		private bool _toldMissing;

		private bool _testRequested;

		private bool _snapshotFailed;

		private readonly int _loadedAt;

		private int _nextFind;

		private int _nextObserve;

		private int _nextRemarks;

		private int _nextThought;

		private int _nextLife;

		private int _nextSave;

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

		private Dictionary<string, NemesisRecord> _recById = new Dictionary<string, NemesisRecord>();

		private KeySpec _testKey;

		private KeySpec _flirtKey;

		private bool _flirtRequested;

		private readonly Random _rng = new Random();

		private static readonly Regex Childlike = new Regex("kid|child|teen|jack|(?<!cow)boy|(?<!show|saloon|cow|bar)girl");

		private int _nextAmbient;

		private int _lastCoupleAt = -1000000;

		private int _lastStrangersAt = -1000000;

		private bool _flirtHinted;

		private readonly List<KeyValuePair<object, NemesisRecord>> _pendingPlans = new List<KeyValuePair<object, NemesisRecord>>();

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
			Feed.UseGameFeed = _cfg.UseGameFeed;
			_testKey = KeySpec.Parse(_cfg.TestKey, Keys.F7);
			_flirtKey = KeySpec.Parse(_cfg.FlirtKey, Keys.F6);
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
						Save();
						return;
					}
					catch
					{
						return;
					}
				}
			}
		}

		private void Step()
		{
			if (!InWorld())
			{
				return;
			}
			int gameTime = Game.GameTime;
			Feed.Tick();
			Frame(gameTime);
			SettingsStep(gameTime);
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
				Test();
			}
			if (_flirtRequested)
			{
				_flirtRequested = false;
				if (_cfg.Romance)
				{
					Flirt();
				}
			}
			ReaderInput();
			PollWorldEvents(gameTime);
			PollGreets(gameTime);
			Greets(gameTime);
			Insults(gameTime);
			if (gameTime >= _nextObserve)
			{
				_nextObserve = gameTime + 5000;
				Observe();
			}
			ApplyEffects();
			WorldRequests();
			if (!Feed.Busy)
			{
				RevealPlans();
			}
			Tick04(gameTime);
			bool flag = PLAYER.IS_PLAYER_CONTROL_ON(Game.Player.Handle);
			if (_cfg.Remarks && flag && gameTime >= _nextRemarks)
			{
				_nextRemarks = gameTime + 1000;
				Remarks(gameTime);
			}
			if (flag && (_cfg.AmbientFlirting || _cfg.NpcRomance) && gameTime >= _nextAmbient)
			{
				_nextAmbient = gameTime + 2000;
				Ambient(gameTime);
			}
			if (_cfg.Thoughts && flag && gameTime >= _nextThought)
			{
				_nextThought = gameTime + (int)((double)(_cfg.ThoughtEverySeconds * 1000) * (0.75 + _rng.NextDouble() * 0.5));
				Toast toast = _engine.NextThought(Bridge.Now());
				if (toast != null)
				{
					_engine.Out.Add(toast);
				}
			}
			if (_cfg.LifeUpdates && gameTime >= _nextLife)
			{
				_nextLife = gameTime + 10000;
				Life();
			}
			if (flag)
			{
				Notices(gameTime);
			}
			if (gameTime >= _nextSave)
			{
				_nextSave = gameTime + 60000;
				Save();
			}
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
				if (!_toldMissing && Environment.TickCount - _loadedAt > 120000)
				{
					_toldMissing = true;
					Log.Write("enRAGE isn't running (not among Script Hook .NET's scripts). The add-on waits for it.");
					Feed.Notice("RealNPCs", "enRAGE isn't running. This add-on needs enRAGE installed.");
				}
				return;
			}
			if (!_bridge.Usable)
			{
				_off = true;
				Feed.Notice("RealNPCs", "This enRAGE version isn't supported yet (see RealNPCs.log).");
				return;
			}
			List<NemesisRecord> list = _bridge.Roster();
			if (list != null && list.Count != 0 && Bridge.Now() > 0)
			{
				LoadStore();
				Observe();
				int num = _store.People.Values.Count((PersonState s) => s.Talks > 0 || s.Heard.Count > 0);
				Log.Write("Ready: enRAGE " + _bridge.Version + ", " + list.Count + " people, playing as " + PlayerName() + ", " + _store.People.Count + " people in the add-on's memory.");
				Feed.Notice("RealNPCs", "listening. " + list.Count + " people in the world" + ((num > 0) ? (", " + num + " who know you.") : "."));
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
			_store = Store.Load(StorePath, Log.Write);
			_store.Player = _player;
			_engine = new Engine(_store, PlayerRef.For(flag), _cfg, Environment.TickCount)
			{
				DateText = (long m) => GameDate.FromMinutes(m).ToString("MMMM d, yyyy", CultureInfo.InvariantCulture),
				Log = Log.Write,
				John = flag,
				IntelFor = IntelFor
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

		private void Save()
		{
			if (_store == null || _player == null)
			{
				return;
			}
			try
			{
				_store.Save(StorePath);
			}
			catch (Exception ex)
			{
				Log.Write("Couldn't save " + StorePath + ": " + ex.Message);
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
			List<NemesisRecord> list = _bridge.Roster();
			long num = Bridge.Now();
			if (list == null || num <= 0)
			{
				return;
			}
			Voice.NowYear = GameDate.FromMinutes(num).Year;
			Dictionary<string, NemesisRecord> dictionary = new Dictionary<string, NemesisRecord>();
			foreach (NemesisRecord item in list)
			{
				if (item != null && item.Id != null && !dictionary.ContainsKey(item.Id))
				{
					dictionary[item.Id] = item;
				}
			}
			_recById = dictionary;
			List<Person> list2 = new List<Person>(list.Count);
			foreach (NemesisRecord value in dictionary.Values)
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
			Vector3 position = Game.Player.Character.Position;
			_engine.PlayerX = position.X;
			_engine.PlayerY = position.Y;
			_engine.PlayerZ = position.Z;
			_engine.Observe(list2, num);
			WriteDiaries();
		}

		private Person PersonOf(NemesisRecord r)
		{
			return Snapshot.Of(r, _bridge, _recById, Bridge.Now());
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
						NemesisRecord nemesisRecord = _bridge.RecordAt(key);
						if (_cfg.Conversations)
						{
							if (nemesisRecord != null && nemesisRecord.IsAlive)
							{
								TalkTo(nemesisRecord, key);
							}
							else
							{
								Acquaintance acquaintance = _bridge.AcquaintanceAt(key);
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

		private void TalkTo(NemesisRecord rec, int ped)
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
					Log.Write("Marked " + num + " of " + person.Name + "'s past as known in enRAGE.");
				}
			}
			_remarked[person.Id] = Game.GameTime;
		}

		private void TalkTo(Acquaintance a, int ped)
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
				Feed.Say(you ? firstName : speaker, text, voice);
			}
		}

		private List<Gossip> GossipFor(Person p)
		{
			List<Gossip> list = new List<Gossip>();
			List<WorldEvent> list2 = _bridge.Events();
			if (list2 == null)
			{
				return list;
			}
			long num = Bridge.Now();
			Dictionary<string, string> dictionary = new Dictionary<string, string>();
			if (p.KinId != null && _recById.TryGetValue(p.KinId, out var value))
			{
				dictionary[p.KinId] = "my " + RelationOf(p.KinRelation, value) + " " + Person.FirstOf(value.Name);
			}
			foreach (NemesisRecord value4 in _recById.Values)
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
				WorldEvent worldEvent = list2[num2];
				if (worldEvent != null && worldEvent.Title != null && num - worldEvent.GameMin <= 14400 && worldEvent.People != null)
				{
					foreach (WorldPart person in worldEvent.People)
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

		private static string RelationOf(string pIsToKin, NemesisRecord kin)
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
				NemesisRecord nemesisRecord = _bridge.RecordAt(handle);
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
							Show(person.Name, person.Woman, remark, Speech.For(handle, remark.Text, character.Handle, person.Hate, person.Fear, person.Friendly, remark.Uncanny ? "uneasy" : null));
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
					Acquaintance acquaintance = _bridge.AcquaintanceAt(handle);
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

		/// A passing remark. Muttered ones stay silent; the rest get the speaker's recorded voice when one fits (uncanny ones
		/// an unsettled one).
		private static void Show(string name, bool woman, Remark r, Func<string> voice = null)
		{
			Log.Write("REMARK " + name + (r.Muttered ? " (muttered)" : "") + ": " + r.Text);
			if (r.Muttered)
			{
				Feed.Mutter(name, woman, r.Text);
			}
			else
			{
				Feed.Say(name, r.Text, voice);
			}
		}

		private void Life()
		{
			List<WorldEvent> list = _bridge.Events();
			if (list == null)
			{
				return;
			}
			long num = Bridge.Now();
			for (int i = Math.Max(0, list.Count - 40); i < list.Count; i++)
			{
				WorldEvent worldEvent = list[i];
				if (worldEvent != null && worldEvent.Id != null && !worldEvent.Id.StartsWith("ww_") && !worldEvent.IsPast && num - worldEvent.GameMin <= 2880)
				{
					Toast toast = _engine.LifeUpdate(Snapshot.Of(worldEvent));
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
			if (_testKey.Matches(e))
			{
				_testRequested = true;
			}
			else if (_flirtKey.Matches(e))
			{
				_flirtRequested = true;
			}
		}

		private void Test()
		{
			Ped character = Game.Player.Character;
			NemesisRecord nemesisRecord = null;
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
					NemesisRecord nemesisRecord2 = _bridge.RecordAt(ped.Handle);
					if (nemesisRecord2 != null)
					{
						nemesisRecord = nemesisRecord2;
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
			Log.Write("TEST posts: " + PostsStatus());
			if (nemesisRecord == null)
			{
				int num3 = _store.People.Values.Count((PersonState s) => s.Talks > 0 || s.Heard.Count > 0);
				string summary = _engine.Summary() ?? (_recById.Count + " people in the world");
				Feed.Notice("RealNPCs " + _version, "running. " + summary + ". " + num3 + " who know you, " + _store.TownRumours.Count + " rumours, " + _store.Papers.Count(x => x.Delivered) + " papers in your satchel. Nobody enRAGE knows is close by.");
				News("RealNPCs", "test: the ticker works. " + summary + ".");
				return;
			}
			Person person = PersonOf(nemesisRecord);
			PersonState personState = _store.Get(person);
			string thought = _engine.TickerThought(person, Bridge.Now());
			Feed.Notice(person.Name + ", to " + (person.Woman ? "herself" : "himself"), "\"" + thought + "\"");
			_ticker.Refresh(person.Id);
			Log.Write("TEST " + person.Name + " (" + num.ToString("0") + " m): " + Describe(person, personState) + ", awareness " + Hosts.Awareness(person, personState) + ", knows " + personState.KnownDeeds.Count + " deeds");
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
			if (ped == null)
			{
				if (!_flirtHinted)
				{
					_flirtHinted = true;
					KeySpec flirtKey = _flirtKey;
					Feed.Notice("RealNPCs", "look at someone the way you would to greet them, then press " + flirtKey.ToString() + " to flirt.");
				}
				return;
			}
			num = ped.Handle;
			if (!ENTITY.DOES_ENTITY_EXIST(num) || ENTITY.IS_ENTITY_DEAD(num) || !PED.IS_PED_HUMAN(num) || PED.IS_PED_A_PLAYER(num) || ped.Position.DistanceTo(character.Position) > 8f || PED.IS_PED_IN_COMBAT(num, 0) || Feed.Talking)
			{
				return;
			}
			if (!IsGrownUp(ped))
			{
				Log.Write("FLIRT ignored: not a grown-up.");
				return;
			}
			long now = Bridge.Now();
			NemesisRecord nemesisRecord = _bridge.RecordAt(num);
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
			Acquaintance acquaintance = _bridge.AcquaintanceAt(num);
			if (acquaintance != null)
			{
				Folk folk = Snapshot.Of(acquaintance);
				SayFlirt(_engine.FlirtWithFolk(folk, grownUp: true, now), folk.Name, num, folk.Grudge);
				LookAt(num, character.Handle);
				ApplyEffects();
				NoteFlirt(ped, folk.Name);
			}
			else if (!ENTITY.IS_ENTITY_A_MISSION_ENTITY(num))
			{
				bool flag = !PED.IS_PED_MALE(num);
				bool drawn = Romance.DrawnTo("ped" + num, flag, otherIsWoman: false, _cfg.SameSexRomance);
				string seed = num + "|" + Game.GameTime / 20000;
				string reply = Romance.StrangerReply(flag, drawn, seed);
				Feed.Say(_engine.Player.FirstName, Romance.PlayerLine(_engine.John, flag, 0, seed), Speech.When(character.Handle, "flatter", num));
				Feed.Say(flag ? "A woman" : "A man", reply, Speech.For(num, reply, character.Handle, fallback: drawn ? "flatter" : "no"));
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
			Feed.Say(_engine.Player.FirstName, r.YouSay, Speech.When(me, "flatter", ped));
			if (r.Reply != null)
			{
				Feed.Say(name, r.Reply, Speech.For(ped, r.Reply, me, hate));
			}
			foreach (KeyValuePair<Person, string> jealou in r.Jealous)
			{
				Feed.Mutter(jealou.Key.Name, jealou.Key.Woman, jealou.Value);
			}
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
					NemesisRecord nemesisRecord = _bridge.RecordAt(handle);
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
					if (_cfg.Romance && ENTITY.DOES_ENTITY_EXIST(key) && !ENTITY.IS_ENTITY_DEAD(key))
					{
						NemesisRecord nemesisRecord = _bridge.RecordAt(key);
						if (nemesisRecord != null && nemesisRecord.IsAlive)
						{
							Person person = PersonOf(nemesisRecord);
							string text = _engine.Insulted(person, Bridge.Now());
							if (text != null && !Feed.Talking)
							{
								Feed.Say(person.Name, text, Speech.For(key, text, Game.Player.Character.Handle, person.Hate, person.Fear, fallback: "mild"));
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
				NemesisRecord value;
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

		private void Marry(NemesisRecord r, EnrageEffect e)
		{
			r.Family = "married";
			if (r.Past == null)
			{
				r.Past = new List<PastEntry>();
			}
			DateTime dateTime = GameDate.FromMinutes(Bridge.Now());
			r.Past.Add(new PastEntry
			{
				Year = dateTime.Year,
				Month = dateTime.Month,
				Text = "Married " + e.OtherName,
				Kind = "family",
				Public = true,
				Known = true
			});
			if (e.OtherId != null && _recById.TryGetValue(e.OtherId, out var _) && r.FriendIds != null && !r.FriendIds.Contains(e.OtherId) && r.FriendIds.Count < 3)
			{
				r.FriendIds.Add(e.OtherId);
			}
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
				_pendingPlans.Add(new KeyValuePair<object, NemesisRecord>(plan, value));
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
			foreach (KeyValuePair<object, NemesisRecord> pendingPlan in _pendingPlans)
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
				List<KeyValuePair<Ped, NemesisRecord>> list2 = new List<KeyValuePair<Ped, NemesisRecord>>();
				foreach (Ped item in list)
				{
					NemesisRecord nemesisRecord = _bridge.RecordAt(item.Handle);
					if (nemesisRecord != null && nemesisRecord.IsAlive)
					{
						list2.Add(new KeyValuePair<Ped, NemesisRecord>(item, nemesisRecord));
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
			List<Ped> list4 = list.Where((Ped p) => ENTITY.GET_ENTITY_SPEED(p.Handle) < 0.6f && IsGrownUp(p) && (!ENTITY.IS_ENTITY_A_MISSION_ENTITY(p.Handle) || _bridge.AcquaintanceAt(p.Handle) != null)).ToList();
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
			Acquaintance acquaintance = _bridge.AcquaintanceAt(p.Handle);
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
