# Battle specification 03 — RNG draw order of a whole battle

One ordered walkthrough of every random draw from the battle's caller to the post-battle
wrapper, merging the RNG tables of [01-flow.md §12](01-flow.md#12-rng-draws-in-order) (ids
**R1-R19**) and [02-resolution.md §8](02-resolution.md#8-rng-draws-in-order) (rows **02#1-02#20**).
A port that consumes draws in exactly this order reproduces the original sequence.

## 0. Rules

- `Y 4, n` is the game's **only** random source (`Y 3` is a pause; `ZR` and DLL modules are
  unused). `RND(n)` returns 1..n; **n = 0 or 1 returns n and does not draw** — every "draws"
  below means "draws if n ≥ 2".
- Coverage: every `Y 4` site of the battle files is placed below — `FIGT.ADV` 36 sites,
  `FIGT作.ADV` 2, `FIGT_B.ADV` 2, `FIGTATTK.ADV` 1, `FIGT_M.ADV` 92 (all listed in 02 §5.2/§5.3).
  `FIGT_A`, `FIGT_SEL`, `Sﾌﾞﾀｲ`, `死亡捕獲`, `FIGTﾐｶDE`, `FIGTﾃｷDE`, `WAO_SUB`, `PCMPLAY` and `M.ADV`
  contain none. `Y 3` at `FIGT.ADV:1418/1898/1910` and `FIGTATTK.ADV:127/133` are pauses.
- `RedrawBattlefield` (`L_03058`) and `Highlight` (`L_0320f`) never draw but overwrite `RetVal`
  (`FIGT.ADV:2075`); no draw result is read across them.
- Draws inside story events started by the wrappers or by 死亡捕獲 belong to those events and
  are not listed.

## 1. Before FIGT (caller)

| Step | Site | n | Condition | Ids |
|---|---|---|---|---|
| 1.1 | caller-specific (`T*2` garrison refills, `T*` army refills, dungeon encounter `RND(4)`/`RND(10)`/`RND(MonsterPower)`, Gﾏｼﾞｯｸ `RND(48)` loop, …) | — | per caller; specified with the AI / dungeon / story | 01 §2.2-§2.6 |
| 1.2 | `FIGT_B.ADV:186` | 2 | enemy attack, player chose to defend inside the city | R1 |

`FIGT_A` (player attack) draws nothing. "Abandon City" never reaches FIGT (no further draws).

## 2. FIGT setup and strategy

| Step | Site | n | Condition | Ids |
|---|---|---|---|---|
| 2.1 | `FIGT.ADV:17` | 2 | always (also duels and mode 6, where the value is ignored/overridden) | R3 |
| 2.2 | `FIGT作.ADV:9` (player) or `:350` (enemy) | 100 | `Bt_Kind ≠ 3` and the two best strategy values differ; at most one | R4 |

## 3. Round loop (repeat until an exit)

### 3.1 Round start (`FIGT.ADV:105-313`)

| Step | Site | n | Condition | Ids |
|---|---|---|---|---|
| 3.1.1 | `FIGT.ADV:3172` via `:220/226/233/240/246/252/259/266` | troops/10 | `Prov_Cur = 82` (Land of Death); in order P1, P2, P3, P4, E1, E2, E3, E4 (slots 2-4 only if occupied); no draw while troops < 20 | R5 |

The retreat checks for modes 3/4 (`:106-111`) come first and draw nothing.

### 3.2 Step loop — one pass per action (`FIGT.ADV:314-446`)

Before every actor search: if `Goliath_State = 1 and Goliath_Prov = Prov_Cur` →
**Ex** (§4) with no prelude, once (01 §8.8, 02 §3.4). Then the selected actor:

| Actor / command | Draws, in order | Ids |
|---|---|---|
| Player, manual attack or special (codes 1-3, 10-21; also a summoned kaiju) | `RND(10)` at `FIGT.ADV:1587`, repeated while `Unit_Cur = 32` (Miki) rolls 1; then **Ex** | R6 = 02#1, Ex |
| Player, Defend (code 4), standing order 2, or order 1 that falls back to defend | `RND(20)` at `FIGT.ADV:1383` | R7 |
| Player, Wait/Focus (code 5) | `RND(2)` at `FIGT.ADV:1281` only for Miki (32) | R8 |
| Player, standing order 1 that attacks (`L_01fd5`) | **Ex** (no R6) | Ex |
| Player, kaiju summon (30/31), info (53/54), order for another slot (50-52), Fog Card (32), retreat menu, refused retreat, debug Repel | none (kaiju/info/orders re-open the menu; Fog Card ends the action; Repel ends the battle → §5) | — |
| Enemy (`L_02112`) | `RND(2)` `:1421` if player slot 2 occupied; `RND(3)` `:1424` if slot 3; `RND(4)` `:1427` if slot 4; `RND(2)` `:1430` if class 0/10 and the result > 2; `RND(3)` `:1450` always; then defend / idle (no more draws) or **Ex** | R9, R10, R11, R12, R13, Ex |

The round end (`FIGT.ADV:447-482`) draws nothing.

## 4. Ex — one exchange

Entered from `Resolve` / `ResolveMagic` (manual, auto, enemy), from Goliath, and nested from the
ninja follow-up. Rows are 02 §8; procedure specs in 02 §5-§7.

| Step | Site | n | Condition | Ids |
|---|---|---|---|---|
| 4.1 | `FIGT_M.ADV:5` | 2 | always (side taken from `Bt_MsgMode` when 1..4) | 02#2 |
| 4.2 | `FIGT_M.ADV:25` (player side) or `:2825` (enemy side) | 3 | always | 02#3 |
| 4.3 | one branch site of 02 §5.2 / §5.3 | 2, 3, 4, 5 or 10 | only if no earlier guard of the speaker's branch matched; at most one | 02#4 |
| 4.4 | step a: `FIGT.ADV:2458` partner, then `:2178` ×max(1,shots) or spread `:2160`/`:2171` ×1 | 3; 100 / 3 / 3k | enemy army fits the limit and troop→commander shots ≠ 0; partner draw only if PA ≠ 99 and the partner is deployed | 02#5, 02#6 |
| 4.5 | step b: `:2592` then `:2191`/spread — or `:2648` (+`:2653`) then `:2191` | `Unit_Attacks`; `Unit_Attacks/5`, 6 | player army fits the limit; enemy engaged > 9 → first form, < 10 → second | 02#7/8 or 02#7'/8' |
| 4.6 | step c: `:2458` partner, then spread ×1 | 3; 3 / 3k | player engaged ≠ 0; partner draw only if PA ≠ 99 | 02#9, 02#10 |
| 4.7 | step d: `:2178` ×max(1,shots) or spread ×1 | 100 / 3 / 3k | player army fits the limit and shots ≠ 0 | 02#11 |
| 4.8 | step e: `:2475` partner (EA 0/1 only), `:2784`, hits — or `:2832` (+`:2837`, +`:2840` Kayblis at war), hits | 3; `Cmdr_Attacks`; …; 6; 100 | enemy army fits the limit; player engaged > 9 / < 10 | 02#12-14 or 02#13'/14' |
| 4.9 | step f: `:2475` partner, then spread ×1 | 3; 3 / 3k | enemy engaged ≠ 0; partner draw only if EA ≠ 99 | 02#15, 02#16 |
| 4.10 | special override: 1 → step-b' draws again; 2 → `:2592` + hits; 5 → `:2370`; 6 → `:2592`, `:2598`, hits | | `Bt_SpecialAtk` 1/2/5/6 | 02#17 |
| 4.11 | `FIGTATTK.ADV:234` | 2 | always (result unused) | 02#18 |
| 4.12 | `FIGT.ADV:1915` | 3 | the commander tested after the death branches has HP ≠ 0 (after a player death that is the **last** enemy slot's commander, 01 Q-27) | R14 = 02#19 |
| 4.13 | nested Ex: 4.1, 4.2 (unit 51's player branch has no draw), 4.4-4.12 with the stale unit-51 record | | R14 = 1, player unit HP ≠ 0, commander HP > 1, `Bt_MsgMode` ∉ {2, 4}, unit Menad (10) / Hubert (21) / Gandhi (37) / Magic (39) with its ninja flag 0 | 02#20 |

Notes: the side (4.2) and the branch (4.3) depend on `Bt_MsgMode`, which Goliath and the ninja
inherit from the previous exchange (02 Q11 / 01 Q-10). The nested exchange's own 4.12 can never
start another ninja (unit 51). A player-commander death of Rance outside the prologue jumps to
`OVER.ADV` inside 4.12's death branch — no further draws at all.

## 5. Battle end (`AwardExp`, `FIGT.ADV:2977-3160`)

Every exit (annihilation, timeout, retreats, debug Repel) except the game-over jump:

| Step | Site | n | Condition | Ids |
|---|---|---|---|---|
| 5.1 | `FIGT.ADV:2978` | 15 | always | R15 |
| 5.2 | `FIGT.ADV:2981` | 50 | `Bt_ExpBonusNext ≠ 0` | R16 |
| 5.3 | per occupied player slot 1→4, `LevelUp`: `:3057`, then `:3100`, then one of `:3102/3111/3120/3129/3138/3147` | 5; 12+Level; 3 | levels up; `Recovery ≠ 0` for `:3057`; not Mill (25) and not Popularity Staff with troops for `:3100`; result ≤ 6 for the last | R17, R18, R19 |

## 6. After FIGT (wrapper)

| Step | Site | n | Condition | Ids |
|---|---|---|---|---|
| 6.1 | `FIGT_B.ADV:282` | 5 | enemy attack, won, `30 < Prov_Cur < 50` (Log B (31) through Great Wall of Helman A (49)), after 死亡捕獲 | R2 |

`FIGT_A`, the dungeon and the God-dungeon garrisons draw nothing after FIGT in battle scope.

## 7. Seam check result

- **No double counting** once the ids are mapped: 01's R6 is 02#1 (owned by the prelude, only
  for manual commands); 01's R14 is 02#19 and belongs inside Ex; the FIGTATTK draw appears in
  both files at the same position (02#18).
- **Nothing missing**: every `Y 4` site of the battle files is placed above (see §0); the
  presentation code between the steps (`PresentEngagement`, `RedrawBattlefield`, `Highlight`,
  `FIGT_SEL`, defeat scenes) contains no draw.
- The first draft of 01 described FIGT_M's draws only roughly; it now points to 02 §5.5
  (2 or 3 draws per call).
