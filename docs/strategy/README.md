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

Not yet surveyed: `Iｶﾐ.ADV` (6.5k lines, God's Door, four battles) and `FIGT表示.ADV`.

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
| **W2** | `MAIN` week loop + the nation-phase hosts hidden in scenes (RULE-1). Stage 0 done (2026-10-08): `Strategy/WeekLoop` ports `MAIN.ADV:1-82` by hand — `RunWeekAsync` is one pass of `L_00020` (the `week` capture's span), `RunAsync` the endless loop; the top of the week (`WAO_SUB` 3, `M` 0, `Week + 1`, the history shift STR-01, music off), the thieves' week (autosave, date, music 1, pan to the hideout, banner, units 1-4 refreshed, `Hist_Funds`, `COMMAND`, `StoryStage 99 → 1` + `Iﾊﾝﾗﾝ`, "Then some time passed"); the Leazas week (`:83-1045`) throws `NotSupportedException` before its first statement. The autosave is a call of `ﾛｰﾄﾞｾｰﾌﾞ` with `RetVal 3` (decision 6), ported with G0 part 1; the week loop names the call (`ScriptContext.SaveResume`) so a slot knows where a load goes on. Routed in `StrategyRules` (`MAIN.ADV`); `GoldenReplay` replays `week` captures (label root `MAIN.ADV@20`, `ReplayableLabelRoots`) — **none recorded yet**, proven on a capture the core writes. Checked against `MAIN.ADV` itself run by `SceneRunner` on 1500 random states for one to three weeks (calls, events, inputs, memory equal — `WeekLoopDifferentialTests`), unit tests (`WeekLoopTests`), and `PrologueCommandTests`: the new game's first two weeks through the router, and Turn END in week 23 → `Iﾄｳｿﾞｸ` 2 (duel lost) → `ｵｰﾌﾟﾆﾝｸﾞ` → stage 1 + `Iﾊﾝﾗﾝ` → the Leazas week. Remaining: the Leazas week, with the nation phases as hooks | main, events | W1, nations as hooks |
| **W3** | `COMMAND` player phase (2026-10-08, in progress): the whole file hand-ported in `Strategy/CommandPhase` — `COM`, the construction timer, the weekly flags, the map loop, both menus, Turn END, the click routing; every other file is called by name through the hooks. Owner decision: the map is read as the script reads it — `IM` polled, `IC` (`ISceneInput.SetCursorAsync`), the drag-scroll `WAO_MAP` op 1 ported in the context — and a UI that thinks in provinces queues gestures (`Script/PointerScript`). Quirks STR-19 to STR-21. Checked against `COMMAND.ADV` itself on 800 random states with random gestures (every file call reached; inputs, calls, events and memory equal — `CommandPhaseDifferentialTests`), the drag against `WAO_MAP.ADV` on 2000 pointer runs, plus unit tests; `GoldenReplay` replays `command` captures (root `COMMAND.ADV`). Part 2 (2026-10-08, owner's plan): the prologue paths of the command files ported by hand in `Strategy/` on a shared `CommandFile` base — `EnemyCityCommand` (`Cﾃｷﾄｼ:1-108`: header, menu, Pillage → `Iﾄｳｿﾞｸ` 1 and `ｵｰﾌﾟﾆﾝｸﾞ`, AL Church, Ninja, Return), `DistantCityCommand` (`Cﾃｷﾄｼ2`, whole), `SubordinatesCommand` (`Cﾌﾞｶ`: the picker loops, the status page `L_0179c`, the 50 talk labels, Rance's monologue, the prologue's `L_00f4b`), `HaremCommand` (`Cﾊｰﾚﾑ`: payment and refund, the stage-0 list, the 50 visit labels, the reward), `DungeonCommand` (`Cﾒｲｷｭｳ:1-174` with the names from the player's files, and `L_03cbd`); their other paths (attack, troop menu, the later harem list, the dungeon search and dispatch) throw `NotSupportedException`. The router `StrategyRules` (an `IStoryHooks` bound to the context) runs `COM`, `COMMAND` and these files and hands every other file to the hooks it wraps; it is the rule hooks of a `SceneRunner`. Quirk CMD-21. Checked against each file run by `SceneRunner` on 1500 random states (mostly stage 0) with random input and scribbling hooks (`CommandSliceDifferentialTests`; where the port stops at an unported path, the run is compared up to there), unit tests, and a prologue player phase on the boot's world with the player's scenes (`PrologueCommandTests`: Pillage, Bound's talk, Sill's visit, the hideout, Turn END); `GoldenReplay` has entry points for the `commands` roots. Part 3 (2026-10-08): the pickers ported by hand on a shared `PickerScreen` base (the 3×17 grid, the pointer loop with its highlight and details panel, the bar) — `DeploymentScreen` (`Sﾌﾞﾀｲ`: party toggle, remove-last, Go = validate + pay, Stop; BFL-01, CMD-16), `SubordinateScreen` (`Sﾌﾞｶ`, all modes), `HaremScreen` (`Sﾊｰﾚﾑ`, its name table read from the player's files: `ScenarioParser` now reads `#L_x, 1:` string tables, `F1`/`F3` and data lines, and `SceneRunner` runs `NC`/`NB` on bound variables); routed in `StrategyRules`; `PointerScript.PickerCell`/`PickerButton`/`PickerOutside` give a UI the gestures. Quirks CMD-22 to CMD-24. Checked against each file run by `SceneRunner` on 1500 random states with random pointers (`CommandSliceDifferentialTests`), unit tests (`PickerScreenTests`), and `PrologueCommandTests` (the week now picks Rance, Bound and Sill with clicks); `ScreenReplayTests` replays `screens` captures — **none recorded yet**. The battle's own `Sﾌﾞﾀｲ` call (`FIGT_B`) reaches the ported picker since the battle runs in the router on the game's input (decision 7). W2 (stage 0) followed | main, commands | W1 |
| **P2** | Army screens `Sﾌﾞﾀｲ`, `Sﾌﾞｶ`, `Sｱｲﾃﾑ`, `Cﾀｶﾗ` (replaces the battle's `Sﾌﾞﾀｲ` hook); `Sﾌﾞﾀｲ` and `Sﾌﾞｶ` are ported with W3 part 3, and the battle reaches them since it runs in the router (decision 7) | commands | F0 |
| **P3** | Enemy-city attack and Use Force (`Cﾃｷﾄｼ` rules, `Cﾃｷﾄｼ2`) | commands | P2, N1 |
| **P4-P7** | Negotiations; own city + merchant; subordinates + ninja + construction; harem + `各種DATA` | commands | W3, E2 |
| **N1** | Nation template + Rebels, Patton, Rudra + all `T*2` defender pickers | nations | F0 |
| **N2 / N3** | Helman, Zeth, JAPAN / Kayblis | nations | N1 |
| **D1 / D2** | Dungeon flow / dungeon scenes with state | dungeon | P2, E2 |
| **G0** | Godot: strategic map screen, command menus, scene player, save/load (`ﾛｰﾄﾞｾｰﾌﾞ` ported whole, decision 6); one input implementing `IGameInput` for the battle and the strategic screens (decision 7). Done 2026-10-09 in four commits (decision 8). Part 1 (2026-10-09): `ﾛｰﾄﾞｾｰﾌﾞ` ported whole by hand in `Strategy/LoadSaveScreen` (modes 0/1/2/3/10/11 and the error box, the slot lists, the page-4 sync, the autosave notice, and `ResumeAsync`, the way back after a load: `:34` with `RetVal 0`, "Load Complete"), routed in `StrategyRules`; the engine's save commands `QD`/`LD`/`LP`/`QP`/`QC`/`LT` in `Script/ScriptContext.Save.cs` on an `ISaveStore` (`Script/SaveData.cs`: the slot = page 0, pages 1, 2, 4, 10 and the strings plus the autosave's call as the resume point; `MemorySaveStore`, `DirectorySaveStore`, the port's own versioned format, decision 11), shared with `SceneRunner`, which now runs them; `LD` leaves the running code by `GameLoadedException`. Quirks STR-22, STR-23. Checked against the file run by `SceneRunner` on 800 random states and slot sets, every mode and the way back (`LoadSaveDifferentialTests`: events, menus with their captured texts, slot reads/writes/times, memory), unit tests (`SaveDataTests`, `LoadSaveScreenTests`), and the prologue weeks through the router now autosave into slot 1 (`PrologueCommandTests`). Part 2 (2026-10-09): the game driver `Strategy/GameSession` — one boot on a fresh state, then the new game or a load from the title (`ﾛｰﾄﾞｾｰﾌﾞ` 0; backing out returns null), the week loop; a `GameLoadedException` from anywhere (the title, `COMMAND`'s Load/Save) goes on with the save screen's "Load Complete" and `WeekLoop.ResumeAsync` (the week from `MAIN.ADV:21`; the Leazas autosave `:83` not ported); `&OVER` ends the game with its `EndingId` (`GameEnd`), `&MAIN` is followed; the view/input proxies `ViewProxy` and `GameInputProxy` (decision 4). Tested on the player's files: a game saved at week 9 loaded from the title in a new session, a load from the command menu, backing out of the load list (`PrologueCommandTests`), and `WeekLoopTests`. Part 3 (2026-10-09): semantic screen events in the core (`Script/ScreenEvents.cs`: `MapScreenEvent` from `COMMAND`'s map wait, `PickerScreenEvent` from each picker, `SaveScreenEvent` from `ﾛｰﾄﾞｾｰﾌﾞ`, `BattleScreenEvent` from the router around the battle files; skipped by the differential comparisons) and `PointerScript.Move` (the pickers' highlight follows the pointer); the presenter `Kichikuou.Content/Strategy/GamePresenter` on two layers (decision 12): the stage — the engine surface where the scenes' own drawing commands run, the map is drawn from GameState and the battle draws through `BattlePresenter` on the same surface — and the screens drawn by hand over it, composed into a 640×400 `Screen`: the `WAO_SUB` window (frame and text from `Ui_*` and the events, `TextBox`), the portraits and centre picture, the menus (`SelectionBox` at the `B1`/`B2` window), the pickers (`PickerView`: grid from GameState, highlight from `Tmp0033`/`Tmp0029`, the panels the script prints into, bar labels and harem names from the player's files), fades. Tested headless on the player's files (`GamePresenterTests`: week 9 with Sill's CG scene, Pillage with the picker and the battle, a talk, a harem visit, the hideout, a save into slot 2, week 10; the opening; no unresolved text, picker clicks through `PickerView.HitTest`). Part 4 (2026-10-09): the Godot screens — the title `godot/Main` (decision 10: New game, Load, Quit, the battle-capture viewer `Battles/CaptureList` as a debug entry) and `godot/Strategy/GameScreen`, one game per screen: a fresh `GameState` and `GameSession` (`XSystem35Random` on the tick count or `--seed`, slots in `DirectorySaveStore(user://saves)`, unported files logged and skipped), its view and input behind `ViewProxy`/`GameInputProxy`, drawn by the `GamePresenter` (fades animated over their ticks) and played with the mouse — key waits, menus (`MenuHitTest`, right click cancels), the map's clicks, drags (in steps while held) and right clicks and the pickers' clicks (through `HitTest`) as `PointerScript` gestures, one pointer read per frame where the scripts poll, the battle's input screens as in the capture viewer; the clock is the real date and a stopwatch (`ZT1`-`ZT5` as `cmdz.c`), `SC` reports no CD, `IC` returns the shape before. `GameEnd` shows "THE END — ending n", a `NotSupportedException` a message, both back to the title. The presenter gained `MaskEventCgs` (debug: the stage under the full-screen window is a grey placeholder with the CG's number, on for the agent's dumps and `--shots`, off for play) and closes a picker its file left open when the map is shown (`Cﾌﾞｶ`'s End returns without closing `Sﾌﾞｶ`; `COMMAND.ADV:358-359` shows the map). Smoke-tested headless: `--newgame --auto --fast --quit` plays the whole prologue unattended (one visit each to Harem, Subordinates and the hideout, Turn END every week, the duel, the opening) to the Leazas week's `NotSupportedException`, `--load` resumes the autosave; screens checked masked with `--shots` | — | W3, E2 |

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
   the pointer gestures the script reads (`Script/PointerScript`). The look follows the original
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
`StoryStage := 0`, `Gold := 50`, `Iﾌﾟﾛﾛｰｸﾞ` (writes only `CurrentBgm := 1`, `Dgn_Cur := 1`),
`&MAIN`. `SET` ran at boot: units 1-4 alive, Leazas owns provinces 1-16 and 75.
`MAIN.ADV:19-82` (gate `StoryStage = 0`) is the whole week: week + 1, history shift, autosave
(`:20`), units 1-4 with status ≠ 0 get status 1, full HP, troops + 2 up to the cap (`:50-64`),
`Hist_Funds := Gold`, `COMMAND`, then `StoryStage = 99` → 1 + `Iﾊﾝﾗﾝ` (`:70-73`), loop. No
income, expenses, nation phases or end-of-week checks — **no `T*` file runs**. `COM.ADV:1-10`
plays `Iｼﾚｲﾌﾞ` 10 in week 9 and `Gｿｳﾙ` 4 otherwise and returns: no upkeep, no triggers.
`COMMAND`: weekly flags (`:38-46`), map loop (`:47-161`), stage-0 menu (`:183-225`).

| Input | Handler | Effect |
|---|---|---|
| Province 33/34 | `COMMAND.ADV:516-521` → `Cﾃｷﾄｼ` | stats as "??", menu Pillage / Return (`:62-84`); Pillage → `Iﾄｳｿﾞｸ` 1, then `ｵｰﾌﾟﾆﾝｸﾞ` if the stage changed (`:99-108`) |
| Any other province | `Cﾃｷﾄｼ2` | view only (`:42-64`) |
| Dungeon 1 | `Cﾒｲｷｭｳ:168-170`, `L_03cbd` | hideout banter, `Y 4, 3` (`:2804`) |
| Dungeons 2/3/15 | `Cﾒｲｷｭｳ:7-28, 172-173` | camera pan, strings 8/10, `Ui_CmdDungeonAvail := 0`, return |
| Menu: Harem | `COMMAND:337-343` → `Cﾊｰﾚﾑ` (`Sﾊｰﾚﾑ` list) | once a week; Sill, Soul, Anasel if `F_Thieves_State > 1`; `G*` 10; `Harem_SexCount + 1`, unit 1 EXP + `Harem_ExpReward` (`:553-577`) |
| Menu: Subordinates | `COMMAND:354-360` → `Cﾌﾞｶ` (`Sﾌﾞｶ`) | once a week a talk: unit 1 monologue (`N_RanceSelfTalk + 1`), 2/3/4 → `Gｼｨﾙ`/`Gﾊﾞｳﾝﾄﾞ`/`Gｿｳﾙ` 20; afterwards status view only |
| Menu: →Thieves' Hideout | `COMMAND:490-501` | camera pan |
| Menu: Load/Save | `COMMAND:370-376` | `ﾛｰﾄﾞｾｰﾌﾞ` with `RetVal 2` |
| Menu: Turn END | `COMMAND:377-425` | `Week > 22` → `Iﾄｳｿﾞｸ` 2, `ｵｰﾌﾟﾆﾝｸﾞ`, `StoryStage := 99` (`:419-424`) |

**Scenes.** `Gｿｳﾙ` 4, `Gｿｳﾙ`/`Gﾊﾞｳﾝﾄﾞ`/`Gｼｨﾙ` 20 write nothing outside `ui`/`scratch`/`system`;
`Iｼﾚｲﾌﾞ` 10 only leaves `Prov_Cur = 34`. `Gｼｨﾙ` 10 (`:2599`) and `Gｿｳﾙ` 10 (`:3063`) set
`VAR0398 := 1`; `Gｱﾅｾﾙ` 10 sets `F_Thieves_State 2 → 3` (first scene, leaves `Prov_Cur = 39`); the
harem visits set `Harem_ExpReward` (Sill 5, Soul 3, Anasel 4) — confirmed by running them (E2,
`PlayerSceneTests`). `Iﾄｳｿﾞｸ` 1 (Pillage, `:110-953`): once a week
(`Ui_CmdAttackAvail`), first use `F_Thieves_State := 1`, commander 58 reset, `Sﾌﾞﾀｲ` picks units
(cancel returns), `FIGT_A` (`Bt_Kind 1`; the conquest at `FIGT_A:104-107` is skipped at stage
0). After a win `Y 4, 4` banter (`:342`, drawn even when replaced): `F_Thieves_State = 3` on 34
→ the Merci path (`L_013ff`); `F_Thieves_State = 1` on 34 with `VAR0398 = 1` → Anasel captured,
`F_Thieves_State := 2`; otherwise `Gold + 1`. Deaths are not fatal at stage 0 (`FIGTﾐｶDE`,
`死亡捕獲:29, 36-38, 106-110`).

**End.** Both ways lead to the duel `Iﾄｳｿﾞｸ L_016f2` (units 1, 3, 4, 2 vs commander 31,
`Bt_TroopLimit 5000`, `Bt_IsDuel 1`, `:1384-1399`): (A) Turn END in week 23, (B) the Merci
path. Losing it is the story: `StoryStage := 99` at `Iﾄｳｿﾞｸ.ADV:1841` (the first write off 0),
`ｵｰﾌﾟﾆﾝｸﾞ` (`F_OpeningSeen := 1`), back to `MAIN:70-72` → stage 1, `Iﾊﾝﾗﾝ` (stage 1 again,
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
   clicked last: province 55 would start `IJAPAN` 3 (`N_JapanInv_Stage := 1`), and a non-zero
   `Prov_Defense` makes `FIGT_A:43-44` replace `TroopLimit 5000` with `Prov_BattleArea`
   (`Iﾄｳｿﾞｸ:1394`). Capture a week-23 Turn END (not in the owner's first prologue run, 2026-10-09).
2. The map hit-tests (`COMMAND.ADV:99, 145`) start at index 0: a click near (0, 0) would pick
   province 0 / dungeon 0 or 27-30. Captured 2026-10-09: a click at (9, 5) picks province record 0
   and opens `Cﾃｷﾄｼ2` on it; the port matches (STR-12).
3. `Cﾒｲｷｭｳ.ADV:11-13` reads a leftover `Cmdr_Cur` on dungeon clicks 2/3/15 — the port matches the
   original on the two such clicks of the owner's prologue run (2026-10-09, docs/golden-tests.md).
4. A unit left at status 0 by a battle is not refreshed by `MAIN.ADV:56`.
