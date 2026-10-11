# What stage 3 reaches (`StoryStage = 3`, swept 2026-10-11)

The sweep behind milestone 4 (owner's decision 15 in [README.md](README.md)); the sequel of
[stage-2-sweep.md](stage-2-sweep.md). Lines refer to `_extract\scenario_named` (assignments read
`!Var : n!`, `\` is ≠). [I] marks an inference; everything else was read. **S** is the week whose
Kayblis Empire phase runs first: the gate attack's week (`Iｼﾚｲﾌﾞ` 6) or the week after `Iﾏｼﾞﾝ` 51.
Owners as in stage-2-sweep (8 Kayblis, 9 Hornet, 10 Angels).

**The demon lands at S.** Tower of Evil (78) through Tearing Forest (91) are all Kayblis's: `Iﾏｼﾞﾝ` 16
gave him Cascade Bau (83) (`Iﾏｼﾞﾝ:6413-6414`), 18 Land of Death (82) and Silky's Castle (81)
(`:7800-7814`), 20 Tower of Evil (78), Sulfur Forest (80), Grief Valley (89) and Demon Lord's Castle
(79) (`:8398-8444`), and both ways into stage 3 pass `Iﾏｼﾞﾝ` 20. No owner-9 or owner-10 province is
left. Adjacency (`SET_MAP` `Prov_Adj1-5`): Great Wall of Helman A (49) – Tower of Evil (78) – Demon
Lord's Castle (79); Great Wall of Helman B (50) – Grief Valley (89) – Demon Lord's Castle (79); Demon
Lord's Castle (79) – Sulfur Forest (80) – Silky's Castle (81); Silky's Castle (81) – Land of Death (82)
– Kayblis's Castle (84); Silky's Castle (81) – Cascade Bau (83) – Kesselring's Castle (87); Maginot
Stronghold (72) – Bone Forest (90) – Kesselring's Castle (87) – Red House (85) – Kayblis's Castle
(84) – Blue House (86) – Camilla's Castle (88) – Tearing Forest (91) – Maginot Stronghold (76).

## Start

- **`Iｼﾚｲﾌﾞ` 6** (`:1526-1711`): `StoryStage := 3`, `F_HQMeeting_Done := 1` (`:1527-1528`); the
  rest is text (branches on Sill (unit 2), Miki (unit 32), Kentarou (unit 31)). Back in `Cﾃｷﾄｼ` the
  attack goes on (`Sﾌﾞﾀｲ`, `Tｹｲﾌﾞﾘｽ2`, `FIGT_A`): at Tower of Evil (78) with `N_Warg_Stage = 0`
  `Gﾜｰｸﾞ` 1 replaces the battle (`Tｹｲﾌﾞﾘｽ2:92-96`, `Cﾃｷﾄｼ:356-358`), so stage 3 starts with nothing
  taken; Grief Valley (89), Bone Forest (90) and Tearing Forest (91) are fought against Monster
  Generals (commanders 86-89) in the plains (`Tｹｲﾌﾞﾘｽ2:112-123`, NAT-21). Both wars are won here
  (the gate's precondition, `Cﾃｷﾄｼ:147-153`).
- **`Iﾏｼﾞﾝ` 51** (`:8979-9195`): `F_DemonWar_Stage := 12`, `N_Majin_EventTimer := 8`, `StoryStage
  := 3`, `F_HQMeeting_Done := 1` (`:8980-8983`), at the week's end (`MAIN:937-940`, after that week's
  Kayblis gate at `:594`). Any war state; about week 103 with `--seed 2` [I, stage-2-sweep], i.e.
  past week 100, so the Assault Leaders march from S on (below).
- **Week S, the Kayblis phase** (`MAIN:594-629`, after the player phase and the JAPAN, Helman,
  Patton and Zeth phases; `Ev_PhaseNation := 8`, army name "Kayblis Unit", no commander refresh in
  `MAIN`): `Tｹｲﾌﾞﾘｽ:3-8` plays `Gｹｲﾌﾞﾘｽ` 1 (`:1393-2109`, the war council; text branches on
  `F_DemonWar_Stage = 12` at `:1434`/`:1485` and on Lei (commander 92) and Sieg (commander 93) alive,
  `:1914-2086`), which sets `F_DemonWar_Stage := 20` and `N_Camilla_Stage := 100` (`:2105-2106`), and
  jumps to the dispatch `L_004d1` — the `Ai_*` reset, the recovery and the event chain are skipped
  this once.

**Closed from S:** the Camilla Unit (`MAIN:631` needs `N_Camilla_Stage` 2-99; it runs after the
Kayblis phase, so not even in S) and the scenes it hosts (`Gｼﾞｰｸ` 1-5, `Gｻｲｾﾞﾙ` 1/2 at `:653-662`,
`Gｶﾐｰﾗ` 1/2/8, `Gﾚｲ` 1, `Gｻﾃﾗ` 1 — an `F_Seizel_State` of 1 or 2 left at stage 3 stays [I]); the demon
war (`:936-985`: 12 and 20 match none of its entries 0-11); the Kayblis-war branch (`:986`, needs
`F_Kayblis_War = 1`, which never reaches stage 3 [I]: "Rape Demon Lord" ends in `Iﾏｼﾞﾝ2` 24, ending 6,
`Iﾏｼﾞﾝ2:600-605`, before `F_DemonWar_Stage` can reach 10); the stage-2 gates `Iﾏｼﾞﾝ` 1 (`MAIN:887`),
`Iｼﾚｲﾌﾞ` 4 (`COM:192`), `Iｼﾚｲﾌﾞ` 5/6 (`Cﾃｷﾄｼ:147`), `GAL` 1 (`COM:152`), `Gｼｨﾙ2` 1 (`COM:371`),
`Gｽｽﾞﾒ` 1 (`COM:472`; its `Y 4, 5` at `:471` is still drawn).

## The week (`MAIN.ADV`, ported whole)

Open at stage 3, besides what stage 2 has:

- **Expenses:** Galtia's food −45 (`MAIN:185-202`) once Galtia (unit 41) joins — `Gｶﾞﾙﾃｨｱ` 2, called
  after the player loses a defence against Galtia's unit (`Tｹｲﾌﾞﾘｽ:1204-1207`), with
  `F_Martina_State = 6` (`Gｶﾞﾙﾃｨｱ:300-315, 1140-1144`; unit 41 to status 2, Galtia (commander 131)
  to 0). M Land and the AL Church as at stage 2.
- **The nation phases:** JAPAN (`:348`), Patton (`:454`), Mars (`:229`, `Gｶｾｲ` 2) and the AL Church
  (`:260`, `GAL` 5) have no stage test; Helman (`:401`) and Zeth (`:507`) run only on the `Iﾏｼﾞﾝ` 51
  path, where their wars may still be on [I]. All ported or scenes. **Kayblis** (`:594`, `Tｹｲﾌﾞﾘｽ`)
  is the one rule file of the week not ported. Creator (`:816`, `Tﾙﾄﾞﾗ`) needs stage 4.
- **La Vaswald** (`:848-886`, La Vaswald (commander 98) alive): `Gｻｲｾﾞﾙ` 12 every week — a random
  player province (`Gｻｲｾﾞﾙ:471-491`, `Y 4, Tmp0621` from 80 down, survey-events R3) and `FIGT_B`.
  Opened by `Gｻｲｾﾞﾙ` 9 (`COM:694-699`: `Y 4, 10` = 1 once `N_Saizel_Weeks > 40`; it also kills
  Medusa (commander 134), `Gｻｲｾﾞﾙ:2817-2824`, unless `F_Seizel_State > 4`). The counter runs from
  `Gｻｲｾﾞﾙ` 1 (stage 2, `COM:49-50`), so a slow stage 3 reaches it [I; stage-2-sweep's estimate].
- **The week's end:** the Sill chain (`:896`, `F_Helman_War ≠ 2`) and `Gﾏｼﾞｯｸ` 5 (`:926`) as at
  stage 2.

**`COM` at stage 3** (new or live; none calls a rule file). Tier 1: `Gﾜｰｸﾞ` 2 (`:96`:
`F_DemonCastle_Attacked ≠ 0` — set by `Gｹｲﾌﾞﾘｽ` 4 on the first attack on the Demon Lord's Castle
(79) —, `N_Warg_Stage = 0`, `F_Kayblis_War = 0`; Warg joins, `N_Warg_Stage := 1`,
`Gﾜｰｸﾞ:3604-3605`), `Gﾊﾞﾎﾞﾗ` 5 (`:228-233`, the pitfall), `Gﾏﾘｽ` 17 (`:284`, stage 3, Redeye met).
Tier 2: `Gﾚｯﾄﾞｱｲ` 3 (`:337`), `Gﾌﾘｰｸ` 2/3/5/6 (`:402-421`, Freak (unit 20)), `Gﾊﾞﾎﾞﾗ` 6 / `Gﾚｯﾄﾞｱｲ`
7 / `Gｶｲﾄ` 8 (`:423-441`, after Galtia is met), **`Gﾐｷ` 6** (`:566-573`: stage 3, `N_Miki_Stage = 6`,
Piar's arc running, PG-9 (commander 137) alive) and **`Gﾐｷ` 9** (`:574-578`: `N_Miki_Stage = 8`, i.e.
Miki (unit 32) left through `Gﾐｷ` 8, `COM:178-185`, `Gﾐｷ:253-256`) — both set `Ev_KayblisAssault`
(`Gﾐｷ:3162`, `:429`); `Gﾏﾘｽ` 16 (`:811-815`, Satella (unit 29) present, `F_Kesselring_State = 2`);
`Gｻｲｾﾞﾙ` 9 (`:694`). After Kayblis's death (stage 4): `Gﾐｷ` 11 (`:552`), `Gｻﾃﾗ` 3 (`:817`).

## The Kayblis Empire phase (`Tｹｲﾌﾞﾘｽ.ADV`, 1855 lines)

survey-nations §2.2/§2.4/§5 has the labels, the armies' lists and the draws; checked against the
file — where it differs, it is said below (captures, quirk candidates). Shape:

| Lines | Label | What |
|---|---|---|
| 1-8 | — | `Ev_WeeklyEventDone := 0`, RM 2; `F_DemonWar_Stage < 20` → `Gｹｲﾌﾞﾘｽ` 1, jump to `L_004d1` (week S only) |
| 9-62 | — | `Ev_KayblisAssault = 1` → `Gｹｲﾌﾞﾘｽ` 2 (the assault); the `Ai_*` reset (`:13-16`); the recovery `L_02662`, 24 calls on 22 commanders (`:17-62`; Babolat (commander 129) three times, NAT-05) |
| 63-187 | — | the event chain, first match then `@L_004d1` (`Gｼﾙｷｨ` 8 alone does not jump, NAT-26): Medusa's first scene (`F_Medusa_State = 0`, the week not marked), her counter's scenes (NAT-25), Camilla (91) (`Y 4, 3` drawn; `Gｶﾐｰﾗ` 3 at `F_Camilla_State < 10`, 4 / 5 at 10 / 11 on a 1; 5 kills her, `Gｶﾐｰﾗ:2160-2163`), Silky (`:110-148`, only while `F_Kayblis_War = 0`: `Gｼﾙｷｨ` 1, 2-6 on each freed soul of Redeye, Piar, Medusa, Kesselring, Camilla, 8, 9), Warg (`Gﾜｰｸﾞ` 3/4 on `F_Stessel_State` 4/5), `Gﾅｷﾞ` 9 (Kesselring alive, `F_Nagi_State = 5`), `Gｹｲﾌﾞﾜﾝ` 3 / `Gｹｲﾌﾞﾆｬﾝ` 3 (state 2), `Gｻﾃﾗ` 2 (Satella (commander 157) alive, `F_Satella_State = 11`), Medusa 5 (`Y 4, 5`) |
| 188-325 | `L_004d1` | the dispatch (below); `:321-325` gives Monster General (commander 86) its name back; `%0` |
| 326-624 | `L_00881`, `L_00c91`, `L_00faf` | the Assault Leaders' armies (north / south list) and the shared attack-or-capture |
| 625-669 | `L_00fd0` | the Grief Valley guards: Lis Guard (commander 186), `20 + RND(100)` troops, `FIGT_B`, `@L_004d1` (NAT-02) |
| 670-728 | `L_010af`, `L_0114c` | Piar's unit: target `Y 4, 77` (NAT-15), `Gﾊﾟｲｱｰﾙ` 2, PG-7 (136), PG-9 (137), PG-X mk2 (138); after `FIGT_B` Miki (unit 32) in the party → `Gﾊﾟｲｱｰﾙ` 6, else `F_Piar_State` one step (`Gﾊﾟｲｱｰﾙ` 3/4/5) |
| 729-1328 | `L_0120e`, `L_01478`, `L_017e4`, `L_01b5e`, `L_01db2`, `L_01f71` | the generals' units: Babolat, Redeye, Kite, Galtia, Kesselring (with his "flies from" box) |
| 1329-1508 | `L_02038`, `L_021f2`, `L_0227b`, `L_023f8` | Kayblis's main army (at war only) |
| 1509-1614 | `L_0241e`, `L_02464`, `L_025d6`, `L_02662` | troop sum, picker, filler (troops < 99 and class < 5 once slot 1 is filled), recovery |
| 1615-1778 | `L_026bf`, `L_02897`, `L_0291b`, `L_029b2` | capture without battle; a Zeth tower's minister dies; Helman's / Zeth's collapse (NAT-01) |
| 1779-1855 | — | window helpers |

**The dispatch** (`L_004d1`), every week in this order:

1. Grief Valley (89) the player's and `F_Kayblis_Moved = 0` → the guards (`:189-192`), which jump
   back here after each battle — so they attack again until Grief Valley (89) falls. Each defence
   needs a unit at status 1 (`Sﾌﾞﾀｲ`), so the loop ends when the player abandons or loses [I]:
   **Grief Valley (89) cannot be held through a Kayblis phase before `F_Kayblis_Moved = 1`.**
2. The generals, each introduced once and then attacking every week while alive:
   `Gﾊﾞﾎﾞﾗ` 1 (`:193-197`, no `Ev_WeeklyEventDone` test, so in week S), `Gﾚｯﾄﾞｱｲ` 1, `Gｶｲﾄ` 1,
   `Gｶﾞﾙﾃｨｱ` 1 (`:202-224`, one a week: S+1, S+2, S+3 [I]); Kesselring's `Gｹｯｾﾙ` 1 once the
   player holds Bone Forest (90) or Red House (85) (`:229-240`). Their units (`:198-244`): Babolat
   (commander 129) alone; Truffle 77 (148), Moreinyan (149), Ouzawa (150), Redeye (130); Kite (132),
   Tobroiv (151), Deckmaster (152), Strength (153); Derandron (154), Nasport (155), Galtia (131)
   (while `F_Galtia_DefectedToKayblis = 0`); Kesselring (133) alone. Each walks its fixed list (survey-nations §2.4)
   to the first province not Kayblis's: the player's → `FIGT_B` (RM 2; Kesselring RM 1), any
   other → captured without a battle (`L_026bf`). A battle that times out with the player holding
   (`Bt_RoundsLeft = 0`, `Bt_Result = 2`) is fought again once (NAT-03). Babolat's target equal to
   `Babolat_PitfallProv` → `Gﾊﾞﾎﾞﾗ` 4 instead (he dies, `Gﾊﾞﾎﾞﾗ:1764-1766`). After Redeye's battle
   `Y 4, 2` and his state steps (`:949-970`); after Kite's, `Gｶｲﾄ` 2-5 (`:1091-1110`); after a lost
   defence against Galtia, `Gｶﾞﾙﾃｨｱ` 2; after Kesselring's, `Gｹｯｾﾙ` 2 when he is at state 3 and the
   player holds Bone Forest (90) or Red House (85) (`:1273-1285`). Checked with step 2
   (2026-10-11): the pitfall is tested only on a player's province; Kesselring has no second attack
   and no empty-unit test (the others' are dead, NAT-16); with no ninja free, Redeye's battle at
   Maunes (13) after `Gﾚｯﾄﾞｱｲ` 8 hangs on a stale cursor (NAT-28).
3. Piar (`:245-269`): once three of Babolat (129), Redeye (130), Galtia (131), Kite (132) are dead,
   `Gﾊﾟｲｱｰﾙ` 1; then while `F_Piar_State` is 1-99 Piar's unit attacks a random player province.
4. `F_Kayblis_War = 2` → Kayblis's main army (`:270-272`).
5. Silky's Castle (81) the player's and `F_Kayblis_Moved = 0` → `Gｹｲﾌﾞﾘｽ` 3 (`:275-279`): Kayblis
   moves (`F_Kayblis_Moved := 1`), Lis Guard (186) at full troops (5200) attacks Silky's Castle (81)
   (`Gｹｲﾌﾞﾘｽ:658-758`). From then on the guards' loop is closed.
6. The Assault Leaders (`:289-320`): one army per threshold passed — week > 100 1st Assault Leader
   (commander 191), north list; > 110 2nd (192), south; > 120 3rd (193); > 130 4th (194); > 140 5th
   (195); > 150 6th (196); > 155 7th (197); > 160 8th (198), alternating. Each refills Monster
   Generals (commanders 87, 88) to `max/5 + RND(max/3)` (two draws); a dead leader is revived at HP 1
   and does not march that week (`:347-351`), one under 1000 troops after `+Recovery` neither
   (`:360-362`); the leader goes alone at Maunes (13) on the north list (`:436-442`). Checked with
   step 5 (2026-10-11): all confirmed; retreat mode 1; the two Monster Generals are called up alive at
   full HP for every leader, dead, weak or marching (under 99 troops and of a class under 5 the filter
   leaves them out); a living leader gains 3 HP and his recovery once (each up to its maximum); no
   troop sum, no empty-unit test (the leader is alive in slot 1); the south list keeps the unit at
   Maunes (13); with all of the north list (31 provinces, Sulfur Forest (80) … Leazas Castle (1))
   Kayblis's the leader stays home, while the south list (22) runs on into `L_00faf` at Leazas Castle
   (1) and takes it again (`:614-618`, NAT-16, dead in play).

**Kayblis's main army** (`L_02038`, only with `F_Kayblis_War = 2`): Kayblis (commander 135) alone;
then each of Silky (156), Galtia (131, with `F_Galtia_DefectedToKayblis ≠ 0`), Satella (157), Meglass (158) alive with
Monster Generals (86, 88, 89); then Monster Generals (87, 86, 88, 89) — each after `L_021f2` refills
86-89 to `max + RND(max/2)` (NAT-10), up to six walks a week, all RM 2, all along Grief Valley (89),
Great Wall of Helman B (50), Laurengrad (48) … Maunes (13), North (2), Leazas Castle (1)
(`:1440-1499`; checked with step 3, 2026-10-11: no unit is tested for being empty, and the line is
Redeye's without his stop at Maunes (13)). `Gｹｲﾌﾞﾘｽ` 2 (`:759-1392`) remade Kayblis for this war: class 10, attack 138, 1000
attacks, HP 60000 (`:1369-1389`; at boot HP 40, attack 38, 100 attacks, `SET_CHR.ADV:5534-5539`), and
`FIGT.ADV:2839-2841` gives him `RND(100)` shots at war (BRS-17). In a defence against him Satella
(unit 29), Meglass (unit 30) and Galtia (unit 41) defect (`FIGT_B.ADV:217-231` → `Gｹｲﾌﾞﾘｽ` 5/6/7:
the unit to status 0, Satella (commander 157) / Meglass (158) / Galtia (131) to 1, `F_Galtia_DefectedToKayblis := 1`
for Galtia, the unit out of the party).

**The assault** (`F_Kayblis_War := 2`, `Gｹｲﾌﾞﾘｽ:760`) follows `Ev_KayblisAssault = 1`, set by
`Gﾊﾟｲｱｰﾙ` 6 (Miki (unit 32) defending against Piar's unit, `Gﾊﾟｲｱｰﾙ:1371-1374`; Miki leaves), `Gﾐｷ`
6 or `Gﾐｷ` 9 (`COM:566-578`) — all three need Piar's arc running, i.e. three of the four generals
dead [C]. It ends the Silky events (`:110-148`), `Gﾜｰｸﾞ` 2, and turns `Gｼﾙｷｨ` 9 into Silky joining
Kayblis (`Gｼﾙｷｨ:1217-1219`, `:2561-3019`).

**Captures** (`L_026bf`): the province to owner 8, its flag redrawn. Lang Bau (45) while
`F_Helman_War ≠ 2` → Helman collapses (`F_Helman_War := 2`, `F_Helman_FallCause := 1`, which shuts
`Gﾊﾟｯﾄﾝ` 6, `COM:223`); Chizuko's Tower (68), Papaya's Tower (70), Nagi's Tower (67), Magic's Tower
(66) kill their minister (Chizuko (101), Papaya (104), Nagi (107), Magic (110)) and `L_02897` sets
the Zeth story states, the barrier open, and — the four dead and `F_Zeth_War ≠ 2` — Zeth's collapse;
Adam's Fortress (58), Tape (61), Amber Castle (65), Old Zeth (63) kill Saias (113), Uspira (117),
Kabachahn (121), Alex (125). Both collapse tests also fire with the war never started (`≠ 2`
includes 0, NAT-27). The collapse loops stop after one province (NAT-01): Helman's flips **nothing**, since
its first province, Shangri-La (29), is never Helman's (only `Iｼｬﾝｸﾞﾘﾗ:1088, 4403` write its owner,
to 3 and 1; checked 2026-10-11); Zeth's flips Adam's Fortress (58) if Zeth's — and since the capture
goes on testing `Prov_Cur`, now 58, Zeth's collapse also kills Saias (113) (`:1670-1675`, checked by
the port's differential test). On the main path Kayblis's first captures
are Helman's remnants (`Gﾐﾈﾊﾞ:2447-2498` gave back Laurengrad (48), Ikoma (53), Great Walls of
Helman A (49) and B (50), Walkland (54)): Babolat's list reaches Great Wall of Helman A (49), Ikoma
(53), Laurengrad (48), Redeye's Great Wall of Helman B (50) [I]; Kite's ends with Rockearth (17), Ice
(18), Diphtheria (19), Kesselring's with Diphtheria (19), Red (23) — independent cities left are
taken too.

**Draws:** 18 `Y 4` sites (survey-nations §5, all confirmed), interleaved with up to ~9 `FIGT_B`
battles a week, the scenes' own (`Gｶﾞﾙﾃｨｱ` 2 `:324`) and, through `FIGT表示`, `FIGTATTK`'s
(below).

**Quirk candidates** (settled with step 1, 2026-10-11):
- **Medusa's scenes replay** — NAT-25. The counter `N_Medusa_Events` moves on a 1-in-5 roll
  (`:70-73`), and `Gﾒﾃﾞｨｳｻ` 2/3/4 play while it *equals* 2/4/6 (`:74-88`), every week until the next
  roll. They kill Milli (unit 24), Isoroku (unit 27), the ninja Kaoru, each once (`Gﾒﾃﾞｨｳｻ:1060-1064,
  1507-1513, 1893-1899`); the replays return at once but still end the week's event chain
  (`@L_004d1`).
- ~~**`Gｼﾙｷｨ` 8 every week.**~~ Not so: `Gｼﾙｷｨ` 8 runs on into `L_01cd1` (entry 300's label) and ends
  with `F_Silky_State := 8` (`Gｼﾙｷｨ:1784-1789`), so it plays once, twelve weeks after the second
  absorption (`Silky_NextEventWeek`, written only at `Gｼﾙｷｨ:1015-1016`) with the state still 2-5.
  What is left is the missing jump after it (`:140-143`): the chain goes on — NAT-26.
- **Dead code** — NAT-16: `:107-109` (`Ev_WeeklyEventDone` is still 0 there: no scene of the chain
  before it writes it). The south list's fall-through into `L_00faf` (`:614-618`, only with Leazas
  Castle (1) lost) — confirmed with step 5, added to NAT-16.
- NAT-04 is dead in play (confirmed): the jump at `:3-8` happens once, in week S, before any `Ai_*`
  write (`F_DemonWar_Stage` reaches 20 only at `Gｹｲﾌﾞﾘｽ:2105`; `MAIN:989-993` zeroes it only with
  `F_Kayblis_War = 1`, when the phase never runs).
- The collapse tests fire for wars never started — NAT-27.
- (Found with step 2.) After `Gﾚｯﾄﾞｱｲ` 8 the test of Redeye's death reads the cursor the troop sum
  left when the scene returns at once (no ninja free): Redeye only in a full unit, else commander
  record 0 — NAT-28.

## The other nation files

- **`Tｹｲﾌﾞﾘｽ2`** (ported 2026-10-10, `KayblisDefenders`; survey-nations §2.5): at stage 3 `:32` goes
  straight to the named garrisons. Blue House (86): Hikaru (160), Rika (161), Kaybwan (159); Red House
  (85): Wao (163), Kunihiko (164), Kaybnyan (162); Tower of Evil (78): `Gﾜｰｸﾞ` 1 while
  `N_Warg_Stage = 0`; Kesselring's Castle (87): Kesselring (133) + Monster Generals (87-89); Camilla's
  Castle (88): Camilla (91, maximum 200 → 2000) + Monster General (88); **Demon Lord's Castle (79)**:
  `Gｹｲﾌﾞﾘｽ` 4 on the first attack (`F_DemonCastle_Attacked := 1`), walls, Monster Generals (181,
  182, 183) + Kayblis (135); Kayblis's Castle (84): walls, Medusa (134) + Monster Generals (87, 88);
  a dead leader or any other province: Monster Generals (86-89), no walls (NAT-21).
- **`Tﾍﾙﾏﾝ2`**: Helman's remnants are militia ("A Certain Country's People") after the war — until
  Kayblis takes them. **`TJAPAN`/`TJAPAN2`, `Tﾊﾟｯﾄﾝ`**, and `Tﾍﾙﾏﾝ`/`Tｾﾞｽ` on the `Iﾏｼﾞﾝ` 51 path:
  as at stage 2, ported.
- **`Tﾙﾄﾞﾗ`** (205 lines) and `Tﾙﾄﾞﾗ2` (empty): only with `StoryStage = 4` (`MAIN:816`). Its first
  run is the Creator phase of the week stage 4 begins (after the player phase or the Kayblis
  phase that killed Kayblis) — beyond stage 3.

## The player's commands

**`COMMAND` and the command files have no stage-3 gate** (`COMMAND` tests `StoryStage` at `:4`,
`:183`, `:419`, `:516` only). `Cﾃｷﾄｼ`: `:147` is stage 2 only, so Tower of Evil (78), Grief Valley (89),
Bone Forest (90), Tearing Forest (91) are attacked plainly; `:109` refuses at stage 4 only; Land of
Death (82) has its warning (`:115`); Satella (unit 29) and Meglass (unit 30) refuse any target not
owner 8 or 10 (`:266-307`), so at stage 3 they fight the demons; owner 8 → `Tｹｲﾌﾞﾘｽ2` (`:354-360`);
one conquest a week (`:112`). What the demon provinces hold for an attacker:

| Province | On attack | Holding it |
|---|---|---|
| Tower of Evil (78) | `Gﾜｰｸﾞ` 1, no battle, while `N_Warg_Stage = 0` — which needs `Gﾜｰｸﾞ` 2, i.e. an attack on the Demon Lord's Castle (79) first | Dungeon of Lies (dungeon 19) |
| Grief Valley (89) | Monster Generals (86-89) | the guards' loop until `F_Kayblis_Moved = 1` |
| Bone Forest (90) / Red House (85) | plains / Kaybnyan's trio | `Gｹｯｾﾙ` 1 (Kesselring marches weekly); Red House (85): Mos's Dungeon (dungeon 20) |
| Kesselring's Castle (87) | Kesselring + three generals | if Kesselring dies there `Gｹｯｾﾙ` 10 (`死亡捕獲:349-354`) revives him (HP 20, +200 troops) and gives the castle back to Kayblis (`Gｹｯｾﾙ:306-329, 547-603`); he dies only in `Gｹｯｾﾙ` 2 (castle to Leazas, `:2832-2838`); Maid Graveyard (dungeon 21) |
| Camilla's Castle (88) | Camilla (2000 troops) | won with Camilla down → she dies, `Gｶﾐｰﾗ` 11 (`死亡捕獲:375-385`) |
| Blue House (86) | Kaybwan's trio | Rex's Dungeon (dungeon 22) |
| Kayblis's Castle (84) | walls, Medusa | — |
| Silky's Castle (81) | plains | `Gｹｲﾌﾞﾘｽ` 3 in the next Kayblis phase |
| Demon Lord's Castle (79) | `Gｹｲﾌﾞﾘｽ` 4 first; walls, Kayblis | Kayblis killed → end of stage 3 |
| Tearing Forest (91) | plains; `FIGT_A`'s `Gﾏｼﾞｯｸ` 25 with Magic (unit 39) and `F_Magic_State = 5` | Piar's Ship (dungeon 9): `Gﾊﾟｲｱｰﾙ` 7 ends Piar's arc (`Cﾒｲｷｭｳ:192-198`, `F_Piar_State := 100`) |

Other commands: `Cｼﾊｲﾄｼ`'s places reach `Gｹｲﾌﾞﾜﾝ` 2, `Gｹｲﾌﾞﾆｬﾝ` 2 (`:523-532`), `Gｶﾞﾙﾃｨｱ` 3, `Gﾊﾞﾎﾞﾗ`
3 (`:631-645`); `Cﾒｲｷｭｳ` has Kaybwan and Kaybnyan at floor 50 of Rex's Dungeon (dungeon 22) and Mos's
Dungeon (dungeon 20) (`:800-818`); God's Door as at stage 2. All ported.

## End of stage 3

**Kayblis (commander 135) dies in a battle** → `死亡捕獲:362-366` → `Gｹｲﾌﾞﾘｽ` 10 (`:2110-3130`:
Kaybwan's and Kaybnyan's arcs closed; unless Satella (unit 29) is gone, `VAR0386 := 1` and, with
`F_Silky_State = 8`, `F_Hornet_Joined := 1`, `:2437-2550, 3109-3110`) → **`Iｼﾚｲﾌﾞ` 7**
(`:3128-3129`). There (`:149-168`)
every province 1-91 but Shangri-La (29) and Nothing (75) is counted: **all Leazas's → `StoryStage :=
4`** (`:168`) and the game goes on; **any other → ending 8** (`:239-1467`, `EndingId := 8` and
`&#OVER` at `:1466-1467`).

If he dies in a battle of his own army and `Iｼﾚｲﾌﾞ` 7 moves the story to stage 4 (rather than ending
the game), the same phase goes on: the rest of his army's marches, `Gｹｲﾌﾞﾘｽ` 3 and the Assault Leaders
still run [I].

Kayblis fights in two places only (no other file puts commander 135 in a party): the Demon Lord's
Castle (79) attacked (`Tｹｲﾌﾞﾘｽ2:124-143`, RM 1) and his own army at war (`Tｹｲﾌﾞﾘｽ:1333`) (`FIGT_B`; the castle stays his, so ending 8 unless the player took the
Demon Lord's Castle (79) earlier without killing him [I]). Class 10, he takes commander damage only
from a unit of class ≥ 10 or one holding Chaos (item 1) or Nikkou (item 2) (`FIGT.ADV:2428-2435`).
Before the assault he has 40 HP; Silky's `Gｼﾙｷｨ` 9 (`F_Silky_State = 6`: five souls absorbed, no
war) drops them to 2 (`Gｼﾙｷｨ:2555-2557`); at war he has 60000 [I: practically unbeatable].

**Shortest path to the end** [I]: the Demon Lord's Castle (79) is reached from Sulfur Forest (80) or
Grief Valley (89) — Tower of Evil (78) is closed by Warg, Grief Valley (89) by the guards until
Silky's Castle (81) is taken. West: Bone Forest (90) (the gate attack, S) → Kesselring's Castle (87)
→ Cascade Bau (83) → Silky's Castle (81) → Sulfur Forest (80) or Grief Valley (89) → Demon Lord's
Castle (79): six conquests, Kayblis dead in S+5 at one conquest a week — if Kesselring's Castle (87)
holds (he must not die there). East from Tearing Forest (91): Camilla's Castle (88), Blue House
(86), Kayblis's Castle (84), Land of Death (82), Silky's Castle (81), then the same two: eight. Both
end in **ending 8**. **Stage 4** needs the whole map at the moment Kayblis dies: the 14 demon
provinces, Helman's remnants (taken by Kayblis meanwhile), JAPAN's Nagasaki (55), Osaka (56), Sado
(57), Temple AL HQ (77) and every independent city not yet taken — at least 14 and typically 20-30
conquests [I], while up to six generals' units (plus the Assault Leaders after week 100) capture or
attack every week.

## Game over at stage 3

- **Ending 8**, Kayblis killed without the whole map (above).
- **Ending 2**, Leazas Castle (1) lost in a `FIGT_B`: every Kayblis list ends there (Babolat,
  Redeye, Galtia, Kesselring, Kayblis's army, both Assault Leader lists), plus La Vaswald, Mars, the
  AL Church, JAPAN. **Ending 1**, Rance's death (`死亡捕獲:27-31`).
- On the `Iﾏｼﾞﾝ` 51 path with the wars on: ending 3 (`Gｺｰﾄﾞ` 4), Papaya's trap (`Gﾊﾟﾊﾟｲｱ` 2,
  ending 1); with JAPAN: ending 4 (Shinobu).
- Not stage-gated, as at stage 2: endings 12, 7, 5 (talks and harem scenes), God's Door (`Iｶﾐ`:
  9/10/11/13/1). Ending 6 is closed (above).

## The story spine at stage 3

| `Gｹｲﾌﾞﾘｽ` | Lines | Caller (condition) | Does |
|---|---|---|---|
| 1 | 1393-2109 | `Tｹｲﾌﾞﾘｽ:3-8` (week S) | the war council; demon war 20, Camilla 100 |
| 2 | 759-1392 | `Tｹｲﾌﾞﾘｽ:9-12` (`Ev_KayblisAssault = 1`) | the assault: `F_Kayblis_War := 2`, Kayblis remade |
| 3 | 658-758 | `Tｹｲﾌﾞﾘｽ:275-279` (Silky's Castle (81) the player's, not moved) | `F_Kayblis_Moved := 1`, Lis Guard (186) attacks Silky's Castle (81) |
| 4 | 503-657 | `Tｹｲﾌﾞﾘｽ2:124-128` (first attack on the Demon Lord's Castle (79)) | Kayblis's reaction; `F_DemonCastle_Attacked := 1` |
| 5 / 6 / 7 | 44-232 / 233-360 / 361-502 | `FIGT_B:217-231` (at war, Kayblis leading) | Satella (unit 29) / Meglass (unit 30) / Galtia (unit 41) defect |
| 10 | 2110-3130 | `死亡捕獲:362-366` (Kayblis dead) | `Iｼﾚｲﾌﾞ` 7: stage 4 or ending 8 |

| Kayblis's general | Ends |
|---|---|
| Babolat (129) | dies → `Gﾊﾞﾎﾞﾗ` 10 (`死亡捕獲`); or the pitfall, `Gﾊﾞﾎﾞﾗ` 4 |
| Redeye (130) | dies → `Gﾚｯﾄﾞｱｲ` 10; his soul freed for Silky; Freak's arc (`Gﾌﾘｰｸ` 4 replaces a battle, `:923-927`) |
| Kite (132) | dies → `Gｶｲﾄ` 10; Calory's arc `Gｶｲﾄ` 2-7 |
| Galtia (131) | dies → `Gｶﾞﾙﾃｨｱ` 10; or joins Leazas (`Gｶﾞﾙﾃｨｱ` 2, Martina at 6) |
| Kesselring (133) | revived when killed; dies only in `Gｹｯｾﾙ` 2 (state 3 from `Gｻﾃﾗ`, `Gﾎﾗｶﾞ` or `Gﾒｶﾞﾗｽ`) |
| Piar (PG-7 (136), PG-9 (137)) | `Gﾊﾟｲｱｰﾙ` 11/12 when they die; `Gﾊﾟｲｱｰﾙ` 7 at Piar's Ship (dungeon 9) |
| Medusa (134) | dies → `Gﾒﾃﾞｨｳｻ` 10; or `Gｻｲｾﾞﾙ` 9 |
| Camilla (91) | `Gｶﾐｰﾗ` 5 (event chain) or 11 (her castle won) |

## Not ported yet, reached at stage 3

| What | Lines | Reached |
|---|---|---|
| ~~`Tｹｲﾌﾞﾘｽ`~~ | 1855 | every week from S (`MAIN:594-629`) — main path. **Ported whole** (2026-10-11): the frame (step 1, `Strategy/KayblisTurn`), the generals' units `:729-1328` (step 2, `Strategy/KayblisTurn.Generals.cs`), Piar's unit `:670-728` and Kayblis's army `:1329-1508` (step 3, `Strategy/KayblisTurn.Armies.cs`), the Assault Leaders `:326-624` (step 5, `Strategy/KayblisTurn.Assault.cs`) |
| `FIGTATTK` | 395 | from `FIGT表示`, a scene: its entries 2-7, 11-14 and 26 call it (24 sites); at stage 3 `Gｼﾙｷｨ` 8 (`FIGT表示` 26, once, above) and `Gﾌﾘｰｸ` 4 (`FIGT表示` 2); already at stage 2 `Iﾏｼﾞﾝ` 12, 14, 16, 18 (`FIGT表示` 3, 5, 6 and 7, 14) and `Gﾊﾟｯﾄﾝ` 2 (4). Was neither a scene file (`SceneRunner.IsSceneFile`) nor in `StrategyRules.PortedFiles`, so `ScriptContext.CallAsync` sent it to the unported hooks (Godot: skipped with a notice; `--auto`: stops), losing its one `Y 4, 2` per call (`FIGTATTK.ADV:234`). **Routed 2026-10-11** to the battle's port of the file (`Battle.PlayExchangeAnimationAsync`), checked against the file on 1900 random runs of `FIGT表示`; `ScenarioProgramSweepTests` now checks that every file the scenes call runs somewhere |
| `OVER` | 2210 | ending 8 and every other ending; `GameEnd` until W4 |
| `Tﾙﾄﾞﾗ` | 205 | the week stage 4 begins — after the milestone |

Every scene stage 3 reaches runs from the player's files; their rule calls are `FIGT_A`/`FIGT_B`
(`Gｹｲﾌﾞﾘｽ` 3, `Gｹｯｾﾙ` 2's duel and battle `:2420-2437`/`:2951-2963`, `Gｻｲｾﾞﾙ` 12, `Gｻﾃﾗ` 20), `死亡捕獲`,
`Sﾌﾞﾀｲ` and `FIGT表示`; none calls a `T*` file. Since step 1 `Tｹｲﾌﾞﾘｽ` is routed (it was skipped with a notice in
interactive Godot before, so stage 3 had no Kayblis phase at all). Since step 5 the whole phase runs.

## Open questions

1. Can a player hold Grief Valley (89) through a Kayblis phase before Silky's Castle (81) — i.e. is
   the guards' loop really ended only by abandoning or losing (NAT-02)? It decides the shortest path.
2. Can Kesselring's Castle (87) be won with Kesselring alive (troops gone, HP left)? `Gｹｯｾﾙ` 2's
   branch at `:871` reads a `Tmp0001` left by the battle before it — duel or not [I: stale].
3. Can Kayblis at 40 HP be killed without Silky's `Gｼﾙｷｨ` 9 — and at war (60000 HP) at all?
4. Medusa's replays (NAT-25): confirm by capture. (`Gｼﾙｷｨ` 8 does not replay: NAT-26.)
5. Is La Vaswald reached at stage 3 in practice (`N_Saizel_Weeks > 40`)?
6. ~~`docs/variables.tsv`~~ Renamed with step 1 (2026-10-11): VAR0744 is `F_DemonCastle_Attacked` (the
   first attack on the Demon Lord's Castle (79) reported, `Gｹｲﾌﾞﾘｽ` 4), VAR0637
   `F_Galtia_DefectedToKayblis` (Galtia (unit 41) defected, `Gｹｲﾌﾞﾘｽ` 7, read at `Tｹｲﾌﾞﾘｽ:226, 1360`) —
   this answers survey-nations §9 Q3.
7. Stage-3 captures: from the owner's saves at the gate attack; `ｽﾀMENU`'s "From 4" (week 77, demon
   war at 9) needs `DebugMode`.

## Proposed order — milestone 4 (story stage 3)

The milestone would start in week S and end with `StoryStage := 4` (`Iｼﾚｲﾌﾞ` 7), before that
week's Creator phase (`Tﾙﾄﾞﾗ`), or with ending 8. The week, the commands, `Tｹｲﾌﾞﾘｽ2` and the scenes
are ported or run already; what is missing is `Tｹｲﾌﾞﾘｽ` (on `NationFile`, its walk "first province
not Kayblis's", its filter and its capture with the collapses). Main path first:

0. **Sweep and harness**: this file; the owner's stage-3 checklist in
   [../golden-tests.md](../golden-tests.md) (`Tｹｲﾌﾞﾘｽ` is a capture root already).
1. **`Tｹｲﾌﾞﾘｽ`, the frame** ✓ (2026-10-11, `Strategy/KayblisTurn`; README "Milestone 4") (~720 lines): `:1-325` (week S's jump with `Gｹｲﾌﾞﾘｽ` 1, the `Ai_*` reset,
   the 24 recoveries of 22 commanders, the event chain, the dispatch), the guards `:625-669`, the template
   `:1509-1614`, the captures and collapses `:1615-1778`, the helpers; the armies throw
   `NotSupportedException` until their step, as in milestone 3.
2. **The generals' units** ✓ (2026-10-11, `Strategy/KayblisTurn.Generals.cs`) (~600): Babolat, Redeye,
   Kite, Galtia, Kesselring `:729-1328`.
3. **Piar and the assault** ✓ (2026-10-11, `Strategy/KayblisTurn.Armies.cs`) (~240): Piar `:670-728`,
   Kayblis's main army `:1329-1508`.
4. ~~**`FIGTATTK`**~~ — done before the milestone (2026-10-11): routed to the battle's port, since
   stage 2 needs it too. Left: the presenter hands the battle's events to its battle layer only
   between a battle's `BattleScreenEvent`s, so `FIGT表示`'s counters are probably not drawn yet
   [I] (display only).
5. **The Assault Leaders** ✓ (2026-10-11, `Strategy/KayblisTurn.Assault.cs`) (~300): `:326-624`; from S
   on the `Iﾏｼﾞﾝ` 51 path, else after week 100.
6. **The end**: `Iｼﾚｲﾌﾞ` 7 runs from the player's files; a test that Kayblis's death reaches stage 4
   (the session then stops at `Tﾙﾄﾞﾗ`, "not ported yet") or ending 8 (`GameEnd`).

Optional fronts need no new rule code: La Vaswald (`Gｻｲｾﾞﾙ` 12), JAPAN, the AL Church, God's Door,
Helman and Zeth on the `Iﾏｼﾞﾝ` 51 path run on ported files.
