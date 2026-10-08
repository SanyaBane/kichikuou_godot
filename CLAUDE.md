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
`NotSupportedException`), and so are the pickers `Sﾌﾞﾀｲ`, `Sﾌﾞｶ`, `Sﾊｰﾚﾑ`
(`Strategy/*Screen.cs` on `PickerScreen`), all routed by `Strategy/StrategyRules`. W2 is done
for stage 0: `MAIN.ADV` (the week loop) is `Strategy/WeekLoop` up to the thieves' week (the
Leazas week throws `NotSupportedException`); the autosave stays a `ﾛｰﾄﾞｾｰﾌﾞ` hook until G0. The battle
runs in the router too (`FIGT_A`/`FIGT_B`/`FIGT`/`死亡捕獲`), so the game's input is an
`Strategy/IGameInput` (battle + scenes; owner's decisions 6-7 in `docs/strategy/README.md`).
Next is G0 (the Godot screens).

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
  `COMMAND.ADV`, the `*Command` classes (on `CommandFile`) are the `C*.ADV` command files, the
  `*Screen` classes (on `PickerScreen`) their `S*.ADV` pickers, and `StrategyRules` routes calls of these files to their ports (the rule hooks of a `SceneRunner`).
- `src/Kichikuou.Content` — engine-independent access to the player's own game data (never
  part of the repository): `ScenarioText` reads message texts from the decompiled scenario by
  `ScriptRef`; `Scripts/ScenarioScripts` reads the data files' statements and whole files as programs
  (`ScenarioParser`; strictly: anything unexpected fails with its location); `Graphics/` decodes the CGs keeping palette indices (`IndexedPng`, `Pms`,
  `CgLibrary`, `Palette` with the engine's reserved entries; `ToRgba(40)` = `G n, 40`);
  `Audio/` maps sound and music numbers to files. `Screen/` emulates the engine's drawing
  surface (3200×800 at 2× RGBA, `G`/`CC`/`CS`/`CF`), message text and selection windows;
  `Battles/BattlePresenter` draws every core event the way the original does
  ([docs/battle/screen-layout.md](docs/battle/screen-layout.md)), `FigtSelScreen` /
  `BattleMenuScreen` are the mouse-driven input screens. Tests in
  `tests/Kichikuou.Content.Tests` (the golden part draws and plays every capture and resolves
  every text; `KICHIKUOU_SCENARIO_DIR` / `KICHIKUOU_EXTRACT_DIR` / `KICHIKUOU_GAME_DIR`
  override the local folders).
- `tests/Kichikuou.Core.Tests` — xUnit tests for the core. `Golden/GoldenBattleTests` replays the
  captures in `E:\Modding games\Modding Rance\Kichikou\_extract\golden` (or
  `KICHIKUOU_GOLDEN_DIR`) and is skipped when there are none.
- `godot/` — Godot 4.7.2 .NET project (`Kichikuou.csproj`, namespace `Kichikuou.Game`);
  presentation only: `Main` picks a capture, `Battles/BattleScreen` shows the presenter's
  surface and forwards mouse/keys, `GodotGlyphRenderer` (MS Gothic via TextServer),
  `BattleAudio`. Solution `Kichikuou.sln` at the root (`dotnet/project/solution_directory`).
- `tools/analysis/` — Python scripts for reading the decompiled scenario; `tools/codegen/` —
  generators for committed C# sources; `tools/golden/` — harness launcher and page classes.
- `docs/` — brief, spike findings, variable dictionary.

## Commands

- Tests: `dotnet test tests/Kichikuou.Core.Tests` and `dotnet test tests/Kichikuou.Content.Tests`
- Build everything: `dotnet build Kichikuou.sln`
- Run the game headless (smoke test): `<Godot console exe> --headless --path godot --quit-after 30`;
  the editor path is in `docs/spike-findings.md`. Commit the `*.uid` files Godot creates.
- Watch a capture headless and save the screen at every key wait and long pause:
  `<Godot console exe> --headless --path godot -- --watch <capture> --fast --quit --shots <dir>`.

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
- IDE is JetBrains Rider.
