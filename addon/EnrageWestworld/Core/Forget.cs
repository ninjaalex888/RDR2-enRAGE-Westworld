// "Forget you, keep the world" (Lex, 2026-10-30: "the NPCs very quickly come to have very high fear and anger levels with
// the player"; his pick of what a reset at the station clerk forgets). Everything RealNPCs keeps of what people know and feel
// about the player goes: his name to them, the talks, the sightings, the hearsay and the deeds of his they saw or heard of,
// their memories of him, their fondness and courting, his own deeds and the town's talk of them, the kin riding in for
// revenge, the word and the diary entries still waiting their time, the dead he killed being his. The people stay, with
// their families, their diaries already written, their worries, the Awakening and the ties between them. Pure: the game
// side backs up the save first, lets go of everyone and reloads (WestworldScript.Forget.cs).
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public sealed class ForgetCounts
    {
        public int People, Memories, Deeds, Rumours, Avengers, Deaths, Happenings, Waiting, Debts;

        public string Said()
        {
            return People + " people forgot you (" + Memories + " memories of you), " + Deeds + " deeds and " + Rumours + " rumours of them gone, " +
                   Avengers + " avengers stood down, " + Deaths + " deaths no longer by your hand, " + Happenings + " happenings, " + Waiting + " waiting word, " + Debts + " debts";
        }
    }

    public static class Forget
    {
        public const string You = "player";

        public static ForgetCounts AboutPlayer(Store s)
        {
            var c = new ForgetCounts();
            if (s == null)
                return c;
            if (s.People != null)
                foreach (PersonState p in s.People.Values)
                {
                    if (p == null)
                        continue;
                    int memories = p.Mind != null ? p.Mind.RemoveAll(Minds.AboutYou) : 0;
                    bool knew = memories > 0 || p.KnowsName || p.Talks > 0 || p.Sightings > 0 || p.ContactAt > 0 || p.Flirts > 0 || p.Affection != 0 || p.Heart != 0 ||
                                (p.SawDeeds != null && p.SawDeeds.Count > 0) || (p.KnownDeeds != null && p.KnownDeeds.Count > 0);
                    c.Memories += memories;
                    p.KnowsName = false;
                    p.NameAt = 0;
                    p.Talks = 0;
                    p.LastTalkAt = 0;
                    p.Told?.Clear();
                    p.Spread?.Clear();
                    p.Heard?.RemoveAll(h => h == null || h.Text != "died");   // a relative's death, heard of, is their own news
                    p.Sightings = 0;
                    p.FirstSeenTown = null;
                    p.FirstSeenAt = 0;
                    p.LastSeenAt = 0;
                    p.LastRemarkAt = 0;
                    p.Affection = 0;
                    p.Heart = 0;
                    p.HeartAt = 0;
                    p.Flirts = 0;
                    p.FlirtedAt = 0;
                    p.ContactAt = 0;
                    p.JealousOf = null;
                    p.KnowsWife = false;
                    p.KnownDeeds?.Clear();
                    p.SawDeeds?.Clear();
                    if (knew)
                        c.People++;
                }
            if (s.Residents != null)
                foreach (Resident r in s.Residents)
                    if (r != null)
                    {
                        r.WoundedByPlayer = false;
                        r.KnowsShooter = false;
                    }
            c.Deeds = s.Deeds != null ? s.Deeds.Count : 0;
            s.Deeds?.Clear();
            c.Rumours = (s.TownRumours != null ? s.TownRumours.Count : 0) + (s.Rumors != null ? s.Rumors.Count : 0);
            s.TownRumours?.Clear();
            s.Rumors?.Clear();
            c.Avengers = s.Avengers != null ? s.Avengers.Count : 0;
            s.Avengers?.Clear();
            if (s.Deaths != null)
                foreach (DeathEntry d in s.Deaths)
                    if (d != null && d.By == You)
                    {
                        d.By = "unknown";
                        d.ById = null;
                        c.Deaths++;
                    }
            if (s.Reveries != null)
                foreach (Reverie r in s.Reveries.Values)
                    if (r != null)
                        r.ByPlayer = false;
            c.Happenings = s.WorldLog != null ? s.WorldLog.RemoveAll(h => h != null && (h.DoerId == You || h.TargetId == You)) : 0;
            c.Waiting = s.Later != null ? s.Later.RemoveAll(a => a != null && (a.Kind == "word" || a.Kind == "diary")) : 0;   // the town's news stays
            c.Debts = s.Debts != null ? s.Debts.RemoveAll(d => d != null && (d.FromId == You || d.ToId == You)) : 0;
            return c;
        }
    }
}
