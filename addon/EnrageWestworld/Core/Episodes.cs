// Memory v1 (the mind design's memory layer, daytime run 2026-10-27, item 11; Core only, nothing in the game calls it
// yet): episodes, what someone saw, heard or was told of a recorded happening (WorldEvents), that fade by how much they
// mattered and get retold between people who know each other, losing sureness and details on the way. In memory only,
// capped, never saved (a mind's live state; cleared with the rest on a rewind). Plain C#, no game.
//
// What's grounded where: forgetting by an exponential curve whose half-life grows with how strongly it was felt (the
// emotional enhancement of memory: arousing events are kept longer); retelling as serial reproduction (Bartlett 1932;
// Allport and Postman 1947: told on, a story gets shorter and loses names and particulars, keeps its gist, and is
// believed a little less at each remove).
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    /// One person's memory of one happening.
    public sealed class Episode
    {
        public string Key;            // the happening's: kind|doer|target|at (the same thing, however it reached them)
        public string Kind;           // the happening's kind
        public string Doer, Target;   // as this person knows them: a name, or a description ("a stranger") once lost in the telling
        public string DoerId, TargetId;       // as they know them (null once lost)
        public string WhoDid, WhoTo;          // the happening's own ids, never lost: nobody is told of their own doing
        public string Town, Place;    // the place may drift in the telling (null: the street)
        public long At;               // when it happened (game minute)
        public long Learned;          // when they came to know it
        public string How;            // saw, heard, told
        public string From;           // who told them (a first name), for "told"
        public int Removes;           // how many tellings away from someone who saw it (0: saw or heard it themselves)
        public float Felt;            // how strongly it was felt when it came to them (0..1)
        public float Sure;            // how sure they are it happened as they have it (0..1)
        public List<string> ToldTo = new List<string>();   // ids they've told it to (never twice to the same person)
    }

    public static class Episodes
    {
        public const int PerPerson = 12, People = 600;
        public const float Forgotten = 0.05f;

        /// Half-life in game minutes by how strongly it was felt: a mild thing about half a day, a strong one about a week
        /// and a half; their own part doubles it.
        public static double HalfLife(Episode e, bool ownPart)
        {
            double h = 720 * Math.Pow(20.0, Math.Max(0f, Math.Min(1f, e.Felt)));   // 0 -> 12 h, 0.5 -> 2.2 days, 1 -> 10 days
            return ownPart ? h * 2 : h;
        }

        /// How much of it is left now (0..1): how strongly it came to them, faded since they learned it, by how sure.
        public static float Strength(Episode e, long now, string selfId = null)
        {
            if (e == null || now < e.Learned)
                return 0f;
            bool own = selfId != null && (e.DoerId == selfId || e.TargetId == selfId);
            double age = now - e.Learned;
            double left = (0.35 + 0.65 * e.Felt) * Math.Pow(0.5, age / HalfLife(e, own));
            float s = (float)(left * (0.5 + 0.5 * e.Sure));
            // what they saw and felt hard isn't lost the way the rest is: forgetting slows in the tail (consolidation), so
            // it stays just above forgetting, a memory that can still come back
            return e.How == "saw" && e.Felt >= 0.8f ? Math.Max(s, Forgotten + 0.01f) : s;
        }

        /// The episode for someone who saw or heard a happening (how: saw | heard), with how strongly they felt it.
        public static Episode Of(Happening h, string how, float felt)
        {
            if (h == null || h.Kind == null)
                return null;
            bool saw = how == "saw";
            if (float.IsNaN(felt))
                felt = 0f;
            // only heard (a shout, a shot): who it was isn't known, either of them
            return new Episode
            {
                Key = KeyOf(h), Kind = h.Kind, Doer = saw ? h.DoerName : null, Target = saw ? h.TargetName : null, DoerId = saw ? h.DoerId : null, TargetId = saw ? h.TargetId : null,
                WhoDid = h.DoerId, WhoTo = h.TargetId, Town = h.Town, Place = h.Place, At = h.At, Learned = h.At, How = saw ? "saw" : "heard", Removes = 0,
                Felt = Math.Max(0f, Math.Min(1f, felt)), Sure = saw ? 1f : 0.7f
            };
        }

        public static string KeyOf(Happening h)
        {
            return h.Kind + "|" + h.DoerId + "|" + h.TargetId + "|" + h.At;
        }

        /// What the listener comes away with when the teller tells it (serial reproduction: levelling and sharpening): one
        /// remove further, a little less sure, felt as much as it matters to them (listenerFelt, from their own appraisal,
        /// when the caller has it; else 0.6 of the teller's), and by the seed a name lost (to "somebody", or to "a stranger":
        /// the gist bends toward the outsider), the place moved, or a quarrel told as blows.
        public static Episode Retold(Episode e, string tellerFirstName, long now, string seed, float listenerFelt = -1f)
        {
            int roll = Voice.Hash(seed + "|rt") % 100;
            var r = new Episode
            {
                Key = e.Key, Kind = e.Kind, Doer = e.Doer, Target = e.Target, DoerId = e.DoerId, TargetId = e.TargetId, WhoDid = e.WhoDid, WhoTo = e.WhoTo, Town = e.Town,
                Place = e.Place, At = e.At, Learned = now, How = "told", From = tellerFirstName, Removes = e.Removes + 1,
                Felt = listenerFelt >= 0f && !float.IsNaN(listenerFelt) ? Math.Min(1f, listenerFelt) : e.Felt * 0.6f, Sure = e.Sure * 0.8f
            };
            if (r.Doer != null && roll < 25 + 10 * e.Removes)
            {
                r.Doer = Voice.Hash(seed + "|rs") % 2 == 0 ? "a stranger" : null;   // "somebody", or the outsider, by the second or third telling
                r.DoerId = null;
            }
            if (r.Place != null && Voice.Hash(seed + "|rp") % 100 < 12)
                r.Place = null;   // "in the street", near enough
            if ((r.Kind == "scene:argue" || r.Kind == "scene:snap" || r.Kind == "scene:quarrel") && Voice.Hash(seed + "|rk") % 100 < 8)
                r.Kind = "scene:brawl";   // a quarrel told as a fight
            return r;
        }

        /// Someone's episodes (newest last) and the person who has them; at most PerPerson each (the weakest goes) and
        /// People people (the first in goes); never saved.
        public sealed class Book
        {
            private readonly Dictionary<string, List<Episode>> _of = new Dictionary<string, List<Episode>>();
            private readonly Queue<string> _order = new Queue<string>();
            private readonly HashSet<string> _told = new HashSet<string>();   // teller>listener>key, so nothing is told twice even after it's forgotten and learned again

            public int Count
            {
                get { return _of.Count; }
            }

            public List<Episode> Of(string id)
            {
                List<Episode> l;
                return id != null && _of.TryGetValue(id, out l) ? l : null;
            }

            /// Keeps it unless they know it already (then the surer, stronger version stays). True if it was new to them.
            public bool Add(string id, Episode e, long now)
            {
                if (id == null || e == null)
                    return false;
                List<Episode> l;
                if (!_of.TryGetValue(id, out l))
                {
                    while (_of.Count >= People && _order.Count > 0)
                        _of.Remove(_order.Dequeue());   // the first in goes (an id already forgotten is passed over)
                    _of[id] = l = new List<Episode>(PerPerson);
                    _order.Enqueue(id);
                }
                for (int i = 0; i < l.Count; i++)
                    if (l[i].Key == e.Key)
                    {
                        if (e.Sure > l[i].Sure)
                        {
                            e.ToldTo = l[i].ToldTo;
                            l[i] = e;   // they saw what they'd only been told: the surer stays
                        }
                        return false;
                    }
                if (l.Count >= PerPerson)
                {
                    int weakest = 0;
                    for (int i = 1; i < l.Count; i++)
                        if (Strength(l[i], now, id) < Strength(l[weakest], now, id))
                            weakest = i;
                    if (Strength(e, now, id) <= Strength(l[weakest], now, id))
                        return false;   // weaker than anything they hold: it doesn't stay
                    l.RemoveAt(weakest);
                }
                l.Add(e);
                return true;
            }

            /// Drops what's faded past remembering.
            public int Forget(long now)
            {
                int n = 0;
                List<string> empty = null;
                foreach (var kv in _of)
                {
                    n += kv.Value.RemoveAll(e => Strength(e, now, kv.Key) < Forgotten);
                    if (kv.Value.Count == 0)
                        (empty ?? (empty = new List<string>())).Add(kv.Key);
                }
                if (empty != null)
                {
                    foreach (string id in empty)
                        _of.Remove(id);   // nobody with nothing left takes a place under the cap
                    var keep = new List<string>();
                    foreach (string id in _order)
                        if (_of.ContainsKey(id))
                            keep.Add(id);
                    _order.Clear();
                    foreach (string id in keep)
                        _order.Enqueue(id);
                }
                return n;
            }

            /// What the teller would tell the listener now, if anything: the strongest thing they have that's still worth
            /// telling (strength 0.2 or more, under three days old, not told to this listener before, not something the
            /// listener knows already, and never the listener's own doing told back to them).
            public Episode WorthTelling(string teller, string listener, long now)
            {
                List<Episode> mine = Of(teller), theirs = Of(listener);
                if (mine == null)
                    return null;
                Episode best = null;
                float most = 0.2f;
                foreach (Episode e in mine)
                {
                    if (now - e.At > 3 * 1440 || e.ToldTo.Contains(listener) || _told.Contains(teller + ">" + listener + ">" + e.Key) || e.WhoDid == listener || e.WhoTo == listener)
                        continue;
                    if (theirs != null && theirs.Any(x => x.Key == e.Key))
                        continue;
                    float s = Strength(e, now, teller);
                    if (s > most)
                    {
                        most = s;
                        best = e;
                    }
                }
                return best;
            }

            /// The teller tells it: the listener keeps the retold version. True if it was new to them.
            public bool Tell(string teller, string tellerFirstName, string listener, Episode e, long now)
            {
                if (e == null || e.ToldTo.Contains(listener) || !_told.Add(teller + ">" + listener + ">" + e.Key))
                    return false;
                if (_told.Count > 20000)
                    _told.Clear();   // capped; a week-old telling forgotten is no harm
                e.ToldTo.Add(listener);
                return Add(listener, Retold(e, tellerFirstName, now, teller + "|" + listener + "|" + e.Key), now);
            }

            public void Clear()
            {
                _of.Clear();
                _order.Clear();
                _told.Clear();
            }
        }

        /// In plain words, for the log and (later) the language layer's request: "saw Earl Platt and Hollis Barr come to
        /// blows in the saloon in Valentine, 2 hours ago" / "heard from Abe that somebody and Hollis Barr came to blows ...".
        public static string Describe(Episode e, long now)
        {
            bool saw = e.How == "saw";
            string doer = e.Doer ?? "somebody", target = e.Target ?? "somebody";
            string where = (e.Place != null ? " in the " + e.Place : "") + (e.Town != null ? " in " + e.Town : "");
            long ago = Math.Max(0, now - e.At);
            string when = ago < 90 ? "just now" : ago < 120 ? "an hour ago" : ago < 1440 ? (ago / 60) + " hours ago" : ago < 2880 ? "yesterday" : (ago / 1440) + " days ago";
            string body = WhatOf(e.Kind, !saw).Replace("{d}", doer).Replace("{t}", target) + where;
            return (saw ? "saw " : e.How == "heard" ? "heard that " : "heard from " + (e.From ?? "somebody") + " that ") + body + ", " + when + (e.Sure < 0.5f ? " (not sure of it)" : "");
        }

        private static string WhatOf(string kind, bool past)
        {
            string k = kind != null && kind.StartsWith("scene:", StringComparison.Ordinal) ? kind.Substring(6) : kind ?? "";
            switch (k)
            {
                case "brawl": return past ? "{d} and {t} came to blows" : "{d} and {t} come to blows";
                case "rough": return past ? "{d} shoved {t} about" : "{d} shove {t} about";
                case "argue": case "quarrel": case "snap": return past ? "{d} and {t} quarrelled" : "{d} and {t} quarrel";
                case "pickpocket": return past ? "{d} lifted {t}'s purse" : "{d} lift {t}'s purse";
                case "death": return past ? "{t} died" : "{t} die";
                case "deed:assault": return "{d} beat {t}";
                case "deed:robbery": return past ? "{d} robbed {t}" : "{d} rob {t}";
                case "deed:theft": return past ? "{d} stole from {t}" : "{d} steal from {t}";
                case "help": case "charity": return past ? "{d} helped {t}" : "{d} help {t}";
                case "standoff": return past ? "{d} and {t} drew on each other" : "{d} and {t} draw on each other";
                case "passby": return past ? "{d} walked past {t} when {t} was down" : "{d} walk past {t} when {t} was down";
                case "scorn": case "brushoff": return past ? "{d} gave {t} the cold shoulder" : "{d} give {t} the cold shoulder";
                case "flirt": case "court": return past ? "{d} sweet-talked {t}" : "{d} sweet-talk {t}";
                default:
                    if (k.StartsWith("encounter:", StringComparison.Ordinal))
                        return past ? "something strange happened out there" : "something strange out there";
                    return past ? "there was some business between {d} and {t}" : "some business between {d} and {t}";
            }
        }
    }
}
