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

The **battle** (`FIGT*.ADV`) is ported to `src/Kichikuou.Original/Combat` following
[docs/porting-conventions.md](docs/porting-conventions.md) and the spec in
[docs/battle/](docs/battle/README.md); it has been reviewed against the script and has unit and
end-to-end tests. The golden-test harness is built ([docs/golden-tests.md](docs/golden-tests.md)):
a patched xsystem35 (the checkout `KICHIKUOU_XSYSTEM35_DIR`, branch `golden-harness`, built with
MSYS2 — see `docs/spike-findings.md`) records battles, `src/Kichikuou.Original/Golden` replays them.
37 captured battles match; the battle screen reproduces the original layout at 1280×800.

Now: the **strategic layer** (week loop, commands, nation turns, data, scenes). Surveys, parts
and the owner's decisions are in [docs/strategy/](docs/strategy/README.md); milestone 1 is the
whole prologue, playable. Record data and scenes are read at run time from the player's files.
F0 is done: the shared services (message windows, map, sound, scene calls) live in
`Script/` (the engine's runtime and the original's helper files). E1 and E2 are done: every scene file reads as a goto program
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
and the view/input proxies; part 3 is the presenter `Kichikuou.Original.Content/Strategy/GamePresenter`
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
The UI survey's parts 1-4 are docs/ui/survey.md. Decision 25 (2026-10-12): our own screens come one by
one over the original layout, chosen by Option → Interface; the first, the deployment table over
`Sﾌﾞﾀｲ`'s grid, is built with the rest of its screen and its third part (the party as a formation with
the units' faces, drag and drop; Debug → Portraits) — docs/ui/deployment-table.md. The battle screen
followed (docs/ui/battle-screen.md); next (owner, 2026-10-16) the map and command screen, its survey
and mock-ups in docs/ui/command-screen.md; then (owner, 2026-10-18) the messages of the turn's start,
surveyed in docs/ui/turn-start.md — the week report is built, the nations' banners put off; then (owner,
2026-10-18) the message windows in our lacquer, variant A, the battle's included — docs/ui/messages.md; its step 1
(the windows 111/112/121/122/113/123) and step 2 (window 131, the speaker on a plate) and step 3 (the `]` menus, `Strategy/SceneMenuView`, mock-up M1) and step 4 (the text typed letter by letter, Option → Text Speed…, mock-ups T1 and S2) are built, and so is step 5 (the battle's windows: those it opens off the field, and its info boxes 143/153 as the owner's mock-ups K3 and K1 — a defence's badge, a fall's stamp, the results plate, our panel; `Battles/BattleInfoBox`, `godot/Battles/BattleInfoLayer`), and so is step 6 (the message log, mock-up L3a: `Strategy/MessageLog` filled by `GamePresenter.Log`, the speakers by `Strategy/PortraitNames`, `godot/Strategy/MessageLogView` on the wheel up at a key wait, kept for the launch in `EngineState.Log`);
then (owner, 2026-10-18) our own Subordinates screen after mock-up F1 (no dialogue with Maris; the orders on our
buttons, answered through `SubordinateOrder`) — docs/ui/subordinates.md, built; its look (owner, 2026-10-18) is mock-up
G3, lacquer with the table a paper ledger, built, awaiting the owner's look in a window; then (owner, 2026-10-19) the
Harem and Treasure screens (`Sﾊｰﾚﾑ`, `Sｱｲﾃﾑ`) in the same lacquer — the survey and mock-ups are
docs/ui/pickers.md (`tools/ui/mock_pickers.py`): the harem is H2 with a Summon button, built (`godot/Strategy/HaremView`, `Original.Content/Strategy/HaremRoster`), looked at by the owner and changed after it; the treasure was S5 (Sengoku Rance's manner), built and not accepted (no item icons); after rounds T and U the owner chose V, `Sｱｲﾃﾑ`'s own screen in lacquer as the harem's, built in its place (`godot/Strategy/TreasureView`, `GameScreen.Treasure.cs`, the model `Original.Content/Strategy/TreasureRoster`: the picker's clicks); after the owner's look its refusals and questions are round W's W2, a callout at the clicked cell with the Yes/No inside (`godot/Strategy/TreasureCallout`), plus our own question before an item picked first replaces a character's (`TreasureRoster.ReplaceQuestion`), built and accepted by the owner (2026-10-19); then (owner, 2026-10-19) the command panel's Load/Save split into Load and Save rows, the file's Load / Save / Don't menu answered unseen (`Original.Content/Strategy/LoadSaveShortcut`; docs/ui/command-screen.md, "The command panel").
**The variant split is done** (decision 24, 2026-10-12): a shared engine and one rule set per game
variant, the original alone for now — the plan, its steps and what is deferred until a second variant
are docs/variants.md. The Sparrow Mod survey (docs/mods/sparrow-mod.md) is why; its port waits for its
author's permission.
`tools/analysis/port_draft.py` drafts text-heavy ranges for hand ports.
**Agent screen dumps mask the event CGs** (`GamePresenter.MaskEventCgs`, on with `--shots` and
`KICHIKUOU_SHOTS_DIR`); never look at unmasked ones.

## Layout

The code is a shared engine plus one rule set per game variant, the original alone for now
(docs/variants.md); the compiler keeps the layers: Engine ← Content, Engine ← Original,
Original + Content ← Original.Content ← Godot. All four libraries are `net10.0`, warnings are errors.

- `src/Kichikuou.Engine` — what works unchanged for any System 3.9 game, no rule of the original.
  `LocalPaths` reads the developer's local paths ("Local paths" below).
  `Sys3/` holds exact reproductions of System 3 / xsystem35 semantics: `Sys3Int` (16-bit
  saturating arithmetic — use it for every value that comes from the script),
  `Mt19937Legacy` + `XSystem35Random` (`Y 4,n`), `IScriptRandom` for injecting recorded draws,
  and `ScriptMemory` (variables, array pages, strings in the original engine's layout).
  `Script/` is the runtime: `ScriptState` (what the runtime sees: the memory, `RetVal`, the `DS`
  bindings, and `Extras`, the port's own data kept with the saves — no rule reads it), `VariantProfile` (the facts the runtime takes from a variant — variable count, scene
  rule, saved pages), `ScriptContext` (the engine's side: state, draws, pauses, scene calls, saves;
  helper calls go through the virtual `CallHelperAsync`), the shared events (`ScriptEvent`) and the
  view/input/hook interfaces, `SaveData` (the slot format, 2 since `Extras`; format 1 still loads), `MessageSkip`, `PointerScript` (the
  gesture queue), `IScenarioSource`, plus
  `ScriptStatements` — scenario code read at run time: a goto program that mirrors the
  decompile line for line (`ScriptProgram`, one statement per engine command, jumps resolved,
  no idiom rewritten); `RunAsync` runs the straight-line `SET_*` data files and `SceneRunner`
  runs scene programs (`ISceneInput` adds menus, mouse and clock to the inputs).
  `Golden/` is the harness's capture format (`GoldenCapture`, `MemoryDump`).
- `src/Kichikuou.Original` — the original game's rules (references the engine only).
  `State/GameState` is the typed view over the memory: `GameState.g.cs` is **generated** by
  `python tools/codegen/gen_state.py` from `docs/variables.tsv` — never edit it by hand;
  re-run the generator after changing the dictionary. Rule code addresses records explicitly
  (`state.Units[state.Unit_Cur].Troops`) to keep the script's cursor quirks visible.
  `GameState` derives from the engine's `ScriptState` and carries the original's profile
  (`State/OriginalVariant`); `State/VariableCatalog.g.cs` (names, groups, page bindings) is
  generated with `GameState.g.cs`. `Script/` holds the original's `OriginalScriptContext` on the
  engine's context with the helper files `WAO_SUB` (message windows), `M`/`WAO_MAP` (map) and
  `PCMPLAY` (sound), their events (`HelperEvents`), the semantic screen events (`ScreenEvents`)
  and the screens' gesture geometry (`GesturePoints`). `Golden/` replays harness captures through
  the port (`GoldenReplay`, `MemoryComparison`). `Combat/` is the battle. `Setup/GameSetup` is
  the boot (`SET.ADV`) and the new game.
  `Strategy/` holds the strategic rule files: `WeekLoop` is `MAIN.ADV`, `WeeklyEvents` is `COM.ADV`, `CommandPhase` is
  `COMMAND.ADV`, `LoadSaveScreen` is `ﾛｰﾄﾞｾｰﾌﾞ.ADV`, the `*Command` classes (on `CommandFile`) are the `C*.ADV` command files, the
  `*Screen` classes (on `PickerScreen`) their `S*.ADV` pickers (`ItemScreen` is `Sｱｲﾃﾑ`), `AllDataCommand` is `各種DATA.ADV`, the nation files sit on `NationFile` (the
  template every `T*`/`T*2` repeats: priority list, picker, filler, troop sum, target walk; the
  army picker itself is on `CommandFile`, since `Cﾃｷﾄｼ` has a copy) as
  `*Turn` (`T*.ADV`, e.g. `RebelTurn`) and `*Defenders` (`T*2.ADV`), and `StrategyRules` routes calls of these files to their ports (the rule hooks of a `SceneRunner`).
- `src/Kichikuou.Content` — engine-independent access to the player's own game data (never
  part of the repository; references the engine only): `ScenarioText` reads message texts from the decompiled scenario by
  `ScriptRef`; `Scripts/ScenarioScripts` reads the data files' statements and whole files as programs
  (`ScenarioParser`; strictly: anything unexpected fails with its location); `Graphics/` decodes the CGs keeping palette indices (`IndexedPng`, `Pms`,
  `CgLibrary`, `Palette` with the engine's reserved entries; `ToRgba(40)` = `G n, 40`);
  `Audio/MusicTable` maps music numbers to files. `Screen/` emulates the engine's drawing
  surface (3200×800 at 2×, 8-bit indexed as the engine's: a palette load recolours what is
  drawn), its drawing commands (`EngineStage`: `G`/`CC`/`CS`/`CF`/`CM`, `WV`, the fades, the text
  state; its owner is told of the drawing through `IStageObserver`), message text (`TextBox`) and selection windows.
- `src/Kichikuou.Original.Content` — the original's presenters (references Original and Content):
  `Audio/SoundTable` maps the original's sound numbers to files;
  `Battles/BattlePresenter` draws every core event the way the original does
  ([docs/battle/screen-layout.md](docs/battle/screen-layout.md)), `FigtSelScreen` /
  `BattleMenuScreen` are the mouse-driven input screens. `Strategy/GamePresenter` draws a whole
  game: the stage (`Battle` shares it) and the hand-drawn layers (`TextBox`, `PickerView`, menus,
  All Data in `GamePresenter.Data.cs`) composed into `Screen`; a UI asks it what is open (`MapWaiting`, `Picker`, `Data`) and where a menu
  item is (`MenuHitTest`); a gesture UI turns clicks into the script's inputs with
  `PointerScript` (`Picker.HitTest`).
- `tests/` — xUnit, one project per library: `Kichikuou.Engine.Tests` (Sys3, local paths, the
  timer, the message skip and the pauses, on a `TestState` of plain variables),
  `Kichikuou.Content.Tests` (graphics, scenario text), `Kichikuou.Original.Tests` (the rules, the
  battle, the engine's runtime on the original's state; `Golden/GoldenBattleTests` replays the
  captures in `_extract\golden` (`KICHIKUOU_GOLDEN_DIR`) and is skipped when there are none) and
  `Kichikuou.Original.Content.Tests` (everything that reads the player's files: the differential
  tests, the scenes, the presenters; the golden part draws and plays every capture and resolves every
  text; its folders are the local paths, "Local paths" below). `Kichikuou.TestSupport` (on the
  engine alone) holds the doubles the tests share: `OneProgram`, `LoggingView`, `LoggingRandom`,
  `UnexpectedInput`, the `ScribblingHooks` frame, `RandomStates`, `Differential` (state copy, log
  and memory differences) and the like.
- `godot/` — Godot 4.7.2 .NET project (`Kichikuou.csproj`, namespace `Kichikuou.Game`, references
  the four libraries; its build does not make warnings errors, so check it at run time);
  presentation only: `Main` opens the title (a `GameScreen` running `ｽﾀMENU`) or what the
  development switches ask for; `Strategy/GameScreen` runs one game from the boot (`GameSession` on a `GamePresenter`;
  mouse and keys are a state, `Strategy/PlayerInput`, that `IM`, `Y 3`, `IK 0` and the menus poll
  once a frame as xsystem35 does — the map and the pickers decide clicks and drags themselves;
  `PointerScript` gestures only for `--auto`; `GameScreen.MenuBar` is xsystem35's menu bar —
  Restart, MsgSkip on `Engine/Script/MessageSkip` (it skips the opening whole, `OriginalVariant.WholeScenes`, `Engine/Script/EngineTimer`),
  docs/window-menu.md, plus our Debug menu; `Strategy/EngineState` keeps
  the random generator and the message skip for the whole launch); `Battles/CaptureList` (Debug → Battle Captures) picks a capture, `Battles/BattleScreen` shows its battle;
  `Strategy/PortraitViewer` (Debug → Portraits: units', commanders' and every face as our screens cut them, `UnitFaces`, through `Strategy/FacePictures`, which the deployment screen's party cards share; the owner's edits of a square, `FaceCropEditor`, are kept in `user://face-crops.txt`, `FaceCropFile`; `--portraits` opens it);
  `Battles/Effects` (our exchange effects after Sengoku Rance's, drawn by code: `EffectTextures`, particles from a seed
  as `MeleeEffect`, `BowEffect`, `MagicEffect`, `RanceAttackEffect`, `KentarouSlashEffect` (Kentarou's Rance Attack), `WhiteBeamEffect` (the Black D. Beam: the same laid over dark, `EffectPlayer.Dark`; the beams each placed for its user: `ShizukaBeamEffect`, `MagicBeamEffect`, `EpoLightEffect`, `ChizukoBeamEffect`, `NagiBeamEffect`), `WhirlwindShotEffect`, `HighSpeedEffect` (By la Way: Rance Attack's in red, `RanceAttackEffect.BuildRed`), `NinjaEffect` (the ninja's follow-up), `VanishBombEffect`, `WarDanceEffect`, `ChuhaiEffect`, `GoliathEffect` (T2 Goliath's, 15 frames), `SoaringWaveEffect` (Rolex's T2 Soaring Wave, our own), drawn by `EffectCanvas` and laid over the scene by `EffectPlayer`; Debug → Effects, `EffectViewer` (a special over the sprite its user shows),
  `--effects` (`--effects white-beam-magic,black-beam-nagi --shots DIR` saves only those); played in the exchange from its shots by `BattleBoardView`, the choice `Original.Content/Battles/ExchangeEffect`, Option → Battle Screen… → "Effects" — docs/ui/battle-screen.md, "The exchange's effects");
  `Strategy/DeploymentTableView` and `Strategy/DeploymentPanels` (our own deployment screen, Option → Interface → New, `InterfaceChoice`;
  the owner's E1/E2 in lacquer, the Subordinates screen's layout: the ledger across the top, the hovered unit's picture
  panel (`FacePictures.PicturePanel`) below left, the party's 2×2 cards with one gold triangle right of them (the way the
  party faces), totals and bar below right — docs/ui/deployment-table.md,
  "The lacquer look"; `GameScreen` feeds the script its gestures, `Original.Content/Strategy/DeploymentTable` the rows);
  `Strategy/SubordinatesView` (our Subordinates screen over `Sﾌﾞｶ` and `Cﾌﾞｶ`'s troop menu, Option → Interface → New,
  docs/ui/subordinates.md: drawn by hand in lacquer (mock-up G3) over a wave-patterned backdrop — the table across the
  top, `DeploymentTableView` as a chooser and a paper ledger (`ledger: true`, not the palette's colours) with
  `Original.Content/Strategy/SubordinatesScreen.Columns` —, the status CG, the orders' panel; an order is
  `Original.Content/Strategy/SubordinateOrder`, fed by `GameScreen.Subordinates.cs`: the cell's click, the menus and key
  waits answered unseen, the command opened again on COMMAND's map after a troop order; the figures are
  `SubordinatesCommand.Offer`/`AddMore`/`Refill`);
  `Strategy/MapMarkersView` (our name plates over the map in `COMMAND`'s map wait, Option → Interface → New,
  docs/ui/command-screen.md, "The map's markers": the model `Original.Content/Strategy/MapMarkers`, a plate's click a
  still click at its marker's point, the camera's form `MapCameraForm`, placed at the view the screen shows,
  `GamePresenter.MapViewShown`; the dimmed ones hidden by Option → Hide Dimmed Map Names, `MapNamesChoice`;
  a hovered province's card right of its plate, `MapMarkers.CardOf`: its figures for ours and the provinces the
  player has seen — `GameState.KnownProvinces`, in `Extras`, marked at each map wait and by the ninja's scouting —
  else "??");
  `Strategy/ProvinceMenuView` (our province menu over a city file's first menu, Option → Interface → New, the owner's
  mock-up 4a in lacquer and gold: `Original.Content/Strategy/ProvinceMenu`, `GamePresenter.MenuCovered`,
  docs/ui/command-screen.md, "The province menu"; also the dungeon's menu after it);
  `Strategy/CommandMenuView` (our command panel over `COMMAND`'s right-click menu and its status window, Option →
  Interface → New, centred as the original's menu: `Original.Content/Strategy/CommandMenu`, Load/Save as two rows
  answered through `LoadSaveShortcut`, docs/ui/command-screen.md, "The command panel"); `Strategy/StatusStripView` (our status strip over the map's top while the command
  panel is open, in one row: the status window's lines and the week's uses in bracketed groups,
  `Original.Content/Strategy/StatusStrip`, "The status strip"); `Strategy/WeekReportView` (our week report over the
  week start's windows — the banner, the budget as an account, the troops replenished, a triangle asking for the click;
  Option → Interface → New, docs/ui/turn-start.md: `Original.Content/Strategy/WeekReport` gathered by
  `GamePresenter.CoverWeekReport`, the windows' waits answered at once, the player's click at their close,
  `GameScreen.WeekReportAsync`); `Strategy/MessageWindowView` (our message windows 111/112/121/122/113/123 and 131 where the
  original has them — speech with a tail, thought, narration, a place's title, 131 with its speaker on a plate — the text at its cells in the game's font,
  typed letter by letter (its speed Option → Text Speed…, `Strategy/TextSpeedView` over the picture with the game's 121 as the preview, `Strategy/TextSpeedChoice`; a click while it types shows it whole, `GameScreen.FinishTypingAsync`),
  the triangle while the player's key is awaited, a menu over it shown on top; Option → Interface → New, docs/ui/messages.md:
  `Original.Content/Strategy/MessageWindow` from `GamePresenter.MessageWindow` while `CoverMessages`, in a battle `BattlePresenter.MessageWindow`);
  `Strategy/SceneMenuView` (our `]` menus: lacquer rows at the selection box's place, `GamePresenter.MenuBoxArea`, while
  `SceneMenuCovered`; not over our battle screen; docs/ui/messages.md, §5 step 3);
  `Strategy/MessageLogView` (the message log, a paper chronicle, mock-up L3a, in either interface: the wheel up at a key wait
  opens it, the game waits; `Original.Content/Strategy/MessageLog` from `GamePresenter.Log` — each key wait's page, a
  dialogue question's answer, `MenuAnswered` (an event scene's menu or one after a speech, never the play's; the play's narration left out, `MessageLog.Keeps`) — the speakers by the portraits' plates, `PortraitNames`; kept for the launch,
  `EngineState.Log`; docs/ui/messages.md, §5 step 6); the six and the Subordinates screen draw with `Strategy/Lacquer` (the style's colours and parts, the ledger's, the click's triangle);
  `Battles/BattleBoardView` (our own battle screen, Option → Interface → New, docs/ui/battle-screen.md:
  `Original.Content/Battles/BattleScene` on top, the stage's windows over it through `WindowMask`, the
  cards and the round's order from `BattleBoard`/`RoundOrder`, the order strip `OrderStrip` moving on its own;
  the exchange's troops and HP counters at the scene's foot after Sengoku Rance's, `Battles/ExchangeCountersView` from
  `BattlePresenter.ExchangeCounters` (the cards run in their steps; Option → Battle Screen… → "Counters: run");
  `FIGT_SEL` answered by our own input, `BattleCommandLayer`: banners and context menus from
  `Original.Content/Battles/BattleCommands`; the retreat confirmation our dialog, `BattleDialogLayer`
  from `BattleDialog`; the speech bubbles' lines our balloon, `SpeechBalloon` from `BattlePresenter.Speech`; the info boxes 143/153 (a defence's badge, a fall's stamp, the results plate, our panel) `Battles/BattleInfoLayer` from `BattlePresenter.InfoBox` (docs/ui/messages.md, §5 step 5); the ninja's scout report, before the field, our overlay `Battles/ScoutReportView` from `BattlePresenter.Scout` (`Original.Content/Battles/ScoutReport`); a successful strategy, before the field, our cut-in `Battles/StrategyCutInView` from `BattlePresenter.Strategy` (`Original.Content/Battles/BattleStrategy`; one click, the strategist's line); the first round header our opening `Battles/BattleOpeningLayer` from `GamePresenter.Opening` (`Original.Content/Battles/BattleOpening`; the later headers answered at once; its fonts Shojumaru and Cinzel bundled in `godot/fonts`); the exchange's result held ("Results: hold") and answered by `GameScreen` unless a wiped-out mark is up; the order messages not shown, the card's order label flashing (`BattleBoardView.FlashOrder`, `BattlePresenter.OrderedUnit`); every card's stats always under its portrait, `Battles/CardPortrait` from `Original.Content/Battles/BattleCardStats` (no menu offers Info), the focus "◆ n" on it; the advantages as what they do ("Walls +8%", "Strategy +10%") and the battle's limit on a plate at the scene's top (`BattleOpening.Effects`, `LimitText`); its settings Option → Battle Screen…, `BattleScreenSettings`);
  Option → Palette (`DeploymentPalette`, steel by default) colours our battle screen and the map's plates;
  `Battles/AdvantageTip` (the instant tooltips of the advantages next to the armies' names, `BattleAdvantage.Parts`); `GodotGlyphRenderer` (TextServer; MS Gothic or the bundled BIZ UDGothic in `godot/fonts`, Option →
  Fonts, `--font ms-gothic|biz-ud-gothic` for one launch), `BattleAudio`; `Screens` switches screens. Solution `Kichikuou.sln` at the root (`dotnet/project/solution_directory`).
- `tools/analysis/` — Python scripts for reading the decompiled scenario; `tools/codegen/` —
  generators for committed C# sources; `tools/golden/` — harness launcher and page classes;
  `tools/ui/` — mock-ups of our screens drawn over screen dumps (the images stay outside the repository).
- `docs/` — brief, spike findings, variable dictionary; `window-menu.md` records
  xsystem35's window menu bar (our reference; File/Screen/Option/MsgSkip) and what its commands do;
  `ui/survey.md` surveys the screens and input for our own interface; `sengoku/` surveys Sengoku
  Rance's battle from its code, the reference for our own battle screen (`tools/sengoku/split_jam.py`).

## Local paths

The paths of the owner's machine never enter the repository: they live in **`local.env`** at the
root, which git ignores (`KEY=value` lines; the committed template `local.env.example` lists every
key; an environment variable of the same name overrides a line). **Read `local.env` first** whenever
a command or a doc needs a folder or a tool: `KICHIKUOU_GAME_DIR` (the game), `KICHIKUOU_EXTRACT_DIR`
(the extract, `_extract` in the docs: `scenario`, `scenario_named`, `cg_png`, `cg_pms`, `wav`,
`golden`), `KICHIKUOU_GODOT_EXE` (the Godot console exe), `KICHIKUOU_XSYSTEM35_DIR`,
`KICHIKUOU_MSYS2_DIR`, `KICHIKUOU_ALICE_TOOLS_DIR` and the spike's tools (docs/spike-findings.md,
"Local layout"); `SENGOKU_RANCE_GAME_DIR` and `SENGOKU_RANCE_EXTRACT_DIR` (docs/sengoku/README.md). `Kichikuou.Engine.LocalPaths` reads it for the tests and the Godot game's default
folders, `tools/golden/capture.ps1` for the harness. Docs and code name a key, never a path; never
commit `local.env`.

## Commands

- Tests: `dotnet test tests/Kichikuou.Engine.Tests`, `tests/Kichikuou.Content.Tests`,
  `tests/Kichikuou.Original.Tests` (seconds; "the core tests" below are these three) and
  `tests/Kichikuou.Original.Content.Tests` ("the content tests").
  The content tests run in two sizes (`DifferentialRuns`). The quick run, the default, takes an eighth
  of two things: the random cases of the differential tests (a port against its file run from the
  decompile on random states — the command phase, the command slices, the nation turns, the week
  loop, the weekly events, the load/save screen — and the presenter's audit, `GamePresenterTests.Audit`)
  and the captured battles the battle-screen tests replay (`BattleSceneTests.Captures`: the scene,
  the opening, the result waits, the strikes, the windows, the exchange effects, the scout report).
  `KICHIKUOU_FULL_TESTS=1` runs them all (the whole project ~8 min, its long tests spread over classes
  that xUnit runs in parallel, `Shards.cs` in the content tests; only the full counts fail a test
  whose random states never reached a path it lists). **Run the full counts only where they can find
  what the quick run cannot** (owner, 2026-10-14): a change reaches the scaled tests whose code it
  changes — run those, filtered, with the full counts
  (`KICHIKUOU_FULL_TESTS=1 dotnet test tests/Kichikuou.Original.Content.Tests --filter "FullyQualifiedName~ExchangeEffectTests"`);
  the whole project only when the change reaches what many of them share — the rule code the
  differential tests compare, the script runtime, the content readers, the presenter's drawing. A
  change no scaled test reaches (docs, comments, Godot-only code, a class only unscaled tests read)
  needs the build, the core tests and its own tests only. Run a due full run once, when the change is
  settled, not while the owner may still ask for changes (owner's request 2026-10-11). Say in the
  reply which run a result comes from and why the full counts were or were not due.
- Build everything: `dotnet build Kichikuou.sln`
- Build and play in a window without Rider or the editor: `run-game.cmd` at the root (a double-click), or
  `powershell -ExecutionPolicy Bypass -File tools
un-game.ps1 [-Console] [-NoBuild] [game switches]` — it builds
  `godot/Kichikuou.csproj` and starts `KICHIKUOU_GODOT_EXE` (the exe without `_console` unless `-Console`) on `godot`,
  passing the rest after `--`.
- Run the game headless (smoke test): `<Godot console exe> --headless --path godot --quit-after 30`;
  `<Godot console exe>` is `KICHIKUOU_GODOT_EXE` in `local.env` (the editor's exe is next to it).
  Commit the `*.uid` files Godot creates.
- Watch a capture headless and save the screen at every key wait and long pause:
  `<Godot console exe> --headless --path godot -- --watch <capture> --fast --quit --shots <dir>`.
- Play the game unattended headless (slots in `user://saves-auto`; `--load` resumes them):
  `<Godot console exe> --headless --path godot -- --newgame --auto --fast --quit [--seed n] [--shots <dir>] [--weeks n] [--stage n] [--interface new]`;
  one visit each to Harem, Subordinates and the hideout in the prologue and to Treasure and All Data
  at stage 1 (after one search of the Monsters' Cave with the weakest unit: down to a battle, Retreat,
  Finish); from stage 1 once a week a replenish (`Cﾌﾞｶ`) and an attack on the rebels (from their
  second week, while Rance has 3/4 of his troops), defences with the strongest; Turn END every week;
  ends at the first file or path not ported yet ("not ported yet"), at a game over, after week 30
  (`--weeks n`) or two weeks after story stage `--stage n` is reached — `--seed 2 --weeks 60 --stage 2`
  takes Oaks in week 34 and stops in week 36, ~15 min (exit code 0; 1 after an error). `--interface new`
  builds the parties in our own deployment table and swaps the first two places by drops and back,
  and in a window, once a launch, dumps the week report (`week-report`), each kind of our message windows
  (`message-speech-left`, `message-speech-right`, `message-thought-left`, `message-thought-right`, `message-narration`,
  `message-place-title`; the kept save `saves-week26` reaches all but the right thought; 131 as `message-cg-bar-speech`, `-thought`, `-narration`, with `--harem`), the message log (`message-log`, once 20 pages are logged; plain `--auto` in a window shows it too, without moving the pointer), the battle's info boxes (`info-defence`, `info-fall`, `info-results`, `info-level-up`, `info-panel`; the week-18 save played to the week's end, `--weeks 18`, reaches the first three), the first map wait's plates (`map-markers`, `map-markers-hover`, `map-markers-active`), the province menu (`province-menu`) and
  clicks the week's attack target through its plate (`map-marker-hover`; Godot's input, the system pointer
  moved), and answers the first turn through the pointer on our command input
  (the acting unit's menu, another own card's, closed by a click off it; Retreat, whose dialog it
  cancels; back at the turn, another own card given the order it has already, whose label flashes;
  each advantage's tooltip, each enemy card's banners, two sample menus' two and three banners never
  answered, then a click on the target's Melee banner; dumps `menu`, `menu-orders`, `retreat`,
  `advantage-tip-1`…, `banners-E1`…, `sample-2-banners`, `sample-3-banners`, `banner-hover`, and without `--fast` `order-flash`, `slide`
(halfway through the first wait for the armies' slides; `--army-seconds 1` makes them long enough) and each exchange effect once, at its peak (`effect-melee-player`, …); it moves the system pointer,
  so leave the mouse alone); its dumps need a window (no `--headless`). With `--until battle` the exercise ends
  the run once its attack's exchange is over (owner, 2026-10-15: the rest of the battle only cost time), so the
  battle's result is not reached.
- **A look at one screen** (screen dumps for the owner) starts from a kept save, never from the
  prologue (owner's requests 2026-10-12): `user://saves-week18` holds the owner's own game at week 18
  (LP 3, month 5, week 2; stage 1, the Leazas week) — the one to use, chosen by the owner (2026-10-13)
  because no event plays at its turn's start — and
  `<Godot console exe> --path godot -- --load --auto --fast --quit --seed 2 --saves user://saves-look --slots-from user://saves-week18 --until battle [--interface new] --shots <dir>`
  copies it into a working folder (the kept one stays unchanged), plays the week's attack on Oaks (6)
  — its deployment picker and its battle: Rance (unit 1), Haurein (unit 12), Cordoba (unit 8) and
  Barres (unit 6) against Ex (commander 1), Sakanaku (commander 6), Dodge (commander 5) and Jibul
  (commander 7), lost with `--seed 2` — and stops (`--until deployment` stops after the picker; in a window with
  `--interface new`, after the battle screen's exercise and its attack). A load
  replays the week from its autosave (a manual save is a copy of it, `LoadSaveScreen`): the load
  list, the banner, income and expenses, then the command menu. `--load --auto` always loads slot 1
  (the list's first item), so a kept folder holds one `slot01.ksav`: the owner's slot copied under that
  name. It cannot be made again by a command; when the save format or the boot changes, ask the owner
  for a new one. The older kept save `user://saves-week26` (the bot's own game, the second Leazas week:
  the attack on South (4)) plays the week's events (Lia's scene, Maris's schedule) first. Making it
  again: first week 25, the first Leazas week, ~2 min headless —
  `--newgame --auto --fast --quit --seed 2 --weeks 24 --saves user://saves-leazas`; then week 25
  ended at once and the first attack on it (on North (2)) repelled by Rance (unit 1), Menad (unit 10) and Asuka
  (unit 11) (the owner's choice; the second, on Wes (8), with the bot's own party), seconds —
  `--load --auto --fast --quit --seed 2 --turn-end --party 1,10,11 --weeks 25 --saves user://saves-week26 --slots-from user://saves-leazas`.
  The scout report (owner, 2026-10-14): `--turn-end --scout` on the week-18 save ends the week at once; the rebels
  attack Ys (5) the same week and `--scout` chooses Ninja Scout in `FIGT_B`'s menu at that first enemy attack (once
  a launch; the ninja menu takes its first item, the first free ninja: Kanami) — both menus clicked as a player clicks
  them (press, held six frames, release; the system pointer stays), and the key wait after each must find no click
  left, else the run fails (the bug of 2026-10-16, `Engine/Script/InputLatch`) — then it intercepts as usual —
  `--load --auto --fast --quit --seed 2 --saves user://saves-scout --slots-from user://saves-week18 --turn-end --scout --until deployment [--interface new] --shots <dir>`
  (Ex (commander 1), Sakanaku (commander 6), Dodge (commander 5), Jibul (commander 7); our overlay needs a window,
  and `--until deployment` stops before the battle, so nothing moves the system pointer).
  A harem scene (window 131 under an event CG, 2026-10-18): `--harem <cell>` on the week-18 save makes the first
  map wait open the menu, choose Harem and pick the girl in that cell of its picker (left to right, top to bottom:
  Lia 2, Maris 3, Kanami 6, Haurein 9), before the replenish and the attack —
  `--load --auto --fast --quit --seed 2 --saves user://saves-look-131 --slots-from user://saves-week18 --harem 6 --until deployment [--interface new] --shots <dir>`.
  The Treasure screen (2026-10-19): `--treasure` on the week-18 save makes the first map wait open Treasure (the
  L. Holy Sword (item 6) on Rance (unit 1), By Lord (item 7) on Rick (unit 7)); with `--interface new` unattended play
  clicks through our screen — Rance picked and the pick dropped, his sword taken off (the script's question answered
  Yes) and given back (picked first and offered to Rick, whose cell asks our question, answered No, then to Rance),
  then given again (refused, "Already equipped.") — and in a window dumps `treasure`, `treasure-hover`,
  `treasure-which-item`, `treasure-items`, `treasure-unequipped`, `treasure-question` (the callout),
  `treasure-replace` (our question), `treasure-equipped`, `treasure-refusal`; else it plays the original picker (`AutoItemSteps`) —
  `--load --auto --fast --quit --seed 2 --saves user://saves-look-items --slots-from user://saves-week18 --treasure --until deployment [--interface new] --shots <dir>`.
  Add another kept save the same way when a screen needs another phase.
- Show an ending's `OVER` (its picture, staff roll, Completion Report) on a booted game:
  `<Godot console exe> --headless --path godot -- --ending <1-14> --auto --fast --quit [--shots <dir>]`
  (without `--auto` the keys wait for the player; slots in `user://saves-ending`).
- Render our exchange effects' frames (Debug → Effects; docs/ui/battle-screen.md, "The exchange's effects"), in a
  window — the pointer is not moved: `<Godot console exe> --path godot -- --effects --shots <dir> --quit`. Sengoku
  Rance's reference frames come from `tools/sengoku/effect_frames.py`, into a folder outside the repository.

## Rules

- **Faithful port first.** Reproduce the original's mechanics and balance one-to-one; modernise
  only the interface. Do not "improve" rules. A Sengoku-mechanics hybrid comes later, on a
  separate branch, only after the full port.
- **Register every quirk.** Bugs and oddities of the original are reproduced as is and each one is
  recorded in [docs/quirks.md](docs/quirks.md) (stable ID, cited as `// QUIRK: <ID>`), so some can
  be offered as optional fixes later.
- **Engine-independent core.** Game rules live in `Kichikuou.Original` (on the shared `Kichikuou.Engine`), .NET class libraries with no
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
