# Survey — player commands (except the dungeon) and status screens

Scope: `Cﾃｷﾄｼ`, `Cﾃｷﾄｼ2`, `Cｼﾊｲﾄｼ`, `Cｼｮｳﾆﾝ`, `Cﾌﾞｶ`, `Cﾆﾝｼﾞｬ`, `Cﾃｲﾄ`, `Cﾊｰﾚﾑ`, `Cﾀｶﾗ`,
`Sｱｲﾃﾑ`, `Sﾊｰﾚﾑ`, `Sﾌﾞｶ`, `Sﾌﾞﾀｲ`, `各種DATA`; callers of `死亡捕獲`. Every file was read in full
(2026-10-08). Line refs are to `_extract\scenario_named\`. Tags: **[C]** confirmed by reading,
**[I]** inference, **[G]** guess. Gold is in units of 100K (the screen appends "00K GOLD").

## 1. Role

These files are what the player does in the **player phase** of a week. `COMMAND.ADV` (the
menu loop, surveyed with the main loop) resets one availability flag per command at the start of
the week (`COMMAND.ADV:38-46`) and dispatches either a menu item or a province click
(`COMMAND.ADV:514-576`). Each command file runs its own menus and dialogue, consumes its flag,
and returns (`%0`) to the loop (`@L_00129`). **No command ends the phase**; only "Turn END"
(`COMMAND.ADV:377-425`, `%0` to `MAIN`) does, plus the prologue exit when `StoryStage` becomes
99 after `Cﾃｷﾄｼ` (`COMMAND.ADV:518-521`) [C]. The `S*` files are the original's list screens
(pickers); two of them also carry rules (`Sﾌﾞﾀｲ` pays war costs, `Sｱｲﾃﾑ` equips items).
By volume the group is ~15.6k lines, but **rules are well under 2k lines**: ~85 % is dialogue
(e.g. 466 key waits in `Cﾃｷﾄｼ` alone) and story dispatch.

## 2. Inventory

### 2.1 Files and entry points

| File | Lines | Reached from | Weekly flag consumed |
|---|---|---|---|
| `Cﾃｷﾄｼ` enemy city (adjacent) | 4497 | province click: enemy province adjacent to an own one (`COMMAND.ADV:535-571`); prologue click on 33/34 (`:518`) | `Ui_CmdAttackAvail` (only on a conquest); `Ui_CmdOwnCityAvail` (AL Church visit, `Cﾃｷﾄｼ.ADV:95`) |
| `Cﾃｷﾄｼ2` enemy city (not adjacent) | 190 | other enemy province (`COMMAND.ADV:576`); any other prologue click (`:524`) | — |
| `Cｼﾊｲﾄｼ` own city | 1823 | click on own province, `StoryStage ≠ 0` (`COMMAND.ADV:527-530`) | `Ui_CmdOwnCityAvail` (global, one action per week) |
| `Cｼｮｳﾆﾝ` merchant | 1794 | menu "Merchant" (`COMMAND.ADV:255-266, 349-353`) | `Ui_CmdMerchantAvail` (only on "Send Away") |
| `Cﾌﾞｶ` subordinates | 1467 | menu "Subordinates" (`COMMAND.ADV:263-282, 354-360`) | `CmdAvail_SubTroops`, `CmdAvail_SubTalk` → `Ui_CmdSubordAvail` when both used |
| `Cﾆﾝｼﾞｬ` ninja | 1246 | "Ninja Maneuver" in `Cﾃｷﾄｼ.ADV:427` / `Cﾃｷﾄｼ2.ADV:66` | per-ninja `F_*_NinjaUsed` (5 ninjas, shared with FIGT_B scouting) |
| `Cﾃｲﾄ` construction | 1055 | menu "Construction" (`COMMAND.ADV:227-237, 330-336`) | `Ui_CmdBuildAvail` |
| `Cﾊｰﾚﾑ` harem | 695 | menu "Harem" (`COMMAND.ADV:185-195, 239-249, 337-343`) | `Ui_CmdHaremAvail` (refunded on cancel) |
| `Cﾀｶﾗ` treasure | 15 | menu "Treasure", always available (`COMMAND.ADV:250-253, 344-348`) | — |
| `Cｹﾝｷｭｳ` | 1 (empty) | unreachable call site `COMMAND.ADV:361-364` (no label) | — |
| `Sｱｲﾃﾑ` item screen | 725 | `Cﾀｶﾗ.ADV:5` only | — |
| `Sﾊｰﾚﾑ` harem picker | 363 | `Cﾊｰﾚﾑ.ADV:214` only | — |
| `Sﾌﾞｶ` subordinate picker | 242 | `Cﾌﾞｶ` ×9 (CallArg mode 1-5) | — |
| `Sﾌﾞﾀｲ` deployment picker | 659 | 10 sites (§2.3) | — (callers consume) |
| `各種DATA` statistics | 396 | menu "All Data" (`COMMAND.ADV:365-369`) | — |
| `死亡捕獲` | 462 | FIGT_A:189, FIGT_B:274, Cﾒｲｷｭｳ:1288, Gｹｯｾﾙ:2436/2963, Gﾅｷﾞ:4172 — **no command file calls it directly** | ported (`Battle.DeathCaptureAsync`) |

`Ev_CommandMode` 1/2 (forced Cafe trip / Maria tulip attack) hijacks `Cﾃｷﾄｼ`, `Cﾃｷﾄｼ2` and
`Cｼﾊｲﾄｼ` at entry: Gｶﾌｪ#31 or Gﾏﾘｱ#5, then return without a menu (`Cﾃｷﾄｼ.ADV:7-15`,
`Cﾃｷﾄｼ2.ADV:6-13`, `Cｼﾊｲﾄｼ.ADV:4-13`) [C].

### 2.2 Main procedures (rule-bearing parts only)

| File | Lines | Procedure |
|---|---|---|
| Cﾃｷﾄｼ | 1-98 | entry (`Bt_TargetProv := Prov_Cur`, `Ui_CityScouted := 0`), header, main menu (Attack/Pillage, Ninja, AL Church, Return) |
| Cﾃｷﾄｼ | 99-425 | attack pipeline → `Sﾌﾞﾀｲ` → refusals → defender setup (T*2 files) → `FIGT_A` (01-flow §2.1) |
| Cﾃｷﾄｼ | 429-633, 634-3648 | independent cities (owner 3, provinces 17-29): Negotiate / Use Force / Stop + per-city negotiation scenes |
| Cﾃｷﾄｼ | 3649-3828 | Use Force (independent city battle) |
| Cﾃｷﾄｼ | 3829-4364 | refusal / barrier dialogues, "first four alive" picker `L_04f2a`, `L_05109` (remove cmdr), `L_05118` (undeploy) |
| Cｼﾊｲﾄｼ | 32-507, 508-801 | menu builder (~30 province-specific story items), story stubs, castle cell submenu |
| Cｼﾊｲﾄｼ | 810-1181 | Temporary Levy (all 8 draws) |
| Cｼﾊｲﾄｼ | 1504-1731 | Bolster Defense |
| Cｼｮｳﾆﾝ | 1-129 | greeting state machine (`F_Merchant_State`), main menu |
| Cｼｮｳﾆﾝ | 205-925, 1059-1400 | mercenaries (units 49/50), item shop |
| Cﾌﾞｶ | 1-207 | view-only loop / pick loop over `Sﾌﾞｶ`; 208-712 50 per-unit talk stubs |
| Cﾌﾞｶ | 725-1097 | new-troop offer, Add More, buy, replenish, talk |
| Cﾆﾝｼﾞｬ | 1-330 | ninja pick, mission menu; 331-643 Collect Info / Sabotage / capture; 644-1166 per-ninja effects, commander picker `L_01691` |
| Cﾃｲﾄ | 1-286, 290-976 | busy check, building list, 6 build branches (+1 dead) |
| Cﾊｰﾚﾑ | 1-212, 213-577 | intro events, availability list, `Sﾊｰﾚﾑ`, 50-way dispatch, EXP reward |
| Sﾌﾞﾀｲ | 106-268, 269-379 | click dispatch, Go (validate + pay), toggle; cost recomputation |
| Sｱｲﾃﾑ | 47-169, 416-427 | view toggle, equip `L_0043a`, unequip `L_00b39` |

### 2.3 `Sﾌﾞﾀｲ` call sites (input `Tmp0021` = max units, `Tmp0022` = mode) [C]

| Site | Max | Mode | Note |
|---|---|---|---|
| Cﾃｷﾄｼ:218 | 4, or 1 at the mountain passes (`:172-199`) | 1 Attack | `PParty1 = 0` → back to menu |
| Cﾃｷﾄｼ:464 / :3662 | 4 | 1 | province 27 (→ Gｻｰﾅｷｱ#1) / Use Force |
| FIGT_B:80 | 4 | 2 Plains/City | port: `Battle.Flow.Defence.cs:146-149` via `IStoryHooks` (hook) |
| Cﾒｲｷｭｳ:335, Gﾄｳｹﾞﾝ:278 | 1 | 3 Search | |
| Gｶﾌｪ:1959, Iｶﾐ:4816, Iｼｬﾝｸﾞﾘﾗ:4526, Iﾄｳｿﾞｸ:305 | 4 | 1 | story battles |

## 3. Commands — costs, effects, limits

### 3.1 Attack (`Cﾃｷﾄｼ` → `FIGT_A`) — already ported downstream

01-flow §2.1 is correct with one correction: **province 31 also opens to a full party if the
player owns 30** (`Cﾃｷﾄｼ.ADV:188-192`) [C]. Order: prologue (`StoryStage 0` → Iﾄｳｿﾞｸ#1,
ｵｰﾌﾟﾆﾝｸﾞ, `:101-108`) → stage-4 refusal (`:109`) → `Ui_CmdAttackAvail = 0` refusal (`:112`) →
province 82 warning (`:115`) → stage-1 "civil war" refusal unless owner 2 (`:118`) → owner 3 →
independent flow (`:121`) → Patton hooks (`:124-134`) → rebels refusal (`:144`) → Iｼﾚｲﾌﾞ#5/#6
gate for 78/89/90/91 (`:147-157`) → **`Sﾌﾞﾀｲ` (cost paid here)** → unit refusals (`:226-307`, no
refund, Q-1) → defender setup by owner: TCﾘｰｻﾞｽ2 `:319`, TJAPAN2 `:342`, Tｾﾞｽ2 `:345`,
Tﾍﾙﾏﾝ2 `:348`, GAL#9 `:351`, Tｹｲﾌﾞﾘｽ2 `:355` → no-battle exits (Warg `:377`, Zeth barrier
`:381-387`, Papaya `:388`, unopposed `:395`) → `Bt_Kind := 1; FIGT_A; Bt_Kind := 0` (`:419-425`) [C].

**Cost:** deploy cost `Σ (Unit_DeployCost + Unit_Troops/300)` and `Unit_Status := 2` per unit
(paid in `Sﾌﾞﾀｲ`); `Ui_CmdAttackAvail := 0` only on a conquest (FIGT_A win, unopposed take,
peaceful merge, Papaya) — a lost battle, refusal, barrier or Warg exit leaves it set, so the
player may attack again the same week with other units (Q-2) [C]. Win/loss handling, ownership
change and `死亡捕獲` live in FIGT_A; `Cﾃｷﾄｼ` adds only the map redraw (`:424`) [C].

### 3.2 Independent cities (owner 3, `Cﾃｷﾄｼ.ADV:429-3828`)

Pre-menu specials: 28 + `F_Merchant_State = 1` → Portugal merchant scene, `Gold += 100`, state 2
(`:2432-2694`); 29 → Iｼｬﾝｸﾞﾘﾗ#1/#2 (`:434-442`); 21 → Iｼﾚｲﾌﾞ#3 (`:456`); 27 → `Sﾌﾞﾀｲ` then
Gｻｰﾅｷｱ#1 (`:461-471`). Sub-menu Negotiate / Use Force / Stop (`:486-506`). Negotiation is free
and repeatable; success = `Prov_Owner := 1`, `Ui_CmdAttackAvail := 0`, the city's commanders
removed (`L_05109`) [C]:

| Prov | Outcome of negotiation |
|---|---|
| 17 Rockearth | "approve" → `F_DX_State := 1`, `Gold += 500`, success (`:1324, 1400-1418`); "die" → `F_DX_State := 100`, never again (`:1268`) |
| 18 Ice | always succeeds (`:1515-1520`) |
| 19 Diphtheria, 24 Zeo | never succeed (`:956`, `:2795-2810`) |
| 20 Lazeal | first: "wait" → `DXTalk_RetryWeek := Week + 4` (`:1696`); due when `RetryWeek < Week` → Gﾚｨﾘｨ#1, success (`:1963-1981`) |
| 22 Mars | "Make a General" → unit 26 `Status := 1`, `N_Mars_Score := 2`, success (`:897-903`) |
| 23 Red | "I promise" → success, nothing recorded (`:2125-2130`) |
| 25 M Land | "Maintain the Park" → `F_MLand_Funded := 1` (−20 gold weekly, MAIN.ADV:190), `F_Sayori_State := 1`, success (`:3078-3291`) |
| 26 Hanna | "Pay": refused if `Gold < 29`, else `Gold −= 30` (`:3515-3588`) |
| 28 Portugal | "Pay" −90, but "Don't Pay" reaches the same success for free (`:2328-2413`) |

**Use Force** (`:3649-3828`): `Sﾌﾞﾀｲ` (4), `Bt_EnemyRetreatMode := 1`, `MS 10` army name, fixed
commander list per city → `L_04f2a` → FIGT_A; province 20 clears `DXTalk_RetryWeek` (`:3711`);
after a win at 28 → Gﾚﾍﾞｯｶ#2, at 22 → Gｶｾｲ#1 (`:3817-3824`). **Skips the unit refusals** [C].

### 3.3 Own city (`Cｼﾊｲﾄｼ`) — one action per week across all own provinces [C]

| Action | Cost | Effect | Lines |
|---|---|---|---|
| Temporary Levy (hidden at Economy 0) | `Ui_CmdOwnCityAvail := 0` | `Gold += Y4(Prov_Economy*10)`; then the **first** matching story trigger (~30, keyed by province + flags) else item rolls (Cat Bread 24, Kuriripu 25), Gｶﾊﾟｰﾗ#1 roll, else a girl: `Prov_Girls −1`, `N_SpecialRoom_Girls +1`. No economy/defence penalty, no `Hist_*` | 810-1181 (triggers 897-1054) |
| Bolster Defense | `Prov_Defense*20` (5 if 0); `Ui_CmdOwnCityAvail −= 1` | `Gold −= cost` **without a gold check** (floors at 0), `Prov_Defense += 1`, no cap | 1504-1699 (1593-1609) |
| ~30 story items (province + flag gated) | `Ui_CmdOwnCityAvail := 0` | `Ev_Entry := n; %#G*` — table at 87-292 / stubs 508-801 (e.g. buildings on prov 1 → Gﾏﾘｱ/Gｱﾋﾞｧﾄｰﾙ/Gｱｰﾔ/Gﾏｹﾝ/Gﾕｳﾚｲ…/Gｽｽﾞﾒ; Special Room → Gﾄｸﾍﾞﾂ) | 87-801 |
| Castle cells (prov 1) | same flag | G1#1, Gﾚﾘｭｰｺﾌ#4, Gﾙｰﾍﾞﾗﾝ#10, Gﾊﾟﾊﾟｲｱ#6, Gｶﾌｪ#2 | 728-801 |
| Ruins Search (prov 35) | gated only by `Ui_CmdDungeonAvail` (consumed in Cﾒｲｷｭｳ:25) | sets dungeon record 30 (`Dgn_State := 0`, `Dgn_Prov := 35`), calls `Cﾒｲｷｭｳ` | 434-439, 538-548 |

When the flag is 0 every item shows "×" and loops back to the menu (`:294-499`) [C].

### 3.4 Merchant (`Cｼｮｳﾆﾝ`) — any number of purchases per visit [C]

Flag `Ui_CmdMerchantAvail := 0` only on Send Away (`:130-153`). Price test is `Gold < price−1`
(off by one; Gold floors at 0) [C].

| Item | Price | Effect | Lines |
|---|---|---|---|
| Louis (unit 49) 200 / 1000 troops | 40 / 80 | `Status := 1`, `Troops = MaxTroops = n`; shown while `F_MercA_Lost = 0`, `Status = 0` | 279-302 |
| Cecil (unit 50) 1000 / 5000 | 30 / 110 | same, `F_MercB_Lost` | 616-639 |
| Shop: Popularity Staff 3 (500), Napoleon 4 (800), Shingen 5 (800), Substitute Ninja 8 (1000), Hirami Lemon 43 (500, unit 32 present), Clothes 19 (1, Sill present; sets `F_SillClothes_State`), Ninja Birdlime 44 (30, `N_Freya_State = 3`), Fog Card 28 (`FogCard_Price`) | | `Item_Holder := 100`; each sold once (shown while `Item_Holder = 0`) | 1059-1400 |
| Fuck Rebecca (once per visit) / Buy Rebecca | free | Gﾚﾍﾞｯｶ#1 / dialogue only | 926-1058 |

All purchases add to `Hist_SundryExp`. `F_SillCow_State` 6/2 → Gｼｨﾙ#6/#2 and return **without
consuming the flag** (`:9-18`) [C].

### 3.5 Subordinates (`Cﾌﾞｶ`) [C]

One troop action and one talk per week. Using either clears its `CmdAvail_Sub*`; when both are
0, `Ui_CmdSubordAvail := 0` (menu "△", view-only loop `:1-20`). Only `Unit_Status = 1` units act.

| Action | Cost | Effect | Lines |
|---|---|---|---|
| Add New Troops | offer table `:757-799`: Knights 110/25 men, Soldiers 50/25, Mages 100/25, units 3-4 30/50, units 5/33/36/18 200, unit 16 120, class 3 150, other 320/10 men; units >47, 25, 41 none; `+ Week/4`. "Add More": men ×2, cost ×3 while cost < 10000 | `Gold −= cost`, `Hist_MilitaryExp += cost`, `Troops += men`, `MaxTroops += men`, `Recovery += 1` | 922-965 |
| Replenish | `max(1, (Max−Troops)/50)`; nothing if full | `Troops := MaxTroops` | 974-1097 |
| Talk | `CmdAvail_SubTalk := 0` before the scene | unit N → its G file with **stale `Ev_Entry = 20`** (set once at `:25`); 49/50 → Gﾖｳﾍｲ#3/#4; 48 → Gﾊﾆｰ/Gｵｳｼﾞ by `Unit48_Identity`; Rance → inline self-talk, `VAR0469 += 1` | 966-973, 208-712, 1338-1377 |
| Prologue (`StoryStage 0`) | talk only, no menu | `Ui_CmdSubordAvail := 0` | 713-724 |

Every action leaves `Cﾌﾞｶ` (`%0`); Info / Do Nothing don't consume anything.

### 3.6 Ninja (`Cﾆﾝｼﾞｬ`) [C]

No gold, no command flag. Picking ninja k sets `F_k_NinjaUsed := 1` **before** the mission menu
(spent on "No Mission"); COM.ADV:11-32 resets the five flags weekly (99 = not recruited);
FIGT_B:395-491 uses the same pool. Missions: Collect Info (1 % capture; shows Economy/Defense/
Battle Area, `Ui_CityScouted := 1`), Sabotage (allowed unless owner 6/4/7 and that war not
started, `:261-270`; capture 10 %, 70 % vs owner 8; success 1/2), Burger (Kanami only, random).

| Ninja | Sabotage success | Capture effect |
|---|---|---|
| Kanami | `Prov_Defense −1` (`:698`) | Gｶﾅﾐ#100 |
| Freya | random commander loses `min(Y4(200)…)` troops (`:749-761`) | `N_Freya_State := 10` (+Gﾌﾚｲｱ#100 vs Helman) |
| Kaoru | commander `TroopAtk −1` or `TroopDef −1` (`:854-867`) | `N_Kaoru_State := 0` |
| Wichita | commander `Recovery /= 2` (`:971-978`) | `N_Wichita_State := 0` |
| Saitou | always fails (`:1091-1148`) | `F_Saitou_State := 0` |

Substitute Ninja (item 8, `Item_Holder = 100`) converts a capture into loss of the item
(`:360-364, 529-540, 582-600`). Kanami's Ev 12 interrupt (20 %) refunds her (`:95-102`).

### 3.7 Construction (`Cﾃｲﾄ`) [C]

One building at a time (`BuildWeeksLeft ≠ 0` → "busy", no flag consumed, `:72-220`). Build:
`Gold < cost` → back to list; else `Gold −= cost`, `Hist_SundryExp += cost`,
`BuildWeeksLeft := w`, `Bld_X := 2`, `Ui_CmdBuildAvail := 0`. Completion at week start
(`COMMAND.ADV:18-36` decrement, `:578-614` sets `Bld_X := 1` + scene).

| Building | Gold | Weeks | Completion scene | Lines |
|---|---|---|---|---|
| Girls' Military Academy | 600 | 16 | Gﾏﾘｽ#4 | 364-395 |
| Library | 400 | 8 | Gﾏﾘｽ#5 | 451-476 |
| Hospital | 400 | 12 | Gｱｰﾔ#2 | 553-587 |
| S&M Tower | 800 | 12 | Gﾏﾘｽ#6 | 734-761 |
| Maria Research (unit 16 present, `F_Maria_Tulip > 1`) | 980 | 8 | Gﾏﾘｽ#7 | 839-871 |
| Magic Research | 900 | 12 | Gﾏﾘｽ#8 | 933-962 |

Completed buildings unlock own-city items on province 1 (§3.3).

### 3.8 Harem (`Cﾊｰﾚﾑ` + `Sﾊｰﾚﾑ`) [C]

`Ui_CmdHaremAvail −= 1` (`:36`), refunded on cancel (`:547-552`). Intro draw `Y 4,4` → Suzume /
Wendy first meetings; Cafe trigger Gｶﾌｪ#3 consumes the visit (`:6-27`). Availability of 50 girls
built at `:39-212` (unit present and/or story flags; stage 0 only Sill/Soul/Anasel). Pick →
`Harem_LastPick`, `Harem_LastWeek[k] := Week` (`:220-223`), `Ev_Entry := 10` → G-file. If the
scene set `Harem_ExpReward`: `Harem_SexCount[k] += 1`, `Unit_Exp[1] += reward` (Rance; no
level-up here) (`:553-577`). `N_Lia_Neglect`: +1 per visit (cap 10) while `F_Lia_State = 1`,
−1 on cancel, reset when Lia is picked (`:29-34, 396-408, 548-550`).

### 3.9 Treasure / items (`Cﾀｶﾗ` + `Sｱｲﾃﾑ`) [C]

`Cﾀｶﾗ.ADV:1-15`: items held by a unit with `Status = 0` are lost (`Item_Holder := 0`), then
`Sｱｲﾃﾑ`. Equip / unequip / swap are free and unlimited; items 1 and 2 are locked (and lock their
holder); `Item_Effect = 0` cannot be equipped (`Sｱｲﾃﾑ.ADV:139-160, 250-371, 416-427`).
`Item_Holder`: 0 not owned, 100 in stock, 1..50 held by unit.

### 3.10 Display-only commands

`各種DATA` (graphs of `Hist_Funds/SundryExp/MilitaryExp/Casualties` over 16 weeks, minimap
marks; persistent scale preferences `Ui_MoneyGraphDiv/Scale`, `Ui_DamageGraphDiv/Scale`) and
Load/Save. `Sﾌﾞｶ` and `Sﾊｰﾚﾑ` are pickers with no rule writes (except stale cursors, §4).

## 4. Call graph

| From | Rule/UI targets (count) | Story targets |
|---|---|---|
| Cﾃｷﾄｼ | M 41, WAO_SUB 23, Sﾌﾞﾀｲ 3, FIGT_A 2, Cﾆﾝｼﾞｬ 1, T*2 ×5 (one each), ｵｰﾌﾟﾆﾝｸﾞ 1 | Iｼﾚｲﾌﾞ 3, GAL 2, Gﾊﾟｯﾄﾝ 2, Iｼｬﾝｸﾞﾘﾗ 2, 1 each: Gｶﾌｪ, Gﾏﾘｱ, Iﾄｳｿﾞｸ, Gﾊﾟﾊﾟｲｱ, Gｻｰﾅｷｱ, Gﾚｨﾘｨ, Gﾚﾍﾞｯｶ, Gｶｾｲ |
| Cﾃｷﾄｼ2 | WAO_SUB 23, Cﾆﾝｼﾞｬ 1 | Gｶﾌｪ, Gﾏﾘｱ |
| Cｼﾊｲﾄｼ | WAO_SUB 23, M 3, PCMPLAY 3, Cﾒｲｷｭｳ 1 | ~70 calls into ~50 G/I files (G1 5, G2 5, Gﾕｳﾚｲ 5, Gﾏﾘｱ 3, …) |
| Cｼｮｳﾆﾝ | WAO_SUB 23, M 1 | Gｼｨﾙ 2, Gﾚﾍﾞｯｶ 1 |
| Cﾌﾞｶ | WAO_SUB 24, Sﾌﾞｶ 9, M 2 | 51 talk calls into 50 G files (map unit→file: `Cﾌﾞｶ.ADV:208-712`) |
| Cﾆﾝｼﾞｬ | WAO_SUB 23, M 2 | Gｺｰﾄﾞ 4, Gｶﾅﾐ 3, Gﾌﾚｲｱ 1 |
| Cﾃｲﾄ | WAO_SUB 23 (stubs), M 2 | — (completion scenes are in COMMAND) |
| Cﾊｰﾚﾑ | Sﾊｰﾚﾑ 1, M 1, WAO_SUB 23 (17 dead) | 56 calls into 49 G files, `Ev_Entry 10` (Gｼｨﾙ 4, Gｳｪﾝﾃﾞｨ 3, …) |
| Sｱｲﾃﾑ / Sﾌﾞﾀｲ / Sﾌﾞｶ / Sﾊｰﾚﾑ | WAO_SUB, PCMPLAY, M (Sｱｲﾃﾑ) | — |

No `&#` jumps anywhere in the group [C]. Talk scenes all dispatch on `Ev_Entry = 20` except
Gﾖｳﾍｲ (1-4) [C]; GDX, Gｱﾘｽ, IMﾗﾝﾄﾞ are called from `Cｼﾊｲﾄｼ` without `Ev_Entry` and don't
dispatch on it [C].

## 5. State

| Group | Written by this group |
|---|---|
| economy | `Gold` (all commands; `+` Levy, Rockearth +500, Portugal +100), `BuildWeeksLeft`, `Bld_*`, `F_MLand_Funded` |
| history (page 2, cursor `Hist_Cur`) | `Hist_SundryExp` (merchant, construction, Hanna/Portugal), `Hist_MilitaryExp` (Cﾌﾞｶ, Sﾌﾞﾀｲ), `Harem_SexCount[k]`, `Harem_LastWeek[k]` |
| prov | `Prov_Owner` (negotiations, unopposed take), `Prov_Defense` (±1), `Prov_Girls` |
| unit | `Unit_Status` (Sﾌﾞﾀｲ 1→2, undeploy, Mars, mercenaries), `Troops`, `MaxTroops`, `Recovery`, `Unit_Item`, `Unit_Exp[1]` |
| cmdr | `Cmdr_Status/Troops` (L_05109, ninja), `TroopAtk/Def`, `Recovery`; cmdr 73 refilled and **renamed** (`Cﾃｷﾄｼ.ADV:329-338`) |
| item | `Item_Holder` (shop, finds, equip, Cﾀｶﾗ loss, Substitute Ninja) |
| ui | all `Ui_Cmd*Avail`, `CmdAvail_Sub*`, `Ui_CityScouted`, `HaremAvail_*` (50), `Ui_ItemViewMode` (persistent), graph scales, `Ui_SquadMode/Choice`, `Ui_SubordMode` |
| battle | `Bt_TargetProv`, `Bt_PlayerParty1-4`, `Bt_EnemyParty*`, `Bt_EnemyRetreatMode`, `Bt_WallBonus`, `Bt_Kind`, `Bt_ScoutNinja`, `F_*_NinjaUsed` |
| story / time | ~15 flags directly (`F_DX_State`, `F_Merchant_State`, `N_Mars_Score`, `N_Lia_Neglect`, `F_Suzume_Met`, `F_Wendy_Met`, `N_Freya/Kaoru/Wichita_State`, `F_Saitou_State`, `F_Mokomoko_State`, `F_Makoto_State`, `N_SpecialRoom_Girls`, `DXTalk_RetryWeek`, `VAR0469`…); reads ~120 more as menu gates |
| array page 3 | `Buf_Val` (Sﾊｰﾚﾑ copy, `NB` save/restore of `Tmp0021-0040` in Cﾃｷﾄｼ:201/217) |
| unused | `VAR0055` (write-only tally of levy/defence gold, `Cｼﾊｲﾄｼ.ADV:894, 1607`) |

Strings: `MM 8` province name, `MM 1/2/3/4/7` names for display, `MS 1` button labels,
`MS 10` enemy army name; **`MS 4/5` permanently rename commander 73** (JAPAN "Fleeing
Soldier"/"Foot Soldiers", `Cﾃｷﾄｼ.ADV:334-338`) — a persistent string-table write [C]. No array
page 4 use [I].

**Stale cursors / aliasing** [C]: `Unit_Cur` left at 16 (Cﾃｲﾄ:268, Cｼﾊｲﾄｼ:88), 32, 503 (picker
cancel), last party member (Sﾌﾞﾀｲ Go), 1 (harem reward); `Item_Cur` at 8 or 28; `Cmdr_Cur` at a
random commander or 100; `Prov_Cur := 1` left after the first Zeth-barrier failure
(`Cﾃｷﾄｼ.ADV:4172`); `Bt_TargetProv` left set on every exit but Return (`:88`) and FIGT_A;
`Hist_Cur` left at the harem girl index (`Cﾊｰﾚﾑ.ADV:560`) so later expenses that week land in
history slot k (graphs only); **`Sｱｲﾃﾑ` equip writes `Unit_Item[100]`** (`:148-151`), which
aliases `Unit_DgnAtkMod[48]` (page 1, stride 52) [I effect]. Whether any later code reads these
cursors without setting them is **unknown** — the golden comparison will tell.

## 6. Randomness — 28 `Y 4` sites, 21 live [C]

| Site | n | Condition | Decides |
|---|---|---|---|
| Cｼﾊｲﾄｼ:854 | 3 | every levy | flavour text |
| Cｼﾊｲﾄｼ:885 | `Prov_Economy*10` | every levy | gold gained |
| Cｼﾊｲﾄｼ:1039 / 1044 / 1049 | 5 / 5 / 5 | no story trigger fired; each only if the previous did not hit | Cat Bread / Kuriripu / Gｶﾊﾟｰﾗ#1 (`Prov < 17`) — **drawn even if the item is owned** |
| Cｼﾊｲﾄｼ:1063 / 1086 / 1146 | 5 / 10 / 3 | `Prov_Girls ≠ 0` | girl adjective, occupation, extra quip (display, but consume draws) |
| Cﾆﾝｼﾞｬ:95 | 5 | Kanami picked | Ev 12 interrupt (if `F_Kanami_Scene4 = 0`) |
| Cﾆﾝｼﾞｬ:281 | 5 | **every mission menu, any ninja** | Burger item shown (Kanami only) |
| Cﾆﾝｼﾞｬ:359 | 100 | Collect Info | 1 → capture |
| Cﾆﾝｼﾞｬ:529 / 531 | 100 / 300 | Sabotage / owner 8 redraw | > 90 → capture |
| Cﾆﾝｼﾞｬ:541 | 2 | sabotage not captured (incl. Saitou) | success / fail |
| Cﾆﾝｼﾞｬ:1154 / 1158 / 1162 | 30 / 12 / 28 | success, Freya/Kaoru/Wichita, owner 4 / 6 / 7 | target commander 22-51 / 71-82 / 101-128 |
| Cﾆﾝｼﾞｬ:755 / 757 | 200 / `Cmdr_Troops` | Freya success; redraw if troops < first draw | troops killed |
| Cﾆﾝｼﾞｬ:860 | 2 | Kaoru success | TroopAtk vs TroopDef |
| Cﾊｰﾚﾑ:7 | 4 | every harem command, `StoryStage ≠ 0`, `F_SillCow_State ≠ 5`; drawn even if cancelled | Suzume / Wendy intro |
| Cﾊｰﾚﾑ:578-615 (7) | 6, 4×6 | **dead code** (no label) | — |

Sabotage draw order: 281 → 529 (→531) → 541 → (Gｺｰﾄﾞ) → picker → 755/757 or 860 [C].
`Cﾃｷﾄｼ`, `Cﾃｷﾄｼ2`, `Cｼｮｳﾆﾝ`, `Cﾌﾞｶ`, `Cﾃｲﾄ` and all `S*` / `各種DATA` draw nothing; draws
reached through them are inside T*2 defender setups, FIGT and story scenes.

## 7. Input and display

- **Menus** (`$…$` + `]`, preceded by an `IM` wait-for-release loop; `IZ` warps the cursor back
  on cancel): Cﾃｷﾄｼ 9 (main, independent sub-menu, 7 negotiation choices), Cﾃｷﾄｼ2 1,
  Cｼﾊｲﾄｼ 3, Cｼｮｳﾆﾝ 5, Cﾌﾞｶ 3, Cﾆﾝｼﾞｬ 2, Cﾃｲﾄ 2 + 6 confirms, Sｱｲﾃﾑ 1 (Yes/No), 各種DATA 2.
  A right-click on `]` falls through to the next statement [I — confirm in xsystem35]; each
  site's fall-through target is a rule (some loop, some exit).
- **Grid pickers** (Sﾊｰﾚﾑ, Sﾌﾞｶ, Sﾌﾞﾀｲ, Sｱｲﾃﾑ share one widget): `IM` polling, 3×17 grid
  ids 1..51, a bottom bar with per-file codes (501/502/503/997/998/999), right click 999
  (998 in Sｱｲﾃﾑ) [C]. The code semantics are rules (e.g. Sﾌﾞﾀｲ left-half click = remove last);
  the geometry is presentation.
- **Key waits:** every dialogue line (`WAO_SUB` op 1); 466 in Cﾃｷﾄｼ.
- **Display only, free to redesign:** headers (name, Economy/Defense/Battle Area), all dialogue
  and portraits, cost/budget readouts, Sﾌﾞｶ modes 2-5 (screen save/restore), hover panels,
  Cﾌﾞｶ info pages (`:1113-1337`, shows "Exp/(100+Level*30)"), the whole of 各種DATA, Sﾊｰﾚﾑ
  date text (which **writes** `DateYear/Month/Week`, recomputed by COMMAND before every menu —
  harmless [C]).

## 8. Rules vs presentation; quirks

Separation is moderate: rules are short blocks between long dialogue runs; menu gates are rules
(the set of offered items depends on ~150 flags); the pickers mix click geometry with rules.
Quirks a faithful port must keep (or consciously decide on) [C unless tagged]:

1. **No refund** of war costs on refusals, Zeth barrier, Warg exit (`Cﾃｷﾄｼ.ADV:226-307, 377-387`); units stay `Status 2`.
2. **Attack flag survives a defeat** (Q-2): repeated attacks per week are possible with other units.
3. **Use Force skips unit refusals** (Satella, Meglass, Gandhi, unit 44 fight independent cities); province 56 + `F_Japan_State = 1` skips Satella/Meglass (`:263`); the JAPAN "Fleeing Soldier" path skips the Papaya/Warg/unopposed checks (`:339`).
4. **Portugal**: paying 90 = not paying. **Hanna**: tests `< 29`, subtracts 30 (Gold 29 → 0, saturating).
5. **Bolster Defense has no gold check and no cap**; the cost floors Gold at 0.
6. **Merchant off-by-one** `Gold < price−1`: Clothes free at 0 gold.
7. **Levy has no downside** and its three item/scene rolls are always drawn.
8. **Merchant flag not consumed** by the Gｼｨﾙ#2/#6 branches; **Ruins Search** ignores the own-city flag and resets dungeon record 30.
9. **Ninja is spent on "No Mission"**; the Burger draw is always made; Kanami's interrupt refunds her and can recur [I]; commander record 100 is zeroed by every sabotage-success picker call (`Cﾆﾝｼﾞｬ.ADV:1150-1152`); Freya/Kaoru/Wichita always fail against owners other than 4/6/7.
10. **Cﾌﾞｶ**: talk relies on stale `Ev_Entry = 20`; Add-More multipliers survive a cancel (`:918`); Replenish sets `Troops := Max` even if above (unreachable: MAIN's weekly tick caps troops, `MAIN.ADV:147-164` [I]).
11. **Cﾃｲﾄ**: "all built" check (`:1`) can never be true; pool branch (`:588-657`) dead; `Unit_Cur := 16` clobber (`:268`).
12. **Harem**: cancel lowers `N_Lia_Neglect` when it was at the cap; stale `Hist_Cur`; Gｻｲﾄｳ/Gﾏｼﾞｯｸ/Gﾜｰｸﾞ never set `Harem_ExpReward`.
13. **Sﾌﾞﾀｲ**: clicking a selected unit moves it to the end instead of deselecting; returns only 501/502/503 (**answers battle open question 8**: FIGT_B's plains fall-through is unreachable).
14. **Sｱｲﾃﾑ**: `Unit_Item[100]` alias write on every equip; bottom-bar id 997 unfiltered → out-of-range writes [I effect]; item-first view reads `Unit_Item[0]`.
15. **Cﾀｶﾗ**: items of dead units are destroyed, not returned to stock.
16. Dead code: `Cｹﾝｷｭｳ` call, `Cｼﾊｲﾄｼ.ADV:1182-1235`, Cﾊｰﾚﾑ random stat routine (`:578-633`), Cﾃｷﾄｼ Shangri-La list (`:3800-3812`), WAO stub tails after `\0` in every file.
17. Dictionary fixes: `DXTalk_RetryWeek` is Lazeal's surrender wait; `VAR0469` = Rance self-talk count (not debug); `F_Warg_State` reset at `Cﾃｷﾄｼ.ADV:357` contradicts "1 = joined"; `F_Pool_Built` never set; `Hist_Cur` stale after harem.

## 9. Porting proposal

Rules are small; the bulk is dialogue that should go through the event converter (`ScriptRef`
ranges), not hand-porting. Proposed parts (script lines include dialogue; C# is small):

| Part | Files | Script lines | Content | Depends on |
|---|---|---|---|---|
| **P1 Command shell** | `COMMAND` dispatch for these files (with the main-loop survey), flag model | — | `CommandAvailability`, province-click routing, `Ev_CommandMode` hijack, return codes | main-loop part |
| **P2 Army screens** | Sﾌﾞﾀｲ, Sﾌﾞｶ, Sｱｲﾃﾑ, Cﾀｶﾗ | ~1.6k | `DeploymentRules` (toggle, remove-last, cost, Go/Stop), `SubordinatePicker`, `ItemEquipRules`, Cﾀｶﾗ loss; replace the battle's `Sﾌﾞﾀｲ` hook | — |
| **P3 Enemy city** | Cﾃｷﾄｼ (rule parts 1-430, 3649-4497), Cﾃｷﾄｼ2 | ~1.5k | attack pipeline up to `AttackProvinceAsync`, refusals, defender-setup interface (T*2 belong to the nation survey), Use Force | P2, T*2 setups, FIGT_A (done) |
| **P4 Negotiations** | Cﾃｷﾄｼ 429-3648 | ~3.2k (95 % text) | outcome table (≈200 C# lines) + scene refs | P3 |
| **P5 Own city + merchant** | Cｼﾊｲﾄｼ, Cｼｮｳﾆﾝ | ~3.6k | levy (draw order!), Bolster, story-item table (data), cells, merchant contracts and shop table | P1 |
| **P6 Subordinates, ninja, construction** | Cﾌﾞｶ, Cﾆﾝｼﾞｬ, Cﾃｲﾄ | ~3.8k | offer table, buy/replenish/talk, ninja missions (draw order), building table + completion tick | P2 (Sﾌﾞｶ), P3 (ninja entry) |
| **P7 Harem + data** | Cﾊｰﾚﾑ, Sﾊｰﾚﾑ, 各種DATA | ~1.45k | availability predicate table (50 rows), intro draw, cost/refund, reward, Lia counter; 各種DATA as a view over history | P1 |

Interfaces (reuse the battle's where possible): `IStoryHooks.RunEventAsync(file, Ev_Entry)` for
every G/I/T*2/Cﾒｲｷｭｳ call; a menu input returning item index or cancel (with the
fall-through rule in the core); `IDeployInput`/`IUnitPicker`/`IHaremPicker`/`IItemScreenInput`
returning the raw codes the scripts use; `IScriptRandom` (exists); view events for dialogue
(`ScriptRef`), portraits, sounds (PCMPLAY) and map refresh (`M.ADV` ops 0/1/3/4/10, already in
`ScriptContext.Map.cs`).

**Golden harness changes:** today a capture is one battle. Commands need captures rooted at
each command file (`Cﾃｷﾄｼ`, `Cｼﾊｲﾄｼ`, `Cｼｮｳﾆﾝ`, `Cﾌﾞｶ`, `Cﾆﾝｼﾞｬ` (inner of Cﾃｷﾄｼ), `Cﾃｲﾄ`,
`Cﾊｰﾚﾑ`, `Cﾀｶﾗ`) or, better, one capture per **player phase** rooted at `COMMAND.ADV`. Classes:
inner = C*/S* files of the part under test, WAO_SUB, M, WAO_MAP, PCMPLAY; hook = G*/I*/T*2,
FIGT_A when not under test; `Sﾌﾞﾀｲ`/`Sﾌﾞｶ`/`Sﾊｰﾚﾑ` either inner or **pick** (log `CallArg`,
`PParty1-4`, `Gold`, `Unit_Status`). Inputs to log: every menu (already), every `IM` click
(new record: button + coordinates or the decoded grid code). The comparison must not ignore
`ui` for the command flags (`Ui_Cmd*Avail`, `CmdAvail_Sub*`, `HaremAvail_*`, `Ui_ItemViewMode`).

## 10. Open questions

1. `]` right-click semantics (fall-through assumed) and `IM` codes (16 left / 32 right) — confirm in xsystem35 `cmdi.c`.
2. Can the player attack an owner-9/10 province (no defender setup → stale `EParty`)? (`Cﾃｷﾄｼ.ADV:317-376`)
3. Does anything read the stale `Unit_Cur`/`Item_Cur`/`Cmdr_Cur`/`Prov_Cur = 1`/`Bt_TargetProv`/`Hist_Cur` before resetting them?
4. Is the `Unit_DgnAtkMod[48]` alias write real in-game, and does the id-997 path write out of range or get bounds-checked? Needs a capture.
5. Gｻｰﾅｷｱ#1 (prov 27): does it fight / clear `Ui_CmdAttackAvail`? Gｼｨﾙ#5 (harem) and #2/#6 (merchant): do they touch the command flags?
6. Gﾊﾟｯﾄﾝ#3 then #4 in one pass (separate `if`s, `Cﾃｷﾄｼ.ADV:124-134`) — possible?
7. Meaning of `N_Mars_Score := 2` vs dictionary "1 = active"; where unit 26's troops come from.
8. Commander 39 `TroopAtk = 20` as a levy story gate (`Cｼﾊｲﾄｼ.ADV:928`).
9. Where Rance's level-up from harem EXP happens (not here).
10. Is commander record 100 ever live when `Cﾆﾝｼﾞｬ` zeroes it?
