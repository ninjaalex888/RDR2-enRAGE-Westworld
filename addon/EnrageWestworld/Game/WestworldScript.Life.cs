// Lives in motion around the player: scenes between strangers (and loose talk that gives away gang jobs),
// weddings at the church, mourners at enRAGE's funerals, kin met on the street in their own town, and the
// engine's requests that need the game: waking the dead, a relative riding in, a newspaper item.
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
        private int _nextDrama = -1, _nextCeremony, _nextCast;
        private PlanInfo _sceneJob;
        private readonly HashSet<string> _funeralsHeard = new HashSet<string>();
        private readonly Dictionary<int, KeyValuePair<Relative, string>> _casts = new Dictionary<int, KeyValuePair<Relative, string>>();   // the body walking up: whose kin
        private WeddingRun _wedding;

        private static readonly Dictionary<string, Vector3> Churches = new Dictionary<string, Vector3>
        {
            { "Valentine", new Vector3(-238f, 822f, 123.5f) }, { "Rhodes", new Vector3(1285f, -1225f, 81f) }, { "Blackwater", new Vector3(-997f, -1195f, 58.7f) },
            { "Strawberry", new Vector3(-1761f, -241.5f, 182.6f) }, { "Armadillo", new Vector3(-3326f, -2866f, -6f) }, { "Van Horn", new Vector3(2900f, 495f, 66f) }
        };

        // ---- scenes between strangers -----------------------------------------------------------------

        private void DramaTick(int now)
        {
            Ped me = Game.Player.Character;
            FallWanderStep(now);
            WanderLaterStep(now);
            PatientStep(now);   // the doctor's patient on the way there and back (WestworldScript.Patient.cs)
            ThrownOutStep(now);   // the card cheat on the boardwalk
            SabbathWatchStep(now);   // the one who saw the kneel thinks of it, a few seconds on
            if (_director.Busy)
            {
                FallWatchStep(me, now);   // the player may give the fallen a hand up first (WestworldScript.Kindness.cs)
                if (_director.Busy)
                    _director.Tick(me, SceneSay);
                return;
            }
            _fallWatch = null;
            if (_director.Custom == null)
                _director.Custom = SceneAction;
            if (_director.Slam == null)
            {
                // the rough move for the Director's "slam" (WestworldScript.Roughs.cs)
                _director.Slam = RoughForScene;
                _director.SlamGoing = () => _rough != null && _rough.ForScene;
                _director.SlamResult = () => _roughResult;
                _director.React = OnReaction;   // WestworldScript.Reactions.cs
                // a fight that never came to anything (the walker never got there) sits nobody out for long
                _director.NeverMet = (one, other) =>
                {
                    _restMsOf.Remove(one.Handle);
                    _restMsOf.Remove(other.Handle);
                };
                _director.SlamStop = () =>
                {
                    if (_rough != null && _rough.ForScene)
                        RoughEnd(_rough, "stopped: the scene ended", _now);
                };
                _director.Happened = what =>
                {
                    // the trouble scenes leave their tie (and the memory of it) only once it has really happened
                    Pending p = _pendingNote;
                    _pendingNote = null;
                    if (p != null)
                        NoteHappened(p, what == "shove" ? "argue" : p.S.Kind);
                };
            }
            if (_nextDrama < 0)
                _nextDrama = now + 45000;
            if (!_cfg.Drama || now < _nextDrama || Feed.Busy || _stalker != null || _wedding != null || _theft != null || _plea != null || _visitor != null)
                return;   // (and never gossip beside a figure crouched over a corpse)
            _nextDrama = now + 8000;
            bool saloon = _placeKind == "saloon";
            if (_interiorType != 0 && !saloon)
                return;
            string fallWhy;
            if (_interiorType == 0 && PreacherScene(me, now, false, out fallWhy))
                return;   // a Sunday afternoon's street preacher (WestworldScript.Preacher.cs)
            if (_interiorType == 0 && LostChildScene(me, now, false, out fallWhy))
                return;   // a mother asking around for her child (WestworldScript.LostChild.cs)
            if (_interiorType == 0 && PatientScene(me, now, false, out fallWhy))
                return;   // someone walks to the doctor for a child's fever (WestworldScript.Patient.cs)
            if (_interiorType == 0 && SabbathScene(me, now, false, out fallWhy))
                return;   // a Sunday morning's kneel by the church (WestworldScript.Sabbath.cs)
            if (_interiorType == 0 && BellScene(me, now, false, out fallWhy))
                return;   // the bell at three that only the awake hear (WestworldScript.Bell.cs)
            if (_interiorType == 0 && FallScene(me, now, false, out fallWhy))
                return;   // someone took a fall (WestworldScript.Kindness.cs)
            if (saloon && RoundScene(me, now, false, out fallWhy))
                return;   // someone stands the house a drink (WestworldScript.Round.cs)
            if (saloon && DrinkScene(me, now, false, out fallWhy))
                return;   // someone drinks away bad news (WestworldScript.Vices.cs)
            if (GrudgeScene(me, now, false, out fallWhy))
                return;   // a friend takes up a wrong (WestworldScript.Grudges.cs)
            if (_interiorType == 0 && CalloutScene(me, now, false, out fallWhy))
                return;   // a man calls another out at noon (WestworldScript.Callout.cs)
            List<Ped> pool = World.GetAllPeds().Where(p => Natives.Bystander(p, me.Handle) && !Spawner.IsOurs(p.Handle) && _bridge.RecordAt(p.Handle) == null && PostKindOf(p) == null &&
                                                         !PED.IS_PED_IN_ANY_VEHICLE(p.Handle, false) && !PED.IS_PED_ON_MOUNT(p.Handle) &&
                                                         ENTITY.GET_ENTITY_SPEED(p.Handle) < 0.8f && p.Position.DistanceTo(me.Position) < 22f &&
                                                         !Resting(p.Handle, now)).ToList();
            if (pool.Count >= 2 && now >= _nextSpecial && SpecialScene(pool, me, saloon, now))
                return;
            // first, two people near you who have a reason to have something to do with each other (Reasons.cs)
            if (ReasonScene(me, saloon, now))
                return;
            // nobody with a reason: now and then a chance meeting between the two standing closest, where you can see
            if (pool.Count < 2 || now < _nextChance)
                return;
            Ped a = null, b = null;
            float best = 3.4f;
            for (int i = 0; i < pool.Count; i++)
                for (int j = i + 1; j < pool.Count; j++)
                {
                    float d = pool[i].Position.DistanceTo(pool[j].Position);
                    if (d < best)
                    {
                        best = d;
                        a = pool[i];
                        b = pool[j];
                    }
                }
            if (a == null || !(ENTITY.IS_ENTITY_ON_SCREEN(a.Handle) || ENTITY.IS_ENTITY_ON_SCREEN(b.Handle)))
                return;
            _nextChance = now + (int)(_cfg.DramaGapSeconds * 1500);   // was x3 (Lex, 2026-10-29: the chance meetings carry the lively kinds: songs, tales, haggling, small talk)
            // the older one tells the tales
            if (Director.OldModel(b) && !Director.OldModel(a))
            {
                Ped x = a;
                a = b;
                b = x;
            }
            string seed = a.Handle + "|" + b.Handle + "|" + now / 60000;
            Ped crowd = pool.Where(p => p != a && p != b && p.Position.DistanceTo(a.Position) < 9f).OrderBy(p => p.Position.DistanceTo(a.Position)).FirstOrDefault();
            SceneCast cast = BuildCast(a, b, crowd, saloon, me, now, seed);
            cast.Off.Add("debt");   // debts come from people's own worries (Links, Reasons), never from a chance meeting
            Scene s = null;
            List<Ped> people = new List<Ped> { a, b };
            if (!cast.AWoman && cast.BWoman || cast.AWoman && !cast.BWoman)
            {
                Ped man = cast.AWoman ? b : a;
                Ped c = pool.FirstOrDefault(p => p != a && p != b && PED.IS_PED_MALE(p.Handle) && p.Position.DistanceTo(a.Position) < 15f);
                if (c != null && _rng.Next(100) < 12)
                {
                    Ped woman = cast.AWoman ? a : b;
                    people = new List<Ped> { man, woman, c };
                    cast.AWoman = false;
                    cast.BWoman = true;
                    cast.CWoman = false;
                    cast.HasCrowd = false;
                    cast.Names = SceneNames(man, woman, c);   // in the new order
                    s = Scenes.Jealous(cast, seed);
                }
            }
            if (s == null && !cast.AWoman && !cast.BWoman && Director.OldModel(a) && Director.OldModel(b) && !cast.Off.Contains("smalltalk") && Voice.Chance(seed + "|olddays", 45))
            {
                s = Scenes.OldDays(cast, seed);   // two old men: the old days (Core/Scenes.OldDays.cs)
                _oldDays++;
                Log.Write("OLD DAYS " + (cast.Names[0] ?? "an old man") + " & " + (cast.Names[1] ?? "another") + ": " + s.Beats[0].Line);
            }
            if (s == null)
            {
                s = Scenes.Choose(cast, seed, true);
                if (s != null && s.Kind == "hosts" && cast.HostTier < 0)
                    s = Scenes.Argue(cast, seed);
                if (s != null && s.Kind == "debt" && !_cfg.Debts)
                    s = Scenes.Argue(cast, seed);
                if (s != null && s.Kind == "pickpocket" && (cast.AWoman || _theft != null))
                    s = Scenes.SmallTalk(cast, seed);
                if (s != null && s.Cast > 2 && crowd != null)
                    people.Add(crowd);
            }
            if (s == null)
                return;
            if (s.Kind == "gossip" && saloon && _cfg.JobRumours)
                AddJobTalk(s, seed);
            s = LadderScene(s, a, b, null, null, cast, seed, null, people, now);   // a chance meeting's argue or brawl from the quarrel ladder, behind MindLive (WestworldScript.Ladder.cs)
            AddBarkeep(s, people, cast, seed, saloon);
            string stood;
            if (s.Ladder == null || s.Cast == 2)
                StandUpFor(s, people, cast, seed, now, false, out stood);
            AddBystander(s, people, cast, seed, now);
            StartScene(s, people, cast, seed, now);
        }

        private sealed class Pending
        {
            public Scene S;
            public List<Ped> People;
            public string[] Names;
        }

        private Pending _pendingNote;   // a trouble scene's tie, waiting for the moment it really happens (Director.Happened)

        private readonly Dictionary<string, int> _courtCount = new Dictionary<string, int>();
        private string _courtLast;

        /// A courtship began: who was taken with whom, and how it was taken (the scene follows; the tie it leaves is a
        /// romance for a yes or a maybe, a brush-off for a no).
        private void NoteCourt(Someone suitor, Someone other, string answer, string why)
        {
            int c;
            _courtCount.TryGetValue(answer, out c);
            _courtCount[answer] = c + 1;
            _courtLast = suitor.Name + " -> " + other.Name + ": " + answer + " (" + why + ")";
            Log.Write("ROMANCE " + suitor.Name + " (" + (suitor.Temper ?? "no temper") + ") makes eyes at " + other.Name + " (" + (other.Temper ?? "no temper") +
                      (other.Mood != null ? ", " + other.Mood + " today" : "") + "): " + answer + ", " + why);
        }

        private sealed class LaterThought
        {
            public int At, Handle;
            public string Who, Text;
        }

        private readonly List<LaterThought> _courtThoughts = new List<LaterThought>();
        private int _courtShown, _courtUnseen;

        /// A courtship you can see and that lasts: the one asking thinks as they go to it, the other as they answer
        /// (on the ticker, only while they're in view: overheard talk has no subtitle, so this is how you know); a yes
        /// or a maybe keeps any of the add-on's own people in it, so the romance is remembered and their thoughts turn
        /// to it.
        private void CourtSeen(Ped asker, Ped other, Someone a, Someone b, string answer, string why, bool apart, string seed)
        {
            int now = Game.GameTime;
            _courtThoughts.Add(new LaterThought { At = now, Handle = asker.Handle, Who = a.Name, Text = Scenes.CourtThought(true, answer, why, Person.FirstOf(b.Name), seed) });
            _courtThoughts.Add(new LaterThought { At = now + (apart ? 12000 : 7000), Handle = other.Handle, Who = b.Name, Text = Scenes.CourtThought(false, answer, why, Person.FirstOf(a.Name), seed) });
            if (answer == "no")
                return;
            foreach (Ped p in new[] { asker, other })
            {
                ResBind rb;
                if (p != null && _resBind.TryGetValue(p.Handle, out rb) && !rb.R.Dead && !_store.Residents.Contains(rb.R))
                {
                    _engine.KeepResident(rb.R, Bridge.Now());
                    Log.Write("ROMANCE " + rb.R.Name + " is kept now, so the romance is remembered");
                }
            }
        }

        /// The courtship thoughts that are due, on the ticker if the one thinking is in plain view.
        private void CourtThoughtsDue(int now)
        {
            int player = Game.Player.Character.Handle;
            for (int i = _courtThoughts.Count - 1; i >= 0; i--)
            {
                LaterThought t = _courtThoughts[i];
                if (now < t.At)
                    continue;
                _courtThoughts.RemoveAt(i);
                if (now - t.At > 15000 || !ENTITY.DOES_ENTITY_EXIST(t.Handle) || !ENTITY.IS_ENTITY_ON_SCREEN(t.Handle) || !ENTITY.HAS_ENTITY_CLEAR_LOS_TO_ENTITY(player, t.Handle, 17))
                {
                    _courtUnseen++;
                    Log.Detail("ROMANCE thought not shown (" + t.Who + " out of view): " + t.Text);
                    continue;
                }
                _courtShown++;
                _ticker.Push(t.Who + ", thinking", t.Text, "near", t.Handle);   // the gold mark goes over them
                Log.Write("ROMANCE thought, " + t.Who + ": " + t.Text);
            }
        }

        /// F7: courtships this session by answer, the last, and the romances going on in this town.
        private string RomanceStatus()
        {
            if (!_cfg.NpcFlirting || !_cfg.Romance)
                return "off ([Romance] NpcFlirting)";
            int here = _store.Ties.Count(t => t.Town == _placeTown && Ties.Sweethearts(t) && t.Warmth > 0);
            int odds = _store.Ties.Count(t => t.Town == _placeTown && Ties.Cooled(t));
            return "this session: " + (_courtCount.Count == 0 ? "none yet" : string.Join(", ", _courtCount.Select(kv => kv.Key + " " + kv.Value))) +
                   (_courtLast != null ? "; last: " + _courtLast : "") + "; thoughts on the ticker " + _courtShown + ", out of view " + _courtUnseen +
                   "; romances in " + (_placeTown ?? "this place") + ": " + here + ", at odds " + odds +
                   "; sweethearts' words " + _tiffs + ", made up " + _mendsTaken + ", gifts turned down " + _mendsRefused + (_loversLast != null ? " (last: " + _loversLast + ")" : "");
        }

        private int _tiffs, _mendsTaken, _mendsRefused, _mendTurn, _condoles;
        private string _condoleLast;
        private string _loversLast;

        private static readonly HashSet<string> TroubleKinds = new HashSet<string> { "rough", "lift", "pickpocket", "drunk", "charity", "scorn", "avoid", "comfort" };
        private readonly Dictionary<string, int> _troubleCount = new Dictionary<string, int>();
        private string _troubleLast;

        /// F7: the trouble scenes' settings, how many of each played this session, and the last.
        private string TroubleStatus()
        {
            return "rough " + (_cfg.RoughHandling ? "on" : "off") + ", thieves " + (_cfg.NpcThieves ? "on" : "off") + ", drunks " + (_cfg.DrunkChallenges ? "on" : "off") +
                   "; this session: " + (_troubleCount.Count == 0 ? "none yet" : string.Join(", ", _troubleCount.Select(kv => kv.Key + " " + kv.Value))) +
                   (_troubleLast != null ? "; last: " + _troubleLast : "");
        }

        /// F7: what the thoughts of the people near can turn to now (InnerLife.Now.cs): the weather, the place, a scene
        /// just seen, and who's in sight.
        private string NowStatus()
        {
            long g = _store.LastGameMin;
            InnerLife.Witnessed seen = _engine.LastScene;
            return "weather " + (_engine.NowWeather ?? "unknown") + (_engine.NowIndoors ? ", indoors" : "") + ", place " + (_engine.NowPlace ?? "the street") +
                   "; scene just seen: " + (seen != null && g - seen.At >= 0 && g - seen.At < Engine.SeenForMinutes ? seen.Kind + " in " + seen.Town + ", " + (g - seen.At) + " game min ago" : "none") +
                   "; in sight: " + (_engine.Nearby != null ? _engine.Nearby.Count : 0) + " (noted " + (g - _engine.NearbyAt) + " game min ago)";
        }

        private void StartScene(Scene s, List<Ped> people, SceneCast cast, string seed, int now, bool test = false)
        {
            // the labels over the lines say the names the lines were written with (cast.Names, set by BuildCast)
            string[] names = cast.Names ?? new string[0];
            if (_director.Start(s, people, names, seed, () => { }, cast))
            {
                _nextDrama = now + (int)(_cfg.DramaGapSeconds * 1000 * (0.75 + _rng.NextDouble() * 0.5));
                if (TroubleKinds.Contains(s.Kind))
                {
                    int c;
                    _troubleCount.TryGetValue(s.Kind, out c);
                    _troubleCount[s.Kind] = c + 1;
                    _troubleLast = s.Kind + " (" + string.Join(" & ", names.Where(x => x != null)) + ")" + (test ? ", a test" : "");
                }
                var p = new Pending { S = s, People = new List<Ped>(people), Names = names };
                // a ladder quarrel (Scenes.Ladder.cs) is noted by what happened in it, not its top rung: a standoff that came to
                // blows first is a brawl to the ties, the memories, the rests and the ledger; an argue that ended in an apology
                // or a drink stood is an apology (the review)
                string noted = s.Ladder == null ? s.Kind : s.Fight && s.Kind == "standoff" ? "brawl"
                             : s.Kind == "argue" && s.Ladder.Count > 0 && (s.Ladder[s.Ladder.Count - 1].Ended == "apology" || s.Ladder[s.Ladder.Count - 1].Ended == "drink") ? "apology" : s.Kind;
                // a face in the bar, a drunk on the floor, a purse gone: noted only once it happens (a walk-over that
                // never arrives leaves nothing); any other scene counts as soon as it starts, broken up or not
                _pendingNote = !test && (s.Kind == "rough" || s.Kind == "drunk" || s.Kind == "lift" || s.Kind == "help" || s.Kind == "passby" || s.NoteAtCue) ? p : null;
                if (_pendingNote == null && !test)
                    NoteHappened(p, noted);   // a test leaves no tie between them
                NoteInScene(people, noted, now);
                Onlookers(people);
            }
        }

        /// What a scene leaves: the tie between its two (kind may differ from the scene's: a rough that came to a
        /// shove is a quarrel), and the memory of it for everyone around (never a lift, which nobody saw).
        private void NoteHappened(Pending p, string kind)
        {
            NoteSceneTie(kind, p.People, p.S != null ? p.S.Doer : -1);
            NoteMinds(kind, p.People, p.Names);   // what they and those watching remember of it (Minds)
            Safe("world events", () => RecordSceneEvent(kind, p.People));   // and the plain record, with its witnesses (WorldEvents.cs)
            if (kind != "lift" && p.People.Count >= 2)
            {
                string aId, aName, bId, bName;
                LastingId(p.People[0], out aId, out aName);
                LastingId(p.People[1], out bId, out bName);
                _engine.SawScene(kind, p.Names.Length > 0 && p.Names[0] != null ? p.Names[0] : aName, p.Names.Length > 1 && p.Names[1] != null ? p.Names[1] : bName, aId, bId, _placeTown);
            }
        }

        /// What the scene leaves between its two people (Ties.cs), when both can be met again: a town regular, a
        /// resident saved for something else, or one of enRAGE's townsfolk. Nobody is saved just for this.
        private void NoteSceneTie(string kind, List<Ped> people, int doer = -1)
        {
            if (people.Count < 2 || Ties.Effect(kind) == null)
                return;
            string aId, aName, bId, bName;
            if (!LastingId(people[0], out aId, out aName) || !LastingId(people[1], out bId, out bName))
                return;
            // who did it to whom: the tie's own rule (Ties.Trouble: A), or the scene's word when its shape says (Scene.Doer: the
            // creditor who called the debt in, the one who brushed the other off), so the appraisal can read it (item 17)
            SceneTie t = doer == 0 || doer == 1 ? Ties.Note(_store.Ties, aId, aName, bId, bName, _placeTown, kind, Bridge.Now(), doer == 0 ? aId : bId)
                       : Ties.Note(_store.Ties, aId, aName, bId, bName, _placeTown, kind, Bridge.Now());
            if (t != null)
                Log.Write("TIE " + t.AName + " & " + t.BName + ": " + kind + " (" + (t.Warmth > 0 ? "warm" : t.Warmth < 0 ? "hostile" : "acquainted") + ", " +
                          t.Count + " scene" + (t.Count == 1 ? "" : "s") + " between them)");
        }

        private bool LastingId(Ped p, out string id, out string name)
        {
            string temper;
            return LastingId(p, out id, out name, out temper);
        }

        /// ...and their temperament, for those who need it (acknowledgement in passing).
        private bool LastingId(Ped p, out string id, out string name, out string temper)
        {
            id = name = temper = null;
            if (p == null || !p.Exists())
                return false;
            RecordRef rec = _bridge.RecordAt(p.Handle);
            if (rec != null && rec.IsAlive)
            {
                Person rp = PersonOf(rec);   // enRAGE's people are met again by their record
                id = rp.Id;
                name = rp.Name;
                temper = rp.Personality;
                return true;
            }
            FolkRef ac = _bridge.AcquaintanceAt(p.Handle);
            if (ac != null && !string.IsNullOrEmpty(ac.Name))
            {
                Folk f = Snapshot.Of(ac);
                id = f.Key;
                name = f.Name;
                temper = f.Temper;
                return true;
            }
            ResBind b;
            if (_resBind.TryGetValue(p.Handle, out b) && !b.R.Dead && (b.R.Regular || _store.Residents.Contains(b.R)))
            {
                id = b.R.Id;
                name = b.R.Name;
                temper = b.R.Personality;
                return true;
            }
            return false;
        }

        /// The latest ties between people, for the F7 dump.
        private string TiesStatus()
        {
            if (_store.Ties.Count == 0)
                return "none yet";
            long now = Bridge.Now();
            return _store.Ties.Count + " kept; latest: " + string.Join("; ", _store.Ties.OrderByDescending(t => t.At).Take(5)
                .Select(t => t.AName + " & " + t.BName + " (" + t.Kind + ", warmth " + t.Warmth + ", " + t.Count + "x, " + Math.Max(0, now - t.At) / 60 + " h ago, " + (t.Town ?? "?") + ")"));
        }

        /// The first names the people in a scene go by, in the order the scene is built with: enRAGE's name for one
        /// of its townsfolk, a resident's own (made on the spot if need be); null for anyone else, whom the scene
        /// names itself. Two with the same first name: the later one goes by their surname.
        private string[] SceneNames(Ped a, Ped b, Ped c)
        {
            string[] full = { FullNameOf(a), FullNameOf(b), FullNameOf(c) };
            string[] first = full.Select(n => n == null ? null : Person.FirstOf(n)).ToArray();
            for (int i = 1; i < 3; i++)
                for (int j = 0; j < i; j++)
                    if (first[i] != null && first[i] == first[j])
                        first[i] = full[i].Split(' ').Last();
            return first;
        }

        private string FullNameOf(Ped p)
        {
            if (p == null || !p.Exists())
                return null;
            RecordRef rec = _bridge.RecordAt(p.Handle);
            if (rec != null && !string.IsNullOrEmpty(rec.Name))
                return ShownName(p.Handle, rec.Name);
            FolkRef ac = _bridge.AcquaintanceAt(p.Handle);
            if (ac != null && !string.IsNullOrEmpty(ac.Name))
                return ac.Name;
            Resident r = ResidentEligible(p) ? ResidentOf(p) : null;
            return r != null ? ShownName(p.Handle, r.Name) : null;
        }

        /// Whether the others in a scene know each person by name: a tie between them, or the same home town (a resident
        /// of this town to another, one of enRAGE's townsfolk of this town to another). Someone nobody knows is spoken to
        /// as "mister" or "ma'am" (night 2026-10-28, item 10). A cast of one known person is nobody's stranger.
        private bool[] CastKnows(Ped a, Ped b, Ped crowd)
        {
            var peds = new[] { a, b, crowd };
            var ids = new string[3];
            var homes = new string[3];
            for (int i = 0; i < 3; i++)
            {
                if (peds[i] == null || !peds[i].Exists())
                    continue;
                string id, name;
                if (LastingId(peds[i], out id, out name))
                    ids[i] = id;
                homes[i] = CastHome(peds[i]);
            }
            var knows = new bool[3];
            for (int i = 0; i < 3; i++)
            {
                if (peds[i] == null)
                    continue;
                for (int j = 0; j < 3 && !knows[i]; j++)
                {
                    if (j == i || peds[j] == null)
                        continue;
                    if (homes[i] != null && homes[i] == homes[j])
                        knows[i] = true;
                    else if (ids[i] != null && ids[j] != null)
                        foreach (SceneTie t in _store.Ties)
                            if (t.Has(ids[i]) && t.Has(ids[j]))
                            {
                                knows[i] = true;
                                break;
                            }
                }
            }
            return knows;
        }

        /// The ties' warmth between the cast (A and B, A and C, B and C), read once from the store for the reactions' appraisal.
        private void CastWarmth(Ped a, Ped b, Ped crowd, SceneCast cast)
        {
            var peds = new[] { a, b, crowd };
            var ids = new string[3];
            for (int i = 0; i < 3; i++)
            {
                string id, name;
                if (peds[i] != null && peds[i].Exists() && LastingId(peds[i], out id, out name))
                    ids[i] = id;
            }
            cast.Warmth = new int[3];
            cast.Tied = new bool[3];
            int[][] pairs = { new[] { 0, 1 }, new[] { 0, 2 }, new[] { 1, 2 } };
            for (int k = 0; k < 3; k++)
            {
                string x = ids[pairs[k][0]], y = ids[pairs[k][1]];
                if (x == null || y == null)
                    continue;
                foreach (SceneTie t in _store.Ties)
                    if (t.Has(x) && t.Has(y))
                    {
                        cast.Warmth[k] = t.Warmth;
                        cast.Tied[k] = true;
                        break;
                    }
            }
        }

        /// The town someone is of: a bound resident's, or one of enRAGE's townsfolk's; null for a record or a stranger.
        private string CastHome(Ped p)
        {
            ResBind rb;
            if (_resBind.TryGetValue(p.Handle, out rb) && rb.R != null && !rb.R.Dead)
                return rb.R.Town;
            FolkRef ac = _bridge.AcquaintanceAt(p.Handle);
            return ac != null ? ac.Town : null;
        }

        /// Everything a scene might use about these people and this place.
        private SceneCast BuildCast(Ped a, Ped b, Ped crowd, bool saloon, Ped me, int now, string seed)
        {
            long g = Bridge.Now();
            SceneCast cast = new SceneCast
            {
                AWoman = !PED.IS_PED_MALE(a.Handle), BWoman = !PED.IS_PED_MALE(b.Handle), Town = _placeTown, Saloon = saloon, Night = Natives.Night(), Trouble = _cfg.TroubleWeight,
                HostTier = TownHostTier(), HasCrowd = crowd != null, CWoman = crowd != null && !PED.IS_PED_MALE(crowd.Handle), OldTimer = Director.OldModel(a),
                PlayerNear = a.Position.DistanceTo(me.Position) < 12f,
                Names = SceneNames(a, b, crowd),
                Tempers = new[] { CastTemper(a), CastTemper(b), crowd != null ? CastTemper(crowd) : null }
            };
            cast.Knows = CastKnows(a, b, crowd);
            CastWarmth(a, b, crowd, cast);
            if (!saloon || _cfg.SaloonGossip)
                cast.Rumours.AddRange(_engine.RumourPool(_placeTown, 4));
            // guns come out in a standoff only when both carry them and no lawman is close enough to start shooting
            bool lawNear = World.GetAllPeds().Any(p => p.Exists() && !ENTITY.IS_ENTITY_DEAD(p.Handle) && PED.IS_PED_HUMAN(p.Handle) && p.Position.DistanceTo(a.Position) < 45f && Natives.IsLaw(p));
            bool armedA = WEAPON.IS_PED_ARMED(a.Handle, 4), armedB = WEAPON.IS_PED_ARMED(b.Handle, 4);
            cast.Armed0 = cast.Armed1 = armedA && armedB && !lawNear && !saloon;
            cast.Legend = _engine.Legend(g, seed);
            cast.PaperItems.AddRange(_engine.PaperItems(g, 3));
            if (!_cfg.Standoffs) cast.Off.Add("standoff");
            if (!_cfg.Songs) cast.Off.Add("song");
            if (!_cfg.TallTales) cast.Off.Add("talltale");
            if (!_cfg.NewspaperTalk) cast.Off.Add("paper");
            if (!_cfg.Haggling) cast.Off.Add("haggle");
            if (!_cfg.SnakeOil) cast.Off.Add("pitch");
            if (!_cfg.Pickpockets) cast.Off.Add("pickpocket");
            if (!_cfg.SmallTalk) cast.Off.Add("smalltalk");
            return cast;
        }

        /// A widow grieving for a man the player shot here, or men getting up a posse over what he did.
        private bool SpecialScene(List<Ped> pool, Ped me, bool saloon, int now)
        {
            _nextSpecial = now + 30000;
            long g = Bridge.Now();
            if (_cfg.Mourners && !saloon)
            {
                Fallen f = _engine.MournFor(_placeTown, g);
                if (f != null)
                {
                    Ped mourner = pool.Where(p => f.Woman || !PED.IS_PED_MALE(p.Handle)).OrderBy(p => p.Position.DistanceTo(me.Position)).FirstOrDefault();
                    Ped friend = mourner == null ? null : pool.Where(p => p != mourner && p.Position.DistanceTo(mourner.Position) < 6f).OrderBy(p => p.Position.DistanceTo(mourner.Position)).FirstOrDefault();
                    if (mourner != null && friend != null)
                    {
                        if (string.IsNullOrEmpty(f.Name))
                            f.Name = Scenes.FirstName(f.Woman, f.Id);   // from now on, the dead have a name
                        f.Mourned = true;
                        string seed = f.Id + "|mourn";
                        SceneCast cast = BuildCast(mourner, friend, null, saloon, me, now, seed);
                        cast.DeadName = f.Name;
                        cast.DeadLabel = f.Label;
                        cast.DeadWoman = f.Woman;
                        cast.DeadSeen = f.Seen > 0;
                        cast.DeadWillReturn = f.ReturnAt > 0;
                        cast.PlayerNear = mourner.Position.DistanceTo(me.Position) < 14f;
                        Scene s = Scenes.Mourn(cast, seed);
                        Log.Write("WIDOW scene for " + f.Name + " (" + (f.Label ?? "a stranger") + ", killed by you in " + f.Town + ")");
                        StartScene(s, new List<Ped> { mourner, friend }, cast, seed, now);
                        return true;
                    }
                }
            }
            if (_cfg.PosseTalk)
            {
                Deed d = _engine.RecentCrime(_placeTown, g);
                if (d != null && !_posseFor.Contains(d.Id))
                {
                    List<Ped> men = pool.Where(p => PED.IS_PED_MALE(p.Handle)).ToList();
                    Ped a = null, b = null;
                    float best = 4f;
                    for (int i = 0; i < men.Count; i++)
                        for (int j = i + 1; j < men.Count; j++)
                        {
                            float dist = men[i].Position.DistanceTo(men[j].Position);
                            if (dist < best)
                            {
                                best = dist;
                                a = men[i];
                                b = men[j];
                            }
                        }
                    if (a != null)
                    {
                        _posseFor.Add(d.Id);
                        string seed = d.Id + "|posse";
                        SceneCast cast = BuildCast(a, b, null, saloon, me, now, seed);
                        cast.CrimeRumour = Deeds.Rumour(d, d.Witnesses > 0 ? "a stranger" : "somebody");
                        cast.CrimeDesc = _engine.CrimeDesc(d);
                        cast.CrimeSeen = d.Witnesses > 0;
                        Scene s = Scenes.Posse(cast, seed);
                        Log.Write("POSSE talk in " + _placeTown + " over: " + cast.CrimeRumour);
                        StartScene(s, new List<Ped> { a, b }, cast, seed, now);
                        return true;
                    }
                }
            }
            return false;
        }

        private void SceneSay(string label, string line, bool uncanny, bool woman, int ped)
        {
            Feed.Say(label, line, null, ped);   // the director plays the speakers' own recorded lines
            // and on the ticker as it is said, while the scene is within earshot (Lex, 2026-10-29: he couldn't tell a woman had stepped in)
            if (_cfg.ScenesOnTicker && _cfg.Ticker && ped != 0 && ENTITY.DOES_ENTITY_EXIST(ped) && ENTITY.GET_ENTITY_COORDS(ped, true, true).DistanceTo(Game.Player.Character.Position) <= _cfg.ScenesOnTickerMetres)
                SpeechCaption(label, line, ped, Math.Min(7000, 1600 + line.Length * 55));
        }

        /// Loose talk in a saloon about a gang job nobody's supposed to know about.
        private void AddJobTalk(Scene s, string seed)
        {
            if (!_bridge.CanTellPlans || _rng.Next(100) >= 45)
                return;
            long now = Bridge.Now();
            PlanInfo job = _bridge.Plans().Where(p => p.Kind == "job" && !p.Known && !p.Done && p.DueAt > now && p.Gang != null && !string.IsNullOrEmpty(p.Title))
                .OrderBy(p => _placeTown != null && Rumours.Towns.ContainsKey(_placeTown) ? new Vector3(p.X, p.Y, 0f).DistanceTo(new Vector3(Rumours.Towns[_placeTown][0], Rumours.Towns[_placeTown][1], 0f)) : 0f)
                .FirstOrDefault();
            if (job == null)
                return;
            _sceneJob = job;
            string gang = _bridge.GangName(job.Gang) ?? "some outfit";
            string when = _bridge.WhenText(job.DueAt);
            string what = Grammar.LowerFirst(job.Title.TrimEnd('.'));
            s.Add(0, "Lean in. Heard " + gang + " mean to " + what.Replace(gang + " ", "") + (string.IsNullOrEmpty(when) ? "" : ", " + when) + ".", null, "cb:reveal", 600)
             .Add(1, Voice.Pick(seed + "j", "Keep your voice down, fool.", "You'll get us both killed talking like that.", "...Who told you that?"), "mild")
             .Add(0, Voice.Pick(seed + "k", "Fella who rides with them. Drunk as a lord.", "I hear things. That's all.", "Just saying. Stay off the roads."));
        }

        private void SceneAction(string what)
        {
            if (what == "thief")
            {
                StartTheft();
                return;
            }
            if (what == "stoodup")
            {
                StoodUp();   // WestworldScript.StandUp.cs
                return;
            }
            if (what == "round")
            {
                RoundGoesAround();   // WestworldScript.Round.cs
                return;
            }
            if (what == "thrownout")
            {
                // the card cheat shown the door: his thought once he's outside, a few seconds on
                Ped cheat = _director.CastAt(1);
                if (cheat != null && cheat.Exists())
                {
                    _thrownOut = cheat;
                    _thrownOutName = FullNameOf(cheat);
                    _thrownOutAt = Game.GameTime + 9000;
                }
                return;
            }
            if (what == "reveal" && _sceneJob != null)
            {
                _bridge.RevealPlanAs(_sceneJob.Raw, null, "loose talk in a saloon" + (_placeTown != null ? " in " + _placeTown : ""));
                Log.Write("JOB overheard: " + _sceneJob.Title);
                _sceneJob = null;
            }
        }

        private Ped _thrownOut;
        private string _thrownOutName;
        private int _thrownOutAt, _thrownOuts;

        /// The cheat shown the door thinks of it once he's out (in your view, not taken by anything else), once.
        private void ThrownOutStep(int now)
        {
            if (_thrownOut == null || now < _thrownOutAt)
                return;
            Ped p = _thrownOut;
            _thrownOut = null;
            if (now - _thrownOutAt < 20000 && p.Exists() && !p.IsDead && !_director.Involves(p.Handle) && NodInView(Game.Player.Character, p))
            {
                _ticker.Push((_thrownOutName != null ? Person.FirstOf(_thrownOutName) : "The card cheat") + ", thinking", Scenes.CheatOutside(p.Handle + "|" + now / 60000), "near", p.Handle);
                _thrownOuts++;
                Log.Write("CHEAT " + (_thrownOutName ?? "the card cheat") + " outside, shown the door");
            }
        }

        // ---- weddings and funerals ---------------------------------------------------------------------

        private sealed class WeddingRun
        {
            public Wedding W;
            public Vector3 At;
            public Ped Preacher, A, B;
            public List<Ped> Guests = new List<Ped>();
            public List<KeyValuePair<string, string>> Lines;
            public int Step, NextAt, GiveUpAt, ReleaseAt, SpawnedAt;
            public uint PreacherModel, AModel, BModel, GuestModel;
            public Dictionary<int, int> Health = new Dictionary<int, int>();
        }

        private void Ceremonies(int now)
        {
            if (_wedding != null)
            {
                WeddingStep(now);
                return;
            }
            if (now < _nextCeremony)
                return;
            _nextCeremony = now + 2000;
            Vector3 pos = Game.Player.Character.Position;
            long g = Bridge.Now();
            if (_cfg.Weddings && _placeTown != null)
            {
                Wedding w = _engine.WeddingNear(_placeTown, g);
                Vector3 church;
                if (w != null && ChurchOf(w.Town, out church) && church.DistanceTo2D(pos) < 90f)
                    BeginWedding(w, church, now);
            }
            if (_cfg.Funerals)
                Funerals(pos, g);
        }

        private bool ChurchOf(string town, out Vector3 at)
        {
            if (town != null && Churches.TryGetValue(town, out at))
                return true;
            float r;
            return _bridge.TownCore(town, out at, out r);
        }

        private void BeginWedding(Wedding w, Vector3 church, int now)
        {
            Vector3 spot;
            if (!Natives.StandSpot(church + new Vector3(4f, 4f, 0f), church.Z, out spot))
                spot = church;
            WeddingRun run = new WeddingRun { W = w, At = spot, GiveUpAt = now + 15000, Lines = Scenes.Vows(w, w.Id) };
            run.PreacherModel = Natives.Hash(w.Town == "Saint Denis" ? "u_m_m_nbxpriest_01" : "u_m_m_rhdpreacher_01");
            run.AModel = ModelOfPerson(w.AId, w.AWoman, w.Town, 1);
            run.BModel = ModelOfPerson(w.BId, w.BWoman, w.Town, 2);
            run.GuestModel = Spawner.ModelFor(w.Town, _rng.Next(2) == 0, _rng.Next());
            _wedding = run;
            Log.Write("WEDDING staging: " + w.AName + " & " + w.BName + " at the " + w.Town + " church");
        }

        private uint ModelOfPerson(string id, bool woman, string town, int salt)
        {
            RecordRef r;
            if (id != null && _recById.TryGetValue(id, out r) && r.ModelHash != 0)
                return r.ModelHash;
            return Spawner.ModelFor(town, woman, salt + (id ?? "").GetHashCode());
        }

        private void WeddingStep(int now)
        {
            WeddingRun r = _wedding;
            long g = Bridge.Now();
            if (r.Preacher == null)
            {
                bool ready = Spawner.Ready(r.PreacherModel) & Spawner.Ready(r.AModel) & Spawner.Ready(r.BModel) & Spawner.Ready(r.GuestModel);
                if (!ready)
                {
                    if (now > r.GiveUpAt)
                    {
                        News("Wedding", "You arrive as " + r.W.AName + " and " + r.W.BName + " say their vows at the church in " + r.W.Town + ".");
                        FinishWeddingRun(g, true);
                    }
                    return;
                }
                Vector3 p0 = r.At;
                r.Preacher = Spawner.Spawn(r.PreacherModel, p0, 0f);
                r.A = Spawner.Spawn(r.AModel, p0 + new Vector3(-0.9f, 1.8f, 0f), 180f);
                r.B = Spawner.Spawn(r.BModel, p0 + new Vector3(0.9f, 1.8f, 0f), 180f);
                for (int i = 0; i < 2; i++)
                {
                    Ped guest = Spawner.Spawn(r.GuestModel, p0 + new Vector3(-1.5f + i * 3f, 4.2f, 0f), 180f);
                    if (guest != null)
                        r.Guests.Add(guest);
                }
                if (r.Preacher == null || r.A == null || r.B == null)
                {
                    News("Wedding", "You arrive as " + r.W.AName + " and " + r.W.BName + " say their vows at the church in " + r.W.Town + ".");
                    FinishWeddingRun(g, true);
                    return;
                }
                foreach (Ped p in new[] { r.Preacher, r.A, r.B }.Concat(r.Guests))
                {
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(p.Handle, true);
                    r.Health[p.Handle] = ENTITY.GET_ENTITY_HEALTH(p.Handle);
                }
                r.SpawnedAt = now;
                TASK.TASK_TURN_PED_TO_FACE_ENTITY(r.A.Handle, r.Preacher.Handle, 60000, 0f, 0f, 0f);
                TASK.TASK_TURN_PED_TO_FACE_ENTITY(r.B.Handle, r.Preacher.Handle, 60000, 0f, 0f, 0f);
                TASK.TASK_TURN_PED_TO_FACE_ENTITY(r.Preacher.Handle, r.A.Handle, 60000, 0f, 0f, 0f);
                r.NextAt = now + 2500;
                return;
            }
            if (r.ReleaseAt > 0)
            {
                if (now > r.ReleaseAt)
                {
                    foreach (Ped p in new[] { r.Preacher, r.A, r.B }.Concat(r.Guests))
                        Spawner.Release(p, false);
                    _wedding = null;
                }
                return;
            }
            bool byYou = false;
            string broke = now - r.SpawnedAt < 1500 ? null : WeddingBroken(r, out byYou);
            if (broke != null)
            {
                BreakWedding(r, byYou, broke, now, g);
                return;
            }
            if (Game.Player.Character.Position.DistanceTo(r.At) > 90f)
            {
                // he rode off before the end
                bool stayed = r.Step >= 3;
                News("Wedding", r.W.AName + " and " + r.W.BName + " are married" + (stayed ? ". You left before the end." : ". You didn't stay."));
                FinishWeddingRun(g, stayed);
                r.ReleaseAt = now + 1;
                return;
            }
            if (now < r.NextAt)
                return;
            if (r.Step < r.Lines.Count)
            {
                KeyValuePair<string, string> line = r.Lines[r.Step++];
                Feed.Say(line.Key, line.Value);
                Ped speaker = line.Key == "Preacher" ? r.Preacher : line.Key == r.W.AName ? r.A : r.B;
                if (speaker != r.Preacher)
                    Speech.SayAny(speaker.Handle, Speech.Yes, r.Preacher.Handle);
                r.NextAt = now + 2600 + line.Value.Length * 45;
                return;
            }
            foreach (Ped guest in r.Guests)
                Speech.SayAny(guest.Handle, Speech.Won, r.A.Handle);
            News("Wedding", r.W.AName + " and " + r.W.BName + " are married. They saw you there.");
            FinishWeddingRun(g, true);
            TASK.TASK_WANDER_STANDARD(r.A.Handle, 10f, 10);
            TASK.TASK_WANDER_STANDARD(r.B.Handle, 10f, 10);
            r.ReleaseAt = now + 30000;
        }

        private IEnumerable<Ped> WeddingParty(WeddingRun r)
        {
            return new[] { r.Preacher, r.A, r.B }.Concat(r.Guests).Where(p => p != null);
        }

        /// Gunfire, a body, a drawn gun: the wedding is over. Returns why, or null.
        private string WeddingBroken(WeddingRun r, out bool byYou)
        {
            byYou = false;
            Ped me = Game.Player.Character;
            foreach (Ped p in WeddingParty(r))
            {
                if (!p.Exists())
                    return "someone in the wedding party vanished";
                int h = p.Handle;
                if (PLAYER.IS_PLAYER_FREE_AIMING_AT_ENTITY(Game.Player.Handle, h))
                {
                    byYou = true;
                    return "a gun pointed at the wedding party";
                }
                int start;
                if (!r.Health.TryGetValue(h, out start))
                    start = ENTITY.GET_ENTITY_HEALTH(h);
                if (p.IsDead || PED.IS_PED_FLEEING(h) || PED.IS_PED_IN_COMBAT(h, 0) || ENTITY.GET_ENTITY_HEALTH(h) < start - 10)
                {
                    byYou = ENTITY.HAS_ENTITY_BEEN_DAMAGED_BY_ENTITY(h, me.Handle, true, true) || PED.IS_PED_IN_COMBAT(h, me.Handle) ||
                            (p.IsDead && PED.GET_PED_SOURCE_OF_DEATH(h) == me.Handle);
                    return p.IsDead ? "someone in the wedding party is dead" : "violence at the wedding";
                }
            }
            if (PED.IS_PED_SHOOTING(me.Handle) && me.Position.DistanceTo(r.At) < 70f)
            {
                byYou = true;
                return "shots fired near the church";
            }
            return null;
        }

        private void BreakWedding(WeddingRun r, bool byYou, string why, int now, long g)
        {
            Ped me = Game.Player.Character;
            bool someoneDied = false;
            bool screamed = false;
            foreach (Ped p in WeddingParty(r))
            {
                if (!p.Exists())
                    continue;
                if (p.IsDead)
                {
                    someoneDied |= PED.GET_PED_SOURCE_OF_DEATH(p.Handle) == me.Handle;
                    continue;
                }
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(p.Handle, false);
                TASK.CLEAR_PED_TASKS(p.Handle, true, false);
                TASK.TASK_SMART_FLEE_PED(p.Handle, me.Handle, 120f, -1, 0, 3f, 0);
                if (!screamed)
                    screamed = Speech.SayAny(p.Handle, Speech.Scared, me.Handle);
            }
            News("Wedding", "The wedding of " + r.W.AName + " and " + r.W.BName + " broke up in panic" + (byYou ? ". They know who to blame." : "."));
            _engine.RuinWedding(r.W, g, byYou);
            if (someoneDied && _cfg.Deeds)
            {
                Vector3 pos = me.Position;
                _engine.OnDeed(Deeds.Make("murder", "a wedding guest", r.W.Town, ZoneAt(pos), g, pos.X, pos.Y, pos.Z, WeddingParty(r).Count() + 1),
                    new[] { r.W.AId, r.W.BId }.Where(x => x != null).ToList(), g);
            }
            r.ReleaseAt = now + 20000;
            Log.Write("WEDDING broke up: " + why + (byYou ? " (you)" : ""));
        }

        /// Ends a staged wedding early (a mission started): the party is handed back to the game, or vanishes unseen.
        private void EndWeddingRun(string why)
        {
            WeddingRun r = _wedding;
            _wedding = null;
            if (r == null)
                return;
            foreach (Ped p in WeddingParty(r))
                Spawner.Release(p, !ENTITY.IS_ENTITY_ON_SCREEN(p.Handle));
            if (!r.W.Done)
                _engine.FinishWedding(r.W, null, Bridge.Now(), false);
            Log.Write("WEDDING staging ended early: " + why);
        }

        private void FinishWeddingRun(long g, bool attended)
        {
            WeddingRun r = _wedding;
            _engine.FinishWedding(r.W, null, g, attended);
            if (attended && _cfg.Deeds)
            {
                Vector3 pos = Game.Player.Character.Position;
                _engine.OnDeed(Deeds.Make("wedding", Person.FirstOf(r.W.AName) + " and " + Person.FirstOf(r.W.BName), r.W.Town, null, g, pos.X, pos.Y, pos.Z, 3),
                    new[] { r.W.AId, r.W.BId }.Where(x => x != null).ToList(), g);
            }
            if (r.Preacher == null)
                _wedding = null;
        }

        private void Funerals(Vector3 pos, long g)
        {
            foreach (PlanInfo f in _bridge.Plans().Where(p => p.Kind == "funeral" && !p.Done && g >= p.DueAt - 30 && g <= p.DueAt + Math.Max(60, p.WindowMin)))
            {
                string key = f.Id ?? f.Title;
                if (key == null || _funeralsHeard.Contains(key) || f.Pos.DistanceTo2D(pos) > 30f)
                    continue;
                _funeralsHeard.Add(key);
                RecordRef deadRec = null;
                if (f.Dead != null)
                    _recById.TryGetValue(f.Dead, out deadRec);
                Person dead = deadRec != null ? PersonOf(deadRec) : null;
                string name = deadRec != null ? deadRec.Name : "him";
                bool byYou = deadRec != null && deadRec.KilledByPlayer;
                List<Ped> mourners = World.GetAllPeds().Where(p => !ENTITY.IS_ENTITY_DEAD(p.Handle) && PED.IS_PED_HUMAN(p.Handle) && p.Handle != Game.Player.Character.Handle &&
                                                              p.Position.DistanceTo2D(f.Pos) < 16f).Take(3).ToList();
                List<string> lines = Scenes.Eulogy(name, dead, byYou, true, key);
                for (int i = 0; i < lines.Count; i++)
                {
                    Ped speaker = mourners.Count > 0 ? mourners[i % mourners.Count] : null;
                    RecordRef who = speaker != null ? _bridge.RecordAt(speaker.Handle) : null;
                    string label = who != null ? who.Name : speaker != null ? "A mourner" : "Someone at the graveside";
                    Feed.Say(label, lines[i], speaker != null ? Speech.For(speaker.Handle, lines[i], fallback: "replybad") : null);
                    if (speaker != null && i == lines.Count - 1 && byYou)
                        TASK.TASK_LOOK_AT_ENTITY(speaker.Handle, Game.Player.Character.Handle, 8000, 0, 51, 0);
                }
                if (_cfg.Deeds && deadRec != null)
                    _engine.OnDeed(Deeds.Make("funeral", deadRec.Name, _placeTown, ZoneAt(pos), g, pos.X, pos.Y, pos.Z, mourners.Count), mourners.Select(m => _bridge.RecordAt(m.Handle)).Where(r => r != null).Select(r => r.Id).ToList(), g);
                Log.Write("FUNERAL of " + name + ": mourners spoke (" + mourners.Count + " near the grave)");
                return;
            }
        }

        // ---- kin in their own town --------------------------------------------------------------------

        // One relative at a time walks up to the player in their own town (Lex, 2026-10-28, after eight walk-ups in his log
        // that never arrived: an ambient passer-by got one 20 s walk order the game's own script overrode, and a notice named
        // the kin before anyone had reached him). Now: a body RealNPCs may direct, in view, within 18 m, carrying no name of
        // its own (not a bound resident, not one of enRAGE's townsfolk or records, not ours); the walk order re-sent every
        // 4 s while they aren't getting nearer; given up after 30 s; one relative a town a game day and the same one once a
        // week (Pacing "kin"); never in the first two minutes after a start or a load; no notice: they're named when they
        // reach you and speak (GreetedSpecial). F7 `TEST kin:`; the Testing row `kin` skips the rests and the wait.
        private sealed class KinWalk
        {
            public Ped Ped;
            public Relative R;
            public string StateId, StateName, Town;
            public uint Model;   // the body's model at the pick: a handle the game reused for someone else is seen
            public int Since, Until, LastOrder, Said, Reorders;
            public bool Spawned; // made for the part (nobody free on the street): let go to the game when they have spoken
            public int Pending;  // the walk order still to send (the body was let go of a scenario first), 0 none
            public float LastDist, StartDist;
            public bool Test;
        }

        /// A relative waiting to be made out of sight (nobody free on the street): made on a tick with the panel closed and the
        /// model streamed in, then walked up as any passer-by (KinSpawnStep).
        private sealed class KinPending
        {
            public Relative R;
            public string StateId, StateName, Town, Counts;
            public uint Model;
            public int Since, Tries, NextTryAt, Remakes;   // tries spaced 700 ms (the game's ped pool is full now and then); remade when enRAGE names the body
            public bool Test;
        }

        private KinPending _kinPending;
        private int _kinRemakes;   // bodies made for the current relative that enRAGE named (let go and made again, twice at most)
        private KinWalk _kinWalk;
        private int _kinNotBefore, _kinWalks, _kinArrived, _kinGaveUp;
        private string _kinLast, _kinWhyNot;
        private const int KinLookMs = 10000, KinWalkMs = 30000, KinReorderMs = 4000, KinFirstMs = 120000;
        private const float KinNear = 3.2f, KinFar = 18f;

        private void CastTick(int now)
        {
            KinWalkStep(now);
            if (!_cfg.CastRelatives || now < _nextCast || _kinWalk != null)
                return;
            _nextCast = now + KinLookMs;
            if (_kinNotBefore == 0)
                _kinNotBefore = now + KinFirstMs;   // not in the first two minutes after a start or a load
            if (_placeTown == null || now < _kinNotBefore || Feed.Busy || _director.Busy)
                return;
            string why;
            KinWalkUp(Game.Player.Character, now, false, out why);
        }

        /// Sends a relative of someone the player knows walking over, if one lives here and a body will do; why not, else.
        private bool KinWalkUp(Ped me, int now, bool test, out string why)
        {
            why = null;
            if (!_cfg.CastRelatives)
            {
                why = "[World] CastRelatives is off";
                return false;
            }
            if (_kinWalk != null)
            {
                why = _kinWalk.R.Name + " is coming up to you already";
                return false;
            }
            if (_kinPending != null)
            {
                why = _kinPending.R.Name + " is being made out of sight already";
                return false;
            }
            if (_placeTown == null || me == null || !me.Exists())
            {
                why = _kinWhyNot = "you aren't in any town";
                return false;
            }
            if (me.IsDead || PED.IS_PED_ON_MOUNT(me.Handle) || PED.IS_PED_IN_ANY_VEHICLE(me.Handle, false) || ENTITY.GET_ENTITY_SPEED(me.Handle) > 2f)
            {
                why = _kinWhyNot = "you're riding, driving or moving too fast for anyone on foot to catch up";
                return false;
            }
            long g = _store.LastGameMin;
            var kin = _store.People.Values.Where(st => st.Family != null).SelectMany(st => st.Family.Select(r => new { st, r }))
                .Where(x => x.r.Town == _placeTown && !_store.Avengers.Any(a => a.Name == x.r.Name && a.Stage < 3) &&
                            (x.st.Talks > 0 || x.r.Heard.Count > 0 || (_store.Reveries.ContainsKey(x.st.Id) && _store.Reveries[x.st.Id].ByPlayer)))
                .OrderBy(x => _rng.Next()).ToList();
            if (kin.Count == 0)
            {
                why = _kinWhyNot = "nobody you know has family in " + _placeTown;
                return false;
            }
            // the street first: the nearest free body of each sex, counted by what kept the rest out, so the refusal says
            // what the street was made of (Lex, 2026-10-29: six tries in Valentine refused with every count at 0: the
            // town's walkers are all in the game's walking scenarios, which were passed over uncounted; then four more
            // refused "9 the wrong sex": the relative was drawn before the street was looked at, and a sister wanted a
            // woman on a street of men). Now a scenario is let go of first, someone in one is taken only when nobody
            // free of one is to hand, and the relative is the first whose sex the street can supply.
            int total = 0, theGames = 0, tooClose = 0, ours = 0, riding = 0, inScenario = 0, outOfView = 0, named = 0, notFree = 0, mayNot = 0, men = 0, women = 0;
            Ped[] bodies = new Ped[2];                           // 0 a man, 1 a woman
            float[] bestD = { float.MaxValue, float.MaxValue };
            bool[] bodyInScenario = new bool[2];
            foreach (Ped p in World.GetAllPeds())
            {
                if (p == null || !p.Exists() || p.Handle == me.Handle || ENTITY.IS_ENTITY_DEAD(p.Handle) || !PED.IS_PED_HUMAN(p.Handle))
                    continue;
                int h = p.Handle;
                float d = p.Position.DistanceTo(me.Position);
                if (d >= KinFar)
                    continue;
                total++;
                if (d <= 5f)
                {
                    tooClose++;
                    continue;
                }
                if (!Natives.Bystander(p, me.Handle))
                {
                    theGames++;   // the game's own scripts' people (mission entities), lawmen, children, anyone fighting or fleeing
                    continue;
                }
                if (Spawner.IsOurs(h))
                {
                    ours++;
                    continue;
                }
                if (PED.IS_PED_IN_ANY_VEHICLE(h, false))
                {
                    riding++;
                    continue;
                }
                if (_bridge.RecordAt(h) != null || _bridge.AcquaintanceAt(h) != null || _resBind.ContainsKey(h))
                {
                    named++;   // someone with a name of their own already: the kin would be a second name on the same body
                    continue;
                }
                if (!NodInView(me, p))
                {
                    outOfView++;
                    continue;
                }
                if (NotFree(h))
                {
                    notFree++;
                    continue;
                }
                if (MayDirectWhyNot(p) != null)
                {
                    mayNot++;
                    continue;
                }
                int sex = PED.IS_PED_MALE(h) ? 0 : 1;
                if (sex == 0) men++; else women++;
                bool scenario = PED.IS_PED_USING_ANY_SCENARIO(h);
                if (scenario)
                    inScenario++;
                // someone free of a scenario first, the nearest of them; else the nearest in one (let go of it first)
                if (bodies[sex] == null || (bodyInScenario[sex] && !scenario) || (bodyInScenario[sex] == scenario && d < bestD[sex]))
                {
                    bestD[sex] = d;
                    bodies[sex] = p;
                    bodyInScenario[sex] = scenario;
                }
            }
            // the relative: the first of them (shuffled) whose sex the street can supply and whose rests allow
            string pace = null, skipped;
            var pick = kin[0];
            Ped body = null;
            int withBody = 0;
            foreach (var k in kin)
            {
                if (bodies[k.r.Woman ? 1 : 0] == null)
                    continue;
                withBody++;
                pace = Pacing.WhyNot(_store.Pace, "kin", _placeTown, k.r.Name, g, _rng.Next(100), test, out skipped);
                if (pace == null)
                {
                    pick = k;
                    body = bodies[k.r.Woman ? 1 : 0];
                    break;
                }
            }
            bool spawned = false;
            if (body == null && withBody > 0 && pace != null)
            {
                why = _kinWhyNot = "rests: " + pace;
                return false;
            }
            if (body == null)
            {
                // the street has nobody to send (in a town enRAGE has named most of the walkers by their looks, and the test cast
                // carry names of their own): the relative is made out of sight behind the player instead ([World]
                // CastRelativesSpawn; Lex, 2026-10-29: "8 with a name already" four times running in Valentine), let go to the
                // game when they've spoken. The full counts go to the log; the ticker gets a line that fits.
                string counts = total + " people in range: " + men + " men and " + women + " women free; " + theGames + " the game's own, lawmen or children, " + tooClose + " within 5 m, " + ours + " ours, " +
                                riding + " riding, " + named + " with a name already (enRAGE's townsfolk, the test cast, our regulars), " + outOfView + " out of view, " + notFree + " busy, " + mayNot + " not ours to direct";
                string shortWhy = "nobody free on the street for " + KinSexes(kin.Select(k => k.r.Woman)) + ": " + total + " in range, " + named + " named already, " + theGames + " the game's own";
                if (!_cfg.CastRelativesSpawn)
                {
                    Log.Detail("CAST nobody to send (" + counts + ")");
                    why = _kinWhyNot = shortWhy + " (CastRelativesSpawn is off; the counts are in the log)";
                    return false;
                }
                foreach (var k in kin)
                {
                    pace = Pacing.WhyNot(_store.Pace, "kin", _placeTown, k.r.Name, g, _rng.Next(100), test, out skipped);
                    if (pace == null)
                    {
                        pick = k;
                        break;
                    }
                }
                if (pace != null)
                {
                    why = _kinWhyNot = "rests: " + pace;
                    return false;
                }
                // made on a later tick, as the test cast is (KinSpawnStep): a spawn from the Testing page's own key press, with
                // the panel open and the clock slowed, gave nobody twice (the game made nobody at the spot, two models)
                uint model = Spawner.ModelFor(_placeTown, pick.r.Woman, Voice.Hash(pick.r.Name + "|kin|" + now / 60000));
                Spawner.Ready(model);   // streaming starts now
                _kinPending = new KinPending { R = pick.r, StateId = pick.st.Id, StateName = pick.st.Name, Town = _placeTown, Model = model, Since = now, Test = test, Counts = counts };
                Log.Write("CAST " + pick.r.Name + " (" + pick.r.Relation + " of " + pick.st.Name + ") in " + _placeTown + ": nobody free on the street (" + counts + "); being made out of sight" + (test ? ", a test" : ""));
                why = null;
                return true;
            }
            float best = bestD[pick.r.Woman ? 1 : 0];
            bool bestInScenario = bodyInScenario[pick.r.Woman ? 1 : 0];
            int bh = body.Handle;
            _casts[bh] = new KeyValuePair<Relative, string>(pick.r, pick.st.Id);
            int pending = KinOrder(bh, me, now);
            _kinWalk = new KinWalk { Ped = body, R = pick.r, StateId = pick.st.Id, StateName = pick.st.Name, Town = _placeTown, Model = ENTITY.GET_ENTITY_MODEL(bh),
                                     Since = now, Until = now + KinWalkMs, LastOrder = now, LastDist = best, StartDist = best, Test = test, Pending = pending, Spawned = spawned };
            _kinWalks++;
            Log.Write("CAST " + pick.r.Name + " (" + pick.r.Relation + " of " + pick.st.Name + ") in " + _placeTown + ": " + (spawned ? "nobody free on the street, so a passer-by made out of sight " : "a passer-by ") + (int)best + " m off walks over" +
                      (bestInScenario ? " (let go of what they were doing first; " + inScenario + " of " + total + " in range were busy that way)" : "") + (test ? ", a test" : ""));
            return true;
        }

        /// "a woman", "a man" or "a man or a woman": the sexes the relatives here would need, for the refusal.
        private static string KinSexes(IEnumerable<bool> women)
        {
            bool w = false, m = false;
            foreach (bool x in women)
            {
                if (x) w = true; else m = true;
            }
            return w && m ? "a man or a woman" : w ? "a woman (the relatives here are women)" : "a man (the relatives here are men)";
        }

        /// The relative waiting to be made: on a tick with the panel closed and the model in, a spot (out of sight behind the player
        /// first, then the open street as the test cast is brought) and the body; three tries, then given up with the spawner's reason.
        private void KinSpawnStep(int now)
        {
            KinPending k = _kinPending;
            if (k == null)
                return;
            Ped me = Game.Player.Character;
            if (me == null || !me.Exists() || _placeTown != k.Town)
            {
                _kinPending = null;
                Log.Write("CAST " + k.R.Name + " not made: the town changed");
                return;
            }
            if (PanelOpen || MISC.GET_MISSION_FLAG() || now < k.NextTryAt)
                return;
            k.NextTryAt = now + 700;
            if (!Spawner.Ready(k.Model))
            {
                if (now - k.Since > 15000)
                {
                    _kinPending = null;
                    Log.Write("CAST " + k.R.Name + " not made: the body (model " + k.Model.ToString("x8") + ") never streamed in");
                }
                return;
            }
            Vector3 at;
            if (!EncounterSpot(me, 14f, 26f, out at) && !EncounterSpot(me, 8f, 40f, out at) && !TestCastOpenSpot(me, out at))
            {
                if (++k.Tries >= 12)
                {
                    _kinPending = null;
                    Log.Write("CAST " + k.R.Name + " not made: no spot for them");
                }
                return;
            }
            Vector3 mp = me.Position;
            Ped body = Spawner.Spawn(k.Model, at, (float)(Math.Atan2(-(mp.X - at.X), mp.Y - at.Y) * 180.0 / Math.PI));
            if (body == null)
            {
                if (++k.Tries >= 12)
                {
                    _kinPending = null;
                    _kinWhyNot = k.R.Name + " couldn't be made: " + (Spawner.LastSpawnWhy ?? "the spawn failed");
                    Log.Write("CAST " + k.R.Name + " not made: " + (Spawner.LastSpawnWhy ?? "the spawn failed") + " (twelve tries over eight seconds: the game's ped pool is full here)");
                }
                return;
            }
            _kinPending = null;
            if (k.Remakes == 0)
                _kinRemakes = 0;
            int bh = body.Handle;
            float d = body.Position.DistanceTo(mp);
            _casts[bh] = new KeyValuePair<Relative, string>(k.R, k.StateId);
            int pending = KinOrder(bh, me, now);
            _kinWalk = new KinWalk { Ped = body, R = k.R, StateId = k.StateId, StateName = k.StateName, Town = k.Town, Model = ENTITY.GET_ENTITY_MODEL(bh),
                                     Since = now, Until = now + KinWalkMs, LastOrder = now, LastDist = d, StartDist = d, Test = k.Test, Pending = pending, Spawned = true };
            _kinWalks++;
            Log.Write("CAST " + k.R.Name + " (" + k.R.Relation + " of " + k.StateName + ") in " + k.Town + ": made out of sight " + (int)d + " m off, walks over" + (k.Test ? ", a test" : ""));
        }

        /// The walk order. Someone in a scenario (leaning, sitting, the game's walking routes) is let go of it first and gets
        /// the order a moment later (as the Director's "meet" does: a walk order alone never moves them); returns when that
        /// order is due, else 0.
        private static int KinOrder(int h, Ped me, int now)
        {
            if (PED.IS_PED_USING_ANY_SCENARIO(h))
            {
                TASK.CLEAR_PED_TASKS(h, true, false);
                TASK.TASK_LOOK_AT_ENTITY(h, me.Handle, KinWalkMs, 0, 51, 0);
                return now + 1800;
            }
            TASK.TASK_GO_TO_ENTITY(h, me.Handle, -1, 2.2f, 1.4f, 0f, 0);
            TASK.TASK_LOOK_AT_ENTITY(h, me.Handle, KinWalkMs, 0, 51, 0);
            return 0;
        }

        /// The relative on their way: the order re-sent while they aren't getting nearer, the meeting when they're close
        /// (they speak first if the player doesn't greet them), given up when they can't reach him.
        private void KinWalkStep(int now)
        {
            if (_kinPending != null)
                KinSpawnStep(now);
            KinWalk w = _kinWalk;
            if (w == null)
                return;
            Ped me = Game.Player.Character;
            if (w.Ped == null || !w.Ped.Exists() || w.Ped.IsDead || me == null || !me.Exists())
            {
                KinEnd(w, now, "the body is gone");
                return;
            }
            int h = w.Ped.Handle;
            if (ENTITY.GET_ENTITY_MODEL(h) != w.Model)
            {
                KinEnd(w, now, "the body is gone (the handle is someone else's now)");
                return;
            }
            if (w.Said == 0 && !_casts.ContainsKey(h))
            {
                w.Said = now;   // the player greeted them first (GreetedSpecial took them, and they spoke)
                KinArrived(w, now, "you greeted them first");
            }
            float d = w.Ped.Position.DistanceTo(me.Position);
            if (w.Said == 0)
            {
                // a body made for the part that enRAGE takes for one of its own townsfolk by its look (its card goes on him within its
                // 2 s people sync: Lex saw "Manley Biggs" over James McLoughlin) is let go unseen and made again with another look, twice
                if (w.Spawned && now - w.Since < 3000 && _bridge.AcquaintanceAt(h) != null)
                {
                    string theirs = _bridge.EnrageNameAt(h) ?? "one of its townsfolk";
                    int remakes = _kinRemakes + 1;
                    _casts.Remove(h);
                    Spawner.Release(w.Ped, true);
                    _kinWalk = null;
                    if (remakes <= 2)
                    {
                        _kinRemakes = remakes;
                        uint model = Spawner.ModelFor(w.Town, w.R.Woman, Voice.Hash(w.R.Name + "|kin|" + now + "|" + remakes));
                        Spawner.Ready(model);
                        _kinPending = new KinPending { R = w.R, StateId = w.StateId, StateName = w.StateName, Town = w.Town, Model = model, Since = now, Test = w.Test, Remakes = remakes };
                        Log.Write("CAST " + w.R.Name + ": enRAGE took the body for " + theirs + "; let go unseen and made again with another look (" + remakes + ")");
                    }
                    else
                    {
                        _kinRemakes = 0;
                        _kinGaveUp++;
                        _kinLast = w.R.Name + " gave it up: enRAGE named every body made for them";
                        Log.Write("CAST " + w.R.Name + " gave it up: enRAGE took every body made for them for its own townsfolk (" + theirs + " last)");
                    }
                    return;
                }
                string may = w.Spawned ? null : MayDirectWhyNot(w.Ped);   // a body made for the part is ours by nature
                if (may != null || _director.Involves(h))
                {
                    KinEnd(w, now, may ?? "a scene took them");
                    return;
                }
                if (d > w.StartDist + 6f)
                {
                    KinEnd(w, now, "outrun (" + (int)d + " m off, from " + (int)w.StartDist + ")");
                    return;
                }
                if (d < KinNear)
                {
                    if (Feed.Talking)
                    {
                        if (now > w.Until)
                            KinEnd(w, now, "a conversation ran on while they stood by");
                        return;   // waits for the talk to end before speaking
                    }
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, me.Handle, 4000, 0f, 0f, 0f);
                    w.Said = now;
                    KinArrived(w, now, "after " + (now - w.Since) / 1000 + " s from " + (int)w.StartDist + " m" + (w.Reorders > 0 ? ", the order sent again " + w.Reorders + " time" + (w.Reorders == 1 ? "" : "s") : ""));
                    GreetedSpecial(h);
                    return;
                }
                if (now > w.Until)
                {
                    KinEnd(w, now, "never reached you in " + KinWalkMs / 1000 + " s (" + (int)d + " m off, from " + (int)w.StartDist + ")");
                    return;
                }
                if (w.Pending > 0 && now >= w.Pending)
                {
                    // let go of a scenario a moment ago: the walk order now (a second clear if the game put them straight back)
                    w.Pending = KinOrder(h, me, now);
                    w.LastOrder = now;
                    w.LastDist = d;
                    return;
                }
                if (now - w.LastOrder >= KinReorderMs)
                {
                    if (d > w.LastDist - 0.5f)
                    {
                        w.Pending = KinOrder(h, me, now);   // not getting nearer: the game's own script may have given them something else to do
                        w.Reorders++;
                    }
                    w.LastOrder = now;
                    w.LastDist = d;
                }
                return;
            }
            if (now - w.Said > 8000)
            {
                if (w.Spawned)
                    Spawner.Release(w.Ped, false);   // made for the part: handed to the game, which clears them out of sight
                else
                    TASK.TASK_WANDER_STANDARD(h, 10f, 10);   // said their piece; goes about their day
                _casts.Remove(h);
                _kinWalk = null;
            }
        }

        /// They reached the player (or he greeted them first): counted, and the town's day and the relative's week noted.
        private void KinArrived(KinWalk w, int now, string how)
        {
            _kinArrived++;
            _kinLast = w.R.Name + " (" + w.R.Relation + " of " + w.StateName + ") reached you, " + how;
            Log.Write("CAST " + _kinLast);
            if (!w.Test)
                Pacing.Note(_store.Pace, "kin", w.Town, w.R.Name, _store.LastGameMin);
        }

        /// The walk is over without a meeting: the body goes about its day, and the next look waits a minute. After the
        /// meeting (the body gone in the 8 s tail) nothing is counted.
        private void KinEnd(KinWalk w, int now, string why)
        {
            if (w.Ped != null && w.Ped.Exists() && !w.Ped.IsDead && ENTITY.GET_ENTITY_MODEL(w.Ped.Handle) == w.Model)
            {
                if (w.Spawned)
                    Spawner.Release(w.Ped, false);
                else
                    TASK.TASK_WANDER_STANDARD(w.Ped.Handle, 10f, 10);
            }
            if (w.Ped != null)
                _casts.Remove(w.Ped.Handle);
            _kinWalk = null;
            if (w.Said > 0)
                return;
            _kinGaveUp++;
            _kinLast = w.R.Name + " gave it up: " + why;
            Log.Write("CAST " + w.R.Name + " gave it up: " + why);
            _kinNotBefore = now + 60000;
        }

        /// A mission, a load, the wipe or shutdown: the relative on their way is let go, and the next waits two minutes.
        private void KinAbort()
        {
            _kinPending = null;
            if (_kinWalk != null)
            {
                if (_kinWalk.Ped != null && _kinWalk.Ped.Exists() && !_kinWalk.Ped.IsDead)
                {
                    if (_kinWalk.Spawned)
                        Spawner.Release(_kinWalk.Ped, false);
                    else
                        TASK.TASK_WANDER_STANDARD(_kinWalk.Ped.Handle, 10f, 10);
                }
                _kinWalk = null;
            }
            _casts.Clear();
            _kinNotBefore = 0;
        }

        private string KinStatus()
        {
            if (!_cfg.CastRelatives)
                return "off ([World] CastRelatives)";
            KinWalk w = _kinWalk;
            Ped me = Game.Player.Character;
            string coming = w != null && w.Ped != null && w.Ped.Exists() && me != null && me.Exists()
                ? "; " + w.R.Name + " (" + w.R.Relation + " of " + w.StateName + ") is coming, " + (int)w.Ped.Position.DistanceTo(me.Position) + " m off, " + (_now - w.Since) / 1000 + " s in" + (w.Said > 0 ? ", spoke" : "")
                : "";
            return _kinWalks + " walked up this session, " + _kinArrived + " reached you, " + _kinGaveUp + " gave up" + coming +
                   (_kinLast != null ? "; last: " + _kinLast : "") + (_kinWhyNot != null ? "; the last look: " + _kinWhyNot : "");
        }

        /// The player greeted someone special: the stalker, or kin met on the street.
        private bool GreetedSpecial(int handle)
        {
            if (TravellerGreeted(handle, true))
                return true;   // the traveller asking the way: a greeting is an answer (WestworldScript.Travellers.cs)
            if (SupperGreeted(handle, true))
                return true;   // the man with supper on: a greeting is a yes (WestworldScript.Supper.cs)
            if (_stalker != null && _stalker.Handle == handle)
            {
                StalkerSay(_stalker.Label, _stalker.Revenant != null ? Hosts.RevenantAloud(_stalker.Revenant, _engine.Player, _stalker.Revenant.Id + "g")
                    : Hosts.StalkerCaught(false, _engine.Player, "greet" + Game.GameTime), _stalker.Revenant != null);
                return true;
            }
            KeyValuePair<Relative, string> cast;
            if (!_casts.TryGetValue(handle, out cast))
                return false;
            _casts.Remove(handle);
            Relative r = cast.Key;
            PersonState st = _store.Find(cast.Value);
            Person p = _engine.Find(cast.Value) ?? new Person { Id = cast.Value, Name = st != null ? st.Name : "him" };
            Reverie rv;
            bool killed = st != null && _store.Reveries.TryGetValue(st.Id, out rv) && rv.ByPlayer && !rv.Woke;
            string first = Person.FirstOf(p.Name ?? "");
            string kin = Engine.KinWord(r.Relation, p.Woman);
            List<string> lines = new List<string>();
            string seed = r.Name + Game.GameTime / 60000;
            if (killed)
            {
                lines.Add(Voice.Pick(seed, "You're him. The one who killed my " + kin + ". " + first + ".", "I'd know you anywhere. They described you down to the boots. You killed my " + kin + "."));
                lines.Add(r.Woman ? Voice.Pick(seed + "w", "I won't scream. I won't give you that. Just tell me why.", "Get away from me. Get away before I find a gun.")
                                  : Voice.Pick(seed + "m", "If I had my rifle, you'd be dead already.", "Walk away. Walk away while I can still let you."));
                Speech.SayAny(handle, r.Woman ? Speech.Scared : Speech.Angry, Game.Player.Character.Handle);
            }
            else
            {
                MemoryItem good = p.Memories.FirstOrDefault(m => m.AboutYou && m.Emotion == "Joy");
                if (good != null)
                    lines.Add(Voice.Pick(seed, "You're the one " + first + " wrote about! You " + Voice.Retell(good.Text, p.Woman).Replace("you ", "") + ". God bless you, mister.",
                        "My " + kin + " " + first + " told me what you did. I never thought I'd get to thank you."));
                else if (r.Heard.Count > 0)
                    lines.Add(Voice.Pick(seed, "My " + kin + " " + first + " wrote to me about you. I didn't believe half of it.", first + "'s letters are full of you, you know. Not all of it kind."));
                else
                    lines.Add(Voice.Pick(seed, "You know my " + kin + ", don't you? " + first + ". How is " + (p.Woman ? "she" : "he") + "?", "You've met my " + kin + ". " + first + ". Tell " + (p.Woman ? "her" : "him") + " to write more."));
                lines.Add(Grammar.Sentence(r.Line != null ? "I " + r.Line.Replace("{p}", first) + ", if you ever need anything" : "Look after yourself out there"));
                Speech.SayAny(handle, Speech.Greet, Game.Player.Character.Handle);
            }
            for (int i = 0; i < lines.Count; i++)
                Feed.Say(r.Name, lines[i], i == 0 ? null : Speech.For(handle, lines[i], Game.Player.Character.Handle), toYou: true);   // the first one's voice played above
            return true;
        }

        // ---- what the engine needs done in the world ----------------------------------------------------

        private void WorldRequestsImpl()
        {
            if (_engine == null || _engine.Requests.Count == 0)
                return;
            List<WorldRequest> list = _engine.Requests.ToList();
            _engine.Requests.Clear();
            foreach (WorldRequest q in list)
            {
                try
                {
                    switch (q.Kind)
                    {
                        case "revive":
                            Revive(q);
                            break;
                        case "avenger":
                            Avenge(q.Avenger);
                            break;
                        case "paper":
                            if (_cfg.Newspaper)
                            {
                                Vector3 c;
                                float r;
                                bool has = _bridge.TownCore(q.Town, out c, out r);
                                if (_bridge.AddTownItem(q.Text, q.Town != null ? "in " + q.Town : null, has ? (Vector3?)c : null))
                                    Log.Write("NEWSPAPER item for " + (q.Town ?? "the territory") + ": " + q.Text);
                            }
                            break;
                        case "gooddeed":
                            if (_cfg.Newspaper && q.Text != null)
                            {
                                Vector3 c;
                                float r;
                                bool has = _bridge.TownCore(q.Town, out c, out r);
                                _bridge.AddDeed("good", q.Text, q.Town != null ? "word got around " + q.Town : null, has ? (Vector3?)c : null);
                            }
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Log.Write("World request " + q + " failed: " + ex.Message);
                }
            }
        }

        private void Revive(WorldRequest q)
        {
            RecordRef rec;
            if (q.PersonId == null || !_recById.TryGetValue(q.PersonId, out rec))
                return;
            Reverie r = q.Reverie;
            int bravery = rec.Facets != null && rec.Facets.ContainsKey("Bravery") ? rec.Facets["Bravery"] : 50;
            bool ok = _bridge.Revive(rec, bravery < 50 ? "Fear" : "Anger", 85, Hosts.ReverieMemory(r), q.Text, r.ByPlayer ? 25 : 0, bravery < 50 ? 20 : 5, 720);
            Log.Write("REVIVED " + rec.Name + (ok ? "" : " - failed (see above)"));
        }

        private void Avenge(Avenger a)
        {
            if (a == null || !_cfg.Avengers || !_bridge.CanMakePeople)
            {
                if (a != null)
                    a.Stage = 3;
                return;
            }
            Vector3 pos = Game.Player.Character.Position;
            uint model = Spawner.ModelFor(Rumours.Towns.ContainsKey(a.Town ?? "") ? a.Town : null, a.Woman, (a.Name ?? "").GetHashCode());
            RecordRef rec = _bridge.NewPerson(model, _bridge.ZoneName(pos) ?? a.Town);
            if (rec == null)
            {
                a.Stage = 3;
                return;
            }
            string deadFirst = Person.FirstOf(a.DeadName ?? "");
            string theirs = a.Woman ? "her" : "his";
            string relDead = Engine.KinWord(a.Relation, a.DeadWoman);
            // the record's filling in and the goal, the writes themselves (Typed.cs, AvengerFill)
            Typed.AvengerFill(rec.Raw, a.Name, a.DeadId, a.Relation, 80 + _rng.Next(16), pos.X, pos.Y, pos.Z, Bridge.Now() + 60,
                              "avenge " + theirs + " " + relDead + " " + a.DeadName, Bridge.Now());
            if (!_bridge.AddToRoster(rec))
            {
                a.Stage = 3;
                return;
            }
            _bridge.Remember(rec, "Grief", 90, "you killed " + theirs + " " + relDead + ", " + a.DeadName, "you");
            _bridge.History(rec, "RODE IN from " + a.Town + " to avenge " + theirs + " " + relDead + " " + a.DeadName);
            _bridge.RosterChanged();
            a.RecordId = rec.Id;
            a.Stage = 3;
            Feed.Notice("Avenger", a.Name + " has come west for you. " + (a.Woman ? "She" : "He") + " means to settle " + theirs + " " + relDead + " " + deadFirst + "'s account.");   // a top-left update (it was enRAGE's white line)
            Log.Write("AVENGER " + a.Name + " is on enRAGE's roster now (" + rec.Id + "), hate " + rec.Hate);
        }
    }
}
