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

There is no way to run the game in CI. Play-test in game and read the log at `Documents\Rockstar Games\Red Dead Redemption 2\enRAGE-Westworld\enRAGE-Westworld.log`. Hotkeys: F7 (status and log dump), Shift+F7 (call a stalker now), F6 (flirt), F5 (letters and diaries).

## Stalkers (current design, 0.5.4)

- Stalker "days" run 05:00 to 05:00. Each day rolls once: none (100 - `StalkerChance`), else day only / night only / both by the weights `StalkerDayOnly` / `StalkerNightOnly` / `StalkerBoth`.
- Pending stalkers are kept as slots (`StalkSlot`: test, day, night) in `WestworldScript.Hosts.cs`. A due stalker waits for a clear moment (outdoors, no story mission, no lawmen, not fighting, not in water, screen not fading). A day slot is dropped at 18:00; a night slot carries over to the next night.
- Failed spawns retry after 1, 2, 4, 8, then 10 minutes. The log says what a stalker is waiting for (`STALKER due, waiting: ...`).
- Not yet confirmed in game: stalkers, and most of the 0.5 street scenes.

## Releasing

1. Update the version in `EnrageWestworld.csproj`, the title and "NEW IN" section of `package/README.txt`, `README.md` and `CHANGELOG.md`.
2. Build, then zip `EnrageWestworld.dll` and `README.txt` as `release/enRAGE-Westworld-<version>.zip`, replacing the old zip, and update the link in `README.md`.
3. Commit and push. A GitHub Release with the zip attached is nicer than the folder link if `gh` can make one.

## Roadmap

1. The Awakening (hosts slowly realise they're in a loop). 2. Interaction menu. 3. Spreading saloon brawls. 4. Answering letters. 5. Narratives. 6. Town events. 7. New arrivals. 8. Natural voices (Piper). 9. Stalkers on horseback and revenants at camp. Ask Lex before adding anything not on this list.
