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
  `VAR0068` (Shizuka), `VAR0448` (Miki); `F_Suzume_Met`, `F_Wendy_Met` (they add the girls to the harem's picker).
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
  `F_Maris_State2`, `F_Menad_Flag`, `F_Sill_State` — they change a hint's text, not the game. Proposed first: count
  them as text only. **They count** (owner, 2026-10-22), as everything but the bookkeeping and Rance's counter does:
  a silent step hides a scene otherwise. Sill's (unit 2) harem night `L_020a7` sets her state 2 (`Gｼｨﾙ:1773`); her
  next talk plays her repeated line (read) and sets 3 silently (`Gｼｨﾙ:1025`); only the talk after it plays the new
  scene (`:83` → `L_00297`). Counted as text only, the talk at 2 would be "read", the player would have no reason to
  talk, and the new scene would stay hidden. Every reader in the scenario (besides the boot's
  and the debug start's resets): `F_Haurein_State2` her harem scenes (`Gﾊｳﾚｰﾝ:2710`, `:3133`) and her leaving on a
  talk (`:2300`), the hint `Iｶﾝｶﾞｴ:360`; `F_Maris_State2` which of Maris's scenes plays (`Gﾏﾘｽ:957-960`), the hint
  `Iｶﾝｶﾞｴ:93`; `F_Menad_Flag` which of Menad's scenes plays (`Gﾒﾅﾄﾞ:32-35`), the hint `Iｶﾝｶﾞｴ:461`; `F_Sill_State`
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

## 6. Open questions

- Where the marks are drawn (the Subordinates picker's cells and Talk, the harem picker's cells): the mock-ups, later.
