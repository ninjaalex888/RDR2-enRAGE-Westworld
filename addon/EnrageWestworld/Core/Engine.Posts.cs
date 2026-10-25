// Town posts (see Posts.cs): one person at each counter, kept for good. Shot down, they survive and come back
// bandaged (the default), or with TownPostsAfterDeath = successor the post passes on.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public sealed partial class Engine
    {
        /// The post a model plays, made the first time it's seen.
        public Post PostFor(PostKind kind)
        {
            string id = "post:" + kind.Model.ToLowerInvariant();
            Post p = Store.Posts.FirstOrDefault(x => x.Id == id);
            if (p == null)
            {
                p = new Post { Id = id, Model = kind.Model, Town = kind.Town, Role = kind.Role };
                Store.Posts.Add(p);
            }
            return p;
        }

        /// Who keeps the post now: the same person while they live; after a death, whoever took it over (made now,
        /// the first time the game puts someone at the counter again).
        public Resident HolderOf(PostKind kind, uint modelHash, long now)
        {
            Post post = PostFor(kind);
            Resident holder = post.HolderId != null ? Store.FindResident(post.HolderId) : null;
            if (holder != null && holder.Dead && Cfg.TownPostsAfterDeath != "successor")
            {
                // noted as dead before survive was the rule: they were only shot down, and the game brings them back
                holder.Dead = false;
                holder.Wounds = Math.Max(1, holder.Wounds);
                holder.WoundedAt = post.VacantAt;
                holder.WoundedByPlayer = post.KilledByPlayer;
                holder.KnowsShooter = post.KillerKnown;
                post.FellAt = Math.Max(post.FellAt, post.VacantAt);
            }
            if (holder != null && !holder.Dead)
                return holder;
            string id = Resident.Prefix + (++Store.NextResident);
            bool first = holder == null && post.LastName == null;
            if (post.HolderId != null && !post.Former.Contains(post.HolderId))
                post.Former.Add(post.HolderId);   // before the successor is made: their name comes from the count
            Resident r = first ? Posts.First(post, kind, modelHash, id, now, Voice.NowYear) : Posts.Successor(post, kind, holder, modelHash, id, now, Voice.NowYear);
            post.HolderId = r.Id;
            post.Outfit = -1;   // the game layer decides how a successor is dressed
            Store.KeepResident(r, now);
            if (Cfg.AwakeningShare > 0 && _rng.Next(100) < Cfg.AwakeningShare)
                Advance(PersonOfResident(r), Store.Get(PersonOfResident(r)), now);
            Log("POST " + post.Town + ", " + kind.Role + ": " + r.Name + (Posts.Story(r) != null ? ", " + Posts.Story(r) : first ? ", the first we know of" : ""));
            return r;
        }

        /// The person at a post died. byPlayer: the player killed them; seen: how many saw it. Nothing happens if
        /// the post has already been vacated for them.
        public void VacatePost(PostKind kind, uint modelHash, bool byPlayer, int seen, long now)
        {
            Post post = PostFor(kind);
            Resident holder = post.HolderId != null ? Store.FindResident(post.HolderId) : null;
            if (holder != null && holder.Dead)
                return;
            if (holder == null)
                holder = HolderOf(kind, modelHash, now);   // killed before anyone knew their name: they had one all the same
            holder.Dead = true;
            holder.TouchedAt = now;
            post.LastName = holder.Name;
            post.VacantAt = now;
            post.KilledByPlayer = byPlayer;
            post.KillerKnown = byPlayer && seen > 0;
            Log("POST " + post.Town + ", " + kind.Role + ": " + holder.Name + " died" + (byPlayer ? " by your hand" + (seen > 0 ? ", seen by " + seen : ", unseen") : "") +
                "; the counter will pass to a relative or a newcomer");
        }

        /// The person at a post was shot down. survive (the default): the game brings them back bandaged, so they
        /// lived: the same person, wounded, who remembers who did it (knew: they saw it was the player).
        /// successor: they died, and the post passes on (VacatePost). witnesses: who saw them fall.
        public void PostShot(PostKind kind, uint modelHash, bool byPlayer, bool knew, int seen, IList<string> witnesses, long now)
        {
            if (Cfg.TownPostsAfterDeath == "successor")
            {
                VacatePost(kind, modelHash, byPlayer, seen, now);
                return;
            }
            Post post = PostFor(kind);
            Resident holder = HolderOf(kind, modelHash, now);   // made now if nobody knew their name yet
            holder.Wounds++;
            holder.WoundedAt = now;
            holder.WoundedByPlayer = byPlayer;
            holder.KnowsShooter = byPlayer && knew;
            post.FellAt = now;
            post.SawFall = (witnesses ?? new List<string>()).Take(12).ToList();
            Store.KeepResident(holder, now);
            Log("POST " + post.Town + ", " + kind.Role + ": " + holder.Name + " was shot down" + (byPlayer ? " by you" + (holder.KnowsShooter ? ", and knows it" : ", unseen") : "") +
                (seen > 0 ? ", " + seen + " saw" : "") + "; the game will bring " + (holder.Woman ? "her" : "him") + " back bandaged (time " + holder.Wounds + ")");
        }

        /// Someone shot down at their counter is back at it: those who saw them fall find it uncanny (in West World),
        /// and the town hears. Returns the news, or null.
        public string PostBack(Post post, Resident r, long now)
        {
            post.BackAt = now;
            int uneasy = 0;
            if (Cfg.WestWorld)
                foreach (string id in post.SawFall)
                {
                    Person w = Find(id);
                    if (w == null || !w.IsAlive)
                        continue;
                    PersonState st = Store.Get(w);
                    st.Awareness = Math.Min(100, st.Awareness + 15);   // a man they saw fall, back at his counter
                    uneasy++;
                }
            Log("POST " + post.Town + ", " + post.Role + ": " + r.Name + " is back at the counter, bandaged" + (uneasy > 0 ? "; " + uneasy + " who saw the fall find it uncanny" : ""));
            return Posts.BackNews(r, post);
        }

        /// The post this resident keeps, if any.
        public Post PostOf(Resident r)
        {
            return r == null || r.PostId == null ? null : Store.Posts.FirstOrDefault(x => x.Id == r.PostId);
        }

        /// What someone keeping a post is thinking: their counter, the one before them, the player; else like anyone else.
        public string PostThought(Resident r, bool near, int hour, string seed, InnerLife.PlayerLook look = null)
        {
            Post post = PostOf(r);
            PersonState st = Store.Find(r.Id);
            // the same person at the same counter every visit: the worry they're living through shows, day by day
            // (not while new to the counter or freshly shot down: those thoughts come first)
            bool fresh = r.TookOverAt > 0 && Store.LastGameMin - r.TookOverAt < 3 * 1440;
            bool hurt = r.Wounds > 0 && Store.LastGameMin - r.WoundedAt < 3 * 1440;
            if (!fresh && !hurt && Store.LastGameMin > 0 && Voice.Chance(seed + "pw", 25))
            {
                EnsureLink(PersonOfResident(r));
                string w = InnerLife.WorryThought(PersonOfResident(r), Store.LastGameMin / 1440, seed, st != null ? st.Musings : null);
                if (w != null)
                    return w;
            }
            string t = post != null ? Posts.Thought(r, post, Posts.Of(post.Model), near, st != null ? st.Sightings : 0, Store.LastGameMin, seed) : null;
            return t ?? ResidentThought(PersonOfResident(r), hour, seed, look);
        }
    }
}
