// Rough handling (Lex, 2026-10-25, roadmap 7): one man grabs another and smashes his face on whatever is at hand, a bar,
// a table, a piano, or into a wall, with the game's own paired grapple moves (mech_grapple ... environment@table_smash
// and @wall_smash: an attacker half and a victim half, authored to play from one shared origin). Lex's call: anywhere
// the move can play, between two men with the temper and the grudge for it, now and then by chance. This is still the
// test: Shift+F9 stages it on two men near you who are free to be directed. Around the victim it looks for a surface
// (`RoughSurfaces`: a top at waist height, or a wall, within about a metre, by raycasts), and from the game's own data
// for each angle of the move it knows which way the victim is driven (`RoughMoves`), so it picks the angle that sends
// him into that surface. The origin is worked out (GET_ANIM_INITIAL_OFFSET_*) so the victim starts where he stands,
// facing the way he faces; only the attacker steps in. Nothing is hit: the move is played, no damage, so no crime and
// no law. Street scenes use it too (Scenes.Rough, from a reason in Reasons.cs, [Scenes] RoughHandling): the Director's
// "slam" beat calls `RoughForScene` and waits while it plays; with no surface by the victim the reason falls back to a
// fistfight or words before the scene starts, and if he has moved off it by the time, the slam is a hard shove.
using System;
using System.Collections.Generic;
using System.Linq;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Math;
using RDR2.Native;
using System.Windows.Forms;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private static readonly KeySpec RoughTestKey = KeySpec.Parse("Shift+F9", Keys.F9);   // not F6: that is the game's camera
        private const string SmashDict = "mech_grapple@unarmed@_male@_ambient@_healthy@front@environment@table_smash";
        private const string WallDict = "mech_grapple@unarmed@_male@_ambient@_healthy@front@environment@wall_smash";
        private const string DrunkDict = "script_re@drunk_dueler";
        // the angles of each move, as the game names them; each has _att and _vic
        private static readonly string[] SmashAngles = { "smash_near_front", "smash_near_left", "smash_near_right", "smash_near_backleft", "smash_near_backright", "smash_far_front", "smash_far_left", "smash_far_right" };
        private static readonly string[] WallAngles = { "attack_wall-slam_front_v01", "attack_wall-slam_left_v01", "attack_wall-slam_right_v01", "attack_wall-slam_far_front_v01", "attack_wall-slam_far_left_v01", "attack_wall-slam_far_right_v01" };
        // the game's scenarios for a customer at the counter (scenario type names from rdr3_discoveries'
        // scenario_types_with_conditional_anims.lua; seen in the Rhodes saloon: WORLD_HUMAN_BARCUSTOMER_WHISKEY)
        private static readonly string[] BarScenarios = { "WORLD_HUMAN_BARCUSTOMER", "WORLD_HUMAN_BARCUSTOMER_BEER", "WORLD_HUMAN_BARCUSTOMER_WHISKEY", "SC_WORLD_HUMAN_STAND_BAR",
                                                          "WORLD_HUMAN_LEAN_BAR_READ_NEWSPAPER", "WORLD_HUMAN_DRUNK_BAR_SLUMPED" };
        // a few others seen near the bar, so the log can name them
        private static readonly string[] OtherScenarios = { "WORLD_HUMAN_BARTENDER_CLEAN_GLASS", "WORLD_HUMAN_BARTENDER", "PROP_HUMAN_PIANO", "GENERIC_SEAT_CHAIR_TABLE_SCENARIO",
                                                            "PROP_HUMAN_SEAT_CHAIR_TABLE_FAN_WHORE", "PROP_CAMP_SEAT_CHAIR_TABLE_STEW", "PROP_HUMAN_SEAT_CHAIR_TABLE_EATING_KNIFE_FORK" };
        private const int RoughFlags = 0x80 | 0x100;   // OverridePhysics | ExtractInitialOffset: both play from the shared origin
        private const float RoughNear = 10f;           // the test picks men this close to you
        private const int RoughWalkMs = 8000;          // longest the attacker may take to step in
        private const float TopLow = 0.55f, TopHigh = 1.3f;   // a top (table, bar, piano) is this high above the floor
        private const float RoughFit = 40f;            // a move fits a surface whose direction is within this many degrees of its push

        /// A surface by the victim: a top at waist height or a wall, in a direction relative to the way he faces
        /// (degrees counter-clockwise: 0 ahead of him, 90 to his left, 180 behind, 270 to his right).
        private struct Surface
        {
            public bool Wall;
            public float Bearing, Dist, Height;
        }

        /// One angle of a move, and which way and how far it drives the victim, relative to the way he starts facing.
        private sealed class RoughMove
        {
            public string Dict, Clip;
            public bool Wall;
            public float Bearing, Dist, Length;
        }

        private sealed class Rough
        {
            public Ped Att, Vic;
            public string AttName, VicName, Dict, Angle, Why;
            public Vector3 Origin, Rot, AttStart, VicFrom;
            public int Stage;                  // 0 loading (and the attacker getting up), 1 stepping in, 2 playing, 3 after
            public int At, Until, Started;
            public float Length;               // the move's length, seconds
            public bool ForScene;              // played for a street scene: the Director holds the two and says the lines
        }

        private Rough _rough;
        private string _roughResult;   // how the last scene's move came out: "rough" (it played), "shove", or null (Director.SlamResult)
        private bool _roughTestRequested;
        private int _roughAngle;
        private List<RoughMove> _roughMoves;   // read once both moves have loaded

        private int _nextRoughAsk;

        private void RoughStep(int now)
        {
            if (_aRough == null)
                _aRough = () => RoughTick(_now);
            if (now >= _nextRoughAsk)
            {
                // kept loaded (asked again if the game let them go), so a scene can tell at once whether the move fits,
                // and the drunk's moves are there when he squares up
                _nextRoughAsk = now + 10000;
                ReactionSets();   // and the hurt reaction's set (WestworldScript.Reactions.cs)
                foreach (string d in _cfg.RoughHandling ? new[] { SmashDict, WallDict } : new string[0])
                    if (!STREAMING.HAS_ANIM_DICT_LOADED(d) && STREAMING.DOES_ANIM_DICT_EXIST(d))
                        STREAMING.REQUEST_ANIM_DICT(d);
                if (_cfg.DrunkChallenges && !STREAMING.HAS_ANIM_DICT_LOADED(DrunkDict) && STREAMING.DOES_ANIM_DICT_EXIST(DrunkDict))
                    STREAMING.REQUEST_ANIM_DICT(DrunkDict);
            }
            Safe("rough handling", _aRough);
        }

        /// Can the rough move be played on this man where he stands: both moves loaded, and a surface by him that one
        /// of its angles drives him into. For the reasons, before a "rough" scene is chosen.
        private bool RoughCanPlay(Ped vic)
        {
            if (_rough != null || !STREAMING.HAS_ANIM_DICT_LOADED(SmashDict) || !STREAMING.HAS_ANIM_DICT_LOADED(WallDict))
                return false;
            RoughMove m;
            Surface s;
            return RoughFitFor(vic, out m, out s);
        }

        /// The Director's "slam": the rough move between these two, for a scene. False if it can't start (then the
        /// Director shoves him instead).
        private bool RoughForScene(Ped att, Ped vic)
        {
            if (_rough != null || att == null || vic == null || !att.Exists() || !vic.Exists() || !STREAMING.HAS_ANIM_DICT_LOADED(SmashDict) || !STREAMING.HAS_ANIM_DICT_LOADED(WallDict))
                return false;
            int now = _now;
            _roughResult = null;
            bool getUp = PED.IS_PED_USING_ANY_SCENARIO(att.Handle);
            if (getUp)
                TASK.CLEAR_PED_TASKS(att.Handle, true, false);   // up off the bar first, as the test does
            _rough = new Rough { Att = att, Vic = vic, AttName = TalkName(att), VicName = TalkName(vic), VicFrom = vic.Position, Until = now + 5000, At = now, ForScene = true, Started = getUp ? now + 2000 : 0 };
            Log.Write("ROUGH scene: " + _rough.AttName + " grabs " + _rough.VicName + " (" + (AtBar(vic) ?? ScenarioName(vic)) + ")");
            return true;
        }

        private Action _aRough;

        private void RoughTick(int now)
        {
            if (_roughTestRequested)
            {
                _roughTestRequested = false;
                RoughTestStart(now);
            }
            Rough r = _rough;
            if (r == null || now < r.At)
                return;
            foreach (Ped p in new[] { r.Att, r.Vic })
                if (p == null || !p.Exists() || ENTITY.IS_ENTITY_DEAD(p.Handle))
                {
                    RoughEnd(r, "stopped: someone is gone", now);
                    return;
                }
            switch (r.Stage)
            {
                case 0:   // the moves load, and an attacker who was sitting or leaning gets up; then the angle is chosen
                    if (!STREAMING.HAS_ANIM_DICT_LOADED(SmashDict) || !STREAMING.HAS_ANIM_DICT_LOADED(WallDict) || now < r.Started)
                    {
                        if (now > r.Until)
                            RoughEnd(r, "stopped: the moves' animations didn't load", now);
                        else
                            r.At = now + 100;
                        return;
                    }
                    r.Started = 0;
                    if (!RoughChoose(r))
                    {
                        // a scene's victim has moved off the surface since the reason was found: a hard shove instead
                        PED.SET_PED_TO_RAGDOLL(r.Vic.Handle, 1500, 2500, 0, false, false, "");
                        RoughEnd(r, "stopped: no angle fits where he stands now, so a shove", now, true);
                        return;
                    }
                    RoughPlace(r);
                    // round the tables to the spot, facing the way the move starts
                    TASK.TASK_FOLLOW_NAV_MESH_TO_COORD(r.Att.Handle, r.AttStart.X, r.AttStart.Y, r.AttStart.Z, 1f, RoughWalkMs, 0.3f, 0, RoughHeading(r.AttStart, r.Vic.Position));
                    TASK.TASK_LOOK_AT_ENTITY(r.Vic.Handle, r.Att.Handle, RoughWalkMs, 0, 51, 0);
                    r.Stage = 1;
                    r.Until = now + RoughWalkMs;
                    r.At = now + 200;
                    return;
                case 1:   // the attacker steps in to where the move starts
                    float off = Flat(r.Att.Position, r.AttStart);
                    if (off > 0.6f && now < r.Until)
                    {
                        r.At = now + 100;
                        return;
                    }
                    RoughPlay(r, now, off);
                    return;
                case 2:   // the move plays; check once that both are in it, then wait for the end
                    if (r.Started == 0)
                    {
                        bool att = ENTITY.IS_ENTITY_PLAYING_ANIM(r.Att.Handle, r.Dict, r.Angle + "_att", 3), vic = ENTITY.IS_ENTITY_PLAYING_ANIM(r.Vic.Handle, r.Dict, r.Angle + "_vic", 3);
                        Log.Write("ROUGH " + r.AttName + " & " + r.VicName + ": playing " + (att ? "yes" : "NO") + " / " + (vic ? "yes" : "NO") +
                                  "; the victim is " + Flat(r.Vic.Position, r.VicFrom).ToString("0.0") + " m from where he stood");
                        r.Started = now;
                        if (att && r.ForScene)
                            _roughResult = "rough";   // the face really goes into the bar
                        if (!att)
                        {
                            // without the attacker's half the victim would smash his own face: stop it, and a shove instead
                            if (vic)
                                TASK.CLEAR_PED_TASKS(r.Vic.Handle, true, false);
                            PED.SET_PED_TO_RAGDOLL(r.Vic.Handle, 1500, 2500, 0, false, false, "");
                            RoughEnd(r, "stopped: the attacker's half didn't start" + (vic ? " (the victim's did)" : "") + ", so a shove", now, true);
                            return;
                        }
                    }
                    if (now - r.Started < (int)(r.Length * 1000) + 300)
                    {
                        r.At = now + 200;
                        return;
                    }
                    if (r.ForScene)
                    {
                        RoughEnd(r, "over", now);   // the scene says the rest
                        return;
                    }
                    // after: the victim cries out
                    Speech.SayAny(r.Vic.Handle, Speech.Scared, r.Att.Handle);
                    r.Stage = 3;
                    r.At = now + 2500;
                    return;
                default:
                    RoughEnd(r, "over", now);
                    return;
            }
        }

        /// The angle to play: the one whose push best matches a surface by the victim (a top first, else a wall); with
        /// no surface, or the moves' directions unreadable, the next table angle in turn, as the first tests did.
        private bool RoughChoose(Rough r)
        {
            List<RoughMove> moves = RoughMoves();
            RoughMove best;
            Surface at;
            List<Surface> found;
            RoughFitFor(r.Vic, out best, out at, out found);
            string surfaces = found.Count == 0 ? "no surface by him" : "by him: " + string.Join(", ", found.Select(Describe));
            if (best == null && r.ForScene)
            {
                Log.Write("ROUGH " + r.AttName + " & " + r.VicName + ": " + surfaces + "; no angle drives him into one");
                return false;
            }
            if (best != null)
            {
                r.Dict = best.Dict;
                r.Angle = best.Clip;
                r.Why = surfaces + "; " + best.Clip + " drives him " + best.Dist.ToString("0.0") + " m " + Side(best.Bearing) + ", into the " + Describe(at);
            }
            else
            {
                r.Dict = SmashDict;
                r.Angle = SmashAngles[_roughAngle++ % SmashAngles.Length];
                r.Why = surfaces + "; no angle drives him into one" + (moves.All(m => m.Dist <= 0.1f) ? " (the moves' directions couldn't be read)" : "") + ", so the next in turn: " + r.Angle;
            }
            Log.Write("ROUGH " + r.AttName + " & " + r.VicName + ": " + r.Why);
            return true;
        }

        private bool RoughFitFor(Ped vic, out RoughMove best, out Surface at)
        {
            List<Surface> found;
            return RoughFitFor(vic, out best, out at, out found);
        }

        /// The angle whose push best matches a surface by him (a top first, else a wall), or null.
        private bool RoughFitFor(Ped vic, out RoughMove best, out Surface at, out List<Surface> found)
        {
            List<RoughMove> moves = RoughMoves();
            found = RoughSurfaces(vic);
            best = null;
            at = default(Surface);
            float bestScore = float.MaxValue;
            foreach (Surface s in found)
                foreach (RoughMove m in moves.Where(x => x.Wall == s.Wall && x.Dist > 0.1f))
                {
                    float diff = AngleDiff(m.Bearing, s.Bearing);
                    if (diff > RoughFit)
                        continue;
                    float score = diff + 25f * Math.Abs(m.Dist - s.Dist) + (s.Wall ? 15f : 0f);
                    if (score < bestScore)
                    {
                        bestScore = score;
                        best = m;
                        at = s;
                    }
                }
            return best != null;
        }

        /// Each angle of both moves, and which way it drives the victim: his place at the start and partway through,
        /// as the game gives them (GET_ANIM_INITIAL_OFFSET_POSITION at a phase), turned to the way he starts facing.
        private List<RoughMove> RoughMoves()
        {
            if (_roughMoves != null)
                return _roughMoves;
            var moves = new List<RoughMove>();
            Vector3 zero = new Vector3(0f, 0f, 0f);
            foreach (bool wall in new[] { false, true })
                foreach (string clip in wall ? WallAngles : SmashAngles)
                {
                    string dict = wall ? WallDict : SmashDict, vic = clip + "_vic";
                    Vector3 start = PED.GET_ANIM_INITIAL_OFFSET_POSITION(dict, vic, zero, zero, 0f, 2);
                    float facing = PED.GET_ANIM_INITIAL_OFFSET_ROTATION(dict, vic, zero, zero, 0f, 2).Z;
                    // the furthest he is driven from his start (the face meets the surface partway, not at the end)
                    float dist = 0f, bearing = 0f;
                    foreach (float phase in new[] { 0.3f, 0.45f, 0.6f })
                    {
                        Vector3 then = PED.GET_ANIM_INITIAL_OFFSET_POSITION(dict, vic, zero, zero, phase, 2);
                        float d = Flat(then, start);
                        if (d > dist)
                        {
                            dist = d;
                            bearing = Norm(RoughHeading(start, then) - facing);
                        }
                    }
                    moves.Add(new RoughMove { Dict = dict, Clip = clip, Wall = wall, Bearing = bearing, Dist = dist, Length = ENTITY.GET_ANIM_DURATION(dict, vic) });
                }
            Log.Write("ROUGH moves: " + string.Join("; ", moves.Select(m => m.Clip + " drives him " + m.Dist.ToString("0.00") + " m " + Side(m.Bearing) + " (" + (int)m.Bearing + "), " + m.Length.ToString("0.0") + " s")));
            if (moves.All(m => m.Dist <= 0.1f))
                Log.Write("ROUGH: no angle's push could be read from the game (all about 0 m), so street scenes can't fit the move to a surface: they stay fistfights. The Shift+F9 test still takes the angles in turn.");
            return _roughMoves = moves;
        }

        /// Surfaces within about a metre of this man: a top at waist height (looked for straight down at three
        /// distances in eight directions) and walls (looked for level at chest height), relative to the way he faces.
        private static List<Surface> RoughSurfaces(Ped p)
        {
            var found = new List<Surface>();
            Vector3 at = p.Position, hit, normal;
            float floor = Natives.Probe(at, new Vector3(at.X, at.Y, at.Z - 2.5f), p.Handle, out hit, out normal) ? hit.Z : at.Z - 1f;
            float facing = ENTITY.GET_ENTITY_HEADING(p.Handle);
            for (int i = 0; i < 8; i++)
            {
                float bearing = i * 45f, h = (facing + bearing) * (float)Math.PI / 180f;
                float dx = -(float)Math.Sin(h), dy = (float)Math.Cos(h);
                foreach (float dist in new[] { 0.55f, 0.85f, 1.15f })
                {
                    float x = at.X + dx * dist, y = at.Y + dy * dist;
                    if (Natives.Probe(new Vector3(x, y, floor + 1.7f), new Vector3(x, y, floor + 0.3f), p.Handle, out hit, out normal) && normal.Z > 0.8f &&
                        hit.Z - floor >= TopLow && hit.Z - floor <= TopHigh)
                    {
                        found.Add(new Surface { Wall = false, Bearing = bearing, Dist = dist, Height = hit.Z - floor });
                        break;
                    }
                }
                Vector3 chest = new Vector3(at.X, at.Y, floor + 1.3f);
                if (Natives.Probe(chest, new Vector3(at.X + dx * 1.3f, at.Y + dy * 1.3f, floor + 1.3f), p.Handle, out hit, out normal) && Math.Abs(normal.Z) < 0.35f)
                    found.Add(new Surface { Wall = true, Bearing = bearing, Dist = Flat(hit, chest), Height = 1.3f });
            }
            return found;
        }

        private static string Describe(Surface s)
        {
            return (s.Wall ? "wall " : "top " + s.Height.ToString("0.00") + " m high, ") + s.Dist.ToString("0.0") + " m " + Side(s.Bearing);
        }

        private static string Side(float bearing)
        {
            string[] names = { "ahead of him", "ahead and to his left", "to his left", "behind and to his left", "behind him", "behind and to his right", "to his right", "ahead and to his right" };
            return names[(int)Math.Round(Norm(bearing) / 45f) % 8];
        }

        private static float Norm(float degrees)
        {
            degrees %= 360f;
            return degrees < 0f ? degrees + 360f : degrees;
        }

        private static float AngleDiff(float a, float b)
        {
            float d = Math.Abs(Norm(a) - Norm(b));
            return d > 180f ? 360f - d : d;
        }

        /// The shared origin: the victim starts where he stands and faces the way he faces; the attacker's start comes
        /// from it. The game gives each half's start for an origin, so the origin is turned and moved until the victim's
        /// start is his own spot.
        private void RoughPlace(Rough r)
        {
            string vic = r.Angle + "_vic", att = r.Angle + "_att";
            Vector3 at = r.Vic.Position;
            float heading = ENTITY.GET_ENTITY_HEADING(r.Vic.Handle);
            Vector3 rot = new Vector3(0f, 0f, heading);
            Vector3 vicRot = PED.GET_ANIM_INITIAL_OFFSET_ROTATION(r.Dict, vic, at, rot, 0f, 2);
            rot = new Vector3(0f, 0f, heading + (heading - vicRot.Z));
            Vector3 vicAt = PED.GET_ANIM_INITIAL_OFFSET_POSITION(r.Dict, vic, at, rot, 0f, 2);
            Vector3 origin = new Vector3(at.X - (vicAt.X - at.X), at.Y - (vicAt.Y - at.Y), at.Z - (vicAt.Z - at.Z));   // height too (it came out 1 m high)
            r.Origin = origin;
            r.Rot = rot;
            r.AttStart = PED.GET_ANIM_INITIAL_OFFSET_POSITION(r.Dict, att, origin, rot, 0f, 2);
            Vector3 check = PED.GET_ANIM_INITIAL_OFFSET_POSITION(r.Dict, vic, origin, rot, 0f, 2);
            Log.Write("ROUGH " + r.AttName + " & " + r.VicName + ": " + r.Angle + ", origin turned " + (heading - vicRot.Z).ToString("0") + " degrees; the victim starts " +
                      Flat(check, at).ToString("0.00") + " m from his spot (height " + (check.Z - at.Z).ToString("0.00") + "), the attacker " +
                      Flat(r.AttStart, r.Att.Position).ToString("0.0") + " m from his");
        }

        private void RoughPlay(Rough r, int now, float off)
        {
            // let the victim go of what he was doing first (a scenario holds him), then both halves from the one origin
            if (PED.IS_PED_USING_ANY_SCENARIO(r.Vic.Handle))
                TASK.CLEAR_PED_TASKS_IMMEDIATELY(r.Vic.Handle, false, true);
            r.Length = Math.Max(ENTITY.GET_ANIM_DURATION(r.Dict, r.Angle + "_att"), ENTITY.GET_ANIM_DURATION(r.Dict, r.Angle + "_vic"));
            TASK.TASK_PLAY_ANIM_ADVANCED(r.Att.Handle, r.Dict, r.Angle + "_att", r.Origin.X, r.Origin.Y, r.Origin.Z, r.Rot.X, r.Rot.Y, r.Rot.Z, 8f, -8f, -1, RoughFlags, 0f, 2, 0, 0);
            TASK.TASK_PLAY_ANIM_ADVANCED(r.Vic.Handle, r.Dict, r.Angle + "_vic", r.Origin.X, r.Origin.Y, r.Origin.Z, r.Rot.X, r.Rot.Y, r.Rot.Z, 8f, -8f, -1, RoughFlags, 0f, 2, 0, 0);
            Speech.SayAny(r.Att.Handle, Speech.Angry, r.Vic.Handle);
            // those watching gasp as he grabs him (two at most, the nearest)
            Ped me = Game.Player.Character;
            foreach (Ped w in World.GetAllPeds().Where(p => p != null && p.Exists() && p.Handle != r.Att.Handle && p.Handle != r.Vic.Handle && Natives.Bystander(p, me.Handle) &&
                                                            p.Position.DistanceTo(r.Vic.Position) < 10f && !Speech.Busy(p.Handle))
                                                 .OrderBy(p => p.Position.DistanceTo(r.Vic.Position)).Take(2))
                Speech.SayAny(w.Handle, Speech.Shame, r.Att.Handle);
            Log.Write("ROUGH " + r.AttName + " & " + r.VicName + ": the move begins (" + r.Length.ToString("0.0") + " s), the attacker " + off.ToString("0.0") + " m off his start" +
                      (now >= r.Until ? " (he didn't get there in time)" : ""));
            r.Stage = 2;
            r.At = now + 300;
        }

        private void RoughEnd(Rough r, string how, int now, bool shoved = false)
        {
            if (shoved && r.ForScene)
                _roughResult = "shove";
            foreach (Ped p in new[] { r.Att, r.Vic })
                if (p != null && p.Exists())
                {
                    if (!r.ForScene)
                        PED.SET_BLOCKING_OF_NON_TEMPORARY_EVENTS(p.Handle, false);   // a scene's Director lets them go at its end
                    // a victim just shoved instead is left to fall (clearing his tasks now could cancel the stumble)
                    if (!ENTITY.IS_ENTITY_DEAD(p.Handle) && !PED.IS_PED_RAGDOLL(p.Handle) && !(shoved && p == r.Vic))
                        TASK.CLEAR_PED_TASKS(p.Handle, true, false);
                }
            Log.Write("ROUGH " + r.AttName + " & " + r.VicName + " " + how);
            _rough = null;
        }

        private int _roughTestTurn;
        private static readonly string[] RoughTestAnswers = { "sharp", "cold", "kind" };

        /// Shift+F9: the whole rough scene (Scenes.Rough through the Director: words, the slam, his cry, and his answer,
        /// which turns sharp, cold, kind with each press), with no tie left between them. The victim is the man nearest
        /// you at the bar, else the nearest man on his feet, else the nearest; the attacker the nearest other man, one on
        /// his feet first. The angle is chosen by what's by the victim.
        private void RoughTestStart(int now)
        {
            if (_rough != null || _director.Busy)
            {
                Log.Write("ROUGH test: " + (_rough != null ? "the move is already playing." : "a street scene is going on; try again when it's over."));
                return;
            }
            Ped me = Game.Player.Character;
            if (!STREAMING.DOES_ANIM_DICT_EXIST(SmashDict) || !STREAMING.DOES_ANIM_DICT_EXIST(WallDict))
            {
                Log.Write("ROUGH test: this game is missing the moves' animation sets (" + SmashDict + ", " + WallDict + ").");
                return;
            }
            List<Ped> men = World.GetAllPeds().Where(p => p != null && p.Exists() && PED.IS_PED_MALE(p.Handle) && p.Position.DistanceTo(me.Position) < RoughNear &&
                                                          TalkLeftOut(p, me) == null && CanDirect(p) && IsGrownUp(p) && _bridge.RecordAt(p.Handle) == null)
                                              .OrderBy(p => p.Position.DistanceTo(me.Position)).ToList();
            Ped vic = men.FirstOrDefault(p => AtBar(p) != null) ?? men.FirstOrDefault(p => !PED.IS_PED_USING_ANY_SCENARIO(p.Handle)) ?? men.FirstOrDefault();
            // the attacker: someone on his feet first (a man at the piano or at his supper is a last resort)
            Ped att = vic == null ? null : men.Where(p => p.Handle != vic.Handle).OrderBy(p => (PED.IS_PED_USING_ANY_SCENARIO(p.Handle) ? 20f : 0f) + p.Position.DistanceTo(vic.Position)).FirstOrDefault();
            if (vic == null || att == null)
            {
                Log.Write("ROUGH test: two men free to be directed are needed within " + RoughNear + " m (" + men.Count + " around).");
                return;
            }
            bool saloon = _placeKind == "saloon";
            string seed = att.Handle + "|" + vic.Handle + "|test" + now;
            SceneCast cast = BuildCast(att, vic, null, saloon, me, now, seed);
            string cant = TroubleCant("rough", att, vic, cast);
            if (cant != null)
            {
                Log.Write("ROUGH test: " + TalkName(att) + " can't rough up " + TalkName(vic) + " here (" + cant + ").");
                return;
            }
            string answer = RoughTestAnswers[_roughTestTurn++ % RoughTestAnswers.Length];
            Scene s = Scenes.Rough(cast, seed, false, answer);
            float apart = att.Position.DistanceTo(vic.Position);
            if (apart > 2.6f)
                s.Beats.Insert(0, new Beat(0, null, null, "meet"));   // he walks over to the victim, as in a real scene
            Log.Write("ROUGH test: " + TalkName(att) + " (" + Natives.ModelName(att) + ", " + (cast.Tempers[0] ?? "no temper") + ") picks on " + TalkName(vic) + " (" + Natives.ModelName(vic) + ", " +
                      (AtBar(vic) ?? "not at the bar: " + ScenarioName(vic)) + ", " + (cast.Tempers[1] ?? "no temper") + "), " + (int)apart + " m apart, " + (int)vic.Position.DistanceTo(me.Position) +
                      " m from you: the whole scene, and he answers " + answer + " (" + (answer == "sharp" ? "swings back" : answer == "cold" ? "walks off promising more" : "runs") + ")");
            StartScene(s, new List<Ped> { att, vic }, cast, seed, now, test: true);
        }

        /// The bar scenario this person is in, by name, or null.
        private static string AtBar(Ped p)
        {
            uint type = (uint)TASK._GET_SCENARIO_POINT_TYPE_PED_IS_USING(p.Handle);
            foreach (string s in BarScenarios)
                if (type == Natives.Hash(s) || PED.IS_PED_USING_SCENARIO_HASH(p.Handle, Natives.Hash(s)))
                    return s;
            return null;
        }

        /// The scenario this person is in, by name where it's one we know, else its number; "none" when there isn't one.
        private static string ScenarioName(Ped p)
        {
            if (!PED.IS_PED_USING_ANY_SCENARIO(p.Handle))
                return "none";
            uint type = (uint)TASK._GET_SCENARIO_POINT_TYPE_PED_IS_USING(p.Handle);
            string known = BarScenarios.Concat(OtherScenarios).FirstOrDefault(s => Natives.Hash(s) == type);
            return known ?? "scenario 0x" + type.ToString("X8");
        }

        private static float Flat(Vector3 a, Vector3 b)
        {
            float dx = a.X - b.X, dy = a.Y - b.Y;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }

        private static float RoughHeading(Vector3 from, Vector3 to)
        {
            return (float)(Math.Atan2(-(to.X - from.X), to.Y - from.Y) * 180.0 / Math.PI);
        }

        /// F7: which of the moves' animation sets the game has, which way each angle drives the victim, who is in a
        /// scenario near you (the bar marked), and the surfaces by the nearest man.
        private string RoughStatus()
        {
            var parts = new List<string>();
            foreach (string d in new[] { SmashDict, WallDict, DrunkDict })
                parts.Add(d.Substring(d.LastIndexOf('@') + 1) + (STREAMING.DOES_ANIM_DICT_EXIST(d) ? " yes" : " NO"));
            if (STREAMING.HAS_ANIM_DICT_LOADED(SmashDict) && STREAMING.HAS_ANIM_DICT_LOADED(WallDict))
                parts.Add(RoughMoves().Count + " angles read (listed under ROUGH moves)");
            else
            {
                STREAMING.REQUEST_ANIM_DICT(SmashDict);   // read on the next F7
                STREAMING.REQUEST_ANIM_DICT(WallDict);
            }
            Ped me = Game.Player.Character;
            var near = World.GetAllPeds().Where(p => p != null && p.Exists() && p.Handle != me.Handle && PED.IS_PED_HUMAN(p.Handle) && p.Position.DistanceTo(me.Position) < 20f)
                                         .OrderBy(p => p.Position.DistanceTo(me.Position)).ToList();
            var bar = near.Where(p => PED.IS_PED_USING_ANY_SCENARIO(p.Handle)).Take(10)
                          .Select(p => TalkName(p) + " (" + (AtBar(p) != null ? "AT THE BAR " + AtBar(p) : ScenarioName(p)) + ", " +
                                       (int)p.Position.DistanceTo(me.Position) + " m, facing " + (int)ENTITY.GET_ENTITY_HEADING(p.Handle) + ")").ToList();
            parts.Add("in scenarios near you: " + (bar.Count == 0 ? "nobody" : string.Join("; ", bar)));
            Ped man = near.FirstOrDefault(p => PED.IS_PED_MALE(p.Handle));
            if (man != null)
            {
                List<Surface> by = RoughSurfaces(man);
                parts.Add("by " + TalkName(man) + ": " + (by.Count == 0 ? "no surface" : string.Join(", ", by.Select(Describe))));
            }
            return string.Join("; ", parts);
        }
    }
}
