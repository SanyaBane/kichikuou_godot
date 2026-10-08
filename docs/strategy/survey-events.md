# Survey — story scenes (`G*`, `I*`) as seen from the rules

Scope: the 122 `G*.ADV` (373,059 lines) and 14 `I*.ADV` (49,898 lines) files — 136 files,
422,957 lines. Source: `_extract\scenario_named\` (2026-10-08), profiled with Python over all
files (scripts in the session scratchpad, not in the repo). Confidence: **[C]** read or
counted, **[I]** inference, **[G]** guess. The project brief sends these files through an
**automatic converter into our own event format**; this survey defines what that converter and
the event runtime must support and which pieces are really rules.

## 1. Role

The scene files hold ≈85% of the scenario: dialogue with portraits, CGs and music, story flags,
and the menus of a few "location" hubs. Every file is a library of numbered scenes reached by a
page call (`%#FILE.ADV:`) after the caller sets `Ev_Entry` (`VAR0520`); each scene ends with a
page return (`%0:`). The callers are the rule files: the weekly dispatcher `COM`, the week loop
`MAIN`, the player commands `C*`, the nation turns `T*`, the battle `FIGT_A/B/FIGT`, the
death/capture handler `死亡捕獲` and the CG gallery `CG`. Most files only print text and set
their own flags, but a minority also mutates the world (unit joins and deaths, commander
records, items, gold, province owners, story stage, the week counter) and a handful **host
rule mechanics outright**: the AL Church nation phase, the La Vaswald and Xacalite weekly
attacks, raider troop pools, Kesselring's march, minigames with random rolls, and the God
dungeon floors (§7.2).

## 2. Inventory (aggregate)

### 2.1 Size and entry tables

| Measure | Value | Source |
|---|---|---|
| Files / lines | 136 / 422,957 (median 2,626, mean 3,110) | [C] |
| Size buckets | <500: 2 · 500-1,999: 41 · 2,000-4,999: 68 · ≥5,000: 25 | [C] |
| Largest | `Gｱﾘｽ` 9,641 · `Iﾏｼﾞﾝ` 9,278 · `Gｼｨﾙ` 8,422 · `Gﾅｷﾞ` 7,172 · `Gｶﾌｪ` 7,123 · `Gｱﾋﾞｧﾄｰﾙ` 6,902 · `IJAPAN` 6,880 · `Iｾﾞｽ` 6,851 | [C] |
| Files with a top dispatch table | 125, holding **795** `{Ev_Entry = n: @L_x}` entries (1-23 per file) | [C] |
| Files without a table | 11: `GDX`, `Gｱﾘｽ`, `Gﾀﾐ`, `Gﾑﾐｿ`, `Gﾜｵ`, `IMﾗﾝﾄﾞ`, `Iｱﾘｽ`, `Iｶﾐ`, `Iｶﾝｶﾞｴ`, `Iﾄｼ` (1 line, `%0:` only, never called), `Iﾌﾟﾛﾛｰｸﾞ` | [C] |

Entry numbers follow a convention [C, counted over the tables; meaning from the callers]:

| `Ev_Entry` | Entries | Meaning | Set by |
|---|---|---|---|
| 1-9 | 454 | story scenes of that character/arc | `COM`, `MAIN`, `T*`, `C*`, `FIGT_A/B`, `死亡捕獲` |
| 10 | 62 | harem visit | `Cﾊｰﾚﾑ.ADV:35`, then 49 calls without re-setting it |
| 20 | 49 | subordinate "Talk" | `Cﾌﾞｶ.ADV:25` |
| 11-51 (not 20) | 85 | later arc steps (`Iﾏｼﾞﾝ` 11-20/50/51 by `F_DemonWar_Stage`, `Iﾏｼﾞﾝ2` 21-30, `Gｼｨﾙ` 21-27 Sill kidnap, death scenes 40-43 in `Iｾﾞｽ`) | `MAIN.ADV:897-1033`, `死亡捕獲.ADV:229-297` |
| 100 | 4 | special (e.g. `Gｶﾅﾐ` 100 from `FIGT_B.ADV:687`) | |
| 300-304 | 141 | **CG gallery replay** (82 files have 300) | `CG.ADV`, e.g. `:827-836` |

Example table: `Gｴｸｽ.ADV:1-25` (entries 10, 1-6, 20); the first label after the table
(`Gｴｸｽ.ADV:26`) is also the **default**: an unmatched `Ev_Entry` falls through into it [C].
`Iﾊﾝﾗﾝ.ADV:1` uses a range test `{Ev_Entry < 299:` [C]. Files without a table are hubs or
one-shot sequences entered at line 1 (`Gﾀﾐ` tests `CallArg` instead, `Gﾀﾐ.ADV:1`) [C].

### 2.2 File families (by who calls them)

| Family | Files (examples) | Entered from | Character |
|---|---|---|---|
| Character scenes | ~100 `G<name>` (`Gｴｸｽ`, `Gｼｨﾙ`, `Gﾅｷﾞ`, …) | `COM` triggers, `Cﾊｰﾚﾑ` (10), `Cﾌﾞｶ` (20), `T*`, `FIGT_A/B`, `死亡捕獲`, `CG` (300+) | dialogue + own flags; some joins/deaths |
| Location hubs | `GDX`, `Gｱﾘｽ`, `IMﾗﾝﾄﾞ`, `Gﾏｹﾝ`, `Gｶﾊﾟｰﾗ`, `Gﾄｸﾍﾞﾂ` | `Cｼﾊｲﾄｼ` (own-city visit, 75 sites into 50 files) | menus (`]`) looping back, small economy effects |
| Dungeon events | `G1`, `G2`, `Gﾄｳｹﾞﾝ`, `Gﾎﾗｶﾞ`, `Gﾀﾐ` | `Cﾒｲｷｭｳ` (40 sites, 22 files), `FIGT.ADV:480` (`Gﾀﾐ`) | read the party `Bt_PlayerParty1-4`; stat boosts, items |
| Arcs `I*` | `Iﾌﾟﾛﾛｰｸﾞ`, `Iﾊﾝﾗﾝ`, `Iﾄｳｿﾞｸ`, `Iｼﾚｲﾌﾞ`, `IJAPAN`, `Iｾﾞｽ`, `Iﾏｼﾞﾝ(2)`, `Iｼｬﾝｸﾞﾘﾗ`, `Iｶﾐ` | `ｽﾀMENU`, `MAIN`, `COM`, `COMMAND`, `T*` | stage changes, mass province transfers, scripted battles |
| Phase hosts | `GAL` (entry 5), `Gｻｲｾﾞﾙ` (12), `Gﾏｼﾞｯｸ` (5), `Gｶﾐｰﾗ` (8), `Gｼﾞｰｸ` (1-5) | `MAIN.ADV:296, 665-813, 882, 929` | **rules**: random targets, `FIGT_B`/`FIGT_A` (§7.2) |
| Gallery-only extras | `Gｱﾘｽ2`, `Gﾑﾐｿ`, `Gﾜｵ` | other scenes (`Gｱﾘｽ.ADV:2813-9290`), `ｽﾀMENU.ADV:575` | jokes/staff room, display effects (`CC`, `J0`) |

### 2.3 Procedures shared by every file

Each file ends with an identical **stub block** of 13 labels (`G1.ADV:3242-3324`): each stub
is `!Ui_WaoOp : n! %#WAO_SUB.ADV: \0:` for window ops 111/112/113 (open), 311/312/321/322
(portraits left/right), 121-123, 131 (fullscreen), 331 (centre CG), 0 (close), 1 (wait); the
second `!Ui_WaoOp` after each `\0:` (211, 212, …, 2) is **unreachable** [C]. The 139,813 local
calls into stubs carry the whole presentation; only **186** local calls go to real
subroutines (list in §7.3) [C]. A message is always
`!CallArg : <portrait CG>! \L_portrait: \L_open: ZH 1: 'text' R 'text' \L_wait: \L_close:`
(`Gｴｸｽ.ADV:63-69`) [C].

## 3. Call graph

### 3.1 Calls into scenes (845 `%#` sites in 41 caller files, 135 of 136 files reached)

| Caller | Sites | Distinct targets | Notes |
|---|---|---|---|
| `COM.ADV` | 151 | 73 | weekly triggers; see `survey-main.md` §2.3 |
| `CG.ADV` | 141 | 82 | gallery, `Ev_Entry` 300-304 |
| `Cｼﾊｲﾄｼ` / `Cﾊｰﾚﾑ` / `Cﾌﾞｶ` / `Cﾒｲｷｭｳ` / `Cﾃｷﾄｼ(2)` / `Cﾆﾝｼﾞｬ` / `Cｼｮｳﾆﾝ` | 75 / 56 / 51 / 40 / 19 / 8 / 3 | 50 / 49 / 49 / 22 / 14 / 3 / 2 | player commands |
| `T*` (10 files) | 147 | 46 | `Tｹｲﾌﾞﾘｽ` 54, `Tｾﾞｽ` 48 (`Gﾁｽﾞｺ`, `Gﾅｷﾞ`, `Gﾊﾟﾊﾟｲｱ`, `Gﾏｼﾞｯｸ`, `Iｾﾞｽ`), `Tﾍﾙﾏﾝ` 19, … |
| `MAIN.ADV` | 49 | 15 | phase hosts and arc stages (§2.2) |
| `死亡捕獲.ADV` | 41 | 32 | death/capture scenes after a battle |
| `FIGT_A/B/FIGT` | 13 / 11 / 1 | 9 / 4 / 1 | already hooked in the port (`IStoryHooks.RunEventAsync`, `Battle.cs:68-74`; `Battle.Flow.Support.cs:241-252`) |
| `COMMAND.ADV` | 10 | 6 | buildings, `Gｶﾌｪ` 30, `Iｶﾝｶﾞｴ` |
| `ｽﾀMENU.ADV` | 6 | 6 | prologue, chapter starts, info |
| scenes themselves | 23 | 16 | e.g. `Gｱﾘｽ`→`Gｱﾘｽ2` ×3, `Gｼｨﾙ`→`Gｼｨﾙ2` ×2, `GAL.ADV:2993-2999`→`Gｾﾙ` |

The caller sets `Ev_Entry` immediately before the call in 748 sites; 97 sites rely on a value
set earlier (`Cﾊｰﾚﾑ`, `Cﾌﾞｶ`, `Gﾀﾐ`, `Sﾌﾞﾀｲ`-style sub-files) [C, 15-line look-back].

### 3.2 Calls out of scenes

| Target | Sites | What for |
|---|---|---|
| `WAO_SUB.ADV` | 3,116 (2,872 in stub blocks) | message window, portraits, centre CG |
| `M.ADV` | 2,733 | map: op 0 redraw ×2,117, op 1 pan to `MapTargetX/Y` ×273, op 10 marker ×266, op 4 ×44, op 3 ×27, ops 11/12 ×3 each |
| `FIGT_A.ADV` | 18 | scripted battles/duels (§4.6) |
| `FIGT_B.ADV` | 8 | defensive battles hosted by scenes (`GAL.ADV:2836, 2973`, `Gｻｲｾﾞﾙ.ADV:490`, `Gｶｾｲ.ADV:3093`, `Gﾚﾘｭｰｺﾌ.ADV:63`, `Gｹｲﾌﾞﾘｽ.ADV:757`, `Iﾏｼﾞﾝ.ADV:80, 596`) |
| `FIGT.ADV` | 4 | God-dungeon garrison battles, `Iｶﾐ.ADV:4607, 4670, 4733, 4796` |
| `FIGT表示.ADV` | 19 | battle-screen story inserts, itself dispatched on `Ev_Entry` (`FIGT表示.ADV:1-30`) |
| `死亡捕獲.ADV` | 3 | after scripted battles (`Gｹｯｾﾙ.ADV:2436, 2963`, `Gﾅｷﾞ.ADV:4172`) |
| `Sﾌﾞﾀｲ.ADV` | 5 | party picker (input) (`Gｶﾌｪ.ADV:1959`, `Gﾄｳｹﾞﾝ.ADV:278`, `Iｶﾐ.ADV:4816`, `Iｼｬﾝｸﾞﾘﾗ.ADV:4526`, `Iﾄｳｿﾞｸ.ADV:305`) |
| `PCMPLAY.ADV` | 10 | sound effect (`RetVal` = sound number, e.g. `Gﾏﾘｱ.ADV:4553`) |
| `WAO_MAP.ADV` | 5 | map flag/terrain redraw (`Iｼｬﾝｸﾞﾘﾗ.ADV:908`, `Iｱﾘｽ.ADV:1321`) |
| `ｵｰﾌﾟﾆﾝｸﾞ.ADV` | 1 | opening (`Iｱﾘｽ.ADV:1340`) |
| `&#OVER.ADV` (jump, no return) | 14 in 9 files | game over / endings after `!EndingId : n!` (`Iｶﾐ.ADV:1335-4560` ×6, `Gﾏﾘｱ.ADV:4863`, `Iﾄｳｿﾞｸ.ADV:2177`, …) |

No scene calls `COM`, `MAIN`, `COMMAND`, `C*`, `T*` or `SET*` [C]. Nested calls happen: a scene
calls `FIGT_A`, which may call another scene (`FIGT_A.ADV:168-202`) [C].

## 4. State

### 4.1 Assignments by dictionary group (18,167 `!var : expr!` in all)

| Group | Writes | Of which | Notes |
|---|---|---|---|
| system | 9,983 | `CallArg` 9,972 | portrait CG / map op / sub-file argument — presentation |
| ui | 4,307 | `Ui_WaoOp` 3,116; `MapTargetX/Y` 638; `MapCamX/Y` 540; `Ui_Cmd*Avail` 13 | the 13 command-availability writes are **rule state** (`Gﾊﾟｯﾄﾝ.ADV:3122`, `Gｶﾌｪ.ADV:5881`, …) |
| story | 998 | 235 vars: `F_*` 190 vars/763 writes, `N_*` 37/185 | 99% constant writes; most flags are written by one or two files |
| unit | 770 | `Unit_Cur` 431, `Unit_Status` 108, stats 231 | cursor-addressed records |
| cmdr | 493 | `Cmdr_Cur` 177, `Cmdr_Status` 85, stats 231 | |
| scratch | 433 | `Tmp0001` 106, `Tmp0021` 70, `Tmp0690` 44 (random dialogue pick), `Tmp0621` 33 | carried into sub-calls |
| prov | 418 | `Prov_Cur` 336, `Prov_Owner` 44, adjacency 13, `Freya_TargetProv` 10 | |
| battle | 343 | `Bt_EnemyParty1-4` 132, `Bt_PlayerParty1-4` 81, `Bt_Kind` 44, `Bt_EnemyRetreatMode` 31, `Bt_IsDuel` 30 | battle setup before `FIGT_*` |
| event | 200 | `Harem_ExpReward` 83 (45 files), `Ev_Entry` 40, God-dungeon rearguards 32 | |
| item | 86 | `Item_Holder` 40, `Goliath_*` 3 | |
| unnamed (`VAR0063`…) | 37 | 19 vars | dictionary has them at G or "unused" |
| time | 35 | `StoryStage` 6, `Week` 4, `*_EventWeek` timers | |
| dungeon | 32 | `Dgn_State` 6, `Dgn_Prov` 5, `F_Arcy_State` 3 | |
| history | 18 | AL raider pools 16 (`GAL`), `Hist_SundryExp` 2 | |
| economy | 13 | `Gold` 11, `Bld_GirlsAcademy` 2 | |

Reads in conditions/expressions are dominated by `Ev_Entry` (939), `Unit_Status` (309),
`Prov_MapX/Y` (297 each, camera targets), `RetVal` (295) and story flags [C]. Implicit inputs
from the caller: the cursors (`Unit_Cur`, `Prov_Cur`, `Cmdr_Cur`), the party
`Bt_PlayerParty1-4` (13 files read it without writing it, e.g. `G1`, `G2`, `Gﾄｳｹﾞﾝ`, `Gﾁｽﾞｺ`),
`Bt_Result`/`Bt_PlayerLosses` after a nested battle (`Iﾄｳｿﾞｸ.ADV:315-317`,
`Gﾏｼﾞｯｸ.ADV:2398`) and stale `Ev_Entry` [C].

### 4.2 World mutations (the event ↔ state interface)

| Operation | Pattern | Sites | Examples |
|---|---|---|---|
| Unit joins | `Unit_Cur := u; Unit_Status := 1` (2 = joined, already acted) | 30 + 33 | `Gｲｿﾛｸ.ADV:668-669`; `Iｶﾐ.ADV:4826-4840` (party restore) |
| Unit leaves / dies | `Unit_Status := 0` | 45 | `GAL.ADV:2771-2775` (unit 44), `Gﾊﾟﾊﾟｲｱ.ADV:4768-4828` (whole party) |
| Unit record created | all 32 fields written | 4 | unit 48 Hanny King `Gﾊﾆｰ.ADV:342-382` / Kaiju Prince `Gｵｳｼﾞ.ADV:321-361` (`Unit48_Identity`), Carfeknight `Gｶﾌｪ.ADV:4084-4123`, stage-1 roster `Iﾊﾝﾗﾝ.ADV:14-34` |
| Unit stat change | `Unit_Atk/Def/MaxHP/Magic/Troops/Exp…` | ≈80 | button room `G1.ADV:907-1100`, Maria `Gﾏﾘｱ.ADV:3702-3703`, Melfeis `Gﾒﾙﾌｪｲｽ.ADV:3448-3449` |
| Commander out / back | `Cmdr_Status := 0 / 1` | 46 / 39 | `Gﾊﾟｯﾄﾝ.ADV:4457`, `Iﾏｼﾞﾝ.ADV:69-79` |
| Commander record created | 21 fields; slot **100** is a reusable scratch opponent | 6 | `G1.ADV:1107-1130`, `GAL.ADV:2840-2862`, `Gｻﾃﾗ.ADV:3402-3421`, `Gｾﾙ.ADV:5790-5813`; fixed ids `Gｶｾｲ.ADV:2758` (71), `Gｹｲﾌﾞﾘｽ.ADV:1370` (135) |
| Items | `Item_Holder := 100` gain, `0` lose, `n` equipped by unit n | 40 | `G1.ADV:259-261`, `Gｹﾝﾀﾛｳ.ADV:1637-1642`, `Iﾏｼﾞﾝ.ADV:3310-3317` |
| Gold | `Gold := Gold ± n` / `:= n` | 11 | `Gｶﾊﾟｰﾗ.ADV:2117-2163`, `Gﾄｳｹﾞﾝ.ADV:207`, `Iﾊﾝﾗﾝ.ADV:58` (`:= 685`), `Gﾌﾚｲｱ.ADV:3071` (`:= 2`) |
| Province owner | `Prov_Cur := p; Prov_Owner := o` (+ flag redraw `Tmp0021 := Prov_Owner + 819; CallArg := 4`) | 44 in 9 files | §4.3 |
| Map topology | `Prov_Adj1-5` rewired | 13 | Shangri-La appears/vanishes `Iｼｬﾝｸﾞﾘﾗ.ADV:912-922, 1096-1100` |
| Province fields | `Prov_BgCg/Defense/Economy/BattleArea` | 13 | Zeth palace destroyed `Gﾊﾟﾊﾟｲｱ.ADV:1799-1802`, `Gﾐｷ.ADV:4645-4646` |
| Story stage | `StoryStage := 1/2/3/4/99` | 6 | `Iﾊﾝﾗﾝ.ADV:11`, `Iｼﾚｲﾌﾞ.ADV:168, 1527, 2689`, `Iﾄｳｿﾞｸ.ADV:1841`, `Iﾏｼﾞﾝ.ADV:8982` |
| Time | `Week := Week + 1` (+ date recompute) | 4 | `Iｼｬﾝｸﾞﾘﾗ.ADV:2262, 2585, 2871`, `Iﾊﾝﾗﾝ.ADV:12` |
| Week timers | `X_EventWeek := Week + n` | 22 | `Gｲｿﾛｸ.ADV:2402`, `Gｶﾞﾝｼﾞｰ.ADV:705`, `Gﾏｹﾝ.ADV:339` |
| Command mode / availability | `Ev_CommandMode`, `Ui_Cmd*Avail` | 18 | `Gｶﾌｪ.ADV:5879-6059`, `Gﾏﾘｱ.ADV:117-123` |
| Harem EXP | `Harem_ExpReward := n`, applied by `Cﾊｰﾚﾑ.ADV:557-574` | 83 | |
| Ending | `EndingId := n; &#OVER.ADV` | 14 | §3.2 |

No scene writes the persistent page 4 (`Glob_*`) [C]; `Iｱﾘｽ.ADV:19` reads `F_OpeningSeen`
from the global save with `LP` [C]. `Iｱﾘｽ.ADV:119` sets `DebugMode := 1` (secret click) [C].

### 4.3 Province-owner writes

| Where | What |
|---|---|
| `Iｼﾚｲﾌﾞ.ADV:2689-2702` | `StoryStage := 2`, provinces 2-16 → player (loop) |
| `Iｼﾚｲﾌﾞ.ADV:562, 569, 2696, 4183` | single provinces → player |
| `Gﾐﾈﾊﾞ.ADV:2449-2462` | every Helman (4) province 30-54 → player (loop); `:2470-2498` five back to Helman |
| `Iｾﾞｽ.ADV:1302-1318` | every Zeth (7) province 58-76 → player (loop), `F_Zeth_War := 2` |
| `Iﾊﾝﾗﾝ.ADV:2142-2170` | provinces 3-7 → rebels (2) |
| `Gﾊﾟｯﾄﾝ.ADV:2162, 3008, 4422-4717` | Patton (5) takes 46/47, later 40-47 → player |
| `Iﾏｼﾞﾝ.ADV:6414-8444`, `Iﾏｼﾞﾝ2.ADV:39-81` | demon war: 15 provinces → Kayblis (8), one per `F_DemonWar_Stage` |
| `Iｼｬﾝｸﾞﾘﾗ.ADV:1088, 4403`; `Gｹｯｾﾙ.ADV:603, 2838` | Shangri-La 29 → 3 then 1; Kesselring's province |

### 4.4 String table

`MS n, "text"` ×75 (slot 4/5 commander name ×30, slot 10 army name for the battle header ×18,
slot 1 unit name ×5), `MM` ×175 — 110 load a province name into slot 8 for display,
28 load a unit name into slot 1, **37 write the persistent unit/commander name tables**
(`MM Unit_StrBase + (Unit_Cur - 1) * 3 + k, n`, e.g. `Iﾊﾝﾗﾝ.ADV:24-26`, `GAL.ADV:2844-2845`),
`MA` 14 / `MH` 6 / `MP` 2 for composed strings (`Gﾄｳｹﾞﾝ.ADV:1492-1500`) [C].

### 4.5 Aliasing and scratch

Records are always addressed through cursors; scenes move `Unit_Cur`/`Prov_Cur`/`Cmdr_Cur`
1,000 times and never restore them [C], so the caller sees the moved cursor. `Ev_Entry` is a
global: a nested `FIGT_A` → scene call overwrites it, and some files test it again later
(141 gallery guards) [I]. `M.ADV` and `WAO_SUB.ADV` leave values in `Tmp0021-0030` and the
camera (ported for battle in `ScriptContext.Map.cs`/`ScriptContext.Wao.cs`, `docs/battle/README.md`) [C].

### 4.6 Battle setup by scenes

30 battle calls in 17 files. A scripted fight sets `Bt_PlayerParty1-4`, `Bt_EnemyParty1-4`,
`Bt_EnemyRetreatMode`, `MS 10` (army name), `Bt_Kind := 1|2|3`, `Bt_IsDuel`, optionally
`Bt_WallBonus`, `Bt_TroopLimit`, `Bt_BackgroundCg`, `Bt_TargetProv`, then `%#FIGT_A/B/FIGT`,
resets `Bt_Kind`/`Bt_IsDuel` and branches on `Bt_Result` or `Cmdr_Status`
(`Gｾﾙ.ADV:5816-5830`, `Iﾄｳｿﾞｸ.ADV:1384-1399`, `Iﾏｼﾞﾝ.ADV:67-82`) [C].

## 5. Randomness

`Y 4` appears **108 times in 54 files** (plus 20 `Y 3` waits and one `Y 1, 1` text reset in
`Gﾜｵ.ADV:631`) [C]. Every draw consumes the shared generator, so draw order inside scenes
matters for any golden test that spans a scene.

| Purpose | Draws | Sites |
|---|---|---|
| **Rule mechanics** (target choice, troop arithmetic) | 22 | `GAL.ADV:2738` (troop levy, `rnd(Unit_Troops/6)` per unit), `:2812` (rnd 10 target), `:2867-2917` (4 × rnd 500 growth + 4 × rnd 2 raid), `:2958`, `:2964` (rnd 28 target); `Gｻｲｾﾞﾙ.ADV:474` (rejection loop); `Gﾏｼﾞｯｸ.ADV:2296` (rnd 48 victim); `Gｶｾｲ.ADV:469` (1/7); `Gﾌﾚｲｱ.ADV:2412`; `Gｶﾊﾟｰﾗ.ADV:2108, 2131, 2154, 2191` (rank ± rnd); `Gﾄｳｹﾞﾝ.ADV:1556, 1562` (contest) |
| Story rolls that set flags | 9 | `Gｱｵｲ.ADV:1191, 1311, 1396`; `Gｲｿﾛｸ.ADV:4599` (1/5, needs hospital); `Gｹﾝﾀﾛｳ.ADV:608`; `Gﾊﾆｰ.ADV:986`; `Gﾏﾘｱ.ADV:5510`; `Gｶﾅﾐ.ADV:182`; `GAL.ADV:3420` |
| Dialogue variant (`!Tmp0690 : RetVal!` then text) | 41 | e.g. `Gﾏｼﾞｯｸ.ADV:108-501` (7 in one visit), `Gｱﾘｽ.ADV:28-2828` |
| Branch to another sub-scene / joke | 35 | e.g. `Gｿｳﾙ.ADV:27`, `Gﾘｯｸ.ADV:8, 19`, `Gｶﾐｰﾗ.ADV:3661-4422` |
| Draw shown as a number only | 1 | `Gﾌｳｶ.ADV:2258` (`Y 4, 99` then `H 0, RetVal`) |

Several draws sit at the very start of a scene, before any condition (`Gﾏｼﾞｯｸ.ADV:108`), or
are drawn and then ignored when a flag fails (`Gﾊｳﾚｰﾝ.ADV:2299`) [C]. `Y 4` with argument
`Tmp0621`/`Tmp0226`/`Tmp0228`/`Unit_Troops / 6` can be 0 or 1, which returns the argument
without drawing (`docs/spike-findings.md`, Randomness) [C].

## 6. Input and display

### 6.1 Commands used in the scenes (complete set)

| Class | Mnemonic → count |
|---|---|
| Text | `'…'` 116,136 · `ZH` 61,079 (half-width before every string) · `R` 57,854 · `ZM` 2,272 (font size) · `X` 121 (print string slot: 1 ×84, 8 ×33, 4 ×3, 10 ×1) · `H` 82 (print number) |
| Strings | `MM` 175 · `MS` 75 · `MA` 14 · `MH` 6 · `MP` 2 |
| Windows/portraits | local stub calls 139,813 → `WAO_SUB` (§2.3) · `CallArg` writes 9,972 |
| Graphics | `G` 617 · `PF0/PF1` 629/623 (fade) · `EC` 96 (erase text) · `J0` 49 / `J1` 3 (CG position) · `CC` 27 · `PW0/PW1` 19/18 · `CS` 7 · `CB` 5 · `B4` 2 · `B3` 1 · `CL` 2 · `CF` 1 · `ZT0` 1 |
| Sound | `SS` 1,170 (BGM) · `PCMPLAY` 10 calls |
| Map | `M.ADV` 2,733 calls · `WAO_MAP` 5 · `MapTarget*/MapCam*` 1,178 writes |
| Flow | labels 3,770 · `{…:` 3,003 · `@` jump 2,045 · `\L` call 139,999 (186 non-stub) · `\0:` 3,309 · `%0:` 1,385 · `%#` 5,945 · `&#` 14 |
| State | `!var : expr!` 18,167 (≈3,900 non-presentation) · `Y 4` 108 · `LP` 1 |
| Input | menus: 123 `]` with 426 items (`$L_x$ 'text' $`), 74 items inside `{…}` (conditional) · `IM` 115 (114 are button-release waits `IM …: {RetVal: @back}`, mostly before menus) · `A` 9 (all `Gﾀﾐ`) · `IK 0` 2 (`Gﾜｵ.ADV:494`, `Iｱﾘｽ.ADV:1309`) · `Y 3` waits 20 · `Sﾌﾞﾀｲ` 5 |

Not used: arrays (`DC/DS/DF` and `var[i]`), `~` functions, `<` loops, `ZG`, `US/UG`, `F1/F3`
tables [C]. Everything is plain variables, constant labels and nested `if`.

### 6.2 Player choice points

Menus are the only real choices (123 in 67 files, `Gｱﾘｽ.ADV:82-100` is typical: release-wait,
items, `]`, then `@` back to the menu if cancelled — 310 of the 482 backward jumps are such
re-shows) [C]. The other inputs are the party picker `Sﾌﾞﾀｲ` (5) and key/click waits inside
`WAO_SUB` op 1. Display (fonts, window ops, fades, CG placement, map camera) can be redesigned
freely as long as the order of waits and choices is kept.

## 7. Rules vs presentation

### 7.1 How clean the split is

- 34 files have no record write, no `Y 4` and no battle call; 50 have no record write at all
  [C]. Their state effect is story flags only.
- ≈3,900 of 422,957 lines change non-presentation state; 1,000 of those only move cursors.
- Control flow is simple: all jumps and calls use constant labels, no computed jumps; the only
  dispatch is `Ev_Entry` compare chains [C]. Difficulties for a converter:
  - 7 labels sit **inside** an `if` block (jump into a block): `Gｶﾐｰﾗ.ADV:3323`,
    `Gｿﾐｰﾀ.ADV:1560`, `Gﾏﾘｱ.ADV:3443, 5585`, `Gﾒﾘﾑ.ADV:1784`, `Gﾘｱ.ADV:564`,
    `Gﾚｲﾗ.ADV:5919` [C] — the IR must be goto-based, not tree-based.
  - 482 backward jumps: 310 menu re-shows, 114 release-waits, 29 record/counter loops,
    29 others (multi-hundred-line story loops, e.g. `Gｾﾙ.ADV:2865 → 878`) [C].
  - `%0:` inside a local subroutine (`Gﾄｳｹﾞﾝ.ADV:905-964`) and `\0:` used as an early exit
    inside nested ifs (`Gﾚﾘｭｰｺﾌ.ADV:56-65`) — page return must unwind any local call depth [I].
  - Entry fallthrough to the first label; unmatched entries play the default scene [C].
  - Gallery guards `{Ev_Entry = 30x: %0:}` ×141 (140 return immediately) cut off the
    state-changing tail of a replayed scene [C]; whether the replayed part writes state before
    the guard is unknown.

### 7.2 Rule mechanics hosted in scene files

| # | Mechanic | Where | Called from |
|---|---|---|---|
| R1 | AL Church war start: every unit 1-43 (minus 19, 20, 25, 29, 30, 41, 42) loses `rnd(Troops/6)` troops **and max troops**; unit 44 leaves; raider pools := 1000 | `GAL.ADV:2603-2608, 2727-2775` | `COM.ADV:148` (entry 4) |
| R2 | AL Church nation phase: main army rnd(10) province, if player-owned `FIGT_B` vs 139-142 (who regain +2 HP via `L_0196a` `:1409`); four raider pools +rnd(500)/week, raid on a 1/2 roll when >800 troops into rnd(28) province with commander slot 100 rebuilt as "AList"; pool := 10 after defeat | `GAL.ADV:2805-2985` | `MAIN.ADV:296`, `COM.ADV:943` (entry 5) |
| R3 | La Vaswald (cmdr 98) weekly attack: `Y 4, Tmp0621` with `Tmp0621` shrinking from 80 until a player province ≠ 75; `LaVaswald_Prov`, `FIGT_B` | `Gｻｲｾﾞﾙ.ADV:471-490` | `MAIN.ADV:882` (entry 12) |
| R4 | Xacalite (cmdr 97) weekly duel: up to 9 × rnd(48) (32 → 31) to pick an active unit, forced `FIGT_A` duel, `N_Xacalite_KillsLeft -= Bt_PlayerLosses`, 97 healed | `Gﾏｼﾞｯｸ.ADV:2283-2410` | `MAIN.ADV:929` (entry 5) |
| R5 | Mars the Great Army (cmdr 71, 32,600 troops): 1/7 roll to target unit 2; `FIGT_B` at province 1; `N_Mars_Score` reset when 71 falls | `Gｶｾｲ.ADV:465-480, 2758-2781, 3080-3094` | `COM.ADV:745` (entry 4 → `:465`); the `FIGT_B` block probably entry 2 from `MAIN.ADV:258` [I] |
| R6 | Kesselring's march: fixed route 1 → 4 → 23 → 19 → 61 → 70, stops on a Kayblis-owned province | `Gｹｯｾﾙ.ADV:2991-3037` | `Tｹｲﾌﾞﾘｽ.ADV:233-1283` |
| R7 | Lelyukov (cmdr 26) attacks with each of commanders 28, 23, 24, 25, 22 still alive, `FIGT_B` each [I] | `Gﾚﾘｭｰｺﾌ.ADV:30-65` | `Tﾍﾙﾏﾝ.ADV:43, 51, 175` |
| R8 | Lady unit (cmdrs 143-145) duel at Leazas castle, each +50 troops and revived before it | `Gｶﾌｪ.ADV:2019-2049` | unknown entry (Lady Caravan walk itself: `COM.ADV:56-67`) |
| R9 | Freya ninja mission target: province from `DateWeek` × rnd(2), `N_Freya_Missions + 1` | `Gﾌﾚｲｱ.ADV:2412-2445` | `Tﾍﾙﾏﾝ.ADV:39` (entry 1) |
| R10 | Kapala sponsorship: pay 10/50/100 gold, rank += rnd(n) − 1, capped 12; report flag | `Gｶﾊﾟｰﾗ.ADV:2035, 2104-2200` | `Cｼﾊｲﾄｼ`, `COM` |
| R11 | Magic Research Institute projects: done week = `Week + Unit_Magic * 2` / +10 / +4, cancelled when the unit is gone | `Gﾏｹﾝ.ADV:338-339, 427, 511, 2545-2552` | `Cｼﾊｲﾄｼ` |
| R12 | Tougen bunny contest: automatic duel of two 100-point pools hit by `rnd(Tmp0226)`/`rnd(Tmp0228)`, items 49/50 as prizes, record kept | `Gﾄｳｹﾞﾝ.ADV:1486-1600` | `Cﾒｲｷｭｳ` |
| R13 | God dungeon: per-floor garrison `FIGT` (`Bt_Kind 3`, enemy 187…), rearguard units saved in `Ev_GodRear2F-5F_*`, party picker loop | `Iｶﾐ.ADV:1-12, 4561-4900` | `Cﾒｲｷｭｳ.ADV:4556` |
| R14 | Shangri-La: time passes (`Week + 1` and its own date calculation `L_05f62`, duplicating `MAIN`'s), adjacency rewired, province 29 changes hands | `Iｼｬﾝｸﾞﾘﾗ.ADV:908-922, 1088-1100, 2262, 5309-5325` | `Gﾏﾘｽ.ADV:1037`, `COM` |
| R15 | Stage-1 setup: `StoryStage := 1`, `Week + 1`, Rance's units built, `Gold := 685`, dungeon 1 placed | `Iﾊﾝﾗﾝ.ADV:1-60` | `MAIN.ADV:72`, `ｽﾀMENU.ADV:302` |
| R16 | Conquest check: count non-player provinces 1-91 (skip 29, 75); none left → `StoryStage := 4` (Creator phase, `MAIN.ADV:816-838`) | `Iｼﾚｲﾌﾞ.ADV:149-168` (entry 7) | `Gｹｲﾌﾞﾘｽ.ADV:3129` |
| R17 | Mass losses: all units lose half troops and HP (`Gﾐｷ.ADV:6014-6029`); whole party killed, mercenary-lost flags (`Gﾊﾟﾊﾟｲｱ.ADV:4768-4828`) | | |
| R18 | Button room: one-time Atk/Def +1, Atk +1, MaxHP +2, item | `G1.ADV:907-1100` | `Cﾒｲｷｭｳ` |
| R19 | Mass province transfers (§4.3) and the demon-war stage sequence | `Iﾏｼﾞﾝ`, `Iﾏｼﾞﾝ2`, `Iｾﾞｽ`, `Gﾐﾈﾊﾞ`, `Iｼﾚｲﾌﾞ` | `MAIN.ADV:936-1033` |

The Helman/Zeth sub-army state machines and the garrison regeneration/capsule kaiju growth
named in the spike live in the `T*` files and `COM.ADV:11-84`; the scenes are only their story
outputs (e.g. `Tｾﾞｽ` → `Gﾁｽﾞｺ`/`Gﾅｷﾞ`/`Gﾊﾟﾊﾟｲｱ`/`Gﾏｼﾞｯｸ`/`Iｾﾞｽ`, 48 sites) [C, call sites].

### 7.3 Real subroutines (186 calls to 63 labels)

Window/map helpers per file (`Gｲｿﾛｸ.ADV:61-79`, `Gｳｪﾝﾃﾞｨ.ADV:20`), `PCMPLAY` wrappers
(`Gﾐﾙ.ADV:2567-2571`), record helpers (`GAL.ADV:1409` heal, `:4496` clear enemy party, `:4556`
add enemy; `Gｹｲﾌﾞﾘｽ.ADV:25` remove a unit from the party), name printers (`Iｶﾐ.ADV:5681`), the date
calculation `Iｼｬﾝｸﾞﾘﾗ.ADV:5309`, and long text bodies called once (`Gﾏｹﾝ`, `Gﾜｰｸﾞ`) [C].

### 7.4 Quirks noticed

- Unreachable second half of every stub (§2.3) [C].
- `Gｻｲｾﾞﾙ.ADV:474-478`: the retry range shrinks by one per miss, so low-numbered player
  provinces are favoured; at `Tmp0621 = 0` the draw returns 0 and `Prov_Cur := 0` [C]; whether
  that can loop forever depends on record 0's owner (unknown).
- `Gﾏｼﾞｯｸ.ADV:2297-2299`: draw 32 is remapped to 31, doubling unit 31's odds [C].
- Commander slot 100 is shared by AL raiders and several duel opponents (§4.2) [C].
- `Iｼｬﾝｸﾞﾘﾗ` advances `Week` without running the week loop [C].
- 19 unnamed variables are written here (`VAR0126/0127/0129` zeroed with commander records,
  `VAR0427` Mill quest, …) — see `docs/variables.tsv` [C].

## 8. Porting proposal

### 8.1 Event format — required operations

The IR is a goto program per file (labels, conditional jumps), one entry table per file, with
these operations. "Core" names what `Kichikuou.Core` must provide.

| Op | From | Core dependency |
|---|---|---|
| `Say(ScriptRef, segments)` with inline `Name(slot)` / `Number(expr)` / line break | `'…'`, `R`, `X`, `H`, `ZH`, `ZM` | text resolved by `ScenarioText` |
| `Window(op, cg?)` | stub calls + `CallArg` | `WAO_SUB` state effects (reuse `ScriptContext.Wao.cs` logic) |
| `Cg(n, pos)`, `Fade(kind)`, `ClearText`, `Effect(raw)` | `G`, `J0/J1`, `PF*`, `PW*`, `EC`, `CC/CS/CF/CB/CL/B3/B4` | none (42 raw effect sites can stay opaque) |
| `Music(n)`, `Sound(n)`, `Wait(t)` | `SS`, `PCMPLAY`, `Y 3` | `IScriptInput.PauseAsync`-style key result into `RetVal` |
| `Map(op)` | `M.ADV` ops 0/1/3/4/10/11/12, `WAO_MAP` | `M.ADV` state effects (reuse `ScriptContext.Map.cs`) |
| `Menu(items[cond, label])` → goto | `$…$`, `]` (drop release-wait loops) | input interface |
| `PickParty(max, mode)` | `Sﾌﾞﾀｲ` | rule/UI port of `Sﾌﾞﾀｲ` |
| `Goto`, `If(expr) goto`, `Gosub/Return`, `End` (`%0`, unwinds), `JumpGameOver` (`&#OVER`) | flow | `ScenarioJumpException` exists |
| `CallScene(file)` (uses current `Ev_Entry`) | 23 scene→scene sites | re-entrant runtime |
| `CallRule(name)` | `FIGT_A/B/FIGT`, `死亡捕獲`, `FIGT表示` | `Battle.AttackProvinceAsync` etc.; `DeathCaptureAsync` |
| `Set(var, expr)` | `!v : e!` | `GameState` by **name**, `Sys3Int` evaluator (precedence as xsystem35) |
| `Random(n)` → `RetVal` | `Y 4` | the shared `Rnd` / `IScriptRandom` (same stream as rules) |
| `SetString/CopyString/AppendString/NumberToString` | `MS`, `MM`, `MA`, `MH`, `MP` | `ScriptMemory` string table |
| `LoadGlobal` | `LP` (1 site) | save system |

Generic `Set` over named variables keeps every quirk (cursors, scratch, saturation) and needs
only an expression evaluator plus `GameState` access by name; the typed operations of §4.2
(join, kill, give item, set owner, add gold, create record) should be **recognised by the
converter as annotations** on top of `Set`, not replace it — they give the hybrid branch and
tooling an index of which scenes touch the world.

### 8.2 Converter vs hand-port

| Part | Content | Size | Handling |
|---|---|---|---|
| EV-A | 34 pure-dialogue files + flag-only scenes in the rest | ≈80% of lines | converter only |
| EV-B | scenes with world mutations (§4.2) and scripted battles (§4.6) | ≈90 files | converter; `CallRule` into the ported battle; annotations |
| EV-C | location hubs and minigames (R9-R12, R18) | `Gｶﾊﾟｰﾗ`, `Gﾏｹﾝ`, `Gﾄｳｹﾞﾝ`, `Gﾌﾚｲｱ`, `G1` | converter (self-contained, no hybrid interest), verified by scene captures |
| RULE-1 | nation-phase hosts R1-R5 | `GAL.ADV:1409-1600, 2603-2985`, `Gｻｲｾﾞﾙ.ADV:471-490`, `Gﾏｼﾞｯｸ.ADV:2283-2410`, `Gｶｾｲ.ADV:465-480, 3075-3094` (≈800 lines, ≈60% text) | **hand-port with `MAIN`'s nation phases**; their dialogue stays converted fragments called back through `IStoryHooks` |
| RULE-2 | army movers R6-R8 | `Gｹｯｾﾙ.ADV:2991-3037`, `Gﾚﾘｭｰｺﾌ.ADV:30-65`, `Gｶﾌｪ.ADV:2010-2049` (≈150 lines) | hand-port with the `T*`/`COM` part that calls them |
| RULE-3 | arc mechanics R13-R16, R19 | `Iｶﾐ.ADV:4561-4900`, `Iｼｬﾝｸﾞﾘﾗ.ADV:5309-5325`, `Iﾊﾝﾗﾝ.ADV:1-60`, `Iｼﾚｲﾌﾞ.ADV:150-168` | converter, but each block gets a golden capture; `Iﾊﾝﾗﾝ` setup belongs to new-game init (`survey-data`) |

Suggested work units (each one agent):
1. **EV1 converter front end** — parser for the 40 mnemonics, stub/menu/release-wait idioms,
   entry tables, goto IR, `ScriptRef` for text; acceptance: the profile counts above reproduce
   exactly. No game text in the output.
2. **EV2 event runtime** in `Kichikuou.Core/Events` — IR interpreter over `GameState`,
   `Sys3Int` expression evaluator, re-entrancy (scene → battle → scene), page-return unwind,
   `Rnd`; implements `IStoryHooks` so the battle port uses it unchanged.
3. **EV3 presentation** — map `Say/Window/Cg/Fade/Map/Menu` onto the existing `Screen`
   emulator and Godot layer (reuse the `WAO_SUB` and `M.ADV` state ports from the battle).
4. **RULE-1/RULE-2** — specified and ported together with the `MAIN`/`T*` parts that call them.
5. **EV4 verification** — scene captures (below) for EV-B/EV-C/RULE-3 files.

Dependencies: EV1 → EV2 → EV3; RULE parts need EV2 only for their dialogue fragments.

### 8.3 Golden tests for scenes

- New capture root: a page call into any `G*`/`I*` file made while nothing is captured; inner
  files `WAO_SUB`, `M`, `WAO_MAP`, `PCMPLAY`; **hook** `FIGT_A/B/FIGT`, `死亡捕獲`, `Sﾌﾞﾀｲ`
  (memory dumps at enter/exit, so scene tests do not depend on the battle port); log draws,
  menu selections (`]` result label), `IM` clicks and key waits.
- Free test vectors already exist: every battle capture dumps memory around each story hook
  (`docs/golden-tests.md`, "hook"). Scenes without menus can be replayed from those pairs: the
  generator state is in the dump, so the runtime must reproduce the scene's own draws.
- Compare all groups except `ui` (as for battles), but **keep `scratch`**: cursors and
  `Tmp*` leak to the caller (§4.5).

## 9. Open questions

1. Does a gallery replay (`Ev_Entry` 300-304) write story state before its guard, and does
   `CG.ADV` restore anything afterwards? — `CG.ADV:2322-2349`, e.g. `Gｴﾚﾅ.ADV:25-27, 1157`.
2. xsystem35 semantics of `%0:` while local calls are pending (`Gﾄｳｹﾞﾝ.ADV:905-964`) — check
   `sl_retFar` in `F:\sources\Rance\xsystem35-sdl2\src`.
3. `Gｻｲｾﾞﾙ.ADV:474-478` at `Tmp0621 = 0`: owner of province record 0; reachable at all?
4. Who calls `Gｶﾌｪ` with the entry that reaches the Lady-unit battle (`Gｶﾌｪ.ADV:2019`)? Unknown.
5. Is the date calculation `Iｼｬﾝｸﾞﾘﾗ.ADV:5309-5325` identical to `MAIN`'s `L_01881`?
6. May converted event IR (logic, no text) be committed like `Battle.Flavour.g.cs`, or must it
   be generated from the player's files at install time?
7. `R7` (Lelyukov) and the exact meaning of `{Prov_Owner \ 0: %0:}` after each attack
   (`Gﾚﾘｭｰｺﾌ.ADV:30-50`) — confirm by reading `Tﾍﾙﾏﾝ.ADV:40-55`.
8. `FIGT表示.ADV` (3,077 lines, `Ev_Entry`-dispatched story inserts) belongs to no survey yet;
   it should go through the converter with the scenes.
