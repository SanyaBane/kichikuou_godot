# Survey — the game frame (W4: `OVER`, `ｽﾀMENU`, `HAPPY_C`)

Survey behind milestone 6 (owner's decision 18 in [README.md](README.md)): the ending file every
ending jumps to, the original's start menu and the "Fortune Check" it opens. Source:
`scenario_named\` (assignments read `!Var : n!`, `\` is ≠, `*` binds before `+`). Read on 2026-10-11 by
three subagents (one per file, one for the port's side); the frame of each file and the load-bearing
lines were re-read and confirmed (`OVER.ADV:1-50`, `L_02a10` `:2084-2133`, `:2072-2083`, roster entries
0 and 41; `ｽﾀMENU.ADV:50-86`, `:244-268`, `:292-303`). The per-entry conditions of the fate roster
(§2.5) and the debug presets' long lists (§3.4) are as the subagents read them, not each re-read.
**[I]** marks an inference. survey-main's W4 row and its rows for these files are the first look;
this survey supersedes them where they differ (§5).

## 1. Where the port stands

| File | Lines | Reached from | In the port today |
|---|---|---|---|
| `OVER.ADV` | 2210 | `&#OVER` at 19 sites, every ending | `ScriptContext.JumpTo` throws `ScenarioJumpException("OVER.ADV")`; `GameSession.PlayAsync` returns `GameEnd(EndingId)` (`GameSession.cs:108-110`); Godot shows "THE END — ending n" and goes back to our title (`godot/Strategy/GameScreen.cs:252-302`, decision 10). Nothing of the file runs. |
| `ｽﾀMENU.ADV` | 871 | `ｽﾀｰﾄ.ADV:872` | Only the way to a new game: `GameSetup.NewGameAsync` (`Setup/GameSetup.cs:80-103`: the title's state lines `:8, 55, 84, 103-139`, `:244-247`, `L_00670` `:279-290`), then `MAIN` (`GameSession.cs:65`); Load (`:238-243`) is `GameSession.LoadFromTitleAsync`. The debug starts throw (`GameSetup.cs:94-95`). Our own title is `godot/Main.cs` (New game, Load, Quit, the capture viewer). |
| `HAPPY_C.ADV` | 1451 | `ｽﾀMENU.ADV:236` | Not routed, not run. |
| `CG`, `MMODE`, `ﾌﾟﾛﾛｰｸﾞ`, `XSPEC`, `Iｱﾘｽ`, `Gｻｲﾄｳ` | 3977, 560, 187, 48, … | `ｽﾀMENU` | Not routed (`StrategyRules` has no case; `SceneRunner.IsSceneFile` takes `G*`/`I*`, so `Iｱﾘｽ` and `Gｻｲﾄｳ` would run as scenes). |

**Page 4** (the persistent page: `Glob_Fate`, `Glob_MenuItem`, `Glob_CgSeen`, 2100 words, `DS` at
`SET.ADV:290`) is already handled: `SaveData.SavedPages` keeps it in every slot (`Script/SaveData.cs:25`);
`ﾛｰﾄﾞｾｰﾌﾞ` 10 (read at boot, `GameSetup.Set.cs:241-242` for `SET.ADV:340`) and 11 (written by every save
and the autosave) are ported one to one (`Strategy/LoadSaveScreen.cs:342-387`); Godot's
`DirectorySaveStore` keeps the slots across launches. As in the original, page 4 lives only inside
existing slots.

## 2. `OVER.ADV` — the ending

No `&#` jump, no `%0`, no menu, no mouse, no `Y 4`. It shows the ending's CG and jingle, a staff roll
for four endings, then the **Completion Report** — 66 characters' fortunes and misfortunes, each
recorded on page 4 — then `ﾛｰﾄﾞｾｰﾌﾞ` 11, "THE END", and `ZZ0, 0`: the program quits (STR-10).

### 2.1 Labels

| Label | Lines | Purpose | Ends |
|---|---|---|---|
| (entry) | 1-43 | the clear (`:1-5`), `Tmp0002` (`:6-23`), `M` 0, `SS 0`, `Y 3, 120`, fades, `WAO_SUB` 113, black `CF`, the ending CG `G 269 + EndingId` (`:33`, drawn at (0, 400) and copied up), the jingle `PCMPLAY` 1 or 2 (`:35-43`) | falls into `L_0014d` |
| `L_0014d` | 44-50 | `Y 3, 360`, `IK 0`; endings 7, 8, 11, 13 → the staff roll | `@L_0017e` / `@L_004a8` |
| `L_0017e` | 51-158 | staff roll: CG 850-857 off screen, `SL 1`, `SS 23`, six scroll segments | `@L_004a8` on a skip, else falls into `L_003f0` |
| `L_003f0` | 159-165 | polls `SR1` into `Tmp0011` until 0, `Y 3, 300` | `@L_004a8` |
| `L_00413` | 166-182 | one scroll segment, 2 px a frame, `Y 3, 3` a frame | `\0` |
| `L_004a8` | 183-2083 | the report: CG 840, markers from CG 841, the 66 entries (`:203-2072`), `ﾛｰﾄﾞｾｰﾌﾞ` 11 (`:2073-2075`), "THE END", "Closing …" | `PF1 4`, `ZZ0, 0` |
| `L_02a10` | 2084-2133 | records the entry's fate in `Glob_Fate` (§2.4) | `@L_02b41` |
| `L_02b41` … `L_02c6c` | 2134-2192 | the marker (fortune `CS 120,400` + PCM 89, misfortune `CS 140,400` + PCM 90), key wait, `PCMPLAY` 3, portrait erased | `\0` |
| `L_02c95`, `L_02cac`, `L_02cc7` | 2193-2210 | portrait `G CallArg, 40` (then `CallArg := 0`); `WAO_SUB` 113; key wait and text area restored | `\0` |

### 2.2 The clear and the suppressed fortunes

- `:1-5`: `EndingId > 4` and not 6, 7, 12, 14 — that is **5, 8, 9, 10, 11, 13** — adds 1 to the clear
  count `Glob_Fate[79]` (`Glob_FateCur := 79`) and copies it to `Tmp0020`.
- `:6-23`: `Tmp0002 := 1` — no fortune shown or recorded (§2.5) — for endings 6, 7, 9, 14 and whenever
  `F_Kayblis_War = 2`.

### 2.3 Per ending

| Ending | Set at | Clear | Fortunes off | CG | Jingle | Staff roll | Its own roster lines |
|---|---|---|---|---|---|---|---|
| 1 | `死亡捕獲:30`, `FIGTﾐｶDE:38`, `Gﾊﾟﾊﾟｲｱ:4925` | no | no | 270 | 2 | no | — |
| 2 | `FIGT_B:824, 880` | no | no | 271 | 2 | no | — |
| 3 | `Gｺｰﾄﾞ:2117` | no | no | 272 | 2 | no | Kanami's fortune |
| 4 | `TJAPAN:2373` | no | no | 273 | 2 | no | — |
| 5 | `Gﾏﾘｱ:4863` | yes | no | 274 | 1 | no | Maria's fortune |
| 6 | `Iﾏｼﾞﾝ2:603` | no | yes | 275 | 2 | no | Miki's misfortune |
| 7 | `Gｻﾃﾗ:5342` | no | yes | 276 | 2 | yes | (Satella's fortune is unguarded) |
| 8 | `Iｼﾚｲﾌﾞ:1466` | yes | no | 277 | 1 | yes | Kouhime's fortune (unguarded) |
| 9 | `Iｶﾐ:1335, 1810, 2774` | yes | yes | 278 | 2 | no | — |
| 10 | `Iｶﾐ:2777, 3723` | yes | no | 279 | 2 | no | — |
| 11 | `Iｶﾐ:3728` | yes | no | 280 | 1 | yes | — |
| 12 | `Gｶﾁｭｰｼｬ:2293` | no | no | 281 | 2 | no | — |
| 13 | `Iｶﾐ:4315` | yes | no | 282 | 1 | yes | — |
| 14 | `Iﾄｳｿﾞｸ:2177` | no | yes | 283 | 2 | no | Suzume's and Wendy's misfortunes off |

The staff roll can be skipped only with `Tmp0020 > 1` (`:74`, `:82`, `:95`, `:175`): from the second
clear on. survey-main's "`:15-17` marks 9" and the sweeps' "11 and 13 share the long epilogue
(`:35`, `:47`)" were wrong: those lines set `Tmp0002` and pick the jingle and the staff roll; there is no
per-ending epilogue text in the file — the endings' own texts are in the files that set `EndingId`.

### 2.4 The fate record (`L_02a10`)

Each entry sets `Glob_FateCur := i` (0-65) and `Tmp0001` (0 nothing, 1 fortune, 2 misfortune), then
merges it into `Glob_Fate[i]` across plays: 0 → 1 (fortune) or 10 (misfortune); 1 + misfortune → 100;
10 + fortune → 100; 100 stays. Another value prints "If you see this, something's wrong" (`:2128-2132`,
unreachable [I]). Then `:2073-2075`: `RetVal := 11`, `ﾛｰﾄﾞｾｰﾌﾞ`, which writes page 4 into every slot it
can read (`ﾛｰﾄﾞｾｰﾌﾞ:529-544`) — with no save, nothing of the run is kept.

### 2.5 The roster (`:203-2072`)

P(x) is `Unit_Status ≠ 0` for unit x; every fortune also needs `Tmp0002 = 0` unless marked
"unguarded"; when both conditions hold, both texts show and the misfortune is recorded (§2.7, Q4).

| # | Name | Fortune | Misfortune |
|---|---|---|---|
| 0 | Sill | `F_Sill_Fate 1`, P(Sill (unit 2)) | `F_Sill_Fate 2` |
| 1, 2 | Lia, Maris | `F_Ending_LiaRoute 1` | `F_Ending_LiaRoute 2` |
| 3 | Suzume | `F_Suzume_State 5` | `< 4`, ending ≠ 14 |
| 4 | Wendy | `F_Wendy_BedroomSeen 1` | `F_Wendy_State < 2`, ending ≠ 14 |
| 5 | Kanami | ending 3 | `F_Kanami_Dismissed 2` |
| 6 | Leila | `F_LeilaRick_State > 5`, P(Leila (unit 5)), P(Rick (unit 7)) | `F_Leila_NobunagaWife 1` |
| 7 | Menad | `F_Menad_State 8`, P(Menad (unit 10)) | `F_Menad_Fate 2` |
| 8-10 | Haurein, Melfeis, Soul | their `*_Fate 1` and P(unit 12 / 13 / 4) | `*_Fate 2` |
| 11 | Anasel | `F_Anasel_Fate < 2` | `> 1` |
| 12 | Maria | ending 5 | `F_Maria_Tulip 100` |
| 13 | Shizuka | `N_Shizuka_Stage 7` | `F_RagarlRitual_State 7` |
| 14 | Eleanor Ran | P(Eleanor (unit 23)), `F_Ran_State 0` | `F_EleanorRan_Fate 2` |
| 15 | Milli | P(Milli (unit 24)) | `F_Milli_Fate 2` |
| 16 | Mill | `F_Mill_State > 2`, P(Mill (unit 25)) | `F_Mill_State 1`, P(Mill (unit 25)) |
| 17 | Mokomoko | `F_Mokomoko_State > 14` | `2` |
| 18 | Sayori | `F_Sayori_Fate 1` | `2` |
| 19 | Merim | `N_Merim_Inscriptions > 5`, `F_Merim_Dungeon 1` | `F_Merim_Dungeon 3` |
| 20 | Raylee | `F_Raylee_State 10` | `5` |
| 21 | Shariela | `F_Shariela_State` not 0, 4 | `4` |
| 22-24 | Amiran, Katyusha, Saulnia | their `*_Fate 1` | `*_Fate 2` |
| 25 | Satella | `F_Satella_State 10` (unguarded) | `12` |
| 26 | Julia | `F_Julia_State 3`, `F_HannyKing_State > 5`, P(Julia (unit 33)) | `F_Julia_State 10` |
| 27 | Rafalia | `F_Rafalia_State ≠ 0`, P(Rafalia (unit 34)) | `F_Arlcoate_State ≠ 0`, `F_Rafalia_State 0` |
| 28 | Arlcoate | `F_Arlcoate_State > 6`, ≠ 10, P(Arlcoate (unit 35)) | `10` |
| 29 | Cream | `N_Cream_Tactics > 10`, P(Cream (unit 28)) | `F_Cream_Fate 2` |
| 30 | Ruberan | `F_Ruberan_State 100` | not 0, not 100 |
| 31 | Sheila | `F_Sheila_State 11` | `5` |
| 32 | Chizuko | `F_Chizuko_State > 3`, P(Chizuko (unit 38)) | `F_Chizuko_Fate 2` |
| 33 | Magic | `F_Magic_State 6`, P(Magic (unit 39)) | `100` |
| 34 | Nagi | `F_Nagi_State > 9`, P(Nagi (unit 40)) | `4` |
| 35 | Makoto | `F_Makoto_State 0` | `≠ 0` |
| 36, 37 | Kaoru, Wichita | `F_Gandhi_State 100` | `F_Kaoru_Fate 2`; `F_Gandhi_State 99` and `N_Wichita_State 0` |
| 38 | Elisabeth | `F_Elisabeth_State` not 0, 10 | `10` |
| 39 | Isoroku | `N_Isoroku_Stage 6`, P(Isoroku (unit 27)) | `F_Isoroku_Fate 2` |
| 40 | Merci | `F_Merci_State < 2` | `> 1` |
| 41 | Kouhime | ending 8, `F_Kou_State 3`, `N_Isoroku_Stage 6`, P(Isoroku (unit 27)) (unguarded) | `F_Kouhime_Dead 2` |
| 42 | Rebecca | `F_Rebecca_State > 1`, ≠ 100 | `100` |
| 43 | Annis | `F_Annis_State 6` | `F_RagarlRitual_State 6` |
| 44 | Elena | `F_Elena_State > 6` | `< 7` |
| 45 | Aya | `N_Aaya_Stage` not 0, 8 | `8` |
| 46 | Aoi | `F_Aoi_State 100` | `2` |
| 47 | Aviator | `F_Aviator_State 100`, `F_Arlcoate_State ≠ 0`, `F_Rafalia_State ≠ 0` | `11` |
| 48 | Uspira | `F_Uspira_Fate 1` | `2` |
| 49 | Kapala | `F_Kapala_State 0`, `N_Kapala_Rank 12` | `F_Kapala_State 3` |
| 50 | Cafe | `F_Cafe_State > 19`, P(unit 45) | `10` or `11` |
| 51 | Calory | `F_Calory_State 6` | `10` |
| 52 | Sanakia | Toushin City (27) owner 3 | owner 1 |
| 53 | Cell | `F_Cell_Arc > 4`, `F_Cell_ChurchArc 1` | `F_Cell_ChurchArc > 11` |
| 54 | Somita | `F_Somita_Fate 1` | `2` |
| 55 | Pastel | `F_Pastel_State > 4` | `0` |
| 56 | Papaya | `F_Papaya_State 10` | `11` |
| 57 | Fuuka | `F_Fuuka_State > 19` | `19` |
| 58 | Fururu | P(Cordoba (unit 8)), `F_Fururu_State > 2` | `11` |
| 59 | Freya | `N_Freya_State 11` | `12` |
| 60 | Martina | P(Galtia (unit 41)) | `F_Martina_State 100` |
| 61 | Miki | P(Kentarou (unit 31)) | ending 6 |
| 62 | Library Ghost | `F_LibraryGhost_State 6` | not 6 and > 1 |
| 63 | Rona | `F_Redeye_State 13` | `10` |
| 64 | Hanty | Patton (unit 18 or commander 54) and Hanty (unit 19 or commander 57) alive, `F_Patton_State ≠ 0` | Patton (unit 18, commander 54) both at 0, `F_Patton_State ≠ 0` |
| 65 | Seizel & Hawzel | P(Seizel Sisters (unit 42)) | `F_Seizel_State 20` |

Cursors written on the way: `Unit_Cur` (each P), `Cmdr_Cur` (`:2016, 2019`), `Prov_Cur := 27` (`:1671`),
`Tmp0621`, `Tmp0010`, `Tmp0019`; the game ends after them.

### 2.6 Inputs, commands, dead code

- Inputs: `Y 3, 120` (`:27`), `Y 3, 360` and `IK 0` (`:45-46`); the staff roll's `Y 3, 420`, `Y 3, 840`,
  up to 1200 × `Y 3, 3`, the `SR1` poll, `Y 3, 300`; the report: a key after its title (`:202`), one per
  entry shown, after "THE END" and after "Closing". `Y 3, 10000` / `10001` (`:72`, `:74`) switch
  xsystem35's waits to non-cancelable / cancelable (`cmdy.c:118-124`).
- Commands: `CC`, `CF`, `CS`, `G` (with `G n, 40`), `IK`, `J0`, `PF0`, `PF1`, `R`, `SL`, `SR1`, `SS`, `Y 1`,
  `Y 3`, `ZH`, `ZZ0` (`sys_exit(RetVal)`). `SceneRunner` throws on `ZZ0` today (`SceneRunner.cs:255-256`).
- Dead: `:2150-2155` (a second marker initialiser after `\0`), `:2202-2204`, `:2133`, `:2144-2148`,
  `:2128-2132` [I], `Tmp0003` (`:8`) and `Tmp0014` (`:88`) never read, the `Tmp0690` save around `PF0`
  (`:79-81`).

### 2.7 Quirk candidates

| # | What | Lines | Kind |
|---|---|---|---|
| O1 | Ending 7 is no clear, so `Tmp0020` is stale (e.g. `IM Tmp0019, Tmp0020` from `COMMAND.ADV:616`): its staff roll is probably skippable on a first viewing [I] | `:4`, `:73-95`, `:175` | bug |
| O2 | The wait for the credits music polls `SR1` (MIDI) while the music is `SS`: under xsystem35 it returns 0 at once, so it never waits; the loop has no pause inside [I for the original engine] | `:159-165` | engine artefact |
| O3 | Cell's fortune prints "（Misfortune）" but records a fortune | `:1701`, `:1707` | bug, text only |
| O4 | Entries whose two conditions read different variables can show both texts (run together, the second portrait `G 0` after `L_02c95` zeroed `CallArg`, except Leila's and Seizel's) and record the misfortune: Wendy, Kanami, Leila, Menad, Maria, Shizuka, Eleanor Ran, Milli, Cream, Chizuko, Kaoru, Isoroku, Kouhime, Annis, Fururu, Martina, Seizel [I: reach not traced] | §2.5 | bug |
| O5 | No `Tmp0002` guard on Satella's and Kouhime's fortunes (Satella's likely meant, for ending 7) | `:908`, `:1360` | odd design |
| O6 | Ending 9 counts as a clear but shows no fortune; endings 1-4 and 12 show fortunes | `:1-23` | odd design |
| O7 | The clear and the fates persist only into existing save slots | `:2075`; `ﾛｰﾄﾞｾｰﾌﾞ:529-544` | odd design |
| O8 | Kouhime's fortune needs `F_Kou_State = 3` exactly (her arc runs 3-11) and ending 8 | `:1360` | odd design [I] |
| O9 | Dead code (§2.6) | — | dead code |

## 3. `ｽﾀMENU.ADV` — the start menu

### 3.1 Shape

`:1-5` `PCMPLAY` 10 (a no-op), the spec warning (`L_00f3a`, `:716-738`: only with a screen depth ≠ 8
and `SpecGraphics ≥ 210` [I: never on modern hardware]), the logo (`%#ﾌﾟﾛﾛｰｸﾞ`), `SS 27`; `L_00035`
(`:6-40`) draws the title (CGs 301-303); `L_000f5` (`:42-86`) loads the hit boxes and sets the
unlocks; `L_00210` draws the unlocked buttons; `L_0026f`/`L_0028d` (`:97-130`) poll the mouse
(hover highlights from an off-screen sheet; left click 16 on a non-zero state dispatches at
`L_0036f`, `:131-169`; right click 32 shows the button's description in window 113, `L_00c80`
`:585-678`). Items 0, 1, 2, 4, 7 come back to `L_00035` (full redraw and the unlock rolls again);
3, 5, a cancelled debug menu and 8-10 to `L_0026f`.

### 3.2 The buttons (`Glob_MenuItem`, page 4)

State 0 hidden, 1 drawn and clickable (0-8), 2 clickable but never drawn. "Clears" is `Glob_Fate[79]
≠ 0` (§2.2). Unlocks are only ever set.

| # | State set (`:53-83`) | Left click | Back to |
|---|---|---|---|
| 0 | 1 with clears | `HAPPY_C` (Fortune Check) | `L_00035` |
| 1 | 1 with clears | `MMODE` (music room) | `L_00035` |
| 2 | 1 always | `Iｱﾘｽ` (Alice's Mansion) | `L_00035` |
| 3 | 1 with clears on `Y 4, 10` = 10 (`:62-65`) | the requests slip (§3.3) | `L_0026f` |
| 4 | 1 with clears on `Y 4, 20` = 20 (`:67-70`) | `Gｻｲﾄｳ` 1 | `L_00035` |
| 5 | 1 | Load (`ﾛｰﾄﾞｾｰﾌﾞ`, `RetVal 0`; ported) | `L_0026f` |
| 6 | 1 | New game (ported), or with `DebugMode ≠ 0` the "From 1-4" menu (`:249-266`) | — |
| 7 | 1 with clears | `CG` (gallery), then `SS 27` | `L_00035` |
| 8 | 1 | nothing | `L_0026f` |
| 9, 10 | 2 | nothing (10 has a joke description) | `L_0026f` |
| 11 | never loaded (boxes load `< 11`, `:50`; the test scans 0-11, `:701`, `:712`) | chapter 4 (`:166-167`) | unreachable |

The rolls `:62`, `:67` are drawn on every pass of `L_00035` while there are clears, unlocked or not;
`{RetVal - 9:` relies on the saturating `-` (ENG-01): only 10 passes (a naive port gives 9 in 10). Every
pass also marks `Glob_CgSeen[240] := 1` (`:84-85`).

### 3.3 The requests slip (`:182-234`)

`Y 4, 100`; ≤ 64 → `Y 4, 5`, else `Y 4, 15` (the table's count, `:743`); both drawn again while the
pick equals `Ui_StartMenuLastPick` (0 after the boot, `ｽﾀｰﾄ:678`); one of 15 texts (`:745-790`) at
`T 48, 240 + 30k`, `IK 0`. No state but the last pick.

### 3.4 The starts (all `&#MAIN`)

Common: the map camera 1000/400 (`:136-139`), `CurrentBgm := 4` (`:245`; `Iﾌﾟﾛﾛｰｸﾞ:1` overwrites it),
`PF1 1`, `EC 0`, `PF0 1`, `WAO_MAP` 3, `M` 0.

| Start | Lines | Sets | Scene | Status |
|---|---|---|---|---|
| From 1 / New game | `:279-291` | `Week 8`, `StoryStage 0`, `Gold 50` | `Iﾌﾟﾛﾛｰｸﾞ` | ported (`GameSetup.NewGameAsync`) |
| From 2 | `:292-303` | `Week 12`, `StoryStage 1` | `Iﾊﾝﾗﾝ` with the `Ev_Entry` left (below 300: `:10-` runs, `Week + 1`, `Gold 685`) | debug only |
| From 3 | `:304-359` | `Week 16`, `StoryStage 2`, `Gold 30000`; Sill (unit 2), Bound (unit 3), Soul (unit 4) to 0; Leila (unit 5) … Melfeis (unit 13) to 1; Rance's unit as `Iﾊﾝﾗﾝ:20-33` makes it ("Leazas Knights", L. Holy Sword (item 6)) | `Iｼﾚｲﾌﾞ` 2 | debug only |
| From 4 | `:360-546` | `Week 77`, `StoryStage 2`, `Gold 60000`, ~40 story flags (`:370-410`), units, items, five commanders to 0, provinces North (2) … Temple AL HQ (77) Leazas's and seven Kayblis's | none | debug only |

From 2-4 are reached only with `DebugMode ≠ 0` (Alice's secret, EVT-09) — or From 4 through the
unreachable button 11.

### 3.5 Quirk candidates

| # | What | Lines | Kind |
|---|---|---|---|
| M1 | Button 11 never loaded: the boxes load `< 11`, the hit test scans 0-11, so the chapter-4 button and its "Special?" text are unreachable | `:50`, `:166-167`, `:671-677`, `:701`, `:712` | bug |
| M2 | The unlock rolls rely on the saturating `-`, are drawn on every title pass, and stay unlocked once a save writes page 4 | `:62-70` | odd design |
| M3 | Stale highlight: `:102` resets `Tmp0002 := 98` without redrawing the highlighted button (after a cancelled Load or debug menu, or the slip, which also restores a copy taken while it was lit) | `:102`, `:183`, `:233` | bug, screen only |
| M4 | Title side effects carry into the new game: `Gｻｲﾄｳ:8` sets `F_Saitou_State := 1` (then `COM.ADV:705` plays `G2` 3 [I: maybe the intended unlock]); the camera preset | `:136-139`, `:278` | odd design [I] |
| M5 | No music after the music room: `MMODE` ends with `SS 0` and `L_00035` does not play `SS 27` again (the gallery's way does, `:580`) | `:273`, `MMODE:26-37` | bug |
| M6 | The debug presets disagree with themselves: From 3 gives Rance item 6 without `Item_Holder`, sets battle-only modifiers, copies a stale string 3; From 4 is `StoryStage 2` at week 77. (Withdrawn in step 2: "From 4 gives Nikkou (item 2) to Kentarou (unit 31) without his `Unit_Item`" — he holds it from `SET_CHR.ADV:1204`.) | `:304-546` | bug (debug only) |
| M7 | Dead: `:170-181` (XSPEC re-measure), `:553-576` (a map test, a "Start/Don't" menu calling `Gﾜｵ`), `:584`, the WAO helpers' tails `:810-854`, `:863-871`; blank prints `:8-38` [I: blanked by the translation] | — | dead code |

## 4. `HAPPY_C.ADV` — Fortune Check

A viewer of `Glob_Fate[0..65]` (never index 79, never written): two pages of 33 (CGs 837, 838), a stamp
per entry from CG 841 (100 both, 1 fortune, 10 misfortune), a detail view (CG 839, the portrait, the
fortune and misfortune texts, "Undiscovered" for a half not yet seen). Mouse: hover highlights (`CB`),
a left click on a cell opens it, 98 flips the page, 99 or a right click anywhere returns (`%0`, `:89`).
No draws. Writes only scratch (`Glob_FateCur`, `CallArg`, strings 1-6, `Tmp0001-0010`, `MapTargetX/Y`
as `IM` scratch). Quirk candidates: H1 the hover state starts at 0, not 100 (`:45`, `:255`) — a click on
the first poll opens fate 0 or 33 (confirmed in step 3: the button tested before the place, `:50-72`;
the title waits for its release first, so it takes a press during the fade-in); H2 portraits shown for
undiscovered fates; H3 three portraits differ from the one `OVER` sets before an entry's tests (eleven
entries change it for one half): Amiran 39 (`OVER` 40), Somita 46 (47; never drawn by `OVER`), Pastel
41 (493) — 39 and 41 are their misfortunes'; H4 dead code (`:90-91`, `:195-203`, `:382-384`,
`:389-391`, error traps `:192-194`, `:249-250`, `:277-280`, `:346-347`, `:1449-1450`).

## 5. Against survey-main

Its W4 row ("`ｽﾀｰﾄ`, `ｽﾀMENU` new-game paths, `ﾛｰﾄﾞｾｰﾌﾞ`, `OVER` state lines") is mostly done: the
boot, the new game, `ﾛｰﾄﾞｾｰﾌﾞ` and page 4's store are ported (F1, G0). Corrected here: `OVER` has no
per-ending epilogue (§2.3); the "dead debug picker `:554-576`" is a dead "Start/Don't" menu, the live
debug menu is `:249-266` (§3.4); the unlock rolls pass only on 10 and 20 (§3.2).

## 6. Open questions — for the owner

1. **How should the frame run?** (a) `OVER`, `ｽﾀMENU` and `HAPPY_C` run from the player's files
   through `SceneRunner` and draw on the engine surface (decision 12's way for scenes), with the
   engine commands they need (`ZZ0` as the end, `SR1`, `SL`, `Y 3, 10000/10001`, `IZ`, `ZZ3`, `F1`/`F3`
   data reads, `MC`, `CB`, … — which of them `SceneRunner` lacks is to be listed by reading the three
   files as programs); or (b) the rules ported by hand (the clear, the fate record, the unlocks) under
   screens of our own (decisions 9-10). Most of the three files is presentation; the rules are §2.2,
   §2.4, §2.5 and §3.2.
2. **After `ZZ0`**: quit the program as the original (STR-10), or back to the title as now?
3. **Scope**: the gallery (`CG`, 3977 lines, replays scenes at entry 300 — EVT-07), the music room
   (`MMODE`), Alice's Mansion (`Iｱﾘｽ`), `Gｻｲﾄｳ`, the requests slip, the debug starts From 2-4 — in
   milestone 6 or later?
4. **Our own title** (decision 10) — kept, or replaced by `ｽﾀMENU`?

## 7. Proposed order (after the owner's answers)

0. This survey ✓.
1. `OVER`: the clear, the fate record and `ﾛｰﾄﾞｾｰﾌﾞ` 11 (the rules), then the report and the ending's
   screen per question 1; content tests from the player's files — an ending recorded on page 4 and
   kept in a slot; the roster against chosen states.
2. `ｽﾀMENU`: the unlocks (and the rolls) and the buttons per questions 1, 3, 4; New game and Load stay
   as ported.
3. `HAPPY_C` per question 3.
4. Docs: the quirk candidates O1-O9, M1-M7, H1-H4 registered; survey-main's W4 row.
