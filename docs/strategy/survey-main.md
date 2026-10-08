# Survey — main loop, command menu, event dispatcher, game frame

Scope: `MAIN`, `COMMAND`, `COM`, `MMODE`, `M`, `WAO_MAP`, `ｽﾀｰﾄ`, `ｽﾀMENU`, `ﾛｰﾄﾞｾｰﾌﾞ`,
`ｵｰﾌﾟﾆﾝｸﾞ`, `ﾌﾟﾛﾛｰｸﾞ`, `OVER`, `HAPPY_C`, `CG`, `TEST`, `XSPEC`, `PCMPLAY`, `ﾓｼﾞｷｬｯｼｭ`, `END`.
Source: `_extract\scenario_named\` (2026-10-08). Confidence: **[C]** read, **[I]** inference,
**[G]** guess. xsystem35 semantics cite `F:\sources\Rance\xsystem35-sdl2\src`.

## 1. Role

`ｽﾀｰﾄ` boots (zeroes the variables, runs `SET`) and jumps to the title `ｽﾀMENU`; a new game
(or a debug chapter start) sets week/stage/gold, plays the prologue scene and jumps (`&`) to
`MAIN`, which never returns: one pass of its loop is one **week** — autosave, a player phase
(income/replenishment, then `COMMAND`), then a fixed sequence of gated nation phases (`T*`
files, plus two armies scripted inline), then end-of-week story checks. `COMMAND` runs the map
and the command menu and calls the `C*` command files; on entry it calls `COM`, the weekly
event dispatcher (counter upkeep + an ordered list of story-event triggers into `G*`/`I*`).
`ﾛｰﾄﾞｾｰﾌﾞ` is save/load (one engine snapshot per week) and the persistent page 4. Game overs and
endings jump to `OVER` (epilogue, records fates on page 4, quits). `M`/`WAO_MAP` are the map
helpers. Everything else in the group is presentation (title, gallery, music room, logo,
opening credits, benchmark, font cache, sound wrapper) or debug (`TEST`).

## 2. Inventory

| File | Lines | Entered from | What it is | Rules? |
|---|---|---|---|---|
| `MAIN` | 1155 | `&` from `ｽﾀMENU.ADV:291,303,359,546` | week loop | **yes** |
| `COMMAND` | 718 | `MAIN.ADV:69` (thieves), `:213` (Leazas) | player phase: map, menu, construction timer, Turn END | **yes** |
| `COM` | 947 | `COMMAND.ADV:3` only | weekly upkeep + event dispatcher | **yes** |
| `ﾛｰﾄﾞｾｰﾌﾞ` | 656 | `MAIN.ADV:1058`, `COMMAND.ADV:374`, `ｽﾀMENU.ADV:241`, `SET.ADV:340`, `OVER.ADV:2075`, `TEST.ADV:93` | save/load, persistent page sync | state only |
| `ｽﾀｰﾄ` | 872 | boot (first file in `xsys35dc.hed`) | zero 846 vars (`ｽﾀｰﾄ.ADV:1-846`), window setup, `XSPEC`, `SET`, `&ｽﾀMENU` (`:851-872`) | init |
| `ｽﾀMENU` | 871 | `ｽﾀｰﾄ.ADV:872` | title menu, new game, debug chapter starts | new-game setup |
| `M` | 312 | 2984 calls, ops 0/1/3/4/10-13 (§3) | camera, owner markers, target blink | ported (`Battle.Map.cs`) |
| `WAO_MAP` | 196 | `M`, `COMMAND.ADV:61` (op 1), title/load (op 3) | blit, drag-scroll input loop (op 1), marker list (3/4/5), debug picker (100) | UI |
| `OVER` | 2210 | `&` from 19 sites (FIGT_B, 死亡捕獲, Iｶﾐ ×6, Gｻﾃﾗ, TJAPAN, Iﾄｳｿﾞｸ, …, `TEST.ADV:343`) | ending CG `269+EndingId` (`:33`), completion report, fate record, `ZZ0` quit (`:2083`) | page 4 only |
| `HAPPY_C` | 1451 | `ｽﾀMENU.ADV:236` | "Fortune Check": reads page 4 (`Glob_Fate`), draws it | no |
| `CG` | 3977 | `ｽﾀMENU.ADV:579`, `TEST.ADV:94` | CG gallery; replays CGs via entry 300 of 80 scene files (`CG.ADV:840-857`) | no |
| `MMODE` | 560 | `ｽﾀMENU.ADV:273` | music room; `US`/`UG` save and restore `RetVal`, `CallArg`, `Tmp0001-20`, `Tmp0021-40` (`MMODE.ADV:1-4,34-37`) | no |
| `ｵｰﾌﾟﾆﾝｸﾞ` | 1513 | `COMMAND.ADV:422`, `Cﾃｷﾄｼ.ADV:105`, `Iｱﾘｽ.ADV:1340` | opening-credits animation after the prologue; sets `F_OpeningSeen := 1` (`:2`), `US/UG MapTargetX..+3` (`:1,1420`) | flag only |
| `ﾌﾟﾛﾛｰｸﾞ` | 187 | `ｽﾀMENU.ADV:4` | AliceSoft logo animation | no |
| `TEST` | 660 | `COMMAND.ADV:328` (menu "TADA", `DebugMode = 1`) | debug: extra command, money, buildings, time, RNG histogram (`:425-440`), all items/units/CGs, game over, God dungeon | debug |
| `XSPEC` | 48 | `ｽﾀｰﾄ.ADV:851`, `ｽﾀMENU.ADV:179` | timing benchmark → `SpecGraphics/SpecCpu` | no |
| `PCMPLAY` | 46 | 52 calls | `SP` sound effect wrapper; `CallArg 10` is a no-op (`:17-19,45`) | no |
| `ﾓｼﾞｷｬｯｼｭ` | 101 | `SET.ADV:331` | draws text off-screen (font cache warm-up) | no |
| `END` | 0 | nobody | empty file | — |

Main procedures:

| Proc | Lines | Purpose |
|---|---|---|
| `MAIN L_00020` | `MAIN.ADV:1-1045` | week loop (see §2.1) |
| `MAIN L_01831` | `:1046-1055` | Camilla-unit commander refresh: HP+2, troops + 3×recovery, capped |
| `MAIN L_01875` | `:1056-1059` | autosave: `RetVal := 3`, `%#ﾛｰﾄﾞｾｰﾌﾞ` |
| `MAIN L_01881` | `:1060-1076` | date: `DateYear = 3+(Week-1)/48`, `DateMonth`, `DateWeek` (4 weeks × 12 months); leaves `Tmp0001-0005` |
| `MAIN L_0191e/01a0b/019fe/01a25` | `:1089-1155` | `WAO_SUB` op 113 (open), 1 (key wait), 0 (close), 3 (`Ui_FaceOverMap`) |
| `COMMAND` top | `COMMAND.ADV:1-46` | `COM`, pan to castle, construction countdown, reset weekly availability flags |
| `COMMAND L_00129..L_0024d` | `:47-161` | map loop: `WAO_MAP` op 1 drag, left click → hit-test provinces (radius 12, capitals 20-36) and dungeons (radius 10); right click → menu |
| `COMMAND L_00431` | `:173-316` | command menu (thieves variant `:183-225`, Leazas variant `:226-316`) |
| `COMMAND L_007cf` | `:377-425` | Turn END: Sill scene, Tange Dog revive, prologue end (`Week > 22`) |
| `COMMAND L_00a95` | `:514-577` | province click → `C*` choice (§2.2) |
| `COMMAND L_00c21` | `:578-614` | construction finished → first building in state 2 → 1 and its scene |
| `COMMAND L_008e4 / L_0099a` | `:426-489` | status line (sum of troops); leaves `Unit_Cur = 51` / `4` |
| `COM` | `COM.ADV:1-947` | §2.3 |
| `ﾛｰﾄﾞｾｰﾌﾞ` dispatch | `ﾛｰﾄﾞｾｰﾌﾞ.ADV:1-29` | `RetVal` 0 load menu, 1 save menu, 2 Load/Save/Don't, 3 autosave, 10 read page 4 from all slots, 11 write page 4 to all slots |

### 2.1 One week, in order (`MAIN.ADV`)

| # | Step | Gate | Lines |
|---|---|---|---|
| 1 | `WAO_SUB` op 3, `M` op 0 (redraw) | — | `:2-4` |
| 2 | `Week += 1`; history shift (`NB` ×4, 299 entries) and clear this week's `Hist_*` | — | `:5-15` |
| 3a | **Thieves phase** (whole week): autosave; date; BGM 1; pan to dungeon 1; banner; units 1-4 alive → status 1, HP full, troops +2 (cap); `Hist_Funds := Gold`; `COMMAND`; if `StoryStage = 99` → `StoryStage := 1`, `Iﾊﾝﾗﾝ`; "Then some time passed"; loop | `StoryStage = 0` | `:19-82` |
| 3b | **Leazas phase**: autosave (`:83`); date, BGM 4, pan to province 1, banner | otherwise | `:83-112` |
| 4 | Income: sum `Prov_Economy` of owner-1 provinces (`Tmp0022`; count in `Tmp0021`, unused) | — | `:113-126` |
| 5 | Cream counter (unit 28): status 1 & `F_Cream_State > 10` → +1; status 2 → 11 | — | `:127-133` |
| 6 | Units 1-48 alive: item 48 → status 1, troops −= rnd(troops/20), **skip rest**; item 50 → troops += rnd(max/10); status := 1; HP +2 (cap); not full → troops += rnd(2×Recovery) + Recovery (cap); `Tmp0024` = total replenished | — | `:134-167` |
| 7 | `Gold += income`; `Hist_Funds := Gold`; report window | — | `:168-184` |
| 8 | Expenses: M Land −20, AL Church −65, Galtia food −45 (each to `Hist_SundryExp`) | `F_MLand_Funded = 1`; `N_ALChurch_Stage < 19`; unit 41 alive | `:185-211` |
| 9 | `COMMAND` (player phase, §2.2), then `M` op 0 | — | `:213-215` |
| 10 | Post-command scenes: `Gﾌｳｶ` 4, `Gｶﾄﾞﾐｭｳﾑ` 1, `Gｸﾘｰﾑ` 3 | `F_Fuuka_State = 9`; `F_Asuka_State = 1`; unit 28 status 1 & `F_Cream_State > 19` | `:216-228` |
| 11 | Mars the Great: `Ev_PhaseNation := 3`, `Gｶｾｲ` 2 | `N_Mars_Score = 1` | `:229-259` |
| 12 | AL Church: `Ev_PhaseNation := 11`, `GAL` 5 | `N_ALChurch_Stage ∈ {21, 100}` | `:260-297` |
| 13 | Leazas Rebels: cmdrs 1-7 full HP, troops + 3×rec; `TCﾘｰｻﾞｽ` | `StoryStage = 1` | `:298-347` |
| 14 | JAPAN: cmdrs 72-82 HP+2, troops + 3×rec; `TJAPAN` | `F_Japan_State = 1` | `:348-400` |
| 15 | Helman: cmdrs 22-57; `Tﾍﾙﾏﾝ` | `F_Helman_War = 1` | `:401-453` |
| 16 | Patton Army: cmdrs 54-57; `Tﾊﾟｯﾄﾝ` | `F_PattonArmy_State ≠ 0` | `:454-506` |
| 17 | Zeth: BGM 10/12; capital = first of provinces 69/68/66/70/67 owned by 7; cmdrs 101-128, 165-180; `Tｾﾞｽ` | `F_Zeth_War = 1` | `:507-593` |
| 18 | Kayblis: `Ev_PhaseNation := 8`, string 10 := "Kayblis Unit", `Tｹｲﾌﾞﾘｽ` (no refresh here) | `StoryStage = 3` & `F_Kayblis_War ∈ {0, 2}` | `:594-630` |
| 19 | Camilla Unit: **inline AI** — cmdrs 92/93/90/92/91 each pick province rnd(16), attack via `FIGT_B` if player-owned and a roll passes; Sieg/Seizel/Satella/Rei/Camilla scenes | `N_Camilla_Stage > 1` & `≠ 100` | `:631-815` |
| 20 | Creator: `Ev_PhaseNation := 11`, string 10 := "Creator", `Tﾙﾄﾞﾗ` | `StoryStage = 4` | `:816-847` |
| 21 | La Vaswald: pan to `LaVaswald_Prov`, `Gｻｲｾﾞﾙ` 12 | cmdr 98 alive | `:848-883` |
| 22 | `Iﾏｼﾞﾝ` 1 (BGM 16) / `N_Camilla_Stage 0 → 1` | `StoryStage = 2` & Camilla 1 / 0 | `:887-895` |
| 23 | Sill kidnapping chain `Gｼｨﾙ` 21-27 at weeks >10/20/30/35/40/45/50 | `F_Helman_War ≠ 2`, `F_SillKidnap_Stage = 0..6` | `:896-925` |
| 24 | `Gﾏｼﾞｯｸ` 5 | `F_Magic_State ∈ {2,3}` & `N_Xacalite_KillsLeft ≠ 0` | `:926-931` |
| 25 | `N_Majin_EventTimer −= 1` (floors at 0); forced 0 when both wars won and `F_DemonWar_Stage < 10` | — | `:932-935` |
| 26 | Demon war: `Iﾏｼﾞﾝ` 51/50/20…11 by `F_DemonWar_Stage` 11…0, tested **descending** | `F_Kayblis_War = 0`, Camilla > 5, timer 0 | `:936-985` |
| 27 | Kayblis war: units 29 or 30 alive → `F_DemonWar_Stage := 0`; `Iﾏｼﾞﾝ2` 30…21 by stage 9…0 descending | `F_Kayblis_War = 1`, timer 0 | `:986-1035` |
| 28 | `M` op 0, "Then some time passed", `@L_00020` | — | `:1036-1045` |

Every nation banner calls `L_01881` (date) and pans the camera (`M` ops 1, 10) to the nation's
capital [C]. Gating flags are only written by story scenes and `T*` files, never here, except
`StoryStage 99 → 1` (`:70-71`) and `N_Camilla_Stage` (`:808-810`, `:893-895`) [C].

### 2.2 Player phase (`COMMAND.ADV`)

Entry order: `PCMPLAY` op 10 (no-op) → **`COM`** (`:3`) → pan to castle (`:4-13`) →
`BuildWeeksLeft −= 1`, at 0 → `L_00c21` (3× in debug) (`:18-37`) → all weekly flags := 1
(`Ui_CmdHaremAvail … CmdAvail_SubTalk`, `:38-46`) → map loop. `Ev_CommandMode` 1 forces
`Gｶﾌｪ` 30 on every menu open, 2 locks the menu (map only) (`:175-182`) [C].

| Player action | Leads to | Lines |
|---|---|---|
| click own province | `Cｼﾊｲﾄｼ` | `:527-530` |
| click province adjacent (Adj1-5) to an owned one | `Cﾃｷﾄｼ` (attack/negotiate; it calls `Cﾆﾝｼﾞｬ`) | `:531-574` |
| click any other province | `Cﾃｷﾄｼ2` (view; calls `Cﾆﾝｼﾞｬ`) | `:575-576` |
| thieves phase: click province 33/34 / other | `Cﾃｷﾄｼ` (ends phase if it set `StoryStage 99`) / `Cﾃｷﾄｼ2` | `:516-526` |
| click dungeon whose province is owned or 0 | `Cﾒｲｷｭｳ` | `:145-158` |
| menu Construction / Harem / Treasure / Merchant / Subordinates | `Cﾃｲﾄ` / `Cﾊｰﾚﾑ` / `Cﾀｶﾗ` / `Cｼｮｳﾆﾝ` / `Cﾌﾞｶ` | `:330-360` |
| menu All Data / Think / TADA (debug) | `各種DATA` / `Iｶﾝｶﾞｴ` / `TEST` | `:320-329,365-369` |
| menu Load/Save | `ﾛｰﾄﾞｾｰﾌﾞ` with `RetVal 2` | `:370-376` |
| menu →Leazas Castle / →Thieves' Hideout | camera pan only | `:490-513` |
| menu Turn END | `L_007cf`, then `%0` to MAIN | `:377-425` |
| (unreachable) | `Cｹﾝｷｭｳ` (1-line file) | `:361-364` |

"×" items jump to `L_006eb` (menu again); "△ Merchant" also blocks, "△ Subordinates" still
opens `Cﾌﾞｶ` [C]. Availability flags are cleared by the `C*` files themselves (dictionary).

### 2.3 Event dispatch (`COM.ADV`) and the `G*`/`I*` tables

Every scene file starts with a table `{Ev_Entry = n: @L_x}` (e.g. `Gｼｨﾙ.ADV:1-30`);
122 files carry one, 785 entries in all; 80 files have entry 300 (gallery CG viewer) [C,
script count]. Callers set `Ev_Entry` and page-call the file; there is no central table —
`COM` and `MAIN` are hand-written `if` chains. `COM` is called **once per week**, only from
`COMMAND.ADV:3`, i.e. at the start of the player phase, before the player gets control.

| Section | Lines | Behaviour |
|---|---|---|
| Early exits | `:1-10` | `Week = 9` → `Iｼﾚｲﾌﾞ` 10, return; `StoryStage = 0` → `Gｿｳﾙ` 4 (weekly thieves message), return — **upkeep below is skipped** |
| Upkeep | `:11-84` | ninja-used flags := 99 then 0 per available ninja; clear garrison battle vars; Goliath 2→1; counters (`N_Melfeis_Stage`, `N_Isoroku_Stage 5→6`, `N_Aviator_StudyWeeks −1`, `N_Miki_Timer −1` until 5, `N_Aaya_Stage`, `N_Saizel_Weeks`, `N_ALChurch_Stage <3`, `N_Soul_Stage`); Lady Caravan walk (rnd 2 = ±1 province, or +1 after capture); `TangeDog_UsedThisWeek := 0`; kaiju attack growth (items 26/27, cap 2000) |
| Tier 1 "all that apply" | `:85-300` | 38 calls; each sets `Ev_WeeklyEventDone := 1` and execution **continues**, so several scenes may play in one week; `:301-303` return if any fired |
| Tier 2 "first match" | `:304-946` | 111 calls; each followed by `%0`, except 5 that fall through (`Gﾕｳﾚｲ` 6 `:400`, `Gﾌﾘｰｸ` 2 `:406`, `Gｱﾋﾞｧﾄｰﾙ` 3 `:662`, `Gｱｰﾔ` 3 `:666`, `Gｵｳｼﾞ` 1 `:940`); 11 random rolls interleaved (§5); `N_Miki_Stage 4 → 5` returns without a scene (`:557-560`) |

`COM` reaches 142 distinct (file, entry) pairs in 73 files; `MAIN` 47; `COMMAND` 9 (building
scenes, `Gｶﾌｪ` 30, `Gｼｨﾙ2` 5, `Iﾄｳｿﾞｸ` 2). Other event callers: `C*` 241 calls, `T*` 147,
`死亡捕獲` 41, `CG` 139, `FIGT_A/B` 23 [C, script count]. Conditions are pure reads of story
flags, unit/commander status via cursors, `Week`, `DateMonth/DateWeek` (`:320`, New Year
feast) and the `*_EventWeek` timers.

### 2.4 New game, load, save, game over

| Flow | Path | Lines |
|---|---|---|
| Boot | zero 846 vars → `XSPEC` → `SET` (allocs, `SET_MAP/CHR/ITEM`, `ﾓｼﾞｷｬｯｼｭ`, reads page 4 via `ﾛｰﾄﾞｾｰﾌﾞ` 10) → `&ｽﾀMENU` | `ｽﾀｰﾄ.ADV:1-872`, `SET.ADV:328-340` |
| Title | spec warning, `ﾌﾟﾛﾛｰｸﾞ`, button table `L_011ca`; unlocks from `Glob_Fate[79]` (clears) | `ｽﾀMENU.ADV:1-169` |
| New game | `Start` → `WAO_MAP` 3, `M` 0, `Week := 8`, `StoryStage := 0`, `Gold := 50`, `Iﾌﾟﾛﾛｰｸﾞ`, `&MAIN` | `ｽﾀMENU.ADV:244-291` |
| Debug starts | `DebugMode = 1` → menu "From 1-4": week 12/stage 1 (`Iﾊﾝﾗﾝ`), week 16/stage 2 (`Iｼﾚｲﾌﾞ` 2, units set), week 77 late-game preset (≈40 flags, provinces 2-77 to Leazas, 78-83/89 to Kayblis) | `:292-546` |
| First week | `Week` 9 → `COM` plays `Iｼﾚｲﾌﾞ` 10 | `COM.ADV:1-5` |
| Prologue end | the lost duel in `Iﾄｳｿﾞｸ` writes `StoryStage := 99` (`:1841`); reached by Turn END with `Week > 22` (`Iﾄｳｿﾞｸ` 2) or by the Merci path (Pillage on 34 with `F_Thieves_State = 3`) → `ｵｰﾌﾟﾆﾝｸﾞ` → MAIN sets 1 and plays `Iﾊﾝﾗﾝ`; see README "What the prologue reaches" | `COMMAND.ADV:419-424`, `Cﾃｷﾄｼ.ADV:99-108`, `MAIN.ADV:70-73` |
| Autosave | each week at phase start: `QD 1` (slot 1), then page 4 to all slots | `MAIN.ADV:20,83`, `ﾛｰﾄﾞｾｰﾌﾞ.ADV:30-58` |
| Manual save | `QC n,1` = **copy of this week's autosave** into slot 2-10, `QP n, Tmp0022` patches the slot number in | `ﾛｰﾄﾞｾｰﾌﾞ.ADV:325-512` |
| Load | page 4 from all slots, `QC 1,n` (autosave := chosen slot), `LD n` → resumes after `QD 1` with `RetVal 0` → "Load Complete", `SS CurrentBgm`, returns into `MAIN` at the phase start | `ﾛｰﾄﾞｾｰﾌﾞ.ADV:105-299,59-81` |
| Game over / ending | caller sets `EndingId` and `&OVER`; `OVER` counts a clear for `EndingId` 5, 8, 9, 10, 11, 13 (`:1-5`), shows the fate roster, writes `Glob_Fate`, `ﾛｰﾄﾞｾｰﾌﾞ` 11, "THE END", `ZZ0` (exit program — no return to title) | `OVER.ADV:1-2083` |

Save contents (xsystem35 `savedata.c:639-720`, `cmdq.c:35-82`): PC (page, index) and the call
stack, all string variables, and every array page allocated with save flag 1 — page 0 (all
806 vars), 1 (units/cmdrs/provinces/items/dungeons, `SET.ADV:135`), 2 (history, `:241`),
4 (persistent, `:282`), 10 (map markers, `:320`); page 3 (scratch buffer, `:269`) is **not**
saved [C]. Because every save is a copy of the autosave, a save is always "the state right
after `Week += 1`, history shift and BGM stop, at the start of the player phase" [C].

## 3. Call graph (counts of `%#` call sites)

| From | To |
|---|---|
| `MAIN` | `M` 27, `WAO_SUB` 25, `COMMAND` 2, `ﾛｰﾄﾞｾｰﾌﾞ` 1, `T*` 7 (`TCﾘｰｻﾞｽ`, `TJAPAN`, `Tﾍﾙﾏﾝ`, `Tﾊﾟｯﾄﾝ`, `Tｾﾞｽ`, `Tｹｲﾌﾞﾘｽ`, `Tﾙﾄﾞﾗ`), `FIGT_B` 4, `Iﾏｼﾞﾝ` 13, `Iﾏｼﾞﾝ2` 10, `Gｼｨﾙ` 7, `Gｼﾞｰｸ` 4, `Gｻｲｾﾞﾙ` 3, `Gｶﾐｰﾗ` 3, `Iﾊﾝﾗﾝ`, `Gﾌｳｶ`, `Gｶﾄﾞﾐｭｳﾑ`, `Gｸﾘｰﾑ`, `Gｶｾｲ`, `GAL`, `Gｻﾃﾗ`, `Gﾚｲ`, `Gﾏｼﾞｯｸ` 1 each |
| `COMMAND` | `COM` 1, `M` 11, `WAO_MAP` 1, `WAO_SUB` 23, `PCMPLAY` 2, `Cﾃｷﾄｼ` 6, `Cﾃｷﾄｼ2` 2, `Cｼﾊｲﾄｼ`, `Cﾒｲｷｭｳ`, `Cﾃｲﾄ`, `Cﾊｰﾚﾑ`, `Cﾀｶﾗ`, `Cｼｮｳﾆﾝ`, `Cﾌﾞｶ`, `Cｹﾝｷｭｳ` (dead), `各種DATA`, `Iｶﾝｶﾞｴ`, `TEST`, `ﾛｰﾄﾞｾｰﾌﾞ` 1 each, `Gﾏﾘｽ` 5, `Gｱｰﾔ`, `Gｶﾌｪ`, `Gｼｨﾙ2`, `Iﾄｳｿﾞｸ`, `ｵｰﾌﾟﾆﾝｸﾞ` 1 each |
| `COM` | 151 scene calls (2 early exits + 38 tier 1 + 111 tier 2) into 73 `G*`/`I*` files (most: `Gﾏﾘｽ` 9, `Gﾐｷ` 8, `Gｶﾞﾝｼﾞｰ` 7, `Gｶﾄﾞﾐｭｳﾑ` 6, `GAL` 5); no rule files |
| `ｽﾀMENU` | `M` 7, `WAO_MAP` 6, `ﾌﾟﾛﾛｰｸﾞ`, `XSPEC`, `HAPPY_C`, `ﾛｰﾄﾞｾｰﾌﾞ`, `Iｱﾘｽ`, `MMODE`, `Gｻｲﾄｳ`, `Iﾌﾟﾛﾛｰｸﾞ`, `Iﾊﾝﾗﾝ`, `Iｼﾚｲﾌﾞ`, `CG`; `&MAIN` 4 |
| `M` op use (whole scenario) | op 0 ×2184, 1 ×298, 10 ×296, 4 ×152, 3 ×46, 11/12 ×3, 13 ×1; op 5 never [C, `CallArg` set on the line before the call] |

`M` and `WAO_MAP` ops 0, 1, 3, 4, 10-13 are ported for state in
`src/Kichikuou.Core/Combat/Battle.Map.cs`; not ported: `WAO_MAP` op 1 (drag-scroll input,
`WAO_MAP.ADV:5-82`, used only by `COMMAND`), op 5 (marker delete, never reached) and op 100
(debug coordinate picker, only in dead code `ｽﾀMENU.ADV:558`).

## 4. State

| Group | Read | Written |
|---|---|---|
| time | `Week`, `StoryStage`, `Date*` | `Week` (`MAIN:5`), `StoryStage` (`MAIN:71`, `COMMAND:423`, `ｽﾀMENU`), `Date*` (every `L_01881`, `COMMAND L_00d03`, `ﾛｰﾄﾞｾｰﾌﾞ L_00ba9`) |
| economy | `Gold`, `Prov_Economy`, `BuildWeeksLeft`, `Bld_*` | `Gold` (`MAIN:170-206`), `BuildWeeksLeft`, `Bld_* 2→1` (`COMMAND:18-37,578-614`) |
| history (page 2) | — | shift and clear (`MAIN:6-15`), `Hist_Funds` (`:68,171`), `Hist_SundryExp` (`:192-206`) |
| unit / cmdr | status, HP, troops, recovery, item via cursors | weekly refresh (§2.1 rows 3a, 6, 13-17, 19); `Unit_Status 2→1` by Tange Dog (`COMMAND:384-418`) |
| event | ~120 story flags and counters (`COM` 191 writes, mostly `Ev_Entry`) | `Ev_Entry`, `Ev_WeeklyEventDone`, `Ev_PhaseNation`, `Bt_EnemyParty1-4`, `Bt_EnemyRetreatMode` (MAIN Camilla block) |
| ui (golden-ignored) | `Ui_Cmd*Avail`, `Ev_CommandMode` | weekly flags := 1 (`COMMAND:38-46`), camera, drag, markers |
| system | `BgmEnabled`, `CurrentBgm`, `DebugMode` | `CurrentBgm` per phase (restored by `SS CurrentBgm` after load) |
| strings | — | `MS 10` army name: "Kayblis Unit" / "Camilla Unit" / "Creator" (`MAIN:628,664,845`); `MM 1` unit name (`COMMAND:387`) |
| page 4 (persistent) | `Glob_Fate[79]` (clears) | `OVER:2-4` (clear count), fates per character (`OVER L_02a10`); synced to every slot by `ﾛｰﾄﾞｾｰﾌﾞ` 10/11 (`:513-544`); `VAR0194` saved too, never set |

Aliasing: `Hist_Prev*` = `Hist_*` at index 1 (`SET.ADV:244-255`); the shift is
`NB Hist_PrevFunds, Hist_Funds, 299` — a forward copy onto an overlapping block (§7).
Cursor leftovers that later code may see: `Unit_Cur = 51` after the Leazas status line
(`COMMAND:428-439`), `Unit_Cur`/`Cmdr_Cur`/`Prov_Cur` from the gates (e.g. `MAIN:848` leaves
`Cmdr_Cur 98`), `Tmp0001-0005` from the date routine, `RetVal` from the last roll.

## 5. Randomness (`Y 4` sites)

| File:line | n | Condition / use |
|---|---|---|
| `MAIN.ADV:143` | `Unit_Troops/20` | unit alive with item 48: troop loss |
| `MAIN.ADV:148` | `Unit_MaxTroops/10` | unit alive with item 50: troop gain |
| `MAIN.ADV:160` | `Unit_Recovery*2` | each unit 1-48 alive, not item 48, troops < max |
| `MAIN.ADV:689, 691` | 16, 2 | Camilla phase, cmdr 93 alive: target province, attack if roll ≠ 2 |
| `MAIN.ADV:725, 727` | 16, 3 | cmdr 90 alive: province, attack if roll ≠ 2 and HP full |
| `MAIN.ADV:751, 753` | 16, 3 | cmdr 92 alive: province, attack if roll ≠ 2 |
| `MAIN.ADV:782, 784` | 16, 2 | cmdr 91 alive: province, attack if roll = 1 |
| `COM.ADV:59` | 2 | Lady Caravan walk, `F_Cafe_State < 10` (upkeep, every normal week) |
| `COM.ADV:459, 465, 471` | 4, 2, 5 | Tier 2, unconditional once reached: Kapala, Menad, Suzume |
| `COM.ADV:612, 618, 633` | 15, 3, 10 | Pastel, Merim, Barres |
| `COM.ADV:694` | 10 | Seizel (`N_Saizel_Weeks > 40`) |
| `COM.ADV:742` | 5 | only if unit 26 alive (Mars scene) |
| `COM.ADV:800` | 2 | Elena 4 (roll 1) / 5 (roll 2) |
| `COM.ADV:878` | 10 | one roll shared by Leila (1), Brittany (2), Julia (3), Horaga (4), Magic (5) |
| `COM.ADV:934` | 10 | Kaiju prince; forced 1 after either war is won |
| `ｽﾀMENU.ADV:62, 67` | 10, 20 | title-menu unlocks after a clear (1 in 10 / 1 in 20 per title visit) |
| `ｽﾀMENU.ADV:191-196` | 100, 5 or table count | title "requests" slip, re-rolled until ≠ last pick |
| `TEST.ADV:434` | 4 | debug RNG histogram (100 draws) |
| `XSPEC.ADV:35` | 1 | n = 1: no draw (`Y 4` returns n for n ≤ 1) |

`COMMAND` and `ﾛｰﾄﾞｾｰﾌﾞ` draw nothing. The number of `COM` draws in a week depends on which
tier-1/tier-2 trigger fires first, so the order is story-dependent; a week with any tier-1
scene draws only `:59` [C]. Draws with `n = 0` (e.g. recovery 0, troops < 20) consume nothing.

## 6. Input and display

| Where | Input | Shown |
|---|---|---|
| `COMMAND` map loop | `IM` polling (`:63,77,163,169`), `IC` cursor shape, drag via `WAO_MAP` op 1 (`IM` loop, `Y 3, Ui_ScrollWait`) | scrolling map, markers |
| `COMMAND` menus | `]` (`:221,313`), cancel returns to map | status line: funds, troops, month/week |
| `MAIN` banners, income report, "time passed" | key wait via `WAO_SUB` op 1 (`L_01a0b`) | phase name + date, revenue/replenished/expenses |
| `ﾛｰﾄﾞｾｰﾌﾞ` | `]` Load/Save/Don't, slot lists (10 slots, auto = 1), `IM` release wait | slot dates via `LP` of `Week` |
| `ｽﾀMENU` | `IM` hit-test on 12 buttons (`L_00e91`), right click = description | title, descriptions |
| `OVER` | `IK 0`, timed `Y 3` | ending CG, fortune/misfortune roster |
| `HAPPY_C`, `CG`, `MMODE` | `IM`, `IK` | gallery screens |

All of it can be redesigned freely: the only state the display code writes is `Date*`,
`Tmp*`, `Ui_*`, cursors, `CurrentBgm`, string 10 and the marker list. The menus are **not**
rules: availability is just the `Ui_Cmd*Avail` / `CmdAvail_*` flags; the real decisions
happen in the `C*` files.

## 7. Rules vs presentation; quirks

Separation is fair: `MAIN` interleaves 5-30 lines of banner code per phase but the rule lines
are contiguous blocks; `COM` is pure rules (no drawing at all); `COMMAND` is mostly UI with
three small rule blocks (construction timer, weekly flags, Turn END).

Quirks (port them):

1. **History shift smears** (`MAIN.ADV:8-11`): `NB` copies forward one cell at a time
   (`cmdn.c:56-58`), so `NB Hist[1], Hist[0], 299` sets all 300 entries to last week's value
   under xsystem35 [C]. Whether `system39.exe` used a block move is unknown [G]. The core has no
   `NB` helper yet.
2. **Stale `RetVal`** in the Camilla block: `:798`/`:803` test `RetVal = 2` from cmdr 91's
   roll, but if 91 is dead it is whatever cmdr 92's block or its `FIGT_B`/scene left [C].
3. `N_Rei_Stage = 0` re-opens `Ev_WeeklyEventDone` (`:754-756`), allowing a second Camilla-unit
   battle in one week [C]. Cmdr 93 is refreshed (`L_01831`) only when `N_Miki_Stage = 1` [C].
4. `COM` early exits (`:1-10`) skip all upkeep in week 9 and in the whole thieves phase
   (Lady Caravan does not move, timers do not tick) [C].
5. Item 48 holders skip HP recovery (`MAIN.ADV:141-146`) [C]; expenses can drive `Gold` to 0
   only (saturating subtraction, no debt) [C].
6. Patton commanders 54-57 are refreshed twice if both the Helman and Patton phases run
   (`:431-448`, `:484-501`) [C]; whether both gates can be open together is unknown.
7. Demon-war chains test stages in descending order so a scene that advances the stage
   cannot chain in the same week [I]. Units 29/30 alive reset `F_DemonWar_Stage` to 0 every
   Kayblis-war week (`:987-994`) [C].
8. `COMMAND L_00c21` returns (`\0`) after the first finished building [C]; the
   `BuildWeeksLeft` timer is shared by all buildings [I].
9. Dead code: `COMMAND.ADV:361-364` (`Cｹﾝｷｭｳ`), `ｽﾀMENU.ADV:554-576` (debug picker, `Gﾜｵ`
   menu), title button 11 "Special" (its `Glob_MenuItem[11]` is never set) [I].
10. `Ev_PhaseNation := 11` in the Creator phase (`MAIN.ADV:843`) while the dictionary calls
    the Creator nation 10 (Angels) — check against `Tﾙﾄﾞﾗ`/`FIGT_B`.
11. One new game per process: `SET` runs only at boot, `OVER` exits the program; the CG
    gallery replays scene entry 300 before a new game may start [I].

## 8. Porting proposal

Already done: `M`/`WAO_MAP` state effects (`Battle.Map.cs`) and `WAO_SUB` (`Battle.Wao.cs`);
lift both out of `Combat` into a shared `Map`/`Ui` helper, since `MAIN` and `COMMAND` call them.

| Part | Script | Size (rule lines) | Contents | Depends on |
|---|---|---|---|---|
| **W1 Week loop** | `MAIN` | 1155 (~650 rules) | §2.1 in order: history shift (with `NB` quirk), autosave hook point, income/replenishment, expenses, phase gating and commander refresh loops, Camilla inline AI with its 8 draws and `FIGT_B` calls, end-of-week story checks | `FIGT_B` (ported), hooks below |
| **W2 Dispatcher** | `COM` | 947 (all rules) | upkeep + tier 1/tier 2 chains, 12 draws, exact `%0` / fall-through set. Mechanical; a generator (like `gen_figt_m.py`) could emit it from the script | `IStoryHooks` |
| **W3 Player phase** | `COMMAND` | 718 (~250 rules) | weekly flags, construction timer + building scenes, click → command routing, menu availability, Turn END (Tange Dog, prologue end). Input becomes `IStrategyInput.NextActionAsync()` returning *province n / dungeon d / menu item* | `ICommandFiles` (one method per `C*`), W2 |
| **W4 Game frame** | `ｽﾀｰﾄ`, `ｽﾀMENU` new-game paths, `ﾛｰﾄﾞｾｰﾌﾞ`, `OVER` state lines | ~400 rules | boot zeroing order, new game (`Week 8`, `Gold 50`, `Iﾌﾟﾛﾛｰｸﾞ`), debug chapter presets, save = snapshot at the autosave point (pages 0/1/2/4/10 + strings; page 3 excluded), persistent page 4 store, `EndingId`/fate/clear recording | `SET_*` port (other survey) |
| — | `HAPPY_C`, `CG`, `MMODE`, `ｵｰﾌﾟﾆﾝｸﾞ`, `ﾌﾟﾛﾛｰｸﾞ`, `XSPEC`, `PCMPLAY`, `ﾓｼﾞｷｬｯｼｭ`, `TEST` | ~11k | redesign in Godot; only `F_OpeningSeen := 1` and page-4 reads matter; `TEST` → own debug tools | — |

Order: W2 first (no dependencies, highest event density), then W1 (needs the `T*` turns as
hooks until ported), W3 (needs the `C*` files as hooks), W4 last together with the save format.

Interfaces: `IStoryHooks.Event(file, entry)` (exists); `INationTurns` (one call per `T*`
file, a hook until each `T*` is ported); `ICommandFiles` for `C*`, `各種DATA`, `Iｶﾝｶﾞｴ`;
`ISaveService` for `ﾛｰﾄﾞｾｰﾌﾞ` (autosave at the phase start; load = restore snapshot and resume
there); `IStrategyInput`; `ScenarioJumpException` for `&OVER` (exists).

Golden harness additions:

- **New capture roots** that are page calls already: `COM.ADV` (from `COMMAND.ADV:3`, all
  scene files as hooks) and each `T*` file (from `MAIN`). `COMMAND.ADV` as root with every
  `C*`, `ﾛｰﾄﾞｾｰﾌﾞ`, `各種DATA`, scene file as a hook: the replay needs no mouse log — the
  sequence of hook calls (file + address, with the hook-enter dump giving `Prov_Cur` /
  `Ui_ClickedProv` / `Dgn_Cur`) *is* the player's input.
- **Week capture** for `MAIN`: a label-triggered root (`MAIN.ADV L_00020`, address `0x20`)
  ending at the next arrival there, with `COMMAND`, `T*`, `FIGT_B`, `ﾛｰﾄﾞｾｰﾌﾞ` and scenes as
  hooks. Needs a harness change (roots are page calls today).
- `ﾛｰﾄﾞｾｰﾌﾞ` must be a hook (it writes save files); a **load** aborts the capture already
  (`golden_abort("load")`, `savedata.c`). Log `IM`-free: no polling input is needed if
  `COMMAND` is the root.
- The comparison must cover page 2 (history) and page 10 (markers, or ignore as ui) and the
  string table (string 10).

## 9. Open questions

1. Does `system39.exe` implement overlapping `NB` as a forward copy like xsystem35
   (history graphs flat) — `MAIN.ADV:8-11`?
2. Creator phase nation code 11 vs 10 — `MAIN.ADV:843`, `Tﾙﾄﾞﾗ.ADV`.
3. Can `F_Helman_War = 1` and `F_PattonArmy_State ≠ 0` hold together (double refresh)?
4. Identity of cmdrs 90-93 (Camilla, Satella?, Rei, Sieg) — confirm from `SET_CHR`.
5. Does anything rely on the cursor leftovers (`Unit_Cur = 51` after the status line,
   `Cmdr_Cur 98` after the La Vaswald gate) inside `C*`/`T*` files?
6. `SET_VER.ADV` is called from nowhere; `ｽﾀｰﾄ.ADV` contains the same resets — confirm it is
   dead and which one the port's new-game init follows.
7. Does viewing gallery entries (`CG`, entry 300) change story state before a new game?
