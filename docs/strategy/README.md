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
| **F0** | Shared services out of `Combat`: message windows (`Battle.Wao.cs`), map ops (`Battle.Map.cs`), sound, scene calls (`IStoryHooks`) | main, dungeon, data | — |
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

## Decisions

Open — to be decided by the owner (see the session of 2026-10-08):

1. Record data (`SET_*`) and scenes: read at run time from the player's decompiled scenario,
   or generated into committed C#?
2. First playable milestone.
3. Look of the strategic screens during the port (the owner does not need a pixel-exact copy of
   the original; mechanics stay exact).
