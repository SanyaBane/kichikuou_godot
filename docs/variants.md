# Game variants — the shared engine and the variant split (plan, 2026-10-12)

Owner's decision 24 ([strategy/README.md](strategy/README.md)): the code becomes a **shared engine**
plus **one rule set per game variant**. Variant 1 is the original game, today's port. Variant 2 is the
Sparrow Mod ([mods/sparrow-mod.md](mods/sparrow-mod.md)); its port starts only once its author allows it.
The hybrid with Sengoku Rance's mechanics (brief, scope decision 2) would be a third variant. **This
plan is the refactor that comes first.** It changes no behaviour of the original and adds nothing of the
mod.

Status: **approved** (2026-10-12; the owner accepted the names). The facts come from a read-only sweep
of the code on 2026-10-12, at commit `616eb12`.

**It must pay off with the original alone.** The owner expects the mod's author to refuse. So the split is
worth doing only for what it gives the original's code:

- the layers the compiler enforces (the runtime can no longer reach into the rules);
- one place for the facts the runtime takes from the game, instead of constants scattered around;
- the hand ports of the helper files apart from the generic runtime;
- one support project for the test doubles that the differential tests now copy;
- the ground the hybrid (scope decision 2) and our own interface stand on.

Steps that pay off only with a second variant are **deferred** until one starts (see "Deferred").

## Principles

- **Nothing changes for the player.** Every step ends with the build (warnings are errors), the core
  tests, the content tests' full run, the golden replays and a headless Godot smoke test all green. At
  the end an `--auto` run with a fixed seed must reach the same weeks as before.
- **Move to the engine only what is generic now.** A class goes to the engine when it would work
  unchanged for any System 3.9 game. Everything else stays with the original. The Sparrow port copies
  what it needs later, so there is no speculative abstraction.
- **Per variant, never `#if`.** A variant's rules are its own C# files. The mod changed nearly every
  rule file and shifted every message line, so shared sources with conditionals would be unreadable.
- **Small steps.** One seam per commit where possible; the purely mechanical moves (files, namespaces)
  get commits of their own, separate from logic.

## Target structure

```
src/Kichikuou.Engine            shared: Sys3, the script runtime, saves, the golden capture format
src/Kichikuou.Content           shared: player-file readers, graphics, music, the engine surface
src/Kichikuou.Original          the original's GameState, boot, rules, battle, golden replay
src/Kichikuou.Original.Content  the original's presenters (battle screen, map, pickers, All Data), sounds
(later) src/Kichikuou.Sparrow, src/Kichikuou.Sparrow.Content
godot/                          one app; the variant chosen by the game files (only Original for now)
tests/                          the same split; one support project for the shared test doubles
```

Owner's choices in it (see "Questions"): the project names, and one Godot app rather than two.

## The cut: what goes where

**Engine** (`Kichikuou.Engine`, about 3k lines):

- `Sys3/*`: no edges into variant code; one compile-checked doc `cref` to `GameState` goes.
- The runtime from `Script/`: `ScriptProgram`, `ScriptStatements`, `SceneRunner`, `SceneInput`,
  `EngineTimer`, `ScriptInterfaces`, the generic parts of `ScriptContext` (draws, pauses,
  `EngineAsync`, the CG counter, statement evaluation, strings and data reader, `ScriptContext.Save`,
  `SS`/`SR`), `SaveData`'s slot format, `MessageSkip` without its scene list, the gesture queue of
  `PointerScript`, and the generic events of `ScriptEvents`.
- `IScenarioSource`, moved from `Setup/` (Content's reader implements it).
- `LocalPaths`, and from `Golden/` the capture format (`GoldenCapture`, `MemoryDump`).

**Content** (generic): `Hankaku`, `ScenarioText`, `Scripts/*`, `Graphics/*`, `MusicTable`, `Screen/*`,
`TextBox`, and the engine-command emulation now inside `GamePresenter.Engine()` and `DrawCg` (`G`,
`CC`, `CS`, `CF`, `CM`, `WV`, `CB`, `CL`, `EC`, `PF`, `PW`, `B1-4`, `T`, `ZM`, `ZC`, `SL`).

**Original**: `State/` (`GameState`, generated), `Setup/`, `Strategy/`, `Combat/`, the rest of `Golden/`
(`GoldenReplay`, `MemoryComparison`), and from `Script/` the hand ports of the helper files — `WAO_SUB`
(`ScriptContext.Wao`), `M`/`WAO_MAP` (`ScriptContext.Map`), `PCMPLAY` — and the semantic events
(`ScreenEvents`, the window/sound/map events of `ScriptEvents`).

**Original.Content**: `Battles/*`, `GamePresenter` with `Compose`, `Data`, `PickerView`, `SoundTable`,
`IBattleAudio`.

**Godot**: stays one project. The composition root in `GameScreen` takes a variant. The original-only
parts stay as they are: the `--auto` bot, the battle capture viewer and `--ending`.

## The seams to cut (the real work)

1. **A state base class.** The runtime is typed on the generated `GameState`; that type and its
   static binding index are the original's. The engine gets a `ScriptState` base (the `ScriptMemory`,
   `RetVal` = VAR0000, which is the engine's, and an instance `IsBound(int)`), and `GameState` derives
   from it. The generated static layout can stay, because it is per type, and a second variant has its
   own type. `SceneRunner.cs:417`'s static `GameState.IsBound` becomes the instance call.
2. **The variable count per instance.** `ScriptMemory.VariableCount` is a `const` 806, baked into
   `GameState`, `SaveData`, `ScriptContext.Save`, `MemoryDump` and `MemoryComparison`. It becomes the
   memory's own size, which the variant gives.
3. **`ScriptContext` in two.** One `partial` class mixes the runtime with three hand ports, and a
   partial class cannot span assemblies. The engine keeps `ScriptContext`, unsealed, which routes helper
   calls through a virtual `CallHelperAsync(file)`. Then `OriginalScriptContext : ScriptContext` takes
   the `Wao`, `Map` and `PCMPLAY` partials, the uses of the ~60 named original variables (`CallArg`,
   `Ui_Win*`, `MapCam*` …) and the helper names. Rule code switches its `Script` type to it; the calls
   stay the same.
4. **A variant profile.** The facts the engine needs from a variant, gathered in one instance
   (`VariantProfile`) instead of statics and constants:
   - the variable count and the event-entry variable (`ScriptProgram.EvEntryVar = 520`);
   - the scene rule (`SceneRunner.IsSceneFile`: the `G`/`I` prefix plus 8 names; the runner already has
     an `isScene` hook that `GameSession` does not pass);
   - the scenes the message skip skips whole (`MessageSkip.WholeScenes`);
   - the saved pages (`SaveData.SavedPages`);
   - the variant's id for saves and user data.
5. **Content's presenter in two.** The engine-command emulation leaves `GamePresenter` for a generic
   class in `Screen/`; the hand-drawn layers stay with the original.
6. **Interfaces in the right place.** `IScenarioSource` goes to the engine and `IBattleAudio` to
   Original.Content; the original's semantic events get a namespace of their own.

## Steps

Each step ends green (see "Principles").

0. **Plan and decision** (this document): decision 24, the backlog, the Sparrow survey.
1. ✓ (2026-10-12) **De-static inside today's projects.** The variable count per memory, `ScriptState`
   with an instance `IsBound`, `VariantProfile` (the original's built by `Original` code, passed by
   `GameSession`, the tests and Godot). No file moves. As done:
   - `Script/ScriptState` holds the memory, `RetVal` (the generator no longer emits VAR0000), the reads
     through the `DS` bindings and `IsBound`. The variant's type gives the bindings (`BindingOf`) and
     the profile, so the profile reaches the runtime through the state (`ScriptState.Profile`): the
     session and the tests pass nothing new.
   - `State/OriginalVariant` builds the original's profile: the scene rule, the whole scenes, the saved
     pages, `Ev_Entry` = VAR0520, the count from the catalog.
   - `ScriptProgram.FindEntries` takes the entry variable, `MessageSkip.SkipsScene` the whole scenes,
     `SaveData.Capture` the state. The context reads a slot of another variable count as empty (the
     check left `SaveData.ReadFrom`).
   - The variant's id waits for the saves per variant (Deferred): nothing would read it yet.
2. **Split `ScriptContext`** into the engine part and `OriginalScriptContext`; move the original's
   events into their own namespace. Still one assembly.
3. **Split the presenter**: the engine-command emulation into `Screen/`.
4. **The assemblies**: create `Kichikuou.Engine` and `Kichikuou.Original.Content`; `Kichikuou.Core`
   becomes `Kichikuou.Original`, with namespaces following the folders. This step is mechanical:
   `InternalsVisibleTo` per assembly, the solution, the Godot project's references (Godot's scene files
   reference scripts by path; the Godot scripts do not move).
5. **The tests**: the same split (engine, original, original content), and one `Kichikuou.TestSupport`
   project for the doubles each differential file now copies (`OneProgram` ×6, `RandomState` ×6,
   `LoggingView` ×5, `ScribblingHooks` ×4 …) — the Sparrow port will need them too.
6. **The tools' paths**: `gen_state.py`, `gen_figt_m.py` and `tools/golden/describe.py` write and read
   the new paths; `gen_state.py`'s output must stay byte-identical.
7. **Docs**: CLAUDE.md (layout, commands), README, porting conventions; the `--auto` regression run
   (`--seed 2 --weeks 60 --stage 2`: Oaks (6) in week 34, stop in week 36).

## Deferred until a second variant starts

These pay off only with a second variant (the Sparrow port or the hybrid):

- **The generators per variant**: `gen_state.py` taking a variant's inputs (dictionary, pages, tables,
  count, namespace, output path) instead of the original's constants.
- **Saves and user data per variant**: the variant's id in the save header (format version 2,
  version-1 slots read as the original's), and folders of its own for slots and seen messages (the
  original keeps today's folders).
- **Choosing the variant in Godot**: by the game folder the player points at (question 2 below), together
  with the detection by archive hashes that the release's version check needs too (backlog "Release").
- **What only the mod needs**: System 3.9 functions, `+:`, `else` and loops in the parser and the runner;
  Sparrow's dictionary and data.

## Size and risk

- Steps 1–3 are the design work: a few hundred lines change, every test file that builds a `GameState` or a
  `ScriptContext` is touched.
- Steps 4–5 are large but mechanical: about 42k lines of source and 27k of tests change their
  namespaces. The docs name files as `Strategy/WeekLoop` and survive; references to
  `src/Kichikuou.Core/...` are replaced.
- Risks:
  - A big mechanical diff collides with any work in flight; the tree is clean today, so the refactor
    should run with no other work beside it.
  - `internal` members used across the new boundaries (`KayblisTurn.CaptureAsync`,
    `BattlePresenter.Panels`, `Palette.Rgb`, `ScenarioText.TryParse` …) need `InternalsVisibleTo` or a
    public seam.
  - The Godot project builds without warnings as errors, so its breakage shows only at run time: the
    smoke test and one `--auto` run after step 4.

## Questions for the owner

1. ~~The names~~: `Kichikuou.Engine`, `Kichikuou.Content`, `Kichikuou.Original`,
   `Kichikuou.Original.Content` (later `Kichikuou.Sparrow`, `Kichikuou.Sparrow.Content`) — accepted
   2026-10-12.
2. One Godot app that picks the variant from the game folder the player points it at (recommended: one
   interface to maintain, and two separate builds remain possible as export presets), or two Godot
   projects? Open; nothing in steps 1–7 depends on it.
