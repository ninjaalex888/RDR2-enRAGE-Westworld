// What the player does, read from the game: who he hurts and kills (and with what), what he loots, which
// crimes are seen, which towns and shops he walks into. Turned into deeds, deaths for the reveries,
// diaries to pick up, letters to collect, and the town talk waiting for him when he rides in. Killing or hurting
// someone who came at him (or was hostile to him) is self-defence: no crime, no witnesses, no rumour.
using System;
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using NemesisSystem;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private const uint EventDamaged = 402722103u;
        private const uint EventLootComplete = 1376140891u;
        private const uint EventCrimeConfirmed = 1924269094u;

        private sealed class Hit
        {
            public int At;
            public uint Weapon;
            public bool Fighting;   // the victim was fighting the player when hit
            public bool CameAtYou;  // ...and started it, or was hostile to him: self-defence, not a crime (set once, at the first hit)
            public bool Human;
            public string Why;
            public bool Done;
            public string RecordId; // who enRAGE says they are (read while alive: enRAGE forgets dead peds fast)
            public string AcqName;
        }

        private sealed class Pickup
        {
            public string PersonId, Name;
            public Vector3 Pos;
            public float Radius;
            public int Until;
            public string From;     // "body" or "camp"
        }

        private readonly Dictionary<int, Hit> _hits = new Dictionary<int, Hit>();
        private readonly Dictionary<int, int> _cameAtYouSeen = new Dictionary<int, int>();   // fighting him before he hurt them
        private readonly Dictionary<int, int> _aimedAt = new Dictionary<int, int>();         // he had a gun on them, when
        private readonly Dictionary<int, int> _youStarted = new Dictionary<int, int>();      // he hurt them before they came at him
        private int _nextDefenceScan, _lastBounty = -1, _lastRealCrimeAt = -1000000, _lastDefenceAt = -1000000, _lastOffenceAt = -1000000;
        private string _lastDefenceWho;
        private int _defendSince = -1, _bountyBase;
        private bool _wantedBefore, _defenceTaintNoted, _defenceWantedNoted;
        private Vector3 _defendAt;
        private uint _defendState;
        private readonly List<KeyValuePair<int, int>> _bountyHistory = new List<KeyValuePair<int, int>>();   // (when, bounty; negative = wanted)
        private readonly HashSet<string> _campOffered = new HashSet<string>();
        private Pickup _pickup;
        private int _nextPlaces, _nextDeaths, _lastCrimeAt;
        private string _lastCrimeKind;
        private uint _interiorType;
        private string _placeKind;
        private string _placeTown;
        private List<CampInfo> _camps;
        private Dictionary<uint, string> _interiorKinds;

        private static readonly string[] PostTowns = { "Valentine", "Rhodes", "Saint Denis", "Blackwater", "Strawberry", "Annesburg", "Van Horn", "Armadillo", "Tumbleweed", "Emerald Ranch", "Flatneck Station", "Riggs Station", "Benedict Point", "Lagras" };

        // ---- game events (every frame: they only live for one) ------------------------------------

        private unsafe void PollWorldEventsImpl(int now)
        {
            ShieldDefence(now);
            int count = SCRIPTS.GET_NUMBER_OF_EVENTS(0);
            if (count <= 0)
                return;
            int player = Game.Player.Character.Handle;
            ulong* p = stackalloc ulong[12];
            for (int i = 0; i < count; i++)
            {
                uint ev = SCRIPTS.GET_EVENT_AT_INDEX(0, i);
                if (ev != EventDamaged && ev != EventLootComplete && ev != EventCrimeConfirmed)
                    continue;
                for (int j = 0; j < 12; j++)
                    p[j] = 0;
                int size = ev == EventDamaged ? 9 : 3;
                if (!SCRIPTS.GET_EVENT_DATA(0, i, p, size))
                    continue;
                try
                {
                    if (ev == EventDamaged)
                    {
                        int victim = (int)p[0];
                        if ((int)p[1] == player && victim != 0 && victim != player)
                        {
                            Hit h;
                            if (!_hits.TryGetValue(victim, out h))
                            {
                                h = new Hit { Fighting = WasFight(victim, player) };
                                if (PED.IS_PED_HUMAN(victim))
                                {
                                    string why;
                                    h.Human = true;
                                    h.CameAtYou = CameAtYou(victim, player, out why);
                                    h.Why = why;
                                    if (h.CameAtYou)
                                        _lastDefenceWho = Director.Kind(new Ped(victim), !PED.IS_PED_MALE(victim));
                                    else
                                    {
                                        _lastOffenceAt = now;       // someone who hadn't come at him: any bounty now may be his own doing
                                        _youStarted[victim] = now;  // and however long they fight back, he started it
                                    }
                                    NemesisRecord who = _bridge.RecordAt(victim);
                                    h.RecordId = who != null ? who.Id : null;
                                    Acquaintance acq = who == null ? _bridge.AcquaintanceAt(victim) : null;
                                    h.AcqName = acq != null && !string.IsNullOrEmpty(acq.Name) ? acq.FullName : null;
                                }
                                _hits[victim] = h;
                            }
                            h.At = now;
                            h.Weapon = (uint)p[2];
                            if (h.CameAtYou)
                            {
                                Defending(now);   // every hit on them keeps the fight going, not just the first
                                _lastDefenceHitAt = now;
                            }
                        }
                    }
                    else if (ev == EventLootComplete)
                    {
                        if ((int)p[0] == player && (p[2] & 0xFFFFFFFF) != 0)
                            OnLooted((int)p[1], now);
                    }
                    else if ((int)p[1] == player)
                    {
                        OnCrime((uint)p[0], (int)p[2], now);
                    }
                }
                catch (Exception ex)
                {
                    Log.Detail("event " + ev + " failed: " + ex.Message);
                }
            }
            if (_lastDefenceHitAt == now)
                ShieldDefence(now);   // the first shot back: shielded this very frame, not the next
        }

        /// A fight, not a murder: they were fighting him, or armed and fighting anyone, or hostile to him
        /// (rival gangs, the law hunting him), or it's a story mission.
        private static bool WasFight(int victim, int player)
        {
            if (PED.IS_PED_IN_COMBAT(victim, player) || MISC.GET_MISSION_FLAG())
                return true;
            if (PED.IS_PED_IN_COMBAT(victim, 0) && WEAPON.IS_PED_ARMED(victim, 4))
                return true;
            return PED.GET_RELATIONSHIP_BETWEEN_PEDS(victim, player) == 5;   // hate: rival gangs and the like
        }

        /// Self-defence, decided at the first hit and never changed after: enRAGE saw them come at him, or they were
        /// fighting him before he hurt them (and he hadn't drawn on them first), or they're hostile to him (red: rival
        /// gangs and the like), or it's a story mission. A lawman never is: he's doing his job.
        private bool CameAtYou(int victim, int player, out string why)
        {
            why = null;
            if (Natives.IsLaw(new Ped(victim)) || _youStarted.ContainsKey(victim))
                return false;
            if (_bridge != null && _bridge.EnrageSaysCameAtYou(victim))
                why = "enRAGE saw them come at you";
            else if (_bridge != null && _bridge.EnrageFighter(victim))
                why = "one of enRAGE's fighters, red on your map";
            else if (_cameAtYouSeen.ContainsKey(victim))
                why = "they were fighting you before you hurt them";
            else if (PED.GET_RELATIONSHIP_BETWEEN_PEDS(victim, player) == 5)
                why = "hostile to you";
            else if (MISC.GET_MISSION_FLAG())
                why = "a story mission";
            return why != null;
        }

        /// A few times a second: who is fighting him that he hasn't hurt or drawn on (they came at him), and whether
        /// his bounty rose right after he fought off someone who did.
        private unsafe void DefenceScan(int now)
        {
            if (now < _nextDefenceScan)
                return;
            _nextDefenceScan = now + 400;
            Ped me = Game.Player.Character;
            int player = me.Handle;
            ulong slot = 0;   // out-parameters take 8 bytes
            if (PLAYER.GET_ENTITY_PLAYER_IS_FREE_AIMING_AT(Game.Player.Handle, (int*)&slot) && (int)slot != 0)
                _aimedAt[(int)slot] = now;
            slot = 0;
            if (PLAYER.GET_PLAYER_TARGET_ENTITY(Game.Player.Handle, (int*)&slot) && (int)slot != 0)
                _aimedAt[(int)slot] = now;
            Vector3 pos = me.Position;
            foreach (Ped ped in World.GetAllPeds())
            {
                int h = ped.Handle;
                if (h == player || _hits.ContainsKey(h) || _youStarted.ContainsKey(h) || _cameAtYouSeen.ContainsKey(h) || ENTITY.IS_ENTITY_DEAD(h) || !PED.IS_PED_HUMAN(h))
                    continue;
                if (!PED.IS_PED_IN_COMBAT(h, player) || ped.Position.DistanceTo(pos) > 150f || Natives.IsLaw(ped))
                    continue;
                int at;
                if (_aimedAt.TryGetValue(h, out at) && now - at < 10000)
                    continue;   // he drew on them first: they're answering him
                _cameAtYouSeen[h] = now;
            }
            foreach (int h in _cameAtYouSeen.Where(kv => now - kv.Value > 300000 || !ENTITY.DOES_ENTITY_EXIST(kv.Key)).Select(kv => kv.Key).ToList())
                _cameAtYouSeen.Remove(h);
            foreach (int h in _aimedAt.Where(kv => now - kv.Value > 30000).Select(kv => kv.Key).ToList())
                _aimedAt.Remove(h);
            foreach (int h in _youStarted.Where(kv => now - kv.Value > 300000 || !ENTITY.DOES_ENTITY_EXIST(kv.Key)).Select(kv => kv.Key).ToList())
                _youStarted.Remove(h);

            DefenceBounty(now);
        }

        /// While he fights off people who came at him (and for 90 s after his last hit or kill on one), the bounty and
        /// wanted level he had before the fight are kept (SelfDefenceNoBounty), unless he also hurt someone who hadn't
        /// come at him or did another crime from five minutes before it on: then the game's call stands. A fight that
        /// moves a long way, or into another state, starts again from the bounty there, so a bounty he owes elsewhere is
        /// never wiped. Story missions keep their own law. enRAGE does the same for its own outlaws (DefenceLaw decides).
        private void DefenceBounty(int now)
        {
            int me = Game.Player.Handle;
            int bounty = LAW.GET_BOUNTY(me);
            bool wanted = PLAYER.GET_PLAYER_WANTED_LEVEL(me) > 0;
            bool mission = MISC.GET_MISSION_FLAG();
            Vector3 pos = Game.Player.Character.Position;
            bool moved = _defendSince >= 0 && (pos.DistanceTo(_defendAt) > 300f || StateAt(pos) != _defendState);
            bool clearWanted;
            DefenceAct act = DefenceLaw.Decide(now, bounty, wanted, _defendSince, _lastDefenceAt, _bountyBase, _wantedBefore,
                                               _lastOffenceAt, _lastRealCrimeAt, mission, _cfg.SelfDefenceNoBounty, moved, out clearWanted);
            switch (act)
            {
                case DefenceAct.Mission:
                    _defendSince = -1;   // a story mission sets its own bounty and wanted level: hands off
                    _bountyHistory.Clear();
                    break;
                case DefenceAct.None:
                    if (_defendSince >= 0)
                        Log.Detail("SELF-DEFENCE: the fight is over");
                    _defendSince = -1;
                    _bountyHistory.Add(new KeyValuePair<int, int>(now, wanted ? -bounty - 1 : bounty));
                    _bountyHistory.RemoveAll(x => now - x.Key > 5000);
                    break;
                case DefenceAct.Moved:
                    Log.Write("SELF-DEFENCE: the fight has moved " + (StateAt(pos) != _defendState ? "into another state" : "a long way") +
                              ": from here the bounty is kept at what it is now ($" + bounty / 100 + ")");
                    _bountyBase = bounty;
                    _wantedBefore = wanted;
                    _defendAt = pos;
                    _defendState = StateAt(pos);
                    break;
                case DefenceAct.Tainted:
                    if (!_defenceTaintNoted && (bounty > _bountyBase || (wanted && !_wantedBefore)))
                    {
                        _defenceTaintNoted = true;
                        Log.Write("SELF-DEFENCE: you also hurt someone who hadn't come at you, or did another crime: the game's bounty stands ($" + bounty / 100 + ")");
                    }
                    break;
                case DefenceAct.Off:
                    if (bounty > _lastBounty)
                        Log.Write("SELF-DEFENCE: your bounty rose $" + _lastBounty / 100 + " -> $" + bounty / 100 + " while you fought off " + (_lastDefenceWho ?? "someone") +
                                  " who came at you (SelfDefenceNoBounty is off, so it stays)");
                    break;
                case DefenceAct.Restore:
                    if (bounty > _bountyBase)
                    {
                        LAW.SET_BOUNTY(me, _bountyBase);
                        Log.Write("SELF-DEFENCE: bounty $" + bounty / 100 + " -> $" + _bountyBase / 100 + ": you were fighting off " + (_lastDefenceWho ?? "someone") + " who came at you");
                        bounty = _bountyBase;
                    }
                    if (clearWanted)
                    {
                        PLAYER.CLEAR_PLAYER_WANTED_LEVEL(me);
                        if (!_defenceWantedNoted)
                        {
                            _defenceWantedNoted = true;
                            Log.Write("SELF-DEFENCE: wanted level cleared: you were fighting off " + (_lastDefenceWho ?? "someone") + " who came at you (kept clear while the fight lasts)");
                        }
                    }
                    break;
            }
            _lastBounty = bounty;
        }

        // the crimes the game would charge him with for fighting back: murder, assault (armed, unarmed, with a lasso),
        // threatening (aiming at them) and the disturbance of the shots. Never the ones against lawmen or animals.
        private static readonly uint[] DefenceCrimes = { 0xE28ECE7Eu, 0x0BADC882u, 0x5098CC5Au, 0x56EE5D5Au, 0x941C985Au, 0x5011F613u };
        private int _lastDefenceHitAt = -1000000, _lastShieldLog;
        private bool _shieldOn;

        /// Every frame: while he fights off people who came at him, the game isn't allowed to call it a crime, so no
        /// witness runs to report it and no bounty comes of it. Only right after he hits one of them, while one he hit
        /// is still alive, or while he aims at one; never once he has hurt anyone who hadn't come at him (from five
        /// minutes before the fight on) or a crime of his was confirmed; never in story missions (SelfDefenceNoBounty).
        private unsafe void ShieldDefence(int now)
        {
            bool on = false;
            if (_cfg.SelfDefenceNoBounty && !MISC.GET_MISSION_FLAG() && now - _lastOffenceAt > DefenceLaw.LookbackMs && now - _lastRealCrimeAt > DefenceLaw.LookbackMs &&
                (_defendSince < 0 || (_lastOffenceAt < _defendSince - DefenceLaw.LookbackMs && _lastRealCrimeAt < _defendSince - DefenceLaw.LookbackMs)))
            {
                on = now - _lastDefenceHitAt < 1500 || _hits.Values.Any(x => x.CameAtYou && !x.Done && now - x.At < 20000);
                if (!on)
                {
                    ulong slot = 0;   // out-parameters take 8 bytes
                    int aimed = PLAYER.GET_ENTITY_PLAYER_IS_FREE_AIMING_AT(Game.Player.Handle, (int*)&slot) ? (int)slot : 0;
                    if (aimed == 0)
                    {
                        slot = 0;
                        aimed = PLAYER.GET_PLAYER_TARGET_ENTITY(Game.Player.Handle, (int*)&slot) ? (int)slot : 0;
                    }
                    on = aimed != 0 && PED.IS_PED_HUMAN(aimed) && !Natives.IsLaw(new Ped(aimed)) && !_youStarted.ContainsKey(aimed) &&
                         (_cameAtYouSeen.ContainsKey(aimed) || PED.GET_RELATIONSHIP_BETWEEN_PEDS(aimed, Game.Player.Character.Handle) == 5 ||
                          (_bridge != null && (_bridge.EnrageSaysCameAtYou(aimed) || _bridge.EnrageFighter(aimed))));
                }
            }
            if (on)
                foreach (uint c in DefenceCrimes)
                    LAW.SUPPRESS_CRIME_THIS_FRAME(Game.Player.Handle, c, 0, 0, 0);
            if (on != _shieldOn && now - _lastShieldLog > 5000)
            {
                _lastShieldLog = now;
                Log.Detail("SELF-DEFENCE: " + (on ? "fighting back isn't a crime right now (no witness, no bounty)" : "the law sees everything again"));
            }
            _shieldOn = on;
        }

        /// A hit or a kill on someone who came at him: the fight starts (or goes on). Not in story missions.
        private void Defending(int now)
        {
            if (MISC.GET_MISSION_FLAG())
                return;
            StartDefence(now);
            _lastDefenceAt = now;
        }

        /// The first hit on someone who came at him: the fight starts, and what he owed before it is remembered (the
        /// lowest bounty of the last few seconds, as reports can lag the shot), with where it began.
        private void StartDefence(int now)
        {
            if (_defendSince >= 0 && now - _lastDefenceAt < DefenceLaw.WindowMs)
                return;
            _defendSince = now;
            _defenceTaintNoted = false;
            _defenceWantedNoted = false;
            _defendAt = Game.Player.Character.Position;
            _defendState = StateAt(_defendAt);
            int me = Game.Player.Handle;
            List<KeyValuePair<int, int>> recent = _bountyHistory.Where(x => now - x.Key <= 4000).ToList();
            if (recent.Count == 0)
            {
                bool w = PLAYER.GET_PLAYER_WANTED_LEVEL(me) > 0;
                int b = LAW.GET_BOUNTY(me);
                recent.Add(new KeyValuePair<int, int>(now, w ? -b - 1 : b));
            }
            _bountyBase = recent.Min(x => x.Value < 0 ? -x.Value - 1 : x.Value);
            _wantedBefore = recent[0].Value < 0;
            Log.Detail("SELF-DEFENCE: a fight with someone who came at you; bounty before it $" + _bountyBase / 100 + (_wantedBefore ? ", already wanted" : ""));
        }

        /// The state (New Hanover, Lemoyne...) a place is in: the game keeps the law by region.
        private static uint StateAt(Vector3 p)
        {
            return ZONE._GET_MAP_ZONE_AT_COORDS(p.X, p.Y, p.Z, 0);
        }

        /// Everyone he hurt in the last while came at him (and he hurt somebody).
        private bool OnlyDefending(int now, int ms)
        {
            List<Hit> recent = _hits.Values.Where(x => x.Human && now - x.At < ms).ToList();
            return recent.Count > 0 && recent.All(x => x.CameAtYou);
        }

        /// Checks the people the player hurt: who died, how.
        private void Deaths(int now)
        {
            DefenceScan(now);
            if (now < _nextDeaths || _hits.Count == 0)
                return;
            _nextDeaths = now + 400;
            int player = Game.Player.Character.Handle;
            foreach (KeyValuePair<int, Hit> kv in _hits.ToList())
            {
                int h = kv.Key;
                Hit hit = kv.Value;
                if (hit.Done || now - hit.At > 20000 || !ENTITY.DOES_ENTITY_EXIST(h))
                {
                    if (now - hit.At > 20000 || !ENTITY.DOES_ENTITY_EXIST(h))
                        _hits.Remove(h);
                    continue;
                }
                if (!ENTITY.IS_ENTITY_DEAD(h) || now - hit.At < 300)
                    continue;
                hit.Done = true;
                int source = PED.GET_PED_SOURCE_OF_DEATH(h);
                if (source != player && source != 0 && now - hit.At > 3000)
                    continue;
                uint cause = PED.GET_PED_CAUSE_OF_DEATH(h);
                if (!hit.Fighting && (MISC.GET_MISSION_FLAG() || PED.IS_PED_IN_COMBAT(h, player)))
                    hit.Fighting = true;   // a mission, or it turned into a fight after the first shot
                OnKilled(new Ped(h), hit, cause != 0 ? cause : hit.Weapon, now);
            }
        }

        private void OnKilled(Ped victim, Hit hit, uint weapon, int now)
        {
            int h = victim.Handle;
            if (hit.CameAtYou)
                Defending(now);   // the kill is what usually brings the bounty, and reports lag: the fight goes on from here
            if (Spawner.IsOurs(h) && !IsHermit(h) && !IsGatherer(h) && !_encounterPeds.Contains(h))
                return;           // the add-on's own stalkers and the like. The people of an encounter are judged like anyone: attackers came at you (no deed), the rest are killings
            Vector3 pos = victim.Position;
            string town = _bridge.TownAt(pos);
            string zone = ZoneAt(pos);
            long gameMin = Bridge.Now();
            if (!PED.IS_PED_HUMAN(h))
            {
                if (_cfg.Deeds && ENTITY.GET_IS_ANIMAL(h))
                {
                    string animal = AnimalName(victim);
                    if (animal != null)
                        _engine.OnDeed(Deeds.Make("hunt", animal, town, zone, gameMin, pos.X, pos.Y, pos.Z, 0), null, gameMin);
                }
                return;
            }
            if (Natives.IsChild(victim))
                return;
            string how = Weapons.Phrase(weapon);
            int bone = Natives.LastBone(h);
            bool headshot = bone == 21030 || bone == 31086 || ((BoneID)bone).ToString().Contains("HEAD");
            string place = town ?? zone;
            List<string> witnesses = WitnessesNear(pos, h, 35f);
            int seen = CountPeopleNear(pos, h, 35f);
            PostKilled(victim, seen);   // someone who kept a counter: the post passes on
            NemesisRecord rec = null;
            if (hit.RecordId == null || !_recById.TryGetValue(hit.RecordId, out rec))
                rec = _bridge.RecordAt(h);
            if (rec != null)
            {
                _engine.NoteDeath(rec.Id, place, how, headshot);
                bool defended = hit.CameAtYou && !Natives.IsLaw(victim);
                if (_cfg.Deeds && !defended)
                    _engine.OnDeed(Deeds.Make("namedkill", rec.Name, town, zone, gameMin, pos.X, pos.Y, pos.Z, seen), witnesses, gameMin);
                Log.Write("KILLED " + rec.Name + (how != null ? " with " + how : "") + (headshot ? " (head)" : "") + " near " + place +
                          (defended ? "; self-defence (" + hit.Why + "): no crime noted" : ""));
                return;
            }
            bool woman = !PED.IS_PED_MALE(h);
            bool law = Natives.IsLaw(victim);
            Acquaintance a = _bridge.AcquaintanceAt(h);
            string known = a != null && !string.IsNullOrEmpty(a.Name) ? a.FullName : hit.AcqName;
            string label = law ? "a lawman" : Director.Kind(victim, woman);
            bool thief = _thieves.Remove(h);
            string beggarName = null;
            bool begged = !thief && DiedBegging(h, now, out beggarName);
            if (begged && known == null)
                known = beggarName;
            string kind = Deeds.KillKind(law, thief, begged, hit.CameAtYou, hit.Fighting);
            if (kind == null)
                Log.Write("SELF-DEFENCE: killed " + (known ?? label) + " near " + place + ", who came at you (" + hit.Why + "): no crime noted, no witnesses, no rumour");
            if (_cfg.Deeds && kind != null)
            {
                Deed d = Deeds.Make(kind, thief ? "a pickpocket" : begged ? label : known ?? label, town, zone, gameMin, pos.X, pos.Y, pos.Z, seen);
                Deed last = _store.Deeds.LastOrDefault();
                if (last != null && last.Kind == kind && gameMin - last.At <= 10 && last.Where == d.Where)
                {
                    last.Count++;   // two men in the same fight
                    last.Weight = Math.Min(100, last.Weight + 8);
                }
                else
                {
                    _engine.OnDeed(d, witnesses, gameMin);
                }
            }
            // story missions and story characters stay out of the reveries: nobody from a mission walks again
            if (_cfg.Reveries && !MISC.GET_MISSION_FLAG() && !Natives.StoryModel(victim))
            {
                Fallen f = new Fallen
                {
                    Id = Ids.New(), Model = ENTITY.GET_ENTITY_MODEL(h), Woman = woman, Name = known,
                    Label = label, Town = town, Zone = zone, X = pos.X, Y = pos.Y, Z = pos.Z, Weapon = how, Headshot = headshot, At = gameMin, Rolled = true,
                    Seen = seen, Begged = begged
                };
                if (!law && _rng.Next(100) < _cfg.StrangerReturnChance)
                    f.ReturnAt = gameMin + 1440 + _rng.Next(1440 * 5);
                _store.Fallen.Add(f);
                Log.Detail("FALLEN " + (f.Name ?? f.Label) + " near " + place + (f.ReturnAt > 0 ? " - will come back" : ""));
            }
        }

        private void OnLooted(int looted, int now)
        {
            if (looted == 0 || !ENTITY.DOES_ENTITY_EXIST(looted) || !PED.IS_PED_HUMAN(looted))
                return;
            NemesisRecord rec = _bridge.RecordAt(looted);
            if (rec != null && _cfg.FindDiaries)
            {
                PersonState st = _store.Find(rec.Id);
                if (st != null && st.Diary.Count > 0 && !st.DiaryFound)
                {
                    _pickup = new Pickup { PersonId = rec.Id, Name = rec.Name, Pos = new Ped(looted).Position, Radius = 5f, Until = now + 30000, From = "body" };
                    Hint("You find a diary on " + rec.Name + ".  [" + _readerKey + "] take it", 30000);
                }
            }
        }

        private void OnCrime(uint crime, int witness, int now)
        {
            string kind = CrimeKind(crime);
            if (kind == null)
            {
                // not one RealNPCs knows (a killing, perhaps): during a fight in self-defence, the log says it was seen
                if (_defendSince >= 0 && now - _lastDefenceAt < DefenceLaw.WindowMs)
                    Log.Write("SELF-DEFENCE: the game confirmed a crime 0x" + crime.ToString("X8") + " during the fight with someone who came at you");
                return;
            }
            if (kind == "assault" && OnlyDefending(now, 30000))
            {
                Log.Write("SELF-DEFENCE: the game called it an assault, but everyone you hurt just now came at you: no crime noted");
                return;
            }
            _lastRealCrimeAt = now;
            if (!_cfg.Deeds)
                return;
            if (kind == _lastCrimeKind && now - _lastCrimeAt < 120000)
                return;
            _lastCrimeKind = kind;
            _lastCrimeAt = now;
            Ped me = Game.Player.Character;
            Vector3 pos = me.Position;
            long gameMin = Bridge.Now();
            string detail = kind == "robbery" ? (_placeKind != null ? "the " + PlaceName(_placeKind) : Voice.Pick(now.ToString(), "a traveller", "a farmer", "a townsman"))
                : kind == "assault" || kind == "kidnap" ? "a stranger" : null;
            int seen = Math.Max(witness != 0 ? 1 : 0, CountPeopleNear(pos, 0, 35f));
            _engine.OnDeed(Deeds.Make(kind, detail, _bridge.TownAt(pos), ZoneAt(pos), gameMin, pos.X, pos.Y, pos.Z, seen), WitnessesNear(pos, 0, 35f), gameMin);
        }

        private readonly Dictionary<string, long> _flirtNoted = new Dictionary<string, long>();

        /// Sweet-talking someone is a deed too: whoever saw it talks, and it travels.
        private void NoteFlirt(Ped ped, string who)
        {
            if (!_cfg.Deeds || _engine == null || ped == null || who == null)
                return;
            long g = Bridge.Now();
            long last;
            if (_flirtNoted.TryGetValue(who, out last) && g - last < 240)
                return;
            _flirtNoted[who] = g;
            Vector3 pos = ped.Position;
            _engine.OnDeed(Deeds.Make("flirt", who, _bridge.TownAt(pos), ZoneAt(pos), g, pos.X, pos.Y, pos.Z, CountPeopleNear(pos, ped.Handle, 20f)),
                WitnessesNear(pos, ped.Handle, 20f), g);
        }

        private static string CrimeKind(uint crime)
        {
            switch (crime)
            {
                case 0x72ADE410: return "theft";
                case 0xBE3A5838: return "horsetheft";
                case 0x82F7E4A2: return "horsetheft";
                case 0xA3BEDE4C: return "robbery";
                case 0x6A1ADE3D: return "bankrobbery";
                case 0xFC738E61: return "stagecoach";
                case 0x647D2A5A: return "trainrobbery";
                case 0x0BADC882: return "assault";
                case 0x5098CC5A: return "assault";
                case 0x55AD2BEB: return "looting";
                case 0x971EA5AF: return "graverobbing";
                case 0x98F908DB: return "kidnap";
                case 0x68134DC7: return "arson";
                case 0xA2FF1145: return "cheating";
                case 0xA54C77E0: return "theft";
                default: return null;
            }
        }

        private List<string> WitnessesNear(Vector3 pos, int except, float radius)
        {
            List<string> ids = new List<string>();
            foreach (Ped p in World.GetAllPeds())
            {
                int h = p.Handle;
                if (h == except || h == Game.Player.Character.Handle || ENTITY.IS_ENTITY_DEAD(h) || p.Position.DistanceTo(pos) > radius)
                    continue;
                NemesisRecord r = _bridge.RecordAt(h);
                if (r != null && r.IsAlive && r.Id != null)
                    ids.Add(r.Id);
            }
            return ids;
        }

        private static int CountPeopleNear(Vector3 pos, int except, float radius)
        {
            int n = 0;
            int me = Game.Player.Character.Handle;
            foreach (Ped p in World.GetAllPeds())
            {
                int h = p.Handle;
                if (h != except && h != me && !ENTITY.IS_ENTITY_DEAD(h) && PED.IS_PED_HUMAN(h) && p.Position.DistanceTo(pos) <= radius)
                    n++;
            }
            return n;
        }

        private static string AnimalName(Ped p)
        {
            string m = Natives.ModelName(p);
            if (!m.StartsWith("a_c_") || m.Contains("horse") || m.Contains("dog") || m.Contains("cat_") || m.Contains("fish"))
                return null;
            string n = m.Substring(4);
            int u = n.IndexOf('_');
            n = u > 0 ? n.Substring(0, u) : n;
            switch (n)
            {
                case "buck": return "buck";
                case "bearblack": return "black bear";
                case "bighornram": return "bighorn ram";
                case "cranewhooping": return "whooping crane";
                case "goosecanada": return "goose";
                case "turkeywild": return "wild turkey";
                case "snakeblacktailrattle": case "snakeferdelance": case "snakeredboa": case "snakewater": return "snake";
                case "lionmangy": return "mountain lion";
                case "cougar": case "panther": return "big cat";
                case "boarlegendary": return "boar";
                default: return n;
            }
        }

        private string ZoneAt(Vector3 pos)
        {
            string z = _bridge.ZoneName(pos);
            return z == "the wilds" ? null : z;
        }

        // ---- places ---------------------------------------------------------------------------------

        private void Places(int now)
        {
            Deaths(now);
            if (now < _nextPlaces)
                return;
            _nextPlaces = now + 1500;
            Ped me = Game.Player.Character;
            Vector3 pos = me.Position;
            string town = _bridge.TownAt(pos);
            if (town != _placeTown)
            {
                string old = _placeTown;
                _placeTown = town;
                _town = town;
                _engine.CurrentTown = town;
                if (town != null)
                    EnteredTown(town, old);
            }
            uint interior = Natives.InteriorType(me.Handle);
            if (interior != _interiorType)
            {
                _interiorType = interior;
                _placeKind = interior != 0 ? InteriorKind(interior) : null;
                if (_placeKind != null && _cfg.Deeds)
                {
                    long g = Bridge.Now();
                    _engine.OnDeed(Deeds.Make(_placeKind, null, town, ZoneAt(pos), g, pos.X, pos.Y, pos.Z, CountPeopleNear(pos, 0, 25f)), WitnessesNear(pos, 0, 25f), g);
                }
            }
            if (_cfg.FindDiaries && interior == 0)
                CampDiary(pos, now);
        }

        private void EnteredTown(string town, string from)
        {
            long now = Bridge.Now();
            long last;
            bool firstVisit = !_store.TownVisits.ContainsKey(town);
            bool fresh = !_store.TownVisits.TryGetValue(town, out last) || now - last > 720;
            _store.TownVisits[town] = now;
            bool settlement = Rumours.Towns.ContainsKey(town);   // enRAGE also names regions ("the Heartlands"); only towns get a welcome
            if (fresh && settlement)
                QueueArrival(town, firstVisit);
            if (fresh && settlement && _cfg.Deeds)
            {
                Vector3 pos = Game.Player.Character.Position;
                _engine.OnDeed(Deeds.Make("visit", town, town, null, now, pos.X, pos.Y, pos.Z, 0), null, now);
            }
            if (_cfg.RumoursTravel)
                foreach (string line in _engine.TownGossip(town, 2))
                    News("In " + town, line);
            if (_cfg.Letters && PostTowns.Contains(town))
            {
                List<Paper> waiting = _store.Papers.Where(p => !p.Delivered && (p.Town == null || p.Town == town)).ToList();
                if (waiting.Count > 0)
                {
                    foreach (Paper p in waiting)
                    {
                        p.Delivered = true;
                        p.Town = town;
                        p.At = now;
                    }
                    string what = waiting.Count == 1 ? "a letter" : Voice.Number(waiting.Count) + " letters";
                    News("Post office", "The clerk in " + town + " had " + what + " for you" + (waiting.Count == 1 && waiting[0].From != null ? ", from " + waiting[0].From : "") + ". [" + _readerKey + "] to read.");
                    Log.Write("LETTERS collected in " + town + ": " + string.Join(", ", waiting.Select(p => p.Title)));
                }
            }
        }

        private string InteriorKind(uint type)
        {
            if (_interiorKinds == null)
            {
                _interiorKinds = new Dictionary<uint, string>();
                Action<string, string[]> add = (kind, names) =>
                {
                    foreach (string n in names)
                        _interiorKinds[Natives.Hash(n)] = kind;
                };
                add("saloon", new[] { "arm_saloon_int", "bla_saloon_int", "eme_saloon_int", "rho_saloon_int", "tum_saloon_int", "val_saloon_int", "val_saloon2_int", "van_saloon_int", "new_saloon01_int_main", "new_saloon02_int", "new_gamble_int" });
                add("genstore", new[] { "arm_genstore_int", "bla_genstore_int", "eme_genstore_int", "rho_genstore_int", "str_genst_int", "tum_genstore_int", "val_genstore", "new_grocery_int", "dis_tradepost_int" });
                add("gunsmith", new[] { "ann_gunsmith_int", "rho_gunsmith_int", "tum_gunsmith_int", "val_gunsmith_int_main", "new_gunshop_int" });
                add("bank", new[] { "arm_bank_int", "bla_bank_int", "rho_bank_int", "val_bank_int_main", "new_com_bank_int" });
                add("sheriff", new[] { "ann_jail_int", "arm_sheriff_int", "bla_jail_int", "rho_sheriff_int", "str_jailpost_int", "tum_jail_int", "val_jail_int", "new_police_int", "bur_calaboose_int", "old_jailsmth_int" });
                add("doctor", new[] { "str_doctor_int", "val_doctor_int", "new_res_doctor_int", "new_pharmacy_int" });
                add("barber", new[] { "bla_barber_int", "new_barber_int" });
                add("bath", new[] { "ann_roomnbath_int", "tum_roomnbath_int", "van_roomnbath_int" });
                add("hotel", new[] { "val_hotel_int_main", "van_hotel_int", "new_hotel_3rd_floor_int" });
                add("station", new[] { "arm_trainstation_int", "bla_train_int", "rho_train_int", "val_trainstn_int", "wal_trainstation_int", "ann_depot_int", "eme_depot_int", "van_depot_int", "swa_depot01_int", "new_traindepot_int", "the_ticketoffice_int", "rig_station_int" });
                add("tailor", new[] { "bla_tailor_int", "new_tailor01_int_main" });
                add("fence", new[] { "bla_fenceshop_int", "rho_fenceshop_int", "van_fenceshop_int", "new_shady_shop_int" });
                add("theatre", new[] { "bla_theater_int", "new_theater_int", "val_magiclantern_int", "new_magic_int" });
                add("photo", new[] { "bla_photo_int", "new_photo_int" });
                add("church", new[] { "dis_1rmchurch_int" });
                add("stable", new[] { "bra_stable_int", "old_stable_int", "val_stable_int", "jum_horseshop_int", "hrs_02_shop01_int" });
                add("store", new[] { "new_exotic_int", "van_exoticgoods_int", "new_art_int" });
            }
            string k;
            return _interiorKinds.TryGetValue(type, out k) ? k : null;
        }

        private static string PlaceName(string kind)
        {
            switch (kind)
            {
                case "genstore": return "general store";
                case "gunsmith": return "gunsmith";
                case "bank": return "bank";
                case "saloon": return "saloon";
                case "tailor": return "tailor";
                case "doctor": return "doctor";
                case "barber": return "barber";
                case "fence": return "fence";
                case "station": return "station";
                case "store": return "shop";
                default: return kind;
            }
        }

        // ---- diaries -------------------------------------------------------------------------------

        private void CampDiary(Vector3 pos, int now)
        {
            if (_camps == null)
                _camps = _bridge.Camps();
            if (_camps.Count == 0 || (_pickup != null && now < _pickup.Until))
                return;
            CampInfo camp = _camps.FirstOrDefault(c => c.Pos.DistanceTo2D(pos) < 22f && Math.Abs(c.Pos.Z - pos.Z) < 15f);
            if (camp == null || camp.Gang == null)
                return;
            string key = camp.Id + "|" + Bridge.Now() / 1440;
            if (_campOffered.Contains(key) || PED.IS_PED_IN_COMBAT(Game.Player.Character.Handle, 0))
                return;
            _campOffered.Add(key);
            NemesisRecord owner = (_bridge.Roster() ?? new List<NemesisRecord>()).Where(r => r != null && r.Gang == camp.Gang && r.Id != null)
                .Select(r => new { r, st = _store.Find(r.Id) }).Where(x => x.st != null && x.st.Diary.Count >= 2 && !x.st.DiaryFound)
                .OrderBy(x => x.r.IsAlive ? 1 : 0).Select(x => x.r).FirstOrDefault();
            if (owner == null)
                return;
            _pickup = new Pickup { PersonId = owner.Id, Name = owner.Name, Pos = camp.Pos, Radius = 30f, Until = now + 60000, From = "camp" };
            Hint("Among the bedrolls lies a worn diary.  [" + _readerKey + "] take it", 20000);
        }

        private void Pickups(int now)
        {
            if (_pickup != null && (now > _pickup.Until || Game.Player.Character.Position.DistanceTo(_pickup.Pos) > _pickup.Radius + 10f))
            {
                _pickup = null;
                _hint = null;
            }
        }

        /// The reader key with a diary at hand takes it, and opens it.
        private bool TakePickup()
        {
            Pickup p = _pickup;
            if (p == null || Game.GameTime > p.Until || Game.Player.Character.Position.DistanceTo(p.Pos) > p.Radius)
                return false;
            _pickup = null;
            _hint = null;
            PersonState st = _store.Find(p.PersonId);
            if (st == null || st.Diary.Count == 0)
                return false;
            st.DiaryFound = true;
            Paper diary = new Paper
            {
                Id = Ids.New(), Kind = "diary", Title = "The diary of " + p.Name, From = p.Name, FromId = p.PersonId,
                Body = string.Join("\n\n", st.Diary.Select(d => d.Text)), At = Bridge.Now(), Delivered = true
            };
            _store.Papers.Add(diary);
            Log.Write("DIARY taken from " + p.Name + "'s " + p.From + " (" + st.Diary.Count + " entries)");
            News("Papers", "You took " + p.Name + "'s diary.");
            if (_cfg.Deeds && p.From == "body")
            {
                Vector3 pos = Game.Player.Character.Position;
                long g = Bridge.Now();
                _engine.OnDeed(Deeds.Make("looting", null, _bridge.TownAt(pos), ZoneAt(pos), g, pos.X, pos.Y, pos.Z, CountPeopleNear(pos, 0, 30f)), WitnessesNear(pos, 0, 30f), g);
            }
            List<Paper> papers = _store.Papers.Where(x => x.Delivered).OrderByDescending(x => x.At).ToList();
            _reader.Show(papers, papers.IndexOf(diary));
            return true;
        }
    }
}
