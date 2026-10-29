using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using EnrageWestworld.Core;
using RDR2.Math;

namespace EnrageWestworld.InGame
{
	internal sealed partial class Bridge
	{
		private const BindingFlags AnyInstance = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

		private const BindingFlags AnyStatic = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static;

		public readonly object Mod;

		public readonly string Version;

		private readonly MethodInfo _recordAt;

		private readonly PropertyInfo _roster;

		private readonly PropertyInfo _chronicle;

		private readonly FieldInfo _acqOf;

		private readonly MethodInfo _townAt;

		private readonly MethodInfo _zoneAt;

		private readonly MethodInfo _gangById;

		private readonly Dictionary<string, string> _gangNames = new Dictionary<string, string>();

		private readonly HashSet<string> _failed = new HashSet<string>();

		private MethodInfo _addThought;

		private MethodInfo _remember;

		private MethodInfo _history;

		private MethodInfo _saveRoster;

		private MethodInfo _saveWorld;

		private MethodInfo _factionJob;

		private MethodInfo _intelNotice;

		private MethodInfo _agendaText;

		private MethodInfo _whenText;

		private PropertyInfo _people;

		private FieldInfo _peopleDirty;

		// One ref per raw object of enRAGE's (standing alone, 2026-10-29): reference equality holds as it did on the raw objects,
		// and the refs go when enRAGE drops the objects (a save point restored swaps the roster's).
		private readonly ConditionalWeakTable<object, RecordRef> _recs = new ConditionalWeakTable<object, RecordRef>();
		private readonly ConditionalWeakTable<object, FolkRef> _folks = new ConditionalWeakTable<object, FolkRef>();

		private static Func<long> _clock;
		private static int _clockLookedAt = -1;

		public bool Usable
		{
			get
			{
				if (_recordAt != null)
				{
					return _roster != null;
				}
				return false;
			}
		}

		public bool CanWrite { get; private set; }

		public bool CanTellPlans { get; private set; }

		/// The bridge with no enRAGE behind it (standing alone, 2026-10-29, roadmap 34): every lookup finds nothing, every
		/// write is refused, the places come from our own tables (Places.cs), the clock from GameDate. Mod is null and
		/// Present false; nothing else in the add-on need test for it, since every method answers as it would when the
		/// member it wanted wasn't found in enRAGE.
		public static Bridge Absent()
		{
			return new Bridge();
		}

		private Bridge()
		{
			Mod = null;
			Version = "none";
		}

		/// Whether enRAGE is behind this bridge (false for Absent()).
		public bool Present
		{
			get { return Mod != null; }
		}

		private Bridge(object mod)
		{
			Mod = mod;
			Type type = mod.GetType();
			_clock = Typed.ClockOf(mod);
			Version = type.Assembly.GetName().Version.ToString();
			_recordAt = type.GetMethod("RecordAt", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, new Type[1] { typeof(int) }, null);
			_roster = type.GetProperty("Roster", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			_chronicle = type.GetProperty("Chronicle", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			_acqOf = type.GetField("_acqOf", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			_townAt = type.GetMethod("TownAt", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, new Type[1] { typeof(Vector3) }, null);
			_zoneAt = type.GetMethod("GetZoneName", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, null, new Type[1] { typeof(Vector3) }, null);
			Type type2 = type.Assembly.GetType("NemesisSystem.Gangs", throwOnError: false);
			_gangById = ((type2 != null) ? type2.GetMethod("ById", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static, null, new Type[1] { typeof(string) }, null) : null);
			List<string> list = new List<string>();
			if (_recordAt == null)
			{
				list.Add("RecordAt (who a person is)");
			}
			if (_roster == null)
			{
				list.Add("Roster (everyone enRAGE knows)");
			}
			if (_chronicle == null)
			{
				list.Add("Chronicle (world news)");
			}
			if (_acqOf == null)
			{
				list.Add("_acqOf (townsfolk)");
			}
			if (_townAt == null && _zoneAt == null)
			{
				list.Add("TownAt (town names)");
			}
			if (_gangById == null)
			{
				list.Add("Gangs.ById (outfit names)");
			}
			Log.Write("enRAGE " + Version + " found." + ((list.Count == 0) ? " Everything the add-on reads is there." : (" Not found, those features are off: " + string.Join(", ", list))));
			ResolveWrites(type);
		}

		public static Bridge Find()
		{
			Type type = null;
			Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
			for (int i = 0; i < assemblies.Length; i++)
			{
				Type type2 = assemblies[i].GetType("RDR2DN.ScriptDomain", throwOnError: false);
				if (type2 != null)
				{
					type = type2;
					break;
				}
			}
			if (type == null)
			{
				return null;
			}
			PropertyInfo property = type.GetProperty("CurrentDomain", BindingFlags.Public | BindingFlags.Static);
			object obj = ((property != null) ? property.GetValue(null, null) : null);
			PropertyInfo property2 = type.GetProperty("RunningScripts");
			IEnumerable enumerable = ((obj != null && property2 != null) ? (property2.GetValue(obj, null) as IEnumerable) : null);
			if (enumerable == null)
			{
				return null;
			}
			foreach (object item in enumerable)
			{
				if (item != null)
				{
					PropertyInfo property3 = item.GetType().GetProperty("ScriptInstance");
					object obj2 = ((property3 != null) ? property3.GetValue(item, null) : null);
					if (obj2 != null && obj2.GetType().FullName == "NemesisSystem.NemesisMod")
					{
						return new Bridge(obj2);
					}
				}
			}
			return null;
		}

		private void Fail(string what, Exception ex)
		{
			if (_failed.Add(what))
			{
				Log.Write("enRAGE " + what + " failed: " + (ex.InnerException ?? ex).Message + " (won't repeat this message)");
			}
		}

		/// The game minute: enRAGE's clock while its delegate is set (so both count alike), else our own GameDate (standing alone,
		/// or before enRAGE has set it; the arithmetic is the same, so the saved minutes agree either way).
		public static long Now()
		{
			Func<long> clock = _clock;
			if (clock != null)
			{
				try
				{
					long n = clock();
					if (n > 0)
						return n;
				}
				catch
				{
				}
			}
			return GameDate.MinutesNow();
		}

		/// True while enRAGE's own clock answers (Ready waits for it with enRAGE present, as before).
		public bool ClockRuns()
		{
			if (Mod == null)
				return false;
			if (_clock == null && RDR2.Game.FrameCount - _clockLookedAt > 300)
			{
				_clockLookedAt = RDR2.Game.FrameCount;
				_clock = Typed.ClockOf(Mod);
			}
			Func<long> clock = _clock;
			if (clock == null)
				return false;
			try
			{
				return clock() > 0;
			}
			catch
			{
				return false;
			}
		}

		internal RecordRef Wrap(object raw)
		{
			if (raw == null)
				return null;
			RecordRef r;
			if (!_recs.TryGetValue(raw, out r))
			{
				r = new RecordRef(raw);
				_recs.Add(raw, r);
			}
			return r;
		}

		internal FolkRef WrapFolk(object raw)
		{
			if (raw == null)
				return null;
			FolkRef f;
			if (!_folks.TryGetValue(raw, out f))
			{
				f = new FolkRef(raw);
				_folks.Add(raw, f);
			}
			return f;
		}

		private static bool IsRecordType(Type t)
		{
			return t != null && t.FullName == "NemesisSystem.NemesisRecord";
		}

		public RecordRef RecordAt(int handle)
		{
			if (_recordAt == null)
			{
				return null;
			}
			try
			{
				return Wrap(_recordAt.Invoke(Mod, new object[1] { handle }));
			}
			catch (Exception ex)
			{
				Fail("RecordAt", ex);
				return null;
			}
		}

		/// enRAGE's roster list itself (its List<NemesisRecord>, held as a list of objects).
		internal IList RosterRaw()
		{
			if (_roster == null)
			{
				return null;
			}
			try
			{
				return _roster.GetValue(Mod, null) as IList;
			}
			catch (Exception ex)
			{
				Fail("Roster", ex);
				return null;
			}
		}

		/// Everyone on enRAGE's roster, wrapped (a new list each call; the refs are the same objects each time).
		public List<RecordRef> Roster()
		{
			IList raw = RosterRaw();
			if (raw == null)
			{
				return null;
			}
			try
			{
				List<RecordRef> list = new List<RecordRef>(raw.Count);
				foreach (object o in raw)
				{
					list.Add(Wrap(o));
				}
				return list;
			}
			catch (Exception ex)
			{
				Fail("Roster", ex);
				return null;
			}
		}

		/// enRAGE's chronicle's events list itself (its List<WorldEvent>), or null.
		private object EventsRaw()
		{
			if (_chronicle == null)
			{
				return null;
			}
			try
			{
				object value = _chronicle.GetValue(Mod, null);
				PropertyInfo propertyInfo = value?.GetType().GetProperty("Events");
				return (propertyInfo != null) ? propertyInfo.GetValue(value, null) : null;
			}
			catch (Exception ex)
			{
				Fail("Chronicle", ex);
				return null;
			}
		}

		/// enRAGE's events within `withinMinutes` of now among the last `lastN` (0: all), oldest first, as our own LifeEvents;
		/// null when the chronicle wasn't found.
		public List<LifeEvent> RecentEvents(long now, int withinMinutes, int lastN)
		{
			object raw = EventsRaw();
			if (raw == null)
			{
				return null;
			}
			try
			{
				return Typed.RecentEvents(raw, now, withinMinutes, lastN);
			}
			catch (Exception ex)
			{
				Fail("Chronicle", ex);
				return null;
			}
		}

		/// enRAGE's townsperson in this body. The test cast are nobody's here (evenCast for F7): enRAGE knows its townsfolk
		/// by their look, and took the cast for people it had met who looked the same (Lex's test, 2026-10-28 00:08: the
		/// preacher became an awake townsman, who walked off on a waking act of his).
		public FolkRef AcquaintanceAt(int handle, bool evenCast = false)
		{
			if (_acqOf == null || !evenCast && Spawner.IsCast(handle))
			{
				return null;
			}
			try
			{
				return (_acqOf.GetValue(Mod) is IDictionary dictionary && dictionary.Contains(handle)) ? WrapFolk(dictionary[handle]) : null;
			}
			catch (Exception ex)
			{
				Fail("townsfolk", ex);
				return null;
			}
		}

		/// The town at pos, else the district or region's name ("the Heartlands"), else null in the wilds: enRAGE's own
		/// lookups where they were found, ours (Places.cs) where not.
		public string TownAt(Vector3 pos)
		{
			try
			{
				if (_townAt == null && _zoneAt == null)
				{
					string ours = Places.TownAt(pos) ?? Places.ZoneName(pos);
					return ours == "the wilds" ? null : ours;
				}
				string text = ((_townAt != null) ? (_townAt.Invoke(Mod, new object[1] { pos }) as string) : Places.TownAt(pos));
				if (text == null)
				{
					text = _zoneAt != null ? _zoneAt.Invoke(Mod, new object[1] { pos }) as string : Places.ZoneName(pos);
					if (text == "the wilds")
					{
						text = null;
					}
				}
				return text;
			}
			catch (Exception ex)
			{
				Fail("TownAt", ex);
				return null;
			}
		}

		public string GangName(string id)
		{
			if (string.IsNullOrEmpty(id) || _gangById == null)
			{
				return null;
			}
			if (_gangNames.TryGetValue(id, out var value))
			{
				return value;
			}
			try
			{
				object obj = _gangById.Invoke(null, new object[1] { id });
				FieldInfo fieldInfo = obj?.GetType().GetField("Name", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
				value = ((fieldInfo != null) ? (fieldInfo.GetValue(obj) as string) : null);
			}
			catch (Exception ex)
			{
				Fail("Gangs.ById", ex);
			}
			if (value != null && !value.StartsWith("the ", StringComparison.OrdinalIgnoreCase) && !value.StartsWith("The "))
			{
				value = "the " + value;
			}
			_gangNames[id] = value;
			return value;
		}

		private void ResolveWrites(Type t)
		{
			MethodInfo[] methods = t.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			foreach (MethodInfo methodInfo in methods)
			{
				ParameterInfo[] parameters = methodInfo.GetParameters();
				switch (methodInfo.Name)
				{
				case "AddThought":
					if (parameters.Length == 7 && IsRecordType(parameters[0].ParameterType) && parameters[5].ParameterType == typeof(long))
					{
						_addThought = methodInfo;
					}
					break;
				case "Remember":
					if (parameters.Length == 5 && IsRecordType(parameters[0].ParameterType) && parameters[2].ParameterType == typeof(int))
					{
						_remember = methodInfo;
					}
					break;
				case "History":
					if (parameters.Length == 3 && IsRecordType(parameters[0].ParameterType) && parameters[1].ParameterType == typeof(string))
					{
						_history = methodInfo;
					}
					break;
				case "SaveRoster":
					if (parameters.Length == 0)
					{
						_saveRoster = methodInfo;
					}
					break;
				case "SaveWorld":
					if (parameters.Length == 0)
					{
						_saveWorld = methodInfo;
					}
					break;
				case "FactionJob":
					if (parameters.Length == 2 && parameters[1].ParameterType == typeof(bool))
					{
						_factionJob = methodInfo;
					}
					break;
				case "IntelNotice":
					if (parameters.Length == 1)
					{
						_intelNotice = methodInfo;
					}
					break;
				case "AgendaText":
					if (parameters.Length == 2)
					{
						_agendaText = methodInfo;
					}
					break;
				case "WhenText":
					if (parameters.Length == 1 && parameters[0].ParameterType == typeof(long))
					{
						_whenText = methodInfo;
					}
					break;
				}
			}
			_people = t.GetProperty("People", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			_peopleDirty = t.GetField("_peopleDirty", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			CanWrite = _addThought != null && _remember != null && _saveRoster != null;
			CanTellPlans = _factionJob != null && _intelNotice != null && _saveWorld != null && _gangById != null;
			List<string> list = new List<string>();
			if (_addThought == null)
			{
				list.Add("AddThought");
			}
			if (_remember == null)
			{
				list.Add("Remember");
			}
			if (_history == null)
			{
				list.Add("History");
			}
			if (_saveRoster == null)
			{
				list.Add("SaveRoster");
			}
			if (!CanTellPlans)
			{
				list.Add("gang plans (FactionJob/IntelNotice)");
			}
			if (_people == null)
			{
				list.Add("People (townsfolk)");
			}
			Log.Write((list.Count == 0) ? "enRAGE's own record-keeping found: romance can change moods, memories, grudges and tell gang plans." : ("Not found in enRAGE, those romance effects are off: " + string.Join(", ", list)));
		}

		private bool Call(MethodInfo m, string what, params object[] args)
		{
			if (m == null)
			{
				return false;
			}
			try
			{
				m.Invoke(Mod, args);
				return true;
			}
			catch (Exception ex)
			{
				Fail(what, ex);
				return false;
			}
		}

		public bool AddThought(RecordRef r, string mood, string text, int weight, int hours)
		{
			return Call(_addThought, "AddThought", r?.Raw, mood, text, weight, hours, Now(), null);
		}

		public bool Remember(RecordRef r, string emotion, int intensity, string text, string cause)
		{
			return Call(_remember, "Remember", r?.Raw, emotion, intensity, text, cause);
		}

		public bool History(RecordRef r, string text)
		{
			return Call(_history, "History", r?.Raw, text, null);
		}

		public void RosterChanged()
		{
			Call(_saveRoster, "SaveRoster");
		}

		public string[] Intel(RecordRef r, out object plan)
		{
			plan = null;
			if (!CanTellPlans || string.IsNullOrEmpty(r.Gang))
			{
				return null;
			}
			try
			{
				object obj = _gangById.Invoke(null, new object[1] { r.Gang });
				if (obj == null)
				{
					return null;
				}
				string text = ((_agendaText != null) ? (_agendaText.Invoke(Mod, new object[2] { obj, null }) as string) : null);
				object obj2 = _factionJob.Invoke(Mod, new object[2] { obj, true });
				string text2 = null;
				if (obj2 != null && !Get<bool>(obj2, "Known") && !Get<bool>(obj2, "Done"))
				{
					string text3 = Get<string>(obj2, "Title");
					long num = Get<long>(obj2, "DueAt");
					string text4 = ((_whenText != null) ? (_whenText.Invoke(Mod, new object[1] { num }) as string) : null);
					if (!string.IsNullOrEmpty(text3))
					{
						text2 = text3 + (string.IsNullOrEmpty(text4) ? "" : (" on " + text4));
						plan = obj2;
					}
				}
				if (text == null && text2 == null)
				{
					return null;
				}
				return new string[2] { text, text2 };
			}
			catch (Exception ex)
			{
				Fail("gang plans", ex);
				return null;
			}
		}

		public void RevealPlan(object plan, RecordRef teller)
		{
			if (plan == null || Get<bool>(plan, "Known"))
			{
				return;
			}
			try
			{
				Set(plan, "Known", true);
				Set(plan, "ToldBy", teller?.Id);
				Set(plan, "ToldAbout", "a sweetheart's whisper");
				Call(_saveWorld, "SaveWorld");
				Call(_intelNotice, "IntelNotice", plan);
			}
			catch (Exception ex)
			{
				Fail("RevealPlan", ex);
			}
		}

		/// One of enRAGE's townsfolk (by our key, "folk:" + their id or name) has died, as enRAGE has it. Read only.
		public bool FolkDead(string folkKey)
		{
			if (_people == null || folkKey == null || !folkKey.StartsWith("folk:"))
			{
				return false;
			}
			try
			{
				string id = folkKey.Substring(5);
				object acquaintance = Typed.FolkById(_people.GetValue(Mod, null), id);
				return acquaintance != null && Typed.AcqDead(acquaintance);
			}
			catch
			{
				return false;
			}
		}

		/// One of the direct writes (CLAUDE.md, "Writing to enRAGE"): a townsperson's death enRAGE didn't see, or, after a
		/// reload undid it, taking that back. Found the way FolkDead finds them; enRAGE saves it with its people.
		public bool MarkFolkDead(string folkKey, bool dead)
		{
			if (_people == null || folkKey == null || !folkKey.StartsWith("folk:"))
			{
				return false;
			}
			try
			{
				string id = folkKey.Substring(5);
				object acquaintance = Typed.FolkById(_people.GetValue(Mod, null), id);
				if (acquaintance == null || Typed.AcqDead(acquaintance) == dead)
				{
					return false;
				}
				Typed.AcqSetDead(acquaintance, dead);
				if (_peopleDirty != null)
				{
					_peopleDirty.SetValue(Mod, true);
				}
				return true;
			}
			catch (Exception ex)
			{
				Fail("townsfolk deaths", ex);
				return false;
			}
		}

		private MethodInfo _gunButt;
		private bool _gunButtLooked;

		/// enRAGE's own test for someone it knocked out with a gun butt (its GunMelee), read only, so the ledger never
		/// buries them. False if it can't be found.
		public bool KnockedOutByGunButt(RDR2.Ped p)
		{
			if (!_gunButtLooked)
			{
				_gunButtLooked = true;
				if (Mod == null)
					return false;
				Type type = Mod.GetType().Assembly.GetType("NemesisSystem.GunMelee");
				_gunButt = ((type != null) ? type.GetMethod("KnockedOutByGunButt", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) : null);
				if (_gunButt == null)
				{
					Log.Write("enRAGE's gun-butt check wasn't found: the people ledger tells the knocked-out from the dead without it.");
				}
			}
			try
			{
				return _gunButt != null && p != null && (bool)_gunButt.Invoke(null, new object[1] { p });
			}
			catch (Exception ex)
			{
				Fail("gun-butt check", ex);
				return false;
			}
		}

		public void LikeMore(string acquaintanceId, int delta)
		{
			if (_people == null || acquaintanceId == null)
			{
				return;
			}
			try
			{
				object acquaintance = Typed.FolkByExactId(_people.GetValue(Mod, null), acquaintanceId);
				if (acquaintance != null)
				{
					Typed.AcqSetLike(acquaintance, Math.Max(0, Math.Min(100, Typed.AcqLike(acquaintance) + delta)));
					if (_peopleDirty != null)
					{
						_peopleDirty.SetValue(Mod, true);
					}
				}
			}
			catch (Exception ex)
			{
				Fail("townsfolk liking", ex);
			}
		}

		private static T Get<T>(object o, string name)
		{
			PropertyInfo property = o.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			object obj = ((property != null) ? property.GetValue(o, null) : null);
			if (!(obj is T))
			{
				return default(T);
			}
			return (T)obj;
		}

		/// One of the few direct writes into enRAGE (CLAUDE.md, "Writing to enRAGE"). A property an enRAGE update renamed
		/// or made read-only is logged once, not skipped in silence.
		private void Set(object o, string name, object value)
		{
			PropertyInfo property = o.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			if (property != null && property.CanWrite)
			{
				property.SetValue(o, value, null);
				return;
			}
			Fail("setting " + o.GetType().Name + "." + name, new MissingMemberException(o.GetType().Name, name));
		}
	}
}
