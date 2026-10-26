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
            public bool[] Introduced;
            public bool[] Women;
            public string[] Kinds;
            public int Index;
            public int NextAt;
            public int StartedAt;
            public bool Fighting;
            public int FightUntil;
            public int[] StartHealth;
            public string Seed;
            public Action Done;
            public SceneCast Info;
            public bool Intruded;
            public bool[] Aimed = new bool[3];   // guns out in a standoff: put away if the scene is cut short
            public int MeetUntil;                // "meet": B is walking over; nothing is said until they're close, or give up then
        }

        private Live _live;
        private int _player;
        private Action<string, string, bool, bool, int> _say;

        /// Called for actions the director doesn't know ("cb:reveal"), with the part after "cb:".
        public Action<string> Custom;

        public bool Busy { get { return _live != null; } }
        public string LastSummary { get; private set; }
        public string CurrentKind { get { return _live != null ? _live.Scene.Kind : null; } }

        /// Is this ped one of the cast of the scene running now?
        public bool Involves(int handle)
        {
            Live l = _live;
            return l != null && handle != 0 && l.Cast.Any(p => p != null && p.Handle == handle);
        }

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
            Log.Write("SCENE " + s.Kind + ": " + string.Join(" & ", l.Names));
            return true;
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
        /// so nobody calls the winner by his own name.
        private static void SwapNamesInLines(Live l, string x, string y)
        {
            if (string.IsNullOrEmpty(x) || string.IsNullOrEmpty(y) || x == y)
                return;
            Regex both = new Regex(@"\b(" + Regex.Escape(x) + "|" + Regex.Escape(y) + @")\b");
            for (int j = l.Index; j < l.Scene.Beats.Count; j++)
            {
                Beat b = l.Scene.Beats[j];
                if (b.Line != null)
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
            for (int i = 0; i < l.Cast.Length; i++)
            {
                Ped p = l.Cast[i];
                if (p != null && p.Exists())
                {
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(p.Handle, false);
                    if (l.Fighting || (i < l.Aimed.Length && l.Aimed[i]))
                        TASK.CLEAR_PED_TASKS(p.Handle, true, false);
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
            if (l.Cast.Min(p => p.Position.DistanceTo(player.Position)) > 50f || now - l.StartedAt > 90000)
            {
                Abort("the player left");
                return;
            }
            if (l.Fighting)
            {
                for (int i = 0; i < 2; i++)
                    KeepFists(l.Cast[i]);
                bool hurt = Health(l, 0) < 0.6f || Health(l, 1) < 0.6f;
                if (now < l.FightUntil && !hurt)
                    return;
                l.Fighting = false;
            }
            if (now < l.NextAt)
                return;
            if (l.MeetUntil > 0)
            {
                // B is still walking over: the first word waits until they're face to face
                if (l.Cast[0].Position.DistanceTo(l.Cast[1].Position) <= 2.4f)
                {
                    l.MeetUntil = 0;
                    Act(l, "face", 0, 1, 0);
                }
                else if (now > l.MeetUntil)
                {
                    Ped walker = l.Cast[1];   // why, for the log: how far off, and whether a scenario (a bench, a rail) held them
                    Abort("they never got close enough to talk (still " + l.Cast[0].Position.DistanceTo(walker.Position).ToString("0.0") + " m apart" +
                          (PED.IS_PED_USING_ANY_SCENARIO(walker.Handle) ? ", a scenario held the walker" : "") +
                          (PED.IS_PED_USING_ANY_SCENARIO(l.Cast[0].Handle) ? ", a scenario held the other" : "") + ")");
                    return;
                }
                else
                    return;
            }
            if (l.Index >= l.Scene.Beats.Count)
            {
                Log.Write("SCENE over: " + l.Scene.Kind + " (" + string.Join(" & ", l.Names) + "), played to the end");
                End();
                return;
            }
            Beat b = l.Scene.Beats[l.Index++];
            int who = Math.Min(b.Who, l.Cast.Length - 1);
            int other = who == 0 ? 1 : 0;
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
                    TASK.TASK_GO_TO_ENTITY(h, l.Cast.Length > 2 && who == 2 ? l.Cast[0].Handle : t, -1, 1.4f, 1.3f, 0f, 0);
                    return 2600;
                case "meet":
                    // walks over to the other, who waits; the next line waits until they're close (Tick)
                    TASK.TASK_GO_TO_ENTITY(h, t, 20000, 1.4f, 1.3f, 0f, 0);
                    TASK.TASK_LOOK_AT_ENTITY(t, h, 20000, 0, 51, 0);
                    l.MeetUntil = Game.GameTime + 15000;
                    return 300;
                case "shove":
                    Ped victim = who == 2 ? l.Cast[0] : them;
                    PED.SET_PED_TO_RAGDOLL(victim.Handle, 900, 1600, 0, false, false, "");
                    Speech.SayAny(victim.Handle, Speech.Scared, h);
                    return 1800;
                case "fight":
                    TASK.CLEAR_PED_TASKS(h, true, false);
                    TASK.CLEAR_PED_TASKS(t, true, false);
                    uint fists = Natives.Hash("WEAPON_UNARMED");
                    foreach (Ped p in l.Cast.Take(2))
                        WEAPON.SET_CURRENT_PED_WEAPON(p.Handle, fists, true, 0, false, false);
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(h, false);
                    PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(t, false);
                    TASK.TASK_COMBAT_PED(h, t, 0, 16);
                    TASK.TASK_COMBAT_PED(t, h, 0, 16);
                    l.StartHealth = new[] { Math.Max(1, ENTITY.GET_ENTITY_HEALTH(l.Cast[0].Handle)), Math.Max(1, ENTITY.GET_ENTITY_HEALTH(l.Cast[1].Handle)) };
                    l.Fighting = true;
                    l.FightUntil = Game.GameTime + Math.Max(5000, ms);
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
                    return 600;
                case "flee":
                    TASK.TASK_SMART_FLEE_PED(h, t, 40f, -1, 0, 2f, 0);
                    return 600;
                case "kneel":
                    TASK.TASK_START_SCENARIO_IN_PLACE_HASH(h, Natives.Hash("WORLD_HUMAN_CROUCH_INSPECT"), 9000, true, 0u, -1f, false);
                    return 1800;
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
                    if (_player != 0)
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

        /// Health now, as a share of what it was when the fight started.
        private static float Health(Live l, int i)
        {
            int start = l.StartHealth != null ? l.StartHealth[i] : Math.Max(1, ENTITY.GET_ENTITY_MAX_HEALTH(l.Cast[i].Handle, false));
            return ENTITY.GET_ENTITY_HEALTH(l.Cast[i].Handle) / (float)start;
        }

        /// A brawl stays a brawl: if a fighter reaches for a gun or a knife, it goes back in the holster.
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
