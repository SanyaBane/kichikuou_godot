# KichikuouGodot

A faithful port of **Kichikuou Rance** (AliceSoft, 1996, System 3.x) onto our own engine with a
**Sengoku Rance**–style interface, built in **Godot 4 + C#**. The owner directs; the agent writes
almost all code.

**Read [docs/project-brief.md](docs/project-brief.md) first** — it holds the decisions, the
reasoning, rejected alternatives, unverified facts about the source game, and the next step.

## Status

Spike surface analysis done — see [docs/spike-findings.md](docs/spike-findings.md) for
measurements, data model, local paths of the game and tools. Porting strategy decided (hybrid,
see "Next step" in the brief). The variable dictionary [docs/variables.tsv](docs/variables.tsv)
is done; when reading the scenario, use the renamed copy (`tools/analysis/rename_vars.py`) and
keep the dictionary updated when a meaning is confirmed or corrected.

The **battle** (`FIGT*.ADV`) is ported to `src/Kichikuou.Core/Combat` following
[docs/porting-conventions.md](docs/porting-conventions.md) and the spec in
[docs/battle/](docs/battle/README.md); it has been reviewed against the script and has unit and
end-to-end tests. The golden-test harness is built ([docs/golden-tests.md](docs/golden-tests.md)):
a patched xsystem35 (`F:\sources\Rance\xsystem35-sdl2`, branch `golden-harness`, built with
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
so `Cﾒｲｷｭｳ` is ported whole; `Iｶﾐ` runs from the player's files (not run end to end yet). Step 7
(`--auto` through stage 2, the owner's stage-2 captures) is the owner's call.
`tools/analysis/port_draft.py` drafts text-heavy ranges for hand ports.
**Agent screen dumps mask the event CGs** (`GamePresenter.MaskEventCgs`, on with `--shots` and
`KICHIKUOU_SHOTS_DIR`); never look at unmasked ones.

## Layout

- `src/Kichikuou.Core` — engine-independent rules library (`net10.0`, warnings are errors).
  `Sys3/` holds exact reproductions of System 3 / xsystem35 semantics: `Sys3Int` (16-bit
  saturating arithmetic — use it for every value that comes from the script),
  `Mt19937Legacy` + `XSystem35Random` (`Y 4,n`), `IScriptRandom` for injecting recorded draws,
  and `ScriptMemory` (variables, array pages, strings in the original engine's layout).
  `State/GameState` is the typed view over it: `GameState.g.cs` is **generated** by
  `python tools/codegen/gen_state.py` from `docs/variables.tsv` — never edit it by hand;
  re-run the generator after changing the dictionary. Rule code addresses records explicitly
  (`state.Units[state.Unit_Cur].Troops`) to keep the script's cursor quirks visible.
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
  every text; `KICHIKUOU_SCENARIO_DIR` / `KICHIKUOU_EXTRACT_DIR` / `KICHIKUOU_GAME_DIR`
  override the local folders).
- `tests/Kichikuou.Core.Tests` — xUnit tests for the core. `Golden/GoldenBattleTests` replays the
  captures in `E:\Modding games\Modding Rance\Kichikou\_extract\golden` (or
  `KICHIKUOU_GOLDEN_DIR`) and is skipped when there are none.
- `godot/` — Godot 4.7.2 .NET project (`Kichikuou.csproj`, namespace `Kichikuou.Game`);
  presentation only: `Main` is the title (New game, Load, Quit, the capture viewer) and reads the
  development switches; `Strategy/GameScreen` runs one game (`GameSession` on a `GamePresenter`;
  mouse and keys are a state, `Strategy/PlayerInput`, that `IM`, `Y 3`, `IK 0` and the menus poll
  once a frame as xsystem35 does — the map and the pickers decide clicks and drags themselves;
  `PointerScript` gestures only for `--auto`; `GameScreen.MenuBar` is xsystem35's menu bar —
  Restart, MsgSkip on `Core/Script/MessageSkip`, docs/window-menu.md; `Strategy/EngineState` keeps
  the random generator and the message skip for the whole launch); `Battles/CaptureList` picks a capture, `Battles/BattleScreen` shows its battle;
  `GodotGlyphRenderer` (MS Gothic via TextServer), `BattleAudio`; `Screens` switches screens. Solution `Kichikuou.sln` at the root (`dotnet/project/solution_directory`).
- `tools/analysis/` — Python scripts for reading the decompiled scenario; `tools/codegen/` —
  generators for committed C# sources; `tools/golden/` — harness launcher and page classes.
- `docs/` — brief, spike findings, variable dictionary; `window-menu.md` records
  xsystem35's window menu bar (our reference; File/Screen/Option/MsgSkip) and what its commands do.

## Commands

- Tests: `dotnet test tests/Kichikuou.Core.Tests` and `dotnet test tests/Kichikuou.Content.Tests`.
  The content tests' differential tests (a port against its file run from the decompile on random
  states) run an eighth of their random cases by default — the quick run, for checks while working
  (`DifferentialRuns`). **Before every commit that changes code, run the full counts**:
  `KICHIKUOU_FULL_TESTS=1 dotnet test tests/Kichikuou.Content.Tests` (only the full run fails a test
  whose random states never reached a path it lists). Report which run a result comes from.
- Build everything: `dotnet build Kichikuou.sln`
- Run the game headless (smoke test): `<Godot console exe> --headless --path godot --quit-after 30`;
  the editor path is in `docs/spike-findings.md`. Commit the `*.uid` files Godot creates.
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
- **Verify before relying.** Facts marked "unverified" in the brief are hypotheses; confirm them
  and update the brief.
- `F:\sources\Rance\alice-tools` is a read-only reference; do not modify it.
- **Name records.** The owner cannot tell provinces, units or commanders apart by number: in docs
  and replies never write a bare record ID — name first, ID after it: "Oaks (6)", "Oku (3), South
  (4) or Baranchi (7)", "Kinkaid (unit 9)", "Ruberan (commander 31)"; the same for dungeons ("the
  Monsters' Cave (dungeon 2)"). Provinces: the `MS 8` strings of `SET_MAP.ADV` (record `VAR0158`);
  units and commanders: `MS 1` / `MS 4` of `SET_CHR.ADV` (records `Unit_Cur` / `Cmdr_Cur`; some
  scenes rename a record at run time — `MM Unit_StrBase…` / `MM Cmdr_StrBase…` — so check the
  context); dungeons: docs/strategy/survey-dungeon.md §2.1.
- IDE is JetBrains Rider.
