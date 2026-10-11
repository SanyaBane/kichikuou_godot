# Survey — enemy nation turns and AI (`T*.ADV`)

Scope: `TCﾘｰｻﾞｽ`, `TJAPAN`, `Tﾍﾙﾏﾝ`, `Tﾊﾟｯﾄﾝ`, `Tｾﾞｽ`, `Tｹｲﾌﾞﾘｽ`, `Tﾙﾄﾞﾗ` (the nation phases) and
their `*2` companions. Sources: `_extract\scenario_named\` (cited `FILE.ADV:line`). Confidence:
**[C]** read in the script, **[I]** inference, **[G]** guess. Battle names follow
[../battle/01-flow.md](../battle/01-flow.md); the week loop that calls the phases is in
[survey-main.md](survey-main.md). `TEST.ADV` (660 lines) is a debug file, not part of this group.

## 1. Role

Each base file is one enemy nation's **weekly phase**: `MAIN` sets the BGM, centres the map, prints
"<Nation> Phase", refreshes the nation's commanders (HP +2, troops + 3×`Cmdr_Recovery`, capped —
done in `MAIN`, not here, except Kayblis) and page-calls the file (`MAIN.ADV:298-846`) [C]. The
file fires story scenes, picks up to four commanders from fixed priority lists into
`Bt_EnemyParty1-4`, checks a minimum troop total, walks a fixed province list and attacks the
first player province through `FIGT_B` (= `Battle.DefendProvinceAsync`), or takes a non-player
province without a battle. There is no evaluation of the map beyond "first owned province in
the list" — the "AI" is data. Each `*2` file is the **defence picker** used when the *player*
attacks that nation: `Cﾃｷﾄｼ.ADV:319-355` calls it before `FIGT_A` to fill `Bt_EnemyParty1-4`,
`Bt_EnemyRetreatMode` and `Bt_WallBonus` [C]. Large parts of `TJAPAN` and `Tｹｲﾌﾞﾘｽ` are story
dispatch; `TJAPAN.ADV:295-2744` is a self-contained set of story scenes (Gekkou/Shinobu).

## 2. Inventory

### 2.1 Files

| File | Lines | Caller | Gate (MAIN) | `Ev_PhaseNation` | Rule lines (est.) |
|---|---|---|---|---|---|
| `TCﾘｰｻﾞｽ` | 268 | `MAIN.ADV:346` | `StoryStage = 1` (`:298`) | 2 (`:1`) | 200 (+68 dead helpers) |
| `TJAPAN` | 2851 | `MAIN.ADV:399` | `F_Japan_State = 1` (`:348`) | 6 (`:1`) | ~320; ~2450 scene text |
| `Tﾍﾙﾏﾝ` | 792 | `MAIN.ADV:452` | `F_Helman_War = 1` (`:401`) | 4 (`:1`) | ~720 |
| `Tﾊﾟｯﾄﾝ` | 226 | `MAIN.ADV:505` | `F_PattonArmy_State ≠ 0` (`:454`) | 5 (`:1`) | ~160 |
| `Tｾﾞｽ` | 777 | `MAIN.ADV:592` | `F_Zeth_War = 1` (`:507`) | 7 (`:1`) | 777 |
| `Tｹｲﾌﾞﾘｽ` | 1855 | `MAIN.ADV:629` | `StoryStage = 3` & `F_Kayblis_War ∈ {0,2}` (`:594`) | 8 (set by `MAIN`) | ~1700 |
| `Tﾙﾄﾞﾗ` | 205 | `MAIN.ADV:846` | `StoryStage = 4` (`:816`) | 10 (`:6`) | ~130 |
| `TCﾘｰｻﾞｽ2` | 162 | `Cﾃｷﾄｼ.ADV:319` | `Prov_Owner = 2` | 2 | 162 |
| `TJAPAN2` | 219 | `Cﾃｷﾄｼ.ADV:342` | owner 6 (not when `F_Japan_State = 10`, `:321`) | 6 | 219 |
| `Tﾍﾙﾏﾝ2` | 377 | `Cﾃｷﾄｼ.ADV:348` | owner 4 | 4 | 377 |
| `Tｾﾞｽ2` | 401 | `Cﾃｷﾄｼ.ADV:345` | owner 7 | 7 | 401 |
| `Tｹｲﾌﾞﾘｽ2` | 347 | `Cﾃｷﾄｼ.ADV:355` | owner 8 | 8 | 278 (+69 dead helpers) |
| `Tﾙﾄﾞﾗ2` | 1 | none (`%0:` only) | — | — | 0 |

Patton has no `*2`: `Cﾃｷﾄｼ.ADV:361-376` inlines the same list (Patton (54), Hubert (55), Freak (56), Hanty (57), +Aristoles (32)) via its own picker
`L_04f2a` (`:4265`) [C]. Phases not in this group but built on the same pattern: AL Church
(`GAL` 5), Camilla Unit (inline in `MAIN.ADV:631-815`), La Vaswald (`Gｻｲｾﾞﾙ` 12) — see survey-main.

### 2.2 Procedures (base files)

| File | Label / lines | Purpose |
|---|---|---|
| TCﾘｰｻﾞｽ | `:1-14` | army name; `F_LeazasRebels_State 1→2` + `Gｴｸｽ` 10; Ys (5) player-owned → group C |
| | `:15-48` A, `L_00132` `:49-79` B, `L_001ce` `:80-105` C | three groups, see §2.4 |
| | `L_00251` `:108`, `L_00297` `:119`, `L_00409` `:179` | troop sum, picker, slot filler (troops < 190 skipped, also for slot 1) |
| TJAPAN | `:1-62` | Gekkou attempt, Kou events, 1/4 idle roll, army choice `Y 4,5` |
| | `:63-147`, `L_00366` `:148-202` | target walk; capture of city-state provinces |
| | `L_0046d` `:203`, `L_004b3` `:214`, `L_00625` `:274` | sum, picker, filler (troops < 190 skipped once slot 1 is filled) |
| | `L_006ac` `:295-487` | Gekkou raid: gold theft or assassination (`L_00a2b` `:488`) or Shinobu (`L_024b0` `:1960`) |
| | `L_00b86/00f8b/012f5/016ab/01b07/01e9c` `:551-1959` | assassination scenes for units Barres (6), Rick (7), Cordoba (8), Ex (14), Eleanor (23), Gengorou (15) |
| | `L_024b0` `:1960-2161`, `L_02851` `:2162-2375`, `L_02c04/02ddd` `:2376-2744` | Shinobu attempt; game over (ending 4); 5th-attempt finale (display only) |
| | `L_0327c` `:2745` | random Kou / `IJAPAN` event |
| Tﾍﾙﾏﾝ | `:4-98` | Stessel/Lelyukov/Aristoles/Patton events, Tainyar (commander 26) revival, army dispatch, `Gｽﾃｯｾﾙ` 14 if nothing happened |
| | `L_002e1` `:99`, `L_00521` `:208`, `L_0068c` `:270`, `L_00824` `:345`, `L_0090d` `:384`, `L_009c3` `:418`, `L_00c09` `:517` | 1st-5th armies, 4th-army variant, Crusaders |
| | `L_00d83` `:584`, `L_00dc9` `:595`, `L_00f3b` `:655`, `L_00fc2` `:676` | sum, picker, filler (<190 once slot 1 filled), capture from Kayblis |
| Tﾊﾟｯﾄﾝ | `:1-68`, `L_00198`, `L_001de`, `L_00350` | events 7/8/9/11 (`Gﾊﾟｯﾄﾝ`), `Gｱﾘｽﾄﾚｽ` 3; one army; sum; picker; filler (no filter) |
| Tｾﾞｽ | `:1-53` | ritual/arc events (`Gﾊﾟﾊﾟｲｱ`, `Gﾅｷﾞ`, `Iｾﾞｽ`, `Gﾏｼﾞｯｸ`); state 2 → 4 with Saias's army alone (`:36-44`); `Iｾﾞｽ` 2 at state 1 sets state 2 (`Iｾﾞｽ.ADV:2900`) |
| | `:54-75` → `L_00a5e/00bb1/00d04/00e55` `:433-688` | four ministers' armies, 1/2 each (none while state 2) |
| | `:76-187` → `L_00667/006f3/0077f/0080b` `:245-352` | the generals' scene roll `Y 4,7` (`Iｾﾞｽ` 30-36, each with one general's army if the generals it names live, all four on 7; voided to 10 at state 2, `:97-99`), then up to two `Y 4,4` single armies |
| | `L_00640` `:238`, `L_00897` `:353`, `L_00fa8` `:689`, `L_00fee` `:700`, `L_01160` `:760` | revive one subordinate, target walk, sum, picker, filler (no filter) |
| Tｹｲﾌﾞﾘｽ | `:1-187` | week S: `Gｹｲﾌﾞﾘｽ` 1 and straight to `L_004d1` (`:3-8`); the assault `Gｹｲﾌﾞﾘｽ` 2, the `Ai_*` reset, recovery `L_02662` ×24 on 22 commanders (Babolat (129) ×3), the weekly event chain (one event → skip to `L_004d1`, but `Gｼﾙｷｨ` 8 does not, NAT-26) |
| | `L_004d1` `:188-325` | dispatch: the guards, the generals' introductions and units, Piar, Kayblis main army, `Gｹｲﾌﾞﾘｽ` 3, the Assault Leaders, Monster General (86)'s name back |
| | `L_00fd0` `:625`, `L_010af` `:670`, `L_0120e` `:729`, `L_01478` `:831`, `L_017e4` `:972`, `L_01b5e` `:1112`, `L_01db2` `:1209` | Grief-Valley guards, Piar, Babolat, Redeye, Kite, Galtia, Kesselring |
| | `L_02038` `:1329`, `L_021f2` `:1417`, `L_0227b` `:1439` | Kayblis main army, refill Monster Generals (commanders 86-89), target walk |
| | `L_00881` `:326`, `L_00c91` `:492`, `L_00faf` `:618` | monster generals (1st…8th Assault Leader, commanders 191-198) (north/south lists) |
| | `L_0241e` `:1509`, `L_02464` `:1520`, `L_025d6` `:1580`, `L_02662` `:1601` | sum, picker, filler (troops < 99 and class < 5 once slot 1 filled), recovery |
| | `L_026bf` `:1615-1694`, `L_02897` `:1695-1715`, `L_0291b` `:1716`, `L_029b2` `:1748` | capture by Kayblis + side effects (ministers' and generals' deaths); Zeth's story after a minister's death; Helman / Zeth collapse |
| Tﾙﾄﾞﾗ | `:1-21`, `L_00093` `:22`, `L_000f8` `:41`, `L_001d4` `:83`, `L_00255` `:105` | 5 attacks + 2 earthquakes; random target search |

### 2.3 Common template [C]

All files carry near-identical copies of three routines (only the filter differs):

```
Picker(Tmp0001..Tmp0009):                         # e.g. TJAPAN.ADV:214-294
  EParty1..4 := 0
  for k in 1..9: Cmdr_Cur := Tmp000k
     if Cmdr_Status ≠ 0: Filler()                 # fills first empty slot
     if k ≥ 4 and EParty4 ≠ 0: return
Filler(): if <filter>: return; put Cmdr_Cur into first empty EParty slot
TroopSum(): CallArg := Σ Cmdr_Troops of EParty1..4   # empty slot → Cmdr_Cur = 0 (record 0)
AttackWalk(list): for p in list: Prov_Cur := p; if Prov_Owner = 1: FIGT_B; return
                  (Kayblis: if Prov_Owner ≠ 8 → owner 1: FIGT_B, else Capture(); Helman/JAPAN: capture on a specific owner)
Capture(): centre map; Prov_Owner := nation; flag CG Tmp0021 := owner+819; M ops 4,3,0
```

| Filler filter | Files |
|---|---|
| none | Tﾊﾟｯﾄﾝ, Tｾﾞｽ, Tｾﾞｽ2, TJAPAN2, TCﾘｰｻﾞｽ2, Tｹｲﾌﾞﾘｽ2, `Cﾃｷﾄｼ L_0509c` |
| `Cmdr_Troops < 190` (any slot) | TCﾘｰｻﾞｽ `:180` |
| `Cmdr_Troops < 190` and slot 1 filled | TJAPAN `:275`, Tﾍﾙﾏﾝ `:656` |
| same, except at `Prov_Cur = 45` (Lang Bau) | Tﾍﾙﾏﾝ2 `:358` |
| `Cmdr_Troops < 99` and `Cmdr_Class < 5` and slot 1 filled | Tｹｲﾌﾞﾘｽ `:1581` |

### 2.4 Armies per nation (data a port can table) [C]

`Thr` = minimum `TroopSum` (`—` = only `EParty1 ≠ 0`, `none` = not checked); `RM` = `Bt_EnemyRetreatMode`.

| Army (gate) | Priority list | Thr | RM | Targets in order (`*` = special) |
|---|---|---|---|---|
| Rebels A (Ys (5) not player's) | Pegasus (3), Dodge (5), Melfeis (4) | 500 | 4 | South (4), Oku (3), North (2), Leazas Castle (1) → then always B |
| Rebels B | Ex (1), Haurein (2), Sakanaku (6), Jibul (7) | 500 | 4 | Baranchi (7), Wes (8), Leazas Castle (1); none found → **falls into C** (`TCﾘｰｻﾞｽ.ADV:75-80`) — only with Leazas Castle (1) lost, which ends the game |
| Rebels C | Ex (1), Haurein (2), Pegasus (3), Sakanaku (6), Melfeis (4), Dodge (5), Jibul (7) | 500 | 4 | Oaks (6), Ys (5) |
| JAPAN (`Y 4,5` = 1) | Nobunaga Oda (72), Kenichi Oda (73), Yamato Akechi (74), Denden Mori (78), Mori Harada (77), Katsuie (75), Isoroku Yamamoto (82), One-Shot Tarou (79), Noberu Maeda (80) | — & 500 | 3 | Nagasaki (55), Portugal (28), Toushin City (27)\*, M Land (25), Hanna (26)\*, Zeo (24)\*, Red (23)\*, Oaks (6), Oku (3), North (2), Ys (5), South (4), Leazas Castle (1) (`*` owner 3 → capture, ends turn) |
| JAPAN (2,3) / (4,5) | Katsuie (75) through Denden Mori (78) / One-Shot Tarou (79) through Isoroku Yamamoto (82) | — & 500 | 3 | same |
| Helman 1st (`F_Helman_War=1`, Lelyukov (22) alive, Lelyukov ≠ 1) | stronger of Jackal (24), Gadman (23), Lelyukov (22), Samisukan (25), Luke (29), Yorkdelta (30), Laumiere (27), Luc (28), Tainyar (26) and Laumiere (27), Luc (28), Luke (29), Yorkdelta (30), Jackal (24), Samisukan (25), Lelyukov (22), Gadman (23), Tainyar (26) (tie → 2nd) | none | 3 | Archgrad (36), Siberia (38), Golak (34), Log A (32), Log B (31), Volgo Z (30), Maunes (13)\*, North (2)\*, Leazas Castle (1) (Maunes (13): `Y 4,2` → `Gﾚﾘｭｰｺﾌ` 5 or Tainyar (26) alone; Maunes (13) / North (2) owner 8 → capture) |
| Helman 2nd (Aristoles ≠ 0, Aristoles (32) alive) | Yutos (37), Keigo (38), Ivan (34), Samon (35), Yasutoeh (36), Aristoles (32), Convert (33) | 1500 | 4 | Micrograd (44), Parnas (47), Sudori 17 (46), Pawn (40), Sudori 13 (41), Laborie (52) |
| Helman 3rd (Mineva ≠ 0, Mineva (39) alive) | Capricorn (184), Ortega (185), Serges (42), Booch (43), Mineva (39), Tami (40), Maha (41) | 1500 | 3 | Micrograd (44)\* (re-pick without Capricorn (184) / Ortega (185), not summed again — NAT-19; `Gﾐﾈﾊﾞ` 4 first while Maha (41) lives and `F_Pastel_State = 0`), Laurengrad (48), Pawn (40), Sudori 10 (42), Parnas (47), Rosesgrad (39), Archgrad (36) |
| Helman 4th (Nero ≠ 0 and (≠ 100 or Cream = 0)) | Sukakumi (49), Marquez (46), Hyam (47), Nero (44), Cream (45), Yang (48) | — | 4 | Pawn (40), Sudori 13 (41) (Nero 1,2 → +1; 3 → `Gﾈﾛ` 2) |
| Helman 4th, Nero = 6 | Hyam (47), Marquez (46), Sukakumi (49), Cream (45), Yang (48) | — & 1000 | 3 | Pawn (40), Sudori 13 (41) (the week left unmarked, NAT-18) |
| Helman 5th (Rolex (50) alive, Rolex ≥ 4) | Alexei (53), Rolex (50), Saulnia (52), Katyusha (51) | 500 | 4 | Rosesgrad (39), Micrograd (44), Pawn (40), Sudori 13 (41), Archgrad (36), Log B (31), Volgo Z (30), Laurengrad (48), Parnas (47), Sudori 17 (46) |
| Crusaders (Olhound ∉ {0,100}) | Olhound (60) alone, troops 500+rnd(500) | none | 1 | Path (43), Micrograd (44), Laurengrad (48), Parnas (47), Sudori 17 (46), Pawn (40), Sudori 13 (41), Sudori 10 (42), Rosesgrad (39), Archgrad (36) |
| Patton | Patton (54), Hubert (55), Freak (56), Hanty (57) (+Aristoles (32) if Aristoles > 9) | 500 | 4 | Sudori 17 (46), Sudori 13 (41), Pawn (40), Micrograd (44), Lang Bau (45) |
| Zeth minister ×4 (each `Y 4,2`, minister alive, `F_Zeth_State ≠ 2`) | Zamoor (165) through Annis (168) / Backslock (169) through Am (172) / Lawson (173) through Hiroko (176) / Kenichi (177) through Rue (180) | — | 4 | own lists (`Tｾﾞｽ.ADV:448-688`), pre-battle `Gﾁｽﾞｺ` 6 / `Gﾊﾟﾊﾟｲｱ` 9 / `Gﾅｷﾞ` 22 / `Gﾏｼﾞｯｸ` 24 |
| Zeth general Saias (113) / Uspira (117) / Kabachahn (121) / Alex (125) | 3 subordinates then the general (Dabinch (116), Maskloader (115), Reshie (114) / Minamin (120), Dan Cero (119), Starlighter (118) / Dancelove (124), The Bomber (123), Kyouko (122) / Shitareriha (128), Haseras (127), Sanneri (126)); one of the three revived first (`Y 4,3`, troops max/8) | `CallArg ≠ 0` | 4 | Chizuko's Tower (68), Papaya's Tower (70), Magic's Tower (66), Nagi's Tower (67), Nagarmaule (62), Old Zeth (63), Tape (61), Italia (60), Sabasaba (59), Adam's Fortress (58), Parapara Fortress (16), Rich (15), Maunes (13), North (2), Leazas Castle (1); none found → falls into Chizuko's army (NAT-20, dead in play) |
| Kayblis guards (Grief Valley (89) player-owned, `F_Kayblis_Moved=0`) | Lis Guard (186), troops 20+rnd(100) | none | 1 | Grief Valley (89), **repeated** until lost (`:669 @L_004d1`) |
| Babolat (129 alive, state ≠ 0) | Babolat (129) | — | 2 | Tower of Evil (78), Great Wall of Helman A (49), Ikoma (53), Laurengrad (48), Lang Bau (45), Micrograd (44), Sudori 10 (42), Rosesgrad (39), Siberia (38), Golak (34), Log A (32), Scale (12), North (2), Leazas Castle (1); pitfall prov → `Gﾊﾞﾎﾞﾗ` 4 instead |
| Redeye (130) | Truffle 77 (148), Moreinyan (149), Ouzawa (150), Redeye (130) | — | 2 | Grief Valley (89), Great Wall of Helman B (50), Laurengrad (48), Parnas (47), Sudori 17 (46), Sudori 13 (41), Pawn (40), Micrograd (44), Sudori 10 (42), Rosesgrad (39), Archgrad (36), Log B (31), Maunes (13) (Redeye (130) alone), North (2), Leazas Castle (1) |
| Kite (132) | Kite (132), Tobroiv (151), Deckmaster (152), Strength (153) | — | 2 | Maginot Stronghold (76), Marc (74), Papaya's Tower (70), Magic's Tower (66), Zeth Palace (69), Ardennes (73), Amber Castle (65), Nagi's Tower (67), Nagarmaule (62), Sabasaba (59), Adam's Fortress (58), Parapara Fortress (16), Rich (15), Poor (14), Rockearth (17), Ice (18), Diphtheria (19) |
| Galtia (131, `F_Galtia_DefectedToKayblis=0`) | Derandron (154), Nasport (155), Galtia (131) | — | 2 | Maginot Stronghold (76), Marc (74), Amber Castle (65), Old Zeth (63), Magic's Tower (66), Tape (61), Italia (60), Sabasaba (59), Adam's Fortress (58), Parapara Fortress (16), Rich (15), Maunes (13), North (2), Leazas Castle (1) |
| Kesselring (133) | Kesselring (133) | none | 1 | first non-Kayblis of Papaya's Tower (70), Tape (61), Diphtheria (19), Red (23), South (4), Leazas Castle (1) ("moves from" previous) |
| Piar (state ∉ {0,100}) | PG-7 (136), PG-9 (137), PG-X mk2 (138) | — | 2 | random province (§5) |
| Kayblis main (`F_Kayblis_War=2`) | Kayblis (135); then Silky (156) / Galtia (131) (`F_Galtia_DefectedToKayblis≠0`) / Satella (157) / Meglass (158) each + Monster Generals (86, 88, 89); then Monster Generals (87, 86, 88, 89) | none | 2 | Grief Valley (89), Great Wall of Helman B (50), Laurengrad (48), Parnas (47), Sudori 17 (46), Sudori 13 (41), Pawn (40), Micrograd (44), Sudori 10 (42), Rosesgrad (39), Archgrad (36), Log B (31), Maunes (13), North (2), Leazas Castle (1) |
| Monster general (1st…8th Assault Leader, 191..198) (Week > 100,110,…,160; cumulative) | the leader, Monster Generals (87, 88) refilled to `max/5 + RND(max/3)` whether it marches or not; a dead leader is revived at HP 1 and stays home; the leader alone at Maunes (13) on the north list | the leader's own troops ≥ 1000 after `+Recovery` | 1 | north list `:364-490` / south list `:530-617`, alternating |
| Rudra / Creator | Angel Knight (146), Angel Knight (147) (full troops, **status 0**) | none | 1 | random player province ×5 |

All Kayblis walks attack/capture the first province **not owned by Kayblis**; a non-player one
is captured without a battle (`L_026bf`). Helman (`L_00fc2`) and JAPAN (`L_00366`) capture only
Kayblis- / city-state-owned provinces at specific list positions.

### 2.5 `*2` defence pickers [C]

| File | Province → defenders | Notes |
|---|---|---|
| TCﾘｰｻﾞｽ2 | Baranchi (7), Wes (8) → B; North (2), Oku (3), South (4) → A; Ys (5), Oaks (6) → C (RM 2 / 1); else → B | A/B: sum < 100 → return with **stale RM**; only Oaks (6) gets walls (NAT-17) |
| TJAPAN2 | Nagasaki (55) → Katsuie (75) through Denden Mori (78) (sum < 10 → stale RM, else RM 1); Osaka (56) → full list RM 1; else the **strongest** of the three lists, sum < 500 → stale RM, else RM 4 | sets `F_Japan_State := 1` (`:4`) |
| Tﾍﾙﾏﾝ2 | Pawn (40) / Sudori 13 (41) / Cossack (37) → 4th; Rosesgrad (39), Siberia (38), Golak (34), Log A (32), Log B (31), Volgo Z (30), Maunes (13), North (2), Archgrad (36) → 1st (stronger order; Volgo Z (30) & Laumiere (27) alive → Laumiere (27) alone); Sudori 17 (46) through Laurengrad (48) → 2nd; Lang Bau (45) / Micrograd (44) → 3rd (+ militia Civilian Generals (58, 59) at Lang Bau (45)); Archgrad (36) & Lelyukov 1 → Lelyukov (22) alone, troops 10; Sudori 10 (42) → 5th; Ancient Ruins (35) → Ruberan (31) alone; else / leader dead → militia Civilian Generals (58, 59) (troops 100+rnd(800)) | `F_Helman_War = 2` → militia ("A Certain Country's People"); `Gｼｰﾗ` 2 may cancel via `Tmp0001 = 1` |
| Tｾﾞｽ2 | garrison Wall (94, 95) and Mage Leader (96) regenerated (on a repeated attack of the same province less `RND(troops / 2)` each); Adam's Fortress (58) / Tape (61) / Amber Castle (65) / Old Zeth (63) → that general's army (the general picked alone → Wall (94, 95) at 10 troops + general); Magic's Tower (66) / Nagi's Tower (67) / Chizuko's Tower (68) / Papaya's Tower (70) → ministers' armies after `Gﾏｼﾞｯｸ` 1 / `Gﾅｷﾞ` 5 / `Gﾁｽﾞｺ` 1 / `Gﾊﾟﾊﾟｲｱ` 3 (`Gﾏｼﾞｯｸ` 1 answering `Tmp0001 = 1` sends Alex's army, 128-125); else Wall (94, 95), Mage Leader (96) (Magic's Tower (66): Mage Leader (96) alone); Parapara Fortress (16) without walls | ministers' provinces need a unit with item 12 unless `F_ZethBarrier_State = 2` (formerly `VAR0063`), else `Bt_PlayerParty1 := 0` cancels (`:91-130`); first attack sets `F_Zeth_War := 1` + `Iｾﾞｽ` 1; two units or more set Papaya's trap (`F_Papaya_State` 1 → 2) |
| Tｹｲﾌﾞﾘｽ2 | monsters (Monster Generals 86-89) regenerated at full HP with `max/(4 + 2 hits) + RND(max/Tmp0226)` troops; stage 3 or capitals Tower of Evil (78) / Grief Valley (89) / Bone Forest (90) / Tearing Forest (91) → named garrisons (Blue House (86): Hikaru (160), Rika (161), Kaybwan (159); Red House (85): Wao (163), Kunihiko (164), Kaybnyan (162); Kesselring's Castle (87): Kesselring (133) + Monster Generals (87-89); Camilla's Castle (88): Camilla 91 + Monster General (88); Demon Lord's Castle (79): Monster Generals (181-183) + Kayblis (135); Kayblis's Castle (84): Medusa 134 + Monster Generals (87, 88); a dead leader → Monster Generals (86-89)); else Monster Generals (86, 87) (+88, 89 on `Y 4,2 = 1`) in the plains | `StoryStage 2 → 3` on Tower of Evil (78) / Grief Valley (89) / Bone Forest (90) / Tearing Forest (91) (`:38`, in play never: `Iｼﾚｲﾌﾞ` 6 sets stage 3 first); Tower of Evil (78) → `Gﾜｰｸﾞ` 1 replaces the battle while `N_Warg_Stage = 0` (`:92-96`); at stage 3 walls only at 79 and 84 (NAT-21) |

`Bt_TroopLimit` written by `*2` is dead: `FIGT_A.ADV:31-55` recomputes it from `Bt_WallBonus`,
which `Cﾃｷﾄｼ.ADV:317` zeroes before the call [C]. After the call `Cﾃｷﾄｼ` cancels on
`Bt_PlayerParty1 = 0` (`:381-386`), fires `Gﾊﾟﾊﾟｲｱ` 2 if `Tｾﾞｽ2` raised `F_Papaya_State` to 2
(`:388-393`), and occupies without a fight on `Bt_EnemyParty1 = 0` (`:395-417`) [C].

## 3. Call graph

| From | To (count of call sites) |
|---|---|
| `MAIN` | each base file once (7) |
| `Cﾃｷﾄｼ` | `TCﾘｰｻﾞｽ2`, `TJAPAN2`, `Tｾﾞｽ2`, `Tﾍﾙﾏﾝ2`, `Tｹｲﾌﾞﾘｽ2` (1 each) |
| `FIGT_B` | TCﾘｰｻﾞｽ 9, TJAPAN 13, Tﾍﾙﾏﾝ 46, Tﾊﾟｯﾄﾝ 5, Tｾﾞｽ 39, Tｹｲﾌﾞﾘｽ 9, Tﾙﾄﾞﾗ 1 (122 sites, never `FIGT_A`/`FIGT`) |
| `M.ADV` (map ops 0,1,3,4,10) | TJAPAN 10, Tｹｲﾌﾞﾘｽ 11, Tﾍﾙﾏﾝ 3, Tﾙﾄﾞﾗ 2 |
| `WAO_SUB` | via per-file window helpers (ops 0,1,111-113,311/312,321/322,331/332); 23 call sites per copy, many in dead copies |
| `OVER` (`&` jump) | TJAPAN `:2374` (ending 4) |
| Story `G*` | TCﾘｰｻﾞｽ: `Gｴｸｽ` 1. TJAPAN: `Gｺｳﾋﾒ` 4. Tﾍﾙﾏﾝ: `Gｽﾃｯｾﾙ` 5, `Gﾚﾘｭｰｺﾌ` 3, `Gｱﾘｽﾄﾚｽ` 2, `Gﾊﾟｯﾄﾝ` 2, `Gﾐﾈﾊﾞ` 2, `Gﾛﾚｯｸｽ` 2, `Gﾌﾚｲｱ`, `Gﾈﾛ`, `Gｵｰﾙﾊｳﾝﾄ` 1. Tﾊﾟｯﾄﾝ: `Gﾊﾟｯﾄﾝ` 4, `Gｱﾘｽﾄﾚｽ` 1. Tｾﾞｽ: `Gﾅｷﾞ` 11, `Gﾏｼﾞｯｸ` 9, `Gﾁｽﾞｺ` 8, `Gﾊﾟﾊﾟｲｱ` 8. Tｹｲﾌﾞﾘｽ: `Gｼﾙｷｨ` 8, `Gｶｲﾄ` 7, `Gﾊﾟｲｱｰﾙ` 6, `Gﾚｯﾄﾞｱｲ` 6, `Gﾒﾃﾞｨｳｻ` 5, `Gｹｯｾﾙ` 4, `Gﾊﾞﾎﾞﾗ` 3, `Gｶﾐｰﾗ` 3, `Gｹｲﾌﾞﾘｽ` 3, `Gｶﾞﾙﾃｨｱ` 2, `Gﾜｰｸﾞ` 2, `Gｹｲﾌﾞﾜﾝ`, `Gｹｲﾌﾞﾆｬﾝ`, `Gｻﾃﾗ`, `Gﾅｷﾞ`, `Gﾌﾘｰｸ` 1. Tﾙﾄﾞﾗ: `Gﾙﾄﾞﾗ` 1. `*2`: `Gｼｰﾗ` 2, `Gｽﾃｯｾﾙ`, `Gﾈﾛ`, `Gﾊﾟｯﾄﾝ`, `Gﾙｰﾍﾞﾗﾝ`, `Gﾁｽﾞｺ`, `Gﾅｷﾞ`, `Gﾊﾟﾊﾟｲｱ`, `Gﾏｼﾞｯｸ`, `Gｹｲﾌﾞﾘｽ`, `Gﾜｰｸﾞ` 1 |
| Story `I*` | TJAPAN: `IJAPAN` 2 (7, 8). Tｾﾞｽ: `Iｾﾞｽ` 12 (2-6, 30-36). Tｾﾞｽ2: `Iｾﾞｽ` 1 |

Hooks that **return values in scratch variables** (must be modelled by the hook interface):
`Gﾏｼﾞｯｸ` 1 → `Tmp0001` (`Tｾﾞｽ2.ADV:144-147`), `Gｼｰﾗ` 2 → `Tmp0001` (`Tﾍﾙﾏﾝ2.ADV:15-16`);
`Gﾚｯﾄﾞｱｲ` 8 may change `Cmdr_Status` of the current cursor (`Tｹｲﾌﾞﾘｽ.ADV:928-933`) [C/I].

## 4. State

| Group | Read | Written |
|---|---|---|
| cmdr (`Cmdr_Cur`, Status/HP/Troops/MaxTroops/MaxHP/Recovery/Class) | everywhere | revive/refill: Zeth `L_00640` (troops max/8), Kayblis Monster Generals 86-89 (max/5+rnd(max/3) or max+rnd(max/2) — **above max**), guards Lis Guard (186), Assault Leaders (191-198), militia Civilian Generals (58/59), Crusader Olhound (60), Rudra Angel Knights (146/147) (status **0**), Tainyar (commander 26) (`Tﾍﾙﾏﾝ:25-36`), Camilla (91) max 200→2000 (`Tｹｲﾌﾞﾘｽ2:173-177`); Kayblis recovery `L_02662`; Status := 0 for Zeth leaders on Kayblis capture (`:1650-1693`) |
| unit | `Unit_Status` of Rance (1), Barres (6), Rick (7), Cordoba (8), Ex (14), Gengorou (15), Eleanor (23) (TJAPAN), `Unit_Item` (Tｾﾞｽ2) | `Unit_Status := 0` (Gekkou assassination); `Unit_Cur := 101` (Tｾﾞｽ `:130`, see §7) |
| prov | Owner, MapX/Y, BgCg, BattleArea, Defense | `Prov_Owner` (captures), Rudra: Economy 0, Defense 0, BattleArea ×2, BgCg 1043 (`Tﾙﾄﾞﾗ:66-69`) |
| battle | `Bt_Result`, `Bt_RoundsLeft`, `Bt_PlayerParty1-4` | `Bt_EnemyParty1-4`, `Bt_EnemyRetreatMode`, `Bt_TroopLimit`/`Bt_WallBonus` (`*2`), `Bt_GarrisonProv/Hits` (`*2`), `Ai_{Babolat,Redeye,Kite,Galtia}Atk` (0/10/11) |
| economy | `Gold` | `Gold /2` or `/4*3` (Gekkou, `TJAPAN:405, 441`) |
| story | ~60 `F_*`/`N_*` flags | `F_LeazasRebels_State`, `N_Gekkou_Attempts`, `N_Medusa_Events`, `F_Piar_State`, `F_Redeye_State`, `F_Rolex_State`, `F_Aristoles_Helman2ndState`, `F_Nero_Helman4thState`, `F_Zeth_State`, `F_Zeth_War`, `F_Helman_War`, `F_Helman_FallCause`, `F_RagarlRitual_State`, `F_Papaya_State`, `VAR0063`, `F_Japan_State`, `F_Mineva_State`, `F_Lelyukov_State`, `StoryStage`, `N_Rudrasaum_Turns`, `Kesselring_LastProv` |
| event | `Ev_WeeklyEventDone` | `Ev_Entry`, `Ev_PhaseNation`, `Ev_WeeklyEventDone` (also "an army attacked" in Tﾍﾙﾏﾝ) |
| scratch | `Tmp0001-0003` = date on entry (`MAIN L_0191e`) | `Tmp0001-0009` (lists), `Tmp0011/0012` (Helman), `Tmp0021/0022`, `Tmp0226`, `Tmp0229`, `Tmp0690`, `CallArg`, map `MapTargetX/Y`, `MapCamX/Y` |
| strings | — | `MS 10` army name (all files); `MS 4` + `MM` cmdr 86 name "Monster General" (`Tｹｲﾌﾞﾘｽ:321-324`); `MM 8` province name |

Nothing on array page 4 [I: no page-4 variable appears in these files].

## 5. Randomness — 60 `Y 4` sites [C]

`Y 4, n` with n ≤ 1 returns n **without a draw** (`xsystem35 cmdy.c:144-148`): relevant to the
computed ranges (`Cmdr_MaxTroops / k`, `Cmdr_Troops / 2`).

| Site | n | Condition / use |
|---|---|---|
| TJAPAN `:10`, `:16` | 2, 2 | always; Kou events 4 / 3 |
| TJAPAN `:22` | 4 | always; 1 → no attack this week (+ `L_0327c` if no event yet) |
| TJAPAN `:29` | 5 | army group (§2.4) |
| TJAPAN `:296`, `:299` | 2, 3 | Gekkou active; second draw only if Rance `Unit_Status = 2`; 2 → Shinobu |
| TJAPAN `:357`, `:370`, `:490` | 2, 3, 6 | Gekkou: assassination vs theft; theft amount; target unit |
| TJAPAN `:2746` | 4 | `L_0327c`: `Gｺｳﾋﾒ` 1/2, `IJAPAN` 7/8 |
| Tﾍﾙﾏﾝ `:60` | 2 | always; Patton events |
| Tﾍﾙﾏﾝ `:172` | 2 | 1st army at Maunes (13): `Gﾚﾘｭｰｺﾌ` 5 vs Tainyar (commander 26) |
| Tﾍﾙﾏﾝ `:423`, `:428` | 2, 2 | cmdr 50 alive: Rolex state 0→1, 1→2 (each returns) |
| Tﾍﾙﾏﾝ `:530` | 500 | Crusader troops |
| Tｾﾞｽ `:55,60,65,70` | 2 ×4 | `F_Zeth_State ≠ 2`: each minister's army |
| Tｾﾞｽ `:96`, `:146`, `:167` | 7, 4, 4 | the generals' scene roll (always drawn, voided at state 2), then up to two single-army rolls (the second only if the first sent nobody) |
| Tｾﾞｽ `:195`, `:216` | 4, 4 | Zeth state 8/9, 10/11: minister scenes |
| Tｾﾞｽ `:247,274,301,328` | 3 | which subordinate to revive before a general's attack |
| Tｹｲﾌﾞﾘｽ `:70` | 5 | Medusa (134) alive: `N_Medusa_Events += 1` on 1 |
| Tｹｲﾌﾞﾘｽ `:91` | 3 | always (if reached); Camilla events 4/5 |
| Tｹｲﾌﾞﾘｽ `:181` | 5 | reached only if no event yet: Medusa 5 |
| Tｹｲﾌﾞﾘｽ `:339,344,505,510` | max/3 | monster-general escorts 87, 88 |
| Tｹｲﾌﾞﾘｽ `:629` | 100 | guards' troops, per repeat |
| Tｹｲﾌﾞﾘｽ `:673`, `:676` | 77, 10 | Piar target; on a non-player/Nothing (75) province 1/10 abort, else redraw — **unbounded loop** |
| Tｹｲﾌﾞﾘｽ `:949` | 2 | after Redeye's battle (Freak stage 5 event) |
| Tｹｲﾌﾞﾘｽ `:989`, `:992` | 3, 10 | Kite events 6/7; 10 replaces 3 while `F_Calory_State < 4` |
| Tｹｲﾌﾞﾘｽ `:1421-1436` | max/2 ×4 | refill Monster Generals (86-89), up to 5× per week |
| Tｹｲﾌﾞﾘｽ2 `:15-30` | max/`Tmp0226` ×4 | garrison regen every player attack (`Tmp0226` = 4, or 2+hits on a repeat) |
| Tｹｲﾌﾞﾘｽ2 `:45` | 2 | 2 or 4 defenders in the plains |
| Tｾﾞｽ2 `:22,30,38` | troops/2 ×3 | only when the same province is attacked again |
| Tﾍﾙﾏﾝ2 `:64,69`, `:173,178` | 800 | militia Civilian Generals (58, 59) |
| Tﾙﾄﾞﾗ `:113`, `:120` | 91, 2 | target search: ≤ 19 tries; `Turns > 5` adds the castle roll |
| Tﾙﾄﾞﾗ `:85` | 4 | **dead** (after `\0:` at `:84`) |

Draws are interleaved with nested `FIGT_B` battles (each with its own draws, see
[../battle/03-rng-order.md](../battle/03-rng-order.md)), so a nation turn is one long ordered stream.

## 6. Input and display

- **No player choice in these files** [C]. All input happens inside `FIGT_B` (intercept / scout /
  abandon menu, `Sﾌﾞﾀｲ`) and inside story hooks.
- Key waits (`WAO_SUB` op 1): TJAPAN 338 (all in the Gekkou/Shinobu scenes), Tｹｲﾌﾞﾘｽ 9
  (captures, Kesselring move, guard speech), Tﾍﾙﾏﾝ 3 (capture from Kayblis), Tﾙﾄﾞﾗ 3 (earthquake).
- Display: map centring (`M` ops 1, 10), province flag redraw (ops 4, 3, 0) on capture,
  "X appeared around Y / VS / occupied" boxes (`TJAPAN:159-191`, `Tｹｲﾌﾞﾘｽ:1625-1637`),
  portraits by `CallArg` (e.g. 421 Gekkou, 422 Shinobu, 415/466 Kayblis), `G 806/133` CG in the
  Shinobu scene (`TJAPAN:2207-2208`). All of it can be redesigned freely; the map ops leave
  scratch (`Tmp0021-0030`, camera) that the battle port already models (`ScriptContext.Map.cs`).

## 7. Rules vs presentation; quirks

The rule skeleton separates cleanly: drawing is confined to window-helper calls, `M.ADV` and
text; the only display→state coupling is `M`'s scratch. TJAPAN's scenes are rules-light text
(6 state effects in 2450 lines). Quirks to reproduce (all [C] unless marked):

1. **Collapse loops return after one province**: `L_0291b`/`L_029b2` end their loop body with
   `\0:` (`Tｹｲﾌﾞﾘｽ.ADV:1742`, `:1773`) — only Shangri-La (29, Helman) / Adam's Fortress (58, Zeth)
   can flip to Kayblis; `F_Helman_War`/`F_Zeth_War := 2` still set. `Prov_Cur` is left at
   Shangri-La (29) / Adam's Fortress (58). Helman's flips nothing in play (Shangri-La is never
   Helman's), and the capture's own test of Adam's Fortress then kills Saias (113) — NAT-01.
2. **Guards repeat**: `L_00fd0` jumps back to `L_004d1` (`:669`), which re-attacks Grief Valley
   (89) while it stays player-owned and `F_Kayblis_Moved = 0` — one battle per win, new `Y 4,100`
   each time [I: until lost].
3. **Second attack on timeout**: Babolat/Redeye/Kite/Galtia re-attack once if `Bt_RoundsLeft = 0`
   and `Bt_Result = 2` (`:826`, `:945`, `:1087`, `:1200`). The `Ai_*Atk` reset (`:13-16`) is
   skipped when `F_DemonWar_Stage < 20` (`:3-8` jumps over it) — stale 10/11 carry over.
4. **Babolat recovers three times** (`Tｹｲﾌﾞﾘｽ.ADV:27-30`, `L_02662` ×3 on Babolat (commander 129)).
5. **Rebels B falls into C** (`TCﾘｰｻﾞｽ.ADV:75-80`): up to two attacks per week; A always continues to B.
   B's last target is Leazas Castle (1), whose loss ends the game, so the fall into C needs a castle
   the player no longer holds (NAT-06).
6. **Stale cursor**: `Tｾﾞｽ.ADV:130` sets `Unit_Cur := 101` but tests `Cmdr_Status` of Alex
   (commander 125) (cursor from `:92`); probably meant `Cmdr_Cur` (NAT-07).
7. **Inverted check** `Ev_WeeklyEventDone \ 0` for Patton events 2/12 (`Tﾍﾙﾏﾝ.ADV:61`, `:67`):
   they fire only if another event already fired this week.
8. **Precedence**: `Tｹｲﾌﾞﾘｽ2.ADV:38` — only Tearing Forest (91) is gated by `StoryStage = 2`.
   (`Tﾍﾙﾏﾝ.ADV:84` is parenthesised and reads as meant — checked 2026-10-10.)
9. **Stale `Bt_EnemyRetreatMode`** when `*2` returns early (`TCﾘｰｻﾞｽ2:31-32/48-49`,
   `TJAPAN2:26-27`, `:124-125`): the defenders still fight with last battle's mode.
10. **Above-max troops**: Kayblis refill `max + rnd(max/2)` (`:1421-1437`); the Camilla 200→2000 patch.
11. **Rudra commanders enter with `Cmdr_Status = 0`** (`Tﾙﾄﾞﾗ.ADV:31,35`); earthquake jitter
    `L_001d4` is dead (`\0:` first, `:84`), so `Prov_MapX/Y` never move.
12. **Empty party slots** read commander record 0 in every `TroopSum` [I: aliases record 220 of
    the previous field with stride 220, probably 0].
13. **Dead code**: unlabelled window-helper copies (`TCﾘｰｻﾞｽ:200-268`, `Tｹｲﾌﾞﾘｽ2:279-347`,
    `Tﾙﾄﾞﾗ:132-143`), second ops of two-op helpers (e.g. `TJAPAN:2772-2774`), unlabelled troop sums
    (`Tｹｲﾌﾞﾘｽ2:191-200`, `Tｾﾞｽ2:314-323`) (`Tｾﾞｽ.ADV:97-99`, once listed here, is live: `Iｾﾞｽ` 2
    at `:45-47` sets state 2 after the test at `:36` — NAT-16), `Tﾍﾙﾏﾝ2.ADV:31-32` (owner 5 never reaches it), `Tｹｲﾌﾞﾘｽ.ADV:642-645`, sums whose `CallArg`
    is never tested (Babolat/Redeye/Kite/Galtia/Kesselring/Kayblis), `Tﾙﾄﾞﾗ2`, `Bt_TroopLimit` in `*2`.
14. **Helman 1st army** has no troop or empty-party check before `FIGT_B` (`Tﾍﾙﾏﾝ.ADV:99-207`).
15. **Gekkou**: assassination kills only a target with `Unit_Status = 2` (deployed this week);
    Shinobu with Rance at status 2 → `EndingId 4`, game over (`TJAPAN:2111-2113`, `:2373-2374`);
    only Shinobu attempts count toward 5 (`:1961`).
16. **`Y 4` returning n+1** (battle Q20) would leave `Tmp0001-0009` = date values in TJAPAN (`:29`
    → 6 matches no group) and pick nonexistent province 92 / Tower of Evil (78) (Rudra / Piar).

## 8. Porting proposal

Code home: `src/Kichikuou.Core/Strategy/Nations/` (partial class `NationTurns`), same conventions
as the battle; `FIGT_B` through the existing `Battle.DefendProvinceAsync`; story files through
`IStoryHooks` (with scratch return values, §3); `M.ADV`/`WAO_SUB` state effects reused from
`ScriptContext.Map.cs` / `ScriptContext.Wao.cs` (move them to a shared helper).

As ported (N1 part 1, 2026-10-10; [README](README.md), the N1 row): the template is a base class,
`Strategy/NationFile` on `CommandFile`, the filter a method each file overrides; every file is a
class of its own in `Strategy/` (`RebelTurn` = `TCﾘｰｻﾞｽ`, `RebelDefenders` = `TCﾘｰｻﾞｽ2`,
`HelmanTurn` = `Tﾍﾙﾏﾝ`, `HelmanDefenders` = `Tﾍﾙﾏﾝ2`, `PattonTurn` = `Tﾊﾟｯﾄﾝ`, `ZethTurn` = `Tｾﾞｽ`, `ZethDefenders` = `Tｾﾞｽ2`, `KayblisDefenders` = `Tｹｲﾌﾞﾘｽ2`), its lists written in the script's order at
the place the script sets them; a capture stays with its file (`HelmanTurn.CaptureAsync` is
`Tﾍﾙﾏﾝ`'s `L_00fc2`).

| Part | Content | Script lines (rule) | Depends on |
|---|---|---|---|
| **N1 template + small nations** | §2.3 helpers as parameterised methods (filter enum), `CaptureProvince`, TCﾘｰｻﾞｽ, Tﾊﾟｯﾄﾝ, Tﾙﾄﾞﾗ, all five `*2` files (+ `Cﾃｷﾄｼ L_04f2a` Patton list) | ~1.6k (≈1.1k rule) | battle port; Cﾃｷﾄｼ owner for the call site |
| **N2 Helman + Zeth + JAPAN rules** | Tﾍﾙﾏﾝ, Tｾﾞｽ, TJAPAN `:1-294` + `:2745-2767`, Gekkou/Shinobu as rule skeleton (draws, `Gold`, `Unit_Status`, counter, game over) with the scenes as `ScriptRef` text events | ~1.9k rule + 2.45k text | N1 |
| **N3 Kayblis** | Tｹｲﾌﾞﾘｽ entirely (events chain, 8 sub-armies, main army, monster generals, captures with nation collapse) | ~1.7k | N1 |

Per-nation data (lists, thresholds, retreat modes, target lists of §2.4) becomes static tables
so the hybrid branch can swap AIs; control flow with fall-throughs (Rebels B→C, guards loop,
second attacks) stays explicit code. The TJAPAN scenes could alternatively go through the event
converter if it handles `Unit_Status`/`Gold` writes and the `&` jump.

Golden harness: add the seven base files as **roots** (called from `MAIN`) with `FIGT*`,
`死亡捕獲`, `M`, `WAO_SUB`, `WAO_MAP` inner and `FIGT_SEL` pick; story files and `Sﾌﾞﾀｲ` stay
hooks. A capture is then one nation turn with zero or more nested battles; the replay runs the
battle port inside it. `*2` files as roots called from `Cﾃｷﾄｼ` (no input; draws and hooks only),
or later the whole `Cﾃｷﾄｼ` attack. Captures needed: each nation with and without an attack;
JAPAN idle/Gekkou theft/assassination; Rebels two attacks; Zeth combined attack; Kayblis
timeout second attack, AI-vs-AI capture incl. Helman capital Lang Bau (45) (quirk 1), guards repeat,
monster generals (needs Week > 100 — save editing), Piar loop; Rudra; each `*2` incl. a
repeated Kayblis/Zeth garrison attack and the item-12 cancel. The `&#OVER` jump ends a capture.

## 9. Open questions

1. Quirk 1: does the original really flip only Shangri-La (29) / Adam's Fortress (58) on a Kayblis
   collapse? A capture of Kayblis taking Lang Bau (45) settles it (`Tｹｲﾌﾞﾘｽ.ADV:1729-1747`). (Helman's flips nothing in
   play, Shangri-La (29) never being Helman's; Zeth's also kills Saias (113): NAT-01.)
2. Can the guards loop (`:625-669`) run more than twice in practice, and what ends it besides losing?
3. Who sets `F_Kayblis_Moved` (only `Gｹｲﾌﾞﾘｽ.ADV:659`) and `VAR0637` (`Gｹｲﾌﾞﾘｽ.ADV:368`) — meaning of 637.
   **Answered (2026-10-11, porting `Tｹｲﾌﾞﾘｽ`'s frame):** `F_Kayblis_Moved` is set by `Gｹｲﾌﾞﾘｽ` 3, which the turn
   plays once the player holds Silky's Castle (81); VAR0637 is `F_Galtia_DefectedToKayblis`, set by
   `Gｹｲﾌﾞﾘｽ` 7 when Galtia (unit 41) defects in a defence against Kayblis's army (stage-3-sweep).
4. Content of commander record 0 (quirk 12) — read it from a memory dump.
5. Effect of `Cmdr_Status = 0` defenders (Rudra's Angel Knights 146/147) inside FIGT — compare with dungeon
   captures of a status-0 commander.
6. Units Barres (6), Rick (7), Cordoba (8), Ex (14), Gengorou (15), Eleanor (23) — who they are, and whether `Unit_Status = 2` really means
   "deployed this week" at JAPAN-phase time (week refresh order in `MAIN`). **Answered (2026-10-11, porting
   `TJAPAN:295-2744`):** the names are `SET_CHR`'s `MS 1`; the status is reset at `MAIN.ADV:151`, before the
   player phase (`:213`), and JAPAN moves after it (`:399`), so 2 is "deployed this week" (NAT-14).
7. Who first sets `F_Helman_War = 1` and `F_Zeth_War` paths — `Gｽﾃｯｾﾙ` 1 / `Iｾﾞｽ` 1 hooks (unverified).
8. `Tｾﾞｽ.ADV:130` intent (quirk 6) and `Tﾍﾙﾏﾝ.ADV:61/67` (quirk 7) — reproduce as is; confirm by capture.
9. Is the Piar redraw loop (`Tｹｲﾌﾞﾘｽ.ADV:673-681`) guaranteed to end quickly when the player
   owns almost nothing (1/10 abort per miss) — yes probabilistically, but the draw count is unbounded.
