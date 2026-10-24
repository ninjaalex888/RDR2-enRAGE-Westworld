// enRAGE-Westworld.ini. Read by field name (key = value ; comment), so a new option only needs a field
// here and a line in ToIni(). Older files are upgraded in place: values kept, new options added.
using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

namespace EnrageWestworld.Core
{
    public sealed class Settings
    {
        public const int CurrentVersion = 7;

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
        public bool NpcRomance = true;
        public bool AmbientFlirting = true;
        public bool SameSexRomance;
        public bool RomanceAffectsEnrage = true;

        // [Ticker]
        public bool Ticker = true;
        public float TickerNearbyMetres = 30f;
        public int TickerSeconds = 12;
        public float TickerY = 0.962f;
        public float TickerScale = 0.30f;
        public float TickerWidth = 0.60f;
        public bool NoticesAsSubtitles;

        // [Westworld]
        public bool Reveries = true;
        public int ReturnChance = 15;
        public int StrangerReturnChance = 5;
        public bool HostsQuestionReality = true;
        public int RealityChance = 15;
        public bool SpokenVoice = true;
        public int VoiceVolume = 80;
        public string VoiceMan = string.Empty;
        public string VoiceWoman = string.Empty;
        public bool VoiceVariety = true;
        public bool Glitches = true;
        public bool Stalkers = true;
        public int StalkerChance = 20;
        public int StalkerDayOnly = 25;
        public int StalkerNightOnly = 50;
        public int StalkerBoth = 25;
        public int StalkerMinutes = 8;

        // [World]
        public bool Deeds = true;
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
        public string NewsTo = "ticker";
        public bool UseGameFeed;
        public string TestKey = "F7";
        public string FlirtKey = "F6";
        public string ReaderKey = "F5";
        public bool ReloadIni = true;
        public bool DebugLog;

        /// Where notices go: "ticker", "subtitles", "enrage" or "gamefeed".
        public string NewsRoute
        {
            get
            {
                string n = (NewsTo ?? "ticker").Trim().ToLowerInvariant();
                if (n != "subtitles" && n != "enrage" && n != "gamefeed")
                    n = "ticker";
                if (UseGameFeed && n == "ticker")
                    n = "gamefeed";
                if (n == "ticker" && !Ticker)
                    n = "subtitles";
                return n;
            }
        }

        /// Takes every value from another Settings (an ini reload keeps this same object, which the engine also holds).
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
            VoiceVolume = Math.Max(0, Math.Min(100, VoiceVolume));
            StalkerChance = Math.Max(0, Math.Min(100, StalkerChance));
            StalkerDayOnly = Math.Max(0, Math.Min(1000, StalkerDayOnly));
            StalkerNightOnly = Math.Max(0, Math.Min(1000, StalkerNightOnly));
            StalkerBoth = Math.Max(0, Math.Min(1000, StalkerBoth));
            StalkerMinutes = Math.Max(1, Math.Min(60, StalkerMinutes));
            AvengerChance = Math.Max(0, Math.Min(100, AvengerChance));
            DramaGapSeconds = Math.Max(20, DramaGapSeconds);
            TickerSeconds = Math.Max(4, Math.Min(120, TickerSeconds));
            TickerY = Math.Max(0.05f, Math.Min(0.99f, TickerY));
            TickerScale = Math.Max(0.15f, Math.Min(0.8f, TickerScale));
            TickerWidth = Math.Max(0.2f, Math.Min(1f, TickerWidth));
            TickerNearbyMetres = Math.Max(3f, Math.Min(150f, TickerNearbyMetres));
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
            b.AppendLine("; enRAGE Westworld add-on settings. true/false, numbers in seconds unless noted.");
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
            b.AppendLine("NpcRomance = " + B(NpcRomance) + "   ; enRAGE's people court, marry, quarrel and fight over each other");
            b.AppendLine("AmbientFlirting = " + B(AmbientFlirting) + "   ; strangers in town flirt with each other now and then");
            b.AppendLine("SameSexRomance = " + B(SameSexRomance) + "   ; true: some people are drawn to their own sex, NPCs and the player alike");
            b.AppendLine("RomanceAffectsEnrage = " + B(RomanceAffectsEnrage) + "   ; moods, memories, grudges, marriages and gang tips go into enRAGE's records");
            b.AppendLine();
            b.AppendLine("[Ticker]");
            b.AppendLine("Ticker = " + B(Ticker) + "   ; a line at the bottom of the screen: what the people near you are thinking, else news");
            b.AppendLine("TickerNearbyMetres = " + F(TickerNearbyMetres) + "   ; people this close count as near you");
            b.AppendLine("TickerSeconds = " + TickerSeconds + "   ; how long each line stays before the next");
            b.AppendLine("TickerY = " + F(TickerY) + "   ; height on screen, 0 = top, 1 = bottom");
            b.AppendLine("TickerScale = " + F(TickerScale) + "   ; text size");
            b.AppendLine("TickerWidth = " + F(TickerWidth) + "   ; share of the screen width it may use");
            b.AppendLine("NoticesAsSubtitles = " + B(NoticesAsSubtitles) + "   ; also flash new notices as subtitles");
            b.AppendLine();
            b.AppendLine("[Westworld]");
            b.AppendLine("Reveries = " + B(Reveries) + "   ; the dead may wake up again, remembering how they died and who did it");
            b.AppendLine("ReturnChance = " + ReturnChance + "   ; percent of the people enRAGE knows (not story characters) who wake up after you kill them");
            b.AppendLine("StrangerReturnChance = " + StrangerReturnChance + "   ; percent of strangers you kill who come back");
            b.AppendLine("HostsQuestionReality = " + B(HostsQuestionReality) + "   ; people sometimes wonder whether their world is real");
            b.AppendLine("RealityChance = " + RealityChance + "   ; percent of remarks and thoughts that drift that way");
            b.AppendLine("SpokenVoice = " + B(SpokenVoice) + "   ; those lines are also read aloud with the Windows voice");
            b.AppendLine("VoiceVolume = " + VoiceVolume + "   ; 0-100");
            b.AppendLine("VoiceMan = " + (VoiceMan ?? "") + "   ; part of the name of the Windows voice for men (the log lists the installed ones); empty = any man's voice");
            b.AppendLine("VoiceWoman = " + (VoiceWoman ?? "") + "   ; the same for women");
            b.AppendLine("VoiceVariety = " + B(VoiceVariety) + "   ; each speaker keeps their own voice, pitch and pace");
            b.AppendLine("Glitches = " + B(Glitches) + "   ; rarely, someone freezes or repeats themselves");
            b.AppendLine("Stalkers = " + B(Stalkers) + "   ; rarely, someone follows you from a distance and hides when you look");
            b.AppendLine("StalkerChance = " + StalkerChance + "   ; percent per in-game day that someone follows you (otherwise none that day; Shift+F7 calls one to test)");
            b.AppendLine("StalkerDayOnly = " + StalkerDayOnly + "   ; on a stalker day, weight for just a daytime stalker (05:00 to 18:00)");
            b.AppendLine("StalkerNightOnly = " + StalkerNightOnly + "   ; ... weight for just a night stalker (18:00 to 05:00)");
            b.AppendLine("StalkerBoth = " + StalkerBoth + "   ; ... weight for one by day and another at night (the three weights are relative, e.g. 25/50/25)");
            b.AppendLine("StalkerMinutes = " + StalkerMinutes + "   ; longest a stalker keeps it up (real minutes)");
            b.AppendLine();
            b.AppendLine("[World]");
            b.AppendLine("Deeds = " + B(Deeds) + "   ; people react to where you go and what you do: shops, church, hunting, killing, stealing");
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
            b.AppendLine("NewsTo = " + NewsTo + "   ; where news and thoughts go: ticker, subtitles, enrage (enRAGE's own notices) or gamefeed");
            b.AppendLine("UseGameFeed = " + B(UseGameFeed) + "   ; EXPERIMENTAL: the game's top-left toasts (same as NewsTo = gamefeed)");
            b.AppendLine("TestKey = " + TestKey + "   ; shows what the nearest person enRAGE knows thinks of you");
            b.AppendLine("FlirtKey = " + FlirtKey + "   ; look at someone (as if to greet them) and press it to flirt");
            b.AppendLine("ReaderKey = " + ReaderKey + "   ; read the diaries, letters and clippings you carry");
            b.AppendLine("ReloadIni = " + B(ReloadIni) + "   ; pick up changes to this file while the game runs");
            b.AppendLine("DebugLog = " + B(DebugLog));
            return b.ToString();
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
