// An execution on the road (Executions.cs): the plan from the captive's own life, and what follows a rescue or a death.
// The death itself is the people ledger's (the game side binds him, so it's noted with how and by whose hand).
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public sealed partial class Engine
    {
        /// Why he's on his knees, from the worry he's living through today and who is at the other end of it.
        public Executions.Plan ExecutionPlan(Resident r, string region, string seed)
        {
            Person p = PersonOfResident(r);
            long day = Store.LastGameMin / 1440;
            string kind;
            int stage;
            bool good;
            string worry = InnerLife.WorryNow(p, day, out kind, out stage, out good) ? kind : null;
            string who = worry == "debt" || worry == "feud" ? InnerLife.WorryFill(p, day, "{who}") : null;
            WorryLink link = worry != null ? Store.Links.LastOrDefault(l => l.Id == r.Id && l.Kind == worry) : null;
            return Executions.For(worry, who, link != null ? link.OtherId : null, region, seed);
        }

        /// He met the player over it: they've met, whatever happens next (the corner speaks only of people he's met).
        public void ExecutionSeen(Resident r, long now)
        {
            PersonState st = Store.Get(PersonOfResident(r));
            st.Sightings = Math.Max(st.Sightings, 2);
            KeepResident(r, now);
        }

        /// Saved: fonder of him for good, thinking of it for days, and the town hears of it.
        public void Rescued(Resident r, Executions.Plan plan, string town, string zone, float x, float y, float z, long now)
        {
            Person p = PersonOfResident(r);
            PersonState st = Store.Get(p);
            st.Talks++;
            st.Sightings = Math.Max(st.Sightings, 2);
            st.Affection = Romance.Clamp(st.Affection + 30, -100, 100);
            st.Mark("rescued:" + now / 1440);
            st.Mark("rescue reason:" + plan.Reason + ":" + plan.Boss);
            KeepResident(r, now);
            if (Cfg.Minds)
                Minds.Note(st.Mind, new MindMemory
                {
                    Kind = "rescued", Sub = "rescue", How = "did", Names = plan.Boss != null ? new List<string> { plan.Boss } : null, Town = town,
                    At = Store.LastGameMin, Feel = 3, Strength = 80, Key = "rescued:" + Store.LastGameMin / 1440
                }, Store.LastGameMin);   // on his mind for days, fading (Minds.cs)
            OnDeed(Deeds.Make("rescue", p.Name, town, zone, now, x, y, z, 1), null, now);
            Log("EXECUTION " + p.Name + ": saved by you (" + plan.Reason + ", " + plan.Boss + ")");
        }

        /// The player gave someone a hand up after a fall: fonder of him, and they remember it (a kindness done to them).
        /// Only for those who can remember (a lasting id); the game side keeps an unsaved resident first.
        public void HelpedUp(Person p, bool nobodyElse, long now)
        {
            if (p == null || !CanRemember(p.Id))
                return;
            PersonState st = Store.Get(p);
            st.Talks++;
            st.Sightings = Math.Max(st.Sightings, 1);
            st.Affection = Romance.Clamp(st.Affection + (nobodyElse ? 10 : 6), -100, 100);
            if (Cfg.Minds)
                Minds.Note(st.Mind, new MindMemory
                {
                    Kind = "deed:kindness", Sub = "helpup", How = "did", Who = new List<string> { "player" }, Town = CurrentTown, At = Store.LastGameMin,
                    Feel = 2, Strength = nobodyElse ? 60 : 50, Key = "helpup:" + Store.LastGameMin / 1440
                }, Store.LastGameMin);
            Log("KIND " + p.Name + ": you gave them a hand up" + (nobodyElse ? " (nobody else stopped)" : ""));
        }

        /// The night visitor was out in this town: next morning (07:00-09:00) the town speaks of a body nobody can account
        /// for (a corner notice, no names).
        public void VisitorSeen(string town, long now)
        {
            long morning = (now / 1440 + (now % 1440 >= 7 * 60 ? 1 : 0)) * 1440 + 7 * 60 + Voice.Hash(town + now) % 120;
            Later(new LaterAct { Kind = "news", Title = "Word around " + ShortTown(town), At = now, Text = Visitor.Morning(town, town + now), DueAt = morning });
            Log("VISITOR the town will talk of it in the morning (" + (morning - now) + " game min)");
        }

        /// Someone stood the player a drink with the rest of the room, and he took it: a little fonder of him.
        public void StoodYouADrink(Person p)
        {
            if (p == null || !CanRemember(p.Id))
                return;
            PersonState st = Store.Get(p);
            st.Sightings = Math.Max(st.Sightings, 1);
            st.Affection = Romance.Clamp(st.Affection + 3, -100, 100);
        }

        /// Shot: his kin hear of it, and once he's found, the town (a notice for the corner, 6 to 18 game hours on).
        public void Executed(Resident r, Executions.Plan plan, string town, long now)
        {
            Person p = PersonOfResident(r);
            if (Cfg.WordOfMouth)
                QueueDeathNews(p, now, byYou: false);   // the men did it: no avenger, no grief letter for him
            Later(new LaterAct
            {
                Kind = "news", PersonId = p.Id, Title = "Word around " + ShortTown(town ?? r.Town), At = now,
                Text = Executions.Found(plan, p.Name, town ?? r.Town, p.Id), DueAt = Knowing.TalkDue(now, false, p.Id + "found")
            });
            Log("EXECUTION " + p.Name + ": shot (" + plan.Reason + ", " + plan.Boss + "); the town hears once he's found");
        }

        /// A thought from the days after he was saved, or null (3 game days).
        private string AfterRescue(PersonState st, string seed)
        {
            if (st == null)
                return null;
            long day = Store.LastGameMin / 1440;
            bool lately = false;
            for (long d = day; d > day - 3 && !lately; d--)
                lately = st.Has("rescued:" + d);
            if (!lately)
                return null;
            Executions.Plan plan = null;
            string mark = st.Told != null ? st.Told.LastOrDefault(t => t.StartsWith("rescue reason:")) : null;
            if (mark != null)
            {
                string[] parts = mark.Split(':');
                plan = new Executions.Plan { Reason = parts.Length > 1 ? parts[1] : null, Boss = parts.Length > 2 ? parts[2] : null };
            }
            return Executions.After(plan, seed);
        }
    }
}
