// Town posts (see Posts.cs): one person at each counter, kept for good; when they die, the post passes on.
using System;
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
            string t = post != null ? Posts.Thought(r, post, Posts.Of(post.Model), near, st != null ? st.Sightings : 0, Store.LastGameMin, seed) : null;
            return t ?? ResidentThought(PersonOfResident(r), hour, seed, look);
        }
    }
}
