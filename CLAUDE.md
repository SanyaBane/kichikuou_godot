# KichikuouGodot

A faithful port of **Kichikuou Rance** (AliceSoft, 1996, System 3.x) onto our own engine with a
**Sengoku Rance**–style interface, built in **Godot 4 + C#**. The owner directs; the agent writes
almost all code.

**Read [docs/project-brief.md](docs/project-brief.md) first** — it holds the decisions, the
reasoning, rejected alternatives and unverified facts about the source game.

**Backlog: [docs/backlog.md](docs/backlog.md)** holds everything put off (release, saves, captures,
interface, the surveys' open questions, tooling). Whenever work is put off — by the owner or by
you — add it there with the date and a link to its details; remove an item when it is done or taken
into a milestone. Keep it in English, like everything in the repository.

## Status

Spike surface analysis done — see [docs/spike-findings.md](docs/spike-findings.md) for
measurements, data model, the local layout of the game and tools. Porting strategy decided (hybrid,
see "Porting strategy" in the brief). The variable dictionary [docs/variables.tsv](docs/variables.tsv)
is done; when reading the scenario, use the renamed copy (`tools/analysis/rename_vars.py`) and
keep the dictionary updated when a meaning is confirmed or corrected.

The **battle** (`FIGT*.ADV`) is ported to `src/Kichikuou.Core/Combat` following
[docs/porting-conventions.md](docs/porting-conventions.md) and the spec in
[docs/battle/](docs/battle/README.md); it has been reviewed against the script and has unit and
end-to-end tests. The golden-test harness is built ([docs/golden-tests.md](docs/golden-tests.md)):
a patched xsystem35 (the checkout `KICHIKUOU_XSYSTEM35_DIR`, branch `golden-harness`, built with
MSYS2 — see `docs/spike-findings.md`) records battles, `src/Kichikuou.Core/Golden` replays them.
37 captured battles match; the battle screen reproduces the original layout at 1280×800.

Now: the **strategic layer** (week loop, commands, nation turns, data, scenes). Surveys, parts
and the owner's decisions are in [docs/strategy/](docs/strategy/README.md); milestone 1 is the
whole prologue, playable. Record data and scenes are read at run time from the player's files.
F0 is done: the shared services (message windows, map, sound, scene calls) live in
`src/Kichikuou.Core/Script`. E1 and E2 are done: every scene file reads as a goto program
(`ScriptProgram`) and `SceneRunner` runs it as the context's story hooks. W1 is done: `COM.ADV`
(weekly upkeep and story triggers) is `Strategy/WeeklyEvents`. W3 is under way: `COMMAND.ADV`
(the player phase) is `Strategy/CommandPhase`, reading the pointer as the script does
(`ISceneInput`, `Script/PointerScript` for gesture-level UIs), and the prologue paths of the
command files `Cﾃｷﾄｼ`, `Cﾃｷﾄｼ2`, `Cﾌﾞｶ`, `Cﾊｰﾚﾑ`, `Cﾒｲｷｭｳ` are ported (their later paths throw
`NotSupportedException`; `Cﾃｷﾄｼ`'s attack, `Cﾌﾞｶ`'s troop menu and the rest of `Cﾊｰﾚﾑ` came with milestone 2), and so are the pickers `Sﾌﾞﾀｲ`, `Sﾌﾞｶ`, `Sﾊｰﾚﾑ`
(`Strategy/*Screen.cs` on `PickerScreen`), all routed by `Strategy/StrategyRules`. W2 is done:
`MAIN.ADV` (the week loop) is `Strategy/WeekLoop`, the thieves' week and the Leazas week with
every nation phase (the `T*` files are calls through the router). The battle
runs in the router too (`FIGT_A`/`FIGT_B`/`FIGT`/`死亡捕獲`), so the game's input is an
`Strategy/IGameInput` (battle + scenes; owner's decisions 6-7 in `docs/strategy/README.md`).
G0 (the Godot screens, owner's decisions 8-12) is done: part 1 ported `ﾛｰﾄﾞｾｰﾌﾞ`
(`Strategy/LoadSaveScreen`, the save commands in `Script/ScriptContext.Save.cs` on an
`ISaveStore`, `Script/SaveData.cs`); part 2 is the game driver `Strategy/GameSession` (boot,
new game, load from the title or `COMMAND`, resume after the autosave, `&OVER` as `GameEnd`)
and the view/input proxies; part 3 is the presenter `Kichikuou.Content/Strategy/GamePresenter`
(decision 12: the scenes' drawing runs on the engine surface, map, windows, menus and pickers are
drawn by hand over it from the semantic `ScreenEvent`s and GameState); part 4 is the title and
`godot/Strategy/GameScreen` — the whole prologue is playable in Godot up to the Leazas week, which
is not ported. The owner's prologue capture run (2026-10-09, docs/golden-tests.md, "Findings so
far") matches the port everywhere, and so do two more runs: top-left map clicks (STR-12 confirmed) and
path A's week-23 duel (EVT-08 confirmed); README open items 1-2 are answered.
**Milestone 2** (owner's decision 13, 2026-10-10) is story stage 1 as a whole — the Leazas week,
the rebels, every command a stage-1 player has, the dungeon search — in the order of
docs/strategy/README.md, "Milestone 2" (with the sweep of what stage 1 reaches). Steps 0-3 are
done: every stage-1 file and every scene is a capture root, docs/golden-tests.md has the owner's
stage-1 checklist, the Leazas week is ported (the W2 row), and so are the nation template with
the rebels' turn `TCﾘｰｻﾞｽ` and their defender picker `TCﾘｰｻﾞｽ2` (the N1 row), and the attack in
`Cﾃｷﾄｼ` (`EnemyCityCommand`, the P3 row; the independent cities still throw). Step 4 is done:
`Cﾌﾞｶ`'s troop menu and the whole `Cﾊｰﾚﾑ` are ported (`SubordinatesCommand`, `HaremCommand`, both
files whole; the P4-P7 row). Step 5 is done: `Cｼｮｳﾆﾝ` and `Cｼﾊｲﾄｼ` are ported whole (`MerchantCommand`,
`OwnCityCommand`). Step 6 is done: `Cﾆﾝｼﾞｬ` and `Cﾃｲﾄ` are ported whole (`NinjaCommand`,
`ConstructionCommand`). Step 7 is done: `Cﾀｶﾗ`, `Sｱｲﾃﾑ` and `各種DATA` are ported whole
(`TreasureCommand`, `ItemScreen` on `PickerScreen`, `AllDataCommand`; semantic events
`PickerKind.Items`, `DataScreenEvent`) and their screens are drawn by hand (`PickerView`, the
presenter's All Data layer). Step 8 is done: `Cﾒｲｷｭｳ` is ported but God's Door (`DungeonCommand`, its
scenes in `DungeonCommand.Scenes.cs`; the boss and Hanny setters are record data run from the player's
files through `IScenarioSource.ReadBlock`; God's Door came with milestone 3 step 6). Step 9:
`--auto` plays the whole stage to stage 2 (`--seed 2`: Oaks in week 34, `Iﾏｼﾞﾝ` 1, stop in week 36,
nothing unported met). **Milestone 2 is closed** (2026-10-10): the owner's three stage-1 capture
runs, the end of the stage included, match the port whole (all 885 captures in the folder, none
reaching an unported path). **Milestone 3** (owner's decision 14, 2026-10-10) is story stage 2 as
a whole, up to the gate attack that starts stage 3: the sweep is docs/strategy/stage-2-sweep.md,
the order docs/strategy/README.md, "Milestone 3" (Helman, Zeth, the gate, the independent cities,
JAPAN, God's Door), the owner's capture checklist docs/golden-tests.md, "Capturing stage 2".
Step 1 (Helman) is done: `Tﾍﾙﾏﾝ2`, `Tﾍﾙﾏﾝ` and `Tﾊﾟｯﾄﾝ` are ported (`Strategy/HelmanDefenders`,
`Strategy/HelmanTurn`, `Strategy/PattonTurn`). Step 2 (Zeth) is done: `Tｾﾞｽ2` and `Tｾﾞｽ` are
ported (`Strategy/ZethDefenders`, `Strategy/ZethTurn`). Step 3 (the gate) is done: `Tｹｲﾌﾞﾘｽ2` is
ported (`Strategy/KayblisDefenders`). Step 4 (the independent cities, P4, `Cﾃｷﾄｼ.ADV:429-3828`) is
done, so `Cﾃｷﾄｼ` is ported whole: the cities' exits, the menu and Use Force are in
`Strategy/EnemyCityCommand.Cities.cs`, the negotiations (`:604-3648`) in `Strategy/EnemyCityCommand.Talks.cs`.
Step 5 (JAPAN) is done: `TJAPAN2` is ported (`Strategy/JapanDefenders`), and so is `TJAPAN` whole —
its rules in `Strategy/JapanTurn`, Gekkou's and Shinobu's attempts (`:295-2744`, ending 4) in
`Strategy/JapanTurn.Gekkou.cs`. Step 6 (God's Door, D2) is done: `Strategy/DungeonCommand.GodsDoor.cs`,
so `Cﾒｲｷｭｳ` is ported whole; `Iｶﾐ` runs from the player's files (not run end to end yet).
**Milestone 3 is closed** (2026-10-11, owner's decision 15): no `--auto` through stage 2, the stage-2
captures put off. `FIGTATTK` (the battle's animation, which the scene `FIGT表示` calls) is routed to the
battle's port. **Milestone 4** is story stage 3 as a whole: the sweep is docs/strategy/stage-3-sweep.md,
the order docs/strategy/README.md, "Milestone 4". Step 1 is done: `Tｹｲﾌﾞﾘｽ`'s frame (week S, the
recoveries, the event chain, the dispatch, the Grief Valley guards, the capture with Helman's and Zeth's
collapses) is `Strategy/KayblisTurn`. Step 2 is done: the generals' units (`:729-1328`: Babolat, Redeye,
Kite, Galtia, Kesselring) are `Strategy/KayblisTurn.Generals.cs`. Step 3 is done: Piar's unit
(`:670-728`) and Kayblis's army at war (`:1329-1508`) are `Strategy/KayblisTurn.Armies.cs`. Step 5 is
done: the Assault Leaders (`:326-624`) are `Strategy/KayblisTurn.Assault.cs`, so `Tｹｲﾌﾞﾘｽ` is ported
whole. Step 6 is done: `StageThreeEndTests` runs Kayblis's death (`死亡捕獲` → `Gｹｲﾌﾞﾘｽ` 10 →
`Iｼﾚｲﾌﾞ` 7 from the player's files) to stage 4 or ending 8. **Milestone 4 is closed** (2026-10-11,
owner's decision 16: no stage-3 capture, no `--auto` through stage 3). **Milestone 5** is story stage 4
(the Creator phase `Tﾙﾄﾞﾗ` and on to the game's end), a sweep first (docs/strategy/stage-4-sweep.md;
docs/strategy/README.md, "Milestone 5"). Step 1 is done: `Tﾙﾄﾞﾗ` is `Strategy/CreatorTurn` (on
`NationFile`, using none of the template), so N1 is done and nothing stage 4 reaches is left unported
but `OVER` (`GameEnd`). Owner's decision 17: `Iｶﾐ` gets a survey of its own before step 2's end tests,
and W4 (`OVER`, `ｽﾀMENU`) stays out of milestone 5 — every ending stays `GameEnd`. Step 2a is done
(docs/strategy/survey-ikami.md); step 2 is done: `IkamiEndTests` (content) runs `Iｶﾐ` end to
end from the player's files to endings 9, 10, 11 and 13, and `GodsDoorEndTests` the way there from
Kayblis's death — Kou at 8 when `Gﾙﾄﾞﾗ` plays, Ho Raga's Bonsai Statue, the four statues at God's Door,
`Iｶﾐ` to ending 11; Kou below 8: Ho Raga refuses and the door stays shut (`EndGameRig.cs` is their rig).
Step 3 (docs) is done: the quirks are registered (EVT-10 to EVT-20, NAT-31, DGN-19). **Milestone 5 is
closed** (2026-10-11, owner's decision 18: no stage-4 capture, no `--auto` through stage 4).
**Milestone 6** is W4 — `OVER` (the endings) and `ｽﾀMENU` (the start menu, with `HAPPY_C`), which
replace the minimal "THE END" of decision 10; a survey first (docs/strategy/README.md, "Milestone 6").
Step 0 is done (docs/strategy/survey-w4.md). Owner's decision 19: the three files run from the
player's files through `SceneRunner` (not hand-ported), after `ZZ0` back to the title, `ｽﾀMENU`
replaces our title; the gallery `CG` and the music room `MMODE` later. Step 1 is done: `OVER` runs
from the player's files (`OriginalVariant.IsSceneFile`; `ZZ0, 0` is `GameQuitException`, which
`GameSession` turns into `GameEnd`; Godot then goes back to the title), tested by `OverTests`.
Step 2 is done: `ｽﾀMENU` (with its logo `ﾌﾟﾛﾛｰｸﾞ`) is the title, run from the player's files by
`GameSession.TitleAsync` after the boot (which now keeps `ｽﾀｰﾄ.ADV:858`'s `Y 3, 10000`): New game,
Load, `Iｱﾘｽ`, `Gｻｲﾄｳ`, the requests slip and the debug starts through it, the gallery and the music
room unported (`TitleTests`); Godot's `Main` opens it, the capture viewer is in the Debug menu.
Step 3 is done: `HAPPY_C` (Fortune Check, the title's button 0) runs from the player's files
(`FortuneCheckTests`). Step 4 is done: the frame's quirks are registered (STR-25 to STR-42, STR-13).
**Milestone 6 is closed** (2026-10-11, owner's decision 20: no capture of the frame). **Milestone 7**
is the gallery `CG` and the music room `MMODE`, the last files the game reaches that do not run
(docs/strategy/README.md, "Milestone 7"). Step 0 is done (docs/strategy/survey-gallery.md): the
gallery reads the engine's CG load counter (`ZG Glob_CgSeen`), which the port does not count yet;
EVT-07 does not hold. Owner's decision 21: the port counts every CG it shows on every path into
`Glob_CgSeen`; `F1` past its string table reads an empty string. Step 1 is done: the boot's `ZG`
binds the counter and `ScriptContext.CountCgLoad` counts each load (STR-43) — every `G` of a scene or
a hand port through `ScriptContext.EngineAsync`, and where `WAO_SUB`, the map and the battle load
CGs they draw through their own events. Step 2 is done: the gallery `CG` runs from the player's files
(`OriginalVariant.IsSceneFile`, `GalleryTests`) and the agent's dumps mask it from its sheets to the
title's background (`GamePresenter.MaskEventCgs`). Step 3 is done: the music room `MMODE` runs from
the player's files (`MusicRoomTests`); `F1` past its string table reads an empty string (STR-44).
Step 4 is done: the gallery's and the music room's quirks are STR-43 to STR-57, EVT-07 is withdrawn.
Every file the game reaches now runs but the dead `XSPEC`. **Milestone 7 is closed** (2026-10-11,
owner's decision 22: no capture). Decision 22's next step is done: the absolute paths of the owner's
machine left the repository for `local.env`, which git ignores ("Local paths" below). Decision 23:
the project is GPL-2.0-or-later (`LICENSE`; README, "License").
Next (owner's requests 2026-10-11/12): an interface of our own next to the original layout, chosen
by the player, and an Android build later (docs/backlog.md). The game logic stays the original's.
The UI survey's parts 1-4 are docs/ui/survey.md; the design waits for the owner.
**Now: the variant split** (decision 24, 2026-10-12): a shared engine and one rule set per game variant,
the original first and alone — the plan, its steps and what is deferred are docs/variants.md. The Sparrow
Mod survey (docs/mods/sparrow-mod.md) is why; its port waits for its author's permission.
`tools/analysis/port_draft.py` drafts text-heavy ranges for hand ports.
**Agent screen dumps mask the event CGs** (`GamePresenter.MaskEventCgs`, on with `--shots` and
`KICHIKUOU_SHOTS_DIR`); never look at unmasked ones.

## Layout

- `src/Kichikuou.Core` — engine-independent rules library (`net10.0`, warnings are errors);
  `LocalPaths` (no rule) reads the developer's local paths ("Local paths" below).
  `Sys3/` holds exact reproductions of System 3 / xsystem35 semantics: `Sys3Int` (16-bit
  saturating arithmetic — use it for every value that comes from the script),
  `Mt19937Legacy` + `XSystem35Random` (`Y 4,n`), `IScriptRandom` for injecting recorded draws,
  and `ScriptMemory` (variables, array pages, strings in the original engine's layout).
  `State/GameState` is the typed view over it: `GameState.g.cs` is **generated** by
  `python tools/codegen/gen_state.py` from `docs/variables.tsv` — never edit it by hand;
  re-run the generator after changing the dictionary. Rule code addresses records explicitly
  (`state.Units[state.Unit_Cur].Troops`) to keep the script's cursor quirks visible.
  `GameState` derives from `Script/ScriptState` (what the runtime sees: the memory, `RetVal`, the
  `DS` bindings) and carries the original's `Script/VariantProfile` (`State/OriginalVariant`: the
  facts the runtime takes from a variant — variable count, scene rule, saved pages; docs/variants.md).
  `Golden/` parses harness captures and replays them through the port (`GoldenReplay`);
  `State/VariableCatalog.g.cs` (names, groups, page bindings) is generated with `GameState.g.cs`.
  `Script/` holds what every ported part shares: `ScriptContext` (state, draws, pauses, scene
  calls) with the helper files `WAO_SUB` (message windows), `M`/`WAO_MAP` (map) and `PCMPLAY`
  (sound), the shared events (`ScriptEvent`) and the view/input/hook interfaces, plus
  `ScriptStatements` — scenario code read at run time: a goto program that mirrors the
  decompile line for line (`ScriptProgram`, one statement per engine command, jumps resolved,
  no idiom rewritten); `RunAsync` runs the straight-line `SET_*` data files and `SceneRunner`
  runs scene programs (`ISceneInput` adds menus, mouse and clock to the inputs). `Setup/GameSetup` is the boot (`SET.ADV`) and the new game.
  `Strategy/` holds the strategic rule files: `WeekLoop` is `MAIN.ADV`, `WeeklyEvents` is `COM.ADV`, `CommandPhase` is
  `COMMAND.ADV`, `LoadSaveScreen` is `ﾛｰﾄﾞｾｰﾌﾞ.ADV`, the `*Command` classes (on `CommandFile`) are the `C*.ADV` command files, the
  `*Screen` classes (on `PickerScreen`) their `S*.ADV` pickers (`ItemScreen` is `Sｱｲﾃﾑ`), `AllDataCommand` is `各種DATA.ADV`, the nation files sit on `NationFile` (the
  template every `T*`/`T*2` repeats: priority list, picker, filler, troop sum, target walk; the
  army picker itself is on `CommandFile`, since `Cﾃｷﾄｼ` has a copy) as
  `*Turn` (`T*.ADV`, e.g. `RebelTurn`) and `*Defenders` (`T*2.ADV`), and `StrategyRules` routes calls of these files to their ports (the rule hooks of a `SceneRunner`).
- `src/Kichikuou.Content` — engine-independent access to the player's own game data (never
  part of the repository): `ScenarioText` reads message texts from the decompiled scenario by
  `ScriptRef`; `Scripts/ScenarioScripts` reads the data files' statements and whole files as programs
  (`ScenarioParser`; strictly: anything unexpected fails with its location); `Graphics/` decodes the CGs keeping palette indices (`IndexedPng`, `Pms`,
  `CgLibrary`, `Palette` with the engine's reserved entries; `ToRgba(40)` = `G n, 40`);
  `Audio/` maps sound and music numbers to files. `Screen/` emulates the engine's drawing
  surface (3200×800 at 2×, 8-bit indexed as the engine's: a palette load recolours what is
  drawn; `G`/`CC`/`CS`/`CF`/`CM`, `WV`), message text and selection windows;
  `Battles/BattlePresenter` draws every core event the way the original does
  ([docs/battle/screen-layout.md](docs/battle/screen-layout.md)), `FigtSelScreen` /
  `BattleMenuScreen` are the mouse-driven input screens. `Strategy/GamePresenter` draws a whole
  game: the stage (`Battle` shares it) and the hand-drawn layers (`TextBox`, `PickerView`, menus,
  All Data in `GamePresenter.Data.cs`) composed into `Screen`; a UI asks it what is open (`MapWaiting`, `Picker`, `Data`) and where a menu
  item is (`MenuHitTest`); a gesture UI turns clicks into the script's inputs with
  `PointerScript` (`Picker.HitTest`). Tests in
  `tests/Kichikuou.Content.Tests` (the golden part draws and plays every capture and resolves
  every text; its folders are the local paths, "Local paths" below).
- `tests/Kichikuou.Core.Tests` — xUnit tests for the core. `Golden/GoldenBattleTests` replays the
  captures in `_extract\golden` (`KICHIKUOU_GOLDEN_DIR`) and is skipped when there are none.
- `godot/` — Godot 4.7.2 .NET project (`Kichikuou.csproj`, namespace `Kichikuou.Game`);
  presentation only: `Main` opens the title (a `GameScreen` running `ｽﾀMENU`) or what the
  development switches ask for; `Strategy/GameScreen` runs one game from the boot (`GameSession` on a `GamePresenter`;
  mouse and keys are a state, `Strategy/PlayerInput`, that `IM`, `Y 3`, `IK 0` and the menus poll
  once a frame as xsystem35 does — the map and the pickers decide clicks and drags themselves;
  `PointerScript` gestures only for `--auto`; `GameScreen.MenuBar` is xsystem35's menu bar —
  Restart, MsgSkip on `Core/Script/MessageSkip` (it skips the opening whole, `OriginalVariant.WholeScenes`, `Core/Script/EngineTimer`),
  docs/window-menu.md, plus our Debug menu; `Strategy/EngineState` keeps
  the random generator and the message skip for the whole launch); `Battles/CaptureList` (Debug → Battle Captures) picks a capture, `Battles/BattleScreen` shows its battle;
  `GodotGlyphRenderer` (TextServer; MS Gothic or the bundled BIZ UDGothic in `godot/fonts`, Option →
  Fonts, `--font ms-gothic|biz-ud-gothic` for one launch), `BattleAudio`; `Screens` switches screens. Solution `Kichikuou.sln` at the root (`dotnet/project/solution_directory`).
- `tools/analysis/` — Python scripts for reading the decompiled scenario; `tools/codegen/` —
  generators for committed C# sources; `tools/golden/` — harness launcher and page classes.
- `docs/` — brief, spike findings, variable dictionary; `window-menu.md` records
  xsystem35's window menu bar (our reference; File/Screen/Option/MsgSkip) and what its commands do;
  `ui/survey.md` surveys the screens and input for our own interface.

## Local paths

The paths of the owner's machine never enter the repository: they live in **`local.env`** at the
root, which git ignores (`KEY=value` lines; the committed template `local.env.example` lists every
key; an environment variable of the same name overrides a line). **Read `local.env` first** whenever
a command or a doc needs a folder or a tool: `KICHIKUOU_GAME_DIR` (the game), `KICHIKUOU_EXTRACT_DIR`
(the extract, `_extract` in the docs: `scenario`, `scenario_named`, `cg_png`, `cg_pms`, `wav`,
`golden`), `KICHIKUOU_GODOT_EXE` (the Godot console exe), `KICHIKUOU_XSYSTEM35_DIR`,
`KICHIKUOU_MSYS2_DIR`, `KICHIKUOU_ALICE_TOOLS_DIR` and the spike's tools (docs/spike-findings.md,
"Local layout"). `Kichikuou.Core.LocalPaths` reads it for the tests and the Godot game's default
folders, `tools/golden/capture.ps1` for the harness. Docs and code name a key, never a path; never
commit `local.env`.

## Commands

- Tests: `dotnet test tests/Kichikuou.Core.Tests` and `dotnet test tests/Kichikuou.Content.Tests`.
  The content tests' differential tests (a port against its file run from the decompile on random
  states) run an eighth of their random cases by default — the quick run, for checks while working
  (`DifferentialRuns`). The full counts, `KICHIKUOU_FULL_TESTS=1 dotnet test tests/Kichikuou.Content.Tests`
  (~5 min; only the full run fails a test whose random states never reached a path it lists), are
  **not for every commit — only when the change could break something they cover**: the rule code,
  the script runtime, the content readers, the presenter. A change no content test reaches (docs,
  comments, Godot-only code, a list the content tests do not read) needs the build, the core tests
  and the targeted tests only. Judge by what the diff touches and say why in the reply; when the full
  run is due, run it once, right before the commit, not while the owner may still ask for changes
  (owner's request 2026-10-11). Report which run a result comes from.
- Build everything: `dotnet build Kichikuou.sln`
- Run the game headless (smoke test): `<Godot console exe> --headless --path godot --quit-after 30`;
  `<Godot console exe>` is `KICHIKUOU_GODOT_EXE` in `local.env` (the editor's exe is next to it).
  Commit the `*.uid` files Godot creates.
- Watch a capture headless and save the screen at every key wait and long pause:
  `<Godot console exe> --headless --path godot -- --watch <capture> --fast --quit --shots <dir>`.
- Play the game unattended headless (slots in `user://saves-auto`; `--load` resumes them):
  `<Godot console exe> --headless --path godot -- --newgame --auto --fast --quit [--seed n] [--shots <dir>] [--weeks n] [--stage n]`;
  one visit each to Harem, Subordinates and the hideout in the prologue and to Treasure and All Data
  at stage 1 (after one search of the Monsters' Cave with the weakest unit: down to a battle, Retreat,
  Finish); from stage 1 once a week a replenish (`Cﾌﾞｶ`) and an attack on the rebels (from their
  second week, while Rance has 3/4 of his troops), defences with the strongest; Turn END every week;
  ends at the first file or path not ported yet ("not ported yet"), at a game over, after week 30
  (`--weeks n`) or two weeks after story stage `--stage n` is reached — `--seed 2 --weeks 60 --stage 2`
  takes Oaks in week 34 and stops in week 36, ~15 min (exit code 0; 1 after an error).
- Show an ending's `OVER` (its picture, staff roll, Completion Report) on a booted game:
  `<Godot console exe> --headless --path godot -- --ending <1-14> --auto --fast --quit [--shots <dir>]`
  (without `--auto` the keys wait for the player; slots in `user://saves-ending`).

## Rules

- **Faithful port first.** Reproduce the original's mechanics and balance one-to-one; modernise
  only the interface. Do not "improve" rules. A Sengoku-mechanics hybrid comes later, on a
  separate branch, only after the full port.
- **Register every quirk.** Bugs and oddities of the original are reproduced as is and each one is
  recorded in [docs/quirks.md](docs/quirks.md) (stable ID, cited as `// QUIRK: <ID>`), so some can
  be offered as optional fixes later.
- **Engine-independent core.** Game rules live in `Kichikuou.Core`, a .NET class library with no
  Godot references, covered by unit tests. The Godot project only renders state and forwards input.
  Never put rules in UI code.
- **Godot 4 only.** Use Godot 4 C# APIs; never Godot 3 idioms (`yield`, old method/signal names).
  When unsure about an API, check the docs for the installed version.
- **Never commit original game assets or extracted data** — the player supplies their own files.
- **License: GPL-2.0-or-later** ([LICENSE](LICENSE), decision 23). Code translated from a
  third-party source (xsystem35's C, `mt19937-1.c`, …) gets a header naming the file and its
  copyright holders, as `Sys3/Mt19937Legacy.cs` and `Graphics/Pms.cs` have; reproducing behaviour
  needs none. Never copy code whose license is not GPL-2.0-or-later compatible.
- **Verify before relying.** Facts marked "unverified" in the brief are hypotheses; confirm them
  and update the brief.
- The alice-tools source (`KICHIKUOU_ALICE_TOOLS_DIR`) is a read-only reference; do not modify it.
- **Name records.** The owner cannot tell provinces, units or commanders apart by number: in docs
  and replies never write a bare record ID — name first, ID after it: "Oaks (6)", "Oku (3), South
  (4) or Baranchi (7)", "Kinkaid (unit 9)", "Ruberan (commander 31)"; the same for dungeons ("the
  Monsters' Cave (dungeon 2)"). Provinces: the `MS 8` strings of `SET_MAP.ADV` (record `VAR0158`);
  units and commanders: `MS 1` / `MS 4` of `SET_CHR.ADV` (records `Unit_Cur` / `Cmdr_Cur`; some
  scenes rename a record at run time — `MM Unit_StrBase…` / `MM Cmdr_StrBase…` — so check the
  context); dungeons: docs/strategy/survey-dungeon.md §2.1.
- IDE is JetBrains Rider.
