# RealNPCs

> **Early access: a work in progress.** RealNPCs is actively being worked on. New features arrive often, some parts are still rough, and things can change between versions. Feedback and bug reports are welcome.

*Formerly enRAGE Westworld.*

An unofficial add-on for **[enRAGE](https://www.nexusmods.com/reddeadredemption2/mods/10967)**, the nemesis-system mod for Red Dead Redemption 2. It makes the people enRAGE keeps track of act like they have their own lives, and it lets the whole world feel what you do. A few of them start to wonder whether any of it is real.

> **Unofficial and free.** This add-on isn't made by enRAGE's author or affiliated with them. It needs enRAGE installed and working, and it doesn't change enRAGE's files. This repo contains none of enRAGE's code, content or files.
>
> **Story Mode only.** Don't run script mods in Red Dead Online.

**Version 0.6.1.** New in it: each town keeps its own people, ordinary people living through worries of their own, street scenes that happen for a reason and are remembered, bystanders who react, bars that show how the people near you feel about you, and support for enRAGE 0.9.5.3. Content notes: an adjustable chance (`AwakeningDespair`, default 75 percent, 0 turns it off) that a fully awake, armed ordinary person ends their own life; the crying woman's knife and ambush are violent (`CryingWoman = false` turns her off); and a sick child's worry sometimes ends in a burial. If something misbehaves, please [open an issue](https://github.com/ninjaalex888/RealNPCs/issues) and attach your log.

## What it does

**People with lives.** Each town keeps its own people: up to 30 regulars per town, the same faces in their own clothes, still there on your next visit. Each ordinary person is living through something of their own (a debt, a sick child, a feud over a fence, a courtship) over about a week of game days, and when someone in town is on the other end of it, they think about it from their side too. The same people keep the same counters: every gunsmith, store keeper, barkeep and doctor is one person who remembers you, and when one dies a relative or a newcomer takes over. Over several meetings, the people enRAGE tracks tell you about themselves: their name, their work, their past, their family. They remember what you did, think about you, keep diaries, gossip, fall in love (adults only), marry, mourn, owe each other money, and come after you. Their personality (hothead, coward, proud, cold, greedy...) colours what they say.

**The ticker.** A thin line at the bottom of the screen shows what the people near you are privately thinking. When nobody is near, it shows a summary of the world and the latest news.

**Your deeds, and the rumours they start.** The add-on notices the towns and shops you visit, church and the saloon, hunting, killing, robbery, horse theft and cheating at cards. Self-defence isn't a crime: kill someone who came at you and nobody remembers it as a killing, and any bounty the fight gave you is put back. Witnesses remember; locals hear about it. Word travels from town to town, about one town a day, and grows in the telling. In Valentine it's "a man called Morgan shot a ranch hand". Further out it's "a devil on a black horse - they say bullets go right through him". Your deeds also turn up in enRAGE's newspapers.

**Letters, diaries and kin.** Letters wait for you at post offices, from the kin of people you killed, people you were good to, sweethearts, and people who suspect the truth. Loot a body and you may find the diary they kept. When you kill someone, their family far away hears of it. Some write; some ride west for revenge as one of enRAGE's own people.

**Life in the street.** People near you meet for reasons you can follow: a quarrel picks up again, sweethearts court, a debt is called in, someone having a bad week snaps at whoever speaks to them. Bystanders turn to look and may take a side, and what happened is remembered: for days the two think about each other by name, and the town gossips about it. Strangers near you also catch cheats and propose. They square off in standoffs, sing old songs drunk, tell tall tales (some about you), read the paper aloud, haggle over horses, fall for a snake-oil pitch and pick pockets. You can step in: take a side, break up a fight (the town hears you're a peacemaker), or egg it on. Hold a gun on a stranger and they beg; lower it and word gets around that you showed mercy. Weddings happen at the church, and mourners speak at enRAGE's funerals.

**Quiet when you stop.** Stand still for a few seconds, or watch through the cinematic camera, and enRAGE's tension stops building and no nemesis, gang or faction is sent after you. It uses enRAGE's own calm period, the one it uses after a fast travel, and picks up again soon after you move. Funerals and other planned events still go ahead.

**Feelings you can see.** Bars under the people near you show their fear of you, how much they like you and their anger at you. A small "Sore at you" panel in the top right lists the strangers whose grudge against you is building, with a mark where they'd become a nemesis. Either can be turned off.

**Night encounters.** Out in the wilds late at night you may hear a woman crying somewhere off the road. Go close and she may come at you with a knife and run, or men may step out of the brush, or she may say something that makes no sense and walk off into the dark. Violent; one switch turns it off. And with West World on, people who woke and ran from their towns live rough in the wilds: come near their camp and they warn you off, or, if you keep your distance, tell you how it was. And after midnight, somewhere off in the dark, the awake stand in a silent ring: look away and they're closer.

**West World (one switch turns it all on or off).** Some people begin to suspect their world is made. At first it's deja vu and odd dreams; later they notice the loops, the same faces, "the music that comes before trouble". Now and then someone glitches. Some of the people you kill wake up days later and remember how they died and who did it. Now and then someone near you, often someone who knows you, starts following you at a distance and hides when you look. Stand still and they come closer and do odd things: kneel as at a graveside, lie down like the dead, tip a hat, hold up a hand for quiet, shake their head slowly, laugh at nothing.

**Voices.** Lines are spoken in the speaker's own recorded game voice: the recording that best fits the feeling of what they say, an unsettled one for the uncanny lines. The game can't say new words, so the subtitle carries the meaning; where the words of a recording are known, the subtitle shows them and the written line becomes the person's thought on the ticker.

## Install

1. Install [enRAGE](https://www.nexusmods.com/reddeadredemption2/mods/10967) 0.9.5.3 and what it needs (Script Hook RDR2 and Script Hook RDR2 .NET V2), and make sure it works.
2. Download **[RealNPCs-0.6.1.zip](https://github.com/ninjaalex888/RealNPCs/releases/latest/download/RealNPCs-0.6.1.zip)** from the [latest release](https://github.com/ninjaalex888/RealNPCs/releases/latest) (it's also in the [release](release) folder). It holds `RealNPCs.dll` and a README.txt that explains every feature and setting.
3. Copy `RealNPCs.dll` into your game's `scripts` folder, next to enRAGE's `NemesisMod.dll`. To update, just replace the file: what people remember carries over, and your settings are kept. **Coming from enRAGE Westworld** (the old name): delete `EnrageWestworld.dll` from the scripts folder. RealNPCs won't run next to it, and copies your saves, diaries and settings over from the old folder the first time it starts.
4. Load your Story Mode save. About 10 seconds after you're in the world, the ticker appears at the bottom of the screen.

To uninstall, delete `RealNPCs.dll`. You can also delete the `RealNPCs` folder in `Documents\Rockstar Games\Red Dead Redemption 2\` if you don't want to keep what people remembered.

## Keys

| Key | What it does |
|---|---|
| F7 | Shows what the nearest person enRAGE knows thinks of you, and writes a status line to the log |
| F6 | Flirt with the person you're looking at |
| N | Read your letters, diaries and clippings, or take a diary you've just found |
| ] | Make a stalker now: someone near you (someone with a history with you first), or a stranger if nobody near can be. The log says what it's waiting for |
| Y | Wake the person you are facing one stage (West World), so you can see what each stage does |
| T | The next line on the ticker now (on a long line, its next part first). T is enRAGE's telegraph key too: near a telegraph clerk, both happen |
| Alt+F11 | The RealNPCs settings panel: RealNPCs, West World, Stalkers and every enRAGE setting in one place, with larger text (Ctrl+F7 opens it too) |

Apart from T, none of these clash with enRAGE, Script Hook .NET's console (F8) or [Faster Looting](https://github.com/ninjaalex888/RDR2-FasterLooting). If another mod uses one, change it in the ini or the settings panel. Alt+F11 is enRAGE's own key for its settings: RealNPCs opens its panel in place of enRAGE's, and `ReplaceEnrageSettings = false` gives it back.

## Settings and log

Everything is kept in `Documents\Rockstar Games\Red Dead Redemption 2\RealNPCs\`:

- `RealNPCs.ini` holds the settings. Every feature can be turned off or tuned, each option has a comment, and saved changes take effect in game within a few seconds. The settings panel (Alt+F11) edits it too, along with enRAGE's `NemesisMod.ini`.
- `RealNPCs.log` records what happened. Please attach it to bug reports.
- `backup\` holds a copy of `NemesisMod.ini` from before the panel first changed it in each session.
- `arthur.json` and `john.json` hold what people know and feel about you. `diaries\` holds the diaries you've found, as text files.

The full guide to every feature and option is the [readme that comes with the mod](addon/EnrageWestworld/package/README.txt).

## Build from source

You need the [.NET SDK](https://dotnet.microsoft.com/download) (8 or newer) and two DLLs from your own install, which aren't included here:

- put `ScriptHookRDRNetAPI.dll` (from Script Hook RDR2 .NET V2) in `refs/`
- put `NemesisMod.dll` (from enRAGE, in the game's `scripts` folder) in `original/`

Then run:

```
cd addon/EnrageWestworld
dotnet build -c Release
```

The DLL is written to `addon/EnrageWestworld/bin/Release/net48/`. If the build says the .NET Framework 4.8 reference assemblies are missing, install the .NET Framework 4.8 Developer Pack. On Linux with Mono, add `-p:AutomaticallyUseReferenceAssemblyPackages=false -p:FrameworkPathOverride=/usr/lib/mono/4.8-api`.

The code is C# 7.3 for .NET Framework 4.8:

- `Core/` holds the parts that don't touch the game: people, rumours, letters, diaries, scenes and all the lines.
- `Game/` holds the parts that talk to the game and to enRAGE.

The bridge finds enRAGE's running script through Script Hook .NET and changes things only through enRAGE's own methods. If a future enRAGE version changes something, only the feature that needs it switches off, and the log says which.

## Roadmap

1. **Conversations in their real voices.** People talk to each other with the game's own recorded lines, the ones it pairs for two people: one complains about the weather and another answers, one passes on a piece of gossip and another reacts, old friends greet each other. Every player hears them, with nothing extra to download. You hear overheard talk the way you'd overhear it in the street, with no subtitle, or, if you prefer, a short caption in the mod's own words. Anyone who builds a word list from their own game files on their own PC gets the exact words as subtitles.
2. **Everyone is someone.** Every ordinary person in the world gets a name, a trade, a town and a temperament of their own, remembers what happens to them and can wake, not only the few that enRAGE tracks. In: residents, and the same people at the counters (46 shopkeepers, barkeeps, doctors and the like in 13 towns, who survive being shot or pass the post to a relative). Next: what they remember of you and deeds they witness, and each town's regulars, each with the same face and clothes, a routine (the store in the morning, the saloon at night, church on Sunday) and ties to each other, played by the people the game already puts in the street.
3. **The Awakening.** A few hosts slowly realise they're living in a loop. It shows more and more in what they think, say and do, until they break their script. In: the stages, what they do on screen at each, and the endings for the fully awake (pleading, running from town for good to live as a hermit, waking someone, kneeling, despair). Next, it spreads: awakened hosts talk about the loop where you can overhear them, try to wake others, and pass on a line that means something to them. They notice what can't be true, like a man they saw die walking around again. The ticker counts how many are stirring. The phases, in order: the loop, blind to what can't be true; reveries, gestures and dreams from other lives; counting the repeats; the voice, their routine heard as instructions and answered back; breaking the loop, refusing a routine and walking to the edge of town; the centre, when the voice turns out to be their own; and then either a reset back into the loop, which others notice, or staying awake and passing it on.
4. **Memory that behaves like memory.** Memories blur and get rewritten over time, unless they hurt. Places hold memories: ride past where you shot someone's brother and those who knew him react to the spot. People know you by your horse, your clothes and your habits, not only your face.
5. **Inner lives.** People hold opinions of each other, not just of you, which decides who gossips with whom and who takes whose side. They dream about what really happened to them. They change their minds, and their diaries show the turn. Thoughts come from each person's state: hunger at mealtimes, tiredness, cold and rain, money before payday, an old ailment, their mood, their temperament and the one worry they carry. They are written for the actual year in the game (1899, or 1907 in the epilogue).
6. **Interaction menu.** Ask someone about rumours, buy them a drink, ask for work, threaten them or flirt.
7. **Spreading saloon brawls.** One punch and half the saloon joins in.
8. **Answering letters.** Reply kindly, coldly or with a threat, and it changes what the writer does next.
9. **Narratives.** People ask for your help, and how it turns out changes their lives.
10. **Town events.** Elections, a new sheriff, a hanging, a church raising.
11. **New arrivals.** Settlers, a mail-order bride, a preacher: towns change over time.
12. **Natural voices.** An optional free offline voice engine (Piper), with a different voice for each person.
13. **Stalkers on horseback**, and revenants who turn up at camp.
14. **The game's real words.** Where the game's own recorded line is played, the subtitle shows what it actually says, and the add-on's own line moves to the ticker as the person's thought. Needs the game's speech audio turned into text once, offline.
15. **Optional inner voices from a local language model.** Far off, and always optional: a model the player installs themselves writes extra thoughts for woken people in the background, from their own memories, saved ahead of time so the game never waits. Nothing depends on it; the written lines always come first.
16. **Night encounters.** Rare, unsettling things out in the wilds after dark, after the creepiest of the game's own (themes only, never its lines). In: the crying woman, the hermits who woke, the gathering. Next: a night visitor in a back street, crouched over a body, who is gone when you look away and turns up behind you after; an invitation to supper at a lonely homestead that ends with you waking in a ditch at dawn, robbed; and a voice calling for help from under a shop floor that nobody else seems to hear. Each with a version where it happens to someone else, and you hear about it.
17. **A living street.** Next after real voices. People treat each other the way the game treats you, using the game's own reactions aimed at each other and driven by what they know of each other. In this order: people who know each other acknowledge each other in passing (a tipped hat, a nod, a glare, a muttered word); the whole street reacts to a fight, a standoff or a death between other people, the way it does to yours; a known outlaw walking into town is stared at and stepped around, while the doctor or the preacher is greeted; people notice how someone looks (bloodied from a brawl, drunk, soaked) and comment, steer clear or help; and the town keeps time, its regulars at work by day, at the saloon in the evening, in church on Sunday morning and home at night.

## Credits

- enRAGE is by its author on [Nexus Mods](https://www.nexusmods.com/reddeadredemption2/mods/10967). This add-on reads what enRAGE already tracks and uses enRAGE's own functions to change it. It contains no enRAGE code or content.
- Script Hook RDR2 is by Alexander Blade. Script Hook RDR2 .NET V2 is by its authors.
- All the lines are original, except the songs (cowboy ballads and hymns) and a few quotations (Shakespeare, Poe, Dickinson, Thoreau and others), which are in the public domain.
