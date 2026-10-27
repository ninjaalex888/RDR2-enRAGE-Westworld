// A night together, off screen (0.6.3, [Romance] Trysts; Lex: the saloon's women always willing, single women likely,
// married ones less so, Interest.ForPlayer). When a woman says yes to the player's flirting, flirting with her again
// within two minutes is asking her somewhere quieter: the two lines (Core/Trysts.cs), then the screen fades out, an
// hour passes on the clock, it fades back in, and her thought after shows over her head. Nothing is shown. The player's
// controls are off while it's dark; a watchdog outside the feature's guard brings the screen back whatever happens.
using System;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private sealed class TrystOffer
        {
            public int Handle, Until;
            public string Name, Kind;
            public Person Who;
            public bool Hookups;   // the night is CasualHookups' (WestworldScript.Hookups.cs): no fade of ours
            public bool Night;     // her yes to the flirting is a yes to the night too; else asking gets her no (Trysts.NightChance)
            public int Greeted;    // he greeted her first (1) or flirted cold (-1)
        }

        private sealed class TrystRun
        {
            public TrystOffer Offer;
            public int Phase, At;
            public string After;
        }

        private TrystOffer _trystOffer;
        private TrystRun _tryst;
        private int _trystDarkSince, _trystCount, _trystHints;
        private string _trystLast;
        private Action _aTryst;

        /// A woman said yes (FlirtResult.Tryst): a second flirt soon is the ask.
        private void TrystOffered(FlirtResult r, string name, int ped)
        {
            if (r == null || !r.Tryst || r.Who == null || !_cfg.Trysts)
                return;
            // a kind CasualHookups turns down, or no to the night: the night is ours, or nobody's
            bool hookups = r.Night && HookupsLinked && !HookupsWont(ped);
            _trystOffer = new TrystOffer { Handle = ped, Name = name, Kind = r.TrystKind, Who = r.Who, Hookups = hookups, Night = r.Night, Greeted = r.Greeted,
                                           Until = Game.GameTime + (hookups ? HookupWillingMs : Trysts.OfferMs) };
            Log.Write("TRYST offered: " + name + " (" + r.TrystKind + ", chance " + r.Chance + "%, the night " + r.NightChance + "% -> " + (r.Night ? "yes" : "no") +
                      (r.Greeted > 0 ? ", greeted first" : r.Greeted < 0 ? ", flirted cold" : "") + "): " +
                      (!r.Night ? "asking her (Hook up, H) gets a no today" : hookups ? "willing for CasualHookups' Flirt (" + HookupsKeyText + ") for " + HookupWillingMs / 1000 + " s" : "Hook up (H) within " + Trysts.OfferMs / 1000 + " s"));
            if (r.Night && _trystHints < 3)
            {
                _trystHints++;
                Feed.Notice("RealNPCs", hookups ? name + " is willing: Hook up (H), then CasualHookups' Flirt (" + HookupsKeyText + "), in the next five minutes."
                                                : name + " is willing: Hook up (H) within two minutes to go somewhere quieter.");
            }
        }

        /// The second flirt, on the woman who said yes: true if it was taken as the ask.
        private bool TrystAsked(Ped her)
        {
            TrystOffer o = _trystOffer;
            if (o == null || her == null || her.Handle != o.Handle || Game.GameTime > o.Until || _tryst != null || !_cfg.Trysts)
                return false;
            Ped me = Game.Player.Character;
            string cant = MISC.GET_MISSION_FLAG() ? "a story mission" : PED.IS_PED_IN_COMBAT(me.Handle, 0) ? "there's fighting going on" : PED.IS_PED_ON_MOUNT(me.Handle) ? "you're on horseback" :
                          PED.IS_PED_IN_COMBAT(her.Handle, 0) || PED.IS_PED_FLEEING(her.Handle) ? "she's frightened off" : her.Position.DistanceTo(me.Position) > 6f ? "she's too far off" : null;
            if (cant != null)
            {
                Log.Write("TRYST not now: " + cant);
                return true;   // still taken as the ask, so the flirt doesn't count against her
            }
            string seed = o.Who.Id + "|" + Bridge.Now() / 60;
            string ask = Trysts.Ask(seed), yes = Trysts.Yes(o.Kind, seed);
            Feed.Say(_engine.Player.FirstName, ask, FlirtVoice(_engine.Player.FirstName, Speech.WhenFirst(me.Handle, FlirtVoices.You(true, seed), her.Handle)), ahead: true);
            if (!o.Night)
            {
                // she liked the flirting, not that much: her no, in words and a voice that fit (a cold approach hears of it)
                string no = Trysts.NotTonight(o.Kind, o.Greeted, seed);
                Feed.Say(o.Name, no, FlirtVoice(o.Name, Speech.WhenFirst(her.Handle, FlirtVoices.Reply(o.Who, "no", o.Greeted < 0 ? "cold" : "busy", 0, seed), me.Handle)), toYou: true, ahead: true);
                _flirtSaidUntil = Game.GameTime + 7000;
                _trystOffer = null;
                Log.Write("TRYST asked: " + o.Name + " (" + o.Kind + "): " + ask + " -> not tonight" + (o.Greeted < 0 ? " (flirted cold)" : "") + ": " + no);
                return true;
            }
            Feed.Say(o.Name, yes, FlirtVoice(o.Name, Speech.WhenFirst(her.Handle, FlirtVoices.Night(o.Kind, seed), me.Handle)), toYou: true, ahead: true);
            _flirtSaidUntil = Game.GameTime + 7000;
            if (o.Hookups)
            {
                // the night is CasualHookups': the words only, and she stays willing a while longer
                o.Until = Math.Max(o.Until, Game.GameTime + HookupWillingMs);
                Log.Write("TRYST asked: " + o.Name + " (" + o.Kind + "): " + ask + " -> " + yes + "; CasualHookups' Flirt (" + HookupsKeyText + ") next");
                return true;
            }
            _tryst = new TrystRun { Offer = o, Phase = 0, At = Game.GameTime + 4500 };
            _trystOffer = null;
            Log.Write("TRYST asked: " + o.Name + " (" + o.Kind + "): " + ask + " -> " + yes);
            return true;
        }

        /// She said yes and CasualHookups started nothing: she says yes again, in her voice, and the night is RealNPCs' own,
        /// so her words and what happens agree (2026-10-26, Lex). If now won't do, a second flirt still asks her.
        private void TrystFallback(TrystOffer o, string why)
        {
            if (o == null || _tryst != null)
                return;
            Ped me = Game.Player.Character;
            bool there = ENTITY.DOES_ENTITY_EXIST(o.Handle) && !ENTITY.IS_ENTITY_DEAD(o.Handle) && new Ped(o.Handle).Position.DistanceTo(me.Position) <= 8f;
            string cant = !there ? "she's gone or too far off" : MISC.GET_MISSION_FLAG() ? "a story mission" : PED.IS_PED_IN_COMBAT(me.Handle, 0) ? "there's fighting going on" :
                          PED.IS_PED_ON_MOUNT(me.Handle) ? "you're on horseback" : PED.IS_PED_IN_COMBAT(o.Handle, 0) || PED.IS_PED_FLEEING(o.Handle) ? "she's frightened off" : null;
            o.Hookups = false;
            if (cant != null)
            {
                o.Until = Game.GameTime + Trysts.OfferMs;
                _trystOffer = there ? o : null;
                Log.Write("TRYST " + o.Name + ": " + why + ", and RealNPCs' own night can't be now (" + cant + ")" + (there ? "; flirt with her again to ask" : ""));
                return;
            }
            string seed = o.Who.Id + "|" + Bridge.Now() / 60;
            string yes = Trysts.Yes(o.Kind, seed);
            Feed.Say(o.Name, yes, FlirtVoice(o.Name, Speech.WhenFirst(o.Handle, FlirtVoices.Night(o.Kind, seed), me.Handle)), toYou: true, ahead: true);
            _flirtSaidUntil = Game.GameTime + 7000;
            _trystOffer = null;
            _tryst = new TrystRun { Offer = o, Phase = 0, At = Game.GameTime + 4500 };
            Log.Write("TRYST " + o.Name + " (" + o.Kind + "): " + why + ", so RealNPCs' own night: " + yes);
        }

        private void TrystStep(int now)
        {
            if (_aTryst == null)
                _aTryst = () => TrystTick(_now);
            Safe("trysts", _aTryst);
        }

        private void TrystTick(int now)
        {
            TrystRun t = _tryst;
            if (t == null || now < t.At)
                return;
            int h = t.Offer.Handle, player = Game.Player.Handle;
            switch (t.Phase)
            {
                case 0:   // the words said: dark, if nothing has come between
                    if (MISC.GET_MISSION_FLAG() || PED.IS_PED_IN_COMBAT(Game.Player.Character.Handle, 0) || !ENTITY.DOES_ENTITY_EXIST(h) || ENTITY.IS_ENTITY_DEAD(h))
                    {
                        Log.Write("TRYST called off before the dark: something came between");
                        _tryst = null;
                        return;
                    }
                    if (MayDirect(new Ped(h)))
                        TASK.TASK_STAND_STILL(h, 8000);
                    PLAYER.SET_PLAYER_CONTROL(player, false, 0, false);
                    CAM.DO_SCREEN_FADE_OUT(1500);
                    _trystDarkSince = now;
                    t.Phase = 1;
                    t.At = now + 1700;
                    break;
                case 1:   // dark: an hour passes
                    if (!CAM.IS_SCREEN_FADED_OUT() && now - _trystDarkSince < 4000)
                    {
                        t.At = now + 200;
                        return;
                    }
                    CLOCK.ADD_TO_CLOCK_TIME(1, 0, 0);
                    t.After = _engine.Tryst(t.Offer.Who, t.Offer.Kind, Bridge.Now());
                    t.Phase = 2;
                    t.At = now + 2500;
                    break;
                case 2:   // back
                    CAM.DO_SCREEN_FADE_IN(1500);
                    PLAYER.SET_PLAYER_CONTROL(player, true, 0, false);
                    _trystDarkSince = 0;
                    t.Phase = 3;
                    t.At = now + 2200;
                    break;
                default:   // her thought, after
                    if (t.After != null && ENTITY.DOES_ENTITY_EXIST(h))
                        _ticker.Push(t.Offer.Name + ", thinking", t.After, "near", h);
                    _trystCount++;
                    _trystLast = t.Offer.Name + " (" + t.Offer.Kind + ")";
                    Log.Write("TRYST over: " + t.Offer.Name + " (" + t.Offer.Kind + "); she thinks: " + t.After);
                    _tryst = null;
                    break;
            }
        }

        /// InWorld() is false while the screen is faded out, so the tryst's dark is stepped from here, or it never ends
        /// (2026-10-26: Lex was left on a black screen with the controls off; the step and its watchdog both sat behind
        /// that gate, in Tick04).
        private void TrystInTheDark()
        {
            if (_engine == null || (_tryst == null && _trystDarkSince == 0) || Game.IsLoading || Game.IsPaused)
                return;
            int now = Game.GameTime;
            _now = now;
            TrystWatchdog(now);
            TrystStep(now);
        }

        /// Outside the feature's guard, every tick: if the screen has been dark for a tryst longer than it ever should,
        /// bring it back and give the player his controls, whatever went wrong.
        private void TrystWatchdog(int now)
        {
            if (_trystDarkSince == 0 || now - _trystDarkSince < 12000)
                return;
            try
            {
                CAM.DO_SCREEN_FADE_IN(500);
                PLAYER.SET_PLAYER_CONTROL(Game.Player.Handle, true, 0, false);
                Log.Write("TRYST watchdog: the screen was dark too long; brought back");
            }
            catch
            {
            }
            _trystDarkSince = 0;
            _tryst = null;
        }

        /// A mission started, or the add-on is stopping: the light back and the controls with it, if it was dark.
        private void TrystAbort()
        {
            _trystOffer = null;
            _hookRun = null;
            if (_tryst == null && _trystDarkSince == 0)
                return;
            try
            {
                if (_trystDarkSince != 0 || (_tryst != null && _tryst.Phase >= 1))
                {
                    CAM.DO_SCREEN_FADE_IN(500);
                    PLAYER.SET_PLAYER_CONTROL(Game.Player.Handle, true, 0, false);
                }
            }
            catch
            {
            }
            _trystDarkSince = 0;
            _tryst = null;
        }

        /// F7: the setting, the chances, a waiting offer, this session's.
        private string TrystStatus()
        {
            if (!_cfg.Trysts)
                return "off ([Romance] Trysts); women answer by chance: single " + _cfg.WillingSingle + "%, married or spoken for " + _cfg.WillingTaken + "%, the saloon's always";
            string offer = _trystOffer != null && Game.GameTime < _trystOffer.Until ? _trystOffer.Name + " (" + _trystOffer.Kind + ", " + (_trystOffer.Until - Game.GameTime) / 1000 + " s left)" : "none";
            return "on; women answer by chance: single " + _cfg.WillingSingle + "%, married or spoken for " + _cfg.WillingTaken + "%, the saloon's always, what they think of you tipping it; " +
                   "offer waiting: " + offer + (_tryst != null ? "; one going on (phase " + _tryst.Phase + ")" : "") + "; this session " + _trystCount + (_trystLast != null ? ", last " + _trystLast : "");
        }
    }
}
