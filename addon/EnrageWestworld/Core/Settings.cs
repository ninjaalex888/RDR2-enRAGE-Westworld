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
        public const int CurrentVersion = 126;

        public int SettingsVersion = CurrentVersion;

        // [Features]
        public bool Conversations = true;
        public bool PanelShowsAll;   // the settings panel shows every setting and the Testing page; off: the essentials (Lex, 2026-10-30; settings version 123)
        public bool ResetAtClerk = true;   // "Reset RealNPCs Memory" in a station clerk's list (Lex, 2026-10-30; settings version 121)
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
        public bool TickerShowsScene = true;   // the bar names the street scene playing out near you, while it plays (Lex, 2026-10-30; settings version 122)
        public bool ScenesOnTicker = true;        // a street scene's lines on the ticker while it plays within ScenesOnTickerMetres (2026-10-29, Lex)
        public float ScenesOnTickerMetres = 12f;   // was 30 before settings version 120 (Lex, 2026-10-30: a mourner's words subtitled from 26 m away)
        public bool ShareWithEnrage = true;
        public bool Minds = true;
        public bool RecordMinds = true;
        public bool RecordFates = true;
        public bool MindShadow = true;    // the NPC mind (daytime 2026-10-27): works out traits, needs, feelings and mood and only reports them
        public bool MindLive = true;      // what the mind decides reaches the screen (Lex, 2026-10-30: on for everyone from 0.7.2; off until settings version 126)
        public bool MindLevels = false;   // the shadow's scan run from the levels scheduler (night 2026-10-28, item 7; settings version 89): a near ring in view, the town around every 10-30 s, the people away one time a game day, a budget of work a frame; off, the scan as it was
        public bool QuarrelLadder = true;  // with MindLive: an argue or a brawl between two people the mind knows is played from the quarrel ladder (Core/Escalation.cs, Scenes.Ladder.cs; night 2026-10-28, item 16; settings version 92): how far it goes and how it ends from their traits and the surroundings; the shot is never played
        public bool ThoughtVariety = true;   // no two people near you think the same frame within an hour, and a source that gave the last few thoughts gives way to the others (Lex, 2026-10-29; settings version 93; Engine.Minds.cs)
        public bool MindStandUp = true;      // with MindLive: who steps into a rough or a brawl between two others comes from the onlookers' own minds, and whether the bully backs down from his (Core/StepIn.cs; roadmap 39; night 2026-10-29, item 2; settings version 107); off, the old roll
        public bool MindOnlookers = true;    // with MindLive: someone who only watched a quarrel, a fight or something you did acts on what they feel about it (walks off from it, stands rooted at a gun, shakes their head, bristles, turns to watch), two a happening at most (Core/Onlooking.cs; roadmap 39; night 2026-10-29, item 3; settings version 108); off, they only turn their heads
        public bool MindWakes = true;        // with MindLive and West World: what someone has lived through (the shadow's doubts about "things are as they seem") leans the Awakening, a buried man seen walking breaks that belief, and the moment it gives way they stir (Core/Engine.Doubt.cs; the mind doc's step 10; night 2026-10-29, item 4; settings version 109); off, the Awakening by its own roll alone
        public bool MindPassing = true;      // with MindLive: a greeting in passing follows what the first one feels about the other lately and their mood (Core/Passing.Mind.cs; roadmap 39; night 2026-10-29, item 6; settings version 111); and how someone shows you what they feel about you from a while back as you pass (Core/Passing.ToYou.cs, item 6f); off, the tie's greeting
        public bool MindHelps = true;        // with MindLive: who helps someone up after a fall, and how, comes from what those who saw it feel (Scenes.FallAnswerOf; roadmap 39; night 2026-10-29, item 6b; settings version 112); off, the tie and a roll
        public bool MindRescue = true;       // with MindLive: when someone awake begins their last words, someone near whom it moves (a friend, a kind stranger) may go to them and talk them down (Core/Rescue.cs; roadmap 3's other side; night 2026-10-29, item 6d; settings version 113); off, nobody steps in
        public int RescueChance = 60;        // with MindRescue: of the despairs where someone near is moved to go to them, this share in percent where they do (the rest hesitate too long); seeded (settings version 114)
        public bool MindMeetings = true;     // with MindLive: the street's chance meetings lean by the two's moods today and how much they want company (Core/Reasons.Mood.cs; roadmaps 18 and 30; night 2026-10-29, item 6e; settings version 115); off, by the reasons alone
        public bool Language = false;        // the language layer (2026-10-29; roadmap 15): with MindLive, the mind's own thoughts worded by a local model (Ollama at LanguageUrl, LanguageModel) through Language.Check, the written line always the fallback; off until the generated thoughts pass the checks at scale (settings version 94)
        public string LanguageUrl = "http://127.0.0.1:11434";   // the local server; nothing leaves the machine
        public string LanguageModel = "llama3.1:8b";           // the model Ollama holds (ollama pull <name>)

        // [Display] - drawn on screen besides the ticker
        public bool FeelingBars = true;
        public float FeelingBarsMetres = 15f;
        public bool FeelingBarsShowEmpty;
        public string NameCards = "known";        // off | known | everyone: names over heads, enRAGE's way (2026-10-29)
        public float NameCardsMetres = 30f;       // strangers' cards within
        public float NameCardsKnownMetres = 75f;  // people you know within
        public bool NameCardsBesideEnrage;        // ours beside enRAGE's own cards too (else left to enRAGE while its are on)
        public bool NameCardsStrangerNames;      // a stranger's own name over "Stranger"; off: "Stranger" alone (Lex, 2026-10-30; settings version 124)
        public bool NameCardsSecondLine = true;   // the line under the name (temper, trade, Stranger, gang); off: the name alone (players, 2026-10-30; settings version 117)
        public bool WhosWho = true;               // for everyone (Lex, 2026-10-30; MindLive only for its Lately): the reader (the papers key) ends with a page of the people you've met, read from what is kept (Core/WhosWho.cs; roadmap 24; night 2026-10-29; settings version 116)
        public bool GrudgeMeter = true;

        // [WestWorld] - the master switch and everything under it
        public bool WestWorld = true;
        public int AwakeningShare = 5;
        public int AwakeningDays = 1;
        public int AwakeningBreakdown = 90;
        public int AwakeningDespair = 75;
        public bool WakeVoices = true;   // the waking speak the game's own lines, oddly, more each stage (Core/WakeVoice.cs; settings version 78)
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
        public bool StalkerRecords = true;
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
        public int EncountersAtOnce = 5;   // the encounters and a stalker going on at once (Lex, 2026-10-27; it was one; settings version 79)
        public bool CryingWoman = true;
        public int CryingWomanChance = 15;
        public bool NightVisitor = true;
        public int NightVisitorChance = 18;
        public bool Travellers = true;
        public int TravellerChance = 20;
        public bool Campfires = true;     // [Encounters] strangers at a campfire at night in the wilds (night 2026-10-28, item 14; settings version 90)
        public int CampfireChance = 15;
        public bool VoiceBelow = true;    // [Encounters] West World: late at night in a saloon, a muffled plea from under the boards and the barkeep's thoughts (night 2026-10-28, item 14; settings version 90)
        public bool SupperInvitation;   // the supper invitation, the player version (Lex, 2026-10-27 18:33; settings version 81); off until it's been tested (Lex, the same evening; version 82)
        public int SupperChance = 12;
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
        public int AvengersAtOnce = 2;      // riding, here or still alive on enRAGE's roster (Lex's players, 2026-10-28)
        public int AvengerGapHours = 48;    // game hours between one avenger's arrival and the next's
        public bool CastRelatives = true;
        public bool CastRelativesSpawn = true;   // when nobody free is on the street (enRAGE has named most walkers in a well-played town), the relative is made out of sight behind you and let go to the game after (Lex, 2026-10-29; settings version 95)
        public bool OwnLooks = false;   // [World] standing alone: an ordinary person is known again by the look of the body they were met in (the model and every piece they wear), so the same townsman met again is the same man (roadmap 34, phase A; night 2026-10-29, item 5; settings version 110); off as it comes until seen in game; with enRAGE running, its own knowing by look is used and this never
        public bool Drama = true;
        public int DramaGapSeconds = 60;   // 150 until settings version 87, 90 until 101 (Lex, 2026-10-28 and 10-29: the world lively)
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
        public float TroubleWeight = 1f;   // [Scenes] every chance of a fight or a quarrel between people scaled by it (Lex, 2026-10-28; settings version 88)
        public bool WomenInFights = true;   // [Scenes] women may come to blows too, at half a man's chance for each woman in it (Lex, 2026-10-29: "we can allow men to trade blows with women in this mod ... 50% less likely than a man getting in a fight until we have the innermind logic take over"; night 2026-10-29, item 2b; settings version 107); false: two men only, as before
        public bool NpcThieves = true;
        public bool DrunkChallenges = true;
        public bool Reactions = true;
        public bool Acknowledge = true;
        public bool SightReactions = true;
        public bool Kindnesses = true;
        public bool Vices = true;
        public bool Grudges = true;
        public bool DoctorsPatient = true;   // [Scenes] someone goes to the doctor for what ails them, tells him, hears his word, and walks home (night 2026-10-28, item 14; widened 2026-10-29; settings version 90)
        public float ErrandPace = 2.0f;   // [Scenes] how fast people walk on errands for RealNPCs: 1 a walk, 2 brisk (was about 1-1.4), 3 a run (Lex, 2026-10-29)
        public bool LawChases = true;   // [Scenes] a lawman near runs down a pickpocket of ours and brings him down (Lex, 2026-10-29)
        public bool Lifts = true;   // [Scenes] a man bumps into you in town and lifts some of your cash; catch him to get it back (Lex, 2026-10-29)
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
        public string Subtitles = "game";   // game | ours | both | off (2026-10-28)
        public bool UseGameFeed;
        public string TestKey = "F7";
        public string WarNewsTo = "feed";
        public bool TerritoryNews = true;
        public string FlirtKey = "None";   // F2 until 2026-10-29 (Lex: H flirts in the lock-on menu, G hooks up; no keyboard key)
        public string WakeKey = "Y";
        public string ReaderKey = "N";
        public bool ReloadIni = true;
        public bool ReplaceEnrageSettings = true;
        public string SettingsKey = "Alt+R";   // Ctrl+F7 until 2026-10-29 (Lex: Alt+R first, Alt+F second)
        public string SettingsKey2 = "Alt+F";   // the second key; Alt+R from 98 to 101, Alt+F before and since 102 (Lex, 2026-10-29)
        public bool PanelSlowsTime = true;    // the world slows while the settings panel is open, as it does for the game's wheels (Lex, 2026-10-27; settings version 84)
        public float PanelTimeScale = 0f;   // 0: paused (Lex, 2026-10-30; 0.25 until settings version 125)
        // [Controller] (Lex, 2026-10-28; settings version 86): a controller's buttons for RealNPCs' own keys, a button
        // held and another pressed (Core/PadChord.cs); read only while a controller is the last thing touched
        public bool PadKeys = true;
        public string PadSettings = "RB+DPadUp";
        public string PadStatus = "RB+DPadLeft";
        public string PadLetters = "RB+DPadRight";
        public string PadTickerNext = "None";
        public bool DebugLog;
        public bool PerfMeter = true;

        // [Testing] (overnight 2026-10-27): the Testing page's keys (Core/TestActions.cs). The ones that were settings
        // already (StalkerKey, WakeKey, ExecutionKey...) stay fields; the rest are kept here by their ini name.
        public bool TestKeys = true;
        public bool ShowMinds = false;   // off as it comes (Lex, 2026-10-28; on from settings version 77 to the night of 10-28)
        public bool ShowWakeStages;   // [Testing] a tag over each person who has begun to wake (2026-10-27)
        public bool TestCastReturns = false;  // [Testing] the test cast kept in the save come back by themselves when you come to their town; off as it comes since 2026-10-29 (Lex: they should come only on the row's key; an ini that says true is set false once, version 96) (night 2026-10-28; settings version 90)
        public Dictionary<string, string> TestKeyMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// The key text a Testing action is set to: its field, its line in [Testing], or its default.
        public string TestKeyOf(TestActions.Entry e)
        {
            if (e == null)
                return "None";
            if (e.Field)
            {
                FieldInfo f = typeof(Settings).GetField(e.KeySetting, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
                string v = f != null ? f.GetValue(this) as string : null;
                return string.IsNullOrEmpty(v) ? e.DefaultKey : v;
            }
            string t;
            return TestKeyMap.TryGetValue(e.KeySetting, out t) && !string.IsNullOrEmpty(t) ? t : e.DefaultKey;
        }

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
                g.NightVisitor = false;
                g.Travellers = false;
                g.SupperInvitation = false;
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
                raised.Add("FlirtKey " + s.FlirtKey + " -> None");
                s.FlirtKey = "None";   // F2 until 2026-10-29: flirting is H in the lock-on menu now
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
            if (s.SettingsVersion < 83)
            {
                // Show minds has a key of its own since version 83 (Lex, 2026-10-27: on as it comes, with a key to turn
                // it off and on); an ini's "None" was the old default, a key of your own is kept
                string k;
                if (!s.TestKeyMap.TryGetValue("TestShowMindsKey", out k) || string.IsNullOrEmpty(k) || KeyIs(k, "None"))
                {
                    s.TestKeyMap["TestShowMindsKey"] = "Ctrl+F2";
                    raised.Add("TestShowMindsKey None -> Ctrl+F2");
                }
            }
            if (s.SettingsVersion < 87 && s.DramaGapSeconds == 150)
            {
                // the old default (Lex, 2026-10-28: scenes between people fairly often); a gap of your own is kept
                s.DramaGapSeconds = 90;
                raised.Add("DramaGapSeconds 150 -> 90");
            }
            if (s.SettingsVersion < 126 && !s.MindLive)
            {
                s.MindLive = true;   // 0.7.2 (Lex, 2026-10-30): the mind acts for everyone; it was off while it was tested
                raised.Add("MindLive false -> true (on for everyone from 0.7.2)");
            }
            if (s.SettingsVersion < 125 && Math.Abs(s.PanelTimeScale - 0.25f) < 0.001f)
            {
                s.PanelTimeScale = 0f;   // 2026-10-30 (Lex): the panel pauses the world; a speed of your own is kept
                raised.Add("PanelTimeScale 0.25 -> 0 (paused)");
            }
            if (s.SettingsVersion < 120 && Math.Abs(s.ScenesOnTickerMetres - 30f) < 0.01f)
            {
                s.ScenesOnTickerMetres = 12f;   // 2026-10-30 (Lex): the subtitles only for people near you; a distance of your own is kept
                raised.Add("ScenesOnTickerMetres 30 -> 12");
            }
            if (s.SettingsVersion < 101 && s.DramaGapSeconds == 90)
            {
                s.DramaGapSeconds = 60;   // 2026-10-29 (Lex): "these NPC interactions happening pretty frequently to make the world feel lively"
                raised.Add("DramaGapSeconds 90 -> 60");
            }
            if (s.SettingsVersion == 81 && s.SupperInvitation)
            {
                // version 81 had the supper invitation on as it came, for an hour; off until it's been tested (Lex,
                // 2026-10-27), so an 81 ini's "on" was that default, not a choice
                s.SupperInvitation = false;
                raised.Add("SupperInvitation on -> off (until it's been tested)");
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
            var testSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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
                    if (TestActions.ByKey(key) != null)
                        testSeen.Add(key);
                    FieldInfo f = typeof(Settings).GetField(key, BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.Public);
                    if (f == null)
                    {
                        if (TestActions.ByKey(key) != null)
                            s.TestKeyMap[key] = value;   // one of the Testing page's keys (Core/TestActions.cs)
                        continue;
                    }
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
            try
            {
                s.Clamp();   // a value Clamp can't take (night 2026-10-28: the controller's "+") must not escape the script's constructor
            }
            catch (Exception ex)
            {
                log?.Invoke("Settings: couldn't clamp " + path + ": " + ex.Message);
            }
            bool testMissing = false;
            foreach (TestActions.Entry te in TestActions.All)
                testMissing |= !testSeen.Contains(te.KeySetting);
            if (testMissing && s.SettingsVersion >= CurrentVersion)
                s.SettingsVersion = CurrentVersion - 1;   // a Testing key missing from the file: written again with all of them (values kept)
            if (s.SettingsVersion < CurrentVersion)
            {
                if (s.SettingsVersion < 3 && s.TestKey != null && s.TestKey.Replace(" ", "").Equals("Ctrl+F7", StringComparison.OrdinalIgnoreCase))
                    s.TestKey = "F7";
                if (s.SettingsVersion < 6 && s.StalkerChance == 8)
                    s.StalkerChance = 20;   // the old default: stalkers were too rare to ever meet
                if (s.SettingsVersion < 8 && (s.StalkerMinutes == 8 || s.StalkerMinutes == 60))
                    s.StalkerMinutes = 120; // the old default and the old ceiling: a stalker now stays up to two hours
                if (s.SettingsVersion < 96 && s.TestCastReturns)
                {
                    s.TestCastReturns = false;   // 2026-10-29 (Lex): the test cast come only on the row's key, not by themselves at every visit
                    log?.Invoke("Settings: TestCastReturns set off (the test cast come only on the Testing row's key now); set it on again if you want them back by themselves).");
                }
                if (s.SettingsVersion < 102)
                {
                    // 2026-10-29 (Lex): Alt+R opens the settings and Alt+F is the second key; Ctrl+F7 goes. An ini on any of the old
                    // defaults (Ctrl+F7 and Alt+F, or the afternoon's Ctrl+F7 and Alt+R) is moved; a key of the player's own is kept.
                    string k1 = s.SettingsKey, k2 = s.SettingsKey2;
                    bool old1 = KeyIs(k1, "Ctrl+F7") || KeyIs(k1, "Alt+F") || KeyIs(k1, "Alt+R");
                    bool old2 = KeyIs(k2, "Ctrl+F7") || KeyIs(k2, "Alt+F") || KeyIs(k2, "Alt+R");   // an empty or None second key is the player's choice: kept
                    if (old1)
                        s.SettingsKey = "Alt+R";
                    if (old2 || KeyIs(k2, s.SettingsKey))
                        s.SettingsKey2 = KeyIs(s.SettingsKey, "Alt+F") ? "Alt+R" : "Alt+F";
                    if (old1 || old2)
                        log?.Invoke("Settings: the settings panel's keys are " + s.SettingsKey + " and " + s.SettingsKey2 + " (were " + k1 + " and " + k2 + "; Ctrl+F7 opens nothing now).");
                }
                if (s.SettingsVersion < 106 && KeyIs(s.FlirtKey, "F2"))
                {
                    s.FlirtKey = "None";   // 2026-10-29 (Lex): flirting is H in the lock-on menu, the hook up G; F2 goes
                    log?.Invoke("Settings: FlirtKey F2 -> None (flirt with H in the lock-on menu, then Hook up with G once she says yes).");
                }
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
            EncountersAtOnce = Math.Max(1, Math.Min(8, EncountersAtOnce));
            CryingWomanChance = Math.Max(0, Math.Min(100, CryingWomanChance));
            NightVisitorChance = Math.Max(0, Math.Min(100, NightVisitorChance));
            TravellerChance = Math.Max(0, Math.Min(100, TravellerChance));
            CampfireChance = Math.Max(0, Math.Min(100, CampfireChance));
            SupperChance = Math.Max(0, Math.Min(100, SupperChance));
            ExecutionChance = Math.Max(0, Math.Min(100, ExecutionChance));
            HermitChance = Math.Max(0, Math.Min(100, HermitChance));
            GatheringChance = Math.Max(0, Math.Min(100, GatheringChance));
            TownPostsAfterDeath = (TownPostsAfterDeath ?? "").Trim().ToLowerInvariant() == "successor" ? "successor" : "survive";
            RegularsPerTown = Math.Max(5, Math.Min(80, RegularsPerTown));            StalkerChance = Math.Max(0, Math.Min(100, StalkerChance));
            StalkerMinutes = Math.Max(1, Math.Min(240, StalkerMinutes));
            AvengerChance = Math.Max(0, Math.Min(100, AvengerChance));
            AvengersAtOnce = Math.Max(1, Math.Min(10, AvengersAtOnce));
            AvengerGapHours = Math.Max(0, Math.Min(720, AvengerGapHours));
            DramaGapSeconds = Math.Max(20, DramaGapSeconds);
            TroubleWeight = Math.Max(0f, Math.Min(5f, TroubleWeight));
            RescueChance = Math.Max(0, Math.Min(100, RescueChance));
            TickerSeconds = Math.Max(4, Math.Min(120, TickerSeconds));
            TickerY = Math.Max(0.05f, Math.Min(0.99f, TickerY));
            TickerScale = Math.Max(0.15f, Math.Min(0.8f, TickerScale));
            TickerWidth = Math.Max(0.2f, Math.Min(1f, TickerWidth));
            TickerNearbyMetres = Math.Max(3f, Math.Min(150f, TickerNearbyMetres));
            ThoughtsOverHeadScale = Math.Max(0.12f, Math.Min(0.6f, ThoughtsOverHeadScale));
            ScenesOnTickerMetres = Math.Max(5f, Math.Min(80f, ScenesOnTickerMetres));
            PanelTimeScale = Math.Max(0f, Math.Min(1f, PanelTimeScale));
            PadSettings = PadChord.Normal(PadSettings) ?? "RB+DPadUp";   // one that can't be read: the default (the panel is how a controller reaches everything)
            PadStatus = PadChord.Normal(PadStatus) ?? "None";
            PadLetters = PadChord.Normal(PadLetters) ?? "None";
            PadTickerNext = PadChord.Normal(PadTickerNext) ?? "None";
            if (string.IsNullOrWhiteSpace(LanguageUrl)) LanguageUrl = "http://127.0.0.1:11434";
            if (string.IsNullOrWhiteSpace(LanguageModel)) LanguageModel = "llama3.1:8b";
            LanguageUrl = LanguageUrl.Trim().TrimEnd('/');
            LanguageModel = LanguageModel.Trim();
            WillingSingle = Math.Max(0, Math.Min(100, WillingSingle));
            WillingTaken = Math.Max(0, Math.Min(100, WillingTaken));
            TickerGapSeconds = Math.Max(0, Math.Min(20, TickerGapSeconds));
            RealVoiceTalkSeconds = Math.Max(30, Math.Min(1800, RealVoiceTalkSeconds));
            // game | ours | both | off (Lex, 2026-10-28: the game's own white subtitle for a recorded line can't be stopped, so
            // one set at a time); the old on (and matched, game) read as game
            string sm = (Subtitles ?? "").Trim().ToLowerInvariant();
            Subtitles = sm == "off" || sm == "ours" || sm == "both" ? sm : "game";
            ErrandPace = Math.Max(1f, Math.Min(3f, ErrandPace));
            FeelingBarsMetres = Math.Max(3f, Math.Min(40f, FeelingBarsMetres));
            string nc = (NameCards ?? "known").Trim().ToLowerInvariant();
            NameCards = nc == "off" || nc == "everyone" ? nc : "known";
            NameCardsMetres = Math.Max(5f, Math.Min(150f, NameCardsMetres));
            NameCardsKnownMetres = Math.Max(5f, Math.Min(150f, NameCardsKnownMetres));
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
            b.AppendLine("; RealNPCs settings (an unofficial add-on for enRAGE). true/false, numbers in seconds unless noted. Alt+R in game edits them too (Alt+F as well; Alt+F11 on enRAGE 0.9.5.3).");
            b.AppendLine("; Saved changes are picked up in game within a few seconds (ReloadIni).");
            b.AppendLine("SettingsVersion = " + SettingsVersion + "   ; leave this alone");
            b.AppendLine();
            b.AppendLine("[Features]");
            b.AppendLine("ResetAtClerk = " + B(ResetAtClerk) + "   ; a station clerk's list offers Reset RealNPCs Memory (hold Q): everyone forgets what they know and feel about you, enRAGE's townsfolk too; the people and their lives stay; copies of the saves are kept");
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
            b.AppendLine("Minds = " + B(Minds) + "   ; people think about what is most on their minds: what they lived through (a fight they were in or saw, a kind word, a slight), their worry, what's around them; false: the old mix of thoughts");
            b.AppendLine("RecordMinds = " + B(RecordMinds) + "   ; enRAGE's own people think the same way: what RealNPCs saw them live through, weighed with enRAGE's own record of them (what they feel about you, its memories of you, what they've heard, their goals, their mood)");
            b.AppendLine("RecordFates = " + B(RecordFates) + "   ; when one of enRAGE's people dies of something RealNPCs did (despair, a street fight gone too far), it is written into enRAGE's record of them (their history, and their death if enRAGE missed it); it also lets despair reach them");
            b.AppendLine("ShareWithEnrage = " + B(ShareWithEnrage) + "   ; RealNPCs keeps its own record of who has died; when one of enRAGE's townsfolk dies and enRAGE didn't see it (the area unloaded before it looked), RealNPCs marks them dead in enRAGE's records too, so it doesn't bring them back");
            b.AppendLine("MindShadow = " + B(MindShadow) + "   ; the mind underneath (in testing): works out each nearby person's traits, needs, feelings and mood and writes them to the log, the F7 dump and the Show minds cards; it changes nothing they do or think");
            b.AppendLine("MindLive = " + B(MindLive) + "   ; what that mind decides reaches the screen (their moods, and thoughts of what they really feel and need, and what they do about it); each part has its own switch below");
            b.AppendLine("MindLevels = " + B(MindLevels) + "   ; (in testing) the mind's work spread by distance and a budget a frame: the people in view every look, the town around every 10-30 s, everyone saved elsewhere one time a game day (counted only); off, exactly as before");
            b.AppendLine("QuarrelLadder = " + B(QuarrelLadder) + "   ; with MindLive on: an argue or a brawl between two people the mind knows climbs or cools by the quarrel ladder (words, a shove, blows, a gun drawn; an apology, a joke, backing down, a friend stepping in, a drink, the law in sight) from their natures and the surroundings; the shot is never fired. Off: the fixed scenes");
            b.AppendLine("ThoughtVariety = " + B(ThoughtVariety) + "   ; no two people near you think the same thought-shape within an hour (the names aside), and a kind of thought that has had the last few turns gives way to the others (the weather, the place, the day), so a town doesn't read as one worried chorus; false: the old choice");
            b.AppendLine("MindStandUp = " + B(MindStandUp) + "   ; with MindLive on: who steps into a rough or a brawl between two others comes from the onlookers' own minds (what it means to them, how many are watching, what a man or a woman of 1899 may let show), and the bully backs down or goes on from his; false: the old roll");
            b.AppendLine("MindOnlookers = " + B(MindOnlookers) + "   ; with MindLive on: someone who only watched a quarrel, a fight or something you did acts on what they feel about it, by their nature and what 1899 lets them show (the frightened walk off, a man who keeps his fear off his face slips away, a gun or something uncanny roots them to the spot, contempt shakes its head, the pious walk off disapproving, a keen eye turns to watch), at most two each time; false: they only turn their heads");
            b.AppendLine("MindWakes = " + B(MindWakes) + "   ; with MindLive and West World on: the Awakening follows what people have lived through: the uncanny, a killing or a buried man seen walking shake their sense that things are as they seem, so they wake sooner, a good turn steadies them, and the moment that sense gives way they begin to stir; someone who saw a man die and sees him walk again is frightened by it and begins to wake (with MindOnlookers on, backs away from him, or, struck hard, stands rooted); false: the Awakening by its own roll alone");
            b.AppendLine("MindPassing = " + B(MindPassing) + "   ; with MindLive on: when two who know each other pass, the greeting follows how the first feels about the other lately and the day they are having: sore at a friend, a nod and no word; afraid, eyes away; ashamed before them, eyes down; grateful, a warm word; a sore day, a nod; a glad one, a word for anyone they know; and someone who feels something about you from a while back shows it as you pass (grateful, a hat tip and a word; sore, a hard look; afraid or shamed, eyes away); false: the greeting the tie gives");
            b.AppendLine("MindHelps = " + B(MindHelps) + "   ; with MindLive on: when someone takes a fall, the one most moved by it of those who saw it is the one who comes, and how they help comes from what they feel (a hand up, a gruff one, or walking past); a foe still sneers; false: the nearest, by the tie and a roll");
            b.AppendLine("MindRescue = " + B(MindRescue) + "   ; with MindLive on (West World): when someone fully awake begins to say their last words, someone near who is moved by it (a friend, or a kind stranger) may walk over and try to talk them down; a friend is heeded more often; false: nobody steps in");
            b.AppendLine("RescueChance = " + RescueChance + "   ; with MindRescue on: of the despairs where someone near is moved to step in, how often in percent they really do before it is too late (0-100); lower it to see more despair, raise it to see more people saved");
            b.AppendLine("MindMeetings = " + B(MindMeetings) + "   ; with MindLive on: what passes between two people who meet near you leans by the day they are having: two sore people come to words sooner than small talk, two glad ones the other way, and someone lonely stops for a word; false: by their reasons alone");
            b.AppendLine("Language = " + B(Language) + "   ; (in testing) with MindLive on, the mind's own thoughts are worded by a local language model running on your own PC (Ollama; nothing leaves the machine); every answer is checked for the period, the names and the length, and the written line shows whenever the model's won't do");
            b.AppendLine("LanguageUrl = " + LanguageUrl + "   ; where Ollama listens");
            b.AppendLine("LanguageModel = " + LanguageModel + "   ; the model it holds (ollama pull <name>)");
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
            b.AppendLine("ThoughtsOverHeads = " + B(ThoughtsOverHeads) + "   ; the thoughts of people near you show above their head in small type, not on the bar (the news and the rest stay on the bar; a thought whose thinker is out of view is passed over, never put on the bar)");
            b.AppendLine("ThoughtsOverHeadScale = " + F(ThoughtsOverHeadScale) + "   ; text size of thoughts above heads (TickerScale is the bar's)");
            b.AppendLine("TickerShowsScene = " + B(TickerShowsScene) + "   ; while a street scene plays out within 40 m, the bar at the bottom says what is going on and who is in it (\"Nearby: ...\")");
            b.AppendLine("ScenesOnTicker = " + B(ScenesOnTicker) + "   ; what people say near you (a street scene, a hello, the doctor, the law) shows as they say it in the subtitle box, the game's way (their name, the line); never on the bar; [Output] Subtitles = off hides it");
            b.AppendLine("ScenesOnTickerMetres = " + F(ScenesOnTickerMetres) + "   ; what people say within this many metres shows as a subtitle (5-80)");
            b.AppendLine();
            b.AppendLine("[Display]");
            b.AppendLine("FeelingBars = " + B(FeelingBars) + "   ; bars under people near you who feel something about you: their fear, fondness and anger, as enRAGE keeps them");
            b.AppendLine("FeelingBarsMetres = " + F(FeelingBarsMetres) + "   ; how close they must be (the nearest three in plain view get bars)");
            b.AppendLine("FeelingBarsShowEmpty = " + B(FeelingBarsShowEmpty) + "   ; also show the empty part of each bar, so a low value reads as a little out of a whole bar");
            b.AppendLine("NameCards = " + NameCards + "   ; names over heads in the game's title type, the way enRAGE drew them: off, known (people RealNPCs knows: enRAGE's records and townsfolk, regulars, anyone who thought or spoke or took part in a scene), everyone (the rest get what they look like, \"A townsman\", in grey). While enRAGE runs with its own cards on, ours stay off unless NameCardsBesideEnrage; off on the settings panel turns enRAGE's own cards off too");
            b.AppendLine("NameCardsMetres = " + F(NameCardsMetres) + "   ; strangers' names within (metres); they fade out near the edge");
            b.AppendLine("NameCardsKnownMetres = " + F(NameCardsKnownMetres) + "   ; the names of people you know within (metres)");
            b.AppendLine("NameCardsBesideEnrage = " + B(NameCardsBesideEnrage) + "   ; draw ours even while enRAGE draws its own (two cards over each head enRAGE knows)");
            b.AppendLine("NameCardsStrangerNames = " + B(NameCardsStrangerNames) + "   ; someone you haven't met shows their name with Stranger under it; false: Stranger alone, until you've greeted them or dealt with them");
            b.AppendLine("NameCardsSecondLine = " + B(NameCardsSecondLine) + "   ; the smaller line under each name: their temper and trade, \"Stranger\", their gang, \"Wanted\", a nemesis's mood. Off shows the name alone; NameCards = off hides the cards altogether");
            b.AppendLine("WhosWho = " + B(WhosWho) + "   ; the papers you carry end with Who's who, the people you have met, the most recent first: who they are, how they take you, who they are friends or at odds with (and, with [Mind] MindLive, how they feel about you lately); read only");
            b.AppendLine("GrudgeMeter = " + B(GrudgeMeter) + "   ; top right: strangers whose grudge against you is building, with the mark where they'd become a nemesis, and the nemesis after you");
            b.AppendLine();
            b.AppendLine("[WestWorld]");
            b.AppendLine("WestWorld = " + B(WestWorld) + "   ; master switch: the people who start to see their world is made. false turns off everything in this section");
            b.AppendLine("AwakeningShare = " + AwakeningShare + "   ; percent of people who slowly wake up and see the world for what it is (0 turns the Awakening off)");
            b.AppendLine("AwakeningDays = " + AwakeningDays + "   ; in-game days a woken person spends on each of the first three stages");
            b.AppendLine("AwakeningBreakdown = " + AwakeningBreakdown + "   ; percent of the time an unravelling or awake person breaks down (kneels, shouts at the sky, runs) rather than keeping it together");
            b.AppendLine("AwakeningDespair = " + AwakeningDespair + "   ; percent chance that a fully awake ordinary person, alone, can't bear it and ends their own life, with a gun that fits them, poison, a knife, or by running at a wall (0-100, 0 turns this off; a content note is in the readme)");
            b.AppendLine("WakeVoices = " + B(WakeVoices) + "   ; the waking say the game's own recorded lines in the wrong places: to nobody, the same words again, to the sky, at you; a little at the first stage, a great deal by the fourth, with a thought over the head for what they make of it");
            b.AppendLine("HostsQuestionReality = " + B(HostsQuestionReality) + "   ; people sometimes wonder whether their world is real");
            b.AppendLine("RealityChance = " + RealityChance + "   ; percent of remarks and thoughts that drift that way");
            b.AppendLine("Glitches = " + B(Glitches) + "   ; rarely, someone freezes or repeats themselves");
            b.AppendLine("Reveries = " + B(Reveries) + "   ; the dead may wake up again, remembering how they died and who did it");
            b.AppendLine("ReturnChance = " + ReturnChance + "   ; percent of the people enRAGE knows (not story characters) who wake up after you kill them");
            b.AppendLine("StrangerReturnChance = " + StrangerReturnChance + "   ; percent of strangers you kill who come back");
            b.AppendLine();
            b.AppendLine("[Stalkers]");
            b.AppendLine("Stalkers = " + B(Stalkers) + "   ; master switch: rarely, someone follows you from a distance and hides when you look. false turns off everything in this section");
            b.AppendLine("StalkerEveryMinutes = " + StalkerEveryMinutes + "   ; every this many real minutes of play, a roll for a stalker (1-120)");
            b.AppendLine("StalkerChance = " + StalkerChance + "   ; percent chance on each roll");
            b.AppendLine("StalkerRecords = " + B(StalkerRecords) + "   ; one of enRAGE's people may be the stalker (one who hates you most likely), while enRAGE has no use for them; handed back the moment it has, and after 20 minutes at most");
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
            b.AppendLine("EncountersAtOnce = " + EncountersAtOnce + "   ; how many encounters (and a stalker) may be going on at once, one of each kind (1-8); each roll starts one at most, so they come one by one");
            b.AppendLine("CryingWoman = " + B(CryingWoman) + "   ; at night in the wilds, a woman sobbing in the dark. Go close and she may draw a knife, or not be alone");
            b.AppendLine("CryingWomanChance = " + CryingWomanChance + "   ; percent on each roll, between ten at night and four in the morning");
            b.AppendLine("NightVisitor = " + B(NightVisitor) + "   ; after midnight in a town, a pale figure crouched over a body: it rises, stares, and is gone when you look away; next morning the town speaks of it");
            b.AppendLine("NightVisitorChance = " + NightVisitorChance + "   ; percent on each roll, midnight to four, once a night at most");
            b.AppendLine("Travellers = " + B(Travellers) + "   ; by day on the roads, a man on foot waves you down and asks the way to the nearest town; greet him, or stop a moment, and he finds his way");
            b.AppendLine("TravellerChance = " + TravellerChance + "   ; percent on each roll, seven in the morning to seven at night, once a day at most");
            b.AppendLine("Campfires = " + B(Campfires) + "   ; at night in the wilds, two or three men round a fire off the road call you over for coffee and a tale; now and then a traveller walks in from the dark");
            b.AppendLine("CampfireChance = " + CampfireChance + "   ; percent on each roll, eight in the evening to four, with ten game hours between fires");
            b.AppendLine("VoiceBelow = " + B(VoiceBelow) + "   ; West World: late at night in a saloon, a muffled plea from under the boards behind the bar; the barkeep says it's the pipes, and his thoughts say otherwise; once a night a town");
            b.AppendLine("SupperInvitation = " + B(SupperInvitation) + "   ; at dusk on the roads, a too-friendly man asks you to supper at his place. Follow him there on foot and you wake in a ditch at first light with most of your cash gone ($250 at most). Ride on and he lets you go. Off until it's been tested");
            b.AppendLine("SupperChance = " + SupperChance + "   ; percent on each roll, half past five to nine in the evening, once a day at most");
            b.AppendLine("Executions = " + B(Executions) + "   ; out on the roads, two men holding one of a nearby town's people on his knees at gunpoint, for a reason from his own life. Ride on and they go through with it");
            b.AppendLine("ExecutionChance = " + ExecutionChance + "   ; percent on each roll, once a day at most, outside the towns");
            b.AppendLine("Hermits = " + B(Hermits) + "   ; West World: people who woke and walked out of their towns live rough in the wilds, and keep their camps");
            b.AppendLine("HermitChance = " + HermitChance + "   ; percent on each roll that a new hermit's camp turns up near you in the wilds");
            b.AppendLine("Gatherings = " + B(Gatherings) + "   ; West World: around midnight in the wilds, the awake stand in a silent circle");
            b.AppendLine("GatheringChance = " + GatheringChance + "   ; percent on each roll, between midnight and half past three");
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
            b.AppendLine("AvengersAtOnce = " + AvengersAtOnce + "   ; at most this many avengers riding, arrived or still alive at a time (1-10); the rest grieve at home");
            b.AppendLine("AvengerGapHours = " + AvengerGapHours + "   ; game hours between one avenger's arrival and the next's (0-720)");
            b.AppendLine("CastRelatives = " + B(CastRelatives) + "   ; in their town, folk on the street turn out to be someone's relative");
            b.AppendLine("CastRelativesSpawn = " + B(CastRelativesSpawn) + "   ; when nobody free is on the street (a well-played town: enRAGE has named most walkers by their looks, and the test cast carry names), the relative is made out of sight behind you and let go to the game once they have spoken; false: the walk-up waits for a free passer-by");
            b.AppendLine("OwnLooks = " + B(OwnLooks) + "   ; (in testing) without enRAGE: someone you met is known again by how they look (the same clothes on the same build), so the townsman you spoke to yesterday is the same man today, not a stranger with his face; with enRAGE running, its own knowing by look is used instead; false: a new stranger each time a body is met again");
            b.AppendLine("Drama = " + B(Drama) + "   ; strangers argue, flirt, brawl, propose and gossip around you");
            b.AppendLine("DramaGapSeconds = " + DramaGapSeconds + "   ; at most one street scene this often (seconds); a chance meeting between two people with no reason of their own waits one and a half times this. Lower for a livelier street");
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
            b.AppendLine("Grudges = " + B(Grudges) + "   ; grudges that travel: a friend of someone wronged has it out with the one who did it; and someone whose friend you killed may come up and say so to your face (never violent)");
            b.AppendLine("DoctorsPatient = " + B(DoctorsPatient) + "   ; by day in a town with its doctor (Valentine, Rhodes, Saint Denis), someone near you walks to him for what ails them (a child's fever, a tooth, ribs after a fight, a back, a cough...), tells him, hears his word, and walks home thinking it over");
            b.AppendLine("ErrandPace = " + F(ErrandPace) + "   ; how fast people walk on their errands here (to a scene, to the doctor, to a talk, to a camp seat, home again): 1 an ordinary walk, 2 brisk, 3 a run");
            b.AppendLine("LawChases = " + B(LawChases) + "   ; when a pickpocket runs and a lawman is near, the lawman gives chase, brings him down and holds him for arrest");
            b.AppendLine("Lifts = " + B(Lifts) + "   ; a man may bump into you anywhere and lift a little of your cash (a fifth, $1 to $25), then run: catch him, or let a lawman, to get it back");
            b.AppendLine("Vices = " + B(Vices) + "   ; habits and vices: someone in a saloon whose trouble ended badly today, or who has just heard a friend has died, drinks the night away (and a friend may sit with them); drunk for the rest of the night");
            b.AppendLine("Kindnesses = " + B(Kindnesses) + "   ; small kindnesses between people: someone who trips in the street is helped up by a friend or a kind passer-by, or left lying by the cold and their enemies (and you can give them a hand up yourself); remembered either way");
            b.AppendLine("SightReactions = " + B(SightReactions) + "   ; people near you notice how you look (bloodied, drunk, filthy, soaked) and take it by their nature: the timid afraid, the hard sizing you up, the kind concerned; and a man walking off bloodied from a street fight gets looked at");
            b.AppendLine("Acknowledge = " + B(Acknowledge) + "   ; two people who know each other nod, tip a hat or wave as they pass (enemies give a hard look, sweethearts a long one), and each thinks something of it; a pair once in 45 game minutes");
            b.AppendLine("TroubleWeight = " + F(TroubleWeight) + "   ; how often people come to blows or quarrel: 1 as built, 2 twice as often, 0.5 half, 0 never by chance (0-5); it scales the brawl's and the argument's share of a chance meeting, hot blood between strangers, drink and temper, and a snap that comes to blows; over 1 it also shortens everyone's rest between scenes (2 min, 15 after blows, divided by it)");
            b.AppendLine("WomenInFights = " + B(WomenInFights) + "   ; women may come to blows in a quarrel too (with a man or another woman), half as often as two men for each woman in it; false: only two men ever fight");
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
            b.AppendLine("Subtitles = " + Subtitles + "   ; what people say to you, while their voice plays (their recorded words when known). The game draws its own white subtitle for any recorded line while its Subtitles option is on, and a mod can't stop that, so: game: RealNPCs' boxed subtitles only while the game's option is off (one set at a time); ours: RealNPCs' always (switch the game's off to see only these); both; off: none of RealNPCs'. Talk you only overhear (ScenesOnTicker) shows in the same box unless this is off");
            b.AppendLine("UseGameFeed = " + B(UseGameFeed) + "   ; EXPERIMENTAL: the game's top-left toasts (same as NewsTo = gamefeed)");
            b.AppendLine("TestKey = " + TestKey + "   ; shows what the nearest person enRAGE knows thinks of you");
            b.AppendLine("WarNewsTo = " + WarNewsTo + "   ; where gang and faction news (wars, feuds, clashes, rivalries) is shown: feed (the game's own updates, top left), corner (the \"Around town\" box) or ticker");
            b.AppendLine("TerritoryNews = " + B(TerritoryNews) + "   ; enRAGE's territory changes (a gang gaining or losing its hold on a district) shown where WarNewsTo says. enRAGE also prints them itself, as white text, while its own [Menu] Messages is 3");
            b.AppendLine("FlirtKey = " + FlirtKey + "   ; look at someone (as if to greet them) and press it to flirt");
            b.AppendLine("ReaderKey = " + ReaderKey + "   ; read the diaries, letters and clippings you carry");
            b.AppendLine("ReloadIni = " + B(ReloadIni) + "   ; pick up changes to this file while the game runs");
            b.AppendLine("ReplaceEnrageSettings = " + B(ReplaceEnrageSettings) + "   ; Alt+F11 opens the larger RealNPCs settings panel (RealNPCs, West World, Stalkers and every enRAGE section) instead of enRAGE's own (enRAGE 0.9.5.3 only)");
            b.AppendLine("SettingsKey = " + SettingsKey + "   ; opens the RealNPCs settings panel (with any enRAGE version), and closes it again");
            b.AppendLine("SettingsKey2 = " + SettingsKey2 + "   ; a second key that does the same (None: no second key)");
            b.AppendLine("PanelSlowsTime = " + B(PanelSlowsTime) + "   ; the world slows while the settings panel is open, as it does while the game's weapon wheel or satchel is open (never in a story mission)");
            b.AppendLine("PanelShowsAll = " + B(PanelShowsAll) + "   ; the settings panel shows every setting and the Testing page; false: only the essentials, the hotkeys and the mod list (F on the panel switches)");
            b.AppendLine("PanelTimeScale = " + F(PanelTimeScale) + "   ; 0 pauses the world and its clock while the panel is open; 1 is normal speed, 0.25 a quarter (0-1)");
            b.AppendLine("DebugLog = " + B(DebugLog));
            b.AppendLine("PerfMeter = " + B(PerfMeter) + "   ; times every part of RealNPCs each frame: F7 writes the costliest (TEST perf), and the log says when the add-on goes over 1 ms a frame for 10 s or one frame over 4 ms (PERF)");
            b.AppendLine();
            b.AppendLine("[Controller]");
            b.AppendLine("PadKeys = " + B(PadKeys) + "   ; a controller's buttons for RealNPCs' keys below, used while a controller is the last thing you touched");
            b.AppendLine("; each is a button held and another pressed (RB+DPadUp), or one button alone. Buttons: A B X Y LB RB LT RT LS RS Select DPadUp DPadDown");
            b.AppendLine("; DPadLeft DPadRight (PlayStation: Cross Circle Square Triangle L1 R1 L2 R2 L3 R3 Touchpad), or a game control by name (INPUT_...); None: no button.");
            b.AppendLine("; The settings panel's Hotkeys page sets them from the controller. Everything on its Testing page fires from there with A.");
            b.AppendLine("PadSettings = " + PadSettings + "   ; opens the settings panel (as " + SettingsKey + " does)");
            b.AppendLine("PadStatus = " + PadStatus + "   ; what the nearest person thinks of you, and the status lines in the log (as " + TestKey + " does)");
            b.AppendLine("PadLetters = " + PadLetters + "   ; your letters, diaries and clippings (as " + ReaderKey + " does)");
            b.AppendLine("PadTickerNext = " + PadTickerNext + "   ; the next line on the ticker now (as " + TickerNextKey + " does)");
            b.AppendLine();
            b.AppendLine("[Testing]");
            b.AppendLine("TestKeys = " + B(TestKeys) + "   ; master switch for the keys below (the settings panel's Testing page fires them with it off too)");
            b.AppendLine("ShowMinds = " + B(ShowMinds) + "   ; a small card over named people within 11 m: temper, mood, worry, ties and what is on their mind");
            b.AppendLine("ShowWakeStages = " + B(ShowWakeStages) + "   ; a tag over each person within 25 m who has begun to wake: stirring, questioning, unravelling or awake, and what holds them back (riding, a counter, had their chance)");
            b.AppendLine("TestCastReturns = " + B(TestCastReturns) + "   ; the Testing page's test cast, once made and kept in the save, come back by themselves (out of sight) when you come to their town; off as it comes: the cast come only on the Testing row's key");
            foreach (TestActions.Entry e in TestActions.All)
                b.AppendLine(e.KeySetting + " = " + TestKeyOf(e) + "   ; " + e.Label + (e.Needs != null ? " (" + e.Needs + ")" : string.Empty) + "; None: no key");
            return b.ToString();
        }

        /// A setting's value written the way the ini writes it ("true", "0.962", "rush"), or null for an unknown key.
        public string ValueText(string key)
        {
            TestActions.Entry te = TestActions.ByKey(key);
            if (te != null && !te.Field)
                return TestKeyOf(te);
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
