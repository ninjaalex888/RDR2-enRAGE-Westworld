// Minds (Core/Minds.cs) on the game side: who was in a scene and who watched it, as the engine knows them, so each
// keeps a memory of it. Watchers need a clear view of it; for a strong event (blows, a hand caught in a coat) up to
// three passers-by, nearest first, are made residents on the spot so they remember it too (Lex: every grown-up who
// isn't the story's own has a mind). Fewer than that keeps the 700 saved residents for the regulars.
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
        private const float SceneWatchMetres = 15f;
        private const int NewMindsPerEvent = 3;

        /// What a scene leaves in the minds of the two in it and of those who saw it.
        private void NoteMinds(string kind, List<Ped> people, string[] names)
        {
            if (!_cfg.Minds || people == null || people.Count < 2)
                return;
            // its own guard inside the scenes: a fault here costs the memory, never the scene (and after 15, "minds"
            // switches off on its own, logged)
            Safe("minds", () => NoteMindsImpl(kind, people, names));
        }

        private void NoteMindsImpl(string kind, List<Ped> people, string[] names)
        {
            string group = Minds.GroupOf(kind);
            if (group == null)
                return;
            bool strong = group == "blows" || kind == "pickpocket";
            Minds.Party a = PartyOf(people[0], strong), b = PartyOf(people[1], strong);
            if (a == null || b == null)
                return;
            // someone the scene named itself is known by that name to the other
            if (a.Name == null && names != null && names.Length > 0)
                a.Name = names[0];
            if (b.Name == null && names != null && names.Length > 1)
                b.Name = names[1];
            List<Minds.Party> watchers = MindWatchers(people, SceneWatchMetres, strong);
            _engine.RememberScene(kind, a, b, watchers, _placeTown, _engine.NowIndoors ? _engine.NowPlace : null);
        }

        /// Who can see something happen and remember it: any living grown-up who isn't the player or the story's own.
        /// Not the scene-casting rule (Natives.Bystander), which leaves out the fleeing, the fighting and lawmen: around a
        /// shooting that's most of the people who saw it.
        private static bool CanWitness(Ped p, Ped me)
        {
            if (p == null || !p.Exists())
                return false;
            int h = p.Handle;
            return h != me.Handle && !ENTITY.IS_ENTITY_DEAD(h) && PED.IS_PED_HUMAN(h) && !PED.IS_PED_A_PLAYER(h) && !Natives.IsChild(p) && !Natives.StoryModel(p) && !Spawner.IsOurs(h);
        }

        /// Someone as the minds know them: their lasting id (null if they can't be met again), name, town and look.
        /// `save`: a passer-by may be made a resident for it.
        private Minds.Party PartyOf(Ped p, bool save)
        {
            if (p == null || !p.Exists())
                return null;
            string id, name;
            bool woman = !PED.IS_PED_MALE(p.Handle);
            Minds.Party party = new Minds.Party { Label = Director.Kind(p, woman) };
            if (!LastingId(p, out id, out name) && save && !Natives.StoryModel(p))
            {
                Resident r = ResidentOf(p);
                if (r != null && !r.Dead && r.PostId == null)
                {
                    _engine.KeepResident(r, Bridge.Now());
                    id = r.Id;
                    name = r.Name;
                    Log.Detail("MIND " + r.Name + " (" + party.Label + ") kept for what happened in front of them");
                }
            }
            party.Id = id;
            party.Name = name;
            party.Home = HomeOf(p, id);
            return party;
        }

        private string HomeOf(Ped p, string id)
        {
            if (id == null)
                return null;
            if (id.StartsWith("folk:"))
            {
                FolkRef ac = _bridge.AcquaintanceAt(p.Handle);
                return ac != null ? Snapshot.Of(ac).Town : null;
            }
            Resident r = _store.FindResident(id);
            return r != null ? r.Town : null;
        }

        /// Who saw it: people within reach with a clear view of the first of the two, nearest first. The lasting ones
        /// remember; for a strong event a few passers-by are kept as residents to remember it as well.
        private List<Minds.Party> MindWatchers(IList<Ped> people, float metres, bool strong)
        {
            var list = new List<Minds.Party>();
            Ped first = people.Count > 0 ? people[0] : null;
            if (first == null || !first.Exists())
                return list;
            Vector3 at = first.Position;
            Ped me = Game.Player.Character;
            int saved = 0;
            foreach (Ped p in World.GetAllPeds().Where(x => x != null && x.Exists() && x.Position.DistanceTo(at) <= metres).OrderBy(x => x.Position.DistanceTo(at)))
            {
                int h = p.Handle;
                if (people.Any(x => x != null && x.Handle == h) || !CanWitness(p, me) || !ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(h, first.Handle, 17))
                    continue;
                string id, name;
                bool lasting = LastingId(p, out id, out name) && _engine.CanRemember(id);
                if (!lasting && !(strong && saved < NewMindsPerEvent))
                    continue;
                Minds.Party w = PartyOf(p, !lasting);
                if (w == null || w.Id == null || !_engine.CanRemember(w.Id))
                    continue;
                if (!lasting)
                    saved++;
                list.Add(w);
                if (list.Count >= 12)
                    break;
            }
            return list;
        }

        /// One of the player's deeds: those around with a clear view of him remember it. For violence a few passers-by
        /// are kept as residents, as for a fight. `only`: just this one (someone who came upon the body and saw him).
        private void NoteDeedMinds(Deed d, Vector3 pos, int except, Ped only = null, int victim = 0)
        {
            // the plain record of it first, Minds or not (WorldEvents.cs); one who came upon the body adds no new event.
            // `victim`: the living person it was done to, when the game knows (an assault): their id goes on the deed and
            // the record, so the mind gives them their own part; never a witness of it
            if (d != null && only == null && !MISC.GET_MISSION_FLAG())
                Safe("world events", () =>
                {
                    string vid = null, vname = d.Detail;
                    if (victim != 0 && ENTITY.DOES_ENTITY_EXIST(victim) && !ENTITY.IS_ENTITY_DEAD(victim))
                    {
                        string id, name;
                        if (LastingId(new Ped(victim), out id, out name) && _engine.CanRemember(id))
                        {
                            vid = id;
                            vname = name != null ? Person.FirstOf(name) : d.Detail;
                            if (d.VictimId == null)
                                d.VictimId = id;
                        }
                    }
                    RecordEvent("deed:" + d.Kind, "player", "you", vid, vname, pos, Game.Player.Character.Handle, except, victim, d.Unseen ? "unseen" : null);
                });
            if (!_cfg.Minds || d == null || Minds.DeedGroup(d.Kind) == null || MISC.GET_MISSION_FLAG())
                return;   // not during a story mission: its people are the story's
            Safe("minds", () => NoteDeedMindsImpl(d, pos, except, only));
        }

        private void NoteDeedMindsImpl(Deed d, Vector3 pos, int except, Ped only)
        {
            Ped me = Game.Player.Character;
            bool strong = Minds.DeedGroup(d.Kind) == "deed:violence";
            var list = new List<Minds.Party>();
            int saved = 0;
            IEnumerable<Ped> around = only != null ? (IEnumerable<Ped>)new[] { only } : World.GetAllPeds().Where(x => x != null && x.Exists() && x.Position.DistanceTo(pos) <= 35f).OrderBy(x => x.Position.DistanceTo(pos));
            foreach (Ped p in around)
            {
                int h = p.Handle;
                if (h == except || !CanWitness(p, me) || !ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(h, me.Handle, 17))
                    continue;
                string id, name;
                bool lasting = LastingId(p, out id, out name) && _engine.CanRemember(id);
                if (!lasting && !(strong && saved < NewMindsPerEvent))
                    continue;
                Minds.Party w = PartyOf(p, !lasting);
                if (w == null || w.Id == null || !_engine.CanRemember(w.Id))
                    continue;
                if (!lasting)
                    saved++;
                list.Add(w);
                if (list.Count >= 12)
                    break;
            }
            if (list.Count > 0)
                _engine.RememberDeed(d, list, _engine.NowIndoors ? _engine.NowPlace : null);
        }

        /// A death the ledger noted: whoever had a clear view of the body within 25 m remembers seeing it (a few
        /// passers-by kept, as for a fight). The player's own killings are remembered as his deeds instead.
        private void NoteDeathMinds(int h, DeathEntry e)
        {
            if (e != null && !MISC.GET_MISSION_FLAG())
                Safe("world events", () => RecordEvent("death", e.By == "player" ? "player" : e.ById, e.By == "player" ? "you" : e.By == "self" ? "their own hand" : e.By == "another" ? "someone" : null,
                                                       e.Id, e.Name, new Vector3(e.X, e.Y, e.Z), h, h, 0, e.How));
            if (!_cfg.Minds || e == null || e.By == "player" || MISC.GET_MISSION_FLAG())
                return;
            Safe("minds", () =>
            {
                List<Minds.Party> watchers = MindWatchers(new List<Ped> { new Ped(h) }, 25f, true);
                if (watchers.Count > 0)
                    _engine.RememberDeathSaw(e, watchers);
            });
        }

        /// For the F7 dump: what is on the minds of the nearest people with a name, strongest first.
        private string MindsStatus()
        {
            if (!_cfg.Minds)
                return "off ([Mind] Minds)";
            Ped me = Game.Player.Character;
            int hour = CLOCK.GET_CLOCK_HOURS();
            var parts = new List<string>();
            foreach (Ped p in World.GetAllPeds().Where(x => x != null && x.Exists() && !x.IsDead && x.Handle != me.Handle && x.Position.DistanceTo(me.Position) < 25f)
                                                 .OrderBy(x => x.Position.DistanceTo(me.Position)))
            {
                Person person = null;
                FolkRef ac = _bridge.AcquaintanceAt(p.Handle);
                ResBind b;
                if (ac != null && !string.IsNullOrEmpty(ac.Name))
                {
                    Folk f = Snapshot.Of(ac);
                    person = _engine.PersonOfFolk(f.Key, f.Name, f.Woman, f.Town, Bridge.Now());
                    if (person.Personality == null)
                        person.Personality = f.Temper;
                }
                else if (_resBind.TryGetValue(p.Handle, out b) && !b.R.Dead)
                    person = _engine.PersonOfResident(b.R);
                if (person == null)
                    continue;
                parts.Add((int)p.Position.DistanceTo(me.Position) + " m, " + _engine.MindStatus(person, hour, person.Id + "|f7", null));
                if (parts.Count >= 3)
                    break;
            }
            return parts.Count == 0 ? "nobody with a name within 25 m" : string.Join(" | ", parts);
        }
    }
}
