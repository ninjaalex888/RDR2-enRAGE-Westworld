using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NemesisSystem;
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

		private Bridge(object mod)
		{
			Mod = mod;
			Type type = mod.GetType();
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

		public static long Now()
		{
			return NemesisRecord.GameClock?.Invoke() ?? 0;
		}

		public NemesisRecord RecordAt(int handle)
		{
			if (_recordAt == null)
			{
				return null;
			}
			try
			{
				return _recordAt.Invoke(Mod, new object[1] { handle }) as NemesisRecord;
			}
			catch (Exception ex)
			{
				Fail("RecordAt", ex);
				return null;
			}
		}

		public List<NemesisRecord> Roster()
		{
			if (_roster == null)
			{
				return null;
			}
			try
			{
				return _roster.GetValue(Mod, null) as List<NemesisRecord>;
			}
			catch (Exception ex)
			{
				Fail("Roster", ex);
				return null;
			}
		}

		public List<WorldEvent> Events()
		{
			if (_chronicle == null)
			{
				return null;
			}
			try
			{
				object value = _chronicle.GetValue(Mod, null);
				PropertyInfo propertyInfo = value?.GetType().GetProperty("Events");
				return (propertyInfo != null) ? (propertyInfo.GetValue(value, null) as List<WorldEvent>) : null;
			}
			catch (Exception ex)
			{
				Fail("Chronicle", ex);
				return null;
			}
		}

		public Acquaintance AcquaintanceAt(int handle)
		{
			if (_acqOf == null)
			{
				return null;
			}
			try
			{
				return (_acqOf.GetValue(Mod) is IDictionary dictionary && dictionary.Contains(handle)) ? (dictionary[handle] as Acquaintance) : null;
			}
			catch (Exception ex)
			{
				Fail("townsfolk", ex);
				return null;
			}
		}

		public string TownAt(Vector3 pos)
		{
			try
			{
				string text = ((_townAt != null) ? (_townAt.Invoke(Mod, new object[1] { pos }) as string) : null);
				if (text == null && _zoneAt != null)
				{
					text = _zoneAt.Invoke(Mod, new object[1] { pos }) as string;
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
					if (parameters.Length == 7 && parameters[0].ParameterType == typeof(NemesisRecord) && parameters[5].ParameterType == typeof(long))
					{
						_addThought = methodInfo;
					}
					break;
				case "Remember":
					if (parameters.Length == 5 && parameters[0].ParameterType == typeof(NemesisRecord) && parameters[2].ParameterType == typeof(int))
					{
						_remember = methodInfo;
					}
					break;
				case "History":
					if (parameters.Length == 3 && parameters[0].ParameterType == typeof(NemesisRecord) && parameters[1].ParameterType == typeof(string))
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

		public bool AddThought(NemesisRecord r, string mood, string text, int weight, int hours)
		{
			return Call(_addThought, "AddThought", r, mood, text, weight, hours, Now(), null);
		}

		public bool Remember(NemesisRecord r, string emotion, int intensity, string text, string cause)
		{
			return Call(_remember, "Remember", r, emotion, intensity, text, cause);
		}

		public bool History(NemesisRecord r, string text)
		{
			return Call(_history, "History", r, text, null);
		}

		public void RosterChanged()
		{
			Call(_saveRoster, "SaveRoster");
		}

		public string[] Intel(NemesisRecord r, out object plan)
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

		public void RevealPlan(object plan, NemesisRecord teller)
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

		public void LikeMore(string acquaintanceId, int delta)
		{
			if (_people == null || acquaintanceId == null)
			{
				return;
			}
			try
			{
				Acquaintance acquaintance = ((_people.GetValue(Mod, null) is List<Acquaintance> source) ? source.FirstOrDefault((Acquaintance x) => x != null && x.Id == acquaintanceId) : null);
				if (acquaintance != null)
				{
					acquaintance.Like = Math.Max(0, Math.Min(100, acquaintance.Like + delta));
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

		private static void Set(object o, string name, object value)
		{
			PropertyInfo property = o.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
			if (property != null && property.CanWrite)
			{
				property.SetValue(o, value, null);
			}
		}
	}
}
