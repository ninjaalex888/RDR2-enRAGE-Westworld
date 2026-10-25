# RealNPCs

*Formerly enRAGE Westworld.*

An unofficial add-on for **[enRAGE](https://www.nexusmods.com/reddeadredemption2/mods/10967)**, the nemesis-system mod for Red Dead Redemption 2. It makes the people enRAGE keeps track of act like they have their own lives, and it lets the whole world feel what you do. A few of them start to wonder whether any of it is real.

> **Unofficial and free.** This add-on isn't made by enRAGE's author or affiliated with them. It needs enRAGE installed and working, and it doesn't change enRAGE's files. This repo contains none of enRAGE's code, content or files.
>
> **Story Mode only.** Don't run script mods in Red Dead Online.

**Version 0.5.5.** Stable in play so far. Stalkers, the posse scene, the wake key and the Awakening's first stages have been seen in game; the newest parts (the fuller wake acts, voices matched to feeling, the gang news pop-ups) have had only a short test. One content note: an adjustable chance (`AwakeningDespair`, default 75 percent, 0 turns it off) that a fully awake, armed ordinary person ends their own life. If something misbehaves, please [open an issue](https://github.com/ninjaalex888/RDR2-enRAGE-Westworld/issues) and attach your log.

## What it does

**People with lives.** Over several meetings, the people enRAGE tracks tell you about themselves: their name, their work, their past, their family. They remember what you did, think about you, keep diaries, gossip, fall in love (adults only), marry, mourn, owe each other money, and come after you. Their personality (hothead, coward, proud, cold, greedy...) colours what they say.

**The ticker.** A thin line at the bottom of the screen shows what the people near you are privately thinking. When nobody is near, it shows a summary of the world and the latest news.

**Your deeds, and the rumours they start.** The add-on notices the towns and shops you visit, church and the saloon, hunting, killing, robbery, horse theft and cheating at cards. Witnesses remember; locals hear about it. Word travels from town to town, about one town a day, and grows in the telling. In Valentine it's "a man called Morgan shot a ranch hand". Further out it's "a devil on a black horse - they say bullets go right through him". Your deeds also turn up in enRAGE's newspapers.

**Letters, diaries and kin.** Letters wait for you at post offices, from the kin of people you killed, people you were good to, sweethearts, and people who suspect the truth. Loot a body and you may find the diary they kept. When you kill someone, their family far away hears of it. Some write; some ride west for revenge as one of enRAGE's own people.

**Life in the street.** Strangers near you flirt, quarrel, call in debts, catch cheats and propose. They square off in standoffs, sing old songs drunk, tell tall tales (some about you), read the paper aloud, haggle over horses, fall for a snake-oil pitch and pick pockets. You can step in: take a side, break up a fight (the town hears you're a peacemaker), or egg it on. Hold a gun on a stranger and they beg; lower it and word gets around that you showed mercy. Weddings happen at the church, and mourners speak at enRAGE's funerals.

**West World (one switch turns it all on or off).** Some people begin to suspect their world is made. At first it's deja vu and odd dreams; later they notice the loops, the same faces, "the music that comes before trouble". Now and then someone glitches. Some of the people you kill wake up days later and remember how they died and who did it. Now and then someone near you, often someone who knows you, starts following you at a distance and hides when you look. Stand still and they come closer and do odd things: dig a grave, kneel as at a graveside, put a finger to their lips, wave slowly, beckon.

**Voices.** Most lines come with a fitting recorded line in the speaker's own in-game voice. The stranger lines are read aloud by any Windows voice you have installed, and each speaker keeps their own voice, pitch and pace.

## Install

1. Install [enRAGE](https://www.nexusmods.com/reddeadredemption2/mods/10967) 0.9.5.2 and what it needs (Script Hook RDR2 and Script Hook RDR2 .NET V2), and make sure it works.
2. Download **[enRAGE-Westworld-0.5.5.zip](https://github.com/ninjaalex888/RDR2-enRAGE-Westworld/raw/main/release/enRAGE-Westworld-0.5.5.zip)** (it's also in the [release](release) folder). It holds `EnrageWestworld.dll` and a README.txt that explains every feature and setting.
3. Copy `RealNPCs.dll` into your game's `scripts` folder, next to enRAGE's `NemesisMod.dll`. To update, just replace the file: what people remember carries over, and your settings are kept. **Coming from enRAGE Westworld** (the old name): delete `EnrageWestworld.dll` from the scripts folder. RealNPCs won't run next to it, and copies your saves, diaries and settings over from the old folder the first time it starts.
4. Load your Story Mode save. About 10 seconds after you're in the world, the ticker appears at the bottom of the screen.

To uninstall, delete `RealNPCs.dll`. You can also delete the `RealNPCs` folder in `Documents\Rockstar Games\Red Dead Redemption 2\` if you don't want to keep what people remembered.

## Keys

| Key | What it does |
|---|---|
| F7 | Shows what the nearest person enRAGE knows thinks of you, and writes a status line to the log |
| F6 | Flirt with the person you're looking at |
| F5 | Read your letters, diaries and clippings, or take a diary you've just found |
| 0 | Make a stalker now: someone near you (someone with a history with you first), or a stranger if nobody near can be. The log says what it's waiting for |
| G | Wake the person you are facing one stage (West World), so you can see what each stage does |
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

1. **Everyone is someone.** Every ordinary person in the world gets a name, a trade, a town and a temperament of their own, remembers what happens to them and can wake, not only the few that enRAGE tracks. The first part is in; what they remember of you, and deeds they witness, come next.
2. **The Awakening.** A few hosts slowly realise they're living in a loop. It shows more and more in what they think, say and do, until they break their script. It spreads: awakened hosts talk about the loop where you can overhear them, try to wake others, and pass on a line that means something to them. They notice what can't be true, like a man they saw die walking around again. The ticker counts how many are stirring.
3. **Memory that behaves like memory.** Memories blur and get rewritten over time, unless they hurt. Places hold memories: ride past where you shot someone's brother and those who knew him react to the spot. People know you by your horse, your clothes and your habits, not only your face.
4. **Inner lives.** People hold opinions of each other, not just of you, which decides who gossips with whom and who takes whose side. They dream about what really happened to them. They change their minds, and their diaries show the turn.
5. **Interaction menu.** Ask someone about rumours, buy them a drink, ask for work, threaten them or flirt.
6. **Spreading saloon brawls.** One punch and half the saloon joins in.
7. **Answering letters.** Reply kindly, coldly or with a threat, and it changes what the writer does next.
8. **Narratives.** People ask for your help, and how it turns out changes their lives.
9. **Town events.** Elections, a new sheriff, a hanging, a church raising.
10. **New arrivals.** Settlers, a mail-order bride, a preacher: towns change over time.
11. **Natural voices.** An optional free offline voice engine (Piper), with a different voice for each person.
12. **Stalkers on horseback**, and revenants who turn up at camp.
13. **The game's real words.** Where the game's own recorded line is played, the subtitle shows what it actually says, and the add-on's own line moves to the ticker as the person's thought. Needs the game's speech audio turned into text once, offline.
14. **Optional inner voices from a local language model.** Far off, and always optional: a model the player installs themselves writes extra thoughts for woken people in the background, from their own memories, saved ahead of time so the game never waits. Nothing depends on it; the written lines always come first.

## Credits

- enRAGE is by its author on [Nexus Mods](https://www.nexusmods.com/reddeadredemption2/mods/10967). This add-on reads what enRAGE already tracks and uses enRAGE's own functions to change it. It contains no enRAGE code or content.
- Script Hook RDR2 is by Alexander Blade. Script Hook RDR2 .NET V2 is by its authors.
- All the lines are original, except the songs (cowboy ballads and hymns) and a few quotations (Shakespeare, Poe, Dickinson, Thoreau and others), which are in the public domain.
