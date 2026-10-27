// CasualHookups (another mod: a native .asi by its own author, in the game folder) with RealNPCs deciding who is
// willing (2026-10-26, Lex's choice). RealNPCs can't start its scene: it has its own Flirt prompt in the lock-on menu (its
// PromptKey, G as it comes) and its own dice. So with [Romance] CasualHookupsDecides on and CasualHookups.asi present:
//   - a woman's yes to the player's flirting here (FlirtResult.Tryst) leaves her willing for a while (the tryst offer,
//     with no fade of ours: TrystAsked only says the words);
//   - its key is held (DISABLE_CONTROL_ACTION, every frame) while the lock-on target is a woman who isn't willing, but
//     never G (INPUT_INTERACT_OPTION1), which enRAGE's own lock-on prompts use (Interrogate, Give Her a Few Dollars);
//   - once (Lex's choices): its key moved off G to B, her clothes back after (femaleStaysNaked false: enRAGE knows its
//     people by their look), and its chances set to always give in (0/100/0) only once its key is off G, after a copy
//     of its ini goes to RealNPCs\backup (that copy is also the mark that it was done: the panel's changes stand).
//     B is the lock-on menu's horse-brush key; on foot B also opens the satchel, which the first test will show.
// Not known yet: whether the hold stops its prompt, and whether it reads its ini while running or only at the start.
// The log says what happened (HOOKUPS ...): the key pressed, held, and whether a scene seemed to follow (the player's
// controls off or his undressing, or the two of them within 0.9 m, for 3 s; judged from his side, since its strings
// speak of hiding the woman and replacing a stale one), with her handle's state, so a night still counts here.
using System;
using System.Collections.Generic;
using System.IO;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private const string HookupsGKey = "INPUT_INTERACT_OPTION1", HookupsBKey = "INPUT_INTERACT_HORSE_BRUSH";
        private const uint HookupsGHash = 0x760A9C6F;   // INPUT_INTERACT_OPTION1, G: enRAGE's lock-on prompts, never held
        private bool _hookupsToldNoHold, _hookupsToldHoldFailed;
        private const int HookupWillingMs = 300000;   // how long a yes leaves her willing for CasualHookups' Flirt

        private sealed class HookupRun
        {
            public int Handle, PressedAt, CloseSince, StartedAt, ApartSince, NextLook;
            public string Name, Kind, After;
            public Person Who;
            public TrystOffer Offer;   // her yes, for RealNPCs' own night if CasualHookups starts nothing
            public bool Willing, Stirred;   // Stirred: any sign of its scene at all (controls off, undressing, the two close)
        }

        private const int HookupFallbackMs = 10000;   // no sign of its scene this long after its key: RealNPCs' own night

        private string _hookupsIni;          // CasualHookups.ini, when CasualHookups.asi is in the game folder
        private string _hookupsKey;          // its PromptKey
        private string _hookupsStartKey;     // the PromptKey it started the game with
        private uint _hookupsKeyHash;
        private uint _hookupsStartHash;      // its key when the game started: it may go on using that one until the next start
        private string _hookupsOdds = "?";   // its three chances, as read
        private DateTime _hookupsStamp;
        private int _hookupsNextRead, _hookupsHeld, _hookupsHints, _hookupsScenes;
        private HookupRun _hookRun;
        private string _hookupsLast;
        private bool _hookupsLooked;
        private Action _aHookups;

        /// CasualHookups is installed and RealNPCs decides who it works on.
        private bool HookupsLinked
        {
            get { return _hookupsIni != null && _cfg.Romance && _cfg.Trysts && _cfg.CasualHookupsDecides; }
        }

        /// The game folder (the scripts folder's parent), where .asi mods sit.
        private string GameDir()
        {
            string scripts = Path.GetDirectoryName(base.FileName);
            return scripts != null ? Path.GetDirectoryName(scripts) : null;
        }

        /// Once at the start: is it there, and (if RealNPCs decides) its chances and key set once.
        private void HookupsFind()
        {
            _hookupsLooked = true;
            string dir = GameDir();
            if (dir == null || !File.Exists(Path.Combine(dir, "CasualHookups.asi")))
            {
                Log.Write("HOOKUPS: CasualHookups isn't installed; a woman's yes leads to RealNPCs' own night (a fade to black)");
                return;
            }
            string ini = Path.Combine(dir, "CasualHookups.ini");
            if (!File.Exists(ini))
            {
                Log.Write("HOOKUPS: CasualHookups.asi is installed but its ini isn't there yet (it writes one when the game starts); RealNPCs leaves it alone this time");
                return;
            }
            _hookupsIni = ini;
            HookupsRead();
            _hookupsStartHash = _hookupsKeyHash;
            _hookupsStartKey = _hookupsKey;
            try
            {
                // RealNPCs moved its key after this game started (and was reloaded since, with Insert): it may still be on
                // the one in the copy made before (File.Copy keeps the old write time, so the copy's creation time)
                string mark = Path.Combine(_dir, "backup", "CasualHookups.ini.before-realnpcs");
                if (File.Exists(mark) && File.GetCreationTimeUtc(mark) > System.Diagnostics.Process.GetCurrentProcess().StartTime.ToUniversalTime())
                {
                    IniEntry was = IniDoc.Read(mark, false).FindKey("PromptKey");
                    if (was != null && was.Value.Trim().Length > 0)
                    {
                        _hookupsStartKey = was.Value.Trim();
                        _hookupsStartHash = Natives.Hash(_hookupsStartKey);
                    }
                }
            }
            catch
            {
            }
            if (_cfg.CasualHookupsDecides)
            {
                HookupsSetUpOnce();
                _hookupsStamp = default(DateTime);
                HookupsRead();
            }
            Log.Write("HOOKUPS: CasualHookups found (its Flirt on " + HookupsKeyText + ", chances " + _hookupsOdds + " turned down / yes at once / a second try). " +
                      (HookupsLinked ? "It works only on a woman who has said yes to your flirting here; RealNPCs doesn't fade to black itself." : "RealNPCs leaves it alone ([Romance] CasualHookupsDecides off, or Trysts or Romance off)."));
        }

        /// Its chances to always give in, and its key off enRAGE's G, once (the backup copy is the mark).
        private void HookupsSetUpOnce()
        {
            string mark = Path.Combine(_dir, "backup", "CasualHookups.ini.before-realnpcs");
            if (File.Exists(mark))
                return;
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(mark));
                File.Copy(_hookupsIni, mark, false);
            }
            catch (Exception ex)
            {
                Log.Write("HOOKUPS: couldn't copy CasualHookups.ini to RealNPCs\\backup, so nothing in it was changed: " + ex.Message);
                return;
            }
            IniDoc doc = IniDoc.Read(_hookupsIni, false);
            if (doc.Error != null)
            {
                Log.Write("HOOKUPS: couldn't read CasualHookups.ini: " + doc.Error);
                return;
            }
            // her clothes back after (Lex: enRAGE knows its people by their look), and its key off G (Lex: to B)
            string done = HookupsSet(doc, "femaleStaysNaked", "false");
            IniEntry key = doc.FindKey("PromptKey");
            if (key != null && key.Value.Trim().Equals(HookupsGKey, StringComparison.OrdinalIgnoreCase))
                done += HookupsSet(doc, "PromptKey", HookupsBKey);
            // always giving in only where its key can be held: on G, every woman would go along, whatever she thinks
            key = doc.FindKey("PromptKey");
            if (key != null && !key.Value.Trim().Equals(HookupsGKey, StringComparison.OrdinalIgnoreCase))
                done += HookupsSet(doc, "notInterested", "0") + HookupsSet(doc, "givesInImmediately", "100") + HookupsSet(doc, "needsSecondFlirt", "0");
            if (done.Length == 0)
            {
                Log.Write("HOOKUPS: CasualHookups.ini already set as RealNPCs needs it; nothing changed (a copy is in RealNPCs\\backup)");
                return;
            }
            string err = doc.Save();
            Log.Write(err != null ? "HOOKUPS: couldn't save CasualHookups.ini (" + err + "); nothing changed. The panel's Romance page can still try."
                                  : "HOOKUPS: set CasualHookups.ini so RealNPCs decides who's willing:" + done + ". Its old file is in RealNPCs\\backup. If it read its settings when the game started, these count from the next start.");
        }

        private static string HookupsSet(IniDoc doc, string key, string value)
        {
            IniEntry e = doc.FindKey(key);
            if (e == null || e.Value.Trim() == value || !doc.Set(e, value))
                return "";
            return " " + key + " " + value;
        }

        /// Its key and chances, again whenever the file changes (looked at every 5 s).
        private void HookupsRead()
        {
            if (_hookupsIni == null)
                return;
            try
            {
                DateTime stamp = File.GetLastWriteTimeUtc(_hookupsIni);
                if (stamp == _hookupsStamp && _hookupsKey != null)
                    return;
                _hookupsStamp = stamp;
                IniDoc doc = IniDoc.Read(_hookupsIni, false);
                if (doc.Error != null)
                    return;
                IniEntry k = doc.FindKey("PromptKey"), n = doc.FindKey("notInterested"), y = doc.FindKey("givesInImmediately"), s = doc.FindKey("needsSecondFlirt");
                string key = k != null && k.Value.Trim().Length > 0 ? k.Value.Trim() : HookupsGKey;
                if (key != _hookupsKey && _hookupsKey != null)
                    Log.Write("HOOKUPS: its Flirt key is now " + HookupsKeyName(key));
                _hookupsKey = key;
                _hookupsKeyHash = Natives.Hash(key);
                _hookupsOdds = (n != null ? n.Value.Trim() : "?") + "/" + (y != null ? y.Value.Trim() : "?") + "/" + (s != null ? s.Value.Trim() : "?");
            }
            catch
            {
            }
        }

        /// Its key for the player: the one in its ini, and the one it started with if that differs (it may read its ini
        /// only when the game starts).
        private string HookupsKeyText
        {
            get { return _hookupsStartKey != null && !_hookupsStartKey.Equals(_hookupsKey, StringComparison.OrdinalIgnoreCase) ? HookupsKeyName(_hookupsKey) + ", or " + HookupsKeyName(_hookupsStartKey) + " until the game restarts" : HookupsKeyName(_hookupsKey); }
        }

        /// "B", "G", or the name as written.
        private static string HookupsKeyName(string key)
        {
            if (key == null)
                return "?";
            if (key.Equals(HookupsGKey, StringComparison.OrdinalIgnoreCase))
                return "G";
            if (key.Equals(HookupsBKey, StringComparison.OrdinalIgnoreCase))
                return "B";
            return key;
        }

        private void HookupsStep(int now)
        {
            if (_aHookups == null)
                _aHookups = () => HookupsTick(_now);
            if (!_hookupsLooked || HookupsLinked || _hookRun != null)
                Safe("casual hookups", _aHookups);
        }

        private unsafe void HookupsTick(int now)
        {
            if (!_hookupsLooked)
                HookupsFind();
            if (now >= _hookupsNextRead)
            {
                _hookupsNextRead = now + 5000;
                HookupsRead();
            }
            HookupsWatch(now);
            if (!HookupsLinked || _hookupsKeyHash == 0 || !PLAYER.IS_PLAYER_CONTROL_ON(Game.Player.Handle))
                return;
            int target = 0;
            if (!PLAYER.IS_PLAYER_FREE_AIMING(Game.Player.Handle))
                PLAYER.GET_PLAYER_INTERACTION_TARGET_ENTITY(Game.Player.Handle, &target, false, false);
            if (target == 0 || !ENTITY.DOES_ENTITY_EXIST(target) || !ENTITY.IS_ENTITY_A_PED(target) || !PED.IS_PED_HUMAN(target) || PED.IS_PED_MALE(target) || PED.IS_PED_A_PLAYER(target))
                return;
            TrystOffer o = _trystOffer;
            bool willing = o != null && o.Hookups && o.Night && o.Handle == target && now <= o.Until;
            if (!willing)
            {
                // its Flirt does nothing on her; never G, which enRAGE's own lock-on prompts use (Interrogate, its money)
                bool held = HookupsHold(_hookupsKeyHash) | (_hookupsStartHash != _hookupsKeyHash && HookupsHold(_hookupsStartHash));
                if (!_hookupsToldNoHold && (_hookupsKeyHash == HookupsGHash || _hookupsStartHash == HookupsGHash))
                {
                    _hookupsToldNoHold = true;
                    Log.Write(_hookupsKeyHash == HookupsGHash
                        ? "HOOKUPS: not holding CasualHookups' Flirt: it's on G, which enRAGE's own lock-on prompts use, so it rolls its own dice (the panel's Romance page can move it to B)"
                        : "HOOKUPS: until the game restarts CasualHookups may still be on G, which RealNPCs doesn't hold (enRAGE's own lock-on prompts use it), so till then its Flirt may work on anyone");
                }
                if ((held && PAD.IS_DISABLED_CONTROL_JUST_PRESSED(0, _hookupsKeyHash)) || (held && _hookupsStartHash != 0 && PAD.IS_DISABLED_CONTROL_JUST_PRESSED(0, _hookupsStartHash)))
                {
                    _hookupsHeld++;
                    string name = TalkName(new Ped(target));
                    bool flirted = o != null && o.Handle == target && now <= o.Until;
                    bool ours = flirted && o.Night;   // willing, but CasualHookups won't take her kind
                    Log.Write("HOOKUPS held: " + name + (ours ? " is willing, but CasualHookups turns her kind down; the night is RealNPCs' own" : flirted ? " liked your flirting, but not that much today" : " hasn't said yes to your flirting") + ", so CasualHookups' Flirt (" + HookupsKeyText + ") was held");
                    if (_hookupsHints < 3)
                    {
                        _hookupsHints++;
                        Feed.Notice("RealNPCs", ours ? name + " is willing. CasualHookups won't take her, so Hook up (H) to go somewhere quieter."
                                                     : flirted ? name + " liked your flirting, but she won't go further today. Hook up (H) to hear it from her."
                                                     : name + " hasn't said yes to you. Greet her (R), then Flirt (H); CasualHookups follows her answer.");
                    }
                    if (_hookRun == null)
                    {
                        _hookRun = new HookupRun { Handle = target, Name = name, PressedAt = now, Willing = false };   // to see whether the hold held
                        Log.Write("HOOKUPS her: " + HerState(target));
                    }
                }
                return;
            }
            // a press on her starts a new watch, unless a scene is already under way (2026-10-26: a watch on Pearl Babineaux
            // still waiting hid the press on Willa Tate)
            if ((_hookRun == null || _hookRun.StartedAt == 0) && (PAD.IS_CONTROL_JUST_PRESSED(0, _hookupsKeyHash) || (_hookupsStartHash != 0 && PAD.IS_CONTROL_JUST_PRESSED(0, _hookupsStartHash))))
            {
                if (_hookRun != null && _hookRun.Handle != target)
                    Log.Write("HOOKUPS: no longer watching " + _hookRun.Name + " (nothing came of it before a press on " + o.Name + ")");
                _hookRun = new HookupRun { Handle = target, Name = o.Name, Kind = o.Kind, Who = o.Who, Offer = o, PressedAt = now, Willing = true };
                Log.Write("HOOKUPS key pressed on " + o.Name + " (willing, " + o.Kind + "): CasualHookups' turn; her: " + HerState(target));
            }
        }

        // the kinds of women CasualHookups turns down whatever its chances say ("Lady is not interested" with notInterested
        // 0): seen on 2026-10-26 with Saint Denis' working girls (Willa Tate, Pearl Babineaux). More are learned when its key
        // on a willing woman starts nothing (Store.Counts "hookups won't:<model>"). Its own file lists the other towns'
        // saloon women beside this one, but that alone doesn't say it turns them down.
        private static readonly HashSet<string> HookupsWontSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "a_f_m_nbxwhore_01" };

        private static string ModelOf(int ped)
        {
            return ENTITY.DOES_ENTITY_EXIST(ped) ? NemesisSystem.Models.Name(ENTITY.GET_ENTITY_MODEL(ped)) : null;
        }

        /// CasualHookups won't take her kind: her yes leads to RealNPCs' own night instead.
        private bool HookupsWont(int ped)
        {
            string m = ModelOf(ped);
            return m != null && (HookupsWontSeen.Contains(m) || (_store != null && _store.Counts.ContainsKey("hookups won't:" + m.ToLowerInvariant())));
        }

        private void HookupsLearnWont(int ped, string name)
        {
            string m = ModelOf(ped);
            if (m == null || _store == null || HookupsWont(ped))
                return;
            _store.Counts["hookups won't:" + m.ToLowerInvariant()] = 1;
            Log.Write("HOOKUPS: CasualHookups won't take " + m + " (" + name + "); from now on that kind's yes leads to RealNPCs' own night");
        }

        /// Holds a key for this frame, unless it's G (enRAGE's) or unknown. True if held.
        private static bool HookupsHold(uint key)
        {
            if (key == 0 || key == HookupsGHash)
                return false;
            PAD.DISABLE_CONTROL_ACTION(0, key, false);
            return true;
        }

        /// What became of her handle, for the log: CasualHookups may hide her or put another ped in her place.
        private static string HerState(int h)
        {
            if (!ENTITY.DOES_ENTITY_EXIST(h))
                return "gone";
            return (ENTITY.IS_ENTITY_DEAD(h) ? "dead, " : "") + (ENTITY.IS_ENTITY_VISIBLE(h) ? "visible" : "hidden") + ", alpha " + ENTITY.GET_ENTITY_ALPHA(h) +
                   ", " + ENTITY.GET_ENTITY_COORDS(h, true, true).DistanceTo(Game.Player.Character.Position).ToString("0.0") + " m from you";
        }

        /// The player in a scene of CasualHookups': undressing (its outfit change), or his controls off.
        private static bool PlayerInScene(int me, bool controlOff)
        {
            return controlOff || ENTITY.IS_ENTITY_PLAYING_ANIM(me, "mech_inventory@clothing@outfit_change", "outfit_change_unarmed", 3);
        }

        /// After its key: did a scene follow, and when is it over.
        private void HookupsWatch(int now)
        {
            HookupRun h = _hookRun;
            if (h == null || now < h.NextLook)
                return;
            h.NextLook = now + 250;
            Ped me = Game.Player.Character;
            bool there = ENTITY.DOES_ENTITY_EXIST(h.Handle) && !ENTITY.IS_ENTITY_DEAD(h.Handle);
            float d = there ? ENTITY.GET_ENTITY_COORDS(h.Handle, true, true).DistanceTo(me.Position) : 999f;
            bool controlOff = !PLAYER.IS_PLAYER_CONTROL_ON(Game.Player.Handle);
            bool inScene = PlayerInScene(me.Handle, controlOff);   // from his side: she may be hidden or stood in for
            if (h.StartedAt == 0)
            {
                if (inScene || (there && d < 0.9f))
                    h.Stirred = true;
                if (h.Willing && !h.Stirred && now - h.PressedAt >= HookupFallbackMs)
                {
                    // her words said yes and CasualHookups started nothing (it turned Saint Denis' working girls down,
                    // 2026-10-26): the night is RealNPCs' own, so what she said and what happens agree (Lex)
                    Log.Write("HOOKUPS with " + h.Name + ": no sign of its scene " + HookupFallbackMs / 1000 + " s after its key (her: " + HerState(h.Handle) + ")");
                    HookupsLearnWont(h.Handle, h.Name);
                    _hookRun = null;
                    _hookupsLast = h.Name + ": CasualHookups started nothing, RealNPCs' night instead";
                    TrystFallback(h.Offer, "CasualHookups started nothing");
                    return;
                }
                if (inScene || (there && d < 0.9f))
                {
                    if (h.CloseSince == 0)
                        h.CloseSince = now;
                    if (now - h.CloseSince >= 3000)
                    {
                        h.StartedAt = now;
                        _hookupsScenes++;
                        if (h.Willing)
                        {
                            h.After = _engine.Tryst(h.Who, h.Kind, Bridge.Now());
                            _trystOffer = null;
                            _trystCount++;
                            _trystLast = h.Name + " (" + h.Kind + ", CasualHookups)";
                            Log.Write("HOOKUPS with " + h.Name + ": its scene seems to have begun " + (now - h.PressedAt) / 1000 + " s after its key (your controls " + (controlOff ? "off" : "on") + "; her: " + HerState(h.Handle) + "); noted as a night together");
                        }
                        else
                        {
                            Log.Write("HOOKUPS: a scene with " + h.Name + " seems to have begun though she never said yes to you: the hold on its key didn't work (your controls " + (controlOff ? "off" : "on") + "; her: " + HerState(h.Handle) + ")");
                            if (!_hookupsToldHoldFailed)
                            {
                                // with its chances at always giving in, every woman would go along: say how to have its own dice back
                                _hookupsToldHoldFailed = true;
                                Feed.Notice("RealNPCs", "RealNPCs couldn't stop CasualHookups' Flirt on a woman who hadn't said yes. To give it back its own chances, set them on the settings panel's Romance page.");
                            }
                        }
                    }
                }
                else
                    h.CloseSince = 0;
                if (h.StartedAt == 0 && (now - h.PressedAt > 60000 || !there))
                {
                    Log.Write(h.Willing ? "HOOKUPS with " + h.Name + ": nothing seemed to come of its key in " + (now - h.PressedAt) / 1000 + " s (" + (there ? d.ToString("0.0") + " m apart" : "she's gone") + ", your controls " + (controlOff ? "off" : "on") + ")"
                                        : "HOOKUPS held: no scene followed with " + h.Name + ", so the hold worked");
                    _hookupsLast = h.Name + (h.Willing ? ": nothing came of it" : ": held, no scene");
                    _hookRun = null;
                }
                return;
            }
            // under way: over once he's out of it (controls back, not undressing) and apart from her or she's gone, for
            // 3 s, or after 20 minutes
            bool apart = !inScene && (!there || d > 3f);
            if (apart)
            {
                if (h.ApartSince == 0)
                    h.ApartSince = now;
            }
            else
                h.ApartSince = 0;
            if ((h.ApartSince != 0 && now - h.ApartSince >= 3000) || now - h.StartedAt > 1200000)
            {
                if (h.Willing && h.After != null && there)
                    _ticker.Push(h.Name + ", thinking", h.After, "near", h.Handle);
                Log.Write("HOOKUPS over: " + h.Name + " (" + (now - h.StartedAt) / 1000 + " s; her: " + HerState(h.Handle) + ")" + (h.Willing ? "; she thinks: " + h.After : ""));
                _hookupsLast = h.Name + (h.Willing ? ", a night together" : ", unasked");
                _hookRun = null;
            }
        }

        /// F7.
        private string HookupsStatus()
        {
            if (!_hookupsLooked)
                return "not looked for yet";
            if (_hookupsIni == null)
                return "not installed";
            TrystOffer o = _trystOffer;
            return "installed, its Flirt on " + HookupsKeyText + ", chances " + _hookupsOdds + "; " + (HookupsLinked ? "RealNPCs decides" : "left alone") +
                   "; willing now: " + (o != null && o.Hookups && Game.GameTime <= o.Until ? o.Name + " (" + (o.Until - Game.GameTime) / 1000 + " s left)" : "nobody") +
                   "; held " + _hookupsHeld + " times, scenes seen " + _hookupsScenes + (_hookRun != null ? "; watching " + _hookRun.Name : "") + (_hookupsLast != null ? "; last " + _hookupsLast : "");
        }
    }
}
