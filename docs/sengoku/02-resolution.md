# Sengoku Rance battle survey, part 2: units and combat resolution

This part covers what a unit is in a Sengoku Rance army battle, the troop types, the damage
formulas (physical, magic, troop scaling, counterattack), guard and defence, the battle-wide
modifiers, healing and buffs, and what a battle does to the commanders afterwards. It is one of
four reference surveys for **our own battle screen "in the manner of Sengoku Rance"**, not rules
for the port — Kichikuou's rules stay unchanged. The conventions (source references relative to
`ain\files\` under `SENGOKU_RANCE_EXTRACT_DIR`, places 1-6 with 1-3 the front row, "row" and
"line", **[I]**) are in [01-flow.md](01-flow.md#conventions-shared-by-the-four-parts); the turn
order and the battle's end are in 01-flow.md, the actions one by one in
[03-skills.md](03-skills.md), the screen in [04-screen.md](04-screen.md). Most of the damage
arithmetic is in `多田/関数／本編.jaf.jam`, the action dispatcher in `あべ/C_合戦.JAF.jam`. The
English strings quoted are the MangaGamer release's (the dump has the English text).

---

## 1. What a unit in battle is

### 1.1 The record

There is no separate battle-unit record. A unit in battle **is a `sキャラ` record** (a commander and
the troops under them; `structures.txt` struct `sキャラ`) held in the global 2-D array
`gs[ii][slot]`. The battle uses `ii = 0`, with two blocks of six slots:

- **slots 11-16**: the Oda (player) side, places 1-6 (slot = 10 + place);
- **slots 21-26**: the enemy side, places 1-6 (slot = 20 + place).

Evidence: 多田/関数／本編.jaf.jam:15623-15779 (合戦／初期化 loops `loop = 11..26`, `iic = 11..16`, `iic = 21..26`);
合戦兵力取得 at 多田/関数／本編.jaf.jam:27642/27684. Every rule addresses `gs[0][slot].field`. The `あべ` structs are for
drawing only: `tagBattleBusho`, `tagBattleHp`, `tagBattleLevel`, `tagBattleUnit` and `tagUnitData`
hold sprites, positions, CG numbers and the `m_nHosei` drawing offset (structures.txt 1403-1830).
**[I]** `あべ/T_UNIDAT`, `T_UNICHR`, `T_UNIPOS`, `T_UNICG` (`.JAF.jam`) are therefore unit graphics and play no
part in the rules.

Fields of `sキャラ` that battle resolution reads or writes:

| Field | Meaning in battle | Evidence |
|---|---|---|
| `兵種` | troop type (§2). In a dungeon or solo battle, `Ｓ職種` (job) is used instead | 多田/関数／本編.jaf.jam:20288, 20314 |
| `兵数` | **current troops = HP** in a normal battle | あべ/C_キャラ.jaf.jam:6118 (キャラ_ＨＰ) |
| `兵数最大` | maximum troops (HP max) | あべ/C_キャラ.jaf.jam:6189 |
| `兵数合戦` | troops at battle start. Healing never goes above it; the HP bar draws current / max / start | 多田/関数／本編.jaf.jam:20174; あべ/T_BATHP.JAF.jam:287 |
| `Ｓ体力`, `Ｓ体力最大` | HP and max HP in "individual" mode (solo, dungeon, or a unit with `個人 = 1`) | あべ/C_キャラ.jaf.jam:6079-6093 (キャラ_ＨＰモード), 6112 |
| `個人` | 1 = individual unit (a person, not troops): its HP is `Ｓ体力` even in a normal battle | あべ/C_キャラ.jaf.jam:6093 |
| `攻撃`, `防御`, `知力`, `早さ` | attack, defence, intelligence, speed (the effective values) | 多田/関数／本編.jaf.jam:24674, 24687, 25666, 25679 |
| `攻撃付与` … `早さ付与` | per-stat buffs **in percent** (+50 = ×1.5); only the last value set counts | 多田/関数／本編.jaf.jam:25483-25515; 多田/関数／本編.jaf.jam:16229 |
| `技数`, `現在技数` | action points per battle (max / current). Each action costs `行動技数` | あべ/C_合戦.JAF.jam:351-359; 多田/関数／本編.jaf.jam:15769 |
| `技１`-`技４`, `技能` | the four actions and the passive skill. The `…Ｂ` set is the dungeon set | 多田/▲武将データ.jaf.jam:17744-19299 (デフォルト技能) |
| `Ｓレベル` | level. Only dungeon or solo damage and a few hero moves read it; it is the number drawn on the battle card | 多田/関数／本編.jaf.jam:24964, 23391; あべ/T_BATLVL.JAF.jam:100 |
| `ＦＦ` | the action-state code: 0 none, 1 charging, 2 confused (never set), 3 defending (never read), 21/22/23 guard stances, 99 destroyed | あべ/C_合戦.JAF.jam:3328-3420, 3639-3656 |
| `ＦＦ２`, `ＦＦ３` | the charged action and its remaining "charge resistance" | あべ/C_合戦.JAF.jam:3292-3303 |
| `ＦＦＥ` | guard chance in percent (ally guard) | あべ/C_合戦.JAF.jam:1066-1079 |
| `Ｆ式神` | Defense Shikigami flag: the next hit on this unit does 0 | 多田/関数／本編.jaf.jam:21147-21163 |
| `Ｆ集中` | Meditation stacks (+10 % each on assassination) | 多田/関数／本編.jaf.jam:23054-23067 |
| `重要` | important character: immune to assassination and to post-battle death or capture | 多田/関数／本編.jaf.jam:23263; 多田/関数／本編.jaf.jam:13710 |
| `戦場生死確率`, `Ｆ生死処理` | inputs to the post-battle death and capture roll (§8) | 多田/関数／本編.jaf.jam:13647, 21547-21557 |

`キャラ_ＨＰ` returns `Ｓ体力` when `キャラ_ＨＰモード` is 1, otherwise `兵数`. The mode is 1 when
`変数_ソロ戦闘()` (solo battle) is set, when `迷宮探索()` (dungeon) is set, or when the unit's
`個人` = 1 (あべ/C_キャラ.jaf.jam:6073-6121).

Setting HP (`キャラ_兵数設定`, あべ/C_キャラ.jaf.jam:4370-4433) clamps the value **from above only**, to
`兵数最大`; `Ｓ体力設定` likewise clamps to `Ｓ体力最大`. There is no lower clamp: damage is
capped instead (§3.6).

### 1.2 Places, rows and lines

Each side has 6 places (01-flow.md, "Conventions" and §1.3). **Places 1-3 are the front row**
(前衛) and **places 4-6 the back row** (後衛); place `p+3` stands directly behind `p`, and the
pair `{p, p+3}` is a **line**.

- `isMikataMae` loops over the index `i < 3` (places 1-3); `isMikataUshiro` over `i = 3..5`
  (places 4-6) (あべ/T_BATPLS.JAF.jam:2536-2563, 2605-2632).
- `戦闘_敵列` gathers the target plus `place+3` or `place−3`, i.e. its line (あべ/C_BATTLE.JAF.jam:604-702).
- When a side has **no living unit in its front row**, its rows swap: the back row moves to the
  front (`tagBattlePlace@change`, あべ/T_BATPLS.JAF.jam:1778-1861; called every turn by 合戦_状態補正, あべ/C_合戦.JAF.jam:229).

---

## 2. Troop types

Codes and names come from `文字列／兵種` (多田/関数／本編.jaf.jam:45791-45989). The Japanese names are **[I]**, from the
English names; the code only holds the English. The other columns are explained under the table.

| Code | Unit | Phys. attack base | Phys. vulnerability | Magic base | Magic vulnerability | Counters? | Default actions (技1 / 技2 / 技3 / 技4 / skill) | Post-battle troop gain |
|---|---|---|---|---|---|---|---|---|
| 1 | Samurai (武士) | 30 | 0 | 5 | 20 | yes | — / Wait / Samurai Attack 11 | 10 |
| 2 | Cavalry (騎馬) | 50 | 0 | 5 | 20 | yes | Cavalry Charge 13 in 技3 | 5 |
| 3 | Ashigaru (足軽) | 15 | 10 | 5 | 10 | yes | Ally Guard 21 / Wait / Ashigaru Attack 131; **starts in guard stance** | 15 |
| 4 | Musketeer (鉄砲) | **100** | 30 | 5 | 20 | **no** | Shoot 32 in 技3 | 5 |
| 5 | Archer (弓) | 10 | 15 | 5 | 20 | **no** | Bow Attack 31 in 技3 | 15 |
| 6 | Ninja (忍者) | 8 | 15 | 5 | 10 | yes | Shuriken 33 in 技3 | 10 |
| 7 | Sohei (僧兵) | 20 | 0 | 20 | 10 | yes | Tenshi's Blessing 58 / Wait / Sohei Charge 10 / Guard Cancel 73 | 10 |
| 8 | Onmyouji (陰陽) | 5 | 20 | 40 | 0 | **no** | Defense Shikigami 3 / Wait / Shikigami 40 | 8 |
| 9 | Miko (巫女) | 5 | 20 | 20 | 10 | **no** | Miko Dance 50 / Wait / Bow Attack 31 | 8 |
| 10 | Knight (騎士) | 30 | 0 | 5 | 20 | yes | Ally Guard 21 / Wait / Knight Attack 76 | 8 |
| 11 | Mage (魔法) | 5 | 20 | 45 | 0 | yes | Fire Blast 45 in 技3 | 8 |
| 12 | Cannoneer (大砲) | 80 | 30 (default) | 5 | 20 | **no** | Tulip 36 in 技3 | 5 |
| 13 | Tactician (軍師) | 8 | 20 | 5 | 20 | **no** | Battle Tactics I 63 / Wait / Bow Attack 31 / Status Cancel I 70 | 10 |
| 20 | Animal | 8 | 30 (default) | 5 | 20 | yes | Animal Attack 203 in 技3 | 10 |
| 21 | Youkai | 5 | 30 (default) | 5 | 20 | yes | — | 10 |
| 22 | Monster | 5 | 30 (default) | 5 | 20 | yes | — | 10 |
| 23 | Hanny | 5 | 30 (default) | 5 | 20; **all magic does 0** | yes | Hanny Attack 55 ×2, skill Absolute Magic Defense 106 | 10 |
| 24 | Peasant | 3 | 30 | 5 | 30 | yes | Uprising 14 in 技3 | 10 |
| 25 | Soul Bound | 5 | 30 (default) | 5 | 20 | yes | Infect 69 in 技3 | 10 |
| 27 | Monster (samurai-like) | 30 | 0 | 5 | 20 | yes | as Samurai | 10 |
| 28 | Monster (ashigaru-like) | 15 | 10 | 5 | 20 (magic table lists only 3, not 28) | yes | as Ashigaru | 10 |
| 29 | Monster (archer-like) | 10 | 15 | 5 | 20 | yes | as Archer | 10 |
| 31 | Monster | 5 | 30 (default) | 5 | 20 | yes | Thunderstrike 44 in 技3 | 10 |

Columns:

- **Phys. attack base**: `aa1` of `物理計算` for the attacker's type (多田/関数／本編.jaf.jam:25110-25280, default 5).
- **Phys. vulnerability**: `aa2`, the flat amount **added** when this type is the target
  (多田/関数／本編.jaf.jam:25288-25456, default 30).
- **Magic base / magic vulnerability**: the same two terms of `魔法計算` (多田/関数／本編.jaf.jam:25921-26057;
  defaults 5 and 20).
- **Counters?**: whether the type strikes back when attacked (多田/関数／本編.jaf.jam:24208-24266).
- **Default actions**: what `デフォルト技能` fills into empty slots (多田/▲武将データ.jaf.jam:17773-18985). Only the
  attack in 技3 is listed when slots 1, 2 and 4 are the defaults (技2 is always Wait 1).
- **Post-battle troop gain**: §8.2 (多田/関数／本編.jaf.jam:13814-13914).

### 2.1 Type rules found in the code

- **The physical "vulnerability" is added, not subtracted.** Heavily armed types take 0 extra
  (Samurai, Cavalry, Knight, Sohei). Lighter types take +10 to +30. Unlisted types take the default
  +30: animals, monsters, Hanny and **Cannoneers**.
- **Ranged and support types never counterattack**: Tactician 13, Archer 5, Musketeer 4,
  Onmyouji 8, Miko 9, Cannoneer 12. The type is checked on the defender (`▲兵種Ｂ`) when the
  counter is computed (多田/関数／本編.jaf.jam:24204-24266); for these types the counter returns 0.
- **Ashigaru start every battle in guard stance**: `ＦＦ = 21`, `ＦＦＥ = 50`. The skill 127
  "Enhanced Initial Guard" makes it `ＦＦＥ = 120` (多田/関数／本編.jaf.jam:16121-16169). Until the
  Ashigaru's first turn the stance halves the physical damage they take and lets them intercept
  attacks (§5).
- **Onmyouji's magic does ×4 against the oni**, characters 1236, 1237, 1086 and 1087 (多田/関数／本編.jaf.jam:20592-20663).
- **Hanny (23) take no damage from magic.** Character 1090 (Genbu) takes half (多田/関数／本編.jaf.jam:20881, 21129).
- **The Tactician casts buffs at battle start.** Up to `aa3` random buffs go on its own side, each
  `攻撃/防御/知力/早さ付与 = 知力×5` (in a dungeon `Ｓレベル`, at most 4). Each buff is applied only
  if it is higher than the one in place (多田/関数／本編.jaf.jam:17359-17647).
- **Guns, the "once per battle" rule.** There is no per-type use limit. Every action costs `行動技数`
  points from the commander's `現在技数` (あべ/C_合戦.JAF.jam:359). 99 means "uses up all" (`行動_必殺技`;
  あべ/C_合戦.JAF.jam:346-351). A unit with 0 points cannot act (あべ/C_合戦.JAF.jam:252). Shoot 32 costs 1 point and waits 30
  ticks, against 15 for melee (§4). Guns are therefore not limited by type: it is the commander's
  action count. Musketeers hit hard because of their attack base of 100.

---

## 3. Damage resolution

### 3.1 Call chain

The chain runs from 合戦_行動 (あべ/C_合戦.JAF.jam:532-759). It dispatches on `行動タイプ`:

| 行動タイプ | Handler |
|---|---|
| 1 physical, 2 magic | 合戦_攻撃_単体 or 合戦_攻撃_複数 |
| 3 hero | 合戦_攻撃_勇者 |
| 4 heal | 合戦_回復_* |
| 9 other | 合戦_その他_* |
| 0 wait | logged only |

A single attack is `合戦_戦闘処理(attacker, target, action)` (あべ/C_合戦.JAF.jam:2131-2505):

```
defDmg = 合戦／戦闘計算(attacker, target, action, フラグ=0)      // あべ/C_合戦.JAF.jam:2386
target.HP -= defDmg                                               // あべ/C_合戦.JAF.jam:2395 (キャラ_ＨＰ−)
if 行動ダメージ率敵[action] != 0:                                  // あべ/C_合戦.JAF.jam:2424
    atkDmg = 合戦／戦闘計算(attacker, target, action, フラグ=1)   // the counter, あべ/C_合戦.JAF.jam:2457
    attacker.HP -= atkDmg
battle totals: damage dealt by Oda → ◆被害総数１, damage taken → ◆被害総数２   // あべ/C_合戦.JAF.jam:2466-2480
if target had no Defense Shikigami and target is in guard stance (FF 21/22/23):
    target.ＦＦＥ -= 40                                           // あべ/C_合戦.JAF.jam:2485-2499
then 合戦_壊滅チェック on the target and the attacker
```

A multi-target action calls this once per target, in array order (あべ/C_合戦.JAF.jam:1171-1340). There is one
counter per target.

### 3.2 Physical base, `物理計算(A, B)` (多田/関数／本編.jaf.jam:24613-25601), normal battle

```
base   = TypeAtk[A.兵種] + TypeVuln[B.兵種]                        // §2 table
atkA   = 倍率(A.攻撃 × 10, 100 + A.攻撃付与)
defB   = 倍率(B.防御 × 8,  100 + B.防御付与)
result = max(10, base + atkA − defB)                               // 多田/関数／本編.jaf.jam:25524-25534
```

`倍率(x, p)` = `x × (p / 100.0)` computed in floating point; it returns **0 if x < 0 or p < 0**
(多田/関数／メイン.jaf.jam:18597-18680). **[I]** The conversion back to an integer truncates.

### 3.3 Magic base, `魔法計算(A, B)` (多田/関数／本編.jaf.jam:25603-26188), normal battle

```
base   = MagAtk[A.兵種] + MagVuln[B.兵種]
if the defending side has skill 216 "Onmyou Defense": base −= 10   // 多田/関数／本編.jaf.jam:26066
atkA   = 倍率(A.知力 × 10, 100 + A.知力付与)
defB   = 倍率(B.知力 × 7,  100 + B.知力付与)
result = max(10, base + atkA − defB)
```

### 3.4 Troop scaling, `人数計算(base, troops)` (多田/関数／本編.jaf.jam:26739-26923)

The scaling uses the **acting unit's current troops**.

```
M = min(T, 500) + max(0, min(T, 2000) − 500) / 2 + max(0, T − 2000) / 4   // integer division
M = max(M, 100)
dmg = min(base × M / 100, T)                                            // never more than own troops
```

A unit's punch grows with its troops, at full rate up to 500, half rate from 500 to 2000 and a
quarter beyond. A unit **can never kill more troops than it has.**

### 3.5 The whole hit, `合戦／戦闘計算(…, フラグ=0)` (多田/関数／本編.jaf.jam:20209-24611)

The order of the steps matters:

1. **Magic (`行動タイプ` 2)**, 多田/関数／本編.jaf.jam:20499-21313. A magic hit has no guard halving, no defender
   items and no assassination step.
   - `dmg = 魔法計算`.
   - Normal battle: `dmg = 人数計算(dmg, A.兵数)`.
   - `dmg = 倍率(dmg, 行動ダメージ率)`.
   - `dmg = 合戦補正(dmg, side)` (§6).
   - Action specials: Angel Cutter 46 ×2 vs Sohei and ×2 vs monsters; Miko Storm 67 / 97 = 30 % /
     10 % of the target's max troops.
   - Genbu ½.
   - Talent ×1.4: skill 114 when `◆戦場タイプ` = 1, skill 115 when it is 3, skill 116 when it is 7.
   - The target's skill 106 Absolute Magic Defense → 0.
   - Unity (§3.6).
   - Hanny → 0.
   - Defense Shikigami → 0, and the flag is consumed.
   - Boss correction, then the cap at the target's troops.
2. **Physical or hero, attacker side**, 多田/関数／本編.jaf.jam:21319-24198:
   1. Store the target's current troops in `◆防御側兵数`; the counter uses it (§3.7).
   2. `dmg = 物理計算(A, B)`. Normal battle: `dmg = 人数計算(dmg, A.兵数)`.
   3. `dmg = 倍率(dmg, 行動ダメージ率[action])`, then `dmg = 合戦補正(dmg, side)`.
   4. Action specials (table in §4.2).
   5. Attacker's skill 107 Hibachi: **+50 flat**. Skill 112 Lover: **+30 flat** (normal battle
      only). Genbu ½.
   6. **Guard stance**: if the target's `ＦＦ` is 21, 22 or 23, `dmg /= 2`. Action 215 Guard Break
      then doubles it back (多田/関数／本編.jaf.jam:22105-22161).
   7. Talents ×1.4, as for magic.
   8. Attacker's items: 31, 32, 58 and 60 ×1.2; 61 +50; 62 ×1.5 vs monsters, else ×0.7; 63 +100.
   9. **Defender's items, only if not guarding**: 64 ×0.7; 65 −50; 66 −100; 67 ×0.5; 69 ×0.5 vs
      monsters.
   10. Buff strip (actions 10, 35, 99, 145), then assassination (29, 85, 86, 91, 170). Both are in §4.2.
   11. Hero overrides: when `フラグ = 3`, actions 90 / 95 do `20 + A.Ｓレベル×3` and 92 does the same.
       Fixed-damage moves set their value here (§4.2).
   12. Defense Shikigami → 0. Unity.
   13. Charge break: if `dmg ≠ 0` and the target is charging, `target.ＦＦ３ −= 行動タメ破壊`. At 0
       the charge is cancelled (`ＦＦ = 0`).
   14. Action-point break: `target.現在技数 −= 行動技数破壊` if `dmg ≠ 0`.
   15. Boss correction (`ボスダメージ補正`, 多田/関数／本編.jaf.jam:26190-26737: per-character caps and immunities).
   16. Cap at the target's troops (normal battle, `個人 = 0` only).
   17. Infect 69 and Persuade 17 also lower the target's `兵数最大` and `兵数合戦` by `dmg`;
       Persuade adds the same to the attacker's. Then the per-side damage tally `ＧＦ[172/173]`.
3. **Counter (`フラグ = 1`)**, 多田/関数／本編.jaf.jam:24204-24603: see §3.7.

### 3.6 Losing troops, rout and death in battle

**Damage removes troops one to one**: `HP −= dmg` (あべ/C_キャラ.jaf.jam:6241-6258). The damage was capped at the
target's troops beforehand. There is no separate morale or rout value.

- **Unity (skill 125).** If the target has more than 1 troop and `dmg` would exceed its troops, then
  with a 50 % chance (`RAND(2) = 1`) `dmg = troops − 1` (多田/関数／本編.jaf.jam:21061-21117, 23739-23795).
- **Destroyed.** After each attack, `合戦_壊滅チェック` sets `ＦＦ = 99` when `HP == 0` (あべ/C_合戦.JAF.jam:3166-3200).
  A unit with `ＦＦ = 99` (or no character) is out of the battle (合戦_壊滅状態, あべ/C_合戦.JAF.jam:3370-3394). An
  assassination also sets `ＦＦ = 99` directly (§4.2).
- **Withdraw.** The action number 999 is "withdraw" (`戦闘_さがる`, あべ/C_合戦.JAF.jam:309-316 → あべ/C_BATTLE.JAF.jam:418-448).
  Solo battles only: the clicked unit leaves and a reserve takes its place if there is one
  (01-flow.md §4.2).
- The battle ends when one side is wiped out, when both sides have no action points left, or when
  time runs out (合戦_状態補正, あべ/C_合戦.JAF.jam:136-231; 01-flow.md §4.1).

### 3.7 The counterattack

The counter is computed by 多田/関数／本編.jaf.jam:24204-24603:

```
if the target's type is Tactician / Archer / Musketeer / Onmyouji / Miko / Cannoneer: 0
if dungeon or solo battle: 0
T = ◆防御側兵数 (the target's troops BEFORE this hit; 10 if that was 0)
c = 物理計算(B → A)                        // the target's attack against the attacker's defence
normal battle: c = 人数計算(c, T)
c = 倍率(c, 行動ダメージ率敵[action])       // the action's counter rate, e.g. 25 % for Samurai Attack
NO 合戦補正
defender's item 59 ×1.5; defender's skill 108 ×2 ("Counterattack Rate Up 1"), 109 ×3
attacker's Defense Shikigami → 0; boss correction
cap at the attacker's troops (the code does not check 個人 here)
```

- **A unit wiped out by the hit still strikes back** with its pre-hit strength: the counter is
  computed after the HP is applied, but from `◆防御側兵数`. **[I]** This looks deliberate; the
  fallback "0 → 10" exists for it.
- The attacker's guard stance does **not** halve counter damage.
- The counter is always physical. A magic action with a non-zero counter rate would compute magic
  damage for the counter, because the magic branch ignores `フラグ`. No magic action in the table
  has a counter rate, so this never happens.

### 3.8 Randomness

The base formula has **no random spread, no critical hits and no row modifier**. Neither
`物理計算`, `魔法計算`, `人数計算` nor `戦闘計算` reads a place or row. Randomness enters only
through:

- the gamble charges (168: ×0.5/1/1.5/2/2.5; 169: ×1/1.5/2/2.5/3; one of five, `RAND(5)`);
- Ally Avenger 26 (`+RAND(damage your side has taken so far)`);
- Unity's 50 %;
- the assassination roll;
- the ally-guard roll;
- the boss corrections (`RAND(20)`, `RAND(Ｓ体力/4)`);
- the Tactician's buffs at battle start, and buff skills 119-122 (2/3 chance of a +50 buff).

`RAND(n)` returns `Math.Rand() % n + 1`, so 1..n (sact_std RAND; `乱数` in S_TOOL is the same).
`固定乱数取得` reads a **fixed table of 400 values of `RAND(60)`**. The table is built when a new
game starts (`■初期化`:122) and read cyclically (多田/関数／メイン.jaf.jam:18135-18164). Reloading a save does not
re-roll an assassination.

### 3.9 Dungeon and solo battles (`◆戦闘タイプ = 1`)

These battles use `Ｓ職種` as the type, `Ｓ体力` as HP and the level.

```
lvl = A.Ｓレベル − B.Ｓレベル / 2;  lvl = max(lvl, 5);  if lvl > 30: lvl = 30 + (lvl − 30) / 2
phys = JobBase[A.Ｓ職種] × lvl       // 多田/関数／本編.jaf.jam:24712-25102; JobBase 2..11, e.g. 44→11, 36/50/51→10, default 2
       ×1.3 if A.攻撃付与 ≠ 0; ×0.7 if B.防御付与 ≠ 0
magic = 7 × lvl                     // 多田/関数／本編.jaf.jam:25737-25913; ×1.3 / ×0.7 by 知力付与
```

No defence term, no troop scaling and no counter.

---

## 4. Actions (行動)

### 4.1 The table

多田/▲技能データ.jaf.jam:2-10514 (技能データ初期化) fills global arrays indexed by action number:
**218 entries** — 0 "No Skill" and **217 actions numbered 1-220** (24, 176 and 183 have no entry);
Retreat (999) is named outside the table (`行動_名前`, あべ/C_行動.jaf.jam:52-58). This part keeps
only what the damage rules need; [03-skills.md](03-skills.md) §7 tabulates the battle actions and
passives, and anything else is read from `多田/▲技能データ.jaf.jam` itself. The columns (fields:
03-skills.md §1):

- `行動タイプ`: 0 wait, 1 physical, 2 magic, 3 hero (Rance Attack 90, Commander Charge 92,
  Kentarou-kun Slash 95), 4 heal, 9 other (support or status), 11 passive skill, 19 passive that
  acts at battle start (the two Majesties).
- `行動ターゲット`: who can be targeted (§4.3).
- `行動待ち時間`: the delay to the unit's next turn (01-flow.md §2.2).
- `行動技数`: the action-point cost; 99 = all.
- `行動ダメージ率`: damage %.
- `行動ダメージ率敵`: counter %.
- `行動タメ`: a charge move; it fires on the next turn (あべ/C_合戦.JAF.jam:3257-3309, 505-519).
- `行動タメ耐性`: the charge resistance, i.e. how many hits break the charge.
- `行動タメ破壊`, `行動技数破壊`: charge break and action-point break dealt by this move.
- `行動戦果`: 戦果 (advantage) points — applied only to support actions, never to attacks
  (01-flow.md §3.2).

The basic attacks:

| Action | 行動タイプ | 行動ターゲット | Damage % / counter % | Wait | Points |
|---|---|---|---|---|---|
| Samurai Attack 11 | 1 | 1 | 100 / 25 | 15 | 1 |
| Ashigaru Attack 131 | 1 | 1 | 100 / 25 | 15 | 1 |
| Knight Attack 76 | 1 | 1 | 100 / 25 | 15 | 1 |
| Sohei Charge 10 | 1 | 1 | 100 / 30, strips one buff | 20 | 1 |
| Cavalry Charge 13 | 1 | 2 | 100 / 25 | 20 | 1 |
| Bow Attack 31 | 1 | 2 | 100 / 0 | 20 | 1 |
| Shoot 32 | 1 | 1 | 100 / 0 | 30 | 1 |
| Tulip 36 | 1 | 2 | 100 / 0 | 30 | 1 |
| Shuriken 33 | 1 | 2 | 100 / 0, charge break 1 | 20 | 1 |
| Shikigami 40 | 2 | 7 | 100 | 25 | 1 |
| Fire Blast 45 | 2 | 2 | 100 | 35 | 1 |
| Thunderstrike 44 | 2 | 7 | 50 | 25 | 1 |
| Miko Dance 50 | 4 | 3 | 50 (heal) | 20 | 1 |

### 4.2 Per-action specials in the damage step

From 多田/関数／本編.jaf.jam:20682-21313 and 21427-23580:

| Action | Rule |
|---|---|
| 8 Royal Guard Charge | ×2 if the attacker has fewer troops than the target |
| 15 Pursuit | ×3 if your side leads on battle result (`戦果取得`) |
| 19 Mercy Charge | target `戦場生死確率 −10`, `Ｆ生死処理 = 2`: **certain capture** after the battle (§8) |
| 26 Ally Avenger | `+RAND(total damage your side has taken)` |
| 27 Pillage | Oda only: loot = `総国力(1) × 10` |
| 28 Death by Poison | damage = ½ of the target's troops |
| 89 Backline Crusher | ×2 vs Archer, Onmyouji, Tactician, Miko |
| 139 Ashigaru Target | ×4 vs Ashigaru |
| 140 Ninja Target | ×4 vs Ninja |
| 142 | ×3 when `◆戦場対戦国` = 16 |
| 10 / 35 / 99 / 145 | remove the target's first non-zero buff, in the order attack → defence → int → speed |
| 29 / 85 / 86 / 91 / 170 | **assassination** (below) |
| 93 Reaper Scythe | max troops / 4 |
| 94 / 158 / 179 / 148 / 68 | fixed damage 101 / 201 / 200 / 187 / 9000 |
| 181 | 6000 vs specific targets |
| 46, 67, 97 (magic) | see §3.5 |

**Assassination.** The damage is 0, except for 170 Shoot + Snipe, which keeps its damage.

```
a = 45 (29, 85, 91) / 50 (86) / 40 (170)
a += 10 × attacker.Ｆ集中                    // stacks from Meditation 4 or 166
a −= 10 if the target side has skill 213
normal battle: a += 5 if the attacker's troops > 2 × the target's
               a −= 10 if the target's troops > the attacker's
               a −= 10 more if the target's troops > the attacker's + 1000
dungeon: a = 0 if the attacker's level < the target's
skill 220: a = 100 if the target has 0 action points
success if target.重要 == 0 and a > 固定乱数 (1..60): target ＦＦ = 99, Ｆ生死処理 = 1, 戦場生死確率 = 0
```

多田/関数／本編.jaf.jam:22926-23351. The chance is therefore about (a − 1) / 60.

### 4.3 Who can hit whom (range)

The player's legality check is `tagBattleSelect@checkAction` (あべ/T_BATSEL.JAF.jam:2066-2189); the enemy AI uses
`敵ターゲット_選択` (あべ/C_BATTLE.JAF.jam:1088-1197) with `tagBattleTeki@select` (あべ/T_BATTEK.JAF.jam:136-227).

| 行動ターゲット | Allowed |
|---|---|
| 1 (melee) | **the actor must stand in its own front row, and the target must be in the enemy front row.** The AI: random front-row enemy |
| 2 | any enemy, from any row |
| 5 | any enemy; hits that enemy and the other unit of its line (`戦闘_敵列`) |
| 6 | the actor must be in the front row; any enemy, plus its line |
| 7 | all enemies |
| 3 | one ally |
| 8 | all allies |
| 4 | self |
| Ally Guard 21 / 22 | an ally **in the actor's own row** |
| All Guard 23 | any ally |

The melee set (type 1) is Samurai, Ashigaru, Knight, Sohei and **Shoot (Musketeer)**. Bows, Tulip,
Shuriken, Cavalry Charge and Fire Blast are type 2: they reach the back row from anywhere. A side's
front row only empties completely when every unit in it is destroyed, and then the rows swap (§1.2),
so melee always has a target.

---

## 5. Guard and defence

**Guard stance.** `ＦＦ = 21` (Ally Guard), `22` (Neo Ally Guard) or `23` (All Guard). The actions
add to the guard chance `ＦＦＥ` (多田/関数／本編.jaf.jam:28577-28772):

| Action | Normal battle | Dungeon |
|---|---|---|
| Ally Guard 21 | `+知力×20` | +80 |
| Neo Ally Guard 22 | `+知力×30` | +150 |
| All Guard 23 | `+知力×10` | +100 |

Ashigaru start at 21 with `ＦＦＥ` 50 (§2.1).

The stance has two effects:

1. **Half physical damage** to the unit in stance, whatever its `ＦＦＥ` (§3.5, step 6; Guard Break
   215 cancels this). Magic and counters are not halved.
2. **Interception.** A single-target enemy attack (行動ターゲット 1 or 2) on an ally makes the game
   scan the ally side (slots 11→16 or 21→26) and skip the target itself (あべ/C_合戦.JAF.jam:939-990, 992-1087;
   あべ/C_合戦敵.JAF.jam:1663-1806).
   - A stance-21/22 unit may intercept only if it stands **in the same row as the target**.
   - A stance-23 unit may intercept from any row.
   - It intercepts if `乱数(100) ≤ ＦＦＥ`.
   - **The first unit that succeeds takes the hit instead**, at half damage because it is in stance.

**Wear.** Every hit a unit in stance takes reduces its `ＦＦＥ` by 40 (あべ/C_合戦.JAF.jam:2485-2499).
- The stance itself (`ＦＦ`) stays through the wear, so a worn-out guard keeps the damage halving
  but no longer intercepts. It ends when the guarding unit's own next turn starts: `合戦_行動開始`
  calls `合戦_ＦＦクリア`, which zeroes `ＦＦ`/`ＦＦ２`/`ＦＦ３` of a unit that is not charging and
  keeps `ＦＦＥ` (あべ/C_合戦.JAF.jam:501-529, 3202-3229). An Ashigaru's starting stance therefore
  lasts until its first turn; re-guarding adds to the kept `ＦＦＥ` (03-skills.md §4.2).
- Guard Cancel 73 clears `ＦＦ` and `ＦＦＥ` of every enemy unit that has `ＦＦＥ ≠ 0` (多田/関数／本編.jaf.jam:30292-30456).
- Starting a charge overwrites `ＦＦ` with 1 and so ends the stance (あべ/C_合戦.JAF.jam:3292).

**Other defences:**

- **Defense Shikigami.** Action 3 on one ally, 143 on all allies, or item 70 at battle start: the
  next hit on the unit does 0, and the flag is then consumed (多田/関数／本編.jaf.jam:30468-30598, 17347).
- **"Defend" state `ＦＦ = 3`.** Set by action 6 (多田/関数／本編.jaf.jam:28561). `合戦_防御状態` is never called, so it
  has no effect beyond replacing a guard stance. Open question below.
- **Confusion `ＦＦ = 2`.** Never set anywhere in the battle code.

---

## 6. Battle-wide modifiers

Two globals, set up by 合戦／初期化 (多田/関数／本編.jaf.jam:15879-16057):

- `◆合戦補正１`: the Oda side's bonus %;
- `◆合戦補正２`: the enemy's bonus %.

| Battlefield | Defender gets | Battle-result bonus |
|---|---|---|
| Castle (`◆戦場タイプ = 3`) | +20 | +40 |
| Mansion (`= 2`) | +10 | +20 |
| Fort (`= 1`) | — | +10 only |
| Special: `攻防タイプ = 1`, `対戦国 = 7`, province type 11 | enemy 50 | — |

In addition, the Oda side adds the province's **battle-preparation** value `国.合戦準備` (it is then
cleared). The encampment actions 154-156 add 4 / 6 / 8 during the battle (多田/関数／本編.jaf.jam:30642-30689).

`合戦補正の補正` (多田/関数／本編.jaf.jam:33332-33492) **nets the two against each other**: the side with the larger
bonus keeps the difference, the other drops to 0, and the bonus is capped at **50**.

`合戦補正(dmg, side)` (多田/関数／本編.jaf.jam:27407-27539) multiplies the attacker's damage by `100 + own bonus` % or
by `100 − enemy bonus` %. It applies to physical and magic **hits**, not to counters or heals.

---

## 7. Healing and buffs

**Heal**, `合戦／回復計算` (多田/関数／本編.jaf.jam:19679-20207):

```
h = 倍率(healer.知力 × 6, 100 + 知力付与)
h = 人数計算(h, healer.兵数)
h = 倍率(h, 行動ダメージ率)
cap: troops lost in this battle (兵数合戦 − 兵数), for 個人 = 0
```

- Masamune's Eyedrops 81 and Full Recover 84 heal 999 (81 only in the dungeon, where the code
  checks both; the normal-battle path checks 84 only).
- Snake Grease 163 heals 9999 and restores the target's action points.
- **No heal rises above the troop count at battle start.** A target with no losses gains nothing.
- Dungeon: `Ｓレベル × 5`, ×1.5 with a 知力 buff.

**Auto-heal skills** 100 / 101 / 102. At the end of each of the unit's own actions it heals `ＧＦ[33]`
/ `ＧＦ[34]` / `ＧＦ[35]` troops, capped at its losses (あべ/C_合戦.JAF.jam:2967-3163).

**Buffs.** All buffs are percentages. Each slot holds one value; a new value replaces the old one,
except the Tactician's random buffs, which only raise it. Sources:

- Store Power 96: own attack +70 (多田/関数／本編.jaf.jam:30626).
- Ninja Leaf Camouflage 37: own defence +70.
- Battle Tactics:
  - 60: all four stats of one ally = 知力 × 5;
  - 135: two random stats;
  - 61 / 62 / 63: `troops/100+2`, `troops/200+2`, `troops/300+2` random buffs over the side
    (多田/関数／本編.jaf.jam:28899-29907).
- Status Cancel 70 / 71 / 72: remove all buffs from 2 / 3 / 4 random enemies.
- Battle start:
  - items 46 / 47 / 48 give +50 to all four stats in a dungeon / castle / fort battle;
  - skills 119-122 give one +50 stat buff (2/3 chance);
  - 128 / 129 give all four stats +50;
  - Miko at Work 123 gives Attack +40 to `max(1, troops/250)` **male** allies (`性別 == 0`;
    Rance is created with 0, Suzume with 1, `多田/▲武将データ.jaf.jam:110-135`, `:352-372`), in
    slot order (多田/関数／本編.jaf.jam:16595-16763);
  - 176 gives the enemy +30 to all four stats (多田/関数／本編.jaf.jam:16181-17203).

The buffs act **only through the formulas**: attack and defence buffs in `物理計算`, the int buff in
`魔法計算` and in healing. The speed buff feeds the turn order (`速度補正値`, 多田/関数／本編.jaf.jam:26925-27405;
01-flow.md §2.2). **[I]** The buffs last the whole battle: nothing resets them except the strip moves
(found by searching every write in the battle code).

---

## 8. Levels, experience and the battle's aftermath

### 8.1 No experience in a normal battle

`Ｓレベル` only rises through `ＣＳレベル加算`:

- dungeon clear: `迷宮／勝利後`, 多田/関数／本編.jaf.jam:1248-3369; e.g. +2 for Rance at 多田/関数／本編.jaf.jam:1627;
- dungeon exploration: 多田/関数／本編.jaf.jam:1214;
- items: 多田/関数／本編.jaf.jam:10526;
- events: `多田/Ｆコマンドイベント.jaf.jam`.

In a normal battle the level only matters for the hero moves (20 + 3 × level) and as the number on
the card.

### 8.2 Troop growth after a battle

`Ｐ合戦／戦闘結果(フラグ)`, 多田/関数／本編.jaf.jam:13516-14549. `フラグ` is the battle result
`nKekka` handed over by `tagBattlePlace@kekka` (あべ/T_BATPLS.JAF.jam:559-602, called for 1 and 2
only): 1 = Oda won, 2 = Oda lost (01-flow.md §4.1).

After a **win** (`フラグ == 1`, tested with `ＦＦ ≠ 99` at 多田/関数／本編.jaf.jam:13770-13800)
every surviving Oda unit (`ＦＦ ≠ 99`, `ＡＡ ≠ 0`) gains `g` troops. `g` is the post-battle troop
gain column of the §2 table:

- `g` is reduced by 3 if this commander has the largest `兵数最大` of its type;
- `g` is increased by 1 if a party member has skill 214 Effective Advertising.

`Ｃ兵数追加` adds `g` to `兵数最大` (cap 9999) and the same amount to `兵数` (多田/関数／メイン.jaf.jam:12135-12205).

`▲兵数制限 = 400 + 50 × provinces + bonus` is computed (多田/関数／本編.jaf.jam:13565) but never used.

### 8.3 Destroyed Oda commanders

For each destroyed Oda unit with `ＡＡ ≠ 0`:

```
戦場生死確率 −= 5 (permanent)
item 49 → safe
Rance (1000) / Kou (1078) → death, and the game is over (多田/関数／本編.jaf.jam:14622-14672)
重要 → safe
if 戦場生死確率 > RAND(50) → safe
else dies with probability 1/3
```

### 8.4 Destroyed enemy commanders (`重要 = 0`)

At most **2 die or are captured per battle** (`▲死亡制限`). Nothing happens if the faction has
fewer than 4 characters.

**Capture.** Not attempted if:

- the first-capture event has not happened yet;
- the prison holds more than 25, or more than 20 and a 50 % roll comes up;
- the type is Peasant or Soul Bound;
- `捕虜抵抗値 = 999`;
- it is a generic commander of faction 2, 16 or 17.

Otherwise the commander is captured with probability `1/a`:

| Condition | `a` |
|---|---|
| base | 5 |
| skill 111 or 219 in the party | 2 |
| both skills | 1 |
| Mercy Charge flag | 1 |

A captured commander gets `Ｆ戦闘結果 = 2000` (多田/関数／本編.jaf.jam:14393); a dead one 1000. The
card's result badge shows these (04-screen.md §6.2).

**Death.** Only a **generic** (`番号 = 999`) commander who was not captured can die: probability
1/2. It is 1 (certain) after an assassination, for Soul Bound and for two named characters
(多田/関数／本編.jaf.jam:14082-14536).

---

## Sengoku Rance oddities and open questions

The Sengoku Rance oddities of the damage step (not entries of docs/quirks.md) are listed with the
others in [03-skills.md](03-skills.md) §8; items 1 and 2 below are oddities still unsettled.

1. **Individual units can go below 0 HP.** For a `個人` unit in a normal battle, the physical step
   does not cap damage (the cap is skipped when `個人 ≠ 0`). The magic step and the counter cap
   against `兵数` instead of `Ｓ体力`. `Ｓ体力` has no lower clamp, and `合戦_壊滅チェック` tests
   `HP == 0`. So an individual unit could reach negative HP and not be marked destroyed, unless
   `ボスダメージ補正` caps it. That function caps only specific characters. Not settled.
2. **What action 6 and `ＦＦ = 3` are for.** Action 6 is named "S Basic Monster Attack" in the
   table, yet `行動後処理` treats 6 as "defend". The state is never read.
3. **The battle slot's link to the roster.** That `ＡＡ` means "a real commander occupies this
   battle slot" and that `ＢＢ` is its home index are **[I]**. (Settled: `フラグ = 1` is a win,
   §8.2; the copies are written back by `ＦＰ戻し`, 01-flow.md §4.3.)
4. **`ボスダメージ補正` in detail.** Per-boss caps such as ≤ ⅕ or ½ of max HP, `Ｓレベル` gates on
   actions 98 and 78, and Rance kept at 1 troop. They were read but not tabulated per character.
5. **Rounding of `倍率`.** It works in floats, and the conversion back to an integer is not shown in
   the decompile; truncation is assumed.
