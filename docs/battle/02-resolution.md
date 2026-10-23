# Battle spec 02 — Per-attack resolution

Scope: everything that happens from the moment one battle slot engages another until the
damage is applied — the exchange (`FIGT.ADV:2137-2857`), action/attack-type setters
(`FIGT.ADV:1492-1623`), special attacks, the engagement presentation (`FIGT.ADV:1624-1863`),
battle flavour text (`FIGT_M.ADV`), the result animation (`FIGTATTK.ADV`) and damage
application (`FIGT.ADV:1873-1986`, `2858-2934`).

Out of scope (owned by the flow spec [01-flow.md](01-flow.md)): battle setup, round/actor loop
(`FIGT.ADV:1-600`, 01 §3-§7), the command menu UI (`FIGT_SEL.ADV`, 01 §8.1-§8.3), the enemy AI's
target/defend choice (`FIGT.ADV:1411-1488`, 01 §8.5), EXP and level-up (`FIGT.ADV:2977+`,
01 §11). They are referenced only where they set inputs of the resolution. The seam is §1 here ↔
[01 §9](01-flow.md#9-the-exchange-call-interface); both files use the procedure names of §1.1.
The merged draw order of a whole battle is in [03-rng-order.md](03-rng-order.md).

## 0. Conventions

- All values are u16 and **every single operation saturates** (`+`/`*` cap at 65535, `-`
  floors at 0, `/0` = 0). Pseudocode keeps the original operand order and grouping;
  `a - b + c` means `(a - b) + c`. Never rewrite an expression algebraically.
- `RND(n)` = `Y 4, n`: if `n <= 1` it returns `n` **without drawing**; otherwise it draws once
  and returns `1..n` (xsystem35 can rarely return `n+1`, see Quirk Q20). Every `RND` below names
  its source line.
- Conditions: `*` = AND, `+` = OR, `\` = ≠ (script notation). In pseudocode I write `and`,
  `or`, `!=`. Operator precedence in the decompiled text is the usual one (`*` before `+`).
- Record fields are bound through cursors: `Unit_*` = record `Unit_Cur`, `Cmdr_*` = record
  `Cmdr_Cur`, `Item_*` = record `Item_Cur`.
- `Tmp0001`/`Tmp0002` are the attack/defence operands of the hit-chance formula; I call them
  `Atk`/`Def` in prose but the port must keep them as **global** variables (see §1.4).
- "Line" = a message window with one or more lines of dialogue; text is never transcribed.
- File without prefix = `FIGT.ADV`.

---

## 1. Call interface (black box for the flow spec)

### 1.1 Entry points

| Entry | Lines | Who jumps here | What it does |
|---|---|---|---|
| `SetAction_*` (11 labels) | 1492-1585 | `FIGT_SEL` result dispatch (`FIGT.ADV:641-769`) | set `Bt_PlayerAction`, `Bt_EnemyAction`, `Bt_SpecialAtk`, `Bt_SkillSfx`, spend Focus; then `ManualAttackPrelude` |
| `ManualAttackPrelude` `L_02528` | 1586-1604 | the setters above | Miki random Vanish Bomb (RND(10)), target highlight, `Bt_MsgMode = 3`, then `Resolve` or `ResolveMagic` |
| `ResolveMagic` `L_025a0` | 1605-1614 | `L_02528`, auto order (1379), enemy AI (1487) | `Bt_MsgMode = 1`, then the body of `Resolve` |
| `Resolve` `L_025d7` | 1615-1623 | `L_02528`, auto order (1367/1373), enemy AI (1472/1477/1482) | `PresentEngagement`; `ComputeExchange`; `ApplyExchange`; retreat flag; redraw; back to actor loop |
| `GoliathStrike` `L_01abd` | 1120-1148 | actor loop (327-329) | builds unit record 51 as T2 Goliath, special 10, then `PresentEngagement`; `ComputeExchange`; `ApplyExchange` (no retreat flag) |
| `NinjaFollowUp` `L_02ff0` | 1972-1986 | jump from inside `ApplyExchange` (1928/1942/1955/1968) | builds unit record 51 as a ninja, special 3, recursive `PresentEngagement`; `ComputeExchange`; `ApplyExchange` |

```
proc Resolve():                        // L_025d7, 1615-1623
  PresentEngagement()                  // \L_02609  (includes FIGT_M flavour text)
  ComputeExchange()                    // \L_035fb
  ApplyExchange()                      // \L_02d45  (includes FIGTATTK, deaths, ninja follow-up)
  if Bt_EnemyRetreatMode == 20 or Bt_EnemyRetreatMode == 50:
      Bt_EnemyRetreat = 3
  RedrawBattlefield()                  // \L_03058 (display only)
  goto ActorLoop                       // @L_007ac (flow spec)

proc ResolveMagic():                   // L_025a0, 1605-1614
  Bt_MsgMode = 1
  <same body as Resolve>
```

### 1.2 Inputs the caller must set

Battle-local, per engagement:

| Variable | Meaning / values |
|---|---|
| `Unit_Cur` | the player unit engaged (attacker or target) |
| `Cmdr_Cur` | the enemy commander engaged |
| `Bt_PlayerActSlot` (VAR0613) | 1-4, slot of `Unit_Cur` (used for slot state, focus, compaction) |
| `Bt_EnemyActSlot` (VAR0614) | 1-4, slot of `Cmdr_Cur` |
| `Bt_PlayerAction` (VAR0620) | 0 direct, 1 indirect (bow), 2 magic, 99 no action (defend/does not counter) |
| `Bt_EnemyAction` (VAR0619) | same codes for the enemy side |
| `Bt_SpecialAtk` (VAR0607) | 0 or special id (§3) |
| `Bt_SkillSfx` (VAR0778) | 0 or special sound set (§9.3); reset to 0 by the resolution |
| `Bt_MsgMode` (VAR0618) | 3 = player physical, 4 = enemy initiative (1 is set by `ResolveMagic`) |

Battle-level state read (set by setup / flow): `Bt_Kind`, `Bt_TroopLimit`, `Bt_WallBonus`,
`Bt_StrategyResult`, `Bt_EnemyRetreatMode`, `Bt_PSlot1..4State`, `Bt_ESlot1..4State`,
`Bt_PlayerSlot1..4`, `Bt_EnemySlot1..4`, `Bt_PSlot1..4Focus`, `Bt_PlayerOrder1..4`,
`Bt_IsDuel`, `Bt_PlayerLosses`, `Bt_EnemyLosses`; display only: `Bt_BackgroundCg`,
`Bt_EnemyParty1`.

World state read: unit record `Unit_Cur` (troops, troop atk/def, the six situational
modifiers, class, atk, def, attacks, HP, magic, item, partner, level, CGs, face),
commander record `Cmdr_Cur` (troops, troop atk/def + mods, class, atk, def, attacks, HP,
magic, CGs, face), item record `Unit_Item` (effect, amount), `StoryStage`, the flavour-text
story flags (§5.4), ninja availability `F_Kanami_NinjaUsed`, `F_Freya_NinjaUsed`,
`F_Kaoru_NinjaUsed`, `F_Wichita_NinjaUsed`, `F_Kayblis_War`, and the **whole record of unit 51**
(§1.4).

### 1.3 Outputs

World state written:

- `Unit_Troops`, `Unit_HP`, `Unit_Status` (0 when HP reaches 0), `Cmdr_Troops`, `Cmdr_HP`,
  `Cmdr_Status` (0 when HP reaches 0).
- `Unit_CurAtkMod`/`Unit_CurDefMod` of `Unit_Cur` (copied from the `Bt_Kind` modifiers, 1727-1738).
- Unit record 51 (overwritten by ninja / Goliath builders), `N_Hubert_FreyaUses` (+1 per Freya
  follow-up).
- Flavour-text side effects (§5.4): `F_Asuka_State`, `F_Satella_LineCmdr91..93`,
  `N_Freak_Stage`, Sieg event (`F_Sieg_State`, `F_Sieg_Defeated`, `N_Miki_Stage`, item 1,
  Rance's `Unit_Item`/CGs, Sieg's (commander 93) CGs/face/item/VAR0126/VAR0127, `Ev_Entry`).
- Death scenes (§7.4): `F_Sill_Died`, `F_Asuka_State`, `EndingId` + game over.

Battle-local written: `Bt_PlayerLosses`, `Bt_EnemyLosses`, `Bt_EnemyRetreatMode` (5→50,
2/3/4→20), `Bt_EnemyRetreat` (=3), slot compaction of `Bt_PlayerSlot*`/`Bt_PSlot*State`/
`Bt_PlayerOrder*`/`Bt_PSlot*Focus` and `Bt_EnemySlot*`/`Bt_ESlot*State`, the 16 exchange
variables `Bt_*Shots`/`Bt_*Dmg` (VAR0232-0247, they stay readable after the call),
`Bt_HitChance`, `Bt_PlayerEngaged`, `Bt_EnemyEngaged`, `Bt_PlayerAllEngaged`,
`Bt_EnemyAllEngaged`, `Bt_PlayerSlotDef`, `Bt_EnemySlotDef`, `Bt_PlayerCmdrShield`,
`Bt_EnemyCmdrShield`, `Item_Cur`, `Bt_MsgMode` (may become 2), `Bt_RoundStep` (clobbered),
`Bt_SkillSfx` (=0), `Bt_SfxAtk1/2`, `Bt_SfxDef1/2`. A ninja follow-up also leaves
`Unit_Cur = 51`, `Bt_SpecialAtk = 3`, `Bt_PlayerAction = 0`, `Bt_EnemyAction = 99`.

Cursors: a death branch of `ApplyExchange` calls `RedrawBattlefield` (`L_03058`), which leaves
`Unit_Cur`/`Cmdr_Cur` on the last occupied player/enemy slot and rewrites
`Unit_CurAtkMod`/`Unit_CurDefMod` of every drawn player unit (`FIGT.ADV:2010-2053`, 2103-2114);
after a player death the rest of `ApplyExchange` runs on those records — see §7.1 and
[01 §9.3 / Q-27](01-flow.md#93-applyexchange-l_02d45-figtadv1873-1971). `Resolve` /
`ResolveMagic` end with another `RedrawBattlefield`, so the caller never sees the exchange's
cursors.

Scratch written: `RetVal`, `CallArg`, `Tmp0001-0005`, `Tmp0229`, `Tmp0621`, `Tmp0690`,
`Ui_*`.

### 1.4 Hidden inputs a port must not drop

1. **Unit record 51** is a temporary slot used by kaiju (`L_01b68`, `L_01c53`), ninjas
   (`L_02ff0`) and Goliath (`L_01abd`). Ninja and Goliath builders set only CGs, face, status,
   troops, max troops, HP and max HP; `Unit_Atk`, `Unit_Def`, `Unit_Attacks`, `Unit_Magic`,
   `Unit_Level`, `Unit_Class`, `Unit_Item`, `Unit_Partner`, the modifiers stay from whatever
   last wrote record 51 (a kaiju, or nothing = 0). `Unit_Attacks` decides whether
   `RND(Unit_Attacks)` draws (§6.5) and how many per-shot draws follow, so it changes the
   RNG sequence. Persist record 51 like any other record.
2. **`Tmp0001`/`Tmp0002`** are not set when the side's action is 99 (the operand branches are
   skipped) but the hit chance is still computed from their stale values. Results are zeroed
   afterwards and the number of draws does not depend on the hit chance (the spread always
   draws exactly once, the per-shot loop draws per shot), so only the **values** of discarded
   draws differ. A port may use any value; keeping them global is the safest.
3. **`Bt_MsgMode`** persists between actions; `GoliathStrike` and `NinjaFollowUp` do not set it,
   so the flavour-text side (and its draws) follows the previous engagement (Q11).

---

## 2. Attack types, setters and who counters

### 2.1 Manual command setters (FIGT_SEL result → label)

`FIGT_SEL.ADV` returns the command in `RetVal` and the clicked button in `Tmp0001`
(4-7 = enemy slot 1-4); `Bt_EnemyActSlot = Tmp0001 - 3` and `Cmdr_Cur` is bound (627-640).

| RetVal | UI command | Label | PA | EA | Special | SkillSfx | Focus spent |
|---|---|---|---|---|---|---|---|
| 1 | Direct | `L_02339` 1492 | 0 | 0 | 0 | 0 | 0 |
| 2 | Bow | `L_0234d` 1497 | 1 | 99 | 0 | 0 | 0 |
| 3 | Magic | `L_02362` 1502 | 2 | 99 | 0 | 0 | 0 |
| 10 | Cheater Beam (debug) | `L_024f8` 1578 | 0 | 99 | 100 | 9 | 0 |
| 11 | Rance Attack | `L_02377` 1507 | 0 | 99 | 1 | 1 | 1 |
| 12 | By la Way | `L_023ab` 1516 | 0 | 99 | 1 | 2 | 1 |
| 13 | White D. Beam | `L_023fa` 1532 | 2 | 99 | 2 | 4 | 2 |
| 14 | High Speed | `L_0243d` 1542 | 0 | 99 | 3 | 5 | 1 |
| 15 | Vanish Bomb | `L_0249c` 1560 | 2 | 99 | 4 | 7 | 3 |
| 16 | C. War Dance | `L_024d9` 1571 | 0 | 99 | 5 | 8 | 1 |
| 17 | Black D. Beam | `L_023fa` (same as 13) | 2 | 99 | 2 | 4 | 2 |
| 18 | Whirlwind Shot | `L_0243d` (same as 14) | 0 | 99 | 3 | 5 | 1 |
| 19 | Chuhai | `L_0246e` 1551 | 2 | 99 | 6 | 6 | 1 |
| 20 | T2 Soaring Wave | `L_023dc` 1525 | 0 | **0** | 7 | 3 | 1 |
| 21 | EPO Light | `L_023fa` (same as 13) | 2 | 99 | 2 | 4 | 2 |

Other results are not attacks (flow spec): 4 Defend `L_02078`, 5 Wait/Focus `L_01e34`,
6 Retreat, 7 Repel (debug win), 8/9 set standing order, 30/31 summon capsule kaiju into
record 51 and re-open the menu, 32 Fog Card, 50-52 order change, 53/54 info.
`Bt_SpecialAtk` is reset to 0 at the start of every player action (600) and enemy action (1412).

Each setter with a name also does `MS 7, "<name>"` (string slot 7 = "Rance Attack", "By la
Way", "White Destruction Beam", "Highspeed", "Chuhai", "Vanish Bomb", "Cheater Beam"; none for
20 and 16). Slot 7 is never printed during the battle (Q19).

```
proc SpendFocus():                     // L_01dd3, 1265-1278
  if Bt_PlayerActSlot == k (k=1..4): Bt_PSlotkFocus = Bt_PSlotkFocus - 1    // floors at 0

proc ManualAttackPrelude():            // L_02528, 1586-1604
  loop:
    r = RND(10)                                             // 1587
    if r == 1 and Unit_Cur == 32:                           // Miki
        // L_0249c, 1560-1570: involuntary Vanish Bomb
        MS 7 = "Vanish Bomb"; SpendFocus(); SpendFocus(); SpendFocus()
        Bt_SpecialAtk = 4; Bt_PlayerAction = 2; Bt_EnemyAction = 99; Bt_SkillSfx = 7
        continue loop                                       // @L_02528: draws again
    break
  HighlightSlot(enemy, Bt_EnemyActSlot)                     // 1591-1594, display
  Bt_MsgMode = 3                                            // 1595
  if Bt_PlayerAction == 0: goto Resolve                     // 1596
  if Bt_PlayerAction == 1: goto Resolve                     // 1599
  if Bt_PlayerAction == 2: goto ResolveMagic                // 1602
  // PA is never anything else here; otherwise execution falls into L_025a0 = ResolveMagic
```

### 2.2 Standing order "keep attacking" (`L_01fd5`, 1357-1381) — for reference

Owned by [01 §8.4](01-flow.md#84-standing-orders-l_01f08-l_01f42-l_01f7c-l_01fd5).

Target is always enemy slot 1 (`Bt_EnemyActSlot = 1`, `Cmdr_Cur = Bt_EnemySlot1`), no
`ManualAttackPrelude` (no RND(10), no special):

| Unit class | Condition | MsgMode | PA | EA | Entry |
|---|---|---|---|---|---|
| 0 or 10 | slot 1-2 | 3 | 0 | 0 | `Resolve` |
| 1 | any slot | 3 | 1 | 99 | `Resolve` |
| 2, 3, 11 | any slot | 1 | 2 | 99 | `ResolveMagic` |
| 0/10 in slot 3-4, class 4, others | — | — | — | — | defends instead (`L_02078`, RND(20)) |

### 2.3 Enemy initiative (`L_02112`, 1411-1488) — for reference

Owned by [01 §8.5](01-flow.md#85-enemy-turn--in-battle-ai-l_02112-figtadv1411-1491).

After its own target/defend draws (flow spec) the AI sets `Bt_MsgMode = 4` and:

| Enemy class | Condition | PA | EA | Entry |
|---|---|---|---|---|
| 0 or 10 | enemy slot 3-4 | — | — | no action |
| 0 or 10 | enemy slot 1-2 (target forced to player slot 1-2) | 0 | 0 | `Resolve` |
| 1 | enemy slot > 2 or target slot > 2 | 99 | 1 | `Resolve` |
| 1 | both front | 0 | 0 | `Resolve` |
| 2 or 11 | any | 99 | 2 | `ResolveMagic` (sets MsgMode 1; FIGT_M turns it into 2) |
| 3, 4, 5, others | — | — | — | no action (falls to `L_0232f`) |

### 2.4 Who counters — summary

- A side whose action is 99 still runs all its computations (and consumes the draws) but its
  shots and damage are zeroed (§6).
- Player **Direct** attack: the enemy counters with action 0 (its troops and commander shoot
  back with troop atk / commander atk). Bow, Magic and every special except T2 Soaring Wave:
  no counter (EA 99). T2 Soaring Wave: EA 0, enemy counters.
- Enemy melee (class 0/10 front row) and front-vs-front archers: the player counters with
  action **0 regardless of the unit's class** (a mage counters physically).
- Enemy archers at range and enemy mages: no counter (PA 99).
- Counters use the same exchange routines with roles mirrored (§6.6-6.9).

### 2.5 Targeting constraints (FIGT_SEL, for reference)

`FIGT_SEL.ADV:164-203`: when the target is enemy slot 3-4 **or** the actor is in player slot
3-4, a command is disabled if it is Direct (1) or a special with id > 10 used by a melee unit
(`Bt_PlayerAtkType = 0`, i.e. class not 1/2/3/11). Bow, Magic, Cheater Beam (10) and
specials of archers/mages can target any slot. A melee unit in slot 3-4 gets no target prompt.

---

## 3. Special attacks (`Bt_SpecialAtk`, VAR0607)

### 3.1 Focus

- Focus charges `Bt_PSlot1..4Focus` are zeroed at battle start (9-12) and shifted with slot
  compaction (§7.5). `Bt_ActFocus` = charges of the acting slot, copied at actor selection.
- **Wait** (`L_01e34`, 1279-1314, flow spec): if `Unit_Cur == 32` (Miki): `r = RND(2)` (1281);
  `r == 1` → "Miki couldn't concentrate", no charge, action ends. Otherwise the acting slot's
  Focus += 1.
- Specials spend Focus through `SpendFocus` (§2.1), saturating at 0.

### 3.2 Availability (FIGT_SEL.ADV `L_00bc0`, 521-698)

Specials are listed only when the current command mode is Direct/Bow/Magic (`Tmp0019 <= 3`)
**and** `Bt_ActFocus > 0` (576). Then:

| UI name | RetVal | Unit (id) | Extra condition | Source |
|---|---|---|---|---|
| Rance Attack | 11 | Rance (1), or Kentarou (31) if `F_Kentarou_State > 7` | actor in slot 1-2 | SEL:579-588 |
| By la Way | 12 | Rick (7) | actor in slot 1-2 | SEL:589-597 |
| High Speed | 14 | Meglass (30) | — | SEL:599 |
| White D. Beam | 13 | Shizuka (17), Magic (39) | — | SEL:608-625 |
| Black D. Beam | 17 | Chizuko (38), Nagi (40) | — | SEL:626-643 |
| Vanish Bomb | 15 | Miki (32) | `Bt_ActFocus > 2` | SEL:644 |
| Whirlwind Shot | 18 | Isoroku (27) | — | SEL:653 |
| C. War Dance | 16 | Patton (18) | — | SEL:662 |
| Chuhai | 19 | Asuka (11) | `F_Asuka_State > 7` | SEL:671 |
| EPO Light | 21 | Gandhi (37) | attack type magic (class 2/3/11) | SEL:680 |
| T2 Soaring Wave | 20 | Rolex (47) | attack type melee (class 0/10/4…) | SEL:689 |
| Cheater Beam | 10 | anyone | `DebugMode = 1` | SEL:241 |

Plus two non-menu sources: **Miki's involuntary Vanish Bomb** (10% per manual attack, repeats,
§2.1) and **automatic** ones (Goliath, ninja follow-up, §3.4). The beam needs only 1 charge to
be listed but spends 2 (saturating) — Q6.

### 3.3 What each special does

The normal exchange is always computed first (all its draws happen); the special then
overrides player-side values (`ComputeExchange` step 9, 2324-2413), before the demon shields
(step 10). "T" = `Cmdr_Troops` (whole enemy troop count, not the engaged part).

| Id | Name(s) | PA/EA | Override (2324-2413) | Extra draws |
|---|---|---|---|---|
| 1 | Rance Attack, By la Way | 0/99 | re-run `CmdrVsCmdr` (`L_0412d`); zero P troop→cmdr, troop→troop, cmdr→troop | those of `L_0412d` |
| 2 | White/Black D. Beam, EPO Light | 2/99 | re-run `CmdrVsTroops` (`L_03f93`) with shots = T (no engaged cap); zero P troop→cmdr, troop→troop, cmdr→cmdr. Gandhi (37) gets +3 magic in it (2584) | those of `L_03f93` |
| 3 | High Speed, Whirlwind Shot, ninja | 0/99 | P cmdr→cmdr = 1 shot / **1 damage**; all other P values 0 | none |
| 4 | Vanish Bomb | 2/99 | P cmdr→cmdr = `Cmdr_HP / 2` shots and damage; P cmdr→troops = T shots and damage; P troop values 0 | none |
| 5 | C. War Dance | 0/99 | P troop values 0; **E troop→cmdr shots = T / 5, damage = T / 100** (Patton is hurt); P cmdr→troops shots = T, damage = `T / 2 + RND(T / 2)`; P cmdr→cmdr 0 | RND(T/2) at 2370 |
| 6 | Chuhai | 2/99 | re-run `L_03f93` with shots = `min(RND(15)*100, T)` (engaged cap applies); zero P troop and cmdr→cmdr | those of `L_03f93` incl. RND(15) |
| 7 | T2 Soaring Wave | 0/**0** | P troop values 0; P cmdr→troops = T/2 shots and damage; P cmdr→cmdr = 2 / 2 | none |
| 10 | T2 Goliath | 0/99 | P troop and cmdr→cmdr 0; P cmdr→troops shots = T, damage = T/2 | none |
| 100 | Cheater Beam | 0/99 | all eight P shots and damage = 10000 | none |

Demon shields still apply afterwards (an unequipped non-demon's special 1/3/4 does 0 HP
damage to a class > 9 commander).

### 3.4 Automatic specials

**T2 Goliath** (`L_01abd`, 1120-1148), checked at every actor-search pass when
`Goliath_State == 1 and Goliath_Prov == Prov_Cur` (327):

```
Goliath_State = 2                      // once per week
Unit_Cur = 51; record 51: name "T2 Goliath", CgFull/Partial/Alone = 0, FaceCg = 0, Status = 1,
           Troops = 0, MaxTroops = 0, HP = 0, MaxHP = 0          // other fields stale
Bt_PlayerActSlot = 1; Bt_EnemyActSlot = 1; Cmdr_Cur = Bt_EnemySlot1
Bt_SpecialAtk = 10; Bt_PlayerAction = 0; Bt_EnemyAction = 99
PresentEngagement(); ComputeExchange(); ApplyExchange()          // no retreat-flag check
RedrawBattlefield(); goto ActorLoop
```

**Ninja follow-up** — see §7.3.

---

## 4. PresentEngagement (`L_02609`, 1624-1829)

Purpose: draw the duel scene, set the active troop modifiers, run the flavour text.

```
proc PresentEngagement():
  ShowEngagement(background = Bt_BackgroundCg,
                 enemySprite = Cmdr_CgFull if Cmdr_Troops >= Bt_TroopLimit
                               else Cmdr_CgPartial if Cmdr_Troops != 0 else Cmdr_CgAlone,
                 playerSprite = (Bt_SpecialAtk == 0) ? same rule with Unit_Cg*
                                                     : SpecialSprite(Unit_Cur),   // 1658-1714
                 overlay = Bt_EnemyParty1 != 1137,                                   // 1715
                 faces = Unit_FaceCg, Cmdr_FaceCg (if != 0))                         // 1719-1724
  if Bt_Kind == 1: Unit_CurAtkMod = Unit_OffAtkMod; Unit_CurDefMod = Unit_OffDefMod  // 1727
  if Bt_Kind == 2: Unit_CurAtkMod = Unit_DefAtkMod; Unit_CurDefMod = Unit_DefDefMod  // 1731
  if Bt_Kind == 3: Unit_CurAtkMod = Unit_DgnAtkMod; Unit_CurDefMod = Unit_DgnDefMod  // 1735
  ShowActionLabels(player: Bt_SpecialAtk != 0 ? "Special Attack" : label(Bt_PlayerAction),
                   enemy:  Bt_SpecialAtk != 0 ? blank            : label(Bt_EnemyAction))
                   // label: 0 Direct Attack, 1 Indirect Attack, 2 Magic Attack, 99 Defense
  if StoryStage == 4: Tmp0229 = Cmdr_Cur; Cmdr_Cur = 200        // 1803-1806
  SlideInWindow(); ShowCounters()                               // L_02c00: troops and HP numbers
  FlavourText()                                                 // %#FIGT_M.ADV, 1812
  if StoryStage == 4: Cmdr_Cur = Tmp0229                        // 1813-1815
  SlideAnimation x4 with ShowCounters()                         // 1816-1828
```

`SpecialSprite` (1660-1713; each test independent, display only): unit Rance (1)→1520, Rick
(7)→1521, Asuka (11)→1121, Shizuka (17)→1522, Magic (39)→1523, Gandhi (37)→1524, Nagi
(40)→1525, Chizuko (38)→1526, Meglass (30)→1527, Isoroku (27)→1528, Kentarou (31)→1222, Miki
(32)→1529, Patton (18)→1530, Rolex (47)→1252; `Unit_CgFull` 1164/1176/1182/1185 (ninjas) → same CG; otherwise
nothing (Goliath).

---

## 5. FlavourText (`FIGT_M.ADV`) — control flow and RNG

FIGT_M only shows dialogue windows, but it consumes **2 or 3 draws** per call and can change
story state. Its windows: `L_0c4c1` (left speaker), `L_0c515` (right speaker),
`L_0c4dd`/`L_0c531` (variants), `L_0c576`/`L_0c569` (wait, close) — all through `WAO_SUB.ADV`,
which never draws.

### 5.1 Entry

```
proc FlavourText():                                        // FIGT_M.ADV
  Tmp0621 = 0                                              // :1
  if Bt_MsgMode == 1 and Bt_EnemyAction == 2:              // :2  enemy-initiated magic
      Bt_MsgMode = 2                                       //     PERSISTS after return
  RetVal = RND(2)                                          // :5  draw always consumed
  if Bt_MsgMode == 1: RetVal = 1                           // :6-17 overrides the draw
  if Bt_MsgMode == 2: RetVal = 2
  if Bt_MsgMode == 3: RetVal = 1
  if Bt_MsgMode == 4: RetVal = 2
  if RetVal == 2: goto EnemySide                           // :21
  // RetVal == 1, or anything else, falls into PlayerSide  (:18-24)
PlayerSide:                                                // L_000b5, :24
  R25 = RND(3)                                             // :25, kept in RetVal; read only by unit 2
  dispatch on Unit_Cur (table 5.2); unmatched → return     // :2823
EnemySide:                                                 // L_036f2, :2824
  RND(3)                                                   // :2825, discarded
  dispatch on Cmdr_Cur (table 5.3); unmatched → return     // :7860
```

So: player-initiated → speaker is the player unit (`Unit_Cur`); enemy-initiated physical (4)
or magic (2) → speaker is the enemy commander. The RND(2) at :5 only matters when
`Bt_MsgMode` is outside 1..4 (never in normal play; possible only with a stale value, Q11) — only
the boot's 0 before a game's first exchange, at a Goliath strike (O6, answered 2026-10-23).

### 5.2 Player side — dispatch on `Unit_Cur`

Notation: guards are tested **in order**; the first true guard shows its line(s) and returns
(no further draw). `C` = `Cmdr_Cur`, `Mode` = `Bt_MsgMode` (1 = player magic, 3 = physical),
`Spec` = `Bt_SpecialAtk`. `RND(n)@L` = the final draw at FIGT_M line L, choosing one of n lines
(arms `Tmp0690 == k`); "fixed" = one unconditional line, no draw; "nothing" = no line.
Every unit not listed shows nothing. Line numbers are FIGT_M.ADV.

```
U1  Rance   (L_0ac75 :8956)
     1. C=93 and F_Sieg_State in {2,3} → Ev_Entry=4; SiegEvent (5.4); return
     2. Spec=1          3. StoryStage=0 and C=31
     4. one guard per C, in this order: 1,2,3,4,11,33,39,45,51,52,55,56,57,62,71,72,75,82,
        90,91,92,93,98,101,104,107,110,117,129,130,131,132,133,134,135,136,137,156,157,158,159,162
     5. Cmdr_Class>9 and Unit_Item=1     6. Unit_Troops=0
     else RND(10)@9536
U2  Sill    (L_0ab18 :8889)  R25=1 and Mode=1 | R25=2 and Mode=1 | else RND(5)@8906
U3  Bound   (L_0aa40 :8844)  Mode=2 (dead on this side) | else RND(3)@8859
U4  Soul    (L_0a952 :8797)  C in {83,84,85} | else RND(3)@8810
U5  Leila   (L_0a6ed :8670)  C=1 | C=2 | C=3 | C=4 | C=55 | C=56 | else RND(5)@8751
U6  Barres  (L_0a496 :8545)  C=1 | C=2 | C=3 | C=4 | C in {5,6,7} | else RND(5)@8610
U7  Rick    (L_097ed :7861)  Spec!=0 | C=1 | C=2 | C=3 | C=4 | C in {5,6,7} | C=54 | C=55 | C=56 | C=57
                             | else RND(4)@8015
U8  Cordoba (L_09b88 :8054)  C=1 | C=2 | C=3 | C=4 | C in {5,6,7} | else RND(4)@8121
U9  Kinkaid (L_0a2fc :8458)  C=1 | C=2 | C=3 | C=4 | else RND(3)@8513
U10 Menad   (L_09dc5 :8176)  C=1 | C=2 | C=3 | C=4 | C in {5,6,7} | else RND(3)@8272
U11 Asuka   (L_0a041 :8314)  Spec!=0
                             | F_Asuka_State=0 and (S1=25 or S2=25 or S3=25 or (S4=25 and F_Mill_State<2))
                                  → F_Asuka_State=1, scene          (S = Bt_PlayerSlot; precedence Q8)
                             | Mode=1 → line (+ an insert for C=1,2,3,4 or {5,6,7})
                             | else RND(3)@8428
U12 :59     C=1 | C=3 | C=4 | C in {5,6,7} | else RND(3)@114
U13 :151    Mode=1 → line (+ insert for C=1,2,3) | else RND(3)@192
U14 :233    RND(3)@234                  U15 :275  RND(3)@276
U16 :317    Mode=1 → line (+ two inserts if C=55) | else RND(3)@346
U17 :381    C=107 → (Bt_IsDuel=1 ? line A : line B) | C=109 | Spec=2 | Mode=1 | else RND(3)@430
U18 :467    Spec!=0 | C=22 | C=32 | C=39 | C=44 | C=50 | else RND(3)@550
U19 :585    C=22 | C=32 | C=39 | else RND(3)@618
U20 :653    C=22 | C=32 | C=39 | C=130 → FreakScene (5.4) | else RND(3)@693
U21 :728    C=22 | C=32 | C=39 | C=44 | C=50 | else RND(3)@795
U22 :844    C=32 | C=39 | C=44 | C=50 | else RND(3)@903
U23 :942    Mode=1 | else RND(3)@951
U24 :988    RND(3)@989    U25 :1030 RND(3)@1031    U26 :1066 RND(3)@1067
U27 :1102   Spec!=0 | Mode=1 | fixed
U28 :1136   C=22 | C=32 | C=33 | C=39 | C=44 | C=50 | else RND(3)@1215
U29 :1254   StoryStage<3 and C=91 and F_Satella_LineCmdr91=0 → set flag, scene
            | StoryStage<3 and C=92 and F_Satella_LineCmdr92=0 → set flag, scene
            | C=72 | C=90 | C=91 | C=92 | C=93 | C=98 | C=129 | C=130 | C=135 | else RND(3)@1375
U30 :1414   StoryStage<3 and C=93 and F_Meglass_LineCmdr93=0 → set flag, scene
            | Spec!=0 | C=135 | fixed
U31 :1441   Spec=1 | C=135 | else RND(3)@1464
U32 :1493   Spec!=0 | C=135 | else RND(3)@1512
U33 :1549   RND(3)@1550   U34 :1581 RND(3)@1582   U35 :1625 RND(3)@1626   U36 :1667 RND(3)@1668
U37 :1701   Spec!=0 | C=101 | C=104 | C in {107,109} | C=110 | C in {113,117} | C=121 | C=125
            | C=168 | Mode=1 | else RND(3)@1855
U38 :1902   Spec!=0 | C=104 | C=110 | C in {113,125} | 95<C<129 → (Mode=1 ? A : B) | Mode=1
            | else RND(3)@2012
U39 :2056   Spec!=0 | C=101 | C=104 | C=107 | C=109 | C in {113,117,121} | 95<C<129 → (Mode=1 ? A : B)
            | Mode=1 | else RND(3)@2180
U40 :2225   Spec!=0 | Mode=1 | else RND(3)@2244
U41 :2269   C in {90,91,92,93,129,133,134} | C=98 | C=135 | else RND(3)@2302
U42 :2337   Mode=1 | fixed
U43 :2361   C=54 | C=55 | C=56 | C=57 | else RND(3)@2418
U44 :2455   fixed
U45 :2465   F_Cafe_JoinMode=2 | C in {90,91,92,93,98,129,130,131,132,133,134} | C=135 | else RND(3)@2502
U46 :2537   Mode=1 | else RND(3)@2548
U47 :2583   Spec!=0 | C=54 | C=55 | C=56 | C=57 | else RND(3)@2638
U48 :2673   if Unit48_Identity=1: fixed; else nothing
U49 :2683   RND(3)@2684   U50 :2719 RND(3)@2720
U51 :2757   Unit_FaceCg=9 | =449 | =72 | =71 (the four ninjas) | else nothing (kaiju, Goliath)
```

### 5.3 Enemy side — dispatch on `Cmdr_Cur`

`U` = `Unit_Cur` (the target), `Mode=2` = enemy magic (else 4 = physical).

Branches with a final draw:

```
C1   :2826  U=1 | U=7 | else RND(3)@2855
C2   :2888  U=1 | U=6 | else RND(3)@2924
C8   :3090  RND(3)@3091
C22  :3266  U=1 | U=6 | U=18 | U=19 | U=20 | U=21 | U=43 | U=47 | else RND(3)@3357
C31  :3482  StoryStage=0 → fixed;  :3500 otherwise U=22 | else RND(3)@3511
C32  :3550  U=22 | U=18 | U=19 | U=20 | U=21 | else RND(3)@3615
C39  :3786  U=1 | U=18 | U=19 | U=20 | U=21 | else RND(3)@3841
C40  :3874  RND(3)@3875          C41 :3900 RND(3)@3901
C44  :3964  U=1 | U=18 | U=28 | U=43 | U=47 | else RND(3)@4025
C45  :4066  RND(3)@4067
C50  :4154  U=1 | U=18 | U=21 | U=22 | U=43 | else RND(3)@4212
C51  :4255  U=1 | else RND(3)@4270
C52  :4307  U=1 | else RND(3)@4326
C54  :4373  U=1 | U=6 | U=7 | U=22 | U=43 | U=47 | else RND(3)@4446
C55  :4487  U=1 | U=22 | U=43 | U=47 | else RND(3)@4542
C56  :4575  Mode=2 | else RND(3)@4584
C57  :4621  Mode=2 | else RND(3)@4632
C58  :4665  StoryStage=0 → RND(2)@4666;  :4690 otherwise fixed
C62  :4724  U=1 | else RND(3)@4735
C83  :5014  RND(3)@5015 (arms test RetVal directly, Q13)
C84  :5046  RND(3)@5047 (same)
C90  :5133  Mode=2 → (Unit_Magic<4 ? A : B) | else RND(3)@5159
C91  :5196  U=1 | U in {29,30,41,42} | U=32 | else RND(3)@5233
C92  :5258  U=1 | U in {29,30} | U=32 | else RND(3)@5291
C93  :5318  U=1 | U=32 | U in {29,30,41,42} | else RND(3)@5357
C101 :5536  U=37 | U=39 | U=40 | Mode=2 | else RND(3)@5616
C104 :5689  U=37 | U=38 | U=39 | U=40 | Mode=2 | else RND(3)@5795
C107 :5870  U=17 | Mode=2 | else RND(3)@5891
C109 :5928  U=1 and Bt_IsDuel=1 | U=17 | U=37 | U=39 | Mode=2 | else RND(3)@6004
C110 :6043  U=37 | U=38 | U=40 | F_Zeth_State=9 → (Mode=2 → line | else RND(3)@6124) | Mode=2 | fixed
C113 :6208  U=37 | U=38 | U=39 | U=40 | Mode=2 | else RND(3)@6269
C117 :6350  U=37 | U=38 | U=39 | U=40 | Mode=2 | else RND(3)@6413
C121 :6480  U=37 | U=38 | U=39 | U=40 | Mode=2 | else RND(3)@6547
C125 :6626  U=37 | U=38 | U=40 | Mode=2 | else RND(3)@6683
C129 :6762  U=1 | U=29 | U=30 | U=41 | else RND(3)@6803
C130 :6836  Mode=2 | else RND(3)@6849
C131 :6884  U in {29,30,42} | U=32 | else RND(3)@6905
C132 :6948  U in {29,30,41,42} | U=32 | else RND(3)@6971
C133 :6996  Mode=2 | else RND(3)@7007
C134 :7036  U=1 | U=29 | U in {30,41} | U=32 | else RND(3)@7081
C135 :7122  U=1 | U in {29,30,41,42} | U=32 | else RND(4)@7163
C156 :7415  RND(3)@7416
C157 :7447  → L_0b958 :9636  U=1 | else RND(3)@9647
```

Branches without a final draw (one line, chosen by the guards shown):

```
C3   :2965  U=1 | U=6 | U=7 | fixed          C4 :3034  Mode=2 | fixed
C33  :3656  U=18 | U=19 | U=20 | U=21 | U=22 | fixed
C100 :5440  Cmdr_CgFull=1414 | 1493 | 1494 | 1495 | 1496 | 1497 | Cmdr_FaceCg=75 | 484 | fixed
Mode=2 | fixed:  C96, C103, C106, C112, C114, C118, C122, C126, C167, C171, C172, C175, C176,
                 C179, C180
fixed (no guard): C5-7, C9-21, C23-30, C34-38, C42, C43, C46-49, C53, C59, C60, C63-82, C85-89,
                 C97, C98, C102, C105, C108, C111, C115, C116, C119, C120, C123, C124, C127,
                 C128, C136-155, C158-166, C168-170, C173, C174, C177, C178, C181-190,
                 191<=C<=199 (one shared line, :7848)
nothing:         C94, C95 (the two "Wall" commanders, class 5), C61, C99, C>=200
                 (incl. the StoryStage-4 substitute 200)
```

### 5.4 State changes in FIGT_M

| Where | Trigger | Effect |
|---|---|---|
| :1 | always | `Tmp0621 = 0` |
| :2-3 | `Bt_MsgMode=1` and `Bt_EnemyAction=2` | `Bt_MsgMode = 2` (persists; disables the ninja roll, §7.3) |
| :8325-8327 | Asuka (U11), see 5.2 | `F_Asuka_State = 1` |
| :9677-9678, :9755-9756, :9826-9827 | Satella (U29) vs Camilla (C91) / Lei (C92), Meglass (U30) vs Sieg (C93), StoryStage<3, flag 0 | `F_Satella_LineCmdr91/92/93 = 1` (one-shot scenes) |
| `L_0bddd` :9900-9902 | Freak (U20) vs Redeye (C130) | if `N_Freak_Stage = 0`: `N_Freak_Stage = 1` + long scene; else short scene |
| `L_0c0fc` :10090-10115 | Rance vs Sieg (C93) with `F_Sieg_State` 2/3 (player side only) | **SiegEvent**: `Ev_Entry=4`; `F_Sieg_State=4`; `F_Sieg_Defeated=1`; if `N_Miki_Stage=1` → 2; `Item_Cur=1`, `Item_Holder=1`; commander 93 renamed "Sieg"/"Maneshitas", CgFull/Partial/Alone = 1325/1326/1327, FaceCg 59, `Cmdr_Item=0`, `VAR0126=0`, `VAR0127=0`; `Unit_Cur=1` (Rance), `Unit_Item=1` (Chaos), `Unit_CgPartial=1358`, `Unit_CgAlone=1361`; long scene |

The Sieg event runs **before** `ComputeExchange`, so the same exchange already uses Chaos
(+10 commander attack, demon-shield break).

### 5.5 Draw count

Every call: RND(2) at :5, then RND(3) at :25 (player side) or :2825 (enemy side), then **at
most one** branch draw (n ∈ {2,3,4,5,10}) exactly as the tables say. No draw happens inside a
guard. Total 2 or 3.

---

## 6. ComputeExchange (`L_035fb`, 2200-2449) and helpers

### 6.1 Helpers

```
proc HitChance():                       // L_0347f, 2137-2149; in Tmp0001 (atk), Tmp0002 (def)
  if Tmp0001 > Tmp0002:
      Bt_HitChance = (Tmp0001 - Tmp0002) * 8
      if Bt_HitChance > 99: Bt_HitChance = 100
      return                            // 16..100 in practice (8 if diff 1)
  Bt_HitChance = 8 + Tmp0001 - Tmp0002  // (8 + atk) - def, floors at 0
  if Bt_HitChance == 0: Bt_HitChance = 1

proc ClampHit():                        // L_034d3, 2150-2157
  if Bt_HitChance > 100: Bt_HitChance = 100
  if Bt_HitChance == 0:  Bt_HitChance = 1

proc Spread():                          // L_03502, 2158-2173; in/out CallArg (expected hits)
  if CallArg < 11:
      r = RND(3)                                            // 2160
      if r == 1: CallArg = CallArg + 1
      if r == 2: CallArg = CallArg - 1                      // floors at 0
      return
  Bt_RoundStep = CallArg / 10                               // 2169 clobbers VAR0623 (Q4)
  CallArg = CallArg - Bt_RoundStep * 2
  r = RND(Bt_RoundStep * 3)                                 // 2171  (n >= 3: always draws)
  CallArg = CallArg + r

proc RollShots():                       // L_0356f 2174-2186 and L_035b5 2187-2199 (identical)
  // in Tmp0001 = number of shots; out CallArg = hits. do-while: draws max(1, Tmp0001) times
  Tmp0004 = 1; CallArg = 0
  loop:
      r = RND(100)                                          // 2178 / 2191
      if r < Bt_HitChance + 1: CallArg = CallArg + 1
      Tmp0004 = Tmp0004 + 1
      if Tmp0004 > Tmp0001: return

proc PartnerPresent():                  // tested at 2457 and 2474
  return (Unit_Partner == Bt_PlayerSlot1 or Unit_Partner == Bt_PlayerSlot2
          or Unit_Partner == Bt_PlayerSlot3 or Unit_Partner == Bt_PlayerSlot4)
         and Unit_Partner != 0

proc AtkMods():                         // L_03c69, 2450-2466 (player troop attack)
  if Unit_CurAtkMod < 10: Tmp0001 = Tmp0001 + Unit_CurAtkMod
  if Unit_CurAtkMod > 10: Tmp0001 = Tmp0001 - (Unit_CurAtkMod - 10)
  if PartnerPresent():
      r = RND(3)                                            // 2458
      if r == 1: Tmp0001 = Tmp0001 + 2
      if r == 2: Tmp0001 = Tmp0001 + 1

proc DefMods():                         // L_03cf0, 2467-2483 (player troop defence)
  same as AtkMods on Tmp0002 with Unit_CurDefMod; partner draw RND(3) at 2475
```

`Hits(shots)` below means: `if shots > 80: CallArg = shots * Bt_HitChance / 100; Spread()`
(saturating multiply first — Q1) `else: Tmp0001 = shots (where noted); RollShots()`. The two
tests are written as `{shots > 80}` then `{shots < 81}`; they are exclusive.

### 6.2 Main

```
proc ComputeExchange():                                     // L_035fb
  Bt_PlayerAllEngaged = 1; Bt_EnemyAllEngaged = 1           // 2201
  Bt_PlayerEngaged = Unit_Troops; Bt_EnemyEngaged = Cmdr_Troops
  Item_Cur = Unit_Item                                      // 2205 (0 → record 0, effect 0; Q17)
  all 16 Bt_[PE]{TroopVsCmdr,TroopVsTroop,CmdrVsTroop,CmdrVsCmdr}{Shots,Dmg} = 0   // 2206-2221
  Bt_PlayerSlotDef = Bt_PSlot<Bt_PlayerActSlot>State        // 2222-2233
  Bt_EnemySlotDef  = Bt_ESlot<Bt_EnemyActSlot>State         // 2234-2245
  if Unit_Troops > Bt_TroopLimit: Bt_PlayerEngaged = Bt_TroopLimit; Bt_PlayerAllEngaged = 0
  if Cmdr_Troops > Bt_TroopLimit: Bt_EnemyEngaged = Bt_TroopLimit; Bt_EnemyAllEngaged = 0

  // ---- player side ----
  a: if Bt_EnemyAllEngaged == 1:                                         // 2254
        Bt_PTroopVsCmdrShots = Bt_PlayerEngaged / Bt_EnemyEngaged * 2
        if Bt_EnemyEngaged == 0: Bt_PTroopVsCmdrShots = Bt_PlayerEngaged
        if Bt_PTroopVsCmdrShots != 0: PTroopsVsCmdr()                    // L_03d77
  b: if Bt_PlayerAllEngaged == 1:                                        // 2263
        if Bt_EnemyEngaged > 9:  PCmdrVsTroops()                         // L_03f93
        if Bt_EnemyEngaged < 10: PCmdrVsCmdr()                           // L_0412d
  c: if Bt_PlayerEngaged != 0: PTroopsVsTroops()                         // L_03e79
  // ---- enemy side ----
  d: if Bt_PlayerAllEngaged == 1:                                        // 2274
        Bt_ETroopVsCmdrShots = Bt_EnemyEngaged / Bt_PlayerEngaged * 2
        if Bt_PlayerEngaged == 0: Bt_ETroopVsCmdrShots = Bt_EnemyEngaged
        if Bt_ETroopVsCmdrShots != 0: ETroopsVsCmdr()                    // L_04267
  e: if Bt_EnemyAllEngaged == 1:                                         // 2283
        if Bt_PlayerEngaged > 9:  ECmdrVsTroops()                        // L_044bf
        if Bt_PlayerEngaged < 10: ECmdrVsCmdr()                          // L_045f7
  f: if Bt_EnemyEngaged != 0: ETroopsVsTroops()                          // L_0438f

  // ---- strategy (troop-vs-troop damage only) ----                     // 2294-2317
  if Bt_StrategyResult == 1:
      Tmp0001 = Bt_ETroopVsTroopDmg / 10; Bt_ETroopVsTroopDmg = Bt_ETroopVsTroopDmg - Tmp0001
      Tmp0001 = Bt_PTroopVsTroopDmg / 10; Bt_PTroopVsTroopDmg = Bt_PTroopVsTroopDmg + Tmp0001
  if Bt_StrategyResult == 2:  same with / 2
  if Bt_StrategyResult == 11: E += E/10;  P -= P/10   (E first, then P)
  if Bt_StrategyResult == 12: E += E/2;   P -= P/2
  // ---- clamps ----                                                     // 2318-2323
  if Bt_ETroopVsTroopDmg + Bt_ECmdrVsTroopDmg > Bt_PlayerEngaged:
      Bt_ETroopVsTroopDmg = Bt_PlayerEngaged - Bt_ECmdrVsTroopDmg
  if Bt_PTroopVsTroopDmg + Bt_PCmdrVsTroopDmg > Bt_EnemyEngaged:
      Bt_PTroopVsTroopDmg = Bt_EnemyEngaged - Bt_PCmdrVsTroopDmg
  // ---- specials ----                                                   // 2324-2413
  if Bt_SpecialAtk == 1:
      PCmdrVsCmdr()
      P TroopVsCmdr, TroopVsTroop, CmdrVsTroop Shots/Dmg = 0
  if Bt_SpecialAtk == 2:
      PCmdrVsTroops()
      P TroopVsCmdr, TroopVsTroop, CmdrVsCmdr Shots/Dmg = 0
  if Bt_SpecialAtk == 3:
      Bt_PCmdrVsCmdrShots = 1; Bt_PCmdrVsCmdrDmg = 1
      P TroopVsCmdr, TroopVsTroop, CmdrVsTroop Shots/Dmg = 0
  if Bt_SpecialAtk == 4:
      P TroopVsCmdr, TroopVsTroop Shots/Dmg = 0
      Bt_PCmdrVsCmdrShots = Cmdr_HP / 2;  Bt_PCmdrVsCmdrDmg = Cmdr_HP / 2
      Bt_PCmdrVsTroopShots = Cmdr_Troops; Bt_PCmdrVsTroopDmg = Cmdr_Troops
  if Bt_SpecialAtk == 5:
      P TroopVsCmdr, TroopVsTroop Shots/Dmg = 0
      Bt_ETroopVsCmdrShots = Cmdr_Troops / 5
      Bt_ETroopVsCmdrDmg   = Cmdr_Troops / 100
      Bt_PCmdrVsTroopShots = Cmdr_Troops
      r = RND(Cmdr_Troops / 2)                                            // 2370
      Bt_PCmdrVsTroopDmg = Cmdr_Troops / 2 + r
      Bt_PCmdrVsCmdrShots = 0; Bt_PCmdrVsCmdrDmg = 0
  if Bt_SpecialAtk == 6:
      PCmdrVsTroops()
      P TroopVsCmdr, TroopVsTroop, CmdrVsCmdr Shots/Dmg = 0
  if Bt_SpecialAtk == 7:
      P TroopVsCmdr, TroopVsTroop Shots/Dmg = 0
      Bt_PCmdrVsTroopShots = Cmdr_Troops / 2; Bt_PCmdrVsTroopDmg = Cmdr_Troops / 2
      Bt_PCmdrVsCmdrShots = 2; Bt_PCmdrVsCmdrDmg = 2
  if Bt_SpecialAtk == 10:
      P TroopVsCmdr, TroopVsTroop, CmdrVsCmdr Shots/Dmg = 0
      Bt_PCmdrVsTroopShots = Cmdr_Troops; Bt_PCmdrVsTroopDmg = Cmdr_Troops / 2
  if Bt_SpecialAtk == 100:
      all eight Bt_P*Shots and Bt_P*Dmg = 10000
  // ---- demon shields ----                                               // 2414-2448
  Bt_PlayerCmdrShield = 0; Bt_EnemyCmdrShield = 0
  if Unit_Class > 9:
      Bt_PlayerCmdrShield = 1
      if Cmdr_Class > 9: Bt_PlayerCmdrShield = 2
  if Cmdr_Cur == 146 or Cmdr_Cur == 147: Bt_PlayerCmdrShield = 0
  if Cmdr_Cur in {187,188,189,190}:       Bt_PlayerCmdrShield = 0
  if Cmdr_Class > 9:
      Bt_EnemyCmdrShield = 1
      if Unit_Class > 9: Bt_EnemyCmdrShield = 2
      if Unit_Item == 1 or Unit_Item == 2: Bt_EnemyCmdrShield = 2       // Chaos, Nikkou
  if Bt_PlayerCmdrShield != 0:
      Bt_ETroopVsCmdrDmg = 0
      if Bt_PlayerCmdrShield != 2: Bt_ECmdrVsCmdrDmg = 0
  if Bt_EnemyCmdrShield != 0:
      Bt_PTroopVsCmdrDmg = 0
      if Bt_EnemyCmdrShield != 2: Bt_PCmdrVsCmdrDmg = 0
```

Meaning of the shields: a demon commander (class 10/11) takes **no HP damage from troops**
ever, and HP damage from a commander only if that commander is a demon or (for enemy demons)
wields Chaos/Nikkou. Player demons lose the protection against enemy commanders Angel Knight (146),
Angel Knight (147) and Kaiju Gaga (187) through Kaiju Bobo (190). Shields zero only damage; shots stay (for display).

Engagement summary (from a–f): troops shoot at the enemy commander only when the enemy's
troops all fit the battle limit; `shots at commander = (own engaged / enemy engaged) * 2`
(integer division first), or all engaged troops when the enemy commander is alone; the rest
shoot at troops. A commander fights in person only when **its own** troops all fit the
limit: against troops if the opposing engaged troops are ≥ 10, else against the commander.

### 6.3 PTroopsVsCmdr (`L_03d77`, 2484-2523) — player troops → enemy commander HP

```
if PA == 0 or PA == 1:  Tmp0001 = Unit_TroopAtk; if Item_Effect == 3: Tmp0001 = Tmp0001 + Item_Amount
                        AtkMods(); Tmp0002 = Cmdr_Def
if PA == 2:             Tmp0001 = Unit_Magic; AtkMods(); Tmp0002 = Cmdr_Magic
HitChance()
Bt_HitChance = Bt_HitChance + Unit_Level - Bt_EnemySlotDef                       // 2507
ClampHit()
s = Bt_PTroopVsCmdrShots
if s > 80: CallArg = s * Bt_HitChance / 100; Spread(); Bt_PTroopVsCmdrDmg = CallArg
if s < 81: Tmp0001 = s; RollShots(); Bt_PTroopVsCmdrDmg = CallArg
if PA == 99: Bt_PTroopVsCmdrShots = 0; Bt_PTroopVsCmdrDmg = 0
```

### 6.4 PTroopsVsTroops (`L_03e79`, 2524-2563)

```
if PA == 0 or PA == 1:  Tmp0001 = Unit_TroopAtk (+Item_Amount if Item_Effect == 3); AtkMods()
                        Tmp0002 = Cmdr_TroopDef + Cmdr_TroopDefMod          // raw add, Q9
if PA == 2:             Tmp0001 = Unit_Magic; AtkMods()
                        Tmp0002 = Cmdr_Magic + Cmdr_TroopDefMod
HitChance()
Bt_HitChance = Bt_HitChance + Unit_Level - Bt_EnemySlotDef
if Bt_Kind == 1: Bt_HitChance = Bt_HitChance - Bt_WallBonus * 2               // 2548 attacking a city
ClampHit()
Bt_PTroopVsTroopShots = Bt_PlayerEngaged - Bt_PTroopVsCmdrShots
CallArg = Bt_PTroopVsTroopShots * Bt_HitChance / 100                          // always the spread path
Spread()
Bt_PTroopVsTroopDmg = CallArg
if Bt_PTroopVsTroopDmg + Bt_PCmdrVsTroopDmg > Bt_EnemyEngaged:
    Bt_PTroopVsTroopDmg = Bt_EnemyEngaged - Bt_PCmdrVsTroopDmg
if PA == 99: Bt_PTroopVsTroopShots = 0; Bt_PTroopVsTroopDmg = 0
```

### 6.5 PCmdrVsTroops (`L_03f93`, 2564-2622)

```
if PA == 0 or PA == 1:  Tmp0001 = Unit_Atk (+Item_Amount if Item_Effect == 1)
                        Tmp0002 = Cmdr_TroopDef + Cmdr_TroopDefMod
if PA == 2:             Tmp0001 = Unit_Magic (+Item_Amount if Item_Effect == 5)
                        if Bt_SpecialAtk == 2 and Unit_Cur == 37: Tmp0001 = Tmp0001 + 3   // EPO Light
                        Tmp0002 = Cmdr_Magic
HitChance(); Bt_HitChance = Bt_HitChance + Unit_Level - Bt_EnemySlotDef; ClampHit()
r = RND(Unit_Attacks)                                                        // 2592
Tmp0001 = r + Unit_Attacks / 2
if Bt_SpecialAtk == 2: Tmp0001 = Cmdr_Troops
if Bt_SpecialAtk == 6:
    r = RND(15)                                                              // 2598
    r = r * 100; if Cmdr_Troops < r: r = Cmdr_Troops
    Tmp0001 = r
Bt_PCmdrVsTroopShots = Tmp0001
Hits(Bt_PCmdrVsTroopShots) → Bt_PCmdrVsTroopDmg          // <=80 uses RollShots with Tmp0001 = shots
if Bt_PCmdrVsTroopDmg > Bt_EnemyEngaged and Bt_SpecialAtk != 2:
    Bt_PCmdrVsTroopDmg = Bt_EnemyEngaged
if PA == 99: Bt_PCmdrVsTroopShots = 0; Bt_PCmdrVsTroopDmg = 0
```

### 6.6 PCmdrVsCmdr (`L_0412d`, 2623-2670)

```
if PA == 0 or PA == 1:  Tmp0001 = Unit_Atk (+Item_Amount if Item_Effect == 1); Tmp0002 = Cmdr_Def
if PA == 2:             Tmp0001 = Unit_Magic (+Item_Amount if Item_Effect == 5); Tmp0002 = Cmdr_Magic
HitChance(); Bt_HitChance = Bt_HitChance + Unit_Level - Bt_EnemySlotDef; ClampHit()
r = RND(Unit_Attacks / 5)                                                    // 2648
if r == 0: r = 1
if r > 5: r = RND(6)                                                         // 2653
Tmp0001 = r; Bt_PCmdrVsCmdrShots = r
Hits(Bt_PCmdrVsCmdrShots) → Bt_PCmdrVsCmdrDmg            // always <= 6 shots → RollShots
if PA == 99: Bt_PCmdrVsCmdrShots = 0; Bt_PCmdrVsCmdrDmg = 0
```

### 6.7 ETroopsVsCmdr (`L_04267`, 2671-2713) — enemy troops → player commander HP

```
if EA == 0 or EA == 1:  Tmp0001 = Cmdr_TroopAtk + Cmdr_TroopAtkMod                  // raw add
                        Tmp0002 = Unit_Def (+Item_Amount if Item_Effect == 2)
if EA == 2:             Tmp0001 = Cmdr_Magic + Cmdr_TroopAtkMod
                        Tmp0002 = Unit_Magic (+Item_Amount if Item_Effect == 5)
                                             (+Item_Amount if Item_Effect == 8)     // Q10
HitChance()
Bt_HitChance = Bt_HitChance - Unit_Level - Bt_PlayerSlotDef                          // 2697
ClampHit()
Hits(Bt_ETroopVsCmdrShots) → Bt_ETroopVsCmdrDmg          // <=80: Tmp0001 = shots; RollShots
if EA == 99: Bt_ETroopVsCmdrShots = 0; Bt_ETroopVsCmdrDmg = 0
```

### 6.8 ETroopsVsTroops (`L_0438f`, 2714-2756)

```
if EA == 0 or EA == 1:  Tmp0001 = Cmdr_TroopAtk + Cmdr_TroopAtkMod
                        Tmp0002 = Unit_TroopDef (+Item_Amount if Item_Effect == 4); DefMods()
if EA == 2:             Tmp0001 = Cmdr_Magic + Cmdr_TroopAtkMod
                        Tmp0002 = Unit_Magic (+Item_Amount if Item_Effect == 8); DefMods()
HitChance()
Bt_HitChance = Bt_HitChance - Unit_Level - Bt_PlayerSlotDef
if Bt_Kind == 2: Bt_HitChance = Bt_HitChance - Bt_WallBonus * 2              // 2741 enemy attacks our city
ClampHit()
Bt_ETroopVsTroopShots = Bt_EnemyEngaged - Bt_ETroopVsCmdrShots
CallArg = Bt_ETroopVsTroopShots * Bt_HitChance / 100; Spread()
Bt_ETroopVsTroopDmg = CallArg
if Bt_ETroopVsTroopDmg + Bt_ECmdrVsTroopDmg > Bt_PlayerEngaged:
    Bt_ETroopVsTroopDmg = Bt_PlayerEngaged - Bt_ECmdrVsTroopDmg
if EA == 99: Bt_ETroopVsTroopShots = 0; Bt_ETroopVsTroopDmg = 0
```

### 6.9 ECmdrVsTroops (`L_044bf`, 2757-2803) and ECmdrVsCmdr (`L_045f7`, 2804-2857)

```
ECmdrVsTroops:
  if EA == 0 or EA == 1:  Tmp0001 = Cmdr_Atk
                          Tmp0002 = Unit_TroopDef (+Item_Amount if Item_Effect == 4); DefMods()
  if EA == 2:             Tmp0001 = Cmdr_Magic
                          Tmp0002 = Unit_Magic (+Item_Amount if Item_Effect == 8)   // no DefMods
  HitChance(); Bt_HitChance = Bt_HitChance - Unit_Level - Bt_PlayerSlotDef; ClampHit()
  r = RND(Cmdr_Attacks)                                                       // 2784
  Tmp0001 = r + Cmdr_Attacks / 2; Bt_ECmdrVsTroopShots = Tmp0001
  Hits(Bt_ECmdrVsTroopShots) → Bt_ECmdrVsTroopDmg
  if Bt_ECmdrVsTroopDmg > Bt_PlayerEngaged: Bt_ECmdrVsTroopDmg = Bt_PlayerEngaged
  if EA == 99: Bt_ECmdrVsTroopShots = 0; Bt_ECmdrVsTroopDmg = 0

ECmdrVsCmdr:
  if EA == 0 or EA == 1:  Tmp0001 = Cmdr_Atk; Tmp0002 = Unit_Def (+Item_Amount if Item_Effect == 2)
  if EA == 2:             Tmp0001 = Cmdr_Magic
                          Tmp0002 = Unit_Magic (+Item_Amount if Item_Effect == 5)
                                               (+Item_Amount if Item_Effect == 8)
  HitChance(); Bt_HitChance = Bt_HitChance - Unit_Level - Bt_PlayerSlotDef; ClampHit()
  r = RND(Cmdr_Attacks / 5)                                                   // 2832
  if r == 0: r = 1
  if r > 5: r = RND(6)                                                        // 2837
  if Cmdr_Cur == 135 and F_Kayblis_War == 2: r = RND(100)                     // 2840 Kayblis
  Tmp0001 = r; Bt_ECmdrVsCmdrShots = r
  Hits(Bt_ECmdrVsCmdrShots) → Bt_ECmdrVsCmdrDmg       // Kayblis can exceed 80 → Spread
  if EA == 99: Bt_ECmdrVsCmdrShots = 0; Bt_ECmdrVsCmdrDmg = 0
```

### 6.10 Hit-chance modifiers in one place

| Term | Player attacks | Enemy attacks |
|---|---|---|
| base | `HitChance(atk, def)` | same |
| level | `+ Unit_Level` | `- Unit_Level` |
| slot state of the *defender* | `- Bt_EnemySlotDef` (0 not acted, 1 acted, 20 AI defending) | `- Bt_PlayerSlotDef` (0, 1, 2-21 defending) |
| walls | troops vs troops only, `- Bt_WallBonus*2` if `Bt_Kind == 1` | troops vs troops only, if `Bt_Kind == 2` |
| clamp | 1..100 | 1..100 |
| troop modifier | `AtkMods` on troop attacks (6.3, 6.4) | `DefMods` on player troop defence (6.8, 6.9 phys) |
| partner | RND(3): +2/+1/0 inside AtkMods/DefMods | — |
| enemy troop mods | `+ Cmdr_TroopDefMod` on enemy troop defence (6.4, 6.5 phys) | `+ Cmdr_TroopAtkMod` on enemy troop attacks (6.7, 6.8) |

Item effects: 1 commander atk (6.5/6.6 phys), 2 commander def (6.7/6.9 phys), 3 troop atk
(6.3/6.4 phys), 4 troop def (6.8/6.9 phys), 5 magic atk (6.5/6.6 magic) **and** magic def
against magic hitting the commander (6.7, 6.9b), 8 magic def (6.7, 6.8, 6.9).

---

## 7. ApplyExchange (`L_02d45`, 1873-1971)

### 7.1 Algorithm

```
proc ApplyExchange():
  PlayExchangeAnimation()                          // %#FIGTATTK.ADV (7.2), contains RND(2)
  Bt_PlayerLosses = Bt_PlayerLosses + Bt_ETroopVsTroopDmg                     // 1875
  Bt_PlayerLosses = Bt_PlayerLosses + Bt_ECmdrVsTroopDmg
  Unit_HP     = Unit_HP - Bt_ETroopVsCmdrDmg
  Unit_Troops = Unit_Troops - Bt_ETroopVsTroopDmg
  Unit_Troops = Unit_Troops - Bt_ECmdrVsTroopDmg
  Unit_HP     = Unit_HP - Bt_ECmdrVsCmdrDmg
  Bt_EnemyLosses = Bt_EnemyLosses + Bt_PTroopVsTroopDmg                       // 1881
  Bt_EnemyLosses = Bt_EnemyLosses + Bt_PCmdrVsTroopDmg
  Cmdr_HP = Cmdr_HP - Bt_PTroopVsCmdrDmg
  if Bt_PTroopVsCmdrDmg != 0 and Bt_EnemyRetreatMode == 5: Bt_EnemyRetreatMode = 50
  Cmdr_Troops = Cmdr_Troops - Bt_PTroopVsTroopDmg
  Cmdr_Troops = Cmdr_Troops - Bt_PCmdrVsTroopDmg
  Cmdr_HP = Cmdr_HP - Bt_PCmdrVsCmdrDmg
  if Bt_PCmdrVsCmdrDmg != 0 and Bt_EnemyRetreatMode == 5: Bt_EnemyRetreatMode = 50
  if Unit_HP == 0 and Unit_Cur != 51:                                         // 1893
      Unit_Status = 0
      Bt_PlayerLosses = Bt_PlayerLosses + 1
      PlayerUnitDefeatedScene()                    // %#FIGTﾐｶDE.ADV (7.4)
      RedrawBattlefield(); Wait(10)                // !! moves Unit_Cur / Cmdr_Cur to the last
                                                   //    occupied player / enemy slot (before compaction)
      CompactPlayerSlots(Bt_PlayerActSlot)         // L_04762 (7.5)
  if Cmdr_HP == 0:                                 // 1902 — after a player death: the LAST enemy slot's record
      if Bt_EnemyRetreatMode in {2,3,4}: Bt_EnemyRetreatMode = 20
      Cmdr_Status = 0
      Bt_EnemyLosses = Bt_EnemyLosses + 1
      EnemyCommanderDefeatedScene()                // %#FIGTﾃｷDE.ADV (7.4)
      RedrawBattlefield(); Wait(10)
      CompactEnemySlots(Bt_EnemyActSlot)           // L_04864
      return                                       // no ninja roll
  r = RND(3)                                                                  // 1915
  if Unit_HP != 0 and Cmdr_HP > 1 and r == 1 and Bt_MsgMode != 4 and Bt_MsgMode != 2:
      if Unit_Cur == 10 and F_Kanami_NinjaUsed == 0:  NinjaFollowUp(Kanami)     // Menad
      if Unit_Cur == 21 and F_Freya_NinjaUsed == 0:
          N_Hubert_FreyaUses = N_Hubert_FreyaUses + 1; NinjaFollowUp(Freya)     // Hubert
      if Unit_Cur == 37 and F_Kaoru_NinjaUsed == 0:   NinjaFollowUp(Kaoru)      // Gandhi
      if Unit_Cur == 39 and F_Wichita_NinjaUsed == 0: NinjaFollowUp(Wichita)    // Magic
  return
```

(The four tests are sequential but each matching one jumps away and never returns here.)

**Cursor hazard** (flow spec [01 §9.3, Q-27](01-flow.md#93-applyexchange-l_02d45-figtadv1873-1971)):
after a player death the `Cmdr_HP == 0` test, the RND(3) condition and the ninja tests use the
records left by `RedrawBattlefield`. An enemy commander that also reached HP 0 but is not in
the last occupied enemy slot is not processed now (it keeps `Cmdr_Status ≠ 0`, stays in its
slot and is processed at the end of the next exchange it takes part in); the ninja may follow
up for the unit in the last player slot against the last enemy. Port exactly.

### 7.2 PlayExchangeAnimation (`FIGTATTK.ADV`)

Display, plus one state-relevant part:

```
ShowCounters(troops/HP of both sides)                              // :1-31
SelectSfx()                                                        // L_00796 :256-345, 9.3; sets Bt_SkillSfx = 0
for step in 0, 40, ..., 360: animate the eight shot counters (only non-zero ones)   // :33-131
Wait(40)
show predicted troops/HP: X - dmg1 - dmg2 (saturating) and the eight damage numbers  // :134-232
RND(2)                                                             // :234 — result never used
show "wiped out" marks (CG 889 player troops, 890 enemy troops, 891 player commander,
     892 enemy commander) when the value was non-zero and the prediction is 0         // :235-253
```

The FIGTATTK `Tmp0002`/`Tmp0003`/`Tmp0004`/`Tmp0005` loop counters end at 360/…/408/… (only
relevant as stale `Tmp0002` for a following PA/EA-99 hit chance, §1.4).

### 7.3 NinjaFollowUp (`L_02ff0`, 1917-1986)

```
Unit_Cur = 51
record 51: name/troop-type/extra strings = ninja's; CgFull = CgPartial = CgAlone =
           1164 Kanami | 1176 Freya | 1182 Kaoru | 1185 Wichita;
           FaceCg = 9 | 449 | 72 | 71; Status = 1
           Troops = 0; MaxTroops = 0; HP = 3; MaxHP = 3       // other fields stale (1.4)
Bt_SpecialAtk = 3; Bt_PlayerAction = 0; Bt_EnemyAction = 99
PresentEngagement()          // FIGT_M U51 → the ninja's line; 2 draws (no branch draw)
ComputeExchange()
ApplyExchange()              // recursive; Unit_Cur = 51 → no death handling, no new ninja,
                             // but FIGTATTK RND(2) and (if commander alive) RND(3)@1915 happen
return                       // to the caller of the outer ApplyExchange
```

Effect: exactly 1 HP to the enemy commander (0 against a demon commander), which never kills
because the roll requires `Cmdr_HP > 1`. The availability flags are only read, never set here
(the ninja can follow up every attack). Ninjas follow only player-initiated engagements.

### 7.4 Defeat scenes (black boxes, no draws)

- `FIGTﾐｶDE.ADV` (player unit `Unit_Cur` fell): dialogue keyed by `Unit_Cur`/`StoryStage`;
  state: unit 2 (Sill, StoryStage ≠ 0) `F_Sill_Died = 1`; Asuka (unit 11) `F_Asuka_State = 0`;
  unit 1 (Rance) when `StoryStage ≠ 0`: `EndingId = 1` and jump to `OVER.ADV` (game over,
  never returns). In StoryStage 0 Rance's defeat only shows text.
- `FIGTﾃｷDE.ADV` (enemy commander `Cmdr_Cur` fell): dialogue only, no state.

### 7.5 Slot compaction (`L_04762` 2858-2904, `L_04864` 2905-2934)

```
CompactPlayerSlots(k):   for i = k .. 3: PlayerSlot[i]  = PlayerSlot[i+1]; PSlotState[i] = PSlotState[i+1]
                                         PlayerOrder[i] = PlayerOrder[i+1]; PSlotFocus[i] = PSlotFocus[i+1]
                         PlayerSlot4 = 0; PSlot4State = 0; PSlot4Focus = 0
                         PlayerOrder4 = 0 only when k == 4                       // Q15
CompactEnemySlots(k):    for i = k .. 3: EnemySlot[i] = EnemySlot[i+1]; ESlotState[i] = ESlotState[i+1]
                         EnemySlot4 = 0; ESlot4State = 0
```

(Assignments happen in ascending slot order, as written; the vacated slot 4 gets state 0,
not 100 — flow spec.)

---

## 8. RNG draws in order

One engagement consumes, in this order (✓ = always). Row 1 belongs to `ManualAttackPrelude`
(manual commands only; it is flow-spec draw **R6**); rows 2-20 are one `Resolve` /
`ResolveMagic` / Goliath / ninja exchange (flow-spec "Ex"; row 19 is flow-spec **R14**). The
whole-battle walkthrough is [03-rng-order.md](03-rng-order.md).

| # | Site | n | Condition | Use |
|---|---|---|---|---|
| 1 | 1587 | 10 | manual player command (via FIGT_SEL), **not** auto orders, enemy, Goliath or ninja; repeats while it returns 1 and `Unit_Cur = 32` (Miki) | Miki involuntary Vanish Bomb |
| 2 | FIGT_M:5 | 2 | ✓ | discarded (side choice only if MsgMode ∉ 1..4) |
| 3 | FIGT_M:25 / :2825 | 3 | ✓ (one of them) | player side: Sill's guards; enemy side: discarded |
| 4 | FIGT_M branch | 2,3,4,5,10 | per §5.2/5.3 | flavour line |
| 5 | 2458 | 3 | step a runs, PA ∈ {0,1,2}, partner present | troop atk +2/+1/0 |
| 6 | 2178 ×max(1,s) or 2160/2171 ×1 | 100 / 3 / 3k | step a runs (s = troop→cmdr shots) | hits |
| 7 | 2592 | `Unit_Attacks` | step b, EE > 9, `Unit_Attacks ≥ 2` | shots |
| 8 | 2191 ×max(1,s) or spread ×1 | | step b, EE > 9 | hits |
| 7' | 2648; 2653 | `Unit_Attacks/5` (≥2); 6 if result > 5 | step b, EE < 10 | shots |
| 8' | 2191 ×s | 100 | step b, EE < 10 | hits |
| 9 | 2458 | 3 | step c runs, PA ∈ {0,1,2}, partner | troop atk |
| 10 | 2160 or 2171 | 3 / 3k | step c runs (PE ≠ 0) — exactly 1 | troop kills spread |
| 11 | 2178 ×max(1,s) or spread ×1 | | step d runs | hits on player commander |
| 12 | 2475 | 3 | step e, PE > 9, EA ∈ {0,1}, partner | troop def |
| 13 | 2784 | `Cmdr_Attacks` | step e, PE > 9, ≥ 2 | shots |
| 14 | 2191 ×max(1,s) or spread ×1 | | step e, PE > 9 | hits |
| 13' | 2832; 2837; 2840 | `Cmdr_Attacks/5` (≥2); 6 if > 5; 100 if Kayblis at war | step e, PE < 10 | shots |
| 14' | 2191 ×s or spread ×1 | | step e, PE < 10 | hits |
| 15 | 2475 | 3 | step f runs (EE ≠ 0), EA ∈ {0,1,2}, partner | troop def |
| 16 | 2160 or 2171 | | step f runs — exactly 1 | troop kills spread |
| 17 | special 1: draws 7'/8' again; 2: 7/8; 5: 2370 RND(T/2) if T/2 ≥ 2; 6: 7, 2598 RND(15), 8 | | special | |
| 18 | FIGTATTK:234 | 2 | ✓ | discarded |
| 19 | 1915 | 3 | enemy commander still alive | 1 → ninja (if other conditions) |
| 20 | ninja: rows 2-4 (U51: 2 draws), 5-17, 18, 19 again | | ninja triggered | |

Notes:

- "spread" always draws exactly once (n is 3 or `3*(CallArg/10)` ≥ 3); `RollShots` draws
  `max(1, shots)` times (do-while) — even for 0 shots, e.g. `Unit_Attacks = 0` (Q3).
- Steps run in the order a, b, c, d, e, f regardless of who initiated; a side with action 99
  still draws.
- Upstream draws in the same action, owned by the flow spec (01 §12), for orientation: enemy AI
  target draws and defend draw (1421-1450, R9-R13; 1418 is `Y 3`, a pause), Defend RND(20)
  (1383, R7), Miki Wait RND(2) (1281, R8).
- Defeat scenes, `WAO_SUB`, `PCMPLAY`, `M.ADV` contain no draws.

---

## 9. Presentation events

### 9.1 Event sequence of one `Resolve`

1. (manual) `HighlightTarget(enemySlot)`.
2. `ShowEngagement(...)` with sprites/faces (§4), `ShowActionLabels(...)`.
3. `SlideIn`, `ShowCounters(playerTroops, enemyTroops, playerHP, enemyHP)`.
4. `ShowFlavourLines(speaker side, selection per §5)` — zero, one or several windows.
5. `SlideOut` ×4 with counters.
6. `PlaySfx(atk1, def1)`; `AnimateShots(eight counters)`; `PlaySfx(def2, atk2)` if either ≠ 5.
7. `ShowResults(predicted counters, eight damage numbers, wiped-out marks)`.
8. On defeat: `ShowDefeatScene(unit or commander)`, redraw, short wait, slots shift.
9. Optional ninja: steps 2-8 again with the ninja.
10. Redraw battlefield.

### 9.2 Inputs

The resolution itself has no player choices; the flavour windows and the result screen wait
for a click (`WAO_SUB` op 11). The defeat of Rance outside StoryStage 0 ends the game.

### 9.3 Sound selection (`FIGTATTK.ADV` `L_00796`)

| Condition (applied in this order) | Atk1, Atk2 | Def1, Def2 |
|---|---|---|
| default | 5, 5 | 5, 5 |
| PA 0 / 1 / 2 / 99 | 6,7 / 10,11 (Maria, unit 16: 20,21) / 14,19 / 5,5 | |
| `Unit_Cur = 51` | 10, 11 | |
| EA 0 / 1 / 2 / 99 | | 8,9 / 12,13 / 14,19 / 5,5 |
| SkillSfx 1 / 2 / 3 / 4 / 5 / 6 / 7 / 8 / 9 | 31,32 / 33,34 / 39,40 / 35,36 / 41,42 / 43,44 / 39,40 / 43,44 / 35,36 | |

Then `Bt_SkillSfx = 0`.

---

## 10. Quirks a faithful port must reproduce

- **Q1 Saturating damage cap.** `shots * Bt_HitChance / 100` saturates at 65535 before the
  division, so expected hits never exceed 655 (e.g. 1000 engaged troops at 70% → 655, not
  700). With the spread a troop-vs-troop exchange kills at most 720 before the strategy bonus. (2510, 2553, 2607, 2658,
  2700, 2746, 2788, 2845)
- **Q2 Hit chance plateau.** atk = def and atk = def + 1 both give 8%; atk = def + 2 jumps to
  16% (2138-2148).
- **Q3 Phantom shots.** `RollShots` is a do-while: 0 shots still roll once and can hit once
  (`Unit_Attacks = 0`, e.g. unit 48 or a stale record 51; special 2/6 with no enemy troops).
  Troop-vs-troop always uses `Spread`, so 0 shots can still kill 1 (later clamps usually undo
  it).
- **Q4 `Bt_RoundStep` clobbered** by `Spread` (2169). Harmless because every path returns to
  `L_007ac`, which resets it — keep the write anyway.
- **Q5 Spread is biased.** Large `E`: result ∈ `[E - 2k + 1, E + k]`, `k = E/10`
  (mean ≈ E − k/2). Small `E` (< 11): E ± 1.
- **Q6 Beam cost.** White/Black D. Beam and EPO Light spend 2 Focus but are offered with 1
  (saturating to 0). Vanish Bomb checks `> 2` and spends 3.
- **Q7 Miki loop.** Any manual attack by Miki has a 1/10 chance (repeatable) to turn into
  Vanish Bomb, spending 3 Focus each time even at 0 (1587-1589).
- **Q8 Asuka/Mill precedence.** `S1=25 or S2=25 or S3=25 or (S4=25 and F_Mill_State<2)`
  (FIGT_M:8326) — the Mill-state test applies to slot 4 only.
- **Q9 Enemy troop modifiers are added raw** (`Cmdr_TroopAtkMod`, `Cmdr_TroopDefMod`), unlike
  player modifiers which decode 0-9 = +n, 11-19 = −(n−10), 10 = no change. Values come from
  data (0-2) or from a defecting unit's `Unit_DefAtkMod`/`Unit_DefDefMod`
  (`Gｻﾃﾗ.ADV:3411`), where 11/12 become +11/+12.
- **Q10 Magic-attack item defends.** Item effect 5 adds to the player commander's magic
  defence against enemy magic (2689, 2822) but not against enemy troop magic on troops.
- **Q11 Stale `Bt_MsgMode`** (flow spec Q-10). Goliath and ninja do not set it; FIGT_M also writes 2 back.
  Goliath after an enemy magic action speaks from the enemy side; the RND(2) at FIGT_M:5 is
  meaningful only if the stale value is outside 1..4.
- **Q12 Defend values subtract from hit chance even when "1 = acted"**: an acted slot
  defends with 1, an un-acted slot with 0; the actor itself is always state 1, so counters
  against it get −1.
- **Q13 Multi-arm `RetVal` tests.** Sergeant Hanny (C83) / Lieutenant Hanny (C84)
  (FIGT_M:5016-5075) and Bound (U3) (:8870-8887) test
  `RetVal` after showing a line; if a window call changes `RetVal` a second line could show
  (Q-open O1). Presentation only.
- **Q14 Ninja never kills** (`Cmdr_HP > 1`), never consumes its availability flag, needs the
  1/3 roll at 1915, and only follows player-initiated engagements.
- **Q15 Compaction leaves `Bt_PlayerOrder4`** unchanged when a unit in slot 1-3 falls (2858-2897).
- **Q16 `EnemyLosses` can exceed real losses** (special 100 adds 20000; beam/Vanish/War
  Dance/spread can "kill" more than present; troops floor at 0). It drives retreat modes
  3/4.
- **Q17 Item record 0.** `Unit_Item = 0` binds item record 0; with stride 50, its fields
  alias item 50's (`Item_Effect[0]` ≡ item 50's amount = 0), so "no item" has no effect.
  Item 50's own effect aliases dungeon record 0 — out of scope.
- **Q18 Player-side `Mode = 2` guard is dead** (FIGT_M:8845); `Bt_MsgMode = 2` always routes
  to the enemy side.
- **Q19 Special names in string slot 7** are written but never printed; the UI only says
  "Special Attack".
- **Q20 `RND(n)` may return `n+1`** under xsystem35 (closed-interval MT). Consequences:
  RND(100) = 101 misses even at 100%; flavour selectors show nothing; spread +0; Miki/ninja
  rolls fail. Reproduce whatever the chosen RNG backend does.
- **Q21 Sieg event gives Chaos to Rance** by `Unit_Item = 1` without clearing the holder of
  his previous item (FIGT_M:10096-10113), and changes Sieg (commander 93) before the exchange.
- **Q22 Kayblis (C135) at war rolls 1..100 commander-vs-commander shots** (2839-2841),
  bypassing the `> 5 → RND(6)` cap.
- **Q23 Wall advantage display vs rule.** The battle header shows walls as enemy advantage for
  `Bt_Kind` 1 and 3 (181-186) but the penalty applies only for `Bt_Kind` 1 (player troops) and
  2 (enemy troops).
- **Q24 Commander-only fighting rule.** A commander whose own troops exceed the battle limit
  never fights personally (steps b/e need `AllEngaged`), and troops never shoot a commander
  whose army exceeds the limit (steps a/d).
- **Q25 Goliath skips the retreat-flag check** after its strike (1144-1148).
- **Q26 War Dance hurts Patton**: `Cmdr_Troops/100` HP from enemy troops (2367-2368), unless
  shielded.

---

## 11. Open questions

Merged with the flow spec's open questions in [README.md](README.md).

| # | Question | Lines |
|---|---|---|
| O1 | Does `IK 0` (in `WAO_SUB.ADV` `L_00ce1`, run after every window) overwrite `RetVal`? `WAO_SUB.ADV:292-294` saves/restores `RetVal` around another `IK 0`, which suggests it does. Affects only which flavour lines show for Sergeant Hanny (C83) / Lieutenant Hanny (C84) / Bound (U3) (Q13). | FIGT_M:5014-5077, 8859-8888; WAO_SUB:292-294, 510-519 |
| O2 | Operator precedence of the decompiled expression in Q8 — confirm against the bytecode (RPN order) before relying on it. | FIGT_M:8326 |
| O3 | What record 51 holds at a new game / after loading, before any kaiju is summoned (affects `RND(Unit_Attacks)` for Goliath and ninjas). Answered 2026-10-23 from `SET.ADV:225` and the captures: 0 in every field but those the ninja/Goliath builders and `FIGT表示.ADV` write (CGs, face, status, troops, HP); `Unit_Item` only if the player equips the record through `Sｱｲﾃﾑ`'s 51st cell (CMD-24) — [README](README.md) #3. | FIGT:1126-1137, 1922-1979, 2592, 2648 |
| O4 | Whether the original `system39.exe` RNG can return `n+1` like xsystem35. | — |
| O5 | Meaning of the overlay test `Bt_EnemyParty1 != 1137` (display only). | FIGT:1715 |
| O6 | Whether `Bt_MsgMode` can be outside 1..4 at a Goliath strike (fresh game: `SET_VER` sets 0) — then FIGT_M:5's draw picks the side. Answered 2026-10-23 from `ｽﾀｰﾄ.ADV:619` (the boot's 0; `SET_VER` is never called) and the writers `FIGT:1364-1606`, `FIGT_M:3`: yes, 0, only when the strike is the game's first exchange since the boot — reachable through a debug chapter start (`ｽﾀMENU.ADV:304-546`) — [README](README.md) #6. | FIGT:1144, FIGT_M:5-23 |

---

## Dictionary corrections

Applied to `variables.tsv` on 2026-10-08 (all 18 proposals; no renames). None rejected.
