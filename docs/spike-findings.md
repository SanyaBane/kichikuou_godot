# Spike findings — surface analysis of the scenario (2026-10-07)

Source: the English fan translation (v1.0, Arunaru et al.). The Japanese original is out of scope.
Confidence: **[C]** confirmed by reading code / disassembly, **[I]** strong inference, **[G]** guess.

## Local layout (outside the repo, never committed)

| What | Path |
|---|---|
| Game (EN) | `E:\Modding games\Modding Rance\Kichikou\Kichikuou Rance` |
| xsystem35 2.20.0 (Windows build) | `E:\Modding games\Modding Rance\Kichikou\xsystem35-2.20.0-windows-64bit` |
| xsys35c 1.13.0 (`xsys35dc`, `ald`, `pms`, …) | `E:\Modding games\Modding Rance\Kichikou\xsys35c-1.13.0-64bit` |
| alice-tools 0.13.0 | `E:\Modding games\Modding Rance\alice-tools\alice-tools-0.13.0` |
| Godot 4.7.2 .NET (portable) | `E:\Install PORTABLE\Godot_v4.7.2-stable_mono_win64` |
| xsystem35-sdl2 source (golden-test harness, branch `golden-harness`) | `F:\sources\Rance\xsystem35-sdl2` |
| MSYS2 (toolchain for xsystem35) | `C:\msys64` |
| Extracted data | `E:\Modding games\Modding Rance\Kichikou\_extract\` — `scenario\` (decompiled `.ADV`), `aindump\`, `cg_pms\`, `cg_png\`, `wav\` |

Building xsystem35 (verified 2026-10-08, commit `82c8b76`): the README's `pacman` list lacks
`make`, so use Ninja, which ships with the MSYS2 cmake package. From any shell:

```
MSYSTEM=UCRT64 /c/msys64/usr/bin/bash.exe -lc 'cd /f/sources/Rance/xsystem35-sdl2 && mkdir -p out/debug && cd out/debug && cmake -G Ninja -DCMAKE_BUILD_TYPE=Debug ../.. && ninja'
```

Output: `out/debug/src/xsystem35.exe` (run it with `C:\msys64\ucrt64\bin` on `PATH` for the DLLs).
`bash -lc` starts in the MSYS2 home directory, so always `cd` explicitly.

Tool quirk: `xsys35dc -o` and `ald extract -d` fail on absolute paths with a drive letter
(`cannot create directory E:`). Run them from `_extract\` with relative paths.

## Game files

- Engine is **System 3.9** (`system39.exe`, `System39.ain`, `sys_ver = 3.9`) **[C]**.
- `KICHIKUSA.ALD` — scenario, 201 `.SCO` files.
- `KICHIKUGA.ALD` + `KICHIKUGB.ALD` — two volumes of one CG archive: 1222 PMS images, 8 bpp
  (sizes 640×400, 640×294, 320×256, 192×256, …). All convert to PNG with `pms.exe` without errors.
- `KICHIKUWA.ALD` — 51 WAV sound effects.
- `bgm\kichiku_2..29.mp3` — 28 music tracks played through `_inmm` (CD-audio replacement), so no MIDI.
- `System39.ain` holds only message text (`MSGI`, 126k entries) and variable slots (`VARI`, 806,
  all unnamed `VAR0000..`). `HEL0`/`FUNC` are empty: **no DLLs, no library calls** **[C]**.

## Scenario volume

Decompile is clean: every line parsed, no unknown bytes or warnings **[C]**.

| Metric | Value |
|---|---|
| Files / size / lines | 201 / 7.6 MB / 503k |
| Message lines | 125,670 (≈480k English words) |
| Distinct commands | 66 base mnemonics (77 with variants); no loops, no function calls |
| Story entry points | ≈800; **≈650–700 substantial scenes** (median ≈83 message lines) |

| Group | Files | Role |
|---|---|---|
| `G*` | 122 | Per-character event scenes — almost pure dialogue (≈80% of the text) |
| `I*` | 14 | Story arcs / incidents — dialogue |
| `FIGT*` | 10 | Battle engine (`FIGT`, `FIGTATTK`, `FIGT_SEL`, `FIGT_A/B`) and battle text (`FIGT_M`, `FIGT表示`) |
| `C*` | 11 | Player commands (dungeon, attack city, own city, merchant, harem, …) |
| `T*` | 14 | Enemy-nation turns and AI |
| `SET_*` | 5 | Data initialisation (units, commanders, map, items, flag reset) |
| `S*` | 4 | Status screens |
| `MAIN`, `COMMAND`, `COM`, `M`, `WAO_*`, … | ~21 | Main loop, command menu, event dispatcher, helpers, title/boot |

Dialogue pattern: every message is `\L_open: ZH 1: 'text' R 'text' \L_wait: \L_close:`, where the
stubs call `WAO_SUB.ADV` with a style code in `VAR0521`. Event files start with a dispatch table
on `VAR0520`; `COM.ADV` is the event dispatcher.

## Data model

`SET.ADV:135-292` allocates array pages (`DC`) and binds plain variables onto them (`DS`), so
`VAR0074` means "field 0 of record `VAR0154`". Records are struct-of-arrays with a cursor variable **[C]**.

| Cursor | Records | Fields | Meaning |
|---|---|---|---|
| `VAR0154` | 52 | `VAR0074-0105` | Player-side units / officers |
| `VAR0156` | 220 | `VAR0106-0129` | Enemy commanders |
| `VAR0158` | 100 (91 used) | `VAR0130-0144` | Provinces |
| `VAR0160` | 50 | `VAR0150-0152` | Items |
| `VAR0162` | 30 (26 used) | `VAR0145-0149` | Dungeons **[I]** |

Key fields **[C]**: troops/max `0074/0075` (enemy `0106/0107`), troop atk/def `0076/0077`,
commander atk/def `0087/0088`, attacks `0089`, HP `0090/0091`, strategy `0092`, magic `0093`,
level `0105`, EXP `0104`, weekly recovery `0099`. Province: owner `0130`, income `0132`,
wall `0133`, city battle limit `0134`. Gold `VAR0519` **[I]**. Names live in the string table
(`MS`/`MM`). Story flags are most of `VAR0043-0805` (reset by `SET_VER.ADV`).

## Turn structure

`MAIN.ADV:1` is the loop; one pass = one **week**. Absolute week `VAR0517`; displayed date
LP year / month / week derived from it (4 weeks × 12 months) **[C]**. Story stage `VAR0518`.
Order: player phase (income, `COMMAND.ADV`) → fixed sequence of nation phases (AL Church, Leazas
rebels, JAPAN, Helman, Patton, Zeth, Kayblis, Creator, …), each gated by a war/story flag **[C]**.

## Battle

Full specification: [battle/](battle/README.md).

`FIGT_A` (player attacks) / `FIGT_B` (enemy attacks) → core `FIGT.ADV` **[C]**. 4 vs 4 slots,
3 + rnd(2) = 4–5 rounds (`rnd(n)` is 1..n), lowest-troop unit acts first, attacker loses on timeout. Hit chance
`(atk-def)*8` if atk > def, else `8+atk-def`, adjusted by level / walls, clamped to 1..100.
≤80 attacks: one roll each; >80: expected hits ± random spread. Separate exchanges for troops vs
commander, troops vs troops, commander vs troops, commander vs commander; counterattack mirrors it.
Formula lines: `FIGT.ADV:2138-2186`, `2200-2655`; EXP/level-up `2977-3160`.

## AI

Strategic AI is hand-scripted per army: fixed priority lists of commanders, a troop threshold to
move, then a fixed ordered list of target provinces — first player-owned one gets attacked;
one attack per army per week **[C]**. Story triggers mixed in. In-battle AI: random target,
1/3 chance to defend when troops < max/5 (`FIGT.ADV:1411-1488`) **[C]**.

## Randomness

- `Y 4, n:` → `VAR0000` = 1..n (n ≤ 1 returns n without drawing); 370 uses, 92 of them in
  battle flavour text **[C]**.
- `Y 4` is the **only** consumer of the generator in this game: `Y 3` is a wait, and the other
  `genrand()` callers in xsystem35 (`ZR`, DLL modules such as screen quake) are not used by the
  scenario **[C, xsystem35 source]**.
- xsystem35: **MT19937 (1998 `mt19937-1.c`)**, `genrand()` on the closed [0,1] (so `n+1` is
  possible in rare cases), result `(int)(genrand()*n)+1`; seed = `getpid()`, not configurable **[C]**.
- The original `system39.exe` likely uses a different generator **[G]** — exact sequences can
  only match xsystem35, not the original.
- Golden tests need either a patched xsystem35 with a fixed seed, or recorded `Y 4` draws
  replayed into the core. Either way **draw order must match**, including draws in flavour text.

## Engine semantics the core must reproduce

- **16-bit unsigned saturating arithmetic**: `a-b` floors at 0, `+`/`*` cap at 65535, `/ 0` and
  `% 0` give 0. The script relies on it (`|a-b|` is written `a-b + (b-a)`) **[C]**.
- Array pages with aliasing (`DC/DS/DF/NB/NC`), inline data tables (`F1/F3`), `US/UG`.
  `DC page, n` allocates `n + 1` slots: `n` is the highest index (xsystem35 `cmdd.c`; confirmed
  by a memory dump of the running game) **[C]**.
- Everything else (`G`, `CC`, `CS`, `PF*`, `IM`, …) is display/input.

## Variable dictionary

[variables.tsv](variables.tsv) names and describes all 806 variables (`var, name, group,
meaning, confidence, refs`): 748 named, 652 at C, 144 at I, 10 at G (unnamed). Naming:
`Unit_*`, `Cmdr_*`, `Prov_*`, `Dgn_*`, `Item_*`, `Hist_*` record fields; `Bt_*` battle;
`Ui_*` UI; `F_<Subject>_<What>` story flags; `N_*` counters; `Ev_*` event dispatch;
`Tmp<N>` scratch. A readable copy of the scenario is produced with

```
python tools/analysis/rename_vars.py <_extract\scenario> <_extract\scenario_named>
```

and `tools/analysis/varusage.py` profiles where each variable is written and tested.

Facts the dictionary pass added **[C unless marked]**:

- **Owner codes** (`Prov_Owner`, from the labels in `Cﾃｷﾄｼ2.ADV:69-107`): 1 Leazas (player),
  2 Rebel Army, 3 Independent City-States, 4 Helman, 5 Patton, 6 JAPAN, 7 Zeth, 8 Kayblis,
  9 Hornet, 10 Angels, 11 ALism. `VAR0527` holds the code of the nation whose phase runs.
- **Troop class**: 0 Assault, 1 Archers, 2 Mages, 3 Magic Warriors, 4 Phantom Beasts,
  10 Demon Assault, 11 Demon Magic. Demon commanders take HP damage only from demons or
  item 1/2 holders (`FIGT.ADV:2416-2446`).
- **Province** also has girls left for Temporary Levy (`0131`), up to five adjacent provinces
  (`0137-0141`) and map position (`0142-0143`).
- **Battle**: slot state values (0 not acted, 1 acted, 2-21 player defending = defence bonus,
  20 AI defending, 100 empty); enemy retreat rule `VAR0529`; per-slot standing orders
  `VAR0693-0696`; partner bonus via `Unit_Partner`; duel flag `VAR0322`.
- **Item ownership**: `0150` 0 not owned, 100 owned unequipped, 1-50 equipped by that unit.
- **Weekly mechanics hidden in scene/dispatcher files**: AL Church raider pools (`GAL.ADV`),
  Lady Caravan moving between provinces, monster garrison regeneration, capsule kaiju growth,
  Helman/Zeth sub-army state machines advanced by counters and 1-in-N rolls. These belong to
  the rules port, not the event converter.
- **RNG gotcha**: some draws are discarded (`FIGT_M.ADV:5`) but still consume the sequence.
- **Persistent data**: array page 4 survives across save slots (endings record, clear counter,
  CG gallery, start-menu unlocks); `OVER.ADV` (epilogue) tests each character's fate flag and
  is a ground-truth decoder for story-flag values.
- Benchmark vars `VAR0670-0672` (XSPEC) and debug mode `VAR0057` are irrelevant to the core.

## Open questions

- The 10 G-level variables and the 144 I-level ones — confirm when the code that uses them is ported.
- Whether the fan translation changed any logic (it was a script hack) — compare only if a
  discrepancy shows up; the Japanese original is out of scope.
- How to port: hand-written C# rules vs. translating/interpreting the script (see the brief).
