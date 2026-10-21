# Extending the game — where the counts and the IDs are fixed (survey, 2026-10-21)

The owner asked (2026-10-21): what if the story is to be customised, with new provinces, heroes,
nations, and new AI on the map and in battle — does the whole game have to be rewritten in C#? This
survey answers with facts: where the numbers of provinces, units, commanders and nations are fixed,
what of it is data and what is code, and how large each way of extending the game would be.

Sources: a read-only sweep of the code at commit `7aae00b` and of the decompiled scenario
(`_extract\scenario_named`). Counts of literals are regex counts, so they are approximate; line
counts are `wc -l`. Nothing is decided here; the questions for the owner are at the end.

## 1. Summary

- **The starting world is data, but thin data.** `SET_MAP`, `SET_CHR` and `SET_ITEM` (read from the
  player's files at boot) fill the records of provinces, units, commanders, items and dungeons,
  including the provinces' adjacency and map coordinates. They decide how many slots are *used*, not
  how many *exist*.
- **The sizes are code.** The record strides (units 52, commanders 220, provinces 100, items 50,
  dungeons 30) are written in the port of `SET.ADV` (`Setup/GameSetup.Set.cs:135-143`) and again in the
  generator's `TABLES` (`tools/codegen/gen_state.py:43-66`), whose output is `GameState.g.cs`.
- **A nation is not a record at all.** It is an owner code on each province (`Prov_Owner`, 1-11) plus one
  hand-written C# class per nation file, a phase in the week loop and a few story flags.
- **The rule code names records by literals** (about 1,870 of them, no constants, no enums), because it
  mirrors the script line for line (docs/porting-conventions.md). Most are in the battle's flavour text
  and defeat scenes and in the nations' files.
- **The scenes (85% of the scenario) are stride-independent.** They reach records through cursors
  (`!Unit_Cur : 10!`, then the bound fields), so they keep working if the strides change, as long as the
  IDs they name keep their meaning.
- **Saves are raw memory.** A change of stride or field layout loads old saves *silently wrong*.

So: story and battle AI can be extended on the original as it is; heroes, provinces and nations need
the sizes and the per-nation code turned into data; only different mechanics need a new rule set.
§6 sizes the three ways.

## 2. The state layer

The record tables are packed one after another on array page 1 (`Dc(1, 10000 * 2)`, 20,001 slots). A
field's address is `base + count * k + cursor` (`gen_state.py:154-158`); changing a stride moves every
table after it.

| Entity | Count (slots) | Used (by the `SET_*` data) | Fields | Free slots today |
|---|---|---|---|---|
| Province (`Prov_Cur`) | 100 | 91 | 15 (owner, economy, defense, battle area, background CG, `Adj1-5`, map X/Y…) | 92-99 |
| Unit (`Unit_Cur`) | 52 | 50, plus 51 as a scratch slot | 32 (troops, attack, HP, item, partner, level, five CGs…) | none |
| Commander (`Cmdr_Cur`) | 220 | 197 (up to 199) | 24 (no nation field) | 200-219 |
| Item | 50 | 50 | 3 | none |
| Dungeon | 30 | 26 | 5 | 27-29 |
| Nation | — | owner codes 1-11 | — | — |

- **Spare memory:** the tables end at slot 8,744 of page 1, so about 11,250 slots are unused; a new table
  could go there through a new `TABLES` entry without moving anything (old saves hold 0 there). The
  engine allows pages 1-255 of up to 65,536 slots (`Sys3/ScriptMemory.cs:19, 40-43`).
- **Variables:** 806 (`VariableCatalog.Variables.Count`, `OriginalVariant.cs:40`). 47 are in the
  dictionary's "unused" group; the clearly free ones are `VAR0503`-`VAR0512` (only ever reset). Adding
  a variable changes the count, and a save with another count reads as an empty slot
  (`ScriptContext.Save.cs:142`).
- **Encodings that cap the counts:** `Item_Holder` is "0 = not owned, 100 = owned and unequipped, 1-50 =
  the unit equipping it", so unit IDs must stay below 100. All values are 16-bit saturating
  (`Sys3Int`), so no figure may pass 32,767.
- **The string table** follows the counts too: `Cmdr_StrBase = 20 + Unit_Count * 3`, the provinces' and
  items' bases likewise (`GameSetup.Set.cs:207-209`).
- **Saves** (`SaveData.Capture`, `SaveData.cs:62-81`) store page 0, pages 1, 2, 4 and 10 and every
  string; `Restore` copies by index. Filling free slots keeps saves compatible; moving a stride or adding
  a field needs a format bump and a migration. The golden comparisons with the original engine would stop
  matching too.

## 3. The scenario

201 `.ADV` files, 503,161 lines:

| Kind | Files | Lines | How the port runs it |
|---|---|---|---|
| `G*` scenes | 122 | 373,059 | from the player's files (`SceneRunner`) |
| `I*` scenes | 14 | 49,898 | from the player's files |
| `C*` commands | 11 | 18,530 | hand-ported |
| `T*` nation turns | 13 | 8,481 | hand-ported |
| `S*` pickers | 4 | 1,989 | hand-ported |
| `FIGT*` battle | 10 | 22,268 | hand-ported (`FIGT_M` generated) |
| `SET*` boot and data | 5 | 11,079 | `SET` hand-ported, `SET_MAP`/`SET_CHR`/`SET_ITEM` from the player's files |
| other (`MAIN`, `COM`, `COMMAND`, `OVER`, `ｽﾀMENU`, `CG`, `MMODE`…) | 22 | 17,857 | `MAIN`/`COM`/`COMMAND` hand-ported, the frame from the player's files |

- **Data in `SET_*`:** about 7,400 of their 9,900 lines are `!field : constant!`; they read no story
  flag and draw no random number. `SET_MAP` holds the provinces' adjacency (216 of 455 `Adj` slots set)
  and an inline table of 100 map coordinates (`:2333-2337`). The map picture is six tile CGs
  (`SET.ADV:333-338`); the roads are drawn into them, there is no road data.
- **Scenes name records by number.** 114 of the 136 `G*`/`I*` files touch record fields (2,135 lines,
  773 of them writes); their cursors name 51 units, 66 commanders, 69 provinces, 21 items and 8 dungeons.
  The fields they use most: `Unit_Status` (who is alive or has joined), `Prov_MapX/Y` (camera pans),
  `Prov_BgCg`, `Cmdr_Status`, `Prov_Owner` (44 writes: scenes change the map). One scene rewrites
  adjacency at run time (`Iｼｬﾝｸﾞﾘﾗ.ADV:912-922`, `:1096-1100`).
- **Story state:** about 297 variables (247 `F_*` flags, 47 `N_*` counters, `StoryStage`, `Week`).
  Each scene file starts with a jump table on `Ev_Entry` (785 entries). `COM.ADV` fires the weekly
  scenes: 38 calls in tier 1 (all that hold), 111 in tier 2 (the first that holds); 154 of its 185
  conditions test flags, 57 `Unit_Status`, 16 `Week`, 18 a random roll.
- **Texts by line:** a message is a `ScriptRef(file, line)` into the decompile — about 3,200 line
  numbers in the rule code ([sparrow-mod.md](sparrow-mod.md), §4). An edit that shifts lines of
  an original file shows the wrong text silently; new text must go into new files.

## 4. The rule code (`src/Kichikuou.Original`)

33,661 lines (about 7,050 generated): `Strategy` 16,564, `Combat` 10,870, `State` 4,101, `Script` 1,030,
`Golden` 669, `Setup` 355. Tests: 38,709 lines for the whole solution.

**Loop bounds.** `Unit_Count`, `Prov_Count` and the like are read in four places only (`WeekLoop.cs:280`,
`CommandPhase.cs:281, 301`, `GesturePoints.cs:108, 114`). Elsewhere the walks over records end at
literals (`Unit_Cur > 48`, `Item_Cur == 51`, the picker's `Cursor < 51`, nine such walks), and the
nations' commander ranges are literals (`WeekLoop.cs:436-524`). Some ID ranges are classes: commanders
in one range are monsters, provinces past one number belong to a region (`Battle.Flavour.g.cs:264`,
`Battle.Flow.Attack.cs:75, 164`, `Battle.Flow.Defence.cs:286, 326`).

**Literal record IDs** (cursor sets and comparisons, owner comparisons, priority and target lists):

| File | IDs |
|---|---|
| `Combat/Battle.Flavour.g.cs` (generated from `FIGT_M`) | 574 |
| `Combat/Battle.Resolution.DefeatScenes.cs` | 240 |
| `Strategy/HelmanTurn.cs` | 136 |
| `Strategy/HelmanDefenders.cs` | 88 |
| `Strategy/DungeonCommand.cs` | 65 |
| `Strategy/WeeklyEvents.Triggers.cs` | 56 |
| `Combat/Battle.Resolution.cs` | 51 |
| `Strategy/EnemyCityCommand.cs` | 50 |
| `Strategy/WeekLoop.cs` | 49 |
| the other `Kayblis*`, `Zeth*`, `Rebel*` files, `Battle.Flow.Attack.cs` | 25-42 each |

Owner codes appear in 111 comparisons (63 of them the player's, owner 1).

**Nations.** Each is code:

| Nation (owner) | Turn | Defenders | Lines (turn + defenders) |
|---|---|---|---|
| Rebels (2) | `RebelTurn` | `RebelDefenders` | 86 + 74 |
| Helman (4) | `HelmanTurn` | `HelmanDefenders` | 389 + 296 |
| Patton Army (5) | `PattonTurn` | — | 46 |
| JAPAN (6) | `JapanTurn` (+ `.Gekkou`) | `JapanDefenders` | 808 + 110 |
| Zeth (7) | `ZethTurn` | `ZethDefenders` | 365 + 208 |
| Kayblis (8) | `KayblisTurn` (+ 3 parts) | `KayblisDefenders` | 1,133 + 185 |
| Creator (10) | `CreatorTurn` | — | 138 |
| Camilla, La Vaswald, Mars, AL Church | inline in `WeekLoop` | — | — |

- `NationFile` (57 lines) shares only the mechanics: a priority list of up to nine commanders, the
  army picker with an overridable filter, the troop sum, and the walk over a target list that attacks the
  first province the player holds.
- Each nation writes its own: its owner code, its priority and target lists (commander and province
  literals), its thresholds (attack only with 500 troops or more…), its filter, and its story branches —
  most of the larger files' lines are story (Kayblis's generals, Gekkou's attempts).
- The week loop calls the phases in a fixed order (`WeekLoop.LeazasWeekAfterCommandAsync`); each phase
  checks its own flag and hard-codes its camera province, music and banner line. `StrategyRules.cs:66-105`
  routes by file name; the defender file is chosen by the province's owner (`EnemyCityCommand.cs:367-385`).

**Map AI** is therefore lists in code, not a strategy: which commanders go, which provinces are tried, in
which order.

**Battle AI** is in one place, `Combat/Battle.Flow.Enemy.cs` (102 lines): the target is a random occupied
player slot (melee commanders re-roll a back-row pick), the action a roll and the commander's class; it
names no commander. Character IDs sit elsewhere in the battle: the players' specials by unit
(`Battle.Flow.Player.cs:230-250`, thirteen units), some commanders' and the ninjas' special cases in
`Battle.Resolution.cs`, the flavour lines and defeat scenes (about 750 comparisons, generated or
transcribed from `FIGT_M`).

**Story triggers** (`COM.ADV`) are C#: `WeeklyEvents.cs` (35 triggers) and `WeeklyEvents.Triggers.cs`
(111), about 133 distinct flags read, literal weeks. A new weekly scene is a new line of C# today.

## 5. The presentation layers

`Original.Content` (about 9,900 lines), `Content` (about 6,900), `godot/` (about 22,800; 46 of its 100
files use the original's namespaces).

- **The map** comes from the player's files: positions, adjacency and the tile CGs from the state
  (`MapMarkers.cs:71-91`, `BattlePresenter.Engagement.cs:457-477`). In code: the click radius of twelve
  provinces (`CommandPhase.HitRadius`, `COMMAND.ADV:101-137`), reused for the plates' size, and two
  prologue special cases (`MapMarkers.cs:85, 111`).
- **Faces:** the CG a unit or commander shows is state (`Unit_FaceCg`, `Cmdr_FaceCg`); the crops are a
  table of about 73 CGs (`UnitFaces.cs:51-126`, editable through `FaceCrops`), the speakers' plates a
  table of 244 CGs (`PortraitNames.cs:15-261`).
- **Battle effects:** `MagicSpells.cs` (29 script lines, 13 units, 14 commanders to elements),
  `ExchangeEffect.cs:145-164` (16 special-and-unit pairs), the effect classes tuned to their sprites.
- **Nation flags:** CG `owner + 819` (`StatusStripView.cs:24-25`, `WeekReportView.cs:21`); no colour
  per nation.
- **Our screens hook the script's screens by line:** the province menu, the command panel and the info
  boxes map script line numbers to actions (`ProvinceMenu.cs:91-153`, `CommandMenu.cs:95-102`,
  `BattlePresenter.InfoBoxes.cs:39-45`); the deployment table stops at `LastUnit = 50`
  (`DeploymentTable.cs:216`).

## 6. Three ways, and their size

### A. Mods on the original variant

The original's rules unchanged; a mod adds files and data.

| Wish | What it takes |
|---|---|
| New scenes, branches, dialogues | New `.ADV` files run by `SceneRunner` (the subset of System 3 it reads: no `else`, no loops), never edits of the original's files (texts by line). A way to load a mod's files over the import, and to hook a mod's weekly triggers into `WeeklyEvents` (today C#), flags in the ten free variables or a new page. |
| Battle AI | `EnemyChooseTarget`/`EnemyDecide` behind an interface, the original's as one implementation. |
| A new commander | A free slot (200-219), its `SET_CHR`-like record, and a line in a nation's priority list — code. |
| A new province | A free slot (92-99), its record, its adjacency (at most five), and the map art: the six tile CGs are the original's, so a mod brings its own. |
| A new hero, a new nation | Not possible without B. |

Size: small to medium — a mod loader, a trigger hook, an AI seam; a few thousand lines with tests. Every
change keeps the original playable as is when no mod is loaded.

### B. An extended original (a variant forked from `Kichikuou.Original`)

The original's rules and scenes, with what is fixed in code turned into data:

- **Sizes from data:** the generator takes the counts per variant (variants.md, "Deferred": "the
  generators per variant"); the boot reads them; the saves get a format version and a migration (and the
  variant's id, also deferred there). Scenes keep working, since they reach records through cursors.
- **A nation table:** owner code, name, flag CG, phase order and condition, camera province, music,
  banner, commander range, priority and target lists, defender file. The seven nation classes keep their
  story branches as code, their lists move to data; the inline phases of `WeekLoop` become entries.
- **Hero tables:** the specials by unit, the magic elements, the face crops and plates, the effects —
  the tables of §4 and §5 keyed by ID move to data.
- **The literal walks** (`Unit_Cur > 48` …) read the counts.
- **Map AI** as a strategy per nation behind an interface, the list walk the original's implementation.
- Limits that stay: 16-bit values, unit IDs below 100 (`Item_Holder`), the scenes' own IDs fixed (a new
  hero joins through new scenes; the old scenes never mention them).

Size: medium — a few thousand lines changed in the rule code (the ~900 literals of the nation files and
the week loop matter; the ~800 of the battle's flavour and defeat scenes can stay, they only pick text),
the generator and the saves, the presenters' tables; differential tests of the fork with the original's
data against `Kichikuou.Original` keep the behaviour honest. This covers every wish of the owner's list.

### C. A rule set of our own (the hybrid's way)

A new model (typed lists of entities, no memory layout, no 16-bit), new rules, nations, AI and events
defined as data. What stays: the engine, the content readers, the drawing and our screens' parts.

- **Rules:** comparable to `Kichikuou.Original` itself (about 26,000 hand-written lines and their tests),
  with the original as the reference: in "original mode" it can be compared with the faithful port by
  differential tests, a subsystem at a time (the battle, then the week and the commands, then the
  nations).
- **The story:** the 423,000 lines of scenes write the original's memory layout (773 record writes), so
  either an adapter keeps that layout under the new model (which is B again), or the scenes are converted
  into our own event format — the converter of the brief's first plan ("Porting strategy"), mechanical
  since every field's binding is known.
- **The presentation:** our screens today cover the script's screens and answer them by line
  (`GamePresenter`, `Original.Content`, `GameScreen`'s glue); with our own rules they would be driven by
  the model directly — simpler in the end, but most of `Original.Content` and the Godot glue rewritten.

Size: large — about the size of the whole port so far, minus the engine, the readers and the drawing.
Worth it only for different mechanics (the hybrid with Sengoku Rance's, brief scope decision 2), not for
content.

## 7. Questions for the owner

1. Which of the wishes are wanted, and how many of each (a few heroes, or many; new nations on the old
   map, or a new map)? A and B differ only past the free slots and the nations.
2. B as a fork beside the original (recommended: the faithful port stays the reference and the
   differential tests compare the two), or the original's own code turned into data? The second breaks
   the line-for-line mirror of the script that docs/porting-conventions.md asks for.
3. New scenes in System 3 script (runs today, an unpleasant language for writing) or in an event format
   of our own (a converter and a runner first)?
4. Art for new heroes and provinces (faces, sprites, the map tiles): who makes it, and is a mod's art
   shipped with it while the original's stays the player's files?
