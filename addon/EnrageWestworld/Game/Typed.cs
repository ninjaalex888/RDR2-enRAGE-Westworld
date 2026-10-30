// The one file (with Snapshot.cs) whose methods name enRAGE's types (2026-10-29, roadmap 34 phase A: standing alone).
// Everything else in the add-on holds enRAGE's objects as `object` (RecordRef, FolkRef in Refs.cs) and reads them
// through these, which the runtime compiles only when they are first called: with NemesisMod.dll absent nothing here is
// ever called, and the add-on loads and runs without it. Every method is NoInlining, so a caller's compilation never
// pulls a typed body into itself. A member an enRAGE update renames fails here, once, and the bridge's Fail() logs it.
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using NemesisSystem;

namespace EnrageWestworld.InGame
{
    internal static class Typed
    {
        // ---- records ------------------------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.NoInlining)] public static string RecId(object r) { return ((NemesisRecord)r).Id; }
        [MethodImpl(MethodImplOptions.NoInlining)] public static string RecName(object r) { return ((NemesisRecord)r).Name; }
        [MethodImpl(MethodImplOptions.NoInlining)] public static string RecTitle(object r) { return ((NemesisRecord)r).Title; }
        [MethodImpl(MethodImplOptions.NoInlining)] public static string RecKinId(object r) { return ((NemesisRecord)r).KinId; }
        [MethodImpl(MethodImplOptions.NoInlining)] public static string RecGang(object r) { return ((NemesisRecord)r).Gang; }
        [MethodImpl(MethodImplOptions.NoInlining)] public static uint RecModelHash(object r) { return ((NemesisRecord)r).ModelHash; }
        [MethodImpl(MethodImplOptions.NoInlining)] public static bool RecIsAlive(object r) { return ((NemesisRecord)r).IsAlive; }
        [MethodImpl(MethodImplOptions.NoInlining)] public static bool RecPresumedDead(object r) { return ((NemesisRecord)r).PresumedDead; }
        [MethodImpl(MethodImplOptions.NoInlining)] public static long RecPresumedDeadAt(object r) { return ((NemesisRecord)r).PresumedDeadAt; }
        [MethodImpl(MethodImplOptions.NoInlining)] public static int RecHate(object r) { return ((NemesisRecord)r).Hate; }
        [MethodImpl(MethodImplOptions.NoInlining)] public static void RecSetHate(object r, int v) { ((NemesisRecord)r).Hate = v; }
        [MethodImpl(MethodImplOptions.NoInlining)] public static int RecFear(object r) { return ((NemesisRecord)r).Fear; }
        [MethodImpl(MethodImplOptions.NoInlining)] public static void RecAddHate(object r, int v) { ((NemesisRecord)r).AddHate(v); }
        [MethodImpl(MethodImplOptions.NoInlining)] public static Dictionary<string, int> RecFacets(object r) { return ((NemesisRecord)r).Facets; }

        /// Revive's writes on the record (Bridge.World.cs: alive, presumed dead and when, no longer killed by the player,
        /// no fate, the rest, the hate, the fear, touched).
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void ReviveApply(object raw, long now, int hate, int fear, int restMinutes)
        {
            var r = (NemesisRecord)raw;
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
        }

        /// BookDeath's write: dead on enRAGE's books, and not presumed (as its own death writes do).
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void BookDeathApply(object raw)
        {
            var r = (NemesisRecord)raw;
            r.IsAlive = false;
            r.PresumedDead = false;
        }

        // ---- townsfolk ----------------------------------------------------------------------------------
        [MethodImpl(MethodImplOptions.NoInlining)] public static string AcqId(object a) { return ((Acquaintance)a).Id; }
        [MethodImpl(MethodImplOptions.NoInlining)] public static string AcqName(object a) { return ((Acquaintance)a).Name; }
        [MethodImpl(MethodImplOptions.NoInlining)] public static string AcqFullName(object a) { return ((Acquaintance)a).FullName; }
        [MethodImpl(MethodImplOptions.NoInlining)] public static void AcqSetName(object a, string name) { ((Acquaintance)a).Name = name; }   // its FullName is the title and this
        [MethodImpl(MethodImplOptions.NoInlining)] public static string AcqTown(object a) { return ((Acquaintance)a).Town; }
        [MethodImpl(MethodImplOptions.NoInlining)] public static string AcqTemper(object a) { return ((Acquaintance)a).Temper; }
        [MethodImpl(MethodImplOptions.NoInlining)] public static uint AcqModel(object a) { return ((Acquaintance)a).Model; }
        [MethodImpl(MethodImplOptions.NoInlining)] public static bool AcqMet(object a) { return ((Acquaintance)a).Met; }
        [MethodImpl(MethodImplOptions.NoInlining)] public static bool AcqDead(object a) { return ((Acquaintance)a).Dead; }
        [MethodImpl(MethodImplOptions.NoInlining)] public static void AcqSetDead(object a, bool v) { ((Acquaintance)a).Dead = v; }
        [MethodImpl(MethodImplOptions.NoInlining)] public static int AcqLike(object a) { return ((Acquaintance)a).Like; }
        [MethodImpl(MethodImplOptions.NoInlining)] public static void AcqSetLike(object a, int v) { ((Acquaintance)a).Like = v; }

        /// "Forget you" (Lex, 2026-10-30): every townsperson forgets what they know and feel about the player: knowing him, their
        /// fear, fondness and grudge, the favours, what they saw of him and whether they told the law. How many knew him.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static int AcqForgetYouAll(object people)
        {
            int knew = 0;
            IEnumerable<Acquaintance> all = people as IEnumerable<Acquaintance>;
            if (all == null)
                return -1;
            foreach (Acquaintance a in all)
            {
                if (a == null)
                    continue;
                if (a.Met || a.Fear != 0 || a.Like != 0 || a.Grudge != 0 || a.Favours != 0 || (a.Witnessed != null && a.Witnessed.Count > 0))
                    knew++;
                a.Met = false;
                a.Fear = 0;
                a.Like = 0;
                a.Grudge = 0;
                a.GrudgeSettled = false;
                a.SawFace = null;
                a.TimesSeen = 0;
                a.Favours = 0;
                a.FavourNotes?.Clear();
                a.Witnessed?.RemoveAll(w => w != null && w.IndexOf("you", StringComparison.OrdinalIgnoreCase) >= 0);   // any line of theirs about the player
                a.LastDealt = default(DateTime);
                a.Reported = false;
                a.KeptQuiet = false;
                a.LastScaredMin = 0;
                a.LastGreetDay = 0;
                a.LikeSpread = false;
            }
            return knew;
        }
        [MethodImpl(MethodImplOptions.NoInlining)] public static int AcqGrudge(object a) { return ((Acquaintance)a).Grudge; }
        [MethodImpl(MethodImplOptions.NoInlining)] public static int AcqFear(object a) { return ((Acquaintance)a).Fear; }
        [MethodImpl(MethodImplOptions.NoInlining)] public static List<string> AcqWitnessed(object a) { return ((Acquaintance)a).Witnessed; }
        [MethodImpl(MethodImplOptions.NoInlining)] public static List<string> AcqFavourNotes(object a) { return ((Acquaintance)a).FavourNotes; }

        [MethodImpl(MethodImplOptions.NoInlining)] public static string RecKinRelation(object r) { return ((NemesisRecord)r).KinRelation; }
        [MethodImpl(MethodImplOptions.NoInlining)] public static bool RecKilledByPlayer(object r) { return ((NemesisRecord)r).KilledByPlayer; }

        /// Marry's writes (WestworldScript.Marry): Family married, a "Married" past entry, the other as a friend when asked.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void MarryApply(object raw, string otherName, string friendId, int year, int month)
        {
            var r = (NemesisRecord)raw;
            r.Family = "married";
            if (r.Past == null)
                r.Past = new List<PastEntry>();
            r.Past.Add(new PastEntry { Year = year, Month = month, Text = "Married " + otherName, Kind = "family", Public = true, Known = true });
            if (friendId != null && r.FriendIds != null && !r.FriendIds.Contains(friendId) && r.FriendIds.Count < 3)
                r.FriendIds.Add(friendId);
        }

        /// The avenger's filling in (WestworldScript.Life.cs, Avenge): a fresh record from NewRecord made the dead one's kin,
        /// come for the player, with his goal set through enRAGE's own Mind.SetGoal.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void AvengerFill(object raw, string name, string kinId, string relation, int hate, float x, float y, float z, long restUntil, string goalText, long now)
        {
            var r = (NemesisRecord)raw;
            r.Name = name;
            r.Title = string.Empty;
            r.KinId = kinId;
            r.KinRelation = relation;
            r.Hate = hate;
            r.Origin = GrudgeOrigin.FriendKilled;
            r.HasOrigin = true;
            r.OriginX = x;
            r.OriginY = y;
            r.OriginZ = z;
            r.RestUntil = restUntil;
            Mind.SetGoal(r, new Goal { Type = "Avenge", Target = "you", Text = goalText, Since = now });
        }

        /// enRAGE's clock delegate (NemesisRecord.GameClock, a Func<long> its mod sets), null when the type isn't loaded:
        /// read by reflection so that this one member's absence never stops the add-on (Bridge.Now falls back to GameDate).
        public static Func<long> ClockOf(object mod)
        {
            try
            {
                Type t = mod.GetType().Assembly.GetType("NemesisSystem.NemesisRecord", false);
                var f = t != null ? t.GetField("GameClock", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic) : null;
                object v = f != null ? f.GetValue(null) : null;
                if (v == null && t != null)
                {
                    var p = t.GetProperty("GameClock", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    v = p != null ? p.GetValue(null, null) : null;
                }
                return v as Func<long>;
            }
            catch
            {
                return null;
            }
        }

        // ---- the chronicle ------------------------------------------------------------------------------
        /// An empty list of enRAGE's WorldPart, the ChronicleDeed call's last argument (Bridge.AddDeed).
        [MethodImpl(MethodImplOptions.NoInlining)] public static object NoParts() { return new WorldPart[0]; }

        /// A LIFE item for the papers (Bridge.AddTownItem), added to enRAGE's own events list.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool AddTownItem(object eventsList, string title, string place, long now, int day, string when, float? x, float? y, float? z)
        {
            var events = eventsList as List<WorldEvent>;
            if (events == null)
                return false;
            var e = new WorldEvent
            {
                Id = "ww_" + Guid.NewGuid().ToString("N").Substring(0, 8),
                Kind = "LIFE",
                GameMin = now,
                Day = day,
                When = when,
                Title = title,
                Place = place,
                Heard = true
            };
            if (x.HasValue)
            {
                e.HasPos = true;
                e.X = x.Value;
                e.Y = y.Value;
                e.Z = z.Value;
            }
            events.Add(e);
            return true;
        }

        /// enRAGE's events within `withinMinutes` of now among the last `lastN` (0: all), oldest first, as our LifeEvents.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static List<Core.LifeEvent> RecentEvents(object eventsList, long now, int withinMinutes, int lastN)
        {
            var result = new List<Core.LifeEvent>();
            var events = eventsList as List<WorldEvent>;
            if (events == null)
                return result;
            int from = lastN > 0 ? Math.Max(0, events.Count - lastN) : 0;
            for (int i = from; i < events.Count; i++)
            {
                WorldEvent e = events[i];
                if (e == null || now - e.GameMin > withinMinutes)
                    continue;
                var le = Snapshot.Of(e);
                le.IsPast = e.IsPast;
                result.Add(le);
            }
            return result;
        }

        // ---- townsfolk by id ----------------------------------------------------------------------------
        /// The townsperson with this id (or full name, as enRAGE keys them) in enRAGE's people list, raw.
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static object FolkById(object peopleList, string id)
        {
            var people = peopleList as List<Acquaintance>;
            if (people == null || id == null)
                return null;
            foreach (Acquaintance a in people)
                if (a != null && (a.Id ?? a.FullName) == id)
                    return a;
            return null;
        }

        /// The townsperson with exactly this Id, raw (LikeMore's lookup).
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static object FolkByExactId(object peopleList, string id)
        {
            var people = peopleList as List<Acquaintance>;
            if (people == null || id == null)
                return null;
            foreach (Acquaintance a in people)
                if (a != null && a.Id == id)
                    return a;
            return null;
        }

        /// Whether this raw object is one of enRAGE's townsfolk (for the people list, whose elements are read as objects).
        [MethodImpl(MethodImplOptions.NoInlining)] public static bool IsAcq(object a) { return a is Acquaintance; }
        [MethodImpl(MethodImplOptions.NoInlining)] public static bool IsRec(object r) { return r is NemesisRecord; }
    }
}
