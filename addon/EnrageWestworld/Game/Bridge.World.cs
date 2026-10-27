// More of enRAGE, reached by name (0.4): its UI state, notices, chronicle/newspaper, plans (funerals, jobs),
// town centres, camps, and writing people back to life or onto the roster. Everything is looked up lazily
// and each missing piece only switches off the feature that needs it.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NemesisSystem;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    internal sealed class PlanInfo
    {
        public object Raw;
        public string Id, Kind, Title, Place, Dead, Gang;
        public float X, Y, Z, PlotX, PlotY, PlotZ;
        public bool PlotSet, Live, Done, Known, Visited;
        public long DueAt;
        public int WindowMin;

        public Vector3 Pos { get { return PlotSet ? new Vector3(PlotX, PlotY, PlotZ) : new Vector3(X, Y, Z); } }
    }

    internal sealed class CampInfo
    {
        public string Id, Name, Gang, Region;
        public Vector3 Pos;
        public bool Hideout;
    }

    internal sealed partial class Bridge
    {
        private const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;

        private bool _uiResolved;
        private readonly List<MemberInfo> _uiFlags = new List<MemberInfo>();
        private MethodInfo _feedText, _worldNotice, _grudgeNotice, _chronicleDeed, _townCore, _newRecord, _fitKin;
        private bool _moreResolved;
        private PropertyInfo _campsAll;
        private Func<object, string> _campOwner;

        private void ResolveMore()
        {
            if (_moreResolved)
                return;
            _moreResolved = true;
            Type t = Mod.GetType();
            foreach (MethodInfo m in t.GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
            {
                ParameterInfo[] p = m.GetParameters();
                switch (m.Name)
                {
                    case "Feed":
                        if (p.Length == 2 && p[0].ParameterType == typeof(string))
                            _feedText = m;
                        break;
                    case "WorldNotice":
                        if (p.Length == 1)
                            _worldNotice = m;
                        break;
                    case "GrudgeNotice":
                        if (p.Length == 1)
                            _grudgeNotice = m;
                        break;
                    case "ChronicleDeed":
                        if (p.Length == 6 && p[0].ParameterType == typeof(string))
                            _chronicleDeed = m;
                        break;
                    case "TownCore":
                        if (p.Length == 3 && p[0].ParameterType == typeof(string))
                            _townCore = m;
                        break;
                    case "NewRecord":
                        if (p.Length == 4 && p[0].ParameterType == typeof(uint))
                            _newRecord = m;
                        break;
                    case "FitKinToLife":
                        if (p.Length == 1 && p[0].ParameterType == typeof(NemesisRecord))
                            _fitKin = m;
                        break;
                }
            }
            Type camps = t.Assembly.GetType("NemesisSystem.Camps", false);
            _campsAll = camps != null ? camps.GetProperty("AllSites", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) : null;
            Type site = t.Assembly.GetType("NemesisSystem.CampSite", false);
            FieldInfo owner = site != null ? site.GetField("OwnerOf", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) : null;
            if (owner != null)
                _campOwner = s =>
                {
                    Delegate d = owner.GetValue(null) as Delegate;
                    return d != null ? d.DynamicInvoke(s) as string : null;
                };
            List<string> missing = new List<string>();
            if (_feedText == null) missing.Add("Feed");
            if (_worldNotice == null) missing.Add("WorldNotice");
            if (_chronicleDeed == null) missing.Add("ChronicleDeed (newspaper)");
            if (_townCore == null) missing.Add("TownCore");
            if (_newRecord == null) missing.Add("NewRecord (avengers)");
            if (_campsAll == null) missing.Add("Camps");
            Log.Write(missing.Count == 0 ? "enRAGE 0.4 hooks found: notices, newspaper, town centres, new people, camps."
                : "enRAGE 0.4 hooks not found, those features are off: " + string.Join(", ", missing));
        }

        // ---- is enRAGE showing a menu? ------------------------------------------------------------

        public bool UiOpen()
        {
            if (!_uiResolved)
            {
                _uiResolved = true;
                Type t = Mod.GetType();
                foreach (string name in new[] { "_menu", "_iniPanel", "_welcome", "_convo", "_tg", "_interro", "_bulletImpact" })
                {
                    FieldInfo f = t.GetField(name, Any);
                    if (f != null)
                        _uiFlags.Add(f);
                }
                PropertyInfo paper = t.GetProperty("PaperOpen", Any);
                if (paper != null)
                    _uiFlags.Add(paper);
            }
            try
            {
                foreach (MemberInfo m in _uiFlags)
                {
                    FieldInfo f = m as FieldInfo;
                    if (f == null)
                    {
                        object v = ((PropertyInfo)m).GetValue(Mod, null);
                        if (v is bool && (bool)v)
                            return true;
                        continue;
                    }
                    object o = f.GetValue(Mod);
                    if (o == null)
                        continue;
                    switch (f.Name)
                    {
                        case "_convo":
                        case "_tg":
                        case "_interro":
                            return true;
                        case "_bulletImpact":
                            if (Get<bool>(o, "ViewerOpen"))
                                return true;
                            break;
                        default:
                            if (Get<bool>(o, "IsOpen"))
                                return true;
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Fail("UI state", ex);
            }
            return false;
        }

        // ---- notices ---------------------------------------------------------------------------

        /// enRAGE's own notice (its WorldNotice setting decides whether it shows) plus its feed list.
        public void EnrageNotice(string text)
        {
            ResolveMore();
            Call(_worldNotice, "WorldNotice", text);
            Call(_feedText, "Feed", text, null);
        }

        public void GrudgeNotice(string text)
        {
            ResolveMore();
            if (!Call(_grudgeNotice, "GrudgeNotice", text))
                Call(_worldNotice, "WorldNotice", text);
        }

        // ---- chronicle / newspaper ----------------------------------------------------------------

        /// A deed of the player's in enRAGE's chronicle ("good" turns into a newspaper kindness story).
        public bool AddDeed(string tag, string title, string place, Vector3? pos)
        {
            ResolveMore();
            return Call(_chronicleDeed, "ChronicleDeed", tag, title, place, pos, null, new WorldPart[0]);
        }

        /// A small item for enRAGE's newspapers ("Around town"), placed at a town so the local paper prints it.
        public bool AddTownItem(string title, string place, Vector3? pos)
        {
            List<WorldEvent> events = Events();
            if (events == null || string.IsNullOrEmpty(title))
                return false;
            try
            {
                long now = Now();
                DateTime d = GameDate.FromMinutes(now);
                WorldEvent e = new WorldEvent
                {
                    Id = "ww_" + Guid.NewGuid().ToString("N").Substring(0, 8),
                    Kind = "LIFE",
                    GameMin = now,
                    Day = GameDate.DayKey(d),
                    When = d.ToString("MMM d  HH:mm", System.Globalization.CultureInfo.InvariantCulture),
                    Title = title,
                    Place = place,
                    Heard = true
                };
                if (pos.HasValue)
                {
                    e.HasPos = true;
                    e.X = pos.Value.X;
                    e.Y = pos.Value.Y;
                    e.Z = pos.Value.Z;
                }
                events.Add(e);
                Call(_saveWorld, "SaveWorld");
                return true;
            }
            catch (Exception ex)
            {
                Fail("newspaper item", ex);
                return false;
            }
        }

        // ---- places ------------------------------------------------------------------------------

        public bool TownCore(string town, out Vector3 centre, out float radius)
        {
            ResolveMore();
            centre = Vector3.Zero;
            radius = 0f;
            if (_townCore == null || string.IsNullOrEmpty(town))
                return false;
            try
            {
                object[] args = { town, Vector3.Zero, 0f };
                bool ok = (bool)_townCore.Invoke(Mod, args);
                if (ok)
                {
                    centre = (Vector3)args[1];
                    radius = (float)args[2];
                }
                return ok;
            }
            catch (Exception ex)
            {
                Fail("TownCore", ex);
                return false;
            }
        }

        /// The town at pos, or null anywhere outside one. Unlike TownAt, never the region's name ("the Heartlands"):
        /// enRAGE's own town lookup, else the game's town zone (map zone type 1) if enRAGE's can't be reached.
        public string TownOnly(Vector3 pos)
        {
            try
            {
                if (_townAt != null)
                    return _townAt.Invoke(Mod, new object[] { pos }) as string;
            }
            catch (Exception ex)
            {
                Fail("TownAt", ex);
            }
            return ZONE._GET_MAP_ZONE_AT_COORDS(pos.X, pos.Y, pos.Z, 1) != 0 ? "a town" : null;
        }

        public string ZoneName(Vector3 pos)
        {
            if (_zoneAt == null)
                return null;
            try
            {
                return _zoneAt.Invoke(Mod, new object[] { pos }) as string;
            }
            catch (Exception ex)
            {
                Fail("GetZoneName", ex);
                return null;
            }
        }

        public List<CampInfo> Camps()
        {
            ResolveMore();
            List<CampInfo> list = new List<CampInfo>();
            if (_campsAll == null)
                return list;
            try
            {
                IEnumerable all = _campsAll.GetValue(null, null) as IEnumerable;
                if (all == null)
                    return list;
                foreach (object s in all)
                {
                    if (s == null)
                        continue;
                    string owner = null;
                    try
                    {
                        owner = _campOwner != null ? _campOwner(s) : null;
                    }
                    catch
                    {
                    }
                    list.Add(new CampInfo
                    {
                        Id = Field<string>(s, "Id"),
                        Name = Field<string>(s, "Name"),
                        Gang = owner ?? Field<string>(s, "Gang"),
                        Region = Field<string>(s, "Region"),
                        Hideout = Field<bool>(s, "Hideout"),
                        Pos = new Vector3(Field<float>(s, "X"), Field<float>(s, "Y"), Field<float>(s, "Z"))
                    });
                }
            }
            catch (Exception ex)
            {
                Fail("Camps", ex);
            }
            return list;
        }

        // ---- plans: funerals, jobs ------------------------------------------------------------------

        public List<PlanInfo> Plans()
        {
            List<PlanInfo> list = new List<PlanInfo>();
            if (_chronicle == null)
                return list;
            try
            {
                object world = _chronicle.GetValue(Mod, null);
                IEnumerable plans = world != null ? world.GetType().GetProperty("Plans", Any)?.GetValue(world, null) as IEnumerable : null;
                if (plans == null)
                    return list;
                foreach (object p in plans)
                {
                    if (p == null)
                        continue;
                    list.Add(new PlanInfo
                    {
                        Raw = p,
                        Id = Get<string>(p, "Id"),
                        Kind = Get<string>(p, "Kind"),
                        Title = Get<string>(p, "Title"),
                        Place = Get<string>(p, "Place"),
                        Dead = Get<string>(p, "Dead"),
                        Gang = Get<string>(p, "Gang"),
                        X = Get<float>(p, "X"),
                        Y = Get<float>(p, "Y"),
                        Z = Get<float>(p, "Z"),
                        PlotX = Get<float>(p, "PlotX"),
                        PlotY = Get<float>(p, "PlotY"),
                        PlotZ = Get<float>(p, "PlotZ"),
                        PlotSet = Get<bool>(p, "PlotSet"),
                        Live = Get<bool>(p, "Live"),
                        Done = Get<bool>(p, "Done"),
                        Known = Get<bool>(p, "Known"),
                        Visited = Get<bool>(p, "Visited"),
                        DueAt = Get<long>(p, "DueAt"),
                        WindowMin = Get<int>(p, "WindowMin")
                    });
                }
            }
            catch (Exception ex)
            {
                Fail("Plans", ex);
            }
            return list;
        }

        public string WhenText(long at)
        {
            try
            {
                return _whenText != null ? _whenText.Invoke(Mod, new object[] { at }) as string : null;
            }
            catch (Exception ex)
            {
                Fail("WhenText", ex);
                return null;
            }
        }

        /// Marks a gang plan as known (enRAGE then shows it like any other tip).
        public void RevealPlanAs(object plan, NemesisRecord teller, string how)
        {
            if (plan == null || Get<bool>(plan, "Known"))
                return;
            try
            {
                Set(plan, "Known", true);
                Set(plan, "ToldBy", teller?.Id);
                Set(plan, "ToldAbout", how);
                Call(_saveWorld, "SaveWorld");
                Call(_intelNotice, "IntelNotice", plan);
            }
            catch (Exception ex)
            {
                Fail("RevealPlan", ex);
            }
        }

        // ---- people -----------------------------------------------------------------------------

        public bool CanRevive { get { return _remember != null && _saveRoster != null; } }

        /// Brings a dead person back: alive, "presumed dead" so enRAGE lets him return in his own time.
        public bool Revive(NemesisRecord r, string emotion, int intensity, string memory, string history, int hate, int fear, int restMinutes)
        {
            if (r == null || r.IsAlive || !CanRevive)
                return false;
            try
            {
                long now = Now();
                r.IsAlive = true;
                r.PresumedDead = true;
                r.PresumedDeadAt = now;
                r.KilledByPlayer = false;
                r.Fate = null;
                r.RestUntil = Math.Max(r.RestUntil, now + restMinutes);
                r.Hate = Math.Max(0, Math.Min(100, r.Hate + hate));
                if (fear > 0)
                    r.AddFear(fear);
                r.Touch();
                Remember(r, emotion, intensity, memory, "you");
                if (history != null)
                    History(r, history);
                RosterChanged();
                return true;
            }
            catch (Exception ex)
            {
                Fail("Revive", ex);
                return false;
            }
        }

        /// A record died of something RealNPCs did (despair, a street fight gone too far): its history says how, in plain
        /// words, through enRAGE's own History; if enRAGE hadn't marked them dead (the area unloaded first), IsAlive goes
        /// false too. Fate is left to enRAGE (a Fate keeps a record out of its funerals and burials). Then SaveRoster.
        /// True if RealNPCs marked the death itself.
        public bool BookDeath(NemesisRecord r, string history)
        {
            if (r == null || !CanRevive)
                return false;
            try
            {
                bool marked = false;
                if (r.IsAlive)
                {
                    r.IsAlive = false;
                    marked = true;
                }
                if (history != null)
                    History(r, history);
                RosterChanged();
                return marked;
            }
            catch (Exception ex)
            {
                Fail("BookDeath", ex);
                return false;
            }
        }

        public bool CanMakePeople { get { ResolveMore(); return _newRecord != null && _roster != null && _saveRoster != null; } }

        /// A brand-new person through enRAGE's own generator (name, backstory, traits), not yet on the roster.
        public NemesisRecord NewPerson(uint model, string zone)
        {
            ResolveMore();
            if (_newRecord == null)
                return null;
            try
            {
                return _newRecord.Invoke(Mod, new object[] { model, zone, null, 1 }) as NemesisRecord;
            }
            catch (Exception ex)
            {
                Fail("NewRecord", ex);
                return null;
            }
        }

        public bool AddToRoster(NemesisRecord r)
        {
            List<NemesisRecord> roster = Roster();
            if (r == null || roster == null)
                return false;
            try
            {
                if (_fitKin != null && r.KinId != null)
                    Call(_fitKin, "FitKinToLife", r);
                if (!roster.Contains(r))
                    roster.Add(r);
                RosterChanged();
                return true;
            }
            catch (Exception ex)
            {
                Fail("adding to the roster", ex);
                return false;
            }
        }

        private static T Field<T>(object o, string name)
        {
            FieldInfo f = o.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            object v = f != null ? f.GetValue(o) : null;
            return v is T ? (T)v : default(T);
        }
    }
}
