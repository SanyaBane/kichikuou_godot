# Survey — the final arc (`Iｶﾐ.ADV`: God's Door → Rudrasaum)

Survey of the scene that ends the game, ahead of the content tests that drive it to each ending
(milestone 5 step 2; owner's decision 17 in [README.md](README.md)). Source: `scenario_named\`
(assignments read `!Var : n!`, `\` is ≠, `*` binds before `+`). Unmarked claims were read and
confirmed; **[I]** marks an inference, **[G]** a guess. Lines without a file name are `Iｶﾐ.ADV`.

**Run** (2026-10-11, step 2): `IkamiEndTests` (content) runs `Iｶﾐ` from the player's files after
the boot and a new game, with the four garrisons picked through `DeploymentScreen` and the fights in
the battle's port, to endings 9 (Charge; Create Chaos without Sill; a garrison lost), 10, 11 and 13
— the recipes of §9 hold as written, with `DebugMode 1` and Repel. `GodsDoorEndTests` runs the way
in from Kayblis's death (Ho Raga's Bonsai Statue, God's Door's four statues, Enter) to ending 11.

## 1. Role and shape

`Iｶﾐ.ADV` (6528 lines, a scene: `SceneRunner.IsSceneFile`, `I*`) is entered once, from God's Door:
`Cﾒｲｷｭｳ:4498-4556` (`L_06056`, "Enter") draws windows only and calls `%#Iｶﾐ.ADV` (`:4556`); the
port calls it at `DungeonCommand.GodsDoor.cs:430` and throws if it returns. It never returns: its
six exits are `&#OVER.ADV` (§5), which `ScriptContext.JumpTo` throws as
`ScenarioJumpException("OVER.ADV")` and `GameSession` (`GameSession.cs:108`) ends as `GameEnd`.

- **Down** (`:1-1248`): six floor headers "Underground Dungeon 1F" … "6F" (`:19-21`, `:72-74`,
  `:276-278`, `:497-499`, `:657-659`, `:956-958`). On 2F-5F Rance leaves a **garrison** picked with
  `Sﾌﾞﾀｲ` (§3), and on each floor the garrisons fight their kaiju (§4): 10 battles. Rudrasaum is
  on **6F** (`:1024-1062`); Sill (unit 2) at status 1 has a scene there (`:1063-1233`).
- **The menu** (`:1249-1275`): Charge → ending 9; Think (once / twice) unlocks Create Chaos → ending
  10 or 9, and Leave it to Warg (§5).
- **Up** (only after Leave it to Warg, `L_028ed`, `:2261-2617`): 10 more battles; each floor's
  garrison is checked bottom-up (5F, 4F, 3F, 2F); one flagged lost → ending 9; else the surface,
  then ending 11 (Sill alive) or 13.
- No `Y 4` in the file (grep: 0). No `StoryStage`, `F_Kou_State` or quest read.

| File | Use | Called at | In the port |
|---|---|---|---|
| `Iｶﾐ.ADV` | 6528 lines | `Cﾒｲｷｭｳ:4556` | scene, `SceneRunner` |
| `Sﾌﾞﾀｲ.ADV` | the garrison picker, mode 1, up to 4 units | `:4816` | `Strategy/DeploymentScreen` |
| `FIGT.ADV` | the kaiju battles, `Bt_Kind 3` | `:4607`, `:4670`, `:4733`, `:4796` | `Battle.FightAsync` |
| `FIGT表示.ADV` | entries 21-24 (`FIGT表示:43-54` → `:117-432`), a wiped garrison | `:4582`, `:4645`, `:4708`, `:4771` | scene |
| `WAO_SUB` / `M` | window stubs `:6439-6528`; map op 0 at `:2393`, `:2524`, `:2996`, `:3242`, `:3730`, `:4119` | — | `ScriptContext` |
| `OVER.ADV` | the end | `:1336`, `:1811`, `:2775`, `:3724`, `:4312`, `:4560` | `GameEnd` |

**Against stage-4-sweep's first look** (its "God's Door and `Iｶﾐ`"): Rudrasaum is on 6F, not
after 5F; the way back has **10** battles (`:2362` is a fourth batch), so **20** in all, not 19;
`L_0577f` runs to `:4902` (farewell lines `:4887-4899`); "re-fights each rearguard below" means the
garrisons *above* (left earlier); `FIGT表示` 21-24 call no `FIGTATTK` (§4.6); "Sill present" for
ending 11 is `Unit_Status ≠ 0` (a surviving garrison's status 2 counts) while ending 10 needs
status 1 (§5); "one lost → ending 9" holds only for a garrison wiped before its last fight (§4.5).
The rest is confirmed.

## 2. Labels

Every label checked at its end: no fall-through except the jumps named.

| Label / lines | Purpose | Ends |
|---|---|---|
| (top) `:1-11` | setup: `Prov_BgCg := 1024`, BGM 19, `Tmp0226-0229 := 0`, `Bt_TroopLimit 800`, `Bt_WallBonus 0`, `Bt_BackgroundCg 916`, `DgnRun_Floor 0` | — |
| `:12-66` | 1F (text) | — |
| `:67-272` | 2F: pick `:179`, save `:180-183`, "Fight hard" `:193-204`, fight 2F `:205`, text (CG 1023) | — |
| `:273-493` | 3F: pick `:397`, save `:398-401`, fights 3F, 2F `:426-427` | — |
| `:494-653` | 4F: pick `:549`, save `:550-553`, fights 4F, 2F, 3F `:586-588` | — |
| `:654-955` | 5F: pick `:740`, save `:741-744`, fights 5F, 2F, 3F, 4F `:776-779` | — |
| `:956-1248` | 6F: Rudrasaum (`G 806`, `G 516`, `:1024-1062`); Sill's scene if Sill (unit 2) is at status 1 (`:1063-1233`); `:1235-1247` | — |
| `:1249` | `Tmp0621 := 0` (the Think counter) | falls into `L_016cd` |
| `L_016cd` `:1250-1275` | release wait (`IM`, `:1251-1254`), the menu (§5.1) | `@L_016cd` (cancel) |
| `L_01748` `:1276-1336` | Charge: the tail wipes the army out | `EndingId 9`, `&#OVER` |
| `L_01857` `:1337-1599` | Think: 0 → 1 (`:1338-1396`), 1 and `N_Warg_Stage > 1` → 2 (`:1398-1588`), else "Can't think of anything" (`:1591-1597`) | `@L_01d08` |
| `L_01d08` `:1600-1601` | back to the menu | `@L_016cd` |
| `L_01d0d` `:1602-1811` | Create Chaos: Sill (unit 2) at status 1 → `L_0328c` (`:1767-1769`); else squashed | `EndingId 9`, `&#OVER` |
| `L_020ce` `:1812-2260` | Leave it to Warg: Warg's dream, the wake | `@L_028ed` (the next line) |
| `L_028ed` `:2261-2617` | the way back (§4.4), the surface (`:2392-2612`), Sill's test `:2613-2617` | `@L_04389` / `@L_04dd6` |
| `L_02fc2` `:2618-2648` | "Alright, still alive … retreat to the surface" | `\0` |
| `L_03043` `:2649-2775` | a garrison lost: the passage collapsed, no way out | `EndingId 1` → `9`, `&#OVER` |
| `L_0328c` `:2776-3724` | ending 10: Sill (unit 2) taken hostage; Rance's dark empire | `&#OVER` |
| `L_04389` `:3725-4312` | ending 11: Rance leaves with Sill | `&#OVER` |
| `L_04dd6` `:4313-4560` | ending 13: Rance stays; Lia's child | `&#OVER` |
| `L_05243` / `L_05392` / `L_054e1` / `L_05630` `:4561-4623` / `:4624-4686` / `:4687-4749` / `:4750-4812` | one kaiju fight of the 2F / 3F / 4F / 5F garrison (§4) | `\0` |
| `L_0577f` `:4813-4902` | the garrison picker (§3) | `\0` |
| `L_05958` `:4903-5680` | a garrison member's farewell (by `Unit_Cur`) | `\0` |
| `L_0688c` `:5681-6438` | a garrison member's return (by `Unit_Cur`) | `\0` |
| `:6439-6528` | stubs: `L_0777e` 111, `L_0779a` 112, `L_077b6` 113, `L_077d2` 311, `L_077e0` 312, `L_077ee` 121, `L_07842` 321, `L_07850` 322, `L_0785e` 131, `L_0787a` 331, `L_07888` 332, `L_07896` 0, `L_078a3` 1, `L_078bd` 3, `L_078ca` 4; the ops after each first `\0:` are dead (EVT-01) | `\0` |

## 3. The garrisons

**The pick** (`L_0577f`): `Tmp0021 := 4` (most units), `Tmp0022 := 1` (mode 1: bar "Attack" /
"－－－" / "Stop", `Sﾌﾞﾀｲ:12-42`), `%#Sﾌﾞﾀｲ` (`:4814-4816`). `Sﾌﾞﾀｲ` empties the party (`Sﾌﾞﾀｲ:1-4`),
takes only units at status 1 (status 2 buzzes, 0 is ignored, `:425-431`); "Attack" (501) is refused
without a party (`:163-174`) or with `Gold <` the war expenses Σ `Unit_DeployCost + Unit_Troops / 300`
(`:175-188`), else sets every member to status 2, pays, books `Hist_MilitaryExp`, `CallArg := 501`,
returns (`:189-210`); "－－－" (502) does nothing in mode 1 (`:114-118`); "Stop" (503) empties the
party and returns (`:150-158`). Then `Iｶﾐ`:

| Lines | Condition | Effect |
|---|---|---|
| `:4817-4836` | `Bt_PlayerParty1 = 0` (Stop) | a warning (window 121): "a giant kaiju is approaching, we can't continue without leaving someone"; Rance "I'll pick again"; `@L_0577f` |
| `:4838-4885` | Rance (unit 1) in any slot | every member back to status 1 (`:4839-4854`), Rance notices he picked himself; `@L_0577f`. The gold stays paid (as BFL-01) |
| `:4887-4902` | otherwise | `L_05958` per member, `\0` |

The caller saves the party into its floor's variables; each fight rewrites them with the survivors:

| Floor | Variables (VAR) | Saved at | Read by the fight | Rewritten | Read on the way back |
|---|---|---|---|---|---|
| 2F | `Ev_GodRear2F_Unit1-4` (0304-0307) | `:180-183` | `:4565-4568` | `:4610-4613` | `:2376-2389` |
| 3F | `Ev_GodRear3F_Unit1-4` (0308-0311) | `:398-401` | `:4628-4631` | `:4673-4676` | `:2347-2360` |
| 4F | `Ev_GodRear4F_Unit1-4` (0312-0315) | `:550-553` | `:4691-4694` | `:4736-4739` | `:2317-2330` |
| 5F | `Ev_GodRear5F_Unit1-4` (0316-0319) | `:741-744` | `:4754-4757` | `:4799-4802` | `:2286-2299` |

- Garrison members stay at status 2 for the rest of the file (nothing resets them; `MAIN:151`'s
  weekly reset never comes), so **each floor needs fresh units: four picks of at least one unit at
  status 1 other than Rance**, and the Gold for them.
- **No free unit — soft lock, confirmed.** Without a unit at status 1 besides Rance (or without
  the Gold for any of them [I: units with `DeployCost 0` and fewer than 300 troops cost 0]),
  "Attack" is refused inside `Sﾌﾞﾀｲ` and "Stop" returns an empty party, which `:4817-4836` answers
  by calling `Sﾌﾞﾀｲ` again: no exit. The port runs the same loop (the scene calls
  `DeploymentScreen.RunAsync` again), so a test that offers no unit never ends. One escape [I]:
  unit record 51 at status 1 in the 51st cell (CMD-24) — `FIGT表示` 21-24 set it to 1 (§4.6), so
  does a ninja substitution in a fight.
- **Farewell / return lines**: `L_05958` and `L_0688c` have a line for units 2-47, 49, 50 (none for
  Rance (unit 1), for record 48 — a blank "　" in `SET_CHR` — or 51). `L_0688c` returns at once for a
  unit at status 0 (`:5682-5684`), else prints "<name> ran over." (`:5685-5691`) and the line.
  Portraits vary for Mill (unit 25) with `F_Mill_State` (`:5255-5262`, `:6039-6046`) and Cafe
  (unit 45) with `F_Cafe_JoinMode` (`:5592-5624`, `:6342-6373`). `Iｶﾐ` renames no record: its
  `MM 1, Unit_StrBase …` (`:4591`, `:5685`) copies a name into string 1.

## 4. The kaiju battles

### 4.1 The four fights

Each label does, in order (2F's lines; the others alike): the floor's flag = 1 → `\0` (`:4562-4563`);
`Bt_PlayerParty1-4 :=` the garrison (`:4565-4568`); the floor header (CG 1024, "Underground Dungeon
2F", key wait, `:4569-4578`); **garrison empty** (`Bt_PlayerParty1 = 0`) → flag `:= 1`,
`Ev_Entry := 21`, `%#FIGT表示`, `EC 0`, CG 1023, `\0` (`:4579-4587`); else `Unit_Cur :=` the first
member, his face, "The giant kaiju assaulted <name>." (`:4588-4599`), `Bt_EnemyParty1-4 :=` (kaiju,
0, 0, 0), `Bt_EnemyRetreatMode := 6`, `SS 18`, `Bt_Kind := 3`, `%#FIGT`, `Bt_Kind := 0`,
`SS CurrentBgm` (`:4600-4609`), the garrison `:= Bt_PlayerSlot1-4` (survivors, closed up), and "The
garrison was destroyed before Rance returned." if a garrison's first slot is 0 (`:4614-4622`), `\0`.

| Label | Floor | Flag | Kaiju | Kaiju written | Retreat, kind | `FIGT表示` | "Destroyed" line tests |
|---|---|---|---|---|---|---|---|
| `L_05243` | 2F | `Tmp0226` | Kaiju Gaga (commander 187) | `:4600` | 6, 3 (`:4604-4606`) | 21 | 2F (`:4614`) |
| `L_05392` | 3F | `Tmp0227` | Kaiju Zeze (commander 188) | `:4663` | 6, 3 (`:4667-4669`) | 22 | 3F (`:4677`) |
| `L_054e1` | 4F | `Tmp0228` | Kaiju Dada (commander 189) | `:4726` | 6, 3 (`:4730-4732`) | 23 | **3F** (`:4740`) |
| `L_05630` | 5F | `Tmp0229` | Kaiju Bobo (commander 190) | `:4789` | 6, 3 (`:4793-4795`) | 24 | **3F** (`:4803`) |

Set once at `:1-11` and never again: `Bt_TroopLimit 800`, `Bt_WallBonus 0`, `Bt_BackgroundCg 916`.
**Not set** by `Iｶﾐ`: `Bt_IsDuel` (a stale 1 would stop the rounds from running out, 01-flow §7 [I]),
`Prov_Cur` (Ancient Ruins (35) on the God's Door path [I: `COMMAND:149` sets `Dgn_Prov`, `Cｼﾊｲﾄｼ`
runs on its city], read by FIGT for the Land of Death (82) attrition, Goliath and
`Bt_GarrisonHits`, `FIGT.ADV:217`, `:327`, `:3023`). `:1`'s `Prov_BgCg := 1024` writes that
province's record; FIGT's map redraw draws it (`FIGT.ADV:2008`).

### 4.2 The kaiju (`SET_CHR`, `MS 4` before `!Cmdr_Cur : n!`)

| Commander | Line | Troops / max | HP | Atk / Def | Attacks | Class, recovery |
|---|---|---|---|---|---|---|
| Kaiju Gaga (commander 187) | `SET_CHR:6923` | 900 / 900 | 3999 | 18 / 20 | 80 | 0, 0 |
| Kaiju Zeze (commander 188) | `:6950` | 800 / 800 | 4999 | 19 / 20 | 90 | 0, 0 |
| Kaiju Dada (commander 189) | `:6977` | 960 / 960 | 5999 | 20 / 25 | 100 | 0, 0 |
| Kaiju Bobo (commander 190) | `:7004` | 800 / 800 | 4999 | 30 / 28 | 110 | 0, 0 |

Nothing else writes them (grep `Cmdr_Cur : 18[7-9]|190`: `SET_CHR` and `FIGT表示`'s display only):
troops and HP carry over through all their fights, and a kaiju killed (HP 0, `Cmdr_Status := 0`,
`FIGT.ADV:1901-1905`) is put in slot 1 again at its next fight [I: `FIGT.ADV:1-33` copies the party
without a test]. FIGT disables the player's commander shield against them (`FIGT.ADV:2425-2427`).

### 4.3 What a fight does

`Bt_Kind 3` (01-flow §2.4-2.5, §3, §7): no strategy phase, Retreat refused ("impossible to run in a
dungeon", the turn not consumed), mode 6 → 3 rounds (`FIGT.ADV:17` still draws R3), timeout →
`Bt_Result 0`, EXP 1. A player unit whose HP reaches 0 dies for good (`Unit_Status := 0`,
`FIGT.ADV:1893-1894`) and its slot closes up (`L_04762`, `FIGT.ADV:2858-2905`). `Iｶﾐ` never reads
`Bt_Result` and calls no `死亡捕獲`. Rance is never in a garrison, so ending 1 cannot happen here.
At stage 4 FIGT saves the enemy's number in **`Tmp0229`** around `FIGT_M` (`FIGT.ADV:1802-1815`,
BTX-08) — the 5F flag's variable: it holds 187-190 after a fight, never 1, and the 5F flag is set
and tested with no fight in between (`:2272` → `:2283`), so no effect [I: every writer grepped].

### 4.4 Order of the fights (each a label call)

| Where | Lines | Fights (garrison) | Then |
|---|---|---|---|
| 2F | `:205` | 2F | — |
| 3F | `:426-427` | 3F, 2F | — |
| 4F | `:586-588` | 4F, 2F, 3F | — |
| 5F | `:776-779` | 5F, 2F, 3F, 4F | — |
| back at 5F | `:2269-2272` | 2F, 3F, 4F, 5F | `Tmp0229 = 1` → `L_03043` (`:2283-2285`); 5F returns `:2286-2299`; `L_02fc2` |
| back at 4F | `:2301-2303` | 2F, 3F, 4F | `Tmp0228` (`:2314`); 4F returns; `L_02fc2` |
| back at 3F | `:2332-2333` | 2F, 3F | `Tmp0227` (`:2344`); 3F returns; `L_02fc2` |
| back at 2F | `:2362` | 2F | `Tmp0226` (`:2373`); 2F returns; `L_02fc2` (`:2390`) |

Fights per garrison: 2F 8, 3F 6, 4F 4, 5F 2 — 10 down, 10 up. A flagged garrison's later calls
return at once.

### 4.5 A wiped garrison

The flag is set only at the **start of the garrison's next call** (`:4579-4587`), never after the
fight that wiped it. Wiped going down → the next call plays `FIGT表示` 2x, later calls are skipped,
and the way back finds the flag → `L_03043` → ending 9. **Wiped in its last fight** (5F `:2272`, 4F
`:2303`, 3F `:2333`, 2F `:2362`, the call just before its check) → never flagged: the check passes,
`L_0688c` runs with `Unit_Cur := 0` for slot 1 (`:2286`, `:2317`, `:2347`, `:2376`; slots 2-4 are
guarded), `L_02fc2` says "still alive", and the run goes on to ending 11 or 13 (quirk Q4).

### 4.6 `FIGT表示` 21-24

`FIGT表示:117-195` (21, 2F), `:196-274` (22, 3F), `:275-353` (23, 4F), `:354-432` (24, 5F), alike:
`MS 1, "　"`, `MS 2, "　"`, copied into unit record 51's name strings (a rename of record 51);
record 51: CGs 0, `Unit_Status := 1`, troops, max troops, HP, max HP 0 (`:122-133`);
`Cmdr_Cur :=` the kaiju (`:134`), `Unit_Cur := 51` (`:135`); the battle frame (`L_02808`,
`:2193-2222`, which draws the numbers through `L_03714` / `L_0382a` writing `Ui_MouseX/Y`,
`Tmp0021-0025`); text "Kaiju Gaga raged … collapsing the passage. Nobody was left to defend the
dungeon's 2nd floor." with key waits through WAO op 11 (`L_03a73`, `RetVal :=` the key); `%0`.
**No `FIGTATTK`, no draw** (the file's 11 `FIGTATTK` calls are at `:2337-2935`, other entries).

## 5. Before Rudrasaum: the menu and the endings

### 5.1 The menu (`L_016cd`)

`IM Ui_MouseX, Ui_MouseY` loops while a button is held (`:1251-1254`), then `]` with:

| # | Item (line) | Shown when | Goes to |
|---|---|---|---|
| 1 | "Charge" (`:1256-1258`) | always | `L_01748` → ending 9 |
| 2 | "Think" (`:1259-1261`) | always | `L_01857`: `Tmp0621` 0 → 1 (`:1338-1339`); 1 and `N_Warg_Stage > 1` → 2 (`:1398-1399`); else text; back to the menu |
| 3 | "Create Chaos" (`:1262-1267`) | `Tmp0621 > 0` | `L_01d0d` → ending 10 or 9 |
| 4 | "Leave it to Warg" (`:1268-1273`) | `Tmp0621 > 1` | `L_020ce` → the way back |

Cancel (right click) continues after `]` → `@L_016cd`: the menu again (`:1275`). `N_Warg_Stage`
(VAR0061) becomes 2 only at `Gﾜｰｸﾞ:3525` (`L_03f6d`, reached from entry 10, `L_01089` — Warg's harem
night [I: `Cﾊｰﾚﾑ:35` sets entry 10]; the debug `TEST.ADV:482` too); 1 at `Gﾜｰｸﾞ:3605` (entry 2,
she joins).

### 5.2 Every `EndingId` write and exit

| Line | Writes | Path | Exit |
|---|---|---|---|
| `:1335` | `EndingId 9` | Charge | `&#OVER` `:1336` |
| `:1810` | `EndingId 9` | Create Chaos, Sill (unit 2) not at status 1 (`:1767-1768`) | `&#OVER` `:1811` |
| `:2650`, `:2774` | `EndingId 1`, then `9` | a garrison flagged lost (`:2284`, `:2315`, `:2345`, `:2374`) | `&#OVER` `:2775` |
| `:2777`, `:3723` | `EndingId 10` twice | Create Chaos with Sill (unit 2) at status 1 (`:1769`) | `&#OVER` `:3724` |
| `:3726-3728` | `F_Sill_Fate 1`, `F_Ending_LiaRoute 2`, `EndingId 11` | the way back, every flag 0, Sill (unit 2) `Unit_Status ≠ 0` (`:2613-2615`) | `&#OVER` `:4312` |
| `:4314-4315` | `F_Ending_LiaRoute 1`, `EndingId 13` | the same, Sill at status 0 (`:2617`) | `&#OVER` `:4560` |

Nothing between `:2650` and `:2774` or `:2777` and `:3723` reads `EndingId` (text, windows, `M` op 0
only). For context only (`OVER` is `GameEnd`): `OVER:1-5` counts a clear for 9, 10, 11, 13;
`:15-17` turns ending 9's fortunes off; 11 and 13 share the jingle and the staff roll (`:35`, `:47`);
`F_Sill_Fate` and `F_Ending_LiaRoute` pick the roster's lines for Sill, Lia and Maris
(`OVER:203-273`) — corrected by [survey-w4.md](survey-w4.md) §2.3.

**Sill's two tests differ**: Create Chaos and her 6F scene need status **1** (`:1064`, `:1768`) —
with Rance, not left in a garrison (status 2) or spent this week; the way back needs **≠ 0**
(`:2614`) — a surviving garrison member (2) counts as present. Sill dead in a garrison → 13.

## 6. State read and written

| Variable | Lines | Meaning | A test |
|---|---|---|---|
| `Unit_Status` of units 1-50 | `Sﾌﾞﾀｲ:425-431`; `:1064`, `:1768`, `:2614`, `:5682` | 1 = can be picked / with Rance; 2 = picked; 0 = gone | **preset**: four units at 1 besides Rance (unit 1); Sill (unit 2) per ending |
| `Gold` | `Sﾌﾞﾀｲ:175-206` | pays the garrisons | **preset** ≥ Σ costs of the four picks |
| unit records (troops, HP, class …) | FIGT | the fights' outcome | preset to force a hold or a wipe (§9) |
| `N_Warg_Stage` | `:1398` | "Leave it to Warg" | **preset 2** for endings 11, 13, the lost-garrison 9 |
| `Bt_IsDuel` | FIGT | 1 = rounds never run out | **preset 0** |
| `StoryStage` | FIGT (`:1802-1815`, BTX-08) | stage-4 lines, the `Tmp0229` save | preset 4 (as the real path) |
| `Prov_Cur`, `Goliath_State/Prov` | FIGT `:217`, `:327`, `:3023` | attrition, Goliath, garrison hits | preset `Prov_Cur 35`, Goliath off |
| `DebugMode` | `FIGT_SEL` (port `Battle.Flow.Player.cs:250-259`) | offers Repel (7), Cheater Beam | 1 to end a fight at once (§9) |
| `F_Mill_State`, `F_Cafe_JoinMode` | `:5255`, `:5592` etc. | portraits only | — |
| `Hist_Cur` | `Sﾌﾞﾀｲ:207` | history record charged | — |
| `Tmp0226-0229` (VAR0226-0229) | `:4-7`; `:4580`, `:4643`, `:4706`, `:4769` | garrison 2F-5F flagged lost | written (no preset) |
| `Ev_GodRear2F-5F_Unit1-4` (VAR0304-0319) | §3 | the garrisons | written; assert after fights |
| `Tmp0621` (VAR0621) | `:1249`, `:1339`, `:1399` | the Think counter | written |
| `Bt_PlayerParty1-4`, `Bt_EnemyParty1-4`, `Bt_EnemyRetreatMode`, `Bt_Kind` | §4.1 | battle setup | written |
| `Bt_PlayerSlot1-4` | `:4610` etc. | FIGT's survivors | read |
| `Ev_Entry` | `:4581`, `:4644`, `:4707`, `:4770` | 21-24 | written |
| `EndingId` (VAR0805), `F_Sill_Fate` (VAR0716), `F_Ending_LiaRoute` (VAR0717) | §5.2 | the ending | **assert** |
| `Prov_BgCg`, `CurrentBgm`, `DgnRun_Floor`, `Bt_TroopLimit`, `Bt_WallBonus`, `Bt_BackgroundCg` | `:1-11` | setup; `DgnRun_Floor` read by nobody after (only `Cﾒｲｷｭｳ` reads it) | — |
| unit record 51, strings 1-2, `Cmdr_Cur`, `Unit_Cur` | `FIGT表示` 21-24 | §4.6 | — |
| `Unit_Status := 1` | `:4841-4853` | Rance-refusal restore | — |

## 7. Inputs and engine support

**Inputs, in order** (the context's input is an `IGameInput`; `StageThreeEndTests.KeysOnly` throws on
`GetMouseAsync`, `SelectAsync` and the battle — this file needs all three):

1. Key waits (`WaitForKeyAsync`): every WAO op 1 / 11 with a window open (`ScriptContext.Wao.cs:50-71`)
   — the text of every floor, the picker's refusals, the farewells, `FIGT表示`.
2. Per pick (2F `:179`, 3F `:397`, 4F `:549`, 5F `:740`): `DeploymentScreen` polls `GetMouseAsync`
   (`CommandFile.cs:104`): clicks on `PointerScript.PickerCell(u)` for 1-4 units (cells 108×18 from
   (328, 18), `Sﾌﾞﾀｲ:500-505`), then `PickerButton(1)` ("Attack", 501); `PickerButton(3)` is Stop.
3. Per fight (§4.4 order): the battle's `WaitForKeyAsync` (round header `IK 0`), `PauseAsync`
   (`Y 3`), and `SelectCommandAsync(FigtSelMenu)` at every player unit's turn (fewest troops act
   first, 01-flow §6). `ChooseAsync` is never asked: Kind 3 refuses Retreat before its menu
   (`Battle.Flow.cs:612-631`); the defence menus belong to `FIGT_B`.
4. At 6F: `GetMouseAsync` must report no button (`:1251-1254`), then `SelectAsync(items)` — 1-based,
   items as listed in §5.1 (0 = cancel → the menu again). After each Think the menu repeats.
5. Way back (Warg only): the 10 fights, the returns' key waits.

**Draws**: none in `Iｶﾐ` and none in `FIGT表示` 21-24; every fight draws (R3 `RND(2)` and the
exchanges, 01-flow §12). `QueuedRandom { DefaultToOne = true }` keeps a run deterministic.

**Engine commands** — `Iｶﾐ`: text (`'…'`, `R`, `X 1`, `ZH 1`), `ZM`, `EC`, `G`, `PF0`, `PF1`
(`EngineStatement`), `SS` (`MusicStatement`), `MM` (`CopyStringStatement`), `IM` (`MouseStatement`),
`$label$` / `$` / `]`, `!…!`, `{…}`, `\label`, `\0`, `@label`, `%#`, `&#`; `FIGT表示` 21-24 add `MS`,
`J0`, `J1`, `CC`, `CS`, `%0`. All are handled by `SceneRunner.RunFileAsync` (`SceneRunner.cs:79-258`);
both files parse in `ScenarioProgramTests.Every_scene_file_reads_as_a_program_and_its_texts_resolve`;
every WAO op used (0, 1, 3, 4, 10, 11, 111-113, 121, 131, 151, 153, 311, 312, 321, 322, 331, 332) and
`M` op 0 are ported (`ScriptContext.Wao.cs:41-158`, `ScriptContext.Map.cs:161`). **None unsupported.**

## 8. Quirk candidates

| # | What | Lines | Kind |
|---|---|---|---|
| Q1 | 4F's and 5F's "garrison destroyed" lines test the **3F** garrison: their own wipe goes unannounced, 3F's is announced three times (stage-4-sweep, confirmed) | `:4740`, `:4803` | bug, text only |
| Q2 | `EndingId := 1` overwritten with 9 before anything reads it (confirmed); likewise 10 written twice | `:2650`/`:2774`; `:2777`/`:3723` | dead code |
| Q3 | Picker with no unit at status 1 besides Rance, or no Gold for one, never ends: Stop → `:4817-4836` → `Sﾌﾞﾀｲ` again (confirmed, soft lock) | `:4813-4836`; `Sﾌﾞﾀｲ:150-188` | bug |
| Q4 | A garrison wiped in its last fight is never flagged, counts as held: endings 11/13 with a dead garrison (§4.5) | `:4579-4580` vs `:2272`, `:2303`, `:2333`, `:2362` | bug |
| Q5 | Picking Rance: the war expenses paid by `Sﾌﾞﾀｲ` are not refunded (the BFL-01 pattern, a new site) | `:4838-4885`; `Sﾌﾞﾀｲ:206` | bug |
| Q6 | Sill's tests differ: status 1 for Create Chaos and her scene, ≠ 0 for ending 11 — Sill left in a garrison fails Create Chaos but counts on the way back (§5.2) | `:1064`, `:1768`, `:2614` | odd design [I: may be meant] |
| Q7 | Kaiju never refilled; a killed kaiju returns with HP 0 at its next fight [I] (§4.2) | `:4600`, `:4663`, `:4726`, `:4789` | odd design |
| Q8 | Stale cursors: `Prov_BgCg := 1024` writes `Prov_Cur`'s province record; FIGT reads the stale `Prov_Cur`; `L_0688c` reads unit record 0 for an unflagged wiped garrison [I: ENG-06, the previous field's last slot, normally 0 → silent] | `:1`, `:2286`, `:2317`, `:2347`, `:2376` | engine artefact (ENG-05/06), no visible effect [I] |
| Q9 | `FIGT表示` 21-24 leave unit record 51 at status 1 with 0 troops and HP: the next pickers' 51st cell deploys it (CMD-24), whose death FIGT ignores (BFL-20) — a garrison that cannot be wiped [I: not traced through FIGT] | `FIGT表示:129`, `:208`, `:287`, `:366` | bug (extends CMD-24) |
| Q10 | Dead: `DgnRun_Floor := 0` (no reader on this path); `@L_028ed` jumps to the next line; the stubs' second ops (EVT-01) | `:11`, `:2260`, `:6443-6520` | dead code |

Not a quirk: FIGT's stage-4 save in `Tmp0229` (§4.3).

**Registered** (2026-10-11, milestone 5 step 3): Q1-Q10 are EVT-10 to EVT-19 in
[../quirks.md](../quirks.md), in order (Q1 EVT-10 … Q10 EVT-19).

## 9. What a content test needs, per ending

**Common rig** (as `StageThreeEndTests.KayblisDeadAsync`): boot and new game from the player's files,
`StrategyRules` + `SceneRunner`, `QueuedRandom { DefaultToOne = true }`, a `Recorder` of the calls;
then `StoryStage 4`, `Bt_IsDuel 0`, `Prov_Cur 35`, `Gold` 100; garrison units at status 1 — at boot
only Rance (unit 1), Sill (unit 2), Bound (unit 3), Soul (unit 4) are at 1 (`SET_CHR` [I: the new
game keeps them]), so set two more, e.g. Leila (unit 5) and Rick (unit 7) (cost
`DeployCost + Troops / 300` = 11 and 9). Run `Script.CallFileAsync("Iｶﾐ.ADV")`, expect
`ScenarioJumpException` with `File = "OVER.ADV"`, assert `EndingId` (and the two fates). Input: keys
0, `PauseAsync` 0, a `PointerScript` for the four picks (one unit each: Bound (unit 3) at 2F, Soul
(unit 4) at 3F, Leila (unit 5) at 4F, Rick (unit 7) at 5F), each followed by `PickerButton(1)`;
`SelectAsync` from a queue; `SelectCommandAsync` per the recipe.

**Forcing the fights** [I]: with `DebugMode 1`, answering Repel (7) at a unit's first turn ends the
fight at once (`Bt_Result 2`, no losses) — the garrison acts first while it has fewer troops than its
kaiju (800-960). To wipe a garrison in a chosen fight, make its unit `Troops 0`, `HP 1` and answer
Defend in that fight only (Repel in the others) [G: any HP damage kills it]. The test can tell the
fight by `Bt_EnemyParty1` (the floor's kaiju, §4.1) and the call count; assert `Ev_GodRearNF_Unit1` afterwards.

| Ending | Extra state | Menu answers | Fights | Assert |
|---|---|---|---|---|
| 9 (Charge) | — | 1 | 10 | `EndingId 9` |
| 9 (no Chaos) | Sill (unit 2) at 0 or 2, or picked into a garrison | 2, 3 | 10 | `EndingId 9`; no `L_0328c` |
| 10 | Sill (unit 2) at 1, not picked | 2, 3 | 10 | `EndingId 10` |
| 11 | `N_Warg_Stage 2`; Sill (unit 2) ≠ 0; every garrison holds (Repel) | 2, 2, 4 | 20 | `EndingId 11`, `F_Sill_Fate 1`, `F_Ending_LiaRoute 2` |
| 13 | as 11 with Sill (unit 2) at 0 | 2, 2, 4 | 20 | `EndingId 13`, `F_Ending_LiaRoute 1` |
| 9 (garrison lost) | as 11; Rick (unit 7), the 5F garrison, wiped at `:776` | 2, 2, 4 | 13 (10 down, `:2269-2271`; `:2272` plays `FIGT表示` 24; `:2283` jumps) | `FIGT表示` 24 called, `EndingId 9` |
| Q4 | as 11; Bound (unit 3), the 2F garrison, wiped at `:2362` | 2, 2, 4 | 20 | `EndingId 11`, `Ev_GodRear2F_Unit1 0` |
| Q3 | only Rance (unit 1) at status 1 | — | — | the picker loops: bound the input (e.g. throw after n Stops) |

With `N_Warg_Stage` below 2 the second Think plays "Can't think of anything" and item 4 never shows
— a cheap negative test (menu answers 2, 2, then 3 sees three items).

## 10. Open questions

1. Q4 and Q9 rest on the battle port's behaviour with a wiped garrison and with record 51 in a party
   slot (CMD-24, BFL-20): the end tests should confirm both, and a capture of `Iｶﾐ` would settle them.
   **Q4 confirmed by a run** (`IkamiEndTests`, 2026-10-11): Bound (unit 3), the 2F garrison, killed in
   its last fight (`:2362`, 0 troops, 1 HP, Defend) — `Tmp0226` stays 0 and the run ends with ending 11.
   Q9 not run.
2. Is `Prov_Cur` always Ancient Ruins (35) at entry? `Cﾒｲｷｭｳ` writes it nowhere; the God's Door
   scenes between the click and `:4556` were not traced for map calls that move it [I]. **Answered by
   a run** (`GodsDoorEndTests`, 2026-10-11): the ruins are entered from the city's Ruins Search
   (`Cｼﾊｲﾄｼ:227-232`, `:538-547`), which runs on Ancient Ruins (35), and nothing up to `:4556` moves
   the cursor — `Prov_Cur` is 35 when `Iｶﾐ` starts.
3. `Bt_IsDuel` at entry: every duel caller resets it (01-flow §2.6) [I]; a stale 1 would make the
   fights endless until a side is annihilated.
4. Does Repel at the first turn really skip the kaiju's turn (turn order by troops, 01-flow §6)?
   The recipes in §9 assume it; the first test run answers it. **Answered** (`IkamiEndTests`): Repel
   wins every one of the 20 fights with the four garrisons of the new game's units, none lost.
5. Unit record 0's `Unit_Status` (Q8): which slot it aliases and whether it can be non-zero.
6. `N_Warg_Stage 2`'s path (`Gﾜｰｸﾞ` entry 10 from `Cﾊｰﾚﾑ:35`) was not traced: needed only to tell
   the owner how a player reaches "Leave it to Warg".
7. Register Q1-Q10 (areas EVT/DGN) after the end tests, citing this survey as `ikami` (step 3). Q3
   (the picker never ends with only Rance free: three Stops, four pickers), Q4 and Q5 (Rance picked:
   his war expenses stay paid) are confirmed by runs in `IkamiEndTests`. **Done** (step 3): EVT-10 to
   EVT-19.
