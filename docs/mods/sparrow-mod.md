# Sparrow Mod — survey (2026-10-12)

The question (owner, 2026-10-12): how hard is it to bring the changes of the fan mod **Kichikuou Rance —
Sparrow Mod v2.9.3** (by Jack Sparrow) into our port? This survey measures the mod against the original
we ported and against the port's own couplings. Nothing of the mod is in the repository; the owner's copy
lives on their machine (no `local.env` key yet — add `KICHIKUOU_SPARROW_DIR` if work starts).
Confidence marks as in [../spike-findings.md](../spike-findings.md): **[C]** measured, **[I]** inferred.

## Verdict

The mod is not a patch but a **rewrite of the game's rules and data model** on the same engine: new battle
mechanics, new records and fields, a renumbered variable layout, renamed files. Its changes **cannot be
merged into our port piecemeal**: the mod's scenes and its rule files share the mod's memory layout, our
hand ports share the original's, and the two do not mix. What can be done is one of three whole options
(details in "Options"):

| Option | What the player gets | Effort | Risk |
|---|---|---|---|
| **A. Run the mod as it is** (the whole scenario through our script runtime) | Sparrow Mod in our app, with the mod's own 640×400 screens | medium | low–medium; one spike decides |
| **B. A second hand port** (a "Sparrow" rule set beside the original's) | Sparrow Mod with our interface | large, close to the original port's rule work, and again for every mod version | high: version churn, permission |
| **C. Cherry-pick** ideas into our own rules and interface | our port, plus selected QoL / optional mechanics | small per feature | low; owner's design calls |

Recommendation at the end.

## 1. What the mod is

- **Files** **[C]**: it replaces `KICHIKUSA.ALD` (scenario), `System39.ain`, `KICHIKUGA/GB.ALD` (CGs),
  `_inmm.ini` and `bgm\`; `KICHIKUWA.ALD` (sound effects) is the original's. It also ships
  `KICHIKUSA.ALD.symbols`: xsys35c's debug information for xsystem35-sdl2's debugger (format: xsystem35's
  `src/debug_symbol.c`), which **embeds the mod's whole source with the author's comments** (203 files,
  7.4 MB), its 957 variable names and 2,548 labels/functions. The mod is compiled by xsys35c from that
  source; `HEL0` in its ain is empty — **no DLLs**, plain System 3.9 **[C]**.
- **History** **[C]**: 28 releases on the author's change log (v1.7 … v2.9.3; v2.9.3's scenario is dated 2025-05-31).
  The owner also has v2.8 (2024-06-17): between v2.8 and v2.9.3 the variable layout was **reshuffled again**
  (918 → 957 variables; the unit fields reordered; `GMeglass` renamed `GMegaras`, `JACK_SUB` split into
  `SUBCALL` and `SET_NIN`).
- **Content** (the author's summary and the bundled guide `SparrowModGuide 2.9.xlsx`): a 10-level difficulty
  and five alternate difficulty switches (permadeath off, ironman, enemy growth, an extra enemy wave every
  turn — the new nation file `TApophis` —, a smarter battle AI); a Speed stat deciding turn order;
  Subclasses with action skills; special skills on a Focus cost, Defend, Ally Guard, Auto-Atk/Auto-Def,
  Atk-All/Def-All; ninja ambush skills; Troop Magic split from Commander Magic; situational stats to +5;
  Strategy in 8 tiers; battle area growing with attacks; Develop City, Royal Edicts, three new buildings;
  new commanders (Pegasus, Athena 2.0, Sanakia, DX Enforcer, Wichita, Kabachahn, Katsuie, Ruberan,
  Convert, Hornet, Silky), new ninja, new enemies and factions (Takeda, Uesugi, Houjou), new cities (Kai,
  Shikoku) and a dungeon (Tenma Bridge); a Bandit King survival mode (`BK`); New Game bonus points; an
  endings list; options to turn off the mod's content, its mechanics, or NSFW art; a horizontal and a
  vertical info bar on the map; a 60-slot Subordinates menu; mid-turn saves; "numerous bug, spelling,
  grammar and translation fixes".

## 2. Method

All in a scratch folder, nothing committed (the scripts can go to `tools/analysis/` if work starts):

1. The `.symbols` file parsed (sections `SRCS`, `SCNT`, `LINE`, `FUNC`, `VARI`) into the mod's sources.
2. The mod's `KICHIKUSA.ALD` + `System39.ain` decompiled with xsys35dc 1.13 — clean, no warnings — into
   the same format as our `_extract\scenario`.
3. Files paired with the original's by order in `xsys35dc.hed` (the order is kept; 3 files removed, 5
   appended); labels, file names and the mod's variable names normalised; line diffs per file.
4. **Variable mapping by vote**: lines whose skeleton (variables blanked) matches in both versions vote for
   "mod variable ↔ original variable"; a pair is taken at ≥ 60 % of the votes. Spot checks agree with our
   dictionary (`A_CurTroops` ↔ `Unit_Troops`, `ShizukaStatus` ↔ `N_Shizuka_Stage`, `LadyLocation` ↔
   `LadyCaravan_Prov`). The diffs were then redone with the mapping applied.
5. Our own `ScenarioParser` (`Kichikuou.Content`) run over every file of the mod's decompile.
6. The CG archives listed and compared with xsys35c's `ald`; the new CGs' PMS headers read.
7. A read-only sweep of the port for its couplings to the original (section 4).

## 3. Findings

### 3.1 Files **[C]**

- Every file is **renamed to ASCII**: `ｽﾀｰﾄ` → `START`, `Cﾃｷﾄｼ` → `CCity`, `Cﾒｲｷｭｳ` → `CDungeon`,
  `Tﾍﾙﾏﾝ` → `THelman`, `死亡捕獲` → `POSTBATTLE`, `FIGT表示` → `FIGT_DISPLAY`, `HAPPY_C` → `DESTINY`,
  `Gｼｨﾙ` → `GSill` and so on.
- **Removed**: `SET_VER`, `Cｹﾝｷｭｳ`, `Iﾄｼ` (all three dead in the original; nothing in our code uses them).
  **Added**: `BK` (Bandit King mode, 376 lines), `TApophis` (the extra waves, 886), `SUBCALL` (shared
  functions, 267), `SET_SLOT` (menu slots, 238), `SET_NIN` (ninja data, 48). Page numbers shift by 1–4.

### 3.2 Script language **[C]**

The mod uses System 3.9 forms the original never does; the rest of its command set is the original's.

| Form | Uses | Our parser |
|---|---|---|
| Functions `**Name params:` / calls `~Name args:` | 22 functions, 4,331 calls (≈ 4,100 of them `~BoxClear` into `WAO_SUB`) | not supported |
| Compound assignment `!X +: e!` (`-:`, `*:` …) | 828 | not supported |
| `} else {` | 47 | not supported |
| Loops `<@cond:` … `>` | 13 | not supported |
| `Y 2`, `ZT 21`, `DF` in a rule file, a `$L$text$` menu item | a few | not supported |

Our parser reads **127 of the mod's 203 files** as they are (variables renamed to `VARnnnn`, which the
parser expects); with the first four forms stubbed out, **194**. The other nine stop at the last row's
commands (`CDungeon`, `FIGT_SEL`, `GAlice`, `MAIN`), at the boot's `DC`/`ZD` and `PCMPLAY`'s `SP`, which
the original has too (we hand-port those files), or at an artefact of the stubbing (`SET_SLOT`,
`SUBCALL`). Only the first error of a file is seen, so a few more commands may hide behind them. So the
scene runtime needs four language features and a handful of commands, not a new interpreter.

### 3.3 Memory and records **[C]**

- **957 variables** instead of 806, and **only 53 keep their index**: the event entry `VAR0520` is
  `VAR0507`, the province cursor `VAR0158` is `VAR0148`, and so on. 654 of the mod's variables map to one
  of 641 originals; 72 never occur in a line shared with the original (new); the rest are new or ambiguous.
- The records are re-laid (the mod's `SET.ADV` builds them with `DC`/`DS` from `*_MaxID` constants):

| Record | Original (count × fields) | Mod |
|---|---|---|
| Player units | 52 × 32 | 301 × 41 (Speed, Subclass, Fatigue, Sex, three Tech upgrades, Troop Magic, troop cost …) |
| Enemy commanders | 220 × 24 | 300 × 24 (different fields: Omni Atk/Def, Troop Magic, Sex, Speed) |
| Provinces | 100 × 15 | 93 × 14 (connections, coordinates, Scouted) |
| Items | 50 × 3 | 60 × 4 |
| Dungeons | 30 × 5 | 40 × 14 (floors, enemies, battle area, music, backgrounds) |
| New | — | ninja 20 × 8, menu slots, endings, harem 300 × 6, temp tables |

  The string table moved with them (`MM` bases are now `20 + A_MaxID * 3 …`).
- **Record IDs mostly survive**: 37 of 50 units and 185 of 197 commanders keep ID and name. Moved units:
  Julia (unit 33 → 65), Aviator (unit 36 → 125), Nagi (unit 40 → 77), Galtia (unit 41 → 43), Aristoles
  (unit 43 → 78), Rolex (unit 47 → 81); renamed: Hanty → Hunty, Meglass → Megaras, Mars the Great → Mars,
  Louis → Luis, the Seizel Sisters → Seizel&Hawzel. One-Shot Tarou (commander 79) and Washi Tada (commander 81)
  swap places.

### 3.4 How much code changed **[C]**

Lines identical after the normalisation and the variable mapping, per group (orig → mod lines):

| Group | Files | Lines | Same | Code lines −/+ | Message lines −/+ |
|---|---|---|---|---|---|
| `G*` character scenes | 122 | 373,059 → 321,286 | 83 % | −63,073 / +9,629 | −1,094 / +2,765 |
| `I*` incidents | 13 | 49,897 → 45,710 | 84 % | −7,655 / +2,777 | −151 / +842 |
| `FIGT*` battle | 11 | 22,730 → 24,136 | 86 % | −2,973 / +3,799 | −221 / +801 |
| `T*` nations | 13 | 8,481 → 7,911 | 88 % | −985 / +410 | −22 / +27 |
| `C*` commands | 12 | 20,194 → 22,752 | 51 % | −8,566 / +10,251 | −1,271 / +2,144 |
| `S*` pickers | 4 | 1,989 → 2,182 | 73 % | −511 / +656 | −27 / +75 |
| `SET*` boot and data | 4 | 10,375 → 10,573 | 11 % | −9,264 / +9,462 | 0 |
| frame and helpers | 19 | 15,731 → 15,025 | 84 % | −2,367 / +1,686 | −83 / +58 |

- The scenes' removed code is mostly the `ZH 1:` the original puts before every message (66,189 of them;
  the mod keeps one). Their real logic change is the +9.6k lines.
- **Text**: 125,670 → 129,629 message lines; ≈ 2.9k removed, ≈ 6.7k added (≈ 5 % of the text is the mod's).
- **The files we hand-port** (the 47 files of section 4, and the empty `Tﾙﾄﾞﾗ2`): 54,817 → 58,979
  lines, **71 % the same**, −14.1k / +16.7k code lines, plus the five new files (1.8k). Per file:

| Original → mod | Same | Code −/+ |
|---|---|---|
| `MAIN`, `COM`, `COMMAND` | 76 %, 91 %, 73 % | −265/+483, −83/+156, −178/+1,356 |
| `Cﾃｷﾄｼ` → `CCity`, `Cﾃｷﾄｼ2` → `CCity2` | 76 %, 55 % | −1,055/+757, −82/+39 |
| `Cﾒｲｷｭｳ` → `CDungeon` | **3 %** (rewritten) | −4,636/+4,192 |
| `Cﾌﾞｶ` → `CSubordinate` | 52 % | −649/+1,260 |
| `Cｼｮｳﾆﾝ` → `CShop`, `Cｼﾊｲﾄｼ` → `CControlledCity` | 72 %, 69 % | −450/+829, −505/+763 |
| `Cﾆﾝｼﾞｬ` → `CNinja`, `Cﾃｲﾄ` → `CConstruction` | 57 %, 68 % | −464/+574, −294/+255 |
| `Cﾊｰﾚﾑ` → `CHarem`, `Cﾀｶﾗ` → `CItem`, `各種DATA` → `ALLDATA` | 76 %, 100 %, 93 % | −170/+70, 0, −26/+11 |
| `Sﾌﾞﾀｲ`, `Sﾌﾞｶ`, `Sﾊｰﾚﾑ`, `Sｱｲﾃﾑ` | 62 %, 74 %, 91 %, 74 % | −228/+224, −63/+215, −34/+83, −186/+134 |
| `TCﾘｰｻﾞｽ`, `TCﾘｰｻﾞｽ2` → `TRebels`, `TRebels2` | 68 %, 81 % | −87/+16, −30/+35 |
| `Tﾍﾙﾏﾝ`, `Tﾍﾙﾏﾝ2`, `Tﾊﾟｯﾄﾝ` | 91 %, 99 %, 69 % | −68/+1, −2/0, −71/0 |
| `Tｾﾞｽ`, `Tｾﾞｽ2` | 100 %, 97 % | −2/0, −13/+18 |
| `Tｹｲﾌﾞﾘｽ`, `Tｹｲﾌﾞﾘｽ2` | 92 %, 80 % | −147/+164, −70/+1 |
| `TJAPAN`, `TJAPAN2` | 86 %, 98 % | −384/+39, −5/+2 |
| `Tﾙﾄﾞﾗ` → `TRudrasaum` (and `Tﾙﾄﾞﾗ2`, empty in the original, now 44 lines) | 47 % | −106/+91 |
| `FIGT`, `FIGT_A`, `FIGT_B` | 79 %, 67 %, 70 % | −658/+1,629, −90/+24, −274/+164 |
| `FIGT_M`, `FIGT_SEL`, `FIGT作`, `FIGTﾃｷDE`, `FIGTﾐｶDE` | 89 %, 83 %, 82 %, 88 %, 84 % | −1,050/+1,136, −107/+190, −77/+195, −226/+218, −111/+117 |
| `FIGTATTK`, `死亡捕獲` → `POSTBATTLE` | 100 %, 99 % | −1/+1, −3/+17 |
| `ﾛｰﾄﾞｾｰﾌﾞ` → `LOADSAVE` | 80 % | −126/+27 |
| `WAO_SUB`, `M`, `WAO_MAP`, `PCMPLAY` | 83 %, 78 %, 96 %, 67 % | −87/+76, −70/0, −7/+307, −15/+1 |
| `ｽﾀｰﾄ` → `START`, `SET` | 31 %, 31 % | −601/+705, −240/+104 |

  "Same" counts a line whose variables map to the original's; the meaning of a field can still differ
  where the record changed. The battle's 86 % hides deep changes (turn order by Speed, Focus skills,
  Defend, Ally Guard, auto orders).

### 3.5 Assets **[C]**

- **CGs**: 1,343 instead of 1,222 — 121 new (`CG0832`–`0835`, `CG1531`–`1647`: portraits, map panels, UI
  icons) and 77 redrawn. All are 8-bit PMS version 1, like the original's: our decoder and the indexed
  palette pipeline read them as they are.
- **Music**: 45 tracks (`kichiku_2`–`46`) instead of 28, listed in the mod's `_inmm.ini`, which the port
  already reads at run time.

## 4. How our port is tied to the original

From a read-only sweep of the code (2026-10-12):

- **File names**: files are identified by their decompiled Japanese names, never by page; ≈ 188 distinct
  name literals in `Kichikuou.Core` (now `Kichikuou.Original`; the router `Strategy/StrategyRules.cs`, `SceneRunner.IsSceneFile`'s
  `G`/`I` prefix rule plus 8 names, the helpers in `Script/ScriptContext.Statements.cs`, each port's own
  file and every scene it calls). The two readers (`ScenarioScripts`, `ScenarioText`) are the one place a
  name map could go, but the mod's own calls (`%#CCity.ADV:`) would also need it.
- **Texts by line**: a message is a `ScriptRef(file, line)` into the original decompile — ≈ 3,200 line
  numbers in the rule code (`SpeakAsync`, `SayAsync` in the generated `Battle.Flavour.g.cs`,
  `MessageAsync`, `TextAsync`, `MenuItem` …), plus save slots' resume points and the message skip's seen
  set. A shifted line mostly shows the wrong text **silently**.
- **806 variables** is a constant (`Sys3/ScriptMemory.cs`), used by the save format (which rejects another
  count), the golden dump parser and `tools/codegen/gen_state.py`; `Ev_Entry` = `VAR0520` is hard-coded.
- **Records** are hard-coded twice: `Setup/GameSetup.Set.cs` (counts, `DC` sizes, string bases) and the
  generator's `TABLES` (fields, strides) → `GameState.g.cs`. The rules test ≈ 1,370 record IDs.
- **`SET_MAP`/`SET_CHR`/`SET_ITEM`** are read at run time but only as straight-line code; the mod's
  versions call local subroutines (`\ASetBaseline:`) and would be refused.
- **Hand-ported** (47 files): `MAIN`, `COM`, `COMMAND`, the 10 command files, `各種DATA`, the 4 pickers,
  the 12 nation files, `ﾛｰﾄﾞｾｰﾌﾞ`, the battle (`FIGT`, `FIGT_A`, `FIGT_B`, `死亡捕獲`, `FIGTATTK`,
  `FIGT_M` generated, `FIGT_SEL`, `FIGT作`, `FIGTﾃｷDE`, `FIGTﾐｶDE`), the helpers `WAO_SUB`, `M`,
  `WAO_MAP`, `PCMPLAY`, and the boot (`ｽﾀｰﾄ`, `SET`): ≈ 24k lines of C# in `Combat` (7.7k + 3.2k
  generated) and `Strategy` (16.3k), 225 quirk IDs. **Run from the player's files**: the 136 `G*`/`I*`
  scenes, `ｵｰﾌﾟﾆﾝｸﾞ`, `FIGT表示`, `OVER`, `ｽﾀMENU`, `ﾌﾟﾛﾛｰｸﾞ`, `HAPPY_C`, `CG`, `MMODE`, the three
  `SET_*` data files.
- **Reusable**: the differential tests (`NationTurnDifferentialTests`, `CommandSliceDifferentialTests`)
  run a port and its `.ADV` side by side on random states and compare every message (`File:Line`), draw
  and memory cell — the same harness checks a port of a mod file against the mod's decompile. CGs come
  from an extracted folder and music from `_inmm.ini`, so a second asset folder costs nothing.

**Why nothing can be merged piecemeal.** A mod scene (say Fuuka's, `GFuuka`) reads and writes the mod's
variable indices and records; our rule code reads the original's through `GameState`. Running the mod's
scenes on our rules, or our scenes on the mod's rules, corrupts the state. The unit of integration is the
whole game.

## 5. Options

### A. Run the mod as it is (interpreter variant)

The whole mod scenario runs through our script runtime (`ScriptProgram` + `SceneRunner`, which already
runs every original scene and, in the differential tests, every rule file): no hand port and no router;
the mod's own screens are drawn on the emulated engine surface (`Content/Screen`).

Work:

1. The language: functions with parameters (`~`/`**`, System 3.9 semantics as xsystem35 implements them),
   compound assignments, `else`, `<@ >` loops; the commands `Y 2`, `ZT 21`, `SP`, `ZD`; `DC`/`DS`/`DF`/`ZG`
   at run time (today the boot is hand-ported).
2. Memory from the scenario: the variable count from the decompile, array pages from the running `SET`;
   saves tagged with their variant.
3. A **variant profile**: the scenario and CG folders, the entry file (`START.ADV`), no hand ports. The
   name-keyed hooks stay off.
4. The engine surface must draw everything the mod's rule files draw — the battle screen, the map and its
   two info bars, the menus — which today our presenter draws by hand from semantic events. The gap is not
   measured yet: that is the spike.
5. Verification: golden captures of the mod on the patched xsystem35 (the mod's author debugged on
   xsystem35-sdl2 — the `.symbols` are its format **[I]**; the dump parser must lose its 806), plus an
   unattended run.

**The spike that decides A**: run *the original game* through the same path first (no mod) and compare
with the 37 battle captures and the prologue captures. It needs no new language feature, reuses all our
verification, and shows what the surface lacks. Afterwards the mod adds only items 1–2.

Result: the mod, faithful to itself including its bugs, playable in our app, and **later mod versions
cost little** (a new decompile, a check run). Not our interface: the player sees the mod's 640×400 screens.

### B. A second hand port (a Sparrow rule set)

The way we ported the original, once more for the mod: a dictionary of 957 variables (the `.symbols`
already name them all, many with comments; 654 map to our dictionary), a generated `GameState` for the
mod's layout, the boot, and a re-port of the 47 hand-ported files — ≈ 30 % of their code changed
(`Cﾒｲｷｭｳ` and the subordinates/merchant/city commands most), plus the five new files; ≈ 3,200 text line
numbers re-pointed (every file's lines shift); the 225 quirks re-checked against the mod (it fixes some
and adds its own); new semantic events and hand-drawn screens for the mod's UI (info bars, the 60-slot
Subordinates menu, subclass actions, difficulty and NG+ menus, the endings list). Our differential tests
would check each file against the mod's decompile.

Cost: close to the original port's rule work (the whole port took 2026-10-07 to -11, 169 commits; most of
it was rule files), and **again for every mod release** — v2.8 → v2.9.3 already re-laid the variables.
Two rule sets to maintain. Needs the author's permission: a hand port translates their code into our
GPL repository.

### C. Cherry-pick into our own port

- **Interface-only ideas fit our rules today** ("modernise only the interface"): Atk-All/Def-All and
  auto orders as UI shortcuts, weeks-left and visits-left counters, warnings on Melfeis, Shariela, Cream,
  Somita, the map info bars, an endings list, legibility of panels. Candidates for our own interface
  (docs/ui/survey.md).
- **Mechanics** (Speed, Subclasses, Develop City, Royal Edicts, difficulty switches, the smarter AI, battle
  area growth) change the rules: only as optional rule modules after an owner's decision — the brief puts
  rule changes after the whole port ("Hybrid later"), which is now done. The mod's commented source is a
  ready specification.
- **Content** (new commanders, events, texts, CGs) cannot be taken alone: its scenes need the mod's state
  and mechanics, so it is option B for those parts.

## 6. Recommendation

**Decided** (2026-10-12, decision 24): the owner chose two projects on one code base — a shared engine,
the original and the mod as separate rule sets (option B on a shared engine; running the mod's files not
ported yet through the runtime, as in option A, is the agent's proposal for the way there, still to be
tried). The refactor comes first ([../variants.md](../variants.md)); the
mod's port waits for its author's permission. The survey's recommendation, as it stood:

Decide the goal first (owner):

- *"Players can play Sparrow Mod in our app"* → **A**. Start with the spike on the original game; it also
  serves the release work (reading the player's files, backlog "Release").
- *"Sparrow's content with our interface"* → **B**, pinned to one mod version, only with the author's
  consent, and only after A or a long look at the cost.
- *"A better port of the original"* → **C**: interface ideas into the UI design now; mechanics as options
  later.

A cheap side benefit regardless of the choice: the mod's names and comments for 654 variables can
cross-check our dictionary (`docs/variables.tsv`) — an agent task of an hour.

## 7. Open points

- The author's permission (B, and to ship anything derived from the mod; A reads only the player's files).
- Does the patched xsystem35 (`golden-harness`) run the mod and capture it? **[I]** yes; not tried.
- Which mod version to target, and how to check it (archive hashes, as backlog "Check the game's version").
- The mod's own bugs and its fixes of ours (quirks) — not surveyed.
