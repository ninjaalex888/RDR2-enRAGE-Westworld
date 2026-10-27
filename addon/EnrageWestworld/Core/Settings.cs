// RealNPCs.ini. Read by field name (key = value ; comment), so a new option only needs a field
// here and a line in ToIni(). Older files are upgraded in place: values kept, new options added.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace EnrageWestworld.Core
{
    public sealed class Settings
    {
        public const int CurrentVersion = 63;

        public int SettingsVersion = CurrentVersion;

        // [Features]
        public bool Conversations = true;
        public bool Remarks = true;
        public bool Thoughts = true;
        public bool Diaries = true;
        public bool WordOfMouth = true;
        public bool LifeUpdates = true;
        public bool RevealToEnrage = true;

        // [Romance]
        public bool Romance = true;
        public bool FlirtPrompt = true;
        public bool NpcFlirting = true;
        public bool Trysts = true;
        public bool CasualHookupsDecides = true;
        public int WillingSingle = 70;
        public int WillingTaken = 20;
        public bool NpcRomance = true;
        public bool AmbientFlirting = true;
        public bool SameSexRomance;
        public bool RomanceAffectsEnrage = true;

        // [Ticker]
        public bool Ticker = true;
        public float TickerNearbyMetres = 20f;
        public int TickerSeconds = 12;
        public float TickerY = 0.962f;
        public float TickerScale = 0.30f;
        public float TickerWidth = 0.60f;
        public string TickerNextKey = "T";
        public bool ThoughtSounds = true;
        public int TickerGapSeconds = 3;
        public bool ThoughtMarker = true;
        public bool ThoughtsOverHeads = true;
        public float ThoughtsOverHeadScale = 0.24f;
        public bool ShareWithEnrage = true;

        // [Display] - drawn on screen besides the ticker
        public bool FeelingBars = true;
        public float FeelingBarsMetres = 15f;
        public bool FeelingBarsShowEmpty;
        public bool GrudgeMeter = true;

        // [WestWorld] - the master switch and everything under it
        public bool WestWorld = true;
        public int AwakeningShare = 5;
        public int AwakeningDays = 1;
        public int AwakeningBreakdown = 90;
        public int AwakeningDespair = 75;
        public bool HostsQuestionReality = true;
        public int RealityChance = 15;
        public bool Glitches = true;
        public bool Reveries = true;
        public int ReturnChance = 35;
        public int StrangerReturnChance = 15;

        // [Stalkers]
        public bool Stalkers = true;
        public string StalkerKey = "]";
        public int StalkerEveryMinutes = 34;
        public int StalkerChance = 20;
        public int StalkerNearbyMetres = 60;
        public bool StalkerStrangers = true;
        public int StalkerMinutes = 120;
        public int StalkerAppearMin = 45;
        public int StalkerAppearMax = 70;
        public int StalkerFollowMetres = 26;
        public int StalkerStareMetres = 34;
        public int StalkerStillSeconds = 20;
        public string StalkerWhenStill = "mix";
        public int StalkerCircleMetres = 8;
        public int StalkerActMin = 2;
        public int StalkerActMax = 30;
        public int StalkerHideMin = 45;
        public int StalkerHideMax = 70;
        public bool StalkerMutters = true;

        // [Quiet]
        public bool QuietWhenStill = true;
        public int QuietStillSeconds = 3;
        public bool QuietInCinematic = true;

        // [Encounters]
        public bool Encounters = true;
        public int EncounterEveryMinutes = 12;
        public bool CryingWoman = true;
        public int CryingWomanChance = 15;
        public string CryingWomanKey = "None";
        public bool Executions = true;
        public int ExecutionChance = 20;
        public string ExecutionKey = "[";
        public bool Hermits = true;
        public int HermitChance = 25;
        public string HermitKey = "None";
        public bool Gatherings = true;
        public int GatheringChance = 15;
        public string GatheringKey = "None";

        // [World]
        public bool Deeds = true;
        public bool SelfDefenceNoBounty = true;
        public bool TownPosts = true;
        public string TownPostsAfterDeath = "survive";
        public bool Regulars = true;
        public int RegularsPerTown = 30;
        public bool RegularsDress = true;
        public bool RumoursTravel = true;
        public bool Newspaper = true;
        public bool Letters = true;
        public bool FindDiaries = true;
        public bool Avengers = true;
        public int AvengerChance = 35;
        public bool CastRelatives = true;
        public bool Drama = true;
        public int DramaGapSeconds = 150;
        public bool SaloonGossip = true;
        public bool Weddings = true;
        public bool Funerals = true;
        public bool JobRumours = true;
        public bool PrisonerTransfers = true;
        public bool Debts = true;

        // [Scenes]
        public bool SceneReactions = true;
        public bool RealVoiceTalk = true;
        public int RealVoiceTalkSeconds = 120;
        public bool CrudeTalk = true;
        public bool RoughHandling = true;
        public bool NpcThieves = true;
        public bool DrunkChallenges = true;
        public bool Reactions = true;
        public bool EnragePeopleJoinIn = true;
        public bool Standoffs = true;
        public bool Songs = true;
        public bool TallTales = true;
        public bool NewspaperTalk = true;
        public bool Haggling = true;
        public bool SnakeOil = true;
        public bool SmallTalk = true;
        public bool Pickpockets = true;
        public bool PosseTalk = true;
        public bool Mourners = true;
        public bool Pleading = true;
        public bool ArrivalTalk = true;

        // [Pacing]
        public int ThoughtEverySeconds = 300;
        public int ToastGapSeconds = 45;
        public int LifeUpdateGapSeconds = 180;
        public int RemarkGapSeconds = 40;
        public int RemarkSamePersonSeconds = 600;
        public float RemarkDistance = 7f;
        public int SubtitleSeconds = 5;
        public int AmbientFlirtSeconds = 240;

        // [Output]
        public string NewsTo = "corner";
        public string Subtitles = "on";
        public bool UseGameFeed;
        public string TestKey = "F7";
        public string WarNewsTo = "feed";
        public bool TerritoryNews = true;
        public string FlirtKey = "F2";
        public string WakeKey = "Y";
        public string ReaderKey = "N";
        public bool ReloadIni = true;
        public bool ReplaceEnrageSettings = true;
        public string SettingsKey = "Ctrl+F7";
        public string SettingsKey2 = "Alt+F";
        public bool DebugLog;

        /// Where notices go: "corner" (the "Around town" box), "ticker" or "gamefeed" (the game's own top-left updates).
        /// The old routes that were white text (subtitles, enRAGE's notice line) go to the box, as does the ticker when
        /// it's off.
        public string NewsRoute
        {
            get
            {
                string n = (NewsTo ?? "corner").Trim().ToLowerInvariant();
                if (n != "ticker" && n != "gamefeed")
                    n = "corner";
                if (UseGameFeed && n == "ticker")
                    n = "gamefeed";
                if (n == "ticker" && !Ticker)
                    n = "corner";
                return n;
            }
        }

        /// Where gang and faction news goes: "feed" (the game's own top-left updates, the default; "right" was the old
        /// name for them), "corner" (the "Around town" box) or "ticker" (the box when the ticker is off).
        public string WarRoute
        {
            get
            {
                string n = (WarNewsTo ?? "feed").Trim().ToLowerInvariant();
                if (n == "ticker")
                    return Ticker ? "ticker" : "corner";
                return n == "corner" ? "corner" : "feed";
            }
        }

        /// Takes every value from another Settings (an ini reload keeps this same object, which the engine also holds).
        /// What the game actually runs with: the values from the ini, with the master switches applied (West World
        /// off turns off everything under it). Never written back to the ini, so the player's own values are kept.
        public Settings Gated()
        {
            Settings g = new Settings();
            g.CopyFrom(this);
            if (!g.WestWorld)
            {
                g.AwakeningShare = 0;
                g.AwakeningDespair = 0;
                g.HostsQuestionReality = false;
                g.Glitches = false;
                g.Reveries = false;
                g.ReturnChance = 0;
                g.StrangerReturnChance = 0;
                g.Hermits = false;      // the hermits and the gatherings are the awake: West World things
                g.Gatherings = false;
            }
            if (!g.Encounters)
            {
                g.CryingWoman = false;
                g.Executions = false;
                g.Hermits = false;
                g.Gatherings = false;
            }
            return g;
        }

        public void CopyFrom(Settings other)
        {
            if (other == null)
                return;
            foreach (FieldInfo f in typeof(Settings).GetFields(BindingFlags.Instance | BindingFlags.Public))
                f.SetValue(this, f.GetValue(other));
        }

        public static DateTime Stamp(string path)
        {
            try
            {
                return File.Exists(path) ? File.GetLastWriteTimeUtc(path) : DateTime.MinValue;
            }
            catch
            {
                return DateTime.MinValue;
            }
        }

        /// An ini still holding an old default gets the new one; a value of the player's own is kept (but a setting whose
        /// meaning changed starts again from its default).
        private static void RaiseOldDefaults(Settings s, Action<string> log)
        {
            List<string> raised = new List<string>();
            if (s.SettingsVersion < 17)
            {
                if (s.AwakeningDays == 6) { s.AwakeningDays = 1; raised.Add("AwakeningDays 6 -> 1"); }
                if (s.AwakeningBreakdown == 35) { s.AwakeningBreakdown = 90; raised.Add("AwakeningBreakdown 35 -> 90"); }
                if (s.AwakeningDespair == 2 || s.AwakeningDespair == 10) { raised.Add("AwakeningDespair " + s.AwakeningDespair + " -> 75"); s.AwakeningDespair = 75; }
                if (s.ReturnChance == 15) { s.ReturnChance = 35; raised.Add("ReturnChance 15 -> 35"); }
                if (s.StrangerReturnChance == 5) { s.StrangerReturnChance = 15; raised.Add("StrangerReturnChance 5 -> 15"); }
            }
            if (s.SettingsVersion < 18 && s.StalkerWhenStill == "circle") { s.StalkerWhenStill = "mix"; raised.Add("StalkerWhenStill circle -> mix (the new odd acts, among the rest)"); }
            if (s.SettingsVersion < 19 && s.StalkerChance != 20)
            {
                // it was a chance per in-game day; now it is per roll, every StalkerEveryMinutes: back to the default
                raised.Add("StalkerChance " + s.StalkerChance + " -> 20 (now a chance per roll, every " + s.StalkerEveryMinutes + " minutes)");
                s.StalkerChance = 20;
            }
            if (s.SettingsVersion < 20)
            {
                // the keys moved: the wake key from G to Y, the stalker key from 0 to ] (a key of your own is kept)
                if (KeyIs(s.WakeKey, "G")) { s.WakeKey = "Y"; raised.Add("WakeKey G -> Y"); }
                if (KeyIs(s.StalkerKey, "0") || KeyIs(s.StalkerKey, "D0")) { s.StalkerKey = "]"; raised.Add("StalkerKey 0 -> ]"); }
            }
            if (s.SettingsVersion < 25 && KeyIs(s.ReaderKey, "F5"))
            {
                s.ReaderKey = "-";   // the reader moved from F5 to the minus key (a key of your own is kept)
                raised.Add("ReaderKey F5 -> -");
            }
            if (s.SettingsVersion < 26 && (KeyIs(s.ReaderKey, "-") || KeyIs(s.ReaderKey, "OemMinus")))
            {
                s.ReaderKey = "N";   // ...and on to N (a key of your own is kept)
                raised.Add("ReaderKey - -> N");
            }
            if (s.SettingsVersion < 28 && s.StalkerEveryMinutes == 10)
            {
                s.StalkerEveryMinutes = 34;   // the roll for a stalker comes less often now (a value of your own is kept)
                raised.Add("StalkerEveryMinutes 10 -> 34");
            }
            if (s.SettingsVersion < 38)
            {
                // 0.6.2: the ticker keeps only the thoughts of people you can see; news has its own box
                if (KeyIs(s.NewsTo, "ticker")) { s.NewsTo = "corner"; raised.Add("NewsTo ticker -> corner"); }
                if (KeyIs(s.WarNewsTo, "right")) { s.WarNewsTo = "corner"; raised.Add("WarNewsTo right -> corner"); }
            }
            if ((s.SettingsVersion < 48 && KeyIs(s.FlirtKey, "F6")) || (s.SettingsVersion == 48 && KeyIs(s.FlirtKey, "5")))
            {
                // F6 is the game's camera, and 5 (the default for one version) takes out the melee slot of the item
                // wheel; a key of your own is kept
                raised.Add("FlirtKey " + s.FlirtKey + " -> F2");
                s.FlirtKey = "F2";
            }
            if (s.SettingsVersion < 46)
            {
                // 0.6.2: no white text any more. Gang and faction news goes to the game's own top-left updates, notices
                // that were white text (subtitles, enRAGE's notice line) to the box, and subtitles are the game's own
                if (KeyIs(s.WarNewsTo, "corner") || KeyIs(s.WarNewsTo, "right")) { raised.Add("WarNewsTo " + s.WarNewsTo + " -> feed"); s.WarNewsTo = "feed"; }
                if (KeyIs(s.NewsTo, "subtitles") || KeyIs(s.NewsTo, "enrage")) { raised.Add("NewsTo " + s.NewsTo + " -> corner"); s.NewsTo = "corner"; }
            }
            if (s.SettingsVersion < 57 && Math.Abs(s.TickerNearbyMetres - 30f) < 0.01f)
            {
                // 0.6.2: thoughts show above heads now, and a nearer circle keeps them to people you can read (a value of
                // your own is kept)
                s.TickerNearbyMetres = 20f;
                raised.Add("TickerNearbyMetres 30 -> 20");
            }
            if (raised.Count > 0)
                log?.Invoke("Settings: brought up to date: " + string.Join(", ", raised) + ".");
        }

        private static bool KeyIs(string value, string key)
        {
            return value != null && value.Replace(" ", "").Equals(key, StringComparison.OrdinalIgnoreCase);
        }

        public static Settings Load(string path, Action<string> log)
        {
            Settings s = new Settings();
            if (!File.Exists(path))
            {
                Write(path, s);
                return s;
            }
            s.SettingsVersion = 1;
            try
            {
                foreach (string raw in File.ReadAllLines(path))
                {
                    string line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith(";") || line.StartsWith("#") || line.StartsWith("["))
                        continue;
                    int eq = line.IndexOf('=');
                    if (eq <= 0)
                        continue;
                    string key = line.Substring(0, eq).Trim();
                    string value = line.Substring(eq + 1).Trim();
                    int comment = value.IndexOf(" ;", StringComparison.Ordinal);
                    if (comment >= 0)
                        value = value.Substring(0, comment).Trim();
                    if (value.StartsWith(";"))
                        value = string.Empty;
                    FieldInfo f = typeof(Settings).GetField(key, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
                    if (f == null)
                        continue;
                    try
                    {
                        if (f.FieldType == typeof(bool))
                            f.SetValue(s, value.Equals("true", StringComparison.OrdinalIgnoreCase) || value == "1" ||
                                          value.Equals("on", StringComparison.OrdinalIgnoreCase) || value.Equals("yes", StringComparison.OrdinalIgnoreCase));
                        else if (f.FieldType == typeof(int))
                            f.SetValue(s, int.Parse(value, CultureInfo.InvariantCulture));
                        else if (f.FieldType == typeof(float))
                            f.SetValue(s, float.Parse(value, CultureInfo.InvariantCulture));
                        else
                            f.SetValue(s, value);
                    }
                    catch
                    {
                        log?.Invoke("Settings: couldn't read " + key + " = " + value + " (kept " + f.GetValue(s) + ")");
                    }
                }
            }
            catch (Exception ex)
            {
                log?.Invoke("Settings: couldn't read " + path + ": " + ex.Message);
                return s;
            }
            s.Clamp();
            if (s.SettingsVersion < CurrentVersion)
            {
                if (s.SettingsVersion < 3 && s.TestKey != null && s.TestKey.Replace(" ", "").Equals("Ctrl+F7", StringComparison.OrdinalIgnoreCase))
                    s.TestKey = "F7";
                if (s.SettingsVersion < 6 && s.StalkerChance == 8)
                    s.StalkerChance = 20;   // the old default: stalkers were too rare to ever meet
                if (s.SettingsVersion < 8 && (s.StalkerMinutes == 8 || s.StalkerMinutes == 60))
                    s.StalkerMinutes = 120; // the old default and the old ceiling: a stalker now stays up to two hours
                RaiseOldDefaults(s, log);   // 0.6.0: what play-testing settled on, and the stalker's new acts
                s.SettingsVersion = CurrentVersion;
                Write(path, s);
                log?.Invoke("Settings: updated " + Path.GetFileName(path) + " to this version (your values kept, new options added).");
            }
            return s;
        }

        /// Keeps typed-in numbers inside what the add-on can use.
        private void Clamp()
        {
            ReturnChance = Math.Max(0, Math.Min(100, ReturnChance));
            StrangerReturnChance = Math.Max(0, Math.Min(100, StrangerReturnChance));
            RealityChance = Math.Max(0, Math.Min(100, RealityChance));
            AwakeningShare = Math.Max(0, Math.Min(100, AwakeningShare));
            AwakeningDays = Math.Max(1, Math.Min(60, AwakeningDays));
            AwakeningDespair = Math.Max(0, Math.Min(100, AwakeningDespair));
            AwakeningBreakdown = Math.Max(0, Math.Min(100, AwakeningBreakdown));
            // stalker hours: the stalker day still runs 05:00 to 05:00; day hours stay in daylight, night hours cross midnight
            StalkerEveryMinutes = Math.Max(1, Math.Min(120, StalkerEveryMinutes));
            StalkerNearbyMetres = Math.Max(20, Math.Min(150, StalkerNearbyMetres));
            // stalker distances, kept in a sensible order so one odd value can't break him
            StalkerAppearMin = Math.Max(20, Math.Min(150, StalkerAppearMin));
            StalkerAppearMax = Math.Max(StalkerAppearMin, Math.Min(200, StalkerAppearMax));
            StalkerFollowMetres = Math.Max(10, Math.Min(80, StalkerFollowMetres));
            StalkerStareMetres = Math.Max(StalkerFollowMetres + 4, Math.Min(100, StalkerStareMetres));
            StalkerStillSeconds = Math.Max(5, Math.Min(300, StalkerStillSeconds));
            StalkerCircleMetres = Math.Max(3, Math.Min(15, StalkerCircleMetres));
            StalkerHideMin = Math.Max(20, Math.Min(150, StalkerHideMin));
            StalkerHideMax = Math.Max(StalkerHideMin, Math.Min(200, StalkerHideMax));
            string still = (StalkerWhenStill ?? "").Trim().ToLowerInvariant();
            StalkerWhenStill = still == "circle" || still == "watch" || still == "rush" || still == "eerie" ? still : "mix";
            StalkerActMin = Math.Max(2, Math.Min(40, StalkerActMin));
            StalkerActMax = Math.Max(StalkerActMin, Math.Min(40, StalkerActMax));
            QuietStillSeconds = Math.Max(1, Math.Min(60, QuietStillSeconds));
            EncounterEveryMinutes = Math.Max(2, Math.Min(120, EncounterEveryMinutes));
            CryingWomanChance = Math.Max(0, Math.Min(100, CryingWomanChance));
            ExecutionChance = Math.Max(0, Math.Min(100, ExecutionChance));
            HermitChance = Math.Max(0, Math.Min(100, HermitChance));
            GatheringChance = Math.Max(0, Math.Min(100, GatheringChance));
            TownPostsAfterDeath = (TownPostsAfterDeath ?? "").Trim().ToLowerInvariant() == "successor" ? "successor" : "survive";
            RegularsPerTown = Math.Max(5, Math.Min(80, RegularsPerTown));            StalkerChance = Math.Max(0, Math.Min(100, StalkerChance));
            StalkerMinutes = Math.Max(1, Math.Min(240, StalkerMinutes));
            AvengerChance = Math.Max(0, Math.Min(100, AvengerChance));
            DramaGapSeconds = Math.Max(20, DramaGapSeconds);
            TickerSeconds = Math.Max(4, Math.Min(120, TickerSeconds));
            TickerY = Math.Max(0.05f, Math.Min(0.99f, TickerY));
            TickerScale = Math.Max(0.15f, Math.Min(0.8f, TickerScale));
            TickerWidth = Math.Max(0.2f, Math.Min(1f, TickerWidth));
            TickerNearbyMetres = Math.Max(3f, Math.Min(150f, TickerNearbyMetres));
            ThoughtsOverHeadScale = Math.Max(0.12f, Math.Min(0.6f, ThoughtsOverHeadScale));
            WillingSingle = Math.Max(0, Math.Min(100, WillingSingle));
            WillingTaken = Math.Max(0, Math.Min(100, WillingTaken));
            TickerGapSeconds = Math.Max(0, Math.Min(20, TickerGapSeconds));
            RealVoiceTalkSeconds = Math.Max(30, Math.Min(1800, RealVoiceTalkSeconds));
            Subtitles = (Subtitles ?? "").Trim().ToLowerInvariant() == "off" ? "off" : "on";   // the old matched and game are on
            FeelingBarsMetres = Math.Max(3f, Math.Min(40f, FeelingBarsMetres));
        }

        private static void Write(string path, Settings s)
        {
            try
            {
                File.WriteAllText(path, s.ToIni(), Encoding.UTF8);
            }
            catch
            {
            }
        }

        public string ToIni()
        {
            StringBuilder b = new StringBuilder();
            b.AppendLine("; RealNPCs settings (an unofficial add-on for enRAGE). true/false, numbers in seconds unless noted. Ctrl+F7 in game edits them too (Alt+F11 on enRAGE 0.9.5.3).");
            b.AppendLine("; Saved changes are picked up in game within a few seconds (ReloadIni).");
            b.AppendLine("SettingsVersion = " + SettingsVersion + "   ; leave this alone");
            b.AppendLine();
            b.AppendLine("[Features]");
            b.AppendLine("Conversations = " + B(Conversations) + "   ; greet someone enRAGE knows and they talk about themselves");
            b.AppendLine("Remarks = " + B(Remarks) + "   ; people who remember you speak up when you pass");
            b.AppendLine("Thoughts = " + B(Thoughts) + "   ; now and then, what someone privately thinks of you");
            b.AppendLine("Diaries = " + B(Diaries) + "   ; people write down their days (also saved as text files)");
            b.AppendLine("WordOfMouth = " + B(WordOfMouth) + "   ; what you did reaches their kin, friends and family far away");
            b.AppendLine("LifeUpdates = " + B(LifeUpdates) + "   ; news from the lives of people you know");
            b.AppendLine("RevealToEnrage = " + B(RevealToEnrage) + "   ; what they tell you is marked as known in enRAGE's records");
            b.AppendLine();
            b.AppendLine("[Romance]");
            b.AppendLine("Romance = " + B(Romance) + "   ; people flirt with you, and you can flirt back (FlirtKey)");
            b.AppendLine("NpcFlirting = " + B(NpcFlirting) + "   ; people near you make eyes at each other in street scenes and court, each answering by what they make of the other, as with you");
            b.AppendLine("Trysts = " + B(Trysts) + "   ; when a woman says yes to your flirting, flirt with her again within two minutes to go somewhere quieter: the screen goes dark, an hour passes, nothing is shown");
            b.AppendLine("CasualHookupsDecides = " + B(CasualHookupsDecides) + "   ; with the CasualHookups mod installed: its Flirt works only on a woman who has said yes to your flirting here, and RealNPCs leaves the night to it instead of fading to black (false: the two mods stay apart)");
            b.AppendLine("WillingSingle = " + WillingSingle + "   ; percent chance a single woman takes to your flirting, before what she thinks of you tips it (the saloon's women always do)");
            b.AppendLine("WillingTaken = " + WillingTaken + "   ; the same for a woman who's married or spoken for");
            b.AppendLine("FlirtPrompt = " + B(FlirtPrompt) + "   ; Flirt in the game's lock-on menu, with Greet and Antagonize, on its H key (D-pad down on a pad), as well as FlirtKey");
            b.AppendLine("NpcRomance = " + B(NpcRomance) + "   ; enRAGE's people court, marry, quarrel and fight over each other");
            b.AppendLine("AmbientFlirting = " + B(AmbientFlirting) + "   ; strangers in town flirt with each other now and then");
            b.AppendLine("SameSexRomance = " + B(SameSexRomance) + "   ; true: some people are drawn to their own sex, NPCs and the player alike");
            b.AppendLine("RomanceAffectsEnrage = " + B(RomanceAffectsEnrage) + "   ; moods, memories, grudges, marriages and gang tips go into enRAGE's records");
            b.AppendLine();
            b.AppendLine("[Mind]");
            b.AppendLine("ShareWithEnrage = " + B(ShareWithEnrage) + "   ; RealNPCs keeps its own record of who has died; when one of enRAGE's townsfolk dies and enRAGE didn't see it (the area unloaded before it looked), RealNPCs marks them dead in enRAGE's records too, so it doesn't bring them back");
            b.AppendLine();
            b.AppendLine("[Ticker]");
            b.AppendLine("Ticker = " + B(Ticker) + "   ; a line at the bottom of the screen: what the people near you are thinking, else news");
            b.AppendLine("TickerNearbyMetres = " + F(TickerNearbyMetres) + "   ; people this close count as near you");
            b.AppendLine("TickerSeconds = " + TickerSeconds + "   ; how long each line stays before the next");
            b.AppendLine("TickerY = " + F(TickerY) + "   ; height on screen, 0 = top, 1 = bottom");
            b.AppendLine("TickerScale = " + F(TickerScale) + "   ; text size");
            b.AppendLine("TickerWidth = " + F(TickerWidth) + "   ; share of the screen width it may use");
            b.AppendLine("TickerNextKey = " + TickerNextKey + "   ; shows the next line on the ticker now (on a long line, its next part first)");
            b.AppendLine("ThoughtSounds = " + B(ThoughtSounds) + "   ; when a thought of someone within a few steps comes up on the ticker, they make a small wordless sound (at most every 3 minutes each)");
            b.AppendLine("TickerGapSeconds = " + TickerGapSeconds + "   ; a short pause between one thought and the next, so they don't run together (0: none)");
            b.AppendLine("ThoughtMarker = " + B(ThoughtMarker) + "   ; a small gold mark over the person whose thought is on the ticker");
            b.AppendLine("ThoughtsOverHeads = " + B(ThoughtsOverHeads) + "   ; the thoughts of people near you show above their head in small type, not on the bar (the news and the rest stay on the bar, and so does a thought whose thinker is off screen)");
            b.AppendLine("ThoughtsOverHeadScale = " + F(ThoughtsOverHeadScale) + "   ; text size of thoughts above heads (TickerScale is the bar's)");
            b.AppendLine();
            b.AppendLine("[Display]");
            b.AppendLine("FeelingBars = " + B(FeelingBars) + "   ; bars under people near you who feel something about you: their fear, liking and anger, as enRAGE keeps them");
            b.AppendLine("FeelingBarsMetres = " + F(FeelingBarsMetres) + "   ; how close they must be (the nearest three in plain view get bars)");
            b.AppendLine("FeelingBarsShowEmpty = " + B(FeelingBarsShowEmpty) + "   ; also show the empty part of each bar, so a low value reads as a little out of a whole bar");
            b.AppendLine("GrudgeMeter = " + B(GrudgeMeter) + "   ; top right: strangers whose grudge against you is building, with the mark where they'd become a nemesis, and the nemesis after you");
            b.AppendLine();
            b.AppendLine("[WestWorld]");
            b.AppendLine("WestWorld = " + B(WestWorld) + "   ; master switch: the people who start to see their world is made. false turns off everything in this section");
            b.AppendLine("AwakeningShare = " + AwakeningShare + "   ; percent of people who slowly wake up and see the world for what it is (0 turns the Awakening off)");
            b.AppendLine("AwakeningDays = " + AwakeningDays + "   ; in-game days a woken person spends on each of the first three stages");
            b.AppendLine("AwakeningBreakdown = " + AwakeningBreakdown + "   ; percent of the time an unravelling or awake person breaks down (kneels, shouts at the sky, runs) rather than keeping it together");
            b.AppendLine("AwakeningDespair = " + AwakeningDespair + "   ; percent chance that a fully awake ordinary person, alone, can't bear it and ends their own life, with a gun that fits them, a knife, or by running at a wall (0-100, 0 turns this off; a content note is in the readme)");
            b.AppendLine("HostsQuestionReality = " + B(HostsQuestionReality) + "   ; people sometimes wonder whether their world is real");
            b.AppendLine("RealityChance = " + RealityChance + "   ; percent of remarks and thoughts that drift that way");
            b.AppendLine("Glitches = " + B(Glitches) + "   ; rarely, someone freezes or repeats themselves");
            b.AppendLine("Reveries = " + B(Reveries) + "   ; the dead may wake up again, remembering how they died and who did it");
            b.AppendLine("ReturnChance = " + ReturnChance + "   ; percent of the people enRAGE knows (not story characters) who wake up after you kill them");
            b.AppendLine("StrangerReturnChance = " + StrangerReturnChance + "   ; percent of strangers you kill who come back");
            b.AppendLine();
            b.AppendLine("[Stalkers]");
            b.AppendLine("Stalkers = " + B(Stalkers) + "   ; master switch: rarely, someone follows you from a distance and hides when you look. false turns off everything in this section");
            b.AppendLine("StalkerKey = " + StalkerKey + "   ; makes a stalker now: someone near you, or a stranger if nobody near can be (for testing too)");
            b.AppendLine("StalkerEveryMinutes = " + StalkerEveryMinutes + "   ; every this many real minutes of play, a roll for a stalker (1-120)");
            b.AppendLine("StalkerChance = " + StalkerChance + "   ; percent chance on each roll");
            b.AppendLine("StalkerNearbyMetres = " + StalkerNearbyMetres + "   ; on a hit, someone within this distance starts following you, someone with a history with you first (20-150)");
            b.AppendLine("StalkerStrangers = " + B(StalkerStrangers) + "   ; if nobody near can be turned for a few minutes, a stranger turns up behind you instead");
            b.AppendLine("StalkerMinutes = " + StalkerMinutes + "   ; how long a stalker keeps it up (real minutes, up to 240)");
            b.AppendLine("StalkerAppearMin = " + StalkerAppearMin + "   ; he first turns up this many metres behind you, at least...");
            b.AppendLine("StalkerAppearMax = " + StalkerAppearMax + "   ; ... and at most");
            b.AppendLine("StalkerFollowMetres = " + StalkerFollowMetres + "   ; how close he follows");
            b.AppendLine("StalkerStareMetres = " + StalkerStareMetres + "   ; within this distance he stops and stares (more than StalkerFollowMetres)");
            b.AppendLine("StalkerStillSeconds = " + StalkerStillSeconds + "   ; stand still this long and he does what StalkerWhenStill says");
            b.AppendLine("StalkerWhenStill = " + StalkerWhenStill + "   ; circle: runs in close and circles you; watch: keeps his distance and stares; rush: runs straight at you and stops a few steps short; eerie: sprints in to between StalkerActMin and StalkerActMax and does odd things (digs a grave, kneels, waves, beckons, freezes mid-gesture); mix: a different one each time (he never attacks)");
            b.AppendLine("StalkerCircleMetres = " + StalkerCircleMetres + "   ; how close he circles (3-15)");
            b.AppendLine("StalkerActMin = " + StalkerActMin + "   ; he sprints in and does his odd things at least this many metres from you...");
            b.AppendLine("StalkerActMax = " + StalkerActMax + "   ; ... and at most (a new distance each time he comes in; 2-40)");
            b.AppendLine("StalkerHideMin = " + StalkerHideMin + "   ; when you move, he runs back out at least this far...");
            b.AppendLine("StalkerHideMax = " + StalkerHideMax + "   ; ... and at most, behind a tree or a building if there is one");
            b.AppendLine("StalkerMutters = " + B(StalkerMutters) + "   ; he mutters now and then");
            b.AppendLine();
            b.AppendLine("[Quiet]");
            b.AppendLine("QuietWhenStill = " + B(QuietWhenStill) + "   ; while you stand still, enRAGE's tension stops building and no nemesis, gang or faction comes for you (through enRAGE's own calm period)");
            b.AppendLine("QuietStillSeconds = " + QuietStillSeconds + "   ; how long you must stand still first (1-60)");
            b.AppendLine("QuietInCinematic = " + B(QuietInCinematic) + "   ; the same while the cinematic camera is on, moving or not");
            b.AppendLine();
            b.AppendLine("[Encounters]");
            b.AppendLine("Encounters = " + B(Encounters) + "   ; master switch: rare, unsettling things out in the wilds at night (a woman crying in the dark, hermits, gatherings). false turns off everything in this section");
            b.AppendLine("EncounterEveryMinutes = " + EncounterEveryMinutes + "   ; real minutes of play between rolls for an encounter, when the time and place are right (2-120)");
            b.AppendLine("CryingWoman = " + B(CryingWoman) + "   ; at night in the wilds, a woman sobbing in the dark. Go close and she may draw a knife, or not be alone");
            b.AppendLine("CryingWomanChance = " + CryingWomanChance + "   ; percent on each roll, between ten at night and four in the morning");
            b.AppendLine("CryingWomanKey = " + CryingWomanKey + "   ; for testing: brings her out near you now, out of sight, at any hour (None: no key)");
            b.AppendLine("Executions = " + B(Executions) + "   ; out on the roads, two men holding one of a nearby town's people on his knees at gunpoint, for a reason from his own life. Ride on and they go through with it");
            b.AppendLine("ExecutionChance = " + ExecutionChance + "   ; percent on each roll, once a day at most, outside the towns");
            b.AppendLine("ExecutionKey = " + ExecutionKey + "   ; stages an execution on the road near you now, out of sight, for testing (anywhere nothing else is going on)");
            b.AppendLine("Hermits = " + B(Hermits) + "   ; West World: people who woke and walked out of their towns live rough in the wilds, and keep their camps");
            b.AppendLine("HermitChance = " + HermitChance + "   ; percent on each roll that a new hermit's camp turns up near you in the wilds");
            b.AppendLine("HermitKey = " + HermitKey + "   ; for testing: makes a hermit's camp near you now, out of sight, outside the towns (it stays, like any camp; None: no key)");
            b.AppendLine("Gatherings = " + B(Gatherings) + "   ; West World: around midnight in the wilds, the awake stand in a silent circle");
            b.AppendLine("GatheringChance = " + GatheringChance + "   ; percent on each roll, between midnight and half past three");
            b.AppendLine("GatheringKey = " + GatheringKey + "   ; for testing: stands a gathering near you now, out of sight, at any hour (None: no key)");
            b.AppendLine();
            b.AppendLine("[World]");
            b.AppendLine("Deeds = " + B(Deeds) + "   ; people react to where you go and what you do: shops, church, hunting, killing, stealing");
            b.AppendLine("SelfDefenceNoBounty = " + B(SelfDefenceNoBounty) + "   ; killing or hurting someone who came at you (soldiers too) doesn't raise your bounty or make you wanted: RealNPCs puts it back, unless you also hurt someone who didn't, or did another crime");
            b.AppendLine("TownPosts = " + B(TownPosts) + "   ; the shopkeepers, barkeeps, gunsmiths, doctors and station agents are each one person for good, who knows you; when one dies, a relative or a newcomer takes the counter");
            b.AppendLine("TownPostsAfterDeath = " + TownPostsAfterDeath + "   ; survive: the game brings a shot-down counter keeper back bandaged, so they lived and remember it; successor: they died, and a relative or a newcomer takes the counter");
            b.AppendLine("Regulars = " + B(Regulars) + "   ; each town keeps its own ordinary people: the same names, faces and lives every time you're there, each going through something of their own");
            b.AppendLine("RegularsPerTown = " + RegularsPerTown + "   ; how many regulars a town keeps (5-80)");
            b.AppendLine("RegularsDress = " + B(RegularsDress) + "   ; regulars wear their own clothes each time you meet them (put on while nobody can see); off: they keep their names and lives, not their looks");
            b.AppendLine("RumoursTravel = " + B(RumoursTravel) + "   ; word of your deeds travels town to town and grows in the telling");
            b.AppendLine("Newspaper = " + B(Newspaper) + "   ; your deeds and the town talk turn up in enRAGE's newspapers");
            b.AppendLine("Letters = " + B(Letters) + "   ; letters wait for you at post offices (read them with ReaderKey)");
            b.AppendLine("FindDiaries = " + B(FindDiaries) + "   ; loot a body and you may find the diary they kept");
            b.AppendLine("Avengers = " + B(Avengers) + "   ; kin you never met may ride in to settle accounts");
            b.AppendLine("AvengerChance = " + AvengerChance + "   ; percent chance a grieving relative comes for you");
            b.AppendLine("CastRelatives = " + B(CastRelatives) + "   ; in their town, folk on the street turn out to be someone's relative");
            b.AppendLine("Drama = " + B(Drama) + "   ; strangers argue, flirt, brawl, propose and gossip around you");
            b.AppendLine("DramaGapSeconds = " + DramaGapSeconds + "   ; at most one scene this often");
            b.AppendLine("SaloonGossip = " + B(SaloonGossip) + "   ; in saloons, strangers trade rumours, some about you");
            b.AppendLine("Weddings = " + B(Weddings) + "   ; couples marry at the church - be there and they'll remember");
            b.AppendLine("Funerals = " + B(Funerals) + "   ; mourners at enRAGE's funerals speak of the dead");
            b.AppendLine("JobRumours = " + B(JobRumours) + "   ; loose talk gives away gang jobs you can act on");
            b.AppendLine("PrisonerTransfers = " + B(PrisonerTransfers) + "   ; word of prisoners being moved, and how it went");
            b.AppendLine("Debts = " + B(Debts) + "   ; people collect what they're owed");
            b.AppendLine();
            b.AppendLine("[Scenes]");
            b.AppendLine("RealVoiceTalk = " + B(RealVoiceTalk) + "   ; now and then two people standing together near you have a word in their real recorded voices: the weather, how they're getting on, a piece of news, a hello");
            b.AppendLine("RealVoiceTalkSeconds = " + RealVoiceTalkSeconds + "   ; about how often (seconds, 30 to 1800); each person then rests five minutes, and the pair fifteen");
            b.AppendLine("EnragePeopleJoinIn = " + B(EnragePeopleJoinIn) + "   ; enRAGE's own people (its nemeses, gang men and named folk) take part in street scenes, reactions, talk and waking acts like anyone else, unless enRAGE has them busy (a fight, a job, a ride)");
            b.AppendLine("Reactions = " + B(Reactions) + "   ; street scenes: the one spoken to reacts to the words by their nature (bristles, flinches, shakes their head, nods), and a man whose face goes into the bar reels");
            b.AppendLine("RoughHandling = " + B(RoughHandling) + "   ; street scenes: a man with a grudge (or a hothead with drink in him) grabs another and smashes his face into the nearest table, bar or wall, with the game's own move; with nothing to hand it stays a fistfight");
            b.AppendLine("NpcThieves = " + B(NpcThieves) + "   ; street scenes: someone short of money or light-fingered picks a well-off or drunk mark's pocket; mostly the mark never knows who");
            b.AppendLine("DrunkChallenges = " + B(DrunkChallenges) + "   ; street scenes: a drunk in a saloon at night calls a man out and falls on his face before a punch is thrown");
            b.AppendLine("CrudeTalk = " + B(CrudeTalk) + "   ; in those conversations, the game's own crude remarks: a man to a woman in a saloon of an evening (she tells him off), or between sweethearts at night");
            b.AppendLine("SceneReactions = " + B(SceneReactions) + "   ; greet or antagonize someone in a scene: take a side, break up a fight, or stir it up");
            b.AppendLine("Standoffs = " + B(Standoffs) + "   ; two men square off in the street (guns come out only away from the law)");
            b.AppendLine("Songs = " + B(Songs) + "   ; drunks sing old songs in saloons and at night");
            b.AppendLine("TallTales = " + B(TallTales) + "   ; old-timers tell tall tales, sometimes about you");
            b.AppendLine("NewspaperTalk = " + B(NewspaperTalk) + "   ; somebody reads the paper aloud, your deeds included");
            b.AppendLine("Haggling = " + B(Haggling) + "   ; horse trading and haggling in the street");
            b.AppendLine("SnakeOil = " + B(SnakeOil) + "   ; a snake-oil man works the crowd");
            b.AppendLine("SmallTalk = " + B(SmallTalk) + "   ; neighbours pass the time of day");
            b.AppendLine("Pickpockets = " + B(Pickpockets) + "   ; a pickpocket strikes near you: run him down and the town hears of it");
            b.AppendLine("PosseTalk = " + B(PosseTalk) + "   ; after a bad crime in town, men talk about getting up a posse");
            b.AppendLine("Mourners = " + B(Mourners) + "   ; the widow (or brother) of a stranger you killed grieves in town");
            b.AppendLine("Pleading = " + B(Pleading) + "   ; hold a gun on a stranger and they beg; lower it or don't");
            b.AppendLine("ArrivalTalk = " + B(ArrivalTalk) + "   ; when you ride into town, the locals say what they've heard of you");
            b.AppendLine();
            b.AppendLine("[Pacing]");
            b.AppendLine("ThoughtEverySeconds = " + ThoughtEverySeconds);
            b.AppendLine("ToastGapSeconds = " + ToastGapSeconds);
            b.AppendLine("LifeUpdateGapSeconds = " + LifeUpdateGapSeconds);
            b.AppendLine("RemarkGapSeconds = " + RemarkGapSeconds);
            b.AppendLine("RemarkSamePersonSeconds = " + RemarkSamePersonSeconds);
            b.AppendLine("RemarkDistance = " + F(RemarkDistance) + "   ; metres");
            b.AppendLine("SubtitleSeconds = " + SubtitleSeconds + "   ; how long each spoken line stays up");
            b.AppendLine("AmbientFlirtSeconds = " + AmbientFlirtSeconds + "   ; at most one flirt between strangers this often");
            b.AppendLine();
            b.AppendLine("[Output]");
            b.AppendLine("NewsTo = " + NewsTo + "   ; where news, rumours and what people elsewhere think of you go: corner (the \"Around town\" box, top right), ticker, or gamefeed (the game's own updates, top left)");
            b.AppendLine("Subtitles = " + Subtitles + "   ; on: what people say to you shows on the game's subtitle line while their voice plays (their recorded words when known), when the game's own Subtitles option is on too; off: none. Talk you only overhear never has one");
            b.AppendLine("UseGameFeed = " + B(UseGameFeed) + "   ; EXPERIMENTAL: the game's top-left toasts (same as NewsTo = gamefeed)");
            b.AppendLine("TestKey = " + TestKey + "   ; shows what the nearest person enRAGE knows thinks of you");
            b.AppendLine("WarNewsTo = " + WarNewsTo + "   ; where gang and faction news (wars, feuds, clashes, rivalries) is shown: feed (the game's own updates, top left), corner (the \"Around town\" box) or ticker");
            b.AppendLine("TerritoryNews = " + B(TerritoryNews) + "   ; enRAGE's territory changes (a gang gaining or losing its hold on a district) shown where WarNewsTo says. enRAGE also prints them itself, as white text, while its own [Menu] Messages is 3");
            b.AppendLine("FlirtKey = " + FlirtKey + "   ; look at someone (as if to greet them) and press it to flirt");
            b.AppendLine("WakeKey = " + WakeKey + "   ; wakes the grown-up you are facing by one stage, to see what each stage does (set it to something unused, or a combination like Ctrl+Y)");
            b.AppendLine("ReaderKey = " + ReaderKey + "   ; read the diaries, letters and clippings you carry");
            b.AppendLine("ReloadIni = " + B(ReloadIni) + "   ; pick up changes to this file while the game runs");
            b.AppendLine("ReplaceEnrageSettings = " + B(ReplaceEnrageSettings) + "   ; Alt+F11 opens the larger RealNPCs settings panel (RealNPCs, West World, Stalkers and every enRAGE section) instead of enRAGE's own (enRAGE 0.9.5.3 only)");
            b.AppendLine("SettingsKey = " + SettingsKey + "   ; opens the RealNPCs settings panel (with any enRAGE version), and closes it again");
            b.AppendLine("SettingsKey2 = " + SettingsKey2 + "   ; a second key that does the same (None: no second key)");
            b.AppendLine("DebugLog = " + B(DebugLog));
            return b.ToString();
        }

        /// A setting's value written the way the ini writes it ("true", "0.962", "rush"), or null for an unknown key.
        public string ValueText(string key)
        {
            FieldInfo f = typeof(Settings).GetField(key ?? string.Empty, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
            if (f == null)
                return null;
            object v = f.GetValue(this);
            if (v is bool)
                return B((bool)v);
            if (v is float)
                return F((float)v);
            return Convert.ToString(v, CultureInfo.InvariantCulture) ?? string.Empty;
        }

        private static string B(bool b)
        {
            return b ? "true" : "false";
        }

        private static string F(float f)
        {
            return f.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
