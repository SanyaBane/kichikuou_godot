# Strategic layer — survey index and plan

Everything outside the battle: the week loop, the player's commands, the enemy nations' turns,
data initialisation, and the story scenes as seen from the rules. The battle
([../battle/](../battle/README.md)) is ported and verified; this layer follows the same process —
survey → spec per part → port per part → golden captures.

## Surveys (2026-10-08)

| File | Scripts | Lines | Rules in it |
|---|---|---|---|
| [survey-main.md](survey-main.md) | `MAIN`, `COMMAND`, `COM`, `MMODE`, boot/title/save/endings | ≈15k | week loop (~650), player-phase shell (~250), `COM` weekly upkeep and event triggers (947); the rest is presentation |
| [survey-commands.md](survey-commands.md) | `C*` except the dungeon, `S*`, `各種DATA` | ≈15.6k | < 2k; ~85 % is dialogue and scene dispatch |
| [survey-dungeon.md](survey-dungeon.md) | `Cﾒｲｷｭｳ`, `Cﾀｶﾗ`, `Cｹﾝｷｭｳ` | ≈5.8k | floor counter, encounters, 27 dungeon records, 15 bosses; ~half text |
| [survey-nations.md](survey-nations.md) | `T*`, `T*2` | ≈9.5k | data-driven army AI on one shared template; `*2` files pick defenders when the player attacks |
| [survey-data.md](survey-data.md) | `SET`, `SET_CHR`, `SET_ITEM`, `SET_MAP`, `SET_VER` | ≈11k | pure data (no draws, no conditions); `SET_VER` is dead |
| [survey-events.md](survey-events.md) | `G*`, `I*` (136 files) | ≈423k | ~40 commands, goto-based; 19 rule mechanics hidden in scenes (~950 lines to hand-port) |

| [survey-ikami.md](survey-ikami.md) (2026-10-11, decision 17) | `Iｶﾐ` (with `FIGT表示` 21-24) | ≈6.5k | the garrisons on 2F-5F and their 20 kaiju battles, the menu before Rudrasaum, endings 9, 10, 11, 13; no draws |

Not yet surveyed: `FIGT表示.ADV` beyond its entries 21-24.

## What the surveys established

- **One week** (`MAIN.ADV`): week + 1, history shift, autosave → (before Leazas is unlocked: a
  thieves-only week) → Leazas phase: income, troop/HP refill, expenses, `COMMAND` (which first
  calls `COM`: weekly upkeep, then two tiers of story triggers) → 11 gated nation phases in fixed
  order → end-of-week story checks.
- **Player phase.** No command ends it; each uses its own weekly flag (`Ui_Cmd*Avail`,
  `CmdAvail_*`) and returns to the command loop; only "Turn END" ends the phase. These `ui`
  flags are rule state — the golden comparison must stop ignoring them.
- **Nation AI** is fixed data: commander priority lists, troop thresholds, ordered target
  provinces; the first player-owned target is attacked through `FIGT_B` (already ported).
- **Scenes** are entered by setting `Ev_Entry` and calling the file (795 dispatch entries in
  125 files; no central table). They change the world through plain variable writes (unit
  join/leave, owners, items, gold, story stage).
- **Randomness** is everywhere and must keep its order: `COM` 12 sites, commands 21 live,
  dungeon 11, nations 60, scenes 108 (77 of them only choose dialogue, but all consume the
  generator).
- **Save** = one autosave per week at the start of the player phase; a manual save copies it.
  Pages 0, 1, 2, 4, 10 and all strings; page 4 (endings, gallery) is shared by every slot.

## Parts

Each part is one agent's spec + port, ≈2-4k script lines, following
[../porting-conventions.md](../porting-conventions.md). Dependencies point left.

| Id | Part | Source survey | Needs |
|---|---|---|---|
| **F0** ✓ | Shared services out of `Combat` into `Kichikuou.Core.Script` (2026-10-08): `ScriptContext` (state, draws, pauses, scene calls, jumps) with `WAO_SUB` (`ScriptContext.Wao.cs`), `M`/`WAO_MAP` (`ScriptContext.Map.cs`), `PCMPLAY`/`SS`/`SR` (`ScriptContext.Sound.cs`); `ScriptEvent`/`IScriptView`, `IScriptInput` (`IBattleInput` extends it), `IStoryHooks`; one `WindowEvent` for every `WAO_SUB` call. Script menus and map clicks are generalised with E1/E2 and W3 | main, dungeon, data | — |
| **F1** ✓ | Data (2026-10-08): `Setup/GameSetup` ports `ｽﾀｰﾄ`'s state lines, `SET.ADV` line by line (DAT-01 shadow slot, the layout arithmetic's scratch) and the new game (`ｽﾀMENU.ADV:244-290`); `SET_MAP/CHR/ITEM` run at boot from the player's raw decompile through `Kichikuou.Content.Setup.ScenarioScripts` (a strict reader of their statement forms) and `ScriptContext.RunAsync` (straight-line statements, `Script/ScriptStatements.cs`, the seed of E1's IR). Checked against the first prologue capture: page 1 differs only by the 7 slots one week of play changes; strings, markers and `VAR0134` equal; the F2 boot capture (root `SET.ADV`) replays with 0 differences | data | F0 |
| **F2** ✓ | Harness (2026-10-08, [../golden-tests.md](../golden-tests.md)): nested captures with per-group class tables (`tools/golden/classes.txt`; group folders under the captures folder); roots `SET` (boot), `MAIN.ADV@20` (one week, a label root), `COMMAND`, `COM`, every `T*`/`T*2`, `Cﾒｲｷｭｳ`, `Cﾃｷﾄｼ(2)`, `Cﾌﾞｶ`, `Cﾊｰﾚﾑ`, `Sﾌﾞﾀｲ`/`Sﾌﾞｶ`/`Sﾊｰﾚﾑ` and the prologue scenes; records `mouse` (`IM`, run-length), `cursor` (`IC`), `cdpos` (`SC`), `date`/`timer` (`ZT`); the weekly command flags, harem list and page-10 markers compared (pages 2, string 10 always were); captures of roots without a replay entry point are parsed and listed as skipped. Boot capture verified; **the prologue captures wait for the owner's session** (checklist in golden-tests.md) | main, commands, nations | — |
| **E1** ✓ | Event converter front end (2026-10-08): `Kichikuou.Content.Scripts.ScenarioScripts.ReadProgram` reads a raw decompiled file into a `ScriptProgram` (`Script/ScriptProgram.cs`, `Script/ScriptStatements.cs`): one statement per engine command, jumps/blocks/label calls resolved to statement indices, text commands grouped into `MessageStatement` runs (`ScriptRef` + the `H`/computed-`X` expressions), menu items as `$label$`…`$` around ordinary text (which the runtime routes into the item, also from calls: `ﾛｰﾄﾞｾｰﾌﾞ.ADV:129-131`), presentation-only commands as `EngineStatement`; no idiom rewritten (stubs, release waits and dispatch run as the original). Expression precedence from xsys35dc `print_cali`. All 136 `G*`/`I*` files plus `ｵｰﾌﾟﾆﾝｸﾞ`, `FIGT表示`, `ﾛｰﾄﾞｾｰﾌﾞ`, `OVER` read, every text resolves through `ScenarioText`, and the survey counts reproduce (795 entries in 125 files, 108 `Y 4`, 123 menus / 426 items, 14 `&#OVER`) | events | — |
| **E2** ✓ | Event runtime (2026-10-08): `Script/SceneRunner` runs a `ScriptProgram` statement by statement on the `ScriptContext` and is the context's `IStoryHooks` — scene files (`G*`, `I*`, `ｵｰﾌﾟﾆﾝｸﾞ`, `FIGT表示`) run there, every other file goes to the rule hooks it wraps (scene → battle → scene works); the call stack follows `scenario.c` (label calls, `US`/`UG` restored on `\0:`/`%0:`, `%0:` from any depth), the menu being built is engine-global and captures its items' text when printed (`SceneMenuItem`); new input `ISceneInput` (menus, `IM`, clock, CD, MIDI) and events (`LineBreakEvent`, `MessageClearEvent`, `MessageResetEvent`, `EngineEvent`). Save-data commands and `ZZ0` are not ported (save screen, ending). Tested on synthetic scenes and on the player's files: the new game with `Iﾌﾟﾛﾛｰｸﾞ`, the world effects of the prologue scenes, the first Pillage (`Iﾄｳｿﾞｸ` 1: picker, battle, `Y 4, 4`, gold), `ｵｰﾌﾟﾆﾝｸﾞ` to its end and every entry of the milestone scenes; `GoldenReplay` replays scene captures through it (`scenes:`), proven on a capture the core writes — **the owner's `scenes` captures are still to be recorded** | events | E1, F0 |
| **W1** ✓ | `COM` weekly upkeep and triggers (2026-10-08): the whole file hand-ported in `Strategy/WeeklyEvents` (early exits, upkeep, tier 1 "all that apply", tier 2 "first match" with its 12 rolls), scenes through `ScriptContext.EventAsync`; quirks STR-05, STR-14 to STR-18. Checked against `COM.ADV` itself run by `SceneRunner` from the player's decompile on 2400 random states, most aimed at one call by a guide that reads the program's conditions (every one of the 149 (file, entry) pairs is reached; calls, draws and memory equal — `WeeklyEventsDifferentialTests`), plus unit tests; `GoldenReplay` replays `com` captures (root `COM.ADV`) — **none recorded yet** | main | E2 |
| **W2** ✓ | `MAIN` week loop + the nation-phase hosts hidden in scenes (RULE-1). Stage 0 done (2026-10-08): `Strategy/WeekLoop` ports `MAIN.ADV:1-82` by hand — `RunWeekAsync` is one pass of `L_00020` (the `week` capture's span), `RunAsync` the endless loop; the top of the week (`WAO_SUB` 3, `M` 0, `Week + 1`, the history shift STR-01, music off), the thieves' week (autosave, date, music 1, pan to the hideout, banner, units Rance (1), Sill (2), Bound (3), Soul (4) refreshed, `Hist_Funds`, `COMMAND`, `StoryStage 99 → 1` + `Iﾊﾝﾗﾝ`, "Then some time passed"); the Leazas week (`:83-1045`) threw `NotSupportedException` before its first statement until milestone 2. The autosave is a call of `ﾛｰﾄﾞｾｰﾌﾞ` with `RetVal 3` (decision 6), ported with G0 part 1; the week loop names the call (`ScriptContext.SaveResume`) so a slot knows where a load goes on. Routed in `StrategyRules` (`MAIN.ADV`); `GoldenReplay` replays `week` captures (label root `MAIN.ADV@20`, `ReplayableLabelRoots`), proven on a capture the core writes and on the owner's 2026-10-09 weeks. Checked against `MAIN.ADV` itself run by `SceneRunner` on 1500 random states for one to three weeks (calls, events, inputs, memory equal — `WeekLoopDifferentialTests`), unit tests (`WeekLoopTests`), and `PrologueCommandTests`: the new game's first two weeks through the router, and Turn END in week 23 → `Iﾄｳｿﾞｸ` 2 (duel lost) → `ｵｰﾌﾟﾆﾝｸﾞ` → stage 1 + `Iﾊﾝﾗﾝ` → the Leazas week. The Leazas week (milestone 2 step 1, 2026-10-10): `:83-1044` ported whole by hand in the same class (now on the `CommandFile` base, which reads the army names `MS 10, "…"` from the player's files) — the autosave at `:83` and the resume after it, income (`Tmp0022`, leaves `Prov_Cur` at `Prov_Count`), Cream's counter, the refill of units 1-48 with its draws (items 48/50, STR-06), the report, the donations, `COMMAND`, the scenes after it, the eleven gated phases in order (Mars, AL Church, the rebels with commanders Ex (1) through Jibul (7) healed fully, JAPAN, Helman, Patton STR-07, Zeth with its capital search, Kayblis, the Camilla unit's inline AI with its eight draws and `FIGT_B` calls STR-02/03/04, Creator STR-13, La Vaswald) and the end-of-week checks (`Iﾏｼﾞﾝ` 1 and `Gﾏｼﾞｯｸ` 5 with their unguarded music STR-24, Camilla 0 → 1, Sill's kidnapping chain, the majin timer, the demon and Kayblis wars STR-08); the nation turns, `FIGT_B` and the scenes are calls through the router. Checked against `MAIN.ADV` run by `SceneRunner` on 3000 random states (thieves' and Leazas weeks, every phase gate opened now and then, draws logged; every phase, battle and scene call reached — `WeekLoopDifferentialTests`), unit tests, and the owner's Leazas week capture (week 13, stage 1: the autosave, `COMMAND`, `TCﾘｰｻﾞｽ`, `Gｼｨﾙ` 21 and the exit with 0 differences) | main, events | W1, nations as hooks |
| **W3** | `COMMAND` player phase (2026-10-08, in progress): the whole file hand-ported in `Strategy/CommandPhase` — `COM`, the construction timer, the weekly flags, the map loop, both menus, Turn END, the click routing; every other file is called by name through the hooks. Owner decision: the map is read as the script reads it — `IM` polled, `IC` (`ISceneInput.SetCursorAsync`), the drag-scroll `WAO_MAP` op 1 ported in the context — and a UI that thinks in provinces queues gestures (`Script/PointerScript`). Quirks STR-19 to STR-21. Checked against `COMMAND.ADV` itself on 800 random states with random gestures (every file call reached; inputs, calls, events and memory equal — `CommandPhaseDifferentialTests`), the drag against `WAO_MAP.ADV` on 2000 pointer runs, plus unit tests; `GoldenReplay` replays `command` captures (root `COMMAND.ADV`). Part 2 (2026-10-08, owner's plan): the prologue paths of the command files ported by hand in `Strategy/` on a shared `CommandFile` base — `EnemyCityCommand` (`Cﾃｷﾄｼ:1-108`: header, menu, Pillage → `Iﾄｳｿﾞｸ` 1 and `ｵｰﾌﾟﾆﾝｸﾞ`, AL Church, Ninja, Return), `DistantCityCommand` (`Cﾃｷﾄｼ2`, whole), `SubordinatesCommand` (`Cﾌﾞｶ`: the picker loops, the status page `L_0179c`, the 50 talk labels, Rance's monologue, the prologue's `L_00f4b`), `HaremCommand` (`Cﾊｰﾚﾑ`: payment and refund, the stage-0 list, the 50 visit labels, the reward), `DungeonCommand` (`Cﾒｲｷｭｳ:1-174` with the names from the player's files, and `L_03cbd`); their other paths (the later harem list, the dungeon search and dispatch, the independent cities) throw `NotSupportedException` — the attack came with P3 (milestone 2 step 3), the troop menu with P6 (step 4), the dungeon search with D1 (step 8). The router `StrategyRules` (an `IStoryHooks` bound to the context) runs `COM`, `COMMAND` and these files and hands every other file to the hooks it wraps; it is the rule hooks of a `SceneRunner`. Quirk CMD-21. Checked against each file run by `SceneRunner` on 1500 random states (mostly stage 0) with random input and scribbling hooks (`CommandSliceDifferentialTests`; where the port stops at an unported path, the run is compared up to there), unit tests, and a prologue player phase on the boot's world with the player's scenes (`PrologueCommandTests`: Pillage, Bound's talk, Sill's visit, the hideout, Turn END); `GoldenReplay` has entry points for the `commands` roots. Part 3 (2026-10-08): the pickers ported by hand on a shared `PickerScreen` base (the 3×17 grid, the pointer loop with its highlight and details panel, the bar) — `DeploymentScreen` (`Sﾌﾞﾀｲ`: party toggle, remove-last, Go = validate + pay, Stop; BFL-01, CMD-16), `SubordinateScreen` (`Sﾌﾞｶ`, all modes), `HaremScreen` (`Sﾊｰﾚﾑ`, its name table read from the player's files: `ScenarioParser` now reads `#L_x, 1:` string tables, `F1`/`F3` and data lines, and `SceneRunner` runs `NC`/`NB` on bound variables); routed in `StrategyRules`; `PointerScript.PickerCell`/`PickerButton`/`PickerOutside` give a UI the gestures. Quirks CMD-22 to CMD-24. Checked against each file run by `SceneRunner` on 1500 random states with random pointers (`CommandSliceDifferentialTests`), unit tests (`PickerScreenTests`), and `PrologueCommandTests` (the week now picks Rance, Bound and Sill with clicks); `ScreenReplayTests` replays `screens` captures — **none recorded yet**. The battle's own `Sﾌﾞﾀｲ` call (`FIGT_B`) reaches the ported picker since the battle runs in the router on the game's input (decision 7). W2 (stage 0) followed | main, commands | W1 |
| **P2** ✓ | Army screens `Sﾌﾞﾀｲ`, `Sﾌﾞｶ`, `Sｱｲﾃﾑ`, `Cﾀｶﾗ` (replaces the battle's `Sﾌﾞﾀｲ` hook); `Sﾌﾞﾀｲ` and `Sﾌﾞｶ` are ported with W3 part 3, and the battle reaches them since it runs in the router (decision 7). The rest (milestone 2 step 7, 2026-10-10): `Cﾀｶﾗ` whole in `Strategy/TreasureCommand` (the items of units at status 0 lost — not owned, CMD-18 — then `Sｱｲﾃﾑ`) and `Sｱｲﾃﾑ` whole in `Strategy/ItemScreen` on `PickerScreen`, which gained the file's differences (both cursors follow the pointer, the cell under it is remembered even when not listed, a right click is 998, a list only in views 0 and 1, bar labels at other heights): the two views (`Ui_ItemViewMode`, kept between visits; units at status 1-2, or the items owned in the colours of stock, held and without effect), the details (portrait, war cost, troops, class, item; or the item's picture, whether it is equippable and who holds it), the first pick turning the view with its prompt, the equip `L_0043a` (the unit's own item to stock; the item off its holder, always the stock by then, through `Unit_Item[100]` = `Unit_DgnAtkMod[48]` — CMD-17, confirmed from the page layout), the questions with Yes/No in selection window 2 at the pointer (taking a held item off, `L_00b39`, or moving it to the unit picked), the refusals (items 1 and 2 and their holders, already equipped, no effect), the bar (997 the unlabelled first place taken for a pick — CMD-17 —, 502 the view, 503 End), the right click dropping a half-made pick, the exit with the cursors on the picks and `M` 0. Routed in `StrategyRules`; `GoldenReplay` roots (`Cﾀｶﾗ` in `commands`, `Sｱｲﾃﾑ` through `ScreenReplayTests`). Quirks CMD-17 (confirmed, effects), CMD-18, CMD-20, CMD-22 to CMD-24 extended. Checked against each file run by `SceneRunner` on 4000 and 500 random states with a pointer mostly over the grid (`CommandSliceDifferentialTests`: every view, refusal, question, prompt, equip and exit) and unit tests (`PickerScreenTests`, `CommandSliceTests`); no item or treasure capture recorded yet. The screen (milestone 2 step 7, 2026-10-10): `Kichikuou.Content/Strategy/PickerView` draws `Sｱｲﾃﾑ` by hand (`PickerKind.Items`) — the frame 819, the background of the view the screen was drawn for (600 the units, 692 the items: a first pick turns the view without drawing the screen again, so the other view's stays while the pick is half made), the view's grid (units at a status, white at 1-2; items owned, `Item_StrBase + n - 1`, 255 in stock, 253 held, 12 without effect), the details (the portrait or the item's picture 750 + n, panel `B4 9`), the bar's labels ("Item", "/Character" at size 12, "End"), and the half-made pick's prompt from GameState (`PickerView.PromptLines`: the picture panel as it was at the pick, shrunk to 186×116 at (11, 273) with `Surface.ScaledCopyFrom`, three lines at x 213); refusals and questions are the `WAO_SUB` 113 window over it, Yes/No selection window 2 at the pointer (which opens on No). Godot needed nothing new (the script hit-tests the pointer; a right click is 998). Tested headless (`GamePresenterTests`: a hover, a pick dropped by a right click, the item view, a question, an equip, every click through `PickerView.HitTest`); `--auto` opens Treasure once at stage 1 (a pick, its prompt, dropped, End: nothing equipped) | commands | F0 |
| **P3** | Enemy-city attack and Use Force (`Cﾃｷﾄｼ` rules, `Cﾃｷﾄｼ2`). Part 1 (milestone 2 step 3, 2026-10-10): the attack `Cﾃｷﾄｼ.ADV:99-425` ported whole by hand in `Strategy/EnemyCityCommand` — Maris's refusals (stage 4 `:109`, a conquest already made `:112`, the civil war at stage 1 `:118`, the rebels' first week `:144`), the Land of Death's warning (82), Patton's scenes, the stage-2 gate of Tower of Evil (78) / Grief Valley (89) / Bone Forest (90) / Tearing Forest (91) (`Iｼﾚｲﾌﾞ` 5/6), the mountain passes (Log B (31) / Log A (32): one unit unless a neighbour is held, `Tmp0021-0040` saved in `Buf_Val` around the speech), `Sﾌﾞﾀｲ`, a picker left empty back to the header (`@L_00078`), the units' refusals (44, Gandhi, Satella, Meglass; none at Osaka (56) during the JAPAN war), the defenders by owner (`TCﾘｰｻﾞｽ2` ported; `TJAPAN2`, `Tｾﾞｽ2`, `Tﾍﾙﾏﾝ2`, `Tｹｲﾌﾞﾘｽ2`, `GAL` 9 calls through the router; JAPAN's fleeing soldier with its names `MS 4/5/10` from the player's files; Patton's list inline on the army picker, which moved from `NationFile` to `CommandFile` since `Cﾃｷﾄｼ` has a copy, `L_04f2a`), the exits without a battle (Warg, the Zeth barrier — short and first-time, `F_ZethBarrier_State`, formerly `VAR0063` —, Papaya, the city nobody defends with its map markers) and `FIGT_A` with `Bt_Kind` 1; routed in `StrategyRules` with the scenario. Quirks BFL-01/02/14, CMD-01/02/19 marked in the port; the dictionary's `F_Warg_State` corrected (1 is the transient "met Warg at Tower of Evil (78)", 2 joined). Checked against the file run by `SceneRunner` on 5000 random states (stages 0-4, the rebels' provinces, the passes, Land of Death (82), Osaka (56), the stage-2 gate; scribbling `Sﾌﾞﾀｲ`, `T*2`, `FIGT_A` and `Gﾊﾟｯﾄﾝ` hooks; every refusal, setup file and exit reached — `CommandSliceDifferentialTests`), unit tests (`CommandSliceTests`), an attack on Oku (3) fought through the router after `Iﾊﾝﾗﾝ` (`PrologueCommandTests`), and all 38 of the owner's `Cﾃｷﾄｼ` captures replay whole (the stage-1 one: the "disrupted" refusal). The independent cities (`:429-3828`, Use Force with them) are P4 | commands | P2, N1 |
| **P4-P7** | Negotiations; own city + merchant; subordinates + ninja + construction; harem + `各種DATA`. P4 (milestone 3 step 4, 2026-10-10/11): the independent cities `Cﾃｷﾄｼ.ADV:429-3828` — their exits, the menu, Use Force (`Strategy/EnemyCityCommand.Cities.cs`) and the negotiations (`Strategy/EnemyCityCommand.Talks.cs`) — ported by hand, so `Cﾃｷﾄｼ` is ported whole; see "Milestone 3", step 4. P6, part (milestone 2 step 4, 2026-10-10): `Cﾌﾞｶ`'s troop menu (`:713-1112`) ported by hand in `Strategy/SubordinatesCommand`, so `Cﾌﾞｶ` is ported whole — the unit's header with the offer of new troops (by class and troop attack, units Bound (3) / Soul (4) and Leila (5) / Maria (16) / Patton (18) / Julia (33) / Aviator (36) apart, 320 for 10 men otherwise, none for Mill (25), Galtia (41), unit 48, Louis (49), Cecil (50), plus `Week / 4`), the menu with its ○/× items (`CmdAvail_SubTroops`, `CmdAvail_SubTalk`; the command spent when both are), Info (the status page), Add New Troops with Add More (men × 2, price × 3 while under 10000; CMD-12: a cancel keeps it), Replenish (refused when full, else one per 50 men, at least 1; CMD-20), Talk, Do Nothing; expenses booked to `Hist_Cur`'s slot (CMD-14). Checked against the file run by `SceneRunner` on 4000 random states (stages 0-4, every item, offer, refusal and payment reached — `CommandSliceDifferentialTests`), unit tests (`CommandSliceTests`), and the owner's two stage-1 `Cﾌﾞｶ` captures (a replenish, a talk with Cadmium) replay whole. P7, part (milestone 2 step 4, 2026-10-10): `Cﾊｰﾚﾑ` ported whole in `Strategy/HaremCommand` — after the prologue the draw `:7` on every visit (STR-14), Suzume's and Wendy's first meetings and Wendy's bedroom (each test reads `RetVal` afresh; none of these scenes writes it), the cafe's scene taking the visit (`:22-27`, also after a first meeting; `Gｶﾌｪ` 3 resets `LadyCaravan_Prov`, which `COM.ADV:55-56` raises by one a week, so it comes back about every five weeks while `F_Cafe_State` is 10/11), and the 50-row list (`:47-212`: units present through `Unit_Cur`, left at 27, and story flags); CMD-13/14/15/20/21 marked. Checked against the file run by `SceneRunner` on 4000 random states (stages 0-4, the intro flags, every bound of the list; scribbling `Sﾊｰﾚﾑ` and the scenes; every girl's scene, intro and the cafe reached — `CommandSliceDifferentialTests`), unit tests (`CommandSliceTests`), and the owner's stage-1 harem capture (week 14, a visit to Lia) replays whole; all 12 `Cﾊｰﾚﾑ` captures match. P5, part (milestone 2 step 5, 2026-10-10): `Cｼｮｳﾆﾝ` ported whole in `Strategy/MerchantCommand` — Sill's two scenes leaving without spending the merchant (CMD-08), the first summons (`F_Merchant_State` 0 → 1), Pluepet's return (3 → 4), the greetings, the menu looping until Send Away (the only thing that spends the week's merchant), the mercenaries Louis (49) and Cecil (50) with their contracts, refusals and first-hire scenes, Fuck Rebecca (once a visit) and Buy Rebecca, the shop's eight items with their gates, pages, the one-short price test (CMD-06) and Buy/Don't Buy, every purchase booked to `Hist_Cur`'s slot (CMD-14); the window-stub speech helper moved to `CommandFile` (`SpeakAsync`). Checked against the file run by `SceneRunner` on 3000 random states (every arrival, menu item, page, refusal and purchase reached — `CommandSliceDifferentialTests`) and unit tests (`CommandSliceTests`); no merchant capture recorded yet. P5, the rest (2026-10-10): `Cｼﾊｲﾄｼ` ported whole in `Strategy/OwnCityCommand` — the city's header, the menu (the four commands and the city's 34 places, ○ while the week's action is free and × otherwise, × items leading back to the menu; the Ruins Search ○ by the dungeon flag alone, rewriting dungeon record 30 (Ancient Ruins), CMD-09), Temporary Levy (the flavour draw, `RND(Economy × 10)` gold with no penalty CMD-07, the 30 story triggers in order with their cursors, the Cat Bread/Kuriripu/Kapala rolls drawn even when owned STR-14, the girl for the Special Room with her description draws), Bolster Defense (the cost, no gold check and no cap CMD-05, back to the header), the castle's buildings and cells (sound 74, Look Around), the Giant Library's chain, and the dead audience room `:1182-1235` (CMD-20). Checked against the file run by `SceneRunner` on 12000 random states (every place, cell, trigger, roll and Bolster path reached — `CommandSliceDifferentialTests`) and unit tests (`CommandSliceTests`); no own-city capture recorded yet. P6, the rest (milestone 2 step 6, 2026-10-10): `Cﾆﾝｼﾞｬ` ported whole in `Strategy/NinjaCommand` — Maris's regrets when no ninja is free, the pick spending the ninja at once (CMD-10; Kanami's interrupt `Gｶﾅﾐ` 12 gives her back and recurs while Rance forgives her), the mission menu with its burger draw for every ninja (STR-14), Sabotage barred against Helman, JAPAN and Zeth before their wars, Collect Info (`Ui_CityScouted`), the capture draws (1 of 100; over 90 of 100, of 300 against owner 8) with the Substitute Ninja and each ninja's capture, the success draw (a 3 keeps the stale `Tmp0001`, ENG-02) and each ninja's sabotage (Kanami the walls; Freya, Kaoru and Wichita a commander of the owner's range through `L_01691`, which zeroes record 100 — CMD-11, EVT-04; Saitou never), Code's scenes on Kanami's Zeth missions — and `Cﾃｲﾄ` in `Strategy/ConstructionCommand` — Build or Don't, Maris's refusal while a building is under way (each building at state 2 and the weeks left), the buildings not started (Maria's institute by her presence and her tulip; `Unit_Cur` left at 16), each offer with its terms and budget, Construct (the price booked to `Hist_Cur`'s slot CMD-14, the timer, state 2, the week's construction spent), Something Else, too little gold, the magic lab's Don't Construct, the "all built" opening that needs the pool nobody builds (CMD-20). Both routed in `StrategyRules` and `GoldenReplay` roots. Checked against each file run by `SceneRunner` on 6000 and 4000 random states (every ninja, mission, draw, capture, report and scene; every building, offer, refusal and payment — `CommandSliceDifferentialTests`) and unit tests (`CommandSliceTests`); no ninja or construction capture recorded yet. With them P6 (`Cﾌﾞｶ`, `Cﾆﾝｼﾞｬ`, `Cﾃｲﾄ`) is ported whole. P7, the rest (milestone 2 step 7, 2026-10-10): `各種DATA` ported whole in `Strategy/AllDataCommand` — the map in miniature with its markers at a third, the scales set on the first visit (16x), the menu (a cancel: the next left click shows it again, any other press leaves), the four graphs over history records 0-15 (the funds with the military and sundry spending stacked in each bar and the odd shrink when they top it, the sundry expenses, the military spending, the casualties in tens; bars cut at 300 with "OV", the numbers over them), the wait (left: back, right: the scale menu 1x-128x into `Ui_MoneyGraphDiv/Scale` or `Ui_DamageGraphDiv/Scale`, both buttons: the stale scale label, CMD-25) and the exit with `Hist_Cur` 0 (which undoes CMD-14's stale cursor); the parser reads `ZB` (bold). Routed in `StrategyRules`, a `GoldenReplay` root. Checked against the file run by `SceneRunner` on 1500 random states with presses of both buttons (`CommandSliceDifferentialTests`) and unit tests (`CommandSliceTests`); no All Data capture recorded yet. With it P7 (`Cﾊｰﾚﾑ`, `Sﾊｰﾚﾑ`, `各種DATA`) is ported whole. Its screen (2026-10-10): drawn by hand in the presenter (`GamePresenter.Data.cs`) while a `DataScreenEvent` other than `Closed` is up, and the file's drawing commands and text stay off the stage as a picker's — the screen cleared, the map's tiles in miniature from the stage's tile area at (0, 67) 640×266 with the markers over 10000 (less 10000, keyed, at a third of their place), the menu in the selection window last chosen, and the four graphs from the history records 0-15 and the scales GameState holds: the frame, the title with the scale, the caption, 16 bars with the file's clamps (the funds' stack and its odd shrink), their numbers and "OV". Tested headless (`GamePresenterTests`: every graph, the scale menu, the graph again at a new scale); `--auto` opens All Data once (each graph, the scale menu once) | commands | W3, E2 |
| **N1** ✓ | Nation template + Rebels, Patton, Rudra + all `T*2` defender pickers. Part 1 (milestone 2 step 2, 2026-10-10): the template `Strategy/NationFile` on the `CommandFile` base — the priority list in `Tmp0001-0009`, the picker (stops once slot 4 is taken from the fourth entry on), the filler with the file's filter (`SkipsCommander`), the troop sum into `CallArg`, the target walk that attacks the first province the player holds through `FIGT_B`; the captures of non-player provinces differ in their text from file to file and come with their files (N2, N3) — and on it `TCﾘｰｻﾞｽ` whole (`Strategy/RebelTurn`: `Gｴｸｽ` 10 on the first turn, armies A → B, or C alone once the player holds Ys (5)) and `TCﾘｰｻﾞｽ2` whole (`Strategy/RebelDefenders`: the defenders by province, the walls of Oaks only), the army name `MS 10` read from the player's files; routed in `StrategyRules`. Quirk NAT-17 (only Oaks has walls); NAT-06 refined (B's targets end with Leazas Castle, so B falls into C only once the castle is lost, which ends the game); BFL-13 and NAT-12 marked in the port. Checked against each file run by `SceneRunner` on 3000 random states (draws, calls with the party, events, memory; every army and target, B into C, the stale retreat mode, the jump of a lost castle — `NationTurnDifferentialTests`), unit tests (`NationTurnTests`), and the owner's capture of the first Leazas rebel turn (week 14: `Gｴｸｽ` 10 and two `FIGT_B`) replays with 0 differences (`NationReplayTests`, content tests: the port reads `MS 10`). Patton (`Strategy/PattonTurn`) and the other `T*2` files came with N2 / N3. Part 2 (milestone 5 step 1, 2026-10-11): `Tﾙﾄﾞﾗ` whole in `Strategy/CreatorTurn` (on `NationFile`, using none of the template) — `Gﾙﾄﾞﾗ` 1 in the first phase, the counter `N_Rudrasaum_Turns`, seven searches of up to 19 draws for a province of the player's (Nothing (75) gives up at once, NAT-29; below 5 Leazas Castle (1), Ancient Ruins (35) and Siberia (38) are spared, from 6 on a draw in two puts Leazas Castle in place, 5 has neither, NAT-30), five attacks by Angel Knight (146) and Angel Knight (147) at full HP and troops, status 0 (NAT-11, settled: nothing reads it), retreat mode 1, through `FIGT_B`, two earthquakes (economy and walls 0, battle area doubled, picture 1043); the jitter and the window helpers' dead ops left out (NAT-16, EVT-01); `Tﾙﾄﾞﾗ2` (empty, called by nothing) is not routed. Checked against the file run by `SceneRunner` on 3000 random states (`NationTurnDifferentialTests`: the scene, the give-ups, the spared provinces, the castle roll, the battle area's ceiling, the counter's), unit tests (`CreatorTurnTests`), and `StageThreeEndTests` goes on from Kayblis's death into the first phase with `Gﾙﾄﾞﾗ` run from the player's files. N1 is done | nations | F0 |
| **N2 / N3** | Helman, Zeth, JAPAN / Kayblis. N2, part 1 (milestone 3 step 1, 2026-10-10): `Tﾍﾙﾏﾝ2` whole in `Strategy/HelmanDefenders` on `NationFile` — `Gｽﾃｯｾﾙ` 1 on the first attack (the war starts), Mineva met at Micrograd (44), Laurengrad (48) or Sudori 10 (42); after Helman's fall its people ("A Certain Country's People": `Gｼｰﾗ` 2 may call the attack off, `Gｼｰﾗ` 1) and the militia (Civilian Generals 58, 59 with `100 + RND(800)` troops); before, the walls and the army by province — the 1st (the stronger of its two orders, on a tie the second; Laumiere (27) alone at Volgo Z (30); retreat mode 1 at Rosesgrad (39) and at the passes against one unit), the 2nd, the 3rd (the militia called up anyway, joining it at Lang Bau (45), where nobody is filtered out), the 4th (`Gﾈﾛ` 1), the 5th (`F_Rolex_State` raised to 4), Lelyukov's (22) last stand at Archgrad (36), Ruberan (31) at the Ancient Ruins (35) (`Gﾙｰﾍﾞﾗﾝ` 1), the militia elsewhere or when the leader is dead; the dead Patton branch kept (NAT-16). Routed in `StrategyRules`, replayed by `NationReplayTests`. Checked against the file run by `SceneRunner` on 3000 random states (every army, scene, cancel and filter — `NationTurnDifferentialTests`) and unit tests (`HelmanTests`). N2, part 2 (milestone 3 step 1, 2026-10-10): `Tﾍﾙﾏﾝ` whole in `Strategy/HelmanTurn` on `NationFile` — the week's scenes (Stessel and Sheila `Gｽﾃｯｾﾙ` 3-6 by the weeks since the war began, Tainyar (commander 26) back while Log B (31) and Sudori 13 (41) are not the player's, Freya, Lelyukov's retreat and fall-back, Aristoles, Patton's scenes 2 and 12 after the draw `:60`, NAT-08), then every army whose leader lives, in order: the 1st (the stronger of its two orders, no threshold NAT-13; at Maunes (13) the roll between `Gﾚﾘｭｰｺﾌ` 5 and Tainyar alone; Maunes (13) and North (2) taken from the monsters by `L_00fc2`, the first capture ported, with its three key waits), the 2nd (Aristoles's wait, 1500), the 3rd (`Gﾐﾈﾊﾞ` 1, 1500, re-picked at Micrograd (44) without Capricorn (184) and Ortega (185), NAT-19), the 4th (Nero's states; after he turns the smaller army that leaves the week unmarked, NAT-18), the 5th (Rolex's two rolls and scenes, 500), the Crusaders (Olhound (60), `500 + RND(500)`), and `Gｽﾃｯｾﾙ` 14 for a quiet week. NAT-09's claim on `Tﾍﾙﾏﾝ.ADV:84` corrected (parenthesised), STR-07 settled (never both phases in play). Routed in `StrategyRules`, replayed by `NationReplayTests`. Checked against the file run by `SceneRunner` on 4000 random states (every scene, every army at every target, the captures, the thresholds' paths, scribbling battles and scenes — `NationTurnDifferentialTests`) and unit tests (`HelmanTests`). N2, part 3 (milestone 3 step 1, 2026-10-10): `Tﾊﾟｯﾄﾝ` whole in `Strategy/PattonTurn` on `NationFile` — the army's story scenes by `F_PattonArmy_State` (6: `Gﾊﾟｯﾄﾝ` 11, 5: `Gｱﾘｽﾄﾚｽ` 3, 3/2/1: `Gﾊﾟｯﾄﾝ` 9/8/7, each tested after the one before), then one army, Patton (54), Hubert (55), Freak (56), Hanty (57) and Aristoles (32) once his state is past 9 (`Gﾊﾟｯﾄﾝ` 6 sets 10), nobody left out, at least 500 troops, retreat mode 4, against Sudori 17 (46), Sudori 13 (41), Pawn (40), Micrograd (44), Lang Bau (45); no draws. Routed in `StrategyRules`, replayed by `NationReplayTests`. Checked against the file run by `SceneRunner` on 3000 random states (every scene, target, the weak army and the empty walk — `NationTurnDifferentialTests`) and unit tests (`HelmanTests`). With it Helman's three files are ported (milestone 3 step 1). N2, part 4 (milestone 3 step 2, 2026-10-10): `Tｾﾞｽ2` whole in `Strategy/ZethDefenders` on `NationFile` — Parapara Fortress (16) without walls; the garrison Wall (94), Wall (95), Mage Leader (96) called up at full strength, less `RND(troops / 2)` each when the same province is attacked again (`Bt_GarrisonProv`, `Tmp0226`); Papaya's trap set by two units or more; the first attack starts the war (`Iｾﾞｽ` 1); a general's city — Saias (113) at Adam's Fortress (58), Uspira (117) at Tape (61), Kabachahn (121) at Amber Castle (65), Alex (125) at Old Zeth (63) — sends his army, or him alone with the two Walls at 10 troops; a minister's tower sends the army back while the barrier stands and nobody holds the Barrier Blade (item 12; empty slots read unit record 0, ENG-06), else her scene and army (`Gﾏｼﾞｯｸ` 1 may send Alex's); else the garrison (the Mage Leader alone at Magic's Tower (66)); retreat mode 1. Routed in `StrategyRules`, replayed by `NationReplayTests`. Checked against the file run by `SceneRunner` on 3000 random states (every general, minister, cancel, the Blade, the repeated attack, the trap — `NationTurnDifferentialTests`) and unit tests (`ZethTests`). N2, part 5 (milestone 3 step 2, 2026-10-10): `Tｾﾞｽ` whole in `Strategy/ZethTurn` on `NationFile` — the war's scenes first (Ragarl's ritual `Gﾅｷﾞ` 1-3 / `Gﾊﾟﾊﾟｲｱ` 1, `Iｾﾞｽ` 3-6 by the state; at state 2 the state moves on to 4 and Saias's (113) army marches alone; `Iｾﾞｽ` 2 at state 1 sets state 2 itself, so that week no minister marches and the generals' scene roll is voided — `:97-99` is live, NAT-16 corrected; Magic's `Gﾏｼﾞｯｸ` 3), then the four ministers' armies on a roll in two each (Chizuko's (101) Zamoor (165) through Annis (168), Papaya's (104) Backslock (169) through Am (172), Nagi's (107) Lawson (173) through Hiroko (176), Magic's (110) Kenichi (177) through Rue (180); her scene before the battle, the province set again after it), the generals' scene roll (`Iｾﾞｽ` 30-36, each with one general's army if the generals it names live, all four on 7; roll 6 needs Alex (125) alive too, NAT-07), up to two single rolls — each general reviving one of his three officers at an eighth of his maximum and marching, with any troops at all, on the first of fifteen provinces from Chizuko's Tower (68) to Leazas Castle (1) the player holds (none: the march falls into Chizuko's army, NAT-20, dead in play) — and the ministers' closing scenes (`Gﾊﾟﾊﾟｲｱ` 8, `Gﾁｽﾞｺ` 4/5, `Gﾅｷﾞ` 21, `Gﾏｼﾞｯｸ` 21/22). Routed in `StrategyRules`, replayed by `NationReplayTests`. Checked against the file run by `SceneRunner` on 4000 random states (every scene, every army at every target, the voided roll, the fall-through, the revivals, a march without troops — `NationTurnDifferentialTests`) and unit tests (`ZethTests`). With it Zeth's two files are ported (milestone 3 step 2). N3, part 1 (milestone 3 step 3, 2026-10-10): `Tｹｲﾌﾞﾘｽ2` whole in `Strategy/KayblisDefenders` on `NationFile` — the Monster Generals (86-89) called up at full HP for every attack with `MaxTroops / (4 + 2 hits)` troops and a draw of up to a quarter more (`1 / (2 + hits)` when the same province is attacked again); before stage 3 the gate (Tower of Evil (78), Grief Valley (89), Bone Forest (90); Tearing Forest (91) at stage 2 only, NAT-09) moves the story to stage 3 with its walls, any other province is fought in the plains by two Monster Generals or four on a roll in two; at stage 3 (the gate attack included, `Iｼﾚｲﾌﾞ` 6 sets the stage first) the named garrisons — Kaybwan's (159) at the Blue House (86), Kaybnyan's (162) at the Red House (85), Warg's scene instead of a battle at the Tower of Evil (78) until she is met (`Gﾜｰｸﾞ` 1), Kesselring (133) at his castle (87), Camilla (91) at hers (88, her maximum raised from 200 to 2000, NAT-10), Kayblis (135) at the Demon Lord's Castle (79, `Gｹｲﾌﾞﾘｽ` 4 unless Galtia has joined him), Medusa (134) at Kayblis's Castle (84), else the four Monster Generals — walls only at the last two castles (NAT-21, new); retreat mode 1. Routed in `StrategyRules`, replayed by `NationReplayTests`. Checked against the file run by `SceneRunner` on 3000 random states (every garrison, the gate at each province and in other stages, the plains, the repeated attack, a Monster General too small to draw, Camilla's patch — `NationTurnDifferentialTests`) and unit tests (`KayblisTests`). The gate is ported (milestone 3 step 3). N2, JAPAN part 1 (milestone 3 step 5, 2026-10-11): `TJAPAN2` whole in `Strategy/JapanDefenders` on `NationFile` — `F_Japan_State := 1` on every defence (the war, also over the alliance states 2 and 3), the walls kept (NAT-16); Nagasaki (55): Katsuie (75), Harumi Ueda (76), Mori Harada (77), Denden Mori (78), retreat mode 1 from 10 troops; Osaka (56): the whole army from Nobunaga Oda (72), retreat mode 1; elsewhere the strongest of the whole army, the 75-78 list and the 79-82 list (One-Shot Tarou (79) to Isoroku Yamamoto (82); the earlier wins a tie), retreat mode 4 from 500 troops; below either threshold the retreat mode stays the last battle's (BFL-13). Routed in `StrategyRules`, replayed by `NationReplayTests`. Checked against the file run by `SceneRunner` on 3000 random states (every province kind, list, threshold and the alliance states — `NationTurnDifferentialTests`) and unit tests (`JapanTests`); no JAPAN capture recorded yet. N2, JAPAN part 2 (2026-10-11): `TJAPAN`'s rules in `Strategy/JapanTurn` — retreat mode 3, Kouhime's scenes on two draws (`Gｺｳﾋﾒ` 4 / 3 by `F_Kou_State`), a quiet week in four (then, if nothing happened, `Gｺｳﾋﾒ` 1/2 or `IJAPAN` 7/8 on a draw in four), else the army on a draw in five (the whole army, Katsuie's four or One-Shot Tarou's four; under 190 troops left out once someone leads), with 500 troops the walk from Nagasaki (55) to Leazas Castle (1) — the first held by the player attacked through `FIGT_B`, Toushin City (27), Hanna (26), Zeo (24), Red (23) taken from the independent cities without a battle but M Land (25) passed by (NAT-22), and a walk that finds nothing falling into that capture at Leazas Castle (NAT-23). Checked against the file run by `SceneRunner` on 3000 random states (every scene call, army, threshold, attack, capture and the run-out walk — `NationTurnDifferentialTests`) and unit tests (`JapanTests`). N2, JAPAN part 3 (2026-10-11): Gekkou's attempts in `Strategy/JapanTurn.Gekkou.cs` (`TJAPAN.ADV:295-2744`; while `N_Gekkou_Attempts` is 1-4, the week's event then marked done) — a draw in two, in three when Rance (unit 1) was deployed this week; 2 is Shinobu, else Gekkou slips into Leazas Castle: on a draw in two an attempt on Barres (unit 6), Rick (7), Cordoba (8), Ex (14), Eleanor (23) or Gengorou (15) by a draw in six (a dead one: nobody found), killed only if deployed this week (`Unit_Status := 0`, NAT-14), else a draw in three steals half the gold, a quarter, or nothing. Shinobu's attempt alone counts (`N_Gekkou_Attempts + 1`): with Rance deployed this week her seduction works — the scene with CG 806 / 133 between fades and the game's end, ending 4 (`&#OVER` as `GameEnd`) —, else he runs off; the attempt that makes 5 plays her capture (Leila (unit 5) or, dead, Maris speaks; a dead Leila still gives her order, NAT-24, new) and the raids end. Every scene's text and window is the file's. Checked against the file run by `SceneRunner` on 3000 random states (now with the attempts at 1-4 in three cases of four, Rance, Leila and the six targets dead, idle or deployed; every theft, kill, failure, empty pick, Shinobu's three ends and the turn going on after an attempt — `NationTurnDifferentialTests`) and unit tests (`JapanTests`); the dictionary's `N_Gekkou_Attempts` corrected (only Shinobu's attempts count). With it JAPAN's two files are ported (milestone 3 step 5); no JAPAN capture recorded yet. N3, part 2 (milestone 4 step 1, 2026-10-11): `Tｹｲﾌﾞﾘｽ`'s frame in `Strategy/KayblisTurn` on `NationFile` — in the first phase of stage 3 (`F_DemonWar_Stage < 20`) the war council `Gｹｲﾌﾞﾘｽ` 1 and straight to the dispatch (the reset skipped, NAT-04, dead in play); in every later one the assault `Gｹｲﾌﾞﾘｽ` 2 when pending, the generals' attack marks reset, 24 recoveries of 22 commanders (Babolat (129) three times, NAT-05: status 1, 3 HP, three times the recovery in troops, each up to its maximum; the dead skipped), and the event chain whose first scene ends it — Medusa's first scene, her counter on a roll in five and her scenes 2-4 while it equals 2/4/6 (NAT-25), Camilla (91) on a roll in three (`Gｶﾐｰﾗ` 3 below 10, 4/5 at 10/11 on a 1), a marked week ending the chain (dead, NAT-16), Silky's scenes outside the war (1, the souls 2-6, 8 without the jump, NAT-26, 9), Warg's 3/4, Nagi's 9 while Kesselring (133) lives, Kaybwan's and Kaybnyan's 3, Satella's 2, Medusa's 5 on a roll in five. The dispatch: the Grief Valley (89) guards while the player holds it and Kayblis has not moved (Lis Guard (186) at full HP with `20 + RND(100)` troops, Kayblis's order with his portrait 415 / 466, `FIGT_B`, then the dispatch again, NAT-02); the generals' introductions (`Gﾊﾞﾎﾞﾗ` 1 also on a marked week, `Gﾚｯﾄﾞｱｲ`/`Gｶｲﾄ`/`Gｶﾞﾙﾃｨｱ` 1 only on an unmarked one, `Gｹｯｾﾙ` 1 once the player holds Bone Forest (90) or the Red House (85)) and their units; `Gﾊﾟｲｱｰﾙ` 1 once three of Babolat (129), Redeye (130), Galtia (131), Kite (132) are dead, Piar's unit while her state is 1-99; Kayblis's army at war; the army name; `Gｹｲﾌﾞﾘｽ` 3 once the player holds Silky's Castle (81); the Assault Leaders past week 100; Monster General (86)'s name back (`MS 4` from the player's files). The capture `L_026bf` (owner 8, the report, the flag; Lang Bau (45) brings Helman down, a Zeth tower kills its minister and moves Zeth's story on, the four dead bring Zeth down, a general's city kills its general; the collapses' loops stop after Shangri-La (29) / Adam's Fortress (58) and Zeth's then kills Saias (113), NAT-01; wars never started fall too, NAT-27). The armies threw `NotSupportedException` until their steps (the generals' units: part 3; Piar's unit and Kayblis's army: milestone 4 step 3; the Assault Leaders: step 5). Routed in `StrategyRules`, a `NationReplayTests` root. Checked against the file run by `SceneRunner` on 3000 random states (`NationTurnDifferentialTests`: week S, the assault, every scene of the chain, the marked week, the guards — again, with either portrait —, every introduction, `Gｹｲﾌﾞﾘｽ` 3, a lost Leazas Castle, the file's end; runs that reach an army compared up to the port's stop) and the capture against `L_026bf` run alone on 3000 (every collapse, minister, general, Shangri-La and Adam's Fortress), and unit tests (`KayblisTurnTests`); the dictionary's VAR0637 (`F_Galtia_DefectedToKayblis`), VAR0744 (`F_DemonCastle_Attacked`, was `F_Galtia_JoinedKayblis`) and `F_Kayblis_Moved` corrected. N3, part 3 (milestone 4 step 2, 2026-10-11): the generals' units (`Tｹｲﾌﾞﾘｽ.ADV:729-1328`) in `Strategy/KayblisTurn.Generals.cs` — Babolat (129) alone; Truffle 77 (148), Moreinyan (149), Ouzawa (150), Redeye (130); Kite (132), Tobroiv (151), Deckmaster (152), Strength (153); Derandron (154), Nasport (155), Galtia (131); Kesselring (133) alone — each on its line (survey-nations §2.4) to the first province not Kayblis's: the player's attacked through `FIGT_B` (retreat mode 2; Kesselring 1, after his flight from the province before it on his line — his castle (87) before the first —, "［<province>］ Sky Above", his portrait 416), any other taken without a battle; a battle held to the time limit fought again once (NAT-03; not Kesselring's). Babolat: the pitfall (`Babolat_PitfallProv`, only on a player's province) is `Gﾊﾞﾎﾞﾗ` 4 instead of the battle, `Gﾊﾞﾎﾞﾗ` 2 after one at state 1. Redeye: alone at Maunes (13); `Gﾚｯﾄﾞｱｲ` 2 before the battle at state 2; at state 8 `Gﾌﾘｰｸ` 4 instead of it at Freak's stage 3, else at Maunes `Gﾚｯﾄﾞｱｲ` 8 (with no ninja free the battle hangs on a stale cursor, NAT-28, new); after it a draw in two, `Gﾚｯﾄﾞｱｲ` 6 (Freak's stage 5, the player lost, the draw not 1), `Gﾚｯﾄﾞｱｲ` 5 at 7, 6 to 7, `Gﾚｯﾄﾞｱｲ` 4 at 5, 3 to 4, 1 to 2. Kite: a draw in three (in ten below Calory's state 4) — on 1 / 2 `Gｶｲﾄ` 6 / 7 instead of the march while Zeth Palace (69) is Kayblis's with its own picture (1011) —, after his battle `Gｶｲﾄ` 2 / 3 / 4 at Calory's 1 / 2 / 3 and 5 at 4 once Zeth Palace (69), the province fought over, is Kayblis's. Galtia: `Gｶﾞﾙﾃｨｱ` 2 after a lost defence. Kesselring: no empty-unit test; after his battle `Kesselring_LastProv`, and `Gｹｯｾﾙ` 2 at his state 3 while the player holds Bone Forest (90) or the Red House (85), dead or alive. The troop sums are never read and the generals' empty-unit tests are dead (NAT-16). Checked against the file run by `SceneRunner` on 3000 random states (`NationTurnDifferentialTests`: every general's battle at every province of his line, the captures on the lines but at Grief Valley (89), the second attacks, every general killed, the pitfall, Freak's scene, the battle after `Gﾚｯﾄﾞｱｲ` 8 or not, Redeye's steps, Kite's draws and Calory's scenes 2-7, Galtia's scene, Kesselring's flight and capture, `Gｹｯｾﾙ` 2 at both provinces) and unit tests (`KayblisTurnTests`). N3, part 4 (milestone 4 step 3, 2026-10-11): Piar's unit (`Tｹｲﾌﾞﾘｽ.ADV:670-728`) in `Strategy/KayblisTurn.Armies.cs` — `RND(77)` until the province is the player's and not Nothing (75), each miss giving up on a draw in ten (NAT-15); `Gﾊﾟｲｱｰﾙ` 2 (PG-X mk2 (138) revived when PG-7 (136) and PG-9 (137) are dead), the three through `FIGT_B` with retreat mode 2; after it Miki (unit 32) among the defenders → `Gﾊﾟｲｱｰﾙ` 6 (the assault), else `Gﾊﾟｲｱｰﾙ` 5 at 5, 4 to 5, `Gﾊﾟｲｱｰﾙ` 4 at 3, 2 to 3, `Gﾊﾟｲｱｰﾙ` 3 at 1; her empty-unit test and capture dead (NAT-16). Checked against the file run by `SceneRunner` on 3000 random states (`NationTurnDifferentialTests`: the redraws, giving up, the attack, Miki, every step and scene, and through the scenes' scribble the dead capture and empty unit) and unit tests (`KayblisTurnTests`). N3, part 5 (milestone 4 step 3, 2026-10-11): Kayblis's main army (`Tｹｲﾌﾞﾘｽ.ADV:1329-1508`, while `F_Kayblis_War = 2`) in `Strategy/KayblisTurn.Armies.cs` — up to six marches on Grief Valley (89), Great Wall of Helman B (50), Laurengrad (48) … Maunes (13), North (2), Leazas Castle (1), the first province not Kayblis's attacked through `FIGT_B` (retreat mode 2) when the player's, else taken: Kayblis (135) alone; Silky (156), Galtia (131, with `F_Galtia_DefectedToKayblis ≠ 0`), Satella (157), Meglass (158), each alive, with Monster Generals (86, 88, 89); Monster Generals (87, 86, 88, 89) — each of these after Monster Generals (86-89) are refilled to full HP and `MaxTroops + RND(MaxTroops / 2)` (NAT-10); no empty-unit test (Kayblis lives while the phase runs). Checked against the file run by `SceneRunner` on 3000 random states (`NationTurnDifferentialTests`: every leader, every province of the line, a dead Kayblis's empty unit, the refills) and unit tests (`KayblisTurnTests`). N3, part 6 (milestone 4 step 5, 2026-10-11): the Assault Leaders (`Tｹｲﾌﾞﾘｽ.ADV:326-624`) in `Strategy/KayblisTurn.Assault.cs` — past week 100 the 1st (191), past 110 the 2nd (192), and so on to the 8th (198) past 160, on the north list (`:364-490`: Sulfur Forest (80), Silky's Castle (81), Grief Valley (89) … Maunes (13) … Alan (11), South (4), Leazas Castle (1), 31 provinces) and the south list (`:530-617`: Sulfur Forest (80), Silky's Castle (81), Land of Death (82) … Maunes (13), North (2), Leazas Castle (1), 22) in turn, retreat mode 1, each with Monster Generals (87, 88) called up alive at full HP with `MaxTroops / 5 + RND(MaxTroops / 3)` troops (two draws a leader, whether he marches or not; under 99 troops and of a class under 5 they stay out); a dead leader revived at 1 HP and home that week; a living one +3 HP and his recovery in troops (once, each up to its maximum), home under 1000 troops; else the first province of his list not Kayblis's taken or attacked through `FIGT_B`, the leader alone at Maunes (13) on the north list only; the north list all Kayblis's keeps him home, the south list runs on into Leazas Castle (1) and takes it again (NAT-16, dead in play). No troop sum, no empty-unit test. The sweep's claims checked and confirmed. Checked against the file run by `SceneRunner` on 4000 random states (`NationTurnDifferentialTests`: every leader, every province of both lists attacked and taken, Maunes alone, the fall-through, revived, too weak, held under 1000 by his maximum, the north list all Kayblis's, a Monster General left out or called up without a draw; the test's log now gives every map call and message the province, its owner and the head of the priority list, so a capture is told by its army) and unit tests (`KayblisTurnTests`). With it `Tｹｲﾌﾞﾘｽ` is ported whole (milestone 4 step 5) | nations | N1 |
| **D1** ✓ / **D2** ✓ | Dungeon flow / dungeon scenes with state (milestone 2 step 8, 2026-10-10): `Cﾒｲｷｭｳ.ADV` ported by hand in `Strategy/DungeonCommand` (the flow) and `Strategy/DungeonCommand.Scenes.cs` (D2) — the entry (the Gunagan cursor DGN-01, Merim's reset, the camera, the week's action spent DGN-14, the name and background from the player's files, the Cafe trip DGN-13), the dispatch `L_005b6` (the hideout, the story-only dungeons, the pre-scenes), the menu `L_0076a` (Start Search, Bring/Leave Merim, Bring Pink Cow, Return giving the week back), the searcher `L_008d1` through `Sﾌﾞﾀｲ` mode 3 with the five refusals (BFL-01; back to the dispatch, DGN-06), the way there `L_00a2c` (the blizzards DGN-09, Ho Raga, the Battleship's key DGN-10, Titi Lake, the parameters `:477-612`, the Holy Gal's scene, the resume floor), the floor loop (the fixed floors `:700-857` in order, the menu saving the floor, the moves with their encounter rolls STR-14, the bosses forcing a battle, the monsters' troops DGN-05 and Titi Lake's Hanny tiers, the battle — `FIGT` with `Bt_Kind` 3, then `死亡捕獲` —, the fallen searcher DGN-08, the inscription roll after a battle DGN-02/03, the retreat with its empty second menu DGN-07, the end with its scenes DGN-11), the chests (DGN-18), Orochi, the Uncurse Spring, KD's summit and quiz (DGN-12, DGN-17), Merim's inscriptions, joining and leaving, the snow advice, the Pink Cow; the boss and Hanny setters (`:2872-3470`, DGN-04) are record data run from the player's files (`IScenarioSource.ReadBlock`: a label body up to its `\0:`). God's Door (`L_04ddc` at `:234`, the menu item `:290`, then `Iｶﾐ`) threw `NotSupportedException` until milestone 3 step 6 (2026-10-11): now in `Strategy/DungeonCommand.GodsDoor.cs` — found (`F_GodsDoor_Found := 1`) on a visit to the Ancient Ruins (dungeon 30) once KD's quest is past 1 by Rance (unit 1) holding item 1 or 2 (a scene for each), then the door in the dungeon's menu: its picture (`WAO_SUB` 333 with the statues placed so far, open once the quest is at 6), Kouhime's hint while her state is past 9 and the quest at 3 (the quest to 4; a line for each of the monkey, the sunflower and the bonsai nobody holds), and its menu — each of the statues (items 15-18) someone holds placed (nobody holds it any more, its flag set; the fourth opens the door, `F_GodsDoor_Quest := 6` between window fades), Rance's talk while the door is shut (a scene with CG 806 / 212), Enter once it is open (into `Iｶﾐ`, which ends only in `&#OVER`; were it to return, the file would fall into Merim's inscriptions and return from no label call, so the port throws), Leave (back to the dungeon's menu). Checked against the file run by `SceneRunner` on 8000 random states (now with God's Door's states: found or not, the quest at 1-6, Rance's item, the statues placed, held or not, Kouhime's state; `Iｶﾐ` a hook ending in the jump; every scene, menu item and hint line reached, 0 differences, none unported) and unit tests (`CommandSliceTests`: found by either item, the last statue and Enter, Kouhime's hint). Quirks DGN-16 to DGN-18 added, DGN-02 to DGN-15 marked in the port. Checked against the file run by `SceneRunner` on 8000 random states (`CommandSliceDifferentialTests`: every dungeon's shape, saved floors next to the triggers and bosses, scribbling battles and pickers, a menu policy that goes deeper half the time; every feature reached, 0 differences; where the original runs on into God's Door, up to the port's stop), unit tests (`CommandSliceTests`: a search with a battle, the resume floor, the blizzards, the empty menu, Marder with a setter double, the DO Pot, a refusal, an empty picker), the headless presenter (`GamePresenterTests`: a search of the Monsters' Cave with a battle; the empty menu is drawn as an empty box, as xsystem35's `init_selwindow` does, and only a right click leaves it in Godot too), and the 12 `dungeon` captures still replay whole (all of them clicks: no search is captured yet). The search has no screen of its own (message windows, menus, `WAO_SUB` pictures, the battle); Gunagan's painting (`G 806`/`G 862`) is shown under the full-screen window, which `MaskEventCgs` masks. `--auto` searches the Monsters' Cave once at stage 1 | dungeon | P2, E2 |
| **G0** | Godot: strategic map screen, command menus, scene player, save/load (`ﾛｰﾄﾞｾｰﾌﾞ` ported whole, decision 6); one input implementing `IGameInput` for the battle and the strategic screens (decision 7). Done 2026-10-09 in four commits (decision 8). Part 1 (2026-10-09): `ﾛｰﾄﾞｾｰﾌﾞ` ported whole by hand in `Strategy/LoadSaveScreen` (modes 0/1/2/3/10/11 and the error box, the slot lists, the page-4 sync, the autosave notice, and `ResumeAsync`, the way back after a load: `:34` with `RetVal 0`, "Load Complete"), routed in `StrategyRules`; the engine's save commands `QD`/`LD`/`LP`/`QP`/`QC`/`LT` in `Script/ScriptContext.Save.cs` on an `ISaveStore` (`Script/SaveData.cs`: the slot = page 0, pages 1, 2, 4, 10 and the strings plus the autosave's call as the resume point; `MemorySaveStore`, `DirectorySaveStore`, the port's own versioned format, decision 11), shared with `SceneRunner`, which now runs them; `LD` leaves the running code by `GameLoadedException`. Quirks STR-22, STR-23. Checked against the file run by `SceneRunner` on 800 random states and slot sets, every mode and the way back (`LoadSaveDifferentialTests`: events, menus with their captured texts, slot reads/writes/times, memory), unit tests (`SaveDataTests`, `LoadSaveScreenTests`), and the prologue weeks through the router now autosave into slot 1 (`PrologueCommandTests`). Part 2 (2026-10-09): the game driver `Strategy/GameSession` — one boot on a fresh state, then the new game or a load from the title (`ﾛｰﾄﾞｾｰﾌﾞ` 0; backing out returns null), the week loop; a `GameLoadedException` from anywhere (the title, `COMMAND`'s Load/Save) goes on with the save screen's "Load Complete" and `WeekLoop.ResumeAsync` (the week from `MAIN.ADV:21`; the Leazas autosave `:83` not ported); `&OVER` ends the game with its `EndingId` (`GameEnd`), `&MAIN` is followed; the view/input proxies `ViewProxy` and `GameInputProxy` (decision 4). Tested on the player's files: a game saved at week 9 loaded from the title in a new session, a load from the command menu, backing out of the load list (`PrologueCommandTests`), and `WeekLoopTests`. Part 3 (2026-10-09): semantic screen events in the core (`Script/ScreenEvents.cs`: `MapScreenEvent` from `COMMAND`'s map wait, `PickerScreenEvent` from each picker, `SaveScreenEvent` from `ﾛｰﾄﾞｾｰﾌﾞ`, `BattleScreenEvent` from the router around the battle files; skipped by the differential comparisons) and `PointerScript.Move` (the pickers' highlight follows the pointer); the presenter `Kichikuou.Content/Strategy/GamePresenter` on two layers (decision 12): the stage — the engine surface where the scenes' own drawing commands run, the map is drawn from GameState and the battle draws through `BattlePresenter` on the same surface — and the screens drawn by hand over it, composed into a 640×400 `Screen`: the `WAO_SUB` window (frame and text from `Ui_*` and the events, `TextBox`), the portraits and centre picture, the menus (`SelectionBox` at the `B1`/`B2` window), the pickers (`PickerView`: grid from GameState, highlight from `Tmp0033`/`Tmp0029`, the panels the script prints into, bar labels and harem names from the player's files), fades. Tested headless on the player's files (`GamePresenterTests`: week 9 with Sill's CG scene, Pillage with the picker and the battle, a talk, a harem visit, the hideout, a save into slot 2, week 10; the opening; no unresolved text, picker clicks through `PickerView.HitTest`). Part 4 (2026-10-09): the Godot screens — the title `godot/Main` (decision 10: New game, Load, Quit, the battle-capture viewer `Battles/CaptureList` as a debug entry) and `godot/Strategy/GameScreen`, one game per screen: a fresh `GameState` and `GameSession` (`XSystem35Random` on the tick count or `--seed`, slots in `DirectorySaveStore(user://saves)`, unported files logged and skipped), its view and input behind `ViewProxy`/`GameInputProxy`, drawn by the `GamePresenter` (fades animated over `rate * 16 * 1000 / 60` ms as `ags_fadeIn`/`ags_fadeOut`, never skipped by MsgSkip unless Option → MsgSkip Speeds Up Fades (ours, on by default) — first written as 16 ms ticks, fixed 2026-10-10) and played with the mouse — key waits, menus (`MenuHitTest`, right click cancels), the map's clicks, drags (in steps while held) and right clicks and the pickers' clicks (through `HitTest`) as `PointerScript` gestures, one pointer read per frame where the scripts poll, the battle's input screens as in the capture viewer; the clock is the real date and a stopwatch (`ZT1`-`ZT5` as `cmdz.c`), `SC` reported no CD (the music's position since 2026-10-10, below), `IC` returns the shape before. `GameEnd` shows "THE END — ending n", a `NotSupportedException` a message, both back to the title. The presenter gained `MaskEventCgs` (debug: the stage under the full-screen window is a grey placeholder with the CG's number, on for the agent's dumps and `--shots`, off for play) and closes a picker its file left open when the map is shown (`Cﾌﾞｶ`'s End returns without closing `Sﾌﾞｶ`; `COMMAND.ADV:358-359` shows the map). Smoke-tested headless: `--newgame --auto --fast --quit` plays the whole prologue unattended (one visit each to Harem, Subordinates and the hideout, Turn END every week, the duel, the opening) to the Leazas week's `NotSupportedException`, `--load` resumes the autosave; screens checked masked with `--shots`. Follow-up (2026-10-09): `GameScreen` reads input as xsystem35 does, a state (`PlayerInput`, `SYS35KEY_*` codes) polled once a frame by `IM`, `Y 3` (ends while anything is held, `Y 3, 10000/10001`), `IK 0` (press, then the release of 16; the key repeat) and the menus (`sel_select`: release wait, right click cancels at the press, left chooses at the release), so the map's click/drag/coasting and the pickers' hit tests and highlight are the script's own; `IC` sets the system cursor; `PointerScript` only drives `--auto`. The presenter hides a `WAO_SUB` picture that a drawing covers whole (the map scroll's full-screen copies, `Iｼﾚｲﾌﾞ.ADV:78-89`) until `WAO_SUB` draws or removes it again, as the engine's screen does. The game screen has xsystem35's menu bar (File: Save Screenshot / Restart / Exit, Screen, Option, MsgSkip — all text by default, only seen messages by an option; F1, F4, Alt+R), docs/window-menu.md. The stage runs `CM` (`ags_scaledCopyArea`: each rectangle clipped as `ags_check_param`, `SDL_SoftStretch`'s nearest-neighbour on script pixels, the mirror bits) and `WV` (`ags_setViewArea`: the screen is read from `Surface.View`) — the opening's zoom (`ｵｰﾌﾟﾆﾝｸﾞ.ADV:226-302`), its card thumbnails (`:1056-1068`) and its scroll (`:930-938`) were silently skipped before (2026-10-10). `SC` reports the music as xsystem35's `cdrom.mp3.c` does (the `SS` track and the wall-clock time since it started, 999 when nothing plays; it said "no CD" before, so the opening timed its pictures from 1 s after `SS` instead of the track's 25th frame and lagged the music by ~0.67 s), `SL` sets the next track's play count (the opening's music plays once), and closing the full-screen window (`WAO_SUB` 0, types 31-39) leaves it on the screen as the engine's no-op restore does (`Iﾄｳｿﾞｸ.ADV`'s last line stays while the opening waits for its music). An audit of every presentation command in the scenario against the presenters (2026-10-10) added `CB` (clipped first, `ags_check_param`) and `CL` (`SDL_RenderDrawLine`, the colour's first palette entry), `IZ` (the pointer glide, Option → Mouse Movement, docs/window-menu.md) and `PCMPLAY`'s modes 2 (`SP n, 0`, endless) and 3 (`SP 5, 1`; it was silent in the battle and on the map) with `SP 0` stopping; `EC 1` stays a no-op (no `ES` fills its table) and `ZC 2`-`6` need nothing (the menu colours `ｽﾀｰﾄ.ADV:864-867` are `SelectionBox`'s defaults; `ZC 5`/`6` only colour an overflowing or line-framed message window, docs/battle/screen-layout.md §0.3) | — | W3, E2 |

## Decisions (owner, 2026-10-08)

1. **Record data and scenes are read at run time from the player's own files** (the decompiled
   scenario today, like `ScenarioText`; reading the original archive directly may replace the
   decompile step later). Nothing derived from the game is committed — no generated tables of
   stats, no converted scene IR. Hand-ported rule code stays committed as before.
2. **First milestone: the whole prologue is playable** — new game → the thieves weeks → the
   prologue battles (already captured and verified) → the end of the prologue, with save/load.
3. **Strategic screens first reproduce the original layout**, like the battle screen; a
   Sengoku Rance–style redesign comes later with the owner. The look need not be pixel-exact
   (mechanics must be).
4. **The UI is swappable** (owner's wish): the original-layout UI stays when our own is written,
   and the player picks one at the start of a game, ideally also during play. Consequences for
   the design: the core only raises `ScriptEvent`s and awaits `IScriptInput`-based calls, so a
   UI is one implementation of those interfaces; everything a screen needs must be
   reconstructable from `GameState` (the `ui` variables: window, portraits, camera, markers) so
   a UI taking over mid-game can redraw; a switch happens at a safe point (a key wait or the
   command loop), through a view/input proxy that forwards to the current UI. A redesigned UI
   may need meaning the low-level events lack (who speaks, which province) — derive it in the
   presentation layer from the event and the state, or add semantic events next to the
   low-level ones; never move rules into the UI.
5. **Quirks are reproduced and registered** (2026-10-08): every bug or oddity of the original
   is ported as is and recorded in [../quirks.md](../quirks.md), so some can be offered as
   fixes later.
6. **The autosave stays a hook until G0** (2026-10-08): the week loop calls `ﾛｰﾄﾞｾｰﾌﾞ` with
   `RetVal 3` exactly as `MAIN.ADV:20, 83` do; until then the game's hook does nothing (no save,
   none of the file's scratch writes), tests record the call and golden replays load the
   original's dump. `ﾛｰﾄﾞｾｰﾌﾞ` is ported whole with G0's save/load: the snapshot (pages 0, 1, 2,
   4, 10 and the strings), load = restore and resume right after the autosave, the manual slots.
   Done 2026-10-09 (G0 part 1, `Strategy/LoadSaveScreen`).
7. **The battle joins the router through a combined input** (2026-10-08): `StrategyRules` runs
   `FIGT_A`/`FIGT_B`/`FIGT`/`死亡捕獲` as a `Battle` on the bound context; each part keeps asking
   for the narrow input it uses (`IBattleInput`, `ISceneInput`), and only what drives a whole
   game implements `IGameInput : IBattleInput, ISceneInput` (the Godot input, the golden replay's,
   the test players). The router checks it once, when it is bound (the context holds its input
   as `IScriptInput`), so a game wired with a battle-only or scene-only input fails at once, not
   at its first battle or at the battle's `Sﾌﾞﾀｲ`. Interface segregation over making
   `IBattleInput` extend `ISceneInput`, which would force every battle test input to implement
   the pointer, the clock and the CD. Done the same day: `Strategy/IGameInput`, the routes, a
   router-vs-direct battle test (`StrategyRulesTests`) and a Pillage fought through the router on
   the boot's world (`PrologueCommandTests`).

8. **G0 is built in four commits** (2026-10-09): (1) `ﾛｰﾄﾞｾｰﾌﾞ` with the save store and the save
   commands in `SceneRunner`, differential-tested; (2) the game driver — boot, new game, load,
   resume after the autosave — and the view/input proxies; (3) the presenter of the strategic
   screens in `Kichikuou.Content`; (4) the Godot screens, title, sound and the smoke test.
9. **The strategic screens are drawn by hand** (2026-10-09), not by emulating the engine's
   drawing commands: each screen (map, menus, pickers, the scene window) is drawn from the meaning
   of the events and from `GameState`; of the `EngineEvent`s only what carries meaning is used (a
   CG shown by `G`, fades, `EC`, sound). The core raises semantic events where a screen needs to
   know what is open (filtered out of the differential comparisons), and a UI turns its clicks into
   the pointer gestures the script reads (`Script/PointerScript`) — or, in the original layout,
   hands the script the pointer itself (`GameScreen`, the G0 row). The look follows the original
   layout loosely (decision 3: not pixel-exact).
10. **Title and ending are minimal for now** (2026-10-09): our own title (New game / Load / Quit,
    plus the battle-capture viewer for debugging); `&OVER` shows a plain "THE END" screen with the
    ending number and returns to the title. `ｽﾀMENU` and `OVER` are ported later (W4).
11. **Saves use the port's own format, without the random generator** (2026-10-09): one versioned
    file per slot in the user's data folder, not compatible with the original's `.ASD`; the
    generator's state is not saved, as in the original (after a load the draws go on from the
    running sequence).
12. **The screens are drawn on a stage with hand-drawn layers over it** (2026-10-09; the owner asked
    which variant is cleaner and scales better, the agent recommended this one): the scenes' own
    drawing commands (`G` with `J0`/`J1`/`PC`, `CC`, `CS`, `CF`, `EC`) run on the engine surface as in
    the engine — the opening and the CG scenes need them, and no scene has to be interpreted by
    hand — while everything with a meaning (map, window, pictures, menus, pickers, save screen) is
    drawn by hand over it from the events and GameState; the battle draws on the same surface. A
    redesigned UI replaces the layers and may show or hide the stage.
13. **Milestone 2 is story stage 1, playable as a whole** (2026-10-10; of three scopes the agent
    proposed — the whole stage, only its main path, the whole stage without the dungeon — the owner
    chose the whole stage, in the proposed order): everything a player of stage 1 can do, from the
    first Leazas week to the win at Oaks (6) and the first week of stage 2. What stage 2 opens
    (the independent cities, the wars with Helman, Zeth and JAPAN, their `T*`/`T*2` files) is the
    next milestone. See "Milestone 2" below.
14. **Milestone 3 is story stage 2, playable as a whole** (2026-10-10; of four scopes the agent
    proposed — the whole stage, the endings `ｽﾀMENU`/`OVER` (W4) first, only stage 2's main path,
    or by part regardless of stage — the owner chose the whole stage): as with decision 13, a
    sweep of what stage 2 reaches comes first and decides the parts; the independent cities (P4)
    and the wars stage 2 opens are in, Kayblis (N3) and God's Door (D2, `Iｶﾐ`) only if stage 2
    reaches them. After the sweep ([stage-2-sweep.md](stage-2-sweep.md)) the owner chose
    (2026-10-10): the milestone **ends at the gate attack** — `StoryStage := 3` by `Iｼﾚｲﾌﾞ` 6 with
    its battle (`Tｹｲﾌﾞﾘｽ2` → `FIGT_A`) or by `Iﾏｼﾞﾝ` 51, before that week's Kayblis Empire phase
    (`MAIN:594`; `Tｹｲﾌﾞﾘｽ` is N3, a later milestone); **the main path first** (Helman, Zeth, the
    gate), then the independent cities, JAPAN, and **God's Door as the last step** (it has no
    stage test and is reached at stage 2). See "Milestone 3" below.
15. **Milestone 3 closes without `--auto` and without captures; porting goes on** (2026-10-11):
    `--auto` stays a smoke test of stages 0-1 (it does not play stage 2), the owner's stage-2
    captures (docs/golden-tests.md, "Capturing stage 2") are put off, and the owner asked to go on
    porting. The agent took the next stage as the next milestone, as decisions 13-14 did: milestone
    4 is story stage 3, a sweep first ([stage-3-sweep.md](stage-3-sweep.md)). See "Milestone 4" below.
16. **Milestone 4 closes on the same terms; milestone 5 is story stage 4** (2026-10-11): no stage-3
    capture and no `--auto` through stage 3, as decision 15 had it for stage 2. The next milestone is
    story stage 4 (the Creator phase `Tﾙﾄﾞﾗ` and what stage 4 reaches up to the game's end), a sweep
    first (stage-4-sweep.md). See "Milestone 5" below.
17. **`Iｶﾐ` gets its own survey before the end tests; W4 stays out of milestone 5** (2026-10-11,
    after step 1): a survey of `Iｶﾐ` (the rearguards, the picker, `FIGT表示` 21-24, the party
    restore, the endings) comes first, as its own step and commit, and step 2's end tests follow it;
    `OVER` and `ｽﾀMENU` (W4) are not part of milestone 5 — every ending stays `GameEnd` (sweep, open
    questions 3 and 5).

## Milestone 1 — the prologue

Order: **F0** → **F1** and **F2** (parallel) → **E1** → **E2** → **W1** → **W3** + the prologue
paths of the commands (`StoryStage = 0`: Pillage in `Cﾃｷﾄｼ`, `Cﾃｷﾄｼ2`, subordinate talk in
`Cﾌﾞｶ`, the stage-0 harem) → **W2** (the thieves-only week and the Leazas-phase skeleton) →
**G0** (map, command menus, scene player, save/load in the original layout). Each part is
verified by golden captures of prologue weeks (F2). The sweep below narrows it: at stage 0
W1 is only `COM.ADV:1-10`, W2 only `MAIN.ADV:1-82`, and Pillage needs `Sﾌﾞﾀｲ` (part of P2) and
the stage-0 hideout click in `Cﾒｲｷｭｳ`.

### What the prologue reaches (`StoryStage = 0`, swept 2026-10-08)

Lines refer to `_extract\scenario_named`. Not yet replayed by a capture.

**Path.** Title button 6 → `ｽﾀMENU L_00670` (`:279-291`): `WAO_MAP` 3, `M` 0, `Week := 8`,
`StoryStage := 0`, `Gold := 50`, `Iﾌﾟﾛﾛｰｸﾞ` (writes only `CurrentBgm := 1`, `Dgn_Cur := 1`
(Thieves' Hideout)), `&MAIN`. `SET` ran at boot: units Rance (1), Sill (2), Bound (3), Soul (4)
alive, Leazas owns Leazas Castle (1) through Parapara Fortress (16) and Nothing (75).
`MAIN.ADV:19-82` (gate `StoryStage = 0`) is the whole week: week + 1, history shift, autosave
(`:20`), units Rance (1) through Soul (4) with status ≠ 0 get status 1, full HP, troops + 2 up to
the cap (`:50-64`), `Hist_Funds := Gold`, `COMMAND`, then `StoryStage = 99` → 1 + `Iﾊﾝﾗﾝ`
(`:70-73`), loop. No income, expenses, nation phases or end-of-week checks — **no `T*` file
runs**. `COM.ADV:1-10` plays `Iｼﾚｲﾌﾞ` 10 in week 9 and `Gｿｳﾙ` 4 otherwise and returns: no upkeep, no triggers.
`COMMAND`: weekly flags (`:38-46`), map loop (`:47-161`), stage-0 menu (`:183-225`).

| Input | Handler | Effect |
|---|---|---|
| Vladivostok (33) / Golak (34) | `COMMAND.ADV:516-521` → `Cﾃｷﾄｼ` | stats as "??", menu Pillage / Return (`:62-84`); Pillage → `Iﾄｳｿﾞｸ` 1, then `ｵｰﾌﾟﾆﾝｸﾞ` if the stage changed (`:99-108`) |
| Any other province | `Cﾃｷﾄｼ2` | view only (`:42-64`) |
| Thieves' Hideout (dungeon 1) | `Cﾒｲｷｭｳ:168-170`, `L_03cbd` | hideout banter, `Y 4, 3` (`:2804`) |
| Monsters' Cave (dungeon 2) / Thieves' Cave (dungeon 3) / Isolated Shrine Island (dungeon 15) | `Cﾒｲｷｭｳ:7-28, 172-173` | camera pan, strings 8/10, `Ui_CmdDungeonAvail := 0`, return |
| Menu: Harem | `COMMAND:337-343` → `Cﾊｰﾚﾑ` (`Sﾊｰﾚﾑ` list) | once a week; Sill, Soul, Anasel if `F_Thieves_State > 1`; `G*` 10; `Harem_SexCount + 1`, Rance (unit 1) EXP + `Harem_ExpReward` (`:553-577`) |
| Menu: Subordinates | `COMMAND:354-360` → `Cﾌﾞｶ` (`Sﾌﾞｶ`) | once a week a talk: Rance (unit 1) monologue (`N_RanceSelfTalk + 1`), Sill (2) / Bound (3) / Soul (4) → `Gｼｨﾙ`/`Gﾊﾞｳﾝﾄﾞ`/`Gｿｳﾙ` 20; afterwards status view only |
| Menu: →Thieves' Hideout | `COMMAND:490-501` | camera pan |
| Menu: Load/Save | `COMMAND:370-376` | `ﾛｰﾄﾞｾｰﾌﾞ` with `RetVal 2` |
| Menu: Turn END | `COMMAND:377-425` | `Week > 22` → `Iﾄｳｿﾞｸ` 2, `ｵｰﾌﾟﾆﾝｸﾞ`, `StoryStage := 99` (`:419-424`) |

**Scenes.** `Gｿｳﾙ` 4, `Gｿｳﾙ`/`Gﾊﾞｳﾝﾄﾞ`/`Gｼｨﾙ` 20 write nothing outside `ui`/`scratch`/`system`;
`Iｼﾚｲﾌﾞ` 10 only leaves `Prov_Cur = 34` (Golak). `Gｼｨﾙ` 10 (`:2599`) and `Gｿｳﾙ` 10 (`:3063`) set
`VAR0398 := 1`; `Gｱﾅｾﾙ` 10 sets `F_Thieves_State 2 → 3` (first scene, leaves `Prov_Cur = 39`
(Rosesgrad)); the harem visits set `Harem_ExpReward` (Sill 5, Soul 3, Anasel 4) — confirmed by
running them (E2, `PlayerSceneTests`). `Iﾄｳｿﾞｸ` 1 (Pillage, `:110-953`): once a week
(`Ui_CmdAttackAvail`), first use `F_Thieves_State := 1`, Civilian General (commander 58) reset,
`Sﾌﾞﾀｲ` picks units (cancel returns), `FIGT_A` (`Bt_Kind 1`; the conquest at `FIGT_A:104-107`
is skipped at stage 0). After a win `Y 4, 4` banter (`:342`, drawn even when replaced):
`F_Thieves_State = 3` on Golak (34) → the Merci path (`L_013ff`); `F_Thieves_State = 1` on Golak
(34) with `VAR0398 = 1` → Anasel captured, `F_Thieves_State := 2`; otherwise `Gold + 1`. Deaths
are not fatal at stage 0 (`FIGTﾐｶDE`, `死亡捕獲:29, 36-38, 106-110`).

**End.** Both ways lead to the duel `Iﾄｳｿﾞｸ L_016f2` (units Rance (1), Bound (3), Soul (4), Sill
(2) vs Ruberan (commander 31), `Bt_TroopLimit 5000`, `Bt_IsDuel 1`, `:1384-1399`): (A) Turn END
in week 23, (B) the Merci path. Losing it is the story: `StoryStage := 99` at
`Iﾄｳｿﾞｸ.ADV:1841` (the first write off 0), `ｵｰﾌﾟﾆﾝｸﾞ` (`F_OpeningSeen := 1`), back to
`MAIN:70-72` → stage 1, `Iﾊﾝﾗﾝ` (stage 1 again,
`Week + 1`), next week is a Leazas week. Winning it is game over, ending 14 (`:2177-2178`).

**Draws outside the battle:** `Iﾄｳｿﾞｸ:342` (`Y 4, 4`), `Gｿｳﾙ:27` and `Gﾊﾞｳﾝﾄﾞ:9` (`Y 4, 2`),
`Cﾒｲｷｭｳ:2804` (`Y 4, 3`). None in the stage-0 parts of `MAIN`, `COMMAND`, `COM`, `Cﾃｷﾄｼ(2)`,
`Cﾌﾞｶ`, `Cﾊｰﾚﾑ`, `S*`, `Iｼﾚｲﾌﾞ` 10, `Iﾌﾟﾛﾛｰｸﾞ`.

**Files in milestone 1:** `MAIN`, `COMMAND`, `COM`, `Cﾃｷﾄｼ`, `Cﾃｷﾄｼ2`, `Cﾌﾞｶ`, `Sﾌﾞｶ`,
`Cﾊｰﾚﾑ`, `Sﾊｰﾚﾑ`, `Cﾒｲｷｭｳ` (top and `L_03cbd`), `Sﾌﾞﾀｲ`, `Iﾄｳｿﾞｸ`, `Iﾌﾟﾛﾛｰｸﾞ`, `Iｼﾚｲﾌﾞ` 10,
`Iﾊﾝﾗﾝ`, `Gｼｨﾙ`, `Gｿｳﾙ`, `Gﾊﾞｳﾝﾄﾞ`, `Gｱﾅｾﾙ`, `ｵｰﾌﾟﾆﾝｸﾞ`, `ﾛｰﾄﾞｾｰﾌﾞ`, `OVER` (ending 14), the
battle files, and the helpers `M`, `WAO_MAP`, `WAO_SUB`, `PCMPLAY`. Unreachable at stage 0:
`Cﾆﾝｼﾞｬ`, `Cｼﾊｲﾄｼ`, `Cﾃｲﾄ`, `Cﾀｶﾗ`, `Cｼｮｳﾆﾝ`, `各種DATA`, `Iｶﾝｶﾞｴ`, `TEST`, `Sｱｲﾃﾑ`, every
`T*`, the building scenes, `Gｶﾌｪ`/`Gﾏﾘｱ`, `Gｼｨﾙ2` 5.

**Open [I]:**
1. Path A never sets `Prov_Cur` before the duel, so `FIGT_A.ADV:1-30` sees the province
   clicked last: Nagasaki (55) would start `IJAPAN` 3 (`N_JapanInv_Stage := 1`), and a non-zero
   `Prov_Defense` makes `FIGT_A:43-44` replace `TroopLimit 5000` with `Prov_BattleArea`
   (`Iﾄｳｿﾞｸ:1394`). Captured 2026-10-09: Log A (32; clicked in week 18) reached the duel with
   its walls 6 as `Bt_WallBonus`; the limit, set to its Battle Area 320, goes back to 5000 in the
   duel's round header (Q-6); the port matches (EVT-08).
2. The map hit-tests (`COMMAND.ADV:99, 145`) start at index 0: a click near (0, 0) would pick
   province 0 / dungeon 0 or 27-30. Captured 2026-10-09: a click at (9, 5) picks province record 0
   and opens `Cﾃｷﾄｼ2` on it; the port matches (STR-12).
3. `Cﾒｲｷｭｳ.ADV:11-13` reads a leftover `Cmdr_Cur` on dungeon clicks on the Monsters' Cave (2),
   the Thieves' Cave (3) and the Isolated Shrine Island (15) — the port matches the original on
   the two such clicks of the owner's prologue run (2026-10-09, docs/golden-tests.md).
4. A unit left at status 0 by a battle is not refreshed by `MAIN.ADV:56`.

## Milestone 2 — story stage 1 (the Leazas civil war)

Decision 13. The main path first, then the commands by how often a player of stage 1 uses them;
one or more commits per step, each verified against its file run by `SceneRunner` (differential
tests) and, where the owner has recorded them, golden captures:

0. ✓ **Harness** (2026-10-10): the stage-1 command files, the ninja, the item screen and every other
   scene file are capture roots (groups `commands`, `ninja`, `screens`, `events`;
   [../golden-tests.md](../golden-tests.md)); the owner's checklist for a stage-1 session.
1. ✓ **W2** (2026-10-10, the W2 row): `MAIN.ADV:83-1045` whole — every block, also those closed at stage 1 (the Camilla
   unit's inline AI, the demon war) — and the resume after the Leazas autosave (`:83`); the nation
   phases are calls through the router.
2. ✓ **N1, part** (2026-10-10, the N1 row): the nation template (picker, filler, troop sum, target
   walk; the captures come with N2/N3) with `TCﾘｰｻﾞｽ` and `TCﾘｰｻﾞｽ2` on it. The rebels' turn is the
   first to call `FIGT_B` in a game, whose intercept opens `Sﾌﾞﾀｲ` inside the battle: the presenter
   now draws a picker a battle opens by hand over the battle's screen (it dropped it before), and
   `--auto` defends with the first unit free (abandoning the city when it cannot) and stops after
   week 30 (`--weeks n`).
3. ✓ **P3, part** (2026-10-10, the P3 row): the attack in `Cﾃｷﾄｼ` (`:99-425` with its refusals). From
   here the main path of stage 1 plays to stage 2.
4. ✓ `Cﾌﾞｶ`'s troop menu and the whole `Cﾊｰﾚﾑ` (2026-10-10, the P4-P7 row: both files are ported whole).
5. ✓ `Cｼﾊｲﾄｼ` and `Cｼｮｳﾆﾝ` (2026-10-10, the P4-P7 row: both files are ported whole).
6. ✓ `Cﾆﾝｼﾞｬ` and `Cﾃｲﾄ` (2026-10-10, the P4-P7 row: both files are ported whole).
7. ✓ `Cﾀｶﾗ` with `Sｱｲﾃﾑ`, and `各種DATA`, with their screens in the presenter and in Godot (2026-10-10,
   the P2 and P4-P7 rows: the files are ported whole, the item screen and All Data are drawn by hand).
8. ✓ **D1** (2026-10-10, the D1 / D2 row): the dungeon search in `Cﾒｲｷｭｳ`, the whole file but God's Door (with
   `Iｶﾐ` later); `--auto` searches the Monsters' Cave once, at the first map wait of stage 1 (with `--seed 7`
   in week 25: down to floor 4 with a battle; the rebels still take Leazas Castle, ending 2).
9. ✓ **The whole stage unattended** (2026-10-10): `--auto` plays
   stage 1 to stage 2 with gestures and menu choices only (decision 4; the policy is
   `godot/Strategy/GameScreen.cs`, "unattended play") — once a week a replenish (`Cﾌﾞｶ`, the unit
   whose replenish restores the most) and, while Rance has three quarters of his troops, an attack
   on the first of South (4), Ys (5), Oaks (6), Baranchi (7), Oku (3) the rebels hold next to an own
   province (`Cﾃｷﾄｼ`, Rance and the strongest, all places against Oaks); defences with the strongest,
   the dungeon search with the weakest; `--stage n` stops two weeks after stage n is reached. With
   `--seed 2` it takes South (4; week 26) and Ys (5; week 28), fails at Oaks three times and takes it in week 34 (`Iｼﾚｲﾌﾞ` 2, stage 2), wins
   `Iﾏｼﾞﾝ` 1's two defences of the castle in week 35 and stops in week 36; no file or path "not ported
   yet" is met on the way. Stage 1 is hard on an unattended player: without the weekly replenish
   none of seeds 1-13 took Oaks; with it 6 of 8 did, two reached the stop and in four the castle fell
   afterwards (seed 7: `Iﾏｼﾞﾝ` 1's second defence with nobody free). `--auto` never abandoned a city
   before: `FIGT_B`'s menu comes through the battle input (`ChooseAsync`), and a defence with nobody
   free looped forever; fixed.

**Milestone 2 is closed** (2026-10-10). The owner's three stage-1 capture runs (weeks 14-26 from
saves, every stage-1 command, the dungeon search, the rebels' turns and defences, then Oaks (6)
taken → `Iｼﾚｲﾌﾞ` 2, `GAL` 1, `Iﾏｼﾞﾝ` 1's two defences of Leazas Castle (1) and the first week of
stage 2; [../golden-tests.md](../golden-tests.md), the last three rows of "Findings so far") match
the port whole: all 885 captures, none reaching an unported path. Never captured, covered by the
differential tests only: the Library's completion, a load from the command menu, the ninja's No
Mission. Candidates for milestone 3, for the owner to choose and order: the independent cities
(P4, `Cﾃｷﾄｼ:429-3828` with Use Force), the nations N2/N3 (Helman, Zeth, JAPAN, Kayblis with their
`T*`/`T*2` files), God's Door (D2, `Iｶﾐ`), W4 (`ｽﾀMENU`, `OVER`).

Files without story gates (`MAIN`, `Cﾌﾞｶ`, `Cﾊｰﾚﾑ`, `Cｼﾊｲﾄｼ`, `Cｼｮｳﾆﾝ`, `Cﾆﾝｼﾞｬ`, `Cﾃｲﾄ`,
`Sｱｲﾃﾑ`, `各種DATA`) are ported whole; of `Cﾃｷﾄｼ` only the attack (the independent cities are P4), of `Cﾒｲｷｭｳ`
all but God's Door.

### What stage 1 reaches (`StoryStage = 1`, swept 2026-10-10)

Lines refer to `_extract\scenario_named` (assignments read `!StoryStage : n!`). [I] marks an
inference; everything else was read.

**Start.** `Iﾊﾝﾗﾝ.ADV:13-58` leaves units Rance (1) and Leila (5) through Asuka (11) alive, Sill
(2), Bound (3), Soul (4) dead, `Gold` 685; `:2141-2173` gives Oku (3), South (4), Ys (5), Oaks (6) and Baranchi (7) to the rebels (owner 2). The player
holds Leazas Castle (1), North (2), Wes (8) through Parapara Fortress (16) and Nothing (75).

**The gate of the stage.** `Cﾃｷﾄｼ.ADV:118` `{(StoryStage = 1) * (Prov_Owner \ 2): @L_0481f:}`
comes before the independent cities (`:121`) and Patton (`:124`): at stage 1 only the rebels can
be attacked, every other owner gets Maris's refusal (`:3863-3947`). So no other war can start —
`F_Japan_State := 1` is written only by `IJAPAN:1062` (needs Portugal (28), owner 3), `IJAPAN:2791`
and `TJAPAN2:4`; `F_Helman_War` only by `Gｽﾃｯｾﾙ` 1 (from `Tﾍﾙﾏﾝ2:4`); `F_Zeth_War` only by
`Tｾﾞｽ2:46` — and the independent cities' writers (M Land, Mars) are behind it too.

**The week** (`MAIN.ADV:83-1045`; no stage test in `:83-215`). Open at stage 1: the autosave
(`:83`), the date, the banner, income, the refill of units 1-48, the report; the AL Church
expense (−65, `:197`) every week (`N_ALChurch_Stage` stays 0 until `GAL` 1, called at `COM:152`
only at stage 2); `COMMAND`; the rebel phase `TCﾘｰｻﾞｽ` (`:298`) every week; the Sill kidnapping
chain `Gｼｨﾙ` 21-27 (`:896-925`, gated by `Week` only, so 21 plays at the end of the first Leazas
week — and 22 too in the same week after a prologue that ran to week 23, the tests being in
sequence [I]); `Gﾌｳｶ` 4 only past week 51. Closed: M Land, Galtia, Cream, `Gｶﾄﾞﾐｭｳﾑ` 1, Mars, the
AL Church phase, JAPAN, Helman, Patton, Zeth, Kayblis, Creator, Camilla, La Vaswald,
`Gﾏｼﾞｯｸ` 5, the demon and Kayblis wars. `TCﾘｰｻﾞｽ` is the only nation phase.

**The first Leazas week.** `COM:91` plays `Iｼﾚｲﾌﾞ` 1, which sets `F_LeazasRebels_State := 1`
(`Iｼﾚｲﾌﾞ:2119`): `Cﾃｷﾄｼ:144` refuses attacks on the rebels ("disrupted") that week. The rebel
phase moves it to 2 and plays `Gｴｸｽ` 10 (`TCﾘｰｻﾞｽ.ADV:5-8`).

**Attack.** A click on a rebel province next to an own one (`COMMAND.ADV:531-574`: Oku (3),
South (4), Baranchi (7)) → `Cﾃｷﾄｼ` → `Sﾌﾞﾀｲ` (`:218`) → the unit refusals (`:226-307`; at stage 1
only Satella 29 and Meglass 30, unlikely in the army [I]) → `TCﾘｰｻﾞｽ2` (`:318-320`: defenders by
province, no draws) → an unopposed take when no rebel commander is alive (`:395`) or `FIGT_A`
(`:419-425`). `:112` refuses a second attack in a week after a conquest. Clicks on Rockearth (17),
Shangri-La (29), Zeo (24) once Oku (3) is held (independent), Log B (31), Log A (32) (Helman) and
Adam's Fortress (58) (Zeth; it lists Parapara Fortress (16) as a neighbour) open `Cﾃｷﾄｼ` and get
the refusal (Adam's Fortress (58) confirmed by the owner's first stage-1 run); other enemy
provinces open `Cﾃｷﾄｼ2`. `FIGT_A` at stage 1 calls `Gｴｸｽ` 2 (Ys (5)) / 3
(Oaks (6)) and `死亡捕獲`.

**End.** `FIGT_A.ADV:192-195` `{(StoryStage = 1) * (Prov_Cur = 6) * (Bt_Result = 2): …
%#Iｼﾚｲﾌﾞ.ADV:` (Oaks; entry 2): `StoryStage := 2` (`Iｼﾚｲﾌﾞ.ADV:2689`), North (2) through
Parapara Fortress (16) to Leazas, `Gｴｸｽ` 4 (a three-way choice, `F_Ex_State` 100 or 4) — inside
the player phase. Oaks (6) borders only Ys (5) and Hanna (26, owner 3), so the shortest way is
South (4) or Baranchi (7), then Ys (5), then Oaks (6), one conquest a week: stage 2 in
the fourth Leazas week at the earliest [I] (the unattended run of step 9 took ten). Oaks is the hard
part: `TCﾘｰｻﾞｽ2` gives it up to four defenders who never retreat (`Bt_EnemyRetreatMode` 1), walls 4
and its battle area 232 as the troop limit, and the attacker loses when the 4-5 rounds run out. The
rebels' armies attack the first province the player holds in their lists (`TCﾘｰｻﾞｽ.ADV:10-110`):
A (commanders Pegasus (3), Dodge (5), Melfeis (4)) South (4), Oku (3), North (2), Leazas Castle
(1); B (Ex (1), Haurein (2), Sakanaku (6), Jibul (7)) Baranchi (7), Wes (8), Leazas Castle (1); once the player holds Ys (5), only C (up to four of all seven) at
Oaks (6), Ys (5) — so holding Ys (5) keeps the castle out of their reach. A unit that fought
this week cannot be replenished: `Cﾌﾞｶ.ADV:38-43` shows its status page instead of the troop menu.
At that week's end `MAIN:893-895` sets
`N_Camilla_Stage` 0 → 1; the next week `COM:152` plays `GAL` 1, and at its end `MAIN:887` plays
`Iﾏｼﾞﾝ` 1: two `FIGT_B` defences of Leazas Castle (1) (commanders Monster Generals 86+87, then
86+87 + La Seizel (90);
`N_Camilla_Stage := 2`). The milestone ends at the player phase after that week.
**Game over** at stage 1: losing Leazas Castle (1) (`FIGT_B:816-826, 877-882`, ending 2) or Rance's
death (`死亡捕獲:29-31`, ending 1).

**Commands** (the Leazas menu `COMMAND.ADV:226-316`, no stage gate): Construction `Cﾃｲﾄ`, Harem
`Cﾊｰﾚﾑ` (its `Y 4, 4` at `:7` on every visit; ported), Treasure `Cﾀｶﾗ` →
`Sｱｲﾃﾑ`, Merchant `Cｼｮｳﾆﾝ`, Subordinates `Cﾌﾞｶ` (the troop menu), All Data `各種DATA`, Think
`Iｶﾝｶﾞｴ` (a scene file), Load/Save, →Leazas Castle, Turn END. An own province opens `Cｼﾊｲﾄｼ`
(Temporary Levy with its 8 draws and story triggers such as Leazas Castle (1) `G1` 7, Alan (11)
`G2` 6, Rich (15) `Gﾚｲ` 2; Bolster Defense; the Leazas Castle (1) cells; the Special Room); enemy
cities offer `Cﾆﾝｼﾞｬ` (Kanami, `SET.ADV:16`; Sabotage against owners 2 and 3). Dungeons in own
provinces: 2 (Monsters' Cave, in Poor (14), a floor search), 3 (Thieves' Cave, in Leazas Castle
(1), `Gﾌﾞﾘﾃｨ` 1), 15 (Isolated Shrine Island, in Alan (11), `G1` 2/3/4 and a search); dungeon 1
(the Thieves' Hideout) lies in Golak (34) (Helman) [I].

**Unreachable at stage 1:** the independent cities (`Cﾃｷﾄｼ.ADV:429-3828`), the Patton hooks
(`:124-134`), the `Iｼﾚｲﾌﾞ` 3/5/6 gates, Papaya, Warg, the Zeth barrier, the AL Church visit, the
thieves' hideout, Ruins Search; every `T*` but `TCﾘｰｻﾞｽ`, every `T*2` but `TCﾘｰｻﾞｽ2`.

**Scenes.** `COM` tier 1: `Iｼﾚｲﾌﾞ` 1, `Gﾏﾘｽ` 15 (week 2; needs `F_Maris_RebelTalk`,
`Iﾄｳｿﾞｸ:1842`), `Gﾌｳｶ` 1/2 late; tier 2: `Gﾏﾘｽ` 1/2/3/13/14, `Gｶﾞﾝｼﾞｰ` 1, `Gﾊﾞﾚｽ` 1, `Gｺﾙﾄﾞﾊﾞ` 1,
`Gｼﾞｭﾘｱ` 1, `Gｱﾋﾞｧﾄｰﾙ` 2, `G2` 9, `Gｶﾅﾐ` 13 (about ten pairs [I]). None of the stage-1 scenes
calls a rule file (grep); the rule files they meet are the battles above.

**Files in milestone 2:** `MAIN`, `TCﾘｰｻﾞｽ`, `TCﾘｰｻﾞｽ2`, `Cﾃｷﾄｼ` (attack), `Cﾌﾞｶ`, `Cﾊｰﾚﾑ`,
`Cｼﾊｲﾄｼ`, `Cｼｮｳﾆﾝ`, `Cﾆﾝｼﾞｬ`, `Cﾃｲﾄ`, `Cﾀｶﾗ`, `Sｱｲﾃﾑ`, `各種DATA`, `Cﾒｲｷｭｳ` (search); the scenes
run from the player's files as before.

## Milestone 3 — story stage 2 (the wars of humanity)

Decision 14. What stage 2 reaches is in [stage-2-sweep.md](stage-2-sweep.md): the week is ported
(milestone 2: the Camilla Unit, the AL Church, Mars and the demon war run already), `COMMAND` and
the other command files have no stage-2 gate, the scenes run from the player's files; what is
missing are the nation files of the wars, the independent cities, JAPAN and God's Door. The milestone
starts in the player phase after `Iﾏｼﾞﾝ` 2's question (where milestone 2 ended) and ends with
`StoryStage := 3`. The main path first, then the optional fronts; one or more commits per step,
each verified against its file run by `SceneRunner` (differential tests) and the owner's captures
where recorded:

0. **Sweep and harness** (2026-10-10): [stage-2-sweep.md](stage-2-sweep.md). The harness needs
   nothing new (every `T*`/`T*2`, `Cﾃｷﾄｼ` and every scene file are capture roots already); the
   owner's stage-2 checklist is in [../golden-tests.md](../golden-tests.md). Stage-2 captures start
   from the owner's saves (`ｽﾀMENU`'s "From 3" needs `DebugMode`).
1. **Helman** (N2, part) ✓ (2026-10-10, the N2 / N3 row): `Tﾍﾙﾏﾝ2` (the defenders; its first run
   plays `Gｽﾃｯｾﾙ` 1, which starts the war), `Tﾍﾙﾏﾝ` (the nation turn on `NationFile`) and `Tﾊﾟｯﾄﾝ`
   (Patton's army, after Helman falls).
2. **Zeth** (N2, part) ✓ (2026-10-10, the N2 / N3 row): `Tｾﾞｽ2` (the defenders, the barrier,
   Papaya's trap; its first run starts the war) and `Tｾﾞｽ` (the nation turn on `NationFile`: the
   war's scenes, the ministers' and the generals' armies).
3. **The gate** ✓ (2026-10-10, the N2 / N3 row): `Tｹｲﾌﾞﾘｽ2` (Monster Generals refilled, Warg at
   Tower of Evil (78)), reached by the attack after `Iｼﾚｲﾌﾞ` 6. From here the main path plays to
   stage 3.
4. **The independent cities** (P4) ✓ (2026-10-11): `Cﾃｷﾄｼ.ADV:429-3828` — the dispatch, Negotiate,
   Use Force and the eleven cities' paths; `Cﾃｷﾄｼ` is ported whole. Part 1 (2026-10-10) in `Strategy/EnemyCityCommand.Cities.cs`: the
   cities' own exits (`:429-471`: Pluepet's visit to Portugal (28), `L_02e1e`, Gold + 100, then the
   city's picture again; Shangri-La (29) `Iｼｬﾝｸﾞﾘﾗ` 1/2; Custom (21) `Iｼﾚｲﾌﾞ` 3; Toushin City (27)
   `Sﾌﾞﾀｲ` and `Gｻｰﾅｷｱ` 1), Maris's question and the menu (`:472-506`; no Negotiate at Temple AL HQ
   (77) or Shangri-La), Stop (back to the city's menu without the header), Negotiate up to the
   mayor called out (`:528-603`; Rockearth (17) after the DX president's death refuses), and Use
   Force whole (`:3649-3828`: no refusals CMD-01, the last battle's walls BFL-15, each city's army
   with its name `MS 10` from the player's files, `Gﾚﾍﾞｯｶ` 2 / `Gｶｾｲ` 1 after Portugal / Mars fall).
   Checked against `Cﾃｷﾄｼ` run by `SceneRunner` on 5000 random states (`CommandSliceDifferentialTests`,
   the cities 17-29 added) and unit tests (`CommandSliceTests`). Part 2 (2026-10-11) in
   `Strategy/EnemyCityCommand.Talks.cs`: the cities' negotiations (`:604-3648`) — the dispatch (a
   province it does not name falls into Mars's talk, which only Mars (22) reaches: the cities hold
   17-29 alone), each city's talk and question (`AnswerAsync`, a cancel asks again) and its ends: a
   city that gives in becomes Leazas's, spends the week's attack, loses its heroes (`L_05109`) and
   gets the new owner's flag on the map. Rockearth (17): "I'll approve you" `F_DX_State := 1`, Gold
   + 500; "Disgusting, die" `F_DX_State := 100`, the header. Ice (18) gives in. Diphtheria (19):
   the menu again. Lazeal (20): "No" the header, "I'll wait" `DXTalk_RetryWeek := Week + 4` and the
   file left (the attack not spent), the mayor holding Rance to it until that week has passed, then
   `Gﾚｨﾘｨ` 1 and the city. Mars (22): "Reject" the header, "Make a General" Mars the Great (unit 26)
   joins, `N_Mars_Score := 2` (no `Gｶｾｲ` 1, unlike Use Force). Red (23): "I promise" the city, "I
   can't promise" the header. Zeo (24): refused, the file left. M Land (25): "No" the file left,
   "Maintain the Park" `F_Sayori_State`, `F_MLand_Funded` and the city. Hanna (26): "Pay" refused
   below 29 gold, else 30 (CMD-04) and the city, `F_Shinoda_State := 1` with Gengorou (commander 8)
   alive; "Refuse" the file left. Portugal (28): "Pay and Conquer" refused below 90, else 90;
   "Don't Pay" the city for free (CMD-03). The payments are booked to `Hist_Cur`'s slot (CMD-14).
   Checked against the file run by `SceneRunner` on 3000 random states (the ten cities and now and
   then any province held by the cities, the DX state, Lazeal's week, gold around the demands,
   Gengorou alive or not; every question, answer, refusal and end reached —
   `CommandSliceDifferentialTests`) and unit tests (`CommandSliceTests`); the dictionary's
   `F_DX_State`, `DXTalk_RetryWeek`, `F_Shinoda_State` and `N_Mars_Score` corrected. No stage-2
   capture recorded yet.
5. **JAPAN** (N2, the rest) ✓ (2026-10-11): `TJAPAN2` and `TJAPAN`, both ported whole. Part 1
   (2026-10-11, the N2 / N3 row): `TJAPAN2` (the defenders) in `Strategy/JapanDefenders`. Part 2
   (2026-10-11, the N2 / N3 row): `TJAPAN`'s rules (`:1-294`, `L_0327c`) in `Strategy/JapanTurn`.
   Part 3 (2026-10-11, the N2 / N3 row): Gekkou's attempts (`N_Gekkou_Attempts` 1-4, `L_006ac`:
   the scenes `:295-2744` with Gekkou's and Shinobu's raids, ending 4) in
   `Strategy/JapanTurn.Gekkou.cs`.
6. **God's Door** (D2) ✓ (2026-10-11, the D1 / D2 row): `Cﾒｲｷｭｳ`'s gate (`:233-236`), `L_04ddc`
   and the room (`:3500-4556`) in `Strategy/DungeonCommand.GodsDoor.cs`, so `Cﾒｲｷｭｳ` is ported
   whole; `Iｶﾐ` runs from the player's files (its endings end the game as `GameEnd` until W4 ports
   `OVER`; not run end to end yet — no capture reaches it).
7. **The whole stage unattended, the owner's captures**: not now (decision 15, 2026-10-11) —
   `--auto` does not play stage 2, the stage-2 captures are put off.

**Milestone 3 is closed** (2026-10-11): every file and range stage 2 reaches is ported (steps
1-6), checked against the files run by `SceneRunner` on random states and by unit tests; no
stage-2 capture is recorded yet, and `Iｶﾐ` (God's Door's arc, run from the player's files) has not
run end to end. The stage-3 sweep found one gap stage 2 already had: `FIGTATTK`, the battle's
result animation, which the scene `FIGT表示` calls (from `Iﾏｼﾞﾝ` 12-18 and `Gﾊﾟｯﾄﾝ` 2), was neither
a scene nor routed; it is now routed to the battle's port (`Battle.PlayExchangeAnimationAsync`),
checked against the file on 1900 random runs of `FIGT表示`, and `ScenarioProgramSweepTests` checks
that every file the scenes call is a scene, a port or a helper.

## Milestone 4 — story stage 3 (the Kayblis war)

Decision 15. What stage 3 reaches is in [stage-3-sweep.md](stage-3-sweep.md): the week, the
commands, `Tｹｲﾌﾞﾘｽ2` and the scenes run already; what is missing is `Tｹｲﾌﾞﾘｽ` (1855 lines, the
Kayblis Empire phase of every stage-3 week). The milestone starts in the first week of stage 3 and
ends with `StoryStage := 4` (`Iｼﾚｲﾌﾞ` 7, before that week's `Tﾙﾄﾞﾗ`) or ending 8. Steps, in the
sweep's order (its "Proposed order"):

0. **Sweep** ✓ (2026-10-11): [stage-3-sweep.md](stage-3-sweep.md). The owner's stage-3 checklist
   waits with the captures (decision 15).
1. **`Tｹｲﾌﾞﾘｽ`, the frame** ✓ (2026-10-11, the N2 / N3 row): `:1-325`, the guards `:625-669`, the
   template `:1509-1614`, the captures and collapses `:1615-1778`, the helpers in
   `Strategy/KayblisTurn` on `NationFile`, routed in `StrategyRules`; the armies (`:326-624`,
   `:670-1508`) threw `NotSupportedException` until steps 2, 3 and 5 — so an interactive or
   `--auto` stage 3 stopped then at the first army that marched (Babolat's, in week S). The sweep's
   claims checked: NAT-01 (Shangri-La (29) is never Helman's, so Helman's collapse flips nothing; new:
   Zeth's collapse also kills Saias (113)), NAT-04 (dead in play), survey-nations §2.2/§2.4/§9 fixed;
   the dictionary's VAR0637 is now `F_Galtia_DefectedToKayblis` and `F_Galtia_JoinedKayblis` is now
   `F_DemonCastle_Attacked` (sweep, open question 6). The candidate "`Gｼﾙｷｨ` 8 every week" is wrong (it
   sets Silky's state to 8); new quirks NAT-25 (Medusa's scenes replay), NAT-26 (`Gｼﾙｷｨ` 8 does not end
   the event chain), NAT-27 (the collapses of wars never started).
2. **The generals' units** ✓ (2026-10-11, the N2 / N3 row): `:729-1328` in
   `Strategy/KayblisTurn.Generals.cs` — Babolat, Redeye, Kite, Galtia and Kesselring, each on its line
   to the first province not Kayblis's, the player's attacked (`FIGT_B`), any other taken. The sweep's
   claims checked and confirmed: the second attack after a battle held to the time limit (NAT-03; not
   Kesselring's), Babolat's pitfall (`Gﾊﾞﾎﾞﾗ` 4, only on a player's province), Redeye alone at Maunes
   (13) and his scenes and steps around his battle, `Gﾌﾘｰｸ` 4 instead of a battle, Kite's two draws and
   Calory's scenes `Gｶｲﾄ` 2-7, `Gｶﾞﾙﾃｨｱ` 2 after a lost defence, Kesselring's flight and `Gｹｯｾﾙ` 2, the
   unread troop sums (NAT-16). New: NAT-28 (after `Gﾚｯﾄﾞｱｲ` 8 with no ninja free, the battle at
   Maunes (13) hangs on a stale cursor); the generals' empty-unit tests are dead (NAT-16). An
   interactive or `--auto` stage 3 now stops at Piar's unit, Kayblis's army or the Assault Leaders.
3. **Piar and the assault** (~240): Piar `:670-728`, Kayblis's main army `:1329-1508`. Part 1 ✓
   (2026-10-11, the N2 / N3 row): Piar's unit in `Strategy/KayblisTurn.Armies.cs` — a random province
   of 1-77 until it is the player's and not Nothing (75), a draw in ten giving up after each miss
   (NAT-15), `Gﾊﾟｲｱｰﾙ` 2, PG-7 (136), PG-9 (137), PG-X mk2 (138) through `FIGT_B`, then `Gﾊﾟｲｱｰﾙ` 6
   with Miki (unit 32) defending or Piar's state's steps; her empty-unit test and capture are dead
   (NAT-16). Part 2 ✓ (2026-10-11, the N2 / N3 row): Kayblis's main army in the same file — up to six
   marches on one line (Redeye's without his stop at Maunes (13)), all retreat mode 2: Kayblis (135)
   alone, then Silky (156), Galtia (131, once defected), Satella (157), Meglass (158) alive, each with
   Monster Generals (86, 88, 89), then Monster Generals (87, 86, 88, 89), each after the four are
   refilled above their maximum (NAT-10); no unit is tested for being empty. With it step 3 is done;
   an interactive or `--auto` stage 3 now stops at the first Assault Leader (past week 100).
4. ~~`FIGTATTK`~~ ✓ (done with milestone 3's close, above).
5. **The Assault Leaders** ✓ (2026-10-11, the N2 / N3 row): `:326-624` in
   `Strategy/KayblisTurn.Assault.cs` — one Assault Leader (191-198) per threshold passed (weeks 100,
   110, … 150, 155, 160), on the north and the south list in turn, retreat mode 1, with Monster
   Generals (87, 88) called up for every leader. The sweep's claims checked and confirmed: the call-up
   to `max/5 + RND(max/3)` (two draws a leader, `:339, 344, 505, 510`), a dead leader revived at 1 HP
   and home that week (`:347-351`), home under 1000 troops after his recovery (`:360-362`), alone at
   Maunes (13) on the north list (`:436-442`; the south list keeps the unit there), the south list's
   run-on into `L_00faf` at Leazas Castle (1) (`:614-618`, dead in play, NAT-16). With it `Tｹｲﾌﾞﾘｽ` is
   ported whole: an interactive or `--auto` stage 3 now plays every Kayblis phase.
6. **The end** ✓ (2026-10-11): `StageThreeEndTests` (content) runs Kayblis's (135) death the way a
   battle ends — `死亡捕獲` (the battle's port) calls `Gｹｲﾌﾞﾘｽ` 10, which calls `Iｼﾚｲﾌﾞ` 7, both from
   the player's files, on the world of a new game moved to stage 3: with every province but
   Shangri-La (29) and Nothing (75) Leazas's the story moves to stage 4 (the week's Creator phase,
   `Tﾙﾄﾞﾗ` at `MAIN:816`, then stopped the session as "not ported yet" — ported in milestone 5 step 1,
   which carries the test on into it); with Kayblis's Castle (84),
   Laurengrad (48, Helman's) or Red (23, an independent city) not Leazas's the game ends with
   ending 8 (`&#OVER`, `GameEnd`). Nothing in `Tｹｲﾌﾞﾘｽ` tests the stage, so a Kayblis killed in his
   own army's battle leaves the rest of that phase to run (the sweep's [I] confirmed); the phase
   itself runs only at stage 3 (`MAIN:594`).

**Milestone 4 is closed** (2026-10-11, decision 16): every file and range stage 3 reaches is ported
(steps 1-6; `Tｹｲﾌﾞﾘｽ` whole), checked against the files run by `SceneRunner` on random states and by
unit tests, and stage 3 runs to its end in the port (`StageThreeEndTests`). As with milestone 3, no
stage-3 capture is recorded yet and `--auto` does not play stage 3.

## Milestone 5 — story stage 4

Decision 16. Stage 4 begins when `Iｼﾚｲﾌﾞ` 7 finds the whole map Leazas's at Kayblis's death; its first
rule file is the Creator phase `Tﾙﾄﾞﾗ` (`MAIN:816`, 205 lines), the first file the port stopped at
(ported in step 1).

Steps, in the sweep's order (its "Proposed order"):

0. **Sweep** ✓ (2026-10-11): [stage-4-sweep.md](stage-4-sweep.md). Stage 4 is the last stage: every
   week the Creator phase sends Angel Knights against five random player provinces and an earthquake
   at two more, the player may no longer attack (`Cﾃｷﾄｼ:109`), and the game ends only in `&#OVER` —
   Leazas Castle (1) lost, Rance's death, or God's Door → `Iｶﾐ` (endings 9, 10, 11, 13), which opens
   only at stage 4 and only if Kou's arc stood at 8 when `Gﾙﾄﾞﾗ` played. The owner's stage-4
   checklist waits with the captures (decision 16).
1. **`Tﾙﾄﾞﾗ` whole** ✓ (2026-10-11): `Strategy/CreatorTurn`, routed in `StrategyRules` (the N1 row):
   the first phase's `Gﾙﾄﾞﾗ` 1, the counter, seven searches (Nothing (75) and the 20th try give up,
   NAT-29; the spared provinces below 5 and the castle roll from 6 on, NAT-30), five attacks by the
   Angel Knights through `FIGT_B` and two earthquakes; NAT-11 settled (status 0 changes nothing).
   Checked against the file on random states (`NationTurnDifferentialTests`) and by unit tests
   (`CreatorTurnTests`); `StageThreeEndTests` runs on from Kayblis's death into the first phase with
   `Gﾙﾄﾞﾗ` from the player's files. By the sweep's table nothing stage 4 reaches is left unported but
   `OVER`, which stays `GameEnd` (W4).
2. **The end**, after a survey of `Iｶﾐ` of its own (decision 17; step 2a ✓ 2026-10-11,
   [survey-ikami.md](survey-ikami.md): six floors, Rudrasaum on 6F, four garrisons and 20 kaiju battles,
   the picker's soft lock without a free unit, ten quirk candidates, a recipe per ending): content
   tests from the player's files — `Gﾙﾄﾞﾗ` with Kou at 8 through Ho Raga's Bonsai
   Statue (item 18) and God's Door to `Iｶﾐ`'s endings, and Kou below 8; `Iｶﾐ`'s first run end to end.
3. **Docs**: the sweep's quirk candidates registered, survey-nations and stage-2-sweep corrected, the
   dictionary's entries the sweep found wrong.
4. ~~**W4** (`OVER`, `ｽﾀMENU`) only if the owner puts it in this milestone~~ — not in it (decision
   17): every ending stays `GameEnd`.
