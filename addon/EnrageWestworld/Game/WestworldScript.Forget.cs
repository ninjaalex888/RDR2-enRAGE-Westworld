// "Reset RealNPCs Memory" (Lex, 2026-10-30, with a screenshot of a station clerk's list: Telegraph, Buy a Newspaper, Send
// Mail, Receive Mail, Train Tickets, Pay Bounty). At a station agent or clerk the list also offers it, on Q (a pad's RB:
// INPUT_INTERACT_LOCKON_TARGET_INFO, which that list doesn't use), held two seconds, since it can't be undone: everyone
// forgets what they know and feel about you and the world stays (Core/Forget.cs, his pick of three). With enRAGE installed
// its townsfolk forget you too (their fear, fondness, grudge and favours, what they saw of you, knowing you at all) and the
// strangers' grudges building against you are dropped; its nemeses and their hate are left to it. A copy of RealNPCs' save
// and of enRAGE's people, world and nemeses goes to RealNPCs\backup first, and if a copy fails nothing is forgotten.
// Without enRAGE, RealNPCs' part alone. [Features] ResetAtClerk; the same from the Testing page's Memory rows.
using System;
using System.IO;
using EnrageWestworld.Core;
using RDR2;
using RDR2.Native;

namespace EnrageWestworld.InGame
{
    public partial class WestworldScript
    {
        private const uint ForgetControl = 0x31219490;   // INPUT_INTERACT_LOCKON_TARGET_INFO: Q, or a pad's RB
        private const int ForgetHoldMs = 2000;
        private static IntPtr _forgetWord;
        private int _forgetPrompt, _forgetPromptFor, _forgetPromptGroup, _forgetNotBefore;
        private bool _forgetPromptNoted;
        private Action _aForgetPrompt;

        /// Everyone forgets you; the world stays. What to say back.
        private string ForgetYou(string how)
        {
            if (_store == null || _engine == null)
                return "Nothing to forget yet: RealNPCs hasn't finished loading.";
            if (MISC.GET_MISSION_FLAG())
                return "Not during a story mission.";
            bool enrage = _bridge != null && _bridge.Present;
            string stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss"), kept = Path.Combine(_dir, "backup");
            try
            {
                Save();
                Directory.CreateDirectory(kept);
                if (File.Exists(StorePath))
                    File.Copy(StorePath, Path.Combine(kept, Path.GetFileNameWithoutExtension(StorePath) + ".before-forget-you-" + stamp + ".json"), true);
                if (enrage)
                {
                    string theirs = Path.Combine(Path.GetDirectoryName(_dir.TrimEnd('\\', '/')), "enRAGE"), copies = Path.Combine(kept, "enRAGE.before-forget-you-" + stamp);
                    Directory.CreateDirectory(copies);
                    foreach (string f in new[] { "people.json", "world.json", "nemeses.json" })
                        if (File.Exists(Path.Combine(theirs, f)))
                            File.Copy(Path.Combine(theirs, f), Path.Combine(copies, f), true);
                }
            }
            catch (Exception ex)
            {
                return "Couldn't keep copies of the saves first, so nothing was forgotten: " + ex.Message;
            }
            LetGoOfEveryone("you were forgotten");
            ForgetCounts c = Forget.AboutPlayer(_store);
            _greetedAt.Clear();   // the names over heads say Stranger again
            Save();
            LoadStore();          // the engine and the minds from what is kept now, as on a load
            Save();
            string theirsSaid = enrage ? _bridge.ForgetYou() : null;
            Log.Write("FORGET you (" + how + "): " + c.Said() + (enrage ? "; enRAGE: " + (theirsSaid ?? "couldn't (see above)") : "; enRAGE isn't installed: RealNPCs' part only") +
                      "; the copies are in RealNPCs\\backup (" + stamp + ")");
            return "Everyone has forgotten you: " + c.People + " people" + (enrage ? (theirsSaid != null ? ", and enRAGE's " + theirsSaid : "; enRAGE's townsfolk couldn't be reached (see the log)") : "") +
                   ". The town and its people are as they were. Copies of the saves are in RealNPCs\\backup.";
        }

        // ---- the clerk's list ----------------------------------------------------------------------------------

        private void ForgetPromptStep()
        {
            if (_aForgetPrompt == null)
                _aForgetPrompt = ForgetPromptTick;
            if (_cfg.ResetAtClerk || _forgetPrompt != 0)
                Safe("reset prompt", _aForgetPrompt);
        }

        private unsafe void ForgetPromptTick()
        {
            int target = 0;
            if (_cfg.ResetAtClerk && Game.GameTime >= _forgetNotBefore && !MISC.GET_MISSION_FLAG() && !PLAYER.IS_PLAYER_FREE_AIMING(Game.Player.Handle))
                PLAYER.GET_PLAYER_INTERACTION_TARGET_ENTITY(Game.Player.Handle, &target, false, false);
            if (target == 0 || !ENTITY.DOES_ENTITY_EXIST(target) || !ENTITY.IS_ENTITY_A_PED(target) || ENTITY.IS_ENTITY_DEAD(target) || !StationClerk(new Ped(target)))
            {
                ForgetPromptEnd();
                return;
            }
            int group = HUD._UI_PROMPT_GET_GROUP_ID_FOR_TARGET_ENTITY(target);
            if (_forgetPrompt == 0 || !HUD._UI_PROMPT_IS_VALID(_forgetPrompt) || _forgetPromptFor != target || _forgetPromptGroup != group)
            {
                ForgetPromptEnd();
                _forgetPrompt = NewForgetPrompt(group);
                _forgetPromptFor = target;
                _forgetPromptGroup = group;
                if (!_forgetPromptNoted)
                {
                    _forgetPromptNoted = true;   // once a session: if it never shows in game, the clerk's list isn't this group
                    Log.Write("RESET prompt in the clerk's list (" + Natives.ModelName(new Ped(target)) + ", group " + group + "): hold Q");
                }
            }
            if (HUD._UI_PROMPT_HAS_HOLD_MODE_COMPLETED(_forgetPrompt))
            {
                ForgetPromptEnd();
                _forgetNotBefore = Game.GameTime + 5000;   // not offered again straight away
                Feed.Notice("RealNPCs " + _version, ForgetYou("the station clerk"));
            }
        }

        /// "Reset RealNPCs Memory" on Q, held two seconds, in this target's list.
        private static int NewForgetPrompt(int group)
        {
            if (_literalTag == IntPtr.Zero)
                _literalTag = Kept("LITERAL_STRING");
            if (_forgetWord == IntPtr.Zero)
                _forgetWord = Kept("Reset RealNPCs Memory");
            int prompt = HUD._UI_PROMPT_REGISTER_BEGIN();
            HUD._UI_PROMPT_SET_CONTROL_ACTION(prompt, ForgetControl);
            ulong word = Function.Call<ulong>(VarText, 10, new InputArgument((ulong)_literalTag.ToInt64()), new InputArgument((ulong)_forgetWord.ToInt64()));
            Function.Call(SetPromptText, prompt, new InputArgument(word));
            HUD._UI_PROMPT_SET_HOLD_MODE(prompt, ForgetHoldMs);
            HUD._UI_PROMPT_SET_GROUP(prompt, group, 0);
            HUD._UI_PROMPT_REGISTER_END(prompt);
            HUD._UI_PROMPT_SET_ENABLED(prompt, true);
            HUD._UI_PROMPT_SET_VISIBLE(prompt, true);
            return prompt;
        }

        private void ForgetPromptEnd()
        {
            if (_forgetPrompt != 0)
            {
                if (HUD._UI_PROMPT_IS_VALID(_forgetPrompt))
                    HUD._UI_PROMPT_DELETE(_forgetPrompt);
                _forgetPrompt = 0;
            }
            _forgetPromptFor = 0;
            _forgetPromptGroup = 0;
        }

        /// A station agent or clerk: a counter of RealNPCs' own kind "station", or the game's station and ticket models.
        private static bool StationClerk(Ped p)
        {
            PostKind k = PostKindOf(p);
            if (k != null && k.Kind == "station")
                return true;
            string m = (Natives.ModelName(p) ?? "").ToLowerInvariant();
            return m.Contains("trainstationworker") || m.Contains("stationclerk") || m.Contains("ticketseller") || m.Contains("freightstation") || m.Contains("telegraph");
        }
    }
}
