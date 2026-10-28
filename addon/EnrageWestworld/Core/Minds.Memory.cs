// Memory v1 on the saved memories (the mind design's step 8; night run 2026-10-28, item 4; behind [Mind] MindLive).
// What a MindMemory gains, all worked out when read so the saved record keeps what was first written and a load with
// MindLive off reads the old memory: how sure they are of it (seen, done, or only heard; a hearer below the teller,
// and the teller's own confidence rising with each telling even as the content drifts); retelling, when two people
// part after a talk (Engine.RememberScene hears it): the thing most worth telling passes to the listener as a heard
// memory that names the teller; drift, from age, times told and the rememberer's own id, the way reconstructive memory
// works (a story grows in the telling, the unnamed doer becomes someone the rememberer already dislikes, two men
// become three), never for who did a thing they saw or did themselves, never for the player's deeds (Rumours grow
// those); places (the spot the happening had, back-filled from the record), so standing where it happened pulls it;
// and gist: a memory long past its half-life keeps its kind and feeling and loses strangers' names, never a tie's or
// the dead's. Retold memories count against the twelve and go first. Nothing new is saved but optional fields on the
// record; told things never go through NoteEvent or the saved events log. Plain C#, no game.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static partial class Minds
    {
        public const float HereMetres = 15f;
        public const int ToldDays = 3;          // a thing worth telling is under three days old
        public const int ToldStrength = 15;     // ...and still this strong, worn by time
        public const float ToldSure = 0.8f;     // a hearer is this much as sure as the teller
        public const float TellGain = 0.03f;    // the teller's own confidence, each telling

        // ---- how sure --------------------------------------------------------------------------------

        /// How sure this person is it happened as they have it (0..1): did 1, saw 0.9, heard (a shout, word of a death)
        /// 0.7, told by someone 0.7 x 0.8 a remove; each telling of their own raises it a little (the story firms up as
        /// it's told, whatever happens to its content).
        public static float Sure(MindMemory m)
        {
            if (m == null)
                return 0f;
            float s = m.How == "did" ? 1f : m.How == "saw" ? 0.9f : 0.7f;
            if (m.From != null)
                s *= (float)Math.Pow(ToldSure, Math.Max(1, m.Removes));
            return Math.Min(1f, s + TellGain * m.Told);
        }

        /// When they came to know it: the telling for a told memory, else when it happened.
        public static long LearnedAt(MindMemory m)
        {
            return m.LearnedAt > 0 ? m.LearnedAt : m.At;
        }

        public static bool HasSpot(MindMemory m)
        {
            return m != null && (m.PX != 0f || m.PY != 0f || m.PZ != 0f);
        }

        /// Whether a memory came by telling (not seen or heard with their own ears).
        public static bool WasTold(MindMemory m)
        {
            return m != null && m.From != null;
        }

        // ---- retelling -------------------------------------------------------------------------------

        private static readonly string[] NeverTold = { "talk", "rescued", "flirted", "death" };   // deaths travel by RememberDeathHeard already (the teller's relation isn't the hearer's)

        /// What the teller would tell the listener now, if anything: the strongest thing they have (worn by time) that's
        /// still worth telling: under three days old, not small talk or their own private business, not something done
        /// to the listener or by them, not something the listener knows (the same key), not told to this listener before
        /// (`toldBefore`). Null for nothing.
        public static MindMemory WorthTelling(List<MindMemory> teller, List<MindMemory> listener, string listenerId, long now, Func<MindMemory, bool> toldBefore, Func<string, bool> listenerKnows = null)
        {
            if (teller == null || listenerId == null)
                return null;
            MindMemory best = null;
            double most = ToldStrength;
            foreach (MindMemory m in teller)
            {
                if (m == null || m.Kind == null || Array.IndexOf(NeverTold, m.Kind) >= 0 || m.DueAt > now || m.At > now || now - m.At > ToldDays * 1440)
                    continue;
                if (m.How == "did" && m.Role == "doer")
                    continue;   // nobody tells on himself: a thief, a bully, a drunk, the one who cut another dead
                if (m.Who != null && m.Who.Contains(listenerId))
                    continue;   // never someone's own business told back to them
                if (m.From == listenerId)
                    continue;   // never back to the one who told it
                if (toldBefore != null && toldBefore(m))
                    continue;
                if (listener != null && m.Key != null)
                {
                    bool knows = false;
                    foreach (MindMemory x in listener)
                        if (x.Key == m.Key)
                        {
                            knows = true;
                            break;
                        }
                    if (knows)
                        continue;
                }
                double s = m.Strength * Math.Pow(0.5, Math.Max(0, now - LearnedAt(m)) / HalfLife(m)) * (m.From != null ? 0.6 : 1.0);   // news ages from when they got it
                if (listenerKnows != null && m.Who != null)
                    foreach (string id in m.Who)
                        if (id != null && listenerKnows(id))
                        {
                            s *= 1.5;   // news of someone the listener knows is worth more to them (importance to the hearer)
                            break;
                        }
                if (s > most)
                {
                    most = s;
                    best = m;
                }
            }
            return best;
        }

        /// The listener's memory of what the teller told: heard, from the teller, one remove further, felt and kept less,
        /// with the names and ids as the teller had them (what the telling loses is lost when it's read, by Drifted);
        /// never a spot (they weren't there). A thing the teller was in himself ("did": his memory holds only the other)
        /// gets the teller into its cast, doer first, so the words and the own-business rule know him. The caller counts
        /// the telling (Told) once the copy has stayed with the listener.
        public static MindMemory Retold(MindMemory m, string tellerId, string tellerFirstName, long now)
        {
            var who = m.Who != null ? new List<string>(m.Who) : new List<string>();
            var names = m.Names != null ? new List<string>(m.Names) : new List<string>();
            if (m.How == "did" && !who.Contains(tellerId))
            {
                while (names.Count < who.Count)
                    names.Add("somebody");
                if (m.Role == "done" && who.Count == 0)
                {
                    who.Add(null);   // the one who did it to them, unknown (a lift): the doer's place stays his
                    names.Add("somebody");
                }
                int at = m.Role == "done" ? Math.Min(1, who.Count) : 0;   // the doer first, as the told pools have it
                who.Insert(at, tellerId);
                names.Insert(at, tellerFirstName ?? "somebody");
            }
            return new MindMemory
            {
                Kind = m.Kind, Sub = m.Sub, Who = who, Names = names,
                Town = m.Town, Place = m.Place, At = m.At, LearnedAt = now, How = "heard", Role = null,
                Feel = (int)Math.Round(m.Feel * 0.6), Strength = Math.Max(10, m.Strength * 7 / 10), Key = m.Key, From = tellerId, Removes = m.Removes + 1
            };
        }

        // ---- drift and gist, worked out when read ---------------------------------------------------

        /// What a slight told on grows into: a brush-off becomes a quarrel, a quarrel a fight.
        private static string Worse(string sub)
        {
            switch (sub)
            {
                case "brushoff": case "scorn": case "passby": return "argue";
                case "argue": case "snap": case "x:snap": case "x:quarrel": case "faced": case "rough": return "brawl";
                default: return null;
            }
        }

        /// The memory as this person has it now: the saved record itself when nothing has drifted (anything they saw or
        /// did themselves within twice its half-life, the player's deeds always), else a transient copy with what
        /// reconstructive memory does: a told or heard thing's doer, when nobody knew who it was, becomes someone the
        /// rememberer already dislikes (`foe`: a hostile tie's first name for this reader, or null); a told slight grows;
        /// two become three (`Many`, read by the words); and past twice its half-life a memory keeps its kind and feeling
        /// and loses strangers' names (`known`: a tie's or the dead's id, which never go). One draw per reader and remove,
        /// with the thresholds rising by the day, so the same person has the same memory each time they're asked and what
        /// has drifted stays drifted. Nothing is written back.
        public static MindMemory Drifted(MindMemory m, string readerId, long now, Func<string, bool> known, Func<string, string[]> foe)
        {
            if (m == null)
                return null;
            if (AboutYou(m))
                return m;   // the player's deeds never drift here
            long age = Math.Max(0, now - m.At);
            bool told = m.From != null, heard = m.How == "heard";
            bool gist = age > 2 * HalfLife(m) && m.Names != null && m.Who != null;
            if (!told && !heard && !gist)
                return m;
            string seed = m.Key + "|" + readerId + "|" + m.Removes;   // one draw per reader and remove; only the thresholds rise with time, so what has drifted stays drifted
            int days = (int)Math.Min(8, age / 1440);
            MindMemory d = null;
            Func<MindMemory> copy = () => d ?? (d = new MindMemory
            {
                Kind = m.Kind, Sub = m.Sub, Who = m.Who, Names = m.Names != null ? new List<string>(m.Names) : null, Town = m.Town, Place = m.Place, At = m.At, LearnedAt = m.LearnedAt,
                How = m.How, Role = m.Role, Feel = m.Feel, Strength = m.Strength, Times = m.Times, LastAt = m.LastAt, Key = m.Key, DueAt = m.DueAt,
                From = m.From, Removes = m.Removes, Told = m.Told, PX = m.PX, PY = m.PY, PZ = m.PZ
            });
            if (told || heard)
            {
                if (m.Names != null && m.Names.Count > 0 && (m.Who == null || m.WhoAt(0) == null) && foe != null && Voice.Hash(seed + "|df") % 100 < 15 + 10 * m.Removes + days)
                {
                    string[] f = foe(readerId);   // {id, first name}
                    if (f != null && f.Length == 2 && f[1] != null && f[0] != m.From && (m.Who == null || !m.Who.Contains(f[0])))
                        copy().Names[0] = f[1];   // the unnamed doer becomes the man they already hold against (never one already in it, nor the teller)
                }
                string worse = told ? Worse(m.Sub) : null;
                if (worse != null && Voice.Hash(seed + "|dw") % 100 < 6 + 5 * m.Removes + days)
                {
                    copy().Sub = worse;   // a slight grows in the telling, and with the days
                    d.Kind = GroupOf(worse) ?? m.Kind;
                }
                if (m.Who != null && m.Who.Count >= 2 && m.WhoAt(0) != null && m.WhoAt(1) != null && Voice.Hash(seed + "|dm") % 100 < 5 + 5 * m.Removes + days / 2)
                    copy().Many = true;   // two become three
            }
            if (gist)
            {
                for (int i = 0; i < m.Names.Count; i++)
                {
                    string id = m.WhoAt(i);
                    if (id == "player" || (id != null && known != null && known(id)))
                        continue;   // a tie's name and the dead's never go
                    string n = m.Names[i];
                    if (n != null && !n.StartsWith("a ") && !n.StartsWith("an ") && !n.StartsWith("some"))
                        copy().Names[i] = "somebody";   // never "some fella" for a woman: the memory doesn't say
                }
            }
            return d ?? m;
        }

        /// Whether anything about it has drifted or gone to gist as this person has it now (for the log and the tests).
        public static bool Drifts(MindMemory m, string readerId, long now, Func<string, bool> known, Func<string, string[]> foe)
        {
            MindMemory d = Drifted(m, readerId, now, known, foe);
            return d != null && !ReferenceEquals(d, m);
        }

        // ---- places ----------------------------------------------------------------------------------

        /// Gives the spot of a happening to this person's memories written of it in the same game minute (NoteHappened
        /// writes the memories before the record, so the record fills them in): the memories whose kind is the scene's
        /// and whose people are its people, with no spot yet. The number filled.
        public static int FillSpot(List<MindMemory> mind, Happening h, string ownerId)
        {
            if (mind == null || h == null || h.Kind == null || (h.X == 0f && h.Y == 0f && h.Z == 0f))
                return 0;
            int n = 0;
            bool deed = h.Kind.StartsWith("deed:"), death = h.Kind == "death";
            string kind = h.Kind.StartsWith("scene:") ? h.Kind.Substring(6) : deed ? h.Kind.Substring(5) : h.Kind;
            foreach (MindMemory m in mind)
            {
                if (HasSpot(m) || m.From != null || m.At != h.At || m.Kind == null || !HerePools.ContainsKey("here|" + m.Kind))
                    continue;   // only a memory the spot has words for (small talk has none)
                bool fits = m.Sub == kind || m.Kind == kind;
                if (!fits)
                    continue;
                if (m.Who != null)
                {
                    bool doerIn, targetIn;
                    if (deed)
                    {
                        doerIn = m.Who.Contains("player");   // the player's deed: his memory names him, not the victim
                        targetIn = true;
                    }
                    else if (death)
                    {
                        doerIn = true;   // a death seen names the dead, not who killed them
                        targetIn = h.TargetId == null || m.Who.Contains(h.TargetId);
                    }
                    else
                    {
                        doerIn = h.DoerId == null || h.DoerId == ownerId || m.Who.Contains(h.DoerId);
                        targetIn = h.TargetId == null || h.TargetId == ownerId || m.Who.Contains(h.TargetId);
                    }
                    if (!doerIn || !targetIn)
                        continue;
                }
                m.PX = h.X;
                m.PY = h.Y;
                m.PZ = h.Z;
                n++;
            }
            return n;
        }

        /// The memories with a spot within HereMetres of here, nearest first (old memories have no spot and never come).
        public static List<MindMemory> NearHere(List<MindMemory> mind, float x, float y, float z, List<MindMemory> into = null)
        {
            var list = into ?? new List<MindMemory>();
            list.Clear();
            if (mind == null)
                return list;
            foreach (MindMemory m in mind)
            {
                if (!HasSpot(m))
                    continue;
                if (Dist2(m, x, y, z) <= HereMetres * HereMetres)
                    list.Add(m);
            }
            list.Sort((a, b) => Dist2(a, x, y, z).CompareTo(Dist2(b, x, y, z)));
            return list;
        }

        private static float Dist2(MindMemory m, float x, float y, float z)
        {
            float dx = m.PX - x, dy = m.PY - y, dz = m.PZ - z;
            return dx * dx + dy * dy + dz * dz;
        }
    }
}
