// What happened, and who saw it (overnight 2026-10-27, Tier 1.5; Core/WorldEvents.cs): the game side works out the
// witnesses for each scene, deed, death and encounter (a clear line of sight to it within 30 m, or within earshot of a
// shot or a shout) and writes the record. One scan of the peds around, only when something happens (never a frame
// cost), at most 20 witnesses kept.
using System.Collections.Generic;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private int _eventsNoted;

        /// Writes down what happened at `at`: `focus` is what a witness must see (the first of a scene's two, the player
        /// for his deeds, the body for a death); `a` and `b` are the ones it happened between (never their own witnesses).
        private Happening RecordEvent(string kind, string doerId, string doerName, string targetId, string targetName, Vector3 at, int focus, int a, int b, string outcome)
        {
            if (_engine == null || _store == null)
                return null;
            Happening e = new Happening
            {
                Kind = kind, DoerId = doerId, DoerName = doerName, TargetId = targetId, TargetName = targetName,
                Town = _bridge != null ? _bridge.TownOnly(at) ?? _bridge.TownAt(at) : null, Place = _engine.NowIndoors ? _engine.NowPlace : null,
                X = at.X, Y = at.Y, Z = at.Z, Outcome = outcome
            };
            string loud = WorldEvents.Loudness(kind);
            float reach = loud == "shot" ? WorldEvents.ShotMetres : WorldEvents.SightMetres;
            Ped me = Game.Player.Character;
            foreach (Ped p in World.GetAllPeds())
            {
                if (e.Witnesses.Count >= 20)
                    break;
                if (p == null || !p.Exists())
                    continue;
                int h = p.Handle;
                if (h == a || h == b || h == me.Handle || ENTITY.IS_ENTITY_DEAD(h) || !PED.IS_PED_HUMAN(h) || Natives.IsChild(p))
                    continue;
                float m = p.Position.DistanceTo(at);
                if (m > reach)
                    continue;
                bool sight = focus != 0 && ENTITY.DOES_ENTITY_EXIST(focus) && ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(h, focus, 17);
                string how = WorldEvents.How(m, sight, loud);
                if (how == null)
                    continue;
                string id, name;
                bool known = LastingId(p, out id, out name);
                e.Witnesses.Add(new EventWitness
                {
                    Id = known && _engine.CanRemember(id) ? id : null, Name = name != null ? Person.FirstOf(name) : Director.Kind(p, !PED.IS_PED_MALE(h)),
                    Metres = (float)System.Math.Round(m, 1), How = how
                });
            }
            // the player is a witness too, when it wasn't his own doing
            if (doerId != "player" && targetId != "player")
            {
                float pm = me.Position.DistanceTo(at);
                bool sees = focus != 0 && ENTITY.DOES_ENTITY_EXIST(focus) && ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(me.Handle, focus, 17);
                string how = WorldEvents.How(pm, sees, loud);
                if (how != null)
                    e.Witnesses.Add(new EventWitness { Id = "player", Name = "you", Metres = (float)System.Math.Round(pm, 1), How = how });
            }
            _eventsNoted++;
            return _engine.NoteEvent(e);
        }

        /// A scene between two peds, from what NoteHappened knows.
        private void RecordSceneEvent(string kind, List<Ped> people)
        {
            if (people == null || people.Count < 2 || people[0] == null || !people[0].Exists())
                return;
            string aId, aName, bId, bName;
            LastingId(people[0], out aId, out aName);
            LastingId(people[1], out bId, out bName);
            bool aw = !PED.IS_PED_MALE(people[0].Handle), bw = people[1] != null && people[1].Exists() && !PED.IS_PED_MALE(people[1].Handle);
            RecordEvent("scene:" + kind, aId, aName ?? Director.Kind(people[0], aw), bId, bName ?? (people[1] != null && people[1].Exists() ? Director.Kind(people[1], bw) : null),
                        people[0].Position, people[0].Handle, people[0].Handle, people[1] != null ? people[1].Handle : 0, kind);
        }

        /// For the F7 dump: the last five events.
        private string EventsStatus()
        {
            if (_store == null || _store.WorldLog.Count == 0)
                return "none yet";
            var parts = new List<string>();
            for (int i = _store.WorldLog.Count - 1; i >= 0 && parts.Count < 5; i--)
            {
                Happening e = _store.WorldLog[i];
                parts.Add(WorldEvents.Describe(e) + " (" + System.Math.Max(0, _store.LastGameMin - e.At) + " game min ago)");
            }
            return _store.WorldLog.Count + " kept (" + _eventsNoted + " this session); latest: " + string.Join("; ", parts);
        }
    }
}
