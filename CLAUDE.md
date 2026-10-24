# CLAUDE.md

Notes for working on this repo with Claude Code.

## What this is

enRAGE Westworld: an unofficial C# add-on for the enRAGE nemesis mod for Red Dead Redemption 2. It is a separate DLL (`EnrageWestworld.dll`) that sits next to enRAGE's `NemesisMod.dll` in the game's `scripts` folder. Story Mode only. Owner: Lex (GitHub ninjaalex888). Read `README.md` for features and `CHANGELOG.md` for history. The full design doc is the project doc `westworld-addon-design.md` in the claude.ai Project "Red Dead Redemption 2 Mods".

## Build

Stack: Script Hook RDR2 + Script Hook RDR2 .NET V2, net48, x64, **C# 7.3** (no newer language features).

```
cd addon/EnrageWestworld
dotnet build -c Release
```

Needs `refs/ScriptHookRDRNetAPI.dll` and `original/NemesisMod.dll`, both from Lex's own install and never committed (`*.dll` is git-ignored). Output: `addon/EnrageWestworld/bin/Release/net48/EnrageWestworld.dll`. Copy it into the game's `scripts` folder to test.

## Layout

- `Core/` is plain C# with no game calls: people, rumours, letters, diaries, scenes, all the lines (`Voice`, `Grammar`, `Settings`, `Store`).
- `Game/` talks to the game and to enRAGE (`WestworldScript.*.cs`, `Natives.cs`, `Spawner.cs`, `Stalker.cs`, `Bridge`).
- `addon/EnrageWestworld/package/README.txt` is the player-facing readme that goes in the zip (CRLF line endings in the zip).
- `release/` holds the download zip, because the README links to it. One zip at a time.

## Rules that matter

- **No enRAGE code, text or files in this repo.** The add-on reads what enRAGE tracks and changes things only through enRAGE's own methods. Every line of dialogue must be original; do not copy or lightly reword enRAGE's strings. Never commit decompiled enRAGE source.
- If an enRAGE update breaks something, only the feature that needs it should switch off, and the log should say which.
- Never run script mods in Red Dead Online.
- Writing style for all player-facing text: plain, specific, no filler.
- Settings: add a field in `Settings.cs`, a line in `ToIni()`, clamp it in `Clamp()`. Bump `CurrentVersion` when adding options so old inis are rewritten with the new lines (values kept).
- Native out-parameters use 8-byte slots.

## Testing

There is no way to run the game in CI. Play-test in game and read the log at `Documents\Rockstar Games\Red Dead Redemption 2\enRAGE-Westworld\enRAGE-Westworld.log`. Hotkeys: F7 (status and log dump), Shift+F7 (call a stalker now), G (wake the person you face one stage; `WakeKey`), F6 (flirt), F5 (letters and diaries).

## Stalkers (current design, 0.5.4)

- Stalker "days" run 05:00 to 05:00. Each day rolls once: none (100 - `StalkerChance`), else day only / night only / both by the weights `StalkerDayOnly` / `StalkerNightOnly` / `StalkerBoth`.
- Pending stalkers are kept as slots (`StalkSlot`: test, day, night) in `WestworldScript.Hosts.cs`. A due stalker waits for a clear moment (outdoors, no story mission, no lawmen, not fighting, not in water, screen not fading). A day slot is dropped at 18:00; a night slot carries over to the next night.
- Spawn spots: `GET_SAFE_COORD_FOR_PED` always returns false in RDR2 (seen in game, 0.5.5), so `Natives.StandSpot` falls back to `GET_GROUND_Z_FOR_3D_COORD` (from the player's height first, never inside an interior).
- Behaviour (0.5.5): follow, stare, freeze when watched, hide. Player still for 20 s (no movement or move input; the camera is ignored) -> Circle at 6-10 m; any movement -> Retreat to a hidden spot 45-70 m out (`CoverSpot`), then Lurk. Lasts `StalkerMinutes` (default 120).
- Failed spawns retry after 1, 2, 4, 8, then 10 minutes. The log says what a stalker is waiting for (`STALKER due, waiting: ...`).
- Not yet confirmed in game: stalkers, and most of the 0.5 street scenes.

## Releasing

1. Update the version in `EnrageWestworld.csproj`, the title and "NEW IN" section of `package/README.txt`, `README.md` and `CHANGELOG.md`.
2. Build, then zip `EnrageWestworld.dll` and `README.txt` as `release/enRAGE-Westworld-<version>.zip`, replacing the old zip, and update the link in `README.md`.
3. Commit and push. A GitHub Release with the zip attached is nicer than the folder link if `gh` can make one.

## Roadmap

1. Everyone is someone (residents: every ordinary person gets an identity, saved once touched; never written to enRAGE). 2. The Awakening (hosts slowly realise they're in a loop; it spreads host to host, a trigger line, they notice contradictions such as the revived dead, the ticker counts them). 3. Memory that behaves like memory (fading and rewriting, places hold memories, recognised by horse, clothes and habits). 4. Inner lives (opinions of each other, dreams from real memories, changing their minds). 5. Interaction menu. 6. Spreading saloon brawls. 7. Answering letters. 8. Narratives. 9. Town events. 10. New arrivals. 11. Natural voices (Piper). 12. Stalkers on horseback and revenants at camp. 13. The game's real words in subtitles (needs the speech audio exported and transcribed offline once; the plumbing exists: `voices\banks.txt`, `voices\words. 14. Far future, optional: a local language model writing extra inner thoughts in the background from a person's memories (never required; the written lines come first).txt`, `Core/VoiceBook.cs`). Ask Lex before adding anything not on this list. (Everyone is someone, added 2026-10-24 at Lex's request, is the foundation: residents in `Core/Residents.cs`, enRAGE records stay separate.)

## The Awakening (0.6 work in progress)

- State: `PersonState.WakeStage` (0 asleep, 1 stirring, 2 questioning, 3 unravelling, 4 awake) and `WakeStageAt`. `Hosts.Awareness` never drops below `Awakening.Floor(stage)`, so every existing awareness site follows; `Hosts.Aloud` and `Hosts.Thought` swap in the stage lines (`Core/Awakening.cs`) most of the time, and `DayEntry` adds a secret diary entry from stage 2. No second source of host lines.
- `Engine.AwakeningTick` (daily, `Core/Engine.Awakening.cs`) picks `AwakeningShare` percent of the living (not `Lore`), leaning toward high awareness, and advances stages every `AwakeningDays`.
- Done: slice (a) above, and (b) on-screen acts (`Game/WestworldScript.WakeActs.cs`: glance, stare, freeze, pace, seek; only residents and enRAGE townsfolk, never full records, which enRAGE tasks itself; TASK natives, events blocked while directed, cleared after). Residents (`Core/Residents.cs`) make this reach the whole world. Next: (b2) wrong hour/wrong place behaviours and acts for enRAGE records once their tasking is understood, then (c) spreading, the trigger line, contradictions (the revived dead), breaking script with enRAGE.
- Before shipping new dialogue run the overlap checks. `python` is not installed on this PC, so port them to C# or install Python first.

## Beats and inner lives

- `Core/WakeBeats.cs`: lines that belong to an act (opening thought per act and stage, a thought each time a pacer reaches the end of the path with the real count, a sentence that stops halfway for a freeze). `Game/WestworldScript.WakeActs.cs` calls them as acts start and step (`Think`, `Feed.Mutter`). Add a new act by adding its lines in WakeBeats and its steps in WakeActs together.
- `Core/InnerLife.cs`: ordinary people's thoughts by trade group, hour, temperament, town and faith; reality musings, rumours and fame comments still come first (`Engine.ResidentThought`). Repeats are tracked in `PersonState.Musings` for saved residents.
- An optional local-language-model layer for inner voices is on the roadmap (item 14, far off, never required).

## Wake acts (0.5.5)

- Identity vs direction: `ResidentEligible` (identity) accepts any adult human incl. soldiers, lawmen, script-owned; `CanDirect` decides movement. enRAGE records and script-owned peds only get passive acts (look-at, ticker thought, voice; no task clears). The army models are enRAGE gang models, so script-owned soldiers are probably enRAGE extras.
- Acts live in `WestworldScript.WakeActs.cs`; each act's words are in `Core/WakeBeats.cs`. Awake (stage 4) is a state: approach, talk, then an ending (plead, escape, wake someone, kneel, or the rare despair). `WakeDemo` (G) ends the current act and starts the new stage's.
- Despair: `AwakeningDespair` percent, rolled once per awake person (`despair:rolled` in `PersonState.Told`), residents only, free ambient peds only, armed only (`IS_PED_ARMED(h, 4)`); the bullet's owner is the ped itself so `Watch.cs` (which only counts events whose attacker is the player) books nothing as the player's. Unarmed -> kneel. Knife / head-against-wall endings are NOT built (no verified animations).

## Writing brief for thoughts

Lex's brief is saved at `C:\mods\prompts\You are an expert Behavioral Psycho.txt` (outside the repo). Inner-life lines follow it: short fragmented sentences, one sensory detail, period vocabulary, and the frames: immediate bodily needs, status anxiety (the looking-glass self), negativity and loss bias and the just-world excuse, the gap between the outside and the inside, 1899 anxieties, and granular observation of the player. Pools live in `Core/InnerLife.cs`.
