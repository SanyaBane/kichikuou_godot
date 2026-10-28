# The phone — the build plan (2026-10-28)

The phone's base versions are drawn and chosen in both orientations (docs/ui/phone.md, rounds P1-P19; the screen
gallery's tabs **Smartphone, upright** and **Smartphone, landscape**, docs/ui/gallery.md, §9). This document is the plan
for building them: what the code has today, the owner's answers that set the order, the foundation, and the steps. It is
the implementation's governing document; each step records here what was built and where it stands.

The rules stay the original's (docs/ui/style.md, §1): a phone layout moves and enlarges what our screens draw and where
the finger answers; every answer still reaches the script through the inputs it reads. **The PC and the tablet do not
change**: every phone step leaves the screen gallery's PC and touch passes "same".

## 1. The owner's answers (2026-10-28)

1. **Landscape first, then upright.** The landscape phone keeps the 1280×800 frame (LM1's principle), so the scenes and
   the engine's picture stay where they are and the whole game is playable on the phone soonest; the foundation is
   proven on that case. Until the upright phase the phone is held to landscape (either way round, by the sensor).
2. **The reference phone and longer screens.** The layouts are the Xiaomi Poco X6 Pro's (20:9, 1220 × 2712); a longer
   screen (21:9) gets wider black sides in landscape and more free height upright. 19.5:9 nearly fits (landscape sides
   13.4 mm against 14.5). 18:9 and shorter are put off (docs/backlog.md): their landscape sides are ~7.6 mm, too narrow
   for LM1's rails, and need mock-ups of their own.
3. **The tablet stays landscape.** The orientation choice in Option → Display (System, Upright, Landscape 1,
   Landscape 2; PO1o) is a phone's only; the tablet keeps its sensor landscape and its layout.
4. **The missing base versions are drawn before any build** (the owner's rule of 2026-10-27, "base versions first"):
   three screens of ours have no phone mock-up in either orientation — the **Menu sheet** (the menu bar's File, Screen,
   Option, MsgSkip as finger rows, which the Menu cell opens), the **message log** (`MessageLogView`), and **the
   comment's editing on Save/Load** with the on-screen keyboard up. Rounds P20 (upright) and P21 (landscape), step 0
   below.

## 2. What the code has today

Surveyed 2026-10-28 (references as of commit `199e397`):

- **The frame.** `GameScreen` lays a black `ColorRect` under `_content` (under the 24 px menu bar,
  `GameScreen.MenuBar.cs`), in which `_view`, a `TextureRect` with `KeepAspectCentered`, shows the engine's 1280×800
  picture (`GameScreen.cs:303-317`, fed at `:533-544`). The black sides are what `KeepAspectCentered` leaves; **nothing
  draws in them** — the tablet's drawer, Commands and the battle's rails all stand inside the frame
  (`SkipButtons`, `CommandsButton`, `BattleCommandLayer.Touch.cs:22`).
- **One frame, copied.** The picture's rectangle and its scale are computed in one way, but the formula is copied
  eleven times in `GameScreen` and its partial files (`PictureArea()`, `GameScreen.cs:2088-2094`, is the canonical one),
  and again in `BattleScreen`, `GalleryScreen` and `ImportScreen`.
- **The views are 1280×800 through and through.** Every view of ours is an unanchored `Control` placed each frame by
  `GameScreen._Process` with the frame's rectangle (`Place(Rect2)`, `k = area.Size.X / 1280`) or an origin and a scale,
  and draws in `_Draw` through `DrawSetTransform(k)` at layout-pixel rectangles hard-coded in the view or in its model
  (`SkipButtons`, `MessageWindow`'s panels, `DeploymentPanels.TableArea`); each hit-tests by dividing by its own `k`. No
  view lays itself out from its area's shape.
- **The touch mode** is the static `TouchMode` (`On`: Android or `--touch`; the pointer helpers), read 80 times in 30
  files, and **frozen at load in several views**: `BattleBoardView.Narrow` (`static readonly`), `CardPortrait`'s widths,
  the `readonly bool _touch` of `CommandMenuView`, `ProvinceMenuView`, `FloorLadderView`, `BattleDialogLayer`,
  `BattleCommandLayer.Touch`, and `LedgerTabsBar`. A phone that turns while the game runs needs them read live.
- **Orientation, safe area: none.** `project.godot` sets sensor landscape (`handheld/orientation=4`), the export
  immersive mode; nothing calls `ScreenSetOrientation`, `GetDisplaySafeArea` or the cutout functions; a resize is only
  followed because `_Process` places the views every frame.
- **The engine's picture can move.** `_view` is a plain `TextureRect`, and `ToScript`/`FromScript`/`PictureArea` read
  its place and size, so the picture can stand anywhere (upright's PS2 puts it in the screen's middle). The catch: every
  view takes the picture's rectangle as its own frame, so upright needs a layout area apart from the picture's.
- **Input.** The script's pointer is converted in one place (`GameScreen._GuiInput` → `ToScript`, or
  `BattleBoardView.ToScript` in a battle); taps arrive as emulated mouse events (`emulate_mouse_from_touch` on by
  default); our views hit-test themselves (about 20 `_GuiInput` overrides).
- **The screen gallery** dumps at a forced 1280×800 window (`GalleryScreen.cs:261`), cropped to `PictureArea()`
  (`:286-291`); its passes are argument lists in `tools/ui/gallery.ps1` (the touch pass is `--touch`); the phone tabs are
  `tools/ui/mock_phone.py`'s drawings, not dumps.
- **Settings** are static `*Choice` classes over `user://settings.cfg` (section `[display]`: `font`, `ui_palette`,
  `ledger_row_height_touch`, …); Option → Display is `OptionsView.DrawDisplay` (`OptionsView.cs:486-515`).

So the frame is decided in one place in practice, but the 1280×800 coordinate system is in every view: **each screen
needs a phone layout of its own** (as phone.md, §1 measured), chosen by one live switch and handed out from one place.

## 3. The foundation

Built in step F (landscape) and completed in step P0 (upright).

- **The device, decided live.** One `DeviceLayout` — `Pc`, `Tablet`, `PhoneLandscape`, `PhoneUpright` — decided from
  the touch mode, the screen's physical size and the window's shape, and **read live** wherever a view lays itself out
  (the frozen reads above become reads of it; `TouchMode.On` stays the input's switch: no hover, the long press). A
  phone is a touch screen whose short side is under ~90 mm (`ScreenGetSize` / `ScreenGetDpi`: the Poco 69.5 mm, the
  Galaxy Tab S7 FE 167 mm; robust to Android's density buckets). On the PC `--phone landscape` or `--phone upright`
  (implies `--touch`) opens a window of the reference phone's shape, fitted to the monitor, so every phone screen can be
  looked at and dumped without the device.
- **The phone's own unit: the reference phone's pixel.** The mock-ups and phone.md measure everything in the Poco's
  pixels (1220 × 2712, 17.6 a millimetre), so a phone layout is written in those: the figures of the documents carry over
  unchanged. Landscape scales by the screen's height (1220), upright by its width (1220); the other side flexes (the
  sides' width, the free height). A pure model in `Original.Content` — `PhoneFrame`, with tests — gives, for a screen
  size, an orientation and the safe insets: the frame's rectangle, the black sides' rectangles (landscape), the
  picture's and the layout area's rectangles (upright), and the factor from the reference pixel to the canvas.
- **One frame helper.** The eleven copies of the picture's fit become one (`PictureArea` and its scale), which the
  phone's frame then replaces on a phone; the views take what they take today (a rectangle, or an origin and a scale)
  on the PC and the tablet, and a phone view takes `PhoneFrame` too.
- **The safe area.** `DisplayServer.GetDisplaySafeArea()` on Android; the reference's values — the camera's 110 px
  (6.3 mm) kept free on **both** short sides in landscape (either may hold the hole, phone.md, §12) and at the top
  upright, the gesture bar's 50 px (3 mm) at the foot — as the fallback and under `--phone` on the PC.
- **The sides drawn.** On the phone the menu bar goes (the Menu cell opens the Menu sheet instead), `_content` takes the
  whole screen, and the black sides are ours to draw in (still black under our controls, as the frame's surround is
  today).
- **The orientation** (step P0): Option → Display's four ways on a phone (PO1o) through
  `DisplayServer.ScreenSetOrientation`; System is "as the phone is held, unless its own rotation lock holds it" —
  which Godot constant maps to Android's `fullUser`/`user` is to be checked on the Poco. `handheld/orientation` lets the
  activity turn; a turn at any moment lays every open view out again (the game waits on nothing for it).
- **The screen gallery's phone passes.** `gallery.ps1` gains real passes `phone-land` and `phone` (`--phone …`, the
  window's size an argument instead of `GalleryScreen.cs:261`, the whole window dumped, not `PictureArea()`); dumps at
  the reference's pixels if Windows lets a window be that large, else at a fixed fraction (decided in step F). As each
  phone screen is built, its entry dumps on the pass and its mock-up leaves `mock_phone.py`'s `GALLERY_ENTRIES_LAND` /
  `GALLERY_ENTRIES` (gallery.md, §9); the tab shows dumps and the mock-ups not yet built, each marked.
- **The device.** The Poco by USB through `run-android.cmd` (`ANDROID_SERIAL` when the tablet is connected too;
  `-PushGame` the first time). **The speed** is measured on it in step F — the engine surface is drawn per pixel in C#
  (docs/backlog.md, "Android": not measured yet) —: frame times in a scene, on the map and in a battle, recorded here; a
  phone too slow to play is a task of its own before the screens.

## 4. How a step is made

Every step below, in the house's way (docs/ui/style.md, §7) with the mock-ups already chosen:

1. Read the screen's section of phone.md and look at its mock-up on the gallery's phone tab (`gallery.ps1 -PhoneOnly`).
2. Where the layout has logic of its own (the grid folded in two, which window goes above the picture, the ledger's
   columns at the phone's type), a model in `Original.Content` with tests first.
3. The view's phone layout: a branch of its own keyed by the live `DeviceLayout`, written in the reference phone's
   pixels; the PC's and the tablet's code paths left as they are.
4. The gallery: the entry dumps on its phone pass, its mock-up leaves the tab; the PC and touch passes stay "same".
5. Unattended play with `--phone …` where the step changes a flow the bot goes through (`--auto` from the kept saves,
   CLAUDE.md, "A look at one screen"); dumps through `--shots` keep the event CGs masked.
6. The owner's look on the Poco; this document says "awaiting the owner's look on the phone" until then.
7. A commit per step (docs and code together); the tests the change reaches (CLAUDE.md, "Commands": Godot-only code
   needs the build, the core tests and its own tests; a model in `Original.Content` its content tests, quick run).

## 5. The steps

### Step 0 — the missing base versions (rounds P20, P21)

Drawn by `mock_phone.py --round P20` (upright) and `--round P21` (landscape) over the gallery's dumps, the sheets sent
to the owner before the question (as every round), the owner's choices recorded in phone.md and added to the gallery's
phone tabs:

- **The Menu sheet** — the menu bar's commands (File, Screen, Option, MsgSkip; Debug in a development build) as finger
  rows (7.4 mm), opened by the Menu cell (upright the bar's left, PK1; landscape the left side's top, LM1/LS1).
- **The message log** — the paper chronicle (mock-up L3a, docs/ui/messages.md, §5 step 6) at the phone's type, opened by
  the Log cell, scrolled by a finger, closed by a tap off it (touch.md, "The Skip and Log buttons").
- **The comment's editing** on Save/Load (PL2, LL1: Edit) — where the field stands while Android's keyboard covers the
  screen's lower part.
- **The original's own screens**, shown as they are for the owner to confirm (no layout of ours): the Fortune Check
  (`HAPPY_C`), the CG gallery (`CG`), the music room (`MMODE`), an ending's Completion Report (`OVER`) — landscape in the
  frame as LS1, upright at the screen's width with the bar; a tap is the click, a long press the right click. They are
  drawn over new screen gallery entries of those screens, which `GalleryTitle` reaches by a click on their button (the
  backlog's "The title's other screens in the screen gallery", taken into this step, 2026-10-28; Alice's Mansion and
  Saitou are scenes, laid out as PS2 and LS1, and the debug starts' menu is a development screen).

**Where it stands (2026-10-28):** the gallery entries are built — `fortune-check`, `cg-gallery`, `music-room`
(docs/ui/gallery.md, §7 step 7); the ending's Completion Report is taken from a `--ending 11` run. Rounds P20 and P21
are drawn (docs/ui/phone.md, §25-§26) and chosen by the owner: the Menu sheet PN1 (PN1q) and LN3, the log PR2 and LR2,
the comment's editing PY2 and LY1, the original's own screens as they are (PX…, LX…) — all on the gallery's phone tabs.
**Step 0 is done**; every screen of ours has a chosen base version in both orientations. L1 builds the Menu sheet as
LN3 and the log as LR2; L7 the editing as LY1.

The development screens (the capture viewer, Portraits, Effects, the screen gallery, Debug's sliders) get no phone
layout: on a phone they show as on the tablet, in the frame (the agent's choice, below).

### Landscape (steps F, L1-L10)

| Step | What | Base versions (phone.md) |
|---|---|---|
| **F** | The foundation (§3): `DeviceLayout`, `PhoneFrame`, the one frame helper, the safe area, the sides, the menu bar gone, `--phone landscape`, the gallery's `phone-land` pass, the speed on the Poco. Done when the game runs on the Poco in the frame with today's touch layout inside and black sides, the PC and touch passes same. | §12 (the frame) |
| **L1** | The scenes and the left side: Menu on top of the left side, Skip and Log at the left thumb (the drawer out of the frame, so 131 no longer docks smaller), the `]` menu's rows at the frame's right foot; the Menu sheet and the message log (round P21); the week report's side; the title's description. | LS1, LS1b, LS1m (§19); LW1 (§14); LEd (§23) |
| **L2** | The map: the status stacked in the left side, the week's uses by group in the right, Commands at the right thumb; the command panel in the sides (two-line rows 14.5 × 7.0 mm, Turn END on the right's top, Close where Commands was); a province's card beside it at the phone's type, its commands in the right side; plates with 36 px text. The status no longer over the map, so the map's headroom (`MapHeadroom`) stays off on the phone. | LM1, LM1b, LM1c (§12) |
| **L3** | The battle's turn: the scene's picture at 70%, its sides blurred to the frame; the cards 25.5 × 19.0 mm, their text 40-45 px, the order toggles across the card's foot; the rails in the black sides (rows 14.5 × 7.0 mm, Melee 14.8 mm with the damage forecast at the thumb); Skip on the left rail's top. The retreat question beside Retreat; an exchange with its counters in the scene's blurred sides. | LB2 (§12), LB2r1, LB2x (§17) |
| **L4** | The battle's other states: the opening at the scene's place, the scout report and the strategy's cut-in across the width, the results plate (a level-up's page, a fall's box, our panel the same way), a fighter's line (the portrait in the blurred side, the balloon at 3.5 mm), a defence's ribbon and caption. | LB3o1, LB3s, LB3c, LB3r, LB3b (§17) |
| **L5** | The deployment table and Subordinates: the ledger in one-line rows at the phone's type (rows 112 px, every column of a tab in one line), wholly at the left on the deployment table; the party two cards a row at the right with their ✕, Stop \| Attack at the right thumb; Subordinates' Add New Troops in the left side, Talk, Replenish and End in the right, Replenish's question in a callout left of it; a unit's picture on a long press. | LD2, LU1, LU1q (§13) |
| **L6** | The enemy attack: the facts at the left, the answers in a column at the right thumb (Abandon City narrower, Intercept half as high), Ninja Scout's list in two columns (five ninjas in view). | LA2, LA2n (§14) |
| **L7** | Harem and Treasure with their grid folded after its ninth row (a model, tested), every cell in view, long item names shrunk; Save/Load as the PC's split, the comment's editing as round P21 chose. | LH2, LT2, LL1 (§15) |
| **L8** | The options window across the width, each tab in two columns; the dungeon's ladder and entry menu as the tablet's rail at the phone's sizes. | LO1, LF2, LF2e (§16) |
| **L9** | All Data (the graphs as cells in the left side, the scales as chips in the right, the bars' figures drawn again at 2.3 mm); the title as the PC's (Menu in the left side); the import screen on its paper sheet; the original's own screens as round P21 chose. | LG2m, LG2, LE1, LI, LIp, LIr (§23) |
| **L10** | The landscape phone whole: unattended play with `--phone landscape` from the kept saves through every exercise it has (the deployment, the battle, `--treasure`, `--saveload`, `--scout`, `--dungeon`, `--harem`), the owner's play on the Poco, the docs (phone.md, touch.md, style.md §6, backlog). | — |

**Step F, where it stands (2026-10-28):** done — built, run on the Poco and measured there (below, "The Poco").

- **The device**: `Original.Content/Devices/DeviceLayout` (the enum and `DeviceLayouts.IsPhone` / `Decide`, tested) and
  `godot/Strategy/Device`, which reads it live — the main window followed from `Main` (`Device.Follow`: the layout and
  the safe area read again on every resize or turn), `Device.Phone` from Android's screen size and dpi or `--phone`,
  `Device.TouchLayout` for the views that take the tablet's layout. `TouchMode.On` stays the input's switch and is on
  with `--phone` too. The frozen reads of §2 are live reads of `Device.TouchLayout` now: `BattleBoardView.Narrow` and
  the board's places that hang on it, `CardPortrait`'s widths, the `Touch` of `CommandMenuView`, `ProvinceMenuView`,
  `FloorLadderView`, `BattleDialogLayer` and `BattleCommandLayer`, `LedgerTabsBar.RowHeight`, `LedgerRowsChoice`.
- **The frame**: `Original.Content/Devices/PhoneFrame` (tested, `PhoneFrameTests`): for a screen held landscape and its
  safe insets, the frame (fitted to the height, centred; between the hole's strips on a screen too short for it), the
  black sides whole, the room in them our controls may take, `Unit` (the canvas's units a reference pixel) and `Px`.
  The insets are the system's when it reports them — the larger side's on both sides, so nothing moves when the phone
  is turned round, and at least the gesture zone at the foot, which Android's safe area does not count — else the
  reference's 110 / 50 px.
- **One frame helper**: `godot/Strategy/ScreenFrame` (`In` for the device, `Fit`, `Scale`, `Pixels`, `ToScript`,
  `FromScript`) in place of the eleven copies: `GameScreen.Frame()` (`PictureArea()` is its area) and its partial
  files, `BattleScreen.ToScript`, `GalleryScreen.PictureArea`, `ImportScreen.Layout`.
- **The phone's screen**: `GameScreen.UpdateDevice` (each frame): on a phone the menu bar is hidden and `_content`
  takes the whole screen; held landscape `_view` stands at the phone's frame, its black sides ours. Nothing is drawn in
  them yet (L1 on); the Menu sheet comes with L1, so until then the phone has no File/Option menu (Save and Load are
  in the command panel).
- **`--phone landscape`** (implies the touch mode): the window at the reference phone's shape, as large as the
  monitor's usable area allows, at most 2712 × 1220 (`Device.OpenPhoneWindow`; 2544 × 1144 on the owner's 2560 × 1440
  monitor). `--phone upright` is refused until P0.
- **The gallery's pass** (the agent's choice): a pass of its own, `phone-landscape` ("Smartphone, landscape";
  `gallery.ps1 -PhoneLand`), beside the mock-up tab `phone-land`, which stays as it is — the mock-up tab's folder keeps
  its name, and each screen's mock-up leaves `GALLERY_ENTRIES_LAND` as its phone layout is built. The pass dumps every
  entry with `--phone landscape --window 1808x813`: the whole window, its black sides with it, at **two thirds of the
  reference's pixels** (a window of 2712 × 1220 does not fit a common monitor; at two thirds the frame is 1301 × 813,
  about the PC's own size, and the dumps are the same on every machine). `GalleryScreen.WindowSize` (`--window WxH`)
  replaced the fixed 1280 × 800, which stays the default. At F the pass shows today's touch layout in the frame: the
  baseline the L steps change entry by entry. The Lacquer and Touch passes were "same" (110 and 111 entries).
- **The speed**: `godot/Strategy/FrameTimes`, on on Android and with `--frame-times`: every 10 s the console (logcat,
  tag `godot`) gets the frames of each kind shown — a scene, the map, a battle — their count, mean, 95th percentile and
  longest interval, and the slowest frame's processing in each second (Godot's `TimeProcess` is that, updated once a
  second), its median and longest; `Device` logs the screen's size and dpi, the window and the safe area (§7).

**The Poco** (the owner's play, 2026-10-28: the debug build, the week-18 save copied to it; Android 16, HyperOS):

- **The device**: the window 2712 × 1220, the screen at 480 dpi (Android's bucket; the short side reads 64.6 mm, a
  phone), `PhoneLandscape`. The game is drawn under the cutout: the safe area is (120, 0)-(2592, 1220), the camera's
  hole **120 px** on the left (the reference's 110 was the mock-ups' guess), 0 on the right and at the foot (Android's
  safe area does not count the gesture zone), so `PhoneFrame` keeps 120 px on both sides and the reference's 50 at the
  foot. The process held 1.1-1.7 GB (HyperOS's log).
- **The speed** — the screen runs at 120 Hz, a frame 8.3 ms:

  | Screen | Frame interval: mean / p95 / longest | Slowest processing a second: median / longest |
  |---|---|---|
  | Scenes (the title, the week's start, after the battle) | 8.4-9.8 / 8.3-11.1 / 16-150 ms | 16-45 / 51-313 ms |
  | The map, held and dragged | 8.8-9.8 / 10-19.4 / 58-108 ms | 9-36 / 52-118 ms |
  | The map's first seconds (the talks read ahead, 190 ms) | 11.8-18.2 / 23-110 / 133 ms | 158 ms |
  | The battle, the player's turn | 14.6 / 37.7 / 150 ms | 43 / 459 ms |
  | The battle, the exchanges | **47.4 / 138-143 / 150 ms** | **146-151 / 178 ms** |

  The scenes and the map run at the screen's rate, a long frame now and then (entering a screen, the read-ahead). **The
  battle's exchanges run at about 21 frames a second**, a frame of 140-150 ms every few: playable, but the animation
  stutters. The cause guessed here (the whole 1280×800 picture composed and uploaded each frame) was wrong; the cause
  measured and its fix are "The battle's speed" below: the exchanges now run at about 100 frames a second.
- **Found on the way**: on Android 16 a folder `adb push` creates in the app's external folder is the shell user's with
  no access for others, so the game could not read the pushed game folder (an older Android let it); the Android
  scripts now open what adb put there to the app (`Grant-AppAccess` in `tools/android-adb.ps1`, run by
  `export-android.ps1` and `copy-saves.ps1`). Xiaomi's phones also need Developer options → "Install via USB" before
  adb may install.

**The battle's speed** (2026-10-28, a task of its own before L3; the owner's play on the Poco, the week-18 save's attack
on Oaks (6), the debug build):

- **The cause**, measured with a probe of each frame's parts written to logcat (temporary, not committed). Not the whole
  picture: while our battle screen covers the view, `GameScreen` only takes the change, and the windows' layer was
  composed once in ten seconds. The time went to:
  - the scene's **front layer**, drawn again by `BattleScene` at every shots frame and counter of an exchange though
    without the numbers (our screen's) only its marks can change, then read and uploaded again by `BattleBoardView`:
    13.5 ms, 147 times in 7.6 s;
  - the **surface's reads and keyed copies** (`Surface`): the palette applied a byte at a time with a second pass for a
    layer's alpha, a `Buffer.BlockCopy` call per pixel in the keyed copies. These slowed the layers' reads (~13 ms
    each) and the engine's own drawing of an exchange (`BattlePresenter`: a shots frame 11.6 ms, the counters ~40 ms,
    up to 80);
  - **`BattleBoardView.Place` every frame**: 1.2 ms, its theme overrides set again (a theme change: the armies' names
    shaped and their lines sorted again) and every layer drawn again;
  - **all eight cards built again** when one changed: ~10 ms, then their layout;
  - and **our libraries compiled unoptimized** in Godot's export configurations: the .NET SDK optimizes only `Release`,
    so in `ExportDebug` (this build) and `ExportRelease` (the Windows release too) Mono and CoreCLR ran all our C# —
    the rules, the presenter, the surfaces — without the JIT's optimizations, which on the PC makes the same code
    5-10 times slower.
- **The fix** — nothing drawn differently: the PC and touch gallery passes same (110 and 111 entries):
  - `BattleScene` draws the front again only when what it shows changes: in an exchange with the numbers every time,
    without them only with its overlay or the marks;
  - `Surface` reads a row in one pass, a pixel a word, through a table of the palette for each alpha (stored, opaque,
    a layer's), copies keyed a word at a time into a buffer it keeps, and draws a CG's row from its span;
  - `BattleBoardView` lays the screen out only when its origin, scale or layout changed, and builds again only the
    cards whose contents changed;
  - `Directory.Build.props` sets `Optimize` for `ExportDebug` and `ExportRelease`; the editor's `Debug` stays as it is.

  The surface alone, a 640×256 layer on the PC (stored 1280×512):

  | | Unoptimized: before / after | Optimized: before / after |
  |---|---|---|
  | `ReadLayer` | 11.0 / 4.8 ms | 2.5 / 0.5 ms |
  | `ReadArea` | 10.6 / 5.3 ms | 2.0 / 0.6 ms |
  | `CopyKeyed` | 10.6 / 5.2 ms | 7.7 / 3.5 ms |
  | `DrawCg` | 4.2 / 2.2 ms | 0.4 / 0.5 ms |

- **On the Poco** — the screen at 120 Hz, a frame 8.3 ms:

  | The battle's exchanges | Frame interval: mean / p95 / longest | Slowest processing a second: median / longest |
  |---|---|---|
  | Before, MsgSkip on (without it not measured before) | 28.6-47.1 / 128-133 / 146-149 ms | 43-144 / 161-168 ms |
  | After, MsgSkip off | **9.4-10.2 / 11.9-15.0 / 91-142 ms** | 20-64 / 93-226 ms |
  | After, MsgSkip on | 15.8 / 64 / 111 ms | 20 / 106 ms |

  The exchanges run at about 100 frames a second, near the screen's rate; with MsgSkip, whose pauses take one frame
  each, every frame draws a step of the exchange. Still a long frame (40-140 ms) or two at an exchange's start:
  - the engine's own drawing of its start (`BattlePresenter`: the engagement picture, the slide and the counters,
    copies of 640×400 stored at 2×, ~13-45 ms on the phone, kept as the original draws them);
  - an effect's first play in a launch (its textures made per pixel in C#, `EffectTextures`: ~130 ms the first time,
    ~30 ms the second).

  Both are in docs/backlog.md, "Android". Note on the figures: a frame's interval is Godot's `delta`, so the script's
  work at a frame's start (`process_frame`) is counted in the next frame's interval; a period's figures are the same.

**Step L1, where it stands (2026-10-28):** built, **awaiting the owner's look on the phone**.

- **The models** (`Original.Content/Devices`, tested in `PhoneScenesTests`): `PhoneScenes` — the left side's cells, the
  mock-ups' rail from the hole's strip + 6 to the frame − 10 (Menu 110 high on top, Skip and Log 118 high at the foot,
  Log lowest); a `]` menu's rows at the frame's right foot (123 high, 12 apart, at least 500 wide and wider for a long
  item up to half the frame, lower with their type when a long menu would not fit the height); the log's paper (LR2);
  the week report's scale (LW1's card 1320 wide unless it would then leave less than 60 over and under it). `PhoneMenu`
  — the Menu sheet's pages of tiles (the menu bar's commands; Debug's own page), the questions in the PC's dialogs'
  words, the panel, the tiles four a row, a question's callout above its tile or, near the top, under it.
  `PhoneFrame.Box.Contains`.
- **The views**, each keyed by `Device.Landscape(view)` (the phone's frame over the view's parent; null on any other
  device, whose code paths are as they were): `SkipDrawer` draws Skip and Log as cells in the left side (`SideCells`:
  a plate, the icon left, the word right; Skip lit gold reading "ON" while the skip runs); `MessageWindowView` no
  longer docks 131 there; `SceneMenuView` puts the rows at the frame's right foot (LS1m); `MessageLogView` lays the
  paper across the width with Close at the Menu cell's place (LR2; its figures a `Metrics` record whose PC values are
  the former constants); `WeekReportView` draws the card ×1.44 over the map dimmed (LW1).
- **The Menu** (LN3): `MenuSheetView` — the cell, and the sheet it opens over the whole screen, modal as the PC's
  boxes — and `GameScreen.Phone.cs`: the cell shows on the phone wherever a game is shown and no screen of ours takes
  the side or waits on its own (`MenuCellShown`); the tiles act as the menu bar's items (`PhoneMenuChosen`); the PC's
  confirmation boxes now take their words from `PhoneMenu.QuestionOf`. On the phone the drawer and Log also show at the
  week report (`PhoneWeekReport`), as LW1 has them.
- **The agent's choices**, for the owner to confirm: Debug's tiles on a page of their own, ‹ Menu first, the Debug tile
  in a development build only (`OS.IsDebugBuild`; the PC's bar keeps its Debug always); Battle Captures and Screen
  Gallery ask in the callout too, as the PC's boxes do; the Menu cell also on the map's wait, over the province menu and
  over All Data (the steps that lay those out decide their side), not over our battle and full screens, the command
  panel and the enemy attack; All Data's `]` menu takes LS1m's rows until L9 gives it LG2m's cells.
- **The gallery**: the PC and touch passes same (110 and 111 entries); the phone pass's L1 entries match their
  mock-ups, three entries of its own show the sheet (docs/ui/gallery.md, §9), and LS1, LS1b, LS1m, LW1, LEd, LN3 and
  LR2 left `GALLERY_ENTRIES_LAND`.
- **Unattended play**: `--phone landscape` headless from the week-18 save to its deployment picker runs as before. The
  touch exercise in a window (`--harem` from the week-18 save) now also taps the Menu cell, Sound twice, Restart and its
  Cancel, then Close (`AutoPhoneMenuAsync`); not run — it needs a window, where a hand on the mouse can break it.

### Upright (steps P0-P9)

| Step | What | Base versions (phone.md) |
|---|---|---|
| **P0** | The upright foundation: the orientation choice in Option → Display (§3), `--phone upright`, the gallery's `phone` pass; `PhoneFrame`'s upright rectangles — the engine's picture at the screen's width wherever a screen puts it, and the layout area apart from it. Until P8 a screen not built yet shows the tablet's layout fitted to the width. | PO1o (§21) |
| **P1** | The scenes: the picture in the screen's middle, the upper speaker's window above it and the lower one's under it (a thought by its thinker's portrait the same way; the narration and a speaker with no portrait under it), no name plate; the script's breaks kept (32 cells at 3.6 mm, 49 at 2.6 mm), four lines' height so the picture never moves; 131 under its CG with its plate; a `]` menu's rows at the thumb in the bar's place; the bar with Menu, Log and Skip at its left; the Menu sheet and the log (round P20); the week report's card; the title's description and the requests slip. Which window is "upper" is surveyed first (the windows' kinds and places, `WAO_SUB`). | PS2, PS2l, PS2b, PS2m (§18); PK1 (§21); PW1 (§8); PEd, PEr (§22) |
| **P2** | The map: the whole height at the width, the status on top, the bar with Commands at the right thumb; the command panel as a sheet over the map's foot; a province ringed, its card and commands as a sheet, the map scrolled up by the sheet's height. | PM1a, PM1b, PM1c (§3) |
| **P3** | The battle's turn: our army on top, the enemy's at the thumb, the gauge across between the front rows; Skip on its strip; the deck (the acting unit's commands from the foot up, the target's attacks at the right thumb); the retreat question over the deck's right column; an exchange. | PB3, PB3r, PB3x (§6) |
| **P4** | The battle's other states. | PB4o3 (§20), PB4s, PB4c, PB4r, PB4b (§11) |
| **P5** | The deployment table (the party on top, the ledger at the thumb in two-line rows scrolled sideways — № and name over the type fixed, each tab its own scroll) and Subordinates (the orders under the ledger, Replenish and Talk on one row, End at the thumb with the budget beside it, the question under its button); a unit's picture on a long press. | PD2, PD2d, PU1, PU1q (§7, §20) |
| **P6** | The enemy attack: a screen of its own, Ys over the rebels, the answers at the thumb; Ninja Scout's list above its button. | PA1, PA1n (§8) |
| **P7** | Harem and Treasure (the picture on top, the grid scrolled), Save/Load on one page (the page on the bar, the list above it), the comment's editing as round P20 chose. | PH2, PT2, PL2 (§9) |
| **P8** | The options window (the paper sheet, its tabs on top) and the dungeon's ladder at the right thumb; All Data (the miniature on top, the graphs as tabs, the scales as chips); the title with a row for each button; the import screen. | PO1, PF1, PF1e (§10); PG2m, PG2, PE2, PI, PIp, PIr (§22) |
| **P9** | The phone whole: unattended play with `--phone upright`, turning the phone in every screen (each laid out again, nothing lost: a drag, a question open, a scroll), the owner's play on the Poco, the docs. | — |

## 6. The agent's own choices, for the owner to confirm

- A phone is told from a tablet by the screen's physical short side (under ~90 mm); no setting.
- On a phone the menu bar is gone; its commands are the Menu sheet's.
- The development screens get no phone layout (they show as on the tablet, in the frame).
- Until the upright phase the phone is held to landscape, both ways round by the sensor.
- In landscape the status lives in the sides, so the map's headroom under the strip stays off on the phone.
- The phone's layouts are written in the reference phone's pixels, so phone.md's figures are the code's.

## 7. Risks and checks

- **Speed** on the phone (the per-pixel engine surface; §3): measured in step F — the scenes and the map at the
  screen's rate, the battle's exchanges at ~21 frames a second (step F, "The Poco"), now ~100 (step F, "The battle's
  speed"); a long frame or two at an exchange's start left.
- **The safe area's values**: the Poco reports a hole of 120 px (not the reference's 110) and nothing at the foot; the
  frame reads Android's, the gesture zone's 50 px kept, and the fallback serves only the PC (step F, "The Poco").
- **The keyboard**: Android shows it for the comment's `LineEdit` (`SaveLoadView`); how much it covers and whether Godot
  reports it (`DisplayServer.VirtualKeyboardGetHeight`) is checked in the step that builds the editing.
- **System's rotation lock** (§3): checked on the Poco in P0.
- **A tap's move before its press** (found in round P20, phone.md §25): Fortune Check and the CG gallery act on a click
  on what the read before it lit, so a tap whose move and press reach the script in one poll would act on the cell lit
  before; checked when the original's screens are built (L9), on the tablet too.
- **The tablet unchanged**: the PC and touch gallery passes same after every step, and the tablet's play by the owner
  after F and after L10.
