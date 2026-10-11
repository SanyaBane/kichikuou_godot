# Battle specification 01 — flow (everything except per-exchange damage resolution)

Source: `E:\Modding games\Modding Rance\Kichikou\_extract\scenario_named\` (line numbers are identical
to the raw decompile). Variable names come from `docs/variables.tsv`. Confidence tags as in
`spike-findings.md`: **[C]** read in code, **[I]** inference, **[G]** guess.

Scope of this file: how a battle is entered (`FIGT_A`, `FIGT_B`, dungeon and scripted callers),
setup and the pre-battle strategy roll (`FIGT.ADV:1-104`, `FIGT作.ADV`), the round loop, turn order,
the player command menu (`FIGT_SEL.ADV`, `FIGT.ADV:595-1410`), the enemy in-battle AI
(`FIGT.ADV:1411-1491`), the extra actors (Goliath, capsule kaiju, Fog Card, ninja follow-up),
the call interface into the exchange resolution, applying its results, deaths, slot compaction,
end of battle, EXP / level-up (`FIGT.ADV:2977-3160`), the wrappers' post-battle processing and
`死亡捕獲.ADV`.

Out of scope (black boxes, specified in [02-resolution.md](02-resolution.md)): the exchange
resolution `FIGT.ADV:2137-2857` (`L_0347f` … `L_045f7`, entry `L_035fb` = `ComputeExchange`,
[02 §6](02-resolution.md#6-computeexchange-l_035fb-2200-2449-and-helpers)), the exchange
animation `FIGTATTK.ADV` ([02 §7.2](02-resolution.md#72-playexchangeanimation-figtattkadv)), and
the flavour-text tree `FIGT_M.ADV` ([02 §5](02-resolution.md#5-flavourtext-figt_madv--control-flow-and-rng))
/ death texts `FIGTﾐｶDE.ADV`, `FIGTﾃｷDE.ADV` (only their state writes and RNG are summarised
here). The seam between the two files is §9 here ↔ [02 §1](02-resolution.md#1-call-interface-black-box-for-the-flow-spec);
both use the procedure names of 02 §1.1 (`ManualAttackPrelude`, `Resolve`, `ResolveMagic`,
`PresentEngagement`, `ComputeExchange`, `ApplyExchange`). The merged draw order of a whole
battle is in [03-rng-order.md](03-rng-order.md).

---

## 0. Conventions

### 0.1 Arithmetic and RNG

All values are 16-bit unsigned and **every single operation saturates** (`-` floors at 0, `+`/`*`
cap at 65535, `/0` = 0). Pseudocode keeps the original expression shape; `-` always means the
saturating subtraction.

`RND(n)` = `Y 4, n:` → `RetVal` = 1..n. **If n is 0 or 1 it returns n and does not draw.**
Every `RND` is tagged `[Rk]` and listed in §12 in execution order.

`Y 3, n:` is **not** random: it is a timed pause (presentation; written `Pause(n)` below)
**[C]** (confirmed against xsystem35). `Y 4` is the game's only random source (`ZR` and DLL
modules are unused).

### 0.2 Slot arrays

The script spells out four copies of everything. In the pseudocode:

| Notation | Variables |
|---|---|
| `PParty[i]` | `Bt_PlayerParty1..4` (VAR0569-0572) — deployment chosen before the battle; never changed by FIGT |
| `EParty[i]` | `Bt_EnemyParty1..4` (VAR0530-0533) |
| `PSlot[i]` | `Bt_PlayerSlot1..4` (VAR0590-0593) — live player unit ids |
| `ESlot[i]` | `Bt_EnemySlot1..4` (VAR0594-0597) — live enemy commander ids |
| `PState[i]` | `Bt_PSlot1..4State` (VAR0047-0050) |
| `EState[i]` | `Bt_ESlot1..4State` (VAR0051-0054) |
| `PFocus[i]` | `Bt_PSlot1..4Focus` (VAR0598-0601) |
| `POrder[i]` | `Bt_PlayerOrder1..4` (VAR0693-0696) |

Slot state values: `0` has not acted this round, `1` acted, `2..21` player defended (value =
defence bonus), `20` enemy defended, `100` empty at round start. Any nonzero value means "already
acted". Front row = slots 1-2, back row = slots 3-4.

Record access: `Unit.X` = field X of the player-unit record selected by `Unit_Cur`;
`Cmdr.X` = field X of the enemy-commander record selected by `Cmdr_Cur`; same for `Prov`, `Item`.
Changing the cursor changes which record the field names refer to. `Unit[k].X` / `Cmdr[k].X` /
`Prov[k].X` = "set the cursor to k, then use field X" (the script leaves the cursor at k).
`Event(F, n)` = `Ev_Entry := n; call F.ADV` (a story scene; may change any state). `x ∈ PParty`
= `PParty[1] = x or … or PParty[4] = x`.

Unit 51 is a scratch record: FIGT overwrites it for Goliath, the capsule kaiju and the ninja
follow-up. Its fields not explicitly set keep whatever the previous use left.

### 0.3 Presentation

Screen drawing is summarised as events: `Show(...)`, `Msg(purpose)`, `Redraw()` (= `L_03058`:
redraw the battle field — background, all slot panels), `Highlight(slot)` (= `L_0320f`),
`PlaySfx`, `PlayJingle`. **`Redraw()` and `Highlight()` have a state side effect** — see §9.4.

---

## 1. Overview

```
caller (Cﾃｷﾄｼ attack / T* nation phase / dungeon / story scene)
  sets EParty[], PParty[] (via Sﾌﾞﾀｲ or forced), Bt_EnemyRetreatMode, Prov_Cur, ...
   │
   ├─ FIGT_A.ADV  (player attacks; Bt_Kind=1; plains/city from Bt_WallBonus)
   ├─ FIGT_B.ADV  (enemy attacks; intercept / ninja scout / abandon; Bt_Kind=2)
   └─ direct %#FIGT.ADV (dungeon Bt_Kind=3, God dungeon)
         │
         FIGT.ADV: Setup → Strategy (FIGT作) → repeat { RoundStart → StepLoop → RoundEnd }
                   StepLoop: pick the not-yet-acted unit with the fewest troops →
                             player: command menu (FIGT_SEL) or standing order
                             enemy: AI
                             → exchange: PresentEngagement L_02609 (screen + FIGT_M text)
                                         → ComputeExchange L_035fb (black box, 02 §6)
                                         → ApplyExchange L_02d45 (FIGTATTK animation, apply,
                                           deaths, ninja)
                   exits: annihilation / timeout / enemy retreat / player retreat → EXP → return
   │
wrapper: result messages, ownership change, story hooks, 死亡捕獲.ADV (revives / events)
```

---

## 2. Entry points

### 2.1 Player attack: `Cﾃｷﾄｼ.ADV` → `FIGT_A.ADV`

**Caller setup** (`Cﾃｷﾄｼ.ADV:1-425`, the enemy-province menu "Attack"):

1. `Bt_TargetProv := Prov_Cur` at file entry (`Cﾃｷﾄｼ.ADV:3`).
2. Blocked if `Ui_CmdAttackAvail = 0` (`:112`). Story stage 0 → prologue file instead (`:101-108`).
3. Max party size `Tmp0021 := 4`; mountain passes: attacking Log A (32) or Log B (31) allows only
   **1 unit** unless the player owns one of the listed neighbours (`:172-199`: Log A (32) needs
   Vladivostok (33) / Golak (34) / Log B (31), Log B (31) needs Archgrad (36) / Log A (32) / Volgo Z
   (30) — `:192`).
4. `Sﾌﾞﾀｲ.ADV` (party picker, mode `Tmp0022 = 1`): fills `PParty[]`; only units with
   `Unit_Status = 1` are selectable; on "Go" it requires `PParty[1] ≠ 0` and
   `Gold ≥ Σ (Unit_DeployCost + Unit_Troops / 300)` over the party, then sets each
   `Unit_Status := 2`, `Gold -= cost`, `Hist_MilitaryExp += cost` (`Sﾌﾞﾀｲ.ADV:159-210`, `:347`).
   Cancel → `PParty[] := 0`.
5. Refusals (`Cﾃｷﾄｼ.ADV:226-307`): Gurwin (unit 44) vs owner 11; Gandhi (37) vs owner 7 while
   `F_Zeth_State < 8`; Satella (29) / Meglass (30) unless owner is 8 or 10. A refusal calls
   `L_05118` which only resets `Unit_Status := 1` for the party — **the war expenses already paid
   are not refunded** (quirk Q-1).
6. `Bt_WallBonus := 0` (`:317`), then the defending nation's setup file chooses the defenders,
   the retreat rule and plains/city — see §2.2.
7. If `EParty[1] = 0` after that: `Prov_Owner := 1`, `Ui_CmdAttackAvail := 0`, "occupied without
   difficulty", no battle (`:395-418`).
8. `Bt_Kind := 1`; `%#FIGT_A.ADV`; `Bt_Kind := 0` (`:419-425`).

**`FIGT_A.ADV`** — pseudocode:

```
# pre-battle story hooks (FIGT_A.ADV:1-30)
if Prov_Cur = 5 and F_Ex_State = 1: Event(Gｴｸｽ, 2)
if Prov_Cur = 6 and F_Ex_State = 2: Event(Gｴｸｽ, 3)
if Prov_Cur = 55 and N_JapanInv_Stage = 0:
    N_JapanInv_Stage := 1
    a := (Cmdr[75].Status ≠ 0); b := (Cmdr[82].Status ≠ 0)
    if a and b: Event(IJAPAN, 3)
if Prov_Cur = 56 and N_JapanInv_Stage = 1: N_JapanInv_Stage := 2; Event(IJAPAN, 4)

# plains or city (FIGT_A.ADV:31-55)
if Bt_WallBonus = 0: Bt_TroopLimit := 5000;              Msg(enemy wants a field battle)
if Bt_WallBonus ≠ 0: Bt_TroopLimit := Prov_BattleArea;    Msg(enemy stays in the city)
                     Bt_BackgroundCg := 902
# background (presentation only) (FIGT_A.ADV:56-67)
if Prov_Cur > 77 and Bt_BackgroundCg = 900: Bt_BackgroundCg := 908 (918 for 82/83, 910 for 79)
if EParty[1] = 18: Bt_BackgroundCg := 904

Bt_Kind := 1
call FIGT.ADV
Bt_BackgroundCg := 900

if Bt_IsDuel = 1: goto POST                                    # FIGT_A.ADV:73
if Bt_Result = 1: PlayJingle(defeat); Msg(defeat; "conquest of <prov> failed" if StoryStage ≠ 0)
if Bt_Result = 2:
    if StoryStage ≠ 0 and Bt_Kind = 1: Prov_Owner := 1; Ui_CmdAttackAvail := 0
    MapMarker(Prov_Owner); PlayJingle(victory)
    Msg(success; "<prov> placed under our control" if StoryStage ≠ 0)
POST:                                                           # FIGT_A.ADV:147
Bt_Kind := 0
Prov_Cur := Bt_TargetProv
if Bt_IsDuel = 1: Prov_Cur := 1; Bt_TargetProv := 1
if Prov_Cur = 56 and N_JapanInv_Stage = 2:
    N_JapanInv_Stage := 3
    if Cmdr[72].Status ≠ 0: Event(IJAPAN, 5)
if Prov_Cur = 56 and F_Japan_State ≠ 10 and Bt_Result = 2: Event(IJAPAN, 6)
if Prov_Cur = 30 and Bt_Result = 2 and F_SillKidnap_Stage < 50: Event(Gｼｨﾙ, 28)
Unit_Cur := 9
if (PParty[1] = 9 or PParty[2] = 9) and Unit.Status ≠ 0 and F_Kinkaid_Done = 0 and Bt_Result = 2
   and 29 < Prov_Cur < 54: Event(Gｷﾝｹｰﾄﾞ, 1)
Unit_Cur := 39
if 39 ∈ PParty[1..4] and Unit.Status ≠ 0 and Prov_Cur = 91 and Bt_Result = 2 and F_Magic_State = 5:
    Event(Gﾏｼﾞｯｸ, 25)
if Prov_Cur = 69 and Prov_Owner = 1 and Ev_PhaseNation = 7: Event(Iｾﾞｽ, 50)
Prov_Cur := Bt_TargetProv
call 死亡捕獲.ADV                                              # §11
Prov_Cur := Bt_TargetProv; Bt_TargetProv := 0
if StoryStage = 1 and Prov_Cur = 6 and Bt_Result = 2: Event(Iｼﾚｲﾌﾞ, 2)
if Prov_Cur = 45 and Bt_Result = 2 and F_Helman_War = 1: Event(Gﾐﾈﾊﾞ, 5)
if Prov_Cur = 77 and N_ALChurch_Stage < 50 and Bt_Result = 2: Event(GAL, 10)
return
```

Notes:
- A **defeat does not clear `Ui_CmdAttackAvail`**; only victory does. The player may attack again
  the same week with units still at `Unit_Status = 1` (quirk Q-2).
- In the prologue (`StoryStage = 0`) a victory changes no ownership.
- Troop and HP losses need no write-back: the exchanges modify the unit / commander records
  directly (§9.3).

### 2.2 Defender selection when the player attacks (`T*2.ADV`)

`Cﾃｷﾄｼ.ADV:317-376` dispatches on `Prov_Owner`:

| Owner | Setup |
|---|---|
| 2 Rebels | `%#TCﾘｰｻﾞｽ2.ADV` |
| 6 JAPAN | if `F_Japan_State = 10`: single "Fleeing Soldier" cmdr 73, refilled (Status 1, HP max, troops max), `Bt_EnemyRetreatMode := 1`, straight to battle (`:321-340`); else `%#TJAPAN2.ADV` |
| 7 Zeth | `%#Tｾﾞｽ2.ADV` |
| 4 Helman | `%#Tﾍﾙﾏﾝ2.ADV` |
| 11 ALism | `Event(GAL, 9)` |
| 8 Kayblis | `%#Tｹｲﾌﾞﾘｽ2.ADV`; if `F_Warg_State = 1` afterwards: clear it and return without battle |
| 5 Patton | `Bt_EnemyRetreatMode := 1`; priority list Patton (54), Hubert (55), Freak (56), Hanty (57) (+Aristoles (32) if `F_Aristoles_Helman2ndState > 9`) through `L_04f2a` |
| 3 Free cities | not attacked from this path: `L_00a07` (city story options); the battle option is "Use Force" (`Cﾃｷﾄｼ:3815`, last row of the table below) |

Common helper ("first four alive", `Cﾃｷﾄｼ.ADV:4265-4341`, identical copies in each T* file):

```
EParty[1..4] := 0
for k in 1..9:                       # Tmp0001..Tmp0009 = priority list, 0 = none
    if k ≥ 5 and EParty[4] ≠ 0: return       # checked only from the 5th entry on
    Cmdr_Cur := list[k]
    if Cmdr.Status ≠ 0: put Cmdr_Cur into the first EParty slot that is 0 (none free → dropped)
```

Helman's copy (`Tﾍﾙﾏﾝ2.ADV:358`) additionally skips, after slot 1 is filled, commanders with
`Cmdr_Troops < 190` (except in Lang Bau (45)).

**Plains or city is decided only by `Bt_WallBonus`** (FIGT_A overwrites `Bt_TroopLimit`
itself). There is no generic "walls ⇒ city" rule; each file decides. `Bt_EnemyRetreatMode` is
**not reset** between battles: a path that does not write it inherits the previous battle's value,
possibly the runtime values 20/50 (Q-23).

Defender tables (from reading the T*2 files; `RND` here belongs to the AI spec but is listed for
the sequence):

| Owner / file | Plains or city | Retreat | Lists and notes |
|---|---|---|---|
| 2 `TCﾘｰｻﾞｽ2` | plains; Oaks (6) city (`Prov_Defense`) | 4 (Baranchi (7) / Wes (8) / others, North (2) / Oku (3) / South (4)), 2 (Ys (5)), 1 (Oaks (6)) | Baranchi (7) / Wes (8) / any other → Ex (1), Haurein (2), Sakanaku (6), Jibul (7); North (2) / Oku (3) / South (4) → Pegasus (3), Dodge (5), Melfeis (4); Ys (5) / Oaks (6) → Ex (1), Haurein (2), Pegasus (3), Sakanaku (6), Melfeis (4), Dodge (5), Jibul (7). If the party's troop sum < 100 the file exits before setting the retreat mode (stale), but still fights. `Ev_PhaseNation := 2` |
| 6 `TJAPAN2` | always `Prov_Defense` (city if walls) | 1 (Nagasaki (55), Osaka (56)), 4 otherwise | Nagasaki (55) → Katsuie (75), Harumi Ueda (76), Mori Harada (77), Denden Mori (78); Osaka (56) → Nobunaga Oda (72), Kenichi Oda (73), Yamato Akechi (74), Denden Mori (78), Mori Harada (77), Katsuie (75), Isoroku Yamamoto (82), One-Shot Tarou (79), Noberu Maeda (80); others: of groups A (=Osaka (56) list), B (Katsuie (75) through Denden Mori (78)), C (One-Shot Tarou (79) through Isoroku Yamamoto (82)) the one with the largest troop sum (ties → earlier). Sum < 10 (Nagasaki (55)) / < 500 (others) → exits with stale mode. Writes `F_Japan_State := 1` on every attack |
| 7 `Tｾﾞｽ2` | `Prov_Defense`; Parapara Fortress (16) plains | 1 | Wall cmdrs Wall (94), Wall (95), Mage Leader (96) refilled each attack; on a repeat attack on the same garrison province, 3 draws `RND(Cmdr_Troops/2)` subtracted (Wall (94), Wall (95), Mage Leader (96)). Towers Adam's Fortress (58) / Tape (61) / Amber Castle (65) / Old Zeth (63) / Magic's Tower (66) / Nagi's Tower (67) / Chizuko's Tower (68) / Papaya's Tower (70) have boss lists (fallback Wall (94), Wall (95) + boss with 10 troops when only the boss is alive); otherwise forced Wall (94), Wall (95), Mage Leader (96). Barrier: Magic's Tower (66) / Nagi's Tower (67) / Chizuko's Tower (68) / Papaya's Tower (70) while `VAR0063 ≠ 2` and no deployed unit holds item 12 → `PParty[1] := 0`, no battle. Papaya / Zeth-war story writes |
| 4 `Tﾍﾙﾏﾝ2` | `Prov_Defense` (normal); plains in the `F_Helman_War = 2` branch | 4 mostly, 1 for Rosesgrad (39), Laurengrad (48), Lang Bau (45), Cossack (37), Archgrad (36), Sudori 10 (42), Ancient Ruins (35) and Log B (31) / Log A (32) when `PParty[2] = 0` | Militia (`L_00210`, also the fallback for unlisted provinces): cmdrs Civilian General (58), Civilian General (59) `Status 1`, HP max, troops `100 + RND(800)` each (2 draws), party Civilian Generals (58, 59). Rosesgrad (39) / Siberia (38) / Golak (34) / Log A (32) / Log B (31) / Volgo Z (30) / Maunes (13) / North (2) (and Archgrad (36) unless Lelyukov joined): militia if `F_Lelyukov_State = 1` or Lelyukov (cmdr 22) dead, else the larger-sum of A = Lelyukov (22), Gadman (23), Jackal (24), Samisukan (25), Luke (29), Yorkdelta (30), Laumiere (27), Luc (28), Tainyar (26) / B = Laumiere (27), Luc (28), Luke (29), Yorkdelta (30), Jackal (24), Samisukan (25), Lelyukov (22), Gadman (23), Tainyar (26) (ties → B; Volgo Z (30) with Laumiere (27) alive → Laumiere (27) alone). Sudori 17 (46) / Parnas (47) / Laurengrad (48): Ivan (34), Samon (35), Yasutoeh (36), Yutos (37), Keigo (38), Aristoles (32), Convert (33) (militia if Aristoles (32) dead). Lang Bau (45) / Micrograd (44): 2 more militia draws, then Serges (42), Booch (43), Mineva (39), Tami (40), Maha (41) (+Civilian Generals (58, 59) for Lang Bau (45)) (militia if Mineva (39) dead). Pawn (40) / Sudori 13 (41) / Cossack (37): Sukakumi (49), Marquez (46), Hyam (47), Nero (44), Cream (45), Yang (48) (militia rules with Nero/Cream flags). Archgrad (36) with `F_Lelyukov_State = 1`: forced Lelyukov (22) alone with 10 troops. Sudori 10 (42): Alexei (53), Rolex (50), Hyam (47), Saulnia (52), Katyusha (51). Ancient Ruins (35): Ruberan (31) alone. `F_Helman_War = 2` branch: Sheila events may leave `EParty[1] = 0` (taken unopposed), else militia on the plains |
| 5 inline (`Cﾃｷﾄｼ:361`) | always plains (`Bt_WallBonus` stays 0) | 1 | Patton (54), Hubert (55), Freak (56), Hanty (57) (+Aristoles (32)). `Ev_PhaseNation` and the army name are not set (stale) |
| 8 `Tｹｲﾌﾞﾘｽ2` | city (`Prov_Defense`) when the stage-2→3 switch fires (Tower of Evil (78), Grief Valley (89), Bone Forest (90), or Tearing Forest (91) in stage 2 — `*` binds tighter than `+`) and for Demon Lord's Castle (79), Kayblis's Castle (84); else plains | 1 | 4 draws, cmdrs Monster Generals (86-89): `Status 1`, HP max, `RND(MaxTroops / k)` with k = 4 (new garrison province) or `2 + Bt_GarrisonHits` (repeat), troops `:= MaxTroops / (4 + Bt_GarrisonHits * 2) + r` (`Tｹｲﾌﾞﾘｽ2.ADV:4-31`); then (not stage 3) `RND(2)` picks Monster Generals 86-89 or 86-87. Stage-3 lists per province (Kaybwan (159) through Kunihiko (164), Kesselring (133), Camilla (91), Monster Generals (181-183) + Kayblis (135), Medusa (134)). Tower of Evil (78) with `N_Warg_Stage = 0` → Warg event, no battle |
| 11 `GAL` entry 9 | always plains | 1 | Temple AL HQ (77): Temple Knights (139-142) revived and topped up; elsewhere a rebuilt cmdr 100 ("AList", 50 troops, HP 3) alone |
| 3 (independent) "Use Force", `Cﾃｷﾄｼ:3815` | `Bt_WallBonus` **not written** (stale from the last battle) | 1 | per-province list through `L_04f2a`; afterwards Gﾚﾍﾞｯｶ (Portugal (28)) / Gｶｾｲ (Mars (22)) if owned |

Ways the attack ends without FIGT: `EParty[1] = 0` (taken unopposed), `PParty[1] = 0` set by a
setup file (barrier text), `F_Papaya_State = 2` (event), Kayblis Warg flag.

### 2.3 Enemy attack: `T*` nation phase → `FIGT_B.ADV`

**Caller pattern** (all 14 caller files, survey):

- `Ev_PhaseNation` = attacking nation (set at the top of each `T*` file).
- `Bt_EnemyRetreatMode` set before every attack block (callers must, because FIGT writes 20/50
  back into it).
- `EParty[]` from the per-file picker (priority list of commander ids, first four alive; Helman,
  JAPAN and Rebels skip commanders with `Cmdr_Troops < 190` once slot 1 is filled — the Rebels
  version applies it to slot 1 as well; Kayblis skips, once slot 1 is filled, commanders with
  `Cmdr_Troops < 99` and `Cmdr_Class < 5` — `Tｹｲﾌﾞﾘｽ.ADV:1581`), or written directly.
- Minimum total troops before attacking (500 / 1000 / 1500 depending on the army).
- `Prov_Cur` walks a fixed list of target provinces; the first with `Prov_Owner = 1` is attacked
  (`{Prov_Owner = 1: %#FIGT_B.ADV: \0:}`). One attack per army per week.
- Some callers refill / revive commanders first (e.g. `Tｾﾞｽ.ADV:239-242` troops = max/8,
  `Tﾍﾙﾏﾝ.ADV:528-531` troops = 500 + rnd(500)). These draws belong to the AI spec.
- No caller sets `PParty[]`, `Bt_IsDuel`, `Bt_Kind`, `Bt_TroopLimit`, `Bt_WallBonus`.
- Afterwards most callers read nothing. Exceptions: `Bt_Result = 1` (Iﾏｼﾞﾝ:81/597, Tｹｲﾌﾞﾘｽ:950/1204);
  `Bt_RoundsLeft = 0 and Bt_Result = 2 and Ai_*Atk = 10` → the Kayblis sub-army attacks a second
  time that week (`Tｹｲﾌﾞﾘｽ.ADV:826-828` and 945/1087/1200); leader `Cmdr_Status` checks.
- Callers without an ownership check: Gｶｾｲ:3085 (Leazas Castle (1)), Gｹｲﾌﾞﾘｽ:756 (Silky's Castle
  (81)), Iﾏｼﾞﾝ:62/582 (Leazas Castle (1)); Gﾚﾘｭｰｺﾌ:56-66 inherits `Prov_Cur` from its T caller.

**`FIGT_B.ADV`** — pseudocode:

```
if EParty[1] = 1 and F_Ex_State = 0: Event(Gｴｸｽ, 1)                 # FIGT_B.ADV:1-4
ANNOUNCE:                                                            # L_0003d (:5)
    Bt_TargetProv := Prov_Cur
    CenterMap(Prov); Msg(<army> appeared near <prov>; Maris: "it will be occupied")
MENU:                                                                # L_00105 (:46)
    choice := Menu["Intercept", "Ninja Scout", "Abandon City"]   (no cancel: menu repeats)

  "Intercept":                                                       # :65
    Sﾌﾞﾀｲ(max 4 units, mode 2)  → Tmp0621 := return code (501 plains, 502 city, 503 stop)
    if 37 ∈ PParty and Ev_PhaseNation = 7 and F_Zeth_State < 8:
        Msg(Gandhi refuses); UndoDeploy(); goto ANNOUNCE
    if 29 ∈ PParty and Ev_PhaseNation ∉ {8, 10}: Msg(Satella refuses); UndoDeploy(); goto ANNOUNCE
    if 30 ∈ PParty and Ev_PhaseNation ∉ {8, 10}: Msg(Meglass refuses); UndoDeploy(); goto ANNOUNCE
    if Tmp0621 = 501: goto PLAINS
    if Tmp0621 = 502: goto CITY
    if Tmp0621 = 503: goto ANNOUNCE
    # any other code falls through to PLAINS (after a portrait refresh)
  PLAINS:                                                            # L_00393 (:164)
    Bt_TroopLimit := 5000; Bt_WallBonus := 0; Msg(move out)
    goto FIGHT
  CITY:                                                              # L_003e4 (:181)
    Bt_TroopLimit := Prov_BattleArea
    Bt_WallBonus  := Prov_Defense
    Bt_BackgroundCg := 902
    Prov_BattleArea := Prov_BattleArea + Prov_BattleArea / 10        # permanent +10 % (Q-3)
    RND(2) [R1]; if RetVal = 1: VAR0692 := VAR0692 - 1                # write-only, dead
    Msg(fighting inside the city)
  FIGHT:                                                             # L_0049b (:217)
    if F_Kayblis_War = 2 and EParty[1] = 135:
        if 29 ∈ PParty: Event(Gｹｲﾌﾞﾘｽ, 5)
        if 30 ∈ PParty: Event(Gｹｲﾌﾞﾘｽ, 6)
        if 41 ∈ PParty: Event(Gｹｲﾌﾞﾘｽ, 7)       # these may remove a unit from PParty
    if PParty[1] = 0: goto ANNOUNCE
    if Prov_Cur > 77 and Bt_BackgroundCg = 900: Bt_BackgroundCg := 908 (918 for 82/83)
    Bt_Kind := 2
    call FIGT.ADV
    Bt_BackgroundCg := 900
    if Bt_Result = 2: PlayJingle(victory); Msg(<prov> successfully defended)
    Bt_Kind := 0
    if Bt_Result = 1: LOST()                                           # L_01165
    Prov_Cur := Bt_TargetProv
    call 死亡捕獲.ADV
    Prov_Cur := Bt_TargetProv; Bt_TargetProv := 0
    if PParty[1] = 14 and F_Ex_State = 4: Event(Gｴｸｽ, 6)
    if 30 < Prov_Cur < 50 and Bt_Result = 2:                           # :281
        RND(5) [R2]
        if Prov[40].Owner ≠ 1: RetVal := 4            # (cursor saved/restored via Tmp0583)
        if RetVal = 1 and F_Elena_State = 0: Event(Gｴﾚﾅ, 1); return
        if RetVal = 1 and F_Elena_State = 1: Event(Gｴﾚﾅ, 2); return
        if RetVal = 1 and F_Elena_State = 2: Event(Gｴﾚﾅ, 3); return
        if RetVal = 1 and F_Elena_State < 7: Event(Gｴﾚﾅ, 7); return
    return

  "Ninja Scout":                                                     # L_00727 (:311)
    if F_Kanami_NinjaUsed ≠ 0 and F_Freya_NinjaUsed ≠ 0 and F_Kaoru_NinjaUsed ≠ 0
       and F_Wichita_NinjaUsed ≠ 0 and F_Saitou_NinjaUsed ≠ 0:
        Msg(no ninjas available); goto MENU
    pick := Menu[each ninja whose flag = 0: Kanami, Freya, Kaoru, Wichita, Saitou; "None"]
    "None": Msg; goto MENU
    ninja k: Bt_ScoutNinja := k (1..5); that ninja's flag := 1
             Show(enemy commanders: name, troop type, troops; count; total troops)
             Msg("quite a strong force" if total > 5000)
             goto MENU
    # FIGT_B.ADV:673-713 (ninja never comes back and is captured: Gｶﾅﾐ 100, N_Freya_State := 10,
    # N_Kaoru_State := 0, N_Wichita_State := 0, F_Saitou_State := 0) is UNREACHABLE: it sits after
    # the `\0:` that ends L_00dad and has no label (Q-4).

  "Abandon City":                                                    # L_00f1b (:714)
    Bt_Result := 1; PParty[1..4] := 0; Bt_TargetProv := 0
    if Prov_Cur = 1 and F_Ex_State = 4 and Week > 20:
        Event(Gｴｸｽ, 5)
        Unit[14].Status := 2; PParty := (14, 0, 0, 0)
        Bt_TroopLimit := Prov_BattleArea; Bt_WallBonus := Prov_Defense; Bt_BackgroundCg := 902
        goto FIGHT                       # NB: Bt_TargetProv is already 0 here (Q-5)
    Msg(Rance doesn't care)
    Prov_Owner := Ev_PhaseNation; MapMarker; PlayJingle(defeat); Msg(<prov> occupied by <army>)
    if Prov_Cur = 1: Msg(castle fallen); EndingId := 2; jump OVER.ADV     (game over)
    return                               # no 死亡捕獲, no EXP

LOST():                                                              # L_01165 (:830)
    PlayJingle(defeat); Msg(defeated; <prov> occupied by <army>)
    Prov_Owner := Ev_PhaseNation; MapMarker
    if Prov_Cur = 1: Msg(castle fallen); EndingId := 2; jump OVER.ADV
UndoDeploy():                                                        # L_01270
    for i in 1..4: if PParty[i] ≠ 0: Unit[PParty[i]].Status := 1      # gold not refunded (Q-1)
```

Player input points in FIGT_B: the three-way menu (no cancel), the party picker, the ninja
menu (only ninjas whose weekly flag is 0, plus "None").

### 2.4 Dungeon battles (`Cﾒｲｷｭｳ.ADV:1282`, direct `%#FIGT.ADV`, `Bt_Kind = 3`)

Setup belongs to the dungeon spec; what FIGT receives:

- `PParty[1]` = the single exploring unit (Sﾌﾞﾀｲ mode 3; only slot 1 is used later).
- `EParty = (DungeonMonsterCmdr, 0, 0, 0)` for random encounters, `(100, 0, 0, 0)` for bosses.
- `Bt_TroopLimit` per dungeon (80 … 1000), `Bt_WallBonus := 0`, `Bt_EnemyRetreatMode := 1`,
  `Bt_BackgroundCg` 910 / 912 / 914.
- Random encounter troops: `RND(DgnRun_MonsterPower)`, `Cmdr_Troops := DgnRun_MonsterPower / 3 + r`,
  `Cmdr_HP := Cmdr_MaxHP` (`Cﾒｲｷｭｳ.ADV:1192-1195`), preceded by the encounter roll `RND(4)`
  (and `RND(10)` with item 10). Titi Lake / Haniwa Temple (dungeon 25) picks one of three templates
  for cmdr 100.
- Bosses rebuild cmdr 100 and set `Bt_IsDuel := 1` (all but Okayu Fever); the boss-beaten flag is
  set in the setup label, **before** the fight. Dungeon duels keep the dungeon's troop limit
  (the duel's 5000 applies only to `Bt_Kind ≠ 3`).

In FIGT, `Bt_Kind = 3` means: no strategy phase, retreat refused ("narrow dungeon"), timeout →
`Bt_Result 0`, EXP = 1 (0 on a loss), troop modifiers `Dgn*Mod`, enemy advantage shows the wall
bonus (0). After the battle the dungeon calls `死亡捕獲.ADV`, resets `Bt_Kind := 0`,
`Bt_IsDuel := 0`, never reads `Bt_Result`, and checks `Unit_Status` of `PParty[1]` (dead → leave).

### 2.5 God-dungeon garrisons (`Iｶﾐ.ADV:4607/4670/4733/4796`, direct, `Bt_Kind = 3`)

`Bt_TroopLimit := 800`, `Bt_WallBonus := 0`, `Bt_BackgroundCg := 916` once at file start.
Per floor N (2-5): `PParty := Ev_GodRearNF_Unit1..4` (the garrison left there; Rance excluded);
if `PParty[1] = 0` a FIGT表示 cutscene plays instead. `EParty := (k, 0, 0, 0)` with k = Kaiju
Gaga (187; 2F), Kaiju Zeze (188; 3F), Kaiju Dada (189; 4F), Kaiju Bobo (190; 5F) — the kaiju for
which the player-commander shield is disabled, `Bt_EnemyRetreatMode := 6` (3 rounds), `Bt_Kind := 3`, FIGT, `Bt_Kind := 0`, then
`Ev_GodRearNF_Unit1..4 := PSlot[1..4]` (survivors, compacted). No 死亡捕獲, `Bt_Result` unused.
The same garrison may be attacked repeatedly in cascades until wiped out.

### 2.6 Scripted battles through `FIGT_A.ADV`

FIGT_A does not reset `Bt_IsDuel`; every caller does. In a duel FIGT_A skips the result messages
and forces `Prov_Cur := 1, Bt_TargetProv := 1` (Leazas Castle) before 死亡捕獲. **A non-duel
scripted win in `StoryStage ≠ 0` sets `Prov_Owner := 1` for whatever `Prov_Cur` is** (Q-25).

| Caller | Player party | Enemy | Presets | Afterwards |
|---|---|---|---|---|
| Gｶﾌｪ:2049 | Sﾌﾞﾀｲ, 4 | Carfeknights (143, 144, 145) (troops +50, HP refilled if 0) | duel, retreat 1, WB 0, `Prov_Cur 1` (Leazas Castle) | `Bt_Result = 1` → defeat scene |
| Gｹｯｾﾙ:2434 | Rance | Kesselring (133), HP 2, troops 0 | duel, retreat 1 | 死亡捕獲 called again; Kesselring (133) `Status := 0` |
| Gｹｯｾﾙ:2959 | Rance | Kesselring (133), HP max | **not** duel, retreat 6, `Prov_Cur` inherited | Kesselring (133) `Status := 1`; 死亡捕獲 again |
| Gｻｰﾅｷｱ:581 | Sﾌﾞﾀｲ, 4 | Sanakia (62) | retreat 1, `Prov_Cur = Bt_TargetProv = 27` (Toushin City) | `Bt_Result = 1` → return; win sets Merim flags (and owner of Toushin City (27)) |
| Gｻﾃﾗ:3436 | Rance | 100 = copy of Satella's stats | duel, retreat 1, `Bt_TargetProv 0` | `F_Satella_State := 3` |
| Gｾﾙ:5828 | Rance | 100 "Ruffians" (20 troops, HP 5) | duel, retreat 1, WB 0 | — |
| Gﾅｷﾞ:3240 | Shizuka (17), troops capped to the enemy's | Nagi (107) (Magic 14 temporarily) | duel, retreat 1 | FIGT表示 10 |
| Gﾅｷﾞ:3422 | Shizuka, **HP 1, troops 0** (scripted loss) | Ragarl (109) | duel, retreat 1 | Shizuka (17) `Status := 0`, `F_RagarlRitual_State := 5` |
| Gﾅｷﾞ:4167 | Rance | Ragarl (109) | duel, retreat 1 | Ragarl (109) `Status := 1`, 死亡捕獲 again |
| Gﾏｼﾞｯｸ:2395 | random unit: up to 9 × `RND(48)` (Miki (32) → Kentarou (31)) until `Unit_Status = 1` | 97 Xacalite | duel, retreat 1 | `N_Xacalite_KillsLeft -= Bt_PlayerLosses` |
| Gﾐﾈﾊﾞ:2240 | Rance, troops set to 0 (restored after) | Mineva (39), HP 3, troops 0 | duel, retreat 1 | — |
| Gﾚｲ:2060, 2273 | Rance | Lei (92), troops 0 | duel, retreat 1 | — |
| Iｼｬﾝｸﾞﾘﾗ:3713 | Rance, troops 0 (restored) | Aladdin (21) | duel, WB 0, BG 902 | FIGT表示 1 |
| Iｼｬﾝｸﾞﾘﾗ:5052 | Sﾌﾞﾀｲ, 4, troops cut 10 % twice | Desert Guides (18, 19, 20) | duel (multi-unit), BG 904, `Bt_TargetProv 29` (Shangri-La) | — |
| Iｾﾞｽ:6349 | Rance | Saias (113), troops halved | duel, retreat 1 | `F_Uspira_Fate := 1`, Saias (113) `Status 0` |
| Iﾄｳｿﾞｸ:315 | Sﾌﾞﾀｲ, 4 | Civilian General (58) (troops 10) | not duel, retreat 1, `WB = Prov_Defense`, `Bt_TargetProv = Prov_Cur` | `Bt_Result = 1` → return; win: `RND(4)` loot event |
| Iﾄｳｿﾞｸ:1397 | fixed Rance (1), Bound (3), Soul (4), Sill (2) | Ruberan (31) | duel, retreat 1, limit 5000, `WB = Prov_Defense` | `Bt_Result = 2` → win scene |

Caller survey totals: FIGT.ADV is called directly only by Cﾒｲｷｭｳ (1) and Iｶﾐ (4); FIGT_A by
Cﾃｷﾄｼ (2) and 13 story files (19 calls); FIGT_B by 14 files (MAIN, GAL, Gｶｾｲ, Gｹｲﾌﾞﾘｽ, Gｻｲｾﾞﾙ,
Gﾚﾘｭｰｺﾌ, Iﾏｼﾞﾝ, TCﾘｰｻﾞｽ, TJAPAN, Tｹｲﾌﾞﾘｽ, Tｾﾞｽ, Tﾊﾟｯﾄﾝ, Tﾍﾙﾏﾝ, Tﾙﾄﾞﾗ).

### 2.7 `FIGT表示.ADV` (not a battle)

A cutscene player for 21 scripted "battles" (`Ev_Entry` 1-14, 21-24, 26; callers in Gｼﾙｷｨ, Gﾅｷﾞ,
Gﾊﾟｯﾄﾝ, Gﾌﾘｰｸ, Gﾏｼﾞｯｸ, Gﾐﾈﾊﾞ, Iｶﾐ, Iｼｬﾝｸﾞﾘﾗ, Iﾏｼﾞﾝ). It sets the 16 exchange results by hand and
calls `FIGTATTK.ADV` to animate them; it never calls FIGT/FIGT_A/FIGT_B. It does write state
permanently (e.g. Ev 2: Freak's (unit 20) troops/HP/CGs, Redeye (cmdr 130) troops 0 and HP −10;
Ev 4: Patton (unit 18) troops 500; Ev 9: Magic (unit 39) troops 0; Ev 11/12: Nagi / Ragarl (cmdr
107/109) HP; Ev 14: `Bt_TroopLimit := 5000`),
and every FIGTATTK call consumes one discarded `RND(2)`. Port it as scripted events, not battles.
Done so (2026-10-11): `FIGT表示` runs from the player's files as a scene, and the router sends its
`FIGTATTK` calls to the battle's port of that file (`Battle.PlayExchangeAnimationAsync`).

---

## 3. `FIGT.ADV` setup (`FIGT.ADV:1-104`)

**Entry:** from a wrapper with `Bt_Kind` ∈ {1, 2, 3}, `PParty[]`, `EParty[]`, `Bt_TroopLimit`,
`Bt_WallBonus`, `Bt_EnemyRetreatMode`, `Bt_IsDuel`, `Bt_BackgroundCg`, `Prov_Cur` set.
**Exit:** `%0` at `L_00dfd` with `Bt_Result` set (0/1/2) and all records updated.

Inputs read: world — `Unit.*` / `Cmdr.*` records of the deployed units, `Item.Effect/Amount`,
`Prov_Cur` (Land of Death 82, Goliath, garrison), `Prov_BgCg` (display), `Goliath_State`,
`Goliath_Prov`, `Kaiju_*Attacks`, `FogCard_Price`, `Bt_ExpBonusNext`, `Bt_GarrisonProv`,
`StoryStage`, `F_Kanami/Freya/Kaoru/Wichita_NinjaUsed`, `F_Kentarou_State`, `F_Asuka_State`,
`DebugMode`. Battle — all `Bt_*` listed in §0.2 and below.

Outputs written: world — `Unit.Troops/HP/Status/Exp/Level/Attacks/Recovery/MaxTroops/
Off*/Def*/Dgn*Mod/CurAtkMod/CurDefMod/Item`, `Cmdr.Troops/HP/Status`, the whole unit-51 record,
`Item[28].Holder`, `FogCard_Price`, `Goliath_State`, `Kaiju_AmoebaAttacks`,
`Kaiju_JagiJagiAttacks`, `N_Hubert_FreyaUses`, `N_Cream_Tactics`, `Bt_ExpBonusNext`,
`Bt_GarrisonHits`, `Hist_Casualties`, `Item_Cur`, `Buf_Val[0..99]` (cleared by FIGT_SEL), plus
whatever FIGT_M / FIGTﾐｶDE write (§10). Battle — `Bt_Result`, `Bt_RoundsLeft`,
`Bt_PlayerLosses`, `Bt_EnemyLosses`, `Bt_StrategyResult`, `Bt_EnemyRetreatMode` (20/50),
`Bt_EnemyRetreat`, `Bt_TroopLimit` (duel), all slot arrays, strategists, the exchange variables.

```
# FIGT.ADV:1-33
for i in 1..4: PSlot[i] := PParty[i]; ESlot[i] := EParty[i]
for i in 1..4: PFocus[i] := 0; POrder[i] := 0
RND(2) [R3]; Bt_RoundsLeft := RetVal + 3                  # 4 or 5 rounds
if Bt_EnemyRetreatMode = 6: Bt_RoundsLeft := 3
VAR0603 := 0; Bt_PlayerLosses := 0; Bt_EnemyLosses := 0
Bt_StrategyResult := 0; Bt_SpecialAtk := 0
Bt_PlayerTroopsStart := TroopSum(player)                  # L_04905, CallArg=1; never read
Bt_EnemyTroopsStart  := TroopSum(enemy)                   # L_04905, CallArg=2
Bt_EnemyRetreat := 0
PlayBattleMusic(); Redraw()

# strategists (FIGT.ADV:41-92)
Unit_Cur := PSlot[1]; best := StrategyValue()             # L_04e85, no ≠0 check for slot 1
Bt_PlayerStrategist := PSlot[1]
for i in 2..4: if PSlot[i] ≠ 0:
    Unit_Cur := PSlot[i]; v := StrategyValue()
    if v > best: best := v; Bt_PlayerStrategist := PSlot[i]     # ties keep the earlier slot
Cmdr_Cur := ESlot[1]; ebest := Cmdr.Strategy; Bt_EnemyStrategist := ESlot[1]
for i in 2..4: if ESlot[i] ≠ 0:
    Cmdr_Cur := ESlot[i]
    if Cmdr.Strategy > ebest: ebest := Cmdr.Strategy; Bt_EnemyStrategist := ESlot[i]

# FIGT.ADV:93-104   (Tmp0021 = best, Tmp0022 = ebest)
if Bt_Kind ≠ 3:
    if best > ebest: Strategy(side=1); Redraw()
    if ebest > best: Strategy(side=2); Redraw()    # re-tests clobbered Tmp0021/22: acts as else-if (Q-28)
    # equal → no strategy phase, no draw
goto ROUND_START

StrategyValue():                                           # L_04e85 (:3161)
    v := Unit.Strategy
    if Unit.Item ≠ 0: Item_Cur := Unit.Item; if Item.Effect = 6: v := v + Item.Amount
    return v                                               # in Tmp0023
TroopSum(side):                                            # L_04905 (:2935)
    s := 0; for i in 1..4: if Slot[i] ≠ 0: s := s + record(Slot[i]).Troops; return s   # in CallArg
```

## 4. Pre-battle strategy (`FIGT作.ADV`)

Input `CallArg` 1 (player has the better strategist) or 2 (enemy). `Tmp0021`/`Tmp0022` = the two
best strategy values. Output `Bt_StrategyResult`.

```
side 1 (FIGT作.ADV:7-46):
    d := Tmp0021 - Tmp0022                    # > 0 here
    RND(100) [R4]
    if d < RetVal: return                     # failure: nothing shown, Result stays 0
    Unit_Cur := Bt_PlayerStrategist
    m := d - RetVal
    Msg(ally strategy successful)
    Bt_StrategyResult := 1
    if m > 50: Bt_StrategyResult := 2
    if Unit_Cur = 28: N_Cream_Tactics := N_Cream_Tactics + 1      # line choice: >10 alternative
    Msg(strategist's line — fixed per unit id 2,5,6,10,12,13,14,15,16,20,22,28,34,35,36,37;
        generic otherwise)
    Msg("battle effectiveness +10 %" or "+50 %")
side 2 (FIGT作.ADV:348-388):
    d := Tmp0022 - Tmp0021
    RND(100) [R4]
    if d < RetVal: return
    m := d - RetVal
    Cmdr_Cur := Bt_EnemyStrategist
    Msg(enemy strategy successful)
    Bt_StrategyResult := 11
    if m > 50: Bt_StrategyResult := 12
    Msg(line for cmdr 1, 4, 8, 22, 45; generic otherwise); Msg("-10 %" or "-50 %")
```

Success probability = `min(d, 100) / 100`; great success iff `RetVal < d - 50`.
`Bt_StrategyResult` is consumed only by the resolution (troop damage ±10 % / ±50 %,
`FIGT.ADV:2294-2317`) and the round header display. No strategy in dungeon battles
(`Bt_Kind = 3`); duels do have it.

`FIGT作` changes `Unit_Cur` / `Cmdr_Cur` (the caller redraws afterwards; harmless).

## 5. Round start (`L_00295`, `FIGT.ADV:105-313`)

```
ROUND_START:
if Bt_EnemyRetreatMode = 3 and Bt_EnemyLosses * 2 > Bt_EnemyTroopsStart: goto ENEMY_RETREATS
if Bt_EnemyRetreatMode = 4 and Bt_EnemyLosses * 4 > Bt_EnemyTroopsStart: goto ENEMY_RETREATS
# header box (FIGT.ADV:112-214)
if Bt_IsDuel = 1 and Bt_Kind ≠ 3: Bt_TroopLimit := 5000       # state write inside the header (Q-6)
Show(header: kind "Duel"/"Offensive Battle"/"Defensive Battle"/"Dungeon Battle",
     limit label "Plains"/"Duel"/"City"/"Dungeon" with Bt_TroopLimit,
     "Advantage" numbers, "Turns Remaining: Bt_RoundsLeft" unless duel)
     # player advantage = (Bt_WallBonus if Bt_Kind = 2) + 10 / 50 for strategy 1 / 2
     # enemy  advantage = (Bt_WallBonus if Bt_Kind ∈ {1,3}) + 10 / 50 for strategy 11 / 12
InputSync()                                                     # `IK 0` (presentation)
Redraw()
if Prov_Cur = 82:                                               # Land of Death (FIGT.ADV:217-281)
    for side, i in (P,1),(P,2),(P,3),(P,4),(E,1),(E,2),(E,3),(E,4):
        if i = 1 or Slot[i] ≠ 0:                                # slot 1 is not checked
            t := rec.Troops; n := t / 10
            RND(n) [R5]                                         # no draw if n ≤ 1 (returns n)
            rec.Troops := t - RetVal
            Redraw()
    Msg(corrupt atmosphere damaged all units)
    # not counted in Bt_PlayerLosses / Bt_EnemyLosses
for i in 1..4: PState[i] := 0; EState[i] := 0
for i in 1..4: if PSlot[i] = 0: PState[i] := 100
for i in 1..4: if ESlot[i] = 0: EState[i] := 100
# falls into STEP
```

## 6. Step loop and turn order (`L_007ac` / `L_007ca`, `FIGT.ADV:314-446`)

```
STEP:                                                  # L_007ac — target after every action
if Bt_EnemyRetreat = 3: goto ENEMY_RETREATS
Bt_RoundStep := 0
Bt_PlayerActSlot := 0
PICK:                                                  # L_007ca
if PSlot[1] = 0: goto PLAYER_ANNIHILATED
if ESlot[1] = 0: goto ENEMY_ANNIHILATED
if Goliath_State = 1 and Goliath_Prov = Prov_Cur: goto GOLIATH          # §8.8
Bt_RoundStep := Bt_RoundStep + 1
if Bt_RoundStep = 10: goto ROUND_END
sel := 9; low := 65535                                 # 10000 * 6 + 5535
if PState[1] = 0 and PSlot[1] ≠ 0: Unit_Cur := PSlot[1]; sel := 1; low := Unit.Troops   # no compare
for i in 2..4: if PState[i] = 0 and PSlot[i] ≠ 0:
    Unit_Cur := PSlot[i]; if Unit.Troops < low: sel := i; low := Unit.Troops
for i in 1..4: if EState[i] = 0 and ESlot[i] ≠ 0:
    Cmdr_Cur := ESlot[i]; if Cmdr.Troops < low: sel := 4 + i; low := Cmdr.Troops
if sel ∈ 1..4:
    PState[sel] := 1; Bt_PlayerActSlot := sel; Unit_Cur := PSlot[sel]
    Bt_ActFocus := PFocus[sel]; Bt_CurOrder := POrder[sel]
    goto PLAYER_TURN                                   # §8
if sel ∈ 5..8:
    EState[sel-4] := 1; Bt_EnemyActSlot := sel-4; Cmdr_Cur := ESlot[sel-4]
    goto ENEMY_TURN                                    # §8.5
goto PICK                                              # nobody left: count up to 10
```

Turn order: each round every living unit acts once, **fewest current troops first** (re-evaluated
before every action, so damage taken changes the order). Ties: lower slot first, player before
enemy. A unit with 0 troops (commander alone) acts first. Defending (state ≠ 0) counts as acted.
`Bt_RoundStep` merely counts idle passes: when nobody can act, the loop runs 9 more empty passes
and then ends the round (no effect besides the RNG-free spin).

Edge: a unit with exactly 65535 troops in slots 2-4 or on the enemy side is never selected
(strict `<` against the 65535 sentinel) — unreachable in practice.

## 7. Round end, timeout and the exits (`FIGT.ADV:447-594`)

```
ROUND_END:                                             # L_00b3c
if Bt_IsDuel = 0:
    Bt_RoundsLeft := Bt_RoundsLeft - 1
    if Bt_RoundsLeft = 0:
        Msg("the battle dragged on and it became night" / dungeon: "time ran out")
        Bt_Result := 0
        if Bt_Kind = 1: goto ATTACKER_LOST            # timeout: attacker (player) loses
        if Bt_Kind = 2: goto DEFENDER_WON             # timeout: defending player wins
        if Bt_EnemyRetreatMode = 101: Event(Gﾀﾐ, 1); goto ENEMY_ANNIHILATED   # dead: 101 never set
        AwardExp(); MapRedraw(); goto END               # dungeon timeout: Bt_Result stays 0
goto ROUND_START

PLAYER_ANNIHILATED:                                    # L_00c26
    Bt_Result := 1
    if Bt_IsDuel = 1: MapRedraw(); AwardExp(); goto END
    Msg(our army was annihilated); AwardExp(); goto END
ATTACKER_LOST:                                         # L_00c73 (timeout Kind 1, player retreat)
    Bt_Result := 1; AwardExp(); goto END
ENEMY_ANNIHILATED:                                     # L_00c82
    Bt_Result := 2
    if Bt_IsDuel = 1: MapRedraw(); AwardExp(); goto END
    Msg(the enemy was annihilated); AwardExp(); goto END
DEFENDER_WON:                                          # L_00ccf (timeout Kind 2, enemy retreat, debug "Repel")
    Bt_Result := 2; AwardExp(); goto END
ENEMY_RETREATS:                                        # L_00cde
    Redraw()
    if ESlot[1] = 0: goto ENEMY_ANNIHILATED
    Bt_Result := 2
    if Bt_IsDuel = 1: MapRedraw(); AwardExp(); goto END
    Msg(the enemy army has retreated); goto DEFENDER_WON
PLAYER_RETREAT:                                        # L_00d3f (command 6)
    if Bt_IsDuel = 1: Msg(can't run from this battle); goto PLAYER_TURN   # turn not consumed
    if Bt_Kind = 3:  Msg(impossible to run in a dungeon); goto PLAYER_TURN
    Menu["Retreat" → RETREAT_CONFIRMED, "Cease Retreat" → PLAYER_TURN]   (cancel → PLAYER_TURN)
RETREAT_CONFIRMED:                                     # L_00dd3
    Bt_Result := 1; Msg(our army retreated); goto ATTACKER_LOST
END:                                                   # L_00dfd
    return from FIGT.ADV
```

`MapRedraw()` = `L_02d16` (strategic map or dungeon screen; presentation). Note the non-duel
annihilation paths do not call it.

**Bt_Result** codes: `1` player lost (annihilated, retreated, or timed out as attacker), `2` player
won (enemy annihilated, enemy retreated, or timed out as defender), `0` dungeon timeout only.
The duel never times out (`Bt_RoundsLeft` is not decremented), so a duel ends only by
annihilation or retreat rule.

**Enemy retreat rule** (`Bt_EnemyRetreatMode`; values assigned anywhere: 0, 1, 2, 3, 4, 5, 6;
FIGT writes 20 and 50):

| Mode | Effect |
|---|---|
| 0, 1 | fight to the end |
| 2 | when any enemy commander dies → mode 20 → retreat after the current exchange |
| 3 | at round start if `Bt_EnemyLosses * 2 > Bt_EnemyTroopsStart`; **also** on any commander death (→ 20) |
| 4 | at round start if `Bt_EnemyLosses * 4 > Bt_EnemyTroopsStart`; **also** on any commander death (→ 20) |
| 5 | when an enemy commander takes HP damage from the player (`Bt_PTroopVsCmdrDmg` or `Bt_PCmdrVsCmdrDmg` ≠ 0, in either side's exchange) → 50 → retreat after the exchange |
| 6 | battle lasts 3 rounds instead of 4-5 |
| 20, 50 | runtime: `Bt_EnemyRetreat := 3` after the next normal exchange (§9.6); retreat is taken at the next `STEP` |
| 101 | tested at timeout (`Gﾀﾐ` event, then victory) but never assigned — dead |

`Bt_EnemyLosses` = enemy troops killed + 1 per commander killed (§9.3). An enemy retreat when
`ESlot[1] = 0` is reported as annihilation (same `Bt_Result`).

## 8. Player turn

### 8.1 Command menu entry (`L_00e0d`, `FIGT.ADV:595-640`)

```
PLAYER_TURN:                                           # L_00e0d
Highlight(Bt_PlayerActSlot)
Bt_SpecialAtk := 0
LoadButtonRects()                                      # UI: 8 slot rectangles from L_0512e
if Bt_CurOrder ≠ 0: goto AUTO_ACTION                   # §8.4
Bt_PlayerAtkType := 0
if Unit.Class = 1: Bt_PlayerAtkType := 1               # bow
if Unit.Class ∈ {2, 3, 11}: Bt_PlayerAtkType := 2      # magic
SELECT:                                                # L_00ee3
VAR0617 := Bt_PlayerActSlot                            # write-only
(RetVal, Tmp0001) := FIGT_SEL()                        # §8.2
Bt_EnemyActSlot := Tmp0001 - 3                         # saturating; meaningful only for attacks
if Bt_EnemyActSlot ∈ 1..4: Cmdr_Cur := ESlot[Bt_EnemyActSlot]
DISPATCH:                                              # L_00f55 — see table §8.3
```

`Bt_PlayerAtkType` is derived from the class only (class 0, 4, 10 → melee 0).

### 8.2 `FIGT_SEL.ADV` — target and command selection

Returns `RetVal` = command code, `Tmp0001` = argument. All of it is input handling; the rules:

```
Buf_Val[0..99] := 0; Buf_Val[1] := (ActSlot ≤ 2); Buf_Val[2] := (AtkType = 1);
Buf_Val[3] := (AtkType = 2); Buf_Val[4] := 1; Buf_Val[5] := 1; Buf_Val[6] := (IsDuel = 0)
    # write-only: never read (Q-7)
Bt_PlayerSlotsUsed := index of the last nonzero PSlot (0 if none); same for Bt_EnemySlotsUsed
if either is 0: return RetVal 0                        # FIGT then shows an error and re-enters
mode := Bt_PlayerAtkType + 1                           # 1 direct, 2 bow, 3 magic; 10+ special
```

Screen: the 4 player panels (buttons 0-3) and the enemy panels (buttons 4-7). Prompt: "Attack who?
[mode name]", or "What will you do? (Right-Click)" when the unit is melee (`AtkType = 0`) in the
back row.

Target validity (`FIGT_SEL.ADV:163-171`) — a hovered enemy button is **invalid** when
`(button > 5 i.e. enemy slot 3-4) or (Bt_PlayerActSlot > 2)` **and**
`(mode = 1) or (mode > 10 and Bt_PlayerAtkType = 0)`. So direct attacks and the specials of
melee units only go from the front row to the enemy front row; bow, magic, Cheater Beam (10) and
specials of bow/magic units can target any slot from any slot.

Input handling:

| Input | Result |
|---|---|
| Left click on own panel (button 0..PlayerSlotsUsed-1) | `RetVal 53`, `Tmp0001` = slot (1-4) → unit info |
| Left click on a valid enemy panel | `RetVal` = mode, `Tmp0001` = button (4-7) → attack that enemy slot (`Tmp0001 - 3`) |
| Left click on an invalid enemy panel / elsewhere | ignored |
| Right click on own panel | slot popup: "Manual" (50), "Auto-Atk" (51), "Auto-Def" (52), "Info" (53); `Tmp0001` = slot 1-4; right-click again cancels |
| Right click elsewhere | command popup (below); `Tmp0001` = button under the cursor or 99 |

Command popup items, in display order (`FIGT_SEL.ADV:522-697`, then `:241-278`):

| Item | Condition | Effect |
|---|---|---|
| Direct | mode ≠ 1 | mode := 1, back to targeting |
| Bow | mode ≠ 2 and `AtkType = 1` | mode := 2 |
| Magic | mode ≠ 3 and `AtkType = 2` | mode := 3 |
| Capsule | `Unit.Item = 27` (C. Kaiju B) | return 30 (Amoeba), no target |
| Capsule | `Unit.Item = 26` (C. Kaiju A) | return 31 (Jagi Jagi), no target |
| Fog Card | `Bt_IsDuel = 0` and `Unit.Item = 28` | return 32, no target |
| — specials below only if `mode ≤ 3` and `Bt_ActFocus > 0` — | | |
| Rance Attack | slot 1-2 and (`Unit_Cur = 1` (Rance) or (`Unit_Cur = 31` (Kentarou) and `F_Kentarou_State > 7`)) | mode := 11 |
| By la Way | slot 1-2 and `Unit_Cur = 7` (Rick) | mode := 12 |
| High Speed | `Unit_Cur = 30` (Meglass) | mode := 14 |
| WD Beam | `Unit_Cur = 17` (Shizuka) or `39` (Magic) | mode := 13 |
| BD Beam | `Unit_Cur = 38` (Chizuko) or `40` (Nagi) | mode := 17 |
| Vanish Bomb | `Unit_Cur = 32` (Miki) and `Bt_ActFocus > 2` | mode := 15 |
| W. Shot | `Unit_Cur = 27` (Isoroku) | mode := 18 |
| CW Dance | `Unit_Cur = 18` (Patton) | mode := 16 |
| Chuhai | `Unit_Cur = 11` (Asuka) and `F_Asuka_State > 7` | mode := 19 |
| EPO Light | `Unit_Cur = 37` (Gandhi) and `AtkType = 2` and `Bt_ActFocus > 0` | mode := 21 |
| T2S Wave | `Unit_Cur = 47` (Rolex) and `AtkType = 0` and `Bt_ActFocus > 0` | mode := 20 |
| Cheat | `DebugMode = 1` | mode := 10 (Cheater Beam) |
| Defend | always | return 4 |
| Wait | always | return 5 (= Focus) |
| Auto-Atk | always | return 8 |
| Auto-Def | always | return 9 |
| Info | the cursor was over a panel at the right click (`Tmp0002 ≠ 99`) | return 54, `Tmp0001 := Tmp0002 + 1 - 4` (enemy slot) |
| Retreat | always (also in duels/dungeons; FIGT refuses there) | return 6 |
| Repel | `DebugMode = 1` | return 7 (instant victory) |

Choosing a special or an attack mode returns to targeting; the attack is committed by clicking a
valid target. Once a special mode is chosen (mode > 3) the specials are no longer listed.

### 8.3 Dispatch (`L_00f55`, `FIGT.ADV:641-775`) and the actions

| RetVal | Label | Action | Focus cost | `Bt_SpecialAtk` | Player / enemy action | `Bt_SkillSfx` |
|---|---|---|---|---|---|---|
| 0 | `L_01dae` | error box, re-enter `PLAYER_TURN` | | | | |
| 1 | `L_02339` | Direct attack | 0 | 0 | 0 / 0 | 0 |
| 2 | `L_0234d` | Bow attack | 0 | 0 | 1 / 99 | 0 |
| 3 | `L_02362` | Magic attack | 0 | 0 | 2 / 99 | 0 |
| 4 | `L_02078` | Defend | | | | |
| 5 | `L_01e34` | Focus ("Wait") | | | | |
| 6 | `L_00d3f` | Retreat (§7) | | | | |
| 7 | `L_00ccf` | debug Repel: `Bt_Result := 2`, EXP, end | | | | |
| 8 | `L_01f08` | set own order 1 (keep attacking), act now | | | | |
| 9 | `L_01f42` | set own order 2 (keep defending), act now | | | | |
| 10 | `L_024f8` | Cheater Beam (debug) | 0 | 100 | 0 / 99 | 9 |
| 11 | `L_02377` | Rance Attack | 1 | 1 | 0 / 99 | 1 |
| 12 | `L_023ab` | By la Way | 1 | 1 | 0 / 99 | 2 |
| 13, 17, 21 | `L_023fa` | White / Black Destruction Beam, EPO Light | **2** | 2 | 2 / 99 | 4 |
| 14, 18 | `L_0243d` | High Speed, Whirlwind Shot | 1 | 3 | 0 / 99 | 5 |
| 15 | `L_0249c` | Vanish Bomb | **3** | 4 | 2 / 99 | 7 |
| 16 | `L_024d9` | C. War Dance | 1 | 5 | 0 / 99 | 8 |
| 19 | `L_0246e` | Chuhai | 1 | 6 | 2 / 99 | 6 |
| 20 | `L_023dc` | T2 Soaring Wave | 1 | 7 | 0 / **0** | 3 |
| 30 | `L_01b68` | summon Amoeba (§8.6) | | | | |
| 31 | `L_01c53` | summon Jagi Jagi (§8.6) | | | | |
| 32 | `L_01d41` | Fog Card (§8.7) | | | | |
| 50/51/52 | `L_0129e` | set slot `Tmp0001`'s order to 0/1/2 | | | | |
| 53 | `L_013e0` | show own unit info for slot `Tmp0001`, then `SELECT` | | | | |
| 54 | `L_01878` | show enemy info for slot `Tmp0001`, then `SELECT` | | | | |
| other | | "FIGT command error", re-enter `PLAYER_TURN` | | | | |

Focus cost = number of `L_01dd3` calls: `PFocus[Bt_PlayerActSlot] := PFocus[…] - 1` each,
saturating. The beams need only `Bt_ActFocus > 0` to be listed but cost 2 (focus 1 → 0) (Q-8).

The attack setters (codes 1-3, 10-21) and everything from here on are specified in
[02 §2.1](02-resolution.md#21-manual-command-setters-figt_sel-result--label) (setters,
`ManualAttackPrelude`) and 02 §1.1 (`Resolve`, `ResolveMagic`); summary:

```
ManualAttackPrelude:                                   # L_02528 (FIGT.ADV:1586)
RND(10) [R6]
if RetVal = 1 and Unit_Cur = 32:                       # Miki: 10 % any manual attack becomes Vanish Bomb
    ConsumeFocus ×3; Bt_SpecialAtk := 4; PlayerAction := 2; EnemyAction := 99; Bt_SkillSfx := 7
    goto ManualAttackPrelude                           # draws RND(10) again — may repeat (Q-9)
Highlight(enemy Bt_EnemyActSlot)
Bt_MsgMode := 3
if Bt_PlayerAction ∈ {0, 1}: goto Resolve
if Bt_PlayerAction = 2:      goto ResolveMagic
ResolveMagic:  Bt_MsgMode := 1; <body of Resolve>      # L_025a0 (FIGT.ADV:1605)
Resolve:                                               # L_025d7 (FIGT.ADV:1615)
    PresentEngagement()   # L_02609 (§9.1)
    ComputeExchange()     # L_035fb (§9.2, black box)
    ApplyExchange()       # L_02d45 (§9.3)
    if Bt_EnemyRetreatMode ∈ {20, 50}: Bt_EnemyRetreat := 3
    Redraw(); goto STEP
```

Defend (`L_02078`):
```
RND(20) [R7]; v := RetVal + 1                          # 2..21
PState[Bt_PlayerActSlot] := v
Msg(<unit> prepared; Defense Increase: v); Redraw(); goto STEP
```
The value stays until the next round start; the resolution subtracts it from enemy hit chances
against this slot.

Focus (`L_01e34`):
```
if Unit_Cur = 32:                                      # Miki
    RND(2) [R8]
    if RetVal = 1: Msg(couldn't concentrate); Redraw(); goto STEP      # no focus gained
PFocus[Bt_PlayerActSlot] := PFocus[…] + 1
Msg(<unit> is focusing); Redraw(); goto STEP
```

Set order on another slot (`L_0129e`, codes 50/51/52, `Tmp0001` = slot 1-4):
```
POrder[Tmp0001] := RetVal - 50
u := PSlot[Tmp0001]
if u = Unit_Cur:                                       # the acting unit itself
    if RetVal = 51: RetVal := 8; goto DISPATCH         # → act with "keep attacking" now
    if RetVal = 52: RetVal := 9; goto DISPATCH
Msg(<u> received orders: wait for instructions / keep attacking / keep defending)
WaitRelease(); goto SELECT                             # the turn is not consumed
```

Info screens (`L_013e0`, `L_01878`) are display only; they save/restore `Unit_Cur`/`Cmdr_Cur`
via `Tmp0020`. Unit info shows order, LV, HP, commander atk/def (if no troops) or troops, troop
atk/def with the modifier of the current `Bt_Kind`, magic, attacks, and "Surging with power" if
the unit's `Unit_Partner` is in a slot.

### 8.4 Standing orders (`L_01f08`, `L_01f42`, `L_01f7c`, `L_01fd5`)

```
code 8:  Bt_CurOrder := 1; Msg(continually attack); POrder[Bt_PlayerActSlot] := 1; goto AUTO_ACTION
code 9:  Bt_CurOrder := 2; Msg(continually defend);  POrder[Bt_PlayerActSlot] := 2; goto AUTO_ACTION
AUTO_ACTION:                                           # L_01fd5; also entered from PLAYER_TURN
if Bt_CurOrder = 2: goto DEFEND
Bt_EnemyActSlot := 1; Cmdr_Cur := ESlot[1]             # always the enemy in slot 1
if Unit.Class ∈ {0, 10} and Bt_PlayerActSlot < 3:
    Bt_MsgMode := 3; EnemyAction := 0;  PlayerAction := 0; goto Resolve
if Unit.Class = 1:
    Bt_MsgMode := 3; EnemyAction := 99; PlayerAction := 1; goto Resolve
if Unit.Class ∈ {2, 11, 3}:
    Bt_MsgMode := 1; EnemyAction := 99; PlayerAction := 2; goto ResolveMagic
goto DEFEND                                            # class 4, or melee in the back row
```

Auto attack does **not** go through `ManualAttackPrelude`: no `RND(10)` Miki roll, no target highlight,
`Bt_SkillSfx` keeps its previous value (presentation only), and the target is always enemy slot 1
regardless of player choice. Orders persist across rounds and move with the unit on compaction.
"Manual" (order 0) is set only through the slot popup.

### 8.5 Enemy turn — in-battle AI (`L_02112`, `FIGT.ADV:1411-1491`)

```
ENEMY_TURN:
Bt_SpecialAtk := 0
Highlight(enemy Bt_EnemyActSlot); Pause(15)
RetVal := 1
if PSlot[2] ≠ 0: RND(2) [R9]
if PSlot[3] ≠ 0: RND(3) [R10]
if PSlot[4] ≠ 0: RND(4) [R11]                          # only the last draw counts
if Cmdr.Class ∈ {0, 10} and RetVal > 2: RND(2) [R12]    # melee re-rolls a back-row target
t := RetVal
Unit_Cur := PSlot[t]; Bt_PlayerActSlot := t
Highlight(t)
Bt_MsgMode := 4
RND(3) [R13]
if RetVal = 1 and Cmdr.Troops < Cmdr.MaxTroops / 5 and Cmdr.Class < 9:
    EState[Bt_EnemyActSlot] := 20                      # defend (value 20 lowers player hit chance)
    Redraw(); goto STEP
if Cmdr.Class ∈ {0, 10} and Bt_EnemyActSlot > 2: Redraw(); goto STEP      # back-row melee idles
if Cmdr.Class ∈ {0, 10}:
    EnemyAction := 0; PlayerAction := 0; goto Resolve
if Cmdr.Class = 1 and (Bt_EnemyActSlot > 2 or Bt_PlayerActSlot > 2):
    EnemyAction := 1; PlayerAction := 99; goto Resolve                # indirect, no counter
if Cmdr.Class = 1:
    EnemyAction := 0; PlayerAction := 0; goto Resolve                 # archers melee in front
if Cmdr.Class ∈ {2, 11}:
    EnemyAction := 2; PlayerAction := 99; goto ResolveMagic
Redraw(); goto STEP                                    # any other class (3, 4, 5 walls, …) never acts
```

Target distribution: uniform over the occupied player slots, except melee classes, whose slot 3/4
picks are re-rolled to 1/2 (with 4 slots: P(1) = P(2) = 1/2). Defend chance: 1/3 when troops are
below a fifth of max and class < 9 (demons never defend). In an enemy-initiated exchange the
targeted player unit is `Unit_Cur` and its `PlayerAction` is the counter (0 = counter directly,
99 = none). `Bt_MsgMode` is 4 for physical attacks; `ResolveMagic` overwrites it with 1 and
FIGT_M then turns it into 2 (§10). Same table in [02 §2.3](02-resolution.md#23-enemy-initiative-l_02112-1411-1488--for-reference).

### 8.6 Capsule kaiju (`L_01b68` Amoeba, `L_01c53` Jagi Jagi)

The acting unit hands its turn to a temporary unit 51; the command menu re-opens for it.

```
Unit_Cur := 51
name := "Amoeba" / "Jagi Jagi"; type string := blank
CgFull = CgPartial = CgAlone := 1161 / 1158; FaceCg := 0; Status := 1
Troops := 0; MaxTroops := 0; TroopAtk := 0; TroopDef := 0
OffAtkMod = OffDefMod = DefAtkMod = DefDefMod = DgnAtkMod = DgnDefMod := 0
PortraitCg := 0; Class := 0; Recovery := 0; DeployCost := 0
Atk := 20 / 15;  Def := 10 / 13
Attacks := Kaiju_AmoebaAttacks / Kaiju_JagiJagiAttacks
Kaiju_AmoebaAttacks := 100 / Kaiju_JagiJagiAttacks := 80        # reset on summon
HP := 18; MaxHP := 18; Strategy := 0; Magic := 0 / 8; Item := 0
CurAtkMod := 0; CurDefMod := 0; Partner := 2; Exp := 0; Level := 0
goto PLAYER_TURN
```

The capsule item is **not consumed** (it stays on the summoner; `Item := 0` is unit 51's). The
kaiju is melee (class 0) and acts from the summoner's slot, so in the back row it can only
defend/focus/order/retreat; its focus, state and order writes go to the summoner's slot.
`Unit_Partner = 2`: if Sill (unit 2) is deployed, the kaiju gets the partner bonus rolls.
Its death is ignored (`Unit_Cur = 51` is excluded from death handling).

### 8.7 Fog Card (`L_01d41`)

```
Bt_RoundsLeft := 0
Unit.Item := 0                                         # unequipped from the acting unit
Item_Cur := 28; Item.Holder := 0                       # item lost
FogCard_Price := FogCard_Price + FogCard_Price / 10
Msg(fog covered the battlefield); Redraw(); goto STEP
```

The round continues; at its end `Bt_RoundsLeft - 1` saturates to 0 → timeout: an attacking
player loses (`Bt_Result 1`), a defending player wins (2). Not offered in duels.

### 8.8 Goliath (`L_01abd`)

Checked at every `PICK` while `Goliath_State = 1` and `Goliath_Prov = Prov_Cur` (i.e. defending the
province where the T2 Goliath stands), so it fires once, before the first unit acts.

```
Goliath_State := 2
Unit_Cur := 51; name "T2 Goliath"; CgFull/Partial/Alone := 0; FaceCg := 0; Status := 1
Troops := 0; MaxTroops := 0; HP := 0; MaxHP := 0       # other unit-51 fields left as they were
Bt_PlayerActSlot := 1; Bt_EnemyActSlot := 1; Cmdr_Cur := ESlot[1]
Bt_SpecialAtk := 10; PlayerAction := 0; EnemyAction := 99
PresentEngagement(); ComputeExchange(); ApplyExchange(); Redraw(); goto STEP
```

`Bt_MsgMode` is not set (stale from the previous exchange/battle; Q-10). The retreat check after
the exchange (20/50 → `Bt_EnemyRetreat`) is skipped on this path. Goliath does not consume a
slot's turn. In the resolution, special 10 = shots `Cmdr.Troops`, kills `Cmdr.Troops / 2`
([02 §3.4](02-resolution.md#34-automatic-specials)).

## 9. The exchange: call interface

Counterpart: [02 §1 Call interface](02-resolution.md#1-call-interface-black-box-for-the-flow-spec)
(entry labels, inputs, outputs) and [02 §7 ApplyExchange](02-resolution.md#7-applyexchange-l_02d45-1873-1971).
An exchange is always `PresentEngagement` → `ComputeExchange` → `ApplyExchange` (entered through
`Resolve` / `ResolveMagic`, or directly by Goliath §8.8 and the ninja §9.5) with these variables set:

| Input | Meaning |
|---|---|
| `Unit_Cur` | player unit in the exchange (attacker or target) |
| `Cmdr_Cur` | enemy commander in the exchange |
| `Bt_PlayerActSlot`, `Bt_EnemyActSlot` | their slots (select `PState`/`EState` → hit-chance defence) |
| `Bt_PlayerAction`, `Bt_EnemyAction` | 0 direct, 1 indirect, 2 magic, 99 none/defend |
| `Bt_SpecialAtk` | 0 normal, 1-7, 10 Goliath, 100 Cheater Beam, 3 also ninja |
| `Bt_MsgMode` | flavour-text selector: 1 player magic (also enemy magic on entry), 2 enemy magic (written by FIGT_M), 3 player physical, 4 enemy physical |
| `Bt_SkillSfx` | sound set of a special (presentation; reset to 0 by FIGTATTK) |
| `Bt_Kind`, `Bt_TroopLimit`, `Bt_WallBonus`, `Bt_StrategyResult` | battle context |
| `Unit_CurAtkMod`/`Unit_CurDefMod` | set by `PresentEngagement` from `Bt_Kind` |
| `PSlot[]` | partner bonus check |
| `Unit.Item` | → `Item_Cur` in `ComputeExchange` |
| unit record 51 | stale fields read when `Unit_Cur = 51` (02 §1.4) |

### 9.1 `PresentEngagement` (`L_02609`, `FIGT.ADV:1624-1829`)

Full spec: [02 §4](02-resolution.md#4-presentengagement-l_02609-1624-1829).

```
Show(exchange screen: background, enemy sprite by Cmdr.Troops vs Bt_TroopLimit,
     player sprite by Unit.Troops vs Bt_TroopLimit or the special-attack CG of the unit, faces)
if Bt_Kind = 1: Unit.CurAtkMod := Unit.OffAtkMod; Unit.CurDefMod := Unit.OffDefMod
if Bt_Kind = 2: Unit.CurAtkMod := Unit.DefAtkMod; Unit.CurDefMod := Unit.DefDefMod
if Bt_Kind = 3: Unit.CurAtkMod := Unit.DgnAtkMod; Unit.CurDefMod := Unit.DgnDefMod
Show("Special Attack" or the two action names Direct/Indirect/Magic/Defense)
if StoryStage = 4: Tmp0229 := Cmdr_Cur; Cmdr_Cur := 200      # flavour text uses cmdr 200
call FIGT_M.ADV                                               # §10: RNG + some state writes
if StoryStage = 4: Cmdr_Cur := Tmp0229
Show(troop / HP counters scrolling in)
```

### 9.2 `ComputeExchange` (`L_035fb`) — black box

Specified in [02 §6](02-resolution.md#6-computeexchange-l_035fb-2200-2449-and-helpers)
(specials: 02 §3.3, step 9). Contract:

- **Reads**: the inputs above, `Unit.*`, `Cmdr.*`, `Item.Effect/Amount` of `Unit.Item`,
  `PState[Bt_PlayerActSlot]`, `EState[Bt_EnemyActSlot]`.
- **Writes (outputs)**: the 16 exchange results `Bt_PTroopVsCmdrShots/Dmg`,
  `Bt_PTroopVsTroopShots/Dmg`, `Bt_PCmdrVsTroopShots/Dmg`, `Bt_PCmdrVsCmdrShots/Dmg`,
  `Bt_ETroopVsCmdrShots/Dmg`, `Bt_ETroopVsTroopShots/Dmg`, `Bt_ECmdrVsTroopShots/Dmg`,
  `Bt_ECmdrVsCmdrShots/Dmg` (all reset to 0 at entry).
- **Clobbers**: `Item_Cur`, `Bt_PlayerAllEngaged`, `Bt_EnemyAllEngaged`, `Bt_PlayerEngaged`,
  `Bt_EnemyEngaged`, `Bt_PlayerSlotDef`, `Bt_EnemySlotDef`, `Bt_HitChance`,
  `Bt_PlayerCmdrShield`, `Bt_EnemyCmdrShield`, `Bt_RoundStep` (used as a temp in `L_03502` —
  harmless, `STEP` resets it), `CallArg`, `Tmp0001-0004`, `RetVal`.
- **RNG**: yes, many draws — 02 §8 rows 5-17; whole-battle order in [03](03-rng-order.md).
- Does **not** modify any record.

### 9.3 `ApplyExchange` (`L_02d45`, `FIGT.ADV:1873-1971`)

Same algorithm as [02 §7.1](02-resolution.md#71-algorithm); this file adds the cursor hazard.

```
call FIGTATTK.ADV            # animation of the 16 results; draws RND(2) once (02 §7.2), SFX
Bt_PlayerLosses := Bt_PlayerLosses + Bt_ETroopVsTroopDmg
Bt_PlayerLosses := Bt_PlayerLosses + Bt_ECmdrVsTroopDmg
Unit.HP     := Unit.HP     - Bt_ETroopVsCmdrDmg
Unit.Troops := Unit.Troops - Bt_ETroopVsTroopDmg
Unit.Troops := Unit.Troops - Bt_ECmdrVsTroopDmg
Unit.HP     := Unit.HP     - Bt_ECmdrVsCmdrDmg
Bt_EnemyLosses := Bt_EnemyLosses + Bt_PTroopVsTroopDmg
Bt_EnemyLosses := Bt_EnemyLosses + Bt_PCmdrVsTroopDmg
Cmdr.HP := Cmdr.HP - Bt_PTroopVsCmdrDmg
if Bt_PTroopVsCmdrDmg ≠ 0 and Bt_EnemyRetreatMode = 5: Bt_EnemyRetreatMode := 50
Cmdr.Troops := Cmdr.Troops - Bt_PTroopVsTroopDmg
Cmdr.Troops := Cmdr.Troops - Bt_PCmdrVsTroopDmg
Cmdr.HP := Cmdr.HP - Bt_PCmdrVsCmdrDmg
if Bt_PCmdrVsCmdrDmg ≠ 0 and Bt_EnemyRetreatMode = 5: Bt_EnemyRetreatMode := 50
if Unit.HP = 0 and Unit_Cur ≠ 51:                              # player commander falls
    Unit.Status := 0                                           # permanently out of the army
    Bt_PlayerLosses := Bt_PlayerLosses + 1
    call FIGTﾐｶDE.ADV                                          # §10; Rance → game over
    Redraw(); Pause(10)
    #   !! Redraw leaves Unit_Cur = last occupied PSlot, Cmdr_Cur = last occupied ESlot (Q-27)
    CompactPlayer(Bt_PlayerActSlot)                            # §9.4
if Cmdr.HP = 0:                                                # enemy commander falls
                                                               # (after a player death: the LAST enemy slot's record)
    if Bt_EnemyRetreatMode ∈ {2, 3, 4}: Bt_EnemyRetreatMode := 20
    Cmdr.Status := 0
    Bt_EnemyLosses := Bt_EnemyLosses + 1
    call FIGTﾃｷDE.ADV                                          # text only
    Redraw(); Pause(10)
    CompactEnemy(Bt_EnemyActSlot)
    return                                                     # no ninja roll
RND(3) [R14]
if Unit.HP ≠ 0 and Cmdr.HP > 1 and RetVal = 1 and Bt_MsgMode ≠ 4 and Bt_MsgMode ≠ 2:
    NinjaFollowUp()                                            # §9.5
return
```

**Cursor hazard (Q-27).** When the player commander dies, the `Redraw()` inside that branch moves
both record cursors before the enemy-death test. Consequences a faithful port must keep:
(a) if the exchange's enemy commander also reached HP 0 but is not in the last occupied enemy slot,
it is **not** processed now — it stays in its slot with HP 0 and `Status ≠ 0`, keeps taking turns,
and is processed (Status 0, losses +1, FIGTﾃｷDE, compaction) at the end of the next exchange it is
part of (HP 0 − 0 = 0 still triggers); if it was in the last slot it is processed normally;
(b) the R14 ninja test then uses the unit in the last occupied player slot (possibly the dead one,
HP 0 → no ninja) and the last enemy; a ninja can thus follow up for another unit, against another
enemy, while `Bt_EnemyActSlot` still names the original slot.

Both deaths can happen in the same exchange. Losses and HP are written straight into the
persistent records (that **is** the write-back: no copy is made at battle start). Troops can
reach 0 without the unit dying; only HP 0 removes a commander.

### 9.4 Slot compaction (`L_04762`, `L_04864`) and the `Redraw` side effect

```
CompactPlayer(k):  for j in k..3: PSlot[j] := PSlot[j+1]; PState[j] := PState[j+1];
                                  POrder[j] := POrder[j+1]; PFocus[j] := PFocus[j+1]
                   PSlot[4] := 0; PState[4] := 0; PFocus[4] := 0
                   if k = 4: POrder[4] := 0          # for k < 4 POrder[4] keeps its value (harmless)
CompactEnemy(k):   same for ESlot / EState (no order/focus); ESlot[4] := 0; EState[4] := 0
```

Units keep their acted/defend state when they move up. An emptied slot 4 gets state 0 (not 100);
harmless because selection also requires a nonzero slot.

`Redraw()` (`L_03058` → `L_030f0` → `L_03236` for each occupied slot) sets, **for every drawn
player unit**, `Unit.CurAtkMod/CurDefMod` from `Bt_Kind` exactly as in `PresentEngagement`
(`FIGT.ADV:2103-2114`), and leaves `Unit_Cur`/`Cmdr_Cur` pointing at the last drawn
player/enemy record; it also overwrites `Tmp0021-0024` with panel rectangles and `RetVal`
(`L_03236`, `FIGT.ADV:2075`; so does `Highlight()`). Every consumer
re-selects its record before use **except** the post-death tests in `ApplyExchange` (Q-27), and the
strategy re-test (Q-28) depends on the `Tmp` clobbering.

### 9.5 Ninja follow-up (`FIGT.ADV:1916-1986`)

After an exchange in which neither side died, the player unit is alive, the enemy has HP > 1, and
the exchange was player-initiated (`Bt_MsgMode` 1 or 3):

| Acting unit | Ninja | Needs |
|---|---|---|
| 10 Menad | Kanami (face 9, CG 1164) | `F_Kanami_NinjaUsed = 0` |
| 21 Hubert | Freya (face 449, CG 1176); `N_Hubert_FreyaUses += 1` | `F_Freya_NinjaUsed = 0` |
| 37 Gandhi | Kaoru (face 72, CG 1182) | `F_Kaoru_NinjaUsed = 0` |
| 39 Magic | Wichita (face 71, CG 1185) | `F_Wichita_NinjaUsed = 0` |

```
NinjaFollowUp():   (with RND(3) = 1 already drawn)
    Unit_Cur := 51; name := ninja; CGs and FaceCg as above; Status := 1
    copy the three strings; Troops := 0; MaxTroops := 0; HP := 3; MaxHP := 3
    Bt_SpecialAtk := 3; PlayerAction := 0; EnemyAction := 99
    PresentEngagement(); ComputeExchange(); ApplyExchange()
                                           # nested ApplyExchange: draws its own RND(3), never chains
    return                                 # `\0` at FIGT.ADV:1986 → back to the caller of the
                                           # outer ApplyExchange (Resolve / Goliath)
```

Same as [02 §7.3](02-resolution.md#73-ninjafollowup-l_02ff0-1917-1986).

Special 3 deals exactly 1 HP to the commander, and `Cmdr.HP > 1` is required, so **the ninja
never kills**. The weekly ninja flags are only tested, never set: a ninja can assist every
exchange (Q-11). `Unit_Cur` is left at 51 afterwards. Other unit-51 fields (class, item, level,
modifiers) are stale — the resolution's shield logic reads `Unit.Class`/`Unit.Item` (Q-12).

### 9.6 After the exchange

Back in `Resolve`/`ResolveMagic`: `if Bt_EnemyRetreatMode ∈ {20, 50}: Bt_EnemyRetreat := 3`,
`Redraw()`, `goto STEP` (which takes the retreat, then checks annihilation).

## 10. Text files that touch state or RNG

| File | Called | RNG | State writes |
|---|---|---|---|
| `FIGT_M.ADV` | once per `PresentEngagement` | **2 or 3 draws** per call, exactly as [02 §5.5](02-resolution.md#55-draw-count): `RND(2)` at `:5` (always; result overridden while `Bt_MsgMode` ∈ 1..4), `RND(3)` at `:25` (player side) or `:2825` (enemy side), then at most one branch draw (n ∈ {2,3,4,5,10}) selected by the dispatch tables 02 §5.2/§5.3 (92 `Y 4` sites in the file, all listed there) | `Bt_MsgMode := 2` if `Bt_MsgMode = 1 and Bt_EnemyAction = 2` (`:2-4`); `Tmp0621 := 0`; `Tmp0690`; `F_Satella_LineCmdr91/92/93`, `N_Freak_Stage := 1`, `F_Asuka_State := 1` (`:8326`: Mill, unit 25, in slot 1-3, or in slot 4 with `F_Mill_State < 2`, reading `*` as binding tighter than `+` — see README open question); Sieg block `:10090-10115` (`F_Sieg_State := 4`, `F_Sieg_Defeated := 1`, `N_Miki_Stage` 1→2, item 1 to Rance (unit 1), rewrites Sieg's (cmdr 93) name/CGs, changes Rance's CGs; its `Cmdr_Cur := 93` (Sieg) / `Unit_Cur := 1` (Rance) are no-ops because the block is reached only from Rance's branch with `Cmdr_Cur = 93`, `FIGT_M.ADV:8956-8960`) — full table 02 §5.4 |
| `FIGTﾐｶDE.ADV` | player commander HP 0 | none | Rance (unit 1) with `StoryStage ≠ 0`: `EndingId := 1`, jump `OVER.ADV` (game over in mid-battle); Rance (unit 1) in the prologue only "faints"; Sill (unit 2) with `StoryStage ≠ 0`: `F_Sill_Died := 1` (`FIGTﾐｶDE.ADV:42-90`); Asuka (unit 11): `F_Asuka_State := 0` |
| `FIGTﾃｷDE.ADV` | enemy commander HP 0 | none | none (text) |

Because `Bt_MsgMode` is rewritten inside FIGT_M, an enemy magic attack ends with
`Bt_MsgMode = 2`, which blocks the ninja follow-up as intended; enemy physical attacks keep 4.

## 11. End of battle

### 11.1 EXP (`AwardExp`, `L_04a2c`, `FIGT.ADV:2977-3044`)

Runs on every exit (all `Bt_Result` values, duels included).

```
RND(15) [R15]; exp := 5 + RetVal                       # 6..20
if Bt_ExpBonusNext ≠ 0:                                # set by Rebecca scenes
    RND(50) [R16]; exp := RetVal; Bt_ExpBonusNext := 0
if Bt_Kind = 3: exp := 1                               # dungeon: 1 EXP (R15/R16 still drawn)
if Bt_Result = 1: exp := exp / 3                       # lost/retreated: a third (dungeon: 0)
Show("Leazas"/"Thieves" (StoryStage 0) damages Bt_PlayerLosses, enemy damages Bt_EnemyLosses,
     "exp EXP obtained")
if Bt_GarrisonProv = Prov_Cur and Bt_EnemyLosses > 200: Bt_GarrisonHits := Bt_GarrisonHits + 1
for i in 1..4: if PSlot[i] ≠ 0: Unit_Cur := PSlot[i]; LevelUp(exp)
Hist_Cur := 0; Hist_Casualties := Hist_Casualties + Bt_PlayerLosses
```

Only units still in a slot get EXP (fallen units were compacted out). The same `exp` is given to
every survivor. `Bt_PlayerLosses` includes +1 per fallen commander.

### 11.2 Level-up (`L_04bb7`, `FIGT.ADV:3045-3160`)

```
LevelUp(exp):
Unit.Exp := Unit.Exp + exp
if Unit.Item = 29: Unit.Exp := Unit.Exp + exp          # Happiness Ring: double
if Unit.Exp < 100 + Unit.Level * 30: return
Unit.Exp := 0                                          # surplus EXP discarded; max one level per battle
Unit.Attacks := Unit.Attacks + 1
Unit.Level := Unit.Level + 1
if Unit.Recovery ≠ 0: RND(5) [R17]; Unit.Recovery := Unit.Recovery + RetVal
PlaySfx(level up); Msg(<unit> leveled up; "Leader Attacks +1")
if Unit_Cur = 25:                                      # Mill
    Unit.MaxTroops := Unit.MaxTroops + 50; Unit.Recovery := Unit.MaxTroops
    Msg(summoned 50 more phantom beasts); return
if Unit.Item = 3 and Unit.MaxTroops ≠ 0:               # Popularity Staff
    g := Unit.MaxTroops / (10 + Unit.Level * 2)        # new level
    Unit.MaxTroops := Unit.MaxTroops + g; Msg(Popularity Staff +g troops); return
RND(12 + Unit.Level) [R18]                             # new level
case RetVal:
  1: RND(3) [R19]; if RetVal - Unit.OffAtkMod ≠ 0: Unit.OffAtkMod += 1; Show("Atk on Offense")
  2: RND(3) [R19]; if RetVal - Unit.DefAtkMod ≠ 0: Unit.DefAtkMod += 1; Show("Atk on Defense")
  3: RND(3) [R19]; if RetVal - Unit.DgnAtkMod ≠ 0: Unit.DgnAtkMod += 1; Show("Atk in Dungeons")
  4: RND(3) [R19]; if RetVal - Unit.OffDefMod ≠ 0: Unit.OffDefMod += 1; Show("Def on Offense")
  5: RND(3) [R19]; if RetVal - Unit.DefDefMod ≠ 0: Unit.DefDefMod += 1; Show("Def on Defense")
  6: RND(3) [R19]; if RetVal - Unit.DgnDefMod ≠ 0: Unit.DgnDefMod += 1; Show("Def in Dungeons")
  ≥ 7: nothing
Show(" UP")                                            # shown in every case, even with no stat (Q-13)
```

`RetVal - mod ≠ 0` with saturation means `RetVal > mod`: a modifier 0 always rises, 1 rises with
2/3, 2 with 1/3, 3+ never (except the xsystem35 `n+1` edge case) and the negative encodings
11-19 never. Chance of a modifier roll = 6/(12+L). Mill and Popularity-Staff holders never roll
modifiers.

### 11.3 Wrapper post-processing

See §2.1 (FIGT_A) and §2.3 (FIGT_B). Ownership: attack victory (`StoryStage ≠ 0`) →
`Prov_Owner := 1`; defence loss or abandon → `Prov_Owner := Ev_PhaseNation`; losing Leazas
Castle (1) → game over (`EndingId 2`). Units deployed this week stay at `Unit_Status = 2` (greyed) until the
weekly reset; dead ones are 0.

### 11.4 `死亡捕獲.ADV` — death / capture outcomes

Called by FIGT_A and FIGT_B after every battle (not on "Abandon City" without a fight), with
`Prov_Cur = Bt_TargetProv`; also by the dungeon after each fight and a second time by some story
callers (Gｹｯｾﾙ, Gﾅｷﾞ:4167); not by the God-dungeon garrisons. No RNG. Helpers: `UnitIn(id)` → 0 not deployed, 1 deployed and
alive, 2 deployed and `Unit_Status = 0`; `CmdrIn(id)` → 0 / 1 / 2 likewise on `EParty[]` and
`Cmdr_Status`. Both leave the cursor on that record. In order:

| # | Lines | Condition | Effect |
|---|---|---|---|
| 1 | 1-13 | Menad (10) deployed, `Bt_Kind ≠ 3`, HP ≠ 0 | `F_Menad_State = 4` → Gﾒﾅﾄﾞ 2; `= 3` and `Troops * 3 < MaxTroops` → Gﾒﾅﾄﾞ 1 |
| 2 | 14-25 | mercenary Louis (49) / Cecil (50) deployed and dead | `F_MercA_Lost` / `F_MercB_Lost := 1` |
| 3 | 26-32 | Rance (1) `Status = 0` and `StoryStage ≠ 0` | `EndingId := 1`, game over (normally already done by FIGTﾐｶDE) |
| 4 | 33-39 | Sill (2) `Status = 0` and `StoryStage = 0` | Sill revived: `Status := 2`, `HP := 1` (CmdrIn(2), Haurein, is computed first and unused) |
| 5 | 40-44 | Somita (46) deployed and alive | `N_Somita_BattlesSurvived += 1` |
| 6 | 45-51 | Melfeis (cmdr 4) in battle and dead | Gﾒﾙﾌｪｲｽ 1 |
| 7 | 52-57 | Haurein (cmdr 2) dead | Gﾊｳﾚｰﾝ 1 |
| 8 | 58-69 | Ex (cmdr 1), Nobunaga Oda (cmdr 72) dead | revived: `Status 1, HP 1` (they escape) |
| 9 | 70-90 | Isoroku Yamamoto (cmdr 82) / Lelyukov (22) / Nero (44) dead | Gｲｿﾛｸ 1 / Gﾚﾘｭｰｺﾌ 3 / Gﾈﾛ 3 |
| 10 | 91-105 | cmdr 45 (Cream) | dead: Gｸﾘｰﾑ 2, then `F_Nero_Helman4thState := 5` unless 100; if `F_Nero_Helman4thState = 4` (whether dead or not): set 5, Gｸﾘｰﾑ 1 |
| 11 | 106-116 | Ruberan (cmdr 31) | `StoryStage = 0` → `Status := 1` always; dead and stage ≠ 0 → Gﾙｰﾍﾞﾗﾝ 2 |
| 12 | 117-123 | Aristoles (cmdr 32) dead | Gｱﾘｽﾄﾚｽ 2 |
| 13 | 124-147 | Patton (cmdr 54) dead → Gﾊﾟｯﾄﾝ 5; Hubert (55), Freak (56), Hanty (57) dead → revived HP 1 | |
| 14 | 148-168 | Katyusha (cmdr 51) / Saulnia (52) / Rolex (50) dead | Gｶﾁｭｰｼｬ 1 / Gｿﾙﾆｱ 1 / Gﾛﾚｯｸｽ 3 |
| 15 | 169-180 | cmdr 39 (Mineva) dead | revived HP 2; if `Prov_Cur = 45` (Lang Bau) and won and not duel: `Status := 0`, Gﾐﾈﾊﾞ 5 |
| 16 | 181-219 | Chizuko (cmdr 101) / Papaya (104) / Magic (110) dead | won → Gﾁｽﾞｺ 2 / Gﾊﾟﾊﾟｲｱ 4 / Gﾏｼﾞｯｸ 23; otherwise revived HP 1 |
| 17 | 220-236 | Saias (cmdr 113) dead | revived HP 1 unless `Prov_Cur = 58` (Adam's Fortress); at Adam's Fortress (58): won → Iｾﾞｽ 40, lost → `Status 2, HP 1` |
| 18 | 237-255 | Nagi (cmdr 107) dead → revived HP 1, at Nagi's Tower (67) and won: `Status 0`, Gﾅｷﾞ 6; Ragarl (cmdr 109) dead → revived with full HP | |
| 19 | 256-270 | Alex (cmdr 125) dead | Magic's Tower (66) → Gﾏｼﾞｯｸ 2, else revived HP 1; then Old Zeth (63) and won → `Status 0`, Iｾﾞｽ 41 |
| 19a | 271-304 | Kabachahn (cmdr 121) / Uspira (117) dead | as Saias (113) with home province Amber Castle (65) / Tape (61) and Iｾﾞｽ 42 / 43 |
| 20 | 305-320 | `F_Zeth_War = 1` and cmdrs Chizuko (101), Papaya (104), Nagi (107), Magic (110) all `Status 0` | Iｾﾞｽ 100 (Zeth's Four Lords defeated) — regardless of who fought |
| 21 | 321-368 | cmdrs Babolat (129), Redeye (130), Kite (132), Galtia (131), Kesselring (133) (not duel), Medusa (134), Kayblis (135) dead | Gﾊﾞﾎﾞﾗ / Gﾚｯﾄﾞｱｲ / Gｶｲﾄ / Gｶﾞﾙﾃｨｱ / Gｹｯｾﾙ / Gﾒﾃﾞｨｳｻ / Gｹｲﾌﾞﾘｽ, entry 10 |
| 22 | 369-385 | La Vaswald (cmdr 98) dead → Gｻｲｾﾞﾙ 11; Camilla (cmdr 91) dead → revived HP 2, Camilla's Castle (88) and won: `Status 0`, Gｶﾐｰﾗ 11 | |
| 23 | 386-399 | PG-7 (cmdr 136) / PG-9 (137) dead | Gﾊﾟｲｱｰﾙ 11 / 12 |
| 24 | 400-419 | Kaybwan (cmdr 159) / Kaybnyan (162) dead | `Status := 1`; won → `Status 0`, Gｹｲﾌﾞﾜﾝ 1 / Gｹｲﾌﾞﾆｬﾝ 1 |
| 25 | 420-430 | Leila (5) and Rick (7) both deployed alive, `F_LeilaRick_State < 6`, `Prov_Cur ≠ 1` (Leazas Castle), `F_Leila_State > 3`, won | Gﾚｲﾗ 2, return |
| 26 | 431-441 | Barres (6) and Haurein (12) deployed alive, Haurein `Level > 3`, `F_Haurein_Fate = 0`, won | Gﾊｳﾚｰﾝ 2, return |

Generic rule: a fallen **player** commander stays `Unit_Status = 0` (lost for good; prologue Sill
excepted); a fallen **enemy** commander stays `Cmdr_Status = 0` (out of play) unless listed above
as revived. There is no generic capture/recruit step in battle.

---

## 12. RNG draws in order

Within one battle, in execution order. The merged walkthrough of both specs is
[03-rng-order.md](03-rng-order.md). "Ex" = the draws of one exchange = 02 §8 rows 2-20, in this
order: FIGT_M (2 or 3: `RND(2)` overridden, `RND(3)`, at most one branch draw; 02 §5.5) →
`ComputeExchange` (02 §8 rows 5-17) → FIGTATTK `RND(2)` (`FIGTATTK.ADV:234`, result unused) →
`ApplyExchange`'s `RND(3)` [R14] (skipped if the enemy commander died) → if the ninja triggers,
a nested Ex (whose own R14 cannot chain). R6 (= 02 §8 row 1) precedes Ex only for manual
commands; R14 (= 02 §8 row 19) is part of Ex and listed separately only for its id — count it once.

Caller-side draws before FIGT (AI / dungeon / story specs) also precede R3: e.g. the T*2 garrison
refills, the dungeon encounter `RND(4)` / `RND(10)` / `RND(MonsterPower)`, Gﾏｼﾞｯｸ's `RND(48)` loop.

| # | Where | n | Condition | Used for |
|---|---|---|---|---|
| R1 | FIGT_B.ADV:186 | 2 | defending in the city | `VAR0692 -= 1` on 1 (write-only; effectively discarded) |
| R3 | FIGT.ADV:17 | 2 | always | rounds = 3 + r (overridden to 3 by mode 6, draw still made) |
| R4 | FIGT作.ADV:9 / :350 | 100 | `Bt_Kind ≠ 3` and the strategy values differ | success if `r ≤ d`; great if `d - r > 50` |
| R5 | FIGT.ADV:3172 (`L_04eba`) | troops/10 | Prov_Cur = 82 (Land of Death), each round start, for P1, P2-4 if present, E1, E2-4 if present, in that order; no draw when troops < 20 | troops lost |
| — per action, repeated — | | | | |
| R6 | FIGT.ADV:1587 | 10 | every manual attack/special (not auto), repeated while Miki rolls 1 | Miki's Vanish Bomb trigger (discarded for others) |
| R7 | FIGT.ADV:1383 | 20 | Defend (manual, auto-defend, or auto-attack falling back to defend) | defence bonus r+1 |
| R8 | FIGT.ADV:1281 | 2 | Focus by Miki (unit 32) | 1 = fails |
| R9-R11 | FIGT.ADV:1421/1424/1427 | 2 / 3 / 4 | enemy turn, if player slot 2 / 3 / 4 occupied | target slot (last one wins) |
| R12 | FIGT.ADV:1430 | 2 | enemy melee class and target > 2 | re-rolled target |
| R13 | FIGT.ADV:1450 | 3 | every enemy turn | 1 → may defend |
| Ex | 02 §8 rows 2-20 | | after R6 (manual attack), auto attack, R13 (enemy attacks), Goliath | exchange |
| (R14) | FIGT.ADV:1915 | 3 | inside Ex: end of every `ApplyExchange` unless an enemy commander died | ninja follow-up on 1 |
| — end — | | | | |
| R15 | FIGT.ADV:2978 | 15 | every battle end (not when Rance's death jumps to `OVER.ADV`) | EXP 5 + r |
| R16 | FIGT.ADV:2981 | 50 | `Bt_ExpBonusNext ≠ 0` | EXP = r |
| R17 | FIGT.ADV:3057 | 5 | per surviving unit (slot order) that levels up and has `Recovery ≠ 0` | Recovery + r |
| R18 | FIGT.ADV:3100 | 12+L | level-up, not Mill, not Popularity Staff with troops | which modifier |
| R19 | FIGT.ADV:3102-3147 | 3 | R18 ≤ 6 | modifier rises if r > current |
| R2 | FIGT_B.ADV:282 | 5 | after 死亡捕獲, defence won at Log B (31) .. Great Wall of Helman A (49) | Elena event on 1 (forced off if Pawn (40) not owned) |

Draws inside story events called by the wrappers / 死亡捕獲 / FIGTﾐｶDE are not listed (they belong
to those events). FIGTﾐｶDE, FIGTﾃｷDE, FIGT作 text and 死亡捕獲 draw nothing themselves.

---

## 13. Quirks a faithful port must reproduce

- **Q-1** Refusals (Gandhi/Satella/Meglass, Gurwin (unit 44)) after the party was confirmed undo
  the deployment but keep the war expenses paid in Sﾌﾞﾀｲ (`FIGT_B.ADV:84-150`, `Cﾃｷﾄｼ.ADV:226-307`).
- **Q-2** A lost attack does not clear `Ui_CmdAttackAvail` (`FIGT_A.ADV:103-107`).
- **Q-3** Every city defence permanently raises `Prov_BattleArea` by 10 % (after copying the old
  value into `Bt_TroopLimit`) (`FIGT_B.ADV:182-185`).
- **Q-4** The ninja-scout capture code `FIGT_B.ADV:673-713` is unreachable: scouting is free.
- **Q-5** "Abandon City" with Ex's last stand sets `Bt_TargetProv := 0` before the battle, so after
  it `Prov_Cur := 0` is used for 死亡捕獲 and the Elena check (`FIGT_B.ADV:720`, `:273-281`).
- **Q-6** In a duel the header code forces `Bt_TroopLimit := 5000` every round (unless dungeon).
- **Q-7** `FIGT_SEL` clears `Buf_Val[0..99]` and writes command flags nobody reads.
- **Q-8** Beam specials are listed with 1 focus but cost 2 (saturating to 0).
- **Q-9** Miki's 10 % Vanish Bomb conversion re-rolls `RND(10)` after each conversion and drains
  3 focus each time (saturating), so it can chain.
- **Q-10** Goliath's exchange runs with a stale `Bt_MsgMode` and skips the retreat-flag check.
- **Q-11** Ninja follow-up checks the weekly ninja flags but never sets them.
- **Q-12** Unit 51 is reused without a full reset (Goliath and ninja set only some fields).
- **Q-13** Level-up always prints " UP", even when no modifier rose.
- **Q-14** Enemy classes 3 and 4 (and 5, the walls) never act; class-1 archers in the front row
  vs a front-row target trade direct blows (`PlayerAction 0`).
- **Q-15** Retreat modes 3 and 4 also retreat on the first commander death (they are mapped to 20).
- **Q-16** Auto attack always targets enemy slot 1 and skips the Miki roll R6.
- **Q-17** The first player slot is taken into the turn order without a troop comparison and the
  sentinel 65535 makes a 65535-troop unit unselectable elsewhere.
- **Q-18** `Bt_EnemyActSlot := Tmp0001 - 3` is computed for every command (garbage for non-attacks).
- **Q-19** The kaiju's focus/order/defend writes land in the summoner's slot; the capsule item is
  reusable every turn.
- **Q-20** `Redraw()` overwrites `Unit_CurAtkMod/CurDefMod` of every drawn unit and moves the
  record cursors.
- **Q-21** EXP: `RND(15)` is drawn even when Rebecca's bonus or the dungeon value overrides it;
  max one level per battle (surplus discarded).
- **Q-22** In story stage 4 the flavour text is always selected as if the enemy were cmdr 200.
- **Q-23** `Bt_EnemyRetreatMode` is never reset per battle. Setup paths that exit before writing
  it (TCﾘｰｻﾞｽ2 troop sum < 100, TJAPAN2 < 10 / < 500) fight with the previous battle's value; if that
  was a runtime 20 or 50, the enemy retreats right after the first player/enemy exchange.
- **Q-24** `Bt_WallBonus` is stale on the "Use Force" path (`Cﾃｷﾄｼ.ADV:3815`) and in scripted
  FIGT_A battles that do not set it: plains vs city then depends on the previous battle.
- **Q-25** FIGT_A's victory branch sets `Prov_Owner := 1` for the current `Prov_Cur` in any
  non-duel battle with `StoryStage ≠ 0`, including scripted ones (e.g. Gｹｯｾﾙ:2959 inherits `Prov_Cur`).
- **Q-26** Dungeon boss flags are set before the boss fight, so losing still counts as beaten
  (dungeon spec; listed because it changes the battle's stakes).
- **Q-27** After a player commander dies in `ApplyExchange`, the enemy-death test and the ninja test run on
  the records left by `Redraw()` (last occupied slots), not on the exchange's records — an enemy
  killed in the same exchange can linger with HP 0 (§9.3).
- **Q-28** The two strategy branches (`FIGT.ADV:94-103`) are written as two independent `if`s; the
  second is false after the first ran only because `Redraw()` overwrites `Tmp0021/Tmp0022` with
  a panel's X/Y (448/440 vs ≤ 308). Equivalent to `else if`.

## 14. Open questions

Merged with 02's open questions in [README.md](README.md). Resolved since the first draft:

- ~~`Y 3, n` is random?~~ No: it is a timed pause (confirmed against xsystem35); `Y 4` is the
  only random source.
- ~~Does the Sieg block (`FIGT_M.ADV:10090-10115`) switch the exchange to Rance's record?~~ No:
  it is reached only from Rance's branch (`FIGT_M.ADV:8956-8960`, `Unit_Cur = 1` (Rance),
  `Cmdr_Cur = 93` (Sieg)), so its `Unit_Cur := 1` / `Cmdr_Cur := 93` are no-ops.

Still open:

1. Rance's death in the prologue (`FIGTﾐｶDE.ADV:1-21`, `StoryStage = 0`) leaves `Unit_Status = 0`
   and 死亡捕獲 does not revive him (only Sill, `死亡捕獲.ADV:36-39`); who restores him?
2. ~~`Sﾌﾞﾀｲ` return codes other than 501/502/503 fall through to the plains setup~~ — resolved:
   `Sﾌﾞﾀｲ` returns only 501, 502 (mode 2) or 503 (`Sﾌﾞﾀｲ.ADV:106-149`,
   [../strategy/survey-commands.md](../strategy/survey-commands.md)); the fall-through (`FIGT_B.ADV:160-163`) is unreachable.
3. `FIGT.ADV:1715` draws the background overlay unless `EParty[1] = 1137` — a commander id never
   exceeds 220, so the condition is always true. 1137 is Gunagan's `Cmdr_CgFull` (Cﾒｲｷｭｳ tests
   `Cmdr_CgFull = 1137`), so the intended test was probably on the CG. Presentation only.

## 15. Dictionary corrections

Applied to `variables.tsv` on 2026-10-08. Rename: VAR0641 `N_Rance_BattlesSinceSomita` →
`N_Somita_BattlesSurvived`. Rejected / not applied:

- VAR0617 (unnamed): the dictionary already says "copy of VAR0613 before FIGT_SEL, never read";
  nothing to change.
- VAR0585 `Bt_ScoutNinja` "only displays which ninja reported": too narrow — the variable is also
  the mission ninja of `Cﾆﾝｼﾞｬ.ADV` (`:93-635`), where it drives events. Applied instead as an
  addition (FIGT_B use is display only; the capture branch `FIGT_B.ADV:673-713` is unreachable).
