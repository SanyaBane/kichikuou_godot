# Sengoku Rance battle survey, part 4: the battle screen and its input

This part covers Sengoku Rance's battle screen: its regions and layers, the troops on stage, the
top bar, the turn-order strip, the damage counters, the unit cards, the time gauge, the command
menu on the hovered card, the input, and the step-by-step presentation of each kind of action. It
is one of four reference surveys for **our own battle screen "in the manner of Sengoku Rance"**,
not rules for the port — Kichikuou's rules stay unchanged. The conventions (source references
relative to `ain\files\` under `SENGOKU_RANCE_EXTRACT_DIR`, places 1-6 with 1-3 the front row,
"row" and "line", **[I]**) are in [01-flow.md](01-flow.md#conventions-shared-by-the-four-parts);
the rules behind what is drawn are in 01-flow.md (turns, time, 戦果, the end),
[02-resolution.md](02-resolution.md) (damage) and [03-skills.md](03-skills.md) (commands, states).
Most of the screen code is in `あべ/T_BAT*.JAF.jam`; references give the function name too. The
game is 800×600; the CGs themselves were not looked at (only code), so the look of a CG is never
claimed, only its number, place and use. "Score" below is the drawing's word for 戦果, the
advantage (01-flow.md §3.2).

Record numbers used by the battle code: player-side places/commanders are slots **11–16**, enemy-side
**21–26** (slot = 10 / 20 + place; `あべ/T_BATPLS.JAF.jam:114,121` `tagBattlePlace@create`). "Side"
below is decided by `< 21` everywhere.

---

## 1. Screen regions and z-order

| Region | Rect (x, y, w, h) | What | Ref |
|---|---|---|---|
| Black under-layer | 0, 0, 800, 314 | filled black, z300 | `あべ/T_BATBAS.JAF.jam:33-55` `tagBattleBase@create` |
| Battle background | 0, 0 | CG `◆背景ＣＧ` (if it exists), z301; shaken by quakes | `あべ/T_BATBAS.JAF.jam:59-72,194-203,232` |
| Foreground overlay | 0, 0 | CG `背景ＣＧ + 300` (if it exists), z411 — **above** the troops (401/402) and the effects (406) | `あべ/T_BATBAS.JAF.jam:78-91,256` |
| Turn-order strip backing | 0, 290, 800, 24 | rows 0–23 of CG 10200, z501 | `あべ/T_BATBAS.JAF.jam:133-155` |
| Bottom panel | 0, 314, 800, 254 | rows 24–277 of CG 10200, z601 (ends at y 568) | `あべ/T_BATBAS.JAF.jam:97-124` |
| Below 568 | 0, 568, 800, 32 | not drawn by the battle; the tooltip is clamped to y ≤ 568 (`あべ/T_COMMNT.JAF.jam:542-549`); the system bar (IDs 90000+) lives somewhere here **[I]** | |

Layer stack (low → high): background 301 · dungeon plate 331 · troops 401 (normal) / 402 (attack) ·
effects 406 · foreground 411 · flags 511 · number panels 511/521 (flying digits +200) · bottom panel 601 ·
score bar 601 · arrow 601 · time gauge 602/603 · cards 611–671 (+0…+9 per element) · command buttons
card z + 70 · spotlight mask 701 · dialogue boxes at the max z.

---

## 2. The top scene (y 0–313)

### 2.1 Which troops are shown
At most **two units** are on stage, held in two slots: slot 1 = the acting unit, slot 2 = its target (or the
hovered enemy while the player chooses) (`あべ/T_BATUNI.JAF.jam:2603-2670` `tagBattleUnitManager@moveIn`,
`:3387-3423` `setCurrent`). The **side**, not the slot, decides where a unit stands: every unit is composed
on a full 800×600 work sprite in "player" coordinates (left half, facing right) and an enemy's composition
is **mirrored left↔right** and right-aligned (`あべ/T_BATUNI.JAF.jam:1893-1938` `tagBattleUnit@copySp`).
So the player's unit is always on the left, the enemy's on the right.

### 2.2 How many soldiers are drawn
Not proportional to troops. Each unit type has a fixed **formation template** of up to 10 figure slots
(`あべ/T_UNIDAT.JAF.jam:369-2113` `tagUnitDataManager@0`, e.g. type 1 at `:408-480`): each slot has a draw order
"No", a role ("place": 1 = the commander's own CG, 2 = a second character `ＵＮ２`, 11 = forward soldier,
12 = rear soldier) and a bottom-right anchor (right, bottom). Counts read from the templates:

| Unit type (兵種) | figures | commander / forward / rear | melee flag (`set(type,1)`) |
|---|---|---|---|
| 1 Samurai, 3 Ashigaru, 6 Ninja, 7 Sohei, 8 Onmyouji, 9 Miko, 10 Knight, 11 Mage, 12 Cannoneer, 23 Hanny, 24 Peasant, 25 Soul Bound | 9 | 1 / 4 / 4 | 1, 3, 7, 10, 24, 25 |
| 4 Musketeer | 8 | 1 / 4 / 3 | no |
| 5 Archer | 9 | 1 / 2 / 6 | no |
| 13 Tactician | 7 | 1 / 2 / 4 | no |
| 2 Cavalry, 20 Animal, 32, 33 | 5 | 1 / 2 / 2 | yes |

Soldier CGs are picked at random from the type's forward/rear sets (`あべ/T_UNIDAT.JAF.jam:2382-2403`
`tagUnitDataManager@get`). Figures are drawn from the highest "No" down to No 1, each blended at
(right − w + 1, bottom − h + 1) (`あべ/T_BATUNI.JAF.jam:1193-1244` `createSp1`, `:1806-1833` `blendUnit`).
Only slots with index < troop count are drawn, so a unit with fewer than ~9 men shows fewer figures
(`あべ/T_BATUNI.JAF.jam:1958-1970` `getBlendNormal`). Special cases:
- **HP-mode units** (solo/dungeon battles, or a 個人 "individual" character, `あべ/C_キャラ.jaf.jam:6079-6093`
  `キャラ_ＨＰモード`) count as 1: a single figure anchored at (410, 257) (`あべ/T_BATUNI.JAF.jam:1976-1985`).
- A 480×370 CG is a **large unit**: anchored at (467, 322), flagged large (`あべ/T_BATUNI.JAF.jam:1744-1762`).

### 2.3 Poses and motion
- **Slide in / out**: horizontal, linear, from/to just outside the screen edge of the unit's side
  (player from x = −width, enemy from 800 + width). Default 250 ms (`あべ/T_BATUNI.JAF.jam:351-483` `moveIn`,
  `:502-633` `moveOut`, defaults `:2657-2665,2720-2728`). While the player hovers enemy cards the preview
  slides in/out in **100 ms** (`あべ/T_BATUNI.JAF.jam:3201,3277`).
- **Move down** (guard swap, `戦闘ユニット_交代`): slides straight down to y ≥ 320 in 500 ms, then the guard
  slides in (`あべ/T_BATUNI.JAF.jam:650-778`, `あべ/C_BATUNI.JAF.jam:66-72`); a large unit slides out instead.
- **Attack pose**: the whole composition is rebuilt with attack CGs (status 1, z402) — commander and
  second character use per-character attack CGs (`g_sUnitChara.getAttackCgNo`), soldiers per-type attack
  variants (random) (`あべ/T_BATUNI.JAF.jam:1340-1537` `createSp2`, `:2027-2092` `getBlendAttack`,
  `あべ/T_UNIDAT.JAF.jam:2415-2638`). Attack CGs are anchored with an offset of (w − 270, h − 270) plus a
  per-character correction (`あべ/T_BATUNI.JAF.jam:1769-1788`). For **melee types** an **extra wave** of the
  forward soldiers is added 267 px further left (towards the own edge) when troops > template size
  (`あべ/T_BATUNI.JAF.jam:1546-1626`, `:1792-1793`). There is **no tweened charge**: the pose is swapped in,
  held during the effect, and swapped back (`あべ/C_合戦.JAF.jam:2466-2476`).
- **Hero pose** (Rance's 勇者 attack): single figure, status 2/3 (`あべ/T_BATUNI.JAF.jam:215-304`).
- **Quake** (action 79, the great earthquake): background and both units jitter by random ±25 px every
  frame for 750 ms, sound 2202 (`あべ/T_BATTLE.JAF.jam:1745-1857` `tagBattle@quake`, `あべ/C_BATTLE.JAF.jam:301-309`).

### 2.4 Effects
A frame animation, CG `nCgNo + frame`, played full-screen at (0,0), z406, mirrored when the actor is an
enemy (`あべ/C_BATTLE.JAF.jam:1414-1452`, `あべ/T_BATEFF.JAF.jam:87-118,889`). The effect is per character/action
(`g_sEffectData`), defaults: physical type 1 → 40230, 30 frames; type 2 → 40360, 30 frames (HP-mode:
46180 / 46100, 15 frames) (`あべ/T_BATEFF.JAF.jam:927-977`). Duration = 750 ms when battle speed is 0,
otherwise the action's own time or frames × 50 ms (30 frames = 1500 ms) (`あべ/T_BATEFF.JAF.jam:665-695`).
Its sound comes from a CG→sound table (`あべ/T_BATSND.JAF.jam:550-564`, played at `あべ/T_BATEFF.JAF.jam:280`).
For multi-target "other" actions a copy is placed on each target **card** (card x + 1 for player cards,
+25 for enemy cards, y + 1, z card + 50) (`あべ/T_BATEFF.JAF.jam:516-562` `setPosFromBusho`, `あべ/C_BATTLE.JAF.jam:1477-1497`).
A global option `getBattleEffect()` switches between pre-built and per-frame sprites (`あべ/T_BATEFF.JAF.jam:240,402`).

#### 2.4.1 The effects' table and pictures (2026-10-14)
Surveyed as the reference for our own exchange effects (docs/ui/battle-screen.md, "The exchange's effects").
**[V]** = read in the code or the pictures, **[I]** = inferred.

- **The table** **[V]** is built in `tagEffectDataManager@0` (`あべ/T_EFFDAT.JAF.jam:56-932`) by 95 calls:
  `add3(cg, frames, soloCg, soloFrames, type…)` by troop type (14), `add1(cg, frames, soloCg, soloFrames, action…)`
  by action (65), `add2(cg, frames, character)` by character (16); the lookups are `search1-3`, `:1101-1259`. By
  troop type (types as in 02-resolution.md §2): 40000 Samurai (1) and the samurai- and ashigaru-like monsters
  (27, 28); 40030 Ashigaru (3) and Cavalry (2); 40060 Tactician (13); 40100 Ninja (6); 40130 Musketeer (4); 40160
  Onmyouji (8; solo 45040); 40200 Sohei (7; and 32, 33); 40230 Peasant (24) and 29; 40260 Archer (5) and Miko (9;
  solo 45020); 40300 Knight (10); 40330 Cannoneer (12); 40360 Mage (11) and 31; 40530 Hanny (23) and 26. Without an
  entry the default is 40230 for a physical action, 40360 for a magic one, 30 frames (solo 46180 / 46100, 15;
  `tagBattleEffect@getDefaultCgNo`, `あべ/T_BATEFF.JAF.jam:927-977`). Every entry has 30 frames.
- **The pictures** **[V]**: CGs in `Rance7GA.ald` (2279 of them in 40000-49999; WebP in the MangaGamer release — the
  Japanese release's format was not looked at), each frame **800×314**, the whole top scene (§2), **opaque, black
  where nothing is**: the effect sprite is drawn with draw method 1 (`tagEffectSp@setDrawMethod 1`,
  `あべ/T_BATEFF.JAF.jam:96-100,478-482`), which adds the frame's light to the scene — screen rather than plain
  addition after xsystem4's numbering **[I]**. One frame per 50 ms (§9.11). Mirrored for an enemy actor (above), so
  the drawn side is: **the actor on the left, the target on the right**.
- **What they show** (looked at, every third frame; `tools/sengoku/effect_frames.py` extracts them and makes the
  sheets — into a folder outside the repository):
  - *Samurai, 40000* (the melee reference): on the actor's half a **dust cloud**, sand-coloured (about
    200, 190, 140): frame 0 a scatter of small round puffs, by frames 3-6 merged into one bright billowing cloud
    with a fine speckled grain, then darkening slowly to nothing by frame ~27. On the target's half **crescent
    slashes** (white cores, pale blue to violet edges, sizes from a few pixels to a third of the height, at every
    angle), **"urchins"** (dark blue-teal rings with fine radial spikes), **four-point white sparkles** and small
    rings; brightest at frame 3, new slashes until frame ~21, all gone by frame ~27.
  - *Ashigaru/Cavalry, 40030*: the same dust; on the target's half **thrust wedges** (long white-blue spiky jets
    pointing at the actor, as from spear points) instead of crescents, the same sparkles and small blue rings.
  - *Archer, 40260*: no dust. White **arrows** with fletching fly from the actor's half up and to the right (frames
    0-6), then yellow arrows streak in over the target's half and fall down-right (frames 6-27); on the target,
    **gold bokeh puffs**, small gold splats and teal crescent streaks; a few bright arcs linger on the actor's side.
  - *Mage, 40360* (Fire Blast): on the target's half a **glowing orb** with rays (frame 0), a white-hot ellipse
    (frame 3), a **fireball** of yellow, orange and white billows over the whole half (frames 6-15) with a pale
    **shockwave arc** running off its left edge toward the actor, then embers, gold four-point sparkles and a soft
    white glow fading by frame ~27.
- **Sound**: each effect's sound comes from a CG → sound table (`あべ/T_BATSND.JAF.jam:550-564`), played as it
  starts (`あべ/T_BATEFF.JAF.jam:280`) — not surveyed further.

---

## 3. Top HUD

### 3.1 Score bar (戦果, the tug-of-war)
`tagBattleBar`, shown unless the battle has no time limit (`◆戦闘時間制限 == 1`) or is a solo battle
(`あべ/T_BATBAR.JAF.jam:29-35`). Anchored at **(173, 3)** after dropping in from above in 500 ms
(`:443-469,490-517`).

| Element | Pos (relative to 173,3) | Content | Ref |
|---|---|---|---|
| base | 0,0 | CG 10241 with the two clan emblems blended in: player `10500+clan` at (1,1), enemy at (428,1) | `あべ/T_BATBAR.JAF.jam:41-69` |
| bar | 27,1 · 400×9 | left `nW1` px from CG 10243 (player), the rest from CG 10244 (enemy) | `:76,758-806` |
| boundary line | 24 + nW1, −2 | CG 10245 | `:80,680,793-806` |
| centre arrow | 219, −3 | CG 10242 (marks 50 %) | `:84,653` |
| info left / right | 27,12 / 217,12 | 14 px white, 1 px black shadow: "Battlefield Adjustment +N%" when the side's `◆合戦補正` > 0 | `:90-140,591-623,661-669` |

`nW1 = 400 · score1 / (score1 + score2)` (200/200 when both are 0; rounding remainder goes to the larger
side) (`あべ/T_BATBAR.JAF.jam:820-897`). It starts at 50/50 (`あべ/T_BATTLE.JAF.jam:378`), is reset from the scores
every loop (`あべ/T_BATTLE.JAF.jam:438` `tagBattle@paint`), and animates linearly with every damage/heal effect
(`あべ/T_BATTLE.JAF.jam:1668-1705`). Score corrections (`戦果補正`, e.g. +5 per destroyed unit,
`あべ/T_BATTLE.JAF.jam:681-723`) are replayed one by one, 250 ms each (`あべ/T_BATTLE.JAF.jam:265-311` `updateBar`).
At the end the bar "fades" with a 16-step scan-line wipe (`あべ/T_BATBAR.JAF.jam:236-293,918-1000`).

### 3.2 Dungeon plate
In maze battles a plate CG 10215 at **(174, 3)** (z331) replaces the bar, text "<dungeon name>  depth/floors",
18 px white bold (`あべ/T_BATDUN.JAF.jam:34-160,191-217`).

---

## 4. Turn-order strip ("the strip of names") and the arrow

`tagBattleSequence` / `tagBattleFlag`. It is the **predicted action order**, leftmost = the unit acting now
/ next (`順番_番号(i+1)` → slot i, `あべ/T_BATSEQ.JAF.jam:57-74`). 16 entries are kept (`code.jam` `tagBattleSequence@2`),
**8 are visible**: slot i at **x = 100·i, y = 289**, z511 (`あべ/T_BATSEQ.JAF.jam:2207,2216,987`). Off-screen
slot 8 is x = 800.

Each flag: base CG **10193 (player, blue)** or **10194 (enemy, red)**; the name in an 88×14 sprite at
(+9, +8), 14 px gothic, white with a 1.25 px edge — **blue (100,160,255)** for player units, **red
(255,100,100)** for enemies (`あべ/T_BATFLG.JAF.jam:46-73,146-189,718-727`). So colour = side.

Animations (move time 500 ms, 250 ms at fast speed, `あべ/T_BATSEQ.JAF.jam:2230-2243`):
- Battle start: all flags fly in from x = 800, flag i starting at i·250 ms (125 ms fast), 150 px per
  move-in period, until their slot (`あべ/T_BATFLG.JAF.jam:605-698`, `あべ/T_BATSEQ.JAF.jam:617-825`).
- After a unit acts and is re-inserted (`tagBattleSequence@insert`, `あべ/T_BATSEQ.JAF.jam:171-277`): its old flag
  **sinks** 25 px (y 289 → 314, behind the panel) and hides, the flags after it **slide left** one slot,
  then, if the new position is within the first 8, the flags from there **slide right** and the new flag
  **rises** from y 314 to 289 (`あべ/T_BATFLG.JAF.jam:375-480` modes 1–4).
- Removal (destroyed / no actions left): sink + slide left (`あべ/T_BATSEQ.JAF.jam:291-311,325-427`).
- While the big troop counters are up (§5) **all flags sink** (250 ms) and rise again afterwards
  (`あべ/T_BATTLE.JAF.jam:1212,1462` `moveAllStart(2/1, 250)`).

**Arrow** (`tagBattleArrow`, CG 10174, z601): while the player hovers a command button it marks where the
actor would be re-inserted: `x = 85 + 100·(n − 2)` (i.e. 15 px left of slot n−1, the gap) with
`n = 順番_次(速度補正値(actor, 行動_待ち時間(action)))`, bobbing between y 275 and 280, 1 px per 50 ms;
hidden when n is outside 0–8 or nothing is hovered (`あべ/T_BATARW.JAF.jam:27,59-114,124-173`,
`あべ/T_BATSEL.JAF.jam:1475-1494`).

---

## 5. Big troop counters (attack/heal numbers)

`tagBattleNumberCollection` ×2 (`あべ/T_BATTLE.JAF.jam:1182-1418` `showNumber2`). The **left** counter is
always the player-side unit, at **x = 42**; the right one the enemy, at **x = 598**
(`あべ/C_合戦.JAF.jam:2182-2194`). Each number: 4 digits of 40×34 (digit CG 10171 normal, 10172 damage,
10173 heal), so 160×34 (`あべ/T_BATNUM.JAF.jam:48-87`). One panel per target; several stack upward 42 px apart
(`あべ/T_BATNUM.JAF.jam:1547-1574`). They **rise from behind the panel** (from y 312 to y **278** for the lowest)
in 250 ms while the flags sink, z521 → 511 (`あべ/T_BATTLE.JAF.jam:1289-1414`), and drop back in 250 ms
(`:1442-1599`). A side with no value (−1) gets no panel (heals show only the healed side).

During the effect the number counts from the old to the new value: every 200 ms it jumps to the value due
200 ms later, and each digit "flies": on damage the old digit falls 68 px, drifts ±20 px outward and
rotates 180° while fading out in 200 ms; on heal the new digit drops in from 68–102 px above, fading in
(`あべ/T_BATNUM.JAF.jam:155-233,288-512,631-1102`). Clamped to 0–9999.

---

## 6. The bottom panel: unit cards

### 6.1 Grid
Six cards per side, 2 screen columns × 3 screen rows, each **178×80** (`あべ/T_BATBUS.JAF.jam:141`,
`あべ/T_BATTLE.JAF.jam:2201`). Each screen column is one of the side's rows (front/back); each
screen row holds one line.

| | screen column 0 = **front row** (places 1–3) | screen column 1 = back row (places 4–6) |
|---|---|---|
| player | x = **182** (inner, next to the centre) | x = **2** (outer) |
| enemy | x = **440** (inner) | x = **620** (outer) |

Screen rows: y = **322, 404, 486** (`322 + 82·row`). z = 601 + (index+1)·10, index = place − 1 =
column·3 + row (`あべ/T_BATPLS.JAF.jam:3614-3662` `calcBushoPosX/Y/Z`, `:87-264` `create`). Place p and
p+3 sit in the same screen row and form a line (`あべ/C_BATTLE.JAF.jam:625-695` `戦闘_敵列`);
"front" = places 1–3 (`あべ/T_BATPLS.JAF.jam:2536-2563` `isMikataMae`). The 80 px gap x 360–440
holds the time gauge (§7).

When a whole front row is dead, the front and back rows (screen columns) **swap**: the cards slide apart by 180 px in the
anime time (500/250 ms), sound 212, then are re-seated (`あべ/T_BATPLS.JAF.jam:1779-2158` `change`, called each
loop from `あべ/C_合戦.JAF.jam:229`).

### 6.2 Card anatomy
Coordinates relative to the card origin; **player | enemy** (the enemy card is mirrored).
z is relative to the card z.

| Element | Player | Enemy | Size | Source / content | z | Ref |
|---|---|---|---|---|---|---|
| base (idle) | 0,0 | 0,0 | 178×80 | CG 10202 \| 10222 | 0 | `あべ/T_BATBUS.JAF.jam:43,1024` |
| base (acting) | 0,0 | 0,0 | | CG 10206 \| 10226, replaces the idle base while the unit acts | 0 | `あべ/T_BATBUS.JAF.jam:53,1042` |
| face | 4,4 | 109,4 | 65×72 **[I]** | `chara.ＣＧ + 50000` (battle face), else `ＣＧｍ`, else a generated 65×72 navy box with the name in 10 px white | +1 | `あべ/T_FACE.JAF.jam:71-84,254-276`; `あべ/T_BATBUS.JAF.jam:1940,1985` |
| name plate | 57,3 | 27,3 | | CG 10203 \| 10223 | +2 | `あべ/T_BATNAM.JAF.jam:31,225-237` |
| name text | 65,3 | 29,3 | 84×14 | commander name, 14 px white bold, 1 px black shadow, centred, narrowed to fit | +3 | `あべ/T_BATNAM.JAF.jam:40,98-200,248-254` |
| status | 70,21 | 28,21 | 80×21 | see §6.4 | +2 | `あべ/T_BATST.JAF.jam:394-429` |
| troops "cur/max" | 70,44 | 28,44 | 80×17 | `兵数` / `兵数最大`, see §6.3 | +2 | `あべ/T_BATHP.JAF.jam:39,250-308`; `あべ/T_BATBUS.JAF.jam:1948,1993` |
| action squares | 71…136,62 | 29…94,62 | 6 × 13×17 | see §6.3 | +2 | `あべ/T_BATWAZ.JAF.jam:60,349-377` |
| "no actions left" mark | 69,61 | 26,61 | | CG 10199 when current actions < 1 | +7 | `あべ/T_BATBUS.JAF.jam:108,380,1957,2002` |
| buff icons | 6,68 (+15 each) | 112,68 (+15 each) | 4 icons | CG 10237–10240 = attack / defence / intelligence / speed buff, each shown while its value > 0 (in battle only) | +3 | `あべ/T_BATFUY.JAF.jam:35-53,161,214-240`; `あべ/T_BATBUS.JAF.jam:1814` |
| unit-type icon ("the icon on the side") | 155,33 | 7,33 | | `getIconNo(兵種)`, see below | +1 | `あべ/T_BATBUS.JAF.jam:917-952` |
| shikigami barrier | 150,0 | 1,0 | | CG 10207 \| 10227 while `Ｆ式神 == 1` and not destroyed | +7 | `あべ/T_BATBUS.JAF.jam:118,390,1966,2011` |
| level (solo battles only) | 3,3 | 175 − w − 12, 3 | | CG 10216 + digits CG 10220 (`Ｓレベル`) on an 11 px dark strip | +3 | `あべ/T_BATLVL.JAF.jam:33-38,146-190` |
| selection frame | −1,−1 | 23,−1 | | CG 10195 (covers the card body without the icon tab **[I]**) | +9 | `あべ/T_BATBUS.JAF.jam:132,1975,2020` |
| acting frame | −1,−1 | −1,−1 | | CG 10201 \| 10221, **blinks** (toggled every 500 ms) while acting | +5 | `あべ/T_BATBUS.JAF.jam:104,419-423,1896`; `あべ/T_BATPLS.JAF.jam:961-984` |
| press highlight / hit shape | 0,0 | 0,0 | 178×80 | white at 50 %, the two 24×26 corners of the icon strip cut out; always hidden in battle (only the deployment screen `あべ/T_SANSEN.JAF.jam:1521` shows it), but it is the card's mouse hit area | +9 | `あべ/T_BATBUS.JAF.jam:141-173,1178,1543` |
| destroyed overlay | 0,0 | 0,0 | | CG 10204 \| 10224 | +8 | `あべ/T_BATBUS.JAF.jam:128,1901` |
| result badge (after the battle) | 70,19 | 28,19 | | CG 10257 (`Ｆ戦闘結果` 1000 = died), 10256 (2000 = captured), else 10255 with the number (the troop gain) in 18 px white (01-flow.md §4.3) | +10 | `あべ/T_BATBUS.JAF.jam:1295-1472` |

**Card shape** [I]: the icon strip is the right 24 px (player) / left 24 px (enemy); its top and bottom
24×26 corners (y 0–25 and 54–79) are not card — the hit shape cuts them out, and the
spotlight keeps them dimmed (`あべ/T_BATBUS.JAF.jam:150-173`, `あべ/T_BATTLE.JAF.jam:2204-2246`). So the type icon sits on
a tab sticking out of the card body at y 26–53. The mouse hit test is `m_spFrame2.mouseIn()`, i.e. that
shape (`あべ/T_BATBUS.JAF.jam:843`).

**Type icons** (`あべ/T_BUSHO2.JAF.jam:1668-1726` `getIconNo`; names `多田/関数／本編.jaf.jam` `文字列／兵種`):
1 Samurai 10161 · 2 Cavalry 10160 · 3 Ashigaru 10162 · 4 Musketeer 10164 · 5 Archer 10163 · 6 Ninja 10165 ·
7 Sohei 10167 · 8 Onmyouji 10168 · 9 Miko 10166 · 10 Knight 10170 · 11 Mage 10179 · 12 Cannoneer 10178 ·
13 Tactician 10169 · 23 Hanny, 26–29, 31–33 (monsters) 10158 · 24 Peasant 10159 · 20 Animal, 25 Soul Bound: none.

### 6.3 Troops and the squares
- **Troops (mode 0)**: an 80×17 sprite with `"    /MMMM"` centred (16 px white bold; the max right-aligned
  in 4 digits); the current value is a separate 4-digit number at (+4, +1), 16 px, **yellow (255,248,48)
  while in battle and below the troops it had at the battle's start (`兵数合戦`)**, white otherwise
  (`あべ/T_BATHP.JAF.jam:715-843` `drawCount`, `setForce`). It counts down/up with every effect
  (`あべ/T_BATHP.JAF.jam:122-226`).
- **HP mode (1)** (solo/dungeon/individual units): bar CG 10219 at (+1, +11) on CG 10218, digits CG 10220:
  current at (+19, +1), max at (+55, +1) (`あべ/T_BATHP.JAF.jam:45-60,300-309,471-487`).
- **Squares = action points (技数)**, max 6, filled **from the right** (slot 0 = rightmost):
  slots `< current − reserved` → CG **10196** (available); `current − reserved … current−1` → CG **10198**
  (would be spent by the hovered command); `current … max−1` → CG **10197** (spent); beyond max → empty
  (`あべ/T_BATWAZ.JAF.jam:146-266` `set`). `current` = `キャラ_現在技数` in battle (dungeon: `キャラ_迷宮技数`), max =
  `キャラ_技数` (`あべ/T_BATBUS.JAF.jam:1739-1772`). In **solo battles enemy cards** show one "infinite" sign
  instead, CG 10217 at (+95 | +53, +64) (`あべ/T_BATBUS.JAF.jam:71-88`, `あべ/T_BATWAZ.JAF.jam:37-68,385-405`).

### 6.4 Status line (`ＦＦ`, `ＦＦＥ`)
Shown only in battle (`あべ/T_BATST.JAF.jam:28`, `:289-356`):

| `ＦＦ` | meaning (`あべ/C_合戦.JAF.jam:3325-3365,3643-3651`) | drawn |
|---|---|---|
| 1 | charging (ため) | CG 10155 |
| 2 | confused | an empty 80×21 50 % black box (no text is ever drawn into it) |
| 3 | defending | same empty box |
| 21, 22 | guard (front/back-row restricted) | CG 10156 + 3-digit number `ＦＦＥ` (guard chance %) at (+50, +7) |
| 23 | guard (any) | CG 10157 + `ＦＦＥ` |
| 99 | destroyed | no status; destroyed overlay instead |

### 6.5 Card states
| State | Trigger | Look | Ref |
|---|---|---|---|
| empty slot | no commander | card hidden | `あべ/T_BATBUS.JAF.jam:874-883` |
| idle | | base 10202/10222 | |
| acting | `戦闘_行動開始` | base2 + acting frame blinking every 500 ms | `あべ/T_BATTLE.JAF.jam:920-933`; `あべ/T_BATBUS.JAF.jam:1009-1093,1201-1210` |
| hovered / selected | mouse over (player turn) or target chosen | selection frame 10195, sound 201 | `あべ/T_BATBUS.JAF.jam:1491-1513` |
| target announced | enemy picks a target | selection frame **blinks** (toggle every anime/5 = 100 ms for 500 ms), then stays | `あべ/T_BATPLS.JAF.jam:2315-2397`; `あべ/C_合戦敵.JAF.jam:1704,1756` |
| no actions left | current actions < 1 | CG 10199 over the squares | `あべ/T_BATBUS.JAF.jam:380,1114-1129` |
| destroyed | `ＦＦ == 99` | overlay 10204/10224; ignores the mouse; flag leaves the strip | `あべ/T_BATBUS.JAF.jam:372,834-837`; `あべ/T_BATTLE.JAF.jam:638-649` |
| spotlit | during an action's presentation | everything in the panel (0,320)–(800,568) is covered by black at 50 %, except the actor's and target's 178×80 cards (minus the icon-tab corners) | `あべ/T_BATTLE.JAF.jam:759-885,2140-2248`; `あべ/C_BATTLE.JAF.jam:204-268` |

Cards slide in at battle start from beyond their own side, 400 px over 1 s with a per-card stagger
(column·100 + random 0–99 px) (`あべ/T_BATPLS.JAF.jam:378-553`, `あべ/T_BATBUS.JAF.jam:636-778`). A retreating unit
(solo "Retreat") slides out 400 px towards its own edge (sound 214) and its replacement, if
`Ｓ補充` finds one (01-flow.md §4.2), slides back in (sound 215) (`あべ/T_BATPLS.JAF.jam:1131-1763`).

---

## 7. The central vertical gauge = battle time

`tagBattleTime` (`あべ/T_BATTIM.JAF.jam:17-124`): base CG 10249 at **(375, 326)**, z602; **24 segments** CG 10250
at **x 379, y 326 + 10·i** (i = 0…23), z603. Not shown when the battle has no time limit
(`◆戦闘時間制限 == 1`). It shows the remaining time `24 − 合戦ターン`, the lit segments being the
**bottom** n (`あべ/T_BATTIM.JAF.jam:183-233,355-376`), so it empties from the top. Every finished action
(including Wait and Retreat) calls `戦闘時間_減少`: the turn counter +1 and the top lit segment is
removed with a 6-frame effect (CG 30083 at segment −19, −6) and sound 211 (`あべ/T_BATTIM.JAF.jam:237-317`,
`あべ/C_BATTLE.JAF.jam:138-144,418-430`). A unit skipped for having no actions does not spend time
(`あべ/C_合戦.JAF.jam:252-262`). At 0 the battle is decided by the score (`あべ/C_合戦.JAF.jam:213-226`; rules: 01-flow.md §3.1, §4.1).

---

## 8. Commands: the menu on the hovered card

There is **no unit picking**: the actor is always the head of the turn order (`あべ/C_合戦.JAF.jam:60-78`).
The player picks **command + target in one click**, on the target's card.

### 8.1 Building the menus (`tagBattlePlace@selectAction`, `あべ/T_BATPLS.JAF.jam:671-788`)
For each of the 12 cards a button column is built (`あべ/T_BATSEL.JAF.jam:950-1133` `addButton`): for each of the
actor's 4 skill slots (`キャラ_技(0, actor, i)`, i = 1…4) a button is added **if the skill may target
that card** (`checkAction`, `あべ/T_BATSEL.JAF.jam:2066-2189`):

| target class `行動_ターゲット` | card it appears on |
|---|---|
| 1 | enemy front row, and only if the actor is in the front row |
| 2, 5, 7 | any enemy (5 = that line front+back, 7 = all enemies) |
| 6 | any enemy, only if the actor is in the front row (hits the line) |
| 3, 8 | any ally (8 = all allies) |
| 4, and type 0 (Wait) | the actor's own card |
| type 9 actions 21/22 | allies in the actor's own row (front/back); action 23: any ally |

Solo battles add **"Retreat" (999)** on every other player card (`あべ/T_BATSEL.JAF.jam:1108-1128`,
`あべ/C_行動.jaf.jam:56-58`). Cards where nothing applies get no buttons — that is the only "invalid target"
cue (the card still gets the hover frame and sound).

**Layout**: the column starts at (card x + 56, card y + 5) on player cards and (card x − 25, card y + 5) on
enemy cards; buttons stack downward **38 px** apart (so a 3–4 button column runs over the card below),
z card + 70 (`あべ/T_BATSEL.JAF.jam:35-78,175-185`). Button ID = `100000 + target·1000 + action`
(`:837,2031-2062`).

**Look** (`あべ/T_BATSEL.JAF.jam:242-378`): text = short action name (`GetActionNameShort`, `あべ/C_行動.jaf.jam:95-138`),
16 px bold white, 2 px shadow, centred, margins 7 (16 on the side towards the card body: left on ally
cards, right on enemy cards).

| | normal / hover CG | text shadow | multi-target marker |
|---|---|---|---|
| ally card, normal skill | 10185 / 10188 | green (0,97,48) | 10182 |
| ally card, special (必殺技) | 10181 / 10188 | blue (0,0,128) | 10180 |
| enemy card, normal skill | 10186 / 10189 | orange (172,57,0) | 10183 |
| enemy card, special | 10187 / 10189 | red (150,0,0) | 10184 |

"Special" = `行動_技数 == 99` (`あべ/C_行動.jaf.jam:681`). **Active** if current actions ≥ the skill's cost
(special: > 0) (`あべ/T_BATSEL.JAF.jam:1068-1102`); an inactive button is the normal image **darkened 50 %**
(`あべ/T_BUTTON.JAF.jam:356-362`) and clicking it only plays the cancel sound.

### 8.2 What hovering does
1. **Over a card** (`あべ/T_BATPLS.JAF.jam:2935-3150` `checkMouse`): frame on that card (sound 201), its button
   column shown (only one column visible at a time). If it is an **enemy** card, that enemy's troops slide
   into the scene in 100 ms (previous preview slides out); hovering an ally card removes the preview.
   Buttons are tested before cards, so a column hanging over the next card keeps priority.
2. **Over a button** (`あべ/T_BATPLS.JAF.jam:3353-3438`, `あべ/T_BATSEL.JAF.jam:1221-1496`): button highlight
   (hover sound `playSelect`, `あべ/T_BUTTON.JAF.jam:858-865`); the actor's squares show the cost as "reserved"
   (§6.3; a special reserves all); the strip arrow shows the predicted next turn (§4); for line skills
   (5/6) the other card of the line, and for all-target skills (7/8) every other card of the group, shows a
   ghost of the button (marker CG + name at 75 % opacity, at the same button slot)
   (`あべ/T_BATSEL.JAF.jam:1322-1470`, `:684-787`).
3. **Tooltip** after **1000 ms** on the same button: `行動_説明` (Wait's tells that Space waits), white box, 1 px black top/left border, 6 px half-transparent shadow, 18 px text, ≤ 320 px wide;
   at (column x, column y + 36 for the first button, column y otherwise), pushed left/up to stay on screen
   and above y 568 (`あべ/T_BATSEL.JAF.jam:1747-1897`, `あべ/T_COMMNT.JAF.jam:111-384,462-556`).

### 8.3 Clicks and keys (`あべ/T_BATPLS.JAF.jam:2844-3337`; key codes `あべ/C_KEY.JAF.jam:203-213,320,330`)
| Input | Effect |
|---|---|
| left button or Enter on an **active** button | click sound; if the mouse is still held it tracks the press (button highlight follows whether the pointer is still on it); **releasing on the button** confirms, releasing elsewhere cancels |
| left/Enter on an inactive button, a card, or empty space | cancel sound, nothing happens |
| **Space** | confirms the actor's **Wait** (action 1 on its own card) if available |
| **right button** (VK 2) | moves the mouse pointer to the centre of the actor's Wait button (`setCursor` → `mouseSetPosCenter`, `あべ/T_BUTTON.JAF.jam:977`) **[I: VK 2 = right button]** |
| system bar (IDs 90000+) | 90001 opens the system menu (`あべ/T_BATPLS.JAF.jam:765-771`) |
| Ctrl | skips the battle dialogue boxes (`あべ/T_BATMSG.JAF.jam:399,464`) |

There is no cancel/back step (nothing to cancel: one click = command + target).

---

## 9. Step-by-step sequences

Speeds: `getBattleSpeed()`: below 2 halves the move/anime times (500 → 250, flag fly-in 250 → 125); 0 also
fixes every effect to 750 ms (`あべ/T_BATPLS.JAF.jam:4077-4090`, `あべ/T_BATEFF.JAF.jam:665-671`).
`getAoeEffect()` = 1 plays a multi-target attack as one combined presentation, 0 one target after another
(`あべ/C_合戦.JAF.jam:1192,1530,638`).

### 9.1 Battle start (`あべ/T_BATTLE.JAF.jam:74-136` `tagBattle@start`)
Clear the screen, battle music, then the **start card**: player banner `10600+clan` at (0,0), enemy banner
`10650+clan` at (400,0), "VS" CG 10175 at (374,256), and for attack/defence battles a map plate (CG 10176 at
(258,2): "Battle at <province>", "Controlled by <clan>", facilities) (`あべ/T_BATSTR.JAF.jam:64-152`,
`あべ/T_BATMAP.JAF.jam`). **Waits for a click**, then the banners split apart 400 px and fade over 1000 ms
(`あべ/T_BATSTR.JAF.jam:194-269`). Fade; cards slide in (§6.5); score bar / dungeon plate drop in and the flags fly
in (§4); units created. No troops on stage yet.

### 9.2 Each loop (`あべ/C_合戦.JAF.jam:27-93` `Ｐ合戦処理`)
end check + front/back swap (§6.1) → turn-order time update → `戦闘_情報表示` (bar, strip, all cards
refreshed, `あべ/T_BATTLE.JAF.jam:438-447`) → actor = head of the order → player or enemy turn → re-insert the
actor in the order (§4 animation; removed if it has no actions left, `あべ/C_BATTLE.JAF.jam:356-388`).

### 9.3 Player turn (`あべ/C_合戦.JAF.jam:243-364`)
1. No actions left → skipped silently (no animation).
2. `戦闘_行動開始`: previous actor cleared; actor card turns "acting" and blinks; **the actor's troops slide
   in** from the left (250 ms) (`あべ/T_BATTLE.JAF.jam:920-933`). A charging (ため) unit fires its stored action
   now without the menu (`あべ/C_合戦.JAF.jam:508-519`).
3. Menu (§8) until a confirmed click; preview sprites removed.
4. Targets resolved; for single targets the target's troops slide in from their side; if an ally guard
   triggers, the target's troops sink (500 ms) and the guard's slide in (`あべ/C_合戦.JAF.jam:830-860`).
5. Presentation (9.5–9.8), actions spent (`あべ/C_合戦.JAF.jam:346-359`).
6. `戦闘_行動終了`: target troops slide out, then actor troops (250 ms each), acting state and spotlight
   off, **one time segment removed** (§7) (`あべ/T_BATTLE.JAF.jam:936-962`).

### 9.4 Enemy turn (`あべ/C_合戦.JAF.jam:376-496`, `あべ/C_合戦敵.JAF.jam:1653-1806`)
Same, without the menu: the actor card blinks, its troops slide in from the right, the AI picks; the
**target card's frame blinks for 500 ms**, then the target's (player) troops slide in from the left; guard
swap as above. No click is needed anywhere.

### 9.5 Single attack (`あべ/C_合戦.JAF.jam:1099-1147` → `合戦_戦闘処理` `:2137-2504`)
1. effect prepared; target troops in (if not already);
2. **spotlight** on actor and target cards (frames removed);
3. **counters rise** (left = player unit, right = enemy unit), flags sink — 250 ms;
4. damage computed (and the counter-damage if the skill allows it; rules: 02-resolution.md §3);
5. actor troops switch to the **attack pose**;
6. **one timed animation** of length = effect time (`あべ/T_BATTLE.JAF.jam:1618-1716` `tagBattle@damage`): the effect
   frames play over the scene, both counters tick down with flying digits, both cards' troop numbers count
   down, the score bar slides;
7. actor back to normal pose; counters drop, flags rise — 250 ms;
8. destruction check (9.9); end of turn (9.3/6).
No pause or click between steps; the whole attack is ≈ 250 + effect (750–1500 ms) + 250 ms plus the slides
(the details, and what a wiped-out unit adds: §9.11).

### 9.6 Multi-target attack (`あべ/C_合戦.JAF.jam:1165-1374`)
- Combined (`AoE effect` = 1): spotlight on the actor and **all** targets; if none of the targets is on stage
  a random one slides in; counters: one for the actor, one stacked panel per target; all damage computed;
  one effect and one animation for all (`あべ/T_BATTLE.JAF.jam:2254-2563` `damageall`); counters drop.
- Sequential (= 0): for each target in turn: its troops slide in, then 9.5 steps 2–7.

### 9.7 Heal (`あべ/C_合戦.JAF.jam:1424-1726,2509-2710`)
Spotlight; only the healed side's counter rises; actor in attack pose; counter and card count up with digits
dropping in (`あべ/T_BATTLE.JAF.jam:1861-1975` `heal`); a target without wounded men is skipped (no animation).
Multi-heal: like 9.6 (the healer's own heal first). Automatic healing skills run the same at the end of
the unit's turn (`あべ/C_合戦.JAF.jam:2967-3163`).

### 9.8 Other actions (`あべ/C_合戦.JAF.jam:1729-1924`)
Guard, status, time change, score change: spotlight on actor (+targets), attack pose, the effect plays
(no counters), the affected cards refresh (`あべ/T_BATTLE.JAF.jam:1977-2138` `etc`/`etc2`). Time change waits
750 ms more; score change animates the bar. Earthquake (79): every target in turn with the quake (§2.3).
Charging: only the log line (a no-op, see §11) and the status "charging" appears; turn ends.

### 9.9 Destruction (`あべ/C_合戦.JAF.jam:3166-3197`, `あべ/T_BATTLE.JAF.jam:590-726`)
A unit at 0 troops gets `ＦＦ = 99`; after the presentation its flag sinks out of the strip, the card gets
the destroyed overlay and stops reacting; the other side gets +5 score per destroyed unit, replayed on the
bar. In solo battles the destroyed card slides out instead and a reserve slides in.

### 9.10 Battle end (`あべ/T_BATTLE.JAF.jam:138-226`, `あべ/T_BATEND.JAF.jam`)
Bar wipe-fade; bar set to the final scores (100:0 / 0:100 on annihilation); result badges on the cards;
win/loss effect (sound 221/222, music lowered), then a land panel (CG 10248 at (223,211), facilities at
(267,217)) cross-fading the province's land count ±1 (sound 223/224); a click skips each phase.

### 9.11 The end of an attack and its waits (2026-10-14)
Surveyed for our battle screen's exchange result wait (docs/ui/battle-screen.md, question 3). **[V]** = read
in the code, **[I]** = inferred.

- **No click, no key, no pause of its own** **[V]**. Every step of an attack is a timed loop
  (`while (nTime < X) { …; UPDATE; nTime = sTimer.Get }`), and `UPDATE` only updates the sprites, peeks the
  message queue and sleeps 1 ms (`C_スクリ.jaf.jam:2-22`, `System/SACT/SCREEN/sact_screen.jaf.jam:2-44`). None
  of `T_BATTLE`, `T_BATNUM`, `T_BATEFF`, `T_BATSEQ`, `T_BATUNI`, `T_BATBAR` reads a key or the mouse (no key
  call in them at all). The only input read in a battle turn is the player's own choice (§8; `T_BATPLS:2907`,
  `:3274-3313`) and, between actions, the system bar (`戦闘_システム確認`, `T_BATTLE:895-913`) **[I: polls only]**.
- **The order after the blow** **[V]**: the damage loop ends (`T_BATTLE:1677-1702`: the counters, the cards'
  troops, the bar and the effect all run on one clock) and everything is closed at once (`:1703-1716`); the
  actor goes back to its normal pose (`C_合戦:2471-2477`, no time); the counters drop, **250 ms**
  (`戦闘_兵数消去` → `hideNumber`, `C_合戦:2479`, `T_BATTLE:1538-1591`); the destruction check and its
  processing (`C_合戦:1133-1144`, below); the assassination follow-up (`C_合戦:749-754`, only with `ＧＦ[55]`,
  a text through `ＴＸ` — whether it waits was not traced); the turn's end (`合戦_行動終了`, `C_合戦:761-768`):
  automatic heals (§9.7), then the target's and the actor's troops slide out, **250 ms each**
  (`T_BATTLE:936-962`, `T_BATUNI:2716-2728`, loop `:574-620`), the time gauge ticks without blocking
  (`T_BATTIM:296-313`, sound 211); the next actor at once (`C_合戦:33-88`).
- **The counters do not hold** **[V]**: the drop follows the damage loop directly; rise and drop are 250 ms
  whatever the speed (`T_BATTLE:1211,1344,1461,1540`). The number moves in 200 ms steps, each showing the value
  due 200 ms later (`T_BATNUM:230,310-314,341-369`), so the final value stands still only the last 0-200 ms of
  the loop **[I: from the arithmetic]**: ~100 ms with a 1500 ms effect, ~150 ms with 750 ms.
- **The effect's length** **[V]** (`tagBattleEffect@getTime`, `T_BATEFF:656-697`): 750 ms at speed 0; else the
  effect's own time if set (750 for the targets of a multi-target attack, `bAllFlag`, and for solo battles'
  effects 47000-48999, `:749-754,807-827`); else its frame count × 50 ms (30 frames = **1500 ms** in a normal
  battle, 15 in a solo battle, `:932-968`); else 1500 ms. An attack with the default effect therefore takes
  250 + 1500 + 250 ms, then 500 ms of slides out: **≈ 2.5 s from the counters' rise to the next actor**, ≈ 1.75 s
  at speed 0.
- **A unit wiped out** **[V]**: `合戦_壊滅チェック` (`C_合戦:3166-3200`) sets `ＦＦ = 99` and logs a line that
  is never shown (§11) — no mark, sound, effect, pause or click. `tagBattle@destroy` (`T_BATTLE:590-726`; the
  actor's own `actionEnd` first if it was the one destroyed, `C_BATTLE:404-415`): the card's destroyed overlay
  at once (`setBushoDestroy`, `T_BATBUS:1213-1232`); the flag sinks out of the strip and the strip closes the
  gap, **500 ms each** (250 below speed 2; `T_BATSEQ:280-312,1256-1293,1319-1334`, `getMoveTime`
  `:2220-2245`; several at once together, `moveDown2`/`moveLeft2`); +5 to the other side's score per unit
  destroyed, the bar sliding **250 ms** (`T_BATTLE:694-723,300-308`, `T_BATBAR:250-281`). So a destruction adds
  ≈ 1.25 s of movement and nothing to click. Solo battles: the card leaves (`leave2`, sounds 214/215,
  `T_BATPLS:1278ff`) and a reserve comes in.
- **Commanders who fall** **[V]**: no scene or message during the battle; dead / captured is shown only at the
  battle's end, as the cards' result badges (§9.10; `tagBattlePlace@kekka`, `T_BATTLE:173,196`;
  `T_BATPLS:555-602`; CG 10257 for 1000, 10256 for 2000, else 10255, `T_BATBUS:1294-1330` — which code is death
  and which capture **[I]**).
- **Settings** **[V]** (`T_SYSMEN:2118-2263`; the labels are English in this build): **Combat Speed** —
  `getBattleSpeed() = (g_nBattleMode & 255) % 10` (`T_GLOB:284-294`, clamped 0-2 `:912-924`, default 2
  `:41`): 2 "Standard", 1 "Lil Fast", 0 "Max Speed" (with a warning that it disturbs graphics and sound). Below 2
  the strip's moves, the card blink (`T_BATPLS:4067-4092`) and the flags' fly-in (`T_BATFLG:849-870`) are halved;
  at 0 every effect is 750 ms. The counters' rise and drop, the troops' slides, the bar and the 200 ms number
  steps never change. **Combat Effect Display** (`getBattleEffect() = (mode & 255) / 10`): "Standard",
  "Limited", "Don't Show" — only how the effect's sprites are made (`T_BATEFF:236-244,398-406`, `T_EFFSP:74,200`);
  `getTime` does not read it, so "Don't Show" does not shorten the attack **[I]**. **Multi-Target Attacks**
  (bit 9): "Animate Each Target" / "Animate All At Once" (§9.6). Message skip is off for the whole battle
  (`msgSkipStop`, `T_BATTLE:72`, restored `:224`). No auto-battle and no skip-animation option.
- **Hurrying it** **[V]**: impossible — no timing loop reads input, and the two screen transitions in an attack
  are `EFFECT(1, 250, 0)` with the key flag 0 (`C_合戦:579-582,2956-2959`). The player's only lever is the speed
  setting.
- **The enemy's attacks** end the same way **[V]** (`合戦_敵行動` `C_合戦:369-499` mirrors `合戦_味方行動`
  `:236-367`; `◆合戦味方ターン` only picks which counter shows which number, `:2179-2195,2428-2458`); the only
  extra beat is before the blow: the target's card blinks 500 ms (250 below speed 2; `T_BATTLE:1017-1024`,
  `T_BATPLS:2328-2391`, `C_合戦敵:1704`).
- **Heals and multi-target attacks** **[V]**: the same pattern, no wait at the end (`C_合戦:2509-2713`,
  `T_BATTLE:1861-1975`; revive 166 then `戦闘_復帰`, `C_合戦:1452-1457`). "Each Target": rise → effect → drop for
  every target back to back, those after the first at 750 ms (`C_合戦:1304-1343`); "All At Once": one 750 ms
  `damageall` loop (`T_BATTLE:2429-2561`); the destruction checks after all targets (`C_合戦:1345-1371`).

In short: Sengoku Rance never stops after an attack. The result is read during the ≈ 1.5 s the counters run;
the numbers vanish 250 ms after they settle; a wiped-out unit is told by its card's overlay, its flag leaving the
strip and the bar, all moving, nothing to click; the player can only choose a faster speed.

---

## 10. Battle dialogue boxes (`あべ/T_BATMSG.JAF.jam` `Ｐ合戦会話`, called from the scenario scripts)
Face box CG 10011 at (12,47) (left speaker) or (590,47) (right), face inside at (+6,+33) on a 186×220 dark
area, name title on top; speech balloon CG 10251/10252 (left: talk/think) or 10253/10254 (right) at
(200,234) / (238,234); text 329×115, 24 px bold, black (white when thinking) (`:104-354`).
`Ｐ合戦会話` shows it for **2 s** (`W(2000,1)`, `:388`); `Ｐ合戦会話２` waits for a click/Enter with an
animated cursor CG 10013 (`:399-487`).

## 11. Not on the screen
`LOG_BATTLE` (all the "X attacks Y, N men lost" lines in `C_合戦`) is an empty function
(`あべ/Dummy/DUMMY.JAF.jam` FUNC 4601) — Sengoku Rance shows **no battle text log**; damage is shown only by the
counters, the card numbers and the bar.

---

## 12. Open questions
1. Exact pixel sizes and look of CGs 10181–10189 (buttons), 10195/10201 (frames), 10202/10222 (card bases),
   10249/10250 (gauge) — the CGs were not extracted; sizes above are only those the code creates.
2. Statuses 2 (confused) and 3 (defending) draw an empty dark box: the text sprite is created but never
   written in this (English) build; whether the Japanese build drew a label is unknown. (Neither state
   is ever reached in battle, 03-skills.md §8.)
3. The system bar (IDs 90000+) and its placement during battle were not surveyed (`tagSystemBar`).
4. ~~The battle-speed values and their names~~ — answered in §9.11 (2 "Standard", 1 "Lil Fast", 0 "Max
   Speed").

Settled while the four parts were made consistent: the result badges show `Ｆ戦闘結果` (1000 died,
2000 captured, else the troop gain; §6.2, 01-flow.md §4.3), and every commander has Wait in slot 2,
so Space and the right button always find it (01-flow.md §6).
