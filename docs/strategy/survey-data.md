# Survey — data initialisation (`SET*.ADV`)

Scope: `SET.ADV` (348 lines), `SET_CHR.ADV` (7339), `SET_ITEM.ADV` (351), `SET_MAP.ADV` (2337),
`SET_VER.ADV` (704); plus the boot/new-game glue they hang off (`ｽﾀｰﾄ.ADV`, `ｽﾀMENU.ADV`,
`ﾛｰﾄﾞｾｰﾌﾞ.ADV` mode 10). Line citations are to `_extract\scenario_named\`.
Confidence: **[C]** confirmed by reading (or by a script run against the files/captures),
**[I]** inference, **[G]** guess.

## 1. Role

The five files build the **initial world** once per process, at boot, before the title menu:
`SET.ADV` sets ~130 plain variables, allocates the array pages and binds the record fields onto
them (`DC`/`DS`), then calls `SET_MAP` (91 provinces + 26 dungeons), `SET_CHR` (50 player-side
units + 197 enemy commanders) and `SET_ITEM` (50 items); every record also gets its name strings
written into the string table. Nothing in these files reads the story state, draws a random
number, or waits for input: they are **data in imperative form** — 7,437 of the 9,958 live
lines of the three record files are `!field : constant!`. A new game starts from this boot state plus three values set by
the title menu; a loaded game replaces it from the save file. `SET_VER.ADV` is **never called**:
it is a compiler artefact that fixes the variable numbering (§2.5).

## 2. Inventory

### 2.1 When the group runs [C]

| Moment | Path | Effect on this group's data |
|---|---|---|
| Process start | `ｽﾀｰﾄ.ADV:1-846` zeroes `VAR0000-0804`, `:851` calls `XSPEC.ADV`, `:871` `%#SET.ADV:`, `:872` `&#ｽﾀMENU.ADV:` | `SET.ADV` and its three callees run **exactly once** (the only caller of `SET.ADV` is `ｽﾀｰﾄ.ADV:871`; nothing jumps or calls back into `ｽﾀｰﾄ.ADV`) |
| New game ("From 1", the only entry when `DebugMode = 0`) | `ｽﾀMENU.ADV:244-291`: `Week := 8`, `StoryStage := 0`, `Gold := 50` (`:287-289`), `Iﾌﾟﾛﾛｰｸﾞ.ADV`, `&#MAIN.ADV` | boot state used as is |
| Debug starts "From 2/3/4" (`DebugMode ≠ 0`, `ｽﾀMENU.ADV:246-266`) | `:292-303` (week 12, stage 1), `:304-359` (week 16, stage 2, gold 30000, roster edits), `:360-…` (week 77, ~40 story flags) | overwrite parts of the boot state; out of scope for a faithful port except as test fixtures [I] |
| Load from the title menu or `COMMAND.ADV:374` | `ﾛｰﾄﾞｾｰﾌﾞ.ADV` `LD n` | whole memory replaced from the save (pages saved with flag 1: 1, 2, 4, 10 — see §4.4) |
| Game over / ending | `OVER.ADV:2083`, `HAPPY_C.ADV:91` `ZZ0, 0:` = quit the program (xsystem35 `cmdz.c:343-354`) | no in-process "new lap": a new run is a new process, so a new boot |

There is **no "lap" re-initialisation**. What survives between runs is array page 4 (endings,
CG gallery, title-menu unlocks), which `SET.ADV:339-340` reloads from every save slot through
`ﾛｰﾄﾞｾｰﾌﾞ.ADV` mode 10 (`L_00a7b`, `:16-19`: `LP slot, Glob_Fate, 2100` for each slot whose
header says it exists) **[C]**.

### 2.2 `SET.ADV` — boot procedure (one straight-line block, one condition)

| Lines | Purpose |
|---|---|
| 1-31 | Plain variables: camera, battle slot states, `VAR0055 := 100`, `FadeSpeed := 4`, `N_Kanami_State := 1`, `LadyCaravan_Prov := 40`, `Bt_BackgroundCg := 900`, `BgmEnabled := 1`, `Ui_ItemViewMode := 1`; the rest 0 |
| 32-124 | Placeholder strings (`MS 1` "General's Name", `2`, `4`, `5`, `6`, `7`, `8`) and every record field set to 0 — **before** the fields are bound, so these write the plain variable slots (`Prov_BattleArea := 500` at `:104` is the only non-zero one; §7) |
| 125-134 | Record counts and cursors: `Unit_Count 52`, `Cmdr_Count 220`, `Prov_Count 100`, `Item_Count 50`, `Dgn_Count 30`, cursors 0 |
| 135-225 | `DC 1, 20000, 1` and the `DS` bindings of units (32 fields), commanders (24), provinces (15), items (3), dungeons (5); `DF Unit_Troops, 20000, 0` clears page 1 |
| 226-255 | History/harem counters, `DC 2, 1800, 1`, `DS` of `Hist_*` and its alias cursor `Hist_PrevCur` |
| 256-281 | `DC 3, 7000, 0` (not saved): `Buf_*` scratch buffer (alias `VAR0181/0182`) and `Ui_Btn*` button rectangles |
| 282-297 | `DC 4, 2100, 1` persistent page: `Glob_Fate` @0, `Glob_CgSeen` @80, `Glob_MenuItem` @2080; `DF` clears it; `ZG Glob_CgSeen` registers the engine's CG load counter |
| 298-301 | String bases: `Unit_StrBase 20`, `Cmdr_StrBase 176`, `Prov_StrBase 836`, `Item_StrBase 936` |
| 302-327 | Pointer/drag state; map marker list `DC 10, 300, 1` (`Ui_Mark*`, alias `Ui_MarkNext*`) |
| 328-331 | `%#SET_MAP`, `%#SET_CHR`, `%#SET_ITEM`, `%#ﾓｼﾞｷｬｯｼｭ` (glyph cache, display only) |
| 332-338 | `ZD 0, 1` (debug-message switch); six map tile CG numbers `Ui_MapCg* = 812, 801-805` |
| 339-340 | `RetVal := 10`, `%#ﾛｰﾄﾞｾｰﾌﾞ.ADV` — load page 4 from the save files |
| 341-347 | `Ui_ScrollWait := 2/1/0` by `SpecGraphics / SpecBaseline` (machine benchmark from `XSPEC.ADV`) — the only condition in the group |

### 2.3 `SET_CHR.ADV` — units and commanders [C]

| Lines | Records | Block shape |
|---|---|---|
| 1-1950 | units 1-50 (39 lines each) | `ZH 1` / `MS 1, "<name>"` / `R` / `MS 2, "<troop type>"` / `!Unit_Cur : n!` / `MM Unit_StrBase+(Unit_Cur-1)*3+0, 1` / `…+1, 2` / 32 field sets |
| 1951-7269 | commanders 1-199 except 61 and 99 = 197 records (27 lines each) | same with `MS 4/5`, `Cmdr_Cur`, `Cmdr_StrBase`, 20 field sets |
| 7270 | `%0:` | return |
| 7271-7339 | 23 × `!Ui_WaoOp : n!` / `%#WAO_SUB.ADV:` / `\0:` | **dead**: after the return and with no label (the decompiler emits labels only for referenced addresses) — the usual dialogue-stub tail of the file template |

Every unit sets all 32 fields; every commander the same 20 (never `Cmdr_Item`, `VAR0126`,
`VAR0127`, `VAR0129`, which stay 0 from `DF`). All values are decimal constants except
unit 32 (Miki) `Unit_HP`/`Unit_MaxHP : 10000 * 3` (`:1239-1240`). Units 51-52 and commanders
61, 99, 200-220 are never initialised; unit 51 is the battle's summon slot (`FIGT.ADV:1126`),
commander 100 is overwritten by the dungeon (`Cﾒｲｷｭｳ.ADV`, 49 × `Cmdr_Cur : 100`), the Temple
Knights (139-142) by `GAL.ADV` raiders.

Boot values worth knowing: units with `Unit_Status 1` = 1-4 (Rance, Sill, Bound, Soul), 46 at 0;
commanders `Cmdr_Status 1` = 190, at 0 = 7 (Sieg, La Vaswald, slot 100, PG-9, Silky, Satella,
Meglass). Class histogram — units: 0×29, 1×3, 2×10, 3×3, 10×2, 11×3; commanders: 0×139, 1×7,
2×33, 5×2, 10×13, 11×3.

### 2.4 `SET_ITEM.ADV` and `SET_MAP.ADV` [C]

| File, lines | Records | Block shape |
|---|---|---|
| `SET_ITEM` 1-350 | items 1-50 (7 lines each) | `!Item_Cur : n!` / `ZH 1` / `MS 7, "<name>"` / `MM Item_StrBase+Item_Cur-1, 7` / `Item_Holder`, `Item_Effect`, `Item_Amount` |
| `SET_ITEM` 351 | `%0:` | |
| `SET_MAP` 1-2176 | provinces 1-91 (24 lines; Nothing (75) has 16) | `!Prov_Cur : n!` / `ZH 1` / `MS 8, "<name>"` / `MM Prov_StrBase+Prov_Cur-1, 8` / 12 field sets / `#L_03bf2, 1:` / `F3, Prov_MapX, (Prov_Cur-1)*2` / `F3, Prov_MapY, …+1` / `MapTargetX/Y := Prov_MapX/Y` / `Tmp0021 := Prov_Owner + 819` / `CallArg := 4` / `%#M.ADV:` |
| `SET_MAP` 2177-2332 | dungeons 1-26 (6 lines) | `!Dgn_Cur : n!` + `MapX`, `MapY`, `State` (all 0), `SavedFloor` (all 0), `Prov` |
| `SET_MAP` 2333-2337 | `%0:`, `*L_03bf2:` `_L_03bf6:` (address word), `*L_03bf6:` `[1638, 408, …]` | the inline word table: 100 (x, y) pairs; pairs 75 and 92-100 are (0, 0) |

`#L_03bf2, 1` sets the data-table pointer indirectly through the address word at `L_03bf2`
(xsystem35 `cmd_check.c:94-105`), `F3 var, k` reads word `k` (`cmdf.c:110-117`). Province 75
("Nothing", owner 1, no adjacencies) skips the map position and the marker (`:1777-1792`).
`Prov_Owner` at boot: 1×17, 3×13, 4×25, 6×3, 7×18, 8×7, 9×7, 11×1. `VAR0136` is set to 0 for
every province; `VAR0144` never (both `unused` in the dictionary).

Two item/unit pairs disagree at boot **[C]**: item 6 (L. Holy Sword) has `Item_Holder 1` while
Rance's `Unit_Item` is 0 until `Iﾊﾝﾗﾝ.ADV:32`; Kentarou (unit 31) has `Unit_Item 2` while item 2
has `Item_Holder 0` until `Gｹﾝﾀﾛｳ.ADV:1636-1637`. Consistent with "holder set = equipped";
reproduce as is.

### 2.5 `SET_VER.ADV` — dead, a numbering file [C]

704 lines: 703 × `!var : 0!` and `%0:`. No file calls it (only `xsys35dc.hed:4` names it;
grep for `SET_VER` over all 201 files finds nothing else). It assigns 0 to `VAR0043-0805` except
70, 73 and most of 163-225 (the history, buffer, string-base and marker variables) — including the record cursors and counts
(`Unit_Count := 0` would wreck the layout if it ever ran). The first appearances of the new
variables `VAR0226…0805` occur in strictly increasing order, so this is the file that fixes the
variable numbering at compile time (System 3 numbers variables by first appearance in page
order; page 2 = right after `SET.ADV`) **[I, strong]**. Port: nothing. The brief's "flag reset /
new game" reading is wrong — `ｽﾀｰﾄ.ADV:1-846` does the zeroing.

## 3. Call graph

| From | To | Count | Notes |
|---|---|---|---|
| `ｽﾀｰﾄ.ADV:871` | `SET.ADV` | 1 | only entry |
| `SET.ADV:328-330` | `SET_MAP`, `SET_CHR`, `SET_ITEM` | 1 each | |
| `SET.ADV:331` | `ﾓｼﾞｷｬｯｼｭ.ADV` | 1 | draws text into a hidden surface to warm the glyph cache; no `!` assignments (display only) [C] |
| `SET.ADV:340` | `ﾛｰﾄﾞｾｰﾌﾞ.ADV` (mode 10) | 1 | page 4 from save files (`LP`) |
| `SET_MAP` (90 provinces) | `M.ADV` op 4 → `WAO_MAP.ADV` op 4 | 90 | append the owner flag (CG 820-830, +10000 = raised) to the marker list on page 10 |
| `SET_CHR:7272-7338` | `WAO_SUB.ADV` | 23 | unreachable |
| `SET_ITEM`, `SET_VER` | — | 0 | |

No calls to `FIGT*`, `COM.ADV`, story scenes or `T*`. The marker op is already ported, as private
members of the battle: `ScriptContext.MapAsync` case 4 and `MapWaoMap` case 4 / `MapFindMarker`
(`src/Kichikuou.Original/Script/OriginalScriptContext.Map.cs`, `M.ADV:118-124`, `WAO_MAP.ADV:110-116, 183-196`).

## 4. State

### 4.1 Written

| Group (dictionary) | Written by | What |
|---|---|---|
| `unit` page 1, 52×32 slots | `SET_CHR` | 50 records, every field |
| `cmdr` page 1, 220×24 | `SET_CHR` | 197 records, 20 fields |
| `prov` page 1, 100×15 | `SET_MAP` | 91 records, 14 fields (`VAR0144` never) |
| `dungeon` page 1, 30×5 | `SET_MAP` | 26 records |
| `item` page 1, 50×3 | `SET_ITEM` | 50 records |
| `ui` page 10 | `SET_MAP` via `M.ADV` | 90 markers (`Ui_MarkCount = 90`, 270 non-zero slots) |
| strings | all three | 635 strings: units 20-175 (name, troop type; third slot not set), commanders 176-835, provinces 836-935, items 936-985; scratch strings 1, 2, 4, 5, 7, 8 hold the last record's text |
| cursors/scratch | all three | final `Unit_Cur 50` (Cecil), `Cmdr_Cur 199` (9th Assault Leader), `Item_Cur 50`, `Prov_Cur 91` (Tearing Forest), `Dgn_Cur 26` (Hyper Building); `MapTargetX/Y`, `Tmp0021-0023`, `CallArg`, `RetVal` from the last `M.ADV` call |
| `system`/`ui`/`story`/`history` plain vars | `SET.ADV` | §2.2 |

Reads: only `Prov_Owner`, `Prov_MapX/Y` (just written), the counts and string bases, and
`SpecGraphics`/`SpecBaseline` (`SET.ADV:342-347`). No story flag, `StoryStage` or `Week` is read
**[C]**.

### 4.2 String table layout [C]

`*_StrBase` are ordinary variables computed at `SET.ADV:298-301`; each unit/commander owns three
consecutive strings, each province and item one. The third unit string is written later by
`Iﾊﾝﾗﾝ.ADV:26`, `FIGT.ADV:1975` and the debug starts. Names are **game state**, not constants:
`ｽﾀMENU`/`Iﾊﾝﾗﾝ` rename Rance's troop type, `Cﾒｲｷｭｳ.ADV` (20 × `MM`) and `GAL.ADV` (7) rewrite
commander names, and a capture after the prologue shows strings 21/24 changed to "Leazas
Knights"/"Leazas Mages".

### 4.3 What the core already reproduces

| `SET.ADV` part | Core | Status |
|---|---|---|
| `DC` pages 1, 2, 3, 4, 10 with `maxindex + 1` slots | `gen_state.py` `PAGES` → `GameState.AllocatePages` | **done** [C] |
| all 12 `DS` tables, field order, offsets, alias cursors (`Hist_Prev*`, `VAR0181/0182`, `Ui_MarkNext*`) | `gen_state.py` `TABLES` → `RecordTable<T>`; `VariableCatalog.g.cs` | **done**; golden replay fails early if a dump's `bind` lines differ [C] |
| out-of-range record indexing reading the next field | `RecordTable` doc | done |
| ~130 initial plain values, counts, string bases, `Ui_MapCg*` | — | **missing** (only present when a dump is loaded) |
| shadow values of bound variables (`sysVar[134] = 500`) | `ScriptMemory.Var(134)` exists but no typed property | **missing**; dumps carry it (`v 134 = 500` in every capture) [C] |
| `DC` save flag (page 3 not saved) | — | missing; matters for save/load only |
| `ZG Glob_CgSeen` (engine counts CG loads) | — | missing; already on the golden ignore list |
| `DF` clears | fresh pages are zero | nothing to do |
| `Ui_ScrollWait` from the benchmark | — | machine-dependent; `ui` group (ignored in comparisons) |

### 4.4 Persistence [C]

`DC page, max, save` — third operand = save flag (`cmdd.c:30-38` → `v_allocatePage(…, !!save)`;
`savedata.c:190-192` skips pages without it). Saved: 1, 2, 4, 10; not saved: 3. Page 4 is
additionally shared across slots by `LP`/`QP` partial loads/saves (`ﾛｰﾄﾞｾｰﾌﾞ.ADV:L_00a7b`,
`L_00ad6`).

## 5. Randomness

None. Zero `Y 4` (and no `Y 3`) in all five files **[C]** (statement profile: `SET_CHR` =
5808 constant sets, 2 expression sets, 494 `MS`, 494 `MM`, 247 `ZH`, 247 `R`, 24 `%`, 23 `\0`;
`SET_ITEM` = 200/50/50/50 + `%0`; `SET_MAP` = 1429 constant, 270 expression, 180 `F3`, 90 `#`,
91 `MS`/`MM`/`ZH`, 91 `%`; `SET_VER` = 703 constant). The boot generator state therefore depends
only on `ｽﾀｰﾄ`/`XSPEC`/the title menu, not on this group.

## 6. Input and display

| Where | What | Note |
|---|---|---|
| `SET_*` `ZH 1` / `R` around every `MS` | message-mode switch and line break | no window open at boot; `ZH` affects only message output (`nact.c:66`), not `MS` [C]. Display-only, drop |
| `SET_MAP` → `M.ADV` op 4 | marker list on page 10 (+ drawing onto the hidden 3200×800 map surface set up at `ｽﾀｰﾄ.ADV:852`) | list = state (`ui` group); drawing = presentation |
| `ﾓｼﾞｷｬｯｼｭ.ADV` | glyph cache | display-only, drop |
| `SET.ADV:332` `ZD 0, 1` | debug message switch | no-op |

No player input anywhere in the group.

## 7. Rules vs presentation, quirks

Rules and presentation separate trivially: all state effects are the assignments, strings and the
marker list. Quirks to reproduce (or to know about when comparing memory):

1. **Shadow variables.** `SET.ADV:36-124` writes the record fields while they are still plain
   variables; after `DS` the script can no longer see those slots, but the engine keeps them and
   the dump records them. Only `Prov_BattleArea := 500` (`:104`) is non-zero, so `VAR0134 = 500`
   sits in the plain slot forever while `Provs[0].BattleArea` (page 1 offset 7344) is 0 **[C]**.
   A boot port must write `Memory.Var(134) = 500` directly.
2. **Leftover scratch.** The last province's `M.ADV` call leaves `MapTargetX/Y`, `Tmp0021-0023`,
   `CallArg`; `SET.ADV:339` then sets `RetVal := 10` and `ﾛｰﾄﾞｾｰﾌﾞ` overwrites `Tmp0039/0040`.
   `ｽﾀMENU` overwrites most of them before play; a boot golden test will see them.
3. **Inconsistent item pairs** at boot (§2.4).
4. **Dead code**: `SET_VER.ADV` entirely; `SET_CHR.ADV:7271-7339`.
5. **Unused fields** initialised anyway: `VAR0126/0127/0129` (cmdr), `VAR0136` (prov), `VAR0144`
   never; `VAR0055` (write-only tally starting at 100).
6. **Machine dependence**: `Ui_ScrollWait` (`SET.ADV:341-347`) — use a constant in the port.

## 8. Porting proposal

### 8.1 Data vs code

The record files are pure data with a fixed grammar of seven statement kinds: `!field : const!`
(or `10000 * 3`), `!cursor : n!`, `MS n, "text"`, `MM base-expr, n`, `ZH`/`R` (ignore),
`#L, 1` + `F3, field, expr` (inline table), and the fixed 4-line `M.ADV` marker call. That makes
them ideal for **extraction at the player's side** rather than hand-porting 9.8k lines.

Feasibility was checked **[C]**: a 90-line throw-away Python evaluator (scratchpad, not in the
repo) ran `SET_MAP`, `SET_CHR`, `SET_ITEM` from the raw decompile over an empty page 1, using
the `bind` lines of a capture for the layout, and compared with the `mem enter` dumps:

| Capture | Page-1 slots the files write | Differ | Strings ≥ 20 differ | Non-zero page-1 slots not written by SET_* |
|---|---|---|---|---|
| `095048-01` (first prologue battle) | 7092 | 7 (troops of units Rance (1) / Bound (3) / Soul (4), their status 1→2, Civilian General (cmdr 58) troops) | 0 of 635 | 0 |
| `113558-01` (stage 1, Oaks) | 7092 | 69 | 2 | 0 |
| `125124-01` (Log B) | 7092 | 99 | 3 | 0 |

So the three files are the only source of page-1 content, and a mechanical reader reproduces
them exactly; every difference is gameplay. The marker count in the dump is 90, as predicted.

Recommended shape (owner to confirm, see §9): a **restricted loader in `Kichikuou.Content`**
(`Setup/InitialDataLoader`) that reads the three files from the player's decompiled scenario —
the same place `ScenarioText` already reads — and writes `GameState` directly, rejecting any
statement outside the grammar above (so a different translation/version fails loudly, not
silently). Strings come from the `MS` literals; a small `ScenarioText` extension reads an `MS`
literal by `ScriptRef`. The marker call goes through the shared map op (below). Nothing from the
game is committed. Alternative if a build-time artefact is preferred: a generator like
`gen_figt_m.py` that emits C# with numbers and `ScriptRef`s for names — this commits stat tables
(numbers), which is the gray zone of the "no extracted data" rule.

`SET.ADV` itself is code (layout + ~130 values + one condition + two hooks): port it by hand as
`Boot.Initialise(GameState, IPersistentStore)`, with the layout part already generated.

### 8.2 Parts

| Part | Content | Size | Depends on |
|---|---|---|---|
| **D0** (prerequisite, small) | Move `M.ADV`/`WAO_MAP.ADV` map ops out of `Battle` into a shared `MapOps` (the strategic layer calls `M.ADV` from `MAIN.ADV` dozens of times anyway) | refactor, ~300 C# lines moved | — |
| **D1** | `SET.ADV` port: plain values, string bases, shadow `Var(134)`, page save flags in `ScriptMemory`, `ﾛｰﾄﾞｾｰﾌﾞ` mode 10 as `IPersistentStore.LoadGlobals` hook, `ﾓｼﾞｷｬｯｼｭ` dropped; new-game start (`ｽﾀMENU.ADV:279-291`) as `NewGame()` | 348 + ~20 script lines | layout (done) |
| **D2** | Initial-data loader for `SET_CHR`/`SET_ITEM`/`SET_MAP` (+ `MS` literal reader), unit tests on a synthetic mini-scenario, a content test over the player's files | ~9.8k script lines, but one grammar; ~300-400 C# lines | D0, D1 |

One agent can do D0-D2 in one session; D2 is the only one with volume, and it is mechanical.
`SET_VER.ADV` needs no part.

### 8.3 Golden verification

Every battle capture's `mem enter` dump already contains the full result of this group, as
modified by play (§8.1 table); those cannot test the boot alone. A dedicated **boot capture**
is cheap with the existing harness: add `root SET.ADV`, `inner SET_MAP.ADV SET_CHR.ADV
SET_ITEM.ADV` (with `M.ADV`/`WAO_MAP.ADV` already inner), leave `ﾛｰﾄﾞｾｰﾌﾞ.ADV` and
`ﾓｼﾞｷｬｯｼｭ.ADV` as hooks. The harness treats `root` generically (`golden.c:114-115, 311`), so one
capture per launch results: `mem enter` = memory after `ｽﾀｰﾄ`/`XSPEC`, `mem exit` = boot state;
the `ﾛｰﾄﾞｾｰﾌﾞ` hook dumps resynchronise page 4 (save-file content) without porting it. Core side:
a `SET.ADV` entry point in `GoldenReplay`. Expected ignorable differences: `Ui_ScrollWait`
(already `ui`), `Glob_CgSeen` (ignored). A second check: a capture of the first `Iﾌﾟﾛﾛｰｸﾞ` call
after "From 1" would verify `NewGame()` (hook-enter dump = boot + three values).

## 9. Open questions

Answered by the owner and F1 (2026-10-08): 1 — nothing is committed, the record files are read
at run time (decision 1 in [README.md](README.md)); 2 — the port reads the raw decompile
(`KICHIKUOU_SCENARIO_DIR`) for now; reading the `.ALD` directly may replace it later; 3 — the
debug starts are not ported (`GameSetup.NewGameAsync` rejects `DebugMode ≠ 0`).

1. **Commit numbers or not?** Runtime loader over the player's decompile (nothing committed) vs.
   generated C# with stat numbers (like the battle constants, but a whole table). Owner decision.
2. **Does the port read the decompiled `.ADV` at run time?** Today `Kichikuou.Content` does
   (`KICHIKUOU_SCENARIO_DIR`). If the shipped game should start from the player's `.ALD` only,
   the loader needs `xsys35dc` (or our own SCO reader) at install time — a cross-cutting decision.
3. Is the debug "From 2-4" path worth porting (handy as test fixtures; `DebugMode` is never set
   in normal play — `ｽﾀMENU.ADV:246`)? Proposed: no, or only as test setup.
4. The boot-time smoke dump mentioned in `golden-tests.md` (first `PCMPLAY.ADV` after `SET.ADV`)
   is not in the golden folder; unknown whether it was kept.
5. Meaning of the third unit string slot (written by `Iﾊﾝﾗﾝ.ADV:26`, `FIGT.ADV:1975`) — belongs to
   the survey of those files. Answered 2026-10-23 from every `MM`/`MS`/`X` of string 3 and of the
   slot: it has no meaning. The slot is only ever copied from string 3 (`Iﾊﾝﾗﾝ.ADV:26`,
   `FIGT.ADV:1975`, `ｽﾀMENU.ADV:353, 447`, each right after `MS 1`/`MS 2` set the other two) and back
   into string 3 (`Cﾌﾞｶ.ADV:11, 37`), and string 3 is set (`MS 3`) and shown (`X 3`) only by
   `HAPPY_C` (Fortune Check, `HAPPY_C.ADV:316-400, 357`) — so the slot holds an empty string, or
   Fortune Check's last line when it ran before in that launch, and nothing ever shows it.
6. `VAR0055` starts at 100 and is never read (dictionary: write-only tally); it still has to be
   kept for memory equality — confirm the "never read" when the strategic files are surveyed.
