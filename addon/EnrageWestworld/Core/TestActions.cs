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
            new Entry("gathering", "Encounters", "A gathering of the awake", "GatheringKey", field: true),
            new Entry("stalker", "Encounters", "A stalker now", "StalkerKey", "]", needs: "outdoors", field: true),
            // ---- people --------------------------------------------------------------------------------------------
            new Entry("wake", "People", "Wake the person you face one stage", "WakeKey", "Y", needs: "face someone a few steps off", field: true),
            new Entry("realtalk", "People", "Two people talk in their real voices", "TestTalkKey", "Shift+F7", needs: "two people within 20 m"),
            new Entry("rough", "People", "A face into the bar (rough handling)", "TestRoughKey", "Shift+F9", needs: "two men within 10 m, best in a saloon"),
            new Entry("font", "People", "The subtitle line in each font", "TestFontKey", "Shift+F2"),
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
