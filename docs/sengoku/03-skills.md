# Sengoku Rance battle survey, part 3: commands and skills in battle

This part covers the commands a Sengoku Rance unit has in battle (four slots and a passive), the
action table's fields, how a command executes (targets, preparation, attacks, heals, support,
Retreat), the states and buffs, the per-target command menu, and every battle action and passive
in tables. It is one of four reference surveys for **our own battle screen "in the manner of
Sengoku Rance"**, not rules for the port — Kichikuou's rules stay unchanged. The conventions
(source references relative to `ain\files\` under `SENGOKU_RANCE_EXTRACT_DIR`, places 1-6 with 1-3
the front row, "row" and "line", **[I]**) are in
[01-flow.md](01-flow.md#conventions-shared-by-the-four-parts); the damage formulas are in
[02-resolution.md](02-resolution.md), the drawing of the menu in [04-screen.md](04-screen.md) §8.
A bare line number after a function name refers to the file named last in that paragraph (most
often `多田/関数／本編.jaf.jam`).

## 0. Vocabulary and the unit record

- **Action** (行動) is the code's word for every command, skill and passive. One table of parallel
  global arrays holds them (`globals.txt` 0x83–0x91), filled by `技能データ初期化`
  (`多田/▲技能データ.jaf.jam:5-10514`): 218 entries — 0 "No Skill" and **217 actions numbered
  1–220** (24, 176 and 183 have none); 999 = Retreat is named outside the table. Accessors with
  range checks are in `あべ/C_行動.jaf.jam` (`行動_タイプ` :203, `行動_ターゲット` :250, …).
- A battle has 12 slots: player units **11–16**, enemy units **21–26**, slot = 10 / 20 + place
  (`合戦_味方全滅`, `あべ/C_合戦.JAF.jam:3425-3486`). Places 1–3 are the front row, 4–6 the back
  row; place p and p+3 form a **line** (`戦闘_敵列`, `あべ/C_BATTLE.JAF.jam:604-705`).
- Battle-relevant fields of `sキャラ` (`structures.txt`; index used by Tada's code as `gs[0][unit][n]`):

| idx | field | meaning in battle |
|---|---|---|
| 20 | 兵種 | unit type (table below) |
| 21 / 22 / 23 | 兵数 / 兵数最大 / 兵数合戦 | troops / max troops / troops at battle start (injured = 23 − 21) |
| 29 / 30 / 31 | 技数 / 現在技数 / 迷宮技数 | max moves / current moves / dungeon moves |
| 32–35 | 攻撃 防御 知力 早さ | attack, defence, intelligence, speed |
| 36–40 | 技１–技４, 技能 | four command slots + one passive slot |
| 41–45, 46 | 技１Ｂ–技能Ｂ, 技セット | second set (individual battles), which set is active |
| 54–56 | Ｓ体力, Ｓ体力最大, Ｓレベル | HP / max HP / level in individual battles |
| 69 | ＦＦ | battle state (section 4) |
| 70 / 71 | ＦＦ２ / ＦＦ３ | action being prepared / its remaining prep resistance |
| 72 | ＦＦＥ | guard rate (%) |
| 73 | Ｆ式神 | defence shikigami present |
| 74 | Ｆ集中 | concentration (Meditation stacks) |
| 75 | Ｆ生死処理 | post-battle fate flag (assassinated = 1, Mercy Charge = 2) |
| 77–80 | 攻撃付与 防御付与 知力付与 早さ付与 | the four buffs, in percent |
| 83 | 予備１ | "came back" flag: +6 delay (set by Action Transfer) |

Unit types (`文字列／兵種`, `多田/関数／本編.jaf.jam:45794-45989`): 1 Samurai, 2 Cavalry,
3 Ashigaru, 4 Musketeer, 5 Archer, 6 Ninja, 7 Sohei, 8 Onmyouji, 9 Miko, 10 Knight, 11 Mage,
12 Cannoneer, 13 Tactician, 20 Animal, 21 Youkai, 22 Monster, 23 Hanny, 24 Peasant,
25 Soul Bound, 27/28/29/31 Monster (four monster sub-types).

`◆戦闘タイプ` 0 = army battle (合戦), 1 = individual battle (HP and levels); `変数_ソロ戦闘` is true
for type 1 and for dungeon battles (`あべ/C_変数.jaf.jam:132-163`).

## 1. The skill record (fields)

| field (global array) | meaning | read in |
|---|---|---|
| 行動名前 / 行動説明 | English label / hover text (`@` = line break). A short label exists for 8 long names (`GetActionNameShort`, `あべ/C_行動.jaf.jam:95-141`: "Full Power Attack", "Province Destroyer", "Leaf Camouflage", "Encampment Construction n", "Ieyasu's Special Move") | button, tooltip |
| 行動タイプ | 0 wait, 1 physical attack, 2 magic attack, 3 hero (commander strike + unit charge), 4 heal, 9 support/other, 11 passive, 19 passive that also moves the advantage | `合戦_行動` switch, `あべ/C_合戦.JAF.jam:653-747` |
| 行動ターゲット | 1 one enemy, melee (user and target must both be front row); 2 one enemy, anywhere; 3 one ally (self included); 4 self; 5 an enemy **line** (target + the unit in front of / behind it); 6 an enemy line, user must be front row; 7 all enemies; 8 all allies | `tagBattleSelect@checkAction`, `あべ/T_BATSEL.JAF.jam:2070-2196`; `合戦_ターゲット選択`, `あべ/C_合戦.JAF.jam:793-937` |
| 行動技数 | **move cost**. 0 = free (Wait, Draw Attention, Snake Grease); **99 = "special" (必殺技)**: needs ≥ 1 move and spends all that remain | `行動_必殺技` `あべ/C_行動.jaf.jam:672`; `合戦_味方行動` :346-359 |
| 行動待ち時間 | **delay** added to the unit's next turn (`速度補正値(unit, delay)`); the base is in 01-flow.md §2.2 | `合戦_行動終了` :765-783 |
| 行動ダメージ率 | percent multiplier on the normal physical/magic/heal result. Re-used as a plain number by some support skills (Encampment +n, Advantage Down +n) | `合戦／戦闘計算` 20539, 21380; `合戦／回復計算` 20078 |
| 行動ダメージ率敵 | **counterattack** percent; 0 → no counter at all | `合戦_戦闘処理` `あべ/C_合戦.JAF.jam:2343` |
| 行動タメ | **preparation**: ≠ 0 → the first use only declares; the effect fires on the unit's next turn. 2 (and 3, 4) also charge the delay twice (see 3.3) | `合戦_ため処理` :3261-3309 |
| 行動タメ耐性 | prep resistance: how many Prep Break points it takes to cancel the preparation | idem; `戦闘計算` 23803-23899 |
| 行動タメ破壊 | **Prep Break**: points removed from a preparing target's resistance per damaging hit | `戦闘計算` 23803 |
| 行動技数破壊 | **Move Break**: current moves removed from the target per damaging hit | `戦闘計算` 23908-23956 |
| 行動戦果 | advantage (戦果) added to the user's side — applied only by `合戦／行動後処理` (28049-28086), which attacks never call, so it works for Advantage Up 1/2 but **not** for Shouting Charge or the hero attacks (oddity, §8) | |
| 行動ＴＸ | 1 = the enemy AI never picks this skill in its generic fallback (only through its special rules); set on 17 entries, read only by `合戦敵_行動選択` through `行動_ＴＸ` (`あべ/C_合戦敵.JAF.jam:671`, `:717`, `:747`) | |
| 行動説明２ | declared, never filled | |

Fields absent from an entry are 0.

## 2. Which commands a unit has

### 2.1 Slots

A unit has **four command slots (技１–技４) and one passive slot (技能)**, plus a second set B
used in individual battles. Nothing else adds commands: unit type only supplies defaults, items
add no commands (they act as hidden modifiers, 2.4), and the only command outside the slots is
**Retreat**.

`ＣＨＢ` (`多田/▲武将データ.jaf.jam:14218-14440`) creates a commander: it calls
`デフォルト技能(兵種)` (17759-19299), which fills **only empty** slots with the type's defaults, then
writes the commander's own non-zero skills over them. Defaults (slot 1 / 2 / 3 / 4 / passive;
set B slots 1–4):

| type | set A (mass battle) | set B (individual battle) |
|---|---|---|
| Samurai, monster 27 | – / Wait / Samurai Attack (11) / – | – / Wait / S Sword Attack / S Strong Sword Attack |
| Ashigaru, monster 28 | Ally Guard (21) / Wait / Ashigaru Attack (131) / – | Ally Guard / Wait / S Spear / S Strong Spear |
| Knight | Ally Guard / Wait / Knight Attack (76) / – | – / Wait / S Stab Attack / – |
| Archer, monster 29 | – / Wait / Bow Attack (31) / – | – / Wait / S Bow / S Strong Bow |
| Musketeer | – / Wait / Shoot (32) / – | – / Wait / S Strike Attack / – |
| Cannoneer | – / Wait / Tulip (36) / – | – / Wait / S Tulip / – |
| Cavalry | – / Wait / Cavalry Charge (13) / – | – / Wait / S Strike / S Strong Strike |
| Ninja | – / Wait / Shuriken (33) / – | – / Wait / S Shuriken / S Strong Shuriken |
| Miko | Miko Dance (50) / Wait / Bow Attack / – | Neo Miko Dance / Wait / S Strike / S Strong Strike |
| Sohei | Tenshi's Blessing (58) / Wait / Sohei Charge (10) / Guard Cancel (73) | Action Transfer / Wait / S Strike / S Strong Strike |
| Onmyouji | Defense Shikigami (3) / Wait / Shikigami (40) / – | Defense Shikigami / Wait / S Shikigami Blast / S Strong Shikigami Blast |
| monster 31 | – / Wait / Thunderstrike (44) / – | Defense Shikigami / Wait / S Shikigami Blast / S Strong … |
| Mage | – / Wait / Fire Blast (45) / – | – / Wait / Fire Blast / – |
| Tactician | Battle Tactics I (63) / Wait / Bow Attack / Status Cancel I (70) | Battle Tactics I / Wait / S Strike / Status Cancel I |
| Peasant | – / Wait / Uprising (14) / – | – / Wait / S Strike / – |
| Soul Bound | – / Wait / Infect (69) / – | – / Wait / S Strike / – |
| Hanny | – / Wait / Hanny Attack ×2 (55) / passive Absolute Magic Defense (106) | – / Wait / S Hanny Flash ×2 / 106 |
| Animal | – / Wait / Animal Attack (203) / – | – / Wait / S Animal / S Strong Animal |

So **slot 2 is always Wait**, slot 3 is the type's basic attack, slots 1 and 4 hold the type's
or the commander's specials. Hero skills (Rance Attack 90, Kentarou-kun Slash 95, …) are given
to slot 4 by story events (`Ｃ技能変更`, e.g. `多田/Ｆコマンドイベント.jaf.jam:68934-68953`, which
also puts the S version in set B slot 4).

### 2.2 Set B (individual battles)

At battle start in an individual battle every slot whose `技セット` is 0 swaps A ↔ B for slots
1–4 (and the passive too for Rance, char 1000) (`Ｃ技セット変更`,
`多田/関数／メイン.jaf.jam:9652-9909`, called from `合戦／初期化`,
`多田/関数／本編.jaf.jam:15668-15690`). Player units then start with `迷宮技数` moves instead of
their max (15754-15769).

### 2.3 Where the "2" comes from (upgrades)

Names with "2", "Neo", "I/II/III" are **separate entries**, not skill levels — there is no skill
level in army battles (an individual battle has the commander's `Ｓレベル`, which is not a skill
level). A stronger entry replaces a weaker one in its slot through the commander **Power Up**
menu (`能力ＵＰ`, `多田/関数／システム.jaf.jam:743-1780`: Stat Up, "<skill>: Change" for slots 1, 3,
4 and the passive, Troops +n, Level +2). The successors offered per slot
(`技能１処理` 1878-2549, `技能３処理` 2559-3106, `技能４処理` 3116-4471, `技能５処理` 4480-5106) form trees,
e.g.:

- Miko slot 1: Miko Dance → Neo Miko Dance or Healing Fog; Healing Fog → Miko Dance/Wind.
  Miko slot 4: Miko Storm (10 %) or Status Cancel I. ("Miko Storm 2", 30 %, is not in the Power Up
  tree; it comes from a commander's own data **[I]**.)
- Ashigaru/Knight slot 1: Ally Guard → Neo Ally Guard or All Guard. Ashigaru slot 4: Lancer Legion →
  Lancer Legion 2; Pillage; Ally Avenger.
- Samurai slot 3: Samurai Attack → Samurai Attack 2 → Guard Break; slot 4: Shouting Charge,
  Roundabout Charge, Mercy Charge, Full Power → Full Power 2.
- Tactician slot 1: Battle Tactics I → II → III or (Single) → (All); slot 4: Status Cancel I → II → III,
  Pass Time → Pass Time 2, Advantage Down 1 → 2.
- Sohei slot 4: Guard Cancel → Youkai Purge or Extended Battle → Extended Battle 2.

Some branches carry extra character/stat conditions that were not decoded (open question 3).

### 2.4 Items in battle (no commands)

Items only modify numbers, through `アイテム確認(item, unit)` in `合戦／戦闘計算`
(`多田/関数／本編.jaf.jam:22303-22750`, 24351) and `合戦／初期化` (16181-16383, 17337): attacker
×1.2 (31 Chaos, 32 Nikkou, 58 Murasame, 60 Cao Cao Poison Dagger), +50 (61 Maulrus Tusks), +100
(63 Urashimaru), ×1.5 vs monster types else ×0.7 (62 Monster Murder Chisel); defender, only when
not guarding: ×0.7 (64), −50 (65), −100 (66), ×0.5 (67), ×0.5 when the wearer is a monster type
(69); counterattacker ×1.5 (59 Wontonbokiri); at battle start: +5 battle turns (68 Yomi Clock),
all four buffs +50 on battlefield 1/3/7 (48/47/46, the three books), a defence shikigami
(70 Protection God Paper). Item names: `多田/▲アイテムデータ.jaf.jam` (`SetItemNames`).

## 3. Executing a command

### 3.1 Turn of a unit (`合戦_味方行動` / `合戦_敵行動`, `あべ/C_合戦.JAF.jam:242-499`)

```
if current moves == 0: skip, delay 40
action = 合戦_行動開始(unit)          # preparing? -> the stored action (ＦＦ２), else clear ＦＦ/ＦＦ２/ＦＦ３
if action == 0: action = player menu (戦闘_行動選択) / AI (合戦敵_行動選択)
targets = 合戦_ターゲット選択(...)     # 3.2
if action == 999 (Retreat): 戦闘_さがる(target); delay = 0; no move spent
r = 合戦_行動(unit, action, targets)  # 3.3
if r > 0 (preparation just declared): delay = 待ち時間(r); no move spent
else: moves -= cost, or moves = 0 for a special (enemies in individual battles spend nothing)
      auto-heal passive (3.6); delay = 0 if 行動タメ == 1 else 待ち時間
```

### 3.2 Targets

- The player picks the target and the command in one click (section 5; the acting unit is the
  head of the turn order, 01-flow.md §2). Before acting the target is re-read;
  if it was destroyed meanwhile (a prepared action), a new one is chosen by kind: for the player
  1/5/6 the enemy front-row unit with most troops, 2 the enemy with most troops, 3 the own unit
  with fewest troops (`tagBattlePlace@getTarget`, `あべ/T_BATPLS.JAF.jam:797-875`); for the AI a
  random one (01-flow.md §2.4 step 4, §5.3).
- Kinds 1 and 2 (single enemy) can be **intercepted by an ally guard**: the defending side's units
  in guard state are tried in slot order; a guard in state 21/22 only covers a target in its own row,
  state 23 covers any; it steps in if `乱数(100) <= ＦＦＥ` and becomes the target
  (`合戦_味方ガード取得/発生`, `あべ/C_合戦.JAF.jam:944-1087`; enemy side `合戦敵_…`,
  `あべ/C_合戦敵.JAF.jam:1816-1949`). Line, "all" and support skills are never intercepted.
- 5/6 → the line (1–2 units); 7/8 → every living unit of the side.

### 3.3 Dispatch (`合戦_行動`, `あべ/C_合戦.JAF.jam:540-759`)

```
合戦／行動前処理 (◆経過時間 +1 — not the time gauge, which counts ◆合戦ターン; speech line ＴＸ)
if 行動タメ != 0 and not already preparing:
    ＦＦ = 1, ＦＦ２ = action, ＦＦ３ = 行動タメ耐性 ; log "is storing power" ; return action
clear preparation
if action is Ally/Neo/All Guard (21/22/23) -> 合戦_ガード処理     (effects 4.2)
elif Pass Time / Extended Battle (5,150,160,161) -> 合戦_時間変更処理
elif Advantage Down 1/2, Witty Turnabout (38,39,66) -> 合戦_戦果変更処理
elif Grand Earthquake (79) with the AoE-animation option off -> 合戦_大地震
else by type: 0 log only | 1,2 合戦_攻撃_単体/複数 | 3 合戦_攻撃_勇者 | 4 合戦_回復_単体/複数 | 9 合戦_その他_単体/複数
then 合戦暗殺後処理 (message if an assassination happened)
```

A prepared action therefore costs one turn with its delay, then fires on the next turn and charges
its delay again unless `行動タメ == 1` (then 0) — so `行動タメ` 2/3/4 mean a longer total wait, not
more turns of waiting **[I from the code path]**. Moves are only spent when it fires.

### 3.4 Attacks (types 1, 2, 3)

- Per target `合戦_戦闘処理` (`あべ/C_合戦.JAF.jam:2131-2507`): damage = `合戦／戦闘計算(…, flag 0)`; then,
  if `行動ダメージ率敵 != 0`, the counter = `合戦／戦闘計算(…, flag 1)` applied to the attacker.
  Multi-target attacks resolve each target in turn (each may counter).
- After the hit, a target in a guard state without a shikigami loses 40 guard rate
  (`キャラ_ＦＦＥ−`, no floor: it can go negative) (:2485-2499).
- Skill-specific steps inside `合戦／戦闘計算` (`多田/関数／本編.jaf.jam:20233-24611`; the base
  formulas `物理計算`/`魔法計算`/`人数計算` are in 02-resolution.md §3):
  - magic (type 2) branch 20499-21313: magic formula × troops scaling × `ダメージ率`; side
    correction; skill overrides (Angel Cutter ×2s, Miko Storm/2 = 10/30 % of max troops, char
    1090 takes ½, battlefield talents ×1.4); **Absolute Magic Defense → 0; Unity; Hanny type → 0;
    shikigami → 0**; boss correction; capped at the target's troops. Magic never halves for guard,
    never breaks preparation or moves.
  - physical branch 21319-24198: physical formula × troops scaling × `ダメージ率`; side
    correction; per-skill rules (tables); flat passives Hibachi +50, Lover +30; char 1090 ½;
    **target in a guard state → ½** (Guard Break ×2 back); talents ×1.4; items; buff removal
    (10/35/99/145); assassination block (3.5); hero fixed damage; fixed-damage skills; shikigami → 0;
    Unity; **Prep Break, Move Break** (only if damage ≠ 0); boss correction; cap at troops;
    Infect/Persuade max-troop changes.
  - counter (flag 1) 24204-24603: **no counter if the defender is Tactician, Archer, Musketeer,
    Onmyouji, Miko or Cannoneer, or in an individual battle**; counter = physical(defender →
    attacker) × troops scaling (defender's troops before the hit, 10 if it died) ×
    `ダメージ率敵`; ×1.5 Wontonbokiri, ×2/×3 passive 108/109; **a shikigami on the attacker
    blocks it**; boss correction; cap.
- Hero (type 3, `合戦_勇者処理` :2723-2965): first a commander strike for a fixed
  `20 + 3 × Ｓレベル` troops (flag 3, 23356-23449); if the target survives, `合戦_攻撃_単体` with the same
  action (100 %, no counter).
- Each hit also adds to the side damage totals ◆被害総数１/２ and ＧＦ[172]/[173] (damage taken by
  player / by enemy), which feed the advantage bar and Ally Avenger.

### 3.5 Assassination / snipe (29, 85, 86, 91, 170) (`戦闘計算` 22926-23351)

```
damage = 0 except Shoot + Snipe (170)
chance = 45 (29, 85, 91) | 50 (86) | 40 (170)
chance += 10 × user's Ｆ集中 (Meditation stacks)
chance -= 10 if any unit of the target's side has Assassination Defense (213)
army battle: +5 if user troops > 2 × target troops; −10 if target troops > user troops;
             another −10 if target troops > user troops + 1000
individual battle: chance = 0 if user Ｓレベル < target Ｓレベル
chance = 100 if the user has Assassinate Skill+ (220) and the target has 0 moves left
success if the target is not "important" (重要 = 0) and chance > 固定乱数取得()
  (a fixed 400-entry random table, 関数／メイン 18137-18164):
  target ＦＦ = 99 (out), Ｆ生死処理 = 1, 戦場生死確率 = 0
```

### 3.6 Heals (type 4) (`合戦_回復処理` `あべ/C_合戦.JAF.jam:2519-2713`; `合戦／回復計算` 19698-20207)

```
army battle: heal = 人数計算(知力 × 6 × (100 + 知力付与)%, user troops) × ダメージ率%
             Full Recover (84) = 999; Snake Grease (163) = 9999, target moves refilled, user's slot 1 erased
             capped at the target's injured troops (兵数合戦 − 兵数) unless it is a solo record
individual:  heal = Ｓレベル × 5 (×1.5 with an intelligence buff); 81/84 = 999 (ダメージ率 unused)
S Healing (166) on Rance (char 1000): +1 current move, even at full HP, and he is re-queued.
```
A unit with nothing to heal is skipped (no animation). Passives 100/101/102 (Auto-Heal Sill/1/2)
heal ＧＦ[33]/[34]/[35] troops (30/30/50 at game start, `多田/■初期化.jaf.jam:169-185`) at the end
of each of the unit's own actions, capped at its injured troops (`合戦_自動回復処理`, :2975-3164).

### 3.7 Support (type 9) and the guards

Their effects are all in `合戦／行動後処理` (`多田/関数／本編.jaf.jam:27963-30697`); see the tables
and section 4. Guards: `合戦_ガード処理` (:1733-1758) — the effect is on the **user**; the target
in the menu only marks which allies are covered.

### 3.8 Retreat (999)

Name "Retreat" (`行動_名前`, `あべ/C_行動.jaf.jam:52-58`), target kind 3, cost 0, delay 0. Offered **only
in individual/dungeon battles**, on every ally except the acting one
(`tagBattleSelect@addButton`, `あべ/T_BATSEL.JAF.jam:1108-1128`). `戦闘_さがる` (`あべ/C_BATTLE.JAF.jam:421-432`)
plays the clicked unit's leave animation; `tagBattlePlace@hoju` (`あべ/T_BATPLS.JAF.jam:3975-4071`)
asks `Ｓ補充(row, slot)` for a reserve for that row and seats it, or empties the place when there
is none; the place is re-registered in the turn order (`戦闘_補充`) and the battle time drops by
one (`戦闘時間_減少`). The actor spends nothing (01-flow.md §4.2). Which reserve `Ｓ補充` picks
is open (§9).

## 4. States, buffs and debuffs

### 4.1 The four buffs (付与)

| buff | effect | source |
|---|---|---|
| 攻撃付与 (attack) | physical attack value × (100 + n) % | `物理計算` 25483-25490 |
| 防御付与 (defence) | physical defence value × (100 + n) % | 25508-25515 |
| 知力付与 (intelligence) | magic attack **and** magic defence × (100 + n) %; heal × (100 + n) % | `魔法計算` 26092-26117; 回復計算 20033 |
| 早さ付与 (speed) | −4 delay on each registration in the turn order | `速度補正値` 27367-27374 |

- **Duration: the whole battle.** They are zeroed only at battle start (`ＧＳ初期化／合戦部分`,
  `多田/関数／メイン.jaf.jam:7205-7346`) or by removal skills; no countdown exists.
- Granted by: Store Power 70 (atk), Draw Attention 50 (atk), Leaf Camouflage 70 (def), Battle Tactics
  (Single/All/I/II/III) 知力 × 5 (solo: Ｓレベル), Tactician units at battle start (知力 random boosts
  of 知力 × 5; solo 4 boosts of Ｓレベル, 17359-17652), Miko at Work 40 (atk, male units), luck passives
  50, Triplets/Furinkazan 50 ×4, the three battlefield books 50 ×4, passive 176 (no data entry) 30 ×4
  on units 21-26 **[I: a boss aura]**, characters 1010 + 1011 together 50 atk/def (16834-17326).
  Battle Tactics I–III only raise a buff that is lower than the new value.
- Removed by: Sohei Charge/Neo, Sandal Shuriken, Sekimei Sword (the first non-zero of atk, def, int,
  spd of the target, per hit, 22756-22921); Status Cancel I/II/III (all four, on 2/3/4 random buffed
  enemies, 29988-30287).
- Screen: four icons (CG 10237–10240) shown when the value is > 0, 15 px apart, at unit
  x + 6 (player) / + 112 (enemy), y + 68 (`あべ/T_BATFUY.JAF.jam:21-59, 200-243`).

### 4.2 Battle state ＦＦ (one at a time)

| ＦＦ | state | set by | cleared | effect |
|---|---|---|---|---|
| 0 | none | | | |
| 1 | **preparing** (ため) | a skill with 行動タメ ≠ 0 | when it fires, or when Prep Break brings ＦＦ３ to exactly 0 (also clears ＦＦ２) | the stored action fires next turn without a menu. A prep resistance of 0 can never be broken: ＦＦ３ goes negative **[I]** |
| 2 | confusion (混乱) | nothing found | | none found (dead) |
| 3 | defence (防御) | only by action 6 through 行動後処理, which an attack never reaches | | none found (dead; has an icon) |
| 21 / 22 | Ally Guard / Neo Ally Guard | the skill; **every Ashigaru unit starts the battle in 21 with guard rate 50** (Enhanced Initial Guard passive: rate 120) (16121-16170) | at the unit's next turn (`合戦_ＦＦクリア` in `合戦_行動開始`) or by Guard Cancel | covers allies in the same row (interception, 3.2); physical damage taken ½ |
| 23 | All Guard | the skill | idem | covers every ally; physical damage taken ½ |
| 99 | out | troops 0 (`合戦_壊滅チェック`) or assassination | | |

Guard rate (ＦＦＥ) is **added** on each use (Ally Guard +知力×20, Neo +知力×30, All +知力×10; solo
+80/+150/+100), can exceed 100, drops 40 per hit taken, and is zeroed only at battle start or by
Guard Cancel — re-guarding stacks it (`行動後処理` 28577-28794). Note that the guard rate survives
the state: the halving needs ＦＦ 21–23, the interception too.

Status icon (`あべ/T_BATST.JAF.jam:261-380`): ＦＦ 1 → CG 10155, 2 and 3 → text sprites, 21/22 →
CG 10156 with the guard rate as a number, 23 → CG 10157 with the number (shown only if rate > 0).

### 4.3 Other flags

- **Defence shikigami** (Ｆ式神): the next damage the unit takes — attack, magic or counter
  received — becomes 0 and the shikigami is consumed (21147-21194, 23679-23728, 24450-24497);
  so an attacker's shikigami also blocks the counter it would receive. Sources: Defense
  Shikigami (one ally), Defend All Shikigami (all allies), item 70 at start.
- **Concentration** (Ｆ集中): +10 to assassination chance per Meditation, reset each battle.
- **Prepared action** (ＦＦ２/ＦＦ３): see ＦＦ = 1.
- **Moves**: Move Break removes current moves only if the target has some left (no floor
  check beyond that). Goddess of War's Majesty / Empress's Majesty remove 1 / 2 moves from every
  enemy at start and reset a result of exactly 0 to 1 — a result of −1 (Empress vs a unit with
  1 move) is left negative (a Sengoku Rance oddity, §8; its consequences **[I]**).

### 4.4 Side-wide effects

- **Advantage** (戦果): `戦果補正(oda, enemy, label)` adds points to one side
  (`あべ/T_SENKA.JAF.jam:7-19`); the bar compares the sides, so "lower the enemy's" is implemented
  as "raise ours". Skills: Advantage Up 1/2 (+8/+15), Advantage Down 1/2 (+5/+10),
  Goddess of War's Majesty / Empress's Majesty (+5/+10 at start), Instant Advantage (+10 at start);
  Witty Turnabout swaps both sides' damage totals and advantage corrections.
- **Side damage correction** (◆合戦補正１/２): Encampment Construction 1/2/3 adds +4/+6/+8; castles,
  mansions give the defender +20/+10 (plus advantage +40/+20; forts only advantage +10) (15879-16008). `合戦補正の補正` (33334-33492)
  nets the two values against each other and caps the result at 50 (02-resolution.md §6); `合戦／合戦補正` (27413-27539)
  multiplies a side's damage by 100 ± the net value %.
- **Battle time** (◆合戦ターン): Pass Time +2 / 2: +4, Extended Battle −4 / 2: −6, Instant Time Pass
  and Yomi Clock +5 at start.

## 5. The command menu next to the target (`あべ/T_BATSEL.JAF.jam`)

### 5.1 Building it

`tagBattlePlace@selectAction` (`あべ/T_BATPLS.JAF.jam:654-797`) creates a `tagBattleSelect` for the
acting unit and calls `addButton` once for **every unit on the field**, players 11–16 and enemies
21–26. `tagBattleSelect@addButton` (`あべ/T_BATSEL.JAF.jam:960-1140`):

```
for slot in 1..4:
    a = actor's skill in that slot
    if checkAction(a, thisUnit):
        special = (cost == 99)
        active  = special ? current moves > 0 : current moves >= cost
        button(a, special, active)              # one column of buttons beside this unit
if individual/dungeon battle and thisUnit is an ally other than the actor: button(999 Retreat)
```

`checkAction` (`あべ/T_BATSEL.JAF.jam:2070-2196`) decides **on which units a command appears**:

| command | appears on |
|---|---|
| Wait (type 0) | the actor only |
| Ally Guard / Neo Ally Guard | allies in the actor's row |
| All Guard | every ally |
| target 1 | enemy front-row units, and only if the actor is in the front row |
| target 6 | every enemy, only if the actor is in the front row |
| target 2, 5, 7 | every enemy |
| target 3, 8 | every ally (actor included) |
| target 4 | the actor only |

So the menu is a **per-target list of the actor's own slots**: clicking an enemy shows the attacks
that can reach it; clicking an ally shows heals and support; the actor's own column holds Wait and
self-skills. Each button is identified as `100000 + target × 1000 + action`
(`tagBattleButton@getButtonID` :829-839; decoded by `getTargetNo/getActionNo` :2027-2070), so one
click yields both command and target.

### 5.2 Greying out

A button is created **inactive** when the actor lacks the moves (above). An inactive button stays
visible; clicking it plays the cancel sound and does nothing (`tagBattlePlace@checkClick`,
`あべ/T_BATPLS.JAF.jam:3160-3277`). No other condition greys a button — commands that cannot reach a
target are not shown at all on that target.

### 5.3 Look and behaviour

- Buttons are stacked 38 px apart beside the unit; label = short name, 16 px Gothic, white with a
  coloured outline. CG/outline colour (`tagBattleButton@add` :148-544): player-side target —
  normal 10185 (outline 0,97,48 green), special 10181 (0,0,128 blue); enemy-side target — normal
  10186 (172,57,0 orange), special 10187 (150,0,0 red); hover sprite 10188/10189.
- Multi-target commands (targets 5–8) also prepare a marker (CG 10182/10180/10183/10184 plus the name)
  that, while the button is hovered, is drawn on **every other unit the command would hit**
  (line partner, or the whole side) (`tagBattleSelect@setSelect` :1237-1505).
- Hovering a command: the actor's move pips show the cost as "reserved" (all remaining for a
  special) (`tagBattlePlace@setSelect` :3349-3447 → `tagBattleWaza@set`,
  `あべ/T_BATWAZ.JAF.jam:126-284`: up to 6 pips, CG 10196 free / 10198 reserved / 10197 spent;
  enemies in individual battles show an "infinite" sprite 10217 instead); the turn-order arrow jumps
  to where the unit would act next (`順番_次(速度補正値(actor, delay))`); after 1 s a tooltip shows
  the description (`updateComment` :1835-1897; Wait's says that Space waits).
- Keys (`tagBattlePlace@checkKey` :3284-3337): **Space = Wait** at once; right button moves the
  cursor to the actor's Wait button.

## 6. Enemy command choice (summary; the full rule table is 01-flow.md §5.2)

`合戦敵_行動選択` (`あべ/C_合戦敵.JAF.jam:14-792`) walks a fixed priority list over the enemy's four
slots, each rule gated by `合戦敵_行動チェック` (moves enough, a front-row-only skill needs the user
in front, guards need a partner in the row, then a percent roll) — e.g. Masamune's Eyedrops / Full
Recover and Tenshi's Blessing at ≥ 50 % losses (80/70/80 %), Status Cancel when the player has ≥ 5 buffs (100 %) or ≥ 1
(50 %), an Ashigaru's slot-1 skill (its guard) under a row/unit-type condition (70 %), ninja vs a preparing unit (80 %), miko
heal at 50/25 % losses (90/60 %), Guard Cancel if any guard (70 %), Battle Tactics while ≥ 10 turns
remain (50 %), Witty Turnabout when behind (90 %), Pass Time when ahead (60 %), Defense Shikigami,
Assassinate (≥ 3 units, ≥ 10 turns, 50 %), preparation skills (≥ 15 turns, 80 %), then slot 4 (40 %),
slot 1 (20 %), slot 3 (100 %) skipping `行動ＴＸ = 1` skills, finally Wait.

## 7. Skill tables

Columns: **Type** (section 1), **Target** E1 one enemy melee / E one enemy / A one ally / S self /
EL enemy line / EL1 enemy line from the front row / EA all enemies / AA all allies; **Moves**
cost; **Delay**; **Dmg %** multiplier (or a plain number for support skills); **Counter %**;
**Adv.** 行動戦果; **Prep/Resist/PrepBreak**; **MoveBreak**; **Line** in
`多田/▲技能データ.jaf.jam`. Effects are from `合戦／戦闘計算` (20233-24611), `合戦／行動後処理`
(27963-30697) and `合戦／回復計算` (19698-20207) of `多田/関数／本編.jaf.jam`. Empty effect =
nothing beyond the formula and the columns.

#### Wait and self-preparation

| ID | Label | Type | Target | Moves | Delay | Dmg % | Counter % | Adv. | Prep/Resist/PrepBreak | MoveBreak | Line | Effect beyond the formula |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 1 | Wait | wait | S | 0 | 25 | - | - | - | -/-/- | - | 19 | Does nothing. Slot 2 of every default set; Space bar picks it. |
| 4 | Meditation | supp | S | 1 | 15 | - | - | - | -/-/- | - | 61 | Own Ｆ集中 +1; each point adds +10 to this unit's later assassinate/snipe chance (only use found). |
| 96 | Store Power | supp | S | 1 | 5 | - | - | - | -/-/- | - | 103 | Own attack buff := 70. Persists for the battle (no consumption found, although its description says the next attack). |
| 2 | Draw Attention | supp | S | 0 | 25 | - | - | - | -/-/- | - | 2257 | Own attack buff := 50; costs no moves. |
| 37 | Ninja Art: Leaf Camouflage | supp | S | 1 | 20 | - | - | - | -/-/- | - | 2989 | Own defence buff := 70. |

#### Samurai, knight, general melee

| ID | Label | Type | Target | Moves | Delay | Dmg % | Counter % | Adv. | Prep/Resist/PrepBreak | MoveBreak | Line | Effect beyond the formula |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 11 | Samurai Attack | phys | E1 | 1 | 15 | 100 | 25 | - | -/-/- | - | 145 | Basic samurai attack. |
| 182 | Samurai Attack 2 | phys | E1 | 1 | 15 | 110 | 25 | - | -/-/- | - | 199 | Upgrade of 11. |
| 215 | Guard Break | phys | E1 | 1 | 15 | 80 | 5 | - | -/-/- | - | 253 | If the target is in a guard state, damage x2 (cancels the guard halving). |
| 18 | Full Power Samurai Attack | phys | E1 | 2 | 20 | 150 | 50 | - | -/-/- | - | 307 | Strong hit, heavy counter. |
| 80 | Full Power Samurai Attack 2 | phys | E1 | 2 | 25 | 200 | 50 | - | -/-/- | - | 361 | Upgrade of 18. |
| 12 | Shouting Charge | phys | E1 | 2 | 20 | 120 | 25 | 3 | -/-/- | - | 415 | Advantage +3 is in the data but never applied (attacks skip 行動後処理) (oddity, §8). |
| 16 | Roundabout Charge | phys | E1 | 2 | 20 | 100 | 0 | - | -/-/- | - | 475 | No counterattack. |
| 19 | Mercy Charge | phys | E1 | 2 | 20 | 20 | 20 | - | -/-/- | - | 529 | Target's 戦場生死確率 -10, Ｆ生死処理 := 2 (better capture after battle). |
| 168 | Gamble Charge | phys | E1 | all (special) | 10 | 100 | 50 | - | -/-/- | - | 583 | Damage x0.5/1/1.5/2/2.5 (RAND(5)). |
| 169 | Gamble Charge/Love | phys | E1 | all (special) | 10 | 100 | 50 | - | -/-/- | - | 637 | Damage x1/1.5/2/2.5/3 (RAND(5)). |
| 76 | Knight Attack | phys | E1 | 1 | 15 | 100 | 25 | - | -/-/- | - | 5581 | Basic knight attack. |
| 8 | Royal Guard Charge | phys | E1 | 2 | 20 | 100 | 10 | - | -/-/- | - | 5635 | x2 if the user has fewer troops than the target. |

#### Hero (type 3)

| ID | Label | Type | Target | Moves | Delay | Dmg % | Counter % | Adv. | Prep/Resist/PrepBreak | MoveBreak | Line | Effect beyond the formula |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 90 | Rance Attack | hero | E1 | all (special) | 15 | 100 | 0 | 5 | -/-/- | - | 691 | Commander strike: fixed 20 + 3 x Sレベル troops; if the target survives, the unit charges at 100% (no counter). Advantage +5 never applied. |
| 95 | Kentarou-kun Slash | hero | E1 | all (special) | 15 | 100 | 0 | 5 | -/-/- | - | 751 | As 90. |
| 92 | Commander Charge | hero | E1 | all (special) | 15 | 100 | 0 | 5 | -/-/- | - | 811 | As 90. |

#### Ashigaru

| ID | Label | Type | Target | Moves | Delay | Dmg % | Counter % | Adv. | Prep/Resist/PrepBreak | MoveBreak | Line | Effect beyond the formula |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 131 | Ashigaru Attack | phys | E1 | 1 | 15 | 100 | 25 | - | -/-/- | - | 997 | Basic ashigaru attack. |
| 184 | Ashigaru Attack 2 | phys | E1 | 1 | 15 | 120 | 25 | - | -/-/- | - | 1051 | Upgrade of 131. |
| 132 | Lancer Legion | phys | E1 | 2 | 15 | 200 | 25 | - | -/-/- | - | 1105 | x2. |
| 138 | Lancer Legion 2 | phys | E1 | 3 | 15 | 300 | 25 | - | -/-/- | - | 1159 | x3, 3 moves. |
| 26 | Ally Avenger | phys | E1 | all (special) | 20 | 50 | 0 | - | -/-/- | - | 1213 | 50% + RAND(damage the user's side has taken this battle). |
| 27 | Pillage | phys | E1 | 1 | 20 | 80 | 40 | - | -/-/- | - | 1267 | Player only: battle loot := 総国力(1) x 10 (payout after the battle not traced). |
| 21 | Ally Guard | supp | S | 1 | 20 | - | - | - | -/-/- | - | 871 | Guard state 21; guard rate += 知力x20 (solo +80). Covers allies in the user's row. |
| 22 | Neo Ally Guard | supp | S | 1 | 20 | - | - | - | -/-/- | - | 913 | Guard state 22; guard rate += 知力x30 (solo +150). Covers the user's row. |
| 23 | All Guard | supp | S | 1 | 20 | - | - | - | -/-/- | - | 955 | Guard state 23; guard rate += 知力x10 (solo +100). Covers all allies. |

#### Cavalry

| ID | Label | Type | Target | Moves | Delay | Dmg % | Counter % | Adv. | Prep/Resist/PrepBreak | MoveBreak | Line | Effect beyond the formula |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 13 | Cavalry Charge | phys | E | 1 | 20 | 100 | 25 | - | -/-/- | 0 | 1417 | Basic cavalry attack (Move Break field 0 = none). |
| 165 | Cavalry Charge 2 | phys | E | 1 | 20 | 110 | 25 | - | -/-/- | 0 | 1477 | Upgrade of 13. |
| 133 | Advance | phys | E | 2 | 20 | 130 | 25 | - | -/-/- | 1 | 1537 | Move Break 1. |
| 89 | Backline Crusher | phys | E | 2 | 20 | 100 | 25 | - | -/-/- | - | 1597 | x2 vs Archer, Onmyouji, Miko, Tactician. |
| 15 | Pursuit | phys | E | all (special) | 20 | 100 | 0 | - | -/-/- | - | 1651 | x3 if the user's side leads in advantage (戦果). |

#### Archer

| ID | Label | Type | Target | Moves | Delay | Dmg % | Counter % | Adv. | Prep/Resist/PrepBreak | MoveBreak | Line | Effect beyond the formula |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 31 | Bow Attack | phys | E | 1 | 20 | 100 | 0 | - | -/-/- | - | 1705 | Basic bow attack. |
| 30 | Neo Bow Attack | phys | E | 1 | 20 | 110 | 0 | - | -/-/- | - | 1759 | Upgrade of 31. |
| 171 | Yamamoto Arrow Barrage | phys | EL | 1 | 20 | 90 | 0 | - | -/-/- | - | 1813 | Line pair, 90%. |
| 151 | Enhanced Bow Attack | phys | E | 3 | 20 | 200 | 0 | - | -/-/- | - | 1867 | x2, 3 moves. |
| 139 | Ashigaru Target | phys | E | 2 | 10 | 70 | 0 | - | -/-/- | - | 1921 | x4 vs Ashigaru (x2.8 net; text says triple). |
| 140 | Ninja Target | phys | E | 2 | 10 | 70 | 0 | - | -/-/- | - | 1975 | x4 vs Ninja (x2.8 net). |
| 152 | Amateur Volley | phys | EA | all (special) | 30 | 40 | - | - | 1/1/- | - | 2029 | All enemies, prep 1 (resist 1). |
| 87 | Volley | phys | EA | all (special) | 30 | 50 | - | - | -/-/- | - | 2089 | All enemies. |
| 7 | Precision Shot | phys | EA | all (special) | 25 | 30 | 0 | - | -/-/1 | 1 | 2137 | All enemies; Prep Break 1 and Move Break 1 on each hit. |
| 91 | Piercing Tempest | phys | E | 2 | 10 | 0 | 0 | - | -/-/- | - | 2203 | No damage; kill chance 45 on the target commander. |

#### Musketeer / cannoneer

| ID | Label | Type | Target | Moves | Delay | Dmg % | Counter % | Adv. | Prep/Resist/PrepBreak | MoveBreak | Line | Effect beyond the formula |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 32 | Shoot | phys | E1 | 1 | 30 | 100 | 0 | - | -/-/- | - | 2299 | Basic musket shot. |
| 170 | Shoot + Snipe | phys | E1 | 1 | 30 | 100 | 0 | - | -/-/- | - | 2353 | Damage + kill chance 40. |
| 59 | Penetration Shot | phys | EL1 | 1 | 40 | 50 | 0 | - | -/-/- | - | 2407 | Line pair, user in front row. |
| 153 | Penetration Shot 2 | phys | EL1 | 2 | 40 | 70 | 0 | - | -/-/- | - | 2461 | Upgrade of 59 (Power Up needs 技数基礎値 > 1). |
| 29 | Commander Snipe | phys | E1 | all (special) | 30 | 1 | 0 | - | -/-/- | - | 2515 | No damage; kill chance 45. |
| 83 | Finisher: Destroyer of Provinces | phys | EA | all (special) | 30 | 100 | 0 | - | -/-/- | - | 2569 | All enemies, 100%. |
| 36 | Tulip | phys | E | 1 | 30 | 100 | 0 | - | -/-/- | - | 5689 | Basic Tulip shot. |
| 9 | Tulip Rapidfire | phys | E | all (special) | 30 | 200 | 0 | - | -/-/- | - | 5743 | x2, special. |

#### Ninja

| ID | Label | Type | Target | Moves | Delay | Dmg % | Counter % | Adv. | Prep/Resist/PrepBreak | MoveBreak | Line | Effect beyond the formula |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 33 | Shuriken | phys | E | 1 | 20 | 100 | 0 | - | -/-/1 | - | 3175 | Prep Break 1. |
| 157 | Shuriken 2 | phys | E | 1 | 20 | 110 | 0 | - | -/-/1 | - | 3235 | Upgrade of 33. |
| 159 | Chirp Chirp Shuriken | phys | EL | 2 | 20 | 90 | 0 | - | -/-/1 | - | 3295 | Line pair, Prep Break 1. |
| 34 | Sonic Shuriken | phys | E | 2 | 2 | 100 | 0 | - | -/-/1 | - | 3355 | Delay 2, Prep Break 1. |
| 85 | Assassinate | phys | E | all (special) | 25 | 0 | 0 | - | -/-/- | - | 3415 | No damage; kill chance 45. |
| 86 | Assassinate 2 | phys | E | all (special) | 25 | 0 | 0 | - | -/-/- | - | 3475 | No damage; kill chance 50. |
| 154 | Ninja Art: Encampment Construction 1 | supp | S | 1 | 10 | 4 | - | - | -/-/- | - | 3031 | Own side 合戦補正 +4 (damage multiplier, netted against the enemy's). |
| 155 | Ninja Art: Encampment Construction 2 | supp | S | 1 | 10 | 6 | - | - | -/-/- | - | 3079 | As 154, +6. |
| 156 | Ninja Art: Encampment Construction 3 | supp | S | 1 | 10 | 8 | - | - | -/-/- | - | 3127 | As 154, +8. |
| 94 | 101 Doggies | phys | EA | all (special) | 15 | 0 | 0 | - | -/-/- | - | 3535 | Fixed 101 to every enemy. |
| 158 | 201 Doggies | phys | EA | all (special) | 15 | 0 | 0 | - | -/-/- | - | 3589 | Fixed 201 to every enemy. |

#### Miko and healing

| ID | Label | Type | Target | Moves | Delay | Dmg % | Counter % | Adv. | Prep/Resist/PrepBreak | MoveBreak | Line | Effect beyond the formula |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 50 | Miko Dance | heal | A | 1 | 20 | 50 | - | - | -/-/- | - | 2623 | Heal one ally. |
| 52 | Neo Miko Dance | heal | A | 1 | 20 | 100 | - | - | -/-/- | - | 2671 | Upgrade of 50. |
| 141 | Miko Dance/Wind | heal | A | 1 | 5 | 100 | - | - | -/-/- | - | 2719 | Upgrade of 51; delay 5. |
| 51 | Healing Fog | heal | AA | 2 | 30 | 50 | - | - | -/-/- | - | 2767 | Heal all allies 50%. |
| 58 | Tenshi's Blessing | heal | S | 1 | 15 | 200 | - | - | -/-/- | - | 3703 | Self heal 200%. |
| 126 | Tenshi's Blessing 2 | heal | S | 2 | 15 | 400 | - | - | -/-/- | - | 3757 | Self heal 400%. |
| 56 | Monster Healing | heal | A | 1 | 20 | 100 | - | - | -/-/- | - | 8413 | Heal one ally. |
| 81 | Masamune's Eyedrops | heal | S | 1 | 20 | 500 | - | - | -/-/- | - | 8461 | Self heal: fixed 999 in individual battles; in an army battle only 84 is fixed, so 81 heals the formula at 500 % (3.6). |
| 84 | Full Recover | heal | A | 1 | 20 | 50 | - | - | 2/2/- | - | 9943 | Fixed 999 heal; prep 2 (resist 2). |
| 163 | Use Snake Grease | heal | S | 0 | 10 | 8000 | - | - | -/-/- | - | 8887 | Heal 9999 and refill moves; the user's skill 1 is erased (once). |
| 166 | S Healing | heal | A | 1 | 20 | 200 | - | - | -/-/- | - | 8365 | Heal; on Rance (char 1000) also +1 current move. |
| 28 | Death by Poison | phys | EA | all (special) | 20 | - | - | - | 2/2/- | - | 2815 | Every enemy loses half its current troops (formula replaced); prep 2 (resist 2). |
| 67 | Miko Storm 2 | mag | EA | all (special) | 5 | 0 | - | - | 1/1/- | - | 2869 | Every enemy loses 30% of its max troops; prep 1 (resist 1). |
| 97 | Miko Storm | mag | EA | all (special) | 5 | 0 | - | - | 1/1/- | - | 2929 | Every enemy loses 10% of its max troops; prep 1 (resist 1). |

#### Sohei

| ID | Label | Type | Target | Moves | Delay | Dmg % | Counter % | Adv. | Prep/Resist/PrepBreak | MoveBreak | Line | Effect beyond the formula |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 10 | Sohei Charge | phys | E1 | 1 | 20 | 100 | 30 | - | -/-/- | - | 3919 | Removes the first nonzero of target's atk/def/int/spd buffs. |
| 35 | Neo Sohei Charge | phys | E1 | 1 | 20 | 120 | 30 | - | -/-/- | - | 3973 | Upgrade of 10. |
| 142 | Youkai Purge | phys | E1 | 1 | 15 | 100 | 0 | - | -/-/- | - | 4027 | x3 when the enemy nation (◆戦場対戦国) is 16. |
| 181 | Haniwa Splitter | phys | E | 1 | 20 | 50 | 0 | - | -/-/- | - | 4081 | Fixed 6000 vs nation 17 or chars 1228/1220/1240 (hannies); else 50%. |
| 73 | Guard Cancel | supp | EA | 1 | 10 | - | - | - | -/-/- | - | 4135 | All enemies with guard rate != 0 lose guard state and guard rate. |
| 93 | Reaper Scythe | phys | EA | all (special) | 10 | 0 | 0 | - | 2/1/- | - | 4183 | Every enemy loses 25% of its max troops; prep 2 (resist 1). |
| 160 | Extended Battle | supp | EA | 2 | 20 | - | - | - | -/-/- | - | 4249 | Battle turn -4 (night later). |
| 161 | Extended Battle 2 | supp | EA | 2 | 20 | - | - | - | -/-/- | - | 4291 | Battle turn -6. |
| 174 | Action Transfer | supp | A | 1 | 15 | 100 | - | - | -/-/- | - | 3811 | Target ally: current moves +1 (cap at max), delay +6 on its next registration, re-queued. Oddity (§8): may reset the USER's state. |
| 175 | Action Transfer 2 | supp | A | 2 | 15 | 100 | - | - | -/-/- | - | 3865 | As 174 with +2 moves. |
| 17 | Persuade | phys | E1 | 2 | 30 | 10 | 0 | - | -/-/- | - | 3643 | 10% damage; the damage is taken from the target's max troops and added to the user's. |

#### Onmyouji

| ID | Label | Type | Target | Moves | Delay | Dmg % | Counter % | Adv. | Prep/Resist/PrepBreak | MoveBreak | Line | Effect beyond the formula |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 3 | Defense Shikigami | supp | A | 1 | 20 | - | - | - | -/-/- | - | 4333 | Ally gets a defence shikigami: the next damage it takes (attack or counter) is 0. |
| 143 | Defend All Shikigami | supp | AA | all (special) | 20 | - | - | - | -/-/- | - | 4375 | Defence shikigami on all allies. |
| 40 | Shikigami | mag | EA | 1 | 25 | 100 | 0 | - | 1/-/- | - | 4417 | All enemies; prep 1 (resist 0 -> cannot be broken [I]). |
| 162 | Shikigami 2 | mag | EA | 1 | 25 | 110 | 0 | - | 1/-/- | - | 4477 | Upgrade of 40. |
| 41 | High Grade Shikigami | mag | EA | all (special) | 35 | 150 | - | - | 1/1/- | - | 4537 | All enemies; prep 1 (resist 1). |
| 42 | Summon Oni | mag | E | 1 | 20 | 50 | 0 | - | -/-/- | - | 4597 | Single. |
| 43 | Neo Summon Oni | mag | E | 2 | 20 | 100 | 0 | - | -/-/- | - | 4651 | Upgrade of 42. |
| 48 | Souun's Summoning | mag | E | 1 | 25 | 100 | - | - | -/-/- | - | 4705 | Single. |
| 49 | Summon Suzaku | mag | EA | all (special) | 15 | 200 | 0 | - | -/-/- | - | 4753 | All enemies 200%. |

#### Tactician

| ID | Label | Type | Target | Moves | Delay | Dmg % | Counter % | Adv. | Prep/Resist/PrepBreak | MoveBreak | Line | Effect beyond the formula |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 135 | Battle Tactics (Single) | supp | A | 1 | 20 | - | - | - | -/-/- | - | 4807 | Target ally: one random stat buff (only if that stat has none) of 知力x5 (solo: Sレベル). |
| 60 | Battle Tactics (All) | supp | A | 2 | 20 | - | - | - | -/-/- | - | 4855 | Target ally: all four buffs := 知力x5 (solo: Sレベル). |
| 61 | Battle Tactics III | supp | AA | 1 | 15 | - | - | - | -/-/- | - | 4903 | (troops/100)+2 boosts of 知力x5 to random allies. |
| 62 | Battle Tactics II | supp | AA | 1 | 15 | - | - | - | -/-/- | - | 4951 | (troops/200)+2 boosts. |
| 63 | Battle Tactics I | supp | AA | 1 | 15 | - | - | - | -/-/- | - | 4999 | (troops/300)+2 boosts. |
| 70 | Status Cancel I | supp | EA | 1 | 10 | - | - | - | -/-/- | - | 5047 | Clears all buffs of up to 2 random buffed enemies. |
| 71 | Status Cancel II | supp | EA | 1 | 10 | - | - | - | -/-/- | - | 5095 | Up to 3. |
| 72 | Status Cancel III | supp | EA | 1 | 10 | - | - | - | -/-/- | - | 5143 | Up to 4. |
| 5 | Pass Time | supp | EA | 2 | 20 | - | - | - | -/-/- | - | 5191 | Battle turn +2 (night sooner). |
| 150 | Pass Time 2 | supp | EA | 2 | 20 | - | - | - | -/-/- | - | 5233 | Battle turn +4. |
| 38 | Advantage Down 1 | supp | EA | 1 | 20 | 5 | - | - | -/-/- | - | 5275 | Own advantage +5 (the bar is relative). |
| 39 | Advantage Down 2 | supp | EA | 1 | 20 | 10 | - | - | -/-/- | - | 5323 | Own advantage +10. |
| 66 | Witty Turnabout | supp | AA | all (special) | 30 | - | - | - | 1/1/- | - | 5371 | Swaps both sides' damage totals and advantage corrections; if ＧＦ[160] > 2 the user's skill 1 becomes 63 for good. |
| 25 | Advantage Up 1 | supp | AA | 2 | 15 | - | - | 8 | -/-/- | - | 1321 | Own advantage +8. |
| 20 | Advantage Up 2 | supp | AA | 2 | 15 | - | - | 15 | -/-/- | - | 1369 | Own advantage +15. |

#### Mage

| ID | Label | Type | Target | Moves | Delay | Dmg % | Counter % | Adv. | Prep/Resist/PrepBreak | MoveBreak | Line | Effect beyond the formula |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 45 | Fire Blast | mag | E | 1 | 35 | 100 | 0 | - | -/-/- | - | 5431 | Single magic. |
| 46 | Angel Cutter | mag | EA | all (special) | 35 | 50 | - | - | -/-/- | - | 5485 | All enemies; x2 vs Sohei, x2 vs monster types 27/28/29/31. |
| 47 | White Destruction Beam | mag | EL | all (special) | 25 | 150 | - | - | -/-/- | - | 5533 | Line pair. |

#### Others (animal, peasant, soul bound, oni, unique)

| ID | Label | Type | Target | Moves | Delay | Dmg % | Counter % | Adv. | Prep/Resist/PrepBreak | MoveBreak | Line | Effect beyond the formula |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 55 | Hanny Attack | phys | E | 1 | 15 | 100 | 0 | - | -/-/- | - | 5797 | Basic hanny attack. |
| 203 | Animal Attack | phys | E1 | 1 | 15 | 100 | 25 | - | -/-/- | - | 5851 | Basic animal attack. |
| 14 | Uprising | phys | E1 | 1 | 20 | 100 | 80 | - | -/-/- | - | 5905 | Peasant attack, counter 80%. |
| 69 | Infect | phys | E1 | 1 | 40 | 100 | 50 | - | -/-/- | - | 5959 | Damage also cuts target's max troops (min 1). |
| 205 | Oni Attack | phys | E1 | 1 | 20 | 100 | 30 | - | -/-/- | - | 6013 | Oni attack. |
| 44 | Thunderstrike | mag | EA | 1 | 25 | 50 | 0 | - | -/-/- | - | 6067 | All enemies 50%. |
| 180 | Great Thunderstrike | mag | EA | 1 | 10 | 150 | 0 | - | 2/2/- | - | 6121 | All enemies 150%; prep 2 (resist 2). |
| 68 | Begone Bomb | phys | E | 1 | 5 | 500 | 0 | - | -/-/- | - | 6187 | Fixed 9000. |
| 110 | Atomic Explosion | phys | EA | 1 | 40 | 5000 | 0 | - | 4/-/- | - | 7213 | All enemies 5000%; prep 4. |
| 179 | Motonari Punch | phys | E1 | 1 | 15 | 100 | 0 | - | -/-/- | - | 8833 | Fixed 200. |
| 148 | Hibachi Charge | phys | E1 | 2 | 10 | 200 | 0 | - | -/-/- | 0 | 10171 | Fixed 187. |

#### Monster and boss moves

| ID | Label | Type | Target | Moves | Delay | Dmg % | Counter % | Adv. | Prep/Resist/PrepBreak | MoveBreak | Line | Effect beyond the formula |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 6 | S Basic Monster Attack | phys | E1 | 1 | 15 | 100 | 0 | - | -/-/- | - | 8779 |  |
| 57 | S Rommel-type Spell Attack | mag | E | 1 | 20 | 100 | 0 | - | -/-/- | - | 8935 |  |
| 204 | S Normal Single Target Monster Magic | mag | E | 1 | 20 | 100 | 0 | - | -/-/- | - | 8989 |  |
| 74 | S Zombie Attack | phys | E1 | 1 | 20 | 100 | 50 | - | -/-/- | - | 9043 |  |
| 77 | All-Targeting Monster Magic | mag | EA | 1 | 20 | 60 | 0 | - | 1/1/- | - | 9097 | All enemies; prep 1 (resist 1). |
| 178 | Shogun Whirlwind | mag | EA | 1 | 20 | 80 | 0 | - | 3/3/- | - | 9163 | All enemies; prep 3 (resist 3). |
| 64 | S Flamethrower | phys | EL | 1 | 30 | 100 | 0 | - | -/-/- | - | 9229 |  |
| 65 | S Nyo | phys | EA | 1 | 30 | 50 | 0 | - | 1/1/- | 1 | 9283 | All enemies; prep 1 (resist 1), Move Break 1. |
| 75 | Fiend Slash | phys | E1 | 1 | 18 | 100 | 25 | - | -/-/- | - | 9355 |  |
| 98 | Black Flame | mag | EA | 1 | 15 | 30 | 0 | - | -/-/- | - | 9409 |  |
| 99 | Sandal Shuriken | phys | E | 1 | 5 | 100 | 0 | - | -/-/- | 1 | 9463 | Move Break 1 and removes one buff (as 10). |
| 130 | Byakko Fang | phys | E1 | 1 | 5 | 100 | 0 | - | -/-/- | 1 | 9523 | Move Break 1. |
| 164 | Apostle Thunderstrike | mag | EA | 1 | 10 | 100 | 0 | - | 1/3/- | - | 9583 | All enemies; prep 1 (resist 3). |
| 177 | Seven Head Claws | phys | E1 | 1 | 50 | 40 | 0 | - | -/-/- | - | 9649 |  |
| 134 | Gigai Punch | phys | E1 | 1 | 10 | 100 | 0 | - | -/-/- | 1 | 9703 | Move Break 1. |
| 78 | Orochi Beam | phys | EL | 1 | 15 | 400 | 0 | - | -/-/- | 0 | 9763 | Line pair 400%. |
| 79 | Grand Earthquake | phys | EA | 1 | 10 | 2000 | 0 | - | -/-/- | 1 | 9823 | Move Break 1; own animation when AoE effect option is off. |
| 82 | Xavier Slash | phys | E1 | 1 | 10 | 100 | 0 | - | -/-/- | 1 | 9883 | Move Break 1. |
| 88 | Petrify | phys | E1 | 1 | 10 | 100 | 0 | - | -/-/- | 1 | 10003 | Move Break 1. |
| 144 | S Okayu | phys | EL | 1 | 10 | 100 | 0 | - | -/-/- | - | 10063 |  |
| 145 | Sekimei Sword | phys | E | 1 | 2 | 50 | 0 | - | -/-/- | - | 10117 | Removes one buff (as 10). |
| 167 | Ieyasu Tokugawa's Special Move | phys | EL1 | 1 | 10 | 200 | 0 | - | 2/2/- | - | 10231 | Line pair (front row); prep 2 (resist 2). |
| 207 | Guna Beam | phys | EA | 1 | 20 | 100 | 0 | - | -/-/- | - | 10297 |  |
| 208 | S Crossbow Shot | phys | E | 1 | 20 | 150 | 0 | - | -/-/- | - | 10351 |  |
| 209 | Conturtle Attack | phys | E | 1 | 20 | 100 | 0 | - | -/-/- | - | 10405 |  |
| 212 | Bellows Attack | phys | E | 1 | 20 | 100 | 0 | - | -/-/- | - | 10459 |  |

#### Individual-battle (S) set

| ID | Label | Type | Target | Moves | Delay | Dmg % | Counter % | Adv. | Prep/Resist/PrepBreak | MoveBreak | Line | Effect beyond the formula |
|---|---|---|---|---|---|---|---|---|---|---|---|---|
| 185 | S Sword Attack | phys | E1 | 1 | 15 | 100 | 0 | - | -/-/- | - | 7273 |  |
| 186 | S Strong Sword Attack | phys | E1 | all (special) | 15 | 150 | 0 | - | -/-/- | - | 7327 |  |
| 210 | S Cavalry Charge | phys | E | 1 | 15 | 100 | 0 | - | -/-/- | - | 7381 |  |
| 211 | S Strong Cavalry Charge | phys | E | all (special) | 15 | 150 | 0 | - | -/-/- | - | 7435 |  |
| 172 | S Rance Attack | phys | E1 | all (special) | 15 | 200 | 0 | - | -/-/- | - | 7489 |  |
| 173 | S Kentarou-kun Slash | phys | E1 | all (special) | 15 | 200 | 0 | - | -/-/- | - | 7543 |  |
| 187 | S Spear Attack | phys | E1 | 1 | 15 | 100 | 0 | - | -/-/- | - | 7597 |  |
| 188 | S Strong Spear Attack | phys | E1 | all (special) | 15 | 150 | 0 | - | -/-/- | - | 7651 |  |
| 190 | S Bow Attack | phys | E | 1 | 20 | 100 | 0 | - | -/-/- | - | 7705 |  |
| 191 | S Strong Bow Attack | phys | E | all (special) | 20 | 150 | 0 | - | -/-/- | - | 7759 |  |
| 192 | S Arquebus Shot | phys | E1 | 1 | 30 | 100 | 0 | - | -/-/- | - | 7813 |  |
| 206 | S Rengoku Shot | phys | E1 | 1 | 30 | 100 | 0 | - | -/-/- | - | 7867 |  |
| 193 | S Strike Attack | phys | E1 | 1 | 30 | 50 | 0 | - | -/-/- | - | 7921 |  |
| 194 | S Strong Strike Attack | phys | E1 | all (special) | 30 | 75 | 0 | - | -/-/- | - | 7975 |  |
| 196 | S Shuriken | phys | E | 1 | 20 | 100 | 0 | - | -/-/1 | - | 8029 |  |
| 197 | S Strong Shuriken | phys | E | all (special) | 20 | 150 | 0 | - | -/-/1 | - | 8089 |  |
| 198 | S Shikigami Blast | mag | E | 1 | 20 | 100 | 0 | - | -/-/- | - | 8149 |  |
| 199 | S Strong Shikigami Blast | mag | E | all (special) | 20 | 150 | 0 | - | -/-/- | - | 8203 |  |
| 200 | S Fire Blast | mag | E | 1 | 35 | 100 | 0 | - | -/-/- | - | 8257 |  |
| 201 | S Angel Cutter | mag | E | all (special) | 35 | 150 | 0 | - | -/-/- | - | 8311 |  |
| 189 | S Stab Attack | phys | E1 | 1 | 10 | 100 | 0 | - | -/-/- | - | 8509 |  |
| 195 | S Tulip | phys | E | 1 | 30 | 100 | 0 | - | -/-/- | - | 8563 |  |
| 53 | S Animal Attack | phys | E1 | 1 | 15 | 100 | 0 | - | -/-/- | - | 8617 |  |
| 54 | S Strong Animal Attack | phys | E1 | all (special) | 15 | 200 | 25 | - | -/-/- | - | 8671 |  |
| 202 | S Hanny Flash | phys | E | 1 | 15 | 100 | 0 | - | -/-/- | - | 8725 |  |


#### Passives (slot 技能; type 11, Majesties type 19, Unity type 9)

Data lines: `多田/▲技能データ.jaf.jam:6241-7189`. Battle effects (`多田/関数／本編.jaf.jam`):

| ID | Label | Battle effect (where) |
|---|---|---|
| 100 / 101 / 102 | Auto-Heal (Sill) / 1 / 2 | heal ＧＦ[33]/[34]/[35] troops (30/30/50 initially) after each own action (`あべ/C_合戦.JAF.jam:2975-3164`) |
| 103 | Goddess of War's Majesty | at start: own advantage +5, every enemy −1 current move (0 → 1) (16834-16973) |
| 104 | Empress's Majesty | at start: +10, every enemy −2 moves (only an exact 0 is reset to 1) (16984-17120) |
| 106 | Absolute Magic Defense | magic damage taken = 0 (21030-21050) |
| 107 / 112 | Hibachi / Lover | army battle: +50 / +30 flat physical damage (21991-22062) |
| 108 / 109 | Counterattack Rate Up 1 / 2 | own counters ×2 / ×3 (24384-24440) |
| 114 / 115 / 116 | Field / Castle Siege / Dungeon Battle Talent | ×1.4 attack damage on battlefield type 1 / 3 / 7 (20909-21019, 22184-22295) |
| 119–122 | Attack/Defense/Intelligence/Speed Luck | at start, 2 in 3 chance of +50 in that buff (16395-16516) |
| 123 | Miko at Work | at start, max(1, troops / 250) male units of the own side, in slot order, get attack buff := 40 (overwrites) (16595-16763) |
| 124 | First Strike | at start the unit is spared the +10 extra delay that randomly drawn units get on their first registration (17662-17803) |
| 125 | Unity | when a hit would wipe out the unit and it has > 1 troop: 50 % chance it keeps 1 (21061-21117, 23739-23795) |
| 127 | Enhanced Initial Guard | starts in guard state 21 with guard rate 120 (16149-16170) |
| 128 / 129 | Triplets / Furinkazan | at start all four buffs := 50 (16527-16584) |
| 136 | Instant Time Pass | army battle: battle turn +5 at start (16774-16791) |
| 137 | Instant Advantage | own advantage +10 at start (16802-16823) |
| 213 | Assassination Defense | −10 to assassination chances against its side (23076-23083) |
| 216 | Onmyou Defense | −10 in the magic formula against its side (26066-26073) |
| 220 | Assassinate Skill+ | assassination certain against a non-important target with 0 moves (23229-23251) |
| 105, 111, 113, 117, 118, 146, 147, 149, 214, 217, 218, 219 | Fan Club, Fleeing Warrior Hunt, Pursuit Battle, Merchant, Money Management, Dungeon Scouter, Healthy, Ending Points, Effective Advertising, Twin Action/Recon Lord, Sticky Ground | strategic or post-battle; not in the battle code read (219 and 214 appear in `Ｐ合戦／戦闘結果`, 13944, 14290) |

Passive 176 has no data entry; at battle start it gives units 21–26 all four buffs 30 (17130-17208).

## 8. Sengoku Rance oddities and dead data found

Sengoku Rance's own oddities, for the design of our screen; they are not entries of the project's
quirk register (docs/quirks.md). 01-flow.md §6 and 02-resolution.md's last section list the
others.

1. **Advantage of attacks never applied**: Shouting Charge (+3), Rance Attack, Kentarou-kun Slash,
   Commander Charge (+5) have 行動戦果, but only `合戦／行動後処理` applies it and the attack paths
   never call it (callers: `あべ/C_合戦.JAF.jam:1643, 1740, 1793, 1845, 1880, 1959`; the accessor
   `行動_戦果`, `あべ/C_行動.jaf.jam:547`, is never called).
2. **Store Power**'s description promises a one-attack boost; the buff lasts the whole battle.
3. **Ashigaru Target / Ninja Target**'s description says triple; the code does ×4 on a 70 % skill
   (×2.8).
4. **Field Battle Talent** and the field book check battlefield type 1 only (logged as 砦 "fort");
   the open-field type 0 gets nothing.
5. **Action Transfer** (174/175): when the target is out (ＦＦ 99) with 0 moves and still has HP and
   troops, the code resets the **user's** ＦＦ, not the target's (`行動後処理` 28301-28369).
6. **Defence state (ＦＦ 3)** is set only for action 6 in a function attacks never reach; **confusion
   (ＦＦ 2)** is never set; neither has an effect.
7. **S Healing** (166) Ｆ集中 +1 in `行動後処理` is unreachable (single heals skip it).
8. **Prep resistance 0** (Shikigami, Shikigami 2): Prep Break drives ＦＦ３ negative, so the
   preparation cannot be cancelled **[I]**.
9. **Guard rate** can go negative (−40 per hit, no floor) and stacks on re-use.
10. **Empress's Majesty** can leave an enemy at −1 moves.
11. **Golden Mask** (69) halves damage when the wearer (the defender) is a monster type — probably
    meant for the attacker's type **[I]**.

## 9. Open questions

1. Which reserve `Ｓ補充` (`多田/関数／本編.jaf.jam:31264`) brings in for a Retreat in an
   individual/dungeon battle (3.8; `tagBattlePlace@hoju`, `あべ/T_BATPLS.JAF.jam:3975-4071`).
2. Pillage loot (◆合戦略奪金) and Sticky Ground's capture effect: post-battle code
   (`合戦／勝利処理`, `Ｐ合戦／戦闘結果`) not read. (Mercy Charge's `Ｆ生死処理 = 2` makes the
   capture certain and ignores the 2-casualty cap: 02-resolution.md §8.4, 01-flow.md §4.3.)
3. Power Up branches carry extra conditions (character checks around `技能３処理` 2955-3049,
   `技能４処理` 4146-4414, `技能５処理` 4502-4627) not decoded; the price of a Power Up not read.
4. Who owns Miko Storm 2, Snake Grease, Masamune's Eyedrops etc.: commander data (`ＣＨＢ` calls in
   `多田/▲武将データ.jaf.jam`), not tabulated.
5. ＧＦ[160] (Witty Turnabout's condition) and nation numbers 16/17 (Youkai Purge, Haniwa Splitter)
   not identified.

(Settled: `RAND(n)` and `乱数(n)` both return 1..n — 01-flow.md §2.2, 02-resolution.md §3.8.)
