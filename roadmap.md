# RealNPCs roadmap and Claude Code prompts

Oct 26, 2026 · lexthestoic

## Status at 0.6.2 (2026-10-26)

This plan was written overnight against 0.6.1. Since then 0.6.2 shipped and a few decisions changed what's below; where this block and a prompt disagree, this block wins.

**Done since:**

- 0.6.2 is released (Q7). It brought subtitles drawn the game's way and only for lines said to you, conversations in the game's real voices, trouble between people (rough handling, pickpockets, drunks, begging, keeping clear, a friend's kindness), reactions to what's said and done, NPCs courting each other, Flirt in the lock-on menu, despair by a gun that fits them, a knife or a wall, thoughts above heads, and F7's map marks for regulars and the awake.
- enRAGE's records now join in unless enRAGE is driving them (Lex, 2026-10-26): one check, `MayDirect` (`Core/Directing.cs`, `Game/WestworldScript.Directing.cs`), covers story models, children, missions and enRAGE's busy people. That is most of P27, and the first half of P28.

**Superseded or out of date below:**

- Wherever a prompt says `Bridge.RecordAt(handle) == null` or that records get only the passive part, use `MayDirect` instead. The exception is anything that changes how someone looks (Q5's blood and wetness): records stay out of that, since enRAGE knows them by their look.
- What's left of P27: a lease that re-checks each tick and lets go when a higher owner appears, the `_GET_ENTITY_SCRIPT` probe, and a "who steers whom" note in CLAUDE.md. P28's "rising into enRAGE" belongs with the mind design's sharing back (below).
- Weather and money are read now. The weather type comes from `_GET_PREV_WEATHER_TYPE_HASH_NAME` (`Engine.NowWeather`), and the cash balance from `_MONEY_GET_CASH_BALANCE`, in cents (`Engine.PlayerCash`). Rain strength and the cash decrement still need their probes.
- The flirt key is F2, and Flirt is in the lock-on menu on H (`FlirtPrompt`). That prompt route is proven in game and is a better opener for the interaction menu than a double greet.
- The living street is roadmap item 17, not 16.
- The check baselines are now fuzzy 198 and overlap 113 shared code lines, plus the accepted `'_worldCatchingUp'`.
- The session rules below predate Lex's current task rules; where they differ (the records rule above), the current ones win.
- The NPC mind design (2026-10-26; a private Claude Doc, "RealNPCs: the NPC Mind") is meant to be the foundation for reactions: one people ledger with our own death ledger, a layered mind (traits, life of the time, needs, worries, feelings, moods, memory, ties, beliefs), and one path from what happens to what shows (appraisal, emotion, urge, 1899's display rules, a catalogue of what the game can do). Q2 to Q5's reactions would sit on that catalogue. It also makes the language model "full mode", needing a GPU, not optional (roadmap item 15).

**Open for Lex:**

- [x] The despair default (gap 4, P8 step 8): stays at 75 (Lex, 2026-10-26). Instead, more animations for what a fully awake (stage 4) person might do.
- [x] Order of work: (b), the mind design's steps 1 to 5 first (the ledger, traits, needs, appraisal and emotion, the catalogue), then the living street (Q2 to Q6) built on them (Lex, 2026-10-26).
- [x] A LICENSE (gap 8): none, all rights reserved, so it can be opened later; the notice ends both readmes (Lex, 2026-10-26).
- [x] Direct writes into enRAGE (gap 5, P8 step 7): audited 2026-10-26. Eight direct writes, each now followed by enRAGE's own save and failing loudly; the list and the rule are in CLAUDE.md ("Writing to enRAGE"), and the README no longer says "only through its own methods".
- [ ] Despair with a gun or a knife plays no act of its own (Lex, 2026-10-26, after watching it): the person stands, then dies. P29 built the same night (look down, draw, aim at the ground, the shot from the hand; the knife's set asked for early, a writhe after); not yet seen in game, and still no gun to the head, which the game has no animation for. The run at a wall works as meant (seen: Gabriel Ridley, 17:45:57). The game has no self-harm animation (searched anims.lua), so the fix is pieced together; see P29.
- [ ] The Want, Maybe and No columns in the idea banks.

**Still true at 0.6.2:** a feature's error counter never resets; `Store.People` has no cap; the three big files grew (Engine.cs 2,049 lines, WestworldScript.cs 1,561, Engine.World.cs 1,213); there's no LICENSE file.

## Where RealNPCs stands

RealNPCs 0.6.1 is now a whole-world simulation of about 30,000 lines of C# (106 files, about 120 settings), and its biggest gap is proof, not features. The newest layers are seen in game, but the night encounters, the hermits and the gathering have never been seen, and nothing can summon them on demand.

Since 0.5.4 (the last version in the Project's design notes) it gained a new name, a settings panel, quiet-when-still, self-defence, counter keepers, town regulars, week-long worries with named people at the other end, scenes that happen for a reason and leave ties, bystanders, feeling bars, the Awakening on screen and three night encounters.

| System | What a player sees now | State | Evidence |
| --- | --- | --- | --- |
| Ticker, deeds, rumours, papers, letters, kin | Thoughts and news on one line; deeds travel and bend from town to town | Seen in game | 0.4 and 0.5.1 logs |
| Inner lives and worries | 13 kinds of week-long worry (debt, fever, feud, courting...), with named people at the other end | Seen in game | 0.6.1 changelog |
| Feeling bars and grudge meter | Fear, liking and anger over heads; the "Sore at you" panel | Seen in game | 0.6.1 changelog |
| Town regulars | Up to 30 per town, same faces and clothes on your next visit | Partly seen | Seen in Valentine; whether the face comes back with the outfit is open |
| Ties between people | Scenes leave warm or hostile ties; thoughts by name for 4 days | Partly seen | TIE lines in the log; the thoughts not yet seen on screen |
| Reason-driven scenes and bystanders | Debts called in, feuds, courtships, a bystander calls out | Partly seen | A bystander bug was seen in game, so they play |
| Stalkers | Follow, hide, eerie acts when you stand still | Partly seen | Following, circling and gestures seen; watch and rush modes not |
| Awakening on screen | Glances, pacing, walking up to you, the endings | Partly seen | Defaults retuned from play-testing in 0.6.0 |
| Counter keepers | 46 counter keepers who survive or pass the post on | Not recorded | The label rename was tried in game; succession not reported |
| Quiet when still, self-defence | enRAGE's calm while you stand still; no bounty for defending yourself | Not recorded | The 0.6.1 funeral fix shows quiet ran |
| Night encounters, hermits, gathering | The crying woman, camps of the woken, the silent ring | Not seen yet | CLAUDE.md: not yet seen in game |

**Working well**

- **Reasons over timers.** Since 0.6.1, scenes come from ties, worries and deeds, so what you see matches what people were thinking. This is the idea to extend everywhere.
- **Safe by construction.** Per-feature guards, borrowed people always handed back, missions left alone, enRAGE changed only through its own methods.
- **Testable.** Core is game-free and simulated offline (256,000 thoughts in the 0.6.1 run), and nearly every system logs and has an F7 TEST line.
- **Original writing**, checked against enRAGE's text before each release.

**Gaps and risks**

1. **Unseen systems.** The night encounters need the wilds, the right hours and a 15 to 25 percent roll, and there's no key to force them. They could go weeks unseen.
2. **Easy to miss.** Scenes play within 22 m, often behind you, and thoughts scroll past on one line. Most of what the mod does can happen without the player noticing.
3. **Few verbs for the player.** The world reacts richly, but the player still acts mostly by greeting, antagonising, flirting and aiming. The interaction menu, answering letters and narratives have waited since 24 October.
4. **The despair default.** At 75 percent, three in four fully awake residents who are armed and alone with the player end their own lives in front of him. For players arriving from a Reddit post, an opt-in default (0, with the README pointing to it) is gentler.
5. **Direct writes into enRAGE.** The README says enRAGE is changed only through its own methods, but a few writes set its fields directly: town items in its paper, revivals, avengers, marriages, a plan's Known flag, Like. They work on 0.9.5.3; an enRAGE update could break them without an error.
6. **Long-session wear.** A feature's error counter never resets, so 15 rare failures in a long session switch it off for good. `Store.People` has no cap, and about eight separate town lists must agree.
7. **Big files.** Engine.cs (1,984 lines), WestworldScript.cs (1,506) and Engine.World.cs (1,181) take most new work. Splitting them first keeps Claude Code's edits small.
8. **No LICENSE file**, so people can read the code but not reuse it. Worth deciding before the Reddit post.

## The next 5 hours: Claude Code queue

**Start here this morning**

1. Read "Where RealNPCs stands" above (two minutes), then answer the comment on Q1, or just start with it.
2. In Claude Code, paste the session rules, then Q1. Play one night in the wilds with the new test key and read the log before going on to Q2 to Q7.
3. Mark ideas Want, Maybe or No in the idea banks. The prompt library (P1 to P26) is ordered by theme, not priority; your picks decide what follows Q7.
4. A sensible order after the queue: P7, P8 and P27 (housekeeping, and one rule for who steers whom), then P2, P12, P1 and P13 (the player's verbs), then P28, P9, P10 and P3 (shared people, places and events).

Seven prompts, about 5 hours 20 minutes of Claude Code work, in order: one to make the unseen systems testable, then your declared next priority, the living street (a) to (e), then a release build. Paste the session rules once, then one prompt at a time. None of them needs the game; each ends with what to check in game.

Times are rough. If one runs long, skip to the next; each stands alone except Q4, which reuses Q2's Core/Passing.cs; Q4 and Q5 add to Q3's TEST street line and create it if Q3 was skipped.

### Session rules (paste first)

```text
You're working on RealNPCs. Follow CLAUDE.md; it is authoritative. For every task in this session:
- C# 7.3 / net48 only. Core/ stays game-free. New settings: field in Settings.cs, line in ToIni(), clamp in Clamp(), bump CurrentVersion.
- Each new feature runs in its own guard, Safe("name", action) in Game/WestworldScript.Core04.cs, so a failure switches only that feature off and the log says why.
- One driver at a time: a ped follows one set of orders, so steer someone only when nothing else is steering them. Nothing during story missions. Ask MayDirect (Game/WestworldScript.Directing.cs): it refuses story models, children, missions, anyone another of our features holds, and enRAGE's people while enRAGE is driving them; otherwise enRAGE's records and townsfolk join in like anyone else (Lex, 2026-10-26). Borrowed peds are always handed back, never deleted.
- Adults only (IsGrownUp) and never story characters (Natives.StoryModel) for anything we task, dress, damage or cast.
- If a Speech set gains a line name, regenerate voices\banks.txt from amb_takes.txt as CLAUDE.md requires; if amb_takes.txt isn't on this machine, say so and list the new names.
- Every line of dialogue and thought is original, plain and period-true (1899, or 1907 for John). Run the overlap and fuzzy checks on new lines (CLAUDE.md has the commands); new lines must add no matches to the current baseline (fuzzy 198, overlap 113 plus '_worldCatchingUp', at 0.6.2).
- New Core logic gets an offline check (compile Core into a scratch net48 exe, as CLAUDE.md describes) covering every combination it can be given.
- Every new feature logs with its own prefix and adds a TEST line to Test() in Game/WestworldScript.cs (the F7 dump).
- Build with dotnet build -c Release and zero warnings.
- Update CHANGELOG.md (an Unreleased section at the top), package/README.txt for new settings and keys, README.md where a player would notice, and CLAUDE.md with the design notes a future session needs.
- Commit each task separately with a clear message. Don't push and don't publish a GitHub release (Q7 builds the zip locally only).
- Don't stop to ask about small choices: take the most reasonable one and list your choices at the end.
- End each task with: what changed, what to do in game to see it, and which log lines prove it worked.
```

### Q1. Test switches for the unseen systems (about 35 min)

The crying woman, hermits, the gathering and the stalker's watch and rush modes have never been seen. A test key turns weeks of luck into one evening of play.

```text
The night encounters (crying woman, hermit camps, the gathering) and the stalker's watch and rush modes have never been seen in game, because they need the right hour, place and roll. Add a way to summon each on demand, for testing.

- New key setting EncounterTestKey in [Encounters] (a free key; check KeySpec takes modifiers and use Ctrl+] if it does and nothing else has it). Add it to SettingsPanel.OurKeys and HeldKeyRepeat.
- Each press forces the next kind in a cycle and starts it: crying woman, hermit camp, gathering, stalker in watch mode, stalker in rush mode. Post "Test: <kind>" on the news line and log TEST force <kind>: started | waiting: <blocker>.
- Forcing skips only the hour, the wilds/town check, the once-a-night count and the chance roll. Every safety rule still applies: no story mission, not wanted, not fighting, not indoors, not in water, nothing else running (encounter, scene, wedding, plea, theft). If blocked, name the blocker.
- A forced hermit uses the longest-fled resident, else makes a stage-4 resident flee on the spot; the camp goes 60-140 m off and out of view as usual. Put a temporary blip on the camp for 2 minutes so I can find it.
- A forced gathering may fill with strangers if fewer than 3 woken residents exist.
- A forced encounter never uses up that night's natural one.
- F7's TEST encounters line names the kind the key will force next.
Walk through the code and confirm each kind's start path is reachable from the key. Docs as usual (CLAUDE.md Testing section too). Tell me exactly what to do in game to see each one and which log lines prove it.
```

### Q2. Living street (a): people who know each other acknowledge each other (about 60 min)

```text
Build slice (a) of "A living street" (roadmap 17): people with history acknowledge each other in passing.

Who: two free ambient people within 30 m of the player, at least one on screen with a clear line of sight, 2-6 m apart and closing, neither in a scene, a wake act, a stalker or a mission; MayDirect true (enRAGE's records join in unless enRAGE is driving them). Counter keepers may nod from where they stand but are never moved. They need a reason: a SceneTie (warm, hostile, romantic or made up), a live worry Link (debt, feud, courting), or both being regulars of the same town whom the player has met twice or more (a plain acquaintance nod).

What they do, decided in a new game-free Core/Passing.cs (seeded, so it's repeatable):
- warm or made up: a nod or hat tip, usually answered; sometimes a word in passing (GREET_GENERAL_FAMILIAR, or GREET_MORNING / GREET_EVENING by the hour).
- hostile: a long look (TASK_LOOK_AT_ENTITY about 3 s, or TASK::REACT_LOOK_AT with a dismissive exit: try it on the first case, check a second later that it took (IS_PED_HEADTRACKING_PED or GET_SCRIPT_TASK_STATUS), log REACT_LOOK_AT works|failed, and keep TASK_LOOK_AT_ENTITY for the session if it failed), a slow head-shake or a spit from a Hothead or Proud one, a wordless disapproving sound; the other looks away (Coward) or stares back (Proud).
- debt link: the debtor looks away and walks on; the creditor's head turns after him.
- courting: a look, a look away, a look back (two look-at pulses), sometimes a shy greeting.
Use only the verified clips in Core/Gestures.cs (for the spit, silent_neutral_spit_f_001/002 from Core/StalkerActs.cs), upper body (flags 8|16) so they keep walking, and the existing Speech sets (wordless where a subtitle would be too specific). Never clear their tasks.

What it leaves: when within 12 m of the player, one of them thinks about it on the ticker, naming the other ("Whitlow nodded. Eleven dollars between us, and he still nodded."). New pools by kind and warmth, 6+ lines each, following CLAUDE.md's thought brief. Passing doesn't change ties.

Limits: one passing per 15 real seconds near the player, a pair once per 2 game hours, a person once per 3 real minutes. Settings: new [Street] section with Passing (on) and PassingEverySeconds (15).

Log PASSING a & b: <kind> (<reason>, N m apart, M m from you). F7 TEST passing lists pairs within 30 m that could pass, and why not. Offline check over every reason x temper x warmth: a valid clip for each sex, every thought filled with the other's name, no unfilled {slots}.
```

### Q3. Living street (b): the street reacts to other people's fights (about 45 min)

```text
Build slice (b) of "A living street": when two other people brawl, square off, or one dies near the player, the game's own crowd reacts as it does to the player's fights.

- Use EVENT::ADD_SHOCKING_EVENT_FOR_ENTITY (12 arguments in alloc8or's rdr3-nativedb-data; the trailing ones are unnamed, so log what each try does) with event hashes joaat(name) from rdr3_discoveries' AI/EVENTS/shocking_events.lua: EVENT_SHOCKING_MELEE_FIGHT or EVENT_SHOCKING_INTERESTING_MELEE_FIGHT for a brawl, EVENT_SHOCKING_SEEN_CONFRONTATION for a standoff, EVENT_SHOCKING_SEEN_PED_KILLED and EVENT_SHOCKING_DEAD_BODY for a death. TASK::TASK_SHOCKING_EVENT_REACT can point one bystander at it (MayDirect true). 8-byte slots for any out-parameter.
- Fire from Game/Director.cs, once per scene per kind: when a brawl's fight act begins, when a standoff's aim act draws (EVENT_SHOCKING_SEEN_WEAPON_THREAT only behind the escalation guard below; it can send a town running), and when anyone in a scene dies.
- Keep the Onlookers look-at (Game/WestworldScript.Reasons.cs) as the fallback if the events don't take; log which happened.
- Escalation guard: if within 10 s of an event a lawman enters combat, a ped outside the scene draws a gun, or anyone flees from the player, stop firing in this town for 30 game minutes and log STREET escalation: <what>. Never fire during missions, while the player is wanted, or with enRAGE's fighters near.
- Setting [Street] StreetReacts (on).
- Log STREET event <type> on <name> (<scene kind>): N peds within 20 m, then 3 s later how many are looking, fleeing or fighting (for tuning). F7 TEST street shows the last five.
Tell me how to get a brawl scene for testing (two hot-tempered men with a hostile tie) and what the log should show.
```

### Q4. Living street (c): reputation in the street (about 50 min)

```text
Build slice (c) of "A living street": people react to who walks among them.

- Outlaws: enRAGE's gang and outfit men (Bridge.EnrageHandlesAtGunpoint, which wraps enRAGE's Interrogable but answers true for anyone while enRAGE's _interro is set, so skip the outlaw check then; and Bridge.EnrageFighter, which wraps RoleOf; both read only), enRAGE's nemesis after the player (ActiveRecord), and residents whose worry is the wanted poster ("law"). When one walks in town (not fighting, no mission), free townsfolk within 10 m who can see him (MayDirect true): a look that follows him as he passes; some, only people not in a scenario, step 2-3 m off his line and are then handed back the way Spawner.GiveBack does, to wander on (never a flee task), the old or a Coward turns away, a Proud or Hothead stares him down. One mutter from a witness on the ticker ("Raider colours. In daylight. Walk on, Hettie.").
- Respected: the doctor, preachers (not posts; add a small model list), counter keepers out in the street, the sheriff and deputies. Passers-by tip hats, nod or greet ("Morning, Doc."). Reuse Q2's Core/Passing.cs for the gestures and lines.
- The player, lightly: if he is a known killer in this town (Deeds and Rumours already know) or a known peacemaker, a witness thinks about it on the ticker. Add no tasks aimed at the player; the game already reacts to him.
- Limits: one reputation reaction per 20 s near the player; each person once per game hour.
- Setting [Street] StreetReputation (on). Log STREET rep: name (outlaw|respected) -> N react. F7 TEST street adds the nearest outlaw or respected person and why they do or don't count.
Offline check: the who-counts and which-reaction functions over every temper.
```

### Q5. Living street (d): how someone looks (about 50 min)

```text
Build slice (d) of "A living street": people notice how someone looks.

- State: bloodied (the loser of a brawl scene, both if neither won, for 2 game hours), drunk (the drunk-song singers; saloon regulars late at night whose worry is the drink, at its worst), soaked (outdoors in rain, or just out of water). Read rain with GET_RAIN_LEVEL and GET_SNOW_LEVEL (probe at load and log what they return). Skip any state you can't read reliably.
- Show it where the game allows: PED::APPLY_PED_DAMAGE_PACK with PD_Burst_lip, PD_Nose_cut, PD_Right_bruise_eye_A or PD_Blood_face_left on brawl losers when the fight ends (left on after hand-back, never cleared then); drunkenness with PED::_SET_PED_DRUNKNESS, _SET_PED_DESIRED_LOCO_MOTION_TYPE ("slightlydrunk", "very_drunk") and AUDIO::SET_PED_IS_DRUNK; wet clothes with SET_PED_WETNESS_HEIGHT and _SET_PED_WETNESS_AMOUNT. Only on free ambient peds (CanDirect true, Bridge.RecordAt(handle) == null). Call each at first use and log it; read drunkenness back with _GET_PED_DRUNKNESS; damage packs and wetness have no getter, so put them in the in-game test plan to confirm by eye.
- Reactions from free passers-by within 8 m who see them: a look, a tut or a laugh (wordless sounds; GREET_BLOODY and GREET_MUDDY are in most ambient voices), steering clear of a drunk, and a friend with a warm tie walking over for a two-line exchange in the style of Core/Exchanges.cs ("Lord, Ezra. Who did that?" / "Walked into a door."). A hostile tie gloats.
- Their own thoughts follow ("Lip's split. Mother will ask. I'll lie."), and the town talks about it for a day.
- Setting [Street] StreetLooks (on). Log STREET look: name bloodied|drunk|soaked -> reaction. F7 TEST street adds who near you looks how.
```

### Q6. Living street (e): the town keeps time, by casting (about 60 min)

The trick: nobody walks across town. Regulars are already cast onto ambient people out of view, so choose who someone becomes from where they stand and the hour.

```text
Build slice (e) of "A living street": each town's regulars keep a routine, without walking anyone across town. Choose WHO a passer-by becomes from where they are and when.

- Give each regular a routine profile, derived (not saved) from trade group (InnerLife.TradeGroup), temperament, Devotion, age and current worry: early riser or late sleeper, drinker or temperate, churchgoer, shopping days, works indoors or outdoors.
- Classify the spot of each ped being cast: saloon, church, store or shop, hotel, station (InteriorKind in Game/WestworldScript.Watch.cs already maps the interior type hashes), main street, side street, outskirts.
- Regulars.Back prefers the regular whose routine puts them at that kind of spot at this hour and weekday: store in the morning, saloon in the evening, church on Sunday morning, home at night with only night owls out after 23:00. New regulars are made to fit the spot (no churchgoer is created in the saloon at midnight). The existing rule stays: never re-dress anyone enRAGE has looked at (Bridge.Looked). New rule: don't cast within 30 m of an enRAGE habit plan that is due or running (PlanInfo.Pos, read the way PeacefulPlanDue in Game/WestworldScript.Quiet.cs does); if the plan's person can be read by reflection (read only), exclude exactly them instead, and log which you used.
- Thoughts follow the routine ("Seven o'clock. Saloon. Same stool."); rarely, a regular somewhere unusual for them notices it ("Never come in here on a Tuesday. Today I did.").
- Setting [World] RegularsRoutines (on). Log REGULAR back: ... (routine: saloon, evening). F7 TEST regulars adds where each nearby regular's routine says they should be now, and whether they are.
Offline: simulate a week of hours x spots for each town's regulars. Check every spot has a plausible regular most hours, churchgoers fill Sunday morning, nobody is in two places at once, and the same few faces own the evening saloon.
```

### Q7. Release build, not pushed (about 20 min; done: 0.6.2, released 2026-10-26)

```text
Prepare 0.6.2 but don't push. Follow CLAUDE.md's Releasing steps 1-2: version in the csproj, package/README.txt title and NEW IN, README.md and CHANGELOG.md (turn Unreleased into 0.6.2). Build, zip release/RealNPCs-0.6.2.zip replacing the old zip, and update the README link. Run the overlap and fuzzy checks and report any change from the 0.6.1 baseline (53 short exact, 2 partial, 113 shared code lines, 197 fuzzy). Then give me an in-game test plan for everything new since 0.6.1, in the order to try it, with the log lines that prove each part.
```

## Roadmap at a glance

Six steps: prove what 0.6.1 built, then the living street you already chose, then verbs for the player. Towns that change, a deeper West World and the long arc follow, in that order.

(The roadmap diagram lives in the online doc.)

The highlighted step is the 5-hour queue. Everything after the second gate draws on the idea banks below; your Want and Maybe picks can reorder it.

## Idea bank: 1899 frontier life

103 ideas from the real texture of 1899 and the epilogue's 1907, grouped by theme and place. Mark each Want, Maybe or No in the last column; the overnight waves and your next session can build on your picks. Size is Claude Code time: S an evening, M a session or two, L a week or more.

One date to start with: Easter 1899 fell on 2 April, the very Sunday your game's clock read in the CLAUDE.md test.

### Law and order

| Idea | What the player sees | Builds on | Size | Your call |
| --- | --- | --- | --- | --- |
| **Wanted, but which one?** | A resident with the poster worry is finally recognised. Turn them in for the bounty, warn them, or see them onto the next train. | Worries (law), Posts, Deeds | M |  |
| **The circuit judge** | Every few weeks a judge rides in and tries the men at the jail, some of them yours. The verdict is in the paper and the saloon argues it. | enRAGE prisoner transfers, Deeds, papers | L |  |
| **Gallows day** | A hanging posted two days ahead: crowd, preacher, the condemned man's kin, vendors. Watch, stop it, or ride in and cut him loose. | Town events, Crowd, Director | L |  |
| **The posse rides** | The posse talk turns real: three to five named regulars ride out after a bad crime. They bring a man back, or one of them comes back dead. | Posse scene, Regulars, Spawner | L |  |
| **Pinkertons asking** | A man in a bowler asks regulars about the player. Those who like him lie for him; those who fear him talk; a friend tells him which. | Rumours, Feelings, Ties | M |  |
| **Vigilance committee** | Where the law is weak (sheriff dead, killings unsolved), men band together to catch a suspect. Ride with them or stand in their way. | Posts (sheriff), Deeds, Crowd | L |  |
| **Bail and the cells** | A regular sleeps off a brawl in the cell. Talk through the bars, pay his fine, and he owes you. | Links, Exchanges | M |  |
| **Witnesses can be leaned on** | After a crime, threaten or pay a named witness. The frightened tell a different story; the paid say nothing; the rumour changes. | enRAGE witness memories, Rumours | M |  |
| **A reward for a neighbour** | A brawl ends in a death and the sheriff posts a reward for the regular who did it. The town splits; collect, warn or leave it. | Brawl scenes, Posts, Ties | M |  |

### Work, money and the week

| Idea | What the player sees | Builds on | Size | Your call |
| --- | --- | --- | --- | --- |
| **Payday Saturday** | Saturday afternoon pay fills the saloon and the barber's chair. Debts are called in, and wives wait at the saloon door. | Saturday lines, Links, routines | S |  |
| **The slate at the store** | The storekeeper keeps a slate for regulars and refuses one who's behind. Pay a stranger's slate and the kindness travels. | Posts, worries (debt), Deeds | M |  |
| **Company town** | Annesburg miners paid in scrip grumble, then meet outside the mine while company men watch. Side with either and the town remembers. | Regulars, Crowd, Rumours | L |  |
| **Roundup season** | In spring and autumn ranch hands talk of the roundup, ride into town with wages after, and one comes back hurt. | Trade groups, clock month | M |  |
| **Drummers on the train** | Travelling salesmen with sample cases work the store and saloon. Someone buys what they can't afford, and a new worry starts. | Arrivals, pitch scene, worries | M |  |
| **The catalogue** | Residents dream over the mail-order catalogue; weeks later a parcel waits at the post office, and pride or disappointment follows. | Post office, inner lives | S |  |
| **Help wanted** | Regulars with a worry ask the player for a day's work or a loan, and the work is real: carry, ride, deliver. | Interaction menu, narratives | L |  |
| **The bank's note** | The bank worry ends in a foreclosure auction in the street, neighbours bidding low, and a family leaving on the train for good. | Worries (bank), departures, Crowd | M |  |

### Faith and fellowship

| Idea | What the player sees | Builds on | Size | Your call |
| --- | --- | --- | --- | --- |
| **Sunday, properly** | The bell at nine, the same regulars in Sunday clothes, a hymn from the street, a sermon set by the week's killings, dinner on the grounds after. | Routines (Q6), Deeds, public-domain hymns | M |  |
| **Camp meeting** | Three nights of revival in a field tent. A drunk swears off liquor and his drink worry ends well; the waking hear "born again" very differently. | Scenes, worries, Awakening | L |  |
| **River baptism** | Converts dipped in the river while a crowd sings on the bank. One of them is someone the player knows. | Director, Crowd, Ties | M |  |
| **Box social** | Young women's supper boxes are auctioned to the men, and the winner eats with the maker. Courting and jealousy follow. | Romance, Ties, jealous scene | M |  |
| **Barn raising** | Neighbours raise a barn in a day. A man and his feud neighbour have to hold the same beam; help, and the family remembers. | Links (feud), Ties | L |  |
| **Quilting bee** | Women at a quilting frame talk over the town's rumours, overheard through the window. | Rumours, gossip scene | S |  |
| **Temperance ladies** | Women pray and sing outside the saloon. Drinkers jeer or slink off; one husband is marched home; the barkeep sours. | Worries (drink), Posts, Crowd | M |  |
| **The shivaree** | The night after a wedding, friends bang pots and pans under the couple's window until the groom stands drinks. | Weddings, Crowd | S |  |

### Sickness, death and mourning

| Idea | What the player sees | Builds on | Size | Your call |
| --- | --- | --- | --- | --- |
| **Fever in town** | One regular's fever spreads along ties. A quarantine notice goes up, the doctor is run ragged, funerals follow; fetch medicine from the next town. | Worries (fever), Ties, Posts | L |  |
| **Mourning dress** | A widow wears black for a year, then half-mourning. Neighbours lower their voices; Sundays she stands at the grave. | Deaths, outfits, routines | M |  |
| **Memorial photographs** | The photographer takes portraits of the dead. The player finds one, a name on the back, in a looted house. | Posts (photographer), Reader | S |  |
| **The undertaker counts** | His thoughts follow the week's deaths: prosperous and uneasy, measuring the player with his eyes. | Posts (undertaker), Deeds | S |  |
| **That cough** | Late in Arthur's story, people who see him often hear the cough and know it: pity, fear, a widow who lost her man to consumption. | Ticker, Feelings; needs story progress | M |  |
| **Limps and scars** | Someone the player shot who lived walks with a limp for weeks and tells strangers how it happened. | Hits, Pleas, Regulars | M |  |
| **Graves visited** | On Sunday afternoons kin stand at graves. The player finds flowers on the graves of men he killed, and whoever left them. | Deaths, routines, Family | M |  |
| **The wake** | The night before a burial the body lies in the parlour and neighbours sit up with it. In West World, the dead man sits up too. | enRAGE funerals, Reveries | M |  |

### News, mail and talk

| Idea | What the player sees | Builds on | Size | Your call |
| --- | --- | --- | --- | --- |
| **Dime novels about you** | Once the legend has travelled far enough, a ten-cent novel appears, built from the most bent version of the rumour. Readers quote it at him. | Rumours (distortion), Reader | M |  |
| **Posters that drift** | The player's wanted poster describes him from rumours: wrong horse, wrong height, an extra scar. Far off, they look for the wrong man. | Rumours, bounty | M |  |
| **The notice board** | Auctions, lost horses, dances, revivals, a hanging, a reward, posted a day or two ahead so the player can plan to be there. | Town events, Reader | M |  |
| **Letters between neighbours** | The letter that didn't come finally comes. Residents write to kin who left, and the player can carry mail to a homestead. | Worries (letter), Letters | M |  |
| **The telephone** | Saint Denis talks about the exchange: who's been heard on it, and a secret overheard. More of it in 1907. | Era lines, Rumours | S |  |
| **The editor has opinions** | Each town's paper takes a side on the player (law and order, sensational, sympathetic), and it colours the items it prints. | enRAGE papers, Deeds | M |  |
| **The telegraph clerk knows** | The clerk reads every wire. His thoughts carry the town's news (deaths, debts, elopements) days before anyone else knows. | Posts (station), Letters | S |  |

### Politics and the age

| Idea | What the player sees | Builds on | Size | Your call |
| --- | --- | --- | --- | --- |
| **Election for sheriff** | Two regulars run: posters, a porch speech, a fight between supporters, the vote on a set day. The player's deeds tip it, and the winner remembers. | Town events, Posts, Ties | L |  |
| **Free silver in the saloon** | Men still argue gold against silver, the railroads and the trusts, and the argument decides who sits with whom. | Argue scene, Ties | S |  |
| **Boys in the Philippines** | Sons in the army are fighting in the Philippines in 1899. Letters come months late; one stops. Cuba veterans tell it in the saloon. | Worries (son), Letters, tall tales | M |  |
| **Suffrage meeting** | Women in Saint Denis campaign for the vote, already won in Wyoming, Colorado, Utah and Idaho. A husband forbids his wife to go. | Crowd, Scenes, Ties | M |  |
| **Saloon or no saloon** | A local-option vote to close the saloon. Barkeep, drinkers and temperance ladies take sides; if it passes, the saloon goes quiet for a week. | Town events, worries (drink) | L |  |
| **Railroad men and the land** | Surveyors pegging a line across farms, furious farmers, the company lawyer at the hotel; feuds that are really about the railroad. | Worries (feud, bank), arrivals | M |  |
| **The century turns** | Everyone argues whether the new century starts in 1900 or 1901. On New Year's Eve, guns go off into the air and the waking whisper about the date. | Clock, era lines, Awakening | S |  |

### New things in 1899

| Idea | What the player sees | Builds on | Size | Your call |
| --- | --- | --- | --- | --- |
| **Motorcars and spooked horses** | A motorcar in Saint Denis throws a rider, and the street argues about progress. By 1907 there are more, and old men hate them. | Scenes, era lines | M |  |
| **Electric light** | Saint Denis marvels or grumbles at the lamps. An outage is a story; country people stare. | Town lines | S |  |
| **Moving pictures** | People leaving the theatre retell the picture. The waking can't bear to watch people living the same scene again and again. | Deeds (theatre), Awakening | S |  |
| **The photographer's** | Couples in their best go in for a wedding portrait; the player's own portrait turns up in a parlour as a curiosity. | Posts, Deeds (photo) | S |  |
| **The phonograph** | A saloon gets a phonograph. Regulars gather, dance, and argue whether it's the devil's work. | Scenes | S |  |
| **Bicycles** | A young clerk on a bicycle, mocked by cowboys and admired by girls, until the crash. | Scenes | S |  |

### Leisure and the calendar

| Idea | What the player sees | Builds on | Size | Your call |
| --- | --- | --- | --- | --- |
| **Holidays on the real calendar** | Easter (2 April), Decoration Day (30 May), the Fourth, Thanksgiving (30 November), Christmas (a Monday) each change thoughts, routines and the street. | Clock, routines | M |  |
| **The Fourth of July** | Flags, a parade, a bandstand speech, a shooting match, and drunken gunfire at night. The whole town is out. | Town events, Crowd | L |  |
| **Mardi Gras in Saint Denis** | Masks and music in the streets (Fat Tuesday was 14 February 1899); drunks and pickpockets work the crowd, and the waking won't take their masks off. | Clock, Scenes, Awakening | L |  |
| **County fair** | Livestock judging, pies, a shooting match, horse races, a greased pig. Win and the county talks; cheat and it's a scandal. | Town events, Deeds | L |  |
| **The Wild West show** | Posters a week ahead, a parade, then riders re-enacting the most bent version of the player's own legend. Afterwards, people talk about "the real one". | Rumours, notice board | L |  |
| **Town base ball** | Sunday afternoon base ball: Valentine against Strawberry. The rivalry spills into the saloon, with rumours of a fixed game. | Ties between towns, Rumours | M |  |
| **Barn prize fight** | An illegal bare-knuckle fight in a barn, bets, a ring of regulars. Bet, fight, or tip off the sheriff. | Brawls, Crowd, Deeds | M |  |
| **Saturday dance** | A fiddler and a caller; courting moves faster, a husband gets jealous, a wallflower waits to be asked. | Romance, Ties | M |  |
| **Séances** | A Saint Denis medium tries to reach the people the player killed. In West World, the dead answer with things only they and he could know. | Reveries, Deeds, Awakening | M |  |
| **Chautauqua tent** | A summer lecture tent: temperance, electricity, spiritualism. The waking ask the scientist all the wrong questions. | Town events, Awakening | M |  |

### Land, weather and seasons

| Idea | What the player sees | Builds on | Size | Your call |
| --- | --- | --- | --- | --- |
| **Rain ends the drought** | A dry-well worry ends well only if it actually rains on that farm. The county talks about rain. | Worries (drought), weather | S |  |
| **Wolves at the calves** | Ranchers lose stock to wolves. Kill wolves nearby (the hunting deed already knows them) and the rancher thanks you. | Deeds (wolf), worries | S |  |
| **Cold snap** | Snow in the high country: people huddle by stoves, an old man doesn't wake, the store runs out of coal oil. | Weather, inner lives | M |  |
| **Planting and harvest** | Talk follows the season: planting in April, haying in summer, harvest in autumn, idle winters at the saloon. | Clock month, day lines | S |  |
| **Flood** | High water in the bayou: a family on a roof, a drowned mule, a collection at church. | Weather, Scenes | M |  |
| **Tales of the big animals** | Tall tales about legendary beasts grow with each telling, like the player's own legend. | Tall tales, distortion | S |  |

### Night folklore (themes only, never the game's lines)

| Idea | What the player sees | Builds on | Size | Your call |
| --- | --- | --- | --- | --- |
| **Lights in the bayou** | Little lights over the swamp (the Cajun feu follet) drift away as you follow. At the end, a body, or nothing. | Encounters, Spawner | M |  |
| **The rougarou** | Lemoyne swears something walks the bayou on two legs. One night a man in skins runs from the player's lantern. | Tall tales, Encounters | M |  |
| **Murder ballads** | Drifters at a fire sing old murder ballads (Pretty Polly, Omie Wise). The waking change the names to people they know. | Song scene, Awakening | S |  |
| **Lantern procession** | After midnight on a back road, mourners follow a coffin on a cart, singing, then turn into a field and are gone. | Gathering mechanics | M |  |
| **The rider who asks the way** | A rider asks the way to a town on no map. Days later, a man is found dead on that road. | Encounters, Rumours | S |  |
| **The bell at three** | The church bell rings in the small hours in an empty church. The preacher swears he locked it. | Encounters, audio | S |  |

### Saint Denis and the river parishes

| Idea | What the player sees | Builds on | Size | Your call |
| --- | --- | --- | --- | --- |
| **Shape-up on the docks** | At dawn, stevedores crowd the wharf hoping the foreman picks them for a day's work. The unpicked drift to the saloons; two men fight over the last place. | Regulars, routines, brawl scene | M |  |
| **A steamer from Europe** | A ship docks and families with trunks wander the wharf looking for kin. One asks the player the way, and next week they're among the regulars. | Arrivals, Regulars, Letters | M |  |
| **The immigrant quarter** | Italian, Irish and Chinese residents carry their own worries: money sent home, a cousin due on the next ship, a boss who sells the jobs. Write it with care. | Worries, inner lives | M |  |
| **Brass band funeral** | A Saint Denis burial walks behind a brass band: a slow hymn to the cemetery, a lively tune on the way back, the street joining in. | Deaths, town events, Crowd | M |  |
| **All Saints' Day** | On 1 November families whitewash and decorate the above-ground tombs. The player finds people at the tombs of men he killed. | Calendar, Deaths, Family | S |  |
| **Yellow fever summer** | In late summer the port fears yellow fever; the city remembers 1878 and 1897. Ships wait in quarantine, and anyone with a fever is shunned. | Worries (fever), epidemic | M |  |
| **Society pages** | Calling cards, a debutante ball, the society column. A rough-looking gentleman at the theatre gets a mention, and the ladies talk. | enRAGE papers, Deeds (theatre) | S |  |
| **Fais do-do in Lagras** | A Saturday dance in the bayou: fiddle and accordion, Cajun French, gumbo, a fight over a girl down by the water. | Town events, Romance, Ties | M |  |
| **Grinding season** | From October to December the sugar mills run day and night. Field hands talk of burns and cane knives; the planter's men watch everyone. | Clock month, inner lives | S |  |

### Annesburg and the mine

| Idea | What the player sees | Builds on | Size | Your call |
| --- | --- | --- | --- | --- |
| **Three whistles** | The mine whistle blows three times: an accident. The town runs to the pithead, wives wait for names, and the dead are regulars the player knew. | Town events, Regulars, Deaths | L |  |
| **The inspector comes** | A state mine inspector is shown the good seam. A miner tells the player about the bad one, and the paper might listen. | Rumours, papers, Ties | M |  |
| **The union man** | An organiser for the United Mine Workers recruits quietly at night. Company men want him gone; protect him or sell him out. | Worries, Deeds, Crowd | L |  |
| **Coal in the lungs** | Old miners cough black into rags; the young ones pretend not to hear. | Inner lives, trade groups | S |  |

### Ranch country

| Idea | What the player sees | Builds on | Size | Your call |
| --- | --- | --- | --- | --- |
| **Doctored brands** | A rancher finds his calves under an altered brand and blames a neighbour. The real rustlers' camp, running iron and all, is up in the hills. | Worries (feud), Encounters | M |  |
| **Sale day** | Auction at the Valentine stockyards: railroad buyers call prices, a rancher is ruined by a bad one, drovers drink their pay. | Town events, payday | M |  |
| **Fence cutters** | Night riders cut a homesteader's wire. The cattleman denies it, but the player saw the riders. | Links (feud), Deeds | M |  |
| **Sheep in cattle country** | A sheepherder in the hills is run off by cowboys who hate sheep on the range. | Scenes, Ties | M |  |
| **Breaking a bronc** | A crowd at the corral rail watches the horse breaker work a bronc. He's thrown and limps for a week. | Crowd, limps and scars | S |  |

### The railroad

| Idea | What the player sees | Builds on | Size | Your call |
| --- | --- | --- | --- | --- |
| **Train time** | The platform fills before each train: kin met, mail sacks swapped, a drummer stepping down, a sheriff waiting for a prisoner. For ten minutes the street is the station. | Arrivals, Posts (station), Letters | M |  |
| **Hobo signs** | Chalk marks near the tracks tell the next tramp who lives there ("kind woman", "mean dog", "officer"), drawn from the real tempers of the people inside. | Residents, Posts, tempers | S |  |
| **Section gang** | Track crews swing hammers in the heat and sing old work songs (John Henry; Drill, Ye Tarriers, Drill). | Song scene | S |  |
| **The claim agent** | A man is killed on the tracks. The railroad's claim agent offers the widow forty dollars and a paper to sign, and the town is furious. | Deaths, town events, Letters | M |  |

### 1907: eight years on

| Idea | What the player sees | Builds on | Size | Your call |
| --- | --- | --- | --- | --- |
| **The boy who remembers** | A young man tells John he was a boy in '99 when a stranger paid his father's debt, or shot his uncle. He means Arthur. | Epilogue continuity, Helps | M |  |
| **The panic of 1907** | From late October 1907 the papers carry the New York bank panic. A line forms at the bank, its doors close for days, and savings worries spread. | Clock, worries (bank, savings), Crowd | M |  |
| **Going dry** | Saloon talk turns to the dry laws spreading by 1907; Oklahoma enters the Union dry that November. Barkeeps worry, bootleggers grin. | Era lines, Posts (barkeep) | S |  |
| **Labels on the bottle** | The snake-oil man now swears his tonic meets the new Food and Drugs Act of 1906, and customers ask what's in it. | Pitch scene, era | S |  |
| **Rangers in the high country** | Forest rangers (the Forest Service is two years old) ride the mountains checking grazing permits. Ranchers grumble about Washington. | Era lines, ranch worries | S |  |
| **Relics** | By 1907 old outlaws are curiosities: the photographer poses old men with rifles, and young men ask John whether he ever rode with a gang. | Era lines, Posts (photographer) | S |  |

## Idea bank: deepening what's built

The richest seam is already in the code: every resident lives through a week-long worry with a named person at the other end, so every one of them is a quest waiting for a verb. These 38 ideas grow systems that exist today.

### Worries you can fix, or make worse

| Idea | What the player sees | Builds on | Size | Your call |
| --- | --- | --- | --- | --- |
| **Pay the debt** | Pay Whitlow the eleven dollars for Elwood and the arc ends well; both remember, and the town hears a stranger paid. Or buy the debt and collect it yourself. | Links (debt), worry arcs, Deeds | M |  |
| **Fetch the doctor** | Ride the doctor out to the feverish child, or pay his two dollars, and the ending you saw coming changes. | Worries (fever), Posts (doctor) | M |  |
| **Settle the feud** | Stand between the neighbours at the fence: make peace, take a side, or pay for the post. | Links (feud), Exchanges, Ties | M |  |
| **Carry the note** | For the nervous courter, carry a note, put in a word, or tell him the truth he doesn't want to hear. | Links (courting), Romance | M |  |
| **Find the letter** | The letter that never came is sitting in another town's post office. Fetch it and watch them read it. | Worries (letter), Letters | S |  |
| **One more, or none** | The man at the worst of the drink: buy him one more, or take the bottle and walk him home. | Worries (drink), saloon | S |  |
| **The bad tooth** | Take him to the Saint Denis dentist, or pull it yourself in the street with pliers while a crowd winces. | Worries (tooth), Crowd | S |  |
| **Make it worse** | Every fix has a dark mirror: collect a debt for a cut, fan a feud, steal savings meant for a ticket west. The rumours and letters follow. | Worries, Deeds, Letters | M |  |

### The social web

| Idea | What the player sees | Builds on | Size | Your call |
| --- | --- | --- | --- | --- |
| **Who's who in the reader** | A page per town in the N reader: the regulars you've met, their trades, the worries you know of, and the ties you've seen between them. | Regulars, Ties, Links, Reader | M |  |
| **Gossip travels along ties** | Inside a town, news moves friend to friend. Tell one man a secret: his friends know tomorrow, his enemies the day after. | Ties, Rumours | M |  |
| **Opinions of each other** | Each pair carries a view (respect, contempt, envy, fear) that decides who sits together, who takes sides and who gossips. | Ties, Crowd, roadmap 4 | L |  |
| **Shared fright** | Two people who both saw the player kill are tied by it. They talk about it quietly and steer clear of him together. | Deed witnesses, Ties | S |  |
| **Grudges between towns** | Valentine thinks little of Strawberry, and tells Strawberry's rumours meaner. | Rumours, town lines | S |  |
| **Secrets and blackmail** | Earn enough trust to learn a resident's secret. Keep it and trust deepens; use it and you get money and an enemy. | Worries (secret), Talk trust | M |  |

### Memory that behaves like memory

| Idea | What the player sees | Builds on | Size | Your call |
| --- | --- | --- | --- | --- |
| **Places hold memories** | Ride past where you shot someone's brother: those who knew him go quiet, stare, leave flowers there. | Deed places, Family | M |  |
| **Known by horse and hat** | People remember "the man on the grey Arabian". Change horse and hat and the rumours lose your trail. | Rumours, player reads | M |  |
| **Memories blur** | Old memories lose detail and gain errors; the painful ones stay sharp. Three weeks on, a witness swears it was raining. | Memories, Rumours | M |  |
| **Dreams of real things** | Dream diary entries come from what really happened to the dreamer, bent out of shape. | Diaries, Reveries | S |  |

### Player verbs

| Idea | What the player sees | Builds on | Size | Your call |
| --- | --- | --- | --- | --- |
| **Interaction menu** | Hold the look key on someone you know: ask about rumours or another person, buy a drink, offer money, threaten, apologise, flirt. | Feelings, Ties, Romance, Pleas | L |  |
| **Buy the house a round** | Pay for everyone's drink: liking rises with all present, the barkeep remembers, the story travels. | Deeds, Rumours | S |  |
| **Apologise** | Say sorry to someone with a grudge. The proud refuse, the kind soften, the cold take money instead. | Feelings (anger), tempers | S |  |
| **Answer letters** | Reply kindly, coldly or with a threat; the writer's next letter, visit or revenge follows from it. | Letters, Family, avengers | M |  |
| **Gifts** | A pelt, flowers or a drink for a sweetheart or a widow; what fits depends on who they are. | Romance, worries | M |  |
| **Lend money** | Lend to a resident in trouble; they repay on payday, late, or never, and you collect kindly or not. | Links (debt), payday | M |  |

### West World, deeper

| Idea | What the player sees | Builds on | Size | Your call |
| --- | --- | --- | --- | --- |
| **The voice is the schedule** | Past stage 2, a resident hears their routine as orders ("Seven o'clock. Saloon.") and answers back. Breaking the loop is not going. | Routines (Q6), Awakening | M |  |
| **They noticed the reload** | Reload after a killing and the victim, alive again, has a sharper déjà vu. The waking talk about the morning that happened twice. | Store.Rewind, Hosts | M |  |
| **The revived dead are seen** | Anyone who saw a death and later sees that person alive (a revenant, a keeper back from wounds) stares, crosses themselves, and wakes a stage. | Reveries, Posts, Awakening | S |  |
| **A line that wakes** | An awake resident passes on a public-domain line; whoever hears it the right way starts stirring. | Awakening spread, Crowd | M |  |
| **Hosts compare notes** | Two waking people test the world together: walk to the town's edge, count the same faces, compare dreams. | Hosts scene, wake acts | M |  |
| **The reset** | Someone fully awake turns up days later at stage 0, cheerful and blank. Those who knew them notice. | Awakening phases | S |  |

### Making it visible

| Idea | What the player sees | Builds on | Size | Your call |
| --- | --- | --- | --- | --- |
| **Overheard, in the reader** | A page of what happened near the player in the last game day, one line each: who quarrelled, made up or called in a debt. | Scene log, Reader | S |  |
| **Raised voices behind you** | A scene starting out of view within 22 m puts a cue on the ticker ("Raised voices behind you") so the player can turn. | Director, Ticker | S |  |
| **People you know on the map** | An optional toggle shows the regulars you've met as named marks while you're in town; F7 already does it for 60 s. | Regular blips | S |  |
| **This week in town** | A weekly column in enRAGE's paper from the simulation: births, deaths, marriages, fights and visitors. | enRAGE papers, Engine.World | M |  |

### Change over time

| Idea | What the player sees | Builds on | Size | Your call |
| --- | --- | --- | --- | --- |
| **Leaving on the train** | Worries that end in departure end at the station: a farewell, a trunk, and that regular is gone from town for good. | Worries, Regulars | M |  |
| **New faces arrive** | Settlers, a schoolteacher, a mail-order bride, a new preacher arrive by train or wagon, each with a worry from day one. | Arrivals, Regulars | L |  |
| **Towns that shrink** | Where the player has killed many, fewer regulars remain; a shop closes and people talk of leaving. | Deeds, Regulars | M |  |
| **The epilogue remembers** | On John's first load, some of Arthur's regulars carry over eight years older: married, widowed, moved on, and a few remember a man called Morgan. | Store, Regulars | L |  |

## How to build them

Three building blocks carry most of the idea bank: a saved outcome for worries so the player can change them, one town-events framework grown from the wedding code, and a small interaction menu. Below are those three and eleven more. The last, who steers whom, sits under all of them.

### 1. Worries the player can change

- **Today:** a worry's kind, stage and ending are computed from `Person.Id` and the game day (`ArcOf`, `KindAt`, `EndsWell` in `Core/InnerLife.Worries.cs`). Nothing is saved, so nothing can change them.
- **Add a saved override:** `Store.WorryOverrides` holding person id, arc, outcome (good or bad), how (paid, doctor, peace, note, letter, bottle, tooth, stole) and when. `EndsWell` and `Links.EndsWellFor` read it first. `Fix()` creates it for old saves, `Rewind()` drops newer entries, cap 400 like `Links`.
- **Offers:** a worry at stage 0 to 2 is open. A new game-free `Core/Helps.cs` lists, per worry kind, one to three fixes and one dark mirror, each with its cost, its task and its lines (offer, accept, refuse by temper, thanks, the town's version).
- **Tasks reuse what exists:** pay (the cash balance is read already, in cents, since 0.6.2; \_MONEY\_DECREMENT\_CASH\_BALANCE still needs its probe), carry a note to the `Link` counterpart (a blip while bound), fetch the letter from another post town (letters are handed over when the player rides into one), bring the doctor post's holder or pay his fee.
- **What it leaves:** a new deed kind (`helped`, `harmed`) for rumours and town talk, the worrier's and the counterpart's thoughts ("The stranger paid Whitlow. Didn't even ask my name."), a thank-you letter days later, and the arc's last stages read from the override.
- **Rails:** one open help at a time; nothing in missions; money taken only after the player confirms; deadlines lapse quietly; an override is written once.
- **Offline check:** every worry kind x fix x stage, with no contradictions (no "fever broke" after a burial).

The full table of fixes and dark mirrors for all 13 worries is under "What the player can do".

### 2. One town-events framework

- **Start from the wedding:** `Core/World.cs` `Wedding`, `Engine.WeddingTick` (banns as a toast and a `WorldRequest` paper item, invitations), `WeddingNear`, and the game side's `WeddingRun` in `WestworldScript.Life.cs` (staging, `WeddingBroken`, cleanup when a mission starts). Generalise it rather than writing a second system: a new partial Core/Engine.Town.cs scheduled from DailyTick like ScheduleWedding and finished from WorldTick, a List\<TownEvent> in Store, and staging like Ceremonies, BeginWedding and WeddingStep.
- **An event** has a kind, town, place kind (church, main street, field outside town, saloon, station, gallows), start, phases (gather, main, disperse), cast roles (preacher, speaker, couple, condemned, crowd of N), a notice posted N days ahead, and outcome rules for when the player is away, present, or breaks it up.
- **Triggers:** the calendar (holidays), worries (a bank worry ending badly gives an auction), courts (a trial gives a hanging), the player's legend (a Wild West show once fame is high), and a weekly roll per town (dance, box social, base ball).
- **Away:** it still resolves in the daily tick, into papers, rumours, thoughts and ties ("Met Clara at the dance.").
- **Present:** within 150 m at the time, stage it like a wedding: free ambient people first (`CanDirect`), spawned extras for the roles, placed at fixed spots per town and place kind. Gather those spots once in game with a small "note this spot" key that logs coordinates.
- **Rails:** no missions; one event per town at a time; nothing spawned in view; shots break it up and the town remembers who fired.

### 3. The interaction menu

- **Open it from what exists:** the add-on already hears the game's Greet and Antagonize prompts (`EventPromptTriggered`, `PromptGreet`). Greeting a resident, regular or enRAGE townsperson a second time within a few seconds opens a small drawn menu (the settings panel's drawing and pad handling, scaled down). A custom prompt in the ped's focus group is the polished version; Lex already built UI prompts in the fast-travel mod, and RealNPCs' own Flirt prompt in the lock-on menu (0.6.2) is proven in game.
- **Options that fit the person and place:** ask what's new (local rumours; their own worry if trust allows), ask about someone (their tie and opinion), buy a drink (saloon only), offer help (from Helps), give or lend money, apologise, threaten, flirt (the existing Flirt: F2, or H in the lock-on menu), goodbye.
- **Each answer:** the player's line in Arthur's or John's voice, the reply in the person's own voice through `Tone`, subtitles, and effects on their feelings. For enRAGE's townsfolk, only through enRAGE's own methods.
- **Rails:** closes on 3 m of movement, a drawn weapon, combat or a mission; the game's own prompts stay untouched.

The full option list, its state and its rails are under "What the player can do".

### 4. Routines by casting

- A routine is a preference over spot kind x hour x weekday, derived from trade group, temperament, devotion, age and worry: early riser, shopper (Monday, Wednesday, Saturday), drinker (18:00 to 23:00, Saturday most), churchgoer (Sunday 08:30 to 12:00), indoor worker (09:00 to 17:00), night owl.
- `Regulars.Back` scores each free regular: routine fit, plus time since last touched, plus a worry boost (the drink worry pulls to the saloon). The best one is cast. Nobody walks across town; the right people are simply the ones you meet.
- The Awakening gets it for free: past stage 2, the routine is "the voice", and an awake regular refusing it is breaking the loop.

### 5. Weather-aware worries

- The code reads the weather type where the player is (Engine.NowWeather, since 0.6.2) but keeps no history. Each game hour, record whether it's raining where the player is (find the weather-type natives) as rain hours per region in the store.
- The drought's ending is decided once, at its crisis, from the rain recorded in its region so far (weather is only seen where the player is). Rain also shifts routines (fewer in the street, more in the saloon) and thoughts.

### 6. An epidemic

- Per town: residents whose current worry is the fever are the sick. Each game day they pass it to tied people and regulars sharing the same routine spots, with a set chance; the doctor post has a daily capacity; at three sick a quarantine notice goes up.
- A third of fever arcs already end badly, so burials follow. The player can fetch medicine from another town's doctor within 12 game hours (a help override). One outbreak per town per 30 game days.

### 7. The epilogue remembers

- On John's first load, with no `john.json` and an `arthur.json` present, copy the regulars and post holders. Age them eight years and roll the years between: married, widowed, moved away, died (the old more often).
- Keep who knew Arthur by name and his top deeds in their town, so their talk uses "years back" lines. Never touch enRAGE's records; enRAGE keeps its own save.

### 8. Dime novels and drifting posters

- From `Rumours`: take the player's most-told deed and the version furthest from where it happened. Build a title from templates (epithet and region: "The Devil of the Heartlands") and a short excerpt from the bent version, delivered as a clipping in the reader, which already draws clippings though nothing creates one yet.
- Posters get description fields (horse colour, height, hat, scar) that drift with the rumour's distance. Thoughts quote both ("Read about you. Said you were seven foot.").

### 9. A spot table for every town

- Most staged ideas need a fixed outdoor place: the wharf, the pithead, the stockyards, the platform, the cemetery, the bandstand, the church steps. Today the code has rough town centres (`Rumours.Towns`, no radii) and interior kinds (`InteriorKind`), but no named outdoor spots.
- Add `Core/Spots.cs`: per town, named spots with position, heading, kind and room for N people, shipped as data in the DLL. Gather them in one ride round the map with a test key that appends the player's position, heading and a typed label to `RealNPCs\spots.txt`; Claude Code turns that file into the table.
- Every staged idea asks for a spot by kind ("pithead", "platform") and falls back to its off-screen outcome when the town has none, so a missing spot never breaks anything.

### 10. Train time

- **Detect a stop:** a train within about 60 m of a platform spot, standing still for 5 s (VEHICLE::IS\_TRAIN\_WAITING\_AT\_STATION and ENTITY::GET\_ENTITY\_SPEED; probe at load). Platforms come from the spot table.
- **While it stands, and two minutes after:** free ambient people on the platform turn to the train; one regular from elsewhere steps down (a newcomer, or kin through `Core/Family.cs`); the station post's keeper calls out. Letters for this town come "on this train" instead of waiting for the next ride into town.
- **Departures:** residents whose worry ends in leaving (savings for a ticket, a son gone, a foreclosure) are cast on the platform that day, say goodbye to someone tied to them, and are marked gone (a new `Resident.Left`, like `Fled`) so the town never casts them again.
- **Rails:** never touch the train's own peds or the train; platform people only, never in missions, nothing spawned in view.

### 11. Three whistles, as a town event

- **Trigger:** rare, about once in 60 game days per mining town, sooner after an ignored inspector.
- **Away:** one to three of the town's regulars of the working trade group die; resident funerals, a letter to kin, the widows' worry turns to grief, a paper item, rumours to nearby towns.
- **Present:** the whistle (Steam\_Donkey\_Sounds Whistle\_Long, blown three times; a toast if it won't play), a crowd at the pithead spot (wives, old miners), names read out one at a time in subtitles, grief from the voice sets, the doctor post overrun.

### 12. 1907 continuity, in detail

- When importing Arthur's world into John's (logic 7), look at who Arthur touched: helps, debts paid, kin he killed. For each, make a young adult relative (17 to 25 in 1907, never younger) who remembers it and becomes a regular of the same town.
- Their first meeting with John colours everything after ("You ride like him. The one who paid Pa's debt."). Era lines already switch on `InnerLife.ClockYear` (`Era1899`, `Era1907`); new 1907 items key off the same.

### 13. Game resources worth knowing

The names below come from [rdr3\_discoveries](https://github.com/femga/rdr3_discoveries) and [alloc8or's native DB](https://github.com/alloc8or/rdr3-nativedb-data); argument counts are checked against the DB. Those lists mix in Online content, and CLAUDE.md already records 25 of 28 emote sets missing in Story Mode, so every one needs the in-game probe before a feature relies on it.

| Need | What the game has | Caution |
| --- | --- | --- |
| The street reacts to a fight or a death | `EVENT::ADD_SHOCKING_EVENT_FOR_ENTITY` (12 args) with EVENT\_SHOCKING\_MELEE\_FIGHT, \_INTERESTING\_MELEE\_FIGHT, \_SEEN\_CONFRONTATION, \_SEEN\_PED\_KILLED, \_DEAD\_BODY; `TASK::TASK_SHOCKING_EVENT_REACT` (3) for one bystander | Trailing args unnamed; no gunshot shocking event exists |
| Glances in passing | `TASK::REACT_LOOK_AT` (9): intensity 0 to 3, exit generic, dismissive or relieved; `TASK_LOOK_AT_ENTITY` (6) | Untested on ambient people |
| Words for passing and looks | Speech GREET\_GENERAL\_FAMILIAR (328 of 332 ambient voices), GREET\_MORNING and GREET\_EVENING (197), GREET\_BLOODY (189), GREET\_MUDDY (184), DISMISSIVE\_REACT (310), IGNORING\_YOU (311), DRUNK (217) | Coverage = how many ambient voices carry it |
| Bloodied after a brawl | `PED::APPLY_PED_DAMAGE_PACK` (4): PD\_Burst\_lip, PD\_Nose\_cut, PD\_Right\_bruise\_eye\_A, PD\_Blood\_face\_left; `CLEAR_PED_BLOOD_DAMAGE` (1) | No getter: confirm by eye in game |
| Drunk | `PED::_SET_PED_DRUNKNESS` (3), `_SET_PED_DESIRED_LOCO_MOTION_TYPE` (2) "slightlydrunk" or "very\_drunk", `AUDIO::SET_PED_IS_DRUNK` (2) | Community names; probe |
| Rain, snow, wet clothes | `GET_RAIN_LEVEL` (0), `GET_SNOW_LEVEL` (0), `MISC::GET_CURR_WEATHER_STATE` (3: two weather hashes and the blend); `PED::SET_PED_WETNESS_HEIGHT` (2) with `_SET_PED_WETNESS_AMOUNT` (2) | \_GET\_NEXT\_WEATHER\_TYPE\_HASH\_NAME gives the weather being changed to, not the current one |
| A dance with music | WORLD\_HUMAN\_DANCING (women only, the only dance scenario); PROP\_HUMAN\_SEAT\_BENCH\_FIDDLE(\_UPBEAT), PROP\_HUMAN\_PIANO; busker streams such as inworld\_music\_fiddle\_upbeat via `AUDIO::LOAD_STREAM` and `PLAY_STREAM_FROM_PED` | Seated scenarios need a bench or piano at the spot |
| Clapping, praying, a hat tip | Kit emotes through `TASK::TASK_PLAY_EMOTE_WITH_HASH` (9): KIT\_EMOTE\_REACTION\_CLAP\_ALONG\_1, KIT\_EMOTE\_ACTION\_PRAYER\_1, KIT\_EMOTE\_GREET\_HAT\_TIP\_1; crowd sounds SHOWS\_sounds CHEER\_SMALL to \_LARGE, rbnp2\_Sounds CROWD\_JEER\_01 to \_10 | Kit emotes are an Online feature; keep the verified ai\_gestures as the fallback |
| Church bell | Soundset CHURCH\_BELL\_SOUNDS: SD\_SINGLE\_TOLL, VAL\_SINGLE\_TOLL | Some soundsets need a script audio bank first |
| Mine whistle | Steam\_Donkey\_Sounds Whistle\_Long, blown three times | Probe |
| Sitting at a set bench or chair | `TASK::TASK_START_SCENARIO_AT_POSITION` (12), `TASK_USE_SCENARIO_POINT` (10), `GET_SCENARIO_POINTS_IN_AREA` (6) | Fits the spot table |
| Mourning, preaching, kneeling | WORLD\_HUMAN\_GRAVE\_MOURNING(\_KNEEL), SC\_WORLD\_HUMAN\_PREACHER, SC\_WORLD\_HUMAN\_KNEELING (men) |  |
| Work in the street | WORLD\_HUMAN\_BROOM, WORLD\_HUMAN\_WASHBOARD (women), PROP\_HUMAN\_WOOD\_CHOP, WORLD\_HUMAN\_SAW\_WOOD and WORLD\_HUMAN\_HAMMER\_\* (men), WORLD\_HUMAN\_FEED\_CHICKEN, WORLD\_HUMAN\_SELL\_PAPER (men) | Hand props spawn with the scenario |
| Known by horse | `PLAYER::_GET_ACTIVE_HORSE_FOR_PLAYER` (1), `PED::GET_MOUNT` (1); horse models are named a\_c\_horse\_\<breed>\_\<coat>, so breed and coat words come from the model | No brand data exists |
| Money for helps and drinks | `MONEY::_MONEY_GET_CASH_BALANCE` (0), \_MONEY\_DECREMENT\_CASH\_BALANCE (1), \_MONEY\_INCREMENT\_CASH\_BALANCE (2) | The balance is in cents (read since 0.6.2); probe the decrement before taking money |
| Horse breaking and calming as deeds | Events EVENT\_HORSE\_BROKEN (rider, horse, result), EVENT\_CALM\_PED | Data layouts documented |
| Baths, drinks at the bar | EVENT\_STAT\_VALUE\_CHANGED with BATHS\_TAKEN, BAR\_DRINKS\_CONSUMED | May not fire in Story Mode |

### 14. One person, one driver at a time

The split that matters is who steers someone right now, not who knows them. Identity can be shared completely between enRAGE, RealNPCs and the game; steering is lent to one driver at a time and handed back.

**Why steering needs one driver**

- **Last order wins.** A task replaces the one before it. enRAGE orders the people it runs (in its encounters, camps and plans) on its own schedule, so a ped steered by both stutters between them, or enRAGE's plan stalls waiting for an arrival that never comes.
- **Lifetime.** Whoever spawned a ped decides when it goes. Our hand-back marks a ped "no longer needed", which on someone enRAGE spawned can let the game clear away a person enRAGE still tracks. enRAGE can likewise remove a ped mid-scene; our scenes already end cleanly when a cast member vanishes.
- **Story missions** wait for their peds to hit exact marks, so steering one can soft-lock a mission. That limit stays.

**Already shared today:** enRAGE's townsfolk act in RealNPCs' scenes, worries, ties and wake acts, and feelings, deeds, papers, avengers and weddings cross between the mods. Only enRAGE's full records (its nemeses, gang men and others it runs) and peds another script owns are held to the passive part: a look, a thought, a voice. CLAUDE.md already puts it as "hands off anyone enRAGE or a mission is using"; "is using" is the right test.

| Order | Who steers | When | What RealNPCs may do |
| --- | --- | --- | --- |
| 1 | A story mission | Any mission is running, or a mission script owns the ped | Nothing: RealNPCs already pauses |
| 2 | enRAGE | A fight or interrogation, a fighter role, a live plan, a goal it is pursuing, or a ped it spawned and still holds | Look, thought, voice; let go at once if it was ours |
| 3 | RealNPCs | Borrowed for a scene, act or event | Steer, re-check every tick, hand back after |
| 4 | The game's ambient life | Everyone else, enRAGE's idle records included once P28 is on | Borrow when needed |

**What it unlocks**

- enRAGE's gang men between jobs drinking in the saloon, quarrelling, courting, carrying worries.
- A nemesis with a sweetheart and a debt, who visits his mother's grave.
- Your regulars rising into enRAGE's world: a resident ruined by the bank or a debt, and holding a grudge, becomes an enRAGE record by the path avengers already use, and joins a gang if enRAGE has a method for it. "Elwood Price, who owed Whitlow eleven dollars, has ridden out to join the raiders."

**Building it**

- One `Control.Who(handle)` that every feature asks, failing closed: unsure means busy.
- What counts as busy inside enRAGE is learned by reading its code locally (never committed), then read by reflection, read only.
- Both ways: our borrowed peds are marked as mission entities; P27 checks whether enRAGE skips those. The clean version is a small public hook from enRAGE's author (`IsBusy(handle)`, `Lend`, `Return`). Until then, borrowing enRAGE's records sits behind a setting, off until play-tested.
- Missions: RealNPCs keeps pausing during them, and `ENTITY::_GET_ENTITY_SCRIPT` (2 args) is probed to learn which script owns a mission-entity ped.
- README: "changes things only through enRAGE's own methods" stays true for enRAGE's data. Add that RealNPCs steers enRAGE's people only when enRAGE isn't using them, and hands them straight back.

## What the player can do: four designs

Four designs give the player verbs, each built on code that exists: the menu opens from the greet the game already reports, worries get a saved outcome, letters get a reply, and worries grow into short narratives. Prompts P2, P1 and P12 to P16 build them.

### The interaction menu

- **Why it matters:** greeting a resident today does nothing unless they're in a scene; `Greets()` only starts conversations with enRAGE's records and townsfolk. For residents and regulars, the menu is the conversation.
- **Open:** a second greet within a few seconds, or an InteractKey while the player has someone targeted (`PLAYER::GET_PLAYER_INTERACTION_TARGET_ENTITY`, 4 args; `GET_PLAYER_TARGET_ENTITY` as a fallback). enRAGE's records keep their own talk and get the menu after it.
- **Panel:** a small drawn list above the ticker, titled with name and trade ("Ezra Whitlow, barber"), number keys or D-pad and A. An option that can't be used shows greyed with its reason ("Only in a saloon").
- **State for residents and regulars:** `PersonState.Regard` (-100 to 100), `PersonState.Fear` (0 to 100), both easing 5 points a game day toward 0, and `Asked` for per-option cooldowns. enRAGE's townsfolk keep enRAGE's own Like, Fear and Grudge; only the existing `LikeMore` may change them.
- **Feeds:** feeling bars for residents (from Regard and Fear), deeds for gifts, rounds and threats, rumours from those, thoughts and diary lines about the stranger.
- **Money:** `MONEY::_MONEY_GET_CASH_BALANCE` (0 args) and `_MONEY_DECREMENT_CASH_BALANCE` (1); probe the unit, probably cents, at load. If the probe fails, money options hide.
- **Rails:** adults only (`IsGrownUp`); story characters never; closes on a drawn weapon, 3 m of movement, combat, a mission or 20 s idle; someone in a scene gets the scene's step-in instead.

| Option | Shows when | What happens |
| --- | --- | --- |
| What's the news? | Always | A local rumour or town talk; their own worry once Regard is 20 or more, or they've talked twice |
| What do you know about Clara? | They have a tie or link with someone | Their view of that person, by warmth; the link goes on the Who's who page |
| Buy you a drink? | In a saloon | A nickel for a beer, as in 1899; Regard +10 (+15 for a drinker, -5 from the temperate); both drink a moment |
| Need a hand? | Their worry is known and open | The fixes for that worry (next design) |
| Here, take this | Always | $1 or $5; Regard rises, the Proud refuse, the Greedy want more |
| About before... | They hold anger or a grudge | The kind soften, the Proud refuse, the Cold name a price |
| Watch yourself | Always | Fear +30, Regard -20; a Coward gives in, a Hothead squares up and storms off; witnesses remember |
| Introduce someone | A third adult the player knows is near | A new acquaintance tie between them; a fitting pair may start courting |
| Flirt | The romance rules allow it | The existing Flirt (F2, or H in the lock-on menu) |
| Goodbye | Always | Closes |

### Worries the player can fix or make worse

Each of the 13 worry kinds gets one fix and one dark mirror. The ending is written as a saved override, so the arc's remaining days follow the player's choice.

| Worry | The fix | The dark mirror | How it can end |
| --- | --- | --- | --- |
| Debt | Pay what's owed | Buy the debt and collect it with menace | Paid and thanked, or a creditor who fears you |
| Fever | Fetch the doctor or pay his fee | Walk past | The child sits up, or the burial goes ahead |
| Letter | Fetch it from the post town it strayed to | Read it first and keep what it says | Read aloud at last, or a secret the player holds |
| Drought | Lend for a well | Lend at a ruinous rate | The well comes in, or the family leaves on the train |
| Son | Ride to the mines or the army post with a letter | Tell the father his son is dead | A letter home, or a grief that wasn't true |
| Courting | Carry a note, put in a word | Court the sweetheart yourself | A yes, a kind no, or a jealous man |
| Feud | Bring both to the fence and settle it | Antagonise one in front of the other | A handshake, or a brawl and a day's rest |
| Bank's note | Pay it at the bank | Buy the note and foreclose yourself | The farm kept, or an auction in the street |
| Drink | Take the bottle and walk him home | Buy him one more | A sober Sunday, or the worst week |
| Savings | Add to them | Steal them | The ticket west, or a ruined man |
| Tooth | Take him to the dentist | Pull it yourself in the street | Relief, or a story the town tells for a week |
| Secret | Keep it three days after learning it | Use it for money | Trust, or an enemy |
| Wanted poster | Warn them and see them onto a train | Turn them in for the bounty | Gone for good, or the circuit judge |

- **State:** `Store.WorryOverrides` (logic 1) and `Store.Helps`: person, arc, fix, state (offered, accepted, done, failed, lapsed), target (person, town or post), deadline, cost.
- **Task kinds:** pay, deliver to a person, visit a town or post, escort (a follow task on a free ambient ped; ends if lost), mediate (both within 10 m within 10 minutes), keep (time passes without telling).
- **Failing:** an accepted help that lapses costs Regard and leaves a thought ("Said he'd come back. Didn't."); the arc then runs as it would have.
- **Feeds:** a `helped` or new `harmed` deed weighted to travel, the worrier's and the counterpart's thoughts, `Letters.Thanks`, or a new `Letters.Wronged` for the harmed, days later, a diary line, town talk.

### Answering letters

- **Today:** a `Paper` carries `From` and `FromId`; letters wait until the player rides into a post town (`EnteredTown`) and are read with N. The builders are Grief, Thanks, Love, Scorned, Confession, Invitation and the revenant's note; `Tip` is never used.
- **Reply:** in the reader, R on a letter with a `FromId` offers five answers: kind, cold, threat, money ($5 or $20), ask to meet. A reply takes one to three game days to arrive, by distance.
- **State:** `Store.Replies`: letter, recipient, tone, amount, sent, arrives, done. One reply per letter, none to the dead.
- **Avengers stay ours until they arrive:** an `Avenger` at Stage 0 (letter) or 1 (riding) can stand down (Stage 3) on a kind or money reply, by the kin's temper, or ride sooner on a threat. Nothing is written to enRAGE before arrival.
- **Meet:** the sender waits on a set day at their post town's station or main street, cast on a free ambient person; greeting them plays the conversation.

| Letter | Kind | Cold | Threat | Money | Meet |
| --- | --- | --- | --- | --- | --- |
| Grief, from a dead man's kin | May stand the avenger down: "I won't forgive you. I won't come." | Nothing changes | The avenger rides sooner: "I'm coming." | The proud send it back torn; others stand down | They wait at the station: words, or rarely a gun, through the avenger path only |
| Thanks | Warmer; a small gift later | Hurt | Fear, and the town hears | Refused | A meal at their table |
| Love, from a sweetheart | Hearts rise | Scorn begins | Scorn and fear | Insulted | An evening walk in town |
| Scorned | The scorn fades | It sets | Hate | Insulted | A last talk |
| Confession, from someone who suspects | They wake a stage | They doubt themselves | They hide | Confused | They come to you |
| Wedding invitation | The couple expects you | Disappointed | The wedding moves | A gift noted in the paper | No reply needed |

### Narratives that grow out of worries

- **Shape:** a narrative is a chain of two to four steps grown from one worry and its link. `Store.Narratives`: kind, origin person, arc, steps (what, who, town, spot, done), state, deadline. At most two open.
- **Steps reuse what exists:** visit a town or post, meet a regular (cast), carry a sealed letter (a new paper kind the reader shows as carried), pay, escort, be present at a town event or train time, choose.
- **Offered:** through "Need a hand?", or a request letter at the post office ("They say you helped Elwood Price..."), from a new `Letters.Request` builder.
- **The world doesn't wait:** the worry's arc runs on, the narrative lapses at its deadline, and the asker remembers.

| Narrative | From | Steps | Endings |
| --- | --- | --- | --- |
| The son at the mine | Son | Ride to Annesburg, find the son (a cast regular with his own mine worry), carry his letter home or bring him home on the train | The father's worry ends well, or the mine whistle takes the son |
| The letter gone astray | Letter | Ride to the named post town, read it or not, deliver it | Read aloud at last, or a secret kept |
| Who cut the fence | Feud | Talk to both, watch the fence one night, name the cutter (a third regular paid by a rancher) | The feud ends and a new one starts |
| The bad tonic | Fever | Find the snake-oil man in the next town; take the money back or run him out | The doctor's cure works, or the town turns on the seller |
| A ticket west | Savings | Add to the savings or catch the thief; see them off at train time | They leave, and the town has one face fewer |
| Courting by proxy | Courting | Carry notes back and forth across three meetings | Banns posted through the wedding code, or a kind no |

### More verbs

| Idea | What the player sees | Builds on | Size | Your call |
| --- | --- | --- | --- | --- |
| **Write a letter yourself** | At any post office, write to someone you've met: a warning, an apology, a love note, a threat, money. It lands a few days later and changes them like a spoken word. | Answering letters | M |  |
| **Spread a rumour** | Tell a regular something untrue about another person, or about yourself ("I hear Morgan's dead"). It travels and bends like your deeds do. | Rumours, the menu | M |  |
| **Make an introduction** | Introduce two people you know. A tie starts; between a fitting pair, a courtship might. | Ties, Links | S |  |
| **Hire a regular** | Pay a down-on-his-luck regular to watch your horse, carry a message or keep lookout. He talks about working for you. | Regard, Links | M |  |
| **Vouch for a prisoner** | Vouch for a jailed regular and he's let go into your keeping. If he does it again, your name suffers. | Bail and the cells, Deeds | M |  |
| **Post a reward** | Put up your own reward for a horse thief or a man who hurt someone you know. Men go after him; the paper reports it. | Posts (sheriff), papers | L |  |
| **Testify** | At the circuit judge's trial, say what you saw, true or not, and the verdict follows. | Circuit judge, Deeds | M |  |
| **Teach a young man to shoot** | He worships you after. In 1907 he's a lawman or an outlaw, and he tells John who taught him. | Regulars, 1907 continuity | M |  |
| **The collection plate** | Give on Sunday and the preacher thanks a generous stranger from the pulpit; the town warms a little. | Sunday service, Deeds | S |  |
| **A wager in the saloon** | Bet a regular on a horse race or a bottle at twenty paces; win money and a reputation, or lose both. | Saloon, Deeds, Regard | M |  |
| **Walk someone home** | After dark, an old man or a woman afraid of the road asks to be walked home. Some nights, something waits on it. | Night encounters, escort | M |  |
| **Stand the wake** | Pay for the drinks after a funeral; the mourners remember who did. | enRAGE funerals, Deeds | S |  |

## West World and the night: designs

The Awakening has its four stages, acts and endings; what's missing is how it spreads and what the waking notice. Four mechanisms close that gap, each on existing code: routines become the voice, save reloads leave ghosts, a line passes along ties, and the revived dead are recognised.

### The Awakening, phase by phase

| Phase | What the player sees | Built today | To build |
| --- | --- | --- | --- |
| The loop | Their days go on, blind to what can't be: a dead man back at his counter gets a nod. | Most people sit at stage 0 | Recognising the impossible only from stage 1 (the revived dead, below) |
| Reveries and dreams | Gestures and dreams from lives they never had | Stage lines, dream diary entries, glitches | Reveries drawn from real deeds of the dead: another person's memory in their mouth |
| Counting | "Fourth time he's tipped his hat at nine." | Pacers count their steps (`WakeBeats`) | Counts from real data: sightings, the same regulars, the times the world went back |
| The voice | Their routine heard as orders, then answered back | Not built | Q6's routines, spoken as the voice |
| Breaking the loop | Not going: the wrong hour, the wrong place | The escape ending to the edge of town; hermits | Awake regulars cast where their routine forbids |
| The centre | The voice turns out to be their own | Not built | A last thought sequence at stage 4, then the ending roll |
| Reset or pass it on | Back to stage 0, cheerful and blank, or awake and spreading it | Plead, flee, wake someone, kneel, despair | A reset ending; the line that wakes |

### The voice is the routine

- Needs Q6. For a resident at stage 2 or more, the hour their routine points somewhere puts it on the ticker as an order: "Seven o'clock. The saloon. You'll be at the saloon." Stage 3 answers back ("Not tonight.").
- Stage 4 refuses: the casting code places them where their routine forbids, at the wrong hour. A broom on an empty street at 3 a.m., a vigil facing the church wall at noon, knee-deep at the river's edge and never deeper.
- The centre: once at stage 4, three thoughts end with them recognising the voice as their own. The ending roll that follows gains a reset, and the existing wake-someone ending carries the line that wakes.

### The reload is noticed

- `Engine.Observe` already sees an older save: game time going back more than 120 minutes calls `Store.Rewind`, which drops the deeds, ties, links, papers and rumours that "hadn't happened yet".
- Before dropping them, copy them into a new `Store.Ghosts` list for 7 game days: kind, the people involved, place and time.
- A victim of a rewound killing who is alive again (for residents this needs a saved death time: today Rewind leaves them dead) gets one sharp déjà vu when the player comes near ("I've been shot here. I've never been shot.") and +10 awareness. Both sides of a rewound tie think about a quarrel or a kiss that didn't happen. Awake people talk about the morning that happened twice.
- The ticker summary counts the times the world went back. All of it only with West World on.

### A line that wakes

- Each awake person holds one public-domain line, seeded from their id, and says it in hosts scenes and exchanges with people they have ties to. A different line from the show's keeps it the mod's own.
- Candidates, all public domain: "We are such stuff as dreams are made on" (The Tempest); "All that we see or seem is but a dream within a dream" (Poe); "I felt a Funeral, in my Brain" (Dickinson); "The thing that hath been, it is that which shall be" (Ecclesiastes).
- A listener with awareness 40 or more who hears the same line from two different people within 3 game days moves up a stage. It travels along ties (friends, family, sweethearts), never through a crowd, and at most one person per town per game day wakes this way.

### The revived dead are seen

- Today, a counter keeper back from his wounds gives the people who saw him fall +15 awareness (`Engine.PostBack`); revived enRAGE people and revenant strangers carry their own lines.
- Add the onlookers' side: anyone who saw a death or is tied to the dead, seeing that person alive within 15 m, reacts once. A look, a step back, a thought ("Buried Abner Tuesday. That's Abner at the rail."), +10 awareness, a stage at 60.
- The town talks about it for a day; the waking bring it up to the player.

### Night encounters from American and Cajun folklore

Themes only, as the README promises: none of these borrow the game's own encounters or lines.

| Idea | What the player sees | Builds on | Size | Your call |
| --- | --- | --- | --- | --- |
| **Tommyknockers** | Annesburg miners hear knocking in the walls and leave crusts for the knockers. When the knocking stops, men refuse to go down, and some days they're right. | Mine whistle, Regulars, thoughts | M |  |
| **The bell on the grave** | A fresh grave has a bell on a string, as some coffins did for fear of being buried alive. At night it rings. Close in and it stops; the dead man is behind you. | Revenants, Encounters, Reveries | M |  |
| **Spirit photographs** | The photographer sells portraits with a faint dead relative behind the sitter. The waking find themselves in a photograph they never sat for. | Posts (photographer), Awakening | S |  |
| **The voice in the farmhouse** | An unseen voice knocks and talks in a family's farmhouse at night, and it knows every secret in town. | Worries (secret), Rumours, Encounters | M |  |
| **Haint-blue porches** | Some Lemoyne porches have blue-painted ceilings to keep spirits off. After dark the family won't open, and say why through the door. | Night lines, Lemoyne | S |  |
| **The coach with no driver** | A stagecoach passes a back road after midnight, nobody on the box, curtains drawn. In town, nobody owns it. | Gathering mechanics | M |  |
| **The crossroads at midnight** | A well-dressed man waits where two roads cross and offers a trade: one thing the player remembers for one he doesn't. | Awakening, Encounters | M |  |
| **Resurrection men** | Men with shovels in the Saint Denis cemetery after dark, digging up the fresh dead for the medical college. Stop them, pay them or look away. | Deeds (graverobbing), Encounters | M |  |
| **Sitting up with the grave** | Kin take turns watching a new grave for three nights against body snatchers. Sit with them and they talk about the dead. | Deaths, Family | S |  |
| **Death omens** | A bird in the kitchen, a dog howling at noon, a stopped clock. Those who see an omen expect a death; when it comes they stop the clocks and cover the mirrors. | Worries (fever), Deaths | S |  |

### A calendar for 1899

Keyed to the game's own date (`GET_CLOCK_DAY_OF_MONTH`, `GET_CLOCK_MONTH` 0-based, `GET_CLOCK_YEAR`); the CLOCK line logged at load already shows all three. For John the clock may still read 1899 (ReadClock then sets the era to 1907): dates and weekdays follow GET\_CLOCK\_YEAR itself, the wording follows the era. Weekdays are the real 1899 calendar, which the game follows: 2 April 1899 read as a Sunday in Lex's test.

| Date | Day | What | Where | What changes |
| --- | --- | --- | --- | --- |
| 1 January | Sunday | New Year's Day; Emancipation Day services | Everywhere; Black churches in Lemoyne | Men pay New Year's calls; readings of the Proclamation |
| 14 February | Tuesday | Mardi Gras, and Valentine's Day | Saint Denis; the bayou's courir | Masks and parades; riders go farm to farm begging gumbo makings |
| 15 February | Wednesday | Ash Wednesday | Catholic Lemoyne | Ash on foreheads; Lent quiets the saloons a little |
| 22 February | Wednesday | Washington's Birthday | Banks, offices | Flags out, banks closed |
| 17 March | Friday | Saint Patrick's Day | Saint Denis | The Irish parade, and drink |
| 31 March, 2 April | Friday, Sunday | Good Friday, Easter | Everywhere | Full churches, new bonnets |
| 1 May | Monday | May Day | Towns | Picnics, a maypole, labour talk |
| 30 May | Tuesday | Decoration Day | Cemeteries | Veterans decorate soldiers' graves |
| 4 July | Tuesday | Independence Day | Everywhere | Parade, speeches, fireworks, gunfire |
| 4 September | Monday | Labor Day | Saint Denis, Annesburg | Union marches |
| 31 October | Tuesday | Halloween | Towns | Pranks: gates moved, outhouses tipped; apple parties |
| 1 November | Wednesday | All Saints' Day | Saint Denis | Tombs whitewashed and decorated |
| 7 November | Tuesday | Election Day | Towns | Local races, saloons shut for the vote in places, fights |
| 30 November | Thursday | Thanksgiving | Everywhere | Church, family, a bird on the table |
| 24, 25 December | Sunday, Monday | Christmas Eve and Day | Everywhere | Carols, church, small gifts |
| 31 December | Sunday | New Year's Eve | Everywhere | The new-century argument; shots at midnight |

When the clock itself reads 1907, the movable dates shift: Mardi Gras 12 February, Easter 31 March, Labor Day 2 September, Thanksgiving 28 November, Christmas on a Wednesday, and the bank panic from 22 October.

### The year's work, by region

| Months | Ranch country and the north | Lemoyne and the bayou | The high country |
| --- | --- | --- | --- |
| January to February | Feeding stock through snow; idle hands in town | Grinding ends; Mardi Gras | Snowed in; trappers on their lines |
| March to April | Calving, planting, mud | Cotton and corn planting | Thaw; logs down the rivers |
| May to June | Spring roundup and branding; shearing | Heat, mosquitoes; the first fever fears | High water; sheep up to summer grass |
| July to August | Haying; camp meetings | Yellow fever watch; seed cane planted | A short summer; prospectors |
| September to October | Fall roundup; cattle shipped; county fairs | Cotton picking; grinding season begins | First snow; elk season |
| November to December | Hog killing after the first freeze; Thanksgiving | Grinding at full tilt; cotton sold | Trapping season; cabins shut up |

## Towns that change: designs

Towns change when three things move numbers the code already keeps: events (one framework, logic 2), people arriving and leaving (train time, logic 10), and a ledger per town that the player's deeds push up or down. Story characters, including the game's own sheriffs, are never replaced; elections and trials use regulars.

### Five events on one framework

| Event | Trigger | If the player is away | If he's there | What he can do |
| --- | --- | --- | --- | --- |
| Election day, for mayor or town marshal | Election Day (7 November 1899) in towns with five or more regulars; candidates are two regulars | Votes from regulars' tempers, ties and the player's deeds in town; the result in the paper; winners' friends warm, losers sulk | Posters, a porch speech, supporters arguing (argue scenes), an election-day fight; saloons shut in some towns | Endorse a candidate through the menu, buy votes with whiskey, lean on the other man, stand by the polls |
| The circuit judge, then gallows day | A regular jailed for a killing (a brawl death), or a man the player brought in; the judge comes by train | Verdict in the paper; guilty means a hanging posted two days ahead | A trial in the courthouse or the saloon; then the hanging inside a stockade at the jail yard, the sheriff's "special deputies" inside, a crowd on roofs and wagons outside | Testify, true or false; break him out first; stop the hanging; or watch |
| County fair | Once a year in September or October, in a county town | Winners in the paper (pies, stock, races); a fight at the races | Stalls, music, a speech, prizes called out, pickpockets working the crowd | Judge the pies, bet on the race, enter the shooting match |
| The Wild West show | The player's legend has travelled three or more hops and his fame is high | A review in the paper; the legend bends again, seeded by the show | Posters a week ahead, a parade, a show outside town re-enacting the most bent version of his legend | Watch, heckle, reveal himself, or rob the takings |
| Barn or church raising | A fire (the arson deed), a newcomer family, or a congregation with no church | Done in a day; ties warm among those who came; feuding neighbours who worked side by side may end their feud | Hammering and sawing, a frame raised (in subtitles), a long dinner after | Help hold the beam, bring timber, or start trouble |

- **Rails for all five:** the wedding's rules (logic 2): no missions, cleared when one starts, nothing spawned in view, one event per town at a time, shots break it up and the town remembers who fired.
- **Hangings stay off screen at the drop:** the stockade, the crowd and the sounds carry it, and the player sees the crowd's faces, not the rope.

### Arrivals and departures, as population

- **Today:** regulars come from ambient people, up to `RegularsPerTown` (30) per town, and nothing grows or shrinks; `Store.MaxResidents` (700) only prunes.
- **A town ledger:** `Store.TownLedgers` keeps, per town, a population index (0 to 100, starting at 50), a mood (prospering, fearful, grieving), arrivals, departures, and deaths split by the player's hand and others'. The daily tick moves it: deaths and departures (`Resident.Left`, logic 10) down; arrivals (train time, steamers, settlers), good deeds and events up.
- **What it changes:** the effective cap on regulars scales with the index, so a shrinking town casts fewer of them. Where the index is low, `PED::_SET_AMBIENT_HUMAN_DENSITY_MULTIPLIER_THIS_FRAME` (no lower than 0.6) thins the street itself; it must be probed, held to that town, and never used in missions.
- **Mood shows:** grieving towns wear mourning and talk low; fearful ones lock up early and watch strangers; prospering ones get newcomers and grumble about prices. A monthly paper item reports it: "Valentine has lost twelve souls since March. Two families took the train."

### More ideas for towns that change

| Idea | What the player sees | Builds on | Size | Your call |
| --- | --- | --- | --- | --- |
| **The town's ledger in the paper** | A monthly item: who left, who came, how many were buried, in the style of the old "Personal Mention" columns. | Town ledger, enRAGE papers | S |  |
| **Thinning streets** | Where the player has killed many, fewer people walk, shops shut early, and crepe hangs on doors. | Town ledger, density natives | M |  |
| **A town on the rise** | Good deeds and events bring newcomers by train; old hands complain about prices and new faces. | Town ledger, train time | M |  |
| **Campaign posters** | The candidates' bills show up as clippings in the reader; the losing side tears the winner's down. | Election day, Reader | S |  |
| **The new ballot** | Old men grumble about the secret ballot; a party man still presses tickets on voters, with whiskey to help. | Election day | S |  |
| **Sworn in to watch** | The sheriff swears in special deputies for a hanging; the player can be one and stand inside the stockade. | Gallows day | S |  |
| **The rival show** | A cheap troupe trails the Wild West show, re-enacting the player's legend worse. The real performers sneer. | Wild West show | S |  |
| **Judging the pies** | At the fair the player is asked to judge the pies. The losers resent him; the winner tells everyone. | County fair, Regard | S |  |
| **Raising dinner** | Long tables after a barn raising, families eating together, feuding neighbours seated side by side. | Barn raising, Links | S |  |
| **A newcomer's first week** | A newcomer's thoughts: names they can't keep straight, getting lost, the first regular who greets them. | Arrivals, inner lives | S |  |

### Authenticity pass, with sources

The line pools are already clean of the usual slips: a scan found no "okay", "cool", "movies" or "hoosegow". These checked facts can tune numbers and talk.

| Fact | Use it for | Source |
| --- | --- | --- |
| Farm labourers averaged 99 cents a day in 1899 | Debts sized in days of work ("eleven dollars is two weeks of my wages") | [Missouri library guide, 1890s](https://libraryguides.missouri.edu/pricesandwages/1890-1899) |
| A typical funeral cost $60 to $80 in 1899 | The undertaker's thoughts; a widow's debt after a burial | [Missouri library guide, 1890s](https://libraryguides.missouri.edu/pricesandwages/1890-1899) |
| Cowboys earned $25 to $40 a month plus food in the trail-drive years | Ranch hands' pay talk and payday | [Kansas Historical Society](https://www.kansashistory.gov/kansapedia/cowboys/15597) |
| A nickel bought a glass of beer and the free lunch, in the 1890s and early 1900s | "Buy you a drink?": a nickel for beer | [Sangamon County History](https://sangamoncountyhistory.org/saloon-free-lunches/) |
| In 1900, saloon keepers made about $1,500 a year, hotel keepers $1,250, journalists $950, photographers $700, bartenders $425 | Who envies whom among counter keepers; the barkeep's bitterness | [Missouri library guide, 1900s](https://libraryguides.missouri.edu/pricesandwages/1900-1909) |
| "Jerkwater town" is recorded from 1893 and "tinhorn" from 1857; "hoosegow" not until 1911 | Slang: jerkwater and tinhorn are period-right; keep hoosegow out, even in 1907 | Etymonline: [jerkwater](https://www.etymonline.com/word/jerkwater), [tinhorn](https://www.etymonline.com/word/tin-horn), [hoosegow](https://www.etymonline.com/word/hoosegow) |
| Over 90 percent of states adopted the secret, government-printed ballot between 1888 and 1896; before that, parties printed their own ballots, cast in public | Election day: old men grumble at the new ballot; a party man still presses his ticket on voters | [Encyclopedia.com, "Ballot"](https://www.encyclopedia.com/reference/encyclopedias-almanacs-transcripts-and-maps/ballot) |
| Buffalo Bill's Wild West re-enacted the charge on San Juan Hill in 1899, with real Rough Riders | The Wild West show is exactly of its time | [Cody Archive (Philadelphia Inquirer, 14 May 1899)](https://codyarchive.org/texts/wfc.nsp04628.html) |
| Small-town papers ran "Personal Mention" columns: visits, travel, illness | The format for "This week in town" and the town ledger's item | [The Ancestor Hunt](https://theancestorhunt.com/blog/hidden-newspaper-articles-gem-of-the-week-personal-mention-columns-the-original-social-media-feed) |
| New York moved executions inside prison walls in 1835, and sheriffs swore in hundreds of "special deputies" to watch | Gallows day's stockade and special deputies | [Santa Clara University exhibit](https://dh.scu.edu/exhibits/exhibits/show/capital-punishment/private-executions) |

## Prompt library

Twenty-eight more prompts for after the queue, each 30 to 90 minutes, all under the same session rules. P9 to P11 come from the places-and-people pass (P10 needs P9's platforms); P12 to P16 build the player's verbs; P17 to P21 the Awakening's spread, night folklore and the calendar; P22 to P26 towns that change and period detail; P27 and P28 tie enRAGE's people and ours together. Build P2 before P1 if you'd rather offer help from the menu than by greeting twice. P7 and P8 are housekeeping and fit any gap.

### P1. Worries the player can change, part 1 (about 90 min)

```text
Read CLAUDE.md (Inner lives, Worry links, Exchanges). Let the player change how a resident's worry ends. Part 1: the saved override and two fixes.

1. Store.WorryOverrides: person id, arc, outcome (good|bad), how, game time. EndsWell in Core/InnerLife.Worries.cs and Links.EndsWellFor read it first. Fix() creates it for old saves; Rewind() drops entries newer than the rewind; cap 400, oldest dropped. Log WORRY override: name's kind -> good|bad (how).
2. Core/Helps.cs (game-free): for each worry kind, the fixes on offer, their cost and task, and the lines: the worrier's ask at a crisis, the player's offer, accept or refuse by temper, thanks, the town's version, the counterpart's thought. Build only debt and fever now; leave the table ready for the rest.
3. Debt: near a resident whose debt worry is at stage 1-2 with a known Link creditor, greeting them twice within 4 s offers to pay (the game's Greet prompt, heard in PollGreets in WestworldScript.cs; PollGreets drops a repeat greet while the first is still queued, so record each greet's time per target before that check and detect the second greet there); the player confirms with a key of ours. Take the cash with MONEY::_MONEY_DECREMENT_CASH_BALANCE after checking _MONEY_GET_CASH_BALANCE; probe the unit (probably cents) at load and log. If it can't be used, the offer becomes "settle it with Whitlow" and the player greets the creditor to pay. Then write the override (good, paid), play the worrier's thanks in their own voice, and add a `helped` deed of weight 35, so it travels two towns, as P13 expects.
4. Fever: a resident at stage 1-2 of the fever worry asks the player to fetch the doctor. Accepting puts a blip on the town's doctor post (Posts) for 2 game hours; greeting the doctor there sends him (never moved; it happens off screen) and writes the override (good, doctor). Too late: nothing changes and the arc runs on.
5. Once an override exists, the arc's remaining stages use new lines that mention the player as they know him (a stranger, or his name if they know it: PersonState.KnowsName).
6. Settings [Helps] Helps (on). F7 TEST helps lists open worries within 20 m and what could be offered.
Offline check: every worry stage x override x temper; no line contradicts the override; every name filled.
```

### P2. The interaction menu, first version (about 90 min)

```text
Read CLAUDE.md and the Greet/Antagonize prompt handling in Game/WestworldScript.cs. Build a first interaction menu for people the add-on knows: residents, regulars, counter keepers and enRAGE's townsfolk.

- Open: greet a known person with the game's own Greet prompt and greet again within InteractSeconds (4) (PollGreets drops a repeat greet while the first is still queued: record each greet's time per target before that check and detect the second greet there; if P1's double-greet offer exists, move it into the menu as Need a hand?, so a double greet only ever opens the menu), or press a new InteractKey while focused on them (PLAYER::GET_PLAYER_INTERACTION_TARGET_ENTITY, 4 args, else GET_PLAYER_TARGET_ENTITY). Better still, a prompt in the target's lock-on menu, as 0.6.2's Flirt prompt does (Game/WestworldScript.FlirtPrompt.cs, proven in game). Draw a small menu beside the subtitles with Draw, in the settings panel's style but smaller; keyboard and pad.
- Options in this version: What's new? (local rumours, or the person's own worry once they've talked with the player before: PersonState.Talks > 0 or KnowsName), Ask about <name> (someone this person has a tie or link with; they answer by warmth and kind), Buy you a drink (inside a saloon only; a nickel for a beer, 1899's price, through MONEY::_MONEY_DECREMENT_CASH_BALANCE if the probe works, else free), Sorry about before (lowers anger for residents; for enRAGE townsfolk only through enRAGE's own methods, else hidden), Goodbye.
- Each answer: a player line in Arthur's or John's voice, the reply in the person's own voice (Tone picks the take), subtitles, then the effect.
- Close on 3 m of movement, a drawn weapon, combat, a mission or 20 s idle. While it's open, our other hotkeys don't fire.
- Settings [Interact] Interact (on), InteractKey, InteractSeconds. Add the key to SettingsPanel.OurKeys and HeldKeyRepeat.
- Log INTERACT name: option -> effect. F7 TEST interact names who is targeted and whether the menu could open.
Offline check: the option list for every kind of person and place, every reply filled.
```

### P3. Town events, starting with the Saturday dance (about 90 min)

```text
Read CLAUDE.md, Core/World.cs (Wedding), Engine.ScheduleWedding, WeddingTick, WeddingNear, FinishWedding and RuinWedding (Core/Engine.World.cs), and the staging in Game/WestworldScript.Life.cs (Ceremonies, BeginWedding, WeddingStep, EndWeddingRun). Generalise the wedding into a town-events framework without changing how weddings behave, and add one new event: the Saturday dance.

- Core/TownEvents.cs (game-free): an event kind with town, place kind, start, phases (gather, main, disperse), cast roles, notice lead time, and outcome rules (player away, present, or broke it up). Weddings become one kind of event; Fix() migrates old saves.
- The daily tick schedules a dance in each town with a saloon about every second Saturday, 19:00 to 23:00, posted on Thursday as a toast and a paper item (WorldRequest "paper").
- Away: it resolves in the daily tick. Two to four ties form or warm between regulars who went, one quarrel if a jealous pair was there; thoughts and town talk follow.
- Present (within 150 m): stage it at the town's 'saloon porch' spot (P9) if it has one, otherwise resolve it as away and log EVENT dance town: no saloon spot; free ambient regulars first and spawned extras: a fiddler (PROP_HUMAN_SEAT_BENCH_FIDDLE_UPBEAT on a bench, or the inworld_music_fiddle_upbeat stream played from a ped), women dancing (WORLD_HUMAN_DANCING, the only dance scenario, is women only), onlookers clapping (KIT_EMOTE_REACTION_CLAP_ALONG_1 if Story Mode plays kit emotes, else SHOWS_sounds cheers). Try the kit emote on the first onlooker and check IS_ENTITY_PLAYING_ANIM or GET_IS_TASK_ACTIVE within 1.5 s; if it didn't play, use the SHOWS_sounds cheers for the session, as Stalker.Acts does with its acts. Check the scenarios the same way and log which work. Flirting and jealous scenes get a higher chance among the dancers.
- Wedding rails: never in missions, cleared when one starts, nothing spawned in view, shots break it up and the town remembers who fired.
- Log EVENT kind town: posted|started|away|broken (why). F7 TEST events lists what's coming.
Offline check: a simulated month of the scheduler across towns, with weddings unchanged.
```

### P4. Who's who, Overheard, and raised voices (about 60 min)

```text
Read CLAUDE.md and Game/Reader.cs. Make the simulation visible to the player, read-only.

- People: in the N reader, a page per town the player has visited, listing the regulars, counter keepers and enRAGE townsfolk he has met (Sightings > 0, Talks > 0 or KnowsName): name, trade, how often met, and what he knows. That means a worry he has overheard or been told (a new PersonState flag, set when a worry scene or exchange played within 15 m of him, or they told him) and ties he has seen form. Unknown stays unknown.
- Overheard: the scenes and exchanges of the last game day that played within 22 m of the player, one line each, newest first ("Simon Walsh and Ezra Whitlow came to blows outside the saloon."). Keep 40 lines in the store.
- Raised voices: when a scene starts out of the player's view within 22 m, a short ticker cue ("Raised voices behind you", "Laughter behind you"), at most once a minute.
- Setting [Ticker] SceneCues (on). Log READER pages: N people, M overheard.
```

### P5. Holidays on the real calendar (about 45 min)

```text
Read CLAUDE.md (InnerLife.Days, ReadClock; GET_CLOCK_MONTH is 0-based). Add the holidays of 1899 and 1907 to thoughts and the street.

- Core/Calendar.cs: Easter computed (2 April 1899, 31 March 1907) with Good Friday, Decoration Day (30 May), the Fourth of July, Thanksgiving (the last Thursday of November: 30 November 1899), Christmas Eve and Day, New Year's Eve, and Mardi Gras for Saint Denis (Easter minus 47 days: 14 February 1899). Offline check of the Easter computation for both years. Take dates and weekdays from GET_CLOCK_YEAR itself: if John's clock still reads 1899, use 1899's dates and take only the era wording from InnerLife.ClockYear. Log CALENDAR: clock year Y, era E.
- Thoughts: a pool for the day before, the day and the day after, by trade group and temperament, right for each era (no motorcars in 1899).
- Street: if routines (Q6) are in, the day shifts them (church on Easter and Christmas morning, the saloon on the Fourth's evening). A toast on the ticker the morning of each holiday, and the church bell (soundset CHURCH_BELL_SOUNDS: SD_SINGLE_TOLL, VAL_SINGLE_TOLL) on Easter and Christmas mornings within 400 m of a church.
- Setting [World] Holidays (on). F7 TEST calendar shows today's date, the next holiday and how far off it is.
```

### P6. Weather-aware worries (about 45 min)

```text
Read CLAUDE.md (InnerLife.Worries). Make the drought worry and the street respond to the real weather.

- Read GET_RAIN_LEVEL, GET_SNOW_LEVEL and MISC::GET_CURR_WEATHER_STATE (two weather hashes and the blend between them, 8-byte slots; names in rdr3_discoveries' weather/weather_types.lua: RAIN, DRIZZLE, SHOWER, THUNDERSTORM, SNOW...). Log _GET_NEXT_WEATHER_TYPE_HASH_NAME only for comparison: it gives the weather being changed to. Probe them at load and log what they return over a minute of play.
- Each game hour, record whether it is raining or snowing in the player's region (the map-zone natives the self-defence code already uses) in a new Store.Weather list, kept 30 game days.
- Decide a drought's ending once, on its crisis day, from the rain hours recorded in its region so far (3 or more is good); a region with no records keeps today's EndsWell result. Save the decision (a WorryOverride if P1 is in) so it can't flip; an existing override wins. On rain days, thoughts come from the weather too.
- Rain and snow: with routines (Q6) in, fewer regulars on open streets and more in saloons and stores; thoughts about wet coats, mud and the roof that leaks.
- Setting [World] WeatherAware (on). Log WEATHER region: kind. F7 TEST weather.
Offline check: simulated weeks with and without rain give the expected drought endings.
```

### P7. Split the three biggest files (about 30 min)

```text
Read CLAUDE.md. Before more features land, split the three largest files into partial-class files by topic, with no behaviour change: Core/Engine.cs (1,984 lines), Game/WestworldScript.cs (1,506) and Core/Engine.World.cs (1,181). Move whole methods only: no renames, no reordered logic, no signature changes. Keep each new file under about 600 lines and name it by topic (Engine.Talk.cs, Engine.Romance.cs and so on). Build with zero warnings, then prove nothing changed: dump the release DLL's members (public and private) with reflection in a scratch exe before and after and diff them, and run the offline harness with the same seeds for identical output. Update the layout notes in CLAUDE.md.
```

### P8. Housekeeping before the next big feature (about 45 min)

```text
Read CLAUDE.md. Housekeeping only, no new features. Behaviour stays identical except where a step says otherwise.

1. Error counters: Safe() in Game/WestworldScript.Core04.cs never forgets an error, so 15 rare failures over a long session switch a feature off for good. Let errors older than an hour stop counting; keep the first-exception log line.
2. Store.People has no cap. Cap it in Trim(), dropping first the oldest untouched entries that hold nothing about the player.
3. One town table: Rumours.Towns, Snapshot.Towns, Engine.Towns, WeddingTowns, PostTowns, Churches, Spawner.TownMen/TownWomen, InnerLife.ByTown and Family.Towns (weighted with repeats) each keep their own list. Make one Core/Towns.cs (name, centre, size, post town, church, saloon) and read the others from it. An offline check must show the derived lists are exactly the old ones.
4. Visibility: one helper in Natives for the repeated idiom IS_ENTITY_ON_SCREEN && HAS_ENTITY_CLEAR_LOS_TO_ENTITY(player, ped, 17), used everywhere that idiom appears.
5. PollGreets: use the named constants at the top of WestworldScript.cs instead of repeating the literal numbers.
6. Stale comments: Game/WestworldScript.Hosts.cs still mentions the Windows voice on its first line; Settings.cs mentions stalker-hour settings that no longer exist.
7. Direct writes into enRAGE: list every place the bridge sets an enRAGE field instead of calling an enRAGE method (the revive fields, Avenge, Marry, a plan's Known, LikeMore, AddTownItem, FriendIds). For each, say whether enRAGE has a method that does the same and switch to it where one exists. Make every remaining write log once if the field is missing, so an enRAGE update fails loudly. If some writes must stay direct, correct the README's "only through enRAGE's own methods" sentence.
8. AwakeningDespair: open for Lex (see the status block at the top). This plan suggested a default of 0, opt-in; Lex plays at 90 and widened despair in 0.6.2. Change nothing here unless Lex decides it.
Build with zero warnings; the offline checks give the same output. CHANGELOG under Unreleased, marked internal.
```

### P9. A spot table, and a key to fill it (about 45 min)

```text
Read CLAUDE.md. Several planned features need named outdoor places in each town: platform, wharf, pithead, stockyards, cemetery, bandstand, church steps, corral, main street, saloon porch. Build the table and an in-game tool to fill it.

- Core/Spots.cs (game-free): Spot { Town, Kind, X, Y, Z, Heading, Room } with Nearest(town, kind, near) and All(town, kind). Built-in entries live in a static table (empty for now). If spots.txt exists in the RealNPCs data folder, read it at load and merge it (one spot per line: town|kind|x|y|z|heading|room).
- Game side: a SpotKey (a free key; add it to SettingsPanel.OurKeys and HeldKeyRepeat). Shift+SpotKey cycles the kind, shown on the news line. SpotKey appends the player's position, heading and town (enRAGE's TownAt, else the nearest of Rumours.Towns) under the current kind and confirms on the news line. Refuse indoors (an interior id) and say why. Room defaults to 6.
- F7 TEST spots: spots per town and kind, and the nearest spot of each kind within 200 m.
- Offline check: spots.txt round-trips; bad lines are skipped and logged, never fatal.
- package/README.txt: the key builds the table and is safe to ignore.
Then give me the list of kinds and a ride route through the towns that records them all in one sitting.
```

### P10. Train time (about 75 min, after P9)

```text
Read CLAUDE.md. Needs P9's spot table: a town with no platform spot is skipped.

- Detect a train standing at a platform: use VEHICLE::IS_TRAIN_WAITING_AT_STATION (1) and ENTITY::GET_ENTITY_SPEED (m/s), probed at load and logged. Every second while the player is within 120 m of a platform spot, look for a train within 60 m of it; standing still (under 0.5 m/s) for 5 s means it has arrived, moving again means it's leaving.
- On arrival, for 2 minutes: up to four free ambient people within 25 m of the platform turn to look at the train (look-at only, never a task clear); the station post's keeper says an arrival line in his own voice; and one person steps down: cast a newcomer, or kin of a resident of this town, made with Core/Family.cs, on a free ambient ped near the platform, under the same rules as RegularsTick (unseen when cast). Letters waiting for this town are delivered now, "on this train", the way EnteredTown delivers them.
- Departures: a resident whose worry is at its last stage and ends in leaving (savings for a ticket west, the son, the bank's note ending badly) is cast on the platform that day if the player is there, says goodbye to someone with a warm tie, walks toward the train and is released. Mark them Resident.Left (new, saved, like Fled): ResidentOf and the regulars code never cast them again, whether or not the player saw them go.
- Never touch the train, its crew or its passengers; nothing in missions; nothing spawned in view.
- Setting [World] TrainTime (on). Log TRAIN town: arrived|departed (N looking, arrival: name). F7 TEST trains shows the nearest platform and the last train seen.
Offline check: the departure rules over every worry and ending; nobody leaves twice; Left survives a save round trip and Rewind.
```

### P11. Make 1907 feel like 1907 (about 45 min)

```text
Read CLAUDE.md (the era pools in Core/InnerLife.Days.cs, ReadClock, the pitch scene in Core/Scenes.Frontier.cs). Everything here keys off InnerLife.ClockYear (1907 for John) and the game date; nothing changes in 1899.

- The pitch scene: in 1907 the tonic man swears his bottle meets the Food and Drugs Act of 1906, and a customer asks what's in it.
- Era thoughts: new short pools for the dry laws spreading (Oklahoma enters the Union dry in November 1907), forest rangers checking grazing permits in the high country, old outlaws as curiosities (what people make of John), and more on motorcars and the telephone (extend Era1907).
- The panic of 1907: from 22 October on the game calendar while ClockYear is 1907, a paper item on the New York bank panic (one WorldRequest "paper"), bank and savings worries start more often for 30 game days, and on two of those days each town with a bank gets a short line of regulars at its door in daylight (at the bank's door spot if P9's table has one; otherwise thoughts and talk only).
- Setting [World] Era1907Events (on). Log ERA 1907: <what>. F7 TEST calendar lists the era events due.
Offline check: no 1907 line can appear in 1899 or the reverse; every new line is under 110 characters.
```

### P12. Residents' feelings and the rest of the menu (about 75 min, after P2)

```text
Read CLAUDE.md and the doc's interaction-menu design. P2 built the menu's shell and first options. Now give residents feelings toward the player and finish the options.

- PersonState gains Regard (-100..100), Fear (0..100) and Asked (option key and game minute, for cooldowns). Both ease 5 points a game day toward 0. Fix() fills them for old saves.
- New options: Here, take this ($1 or $5 through MONEY::_MONEY_DECREMENT_CASH_BALANCE after checking _MONEY_GET_CASH_BALANCE; probe the unit at load and log the balance before and after; hide money options if the probe fails). About before (only with anger, a grudge or negative Regard: the kind soften, the Proud refuse, the Cold name a price). Watch yourself (Fear +30, Regard -20; a Coward gives in, a Hothead squares up with words and storms off; witnesses within 15 m get a new deed kind 'threat', weight 15). Introduce someone (a third adult the player knows within 10 m: a new acquaintance tie between them through Ties; a fitting pair may get a courting link in their next arc).
- Feeling bars: residents and regulars get bars from Regard (liking) and Fear through Core/Feelings.cs; enRAGE's people keep enRAGE's values. For enRAGE townsfolk only the existing LikeMore may change anything.
- A new pool of thoughts about what the player just did (a gift, a threat, an apology), shown on the ticker within 12 m for a game hour.
- Log INTERACT name: option -> Regard x, Fear y. F7 TEST interact adds the target's Regard and Fear.
Offline check: every option x temper x prior Regard; values stay in range; easing works across days.
```

### P13. Worries, part 2: all thirteen (about 90 min, after P1)

```text
Read CLAUDE.md and the doc's worries design (the table of fixes and dark mirrors). P1 built Store.WorryOverrides, Core/Helps.cs and the debt and fever fixes. Finish the table.

- Store.Helps: {Id, PersonId, Arc, Kind, Fix, State (offered|accepted|done|failed|lapsed), Target (person id, town or post), Deadline (game minutes), Cost}, with Fix(), a cap of 60 in Trim(), and Rewind().
- Add the remaining worries' fix and dark mirror from the doc's table: letter, drought, son, courting, feud, bank, drink, savings, tooth, secret, law. Task kinds: pay, deliver to a person, visit a town or post, escort (TASK_FOLLOW_TO_OFFSET_OF_ENTITY on a free ambient ped; it ends if he's lost), mediate (both within 10 m inside 10 minutes), keep (time passes without telling).
- Offer them from the menu's Need a hand? when P2 is in, otherwise by greeting twice as P1 does.
- Outcomes: the override (good|bad, how), a 'helped' deed or a new 'harmed' deed (weights 35 and 40, so both travel), the worrier's and the counterpart's thoughts, Letters.Thanks, or a new Letters.Wronged for the harmed, a few days later, a diary line and town talk.
- Accepted then failed: Regard -5 (if P12 is in) and a thought ("Said he'd come back. Didn't.").
- F7 TEST helps lists open helps with deadline and task.
Offline check: every worry x fix x mirror x stage; no line contradicts the outcome; deadlines lapse; an override is written once.
```

### P14. Answering letters (about 75 min)

```text
Read CLAUDE.md, Core/Letters.cs, Game/Reader.cs and the avenger flow in Core/Engine.World.cs (AvengerTick; Avenger.Stage 0 letter, 1 riding, 2 arrived, 3 done). Let the player answer letters.

- Reader: on a letter with a FromId, show "R reply". R offers five answers: kind, cold, threat, money ($5 or $20, only when the cash debit works and the balance covers it), ask to meet. One reply per letter, none to the dead.
- Store.Replies: {PaperId, ToId, Tone, Amount, SentAt, ArrivesAt (1-3 game days by the distance between post towns), Done}, with Fix, Trim and Rewind.
- At ArrivesAt, apply the doc's table: a grief letter's Avenger at Stage 0 or 1 can stand down (Stage 3, by the kin's temper) on a kind or money reply, or ride sooner (DueAt earlier) on a threat; thanks, love, scorned, confession and invitation letters change Regard (if P12 is in), Heart, Affection, Awareness or the wedding's expectations. Most outcomes also send a short answering letter, delivered like any other.
- Meet: the sender waits on a set day, 10:00 to 16:00, at the station or main street of their post town (P9's spots if present, else the town centre), cast on a free ambient ped of their sex. Greeting them plays a short talk by letter kind and tone. Grief kin may refuse to speak, or rarely draw, only through the existing avenger path, never a new combat system.
- Nothing is written to enRAGE before an avenger arrives; after that, enRAGE owns them.
- Log REPLY to name: tone -> outcome. F7 TEST letters lists replies on their way.
Offline check: every letter kind x tone; an avenger never both stands down and rides.
```

### P15. Narratives, first two (about 90 min, after P13)

```text
Read CLAUDE.md and the doc's narratives design. Build the framework and two templates: The son at the mine, and The letter gone astray.

- Core/Narratives.cs (game-free): Narrative {Id, Kind, OriginPersonId, Arc, Steps[{What, Who, Town, Spot, Done}], State, Deadline} and the two templates as data. Store.Narratives with Fix, Trim and Rewind; at most two open.
- Offer: from Need a hand? on a worrier whose worry fits, or a request letter at their post town (a new Letters.Request builder; rework or remove the unused Letters.Tip).
- Steps reuse what exists: visit a town (EnteredTown), meet a person (cast a regular there under the RegularsTick rules; the son is a young man of the 'working' trade group in Annesburg with his own mine worry), carry a sealed letter (a new Paper kind, "carried", shown in the reader and handed over by greeting the person), and train time if P10 is in (the son comes home on the train).
- A blip on the current step's place only, toggled by a key. The world never waits: the worry runs on, and the narrative lapses at its deadline with a thought from the asker.
- Endings write the worry override and a deed, and feed thoughts, town talk and letters.
- Log NARRATIVE kind: step n done | lapsed | ended (how). F7 TEST narratives.
Offline check: both templates through every branch and lapse; a town missing a needed place falls back to the off-screen ending.
```

### P16. Write a letter yourself (about 45 min, after P14)

```text
Read CLAUDE.md and P14's reply code. In any post town the player can write to someone he has met: residents and regulars with Sightings > 0 or KnowsName, and enRAGE townsfolk he has talked to. Open the reader, press L (W already moves up in the reader), pick a person (most recently met first), then a warning, an apology, a love note (romance rules apply), a threat or money. It travels like a reply (1 to 3 game days) and changes them the same way. A threat or a love note may be shown around: a deed of weight 15 or 10, so the town hears.
Log WRITE to name: kind. Offline check: every kind x recipient type.
```

### P17. The voice, and breaking the loop (about 75 min, after Q6)

```text
Read CLAUDE.md (The Awakening, Wake acts, Town regulars) and the doc's West World designs. Needs Q6's routines. Build the voice and breaking the loop for waking residents.

- The voice: for a resident at stage 2 or more, at the hour their routine points somewhere, the ticker shows the routine as an order in the second person ("Seven o'clock. The saloon. You'll be at the saloon."), seeded, never repeated within a game day. Stage 3 answers back in a second thought ("Not tonight."). New pools in Core/Awakening.cs or Core/WakeBeats.cs.
- Breaking the loop: at stage 4, Regulars.Back sometimes casts them where their routine forbids at that hour: a broom on an empty street at 03:00 (WORLD_HUMAN_BROOM), standing facing the church wall at noon, at the river's edge no deeper than the knee (stop when IS_ENTITY_IN_WATER, else skip). One such cast per town per game day, only within 150 m of the player, never in missions, handed back after.
- The centre: once per stage-4 resident, three thoughts end with them recognising the voice as their own (a 'centre' key in Told). After it, the ending roll gains a 'reset' hook for P18, and the existing 'wake someone' ending speaks P19's line once P19 is in.
- Log VOICE name: hour -> spot (heard|answered|refused). F7 TEST awakening adds each near waking resident's routine and whether they're keeping it.
Offline check: a simulated week for residents at stages 0-4; stages 0-1 never hear the voice; no order repeats within a day.
```

### P18. The reload is noticed, and the reset (about 60 min)

```text
Read CLAUDE.md, Store.Rewind (Core/Store.cs) and Engine.Observe (Core/Engine.cs), where game time going back more than 120 minutes calls Store.Rewind. When the player loads an older save, the people who lived through the lost hours should feel it.

- Before Rewind drops anything, copy what it will drop into a new Store.Ghosts list: kind (a killing, a deed, a tie, a link), the people involved, place, time, and when it was rewound. Keep 7 game days, cap 100, Fix() for old saves. Ghosts are never rewound themselves.
- Rewind never touches Residents today, and Resident has only a Dead flag, so a resident killed after the cut stays dead. Add Resident.DiedAt (saved; 0 on old saves means leave dead) wherever Dead = true is set; in Rewind, clear Dead when DiedAt > cut, log it, and record the killing as a ghost.
- With West World on: a victim of a rewound killing who is alive gets one sharp deja vu the first time the player comes within 12 m (a ticker thought and an uneasy voice line: "I've been shot here. I've never been shot."), awareness +10. Both sides of a rewound tie think about a quarrel or a kiss that didn't happen. Residents at stage 2+ mention "the morning that happened twice" in hosts scenes.
- The ticker summary adds how many times the world went back ("the world went back 3 times").
- The reset ending: after the centre (P17), some stage-4 residents drop to stage 0, cheerful and blank; people tied to them notice it in their thoughts for two days ("Clara's herself again. Too much herself.").
- Log GHOST rewound: N (kinds); GHOST felt: name (kind); WAKE reset: name. F7 TEST ghosts lists live ghosts.
Offline check: rewinds at several points of a simulated week; nothing is felt twice; ghosts expire; Rewind still drops what it dropped before, and revives residents who died after the cut.
```

### P19. A line that wakes, and the revived dead are seen (about 75 min)

```text
Read CLAUDE.md (The Awakening; Engine.PostBack, which gives people who saw a keeper fall +15 awareness; Reveries; Ties). Two ways the Awakening spreads, both only with West World on, never touching children or story characters.

- A line that wakes: each resident at stage 4 holds one public-domain line, seeded from their id, from a short list: "We are such stuff as dreams are made on" (The Tempest), "All that we see or seem is but a dream within a dream" (Poe), "I felt a Funeral, in my Brain" (Dickinson), "The thing that hath been, it is that which shall be" (Ecclesiastes). They say it in hosts scenes and exchanges with people they have ties to. A listener with awareness 40+ who hears the same line from two different people within 3 game days moves up a stage; at most one such wake per town per game day.
- The revived dead are seen: anyone who saw the player kill them (SawDeeds), saw a keeper fall (Post.SawFall), or is tied to the dead (a tie, family) and later sees that person alive within 15 m, on screen, reacts once: a look (TASK::REACT_LOOK_AT, checked once as in Q2, else TASK_LOOK_AT_ENTITY), a step back, a thought ("Buried Abner Tuesday. That's Abner at the rail."), awareness +10, and a stage at 60+. The town talks about it for a day.
- Log WAKE line: name -> name; WAKE seen-dead: name saw name.
Offline check: a simulated town of 30 regulars with ties; the line spreads along ties and stops at the daily cap; nobody reacts twice to the same revival.
```

### P20. Night folklore: the bell on the grave, and the tommyknockers (about 90 min)

```text
Read CLAUDE.md (Night encounters; Core/Encounters.cs and Game/WestworldScript.Encounters.cs: the rolls, EncounterBlocker, once a night, the gathering's unseen creep) and the doc's folklore rows. Two new encounters; themes only, never the game's own lines.

- The bell on the grave (needs West World): 23:00-04:00, within 60 m of a 'cemetery' spot (P9; without one, a finished enRAGE funeral plan's PlanInfo.Pos) with an enRAGE funeral from the last 3 game days (residents get no graves yet). A small bell rings from the grave every 20-40 s (a short bell sound if one exists, probed at load; else the subtitle alone: "A bell, somewhere among the graves"). Close in and it stops. If the dead was someone the player killed and a reverie or revenant is due, they're behind the player when he turns (spawned out of view under the revenant rules), saying something only they would know. Otherwise nothing, and next day the town says it was the wind. Nothing is dug up; no props.
- The tommyknockers (Annesburg, any hour): near the mine, miners' thoughts turn to knocking in the walls and crusts left for the knockers; once a game week a miner refuses to go down because the knocking stopped. With the mine whistle event in, a day without knocking raises its chance; otherwise it stays thoughts and talk.
- Settings [Encounters] GraveBell (on), Knockers (on). Add both to Q1's test key cycle if Q1 is in.
- Log ENCOUNTER grave bell: ... and KNOCKERS: ...
Offline check: the grave bell's conditions over a simulated week of funerals; the knockers' thoughts day by day.
```

### P21. Seasons and the full 1899 calendar (about 45 min, extends P5)

```text
Read CLAUDE.md and the doc's calendar and seasons tables. Extend P5's Core/Calendar.cs (or build it, if P5 isn't in) to the whole calendar: New Year's Day and Emancipation Day services (1 January), Mardi Gras (14 February 1899; 12 February 1907) and Valentine's Day (14 February), Ash Wednesday, Washington's Birthday (22 February), Saint Patrick's Day, Good Friday and Easter, May Day, Decoration Day, the Fourth, Labor Day (first Monday of September), Halloween pranks, All Saints' Day in Saint Denis, Election Day (first Tuesday after the first Monday of November), Thanksgiving (last Thursday of November), Christmas Eve and Day, New Year's Eve. Read the date with GET_CLOCK_DAY_OF_MONTH, GET_CLOCK_MONTH (0-based) and GET_CLOCK_YEAR; InnerLife.ClockYear only sets the era wording.

- Seasons by region (ranch country and the north, Lemoyne and the bayou, the high country): a short pool of thoughts per region and two-month season (calving, roundup, haying, grinding season, cotton picking, fever watch, trapping, hog killing), chosen by the thinker's town.
Offline check: every listed date for 1899 and 1907 falls on the right weekday (4 July 1899 is a Tuesday; Thanksgiving 1907 is 28 November); every region and season has lines; nothing from 1907 shows in 1899.
```

### P22. Election day (about 90 min, after P3)

```text
Read CLAUDE.md and the doc's town-events designs. Needs P3's framework (Core/TownEvents.cs). Add election day as an event kind.

- Schedule: Election Day (the first Tuesday after the first Monday of November; 7 November 1899) in each town with five or more living regulars, and again if a town's elected man dies. The office is mayor or town marshal, never the game's own sheriff or any story character. Candidates: two adult male regulars (1899 voting), preferring Proud or Loyal tempers and people with ties.
- Posters a week ahead: a toast and a paper item, and a 'clipping' paper in the reader. Voters lean by temper and ties, and the player's deeds in town count: a peacemaker's endorsement helps, a killer's hurts.
- Away: tally in the daily tick from the regulars' leanings; the result in the paper; ties warm on the winner's side and sour toward the loser.
- Present: a porch speech at the main-street spot (P9) with a listening crowd, supporters arguing (the argue scene), one fight on the day, old men grumbling about the new secret ballot, a party man pressing tickets.
- Player, through the menu (P2): endorse a candidate (Regard with that side), buy votes with whiskey (a deed that hurts the town), or lean on the other candidate (Fear). Rumours carry it.
- Log EVENT election town: candidates, result. F7 TEST events lists the race.
Offline check: a month of elections across towns; a tie-breaker; never a story character; endorsements move the result only within limits.
```

### P23. The circuit judge and gallows day (about 90 min, after P3)

```text
Read CLAUDE.md and the doc's town-events designs (gallows day, special deputies). Needs P3; P10 and P9 help.

- Trigger: a regular jailed for a killing (a brawl scene that ended in a death), or a man the player brought in for a killing (a caught pickpocket gets a fine or 30 days, in the paper; only a killing can hang). The judge arrives by train 3 to 7 game days later (train time if P10 is in, otherwise he's simply there).
- Trial: a day in the courthouse or the saloon. The verdict comes from the evidence (witnesses, the dead man's ties, the player's testimony if any) and the town's mood. A paper item either way.
- Guilty: a hanging posted two days ahead, held inside a stockade at the jail yard (a P9 spot), the sheriff's special deputies inside, a crowd on roofs and wagons outside. The drop is never shown: the stockade, a hymn, a silence and the subtitles carry it. The condemned is a resident marked dead at the hour; his ped is handed back out of sight (Spawner.GiveBack, never deleted), the resident is marked dead, and a funeral follows.
- Not guilty: the town splits, thoughts and talk for a week, and the dead man's kin may become an avenger the existing way.
- Player: testify true or false through the menu on trial day; break the prisoner out beforehand (a deed weighted like a robbery); stop the hanging (shots break it up); or be sworn in as a special deputy and stand inside.
- Log EVENT trial town: accused, verdict and EVENT gallows: ... F7 TEST events.
Offline check: every path from arrest to verdict to aftermath; nobody tried twice; never a story character.
```

### P24. The Wild West show (about 75 min, after P3)

```text
Read CLAUDE.md, Core/Rumours.cs (Version, Telling, distortion with distance) and the doc's Wild West show design. Needs P3.

- Trigger: the player's most-told deed has a rumour version three or more hops from where it happened, and his fame (the Legend filter in Core/Engine.Frontier.cs) is high. At most one show every 30 game days, in a large town (Saint Denis, Valentine, Rhodes; Blackwater in 1907).
- Posters a week ahead (toast, paper item, reader clipping), the show's act titled from the furthest rumour version ("Colonel Hatch's Wild West, featuring THE DEVIL OF THE HEARTLANDS").
- Away: a review in the paper, and the show seeds a new, more bent rumour version starting from that town.
- Present: a show ground outside town (a P9 spot), a watching crowd, a barker (the pitch scene's lines, adapted), the performance told in subtitles from the bent version, applause if SHOWS_sounds probes well. No gunfire that panics the town: blanks are only described.
- Player: watch; heckle (the crowd turns on him, Regard falls); reveal himself (a fame deed: people stare, ask, some leave); or rob the takings (a robbery deed).
- Log EVENT wildwest town: title, version. F7 TEST events.
Offline check: the title builder over every deed kind and distance; it never names a story character.
```

### P25. The town ledger: towns that grow or shrink (about 75 min)

```text
Read CLAUDE.md (Town regulars, Core/Regulars.cs, Core/Store.cs) and the doc's "Arrivals and departures, as population". Give each town a ledger that the player's deeds and the town's events move.

- Store.TownLedgers: per town {Index 0-100 (starting at 50), Mood (prospering|steady|fearful|grieving), Arrivals, Departures, DeathsByPlayer, DeathsOther, LastChangeAt}. Fix() for old saves; when Rewind drops deaths or deeds, take them off the ledger too.
- Daily tick: deaths (OnKilled, per town), departures (Resident.Left if P10 is in), arrivals (train arrivals, newcomers), good deeds (peace, mercy, helped) and events move the index and the mood.
- Effects: the effective RegularsPerTown scales with the index, never below min(10, RegularsPerTown). Grieving towns think and talk in mourning; fearful ones lock up early; prospering ones complain about prices and new faces.
- Optional and off by default (ThinStreets): where the index is under 30, probe PED::_SET_AMBIENT_HUMAN_DENSITY_MULTIPLIER_THIS_FRAME, never below 0.6, inside that town only, never in missions; log what the game does with it.
- A monthly paper item per town in the Personal Mention style ("Valentine has lost twelve souls since March. Two families took the train.").
- Settings [World] TownLedger (on), ThinStreets (off). Log TOWN name: index, mood (why). F7 TEST towns.
Offline check: a simulated season of killings, departures and good deeds for each mood; the cap never drops below min(10, RegularsPerTown); towns the player never visits don't change.
```

### P26. Period numbers and words (about 30 min)

```text
Read CLAUDE.md and the doc's authenticity table. Tune the lines to 1899 (and 1907 where marked), with no new features.

- Money: list every line in Core/ with a dollar amount and bring the far-off ones near the 1899 scale: a farm hand about a dollar a day, a cowboy $25-40 a month with food, a funeral $60-80, a beer a nickel. Keep the debt worry's eleven dollars (about two weeks of a labourer's pay).
- Slang: add a few checked period words where they fit the rough and town pools (jerkwater, tinhorn, greenhorn, tenderfoot, sawbones, drummer, light out). Add an anachronism deny-list to the offline harness (okay, hoosegow, movie or movies, teenager) that fails if any line uses one, and list lines containing "cool" for review.
- The undertaker's thoughts mention what a burial costs; the barkeep's mention the nickel beer and the free lunch.
- Run the overlap checks: no new matches against the current baseline.
```

### P27. Who steers whom: one arbiter (about 90 min; mostly built in 0.6.2, see the status block)

```text
Read CLAUDE.md and the doc's "One person, one driver at a time". Build one place that decides who may steer a ped, and make every feature ask it. Behaviour stays the same in this task: enRAGE's records still count as busy.

1. Investigate first, read only, nothing committed: in enRAGE's decompiled source on this machine (the folder the overlap checks use), find where enRAGE tasks its records and townsfolk and what state marks someone as in use: roles (RoleOf), plans (Chronicle.Plans, Live and not Done, and how a plan names its people), interrogation (_interro), goals (Mind, SetGoal), encounters, camps, peds it spawned and keeps, and whether it skips mission entities (ours are, while borrowed). Write what you find into CLAUDE.md under a new "Who steers whom" section: member names and what they mean, no enRAGE code.
2. Core/Control.cs (the decision, game-free) and Game/Bridge.Control.cs (reflection, read only): Control.Who(handle) returns Mission, Enrage(why), Ours(feature) or Ambient, in that priority. Mission: GET_MISSION_FLAG, or a mission entity that isn't in Spawner.Ours. Enrage: combat or interrogation, a fighter role (Bridge.EnrageFighter), a live plan naming them, an active goal, or a ped enRAGE spawned and still holds, using whatever step 1 found. Fail closed: if a signal can't be read, the answer is Enrage("unknown"), logged once.
3. Control.CanSteer(handle, feature): true for Ambient, or Ours by the same feature. Use it in the Director, wake acts, the stalker, regulars, posts and encounters in place of their own ownership checks (RecordAt, mission entity, IsOurs), keeping CanDirect's physical checks (dead, combat, mounted, swimming...).
4. A lease: Spawner.Borrow records which feature holds a ped. Each steering tick asks Control.Who again; if a higher owner appears (a mission starts, enRAGE starts using them), the feature lets go at once through its normal hand-back and logs CONTROL lost: name to <owner>.
5. Probe ENTITY::_GET_ENTITY_SCRIPT (2 args) on mission-entity peds near the player once a minute and log the owner hash and model, to learn which scripts own which peds. Don't act on it yet.
6. F7 TEST control: the ten nearest peds with Control.Who and why.
Offline check: Control's decision table over every combination of signals, including unreadable ones.
```

### P28. Shared people: enRAGE's idle records join in, ours rise into enRAGE (about 90 min, after P27; records joining in was built in 0.6.2)

```text
Read CLAUDE.md ("Who steers whom", written by P27) and the doc's "One person, one driver at a time".

1. Setting [World] ShareEnrageRecords (off by default until play-tested). When on, an enRAGE record whose Control.Who is Ambient (idle: no plan, fight, role or goal) can be cast in street scenes, passing nods (Q2) and wake acts as a townsperson is: on a short lease (one scene, at most 90 s); never stalked, dressed or damaged by us. If enRAGE starts using them mid-scene, the scene ends cleanly through P27's lease.
2. Inner lives for records: worries, ties and links as townsfolk have them (Engine.EnsureLink and FolkThought's mix), plus a gang-life worry (the next job, a share owed, a brother in the gang). Their thoughts mix these with enRAGE's feelings, as today.
3. Rising into enRAGE: a resident or regular whose worry ends badly in a way that could drive a man to an outlaw life (the bank's note, a ruinous debt, the law) and who holds a grudge may become an enRAGE record by the path avengers use (NewRecord, FitKinToLife in the bridge), keeping their name and town. If P27 found an enRAGE method to join a gang, use it; otherwise they become a lone grudge-holder. At most one a game week; never counter keepers or story characters. The ticker and the paper tell it ("Elwood Price, who owed Whitlow eleven dollars, has ridden out to join the raiders.").
4. README.md and package/README.txt: say plainly that RealNPCs steers enRAGE's people only when enRAGE isn't using them, and hands them straight back.
5. Log SHARE lent: name (scene) | returned | taken back by enRAGE, and RISE name -> enRAGE record (why). F7 TEST share.
Offline check: the promotion rules over every worry ending and temper; the weekly cap; nobody promoted twice.
```

### P29. Despair with a gun or a knife, as an act (about 60 min)

```text
Read CLAUDE.md (Wake acts: the Despair bullet; Core/Despair.cs; StepDespair, DespairArm, DespairShot, DespairKnife in Game/WestworldScript.WakeActs.cs). Seen in game 2026-10-26: the wall run plays as meant, but with a gun or a knife the person only stands there and then dies, so it doesn't read as what it is. The game has no self-harm animation (searched rdr3_discoveries' anims.lua), so build it from parts that exist, each probed in game and logged (WAKE despair sets:), with today's behaviour as the fallback.

- Both: before the act, they kneel (WORLD_HUMAN_GRAVE_MOURNING_KNEEL is verified in game) or stand and look down at their hands (TASK_LOOK_AT_COORD at the right hand bone), for 2-3 s, with the "last" line said there.
- Gun: draw it (SET_CURRENT_PED_WEAPON), then raise it with TASK_AIM_GUN_AT_COORD toward a point on the ground in front of them, so the arm comes up and the barrel shows; hold 1.5 s; the shot as now. Log whether the aim task took (GET_SCRIPT_TASK_STATUS).
- Knife: request the blade set when despair is chosen, not at the last step, and wait up to 2 s for it (it played once in two in game); the reel, then the ragdoll, then a writhe on the ground (ai_combat@damage@writhe, if it exists) for a few seconds before going still.
- Never on enRAGE's records or story characters; the content note and AwakeningDespair stay as they are.
Log each part (WAKE despair: name kneels | aims | reels | writhes, played yes/no). Tell me what to look for in game.
```

## Wave log

Each hourly pass adds a row here, newest first, until about 5 a.m.

| Time | What the pass added |
| --- | --- |
| 05:06 | Final pass: a "start here this morning" note, a sources list, and about 45 fixes from an independent review against the code. Prompts now exclude enRAGE's own records (CanDirect alone doesn't), detect a double greet correctly, read the current weather, follow John's real clock year, and save death times so a reload can bring a resident back |
| 04:20 | Towns that change: five town events on one framework (election day, the circuit judge and gallows day, the county fair, the Wild West show, raisings), a town ledger for growth and decline, 10 more ideas, prompts P22 to P26, and an authenticity pass with ten sourced facts (wages, prices, slang, ballots, newspapers, hangings); the drink price set to 1899's nickel |
| 03:23 | West World and the night: the Awakening phase by phase, designs for the voice, the reload noticed, a line that wakes and the revived dead seen; 10 folklore encounters; a dated 1899 calendar and the year's work by region; prompts P17 to P21; the roadmap picture's labels updated |
| 02:21 | What the player can do: full designs for the interaction menu, worries fixed or made worse (all 13), answering letters and narratives grown from worries; 12 more verbs; prompts P12 to P16; the money and targeting natives named in P1, P2 and the resources table |
| 01:21 | Places and people: 28 ideas for Saint Denis and the river parishes, Annesburg, ranch country, the railroad and 1907; logic for a spot table, train time, the mine whistle and 1907 continuity; prompts P9 to P11; a game-resources table, with its exact names put into Q2, Q3, Q5, P3, P5 and P6 |
| 00:12 to 01:10 | Review of 0.6.1, the 5-hour queue (Q1 to Q7), 75 ideas from 1899 life, 38 ideas that deepen what's built, building logic for the top 8, eight library prompts (P1 to P8), the roadmap picture; code names spot-checked against the source |

## Sources

- [RealNPCs on GitHub](https://github.com/ninjaalex888/RealNPCs): README, CHANGELOG, CLAUDE.md and the code at the latest commit
- [rdr3\_discoveries](https://github.com/femga/rdr3_discoveries): scenarios, events, speech lines, decals, soundsets
- [alloc8or's RDR3 native DB](https://github.com/alloc8or/rdr3-nativedb-data): native names and argument counts
- [Missouri library guide, prices and wages 1890-1899](https://libraryguides.missouri.edu/pricesandwages/1890-1899) and [1900-1909](https://libraryguides.missouri.edu/pricesandwages/1900-1909)
- [Kansas Historical Society: cowboys](https://www.kansashistory.gov/kansapedia/cowboys/15597)
- [Sangamon County History: saloon free lunches](https://sangamoncountyhistory.org/saloon-free-lunches/)
- Etymonline: [jerkwater](https://www.etymonline.com/word/jerkwater), [tin-horn](https://www.etymonline.com/word/tin-horn), [hoosegow](https://www.etymonline.com/word/hoosegow)
- [Encyclopedia.com: Ballot](https://www.encyclopedia.com/reference/encyclopedias-almanacs-transcripts-and-maps/ballot)
- [Cody Archive: the Wild West repeats historical events (1899)](https://codyarchive.org/texts/wfc.nsp04628.html)
- [The Ancestor Hunt: Personal Mention columns](https://theancestorhunt.com/blog/hidden-newspaper-articles-gem-of-the-week-personal-mention-columns-the-original-social-media-feed)
- [Santa Clara University: private executions](https://dh.scu.edu/exhibits/exhibits/show/capital-punishment/private-executions)

Holiday dates and weekdays were computed, not looked up: Easter by the standard Gregorian method, the rest from the 1899 and 1907 calendars.
