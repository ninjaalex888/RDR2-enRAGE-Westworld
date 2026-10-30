// Town posts in the game (see Core/Posts.cs). The person the game puts behind each counter is one person for good,
// named and remembering the player. Shot down, by anyone's hand, they come back bandaged the next time the game puts
// them at the counter, remembering who did it (TownPostsAfterDeath = survive); or the post passes to a relative or a
// newcomer (successor), who is dressed in another of the model's outfits, only while nobody can see it happen (the
// outfit probe logs how many each model has). Nothing here moves them: they stay
// at their counters, out of scenes, stalking and the wake acts that walk people about. Not during story missions.
using System;
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private readonly HashSet<uint> _outfitsLogged = new HashSet<uint>();
        private readonly Dictionary<int, int> _dressDue = new Dictionary<int, int>();   // handle -> outfit preset to put on unseen
        private readonly HashSet<int> _postDeathsSeen = new HashSet<int>();
        private readonly Dictionary<int, string> _bodyWas = new Dictionary<int, string>();   // handle -> the holder it was bound to, this session
        private int _nextPostTick, _nextPostDeaths;
        private Action _aPosts;

        private void PostsStep(int now)
        {
            if (_aPosts == null)
                _aPosts = () => PostsTick(_now);
            Safe("town posts", _aPosts);
        }

        private bool PostsOn { get { return _cfg.TownPosts && !MISC.GET_MISSION_FLAG(); } }

        private int _nextSpotLook;

        /// Where each town's places are: a counter keeper standing still at work, within 120 m, marks his place (Places).
        private void SpotsLook(Ped me)
        {
            if (_store == null)
                return;
            Vector3 at = me.Position;
            foreach (Ped p in World.GetAllPeds())
            {
                if (p == null || p.Position.DistanceTo(at) > 120f)
                    continue;   // the distance first
                PostKind k = PostKindOf(p);
                if (k == null || p.IsDead || ENTITY.GET_ENTITY_SPEED(p.Handle) > 0.3f || PED.IS_PED_IN_ANY_VEHICLE(p.Handle, false))
                    continue;
                Vector3 pos = p.Position;
                if (PlaceSpots.Note(_store.Spots, k.Town, k.Kind, k.Model, pos.X, pos.Y, pos.Z, _store.LastGameMin))
                    Log.Write("PLACE " + k.Town + ": " + k.Role + "'s spot, " + (int)pos.DistanceTo(at) + " m from you (where people who choose it will go)");
            }
        }

        private static PostKind PostKindOf(Ped p)
        {
            return p != null && p.Exists() ? Posts.Of(Natives.ModelName(p)) : null;
        }

        /// The person keeping this post, bound to this ped. Null if posts are off or the ped is dead.
        private Resident HolderAt(Ped ped, PostKind kind)
        {
            int h = ped.Handle;
            if (!PostsOn || ENTITY.IS_ENTITY_DEAD(h))
                return null;
            uint model = ENTITY.GET_ENTITY_MODEL(h);
            ResBind b;
            if (_resBind.TryGetValue(h, out b) && b.Model == model && !b.R.Dead && b.R.PostId != null)
                return b.R;
            Resident r = _engine.HolderOf(kind, model, Bridge.Now());
            _resBind[h] = new ResBind { R = r, Model = model };
            _bodyWas[h] = r.Id;   // kept after the binding goes, so its body is known for whose it was
            Post post = _engine.PostOf(r);
            if (post != null && post.FellAt > post.BackAt && _cfg.TownPostsAfterDeath != "successor")
            {
                // shot down here, and the game has brought them back bandaged
                string news = _engine.PostBack(post, r, Bridge.Now());
                if (news != null)
                    News(post.Town, news, "news");
            }
            int presets = OutfitCount(ped, kind);
            if (post != null && presets > 1)
            {
                if (post.Outfit < 0 && r.TookOverAt > 0)
                    post.Outfit = 1 + Voice.Hash(r.Id + "|outfit") % (presets - 1);   // the one who took over looks like someone else
                if (post.Outfit >= 0)
                    _dressDue[h] = post.Outfit;   // the game brings them back as it made them: dress them again, unseen
            }
            return r;
        }

        private int OutfitCount(Ped ped, PostKind kind)
        {
            int n = PED.GET_NUM_META_PED_OUTFITS(ped.Handle);
            if (_outfitsLogged.Add(ENTITY.GET_ENTITY_MODEL(ped.Handle)))
                Log.Write("OUTFIT probe: " + kind.Model + " (" + kind.Town + ", " + kind.Role + ") has " + n + " outfit preset" + (n == 1 ? "" : "s"));
            return n;
        }

        /// Once a second: dress anyone due a new look while nobody can see; every few seconds, a death at a post
        /// (by any hand) vacates it. The player's own kills are noted at once (PostKilled).
        private void PostsTick(int now)
        {
            if (now < _nextPostTick)
                return;
            _nextPostTick = now + 1000;
            Ped me = Game.Player.Character;
            foreach (KeyValuePair<int, int> kv in _dressDue.ToList())
            {
                int h = kv.Key;
                if (!ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h))
                {
                    _dressDue.Remove(h);
                    continue;
                }
                if (ENTITY.IS_ENTITY_ON_SCREEN(h) || ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(me.Handle, h, 17))
                    continue;
                PED._EQUIP_META_PED_OUTFIT_PRESET(h, kv.Value, false);
                PED._UPDATE_PED_VARIATION(h, false, true, true, true, false);
                _dressDue.Remove(h);
                ResBind b;
                Log.Write("OUTFIT probe: dressed " + (_resBind.TryGetValue(h, out b) ? b.R.Name : "someone") + " in preset " + kv.Value + ", out of sight");
            }
            if (now >= _nextSpotLook)
            {
                _nextSpotLook = now + 5000;
                SpotsLook(me);
            }
            if (!PostsOn || now < _nextPostDeaths)
                return;
            _nextPostDeaths = now + 3000;
            foreach (Ped p in World.GetAllPeds())
            {
                int h = p.Handle;
                if (!ENTITY.IS_ENTITY_DEAD(h) || _postDeathsSeen.Contains(h))
                    continue;
                PostKind k = PostKindOf(p);
                if (k == null)
                    continue;
                _postDeathsSeen.Add(h);
                uint model = ENTITY.GET_ENTITY_MODEL(h);
                Post post = _engine.PostFor(k);
                Resident holder = post.HolderId != null ? _store.FindResident(post.HolderId) : null;
                string was;
                _bodyWas.TryGetValue(h, out was);
                bool living = World.GetAllPeds().Any(x => x.Handle != h && !ENTITY.IS_ENTITY_DEAD(x.Handle) && ENTITY.GET_ENTITY_MODEL(x.Handle) == model);
                bool counts = _cfg.TownPostsAfterDeath == "successor" ? Posts.BodyVacates(holder, post, was, living, Bridge.Now()) : Posts.BodyFells(holder, post, was, living, Bridge.Now());
                if (!counts)
                {
                    Log.Detail("POST " + post.Town + ", " + post.Role + ": an old body (" + (was ?? "nobody we knew") + "), not " + (holder != null ? holder.Name : "anyone") + "'s");
                    continue;
                }
                PostFell(p, k, PED.GET_PED_SOURCE_OF_DEATH(h) == me.Handle, CountPeopleNear(p.Position, h, 35f));
            }
            _postDeathsSeen.RemoveWhere(h => !ENTITY.DOES_ENTITY_EXIST(h));
            foreach (int h in _bodyWas.Keys.Where(x => !ENTITY.DOES_ENTITY_EXIST(x)).ToList())
                _bodyWas.Remove(h);
        }

        /// One line for the F7 dump: who keeps which counter, and who took one over.
        private string PostsStatus()
        {
            if (!_cfg.TownPosts)
                return "off";
            List<string> parts = _store.Posts.OrderBy(p => p.Town).Select(p =>
            {
                Resident r = _store.FindResident(p.HolderId);
                return p.Town + " " + p.Role.Replace("the ", "") + ": " + (r == null ? "nobody yet" : r.Name + (r.Dead ? " (died, awaiting a successor)" : Posts.Story(r) != null ? " (" + Posts.Story(r) + ")" : "")) +
                       (r != null && r.Wounds > 0 ? (p.FellAt > p.BackAt ? ", shot down, not back yet" : ", shot down " + r.Wounds + (r.Wounds == 1 ? " time" : " times") + " and back") : "") +
                       (p.Outfit >= 0 ? ", outfit " + p.Outfit : "");
            }).ToList();
            return parts.Count == 0 ? "none met yet" : parts.Count + " known: " + string.Join("; ", parts);
        }

        /// The player killed someone: if they kept a post, they're shot down now (seen: how many saw it).
        private void PostKilled(Ped victim, int seen)
        {
            PostKind k = PostKindOf(victim);
            if (k == null || !PostsOn || !_postDeathsSeen.Add(victim.Handle))
                return;
            Post post = _engine.PostFor(k);
            if (_cfg.TownPostsAfterDeath != "successor" && post.HolderId != null && post.FellAt > post.BackAt)
                return;   // already down: the same fall
            PostFell(victim, k, true, seen);
        }

        /// A counter's person fell: shot down to come back bandaged (survive), or dead and succeeded (successor).
        /// They know it was the player if anyone saw, or if he was close enough to see him themselves.
        private void PostFell(Ped body, PostKind k, bool byPlayer, int seen)
        {
            Ped me = Game.Player.Character;
            bool knew = byPlayer && (seen > 0 || body.Position.DistanceTo(me.Position) < 25f);
            _engine.PostShot(k, ENTITY.GET_ENTITY_MODEL(body.Handle), byPlayer, knew, seen, WitnessesNear(body.Position, body.Handle, 35f), Bridge.Now());
        }

        /// The player is at their counter: counted once a game hour, so they know a new face from a regular.
        private void NotePostVisit(Resident r, float dist)
        {
            if (dist > 8f)
                return;
            long g = Bridge.Now();
            PersonState st = _store.Get(_engine.PersonOfResident(r));
            if (g - st.LastSeenAt < 60)
                return;
            st.Sightings++;
            st.LastSeenAt = g;
            _engine.KeepResident(r, g);
        }
    }
}
