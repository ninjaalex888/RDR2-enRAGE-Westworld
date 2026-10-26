// How people take what is done and said to them in a street scene ([Scenes] Reactions; the choice is
// Core/Reactions.cs). A little into each line the one it's said to looks at the speaker and answers with their body
// (the Director's React), and the man whose face has just gone into the bar reels ("hurt"): the street fight's own
// hurt move where the game has it, else a wince. Only for someone we may task (SteerCant); someone in a scenario only
// looks, as does one of enRAGE's own records; a story character not even that. Runs in its own guard ("reactions").
using System;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private const string StreetFightDict = "script_re@street_fight@v3", HurtClip = "hurt_exit_victim";
        private Director.Reaction _reaction;
        private Action _aReact;
        private int _reacted;
        private string _reactLast;
        private bool _reactProbed, _streetFight;

        /// From the Director: someone in the scene reacts. Played at once, in the "reactions" guard.
        private void OnReaction(Director.Reaction r)
        {
            if (_aReact == null)
                _aReact = ReactImpl;
            _reaction = r;
            Safe("reactions", _aReact);
        }

        private void ReactImpl()
        {
            Director.Reaction r = _reaction;
            _reaction = null;
            if (r == null || _cfg == null || !_cfg.Reactions || r.By == null || r.To == null || !r.By.Exists() || !r.To.Exists())
                return;
            int h = r.By.Handle;
            bool hurt = r.Tone == Reactions.HurtTone;
            string cant = MayDirectWhyNot(r.By);
            if (cant != null)
            {
                Log.Detail("REACT " + r.ByName + " left alone (" + cant + ")");
                return;
            }
            if (ENTITY.IS_ENTITY_DEAD(h) || PED.IS_PED_RAGDOLL(h) || PED.IS_PED_FLEEING(h) || (!hurt && ENTITY.GET_ENTITY_SPEED(h) > 1.2f))
                return;   // down, running or walking off: leave them to it
            TASK.TASK_LOOK_AT_ENTITY(h, r.To.Handle, 3500, 0, 51, 0);
            string kind = Reactions.Listener(r.Tone, r.Temper, r.Seed);
            if (kind == null)
                return;
            if (PED.IS_PED_USING_ANY_SCENARIO(h))
            {
                Log.Write("REACT " + r.ByName + " only looks at " + r.ToName + " (in a scenario: leaning, sitting or at work)");
                return;
            }
            string dict, clip;
            int flags = 8 | 16;   // upper body, secondary: their feet stay where the scene put them
            if (hurt && !r.Woman && _streetFight && STREAMING.HAS_ANIM_DICT_LOADED(StreetFightDict))
            {
                dict = StreetFightDict;
                clip = HurtClip;
                flags = 0;   // the street fight's own: the whole body, bent over and coming up again
            }
            else
            {
                dict = Reactions.Dict(r.Woman);
                clip = Reactions.Clip(kind, r.Woman, r.Seed);
            }
            if (clip == null || !STREAMING.HAS_ANIM_DICT_LOADED(dict))
            {
                STREAMING.REQUEST_ANIM_DICT(dict);
                Log.Detail("REACT " + r.ByName + ": " + dict + " not loaded yet");
                return;
            }
            TASK.TASK_PLAY_ANIM(h, dict, clip, 4f, -4f, -1, flags, 0f, false, 0, false, "", false);
            _reacted++;
            _reactLast = r.ByName + " " + Reactions.Verb(kind) + " (" + clip + ")";
            Log.Write("REACT " + r.ByName + " " + Reactions.Verb(kind) + (hurt ? " after the slam" : " at " + r.ToName + "'s " + r.Tone + " line") +
                      " (" + (r.Temper ?? "no temper") + "; " + clip + ")");
        }

        /// A scene's people's temperaments (Reactions.Listener): enRAGE's or the resident's own, else null.
        private string CastTemper(Ped p)
        {
            Someone s = p != null && p.Exists() ? SomeoneOf(p) : null;
            return s != null ? s.Temper : null;
        }

        /// Every 10 s with the rough moves: the street fight's hurt move kept loaded where the game has it, and once,
        /// in the log, which of the reactions' sets there are.
        private void ReactionSets()
        {
            if (!_cfg.Reactions)
                return;
            if (!_reactProbed)
            {
                _reactProbed = true;
                _streetFight = STREAMING.DOES_ANIM_DICT_EXIST(StreetFightDict);
                Log.Write("REACT sets: street fight hurt (" + StreetFightDict + ") " + (_streetFight ? "yes" : "no, so a wince instead") + "; men's silent gestures " +
                          (STREAMING.DOES_ANIM_DICT_EXIST(Gestures.MaleSilent) ? "yes" : "NO") + ", women's " + (STREAMING.DOES_ANIM_DICT_EXIST(Gestures.FemaleSilent) ? "yes" : "NO"));
            }
            if (_streetFight && !STREAMING.HAS_ANIM_DICT_LOADED(StreetFightDict))
                STREAMING.REQUEST_ANIM_DICT(StreetFightDict);
        }

        /// F7: on or off, the sets, how many reactions so far and the last.
        private string ReactionsStatus()
        {
            if (!_cfg.Reactions)
                return "off ([Scenes] Reactions)";
            string hurt = !_reactProbed ? "not looked yet" : !_streetFight ? "no (a wince instead)"
                        : STREAMING.HAS_ANIM_DICT_LOADED(StreetFightDict) ? "yes, " + ENTITY.GET_ANIM_DURATION(StreetFightDict, HurtClip).ToString("0.0") + " s" : "yes, loading";
            return "on; street fight hurt " + hurt + "; " + _reacted + " so far" + (_reactLast != null ? ", last: " + _reactLast : "");
        }
    }
}
