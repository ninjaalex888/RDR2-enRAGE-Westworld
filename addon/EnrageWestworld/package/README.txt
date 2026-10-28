RealNPCs 0.6.3 - an unofficial add-on for enRAGE (formerly enRAGE Westworld)
=========================================================================

The people enRAGE keeps track of start acting like they have their own lives. They tell you about
themselves, remember what you did, think about you, keep diaries, gossip, fall in love, marry,
mourn, owe each other money, and come after you. The whole world feels it: word of what you do
travels from town to town and grows in the telling, kin you never met write to you or ride in for
revenge, strangers quarrel, sing, haggle, court and square off around you (and you can step in),
and some people begin to wonder whether any of this is real. A few of the dead don't stay dead.

It's free, and it's not made by the enRAGE author. It needs enRAGE to work and never modifies
enRAGE's program (it edits enRAGE's settings file only when you change an enRAGE setting in
its panel, after a backup). enRAGE is on Nexus Mods: https://www.nexusmods.com/reddeadredemption2/mods/10967
Source code and bug reports: https://github.com/ninjaalex888/RealNPCs

Story Mode only. Don't run script mods in Red Dead Online.


NEW IN THE NEXT VERSION (not yet released; nothing here is seen in game yet)
------------
- Fixes: a controller binding of "+" alone no longer stops RealNPCs starting; the test cast fit
  their parts for the mind too; the supper invitation's host wanders off when nobody comes, and a
  fault in the dark no longer says your pockets were turned out; Show minds' cards cost less in a
  crowd; a talk cut short isn't remembered as small talk.
- Strangers at a campfire: at night out of the towns, men round a fire call you over for
  coffee and a tale; now and then a traveller walks in from the dark. [Encounters] Campfires.
- The voice below the floor (West World): late at night in a saloon, a muffled plea from under
  the boards behind the bar; the barkeep says pipes, and thinks otherwise. [Encounters] VoiceBelow.
- The doctor's patient: by day, someone whose child has the fever walks to the doctor's and
  back, thinking it over. [Scenes] DoctorsPatient.
- Manners of 1899: a friend is someone you've had two scenes with (one kind word no longer
  makes a mourner); a respectable woman doesn't drink herself drunk, stand rounds or spit,
  and shames a bully rather than squaring up; strangers are "mister" and "ma'am"; he and she,
  never they; the old men and the sweethearts have twice the talks.
- The mind in shadow goes on: the man you assault judges it himself (his MIND line in the log),
  an execution on the road counts as a killing seen, the night visitor as the uncanny. With
  [Mind] MindLive on, people's thoughts from their feelings have twelve lines each instead of
  three, say the mask when the face hides the feeling, and a hungry farmhand doesn't sound like a
  hungry clerk. Memory behaves like memory: after a talk one tells the other the freshest thing
  worth telling, the hearer keeps it a little less surely, and what was heard drifts in the retelling
  (the unnamed doer becomes someone the hearer dislikes, a slight grows, two men become three); what
  someone saw never changes who did it, and what you did never drifts. Standing where something
  happened brings it back. The one spoken to in a street scene answers with their body from
  their own mind and the manners of 1899. Off (as it comes), thoughts, memories and reactions
  are exactly as in 0.6.3.
  And the mind now says what each person near you would do (the log's MIND would: lines, the
  card's "would:"): a meal, the work, the saloon of an evening, church on Sunday, a word with a
  friend, stepping clear of a foe. Nobody is moved by it yet. And what each believes: the log
  says when a belief flips (MIND believes: "now that stranger is a dangerous man"). Nothing acts
  on it yet. [Mind] MindLevels (off, in testing) spreads the mind's work by distance and a
  budget a frame: the people in view every look, the town around now and then, everyone saved
  elsewhere once a game day.

NEW IN 0.6.3
------------
- A livelier street. People deal with each other around you far more often: brawls between
  hot-blooded strangers by day or night, saloon fights at any hour, someone standing up to a
  bully (and the fight that follows, with real punches), falls and a hand up, rounds bought,
  grudges taken up, a mother asking after her lost child, a street preacher any day, two old
  men arguing over the old days, a man calling another out at noon, condolences in the street,
  sweethearts falling out and making up, a debt paid in public, a card cheat shown the door,
  the Sabbath, and a bell at three only the awake hear. [Scenes] TroubleWeight (1 as built,
  2 twice as often, 0 never by chance) sets how often people come to blows or quarrel.
- Night encounters: a figure crouched over a body who stands and stares, then turns up behind
  you; a stranded traveller asking the way (lost, snakebitten, or being robbed on the road);
  and, off until you switch it on, the supper invitation (CONTENT NOTE: accepting it costs you
  most of your cash and the night, and it can't be undone; [Encounters] SupperInvitation).
- People notice how you look (bloodied, drunk, filthy, soaked) and take it by their nature, and
  people who know each other nod, tip a hat or wave as they pass (enemies give a hard look).
- The dead are kept in a ledger shared with enRAGE, so a death it missed is still a death.
  enRAGE's own records can stalk you and can despair (CONTENT NOTE; booked in enRAGE's history).
- Despair has a voice: the farewell in the person's own recorded voice, chosen by temperament.
- A mind under every person, run beside the game and only reported while it's tested: traits,
  needs, worries, what they feel about what happens near you, their mood. Show minds (Ctrl+F2)
  puts a card over the people near you; in a crowd the nearest has the full card and the rest a
  line, never overlapping. [Mind] MindShadow, MindLive (off).
- Controllers: hold RB and press D-pad up for the settings panel, left for F7, right for your
  letters; set your own on the Hotkeys page from the controller ([Controller]).
- Subtitles, one set at a time: the game draws its own white subtitle for any recorded line
  while its Subtitles option is on, so RealNPCs' boxed ones show only while that option is off
  ([Output] Subtitles = game); "ours" shows RealNPCs' always.
- The settings panel's Testing page fires everything RealNPCs can do, by key or by Enter, and
  "Bring the test cast here" brings eight people the tests need, kept in your save.
- The world slows while the settings panel is open. Saves happen in quiet moments (loading,
  paused, faded out) and never put an empty save over a real one.
- Seen working in game: the street scenes above (the brawl, the stand-up, the fall, the lost
  child, the old days, the preacher, the round's words), the test cast, the mind cards, despair's
  farewell, the stalker, the execution. Not yet seen: the controller buttons, the supper
  invitation, and several of the rarer scenes (the callout, the Sabbath, the bell).

NEW IN 0.6.2
------------
- Thoughts above their heads. The thought of the person whose turn it is shows over them in small
  type, one person at a time, for the people within 20 m that you can see (it was 30). News and
  the world summary stay on the bar at the bottom. [Ticker] ThoughtsOverHeads,
  ThoughtsOverHeadScale (0.24); with it off, thoughts go on the bar as before.
- News has a box of its own, "Around town", in the top right under the "Sore at you" panel.
- No more white text. Subtitles look like the game's own now (white serif text in a dark box),
  only for what people say to you, and only while their voice is playing. Notices are the game's
  own updates at the top left, and so are enRAGE's territory changes ([Output] TerritoryNews).
- Conversations in their real voices. About every two minutes, two people standing near you have
  a word in their own recorded voices: the weather, how they're getting on, a piece of news, a
  hello. They remark on riders going by, tell a story about their town, sweethearts pay each other
  compliments, and in a saloon of an evening there are crude remarks (CrudeTalk = false leaves
  those out). Overheard talk has no subtitle. [Scenes] RealVoiceTalk, RealVoiceTalkSeconds (120).
- Trouble between people, each time for a reason: a man with a grudge and a temper smashes the
  other's face into the bar, a table or a wall (RoughHandling); pickpockets among the townsfolk
  (NpcThieves); drunks in a saloon at night who call a man out and fall on their face before a
  punch is thrown (DrunkChallenges); someone short of money asking a stranger for a coin; the
  timid crossing the street to avoid whoever hurt them; a friend seeing to someone who was roughed
  up. The town remembers who did what.
- People react to what's said and done to them, each by their own nature. What's around them is
  on their minds too: a scene they just saw, the people in sight, the weather, the place.
- People flirt with each other and court. You see it in their thoughts: the one asking working up
  to it, the other as they answer. A yes or a maybe becomes a romance that goes on each time they
  meet. [Romance] NpcFlirting.
- Flirting: the flirt key is F2 (it was F6, the game's camera), and Flirt is in the game's lock-on
  menu, on H (FlirtPrompt). Whether they take to it comes from what they think of you and who they
  are: their nature, faith, troubles and mood, against your money and your name in town.
- enRAGE's own people (its nemeses, gang men and named folk) join in street scenes, reactions,
  talk and waking acts, unless enRAGE has them busy. [Scenes] EnragePeopleJoinIn.
- CONTENT NOTE: despair no longer needs a gun on them. Someone fully awake who can't bear it does it
  the way that fits them: a gun that suits them (drawn, and held a moment, before the shot), a
  knife, or running at a wall. It can be one of
  RealNPCs' own people or one of enRAGE's townsfolk, never enRAGE's nemeses or gang men, and never
  with you within 3 m. AwakeningDespair (75; 0 turns it off). Everyone gets one fresh chance.
- F7 marks the fully awake around you on the map for a minute, with what they'd do, and names the
  nearest who could.
- Self-defence lasts as long as the fight: whoever came at you stays fair game while they fight on,
  draw on you or stand at your gunpoint.
- The settings panel opens with Alt+F too, scrolls with the mouse wheel, and a notice after loading
  says which key opens it.
- Seen working in game: the thoughts above heads, the map marks, the real-voice conversations,
  the bar slam and the reactions to it, Flirt in the lock-on menu, the new subtitles, and the knife
  and gun endings.
- Known issues: about one walk-over in ten still gives up before the two meet. The drunk's
  challenge, begging and the wall ending haven't been seen in a game yet. The stalker's digging,
  hammering and scrubbing don't start yet.


NEW IN 0.6.1
------------
- Built for enRAGE 0.9.5.3.
- Each town keeps its own people. When the game puts an ordinary person in the street out of your
  sight, they become one of that town's regulars: someone met there before, back in their own
  clothes, or, while the town has room, someone new with an outfit of their own. Their names,
  worries and lives go on between visits, and the ones you've met know you again. Nobody changes
  clothes in front of you, and nobody enRAGE has already named is touched. [World] Regulars,
  RegularsPerTown (30), RegularsDress.
- Ordinary people have lives of their own. Each one is going through something that fits their
  trade and age: a debt, a sick child, a letter that doesn't come, a feud over a fence, a
  courtship, the drink, a secret. It runs about eight game days in their thoughts (it begins,
  gets worse, comes to a head, and ends well or badly) with the same named people all through.
  When the other person in a debt, a feud or a courtship can be met, it's a real regular in town,
  who thinks about it from their side. Thoughts also follow the day (meals, Saturday night,
  Sunday church), the year (1899, or 1907 in the epilogue) and what you did in town lately.
  enRAGE's townsfolk think about their own lives too when they don't feel strongly about you.
- CONTENT NOTE: a sick child doesn't always get better. About a third of those worries end with a
  burial.
- The ticker gives up to three people near you a turn, and the person right beside you every
  other turn, with a new thought about every 30 seconds.
- Street scenes happen for a reason, and where you can see them. A quarrel goes on, and between
  two hot-tempered men it comes to blows. Sweethearts flirt, and in time one asks. A debt is
  called in. Someone at the worst of a bad week snaps at whoever speaks to them; someone whose
  trouble just ended tells it. If the two are apart, one walks over first. A pair's story moves
  on each time they meet, and what happened is remembered: for days they think about each other
  by name, and the town gossips about it. If one of them has died since, the other thinks of that
  instead: guilt after a quarrel, grief for a friend.
- People notice. Bystanders turn to look, and one may call out: taking a side, trying to calm it,
  or egging it on. People make the gestures that go with what they say.
- Bars under the people near you show their fear of you, how much they like you and their anger
  at you, as enRAGE keeps them. A "Sore at you" panel in the top right lists the strangers whose
  grudge against you is building, with a mark where they'd become a nemesis, and the nemesis
  after you now. [Display] FeelingBars, FeelingBarsMetres (15), FeelingBarsShowEmpty, GrudgeMeter;
  in the panel (Ctrl+F7 or Alt+F), "Bars and meter".
- Standing still no longer holds up enRAGE's funerals or someone's evening at the saloon.
- Aiming at one of enRAGE's outlaws leaves him to enRAGE (he stands his ground and his crew draws
  on you). Ordinary townsfolk still plead.
- Stalkers make gestures Story Mode has (a hat tip, a hand up for quiet, a slow head-shake...),
  crouch as they creep up, and sprint in to 2-30 m before doing odd things.
- F7 shows the town regulars around you on the map for a minute.
- Fixes: the names over street-scene lines match the names inside them; the paper reader no
  longer answers peaceful news as if it were a killing; "Thought he should know" instead of
  "Thought him should know"; rumours keep a name's capital.
- Known issues: about one walk-over in ten gives up before the two meet, and the scene is
  dropped. The stalker's digging, hammering and scrubbing don't start yet.


NEW IN 0.6.0
------------
- New name: enRAGE Westworld is now RealNPCs. The file is RealNPCs.dll and its folder in Documents is
  RealNPCs. If you had the old version: DELETE EnrageWestworld.dll from the scripts folder (RealNPCs
  won't run next to it, and tells you so). Your saves, diaries and settings are copied over from
  the old enRAGE-Westworld folder the first time RealNPCs starts; the old folder is left as it was.
- Two master switches. WestWorld turns the whole West World side on or off: the Awakening, people
  wondering if the world is real, glitches, and the dead who wake. Off, nothing of it happens and
  people already waking go quiet (they pick up where they left off if you turn it back on).
  Stalkers is its own switch now, with its own settings: how often and how likely, how
  far back they follow, how close they come, how far they run off, whether they mutter, and what
  they do when you stand still (StalkerWhenStill: circle you close, watch you from where they are,
  run straight at you and stop a few steps short, do odd things, or a mix - they never attack).
- A new settings panel on Alt+F11, with larger text: RealNPCs, West World, Stalkers and every enRAGE
  setting in one place. Every key, RealNPCs' and enRAGE's, is on one Hotkeys page, and all of
  enRAGE's settings are on one enRAGE page, section by section. Up/Down choose (Shift: the next
  section), Left/Right change (Shift: ten at a time), Q/E turn the page, Tab jumps between
  RealNPCs and enRAGE, Enter sets a key, Backspace closes. Greyed-out
  settings need their master switch on first. Changes apply at once. NemesisMod.ini is backed up
  to Documents\Rockstar Games\Red Dead Redemption 2\RealNPCs\backup before the first change.
  ReplaceEnrageSettings = false gives Alt+F11 back to enRAGE's own panel; Ctrl+F7 (SettingsKey)
  opens this one too. A pad works too: D-pad, LB/RB, A, and B to close.
- AwakeningBreakdown (90): how often an unravelling or awake person falls apart (kneels, shouts
  at the sky, runs) rather than holding it together.
- The ini is reorganised into [WestWorld], [Stalkers] and [Voices]; your values are kept.
- The ticker is always one line high, so it never overlaps the game's subtitles.
- T (TickerNextKey) shows the next line on the ticker now; on a long line shown in parts, the
  next part first. T is also enRAGE's telegraph key: near a telegraph clerk, both happen.
- New defaults: AwakeningDays 1, AwakeningBreakdown 90, AwakeningDespair 75, ReturnChance 35,
  StrangerReturnChance 15. If your ini still has the old defaults (6, 35, 2, 15, 5), they are
  raised to these, and the log says which; values you chose yourself are kept.
- CONTENT NOTE: AwakeningDespair is now 75 percent by default, so most fully awake, armed
  ordinary people end their own life. Set it to 0 if you would rather not see it.
- Stalkers do odd things now. Stand still and, more often than not, the stalker sprints in to
  somewhere between 2 and 30 m (StalkerActMin / StalkerActMax), faces you and works through the
  game's own gestures and tasks, one after another with a stare between: digging a grave,
  scrubbing blood from the ground, kneeling as at a graveside, lying down like the dead, writing in
  a notebook, tipping a hat, a hand up for quiet, a slow head-shake, spitting, laughing at
  nothing, a bow, a jig. Some
  gestures are done twice exactly alike, stop dead partway, or go slow. Two play on not being seen:
  it creeps closer while it is off your screen and stops dead the moment you look, or each time
  you look away and back it is standing somewhere else round you. Move and it stops at once and
  runs off. StalkerWhenStill is now mix by default (odd things most times, else circling,
  watching or rushing); eerie is odd things only. An ini still on the old default (circle) is
  moved to mix.
- Who stalks you has changed. Every StalkerEveryMinutes (34) real minutes of play there is a roll
  at StalkerChance (20 percent). On a hit, someone already near you (within StalkerNearbyMetres,
  60) stops what they're doing, stares at you, and starts following you: whoever has the most
  history with you, such as an enRAGE townsperson with a grudge or who saw what you did, or someone
  who saw your deeds or talked with you; a passer-by only if there's nobody like that. enRAGE's own
  people are never taken over (enRAGE runs them). If nobody near can follow you for a few minutes, a
  stranger turns up behind you instead (StalkerStrangers = false turns that off). Afterwards they
  go back to their day. The daily roll and its day and night hours (StalkerDayOnly, StalkerNightOnly,
  StalkerBoth, StalkerDayFrom/To, StalkerNightFrom/To) are gone, and StalkerChance starts again at
  20 because it now means each roll.
- The ] key (StalkerKey) makes a stalker now, for testing or whenever you like: someone near you,
  or a stranger if nobody near can be. It replaces Shift+F7.
- The wake key is now Y (WakeKey, was G) and the stalker key ] (was 0). An ini still on G or 0 is
  moved over and the log says so; a key you chose yourself is kept.
- No more Windows voice. Every line is now spoken, when it is spoken, in the person's own recorded
  game voice: a line that fits the feeling of what the subtitle says. The uncanny lines (people
  wondering if their world is real, glitches, stalkers muttering behind you) get the game's
  unsettled or taunting reactions instead. The game can't say new words, so the subtitle carries
  the meaning; where the words of a recording are known (voices\words.txt), the subtitle shows
  what they really say and the line written for them goes to the ticker as their thought.
  The [Voices] settings (SpokenVoice, VoiceVolume, VoiceMan, VoiceWoman, VoiceVariety) are gone.
- The reader for letters, diaries and clippings is now the N key (ReaderKey, was F5). An ini
  still on F5 moves over; a key you chose yourself is kept.
- More of the game's own recordings, chosen to fit what the subtitle says: "get away from me",
  "what are you looking at", "are you following me", "who's there", a sentence that trails off.
  The hermit's warnings get sharper each time, and he uses the game's own words for someone
  coming up to a camp. What people mutter under their breath, and the gathering's murmurs, get a
  wordless sound instead of words that wouldn't match. When the ticker shows the thought of
  someone within a few steps, they make a small wordless sound too, at most every 3 minutes each
  (ThoughtSounds in [Ticker]; false turns it off). The words are still the game's own: the
  subtitle carries the meaning.
- Quiet while you stand still. Stand still for 3 seconds (QuietStillSeconds), or turn on the
  cinematic camera, and enRAGE's tension stops building and no nemesis, gang or faction is sent
  after you, through enRAGE's own calm period (the one it uses after a load or a fast travel).
  It carries on within about 10 seconds of you moving again. A fight already going on carries
  on, and enRAGE's town regulars wait too. QuietWhenStill and QuietInCinematic in [Quiet] turn
  each off. It needs enRAGE's own [Calm] Enabled (on by default).
- Self-defence isn't a crime. Kill or hurt someone who came at you, or who was hostile to you
  (red), and RealNPCs notes no crime: no witnesses remember it, no rumour, no posse talk, no
  newspaper. Someone you shot or drew on first still counts, even if they fire back, and so do
  lawmen and anyone who begged. enRAGE's fighters (red on your map) count as having come at
  you even if you fire first. While you fight them off, the game isn't allowed to call it a crime
  at all, so no witness runs to report it. enRAGE judges its own witnesses the same way, and resets the
  bounty after fights with its own outlaws. For everyone else who comes at you, soldiers too,
  RealNPCs puts back any bounty or wanted level the fight gave you (SelfDefenceNoBounty), unless
  you also hurt someone who hadn't come at you or did another crime in the last few minutes.
  Story missions keep their own law, and a fight that moves into another state keeps the
  bounty you owe there.
- The same people at the counters. Each general store keeper, gunsmith, butcher, doctor, barber,
  barkeep, hotel keeper, undertaker, photographer, station agent, trader and stable hand in
  every town is one person for good, with a name, a life of their own and a memory of you: a new
  face at first, a regular later. Get near and the ticker shows what they're thinking, about the
  work, the town, and you. Shoot one down and the game brings them back bandaged: they survived.
  The same person, who talks about the wound, and who is frightened of you (or bitter, or coldly
  polite, by temperament) if they know it was you. Anyone who saw them fall finds it uncanny that
  they're back. TownPostsAfterDeath = successor makes a death final instead: the counter passes to
  a relative or a newcomer who bought the business. Sheriffs, preachers and anyone a story mission
  names are left alone. TownPosts = false turns it off.
- People think about what is on their minds ([Mind] Minds). Each keeps memories of what they lived
  through: a fight they were in or saw, a slight, a kindness, a theft, the drunk who called them
  out. What they think is whatever pulls hardest right now: that, their worry, the weather,
  someone in sight, you. Memories fade, come back when someone in them walks by, and weigh
  differently on each temperament. Nobody thinks the same line twice in a day. So far from scenes
  between people, from what they saw you do, from deaths (a friend grieves for weeks, once word
  reaches them), from your flirting, and from being saved. Minds = false: the old mix.
- An execution on the road ([Encounters] Executions, ExecutionChance 20). Away from the towns, once a
  day at most, one of the nearest town's regulars kneels at gunpoint, a mark on your map, for a
  reason from his own life: his debt (the creditor and a hired man), his feud (the other family),
  or talking to the law (the region's outlaws). Shoot or rush them and they fight you, with no
  crime noted. Save him and the town hears of it; wait or ride on and they shoot him, and his kin
  and his town hear. To see it now: press [, or the settings panel's Testing page.
  CONTENT NOTE: a man can be shot dead in front of you. Executions = false turns it off.
- Night encounters ([Encounters]). Out in the wilds between ten at night and four in the morning,
  you may hear a woman crying somewhere off the road (CryingWomanChance, 15 percent on each roll
  every EncounterEveryMinutes, 12, at most once a night). She kneels in the dark and calls for
  help in her own voice. Go close, or point a gun at her, and she may lunge at you with a knife
  and run (and later cry again somewhere else), or two or three armed men may step out of the
  brush, or she may say something that makes no sense and walk off into the night. Anyone who
  attacks is hostile from the start, so fighting them off (her too) is self-defence. Never in towns,
  indoors, in missions, while you're wanted or fighting, or while a stalker is out.
  CONTENT NOTE: the knife and the ambush are violent. CryingWoman = false, or Encounters =
  false for the whole section, turns it off.
- The hermits who woke (West World). Someone fully awake who runs from their town in the end
  of a wake act never comes back to it: they live rough in the wilds now. Out there, on a roll
  (HermitChance, 25 percent, at most one new camp a day, up to 8), one of them makes camp out of
  sight 60 to 140 m away and shouts once at the voice they hear (if nobody has fled yet,
  someone who walked out before you came by is made instead). The camp is saved: come within
  about 150 m and they are there, by a fire, talking back to the voice, or asleep beside it
  from midnight to five. They stand and watch as you come, warn you off if you come closer, and
  tell you how it was if you keep your distance (each thing once). Crowd them, aim at them or
  hurt them and they run, and are back another time. They are never armed. Killing one counts
  like any killing.
- The gathering (West World). From midnight to half past three in the wilds, on a roll
  (GatheringChance, 15 percent, at most once a night), four to seven of the awake stand in a
  silent ring somewhere off in the dark, facing in: people who have begun to come apart or are
  fully awake, as they looked when you met them, and strangers. Come near and every head turns to
  you. Look away and they are closer; look back and they stop dead, never nearer than a few
  steps. Stand in the middle and they speak at once. Aim at one, shoot, or hit one and they
  scatter. They never attack.


NEW IN 0.5.5
------------
- Stalkers can appear again. In 0.5.4 the game turned down every spot a stalker tried to appear
  on, so none ever showed up. If the game won't give a spot, the stalker now uses plain ground
  behind you (never inside a building). Staged weddings use the same fix.
- Stand still for 20 seconds and the stalker runs in close and circles you. Take a step and
  he runs back out to 45 to 70 m, behind a tree or a building corner if there is one, and
  watches you from there. Turning the camera doesn't scare him off. Walking up to him no
  longer ends it; drawing on him still does.
- More of what people say comes with their real voice. Conversations with enRAGE's people,
  remarks as you pass, replies to insults, flirting, couples and strangers chatting, and
  mourners at funerals now play a recorded game line that fits the feeling of the subtitle:
  angry, scared, grieving, grateful, gossiping, flirting, brushing you off. Arthur or John
  answers in his own voice when you flirt or talk. The words differ from the subtitle (the game
  can't say new words); the feeling matches. Uncanny lines keep the Windows voice.
- A stalker now keeps it up for StalkerMinutes in full, and the default is 120 (two hours, up to
  240). If yours was the old default (8) or the old ceiling (60), it's raised to 120 for you.
- The Awakening, first part. About 5% of the people enRAGE tracks are quietly chosen to wake up, leaning toward those who already suspect the most. Over days of game time each one moves through four stages: stirring (deja vu), questioning (they count things, test the world), unravelling (fragments, repeated words), awake (lucid, looking for you). What they think, say and write changes with the stage, and from stage two they keep a secret diary. The ticker summary counts them. Settings: AwakeningShare (5), AwakeningDays (6 days per stage). To see it at once, face one of enRAGE's people and press G (WakeKey in the ini): they wake one stage and say something.
- Everyone is someone. Ordinary people who are not enRAGE's (most of the world) get a name, a trade, a town and a temperament of their own when you press the wake key near them or the ticker shows their thoughts. They can wake like anyone else: press G (WakeKey) near any adult and they wake one stage. They exist only in this add-on and are saved once something happens to them.
- The Awakening on screen. Woken people now act oddly when you are near. Stirring: a glance a moment too long, or a stop and a stare. Questioning: they stand perfectly still watching you, or pace the same few steps over and over. Unravelling: they freeze mid-step and pace faster, with pauses, as if stuck in a loop. Awake: they walk straight up to you, face you and say something lucid. Only ordinary people and enRAGE's named townsfolk are directed (never enRAGE's full people, never during story missions or in danger), and they are handed back as they were. Use G near someone to wake them and watch.
- Ordinary people's thoughts are now written from real psychology: the body, status anxiety, loss and threat bias, the worries of 1899, the mask over what they feel, and what they notice about you when you are close.
- Every adult can now be a person with a mind, soldiers and lawmen too. Ordinary people get the full acts; enRAGE's own people and soldiers in its camps only turn their heads, think and speak (their movement is enRAGE's).
- Fuller acts. Stirring: double-takes. Questioning: standing perfectly still, pacing, walking out to see where the town ends, asking a neighbour if they feel it too. Unravelling: freezing mid-step, shouting at the sky, kneeling, running with no goal. Awake is a lasting state: they come to you, say a few things (who is writing this, is there an outside) and end by pleading, trying to leave town, trying to wake someone or refusing to get up.
- CONTENT NOTE: a chance (AwakeningDespair, default 75 percent since 0.6.0, 0 turns it off) that a fully awake ordinary person cannot bear it and ends their own life: with a gun that fits them, with poison, with a knife, or by running at a wall. It never happens to anyone keeping a counter, or to anyone enRAGE has busy at that moment (a fight, a job, a ride), and it is not counted as anything you did. Set it to 0 if you would rather not see it.
- Acts and words belong together now: people who pace count their steps in their heads, people who freeze lose their sentence halfway, and the one walking up to you thinks about why. The thoughts appear on the ticker as it happens.
- Ordinary people have inner lives of their own: what a passer-by thinks now comes from their trade, town, temperament, faith or the hour of the day.
- Gang and faction news (wars, feuds, clashes, rivalries) now appears as the game's pop-up on the right of the screen, away from the bottom ticker, which keeps what people think and do. WarNewsTo in the ini (right or ticker) switches it back.
- The wake key now makes the person act at once so you can see what each stage does.
- The log shows the game's first few answers ("SAFECOORD probe"), to help pin down the cause.


NEW IN 0.5.4
------------
- Daytime stalkers. Each in-game day now rolls one of four outcomes: no stalker, one by day
  (06:00 to 17:00), one at night (18:00 to 04:00), or both. StalkerChance is the chance that
  someone follows you that day; StalkerDayOnly, StalkerNightOnly and StalkerBoth (default
  25 / 50 / 25) set how a stalker day splits. A daytime stalker who never gets a clear moment
  before dusk is dropped; a night stalker still waits until the next night.
- The log shows each day's roll, e.g. "Stalker roll (20%): one by day and one at night".


NEW IN 0.5.3
------------
- Stalkers fixed. A stalker due after midnight was wiped out when the day changed, so many nights
  never got one, and one who couldn't find a hidden spot was given up for the day. Now a stalker
  who is due waits for the first clear moment at night (outdoors, no story mission, no lawmen
  after you, not fighting), and if the night ends first he comes the next night.
- The log says when a stalker is due and what he's waiting for.
- Shift+F7 calls a stalker right away, for testing (see TESTING IT). F7's log dump now includes
  the stalker's state.
- Old saved talk that read "Pvt. saw ..." or "Pvt.'s brother" (from before 0.5.1) now reads
  "a private saw ..." and "a private's brother".


NEW IN 0.5.2
------------
- About twenty lines of dialogue reworded, so every line is the add-on's own.
- A diary line about a person's goal no longer garbles words like "your".
- The source code is now on GitHub (link above).


NEW IN 0.5.1
------------
- Stalkers are more common: a 20% chance per in-game day (was 8%). If you never changed the old
  setting, it's raised for you; the log notes each night a stalker is due.
- Ranks read properly: "Pvt. Maddox says..." instead of "Pvt. says he Pvt. ...".
- Holding F5, F6 or F7 no longer repeats it over and over.
- Killing armed men in a fight, rival gangs out for your blood, or anyone during a story mission
  counts as a killing, not a murder.
- Regions such as the Heartlands no longer count as towns you "ride into".
- Small fixes to life updates ("hit the The Klan").


NEW IN 0.5
----------
- More life in the street. Strangers now also square off in standoffs, sing old songs drunk, tell
  tall tales (some about you), read the paper aloud, haggle over horses, fall for a snake-oil
  pitch, make small talk, and pick pockets. After a bad crime, men talk about getting up a posse.
  The widow of a man you shot may be grieving in town, and may recognize you.
- Step into a scene: greet or antagonize someone in it. Take a side in a quarrel, break up a fight
  or a standoff (the town hears you're a peacemaker), or egg it on.
- Pickpockets: when one strikes near you, run him down (lasso, hogtie, tackle) and the town hears
  of it. Shoot him and it's a killing.
- Hold a gun on a stranger and they beg, with a name, a family, a trade. Lower the gun and word
  gets around that you showed mercy. Shoot anyway and that's what they'll say about you.
- Riding into town: locals react to what they've heard of you, your bounty, or your good name.
- The game's own voices are used far more: most of the add-on's lines now come with a fitting
  recorded line in that person's real voice (cheering, mocking, pleading, gossip, thanks...).
- The Windows voice can be any voice you have installed, and each speaker keeps their own voice,
  pitch and pace.
- Westworld: more host lines and conversations, people returning to one day of their past, strange
  dreams in their diaries, and a few lines they find in their heads (Shakespeare, Poe, Dickinson).
- Everyday thoughts from passers-by on the ticker.


NEW IN 0.4
----------
- The ticker: a line at the bottom of the screen that's always there. It shows what the people
  near you are thinking, and when nobody is near, a summary of the world and the latest news.
- Your deeds matter: towns and shops you visit, church, the saloon, hunting, killing, robbing,
  stealing horses, cheating at cards, sweet-talking, letting a beaten man go. People who saw it
  remember; people who didn't hear about it.
- Rumours travel town to town, about a hop a day, and get bigger and stranger the further they go.
- Your deeds turn up in enRAGE's newspapers.
- Letters wait for you at post offices: from the kin of people you killed, from people you were
  good to, from sweethearts, from people who suspect the truth, and wedding invitations.
- Diaries: loot a body (or search a gang camp) and you may find the diary they kept. Read it, and
  your letters and clippings, with the N key. People write about their work, their town, their family,
  their faith and habits, what's on their mind, and you.
- Kin far away hear when you kill someone. Some write. Some come west for you, as one of enRAGE's
  own people with a grudge.
- In someone's home town, a passer-by may turn out to be their mother or brother.
- Strangers around you flirt, quarrel, call in debts, catch cheats, propose and come to blows.
  In saloons they trade rumours, and loose talk can give away a gang job.
- Weddings happen at the church. Be there and the couple will remember it. Shoot it up and they'll
  remember that instead.
- Mourners at enRAGE's funerals speak of the dead.
- People go after what they want, owe each other money and collect it, and get moved out of jail.
- Westworld: some people start to question their world, out loud. Some of it is read aloud by the
  Windows voice. Now and then someone glitches.
- Reveries: some of the people you kill wake up again days later and remember how they died and
  who did it. Strangers you killed can come back too.
- Stalkers: rarely, someone follows you from a distance and hides when you look.
- Personalities colour what people say: a hothead, a coward, a proud man, a cold one.
- Changes to the ini take effect in game within a few seconds.
- Story missions are left alone: no scenes, stalkers or staged weddings while one is running, and
  nobody from a mission comes back as a revenant.


THE TICKER
----------
A thin bar at the very bottom of the screen.
- When someone enRAGE knows (or a townsperson who knows you) is within about 30 metres, it shows
  what they're privately thinking, closest person first. A stranger's passing thought can show too.
- When nobody is near, it shows a summary ("140 lives - 12 know your name - 3 hold a grudge ...")
  and then rotates through the latest news: thoughts from far away, diary entries, life updates,
  letters, rumours.
- New news gets a turn straight away and flashes brighter.
- T (TickerNextKey) moves on to the next line now. A long line shown in parts goes to its next
  part first; from someone's thought it moves on to the news.
- It hides in menus, cutscenes, the pause menu, when the HUD is hidden, and while enRAGE's own
  menus are open.
Subtitles (0.6.2): only what people say to you, and only while their voice plays; talk you only
overhear has none. Ticker = false turns the ticker off.


YOUR DEEDS
----------
The add-on notices what you do and where:
- Towns you ride into, and shops: the gunsmith, general store, saloon, church, bank, doctor,
  barber, bath, hotel, sheriff, station, tailor, fence, theatre, photographer, stable.
- Who you sweet-talk (F2), and the people enRAGE knows that you beat and then let go.
- Hunting (what you bring down), killing (strangers, lawmen, people enRAGE knows), and the crimes
  the game itself sees: robbery, bank, coach and train robbery, theft, horse theft, assault,
  looting, grave robbing, kidnapping, arson, cheating.
People enRAGE knows who saw it remember it (it goes into their enRAGE memories). Locals hear about
it. Later they say so to your face ("Saw you at church. Didn't figure you for the praying kind."),
think about it, write it in their diaries, and gossip.

Word travels. A deed people talk about spreads to the nearest towns, then on from there. Each town
keeps its own version, and it bends with distance: in Valentine "a man called Morgan shot a ranch
hand", in Saint Denis "some outlaw gunned down three men and rode off laughing", and further out
"a devil on a black horse - they say bullets go right through him". You hear the local version
when you ride into town.

The newspapers enRAGE sells pick up the talk too, in the small items about town.


LETTERS, DIARIES AND THE READER (N)
-----------------------------------
Letters are held at the post office. Ride into a town with one (most towns) and the clerk hands
them over. Press N to read them.
Diaries: loot the body of someone enRAGE knows who kept a diary, and you'll be offered it ("You
find a diary on ... [N] take it"). Searching an empty gang camp can turn up a diary too.
N opens your papers: Left/Right turn pages, Up/Down go to the next paper, Backspace closes.
Diaries are also saved as text files (see FILES).


FAMILY FAR AWAY
---------------
The people enRAGE knows have family: a mother in Saint Denis, a brother in Blackwater, a sister
back East. Kill someone and, a day or two later, their family gets a telegram. Some write to you.
Some decide to come west and settle it. A relative who comes for you becomes one of enRAGE's own
people, hates you, and hunts you the way enRAGE's people do.
In a relative's home town, if they've heard of you, a passer-by may walk up and turn out to be them.


LIVES IN MOTION
---------------
- Scenes: strangers near you flirt, argue, call in debts, catch a cheat at cards, propose, catch
  their spouse being sweet-talked, and sometimes come to blows (fists only: if one reaches for a
  gun, it goes back in the holster): most often two hot-tempered men with bad blood, or a hothead
  with drink in him in a saloon at night. In saloons they trade
  the rumours going round, and loose talk can give away a gang job, which then shows up in
  enRAGE like any other tip.
- Weddings: when two of enRAGE's people get engaged, a wedding is set at the church in their town.
  The banns are posted (and printed in the paper); friends of yours get an invitation. Be there
  at the time and you'll see it, and they'll remember you came. Draw a gun or start shooting and
  the wedding breaks up; they marry quietly later and remember who spoiled it.
- Funerals: stand near the grave at one of enRAGE's funerals and the mourners speak of the dead.
  If you killed him, they'll know you.
- People pursue their goals, owe each other money and collect it (one way or another), and the
  ones held in jail get moved (and often don't arrive).


FRONTIER SCENES AND YOU (0.5)
-----------------------------
Scenes happen near you between strangers, at most one every couple of minutes (DramaGapSeconds):
- Standoff: two men square off, a crowd gathers. Guns come out only when both are armed and no
  lawman is close; it ends with someone backing down, or with fists. Nobody fires.
- Drunk singing (saloons, nights), tall tales from old-timers, the paper read aloud (your deeds
  included, once they're in print), horse trading, a snake-oil man, small talk between neighbours.
- A pickpocket: he bumps someone, runs, and the victim begs you to stop him.
- A posse: within a day of a bad crime in town, men talk about riding after whoever did it. If
  someone saw your face, they may notice you standing there.
- A widow (or brother) grieving a stranger you killed in that town in the last week.
Greet or antagonize anyone in a scene and it turns on you: take a side in a quarrel or a haggle,
break up a fight or a standoff ("peace": it travels like your other deeds), or stir it up.

Pleading: hold your gun on a stranger for a moment and they'll beg. Lower it and they thank you
("mercy"). Shoot and it's an execution, and that's how the rumour goes. In towns where the dead
have been waking, some say stranger things.

Riding in: the first time, or after a while away, someone may say what they've heard of you:
fear after killings, scorn after thefts, warmth after good deeds, a remark if you have a bounty.

The lines are original. The songs (cowboy ballads and hymns) and the few quotations are public
domain, and the movie flavour comes from the genre's stock moments and period slang.


WESTWORLD
---------
Hosts. Some people start to suspect their world is made. At first it's deja vu and odd dreams.
Later they notice the loops: the same words, the same faces, the music that comes before trouble.
The ones who've died and come back, or who watched you die and get up again, say stranger things
still. Strangers in the street wonder out loud too, in their own recorded voices, unsettled.
Rarely, someone glitches: freezes and stares, or says the same greeting three
times exactly alike.

Reveries. When you kill one of enRAGE's people (not story characters), there's a chance
(ReturnChance, 35%) they wake up again a few days later. They remember where they died, what
killed them and who did it, and they come back the way enRAGE's people come back from the dead.
Strangers you kill are remembered too, and a few (StrangerReturnChance, 15%) come back.

Stalkers. Now and then someone near you, often someone who knows you, starts following you. They keep their distance, stand and stare when they think
you can't see, freeze when you look at them and slip out of sight, and somehow turn up behind you
again after you've ridden off. Stand still for 20 seconds and they come closer and do something:
odd gestures and tasks (kneeling as at a graveside, a tipped hat, a hand up for quiet), circling you, or
just watching; move and they run back out, behind a tree or a building if there is one. They
never attack. Draw on them and they run for good. One can follow you for up to two hours.
Sometimes it's someone you killed.


REQUIREMENTS
------------
- Red Dead Redemption 2 (PC), Story Mode
- Script Hook RDR2 and Script Hook RDR2 .NET (V2), the same ones enRAGE needs
- enRAGE 0.9.5.3 installed and working. If a later enRAGE changes something, the parts that need it
  turn off and the log says which.
- Optional: CasualHookups, for an on-screen night together (see INSTALL, step 4).


INSTALL
-------
1. Copy RealNPCs.dll into your game's "scripts" folder, next to enRAGE's NemesisMod.dll.
   Coming from enRAGE Westworld (the old name): delete EnrageWestworld.dll from that folder.
   Updating from an earlier RealNPCs: just replace the file. What people remember about you
   carries over, and your ini is updated with the new options (your values are kept).
2. Start the game and load your Story Mode save.
3. About 10 seconds after you're in the world, the ticker appears at the bottom of the screen.
4. Optional: CasualHookups, a separate mod by its own author
   (https://www.nexusmods.com/reddeadredemption2/mods/7608), plays an on-screen night together
   instead of RealNPCs' fade to black. Install it as its page says (CasualHookups.asi and
   CasualHookups.ini in the game folder). Its Flirt then works only on a woman who has said yes
   to your flirting here ([Romance] CasualHookupsDecides). The first time RealNPCs starts with it,
   it changes CasualHookups.ini once: its Flirt key moves from G (enRAGE's own lock-on prompts
   use G) to B, her clothes come back afterwards (enRAGE knows its people by their clothes), and
   it always gives in, since she has already said yes. The original file is kept as
   RealNPCs\backup\CasualHookups.ini.before-realnpcs. Restart the game once after that: it may
   only read its settings when the game starts. Its settings are on the panel's Romance page.

To uninstall, delete RealNPCs.dll. You can also delete the RealNPCs folder in
Documents if you don't want to keep what people remembered.


TESTING IT
----------
This version was checked outside the game (it builds, every enRAGE function it uses was found,
the people, rumours, letters and diaries were run through simulated weeks, and every scene, plea
and remark was checked line by line). 0.4 and 0.5.1 have since been played in game without
trouble; of the new 0.5 scenes, the posse has been seen so far. If something misbehaves, the log
(see FILES) says what happened. A feature that keeps failing switches itself off and says so in
the log; the rest carries on. Bug reports (with your log) are welcome on GitHub (link above).
- Press F7 near someone enRAGE knows: it shows what they're thinking now. Away from anyone, it
  shows the summary, puts a test line on the ticker, and writes the state of every feature to
  the log.
- Walk into a saloon or a church, buy cartridges, ride into another town a day later: listen to
  what people say, and watch the ticker.
- Kill one of enRAGE's people, loot the body for a diary, then visit a post office a few days later.
- Stand around a busy street or saloon for a few minutes. Greet someone in the middle of a quarrel.
- Stay a while in one town: watch who meets whom, and why, and the bars under the people near you.
- Hold your gun on a passer-by in town (then lower it). Ride into a town where you've done things.
- Press ] to make a stalker right away: someone near you, or a stranger if nobody near can be. It
  happens once you're outdoors, off any story mission, with no lawmen after you and not fighting;
  until then the log says what it's waiting for. Go about
  your business and look behind you now and then (he keeps 30 to 70 metres back).


FILES
-----
Documents\Rockstar Games\Red Dead Redemption 2\RealNPCs\
    RealNPCs.ini            settings (created on first run, updated by new versions)
    RealNPCs.log            what happened
    arthur.json, john.json  what people know and feel about you, and the world's state,
                            kept separately for each character
    diaries\                one text file per diary, e.g. "arthur - Elihu Starling.txt"
    backup\                 NemesisMod.ini and CasualHookups.ini as they were before RealNPCs
                            changed them

If you load an older save, the add-on notices that game time went backwards and forgets anything
that hadn't happened yet.


SETTINGS (RealNPCs.ini)
-----------------------
Saved changes are picked up in game within a few seconds (ReloadIni). Every option has a comment
in the file. The new ones:

[Ticker]    Ticker (true), TickerNearbyMetres (20), TickerSeconds (12), TickerY (0.962, height on
            screen), TickerScale (0.30, text size), TickerWidth (0.60),
            TickerNextKey (T): the next line now, ThoughtSounds (true): a small sound from someone
            near as their thought shows, ThoughtsOverHeads (true): the thoughts of people near you
            above their head in small type instead of on the bar, ThoughtsOverHeadScale (0.24)
[Display]   FeelingBars (true): bars under people near you for their fear, liking and anger,
            FeelingBarsMetres (15), FeelingBarsShowEmpty (false): the empty part of each bar too,
            GrudgeMeter (true): the "Sore at you" panel, top right
[WestWorld] WestWorld (true): the master switch. false turns off everything below it.
            AwakeningShare (5), AwakeningDays (1), AwakeningBreakdown (90, how often an unravelling
            or awake person falls apart), AwakeningDespair (75, 0-100, see the content note above),
            WakeVoices (true): the waking say the game's own lines in the wrong places, more each stage,
            HostsQuestionReality (true), RealityChance (15), Glitches (true),
            Reveries (true), ReturnChance (35), StrangerReturnChance (15)
[Stalkers]  Stalkers (true): the master switch. StalkerRecords (true): one of enRAGE's people may be
            the stalker while enRAGE has no use for them. StalkerKey (]): make one now,
            StalkerEveryMinutes (34): real minutes between rolls, StalkerChance (20, percent per roll),
            StalkerNearbyMetres (60): someone this close becomes the stalker, StalkerStrangers (true):
            a stranger if nobody near can, StalkerMinutes (120), StalkerAppearMin (45) /
            StalkerAppearMax (70): how far behind you a stranger first turns up, StalkerFollowMetres (26), StalkerStareMetres (34),
            StalkerStillSeconds (20), StalkerWhenStill (mix | eerie | circle | watch | rush): what he
            does when you stand still, StalkerCircleMetres (8), StalkerActMin (2) / StalkerActMax
            (30): how far off he does odd things, StalkerHideMin (45) / StalkerHideMax (70):
            how far he runs off when you move, StalkerMutters (true). He never attacks.
[Quiet]     QuietWhenStill (true): while you stand still, enRAGE sends nothing after you and its
            tension stops building, QuietStillSeconds (3), QuietInCinematic (true): the same in the
            cinematic camera
[Encounters] Encounters (true): the master switch. EncounterEveryMinutes (12): real minutes
            between rolls, EncountersAtOnce (5, 1-8): how many may be going on at once, a stalker
            among them (one of each kind; a roll starts one at most), Executions (true), ExecutionChance (20, percent per roll, away from the
            towns, once a day), CryingWoman (true), CryingWomanChance (15, percent per roll, 10 pm to
            4 am in the wilds), Hermits (true, needs WestWorld), HermitChance (25, a new
            camp's chance per roll in the wilds), Gatherings (true, needs WestWorld),
            GatheringChance (15, percent per roll, midnight to 3:30 am in the wilds), ExecutionKey ([),
            CryingWomanKey, HermitKey, GatheringKey (None): bring one out near you now, for testing
            NightVisitor (true), NightVisitorChance (18): after midnight in a town, a pale figure over a
            body, gone when you look away; the town talks of it next morning
            Travellers (true), TravellerChance (20): by day on the roads, a man on foot asks you the way to
            the nearest town; greet him or stop a moment and he finds his way (one in three is
            snakebit and asks for help instead; one in five is being robbed, and you can step in)
            SupperInvitation (false until tested), SupperChance (12): at dusk on the roads, a too-friendly man asks
            you to supper at his place; follow him there on foot and you wake in a ditch at first
            light with most of your cash gone ($250 at most, and it can't be undone); ride on and
            he lets you go
[World]    Deeds, SelfDefenceNoBounty (true), TownPosts (true), TownPostsAfterDeath (survive | successor),
            Regulars (true), RegularsPerTown (30), RegularsDress (true), RumoursTravel, Newspaper, Letters, FindDiaries, Avengers (all true),
            AvengerChance (35), CastRelatives, Drama (true), DramaGapSeconds (150), SaloonGossip,
            Weddings, Funerals, JobRumours, PrisonerTransfers, Debts (all true)
[Scenes]    SceneReactions, Standoffs, Songs, TallTales, NewspaperTalk, Haggling, SnakeOil,
            SmallTalk, Pickpockets, PosseTalk, Mourners, Pleading, ArrivalTalk (all true),
            Reactions (true): in a street scene the one spoken to reacts to the words by their
            nature, and a man whose face goes into the bar reels
            Acknowledge (true): people who know each other nod, tip a hat, wave or say hello as they
            pass; enemies give a hard look, sweethearts a long one; each thinks something of it
            SightReactions (true): people near you notice when you look bloodied, drunk, filthy or
            soaked, and react by their nature
            Kindnesses (true): someone who trips in the street is helped up, or left lying, by the one
            who saw it; walk up while they're down and you help them yourself; someone may step in
            before a bully's blow lands; and of an evening someone glad stands the saloon a round
            (the evening after a wedding, a toast to the newlyweds)
            Vices (true): someone with bad news, or word of a friend's death, drinks the night away in
            the saloon (and a friend may sit with them)
            Grudges (true): a friend has it out with whoever wronged their friend; someone whose friend
            you killed may tell you so to your face (never violent)
            (Standoffs also brings the noon callout: two men with bad blood face off in the street about
            noon until one backs down; nobody fires)
            (Drama also brings two old men arguing about the old days, on a Sunday afternoon a
            street preacher, answered by each listener's own faith, and by day a mother asking after
            her lost child)
            RoughHandling (true): a man with a grudge (or a hothead with drink in him) smashes another's
            face into the nearest table, bar or wall; NpcThieves (true): pickpockets among the
            townsfolk, and begging; DrunkChallenges (true): a drunk calls a man out at night
            EnragePeopleJoinIn (true): enRAGE's own people take part in all of these too, unless
            enRAGE has them busy (a fight, a job, a ride)
            RealVoiceTalk (true), RealVoiceTalkSeconds (120): people near you talk to each other in
            their real recorded voices; CrudeTalk (true): the game's crude remarks among them
[Romance]   NpcFlirting (true): people near you make eyes at each other and court, each answering by
            what they make of the other; their thoughts on the ticker show it as it happens;
            sweethearts fall out now and then, and one comes back with a small gift to make it up
            FlirtPrompt (true): Flirt in the game's lock-on menu, with Greet and Antagonize, on H (only on women while SameSexRomance is off)
            (D-pad down on a pad)
            WillingSingle (70), WillingTaken (20): percent chance a single, or a married or
            spoken-for, woman takes to your flirting before what she thinks of you tips it (the
            saloon's women always do); greeting her first (R) helps by 15, flirting cold costs 15
            Trysts (true): when she says yes, Flirt becomes Hook up (H) for two minutes; her answer
            to that is its own (better after a greeting), then the screen goes dark, an hour passes
            CasualHookupsDecides (true): with CasualHookups installed, its Flirt works only on a
            woman who has said yes to you, and RealNPCs doesn't fade to black itself
[Output]    PerfMeter (true): times every part of RealNPCs each frame; F7 writes the costliest,
            and the log says when it goes over budget (PERF)
[Mind]      Minds (true): people think about what is most on their minds, from what they lived through;
            false: the old mix of thoughts. RecordMinds (true): enRAGE's own people too, weighed with
            enRAGE's record of them. RecordFates (true): a death RealNPCs caused goes into enRAGE's record.
            ShareWithEnrage (true): RealNPCs keeps its own record of
            who has died; when one of enRAGE's townsfolk dies and enRAGE didn't see it, RealNPCs
            marks them dead there too
            MindShadow (true, while it is tested): the mind underneath works out the traits, needs,
            feelings and mood of the people near you and writes them to the log (MIND lines), F7
            (TEST mind shadow) and the Show minds cards; it changes nothing anyone does or thinks.
            MindLive (false): what that mind decides reaches the screen: moods, and thoughts of what people really feel (about you, a friend, a stranger) and of a pressing want
[Output]    NewsTo = corner | ticker | gamefeed
                corner: the "Around town" box (default); ticker: the ticker;
                gamefeed: the game's own updates, top left
            WarNewsTo (feed | corner | ticker): where gang and faction news goes (feed, the default:
                the game's own updates, top left)
            TerritoryNews (true): enRAGE's territory changes go there too. enRAGE also prints
                them as white text while its own [Menu] Messages is 3 (2 or 4 stops that)
            Subtitles (on | off): what people say to you, on the game's subtitle line while their voice
                plays (the game's own Subtitles option has to be on too)
            TestKey (F7), FlirtKey (F2), WakeKey (Y), ReaderKey (N), ReloadIni (true),
            ReplaceEnrageSettings (true): Alt+F11 opens the RealNPCs panel (enRAGE 0.9.5.3 only),
            SettingsKey (Ctrl+F7) and SettingsKey2 (Alt+F) open and close it with any enRAGE version,
            PanelSlowsTime (true), PanelTimeScale (0.25): the world slows to a quarter speed while the
            panel is open, as it does for the weapon wheel (never in a story mission)
All the older options are still there and work the same.


KEYS
----
Ctrl+F7   the RealNPCs settings panel (all RealNPCs and enRAGE settings; SettingsKey). Alt+F
          (SettingsKey2) opens it too, and either closes it again. Its Testing page lists everything
          RealNPCs can make happen near you now, by group: Enter on a row sets its key, Space fires it
          (the panel closes first; on a gamepad, A fires it), Delete leaves it without a key. A key
          another test has already moves to the new one and leaves that test without a key; one of
          enRAGE's keys, or one RealNPCs needs (F7, the flirt key...), is refused, naming whose it is. The keys are in [Testing] in the ini (TestKeys switches
          them all off). Every street scene has a row (the nearest named pair that fits plays it),
          and so do pleading, arrivals, letters and the strange remarks. Show minds (on as it comes) puts a card over
          the people near you: their temper, mood, worry, ties and what is on their mind; Replay fires
          the last test again. "Bring the test cast here" brings eight people the tests need (a mother,
          two old men, a preacher and a listener, three men for a bully) to stroll about a spot out of
          your sight behind you; they're kept in the save, so the same people come each time, and
          "Let the test cast go" takes them away. There is also a row that wipes every NPC's memory (Enter twice). Alt+F11 opens it as well, but
          only on enRAGE 0.9.5.3; on other versions Alt+F11 opens enRAGE's own.
          About 15 seconds after loading, a notice says which key to use.
RB+D-pad  on a controller: hold RB and press D-pad up for the settings panel, left for F7, right
          for your letters (the letters turn with the D-pad and close with B). Set them from the
          controller on the panel's Hotkeys page (A, then hold one button and press another), or in
          [Controller] in the ini (PlayStation names work: R1+Up). Never in the game's own menus.
          Subtitles: the game draws its own white one for any recorded line while its Subtitles option
          is on, so RealNPCs' boxed ones show only while that option is off ([Output] Subtitles = game);
          set it to "ours" and switch the game's off to see only RealNPCs'. [Scenes] TroubleWeight sets
          how often people come to blows or quarrel (1 as built, 2 twice as often, 0 never by chance).
F7  what the nearest person enRAGE knows thinks of you (and a status line in the log); the town
          regulars and the fully awake around you show on the map for a minute
F2        flirt with the person you're looking at (FlirtKey; it was F6, the game's camera).
          Locked on to someone, the game's menu also offers Flirt, on H (FlirtPrompt).
N         read your letters, diaries and clippings (or take a diary you've just found; ReaderKey)
]         make a stalker now: someone near you, or a stranger (StalkerKey)
[         stage an execution on the road near you, out of sight, to see one now (ExecutionKey)
Y         wake the person you are facing one stage (the Awakening, for testing; WakeKey)
Shift+F7  two people near you talk in their real voices (TestTalkKey)
Shift+F9  one man near you puts another's face into the bar (TestRoughKey)
Shift+F2  the subtitle line in each font, to find the game's (TestFontKey)
Ctrl+F2   Show minds on or off for this session: the card over people near you (TestShowMindsKey;
          the settings panel's Testing page has the switch that's saved)
          These seven (], [, Y, Ctrl+F2 and the three Shift keys) are the Testing page's; the rest of its
          rows have no key as they come.
T         the next line on the ticker now (on a long line, its next part first; TickerNextKey).
          T is enRAGE's telegraph key too: near a telegraph clerk, both happen.
None of the others are used by enRAGE (F11, Alt+F11, Ctrl+F10, I, T, Insert), Script Hook .NET's
console (F8) or Faster Looting (F9, F10). If the game or another mod already uses one, change it
in the ini or the panel (e.g. ReaderKey = Ctrl+N).


CREDITS
-------
enRAGE is by its author on Nexus Mods (link above). This add-on reads what enRAGE already tracks
and adds to it, through enRAGE's own functions wherever enRAGE has them. It contains no enRAGE
code or content.
CasualHookups is by its author on Nexus Mods (link above). RealNPCs works with it when it's
installed and contains none of its code or files.


LICENSE
-------
Copyright 2026 lexthestoic. All rights reserved. You're welcome to download and play RealNPCs;
please ask before reusing its code.
