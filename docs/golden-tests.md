# Golden tests — the battle port against the original game

Golden tests prove that a ported rule file behaves like the original scenario, variable by
variable. A patched xsystem35 (the "harness") records what happens when the owner plays a
battle; the core replays the same battle from the same memory, with the same random draws and
the same player inputs, and its memory must end up equal to the original's.

## Pieces

| Piece | Where |
|---|---|
| Harness (patched xsystem35) | `F:\sources\Rance\xsystem35-sdl2`, branch `golden-harness`, `src/golden.c` |
| Capture launcher | `tools/golden/capture.ps1` |
| Captures (game data — never committed) | `E:\Modding games\Modding Rance\Kichikou\_extract\golden\` |
| Capture parser, replay doubles, memory comparison | `src/Kichikuou.Core/Golden/` |
| Golden tests (skip when no captures exist) | `tests/Kichikuou.Core.Tests/Golden/` |

## Scope of one capture

A capture is one **battle**: a page call into a *root* file (`FIGT_A.ADV`, `FIGT_B.ADV`, or
`FIGT.ADV` called directly by the dungeon / God dungeon) made while no battle is being
captured, up to the return from that call, or up to a page jump (`&`) out of it (game over).

While a battle is captured, every page call falls into one of three classes (configured by the
launcher, see "Harness configuration"):

- **inner** — files the core ports or treats as presentation: the `FIGT*` files, `死亡捕獲.ADV`,
  and the UI helpers `WAO_SUB.ADV`, `M.ADV`, `WAO_MAP.ADV`, `PCMPLAY.ADV`. Calls into them are
  transparent; their draws and inputs are logged.
- **pick** — `FIGT_SEL.ADV`, which the core models as one input call
  (`IBattleInput.SelectCommandAsync`). Inputs inside it are not logged; at its return the
  harness logs the result pair `RetVal` (`VAR0000`) and `Tmp0001` (`VAR0001`).
- **hook** — every other file (story scenes, `Sﾌﾞﾀｲ.ADV`, `Gﾀﾐ.ADV`, …), which the core calls
  through `IStoryHooks`. The harness dumps memory when the hook is entered and when it returns,
  and logs nothing inside it. The replay checks the core's memory against the first dump and
  then loads the second one, so the test needs no port of the story scene.

## Capture file format

One UTF-8 text file per battle, LF line ends, one record per line, tokens separated by single
spaces. The harness writes `<stamp>-<seq>-<root>.golden.part` and renames it to `.golden` when the
battle ends normally; a battle interrupted by loading a save, a reset or a crash keeps the
`.part` suffix and is ignored by the tests.

`<where>` is `<file>@<address>`: the scenario file (name from `xsys35dc.hed`) and the hex byte
address of the command inside it (the decompiler's `L_xxxxx` labels use the same addresses).

```
golden 1                                  # format version
seed <n>                                  # sgenrand() seed of this session (XSYS35_SEED or the pid)
root <file> <where>                       # battle entry: callee, and the call site in the caller
mem enter … end-mem                       # memory right after the root call
…events…
mem exit … end-mem                        # memory after the return (or at the jump)
end return | end jump <file>              # how the battle left (end abort <reason>: dropped, stays .part)
```

### Events

| Record | Logged at | Meaning |
|---|---|---|
| `rnd <n> <result> <where>` | every `Y 4, n:` (also n = 0/1, which draw nothing) | the value stored in `RetVal` |
| `key <code> <where>` | every `IK 0`/`IK 1` | the key code stored in `RetVal` (0 while message skip is on) |
| `wait <ticks> <code> <where>` | every timed `Y 3, n:` | the key that ended the pause, stored in `RetVal`; diagnostic only, the core has no input for it |
| `menu <choice> <count> <where>` | every menu (`]`) | `choice` = 1-based index among the `count` items that were registered, 0 = cancelled (right click) |
| `music <track> <where>` | every `SR 0, var` | the track number stored in `var` |
| `pick <retval> <tmp0001> <where>` | the return (`%0`) of a pick file | the pair the core's `SelectCommandAsync` must return |
| `call <file> <where>` | page call into a hook file | followed by `mem hook-enter … end-mem` |
| `ret <file> <where>` | return from that hook | preceded by `mem hook-exit … end-mem` |
| `jump <file> <where>` | page jump (`&`) anywhere in the battle | the capture ends (`mem exit`, `end jump <file>`) |

Inside a hook nothing is logged, so a hook's draws and inputs are invisible to the replay; the
hook-exit dump carries the generator state after them.

### Memory dump

```
mem <tag>
rng <mti>                                 # mt19937-1.c index (0..624)
rngv <i> <w> … (16 hex words)             # mt[i..i+15]; 39 lines cover mt[0..623]
vars <size>                               # page 0 = sysVar[], 65537 slots in System 3.9
v <i> <d> … (16 decimal values)           # sysVar[i..i+15]; only rows with a non-zero value
page <n> <size>                           # every allocated array page (DC)
p <n> <i> <d> … (16 values)               # page n, [i..i+15]; only rows with a non-zero value
bind <var> <page> <offset> <pointvar>     # every variable bound with DS (pointvar = cursor var number)
str <n> <text>                            # every non-empty string variable, converted to UTF-8
end-mem
```

Missing rows are zeros. `<text>` escapes `\` as `\\` and every byte below `0x21` or equal to
`0x7f` as `\xHH`; other bytes (including UTF-8 sequences) are written as they are. String
numbers are the engine's (`MS n` writes string n).

## Replay in the core

`GoldenReplay` (in `src/Kichikuou.Core/Golden/`) runs one capture:

1. **State.** A fresh `GameState`; the `enter` dump is loaded into its `ScriptMemory` (variables
   0..805, every page at the dumped size, strings). Non-zero variables beyond 805 or bindings that
   differ from the generated layout make the replay fail early: the core's layout would be wrong.
2. **Random.** A `ReplayRandom` holds an `Mt19937Legacy` restored from the dump's `rng` state and
   draws exactly like `XSystem35Random`. Every `Next(n)` must meet the next `rnd` record with the
   same `n`, and its own result must equal the recorded one; the first difference stops the
   replay with the original's `<where>` and the number of draws so far. So the generator is not
   reseeded at battle entry: its state is part of the dump, and the patched game plays like the
   unpatched one.
3. **Input.** `ReplayBattleInput` answers `WaitForKeyAsync` from `key`, `ChooseAsync` from
   `menu` (index → the menu's option list; the core's list must have `count` items),
   `GetPlayingTrackAsync` from `music`, and `SelectCommandAsync` from `pick`. Records are consumed
   in log order by all consumers together, so a draw where the original waited for a key (or the
   other way round) is reported as a divergence. `wait` records are skipped.
4. **Hooks.** `ReplayStoryHooks` meets each `call` record: the file must match, the core's memory
   is compared with the `hook-enter` dump (differences are reported as a checkpoint failure but
   the replay goes on), then the `hook-exit` dump is loaded, generator state included. A hook
   whose capture ends in `jump` throws `ScenarioJumpException` after loading the final dump.
5. **Entry point.** `FIGT_A.ADV` → `AttackProvinceAsync`, `FIGT_B.ADV` → `DefendProvinceAsync`,
   `FIGT.ADV` → `FightAsync`.
6. **Result.** After the entry point returns (or throws `ScenarioJumpException` for a capture
   that ends in `jump` to the same file), every record must have been consumed and the memory is
   compared with the `exit` dump.

### What the comparison ignores, and why

Compared: all variables 0..805, every slot of every page, every string variable, the generator
state. Ignored — and listed in the report as "ignored" so they stay visible:

- **The `ui` group of [variables.tsv](variables.tsv)** (variables and page fields), except the
  ones that are rule state despite the prefix: `Ui_CmdAttackAvail` (written by `FIGT_A`, read by
  the command menu, Q-2). Reasons: (1) these are the internals of the message windows, the map
  and the pointer (`WAO_SUB`, `M`, `WAO_MAP`, `FIGT_SEL`), which the Godot layer replaces, so a
  port would be dead code; (2) several depend on the physical pointer and on timing (`IM` writes
  `Ui_MouseX/Y`; drag anchors, button cursors), which no input record can reproduce; (3) no rule
  in the battle reads them; (4) the hook-exit dumps resynchronise them, so a difference cannot
  leak into later rule state through a story scene.
- Nothing else. A scratch variable (`Tmp*`, `RetVal`) that differs is a finding: the script has
  no locals and stale scratch values are observable (porting conventions).

If real captures show further pointer-dependent values (e.g. `FIGT_SEL` scratch written from
the pointer position), each one is added to the ignore list here with its reason, never as a
blanket rule.

## Harness configuration

`tools/golden/capture.ps1` sets the environment and starts the patched build in the game folder:

| Variable | Meaning |
|---|---|
| `XSYS35_SEED` | decimal seed for `sgenrand()` instead of the process id |
| `XSYS35_GOLDEN_DIR` | output folder; capture is off when unset |
| `XSYS35_GOLDEN_HED` | path of `xsys35dc.hed`: page `n` is the `n`-th file name after the `#SYSTEM35` line |
| `XSYS35_GOLDEN_CLASSES` | [tools/golden/classes.txt](../tools/golden/classes.txt): lines `root\|inner\|pick <file> …` (a UTF-8 file, because the names are not ASCII and Windows hands the environment to the program in the ANSI code page) |
| `XSYS35_GOLDEN_TRACE` | `1` = also append every page call/return/jump of the session to `trace.log` |

The harness's hooks are `src/golden.c` plus one call at each logged command (`cmdy.c` `Y 3/4`,
`cmdi.c` `IK`, `selection.c` menus, `cmds.c` `SR`, `cmd_check.c` `&`) and two observers in
`scenario.c` (page call entered, page return resumed). Loading a save (`savedata.c`) or a reset
drops the open capture with `end abort <reason>` (left as `.part`).

Verified on the real game (2026-10-08), with `SDL_VIDEODRIVER=dummy SDL_AUDIODRIVER=dummy` for a
windowless run: the trace names the boot sequence correctly (`ｽﾀｰﾄ.ADV` calls `XSPEC.ADV`, then
`SET.ADV`, which calls `SET_MAP.ADV`, …), so the name table is right; a dump taken at the first
`PCMPLAY.ADV` call after `SET.ADV` parses, its `DS` bindings equal the generated `GameState`
layout, and it fits the core's memory. That dump showed that `DC page, n` allocates `n + 1` slots,
which the core did not do (fixed in `tools/codegen/gen_state.py`).

## Battles to capture

Plains attack, city attack, an enemy attack intercepted (plains and city), a special attack
(Rance Attack or a beam), a commander death on each side, a battle with a story hook (e.g.
the Ex fight, JAPAN invasion). Each `.golden` file becomes one test case.

Start the harness with

```
powershell -ExecutionPolicy Bypass -File tools\golden\capture.ps1
```

and play normally; loading a save before a battle is fine (the capture starts at the battle's
page call). Run the tests with `dotnet test tests/Kichikuou.Core.Tests`; a failing capture prints
the divergence (with the original's `file@address`) or the differing variables by name.
