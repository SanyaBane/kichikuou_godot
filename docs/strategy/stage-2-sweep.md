# What stage 2 reaches (`StoryStage = 2`, swept 2026-10-10)

The sweep behind milestone 3 (owner's decision 14 in [README.md](README.md)); the counterpart of
"What stage 1 reaches" there. Lines refer to `_extract\scenario_named` (assignments read
`!Var : n!`, `\` is ≠). [I] marks an inference; everything else was read. **W** is the week in
which Oaks (6) falls (week 34 in the `--seed 2` run of milestone 2, week 20 in the owner's third
stage-1 capture run).

Owners (`Prov_Owner`, VAR0130): 1 Leazas (the player), 2 the rebels, 3 the independent cities,
4 Helman, 5 Patton, 6 JAPAN, 7 Zeth, 8 Kayblis, 9 Hornet, 10 Angels, 11 the AL Church. At boot
Tower of Evil (78) through Cascade Bau (83) and Grief Valley (89) are Hornet's; Kayblis's Castle
(84) through Camilla's Castle (88), Bone Forest (90) and Tearing Forest (91) Kayblis's.

## Start

`Iｼﾚｲﾌﾞ` 2 (`:2688-3057`, called from `FIGT_A.ADV:192-195` when Oaks (6) is won) sets
`StoryStage := 2` (`:2689`), gives North (2) through Parapara Fortress (16) to Leazas
(`:2691-2702`; of these the rebels held only Oku (3), South (4), Ys (5), Baranchi (7)) and plays
`Gｴｸｽ` 4 (`:2786`). No owner-2 province is left; `MAIN:298` runs the rebels' phase only at stage
1, so `TCﾘｰｻﾞｽ` and `TCﾘｰｻﾞｽ2` never run again.

**The fixed opening** (milestone 2 ends in W+2's player phase):

- **W, at its end:** `N_DemonInvasion_Stage` 0 → 1 (`MAIN:893-895`).
- **W+1:** `COM:152-156` plays `GAL` 1 (`N_ALChurch_Stage := 1`, `GAL:366`); at the week's end
  `MAIN:887-892` plays `Iﾏｼﾞﾝ` 1 — two `FIGT_B` defences of Leazas Castle (1) (Monster Generals
  (commanders 86, 87), then 86 + 87 + La Seizel (commander 90)); `N_DemonInvasion_Stage := 2`, La Seizel
  (90), Camilla (91) and Lei (92) to status 1 (`Iﾏｼﾞﾝ:84-92`).
- **W+2:** `COM:52` moves `N_ALChurch_Stage` 1 → 2; `COM:112-116` plays `Iﾏｼﾞﾝ` 2 with a question
  (`Iﾏｼﾞﾝ:2034-2051`): "Watch For Now" → `N_DemonInvasion_Stage := 3`; "Rape Demon Lord" → `Gﾐｷ` 1:
  `F_Kayblis_War := 1`, `N_DemonInvasion_Stage := 100` (`Gﾐｷ:3752-3757`) — a game over about six weeks
  later (below).
- **W+3:** `Iﾏｼﾞﾝ` 3 (`COM:107-111`): `N_DemonInvasion_Stage := 4`, `N_Miki_Stage := 1`, Nikkou (item 2)
  to Rance (`:3305-3317`), `N_Majin_EventTimer := 4` (`:3318`). `GAL` 2 (`COM:157-160`):
  `N_ALChurch_Stage := 4` and a menu (`GAL:980-994`): Keep Donating (−65 a week goes on) or Stop
  Donating (20). That week's Camilla Unit phase (`MAIN:631-815`) has up to three battles: La Seizel
  (90) forced onto Leazas Castle (1) at full HP (`:721-740`), `Gｻﾃﾗ` 1 (`:742-746`: Satella (unit
  29) and Meglass (unit 30) join, stage 5, `Gｻﾃﾗ:3642, 4401-4404`), Lei (92) and Camilla (91) each
  raid a province, `:808-810` sets 6.
- **W+4** after Stop Donating: `GAL` 4 (`COM:147-150`, 21): the troops of every living unit 1-43
  but Hanty (19), Freak (20), Mill (25), Satella (29), Meglass (30), Galtia (41) and the Seizel
  Sisters (42) cut by `Y 4, Unit_Troops/6` (`GAL:2725-2753`), Gurwin (unit 44) leaves
  (`:2772-2774`); the AL Church phase runs from then on.

## The week (`MAIN.ADV:83-1045`, ported whole in milestone 2)

Open at stage 2, besides what stage 1 has (autosave, income, refill, report, `COMMAND`):

- **Expenses:** the AL Church −65 while `N_ALChurch_Stage < 19` (`:197`); M Land −20 after its
  negotiation (`F_MLand_Funded`, `Cﾃｷﾄｼ:3079`). Galtia's food stays closed (Galtia (unit 41) joins
  only through Kayblis armies) [I].
- **By week:** `Gﾌｳｶ` 1 after week 39, 2 after week 51 (`COM:258, 263`), then `Gﾌｳｶ` 4 (`:216`) the
  same week; the Sill chain `Gｼｨﾙ` 23-27 at weeks > 30/35/40/45/50 (`:896-925`, ascending tests,
  so several can play in one week; it stops once `F_Helman_War = 2`).
- **`Gｶﾄﾞﾐｭｳﾑ` 1** (`:220`): needs `F_Asuka_State = 1`, written in a battle with Mill (unit 25)
  (`FIGT_M:8325-8327`), who joins at Custom (21). **`Gｸﾘｰﾑ` 3** (`:225`): Cream (unit 28) joins
  after Cream (commander 45) of Helman's 4th army is beaten (`死亡捕獲:95`).
- **Mars** (`:229`, `N_Mars_Score = 1`, set by `Gｶｾｲ` 1 after Mars (22) is taken by force): `Gｶｾｲ`
  2 sends Mars the Great (commander 71) against Leazas Castle (1) with `FIGT_B` every week until he
  dies (`Gｶｾｲ:3084-3096`).
- **The AL Church phase** (`:260`, at 21 or 100): `GAL` 5 (`GAL:2805-2962`): the Temple Knights
  (commanders 139-142) at 21 against a province `Y 4, 10`, then the apostles (all commander record
  100, renamed with `MS 4`) grow by `Y 4, 500` and attack on `Y 4, 2` = 1 with troops > 800, then
  "AList" on `Y 4, 2` = 1; each attack picks `Y 4, 28` and runs `FIGT_B` if the province is the
  player's.
- **The nation phases** JAPAN, Helman, Patton, Zeth — open once their wars start (next section).
  Kayblis (`:594`) needs stage 3; Creator (`:816`) stays closed; La Vaswald (`:849`) is
  unreachable [I: `Gｻｲｾﾞﾙ` 9 needs `N_Seizel_Weeks > 40`, which starts after `Iﾏｼﾞﾝ` 16; the slow
  path ends the stage about 38 weeks later — by about three weeks].
- **The Camilla Unit** (`:631-815`, `N_DemonInvasion_Stage` 2-99; losses go to owner 8, `:663`): Sieg
  (commander 93, revived by `Gｼﾞｰｸ` 1 once Lei (92) is dead, `:666-670`), La Seizel (90), Lei (92),
  Camilla (91), each if alive: a refresh, `Y 4, 16` picks a province 1-16, a second roll, `FIGT_B`
  if it is the player's; `Gｶﾐｰﾗ` 1/2 on `RetVal = 2`, else `Gｶﾐｰﾗ` 8.
- **`Gﾏｼﾞｯｸ` 5** (`:926`) in the Zeth war: a `FIGT_A` duel of a random unit (`Y 4, 48`, up to 10
  tries) against Xacalite (commander 97) (`Gﾏｼﾞｯｸ:2283-2402`).
- **The demon war** (`:932-985`): open while `F_Kayblis_War = 0`, `N_DemonInvasion_Stage > 5` and
  `N_Majin_EventTimer = 0`. The timer drops by 1 at every week's end (saturating at 0, `Sys3Int`);
  while both wars are won and the stage is below 10 it is forced to 0 (`:933-935`). The entries
  are tested from the highest stage down, so one plays per week: `Iﾏｼﾞﾝ` 11-19 set stages 1-9
  with timer 5, 20 sets 10 with timer 10, 50 sets 11 with timer 8, **51 sets `StoryStage := 3`**
  (`Iﾏｼﾞﾝ:8982`). They move provinces: 16 gives Cascade Bau (83) to Kayblis and moves Summit's
  Mouth (dungeon 8) to Scale (12) (`:5824, 6413-6466`); 18 Land of Death (82) and Silky's Castle
  (81); 20 Tower of Evil (78), Sulfur Forest (80), Grief Valley (89) and Demon Lord's Castle (79)
  (`:8398-8444`). 12/14/16/18 play `FIGT表示` cutscenes.
- **The Kayblis war** (`:986-1035`), only after "Rape Demon Lord": `Iﾏｼﾞﾝ2` 21-23, then 24 =
  ending 6 (`Iﾏｼﾞﾝ2:600-905`) about W+8 [I]; Satella (29) or Meglass (30) alive would reset it, but
  they join only through `Gｻﾃﾗ` 1, which this branch shuts off [I].

**`COM` at stage 2.** Tier 1: `Iﾏｼﾞﾝ` 2/3, `GAL` 1/2/4, `Gｹﾝﾀﾛｳ` 1/2 (after Sieg (93) falls),
`Gﾌｳｶ` 1/2; with the wars `IJAPAN` 1/2, `Gﾊﾟｯﾄﾝ` 1/6, `Iｾﾞｽ` 100, `Gｺｰﾄﾞ` 4, `Iｼｬﾝｸﾞﾘﾗ` 3/4,
`Iｼﾚｲﾌﾞ` 4 (`:192`, both wars won: the HQ meeting). Tier 2, new: `Gｼｨﾙ2` 1 (`:371`, after Helman
falls), `Gｽｽﾞﾒ` 1 (`:472`), `Gﾏﾘｽ` 12 (`:775`, calls `Iｼｬﾝｸﾞﾘﾗ` 1), `GAL` 8 (`:312`). None of
these scenes calls a rule file; the rule calls in the week come from `Iﾏｼﾞﾝ` 1, `GAL` 5, `Gｶｾｲ` 2,
`Gﾏｼﾞｯｸ` 5, the Camilla block (`FIGT_B`/`FIGT_A`), `Iﾏｼﾞﾝ` 12-18 and `Gﾊﾟｯﾄﾝ` 2 (`FIGT表示`), and
`OVER`.

## The wars and the nation files

| War | Starts | Its files | Ends |
|---|---|---|---|
| Helman (4) | the first attack on Log B (31) or Log A (32) (one unit through the pass, `Cﾃｷﾄｼ:174-213`): `Tﾍﾙﾏﾝ2:2-5` plays `Gｽﾃｯｾﾙ` 1, `F_Helman_War := 1` (`Gｽﾃｯｾﾙ:650-651`), whatever the battle's outcome; possible in W+1, its phase runs the same week | `Tﾍﾙﾏﾝ2` (377), `Tﾍﾙﾏﾝ` (792; `Gﾚﾘｭｰｺﾌ` 5 `FIGT_B`, `Gﾊﾟｯﾄﾝ` 2) | `Gﾐﾈﾊﾞ` 5 (`F_Helman_War := 2`, `Gﾐﾈﾊﾞ:1707-1709`) when Lang Bau (45) is won (`FIGT_A:196-199`) or Mineva (commander 39) dies there (`死亡捕獲:166-178`): every Helman province to Leazas, then Laurengrad (48), Ikoma (53), the Great Walls of Helman A (49) and B (50) and Walkland (54) back to Helman (`:2447-2498`). Shortest: six conquests from Maunes (13) — Log B (31), Archgrad (36), Rosesgrad (39), Sudori 10 (42), Micrograd (44), Lang Bau (45) |
| Patton (5) | `Gﾊﾟｯﾄﾝ` 1 (`COM:132-135`, the week after the Helman war starts) gives Sudori 17 (46) to Patton, `Gﾊﾟｯﾄﾝ` 2 (from `Tﾍﾙﾏﾝ`) Parnas (47); his army phase only after `Gﾊﾟｯﾄﾝ` 6 (`COM:222-226`, Patton (commander 54) alive, Helman won) | `Tﾊﾟｯﾄﾝ` (226); the attack's `Gﾊﾟｯﾄﾝ` 3/4 and the inline list (`Cﾃｷﾄｼ:124-134, 361-376`) are ported | Give Treaty (`Gﾊﾟｯﾄﾝ` 3): Sudori 17 (46) and Parnas (47) to Leazas, Patton (18), Hanty (19), Freak (20), Hubert (21) join |
| Zeth (7) | the first attack on Adam's Fortress (58) (it lists Parapara Fortress (16) as a neighbour): `Tｾﾞｽ2:45-49`, `F_Zeth_War := 1`, `Iｾﾞｽ` 1 | `Tｾﾞｽ2` (401; the barrier `:74-130` empties the party at Magic's Tower (66), Nagi's Tower (67), Chizuko's Tower (68), Papaya's Tower (70) without the Barrier Blade (item 12); Papaya's trap `Gﾊﾟﾊﾟｲｱ` 2), `Tｾﾞｽ` (777; `Gﾏｼﾞｯｸ` 3) | `Iｾﾞｽ` 100 (`COM:231-245` weekly, `死亡捕獲:305-318` after a battle) once the four ministers Chizuko (commander 101), Papaya (104), Nagi (107), Magic (110) are all at status 0: every Zeth province to Leazas. Their towers are four (Nagi's) to six (Papaya's) conquests away |
| JAPAN (6) | `IJAPAN` 1 (`COM:102-105`, Portugal (28) held; every answer leads to `:1062`, "Leave it to Lia" sets 3 and Leila (unit 5) leaves), `IJAPAN` 2 (`COM:127-130`), or the first attack on Nagasaki (55) (`TJAPAN2:4`, over the alliance states too) — all behind Portugal (28), so behind the independent cities | `TJAPAN2` (219, ported 2026-10-11), `TJAPAN` (2851; its rules ported 2026-10-11, Gekkou's scenes `:295-2744` too); `FIGT_A`'s JAPAN hooks (`IJAPAN` 3-6) | — (optional front) |
| The AL Church (11) | Stop Donating (`GAL` 2) → the AL Church phase; an attack on Temple AL HQ (77) (once Diphtheria (19) is held) → `GAL` 9 (`Cﾃｷﾄｼ:350-353`) breaks with the church | scenes only (`GAL` 4/5/7/9/10) | — |

Draws in the nation files: as `survey-nations.md` §5 (`Tﾍﾙﾏﾝ:60`, `Tｾﾞｽ:55-70`, `TJAPAN:10-29` every
phase; `Tﾍﾙﾏﾝ2` the militia `Y 4, 800` at `:64, 69, 173, 178`; `Tｾﾞｽ2:22-38` on a repeated attack;
none in `Tﾊﾟｯﾄﾝ`, `TJAPAN2`).

## The player's commands

**`COMMAND` has no stage-2 gate** (it tests `StoryStage` at `:4`, `:183`, `:419`, `:516` only):
an own province opens `Cｼﾊｲﾄｼ`, an enemy province whose `Prov_Adj1-5` names an own province
`Cﾃｷﾄｼ`, any other `Cﾃｷﾄｼ2` (ported). At the start of stage 2 `Cﾃｷﾄｼ` opens on Rockearth (17), Zeo
(24), Hanna (26), Shangri-La (29) (independent), Log B (31), Log A (32) (Helman) and Adam's
Fortress (58) (Zeth); later on Nagasaki (55) (from Portugal (28)), Temple AL HQ (77) (from
Diphtheria (19)), Tower of Evil (78) / Grief Valley (89) (from the Great Walls of Helman A (49) /
B (50)) and Bone Forest (90) / Tearing Forest (91) (from the Maginot Strongholds (72) / (76)).
`:112` still allows one conquest a week; `Cﾃｷﾄｼ`, `Cﾃｷﾄｼ2` and `COMMAND` have no draws.

**The independent cities** (`Cﾃｷﾄｼ.ADV:429-3828`, 3400 lines, about 45 labels, mostly text, no
draws; ported whole, 2026-10-11 — README, "Milestone 3", step 4).
Before the menu (`:429-485`): Portugal (28) with
`F_Merchant_State = 1` → Pluepet's scene (`:2431-2697`, Gold + 100); Shangri-La (29) →
`Iｼｬﾝｸﾞﾘﾗ` 1 or 2; Custom (21) → `Iｼﾚｲﾌﾞ` 3 (`:456-460`: Custom joins without a battle; Maria
(16), Shizuka (17), Eleanor (23), Milli (24), Mill (25) join); Toushin City (27) → `Sﾌﾞﾀｲ` and
`Gｻｰﾅｷｱ` 1 (Sanakia (commander 62) fought inside the scene; a win takes the city and Merim joins).
The menu (`:486-506`): Negotiate (free, repeatable; a success sets owner 1, spends the attack and
zeroes the city's commanders, `L_05109`), Use Force (`:3649-3828`: `Sﾌﾞﾀｲ` with 4 units, no
refusals CMD-01, no walls BFL-15, retreat mode 1, the city's list through `L_04f2a` into
`FIGT_A`; afterwards Portugal at state 2 → `Gﾚﾍﾞｯｶ` 2, Mars → `Gｶｾｲ` 1), Stop.

| City | Negotiation | Use Force against |
|---|---|---|
| Rockearth (17) | approve → `F_DX_State` 1, Gold + 500, success; "die" → 100, refused for good (`:957-1430`) | Rockearth Hero (commanders 68, 69) |
| Ice (18) | always surrenders (`:1431-1535`) | Ice Hero (66, 67) |
| Diphtheria (19) | the mayor never comes (`:921-956`) | Diphtheria Hero (70) |
| Lazeal (20) | "I'll wait" (`DXTalk_RetryWeek := Week + 4`, the file left, attack not spent), then once that week has passed `Gﾚｨﾘｨ` 1, success (`:1536-1981`) | Lazeal Hero (65) |
| Mars (22) | Make a General → Mars the Great (unit 26) joins, `N_Mars_Score := 2`, success, no `Gｶｾｲ` 1 (`:634-920`) | Mars the Great (71), then `Gｶｾｲ` 1 (Mars phase) |
| Red (23) | "I promise" → success (`:1982-2202`) | Red Hero (63, 64) |
| Zeo (24) | a refusal letter (`:2698-2810`) | Zeo Hero (13-16) |
| M Land (25) | Maintain the Park → `F_MLand_Funded`, `F_Sayori_State` 1, success (`:2811-3310`) | Sayori (11), M Land Hero (12) |
| Hanna (26) | pay 30, refused below 29 (CMD-04); `F_Shinoda_State := 1` while Gengorou (commander 8) lives (`:3311-3648`) | Hanna Hero (9, 10), Gengorou (8) |
| Portugal (28) | Pay and Conquer or Don't Pay, both succeed (CMD-03; `:2203-2430`) | Portugal Hero (17) |
| Shangri-La (29) | none: `Iｼｬﾝｸﾞﾘﾗ` 2 (a duel against the Desert Guides (18-20), no conquest), the next week `Iｼｬﾝｸﾞﾘﾗ` 3 (`COM:137-140`) gives the city to Leazas [I: whatever the duel's result] | — |

**Other commands** have no stage-2 test; new options come with the conquests: `Cｼﾊｲﾄｼ`'s places in
the new provinces (Ruins Search at Ancient Ruins (35), Mars's Palace, the Amusement Park, …) and
their Temporary Levy triggers (`:908-1036`), Maria's places in `Cﾃｲﾄ`, the Sabotage targets and the
new ninja Freya, Kaoru, Wichita, Saitou (joining through scenes) in `Cﾆﾝｼﾞｬ`, Pluepet's return in
`Cｼｮｳﾆﾝ` (Portugal taken by force), new harem rows in `Cﾊｰﾚﾑ` — all ported.

**Dungeons** (clickable when their province is held): besides the Monsters' Cave (dungeon 2), the
Thieves' Cave (dungeon 3) and the Isolated Shrine Island (dungeon 15), conquests open the Uncurse
Dungeon (dungeon 10, Hanna (26)), Peach Blossom Spring (dungeon 13, Toushin City (27)), the
Dangerous Hole (dungeon 24, Custom (21)), Titi Lake (dungeon 25, Lazeal (20)), the Hyper Building
(dungeon 26, Red (23)), Helman's Thieves' Hideout (dungeon 1, Golak (34)), Snow Dungeon (dungeon 4)
and Ho Raga's Tower (dungeon 5) (Siberia (38)), Natural Foe's Dungeon (dungeon 6, Cossack (37)),
Kalar Forest (dungeon 12, Laborie (52)), the Giant Battleship (dungeon 16, Path (43)), Mt. Salen
(dungeon 17, Walkland (54)), the Stone-Cutter Dungeon (dungeon 18, Ikoma (53)), the Ancient Ruins
(dungeon 30, by Ruins Search), Orochi's Pit (dungeon 14, Nagasaki (55)), Green Village (dungeon 7,
Maginot Stronghold (72)), Rising Dragon Mountain (dungeon 11, Paritylaon (71)), the Holy Gal's
Dungeon (dungeon 23, Italia (60)), Summit's Mouth (dungeon 8) once moved to Scale (12). All of
`Cﾒｲｷｭｳ` is ported, God's Door included (milestone 3 step 6, 2026-10-11).

**God's Door has no stage test** (`Cﾒｲｷｭｳ.ADV:232-236`): found in the Ancient Ruins (dungeon 30)
with `F_GodsDoor_Quest > 1`, `F_GodsDoor_Found = 0` and Rance holding Chaos (item 1) or Nikkou
(item 2, given by `Iﾏｼﾞﾝ` 3) — `L_04ddc` (`:3500-3827`); the room `L_05437` (`:3855-4497`; the four
statues, items 15-18, set quest 6; Dance) → "Enter God's Door" → `Iｶﾐ` (`:4498-4556`; a scene file
that runs from the player's files, 6528 lines with four battles, it ends only in `&#OVER` — six
sites, no `%0` [C]). The quest steps: Ho Raga with Chaos or Nikkou (`Gﾎﾗｶﾞ:266-270`, Siberia (38)),
British (`Gﾌﾞﾘﾃｨ:1014`), Ho Raga again (`Gﾎﾗｶﾞ:511`). ~~Reachable at stage 2 [I] after about six
Helman conquests and the four statues.~~ **Corrected** ([stage-4-sweep.md](stage-4-sweep.md), "God's
Door and `Iｶﾐ`"): the door opens only at stage 4 — the Bonsai Statue (item 18) comes only from Ho Raga,
with the quest at 4 (Kouhime's hint at the door, which needs Kou past 9) or at stage 4 with Kou past 9
(`Gﾎﾗｶﾞ:290`), and Kou passes 9 only in stage 4's first Creator phase (`Gﾙﾄﾞﾗ:548`); at stage 2 Ho
Raga refuses (quest 3). Run in milestone 5 step 2 (`GodsDoorEndTests`). Ported (2026-10-11): the gate and `L_04ddc`, `L_0539e` and
the room (`:3828-4556`) in `Strategy/DungeonCommand.GodsDoor.cs`; Kouhime's hint (`L_05ea4`, her
state past 9 and the quest at 3) moves the quest to 4.

## End of stage 2

Three places write `StoryStage := 3`:

1. **`Iｼﾚｲﾌﾞ` 6** (`:1526-1711`; also `F_UnitedAudience_Done := 1`): humanity declares war on the
   monsters. `Cﾃｷﾄｼ.ADV:147-157`: an attack at stage 2 on Tower of Evil (78), Grief Valley (89),
   Bone Forest (90) or Tearing Forest (91) plays `Iｼﾚｲﾌﾞ` 5 (Maris refuses, back to the header)
   unless `F_Zeth_War = 2`, `F_Helman_War = 2` and `F_DemonWar_Stage ≥ 10`; then `Iｼﾚｲﾌﾞ` 6 sets
   stage 3 and the attack goes on — `Sﾌﾞﾀｲ`, then `Tｹｲﾌﾞﾘｽ2` (347 lines: Monster Generals
   (commanders 86-89) refilled with four `Y 4` draws, `:12-31`; at Tower of Evil (78) with
   `N_Warg_Stage = 0` `Gﾜｰｸﾞ` 1 replaces the battle, `:92-96`; the gate is fought without walls,
   NAT-21; ported 2026-10-10), then `FIGT_A`. The four are the only
   demon provinces next to human land, reached from the Great Walls of Helman (49, 50) and the
   Maginot Strongholds (72, 76) [I].
2. **`Iﾏｼﾞﾝ` 51** (`:8979-9195`): the demon war's last entry, by the timer alone — no war has to
   be won. With none won [I, timer arithmetic]: `Iﾏｼﾞﾝ` 11 at W+6, one entry every 5 weeks, 20 at
   W+51, 50 at W+61, **51 at W+69** (week 103 with `--seed 2`). Alice's advice (`Iｱﾘｽ`) warns of it.
3. **`Tｹｲﾌﾞﾘｽ2.ADV:38-39`** never runs [I]: every way into it at stage 2 passes `Iｼﾚｲﾌﾞ` 6 first.
   (`StoryStage := 4` is `Iｼﾚｲﾌﾞ` 7, `:168`, after Kayblis dies.)

**Main path** [I: the intended one]: win Helman and Zeth → `Iｼﾚｲﾌﾞ` 4 the next week (the HQ
meeting) → the demon war advances weekly → after `Iﾏｼﾞﾝ` 20 attack a gate → `Iｼﾚｲﾌﾞ` 6.
**Shortest** [I]: about 13 conquests (6 for Helman, 7 to reach all four Zeth towers), one a week:
both wars won about W+13; `Iﾏｼﾞﾝ` 11 at W+6 and 12 at W+11 on the timer, 13-20 weekly to W+20;
the gate attack in W+21.

**The first week of stage 3** opens the Kayblis Empire phase (`MAIN:594-630`, after the player
phase): `Tｹｲﾌﾞﾘｽ:3-8` plays `Gｹｲﾌﾞﾘｽ` 1 (the war council; `F_DemonWar_Stage := 20`,
`N_DemonInvasion_Stage := 100`, `:2105-2106`), then its army logic (`Gﾊﾞﾎﾞﾗ` 1 and Babolat's
(commander 129) attack at once, `Gｹｯｾﾙ` 1, …) — `Tｹｲﾌﾞﾘｽ` is 1855 lines (N3). Closed from then:
the Camilla Unit, the demon war, `Iｼﾚｲﾌﾞ` 4/5/6, `GAL` 1.

## Game over at stage 2

- **Ending 2**, Leazas Castle (1) lost in a `FIGT_B` (`FIGT_B:816-826, 877-882`; Ex (unit 14) may
  rescue it, `:714-735`): `Iﾏｼﾞﾝ` 1's defences, La Seizel's forced attack, the Camilla Unit, Mars
  every week, the AL Church, the armies whose lists end at the castle.
- **Ending 1**, Rance's death (`死亡捕獲:27-31`); also Papaya's trap `Gﾊﾟﾊﾟｲｱ` 2 with Rance in the
  party (`Gﾊﾟﾊﾟｲｱ:4913-4926`).
- **Ending 6**, "Rape Demon Lord" (`Iﾏｼﾞﾝ2` 24).
- **Ending 4**, JAPAN's Shinobu (`TJAPAN:2111-2113, 2373-2374`); **ending 3**, `Gｺｰﾄﾞ` 4 in the Zeth
  war with two or more `F_Kanami_Scene1-4` flags (`Gｺｰﾄﾞ:1444-1459, 1863-2118`).
- Through talks and harem scenes [I: reach not traced]: ending 12 (`Gｶﾁｭｰｼｬ:2068-2294`), ending 7
  ("Kill Miki", `Gｻﾃﾗ:2427-2434, 4573-4587, 5337-5343`), ending 5 (Maria's tulip arc,
  `Gﾏﾘｱ:4244-4864`); God's Door (`Iｶﾐ`): endings 9/10/11/13 and 1.

## `ｽﾀMENU`'s stage starts

With `DebugMode ≠ 0` the title's Start shows "From 1-4" (`ｽﾀMENU:250-266`): From 2 (`:292-303`,
week 12, stage 1, `Iﾊﾝﾗﾝ`), From 3 (`:304-359`, week 16, stage 2, Gold 30000, a preset army, then
`Iｼﾚｲﾌﾞ` 2), From 4 (`:360-546`, week 77, stage 2, both wars and JAPAN won, demon war at 9: just
before stage 3). Not reachable in play [I]: `DebugMode := 1` only at `Iｱﾘｽ.ADV:118-119`, behind a
`Tmp0010 = 10` nothing sets; the hidden title button 11 (`:582-583`, `:805`) is never loaded. So
stage-2 captures start from the owner's own saves (or a harness that sets `DebugMode`, VAR0057 [I]).

## The story spine

| `Iｼﾚｲﾌﾞ` | Lines | Caller (condition) | Does |
|---|---|---|---|
| 10 | 25-148 | `COM:1-5` (week 9) | the first prologue week |
| 1 | 2118-2687 | `COM:91-95` (stage 1) | the first Leazas week; `F_LeazasRebels_State := 1` |
| 2 | 2688-3057 | `FIGT_A:192-195` (Oaks (6) won); `ｽﾀMENU:357-358` | stage 2 (above) |
| 3 | 3058-4465 | `Cﾃｷﾄｼ:456-459` (Custom (21)) | Custom joins without a battle |
| 4 | 1712-2117 | `COM:192-196` (stage 2, both wars won) | the HQ meeting; `F_UnitedAudience_Done := 1` |
| 5 | 1468-1525 | `Cﾃｷﾄｼ:148-153` | Maris refuses the gate attack |
| 6 | 1526-1711 | `Cﾃｷﾄｼ:155-156` | stage 3; the attack goes on |
| 7 | 149-1467 | `Gｹｲﾌﾞﾘｽ:3128-3129` (Kayblis dead) | stage 4, or ending 8 |

| `Iﾏｼﾞﾝ` | Lines | Caller | Does |
|---|---|---|---|
| 1 | 61-1719 | `MAIN:887-892` | the two defences of Leazas Castle (1); `:1569-1719` (after a loss) probably dead [I] |
| 2 | 1720-2094 | `COM:112-116` | the question |
| 3 | 2095-3323 | `COM:107-111` | Nikkou (item 2) to Rance; timer 4 |
| 11-19 | 3324-8180 | `MAIN:949-984` | the demon war, stages 1-9 |
| 20 | 8181-8769 | `MAIN:945-948` | stage 10 |
| 50 / 51 | 8770-8978 / 8979-9195 | `MAIN:941-944` / `:937-940` | stage 11 / `StoryStage := 3` |
| 300-304 | inside 3, 19, 14, 17, 16 | `CG.ADV` | gallery replays |

## Not ported yet, reached at stage 2

| What | Lines | Reached |
|---|---|---|
| `Tﾍﾙﾏﾝ2`, `Tﾍﾙﾏﾝ` | 377, 792 | the first attack on Log B (31) / Log A (32) — main path (both ported 2026-10-10) |
| `Tｾﾞｽ2`, `Tｾﾞｽ` | 401, 777 | the first attack on Adam's Fortress (58) — main path (both ported 2026-10-10) |
| `Tﾊﾟｯﾄﾝ` | 226 | after Helman falls (`Gﾊﾟｯﾄﾝ` 6) — main path (ported 2026-10-10) |
| `Tｹｲﾌﾞﾘｽ2` | 347 | the gate attack after `Iｼﾚｲﾌﾞ` 6 (at stage 3, inside the same command) — ported 2026-10-10 |
| `Cﾃｷﾄｼ` independent cities | `:429-3828`, 3400 | from W+1 (Rockearth (17), Zeo (24), Hanna (26), Shangri-La (29)) — ported 2026-10-10/11 |
| `TJAPAN2`, `TJAPAN` | 219, 2851 | after Portugal (28) (both ported whole 2026-10-11, Gekkou's scenes `:295-2744` included) |
| `Cﾒｲｷｭｳ` God's Door | `:233-236`, `:3500-4556`, ~1060 | after the Ancient Ruins (35) and the quest (ported 2026-10-11; `Iｶﾐ` from the player's files) |
| `Tｹｲﾌﾞﾘｽ` | 1855 | the first week of stage 3 only |

The week, `COMMAND`, the other command files, the pickers and every scene are ported or run from
the player's files — but `FIGTATTK` (the battle's animation, which the scene `FIGT表示` calls from
`Iﾏｼﾞﾝ` 12-18 and `Gﾊﾟｯﾄﾝ` 2) was neither until 2026-10-11, when it was routed to the battle's port
(found by the stage-3 sweep). In interactive Godot an unported `T*2` is skipped with a notice
(`GameScreen.UnportedFiles`), which leaves the war flags unset; `--auto` stops there.

## Open questions

1. The AL Church attack: `GAL` 9 fills `Tmp0001-0004` with the Temple Knights (commanders
   139-142), but `Cﾃｷﾄｼ` calls `L_04f2a` (which copies them into the enemy party) only for Patton
   (`:350-353` against `:375`), so the battle may fight the previous battle's enemy party, or the
   city may be taken unopposed (`:395`) — a quirk candidate next to CMD-19; a capture would settle it.
2. Shangri-La (29) after a lost duel: still taken the next week (`COM:137` does not test the
   result)?
3. Does taking a Zeth tower kill or capture its minister (they are also units 38-40)? It sets the
   Zeth path's length. Answered 2026-10-23 from `Tｾﾞｽ2:91-130` and its parties `:143-217`,
   `FIGT.ADV:320-326` (docs/battle/01-flow.md §7), `死亡捕獲:181-219, 237-255, 256-260`: it kills her —
   always. While the minister lives she defends her own tower (Chizuko (commander 101) third behind 103,
   102; Papaya (104) behind 106, 105; Nagi (107) behind 108 and Ragarl (109); Magic (110) behind 112, 111),
   retreat mode 1, and an attack is won only by annihilation (a timeout loses it), so the win that takes the
   tower leaves her at status 0: Chizuko, Papaya and Magic stay dead after any won battle (`Gﾁｽﾞｺ` 2,
   `Gﾊﾟﾊﾟｲｱ` 4, `Gﾏｼﾞｯｸ` 23), Nagi only at Nagi's Tower (67) (`Gﾅｷﾞ` 6; anywhere else she is revived at
   HP 1). At Magic's Tower (66) with Alex (commander 125) alive (`Gﾏｼﾞｯｸ` 1 → `Tmp0001 = 1`) the party is
   Alex and 126-128 without Magic, and Alex's death there plays `Gﾏｼﾞｯｸ` 2, which sets Magic to status 0
   (`Gﾏｼﾞｯｸ:5808-5809`). The death scenes give no status back (only `Gﾅｷﾞ` 3, the Ragarl ritual from
   `Tｾﾞｽ:8-10`, sets Nagi and Ragarl to 1, `Gﾅｷﾞ:3424-3427` [I: whether it can follow her death is not
   traced]); the only taking-in is Nagi
   (unit 40) in `Gﾅｷﾞ` 6's choice when Shizuka (unit 17) is in the party with `VAR0069 = 1` (Shizuka leaves,
   `Gﾅｷﾞ:6455-6459`). So the four towers are the four deaths `Iｾﾞｽ` 100 needs.
4. What `RetVal` holds after the Camilla Unit's `FIGT_B`, which `MAIN:798-806` tests. Answered
   2026-10-23 from `FIGT_B:253, 714-826, 830-866`, `M.ADV` `CallArg = 4` → `WAO_MAP:116-122, 183-196`,
   `WAO_SUB:292-294` (a key wait keeps `RetVal`): it is never Camilla's (commander 91) roll after her own
   battle. Won: 1 (`:253`). Lost: 0 — `L_01165` sets 2 (`:836`), but the map's marker call `M` 4 (`:861-862`)
   then looks the province's marker up and leaves 0 (every province has one from `SET_MAP`), 1 only if none
   is found. Abandoned ("Abandon City", `:714`): 2 (`:791`), so `Gｶﾐｰﾗ` 1 or 2 plays right after the
   battle. A scene `死亡捕獲` plays (a fallen unit's, `Gｴｸｽ` 6) may leave another value. The port does the
   same (`Combat/Battle.Flow.Defence.cs`, `Script/OriginalScriptContext.Map.cs` `MapFindMarker`). STR-02
   holds while she lives too.
5. Is `Iﾏｼﾞﾝ` 1's `:1569-1719` ever reached? Can Satella (29) or Meglass (30) join after "Rape
   Demon Lord"? Are endings 5 and 12 reachable at stage 2? Answered 2026-10-23: (a) No — `L_01cc0`
   (`:1569`, untranslated Japanese) is reached only on `Bt_Result = 1` after a `FIGT_B` of Leazas Castle
   (1) (`:80-83`, `:596-599`), and both ways to it — the lost battle (`FIGT_B:830-882`) and "Abandon City"
   (`:714-826`) — end the game there with ending 2 (`&#OVER`) at `Prov_Cur = 1`; `FIGT` never writes
   `Prov_Cur`. Dead code. (b) No — they join only in `Gｻﾃﾗ` 1 (`Gｻﾃﾗ:4401-4404`), called only by
   `MAIN:742-746` at `N_DemonInvasion_Stage = 4` inside the Camilla Unit block, which needs the stage ≠ 100;
   "Rape Demon Lord" sets 100 (`Gﾐｷ:3754`) and nothing lowers it (its writers: `Iﾏｼﾞﾝ:84, 2054, 2096`,
   `Gｻﾃﾗ:3642`, `MAIN:809, 894`, the boot), so `MAIN:986-993` never resets the war and ending 6 follows.
   (c) Yes, both: ending 12 is `Gｿﾙﾆｱ` 10 (a harem visit) with Saulnia's state 2-8 and Katyusha's > 1 →
   "Have Threesome" (`Gｿﾙﾆｱ:1349-1366`) → `Gｶﾁｭｰｼｬ` 100 → ending 12 unless Sill (unit 2) is back
   (`Gｶﾁｭｰｼｬ:1844-2068, 2293`); both witches are the Helman 5th Army's (`Tﾍﾙﾏﾝ:450-454`, `Tﾍﾙﾏﾝ2:239-244`,
   captured through `死亡捕獲:148-161`), and Sill is away from `Iﾊﾝﾗﾝ:35-36` until her rescue. Ending 5 is
   `Cｼﾊｲﾄｼ`'s Tulip Research (`:87-93`: Maria (unit 16) alive, her lab built in `Cﾃｲﾄ`, which needs
   `F_Maria_Tulip > 1`) → `Gﾏﾘｱ` 1: each visit adds 1 to the tulip count (`:3463-3465`), at 9 or 13 (unless
   `F_Maria_Flag2 = 2`) "Look" → ending 5 (`:3846-3857, 4098-4109, 4244-4864`); no stage gate, so from
   stage 1 on.
6. Use Force on a city whose listed commanders are all dead goes to `FIGT_A` with an empty enemy
   party (no unopposed exit there) — can it happen? Answered 2026-10-23 from `Cﾃｷﾄｼ:3649-3828` and
   `L_04f2a`, `FIGT.ADV:320-326`, docs/battle/01-flow.md §9.3, `死亡捕獲` (docs/battle/01-flow.md §11.4):
   only through a lost Use Force whose last defender dies in the same exchange as the player's last unit
   (Rance (unit 1) not in the party, or the game ends): the step tests the player's side first, so the
   battle is lost (`Bt_Result = 1`) with every defender at status 0, and `死亡捕獲` revives none of the
   cities' commanders (8-17, 63-71). Any other last kill wins and takes the city; a successful negotiation
   zeroes them but takes it too; no other battle fights them while the city is independent (Mars
   (commander 71) only after Mars (22) is taken, `Gｶｾｲ:2750, 3089`; the Desert Guides (18-20) only in
   `Iｼｬﾝｸﾞﾘﾗ` 2, Shangri-La never reaching the menu, `Cﾃｷﾄｼ:434-442`). The next Use Force then puts
   nobody in the party and `FIGT`'s first step goes to "the enemy was annihilated" (`:324-326`): the city
   is taken unopposed [I: by the flow; not run].
