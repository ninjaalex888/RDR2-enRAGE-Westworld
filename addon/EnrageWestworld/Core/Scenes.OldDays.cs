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
            bool second = Voice.Hash(seed + "|old2") % 2 == 1;   // each topic has two tellings, so no two pairs of old men say the same words (night 2026-10-28, item 10)
            switch (topic)
            {
                case "winter" when second:
                    s.Add(0, "Froze the creek solid in '80. Drove a wagon over it.", "mild", "face")
                     .Add(1, "You drove a wagon into it, " + a + ". We fished you out.", "mock")
                     .Add(0, "That was the thaw. Different week.", "mild")
                     .Add(1, "Same wagon. Same fool at the reins.", "mock")
                     .Add(0, "You'd have gone the same way, " + b + ", only you were abed with the grippe.", "angry")
                     .Add(1, "Abed and dry. There's a lesson in it.", "mild")
                     .Add(0, "...Lost a good team that day.", "mild")
                     .Add(1, "Lost your hat too. Floated clear to the mill.", "cheer");
                    break;
                case "coffee" when second:
                    s.Add(0, "A man could eat a week on a dollar when I was young.", "mild", "face")
                     .Add(1, "A man could starve a week on a dollar, is what. You've forgot the prices.", "mild")
                     .Add(0, "Eggs a penny apiece, " + b + ". I remember what I remember.", "mild")
                     .Add(1, "You remember your mother's hens, " + a + ". That ain't a price.", "mock")
                     .Add(0, "Leave my mother out of it.", "angry")
                     .Add(1, "Finest hens in the county. I'm saying nothing against the hens.", "mild")
                     .Add(0, "...They were good layers.", "mild")
                     .Add(1, "Ate one at your wedding. Tough as a saddle.", "cheer");
                    break;
                case "railroad" when second:
                    s.Add(0, "First engine come through, half the town ran the other way.", "mild", "face")
                     .Add(1, "You ran the other way, " + a + ". The town stood and cheered.", "mock")
                     .Add(0, "I went to see to the horses. They were spooked.", "mild")
                     .Add(1, "The horses were tied. You were the one spooked.", "mock")
                     .Add(0, "I was twelve years old, " + b + ".", "angry")
                     .Add(1, "Twelve and fast. Never saw a boy clear a fence like it.", "mild")
                     .Add(0, "...It was a loud machine.", "mild")
                     .Add(1, "Still is. Still clear a fence for it, I'd wager.", "cheer");
                    break;
                case "sheriff" when second:
                    s.Add(0, "Marshal Gaines kept this town quiet with a look. Never drew once.", "mild", "face")
                     .Add(1, "Never drew because he never had to find his gun. Left it in the saloon twice, " + a + ".", "mock")
                     .Add(0, "He left it on purpose. A man like that didn't need it.", "mild")
                     .Add(1, "He left it because he was drunk. I carried it back to him.", "mock")
                     .Add(0, "You carried a lot of things in those days, " + b + ". Most of them weren't yours.", "angry")
                     .Add(1, "That was the once, and I was a boy.", "mild")
                     .Add(0, "...He did have a look about him, though.", "mild")
                     .Add(1, "He did. Looked straight through you. Even sober.", "cheer");
                    break;
                case "dance" when second:
                    s.Add(0, "Fiddler at the Christmas dance of '75. Never heard the like since.", "mild", "face")
                     .Add(1, "That fiddler was my cousin, " + a + ", and he was tone deaf.", "mock")
                     .Add(0, "Then he was a tone-deaf wonder. Whole room was up.", "mild")
                     .Add(1, "Whole room was up because the stove went out. They danced to keep warm.", "mock")
                     .Add(0, "You'd argue with the sunrise, " + b + ".", "angry")
                     .Add(1, "Only when it's late.", "mild")
                     .Add(0, "...He could play, your cousin.", "mild")
                     .Add(1, "He could. Deaf as a post and he could play. Go figure that.", "cheer");
                    break;
                case "ridge" when second:
                    s.Add(0, "That night on the ridge I never closed my eyes once.", "mild", "face")
                     .Add(1, "You snored like a sawmill, " + a + ". The captain threw a boot at you.", "mock")
                     .Add(0, "That was Hollis snoring. I was on watch.", "mild")
                     .Add(1, "Hollis was dead by then, God rest him. It was you, " + b + ".", "mild")
                     .Add(0, "...Was he? That early?", "mild")
                     .Add(1, "That early. You said the words over him yourself, " + a + ".", "mild")
                     .Add(0, "I did. I'd forgot I did.", "mild")
                     .Add(1, "You said them well. I'll give you that. Then you snored.", "cheer");
                    break;
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
                     .Add(1, (InnerLife.ClockYear >= 1905 ? "Near forty years" : "Thirty years and more") + " she's dined out on that story, " + a + ".", "mild")   // from '68: 1899 or 1907
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
