// The Testing page's catalogue (overnight 2026-10-27, Tier 1.2): every interaction and encounter RealNPCs can fire
// on demand, with its label, its group on the page, the ini key that fires it ([Testing], None by default) and what
// the player has to do or where he has to be first. The game side (WestworldScript.Testing.cs) dispatches each id to
// the feature's own start path; a test may skip the time, place and chance rolls, never the safety rules.
using System;
using System.Collections.Generic;
using System.Linq;

namespace EnrageWestworld.Core
{
    public static class TestActions
    {
        public sealed class Entry
        {
            public string Id, Label, Group, KeySetting, DefaultKey, Needs;
            public bool Field;   // the key is one of Settings' own fields (StalkerKey, WakeKey...), else Settings.TestKeyMap

            public Entry(string id, string group, string label, string keySetting, string defaultKey = "None", string needs = null, bool field = false)
            {
                Id = id;
                Group = group;
                Label = label;
                KeySetting = keySetting;
                DefaultKey = defaultKey;
                Needs = needs;
                Field = field;
            }
        }

        public static readonly string[] Groups = { "Encounters", "People", "Street scenes", "Tools" };

        public static readonly List<Entry> All = new List<Entry>
        {
            // ---- encounters (the encounters' tick starts them once nothing is in the way) ------------------------
            new Entry("crying", "Encounters", "The crying woman", "CryingWomanKey", field: true),
            new Entry("execution", "Encounters", "An execution on the road", "ExecutionKey", "[", needs: "a town with regulars", field: true),
            new Entry("hermit", "Encounters", "A hermit's camp", "HermitKey", needs: "outside a town; the camp stays", field: true),
            new Entry("traveller", "Encounters", "A traveller asking the way", "TestTravellerKey", needs: "out of the towns; best on a road"),
            new Entry("visitor", "Encounters", "The night visitor", "TestVisitorKey", needs: "in a town, out of doors, no lawman within 60 m"),
            new Entry("gathering", "Encounters", "A gathering of the awake", "GatheringKey", field: true),
            new Entry("stalker", "Encounters", "A stalker now", "StalkerKey", "]", needs: "outdoors", field: true),
            // ---- people --------------------------------------------------------------------------------------------
            new Entry("wake", "People", "Wake the person you face one stage", "WakeKey", "Y", needs: "face someone a few steps off", field: true),
            new Entry("realtalk", "People", "Two people talk in their real voices", "TestTalkKey", "Shift+F7", needs: "two people within 20 m"),
            new Entry("rough", "People", "A face into the bar (rough handling)", "TestRoughKey", "Shift+F9", needs: "two men within 10 m, best in a saloon"),
            new Entry("font", "People", "The subtitle line in each font", "TestFontKey", "Shift+F2"),
            new Entry("plea", "People", "A plea at gunpoint (then aim at someone)", "TestPleaKey", needs: "aim at someone within 30 m in the next minute; lowering your gun counts as real mercy"),
            new Entry("arrival", "People", "A word as you ride into town", "TestArrivalKey", needs: "in a town"),
            new Entry("letter", "People", "A letter (the waiting ones, else a test one)", "TestLetterKey"),
            new Entry("musing", "People", "A stranger says something strange", "TestMusingKey", needs: "someone 3-14 m off"),
            new Entry("glitch", "People", "A stranger glitches", "TestGlitchKey", needs: "someone 3-16 m off"),
            new Entry("disbelief", "People", "Disbelief, voiced, over someone's head", "TestDisbeliefKey", needs: "a named person within 15 m"),
            new Entry("fall", "People", "Someone takes a fall (and is helped up, or not)", "TestFallKey", needs: "two named people outdoors, one of them within 14 m and in view"),
            new Entry("callout", "People", "A man calls another out at noon", "TestCalloutKey", needs: "a town street, two named men with bad blood 2-14 m apart"),
            new Entry("grudge", "People", "A friend takes up a wrong", "TestGrudgeKey", needs: "a friend of someone wronged in the last 3 days, near the one who did it"),
            new Entry("grudge:you", "People", "Someone grieving one you killed", "TestGrudgeYouKey", needs: "in town, someone within 20 m whose friend you killed this week"),
            new Entry("drink", "People", "Someone drinks away bad news", "TestDrinkKey", needs: "a saloon, a named person within 15 m"),
            new Entry("round", "People", "Someone stands the house a drink", "TestRoundKey", needs: "a saloon, two named people within 6 m of each other"),
            new Entry("sight", "People", "Someone reacts to how you look", "TestSightKey", needs: "someone within 10 m who can see you; looking ordinary, bloodied, drunk, filthy and soaked in turn"),
            new Entry("nod", "People", "Two who know each other acknowledge it", "TestNodKey", needs: "two people with a tie within 10 m of each other"),
            // ---- street scenes: the nearest named pair that fits plays it now (no tie or memory is noted for a test) --
            new Entry("scene:argue", "Street scenes", "An argument", "TestArgueKey", needs: "two named people within 16 m"),
            new Entry("scene:brawl", "Street scenes", "A fistfight", "TestBrawlKey", needs: "two named men within 16 m"),
            new Entry("scene:smalltalk", "Street scenes", "Small talk", "TestSmallTalkKey", needs: "two named people within 16 m"),
            new Entry("scene:gossip", "Street scenes", "Gossip", "TestGossipKey", needs: "two named people within 16 m"),
            new Entry("scene:flirt", "Street scenes", "A flirt", "TestFlirtSceneKey", needs: "a named man and woman within 16 m"),
            new Entry("scene:court", "Street scenes", "A courtship (the answer turns: yes, maybe, no)", "TestCourtKey", needs: "a named man and woman within 16 m"),
            new Entry("scene:propose", "Street scenes", "A proposal", "TestProposeKey", needs: "a named man and woman within 16 m"),
            new Entry("scene:drunk", "Street scenes", "A drunk calls a man out", "TestDrunkKey", needs: "two named men within 16 m"),
            new Entry("scene:lift", "Street scenes", "A purse lifted", "TestLiftKey", needs: "two named people within 16 m"),
            new Entry("scene:pickpocket", "Street scenes", "A pickpocket caught at it", "TestPickpocketKey", needs: "a named man and another within 16 m"),
            new Entry("scene:beg", "Street scenes", "Someone begs a coin", "TestBegKey", needs: "two named people within 16 m"),
            new Entry("scene:tend", "Street scenes", "A friend tends the one who was hurt", "TestTendKey", needs: "two named people within 16 m"),
            new Entry("scene:avoid", "Street scenes", "Someone keeps clear of the one who wronged them", "TestAvoidKey", needs: "two named people within 16 m"),
            new Entry("scene:standup", "Street scenes", "Someone stands up to a bully", "TestStandUpKey", needs: "two named men and a third named person within 16 m; a fistfight they step into"),
            new Entry("scene:standoff", "Street scenes", "A standoff", "TestStandoffKey", needs: "two named men within 16 m"),
            new Entry("scene:song", "Street scenes", "A song", "TestSongKey", needs: "two named people within 16 m"),
            new Entry("scene:talltale", "Street scenes", "A tall tale", "TestTallTaleKey", needs: "two named people within 16 m"),
            new Entry("scene:haggle", "Street scenes", "Haggling", "TestHaggleKey", needs: "two named people within 16 m"),
            new Entry("scene:cheat", "Street scenes", "A card cheat caught", "TestCheatKey", needs: "two named men within 16 m"),
            new Entry("scene:hosts", "Street scenes", "Two who are waking talk of it", "TestHostsKey", needs: "two named people; HostsQuestionReality on"),
            new Entry("scene:x:snap", "Street scenes", "A snap at the worst of a worry", "TestSnapKey", needs: "someone whose worry is at its worst"),
            new Entry("scene:x:goodnews", "Street scenes", "Good news told", "TestGoodNewsKey", needs: "someone whose worry ended well"),
            new Entry("scene:x:badnews", "Street scenes", "Bad news told", "TestBadNewsKey", needs: "someone whose worry ended badly"),
            new Entry("scene:x:quarrel", "Street scenes", "An old quarrel picked up", "TestQuarrelKey", needs: "two named people within 16 m"),
            new Entry("scene:x:sweet", "Street scenes", "Sweethearts", "TestSweetKey", needs: "a named man and woman within 16 m"),
            new Entry("scene:x:debt", "Street scenes", "A debt called in", "TestDebtKey", needs: "two named people within 16 m"),
            new Entry("scene:x:feud", "Street scenes", "A feud flares", "TestFeudKey", needs: "two named people within 16 m"),
            new Entry("scene:x:courting", "Street scenes", "A courting worry, face to face", "TestCourtingKey", needs: "a named man and woman within 16 m"),
            // ---- tools ---------------------------------------------------------------------------------------------
            new Entry("showminds", "Tools", "Show minds (a card over people near you)", "TestShowMindsKey"),
            new Entry("replay", "Tools", "Fire the last test again", "TestReplayKey"),
        };

        public static Entry Find(string id)
        {
            return id == null ? null : All.FirstOrDefault(e => e.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        }

        /// The entry an ini key belongs to, or null.
        public static Entry ByKey(string key)
        {
            return key == null ? null : All.FirstOrDefault(e => e.KeySetting.Equals(key, StringComparison.OrdinalIgnoreCase));
        }
    }
}
