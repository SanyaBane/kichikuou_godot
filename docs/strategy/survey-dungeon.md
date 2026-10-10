# Survey — the dungeon command (`Cﾒｲｷｭｳ`, with `Cﾀｶﾗ`, `Cｹﾝｷｭｳ`)

Survey of the scripts behind the player's **dungeon search** command, ahead of a spec and a port
into `Kichikuou.Core`. Source: `scenario_named\` (renamed decompile). Confidence: **[C]**
confirmed by reading, **[I]** inference, **[G]** guess.

## 1. Role

`Cﾒｲｷｭｳ.ADV` runs one dungeon visit. The player clicks a dungeon spot on the strategic map
(`COMMAND.ADV:145-160`) or picks "Ruins Search" in the own-city menu of province 35
(`Cｼﾊｲﾄｼ.ADV:227-231`, `538-547`). The file then does one of three things:

- it plays a story scene and returns, for the eight **event-only dungeons**;
- it runs a **floor-by-floor search** with one unit: a menu to go up, go down or retreat, a
  random monster battle on each move (direct `FIGT.ADV` call), forced boss duels, treasure chests
  and story events on fixed floors, and a resume point;
- it opens **God's Door** in the Ancient Ruins (statue puzzle), which leads into the final arc
  `Iｶﾐ.ADV` and never returns.

There is **no maze and no map**. A "dungeon" is a floor counter shown over a background CG in
message windows, plus menus [C]. The command can be used once per week (`Ui_CmdDungeonAvail`).
`Cﾀｶﾗ.ADV` is the separate "Treasure" command: it drops items held by units at status 0, then
opens the item screen `Sｱｲﾃﾑ`. `Cｹﾝｷｭｳ.ADV` is an empty stub.

## 2. Inventory

| File | Lines | Entry points (caller) | Notes |
|---|---|---|---|
| `Cﾒｲｷｭｳ.ADV` | 5747 (942 text lines, ≈2350 non-text, ≈630 of those are enemy-record setters) | `COMMAND.ADV:151` map click on a dungeon spot (radius 10) when `Prov_Owner(Dgn_Prov) = 1` or `Dgn_Prov = 0`; `Cｼﾊｲﾄｼ.ADV:547` own-city "Ruins Search" (prov 35), after it sets record 30: `Dgn_MapX/Y := Prov_MapX/Y`, `Dgn_State := 0`, `Dgn_Prov := 35` (`:538-543`) | Returns with `%0`; the God's Door branch jumps away for good through `Iｶﾐ` (`&#OVER.ADV`) |
| `Cﾀｶﾗ.ADV` | 15 | `COMMAND.ADV:347` (Treasure menu, `L_0074f`) | Loops `Item_Cur` 1..50: if `Item_Holder` ∉ {0,100} and that unit has `Unit_Status = 0`, then `Unit_Item := 0`, `Item_Holder := 0` (`:1-14`); at 51 calls `Sｱｲﾃﾑ.ADV` |
| `Cｹﾝｷｭｳ.ADV` | 1 (`%0:`) | `COMMAND.ADV:363`, **unreachable**: it follows `@L_00129` with no label in between (`:358-364`) | Dead stub ("research") [C] |

Main procedures of `Cﾒｲｷｭｳ.ADV` (labels are byte addresses; lines are inclusive):

| Label / lines | Purpose |
|---|---|
| (top) `:1-166` | Entry gates (`Ev_CommandMode = 2` returns; already searched this week → `L_07418`; Dgn 9 closed unless `F_PG9_State = 2`), resets `DgnRun_MerimInParty`, stale-cursor Gunagan test `:11-13`, map camera (`M` ops 1, 10), `Ui_CmdDungeonAvail := 0`, dungeon name into string 8 and `DungeonBgCg` per dungeon (`:26-150`), Cafe-guide mode (`:151-166`) |
| `L_005b6` `:167-237` | Dispatch: Dgn 1 → `L_03cbd`; prologue (`StoryStage = 0`) returns; event-only dungeons call their scene and return; Dgn 15/16 pre-scenes; Dgn 30 God's Door discovery `L_04ddc` |
| `L_0076a` `:238-305` | Dungeon screen: name, state (Unsearched / In progress / Fully searched), main menu: Start Search, Bring/Leave Merim, Bring Pink Cow, God's Door, Return (`Return` gives the week's action back: `:302`) |
| `L_008d1` `:306-368` | Start search: dialogue, Snow Charm advice (`L_06fe4`) for Dgn 4/5/16, squad pick `Sﾌﾞﾀｲ` (1 unit, mode 3), refusals by units 29/30/32/41/42 |
| `L_00a2c` `:369-668` | Travel: blizzard losses without Snow Charm (Dgn 4/16, Dgn 5), arrival text, Ho Raga / Battleship / Titi Lake scenes, **per-dungeon parameter table** `:469-612`, music, resume-floor menu `:626-668` |
| `L_01137` `:669-857` | Arrival on a floor: header, then the **fixed-floor trigger chain** (treasures, bosses' scenes, story events), each ending in `@L_0177d`; Dgn 30 deep-floor monster upgrades `:850-857` |
| `L_016b4` `:858-893` | Floor menu: Ascend / Descend (labels depend on `DgnRun_FloorMode`), Retreat; saves `Dgn_SavedFloor := DgnRun_Floor` every time (`:863`) |
| `L_0177d` `:894-922` | Re-print the floor header, back to the menu |
| `L_0181c` `:923-944` | Retreat confirmation (Finish / Keep Going) |
| `L_018cd` `:966-998` | End of search: redraw, end-of-search scenes (Melfeis, Mill, Ruberan), `Ev_CommandMode := 0`, return |
| `L_01982` `:999-1080` | Move +1 floor (deeper; "Ascend" in mode 2): encounter roll, forced bosses, battle, Merim inscription roll |
| `L_01c0a` `:1081-1156` | Move −1 floor: the same without the inscription branch |
| `L_01e67` `:1157-1195` | Random encounter: enemy = `DungeonMonsterCmdr`, Hanny records in Dgn 25, troops roll |
| `L_01f47` `:1196-1375` | Boss override (sets record 100), battle setup, `FIGT.ADV`, `死亡捕獲.ADV`, unit-loss exit, Gunagan's treasure, Merim injury roll, Aaya scene |
| `L_023da` `:1376-1444` | Orochi duel (Dgn 14 F38, Fuuka arc), then `Gﾌｳｶ` 5/6 |
| `L_02528`…`L_02da1` `:1445-1932` | 15 treasure-chest routines (`Item_Holder := 100`, fanfare) and `L_02bce` (Milli/G2 scene) |
| `L_02e29` `:1933-2008` | Uncurse Spring menu (Dgn 10 F28) |
| `L_02f97` / `L_0319e` / `L_036f1` `:2009-2749` | KD at the Rising Dragon summit (Dgn 11 F30); "take by force" → floor 20; 3-question quiz for item 17 |
| `L_03cbd` `:2750-2871` | Thieves' Hideout (Dgn 1): `Gﾊﾞｳﾝﾄﾞ`, stage-1 refusal text, prologue random banter |
| `L_03f14`…`L_04c61` `:2872-3470` | Enemy-record setters for commander 100 (bosses, Okayu Fever, Orochi, Hanny tiers); `:3471-3499` dead (Hanny King) |
| `L_04ddc` `:3500-3827` | God's Door found (Dgn 30, holder of item 1 or 2) |
| `L_0539e` `:3828-3854` | Draws the door with the placed statues (`Tmp0001-0004` = overlay CGs, WAO op 333) |
| `L_05437` / `L_054b8` `:3855-4076` | God's Door menu: place statues 15-18, Rance's dialogue, open the door; all four → `L_057b6` (`F_GodsDoor_Quest := 6`) |
| `L_0589d` `:4077-4410` | Rance at the door (dialogue, CG 212) |
| `L_05ea4` `:4411-4497` | Kou's hint (`F_GodsDoor_Quest := 4`) |
| `L_06056` `:4498-4556` | Enter → `%#Iｶﾐ.ADV` (never returns) |
| `L_06153` / `L_06239` / `L_064b1` / `L_066db` `:4557-5056` | Merim inscription progress and rewards to the searching unit |
| `L_06aa0`…`L_06f37` `:5057-5344` | Refusal dialogues of units 41, 42, 32, 30, 29 |
| `L_06fe4` `:5345-5416` | Snow Charm advice (text only) |
| `L_0712e` `:5417-5501` | "Bring Pink Cow" (Sill at the Uncurse Spring), text only |
| `L_072b8` / `L_07344` `:5502-5578` | Merim joins / leaves the party |
| `L_07418` `:5579-5619` | "Already searched this week" refusal |
| `:5620-5747` | Sound stubs (`L_074d0` footsteps 74, `L_07506`, `L_07514` fanfare 101, `L_0753a`) and `WAO_SUB` stubs (`L_07571`=op 111, `L_0758d`=112, `L_075a9`=113, `L_075c5`=311, `L_075d3`=312, `L_075e1`=121, `L_075fd`=122, `L_07619`=123, `L_07635`=321, `L_07643`=322, `L_07651`=131, `L_0766d`=331, `L_0767b`=332, `L_07689`=333, `L_07697`=0, `L_076a4`=1, `L_076be`=3, `L_076cb`=4), `L_0755b` `IM Tmp0019,Tmp0020`, `L_07566` `IZ` |

### 2.1 Dungeon table (records `Dgn_*` + parameters `:469-612`)

| Dgn | Name (string 8) | `Dgn_Prov` (SET_MAP) | Kind | MaxFloor | Mode | `Bt_TroopLimit` | Monster cmdr | `MonsterPower` |
|---|---|---|---|---|---|---|---|---|
| 1 | Thieves' Hideout | 0 (34 after `Iﾊﾝﾗﾝ.ADV:18`) | event (`L_03cbd`) | — | — | — | — | — |
| 2 | Monsters' Cave | 14 | search | 14 | 1 | 80 | 83 | 40 |
| 3 | Thieves' Cave | 1 | event `Gﾌﾞﾘﾃｨ` 1 | | | | | |
| 4 | Snow Dungeon | 38 | search, blizzard | 12 | 1 | 100 | 83 | 200 |
| 5 | Ho Raga's Tower | 38 | event `Gﾎﾗｶﾞ` 1, blizzard | | | | | |
| 6 | Natural Foe's Dungeon | 37 | search | 25 | 1 | 150 | 84 | 300 |
| 7 | Green Village | 72 | event `Gｷｬﾛﾘ` 1 | | | | | |
| 8 | Summit's Mouth | 100 (12 via `Gｻｲｾﾞﾙ`/`Iﾏｼﾞﾝ`) | search | 30 | 1 | 200 | 85 | 300 |
| 9 | Piar's Ship (written to string **9**, `:70`) | 91 | event `Gﾊﾟｲｱｰﾙ` 7 | | | | | |
| 10 | Uncurse Dungeon | 26 | search | 28 | 1 | 400 | 83 | 600 |
| 11 | Rising Dragon Mountain | 71 | search (tower) | 30 | 2 | 500 | 83 | 300 |
| 12 | Kalar Forest | 52 | event `Gｶﾗｰ` 1 | | | | | |
| 13 | Peach Blossom Spring | 27 | event `Gﾄｳｹﾞﾝ` 1 | | | | | |
| 14 | Orochi's Pit | 55 | search | 38 | 1 | 400 | 83 | 600 |
| 15 | Isolated Shrine Island | 11 | event, or search in week 1 | 38 | 1 | 270 | 84 | 320 |
| 16 | Giant Battleship | 43 | search, blizzard, key | 20 | 1 | 500 | 100 (Horus, `G1.ADV:1099-1131`) | 800 |
| 17 | Mt. Salen | 54 | event `G1` 15 | | | | | |
| 18 | Stone-Cutter Dungeon | 53 | search | 27 | 1 | 1000 | 83 | 1000 |
| 19 | Dungeon of Lies | 78 | search | 27 | 1 | 1000 | 83 | 1000 |
| 20 | Mos's Dungeon | 85 | search | 50 | 1 | 1000 | 83 | 1000 |
| 21 | Maid Graveyard | 87 | search | 50 | 1 | 1000 | 85 | 1000 |
| 22 | Rex's Dungeon | 86 | search | 50 | 1 | 1000 | 83 | 1000 |
| 23 | Holy Gal's Dungeon | 60 | search | 76 | 1 | 500 | 85 | 350 |
| 24 | Dangerous Hole | 21 | search | 50 | 1 | 750 | 84 | 380 |
| 25 | Titi Lake / Haniwa Temple | 20 | search (Hanny tiers) | 66 | 1 | 600 | 100 | 1000 |
| 26 | Hyper Building | 23 | search (tower) | 201 | 2 | 600 | 83 | 100 |
| 30 | Ancient Ruins | 35 (`Cｼﾊｲﾄｼ`) | search, God's Door | 1000 | 1 | 600 | 83; 84 above F200, 85 above F500 (`:850-857`) | 250 |

Monster commanders 83/84/85 are Sergeant / Lieutenant / Colonel Hanny (`SET_CHR.ADV:4142`, `4169`,
`4196`). Background CG is 1032 by default, switched to 1031 on arrival (`:457`); per-dungeon
values are at `:28-150` and `:456-465`. Battle background: 910, 912 for Dgn 11, 914 for Dgn 16/26 (`:1274-1280`) [C].

### 2.2 Fixed-floor triggers (`:700-857`, checked in this order on every arrival, first match wins)

| Dgn | Floor | Condition | Effect |
|---|---|---|---|
| 2 | 14 | `Dgn_State = 1` | item 9 DO Pot (`L_02528`), `Dgn_State := 2` |
| 4 | 12 | state 1 | item 11 Snow Charm, state 2 |
| 6 | 25 | state 1 | item 41 Hawaiian Sword, state 2 |
| 8 | 20 | `Item_Holder(42) = 0` | item 42 Aztec Armor |
| 8 | 30 | `F_Seizel_State < 6` | `Gｻｲｾﾞﾙ` 3 |
| 10 | 27 | Merim in party | `Gﾒﾘﾑ` 4 |
| 10 | 28 | — | Uncurse Spring menu (`L_02e29`), state 2 |
| 11 | 30 | — | KD / quiz (`L_02f97`), state 2 |
| 14 | 38 | `F_Fuuka_State = 9`, shrine visited | Orochi duel (`L_023da`); on unit loss: exit command |
| 14 | 20 | state 1 | item 22, state 2 |
| 15 | 4 / 38 | `F_Arcy_State = 1` / — | `G1` 5 / `G1` 6 |
| 16 | 20 | — | `G1` 14, then **end of search** (`@L_018cd`) |
| 18 / 19 | 27 / 25 | state 1 | item 33 / item 21, state 2 |
| 23 | 33 / 76 | — / state 1 | `L_02bce` (`Gﾐﾘ` 3 or `G2` 1) / item 34, state 2 |
| 24 | 50 / 10 | state 1 / `Item_Holder(28) = 0` and `FogCard_Price = 0` | item 29, state 2 / item 28 Fog Card, `FogCard_Price := 120` |
| 22 / 20 | 50 | `F_Kaybwan_State` / `F_Kaybnyan_State` ∈ {2,3} | `Gｹｲﾌﾞﾜﾝ` 4 / `Gｹｲﾌﾞﾆｬﾝ` 4 |
| 20 / 21 | 5 / 16 | holder 0 | item 38 / item 39 |
| 25 | 2 / 8 / 66 | — / holder(30)=0 / `F_HannyKing_State < 7` | `G1` 18 / item 30 / `Gﾊﾆｰ` 1, state 2 |
| 26 | 201 | state 1 | item 23 Wisdom Ring, state 2 |
| 30 | 20 / 50 | holder 0 | item 48 / item 36 |

### 2.3 Bosses (forced battle: `RetVal := 1` on that floor while the flag is 0, `:1020-1064`)

All overwrite enemy commander record **100** (name into strings 4/5 + `MM`, CGs, stats, troops 0)
and set `Bt_EnemyParty1 := 100`, music `SS 18` (`:1196-1271`). Atk/Def/Attacks/HP are given below.

| Dgn F | Boss (setter) | Flag set **at the fight** | Atk/Def/Att/HP (other) |
|---|---|---|---|
| 2 F5 | Marder `L_04376` | `F_Boss_Marder` | 15/10/20/10, class 2 |
| 6 F10 | Okayu Fever `L_03f14` | **none: on every visit**, not a duel | 0/0/0/10 |
| 8 F10 | Stone Guardian `L_042b5` | `F_Boss_StoneGuardian` | 25/18/20/15 |
| 10 F10 | Haw Kyu `L_0442f` | `F_Boss_HawKyu` | 25/5/250/10 |
| 11 F5-25 | Yellow/Red/Green/Blue/Black Dragon `L_04727`…`L_04a23` | `F_*Dragon_Beaten` | 30/22/50/23 … 45/27/160/43 |
| 14 F12 | Musashibou `L_044ea` | `F_Boss_Musashibou` | 20/30/30/19 |
| 14 F38 | Orochi `L_03fc8` (via `L_023da`, no troop roll) | none (Fuuka arc gates it) | 35/24/120/33, magic 4 |
| 20 F10 | SHIMO-JOE `L_0407d` | `F_Boss_ShimoJoe` | 5/5/10/5 |
| 21 F15 | Vevetar `L_04139` | `F_Boss_Vevetar` | 5/18/100/22, class 1, magic 15 |
| 22 F13 | GG Caterpillar `L_041f4` | `F_Boss_GGCaterpillar` | 20/10/20/50 |
| 30 F100 | Japanese Soldier `L_045a7` | `F_Boss_JapaneseSoldier` | 30/10/73/9, strategy 80 |
| 30 F1000 | Gunagan `L_0466c` | `F_Gunagan_State := 1` (2 after the fight, `:1300`) | 38/22/150/45, magic 4 |

Every boss except Okayu Fever sets `Bt_IsDuel := 1`. The flag is written before `FIGT`, so a
boss can be fought **once** whatever the outcome [C].

## 3. Call graph

Calls out of `Cﾒｲｷｭｳ.ADV` (page calls `%#`, count):

| Target | Count | Where / why |
|---|---|---|
| `WAO_SUB.ADV` | 26 stub sites, ≈1300 calls (466 to op 1 key wait, 340 to op 0 close) | message windows, portraits, centre picture |
| `M.ADV` | 15 (op 0 ×13, op 1, op 10) | map camera / redraw |
| `PCMPLAY.ADV` | 8 | sounds 74, 89, 90, 101 |
| `FIGT.ADV` | 1 (`:1282`) | every dungeon battle (`Bt_Kind = 3`) |
| `死亡捕獲.ADV` | 1 (`:1288`) | deaths/captures after the battle |
| `Sﾌﾞﾀｲ.ADV` | 1 (`:335`) | squad pick, `Tmp0021 = 1` unit, `Tmp0022 = 3` dungeon |
| `G1.ADV` | 13 | ev 2,3,4,10,15 (`:212-229`); 11,12,17 (`:442-451`); 13 (`:541`); 5,6,14,18 (`:757-821`) |
| `Gｶﾌｪ.ADV` | 3 | ev 32 / 31 (`:153`, `:164`, Cafe mode), 35 (`:1983`) |
| `Gﾎﾗｶﾞ` | 2 | ev 2 (`:52`, Dgn 5 unsearched), ev 1 (`:436`) |
| `Gﾒﾙﾌｪｲｽ` | 2 | ev 4 end of search (`:987`), ev 3 spring (`:1978`) |
| `Gﾌｳｶ` | 2 | ev 5/6 after Orochi (`:1437-1441`) |
| `G2` | 2 | ev 2 Holy Gal's with Rance (`:620`), ev 1 (`:1814`) |
| one each | 15 | `Gﾌﾞﾘﾃｨ`1 `:184`, `Gｷｬﾛﾘ`1 `:189`, `Gﾊﾟｲｱｰﾙ`7 `:197`, `Gｶﾗｰ`1 `:202`, `Gﾄｳｹﾞﾝ`1 `:207`, `Gｻｲｾﾞﾙ`3 `:722`, `Gﾒﾘﾑ`4 `:727`, `Gｹｲﾌﾞﾜﾝ`4 `:801`, `Gｹｲﾌﾞﾆｬﾝ`4 `:816`, `Gﾊﾆｰ`1 `:831`, `Gﾐﾙ`2 `:992`, `Gﾙｰﾍﾞﾗﾝ`3 `:996`, `Gｱｰﾔ`4 `:1373`, `Gﾐﾘ`3 `:1810`, `Gﾊﾞｳﾝﾄﾞ`1 `:2754` |
| `Iｶﾐ.ADV` | 1 (`:4556`) | final God-dungeon arc: 6528 lines, 4 `FIGT` sites, its own `Sﾌﾞﾀｲ`, ends only in `&#OVER.ADV` (6 sites) [C]; **not covered by this survey** |

Calls in: `COMMAND.ADV:151`, `Cｼﾊｲﾄｼ.ADV:547`. Story scenes that contain their own `FIGT` call
(`Gｶﾌｪ` 1, `Gｻｲｾﾞﾙ` 1) look like hooks from here. No calls to `COM.ADV`, `FIGT_A/B`, `WAO_MAP`
or any `T*` file [C].

## 4. State

| Group | Read | Written |
|---|---|---|
| dungeon record (page 1, cursor `Dgn_Cur`) | `Dgn_MapX/Y`, `Dgn_State`, `Dgn_SavedFloor` | `Dgn_State` 0→1 on a search (`:474`), →2 on the last floor; `Dgn_SavedFloor` on every menu (`:863`) |
| dungeon run scalars | — | `DgnRun_Floor/MaxFloor/FloorMode/MonsterPower/MerimInParty`, `DungeonBgCg`, `DungeonMonsterCmdr` |
| dungeon flags | `F_Boss_*`, `F_*Dragon_Beaten`, `F_Gunagan_State`, `F_GodsDoor_Found`, `F_Statue_*`, `F_Battleship_Door`, `F_Arcy_State` | the same (bosses, statues `:3950-4010`, `F_GodsDoor_Found := 1` `:3501`) |
| battle inputs | — | `Bt_PlayerParty1-4` (Cafe mode `:156-159`, else `Sﾌﾞﾀｲ`), `Bt_EnemyParty1-4` (`:470-473`, `:1158`), `Bt_TroopLimit`, `Bt_EnemyRetreatMode := 1`, `Bt_WallBonus := 0`, `Bt_BackgroundCg` (910/912/914, back to 900), `Bt_Kind := 3` then 0, `Bt_IsDuel` (bosses; reset `:1290`) |
| unit (cursor `Unit_Cur`) | `Unit_Troops`, `Unit_Item`, `Unit_Status`, `Unit_FaceCg` | `Unit_Troops` (blizzard), `Unit_Status := 1` (refusals), `Unit_MaxHP/Atk/Def/Magic` (inscriptions `:4688`, `:4848`, `:5031-5034`) |
| enemy commander (cursor `Cmdr_Cur`) | `Cmdr_MaxHP`, `Cmdr_CgFull` | `Cmdr_Troops`, `Cmdr_HP` of the monster record (`:1192-1195`); whole record 100 by the setters |
| items (cursor `Item_Cur`) | `Item_Holder` of 1, 2, 9-11, 15-18, 21-23, 28-30, 33-34, 36, 38-39, 41-42, 45, 48 | `Item_Holder := 100` (treasures), `:= 0` (statues placed); `FogCard_Price := 120` |
| story | `StoryStage`, `DateWeek`, `Ev_CommandMode`, `F_Merim_*`, `N_Merim_Inscriptions`, `F_Kou_State`, `F_Fuuka_*`, `F_Seizel_State`, `F_HannyKing_State`, `F_Kaybwan/Kaybnyan_State`, `N_Aaya_Stage`, `N_Melfeis_Stage`, `N_Soul_Stage`, `F_PG9_State`, `F_SillCow_State`, `F_Ruberan_State`, `F_Wenlina_CureMilli`, `VAR0427`, `Bld_Hospital`, `DebugMode` | `Ev_CommandMode := 0` (`:989`), `F_Merim_MillArc` 3→4 (`:8-10`), `F_Merim_Dungeon` 2/3, `N_Merim_Inscriptions`, `F_GodsDoor_Quest` 4/6, `F_Gunagan_State` |
| ui | `Ui_CmdDungeonAvail` | `:= 0` on entry (`:25`), `:= 1` on Return / Dgn 1 (`:169`, `:176`, `:302`); `Ui_MouseX/Y`, `Ui_WaoOp` |
| scratch | — | `RetVal`, `CallArg`, `Tmp0690` (roll copies), `Tmp0621` (quiz count), `Tmp0001-0004` (door overlays `:3834-3849`), `Tmp0019/0020` (pointer, `:5647`), `MapTargetX/Y`, `MapCamX/Y` |
| strings | — | `MS 8` dungeon name (`MS 9` for Dgn 9, `:70`), `MS 10` "Monsters" (`:27`), `MS 4/5` + `MM` boss names into the commander-100 name slots, `MM 1` unit name (`:155`, `:343`) |

Not touched: `Gold` and `Hist_*` (the deploy cost is charged inside `Sﾌﾞﾀｲ.ADV:189-207`), page 4.

**Aliasing [C].** `SET.ADV:219-224` binds the five `Dgn_*` fields with stride `Dgn_Count = 30`,
which gives records 0..29. Record **30** (Ancient Ruins, `Cｼﾊｲﾄｼ.ADV:539`) therefore overlaps
record 0: `Dgn_MapX[30]` is the same slot as `Dgn_MapY[0]`, … `Dgn_SavedFloor[30]` is
`Dgn_Prov[0]`, and `Dgn_Prov[30]` lies past the block (page 1 has 20001 slots, `SET.ADV:135`).
The map-click loop scans records 0..30 (`COMMAND.ADV:145-160`). The aliasing comes for free if
the port keeps the `ScriptMemory` page layout.

**`Ui_CmdDungeonAvail` is rule state**: it decides whether the command can run this week and is
reset weekly at `COMMAND.ADV:44`. Like `Ui_CmdAttackAvail`, golden comparisons must not ignore it.

## 5. Randomness — 11 `Y 4` sites (none in `Cﾀｶﾗ`/`Cｹﾝｷｭｳ`)

| # | Line | Draw | Condition | Use |
|---|---|---|---|---|
| 1 | `:386` | `Y 4, Unit_Troops` | Dgn 4/16, `Item_Holder(11) = 0` | troops lost to the blizzard = r |
| 2 | `:405` | `Y 4, Unit_Troops` | Dgn 5, no Snow Charm | lost = min(`Troops/2 + r`, `Troops`) |
| 3 | `:1016` | `Y 4, 4` | every +1 move | 1 = battle, 2 = inscription branch |
| 4 | `:1018` | `Y 4, 10` | +1 move, `Unit_Item = 10` (Safety Bonsai) | replaces #3; both are drawn |
| 5 | `:1075` | `Y 4, 10` | `RetVal = 2` after #3/#4 (or after a battle, see §7) | 1 and Merim in party → `L_06153`; **drawn even without Merim** |
| 6 | `:1098` | `Y 4, 4` | every −1 move | 1 = battle (no branch for 2) |
| 7 | `:1100` | `Y 4, 10` | −1 move, Safety Bonsai | replaces #6 |
| 8 | `:1193` | `Y 4, DgnRun_MonsterPower` | every `L_01e67` battle, **also before a boss overrides it** | `Cmdr_Troops := Power/3 + r` on the monster record |
| 9 | `:1339` | `Y 4, 20` | after a battle, Merim in party | 1 → Merim injured (`Bld_Hospital = 1`) or killed |
| 10 | `:2459` | `Y 4, 5` | each of the 3 KD quiz questions | question variant; answer order differs by variant |
| 11 | `:2804` | `Y 4, 3` | Dgn 1 in the prologue | dialogue variant only |

Forced boss floors still draw #3/#4 and then overwrite `RetVal := 1` (`:1020-1064`). Draws with
`n ≤ 1` (#1, #2 with ≤1 troops) consume nothing. Hooks add their own: `G1` 1, `Gﾌｳｶ` 3,
`Gﾊﾆｰ` 2, `Gﾄｳｹﾞﾝ` 3, `Gｻｲｾﾞﾙ` 2, `Gｶﾌｪ`/`Gﾌﾞﾘﾃｨ`/`Gﾊﾞｳﾝﾄﾞ`/`Gｶﾗｰ`/`Gｱｰﾔ` 1 each, plus the battle's own draws.

## 6. Input and display

| Input | Where | Notes |
|---|---|---|
| Map click | `COMMAND.ADV:145-160` | entry; provinces win over dungeons (they are checked first, radius 12-36) |
| Main dungeon menu (5 items max) | `:266-298` | right-click = Return (`:299-300`) |
| Squad pick | `Sﾌﾞﾀｲ.ADV` via `:335` | 1 unit, charges gold, `Unit_Status := 2`; cancel → re-dispatch `@L_005b6` |
| Resume menu | `:651-661` | "nF On" / "From the Start"; cancel = from the start |
| Floor menu | `:864-893` | Ascend/Descend/Retreat; the item order depends on `FloorMode`; cancel re-shows |
| Retreat confirmation | `:933-944` | Finish / Keep Going; a cancel falls into a second, empty `]` (`:942`) |
| Uncurse Spring | `:1953-1962` | Enter / Don't |
| KD | `:2362-2371`, 5 × 3-answer quiz menus `:2475-2611` | |
| God's Door | `:3887-3932` | statues held, talk, open, leave |
| Key waits | 466 calls to `L_076a4` (WAO op 1) | `IK 0` inside `WAO_SUB` only while a window is open |
| Button-release loops | 13 `IM` loops (`:261`, `:647`, `:859`, `:929`, …) | not logged by the harness; `RetVal = 0` after each |
| Pointer save/restore | `L_0755b` `IM Tmp0019,Tmp0020` / `L_07566` `IZ` around the main menu | **pointer-dependent scratch** |

**Display only, free to redesign:** the dungeon screen (background CG 1008-1056 via WAO op 331),
name, floor and troop header, all monologue, portraits, sounds (74 steps, 89/90 quiz, 101
fanfare), music (`SS 21` dungeon, `SS 18` boss, `SS 26` God's Door), the God's Door picture
with statue overlays, Gunagan's painting (CG 806/862), and the menu layout. No grid, maze or
movement exists to preserve. **Rule-bearing:** the menu *choices* (including which items are
offered), their order of effects, and every draw.

## 7. Rules vs presentation; quirks

The flow is compact (about 1000 lines from `:1` to `:1375`). The bulk of the file is text with
small state effects (treasures, inscriptions, God's Door) and data (setters). Display goes
through stubs, so rules and drawing separate cleanly. Quirks to reproduce or decide on:

1. **Stale cursor, Gunagan** [C/I]: `:11-13` tests `Cmdr_CgFull = 1137` for whatever `Cmdr_Cur`
   is on entry. After a Gunagan fight that `Cmdr_Cur` is still 100, so the next entry marks him
   beaten (`F_Gunagan_State := 2`). After any Gunagan battle in which the unit survives (a retreat
   too), `:1300` shows "a treasure was found behind Gunagan's corpse" [I].
2. **`RetVal` leaks out of the battle** [C]: `:1074` tests `RetVal = 2` after `L_01e67` returns.
   By then `RetVal` is whatever the battle path left: the Merim roll #9 if Merim came, otherwise
   `死亡捕獲`/`Gｱｰﾔ`/`FIGT`. That can trigger draw #5 after a battle.
3. Only the +1 direction has the inscription branch (`:1074-1079` vs `:1147-1156`) [C].
4. **Boss flags are set at the fight**: one attempt per boss. Okayu Fever has no flag; Orochi is
   gated only by the Fuuka arc [C].
5. Monster troops can exceed `MaxTroops` (`Power/3 + rnd(Power)`, e.g. 1333 vs 1200) [C]. The
   roll is consumed before a boss replaces the enemy [C].
6. In Dgn 25 the encounter record is commander 100, rewritten by the Hanny tier setters on each
   battle (`:1183-1191`); the "Hanny King" setter `:3471-3499` is dead code [C].
7. **Re-dispatch on cancel**: cancelling the squad pick or a refusal jumps to `L_005b6` (`:340`,
   `:347`), which re-runs pre-scenes (Dgn 15 `G1` 2/4, Dgn 16 `G1` 10) [C].
8. **Refusing units keep the charge**: `Sﾌﾞﾀｲ` already took the gold and set status 2; the
   refusal sets `Unit_Status := 1` and refunds nothing (`:344-368`) [C].
9. Dead code [C]: `:175-181` (repeated Dgn 1 / stage-0 checks), `:945-962` ("Will you go
   outside?"), `:3471-3499`, the `Cｹﾝｷｭｳ` call; `MS 9` for Dgn 9 (`:70`) is harmless because
   Dgn 9 never reaches `X 8`.
10. Empty menu after a cancelled retreat confirmation (`:941-942`): xsystem35 `sel_select` with
    no items exits only on a right click (`selection.c:414-545`) [C, engine].
11. **Unit lost in battle** (`Unit_Status = 0`): the command exits at once (`:1065-1072`,
    `:1147-1154`, `:742-747`). It skips the end-of-search scenes and `Ev_CommandMode := 0`, and
    leaves `Dgn_SavedFloor` at the floor *before* the move [C].
12. Snow Charm protects if owned at all (`Item_Holder ≠ 0`, `:371`, `:395`). The advice text
    only distinguishes 100 from 0 (`:5347`, `:5374`) [C].
13. With the Giant B. Key **equipped** (holder 1-50), both key checks `:440-448` are skipped and
    the search goes on [C].
14. Treasures gated on `Item_Holder = 0` (Dgn 8, 20, 21, 24 F10, 25, 30) can be found again
    after `Cﾀｶﾗ` drops an item held by a status-0 unit [I].
15. `F_Merim_MillArc` 3→4 on every entry (`:8-10`), so the end-of-search `Gﾐﾙ` 2 (`:990`) fires
    only if 3 was set during this run [I]. Who sets it mid-run is unknown.
16. KD: "take by force" or a wrong answer sets `DgnRun_Floor := 20` (`:2420`, `:2694`); the quiz
    can be retaken on every visit; the prize needs `F_GodsDoor_Quest > 1` and item 17 not owned [C].
17. Cafe mode (`Ev_CommandMode = 1`, Dgn 10) skips `Sﾌﾞﾀｲ`: Rance goes alone, at no cost and
    without status 2 (`:151-160`) [C].
18. Treasure routines `L_02abb`…`L_02da1` do not reset `Unit_Cur := Bt_PlayerParty1` as the
    others do. No state difference was found [I].
19. Story-only dungeons spend the week's action (`:25`) unless the scene sets
    `Ui_CmdDungeonAvail := 1` (`G1.ADV:3050`, `Gﾄｳｹﾞﾝ.ADV:182`, `Gｶﾌｪ.ADV:5881`) [C].

## 8. Porting proposal

The file breaks naturally into a flow layer and a data/scene layer:

| Part | Script lines | Content | Depends on |
|---|---|---|---|
| **D0 shared helpers** (prerequisite, small) | — | Move `WaoSubAsync`, the `M.ADV` ops and PCMPLAY out of `Battle` (`ScriptContext.Wao.cs`, `ScriptContext.Map.cs`) into a shared presentation-helper service. The strategic commands reuse the same ops: WAO 0,1,3,4,111-123,131,311-333; M 0,1,10 | battle port |
| **D1 dungeon flow** | `:1-1375`, `:2872-3470` (setters as data), `:5579-5747`, plus `Cﾀｶﾗ` | entry gates, dispatch, main menu, squad hook, blizzard, parameter table, resume, floor loop, trigger chain (as an ordered table), moves, encounter, boss override, battle setup → `Battle.FightAsync`, `DeathCaptureAsync`, post-battle (Merim, Gunagan, Aaya), end of search. ≈2.4k lines, half of them data | D0, `IStoryHooks`, an `ISquadPicker` (`Sﾌﾞﾀｲ` mode 3, ported later with the S* screens) |
| **D2 dungeon scenes with state** | `:1376-2871`, `:3500-5578` | Orochi, 15 treasure chests, Uncurse Spring, KD + quiz, Thieves' Hideout, God's Door (statues, quest 4/6, entry into `Iｶﾐ` as a hook that ends in a jump), Merim inscriptions/join/leave, refusals, Pink Cow, Snow Charm advice. ≈2.9k lines, ≈80% text | D1 seams |

D2 is mostly linear text runs between state writes. Use the `gen_figt_m.py` approach: generate
the text-run code (ScriptRefs + waits) from the decompile and hand-write only the branches and
writes. Expose the D1/D2 seams the way the battle did: internal parameterless methods that read
`GameState` (`TreasureAsync(label)`, `KdSummitAsync()`, `GodsDoorAsync()`, `MerimInscriptionAsync()`).

**Hooks/interfaces:** `IStoryHooks.RunEventAsync` for the 22 scene files and `Iｶﾐ` (it throws
`ScenarioJumpException`); squad pick; input `ChooseAsync` for the menus (the option list must
reproduce the conditional items and their order); key waits through WAO op 1.

**Golden harness — what to capture:**
- New root `Cﾒｲｷｭｳ.ADV` in `tools/golden/classes.txt`. While a capture is open, a root file that
  is not listed as inner is a hook (`golden.c` `golden_on_call`), so `FIGT.ADV` would become a
  hook with memory dumps. That isolates the dungeon flow from the battle. A second config that
  lists `FIGT*` as inner checks the full run through the battle port. One session cannot capture
  the dungeon and its battles separately.
- `Sﾌﾞﾀｲ.ADV` and the story scenes stay hooks; `WAO_SUB`/`M`/`PCMPLAY`/`死亡捕獲` stay inner.
- Ignore list additions for this root: `Tmp0019`/`Tmp0020` (pointer from `IM`, `:5647`). Check
  whether `RetVal` after `L_0755b` (button state) can leak to the exit dump (path "Return" →
  `%0`). It is normally 0 because a release loop runs just before.
- Do **not** ignore `Ui_CmdDungeonAvail`.
- Wanted captures: a plain search in Dgn 2 with retreat; resume from the saved floor; a boss
  (Marder) and a dragon; a treasure floor; death of the searching unit; Safety Bonsai equipped;
  Merim in the party (draws #5, #9); Dgn 25 Hanny tiers; KD quiz pass and fail; God's Door
  statues; Cafe mode; a blizzard; a refusal by unit 29-42 (checks the gold); the Ruins Search
  entry from `Cｼﾊｲﾄｼ` (record-30 aliasing).

## 9. Open questions

1. Who sets `F_Merim_MillArc = 3` during a search, so that `Gﾐﾙ` 2 at `:990` can fire? (`Gﾐﾙ.ADV:917`.)
2. Is `:11-13` meant as a recovery after an unfinished Gunagan fight, and can `Cmdr_Cur = 100`
   with `Cmdr_CgFull = 1137` survive to a later entry in practice (`FIGT.ADV:1715` battle
   question 5 is related)?
3. Can record 0 (`Dgn_MapX = 0`, `MapY` = record 30's `MapX`) be hit by a map click at the left
   edge (`COMMAND.ADV:147-150`, saturating `Dgn_MapX - 10 = 0`)? It would run the dungeon with
   stale `DgnRun_*` parameters.
4. `RetVal` after `L_0755b` reaches the caller on "Return": can it be non-zero (`IM` button
   state)? A capture answers it.
5. `VAR0427` (Mill quest chain) is unnamed (G); it gates `G2` 2 (`:617`) and `Gﾐﾘ` 3 (`:1808`).
6. `Iｶﾐ.ADV` (6528 lines, 4 `FIGT` sites, its own rearguard state `VAR0304-0319`) is rule-heavy
   and needs its own survey. It is reachable only from God's Door here.
7. *Answered (2026-10-10):* `Sﾌﾞﾀｲ` was ported whole with W3 part 3 (`DeploymentScreen`), mode 3 included;
   the dungeon calls it through the router. Originally: `Sﾌﾞﾀｲ.ADV` is shared by the attack/defence/dungeon commands; which survey owns it? The
   dungeon needs only mode 3 with one unit.
