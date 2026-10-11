# What stage 4 reaches (`StoryStage = 4`, swept 2026-10-11)

The sweep behind milestone 5 (owner's decision 16 in [README.md](README.md)); the sequel of
[stage-3-sweep.md](stage-3-sweep.md). Lines refer to `_extract\scenario_named` (assignments read
`!Var : n!`, `\` is ≠, `*` binds before `+`). [I] marks an inference; everything else was read.
**S4** is the week Kayblis (commander 135) dies and `Iｼﾚｲﾌﾞ` 7 moves the story to stage 4;
**phase k** is the k-th Creator phase (phase 1 runs in S4). Owners as before (1 Leazas, 8 Kayblis,
10 Angels — `Ev_PhaseNation` 10 becomes `Prov_Owner` when the Creator's army takes a province).

**Stage 4 is the last stage.** Nothing writes `StoryStage` past 4 (`Iｼﾚｲﾌﾞ:168` is the only write of
4; no other value follows). Every week the Creator phase (`Tﾙﾄﾞﾗ`) sends Angel Knights against five
random player provinces and ruins two with an earthquake; the player may not attack any more
(`Cﾃｷﾄｼ:109`), so what is lost stays lost. The game ends only in `&#OVER`: Leazas Castle (1) lost
(ending 2), Rance's death (ending 1), or God's Door → `Iｶﾐ` (endings 9, 10, 11, 13) — and **God's
Door can open only at stage 4**, only if Kou's arc stood at state 8 when the Creator first appeared
(below). The one rule file stage 4 adds is `Tﾙﾄﾞﾗ` (205 lines); `OVER` stays `GameEnd` (W4).

## Start

- **`Gｹｲﾌﾞﾘｽ` 10** (`:2110-3130`, from `死亡捕獲:362-366` when Kayblis's status is 0): Kaybwan's and
  Kaybnyan's states to 6 (`:2111-2112`), `Prov_Cur := 79` for the map (`:2114`), text branching on
  Sill (unit 2) (`:2310-2436`); unless Satella (unit 29) is gone, `VAR0386 := 1` (`:2437-2550`) and,
  with `F_Silky_State = 8`, `F_Hornet_Joined := 1` (`:3109-3110`; the text "Hornet joined Rance"
  before it is unconditional, the flag needs Silky) → `Iｼﾚｲﾌﾞ` 7 (`:3128-3129`).
- **`Iｼﾚｲﾌﾞ` 7** (`L_002fb`, `:149-238`): counts the provinces 1-91 but Shangri-La (29) and Nothing
  (75) not owned by Leazas (`:150-163`); any → ending 8 (`L_0049f`, `:239-1467`); none →
  `StoryStage := 4` (`:168`), then text only (the audience room, `CG 1046`: Kayblis defeated, the world
  Rance's, "then there was peace", `:169-237`), `%0`. The loop leaves `Prov_Cur = 92` (`:154`), which
  `死亡捕獲` reads after the return (`:380` Camilla's castle test, `:425` Leila's `Prov_Cur ≠ 1`) until
  `FIGT_A:190` / `FIGT_B:275` restore it from `Bt_TargetProv` (stale cursor, candidate below).
- **State at stage 4**: no other write. `F_DemonWar_Stage = 20` and `N_Camilla_Stage = 100` (from
  `Gｹｲﾌﾞﾘｽ` 1 in week S of stage 3 [I: every way to Kayblis's death passes a Kayblis phase]),
  `N_Rudrasaum_Turns = 0`, every province 1-91 but 29 and 75 Leazas's.

**Which phase.** Kayblis fights in two places only (stage-3-sweep, "End of stage 3"):

1. **The player's attack on the Demon Lord's Castle (79)** (`Cﾃｷﾄｼ` → `Tｹｲﾌﾞﾘｽ2:124-143` →
   `FIGT_A`), the main path [I]. `FIGT_A` gives the castle to Leazas on a win (`:103-107`) *before*
   `死亡捕獲` (`:189`), so a won battle counts it; Kayblis dead but the battle lost leaves it his →
   ending 8. S4 then goes on after `COMMAND` (`MAIN:213`): the post-command scenes (`:214-228`), Mars,
   the AL Church, JAPAN, Helman, Patton, Zeth if their flags are on, **no Kayblis phase** (`:594`
   needs stage 3), no Camilla Unit (`:631`, `N_Camilla_Stage = 100`), the **Creator phase** (`:816`,
   phase 1), La Vaswald, the week's end.
2. **His own army at war** (`Tｹｲﾌﾞﾘｽ:1333`, `FIGT_B`), with the Demon Lord's Castle (79) already the
   player's [I: reachable only if it was taken with Kayblis alive]. The rest of that Kayblis phase
   runs (stage-3-sweep: his army's other marches, `Gｹｲﾌﾞﾘｽ` 3, the Assault Leaders — nothing in
   `Tｹｲﾌﾞﾘｽ` tests the stage), then the Creator phase in the same week. A province Kayblis takes after
   the count stays his for good: no Kayblis phase runs again and the player may not attack [I].

## The week (`MAIN.ADV`, ported whole)

At stage 4, besides what stage 3 has:

- **The Leazas phase** (`:83-212`) unchanged: income is the sum of `Prov_Economy` over owner 1
  (`:113-126`), so every province the Angels take or an earthquake ruins costs income; expenses as at
  stage 3 (M Land, AL Church before stage 19, Galtia's food).
- **The nation phases** are gated on their own flags, not on the stage: Mars (`:229`, `Gｶｾｲ` 2), the
  AL Church (`:260`, `GAL` 5), JAPAN (`:348`), Helman (`:401`), Patton (`:454`), Zeth (`:507`). With the
  whole map Leazas's at S4 the wars are normally over [I: Lang Bau (45) won at war plays `Gﾐﾈﾊﾞ` 5 →
  `F_Helman_War := 2`, `FIGT_A:196-198`, `Gﾐﾈﾊﾞ:1708`; Osaka (56) won plays `IJAPAN` 6,
  `FIGT_A:162-165`, which leads to `F_Japan_State := 10`, `IJAPAN:6198`]; one still on runs as at
  stage 3, on ported files or scenes. **Closed:** the rebels (`:298`, stage 1), Kayblis (`:594`,
  stage 3 only), the Camilla Unit (`:631`). **Open:** the **Creator** (`:816-846`, `Tﾙﾄﾞﾗ`, the one rule
  file not ported) and La Vaswald (`:848-883`, `Gｻｲｾﾞﾙ` 12, while La Vaswald (commander 98) lives).
- **The Creator phase's frame** (`:816-846`): BGM 19, map op 0 only (no camera on a province, unlike
  every other phase), the banner "Creator Phase", `Ev_PhaseNation := 11` (dead: `Tﾙﾄﾞﾗ:6` writes 10,
  STR-13), `MS 10, "Creator"` (the army name `FIGT_B` prints), `%#Tﾙﾄﾞﾗ`.
- **The week's end:** `Iﾏｼﾞﾝ` 1 (`:887`, stage 2) closed; the Sill chain (`:896`, `F_Helman_War ≠ 2`)
  and `Gﾏｼﾞｯｸ` 5 (`:926`) as before; the demon war (`:936-985`: `F_DemonWar_Stage = 20` matches none of
  0-11) and the Kayblis war (`:986`, `F_Kayblis_War = 1`) closed.

**`COM` at stage 4** (`COMMAND:3`, at the start of the player phase, so S4's `COM` ran at stage 3;
none of these calls a rule file):

- Tier 1 (before the one-a-week exit `:301-303`): **`Gｺｳﾋﾒ` 6** (`:268-272`, `F_Kou_State = 10`, set
  by `Gﾙﾄﾞﾗ` in S4): `F_Kou_State := 11` (`Gｺｳﾋﾒ:29`), Rance resolves to kill Rudrasaum and remembers
  God's Door in the Ancient Ruins (text, `:28-315`); marks the week. `Gｺｳﾋﾒ` 5 (`:217`, Kou at 7 → 8,
  `Gｺｳﾋﾒ:2580`) has no stage test. `Gﾜｰｸﾞ` 2 (`:96`) stays possible in S4+1 if the attack that killed
  Kayblis was the first on the Demon Lord's Castle (79) and Warg never joined [I].
- Tier 2: **`Gﾒﾅﾄﾞ` 4** (`:305-309`: `N_Rudrasaum_Turns > 2` — from S4+2 on —, `F_Menad_State < 7`,
  Menad (unit 10) present): Menad (unit 10) to status 0, `F_Menad_Fate := 2` (`Gﾒﾅﾄﾞ:58-475`, text);
  **`Gﾐｷ` 11** (`:552-556`: Miki (unit 32) present, `VAR0386 ≠ 0`, not seen; `Gﾐｷ:43-252`, text);
  **`Gｻﾃﾗ` 3** (`:817-821`: Satella (unit 29) present, `VAR0386`, `F_Hornet_Joined`, not seen;
  `Gｻﾃﾗ:51-199`, Hornet and Silky in Satella's room, text). `VAR0386` is written only at
  `Gｹｲﾌﾞﾘｽ:2550`, so `Gﾐｷ` 11 and `Gｻﾃﾗ` 3 are stage-4 scenes in practice.
- Closed since stage 3: `Gﾏﾘｽ` 17 (`:284`), `Gﾐｷ` 6/9 (`:566`, `:574`). Still open: `Gﾏﾘｽ` 12 (`:775`,
  stage > 1), `Gｻｲｾﾞﾙ` 9 (`:694`), `Gﾎﾗｶﾞ` 3 (`:898`, a text visit), the rest as at stage 3.

## The Creator phase (`Tﾙﾄﾞﾗ.ADV`, 205 lines)

survey-nations §2.1/§2.2/§2.4/§5/§7 has the labels, the army and the draws; checked against the
file — all confirmed; what they leave out is marked new below. Shape:

| Lines | Label | What |
|---|---|---|
| 1-4 | — | `StoryStage = 4` and `N_Rudrasaum_Turns = 0` → `Gﾙﾄﾞﾗ` 1 (phase 1 only; the stage test is redundant, `MAIN:816` gates the call) |
| 5-6 | — | `N_Rudrasaum_Turns + 1`; `Ev_PhaseNation := 10` |
| 7-16 | — | five times: the search `L_00255`, then the attack `L_00093` |
| 17-20 | — | twice: the search, then the earthquake `L_000f8`; `%0` (`:21`) |
| 22-40 | `L_00093` | `Prov_Cur = 0` → return; Angel Knight (commander 146) and Angel Knight (commander 147) in slots 1-2, each `Cmdr_Status := 0`, HP and troops to their maximum (3500 troops, 17 HP at boot, `SET_CHR:5816-5869`); retreat mode 1; `FIGT_B` |
| 41-82 | `L_000f8` | `Prov_Cur = 0` → return; map scroll to the province (`M` op 1), its name (`MM 8`), its picture and "［name］" (key wait); the jitter ×4 (dead); `Prov_Economy := 0`, `Prov_Defense := 0`, `Prov_BattleArea := Prov_BattleArea * 2`, `Prov_BgCg := 1043` (`:66-69`); picture 1043 and "Suddenly, a great earthquake brought [name] to ruin." |
| 83-104 | `L_001d4` | the earthquake jitter: `\0:` first (`:84`), so its `Y 4, 4` (`:85`), the ±4 moves of `Prov_MapX/Y` and the `M` call never run (NAT-16) |
| 105-131 | `L_00255`, `L_00259` | the target search (below) |
| 132-143 | — | unlabelled window helpers 111/211/112/212 — dead (EVT-01) |
| 144-205 | `L_00337`, `L_003fb`, `L_00409`, `L_00417`, `L_00424` | live helpers: `WAO_SUB` 113, 331 (centre picture `CallArg`), 332, 0, 1; the ops after each first `\0:` (`:148-186`, `:203-205`) are dead (EVT-01) |

**The search** (`:105-131`), seven times a phase: `Tmp0001` counts tries; at the 20th it gives up
(`Prov_Cur := 0`), so at most 19 draws of `Y 4, 91` (`:113`). A draw of Nothing (75) gives up at once
(`:115-118`) — no retry (new; cf. Piar's NAT-15). With `N_Rudrasaum_Turns > 5` a second draw
`Y 4, 2` = 1 replaces the province with Leazas Castle (1) (`:119-124`). With `N_Rudrasaum_Turns < 5`
Leazas Castle (1), Siberia (38) and Ancient Ruins (35) are drawn again (`:125-127`). A province of
owner 1 is the target (`:128-130`); any other is drawn again (`:131`). Shangri-La (29) can be a target
when it is the player's.

**The counter** (new): `Gﾙﾄﾞﾗ` sets `N_Rudrasaum_Turns := 1` (`Gﾙﾄﾞﾗ:5`) before `Tﾙﾄﾞﾗ:5` adds 1, so
phase k runs with k + 1:

| Phase | Week | Turns | Leazas Castle (1), Ancient Ruins (35), Siberia (38) spared | Castle roll |
|---|---|---|---|---|
| 1 | S4 | 2 | yes | no |
| 2 | S4+1 | 3 | yes | no |
| 3 | S4+2 | 4 | yes | no |
| 4 | S4+3 | 5 | no | no |
| 5 on | S4+4 on | 6 on | no | yes: about half of the 7 searches end on Leazas Castle (1) [I: arithmetic, 90/91 × 1/2 per first try] |

The three spared provinces are the way out [I: the design]: Siberia (38) holds Ho Raga's Tower
(dungeon 5), Ancient Ruins (35) the Ancient Ruins (dungeon 30) with God's Door, and a dungeon is
entered only from an own province (survey-dungeon §2, `COMMAND:151`, `Cｼﾊｲﾄｼ:547`). From phase 4 the
Angels can take them, and from phase 5 Leazas Castle (1) is attacked about 2-3 times a week and
quaked about once [I], each quake zeroing its walls and doubling its battle area.

**The Angel Knights' battles** (`FIGT_B`, all ported): "the Creator appeared around …"; Satella
(unit 29) and Meglass (unit 30) fight them (`FIGT_B:109-150` refuse only for phases other than 8 and
10); a battle inside the city adds a tenth to `Prov_BattleArea` (`:185`); lost or abandoned →
`Prov_Owner := 10` (`:772`, `:857`), Leazas Castle (1) → ending 2 (`:808-826`, `:867-882`; Ex (unit 14)
may save an abandoned castle, `:721-734`). A unit defends once a week (`Sﾌﾞﾀｲ` takes status 1 and
sets 2, `Sﾌﾞﾀｲ:192-204`; `MAIN:151` resets), so five attacks need five free units, else the province
must be abandoned (`FIGT_B:233-235` loops back to the menu on an empty party) [I]. In battle the
Knights strip a player demon's commander shield (`FIGT.ADV:2422-2424`) and at stage 4 every enemy
speaks commander 200's lines (BTX-08, BSC-01) — both ported.

**`Cmdr_Status := 0`** (NAT-11, survey-nations §9 Q5): no effect — `FIGT` never reads `Cmdr_Status`
(`FIGT.ADV:1906` is its only use, a write) and `死亡捕獲` tests neither commander 146 nor 147
(checked by grep: their only other readers are `FIGT.ADV:2422`, `FIGT_M:7315-7323`,
`FIGTﾃｷDE:1424-1433`, all on `Cmdr_Cur`).

**Draws:** two live sites, `:113` (`Y 4, 91`, up to 19 a search) and `:120` (`Y 4, 2`, only with
Turns > 5), seven searches a phase, interleaved with up to five `FIGT_B` battles and their draws
(`FIGT_B:186`, `:282` for Elena's scenes in provinces 31-49); `:85` dead. The earthquake draws nothing.

**`Gﾙﾄﾞﾗ`** (803 lines, one entry; a scene from the player's files, calls only `M` and `WAO_SUB`):
Rudrasaum under the continent, bored by Rance's peace, sends the Angel Knights; then Leazas Castle.
Kou's branch (new for the port's purposes):
- `F_Kou_State = 8` → `L_009d8` (`:547-723`): `F_Kou_State := 10` (`:548`), Kou's dream of Rudrasaum.
- any other nonzero state (`:468-542`): `F_Kou_State := 0`, `F_Kouhime_Dead := 2` (`:469-470`) —
  Kouhime killed herself; she leaves the harem (`Cﾊｰﾚﾑ:198`) and `OVER`'s epilogue changes (`OVER:1372`).
- 0: nothing.

**`Tﾙﾄﾞﾗ2`**: one line, `%0:`, called by nothing. `Cﾃｷﾄｼ`'s defender dispatch (`:317-376`) has no
owner-10 branch, and at stage 4 an attack never gets there (`:109`). Owner 10 is only named
(`Cﾃｷﾄｼ:4398`, `Cﾃｷﾄｼ2:101`).

**Against survey-nations** (all confirmed): §2.1 row and `Ev_PhaseNation` 10, §2.2 labels, §2.4
(Angel Knights, full troops, status 0, retreat mode 1, five attacks), §3 (`FIGT_B` 1 site, `M` 2 —
one of them, `:101`, dead), §4 (`:66-69`), §5 (`:113`, `:120`, `:85` dead), §7 #11/#13/#16. Not in
the survey: the Nothing (75) abort, the counter's start at 2 and the phase-4 gap, the three spared
provinces. `docs/variables.tsv`'s `N_Rudrasaum_Turns` ("> 5 enables random attacks") is wrong: the
attacks run from phase 1; > 5 adds the castle roll (corrected in milestone 5 step 1).

## The player's commands

**`COMMAND` and the command files have no stage-4 gate** but one: `COMMAND` tests `StoryStage` at
`:4`, `:183`, `:419`, `:516` only; `Cﾒｲｷｭｳ` at `:4`, `:172`, `:179`, `:2757` (stage 0 or not);
`Cﾊｰﾚﾑ:6, 39`, `Cﾌﾞｶ:716`, `Cﾃｷﾄｼ2:48` likewise. **`Cﾃｷﾄｼ:109`**: Attack at stage 4 → `L_04b8a`
(`:4059-4115`), Maris refuses ("chaos all over the country … this isn't the time to be attacking")
and the menu closes (`@L_00205`) before `Ui_CmdAttackAvail` is tested (`:112`), so the week's attack is
not spent. Ninja Maneuver (`Cﾆﾝｼﾞｬ`) stays and writes no owner. So the provinces the Angels take
(owner 10) — and any Kayblis took after the count — are never won back.

Dungeons, the harem, merchants, construction, treasure, All Data: as at stage 3, on own provinces.
Kou's harem night at stage 4 plays its own dream (`Gｺｳﾋﾒ:1613-1614` → `L_020be`, text).

## God's Door and `Iｶﾐ` — the end of the game

**God's Door opens only at stage 4** (new; it corrects stage-2-sweep's "[I] reachable at stage 2").
It opens with four statues (`Cﾒｲｷｭｳ:4003-4005`, `4023-4025` → `L_057b6`, `F_GodsDoor_Quest := 6`,
`:4052`). Three have no stage condition: Gourd Statue (item 15) from British (`Gﾌﾞﾘﾃｨ:1976`), Monkey
Statue (item 16) from Cafe (`Gｶﾌｪ:545`, `:2351`), Flower Statue (item 17) from KD (`Cﾒｲｷｭｳ:2745`). The
**Bonsai Statue (item 18)** comes only from Ho Raga (`Gﾎﾗｶﾞ:507-508`; every `Item_Cur : 18` checked):
his menu item "Get Gold Statue" (`:276-282`: item 18 not held, quest > 1, Rance leading the party)
leads to `L_00578` (`:289-295`), which hands it over only with `F_GodsDoor_Quest = 4` or
`StoryStage = 4` and `F_Kou_State > 9` (`:290`); quest 2 → 3 is his refusal (`L_0096e`, `:510`), 3
another refusal (`:296-357`). Quest 4 is Kou's hint at the door (`Cﾒｲｷｭｳ:3874-3876` → `L_05ea4`,
`:4411-4412`), which also needs `F_Kou_State > 9`. And `F_Kou_State` passes 9 only in `Gﾙﾄﾞﾗ:548`
(10) and `Gｺｳﾋﾒ:29` (11, after 10) — that is, in S4's Creator phase, with Kou at 8.

| Quest | Set by | Needs |
|---|---|---|
| 1 | `Gﾎﾗｶﾞ:1492`, `:1734` (Ho Raga's Tower (dungeon 5), Siberia (38)) | Rance holding Chaos (item 1) / Nikkou (item 2) |
| 2 | `Gﾌﾞﾘﾃｨ:1014` (British) | quest 1 and Chaos or Nikkou |
| 3 | `Gﾎﾗｶﾞ:511` (Ho Raga refuses) | quest 2 |
| 4 | `Cﾒｲｷｭｳ:4412` (Kou's hint before the door) | quest 3, `F_Kou_State > 9` (stage 4) |
| 5 | `Gﾎﾗｶﾞ:359` (Bonsai Statue (item 18)) | quest 4, or quest > 1 at stage 4 with `F_Kou_State > 9` |
| 6 | `Cﾒｲｷｭｳ:4052` (door open) | all four statues placed |

The door itself is found in the Ancient Ruins (dungeon 30) with quest > 1 and Rance holding Chaos
or Nikkou (`Cﾒｲｷｭｳ:233-236`, ported). Kou reaches 8 through her own arc: saved at JAPAN's fall
(`IJAPAN:5565`, state 3; the other choice kills her, `:5534`), four harem nights (4-7,
`Gｺｳﾋﾒ:338, 2006, 2233, 2355`), then `COM:217` (`Gｺｳﾋﾒ` 5, 8). **With Kou not at 8 when `Gﾙﾄﾞﾗ` plays,
no Bonsai Statue exists, God's Door stays shut, and stage 4 can only be lost** [I: no other exit;
endings 12, 7, 5 below are not stage-bound]. Shortest winning run: S4 `Gﾙﾄﾞﾗ` (Kou 10), S4+1 Ho
Raga's Tower with Rance leading (`Gﾎﾗｶﾞ:290` needs no hint), S4+2 God's Door — inside the three
spared phases if the other statues are in hand.

**Run** (2026-10-11, milestone 5 step 2, `GodsDoorEndTests`, content, from Kayblis's death with the quest
at 2, Rance holding Chaos (item 1), the other three statues in stock): the shortest run holds as written,
to ending 11. Ho Raga's Tower is clicked on the map (`COMMAND:149-151`, Siberia (38)); its gate opens to
units 1, 3, 7, 14, 21, 31 (`Gﾎﾗｶﾞ:4-6`), "Get Gold Statue" shows only with Rance (unit 1) searching. The
Ancient Ruins (dungeon 30) are entered from the city's menu, Ruins Search (`Cｼﾊｲﾄｼ:227-232` →
`L_00c9e`, `:538-547`, which writes dungeon 30's record each time, `Dgn_State := 0`), so `Prov_Cur` is 35
when `Iｶﾐ` starts. Leaving God's Door and then the dungeon's menu gives the week's search back
(`Cﾒｲｷｭｳ` `L_008bf`): Ho Raga's Tower can follow the door in the same week. With Kou at 7 or 0 when
`Gﾙﾄﾞﾗ` plays, Ho Raga refuses twice (quest 2 → 3, then `:296-357`), no hint comes at the door and it
stays shut with three statues; with Kou at 11 and the quest at 3, the door first gives the hint (quest 4)
and Ho Raga the statue after it.

**`Iｶﾐ`** (6528 lines; a scene from the player's files, entered from `Cﾒｲｷｭｳ:4556`, never returns;
never run end to end; first look, **corrected by [survey-ikami.md](survey-ikami.md)** (decision 17):
Rudrasaum is on 6F, the way back has 10 battles (20 in all), "each rearguard below" is the garrisons
left above, and a garrison wiped in its last fight is never flagged):

- Floors 1F-5F down to Rudrasaum (`:1-1062`); at 2F-5F Rance leaves a rearguard picked with
  `Sﾌﾞﾀｲ` (`L_0577f`, `:4813-4886`; Rance himself refused `:4838-4885`;
  `Ev_GodRear2F-5F_Unit1-4`). Every floor passed
  re-fights each rearguard below against its kaiju — Kaiju Gaga (commander 187), Kaiju Zeze (188),
  Kaiju Dada (189), Kaiju Bobo (190) — with `FIGT` (`Bt_Kind 3`, retreat mode 6; `L_05243`-`L_05630`,
  `:4561-4812`): 10 battles going down (`:205`, `:426-427`, `:586-588`, `:776-779`). A wiped rearguard
  sets `Tmp0226-0229` and plays `FIGT表示` 21-24. No `Y 4` in the file.
- Before Rudrasaum (`:1249-1275`): Charge → ending 9 (`:1335`); Think (once: "Create Chaos"; twice
  with `N_Warg_Stage > 1`, from `Gﾜｰｸﾞ:3525`: "Leave it to Warg"); Create Chaos → Sill (unit 2) at
  status 1 → ending 10 (`:1767-1769`, `:2777`/`:3723`), else ending 9 (`:1810`); Leave it to Warg →
  the retreat (`:2261-2617`), 10 more kaiju battles (`:2269-2272`, `:2301-2303`, `:2332-2333`, `:2362`), each
  floor's rearguard checked: one lost → ending 9 (`L_03043`, `:2649-2775`); all held → Sill (unit 2)
  present → ending 11 (`:2613-2615`, `:3726-3728`), else ending 13 (`:4313-4315`).
- Its rule calls: `FIGT` ×4 sites, `Sﾌﾞﾀｲ`, `FIGT表示` ×4 (a scene), `&#OVER` ×6 — all ported or
  scenes but `OVER` (`GameEnd`).

## Game over at stage 4

- **Ending 2**, Leazas Castle (1) lost or abandoned in a `FIGT_B` — the Angel Knights from phase 5 on
  (and by the ordinary draw in phase 4), La Vaswald, Mars, the AL Church if theirs run.
- **Ending 1**, Rance's death (`死亡捕獲:27-31`) in a defence or a dungeon battle (`Iｶﾐ`'s `FIGT` calls
  skip `死亡捕獲`, and Rance never stays in a rearguard).
- **`Iｶﾐ`**: endings 9, 10, 11, 13 (above). `OVER` counts a clear for 5, 8, 9, 10, 11, 13 (`OVER:1-5`)
  and quits the program (`:2083`, STR-10).
- Not stage-bound, as before [I: reach not traced]: ending 12 (`Gｶﾁｭｰｼｬ`), 7 (`Gｻﾃﾗ:5342`), 5 (`Gﾏﾘｱ`);
  3 and 4 only with the Zeth war or JAPAN still on; 6, 8 and 14 closed.

## The story spine at stage 4

| Scene | Lines | Caller (condition) | Does |
|---|---|---|---|
| `Gｹｲﾌﾞﾘｽ` 10 | 2110-3130 | `死亡捕獲:362-366` (Kayblis dead) | `VAR0386`, `F_Hornet_Joined`; `Iｼﾚｲﾌﾞ` 7 |
| `Iｼﾚｲﾌﾞ` 7 | 149-238 | `Gｹｲﾌﾞﾘｽ:3128-3129` | `StoryStage := 4` (or ending 8) |
| `Gﾙﾄﾞﾗ` 1 | 1-723 | `Tﾙﾄﾞﾗ:1-4` (phase 1) | `N_Rudrasaum_Turns := 1`; Kou 8 → 10, else Kou dies |
| `Gｺｳﾋﾒ` 6 | 28-315 | `COM:268` (Kou 10) | Kou 11; Rance turns to God's Door |
| `Gﾒﾅﾄﾞ` 4 | 58-475 | `COM:305` (Turns > 2) | Menad (unit 10) leaves |
| `Gﾐｷ` 11, `Gｻﾃﾗ` 3 | 43-252, 51-199 | `COM:552`, `:817` (`VAR0386`) | text |
| `Gﾎﾗｶﾞ` 1 | 105-509 | `Cﾒｲｷｭｳ:434` (Ho Raga's Tower (dungeon 5)) | Bonsai Statue (item 18), quest 5 |
| God's Door | `Cﾒｲｷｭｳ:3828-4556` | the Ancient Ruins (dungeon 30) | statues, quest 6, `Iｶﾐ` |
| `Iｶﾐ` | 1-6528 | `Cﾒｲｷｭｳ:4556` | endings 9, 10, 11, 13 |

## Not ported yet, reached at stage 4

| What | Lines | Reached |
|---|---|---|
| ~~`Tﾙﾄﾞﾗ`~~ | 205 (~110 live) | every week from S4 (`MAIN:816-846`) — main path; the only rule file stage 4 adds. **Ported** (milestone 5 step 1, `Strategy/CreatorTurn`) |
| `OVER` | 2210 | every ending; `GameEnd` until W4 (with `ｽﾀMENU`, and `HAPPY_C`, 1451, which only `ｽﾀMENU:236` calls) |
| `Tﾙﾄﾞﾗ2` | 1 | never (empty) |

Every scene stage 4 reaches runs from the player's files: `Gﾙﾄﾞﾗ`, `Gｺｳﾋﾒ`, `Gﾒﾅﾄﾞ`, `Gﾐｷ`, `Gｻﾃﾗ` 3 and
`Gﾎﾗｶﾞ` call no rule file; `Iｶﾐ` calls `FIGT`, `Sﾌﾞﾀｲ`, `FIGT表示` (→ `FIGTATTK`, routed) and `OVER`. The
battle, `FIGT_B`, `死亡捕獲`, `Sﾌﾞﾀｲ`, `Cﾒｲｷｭｳ` with God's Door, `COM`, `COMMAND`, `Cﾃｷﾄｼ` and `MAIN` are
ported (`StrategyRules.PortedFiles`), and so is `Tﾙﾄﾞﾗ` since milestone 5 step 1: the session no longer
stops at it in S4, and nothing stage 4 reaches is left unported but `OVER` (`GameEnd`).

## Quirk candidates

- **Nothing (75) ends the Creator's search** (`Tﾙﾄﾞﾗ:115-118`): no retry, that attack or earthquake
  is skipped (1 in 91 a try) — like Piar's NAT-15. **Registered: NAT-29** (with the give-up on the
  20th try).
- **The counter's boundaries** (`Gﾙﾄﾞﾗ:5`, `Tﾙﾄﾞﾗ:5, 119, 125`): phase 1 counts 2, so the spared
  provinces last three phases, and phase 4 (Turns 5) has neither the protection nor the castle roll.
  **Registered: NAT-30.**
- **NAT-11 is no quirk in effect**: status 0 changes nothing (above) — close it or reword it.
  **Reworded** (milestone 5 step 1): dead code, a write nothing reads.
- **Dead code** (NAT-16, EVT-01): the jitter `:83-104`, the unlabelled helpers `:132-143`, second ops
  `:148-186`, `:203-205`; `MAIN:843` (STR-13). Already registered.
- **Stale cursor after `Iｼﾚｲﾌﾞ` 7** (`:150-163`): `Prov_Cur = 92` reaches `死亡捕獲:380, 425` (Leila's
  scene test `Prov_Cur ≠ 1` holds even for a defence of Leazas Castle (1) on the own-army path) [I:
  effect only on path 2]. **Registered: EVT-20.**
- **Kayblis's last captures freeze** (path 2): the rest of his phase may take player provinces after
  the count; no phase or attack can touch them again [I]. **Registered: NAT-31.**
- **Ho Raga gives the Bonsai Statue again** after it is placed (`Cﾒｲｷｭｳ:4008-4009` clears the holder;
  `Gﾎﾗｶﾞ:276-291, 358-359`): at stage 4 with Kou past 9 the quest falls from 6 back to 5 and "Enter
  God's Door" (`Cﾒｲｷｭｳ:3921`) hides until the statue is placed again. **Confirmed by a run**
  (`GodsDoorEndTests`: the door opened, left, Ho Raga the same week, the bonsai placed again reopens it).
  **Registered: DGN-19.**
- **`Iｶﾐ`** [first look]: 4F's and 5F's "garrison destroyed" lines test the 3F rearguard
  (`:4740`, `:4803`; text only); `EndingId := 1` (`:2650`) is overwritten with 9 (`:2774`) before
  `&#OVER`; the rearguard picker (`:4813-4886`) loops until a party without Rance is picked — with no
  free unit it never ends [I: soft lock]. **Registered** with the survey's Q1-Q10 (survey-ikami §8):
  EVT-10, EVT-11, EVT-12 (confirmed by a run).
- `F_Hornet_Joined` is set only with Silky at 8 although the text says Hornet joins in any case
  (`Gｹｲﾌﾞﾘｽ:3100-3110`); it gates `Gｻﾃﾗ` 3, where Hornet and Silky both appear — a naming question for
  the dictionary rather than a quirk [I]. **Dictionary** (step 3): described so in `docs/variables.tsv`.

## Open questions

1. Is Kou at 8 really the only way to a winning end? Every writer of `F_Kou_State > 9` and of item 18
   was checked by grep; a capture of S4 with Kou at 8 and one with Kou below would settle it. **Run**
   (`GodsDoorEndTests`): both branches behave as read — Kou at 8 reaches `Iｶﾐ` in S4+2, Kou at 7 or 0
   leaves the statue with Ho Raga and the door shut; "the only way" still rests on the grep.
2. Are Siberia (38) and Ancient Ruins (35) then lost for good to the Angels from phase 4 — i.e. is a
   dungeon on a province of owner 10 really closed (survey-dungeon §2, `COMMAND:151`)?
3. Does milestone 5 include W4 (`OVER`, `ｽﾀMENU`) — decision 16 says "up to the game's end", and stage
   4 ends only there — or does every ending stay `GameEnd`? **Answered** (decision 17): `GameEnd`;
   W4 is not in milestone 5.
4. Path 2 (Kayblis killed by his own army, the Demon Lord's Castle (79) held): reachable at all?
5. `Iｶﾐ` needs its survey (rearguard rules, `FIGT表示` 21-24, the party restore `:4838-4854`) — with
   this milestone, or apart? **Answered** (decision 17): its own survey, before step 2's end tests.
6. `docs/variables.tsv`: `N_Rudrasaum_Turns` (above; corrected in step 1); `F_GodsDoor_Quest` reads "3 got gold statue, 4
   door failed to open", but 3 is Ho Raga's refusal and 4 Kou's hint (table above); `F_Hornet_Joined`.
   **Answered** (milestone 5 step 3): both rows rewritten.
7. Stage-4 captures need a save at the end of stage 3 with the whole map (owner).

## Proposed order — milestone 5 (story stage 4)

The milestone would start in S4 and end at `&#OVER` (`GameEnd`). The week, the commands, the battle,
God's Door and the scenes run already; what is missing is `Tﾙﾄﾞﾗ`, and `Iｶﾐ` has never run end to end.
Main path first:

0. **Sweep and harness**: this file; the owner's stage-4 checklist in
   [../golden-tests.md](../golden-tests.md) (`Tﾙﾄﾞﾗ` is a capture root already): S4 with Kou at 8 and
   below, phases 1-5 (searches, the spared provinces, the castle roll), an earthquake, a defence with
   Satella (unit 29), Ho Raga's Bonsai Statue, God's Door and `Iｶﾐ` to each ending.
1. **`Tﾙﾄﾞﾗ` whole** ✓ (2026-10-11; README, the N1 row) (~110 live lines): `Strategy/CreatorTurn` (no template — a small file like
   `PattonTurn`), routed in `StrategyRules`; `Gﾙﾄﾞﾗ` stays a scene; the dead code left out (NAT-16,
   EVT-01); a differential test against the file run from the decompile on random states (the search,
   the 75 abort, the counter, the castle roll, the earthquake's writes). Then an interactive stage 4
   plays every week.
2. **The end**: content tests from the player's files, as `StageThreeEndTests`: `Gﾙﾄﾞﾗ` with Kou at 8
   → `Gｺｳﾋﾒ` 6 → Ho Raga's Bonsai Statue → the four statues → God's Door → `Iｶﾐ` to endings 9, 10, 11
   and 13 (`GameEnd`); Kou below 8 → she dies and Ho Raga keeps the statue. This is `Iｶﾐ`'s first run
   end to end (its 20 kaiju battles, the rearguard picker, `FIGT表示` 21-24); its survey's open points
   go with it.
3. **Docs**: survey-nations §9 Q5 answered, NAT-11 settled, the new quirks registered,
   `docs/variables.tsv` (question 6), stage-2-sweep's God's Door corrected.
4. **W4** (`OVER`, `ｽﾀMENU`), if the owner puts it in this milestone (question 3).

Optional fronts need no new rule code: La Vaswald, Mars, the AL Church, and Helman, Zeth, JAPAN if
still at war run on ported files.
