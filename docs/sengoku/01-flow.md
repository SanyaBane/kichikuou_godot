# Sengoku Rance battle survey, part 1: the battle's flow and structure

This part covers how a Sengoku Rance (AliceSoft, System 4; the MangaGamer English release)
army battle is set up, how turns are ordered, what the time gauge and the 戦果 (advantage) bar
measure, how a battle ends and what follows it, and how the enemy AI picks its moves. It is one of
four reference surveys for **our own battle screen "in the manner of Sengoku Rance"**; it is not a
rule set for the port — Kichikuou's rules stay unchanged. The other parts:
[02-resolution.md](02-resolution.md) (units and damage), [03-skills.md](03-skills.md) (commands,
skills, states) and [04-screen.md](04-screen.md) (the screen and its input).

## Conventions (shared by the four parts)

- **Sources.** Every reference is `<file>:<line>`, relative to `ain\files\` under the folder named by
  the local key `SENGOKU_RANCE_EXTRACT_DIR` (`local.env`), e.g. `あべ/C_合戦.JAF.jam:229`: one
  alice-tools `.jam` file per original source. `code.jam`, `text.txt`, `structures.txt` and
  `globals.txt` (the whole dump, the strings, the struct and global names) sit beside `files\` in
  `ain\`. The line is that of the statement (or of the function's `FUNC`); a bare `:123` after a
  reference is another line of the same file. **[I]** = inferred, not read in the code.
- **Places.** Each side has six **places, numbered 1-6**: places **1-3 are the front row** (前衛)
  and **4-6 the back row** (後衛); place p+3 stands behind place p. The code has two other numberings
  of the same thing, given where a reference needs them: the place index **i = place − 1** (0-5;
  `isMikataMae` scans i < 3) and the battle **slot = 10 + place** for the player (11-16) and
  **20 + place** for the enemy (21-26) — the slot is the "busho number" the battle code passes
  around, and `< 21` means the player's side.
- **Row and line.** "Row" always means the front or the back row. On the screen each row is a
  vertical column of three cards (see 1.3 and 04-screen.md §6.1). A **line** (列, `戦闘_敵列`) is a
  place and the one behind it (p and p+3) — one horizontal pair of cards.
- **Time and advantage.** The central vertical gauge is **battle time** (24 actions, shared by both
  sides, §3.1); the horizontal bar at the top is **戦果, the advantage** (share of the casualties
  inflicted plus flat bonuses, §3.2). Nothing ends the battle when the bar reaches an end.
- **Battle kinds.** 合戦, the army battle (`◆戦闘タイプ == 0`), is the one this survey is about; the
  solo (individual) battles of events and dungeons are noted in passing only.
- Oddities of Sengoku Rance's own code found by the survey are listed as **Sengoku Rance
  oddities**; they are not part of the project's quirk register (docs/quirks.md), which is about
  Kichikuou.

## 0. Vocabulary and data

| Thing | Where | Meaning |
|---|---|---|
| `gs[0][11..16]` | `多田/関数／本編.jaf.jam:35009` キャラ参加登録 | the player's (Oda's) six battle slots; copies of the commanders (`sキャラ`) |
| `gs[0][21..26]` | `多田/関数／本編.jaf.jam:36367` 敵ＦＰ登録 | the enemy's six battle slots |
| "busho number" in battle | everywhere | the slot index 11..16 / 21..26; `< 21` means the player's side (`あべ/C_合戦.JAF.jam:64`, `あべ/T_BATPLS.JAF.jam:2819`) |
| `兵数` | sキャラ.21 | troops = the unit's HP |
| `技数` / `現在技数` | sキャラ.29 / .30 | action points: maximum / remaining this battle |
| `早さ` | sキャラ.35 | speed stat (0..9+) |
| `ＦＦ` | sキャラ.69 | battle state: 0 none, 1 charging (ため), 2 confused, 3 defending, 21/22/23 guard stances, 99 destroyed |
| `◆被害総数１/２` | globals | casualties inflicted by Oda / by the enemy (the 戦果 basis) |
| `◆戦果補正１/２` | globals | flat 戦果 bonuses of each side |
| `◆合戦補正１/２` | globals | "Battlefield Adjustment +N%", a damage multiplier (02-resolution.md §6) |
| `◆合戦ターン` | globals | actions elapsed; time left = 24 − it (a separate counter `◆経過時間` is raised by `合戦／行動前処理`, `多田/関数／本編.jaf.jam:27723`; the gauge does not read it) |
| `順番番号[] / 順番時間[]` | globals 0x76/0x77 | the action queue: slot number and remaining wait, sorted |

Two battle kinds share the code: **合戦** (army battle, `◆戦闘タイプ == 0`, the one relevant to a
Kichikuou port) and **solo battle** (`◆戦闘タイプ == 1` event battles and dungeon battles,
`変数_ソロ戦闘`, `あべ/C_変数.jaf.jam:132-160`): level-based HP (`Ｓ体力`), reserves, retreat command,
enemies with unlimited action points. Solo specifics are noted in passing only.

## 1. Set-up

### 1.1 Who fights where (strategic side)

- `合戦メイン(フラグ, ii2, iim)` (`多田/関数／本編.jaf.jam:11163`) is the strategic entry. `フラグ` = `◆攻防タイプ`:
  **1 = Oda attacks, 2 = Oda defends** (3 = scripted event battle, `多田/関数／本編.jaf.jam:10931`, `:11057`).
- A province has `支配土地` lands (`s国.3`, up to `支配土地最大`) and per land a facility
  `施設[]` and a stronghold flag `要害[]`. The battlefield type `◆戦場タイプ` is the facility of
  the land being fought over (`多田/関数／本編.jaf.jam:11219-11335`): 0 plain, 1 fort 砦, 2 mansion 屋敷, 3 castle 城,
  4 stronghold 要害, 7 dungeon 迷宮 (log names `多田/関数／本編.jaf.jam:14944-14987`).
- Order: enemy roster first (`敵ＦＰ登録`), then the player's participant screen
  (`Ｐ武将／参加選択`), then an event hook `ＩＶ合戦前`, then the battle `合戦処理` → `Ｐ合戦処理`
  (`多田/関数／本編.jaf.jam:11340-12266`). If the enemy cannot field anyone the player wins without a fight
  (`▲不戦勝 = 1`, `多田/関数／本編.jaf.jam:11373-11380`, `:12083-12110`); if the player cancels a defence the
  defence is lost (`▲不戦勝 = 2`, `多田/関数／本編.jaf.jam:12069-12164`).

### 1.2 Sides and unit count

- **6 slots per side, at most 6 units each** (`あべ/T_BATPLS.JAF.jam:86-264`, loops `i < 6`).
- **Player:** chooses the participants and their positions on the participation screen
  (`あべ/C_MAPCTL.JAF.jam:348-546` → `tagSansenList`, drag & drop, `あべ/T_SANSEN.JAF.jam:1002-1123`). Each commander's
  chosen position `作戦参加` (1..6) becomes slot `10 + position` (`多田/関数／本編.jaf.jam:35072-35085`), so
  **positions 1-3 = front row, 4-6 = back row** (see 1.3). Participants are marked as having
  acted this strategic turn (`行動 = 1`, `多田/関数／本編.jaf.jam:35057`).
- **Scouting (pre-battle option):** on the participation screen the player may send a scout
  (`Ｐ武将／参加選択／斥候`, `あべ/C_MAPCTL.JAF.jam:561-624`); on success the enemy roster is shown
  (`n斥候モード = 2`) and the scout difficulty is added to Oda's battlefield adjustment
  (`g_n斥候難易度` → `合戦_初期化`, `あべ/C_合戦.JAF.jam:122-132`).
- **Enemy roster** (`敵ＦＰ登録`, `多田/関数／本編.jaf.jam:36367-44050`): many scripted fixed rosters (castles,
  stronghold lands, story battles, e.g. `多田/関数／本編.jaf.jam:36858-38334`); otherwise:
  - cap `▲参加数上限` = 3 (turn ≤ 10), 4 (≤ 20), 5 (≤ 30), 4 + RAND(2) (later), **6 when
    desperate** (`必死`: attacking a province on its last land, `国.支配土地 == 1`)
    (`多田/関数／本編.jaf.jam:36457-36514`, `多田/関数／本編.jaf.jam:11348-11367`);
  - optionally (1 in 4) a template composition by troop types, slot by slot
    (`敵ＦＰ定番編成(ii2, t21..t26)`, `多田/関数／本編.jaf.jam:42841-43387`, `:44064`);
  - then up to 4 passes fill the empty slots with random commanders of the enemy force that have
    not acted, needing troops ≥ 98 / 78 / 58 / 38 % of max in passes 1-4 (10 % in passes 3-4
    when desperate); **the front row (slots 21-23) refuses Miko (9), Onmyouji (8), Tactician (13),
    Archer (5); the back row (slots 24-26) refuses Samurai (1) and Musketeer (4)** (`多田/関数／本編.jaf.jam:43447-43721`);
  - an invader with fewer than 3 units withdraws (`多田/関数／本編.jaf.jam:44006-44034`).

### 1.3 Placement: a front and a back row of three

`tagBattlePlace` (`あべ/T_BATPLS.JAF.jam`, the largest file) is the **battlefield model and its card
view**: the place table, card positions, front/back tests, the player's action selection, target
fallbacks, the front-row swap, the result handover. Place p (index i = p − 1) of a side is slot
`10 + p` / `20 + p`, i.e. `11+i` / `21+i` in the code (`あべ/T_BATPLS.JAF.jam:113-120`).

| Place | Index i | Screen column (`calcBushoPosX`) | Screen row (`calcBushoPosY`) |
|---|---|---|---|
| 1, 2, 3 | 0, 1, 2 | col 0 = **front row** (`前衛`, Mae) | 0, 1, 2 |
| 4, 5, 6 | 3, 4, 5 | col 1 = **back row** (`後衛`, Ushiro) | 0, 1, 2 |

- Player card x = 182 − 180·col (front at 182, back at 2); enemy x = 440 + 180·col (front at
  440, back at 620); y = 322 + 82·row (`あべ/T_BATPLS.JAF.jam:3618-3647`). The two front rows face
  each other across the screen centre (04-screen.md §6.1).
- Front = places 1-3, back = 4-6 (`isMikataMae`/`isTekiMae` scan indices 0-2,
  `あべ/T_BATPLS.JAF.jam:2504-2565`, `:2678-2739`).
- A **line** attack (`列`, target types 5/6) hits the target plus the unit **in the same line of
  the other row** (place p ↔ p±3) (`あべ/C_BATTLE.JAF.jam:604-702`, `:713-811`).
- **Front-row collapse:** before every turn, if a side has no living unit in its front row, its
  front and back rows swap (`swap(place[i], place[i+3])`, i = 0..2) with an animation
  (`tagBattlePlace@change`, `あべ/T_BATPLS.JAF.jam:1778-2159`; called by `戦闘_入れ替え` from
  `合戦_状態補正`, `あべ/C_合戦.JAF.jam:229`). Only a fully dead front triggers it; no partial moves.

### 1.4 Battle initialisation (`合戦／初期化`, `多田/関数／本編.jaf.jam:15412-17947`)

1. `◆合戦ターン = GF[115]` (0 normally; an event time limit otherwise) (`:15433-15446`).
2. **Gauge basis:** `◆被害総数１ = ◆被害総数２ = (Oda troops + enemy troops) / 10`
   (`:15465-15479`), so the 戦果 gauge starts at 50 : 50. Bonuses, adjustments reset.
3. Queue cleared: `順番番号[1..14] = 0`, `順番時間[1..14] = 999` (`:15585-15614`).
4. Each slot: `ＧＳ初期化／合戦部分` (`多田/関数／メイン.jaf.jam:7205-7345`): **action points refilled
   (`現在技数 = 技数`)**, states, buffs, guard, shikigami cleared. (Solo battles instead carry
   `迷宮技数` over, `多田/関数／本編.jaf.jam:15754-15768`.)
5. Battlefield bonuses (side that defends the facility):

   | Field | 戦果 bonus to defender | 合戦補正 (damage %) to defender |
   |---|---|---|
   | Castle (3) | +40 (`:15887`, `:15903`) | 20 |
   | Mansion (2) | +20 (`:15925`, `:15941`) | 10 (50 for one special case, `:15953-15978`) |
   | Fort (1) | +10 (`:15995`, `:16005`) | – |

   `国.合戦準備` (prepared province) adds to Oda's 合戦補正 and is consumed (`:16013-16053`).
   `合戦補正の補正` then nets the two adjustments (only the difference stays, on the larger side)
   and caps each at 50 (`多田/関数／本編.jaf.jam:33334-33491`).
6. Per-unit start effects (skills/items; [03-skills.md](03-skills.md) §2.4 and §7 have the
   details): Ashigaru (兵種 3) start in **Ally Guard** stance (`ＦＦ = 21`, guard chance 50;
   skill 127 → 120) (`:16121-16168`); item 68 or skill 136 **shorten the battle by 5 actions**
   (`◆合戦ターン += 5`, `:16181-16196`, `:16774-16789`); skill 137 +10 戦果; skills 103
   "Goddess of War's Majesty" / 104 "Empress's Majesty" give +5/+10 戦果 and remove 1/2 action
   points from every enemy unit, a result of exactly 0 being set back to 1 (so 104 can leave a
   1-point unit at −1, 03-skills.md §8) (`:16802-17117`); Tacticians (13) hand out random stat
   buffs (`:17359-17651`).
7. **Initial action order** (`:17661-17947`), see 2.2.

## 2. Turn structure: a charge-time queue, not rounds

### 2.1 Main loop (`Ｐ合戦処理`, `あべ/C_合戦.JAF.jam:11-95`)

```
合戦_初期化; 戦闘_開始 (screen, card slide-in, queue strip)
loop:
    nRet = 合戦_状態補正(フラグ)          # end checks + front swap (section 4)
    if nRet > 0: break
    順番_時間更新()                        # advance time until someone is ready
    actor = 順番番号[1]                   # head of the queue
    wait = actor < 21 ? 合戦_味方行動(actor) : 合戦_敵行動(actor)
    戦闘_順番登録(actor, 速度補正値(actor, wait))   # re-queue
戦闘_終了(nRet)                           # result screen, casualties
```

There are **no rounds and no side alternation**. Every unit has its own countdown; whoever reaches
zero acts; the same unit may act several times before a slow one acts once.

### 2.2 The queue (`あべ/C_順番.jaf.jam`)

- `順番_時間更新` (`あべ/C_順番.jaf.jam:80-133`): while the head's time ≠ 0, decrement the time of every
  occupied entry by 1. (A tick is abstract; nothing is shown per tick.)
- `順番_登録(unit, delay)` (`あべ/C_順番.jaf.jam:143-225`): sorted insert **before the first entry whose time is
  greater than `delay`** — ties keep first-come order.
- `戦闘_順番登録` (`あべ/C_BATTLE.JAF.jam:360-388`): destroyed units are not re-queued; the unit's old entry is
  deleted; **a unit with 0 action points left is removed from the queue and the strip and never
  acts again** (it stays on the field as a target); otherwise it is inserted and shown.
- **Delay formula** `速度補正値(unit, x)` (`多田/関数／本編.jaf.jam:26933-27404`):

  `delay = base(兵種) + speedAdj(早さ) − (4 if 早さ付与 ≠ 0) + (6 if 予備１ == 1) + x`

  | 兵種 (troop type) | base |
  |---|---|
  | Ninja 6 | 6 |
  | Musketeer 4 | 8 |
  | Cavalry 2 | 10 |
  | Ashigaru 3; monster types 27/28/29/31 | 11 |
  | Samurai 1 | 12 |
  | Knight 10, Onmyouji 8 | 14 |
  | Cannoneer 12, Archer 5, all others (default) | 15 |
  | Miko 9 | 16 |
  | Mage 11 | 17 |
  | Sohei 7, Tactician 13 | 20 |
  | Peasant 24 | 30 |

  | 早さ | 0 | 1 | 2 | 3 | 4 | 5 | 6 | 7 | 8 | >8 |
  |---|---|---|---|---|---|---|---|---|---|---|
  | speedAdj | +8 | +6 | +4 | +2 | 0 | −2 | −4 | −6 | −8 | −10 |

  `x` after acting = the action's wait `行動待ち時間` (`多田/▲技能データ.jaf.jam`, tabulated in
  03-skills.md §7; values 2..50, mostly
  15-20; Wait = 25) — so **heavier actions push the unit further back**. `予備１ = 1` is set on a
  unit revived by "Action Transfer" (`多田/関数／本編.jaf.jam:28425`, `:28554`).
- **Initial order** (`多田/関数／本編.jaf.jam:17661-17947`): 30 draws pick a random slot (side RAND(2), slot
  RAND(6)); a drawn, unregistered, occupied slot is queued with `速度補正値(slot, RAND(4) + 10)`
  (`+0` instead of `+10` with skill 124, `+4` for Onmyouji). Units never drawn are queued
  afterwards with `速度補正値(slot, RAND(4))`, i.e. about **10 ticks earlier** — a ≈7 % chance
  per unit ((11/12)^30). **[I]** looks like an unintended head start (a Sengoku Rance oddity, §6).
  `RAND(n)` is 1..n (`System/SACT/MES/sact_std.jaf.jam:153`); `乱数(n)` is also 1..n after
  discarding 0-99 draws (`あべ/S_TOOL.JAF.jam:87-126`).
- Other queue entries: a revived unit (`戦闘_復帰`, `あべ/C_BATTLE.JAF.jam:453-469`) and a unit brought in
  (`戦闘_補充`, `:437-447`) are queued with `速度補正値(unit, 0)`.

### 2.3 The strip of names = the action order

`tagBattleSequence` (`あべ/T_BATSEQ.JAF.jam`; its look and animations: 04-screen.md §4) builds one flag per queue entry from `順番_番号(1..)`
(`あべ/T_BATSEQ.JAF.jam:27-81`), inserts/removes flags as `戦闘_順番登録` re-queues (`tagBattle@entry`,
`あべ/T_BATTLE.JAF.jam:546-564` → `insert(順番_検索(no), no)`, `あべ/T_BATSEQ.JAF.jam:164-278`). Up to 16 flags
(`code.jam:1149756`, `tagBattleSequence@2` allocates 16), x = 100·index, y = 289
(`あべ/T_BATSEQ.JAF.jam:2193-2218`), so 8 are on screen. Flags show the commander's name only
(`あべ/T_BATFLG.JAF.jam`; `順番_時間` is never called), so **the order is visible, the waits are
not**. Leftmost = next to act.

### 2.4 One unit's action (player side, `合戦_味方行動`, `あべ/C_合戦.JAF.jam:242-366`)

1. No action points left → skip, delay 40 (normally unreachable, see 2.2).
2. `合戦_行動開始` (`あべ/C_合戦.JAF.jam:505-529`): if the unit is **charging** (`ＦＦ == 1`) the stored action
   `ＦＦ２` fires automatically (no choice); otherwise clear states and **the player chooses**.
3. **Choice = action + target in one click** (`tagBattlePlace@selectAction`, `あべ/T_BATPLS.JAF.jam:654-790`;
   `tagBattleSelect@addButton`, `あべ/T_BATSEL.JAF.jam:960-1133`): a button group is put **on every card on the
   field**; each card shows those of the actor's four skills (`技１..技４`) that may target that
   card (`checkAction`, `あべ/T_BATSEL.JAF.jam:2070-2189`). The button id encodes both:
   `100000 + target·1000 + action` (`あべ/T_BATSEL.JAF.jam:2027-2064`). A button is active if
   `現在技数 ≥ 行動技数` (special moves, cost 99: if `現在技数 > 0`) (`あべ/T_BATSEL.JAF.jam:1067-1097`);
   hovering it previews the cost on the actor's pips (`setWazaReserved`, `あべ/T_BATPLS.JAF.jam:3402-3438`).
   **The player never picks which unit acts** — only what the queued unit does.
   Solo battles add "Retreat" (999) on the other own cards (§4.2,
   `あべ/T_BATSEL.JAF.jam:1108-1128`, `あべ/C_合戦.JAF.jam:309-316`).
4. Targets (`合戦_ターゲット選択`, `あべ/C_合戦.JAF.jam:793-936`); target type `行動ターゲット`:

   | Type | Who may be clicked (`checkAction`) | Hits |
   |---|---|---|
   | 1 | actor in front **and** an enemy front unit | one; can be intercepted by a guard |
   | 2 | any enemy | one; can be intercepted by a guard |
   | 3 | any own unit | one ally |
   | 4 | self | self |
   | 5 | any enemy | the target's line: target + the unit behind / in front of it |
   | 6 | actor in front, any enemy | same as 5 |
   | 7 | any enemy | all enemies |
   | 8 | any own unit | all own units |

   Type 0 actions (Wait) only on the actor's own card; guards 21/22 only on own cards in the
   actor's row, 23 on any own card (`あべ/T_BATSEL.JAF.jam:2073-2125`). If the stored target died before a
   charged action fires, a fallback is used (`あべ/T_BATPLS.JAF.jam:797-868`): type 1/5/6 → enemy front unit
   with most troops (`selectTekiMae` keeps the largest `キャラ_兵数`), 2 → enemy with most troops,
   3 → own unit with fewest troops, 4 → self; 7/8 need no fallback. (The enemy AI's fallback is
   random instead, §5.3.)
5. Execution `合戦_行動` (`あべ/C_合戦.JAF.jam:540-758`), by `行動タイプ`: 0 Wait (nothing),
   1 physical / 2 magic attack (one target or several, by the target type; damage both ways —
   counterattack when `行動ダメージ率敵 ≠ 0`, `あべ/C_合戦.JAF.jam:2343`), 3 "hero" attack
   (special hit, then a normal attack if the target survives, `あべ/C_合戦.JAF.jam:1385-1421`),
   4 heal, 9 other (buffs, guards, time and 戦果 changes); special ids: guards 21-23, time changes
   5/150/160/161, 戦果 changes 38/39/66, earthquake 79 (formulas: 02-resolution.md §3,
   03-skills.md §3).
6. **Charging** (`行動タメ ≠ 0`, `あべ/C_合戦.JAF.jam:3261-3308`): the first use only sets `ＦＦ = 1` (charging)
   and ends the turn (time −1, delay = the action's wait, **no action points spent yet**); the
   unit's next turn fires it automatically and spends the points; after a `タメ == 1` action the
   unit re-queues with wait 0 (`あべ/C_合戦.JAF.jam:765-782`).
7. **Action points** (`あべ/C_合戦.JAF.jam:346-359`): spend `行動技数` (1, 2, 3); a special move (`行動技数 ==
   99`) spends **all** remaining points (`現在技数 = 0`). The enemy spends the same way except in
   solo battles, where it has unlimited points (`あべ/C_合戦.JAF.jam:473-489`; ∞ icon, `あべ/T_BATBUS.JAF.jam:68-87`).
8. `合戦_行動終了` (`あべ/C_合戦.JAF.jam:765-782`): automatic heal skills 100/101/102 (`あべ/C_合戦.JAF.jam:2975-3163`);
   **battle time −1** (`戦闘_行動終了` → `戦闘時間_減少`, `あべ/C_BATTLE.JAF.jam:140-145`); return the delay.

### 2.5 The small squares under the troop number = action points

`tagBattleWaza` (`あべ/T_BATWAZ.JAF.jam:126-278`; drawing details in 04-screen.md §6.3): up to
**6 pips**, 13×17 px each: `現在技数 − reserved`
filled (CG 10196), `reserved` highlighted (10198, the hovered button's cost), spent up to `技数`
dim (10197), the rest blank; enemies in solo battles show an ∞ icon instead (CG 10217). Data:
`setWaza` (`あべ/T_BATBUS.JAF.jam:1719-1776`). **Per battle**: refilled at battle start (1.4 step 4), never
recovered by time; only "Action Transfer" 174/175 restore 1/2 points to an ally (cap 技数)
(`多田/関数／本編.jaf.jam:28301-28554`); skills 103/104 drain enemy points at start; "Assassinate" targets the
unit with most points (AI, rule 12 in §5.2).

## 3. The central vertical gauge = battle time; the horizontal bar = 戦果 (advantage)

### 3.1 Time gauge (`tagBattleTime`, `あべ/T_BATTIM.JAF.jam`)

- 24 pips stacked vertically: base CG 10249 at (375, 326), pips CG 10250 at x 379, y 326 + 10·i
  (`あべ/T_BATTIM.JAF.jam:25-124`) — in the gap between the two front rows' cards (player front
  cards end at x 360, enemy front cards start at 440): this is the screen's **central vertical
  gauge** (its drawing: 04-screen.md §7).
- **Time left = 24 − `◆合戦ターン`**, clamped 0..24 (`あべ/T_BATTIM.JAF.jam:171-233`). Each action
  increments `◆合戦ターン` by 1 (`dec`, `あべ/T_BATTIM.JAF.jam:242-317`) — **actions of both sides share
  the same 24-action budget**; Wait and declaring a charge count too. Also Retreat (solo) costs 1
  (`あべ/C_BATTLE.JAF.jam:421-431`); a unit skipped for having no action points costs nothing.
- Modifiers: start +5 actions used (item 68 / skill 136, 1.4); "Pass Time" 5: +2, "Pass Time 2"
  150: +4, "Extended Battle" 160: −4, "Extended Battle 2" 161: −6 (`多田/関数／本編.jaf.jam:28173-28296`);
  event battles may start with `◆合戦ターン = 時間制限` (`多田/関数／本編.jaf.jam:11074-11084`).
- **At 0:** the battle is judged by 戦果 (4.1). In solo/event battles with
  `◆戦闘時間制限 == 1` the time limit is **not** checked at all (`あべ/C_合戦.JAF.jam:194-201`).

### 3.2 戦果 bar (`tagBattleBar`, `あべ/T_BATBAR.JAF.jam`; its drawing: 04-screen.md §3.1)

- **戦果 (advantage; "score" in 04-screen.md's drawing terms)** (`戦果取得`, `多田/関数／本編.jaf.jam:27547-27610`):

  `戦果１ = 100·被害総数１ / (被害総数１ + 被害総数２) + 戦果補正１ − 戦果補正２` (Oda),
  `戦果２ = 100·被害総数２ / (…) + 戦果補正２ − 戦果補正１` (enemy), integer division.

  `被害総数１` grows by the **troops the enemy loses** (hits on the Oda turn, counter damage on the
  enemy turn), `被害総数２` by Oda's losses (`あべ/C_合戦.JAF.jam:2431-2458`, hero hit `:2908-2925`); heals do
  not change them. So 戦果 = **share of all casualties inflicted, in %, plus flat bonuses**;
  both start at 50 (1.4 step 2).
- Flat bonuses `戦果補正(oda, enemy, label)` are queued (`あべ/T_SENKA.JAF.jam:7-18`) and added to
  `◆戦果補正１/２` on the next bar update (`あべ/T_BATTLE.JAF.jam:240-312`): facility defence (1.4), start
  skills, **+5 to the other side per destroyed unit** (army battles only, `あべ/T_BATTLE.JAF.jam:680-726`),
  an action's `行動戦果` (`多田/関数／本編.jaf.jam:28049-28084`) — applied only by
  `合戦／行動後処理`, which the attack paths never call, so it works for "Advantage Up 1/2" (+8/+15)
  but not for the attacks that carry a value (Shouting Charge +3, the hero attacks +5;
  03-skills.md §8) — and "Advantage Down 1/2" 38/39 (+5/+10 to the user's side,
  `多田/関数／本編.jaf.jam:28815-28894`).
- Display: one 400-px horizontal bar, Oda share `W1 = 400·max(戦果１,0) / (max(戦果１,0) +
  max(戦果２,0))` (200/200 if both ≤ 0; the rounding remainder goes to the larger side, to Oda on
  a tie) (`あべ/T_BATBAR.JAF.jam:816-899`); force emblems at both ends
  (`10500 + force`, `あべ/T_BATBAR.JAF.jam:41-69`); "Battlefield Adjustment +N%" texts per side
  (`あべ/T_BATBAR.JAF.jam:572-624`). The bar starts at 50:50 (`あべ/T_BATTLE.JAF.jam:378`).
- **Reaching an end does nothing by itself**: no threshold is checked anywhere (callers of
  `戦果取得`: the judge, the AI, the bar, one damage skill that triples damage while its side
  leads, `多田/関数／本編.jaf.jam:21475-21530`). On a win by annihilation the bar is set to 100:0 (0:100 on a loss
  by annihilation) for the result screen (`あべ/T_BATTLE.JAF.jam:161-191`).

## 4. Ending the battle

### 4.1 End checks (`合戦_状態補正`, `あべ/C_合戦.JAF.jam:141-233`, before every turn)

```
a = Oda wiped (no living unit in 11..16),  e = enemy wiped (21..26)     # あべ/C_合戦.JAF.jam:3425-3485
if a and e: return judge()
if a: return 2 (loss);  if e: return 1 (win)
na = every living Oda unit has 0 action points;  ne = same for the enemy  # あべ/C_合戦.JAF.jam:3555-3633
if ◆戦闘時間制限 == 1:            # solo/event: no time limit
    if na: return 2
elif (na and ne) or time_left <= 0:
    return judge()
front-row swap (1.3); continue
```

"Living" = slot occupied and `ＦＦ ≠ 99` (`あべ/C_合戦.JAF.jam:3373-3419`); a unit becomes destroyed when its
troops reach 0 (`あべ/C_合戦.JAF.jam:3169-3199`).

**Judge** (`合戦_勝敗判定`, `あべ/C_合戦.JAF.jam:3735-3787`): solo battle → 3 (draw); else higher 戦果 wins;
**a tie goes to the defender** (Oda attacking → loss, Oda defending → win, event → win).
Note: if only one side runs out of action points the battle goes on — the other side keeps acting
(its units leave the queue as they empty) until time runs out.

Results: 1 = Oda win, 2 = Oda loss, 3 = draw (logged "引き分け", `多田/関数／本編.jaf.jam:15388-15390`).

### 4.2 Retreat

No retreat from an army battle was found: the battle screen's system button opens only the system
menu (save/load/CG/music, `あべ/T_SYSMEN.JAF.jam:7-69`; `tagBattle@checkSystem`, `あべ/T_BATTLE.JAF.jam:898-913`)
**[I] from absence**. "Retreat" (999) exists only in solo battles. It is offered on the actor's
other own cards and acts on the **clicked** unit, not the actor (`戦闘_さがる`,
`あべ/C_BATTLE.JAF.jam:418-432`): the clicked unit leaves (`tagBattlePlace@leave` →
`tagBattlePlace@hoju`, `あべ/T_BATPLS.JAF.jam:1131`, `:3975-4071`), `Ｓ補充(row, slot)`
(`多田/関数／本編.jaf.jam:31264`) fills its place with a reserve for that row, or the place is
emptied when there is none; the place is re-queued with `速度補正値(slot, 0)` (`戦闘_補充`); battle
time −1. The actor spends no action points and re-queues with delay 0 (`行動_待ち時間(999)` returns
0, `あべ/C_行動.jaf.jam`; `あべ/C_合戦.JAF.jam:300-316`). Which reserve `Ｓ補充` picks was not read.
Losing an important story battle offers "Challenge Again / Give Up For Now / GAME OVER"
(`敗戦処理`, `多田/関数／本編.jaf.jam:33500-33653`; challenge again refills every unit's troops).

### 4.3 After the battle

1. Result screen `tagBattle@end` (`あべ/T_BATTLE.JAF.jam:142-226`) → `tagBattlePlace@kekka` →
   `Ｐ合戦／戦闘結果(result)` for result 1 or 2 only (`あべ/T_BATPLS.JAF.jam:559-602`); its
   `フラグ` is the battle result (1 = Oda win). Each card then shows its `Ｆ戦闘結果` as a badge
   (`tagBattleBusho@setKekka`): 1000 = died, 2000 = captured, else the troop gain
   (04-screen.md §6.2).
2. **Oda units** (`多田/関数／本編.jaf.jam:13577-13981`), each participating commander (`ＡＡ ≠ 0` = a real
   commander copied in):
   - destroyed: `戦場生死確率 −= 5`; then **death check** — item 49 saves; **Rance (1000) and
     Kou (1078) always die if destroyed** (→ their death scene and GAME OVER, `多田/関数／本編.jaf.jam:14612-14672`);
     `重要` units are safe; safe if `戦場生死確率 > RAND(50)`; else safe with 2/3 chance; else
     dead (`Ｆ戦闘結果 = 1000` → removed, `多田/関数／本編.jaf.jam:14685-14694`). So an ordinary commander whose
     unit is destroyed dies with probability 1/3 · P(RAND(50) ≥ 生死確率).
   - alive (`ＦＦ ≠ 99`) after a **win** (`フラグ == 1`, attack or defence,
     `多田/関数／本編.jaf.jam:13770-13800`): permanent **troop growth** `Ｆ戦闘結果 = aa` →
     `Ｃ兵数追加` raises max troops and troops by aa (`多田/関数／メイン.jaf.jam:12141-12204`): aa = 10; Ashigaru,
     Archer 15; Musketeer, Cavalry, Cannoneer 5; Knight, Mage, Onmyouji, Miko 8; −3 for the
     unit holding its type's top troop count; +1 with skill 214 (`多田/関数／本編.jaf.jam:13813-13974`).
3. **Enemy units** (`多田/関数／本編.jaf.jam:13987-14548`), destroyed ones only, at most **2 casualties per
   battle** (`▲死亡制限 = 2`; units with `Ｆ生死処理 == 2` ignore the cap), only if the enemy
   force has ≥ 4 commanders, never for `重要` units, independent of who won:
   - **capture** (not before the first-prisoner event; less likely when prisoners > 20, never
     > 25; never Peasants/Soul Bound or 捕虜抵抗値 999): chance 1/5, 1/2 with skill 111 or 219
     on the Oda side, 1 with both or for some units; a captured unit gets `Ｆ戦闘結果 = 2000`
     (`多田/関数／本編.jaf.jam:14143-14403`; details in 02-resolution.md §8.4);
   - else **death** for generic commanders (`番号 999`): chance 1/2 (always for some)
     (`多田/関数／本編.jaf.jam:14407-14536`).
   Applied by `合戦／戦闘結果実行` (`多田/関数／本編.jaf.jam:14562-14895`).
4. Troops lost stay lost: battle copies are written back (`ＦＰ戻し`, `多田/関数／本編.jaf.jam:35669-35816`), a unit
   at 0 troops is left with 1.
5. Province (`多田/関数／本編.jaf.jam:12561-12626`): Oda **attack win** → enemy loses one land (its stronghold flag
   removed); at 0 lands the province changes hands (`合戦／勝利処理`, `多田/関数／本編.jaf.jam:12875-12941`); a win
   in Oda's own province regains a land (`:12781-12865`). Oda **defence loss** → Oda loses one
   land; at 0 the province is lost (Owari: GAME OVER) (`合戦／敗北処理`, `多田/関数／本編.jaf.jam:13360-13513`).
   A lost attack on a stronghold land changes its difficulty (`多田/関数／本編.jaf.jam:12332-12380`).

## 5. Enemy AI (`あべ/C_合戦敵.JAF.jam`)

### 5.1 Turn (`合戦_敵行動`, `あべ/C_合戦.JAF.jam:375-498`)

Same as the player's (2.4) but the action comes from `合戦敵_行動選択` and the target from
`合戦敵_ターゲット選択`. The actor is fixed by the queue; the AI chooses action and target only.

### 5.2 Action choice: a fixed priority list (`合戦敵_行動選択`, `あべ/C_合戦敵.JAF.jam:14-791`)

Rules are tried in order; the first whose condition holds **and** whose legality + probability
check passes is used. `n` = own living units, `t` = battle time left.

| # | Condition | Action | % |
|---|---|---|---|
| 1 | 技1 = 81 (self heal) and own damage ≥ 50 % | 技1 on self | 80 |
| 2 | 技4 removes buffs (70-72) and Oda units carry ≥ 5 / ≥ 1 buffs | 技4 | 100 / 50 |
| 3 | Ashigaru, another own non-Ashigaru unit in its row | 技1 (its guard) | 70 |
| 4 | Ninja, an Oda unit is charging | 技3 on it | 80 |
| 5 | Miko, an own unit damaged ≥ 50 % / ≥ 25 % | 技1 (heal) on it | 90 / 60 |
| 6 | 技1 = 84 (Full Recover), own damage ≥ 50 % | 技1 on self | 70 |
| 7 | Sohei: 技1 = 58/126 and own damage ≥ 50 % → 技1 self (80); 技4 = 73 (Guard Cancel) and an Oda unit guards → 技4 (70) | | |
| 8 | 技1 is a buff (60-63, 135), an own unit lacks a buff, t ≥ 10 | 技1 | 50 |
| 9 | 技1 = 66 (Witty Turnabout), Oda leads 戦果, n ≥ 3 | 技1 | 90 |
| 10 | 技4 = 5/150 (Pass Time), the AI leads 戦果 | 技4 | 60 |
| 11 | 技1 = 3/143 (Defence Shikigami), **no Oda unit** has a shikigami | 技1 on own unit without one (front first) | 50 |
| 12 | 技4 = 85/86 (Assassinate), n ≥ 3, t ≥ 10 | 技4 on the Oda unit with most action points | 50 |
| 13 | 技4 is a charge move, n ≥ 3, t ≥ 15 | 技4 | 80 |
| 14 | 技3 is a charge move, n ≥ 3, t ≥ 15, no Oda Ninja | 技3 | 80 |
| 15 | 技4 has `行動ＴＸ == 0` (139/140 prefer Ashigaru/Ninja targets) | 技4 | 40 |
| 16 | 技1 has `行動ＴＸ == 0` | 技1 | 20 |
| 17 | 技3 has `行動ＴＸ == 0` (Samurai/Musketeer/Archer/Ninja exclude Ashigaru targets) | 技3 | 100 |
| 18 | otherwise | **Wait** (action 1: type 0, self, wait 25, cost 0, `多田/▲技能データ.jaf.jam:23-59`) | – |

Rule 11 checks the Oda side (11-16) for shikigami while the action protects the AI's own units
(`あべ/C_合戦敵.JAF.jam:1614-1650`, `:480-501`) — **[I] probably a bug** (a Sengoku Rance
oddity). `行動ＴＸ` (17 entries of the action table set it) is read only here, through `行動_ＴＸ`
(`あべ/C_合戦敵.JAF.jam:671`, `:717`, `:747`): an action with `行動ＴＸ ≠ 0` is never picked by the
generic rules 15-17, only by the special rules above. (It is unrelated to the function `ＴＸ`,
the speech line `合戦／行動前処理` shows, `多田/シナリオ３／テキスト.jaf.jam:2`.)

**Legality** `合戦敵_行動チェック` (`あべ/C_合戦敵.JAF.jam:805-1029`): action exists; enough action points
(skipped when `◆戦闘時間制限 == 1`); target types 1/6 need the actor in front; guards 21/22 need
another living own unit in the actor's row, 23 any other own unit; then
`確率チェック(p)` = `乱数(100) ≤ p` (`あべ/S_TOOL.JAF.jam:53-79`).

### 5.3 Target choice

`敵ターゲット_選択` (`あべ/C_BATTLE.JAF.jam:1095-1197`) and `tagBattleTeki` (`あべ/T_BATTEK.JAF.jam`):

- type 1, 5, 6 → `select(1)`: Oda front units; type 2 → `select(2)`: any Oda unit. With a
  priority list (`add1`, set by rule 15) the AI picks among units of those troop types with 70 %,
  with an exclusion list (`add2`, rule 17) among the others; **otherwise uniformly at random**
  (`あべ/T_BATTEK.JAF.jam:142-229`, `:652-693`, `check` `:847-952`).
- type 3: heals → the first own unit damaged ≥ 80/60/40/20/0 % (random among equals); other
  type-3 actions → a random own unit (`あべ/C_BATTLE.JAF.jam:1126-1167`).
- 4 → self; 7/8 → everyone.
- Fallback when the stored target is gone at execution: random as above (`あべ/T_BATTEK.JAF.jam:764-839`).
- Guards on the Oda side may intercept single-target attacks (types 1/2):
  `合戦_味方ガード取得` (`あべ/C_合戦.JAF.jam:944-1086`) — an Oda unit in Ally Guard / Neo Ally Guard
  (`ＦＦ` 21/22) covers units in its own row (both in the front or both in the back,
  `戦闘_味方前衛` / `戦闘_味方後衛`), All Guard (23) any unit; the guarding unit takes the hit at
  half damage (02-resolution.md §5); chance
  `乱数(100) ≤ ＦＦＥ`; `ＦＦＥ` += INT·20 / INT·30 / INT·10 when the stance is taken
  (`多田/関数／本編.jaf.jam:28577-28794`), −40 each time the guarding unit takes a hit without a shikigami
  (`あべ/C_合戦.JAF.jam:2485-2499`). The same works for enemy guards against the player
  (`あべ/C_合戦敵.JAF.jam:1816-1948`).

## 6. Sengoku Rance oddities and open questions

Oddities (Sengoku Rance's own; not entries of docs/quirks.md):

1. The initial-order head start for undrawn units (2.2): about 1 unit in 14 acts ~10 ticks
   earlier than the others; intended or not is not settled.
2. AI rule 11 checks the wrong side for shikigami (§5.2).
3. `行動戦果` of attacks is never applied (§3.2; 03-skills.md §8).

Settled while the four parts were made consistent:

- **Wait is in slot 2 of every commander.** `デフォルト技能` fills an empty `技２` with Wait (1)
  (`多田/▲武将データ.jaf.jam:17759-19299`), all 164 `ＣＨＢ` calls in `多田/▲武将データ.jaf.jam` pass
  0 or 1 as `技２`, and Power Up never offers slot 2 (03-skills.md §2.3). The player picks among
  `技１..技４` (`あべ/T_BATSEL.JAF.jam:1038-1105`), so Wait is always among them; the AI falls back
  to it. Open: whether a story change (`Ｃ技能変更`, `多田/関数／メイン.jaf.jam:10171`) ever
  replaces it.
- `▲兵数制限` (`多田/関数／本編.jaf.jam:13564`) is assigned and never read.
- `行動ＴＸ` is the AI's exclusion flag (§5.2).

Open:

1. Which reserve `Ｓ補充` (`多田/関数／本編.jaf.jam:31264`) brings in for a solo-battle Retreat or
   a destroyed solo unit (§4.2), and what happens to the queue entry of a place left empty.
2. What `合戦_暗殺後処理` (`多田/関数／本編.jaf.jam:30703-30735`) does beyond the message
   (03-skills.md §3.3).
