# Golden tests — the port against the original game

Golden tests prove that a ported rule file behaves like the original scenario, variable by
variable. A patched xsystem35 (the "harness") records what happens when the owner plays; the
core replays the same run of a file from the same memory, with the same random draws and the
same player inputs, and its memory must end up equal to the original's. The battle was the first
part verified this way (37 captures); since F2 the harness also records the strategic layer —
the boot data, the week loop, the player phase, `COM`, the nation turns, the dungeon, the
commands, the screens and the prologue scenes — in the same play session.

## Pieces

| Piece | Where |
|---|---|
| Harness (patched xsystem35) | `F:\sources\Rance\xsystem35-sdl2`, branch `golden-harness`, `src/golden.c` |
| Capture launcher, page classes, summary tool | `tools/golden/capture.ps1`, `classes.txt`, `describe.py` |
| Captures (game data — never committed) | `E:\Modding games\Modding Rance\Kichikou\_extract\golden\`: battles in the folder itself, every other group in a subfolder named after it (`boot\`, `week\`, `command\`, …) |
| Capture parser, replay doubles, memory comparison | `src/Kichikuou.Core/Golden/` |
| Golden tests (skip when no captures exist) | `tests/Kichikuou.Core.Tests/Golden/` |

The battle screen (`godot/Main.cs`) and the Content tests list only the top folder, so they see
battles only.

## Roots, groups and nesting

A capture records one run of a **root**:

- a **page-call root** — a page call (`%#FILE`) into the root file, up to the return from that
  call (`end return`) or a page jump (`&`) out of it (`end jump`, e.g. a game over);
- a **label root** (`FILE@ADDR`) — the arrival at a command address of the file, up to the next
  arrival there (`end label`), a return below the depth it started at, or a page jump. One week of
  `MAIN.ADV` is the label root `MAIN.ADV@20` (`L_00020`, the top of the week loop; `&#MAIN.ADV`
  enters the file there too). The harness checks the address before every command of a file
  that holds a label root (one integer compare per command elsewhere).

Roots are configured in **groups** ([tools/golden/classes.txt](../tools/golden/classes.txt)). A
group is a class table: while one of its captures is open, every page call falls into one of
three classes:

- **inner** — files the port reproduces: calls into them are transparent; their draws and
  inputs are logged. The group's own roots are inner to it, and the UI helpers `WAO_SUB.ADV`,
  `M.ADV`, `WAO_MAP.ADV`, `PCMPLAY.ADV` are inner in every group (their state effects are ported
  in `Kichikuou.Core.Script`).
- **pick** — a file the core models as one input call (`FIGT_SEL.ADV`:
  `IBattleInput.SelectCommandAsync`). Inputs inside it are not logged; at its return the harness
  logs the result pair `RetVal` (`VAR0000`) and `Tmp0001` (`VAR0001`).
- **hook** — every file the group does not list (story scenes, `Sﾌﾞﾀｲ.ADV`, `ﾛｰﾄﾞｾｰﾌﾞ.ADV`, …),
  which the core calls through `IStoryHooks` or an interface of its own. The harness dumps memory
  when the hook is entered and when it returns, and logs nothing inside it. The replay checks the
  core's memory against the first dump and then loads the second one, so the test needs no port
  of the callee.

**Nesting.** Several captures may be open at once. A call into a root starts a new capture
unless an open capture of the same group logs that call as an inner one (so `FIGT_A` calling
`FIGT` stays one battle). The usual case is a root called inside another capture's hook: the
outer capture dumps around the call and logs nothing inside, the inner capture logs inside it —
e.g. week ⊃ `COMMAND` ⊃ `Cﾃｷﾄｼ` ⊃ `Iﾄｳｿﾞｸ` ⊃ `FIGT_A` (five files, each with its own events).
Each open capture classifies calls by its own table; draws and inputs go to every open capture
that is not inside one of its hooks or picks; a page jump ends every capture that is not inside
a hook (a scene may jump and still return, as before). Loading a save or a reset aborts every
open capture (`end abort <reason>`, left as `.part`); a label capture still open inside a hook
when its label is reached again is aborted (`end abort reentered`: a jump left it). Up to 16
captures may be open.

### Groups and their classes

Hooks are everything not listed; "helpers" = the four UI helpers above.

| Group (folder) | Roots | Inner besides the roots and helpers | Pick | Notable hooks — why |
|---|---|---|---|---|
| `battle` (top folder) | `FIGT_A`, `FIGT_B`, `FIGT` | `FIGTATTK`, `FIGT_M`, `FIGT作`, `FIGTﾃｷDE`, `FIGTﾐｶDE`, `死亡捕獲` | `FIGT_SEL` | `Sﾌﾞﾀｲ`, scenes — unchanged since the first harness |
| `boot` | `SET` | `SET_MAP`, `SET_CHR`, `SET_ITEM`, `ﾓｼﾞｷｬｯｼｭ` (display only; the port drops the call) | — | `ﾛｰﾄﾞｾｰﾌﾞ` (mode 10: page 4 from the save files) |
| `week` | `MAIN.ADV@20` | — | — | `COMMAND`, every `T*`, `FIGT_B`, `ﾛｰﾄﾞｾｰﾌﾞ` (the autosave at `MAIN.ADV:20` is a hook call inside the week), `Iﾊﾝﾗﾝ`, `Iﾏｼﾞﾝ`, `GAL`, every `G*` |
| `command` | `COMMAND` | — | — | `COM`, every `C*`, `ﾛｰﾄﾞｾｰﾌﾞ`, `各種DATA`, `Iｶﾝｶﾞｴ`, `Iﾄｳｿﾞｸ`, `ｵｰﾌﾟﾆﾝｸﾞ`, `TEST`, scenes — the sequence of hook calls (with `Prov_Cur` / `Ui_ClickedProv` / `Dgn_Cur` in the hook-enter dump) is the player's choice; the map clicks are in the `mouse` records |
| `com` | `COM` | — | — | every scene (`Iｼﾚｲﾌﾞ`, `Gｿｳﾙ`, …) |
| `nations` | `TCﾘｰｻﾞｽ`, `TJAPAN`, `Tｹｲﾌﾞﾘｽ`, `Tｾﾞｽ`, `Tﾊﾟｯﾄﾝ`, `Tﾍﾙﾏﾝ`, `Tﾙﾄﾞﾗ` and the defender pickers `TCﾘｰｻﾞｽ2`, `TJAPAN2`, `Tｹｲﾌﾞﾘｽ2`, `Tｾﾞｽ2`, `Tﾍﾙﾏﾝ2`, `Tﾙﾄﾞﾗ2` | — | — | `FIGT_B` (verified separately), scenes. No `T*` file calls another |
| `dungeon` | `Cﾒｲｷｭｳ` | `死亡捕獲` (ported in `Combat`; the dungeon calls it directly) | — | `FIGT` (a battle capture of its own), `Sﾌﾞﾀｲ`, scenes, `Iｶﾐ` |
| `commands` | `Cﾃｷﾄｼ`, `Cﾃｷﾄｼ2`, `Cﾌﾞｶ`, `Cﾊｰﾚﾑ`; since milestone 2 `Cｼﾊｲﾄｼ`, `Cｼｮｳﾆﾝ`, `Cﾃｲﾄ`, `Cﾀｶﾗ`, `各種DATA` | — | — | the `S*` screens, `FIGT_A`, the `T*2` pickers, `Cﾆﾝｼﾞｬ`, `Cﾒｲｷｭｳ` (`Cｼﾊｲﾄｼ`'s Ruins Search), `Iﾄｳｿﾞｸ`, `Iｼﾚｲﾌﾞ`, `ｵｰﾌﾟﾆﾝｸﾞ`, scenes — each has its own capture or part |
| `ninja` | `Cﾆﾝｼﾞｬ` (apart: `Cﾃｷﾄｼ`/`Cﾃｷﾄｼ2` call it) | — | — | scenes (`Gｺｰﾄﾞ`, `Gｶﾅﾐ`, `Gﾌﾚｲｱ`) |
| `screens` | `Sﾌﾞﾀｲ`, `Sﾌﾞｶ`, `Sﾊｰﾚﾑ`, `Sｱｲﾃﾑ` | — | — | none reached (only the helpers inside) |
| `scenes` | `Iﾄｳｿﾞｸ`, `Iﾌﾟﾛﾛｰｸﾞ`, `Iｼﾚｲﾌﾞ`, `Iﾊﾝﾗﾝ`, `ｵｰﾌﾟﾆﾝｸﾞ`, `Gｼｨﾙ`, `Gｿｳﾙ`, `Gﾊﾞｳﾝﾄﾞ`, `Gｱﾅｾﾙ` | — | — | `FIGT_A`, `Sﾌﾞﾀｲ`, `Gｴｸｽ`, `Gｼｨﾙ2` |
| `events` (milestone 2) | every other `G*`/`I*` file (128) | — | — | the `scenes` group's roots, `FIGT_A`, `FIGT_B`, `Sﾌﾞﾀｲ`, the `C*` files, `FIGT表示` |

Several roots share a group only where none of them calls another (checked against the
decompiled call graph, 2026-10-08 and 2026-10-10): inside a group a root is inner, so a call
between two of them would not get its own capture. The exception is `events`, whose scenes call
each other: there a scene called by another one of the group is logged inline (the replay runs
it inline too) and gets a capture of its own only when called from elsewhere. It is a group
apart from `scenes`, so the class table the `scenes` captures were recorded with stays as it was
(the replay of a scene capture runs its group's roots and meets every other file as a hook:
`SceneReplayTests`). Not roots yet: the title, ending and gallery files (`ｽﾀMENU`, `OVER`, `CG`,
…), which no milestone has reached.

## Capture file format

One UTF-8 text file per run of a root, LF line ends, one record per line, tokens separated by
single spaces. The harness writes `<group folder>\<stamp>-<seq>-<root>.golden.part` (`<root>` is
the file name without `.ADV`, plus `@<address>` for a label root, e.g. `MAIN@20`; `<seq>` counts
the session's captures) and renames it to `.golden` when the run ends normally; a run interrupted
by loading a save, a reset, a crash or the end of the session keeps the `.part` suffix and is
ignored by the tests. File names are written through the wide Windows API, so `Cﾃｷﾄｼ` stays
readable.

`<where>` is `<file>@<address>`: the scenario file (name from `xsys35dc.hed`) and the hex byte
address of the command inside it (the decompiler's `L_xxxxx` labels use the same addresses).

```
golden 2                                  # format version (1 = battle captures before F2; the parser reads both)
seed <n>                                  # sgenrand() seed of this session (XSYS35_SEED or the pid)
root <file> <where>                       # page-call root: callee, and the call site in the caller
root <file> <where> label                 #   or label root: the file and the label (<where> = <file>@<address>)
group <name>                              # the classes.txt group (absent in format 1: battle)
mem enter … end-mem                       # memory right after the root call / before the label's command
…events…
mem exit … end-mem                        # memory after the return (or at the jump, or at the next arrival at the label)
end return | end jump <file> | end label  # how the run left (end abort <reason>: dropped, stays .part)
```

### Events

| Record | Logged at | Meaning |
|---|---|---|
| `rnd <n> <result> <where>` | every `Y 4, n:` (also n = 0/1, which draw nothing) | the value stored in `RetVal` |
| `key <code> <where>` | every `IK 0`/`IK 1` | the key code stored in `RetVal` (0 while message skip is on) |
| `wait <ticks> <code> <where>` | every timed `Y 3, n:` | the key that ended the pause (0 = ran out), stored in `RetVal` |
| `menu <choice> <count> <where>` | every menu (`]`) | `choice` = 1-based index among the `count` items that were registered, 0 = cancelled (right click) |
| `music <track> <where>` | every `SR 0, var` | the track number stored in `var` |
| `pick <retval> <tmp0001> <where>` | the return (`%0`) of a pick file | the pair the core's `SelectCommandAsync` must return |
| `mouse <button> <x> <y> <count> <where>` | every `IM x, y` (format 2) | `button` stored in `RetVal` (16 = left, 32 = right, 48 = both; xsystem35 `event.c`), the pointer position stored in the two variables. The map and screens poll `IM` in tight loops (`COMMAND.ADV:63-77`), so `count` consecutive `IM` at the same `<where>` with the same three results form one record; the count is timing, not script behaviour |
| `cursor <new> <old> <where>` | every `IC new, var` (format 2) | `old` = the cursor shape set before, stored in `var`: an engine static (starts at 1), not in the memory dump (`COMMAND.ADV:80-92`, `WAO_MAP.ADV:19, 46`) |
| `cdpos <track> <min> <sec> <frame> <where>` | every `SC var` (format 2) | the CD position stored in `var..var+3` (`999` ×4 when nothing plays); `ｵｰﾌﾟﾆﾝｸﾞ.ADV:18`, `MMODE.ADV:262` |
| `date <y> <mo> <d> <h> <mi> <s> <wd> <where>` | every `ZT 0, var` (format 2) | the wall clock stored in `var..var+6`; only `Gﾜｵ.ADV:555` |
| `timer <n> <value> <where>` | every `ZT n, var`, n = 2..5 (format 2) | the timer value stored in `var`; only `XSPEC.ADV` (machine benchmark, before `SET`) uses `ZT 5` |
| `call <file> <where>` | page call into a hook file | followed by `mem hook-enter … end-mem` |
| `ret <file> <where>` | return from that hook | preceded by `mem hook-exit … end-mem` |
| `jump <file> <where>` | page jump (`&`) anywhere in the run | the capture ends (`mem exit`, `end jump <file>`) |

Inside a hook nothing is logged, so a hook's draws and inputs are invisible to the replay; the
hook-exit dump carries the generator state after them.

**Which commands hand the script a value.** Every command of the decompiled scenario (201
files) that stores something the script did not compute itself was checked (2026-10-08): `Y 3`,
`Y 4`, `IK 0`, the menus, `SR 0`, `IM`, `IC`, `SC`, `ZT 0`, `ZT 5` are logged as above (`ZT 2-4`
too, though unused). Not logged, because the scenario never uses them: `IK 2-6` (input state into
`RetVal`), `IX` (message-skip state), `IG` (raw keys). Everything else that writes variables
is computed from memory (`NB`, `NC`, `MH`, `US`/`UG`, …) or runs only inside `ﾛｰﾄﾞｾｰﾌﾞ`, which
is always a hook (`LP`, `LT`, `LD`, `QC`, …). `IZ` moves the pointer and stores nothing (its effect
shows in the next `mouse` record). `MAIN`, `COMMAND`, the `C*` and `S*` files and `Cﾒｲｷｭｳ` use
only `IM`, `IC`, `IZ`, `Y 4` and menus; `IK 0` and `Y 3` come from the helpers.

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
   `GetPlayingTrackAsync` from `music`, `PauseAsync` from `wait` (the ticks must match;
   `Y 3, 10000/10001` only set `ScriptContext.PauseCancelable`, the engine's `Y3waitFlags`, so
   neither the harness nor the core has a wait for them) and
   `SelectCommandAsync` from `pick`. Records are consumed in log order by all consumers together,
   so a draw where the original waited for a key (or the other way round) is reported as a
   divergence.
4. **Hooks.** `ReplayStoryHooks` meets each `call` record: the file must match, the core's memory
   is compared with the `hook-enter` dump (differences are reported as a checkpoint failure but
   the replay goes on), then the `hook-exit` dump is loaded, generator state included. A hook
   whose capture ends in `jump` throws `ScenarioJumpException` after loading the final dump.
5. **Entry point.** `FIGT_A.ADV` → `AttackProvinceAsync`, `FIGT_B.ADV` → `DefendProvinceAsync`,
   `FIGT.ADV` → `FightAsync`, `COM.ADV` → `WeeklyEvents.RunAsync`, `COMMAND.ADV` →
   `CommandPhase.RunAsync`, the `week` group's label root `MAIN.ADV@20` → `WeekLoop.RunWeekAsync`
   (`ReplayableLabelRoots`; a week with an army name of its own, `MS 10`, needs the player's files and is not replayed in the core yet), the `commands` group's `Cﾃｷﾄｼ.ADV`, `Cﾃｷﾄｼ2.ADV`, `Cﾌﾞｶ.ADV`,
   `Cﾊｰﾚﾑ.ADV` → `EnemyCityCommand`, `DistantCityCommand`, `SubordinatesCommand`, `HaremCommand`
   (`GoldenReplay.ReplayableRoots`, `CanReplay`; their paths beyond the prologue throw
   `NotSupportedException`). `Cﾒｲｷｭｳ.ADV` reads its `MS` strings from the player's files, so its
   replay is `RunAsync(capture, entryPoint: s => new DungeonCommand(s, scripts).RunAsync())`
   (`DungeonReplayTests` in the content tests); the
   same holds for the `screens` group (`Sﾌﾞﾀｲ`, `Sﾌﾞｶ`, `Sﾊｰﾚﾑ` → `DeploymentScreen`,
   `SubordinateScreen`, `HaremScreen`, which read their button labels and the harem's name table),
   replayed by `ScreenReplayTests` in the content tests, and for the `nations` group (`TCﾘｰｻﾞｽ`,
   `TCﾘｰｻﾞｽ2` → `RebelTurn`, `RebelDefenders`, which read the army name `MS 10`), replayed by
   `NationReplayTests`, which lists the roots still waiting for their part. The
   player phase, the command files and the pickers read the pointer as the script does, so their replays answer
   `GetMouseAsync` from `mouse` and `SetCursorAsync` from `cursor` (the shape must match); in
   battle captures both stay passive. Every other root has no entry point yet: `GoldenBattleTests.Capture_matches_the_original`
   replays only captures whose header (`GoldenCapture.ReadHeader`)
   names a replayable root; the others are parsed and loaded by
   `GoldenPendingTests.Capture_awaiting_its_port_is_well_formed`, and
   `Every_capture_has_a_replay_entry_point` is skipped with the list of waiting roots and counts.
   A part that ports a root adds its entry point here and to `ReplayableRoots`. Scene captures
   (group `scenes`) replay through the scene runtime: `RunAsync(capture, entryPoint: s =>
   s.CallFileAsync(root), scenes: hooks => new SceneRunner(scripts, hooks, roots.Contains))` —
   the group's roots run from the player's decompile, every other file stays a hook, and the
   input also answers `SelectAsync` from `menu` (the item count must match), `GetMouseAsync`
   from `mouse` (one record answers `count` reads), `SetCursorAsync` from `cursor`, `GetTimerAsync` from `timer`, `GetDateAsync`
   from `date` and `GetCdPositionAsync` from `cdpos` (`SceneReplayTests`).
6. **Result.** After the entry point returns (or throws `ScenarioJumpException` for a capture
   that ends in `jump` to the same file), every record must have been consumed and the memory is
   compared with the `exit` dump. When the port throws `NotSupportedException` (a path not ported
   yet, e.g. the Leazas week or a command file at stage 1), the comparison ends there: every event
   and hook checkpoint up to it matched, the rest waits for its port. The report passes and says
   so (`GoldenReport.Unported`: "matches the original as far as the port reaches"), as the
   differential tests compare an unported path up to where the port stops.
7. **Passive records.** In a battle, `mouse` and `cursor` records are skipped: the battle's `IM`
   loops only wait for the button's release (`FIGT.ADV:829, 1006, 1016, 1105, 1115`,
   `FIGT_B.ADV:47, 341`; `RetVal` ends 0, the position goes to the ignored `Ui_MouseX/Y`) and no
   battle file sets the cursor. Ports of mouse-driven files (the map, the screens) consume them;
   a replay through the scene runtime consumes `mouse`.

### What the comparison ignores, and why

Compared: all variables 0..805, every slot of every page — page 2 (the weekly history) and
page 10 (the map marker list) included — every string variable (string 10 included), the
generator state. Ignored — and listed in the report as "ignored" so they stay visible
(`GoldenFilter`):

- **The `ui` group of [variables.tsv](variables.tsv)** (variables and page fields), except the
  ones that are rule state despite the group (`GoldenFilter.RuleStateUiVariables`):
  - the weekly command flags `Ui_CmdHaremAvail`, `Ui_CmdSubordAvail`, `Ui_CmdOwnCityAvail`,
    `Ui_CmdBuildAvail`, `Ui_CmdAttackAvail`, `Ui_CmdMerchantAvail`, `Ui_CmdDungeonAvail` — reset each
    week (`COMMAND.ADV:38-44`), cleared by the commands (`FIGT_A` on a conquest, BFL-02; the top of `Cﾒｲｷｭｳ`), read by
    the command menu. (`CmdAvail_SubTroops`/`CmdAvail_SubTalk` are in the `time` group and were
    always compared.)
  - the harem list `HaremAvail_*` (rebuilt by `Cﾊｰﾚﾑ` each visit from story state) and
    `Harem_LastPick` (used as the history record cursor, `Cﾊｰﾚﾑ.ADV:560`);
  - `Ui_ClickedProv` (routes the click to a command file, `COMMAND.ADV:515`) and
    `Ui_CityScouted` (the ninja-scouted flag);
  - the marker list on page 10 (`Ui_MarkCg/X/Y` and their `Next` aliases) with `Ui_MarkCur`,
    `Ui_MarkNextCur`, `Ui_MarkCount`: the owner flags of the provinces, which the port keeps
    through the map operations (`M`/`WAO_MAP` op 4, `SET_MAP`).
  Un-ignoring all of them (F2, 2026-10-08) left the 37 battle captures matching. Still ignored
  among the `ui` variables that are not plain pointer/window state: `Ui_SquadMode`,
  `Ui_SquadChoice`, `Ui_SubordMode`, `Ui_ItemSel`, `Ui_ItemUnitSel` — the internals of the
  `Sﾌﾞﾀｲ`/`Sﾌﾞｶ`/`Sｱｲﾃﾑ` screens; revisit when P2 decides how the screens are ported (only their
  results, e.g. `Tmp0001`/`RetVal`, are certainly rule state). `Ui_ScrollWait` comes from the
  machine benchmark in `XSPEC.ADV` (`ZT 5`) and must stay ignored. Reasons for the rest: (1) these are the internals of the message windows, the map
  and the pointer (`WAO_SUB`, `M`, `WAO_MAP`, `FIGT_SEL`), which the Godot layer replaces, so a
  port would be dead code (the map and window operations are ported anyway, for the non-`ui`
  scratch values they leave and because the window state decides whether a key wait happens —
  see "Findings so far" — so their `ui` variables usually match too; `FIGT_SEL`'s are not); (2) several depend on the physical pointer and on timing (`IM` writes
  `Ui_MouseX/Y`; drag anchors, button cursors), which no input record can reproduce; (3) no rule
  in the battle reads them; (4) the hook-exit dumps resynchronise them, so a difference cannot
  leak into later rule state through a story scene.
- **`Glob_CgSeen`** (page 4, `CgSeen[n]` at `80 + n`). `SET.ADV:297` registers it with `ZG`,
  and the engine adds 1 to `CgSeen[n]` whenever CG `n` is read from the archive — only on a
  CG-cache miss (xsystem35 `cg.c:262-278`). Exact counts depend on the engine's cache, not on the
  script. The scenario reads them only as "≠ 0" in the CG gallery (`CG.ADV`, 150 CG numbers
  between 101 and 862) and in the start menu (CG 240), and no CG a battle can show is among them
  (checked against every unit, commander and province CG field and the constants in `FIGT` and
  `FIGT_SEL`). The first capture showed 17 battle CGs (frames, sprites, portraits) counted.
- **`Tmp0010`-`Tmp0015`, in battle captures only** (`GoldenFilter.Default`; every other group
  uses `GoldenFilter.Strategic`, chosen by `GoldenFilter.For`): `FIGT_SEL.ADV`'s target-frame
  colour and popup rectangle, computed from the hovered button and the pointer position. Inside a
  battle no other file uses them, and `FIGT_SEL` writes them before reading them. Outside the
  battle `FIGT_SEL` runs inside a hook, and other files use these scratch variables.
- Nothing else. A scratch variable (`Tmp*`, `RetVal`) that differs is a finding: the script has
  no locals and stale scratch values are observable (porting conventions).

If real captures show further pointer-dependent values, each one is added to the ignore list
here with its reason, never as a blanket rule.

### Findings so far

| Capture | Finding | Fix |
|---|---|---|
| boot-time smoke dump | `DC page, n` allocates `n + 1` slots | generator, `GameStateLayoutTests` |
| FIGT_A, prologue victory at Golak (2026-10-08) | `M.ADV` / `WAO_MAP.ADV` leave scratch values (`Tmp0021-0026`, …) that FIGT_A returns to its caller; `Y 3` stores the key that cut the pause short in `RetVal` | `ScriptContext.Map.cs` ports the map operations the battle reaches; every `Y 3` goes through `IScriptInput.PauseAsync` (`MapTests`) |
| same | `Glob_CgSeen` counters, `Tmp0010` | ignored (above) |
| static audit of `WAO_SUB.ADV` ahead of a FIGT_B capture (2026-10-08) | FIGT_B shows and removes portraits (ops 311/312/321/322/331/332); every removal runs `L_00b0f`, which leaves the clamped camera in `Tmp0021/0022`; the key waits (ops 1/11) skip `IK 0` while no window is open | `ScriptContext.Wao.cs` ports `WAO_SUB`'s state effects (`WaoSubTests`); the FIGT_A capture still matches, and its `ui` variables now match too except `Ui_BtnCur` (FIGT_SEL) |
| 5 more prologue FIGT_A, the Ruberan duel, FIGT_B city and plains intercepts with `Sﾌﾞﾀｲ`/`Gｴｸｽ` hooks (2026-10-08) | none — all match | — |
| 10 stage-1 battles: city attacks that take a province, `Gｴｸｽ` inside FIGT_A, an abandoned and a lost defence (2026-10-08) | none — all match | — |
| 3 more: Focus, Rance Attack and By la Way in an attack that runs out of rounds; two FIGT_B on Ys (2026-10-08) | none — all match | — |
| 14 more: captures with `死亡捕獲` hooks, ten dungeon battles (root `FIGT.ADV`), the `Iﾏｼﾞﾝ` defences, a game over (2026-10-08) | none — all match | — |
| enemy info and the player's retreat (2026-10-08) | none — matches | — |
| F2: the weekly command flags, the harem list, `Ui_ClickedProv`, `Ui_CityScouted` and the page-10 markers compared (2026-10-08) | none — all 37 battle captures still match | — |
| F2: boot capture (`SET.ADV`, windowless run to the title, 2026-10-08) | no draws, no input; one hook (`ﾛｰﾄﾞｾｰﾌﾞ` with `RetVal 10`); the exit dump has the generated layout (`GoldenPendingTests`) | replayed through `GameSetup.SetAsync` with the player's `SET_*` files (`PlayerBootTests` in the content tests, `GoldenReplay.RunAsync(entryPoint:)`): 0 differences at the hook and at the exit; the layout is checked on the exit dump, since SET's own `DC`/`DS` create it |
| The owner's prologue run (2026-10-09, seed 50114115; 113 complete captures, two `.part` from quitting): a new game, weeks 9-12 at stage 0 with every command (harem with Sill, Soul and Anasel, subordinate talks, the hideout and two of the spots 2/3/15, `Cﾃｷﾄｼ2`, pillages of 33/34 won and lost, the party screen), a save in week 11, path B (Merci, the duel lost, `ｵｰﾌﾟﾆﾝｸﾞ`, `Iﾊﾝﾗﾝ`), the first Leazas week with its nation turn `TCﾘｰｻﾞｽ` and two FIGT_B defences | **none — every capture matches**: 4 `MAIN@20` weeks (the stage-0 → stage-1 transition included), 6 `COM`, 5 `COMMAND`, 26 command files, 30 picker screens, 22 scenes, 6 dungeon clicks, 7 battles (all 44 battle captures still match), the boot. Five stage-1 captures (the Leazas `MAIN@20`, `Cﾃｷﾄｼ` attack, `Cﾌﾞｶ` troop menu ×2, the later harem) match up to the unported path. Not played, so still open: path A's week-23 Turn END (README open item 1) and a click near the map's top-left corner (item 2); `TCﾘｰｻﾞｽ` waits for N1 | the replay ends the comparison at a `NotSupportedException` (`GoldenReport.Unported`, above); the `dungeon` group is now replayed (`DungeonReplayTests`) |
| The owner's third run (2026-10-09, seed 1323388227): pillages of 33 and 34, a save in week 11, Turn END to week 23 (path A; province 32 clicked in week 18), the duel lost, quit during `ｵｰﾌﾟﾆﾝｸﾞ` | none — all 56 complete captures match (14 weeks, 15 `COM`, 14 `COMMAND`, 4 command files, 2 pickers, 19 scenes, 3 battles, the boot). The duel ran with `Prov_Cur = 32`: EVT-08 confirmed (README open item 1 answered) | — |
| The owner's second run (2026-10-09, seed 502748417): a new game, week 9 ended, in week 10 two clicks at the map's top-left corner ((9, 5), (11, 6)), Turn END | none — all 14 captures match. Both clicks pick province record 0 and run `Cﾃｷﾄｼ2` on it (STR-12 confirmed; README open item 2 answered) | — |
| The Leazas week ported (milestone 2 step 1, 2026-10-10) | the owner's Leazas week of the first run (`week61009-105401-92-MAIN@20`, week 13, stage 1) now matches whole: the autosave, `COMMAND`, `TCﾘｰｻﾞｽ` and `Gｼｨﾙ` 21 hook checkpoints and the exit, 0 differences | — |
| The rebels' turn ported (milestone 2 step 2, 2026-10-10) | the owner's rebel turn of the first run (`nations`, `20261009-105433-107-TCﾘｰｻﾞｽ`, week 14: `Gｴｸｽ` 10, then `FIGT_B` from army A's walk and from army B's) matches whole: three hook checkpoints and the exit, 0 differences (`NationReplayTests`) | — |

## Harness configuration

`tools/golden/capture.ps1` sets the environment, makes the folder of every group in
`classes.txt` (the harness cannot make one itself on a drive path: xsystem35's `mkdir_p` gives up
at `E:`, whose `mkdir` fails with `EACCES`) and starts the patched build in the game folder:

| Variable | Meaning |
|---|---|
| `XSYS35_SEED` | decimal seed for `sgenrand()` instead of the process id |
| `XSYS35_GOLDEN_DIR` | output folder; capture is off when unset |
| `XSYS35_GOLDEN_HED` | path of `xsys35dc.hed`: page `n` is the `n`-th file name after the `#SYSTEM35` line |
| `XSYS35_GOLDEN_CLASSES` | [tools/golden/classes.txt](../tools/golden/classes.txt), the groups (format below; a UTF-8 file, because the names are not ASCII and Windows hands the environment to the program in the ANSI code page) |
| `XSYS35_GOLDEN_TRACE` | `1` = also append every page call/return/jump and label arrival of the session to `trace.log` |
| `XSYS35_RNGLOG` | `1` = print every draw to stdout, `rnd <k>: Y 4, <n> = <result> <page>@<addr>` (`capture.ps1 -RngLog`, which pipes the GUI program's stdout to its console); the port prints the same lines without the location with `--rnglog` (Rider's Console tab).|
| `XSYS35_RNGLOG_FILE` | the same lines also into this file, emptied at start-up (`capture.ps1 -RngLog`: `%APPDATA%\Godot\app_userdata\Kichikuou\rnglog-original.txt`, beside the port's `--rnglog` file `rnglog.txt`). `python tools/golden/rngwatch.py` watches both files during a parallel run and reports the first draw that differs at once (console, beep, a message box on top), starting over when either program restarts. With the same seed (`-Seed N` / `--seed N`) and the same play both lists agree: the original draws nothing before `Iﾌﾟﾛﾛｰｸﾞ` while page 4 records no clears (else `ｽﾀMENU.ADV:62, 67` draw twice), checked on the owner's three sessions |

`classes.txt`, one directive per line, `#` starts a comment line:

```
inner|pick <file> …          before the first group: defaults of every group (the UI helpers)
group <name> [<dir>]         starts a group; its captures go to <dir> under the output folder
                             (default <name>; "." = the output folder itself — the battle group)
root <file> …                page-call roots of the group (inner to it)
root <file>@<hexaddress>     a label root (one per group)
inner|pick <file> …          the group's own classes; within a group the last line naming a file wins
```

The harness's hooks are `src/golden.c` plus one call at each logged command (`cmdy.c` `Y 3/4`,
`cmdi.c` `IK`, `IM`, `IC`, `selection.c` menus, `cmds.c` `SR`, `SC`, `cmdz.c` `ZT 0/2-5`,
`cmd_check.c` `&`), two observers in `scenario.c` (page call entered, page return resumed) and
the label check in `nact.c` (before each command of a page that holds a label root). Loading a
save (`savedata.c`) or a reset drops every open capture with `end abort <reason>` (left as
`.part`).

Verified without playing (F2, 2026-10-08; windowless runs to the title menu with a fixed seed):
the shipped `classes.txt` yields exactly one complete capture, `boot\…-SET.golden`, and no `.part`
file; a test configuration (SET with `SET_MAP` as a hook, `SET_MAP` and `M` as roots of their own,
`XSPEC` as a page-call root and `XSPEC.ADV@e4` — its benchmark loop — as a label root) gave the
expected nesting: `SET` with two hook calls, `SET_MAP` with 90 `M` hook calls, 90 `M` captures,
one `XSPEC` capture with 2704 `rnd` and 8 `timer` records, and 52 label captures, 51 ending in
`end label` and the last in `end return`; a katakana root (`ﾌﾟﾛﾛｰｸﾞ`) got a readable file name,
and a label root on the title menu's `IM` loop logged `mouse 0 0 0 1 ｽﾀMENU.ADV@26f`. Nothing of
the strategic layer could be played this way (it needs input): the first real captures come
from the owner's session below.

Verified on the real game (2026-10-08), with `SDL_VIDEODRIVER=dummy SDL_AUDIODRIVER=dummy` for a
windowless run: the trace names the boot sequence correctly (`ｽﾀｰﾄ.ADV` calls `XSPEC.ADV`, then
`SET.ADV`, which calls `SET_MAP.ADV`, …), so the name table is right; a dump taken at the first
`PCMPLAY.ADV` call after `SET.ADV` parses, its `DS` bindings equal the generated `GameState`
layout, and it fits the core's memory. That dump showed that `DC page, n` allocates `n + 1` slots,
which the core did not do (fixed in `tools/codegen/gen_state.py`).

## Battles to capture

Each `.golden` file becomes one test case. Captured so far (all match the original):

| Capture | Battle | What it exercises |
|---|---|---|
| `095048-01`, `102936-01` … `103255-05` | FIGT_A from `Iﾄｳｿﾞｸ`, prologue: Rance's party vs the Civilian General at Golak / Vladivostok, victory | direct and magic attacks, enemy commander death, EXP, `M.ADV` map ops |
| `103304-06` | FIGT_A from `Iﾄｳｿﾞｸ@198f`, scripted duel vs Ruberan, defeat (`Bt_IsDuel = 1`) | slot orders (FIGT_SEL 51), player commander deaths, `死亡捕獲`, `Prov_Cur := 1` |
| `103751-07` | FIGT_B, Pegasus/Dodge/Melfeis attack North, `Sﾌﾞﾀｲ` returns 502 (city defence) | intercept setup, portraits, city walls, victory |
| `103847-08` | FIGT_B, Ex's army attacks Wes; hooks `Gｴｸｽ`, then `Sﾌﾞﾀｲ` returning 501 (plains) | story hook before the battle, plains intercept, slot order 52, player commander death (Rick) |
| `105112-01` … `105637-03`, `110045-05`, `110306-08` | FIGT_B intercepts at North, Wes, Baranchi, Ys (stage 1, with and without `Gｴｸｽ`) | more intercepts, ninja scouting, slot orders 51/52 |
| `105457-01` | FIGT_A from `Cﾃｷﾄｼ`, Rance/Kinkaid/Asuka take Baranchi from Ex's army | city attack after the prologue, province changes owner |
| `105900-04` | FIGT_A from `Cﾃｷﾄｼ` on Ys with the `Gｴｸｽ` hook; no command chosen | story hook inside FIGT_A, Rance's level-up |
| `110203-06` | FIGT_B on Ys, defence abandoned, province lost without a fight | abandon branch, `ProvinceLostAsync` |
| `110240-07` | FIGT_B on North with Asuka alone, defeat, province lost, Asuka dies | lost defence, player commander death |
| `111109-01` | FIGT_A from `Cﾃｷﾄｼ` on Oaks with the `Gｴｸｽ` hook; the rounds run out (`Bt_Result = 1`) | Focus, Rance Attack, By la Way, slot order 50, unit info, enemy commander death (Pegasus), attacker lost |
| `111501-02`, `111600-03` | FIGT_B on Ys: a victory, then a defeat that loses the province (Kinkaid dies) | intercepts, lost defence |
| `113558-01` | FIGT_A from `Cﾃｷﾄｼ` on Oaks, victory; Melfeis and Haurein captured, then `Iｼﾚｲﾌﾞ` | hooks from `死亡捕獲` (`Gﾒﾙﾌｪｲｽ`, `Gﾊｳﾚｰﾝ`) and FIGT_A's slave-event hook, story stage changes |
| `114046-02` … `114217-11` | `FIGT.ADV` called by `Cﾒｲｷｭｳ` (dungeon): Rance alone vs Sergeant Hanny, and the boss Marder (`-03`) | dungeon battles (root `FIGT.ADV`), commander already at status 0 |
| `114319-12`, `114531-13` | FIGT_B called by `Iﾏｼﾞﾝ` on Leazas Castle: Monster Generals, then La Seizel | monster army defence, long battle (966 draws) |
| `115148-14` | FIGT_A from `Cﾃｷﾄｼ` on Log B, Rance alone vs the Helman army, dies | game over: the capture ends with `jump OVER.ADV`; enemy bow attacks |
| `125124-01` | FIGT_A on Log B again: enemy info twice (FIGT_SEL 54, slots 1 and 3), then the player retreats | enemy info, player retreat and its confirmation menu |

Coverage of the battle code by these replays (2026-10-08, `--collect "Code Coverage"` on
`GoldenBattleTests`, 37 captures): never reached are the other specials (beams, High Speed,
Chuhai, Vanish Bomb, War Dance, Soaring Wave), the player's bow attacks (enemy bow attacks are
covered), Fog Card, the kaiju summons, Goliath, the ninja follow-up, the Land of Death, the
defence refusal branch, `RaiseModifierAsync`.

Still wanted: one of the player's units with a bow attack type using Bow (FIGT_SEL 2); other
specials, a kaiju summon or Fog Card when available.

The uncaptured paths were checked line by line against the decompiled scenario instead
(2026-10-08), with no difference found: the kaiju summons and Fog Card (`FIGT.ADV:1149-1256`,
the dispatch `:641-775`, `L_00e0d` keeping `Unit_Cur = 51`), Goliath (`:1120-1148` and its
trigger `:327`), the ninja follow-up (`:1915-1986`, the nested `L_02d45` with its own R14 draw),
the Land of Death (`:217-281`, `L_04eba`), every special setter (`:2339-2528`), the special
overrides of the exchange (`:2324-2413`) and `L_03f93`'s beam/Chuhai lines (`:2564-2622`), the
attack sounds (`FIGTATTK.ADV:256-345`), the defence refusal and `L_01270` (`FIGT_B.ADV:84-150`,
`:884-901`) and the modifier roll (`FIGT.ADV:3100-3160`). A capture of any of them still
supersedes this check. A battle starts with focus 0 and the specials are
listed only with focus > 0 (01-flow §8.2), so Focus ("Wait") comes first.

Start the harness with

```
powershell -ExecutionPolicy Bypass -File tools\golden\capture.ps1
```

and play normally; loading a save before a battle is fine (the capture starts at the battle's
page call). `python tools/golden/describe.py <file> …` prints what a capture holds (root, group,
week / stage / gold / province before and after, hooks, event counts, mouse presses; for a battle
also the parties, result and commands) to check it against what was played. Run the tests with
`dotnet test tests/Kichikuou.Core.Tests`; a failing capture prints the divergence (with the
original's `file@address`) or the differing variables by name.

## Capturing the prologue (milestone 1) — checklist for the owner

One session of `capture.ps1` records every root at once; nothing needs switching. Quit the game
normally at the end (open captures stay `.part`; that is expected). What to play so that every
root of milestone 1 ([strategy/README.md](strategy/README.md), "What the prologue reaches") is
captured at least once — the captures named after each step are what to look for afterwards
(`describe.py`):

1. **Start the game and begin a new game** (title button 6). Captures: `boot\…-SET` (at start-up),
   `scenes\…-Iﾌﾟﾛﾛｰｸﾞ`, then a `week\…-MAIN@20` per week from here on.
2. **Week 9 (the first week).** `COM` plays `Iｼﾚｲﾌﾞ` 10 by itself (`com\…-COM` with an
   `Iｼﾚｲﾌﾞ` hook, `scenes\…-Iｼﾚｲﾌﾞ`). In later weeks `COM` plays `Gｿｳﾙ` 4.
3. **In the first weeks, every command once** (each week offers each command once):
   - **Harem** with Sill (`Gｼｨﾙ` 10) one week and Soul (`Gｿｳﾙ` 10) another — `commands\…-Cﾊｰﾚﾑ`,
     `screens\…-Sﾊｰﾚﾑ`, `scenes\…-Gｼｨﾙ`/`Gｿｳﾙ`. Either sets the flag Anasel's capture needs.
   - **Subordinates**: talk to Bound, Soul and Rance (one talk per week; also Sill if time
     allows), and open the status view once after a talk — `commands\…-Cﾌﾞｶ`, `screens\…-Sﾌﾞｶ`,
     `scenes\…-Gﾊﾞｳﾝﾄﾞ`/`Gｿｳﾙ`.
   - **Click the thieves' hideout** (dungeon 1; the banter draws `Y 4, 3`) and once each of the
     dungeon spots 2/3/15 if they show — `dungeon\…-Cﾒｲｷｭｳ`. Also use the menu item
     "→Thieves' Hideout" once (camera pan only).
   - **Click a province other than 33/34** (view only) — `commands\…-Cﾃｷﾄｼ2`.
   - **Pillage Vladivostok (33)**, win; another week **pillage Golak (34) and lose** (a weak party
     or retreat); once **cancel the party screen** instead of choosing a party —
     `commands\…-Cﾃｷﾄｼ`, `scenes\…-Iﾄｳｿﾞｸ`, `screens\…-Sﾌﾞﾀｲ`, battle captures in the top folder.
   - **Click near the top-left map corner** once (open item 2 of the prologue sweep: the hit test
     starts at index 0).
4. **Anasel.** After a Sill or Soul harem scene, **win a pillage of Golak** → Anasel is captured
   (`Iﾄｳｿﾞｸ`, `F_Thieves_State` 1 → 2). Then **Harem with Anasel** (`scenes\…-Gｱﾅｾﾙ`, state 2 → 3).
5. **Save** (menu Load/Save → save to a slot): `ﾛｰﾄﾞｾｰﾌﾞ` appears as a hook in the `COMMAND`
   capture. Note the week.
6. **Path B (Merci).** **Win a pillage of Golak** again → the Merci path, the duel against
   commander 31 — **lose it** (the story continues; winning is game over, ending 14) →
   `ｵｰﾌﾟﾆﾝｸﾞ`, then `MAIN` sets stage 1 and plays `Iﾊﾝﾗﾝ` (`scenes\…-ｵｰﾌﾟﾆﾝｸﾞ`, `…-Iﾊﾝﾗﾝ`; the
   week capture of that week shows both). **Play the first Leazas week to its Turn END**: income,
   `COM` with stage 1, the nation turns (`nations\…-T*`), a full `MAIN@20` week.
7. **Load** the save from step 5 (the load aborts the open captures — expected — and the week
   capture restarts at the next week). **Path A**: do not pillage Golak again; **Turn END every
   week until week 23** → `Iﾄｳｿﾞｸ` 2 and the duel (no `Prov_Cur` is set first — open item 1 of
   the sweep: note which province was clicked last), **lose it** → `ｵｰﾌﾟﾆﾝｸﾞ`, `Iﾊﾝﾗﾝ`.
8. **Optional:** load again and **win the duel** once (game over, ending 14: `Iﾄｳｿﾞｸ` ends in
   `jump OVER.ADV`; the game quits itself). A battle in the dungeon is not part of the prologue.

Afterwards run `dotnet test tests/Kichikuou.Core.Tests`: the battles must still match, the new
captures are parsed (`GoldenPendingTests`) and listed as waiting for their ports; and
`describe.py` on the `week\` captures to check the weeks against what was played.

## Capturing stage 1 (milestone 2) — checklist for the owner

What stage 1 reaches is in [strategy/README.md](strategy/README.md), "Milestone 2". One session of
`capture.ps1` again records every root at once; the port cannot play stage 1 beside it yet, so
this is a session of the original alone. Loading a save is fine (it
aborts the open captures; the next ones start normally). Note the week of each step; the `events`
folder will fill with a capture per scene.

1. **Reach stage 1**: load a save from the end of the prologue, or play path B quickly (harem with
   Sill or Soul, a won pillage of Golak for Anasel, harem with Anasel, a won pillage of Golak, the
   duel lost).
2. **The first Leazas week.** `COM` plays `Iｼﾚｲﾌﾞ` 1. **Click a rebel province** (3, 4 or 7: the
   attack is refused this week, `Cﾃｷﾄｼ:144`). Turn END: the rebel phase `TCﾘｰｻﾞｽ` (`Gｴｸｽ` 10;
   if the rebels attack, defend), `Gｼｨﾙ` 21 at the end of the week.
3. **In the following weeks, every command at least once** (most are once a week):
   - **Attack** a rebel province (4 or 7) and win; in the same week **try a second attack**
     (refused after a conquest, `Cﾃｷﾄｼ:112`); another week **cancel the party screen** once, and
     **lose or retreat** once. Take province 5 — but **keep province 6 for step 5**.
   - **Click provinces 17 or 29 and 31 or 32** (Maris refuses: "civil war") and an enemy province
     farther away (`Cﾃｷﾄｼ2`).
   - **Ninja** (Kanami, from an enemy city's menu): Collect Info one week, Sabotage another, and
     "No Mission" once.
   - **Subordinates**: Add New Troops (with Add More once), Replenish after a battle, a talk with a
     new unit (Leila, Rick, Kinkaid, …), Rance's monologue, Info.
   - **Harem**: a few visits (Lia, Maris, Kanami, …) and one cancel.
   - **Own city** (click an own province): Temporary Levy on province 1 and on two others (e.g.
     11 and 15); Bolster Defense once; on province 1 the dungeon cell.
   - **Merchant**: the first meeting; a mercenary if the gold allows; an item; Send Away.
   - **Construction**: build the Library (400) and play on until it is finished (8 weeks: its
     scene runs at the start of a week).
   - **Treasure**: the item screen; equip and unequip an item if there is one.
   - **All Data**: the graphs, and change a scale once. **Think** once.
   - **Dungeons**: the Monsters' Cave (province 14) — a search of a few floors with a battle, then
     retreat; a click on dungeon 3 (province 1) and on dungeon 15 (province 11).
   - **Save** once (Load/Save, a slot) and **load** it once later from the menu.
4. **The rebels' attacks**: defend once by intercepting; optionally abandon a province once.
5. **The end of the stage**: take province 6 → `Iｼﾚｲﾌﾞ` 2 (stage 2; `Gｴｸｽ` 4 asks a question —
   any answer, note which). Turn END; the next week `GAL` 1, and at its end `Iﾏｼﾞﾝ` 1 with two
   defences of Leazas Castle (win them). Play the following week's player phase, then quit.

Afterwards the same checks as for the prologue: `dotnet test` for both test projects (the scene
and `events` captures replay through the runtime at once; the rest wait for their ports) and
`describe.py` on the `week\` captures.
