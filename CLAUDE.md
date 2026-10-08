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
end-to-end tests, but no comparison with the original game exists yet. Next: the golden-test
harness on a patched xsystem35 (`F:\sources\Rance\xsystem35-sdl2`, branch `golden-harness`,
built with MSYS2 — see `docs/spike-findings.md`).

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
- `tests/Kichikuou.Core.Tests` — xUnit tests for the core.
- `godot/` — Godot 4.7.2 .NET project (`Kichikuou.csproj`, namespace `Kichikuou.Game`);
  presentation only. Solution `Kichikuou.sln` at the root (`dotnet/project/solution_directory`).
- `tools/analysis/` — Python scripts for reading the decompiled scenario; `tools/codegen/` —
  generators for committed C# sources.
- `docs/` — brief, spike findings, variable dictionary.

## Commands

- Tests: `dotnet test tests/Kichikuou.Core.Tests`
- Build everything: `dotnet build Kichikuou.sln`
- Run the game headless (smoke test): `<Godot console exe> --headless --path godot --quit-after 30`;
  the editor path is in `docs/spike-findings.md`. Commit the `*.uid` files Godot creates.

## Rules

- **Faithful port first.** Reproduce the original's mechanics and balance one-to-one; modernise
  only the interface. Do not "improve" rules. A Sengoku-mechanics hybrid comes later, on a
  separate branch, only after the full port.
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
