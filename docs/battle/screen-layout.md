# Battle screen layout of the original (640×400)

What the original battle draws, where and in which order, organised by the core's presentation
events (`src/Kichikuou.Original/Combat/BattleEvents*.cs`) and input points (`BattleInput.cs`). All
coordinates are 640×400 screen pixels (draw ×2 for 1280×800). Script locations are lines of the
decompiled scenario with readable names (`_extract/scenario_named/`); engine semantics cite the
xsystem35 source (`xsystem35-sdl2/src/…`). "Unknown" marks what could not be determined.

Contents: §0 engine conventions · §1 battlefield and slot panels · §2 message windows
(`WAO_SUB.ADV`) · §3 round header · §4 command/target selection (`FIGT_SEL.ADV`) and script
menus · §5 engagement scene and exchange animation · §6 info boxes, strategy screen, scout report,
map · §7 fonts and colours · §8 events without a visible counterpart, open questions.

---

## 0. Engine conventions used below

### 0.1 Surfaces

The game creates one 8-bit surface of 3200×800 and shows its (0,0)-(640,400) corner
(`WW 3200, 800, 8` / `WV 0, 0, 640, 400`, `ｽﾀｰﾄ.ADV:852-853`). The battle uses the rest of it as
scratch space; nothing outside (0,0)-(640,400) is ever visible directly.

| Region (x, y, w, h) | Battle use |
|---|---|
| 0, 0, 640, 400 | the screen |
| 0, 400, 640, 400 | "back buffer": the battlefield is composed here and copied up with `CC 0,400,640,400,0,0` (`FIGT.ADV:2055`); `FIGT_SEL.ADV:1` saves the screen here; the engagement frame with counters is staged here (`FIGT.ADV:1809-1827`) |
| 0, 500, 320, 220 | save slot of windows 141/142/143/151/152/153 (`WAO_SUB.ADV:469, 489, 500`) — overlaps the back buffer |
| 640, 0, 640, 400 | round-header screen save (`FIGT.ADV:112`); engagement scene composition (640,0)-(1280,656) (`FIGT.ADV:1628-1724`); `FIGTATTK.ADV` work frame (`:35`, `:134`); window saves of ops 111/112 (640,0,640,150), 113/121/122 (640,150,640,150), 131 (640,300,640,104); portrait-erase scratch (640,0,192,256) and (1088,144,192,256); target marker CG 814 at (640,0) (`M.ADV:169-170`) |
| 640, 400, 640, 400 | digit/label sheets: CG 893 at (640,400) (`FIGTATTK.ADV:2-3`), CG 888 at (640,512) (`FIGT.ADV:1807-1808`) |
| 1280, 0, 1920, 800 | the strategic map: six 640×400 tiles (`Ui_MapCgTopL/M/R` = 812/802/804 at y 0, `Ui_MapCgBotL/M/R` = 801/803/805 at y 400, `WAO_MAP.ADV:87-98`, `SET.ADV:333-338`) plus province markers |

### 0.2 Drawing commands

- `G n` / `G n, 40` draws CG n at the position stored in the CG file (PMS header x0/y0), or at a
  `J0 x, y` absolute / `J1 dx, dy` relative override that applies to the next `G` only
  (`cmdg.c:29-56`, `cmdj.c:30-44`, `cg.c:203-223, 386-449`). The second argument is the
  transparent ("sprite") palette index; the battle always uses 40. `G 0` draws nothing
  (`cmdg.c:31-35, 46-50`).
- Every `G` of an 8-bit PMS reloads palette entries 10-245 from the CG (all battle CGs set every
  bank bit; entries 0-9 and 246-255 are never loaded) (`cg.c:410-437`). See §7 for the values.
- `CC sx,sy,w,h,dx,dy` copies a rectangle; `CS …,sprite` copies skipping the sprite index;
  `CF x,y,w,h,c` fills with palette index c (`cmdc.c:32-59, 250-261`). `EC 0` fills the view
  area with index 0 (black) (`cmde.c:54-70`; xsystem35 fills 639×399 unless `patch_ec` is set,
  `cmde.c:58` — the original's exact size is unknown).
- `Y 3, n` is a pause of n × 16 ms (`cmdy.c:137`).

### 0.3 Text engine

- A message window is `B3 n, x, y, w, h, save` (text rectangle, `cmdb.c:95-114`) plus
  `B4 n, W, C1, C2, N, M` (select and open it, `cmdb.c:116-134`, `message.c:204-256`). The battle
  always opens with `B4 n, 0, 0, 0, 1, 0`: frame type 0 (none), no save (`save` = 0 in every
  battle `B3`), no clear; the cursor goes to (x, y + 2) (`message.c:165-202`, `LineIncrement` = 2
  at `message.c:73`). The visible frame is a CG drawn by the script.
- Text is drawn at the cursor in colour `ZC 1` (palette index) and size `ZM` (pixel height)
  (`cmdz.c:64-101`, `message.c:110-127`). The engine default is 16 (`message.c:63`); the
  scenario sets `ZM 24` at start (`ｽﾀｰﾄ.ADV:854`), which is the battle size.
- `R` = new line: x back to the window x, y += size + 2, using the size in force at the `R`
  (`message.c:148-163`). Text is never wrapped; a string longer than the window simply runs past
  its right edge.
- Overflow: when a line break leaves y + size > window bottom, the next printed text first waits
  for a key and clears the window to `ZC 6` (`message.c:110-114, 160-162, 165-176`). `ZC 6` = 18
  during battles (`FIGT.ADV:34`). No normal battle text reaches this (line capacities below).
- `ZH 1` (emitted before every string by the decompiler) makes every printed string half-width
  (`cmdz.c:122-129`, `nact.c:76-88`): full-width letters, digits, brackets and the full-width
  space become ASCII. Every character of battle text therefore occupies one half-width cell.
  The font is MS Gothic ([../assets.md](../assets.md) §6), whose half-width glyphs are fixed
  pitch at half the em, so a cell is size / 2 wide (12 px at 24, 8 px at 16). Whether the
  synthetic bold (§7.1) widens the advance in the original is **unverified**.
- `H w, v` prints v in decimal; w = 0: no padding; w > 0: right-aligned in w cells, space padded,
  and only the last w digits are kept when the number is wider (`cmdh.c:33-43`,
  `hankaku.c:385-404`). The battle uses `H 0` everywhere except the round header (`H 2`).
- `MP s, w` prints string variable s cut or padded (with spaces) to exactly w cells
  (`cmdm.c:51-91`); `X s` prints string s unpadded.
- `Y 1, 1` puts the cursor back to the window's top-left without clearing (`cmdy.c:92-99`). The
  battle "clears" a window by redrawing its frame CG over the text.
- `IK 0` waits for a key or click; `A` waits with a ▼ prompt (`message.c:284-295`).

### 0.4 Number sprites

Counters in the engagement scene are not text but 32×32 digit sprites copied from a sheet with
`CS … , 40` (`FIGT.ADV:3175-3217`, copy `FIGTATTK.ADV:353-395`): a 5-cell field (Tmp0021 = 5),
digit k at x + 32·k, leading zeros left blank (a digit is drawn once the running digit sum is
non-zero; the last digit is always drawn, so 0 shows as a single "0" in the rightmost cell).
Style (Tmp0022) selects the sheet row: style 0 at sheet y + 0, style 1 at sheet y + 64.

| Sheet | Placed at | Rows used | Labels |
|---|---|---|---|
| CG 888 (472×145, embedded (0,112)) | (640,512) via `J1 640, 400` (`FIGT.ADV:1807-1808`) | style 0: green digits, sheet (640+32d, 512) | "TROOPS" 100×32 at (990,512), "HP." 64×32 at (990,576) |
| CG 893 (640×400) | (640,400) via `J0 640, 400` (`FIGTATTK.ADV:2-3`) | style 0 green digits at (640+32d, 400); style 1 cream/orange digits at (640+32d, 464) | "TROOPS" at (990,400), "HP." at (990,464) |

CG 893 has four more digit rows (y 528-752, fading) that the battle never uses.

---

## 1. Battlefield and slot panels

### 1.1 RedrawAsync = `L_03058` (`FIGT.ADV:1987-1991`)

`L_0306c` (background into the back buffer) → `L_030f0` (every occupied panel into the back
buffer) → `L_031f9` (`CC 0, 400, 640, 400, 0, 0`, `FIGT.ADV:2054-2055`): the whole screen is
replaced in one copy. Core: `Battle.Display.cs:18-23` raises `BattlefieldBackgroundEvent`, then
one `SlotPanelEvent` per occupied slot; there is **no event for the final copy** — present the
composed frame after the last `SlotPanelEvent` of the redraw.

Visible result of a redraw: any highlight (§1.4), hover frame or window disappears.

### 1.2 BattlefieldBackgroundEvent(Cg) — `L_0306c` (`FIGT.ADV:1992-2009`)

| Case | Commands | Visible result |
|---|---|---|
| Dungeon (`Bt_Kind = 3`, Cg = 0) | `CF 0, 400, 640, 400, 0` and return (`:1993-1995`) | whole screen black (index 0) |
| Otherwise (Cg = `Prov_BgCg`) | `CC Tmp0021 + 1280, Tmp0022, 640, 400, 0, 400` with Tmp0021 = MapCamX − 320 (≤ 1280), Tmp0022 = MapCamY − 200 (≤ 400) (`:1997-2006`); `J0 176, 496` + `G Prov_BgCg` (no transparency) (`:2007-2008`) | the strategic map exactly as the map screen shows it at the current camera, with the province's picture (`Prov_BgCg`, the city-screen picture, e.g. CG 1001-1010, 288×192) opaque at (176, 96)-(464, 288) |

### 1.3 SlotPanelEvent(Panel, Record) — `L_03236` (`FIGT.ADV:2061-2136`)

Panel rectangles (table `L_0512e`, `FIGT.ADV:3268`; core `Battle.Flow.Player.cs:13-17`). The
second table `L_0516e` (`FIGT.ADV:3270`) is never selected (`#L_05126, 1` at `:601`, `:2074`).

| Panel | Side, slot | x, y, w, h | Frame CG 836 drawn at |
|---|---|---|---|
| 1 | player 1 | 12, 8, 182, 84 | (5, 1) |
| 2 | player 2 | 12, 108, 182, 84 | (5, 101) |
| 3 | player 3 | 12, 208, 182, 84 | (5, 201) |
| 4 | player 4 | 12, 308, 182, 84 | (5, 301) |
| 5 | enemy 1 | 448, 8, 182, 84 | (441, 1) |
| 6 | enemy 2 | 448, 108, 182, 84 | (441, 101) |
| 7 | enemy 3 | 448, 208, 182, 84 | (441, 201) |
| 8 | enemy 4 | 448, 308, 182, 84 | (441, 301) |

Drawing order for one panel (all into the back buffer, y + 400):

1. `B3 1, x, y + 400, w, h, 0` + `B4 1, 0, 0, 0, 1, 0` (`:2080-2081`): text window = panel rect.
2. `J0 x − 7, y − 7 + 400` + `G 836, 40` (`:2082-2083`): frame CG 836, 200×98 — dark grey
   interior (index 189, #5c5c5c), thin light double border, transparent corners. Visible at
   (x − 7, y − 7)-(x + 193, y + 91).
3. Text, colour = `ZC 1` in force (18 = #bec3dc in a full redraw, set at `:2011`; 240 when drawn
   for a highlight, §1.4), size 24, 12 px cells, 26 px lines:

| Line | Position | Player panel (`Unit_Cur`) | Enemy panel (`Cmdr_Cur`) |
|---|---|---|---|
| 1 | (x, y + 2) | `[` + string 1 (name, `Unit_StrBase + (Unit_Cur − 1)·3`) as `MP 1, 13` + `]` — always 15 cells = 180 px (`:2085-2091`) | `[` + string 4 (`Cmdr_StrBase + (Cmdr_Cur − 1)·3`) `MP 4, 13` + `]` (`:2117-2123`) |
| 2 | (x, y + 28) | only if `Unit_Troops ≠ 0`: space + string 2 (troop type name, `…·3 + 1`) `MP 2, 14` (`:2093-2096`) | only if `Cmdr_Troops ≠ 0`: space + string 5 `MP 5, 14` (`:2125-2128`) |
| 3 | (x, y + 54) | only if troops ≠ 0: space + `H 0, Unit_Troops` (`:2099-2100`) | space + `H 0, Cmdr_Troops` (`:2131-2132`) |

The panel shows **nothing else**: no portrait or sprite, no HP, no focus, order or status mark.
An empty slot is skipped (`:2012-2051`, `:2062-2073`) and the background shows through. After all
panels, `ZC 1, 22` (`:2052`).

### 1.4 PanelHighlightEvent(Panel) — `L_0320f` (`FIGT.ADV:2057-2059`)

The core raises `SlotPanelEvent` then `PanelHighlightEvent` (`Battle.Display.cs:115-119`). The
caller sets `ZC 1, 240` first, so the panel is redrawn in the back buffer with its text in colour
240 (#dcff91, light yellow-green); then only the panel area is copied to the screen:
`CC x − 7, y + 393, w + 14, h + 14, x − 7, y − 7` (a 196×98 rectangle). No extra frame or box is
drawn — **the highlight is the text colour change**. Callers reset `ZC 1, 22` afterwards.

| Caller | Panel | Line |
|---|---|---|
| player turn start (`L_00e0d`) | acting player slot | `FIGT.ADV:596-599` |
| enemy turn (`L_02112`) | acting enemy slot, then after `Y 3, 15` the chosen target player slot | `:1414-1417`, `:1445-1448` |
| `L_02528` (special-attack path) | acting enemy slot | `:1591-1594` |

There is no un-highlight: the panel stays highlighted until the next full redraw, so in the enemy
turn two panels are highlighted at once.

---

## 2. Message windows (`WAO_SUB.ADV`)

### 2.1 Mechanics common to all window ops

`Ui_WaoOp` selects the op (`WAO_SUB.ADV:1-164`). An "open" op saves the screen area under the
window with `CC` into off-screen memory, sets the text rectangle (`B3`/`B4`, §0.3), draws the frame
CG with `G frame, 40` at its embedded position, stores it in `Ui_WinFrameCg`, and sets
`Ui_WinType`. Text printed afterwards goes into that window. The "2xx" ops (211, 212, 213, 221,
222, 223, 231, 241, 242, 243, 251, 252, 253) only set `Ui_WinType` and draw nothing; no battle file
reaches them (they are dead entries after `\0:` in the wrapper tables).

Core event: `WindowEvent(Op, CallArg)` for every `WAO_SUB.ADV` call (flow, resolution, death
scenes and `FIGT_M.ADV` alike, raised by `ScriptContext.WaoAsync` before the op's state effects),
followed by `MessageEvent` / `ValueMessageEvent` / `PrintStringEvent` for the text.

### 2.2 Window ops

| Op | Script | Save (CC src → dst) | Text rect (B3) | Frame CG (embedded pos, size, look) | Colour / size set | WinType |
|---|---|---|---|---|---|---|
| 111 | `L_00408` `:170-178`, `L_00454` `:188-192` | 0,0,640,150 → 640,0 | win 7: 235, 8, 378, 141 | 807 (200,3) 408×122, white speech box, tail pointing left (to a left portrait) | `ZC 1, 15`; `ZM 24` | 11 |
| 112 | `L_0042e` `:179-187` | same | same | 808, same place, dark (#1e1e1e) box, tail left | `ZC 1, 15`; `ZM 24` | 12 |
| 113 (= 123) | `L_0059e` `:239-247`, `L_005c4` `:248-252` | 0,250,640,150 → 640,150 | win 10: 140, 278, 376, 140 | 809 (128,271) 384×126, cream box with brown border | `ZC 1, 15`; `ZM 24` | 43 |
| 121 | `L_00489` `:193-201`, `L_004d5` `:211-215` | 0,250,640,150 → 640,150 | win 8: 43, 265, 378, 141 | 810 (32,259) 408×122, white speech box, tail pointing right (to a right portrait) | `ZC 1, 15`; `ZM 24` | 21 |
| 122 | `L_004af` `:202-210` | same | same | 811, same place, dark box, tail right | `ZC 1, 15`; `ZM 24` | 22 |
| 131 | `L_0050c` `:216-233`, `L_00567` `:234-238` | 0,296,640,104 → 640,300 | win 9: 8, 305, 626, 140 | 815 (0,294) 640×106 grey bar, drawn opaque, loads no palette (empty bank mask) | `ZC 1, 190` (#505050), `ZC 6, 178` (#e0e0e0), `ZM 24`; clears `Ui_PortraitL/R`, `Ui_CenterCg/Overlay1-4` | 31 |
| 141 | `L_00b78` `:453-459`, `L_00bb6` `:468-472` | 160,180,320,220 → 0,500 | win 10: 200, 196, 240, 200 | 895 (160,194) 264×202, white bubble, tail on the left | none (inherits) | 41 |
| 142 | `L_00b94` `:460-467` | same | same | 896, same place, dark bubble, tail left | `ZC 1, 11` (#d7ebd7) | 42 |
| 143 (= 153) | `L_00c65` `:493-499` | same (`L_00bb6`) | win 10: 200, 196, 240, 200 | 894 (192,194) 256×202, cream box, thin brown border | none (inherits) | 43 |
| 151 | `L_00bee` `:473-479`, `L_00c2d` `:488-492` | 160,180,320,220 → 0,500 | win 10: 224, 196, 240, 200 | 897 (216,194) 264×202, white bubble, tail on the right | none (inherits) | 51 |
| 152 | `L_00c0a` `:480-487` | same | same | 898, same place, dark bubble, tail right | `ZC 1, 11` | 52 |

Line capacity at size 24 (first line y = rect y + 2, pitch 26, cells = width / 12):

| Text rect | Line y positions | Cells | Note |
|---|---|---|---|
| win 7 (111/112) | 10, 36, 62, 88, 114 | 31 | the 5th line runs below the frame CG (bottom 125) |
| win 8 (121/122) | 267, 293, 319, 345, 371 | 31 | frame bottom 381 |
| win 10 (113) | 280, 306, 332, 358, 384 | 31 | the 5th line is cut by the screen edge |
| win 10 (141-153) | 198, 224, 250, 276, 302, 328, 354 | 20 | 7 lines; an 8th line triggers the overflow wait (§0.3) |
| win 9 (131) | 307, 333, 359, 385 | 52 | |

### 2.3 Close, wait and clear ops

| Op | Script | Effect |
|---|---|---|
| 10 | `L_00cb9` `:504-509` | `CC 0, 500, 320, 220, 160, 180` (restore under a 14x/15x window), `ZC 1, 15`, WinType 0 |
| 11 | `L_00ce1` `:510-519` | `IK 0` (wait), `Y 1, 1`, `G Ui_WinFrameCg, 40` — the text is erased by redrawing the frame; the window stays open |
| 0 | `L_005fc` `:253-277` | close by WinType: 11-19 `CC 640,0,640,150 → 0,0` + `ZC 1, 15`; 21-29 `CC 640,150,640,150 → 0,250` + `ZC 1, 15`; 31-39 `CC 0,0,640,400 → 0,0` (a copy onto itself: **the window stays visible**) + `ZC 1, 15` + `ZC 6, 0`; 41-49 `CC 640,150,640,150 → 0,250` (no ZC). Used for windows 111/112/113/121/122 (FIGT_A/FIGT_B) |
| 1 | `L_006fe` `:286-306` | `IK 0` (RetVal preserved by `US`/`UG`), then op 2 |
| 2 | `L_0072b` `:295-306` | `Y 1, 1`; WinType 31-39: `G Ui_WinFrameCg` (opaque) first; then `G Ui_WinFrameCg, 40` (clear page) |
| 3 / 4 | `:307-312` | `Ui_FaceOverMap` := 1 / 0 (not used by the battle files) |
| 5 | `L_006d8` `:278-285` | `IK 0`, then op 2 (not used) |

Op 10 after a 113 window, or op 0 after a 14x/15x window, would restore the wrong area; the battle
never does either (wrapper tables below).

### 2.4 Portrait and centre-picture ops

| Op | Script | Effect |
|---|---|---|
| 311 | `L_00787` `:313-320` | if a left portrait is up, erase it (312) first; `J0 0, 0` + `G CallArg, 40`: face CG (192×256) at (0,0)-(192,256); `Ui_PortraitL` := CallArg |
| 312 | `L_007b1` `:321-351` | erase left portrait: `L_00b0f` (`:437-452`) clamps the camera into Tmp0021 ∈ [320,1600], Tmp0022 ∈ [200,600]; scratch (640,0,192,256) is filled black (`Ui_FaceOverMap = 0`) or copied from the map (`= 1`); if a centre picture is up it is redrawn into the scratch (`J0 816, 96`), its overlays at `J0 176, 96` (screen coordinates — script quirk); `CC 640,0,192,256 → 0,0`. Visible: (0,0)-(192,256) becomes black or the map |
| 321 | `L_008b1` `:352-359` | `J0 448, 144` + `G CallArg, 40`: face at (448,144)-(640,400); `Ui_PortraitR` := CallArg |
| 322 | `L_008dd` `:360-390` | erase right portrait, as 312 with scratch (1088,144) → (448,144) |
| 331 | `L_009e7` `:391-402` | `J0 176, 96` + `G CallArg` (opaque): a 288×192 picture at (176,96)-(464,288) (the battlefield's province-picture spot); `FIGT_B.ADV:784-785` passes `Prov_BgCg` |
| 332 | `L_00a9b` `:423-436` | erase centre picture: `Ui_FaceOverMap = 0`: `CF 288, 192, 176, 96, 0` — fills only (288,192)-(464,288) black (arguments appear swapped against the picture rectangle 176,96,288,192 — script quirk, the rest of the picture stays); `= 1`: `CC` from the map → (176,96) 288×192 |
| 333 | `L_00a25` `:403-422` | centre picture + overlays Tmp0001-0004 (not used by the battle) |

`Ui_FaceOverMap` during a battle: whatever the map screens left (ops 3/4 run only outside the
battle files, so the battle never changes it). At battle entry it is 1 in 27 of the 37 golden
captures and 0 in 10 (`VAR0209` in each capture's `mem enter`); the presenter reads it from the
state, so both cases are exact.

### 2.5 Which file uses which op (live wrapper labels)

| File | Ops (wrapper label, call count) |
|---|---|
| `FIGT.ADV` | 141 (`L_05064` ×2, error box), 143 (`L_0509c` ×21), 10 (`L_0510c` ×24), 11 (`L_05119` ×29) |
| `FIGT_M.ADV` | 141 (`L_0c4c1` ×361), 142 (`L_0c4dd` ×30), 151 (`L_0c515` ×453), 152 (`L_0c531` ×10), 10, 11 |
| `FIGT作.ADV` | 141, 142, 143, 151, 152, 10, 11 (`L_00999`…`L_00a4e`, `:490-538`) |
| `FIGTﾐｶDE.ADV` | 141, 142, 143, 10, 11 |
| `FIGTﾃｷDE.ADV` | 141, 143, 151, 152, 153, 10, 11 |
| `FIGT_A.ADV` | 111 (`L_00380`), 113 (`L_0058a`), 0 (`L_0066a`), 1 (`L_00677`) |
| `FIGT_B.ADV` | 111, 112, 113, 121, 122, 311, 312, 321, 322, 331, 332, 0, 1 (`L_012dd`…`L_01402`, `:902-983`) |

Ops 2 (only as the tail of op 1), 3, 4, 5, 123, 131, 333 and all 2xx ops are not used.

### 2.6 Text colour inside windows

Ops 141/143/151/153 do not set a colour; they print in the last `ZC 1` written. In the battle:

| Last write | Value | Where |
|---|---|---|
| battlefield redraw | 22 (#6e7382) | `FIGT.ADV:2052` |
| after a panel highlight | 22 | `FIGT.ADV:599, 1417, 1448, 1594` |
| after info boxes / order messages | 22 | `FIGT.ADV:827, 1013, 1112` |
| any window close (op 0 / op 10) | 15 (#879187) | `WAO_SUB.ADV:261, 265, 269, 506` |
| battle setup | 22 | `FIGT.ADV:35` |
| battle end | 15 | `FIGT.ADV:593` |

So a 143 message right after a redraw is colour 22, a second message before the next redraw is 15;
the engagement's action-label window is 22, the flavour windows 141/151 that follow it are 15
(op 10 at `FIGT.ADV:1802` precedes `FIGT_M.ADV`), 142/152 are 11. Op 11 does not change colour.
The core's text events do not carry the colour; a faithful UI has to track `ZC 1` as above.

### 2.7 Messages and numbers

- `MessageEvent(ScriptRef)` / `ValueMessageEvent`: the script string (half-width, §0.3)
  printed at the window cursor; `R` = next line. Numbers are `H 0` (plain decimal).
- `PrintStringEvent(Slot)`: `MP slot, width` — widths in the battle files: `MP 1, 8`
  (`FIGTﾐｶDE.ADV`), `MP 4, 18` (`FIGTﾃｷDE.ADV`), `MP 4, 20` / `MP 5, 20` (`FIGT_B.ADV:656, 663`),
  `MP 1, 12` (`FIGT.ADV:3065`), `MP 10, 10` (`FIGT.ADV:3008`).
- `FlowErrorEvent`: `FIGT.ADV:770-774` opens window 141 with a Japanese error line;
  `FIGT.ADV:1257-1263` uses `B4 1, 1, 0, 0, 0, 0` (message window 1, line frame, clear) whose
  rectangle is still the last panel drawn in the back buffer (y ≥ 400) — invisible on screen,
  only the `A` key wait is felt. Both are script-error paths.

---

## 3. Round header — RoundHeaderEvent (`FIGT.ADV:112-216`)

Order:

1. `CC 0, 0, 640, 400, 640, 0` — save the screen (`:112`).
2. `B3 2, 138, 75, 360, 260, 0` + `B4 2, 0, 0, 0, 1, 0` (`:113-114`) — text from (138, 77).
3. `G 887, 40` (`:115`) — CG 887 at its embedded (128, 64), 384×288: pink textured box with a
   red/white border, transparent outer edge.
4. Text, colour = current `ZC 1` (22 in practice: every path into `L_00295` passes a redraw that
   ends with `ZC 1, 22`), size 24, 12 px cells, 26 px lines (x = 138 + 12·cell):

| Line (y) | Content | Script |
|---|---|---|
| 1 (77) | duel: ` [  Duel  ]`; offence: ` [` + `MP 8, 10` (province name, string 8) + ` Offensive Battle]`; defence: ` [` + `MP 8, 10` + ` Defensive Battle]`; dungeon: ` [Dungeon Battle]` | `:116-142` |
| 2 (103) | ` <Plains: Battle Limit>` (limit 5000, not duel) / ` <Duel: Battle Limit>` / ` <City: Battle Limit>` (limit ≠ 5000, not dungeon) / ` <Dungeon: Battle Limit>` | `:145-160` |
| 3 (129) | 6 spaces + `Up to ` + `H 0, Bt_TroopLimit` | `:163-164` |
| 4 (155) | empty | `:166-167` |
| 5 (181) | `MP 7, 11` (string 7 := ` Rance Army`, `:168`) + 4 spaces + `MP 10, 15` (enemy army name, string 10) | `:168-172` |
| 6 (207) | `vs` at cell 12 (x 282) | `:175` |
| 7 (233) | `Advantage` at cell 1 (x 150) and at cell 19 (x 366) | `:177` |
| 8 (259) | `+` at cell 1, `H 2, Tmp0001` (player advantage) in cells 2-3; `+` at cell 21, `H 2, Tmp0002` (enemy) in cells 22-23 | `:200-204` |
| 9 (285) | not duel: `  (Turns Remaining: ` + `H 0, Bt_RoundsLeft` + `)` | `:207-213` |

   `H 2` shows only the last two digits of an advantage ≥ 100 (§0.3). The advantages are
   `Bt_WallBonus` + 10/50 from the strategy result (`:179-198`); the core carries them in the
   event.
5. `IK 0` (`:214`) — `IBattleInput.WaitForKeyAsync`.
6. `CC 640, 0, 640, 400, 0, 0` (`:215`) — restore, then `L_03058` redraw (`:216`).

---

## 4. Command and target selection (`FIGT_SEL.ADV`) — SelectCommandAsync

The whole of `FIGT_SEL.ADV` is one input point (`IBattleInput.SelectCommandAsync`,
`BattleInput.cs`); the core raises no draw events inside it, so the UI draws all of this itself.
On entry the acting player panel is already highlighted (§1.4) and `ZC 1` = 22.

### 4.1 Entry and prompt box

1. `CC 0, 0, 640, 400, 0, 400` (`:1`) — the current screen (battlefield + highlight) is copied to
   the back buffer; it is the "clean" image used to erase hover frames and on exit.
2. `B3 2, 216, 193, 204, 86, 0` + `B4 2, 0, 0, 0, 1, 0` (`:2-3`); `G 861, 40` (`:4`): CG 861 at
   (208,186) 224×100, pink box like the round header's.
3. `L_001bb` (`:64-159`, re-entered on every mode change): `G 861, 40`, `Y 1, 1`, then text at
   (216, 195), size 24, colour 22, lines at y 195, 221, 247:
   - melee unit in a rear slot (`Bt_PlayerAtkType = 0`, slot 3-4): `What will you do?` /
     `(Right-Click)` (`:67-72`);
   - otherwise: `Attack who?` / mode label / `(Click Target)` (`:74-159`). Mode label by
     `Tmp0019`: 1 `[Direct Attack]`, 2 `[Bow Attack]`, 3 `[Magic Attack]`; ≥ 10: `[` in colour
     22, the special's name in colour 249 (red #ff0000) — 10 Cheater Beam, 11 Rance Attack,
     12 By la Way, 13 White D. Beam, 14 High Speed, 15 Vanish Bomb, 16 C. War Dance, 17 Black D.
     Beam, 18 Whirlwind Shot, 19 Chuhai, 20 T2 Soaring Wave, 21 EPO Light, 30/31 Capsule Kaiju,
     32 Fog Card — then `]` in colour 22 (`:89-156`).

Initial mode `Tmp0019` = attack type + 1, frame colour `Tmp0020` = 255 (`:61-63`).

### 4.2 Targeting loop and hover frames (`L_003e6`, `:160-203`)

The loop polls the mouse (`IM`) without waiting. Hit test `L_00b16` (`:498-520`): buttons 0-7 are
the eight panel rectangles of §1.3 (loaded at `FIGT.ADV:601-613`), tested with strict
inequalities (x1 < mx < x2, y1 < my < y2). Player buttons 1-3 are skipped beyond the highest
occupied player slot (`Bt_PlayerSlotsUsed`); enemy buttons stop at 3 + `Bt_EnemySlotsUsed`. No hit
→ 99.

Frame colour for an enemy button (`Tmp0010`, `:163-171`): `Tmp0020` (255 white; 249 red in
special modes 10-21 and Cheat), but 15 (#879187, "invalid") when the hovered button is enemy slot
3-4 (button > 5) or the actor stands in slot 3-4, and the mode is melee (1, 4-9) or a special > 10
of a melee unit. Mode 10 (Cheat) and bow/magic modes are never invalid.

When the hovered button changes (`:172-191`):

1. the previous button's rectangle is restored from the back buffer
   (`CC x1, y1 + 400, w, h, x1, y1`) unless it was 98 (the "fresh" marker set on entry and on every
   mode change);
2. the new button gets a 4-px frame drawn **inside** its panel rectangle with four `CF`s:
   (x1, y1, w, 4), (x1, y2 − 4, w, 4), (x1, y1, 4, h), (x2 − 4, y1, 4, h) — colour 18 (#bec3dc) for
   own panels (0-3), `Tmp0010` for enemy panels.

For "no button" (99) the same code runs with the rectangle stored at index 99 of the button
arrays (page 3 at 1099 / 2099 / 3099 / 4099 / 5099 / 6099, bound in `SET.ADV:276-281`). It is 0
in every golden capture and the script never loads it, so every `CC`/`CF` has a zero width or
height: no visible effect. Because a mode change
resets the previous-button marker to 98, a frame drawn before a popup is not erased afterwards
(script quirk).

### 4.3 Clicks → results (`FigtSelMenu`, `FigtSelChoice`)

`IM` reports the buttons held: 16 = left, 32 = right. Actions trigger on the press.

| Input | Condition | Result (RetVal, Tmp0001) |
|---|---|---|
| left press on own panel k | button valid | (53, k) unit info (`:192-195`, `L_0098a`) |
| left press on enemy panel j (button 3 + j) | frame colour ≠ 15 | (`Tmp0019`, 3 + j): mode 1/2/3 or 10-21 (`:196-203`, `L_00981`) |
| left press on an enemy panel with a grey (15) frame, or elsewhere | — | ignored |
| right press on own panel (mouse x < 199) | — | slot popup (§4.4) |
| right press anywhere else | — | command popup (§4.5) |

On every exit `L_00af4` (`:494-497`): `CC 0, 400, 640, 400, 0, 0` (screen back to the entry image:
prompt box and hover frames vanish) and `B2 1, 1, 0, 0, 0, 0` (selection window 1 for later
script menus).

### 4.4 Slot popup (`L_0076a`, `:295-354`)

- Position: (mouseX − 16, mouseY − 10) of the right press, clamped x ∈ [10, 564], y ∈ [10, 310]
  (`:297-313`); `B1 2, x, y, 68, 81, 1` + `B2 2, 1, 0, 0, 0, 0` (`:314-315`). It opens when all
  buttons are released (`:316-320`).
- Box drawn by the engine (`selection.c:267-310`): (x, y, 4 + 68, 2 + 18·4) = 72×74 filled with
  colour 18 (`ZC 4`), an 8-px line frame outside it — from the outside in: 3 px colour 255
  (`ZC 3`), 2 px colour 18, 1 px colour 255, 2 px colour 18 (`selection.c:401-410`); items at
  (x + 2, y + 2 + 18·i), size 16 (`ZS 16`, `ｽﾀｰﾄ.ADV:855`), colour 22 (`ZC 2`).
- Hover: a 2-px white (255) rectangle (x, y + 18·i, 74, 20) around the item; the area under it is
  saved and restored (`selection.c:370-399`, `EncloseType` 0). Choice = left release on an item.
- Items: `Manual` → (50, slot), `Auto-Atk` → (51, slot), `Auto-Def` → (52, slot) — each first
  restores the hovered panel's frame area (`:356-370`); `Info` → (53, slot) (`:371-373`).
  slot = hovered button + 1.
- Cancel = right click (`selection.c:451-455`): the box is removed; then a right press returns to
  targeting after restoring the panel's frame area (`:347-353`), any other press reopens the popup
  (`:339-346`).

### 4.5 Command popup (`:204-294`, items `L_00bc0` `:521-698`)

- Position (mouseX − 16, mouseY − 10), clamped x ∈ [10, 632 − W], y ∈ [10, 290 − Tmp0015]
  (`:208-233`); `B1 2, x, y, W, Tmp0013, 1` + `B2 2, 1, …` (`:234-235`); opens on release
  (`:236-240`). Box = (x, y, 4 + W, 2 + 18·n) (height follows the item count;
  `WinResizeHeight` is never changed), same frame/colours/font/hover as §4.4.
- W = 68 (`:211`), or 16 × k of the **last** mode/special item added (64 for Direct/Bow/Magic/Fog
  Card/W. Shot, 80 Chuhai/EPO Light/T2S Wave, 96 Capsule/High Speed/WD Beam/BD Beam/CW Dance,
  112 Rance Attack/By la Way/Vanish Bomb). Tmp0015 = 20 per mode/special item + 40 (debug) + 20
  (Info); used only by the clamp.
- Items in order:
  1. mode items (`L_00bc0`): `Direct` (mode ≠ 1); `Bow` (bow unit, mode ≠ 2); `Magic` (magic unit,
     mode ≠ 3); `Capsule` (C. Kaiju B, item 27 → 30); `Capsule` (C. Kaiju A, item 26 → 31); `Fog Card` (not duel, item 28
     → 32); then, only when mode ≤ 3 and `Bt_ActFocus > 0` (`:576-578`): `Rance Attack` (slot 1-2,
     Rance (unit 1) or Kentarou (unit 31) with `F_Kentarou_State > 7`) 11, `By la Way` (slot 1-2,
     Rick (unit 7)) 12, `High Speed` (Meglass (unit 30)) 14, `WD Beam` (Shizuka (unit 17) or Magic
     (39)) 13, `BD Beam` (Chizuko (unit 38) or Nagi (40)) 17, `Vanish Bomb` (Miki (unit 32), focus
     > 2) 15, `W. Shot` (Isoroku (unit 27)) 18, `CW Dance` (Patton (unit 18)) 16, `Chuhai` (Asuka
     (unit 11), `F_Asuka_State > 7`) 19, `EPO Light` (Gandhi (unit 37), magic) 21, `T2S Wave`
     (Rolex (unit 47), melee) 20;
  2. `Cheat` (debug mode only) → mode 10 (`:241-246`);
  3. `Defend` → (4, ·), `Wait` → (5, ·), `Auto-Atk` → (8, ·), `Auto-Def` → (9, ·) (`:247-262`);
  4. `Info` (only if the last hovered button ≠ 99) → (54, last hovered button − 3) (`:263-268`,
     `L_0093e`);
  5. `Retreat` → (6, ·) (`:269-272`); `Repel` (debug) → (7, ·) (`:273-278`).

  "·" = Tmp0001 as left by the hit test at the right press (enemy button 4-7, or 99).
- Choosing a mode or special (Direct/Bow/Magic/specials/Cheat) does not return: it sets
  `Tmp0019`, the frame colour (255 for 1-3, 249 for ≥ 10) and the 98 marker and redraws the prompt
  (`L_0099e`-`L_00aa8`, `:404-478` → `L_001bb`). Capsule/Capsule/Fog Card return at once (30, 31,
  32) (`:479-493`).
- Cancel (right click): a right press returns to targeting (`:289-294`); any other press reopens a
  popup containing only items 2-5 (`L_0065b`, `:236-279`; the mode items are not re-added and W
  keeps its value) — script quirk.

### 4.6 Script menus — ChooseAsync

`$label$ 'item' $ … ]` menus use selection window 1: `B1 1, 250, 50, 150, 100, 1` +
`B2 1, 1, …` (`ｽﾀｰﾄ.ADV:860-861`, reselected by `FIGT_SEL.ADV:496`). Box (250, 50, 154, 2 + 18·n)
with the frame, colours, font and hover of §4.4; item i at (252, 52 + 18·i). Choosing does not
clear the message window (`ZE 0`, `ｽﾀｰﾄ.ADV:857`, `cmdz.c:159-166`). Right click cancels and the
script continues after `]`.

| Menu | Items | Script |
|---|---|---|
| RetreatConfirm | `Retreat`, `Cease Retreat` (cancel → back to the turn) | `FIGT.ADV:571-580` |
| EnemyAttackResponse | `Intercept`, `Ninja Scout`, `Abandon City` (cancel → menu again) | `FIGT_B.ADV:46-64` |
| NinjaScout | the available ninja, `None` | `FIGT_B.ADV:345-380` |

---

## 5. Engagement scene and exchange

### 5.1 EngagementSceneEvent — `L_02609` (`FIGT.ADV:1624-1725`)

Dungeon battles first blank the screen with `EC 0` (`:1625-1627`; the core raises no event for
it, and step 8 below overwrites the whole screen right after, so at most a black flash is seen).
Then a 640×512 scene S is composed off-screen at (640, 0) (S coordinates below = off-screen
x − 640, y):

| Order | Command | Placed in S | Notes |
|---|---|---|---|
| 1 | `J1 640, 256` + `G 899` (`:1628-1629`) | (0, 256), 640×400 | dark blue-grey texture behind the faces |
| 2 | `J1 640, 0` + `G Bt_BackgroundCg` (`:1630-1631`) | (0, 0), 640×256 (900-905: 640×257) | battlefield picture: 900 plains, 902 city, 904/906/908/910/912/914/918 special (variables.tsv) |
| 3 | `J1 640, 0` + `G Cmdr_CgFull / CgPartial / CgAlone, 40` (`:1632-1643`) | embedded x (320 for enemy sprites), y 0; 320×256 | by `Cmdr_Troops` ≥ limit / 0 < troops < limit / 0 |
| 4 | player sprite, same rule with `Unit_Cg*` (`:1644-1657`), or with a special the special CG(s) (`:1658-1714`) | embedded x (0), y 0 | special CGs by `Unit_Cur`: Rance (1)→1520, Rick (7)→1521, Asuka (11)→1121, Shizuka (17)→1522, Magic (39)→1523, Gandhi (37)→1524, Nagi (40)→1525, Chizuko (38)→1526, Meglass (30)→1527, Isoroku (27)→1528, Kentarou (31)→1222, Miki (32)→1529, Patton (18)→1530, Rolex (47)→1252 (640×256, or 320×256 for 1121/1522/1523/1526/1528); plus 1164/1176/1182/1185 when `Unit_CgFull` equals it (ninja). Every match is drawn |
| 5 | `J1 640, 0` + `G Bt_BackgroundCg + 1, 40` if `Bt_EnemyParty1 ≠ 1137` (`:1715-1718`) | (0, 0) | foreground overlay (rocks etc.) over the sprites |
| 6 | `J0 640, 256` + `G Unit_FaceCg, 40` (`:1719-1720`) | (0, 256), 192×256 | player face; `G 0` (Goliath) draws nothing |
| 7 | `J0 1088, 256` + `G Cmdr_FaceCg, 40` if ≠ 0 (`:1721-1724`) | (448, 256), 192×256 | enemy face |
| 8 | `CC 640, 112, 640, 400, 0, 0` (`:1725`) | — | **screen = S rows 112-511**: battle picture rows 112-255 at screen y 0-143, faces fully visible at y 144-399, no counters |

The core's event lists the CGs in this order (`Battle.Resolution.cs:370-411`).

### 5.2 ActionLabelsEvent (`FIGT.ADV:1739-1802`)

`WindowEvent(143)` (CG 894 box, text rect 200,196,240,200, colour 22 in practice) →
`ActionLabelsEvent` → `WindowEvent(11)` + key wait (text erased) → `WindowEvent(10)` (box
removed) (`Battle.Resolution.cs:341-344`). The key wait is `IK 0`, which takes a button already
held: after the player clicks a target (§4.3: on the press) the window shows for one frame, after
the enemy's choice it waits for a click (BSC-07). Text, size 24:

| y | Text |
|---|---|
| 198 | `(` + `MP 1, 16` (player unit name) + `)` |
| 224 | label: `   Special Attack` (special) / `   Direct Attack` / `  Indirect Attack` / `  Magic Attack` / `   Defense` (`:1746-1767`) |
| 250 | empty |
| 276 | `    vs` |
| 302 | empty |
| 328 | `(` + `MP 4, 16` (enemy commander name) + `)` |
| 354 | enemy label, same strings; three spaces for a special (`:1779-1800`) |

### 5.3 EngagementSlideEvent + CountersEvent (`FIGT.ADV:1807-1828`)

`G 888` is placed at (640,512) (`:1807-1808`). Each step k with Offset ∈ {112, 84, 56, 28, 0}:
`CC 640, Offset, 640, 400, 0, 400` (S rows Offset … Offset + 399 into the back buffer),
`L_02c00` counters into the back buffer, `CC 0, 400, 640, 400, 0, 0` (show). **Visible: S drawn at
screen y = −Offset, plus the counters.** The flavour text (`FIGT_M.ADV`, §5.4) runs between the
112 and 84 steps; the 84/56/28/0 steps follow each other with no pause (no `Y 3`), so the picture
scrolls down by 112 px in four 28-px jumps until the battle picture is fully visible (rows 0-255
at y 0-255) and the faces show only their top 144 rows. Finally `CC 640, 0, 640, 400, 0, 400`
(`:1828`) leaves the clean final frame (without counters) in the back buffer.

CountersEvent — `L_02c00` (`:1830-1863`), sprites from CG 888, style 0:

| Item | Condition | Label (CS) at | Number field (5 cells × 32) |
|---|---|---|---|
| player troops | `Unit_Troops ≠ 0` | "TROOPS" 100×32 at (0, 0) | x 100-259, y 0 |
| enemy troops | `Cmdr_Troops ≠ 0` | "TROOPS" at (392, 0) | x 480-639, y 0 |
| player HP | always | "HP." 64×32 at (0, 368) | x 0-159, y 368 (a 3-digit HP lands in x 64-159, right of the label) |
| enemy HP | always | "HP." at (480, 368) | x 480-639, y 368 |

In story stage 4 the 112 step shows commander 200's numbers (`FIGT.ADV:1803-1810`, core quirk
comment `Battle.Resolution.cs:353-355`).

### 5.4 Flavour text — WindowEvent 141/142/151/152, 11, 10

`FIGT_M.ADV` speaks in windows 141 (CG 895, tail left, text at 200,198), 142 (CG 896 dark,
colour 11), 151 (CG 897, tail right, text at 224,198), 152 (CG 898 dark, colour 11), each followed
by op 11 (key wait) and op 10 (restore). 141/151
print in colour 15 (§2.6). The windows sit over the engagement frame of step 112.

### 5.5 Exchange animation — `FIGTATTK.ADV`

1. `G 893` at (640,400) (`:2-3`).
2. **CountersEvent** (`:4-31`): the four numbers (no labels) are drawn again directly on the screen
   at the §5.3 positions, style 0 — visually identical to what is already shown.
3. `SoundPairEvent` (first sound pair, `:338-344`).
4. **ShotsFrameEvent(Step)**, Step t = 0, 40, …, 360 (`:33-131`), each followed by `Y 3, 1`
   (16 ms): the clean final frame (back buffer) is copied to the work area (`:35`), the non-zero
   shot counters are drawn there in style 1 (cream/orange digits), and only 200×64 rectangles
   around them are copied to the screen. With d = t·5/11 (integer):

| Counter (value = `Bt_*Shots`) | Number at (screen) | Refreshed rectangle |
|---|---|---|
| PTroopVsTroop | (48 + t, 80) | (8 + t, 80, 200, 64) |
| PTroopVsCmdr | (48 + t, 100 + d) | (8 + t, 80 + d, 200, 64) |
| PCmdrVsTroop | (48 + t, 280 − d) | (8 + t, 280 − d, 200, 64) |
| PCmdrVsCmdr | (48 + t, 300) | (8 + t, 300, 200, 64) |
| ETroopVsTroop | (416 − t, 80) | (416 − t, 80, 200, 64) |
| ETroopVsCmdr | (416 − t, 100 + d) | (416 − t, 80 + d, 200, 64) |
| ECmdrVsTroop | (416 − t, 280 − d) | (416 − t, 280 − d, 200, 64) |
| ECmdrVsCmdr | (416 − t, 300) | (416 − t, 300, 200, 64) |

   Player numbers move right, enemy numbers move left (they cross); "vs troop" rows converge and
   "vs commander" rows diverge vertically. The refreshed rectangles erase the previous position;
   everything else on screen (picture, troop/HP counters) stays. Final t = 360, d = 163.
5. `Y 3, 40` (640 ms) (`:133`); second sound pair `SoundPairEvent` unless both are 5 (`:346-351`).
6. **ExchangeResultsEvent** (`:134-233`): a complete frame is built in the work area and shown at
   once (`CC 640, 0, 640, 400, 0, 0`, `:233`): clean final scene; "TROOPS" labels and predicted
   troops (current − both troop damages, saturating at 0) at the §5.3 positions when the current
   troops ≠ 0; "HP." labels and predicted HP; for every non-zero shot counter its damage
   (`Bt_*Dmg`) in style 1 at the final position: player row numbers at x 408, enemy at x 56,
   y = 80 / 263 / 117 / 300 for TroopVsTroop / TroopVsCmdr / CmdrVsTroop / CmdrVsCmdr.
7. `Y 4, 2` (a discarded random draw, no display) (`:234`).
8. **WipedOutMarksEvent** (`:235-246`), drawn on the screen with key 40 at the CGs' embedded
   positions, for a value that was non-zero and is predicted to be 0:

| Flag | CG | Rect | Look |
|---|---|---|---|
| PlayerTroops | 889 | (0, 0, 320, 256) | "ANNIHILATED" in red, centre of the top-left quarter |
| EnemyTroops | 890 | (320, 0, 320, 256) | same, top-right |
| PlayerCmdr | 891 | (0, 256, 320, 144) | "UNABLE TO FIGHT", lower left |
| EnemyCmdr | 892 | (320, 256, 320, 144) | "UNABLE TO FIGHT", lower right |

9. **KeyWaitEvent** + `IK 0` (`:247`).
10. After the key (no core event): the commander marks are removed — `CC 640, 300, 320, 100, 0, 300`
    if PlayerCmdr, `CC 960, 300, 320, 100, 320, 300` if EnemyCmdr (`:248-253`), restoring the
    results frame. The troop marks stay until the next redraw or window.

If a commander dies, the death scene (`FIGTﾐｶDE.ADV` / `FIGTﾃｷDE.ADV`) opens its windows over this
results screen, then `L_03058` redraws and `Y 3, 10` pauses (`FIGT.ADV:1893-1912`).

---

## 6. Info boxes, strategy screen, scout report, map

### 6.1 UnitInfoEvent (`FIGT.ADV:834-1020`) and EnemyInfoEvent (`:1021-1119`)

Both open window 143 (`L_0509c`, CG 894, text rect 200,196,240,200) over the battlefield (after
`FIGT_SEL` restored it), colour 22. First line at size 24, then `ZM 16` (`:858`, `:1041`); the `R`
after the first line uses 24, later ones 16 (pitch 18), 8 px cells (30 per line).

| y | Unit info | Enemy info |
|---|---|---|
| 198 (size 24) | `[` + `X` name + `]` (`:853-857`) | `[` + `X` name + `]` (`:1036-1040`) |
| 224 | `  Strategy: ` + `Manual Attack` / `Auto Attack` / `Auto Defense` (order) (`:860-872`) | `  HP: ` + HP (`:1043-1044`) |
| 242 | `  Battle LV: ` + level | troops = 0: `  Commander Attack: ` Atk; else `  Troops: ` n |
| 260 | `  HP: ` + HP | troops = 0: `  Commander Defense: ` Def; else `  Troop Attack: ` TroopAtk + mod |
| 278 | troops = 0: `  Commander Attack: ` Atk; else `  Troops: ` n (`:883-899`) | troops ≠ 0: `  Troop Defense: ` TroopDef + mod; troops = 0: empty |
| 296 | troops = 0: `  Commander Defense: ` Def; else `  Troop Attack: ` TroopAtk + mod (`:900-943`) | `  Magic Attack/Defense: ` Magic (`:1096-1097`) |
| 314 | troops ≠ 0: `  Troop Defense: ` TroopDef + mod (`:946-989`); troops = 0: empty | `  # of Attacks: ` Attacks (`:1100-1101`) |
| 332 | `  Magic Attack/Defense: ` Magic (`:993-994`) | |
| 350 | `  # of Attacks: ` Attacks (`:997-998`) | |
| 368 | `  Surging with power` if the partner is deployed (`:1001-1004`) | |

The `R` that ends the troops block (`:992`, `:1095`) stands outside it, so with no troops the
commander block's own last `R` plus this one leave an empty line; Magic and Attacks keep their
rows either way.

"mod" = the modifier for the battle kind (`Unit_Off/Def/DgnAtkMod`, `…DefMod`; enemy
`Cmdr_TroopAtkMod/DefMod`): > 10 → `(-` + (mod − 10) + `)`, < 10 → `(+` + mod + `)`, = 10 → only
`)` is printed (script quirk). The last unit-info line ends at y 368 + 16 = 384 (fits).

Then: wait until the buttons are released, op 11 (key wait, text erased), op 10 (close),
`ZC 1, 22`, `ZM 24`, wait for release (`:1005-1019`, `:1104-1118`); `FIGT_SEL` runs again.

The order messages (`L_0129e`, `:776-833`) use the same window: `X` unit name / `received orders`
/ `to wait for` / `instructions.` (or `to keep attacking.` / `to keep defending.`).

### 6.2 StrategyScreenEvent (`FIGT作.ADV:15-33` side 1, `:357-375` side 2)

Composed in the back buffer, then `CC 0, 400, 640, 400, 0, 0`:

| Order | Side 1 (player) | Side 2 (enemy) | Visible at |
|---|---|---|---|
| 1 | `J1 0, 400` + `G 899` | same | (0,0) 640×400 texture |
| 2 | `G Bt_BackgroundCg` | `G 900` — always the plains picture (`:360`) | (0,0)-(640,256) |
| 3 | strategist's sprite by troops vs limit (`Unit_Cg*`) | `Cmdr_Cg*` | left / right half (embedded x) |
| 4 | `J0 0, 544` + `G Unit_FaceCg, 40` | `J0 448, 544` + `G Cmdr_FaceCg, 40` | face at (0,144) / (448,144), 192×256, over the picture's lower part |

No overlay (+1) and no counters. Then window 143 (`Ally army's strategy` / `was successful.`,
starting with an empty line), the strategist's own line in 141/142/151/152, and the effect text
in 143 (`(+10%)` / `(+50%)`, enemy `(-…)`), each closed with op 11 + op 10 (`:35-46`, `:313-346`,
`:376-489`). After return, `FIGT.ADV:97/102` redraws the battlefield.

### 6.3 ScoutReportEvent (`FIGT_B.ADV:649-672`)

Inside window 113 (CG 809, text from (140, 280), 31 cells), opened with the ninja's portrait by op
321 (e.g. `CallArg : 94` for Saitou, `:576-579`) at (448,144). After `The Approaching Enemy` /
empty / `[` + `X 10` + `]` and a key wait (`:586-593`), each enemy commander:

| y | Text |
|---|---|
| 280 | `[` + `MP 4, 20` (name) + `]` (22 cells) |
| 306 | troops ≠ 0: `[` + `MP 5, 20` (troop type) + `]` |
| 332 | troops ≠ 0: two spaces + `H 0, Cmdr_Troops` |

then op 1 (key wait + page clear, `:671`). Afterwards `Commanders` / ` ` + count / `Total
Numbers` / ` ` + total (`:612-623`), op 1, op 0 (close), op 322 (remove portrait), and Rance
answers in window 111 (`:624-648`).

### 6.4 Map views — MapEvent (`M.ADV`, `WAO_MAP.ADV`)

| Mode | Script | Visible effect |
|---|---|---|
| 0 | `M.ADV:1-19` | `PC 6` + `G Ui_MapCgTopL` (loads the map palette without drawing), `PC 7`; `WAO_MAP` 0: `CC Tmp0021 + 1280, Tmp0022, 640, 400, 0, 0` — the map at camera corner (MapCamX − 320 ≤ 1280, MapCamY − 200 ≤ 400) fills the screen; clears portrait/centre state |
| 1 | `M.ADV:20-122` | camera scrolls to MapTarget: per step the map is redrawn (`WAO_MAP` 0) and `Y 3, 5` (80 ms); step ≈ 10 px, or distance/15 for long distances |
| 3 | `M.ADV:123-126`, `WAO_MAP.ADV:86-115` | redraws the six tiles at (1280 + 640·i, 0/400) and every marker `Ui_MarkCg` at (MarkX + 1280, MarkY) with key 40; a value > 10000 is drawn as CG − 10000, 18 px higher. Not visible until a mode 0 |
| 4 | `M.ADV:127-133`, `WAO_MAP.ADV:116-123` | adds/updates the marker at the target with CG `Tmp0021 + 10000`; the battle passes `Prov_Owner + 819` (`FIGT_A.ADV:108`, `FIGT_B.ADV:773`) → owner flag CG 820-830 (24×19) |
| 10 | `M.ADV:140-181` | sound 109; CG 814 (32×32 target cross) at (640,0); four times: sprite-copy to (320 + Tx − Cx − 16, 200 + Ty − Cy − 16) (camera clamped to [320,1600]×[200,600]), `Y 3, 8`, restore from the map, `Y 3, 8` — four blinks, ≈ 1 s |
| 13 | `M.ADV:201-239` | as 10 without the sound |
| 11 | `M.ADV:182-186` | `EC 0` (black) + `Y 3, 30` |
| 12 | `M.ADV:187-200` | mode-0 redraw + `Y 3, 30` |

Battle use: `FIGT_A.ADV:72` (0), `:111-116` (4, 3, 0), `:124-125` (13); `FIGT_B.ADV:6-14` (0, 1,
10), `:82-83`, `:245-246` (0), `:776-783` (4, 3, 0, 10), `:861-866` (4, 3, 0); `FIGT.ADV:1866-1867`
(0, end of a field battle). Dungeon counterpart: `ClearScreenEvent` = `EC 0`
(`FIGT.ADV:1869-1871`).

---

## 7. Fonts and colours

### 7.1 Font sizes

| Command | Value | Where | Used for |
|---|---|---|---|
| `ZM` (message) | 24 | `ｽﾀｰﾄ.ADV:854`; `WAO_SUB.ADV:175, 184, 198, 207, 221, 244`; `FIGT.ADV:1014, 1113` | all battle text (panels, header, windows, prompt box) |
| `ZM` | 16 | `FIGT.ADV:858, 1041` | info boxes after the first line |
| `ZS` (selection) | 16 | `ｽﾀｰﾄ.ADV:855` | popups and script menus |

Line pitch = size + 2; selection item pitch = size + 2 (`selection.c:274, 307`). Text is
**bold**: the engine's default weight is bold (`ags.c:141`), and the only `ZB` commands in the
scenario (`各種DATA.ADV:116-272`, small chart labels) switch to thin and back to `ZB 7` (bold)
right away. No text shadow (`ZA` never used; `ags.c:139` default none). The face is **MS Gothic**:
`system39.exe` names `ＭＳ ゴシック`, and xsystem35 on Windows opens `msgothic.ttc` face 0 (all text is
gothic, the mincho switch never occurs) — [../assets.md](../assets.md) §6.

### 7.2 Palette indices used by the battle

Indices 10-245 come from the last CG drawn; every CG the battle draws (UI 807-899, faces,
sprites, backgrounds, map tiles, province pictures) has identical values at the indices below
(checked over all 1222 PMS files: only the 640×294 event CGs 101-240 etc. and CGs 806/862 differ,
CG 396/815 load no palette). 0-9 and 246-255 are fixed (`ags.c:58-81`). The test
`GameDataAssetTests.Battle_text_colours_agree_over_the_battle_cgs` re-checks this table over
CGs 1-100, 801-919, 1001-1064 and 1100-1530 (806/862 excluded: dungeon and gallery screens,
never drawn by the battle).

| Index | RGB | Use (set by) |
|---|---|---|
| 0 | #000000 | dungeon background `CF … 0` (`FIGT.ADV:1994`), portrait erase, `EC 0` |
| 11 | #d7ebd7 | text in dark bubbles 142/152 (`WAO_SUB.ADV:464, 484`) |
| 15 | #879187 | default message colour (`ZC 1, 15`: `ｽﾀｰﾄ.ADV:856`, window opens/closes); invalid-target frame (`FIGT_SEL.ADV:166, 169`) |
| 18 | #bec3dc | panel text (`FIGT.ADV:2011`); message background `ZC 6` (`FIGT.ADV:34`); selection background `ZC 4` (`ｽﾀｰﾄ.ADV:866`); own-panel hover frame (`FIGT_SEL.ADV:179-182`) |
| 22 | #6e7382 | battle message text (`ZC 1, 22`, §2.6); selection text `ZC 2` (`ｽﾀｰﾄ.ADV:864`) |
| 40 | (varies per CG) | transparent key of every `G …, 40` / `CS …, 40` |
| 178 | #e0e0e0 | window 131 background (`ZC 6, 11·16 + 2`, `WAO_SUB.ADV:223`) |
| 190 | #505050 | window 131 text (`ZC 1, 11·16 + 14`, `WAO_SUB.ADV:222`) |
| 240 | #dcff91 | highlighted panel text (`FIGT.ADV:596, 1414, 1445, 1591`) |
| 249 | #ff0000 | special-attack name in the prompt (`FIGT_SEL.ADV:92`); target frame in special modes (`Tmp0020 = 249`) |
| 255 | #ffffff | selection frame `ZC 3` (`ｽﾀｰﾄ.ADV:865`), item hover rectangle; normal target frame (`Tmp0020 = 255`); `ZC 6, 255` at battle end (`FIGT.ADV:592`) |

No `PS` (palette set) command appears in any battle file; `PC` appears only in `M.ADV:11-13`.

---

## 8. Notes for the UI and open questions

Script behaviour without a matching core event (the UI must reproduce it from context):

- the full-screen copy that ends every `RedrawAsync` (`FIGT.ADV:2055`);
- the removal of the "UNABLE TO FIGHT" marks after the exchange key (`FIGTATTK.ADV:248-253`);
- the `EC 0` before a dungeon engagement (`FIGT.ADV:1626`, immediately overdrawn);
- all drawing inside `FIGT_SEL.ADV` (prompt, hover frames, popups) and in script menus;
- the window text colour (`ZC 1` history, §2.6).

Unknown / unverified:

1. Glyph metrics: the face is MS Gothic (fixed-pitch half-width cells of 12 px at 24, 8 px at
   16); whether the original's synthetic bold changes the advance is unverified.
2. ~~`Ui_FaceOverMap` during battles~~ — resolved: taken from the state, both values occur (§2.4).
3. ~~Contents of button index 99~~ — resolved: zero in every capture, nothing drawn (§4.2).
4. That `#L_05126, 1` selects `L_0512e` (as the core assumes) — the panel geometry depends on it;
   `L_0516e` is never selected. Answered 2026-10-23 from xsystem35 `cmd_check.c:93-104` and the
   captures: it does. `#label, n` with n ≠ 0 reads the n-th 4-byte address of the table at the
   label (`sl_getdAt(index + 4 * (n - 1))`) and points `F3` there, so `#L_05126, 1` takes the
   first entry, `_L_0512e` (`FIGT.ADV:3264-3267`); `F3` then reads 16-bit words from it
   (`cmdf.c:110-116`). All 310 memory dumps of the battle captures taken after `FIGT.ADV:601-613`
   hold `L_0512e`'s rectangles in `Ui_BtnX1`/`Ui_BtnY1`/`Ui_BtnW`/`Ui_BtnH` (12, 8, 182, 84 …),
   none `L_0516e`'s (8, 8, 182, 82 …).
5. Exact `EC 0` fill size in the original (xsystem35: 639×399 without `patch_ec`).
6. Frame pacing of the four slide steps (`FIGT.ADV:1816-1827` has no pause between them).
7. The run-time `ZC 1` value for each individual message was derived from the write sites listed
   in §2.6, not traced message by message.

Items 1, 5 and 6 are purely visual and will not be pursued: the owner decided (2026-10-08) that
the port's look need not match the original pixel for pixel — mechanics stay exact, the
interface may be our own. Item 4 mattered because the panel rectangles are also the click
targets; it is answered (2026-10-23).
