// "Flirt" in the game's lock-on menu ([Romance] FlirtPrompt; Core/FlirtPromptRule.cs). While you're locked on to someone
// you could flirt with, the menu with Greet and Antagonize also offers Flirt, on the game's INTERACT_OPTION2 control
// (H on a keyboard, D-pad down on a pad): a menu entry can only show one of the game's controls, and none is on F2.
// Pressing it flirts as FlirtKey does, with the person you're locked on to (Flirt). It is the game's own prompt, put in
// the lock-on prompt group the game keeps for that person, and goes when the lock-on ends. Its own guard.
using System;
using System.Runtime.InteropServices;
using System.Text;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private const uint FlirtControl = 0x84543902;   // INPUT_INTERACT_OPTION2: H, or D-pad down
        private const ulong SetPromptText = 0x5DD02A8318420DD7, VarText = 0xFA925AC00EB830B9;
        private static IntPtr _literalTag, _flirtWord, _hookWord;   // kept for good: the game may hold on to the text
        private bool _flirtPromptHook;   // it reads "Hook up": she said yes to the flirting (Trysts.cs)
        private int _flirtPrompt, _flirtPromptGroup, _flirtPromptFor, _flirtPresses;
        private string _flirtPromptWho;
        private Action _aFlirtPrompt;

        private void FlirtPromptStep()
        {
            if (_aFlirtPrompt == null)
                _aFlirtPrompt = FlirtPromptTick;
            if (_cfg.FlirtPrompt || _flirtPrompt != 0)
                Safe("flirt prompt", _aFlirtPrompt);
        }

        private unsafe void FlirtPromptTick()
        {
            Ped me = Game.Player.Character;
            int target = 0;
            if (_cfg.FlirtPrompt && _cfg.Romance && !PLAYER.IS_PLAYER_FREE_AIMING(Game.Player.Handle))
                PLAYER.GET_PLAYER_INTERACTION_TARGET_ENTITY(Game.Player.Handle, &target, false, false);
            Ped who = target != 0 && ENTITY.DOES_ENTITY_EXIST(target) && ENTITY.IS_ENTITY_A_PED(target) ? new Ped(target) : null;
            bool known = who != null && _bridge != null && (_bridge.RecordAt(target) != null || _bridge.AcquaintanceAt(target) != null);
            if (who == null || !FlirtPromptRule.Offer(_cfg.FlirtPrompt, _cfg.Romance, PED.IS_PED_HUMAN(target), PED.IS_PED_A_PLAYER(target), !ENTITY.IS_ENTITY_DEAD(target),
                                                         IsGrownUp(who), Natives.StoryModel(who), ENTITY.IS_ENTITY_A_MISSION_ENTITY(target), known,
                                                         who.Position.DistanceTo(me.Position), PED.IS_PED_IN_COMBAT(target, 0), MISC.GET_MISSION_FLAG(),
                                                         PED.IS_PED_MALE(target) == PED.IS_PED_MALE(me.Handle), _cfg.SameSexRomance))
            {
                FlirtPromptEnd();
                return;
            }
            int group = HUD._UI_PROMPT_GET_GROUP_ID_FOR_TARGET_ENTITY(target);
            // after her yes the same prompt asks her somewhere quieter (Lex: greet, flirt, then hook up)
            bool hook = _trystOffer != null && _trystOffer.Handle == target && Game.GameTime <= _trystOffer.Until;
            if (_flirtPrompt == 0 || !HUD._UI_PROMPT_IS_VALID(_flirtPrompt) || _flirtPromptFor != target || _flirtPromptGroup != group || _flirtPromptHook != hook)
            {
                FlirtPromptEnd();
                _flirtPrompt = NewFlirtPrompt(group, hook);
                _flirtPromptHook = hook;
                _flirtPromptFor = target;
                _flirtPromptGroup = group;
                _flirtPromptWho = TalkName(who);
                Log.Detail("FLIRT prompt in the lock-on menu for " + _flirtPromptWho);
            }
            if (HUD._UI_PROMPT_HAS_STANDARD_MODE_COMPLETED(_flirtPrompt, 0))
            {
                _flirtPresses++;
                Log.Write("FLIRT prompt pressed: " + _flirtPromptWho);
                _flirtRequested = true;   // Flirt() takes the same lock-on target
                FlirtPromptEnd();         // a fresh one next frame, so it can be pressed again
            }
        }

        /// The game's own prompt: "Flirt" (or "Hook up", after her yes) on INTERACT_OPTION2, pressed (not held), in this
        /// lock-on group.
        private static int NewFlirtPrompt(int group, bool hook)
        {
            if (_literalTag == IntPtr.Zero)
            {
                _literalTag = Kept("LITERAL_STRING");
                _flirtWord = Kept("Flirt");
                _hookWord = Kept("Hook up");
            }
            int prompt = HUD._UI_PROMPT_REGISTER_BEGIN();
            HUD._UI_PROMPT_SET_CONTROL_ACTION(prompt, FlirtControl);
            ulong word = Function.Call<ulong>(VarText, 10, new InputArgument((ulong)_literalTag.ToInt64()), new InputArgument((ulong)(hook ? _hookWord : _flirtWord).ToInt64()));
            Function.Call(SetPromptText, prompt, new InputArgument(word));
            HUD._UI_PROMPT_SET_STANDARD_MODE(prompt, true);
            HUD._UI_PROMPT_SET_GROUP(prompt, group, 0);
            HUD._UI_PROMPT_REGISTER_END(prompt);
            HUD._UI_PROMPT_SET_ENABLED(prompt, true);
            HUD._UI_PROMPT_SET_VISIBLE(prompt, true);
            return prompt;
        }

        private static IntPtr Kept(string s)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(s);
            IntPtr at = Marshal.AllocHGlobal(bytes.Length + 1);
            Marshal.Copy(bytes, 0, at, bytes.Length);
            Marshal.WriteByte(at, bytes.Length, 0);
            return at;
        }

        /// Takes the prompt out of the menu (the lock-on ended, someone else, or the add-on stopping).
        private void FlirtPromptEnd()
        {
            if (_flirtPrompt != 0)
            {
                if (HUD._UI_PROMPT_IS_VALID(_flirtPrompt))
                    HUD._UI_PROMPT_DELETE(_flirtPrompt);
                _flirtPrompt = 0;
            }
            _flirtPromptFor = 0;
        }

        /// F7: on or off, the control, who it's offered for now, and how often it was pressed.
        private string FlirtPromptStatus()
        {
            if (!_cfg.FlirtPrompt || !_cfg.Romance)
                return "off ([Romance] " + (!_cfg.Romance ? "Romance" : "FlirtPrompt") + ")";
            return "on, H (INTERACT_OPTION2); " + (_flirtPrompt != 0 ? "in the lock-on menu for " + _flirtPromptWho : "not shown (nobody locked on to)") + "; pressed " + _flirtPresses + " times";
        }
    }
}
