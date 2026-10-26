// Subtitles are the game's own now ([Output] Subtitles = game): RealNPCs draws none (the white ones were the game's
// objective line, and showed words the voice wasn't saying). Which way of playing a line makes the game show its own
// subtitle isn't known, so Ctrl+Shift+F7 plays one greeting from the person nearest you in each way in turn, five
// seconds apart, each announced by a numbered top-left update ("Subtitle test 3 of 8"): say which numbers got the
// game's subtitle, and lines said to you will be played that way (and lines between other people the quiet way).
// The game's own Subtitles option has to be on for any of them; the log says whether it is.
using System;
using System.Linq;
using System.Windows.Forms;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private static readonly KeySpec SubtitleProbeKey = KeySpec.Parse("Ctrl+Shift+F7", Keys.F7);

        // each way: who it's said to (you, another person near, nobody), the struct's sync flag and its two unnamed flags,
        // or the scripted-speech way
        private static readonly string[] ProbeWays =
        {
            "to you, flags 1/1 (as lines are played now)", "to you, flags 0/0", "to you, flags 1/0", "to you, flags 0/1",
            "to you, synced, flags 1/1", "to you, the scripted way", "to another person, flags 1/1", "to nobody, flags 1/1"
        };

        private bool _subProbeRequested, _subNoted;
        private int _subWay = -1, _subNextAt, _subScripted = -1, _subScriptedUntil;
        private Ped _subWho, _subOther;
        private string _subLast;
        private Action _aSubtitles;

        private void SubtitleStep(int now)
        {
            if (_aSubtitles == null)
                _aSubtitles = () => SubtitleTick(_now);
            if (!_subNoted || _subProbeRequested || _subWay >= 0)
                Safe("subtitle probe", _aSubtitles);
        }

        private void SubtitleTick(int now)
        {
            if (!_subNoted)
            {
                _subNoted = true;
                Log.Write("SUBTITLES: RealNPCs draws none of its own; the game's own Subtitles option is " + (HUD.IS_SUBTITLE_PREFERENCE_SWITCHED_ON() ? "on" : "OFF (turn it on in the game's settings to see any)") +
                          "; [Output] Subtitles = " + _cfg.Subtitles);
            }
            if (_subProbeRequested)
            {
                _subProbeRequested = false;
                ProbeStart(now);
                return;
            }
            if (_subWay < 0)
                return;
            if (_subScripted >= 0)
            {
                int st = Speech.ScriptedPlay(_subScripted);
                if (st != 0 || now > _subScriptedUntil)
                {
                    Log.Write("SUBTITLE probe " + (_subWay + 1) + " of " + ProbeWays.Length + ": the scripted way " + (st == 1 ? "is playing" : st == 2 ? "played and finished" : "never started"));
                    _subScripted = -1;
                }
            }
            if (now < _subNextAt)
                return;
            _subWay++;
            if (_subWay >= ProbeWays.Length || _subWho == null || !_subWho.Exists() || ENTITY.IS_ENTITY_DEAD(_subWho.Handle))
            {
                Log.Write("SUBTITLE probe over" + (_subWay < ProbeWays.Length ? " early: " + _subLast + " is gone" : "") + ". Say which numbers showed the game's own subtitle.");
                _subWay = -1;
                return;
            }
            ProbePlay(_subWay, now);
            _subNextAt = now + 5000;
        }

        private void ProbeStart(int now)
        {
            if (_subWay >= 0)
            {
                Log.Write("SUBTITLE probe: one is already going on.");
                return;
            }
            Ped me = Game.Player.Character;
            var near = World.GetAllPeds().Where(p => p != null && p.Exists() && p.Handle != me.Handle && p.Position.DistanceTo(me.Position) < 10f && TalkLeftOut(p, me) == null)
                                         .OrderBy(p => p.Position.DistanceTo(me.Position)).ToList();
            if (near.Count == 0)
            {
                Log.Write("SUBTITLE probe: nobody near enough (within 10 m, free, not one of the story's own people).");
                Feed.Notice("Subtitle test", "Stand near someone first.");
                return;
            }
            _subWho = near[0];
            _subOther = near.Count > 1 ? near[1] : null;
            _subLast = TalkName(_subWho);
            _subWay = -1;
            _subNextAt = now;
            Log.Write("SUBTITLE probe: " + _subLast + " (" + Natives.ModelName(_subWho) + ", " + (int)_subWho.Position.DistanceTo(me.Position) + " m) greets you " + ProbeWays.Length +
                      " ways, 5 s apart" + (_subOther != null ? "; " + TalkName(_subOther) + " is the other person" : "; nobody else near, so way 7 is said to nobody") +
                      "; the game's Subtitles option is " + (HUD.IS_SUBTITLE_PREFERENCE_SWITCHED_ON() ? "on" : "OFF"));
        }

        /// One way: a numbered top-left update, then a greeting (the first of the set the voice will play).
        private void ProbePlay(int way, int now)
        {
            int n = way + 1, me = Game.Player.Character.Handle, h = _subWho.Handle;
            Feed.Notice("Subtitle test", n + " of " + ProbeWays.Length + ": " + ProbeWays[way]);
            uint prm = Speech.ParamHash(0);
            if (way == 5)
            {
                foreach (string line in Speech.Greet)
                {
                    int handle = Speech.ScriptedMake(h, line, prm, me);
                    if (handle >= 0)
                    {
                        _subScripted = handle;
                        _subScriptedUntil = now + 3000;
                        Log.Write("SUBTITLE probe " + n + " of " + ProbeWays.Length + " (" + ProbeWays[way] + "): " + line + " made (handle " + handle + ")");
                        return;
                    }
                }
                Log.Write("SUBTITLE probe " + n + " of " + ProbeWays.Length + " (" + ProbeWays[way] + "): no scripted speech could be made");
                return;
            }
            int listener = way == 6 ? (_subOther != null && _subOther.Exists() ? _subOther.Handle : 0) : way == 7 ? 0 : me;
            int sync = way == 4 ? 1 : 0, v7 = way == 1 || way == 3 ? 0 : 1, v8 = way == 1 || way == 2 ? 0 : 1;
            foreach (string line in Speech.Greet)
            {
                if (Speech.PlayWith(h, line, prm, listener, sync, v7, v8))
                {
                    Log.Write("SUBTITLE probe " + n + " of " + ProbeWays.Length + " (" + ProbeWays[way] + "): " + _subLast + " says " + line);
                    return;
                }
            }
            Log.Write("SUBTITLE probe " + n + " of " + ProbeWays.Length + " (" + ProbeWays[way] + "): the game turned every greeting down");
        }

        /// F7: the setting, the game's own Subtitles option, and the probe.
        private string SubtitlesStatus()
        {
            return "RealNPCs draws none; [Output] Subtitles = " + _cfg.Subtitles + "; the game's own option " + (HUD.IS_SUBTITLE_PREFERENCE_SWITCHED_ON() ? "on" : "OFF") +
                   "; news to " + _cfg.NewsRoute + ", faction news to " + _cfg.WarRoute + "; probe (Ctrl+Shift+F7) " + (_subWay >= 0 ? "at way " + (_subWay + 1) + " of " + ProbeWays.Length : "idle");
        }
    }
}
