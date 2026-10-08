# Porting conventions — scenario rules into `Kichikuou.Core`

How hand-ported rule code is written. The goal is a one-to-one port that golden tests can
check variable by variable against the original game; readability comes second, a cleaner
model can be layered on later (e.g. for the hybrid branch).

## State

- **Every script variable lives in `GameState`** and is read and written there, never copied
  into a C# local or field that outlives one expression. The script has no locals: scratch
  variables (`Tmp*`, `RetVal`, `CallArg`) carry values between procedures, and stale values are
  sometimes observable (see `docs/battle/02-resolution.md` §1.4). Plain C# locals are allowed
  only for values the script never stores (e.g. the evaluated condition of an `if`).
- **Records go through the cursor exactly as the script does.** If the script sets `Unit_Cur`
  and then uses `Unit_Troops`, the port sets `state.Unit_Cur` and reads
  `state.Units[state.Unit_Cur].Troops` (helpers such as `Unit` => current record are fine,
  as long as they re-read the cursor on every access). Never cache a record across a call
  that may move the cursor.
- **All arithmetic is `Sys3Int`.** Keep the script's expression shape and grouping; every
  operator saturates. Do not "simplify" `a - b + (b - a)`, do not reorder `x * y / z`.
  Script comparisons that yield 0/1 and conditions using `*` as AND / `+` as OR are written
  with C# `&&`/`||` only when that is provably equivalent (both sides 0/1); otherwise compute
  the `Sys3Int` value and test it against 0.

## Randomness

- Every `Y 4, n:` is one call to the engine's `Rnd(n)` helper, which calls
  `IScriptRandom.Next(n)` and stores the result in `RetVal` like the original. Keep draws in
  the same order and under the same conditions, **including draws whose result is unused**.
- Nothing else may draw from `IScriptRandom`.

## Control flow

- One C# method per script procedure (label reached by call), named as in the spec, with a
  doc comment citing `File.ADV:line-line`. Gotos may become loops/returns only when the
  behaviour is identical; otherwise keep an explicit state variable.
- Cross-file calls into story scenes (`Event(F, n)`), death scenes and other non-rule files go
  through an injected hooks interface; the hook's state effects are documented at the call.

## Shared services

Every part runs on one `Kichikuou.Core.Script.ScriptContext`: the state, `Rnd` (`Y 4`), pauses
(`Y 3`), scene calls (`EventAsync`, `CallFileAsync`), jumps (`JumpTo`), and the helper files
every part calls — `WAO_SUB.ADV` (`WaoAsync(op)`), `M.ADV` / `WAO_MAP.ADV` (`MapCallAsync(mode)`),
`PCMPLAY.ADV` and `SS`/`SR` (`SoundAsync`, `MusicAsync`, `GetPlayingTrackAsync`). A part keeps
short private forwarders in its own words (as `Battle` does) but never a second copy of a helper.
An operation of a helper file that is not ported yet is ported there, in the context, when the
first part needs it. Events every part may raise derive from `ScriptEvent`
(`Script/ScriptEvents.cs`); a part's own events derive from its base (`BattleEvent`). Inputs every
part uses are in `IScriptInput`; a part's own decisions extend it (`IBattleInput`).

## Presentation and input

- Rules never draw. Screen output becomes an awaited event (`await View.ShowAsync(new …)`),
  player choices an awaited input call. Both are interfaces implemented by the Godot layer and
  by test doubles; the order of events must match the order of the script's screen updates.
- Text is never embedded. A message is identified by its location in the decompiled scenario
  (`ScriptRef("FIGT_M.ADV", line)`); the presentation layer resolves it from the player's own
  game files. This keeps original assets out of the repository and makes localisation possible.
  The line is the run's **first text command**, which may be an `R`: when a run starts with a
  line break (e.g. `MP 4, 18:` / `R` / `'fell in battle.'`, the name being a separate event),
  point at the `R`, or the break is lost. `ScenarioText.Read` rejects a reference that follows
  an `R` it would leave out.
- Presentation routines that also change state (e.g. the battle redraw moving cursors) are
  ported for their state effects, and emit an event for the visual part.

## Quirks and tests

- Reproduce every quirk listed in the spec. Mark it in code with `// QUIRK: <spec ref>`.
- Each procedure gets unit tests built from the spec: set up state, feed scripted random
  values (`ScriptedRandom`), assert on state and on the exact sequence of draws.
