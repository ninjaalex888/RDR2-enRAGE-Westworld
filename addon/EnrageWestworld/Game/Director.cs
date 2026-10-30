// Stages a Scene with real people standing near the player: turns them to each other, shows each line as a
// subtitle, plays a matching voiced line from the game in the speaker's own voice, shoves, fists, draws,
// kneels, drinks, walks off. Stops the moment anything goes wrong (someone dies, gets drawn into a fight with
// the player, the player draws on them, or the player leaves). The player can step in: Intrude().
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    internal sealed class Director
    {
        private sealed class Live
        {
            public Scene Scene;
            public Ped[] Cast;
            public string[] Names;
            public string BarText;               // "Nearby: ..." for the ticker, made once (TickerLine)
            public bool[] Introduced;
            public bool[] Women;
            public string[] Kinds;
            public int Index;
            public int NextAt;
            public int StartedAt;
            public bool Fighting;
            public int FightUntil;               // until blows start (FightStartMs), then the fight's own length from then
            public bool FightOn;                 // they're at it
            public int FightA, FightB, FightAskedAt, FightOrderedAt, FightMs;
            public Dictionary<int, bool[]> FightAttrs;   // each fighter's own combat attributes, put back at the end
            public int[] StartHealth;
            public string Seed;
            public Action Done;
            public SceneCast Info;
            public bool Intruded;
            public bool[] Aimed = new bool[3];   // guns out in a standoff: put away if the scene is cut short
            public int MeetUntil;                // "meet": B is walking over; nothing is said until they're close, or give up then
            public bool Slamming;                // "slam": the rough move is playing (WestworldScript.Roughs.cs); nothing else until it's done
            public bool WasFighting;             // a "fight" was started: at the end both are let go of it, however it ended
            public int MeetWalker = 1;           // who walks over in "meet" (the second, or the first for trouble)
            public int MeetGoAt;                 // the walker was sitting or leaning: he sets off once he's up (0: on his way)
            public bool Downed;                  // "passout" played: he's lying there until "cometo", or the end gets him up
            public bool MadeDrunk;               // A was given a drunk voice for the scene
            public bool HappenedSent;            // the scene's defining moment has been reported (Happened)
            public int ReactAt;                  // when the one spoken to reacts to the line (0: no reaction due)
            public int ReactBy, ReactTo;         // who reacts, and to whom
            public string ReactTone;             // the tone of the line they react to
            public int Sat = -1;                 // "sitdown": who is sitting on the ground after a fall, until "getup" (or the end gets them up)
            public int ApproachUntil;            // "approach" by the third: the next beat waits until they're at the one they walk to, or goes on then
            public int ApproachTo;               // whom the third walks to
            public int ApproachGoAt;             // the third was sitting or leaning: they set off once they're up (0: on their way)
            public int ApproachCheckAt;          // when the walk was last seen getting nearer, or last sent again
            public float ApproachBest;           // the nearest they've been
        }

        /// What one of the cast does about a line said to them, or about the slam ("hurt"), for the game layer to play.
        public sealed class Reaction
        {
            public Ped By, To;
            public string Tone, Temper, ByName, ToName, Seed;
            public bool Woman;
            public bool Known;      // the two have a tie (the speaker is no stranger to the one reacting)
            public int Warm;        // the tie's warmth, -3..3 (0 none)
        }

        internal static readonly uint Crouch = Natives.Hash("WORLD_HUMAN_CROUCH_INSPECT");   // the crouch over something on the ground (also the night visitor's)
        private Live _live;
        private int _player;
        private Action<string, string, bool, bool, int> _say;

        /// Called for actions the director doesn't know ("cb:reveal"), with the part after "cb:".
        public Action<string> Custom;

        /// "slam": the game layer plays the rough move, attacker then victim; false if it can't (then it's a shove).
        public Func<Ped, Ped, bool> Slam;
        /// While the rough move is still playing.
        public Func<bool> SlamGoing;
        /// The scene ended while the move played: stop it.
        public Action SlamStop;
        /// How the move came out once it's over: "rough" (it played), "shove" (it couldn't, so a shove), or null (nothing).
        public Func<string> SlamResult;
        /// The scene's defining moment really happened, once per scene: "rough" (the move began), "shove" (it couldn't,
        /// so a shove), "drunk" (he went down), "lift" (the thief walked off with it). Ties for these are noted then.
        public Action<string> Happened;
        /// The one spoken to reacts (WestworldScript.Reactions.cs); null: nobody reacts.
        public Action<Reaction> React;
        /// "meet" gave up: the two never got close enough, so nothing happened between them.
        public Action<Ped, Ped> NeverMet;
        /// Asked as the third sets off to walk over (the one who steps in, a friend, a spouse): why they mayn't be moved
        /// now, or null. They were free when the scene was built; a story mission, enRAGE or a fight may have taken them
        /// up since (night 2026-10-29: the mind moves people, checked at the roll and again at the moment).
        public Func<Ped, string> ThirdFree;

        // the drunk who calls a man out (Scenes.Drunk): the game's own drunk-dueler moves, one ped at a time
        public const string DrunkDict = "script_re@drunk_dueler";

        public bool Busy { get { return _live != null; } }
        public string LastSummary { get; private set; }
        public string CurrentKind { get { return _live != null ? _live.Scene.Kind : null; } }

        /// Is this ped one of the cast of the scene running now?
        /// The one who started a street fight, while the two are at it, and the man he is at (0 when none): the bully the
        /// player may step in on (WestworldScript.Watch.cs; Lex, 2026-10-30).
        public int FightStarter(out int against)
        {
            against = 0;
            Live l = _live;
            if (l == null || !l.Fighting || !l.FightOn || l.FightA == 0)
                return 0;
            against = l.FightB;
            return l.FightA;
        }

        /// The street scene playing now, for the bar at the bottom: what it is and who is in it, and where; null when none.
        public string TickerLine(out Vector3 at)
        {
            at = default(Vector3);
            Live l = _live;
            if (l == null || l.Scene == null || string.IsNullOrEmpty(l.Scene.Summary) || l.Cast == null || l.Cast.Length == 0 || l.Cast[0] == null || !l.Cast[0].Exists())
                return null;
            at = l.Cast[0].Position;
            if (l.BarText == null)
            {
                string who = l.Names != null ? string.Join(" and ", l.Names.Take(2).Where(n => !string.IsNullOrEmpty(n))) : "";
                l.BarText = Grammar.UpperFirst(l.Scene.Summary) + (who.Length > 0 ? " (" + who + ")" : "");
            }
            return l.BarText;
        }

        public bool Involves(int handle)
        {
            Live l = _live;
            return l != null && handle != 0 && l.Cast.Any(p => p != null && p.Handle == handle);
        }

        private readonly Dictionary<int, int> _leaving = new Dictionary<int, int>();   // handle -> until: walking off together ("leavewith")

        /// Still walking off together after a scene's "leavewith" (the take-away, night 2026-10-29): busy for everything
        /// else for 30 s, though the scene itself has ended (the collision audit: an onlooker's wander or a wake act cut it).
        public bool Leaving(int handle)
        {
            int until;
            if (handle == 0 || !_leaving.TryGetValue(handle, out until))
                return false;
            int now = Game.GameTime;
            if (now < until && until - now <= LeaveHoldMs)
                return true;
            _leaving.Remove(handle);
            return false;
        }

        private const int LeaveHoldMs = 30000;

        public Ped CastAt(int i)
        {
            Live l = _live;
            return l != null && i >= 0 && i < l.Cast.Length ? l.Cast[i] : null;
        }

        public string NameAt(int i)
        {
            Live l = _live;
            return l != null && i >= 0 && i < l.Names.Length ? l.Names[i] : null;
        }

        public static bool OldModel(Ped p)
        {
            string m = Natives.ModelName(p);
            return m.Contains("_m_o_") || m.Contains("_f_o_");
        }

        public static string Kind(Ped p, bool woman)
        {
            string m = Natives.ModelName(p);
            if (m.Contains("_m_o_")) return "an old-timer";
            if (m.Contains("_f_o_")) return "an old woman";
            if (m.Contains("upperclass") || m.Contains("fancy") || m.Contains("highsociety")) return woman ? "a lady" : "a gentleman";
            if (m.Contains("prostitute") || m.Contains("whore")) return "a working girl";
            if (m.Contains("laborer") || m.Contains("worker") || m.Contains("foreman")) return woman ? "a working woman" : "a labourer";
            if (m.Contains("farm") || m.Contains("rancher")) return woman ? "a farm wife" : "a farmhand";
            if (m.Contains("miner")) return "a miner";
            if (m.Contains("dock") || m.Contains("boatcrew")) return "a dockhand";
            if (m.Contains("cardgame") || m.Contains("gambl")) return "a card player";
            if (m.Contains("slums") || m.Contains("rough") || m.Contains("criminal") || m.Contains("thug") || m.Contains("hillbilly")) return woman ? "a hard-looking woman" : "a roughneck";
            if (m.Contains("survivalist") || m.Contains("hunter")) return "a trapper";
            if (m.Contains("townfolk")) return woman ? "a townswoman" : "a townsman";
            return woman ? "a woman" : "a man";
        }

        /// Starts a scene with these people (2 or 3). names: acquaintance names when enRAGE knows them, else null.
        public bool Start(Scene s, List<Ped> cast, string[] knownNames, string seed, Action done, SceneCast info = null)
        {
            if (s == null || cast.Count < s.Cast || _live != null)
                return false;
            Live l = new Live
            {
                Scene = s, Cast = cast.Take(s.Cast).ToArray(), Names = new string[s.Cast], Introduced = new bool[s.Cast], Women = new bool[s.Cast],
                Kinds = new string[s.Cast], NextAt = Game.GameTime + 600, StartedAt = Game.GameTime, Seed = seed, Done = done, Info = info ?? new SceneCast()
            };
            for (int i = 0; i < l.Cast.Length; i++)
            {
                int h = l.Cast[i].Handle;
                l.Women[i] = !PED.IS_PED_MALE(h);
                l.Kinds[i] = Kind(l.Cast[i], l.Women[i]);
                l.Names[i] = knownNames != null && i < knownNames.Length && knownNames[i] != null ? knownNames[i] : Scenes.FirstName(l.Women[i], seed + i);
                PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, true);
            }
            _live = l;
            foreach (string d in Gestures.Dicts)
                STREAMING.REQUEST_ANIM_DICT(d);   // body language for the lines (Gestures), loaded by the time they're said
            if (s.Beats.Any(x => x.Act == "challenge" || x.Act == "passout" || x.Act == "cometo"))
            {
                STREAMING.REQUEST_ANIM_DICT(DrunkDict);
                AUDIO.SET_PED_IS_DRUNK(l.Cast[0].Handle, true);   // he sounds it, too
                l.MadeDrunk = true;
            }
            Log.Write("SCENE " + s.Kind + ": " + string.Join(" & ", l.Names));
            return true;
        }

        private static string Temper(Live l, int i)
        {
            return l.Info != null && l.Info.Tempers != null && i < l.Info.Tempers.Length ? l.Info.Tempers[i] : null;
        }

        /// A townsfolk gesture that fits the line's tone (Core/Gestures.cs), on the upper body while they say it,
        /// about half the time; never for someone sitting or leaning in a scenario, or before the set has loaded.
        private static void Gesture(int h, string tone, bool woman, string seed)
        {
            if (!Voice.Chance(seed + "gesture", 55) || PED.IS_PED_USING_ANY_SCENARIO(h))
                return;
            string clip;
            string dict = Gestures.For(tone, woman, seed, out clip);
            if (!STREAMING.HAS_ANIM_DICT_LOADED(dict))
                return;
            TASK.TASK_PLAY_ANIM(h, dict, clip, 4f, -4f, -1, 8 | 16, 0f, false, 0, false, "", false);
        }

        /// The two swapped places (B won the fight the script gave to A): the lines still to come swap their names too,
        /// so nobody calls the winner by his own name. A beat that follows its people (`Beat.Follows`, the ladder's)
        /// swaps its Who and To instead and keeps its line: the man the ladder had speaking still speaks it, by name.
        private static void SwapNamesInLines(Live l, string x, string y)
        {
            Regex both = string.IsNullOrEmpty(x) || string.IsNullOrEmpty(y) || x == y ? null : new Regex(@"\b(" + Regex.Escape(x) + "|" + Regex.Escape(y) + @")\b");
            for (int j = l.Index; j < l.Scene.Beats.Count; j++)
            {
                Beat b = l.Scene.Beats[j];
                if (b.Follows)
                {
                    if (b.Who < 2)
                        b.Who = 1 - b.Who;
                    if (b.To == 0 || b.To == 1)
                        b.To = 1 - b.To;
                }
                else if (b.Line != null && both != null)
                    b.Line = both.Replace(b.Line, m => m.Value == x ? y : x);
            }
        }

        public void Abort(string why)
        {
            if (_live == null)
                return;
            Log.Write("SCENE stopped after " + _live.Index + " of " + _live.Scene.Beats.Count + " beats: " + why);
            End();
        }

        private void End()
        {
            Live l = _live;
            _live = null;
            if (l.Slamming)
                SlamStop?.Invoke();
            Ped a0 = l.Cast.Length > 0 ? l.Cast[0] : null;
            if (a0 != null && a0.Exists() && !ENTITY.IS_ENTITY_DEAD(a0.Handle))
            {
                // a drunk still lying where he fell gets up as the scene ends, whatever ended it, and sobers in his voice
                if (l.Downed && STREAMING.HAS_ANIM_DICT_LOADED(DrunkDict) && ENTITY.IS_ENTITY_PLAYING_ANIM(a0.Handle, DrunkDict, "pass_out_fwd_v1_drunk", 3))
                    TASK.TASK_PLAY_ANIM(a0.Handle, DrunkDict, "pass_out_fwd_v1_exit_drunk", 4f, -4f, -1, 0, 0f, false, 0, false, "", false);
                if (l.MadeDrunk)
                    AUDIO.SET_PED_IS_DRUNK(a0.Handle, false);
            }
            // someone still sitting where they fell gets up as the scene ends, whatever ended it, and goes on their way
            if (l.Sat >= 0 && l.Sat < l.Cast.Length && l.Cast[l.Sat] != null && l.Cast[l.Sat].Exists() && !ENTITY.IS_ENTITY_DEAD(l.Cast[l.Sat].Handle))
                TASK.CLEAR_PED_TASKS(l.Cast[l.Sat].Handle, true, false);
            if ((l.Scene.Kind == "help" || l.Scene.Kind == "passby") && l.Cast.Length > 1 && l.Cast[1] != null && l.Cast[1].Exists() && !ENTITY.IS_ENTITY_DEAD(l.Cast[1].Handle) &&
                !PED.IS_PED_USING_ANY_SCENARIO(l.Cast[1].Handle))
                TASK.TASK_WANDER_STANDARD(l.Cast[1].Handle, 10f, 10);
            // a third still walking over when it ended (the player left, someone in it was hurt) is let go on their way
            if (l.ApproachUntil > 0 && l.Cast.Length > 2 && l.Cast[2] != null && l.Cast[2].Exists() && !ENTITY.IS_ENTITY_DEAD(l.Cast[2].Handle))
                TASK.TASK_WANDER_STANDARD(l.Cast[2].Handle, 10f, 10);
            if (l.FightAttrs != null)
                foreach (KeyValuePair<int, bool[]> fa in l.FightAttrs)
                    if (ENTITY.DOES_ENTITY_EXIST(fa.Key))
                        for (int a = 0; a < FightAttrIds.Length; a++)
                            PED.SET_PED_COMBAT_ATTRIBUTES(fa.Key, FightAttrIds[a], fa.Value[a]);   // their own again
            for (int i = 0; i < l.Cast.Length; i++)
            {
                Ped p = l.Cast[i];
                if (p != null && p.Exists())
                {
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(p.Handle, false);
                    if (((l.Fighting || l.WasFighting) && i < 2) || (i < l.Aimed.Length && l.Aimed[i]))
                        TASK.CLEAR_PED_TASKS(p.Handle, true, false);   // the two who fought (never the barkeep or a bystander)
                    if (i < l.Aimed.Length && l.Aimed[i])
                        WEAPON.SET_CURRENT_PED_WEAPON(p.Handle, Natives.Hash("WEAPON_UNARMED"), true, 0, false, false);
                }
            }
            LastSummary = l.Scene.Summary;
            try
            {
                l.Done?.Invoke();
            }
            catch
            {
            }
        }

        private string Label(Live l, int who)
        {
            if (l.Introduced[who])
                return l.Names[who];
            l.Introduced[who] = true;
            return l.Names[who] + ", " + l.Kinds[who];
        }

        /// The player greeted (friendly) or antagonized one of the cast. Rewrites the rest of the scene around
        /// it. Returns the outcome ("peace", "egged", "sided") or null when it didn't apply.
        public string Intrude(int handle, bool friendly, out string kind)
        {
            kind = null;
            Live l = _live;
            if (l == null)
                return null;
            int who = Array.FindIndex(l.Cast, p => p != null && p.Handle == handle);
            if (who < 0 || l.Scene.Kind == "pickpocket" || l.Intruded)
                return null;
            kind = l.Scene.Kind;
            string outcome;
            Scene add = Scenes.Intrude(l.Scene.Kind, who, friendly, l.Info, l.Fighting, l.Seed + "|in", out outcome);
            if (add == null || add.Beats.Count == 0)
                return null;
            Scene s = new Scene { Kind = l.Scene.Kind, Cast = l.Scene.Cast, Fight = l.Scene.Fight, Summary = l.Scene.Summary };
            s.Beats.AddRange(add.Beats.Where(b => b.Who < l.Cast.Length));
            l.Scene = s;
            l.Index = 0;
            l.Intruded = true;
            l.NextAt = Game.GameTime + 300;
            if (l.Fighting && outcome != "egged")
            {
                l.Fighting = false;   // the first beat ends the fight
                l.FightUntil = 0;
                if (!s.Beats.Any(b => b.Act == "endfight"))
                {
                    // a scene the stranger's word has no fight-ending beat for (the trouble scenes): stop it here
                    for (int i = 0; i < Math.Min(2, l.Cast.Length); i++)
                        if (l.Cast[i] != null && l.Cast[i].Exists())
                        {
                            TASK.CLEAR_PED_TASKS(l.Cast[i].Handle, true, false);
                            PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(l.Cast[i].Handle, true);
                        }
                }
            }
            else if (l.Fighting && _say != null && s.Beats.Count > 0 && s.Beats[0].Line != null)
            {
                // egged on mid-fight: the line can't wait for the fight to finish
                Beat b0 = s.Beats[0];
                _say(Label(l, b0.Who), b0.Line, false, l.Women[b0.Who], l.Cast[Math.Min(b0.Who, l.Cast.Length - 1)].Handle);
                Speech.SayTag(l.Cast[b0.Who].Handle, b0.Voice, _player);
                s.Beats.RemoveAt(0);
                l.FightUntil = Math.Max(l.FightUntil, Game.GameTime + 4000);
            }
            Log.Write("SCENE " + l.Scene.Kind + ": you " + (friendly ? "stepped in" : "stirred it up") + (outcome != null ? " (" + outcome + ")" : ""));
            return outcome;
        }

        public void Tick(Ped player, Action<string, string, bool, bool, int> say)
        {
            Live l = _live;
            if (l == null)
                return;
            _player = player.Handle;
            _say = say;
            int now = Game.GameTime;
            foreach (Ped p in l.Cast)
            {
                if (p == null || !p.Exists() || p.IsDead || (PED.IS_PED_IN_COMBAT(p.Handle, player.Handle)) ||
                    PLAYER.IS_PLAYER_FREE_AIMING_AT_ENTITY(Game.Player.Handle, p.Handle))
                {
                    Abort("someone in the scene is gone or threatened");
                    return;
                }
            }
            if (l.Cast.Min(p => p.Position.DistanceTo(player.Position)) > 50f || now - l.StartedAt > (l.Scene.Ladder != null ? 150000 : 90000))   // a ladder quarrel that climbs the whole way runs past 90 s (the review)
            {
                Abort("the player left");
                return;
            }
            if (l.Fighting)
            {
                for (int i = 0; i < 2; i++)
                    KeepFists(l.Cast[i]);
                if (!l.FightOn)
                {
                    // at it: in melee, or a blow landed (the combat flag alone came the moment they were told, with no blows)
                    if (PED.IS_PED_IN_MELEE_COMBAT(l.FightA) || PED.IS_PED_IN_MELEE_COMBAT(l.FightB) || Health(l, 0) < 0.97f || Health(l, 1) < 0.97f)
                    {
                        l.FightOn = true;
                        l.FightUntil = now + l.FightMs;
                        Log.Write("SCENE fight: at it " + ((now - l.FightAskedAt) / 1000.0).ToString("0.0") + " s after the word");
                    }
                    else if (now >= l.FightUntil)
                    {
                        Abort("they never came to blows in " + FightStartMs / 1000 + " s (" +
                              (PED.IS_PED_USING_ANY_SCENARIO(l.FightA) || PED.IS_PED_USING_ANY_SCENARIO(l.FightB) ? "still sat or leaning"
                               : PED.IS_PED_IN_COMBAT(l.FightA, l.FightB) || PED.IS_PED_IN_COMBAT(l.FightB, l.FightA) ? "in combat, never in melee" : "ordered, never in combat") + ")");
                        return;
                    }
                    else
                    {
                        if (now - l.FightOrderedAt > 2000)
                            FightOrder(l);
                        return;
                    }
                }
                bool hurt = Health(l, 0) < 0.6f || Health(l, 1) < 0.6f;
                if (now < l.FightUntil && !hurt)
                    return;
                l.Fighting = false;
            }
            if (l.Slamming)
            {
                if (SlamGoing != null && SlamGoing())
                    return;
                l.Slamming = false;
                string result = SlamResult != null ? SlamResult() : null;
                if (result != null)
                    Report(l, result);   // only now is it known whether the face went into the bar
            }
            if (l.ReactAt > 0 && now >= l.ReactAt)
            {
                // the one spoken to reacts, unless they're about to speak or act themselves (theirs would cut it short)
                l.ReactAt = 0;
                int by = l.ReactBy;
                Beat next = l.Index < l.Scene.Beats.Count ? l.Scene.Beats[l.Index] : null;
                bool theirTurn = next != null && next.Who == by && (next.Line != null || next.Act != null) && l.NextAt - now < 1000;
                if (React != null && !theirTurn && !l.Fighting && l.MeetUntil == 0 && !(l.Downed && by == 0))
                {
                    bool tied = false;
                    int warm = l.Info != null ? l.Info.WarmthBetween(by, l.ReactTo, out tied) : 0;
                    if (l.Info == null)
                        tied = false;
                    React(new Reaction { By = l.Cast[by], To = l.Cast[l.ReactTo], Tone = l.ReactTone, Temper = Temper(l, by), Woman = l.Women[by],
                                         ByName = l.Names[by], ToName = l.Names[l.ReactTo], Seed = l.Seed + l.Index, Known = tied, Warm = warm });
                }
            }
            if (now < l.NextAt)
                return;
            if (l.MeetUntil > 0)
            {
                // the walker was sitting or leaning: he's up now, and sets off
                if (l.MeetGoAt > 0 && now >= l.MeetGoAt)
                {
                    l.MeetGoAt = 0;
                    Ped w = l.Cast[l.MeetWalker], o = l.Cast[1 - l.MeetWalker];
                    TASK.TASK_GO_TO_ENTITY(w.Handle, o.Handle, 20000, Natives.Brisk, 1.3f, 0f, 0);
                }
                // B is still walking over: the first word waits until they're face to face, or near enough across a bar or a
                // table when the one waiting sits or leans there (3.6 m: they often stopped short at the counter)
                Ped waiting = l.Cast[1 - l.MeetWalker];
                if (l.Cast[0].Position.DistanceTo(l.Cast[1].Position) <= (PED.IS_PED_USING_ANY_SCENARIO(waiting.Handle) && l.Sat < 0 ? 3.6f : 2.4f))   // someone down on the ground is helped from close by
                {
                    l.MeetUntil = 0;
                    Act(l, "face", 0, 1, 0);
                }
                else if (now > l.MeetUntil)
                {
                    Ped walker = l.Cast[l.MeetWalker], waiter = l.Cast[1 - l.MeetWalker];   // why, for the log: how far off, and whether a scenario held them
                    if (NeverMet != null)
                        NeverMet(l.Cast[0], l.Cast[1]);
                    Abort("they never got close enough to talk (still " + waiter.Position.DistanceTo(walker.Position).ToString("0.0") + " m apart" +
                          (PED.IS_PED_USING_ANY_SCENARIO(walker.Handle) ? ", a scenario held the walker" : "") +
                          (PED.IS_PED_USING_ANY_SCENARIO(waiter.Handle) ? ", a scenario held the other" : "") + ")");
                    return;
                }
                else
                    return;
            }
            if (l.ApproachUntil > 0 && l.Cast.Length > 2)
            {
                // the third is still walking over: the next beat waits until they're there, or the time is up
                Ped w = l.Cast[2], o = l.Cast[l.ApproachTo];
                if (l.ApproachGoAt > 0)
                {
                    if (now < l.ApproachGoAt)
                        return;
                    l.ApproachGoAt = 0;
                    TASK.TASK_GO_TO_ENTITY(w.Handle, o.Handle, 20000, Natives.Brisk, 1.3f, 0f, 0);
                    l.ApproachCheckAt = now;
                    return;
                }
                float d = w.Position.DistanceTo(o.Position);
                if (d <= ApproachNear)
                    l.ApproachUntil = 0;
                else if (now > l.ApproachUntil)
                {
                    l.ApproachUntil = 0;
                    Log.Write("SCENE   " + l.Names[2] + " never got there (still " + d.ToString("0.0") + " m off" +
                              (PED.IS_PED_USING_ANY_SCENARIO(w.Handle) ? ", a scenario held them" : "") + "): goes on from there");
                }
                else
                {
                    if (d < l.ApproachBest - 0.3f)
                    {
                        l.ApproachBest = d;
                        l.ApproachCheckAt = now;
                    }
                    else if (now - l.ApproachCheckAt > 3000)
                    {
                        // no nearer in 3 s (knocked off the walk, or it never took): the order again, as the kin walk-up does
                        TASK.TASK_GO_TO_ENTITY(w.Handle, o.Handle, 20000, Natives.Brisk, 1.3f, 0f, 0);
                        l.ApproachCheckAt = now;
                    }
                    return;
                }
            }
            if (l.Index >= l.Scene.Beats.Count)
            {
                Log.Write("SCENE over: " + l.Scene.Kind + " (" + string.Join(" & ", l.Names) + "), played to the end");
                End();
                return;
            }
            Beat b = l.Scene.Beats[l.Index++];
            int who = Math.Min(b.Who, l.Cast.Length - 1);
            if (b.Act == "approach" && who == 2 && l.Cast.Length > 2 && ThirdFree != null)
            {
                string taken = null;
                try
                {
                    taken = ThirdFree(l.Cast[2]);
                }
                catch (Exception ex)
                {
                    taken = "the check failed: " + ex.Message;
                }
                if (taken != null)
                {
                    Abort(l.Names[2] + " can't come over now (" + taken + ")");
                    return;
                }
            }
            int other = b.To >= 0 && b.To < l.Cast.Length && b.To != who ? b.To : who == 0 ? 1 : 0;   // said to the other, or to whom the beat names
            Ped me = l.Cast[who], them = l.Cast[other];
            int wait = 900;
            if (b.Act != null)
                wait = Math.Max(wait, Act(l, b.Act, who, other, b.Ms));
            if (b.Voice != null)
                Speech.SayTag(me.Handle, b.Voice, them.Handle);
            if (b.Line != null)
            {
                Log.Write("SCENE   " + l.Names[who] + " (" + (int)me.Position.DistanceTo(new Ped(_player).Position) + " m from you, " +
                          (ENTITY.IS_ENTITY_ON_SCREEN(me.Handle) ? "on screen" : "off screen") + "): " + b.Line);
                if ((b.Act == null || b.Act == "face") && !l.Fighting)
                    Gesture(me.Handle, b.Voice, l.Women[who], l.Seed + l.Index);
                say(Label(l, who), b.Line, l.Scene.Kind == "hosts", l.Women[who], me.Handle);
                wait = Math.Max(wait, 1600 + b.Line.Length * 55);
                if (b.Act != "hurt" && b.Voice != null)
                {
                    l.ReactAt = now + 700;   // the one it's said to takes it, a moment in
                    l.ReactBy = other;
                    l.ReactTo = who;
                    l.ReactTone = b.Voice;
                }
            }
            l.NextAt = now + wait + (b.Act == "fight" ? 0 : b.Ms);
        }

        private int Act(Live l, string act, int who, int other, int ms)
        {
            Ped me = l.Cast[who], them = l.Cast[other];
            int h = me.Handle, t = them.Handle;
            switch (act)
            {
                case "face":
                    foreach (Ped p in l.Cast)
                    {
                        Ped target = p == me ? them : me;
                        // someone sitting at a table or leaning on the bar just looks over
                        if (!PED.IS_PED_USING_ANY_SCENARIO(p.Handle))
                            TASK.TASK_TURN_PED_TO_FACE_ENTITY(p.Handle, target.Handle, 4000, 0f, 0f, 0f);
                        TASK.TASK_LOOK_AT_ENTITY(p.Handle, target.Handle, 20000, 0, 51, 0);
                    }
                    return 1400;
                case "approach":
                    if (who == 2 && l.Cast.Length > 2)
                    {
                        // the third walks over to the pair (the one who steps in, a friend, a spouse) to the one the beat names
                        // (else A), and the next beat waits until they're there (Tick), as "meet" does. Before 2026-10-29 the
                        // walk had 2.6 s and the next beat's face turned them where they stood: the one who stepped in said it
                        // from 9 m off (Lex)
                        l.ApproachTo = other;
                        l.ApproachBest = me.Position.DistanceTo(them.Position);
                        l.ApproachCheckAt = Game.GameTime;
                        l.ApproachUntil = Game.GameTime + Math.Min(ApproachMaxMs, 4000 + (int)(l.ApproachBest * 800f));
                        if (PED.IS_PED_USING_ANY_SCENARIO(h))
                        {
                            // a walk order alone never moves someone sitting or leaning (seen 2026-10-25): let go of it first
                            TASK.CLEAR_PED_TASKS(h, true, false);
                            l.ApproachGoAt = Game.GameTime + 1800;
                            l.ApproachUntil += 1800;
                        }
                        else
                            TASK.TASK_GO_TO_ENTITY(h, t, 20000, Natives.Brisk, 1.3f, 0f, 0);
                        return 300;
                    }
                    TASK.TASK_GO_TO_ENTITY(h, t, -1, Natives.Brisk, 1.3f, 0f, 0);
                    return 2600;
                case "meet":
                    // walks over to the other, who waits; the next line waits until they're close (Tick). Someone sitting or
                    // leaning (a scenario) is let go of it first and sets off once he's up: a walk order alone never
                    // moves him (seen 2026-10-25)
                    l.MeetWalker = who == 0 ? 0 : 1;
                    TASK.TASK_LOOK_AT_ENTITY(t, h, 20000, 0, 51, 0);
                    if (PED.IS_PED_USING_ANY_SCENARIO(h))
                    {
                        TASK.CLEAR_PED_TASKS(h, true, false);
                        l.MeetGoAt = Game.GameTime + 1800;
                        l.MeetUntil = Game.GameTime + 16800;
                    }
                    else
                    {
                        TASK.TASK_GO_TO_ENTITY(h, t, 20000, Natives.Brisk, 1.3f, 0f, 0);
                        l.MeetUntil = Game.GameTime + 15000;
                    }
                    return 300;
                case "shove":
                    Ped victim = who == 2 ? l.Cast[0] : them;
                    PED.SET_PED_TO_RAGDOLL(victim.Handle, 900, 1600, 0, false, false, "");
                    Speech.SayAny(victim.Handle, Speech.Scared, h);
                    return 1800;
                case "hurt":
                    // his face has just gone into the bar: he reels as he cries out (the game layer picks the clip)
                    if (React != null)
                        React(new Reaction { By = me, To = them, Tone = Reactions.HurtTone, Temper = Temper(l, who), Woman = l.Women[who], ByName = l.Names[who],
                                             ToName = l.Names[other], Seed = l.Seed + l.Index });
                    return 0;
                case "slam":
                    // the rough move into a table, a bar or a wall; with nothing it can be played on, a hard shove
                    if (Slam != null && Slam(me, them))
                    {
                        l.Slamming = true;   // reported once it's over (SlamResult)
                        return 300;
                    }
                    PED.SET_PED_TO_RAGDOLL(t, 1500, 2500, 0, false, false, "");
                    Speech.SayAny(h, Speech.Angry, t);
                    Report(l, "shove");
                    return 2200;
                case "challenge":
                    // the drunk lurches up off the bar if he was slumped on it, turns on the other and squares up,
                    // swaying (or, without the move, just looks him up and down)
                    if (PED.IS_PED_USING_ANY_SCENARIO(h))
                        TASK.CLEAR_PED_TASKS_IMMEDIATELY(h, false, true);
                    Vector3 from = me.Position, to = them.Position;
                    ENTITY.SET_ENTITY_HEADING(h, (float)(Math.Atan2(-(to.X - from.X), to.Y - from.Y) * 180.0 / Math.PI));
                    TASK.TASK_LOOK_AT_ENTITY(h, t, 20000, 0, 51, 0);
                    if (STREAMING.HAS_ANIM_DICT_LOADED(DrunkDict))
                    {
                        TASK.TASK_PLAY_ANIM(h, DrunkDict, "main_challenge_drunk", 4f, -4f, -1, 0, 0f, false, 0, false, "", false);
                        return Math.Max(1500, (int)(ENTITY.GET_ANIM_DURATION(DrunkDict, "main_challenge_drunk") * 1000) - 500);
                    }
                    return 1200;
                case "passout":
                    // down he goes, and stays down (the last frame held) until he comes to
                    l.Downed = true;
                    Report(l, "drunk");
                    if (STREAMING.HAS_ANIM_DICT_LOADED(DrunkDict))
                    {
                        TASK.TASK_PLAY_ANIM(h, DrunkDict, "pass_out_fwd_v1_drunk", 4f, -4f, -1, 2, 0f, false, 0, false, "", false);
                        return Math.Max(1500, (int)(ENTITY.GET_ANIM_DURATION(DrunkDict, "pass_out_fwd_v1_drunk") * 1000));
                    }
                    PED.SET_PED_TO_RAGDOLL(h, 6000, 9000, 0, false, false, "");
                    return 2500;
                case "cometo":
                    if (STREAMING.HAS_ANIM_DICT_LOADED(DrunkDict) && ENTITY.IS_ENTITY_PLAYING_ANIM(h, DrunkDict, "pass_out_fwd_v1_drunk", 3))
                    {
                        TASK.TASK_PLAY_ANIM(h, DrunkDict, "pass_out_fwd_v1_exit_drunk", 4f, -4f, -1, 0, 0f, false, 0, false, "", false);
                        return Math.Max(1500, (int)(ENTITY.GET_ANIM_DURATION(DrunkDict, "pass_out_fwd_v1_exit_drunk") * 1000));
                    }
                    return 600;
                case "fight":
                    l.WasFighting = true;
                    TASK.CLEAR_PED_TASKS(h, true, false);
                    TASK.CLEAR_PED_TASKS(t, true, false);
                    uint fists = Natives.Hash("WEAPON_UNARMED");
                    foreach (Ped p in l.Cast.Take(2))
                        WEAPON.SET_CURRENT_PED_WEAPON(p.Handle, fists, true, 0, false, false);
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, false);
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(t, false);
                    l.StartHealth = new[] { Math.Max(1, ENTITY.GET_ENTITY_HEALTH(l.Cast[0].Handle)), Math.Max(1, ENTITY.GET_ENTITY_HEALTH(l.Cast[1].Handle)) };
                    l.Fighting = true;
                    // the fight's own seconds start when they're at it (Tick): two men leaning or sat, ordered to fight
                    // the moment they were told to stop, got up and never swung, and the beat's 9 s ran out on the
                    // words alone (Lex, 2026-10-28)
                    l.FightOn = false;
                    l.FightA = h;
                    l.FightB = t;
                    l.FightAskedAt = Game.GameTime;
                    l.FightOrderedAt = 0;
                    l.FightMs = Math.Max(5000, ms);
                    l.FightUntil = Game.GameTime + FightStartMs;
                    // made to fight with their fists, as enRAGE makes its people (Lex, 2026-10-28: ordered to fight, two
                    // townsmen stood off in "combat" and never swung): fight unarmed (5), always fight (46), never flee
                    // the fight (58); their own put back when the scene ends
                    l.FightAttrs = new Dictionary<int, bool[]>();
                    foreach (int p in new[] { h, t })
                    {
                        l.FightAttrs[p] = FightAttrIds.Select(id => PED._GET_PED_COMBAT_ATTRIBUTE(p, id)).ToArray();
                        foreach (int id in FightAttrIds)
                            PED.SET_PED_COMBAT_ATTRIBUTES(p, id, true);
                    }
                    FightOrder(l);
                    return 1000;
                case "endfight":
                    Ped a = l.Cast[0], b = l.Cast[1];
                    TASK.CLEAR_PED_TASKS(a.Handle, true, false);
                    TASK.CLEAR_PED_TASKS(b.Handle, true, false);
                    float ha = Health(l, 0), hb = Health(l, 1);
                    if (hb > ha)
                    {
                        // the script says A wins; the fists said otherwise
                        l.Cast[0] = b;
                        l.Cast[1] = a;
                        if (l.StartHealth != null)
                        {
                            int sh = l.StartHealth[0]; l.StartHealth[0] = l.StartHealth[1]; l.StartHealth[1] = sh;
                        }
                        string n = l.Names[0]; l.Names[0] = l.Names[1]; l.Names[1] = n;
                        SwapNamesInLines(l, l.Names[0], l.Names[1]);
                        string k = l.Kinds[0]; l.Kinds[0] = l.Kinds[1]; l.Kinds[1] = k;
                        bool i = l.Introduced[0]; l.Introduced[0] = l.Introduced[1]; l.Introduced[1] = i;
                    }
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(l.Cast[0].Handle, true);
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(l.Cast[1].Handle, true);
                    return 1500;
                case "walkaway":
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, false);
                    TASK.TASK_WANDER_STANDARD(h, 10f, 10);
                    if (l.Scene.Kind == "lift" && who == 0)
                        Report(l, "lift");   // the thief walks off with it
                    return 600;
                case "leavewith":
                    {
                        // two leave together (the stand-up's take-away, night 2026-10-29): both walk to one spot 25 m past the third,
                        // away from the one the beat names, the other a metre to the side; the walk order is the one the encounters'
                        // and the campfire's walkers use (seen working), so they go side by side instead of wandering apart
                        Ped lead = l.Cast.Length > 2 && l.Cast[2] != null && l.Cast[2].Exists() ? l.Cast[2] : me;
                        Vector3 dir = lead.Position.DistanceTo2D(them.Position) >= 0.3f ? lead.Position - them.Position : me.Position - them.Position;
                        dir.Z = 0f;
                        dir = Math.Abs(dir.X) + Math.Abs(dir.Y) < 0.1f ? new Vector3(1f, 0f, 0f) : dir.Normalized;
                        Vector3 spot = lead.Position + dir * 25f;
                        if (me != lead)
                            spot += new Vector3(-dir.Y, dir.X, 0f) * 1.2f;
                        PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, false);
                        TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(h, spot.X, spot.Y, spot.Z, 1f, -1, 1f, 0, 0f);
                        if (_leaving.Count > 50)
                            _leaving.Clear();   // only ever two at a time; a guard against a session that never checks
                        _leaving[h] = Game.GameTime + LeaveHoldMs;
                        return 600;
                    }
                case "flee":
                    TASK.TASK_SMART_FLEE_PED(h, t, 40f, -1, 0, 2f, 0);
                    return 600;
                case "kneel":
                    TASK.TASK_START_SCENARIO_IN_PLACE_HASH(h, Crouch, 9000, true, 0u, -1f, false);
                    return 1800;
                case "fall":
                    // a trip: down on the ground for a moment (the old sit down hard instead: their scene leaves this out);
                    // short, so they're done tumbling when they sit up
                    PED.SET_PED_TO_RAGDOLL(h, 900, 1300, 0, false, false, "");
                    return 2300;
                case "faceone":
                    // only this one turns to the one the beat names (the rest carry on: one walking off stays walking)
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, t, 3000, 0f, 0f, 0f);
                    TASK.TASK_LOOK_AT_ENTITY(h, t, 6000, 0, 51, 0);
                    return 900;
                case "sitdown":
                    // stays down, sitting where they fell, until they get up or someone helps them
                    if (!PED.IS_PED_RAGDOLL(h))
                        TASK.TASK_START_SCENARIO_IN_PLACE_HASH(h, Natives.Hash("WORLD_HUMAN_SIT_GROUND"), 30000, true, 0u, -1f, false);
                    l.Sat = who;
                    return 1200;
                case "getup":
                    if (l.Sat == who)
                        l.Sat = -1;
                    TASK.CLEAR_PED_TASKS(h, true, false);
                    if (l.Scene.Kind == "help" || l.Scene.Kind == "passby")
                        Report(l, l.Scene.Kind);   // helped up, or left to get up alone: it happened
                    return 2200;
                case "stand":
                    TASK.CLEAR_PED_TASKS(h, true, false);
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, t, 3000, 0f, 0f, 0f);
                    return 1200;
                default:
                    if (act.StartsWith("cb:"))
                    {
                        try
                        {
                            Custom?.Invoke(act.Substring(3));
                        }
                        catch (Exception ex)
                        {
                            Log.Write("SCENE action " + act + " failed: " + ex.Message);
                        }
                    }
                    return 0;
                case "mourn":
                    TASK.TASK_START_SCENARIO_IN_PLACE_HASH(h, Natives.Hash("WORLD_HUMAN_GRAVE_MOURNING"), 20000, true, 0u, -1f, false);
                    return 1200;
                case "aim":
                    // a standoff: guns come out only when the cast is armed and no lawman is close (the game layer checks)
                    if ((who == 0 && l.Info.Armed0) || (who == 1 && l.Info.Armed1))
                    {
                        // after a fight the fists are still the weapon in hand (the fight act set them, KeepFists held
                        // them): the sidearm back in the hand first, else the aim would point nothing (the ladder's
                        // review; the fixed standoff never aims after a fight)
                        uint best = WEAPON.GET_BEST_PED_WEAPON(h, false, false);
                        if (best != 0 && best != Natives.Hash("WEAPON_UNARMED"))
                            WEAPON.SET_CURRENT_PED_WEAPON(h, best, true, 0, false, false);
                        TASK.TASK_AIM_GUN_AT_ENTITY(h, t, 25000, false, 0);
                        l.Aimed[who] = true;
                        return 900;
                    }
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, t, 3000, 0f, 0f, 0f);
                    TASK.TASK_LOOK_AT_ENTITY(h, t, 20000, 0, 51, 0);
                    return 700;
                case "holster":
                    if (who < l.Aimed.Length)
                        l.Aimed[who] = false;
                    TASK.CLEAR_PED_TASKS(h, true, false);
                    WEAPON.SET_CURRENT_PED_WEAPON(h, Natives.Hash("WEAPON_UNARMED"), true, 0, false, false);
                    TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, t, 2000, 0f, 0f, 0f);
                    return 700;
                case "drunk":
                    TASK.TASK_START_SCENARIO_IN_PLACE_HASH(h, Natives.Hash("WORLD_HUMAN_DRINKING_DRUNK"), 12000, true, 0u, -1f, false);
                    return 900;
                case "flask":
                    TASK.TASK_START_SCENARIO_IN_PLACE_HASH(h, Natives.Hash("WORLD_HUMAN_DRINK_FLASK"), 8000, true, 0u, -1f, false);
                    return 900;
                case "smoke":
                    TASK.TASK_START_SCENARIO_IN_PLACE_HASH(h, Natives.Hash("WORLD_HUMAN_SMOKE"), 15000, true, 0u, -1f, false);
                    return 900;
                case "whittle":
                    TASK.TASK_START_SCENARIO_IN_PLACE_HASH(h, Natives.Hash("WORLD_HUMAN_SIT_GROUND_WHITTLE"), 20000, true, 0u, -1f, false);
                    return 1600;
                case "stoic":
                    TASK.TASK_START_SCENARIO_IN_PLACE_HASH(h, Natives.Hash("WORLD_HUMAN_STARE_STOIC"), 10000, true, 0u, -1f, false);
                    return 900;
                case "run":
                    TASK.CLEAR_PED_TASKS(h, true, false);
                    TASK.TASK_SMART_FLEE_PED(h, _player != 0 ? _player : t, 180f, -1, 0, 3f, 0);
                    return 300;
                case "faceplayer":
                    if (_player != 0 && !(l.Downed && who == 0))   // a drunk lying where he fell only looks
                    {
                        if (!PED.IS_PED_USING_ANY_SCENARIO(h))
                            TASK.TASK_TURN_PED_TO_FACE_ENTITY(h, _player, 3000, 0f, 0f, 0f);
                        TASK.TASK_LOOK_AT_ENTITY(h, _player, 6000, 0, 51, 0);
                    }
                    return 600;
                case "lookplayer":
                    if (_player != 0)
                        TASK.TASK_LOOK_AT_ENTITY(h, _player, 6000, 0, 51, 0);
                    return 500;
            }
        }

        /// The scene's defining moment, once (Happened).
        private void Report(Live l, string what)
        {
            if (l.HappenedSent)
                return;
            l.HappenedSent = true;
            try
            {
                Happened?.Invoke(what);
            }
            catch (Exception ex)
            {
                Log.Write("SCENE report " + what + " failed: " + ex.Message);
            }
        }

        /// Health now, as a share of what it was when the fight started.
        private static float Health(Live l, int i)
        {
            int start = l.StartHealth != null ? l.StartHealth[i] : Math.Max(1, ENTITY.GET_ENTITY_MAX_HEALTH(l.Cast[i].Handle, false));
            return ENTITY.GET_ENTITY_HEALTH(l.Cast[i].Handle) / (float)start;
        }

        /// The third's walk over: there within 2.4 m (as "meet"), and given at most 12 s (4 s and 0.8 s a metre).
        private const float ApproachNear = 2.4f;
        private const int ApproachMaxMs = 12000;

        /// A brawl stays a brawl: if a fighter reaches for a gun or a knife, it goes back in the holster.
        private const int FightStartMs = 12000;
        private static readonly int[] FightAttrIds = { 5, 46, 58 };   // fight unarmed, always fight, never flee the fight

        /// The order to fight, once both are on their feet: a man sat or leaning is told to stop (the clear lets him get
        /// up) and, still in it after 3 s, taken out of it at once. Given again every 2 s until they're at it.
        private static void FightOrder(Live l)
        {
            int now = Game.GameTime;
            bool up = true;
            foreach (int p in new[] { l.FightA, l.FightB })
            {
                if (!PED.IS_PED_USING_ANY_SCENARIO(p))
                    continue;
                if (now - l.FightAskedAt > 3000)
                    TASK.CLEAR_PED_TASKS_IMMEDIATELY(p, false, true);
                else
                    up = false;
            }
            if (!up)
                return;
            TASK.TASK_COMBAT_PED(l.FightA, l.FightB, 0, 16);
            TASK.TASK_COMBAT_PED(l.FightB, l.FightA, 0, 16);
            l.FightOrderedAt = now;
        }

        private static unsafe void KeepFists(Ped p)
        {
            if (p == null || !p.Exists())
                return;
            uint fists = Natives.Hash("WEAPON_UNARMED");
            ulong current = 0;
            if (WEAPON.GET_CURRENT_PED_WEAPON(p.Handle, (uint*)&current, true, 0, false) && (uint)current != fists)
                WEAPON.SET_CURRENT_PED_WEAPON(p.Handle, fists, true, 0, false, false);
        }
    }
}
