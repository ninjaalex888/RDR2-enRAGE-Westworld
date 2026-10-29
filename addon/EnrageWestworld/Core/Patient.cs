// The doctor's patient (OVERNIGHT.md Tier 4, item 16; built in the night run of 2026-10-28, item 14; widened 2026-10-29,
// Lex: "instead of bringing their child in, NPCs can go to the doctor and try to interact"): someone goes to the doctor
// for a complaint of their own (a bad tooth, a back, a cut gone bad, a cough, ribs after a fight, nerves, a stomach, eyes
// going) or a child's fever, says what's wrong to the doctor, hears his word, and walks home thinking it over. Which
// complaint comes from what the game side knows of them: their worry (the fever, a tooth), a beating lately, their age,
// their trade, their sex. The game side does the walking and the talking; this has the hours, the choosing and the words.
// All lines are original to this add-on.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static class Patient
    {
        /// Every complaint there are words for.
        public static readonly string[] Complaints = { "fever", "tooth", "ribs", "back", "hand", "cough", "sleep", "stomach", "eyes" };

        /// The doctor's hours: 08:00 to 18:00.
        public static bool Hour(int clock)
        {
            return clock >= 8 && clock < 18;
        }

        /// Whether this worry takes someone to the doctor: the fever, while it's on (stages 1 and 2: worse, the crisis).
        public static bool Takes(string worryKind, int stage)
        {
            return (worryKind == "fever" || worryKind == "tooth") && (stage == 1 || stage == 2);
        }

        /// What takes this person to the doctor: a child's fever or a tooth when that's their worry, ribs after a beating,
        /// else something of their own by age, trade and sex, the same each day for the same person (the seed).
        public static string ComplaintOf(int age, string trade, bool woman, bool hurt, string worryKind, int worryStage, string seed)
        {
            if (Takes(worryKind, worryStage))
                return worryKind;
            if (hurt)
                return "ribs";
            string t = (trade ?? string.Empty).ToLowerInvariant();
            bool working = t.Contains("farm") || t.Contains("labour") || t.Contains("rancher") || t.Contains("smith") || t.Contains("dock") || t.Contains("miner") ||
                           t.Contains("stable") || t.Contains("hand") || t.Contains("butcher") || t.Contains("rough") || t.Contains("trapper");
            string[] pool = age >= 58 ? new[] { "back", "eyes", "cough", "back" }
                : working ? new[] { "hand", "back", "cough", "stomach" }
                : woman ? new[] { "sleep", "cough", "stomach", "tooth" }
                : new[] { "cough", "stomach", "sleep", "tooth" };
            return pool[Voice.Hash(seed + "|complaint") % pool.Length];
        }

        /// Whether it's bad enough to go today without being asked (a test always goes): a fever, a tooth or ribs always;
        /// anything else one day in five for the same person.
        public static bool Pressing(string complaint, string seed)
        {
            return complaint == "fever" || complaint == "tooth" || complaint == "ribs" || Voice.Hash(seed + "|goes") % 5 == 0;
        }

        /// On the way there ({who}: the child's first name, for the fever).
        public static string Going(string complaint, int stage, string seed)
        {
            string s = seed + "|g" + complaint;
            switch (complaint)
            {
                case "fever":
                    return stage >= 2
                        ? Voice.Pick(s + "2", "Hotter than yesterday. Doc has to see {who} today. Has to.", "Walk faster. {who} was talking to nobody at dawn.", "If he says the word hospital I'll carry {who} there myself.")
                        : Voice.Pick(s + "1", "Doc'll know what to do for {who}. He has to.", "Three days of it now. Doc'll have a powder.", "Mind the mud. Can't go in to the doctor's with mud on me. {who}'d laugh.");
                case "tooth": return Voice.Pick(s, "Can't chew on that side. Can't sleep on it either.", "Pull it or finish me. Either one will do.");
                case "ribs": return Voice.Pick(s, "Every breath catches. He hit harder than he looked.", "Ribs. Could be cracked. Could be my pride.");
                case "back": return Voice.Pick(s, "Back's been seized since Tuesday. Bent like a question mark.", "Can't lift a sack without seeing stars.");
                case "hand": return Voice.Pick(s, "Cut it on a nail. Now it's red to the wrist. That isn't right.", "Hand's swelling. Can't close it round a handle.");
                case "cough": return Voice.Pick(s, "This cough's been with me a fortnight. Long enough.", "Coughed half the night. Woke the whole house.");
                case "sleep": return Voice.Pick(s, "Three nights without a wink. Something has to give.", "Lie down and my head starts talking. Every night.");
                case "stomach": return Voice.Pick(s, "Something I ate. Or something eating me.", "Stomach's been in knots since Sunday.");
                case "eyes": return Voice.Pick(s, "Can't read the paper at arm's length any more. Arm's too short.", "Everything's gone soft round the edges.");
            }
            return null;
        }

        /// What they tell the doctor, aloud.
        public static string Ask(string complaint, int stage, string seed)
        {
            string s = seed + "|a" + complaint;
            switch (complaint)
            {
                case "fever":
                    return stage >= 2
                        ? Voice.Pick(s + "2", "{who} didn't know me this morning. Please, doctor.", "The fever's worse. {who} is shaking with it.")
                        : Voice.Pick(s + "1", "It's {who}. Three days of fever and it won't break.", "My little one's burning up, doctor. {who}. Will you come?");
                case "tooth": return Voice.Pick(s, "It's this tooth, doctor. Back on the left. Nights are the worst.", "I've a tooth gone bad. The clove oil stopped helping.");
                case "ribs": return Voice.Pick(s, "Took a beating yesterday. It hurts to breathe.", "Fell against a chair. And a man. Mostly the man.");
                case "back": return Voice.Pick(s, "It's my back, doctor. Low down. It gets me when I bend.", "I can't straighten up of a morning. Worse than last winter.");
                case "hand": return Voice.Pick(s, "Cut my hand last week. It's gone hot and angry.", "Would you look at this hand? The cut won't close.");
                case "cough": return Voice.Pick(s, "It's this cough, doctor. It's hung on since the last rains.", "Can't shake a cough. Worse lying down.");
                case "sleep": return Voice.Pick(s, "I can't sleep, doctor. Not for days now.", "My nerves are all to pieces. I jump at every door.");
                case "stomach": return Voice.Pick(s, "My stomach's bad. I can't keep much down.", "There's a pain here, after I eat. Every time.");
                case "eyes": return Voice.Pick(s, "My eyes are going, doctor. I can't make out faces across the street.", "The print's gone small on me. Smaller every year.");
            }
            return null;
        }

        /// The doctor's word, aloud.
        public static string Answer(string complaint, int stage, string seed)
        {
            string s = seed + "|d" + complaint;
            switch (complaint)
            {
                case "fever":
                    return stage >= 2
                        ? Voice.Pick(s + "2", "I'll come by after noon. Cool cloths till then.", "If the shaking starts again, bring the child here. Don't wait.")
                        : Voice.Pick(s + "1", "Keep the child cool and give this twice a day.", "It's going round. Plenty of water, and send for me if it climbs.");
                case "tooth": return Voice.Pick(s, "That one's coming out. Sit down and hold the arms of the chair.", "It'll have to be drawn. Half a dollar, and a swallow of whiskey first.");
                case "ribs": return Voice.Pick(s, "Nothing broken that I can feel. Bind it tight and keep out of saloons.", "Cracked, maybe. Rest, and no lifting for two weeks.");
                case "back": return Voice.Pick(s, "Liniment, rubbed in hot, morning and night. Let somebody else do the lifting.", "Years of hauling. There's no cure for years, only rest.");
                case "hand": return Voice.Pick(s, "I'll lance it and clean it. This will sting, so look away.", "Carbolic on it twice a day, and keep it out of the dirt, if you can.");
                case "cough": return Voice.Pick(s, "A spoonful of this at night, and stay out of the damp.", "Your chest is clear enough. Keep warm and it'll pass.");
                case "sleep": return Voice.Pick(s, "A few drops of this in water at bedtime. Not more than a few.", "Less coffee, a walk before bed, and stop fretting over what you can't change.");
                case "stomach": return Voice.Pick(s, "Plain food for a week. Bread, broth, nothing fried.", "Soda in water after meals. And go easy on the whiskey.");
                case "eyes": return Voice.Pick(s, "Spectacles. The store can send for a pair. It's only age.", "Nothing in them that shouldn't be. It's the years. Get reading spectacles.");
            }
            return null;
        }

        /// Walking home, thinking it over.
        public static string Back(string complaint, int stage, string seed)
        {
            string s = seed + "|b" + complaint;
            switch (complaint)
            {
                case "fever":
                    return stage >= 2
                        ? Voice.Pick(s + "2", "Bottle in my coat. Walk, don't run. Running frightens {who}.", "If it's no better by dark I'll go back and bang on his door.", "He didn't say it would pass. He didn't say it wouldn't.")
                        : Voice.Pick(s + "1", "Twice a day. {who}'ll pull faces at it.", "Going round, he says. Then it'll go round and out again.", "Sunday. Hold on to Sunday.");
                case "tooth": return Voice.Pick(s, "Gone. The hole's worse than the tooth was. For now.", "Half a dollar lighter and one tooth lighter.");
                case "ribs": return Voice.Pick(s, "Bound up like a parcel. Two weeks, he says.", "No lifting. No fighting. Nothing worth getting up for.");
                case "back": return Voice.Pick(s, "Liniment. Smells like a horse barn. If it works I don't care.", "Rest, he says. Who does my work while I rest?");
                case "hand": return Voice.Pick(s, "Clean and bound. Throbs less already. Or I'm telling myself so.", "Carbolic smells worse than the cut did.");
                case "cough": return Voice.Pick(s, "A bottle of something brown. Tastes like tar and lemons.", "A clear chest, he says. Then what's rattling in there?");
                case "sleep": return Voice.Pick(s, "A few drops. Not more. I'll count them.", "Stop fretting, he says. Like it's a hat I can take off.");
                case "stomach": return Voice.Pick(s, "Bread and broth for a week. Might as well be in jail.", "Easy on the whiskey. He's never lived my week.");
                case "eyes": return Voice.Pick(s, "Spectacles. At my age. Well, it's that or guess at faces.", "Only age, he says. As if that was nothing.");
            }
            return null;
        }

        /// One line for the log: why they went.
        public static string Why(string complaint, string child, int stage)
        {
            switch (complaint)
            {
                case "fever": return child + "'s fever" + (stage >= 2 ? " (the crisis)" : "");
                case "tooth": return "a bad tooth";
                case "ribs": return "their ribs, after a beating";
                case "back": return "a bad back";
                case "hand": return "a cut gone bad";
                case "cough": return "a cough";
                case "sleep": return "nerves and no sleep";
                case "stomach": return "a bad stomach";
                case "eyes": return "failing eyes";
            }
            return complaint;
        }

        public static IEnumerable<string> Lines()
        {
            foreach (string c in Complaints)
                for (int i = 0; i < 8; i++)
                    for (int st = 1; st <= 2; st++)
                    {
                        string s = "pt" + i;
                        yield return Going(c, st, s);
                        yield return Ask(c, st, s);
                        yield return Answer(c, st, s);
                        yield return Back(c, st, s);
                    }
        }
    }
}
