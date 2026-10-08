# Strategic layer — survey index and plan

Everything outside the battle: the week loop, the player's commands, the enemy nations' turns,
data initialisation, and the story scenes as seen from the rules. The battle
([../battle/](../battle/README.md)) is ported and verified; this layer follows the same process —
survey → spec per part → port per part → golden captures.

## Surveys (2026-10-08)

| File | Scripts | Lines | Rules in it |
|---|---|---|---|
| [survey-main.md](survey-main.md) | `MAIN`, `COMMAND`, `COM`, `MMODE`, boot/title/save/endings | ≈15k | week loop (~650), player-phase shell (~250), `COM` weekly upkeep and event triggers (947); the rest is presentation |
| [survey-commands.md](survey-commands.md) | `C*` except the dungeon, `S*`, `各種DATA` | ≈15.6k | < 2k; ~85 % is dialogue and scene dispatch |
| [survey-dungeon.md](survey-dungeon.md) | `Cﾒｲｷｭｳ`, `Cﾀｶﾗ`, `Cｹﾝｷｭｳ` | ≈5.8k | floor counter, encounters, 27 dungeon records, 15 bosses; ~half text |
| [survey-nations.md](survey-nations.md) | `T*`, `T*2` | ≈9.5k | data-driven army AI on one shared template; `*2` files pick defenders when the player attacks |
| [survey-data.md](survey-data.md) | `SET`, `SET_CHR`, `SET_ITEM`, `SET_MAP`, `SET_VER` | ≈11k | pure data (no draws, no conditions); `SET_VER` is dead |
| [survey-events.md](survey-events.md) | `G*`, `I*` (136 files) | ≈423k | ~40 commands, goto-based; 19 rule mechanics hidden in scenes (~950 lines to hand-port) |

Not yet surveyed: `Iｶﾐ.ADV` (6.5k lines, God's Door, four battles) and `FIGT表示.ADV`.

## What the surveys established

- **One week** (`MAIN.ADV`): week + 1, history shift, autosave → (before Leazas is unlocked: a
  thieves-only week) → Leazas phase: income, troop/HP refill, expenses, `COMMAND` (which first
  calls `COM`: weekly upkeep, then two tiers of story triggers) → 11 gated nation phases in fixed
  order → end-of-week story checks.
- **Player phase.** No command ends it; each uses its own weekly flag (`Ui_Cmd*Avail`,
  `CmdAvail_*`) and returns to the command loop; only "Turn END" ends the phase. These `ui`
  flags are rule state — the golden comparison must stop ignoring them.
- **Nation AI** is fixed data: commander priority lists, troop thresholds, ordered target
  provinces; the first player-owned target is attacked through `FIGT_B` (already ported).
- **Scenes** are entered by setting `Ev_Entry` and calling the file (795 dispatch entries in
  125 files; no central table). They change the world through plain variable writes (unit
  join/leave, owners, items, gold, story stage).
- **Randomness** is everywhere and must keep its order: `COM` 12 sites, commands 21 live,
  dungeon 11, nations 60, scenes 108 (77 of them only choose dialogue, but all consume the
  generator).
- **Save** = one autosave per week at the start of the player phase; a manual save copies it.
  Pages 0, 1, 2, 4, 10 and all strings; page 4 (endings, gallery) is shared by every slot.

## Parts

Each part is one agent's spec + port, ≈2-4k script lines, following
[../porting-conventions.md](../porting-conventions.md). Dependencies point left.

| Id | Part | Source survey | Needs |
|---|---|---|---|
| **F0** ✓ | Shared services out of `Combat` into `Kichikuou.Core.Script` (2026-10-08): `ScriptContext` (state, draws, pauses, scene calls, jumps) with `WAO_SUB` (`ScriptContext.Wao.cs`), `M`/`WAO_MAP` (`ScriptContext.Map.cs`), `PCMPLAY`/`SS`/`SR` (`ScriptContext.Sound.cs`); `ScriptEvent`/`IScriptView`, `IScriptInput` (`IBattleInput` extends it), `IStoryHooks`; one `WindowEvent` for every `WAO_SUB` call. Script menus and map clicks are generalised with E1/E2 and W3 | main, dungeon, data | — |
| **F1** | Data: port `SET.ADV` (initial values, string bases, quirks) + a loader for `SET_CHR/ITEM/MAP` from the player's decompile; new-game start | data | F0 |
| **F2** | Harness: captures rooted at `COM`, `COMMAND`, each `T*`, `Cﾒｲｷｭｳ` and one `MAIN` week; `IM` click records; compare pages 2/10, string 10 and the `ui` command flags | main, commands, nations | — |
| **E1** | Event converter front end: decompiled scene → goto IR (`Say(ScriptRef)`, menus, `Set`, `Random`, calls) | events | — |
| **E2** | Event runtime in Core, implementing `IStoryHooks` (the battle uses it unchanged) | events | E1, F0 |
| **W1** | `COM` weekly upkeep and triggers | main | E2 |
| **W2** | `MAIN` week loop + the nation-phase hosts hidden in scenes (RULE-1) | main, events | W1, nations as hooks |
| **W3** | `COMMAND` player phase (one input call per choice; `C*` as hooks until ported) | main, commands | W1 |
| **P2** | Army screens `Sﾌﾞﾀｲ`, `Sﾌﾞｶ`, `Sｱｲﾃﾑ`, `Cﾀｶﾗ` (replaces the battle's `Sﾌﾞﾀｲ` hook) | commands | F0 |
| **P3** | Enemy-city attack and Use Force (`Cﾃｷﾄｼ` rules, `Cﾃｷﾄｼ2`) | commands | P2, N1 |
| **P4-P7** | Negotiations; own city + merchant; subordinates + ninja + construction; harem + `各種DATA` | commands | W3, E2 |
| **N1** | Nation template + Rebels, Patton, Rudra + all `T*2` defender pickers | nations | F0 |
| **N2 / N3** | Helman, Zeth, JAPAN / Kayblis | nations | N1 |
| **D1 / D2** | Dungeon flow / dungeon scenes with state | dungeon | P2, E2 |
| **G0** | Godot: strategic map screen, command menus, scene player, save/load | — | W3, E2 |

## Decisions (owner, 2026-10-08)

1. **Record data and scenes are read at run time from the player's own files** (the decompiled
   scenario today, like `ScenarioText`; reading the original archive directly may replace the
   decompile step later). Nothing derived from the game is committed — no generated tables of
   stats, no converted scene IR. Hand-ported rule code stays committed as before.
2. **First milestone: the whole prologue is playable** — new game → the thieves weeks → the
   prologue battles (already captured and verified) → the end of the prologue, with save/load.
3. **Strategic screens first reproduce the original layout**, like the battle screen; a
   Sengoku Rance–style redesign comes later with the owner. The look need not be pixel-exact
   (mechanics must be).
4. **The UI is swappable** (owner's wish): the original-layout UI stays when our own is written,
   and the player picks one at the start of a game, ideally also during play. Consequences for
   the design: the core only raises `ScriptEvent`s and awaits `IScriptInput`-based calls, so a
   UI is one implementation of those interfaces; everything a screen needs must be
   reconstructable from `GameState` (the `ui` variables: window, portraits, camera, markers) so
   a UI taking over mid-game can redraw; a switch happens at a safe point (a key wait or the
   command loop), through a view/input proxy that forwards to the current UI. A redesigned UI
   may need meaning the low-level events lack (who speaks, which province) — derive it in the
   presentation layer from the event and the state, or add semantic events next to the
   low-level ones; never move rules into the UI.
5. **Quirks are reproduced and registered** (2026-10-08): every bug or oddity of the original
   is ported as is and recorded in [../quirks.md](../quirks.md), so some can be offered as
   fixes later.

## Milestone 1 — the prologue

Order: **F0** → **F1** and **F2** (parallel) → **E1** → **E2** → **W1** → **W3** + the prologue
paths of the commands (`StoryStage = 0`: Pillage in `Cﾃｷﾄｼ`, `Cﾃｷﾄｼ2`, subordinate talk in
`Cﾌﾞｶ`, the stage-0 harem) → **W2** (the thieves-only week and the Leazas-phase skeleton) →
**G0** (map, command menus, scene player, save/load in the original layout). Each part is
verified by golden captures of prologue weeks (F2). The sweep below narrows it: at stage 0
W1 is only `COM.ADV:1-10`, W2 only `MAIN.ADV:1-82`, and Pillage needs `Sﾌﾞﾀｲ` (part of P2) and
the stage-0 hideout click in `Cﾒｲｷｭｳ`.

### What the prologue reaches (`StoryStage = 0`, swept 2026-10-08)

Lines refer to `_extract\scenario_named`. Not yet replayed by a capture.

**Path.** Title button 6 → `ｽﾀMENU L_00670` (`:279-291`): `WAO_MAP` 3, `M` 0, `Week := 8`,
`StoryStage := 0`, `Gold := 50`, `Iﾌﾟﾛﾛｰｸﾞ` (writes only `CurrentBgm := 1`, `Dgn_Cur := 1`),
`&MAIN`. `SET` ran at boot: units 1-4 alive, Leazas owns provinces 1-16 and 75.
`MAIN.ADV:19-82` (gate `StoryStage = 0`) is the whole week: week + 1, history shift, autosave
(`:20`), units 1-4 with status ≠ 0 get status 1, full HP, troops + 2 up to the cap (`:50-64`),
`Hist_Funds := Gold`, `COMMAND`, then `StoryStage = 99` → 1 + `Iﾊﾝﾗﾝ` (`:70-73`), loop. No
income, expenses, nation phases or end-of-week checks — **no `T*` file runs**. `COM.ADV:1-10`
plays `Iｼﾚｲﾌﾞ` 10 in week 9 and `Gｿｳﾙ` 4 otherwise and returns: no upkeep, no triggers.
`COMMAND`: weekly flags (`:38-46`), map loop (`:47-161`), stage-0 menu (`:183-225`).

| Input | Handler | Effect |
|---|---|---|
| Province 33/34 | `COMMAND.ADV:516-521` → `Cﾃｷﾄｼ` | stats as "??", menu Pillage / Return (`:62-84`); Pillage → `Iﾄｳｿﾞｸ` 1, then `ｵｰﾌﾟﾆﾝｸﾞ` if the stage changed (`:99-108`) |
| Any other province | `Cﾃｷﾄｼ2` | view only (`:42-64`) |
| Dungeon 1 | `Cﾒｲｷｭｳ:168-170`, `L_03cbd` | hideout banter, `Y 4, 3` (`:2804`) |
| Dungeons 2/3/15 | `Cﾒｲｷｭｳ:7-28, 172-173` | camera pan, strings 8/10, `Ui_CmdDungeonAvail := 0`, return |
| Menu: Harem | `COMMAND:337-343` → `Cﾊｰﾚﾑ` (`Sﾊｰﾚﾑ` list) | once a week; Sill, Soul, Anasel if `F_Thieves_State > 1`; `G*` 10; `Harem_SexCount + 1`, unit 1 EXP + `Harem_ExpReward` (`:553-577`) |
| Menu: Subordinates | `COMMAND:354-360` → `Cﾌﾞｶ` (`Sﾌﾞｶ`) | once a week a talk: unit 1 monologue (`N_RanceSelfTalk + 1`), 2/3/4 → `Gｼｨﾙ`/`Gﾊﾞｳﾝﾄﾞ`/`Gｿｳﾙ` 20; afterwards status view only |
| Menu: →Thieves' Hideout | `COMMAND:490-501` | camera pan |
| Menu: Load/Save | `COMMAND:370-376` | `ﾛｰﾄﾞｾｰﾌﾞ` with `RetVal 2` |
| Menu: Turn END | `COMMAND:377-425` | `Week > 22` → `Iﾄｳｿﾞｸ` 2, `ｵｰﾌﾟﾆﾝｸﾞ`, `StoryStage := 99` (`:419-424`) |

**Scenes.** `Iｼﾚｲﾌﾞ` 10, `Gｿｳﾙ` 4, `Gｿｳﾙ`/`Gﾊﾞｳﾝﾄﾞ`/`Gｼｨﾙ` 20 write nothing. `Gｼｨﾙ` 10
(`:2599`) and `Gｿｳﾙ` 10 (`:3063`) set `VAR0398 := 1`; `Gｱﾅｾﾙ` 10 sets `F_Thieves_State 2 → 3`
(first scene, leaves `Prov_Cur = 39`). `Iﾄｳｿﾞｸ` 1 (Pillage, `:110-953`): once a week
(`Ui_CmdAttackAvail`), first use `F_Thieves_State := 1`, commander 58 reset, `Sﾌﾞﾀｲ` picks units
(cancel returns), `FIGT_A` (`Bt_Kind 1`; the conquest at `FIGT_A:104-107` is skipped at stage
0). After a win `Y 4, 4` banter (`:342`, drawn even when replaced): `F_Thieves_State = 3` on 34
→ the Merci path (`L_013ff`); `F_Thieves_State = 1` on 34 with `VAR0398 = 1` → Anasel captured,
`F_Thieves_State := 2`; otherwise `Gold + 1`. Deaths are not fatal at stage 0 (`FIGTﾐｶDE`,
`死亡捕獲:29, 36-38, 106-110`).

**End.** Both ways lead to the duel `Iﾄｳｿﾞｸ L_016f2` (units 1, 3, 4, 2 vs commander 31,
`Bt_TroopLimit 5000`, `Bt_IsDuel 1`, `:1384-1399`): (A) Turn END in week 23, (B) the Merci
path. Losing it is the story: `StoryStage := 99` at `Iﾄｳｿﾞｸ.ADV:1841` (the first write off 0),
`ｵｰﾌﾟﾆﾝｸﾞ` (`F_OpeningSeen := 1`), back to `MAIN:70-72` → stage 1, `Iﾊﾝﾗﾝ` (stage 1 again,
`Week + 1`), next week is a Leazas week. Winning it is game over, ending 14 (`:2177-2178`).

**Draws outside the battle:** `Iﾄｳｿﾞｸ:342` (`Y 4, 4`), `Gｿｳﾙ:27` and `Gﾊﾞｳﾝﾄﾞ:9` (`Y 4, 2`),
`Cﾒｲｷｭｳ:2804` (`Y 4, 3`). None in the stage-0 parts of `MAIN`, `COMMAND`, `COM`, `Cﾃｷﾄｼ(2)`,
`Cﾌﾞｶ`, `Cﾊｰﾚﾑ`, `S*`, `Iｼﾚｲﾌﾞ` 10, `Iﾌﾟﾛﾛｰｸﾞ`.

**Files in milestone 1:** `MAIN`, `COMMAND`, `COM`, `Cﾃｷﾄｼ`, `Cﾃｷﾄｼ2`, `Cﾌﾞｶ`, `Sﾌﾞｶ`,
`Cﾊｰﾚﾑ`, `Sﾊｰﾚﾑ`, `Cﾒｲｷｭｳ` (top and `L_03cbd`), `Sﾌﾞﾀｲ`, `Iﾄｳｿﾞｸ`, `Iﾌﾟﾛﾛｰｸﾞ`, `Iｼﾚｲﾌﾞ` 10,
`Iﾊﾝﾗﾝ`, `Gｼｨﾙ`, `Gｿｳﾙ`, `Gﾊﾞｳﾝﾄﾞ`, `Gｱﾅｾﾙ`, `ｵｰﾌﾟﾆﾝｸﾞ`, `ﾛｰﾄﾞｾｰﾌﾞ`, `OVER` (ending 14), the
battle files, and the helpers `M`, `WAO_MAP`, `WAO_SUB`, `PCMPLAY`. Unreachable at stage 0:
`Cﾆﾝｼﾞｬ`, `Cｼﾊｲﾄｼ`, `Cﾃｲﾄ`, `Cﾀｶﾗ`, `Cｼｮｳﾆﾝ`, `各種DATA`, `Iｶﾝｶﾞｴ`, `TEST`, `Sｱｲﾃﾑ`, every
`T*`, the building scenes, `Gｶﾌｪ`/`Gﾏﾘｱ`, `Gｼｨﾙ2` 5.

**Open [I]:**
1. Path A never sets `Prov_Cur` before the duel, so `FIGT_A.ADV:1-30` sees the province
   clicked last: province 55 would start `IJAPAN` 3 (`N_JapanInv_Stage := 1`), and a non-zero
   `Prov_Defense` makes `FIGT_A:43-44` replace `TroopLimit 5000` with `Prov_BattleArea`
   (`Iﾄｳｿﾞｸ:1394`). Capture a week-23 Turn END.
2. The map hit-tests (`COMMAND.ADV:99, 145`) start at index 0: a click near (0, 0) would pick
   province 0 / dungeon 0 or 27-30.
3. `Cﾒｲｷｭｳ.ADV:11-13` reads a leftover `Cmdr_Cur` on dungeon clicks 2/3/15.
4. A unit left at status 0 by a battle is not refreshed by `MAIN.ADV:56`.
