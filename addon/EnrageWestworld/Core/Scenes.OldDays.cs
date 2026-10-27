// Two old men on a bench arguing about the old days (overnight 2026-10-27, Tier 4): when a chance meeting brings two old
// men together, now and then they fall to disputing the past: the worst winter, the price of coffee, what the town was
// before the railroad, a ridge they both remember differently. Neither gives an inch, and it ends in a laugh. A long
// exchange, overheard. All lines are original to this add-on.
using System;
using System.Collections.Generic;

namespace EnrageWestworld.Core
{
    public static partial class Scenes
    {
        public static readonly string[] OldTopics = { "winter", "coffee", "railroad", "ridge", "sheriff", "dance" };

        public static Scene OldDays(SceneCast c, string seed)
        {
            string a = N(c, 0, seed), b = N(c, 1, seed);
            Scene s = new Scene { Kind = "smalltalk", Summary = "two old-timers argued about the old days" };
            string topic = OldTopics[Voice.Hash(seed + "|old") % OldTopics.Length];
            switch (topic)
            {
                case "winter":
                    s.Add(0, "Worst winter I ever saw was '72. Snow up to the eaves.", "mild", "face")
                     .Add(1, "'72? The winter of '86 was worse, and you know it, " + a + ".", "mild")
                     .Add(0, "In '86 I was down in Texas. In '72 I was here, and I'm telling you.", "mild")
                     .Add(1, "You were in Texas in '72 too, near as I recall.", "mock")
                     .Add(0, "Your recall ain't worth a plug nickel, " + b + ".", "angry")
                     .Add(1, "Better than yours. You can't recall where you left your teeth.", "mock")
                     .Add(0, "...They're in my coat.", "mild")
                     .Add(1, "There. You see?", "cheer");
                    break;
                case "coffee":
                    s.Add(0, "Coffee was ten cents a pound when I was a boy.", "mild", "face")
                     .Add(1, "Ten? It was six, and it was better coffee.", "mild")
                     .Add(0, "It was ten, " + b + ". My pa paid it. I carried it home.", "mild")
                     .Add(1, "Your pa paid ten because he never could haggle, " + a + ".", "mock")
                     .Add(0, "Don't you speak ill of my pa.", "angry")
                     .Add(1, "Speak ill of him? I liked your pa. Couldn't haggle, is all.", "mild")
                     .Add(0, "...He couldn't, could he.", "mild")
                     .Add(1, "Paid double for a mule once. Mule was blind.", "cheer");
                    break;
                case "railroad":
                    s.Add(0, "Before the railroad come through, a man knew every face in this county.", "mild", "face")
                     .Add(1, "Before the railroad come through, a man walked three days for flour.", "mild")
                     .Add(0, "And he was the better for it. Look at them now. Soft.", "mild")
                     .Add(1, "Soft. You took the train to see your sister at Easter, " + a + ".", "mock")
                     .Add(0, "That's different, " + b + ". That's family.", "mild")
                     .Add(1, "Everything's different when it's you.", "mock")
                     .Add(0, "...Smoke and noise, the whole of it.", "mild")
                     .Add(1, "Smoke and noise and a seat by the window. You loved it.", "cheer");
                    break;
                case "sheriff":
                    s.Add(0, "Old Sheriff Pike. Now there was a lawman. Caught a horse thief before breakfast.", "mild", "face")
                     .Add(1, "Pike? Pike couldn't hit a barn from inside it, " + a + ".", "mock")
                     .Add(0, "He hit that thief, didn't he? Twice.", "mild")
                     .Add(1, "The thief tripped. Pike shot the barn.", "mock")
                     .Add(0, "You weren't even there, " + b + ".", "angry")
                     .Add(1, "I was the one held his horse.", "mild")
                     .Add(0, "...That was you?", "mild")
                     .Add(1, "Gave me a nickel and never said thank you. That was Pike all over.", "cheer");
                    break;
                case "dance":
                    s.Add(0, "Danced with your wife before you ever did, " + b + ". Harvest dance, '68.", "mild", "face")
                     .Add(1, "She danced with you out of pity. Told me so herself.", "mock")
                     .Add(0, "Pity. She stepped on my feet the whole night.", "mild")
                     .Add(1, "That was on purpose.", "mock")
                     .Add(0, "...Was it?", "mild")
                     .Add(1, "Forty years she's dined out on that story, " + a + ".", "mild")
                     .Add(0, "Well. Tell her I said she dances better now.", "mild")
                     .Add(1, "I'll tell her. She'll laugh herself sick.", "cheer");
                    break;
                default:
                    s.Add(0, "We'd have held that ridge if the captain had listened to me.", "mild", "face")
                     .Add(1, "He did listen to you, " + a + ". That's how we came to be running down it.", "mock")
                     .Add(0, "That's a lie, " + b + ", and you know it.", "angry")
                     .Add(1, "I know I lost my boot on that ridge. You lost your hat.", "mild")
                     .Add(0, "Lost my hat to a bullet. That's not losing it.", "mild")
                     .Add(1, "Lost it all the same. Never saw it again.", "mild")
                     .Add(0, "...It was a good hat.", "mild")
                     .Add(1, "Best hat in the regiment. I'll grant you the hat.", "cheer");
                    break;
            }
            return s;
        }

        public static IEnumerable<Scene> OldDaysSamples(SceneCast c)
        {
            for (int i = 0; i < 40; i++)
                yield return OldDays(c, "od" + i);
        }
    }
}
