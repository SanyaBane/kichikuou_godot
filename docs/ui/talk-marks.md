# Marks on Talk and the harem — "something new" (survey, 2026-10-22)

The owner asked (2026-10-22) for marks on the subordinates' Talk (`Cﾌﾞｶ`) and on the harem (`Cﾊｰﾚﾑ`): in the
original the player cannot tell before a talk or a visit whether it will play something already read in this
playthrough or something new. **The point: the player has no reason to start a talk or a visit when nothing new can
happen.** The mock-ups (where the marks are drawn) come later; this survey settles the condition.

## 1. What the original does

- **Talk** (`Cﾌﾞｶ.ADV:25`, `:208-712`): the unit's own scene file (`SubordinatesCommand.TalkFiles`) is called with
  `Ev_Entry = 20` (set once, EVT-06); Honey/Prince (unit 48) go to `Gﾊﾆｰ`/`Gｵｳｼﾞ`, Louis (unit 49) and Cecil
  (unit 50) to `Gﾖｳﾍｲ` 3/4, Rance (unit 1) talks to himself in `Cﾌﾞｶ` (`L_01cf3`). One talk a week.
- **The harem** (`Cﾊｰﾚﾑ.ADV:35`): the girl's scene file with `Ev_Entry = 10`; before the picker a draw
  (`:6`, `Y 4,4`, STR-14) may play Suzume's or Wendy's first meeting instead.
- The scene file picks what to play by a chain of tests on the state, e.g. Sill's (unit 2) talk
  (`Gｼｨﾙ.ADV:79-98`): the prologue line at `StoryStage = 0`; at `F_Sill_State = 3` a scene that sets 4 (it plays
  once); `F_SillClothes_State = 1`; `F_Sill_State = 4`; `Unit_Troops < 50` (`Gｼｨﾙ2` 2); else the default line. One-off
  scenes consume their flag and the talk falls back to a repeated line. The tests are not only story flags (Sill's
  troops), so a talk can turn new because of a battle.
- **Rance's self-talk** (`Cﾌﾞｶ.ADV:1338-1377`): `N_RanceSelfTalk` + 1, then one line for talks 1-9, another for
  10-19, "............" from 20 on. The counter is read nowhere else (only reset at the boot and a new game), so the
  talks change nothing but the line.
- **Random draws and menus**: of 50 talk scenes 25 draw (`Y 4`) and 3 have a menu; of 52 harem scenes 6 draw and 12
  have a menu (the agent's walk of every entry path, 2026-10-22). The kinds, §3.

## 2. The rule (owner, 2026-10-22)

The first idea was a "New" mark when a scene can reach a line not read in this playthrough. The owner turned it
round: **a talk or a visit is marked "read" only when it is certain that nothing new happens; otherwise it has a
mark.** "Nothing new" means, for every branch the scene can take in the current state (every outcome of every draw):

1. it shows only lines already read (a line is its place in the scenario, `ScriptRef`); and
2. it changes no variable that counts (§4) — so a talk whose text is read but whose draw can move the story on is not
   "read" (Kentarou's training, Isoroku's stage 2, §3).

A line read once is read: the record of read lines is kept **per save**, in the save's `Extras` (as
`KnownProvinces`); a save from before the feature starts with an empty record.

The marks:

| Mark | When |
|---|---|
| New | something new is certain (every branch new, or the new branch forced, §5) |
| ??? | something new is only possible, and it is not a harmless variant (§3, kinds 3-4) |
| read | nothing new on any branch |

The port computes it by a dry run of the scene on a copy of the state (as the differential tests do), every draw
forked, nothing shown, played or counted (no sound, no `Glob_CgSeen`, no save). Predicting the real draw from the
generator would tell the outcome of chance (whether Haurein leaves this week) — **not done** (owner, 2026-10-22).
**The menus inside the scenes** (owner, 2026-10-22): a menu is the player's choice, so every item counts as
reachable — a talk or a visit whose menu has an item leading to an unread line has a New (what it changes is the
player's choice). Only the character's cell is marked; the menu's items are not. Lines past an ending (a game over
included) do not count: the mark gives no reason to end the game.

## 3. The random draws

The kinds over all entry paths:

1. **A harmless variant of a repeated scene** (29 files: 24 talks, 5 harem scenes; e.g. Maria's (unit 16) 1 of 5
   answers, `Gﾏﾘｱ:679`). No state changes.
2. **A rarer scene, no state** (Rick, Cafe; §3.1-3.2).
3. **A draw that changes the state** (Haurein, Kentarou, Isoroku, Kanami; §3.3-3.6; Suzume's and Wendy's first
   meetings, `Cﾊｰﾚﾑ:6`, are left alone).
4. **A menu that changes the state, an arc or the game's end** (13 menus in 12 files, §6).
5. **A menu that changes only the text** (4: Leila's (unit 5) "who do I like", Lia's, Chizuko's (unit 38), Warg's
   dreams) — the owner counts them with kind 1.

Dead draws: `Gﾊﾞﾚｽ:201` is never tested; `Gﾊｳﾚｰﾝ:2299` draws even when its condition fails. Both only move the
generator.

### 3.1 Rick (unit 7), talk — harmless

`Gﾘｯｸ:8` (`Y 4,4`): 1 → Wendy and Rick chatting in a hallway while `F_Wendy_State = 1` (from Wendy's first harem night
to the DO Pot (item 9) used, `COM:477-480`); 2 → while `F_LeilaRick_State` is exactly 6 a talk about Leila (unit 5),
or, Leila dead, Rick mourning her; otherwise `:19` (`Y 4,2`) one of two common scenes (his helmet, a refused
sparring bout). Nothing changes, every scene repeats (`Unit_Cur` is left at 5, which no one reads).

### 3.2 Cafe (unit 45), talk — harmless

The first talk is not drawn and plays once (`F_Cafe_State` 20 → 21, the Monkey Statue (item 16), one of God's Door's
statues; `Gｶﾌｪ:543-546`). Afterwards `:396` (`Y 4,3`): 1 with Satella (unit 29) in the army → Cafe uneasy about
fighting beside demons (Satella does not appear); else children in town. Nothing changes. In hostage mode
(`F_Cafe_State` 10/11) a fixed scene and no draw.

### 3.3 Haurein (unit 12), talk — she leaves for good

`Gﾊｳﾚｰﾝ:2299` (`Y 4,3`), past stage 1 and unless the marriage scene (`F_Haurein_Arc = 1`) is due: 1 with
`F_Haurein_State2 > 1` (her S&M Tower night, with Barres present) and Barres (unit 6) in the army (ready or deployed)
→ `L_00a19` (`:563-566`): her pregnancy test is positive, she writes to Barres and leaves Leazas Castle;
`F_Haurein_Fate = 2`, her `Unit_Status = 0`. Cannot be undone (nothing sets her back to 1). Lost with her: the good
fate (overwritten if set), the harem, the crochet scene if unseen, the Library Ghost's step 2 → 3 (`Cｼﾊｲﾄｼ:562-566`),
and possibly Ex (unit 14) — at his reinstatement Haurein pleads first, Rick second, with neither he is dismissed
(`Gｴｸｽ:536-552`). Otherwise the kiss prank (repeats). The scene says why she left, not that her unit is gone.

### 3.4 Kanami, harem — feeds the Code ending

The first visit is not drawn (`Gｶﾅﾐ:178-181`). Later `:182` (`Y 4,5`): 1 with `F_Kanami_Scene2 = 0` → scene 2 with a
menu (Rape Her Anyway sets `F_Kanami_Scene2 = 1`; Stop sets nothing and gives no EXP and no count); 2 with the S&M
Tower built → the tower scene, `F_Kanami_Scene1 = 1`, repeats; else the default visit. `Gｺｰﾄﾞ:1444-1459` (Code's event
4) counts `F_Kanami_Scene1`-`4` (3 and 4 from her ninja missions): **two or more → Kanami elopes with Code, Code kills
Rance, ending 3** (`:1863-2118`). Code's arc needs Kanami sent twice into a Zeth province during the Zeth war; without
it the flags do nothing. Nothing in the text hints at it.

### 3.5 Kentarou (unit 31), talk — Rance Attack, silently

The training scene at `F_Kentarou_State` 5 or 6 ends with `Gｹﾝﾀﾛｳ:608` (`Y 4,3`): 1 → state + 1, the text the same.
At 7 the scene branches (`:514-516`) to `L_00b51`: Kentarou counters with Rance Attack, state 8, "learned to use
Rance Attack". From 8 `FIGT_SEL:579-588` offers him Rance Attack (`Battle.Flow.Player.cs:229-231`): a commander
strike on the enemy commander's HP even with 10+ troops, 1-6 shots, no counter, 1 Focus. About seven talks on
average. `Gﾐｷ:3756` (the "Rape Demon Lord" choice) resets the state to 0 for good.

### 3.6 Isoroku (unit 27), harem — pregnancy, silently

Only at `N_Isoroku_Stage = 2` (after her first night): `Gｲｿﾛｸ:4599` (`Y 4,5`): 1 with the Hospital built → stage 3,
the text the same. Her next talk or visit announces the pregnancy (stage 4, the birth in 40 weeks); until the birth
her visits give no EXP; she cannot be deployed in the birth week (`COM:172-175`); stage 6 gives two Completion Report
lines (`OVER:1305`, `:1360`, with ending 8). She is neither lost nor gained by it.

## 4. Variables that count

An agent walked every talk and harem entry path (and `Cﾌﾞｶ`/`Cﾊｰﾚﾑ` around the calls), listed every variable
written and every reader in the whole scenario, and cross-checked the port's rule code (2026-10-22). The walk follows
every condition both ways, so its list is a superset: it put Freak's (unit 20) leaving (`Gﾌﾘｰｸ:579-582`, entry 2
only) and Patton's (unit 18) handing over Lang Bau (`Gﾊﾟｯﾄﾝ:2161`, entry 1 only) on their talks, where a draw's
chain covers every value and nothing falls through (`Gﾌﾘｰｸ:232-240`, `Gﾊﾟｯﾄﾝ:1079-1903`, checked by hand). The
dry run (§2) has no such error: it runs the scene. 236 variables, in three classes:

- **Bookkeeping (73), ignored**: the harem's availability flags (rebuilt on every harem command, read only by its
  picker), `Harem_ExpReward`, `Harem_LastPick`, `Harem_LastWeek`, `Harem_SexCount`, the cursors (`Unit_Cur`,
  `Cmdr_Cur`, `Item_Cur`, `Prov_Cur`, `Hist_Cur`), `Ev_Entry`, `CallArg`, the `Tmp*`, the window and map camera state,
  `Ui_CmdHaremAvail`.
- **Read only on talk and harem paths (13)**: `N_RanceSelfTalk`; `F_Asuka_IntroSeen`, `F_Freak_TalkSeen`,
  `F_Hubert_TalkSeen`, `F_Menad_TrainingSeen`; `F_Isoroku_Flag`, `F_Leila_Flag`, `F_Milli_Flag1`, `F_Milli_Flag2`,
  `VAR0068` (Shizuka), `F_Miki_PowerScene` (Miki); `F_Suzume_Met`, `F_Wendy_Met` (they add the girls to the harem's picker).
  They were to be ignored as "text only", on the reasoning that they only pick a later scene, which the dry run of
  that later talk sees. That fails when a read branch moves such a variable silently (below), so **they count**
  (owner, 2026-10-22), all but `N_RanceSelfTalk`: the owner wants no mark on Rance's talks 2-9 and 11-19 (§1).
- **Counts (150)**: read elsewhere — weekly events, commands, nation turns, battles, other event entries, the
  endings. The story flags (most `F_*_State`, `N_*_Stage`); the records (a unit's status, troops, strategy; item
  holders; a province's owner; the battle variables of Satella's (unit 29) duel); `EndingId` (Satella's talk → ending
  7, Katyusha's and Saulnia's harem → ending 12); flags read only by the epilogue (`F_Amiran_Fate`,
  `F_Melfeis_Fate`, `F_Menad_Fate`, `F_Wendy_BedroomSeen`) or only by another event entry (`F_Kanami_Scene1`/`2` by
  Code's event 4, `F_Merim_Relation`, `F_Sill_SMTowerVisited`, `F_Thieves_State`, `F_Elisabeth_State`).

Refinements:

- **Flags whose only other reader is the Think command** (`Iｶﾝｶﾞｴ`, its hints): `F_Haurein_State2`,
  `F_Maris_State2`, `F_Menad_HaremSeen`, `F_Sill_State` — they change a hint's text, not the game. Proposed first: count
  them as text only. **They count** (owner, 2026-10-22), as everything but the bookkeeping and Rance's counter does:
  a silent step hides a scene otherwise. Sill's (unit 2) harem night `L_020a7` sets her state 2 (`Gｼｨﾙ:1773`); her
  next talk plays her repeated line (read) and sets 3 silently (`Gｼｨﾙ:1025`); only the talk after it plays the new
  scene (`:83` → `L_00297`). Counted as text only, the talk at 2 would be "read", the player would have no reason to
  talk, and the new scene would stay hidden. Every reader in the scenario (besides the boot's
  and the debug start's resets): `F_Haurein_State2` her harem scenes (`Gﾊｳﾚｰﾝ:2710`, `:3133`) and her leaving on a
  talk (`:2300`), the hint `Iｶﾝｶﾞｴ:360`; `F_Maris_State2` which of Maris's scenes plays (`Gﾏﾘｽ:957-960`), the hint
  `Iｶﾝｶﾞｴ:93`; `F_Menad_HaremSeen` which of Menad's scenes plays (`Gﾒﾅﾄﾞ:32-35`), the hint `Iｶﾝｶﾞｴ:461`; `F_Sill_State`
  which of Sill's scenes plays (`Gｼｨﾙ:75-89`, `:1025`), whether the harem offers her (`Cﾊｰﾚﾑ:56`), the hint
  `Iｶﾝｶﾞｴ:21`. Besides the hints they only pick the character's own talk and harem scenes, which the dry run of
  the next talk or visit sees.
- **What every visit pays or earns is not news** (owner, 2026-10-22: as proposed). Some writes happen on every branch of every visit: Rance's (unit 1)
  EXP from the harem; Rebecca's `Bt_ExpBonusNext` (the next battle's EXP bonus, `Gﾚﾍﾞｯｶ:14`, `FIGT:2980`);
  Shariela's `Shariela_EvWeek` (her 40/42-week timer restarted, `Gｼｬﾘｴﾗ:18`, `COM:868`, `:873`); `N_Lia_Neglect`
  (+1 per visit to another girl while `F_Lia_State = 1`, `Cﾊｰﾚﾑ:30-33`, reset by Lia's visit, read by `COM:162`,
  `:167`). Counted, they would keep these visits from ever being "read". So a write the visit makes on every branch
  is part of the visit, not something new.
- **Compare values, not writes** (owner, 2026-10-22): Rolex's (unit 47) talk sets `F_Rolex_GraveSeen = 1` every time (`Gﾛﾚｯｸｽ:17`);
  only the first changes anything.

## 5. Proposed rule change: an unread harmless variant comes for certain

Agreed by the owner (2026-10-22): **an option, on by default** (Option → Game), one switch for all of it — the
harmless variants, the draws that change the state, Kanami's choice; off, the original's odds and Code's event 4 as
the original has it. It would be **decision 30, the third rule change on purpose**:

- In Talk and the harem, when a draw's outcomes include branches that are harmless (they change no variable that
  counts, §4) and show a line not read yet, the outcome is one of those, at random among them; once all are read the
  original's odds hold. E.g. Cafe's (unit 45) 1/3 scene comes on the next talk.
- **The draw is still made** and the generator moves as in the original; only the outcome is replaced (through
  `IScriptRandom`), so every later draw — battles, events — is the same.
- This point is for the harmless branches only; the draws that change what counts have a point of their own below.
- So kinds 1, 2 and 5 never need a mark of their own: an unread variant is a certain New.
- **A point of its own: the draws that change the state** (owner, 2026-10-22, for Haurein, Kentarou and Isoroku):
  the usual scene first; once it is read, the drawn outcome comes for certain whenever its conditions hold — Haurein
  leaves on the talk after her kiss prank was read (§3.3); Kentarou's training moves his state on every talk after
  the first (Rance Attack after four training talks, not about seven, §3.5); Isoroku's pregnancy comes on her second
  stage-2 visit with the Hospital built (§3.6). Their mark is "???", never New. Kanami's scene 2 and tower scene
  (§3.4) come for certain the same way once her usual visit is read, with a plain New: Code's event 4 asks the
  player (below), so they hold no danger (owner, 2026-10-22). Suzume's and Wendy's first meetings (`Cﾊｰﾚﾑ:6`) keep
  the original's 1 in 4 (owner).

**Kanami chooses in Code's event 4** (owner, 2026-10-22; the first change of a story branch, so a decision of its
own, 31, or a point of 30). In the original the ending is not in the harem: Code's event 4 (`Gｺｰﾄﾞ` 4) is a weekly
event (`COM:248-252`, `F_Code_State = 3` during the Zeth war) that comes once, the week after Code's event 3, and
counts the four flags (`Gｺｰﾄﾞ:1444-1459`): two or more → Kanami goes with Code, he kills Rance, ending 3
(`F_Code_State` 5); fewer → she stops him, he dies (4); after it the flags change nothing. The chain: event 1 on a
ninja mission of Kanami's (`Bt_ScoutNinja = 1`) into a province of Zeth's (`Cﾆﾝｼﾞｬ:351`, `:548`), event 2 a weekly
event (`COM:315`), event 3 another such mission (`:355`, `:554`). The other two flags come from the ninja command:
`F_Kanami_Scene3` by its "Get Me A Burger" item (offered 1 in 5 when Kanami is sent, `Cﾆﾝｼﾞｬ:281-294`),
`F_Kanami_Scene4` by a 1 in 5 scene when she is sent (`:92-98`); they stay as they are.

- With fewer than two flags, the original's path: Kanami is loyal, a choice would not fit.
- With two or more, our menu right after her doubts (the scene's lead-in ends with her asking herself whether to stop
  Code, then the clock strikes one) and before the count: **"Stop Code"** plays the fewer-than-two branch (it opens
  with her "I'm a ninja before a woman" and reads as her decision; nothing in it tells the flags), **"Go with Code"**
  the branch of ending 3. The items are the port's own English text, as worded here (owner, 2026-10-22).
- So ending 3 stays reachable, but only on purpose.
- It needs a new mechanism: `Gｺｰﾄﾞ` runs from the player's files, and the port can route a whole file to its own code
  but not answer one test inside a scene. A scene patch — one test (`Gｺｰﾄﾞ:1457`) answered by our menu — is small
  and general. The differential tests run without it.
- The differential tests run without it, so the files run as the original there.

**Kentarou's hint** (owner, 2026-10-22): when his training moves the state (5 → 6, 6 → 7), our own short narration
line after the scene (the port's text, as decision 26's save window) tells the player he is getting closer; the two
lines differ a little, so the second is not a repeat. The owner chose (2026-10-22): **"Kentarou seems to be catching
on to the move."** (5 → 6) and **"Kentarou has nearly caught on to the move."** (6 → 7). The original lines stay
untouched.

## 6. Mock-ups, round 1 (2026-10-22)

Drawn over dumps of our screens from `saves-week18` (`tools/ui/mock_marks.py`; the marks are made up). Our Harem screen's
tablets already show "new" in green where the girl was never summoned (`HaremView`), else her visits; the marks take
that place's meaning.

- **HA — the slot**: the tablet's right slot shows NEW (green), ??? (amber) or, when read, the visits; the info panel
  gains a third figure, "This visit: Something new" (or "Something may happen").
- **HB — a corner**: a folded corner at the tablet's top right, green or amber with "?", none when read; the slot always
  shows the visits ("—" for none).
- **HC — read tablets dimmed**: a read tablet fades (still a click); NEW and ??? as filled pills.
- **SA — a pill in the ledger's margin**: NEW (green ink) or ??? (vermilion) left of No., nothing when read; the chosen
  unit's Talk button carries the same pill.
- **SB — a speech bubble per unit**: a filled green "!" new, a vermilion "?" a draw decides, an empty outline read; Talk
  for a read unit says "nothing new".

**The owner's choice (2026-10-22), drawn as H and S:**

- **The harem: HA without the visits** — the tablet's right slot shows NEW (green) or ??? (amber), and nothing when read;
  the tablet no longer shows the visits (they stay in the panel below the picture, which gains "This visit"). The
  Summon button carries the chosen girl's NEW or ??? pill at its top right, as Talk does, none when read (owner).
- **Subordinates: SB's bubbles in the ledger, SA's pill on Talk** — a speech bubble left of No. in every row (filled
  green "!", vermilion "?", an empty outline when read); the chosen unit's Talk button carries a NEW or ??? pill at its
  top right, none when read.

**Built (2026-10-22, step 8):** H and S as drawn, in code (docs/ui/pickers.md §5, docs/ui/subordinates.md §7).

## 7. Open questions

None: the condition and the look are settled (2026-10-22).

## 8. The build — plan (2026-10-22; built the same day, §9)

What the code has today (an agent's survey, 2026-10-22): scenes run in `Engine/Script/SceneRunner` (a sealed runner, one
`switch` over `ScriptProgram`'s statements, jumps resolved to indexes; no hook on a statement); a shown line is a
`MessageEvent`/`ValueMessageEvent` carrying its `ScriptRef`, raised by the runner and by hand ports' `TextAsync`, and
menu items carry theirs as captured text; every draw goes through `ScriptContext.Rnd` → `IScriptRandom`
(`godot/Strategy/RandomLog.cs` is a decorator over it); a state copy exists only in the tests
(`TestSupport/Differential.Copy`, without `Extras`); `Extras` (`Dictionary<string, int[]>`) is saved whole, a new key
needs no format change; the only "seen" record is MsgSkip's, global (`msgskip.txt`); no setting reaches rule code
today — the rule-visible switches are context properties off by default (`ResumeCommand`, `ReplenishRequest`), which
is what keeps the differential tests (which build their contexts by hand) running the files as the original;
Option → Game holds only the game folder.

Steps, each committed with its tests:

1. **The read lines, per save** (Engine). The context notes every line it shows — `MessageEvent`, `ValueMessageEvent`,
   the items of a menu shown — in `Extras`, one key per file (`read:<file>`, its lines a sorted `int[]`, as
   `KnownProvinces`). Notes only: no rule reads them, the differential tests compare no `Extras`. An old save starts
   empty. Tests: the record, a save and load keep it.
2. **A state copy in the engine**: `ScriptState`'s copy (variables, pages, strings and `Extras`) moves out of the
   tests' `Differential.Copy` into the engine, which the tests then use.
3. **The dry run** (Engine, `Script/ScenePreview`): runs a given start (an async entry on a context) on copies of a
   state again and again, each run with a script of decisions — the value of each draw, the item of each menu, key
   waits answered at once —, branching at the first decision its script does not hold, until every path is run
   (a cap on the paths; past it the mark is ???). A run's view records the lines; nothing is shown, played, counted
   or saved (a fresh context: no `Glob_CgSeen` binding, a memory save store). A path ends at the start's end, at a
   jump out (`&`, an ending), or at a call the scene router does not run (a battle, `FIGT_A`): such a call counts as
   something happening. Per path: its decisions (which were draws), its lines, the state at its end. Tests on
   hand-made programs.
4. **The mark** (Original): the talk and the visit as entries the dry run can start — `SubordinatesCommand`'s talk of a
   unit and `HaremCommand`'s visit of a girl, the same code the commands run (with the visit's writes before and
   after the scene). A path is new when, as of the start, it shows an unread line or changes a variable that counts
   (§4: by value; not the bookkeeping, not `N_RanceSelfTalk`; not a write every path makes); a path that ends the
   game does not count. Read: no path is new. New: a new path the player reaches by menus alone, or one the rule
   change (step 5) makes certain. ???: new only by a draw — the draws that change the state listed in decision 30
   (Haurein `Gﾊｳﾚｰﾝ:2299`, Kentarou `Gｹﾝﾀﾛｳ:608`, Isoroku `Gｲｿﾛｸ:4599`; Kanami's `Gｶﾅﾐ:182` is New), any other
   such draw the dry run meets, and, with the option off, every draw. Content tests on the player's files: Sill's
   silent step (§4), Rance's talks 1, 2, 10, Maria's variants, Haurein, Kentarou, Isoroku, Kanami, a talk with a menu
   (Leila), an ending path (Katyusha) left out.
5. **What is unread comes first** (Original; the option's flag on `OriginalScriptContext`, off by default). During a
   real talk or visit a random decorator draws as the original does, then replaces the value: at each draw it runs
   the dry run from the scene's start (a copy taken there) with the decisions made so far, and (a) chooses at random
   among the values whose continuation is new and changes nothing that counts (a harmless unread variant), else (b) at
   a listed draw, the outcome that changes the state when neither the path so far nor the other values'
   continuations hold a line unread at the scene's start (the usual scene read: Haurein's kiss, Kentarou's training,
   Isoroku's stage-2 visit, Kanami's usual visit), else (c) keeps the value. Suzume's and Wendy's draw (`Cﾊｰﾚﾑ:6`)
   is outside the scenes and keeps its odds. Content tests: Cafe's Satella scene on the next talk, Haurein's leaving
   on the talk after the kiss, Kentarou's four training talks, Isoroku's second stage-2 visit, Kanami's scenes; the
   draws consumed one for one; with the flag off, the original's values.
6. **Scene hooks** (Engine `SceneRunner`, Original): a hook the rule set gives the runner, called at statements of
   chosen `ScriptRef`s — a condition answered by the rules, a step after a statement. With the option on: **Kanami's
   choice** at `Gｺｰﾄﾞ:1457` (`{Tmp0001 > 1:`): when the count is two or more, our menu "Stop Code" / "Go with Code"
   (a menu of the port's own text, as `PortTextEvent` is for text) answers the test; **Kentarou's line** after
   `Gｹﾝﾀﾛｳ:609-611` when the state moved to 6 or 7: our narration (the window opened through `WAO_SUB`'s ops, the
   line as `PortTextEvent`, a key wait). Tests on the player's files; with the option off nothing is asked or shown.
7. **The option**: Option → Game, a check box "New scenes first" (a `*Choice` over `settings.cfg`, on by default),
   copied onto the game's context where `GameScreen` takes `session.Script`.
8. **The screens** (§6, the owner's H and S): `HaremRoster`'s girls and `SubordinatesScreen`'s rows and Talk button
   get their marks, computed by the dry run when `Sﾊｰﾚﾑ` or `Sﾌﾞｶ` opens (from the state the picker sees: `Ev_Entry`
   and the command's own writes as at the call); `HaremView`: the slot NEW/??? (no visits), the Summon pill, "This
   visit"; `SubordinatesView`: the bubbles left of No., the Talk pill. Dumps from `saves-week18` for the owner's look.
9. **Docs**: decision 30 built, CLAUDE.md, the backlog.

Steps 1, 3 and 6 reach the script runtime, so the full content tests are due once the build is settled; steps 4-5 run
the Talk and harem files the differential tests compare (`CommandSliceDifferentialTests`), which must see no
difference with the flag off.

## 9. Decisions taken during the build

The owner answered two questions before the build (2026-10-22); every other choice below was taken by the agent
during the build, for the owner to review.

**The owner's answers at the start (2026-10-22):**

- **The marks while Talk is spent**: when the week's talk is spent (`CmdAvail_SubTalk = 0`), the Subordinates screen
  shows neither the bubbles nor the Talk pill — the marks appear only when they can be acted on.
- **Old saves**: the record of read lines starts empty for a save from before the feature; MsgSkip's global record
  (`msgskip.txt`) is not used to seed it.

**The agent's decisions** (date, step, the question, the choice, why, the alternatives):

- **2026-10-22, step 1 — which line a menu item is.** The item's texts (`CapturedText.Text`), not its `$label$`
  (`SceneMenuItem.At`), which holds no words; every item of a menu shown is noted, the ones not chosen too (they were on
  the screen). Alternative: only the chosen item — but the others were read as much.
- **2026-10-22, step 1 — where the lines are noted.** The context's `View` is wrapped (`ScriptContext.ReadLines.cs`), so
  every message — the scenes, the hand ports, the battle's direct calls — is noted in one place; menus go through a new
  `ScriptContext.SelectAsync`, which the runner, `CommandFile` and `CommandPhase` now call (the input could not be
  wrapped: it is compared and cast elsewhere). Alternative: a noting call at each call site — a later one would slip past.
- **2026-10-22, step 1 — noting is always on.** No switch: it changes no run (no rule reads it, no comparison sees
  `Extras`), and the marks need it whatever "New scenes first" says. Lines our interface answers unseen (the attack
  briefing, the Load/Save menu answered by our panel) are still noted: the script showed them, and they are command
  lines, not the talk and visit scenes the marks are about. The battle's own labels (`ActionLabelsEvent` and the like)
  are not noted: no talk or visit reaches a battle file.
- **2026-10-22, step 2 — the copy's form.** `ScriptState.CopyTo(target)` (exact: a page the source lacks is freed on
  the target, `Extras` replaced whole and cloned) and a factory `ScriptState.Copy(source)`; copying into one target again
  reuses its pages, for the dry run's many copies. The `DS` bindings are the state type's, not copied. `Differential.Copy`
  is gone; the tests call the engine's copy. Alternative: a clone each variant implements — needless, the memory is the
  whole state.
- **2026-10-22, step 3 — the start's shape.** `ScenePreview<TState>(source, Func<PreviewRun<TState>, Task> start)`: the
  caller builds its own context and runner over the run's parts (`State`, `Random`, `View`, `Input`, `Saves`), so the
  engine knows no rule; `Run(forced, inspect)` is synchronous (every part answers at once), so step 5 can call it from
  inside a real draw. Alternative: an async `RunAsync` — no caller could use it inside `IScriptRandom.Next`.
- **2026-10-22, step 3 — the draws' places.** A scene's draw names its `Y 4` statement: `ScriptContext.Rnd(n, at)` hands
  the place to a generator that is an `IPlacedRandom` (the dry run's), the runner passes `s.At`; a plain generator draws as
  before, a hand port's draw has no place. Steps 4-5 need it for the listed draws (`Gﾊｳﾚｰﾝ:2299`, …). Alternative: the
  draw's index in the path only — the listed draws could not be told apart.
- **2026-10-22, step 3 — the order of the values.** A draw's 1..n, a menu's items 1..n and then its cancel (0, a reachable
  right click), depth first: under the path cap the cancel is what is cut first.
- **2026-10-22, step 3 — the answers.** A key wait is a click (Return, 16; a right click, 32, closes windows), a pause runs
  out (0), the mouse is released at (0, 0), the cursor keeps the engine's shapes (1 at first), the date is fixed (Saturday,
  1 January 2000; only `Gﾜｵ:555` reads it in the scenes), the timer runs a second at each read (a wait on it ends), nothing
  plays. The caps: 256 paths (`Truncated` past it), 100,000 steps a run (an event, an input or a draw; past it the path is
  `Aborted`) — a forever release loop costs about 25 ms. A loop that shows and asks nothing is not caught.
- **2026-10-22, step 3 — the save data.** `PreviewRun.Saves` ends the path (`Stopped`, "save data") at any read or write:
  a preview never touches a slot nor plays on with a slot it cannot read.
- **2026-10-22, step 3 — the end states.** One state is reused for every run and seen through `Run`'s `inspect` as each
  path ends; `KeepEndStates` gives each path its own (`PreviewPath.EndState`). A path's lines are what its run showed,
  repeats included, whatever the copy's record held.
- **2026-10-22, step 3 — the programs.** `CachedSceneSource` keeps every program read, shared by the runner each run
  builds.
- **2026-10-22, step 4 — the talk and the visit as entries.** `SubordinatesCommand.PreviewTalkAsync(unit)` and
  `HaremCommand.PreviewVisitAsync(girl)` run the command's own code from the pick on, from the state the picker sees:
  the talk sets `Ev_Entry` 20, the picker's answer, the unit's names, then the talk chosen (`TalkChoiceAsync`, or the
  prologue's `PrologueTalk`, both shared with `RunAsync`) and `TalkAsync`; the visit sets `Ev_Entry` 10 and
  `Harem_ExpReward` 0, the picker's answer, the pick's bookkeeping (`Pick`) and `VisitAndRewardAsync` (her scene and the
  reward), both shared with `RunAsync`. Left out of the talk: the picker's close and, past the prologue, the troop menu
  before Talk (the header, the offer, the menu) — they show only the command's lines and write only windows and
  temporaries. `CommandSliceDifferentialTests` (full counts) see no difference. Alternative: run `RunAsync` with the
  picker and the menu answered — slower, and every talk would carry the offer's lines.
- **2026-10-22, step 4 — the command's own lines.** "Maybe I'll meet with X." (`Cﾌﾞｶ:1104`, before every talk past
  the prologue) and "Rance gained experience" (`Cﾊｰﾚﾑ:566`, after a visit that pays) are never unread for the marks
  (`SceneMarks.FrameLines`): the same on every talk or visit, they would mark every one New until first read. Rance's
  self-talk lines (`Cﾌﾞｶ:1338-1377`) are his talk and count (his talk 10 is New by line 1357).
- **2026-10-22, step 4 — the files the scenes call.** Every call in the scene files (`G*`, `I*`, `FIGT表示`):
  `WAO_SUB`, `M`, `WAO_MAP`, `PCMPLAY` run in the context as always; the scenes (`Gｱﾘｽ2`, `Iｼﾚｲﾌﾞ`, `FIGT表示`, …) in the
  runner; `FIGTATTK` (the exchange's animation, from `FIGT表示`) runs its port with its draws given 1 and no decision (its
  one draw, `FIGTATTK.ADV:234`, is never read; as decisions it would double the paths at each exchange); the battles
  (`FIGT_A`, `FIGT_B`, `FIGT`, `死亡捕獲`), the party picker `Sﾌﾞﾀｲ` and any file not foreseen stop the path (something
  happens there); `&#OVER.ADV` (14 jumps: endings, game overs) ends it, left out. The preview's router is its own
  (`SceneMarks.PreviewRouter`), not `StrategyRules`; its input is a `PreviewInput` that is an `IGameInput` (a battle's
  command or menu stops the path, never reached). Alternative: route through `StrategyRules` and stop only the battles —
  every other ported file would be one more way to touch the game.
- **2026-10-22, step 4 — what counts.** `SceneMarkState`: every variable, page slot and record name, by value, except
  the bookkeeping — the dictionary's groups `scratch`, `ui`, `system`, `array`, `unused`, `debug` (the temporaries, the
  window, map, camera and mouse state, the harem's availability flags, `Harem_LastPick`, the week's command flags,
  `RetVal`, `CallArg`, the persistent page 4 with `Glob_CgSeen`, the scratch page 3, the map marks' page 10) and by name
  the record cursors and strides, the string bases, `Ev_Entry`, `Harem_ExpReward`, `Harem_SexCount`, `Harem_LastWeek`,
  `CmdAvail_SubTalk`, `CmdAvail_SubTroops` and `N_RanceSelfTalk`. A bound variable is compared on its page (a slot takes
  its field's class; an unbound slot counts unless its page is all bookkeeping). Strings: only the records' names
  (`Unit_StrBase` to the last item's, `SET.ADV:298-301`; scenes rename records), the rest is scratch. `Extras` never
  (the read lines, the provinces seen). Alternative: the §4 walk's 73 names alone — the groups cover them and whatever
  the walk did not meet.
- **2026-10-22, step 4 — what every visit pays.** An explicit list, on visits only (`SceneMarkState.VisitFeeNames` and
  Rance's (unit 1) experience): `Bt_ExpBonusNext`, `Shariela_EvWeek`, `N_Lia_Neglect`, `Units[1].Exp`. Rejected: "a slot
  that differs on every path of the result is ignored" — Sill's (unit 2) silent step is a single path, so it would hide
  exactly the change §4 is about.
- **2026-10-22, step 4 — the paths' ends.** Returned: new by an unread line or a counted change. Stopped (a battle, the
  party picker, save data) and Loaded: new. Jumped and Quit (an ending, a game over): left out — read, whatever lines
  they show, and never chosen by the rule change. Aborted (the steps' cap) and a result past the paths' cap: at least
  ???.
- **2026-10-22, step 4 — repeated decisions (engine).** A menu's cancel that shows the menu again (Leila's (unit 5)
  "who do I like", Somita's (unit 46) talk, Aoi's, Arlcoate's, Chizuko's, Warg's visits) and a draw drawn again until
  another value comes (Gandhi's (unit 37) talk, `Gｶﾞﾝｼﾞｰ:59`) made endless trees, cut at 256 paths (??? for six talks
  and visits, ~500 ms each). The dry run now ends a path where it meets a decision at a place it made one already, of
  the same kind and count, with the same memory (`ScriptMemory.Fingerprint`, 64 bits; `PreviewEnd.Repeated`): from there
  it would go on as the first time. Forced decisions are never cut. In the tree such a value is no outcome
  (`MarkNode.Outcomes`): a menu's cancel that only comes back is no choice, a redraw ends with another value. The call
  stack is not part of the fingerprint (a recursion with the same memory would be cut; none was met).
- **2026-10-22, step 4 — where a decision falls among the lines (engine).** `PreviewPath.DecisionLines`: how many lines
  the path had shown before each decision (a menu's items among them), so the tree knows each value's own lines.
- **2026-10-22, step 4 — the tree's rule.** `MarkTree`: a leaf as above; a menu its best outcome; a draw by `Choose`,
  the one function step 5 will call: (a) the outcomes that are harmless — every path under them returns (no ending, stop,
  abort or missing value) and changes nothing beyond what every returning path through the draw changes alike
  (`MarkTree.Common`: the prefix's writes, so a draw after a first-time flag still finds its harmless values) — and whose
  continuation is New, evaluated from the value's own lines on with the changes ignored and every draw below by the same
  rule: one of them, the mark New; (b) at a listed draw, when no line before the draw and no line under the other values
  is unread, the outcomes that change what counts (never one that may end the game or stops): one of them, the mark the
  listed one — ??? for Haurein (unit 12), Kentarou (unit 31), Isoroku (unit 27), New for Kanami — no better than the
  chosen outcomes' worst; (c) the odds: New if every outcome is, Read if every one is, else ???. Option off: (c) always.
  The four listed references are verified as the `Y 4` statements' lines (`Gﾊｳﾚｰﾝ:2299`, `Gｹﾝﾀﾛｳ:608`, `Gｲｿﾛｸ:4599`,
  `Gｶﾅﾐ:182`). Alternative for (a): a value with any unread path — would choose a value whose unread line hides behind
  a further draw it cannot make certain. With the scene before a draw unread and nothing new after it, (a) and (b) do
  not apply and the odds keep the draw — the talk is New anyway (Kentarou's first training: no step).
- **2026-10-22, step 4 — the API.** `SceneMarks` (Original), built over the scenes once: `Talk(state, unit, on)`,
  `Visit(state, girl, on)` (a `SceneMarkReport`: the mark and the tree, each path's end and why it is new), `Talks` and
  `Visits` (the marks of every unit `Sﾌﾞｶ` lets talk, status 1, and every girl `Sﾊｰﾚﾑ` offers, her `HaremAvail_*` flag),
  `Explore(state, target, forced)` and `Preview` for step 5, `Rules(on)`. `GameSession.Marks` holds one, sharing a
  `CachedSceneSource` with the game's runner. Nothing of the game is touched (a test checks the memory's fingerprint and
  `Extras`). Timing (2026-10-22): the owner's week-18 save, 6 talks 10 ms and 5 visits 8 ms once the scenes are read
  (126 ms and 43 ms the first time, reading them); all 50 talks and all 50 visits of a booted game at stage 1, about
  150 ms each once read (470 ms and 290 ms the first time). Step 8 may read the scenes ahead (a first mark at the boot).
- **2026-10-22, step 4 — Katyusha's ending.** Ending 12 (`Gｶﾁｭｰｼｬ:2293`) comes from "Have Threesome" on Katyusha's visit or on
  Saulnia's (which calls `Gｶﾁｭｰｼｬ` 100), and only while Sill (unit 2) is out of the army (`:1847-1848`: with her there,
  Rance is saved); the test sets her out.
- **2026-10-22, step 5 — the flag.** `OriginalScriptContext.NewScenesFirst`, off by default (as `ResumeCommand` and
  `ReplenishRequest`), one switch for steps 5-7: step 6's Kanami's choice and Kentarou's lines read it too, step 7 copies
  the option onto it. The dry run it needs is `OriginalScriptContext.Marks` (`GameSession` gives its `Marks`); without
  them the rule does not act. The differential tests build their contexts by hand and set neither.
- **2026-10-22, step 5 — where the rule hooks in.** Two virtual hooks in the engine's context, overridden by the
  original's: `ScriptContext.Drawn` (in `Rnd`, after the generator has drawn: the value the script gets) and
  `ScriptContext.Selected` (after `SelectAsync`: the menu's answer). `Rnd` stays the only caller of `Next`, one `Next` per
  draw, so the generator's sequence is the original's (a test compares a logging generator's draws with the rule on and
  off). Alternative: a decorator around `IScriptRandom` — the context's generator is fixed when it is built (the launch's,
  `EngineState`), the decorator would still need the menus from the context, and two routes would note one run.
- **2026-10-22, step 5 — where a talk or a visit starts.** The talk where Talk is chosen (`Cﾌﾞｶ`'s `L_014b2`, before
  `TalkChoiceAsync`, or the prologue's pick, before `PrologueTalk`), the visit where the girl is picked (after `Pick`,
  before `VisitAndRewardAsync`); both end in a `finally` (a jump out, a load). The copy is taken there, and the dry run's
  start (`PreviewTalkAsync`, `PreviewVisitAsync`) repeats on it only the writes the real run has just made (the picker's
  answer, the names, `Tmp0021`, the pick's bookkeeping), then runs the same code. Alternative: the copy at the picker's
  state, as the marks take it — a Replenish or Add before Talk in the same command would leave it stale (the scenes read
  the troops: Sill's (unit 2) `Unit_Troops < 50`). `Cﾊｰﾚﾑ:6` (Suzume's and Wendy's draw) is before the visit's start.
- **2026-10-22, step 5 — the decisions noted.** As the dry run makes them: each draw of 2 or more as the value the script
  got, with its place; each menu as its item or 0, its first item's place; `FIGTATTK`'s draws (the exchange's
  animation, from `FIGT表示`) as none, since the dry run gives them 1 — `StrategyRules` runs it in
  `SceneChoices.MutedAsync`. "Unread" is as of the copy (the start), as the tree reads it: a line the talk shows before
  the draw counts as unread, so Kentarou's (unit 31) first training (unread before his draw) keeps the odds.
- **2026-10-22, step 5 — the value among the rule's.** The drawn value when it is one of the values the choice gives,
  else the ((drawn − 1) mod count)-th of them: no draw of its own, the same for the same seed. Not uniform when the
  draw's n is no multiple of their count: Kanami's `Y 4,5` gives scene 2 for 1, 3 and 5, the tower scene for 2 and 4.
  Alternatives: a second draw (the generator would move unlike the original's), a generator of the rule's own (one more
  piece of state to keep with the saves).
- **2026-10-22, step 5 — the fallback.** At each draw the dry run's node at the decisions so far must be a draw of the
  same count and place; when it is not (the forced decisions no longer fit — after a battle, which the dry run stops at —,
  a key answered otherwise than the dry run's click, anything unforeseen) or the dry run throws, the rest of that talk or
  visit keeps the values drawn, and `SceneChoices.Fallbacks` / `LastFallback` keep it (a test makes the dry run fail).
  A node the dry run ends as a repeat (a draw drawn again with the same memory, as Gandhi's (unit 37) `Gｶﾞﾝｼﾞｰ:59` may be;
  not met in the tests) keeps the value and the rule goes on.
- **2026-10-22, step 5 — what the tests found.** Kentarou (unit 31) from state 5: the first training talk keeps the odds,
  the second steps 5 → 6, the third 6 → 7, the fourth plays Rance Attack (state 8, no draw) — four training talks, as §5
  says. Isoroku's (unit 27) first stage-2 visit gets a harmless unread value (her scene after the draw is unread too), the
  second her pregnancy (stage 3). Haurein's (unit 12) first talk the kiss, the second her leaving; Cafe's (unit 45)
  Satella scene and Kanami's scene 2 or tower scene on every seed. A real talk with the rule takes about 4 ms once the
  scenes are read (31 ms the first time).
- **2026-10-22, step 6 — the hook's shape (engine).** `ISceneHooks`, given to `SceneRunner` as an optional constructor
  argument (none by default, so every runner built so far runs as before): `Conditions` and `Steps`, the places
  (`ScriptRef`s), and `ConditionAsync(script, at, holds)` — the test evaluated first, the rules return the answer the
  scene goes on with — and `AfterAsync(script, at)`, after the statement ran (not after one that leaves the page). The
  runner marks the places once per file (a flag array beside the cached program; a file without places costs one
  dictionary look-up a call, a statement one test). A condition's place counts only on an `IfStatement`. Whether a
  patch acts is read from the context at the moment (`NewScenesFirst`), so step 7 can switch it during a game.
  Alternatives: a settable property (the marks would have to be dropped when it changes), a call at every statement
  (a hash of a string per statement).
- **2026-10-22, step 6 — the original's patches.** `Strategy/ScenePatches` (one stateless `Instance`), given by
  `GameSession`; the dry run's runner gets none (`SceneMarks`: its context has the flag off anyway, Kanami's menu is not in a
  talk or a visit, and Kentarou's hint shows no line and makes no decision). The places are verified on the decompile:
  `Gｺｰﾄﾞ:1457` is `{Tmp0001 > 1:` (`{VAR0001 > 1:` in the raw decompile), its block `:1458` `@L_021f7` (`F_Code_State` 5
  at `:1864`, ending 3 at `:2117-2118`), the fewer-than-two branch `:1460` (`F_Code_State` stays the 4 set at `:1337`);
  `Gｹﾝﾀﾛｳ:610` is `!F_Kentarou_State : F_Kentarou_State + 1!` inside `{RetVal = 1:` (`:609`).
- **2026-10-22, step 6 — where Kanami's menu comes.** At the test, as the hook answers it: right after "The clock struck
  one." (`:1441`) with its key wait and the window closed (`:1442-1443`, ops 1 and 0) and the silent count — the same screen
  her own menu in `Gｶﾅﾐ` (`:774-794`) comes on (a key wait, op 0, then `]`). The window is cleared after it, as the runner's
  `]` does. A cancel (a right click) asks again until an item is chosen: a story choice must be made, and a cancel in the
  original's menus goes on after the `]`, which here would be no choice at all. Alternative: a cancel as "Stop Code" —
  a slip of the hand would decide the story.
- **2026-10-22, step 6 — the port's menu items.** `SceneMenuItem.OfPort(at, text)`: an item whose one text is a
  `CapturedText` with `Port` set (the port's words, as `PortTextEvent` is for text); its `At` and its text's place are the
  statement its choice runs next ("Stop Code" `Gｺｰﾄﾞ:1460`, "Go with Code" `:1458`), so inputs that name items by place
  (the tests' `MenuAnswers`) can. `GamePresenter.Read(CapturedText)` returns the port's text as it is, so the original's
  `SelectionBox`, our `SceneMenuView` (fed by the presenter's `Menu`) and the message log (`MenuAnswered`: the first item's
  file is an event scene, so the answer is logged) draw it with no change of theirs; `ScriptContext.SelectAsync` and the dry
  run's input never note a port text as read. Unattended play (`AutoChoice`) takes the first item, "Stop Code".
  Alternatives: a new item type (every consumer of `SceneMenuItem` would have to tell the two apart), an item at the test's
  line (the two items would share one place).
- **2026-10-22, step 6 — Kentarou's hint.** After `Gｹﾝﾀﾛｳ:610` (the step, so only when the state moved), at the state 6
  the first line, at 7 the second (never else). It comes after the scene's last line and its closed window
  (`:603-607`) and before `M 0` (`:612-613`) brings the map back: window 113 through `WAO_SUB` (`WaoAsync(113)`, the
  scene's narration window `\L_022ad`), the line as `PortTextEvent`s, a key wait (op 1, which keeps `RetVal`), the window
  closed (op 0) — as decision 26's save text is printed, and leaving the window state as the scene's own op 0 left it.
  The owner's line is broken in two where it passes the scene's 30 cells ("Kentarou seems to be catching" / "on to the
  move.", "Kentarou has nearly caught on" / "to the move."), as the scene breaks its lines with `R`; the words are the
  owner's, joined by a space. `PortTextEvent` gained an optional `At` (here `Gｹﾝﾀﾛｳ:610`): the presenter takes it as the
  scene's text, so the message log keeps the narration as an event scene's (a port text at no place, decision 26's,
  stays out as before); it is never noted as read. MsgSkip treats it as decision 26's text (no line to have seen).
- **2026-10-22, step 6 — the tests.** Engine (`SceneHookTests`): a test answered both ways, the scenario's answer kept, a
  step after its statement, nothing asked away from the places, a port menu not noted. Content (`ScenePatchesTests`, on
  the player's files with `SceneChoicesTests`' rig, which now wires `ScenePatches` as `GameSession` does): Code's event 4
  with two flags — "Go with Code" → `F_Code_State` 5, ending 3 (`&OVER`); "Stop Code" after a cancel → `F_Code_State` 4,
  the event returns; one flag → no menu; off with two or four flags → no menu, ending 3. Kentarou from state 5, every draw
  1: the two lines after the first two talks (window 113 after "Ah... Yes.", then ops 1 and 0, then `M 0`), none on the
  third (Rance Attack); off, the same steps and nothing shown. `SceneChoicesTests`' four training talks still need four
  with the hints in them and no fallback. `MessageLogTests`: the port menu drawn and logged, the hint logged.
- **2026-10-22, step 7 — the option's place and key.** Option → Game, a section "Scenes" under the game folder with the
  check box "New scenes first" and the hint "Talks and harem visits play unread scenes first, with a few hints and a
  choice of ours." — it names what the switch adds (the hints, Kanami's choice) without telling the story (no Code, no
  Kanami: the options window is open from the first launch). Kept as `[game] new_scenes_first` in `settings.cfg`
  (`godot/Strategy/NewScenesChoice`, the `*Choice` pattern; a section of its own, as the other `[display]` keys are the
  look's). Alternative: the hint the plan suggested ("…; Kanami chooses in Code's event.") — a spoiler of stage 2.
- **2026-10-22, step 7 — the copy and a change during a game.** `GameScreen.RunAsync` sets
  `session.Script.NewScenesFirst` from the choice next to `Library`, before the boot; the options window raises
  `NewScenesFirstChanged` and `GameScreen.OpenOptions` sets the running context's flag at once (as the text speed, the
  font and the palette reach the screens). The patches (Kanami's choice, Kentarou's line) read the flag at their
  statement, so they follow at once; `SceneChoices` reads it at a talk's or visit's start (`Begin`), so a switch while
  a talk or a visit is under way (the window opened at one of its key waits) takes hold from the next one. No change of
  the rule code: a talk half under each setting would make the dry run's forced decisions mean nothing.
- **2026-10-22, step 7 — Defaults on the Game tab.** The Game tab now shows Defaults like the other tabs; it turns New
  scenes first back on and leaves the game folder alone (a folder has no default). Alternative: no Defaults on the tab
  as before — then the one tab with a switch would be the one that cannot reset it.
- **2026-10-22, step 7 — unattended play.** `--auto` and the development switches use the saved choice (on unless the
  developer turned it off), as a player's game does: they are smoke tests of the game as it is played, and nothing in
  them needs the original's odds — `AutoChoice` answers Kanami's menu with its first item, "Stop Code" (step 6), and
  Kentarou's line is a key wait like any other. The generator is untouched by the rule, so the kept saves' looks
  (battles, events) draw the same; only a talk's or visit's outcome, and what follows from it, may differ. Alternative: a fixed value for
  `--auto` (on or off) — a switch of its own nobody asked for, and runs that would not match the developer's play.
- **2026-10-22, step 8 — when the marks are computed.** At the picker's own event: `GameScreen.ShowAsync` hands every
  `PickerScreenEvent` that opens `Sﾊｰﾚﾑ` or `Sﾌﾞｶ` to `PickerMarks.OnPicker` right after the presenter, so the state is the
  one at the picker's call (`HaremAvail_*` set, `Ev_Entry` 20 for `Sﾌﾞｶ`), and the screens read the result each frame
  (never computed in the draw). `Sﾌﾞｶ` raises the event at its mode 2 and again at its mode 1 (and after the status page),
  so a Subordinates opening computes twice — 25-33 ms each on `saves-week18`; the harem's 19-35 ms (the Godot build's
  timings). After a troop order the screen opens again and computes again; after a talk the talk is spent. Our interface
  only (`InterfaceChoice.New`). Alternative: once per command (a fingerprint of the state to skip the second) — not worth
  its code at these costs.
- **2026-10-22, step 8 — the model.** `Original.Content/Strategy/PickerMarks` (over the session's `SceneMarks`) keeps the
  last opening's trees and the marks read from them (`Visits`, `Talks`); a switch of "New scenes first" while a picker is
  open reads the kept trees with the other rules (`SetNewScenesFirst`, from `NewScenesFirstChanged`), no run and no stale
  state. A talk or visit whose dry run throws gets no mark (`Failures`, logged as a warning); the rest are marked.
  `HaremGirl.Mark` and `SubordinatePanel.TalkMark` carry the marks, `HaremRoster.VisitText` and `PickerMarks.Label` the
  words. Alternative: the marks in `GamePresenter` — it serves the original's interface too, which shows none.
- **2026-10-22, step 8 — the talk spent, in the model.** `SubordinatesScreen.TalkOpen` is Talk's own condition (not the
  view-only picker, and past the prologue `CmdAvail_SubTalk` 1); `SubordinatesScreen.TalkMarks` gives no mark at all while
  it does not hold, so the ledger has no bubbles and Talk no pill; `PickerMarks` runs nothing then. Tested on hand-made
  states (`PickerMarksTests`).
- **2026-10-22, step 8 — the units that cannot talk.** No bubble: a unit not at status 1 (deployed this week, wounded,
  …) cannot be given Talk, its pick opens the status page. A read talk's bubble is the empty outline (S); every bubble's
  tooltip says it in words ("Talk: something new", "… may happen", "… nothing new"). Talk's pill also goes while the button
  is greyed (an order under way).
- **2026-10-22, step 8 — a read visit, and no mark.** A read visit: the slot empty and no pill (H), "This visit: Nothing
  new" in the panel's quiet colour (the mock-up drew only "Something new"; a figure that vanishes would read as missing).
  No mark known (a failed dry run): nothing in the slot, no "This visit", no pill.
- **2026-10-22, step 8 — the bubbles' place.** In the ledger's first column (the chooser's), widened by the bubble's room
  on the right, the chosen row's bar and diamond in the rest; widened always on the Subordinates screen, so the columns do
  not move when the marks go (a spent talk). Godot's `Tree` tints a custom cell over its drawing, which turned the chosen
  row's bubble olive: that cell's tint is drawn by its callback under the bubble instead.
- **2026-10-22, step 8 — the scenes read ahead.** At COMMAND's map waits (our interface), once a session, one talk or visit
  per frame — the units able to act and the girls on offer at the first such wait (`PickerMarks.Warm`) — so the first
  picker does not read the scenes: on `saves-week18` 134-162 ms spread over the frames. Alternatives: at the boot (the
  state is not the game's yet, and another stall behind the title), all at the first map wait (a stall of up to ~0.8 s on
  a full stage-1 game), none (the first opening reads them: up to ~0.5 s late in a game). A picker opened before the
  reading is done (`--harem` at the first map wait) reads what it needs itself.
- **2026-10-22, step 8 — the look.** From `saves-week18` (`--harem 9 --until deployment --interface new`): every girl and
  every unit NEW, as expected (the save predates the read lines). A second look a week on (the save played through week
  18 with Haurein's (girl 9) visit, then `--harem 9` again) still shows NEW everywhere: her second visit has unread lines.
  The ??? and read bubbles and the absent pill were not seen in a window; the models are tested.
